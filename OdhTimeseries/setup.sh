#!/bin/bash
# Unified setup script for the Timeseries Streaming API
# Supports both clean setup (with volume pruning) and restart (preserving data)

set -e

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
CYAN='\033[0;36m'
YELLOW='\033[0;33m'
BOLD='\033[1m'
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

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_header() {
    echo ""
    echo -e "${BOLD}=========================================="
    echo -e "  $1"
    echo -e "==========================================${NC}"
    echo ""
}

show_usage() {
    cat << EOF
Usage: $0 [OPTIONS]

Setup and start the Timeseries Streaming API system.

OPTIONS:
    --clean         Clean setup: remove volumes, initialize schema, populate data
    --restart       Restart: preserve existing data, skip initialization
    --no-populate   Skip data population (only with --clean)
    --help          Show this help message

EXAMPLES:
    $0 --clean              # Fresh start with sample data
    $0 --clean --no-populate # Fresh start without sample data
    $0 --restart            # Restart with existing data

DEFAULT: If no flag specified, runs in restart mode
EOF
    exit 0
}

# Parse arguments
MODE="restart"
POPULATE=true

while [[ $# -gt 0 ]]; do
    case $1 in
        --clean)
            MODE="clean"
            shift
            ;;
        --restart)
            MODE="restart"
            shift
            ;;
        --no-populate)
            POPULATE=false
            shift
            ;;
        --help|-h)
            show_usage
            ;;
        *)
            echo "Unknown option: $1"
            show_usage
            ;;
    esac
done

print_header "TIMESERIES STREAMING API SETUP"

if [ "$MODE" = "clean" ]; then
    print_info "Mode: CLEAN SETUP (volumes will be removed)"
else
    print_info "Mode: RESTART (preserving existing data)"
fi

echo ""

# Step 1: Stop existing containers
print_info "Step 1: Stopping existing containers"
if [ "$MODE" = "clean" ]; then
    docker-compose down -v 2>/dev/null || true
    print_success "Containers stopped and volumes removed"
else
    docker-compose down 2>/dev/null || true
    print_success "Containers stopped (volumes preserved)"
fi
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

# Step 3: Wait for PostgreSQL
print_info "Step 3: Waiting for PostgreSQL to be ready"
max_attempts=30
attempt=0
while [ $attempt -lt $max_attempts ]; do
    if PGPASSWORD=password psql -h localhost -p 5556 -U bdp -d timeseries -c "SELECT 1" > /dev/null 2>&1; then
        print_success "PostgreSQL is ready"
        break
    fi
    attempt=$((attempt + 1))
    if [ $((attempt % 5)) -eq 0 ]; then
        echo "Waiting for PostgreSQL... ($attempt/$max_attempts)"
    fi
    sleep 2
done

if [ $attempt -eq $max_attempts ]; then
    print_error "PostgreSQL did not become ready in time"
    exit 1
fi
echo ""

# Step 4: Initialize schema (only in clean mode)
if [ "$MODE" = "clean" ]; then
    print_info "Step 4: Initializing database schema"
    if PGPASSWORD=password psql -h localhost -p 5556 -U bdp -d timeseries -f sql-scripts/init-new.sql > /dev/null 2>&1; then
        print_success "Database schema initialized"
    else
        print_error "Failed to initialize database schema"
        exit 1
    fi
else
    print_info "Step 4: Skipping schema initialization (restart mode)"
    print_info "Using existing database schema"
fi
echo ""

# Step 5: Populate data (only in clean mode and if not disabled)
if [ "$MODE" = "clean" ] && [ "$POPULATE" = true ]; then
    print_info "Step 5: Populating database with sample data"
    if [ -f ./setup_and_populate.sh ]; then
        chmod +x ./setup_and_populate.sh
        if ./setup_and_populate.sh > /dev/null 2>&1; then
            print_success "Sample data populated"
        else
            print_warning "Sample data population failed (continuing anyway)"
        fi
    else
        print_warning "setup_and_populate.sh not found, skipping sample data"
    fi
elif [ "$MODE" = "clean" ] && [ "$POPULATE" = false ]; then
    print_info "Step 5: Skipping data population (--no-populate flag)"
else
    print_info "Step 5: Skipping data population (restart mode)"
    print_info "Using existing data"
fi
echo ""

# Step 6: Check if streaming setup is needed
print_info "Step 6: Checking streaming infrastructure"

