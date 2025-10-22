"""
Data Aggregation Tools
Flexible aggregation strategies for large API responses
"""
import logging
from typing import Any
from tools.base import SmartTool
from tools.data_cache import get_cache
from collections import Counter
import json
import pandas as pd
from tools.pydantic_workaroud import _parse_json_string

logger = logging.getLogger(__name__)
cache = get_cache()


async def _aggregate_data(
    cache_key: str | None = None,
    data: dict | list | None = None,
    strategy: str | None = None,  # NO DEFAULT - force explicit choice
    group_by: str | None = None,
    fields: list[str] | None = None,
    limit: int = 20,
    **kwargs
) -> dict:
    """
    🔧 Swiss Army Knife for Data Aggregation

    Apply various aggregation strategies to large data responses.
    Smart defaults make it easy to use - just provide cache_key or data!

    Args:
        cache_key: Cache key from get_datasets(aggregation_level="full") - RECOMMENDED
        data: Direct data to aggregate (if cache_key not used)
        strategy: How to aggregate (default: "auto" - smart detection)
        group_by: Field name to group by (for count_by, group_by strategies)
        fields: List of field names to extract/analyze
        limit: Number of items to sample (default: 20)

    Available Strategies:
        - "auto": Smart detection - extracts common fields (Shortname, ApiDescription, etc.)
        - "extract_fields": Project specific fields only
        - "count_by": Count grouped by field
        - "sample": Take first N items
        - "group_by": Group with samples
        - "distinct_values": Get unique values
        - "summarize_fields": Statistics (min/max/avg)
        - "count_total": Just count
    """
    if fields:
        fields = _parse_json_string(fields)
    logger.info(f"🔄 AGGREGATION: cache_key={cache_key}, strategy='{strategy}', group_by={group_by}, fields={fields}, limit={limit}")

    # Get data from cache if cache_key provided
    if cache_key:
        logger.info(f"   📦 Loading data from cache: {cache_key}")
        data = cache.get(cache_key)
        if data is None:
            return {
                "error": f"Cache key '{cache_key}' not found or expired",
                "available_keys": "Check get_datasets output for valid cache_key"
            }
        logger.info(f"   ✓ Retrieved data from cache")

    if data is None:
        return {
            "error": "Either 'data' or 'cache_key' parameter is required",
            "usage": "Provide data directly OR cache_key from get_datasets(aggregation_level='full')"
        }

    # Normalize data to list of items
    items = []
    metadata = {}
    data_type_hint = None  # Track what kind of data this is

    if isinstance(data, dict):
        if 'Items' in data:
            items = data.get('Items', [])
            metadata = {k: v for k, v in data.items() if k != 'Items'}
            data_type_hint = "api_response"
        elif 'datasets' in data:
            items = data.get('datasets', [])
            metadata = {k: v for k, v in data.items() if k != 'datasets'}
            data_type_hint = "datasets"
        else:
            # Treat as single item
            items = [data]
    elif isinstance(data, list):
        items = data
    else:
        logger.warning(f"Unexpected data type: {type(data)}")
        return {"error": "Data must be dict or list", "type": str(type(data))}

    logger.info(f"📊 Processing {len(items)} items (type: {data_type_hint})")

    # Strategy detection - now requires explicit thinking
    if strategy is None:
        logger.warning("⚠️  No strategy provided - inferring from parameters")

        # Infer from parameters
        if fields and len(fields) > 0:
            strategy = "extract_fields"
            logger.info(f"   → Inferred: extract_fields (fields provided: {fields})")
        elif group_by:
            strategy = "count_by"
            logger.info(f"   → Inferred: count_by (group_by={group_by})")
        else:
            # ERROR: Must provide strategy or parameters
            return {
                "error": "Missing 'strategy' parameter",
                "help": "You must provide either:",
                "options": [
                    "strategy='extract_fields' with fields=[...]",
                    "strategy='count_by' with group_by='field'",
                    "strategy='sample' with limit=N",
                    "strategy='group_by' with group_by='field'",
                    "strategy='distinct_values' with fields=[...]"
                ],
                "example": "aggregate_data(cache_key='...', strategy='extract_fields', fields=['Shortname', 'Dataspace'])"
            }

    logger.info(f"   ✓ Using strategy: '{strategy}'")

    result = {
        "strategy": strategy,
        "original_count": len(items),
        **metadata
    }

    # Apply strategy
    if strategy == "count_total":
        logger.info(f"✓ Count: {len(items)} items")
        result["count"] = len(items)

    elif strategy == "sample":
        sampled = items[:limit]
        logger.info(f"✓ Sampled {len(sampled)} of {len(items)} items")
        result["items"] = sampled
        result["sampled_count"] = len(sampled)

    elif strategy == "extract_fields":
        if not fields:
            logger.error("❌ extract_fields requires 'fields' parameter")
            return {"error": "extract_fields strategy requires 'fields' parameter"}

        extracted = []
        for item in items:
            extracted_item = {}
            for field in fields:
                if '.' in field:
                    # Nested field
                    parts = field.split('.')
                    value = item
                    for part in parts:
                        if isinstance(value, dict):
                            value = value.get(part)
                        else:
                            value = None
                            break
                    extracted_item[field] = value
                else:
                    extracted_item[field] = item.get(field)
            extracted.append(extracted_item)

        logger.info(f"✓ Extracted {len(fields)} fields from {len(items)} items")
        result["items"] = extracted
        result["extracted_fields"] = fields

    elif strategy == "count_by":
        if not group_by:
            logger.error("❌ count_by requires 'group_by' parameter")
            return {"error": "count_by strategy requires 'group_by' parameter"}

        values = []
        for item in items:
            if '.' in group_by:
                # Nested field
                parts = group_by.split('.')
                value = item
                for part in parts:
                    if isinstance(value, dict):
                        value = value.get(part)
                    else:
                        value = None
                        break
                values.append(value)
            else:
                values.append(item.get(group_by))

        counts = Counter(values)
        logger.info(f"✓ Counted by '{group_by}': {len(counts)} unique values")
        result["counts"] = dict(counts)
        result["grouped_by"] = group_by
        result["unique_values"] = len(counts)

    elif strategy == "distinct_values":
        if not fields:
            logger.error("❌ distinct_values requires 'fields' parameter")
            return {"error": "distinct_values strategy requires 'fields' parameter"}

        distinct = {}
        for field in fields:
            values = set()
            for item in items:
                if '.' in field:
                    parts = field.split('.')
                    value = item
                    for part in parts:
                        if isinstance(value, dict):
                            value = value.get(part)
                        else:
                            value = None
                            break
                else:
                    value = item.get(field)

                if value is not None:
                    if isinstance(value, list):
                        values.update(str(v) for v in value)
                    else:
                        values.add(str(value))

            distinct[field] = sorted(list(values))[:50]  # Limit to 50 per field

        logger.info(f"✓ Found distinct values for {len(fields)} fields")
        result["distinct_values"] = distinct
        result["fields"] = fields

    elif strategy == "summarize_fields":
        if not fields:
            # Auto-detect numeric fields
            fields = []
            if items:
                first_item = items[0]
                for key, value in first_item.items():
                    if isinstance(value, (int, float)):
                        fields.append(key)

        summary = {}
        for field in fields:
            values = []
            for item in items:
                value = item.get(field)
                if isinstance(value, (int, float)):
                    values.append(value)

            if values:
                summary[field] = {
                    "count": len(values),
                    "min": min(values),
                    "max": max(values),
                    "avg": sum(values) / len(values),
                    "sum": sum(values)
                }

        logger.info(f"✓ Summarized {len(summary)} numeric fields")
        result["summary"] = summary

    elif strategy == "group_by":
        if not group_by:
            logger.error("❌ group_by requires 'group_by' parameter")
            return {"error": "group_by strategy requires 'group_by' parameter"}

        grouped = {}
        for item in items:
            key = item.get(group_by, "unknown")
            if key not in grouped:
                grouped[key] = []
            grouped[key].append(item)

        # Count and optionally sample from each group
        grouped_summary = {}
        for key, group_items in grouped.items():
            grouped_summary[key] = {
                "count": len(group_items),
                "sample": group_items[:3]  # First 3 items
            }

        logger.info(f"✓ Grouped by '{group_by}': {len(grouped)} groups")
        result["groups"] = grouped_summary
        result["grouped_by"] = group_by
        result["group_count"] = len(grouped)

    else:
        logger.error(f"❌ Unknown strategy: {strategy}")
        return {
            "error": f"Unknown strategy: {strategy}",
            "available_strategies": [
                "count_total", "sample", "extract_fields", "count_by",
                "distinct_values", "summarize_fields", "group_by"
            ]
        }

    logger.info(f"✅ AGGREGATION COMPLETE: strategy='{strategy}' applied successfully")
    return result


