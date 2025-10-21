"""
Content API Tools
Tools for querying ODH Content API datasets
"""
import logging
from tools.base import SmartTool
from tools.data_cache import get_cache
from clients.content_client import ContentAPIClient
from preprocessing.strategies import aggregate_datasets, aggregate_dataset_entries, field_projection

logger = logging.getLogger(__name__)
cache = get_cache()

# Initialize client
content_client = ContentAPIClient()


# Tool implementations

async def _get_datasets(
    aggregation_level: str = "list",
    dataspace_filter: str | None = None,
    **kwargs
) -> dict:
    """
    Get list of all available datasets with configurable aggregation

    Args:
        aggregation_level: Level of detail - "list" (default), "summary", or "full"
        dataspace_filter: Optional filter by dataspace (e.g., "tourism", "mobility")
    """
    logger.info(f"📋 Fetching datasets with aggregation_level='{aggregation_level}', dataspace_filter={dataspace_filter}")

    datasets = await content_client.get_datasets()
    logger.info(f"   Retrieved {len(datasets)} datasets from MetaData API")

    # Filter by dataspace if requested
    if dataspace_filter:
        datasets = [d for d in datasets if d.get("Dataspace") == dataspace_filter]
        logger.info(f"   Filtered to {len(datasets)} datasets in '{dataspace_filter}' dataspace")

    # Apply aggregation based on level
    if aggregation_level == "full":
        # Store in cache and return cache key (data is too large to return directly!)
        logger.warning(f"   ⚠️  FULL data requested ({len(datasets)} datasets) - storing in cache!")

        cache_key = cache.store(
            data={"total": len(datasets), "datasets": datasets},
            key="datasets_full"
        )

        # Return cache reference with metadata
        return {
            "total": len(datasets),
            "cache_key": cache_key,
            "message": f"Full dataset metadata ({len(datasets)} items) stored in cache.",
            "next_step": f"Use aggregate_data tool with cache_key='{cache_key}' to aggregate this data",
            "sample": datasets[:2] if datasets else []  # Show 2 samples
        }
    elif aggregation_level == "list":
        # Return just names and types - RECOMMENDED for initial discovery
        result = {
            "total": len(datasets),
            "datasets": [
                {
                    "name": d.get("Shortname"),
                    "type": d.get("ApiType"),
                    "dataspace": d.get("Dataspace")
                }
                for d in datasets
            ]
        }
        logger.info(f"   ✓ Returning list format ({len(datasets)} datasets, minimal info)")
        return result
    else:  # summary
        # Return compact summary by dataspace
        result = aggregate_datasets(datasets)
        logger.info(f"   ✓ Returning summary format (grouped by dataspace)")
        return result


async def _get_dataset_entries(
    dataset_name: str,
    page: int = 1,
    pagesize: int = 50,
    raw_filter: str | None = None,
    raw_sort: str | None = None,
    fields: list[str] | None = None,
    aggregate: bool = True,
    **kwargs
) -> dict:
    """
    Get entries from a dataset with optional aggregation

    Args:
        dataset_name: Dataset name (e.g., 'activity', 'accommodation')
        page: Page number
        pagesize: Entries per page
        raw_filter: Filter expression
        raw_sort: Sort expression
        fields: Fields to include (field projection)
        aggregate: Whether to aggregate results (default: True)
    """
    result = await content_client.get_dataset_entries(
        dataset_name=dataset_name,
        page=page,
        pagesize=pagesize,
        raw_filter=raw_filter,
        raw_sort=raw_sort,
        fields=fields
    )

    # Apply preprocessing based on parameters
    if fields and 'Items' in result:
        # Field projection already applied by API, but ensure clean output
        result = field_projection(result, fields)

    if aggregate and 'Items' in result and len(result['Items']) > 5:
        # Aggregate if we have many entries
        entries = result['Items']
        aggregated = aggregate_dataset_entries(entries)
        result['Items'] = aggregated
        result['_aggregated'] = True

    return result


async def _count_entries(
    dataset_name: str,
    raw_filter: str | None = None,
    **kwargs
) -> dict:
    """
    Count entries in a dataset

    Args:
        dataset_name: Dataset name
        raw_filter: Optional filter expression
    """
    count = await content_client.count_entries(
        dataset_name=dataset_name,
        raw_filter=raw_filter
    )
    return {
        'dataset': dataset_name,
        'count': count,
        'filter': raw_filter
    }


