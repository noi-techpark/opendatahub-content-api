# Testing Environment Setup Summary

## What Was Created

### 1. REST API Endpoint for Direct Queries

**File**: `backend/main.py`

Added `/query` POST endpoint that accepts queries directly without WebSocket:

```python
POST http://localhost:8001/query
{
  "query": "Your question here",
  "include_debug": true
}

Response:
{
  "response": "Agent's answer",
  "navigation_commands": [...],
  "iterations": 3,
  "tool_calls": [
    {"name": "get_datasets", "args": {...}},
    {"name": "flatten_data", "args": {...}}
  ],
  "debug_info": {...}
}
```

**Benefits**:
- Easier testing than WebSocket
- Returns structured tool call information
- Includes debug data
- Can be used with curl, httpx, or any HTTP client

---

### 2. Integration Test Framework

**File**: `backend/test_integration.py` (690 lines)

Comprehensive test suite with 6 test cases:

1. **Test 1: Simple Field Extraction**
   - Validates: No AUTO mode, explicit strategy and fields
   - Query: "List all dataset names"

2. **Test 2: Pandas Filtering**
   - Validates: inspect → flatten → dataframe_query workflow
   - Query: "Show me all active hotels"

3. **Test 3: Pandas Sorting**
   - Validates: Sort operation with pandas, not aggregate_data
   - Query: "List datasets sorted by name"

4. **Test 4: Pandas Grouping**
   - Validates: Groupby operation with pandas
   - Query: "How many datasets per dataspace?"

5. **Test 5: Complex Chained Operations**
   - Validates: Multiple dataframe_query calls chained
   - Query: "Show me top 5 active hotels sorted by name"

6. **Test 6: Structure Inspection**
   - Validates: inspect_api_structure called before operations
   - Query: "What fields are available in the datasets?"

**Features**:
- Automatic expectation validation
- Tool call sequence checking
- Parameter validation
- Detailed error reporting
- JSON report generation
- Execution time tracking

---

### 3. Automated Test Runner Script

**File**: `run_integration_tests.sh` (executable)

Shell script that:
1. Checks prerequisites (Python, packages)
2. Starts backend server on port 8001
3. Waits for backend to be healthy
4. Runs all integration tests
5. Captures backend logs with emoji markers
6. Analyzes logs for patterns
7. Generates comprehensive reports
8. Shuts down backend cleanly

**Usage**:
```bash
cd /home/mroggia/git/opendatahub-content-api/OdhDiscovery/chatbot
./run_integration_tests.sh
```

---

### 4. Testing Documentation

**File**: `TESTING_README.md`

Complete testing guide including:
- Quick start instructions
- Detailed test case descriptions
- Expected tool sequences
- Output interpretation guide
- Debugging tips
- Troubleshooting common issues
- CI/CD integration examples

---

## How It Works

### Test Execution Flow

```
┌─────────────────────────────────────────────────┐
│ 1. run_integration_tests.sh                     │
│    - Checks prerequisites                        │
│    - Starts backend on port 8001                │
│    - Captures logs to test_logs/backend_*.log   │
└──────────────────┬──────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────┐
│ 2. test_integration.py                          │
│    - Sends HTTP POST to /query endpoint         │
│    - Receives response with tool_calls          │
│    - Validates expectations                      │
│    - Records results                             │
└──────────────────┬──────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────┐
│ 3. Backend Agent Execution                      │
│    - Agent receives query                        │
│    - Calls tools (logged with emoji markers)   │
│    - Returns response                            │
└──────────────────┬──────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────┐
│ 4. Analysis & Reporting                         │
│    - Test results summary                        │
│    - Log analysis (tool usage, errors)          │
│    - JSON report generation                      │
│    - Success/failure determination               │
└─────────────────────────────────────────────────┘
```

### Log Analysis

Backend logs use emoji markers for easy pattern matching:

