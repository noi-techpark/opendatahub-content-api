# Chatbot Architecture Redesign

## Implementation Status

### ✅ Completed (2025-10-21)

**Phase 1: Pandas-Based Aggregation** - FULLY IMPLEMENTED
- ✅ Removed AUTO mode from aggregate_data
- ✅ Implemented flatten_data tool
- ✅ Implemented dataframe_query tool
- ✅ Updated system prompt with mandatory workflows
- ✅ Comprehensive documentation

See `IMPLEMENTATION_SUMMARY.md` for details.

### 🔄 In Progress

None currently.

### ⏳ TODO

**Phase 2: Fulltext Search Tool** - NOT STARTED
- ⏳ Implement Whoosh-based search_in_data tool
- ⏳ Update system prompt

**Phase 3: Truncation Policy** - NOT STARTED
- ⏳ Review SmartTool max_tokens behavior
- ⏳ Ensure cache-first, never truncate

**Phase 4: Integration Tests** - NOT STARTED
- ⏳ Create test suite for each tool
- ⏳ Reproduce LLM usage patterns

---

## Current Problems

### 1. inspect_api_structure Rarely Used
**Issue**: Agent doesn't inspect structure before fetching large data
**Impact**: Fetches all fields when only few are needed

### 2. AUTO Mode Overused
**Issue**: aggregate_data defaults to AUTO, agent doesn't think about what fields are needed
**Impact**: Returns fields user didn't ask for, wastes tokens

### 3. Limited Aggregation Capabilities
**Issue**: Current aggregate_data only does basic operations
**Impact**: Can't filter, sort, or do complex pandas operations

### 4. Fulltext Search Inefficient
**Issue**: Large API responses sent to LLM for text search
**Impact**: Token waste, slow, error-prone

### 5. Token Truncation Issues
**Issue**: Tools truncate data when exceeding max_tokens
**Impact**: Data loss, incomplete results

### 6. Tool Usage Errors
**Issue**: LLM doesn't have enough examples/validation for complex tools
**Impact**: Frequent parameter errors, failed tool calls

---

## Proposed New Architecture

### Phase 1: Pandas-Based Aggregation Pipeline

```
┌─────────────────────────────────────────────────────────────┐
│  1. INSPECT (mandatory for large responses)                 │
│                                                              │
│  inspect_api_structure(api_type="dataset", dataset_name=...)│
│  → Returns: Available fields, types, samples                │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  2. FETCH with Field Selection                              │
│                                                              │
│  get_dataset_entries(                                       │
│      dataset_name=...,                                      │
│      fields=[...],  # Based on inspection                  │
│      raw_filter=... # OData filter if possible             │
│  )                                                           │
│  → Returns: cache_key (if large)                           │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  3. FLATTEN (convert nested JSON to tabular)                │
│                                                              │
│  flatten_data(                                              │
│      cache_key="...",                                       │
│      max_depth=2,                                           │
│      array_handling="explode"  # or "stringify"            │
│  )                                                           │
│  → Returns: Flattened data, stores as DataFrame            │
│  → New cache_key: "df_<id>"                                │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  4. PANDAS OPERATIONS (on cached DataFrame)                 │
│                                                              │
│  dataframe_query(                                           │
│      df_key="df_<id>",                                     │
│      operation="filter",                                    │
│      query="Shortname.str.contains('Hotel')",              │
│  )                                                           │
│                                                              │
│  dataframe_query(                                           │
│      df_key="df_<id>",                                     │
│      operation="sort",                                      │
│      by="Shortname",                                        │
│      ascending=True                                         │
│  )                                                           │
│                                                              │
│  dataframe_query(                                           │
│      df_key="df_<id>",                                     │
│      operation="groupby",                                   │
│      by="Dataspace",                                        │
│      agg={"Shortname": "count"}                            │
│  )                                                           │
│  → Each returns new df_key or final result                 │
└─────────────────────────────────────────────────────────────┘
```

### Phase 2: Fulltext Search Tool

```
┌─────────────────────────────────────────────────────────────┐
│  FULLTEXT SEARCH (Whoosh-based)                             │
│                                                              │
│  search_in_data(                                            │
│      cache_key="...",                                       │
│      query="hotel mountains",                               │
│      fields=["Shortname", "ApiDescription"],               │
│      limit=20                                               │
│  )                                                           │
│                                                              │
│  Flow:                                                       │
│  1. Load data from cache                                    │
│  2. Index specified fields with Whoosh                     │
│  3. Execute search query                                    │
│  4. Return matching items only                              │
│                                                              │
│  Benefits:                                                   │
│  - No LLM needed for text search                           │
│  - Fast, efficient                                          │
│  - Returns only matches (not all data)                     │
└─────────────────────────────────────────────────────────────┘
```

### Phase 3: No Truncation Policy

