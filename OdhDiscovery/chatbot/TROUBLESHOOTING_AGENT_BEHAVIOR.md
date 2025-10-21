# Troubleshooting Agent Behavior

## Problem: Agent Not Following Multi-Step Workflows

### Symptoms

**User Query**: "Which datasets are available? I want a detailed list of the dataset names"

**Expected Behavior**:
1. Agent calls `get_datasets(aggregation_level="full")`
2. Receives cache_key: "datasets_full"
3. Agent calls `aggregate_data(cache_key="datasets_full")`
4. Receives complete list with all 167 dataset names
5. Agent responds with full list

**Actual Behavior** (Before Fix):
1. Agent calls `get_datasets(aggregation_level="full")` ✓
2. Receives cache_key: "datasets_full" ✓
3. **Agent responds immediately with only 2 sample names** ❌
4. Never calls `aggregate_data` ❌

**Agent Response**:
```
Based on the output, here is a detailed list of the datasets names:

1. Museums
2. Traffic and Transport

Please note that the output contains only two datasets, but the total number
of datasets is 167. The output is truncated and only shows a sample.
```

**User Expectation**: All 167 dataset names!

---

## Root Cause Analysis

### Issue 1: Agent Ignoring Multi-Step Instructions

**What the tool returned**:
```json
{
  "total": 167,
  "cache_key": "datasets_full",
  "message": "Full dataset metadata (167 items) stored in cache.",
  "next_step": "Use aggregate_data tool with cache_key='datasets_full' to aggregate this data",
  "sample": [
    { "Shortname": "Museums", ... },
    { "Shortname": "Traffic and Transport", ... }
  ]
}
```

**Problem**: Agent saw the 2 samples and decided it had enough information to answer, completely ignoring:
- The `cache_key` field
- The `next_step` instruction
- The fact that user asked for "detailed list" and "all" names

**Why This Happened**:
1. **Weak System Prompt**: No explicit rules about multi-step workflows
2. **LLM Optimization**: LLM tries to minimize tool calls (efficient but wrong here)
3. **Sample Confusion**: Seeing samples made LLM think it could answer
4. **No Enforcement**: Nothing forced the agent to check for cache_key

### Issue 2: Insufficient Output Tokens

**Configuration**:
```python
LLM_MAX_TOKENS=4096
```

**Problem**: Even if agent wanted to list all 167 datasets, 4096 tokens might not be enough:
- Average dataset name: ~20 characters
- 167 datasets × 20 chars = ~3340 chars
- Plus formatting, descriptions, etc. = ~5000+ tokens needed

**Result**: Agent might self-limit to avoid exceeding token budget

### Issue 3: Missing Tool Awareness

**Old System Prompt** didn't mention:
- `aggregate_data` tool exists
- Cache-based workflow pattern
- How to use cache_key
- When to call aggregate_data

**Result**: Agent had no guidance on the new tools and workflows

---

## The Fixes

### Fix 1: Strict System Prompt with Mandatory Rules

**Added to `backend/agent/prompts.py`**:

```python
## CRITICAL WORKFLOW RULES - FOLLOW THESE EXACTLY!

### Rule 1: ALWAYS Complete Multi-Step Workflows
When a tool returns a "cache_key" or "next_step" instruction,
you MUST call the next tool before responding!

Example:
get_datasets(aggregation_level="full")
→ Returns: {cache_key: "datasets_full", next_step: "Use aggregate_data..."}

YOU MUST NOW CALL: aggregate_data(cache_key="datasets_full")
DO NOT respond to user yet! Complete the workflow first!

### Rule 2: Cache Key Workflow (MANDATORY)
IF tool_result contains "cache_key":
    THEN call aggregate_data(cache_key=<the_key>)
    WAIT for result
    THEN respond to user
ELSE:
    Respond to user

### Rule 3: Provide Complete Answers
When user asks for a "detailed list" or "all items":
- Use aggregation_level="full" to get complete data
- Use aggregate_data to extract needed fields
- Return ALL items, not just a sample
- Format the list clearly
```

**Impact**: Agent now has **explicit, mandatory rules** in pseudo-code format that even LLMs understand!

### Fix 2: Increased Output Token Limit

**Changed in `.env`**:
```bash
# Before
LLM_MAX_TOKENS=4096

# After
LLM_MAX_TOKENS=8192
```