async def _get_entry_by_id(
    dataset_name: str,
    entry_id: str,
    fields: list[str] | None = None,
    **kwargs
) -> dict:
    """
    Get a single entry by ID

    Args:
        dataset_name: Dataset name
        entry_id: Entry ID
        fields: Fields to include
    """
    return await content_client.get_entry_by_id(
        dataset_name=dataset_name,
        entry_id=entry_id,
        fields=fields
    )


async def _inspect_api_structure(
    api_type: str,
    dataset_name: str | None = None,
    sensor_name: str | None = None,
    type_name: str | None = None,
    **kwargs
) -> dict:
    """
    Universal tool to inspect the structure of ANY API response without fetching full data.

    Fetches a minimal sample (1-3 items) and analyzes field names, types, and statistics.
    Works for datasets, timeseries, measurements, and other APIs.

    Args:
        api_type: Type of API to inspect - "dataset", "timeseries", "measurements", "types", "sensors"
        dataset_name: Required if api_type="dataset" - Shortname of dataset
        sensor_name: Required if api_type="timeseries" or "measurements" - Name of sensor
        type_name: Required if api_type="timeseries" - Type name
    """
    sample_data = None
    context = {}

    # Fetch minimal sample based on API type
    if api_type == "dataset":
        if not dataset_name:
            return {"error": "dataset_name required for api_type='dataset'"}

        result = await content_client.get_dataset_entries(
            dataset_name=dataset_name,
            page=1,
            pagesize=3
        )
        sample_data = result.get("Items", [])
        context = {
            "api_type": "dataset",
            "dataset_name": dataset_name,
            "total_entries": result.get("TotalResults", 0),
            "total_pages": result.get("TotalPages", 0)
        }

    elif api_type in ["timeseries", "measurements"]:
        # Import timeseries client if needed
        from clients.timeseries_client import TimeseriesAPIClient
        ts_client = TimeseriesAPIClient()

        if not sensor_name:
            return {"error": "sensor_name required for timeseries/measurements"}

        # Fetch sample measurements
        result = await ts_client.get_measurements(
            sensor_name=sensor_name,
            limit=3
        )
        sample_data = result if isinstance(result, list) else [result]
        context = {
            "api_type": api_type,
            "sensor_name": sensor_name,
            "type_name": type_name
        }

    elif api_type == "types":
        from clients.timeseries_client import TimeseriesAPIClient
        ts_client = TimeseriesAPIClient()
        result = await ts_client.get_types()
        sample_data = result[:3] if isinstance(result, list) else [result]
        context = {"api_type": "types"}

    elif api_type == "sensors":
        from clients.timeseries_client import TimeseriesAPIClient
        ts_client = TimeseriesAPIClient()
        result = await ts_client.get_sensors(limit=3)
        sample_data = result if isinstance(result, list) else [result]
        context = {"api_type": "sensors"}

    else:
        return {"error": f"Unknown api_type: {api_type}. Use: dataset, timeseries, measurements, types, sensors"}

    if not sample_data:
        return {
            **context,
            "fields": [],
            "sample_count": 0,
            "message": "No data found"
        }

    # Analyze structure from sample
    all_fields = set()
    field_types = {}
    field_samples = {}

    def extract_fields(obj, prefix="", depth=0):
        """Recursively extract field names, types, and sample values"""
        if depth > 3:  # Limit recursion depth
            return

        if isinstance(obj, dict):
            for key, value in obj.items():
                field_path = f"{prefix}.{key}" if prefix else key
                all_fields.add(field_path)

                # Track type
                value_type = type(value).__name__
                if field_path not in field_types:
                    field_types[field_path] = set()
                field_types[field_path].add(value_type)

                # Store sample value (truncated)
                if field_path not in field_samples and value is not None:
                    if isinstance(value, (str, int, float, bool)):
                        sample_str = str(value)[:50]
                        field_samples[field_path] = sample_str
                    elif isinstance(value, list):
                        field_samples[field_path] = f"[array of {len(value)} items]"
                    elif isinstance(value, dict):
                        field_samples[field_path] = f"{{object with {len(value)} keys}}"

                # Recurse for nested objects
                if isinstance(value, dict):
                    extract_fields(value, field_path, depth + 1)
                elif isinstance(value, list) and value and isinstance(value[0], dict):
                    # For arrays of objects, analyze first item
                    extract_fields(value[0], f"{field_path}[]", depth + 1)

        elif isinstance(obj, list) and obj:
            # For root-level arrays
            if isinstance(obj[0], dict):
                extract_fields(obj[0], prefix, depth)

    for item in sample_data:
        extract_fields(item)

    # Create field summary
    fields_summary = [
        {
            "path": field,
            "types": list(field_types.get(field, [])),
            "sample": field_samples.get(field, "")
        }
        for field in sorted(all_fields)
    ]

    return {
        **context,
        "sample_count": len(sample_data),
        "field_count": len(fields_summary),
        "fields": fields_summary[:100],  # Limit to first 100 fields
        "_note": f"Analyzed {len(sample_data)} samples. Showing first 100 of {len(fields_summary)} fields. Use these paths in 'fields' parameter to fetch only needed data."
    }