```
┌─────────────────────────────────────────────────────────────┐
│  TOOL OUTPUT POLICY                                         │
│                                                              │
│  IF result_size > max_tokens:                              │
│      cache_key = cache.store(result)                       │
│      return {                                               │
│          "cached": true,                                    │
│          "cache_key": cache_key,                           │
│          "size_info": {                                     │
│              "total_items": N,                             │
│              "estimated_tokens": X                          │
│          },                                                  │
│          "sample": result[:5],                             │
│          "next_steps": [                                    │
│              "Use dataframe_query to filter",              │
│              "Use search_in_data for text search",         │
│              "Use flatten_data for tabular view"           │
│          ]                                                   │
│      }                                                       │
│  ELSE:                                                       │
│      return result directly                                 │
│                                                              │
│  NEVER truncate or emergency_summarize                      │
└─────────────────────────────────────────────────────────────┘
```

---

## New Tool Specifications

### 1. inspect_api_structure (Enhanced)

**When to use**: MANDATORY before any large fetch

**System Prompt Rule**:
```
BEFORE calling get_datasets(aggregation_level="full") or get_dataset_entries:
  IF you don't know what fields exist:
    CALL inspect_api_structure first
    ANALYZE the fields
    THEN fetch with specific fields parameter
```

**Returns**:
```json
{
  "fields": [
    {"path": "Shortname", "type": "string", "sample": "Hotel ABC"},
    {"path": "GpsInfo.Latitude", "type": "number", "sample": 46.4983}
  ],
  "field_count": 47,
  "total_records": 8532,
  "recommended_fields": {
    "minimal": ["Id", "Shortname"],
    "standard": ["Id", "Shortname", "Type", "GpsInfo"],
    "detailed": ["Id", "Shortname", "Type", "GpsInfo", "ContactInfos", "ApiDescription"]
  }
}
```

### 2. flatten_data (NEW)

**Purpose**: Convert nested JSON to flat tabular structure

**Parameters**:
```python
cache_key: str  # Data to flatten
max_depth: int = 2  # How deep to flatten
array_handling: "explode" | "stringify" = "explode"
columns: list[str] | None = None  # Specific columns to include
```

**Example**:
```python
# Input (nested):
{
  "Shortname": "Hotel ABC",
  "GpsInfo": {"Latitude": 46.5, "Longitude": 11.3},
  "Tags": ["luxury", "spa"]
}

# Output (flattened):
{
  "Shortname": "Hotel ABC",
  "GpsInfo.Latitude": 46.5,
  "GpsInfo.Longitude": 11.3,
  "Tags": "luxury, spa"  # or exploded to multiple rows
}
```

**Returns**:
```json
{
  "df_key": "df_a1b2c3",
  "shape": [167, 25],
  "columns": ["Shortname", "GpsInfo.Latitude", ...],
  "sample": [<first 5 rows>],
  "memory_usage": "2.3 MB"
}
```

### 3. dataframe_query (NEW - Pandas Interface)

**Purpose**: Powerful pandas operations on cached DataFrames

**Operations**:

#### Filter
```python
dataframe_query(
    df_key="df_a1b2c3",
    operation="filter",
    query="Shortname.str.contains('Hotel') & GpsInfo_Latitude > 46.0"
)
```

#### Sort
```python
dataframe_query(
    df_key="df_a1b2c3",
    operation="sort",
    by=["Dataspace", "Shortname"],
    ascending=[True, True]
)
```

#### Group By
```python
dataframe_query(
    df_key="df_a1b2c3",
    operation="groupby",
    by="Dataspace",
    agg={"Shortname": "count", "GpsInfo_Latitude": "mean"}
)
```

#### Select Columns
```python
dataframe_query(
    df_key="df_a1b2c3",
    operation="select",
    columns=["Shortname", "Dataspace"]
)
```

#### Head/Tail
```python
dataframe_query(
    df_key="df_a1b2c3",
    operation="head",
    n=10
)
```

### 4. search_in_data (NEW - Whoosh Fulltext Search)

**Purpose**: Efficient text search in cached data

**Parameters**:
```python
cache_key: str  # Or df_key
query: str  # Search query
fields: list[str]  # Fields to search in
limit: int = 20
fuzzy: bool = False
```

**Example**:
```python
search_in_data(
    cache_key="datasets_full",
    query="hotel mountain south tyrol",
    fields=["Shortname", "ApiDescription.en"],
    limit=20,
    fuzzy=True
)
```

**Returns**:
```json
{
  "matches": 15,
  "items": [
    {
      "score": 0.95,
      "item": {"Shortname": "Mountain Hotel...", ...},
      "matched_fields": ["Shortname", "ApiDescription.en"],
      "highlights": {
        "Shortname": "<b>Mountain</b> <b>Hotel</b>...",
        "ApiDescription.en": "...in <b>South Tyrol</b>..."
      }
    }
  ]
}
```

---

## Integration Tests Structure

### Test File Structure
```
backend/tests/
├── test_tools_integration.py
├── test_tool_inspect.py
├── test_tool_aggregate.py
├── test_tool_dataframe.py
├── test_tool_search.py
└── fixtures/
    ├── sample_datasets.json
    ├── sample_accommodation.json
    └── expected_outputs/
```

### Example Test Cases

