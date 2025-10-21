# Testing Guide - New Pandas-Based Tools

## Quick Start

### 1. Start the Backend

```bash
cd /home/mroggia/git/opendatahub-content-api/OdhDiscovery/chatbot/backend
python -m uvicorn main:app --reload --host 0.0.0.0 --port 8001
```

Or with Docker:
```bash
cd /home/mroggia/git/opendatahub-content-api/OdhDiscovery/chatbot
docker-compose up
```

### 2. Test WebSocket Connection

Use the existing test client:
```bash
cd /home/mroggia/git/opendatahub-content-api/OdhDiscovery/chatbot
python test_websocket.py
```

---

## Test Cases

### Test 1: Simple Field Extraction (aggregate_data - no AUTO mode)

**Query**: "List all dataset names"

**Expected Workflow**:
```
1. get_datasets(aggregation_level="full")
   → Returns: {cache_key: "datasets_full", total: 167}

2. aggregate_data(
     cache_key="datasets_full",
     strategy="extract_fields",
     fields=["Shortname"]
   )
   → Returns: {items: [...], extracted_fields: ["Shortname"]}
```

**What to Check**:
- ✅ Agent calls aggregate_data with explicit strategy
- ✅ Agent specifies fields=["Shortname"] (based on user question)
- ❌ Agent does NOT use strategy without fields
- ❌ Agent does NOT omit strategy parameter

---

### Test 2: Filtering (pandas workflow)

**Query**: "Show me all active hotels"

**Expected Workflow**:
```
1. get_datasets(aggregation_level="full")
   → Returns: {cache_key: "datasets_full"}

2. inspect_api_structure(cache_key="datasets_full")
   → Returns: Available fields including "Active", "Type", "Shortname"

3. flatten_data(
     cache_key="datasets_full",
     fields=["Shortname", "Active", "Type"]
   )
   → Returns: {dataframe_cache_key: "df_xyz", row_count: 167}

4. dataframe_query(
     dataframe_cache_key="df_xyz",
     operation="filter",
     condition="(Active == True) & (Type == 'Hotel')"
   )
   → Returns: {result_count: 42, data: [...]}
```

**What to Check**:
- ✅ Agent uses inspect_api_structure first
- ✅ Agent uses flatten_data before filtering
- ✅ Agent uses correct pandas query syntax
- ✅ Agent extracts only needed fields

---

### Test 3: Sorting (pandas workflow)

**Query**: "List datasets sorted by name"

**Expected Workflow**:
```
1. get_datasets(aggregation_level="full")
2. inspect_api_structure(cache_key="datasets_full")
3. flatten_data(
     cache_key="datasets_full",
     fields=["Shortname", "ApiDescription"]
   )
4. dataframe_query(
     dataframe_cache_key="df_xyz",
     operation="sort",
     sort_by="Shortname",
     ascending=True
   )
```

**What to Check**:
- ✅ Agent uses pandas workflow (not aggregate_data)
- ✅ Agent specifies sort_by parameter correctly

---

### Test 4: Grouping/Counting (pandas workflow)

**Query**: "How many datasets per dataspace?"

**Expected Workflow**:
```
1. get_datasets(aggregation_level="full")
2. inspect_api_structure(cache_key="datasets_full")
3. flatten_data(
     cache_key="datasets_full",
     fields=["Dataspace"]
   )
4. dataframe_query(
     dataframe_cache_key="df_xyz",
     operation="groupby",
     group_by="Dataspace",
     agg_func="count"
   )
```

**What to Check**:
- ✅ Agent recognizes groupby need
- ✅ Agent uses pandas workflow
- ✅ Agent extracts only Dataspace field

---

### Test 5: Complex Query (chained operations)

**Query**: "Show me top 5 active hotels sorted by name"

**Expected Workflow**:
```
1. get_datasets(aggregation_level="full")
2. inspect_api_structure(cache_key="datasets_full")
3. flatten_data(
     cache_key="datasets_full",
     fields=["Shortname", "Active", "Type"]
   )
4. dataframe_query(
     dataframe_cache_key="df_xyz",
     operation="filter",
     condition="(Active == True) & (Type == 'Hotel')"
   )
   → Returns: {cache_key: "result_1"}

5. dataframe_query(
     dataframe_cache_key="result_1",
     operation="sort",
     sort_by="Shortname",
     ascending=True
   )
   → Returns: {cache_key: "result_2"}

6. dataframe_query(
     dataframe_cache_key="result_2",
     operation="head",
     limit=5
   )
```

**What to Check**:
- ✅ Agent chains operations using cache_keys
- ✅ Agent completes all steps before responding
- ✅ Final result has exactly 5 items

---

### Test 6: Nested Field Extraction (flatten_data)

**Query**: "Show me all dataset locations"

