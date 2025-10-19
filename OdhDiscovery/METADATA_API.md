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

### Step-by-Step Process

1. **Determine the Base URL**
   - Use `BaseUrl` field from metadata
   - Examples:
     - Content API: `https://tourism.api.opendatahub.com`
     - Timeseries API (v2): `https://mobility.api.opendatahub.com`

2. **Build the Path** (⚠️ **Different for Content vs Timeseries**)

   **For Content API (v1):**
   - PathParam: `["v1", "ODHActivityPoi"]`
   - Use only the **last element**: `"ODHActivityPoi"`
   - Proxy adds `/v1/` prefix automatically
   - Final path: `/api/v1/content/ODHActivityPoi` → proxied to `/v1/ODHActivityPoi`

   **For Timeseries API (v2):**
   - PathParam: `["v2", "flat", "Bicycle"]`
   - Use the **full path**: `"/v2/flat/Bicycle"`
   - Proxy routes to mobility API
   - Final path: `/api/v2/timeseries/v2/flat/Bicycle` → proxied to `/v2/flat/Bicycle`

3. **Handle Query Parameters** (⚠️ **Different conventions**)

   **Content API uses:**
   - `pagenumber`: Page number (1-based)
   - `pagesize`: Number of items per page
   - Example: `?pagenumber=1&pagesize=50`

   **Timeseries API (v2) uses:**
   - `offset`: Starting index (0-based)
   - `limit`: Number of items to return
   - Example: `?offset=0&limit=50`

   **Conversion:**
   ```javascript
   offset = (pagenumber - 1) * pagesize
   limit = pagesize
   ```

4. **Apply Default Filters from ApiFilter**
   - Parse `ApiFilter` array entries (format: `"key=value"`)
   - Add to query parameters
   - Example: `["tagfilter=museums", "source=lts"]`

5. **Add User Parameters**
   - Filters: `searchfilter`, `rawfilter` (Content API only)
   - Other dataset-specific parameters

6. **Construct Final URL**
   - Combine base URL, path, and query parameters
   - Content API: `https://tourism.api.opendatahub.com/v1/ODHActivityPoi?tagfilter=museums&pagenumber=1&pagesize=10`
   - Timeseries API: `https://mobility.api.opendatahub.com/v2/flat/Bicycle?limit=25&offset=0`

### Code Example (JavaScript)

```javascript
/**
 * Build complete API URL from metadata
 */
function buildUrlFromMetadata(metadata, userParams = {}) {
  // 1. Get base URL
  const baseUrl = metadata.BaseUrl

  // 2. Build path from PathParam
  const path = metadata.PathParam.join('/')

  // 3. Parse ApiFilter into parameters
  const filterParams = {}
  if (metadata.ApiFilter && metadata.ApiFilter.length > 0) {
    metadata.ApiFilter.forEach(filter => {
      const [key, value] = filter.split('=')
      if (key && value) {
        filterParams[key] = decodeURIComponent(value)
      }
    })
  }

  // 4. Merge with user parameters (user params take precedence)
  const allParams = { ...filterParams, ...userParams }

  // 5. Build query string
  const queryString = new URLSearchParams(allParams).toString()

  // 6. Construct final URL
  return `${baseUrl}/${path}${queryString ? '?' + queryString : ''}`
}

// Usage example
const metadata = {
  BaseUrl: "https://tourism.api.opendatahub.com",
  PathParam: ["v1", "ODHActivityPoi"],
  ApiFilter: ["tagfilter=museums"]
}

const url = buildUrlFromMetadata(metadata, {
  pagenumber: 1,
  pagesize: 50
})
// Result: https://tourism.api.opendatahub.com/v1/ODHActivityPoi?tagfilter=museums&pagenumber=1&pagesize=50
```

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

### 1. Fetching All Metadata

```javascript
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
      ApiType: 'content'
    }
  }

  if (shortname === 'Sensor') {
    return {
      Shortname: 'Sensor',
      PathParam: ['v1', 'Sensor'],
      ApiFilter: [],
      ApiType: 'content'
    }
  }

  // Fetch from API
  const allMetadata = await getAllMetadata()
  return allMetadata.find(m => m.Shortname === shortname)
}
```

### 3. Building Endpoint from Metadata (⚠️ API-Aware)

```javascript
export function buildDatasetEndpoint(metadata) {
  if (!metadata || !metadata.PathParam) {
    return null
  }

  const pathParam = metadata.PathParam
  const baseUrl = metadata.BaseUrl || ''
  const apiType = metadata.ApiType || 'content'

  // For mobility timeseries API (v2), build full path
  if (baseUrl.includes('mobility.api.opendatahub.com') && pathParam[0] === 'v2') {
    // PathParam: ["v2", "flat", "Bicycle"]
    // Return: "/v2/flat/Bicycle"
    return '/' + pathParam.join('/')
  }

  // For timeseries API (v1), build full path
  if (apiType === 'timeseries' && pathParam[0] === 'v1') {
    return '/' + pathParam.join('/')
  }

  // For content API, only use the last element
  // PathParam: ["v1", "ODHActivityPoi"]
  // Return: "ODHActivityPoi"
  return pathParam[pathParam.length - 1]
}
```