#### Test: inspect_api_structure
```python
async def test_inspect_dataset_structure():
    """LLM should be able to call inspect_api_structure correctly"""

    # Simulate LLM call
    result = await inspect_api_structure(
        api_type="dataset",
        dataset_name="Accommodation"
    )

    # Validate structure
    assert "fields" in result
    assert "field_count" in result
    assert len(result["fields"]) > 0
    assert "recommended_fields" in result

    # Validate LLM can understand output
    assert any(f["path"] == "Shortname" for f in result["fields"])
```

#### Test: aggregate_data with explicit strategy
```python
async def test_aggregate_extract_specific_fields():
    """LLM should specify fields, not rely on AUTO"""

    # Setup
    cache_key = cache.store(sample_datasets)

    # Simulate LLM call - MUST provide fields
    result = await _aggregate_data(
        cache_key=cache_key,
        strategy="extract_fields",
        fields=["Shortname", "Dataspace"]  # Explicit!
    )

    # Validate
    assert result["strategy"] == "extract_fields"
    assert len(result["items"]) == 167
    assert all("Shortname" in item for item in result["items"])
    assert all("Dataspace" in item for item in result["items"])
```

#### Test: dataframe_query filter
```python
async def test_dataframe_filter():
    """LLM should be able to filter DataFrames"""

    # Setup
    df_key = create_test_dataframe()

    # Simulate LLM call
    result = await dataframe_query(
        df_key=df_key,
        operation="filter",
        query="Type == 'Hotel' and GpsInfo_Latitude > 46.0"
    )

    # Validate
    assert "df_key" in result or "items" in result
    assert all(item["Type"] == "Hotel" for item in result["items"])
```

---

## Implementation Priority

### Phase 1 (Critical - Do First)
1. ✅ Remove AUTO mode as default from aggregate_data
2. ✅ Make inspect_api_structure mandatory in system prompt
3. ✅ Implement "no truncation" policy - always cache large results
4. ✅ Create flatten_data tool
5. ✅ Create basic integration tests

### Phase 2 (Important)
1. ✅ Implement dataframe_query with basic operations
2. ✅ Add DataFrame caching
3. ✅ Update tool descriptions with concrete examples
4. ✅ Test all tools with realistic scenarios

### Phase 3 (Enhancement)
1. ⬜ Implement search_in_data with Whoosh
2. ⬜ Add advanced pandas operations
3. ⬜ Performance optimization
4. ⬜ Comprehensive test suite

---

## System Prompt Changes

### New Mandatory Rules

```
## TOOL USAGE RULES

### Rule 1: ALWAYS Inspect Before Large Fetches
BEFORE calling get_datasets(aggregation_level="full") or get_dataset_entries:
  CALL inspect_api_structure first
  ANALYZE what fields are available
  DECIDE which fields you need for the question
  THEN fetch with fields=[specific fields]

### Rule 2: NEVER Use AUTO Mode
For aggregate_data:
  ANALYZE the user question
  DETERMINE what fields are needed
  CALL aggregate_data with strategy="extract_fields" and explicit fields=[]
  DO NOT use strategy="auto"

### Rule 3: Use Pandas for Complex Operations
For filtering, sorting, grouping:
  CALL flatten_data to convert to DataFrame
  CALL dataframe_query with specific operation
  DO NOT try to do this in aggregate_data

### Rule 4: Use Fulltext Search Tool
For text search queries ("find hotels containing 'mountain'"):
  CALL search_in_data, NOT fetch all data and search manually
```

---

## Tool Description Template

Each tool should follow this structure:

```python
tool = SmartTool(
    name="tool_name",
    description="""
    🔧 TOOL NAME - One Line Purpose

    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    📋 WHEN TO USE
    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    - Specific use case 1
    - Specific use case 2

    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    📋 PARAMETERS (Required First)
    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    1. param_name (type, REQUIRED)
       - What it does
       - Example: "value"
       - ⚠️  Common mistake: don't do X

    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    ✅ CORRECT EXAMPLES
    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    Example 1: Simple case
    tool_name(param1="value", param2="value")

    Example 2: Complex case
    tool_name(param1="value", param2=["a", "b"])

    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    ❌ COMMON ERRORS
    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    Error 1: tool_name(wrong_param="value")
    Fix: tool_name(correct_param="value")

    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    📊 RETURNS
    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    {
      "field": "value",
      "next_step": "what to do next"
    }
    """,
    func=_tool_function,
    max_tokens=None  # Never truncate!
)
```

---

## Success Metrics

1. **inspect_api_structure usage**: Should be called in >80% of large fetch scenarios
2. **AUTO mode usage**: Should drop to <10% of aggregate_data calls
3. **Field specificity**: Agent should request avg 3-5 specific fields, not all fields
4. **Search efficiency**: Fulltext searches should not fetch >100 items to LLM
5. **No truncation**: 0% of tool outputs should be truncated
6. **Test coverage**: 100% of tools should have integration tests
7. **Tool errors**: <5% of tool calls should fail due to parameter errors

---

## Migration Plan

1. Create new tools in parallel to existing ones
2. Test extensively with integration tests
3. Update system prompt gradually
4. Monitor agent behavior
5. Phase out old tools once new ones proven
6. Document all changes

---

**Last Updated**: 2025-10-21
**Version**: 2.0 (Proposed)
**Status**: Design Phase
