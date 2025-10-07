# Quick Start: Real-Time Streaming Subscriptions

This guide will help you get the real-time streaming feature up and running in 5 minutes.

## Prerequisites

- Docker and Docker Compose
- Go 1.21+
- PostgreSQL client tools (psql)

## Step 1: Start Services (2 minutes)

```bash
# Start PostgreSQL and Materialize
docker-compose up -d

# Wait for services to be ready (about 30 seconds)
sleep 30
```

## Step 2: Initialize Database (1 minute)

```bash
# Initialize PostgreSQL schema (if not already done)
PGPASSWORD=password psql -h localhost -p 5556 -U bdp -d timeseries -f sql-scripts/init-new.sql

# Populate with sample data (optional)
./setup_and_populate.sh
```

## Step 3: Setup Streaming (1 minute)

```bash
# Run the streaming setup script
./setup-streaming.sh
```

This will:
- Create PostgreSQL publication
- Configure Materialize source
- Create materialized views

## Step 4: Start the API Server (1 minute)

```bash
# Install dependencies (first time only)
go mod download

# Build and run
go build -o server cmd/server/main.go
./server
```

The server should log:
```
INFO: Connected to Materialize successfully
INFO: Materialize initial sync completed
INFO: Starting HTTP server on port 8080
```

## Step 5: Test the Streaming (1 minute)

### Option A: Using the HTML Test Client

1. Open `test-streaming-client.html` in your browser
2. Click "Connect"
3. Enter sensor names (comma-separated)
4. Click "Subscribe"
5. Insert test data and watch updates appear in real-time!

### Option B: Using WebSocket CLI

```bash
# Install wscat (Node.js WebSocket client)
npm install -g wscat

# Connect to WebSocket endpoint
wscat -c ws://localhost:8080/api/v1/measurements/subscribe

# Send subscription request
{"action":"subscribe","sensor_names":["test-sensor-1"]}
```

### Option C: Using Python

```python
import asyncio
import websockets
import json

async def test_streaming():
    uri = "ws://localhost:8080/api/v1/measurements/subscribe"
    async with websockets.connect(uri) as ws:
        # Subscribe
        await ws.send(json.dumps({
            "action": "subscribe",
            "sensor_names": ["test-sensor-1"]
        }))

        # Listen for updates
        async for message in ws:
            print(json.loads(message))

asyncio.run(test_streaming())
```

## Testing with Sample Data

Insert a test measurement to see it streamed in real-time:

```bash
curl -X POST http://localhost:8080/api/v1/measurements/batch \
  -H "Content-Type: application/json" \
  -d '{
    "provenance": {
      "lineage": "test",
      "data_collector": "manual",
      "data_collector_version": "1.0"
    },
    "measurements": [
      {
        "sensor_name": "test-sensor-1",
        "type_name": "temperature",
        "timestamp": "'$(date -Iseconds)'",
        "value": 23.5
      }
    ]
  }'
```

You should see the measurement appear immediately in your WebSocket client!

## Subscription Request Format

The subscription endpoint accepts the exact same parameters as the `GetLatestMeasurements` endpoint:

```json
{
  "action": "subscribe",
  "sensor_names": ["sensor1", "sensor2"],
  "type_names": ["temperature", "humidity"]
}
```

### With Geospatial Filter

**Bounding Box:**
```json
{
  "action": "subscribe",
  "sensor_names": ["sensor1"],
  "spatial_filter": {
    "type": "bbox",
    "coordinates": [11.0, 46.0, 12.0, 47.0]
  }
}
```

**Radius:**
```json
{
  "action": "subscribe",
  "sensor_names": ["sensor1"],
  "spatial_filter": {
    "type": "radius",
    "coordinates": [11.5, 46.5, 5000]
  }
}
```

### Advanced Discovery-Based Subscriptions

Instead of manually specifying sensor names, you can use DiscoverSensors-style filters to automatically find and subscribe to sensors:

**Subscribe to all sensors with temperature readings above 30°C:**
```json
{
  "action": "subscribe",
  "timeseries_filter": {
    "required_types": ["temperature"]
  },
  "measurement_filter": {
    "latest_only": true,
    "expression": "temperature.gteq.30"
  },
  "limit": 50
}
```

**Subscribe to parking sensors in downtown:**
```json
{
  "action": "subscribe",
  "timeseries_filter": {
    "required_types": ["occupancy", "location"]
  },
  "spatial_filter": {
    "type": "bbox",
    "coordinates": [11.3, 46.4, 11.4, 46.5]
  }
}
```

The server will automatically discover matching sensors and subscribe to them. The acknowledgment message includes the list of discovered sensors.

See `test_streaming_discovery.py` for more examples!

## Architecture Overview

```
Client (WebSocket)
    ↓
Go Service (Filters & Pushes)
    ↓
Materialize (TAIL subscription)
    ↓
PostgreSQL (Logical Replication)
```

### How It Works

1. **PostgreSQL** stores all data and emits changes via logical replication
2. **Materialize** subscribes to PostgreSQL and maintains real-time views of latest measurements
3. **Go Service** subscribes to Materialize views using TAIL and applies geospatial filters
4. **WebSocket** clients receive filtered updates in real-time

## Monitoring

### Check Materialize Status

```bash
# Connect to Materialize
PGPASSWORD="" psql -h localhost -p 6875 -U materialize -d materialize

# Check views
SELECT COUNT(*) FROM latest_measurements_all;
```

### Check Server Logs

```bash
# Server will log connection events
tail -f server.log
```

## Troubleshooting

### Issue: "Failed to connect to Materialize"

**Solution:** Ensure Materialize container is running:
```bash
docker-compose ps
docker-compose logs materialize
```

### Issue: "Publication does not exist"

**Solution:** Run the setup script again:
```bash
./setup-streaming.sh
```

### Issue: No data appearing in subscription

**Checklist:**
1. Is the sensor name correct?
2. Is data being inserted into PostgreSQL?
3. Is Materialize source running? (Check `SELECT * FROM mz_sources`)
4. Are materialized views populated? (Check `SELECT COUNT(*) FROM latest_measurements_all`)

### Issue: WebSocket connection drops immediately

**Solution:** Check CORS settings and verify the WebSocket URL is correct.

## Next Steps

- Read the full [STREAMING.md](STREAMING.md) documentation
- Explore the API documentation at http://localhost:8080/api/swagger/index.html
- Check out example client code in various languages
- Configure authentication and authorization for production use

## Clean Up

```bash
# Stop all services
docker-compose down

# Remove volumes (WARNING: deletes all data)
docker-compose down -v
```

## Support

For issues and questions, please check:
- [STREAMING.md](STREAMING.md) - Detailed documentation
- [README.md](README.md) - Main project documentation
- Server logs for error messages

---

**Congratulations!** 🎉 You now have real-time streaming subscriptions working with geospatial filtering!
