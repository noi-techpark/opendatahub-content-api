# Data Cache Implementation

## Problem Statement

The agent was unable to follow the intended workflow:
1. Fetch full data
2. Aggregate it with chosen strategy

**Why it failed**: The `SmartTool` base class applies automatic preprocessing and truncation to tool results BEFORE the agent sees them. When `get_datasets(aggregation_level="full")` returned 102,920 tokens, it was truncated to 201 tokens via emergency summarization, leaving the agent with no data to aggregate.

## Solution: Data Cache Pattern

Implemented a **cache-based architecture** where large data is stored temporarily and referenced by cache keys instead of passed directly through tool results.

### Architecture

```
┌──────────────────────────────────────────────────────────────┐
│  USER: "Which datasets are available? I want a detailed list" │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│  AGENT ITERATION 1: Fetch Data                                │
│                                                                │
│  Tool Call: get_datasets(aggregation_level="full")            │
│                                                                │
│  Backend:                                                      │
│    1. Fetch all 167 datasets from MetaData API               │
│    2. Store in cache: cache.store(data, key="datasets_full") │
│    3. Return metadata + cache_key (NOT the full data):       │
│       {                                                        │
│         "total": 167,                                          │
│         "cache_key": "datasets_full",                         │
│         "message": "Full data stored in cache",               │
│         "next_step": "Use aggregate_data with cache_key...",  │
│         "sample": [<first 2 items>]                           │
│       }                                                        │
│                                                                │
│  Token Count: ~500 tokens (not 102,920!)                     │
│  ✓ No truncation needed - data is in cache                   │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│  AGENT ITERATION 2: Aggregate Data                            │
│                                                                │
│  Agent Decision: Call aggregate_data with cache_key           │
│                                                                │
│  Tool Call: aggregate_data(                                   │
│    strategy="extract_fields",                                 │
│    cache_key="datasets_full",                                 │
│    fields=["Shortname", "ApiDescription", "Dataspace"]       │
│  )                                                             │
│                                                                │
│  Backend:                                                      │
│    1. Load data from cache: cache.get("datasets_full")       │
│    2. Extract only requested fields from 167 datasets         │
│    3. Return reduced result (~10k tokens):                    │
│       {                                                        │
│         "strategy": "extract_fields",                          │
│         "original_count": 167,                                 │
│         "items": [                                             │
│           {"Shortname": "...", "ApiDescription": {...}}, ...  │
│         ]                                                      │
│       }                                                        │
│                                                                │
│  ✓ Fits in token limits with only needed fields              │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│  AGENT ITERATION 3: Respond to User                           │
│                                                                │
│  Agent has all information needed to answer the question      │
│  Returns detailed list with descriptions to user              │
└──────────────────────────────────────────────────────────────┘
```

## Implementation Details

### 1. Data Cache Module (`backend/tools/data_cache.py`)

Created a simple in-memory cache with TTL (time-to-live):

```python
class DataCache:
    """In-memory cache for storing large data between tool calls"""

    def store(self, data, key=None) -> str:
        """Store data and return cache key"""

    def get(self, key: str) -> Any | None:
        """Retrieve data by key"""

    def delete(self, key: str) -> bool:
        """Delete cached data"""
```

**Features**:
- 5-minute TTL (auto-cleanup of expired entries)
- Access counting (tracks how many times data is retrieved)
- Automatic expiration
- Statistics tracking

### 2. Modified `get_datasets` Tool (`backend/tools/content_api.py`)

When `aggregation_level="full"`:

**Before**:
```python
return {"total": len(datasets), "datasets": datasets}
# Result: 102,920 tokens → emergency truncation → agent gets nothing
```

**After**:
```python
cache_key = cache.store(
    data={"total": len(datasets), "datasets": datasets},
    key="datasets_full"
)

return {
    "total": len(datasets),
    "cache_key": cache_key,
    "message": "Full dataset metadata stored in cache",
    "next_step": "Use aggregate_data tool with cache_key='datasets_full'",
    "sample": datasets[:2]  # Show 2 samples for context
}
# Result: ~500 tokens → no truncation needed!
```

