<template>
  <div class="chatbot-container">
    <!-- Floating Button Activator -->
    <button
      v-if="!isOpen"
      class="chatbot-toggle"
      @click="toggleChat"
      :title="isConnected ? 'Open ODH Assistant' : 'Connect to ODH Assistant'"
    >
      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"></path>
      </svg>
      <span v-if="!isConnected" class="connection-indicator offline"></span>
      <span v-else class="connection-indicator online"></span>
    </button>

    <!-- Chat Window -->
    <div
      v-if="isOpen"
      class="chatbot-window"
      :style="windowStyle"
      ref="chatWindow"
    >
      <!-- Header -->
      <div class="chatbot-header" @mousedown="startDrag">
        <div class="header-left">
          <div class="bot-avatar">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="3" y="11" width="18" height="10" rx="2"></rect>
              <circle cx="12" cy="5" r="2"></circle>
              <path d="M12 7v4"></path>
              <line x1="8" y1="16" x2="8" y2="16"></line>
              <line x1="16" y1="16" x2="16" y2="16"></line>
            </svg>
          </div>
          <div class="header-info">
            <h3>ODH Assistant</h3>
            <p class="status">
              <span v-if="isStreaming" class="typing-indicator">Typing...</span>
              <span v-else-if="isLoading">Processing...</span>
              <span v-else-if="isConnected" class="online-text">Online</span>
              <span v-else class="offline-text">Connecting...</span>
            </p>
          </div>
        </div>
        <div class="header-actions">
          <button
            class="header-btn"
            @click="handleNewChat"
            title="Start New Chat"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="12" y1="5" x2="12" y2="19"></line>
              <line x1="5" y1="12" x2="19" y2="12"></line>
            </svg>
          </button>
          <button
            class="header-btn"
            @click="toggleChat"
            title="Minimize"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="5" y1="12" x2="19" y2="12"></line>
            </svg>
          </button>
        </div>
      </div>

      <!-- Settings Bar -->
      <div class="settings-bar">
        <label class="toggle-setting">
          <input
            type="checkbox"
            v-model="autoNavigate"
            @change="saveSettings"
          />
          <span class="toggle-label">Auto-navigate</span>
          <span class="toggle-hint">(automatically navigate to suggested pages)</span>
        </label>
      </div>

      <!-- Messages -->
      <div class="chatbot-messages" ref="messagesContainer">
        <div v-if="messages.length === 0 && !isLoading" class="welcome-message">
          <div class="welcome-icon">👋</div>
          <h4>Welcome to ODH Assistant!</h4>
          <p>Ask me anything about Open Data Hub datasets, timeseries, sensors, or types.</p>
          <div class="example-questions">
            <p class="example-label">Try asking:</p>
            <button class="example-btn" @click="sendExample('List all available datasets')">
              "List all available datasets"
            </button>
            <button class="example-btn" @click="sendExample('Show me types with sensors')">
              "Show me types with sensors"
            </button>
            <button class="example-btn" @click="sendExample('What timeseries data is available?')">
              "What timeseries data is available?"
            </button>
          </div>
        </div>

        <div
          v-for="(message, index) in messages"
          :key="index"
          class="message"
          :class="`message-${message.role}`"
        >
          <div class="message-avatar" v-if="message.role === 'assistant'">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="3" y="11" width="18" height="10" rx="2"></rect>
              <circle cx="12" cy="5" r="2"></circle>
              <path d="M12 7v4"></path>
            </svg>
          </div>
          <div class="message-content" :class="{ 'error-message': message.isError }">
            <div class="message-text" v-html="renderMarkdown(message.content)"></div>

            <!-- Navigation Commands -->
            <div
              v-if="message.navigationCommands && message.navigationCommands.length > 0"
              class="navigation-commands"
            >
              <a
                v-for="(nav, navIndex) in message.navigationCommands"
                :key="navIndex"
                :href="buildNavigationUrl(nav)"
                @click.prevent="handleNavigation(nav)"
                class="nav-link"
              >
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"></path>
                  <polyline points="15 3 21 3 21 9"></polyline>
                  <line x1="10" y1="14" x2="21" y2="3"></line>
                </svg>
                See more
              </a>
            </div>

            <div class="message-time">{{ formatTime(message.timestamp) }}</div>
          </div>
        </div>

        <!-- Streaming Message -->
        <div v-if="isStreaming" class="message message-assistant streaming">
          <div class="message-avatar">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="3" y="11" width="18" height="10" rx="2"></rect>
              <circle cx="12" cy="5" r="2"></circle>
              <path d="M12 7v4"></path>
            </svg>
          </div>
          <div class="message-content">
            <div class="message-text" v-html="renderMarkdown(currentMessage)"></div>
            <span class="cursor">▊</span>
          </div>
        </div>

        <!-- Loading Indicator -->
        <div v-if="isLoading && !isStreaming" class="message message-assistant">
          <div class="message-avatar">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="3" y="11" width="18" height="10" rx="2"></rect>
              <circle cx="12" cy="5" r="2"></circle>
              <path d="M12 7v4"></path>
            </svg>
          </div>
          <div class="message-content">
            <div class="typing-dots">
              <span></span>
              <span></span>
              <span></span>
            </div>
          </div>
        </div>

        <!-- Error Display -->
        <div v-if="error" class="error-banner">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="8" x2="12" y2="12"></line>
            <line x1="12" y1="16" x2="12.01" y2="16"></line>
          </svg>
          {{ error }}
        </div>
      </div>

      <!-- Input Area -->
      <div class="chatbot-input">
        <textarea
          v-model="inputMessage"
          @keydown.enter.prevent="handleSendMessage"
          placeholder="Ask me anything about ODH data..."
          rows="1"
          ref="inputField"
          :disabled="!isConnected || isLoading"
        ></textarea>
        <button
          class="send-btn"
          @click="handleSendMessage"
          :disabled="!isConnected || isLoading || !inputMessage.trim()"
          title="Send message"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="22" y1="2" x2="11" y2="13"></line>
            <polygon points="22 2 15 22 11 13 2 9 22 2"></polygon>
          </svg>
        </button>
      </div>

      <!-- Resize Handle -->
      <div class="resize-handle" @mousedown="startResize"></div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, nextTick, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useChatbot } from '../composables/useChatbot'
