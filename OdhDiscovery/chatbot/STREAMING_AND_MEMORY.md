# Streaming and Conversation Memory

**Date**: 2025-10-21
**Status**: ✅ Implemented

---

## Overview

The ODH Chatbot now supports:
1. **Streaming Responses**: Real-time token-by-token streaming for a better chatbot feel
2. **Conversation Memory**: Multi-turn conversations with context awareness
3. **Session Management**: In-memory storage with automatic cleanup

---

## Features

### 1. Streaming Responses

Responses are streamed word-by-word with a typing effect instead of waiting for the complete response.

**Benefits**:
- Immediate feedback to users
- Better perceived performance
- Natural chatbot feeling
- User sees progress in real-time

**Implementation**:
- Streams in chunks of 3 words
- 50ms delay between chunks (configurable)
- Sends `{type: "chunk", content: "...", done: false}`
- Final chunk: `{type: "chunk", content: "", done: true}`

### 2. Conversation Memory

Multi-turn conversations with full context awareness.

**Features**:
- Remembers previous messages in conversation
- Agent has access to full conversation history
- Supports follow-up questions
- Maintains context across turns

**Storage**:
- In-memory storage (configurable for Redis/DB later)
- Per-session conversation history
- Automatic session creation
- 24-hour expiration for inactive sessions

### 3. Session Management

Robust session handling with REST API management.

**Capabilities**:
- Auto-generated session IDs (UUID)
- Get session information
- Clear conversation history
- Delete sessions
- List active sessions
- Automatic cleanup of expired sessions

---

## API Reference

### WebSocket Endpoint `/ws`

Real-time chat with streaming responses.

#### Client → Server Messages

**1. Query Message**
```json
{
  "type": "query",
  "content": "User's question here",
  "session_id": "optional-session-id-to-continue-conversation"
}
```

**2. Clear History**
```json
{
  "type": "clear_history",
  "session_id": "session-id"
}
```

**3. Ping (Keepalive)**
```json
{
  "type": "ping"
}
```

#### Server → Client Messages

**1. Session Info** (sent first)
```json
{
  "type": "session",
  "session_id": "generated-or-existing-session-id",
  "message_count": 4
}
```

**2. Status Update**
```json
{
  "type": "status",
  "content": "Processing your question..."
}
```

**3. Streaming Chunk**
```json
{
  "type": "chunk",
  "content": "Here are the ",
  "done": false
}
```

**4. Final Chunk**
```json
{
  "type": "chunk",
  "content": "",
  "done": true
}
```

**5. Navigation Command**
```json
{
  "type": "navigation",
  "data": {
    "type": "navigate",
    "route": "DatasetBrowser",
    "params": {"dataspace": "tourism"}
  }
}
```

**6. Error Message**
```json
{
  "type": "error",
  "content": "Error description"
}
```

**7. Pong (Keepalive Response)**
```json
{
  "type": "pong"
}
```

---

### HTTP Endpoint `/query` (POST)

Non-streaming endpoint for testing or simple integrations.

#### Request
```json
{
  "query": "User's question",
  "session_id": "optional-session-id",
  "include_debug": false
}
```

#### Response
```json
{
  "response": "Complete answer here",
  "session_id": "session-id",
  "navigation_commands": [...],
  "iterations": 2,
  "tool_calls": [...],
  "debug_info": {...}
}
```

---

### Session Management Endpoints

#### Get Session Info `GET /sessions/{session_id}`

```bash
curl http://localhost:8001/sessions/abc-123-def
```

**Response**:
```json
{
  "session_id": "abc-123-def",
  "message_count": 8,
  "created_at": "2025-10-21T20:00:00",
  "last_activity": "2025-10-21T20:15:30",
  "age_hours": 0.25,
  "metadata": {}
}
```

#### Delete Session `DELETE /sessions/{session_id}`

```bash
curl -X DELETE http://localhost:8001/sessions/abc-123-def
```

**Response**:
```json
{
  "message": "Session deleted",
  "session_id": "abc-123-def"
}
```

#### Clear Session History `POST /sessions/{session_id}/clear`

Clears conversation history but keeps session alive.

```bash
curl -X POST http://localhost:8001/sessions/abc-123-def/clear
```

**Response**:
```json
{
  "message": "Session history cleared",
  "session_id": "abc-123-def"
}
```

#### List Active Sessions `GET /sessions`