### 3. Modified `aggregate_data` Tool (`backend/tools/aggregation.py`)

Added `cache_key` parameter as alternative to `data`:

**Signature**:
```python
async def _aggregate_data(
    strategy: str,
    data: dict | list | None = None,
    cache_key: str | None = None,  # NEW!
    group_by: str | None = None,
    fields: list[str] | None = None,
    limit: int = 100,
    **kwargs
) -> dict:
```

**Logic**:
```python
if cache_key:
    logger.info(f"📦 Loading data from cache: {cache_key}")
    data = cache.get(cache_key)
    if data is None:
        return {"error": f"Cache key '{cache_key}' not found or expired"}
    logger.info(f"✓ Retrieved data from cache")

if data is None:
    return {"error": "Either 'data' or 'cache_key' parameter is required"}

# Then proceed with aggregation strategies...
```

## Logging Enhancements

Added extensive logging to track cache operations:

```
📋 Fetching datasets with aggregation_level='full', dataspace_filter=None
   Retrieved 167 datasets from MetaData API
   ⚠️  FULL data requested (167 datasets) - storing in cache!
💾 Stored data in cache with key: datasets_full
   ✅ Result: 543 chars

🤖 AGENT ITERATION 2
🔧 AGENT DECISION: Call 1 tool(s)
   1. aggregate_data({'strategy': 'extract_fields', 'cache_key': 'datasets_full', ...})

⚙️  EXECUTING TOOLS
▶️  Tool 1/1: aggregate_data
🔄 AGGREGATION START: strategy='extract_fields', cache_key=datasets_full
   📦 Loading data from cache: datasets_full
   ✓ Retrieved data from cache
📊 Processing 167 items with strategy 'extract_fields'
✓ Extracted 3 fields from 167 items
✅ AGGREGATION COMPLETE
```

## Benefits

### 1. ✅ Bypasses Token Limits
- Large data never passes through tool results
- Cache key (~30 chars) instead of data (~100k chars)
- No emergency truncation triggered

### 2. ✅ Agent Has Full Control
- Agent sees the cache_key and next_step instructions
- Agent can choose aggregation strategy based on user question
- Agent can call aggregate_data multiple times with different strategies

### 3. ✅ Efficient Memory Usage
- Data stored once, reused multiple times
- Automatic cleanup after 5 minutes (TTL)
- Can aggregate same data multiple ways without re-fetching

### 4. ✅ Clear Workflow
- Tool descriptions explicitly guide agent to use cache_key
- "next_step" field tells agent what to do
- Sample data provides context without bulk

### 5. ✅ Observable
- Extensive logs show cache operations
- Easy to debug: see when data stored/retrieved
- Access counting shows cache efficiency

## Tool Usage Examples

### Example 1: Detailed List

**User**: "Which datasets are available? I want a detailed list"

**Agent Workflow**:
```python
# Iteration 1: Fetch and cache
get_datasets(aggregation_level="full")
→ Returns: {cache_key: "datasets_full", total: 167, next_step: "..."}

# Iteration 2: Aggregate from cache
aggregate_data(
    strategy="extract_fields",
    cache_key="datasets_full",
    fields=["Shortname", "ApiDescription", "Dataspace", "ApiType"]
)
→ Returns: {items: [{Shortname: "...", ApiDescription: {...}}, ...]}

# Iteration 3: Respond
"Here are all 167 datasets with descriptions: ..."
```

### Example 2: Count by Type

**User**: "How many datasets are there of each type?"

**Agent Workflow**:
```python
# Iteration 1: Fetch and cache
get_datasets(aggregation_level="full")
→ Returns: {cache_key: "datasets_full", total: 167}

# Iteration 2: Count by type
aggregate_data(
    strategy="count_by",
    cache_key="datasets_full",
    group_by="ApiType"
)
→ Returns: {counts: {"content": 142, "timeseries": 25}}

# Iteration 3: Respond
"There are 142 content datasets and 25 timeseries datasets."
```

