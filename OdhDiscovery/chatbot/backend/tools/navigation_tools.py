"""
Navigation Tools - Split into dedicated tools per route
Each tool has a clear, focused purpose for better LLM decision-making
"""
import logging
from tools.base import SmartTool

logger = logging.getLogger(__name__)


async def _navigate_to_dataset_browser(
    dataspace: str | None = None,
    apiType: str | None = None,
    datasets: list[str] | None = None,
    search: str | None = None,
    page: int = 1,
    **kwargs
) -> dict:
    """Navigate to the Dataset Browser page"""
    params = {}
    if dataspace:
        params['dataspace'] = dataspace
    if apiType:
        params['apiType'] = apiType
    if datasets:
        params['datasets'] = datasets
    if search:
        params['search'] = search
    if page != 1:
        params['page'] = page

    logger.info(f"🧭 Navigate to DatasetBrowser with params: {params}")

    return {
        'type': 'navigate',
        'route': 'DatasetBrowser',
        'params': params
    }


async def _navigate_to_dataset_inspector(
    datasetName: str,
    view: str = 'table',
    page: int = 1,
    pagesize: int = 50,
    fields: list[str] | None = None,
    rawsort: str | None = None,
    searchfilter: str | None = None,
    language: str | None = None,
    presenceFilters: list[str] | None = None,
    distinctProperties: list[str] | None = None,
    selectedIds: list[str] | None = None,
    **kwargs
) -> dict:
    """Navigate to the Dataset Inspector page"""
    params = {'datasetName': datasetName, 'view': view}

    if page != 1:
        params['page'] = page
    if pagesize != 50:
        params['pagesize'] = pagesize
    if fields:
        params['fields'] = fields
    if rawsort:
        params['rawsort'] = rawsort
    if searchfilter:
        params['searchfilter'] = searchfilter
    if language:
        params['language'] = language
    if presenceFilters:
        params['presenceFilters'] = presenceFilters
    if distinctProperties:
        params['distinctProperties'] = distinctProperties
    if selectedIds:
        params['selectedIds'] = selectedIds

    logger.info(f"🧭 Navigate to DatasetInspector: {datasetName} with view={view}")

    return {
        'type': 'navigate',
        'route': 'DatasetInspector',
        'params': params
    }


async def _navigate_to_timeseries_browser(
    dataType: str | None = None,
    timeseries: list[str] | None = None,
    search: str | None = None,
    page: int = 1,
    **kwargs
) -> dict:
    """Navigate to the Timeseries Browser page"""
    params = {}
    if dataType:
        params['dataType'] = dataType
    if timeseries:
        params['timeseries'] = timeseries
    if search:
        params['search'] = search
    if page != 1:
        params['page'] = page

    logger.info(f"🧭 Navigate to TimeseriesBrowser with params: {params}")

    return {
        'type': 'navigate',
        'route': 'TimeseriesBrowser',
        'params': params
    }


async def _navigate_to_timeseries_inspector(
    typeName: str,
    types: list[str] | None = None,
    view: str = 'table',
    selectedSensors: list[str] | None = None,
    **kwargs
) -> dict:
    """Navigate to the Timeseries Inspector page"""
    params = {'typeName': typeName, 'view': view}

    if types:
        params['types'] = types
    if selectedSensors:
        params['selectedSensors'] = selectedSensors

    logger.info(f"🧭 Navigate to TimeseriesInspector: {typeName}")

    return {
        'type': 'navigate',
        'route': 'TimeseriesInspector',
        'params': params
    }


async def _navigate_to_bulk_measurements(
    sensors: list[str],
    types: list[str] | None = None,
    view: str = 'pretty',
    disabledSensors: list[str] | None = None,
    **kwargs
) -> dict:
    """Navigate to the Bulk Measurements Inspector page"""
    params = {'sensors': sensors, 'view': view}

    if types:
        params['types'] = types
    if disabledSensors:
        params['disabledSensors'] = disabledSensors

    logger.info(f"🧭 Navigate to BulkMeasurementsInspector with {len(sensors)} sensors")

    return {
        'type': 'navigate',
        'route': 'BulkMeasurementsInspector',
        'params': params
    }


