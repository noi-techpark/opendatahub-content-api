# Navigation Command Persistence

**Date**: 2025-10-22
**Status**: ✅ Implemented
**Issue**: Navigation commands were not persisted with session history

---

## Problem

Navigation commands were sent to the client in real-time but NOT stored in the session. When users returned and retrieved old messages, navigation links were missing.

**Before**:
```python
# Backend stored messages only
session.messages = messages  # LangChain message history

# Navigation commands were sent but not persisted
navigation_commands = result.get("navigation_commands", [])  # Lost after response
```

**Frontend**:
```javascript
// Old messages had empty navigationCommands
navigationCommands: []  // Hard-coded empty array
```

---

## Solution

### 1. Backend: Store Navigation Commands with Session

**Updated `/chatbot/backend/conversation_memory.py`**:

```python
@dataclass
class ConversationSession:
    session_id: str
    messages: List[Any] = field(default_factory=list)
    navigation_history: List[List[dict]] = field(default_factory=list)  # NEW!
    # ...

    def add_navigation_commands(self, nav_commands: List[dict]):
        """Store navigation commands for the current exchange"""
        self.navigation_history.append(nav_commands)

    def get_navigation_history(self) -> List[List[dict]]:
        """Get navigation history"""
        return self.navigation_history.copy()
```

**Structure**:
```python
navigation_history = [
    [],  # Exchange 0: No navigation
    [{"type": "navigate", "route": "DatasetBrowser", "params": {...}}],  # Exchange 1
    [],  # Exchange 2: No navigation
    [{"type": "navigate", "route": "DatasetInspector", "params": {...}}]  # Exchange 3
]
```

Each index corresponds to a user-assistant exchange.

### 2. Backend: Persist Navigation Commands

**Updated `/chatbot/backend/main.py` (both /query and /ws endpoints)**:

```python
# Execute agent
result = await agent.ainvoke(initial_state)

# Extract navigation commands
navigation_commands = result.get("navigation_commands", [])

# Store messages
session.messages = messages

# Store navigation commands for this exchange
session.add_navigation_commands(navigation_commands)  # NEW!
```

### 3. Backend: Return Navigation Commands with Messages

**Updated `/chatbot/backend/main.py` GET /sessions/{id}/messages**:

```python
messages = []
exchange_index = 0
navigation_history = session.get_navigation_history()

for msg in session.messages:
    if msg_class == "AIMessage":
        # Get navigation commands for this exchange
        nav_commands = []
        if exchange_index < len(navigation_history):
            nav_commands = navigation_history[exchange_index]

        messages.append({
            "role": "assistant",
            "content": msg.content,
            "timestamp": None,
            "navigationCommands": nav_commands  # Include historical nav commands!
        })

        exchange_index += 1
```

### 4. Frontend: Accept Navigation Commands from History

**Updated `/webapp/src/composables/useChatbot.js`**:

```javascript
const retrieveMessages = async (sessionId) => {
  const response = await axios.get(`${CHATBOT_API_URL}/sessions/${sessionId}/messages`)

  const retrievedMessages = response.data.messages.map(msg => ({
    role: msg.role,
    content: msg.content,
    timestamp: msg.timestamp || new Date().toISOString(),
    navigationCommands: msg.navigationCommands || []  // Accept from backend!
  }))

  messages.value = retrievedMessages
}
```

### 5. Frontend: Auto-Navigate ONLY for Real-Time Messages

**The Key Distinction**:

```javascript
// Real-time navigation (from WebSocket)
case 'navigation':
  lastMessage.navigationCommands.push(data.data)
  pendingNavigation.value = data.data  // ✅ Triggers auto-navigation watcher

// Historical navigation (from retrieveMessages)
// navigationCommands are in message object
// pendingNavigation is NOT set
// ❌ Does NOT trigger auto-navigation watcher
```

**Auto-Navigation Watcher** (`/webapp/src/components/ChatBot.vue`):

```javascript
watch(pendingNavigation, (navCommand) => {
  if (!navCommand) return

  // Only triggered for real-time messages!
  if (autoNavigate.value) {
    console.log('🚀 Auto-navigating (toggle ON)')
    router.push(route)
    isOpen.value = false
  }

  pendingNavigation.value = null
})
```

---

## Behavior Matrix