import { marked } from 'marked'

// Configure marked for safer HTML rendering
marked.setOptions({
  breaks: true, // Convert \n to <br>
  gfm: true, // GitHub Flavored Markdown
  headerIds: false, // Don't add IDs to headers
  mangle: false // Don't mangle email addresses
})

const router = useRouter()

// Chatbot state
const {
  messages,
  isConnected,
  isLoading,
  isStreaming,
  currentMessage,
  sessionId,
  error,
  pendingNavigation,
  connect,
  sendMessage,
  newChat
} = useChatbot()

// UI state
const isOpen = ref(false)
const inputMessage = ref('')
const autoNavigate = ref(true)
const isLoadingHistory = ref(false)

// Window dimensions
const windowWidth = ref(400)
const windowHeight = ref(600)
const windowStyle = ref({
  width: '400px',
  height: '600px'
})

// Refs
const messagesContainer = ref(null)
const chatWindow = ref(null)
const inputField = ref(null)

// Drag state
const isDragging = ref(false)
const dragStartX = ref(0)
const dragStartY = ref(0)
const windowX = ref(0)
const windowY = ref(0)

// Resize state
const isResizing = ref(false)
const resizeStartX = ref(0)
const resizeStartY = ref(0)
const resizeStartWidth = ref(0)
const resizeStartHeight = ref(0)

// Load settings from localStorage
const loadSettings = () => {
  const settings = localStorage.getItem('odh_chatbot_settings')
  if (settings) {
    try {
      const parsed = JSON.parse(settings)
      autoNavigate.value = parsed.autoNavigate ?? true
      windowWidth.value = parsed.windowWidth ?? 400
      windowHeight.value = parsed.windowHeight ?? 600
      updateWindowStyle()
    } catch (err) {
      console.error('Failed to load settings:', err)
    }
  }
}

// Save settings to localStorage
const saveSettings = () => {
  const settings = {
    autoNavigate: autoNavigate.value,
    windowWidth: windowWidth.value,
    windowHeight: windowHeight.value
  }
  localStorage.setItem('odh_chatbot_settings', JSON.stringify(settings))
}

// Update window style
const updateWindowStyle = () => {
  windowStyle.value = {
    width: `${windowWidth.value}px`,
    height: `${windowHeight.value}px`
  }
}

