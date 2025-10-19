import { contentClient, timeseriesClient, genericClient } from './client'

// Cache for metadata to avoid repeated API calls
let metadataCache = null
let metadataCacheTime = null
const CACHE_DURATION = 5 * 60 * 1000 // 5 minutes

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

/**
 * Build query string from parameters object
 */
function buildQueryString(params) {
  const cleanParams = Object.entries(params)
    .filter(([_, value]) => value !== null && value !== undefined && value !== '')
    .reduce((acc, [key, value]) => ({ ...acc, [key]: value }), {})

  return new URLSearchParams(cleanParams).toString()
}

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
 * Fetch all metadata entries from the MetaData endpoint
 */
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

/**
 * Get metadata entry by Shortname
 */
export async function getMetadataByShortname(shortname) {
  // Special case for hardcoded datasets
  if (shortname === 'MetaData') {
    return {
      Shortname: 'MetaData',
      PathParam: ['v1', 'MetaData'],
      ApiFilter: [],
      ApiDescription: { en: 'Metadata about all available datasets in the Content API' }
    }
  }

  if (shortname === 'Sensor') {
    return {
      Shortname: 'Sensor',
      PathParam: ['v1', 'Sensor'],
      ApiFilter: [],
      ApiDescription: { en: 'Sensor data from the Open Data Hub' }
    }
  }

  // Fetch all metadata and find by Shortname
  const allMetadata = await getAllMetadata()
  return allMetadata.find(m => m.Shortname === shortname)
}

/**
 * Build dataset endpoint identifier from metadata
 * Returns the last element of PathParam (used for routing/display only)
 * @param {object} metadata - Metadata object
 * @returns {string} - Endpoint identifier (e.g., "Bicycle", "ODHActivityPoi")
 */
export function buildDatasetEndpoint(metadata) {
  if (!metadata || !metadata.PathParam) {
    return null
  }

  // Return the last element of PathParam as the identifier
  const pathParam = metadata.PathParam
  return pathParam[pathParam.length - 1]
}

/**
 * Get dataset types (for browsing available datasets)
 * Now dynamically fetched from MetaData API - includes both content and timeseries
 */