# Tool 1: Dataset Browser
navigate_to_dataset_browser_tool = SmartTool(
    name="navigate_to_dataset_browser",
    description="""Navigate to the Dataset Browser to show high-level information about multiple datasets.

⚠️  CALL THIS TOOL - DO NOT describe it in your response!

When to use:
✅ User asks about available datasets ("List all datasets", "Show me tourism datasets")
✅ Answer involves listing multiple datasets with filters
✅ User wants to explore datasets by dataspace or API type

PARAMETER SELECTION GUIDE (CRITICAL - Follow this decision tree):
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Q: Do you already know the EXACT dataset names (from a previous tool call)?
   ├─ YES → Use `datasets` array parameter with exact names
   │         ✅ Example: datasets=['Wine by SuedtirolWein', 'Gastronomy', 'Wine Award']
   │         ❌ DON'T use search parameter if you have exact names!
   │
   └─ NO → Use other parameters:
       ├─ User asked about category/domain? → Use dataspace or apiType
       │  ✅ Example: dataspace='tourism', apiType='content'
       │
       └─ User asked with keywords/search terms? → Use search
          ✅ Example: search='hotel' (when you DON'T know exact dataset names)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Parameters:
  - datasets: Array of EXACT dataset short names (e.g., ['Accommodation', 'Gastronomy'])
    👉 Use when: You already fetched datasets and know their exact names
    ❌ DON'T use search if you have exact names!

  - search: Keyword search query (e.g., 'hotel', 'parking')
    👉 Use when: User exploring with keywords AND you don't have exact names yet
    ❌ DON'T use if you already know exact dataset names!

  - dataspace: Exact dataspace filter ('tourism', 'mobility', etc.)
    👉 Use when: User asks for specific dataspace category

  - apiType: API type filter ('content' or 'timeseries')
    👉 Use when: User asks for specific API type

  - page: Page number, default 1 (optional)

Examples:
  ✅ CORRECT: datasets=['Wine by SuedtirolWein', 'Gastronomy'] (you know exact names)
  ✅ CORRECT: dataspace='tourism' (category filter)
  ✅ CORRECT: search='hotel' (user exploring, you don't know exact names)
  ❌ WRONG: search='wine gastronomy' (when you already know exact dataset names!)""",
    func=_navigate_to_dataset_browser,
    max_tokens=2500,
    return_direct=False
)


# Tool 2: Dataset Inspector
navigate_to_dataset_inspector_tool = SmartTool(
    name="navigate_to_dataset_inspector",
    description="""Navigate to the Dataset Inspector to show detailed entries from ONE specific dataset.

⚠️  CALL THIS TOOL - DO NOT describe it in your response!

When to use:
✅ User wants to explore entries within a specific dataset
✅ Answer involves filtering/searching dataset entries
✅ User asks to analyze distinct values or view dataset statistics

Required parameter:
  - datasetName: e.g., 'Accommodation', 'Activity', 'Event', 'Poi'.

Optional parameters:
  - view: 'table' (default), 'raw', 'analysis', 'distinct', 'timeseries'
  - presenceFilters: Array of field paths to filter (e.g., ['Active', 'Type'])
  - searchfilter: Full-text search query
  - fields: Array of field names to display
  - rawsort: Sort expression (e.g., 'Name asc')
  - distinctProperties: For view='distinct', fields to analyze
  - page, pagesize, language, selectedIds

Examples:
  navigate_to_dataset_inspector(datasetName='Accommodation', presenceFilters=['Active'], searchfilter='hotel')
  navigate_to_dataset_inspector(datasetName='Event', view='distinct', distinctProperties=['Type'])
  navigate_to_dataset_inspector(datasetName='Activity')""",
    func=_navigate_to_dataset_inspector,
    max_tokens=2500,
    return_direct=False
)