// Toggle chat window
const toggleChat = async () => {
  isOpen.value = !isOpen.value

  if (isOpen.value && !isConnected.value) {
    await connect()
  }

  if (isOpen.value) {
    await nextTick()
    scrollToBottom()
    inputField.value?.focus()
  }
}

// Send message
const handleSendMessage = () => {
  if (!inputMessage.value.trim() || !isConnected.value || isLoading.value) {
    return
  }

  sendMessage(inputMessage.value)
  inputMessage.value = ''

  nextTick(() => {
    scrollToBottom()
  })
}

// Send example question
const sendExample = (question) => {
  inputMessage.value = question
  handleSendMessage()
}

// Start new chat
const handleNewChat = async () => {
  if (confirm('Start a new chat? This will clear the current conversation.')) {
    await newChat()
    inputMessage.value = ''
  }
}

// Handle navigation command (manual click on "See more")
const handleNavigation = (navCommand) => {
  console.log('Navigation command (manual click):', navCommand)

  // Build route
  const route = {
    name: navCommand.route,
    params: {},
    query: {}
  }

  // Parse params
  if (navCommand.params) {
    for (const [key, value] of Object.entries(navCommand.params)) {
      // Route params (like :datasetName, :typeName)
      if (key === 'datasetName' || key === 'typeName') {
        route.params[key] = value
      } else {
        // Query params
        route.query[key] = value
      }
    }
  }

  console.log('Navigating to:', route)

  // Navigate to route (manual click - always navigate)
  router.push(route)

  // Minimize chat after navigation
  // isOpen.value = false
}

// Build navigation URL for display
const buildNavigationUrl = (navCommand) => {
  let url = '/'

  switch (navCommand.route) {
    case 'DatasetBrowser':
      url = '/datasets'
      break
    case 'DatasetInspector':
      url = `/datasets/${navCommand.params?.datasetName || ''}`
      break
    case 'TimeseriesBrowser':
      url = '/timeseries'
      break
    case 'TimeseriesInspector':
      url = `/timeseries/${navCommand.params?.typeName || ''}`
      break
    case 'BulkMeasurementsInspector':
      url = '/bulk-measurements'
      break
  }

  // Add query params
  if (navCommand.params) {
    const queryParams = new URLSearchParams()
    for (const [key, value] of Object.entries(navCommand.params)) {
      if (key !== 'datasetName' && key !== 'typeName') {
        if (Array.isArray(value)) {
          queryParams.append(key, JSON.stringify(value))
        } else {
          queryParams.append(key, value)
        }
      }
    }
    const queryString = queryParams.toString()
    if (queryString) {
      url += '?' + queryString
    }
  }

  return url
}

// Format navigation params for display
const formatNavParams = (params) => {
  const entries = Object.entries(params)
  if (entries.length === 0) return ''

  const formatted = entries
    .slice(0, 2) // Show first 2 params
    .map(([key, value]) => {
      if (Array.isArray(value)) {
        return `${key}: [${value.length} items]`
      }
      return `${key}: ${value}`
    })
    .join(', ')

  return entries.length > 2 ? `${formatted}...` : formatted
}

// Render markdown content
const renderMarkdown = (content) => {
  if (!content) return ''

  try {
    // Use marked to parse markdown
    return marked.parse(content)
  } catch (err) {
    console.error('Markdown parsing error:', err)
    // Fallback to simple formatting
    return content
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.*?)\*/g, '<em>$1</em>')
      .replace(/`(.*?)`/g, '<code>$1</code>')
      .replace(/\n/g, '<br>')
  }
}

// Format timestamp
const formatTime = (timestamp) => {
  if (!timestamp) return ''
  const date = new Date(timestamp)
  return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })
}

// Scroll to bottom of messages
const scrollToBottom = () => {
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
  }
}

// Watch for new messages and scroll
watch(messages, () => {
  nextTick(() => {
    scrollToBottom()
  })
}, { deep: true })

// Watch streaming message
watch(currentMessage, () => {
  nextTick(() => {
    scrollToBottom()
  })
})