# Check if Materialize has views
VIEWS_EXIST=false
VIEW_COUNT=$(PGPASSWORD="" psql -h localhost -p 6875 -U materialize -d materialize -t -A -c \
    "SELECT COUNT(*) FROM mz_materialized_views WHERE name = 'latest_measurements_all';" 2>/dev/null || echo "0")
if [ "$VIEW_COUNT" -gt 0 ] 2>/dev/null; then
    VIEWS_EXIST=true
fi

if [ "$MODE" = "clean" ] || [ "$VIEWS_EXIST" = false ]; then
    print_info "Setting up streaming infrastructure"

    # Wait for Materialize to be ready
    print_info "Waiting for Materialize to be ready..."
    max_attempts=30
    attempt=0
    while [ $attempt -lt $max_attempts ]; do
        if PGPASSWORD="" psql -h localhost -p 6875 -U materialize -d materialize -c "SELECT 1" > /dev/null 2>&1; then
            print_success "Materialize is ready"
            break
        fi
        attempt=$((attempt + 1))
        if [ $((attempt % 5)) -eq 0 ]; then
            echo "Waiting for Materialize... ($attempt/$max_attempts)"
        fi
        sleep 2
    done

    if [ $attempt -eq $max_attempts ]; then
        print_error "Materialize did not become ready in time"
        exit 1
    fi

    # Run streaming setup
    chmod +x ./setup-streaming.sh
    if ./setup-streaming.sh 2>&1 | grep -q "Setup completed successfully"; then
        print_success "Streaming infrastructure setup complete"
    else
        print_error "Failed to setup streaming infrastructure"
        exit 1
    fi
else
    print_success "Streaming infrastructure already exists (skipping setup)"
fi
echo ""

# Step 7: Build Go server
print_info "Step 7: Building Go server"
if go build -o timeseries-api cmd/server/main.go; then
    print_success "Go server built successfully"
else
    print_error "Failed to build Go server"
    exit 1
fi
echo ""

# Step 8: Check if server is already running
print_info "Step 8: Checking for existing server process"
if lsof -ti:8080 > /dev/null 2>&1; then
    print_warning "Port 8080 is already in use"
    read -p "Kill existing process and start new server? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        lsof -ti:8080 | xargs kill -9 2>/dev/null || true
        print_success "Existing process killed"
    else
        print_info "Keeping existing server running"
        print_header "SETUP COMPLETE"
        print_success "System is ready!"
        echo ""
        print_info "Services status:"
        print_info "  - PostgreSQL: localhost:5556 (running)"
        print_info "  - Materialize: localhost:6875 (running)"
        print_info "  - API Server: localhost:8080 (already running)"
        echo ""
        exit 0
    fi
fi

# Start server in background
print_info "Starting API server in background"
nohup ./timeseries-api > server.log 2>&1 &
SERVER_PID=$!
echo $SERVER_PID > server.pid
print_info "Server PID: $SERVER_PID (saved to server.pid)"

# Wait for server to be ready
print_info "Waiting for server to be ready..."
max_attempts=15
attempt=0
while [ $attempt -lt $max_attempts ]; do
    if curl -s http://localhost:8080/api/v1/health > /dev/null 2>&1; then
        print_success "API server is ready"
        break
    fi
    attempt=$((attempt + 1))
    sleep 1
done

if [ $attempt -eq $max_attempts ]; then
    print_error "API server did not become ready in time"
    print_info "Check server.log for details"
    exit 1
fi
echo ""

# Final status
print_header "SETUP COMPLETE"

print_success "All services are running!"
echo ""

print_info "Services status:"
print_info "  - PostgreSQL: localhost:5556 ✓"
print_info "  - Materialize: localhost:6875 ✓"
print_info "  - API Server: localhost:8080 ✓"
echo ""

print_info "API Endpoints:"
print_info "  - Health: http://localhost:8080/api/v1/health"
print_info "  - Swagger: http://localhost:8080/swagger/index.html"
print_info "  - WebSocket: ws://localhost:8080/api/v1/measurements/subscribe"
echo ""

print_info "Testing:"
print_info "  - Batch endpoint: python3 test_batch_endpoint.py"
print_info "  - Streaming: python3 test_streaming_comprehensive.py"
print_info "  - Interactive: open test-streaming-client.html"
echo ""

print_info "Management:"
print_info "  - View logs: tail -f server.log"
print_info "  - Stop server: kill \$(cat server.pid)"
print_info "  - Stop all: docker-compose down"
echo ""

if [ "$MODE" = "clean" ]; then
    print_info "Data status: Fresh installation with sample data"
else
    print_info "Data status: Existing data preserved"
fi
echo ""
