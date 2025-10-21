#!/bin/bash
# Integration Test Runner for ODH Chatbot
# Starts backend, runs tests, captures logs, and analyzes results

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$SCRIPT_DIR/backend"
LOG_DIR="$SCRIPT_DIR/test_logs"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}ODH Chatbot Integration Test Runner${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Create log directory
mkdir -p "$LOG_DIR"
BACKEND_LOG="$LOG_DIR/backend_${TIMESTAMP}.log"
TEST_LOG="$LOG_DIR/test_${TIMESTAMP}.log"

echo -e "${YELLOW}[1/5] Checking prerequisites...${NC}"

# Check if backend directory exists
if [ ! -d "$BACKEND_DIR" ]; then
    echo -e "${RED}Error: Backend directory not found at $BACKEND_DIR${NC}"
    exit 1
fi

# Check if Python is available
if ! command -v python &> /dev/null; then
    echo -e "${RED}Error: Python not found${NC}"
    exit 1
fi

# Check if required Python packages are installed
cd "$BACKEND_DIR"
if ! python -c "import fastapi, uvicorn, httpx" 2>/dev/null; then
    echo -e "${YELLOW}Installing required packages...${NC}"
    pip install -q -r requirements.txt httpx
fi

echo -e "${GREEN}✓ Prerequisites OK${NC}"
echo ""

# Start backend
echo -e "${YELLOW}[2/5] Starting backend server...${NC}"
echo -e "Backend log: $BACKEND_LOG"

# Kill any existing backend process on port 8001
lsof -ti:8001 | xargs kill -9 2>/dev/null || true

# Start backend in background with logging
cd "$BACKEND_DIR"
nohup python -m uvicorn main:app --host 0.0.0.0 --port 8001 --log-level debug > "$BACKEND_LOG" 2>&1 &
BACKEND_PID=$!

echo -e "Backend PID: $BACKEND_PID"

# Wait for backend to start
echo -e "${YELLOW}Waiting for backend to start...${NC}"
for i in {1..30}; do
    if curl -s http://localhost:8001/health > /dev/null 2>&1; then
        echo -e "${GREEN}✓ Backend started successfully${NC}"
        break
    fi
    sleep 1
    echo -n "."
done

# Check if backend is running
if ! curl -s http://localhost:8001/health > /dev/null 2>&1; then
    echo -e "\n${RED}Error: Backend failed to start${NC}"
    echo -e "${RED}Check log: $BACKEND_LOG${NC}"
    tail -50 "$BACKEND_LOG"
    kill $BACKEND_PID 2>/dev/null || true
    exit 1
fi

echo ""
echo ""

# Run tests
echo -e "${YELLOW}[3/5] Running integration tests...${NC}"
echo -e "Test log: $TEST_LOG"
echo ""

cd "$BACKEND_DIR"
python test_integration.py --url http://localhost:8001 2>&1 | tee "$TEST_LOG"

TEST_EXIT_CODE=${PIPESTATUS[0]}

echo ""

# Analyze logs
echo -e "${YELLOW}[4/5] Analyzing backend logs...${NC}"
echo ""

# Count tool calls
echo -e "${BLUE}Tool Call Analysis:${NC}"
grep -o "🔧 AGENT DECISION: Call .* tool" "$BACKEND_LOG" | sort | uniq -c | sort -rn || echo "No tool calls found"
echo ""

# Check for errors
echo -e "${BLUE}Error Analysis:${NC}"
ERROR_COUNT=$(grep -c "❌" "$BACKEND_LOG" || echo "0")
WARNING_COUNT=$(grep -c "⚠️" "$BACKEND_LOG" || echo "0")
echo "Errors: $ERROR_COUNT"
echo "Warnings: $WARNING_COUNT"

if [ "$ERROR_COUNT" -gt 0 ]; then
    echo ""
    echo -e "${RED}Recent errors from backend log:${NC}"
    grep "❌" "$BACKEND_LOG" | tail -10
fi

echo ""

# Check workflow patterns
echo -e "${BLUE}Workflow Pattern Analysis:${NC}"
echo "AUTO mode usage:"
grep -c "strategy.*auto" "$BACKEND_LOG" || echo "0 (Good!)"

echo "inspect_api_structure usage:"
grep -c "inspect_api_structure" "$BACKEND_LOG" || echo "0"

echo "flatten_data usage:"
grep -c "🔨 FLATTEN" "$BACKEND_LOG" || echo "0"

echo "dataframe_query usage:"
grep -c "🐼 DATAFRAME_QUERY" "$BACKEND_LOG" || echo "0"

echo ""

# Shutdown backend
echo -e "${YELLOW}[5/5] Shutting down backend...${NC}"
kill $BACKEND_PID 2>/dev/null || true
sleep 2
echo -e "${GREEN}✓ Backend stopped${NC}"
echo ""

# Final summary
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}TEST SUMMARY${NC}"
echo -e "${BLUE}========================================${NC}"

if [ $TEST_EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✅ All tests completed${NC}"
else
    echo -e "${RED}❌ Some tests failed (exit code: $TEST_EXIT_CODE)${NC}"
fi

echo ""
echo "Logs saved to:"
echo "  Backend: $BACKEND_LOG"
echo "  Tests:   $TEST_LOG"
echo "  Reports: backend/integration_test_report_*.json"
echo ""

# Quick log analysis
echo -e "${BLUE}Quick Backend Log Analysis:${NC}"
echo "Total agent iterations: $(grep -c "🤖 AGENT ITERATION" "$BACKEND_LOG" || echo "0")"
echo "Total tool executions: $(grep -c "⚙️  EXECUTING TOOLS" "$BACKEND_LOG" || echo "0")"
echo "Successful completions: $(grep -c "✅ AGGREGATION COMPLETE" "$BACKEND_LOG" || echo "0")"
echo ""

# Suggest next steps
if [ $TEST_EXIT_CODE -ne 0 ]; then
    echo -e "${YELLOW}Next steps:${NC}"
    echo "1. Review test log: $TEST_LOG"
    echo "2. Review backend log: $BACKEND_LOG"
    echo "3. Check JSON report for detailed expectations"
    echo "4. Fix agent prompts or tool implementations as needed"
    echo ""
fi

exit $TEST_EXIT_CODE