// Watch for pending navigation and auto-navigate ONLY if toggle is enabled
watch(pendingNavigation, (navCommand) => {
  if (!navCommand) return

  // Check if auto-navigate toggle is ON
  if (autoNavigate.value) {
    console.log('🚀 Auto-navigating (toggle ON):', navCommand)

    // Build route
    const route = {
      name: navCommand.route,
      params: {},
      query: {}
    }

    // Parse params
    if (navCommand.params) {
      for (const [key, value] of Object.entries(navCommand.params)) {
        if (key === 'datasetName' || key === 'typeName') {
          route.params[key] = value
        } else {
          route.query[key] = value
        }
      }
    }

    // Auto-navigate
    router.push(route)

    // Minimize chat after auto-navigation
    // isOpen.value = false
  } else {
    console.log('ℹ️  Navigation available (toggle OFF, click "See more" to navigate)')
  }

  // Clear pending navigation
  pendingNavigation.value = null
})

// Drag functionality
const startDrag = (e) => {
  isDragging.value = true
  dragStartX.value = e.clientX - windowX.value
  dragStartY.value = e.clientY - windowY.value
  document.addEventListener('mousemove', onDrag)
  document.addEventListener('mouseup', stopDrag)
}

const onDrag = (e) => {
  if (!isDragging.value) return
  windowX.value = e.clientX - dragStartX.value
  windowY.value = e.clientY - dragStartY.value
  if (chatWindow.value) {
    chatWindow.value.style.transform = `translate(${windowX.value}px, ${windowY.value}px)`
  }
}

const stopDrag = () => {
  isDragging.value = false
  document.removeEventListener('mousemove', onDrag)
  document.removeEventListener('mouseup', stopDrag)
}

// Resize functionality
const startResize = (e) => {
  e.preventDefault()
  isResizing.value = true
  resizeStartX.value = e.clientX
  resizeStartY.value = e.clientY
  resizeStartWidth.value = windowWidth.value
  resizeStartHeight.value = windowHeight.value
  document.addEventListener('mousemove', onResize)
  document.addEventListener('mouseup', stopResize)
}

const onResize = (e) => {
  if (!isResizing.value) return

  const deltaX = e.clientX - resizeStartX.value
  const deltaY = e.clientY - resizeStartY.value

  // Update dimensions (min 300x400, max 800x900)
  windowWidth.value = Math.max(300, Math.min(800, resizeStartWidth.value + deltaX))
  windowHeight.value = Math.max(400, Math.min(900, resizeStartHeight.value + deltaY))

  updateWindowStyle()
}

const stopResize = () => {
  if (isResizing.value) {
    saveSettings()
  }
  isResizing.value = false
  document.removeEventListener('mousemove', onResize)
  document.removeEventListener('mouseup', stopResize)
}

// Initialize
onMounted(() => {
  loadSettings()
  updateWindowStyle()
})
</script>

<style scoped>
.chatbot-container {
  position: fixed;
  bottom: 24px;
  right: 24px;
  z-index: 9999;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
}

/* Floating Toggle Button */
.chatbot-toggle {
  width: 60px;
  height: 60px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--primary-color, #3b82f6), #7c3aed);
  border: none;
  color: white;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s ease;
  position: relative;
}

.chatbot-toggle:hover {
  transform: scale(1.1);
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.2);
}

.connection-indicator {
  position: absolute;
  top: 4px;
  right: 4px;
  width: 12px;
  height: 12px;
  border-radius: 50%;
  border: 2px solid white;
}

.connection-indicator.online {
  background: #10b981;
  animation: pulse 2s infinite;
}

.connection-indicator.offline {
  background: #ef4444;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

/* Chat Window */
.chatbot-window {
  position: fixed;
  bottom: 24px;
  right: 24px;
  background: white;
  border-radius: 16px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.15);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  transition: all 0.3s ease;
}

/* Header */
.chatbot-header {
  background: linear-gradient(135deg, var(--primary-color, #3b82f6), #7c3aed);
  color: white;
  padding: 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  cursor: move;
  user-select: none;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.bot-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.2);
  display: flex;
  align-items: center;
  justify-content: center;
}

.header-info h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
}

.header-info .status {
  margin: 2px 0 0 0;
  font-size: 12px;
  opacity: 0.9;
}

.typing-indicator {
  font-style: italic;
}

