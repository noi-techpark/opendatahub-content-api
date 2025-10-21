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
        route: Route name or path (e.g., 'DatasetInspector', '/datasets/activity')
        params: Route parameters and query parameters

    Returns:
        Navigation command to send to frontend
    """
    logger.info(f"Navigation command: {route} with params: {params}")

    navigation_command = {
        'type': 'navigate',
        'route': route,
        'params': params or {}
    }

    return navigation_command


navigate_to_page_tool = SmartTool(
    name="navigate_webapp",
    description="""Navigate the webapp to a specific page with parameters.
    Use this to show the user relevant data visualizations and tables.

    Parameters:
    - route (required): Route name or path
      Routes available:
      * 'Home' or '/' - Home page
      * 'DatasetBrowser' or '/datasets' - Browse all datasets
      * 'DatasetInspector' or '/datasets/:datasetName' - Inspect specific dataset
      * 'TimeseriesBrowser' or '/timeseries' - Browse timeseries types
      * 'TimeseriesInspector' or '/timeseries/:typeName' - Inspect specific type
      * 'BulkMeasurementsInspector' or '/bulk-measurements' - Bulk measurements view

    - params: Dictionary of parameters
      For DatasetInspector:
      * datasetName: Dataset name (required)
      * page: Page number
      * pagesize: Entries per page
      * view: 'table', 'map', or 'chart'
      * rawfilter: Filter expression
      * rawsort: Sort expression
      * fields: Array of field names
      * selectedIds: Array of selected entry IDs

      For TimeseriesInspector:
      * typeName: Type name (required)
      * view: 'table' or 'chart'
      * types: Array of type names
      * selectedSensors: Array of sensor names

      For BulkMeasurementsInspector:
      * sensors: Array of sensor names
      * types: Array of type names
      * view: 'table' or 'chart'

    Examples:
    - Show active hotels: route='DatasetInspector', params={'datasetName': 'accommodation', 'rawfilter': 'Active eq true and Type eq "Hotel"', 'view': 'table'}
    - Show temperature chart: route='TimeseriesInspector', params={'typeName': 'temperature', 'view': 'chart'}
    - Show on map: route='DatasetInspector', params={'datasetName': 'gastronomy', 'view': 'map'}

    This tool returns a navigation command that will be sent to the frontend to update the UI.""",
    func=_navigate_to_page,
    max_tokens=5000,
    return_direct=False
)
