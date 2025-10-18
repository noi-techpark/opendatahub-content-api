import axios from 'axios'

// Base axios instance for Content API
export const contentClient = axios.create({
  baseURL: '/api/v1/content',
  headers: {
    'Content-Type': 'application/json'
  }
})

// Base axios instance for Timeseries API
export const timeseriesClient = axios.create({
  baseURL: '/api/v1/timeseries',
  headers: {
    'Content-Type': 'application/json'
  }
})

// Request interceptor for logging
const requestInterceptor = (config) => {
  console.log(`API Request: ${config.method.toUpperCase()} ${config.url}`, config)
  return config
}

// Response interceptor for error handling
const responseErrorInterceptor = (error) => {
  console.error('API Error:', error.response || error)
  return Promise.reject(error)
}

contentClient.interceptors.request.use(requestInterceptor)
contentClient.interceptors.response.use(null, responseErrorInterceptor)

timeseriesClient.interceptors.request.use(requestInterceptor)
timeseriesClient.interceptors.response.use(null, responseErrorInterceptor)
