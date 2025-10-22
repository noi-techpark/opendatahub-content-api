# Prompt Engineering & Rendering Improvements

**Date**: 2025-10-22
**Status**: ✅ Implemented

---

## Problem Statement

The chatbot was experiencing multiple issues:

1. **Bot not following formatting instructions**: Answering with "I found..." instead of "Open Data Hub has...", creating long lists, not using tables
2. **System prompt effectiveness**: Prompt was added once at conversation start and could get "buried" in context during long conversations
3. **Markdown rendering**: Only rendered after message completed, not incrementally during streaming (unlike ChatGPT)
4. **ANALYSIS_PROMPT unused**: Defined but never used in the codebase

---

## Solutions Implemented

### 1. System Prompt Injection Strategy (Best Practice)

**Problem**: System prompt was stored in conversation history, consuming tokens and potentially getting diluted by long conversations.

**Solution**: Implement "ephemeral system prompt" pattern

**Modified**: `/chatbot/backend/agent/graph.py` - `call_model()` function

```python
async def call_model(state: AgentState) -> AgentState:
    # Get conversation messages
    messages = list(state.get("messages", []))
    # Filter out any system messages - we'll inject fresh system prompt
    # This prevents system messages from being stored in conversation history
    conversation_messages = [m for m in messages if not isinstance(m, SystemMessage)]

    # Build LLM payload: ALWAYS inject system message at the start
    # System message is ephemeral (not stored in history) to save tokens
    llm_payload = [SystemMessage(content=SYSTEM_PROMPT)] + conversation_messages + new_messages

    response = await llm_with_tools.ainvoke(llm_payload)
```

**Benefits**:
- ✅ System prompt sent with **every** LLM call (always fresh and prioritized)
- ✅ System prompt **not stored** in conversation history (saves tokens)
- ✅ Separate "conversation history" (stored) from "LLM payload" (ephemeral)
- ✅ No token bloat from repeated system messages

**Best Practice Explanation**:

This is the **industry standard** approach used by most production chatbots:

1. **Conversation history** = User questions + Assistant answers (stored in session)
2. **LLM payload** = System prompt + Conversation history (sent to LLM, not stored)
3. **System prompt is ephemeral** = Fresh with every call, never stored

Alternative approaches considered:
- ❌ **Store system prompt once**: Gets diluted by tool calls and long conversations
- ❌ **Re-inject periodically**: Inconsistent, adds complexity
- ❌ **ANALYSIS_PROMPT two-stage**: Adds latency and token cost (see section below)

---

### 2. Enhanced Response Policies

**Problem**: Rules were too brief ("Rule 2: Never answer with 'I ...'") without clear examples.

**Solution**: Expanded rules with ❌ BAD / ✅ GOOD examples

**Modified**: `/chatbot/backend/agent/prompts.py`

```python
## Response Policies (FOLLOW THESE EXACTLY!)

**Rule 1: Avoid Long Lists**
   - Lists with more than 5-7 items are hard to read
   - Show 3-5 examples + summary (e.g., "...and 162 more datasets")
   - ❌ BAD: [50-item bullet list]
   - ✅ GOOD: "Here are some examples: A, B, C ...and 47 more. Would you like to explore a specific category?"

**Rule 2: Use Third-Person Voice (CRITICAL!)**
   - NEVER use first person ("I found", "I searched", "Let me show you")
   - ALWAYS attribute to Open Data Hub ("Open Data Hub contains...", "Available in ODH are...", "The platform offers...")
   - ❌ BAD: "I found 167 datasets in various domains"
   - ✅ GOOD: "Open Data Hub contains 167 datasets across tourism, mobility, and other domains"

**Rule 3: Be Concise and Engaging**
   - Keep responses short (2-4 sentences for simple queries)
   - Provide enough context to be helpful, not overwhelming
   - End with a guiding question or next step when appropriate
   - ❌ BAD: Long paragraph with excessive detail
   - ✅ GOOD: "Open Data Hub has 167 datasets. Are you interested in tourism, mobility, or other categories?"

**Rule 4: Prefer Tables Over Lists**
   - For data with multiple fields (name, type, count), use markdown tables
   - Tables are easier to scan than bullet lists
   - Keep tables concise (3-7 rows max)
   - ❌ BAD: Bullet list with "Name: X, Type: Y, Count: Z"
   - ✅ GOOD: Markdown table with columns: Name | Type | Count
```

