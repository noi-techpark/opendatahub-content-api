# Chatbot Tools User Guide

## Overview

The ODH Chatbot has two **Swiss Army Knife** tools designed to handle large API responses intelligently. Both tools are sound, well-documented, and incredibly easy to use with smart defaults.

---

## 🔧 Tool 1: `aggregate_data` - Data Aggregation Swiss Army Knife

### Purpose
Apply various aggregation strategies to reduce large data responses. Has **AUTO mode** for maximum ease of use!

### The Magic of AUTO Mode

Simply provide `cache_key` and the tool intelligently decides what to do:

```python
# User: "Which datasets are available? I want a detailed list"

# Step 1: Get cache key
get_datasets(aggregation_level="full")
→ Returns: {cache_key: "datasets_full", ...}

# Step 2: Just pass the cache_key!
aggregate_data(cache_key="datasets_full")

# AUTO mode detects it's dataset metadata and automatically:
# - Uses strategy="extract_fields"
# - Selects fields: ["Shortname", "ApiDescription", "Dataspace", "ApiType", "ApiUrl"]
# - Returns complete, detailed list!
```

**You don't need to specify strategy or fields** - the tool is smart enough to figure it out!

### Parameters (in order of importance)

1. **`cache_key`** (string, recommended)
   - Cache key from `get_datasets(aggregation_level="full")`
   - Example: `"datasets_full"`

2. **`strategy`** (string, optional, default=`"auto"`)
   - `"auto"` - Smart detection (RECOMMENDED!)
   - `"extract_fields"` - Get specific fields
   - `"count_by"` - Count grouped by field
   - `"sample"` - Take first N items
   - `"group_by"` - Group with counts and samples
   - `"distinct_values"` - Get unique values
   - `"count_total"` - Just count

3. **`fields`** (list of strings, optional)
   - Field names to extract
   - Example: `["Shortname", "ApiDescription", "Dataspace"]`
   - If provided, AUTO mode uses `extract_fields`

4. **`group_by`** (string, optional)
   - Field name to group/count by
   - Example: `"Dataspace"` or `"ApiType"`
   - If provided, AUTO mode uses `count_by`

5. **`limit`** (number, optional, default=20)
   - Number of items for sampling

### Usage Examples

#### Example 1: Easiest - Just Cache Key (AUTO mode)
```python
aggregate_data(cache_key="datasets_full")

# Auto-detects dataset metadata
# Auto-selects fields: Shortname, ApiDescription, Dataspace, ApiType, ApiUrl
# Returns detailed list of all 167 datasets
```

**Logs show**:
```
🔄 AGGREGATION: cache_key=datasets_full, strategy='auto'
📊 Processing 167 items (type: datasets)
   🤖 AUTO mode: detecting best strategy...
   → Detected: extract_fields (dataset metadata)
   → Auto-selected fields: ['Shortname', 'ApiDescription', 'Dataspace', 'ApiType', 'ApiUrl']
   ✓ Using strategy: 'extract_fields'
✓ Extracted 5 fields from 167 items
✅ AGGREGATION COMPLETE
```

#### Example 2: With Specific Fields
```python
aggregate_data(
    cache_key="datasets_full",
    fields=["Shortname", "Dataspace"]
)

# Auto-detects strategy="extract_fields" because fields provided
# Returns only those 2 fields
```

#### Example 3: Count by Category
```python
aggregate_data(
    cache_key="datasets_full",
    group_by="Dataspace"
)

# Auto-detects strategy="count_by" because group_by provided
# Returns: {tourism: 120, mobility: 30, weather: 17}
```

#### Example 4: Get Samples
```python
aggregate_data(
    cache_key="datasets_full",
    limit=5
)

# Uses strategy="sample"
# Returns first 5 datasets
```

#### Example 5: Explicit Strategy (Not Usually Needed)
```python
aggregate_data(
    cache_key="datasets_full",
    strategy="distinct_values",
    fields=["ApiType", "Dataspace"]
)

# Returns unique values:
# {ApiType: ["content", "timeseries"], Dataspace: ["tourism", "mobility", ...]}
```

