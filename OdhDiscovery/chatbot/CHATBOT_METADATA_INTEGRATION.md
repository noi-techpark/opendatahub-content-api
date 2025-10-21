# Chatbot MetaData API Integration

## Overview

The ODH Chatbot has been updated to use the MetaData API for dynamic dataset discovery, replacing hardcoded dataset lists with dynamic fetching from `https://tourism.opendatahub.com/v1/MetaData`.

## Changes Made

### 1. **ContentAPIClient** (`backend/clients/content_client.py`)

#### Updated `get_datasets()` Method
- **Before**: Fetched from generic endpoint, unclear structure
- **After**: Fetches from `/MetaData` endpoint with pagination
- **New Features**:
  - Automatically filters out deprecated datasets
  - Uses `removenullvalues=true` for cleaner responses
  - Returns complete metadata including Shortname, ApiUrl, ApiDescription, etc.

```python
async def get_datasets(self) -> list[dict]:
    """Get list of all available datasets from MetaData API"""
    metadata_url = f"{self.base_url}/MetaData"
    # Fetches all pages
    # Filters deprecated datasets
    # Returns 167 active datasets (as of 2025-10-19)
```

#### Added `get_metadata_by_shortname()` Method
New method to fetch metadata for a specific dataset by its Shortname:

```python
async def get_metadata_by_shortname(self, shortname: str) -> dict | None:
    """Get metadata for a specific dataset by its Shortname"""
    # Fetches all metadata and finds by Shortname
    # Returns None if not found
```

### 2. **Preprocessing** (`backend/preprocessing/strategies.py`)

#### Completely Rewrote `aggregate_datasets()`

**Problem**: Original aggregation produced 20,591 tokens, exceeding the 4,000 token limit for `get_datasets_tool`.

**Solution**: Ultra-compact aggregation strategy

- **Before**: Full descriptions, API URLs, filters for each dataset
- **After**: Minimal structure organized by dataspace

**New Structure**:
```python
{
    "total": 167,
    "dataspaces": {
        "tourism": [
            {"name": "Accommodation", "type": "content", "desc": "Hotels, B&Bs..."},
            {"name": "Gastronomy", "type": "content", "desc": "Restaurants..."}
        ],
        "mobility": [
            {"name": "Bicycle", "type": "timeseries", "desc": "Bicycle data..."},
            {"name": "EChargingStation", "type": "timeseries", "desc": "Charging..."}
        ],
        "weather": [...],
        ...
    },
    "_note": "Use dataset 'name' (Shortname) in get_dataset_entries..."
}
```

**Key Optimizations**:
- Abbreviated field names: `name`, `type`, `desc`
- Truncated descriptions to first sentence (max 100 chars)
- Removed API URLs (not needed for LLM to use tools)
- Removed detailed filter information
- Organized by dataspace for better context

### 3. **Tools** (`backend/tools/content_api.py`)

Updated all tool descriptions to:
1. Emphasize using **Shortname** from MetaData API
2. Recommend calling `get_datasets` first
3. Provide correct examples with proper Shortnames
4. Note case-sensitivity of Shortnames

**Updated Tools**:
- ✅ `get_datasets_tool`: Explains it fetches from MetaData API
- ✅ `get_dataset_entries_tool`: Specifies Shortname parameter
- ✅ `count_entries_tool`: Same - use Shortname
- ✅ `get_entry_by_id_tool`: Same - use Shortname

## Configuration

### Environment Variables

**`.env` or `.env.example`**:

```bash
# Content API base URL
# The client appends /MetaData or dataset Shortnames (e.g., /Accommodation)
# Do NOT include /content or /MetaData in the base URL

# Option 1: Direct access (current setup)
CONTENT_API_BASE_URL=https://tourism.opendatahub.com/v1

# Option 2: Via proxy (Docker)
CONTENT_API_BASE_URL=http://proxy:5000/api/v1/content

# Option 3: Local proxy
CONTENT_API_BASE_URL=http://localhost:5000/api/v1/content
```

**Important**: The base URL should NOT include `/content` or `/MetaData`. The client appends these automatically.

## Issues Encountered

### Issue 1: Token Limit Exceeded

**Error**:
```
Tool get_datasets result exceeds token limit (20591 > 4000), applying emergency measures
```

