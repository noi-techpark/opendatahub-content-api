# Navigation Tools Split & LLM Logging

**Date**: 2025-10-22
**Status**: ✅ Implemented
**Issue**: Navigation tool had 228-line description, making it hard for LLM to use correctly

---

## Problem

The single `navigate_webapp` tool had a very long description (228 lines) covering all 5 navigation routes:
- DatasetBrowser
- DatasetInspector
- TimeseriesBrowser
- TimeseriesInspector
- BulkMeasurementsInspector

This made it difficult for the LLM to:
1. Understand when to use navigation
2. Choose the correct route
3. Provide proper parameters

**Result**: The bot was frequently ignoring navigation commands or not calling the tool at all.

---

## Solution

### 1. Split Navigation Tool into 5 Dedicated Tools

**Created `/chatbot/backend/tools/navigation_tools.py`** with 5 separate tools:

```python
# Tool 1: Dataset Browser
navigate_to_dataset_browser_tool = SmartTool(
    name="navigate_to_dataset_browser",
    description="""Navigate to the Dataset Browser to show high-level information about multiple datasets.

    When to use:
    ✅ User asks about available datasets
    ✅ Answer involves listing multiple datasets

    Parameters:
      - dataspace: 'tourism', 'mobility', 'other' (optional)
      - apiType: 'content' or 'timeseries' (optional)
    """,
    func=_navigate_to_dataset_browser
)

# Tool 2: Dataset Inspector
navigate_to_dataset_inspector_tool = SmartTool(
    name="navigate_to_dataset_inspector",
    description="""Navigate to Dataset Inspector to show entries from ONE specific dataset.

    Required: datasetName
    Optional: view, presenceFilters, searchfilter, etc.
    """,
    func=_navigate_to_dataset_inspector
)

# Tool 3: Timeseries Browser
navigate_to_timeseries_browser_tool = SmartTool(
    name="navigate_to_timeseries_browser",
    description="""Navigate to Timeseries Browser to show multiple timeseries types.

    Parameters:
      - dataType: 'numeric', 'string', 'boolean', etc. (optional)
    """,
    func=_navigate_to_timeseries_browser
)

# Tool 4: Timeseries Inspector
navigate_to_timeseries_inspector_tool = SmartTool(
    name="navigate_to_timeseries_inspector",
    description="""Navigate to Timeseries Inspector for ONE specific type.

    Required: typeName
    Optional: types (array), view, selectedSensors
    """,
    func=_navigate_to_timeseries_inspector
)

# Tool 5: Bulk Measurements
navigate_to_bulk_measurements_tool = SmartTool(
    name="navigate_to_bulk_measurements",
    description="""Navigate to Bulk Measurements Inspector to visualize sensor data.

    Required: sensors (array)
    Optional: types, view ('pretty', 'table', 'raw')
    """,
    func=_navigate_to_bulk_measurements
)
```

**Each tool description is now ~20-30 lines** instead of 228 lines total!

### 2. Updated Agent to Use New Tools

**Modified `/chatbot/backend/tools/__init__.py`**:
```python
from .navigation_tools import (
    navigate_to_dataset_browser_tool,
    navigate_to_dataset_inspector_tool,
    navigate_to_timeseries_browser_tool,
    navigate_to_timeseries_inspector_tool,
    navigate_to_bulk_measurements_tool,
    ALL_NAVIGATION_TOOLS
)
```

**Modified `/chatbot/backend/agent/graph.py`**:
```python
tools = [
    search_documentation_tool,
    # ... other tools ...
    *ALL_NAVIGATION_TOOLS,  # Split navigation tools
]
```

### 3. Updated System Prompts

**Modified `/chatbot/backend/agent/prompts.py`**:

**Before**:
```
**Navigation Tool** - Enhance responses with UI navigation
   - navigate_webapp: Navigate to specific pages with filters
```

