# MetaData API Documentation

## Overview

The MetaData API is a discovery endpoint in the Open Data Hub Content API that provides metadata information about all available datasets. It acts as a catalog or registry of datasets, containing information about how to access each dataset, including API endpoints, filters, data providers, and licensing information.

## MetaData API Endpoint

```
https://tourism.opendatahub.com/v1/MetaData
```

### Query Parameters

- `pagenumber` - Page number for pagination (default: 1)
- `pagesize` - Number of items per page (default: 10, max: 100)
- `removenullvalues` - Remove null values from response (recommended: true)
- Standard Content API filters: `searchfilter`, `rawfilter`, etc.

### Example Request

```bash
curl "https://tourism.opendatahub.com/v1/MetaData?pagenumber=1&pagesize=10&removenullvalues=true"
```

## MetaData Entry Structure

Each metadata entry contains the following key fields:

### Core Fields

| Field | Type | Description | Always Present |
|-------|------|-------------|----------------|
| `Id` | string | Unique identifier for the metadata entry | ✓ |
| `Shortname` | string | Human-readable name (e.g., "Museums", "Sensor") | ✓ |
| `ApiType` | string | Type of API: "content" or "timeseries" | ~99% |
| `ApiUrl` | string | Complete example API URL | ✓ |
| `BaseUrl` | string | Base URL of the API (e.g., "https://tourism.api.opendatahub.com") | ✓ |
| `PathParam` | array | Array of path components (e.g., ["v1", "ODHActivityPoi"]) | ✓ |
| `ApiFilter` | array | Array of filter parameters (e.g., ["tagfilter=museums"]) | ~45% |
| `Dataspace` | string | Category: "tourism", "mobility", "weather", etc. | ✓ |
| `Deprecated` | boolean | Whether the dataset is deprecated | ✓ |

### Additional Fields

| Field | Type | Description | Presence |
|-------|------|-------------|----------|
| `ApiDescription` | object | Descriptions in different languages (e.g., `{en: "..."}`) | 100% |
| `DataProvider` | array | List of data providers | 91% |
| `LicenseInfo` | object | License information (Author, License, LicenseHolder) | 92% |
| `ImageGallery` | array | Representative images for the dataset | 98% |
| `SwaggerUrl` | string | Link to Swagger documentation | 100% |
| `FirstImport` | string | Timestamp of first import | 100% |
| `LastChange` | string | Timestamp of last change | 100% |
| `ApiAccess` | object | Access restrictions and authentication info | 29% |
| `Sources` | array | Source system identifiers | 29% |
| `RecordCount` | number | Approximate number of records | 28% |
| `Category` | string | Category classification | 9% |

### Example MetaData Entry

```json
{
  "Id": "00109544-2a58-4051-b619-b350e0035d20",
  "Shortname": "Museums",
  "ApiType": "content",
  "BaseUrl": "https://tourism.api.opendatahub.com",
  "PathParam": ["v1", "ODHActivityPoi"],
  "ApiFilter": ["tagfilter=museums"],
  "ApiUrl": "https://tourism.api.opendatahub.com/v1/ODHActivityPoi?tagfilter=museums",
  "Dataspace": "tourism",
  "Deprecated": false,
  "DataProvider": ["SIAG", "LTS"],
  "LicenseInfo": {
    "Author": "https://noi.bz.it",
    "License": "CC0",
    "ClosedData": false,
    "LicenseHolder": "https://noi.bz.it"
  },
  "ApiDescription": {
    "en": "This dataset contains all points of interest that have the tag 'museums' assigned by IDM."
  },
  "SwaggerUrl": "https://tourism.api.opendatahub.com/swagger/index.html#/ODHActivityPoi"
}
```

## Building API URLs from Metadata

### The ApiUrl-First Approach

The **simplest and recommended approach** is to use the `ApiUrl` field directly. This field already contains:
- The complete base URL
- The full API path
- Default filters from `ApiFilter`

