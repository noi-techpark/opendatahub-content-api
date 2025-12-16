/**
 * Chatbot WebSocket Composable
 * Handles WebSocket connection, session management, and message streaming
 */
import { ref, computed, onUnmounted } from 'vue'
import { chatbotClient } from '../api/client'

const SESSION_STORAGE_KEY = 'odh_chatbot_session_id'

// Get WebSocket URL with token
const getWsUrl = () => {
  const token = localStorage.getItem('odh_auth_token')
  const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:'
  const baseUrl = `${protocol}//${window.location.host}/api/chatbot/ws`
  return token ? `${baseUrl}?token=${encodeURIComponent(token)}` : baseUrl
}

export function useChatbot() {
  const ws = ref(null)
  const messages = ref([])
  const isConnected = ref(false)
  const isLoading = ref(false)
  const currentMessage = ref('') // For streaming
  const sessionId = ref(null)
  const error = ref(null)
  const pendingNavigation = ref(null) // For auto-navigation

  // Load session from localStorage
  const loadSession = () => {
    const storedSessionId = localStorage.getItem(SESSION_STORAGE_KEY)
    if (storedSessionId) {
      sessionId.value = storedSessionId
      return storedSessionId
    }
    return null
  }

  // Save session to localStorage
  const saveSession = (id) => {
    sessionId.value = id
    localStorage.setItem(SESSION_STORAGE_KEY, id)
  }

  // Clear session from localStorage
  const clearSession = () => {
    sessionId.value = null
    localStorage.removeItem(SESSION_STORAGE_KEY)
  }

  // Retrieve old messages from backend
  const retrieveMessages = async (sessionId) => {
    try {
      const response = await chatbotClient.get(`/sessions/${sessionId}/messages`)

      // Transform backend format to UI format
      // Navigation commands ARE included but won't trigger auto-navigation
      const retrievedMessages = response.data.messages.map(msg => ({
        role: msg.role,
        content: msg.content,
        timestamp: msg.timestamp || new Date().toISOString(),
        navigationCommands: msg.navigationCommands || [] // Include historical navigation commands
      }))

      messages.value = retrievedMessages
      console.log(`Retrieved ${retrievedMessages.length} messages from session ${sessionId}`)
      return retrievedMessages
    } catch (err) {
      console.error('Failed to retrieve messages:', err)
      // If session not found (404) or unauthorized (401), clear it
      if (err.response?.status === 404 || err.response?.status === 401) {
        clearSession()
      }
      return []
    }
  }

  // Connect to WebSocket
  const connect = async () => {
    if (ws.value?.readyState === WebSocket.OPEN) {
      return // Already connected
    }

    return new Promise((resolve, reject) => {
      try {
        const wsUrl = getWsUrl()
        ws.value = new WebSocket(wsUrl)

        ws.value.onopen = async () => {
          console.log('WebSocket connected')
          isConnected.value = true
          error.value = null

          // Load session and retrieve old messages if exists
          const storedSessionId = loadSession()
          if (storedSessionId) {
            console.log('Loading session:', storedSessionId)
            await retrieveMessages(storedSessionId)
          }

          resolve()
        }

        ws.value.onmessage = (event) => {
          try {
            const data = JSON.parse(event.data)
            handleMessage(data)
          } catch (err) {
            console.error('Failed to parse WebSocket message:', err)
          }
        }

        ws.value.onerror = (event) => {
          console.error('WebSocket error:', event)
          error.value = 'Connection error'
          isConnected.value = false
          reject(event)
        }

        ws.value.onclose = (event) => {
          console.log('WebSocket disconnected', event.code, event.reason)
          isConnected.value = false

          // Handle authentication errors
          if (event.code === 4001) {
            error.value = 'Authentication failed'
            // Clear token and redirect to login
            localStorage.removeItem('odh_auth_token')
            localStorage.removeItem('odh_auth_user')
            window.location.href = '/login'
          }
        }
      } catch (err) {
        console.error('Failed to create WebSocket:', err)
        error.value = 'Failed to connect'
        reject(err)
      }
    })
  }

  // Handle incoming WebSocket messages
  const handleMessage = (data) => {
    console.log("handleMessage", data)
    switch (data.type) {
      case 'session':
        // Session ID received
        saveSession(data.session_id)
        console.log('Session ID:', data.session_id)
        break

      case 'status':
        // Status update (e.g., "Processing your question...")
        if (data.content === 'Processing your question...') {
          isLoading.value = true
          currentMessage.value = ''
        } else if (data.content === 'Done') {
          isLoading.value = false
        }
        break

      case 'chunk':
        // Streaming response chunk
        if (!data.done) {
          currentMessage.value += data.content
        } else {
          // Final chunk - commit message
          if (currentMessage.value.trim()) {
            messages.value.push({
              role: 'assistant',
              content: currentMessage.value.trim(),
              timestamp: new Date().toISOString(),
              navigationCommands: []
            })
            currentMessage.value = ''
          }
        }
        break

      case 'navigation':
        // Navigation command received (real-time, not from history)
        // Add to last assistant message
        const lastMessage = messages.value[messages.value.length - 1]
        if (lastMessage && lastMessage.role === 'assistant') {
          if (!lastMessage.navigationCommands) {
            lastMessage.navigationCommands = []
          }
          lastMessage.navigationCommands.push(data.data)

          // Store for auto-navigation (component will handle based on toggle)
          pendingNavigation.value = data.data
        }
        break

      case 'error':
        // Error message
        error.value = data.content
        isLoading.value = false
        messages.value.push({
          role: 'assistant',
          content: `Error: ${data.content}`,
          timestamp: new Date().toISOString(),
          isError: true
        })
        break

      case 'pong':
        // Keepalive response
        break

      default:
        console.warn('Unknown message type:', data.type)
    }
  }

  // Send a message to the chatbot
  const sendMessage = (content) => {
    if (!ws.value || ws.value.readyState !== WebSocket.OPEN) {
      error.value = 'Not connected to chatbot'
      return
    }

    if (!content.trim()) {
      return
    }

    // Add user message to UI
    messages.value.push({
      role: 'user',
      content: content.trim(),
      timestamp: new Date().toISOString()
    })

    // Send to backend
    ws.value.send(JSON.stringify({
      type: 'query',
      content: content.trim(),
      session_id: sessionId.value
    }))

    isLoading.value = true
  }

  // Start a new chat (clear session)
  const newChat = async () => {
    // Clear local state
    messages.value = []
    currentMessage.value = ''
    error.value = null

    // Clear backend session if exists
    if (sessionId.value) {
      try {
        await chatbotClient.post(`/sessions/${sessionId.value}/clear`)
      } catch (err) {
        console.error('Failed to clear session:', err)
      }
    }

    // Clear localStorage
    clearSession()
  }

  // Disconnect WebSocket
  const disconnect = () => {
    if (ws.value) {
      ws.value.close()
      ws.value = null
    }
    isConnected.value = false
  }

  // Auto-disconnect on unmount
  onUnmounted(() => {
    disconnect()
  })

  // Computed: Is currently streaming?
  const isStreaming = computed(() => currentMessage.value.length > 0)

  return {
    // State
    messages,
    isConnected,
    isLoading,
    isStreaming,
    currentMessage,
    sessionId,
    error,
    pendingNavigation,

    // Methods
    connect,
    disconnect,
    sendMessage,
    newChat,
    retrieveMessages
  }
}
