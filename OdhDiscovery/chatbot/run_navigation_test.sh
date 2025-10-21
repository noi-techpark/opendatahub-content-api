#!/bin/bash
# Run only the navigation test

echo "=================================="
echo "Navigation Tool Integration Test"
echo "=================================="
echo ""

cd "$(dirname "$0")"

# Start backend if not running
if ! curl -s http://localhost:8001/health > /dev/null; then
    echo "Starting backend..."
    cd backend
    python main.py &
    BACKEND_PID=$!
    echo "Backend PID: $BACKEND_PID"

    # Wait for backend to be ready
    echo "Waiting for backend to start..."
    for i in {1..30}; do
        if curl -s http://localhost:8001/health > /dev/null; then
            echo "✓ Backend ready"
            break
        fi
        sleep 1
        echo -n "."
    done
    echo ""
    cd ..
else
    echo "✓ Backend already running"
    BACKEND_PID=""
fi

# Run the navigation test
echo ""
echo "Running navigation test..."
echo ""

cd backend
python -c "
import asyncio
from test_integration import IntegrationTester

async def run_test():
    tester = IntegrationTester('http://localhost:8001')

    # Check backend health
    try:
        import httpx
        async with httpx.AsyncClient() as client:
            response = await client.get('http://localhost:8001/health')
            response.raise_for_status()
            print(f'✅ Backend is healthy: {response.json()}\n')
    except Exception as e:
        print(f'❌ Backend not accessible: {e}')
        return

    # Run only test 12
    await tester.test_12_navigation_selective()

    # Print results
    if tester.results:
        result = tester.results[0]
        print('\n' + '='*60)
        print('TEST RESULT')
        print('='*60)
        print(f'Status: {result.status.value}')
        print(f'Query: {result.query}')
        print(f'Execution Time: {result.execution_time:.2f}s')
        print(f'Iterations: {result.iterations}')
        print(f'Tool Calls: {len(result.tool_calls)}')
        print('\nExpectations:')
        for exp, passed in result.expectations.items():
            print(f'  {exp}: {\"✅\" if passed else \"❌\"}')

        if result.warnings:
            print('\nWarnings:')
            for warn in result.warnings:
                print(f'  ⚠️  {warn}')

        if result.errors:
            print('\nErrors:')
            for err in result.errors:
                print(f'  ❌ {err}')

        print('\nResponse preview:')
        print(f'  {result.response[:200]}...' if len(result.response) > 200 else f'  {result.response}')

asyncio.run(run_test())
"

# Cleanup
if [ -n "$BACKEND_PID" ]; then
    echo ""
    echo "Stopping backend (PID: $BACKEND_PID)..."
    kill $BACKEND_PID 2>/dev/null
    echo "✓ Backend stopped"
fi

echo ""
echo "Test complete!"