**Key Principle**: `ApiUrl` is your starting point. Just parse it, merge your parameters, and rebuild the URL.

### Step-by-Step Process

1. **Start with ApiUrl**
   - Extract `ApiUrl` from metadata
   - This is a complete, working URL example
   - Example: `https://tourism.api.opendatahub.com/v1/ODHActivityPoi?tagfilter=museums&source=lts`

2. **Parse the URL**
   - Separate the base URL from query parameters
   - Extract existing parameters (these are the default filters)

3. **Merge Parameters**
   - Combine existing parameters with user parameters
   - User parameters take precedence (can override defaults)
   - Filter out null/undefined/empty values

4. **Rebuild URL**
   - Reconstruct URL with merged parameters
   - Return complete URL ready for API call

### Code Example (JavaScript)

```javascript
/**
 * Build full URL from metadata using ApiUrl
 * The ApiUrl already contains the base URL and default filters
 * Additional query params are appended
 */
function buildFullUrlFromMetadata(metadata, params = {}) {
  if (!metadata) {
    return null
  }

  // Get ApiUrl from metadata - this already includes base filters
  const apiUrl = metadata.ApiUrl || metadata.metadata?.ApiUrl
  if (!apiUrl) {
    return null
  }

  // Parse existing URL to separate base URL and existing params
  const url = new URL(apiUrl)

  // Merge existing params with new params (new params take precedence)
  const mergedParams = new URLSearchParams(url.search)
  Object.entries(params).forEach(([key, value]) => {
    if (value !== null && value !== undefined && value !== '') {
      mergedParams.set(key, value)
    }
  })

  // Build final URL
  const queryString = mergedParams.toString()
  return `${url.origin}${url.pathname}${queryString ? `?${queryString}` : ''}`
}

// Usage Example 1: Museums dataset (Content API with default filters)
const museumsMetadata = {
  ApiUrl: "https://tourism.api.opendatahub.com/v1/ODHActivityPoi?tagfilter=museums&source=lts"
}

const museumsUrl = buildFullUrlFromMetadata(museumsMetadata, {
  pagenumber: 1,
  pagesize: 50,
  searchfilter: "castle"
})
// Result: https://tourism.api.opendatahub.com/v1/ODHActivityPoi?tagfilter=museums&source=lts&pagenumber=1&pagesize=50&searchfilter=castle

// Usage Example 2: Bicycle dataset (Timeseries API)
const bicycleMetadata = {
  ApiUrl: "https://mobility.api.opendatahub.com/v2/flat/Bicycle?limit=200&offset=0&shownull=false&distinct=true"
}

const bicycleUrl = buildFullUrlFromMetadata(bicycleMetadata, {
  limit: 50,  // Override default limit
  offset: 100
})
// Result: https://mobility.api.opendatahub.com/v2/flat/Bicycle?limit=50&offset=100&shownull=false&distinct=true

// Usage Example 3: Override existing params
const overrideUrl = buildFullUrlFromMetadata(bicycleMetadata, {
  limit: 25,          // Overrides default limit=200
  offset: 0,          // Overrides default offset=0
  shownull: true      // Overrides default shownull=false
})
// Result: https://mobility.api.opendatahub.com/v2/flat/Bicycle?limit=25&offset=0&shownull=true&distinct=true
```

### Why ApiUrl-First?

**Advantages:**
- ✅ Simpler code - no need to build URLs from scratch
- ✅ Works for all API types (content, timeseries) without special cases
- ✅ Default filters automatically included
- ✅ No need to know API-specific parameter conventions
- ✅ Less error-prone - uses real, working URL as template

**Legacy Approach (Not Recommended):**
The old approach of building URLs from `BaseUrl + PathParam + ApiFilter` required:
- Different logic for content vs timeseries APIs
- Parameter conversion (pagenumber/pagesize ↔ offset/limit)
- Response normalization
- Multiple HTTP clients

