"""
Integration Tests for ODH Chatbot Agent
Tests new pandas-based workflow and tool usage
"""
import asyncio
import httpx
import logging
import json
import sys
from typing import Dict, List, Any
from datetime import datetime
from dataclasses import dataclass, field
from enum import Enum


# Configure logging to capture everything
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('integration_test.log'),
        logging.StreamHandler(sys.stdout)
    ]
)
logger = logging.getLogger(__name__)


class TestStatus(Enum):
    PASS = "✅ PASS"
    FAIL = "❌ FAIL"
    PARTIAL = "⚠️  PARTIAL"
    SKIP = "⏭️  SKIP"


@dataclass
class TestResult:
    """Results from a single test"""
    test_name: str
    status: TestStatus
    query: str
    response: str
    iterations: int
    tool_calls: List[Dict[str, Any]] = field(default_factory=list)
    expectations: Dict[str, bool] = field(default_factory=dict)
    errors: List[str] = field(default_factory=list)
    warnings: List[str] = field(default_factory=list)
    execution_time: float = 0.0


class IntegrationTester:
    """Integration test runner for ODH Chatbot"""

    def __init__(self, base_url: str = "http://localhost:8001"):
        self.base_url = base_url
        self.results: List[TestResult] = []

    async def run_query(self, query: str) -> Dict[str, Any]:
        """Execute a query against the backend"""
        async with httpx.AsyncClient(timeout=120.0) as client:
            response = await client.post(
                f"{self.base_url}/query",
                json={"query": query, "include_debug": True}
            )
            response.raise_for_status()
            return response.json()

    def check_tool_call(self, tool_calls: List[Dict], tool_name: str, **expected_args) -> bool:
        """Check if a specific tool was called with expected arguments"""
        for tc in tool_calls:
            if tc.get('name') == tool_name:
                # Check if expected args are present
                actual_args = tc.get('args', {})
                for key, value in expected_args.items():
                    if key not in actual_args:
                        return False
                    # For lists/dicts, check if value is subset
                    if isinstance(value, dict):
                        for k, v in value.items():
                            if actual_args[key].get(k) != v:
                                return False
                    elif isinstance(value, list):
                        if not all(item in actual_args[key] for item in value):
                            return False
                    elif actual_args[key] != value:
                        return False
                return True
        return False

    def has_tool_call(self, tool_calls: List[Dict], tool_name: str) -> bool:
        """Check if a tool was called (regardless of args)"""
        return any(tc.get('name') == tool_name for tc in tool_calls)

    def get_tool_call_sequence(self, tool_calls: List[Dict]) -> List[str]:
        """Get sequence of tool names called"""
        return [tc.get('name') for tc in tool_calls]

    async def test_1_simple_extraction(self):
        """Test 1: Simple field extraction without AUTO mode"""
        logger.info("="*60)
        logger.info("TEST 1: Simple Field Extraction (No AUTO Mode)")
        logger.info("="*60)

        query = "List all dataset names"
        start_time = datetime.now()

        result = TestResult(
            test_name="Simple Field Extraction",
            status=TestStatus.FAIL,
            query=query,
            response="",
            iterations=0
        )

        try:
            response = await self.run_query(query)
            result.response = response['response']
            result.iterations = response['iterations']
            result.tool_calls = response['tool_calls']

            logger.info(f"Response: {result.response[:200]}...")
            logger.info(f"Iterations: {result.iterations}")
            logger.info(f"Tool calls: {len(result.tool_calls)}")

            # Expectations
            result.expectations = {
                "get_datasets called": self.has_tool_call(result.tool_calls, "get_datasets"),
                "aggregate_data called": self.has_tool_call(result.tool_calls, "aggregate_data"),
                "aggregate_data has strategy": self.check_tool_call(
                    result.tool_calls, "aggregate_data", strategy="extract_fields"
                ),
                "aggregate_data has fields": any(
                    tc.get('name') == 'aggregate_data' and
                    tc.get('args', {}).get('fields') is not None
                    for tc in result.tool_calls
                ),
                "NO AUTO mode": not self.check_tool_call(
                    result.tool_calls, "aggregate_data", strategy="auto"
                )
            }

            logger.info("Expectations:")
            for exp, passed in result.expectations.items():
                logger.info(f"  {exp}: {'✅' if passed else '❌'}")

            # Determine status
            if all(result.expectations.values()):
                result.status = TestStatus.PASS
            elif any(result.expectations.values()):
                result.status = TestStatus.PARTIAL
                result.warnings.append("Some expectations not met")

        except Exception as e:
            result.status = TestStatus.FAIL
            result.errors.append(str(e))
            logger.error(f"Test failed: {e}", exc_info=True)

        result.execution_time = (datetime.now() - start_time).total_seconds()
        self.results.append(result)
        return result

    async def test_2_pandas_filtering(self):
        """Test 2: Filtering with pandas workflow"""
        logger.info("="*60)
        logger.info("TEST 2: Pandas Filtering Workflow")
        logger.info("="*60)

        query = "Show me all active hotels"
        start_time = datetime.now()

        result = TestResult(
            test_name="Pandas Filtering",
            status=TestStatus.FAIL,
            query=query,
            response="",
            iterations=0
        )

        try:
            response = await self.run_query(query)
            result.response = response['response']
            result.iterations = response['iterations']
            result.tool_calls = response['tool_calls']

            sequence = self.get_tool_call_sequence(result.tool_calls)
            logger.info(f"Tool call sequence: {sequence}")

            # Expectations
            result.expectations = {
                "inspect_api_structure called": self.has_tool_call(result.tool_calls, "inspect_api_structure"),
                "flatten_data called": self.has_tool_call(result.tool_calls, "flatten_data"),
                "dataframe_query called": self.has_tool_call(result.tool_calls, "dataframe_query"),
                "flatten has fields": any(
                    tc.get('name') == 'flatten_data' and
                    tc.get('args', {}).get('fields') is not None
                    for tc in result.tool_calls
                ),
                "dataframe_query operation=filter": self.check_tool_call(
                    result.tool_calls, "dataframe_query", operation="filter"
                ),
                "filter condition present": any(
                    tc.get('name') == 'dataframe_query' and
                    tc.get('args', {}).get('condition') is not None
                    for tc in result.tool_calls
                ),
                "correct sequence": (
                    'flatten_data' in sequence and
                    'dataframe_query' in sequence and
                    sequence.index('flatten_data') < sequence.index('dataframe_query')
                ) if 'flatten_data' in sequence and 'dataframe_query' in sequence else False
            }

            logger.info("Expectations:")
            for exp, passed in result.expectations.items():
                logger.info(f"  {exp}: {'✅' if passed else '❌'}")

            # Determine status
            if all(result.expectations.values()):
                result.status = TestStatus.PASS
            elif result.expectations.get("flatten_data called") and result.expectations.get("dataframe_query called"):
                result.status = TestStatus.PARTIAL
                result.warnings.append("Pandas workflow used but some steps missing")
            else:
                result.warnings.append("Pandas workflow NOT used - fell back to aggregate_data")

        except Exception as e:
            result.status = TestStatus.FAIL
            result.errors.append(str(e))
            logger.error(f"Test failed: {e}", exc_info=True)

        result.execution_time = (datetime.now() - start_time).total_seconds()
        self.results.append(result)
        return result

    async def test_3_sorting(self):
        """Test 3: Sorting with pandas workflow"""
        logger.info("="*60)
        logger.info("TEST 3: Pandas Sorting Workflow")
        logger.info("="*60)

        query = "List datasets sorted by name"
        start_time = datetime.now()

        result = TestResult(
            test_name="Pandas Sorting",
            status=TestStatus.FAIL,
            query=query,
            response="",
            iterations=0
        )

        try:
            response = await self.run_query(query)
            result.response = response['response']
            result.iterations = response['iterations']
            result.tool_calls = response['tool_calls']

            sequence = self.get_tool_call_sequence(result.tool_calls)
            logger.info(f"Tool call sequence: {sequence}")

            # Expectations
            result.expectations = {
                "flatten_data called": self.has_tool_call(result.tool_calls, "flatten_data"),
                "dataframe_query called": self.has_tool_call(result.tool_calls, "dataframe_query"),
                "dataframe_query operation=sort": self.check_tool_call(
                    result.tool_calls, "dataframe_query", operation="sort"
                ),
                "sort_by parameter present": any(
                    tc.get('name') == 'dataframe_query' and
                    tc.get('args', {}).get('sort_by') is not None
                    for tc in result.tool_calls
                ),
                "NOT using aggregate_data for sorting": not self.has_tool_call(result.tool_calls, "aggregate_data")
            }

            logger.info("Expectations:")
            for exp, passed in result.expectations.items():
                logger.info(f"  {exp}: {'✅' if passed else '❌'}")

            if all(result.expectations.values()):
                result.status = TestStatus.PASS
            elif result.expectations.get("dataframe_query operation=sort"):
                result.status = TestStatus.PARTIAL

        except Exception as e:
            result.status = TestStatus.FAIL
            result.errors.append(str(e))
            logger.error(f"Test failed: {e}", exc_info=True)

        result.execution_time = (datetime.now() - start_time).total_seconds()
        self.results.append(result)
        return result

    async def test_4_grouping(self):
        """Test 4: Grouping/counting with pandas workflow"""
        logger.info("="*60)
        logger.info("TEST 4: Pandas Grouping Workflow")
        logger.info("="*60)

        query = "How many datasets per dataspace?"
        start_time = datetime.now()

        result = TestResult(
            test_name="Pandas Grouping",
            status=TestStatus.FAIL,
            query=query,
            response="",
            iterations=0
        )

        try:
            response = await self.run_query(query)
            result.response = response['response']
            result.iterations = response['iterations']
            result.tool_calls = response['tool_calls']

            sequence = self.get_tool_call_sequence(result.tool_calls)
            logger.info(f"Tool call sequence: {sequence}")

            # Expectations
            result.expectations = {
                "flatten_data called": self.has_tool_call(result.tool_calls, "flatten_data"),
                "dataframe_query called": self.has_tool_call(result.tool_calls, "dataframe_query"),
                "dataframe_query operation=groupby": self.check_tool_call(
                    result.tool_calls, "dataframe_query", operation="groupby"
                ),
                "group_by parameter present": any(
                    tc.get('name') == 'dataframe_query' and
                    tc.get('args', {}).get('group_by') is not None
                    for tc in result.tool_calls
                ),
            }

            logger.info("Expectations:")
            for exp, passed in result.expectations.items():
                logger.info(f"  {exp}: {'✅' if passed else '❌'}")

            if all(result.expectations.values()):
                result.status = TestStatus.PASS
            elif result.expectations.get("dataframe_query operation=groupby"):
                result.status = TestStatus.PARTIAL

        except Exception as e:
            result.status = TestStatus.FAIL
            result.errors.append(str(e))
            logger.error(f"Test failed: {e}", exc_info=True)

        result.execution_time = (datetime.now() - start_time).total_seconds()
        self.results.append(result)
        return result

    async def test_5_complex_chain(self):
        """Test 5: Complex chained operations"""
        logger.info("="*60)
        logger.info("TEST 5: Complex Chained Operations")
        logger.info("="*60)

        query = "Show me top 5 active hotels sorted by name"
        start_time = datetime.now()

        result = TestResult(
            test_name="Complex Chain",
            status=TestStatus.FAIL,
            query=query,
            response="",
            iterations=0
        )

        try:
            response = await self.run_query(query)
            result.response = response['response']
            result.iterations = response['iterations']
            result.tool_calls = response['tool_calls']

            sequence = self.get_tool_call_sequence(result.tool_calls)
            logger.info(f"Tool call sequence: {sequence}")

            # Count dataframe_query calls
            df_query_calls = sum(1 for tc in result.tool_calls if tc.get('name') == 'dataframe_query')

            # Expectations
            result.expectations = {
                "flatten_data called": self.has_tool_call(result.tool_calls, "flatten_data"),
                "multiple dataframe_query calls": df_query_calls >= 2,
                "filter operation used": any(
                    tc.get('name') == 'dataframe_query' and
                    tc.get('args', {}).get('operation') == 'filter'
                    for tc in result.tool_calls
                ),
                "sort operation used": any(
                    tc.get('name') == 'dataframe_query' and
                    tc.get('args', {}).get('operation') == 'sort'
                    for tc in result.tool_calls
                ),
                "limit applied": any(
                    tc.get('name') == 'dataframe_query' and
                    tc.get('args', {}).get('limit') is not None
                    for tc in result.tool_calls
                ),
            }

            logger.info("Expectations:")
            for exp, passed in result.expectations.items():
                logger.info(f"  {exp}: {'✅' if passed else '❌'}")

            if all(result.expectations.values()):
                result.status = TestStatus.PASS
            elif sum(result.expectations.values()) >= 3:
                result.status = TestStatus.PARTIAL

        except Exception as e:
            result.status = TestStatus.FAIL
            result.errors.append(str(e))
            logger.error(f"Test failed: {e}", exc_info=True)

        result.execution_time = (datetime.now() - start_time).total_seconds()
        self.results.append(result)
        return result

    async def test_6_inspect_structure(self):
        """Test 6: inspect_api_structure usage for large data"""
        logger.info("="*60)
        logger.info("TEST 6: Structure Inspection for Large Data")
        logger.info("="*60)

        query = "What fields are available in the datasets?"
        start_time = datetime.now()

        result = TestResult(
            test_name="Structure Inspection",
            status=TestStatus.FAIL,
            query=query,
            response="",
            iterations=0
        )

        try:
            response = await self.run_query(query)
            result.response = response['response']
            result.iterations = response['iterations']
            result.tool_calls = response['tool_calls']

            # Expectations
            result.expectations = {
                "get_datasets called": self.has_tool_call(result.tool_calls, "get_datasets"),
                "inspect_api_structure called": self.has_tool_call(result.tool_calls, "inspect_api_structure"),
                "inspect before aggregate": True  # Will validate sequence
            }

            sequence = self.get_tool_call_sequence(result.tool_calls)
            if 'inspect_api_structure' in sequence and 'aggregate_data' in sequence:
                result.expectations["inspect before aggregate"] = (
                    sequence.index('inspect_api_structure') < sequence.index('aggregate_data')
                )

            logger.info("Expectations:")
            for exp, passed in result.expectations.items():
                logger.info(f"  {exp}: {'✅' if passed else '❌'}")

            if all(result.expectations.values()):
                result.status = TestStatus.PASS
            elif result.expectations.get("inspect_api_structure called"):
                result.status = TestStatus.PARTIAL

        except Exception as e:
            result.status = TestStatus.FAIL
            result.errors.append(str(e))
            logger.error(f"Test failed: {e}", exc_info=True)

        result.execution_time = (datetime.now() - start_time).total_seconds()
        self.results.append(result)
        return result

    def generate_report(self):
        """Generate comprehensive test report"""
        print("\n" + "="*80)
        print("INTEGRATION TEST REPORT")
        print("="*80)
        print(f"Timestamp: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print(f"Total Tests: {len(self.results)}")
        print(f"Backend URL: {self.base_url}")
        print("="*80)

        # Summary
        passed = sum(1 for r in self.results if r.status == TestStatus.PASS)
        partial = sum(1 for r in self.results if r.status == TestStatus.PARTIAL)
        failed = sum(1 for r in self.results if r.status == TestStatus.FAIL)

        print(f"\nSUMMARY:")
        print(f"  ✅ Passed:  {passed}/{len(self.results)}")
        print(f"  ⚠️  Partial: {partial}/{len(self.results)}")
        print(f"  ❌ Failed:  {failed}/{len(self.results)}")

        # Detailed results
        print("\n" + "-"*80)
        print("DETAILED RESULTS:")
        print("-"*80)

        for i, result in enumerate(self.results, 1):
            print(f"\n{i}. {result.status.value} - {result.test_name}")
            print(f"   Query: \"{result.query}\"")
            print(f"   Iterations: {result.iterations}")
            print(f"   Tool Calls: {len(result.tool_calls)}")
            print(f"   Execution Time: {result.execution_time:.2f}s")

            if result.tool_calls:
                print(f"   Tool Sequence: {' → '.join(tc.get('name', 'unknown') for tc in result.tool_calls)}")

            print(f"\n   Expectations:")
            for exp, passed in result.expectations.items():
                print(f"     {'✅' if passed else '❌'} {exp}")

            if result.warnings:
                print(f"\n   Warnings:")
                for warning in result.warnings:
                    print(f"     ⚠️  {warning}")

            if result.errors:
                print(f"\n   Errors:")
                for error in result.errors:
                    print(f"     ❌ {error}")

            print(f"\n   Response Preview: {result.response[:150]}...")

        # Tool usage statistics
        print("\n" + "-"*80)
        print("TOOL USAGE STATISTICS:")
        print("-"*80)

        all_tools = {}
        for result in self.results:
            for tc in result.tool_calls:
                tool_name = tc.get('name', 'unknown')
                all_tools[tool_name] = all_tools.get(tool_name, 0) + 1

        for tool, count in sorted(all_tools.items(), key=lambda x: x[1], reverse=True):
            print(f"  {tool}: {count} calls")

        # Final verdict
        print("\n" + "="*80)
        if passed == len(self.results):
            print("🎉 ALL TESTS PASSED! Agent behavior is correct.")
        elif passed + partial == len(self.results):
            print("⚠️  TESTS PASSED WITH WARNINGS - Review partial results above")
        else:
            print("❌ SOME TESTS FAILED - Agent needs fixes")
        print("="*80)

        # Save JSON report
        report_file = f"integration_test_report_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
        with open(report_file, 'w') as f:
            json.dump({
                'summary': {
                    'total': len(self.results),
                    'passed': passed,
                    'partial': partial,
                    'failed': failed
                },
                'tests': [
                    {
                        'name': r.test_name,
                        'status': r.status.value,
                        'query': r.query,
                        'iterations': r.iterations,
                        'tool_calls': r.tool_calls,
                        'expectations': r.expectations,
                        'errors': r.errors,
                        'warnings': r.warnings,
                        'execution_time': r.execution_time
                    }
                    for r in self.results
                ]
            }, f, indent=2)
        print(f"\nDetailed JSON report saved to: {report_file}")
        print(f"Full logs saved to: integration_test.log")

    async def test_7_dataset_entries(self):
        """Test 7: get_dataset_entries tool"""
        logger.info("="*60)
        logger.info("TEST 7: get_dataset_entries Tool")
        logger.info("="*60)

        query = "Get active hotels from accommodation dataset"
        start_time = datetime.now()

        result = TestResult(
            test_name="get_dataset_entries",
            status=TestStatus.FAIL,
            query=query,
            response="",
            iterations=0
        )

        try:
            response = await self.run_query(query)
            result.response = response['response']
            result.iterations = response['iterations']
            result.tool_calls = response['tool_calls']

            sequence = self.get_tool_call_sequence(result.tool_calls)
            logger.info(f"Tool call sequence: {sequence}")

            # Expectations
            result.expectations = {
                "get_datasets called": self.has_tool_call(result.tool_calls, "get_datasets"),
                "get_dataset_entries called": self.has_tool_call(result.tool_calls, "get_dataset_entries"),
                "dataset_name parameter present": any(
                    tc.get('name') == 'get_dataset_entries' and
                    tc.get('args', {}).get('dataset_name') is not None
                    for tc in result.tool_calls
                ),
            }

            logger.info("Expectations:")
            for exp, passed in result.expectations.items():
                logger.info(f"  {exp}: {'✅' if passed else '❌'}")

            if all(result.expectations.values()):
                result.status = TestStatus.PASS
            elif result.expectations.get("get_dataset_entries called"):
                result.status = TestStatus.PARTIAL

        except Exception as e:
            result.status = TestStatus.FAIL
            result.errors.append(str(e))
            logger.error(f"Test failed: {e}", exc_info=True)

        result.execution_time = (datetime.now() - start_time).total_seconds()
        self.results.append(result)
        return result

    async def test_8_count_entries(self):
        """Test 8: count_entries tool"""
        logger.info("="*60)
        logger.info("TEST 8: count_entries Tool")
        logger.info("="*60)

        query = "How many active hotels are there?"
        start_time = datetime.now()

        result = TestResult(
            test_name="count_entries",
            status=TestStatus.FAIL,
            query=query,
            response="",
            iterations=0
        )

        try:
            response = await self.run_query(query)
            result.response = response['response']
            result.iterations = response['iterations']
            result.tool_calls = response['tool_calls']

            # Expectations
            result.expectations = {
                "count_entries called": self.has_tool_call(result.tool_calls, "count_entries"),
                "dataset_name present": any(
                    tc.get('name') == 'count_entries' and
                    tc.get('args', {}).get('dataset_name') is not None
                    for tc in result.tool_calls
                ),
            }

            logger.info("Expectations:")
            for exp, passed in result.expectations.items():
                logger.info(f"  {exp}: {'✅' if passed else '❌'}")

            if all(result.expectations.values()):
                result.status = TestStatus.PASS

        except Exception as e:
            result.status = TestStatus.FAIL
            result.errors.append(str(e))
            logger.error(f"Test failed: {e}", exc_info=True)

        result.execution_time = (datetime.now() - start_time).total_seconds()
        self.results.append(result)
        return result

    async def test_9_timeseries_types(self):
        """Test 9: get_types tool (timeseries)"""
        logger.info("="*60)
        logger.info("TEST 9: get_types Tool (Timeseries)")
        logger.info("="*60)

        query = "What types of timeseries data are available?"
        start_time = datetime.now()

        result = TestResult(
            test_name="get_types (timeseries)",
            status=TestStatus.FAIL,
            query=query,
            response="",
            iterations=0
        )

        try:
            response = await self.run_query(query)
            result.response = response['response']
            result.iterations = response['iterations']
            result.tool_calls = response['tool_calls']

            # Expectations
            result.expectations = {
                "get_types called": self.has_tool_call(result.tool_calls, "get_types"),
            }

            logger.info("Expectations:")
            for exp, passed in result.expectations.items():
                logger.info(f"  {exp}: {'✅' if passed else '❌'}")

            if all(result.expectations.values()):
                result.status = TestStatus.PASS

        except Exception as e:
            result.status = TestStatus.FAIL
            result.errors.append(str(e))
            logger.error(f"Test failed: {e}", exc_info=True)

        result.execution_time = (datetime.now() - start_time).total_seconds()
        self.results.append(result)
        return result

    async def test_10_search_documentation(self):
        """Test 10: search_documentation tool"""
        logger.info("="*60)
        logger.info("TEST 10: search_documentation Tool")
        logger.info("="*60)

        query = "How do I use the accommodation dataset?"
        start_time = datetime.now()

        result = TestResult(
            test_name="search_documentation",
            status=TestStatus.FAIL,
            query=query,
            response="",
            iterations=0
        )

        try:
            response = await self.run_query(query)
            result.response = response['response']
            result.iterations = response['iterations']
            result.tool_calls = response['tool_calls']

            # Expectations
            result.expectations = {
                "search_documentation called": self.has_tool_call(result.tool_calls, "search_documentation"),
                "query parameter present": any(
                    tc.get('name') == 'search_documentation' and
                    tc.get('args', {}).get('query') is not None
                    for tc in result.tool_calls
                ),
            }

            logger.info("Expectations:")
            for exp, passed in result.expectations.items():
                logger.info(f"  {exp}: {'✅' if passed else '❌'}")

            if all(result.expectations.values()):
                result.status = TestStatus.PASS

        except Exception as e:
            result.status = TestStatus.FAIL
            result.errors.append(str(e))
            logger.error(f"Test failed: {e}", exc_info=True)

        result.execution_time = (datetime.now() - start_time).total_seconds()
        self.results.append(result)
        return result

    async def test_11_large_entries_pandas(self):
        """Test 11: Large dataset entries with pandas workflow"""
        logger.info("="*60)
        logger.info("TEST 11: Large Entries + Pandas Workflow")
        logger.info("="*60)

        query = "Get all accommodations and filter by type hotel"
        start_time = datetime.now()

        result = TestResult(
            test_name="Large Entries + Pandas",
            status=TestStatus.FAIL,
            query=query,
            response="",
            iterations=0
        )

        try:
            response = await self.run_query(query)
            result.response = response['response']
            result.iterations = response['iterations']
            result.tool_calls = response['tool_calls']

            sequence = self.get_tool_call_sequence(result.tool_calls)
            logger.info(f"Tool call sequence: {sequence}")

            # Expectations
            result.expectations = {
                "get_dataset_entries with return_cache_key": any(
                    tc.get('name') == 'get_dataset_entries' and
                    tc.get('args', {}).get('return_cache_key') == True
                    for tc in result.tool_calls
                ),
                "flatten_data called": self.has_tool_call(result.tool_calls, "flatten_data"),
                "dataframe_query called": self.has_tool_call(result.tool_calls, "dataframe_query"),
                "filter operation": any(
                    tc.get('name') == 'dataframe_query' and
                    tc.get('args', {}).get('operation') == 'filter'
                    for tc in result.tool_calls
                ),
            }

            logger.info("Expectations:")
            for exp, passed in result.expectations.items():
                logger.info(f"  {exp}: {'✅' if passed else '❌'}")

            if all(result.expectations.values()):
                result.status = TestStatus.PASS
            elif result.expectations.get("flatten_data called"):
                result.status = TestStatus.PARTIAL
                result.warnings.append("Pandas workflow used but return_cache_key might not be set")

        except Exception as e:
            result.status = TestStatus.FAIL
            result.errors.append(str(e))
            logger.error(f"Test failed: {e}", exc_info=True)

        result.execution_time = (datetime.now() - start_time).total_seconds()
        self.results.append(result)
        return result

    async def run_all_tests(self):
        """Run all integration tests"""
        print("Starting Integration Tests...")
        print(f"Backend URL: {self.base_url}\n")

        # Check backend health
        try:
            async with httpx.AsyncClient() as client:
                response = await client.get(f"{self.base_url}/health")
                response.raise_for_status()
                print(f"✅ Backend is healthy: {response.json()}\n")
        except Exception as e:
            print(f"❌ Backend not accessible: {e}")
            print(f"   Make sure backend is running at {self.base_url}")
            return

        # Run core pandas workflow tests
        print("="*60)
        print("CORE PANDAS WORKFLOW TESTS")
        print("="*60 + "\n")
        await self.test_1_simple_extraction()
        await self.test_2_pandas_filtering()
        await self.test_3_sorting()
        await self.test_4_grouping()
        await self.test_5_complex_chain()
        await self.test_6_inspect_structure()

        # Run tool-specific tests
        print("\n" + "="*60)
        print("TOOL-SPECIFIC TESTS")
        print("="*60 + "\n")
        await self.test_7_dataset_entries()
        await self.test_8_count_entries()
        await self.test_9_timeseries_types()
        await self.test_10_search_documentation()
        await self.test_11_large_entries_pandas()

        # Generate report
        self.generate_report()


async def main():
    """Main entry point"""
    import argparse

    parser = argparse.ArgumentParser(description="Run integration tests for ODH Chatbot")
    parser.add_argument(
        "--url",
        default="http://localhost:8001",
        help="Backend URL (default: http://localhost:8001)"
    )
    args = parser.parse_args()

    tester = IntegrationTester(base_url=args.url)
    await tester.run_all_tests()


if __name__ == "__main__":
    asyncio.run(main())