.online-text { color: #86efac; }
.offline-text { opacity: 0.7; }

.header-actions {
  display: flex;
  gap: 8px;
}

.header-btn {
  background: rgba(255, 255, 255, 0.2);
  border: none;
  color: white;
  width: 32px;
  height: 32px;
  border-radius: 8px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.2s;
}

.header-btn:hover {
  background: rgba(255, 255, 255, 0.3);
}

/* Settings Bar */
.settings-bar {
  background: #f9fafb;
  border-bottom: 1px solid #e5e7eb;
  padding: 8px 16px;
}

.toggle-setting {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  cursor: pointer;
}

.toggle-setting input[type="checkbox"] {
  cursor: pointer;
}

.toggle-label {
  font-weight: 500;
  color: #374151;
}

.toggle-hint {
  color: #6b7280;
  font-size: 12px;
}

/* Messages Area */
.chatbot-messages {
  flex: 1;
  overflow-y: auto;
  padding: 16px;
  background: #f9fafb;
  min-height: 0;
}

.welcome-message {
  text-align: center;
  padding: 32px 16px;
  color: #6b7280;
}

.welcome-icon {
  font-size: 48px;
  margin-bottom: 16px;
}

.welcome-message h4 {
  margin: 0 0 8px 0;
  color: #111827;
  font-size: 18px;
}

.welcome-message p {
  margin: 0 0 24px 0;
  font-size: 14px;
}

.example-questions {
  display: flex;
  flex-direction: column;
  gap: 8px;
  align-items: center;
}

.example-label {
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
  color: #9ca3af;
  margin-bottom: 4px;
}

.example-btn {
  background: white;
  border: 1px solid #e5e7eb;
  padding: 8px 16px;
  border-radius: 8px;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;
  color: #374151;
  max-width: 100%;
  text-align: left;
}

.example-btn:hover {
  border-color: var(--primary-color, #3b82f6);
  color: var(--primary-color, #3b82f6);
  background: #eff6ff;
}

.message {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
  animation: fadeIn 0.3s ease;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.message-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--primary-color, #3b82f6), #7c3aed);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.message-user {
  flex-direction: row-reverse;
}

.message-user .message-content {
  background: var(--primary-color, #3b82f6);
  color: white;
  border-radius: 16px 16px 4px 16px;
}

.message-assistant .message-content {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 16px 16px 16px 4px;
}

.message-content {
  max-width: 75%;
  padding: 12px 16px;
  word-wrap: break-word;
}

.message-text {
  font-size: 14px;
  line-height: 1.6;
}

/* Markdown styles */
.message-text :deep(h1),
.message-text :deep(h2),
.message-text :deep(h3) {
  margin: 12px 0 8px 0;
  font-weight: 600;
  line-height: 1.3;
}

.message-text :deep(h1) { font-size: 18px; }
.message-text :deep(h2) { font-size: 16px; }
.message-text :deep(h3) { font-size: 15px; }

.message-text :deep(p) {
  margin: 8px 0;
}

.message-text :deep(p:first-child) {
  margin-top: 0;
}

.message-text :deep(p:last-child) {
  margin-bottom: 0;
}

.message-text :deep(ul),
.message-text :deep(ol) {
  margin: 8px 0;
  padding-left: 20px;
}

.message-text :deep(li) {
  margin: 4px 0;
}

.message-text :deep(strong) {
  font-weight: 600;
}

.message-text :deep(em) {
  font-style: italic;
}

.message-text :deep(code) {
  background: rgba(0, 0, 0, 0.1);
  padding: 2px 6px;
  border-radius: 4px;
  font-family: 'Courier New', monospace;
  font-size: 13px;
}

.message-text :deep(pre) {
  background: rgba(0, 0, 0, 0.05);
  border: 1px solid rgba(0, 0, 0, 0.1);
  border-radius: 6px;
  padding: 12px;
  overflow-x: auto;
  margin: 8px 0;
}

.message-text :deep(pre code) {
  background: none;
  padding: 0;
  border-radius: 0;
}

.message-text :deep(a) {
  color: var(--primary-color, #3b82f6);
  text-decoration: underline;
}

.message-text :deep(blockquote) {
  border-left: 3px solid rgba(0, 0, 0, 0.2);
  padding-left: 12px;
  margin: 8px 0;
  color: rgba(0, 0, 0, 0.7);
}

.message-text :deep(table) {
  border-collapse: collapse;
  width: 100%;
  margin: 8px 0;
  font-size: 13px;
}

.message-text :deep(table th) {
  background: #f3f4f6;
  font-weight: 600;
  text-align: left;
  padding: 8px;
  border: 1px solid #e5e7eb;
}

.message-text :deep(table td) {
  padding: 8px;
  border: 1px solid #e5e7eb;
}

.message-text :deep(table tr:nth-child(even)) {
  background: #f9fafb;
}

/* User message markdown overrides */
.message-user .message-text :deep(code) {
  background: rgba(255, 255, 255, 0.2);
}

.message-user .message-text :deep(pre) {
  background: rgba(255, 255, 255, 0.1);
  border-color: rgba(255, 255, 255, 0.2);
}

.message-user .message-text :deep(a) {
  color: white;
  text-decoration: underline;
}

.message-user .message-text :deep(blockquote) {
  border-left-color: rgba(255, 255, 255, 0.4);
  color: rgba(255, 255, 255, 0.9);
}

.error-message {
  border-color: #fca5a5 !important;
  background: #fef2f2 !important;
  color: #991b1b !important;
}

.cursor {
  animation: blink 1s infinite;
  color: var(--primary-color, #3b82f6);
  margin-left: 2px;
  display: inline-block;
  vertical-align: text-bottom;
}

@keyframes blink {
  0%, 50% { opacity: 1; }
  51%, 100% { opacity: 0; }
}

.typing-dots {
  display: flex;
  gap: 4px;
  padding: 8px 0;
}

.typing-dots span {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #9ca3af;
  animation: typing 1.4s infinite;
}

.typing-dots span:nth-child(2) { animation-delay: 0.2s; }
.typing-dots span:nth-child(3) { animation-delay: 0.4s; }

@keyframes typing {
  0%, 60%, 100% { transform: translateY(0); }
  30% { transform: translateY(-10px); }
}

.navigation-commands {
  margin-top: 12px;
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.nav-link {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, var(--primary-color, #3b82f6), #7c3aed);
  border: none;
  border-radius: 8px;
  color: white;
  text-decoration: none;
  font-size: 13px;
  font-weight: 600;
  transition: all 0.2s;
  box-shadow: 0 2px 4px rgba(59, 130, 246, 0.2);
}

.nav-link:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 8px rgba(59, 130, 246, 0.3);
}

.nav-link svg {
  flex-shrink: 0;
}

.message-time {
  font-size: 11px;
  color: #9ca3af;
  margin-top: 4px;
}

.message-user .message-time {
  color: rgba(255, 255, 255, 0.7);
  text-align: right;
}

.error-banner {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px;
  background: #fef2f2;
  border: 1px solid #fca5a5;
  border-radius: 8px;
  color: #991b1b;
  font-size: 13px;
  margin-bottom: 12px;
}

/* Input Area */
.chatbot-input {
  display: flex;
  gap: 8px;
  padding: 16px;
  background: white;
  border-top: 1px solid #e5e7eb;
}

.chatbot-input textarea {
  flex: 1;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 10px 14px;
  font-size: 14px;
  font-family: inherit;
  resize: none;
  outline: none;
  transition: border-color 0.2s;
  max-height: 120px;
}

.chatbot-input textarea:focus {
  border-color: var(--primary-color, #3b82f6);
}

.chatbot-input textarea:disabled {
  background: #f9fafb;
  color: #9ca3af;
  cursor: not-allowed;
}

.send-btn {
  width: 40px;
  height: 40px;
  border: none;
  border-radius: 12px;
  background: var(--primary-color, #3b82f6);
  color: white;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
  flex-shrink: 0;
}

.send-btn:hover:not(:disabled) {
  background: #2563eb;
  transform: scale(1.05);
}

.send-btn:disabled {
  background: #e5e7eb;
  color: #9ca3af;
  cursor: not-allowed;
}

/* Resize Handle */
.resize-handle {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 20px;
  height: 20px;
  cursor: nwse-resize;
  background: linear-gradient(135deg, transparent 50%, #9ca3af 50%);
  border-bottom-right-radius: 16px;
}

.resize-handle:hover {
  background: linear-gradient(135deg, transparent 50%, var(--primary-color, #3b82f6) 50%);
}

/* Scrollbar */
.chatbot-messages::-webkit-scrollbar {
  width: 6px;
}

.chatbot-messages::-webkit-scrollbar-track {
  background: transparent;
}

.chatbot-messages::-webkit-scrollbar-thumb {
  background: #d1d5db;
  border-radius: 3px;
}

.chatbot-messages::-webkit-scrollbar-thumb:hover {
  background: #9ca3af;
}
</style>