The ApiUrl-first approach eliminates all this complexity.

## API Types and Base URLs

### Content API (126 datasets)
- **Base URL**: `https://tourism.api.opendatahub.com`
- **API Type**: `"content"`
- **Dataspace**: Primarily `"tourism"`
- **Use Case**: Static or semi-static content (POIs, accommodations, events, etc.)
- **Path Structure**: `["v1", "DatasetName"]`
- **Query Parameters**: `pagenumber`, `pagesize`, `searchfilter`, `rawfilter`
- **Response Format**:
  ```json
  {
    "TotalResults": 1234,
    "TotalPages": 13,
    "CurrentPage": 1,
    "Items": [...]
  }
  ```

### Timeseries API (45 datasets)
- **Base URL**: `https://mobility.api.opendatahub.com`
- **API Type**: `"timeseries"`
- **Dataspace**: Primarily `"mobility"`, `"weather"`
- **Use Case**: Time-series sensor data, real-time measurements
- **Path Structure**: `["v2", "flat", "SensorType"]`
- **Query Parameters**: `limit`, `offset`
- **Response Format**:
  ```json
  {
    "offset": 0,
    "limit": 25,
    "data": [...]
  }
  ```
- **Note**: Timeseries API does NOT provide `TotalResults` or `TotalPages`

## Common ApiFilter Patterns

### By Source System
```json
"ApiFilter": ["source=lts"]        // LTS (Lodging Tourism South Tyrol)
"ApiFilter": ["source=hgv"]        // HGV (Hoteliers Association)
"ApiFilter": ["source=siag"]       // SIAG (Museums, Weather)
"ApiFilter": ["source=feratel"]    // Feratel (Webcams, Tourism data)
```

### By Tag Filter
```json
"ApiFilter": ["tagfilter=museums"]
"ApiFilter": ["tagfilter=activity"]
"ApiFilter": ["tagfilter=and(lifts)"]
```

### By Multiple Criteria
```json
"ApiFilter": ["origin=webcomp-brennerlec", "source=a22"]
"ApiFilter": ["source=gtfsapi", "tagfilter=72861940-e6b6-435a-9bb9-7a20058bd6d0"]
```

### By Article Type
```json
"ApiFilter": ["articletype=newsfeednoi"]
"ApiFilter": ["articletype=rezeptartikel"]
```

## Special Cases

### Hardcoded Datasets (Not in MetaData)

#### 1. MetaData Dataset
```javascript
{
  Shortname: "MetaData",
  PathParam: ["v1", "MetaData"],
  ApiFilter: [],
  ApiType: "content",
  BaseUrl: "https://tourism.opendatahub.com"
}
```
**Purpose**: The metadata registry itself

#### 2. Sensor Dataset
```javascript
{
  Shortname: "Sensor",
  PathParam: ["v1", "Sensor"],
  ApiFilter: [],
  ApiType: "content",
  BaseUrl: "https://tourism.opendatahub.com"
}
```
**Purpose**: Sensor metadata (not timeseries data itself)

## Statistics (as of 2025-10-19)

- **Total Datasets**: 173
- **Content API**: 126 datasets
- **Timeseries API**: 45 datasets
- **Other/Unknown**: 2 datasets
- **Deprecated**: Filtered out by default

### Dataspace Distribution
- Tourism: 115
- Mobility: 43
- Weather: 7
- Other: 6
- Environmental: 1
- Energy: 1

## Implementation in OdhDiscovery WebApp

The webapp implements the ApiUrl-first approach using a single generic HTTP client for all API calls.

### HTTP Client Setup

```javascript
// src/api/client.js
import axios from 'axios'

// Generic axios instance without baseURL - uses full URLs from metadata
export const genericClient = axios.create({
  headers: {
    'Content-Type': 'application/json'
  }
})

// Legacy content client for backward compatibility
export const contentClient = axios.create({
  baseURL: '/api/v1/content',
  headers: {
    'Content-Type': 'application/json'
  }
})
```