**Expected Workflow**:
```
1. get_datasets(aggregation_level="full")
2. inspect_api_structure(cache_key="datasets_full")
   → Agent sees Location.Position.Latitude/Longitude available

3. flatten_data(
     cache_key="datasets_full",
     fields=[
       "Shortname",
       "Location.Position.Latitude",
       "Location.Position.Longitude"
     ]
   )
```

**What to Check**:
- ✅ Agent uses dot notation for nested fields
- ✅ Flattened data contains extracted nested values

---

### Test 7: Error Handling (missing parameters)

**Query**: "Filter the datasets"

**Expected Behavior**:
```
1. Agent asks user for clarification: "Filter by what criteria?"
   OR
2. Agent calls dataframe_query without condition
   → Returns: {"error": "filter operation requires 'condition' parameter"}
3. Agent explains error to user and asks for filter criteria
```

**What to Check**:
- ✅ Agent handles errors gracefully
- ✅ Agent provides helpful feedback to user

---

## Debugging

### Enable Detailed Logging

The backend already has extensive logging with emoji markers:

- 🔧 aggregate_data operations
- 🔨 flatten_data operations
- 🐼 dataframe_query operations
- 🤖 Agent decisions
- ⚙️ Tool executions

Look for these in the backend logs to trace agent behavior.

### Common Issues

**Issue**: Agent doesn't call inspect_api_structure

**Check**:
- System prompt Rule 2 (line 57-73 in prompts.py)
- System prompt Behavior #2 (line 90-93 in prompts.py)

**Fix**: If agent still doesn't inspect, add to Rule 2:
```
IF cache_key received AND data > 100 items:
    MUST call inspect_api_structure first
```

---

**Issue**: Agent uses aggregate_data for filtering

**Check**:
- System prompt Rule 4 (line 89-107 in prompts.py)
- Tool descriptions emphasize pandas for filter/sort

**Fix**: Make Rule 4 more explicit:
```
NEVER use aggregate_data for:
- Filtering (condition="...")
- Sorting
- Grouping with aggregation

ALWAYS use flatten_data + dataframe_query instead!
```

---

**Issue**: Agent doesn't chain operations

**Check**:
- Tool results include "next_step" guidance
- Agent follows multi-step workflows (Rule 1)

**Fix**: Ensure dataframe_query returns cache_key prominently

---

## Validation Checklist

After running tests, verify:

- [ ] Agent NEVER uses AUTO mode (should error or infer from params)
- [ ] Agent calls inspect_api_structure for large data (>100 items)
- [ ] Agent uses flatten_data before filter/sort/groupby
- [ ] Agent specifies explicit fields in all tools
- [ ] Agent chains pandas operations correctly
- [ ] Agent completes workflows before responding
- [ ] Error messages are helpful and actionable
- [ ] Cache keys work across tools
- [ ] Nested field extraction works (dot notation)
- [ ] Pandas query syntax is correct

---

## Performance Testing

### Test with Large Dataset

```python
# Simulate 1000 items
large_dataset = [{"Id": i, "Name": f"Item {i}", "Active": i % 2 == 0} for i in range(1000)]

# Store in cache
cache_key = cache.store(large_dataset, key="large_test")

# Test flatten performance
flatten_data(cache_key="large_test", fields=["Id", "Name", "Active"])

# Test query performance
dataframe_query(
    dataframe_cache_key="df_large_test",
    operation="filter",
    condition="Active == True"
)
```

**Expected**:
- Flatten should complete in < 1 second
- Filter should complete in < 0.5 seconds
- Memory usage should be reasonable (<100MB for 1000 items)

---

## Next Steps After Testing

1. **Document Issues**: Record any problems in GitHub issues
2. **Adjust Prompts**: If agent behavior is wrong, update system prompt
3. **Add Integration Tests**: Create automated tests for each workflow
4. **Implement Search Tool**: Add Whoosh-based fulltext search (user requirement)
5. **Review Truncation**: Ensure tools cache instead of truncate

---

## Expected Output Format

The agent should provide responses like:

**Good Response** (after successful workflow):
```
I found 42 active hotels in the ODH database. Here are the results sorted by name:

1. Hotel Adler - Bolzano
2. Hotel Aurora - Merano
3. Hotel Bellevue - Cortina
...
42. Hotel Zum Löwen - Brunico

All of these are currently active and available in the tourism dataset.
```

**Bad Response** (incomplete workflow):
```
I found some datasets. Here are 2 examples:
1. Dataset A
2. Dataset B

[Should have returned ALL 167, not just 2 samples!]
```

---

## Success Criteria

✅ All test queries complete successfully
✅ Agent uses pandas workflow for complex operations
✅ Agent never uses AUTO mode
✅ Agent inspects structure before large operations
✅ Error handling is clear and helpful
✅ Performance is acceptable (<2s for typical queries)
✅ No truncation warnings in logs

**If all criteria met → Ready for production use!**