### 4. Selecting the Correct API Client (⚠️ Multi-Client Support)

```javascript
import { contentClient, timeseriesClient, mobilityTimeseriesClient } from './client'

/**
 * Three clients for three different APIs:
 * - contentClient: /api/v1/content → https://tourism.api.opendatahub.com/v1
 * - timeseriesClient: /api/v1/timeseries → http://localhost:8080/api/v1 (local)
 * - mobilityTimeseriesClient: /api/v2/timeseries → https://mobility.api.opendatahub.com
 */
function getClientForDataset(metadata) {
  if (!metadata) {
    return contentClient
  }

  const baseUrl = metadata.BaseUrl || metadata.metadata?.BaseUrl || ''
  const pathParam = metadata.PathParam || metadata.metadata?.PathParam || []

  // Check if it's a mobility timeseries API (v2)
  if (baseUrl.includes('mobility.api.opendatahub.com') && pathParam[0] === 'v2') {
    return mobilityTimeseriesClient
  }

  // Check if it's a local timeseries API (v1)
  const apiType = metadata.ApiType || metadata.metadata?.ApiType || 'content'
  if (apiType === 'timeseries' && pathParam[0] === 'v1') {
    return timeseriesClient
  }

  // Default to content client
  return contentClient
}
```

### 5. Making API Calls with Metadata (⚠️ Parameter Conversion & Response Normalization)

```javascript
/**
 * Convert Content API params to Timeseries API params
 */
function convertToTimeseriesParams(params) {
  const timeseriesParams = { ...params }

  // pagesize → limit
  if (params.pagesize) {
    timeseriesParams.limit = params.pagesize
    delete timeseriesParams.pagesize
  }

  // pagenumber → offset
  if (params.pagenumber) {
    const pagesize = params.pagesize || 50
    timeseriesParams.offset = (params.pagenumber - 1) * pagesize
    delete timeseriesParams.pagenumber
  }

  return timeseriesParams
}

/**
 * Normalize timeseries response to match content API structure
 */
function normalizeTimeseriesResponse(response, params) {
  const limit = params.limit || 50
  const offset = params.offset || 0
  const data = response.data || []
  const currentPage = Math.floor(offset / limit) + 1

  return {
    Items: data,                                    // data → Items
    TotalResults: data.length,                      // Can't know total
    TotalPages: currentPage,                        // Can't calculate without total
    CurrentPage: currentPage,
    PreviousPage: currentPage > 1 ? currentPage - 1 : null,
    NextPage: data.length === limit ? currentPage + 1 : null
  }
}

export async function getDatasetEntries(datasetName, params = {}, metadata = null) {
  // Merge ApiFilter from metadata if available
  let mergedParams = { ...params }
  if (metadata && metadata.ApiFilter && metadata.ApiFilter.length > 0) {
    metadata.ApiFilter.forEach(filter => {
      const [key, value] = filter.split('=')
      if (key && value && !mergedParams[key]) {
        mergedParams[key] = decodeURIComponent(value)
      }
    })
  }

  // Use the appropriate client based on metadata
  const client = getClientForDataset(metadata)
  const isTimeseries = client === mobilityTimeseriesClient || client === timeseriesClient

  // Convert params for timeseries API
  if (isTimeseries) {
    mergedParams = convertToTimeseriesParams(mergedParams)
  }

  const queryString = buildQueryString(mergedParams)
  const url = `${datasetName}${queryString ? `?${queryString}` : ''}`
  const response = await client.get(url)

  // Normalize timeseries response to match content API structure
  if (isTimeseries) {
    return normalizeTimeseriesResponse(response.data, mergedParams)
  }

  return response.data
}
```

## Best Practices

1. **Cache Metadata**: Metadata changes infrequently. Cache it for 5-10 minutes to reduce API calls.

2. **Handle Missing Metadata Gracefully**: Not all datasets may be in MetaData. Provide fallbacks.

3. **Respect ApiFilter**: Always apply default filters from `ApiFilter` before user filters.

4. **Check Deprecated Flag**: Filter out deprecated datasets in production UIs.

5. **Use Correct Client**: Always check `ApiType` to use the right API client (content vs timeseries).

6. **Pagination**: MetaData API is paginated. Always fetch all pages for complete dataset list.

7. **Error Handling**: Metadata fetch failures should gracefully degrade to known datasets.

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

---

**Document Version**: 1.0
**Last Updated**: 2025-10-19
**Author**: OdhDiscovery Team