```bash
curl http://localhost:8001/sessions
```

**Response**:
```json
{
  "active_sessions": 15,
  "max_age_hours": 24
}
```

#### Get Conversation History `GET /sessions/{session_id}/messages`

Retrieve conversation messages from a session to re-populate the chat UI.

**Important**: This endpoint returns only **user questions** and **final bot responses** - it filters out system prompts, tool calls, and intermediate messages. This makes it ideal for re-populating the chat interface.

```bash
curl http://localhost:8001/sessions/abc-123-def/messages
```

**Response**:
```json
{
  "session_id": "abc-123-def",
  "messages": [
    {
      "role": "user",
      "content": "What datasets are available?",
      "timestamp": null
    },
    {
      "role": "assistant",
      "content": "There are 165 datasets available across tourism, mobility, and other categories. Would you like to explore a specific category?",
      "timestamp": null
    },
    {
      "role": "user",
      "content": "Yes, show me tourism datasets",
      "timestamp": null
    },
    {
      "role": "assistant",
      "content": "There are 109 tourism datasets available in the Open Data Hub.",
      "timestamp": null
    }
  ],
  "message_count": 4,
  "created_at": "2025-10-21T20:00:00",
  "last_activity": "2025-10-21T20:15:30"
}
```

**Use Cases**:
- User returns to a session after closing the browser
- User refreshes the page
- User navigates away and comes back
- Restoring conversation state in a new browser tab

**What's Filtered Out**:
- System prompts and initialization messages
- Tool execution results
- Empty assistant messages
- Internal state messages

**What's Included**:
- All user questions (HumanMessage)
- Final assistant responses (AIMessage with content)

---

## Frontend Integration Examples

### WebSocket Client (JavaScript)

```javascript
class ChatbotClient {
  constructor(wsUrl = 'ws://localhost:8001/ws') {
    this.wsUrl = wsUrl;
    this.ws = null;
    this.sessionId = null;
    this.currentResponse = "";
  }

  connect() {
    this.ws = new WebSocket(this.wsUrl);

    this.ws.onopen = () => {
      console.log('Connected to chatbot');
    };

    this.ws.onmessage = (event) => {
      const message = JSON.parse(event.data);
      this.handleMessage(message);
    };

    this.ws.onerror = (error) => {
      console.error('WebSocket error:', error);
    };

    this.ws.onclose = () => {
      console.log('Disconnected from chatbot');
    };
  }

  handleMessage(message) {
    switch(message.type) {
      case 'session':
        // Store session ID for follow-up messages
        this.sessionId = message.session_id;
        console.log(`Session: ${this.sessionId} (${message.message_count} messages)`);
        break;

      case 'status':
        // Show status in UI
        this.updateStatus(message.content);
        break;

      case 'chunk':
        // Append streaming chunk to response
        if (!message.done) {
          this.currentResponse += message.content;
          this.updateDisplay(this.currentResponse);
        } else {
          // Streaming complete
          this.finalizeResponse(this.currentResponse);
          this.currentResponse = "";
        }
        break;

      case 'navigation':
        // Handle navigation command
        this.handleNavigation(message.data);
        break;

      case 'error':
        // Show error
        this.showError(message.content);
        break;

      case 'pong':
        // Keepalive response
        break;
    }
  }

  sendQuery(query) {
    if (!this.ws || this.ws.readyState !== WebSocket.OPEN) {
      console.error('WebSocket not connected');
      return;
    }

    this.ws.send(JSON.stringify({
      type: 'query',
      content: query,
      session_id: this.sessionId  // Include for conversation continuity
    }));
  }

  clearHistory() {
    if (!this.ws || this.ws.readyState !== WebSocket.OPEN) {
      console.error('WebSocket not connected');
      return;
    }

    this.ws.send(JSON.stringify({
      type: 'clear_history',
      session_id: this.sessionId
    }));

    this.sessionId = null; // Reset session
  }

  updateStatus(status) {
    // Update UI status indicator
    document.getElementById('status').textContent = status;
  }

  updateDisplay(text) {
    // Update chat display with streaming text
    const messageEl = document.getElementById('current-message');
    messageEl.textContent = text;
  }

  finalizeResponse(text) {
    // Move from streaming to final message
    const chatContainer = document.getElementById('chat-messages');
    const messageEl = document.createElement('div');
    messageEl.className = 'bot-message';
    messageEl.textContent = text;
    chatContainer.appendChild(messageEl);

    // Clear status
    this.updateStatus('');
  }

  handleNavigation(navData) {
    // Execute navigation command
    if (navData.type === 'navigate') {
      // Your routing logic here
      this.navigateTo(navData.route, navData.params);
    }
  }

  showError(error) {
    // Show error in UI
    alert(`Error: ${error}`);
  }

  async restoreConversation(sessionId) {
    // Restore conversation history from server
    try {
      const response = await fetch(`http://localhost:8001/sessions/${sessionId}/messages`);

      if (!response.ok) {
        console.error('Session not found or expired');
        return false;
      }

      const data = await response.json();
      this.sessionId = data.session_id;

      // Clear existing chat UI
      const chatContainer = document.getElementById('chat-messages');
      chatContainer.innerHTML = '';

      // Re-populate chat with historical messages
      data.messages.forEach(msg => {
        const messageEl = document.createElement('div');
        messageEl.className = msg.role === 'user' ? 'user-message' : 'bot-message';
        messageEl.textContent = msg.content;
        chatContainer.appendChild(messageEl);
      });

      console.log(`Restored ${data.message_count} messages from session ${sessionId}`);
      return true;
    } catch (error) {
      console.error('Failed to restore conversation:', error);
      return false;
    }
  }

  disconnect() {
    if (this.ws) {
      this.ws.close();
    }
  }
}