**Why genericClient?**
- No baseURL - accepts full URLs from `metadata.ApiUrl`
- Works with any API (content, timeseries, etc.)
- Eliminates need for multiple clients
- Simpler and more maintainable

### 1. Fetching All Metadata

```javascript
// src/api/contentApi.js
import { contentClient } from './client'

export async function getAllMetadata() {
  const allMetadata = []
  let currentPage = 1
  let hasMorePages = true

  while (hasMorePages) {
    const response = await contentClient.get('/MetaData', {
      params: {
        pagenumber: currentPage,
        pagesize: 100,
        removenullvalues: true
      }
    })

    allMetadata.push(...(response.data.Items || []))

    if (currentPage >= (response.data.TotalPages || 1)) {
      hasMorePages = false
    } else {
      currentPage++
    }
  }

  return allMetadata
}
```

### 2. Finding Metadata by Shortname

```javascript
export async function getMetadataByShortname(shortname) {
  // Handle hardcoded datasets
  if (shortname === 'MetaData') {
    return {
      Shortname: 'MetaData',
      PathParam: ['v1', 'MetaData'],
      ApiFilter: [],
      ApiType: 'content',
      BaseUrl: 'https://tourism.opendatahub.com',
      ApiUrl: 'https://tourism.opendatahub.com/v1/MetaData'
    }
  }

  if (shortname === 'Sensor') {
    return {
      Shortname: 'Sensor',
      PathParam: ['v1', 'Sensor'],
      ApiFilter: [],
      ApiType: 'content',
      BaseUrl: 'https://tourism.opendatahub.com',
      ApiUrl: 'https://tourism.opendatahub.com/v1/Sensor'
    }
  }

  // Fetch from API and find by Shortname
  const allMetadata = await getAllMetadata()
  return allMetadata.find(m => m.Shortname === shortname)
}
```

### 3. Building Full URL from Metadata (ApiUrl-First)

```javascript
/**
 * Build full URL from metadata using ApiUrl
 * The ApiUrl already contains the base URL and default filters
 * Additional query params are appended
 */
function buildFullUrlFromMetadata(metadata, params = {}) {
  if (!metadata) {
    return null
  }

  // Get ApiUrl from metadata - this already includes base filters
  const apiUrl = metadata.ApiUrl || metadata.metadata?.ApiUrl
  if (!apiUrl) {
    return null
  }

  // Parse existing URL to separate base URL and existing params
  const url = new URL(apiUrl)

  // Merge existing params with new params (new params take precedence)
  const mergedParams = new URLSearchParams(url.search)
  Object.entries(params).forEach(([key, value]) => {
    if (value !== null && value !== undefined && value !== '') {
      mergedParams.set(key, value)
    }
  })

  // Build final URL
  const queryString = mergedParams.toString()
  return `${url.origin}${url.pathname}${queryString ? `?${queryString}` : ''}`
}
```

### 4. Making API Calls with Metadata

