# Integration Testing Guide

## Overview

This testing framework validates the chatbot's agent behavior, especially the new pandas-based workflow for data manipulation.

## Quick Start

### Run All Tests

```bash
cd /home/mroggia/git/opendatahub-content-api/OdhDiscovery/chatbot
./run_integration_tests.sh
```

This script will:
1. Check prerequisites
2. Start backend server
3. Run all integration tests
4. Capture and analyze logs
5. Generate detailed reports
6. Shut down backend

### Run Tests Manually

If you prefer to run backend and tests separately:

```bash
# Terminal 1: Start backend
cd backend
python -m uvicorn main:app --host 0.0.0.0 --port 8001 --log-level debug

# Terminal 2: Run tests
cd backend
python test_integration.py --url http://localhost:8001
```

## Test Cases

### Test 1: Simple Field Extraction
**Query**: "List all dataset names"

**Validates**:
- ✅ get_datasets called with aggregation_level="full"
- ✅ aggregate_data called with explicit strategy
- ✅ aggregate_data has fields parameter
- ❌ NO AUTO mode usage

**Expected Tool Sequence**:
```
get_datasets → aggregate_data(strategy="extract_fields", fields=["Shortname"])
```

---

### Test 2: Pandas Filtering
**Query**: "Show me all active hotels"

**Validates**:
- ✅ inspect_api_structure called first
- ✅ flatten_data called with explicit fields
- ✅ dataframe_query called with operation="filter"
- ✅ Filter condition present
- ✅ Correct sequence (flatten before query)

**Expected Tool Sequence**:
```
get_datasets → inspect_api_structure → flatten_data → dataframe_query(operation="filter")
```

---

### Test 3: Pandas Sorting
**Query**: "List datasets sorted by name"

**Validates**:
- ✅ flatten_data called
- ✅ dataframe_query with operation="sort"
- ✅ sort_by parameter present
- ❌ NOT using aggregate_data for sorting

**Expected Tool Sequence**:
```
get_datasets → flatten_data → dataframe_query(operation="sort", sort_by="Shortname")
```

---

### Test 4: Pandas Grouping
**Query**: "How many datasets per dataspace?"

**Validates**:
- ✅ flatten_data called
- ✅ dataframe_query with operation="groupby"
- ✅ group_by parameter present

**Expected Tool Sequence**:
```
get_datasets → flatten_data → dataframe_query(operation="groupby", group_by="Dataspace")
```

---

### Test 5: Complex Chained Operations
**Query**: "Show me top 5 active hotels sorted by name"

**Validates**:
- ✅ flatten_data called
- ✅ Multiple dataframe_query calls (chaining)
- ✅ Filter operation used
- ✅ Sort operation used
- ✅ Limit applied

**Expected Tool Sequence**:
```
get_datasets → flatten_data →
dataframe_query(filter) →
dataframe_query(sort) →
dataframe_query(head, limit=5)
```

---

### Test 6: Structure Inspection
**Query**: "What fields are available in the datasets?"

**Validates**:
- ✅ get_datasets called
- ✅ inspect_api_structure called
- ✅ inspect called before aggregate_data

**Expected Tool Sequence**:
```
get_datasets → inspect_api_structure → (optional: aggregate_data)
```

## Understanding Test Results

### Test Statuses

- **✅ PASS**: All expectations met
- **⚠️  PARTIAL**: Some expectations met, but workflow not optimal
- **❌ FAIL**: Critical expectations not met or error occurred
- **⏭️  SKIP**: Test skipped

### Expectations

Each test validates specific behaviors:

- **Tool called**: Verifies specific tool was invoked
- **Parameters present**: Checks required parameters provided
- **Correct sequence**: Validates tool call order
- **No anti-patterns**: Ensures bad practices avoided (e.g., AUTO mode)

### Example Output

```
========================================
INTEGRATION TEST REPORT
========================================
Total Tests: 6
Backend URL: http://localhost:8001
========================================

SUMMARY:
  ✅ Passed:  4/6
  ⚠️  Partial: 2/6
  ❌ Failed:  0/6

DETAILED RESULTS:
----------------------------------------

1. ✅ PASS - Simple Field Extraction
   Query: "List all dataset names"
   Iterations: 2
   Tool Calls: 2
   Execution Time: 3.45s
   Tool Sequence: get_datasets → aggregate_data

   Expectations:
     ✅ get_datasets called
     ✅ aggregate_data called
     ✅ aggregate_data has strategy
     ✅ aggregate_data has fields
     ✅ NO AUTO mode

   Response Preview: Here are all 167 dataset names from the Open Data Hub...
```

## Output Files

### Logs

All logs are saved to `test_logs/` directory:

- `backend_YYYYMMDD_HHMMSS.log`: Backend server logs with emoji markers
- `test_YYYYMMDD_HHMMSS.log`: Test execution logs
- `integration_test.log`: Detailed test framework logs

### Reports

- `backend/integration_test_report_YYYYMMDD_HHMMSS.json`: Detailed JSON report with all test results

### Log Markers

Backend logs use emoji markers for easy filtering:

- 🤖 Agent iterations
- 🔧 Tool decisions
- ⚙️  Tool executions
- 🔨 flatten_data operations
- 🐼 dataframe_query operations
- 🔧 aggregate_data operations
- ✅ Successful completions
- ❌ Errors
- ⚠️  Warnings

