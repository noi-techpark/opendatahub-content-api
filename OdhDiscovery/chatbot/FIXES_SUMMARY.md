# Agent Behavior Fixes - Summary

## Problem

**User Query**: "Which datasets are available? I want a detailed list of the dataset names"

**What Happened**:
- Agent returned only 2 sample names (Museums, Traffic and Transport)
- Agent said "output is truncated and only shows a sample"
- Agent suggested user run get_datasets again
- **But the agent had access to all 167 datasets!**

**What Should Have Happened**:
- Agent returns ALL 167 dataset names in a formatted list
- Agent provides complete answer immediately
- No truncation, no samples, full detailed list

---

## Root Causes Identified

### 1. Agent Not Following Multi-Step Workflows

**The Workflow** (should be):
```
Step 1: get_datasets(aggregation_level="full")
        → Returns cache_key="datasets_full" + 2 samples

Step 2: aggregate_data(cache_key="datasets_full")
        → Returns all 167 datasets with selected fields

Step 3: Respond to user with complete list
```

**What Actually Happened**:
```
Step 1: get_datasets(aggregation_level="full") ✓
        → Returns cache_key="datasets_full" + 2 samples

Step 2: [SKIPPED - Agent responded directly] ❌

Step 3: Respond with only 2 samples ❌
```

**Why**: System prompt had no explicit rules about completing multi-step workflows. Agent saw 2 samples and thought "good enough" instead of following the `next_step` instruction.

### 2. Insufficient Output Token Limit

**Configuration**: `LLM_MAX_TOKENS=4096`

**Problem**:
- Listing all 167 datasets requires ~5000-6000 tokens
- Agent might self-limit to avoid exceeding budget
- Results in incomplete answers

### 3. Weak System Prompt

**Missing**:
- No mention of `aggregate_data` tool
- No rules about cache_key workflows
- No enforcement of multi-step processes
- No guidance on when to use which tool

---

## Fixes Applied

### Fix 1: Strict System Prompt with MANDATORY Rules

**File**: `backend/agent/prompts.py`

**Added Section**:
```python
## CRITICAL WORKFLOW RULES - FOLLOW THESE EXACTLY!

### Rule 1: ALWAYS Complete Multi-Step Workflows
When a tool returns a "cache_key" or "next_step" instruction,
you MUST call the next tool before responding!

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

**Impact**:
- ✅ Agent now has explicit, mandatory rules
- ✅ IF/THEN logic LLMs understand
- ✅ Cannot skip steps without violating clear rules

### Fix 2: Increased Output Token Limit

**File**: `.env`

**Change**:
```bash
# Before
LLM_MAX_TOKENS=4096

# After
LLM_MAX_TOKENS=8192
```

**Impact**:
- ✅ Enough tokens to list all 167 datasets
- ✅ Can include descriptions and formatting
- ✅ No self-limiting due to token budget

### Fix 3: Tool Documentation in System Prompt

**Added**:
```
3. **Data Aggregation Tools** - Reduce large responses intelligently
   - aggregate_data: Apply various strategies (AUTO mode - just provide cache_key!)
```

**Impact**:
- ✅ Agent knows tool exists
- ✅ Agent knows how to use it (AUTO mode)
- ✅ Agent knows when to use it (after cache_key)

### Fix 4: Behavior Guidelines

**Added**:
```
### Your Behavior

1. **Complete Workflows**: NEVER skip steps in multi-step workflows
2. **Use AUTO Mode**: For aggregate_data, just provide cache_key
3. **Be Complete**: If user asks for "all", provide ALL items
4. **Follow Instructions**: If tool says "next_step: ...", DO IT!
```

**Impact**:
- ✅ Clear expectations for agent behavior
- ✅ Emphasis on completeness
- ✅ Must follow next_step instructions

---

## Expected Behavior After Fixes

### Test Query

"Which datasets are available? I want a detailed list of the dataset names"

### Expected Flow

**Iteration 1**: Fetch and Cache
```
🤖 AGENT ITERATION 1
📥 USER QUERY: Which datasets are available? I want a detailed list
🔧 AGENT DECISION: Call 1 tool(s)
   1. get_datasets({'aggregation_level': 'full'})

▶️  Tool 1/1: get_datasets
📋 Fetching datasets with aggregation_level='full'
   Retrieved 167 datasets from MetaData API
   ⚠️  FULL data requested (167 datasets) - storing in cache!
💾 Stored data in cache with key: datasets_full
   ✅ Result: ~500 chars

Result includes:
{
  "total": 167,
  "cache_key": "datasets_full",
  "message": "Full dataset metadata (167 items) stored in cache.",
  "next_step": "Use aggregate_data tool with cache_key='datasets_full'",
  "sample": [<2 items>]
}
```

**Iteration 2**: Aggregate
```
🤖 AGENT ITERATION 2
🔧 AGENT DECISION: Call 1 tool(s)
   1. aggregate_data({'cache_key': 'datasets_full'})  ← FOLLOWS next_step!

▶️  Tool 1/1: aggregate_data
🔄 AGGREGATION: cache_key=datasets_full, strategy='auto'
   📦 Loading data from cache: datasets_full
   ✓ Retrieved data from cache
📊 Processing 167 items (type: datasets)
   🤖 AUTO mode: detecting best strategy...
   → Detected: extract_fields (dataset metadata)
   → Auto-selected fields: ['Shortname', 'ApiDescription', 'Dataspace', 'ApiType', 'ApiUrl']
   ✓ Using strategy: 'extract_fields'
✓ Extracted 5 fields from 167 items
✅ AGGREGATION COMPLETE
   ✅ Result: ~15,000 chars