**Impact**: Agent can now generate responses with all 167 dataset names without running out of tokens.

### Fix 3: Tool Documentation in System Prompt

**Added**:
```
3. **Data Aggregation Tools** - Reduce large responses intelligently
   - aggregate_data: Apply various strategies (AUTO mode - just provide cache_key!)
```

**Impact**: Agent knows the tool exists and how to use it.

### Fix 4: Behavior Guidelines

**Added**:
```
### Your Behavior

1. **Complete Workflows**: NEVER skip steps in multi-step workflows
2. **Use AUTO Mode**: For aggregate_data, just provide cache_key
3. **Be Complete**: If user asks for "all", provide ALL items
4. **Follow Instructions**: If tool says "next_step: ...", DO IT!
```

**Impact**: Clear behavioral expectations for the agent.

---

## How to Diagnose Similar Issues

### Step 1: Check the Logs

Look for the agent decision pattern:

```bash
docker-compose logs -f backend | grep "🤖\|🔧\|💬"
```

**Good Pattern** (Multi-step workflow):
```
🤖 AGENT ITERATION 1
🔧 AGENT DECISION: Call 1 tool(s)
   1. get_datasets({'aggregation_level': 'full'})

🤖 AGENT ITERATION 2
🔧 AGENT DECISION: Call 1 tool(s)
   1. aggregate_data({'cache_key': 'datasets_full'})

🤖 AGENT ITERATION 3
💬 AGENT DECISION: Respond to user (no tool calls)
```

**Bad Pattern** (Skipped step):
```
🤖 AGENT ITERATION 1
🔧 AGENT DECISION: Call 1 tool(s)
   1. get_datasets({'aggregation_level': 'full'})

🤖 AGENT ITERATION 2
💬 AGENT DECISION: Respond to user (no tool calls)  ← WRONG! Skipped aggregate_data
```

### Step 2: Check Tool Results

Look for cache_key in tool results:

```bash
docker-compose logs backend | grep "cache_key"
```

**If you see**:
```
"cache_key": "datasets_full"
```

**Then next iteration MUST show**:
```
aggregate_data({'cache_key': 'datasets_full'})
```

**If it doesn't** → Agent is skipping the workflow!

### Step 3: Check Token Limits

Look for token count in logs:

```bash
docker-compose logs backend | grep "Result:"
```

**Example**:
```
✅ Result: 3259 chars
```

If this is followed by immediate response (no aggregate_data call), agent skipped the step.

### Step 4: Check System Prompt

Verify system prompt has the CRITICAL WORKFLOW RULES:

```bash
grep -A 10 "CRITICAL WORKFLOW RULES" backend/agent/prompts.py
```

Should show the mandatory rules.

### Step 5: Check LLM Token Limit

```bash
grep "LLM_MAX_TOKENS" .env
```

Should be at least 8192 for full dataset lists.

---

## Common Failure Patterns

### Pattern 1: "Premature Response"
**Symptom**: Agent responds after first tool call
**Cause**: No multi-step workflow enforcement
**Fix**: Add CRITICAL WORKFLOW RULES to system prompt

### Pattern 2: "Sample Confusion"
**Symptom**: Agent uses samples as complete answer
**Cause**: Tool returning samples confuses agent
**Fix**: System prompt must say "samples are NOT complete data"

### Pattern 3: "Token Budget Anxiety"
**Symptom**: Agent says "output is truncated" even though full data available
**Cause**: LLM_MAX_TOKENS too small
**Fix**: Increase to 8192 or higher

### Pattern 4: "Tool Ignorance"
**Symptom**: Agent doesn't know aggregate_data exists
**Cause**: Tool not mentioned in system prompt
**Fix**: Add tool descriptions to system prompt

### Pattern 5: "Optimization Over-zealousness"
**Symptom**: Agent minimizes tool calls to be "efficient"
**Cause**: LLM trying to reduce latency/cost
**Fix**: Explicit rules that MANDATE certain tool calls

---

## Testing the Fixes

### Test 1: Basic Workflow

**Query**: "Which datasets are available? I want a detailed list"

**Expected Log Pattern**:
```
🤖 AGENT ITERATION 1
🔧 Call get_datasets(aggregation_level='full')
💾 Stored in cache: datasets_full

🤖 AGENT ITERATION 2
🔧 Call aggregate_data(cache_key='datasets_full')
🔄 AUTO mode: extract_fields
✓ Extracted 5 fields from 167 items

🤖 AGENT ITERATION 3
💬 Respond to user
```