# Tool definition
aggregate_data_tool = SmartTool(
    name="aggregate_data",
    description="""🔧 SWISS ARMY KNIFE for Data Aggregation

Apply various strategies to reduce large data responses.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📋 REQUIRED PARAMETERS - You MUST Think About What You Need!
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. cache_key (string, REQUIRED for cached data)
   - Cache key from get_datasets(aggregation_level="full") or other tools
   - Example: "datasets_full"

2. strategy (string, REQUIRED - must explicitly choose)
   AVAILABLE STRATEGIES:
   - "extract_fields": Get specific fields only (requires 'fields' parameter)
   - "count_by": Count grouped by field (requires 'group_by' parameter)
   - "sample": Take first N items (uses 'limit' parameter)
   - "group_by": Group with counts and samples (requires 'group_by' parameter)
   - "distinct_values": Get unique values per field (requires 'fields' parameter)
   - "summarize_fields": Statistics on numeric fields (optional 'fields' parameter)
   - "count_total": Just count items (no additional parameters)

3. fields (list of strings, REQUIRED for extract_fields/distinct_values)
   - Field names to extract or analyze
   - Example: ["Shortname", "ApiDescription", "Dataspace"]
   - ⚠️  You must specify which fields you need based on user's question!

4. group_by (string, REQUIRED for count_by/group_by strategies)
   - Field name to group/count by
   - Example: "Dataspace" or "ApiType"

5. limit (number, optional, default=20)
   - Number of items for sampling

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📚 COMPLETE EXAMPLES - Always Specify Strategy and Required Fields!
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

User: "Which datasets are available? I want a detailed list"
  Step 1: get_datasets(aggregation_level="full")
          → {cache_key: "datasets_full", total: 167}
  Step 2: aggregate_data(
            cache_key="datasets_full",
            strategy="extract_fields",
            fields=["Shortname", "ApiDescription", "Dataspace"]
          )
          → Returns all 167 datasets with only those 3 fields
  Step 3: Respond with complete list

User: "How many datasets per dataspace?"
  Step 1: get_datasets(aggregation_level="full")
  Step 2: aggregate_data(
            cache_key="datasets_full",
            strategy="count_by",
            group_by="Dataspace"
          )
          → {tourism: 120, mobility: 30, weather: 17}

User: "Show me 5 example datasets"
  Step 1: get_datasets(aggregation_level="full")
  Step 2: aggregate_data(
            cache_key="datasets_full",
            strategy="sample",
            limit=5
          )
          → {items: [<5 complete datasets>]}

User: "What types of APIs are available?"
  Step 1: get_datasets(aggregation_level="full")
  Step 2: aggregate_data(
            cache_key="datasets_full",
            strategy="count_by",
            group_by="ApiType"
          )
          → {content: 142, timeseries: 25}

User: "List all dataset names and their dataspaces"
  Step 1: get_datasets(aggregation_level="full")
  Step 2: aggregate_data(
            cache_key="datasets_full",
            strategy="extract_fields",
            fields=["Shortname", "Dataspace"]
          )
          → Returns ALL 167 items with just those 2 fields

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⚠️  IMPORTANT RULES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. ALWAYS provide 'strategy' parameter - think about what the user needs!
2. ALWAYS provide required parameters for your chosen strategy:
   - extract_fields → needs 'fields'
   - count_by → needs 'group_by'
   - group_by → needs 'group_by'
   - distinct_values → needs 'fields'
3. Choose fields based on user's question - don't guess or use defaults!
4. If you're unsure what fields are available, use inspect_api_structure first!

Returns:
{
  "strategy": "extract_fields",
  "original_count": 167,
  "items": [...],  // or "counts", "summary", etc depending on strategy
  "extracted_fields": [...]  // for extract_fields
}

⚠️  TIP: When get_datasets returns cache_key, use it here to get aggregated data!""",
    func=_aggregate_data,
    max_tokens=60000
)