# Tool definitions

get_datasets_tool = SmartTool(
    name="get_datasets",
    description="""Get list of available datasets from the MetaData API.
    Fetches all 167+ active datasets from Open Data Hub.

    Parameters:
    - aggregation_level: Controls detail level (default: "list")
      * "list" (DEFAULT): Just names, types, dataspaces (~2000 tokens) - Use this first!
      * "summary": Compact summary grouped by dataspace (~7000 tokens)
      * "full": Complete metadata (LARGE ~100k tokens) - Only if you need ALL fields
    - dataspace_filter: Optional filter by dataspace ("tourism", "mobility", "weather", etc.)

    ⚠️  IMPORTANT WORKFLOW - Follow these steps for best results:
    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    When user asks: "Which datasets are available?" or "Show me datasets"

    Step 1: Call get_datasets(aggregation_level="list") first
            → Returns minimal info: just names, types, dataspaces
            → Fast, efficient, always works

    Step 2: If user wants MORE detail (descriptions, filters, etc.):
            Call get_datasets(aggregation_level="full") to get complete data
            → This returns a CACHE_KEY (not the data itself!)
            → Data is stored in cache to avoid token limits
            → Response includes: {cache_key: "datasets_full", total: 167, sample: [...]}

    Step 3: Then immediately call aggregate_data tool with the cache_key:
            aggregate_data(
                strategy="extract_fields",
                cache_key="datasets_full",
                fields=["Shortname", "ApiDescription", "Dataspace", "ApiType"]
            )
            → This extracts only the fields user needs from cached data
            → Returns reduced data that fits in context

    Alternative: For counts/grouping with cached data:
            aggregate_data(strategy="count_by", cache_key="datasets_full", group_by="Dataspace")
            aggregate_data(strategy="distinct_values", cache_key="datasets_full", fields=["ApiType"])
    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    Returns:
    - For "list" and "summary": {total: N, datasets: [...]}
    - For "full": {total: N, cache_key: "datasets_full", message: "...", next_step: "...", sample: [...]}

    Examples:
    - Quick list: get_datasets() or get_datasets(aggregation_level="list")
    - Tourism only: get_datasets(aggregation_level="list", dataspace_filter="tourism")
    - Full data for aggregation:
      Step 1: get_datasets(aggregation_level="full")
      Step 2: aggregate_data(strategy="extract_fields", cache_key="datasets_full", fields=[...])""",
    func=_get_datasets,
    max_tokens=10000  # Increased to handle full responses
)

get_dataset_entries_tool = SmartTool(
    name="get_dataset_entries",
    description="""Get entries from a specific dataset with filtering and pagination.

    IMPORTANT: Use inspect_dataset_schema FIRST to see available fields, then use this tool.

    Parameters:
    - dataset_name (required): Shortname of the dataset from MetaData API
      Use get_datasets to find the correct Shortname
    - page: Page number (default: 1)
    - pagesize: Entries per page, max 200 (default: 50)
    - raw_filter: OData-style filter (e.g., "Active eq true", "Type eq 'Hotel'")
    - raw_sort: Sort expression (e.g., "Shortname asc")
    - fields: List of specific fields to retrieve (RECOMMENDED - reduces data size significantly)
      Use inspect_dataset_schema to see available fields first
    - aggregate: Whether to aggregate results (default: true)

    Recommended workflow:
    1. Call inspect_dataset_schema to see available fields
    2. Choose relevant fields based on the question
    3. Call this tool with fields parameter to get only needed data
    4. Process and answer

    Examples:
    - Get hotel names: dataset_name='Accommodation', fields=['Id', 'Shortname', 'Type'], raw_filter='Type eq "Hotel"'
    - Get activities: dataset_name='ODHActivityPoi', fields=['Shortname', 'GpsInfo'], pagesize=20""",
    func=_get_dataset_entries,
    max_tokens=6000  # Increased for larger responses
)