**After**:
```
**Navigation Tools** - Enhance responses with UI navigation
   - navigate_to_dataset_browser: Show multiple datasets with filters
   - navigate_to_dataset_inspector: Show entries from ONE specific dataset
   - navigate_to_timeseries_browser: Show multiple timeseries types
   - navigate_to_timeseries_inspector: Show sensors for ONE specific type
   - navigate_to_bulk_measurements: Visualize measurements from multiple sensors
```

**Updated Rule 3**:
```python
When user asks for a list of datasets:
- Use aggregation_level="list" (NOT "full") for simple dataset names
- ALWAYS call navigate_to_dataset_browser when listing datasets!
- If the length of items is short, return a markdown table AND navigate
```

**Updated Examples**:
```python
User: "List all datasets in tourism"
You:
1. CALL get_datasets(dataspace_filter='tourism', aggregation_level='list')
2. CALL navigate_to_dataset_browser(dataspace='tourism')  # <-- New tool name
3. Respond with markdown list

User: "Show me active hotels"
You:
1. CALL get_dataset_entries(dataset_name='Accommodation', ...)
2. CALL navigate_to_dataset_inspector(  # <-- New tool name
     datasetName='Accommodation',
     presenceFilters=['Active'],
     searchfilter='hotel'
   )
3. Respond with summary
```

---

## LLM Request/Response Logging

### Added Detailed Logging to File

**Modified `/chatbot/backend/agent/graph.py`**:

1. **Created dedicated logger**:
```python
# LLM Request/Response Logger (to file only)
llm_log_dir = Path(__file__).parent.parent / "logs"
llm_log_dir.mkdir(exist_ok=True)
llm_log_file = llm_log_dir / "llm_requests.log"

llm_file_handler = logging.FileHandler(llm_log_file)
llm_logger = logging.getLogger("llm_requests")
llm_logger.setLevel(logging.DEBUG)
llm_logger.addHandler(llm_file_handler)
llm_logger.propagate = False  # Don't propagate to stdout
```

2. **Added logging in call_model function**:
```python
# Before LLM call
llm_payload = messages + new_messages
llm_logger.info(f"{'='*80}")
llm_logger.info(f"LLM REQUEST (Iteration {iteration})")
llm_logger.info(f"{'='*80}")
llm_logger.info(f"Total messages: {len(llm_payload)}")
for i, msg in enumerate(llm_payload):
    msg_type = msg.__class__.__name__
    msg_content = getattr(msg, 'content', str(msg))
    # Truncate very long messages
    if len(msg_content) > 2000:
        msg_content = msg_content[:2000] + "..."
    llm_logger.info(f"Message {i+1}/{len(llm_payload)} ({msg_type}):")
    llm_logger.info(msg_content)
    llm_logger.info("-" * 80)

response = await llm_with_tools.ainvoke(llm_payload)

# After LLM call
llm_logger.info(f"{'='*80}")
llm_logger.info(f"LLM RESPONSE (Iteration {iteration})")
llm_logger.info(f"{'='*80}")
response_content = getattr(response, 'content', str(response))
llm_logger.info(f"Response content: {response_content}")
if hasattr(response, 'tool_calls') and response.tool_calls:
    llm_logger.info(f"Tool calls: {json.dumps(response.tool_calls, indent=2)}")
llm_logger.info(f"{'='*80}\n")
```

### Log File Location

**File**: `/chatbot/backend/logs/llm_requests.log`