async def _flatten_data(
    cache_key: str | None = None,
    data: dict | list | None = None,
    fields: list[str] | None = None,
    explode_arrays: bool = False,
    **kwargs
) -> dict:
    """
    🔨 Flatten nested JSON data into tabular (CSV-like) format

    Transforms complex nested JSON into flat structure suitable for pandas DataFrame.
    Supports nested field extraction with dot notation (e.g., "Location.Position.Latitude").

    Args:
        cache_key: Cache key from previous tool (recommended)
        data: Direct data to flatten (if cache_key not used)
        fields: List of field paths to extract (supports dot notation for nested fields)
                If None, attempts to auto-detect top-level fields
        explode_arrays: If True, create separate rows for array elements (like pandas explode)

    Returns:
        {
            "cache_key": "flattened_xyz",  # Cache key for flattened data
            "dataframe_cache_key": "df_xyz",  # Cache key for pandas DataFrame
            "row_count": 167,
            "column_count": 5,
            "columns": ["Shortname", "Dataspace", "Location.Latitude", ...],
            "sample": [<first 3 rows>],
            "next_step": "Use dataframe_query tool with dataframe_cache_key to filter/sort/group"
        }
    """
    if fields:
        fields = _parse_json_string(fields)

    logger.info(f"🔨 FLATTEN: cache_key={cache_key}, fields={fields}, explode_arrays={explode_arrays}")

    # Get data from cache if cache_key provided
    if cache_key:
        logger.info(f"   📦 Loading data from cache: {cache_key}")
        data = cache.get(cache_key)
        if data is None:
            return {
                "error": f"Cache key '{cache_key}' not found or expired",
                "available_keys": "Check previous tool output for valid cache_key"
            }
        logger.info(f"   ✓ Retrieved data from cache")

    if data is None:
        return {
            "error": "Either 'data' or 'cache_key' parameter is required",
            "usage": "Provide data directly OR cache_key from previous tool"
        }

    # Normalize data to list of items
    items = []
    if isinstance(data, dict):
        if 'Items' in data:
            items = data.get('Items', [])
        elif 'datasets' in data:
            items = data.get('datasets', [])
        elif 'items' in data:
            items = data.get('items', [])
        else:
            # Treat as single item
            items = [data]
    elif isinstance(data, list):
        items = data
    else:
        return {"error": "Data must be dict or list", "type": str(type(data))}

    if not items:
        return {"error": "No items to flatten", "count": 0}

    logger.info(f"📊 Flattening {len(items)} items")

    # Auto-detect fields if not provided
    if not fields:
        logger.info("   ℹ️  No fields specified - auto-detecting from first item")
        if items:
            first_item = items[0]
            fields = list(first_item.keys()) if isinstance(first_item, dict) else []
            logger.info(f"   → Auto-detected {len(fields)} top-level fields")

    if not fields:
        return {"error": "No fields to extract - provide fields parameter or ensure data has fields"}

    # Extract nested values using dot notation
    def get_nested_value(obj: Any, path: str) -> Any:
        """Extract nested value using dot notation path"""
        parts = path.split('.')
        value = obj
        for part in parts:
            if isinstance(value, dict):
                value = value.get(part)
            elif isinstance(value, list) and part.isdigit():
                idx = int(part)
                value = value[idx] if 0 <= idx < len(value) else None
            else:
                return None
            if value is None:
                return None
        return value

    # Flatten each item
    flattened_rows = []
    for item in items:
        row = {}
        for field in fields:
            value = get_nested_value(item, field)

            # Handle arrays
            if isinstance(value, list) and explode_arrays:
                # Mark for later explosion
                row[field] = value
            elif isinstance(value, list):
                # Convert array to string representation
                row[field] = json.dumps(value) if value else None
            elif isinstance(value, dict):
                # Convert nested dict to string
                row[field] = json.dumps(value)
            else:
                row[field] = value

        flattened_rows.append(row)

    # Handle array explosion if requested
    if explode_arrays:
        logger.info("   💥 Exploding arrays into separate rows")
        exploded_rows = []
        for row in flattened_rows:
            # Find array fields
            array_fields = [k for k, v in row.items() if isinstance(v, list)]
            if not array_fields:
                exploded_rows.append(row)
            else:
                # Explode first array field (pandas behavior)
                array_field = array_fields[0]
                array_values = row[array_field]
                if array_values:
                    for value in array_values:
                        new_row = row.copy()
                        new_row[array_field] = value
                        exploded_rows.append(new_row)
                else:
                    exploded_rows.append(row)
        flattened_rows = exploded_rows
        logger.info(f"   → Exploded to {len(flattened_rows)} rows")

    logger.info(f"✅ Flattened to {len(flattened_rows)} rows × {len(fields)} columns")

    # Create pandas DataFrame
    try:
        df = pd.DataFrame(flattened_rows, columns=fields)
        logger.info(f"   📊 Created pandas DataFrame: {df.shape}")

        # Store both flattened data and DataFrame in cache
        flattened_cache_key = cache.store(
            data={"rows": flattened_rows, "columns": fields},
            key=f"flattened_{cache_key}" if cache_key else None
        )

        df_cache_key = cache.store(
            data=df,
            key=f"df_{cache_key}" if cache_key else None
        )

        logger.info(f"   💾 Stored flattened data: {flattened_cache_key}")
        logger.info(f"   💾 Stored DataFrame: {df_cache_key}")

        # Return result with cache keys
        return {
            "success": True,
            "cache_key": flattened_cache_key,
            "dataframe_cache_key": df_cache_key,
            "row_count": len(flattened_rows),
            "column_count": len(fields),
            "columns": fields,
            "sample": flattened_rows[:3],  # First 3 rows
            "dtypes": {col: str(dtype) for col, dtype in df.dtypes.items()},
            "next_step": f"Use dataframe_query(dataframe_cache_key='{df_cache_key}', ...) to filter/sort/group this data"
        }

    except Exception as e:
        logger.error(f"❌ Failed to create DataFrame: {e}", exc_info=True)
        return {
            "error": f"Failed to create DataFrame: {str(e)}",
            "flattened_data": flattened_rows[:5]  # Show sample for debugging
        }


