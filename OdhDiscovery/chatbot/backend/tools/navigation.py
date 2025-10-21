"""
Navigation Tool
Controls webapp navigation via WebSocket commands
"""
import logging
from tools.base import SmartTool

logger = logging.getLogger(__name__)


async def _navigate_to_page(
    route: str,
    params: dict | None = None,
    **kwargs
) -> dict:
    """
    Generate navigation command for frontend

    Args:
        route: Route name (e.g., 'DatasetBrowser', 'DatasetInspector')
        params: Route parameters and query parameters

    Returns:
        Navigation command to send to frontend
    """
    logger.info(f"🧭 Navigation command: {route} with params: {params}")

    navigation_command = {
        'type': 'navigate',
        'route': route,
        'params': params or {}
    }

    return navigation_command


navigate_to_page_tool = SmartTool(
    name="navigate_webapp",
    description="""Navigate the webapp to enhance your response with visual data exploration.

⚠️  IMPORTANT: Use this tool SELECTIVELY, only when navigation enhances the answer.

When to Use Navigation:
✅ User asks to "show", "display", or "explore" data that matches a frontend page
✅ Your answer includes data that would benefit from UI visualization/filtering
✅ You want to suggest the user interactively explore specific datasets/sensors
✅ The data you're showing has many entries that would benefit from pagination/filtering

When NOT to Use Navigation:
❌ Simple count or fact questions ("How many...?", "What is...?")
❌ Knowledge base questions about concepts or documentation
❌ User only asked for a specific number of items (e.g., "show me 5 hotels")
❌ Your answer is purely textual without data to explore

Available Routes and Parameters:

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Route: 'DatasetBrowser' (Browse all datasets)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
When to use: User asks "what datasets are available?" or wants to explore by category

Parameters:
  - dataspace: Filter by dataspace ('tourism', 'mobility', 'other')
  - apiType: Filter by API type ('content', 'timeseries')
  - datasets: Array of dataset names for multiselect filter
  - page: Page number (default: 1, 20 items per page)

Examples:
  User: "Show me all tourism datasets"
  → navigate_webapp(route='DatasetBrowser', params={'dataspace': 'tourism'})

  User: "What content API datasets are available?"
  → navigate_webapp(route='DatasetBrowser', params={'apiType': 'content'})

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Route: 'DatasetInspector' (Inspect specific dataset entries)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
When to use: User wants to explore/filter entries within a specific dataset

Parameters:
  - datasetName: Dataset name (REQUIRED - e.g., 'Accommodation', 'Activity', 'Poi')
  - page: Page number (default: 1)
  - pagesize: Entries per page (default: 50)
  - view: View mode - 'table', 'raw', 'analysis', 'distinct', 'timeseries'
  - fields: Array of field names to display (e.g., ['Id', 'Name', 'Type'])
  - rawsort: Sort expression (e.g., 'Name asc', 'Id desc')
  - searchfilter: Full-text search query
  - language: Language code ('en', 'de', 'it')
  - presenceFilters: Array of field paths to filter for non-null values
  - distinctProperties: Array of field paths for distinct value analysis
  - selectedIds: Array of selected entry IDs

Important Notes:
  - Use 'presenceFilters' instead of building 'rawfilter' manually
  - presenceFilters=['Active', 'Type'] generates: "Active ne null and Type ne null"
  - view='distinct' with distinctProperties shows unique value counts
  - view='analysis' shows dataset statistics

Examples:
  User: "Show me active hotels in the accommodation dataset"
  → navigate_webapp(
      route='DatasetInspector',
      params={
        'datasetName': 'Accommodation',
        'presenceFilters': ['Active'],
        'searchfilter': 'hotel',
        'view': 'table'
      }
    )

  User: "Show me events with location information"
  → navigate_webapp(
      route='DatasetInspector',
      params={
        'datasetName': 'Event',
        'presenceFilters': ['EventDate', 'LocationInfo.Name'],
        'view': 'table'
      }
    )

  User: "Analyze the types in the accommodation dataset"
  → navigate_webapp(
      route='DatasetInspector',
      params={
        'datasetName': 'Accommodation',
        'view': 'distinct',
        'distinctProperties': ['Type', 'AccoTypeId']
      }
    )

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Route: 'TimeseriesBrowser' (Browse timeseries types)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
When to use: User asks "what timeseries types are available?" or wants to filter by data type

Parameters:
  - dataType: Filter by data type ('numeric', 'string', 'boolean', 'json', 'geoposition', 'geoshape')
  - timeseries: Array of timeseries type names for multiselect filter
  - page: Page number (default: 1, 20 items per page)

Examples:
  User: "Show me all numeric timeseries types"
  → navigate_webapp(route='TimeseriesBrowser', params={'dataType': 'numeric'})

  User: "What geographic timeseries are available?"
  → navigate_webapp(route='TimeseriesBrowser', params={'dataType': 'geoposition'})

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Route: 'TimeseriesInspector' (Inspect sensors for specific timeseries types)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
When to use: User wants to explore sensors for specific measurement types

Parameters:
  - typeName: Primary type name (e.g., 'temperature', 'parking')
  - types: Array of type names to inspect together (e.g., ['temperature', 'humidity'])
  - view: View mode - 'table' (sensor table) or 'raw' (JSON viewer)
  - selectedSensors: Array of selected sensor names

Important Notes:
  - If 'types' is empty, 'typeName' is used as the single type
  - Multiple types can be viewed simultaneously using the 'types' array
  - selectedSensors pre-selects sensors for bulk operations

Examples:
  User: "Show me temperature sensors"
  → navigate_webapp(
      route='TimeseriesInspector',
      params={'typeName': 'temperature', 'view': 'table'}
    )

  User: "Show me weather sensors (temperature and humidity)"
  → navigate_webapp(
      route='TimeseriesInspector',
      params={
        'typeName': 'temperature',
        'types': ['temperature', 'humidity'],
        'view': 'table'
      }
    )

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Route: 'BulkMeasurementsInspector' (Load measurements for multiple sensors)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
When to use: User wants to visualize/analyze measurements from multiple sensors

Parameters:
  - sensors: Array of sensor names (REQUIRED)
  - types: Array of measurement type names to pre-select
  - view: View mode - 'table', 'raw', or 'pretty'
  - disabledSensors: Array of sensor names to exclude from queries

View Modes:
  - 'table': Simple tabular view of measurements
  - 'raw': Raw JSON viewer
  - 'pretty': Auto-detected visualizations:
    * Numeric → Chart.js time-series line chart
    * Geographic → Leaflet map with WKT/GeoJSON
    * String/Boolean → Enhanced table
    * JSON → Expandable tree viewer

Important Notes:
  - 'sensors' parameter is REQUIRED
  - User must click "Load Latest" or "Load Historical" to fetch measurements
  - 'types' pre-selects checkboxes but doesn't trigger loading automatically

Examples:
  User: "Show me measurements for parking sensors P1 and P2"
  → navigate_webapp(
      route='BulkMeasurementsInspector',
      params={
        'sensors': ['parking-sensor-p1', 'parking-sensor-p2'],
        'view': 'pretty'
      }
    )

  User: "Visualize temperature and humidity for hotel sensors"
  → navigate_webapp(
      route='BulkMeasurementsInspector',
      params={
        'sensors': ['hotel-1-temp', 'hotel-1-humid', 'hotel-2-temp'],
        'types': ['temperature', 'humidity'],
        'view': 'pretty'
      }
    )

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Return Value:
  This tool returns a navigation command that will be sent to the frontend to update the UI.
  The navigation is optional and supplements your text response - always provide a complete
  text answer even when using navigation.""",
    func=_navigate_to_page,
    max_tokens=5000,
    return_direct=False
)