**Format**:
```
2025-10-22 09:43:12,345 - llm_requests - INFO - ================================================================================
2025-10-22 09:43:12,345 - llm_requests - INFO - LLM REQUEST (Iteration 1)
2025-10-22 09:43:12,345 - llm_requests - INFO - ================================================================================
2025-10-22 09:43:12,345 - llm_requests - INFO - Total messages: 2
2025-10-22 09:43:12,345 - llm_requests - INFO - Message 1/2 (SystemMessage):
2025-10-22 09:43:12,345 - llm_requests - INFO - You are an intelligent assistant for the Open Data Hub...
2025-10-22 09:43:12,345 - llm_requests - INFO - --------------------------------------------------------------------------------
2025-10-22 09:43:12,345 - llm_requests - INFO - Message 2/2 (HumanMessage):
2025-10-22 09:43:12,345 - llm_requests - INFO - List all available datasets
2025-10-22 09:43:12,345 - llm_requests - INFO - --------------------------------------------------------------------------------
2025-10-22 09:43:14,678 - llm_requests - INFO - ================================================================================
2025-10-22 09:43:14,678 - llm_requests - INFO - LLM RESPONSE (Iteration 1)
2025-10-22 09:43:14,678 - llm_requests - INFO - ================================================================================
2025-10-22 09:43:14,678 - llm_requests - INFO - Response content:
2025-10-22 09:43:14,678 - llm_requests - INFO - Tool calls: [
  {
    "name": "get_datasets",
    "args": {"aggregation_level": "list"},
    "id": "call_abc123"
  },
  {
    "name": "navigate_to_dataset_browser",
    "args": {},
    "id": "call_def456"
  }
]
2025-10-22 09:43:14,678 - llm_requests - INFO - ================================================================================
```

---

## Benefits

### 1. Clearer Tool Descriptions
- Each tool: **20-30 lines** vs **228 lines** total
- LLM can quickly understand which tool to use
- Parameters are specific to each tool

### 2. Better Tool Selection
- Tool names are descriptive: `navigate_to_dataset_browser` vs generic `navigate_webapp`
- When to use guidelines are specific to each tool
- Less ambiguity about route selection

### 3. Debugging Capability
- Full LLM request/response logging to file
- See exactly what prompt the LLM receives
- See exactly what tool calls it decides to make
- Troubleshoot decision-making issues

### 4. No Stdout Pollution
- LLM logs go only to file (`logs/llm_requests.log`)
- Regular logs still go to stdout
- Easy to review LLM interactions separately

---

## Testing

To test the changes:

1. **Ask the bot**: "List all available datasets"
   - ✅ Should call `get_datasets(aggregation_level='list')`
   - ✅ Should call `navigate_to_dataset_browser()`
   - ✅ Should provide markdown list of datasets

2. **Check LLM logs**:
   ```bash
   tail -f /home/mroggia/git/opendatahub-content-api/OdhDiscovery/chatbot/backend/logs/llm_requests.log
   ```
   - See full system prompt
   - See user question
   - See LLM response with tool calls

3. **Ask follow-up**: "which ones?"
   - ✅ Should not repeat count
   - ✅ Should call navigation tool again
   - ✅ Should actually list dataset names

---

## Files Modified

### Created:
1. `/chatbot/backend/tools/navigation_tools.py` - 5 separate navigation tools
2. `/chatbot/backend/logs/llm_requests.log` - LLM logging (auto-created)

### Modified:
3. `/chatbot/backend/tools/__init__.py` - Export new navigation tools
4. `/chatbot/backend/agent/graph.py` - Use new tools + add LLM logging
5. `/chatbot/backend/agent/prompts.py` - Update system prompts and examples

---

## Summary

✅ **Navigation tool split into 5 focused tools** - easier for LLM to understand
✅ **Shorter tool descriptions** - 20-30 lines each vs 228 lines total
✅ **Clearer tool names** - `navigate_to_dataset_browser` vs `navigate_webapp(route='DatasetBrowser')`
✅ **LLM request/response logging** - full payload logged to file for debugging
✅ **Updated system prompts** - explicit instructions to use navigation tools
✅ **No stdout pollution** - LLM logs only go to file

**Expected Result**: The bot should now reliably call navigation tools when listing datasets, timeseries types, or showing data that benefits from UI visualization.

🎉 **Navigation Tools Split: COMPLETE**
