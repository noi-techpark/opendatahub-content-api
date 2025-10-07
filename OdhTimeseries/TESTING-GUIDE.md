# Testing Guide

## Test Scripts Overview

### Main Test Scripts

| Script | Purpose | What it Tests |
|--------|---------|---------------|
| `run_e2e_test.sh` | **Full E2E test** | Complete system from Docker to API testing |
| `test_batch_endpoint.py` | **Batch API testing** | /batch endpoint with all data types |
| `test_streaming_comprehensive.py` | **Streaming tests** | All streaming features (simple, discovery, spatial) |
| `test-streaming-client.html` | **Manual testing** | Interactive WebSocket test client |

### Removed Scripts (Consolidated)

The following scripts have been **removed** and consolidated into `test_streaming_comprehensive.py`:
- ~~`test_streaming.py`~~ (basic streaming)
- ~~`test_streaming_manual.py`~~ (manual SQL inserts)
- ~~`test_streaming_discovery.py`~~ (discovery subscriptions)

## Quick Start

### 1. Full E2E Test (Recommended)

Tests everything from scratch:
```bash
./run_e2e_test.sh
```

**What it does:**
1. ✅ Cleans Docker volumes
2. ✅ Starts PostgreSQL + Materialize
3. ✅ Initializes database schema
4. ✅ Populates sample data
5. ✅ Sets up streaming infrastructure
6. ✅ Builds Go server
7. ✅ Starts API server
8. ✅ Tests /batch endpoint
9. ✅ Tests streaming subscriptions

### 2. Test Batch Endpoint

Tests the `/api/v1/measurements/batch` endpoint:
```bash
python3 test_batch_endpoint.py
```

**Tests:**
- Numeric measurements (50 items)
- String measurements (20 items)
- JSON measurements (15 items)
- Geoposition measurements (10 items)
- Large batch (500 items) - performance
- Mixed data types

**Performance metrics:**
- Measures throughput (measurements/sec)
- Validates response times
- Checks success rates

### 3. Test Streaming (Comprehensive)

Tests all streaming subscription features:
```bash
python3 test_streaming_comprehensive.py
```

**Tests:**
1. ✅ Simple subscription (sensor_names)
2. ✅ Discovery subscription (required_types)
3. ✅ Discovery with spatial filter (bbox)
4. ✅ Manual SQL insert + streaming update
5. ✅ Discovery with measurement filter
6. ✅ Validation (simple mode rejects spatial_filter)

### 4. Interactive Testing

Open in browser for manual testing:
```bash
open test-streaming-client.html
# or
firefox test-streaming-client.html
```

**Features:**
- Connection management
- Simple/Discovery mode switching
- Spatial filter configuration (discovery mode only)
- Real-time message log
- Statistics tracking
- Quick example buttons

## Test Requirements

### Prerequisites
- Go 1.21+
- Python 3.8+
- Docker & Docker Compose
- PostgreSQL client tools (psql)
- Python packages: `websockets`, `requests`

### Install Python Dependencies
```bash
pip install websockets requests
```

## Test Scenarios

### Scenario 1: Fresh Installation

```bash
# Start from clean state
docker-compose down -v
./run_e2e_test.sh
```

### Scenario 2: Test After Code Changes

```bash
# Rebuild and test
go build -o timeseries-api cmd/server/main.go
pkill -f timeseries-api
./timeseries-api &
sleep 3
python3 test_streaming_comprehensive.py
```

### Scenario 3: Manual Testing

```bash
# Start server
go run cmd/server/main.go &

# Insert test data
python3 test_batch_endpoint.py

# Test streaming
python3 test_streaming_comprehensive.py

# Or use HTML client
open test-streaming-client.html
```

## Understanding Test Results

### Batch Endpoint Tests

**Success Criteria:**
- ✅ All 6 tests pass
- ✅ Throughput > 50 measurements/sec (good: >100)
- ✅ No errors in responses

**Common Issues:**
- Server not running: Start with `go run cmd/server/main.go`
- Database not initialized: Run `./setup_and_populate.sh`
- Port conflict: Kill existing process with `pkill -f timeseries-api`