count_entries_tool = SmartTool(
    name="count_entries",
    description="""Count entries in a dataset with optional filtering.
    Very efficient - only returns the count, not the actual entries.

    Parameters:
    - dataset_name (required): Shortname of the dataset from MetaData API
    - raw_filter: Optional filter to count only matching entries

    Examples:
    - Count all accommodations: dataset_name='Accommodation'
    - Count active hotels: dataset_name='Accommodation', raw_filter='Active eq true and Type eq "Hotel"'""",
    func=_count_entries,
    max_tokens=500  # Slightly increased
)

get_entry_by_id_tool = SmartTool(
    name="get_entry_by_id",
    description="""Get detailed information about a single entry by its ID.

    Parameters:
    - dataset_name (required): Shortname of the dataset from MetaData API
    - entry_id (required): ID of the entry
    - fields: Optional list of specific fields to retrieve (recommended)

    Example:
    - Get hotel details: dataset_name='Accommodation', entry_id='ABC123', fields=['Shortname', 'GpsInfo', 'ContactInfos']""",
    func=_get_entry_by_id,
    max_tokens=4000  # Increased for full entries
)

inspect_api_structure_tool = SmartTool(
    name="inspect_api_structure",
    description="""🔍 API SCHEMA INSPECTOR - See what fields are available WITHOUT fetching all data

Analyzes API response structure by fetching only 3 samples. Super fast and lightweight!
Works with ALL Open Data Hub APIs: datasets, timeseries, sensors, measurements, types.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📋 PARAMETERS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. api_type (required): What API to inspect
   - "dataset" → Content datasets (accommodation, activities, etc.)
   - "timeseries" → Timeseries/measurements data
   - "sensors" → Sensor listings
   - "types" → Type listings
   - "measurements" → Measurement data

2. dataset_name (required for api_type="dataset")
   - Shortname from get_datasets
   - Example: "Accommodation", "ODHActivityPoi", "Event"

3. sensor_name (required for api_type="timeseries"/"measurements")
   - Sensor identifier
   - Example: "BZ:00001"

4. type_name (optional for timeseries)
   - Type name for timeseries

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✨ SIMPLE USAGE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Inspect a dataset:
  inspect_api_structure(api_type="dataset", dataset_name="Accommodation")

Inspect sensor data:
  inspect_api_structure(api_type="timeseries", sensor_name="BZ:00001")

Inspect available sensors:
  inspect_api_structure(api_type="sensors")

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📚 COMPLETE EXAMPLES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

User: "What info is available for hotels?"
  Step 1: inspect_api_structure(api_type="dataset", dataset_name="Accommodation")
          → Returns: fields like Id, Shortname, Type, GpsInfo, ContactInfos,
                    AccoDetail, ImageGallery, Features, etc.
  Step 2: Explain available fields to user

User: "Get hotel names and locations"
  Step 1: inspect_api_structure(api_type="dataset", dataset_name="Accommodation")
          → See that Shortname and GpsInfo fields exist
  Step 2: get_dataset_entries(
            dataset_name="Accommodation",
            fields=["Shortname", "GpsInfo"]
          )
          → Fetch only those 2 fields instead of all 50+ fields!

User: "What fields are in the Activity dataset?"
  inspect_api_structure(api_type="dataset", dataset_name="ODHActivityPoi")
  → Fast schema inspection without loading all activities

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
💡 WHY USE THIS TOOL?
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Problem: Fetching full data to see what fields exist wastes tokens
Solution: This tool fetches only 3 samples and analyzes structure

Benefits:
✓ Fast - only 3 items analyzed
✓ Lightweight - returns field names, not full data
✓ Smart - shows field types and sample values
✓ Universal - works with all API types

Returns:
{
  "api_type": "dataset",
  "dataset_name": "Accommodation",
  "field_count": 47,
  "total_entries": 8532,
  "sample_count": 3,
  "fields": [
    {"path": "Id", "types": ["string"], "sample": "ABC123"},
    {"path": "Shortname", "types": ["string"], "sample": "Hotel XYZ"},
    {"path": "Type", "types": ["string"], "sample": "Hotel"},
    {"path": "GpsInfo.Latitude", "types": ["number"], "sample": "46.4983"},
    ...
  ],
  "_note": "Use these paths in 'fields' parameter to fetch only needed data"
}

⚠️  PRO TIP: Use this BEFORE fetching data to know what fields to request!""",
    func=_inspect_api_structure,
    max_tokens=5000
)