# Tool definition
flatten_data_tool = SmartTool(
    name="flatten_data",
    description="""🔨 Flatten Nested JSON to Tabular Format

Transform complex nested JSON into flat, CSV-like structure for pandas operations.
Essential first step before filtering, sorting, or grouping data!

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📋 PARAMETERS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. cache_key (string, REQUIRED for cached data)
   - Cache key from previous tool (get_datasets, inspect_api_structure, etc.)
   - Example: "datasets_full"

2. fields (list of strings, REQUIRED)
   - Field paths to extract - supports dot notation for nested fields!
   - Top-level: ["Shortname", "Active", "Type"]
   - Nested: ["Location.Position.Latitude", "Location.Position.Longitude"]
   - Mixed: ["Shortname", "ContactInfos.0.Email", "Location.City"]
   - TIP: Use inspect_api_structure first to see available fields!

3. explode_arrays (boolean, optional, default=False)
   - If True, creates separate rows for array elements (like pandas explode)
   - Useful when you want to analyze array contents as individual rows

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📚 WORKFLOW - Always Use This Sequence!
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Step 1: Understand Structure
  inspect_api_structure(cache_key="datasets_full")
  → See available fields and nested structure

Step 2: Flatten Data
  flatten_data(
    cache_key="datasets_full",
    fields=["Shortname", "Active", "Location.City", "ContactInfos.0.Email"]
  )
  → Returns: {dataframe_cache_key: "df_xyz", columns: [...], sample: [...]}

Step 3: Query/Filter/Sort (use dataframe_query tool - next!)
  dataframe_query(
    dataframe_cache_key="df_xyz",
    operation="filter",
    condition="Active == True"
  )

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
💡 EXAMPLES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

User: "Show me all active hotels with their contact info"
  Step 1: inspect_api_structure(cache_key="datasets_full")
  Step 2: flatten_data(
            cache_key="datasets_full",
            fields=["Shortname", "Active", "Type", "ContactInfos.0.Email"]
          )
  Step 3: dataframe_query(
            dataframe_cache_key="df_xyz",
            operation="filter",
            condition="(Active == True) & (Type == 'Hotel')"
          )

User: "Extract locations of all datasets"
  flatten_data(
    cache_key="datasets_full",
    fields=["Shortname", "Location.Position.Latitude", "Location.Position.Longitude"]
  )

User: "Get all tag names as separate rows"
  flatten_data(
    cache_key="datasets_full",
    fields=["Shortname", "Tags"],
    explode_arrays=True
  )
  → Each tag becomes a separate row with its dataset name

Returns:
{
  "success": True,
  "dataframe_cache_key": "df_xyz",  ← Use this with dataframe_query!
  "row_count": 167,
  "column_count": 4,
  "columns": ["Shortname", "Active", "Type", "Email"],
  "sample": [<first 3 rows>],
  "dtypes": {"Shortname": "object", "Active": "bool", ...},
  "next_step": "Use dataframe_query tool..."
}

⚠️  IMPORTANT: ALWAYS use inspect_api_structure first to understand available fields!
⚠️  Dot notation extracts nested fields: "Location.City" gets obj['Location']['City']
⚠️  Arrays can be accessed by index: "ContactInfos.0.Email" gets first contact's email""",
    func=_flatten_data,
    max_tokens=60000
)