### Available Strategies

| Strategy | What It Does | Required Parameters | Use When |
|----------|--------------|---------------------|----------|
| `auto` | Smart detection | None! | Always (it's the default) |
| `extract_fields` | Get specific fields | `fields` | Need certain fields only |
| `count_by` | Count by category | `group_by` | "How many X per Y?" |
| `sample` | Take first N | `limit` | "Show me some examples" |
| `group_by` | Group with samples | `group_by` | "Group by category" |
| `distinct_values` | Unique values | `fields` | "What values exist?" |
| `count_total` | Just count | None | "How many total?" |
| `summarize_fields` | Statistics | `fields` | "What's the average?" |

### Returns

```json
{
  "strategy": "extract_fields",
  "original_count": 167,
  "items": [...],
  "extracted_fields": ["Shortname", "ApiDescription", ...]
}
```

---

## 🔍 Tool 2: `inspect_api_structure` - API Schema Inspector

### Purpose
See what fields are available **WITHOUT** fetching all data. Analyzes only 3 samples - super fast and lightweight!

### Use Cases

1. **Before fetching data** - Know what fields to request
2. **Exploring new datasets** - See structure without downloading everything
3. **Field validation** - Check if a field exists before querying

### Parameters

1. **`api_type`** (required) - What API to inspect
   - `"dataset"` - Content datasets (accommodation, activities, etc.)
   - `"timeseries"` - Timeseries/measurements
   - `"sensors"` - Sensor listings
   - `"types"` - Type listings
   - `"measurements"` - Measurement data

2. **`dataset_name`** (required for `api_type="dataset"`)
   - Shortname from `get_datasets`
   - Example: `"Accommodation"`, `"ODHActivityPoi"`

3. **`sensor_name`** (required for timeseries/measurements)
   - Sensor identifier
   - Example: `"BZ:00001"`

4. **`type_name`** (optional for timeseries)

### Usage Examples

#### Example 1: Inspect Dataset Structure
```python
# User: "What info is available for hotels?"

inspect_api_structure(
    api_type="dataset",
    dataset_name="Accommodation"
)

# Returns field list:
# - Id (string): "ABC123"
# - Shortname (string): "Hotel Panorama"
# - Type (string): "Hotel"
# - GpsInfo.Latitude (number): 46.4983
# - ContactInfos (object with 5 keys)
# - AccoDetail (object with 12 keys)
# - ImageGallery (array of 15 items)
# ... 40 more fields
```

#### Example 2: Then Fetch Only Needed Fields
```python
# User: "Get hotel names and locations"

# Step 1: See what fields exist
inspect_api_structure(
    api_type="dataset",
    dataset_name="Accommodation"
)
# → Confirms Shortname and GpsInfo fields exist

# Step 2: Fetch only those fields
get_dataset_entries(
    dataset_name="Accommodation",
    fields=["Shortname", "GpsInfo"],
    pagesize=50
)
# → Fetches only 2 fields instead of all 47!
# → Saves ~95% of tokens!
```

#### Example 3: Inspect Timeseries
```python
inspect_api_structure(
    api_type="timeseries",
    sensor_name="BZ:00001"
)

# Returns:
# - timestamp (string)
# - value (number)
# - sensor_id (string)
# - type_name (string)
```

#### Example 4: Inspect All Sensors
```python
inspect_api_structure(api_type="sensors")

# Returns structure of sensor listings
```

### Returns

```json
{
  "api_type": "dataset",
  "dataset_name": "Accommodation",
  "field_count": 47,
  "total_entries": 8532,
  "sample_count": 3,
  "fields": [
    {
      "path": "Id",
      "types": ["string"],
      "sample": "ABC123"
    },
    {
      "path": "Shortname",
      "types": ["string"],
      "sample": "Hotel Panorama"
    },
    {
      "path": "GpsInfo.Latitude",
      "types": ["number"],
      "sample": "46.4983"
    }
  ],
  "_note": "Use these paths in 'fields' parameter"
}
```

---

## 🎯 Complete Workflows

### Workflow 1: Simple List (Easiest)

**User**: "Which datasets are available? I want a detailed list"

```python
# Step 1: Get datasets and store in cache
get_datasets(aggregation_level="full")
→ {cache_key: "datasets_full", total: 167, ...}

# Step 2: Auto-aggregate with cache key
aggregate_data(cache_key="datasets_full")
→ AUTO mode extracts: Shortname, ApiDescription, Dataspace, ApiType, ApiUrl
→ Returns detailed list of 167 datasets

# Step 3: Respond to user
"Here are all 167 available datasets: ..."
```

### Workflow 2: Count by Category

**User**: "How many datasets per dataspace?"

```python
# Step 1: Get datasets
get_datasets(aggregation_level="full")

# Step 2: Count by dataspace
aggregate_data(cache_key="datasets_full", group_by="Dataspace")
→ {tourism: 120, mobility: 30, weather: 17}

# Step 3: Respond
"Tourism: 120, Mobility: 30, Weather: 17"
```

### Workflow 3: Inspect Then Fetch

**User**: "Get hotel names and GPS coordinates"

```python
# Step 1: Inspect structure
inspect_api_structure(api_type="dataset", dataset_name="Accommodation")
→ Confirms Shortname and GpsInfo fields exist

# Step 2: Fetch only those fields
get_dataset_entries(
    dataset_name="Accommodation",
    fields=["Shortname", "GpsInfo"],
    raw_filter="Type eq 'Hotel'"
)
→ ~90% token savings vs fetching all fields!

# Step 3: Return data
```

### Workflow 4: Multiple Aggregations

**User**: "Tell me about the datasets - types, dataspaces, and examples"

```python
# Step 1: Get and cache
get_datasets(aggregation_level="full")
→ cache_key: "datasets_full"

# Step 2: Count by type
aggregate_data(cache_key="datasets_full", group_by="ApiType")
→ {content: 142, timeseries: 25}

# Step 3: Count by dataspace
aggregate_data(cache_key="datasets_full", group_by="Dataspace")
→ {tourism: 120, mobility: 30, ...}

# Step 4: Get samples
aggregate_data(cache_key="datasets_full", limit=5)
→ First 5 full dataset objects

# Step 5: Comprehensive answer
```

---

## 🚀 Why These Tools are Powerful

### 1. Smart Defaults - Zero Configuration

```python
# Just provide cache_key - tool figures out the rest!
aggregate_data(cache_key="datasets_full")
```

No need to specify:
- Strategy (auto-detected)
- Fields (auto-selected for common cases)
- Complex parameters

### 2. Parameter-Based Auto-Detection

```python
# Provide fields → auto-detects extract_fields
aggregate_data(cache_key="...", fields=["Shortname"])

# Provide group_by → auto-detects count_by
aggregate_data(cache_key="...", group_by="Dataspace")

# Provide limit → uses sample
aggregate_data(cache_key="...", limit=10)
```

### 3. Domain-Aware Intelligence

For dataset metadata, AUTO mode knows to extract:
- Shortname (name)
- ApiDescription (what it contains)
- Dataspace (category)
- ApiType (content vs timeseries)
- ApiUrl (endpoint)

These are the most commonly needed fields!

### 4. Extensive Logging

Every decision is logged:
```
🔄 AGGREGATION: cache_key=datasets_full, strategy='auto'
📊 Processing 167 items (type: datasets)
   🤖 AUTO mode: detecting best strategy...
   → Detected: extract_fields (dataset metadata)
   → Auto-selected fields: [...]
   ✓ Using strategy: 'extract_fields'
✓ Extracted 5 fields from 167 items
✅ AGGREGATION COMPLETE
```

You can **see exactly what the tool decided to do**!

### 5. Token Efficient

| Without Tools | With Tools | Savings |
|---------------|------------|---------|
| Fetch all 47 fields | Fetch 2 fields | 95% |
| 102,920 tokens | ~10,000 tokens | 90% |
| Emergency truncation | No truncation | ✓ |

---

## 📊 Comparison: Before vs After

### Before (Manual Strategy Selection - Error Prone)

```python
# Agent had to:
1. Decide strategy manually
2. List all fields to extract
3. Handle errors if wrong strategy chosen
4. Retry with different parameters

# Often resulted in:
- Wrong strategy chosen
- Missing required parameters
- Token limit errors
- Incomplete data
```

### After (AUTO Mode - Bulletproof)

```python
# Agent just:
aggregate_data(cache_key="datasets_full")

# Tool automatically:
✓ Detects data type
✓ Chooses best strategy
✓ Selects appropriate fields
✓ Logs every decision
✓ Returns optimal result

# Result:
- Always works
- No parameter errors
- Efficient token usage
- Complete data
```

---

## 🎓 Pro Tips

### Tip 1: Start with AUTO
Always use AUTO mode first. Only specify strategy if you need something specific.

```python
# Good (AUTO)
aggregate_data(cache_key="datasets_full")

# Also good (explicit fields, still AUTO)
aggregate_data(cache_key="datasets_full", fields=["Shortname", "Type"])

# Only if needed (explicit strategy)
aggregate_data(cache_key="datasets_full", strategy="distinct_values", fields=["ApiType"])
```

### Tip 2: Inspect Before Fetching
Use `inspect_api_structure` to avoid fetching unnecessary fields:

```python
# Bad - fetches all 47 fields
get_dataset_entries(dataset_name="Accommodation")

# Good - inspect first
inspect_api_structure(api_type="dataset", dataset_name="Accommodation")
get_dataset_entries(dataset_name="Accommodation", fields=["Shortname", "GpsInfo"])
```

### Tip 3: Reuse Cache Keys
The cache lasts 5 minutes - you can aggregate the same data multiple ways:

```python
get_datasets(aggregation_level="full")
→ cache_key: "datasets_full"

# Call multiple times with different strategies
aggregate_data(cache_key="datasets_full", group_by="ApiType")
aggregate_data(cache_key="datasets_full", group_by="Dataspace")
aggregate_data(cache_key="datasets_full", limit=5)

# All use same cached data - no re-fetching!
```

### Tip 4: Watch the Logs
Logs show exactly what AUTO mode decided:

```bash
docker-compose logs -f backend | grep "🔄\|🤖\|→\|✓"
```

---

## 📝 Quick Reference

### aggregate_data Cheat Sheet

```python
# Easiest - AUTO mode
aggregate_data(cache_key="...")

# With fields
aggregate_data(cache_key="...", fields=["field1", "field2"])

# Count by category
aggregate_data(cache_key="...", group_by="CategoryField")

# Get samples
aggregate_data(cache_key="...", limit=N)

# Explicit strategy (rarely needed)
aggregate_data(cache_key="...", strategy="distinct_values", fields=[...])
```

### inspect_api_structure Cheat Sheet

```python
# Inspect dataset
inspect_api_structure(api_type="dataset", dataset_name="DatasetName")

# Inspect timeseries
inspect_api_structure(api_type="timeseries", sensor_name="SensorID")

# Inspect sensors
inspect_api_structure(api_type="sensors")

# Inspect types
inspect_api_structure(api_type="types")
```

---

## Summary

Both tools are designed to be:
- **Easy**: Just provide cache_key or api_type
- **Smart**: AUTO mode handles complexity
- **Generic**: Work with any API response
- **Observable**: Extensive logging shows decisions
- **Efficient**: Minimize token usage

Think of them as Swiss Army knives - one tool, many strategies, always the right one!

---

**Last Updated**: 2025-10-19
**Version**: 1.0
**Author**: OdhDiscovery Team
