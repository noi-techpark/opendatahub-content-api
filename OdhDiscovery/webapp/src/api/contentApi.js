import { contentClient } from './client'

/**
 * Available dataset types in the Content API
 */
export const DATASET_TYPES = [
  'Accommodation',
  'Activity',
  'Poi',
  'ODHActivityPoi',
  'Gastronomy',
  'Event',
  'EventShort',
  'Article',
  'Venue',
  'Weather',
  'Webcam',
  'MeasuringPoint',
  'Sensor',
  'SkiArea',
  'SkiRegion',
  'Region',
  'TourismVerein',
  'Municipality',
  'District',
  'MetaRegion',
  'ExperienceArea'
]

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
 */
export async function getDatasetEntries(datasetName, params = {}) {
  const queryString = buildQueryString(params)
  const url = `/${datasetName}${queryString ? `?${queryString}` : ''}`
  const response = await contentClient.get(url)
  return response.data
}

/**
 * Get single entry by ID
 */
export async function getDatasetEntry(datasetName, id, params = {}) {
  const queryString = buildQueryString(params)
  const url = `/${datasetName}/${id}${queryString ? `?${queryString}` : ''}`
  const response = await contentClient.get(url)
  return response.data
}

/**
 * Get dataset types (for browsing available datasets)
 */
export async function getDatasetTypes() {
  return DATASET_TYPES.map(name => ({
    name,
    endpoint: `/${name}`
  }))
}

/**
 * Get dataset metadata (total count, etc.)
 */
export async function getDatasetMetadata(datasetName) {
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
 */
export async function getAllFilteredIds(datasetName, params = {}) {
  const queryString = buildQueryString({ ...params, getasidarray: true })
  const url = `/${datasetName}${queryString ? `?${queryString}` : ''}`
  const response = await contentClient.get(url)
  // Response should be an array of IDs
  return response.data
}

/**
 * Get all entries matching the filter (fetches all pages)
 * Warning: This can be resource-intensive for large datasets
 */
export async function getAllFilteredEntries(datasetName, params = {}, progressCallback = null) {
  // First, get the total count
  const firstPageParams = { ...params, pagesize: 100, pagenumber: 1 }
  const queryString = buildQueryString(firstPageParams)
  const url = `/${datasetName}${queryString ? `?${queryString}` : ''}`
  const firstResponse = await contentClient.get(url)

  const totalResults = firstResponse.data.TotalResults || 0
  const totalPages = firstResponse.data.TotalPages || 1
  let allEntries = [...(firstResponse.data.Items || [])]

  if (progressCallback) {
    progressCallback({ current: 1, total: totalPages, entries: allEntries.length })
  }

  // Fetch remaining pages in parallel (batch of 5 at a time to avoid overwhelming the server)
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
 * Build cURL command for Content API request
 */
export function buildCurlCommand(datasetName, params = {}, method = 'GET') {
  const baseUrl = 'https://tourism.opendatahub.com'
  const queryString = buildQueryString(params)
  const url = `${baseUrl}/v1/${datasetName}${queryString ? `?${queryString}` : ''}`

  return `curl -X ${method} "${url}" -H "Content-Type: application/json"`
}