Result includes ALL 167 datasets with:
- Shortname
- ApiDescription
- Dataspace
- ApiType
- ApiUrl
```

**Iteration 3**: Respond
```
🤖 AGENT ITERATION 3
💬 AGENT DECISION: Respond to user (no tool calls)

Agent Response:
"Here are all 167 available datasets from Open Data Hub:

Tourism Datasets:
1. Accommodation - Hotels, B&Bs, apartments (tourism, content)
2. ODHActivityPoi - Activities and points of interest (tourism, content)
3. Gastronomy - Restaurants and food establishments (tourism, content)
... (164 more)

Mobility Datasets:
165. Parking - Parking facilities data (mobility, timeseries)
166. EChargingStation - Electric charging stations (mobility, content)
167. Bicycle - Bicycle counting data (mobility, timeseries)

You can query any of these datasets using their Shortname."
```

---

## Verification Checklist

Use this to verify the fixes are working:

### 1. System Prompt Check

```bash
grep -A 5 "CRITICAL WORKFLOW RULES" backend/agent/prompts.py
```

✅ Should show the mandatory rules section

### 2. Token Limit Check

```bash
grep "LLM_MAX_TOKENS" .env
```

✅ Should show `LLM_MAX_TOKENS=8192`

### 3. Log Pattern Check

Run test query and check logs:

```bash
docker-compose logs backend | grep -E "🤖|🔧|💬"
```

✅ Should show 3 iterations:
- Iteration 1: get_datasets
- Iteration 2: aggregate_data
- Iteration 3: respond

### 4. Response Completeness Check

Run test and count dataset names in response:

```bash
python test_websocket.py "Which datasets are available? I want a detailed list"
```

✅ Response should contain all 167 dataset names

### 5. Cache Workflow Check

```bash
docker-compose logs backend | grep "cache_key"
```

✅ Should show:
- "Stored data in cache with key: datasets_full"
- "Loading data from cache: datasets_full"

---

## What Each Fix Solves

| Problem | Fix | How It Helps |
|---------|-----|-------------|
| Agent skips aggregate_data | Mandatory workflow rules | Forces agent to check for cache_key |
| Only 2 samples shown | Rule 3: Provide Complete Answers | Agent must return ALL items |
| Truncated output | Increased LLM_MAX_TOKENS to 8192 | Enough space for full list |
| Agent doesn't know tool | Tool docs in system prompt | Agent learns aggregate_data exists |
| No enforcement | IF/THEN pseudo-code | Clear logic agent must follow |
| Vague instructions | "NEVER skip steps" | Explicit prohibition |

---

## Testing After Fixes

### Test 1: Basic List

**Query**: "Which datasets are available? I want a detailed list"

**Expected**:
- All 167 dataset names
- Organized by dataspace
- Includes descriptions

### Test 2: Just Names

**Query**: "Give me a list of all dataset names"

**Expected**:
- All 167 Shortnames
- No extra fields (AUTO mode with fields=["Shortname"])

### Test 3: Count by Category

**Query**: "How many datasets per dataspace?"

**Expected**:
- get_datasets(full) → cache
- aggregate_data(group_by="Dataspace")
- Counts: {tourism: 120, mobility: 30, ...}

### Test 4: Examples

**Query**: "Show me 10 example datasets"

**Expected**:
- aggregate_data(limit=10) OR get_datasets(list) with first 10
- 10 datasets with details

---

## Monitoring

### Watch Real-Time Logs

```bash
# Show workflow steps
docker-compose logs -f backend | grep -E "🤖|🔧|💬|▶️|🔄"

# Show only agent decisions
docker-compose logs -f backend | grep "AGENT DECISION"

# Show cache operations
docker-compose logs -f backend | grep -E "💾|📦"
```

### Check for Problems

**Bad Pattern** (skipped step):
```
🤖 AGENT ITERATION 1
🔧 Call get_datasets

🤖 AGENT ITERATION 2
💬 Respond to user  ← WRONG! Should call aggregate_data first!
```

**Good Pattern** (complete workflow):
```
🤖 AGENT ITERATION 1
🔧 Call get_datasets

🤖 AGENT ITERATION 2
🔧 Call aggregate_data  ← CORRECT!

🤖 AGENT ITERATION 3
💬 Respond to user
```

---

## Summary

### What Was Wrong

1. ❌ Agent skipping multi-step workflows
2. ❌ Token limit too small for complete answers
3. ❌ No documentation of new tools in system prompt
4. ❌ No enforcement of workflow rules

### What Was Fixed

1. ✅ Added CRITICAL WORKFLOW RULES with mandatory IF/THEN logic
2. ✅ Increased LLM_MAX_TOKENS from 4096 to 8192
3. ✅ Documented aggregate_data and cache workflow in system prompt
4. ✅ Added explicit behavioral guidelines

### Expected Result

- ✅ Agent MUST call aggregate_data when receiving cache_key
- ✅ Agent has enough tokens to list all 167 datasets
- ✅ Agent knows all tools and how to use them
- ✅ Complete, accurate answers to user questions

### How to Verify

Run the same query that failed before:

```bash
python test_websocket.py "Which datasets are available? I want a detailed list"
```

Expected response: **ALL 167 dataset names** with descriptions, organized clearly.

---

## Additional Documentation

See also:
- `TROUBLESHOOTING_AGENT_BEHAVIOR.md` - Detailed diagnostic guide
- `TOOLS_GUIDE.md` - Complete tool usage guide
- `CACHE_IMPLEMENTATION.md` - How the cache works
- `AGGREGATION_WORKFLOW.md` - Workflow patterns

---

**Last Updated**: 2025-10-21
**Version**: 1.0
**Author**: OdhDiscovery Team