// Usage
const chatbot = new ChatbotClient();
chatbot.connect();

// Send query
document.getElementById('send-btn').addEventListener('click', () => {
  const query = document.getElementById('query-input').value;
  chatbot.sendQuery(query);
});

// Clear history
document.getElementById('clear-btn').addEventListener('click', () => {
  chatbot.clearHistory();
});

// Restore conversation on page load (e.g., from localStorage)
window.addEventListener('load', async () => {
  const savedSessionId = localStorage.getItem('chatbot_session_id');
  if (savedSessionId) {
    const restored = await chatbot.restoreConversation(savedSessionId);
    if (restored) {
      console.log('Conversation restored!');
    } else {
      // Session expired or not found, clear saved ID
      localStorage.removeItem('chatbot_session_id');
    }
  }
});

// Save session ID to localStorage when received
// (add to handleMessage 'session' case)
// localStorage.setItem('chatbot_session_id', message.session_id);
```

---

### HTTP Client Example (cURL)

```bash
# First query (creates session)
curl -X POST http://localhost:8001/query \
  -H "Content-Type: application/json" \
  -d '{
    "query": "What datasets are available?",
    "include_debug": true
  }'

# Response includes session_id: "abc-123-def"

# Follow-up query (continues conversation)
curl -X POST http://localhost:8001/query \
  -H "Content-Type: application/json" \
  -d '{
    "query": "Show me the tourism ones",
    "session_id": "abc-123-def"
  }'

# Agent now has context from first query!

# Retrieve conversation history (e.g., after page refresh)
curl http://localhost:8001/sessions/abc-123-def/messages

# Response contains all messages in the conversation
```

---

## Configuration

### Memory Store Settings

Located in `conversation_memory.py`:

```python
# Maximum age for inactive sessions before cleanup (hours)
max_age_hours = 24  # Default: 24 hours

# Streaming chunk size (words per chunk)
chunk_size = 3  # Default: 3 words

# Streaming delay between chunks (seconds)
chunk_delay = 0.05  # Default: 50ms
```

### Adjusting Streaming Speed

In `main.py` WebSocket endpoint:

```python
# Faster streaming (less delay)
await asyncio.sleep(0.02)  # 20ms

# Slower streaming (more delay)
await asyncio.sleep(0.1)  # 100ms

# Larger chunks (faster overall)
chunk_size = 5  # 5 words per chunk