**Benefits**:
- ✅ Clear, actionable rules with visual examples
- ✅ Emphasis on table usage (easier to scan than lists)
- ✅ Concise response guideline (2-4 sentences for simple queries)

**Fixed Examples**: Updated all examples to follow the rules

Before:
```
"I found **109 datasets** in tourism:
- Accommodation
- Activity
... (and 103 more)"
```

After:
```
"Open Data Hub contains **109 datasets** in the tourism dataspace:
- Accommodation
- Activity
... (and 104 more)"
```

---

### 3. Incremental Markdown Rendering

**Problem**: Markdown only rendered when message completed, not during streaming. Plain text was shown with a cursor during streaming.

**Solution**: Apply `renderMarkdown()` to streaming message in real-time

**Modified**: `/chatbot/webapp/src/components/ChatBot.vue`

**Before**:
```html
<div v-if="isStreaming" class="message message-assistant streaming">
  <div class="message-content">
    <div class="message-text">{{ currentMessage }}<span class="cursor">▊</span></div>
  </div>
</div>
```

**After**:
```html
<div v-if="isStreaming" class="message message-assistant streaming">
  <div class="message-content">
    <div class="message-text" v-html="renderMarkdown(currentMessage)"></div>
    <span class="cursor">▊</span>
  </div>
</div>
```

**Added cursor styling**:
```css
.cursor {
  animation: blink 1s infinite;
  color: var(--primary-color, #3b82f6);
  margin-left: 2px;
  display: inline-block;
  vertical-align: text-bottom;
}
```

**Added table styling**:
```css
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
```

**Benefits**:
- ✅ Markdown rendered incrementally as tokens arrive (ChatGPT-like experience)
- ✅ Bold, italic, code blocks visible immediately
- ✅ Tables rendered properly with styling
- ✅ Cursor positioned correctly outside HTML content

**Note on table rendering**: Tables may look incomplete until all rows arrive, but this is expected behavior. Basic formatting (bold, italic, code) renders cleanly during streaming.

---

## ANALYSIS_PROMPT: Should It Be Used?

**User Question**: "I saw that there was ANALYSIS_PROMPT, and I was wondering if it could be functional and best practice to send a last LLM call when it returns 'no tools' (prepared for the final answer) with a more specific prompt to tune the final answer without the 'noise' of other prompts, chat history, etc."

**Answer**: **Not recommended for this use case, but valid pattern for other scenarios**

### Two-Stage Approach (ANALYSIS_PROMPT Pattern)

**How it would work**:
1. **Stage 1**: Agent gathers information using tools
2. **Stage 2**: When ready to respond (no tool calls), make a **second LLM call** with:
   - ANALYSIS_PROMPT (focused formatting instructions)
   - Tool results summary
   - User query
   - No tool calls, just final answer synthesis

**Pros**:
- ✅ Focused prompt without "noise" from tool calls
- ✅ Can have different instructions for synthesis vs exploration
- ✅ Can use a cheaper/faster model for final answer

**Cons**:
- ❌ **Adds latency** (one more LLM call = slower response)
- ❌ **Increases token cost** (2 LLM calls instead of 1)
- ❌ **Adds complexity** (need to manage two-stage workflow)
- ❌ **Lost context** (final synthesis might lose nuance from exploration phase)

### Recommendation: Use Ephemeral System Prompt Instead

For **this chatbot**, the **ephemeral system prompt** approach is superior because:

1. **System prompt is always fresh**: Sent with every call, including the final answer
2. **No added latency**: No second LLM call needed
3. **No token overhead**: Only one LLM call per turn
4. **Full context**: Model sees the entire conversation and tool results

**When ANALYSIS_PROMPT pattern is useful**:
- **Complex multi-document RAG systems**: Gather chunks from 10+ documents, then synthesize
- **Research agents**: Explore web for hours, then create final report
- **Different model for synthesis**: Use GPT-4 for research, GPT-3.5-turbo for final answer
- **Very long tool results**: Tool results are 50k+ tokens, need to condense for final answer

**For this chatbot**: Tool results are typically manageable (<10k tokens), so one-stage approach with fresh system prompt is more efficient.

---

