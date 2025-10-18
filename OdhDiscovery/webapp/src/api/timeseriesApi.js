import { timeseriesClient } from './client'

/**
 * Get all timeseries types
 */
export async function getTypes(params = {}) {
  const response = await timeseriesClient.get('/types', { params })
  return response.data
}

/**
 * Get specific timeseries type by name
 */
export async function getTypeByName(name) {
  const response = await timeseriesClient.get(`/types/${name}`)
  return response.data
}

/**
 * Discover sensors (POST - recommended)
 */
export async function discoverSensors(payload) {
  const response = await timeseriesClient.post('/sensors', payload)
  return response.data
}

/**
 * Discover sensors (GET - legacy)
 */
export async function discoverSensorsLegacy(params) {
  const response = await timeseriesClient.get('/sensors/discover', { params })
  return response.data
}

/**
 * Get sensor timeseries by name
 */
export async function getSensorTimeseries(sensorName, params = {}) {
  const response = await timeseriesClient.get(`/sensors/${sensorName}`, { params })
  return response.data
}

/**
 * Get timeseries for multiple sensors (batch)
 */
export async function getTimeseriesForSensors(payload) {
  const response = await timeseriesClient.post('/sensors/timeseries', payload)
  return response.data
}

/**
 * Get types for multiple sensors
 */
export async function getTypesForSensors(payload) {
  const response = await timeseriesClient.post('/sensors/types', payload)
  return response.data
}

/**
 * Verify sensors against filters
 */
export async function verifySensors(payload) {
  const response = await timeseriesClient.post('/sensors/verify', payload)
  return response.data
}

/**
 * Get latest measurements (GET)
 */
export async function getLatestMeasurements(params) {
  const response = await timeseriesClient.get('/measurements/latest', { params })
  return response.data
}

/**
 * Get latest measurements (POST)
 */
export async function getLatestMeasurementsPost(payload) {
  const response = await timeseriesClient.post('/measurements/latest', payload)
  return response.data
}

/**
 * Get historical measurements (GET)
 */
export async function getHistoricalMeasurements(params) {
  const response = await timeseriesClient.get('/measurements/historical', { params })
  return response.data
}

/**
 * Get historical measurements (POST)
 */
export async function getHistoricalMeasurementsPost(payload) {
  const response = await timeseriesClient.post('/measurements/historical', payload)
  return response.data
}

/**
 * Build cURL command for Timeseries API request
 */
export function buildTimeseriesCurl(endpoint, method = 'GET', params = {}, body = null) {
  const baseUrl = 'http://localhost:8080'
  const url = `${baseUrl}/api/v1${endpoint}`

  if (method === 'GET') {
    const queryString = new URLSearchParams(params).toString()
    const fullUrl = queryString ? `${url}?${queryString}` : url
    return `curl -X GET "${fullUrl}" -H "Content-Type: application/json"`
  } else {
    const bodyStr = body ? ` -d '${JSON.stringify(body, null, 2)}'` : ''
    return `curl -X ${method} "${url}" -H "Content-Type: application/json"${bodyStr}`
  }
}