### Streaming Tests

**Success Criteria:**
- ✅ All 6 tests pass
- ✅ Simple subscriptions work
- ✅ Discovery subscriptions find sensors
- ✅ Spatial filtering works in discovery mode
- ✅ Simple mode correctly rejects spatial_filter
- ✅ Real-time updates received (< 10 seconds)

**Common Issues:**
- Materialize not running: Check `docker-compose ps`
- Streaming not setup: Run `./setup-streaming.sh`
- No updates received: Check data exists with `/latest` endpoint

## Test Output Examples

### Successful Test Output

```
==========================================
     END-TO-END TEST SCRIPT
==========================================

✓ Docker cleanup complete
✓ Docker services started
✓ PostgreSQL is ready
✓ Database schema initialized
✓ Sample data populated
✓ Streaming infrastructure setup complete
✓ Go server built successfully
✓ Go server started successfully
✓ Batch endpoint tests passed
✓ All streaming tests passed

==========================================
   ✅ ALL E2E TESTS PASSED!
==========================================
```

### Streaming Test Output

```
======================================================================
            TEST 1: Simple Subscription (sensor_names)
======================================================================

ℹ Connected to WebSocket: ws://localhost:8080/api/v1/measurements/subscribe
ℹ Subscribing to sensor: HUM_Park_067
✓ Simple subscription acknowledged
ℹ Listening for initial updates (3 seconds)...
ℹ Update 1: HUM_Park_067 = 1032.3
ℹ Update 2: HUM_Park_067 = 7.1
✓ Received 2 initial updates
```

## Troubleshooting

### Port 8080 Already in Use

```bash
# Find and kill process
lsof -ti:8080 | xargs kill -9

# Or use pkill
pkill -f timeseries-api
```

### Docker Services Not Starting

```bash
# Check status
docker-compose ps

# View logs
docker-compose logs db
docker-compose logs materialize

# Restart
docker-compose down -v
docker-compose up -d
```

### Materialize Sync Issues

```bash
# Check Materialize views
PGPASSWORD="" psql -h localhost -p 6875 -U materialize -d materialize -c \
  "SELECT COUNT(*) FROM latest_measurements_all;"

# Should return ~500 rows
```

### No Streaming Updates

```bash
# Check if data exists
curl http://localhost:8080/api/v1/measurements/latest \
  -H "Content-Type: application/json" \
  -d '{"sensor_names": ["HUM_Park_067"]}'

# Insert test data
python3 test_batch_endpoint.py
```

## Performance Benchmarks

### Expected Performance

| Metric | Target | Good | Excellent |
|--------|--------|------|-----------|
| Batch throughput | >50/sec | >100/sec | >200/sec |
| Streaming latency | <500ms | <200ms | <100ms |
| WebSocket connect | <2s | <1s | <500ms |
| Discovery query | <5s | <2s | <1s |

### Measuring Performance

The test scripts automatically measure and report:
- Throughput (measurements/second)
- Response times
- Update latency
- Connection time

## Continuous Integration

### CI/CD Integration

```yaml
# Example GitHub Actions workflow
- name: Run E2E Tests
  run: |
    ./run_e2e_test.sh

- name: Test Batch Endpoint
  run: |
    python3 test_batch_endpoint.py

- name: Test Streaming
  run: |
    python3 test_streaming_comprehensive.py
```

## Getting Help

### Check Logs

```bash
# Server logs
tail -f server.log

# Docker logs
docker-compose logs -f

# Materialize logs
docker-compose logs materialize
```

### Common Commands

```bash
# Status check
docker-compose ps
curl http://localhost:8080/api/v1/health

# Quick test
python3 test_batch_endpoint.py

# Full test
./run_e2e_test.sh
```

## Next Steps

After all tests pass:
1. ✅ System is ready for development
2. ✅ Try the HTML test client
3. ✅ Review API documentation in Swagger: http://localhost:8080/swagger/index.html
4. ✅ Start building your application!