**Expected Response**: List of all 167 dataset names with descriptions

### Test 2: Explicit Fields

**Query**: "Get all dataset names and their dataspaces"

**Expected**:
- Iteration 1: get_datasets(full)
- Iteration 2: aggregate_data(cache_key, fields=["Shortname", "Dataspace"])
- Iteration 3: Respond with all 167 items

### Test 3: Count Query

**Query**: "How many datasets per dataspace?"

**Expected**:
- Iteration 1: get_datasets(full)
- Iteration 2: aggregate_data(cache_key, group_by="Dataspace")
- Iteration 3: Respond with counts

### Test 4: Sample Query

**Query**: "Show me 10 example datasets"

**Expected**:
- Iteration 1: get_datasets(full) OR get_datasets(list)
- Option A: aggregate_data(cache_key, limit=10)
- Option B: Directly use list response (if aggregation_level="list")
- Iteration 2/3: Respond with 10 examples

---

## Configuration Checklist

Use this checklist to verify correct setup:

- [ ] `.env` has `LLM_MAX_TOKENS=8192` (or higher)
- [ ] `backend/agent/prompts.py` includes CRITICAL WORKFLOW RULES section
- [ ] System prompt mentions `aggregate_data` tool
- [ ] System prompt has Rule 2: Cache Key Workflow
- [ ] System prompt says "NEVER skip steps"
- [ ] Tool descriptions include cache_key workflow examples
- [ ] Backend logs show emoji markers (🤖 🔧 💬)

---

## Advanced Debugging

### Enable Detailed Logging

Add to your test script:

```python
import logging
logging.basicConfig(level=logging.DEBUG)
```

### Monitor LLM Decisions

```bash
# Show only agent decisions
docker-compose logs -f backend | grep "AGENT DECISION"

# Show only tool calls
docker-compose logs -f backend | grep "▶️"

# Show complete workflow
docker-compose logs -f backend | grep -E "🤖|🔧|💬|▶️|🔄"
```

### Check Message History

Add logging in `agent/graph.py` to see full conversation:

```python
logger.info(f"Messages in context: {len(messages)}")
for msg in messages[-5:]:  # Last 5 messages
    logger.info(f"  {type(msg).__name__}: {str(msg.content)[:100]}")
```

### Inspect Cache State

Add endpoint in `main.py`:

```python
@app.get("/debug/cache")
async def debug_cache():
    from tools.data_cache import get_cache
    cache = get_cache()
    return cache.stats()
```

---

## Prevention Strategies

### 1. Always Use Explicit Rules
Don't rely on LLM "understanding" - use IF/THEN pseudo-code in system prompts.

### 2. Validate Tool Outputs
Add assertions in tools to check cache_key is being passed correctly.

### 3. Monitor Workflows
Set up alerts for patterns like "cache_key returned but aggregate_data not called".

### 4. Test Edge Cases
- Very large lists (200+ items)
- Nested aggregations
- Multiple cache keys in same session
- Cache expiration scenarios

### 5. Use Structured Outputs
Consider using LLM structured outputs to enforce tool calling patterns.

---

## Summary

**The Problem**: Agent was skipping the aggregate_data step despite receiving cache_key and next_step instructions.

**Root Causes**:
1. Weak system prompt with no workflow enforcement
2. Insufficient output tokens (4096 → 8192)
3. Missing tool documentation
4. No mandatory rules for multi-step workflows

**The Solution**:
1. ✅ Added CRITICAL WORKFLOW RULES with pseudo-code
2. ✅ Increased LLM_MAX_TOKENS to 8192
3. ✅ Documented aggregate_data in system prompt
4. ✅ Added explicit behavior guidelines

**The Result**:
- Agent now MUST call aggregate_data when it sees cache_key
- Agent has enough tokens to list all 167 datasets
- Agent knows the tools and how to use them
- Workflows are enforced, not suggested

**Verification**:
```bash
# Run test query
python test_websocket.py "Which datasets are available? I want a detailed list"

# Check logs for 3-step workflow
docker-compose logs backend | grep -E "🤖|🔧|💬"

# Verify response has all 167 names
```

---

**Last Updated**: 2025-10-19
**Version**: 1.0
**Author**: OdhDiscovery Team