async def _dataframe_query(
    dataframe_cache_key: str,
    operation: str,
    condition: str | None = None,
    columns: list[str] | None = None,
    sort_by: str | None = None,
    ascending: bool = True,
    group_by: str | None = None,
    agg_func: str | None = None,
    limit: int | None = None,
    **kwargs
) -> dict:
    """
    🐼 Pandas DataFrame Query Interface

    Perform filter, sort, groupby, and other pandas operations on cached DataFrame.
    This is the power tool for complex data manipulation!

    Args:
        dataframe_cache_key: Cache key from flatten_data (REQUIRED)
        operation: Operation to perform (REQUIRED - see available operations below)
        condition: Query condition for filter operation (pandas query syntax)
        columns: List of columns to select/project
        sort_by: Column name to sort by
        ascending: Sort order (True=ascending, False=descending)
        group_by: Column to group by
        agg_func: Aggregation function (count, sum, mean, min, max, etc.)
        limit: Maximum number of rows to return

    Available Operations:
        - "filter": Filter rows by condition
        - "sort": Sort by column(s)
        - "select": Select specific columns
        - "groupby": Group and aggregate
        - "head": Get first N rows
        - "tail": Get last N rows
        - "describe": Statistical summary
        - "value_counts": Count unique values in a column

    Returns:
        {
            "success": True,
            "operation": "filter",
            "result_count": 42,
            "cache_key": "result_xyz",  # Cached result for chaining operations
            "data": [<rows>],  # Result data (limited by max_tokens)
            "columns": [...],
            "summary": {...}  # Operation-specific summary
        }
    """
    if columns:
        columns = _parse_json_string(columns)
    logger.info(f"🐼 DATAFRAME_QUERY: operation={operation}, cache_key={dataframe_cache_key}")

    # Get DataFrame from cache
    df = cache.get(dataframe_cache_key)
    if df is None:
        return {
            "error": f"DataFrame cache key '{dataframe_cache_key}' not found or expired",
            "help": "Use flatten_data first to create a DataFrame"
        }

    if not isinstance(df, pd.DataFrame):
        return {
            "error": f"Cached data is not a DataFrame (type: {type(df).__name__})",
            "help": "Ensure you're using the dataframe_cache_key from flatten_data"
        }

    logger.info(f"   📊 DataFrame loaded: {df.shape[0]} rows × {df.shape[1]} columns")

    try:
        result_df = df
        operation_summary = {}

        # Execute operation
        if operation == "filter":
            if not condition:
                return {"error": "filter operation requires 'condition' parameter"}

            logger.info(f"   🔍 Filtering with condition: {condition}")
            result_df = df.query(condition)
            operation_summary = {
                "condition": condition,
                "matched_rows": len(result_df),
                "filtered_out": len(df) - len(result_df)
            }

        elif operation == "sort":
            if not sort_by:
                return {"error": "sort operation requires 'sort_by' parameter"}

            logger.info(f"   🔃 Sorting by: {sort_by} (ascending={ascending})")
            result_df = df.sort_values(by=sort_by, ascending=ascending)
            operation_summary = {
                "sorted_by": sort_by,
                "ascending": ascending
            }

        elif operation == "select":
            if not columns:
                return {"error": "select operation requires 'columns' parameter"}

            logger.info(f"   📋 Selecting columns: {columns}")
            result_df = df[columns]
            operation_summary = {
                "selected_columns": columns,
                "dropped_columns": [c for c in df.columns if c not in columns]
            }

        elif operation == "groupby":
            if not group_by:
                return {"error": "groupby operation requires 'group_by' parameter"}
            if not agg_func:
                agg_func = "count"  # Default aggregation

            logger.info(f"   📊 Grouping by: {group_by}, aggregation: {agg_func}")

            if agg_func == "count":
                result_df = df.groupby(group_by).size().reset_index(name='count')
            else:
                # For other agg functions, need to specify columns
                numeric_cols = df.select_dtypes(include=['number']).columns.tolist()
                if not numeric_cols:
                    return {"error": "No numeric columns found for aggregation"}

                result_df = df.groupby(group_by)[numeric_cols].agg(agg_func).reset_index()

            operation_summary = {
                "grouped_by": group_by,
                "aggregation": agg_func,
                "groups_count": len(result_df)
            }

        elif operation == "head":
            n = limit if limit else 10
            logger.info(f"   ⬆️  Getting first {n} rows")
            result_df = df.head(n)
            operation_summary = {"rows_returned": len(result_df)}

        elif operation == "tail":
            n = limit if limit else 10
            logger.info(f"   ⬇️  Getting last {n} rows")
            result_df = df.tail(n)
            operation_summary = {"rows_returned": len(result_df)}

        elif operation == "describe":
            logger.info("   📈 Generating statistical summary")
            description = df.describe(include='all').to_dict()
            return {
                "success": True,
                "operation": "describe",
                "statistics": description,
                "shape": df.shape
            }

        elif operation == "value_counts":
            if not columns or len(columns) != 1:
                return {"error": "value_counts requires exactly one column in 'columns' parameter"}

            col = columns[0]
            logger.info(f"   🔢 Counting values in: {col}")
            value_counts = df[col].value_counts().to_dict()
            return {
                "success": True,
                "operation": "value_counts",
                "column": col,
                "value_counts": value_counts,
                "unique_count": len(value_counts)
            }

        else:
            return {
                "error": f"Unknown operation: {operation}",
                "available_operations": [
                    "filter", "sort", "select", "groupby", "head", "tail", "describe", "value_counts"
                ]
            }

        # Apply limit if specified
        if limit and operation not in ["head", "tail"]:
            logger.info(f"   ✂️  Applying limit: {limit}")
            result_df = result_df.head(limit)

        # Convert result to records
        result_data = result_df.to_dict(orient='records')
        result_columns = result_df.columns.tolist()

        logger.info(f"✅ Query complete: {len(result_data)} rows returned")

        # Cache result for potential chaining
        result_cache_key = cache.store(
            data=result_df,
            key=f"query_result_{operation}"
        )

        return {
            "success": True,
            "operation": operation,
            "original_count": len(df),
            "result_count": len(result_data),
            "cache_key": result_cache_key,  # For chaining operations
            "columns": result_columns,
            "data": result_data,
            "summary": operation_summary,
            "next_step": "You can chain more operations using the cache_key, or respond to user with the data"
        }

    except Exception as e:
        logger.error(f"❌ Query failed: {e}", exc_info=True)
        return {
            "error": f"Query execution failed: {str(e)}",
            "operation": operation,
            "help": "Check your query syntax and column names"
        }