```javascript
import { genericClient, contentClient } from './client'

/**
 * Get dataset entries with pagination and filters
 * @param {string} datasetName - The dataset name (not used if metadata provided)
 * @param {object} params - Query parameters (pagenumber, pagesize, searchfilter, rawfilter, etc.)
 * @param {object} metadata - Metadata object with ApiUrl
 */
export async function getDatasetEntries(datasetName, params = {}, metadata = null) {
  // If metadata is provided, use ApiUrl from metadata
  if (metadata) {
    const fullUrl = buildFullUrlFromMetadata(metadata, params)
    if (fullUrl) {
      const response = await genericClient.get(fullUrl)
      return response.data
    }
  }

  // Fallback to contentClient for backwards compatibility
  const queryString = buildQueryString(params)
  const url = `/${datasetName}${queryString ? `?${queryString}` : ''}`
  const response = await contentClient.get(url)
  return response.data
}

/**
 * Get single entry by ID
 * @param {string} datasetName - The dataset name (not used if metadata provided)
 * @param {string} id - Entry ID
 * @param {object} params - Query parameters
 * @param {object} metadata - Metadata object with ApiUrl
 */
export async function getDatasetEntry(datasetName, id, params = {}, metadata = null) {
  // If metadata is provided, append /{id} to ApiUrl
  if (metadata && metadata.ApiUrl) {
    const apiUrl = metadata.ApiUrl
    const url = new URL(apiUrl)
    url.pathname = `${url.pathname}/${id}`

    // Add params
    Object.entries(params).forEach(([key, value]) => {
      if (value !== null && value !== undefined && value !== '') {
        url.searchParams.set(key, value)
      }
    })

    const response = await genericClient.get(url.toString())
    return response.data
  }

  // Fallback to contentClient
  const queryString = buildQueryString(params)
  const url = `/${datasetName}/${id}${queryString ? `?${queryString}` : ''}`
  const response = await contentClient.get(url)
  return response.data
}

/**
 * Get dataset metadata (total count, etc.)
 * @param {string} datasetName - The dataset name (not used if metadata provided)
 * @param {object} metadata - Metadata object with ApiUrl
 */
export async function getDatasetMetadata(datasetName, metadata = null) {
  // If metadata is provided, use ApiUrl
  if (metadata) {
    const fullUrl = buildFullUrlFromMetadata(metadata, { pagesize: 1, pagenumber: 1 })
    if (fullUrl) {
      const response = await genericClient.get(fullUrl)
      const data = response.data
      return {
        totalResults: data.TotalResults || data.data?.length || 0,
        totalPages: data.TotalPages || 1
      }
    }
  }

  // Fallback to contentClient
  const response = await contentClient.get(`/${datasetName}`, {
    params: {
      pagesize: 1,
      pagenumber: 1
    }
  })
  return {
    totalResults: response.data.TotalResults,
    totalPages: response.data.TotalPages
  }
}
```

### 5. Store Integration (Pinia)

```javascript
// src/stores/datasetStore.js
import { defineStore } from 'pinia'
import { ref } from 'vue'
import * as contentApi from '../api/contentApi'

export const useDatasetStore = defineStore('dataset', () => {
  const currentMetadata = ref(null) // Store metadata for current dataset
  const entries = ref([])
  const currentPage = ref(1)
  const pageSize = ref(50)

  async function loadDatasetEntries(datasetName, params = {}) {
    // Fetch metadata for this dataset (if not already loaded)
    if (!currentMetadata.value || currentMetadata.value.Shortname !== datasetName) {
      try {
        currentMetadata.value = await contentApi.getMetadataByShortname(datasetName)
      } catch (metaErr) {
        console.warn('Could not fetch metadata for dataset:', datasetName, metaErr)
        currentMetadata.value = null
      }
    }

    // Call API with metadata - the URL will be built from metadata.ApiUrl
    const result = await contentApi.getDatasetEntries(datasetName, {
      pagenumber: currentPage.value,
      pagesize: pageSize.value,
      ...params
    }, currentMetadata.value)

    // Handle different response formats
    entries.value = result.Items || result.data || []
    totalResults.value = result.TotalResults || (result.data?.length || 0)
  }

  return {
    currentMetadata,
    entries,
    loadDatasetEntries
  }
})
```

### 6. Building cURL Commands

