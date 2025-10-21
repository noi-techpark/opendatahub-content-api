"""
Timeseries API Tools
Tools for querying ODH Timeseries API data
"""
import logging
from tools.base import SmartTool
from clients.timeseries_client import TimeseriesAPIClient
from preprocessing.strategies import summarize_measurements

logger = logging.getLogger(__name__)

# Initialize client
timeseries_client = TimeseriesAPIClient()


# Tool implementations

async def _get_types(**kwargs) -> list[dict]:
    """Get list of all available timeseries types"""
    return await timeseries_client.get_types()


async def _get_sensors(
    type_name: str,
    **kwargs
) -> list[dict]:
    """
    Get sensors for a specific type

    Args:
        type_name: Type name (e.g., 'temperature', 'parking')
    """
    return await timeseries_client.get_sensors_by_type(type_name)


async def _get_timeseries(
    sensor_names: list[str],
    from_date: str | None = None,
    to_date: str | None = None,
    interval: str | None = None,
    summarize: bool = True,
    **kwargs
) -> dict:
    """
    Get timeseries measurements for sensors

    Args:
        sensor_names: List of sensor names (entry IDs)
        from_date: Start date (ISO format)
        to_date: End date (ISO format)
        interval: Aggregation interval (e.g., '1h', '1d')
        summarize: Whether to summarize measurements (default: True)
    """
    result = await timeseries_client.get_timeseries_bulk(
        sensor_names=sensor_names,
        from_date=from_date,
        to_date=to_date,
        interval=interval
    )

    # Apply summarization if requested
    if summarize:
        result = summarize_measurements(result)

    return result


async def _get_latest_measurements(
    sensor_names: list[str],
    **kwargs
) -> dict:
    """
    Get latest measurements for sensors

    Args:
        sensor_names: List of sensor names
    """
    return await timeseries_client.get_latest_measurements(sensor_names)


# Tool definitions

get_types_tool = SmartTool(
    name="get_timeseries_types",
    description="""Get list of all available timeseries types.
    Returns metadata about each type including name and sensor count.
    Use this to discover what timeseries data is available.

    No parameters required.""",
    func=_get_types,
    max_tokens=10000
)

get_sensors_tool = SmartTool(
    name="get_sensors_by_type",
    description="""Get sensors for a specific timeseries type.
    Returns list of sensors with their metadata.

    Parameters:
    - type_name (required): Type name (e.g., 'temperature', 'parking', 'weather')

    Example:
    - Get all temperature sensors: type_name='temperature'""",
    func=_get_sensors,
    max_tokens=20000
)

get_timeseries_tool = SmartTool(
    name="get_timeseries_measurements",
    description="""Get timeseries measurements for one or more sensors.
    Automatically summarizes large datasets with statistical analysis.

    Parameters:
    - sensor_names (required): List of sensor names (entry IDs from datasets)
    - from_date: Start date in ISO format (e.g., '2024-01-01T00:00:00Z')
    - to_date: End date in ISO format
    - interval: Aggregation interval ('1h', '1d', '1w', etc.)
    - summarize: Whether to summarize with statistics (default: true)

    Examples:
    - Get last 24h: sensor_names=['sensor-1', 'sensor-2'], interval='1h'
    - Get daily averages: sensor_names=['sensor-1'], interval='1d', from_date='2024-01-01T00:00:00Z'
    - Get raw data: sensor_names=['sensor-1'], summarize=false

    Note: When summarize=true, returns statistics (mean, min, max, std) instead of raw measurements.""",
    func=_get_timeseries,
    max_tokens=20000
)

get_latest_measurements_tool = SmartTool(
    name="get_latest_measurements",
    description="""Get the most recent measurement for each sensor.
    Very efficient - returns only the latest value per sensor.

    Parameters:
    - sensor_names (required): List of sensor names (entry IDs)

    Example:
    - Get current values: sensor_names=['sensor-1', 'sensor-2', 'sensor-3']""",
    func=_get_latest_measurements,
    max_tokens=10000
)