- `🤖 AGENT ITERATION X` - Agent thinking/deciding
- `🔧 AGENT DECISION: Call N tool(s)` - Tool selection
- `⚙️  EXECUTING TOOLS` - Tool execution start
- `▶️  Tool X/N: tool_name` - Individual tool execution
- `🔨 FLATTEN:` - flatten_data operations
- `🐼 DATAFRAME_QUERY:` - dataframe_query operations
- `🔧 AGGREGATION:` - aggregate_data operations
- `✅ AGGREGATION COMPLETE` - Successful completion
- `❌` - Errors
- `⚠️` - Warnings

**Example Log Grep Commands**:
```bash
# Find all agent decisions
grep "🔧 AGENT DECISION" test_logs/backend_*.log

# Find pandas operations
grep "🐼" test_logs/backend_*.log

# Count tool calls
grep "▶️  Tool" test_logs/backend_*.log | wc -l

# Find AUTO mode usage (should be 0!)
grep "strategy.*auto" test_logs/backend_*.log
```

---

## Validation Strategy

### What Tests Validate

**1. No AUTO Mode**
```python
# BAD (will fail test)
aggregate_data(cache_key="...", strategy="auto")

# GOOD (will pass test)
aggregate_data(
    cache_key="...",
    strategy="extract_fields",
    fields=["Shortname", "Active"]
)
```

**2. Pandas Workflow for Complex Operations**
```python
# BAD for filtering (will fail test)
aggregate_data(cache_key="...", strategy="extract_fields", fields=[...])

# GOOD for filtering (will pass test)
flatten_data(cache_key="...", fields=[...])
→ dataframe_query(df_key="...", operation="filter", condition="...")
```

**3. inspect_api_structure Usage**
```python
# BAD (will fail test if >100 items)
get_datasets(full) → flatten_data(...)

# GOOD (will pass test)
get_datasets(full) → inspect_api_structure(...) → flatten_data(...)
```

**4. Tool Call Sequencing**
```python
# BAD (wrong order - will fail)
dataframe_query(operation="filter") → flatten_data(...)

# GOOD (correct order - will pass)
flatten_data(...) → dataframe_query(operation="filter")
```

**5. Parameter Completeness**
```python
# BAD (missing required params - will fail)
dataframe_query(df_key="...", operation="filter")  # No condition!

# GOOD (all params - will pass)
dataframe_query(
    df_key="...",
    operation="filter",
    condition="Active == True"
)
```

---

## Output Files

### During Test Run

```
chatbot/
├── test_logs/
│   ├── backend_20251021_164530.log     # Backend server logs
│   └── test_20251021_164530.log        # Test execution logs
│
└── backend/
    ├── integration_test.log             # Detailed test framework logs
    └── integration_test_report_20251021_164530.json  # JSON report
```

### Example JSON Report

```json
{
  "summary": {
    "total": 6,
    "passed": 4,
    "partial": 2,
    "failed": 0
  },
  "tests": [
    {
      "name": "Simple Field Extraction",
      "status": "✅ PASS",
      "query": "List all dataset names",
      "iterations": 2,
      "tool_calls": [
        {"name": "get_datasets", "args": {"aggregation_level": "full"}},
        {
          "name": "aggregate_data",
          "args": {
            "cache_key": "datasets_full",
            "strategy": "extract_fields",
            "fields": ["Shortname"]
          }
        }
      ],
      "expectations": {
        "get_datasets called": true,
        "aggregate_data called": true,
        "aggregate_data has strategy": true,
        "aggregate_data has fields": true,
        "NO AUTO mode": true
      },
      "errors": [],
      "warnings": [],
      "execution_time": 3.45
    }
    // ... more tests
  ]
}
```

---

## Quick Commands

### Run Tests
```bash
./run_integration_tests.sh
```

### Check Test Status
```bash
# Last test run status
echo $?  # 0 = success, non-zero = failure

# View last report
ls -t backend/integration_test_report_*.json | head -1 | xargs cat | jq '.summary'
```

