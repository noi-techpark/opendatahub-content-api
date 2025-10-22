"""
Content API Tools
Tools for querying ODH Content API datasets
"""
import logging
from tools.base import SmartTool
from tools.data_cache import get_cache
from clients.content_client import ContentAPIClient
from preprocessing.strategies import aggregate_datasets, aggregate_dataset_entries, field_projection
from tools.pydantic_workaroud import _parse_json_string

logger = logging.getLogger(__name__)
cache = get_cache()

# Initialize client
content_client = ContentAPIClient()


# Tool implementations

async def _get_datasets(
    aggregation_level: str = "list",
    search_query: str | None = None,
    **kwargs
) -> dict:
    """
    Get list of all available datasets with configurable aggregation

    Args:
        aggregation_level: Level of detail - "list" (default), "summary", or "full"
        search_query: Optional text search across dataset names and descriptions
    """
    logger.info(f"📋 Fetching datasets with aggregation_level='{aggregation_level}', search_query={search_query}")

    datasets = await content_client.get_datasets()
    logger.info(f"   Retrieved {len(datasets)} datasets from MetaData API")

    # Filter by search query if provided
    if search_query:
        query_lower = search_query.lower()
        datasets = [
            d for d in datasets
            if query_lower in d.get("Shortname", "").lower() or
               query_lower in str(d.get("ApiDescription", {}).get("en", "")).lower()
        ]
        logger.info(f"   🔍 Filtered to {len(datasets)} datasets matching '{search_query}'")

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
    return_cache_key: bool = False,
    **kwargs
) -> dict:
    """
    Get entries from a dataset

    Args:
        dataset_name: Dataset name (e.g., 'activity', 'accommodation')
        page: Page number (default: 1)
        pagesize: Entries per page (default: 50, max: 200)
        raw_filter: OData filter expression (e.g., "Active eq true")
        raw_sort: OData sort expression (e.g., "Shortname asc")
        fields: Fields to include in response (field projection)
        return_cache_key: If True, stores large responses in cache and returns cache_key
                          Recommended for >50 items to use with inspect/flatten/pandas tools

    Returns:
        If return_cache_key=True and items > 50:
            {cache_key: "entries_xyz", total: N, next_step: "..."}
        Otherwise:
            {TotalResults: N, Items: [...], ...}

    TIP: For large result sets, use return_cache_key=True, then:
         1. inspect_api_structure(cache_key="...") to see available fields
         2. flatten_data(cache_key="...", fields=[...]) to create DataFrame
         3. dataframe_query(...) for filtering/sorting/grouping
    """
    logger.info(f"📄 Fetching entries from {dataset_name} (page={page}, pagesize={pagesize}, return_cache_key={return_cache_key})")

    result = await content_client.get_dataset_entries(
        dataset_name=dataset_name,
        page=page,
        pagesize=pagesize,
        raw_filter=raw_filter,
        raw_sort=raw_sort,
        fields=fields
    )

    # Apply field projection if requested
    if fields and 'Items' in result:
        result = field_projection(result, _parse_json_string(fields))

    total = result.get('TotalResults', len(result.get('Items', [])))
    items = result.get('Items', [])

    logger.info(f"   Retrieved {len(items)} entries (total: {total})")

    # If return_cache_key requested and we have many items, cache them
    if return_cache_key and len(items) > 50:
        logger.warning(f"   ⚠️  Large response ({len(items)} entries) - storing in cache!")

        cache_key = cache.store(
            data=result,
            key=f"entries_{dataset_name}_{page}"
        )

        return {
            "dataset": dataset_name,
            "total": total,
            "page": page,
            "pagesize": pagesize,
            "items_in_cache": len(items),
            "cache_key": cache_key,
            "next_step": f"Use inspect_api_structure(cache_key='{cache_key}') or flatten_data(cache_key='{cache_key}', fields=[...]) to process this data",
            "sample": items[:2] if items else []
        }

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
      * "list" (DEFAULT): Just names, types, dataspaces - Use this first!
      * "summary": Compact summary grouped by dataspace
      * "full": Complete metadata (LARGE) - Only if you need ALL fields for complex operations

    - search_query: (RECOMMENDED) Text search across dataset names and descriptions
      * Use this for keyword searches: "parking", "hotel", "weather", etc.
      * Returns filtered results directly - no need for pandas workflow!
      * Works with any aggregation_level

    ⚠️  IMPORTANT WORKFLOW GUIDELINES:
    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    SIMPLE SEARCHES (Use search_query parameter):
    ✅ User: "What datasets about parking are available?"
       → get_datasets(search_query="parking", aggregation_level="list")
       → Returns actual filtered results (e.g., Parking, ParkingForecast)

    ✅ User: "Show me tourism datasets"
       → get_datasets(search_query="tourism", aggregation_level="list")
       → Returns datasets with "tourism" in name/description

    ✅ User: "Find hotel datasets"
       → get_datasets(search_query="hotel", aggregation_level="list")

    COMPLEX FILTERING (Use pandas workflow):
    Only use this for complex operations like:
    - Multiple conditions: "tourism datasets with more than 1000 entries"
    - Sorting: "datasets sorted by last update"
    - Grouping/aggregation: "count datasets by dataspace and API type"

    Step 1: get_datasets(aggregation_level="full") → cache_key
    Step 2: flatten_data(cache_key="...", fields=[...]) → dataframe_cache_key
    Step 3: dataframe_query(dataframe_cache_key="...", operation="filter", query="...")
    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    Returns:
    - For "list"/"summary": {total: N, datasets: [{name, type, dataspace}, ...]}
    - For "full": {total: N, cache_key: "datasets_full", sample: [...]}

    Examples:
    ✅ SIMPLE: get_datasets(search_query="parking", aggregation_level="list")
    ✅ SIMPLE: get_datasets(search_query="weather", aggregation_level="list")
    ⚠️  COMPLEX: Only use full + pandas for advanced operations (see guidelines above)""",
    func=_get_datasets,
    max_tokens=10000
)

get_dataset_entries_tool = SmartTool(
    name="get_dataset_entries",
    description="""Get entries from a specific dataset with filtering and pagination.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📋 PARAMETERS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

