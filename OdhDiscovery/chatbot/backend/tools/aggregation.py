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

logger = logging.getLogger(__name__)
cache = get_cache()


async def _aggregate_data(
    cache_key: str | None = None,
    data: dict | list | None = None,
    strategy: str = "auto",
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

    # Auto-detect strategy if needed
    if strategy == "auto":
        logger.info("   🤖 AUTO mode: detecting best strategy...")

        # Check what parameters were provided
        if fields and len(fields) > 0:
            strategy = "extract_fields"
            logger.info(f"   → Detected: extract_fields (fields provided: {fields})")
        elif group_by:
            strategy = "count_by"
            logger.info(f"   → Detected: count_by (group_by={group_by})")
        elif data_type_hint == "datasets" and items:
            # For dataset metadata, extract commonly useful fields
            first_item = items[0] if items else {}
            if "Shortname" in first_item:
                strategy = "extract_fields"
                fields = ["Shortname", "ApiDescription", "Dataspace", "ApiType", "ApiUrl"]
                logger.info(f"   → Detected: extract_fields (dataset metadata)")
                logger.info(f"   → Auto-selected fields: {fields}")
            else:
                strategy = "sample"
                logger.info(f"   → Detected: sample (default, limit={limit})")
        else:
            # Default: sample
            strategy = "sample"
            logger.info(f"   → Detected: sample (default, limit={limit})")

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

Apply various strategies to reduce large data responses. Has smart defaults!

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📋 PARAMETERS (Most to Least Important)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. cache_key (string, RECOMMENDED for large data)
   - Cache key from get_datasets(aggregation_level="full")
   - Example: "datasets_full"

2. strategy (string, optional - defaults to "auto")
   - "auto" (DEFAULT): Smart detection based on data + parameters
   - "extract_fields": Get specific fields only
   - "count_by": Count grouped by field
   - "sample": Take first N items
   - "group_by": Group with counts and samples
   - "distinct_values": Get unique values per field
   - "count_total": Just count items

3. fields (list of strings, optional)
   - Field names to extract or analyze
   - Example: ["Shortname", "ApiDescription", "Dataspace"]

4. group_by (string, optional)
   - Field name to group/count by
   - Example: "Dataspace" or "ApiType"

5. limit (number, optional, default=20)
   - Number of items for sampling

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✨ EASY USAGE - Just provide cache_key!
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

EASIEST: Let auto mode decide
  aggregate_data(cache_key="datasets_full")
  → Automatically extracts: Shortname, ApiDescription, Dataspace, ApiType, ApiUrl

With specific fields:
  aggregate_data(cache_key="datasets_full", fields=["Shortname", "Dataspace"])
  → Auto-detects strategy="extract_fields"

Count by category:
  aggregate_data(cache_key="datasets_full", group_by="Dataspace")
  → Auto-detects strategy="count_by"

Get samples:
  aggregate_data(cache_key="datasets_full", limit=10)
  → Uses strategy="sample"

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📚 COMPLETE EXAMPLES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

User: "Which datasets are available? I want a detailed list"
  Step 1: get_datasets(aggregation_level="full")
          → {cache_key: "datasets_full", total: 167, next_step: "..."}
  Step 2: aggregate_data(cache_key="datasets_full")
          → Auto-extracts common fields (Shortname, ApiDescription, etc.)
  Step 3: Respond with detailed list

User: "How many datasets per dataspace?"
  Step 1: get_datasets(aggregation_level="full")
  Step 2: aggregate_data(cache_key="datasets_full", group_by="Dataspace")
          → {tourism: 120, mobility: 30, weather: 17}

User: "Show me 5 example datasets"
  Step 1: get_datasets(aggregation_level="full")
  Step 2: aggregate_data(cache_key="datasets_full", limit=5)
          → {items: [<5 datasets>]}

User: "What types of APIs are available?"
  Step 1: get_datasets(aggregation_level="full")
  Step 2: aggregate_data(cache_key="datasets_full", group_by="ApiType")
          → {content: 142, timeseries: 25}

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
💡 SMART AUTO MODE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

When strategy="auto" (default), the tool intelligently picks:
- If fields provided → extract_fields
- If group_by provided → count_by
- If dataset metadata detected → extract common fields automatically
- Otherwise → sample (limit=20)

You almost never need to specify strategy explicitly!

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

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
