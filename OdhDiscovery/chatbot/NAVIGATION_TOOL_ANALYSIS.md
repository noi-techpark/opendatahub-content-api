# Navigation Tool Analysis

**Date**: 2025-10-21
**Status**: ⚠️ Needs Updates

---

## Current Implementation

### How It Works

1. **Agent calls** `navigate_webapp(route, params)` tool
2. **Tool returns** navigation command dict: `{type: 'navigate', route: '...', params: {...}}`
3. **Graph detects** navigation commands via check: `result.get('type') == 'navigate'`
4. **State accumulates** navigation commands using `operator.add` annotation
5. **Main.py sends** navigation commands to frontend via WebSocket/HTTP response

### Integration Points

- **Tool Definition**: `backend/tools/navigation.py`
- **Agent Graph**: `backend/agent/graph.py:218-219` (detection)
- **State Management**: `backend/agent/state.py:25` (accumulation)
- **API Response**: `backend/main.py:171,272-273` (delivery to frontend)
- **Agent Prompts**: `backend/agent/prompts.py:42,145,151` (guidance)

---

## Issues Found

### 1. ❌ Tool Documentation Outdated

**Problem**: Tool description doesn't match actual frontend URL parameters

**Current Description**:
```python
params={'datasetName': 'accommodation', 'rawfilter': '...', 'view': 'table'}
```

**Actual Frontend Parameters** (from URL reference):
```javascript
// DatasetInspector
{
  page: 1,
  pagesize: 50,
  view: 'table',  // 'table', 'raw', 'analysis', 'distinct', 'timeseries'
  rawsort: 'Id desc',
  fields: ['Id', 'Name'],  // array
  language: 'en',
  searchfilter: 'bolzano',
  selectedIds: ['id1', 'id2'],  // array
  presenceFilters: ['EventDate', 'LocationInfo.Name'],  // array
  distinctProperties: ['Type', 'Features']  // array
}

// Route parameter (:datasetName) is NOT in params dict
// It's part of the route path!
```

**Impact**: Agent might generate invalid navigation commands

---

### 2. ❌ Missing Guidance on Selective Usage

**Problem**: No clear rules about WHEN to use navigation

**Current State**:
- Tool description doesn't explain it's optional
- Agent prompts only vaguely mention "navigate to show data"
- No examples of when NOT to use it

**Expected Behavior** (from user):
- Navigation should be **selective**, not always used
- Use when answer would be enhanced by frontend visualization
- Use when data shown matches a specific frontend page
- Don't use for simple text-only answers

**Examples When to Use**:
- ✅ "Show me active hotels" → Navigate to /datasets/Accommodation with filter
- ✅ "What temperature sensors are available?" → Navigate to /timeseries/temperature
- ✅ "Show parking occupancy" → Navigate to /bulk-measurements with sensor IDs
- ❌ "How many datasets are there?" → Just answer "X datasets" (no navigation)
- ❌ "What is ODH?" → Knowledge question (no navigation)

---

### 3. ❌ Route Format Inconsistency

**Problem**: Tool uses route names, but frontend expects URL paths

**Current Examples**:
```python
route='DatasetInspector'  # Component name
route='TimeseriesBrowser'  # Component name
```

**Actual Frontend Routes**:
```javascript
'/datasets'                    // DatasetBrowser
'/datasets/:datasetName'      // DatasetInspector
'/timeseries'                 // TimeseriesBrowser
'/timeseries/:typeName'       // TimeseriesInspector
'/bulk-measurements'          // BulkMeasurementsInspector
```

**Question**: Does frontend expect component names or URL paths?
- Need to verify what format the frontend actually uses

---

### 4. ⚠️ Parameter Naming Mismatch

**Problem**: Tool examples use wrong parameter names

