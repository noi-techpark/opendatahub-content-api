# Chatbot Aggregation Workflow

## Overview

The ODH Chatbot now uses a sophisticated **inspect-then-aggregate** workflow to handle large API responses efficiently. This prevents token limit errors and gives the agent intelligent control over data processing.

## Architecture

### Problem Statement

Large API responses (like MetaData API returning 167 datasets with full details = ~100k tokens) exceed LLM context windows and tool output limits. The solution requires:

1. **Intelligent inspection** - Understand data structure without fetching everything
2. **Flexible aggregation** - Apply various strategies to reduce data size
3. **Agent control** - Let the LLM agent decide what data and aggregation to use
4. **Transparency** - Extensive logging to monitor agent decisions

### Solution: Three-Tool Pattern

```
┌─────────────────────────────────────────────────────────────┐
│                    USER QUERY                                │
│           "Which datasets are available?"                    │
│           "I want a detailed list"                           │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 1: INSPECT STRUCTURE (Optional but Recommended)       │
│                                                               │
│  Tool: inspect_api_structure                                 │
│  Purpose: Understand available fields without full data     │
│                                                               │
│  Example:                                                     │
│  inspect_api_structure(                                      │
│      api_type="dataset"                                      │
│  )                                                            │
│                                                               │
│  Returns: Field names, types, samples (3 items only)        │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 2: FETCH DATA                                          │
│                                                               │
│  Tool: get_datasets                                          │
│  Purpose: Fetch dataset list with chosen detail level       │
│                                                               │
│  Strategy A (Fast): aggregation_level="list" (DEFAULT)      │
│      → Returns: {name, type, dataspace} only                 │
│      → Size: ~2000 tokens                                    │
│      → Use when: User wants names, counts, simple list      │
│                                                               │
│  Strategy B (Medium): aggregation_level="summary"            │
│      → Returns: Grouped by dataspace with descriptions      │
│      → Size: ~7000 tokens                                    │
│      → Use when: User wants organized overview              │
│                                                               │
│  Strategy C (Full): aggregation_level="full"                │
│      → Returns: ALL metadata fields                          │
│      → Size: ~100,000 tokens (LARGE!)                        │
│      → Use when: Need specific fields via aggregation       │
│      → ⚠️  MUST use aggregate_data tool after this!          │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 3: AGGREGATE DATA (If needed)                          │
│                                                               │
│  Tool: aggregate_data                                        │
│  Purpose: Apply flexible aggregation to large responses     │
│                                                               │
│  Available Strategies:                                       │
│                                                               │
│  1. extract_fields - Project specific fields                │
│     aggregate_data(                                          │
│         data=datasets,                                       │
│         strategy="extract_fields",                           │
│         fields=["Shortname", "ApiDescription", "Dataspace"] │
│     )                                                         │
│     → Returns only specified fields, drops rest              │
│                                                               │
│  2. count_by - Count grouped by field                        │
│     aggregate_data(                                          │
│         data=datasets,                                       │
│         strategy="count_by",                                 │
│         group_by="Dataspace"                                 │
│     )                                                         │
│     → Returns: {tourism: 120, mobility: 30, weather: 17}    │
│                                                               │
│  3. sample - Take first N items                              │
│     aggregate_data(                                          │
│         data=datasets,                                       │
│         strategy="sample",                                   │
│         limit=10                                             │
│     )                                                         │
│     → Returns: First 10 items only                           │
│                                                               │
│  4. distinct_values - Get unique values for fields           │
│     aggregate_data(                                          │
│         data=datasets,                                       │
│         strategy="distinct_values",                          │
│         fields=["ApiType", "Dataspace"]                      │
│     )                                                         │
│     → Returns: {ApiType: ["content", "timeseries"], ...}    │
│                                                               │
│  5. group_by - Group items by field with samples             │
│  6. summarize_fields - Statistical summary (min/max/avg)     │
│  7. count_total - Just count items                           │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                    AGENT RESPONSE                            │
│              Answers user's question                         │
└─────────────────────────────────────────────────────────────┘
```

## Tool Descriptions

### 1. `inspect_api_structure`

**Purpose**: Analyze API response structure WITHOUT fetching full data

**Parameters**:
- `api_type`: "dataset", "timeseries", "sensors", "types", "measurements"
- `dataset_name`: Required for api_type="dataset"
- `sensor_name`: Required for timeseries/measurements

**Returns**:
- Field names with types
- Sample values (from 3 items only)
- Total available records

**When to use**:
- Before fetching large datasets
- When you need to know available fields
- To decide which fields to extract

**Example**:
```python
inspect_api_structure(api_type="dataset", dataset_name="Accommodation")
# Returns: {fields: [{path: "Shortname", types: ["string"], sample: "Hotel XYZ"}, ...]}
```

### 2. `get_datasets`

**Purpose**: Fetch dataset metadata from MetaData API