# Smaller chunks (smoother effect)
chunk_size = 1  # 1 word per chunk
```

---

## Conversation Examples

### Example 1: Multi-Turn Data Exploration

**User**: "What datasets are available?"

**Bot**: (streams) "There are 165 datasets available across tourism, mobility, and other categories. Would you like to explore a specific category?"

**Session**: `abc-123`

---

**User**: "Yes, show me tourism datasets"

**Bot**: (streams) "Here are the tourism datasets available..." + Navigation to DatasetBrowser

**Session**: `abc-123` (same session, conversation continues)

---

### Example 2: Follow-Up Questions

**User**: "How many hotels are there?"

**Bot**: "There are 4,523 active hotels in the accommodation dataset."

---

**User**: "Show me the ones in Bolzano"

**Bot**: (streams) "Here are the hotels in Bolzano..." + Navigation

The agent knows "the ones" refers to "hotels" from context!

---

## Implementation Details

### Message Flow

1. **Client sends query** with optional `session_id`
2. **Server gets/creates session** and retrieves conversation history
3. **User message added** to conversation
4. **Agent executes** with full conversation context
5. **Response streamed** word-by-word to client
6. **Updated history stored** in session
7. **Client receives** streaming chunks + navigation commands

### Session Lifecycle

```
┌─────────────┐
│  Connect WS │
└──────┬──────┘
       │
       v
┌─────────────┐
│ Send Query  │ ──┐
│ (no session)│   │
└──────┬──────┘   │
       │          │
       v          │
┌─────────────┐   │
│ Server      │   │
│ Creates     │   │
│ Session     │   │
└──────┬──────┘   │
       │          │
       v          │
┌─────────────┐   │
│ Returns     │   │
│ session_id  │   │
└──────┬──────┘   │
       │          │
       v          │
┌─────────────┐   │
│ Client      │   │
│ Stores ID   │   │
└──────┬──────┘   │
       │          │
       v          │
┌─────────────┐   │
│ Follow-up   │ ──┘
│ Query       │
│ (with ID)   │
└──────┬──────┘
       │
       v
┌─────────────┐
│ Continues   │
│ Conversation│
└─────────────┘
```

### Memory Storage

```python
{
  "session_id": "abc-123-def",
  "messages": [
    HumanMessage(content="What datasets are available?"),
    AIMessage(content="There are 165 datasets..."),
    HumanMessage(content="Show me tourism datasets"),
    AIMessage(content="Here are the tourism datasets..."),
    # ... more messages
  ],
  "created_at": datetime(...),
  "last_activity": datetime(...),
  "metadata": {}
}
```

---

## Future Enhancements

### Planned Features

1. **Persistent Storage**:
   - Redis backend for session persistence
   - PostgreSQL for long-term storage
   - Configurable storage backends

2. **Advanced Streaming**:
   - True token-by-token streaming from LLM
   - Stream tool execution status
   - Progress indicators for long operations

3. **Session Analytics**:
   - Track conversation metrics
   - Average session length
   - Popular queries

4. **Context Management**:
   - Automatic summarization of long conversations
   - Configurable context window size
   - Smart context pruning

5. **Multi-User Sessions**:
   - User authentication
   - Per-user session management
   - Privacy controls

---

## Testing

### Test Streaming

```bash
# Start backend
cd backend
python main.py

# Connect via WebSocket (requires wscat or similar)
wscat -c ws://localhost:8001/ws

# Send query
{"type": "query", "content": "What datasets are available?"}

# Observe streaming chunks
```

### Test Conversation Memory

```bash
# Query 1
curl -X POST http://localhost:8001/query \
  -H "Content-Type: application/json" \
  -d '{"query": "How many hotels are there?"}'

# Save session_id from response

# Query 2 (uses context)
curl -X POST http://localhost:8001/query \
  -H "Content-Type: application/json" \
  -d '{"query": "Show me the active ones", "session_id": "SESSION_ID_HERE"}'

# Agent understands "ones" = "hotels" from context!
```

---

## Troubleshooting

### Issue: Session Not Found

**Cause**: Session expired (>24 hours inactive) or was deleted

**Solution**: Let backend create new session automatically

---

### Issue: Streaming Too Fast/Slow

**Cause**: Default chunk delay might not suit your needs

**Solution**: Adjust `await asyncio.sleep(0.05)` in `main.py:351`

---

### Issue: Context Lost Between Queries

**Cause**: Not providing `session_id` in subsequent queries

**Solution**: Store and include `session_id` from server response

---

## Summary

✅ **Streaming Responses**: Word-by-word streaming with 50ms delay
✅ **Conversation Memory**: Full multi-turn conversation support
✅ **Session Management**: REST API for session CRUD operations
✅ **In-Memory Storage**: Fast, with auto-cleanup after 24h
✅ **WebSocket Support**: Real-time bidirectional communication
✅ **HTTP Support**: Traditional request/response for testing

The chatbot now feels like a real conversational interface with streaming responses and context awareness! 🎉