### Example 3: Multiple Aggregations

**User**: "Tell me about the datasets - types, dataspaces, and show some examples"

**Agent Workflow**:
```python
# Iteration 1: Fetch and cache
get_datasets(aggregation_level="full")
→ cache_key: "datasets_full"

# Iteration 2: Count by type
aggregate_data(strategy="count_by", cache_key="datasets_full", group_by="ApiType")
→ {"content": 142, "timeseries": 25}

# Iteration 3: Group by dataspace
aggregate_data(strategy="group_by", cache_key="datasets_full", group_by="Dataspace")
→ {"tourism": {count: 120, sample: [...]}, "mobility": {...}}

# Iteration 4: Get samples
aggregate_data(strategy="sample", cache_key="datasets_full", limit=5)
→ {items: [<5 full dataset objects>]}

# Iteration 5: Respond with comprehensive answer
```

## Configuration

### Environment Variables

```bash
# No new config needed - cache uses defaults
```

### Cache Settings (in code)

```python
# In data_cache.py
DataCache(ttl_minutes=5)  # 5-minute TTL

# Cache key for datasets
key="datasets_full"  # Fixed key for dataset metadata
```

## Troubleshooting

### Issue: "Cache key not found or expired"

**Cause**:
- Cache TTL expired (> 5 minutes since get_datasets was called)
- Agent used wrong cache key

**Fix**:
- Call `get_datasets(aggregation_level="full")` again to refresh cache
- Check logs for cache_key value

### Issue: Agent not using cache_key

**Symptoms**:
```
🔧 AGENT DECISION: Call 1 tool(s)
   1. aggregate_data({'strategy': 'extract_fields', 'data': <large_object>})
   ❌ No cache_key parameter!
```

**Fix**:
- Tool descriptions clearly state to use cache_key
- Agent should see "next_step" instruction in get_datasets response
- May need to adjust system prompt if agent consistently ignores instructions

### Issue: Cache memory usage

**Mitigation**:
- 5-minute TTL auto-cleans old entries
- Only one cache entry per session (key="datasets_full" is overwritten)
- Monitor with `cache.stats()` if needed

## Comparison: Before vs After

### Before (Failed Workflow)

```
User: "Detailed list please"
→ get_datasets(aggregation_level="full")
→ Returns 102,920 tokens
→ SmartTool truncates to 201 tokens (emergency summarization)
→ Agent receives: {"type": "object", "keys": ["total", "datasets"]}
→ Agent can't aggregate - has no data!
→ Agent makes up answer or asks user to retry
```

### After (Cache Workflow)

```
User: "Detailed list please"
→ get_datasets(aggregation_level="full")
→ Stores data in cache, returns cache_key
→ Agent receives: {cache_key: "datasets_full", next_step: "...", sample: [...]}
→ Agent calls: aggregate_data(strategy="extract_fields", cache_key="datasets_full", fields=[...])
→ Returns reduced data (~10k tokens) with only requested fields
→ Agent answers with complete, accurate information
```

## Future Enhancements

1. **Persistent Cache**: Use Redis instead of in-memory for multi-instance deployments
2. **Cache Warming**: Pre-cache common queries on startup
3. **Smart Key Management**: Generate unique keys per session/user
4. **Cache Statistics**: Expose cache hit rate, size metrics
5. **Configurable TTL**: Make TTL configurable per cache entry
6. **Auto-Cleanup**: Background task to cleanup expired entries

## Summary

The cache-based architecture solves the fundamental problem: **large data cannot pass through tool results due to token limits**.

By storing data in a temporary cache and passing only cache keys through tool results, we enable the agent to:
- Fetch full data without truncation
- Aggregate it in subsequent calls with chosen strategies
- Reuse the same data for multiple aggregations
- Follow the intended inspect-then-aggregate workflow

This pattern can be applied to other tools beyond `get_datasets` whenever large API responses need agent-controlled aggregation.

---

**Last Updated**: 2025-10-19
**Version**: 1.0
**Author**: OdhDiscovery Team