**Parameters**:
- `aggregation_level`: "list" (default), "summary", "full"
- `dataspace_filter`: Optional filter by dataspace

**Aggregation Levels**:

| Level | Size | Fields Included | Use When |
|-------|------|----------------|----------|
| `list` | ~2k tokens | name, type, dataspace | User wants names, simple list |
| `summary` | ~7k tokens | + descriptions, grouped | User wants organized overview |
| `full` | ~100k tokens | ALL metadata fields | Need specific fields via aggregate_data |

**Default**: `aggregation_level="list"` ✅

**When to use**:
- Always start with "list" for quick discovery
- Use "full" only when you need specific fields not in "list"
- Always follow "full" with `aggregate_data` tool

**Examples**:
```python
# Quick list (DEFAULT)
get_datasets()
get_datasets(aggregation_level="list")

# Tourism datasets only
get_datasets(aggregation_level="list", dataspace_filter="tourism")

# Full data (for aggregation)
get_datasets(aggregation_level="full")  # Then use aggregate_data!
```

### 3. `aggregate_data`

**Purpose**: Apply flexible aggregation strategies to large data

**Parameters**:
- `data`: The data to aggregate (dict or list)
- `strategy`: Aggregation strategy (see strategies below)
- `group_by`: Field for grouping (for count_by, group_by)
- `fields`: List of fields (for extract_fields, distinct_values)
- `limit`: Limit for sampling

**Strategies**:

#### 1. `extract_fields` - Field Projection
```python
aggregate_data(
    data=full_datasets,
    strategy="extract_fields",
    fields=["Shortname", "ApiDescription", "Dataspace", "ApiType"]
)
```
**Use when**: User wants specific fields from full data

#### 2. `count_by` - Group and Count
```python
aggregate_data(
    data=datasets,
    strategy="count_by",
    group_by="Dataspace"
)
# Returns: {tourism: 120, mobility: 30, weather: 17}
```
**Use when**: User wants counts by category

#### 3. `sample` - Take First N
```python
aggregate_data(
    data=datasets,
    strategy="sample",
    limit=10
)
```
**Use when**: User wants "show me some datasets"

#### 4. `distinct_values` - Unique Values
```python
aggregate_data(
    data=datasets,
    strategy="distinct_values",
    fields=["ApiType", "Dataspace"]
)
# Returns: {ApiType: ["content", "timeseries"], Dataspace: ["tourism", ...]}
```
**Use when**: User wants "what types are available"

#### 5. `group_by` - Group with Samples
```python
aggregate_data(
    data=datasets,
    strategy="group_by",
    group_by="Dataspace"
)
# Returns: {tourism: {count: 120, sample: [first 3 items]}}
```
**Use when**: User wants grouped data with examples

#### 6. `summarize_fields` - Statistics
```python
aggregate_data(
    data=measurements,
    strategy="summarize_fields",
    fields=["value", "temperature"]
)
# Returns: {value: {min: 10, max: 100, avg: 55, count: 1000}}
```
**Use when**: User wants statistics on numeric fields

#### 7. `count_total` - Simple Count
```python
aggregate_data(
    data=datasets,
    strategy="count_total"
)
# Returns: {count: 167}
```
**Use when**: User asks "how many datasets"

## Example Workflows

### Example 1: "Which datasets are available?"

**Agent Decision Path**:
```
1. Call get_datasets() (default: aggregation_level="list")
2. Receive ~2000 tokens with 167 datasets (names only)
3. Return answer: "There are 167 datasets available: Accommodation, Activity, ..."
```

**Logs**:
```
🤖 AGENT ITERATION 1
📥 USER QUERY: Which datasets are available?
🔮 Calling LLM...
🔧 AGENT DECISION: Call 1 tool(s)
   1. get_datasets({})

⚙️  EXECUTING TOOLS
▶️  Tool 1/1: get_datasets
   Args: {}
📋 Fetching datasets with aggregation_level='list', dataspace_filter=None
   Retrieved 167 datasets from MetaData API
   ✓ Returning list format (167 datasets, minimal info)
   ✅ Result: 2143 chars

🤖 AGENT ITERATION 2
💬 AGENT DECISION: Respond to user (no tool calls)
```

### Example 2: "Give me a detailed list of datasets"

**Agent Decision Path** (CORRECT):
```
1. Call get_datasets(aggregation_level="full") to get ALL fields
2. Receive LARGE response (~100k tokens) - stored in context
3. Call aggregate_data(
       data=<full_response>,
       strategy="extract_fields",
       fields=["Shortname", "ApiDescription", "Dataspace", "ApiType"]
   )
4. Receive reduced response with only needed fields
5. Return answer with descriptions
```