export async function getDatasetTypes() {
  const allMetadata = await getAllMetadata()

  // Convert metadata to dataset type format
  // Include ALL API types (content AND timeseries)
  const datasets = allMetadata
    .filter(m => !m.Deprecated) // Filter out deprecated datasets
    .filter(m => m.ApiType === 'content' || m.ApiType === 'timeseries') // Include both content and timeseries
    .map(m => ({
      name: m.Shortname,
      endpoint: buildDatasetEndpoint(m),
      description: m.ApiDescription?.en || '',
      dataspace: m.Dataspace,
      apiFilter: m.ApiFilter || [],
      apiType: m.ApiType,
      metadata: m // Keep full metadata for later use
    }))

  // Add hardcoded datasets that are not in MetaData
  datasets.push({
    name: 'MetaData',
    endpoint: 'MetaData',
    description: 'Metadata about all available datasets in the Content API',
    dataspace: 'system',
    apiFilter: [],
    apiType: 'content',
    metadata: {
      Shortname: 'MetaData',
      PathParam: ['v1', 'MetaData'],
      ApiFilter: [],
      ApiType: 'content',
      BaseUrl: 'https://tourism.opendatahub.com'
    }
  })

  datasets.push({
    name: 'Sensor',
    endpoint: 'Sensor',
    description: 'Sensor data from the Open Data Hub',
    dataspace: 'mobility',
    apiFilter: [],
    apiType: 'content',
    metadata: {
      Shortname: 'Sensor',
      PathParam: ['v1', 'Sensor'],
      ApiFilter: [],
      ApiType: 'content',
      BaseUrl: 'https://tourism.opendatahub.com'
    }
  })

  // Sort by name
  return datasets.sort((a, b) => a.name.localeCompare(b.name))
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

/**
 * Get all IDs matching the current filter (uses getasidarray=true)
 * @param {string} datasetName - The dataset name (not used if metadata provided)
 * @param {object} params - Query parameters
 * @param {object} metadata - Metadata object with ApiUrl
 */
export async function getAllFilteredIds(datasetName, params = {}, metadata = null) {
  // If metadata is provided, use ApiUrl
  if (metadata) {
    const fullUrl = buildFullUrlFromMetadata(metadata, { ...params, getasidarray: true })
    if (fullUrl) {
      const response = await genericClient.get(fullUrl)
      return response.data
    }
  }

  // Fallback to contentClient
  const queryString = buildQueryString({ ...params, getasidarray: true })
  const url = `/${datasetName}${queryString ? `?${queryString}` : ''}`
  const response = await contentClient.get(url)
  return response.data
}

/**
 * Get all entries matching the filter (fetches all pages)
 * Warning: This can be resource-intensive for large datasets
 * @param {string} datasetName - The dataset name (not used if metadata provided)
 * @param {object} params - Query parameters
 * @param {function} progressCallback - Optional callback for progress updates
 * @param {object} metadata - Metadata object with ApiUrl
 */
export async function getAllFilteredEntries(datasetName, params = {}, progressCallback = null, metadata = null) {
  // If metadata is provided, use ApiUrl
  if (metadata) {
    const firstPageParams = { ...params, pagesize: 100, pagenumber: 1 }
    const fullUrl = buildFullUrlFromMetadata(metadata, firstPageParams)

    if (fullUrl) {
      const firstResponse = await genericClient.get(fullUrl)
      const data = firstResponse.data

      // Handle different response formats
      const items = data.Items || data.data || []
      const totalPages = data.TotalPages || 1
      let allEntries = [...items]

      if (progressCallback) {
        progressCallback({ current: 1, total: totalPages, entries: allEntries.length })
      }

      // Fetch remaining pages
      const batchSize = 5
      for (let i = 2; i <= totalPages; i += batchSize) {
        const batch = []
        for (let j = i; j < i + batchSize && j <= totalPages; j++) {
          const pageParams = { ...params, pagesize: 100, pagenumber: j }
          const pageUrl = buildFullUrlFromMetadata(metadata, pageParams)
          batch.push(genericClient.get(pageUrl))
        }

        const responses = await Promise.all(batch)
        responses.forEach(response => {
          const pageItems = response.data.Items || response.data.data || []
          allEntries = allEntries.concat(pageItems)
        })

        if (progressCallback) {
          progressCallback({ current: Math.min(i + batchSize - 1, totalPages), total: totalPages, entries: allEntries.length })
        }
      }

      return allEntries
    }
  }

  // Fallback to contentClient
  const firstPageParams = { ...params, pagesize: 100, pagenumber: 1 }
  const queryString = buildQueryString(firstPageParams)
  const url = `/${datasetName}${queryString ? `?${queryString}` : ''}`
  const firstResponse = await contentClient.get(url)

  const totalPages = firstResponse.data.TotalPages || 1
  let allEntries = [...(firstResponse.data.Items || [])]

  if (progressCallback) {
    progressCallback({ current: 1, total: totalPages, entries: allEntries.length })
  }

  const batchSize = 5
  for (let i = 2; i <= totalPages; i += batchSize) {
    const batch = []
    for (let j = i; j < i + batchSize && j <= totalPages; j++) {
      const pageParams = { ...params, pagesize: 100, pagenumber: j }
      const pageQueryString = buildQueryString(pageParams)
      const pageUrl = `/${datasetName}${pageQueryString ? `?${pageQueryString}` : ''}`
      batch.push(contentClient.get(pageUrl))
    }

    const responses = await Promise.all(batch)
    responses.forEach(response => {
      allEntries = allEntries.concat(response.data.Items || [])
    })

    if (progressCallback) {
      progressCallback({ current: Math.min(i + batchSize - 1, totalPages), total: totalPages, entries: allEntries.length })
    }
  }

  return allEntries
}

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