| Tool Description | Actual Frontend | Status |
|-----------------|----------------|--------|
| `datasetName` (in params) | Route parameter `:datasetName` | ❌ Wrong |
| `typeName` (in params) | Route parameter `:typeName` | ❌ Wrong |
| `rawfilter` | Not directly settable (auto-generated from `presenceFilters`) | ⚠️ Confusing |
| `view: 'map'` | Not a valid view for DatasetInspector | ❌ Invalid |
| `view: 'chart'` | Not a valid view for TimeseriesInspector | ❌ Invalid |

**Correct Parameter Structure**:

**For /datasets/:datasetName**:
```javascript
{
  // Route params embedded in path, NOT in params dict
  // Query params:
  page: 2,
  pagesize: 100,
  view: 'table',  // 'table', 'raw', 'analysis', 'distinct', 'timeseries'
  rawsort: 'Name asc',
  fields: ['Id', 'Name', 'Type'],
  searchfilter: 'hotel',
  presenceFilters: ['Active', 'Type']  // Generates rawfilter automatically
}
```

**For /timeseries/:typeName**:
```javascript
{
  // :typeName in route path
  // Query params:
  types: ['temperature', 'humidity'],  // Can select multiple
  view: 'table',  // 'table' or 'raw' (NOT 'chart')
  selectedSensors: ['sensor1', 'sensor2']
}
```

**For /bulk-measurements**:
```javascript
{
  sensors: ['sensor1', 'sensor2'],  // REQUIRED
  types: ['temperature', 'humidity'],
  view: 'pretty',  // 'table', 'raw', 'pretty'
  disabledSensors: ['sensor3']
}
```

---

### 5. ⚠️ Agent Prompts Too Vague

**Current Prompts** (`prompts.py`):

```python
# Line 42
- navigate_to_page: Navigate to specific pages with filters

# Line 145-146
2. Use navigate_webapp to show results on map

# Line 151
3. Provide summary and navigate to show data
```

**Problems**:
- Doesn't explain WHEN to use navigation
- Doesn't explain it's OPTIONAL
- No concrete examples with proper parameters
- "show results on map" → DatasetInspector doesn't have 'map' view

**What's Needed**:
- Clear guidance on selective usage
- Examples with correct parameter formats
- Explanation of when navigation enhances the answer

---

## Recommended Fixes

### Priority 1: Update Tool Description

**File**: `backend/tools/navigation.py`

Update the tool description to:
1. Match actual frontend URL parameters exactly
2. Explain selective usage (only when it enhances the answer)
3. Provide accurate examples for each route
4. Document valid view modes for each page
5. Clarify that route parameters (`:datasetName`, `:typeName`) are in the path, not params

**Template**:
```python
description="""Navigate the webapp to enhance your response with visual data exploration.

IMPORTANT: Use this tool SELECTIVELY, only when navigation would enhance the answer.
- ✅ Use when showing data that can be visualized or filtered in the UI
- ✅ Use when suggesting the user explore specific datasets/sensors
- ❌ Don't use for simple factual answers (counts, definitions, etc.)
- ❌ Don't use when the user didn't ask for data exploration

Available Routes:
...

Route: /datasets (DatasetBrowser)
Purpose: Browse all available datasets with filters
When to use: User asks "what datasets are available?" or wants to explore by category
Parameters:
  - dataspace: Filter by dataspace ('tourism', 'mobility', 'other')
  - apiType: Filter by API type ('content', 'timeseries')
  - datasets: Array of dataset names for multiselect filter
  - page: Page number (default: 1)

Example:
  User: "Show me tourism datasets"
  → navigate_webapp(
      route='/datasets',
      params={'dataspace': 'tourism'}
    )

...
"""
```

---

### Priority 2: Update Agent Prompts

**File**: `backend/agent/prompts.py`

Add clear section on navigation:

```python
## Navigation Guidelines

Use the navigate_webapp tool SELECTIVELY to enhance responses:

When to Navigate:
✅ User asks to "show" or "explore" data that matches a frontend page
✅ Your answer includes data that would benefit from UI visualization
✅ You want to suggest the user filter/analyze data in the UI

When NOT to Navigate:
❌ Simple count or fact questions ("How many...?", "What is...?")
❌ Knowledge base questions about concepts or documentation
❌ User didn't ask for data exploration or visualization

Examples:

Q: "Show me active hotels"
A: [Call get_dataset_entries] + navigate to /datasets/Accommodation with presenceFilters

Q: "How many datasets are there?"
A: [Call count_entries] + respond with number (NO navigation)

Q: "What sensors are available for temperature?"
A: [Call get_sensors] + navigate to /timeseries/temperature

Q: "What does ODH stand for?"
A: Search documentation + respond (NO navigation - it's a knowledge question)
```

---

### Priority 3: Add Validation

**File**: `backend/tools/navigation.py`

Add parameter validation to catch mistakes:

```python
VALID_ROUTES = {
    '/datasets': {
        'params': ['dataspace', 'apiType', 'datasets', 'page'],
        'description': 'DatasetBrowser'
    },
    '/datasets/:datasetName': {
        'params': ['page', 'pagesize', 'view', 'rawsort', 'fields', 'language',
                   'searchfilter', 'selectedIds', 'presenceFilters', 'distinctProperties'],
        'valid_views': ['table', 'raw', 'analysis', 'distinct', 'timeseries']
    },
    # ...
}

def _validate_navigation(route: str, params: dict) -> None:
    """Validate navigation parameters"""
    if route not in VALID_ROUTES:
        logger.warning(f"Unknown route: {route}")
        return

    route_config = VALID_ROUTES[route]

    # Check view parameter if present
    if 'view' in params and 'valid_views' in route_config:
        if params['view'] not in route_config['valid_views']:
            logger.warning(
                f"Invalid view '{params['view']}' for route {route}. "
                f"Valid views: {route_config['valid_views']}"
            )
```

---

## Questions for User

1. **Route Format**: Does the frontend expect:
   - Component names? (`'DatasetInspector'`)
   - URL paths? (`'/datasets/:datasetName'`)
   - Some other format?

2. **Route Parameters**: For routes like `/datasets/:datasetName`, should the tool:
   - Build the full path: `'/datasets/Accommodation'`
   - Or provide route name + params: `route='DatasetInspector', params={'datasetName': 'Accommodation'}`?

3. **Default Behavior**: Should navigation be:
   - Opt-in (agent must explicitly decide to use it) ✓ Recommended
   - Automatic (always navigate when showing data)

4. **View Validation**: Should the tool:
   - Validate view parameters against allowed values?
   - Let frontend handle invalid views gracefully?

---

## Testing Needed

1. **Test navigation commands reach frontend**:
   - Verify WebSocket delivers navigation_commands array
   - Check HTTP /query endpoint includes navigation_commands in response

2. **Test parameter formats**:
   - Send navigation with arrays: `fields: ['Id', 'Name']`
   - Send navigation with route params: `/datasets/Accommodation`
   - Verify frontend correctly parses and navigates

3. **Test selective usage**:
   - Ask "How many datasets?" → Should NOT navigate
   - Ask "Show me hotels" → SHOULD navigate with filter

4. **Integration test**:
   - Add test case for navigation tool usage
   - Validate that navigation commands are properly structured

---

## Files to Update

1. ✅ `backend/tools/navigation.py` - Tool description and validation
2. ✅ `backend/agent/prompts.py` - Add navigation guidelines
3. ⏳ `backend/test_integration.py` - Add navigation test case
4. ⏳ Frontend - Verify it handles navigation commands correctly

---

## Success Criteria

- ✅ Tool description matches actual frontend URL parameters exactly
- ✅ Agent understands WHEN to use navigation (selective, not always)
- ✅ All examples use correct parameter names and formats
- ✅ Valid view modes documented for each route
- ✅ Agent prompts provide clear guidance on navigation usage
- ✅ Tool validates parameters to prevent errors
- ✅ Integration test validates navigation commands are correctly generated