### Analyze Logs
```bash
# Count agent iterations
grep -c "🤖 AGENT ITERATION" test_logs/backend_*.log

# List all tools used
grep "▶️  Tool" test_logs/backend_*.log | awk '{print $NF}' | sort | uniq -c

# Find errors
grep "❌" test_logs/backend_*.log

# Check for AUTO mode (should be 0)
grep -c "strategy.*auto" test_logs/backend_*.log
```

### Manual Testing
```bash
# Start backend
cd backend
python -m uvicorn main:app --port 8001 --log-level debug

# In another terminal, test with curl
curl -X POST http://localhost:8001/query \
  -H "Content-Type: application/json" \
  -d '{"query": "List all datasets", "include_debug": true}' | jq
```

---

## Success Criteria

Tests are considered successful when:

✅ All 6 test cases pass
✅ No errors in backend logs
✅ No AUTO mode usage detected
✅ inspect_api_structure called for large data
✅ Pandas workflow used for filter/sort/groupby
✅ All tool calls have required parameters
✅ Tool call sequences are correct

---

## Failure Analysis Workflow

If tests fail:

1. **Check Test Output**
   ```bash
   cat test_logs/test_*.log | grep "❌ FAIL"
   ```

2. **Review Expectations**
   ```bash
   cat backend/integration_test_report_*.json | jq '.tests[] | select(.status | contains("FAIL"))'
   ```

3. **Analyze Backend Logs**
   ```bash
   grep "🤖 AGENT DECISION" test_logs/backend_*.log
   ```

4. **Identify Root Cause**
   - AUTO mode used? → Fix aggregate_data tool description
   - Wrong tool sequence? → Update system prompt Rule 4
   - Missing inspection? → Strengthen system prompt Rule 2
   - Wrong parameters? → Improve tool documentation

5. **Apply Fix**
   - Edit `backend/agent/prompts.py` for prompt fixes
   - Edit `backend/tools/*.py` for tool fixes

6. **Re-run Tests**
   ```bash
   ./run_integration_tests.sh
   ```

---

## Integration with Development

### Pre-commit Hook (Optional)

Create `.git/hooks/pre-commit`:
```bash
#!/bin/bash
cd OdhDiscovery/chatbot
./run_integration_tests.sh
if [ $? -ne 0 ]; then
    echo "Integration tests failed! Commit aborted."
    exit 1
fi
```

### CI/CD Integration

Add to GitHub Actions, GitLab CI, or Jenkins:
```yaml
test:
  script:
    - cd OdhDiscovery/chatbot
    - ./run_integration_tests.sh
  artifacts:
    when: always
    paths:
      - chatbot/test_logs/
      - chatbot/backend/integration_test_report_*.json
```

---

## Next Steps

After successful test runs:

1. **Review Partial Results**
   - Understand why some tests are partial
   - Determine if warnings are acceptable

2. **Add More Tests**
   - Edge cases (empty results, errors)
   - Performance tests (response time)
   - Load tests (concurrent requests)

3. **Automate in CI/CD**
   - Run on every commit
   - Block merges if tests fail

4. **Monitor in Production**
   - Track tool usage patterns
   - Alert on unexpected behavior

---

## Benefits of This Setup

✅ **Automated Validation**: No manual testing needed
✅ **Regression Detection**: Catch agent behavior changes
✅ **Documentation**: Tests document expected behavior
✅ **Confidence**: Deploy knowing agent works correctly
✅ **Debugging**: Detailed logs show exactly what agent did
✅ **Metrics**: Track tool usage and performance over time

---

## Troubleshooting

See `TESTING_README.md` for comprehensive troubleshooting guide.

Quick fixes:

- **Port 8001 in use**: `lsof -ti:8001 | xargs kill -9`
- **Tests hang**: Check LLM API key in `.env`
- **Import errors**: `pip install -r backend/requirements.txt httpx`
- **Permission denied**: `chmod +x run_integration_tests.sh`
