# Complete Setup and Test Guide - Real-Time Streaming

This is the complete guide for setting up and testing the real-time streaming subscription system.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Architecture Overview](#architecture-overview)
3. [Setup Instructions](#setup-instructions)
4. [Memory Optimization](#memory-optimization)
5. [Testing the System](#testing-the-system)
6. [API Usage](#api-usage)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

- **Docker & Docker Compose** - For PostgreSQL and Materialize
- **Go 1.21+** - For the API server
- **Python 3.8+** - For test scripts
- **PostgreSQL client tools** (`psql`) - For database setup

### Install Python Dependencies

```bash
pip install websockets requests
```

---

## Architecture Overview

The streaming system has 4 layers:

```
┌─────────────────────────────────────────────────────────────────┐
│ Layer 1: PostgreSQL (Source of Truth)                           │
│  - All measurement data stored here                             │
│  - Logical replication enabled                                  │
│  - 279,869 measurements across 6 data types                     │
└────────────────────┬────────────────────────────────────────────┘
                     │ Logical Replication (WAL streaming)
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ Layer 2: Materialize (Streaming Database)                       │
│  - Source tables: Mirror of PostgreSQL partitions              │
│  - Materialized views: ONLY LATEST measurements per timeseries  │
│  - Memory: Bounded by # of timeseries (500), not total (279k)  │
└────────────────────┬────────────────────────────────────────────┘
                     │ TAIL/SUBSCRIBE (incremental updates)
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ Layer 3: Go Service (Filtering & Push)                          │
│  - Subscribes to Materialize views (500 rows, not 279k!)       │
│  - Applies geospatial filtering (bbox, radius)                  │
│  - Manages WebSocket connections                                │
└────────────────────┬────────────────────────────────────────────┘
                     │ WebSocket (real-time push)
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ Layer 4: Clients (WebSocket Consumers)                          │
│  - Receive real-time updates (< 200ms latency)                  │
│  - Filter by sensor names, types, spatial bounds                │
└─────────────────────────────────────────────────────────────────┘
```

**Key Point:** Materialize source tables contain all data, but **TAIL subscriptions only monitor the materialized views** which contain latest measurements only. This keeps memory usage bounded.

---

## Setup Instructions

### Step 1: Start Docker Services (2 minutes)

```bash
# Navigate to project directory
cd OdhTimeseries

# Start PostgreSQL and Materialize
docker-compose up -d

# Verify services are running
docker-compose ps
```

**Expected output:**
```
NAME                          STATUS
odhtimeseries-db-1            Up (PostgreSQL 16)
odhtimeseries-materialize-1   Up (Materialize v0.159.1)
```

### Step 2: Initialize PostgreSQL (1 minute)

If not already done, initialize the schema:

```bash
export PGPASSWORD=password
psql -h localhost -p 5556 -U bdp -d timeseries -f sql-scripts/init-new.sql
```

If you need test data:

```bash
./setup_and_populate.sh
```

### Step 3: Configure PostgreSQL for Replication (30 seconds)

```bash
export PGPASSWORD=password
psql -h localhost -p 5556 -U bdp -d timeseries << 'EOF'
-- Drop existing publication if it exists
DROP PUBLICATION IF EXISTS timeseries_publication;

-- Create publication for specific measurement partition tables
CREATE PUBLICATION timeseries_publication FOR TABLE
  intimev3.provenance,
  intimev3.sensors,
  intimev3.types,
  intimev3.datasets,
  intimev3.dataset_types,
  intimev3.timeseries,
  intimev3.measurements_numeric_2025,
  intimev3.measurements_numeric_2025_p1,
  intimev3.measurements_numeric_2025_p2,
  intimev3.measurements_numeric_2025_p3,
  intimev3.measurements_string_2025,
  intimev3.measurements_string_2025_p1,
  intimev3.measurements_string_2025_p2,
  intimev3.measurements_string_2025_p3,
  intimev3.measurements_json_2025,
  intimev3.measurements_json_2025_p1,
  intimev3.measurements_json_2025_p2,
  intimev3.measurements_json_2025_p3,
  intimev3.measurements_geoposition_2025,
  intimev3.measurements_geoposition_2025_p1,
  intimev3.measurements_geoposition_2025_p2,
  intimev3.measurements_geoposition_2025_p3,
  intimev3.measurements_geoshape_2025,
  intimev3.measurements_geoshape_2025_p1,
  intimev3.measurements_geoshape_2025_p2,
  intimev3.measurements_geoshape_2025_p3,
  intimev3.measurements_boolean_2025,
  intimev3.measurements_boolean_2025_p1,
  intimev3.measurements_boolean_2025_p2,
  intimev3.measurements_boolean_2025_p3;

-- Set REPLICA IDENTITY FULL for all tables
ALTER TABLE intimev3.provenance REPLICA IDENTITY FULL;
ALTER TABLE intimev3.sensors REPLICA IDENTITY FULL;
ALTER TABLE intimev3.types REPLICA IDENTITY FULL;
ALTER TABLE intimev3.datasets REPLICA IDENTITY FULL;
ALTER TABLE intimev3.dataset_types REPLICA IDENTITY FULL;
ALTER TABLE intimev3.timeseries REPLICA IDENTITY FULL;
-- (continues for all partition tables...)

SELECT 'PostgreSQL replication configured successfully!' as status;
EOF
```

### Step 4: Setup Materialize (2 minutes)

```bash
export PGPASSWORD=""
psql -h localhost -p 6875 -U materialize -d materialize -f sql-scripts/materialize-setup.sql
```

**What this does:**
1. Creates PostgreSQL connection and source
2. Mirrors metadata tables (sensors, types, timeseries)
3. Mirrors measurement partition tables
4. Creates union views for all partitions
5. Creates materialized views with **ONLY latest measurements**

**Wait for initial sync:**

```bash
export PGPASSWORD=""
psql -h localhost -p 6875 -U materialize -d materialize << 'EOF'
-- Check data counts
SELECT 'sensors' as table_name, COUNT(*) as count FROM sensors
UNION ALL
SELECT 'timeseries', COUNT(*) FROM timeseries
UNION ALL
SELECT 'latest_measurements_all', COUNT(*) FROM latest_measurements_all
ORDER BY table_name;
EOF
```

**Expected output:**
```
table_name               | count
-------------------------+-------
latest_measurements_all  | 500
sensors                  | 100
timeseries               | 500
```

✅ If you see these counts, Materialize is ready!

### Step 5: Start the Go Server (30 seconds)

```bash
# Build the server
go build -o server cmd/server/main.go

# Start the server
./server
```

**Expected logs:**
```json
{"level":"info","msg":"Starting timeseries API server","time":"..."}
{"level":"info","msg":"Connected to Materialize successfully","time":"..."}
{"level":"info","msg":"Waiting for Materialize initial sync to complete...","time":"..."}
{"count":500,"level":"info","msg":"Materialize initial sync completed","time":"..."}
{"level":"info","msg":"Starting HTTP server","port":8080,"time":"..."}
```

✅ Server is running on http://localhost:8080

---

## Memory Optimization

### How It Works

**Question:** Won't Materialize consume too much memory with 279,869 measurements?

**Answer:** No! Here's why:

1. **Source Tables** (in Materialize):
   - Yes, they contain all 279,869 measurements
   - Needed for Materialize to compute incremental updates
   - Stored on disk, not all in memory

2. **Materialized Views** (what we TAIL):
   - `latest_measurements_all`: **Only 500 rows** (latest per timeseries)
   - This is what the Go service subscribes to
   - Memory usage: Bounded by # of timeseries, not total measurements

3. **TAIL Subscriptions** (Go service):
   - Only receives updates from the views (500 rows)
   - Memory per subscription: ~4KB + channel buffer
   - Not affected by the 279k source table size

**Verification:**

```bash
export PGPASSWORD=""
psql -h localhost -p 6875 -U materialize -d materialize << 'EOF'
-- Source tables (large, on disk)
SELECT 'Source: measurements_numeric_p1' as view, COUNT(*) FROM measurements_numeric_p1
UNION ALL
-- Materialized view (small, in memory for TAIL)
SELECT 'View: latest_measurements_all', COUNT(*) FROM latest_measurements_all;
EOF
```

**Output:**
```
view                              | count
----------------------------------+--------
Source: measurements_numeric_p1   | 69,967  ← Large (on disk)
View: latest_measurements_all     | 500     ← Small (monitored by TAIL)
```

**Conclusion:** Memory usage is **O(timeseries_count)**, not O(total_measurements). ✅

---

## Testing the System

### Test 1: Quick Verification (1 minute)

Open your browser to http://localhost:8080/ and you should see:

```json
{
  "service": "timeseries-api",
  "version": "1.0.0",
  "documentation": "/api/swagger/index.html"
}
```

### Test 2: Interactive HTML Client (2 minutes)

1. Open `test-streaming-client.html` in your browser
2. Click **Connect**
3. Enter sensor names (comma-separated): `HUM_Park_067, PARK_Highway_052`
4. Click **Subscribe**
5. Watch real-time updates appear!

### Test 3: Automated Test Script (3 minutes)

```bash
# Run the comprehensive test
python3 test_streaming.py
```

**What it tests:**
- ✅ WebSocket connection establishment
- ✅ Subscription with sensor names
- ✅ Subscription with spatial filters
- ✅ Real-time update delivery
- ✅ Multiple concurrent subscriptions

### Test 4: Manual Data Insertion Test

**Terminal 1** - Start monitoring:

```bash
# Open WebSocket connection using wscat
npm install -g wscat
wscat -c ws://localhost:8080/api/v1/measurements/subscribe

# Send subscription
{"action":"subscribe","sensor_names":["HUM_Park_067"]}
```

**Terminal 2** - Insert data:

```bash
export PGPASSWORD=password
psql -h localhost -p 5556 -U bdp -d timeseries << 'EOF'
INSERT INTO intimev3.measurements_numeric_2025 (timeseries_id, timestamp, value, provenance_id, created_on)
SELECT ts.id, NOW(), 999.99, 1, NOW()
FROM intimev3.timeseries ts
JOIN intimev3.sensors s ON ts.sensor_id = s.id
JOIN intimev3.types t ON ts.type_id = t.id
WHERE s.name = 'HUM_Park_067' AND t.name = 'power_generation'
LIMIT 1;
EOF
```

**Terminal 1** - You should see:

```json
{
  "type": "data",
  "data": {
    "sensor_name": "HUM_Park_067",
    "type_name": "power_generation",
    "value": "999.99",
    "timestamp": "2025-10-07T..."
  }
}
```

✅ **Latency:** Typically < 200ms from INSERT to WebSocket delivery!

---

## API Usage

### WebSocket Endpoint

```
ws://localhost:8080/api/v1/measurements/subscribe
```

### Subscription Request

```json
{
  "action": "subscribe",
  "sensor_names": ["sensor1", "sensor2"],
  "type_names": ["temperature", "humidity"],
  "spatial_filter": {
    "type": "bbox",
    "coordinates": [10.5, 46.2, 12.5, 47.2]
  }
}
```

### Spatial Filters

**Bounding Box:**
```json
{
  "type": "bbox",
  "coordinates": [minLon, minLat, maxLon, maxLat]
}
```

**Radius:**
```json
{
  "type": "radius",
  "coordinates": [centerLon, centerLat, radiusMeters]
}
```

### Response Format

**Acknowledgment:**
```json
{
  "type": "ack",
  "message": "Subscription created successfully"
}
```

**Data Update:**
```json
{
  "type": "data",
  "data": {
    "timeseries_id": "uuid",
    "sensor_name": "sensor1",
    "type_name": "temperature",
    "timestamp": "2025-10-07T12:00:00Z",
    "value": "23.5",
    "data_type": "numeric",
    "unit": "°C"
  }
}
```

**Error:**
```json
{
  "type": "error",
  "error": "Error message"
}
```

### Python Client Example

```python
import asyncio
import websockets
import json

async def subscribe():
    uri = "ws://localhost:8080/api/v1/measurements/subscribe"

    async with websockets.connect(uri) as ws:
        # Subscribe
        await ws.send(json.dumps({
            "action": "subscribe",
            "sensor_names": ["sensor1", "sensor2"]
        }))

        # Receive updates
        async for message in ws:
            data = json.loads(message)
            print(f"Received: {data['type']}")
            if data['type'] == 'data':
                print(f"  {data['data']['sensor_name']} = {data['data']['value']}")

asyncio.run(subscribe())
```

### JavaScript Client Example

```javascript
const ws = new WebSocket('ws://localhost:8080/api/v1/measurements/subscribe');

ws.onopen = () => {
  // Subscribe
  ws.send(JSON.stringify({
    action: 'subscribe',
    sensor_names: ['sensor1', 'sensor2'],
    spatial_filter: {
      type: 'bbox',
      coordinates: [10.5, 46.2, 12.5, 47.2]
    }
  }));
};

ws.onmessage = (event) => {
  const data = JSON.parse(event.data);
  console.log('Received:', data.type);
  if (data.type === 'data') {
    console.log(`${data.data.sensor_name} = ${data.data.value}`);
  }
};
```

---

## Advanced Usage: DiscoverSensors Integration

For more powerful filtering, combine the REST API with WebSocket streaming:

**Step 1:** Discover sensors using advanced filters:

```bash
curl -X POST http://localhost:8080/api/v1/sensors \
  -H "Content-Type: application/json" \
  -d '{
    "timeseries_filter": {
      "required_types": ["temperature", "humidity"]
    },
    "measurement_filter": {
      "expression": "temperature > 25",
      "latest_only": true
    },
    "limit": 50
  }'
```

**Response:**
```json
{
  "sensors": [
    {"name": "sensor1", ...},
    {"name": "sensor2", ...}
  ],
  "count": 2
}
```

**Step 2:** Subscribe to discovered sensors:

```javascript
// Extract sensor names from discovery response
const sensorNames = sensors.map(s => s.name);

// Subscribe via WebSocket
ws.send(JSON.stringify({
  action: 'subscribe',
  sensor_names: sensorNames
}));
```

This gives you the full power of DiscoverSensors for real-time streaming!

---

## Troubleshooting

### PostgreSQL Not Starting

```bash
docker-compose logs db
```

**Common issues:**
- Port 5556 already in use: Change port in `docker-compose.yml`
- Insufficient disk space: Clean up Docker volumes

### Materialize Not Starting

```bash
docker-compose logs materialize
```

**Common issues:**
- PostgreSQL not ready: Wait 30 seconds after starting PostgreSQL
- Network issues: Check `docker-compose ps`

### Materialize Sync Slow/Failed

```bash
export PGPASSWORD=""
psql -h localhost -p 6875 -U materialize -d materialize << 'EOF'
-- Check source status
SELECT name, type, status FROM mz_sources;

-- Check for errors
SELECT * FROM mz_internal.mz_source_statuses WHERE error IS NOT NULL;
EOF
```

**Fix:**
- Drop and recreate source: Run `sql-scripts/materialize-setup.sql` again
- Check PostgreSQL publication: `SELECT * FROM pg_publication;`

### Go Server Can't Connect to Materialize

**Check logs:**
```bash
./server 2>&1 | grep -i materialize
```

**Common issues:**
- Materialize not ready: Wait 60 seconds after starting
- Wrong credentials: Check `cmd/server/main.go` configuration

### WebSocket Connection Drops

**Client side:**
- Check network stability
- Implement reconnection logic
- Use heartbeat/ping-pong

**Server side:**
```bash
# Check server logs for errors
tail -f server.log | grep -i websocket
```

### No Updates Received

**Checklist:**
1. Is data being inserted? `SELECT COUNT(*) FROM intimev3.measurements_numeric_2025;`
2. Is Materialize view updating? `SELECT COUNT(*) FROM latest_measurements_all;`
3. Is sensor name correct? Check exact spelling
4. Are sensors active? `SELECT * FROM intimev3.sensors WHERE name = 'your-sensor';`

---

## Performance Tuning

### For High Throughput

**Materialize:**
- Increase cluster size in Materialize Cloud
- Add indexes on frequently filtered columns

**Go Service:**
- Deploy multiple instances behind load balancer
- Increase connection pool size
- Add connection rate limiting

**Clients:**
- Implement batching for high-frequency updates
- Use compression for WebSocket messages

### For Low Latency

**PostgreSQL:**
- Tune `max_wal_senders` and `wal_sender_timeout`
- Use SSDs for WAL storage

**Materialize:**
- Use dedicated cluster for streaming workloads
- Keep materialized views small and focused

**Network:**
- Deploy services in same region/availability zone
- Use dedicated network for replication

---

## Files Reference

### Setup
- `docker-compose.yml` - PostgreSQL + Materialize services
- `sql-scripts/init-new.sql` - PostgreSQL schema
- `sql-scripts/materialize-setup.sql` - Materialize source and views
- `setup-streaming.sh` - Automated setup script

### Implementation
- `cmd/server/main.go` - Main server with streaming
- `internal/streaming/materialize.go` - Materialize client
- `internal/streaming/websocket.go` - WebSocket manager
- `internal/handlers/streaming.go` - HTTP handler

### Testing
- `test-streaming-client.html` - Interactive browser client
- `test_streaming.py` - Automated test script
- `test_streaming_manual.py` - Manual SQL insertion test

### Documentation
- `SETUP-AND-TEST-GUIDE.md` - This file
- `TEST_REPORT.md` - Comprehensive test results
- `IMPLEMENTATION-SUMMARY.md` - Architecture details

---

## Next Steps

1. **Production Deployment**:
   - Add JWT authentication for WebSocket
   - Implement rate limiting per connection
   - Set up monitoring (Prometheus + Grafana)
   - Use Materialize Cloud for HA

2. **Advanced Features**:
   - Historical data streaming with time ranges
   - Aggregation subscriptions (avg, sum, etc.)
   - Multi-region deployment
   - GraphQL subscriptions

3. **Optimization**:
   - Connection pooling
   - Message batching
   - Compression
   - Edge caching

---

## Support

For issues:
- Check logs: `docker-compose logs` and `./server` output
- Review `TEST_REPORT.md` for known issues
- Check `IMPLEMENTATION-SUMMARY.md` for architecture details

**System is fully operational and tested!** 🎉
