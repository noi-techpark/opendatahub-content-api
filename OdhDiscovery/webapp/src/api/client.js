import axios from 'axios'

// Base axios instance for Content API (v1)
export const contentClient = axios.create({
  baseURL: '/api/v1/content',
  headers: {
    'Content-Type': 'application/json'
  }
})

// Base axios instance for Timeseries API (v1 - local)
export const timeseriesClient = axios.create({
  baseURL: '/api/v1/timeseries',
  headers: {
    'Content-Type': 'application/json'
  }
})

// Generic axios instance without baseURL - uses full URLs from metadata
export const genericClient = axios.create({
  headers: {
    'Content-Type': 'application/json'
  }
})

// Chatbot API client (requires authentication)
export const chatbotClient = axios.create({
  baseURL: '/api/chatbot',
  headers: {
    'Content-Type': 'application/json'
  }
})

// Auth token helper
const getAuthToken = () => {
  return localStorage.getItem('odh_auth_token')
}

// Request interceptor for logging
const requestInterceptor = (config) => {
  console.log(`API Request: ${config.method.toUpperCase()} ${config.url}`, config)
  return config
}

// Auth request interceptor - adds Bearer token
const authRequestInterceptor = (config) => {
  const token = getAuthToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  console.log(`API Request: ${config.method.toUpperCase()} ${config.url}`, config)
  return config
}

// Response interceptor for error handling
const responseErrorInterceptor = (error) => {
  console.error('API Error:', error.response || error)
  return Promise.reject(error)
}

// Auth response interceptor - handles 401 errors
const authResponseErrorInterceptor = (error) => {
  console.error('API Error:', error.response || error)

  // If 401, redirect to login
  if (error.response?.status === 401) {
    localStorage.removeItem('odh_auth_token')
    localStorage.removeItem('odh_auth_user')
    // Use window.location to force full page reload to login
    if (window.location.pathname !== '/login') {
      window.location.href = '/login'
    }
  }

  return Promise.reject(error)
}

// Apply interceptors
contentClient.interceptors.request.use(requestInterceptor)
contentClient.interceptors.response.use(null, responseErrorInterceptor)

timeseriesClient.interceptors.request.use(requestInterceptor)
timeseriesClient.interceptors.response.use(null, responseErrorInterceptor)

genericClient.interceptors.request.use(requestInterceptor)
genericClient.interceptors.response.use(null, responseErrorInterceptor)

// Chatbot client needs auth
chatbotClient.interceptors.request.use(authRequestInterceptor)
chatbotClient.interceptors.response.use(null, authResponseErrorInterceptor)
