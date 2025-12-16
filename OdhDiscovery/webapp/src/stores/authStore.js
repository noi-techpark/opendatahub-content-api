/**
 * Authentication Store
 * Handles JWT token-based authentication state
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import axios from 'axios'

const AUTH_TOKEN_KEY = 'odh_auth_token'
const AUTH_USER_KEY = 'odh_auth_user'

export const useAuthStore = defineStore('auth', () => {
  // State
  const token = ref(localStorage.getItem(AUTH_TOKEN_KEY) || null)
  const user = ref(JSON.parse(localStorage.getItem(AUTH_USER_KEY) || 'null'))
  const loading = ref(false)
  const error = ref(null)

  // Getters
  const isAuthenticated = computed(() => !!token.value)

  // Actions
  async function login(username, password) {
    loading.value = true
    error.value = null

    try {
      // Use URLSearchParams for OAuth2 form data format
      const formData = new URLSearchParams()
      formData.append('username', username)
      formData.append('password', password)

      const response = await axios.post('/api/chatbot/auth/login', formData, {
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded'
        }
      })

      const { access_token } = response.data

      // Store token
      token.value = access_token
      localStorage.setItem(AUTH_TOKEN_KEY, access_token)

      // Fetch user info
      await fetchUser()

      return true
    } catch (err) {
      error.value = err.response?.data?.detail || 'Login failed'
      return false
    } finally {
      loading.value = false
    }
  }

  async function fetchUser() {
    if (!token.value) return null

    try {
      const response = await axios.get('/api/chatbot/auth/me', {
        headers: {
          Authorization: `Bearer ${token.value}`
        }
      })
      user.value = response.data
      localStorage.setItem(AUTH_USER_KEY, JSON.stringify(response.data))
      return response.data
    } catch (err) {
      // Token might be invalid
      logout()
      return null
    }
  }

  async function verifyToken() {
    if (!token.value) return false

    try {
      await axios.post('/api/chatbot/auth/verify', null, {
        headers: {
          Authorization: `Bearer ${token.value}`
        }
      })
      return true
    } catch (err) {
      logout()
      return false
    }
  }

  function logout() {
    token.value = null
    user.value = null
    localStorage.removeItem(AUTH_TOKEN_KEY)
    localStorage.removeItem(AUTH_USER_KEY)
    // Also clear chatbot session
    localStorage.removeItem('odh_chatbot_session_id')
  }

  function getToken() {
    return token.value
  }

  return {
    // State
    token,
    user,
    loading,
    error,

    // Getters
    isAuthenticated,

    // Actions
    login,
    logout,
    fetchUser,
    verifyToken,
    getToken
  }
})