```javascript
/**
 * Build cURL command for API request
 * @param {string} datasetName - The dataset name (not used if metadata provided)
 * @param {object} params - Query parameters
 * @param {string} method - HTTP method
 * @param {object} metadata - Metadata object with ApiUrl
 */
export function buildCurlCommand(datasetName, params = {}, method = 'GET', metadata = null) {
  // If metadata is provided, use ApiUrl
  if (metadata) {
    const fullUrl = buildFullUrlFromMetadata(metadata, params)
    if (fullUrl) {
      return `curl -X ${method} "${fullUrl}" -H "Content-Type: application/json"`
    }
  }

  // Fallback for content API
  const queryString = buildQueryString(params)
  const url = `https://tourism.opendatahub.com/v1/${datasetName}${queryString ? `?${queryString}` : ''}`
  return `curl -X ${method} "${url}" -H "Content-Type: application/json"`
}
```

## Best Practices

### 1. Use ApiUrl as Your Starting Point
Always use `metadata.ApiUrl` directly instead of building URLs from BaseUrl + PathParam. This eliminates edge cases and ensures you're using the correct URL format.

```javascript
// ✅ Good - Uses ApiUrl directly
const url = buildFullUrlFromMetadata(metadata, params)

// ❌ Bad - Manually building URL
const url = `${metadata.BaseUrl}/${metadata.PathParam.join('/')}`
```

### 2. Cache Metadata
Metadata changes infrequently. Cache it to reduce API calls.

```javascript
// Example: Simple in-memory cache
let metadataCache = null
let metadataCacheTime = null
const CACHE_DURATION = 5 * 60 * 1000 // 5 minutes

export async function getAllMetadata() {
  const now = Date.now()
  if (metadataCache && metadataCacheTime && (now - metadataCacheTime) < CACHE_DURATION) {
    return metadataCache
  }

  const metadata = await fetchMetadataFromAPI()
  metadataCache = metadata
  metadataCacheTime = now
  return metadata
}
```

### 3. Handle Missing Metadata Gracefully
Not all datasets may be in MetaData. Provide fallbacks for hardcoded datasets.

```javascript
export async function getMetadataByShortname(shortname) {
  // Hardcoded datasets
  if (shortname === 'MetaData' || shortname === 'Sensor') {
    return getHardcodedMetadata(shortname)
  }

  // Fetch from API
  const allMetadata = await getAllMetadata()
  const metadata = allMetadata.find(m => m.Shortname === shortname)

  // Fallback if not found
  if (!metadata) {
    console.warn(`Metadata not found for dataset: ${shortname}`)
    return null
  }

  return metadata
}
```

### 4. Filter Out Deprecated Datasets
Always check the `Deprecated` flag when displaying dataset lists.

```javascript
export async function getDatasetTypes() {
  const allMetadata = await getAllMetadata()

  return allMetadata
    .filter(m => !m.Deprecated)  // Filter out deprecated
    .map(m => ({
      name: m.Shortname,
      description: m.ApiDescription?.en || '',
      metadata: m
    }))
}
```

### 5. Merge Params Correctly
User params should take precedence over default params from ApiUrl.

```javascript
// URLSearchParams.set() ensures user params override defaults
const mergedParams = new URLSearchParams(url.search)  // Existing params
Object.entries(params).forEach(([key, value]) => {
  mergedParams.set(key, value)  // User params override
})
```

### 6. Fetch All Metadata Pages
The MetaData API is paginated. Always fetch all pages for complete dataset list.

```javascript
while (hasMorePages) {
  const response = await contentClient.get('/MetaData', {
    params: { pagenumber: currentPage, pagesize: 100 }
  })

  allMetadata.push(...response.data.Items)

  if (currentPage >= response.data.TotalPages) {
    hasMorePages = false
  } else {
    currentPage++
  }
}
```

### 7. Handle Different Response Formats
Content API and Timeseries API may have different response structures.

```javascript
// Handle both formats
entries.value = result.Items || result.data || []
totalResults.value = result.TotalResults || (result.data?.length || 0)
```

### 8. Use Shortname for Routing
URL parameter should match the `Shortname` field (can contain spaces).

```javascript
// Vue Router will automatically encode spaces
<router-link :to="`/datasets/${dataset.name}`">
  {{ dataset.name }}
</router-link>