**Root Cause**: Original aggregation included too much detail:
- Full API URLs
- Complete descriptions
- All filter information
- Non-abbreviated field names

**Solution**: Rewrote `aggregate_datasets()` with ultra-compact structure (see above)

### Issue 2: 404 Error on Dataset Query

**Error**:
```
Client error '404 Not Found' for url 'https://tourism.opendatahub.com/v1/datasets'
```

**Root Cause**: LLM attempted to query a dataset with Shortname "datasets" instead of using actual dataset Shortnames like "Accommodation" or "ODHActivityPoi".

**Analysis**:
- The URL construction is correct: `base_url + "/" + dataset_name`
- If `dataset_name = "datasets"`, it creates `/v1/datasets` (doesn't exist)
- If `dataset_name = "Accommodation"`, it creates `/v1/Accommodation` (correct)

**Solution**:
1. Tool descriptions now emphasize using proper Shortnames
2. Recommend calling `get_datasets` first to find Shortname
3. Provide examples with correct Shortnames
4. LLM should learn to extract Shortname from get_datasets response

### Issue 3: URL Configuration Confusion

**Confusion**: Whether to include `/content` in `CONTENT_API_BASE_URL`

**Clarification**:
- ❌ **Wrong**: `https://tourism.opendatahub.com/v1/content` (creates `/v1/content/Accommodation`)
- ✅ **Correct**: `https://tourism.opendatahub.com/v1` (creates `/v1/Accommodation`)

**Why**: The client is designed to append dataset endpoints directly to the base URL, not to a `/content` sub-path. The `/content` is only used in proxy configurations.

## Dataset Discovery Flow

### Before (Hardcoded)

```
User: "What datasets are available?"
→ Agent returns hardcoded list
→ Limited to known datasets
→ No context about filters or types
```

### After (Dynamic via MetaData API)

```
User: "What datasets are available?"
→ Agent calls get_datasets tool
→ Backend fetches /MetaData (all pages)
→ Filters out deprecated
→ Aggregates into compact summary (< 4000 tokens)
→ Returns 167 datasets organized by dataspace
→ Agent answers with complete, up-to-date list
```

## Usage Examples

### Example 1: Discover Available Datasets

**User Query**: "What tourism datasets are available?"

**Agent Actions**:
1. Call `get_datasets` tool
2. Receive aggregated summary organized by dataspace
3. Filter for "tourism" dataspace
4. Present list to user

**Response Includes**:
- Accommodation (content) - Hotels, B&Bs, apartments
- Gastronomy (content) - Restaurants and food establishments
- ODHActivityPoi (content) - Activities and points of interest
- Event (content) - Events and happenings
- Article (content) - News and articles
- Etc.

### Example 2: Query Dataset Entries

**User Query**: "How many hotels are there?"

**Agent Actions**:
1. Call `get_datasets` to find tourism datasets
2. Identify "Accommodation" dataset
3. Call `count_entries` with:
   - `dataset_name='Accommodation'`
   - `raw_filter='Active eq true and Type eq "Hotel"'`
4. Return count to user

**Correct Shortname**: `'Accommodation'` (not `'accommodation'` or `'hotels'`)

### Example 3: Get Timeseries Data

**User Query**: "Show me bicycle data"

**Agent Actions**:
1. Call `get_datasets` to find mobility datasets
2. Identify "Bicycle" dataset (type: timeseries)
3. Call `get_dataset_entries` with:
   - `dataset_name='Bicycle'`
   - `pagesize=20`
4. Present bicycle data to user

## Testing

### Start Chatbot

```bash
cd chatbot
docker-compose up -d
docker-compose logs -f backend
```

### Test Queries

1. **"What datasets are available?"**
   - Should call get_datasets
   - Should return ~167 datasets
   - Should be grouped by dataspace
   - Should NOT exceed 4000 token limit

2. **"Show me all tourism datasets"**
   - Should filter by tourism dataspace
   - Should list: Accommodation, Gastronomy, ODHActivityPoi, etc.

3. **"How many hotels are there?"**
   - Should identify Accommodation dataset
   - Should use Shortname 'Accommodation'
   - Should apply filter for Type=Hotel
   - Should return count

4. **"Get data from the Bicycle dataset"**
   - Should identify Bicycle dataset (timeseries type)
   - Should use Shortname 'Bicycle'
   - Should return bicycle sensor data

### Using WebSocket Test Client

```bash
# Install wscat
npm install -g wscat

# Connect
wscat -c ws://localhost:8001/ws

# Send query
> {"type": "query", "content": "What datasets are available?"}
```

## Benefits

1. **✅ Dynamic Discovery**: No hardcoded dataset lists
2. **✅ Always Up-to-Date**: Automatically includes new datasets
3. **✅ No Deprecated Datasets**: Automatically filtered out
4. **✅ Better Context**: LLM knows API types, dataspaces, and descriptions
5. **✅ Organized**: Datasets grouped by category
6. **✅ Configurable Aggregation**: Agent controls detail level (summary/list/full)
7. **✅ Universal Structure Inspection**: Analyze any API response before fetching full data
8. **✅ Token-Efficient**: Increased limits (8000) with smart field projection
9. **✅ Consistent**: Webapp and chatbot use same metadata approach

## Recent Improvements (v2.1)

### Configurable Aggregation
The `get_datasets` tool now supports three aggregation levels:
- **"list"**: Minimal (names, types, dataspaces) - ~2000 tokens - Best for initial discovery
- **"summary"**: Compact summary by dataspace - ~7000 tokens - Good for exploration
- **"full"**: Complete metadata - Large - Use sparingly for specific needs

### Universal Structure Inspection Tool
New `inspect_api_structure` tool works with ALL Open Data Hub APIs:
- Content API datasets (accommodation, activities, etc.)
- Timeseries API (sensors, measurements, types)
- Any API returning large responses

**Workflow**:
1. User asks question about data
2. Agent calls `get_datasets(aggregation_level="list")` to find datasets
3. Agent calls `inspect_api_structure(api_type="dataset", dataset_name="...")` to see available fields
4. Agent selects relevant fields based on question
5. Agent calls `get_dataset_entries(dataset_name="...", fields=[...])` with only needed fields
6. Agent answers with minimal data, avoiding token limits

### Increased Token Limits
- Global `MAX_TOKENS_PER_TOOL`: 8000 (from 2000)
- `get_datasets_tool`: 10000 tokens
- `get_dataset_entries_tool`: 6000 tokens
- `inspect_api_structure_tool`: 4000 tokens

This allows the agent to handle larger responses while using smart aggregation and field projection to stay efficient.

## Future Improvements

1. **Caching**: Cache metadata for 5-10 minutes to reduce API calls
2. **Smart Routing**: Use ApiUrl from metadata to construct URLs
3. **Filter Awareness**: Include default filters in tool calls
4. **Type-Specific Tools**: Separate tools for content vs timeseries
5. **Dataset Recommendations**: Suggest relevant datasets based on query

## Troubleshooting

### "Tool result exceeds token limit"

**Solution**: The `aggregate_datasets()` function has been optimized. If still occurring:
1. Increase `MAX_TOKENS_PER_TOOL` for get_datasets_tool
2. Further truncate descriptions
3. Limit number of datasets shown per dataspace

### "404 Not Found for url '/v1/[dataset_name]'"

**Causes**:
1. LLM using wrong dataset name
2. Typo in Shortname
3. Deprecated dataset

**Solution**:
1. Ensure LLM calls `get_datasets` first
2. Extract exact Shortname from response
3. Use case-sensitive Shortname

### "Base URL configuration unclear"

**Correct Configuration**:
- ✅ `https://tourism.opendatahub.com/v1`
- ✅ `http://proxy:5000/api/v1/content`
- ❌ `https://tourism.opendatahub.com/v1/content`

## Summary

The chatbot now uses the MetaData API for dynamic dataset discovery, providing:
- 167 active datasets (automatically updated)
- Organized by 6 dataspaces (tourism, mobility, weather, etc.)
- Configurable aggregation levels (list/summary/full)
- Universal structure inspection for all API types
- Smart field projection to minimize token usage
- Increased token limits for handling larger responses
- Clear tool descriptions emphasizing Shortname usage
- Better context for LLM to query datasets correctly

The integration ensures the chatbot stays synchronized with the Open Data Hub ecosystem without manual updates, while giving the agent intelligent tools to handle large API responses efficiently.

---

**Last Updated**: 2025-10-19
**Version**: 2.1
**Author**: OdhDiscovery Team