| Scenario | Navigation Commands | Auto-Navigate (if toggle ON) | Shows "See more" |
|----------|---------------------|------------------------------|------------------|
| **Real-time message** | ✅ Sent via WebSocket | ✅ Yes (via `pendingNavigation`) | ✅ Yes |
| **Historical message** | ✅ Retrieved from API | ❌ No (NOT set in `pendingNavigation`) | ✅ Yes |
| **Manual click** | N/A | N/A | ✅ Always navigates |

---

## Updated Agent Prompts

Made the bot more proactive about navigation:

**Before**:
```
Use navigate_webapp SELECTIVELY to enhance responses
```

**After**:
```
Use navigate_webapp to enhance responses with UI visualization.

✅ WHEN TO NAVIGATE (Be Proactive!):
- User asks about datasets → Navigate to DatasetBrowser
- User asks to "list", "show", "display" data → Navigate to appropriate view
- Any time the UI can provide better exploration than text

## Key Principle
If there's a view that can help the user visualize or explore the data
you're talking about, NAVIGATE to it!
```

**Example**: "List all datasets in tourism" now triggers navigation to DatasetBrowser with tourism filter.

---

## Complete Flow

### Real-Time Message Flow

1. User asks: "List all datasets"
2. Agent responds with navigation command
3. **Backend**: Stores navigation command in `session.navigation_history`
4. **Backend**: Sends navigation via WebSocket `{type: "navigation", data: {...}}`
5. **Frontend**: Adds to `message.navigationCommands` + sets `pendingNavigation.value`
6. **Watcher**: If `autoNavigate` is ON → `router.push()`
7. **UI**: Shows "See more" button

### Historical Message Load Flow

1. User returns to session
2. **Frontend**: Calls `GET /sessions/{id}/messages`
3. **Backend**: Returns messages WITH `navigationCommands` field
4. **Frontend**: Displays messages with "See more" buttons
5. **Watcher**: Does NOT trigger (no `pendingNavigation` set)
6. **UI**: User can manually click "See more" to navigate

---

## Testing

### Test Persistence

1. Ask: "List all datasets"
2. Verify "See more" button appears
3. Close chat
4. Reopen chat
5. **Expected**: "See more" button still visible
6. **Expected**: Does NOT auto-navigate (even with toggle ON)
7. Click "See more"
8. **Expected**: Navigates to DatasetBrowser

### Test Auto-Navigation

1. Enable auto-navigate toggle
2. Ask: "Show me tourism datasets"
3. **Expected**: Auto-navigates immediately to DatasetBrowser?dataspace=tourism
4. Return to home
5. Reopen chat (loads history)
6. **Expected**: Previous message still shows "See more" button
7. **Expected**: Does NOT auto-navigate again

---

## Files Modified

### Backend
1. **`conversation_memory.py`**
   - Added `navigation_history` field
   - Added `add_navigation_commands()` method
   - Added `get_navigation_history()` method
   - Updated `clear()` to clear navigation history

2. **`main.py`**
   - Updated `/query` endpoint to store navigation commands
   - Updated `/ws` endpoint to store navigation commands
   - Updated `/sessions/{id}/messages` to return navigation commands
   - Added exchange tracking to match nav commands with messages

### Frontend
3. **`composables/useChatbot.js`**
   - Added `pendingNavigation` ref for auto-navigation
   - Updated `retrieveMessages()` to accept `navigationCommands`
   - Updated WebSocket handler to set `pendingNavigation` for real-time nav

4. **`components/ChatBot.vue`**
   - Added watcher for `pendingNavigation`
   - Auto-navigate ONLY if toggle is ON
   - Historical messages display "See more" without auto-navigation

### Prompts
5. **`agent/prompts.py`**
   - Updated navigation guidelines to be more proactive
   - Added "List all datasets" example with navigation

---

## Summary

✅ **Navigation commands are now persisted** with session history
✅ **Historical messages display "See more" buttons** (just like real-time)
✅ **Auto-navigation ONLY for real-time messages** (if toggle is ON)
✅ **Manual "See more" click always works** (historical or real-time)
✅ **Agent is more proactive about navigation** (updated prompts)

**Result**: Users can return to a session and see all navigation suggestions, but the app won't auto-navigate to old suggestions (preventing unexpected navigation on history load).

🎉 **Navigation Persistence: COMPLETE**