# Tool definition
dataframe_query_tool = SmartTool(
    name="dataframe_query",
    description="""🐼 Pandas DataFrame Query Interface

THE POWER TOOL for filtering, sorting, grouping data!
Use this after flatten_data to perform complex data operations.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📋 REQUIRED PARAMETERS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. dataframe_cache_key (string, REQUIRED)
   - From flatten_data result
   - Example: "df_datasets_full"

2. operation (string, REQUIRED)
   AVAILABLE OPERATIONS:
   - "filter": Filter rows by condition (needs 'condition')
   - "sort": Sort by column (needs 'sort_by')
   - "select": Select specific columns (needs 'columns')
   - "groupby": Group and aggregate (needs 'group_by', optional 'agg_func')
   - "head": Get first N rows (optional 'limit', default=10)
   - "tail": Get last N rows (optional 'limit', default=10)
   - "describe": Statistical summary (no additional params needed)
   - "value_counts": Count unique values (needs 'columns' with one column)

3. Additional parameters (depends on operation):
   - condition: Pandas query string (for filter)
     Examples: "Active == True", "(Type == 'Hotel') & (Active == True)"
   - sort_by: Column name (for sort)
   - ascending: True/False (for sort, default=True)
   - columns: List of column names (for select, value_counts)
   - group_by: Column name (for groupby)
   - agg_func: "count", "sum", "mean", "min", "max", etc. (for groupby, default="count")
   - limit: Max rows to return

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
💡 COMPLETE WORKFLOW EXAMPLES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

User: "Show me all active hotels sorted by name"
  Step 1: inspect_api_structure(cache_key="datasets_full")
  Step 2: flatten_data(cache_key="datasets_full", fields=["Shortname", "Active", "Type"])
          → {dataframe_cache_key: "df_xyz"}
  Step 3: dataframe_query(
            dataframe_cache_key="df_xyz",
            operation="filter",
            condition="(Active == True) & (Type == 'Hotel')"
          )
          → {cache_key: "result_xyz", result_count: 42, data: [...]}
  Step 4: dataframe_query(
            dataframe_cache_key="result_xyz",
            operation="sort",
            sort_by="Shortname"
          )

User: "How many datasets per dataspace?"
  Step 1: flatten_data(cache_key="datasets_full", fields=["Dataspace"])
  Step 2: dataframe_query(
            dataframe_cache_key="df_xyz",
            operation="groupby",
            group_by="Dataspace",
            agg_func="count"
          )

User: "Get top 10 most recent entries"
  Step 1: flatten_data(cache_key="data", fields=["Id", "LastChange", "Shortname"])
  Step 2: dataframe_query(
            dataframe_cache_key="df_xyz",
            operation="sort",
            sort_by="LastChange",
            ascending=False,
            limit=10
          )

User: "What are the unique data types?"
  Step 1: flatten_data(cache_key="datasets_full", fields=["ApiType"])
  Step 2: dataframe_query(
            dataframe_cache_key="df_xyz",
            operation="value_counts",
            columns=["ApiType"]
          )

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔗 CHAINING OPERATIONS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Each operation returns a cache_key that you can use for the next operation!

Example chain:
1. Filter: → cache_key: "result_1"
2. Sort: dataframe_cache_key="result_1" → cache_key: "result_2"
3. Limit: dataframe_cache_key="result_2" → final result

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⚠️  PANDAS QUERY SYNTAX FOR CONDITIONS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

- Equality: Active == True, Type == 'Hotel'
- Comparison: Price > 100, Rating >= 4
- Logic: (Active == True) & (Type == 'Hotel')
- OR: (Type == 'Hotel') | (Type == 'Hostel')
- NOT: Active != False
- String contains: Shortname.str.contains('Bolzano')

Returns:
{
  "success": True,
  "operation": "filter",
  "original_count": 167,
  "result_count": 42,
  "cache_key": "result_xyz",
  "columns": ["Shortname", "Active", "Type"],
  "data": [<filtered rows>],
  "summary": {"condition": "...", "matched_rows": 42}
}

⚠️  IMPORTANT: This tool requires dataframe_cache_key from flatten_data!
⚠️  Use this for ALL filtering, sorting, grouping needs - it's much more powerful than aggregate_data!""",
    func=_dataframe_query,
    max_tokens=60000
)