# Tool 3: Timeseries Browser
navigate_to_timeseries_browser_tool = SmartTool(
    name="navigate_to_timeseries_browser",
    description="""Navigate to the Timeseries Browser to show high-level information about timeseries types.

⚠️  CALL THIS TOOL - DO NOT describe it in your response!

When to use:
✅ User asks about available timeseries types
✅ Answer involves listing multiple timeseries types
✅ User wants to explore timeseries by data type

PARAMETER SELECTION GUIDE (CRITICAL - Follow this decision tree):
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Q: Do you already know the EXACT timeseries type names (from a previous tool call)?
   ├─ YES → Use `timeseries` array parameter with exact names
   │         ✅ Example: timeseries=['temperature', 'humidity', 'parking']
   │         ❌ DON'T use search parameter if you have exact names!
   │
   └─ NO → Use other parameters:
       ├─ User asked about data type category? → Use dataType
       │  ✅ Example: dataType='numeric'
       │
       └─ User asked with keywords/search terms? → Use search
          ✅ Example: search='temperature' (when you DON'T know exact type names)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Parameters:
  - timeseries: Array of EXACT type names (e.g., ['temperature', 'parking', 'humidity'])
    👉 Use when: You already fetched types and know their exact names
    ❌ DON'T use search if you have exact names!

  - search: Keyword search query (e.g., 'weather', 'parking')
    👉 Use when: User exploring with keywords AND you don't have exact names yet
    ❌ DON'T use if you already know exact type names!

  - dataType: Data type filter ('numeric', 'string', 'boolean', 'json', 'geoposition', 'geoshape')
    👉 Use when: User asks for specific data type category

  - page: Page number, default 1 (optional)

Examples:
  ✅ CORRECT: timeseries=['temperature', 'humidity'] (you know exact names)
  ✅ CORRECT: dataType='numeric' (category filter)
  ✅ CORRECT: search='weather' (user exploring, you don't know exact names)
  ❌ WRONG: search='temperature humidity' (when you already know exact type names!)""",
    func=_navigate_to_timeseries_browser,
    max_tokens=2500,
    return_direct=False
)


# Tool 4: Timeseries Inspector
navigate_to_timeseries_inspector_tool = SmartTool(
    name="navigate_to_timeseries_inspector",
    description="""Navigate to the Timeseries Inspector to show sensors for ONE specific timeseries type.

⚠️  CALL THIS TOOL - DO NOT describe it in your response!

When to use:
✅ User wants to explore sensors for a specific timeseries type
✅ Answer involves showing sensor details for one type

Required parameter:
  - typeName: e.g., 'temperature', 'parking', 'humidity'

Optional parameters:
  - types: Array to view multiple types together (e.g., ['temperature', 'humidity'])
  - view: 'table' (default) or 'raw'
  - selectedSensors: Array of sensor names to pre-select

Examples:
  navigate_to_timeseries_inspector(typeName='temperature')
  navigate_to_timeseries_inspector(typeName='parking', view='table')
  navigate_to_timeseries_inspector(typeName='temperature', types=['temperature', 'humidity'])""",
    func=_navigate_to_timeseries_inspector,
    max_tokens=2500,
    return_direct=False
)


# Tool 5: Bulk Measurements Inspector
navigate_to_bulk_measurements_tool = SmartTool(
    name="navigate_to_bulk_measurements",
    description="""Navigate to Bulk Measurements Inspector to visualize measurements from multiple sensors.

⚠️  CALL THIS TOOL - DO NOT describe it in your response!

When to use:
✅ User wants to visualize/analyze measurements from specific sensors
✅ Answer involves showing time-series data or sensor readings

Required parameter:
  - sensors: Array of sensor names (REQUIRED!)

Optional parameters:
  - types: Array of measurement type names to pre-select
  - view: 'pretty' (default, auto-charts), 'table', or 'raw'
  - disabledSensors: Array to exclude

View modes:
  - 'pretty': Auto-detects and creates charts/maps for numeric/geographic data
  - 'table': Simple tabular view
  - 'raw': JSON viewer

Examples:
  navigate_to_bulk_measurements(sensors=['parking-p1', 'parking-p2'], view='pretty')
  navigate_to_bulk_measurements(sensors=['temp-sensor-1'], types=['temperature'])""",
    func=_navigate_to_bulk_measurements,
    max_tokens=2500,
    return_direct=False
)


# Export all tools
ALL_NAVIGATION_TOOLS = [
    navigate_to_dataset_browser_tool,
    navigate_to_dataset_inspector_tool,
    navigate_to_timeseries_browser_tool,
    navigate_to_timeseries_inspector_tool,
    navigate_to_bulk_measurements_tool
]
