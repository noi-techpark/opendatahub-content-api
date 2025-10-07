#!/bin/bash
# End-to-End test script
# Tests the complete system from scratch: Docker, DB, Materialize, API, Streaming

set -e

echo "=========================================="
echo "     END-TO-END TEST SCRIPT"
echo "=========================================="
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${CYAN}ℹ $1${NC}"
}

# Step 1: Clean up existing containers and volumes
print_info "Step 1: Cleaning up existing Docker containers and volumes"
docker-compose down -v 2>/dev/null || true
print_success "Docker cleanup complete"
echo ""

# Step 2: Start Docker services
print_info "Step 2: Starting Docker services (PostgreSQL + Materialize)"
docker-compose up -d
if [ $? -eq 0 ]; then
    print_success "Docker services started"
else
    print_error "Failed to start Docker services"
    exit 1
fi
echo ""

# Step 3: Wait for PostgreSQL to be ready
print_info "Step 3: Waiting for PostgreSQL to be ready"
max_attempts=30
attempt=0
while [ $attempt -lt $max_attempts ]; do
    if PGPASSWORD=password psql -h localhost -p 5556 -U bdp -d timeseries -c "SELECT 1" > /dev/null 2>&1; then
        print_success "PostgreSQL is ready"
        break
    fi
    attempt=$((attempt + 1))
    echo "Waiting for PostgreSQL... ($attempt/$max_attempts)"
    sleep 2
done

if [ $attempt -eq $max_attempts ]; then
    print_error "PostgreSQL did not become ready in time"
    exit 1
fi
echo ""

# Step 4: Initialize database schema
print_info "Step 4: Initializing database schema"
if PGPASSWORD=password psql -h localhost -p 5556 -U bdp -d timeseries -f sql-scripts/init-new.sql > /dev/null 2>&1; then
    print_success "Database schema initialized"
else
    print_error "Failed to initialize database schema"
    exit 1
fi
echo ""

# Step 5: Populate with sample data
print_info "Step 5: Populating database with sample data"
if [ -f ./setup_and_populate.sh ]; then
    chmod +x ./setup_and_populate.sh
    ./setup_and_populate.sh > /dev/null 2>&1
    print_success "Sample data populated"
else
    print_info "setup_and_populate.sh not found, skipping sample data"
fi
echo ""

# Step 6: Setup streaming infrastructure
print_info "Step 6: Setting up streaming infrastructure (PostgreSQL publication + Materialize)"
chmod +x ./setup-streaming.sh
./setup-streaming.sh
if [ $? -eq 0 ]; then
    print_success "Streaming infrastructure setup complete"
else
    print_error "Failed to setup streaming infrastructure"
    exit 1
fi
echo ""

# Step 7: Build Go server
print_info "Step 7: Building Go server"
go build -o timeseries-api cmd/server/main.go
if [ $? -eq 0 ]; then
    print_success "Go server built successfully"
else
    print_error "Failed to build Go server"
    exit 1
fi
echo ""

# Step 8: Start Go server in background
print_info "Step 8: Starting Go API server"
./timeseries-api > server.log 2>&1 &
SERVER_PID=$!
print_info "Server PID: $SERVER_PID"
sleep 5

# Check if server is running
if ps -p $SERVER_PID > /dev/null; then
    print_success "Go server started successfully"
else
    print_error "Go server failed to start"
    cat server.log
    exit 1
fi
echo ""

# Step 9: Run batch endpoint tests
print_info "Step 9: Testing /batch endpoint"
chmod +x test_batch_endpoint.py
if python3 test_batch_endpoint.py; then
    print_success "Batch endpoint tests passed"
else
    print_error "Batch endpoint tests failed"
    kill $SERVER_PID 2>/dev/null || true
    exit 1
fi
echo ""

# Step 10: Run comprehensive streaming tests
print_info "Step 10: Testing streaming subscriptions (comprehensive)"
chmod +x test_streaming_comprehensive.py
if python3 test_streaming_comprehensive.py; then
    print_success "All streaming tests passed"
else
    print_error "Streaming tests failed"
    kill $SERVER_PID 2>/dev/null || true
    exit 1
fi
echo ""

# Cleanup
print_info "Stopping Go server"
kill $SERVER_PID 2>/dev/null || true
print_success "Server stopped"

echo ""
echo "=========================================="
echo "   ✅ ALL E2E TESTS PASSED!"
echo "=========================================="
echo ""
print_info "System is ready for use:"
print_info "  - PostgreSQL: localhost:5556"
print_info "  - Materialize: localhost:6875"
print_info "  - API Server: localhost:8080"
print_info ""
print_info "To start the server: ./timeseries-api"
print_info "To test streaming: open test-streaming-client.html"
echo ""