**Example grep commands**:
```bash
# Find all agent decisions
grep "🤖 AGENT DECISION" test_logs/backend_*.log

# Find all tool calls
grep "🔧" test_logs/backend_*.log

# Find errors
grep "❌" test_logs/backend_*.log

# Find pandas operations
grep "🐼" test_logs/backend_*.log
```

## Analyzing Failures

### Common Failure Patterns

**1. AUTO Mode Still Used**
```
Expectation Failed: NO AUTO mode
Tool Call: aggregate_data(strategy="auto", ...)
```

**Fix**: Update `backend/agent/prompts.py` to be more explicit about strategy requirement.

---

**2. inspect_api_structure Not Called**
```
Expectation Failed: inspect_api_structure called
Tool Sequence: get_datasets → flatten_data
```

**Fix**: Strengthen Rule 2 in system prompt to make inspection mandatory.

---

**3. Wrong Tool for Operation**
```
Expectation Failed: dataframe_query operation=filter
Tool Sequence: get_datasets → aggregate_data
```

**Fix**: Update Rule 4 in system prompt with clearer examples of when to use pandas workflow.

---

**4. Missing Parameters**
```
Expectation Failed: aggregate_data has fields
Tool Call: aggregate_data(cache_key="...", strategy="extract_fields")
```

**Fix**: Tool description needs more emphasis on required parameters.

## Debugging Tips

### Enable Verbose Logging

Edit `backend/.env`:
```bash
LOG_LEVEL=DEBUG
```

### Run Single Test

Modify `test_integration.py`:
```python
async def run_all_tests(self):
    # Comment out tests you don't want to run
    await self.test_2_pandas_filtering()  # Only run this one
    # await self.test_1_simple_extraction()
    # await self.test_3_sorting()
```

### Add Custom Tests

Add new test methods to `IntegrationTester` class:

```python
async def test_my_custom_test(self):
    """Test X: My custom test"""
    query = "Your test query here"
    start_time = datetime.now()

    result = TestResult(
        test_name="My Custom Test",
        status=TestStatus.FAIL,
        query=query,
        response="",
        iterations=0
    )

    try:
        response = await self.run_query(query)
        result.response = response['response']
        result.tool_calls = response['tool_calls']

        # Define expectations
        result.expectations = {
            "some_tool called": self.has_tool_call(result.tool_calls, "some_tool"),
            # Add more expectations
        }

        if all(result.expectations.values()):
            result.status = TestStatus.PASS

    except Exception as e:
        result.errors.append(str(e))

    result.execution_time = (datetime.now() - start_time).total_seconds()
    self.results.append(result)
    return result
```

Then call it in `run_all_tests()`.

### Check Agent Logs in Real-Time

```bash
# Terminal 1: Run backend with visible logs
cd backend
python -m uvicorn main:app --host 0.0.0.0 --port 8001 --log-level debug

# Terminal 2: Watch logs in real-time
watch -n 1 'tail -50 test_logs/backend_*.log'

# Terminal 3: Run tests
cd backend
python test_integration.py
```

## Continuous Integration

### Add to CI/CD Pipeline

```yaml
# .github/workflows/test.yml
name: Integration Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Set up Python
        uses: actions/setup-python@v2
        with:
          python-version: '3.11'
      - name: Install dependencies
        run: |
          cd OdhDiscovery/chatbot/backend
          pip install -r requirements.txt httpx
      - name: Run integration tests
        run: |
          cd OdhDiscovery/chatbot
          ./run_integration_tests.sh
```

## Troubleshooting

### Backend Won't Start

**Error**: `Backend failed to start`

**Solutions**:
1. Check port 8001 is not in use: `lsof -i:8001`
2. Check `.env` file exists in `backend/` directory
3. Verify LLM API key is valid
4. Check backend log for detailed error

### Tests Hang

**Error**: Tests don't complete

**Solutions**:
1. Check LLM API is reachable
2. Increase timeout in `test_integration.py` (default 120s)
3. Check for infinite loops in agent logic

### Connection Refused

**Error**: `Connection refused to localhost:8001`

**Solutions**:
1. Ensure backend is running
2. Check firewall settings
3. Try `0.0.0.0` instead of `localhost`

## Success Criteria

Tests are successful when:

- ✅ All 6 tests pass
- ✅ No AUTO mode usage detected
- ✅ inspect_api_structure called for large data
- ✅ Pandas workflow used for filter/sort/group operations
- ✅ No errors in backend logs
- ✅ All tool calls have required parameters

## Next Steps

After successful tests:

1. **Review Partial Results**: Check warnings on partial passes
2. **Add More Tests**: Cover edge cases and error scenarios
3. **Performance Testing**: Measure response times
4. **Load Testing**: Test with concurrent requests
5. **Production Deployment**: Deploy with confidence!

## Support

If tests fail unexpectedly:

1. Check `IMPLEMENTATION_SUMMARY.md` for recent changes
2. Review `ARCHITECTURE_REDESIGN.md` for design decisions
3. Check `TROUBLESHOOTING_AGENT_BEHAVIOR.md` for known issues
4. Open an issue with:
   - Test output
   - Backend logs
   - JSON report
   - Steps to reproduce