**Logs**:
```
🤖 AGENT ITERATION 1
📥 USER QUERY: Give me a detailed list of datasets
🔧 AGENT DECISION: Call 1 tool(s)
   1. get_datasets({'aggregation_level': 'full'})

⚙️  EXECUTING TOOLS
▶️  Tool 1/1: get_datasets
   Args: {'aggregation_level': 'full'}
📋 Fetching datasets with aggregation_level='full', dataspace_filter=None
   Retrieved 167 datasets from MetaData API
   ⚠️  Returning FULL data (167 datasets) - this is LARGE!
   ✅ Result: 102920 chars

🤖 AGENT ITERATION 2
🔧 AGENT DECISION: Call 1 tool(s)
   1. aggregate_data({'data': <full_data>, 'strategy': 'extract_fields', 'fields': [...]})

⚙️  EXECUTING TOOLS
▶️  Tool 1/1: aggregate_data
🔄 AGGREGATION START: strategy='extract_fields', fields=['Shortname', 'ApiDescription'...]
📊 Processing 167 items with strategy 'extract_fields'
✓ Extracted 4 fields from 167 items
✅ AGGREGATION COMPLETE
   ✅ Result: 15234 chars

🤖 AGENT ITERATION 3
💬 AGENT DECISION: Respond to user (no tool calls)
```

### Example 3: "How many datasets are there by type?"

**Agent Decision Path**:
```
1. Call get_datasets() (minimal list is enough)
2. Call aggregate_data(data=datasets, strategy="count_by", group_by="ApiType")
3. Return: "content: 142, timeseries: 25"
```

## Extensive Logging

The agent now logs every decision with emojis for easy parsing:

### Agent Decision Logs:
- `🤖 AGENT ITERATION N` - Start of iteration
- `📥 USER QUERY` - User's question
- `🔮 Calling LLM` - LLM is thinking
- `🔧 AGENT DECISION: Call N tool(s)` - Agent decided to use tools
- `💬 AGENT DECISION: Respond to user` - Agent has final answer

### Tool Execution Logs:
- `⚙️  EXECUTING TOOLS` - Starting tool execution
- `▶️  Tool N/M: tool_name` - Executing specific tool
- `✅ Result: X chars` - Tool completed successfully
- `❌ Tool execution failed` - Tool error

### Data Flow Logs (in tools):
- `📋 Fetching datasets` - Starting data fetch
- `🔄 AGGREGATION START` - Starting aggregation
- `📊 Processing N items` - Data processing
- `✓ Extracted/Counted/Grouped` - Operation complete
- `✅ AGGREGATION COMPLETE` - Aggregation finished

### Warning Logs:
- `⚠️  Returning FULL data - this is LARGE!` - Watch for this!

## Configuration

### Environment Variables

```bash
# Increased token limits to handle larger responses
MAX_TOKENS_PER_TOOL=8000  # Global limit

# Individual tool limits (in code):
get_datasets_tool: 10000 tokens
get_dataset_entries_tool: 6000 tokens
aggregate_data_tool: 5000 tokens
inspect_api_structure_tool: 4000 tokens
```

## Troubleshooting

### Issue: "Tool result exceeds token limit"

**Symptoms**:
```
Tool get_datasets result exceeds token limit (102920 > 10000)
```

**Diagnosis**:
- Agent called `get_datasets(aggregation_level="full")`
- Did NOT follow up with `aggregate_data` tool
- Full data exceeded limit

**Fix**:
- Agent should call `aggregate_data` immediately after getting full data
- Or use `aggregation_level="list"` or "summary" instead

### Issue: Agent not using aggregate_data

**Symptoms**:
```
🔧 AGENT DECISION: Call 1 tool(s)
   1. get_datasets({'aggregation_level': 'full'})
💬 AGENT DECISION: Respond to user (no tool calls)  ← Missing aggregate_data!
```

**Diagnosis**:
- Agent tool descriptions may not be clear enough
- LLM doesn't understand the workflow

**Fix**:
- Tool descriptions have been updated with explicit workflow boxes
- Monitor logs to see if agent behavior improves
- May need to add system prompt guidance

### Issue: Can't see what's happening

**Fix**: Check logs with extensive emoji markers:
```bash
docker-compose logs -f backend | grep "🤖\|▶️\|🔧"
```

## Benefits

1. ✅ **Token Efficiency**: Only fetch/process data needed for answer
2. ✅ **Transparency**: Detailed logs show every agent decision
3. ✅ **Flexibility**: 7 aggregation strategies for different needs
4. ✅ **Control**: Agent decides strategy based on question
5. ✅ **Scalability**: Works with ANY large API response
6. ✅ **Predictability**: Clear workflow patterns for agent to follow

## Future Improvements

1. **State Caching**: Store fetched data in AgentState for reuse within session
2. **Smart Recommendations**: Suggest aggregation based on data size
3. **Automatic Strategy**: Auto-detect best aggregation for query type
4. **Progressive Loading**: Fetch samples first, then full data if needed

---

**Last Updated**: 2025-10-19
**Version**: 1.0
**Author**: OdhDiscovery Team