## Alternative: Post-Tool Reminder (Not Implemented)

Another approach is to add a **post-tool reminder** after tools execute:

```python
async def execute_tools(state: AgentState) -> AgentState:
    # ... execute tools ...

    # Add formatting reminder after tools
    reminder = SystemMessage(content="""
    REMINDER: Format your response according to Response Policies:
    - Use third-person voice (Open Data Hub has...)
    - Avoid long lists (show 3-5 examples + summary)
    - Prefer markdown tables over bullet lists
    - Keep response concise (2-4 sentences)
    """)

    return {
        **state,
        'messages': [message, reminder],
        'tool_results': tool_results,
    }
```

**Not implemented** because:
- Ephemeral system prompt already provides this
- Adds extra message to conversation
- Redundant with always-fresh system prompt

If bot still doesn't follow formatting after current changes, this could be added as reinforcement.

---

## Expected Behavior After Changes

### Before:
```
User: "List all available datasets"

Bot: "I found 167 datasets in various dataspace:
- Accommodation
- Activity
- Gastronomy
- Event
- Poi
- Article
- Weather
- Parking
[... 40 more items ...]
... (and 117 more)"
```

### After:
```
User: "List all available datasets"

Bot: "Open Data Hub contains 167 datasets across tourism, mobility, and other domains. Here are some examples:

| Dataset | Dataspace |
|---------|-----------|
| Accommodation | tourism |
| Activity | tourism |
| Gastronomy | tourism |
| Parking | mobility |
| Weather | other |

...and 162 more. Would you like to explore a specific category?"
```

**Key improvements**:
- ✅ Third-person voice ("Open Data Hub contains" not "I found")
- ✅ Concise introduction (1 sentence)
- ✅ Table format (easier to scan than list)
- ✅ Limited to 5 examples (not 50)
- ✅ Guiding question at end
- ✅ Markdown rendered incrementally during streaming

---

## Files Modified

### Backend (Python)
1. **`/chatbot/backend/agent/graph.py`**
   - Implemented ephemeral system prompt injection
   - System prompt sent with every LLM call but not stored in history

2. **`/chatbot/backend/agent/prompts.py`**
   - Enhanced Response Policies with ❌/✅ examples
   - Fixed examples to follow rules (no more "I found")
   - Emphasized table usage and concise responses

### Frontend (Vue.js)
3. **`/chatbot/webapp/src/components/ChatBot.vue`**
   - Applied `renderMarkdown()` to streaming message
   - Moved cursor outside of rendered HTML
   - Added table styling for markdown tables
   - Improved cursor positioning

---

## Testing Checklist

1. **System Prompt**:
   - ✅ Check LLM logs - system prompt appears in every request
   - ✅ Verify conversation history doesn't contain system messages
   - ✅ Confirm token count stays reasonable over long conversations

2. **Response Formatting**:
   - ✅ Ask "List all available datasets" → Should use table and third-person voice
   - ✅ Ask "How many datasets?" → Should say "Open Data Hub has..." not "I found..."
   - ✅ Long result lists should be truncated to 3-5 examples

3. **Markdown Rendering**:
   - ✅ Start a query that returns markdown → Bold/italic should render during streaming
   - ✅ Request a table → Table should render with proper styling
   - ✅ Check cursor positioning → Should be after rendered content, not inside

4. **Navigation**:
   - ✅ Verify "See more" button still appears with navigation commands
   - ✅ Auto-navigation works when toggle is enabled

---

## Summary

**System Prompt Strategy**: ✅ **Ephemeral injection is best practice**
- Always send system prompt with every LLM call
- Never store it in conversation history
- Keeps prompt fresh and prioritized
- Saves tokens and prevents dilution

**ANALYSIS_PROMPT**: ❌ **Not recommended for this use case**
- Would add latency and token cost
- Ephemeral system prompt achieves same goal more efficiently
- Useful for other scenarios (complex RAG, long research agents)

**Markdown Rendering**: ✅ **Incremental rendering implemented**
- Markdown rendered as tokens arrive (ChatGPT-like)
- Tables styled properly
- Cursor positioned correctly

**Next Steps**:
- Test bot responses with various queries
- Monitor LLM logs to verify system prompt injection
- Adjust Response Policies if bot still doesn't follow formatting
- Consider post-tool reminder if needed