- dataset_name (REQUIRED): Dataset Shortname from get_datasets
  Examples: 'ODHActivityPoi', 'Accommodation', 'Gastronomy', 'Event'

- page: Page number (default: 1)
- pagesize: Entries per page, max 200 (default: 50)

- raw_filter: OData filter expression
  Examples: "Active eq true", "Type eq 'Hotel'", "Rating gt 4"

- raw_sort: OData sort expression
  Examples: "Shortname asc", "LastChange desc"

- fields: List of specific fields to retrieve (RECOMMENDED)
  Use inspect_api_structure first to see available fields!

- return_cache_key: Boolean (default: False)
  Set to TRUE for large result sets (>50 items) to use with pandas workflow

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
💡 WORKFLOW FOR SMALL DATA (<50 items)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Use raw_filter and raw_sort parameters to filter/sort at API level:

User: "Show me 5 active hotels"
  get_dataset_entries(
    dataset_name='Accommodation',
    raw_filter="Active eq true and Type eq 'Hotel'",
    pagesize=5
  )

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🐼 WORKFLOW FOR LARGE DATA (>50 items) - Use Pandas Tools!
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

For complex filtering/sorting/grouping on large datasets:

User: "Show me all hotels sorted by rating"
  Step 1: get_dataset_entries(
            dataset_name='Accommodation',
            pagesize=200,
            return_cache_key=True
          )
          → {cache_key: "entries_xyz", total: 150}

  Step 2: inspect_api_structure(cache_key="entries_xyz")
          → See available fields

  Step 3: flatten_data(
            cache_key="entries_xyz",
            fields=["Shortname", "Type", "Rating"]
          )
          → {dataframe_cache_key: "df_xyz"}

  Step 4: dataframe_query(
            dataframe_cache_key="df_xyz",
            operation="filter",
            condition="Type == 'Hotel'"
          )
          → {cache_key: "result_1"}

  Step 5: dataframe_query(
            dataframe_cache_key="result_1",
            operation="sort",
            sort_by="Rating",
            ascending=False
          )

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⚠️  IMPORTANT NOTES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

- For filtering/sorting on large data: Use return_cache_key=True + pandas workflow
- For simple queries on small data: Use raw_filter/raw_sort directly
- ALWAYS use fields parameter to reduce data size
- Max pagesize is 200 (API limitation)""",
    func=_get_dataset_entries,
    max_tokens=60000
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
