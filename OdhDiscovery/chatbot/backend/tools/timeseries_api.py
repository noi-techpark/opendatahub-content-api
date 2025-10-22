"""
Timeseries API Tools
Tools for querying ODH Timeseries API data
"""
import logging
from tools.base import SmartTool
from clients.timeseries_client import TimeseriesAPIClient
from preprocessing.strategies import summarize_measurements
from tools.pydantic_workaroud import _parse_json_string

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
        sensor_names=_parse_json_string(sensor_names),
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
    return await timeseries_client.get_latest_measurements(_parse_json_string(sensor_names))


# Tool definitions

get_types_tool = SmartTool(
    name="get_types",
    description="""Get list of all available timeseries types.

Returns metadata about each type including name, description, and sensor count.
Use this to discover what timeseries measurement types are available.

No parameters required.

Example:
  User: "What types of sensor data are available?"
  → get_types()
  → Returns list of types like: temperature, parking, weather, etc.""",
    func=_get_types,
    max_tokens=10000
)

get_sensors_tool = SmartTool(
    name="get_sensors",
    description="""Get sensors for a specific timeseries type.

Returns list of sensors with their metadata including location, status, etc.

Parameters:
  - type_name (required): Measurement type from get_types
    Examples: 'temperature', 'parking', 'weather', 'traffic'

Example:
  User: "Show me all parking sensors"
  → get_types() first to confirm 'parking' type exists
  → get_sensors(type_name='parking')
  → Returns list of parking sensors""",
    func=_get_sensors,
    max_tokens=20000
)

get_timeseries_tool = SmartTool(
    name="get_timeseries",
    description="""Get timeseries measurements for one or more sensors.
Automatically summarizes large datasets with statistical analysis.

Parameters:
  - sensor_names (required): List of sensor names (entry IDs from get_sensors)
  - from_date: Start date in ISO format (e.g., '2024-01-01T00:00:00Z')
  - to_date: End date in ISO format
  - interval: Aggregation interval ('1h', '1d', '1w', etc.)
  - summarize: Whether to summarize with statistics (default: true)

Workflow:
  User: "Show me parking occupancy for last week"
  → get_types() to find 'parking' type
  → get_sensors(type_name='parking') to get sensor IDs
  → get_timeseries(
      sensor_names=['sensor-1', 'sensor-2'],
      from_date='2024-01-15T00:00:00Z',
      interval='1d',
      summarize=true
    )
  → Returns statistical summary of measurements

Note: When summarize=true, returns statistics (mean, min, max, std) instead of raw measurements.""",
    func=_get_timeseries,
    max_tokens=20000
)

get_latest_measurements_tool = SmartTool(
    name="get_latest_measurements",
    description="""Get the most recent measurement for each sensor.
Very efficient - returns only the latest value per sensor.

Parameters:
  - sensor_names (required): List of sensor names (entry IDs from get_sensors)

Workflow:
  User: "What's the current parking occupancy?"
  → get_types() to find 'parking' type
  → get_sensors(type_name='parking') to get sensor IDs
  → get_latest_measurements(sensor_names=['sensor-1', 'sensor-2', 'sensor-3'])
  → Returns current values only

Note: Use this instead of get_timeseries when you only need the most recent value.""",
    func=_get_latest_measurements,
    max_tokens=10000
)
