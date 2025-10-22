"""
Agent tools for ODH Chatbot
Smart tools with built-in preprocessing
"""
from .base import SmartTool
from .content_api import (
    get_datasets_tool,
    get_dataset_entries_tool,
    count_entries_tool,
    get_entry_by_id_tool,
    inspect_api_structure_tool
)
from .timeseries_api import (
    get_types_tool,
    get_sensors_tool,
    get_timeseries_tool,
    get_latest_measurements_tool
)
from .navigation_tools import (
    navigate_to_dataset_browser_tool,
    navigate_to_dataset_inspector_tool,
    navigate_to_timeseries_browser_tool,
    navigate_to_timeseries_inspector_tool,
    navigate_to_bulk_measurements_tool,
    ALL_NAVIGATION_TOOLS
)
from .knowledge import search_documentation_tool
from .aggregation import aggregate_data_tool, flatten_data_tool, dataframe_query_tool

__all__ = [
    "SmartTool",
    "get_datasets_tool",
    "get_dataset_entries_tool",
    "count_entries_tool",
    "get_entry_by_id_tool",
    "inspect_api_structure_tool",
    "get_types_tool",
    "get_sensors_tool",
    "get_timeseries_tool",
    "get_latest_measurements_tool",
    "navigate_to_dataset_browser_tool",
    "navigate_to_dataset_inspector_tool",
    "navigate_to_timeseries_browser_tool",
    "navigate_to_timeseries_inspector_tool",
    "navigate_to_bulk_measurements_tool",
    "ALL_NAVIGATION_TOOLS",
    "search_documentation_tool",
    "aggregate_data_tool",
    "flatten_data_tool",
    "dataframe_query_tool",
]