// Route param will be URL-encoded: "ODH Activity Poi" → "ODH%20Activity%20Poi"
```

### 9. Maintain Backward Compatibility
Keep fallback to contentClient when metadata is not available.

```javascript
export async function getDatasetEntries(datasetName, params = {}, metadata = null) {
  // Try metadata-based approach first
  if (metadata) {
    const fullUrl = buildFullUrlFromMetadata(metadata, params)
    if (fullUrl) {
      return await genericClient.get(fullUrl)
    }
  }

  // Fallback to legacy approach
  const url = `/${datasetName}`
  return await contentClient.get(url, { params })
}
```

### 10. Error Handling
Metadata fetch failures should gracefully degrade.

```javascript
try {
  currentMetadata.value = await contentApi.getMetadataByShortname(datasetName)
} catch (metaErr) {
  console.warn('Could not fetch metadata for dataset:', datasetName, metaErr)
  currentMetadata.value = null
  // Continue with fallback approach
}
```

## References

- **MetaData API**: https://tourism.opendatahub.com/v1/MetaData
- **Content API Swagger**: https://tourism.api.opendatahub.com/swagger/
- **Timeseries API Swagger**: https://mobility.api.opendatahub.com/v2/apispec

## Appendix: Common PathParam Patterns

### Content API Patterns
```
v1/Accommodation              - Hotels, B&Bs, Apartments
v1/ODHActivityPoi             - Activities and Points of Interest
v1/Event                      - Events
v1/Article                    - Articles, News
v1/GeoShapes                  - Geographic shapes
v1/Webcam                     - Webcam data
v1/Area                       - Areas/Regions
v1/District                   - Districts
v1/Municipality               - Municipalities
```

### Timeseries API Patterns
```
v2/flat/ParkingSensor         - Parking sensor data
v2/flat/TrafficSensor         - Traffic sensor data
v2/flat/EChargingStation      - E-Charging station data
v2/flat/BicycleSensor         - Bicycle counting sensors
v2/flat/MeteoStation          - Weather station data
```

## Summary: Key Changes from Legacy Approach

The current implementation uses a **simplified ApiUrl-first approach** that eliminates complexity from the previous implementation:

### What Changed

| Aspect | Old Approach (❌) | New Approach (✅) |
|--------|------------------|------------------|
| **URL Building** | Build from `BaseUrl + PathParam` | Use `metadata.ApiUrl` directly |
| **HTTP Clients** | Multiple clients (`contentClient`, `timeseriesClient`, `mobilityTimeseriesClient`) | Single `genericClient` for all APIs |
| **Parameter Conversion** | Convert `pagenumber/pagesize` ↔ `offset/limit` | Use params as-is from user |
| **Response Normalization** | Normalize timeseries response to match content API | Handle different formats as-is |
| **API Type Handling** | Check `ApiType` and route to correct client | No routing needed - ApiUrl has full path |
| **Path Logic** | Different logic for content (last element) vs timeseries (full path) | No path logic needed - ApiUrl is complete |
| **Default Filters** | Parse `ApiFilter` array and merge manually | Already in ApiUrl query params |

### Benefits of New Approach

1. **Simpler Code**: ~70% less code in URL building logic
2. **No Edge Cases**: Works for all API types without special handling
3. **More Reliable**: Uses real, working URLs from metadata
4. **Easier to Maintain**: Single code path for all datasets
5. **Future-Proof**: New API types work automatically if metadata provides ApiUrl

### Migration Notes

If you have existing code using the old approach:

```javascript
// Old approach - DO NOT USE
const client = getClientForDataset(metadata)
const params = convertToTimeseriesParams(userParams)
const response = await client.get(endpoint, { params })
const normalized = normalizeTimeseriesResponse(response.data)

// New approach - USE THIS
const fullUrl = buildFullUrlFromMetadata(metadata, userParams)
const response = await genericClient.get(fullUrl)
// No conversion or normalization needed
```

---

**Document Version**: 2.0
**Last Updated**: 2025-10-19
**Author**: OdhDiscovery Team
**Changes**: Updated to reflect ApiUrl-first implementation approach
