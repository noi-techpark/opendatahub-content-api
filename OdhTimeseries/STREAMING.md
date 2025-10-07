# Real-Time Streaming Subscriptions with Materialize

This document describes the real-time streaming subscription system built on top of Materialize for the Timeseries API.

## Overview

The streaming system allows clients to subscribe to measurement updates in real-time using WebSockets. The architecture leverages:

- **PostgreSQL**: Primary database storing all timeseries data
- **Materialize**: Streaming database that maintains real-time views of latest measurements
- **WebSocket**: Real-time communication protocol for pushing updates to clients
- **Go Service**: Applies geospatial filtering and manages WebSocket connections

## Architecture

```
┌─────────────┐
│  Client     │
│ (WebSocket) │
└──────┬──────┘
       │
       │ WebSocket
       │
┌──────▼──────────────────────────────────┐
│  Go Service                              │
│  ┌────────────────────────────────────┐ │
│  │ WebSocket Manager                  │ │
│  │ - Connection management            │ │
│  │ - Geospatial filtering             │ │
│  └────────┬───────────────────────────┘ │
│           │                              │
│  ┌────────▼───────────────────────────┐ │
│  │ Materialize Client                 │ │
│  │ - TAIL subscription                │ │
│  │ - Incremental updates              │ │
│  └────────┬───────────────────────────┘ │
└───────────┼──────────────────────────────┘
            │
            │ TAIL (Subscribe)
            │
┌───────────▼──────────────────────────────┐
│  Materialize                              │
│  ┌────────────────────────────────────┐  │
│  │ Materialized Views                 │  │
│  │ - latest_measurements_numeric      │  │
│  │ - latest_measurements_string       │  │
│  │ - latest_measurements_json         │  │
│  │ - latest_measurements_geoposition  │  │
│  │ - latest_measurements_geoshape     │  │
│  │ - latest_measurements_boolean      │  │
│  │ - latest_measurements_all (union)  │  │
│  └────────▲───────────────────────────┘  │
│           │                               │
│  ┌────────┴───────────────────────────┐  │
│  │ PostgreSQL Source                  │  │
│  │ - Logical replication              │  │
│  └────────▲───────────────────────────┘  │
└───────────┼───────────────────────────────┘
            │
            │ Logical Replication
            │
┌───────────▼───────────────────────────────┐
│  PostgreSQL (Primary Database)            │
│  - sensors                                 │
│  - types                                   │
│  - timeseries                              │
│  - measurements_* (partitioned)            │
└────────────────────────────────────────────┘
```

## Setup Instructions

### 1. Start Docker Services

Start PostgreSQL and Materialize containers:

```bash
docker-compose up -d
```

This will start:
- PostgreSQL on port 5556 with logical replication enabled
- Materialize on port 6875

### 2. Initialize Database Schema

If not already done, initialize the PostgreSQL schema:

```bash
PGPASSWORD=password psql -h localhost -p 5556 -U bdp -d timeseries -f sql-scripts/init-new.sql
```

### 3. Setup Streaming Infrastructure

Run the streaming setup script:

```bash
./setup-streaming.sh
```

This script will:
1. Create PostgreSQL publication for logical replication
2. Wait for Materialize to be ready
3. Create Materialize source from PostgreSQL
4. Create materialized views for latest measurements

### 4. Start the Go Service

```bash
go run cmd/server/main.go
```

The service will:
- Connect to PostgreSQL
- Connect to Materialize
- Wait for initial sync to complete
- Start HTTP server with WebSocket endpoint

## API Usage

### WebSocket Endpoint

```
ws://localhost:8080/api/v1/measurements/subscribe
```

### Subscription Request Format

After establishing a WebSocket connection, send a subscription request:

```json
{
  "action": "subscribe",
  "sensor_names": ["sensor1", "sensor2"],
  "type_names": ["temperature", "humidity"],
  "spatial_filter": {
    "type": "bbox",
    "coordinates": [11.0, 46.0, 12.0, 47.0]
  }
}
```

#### Parameters

- **action**: Must be "subscribe" or "unsubscribe"
- **sensor_names**: Array of sensor names to subscribe to (required for simple subscriptions)
- **type_names**: Array of measurement type names (optional, if not provided all types are included)
- **spatial_filter**: Geospatial filter (optional)

### Advanced Discovery-Based Subscriptions

Instead of specifying sensor names directly, you can use DiscoverSensors-style filters to automatically discover and subscribe to sensors matching complex criteria.

#### Discovery Subscription Format

```json
{
  "action": "subscribe",
  "timeseries_filter": {
    "required_types": ["temperature", "humidity"],
    "optional_types": ["pressure"],
    "dataset_ids": ["weather_stations"]
  },
  "measurement_filter": {
    "latest_only": true,
    "expression": "temperature.gteq.20",
    "time_range": {
      "start_time": "2024-01-01T00:00:00Z",
      "end_time": "2024-12-31T23:59:59Z"
    }
  },
  "spatial_filter": {
    "type": "bbox",
    "coordinates": [10.5, 46.2, 12.5, 47.2]
  },
  "limit": 100
}
```

#### Discovery Parameters

- **timeseries_filter**: Filter sensors by the timeseries types they own
  - **required_types**: Sensors must have ALL of these measurement types
  - **optional_types**: Sensors may have ANY of these measurement types
  - **dataset_ids**: Filter by dataset membership

- **measurement_filter**: Filter sensors by their measurement values
  - **latest_only**: Only consider latest measurements (default: false)
  - **expression**: Value filter expression (e.g., `"temperature.gteq.20"`, `"or(temp.eq.25, humidity.lt.50)"`)
  - **time_range**: Time constraints for measurements

- **spatial_filter**: Same as simple subscriptions (bbox, radius)
- **limit**: Maximum number of sensors to subscribe to

#### Discovery Response

The acknowledgment includes the list of discovered sensors:

```json
{
  "type": "ack",
  "message": "Subscription created successfully",
  "data": {
    "sensor_count": 15,
    "sensor_names": ["sensor1", "sensor2", "sensor3", "..."],
    "type_names": null
  }
}
```

#### Use Cases

**1. Subscribe to all weather stations with high temperatures:**
```json
{
  "action": "subscribe",
  "timeseries_filter": {
    "required_types": ["temperature"],
    "dataset_ids": ["weather_stations"]
  },
  "measurement_filter": {
    "latest_only": true,
    "expression": "temperature.gteq.30"
  }
}
```

**2. Subscribe to parking sensors in a specific area:**
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

**3. Subscribe to any sensor with power generation data:**
```json
{
  "action": "subscribe",
  "timeseries_filter": {
    "required_types": ["power_generation"]
  },
  "limit": 50
}
```

#### Spatial Filters

**Bounding Box Filter:**
```json
{
  "type": "bbox",
  "coordinates": [minLon, minLat, maxLon, maxLat]
}
```

**Radius Filter:**
```json
{
  "type": "radius",
  "coordinates": [centerLon, centerLat, radiusMeters]
}
```

### Response Format

**Acknowledgment Response:**
```json
{
  "type": "ack",
  "message": "Subscription created successfully",
  "data": {
    "sensor_names": ["sensor1", "sensor2"],
    "type_names": ["temperature"]
  }
}
```

**Data Update Response:**
```json
{
  "type": "data",
  "data": {
    "timeseries_id": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2024-01-01T12:00:00Z",
    "value": "23.5",
    "sensor_name": "sensor1",
    "type_name": "temperature",
    "data_type": "numeric",
    "unit": "°C",
    "sensor_id": 123,
    "type_id": 456,
    "provenance_id": 789,
    "created_on": "2024-01-01T12:00:01Z",
    "sensor_metadata": {"location": "downtown"},
    "diff": 1
  }
}
```

**Error Response:**
```json
{
  "type": "error",
  "error": "At least one sensor name must be provided"
}
```

## Example Client Code

### JavaScript/Node.js

```javascript
const WebSocket = require('ws');

const ws = new WebSocket('ws://localhost:8080/api/v1/measurements/subscribe');

ws.on('open', () => {
  console.log('Connected to streaming API');

  // Subscribe to measurements
  ws.send(JSON.stringify({
    action: 'subscribe',
    sensor_names: ['sensor1', 'sensor2'],
    type_names: ['temperature'],
    spatial_filter: {
      type: 'bbox',
      coordinates: [11.0, 46.0, 12.0, 47.0]
    }
  }));
});

ws.on('message', (data) => {
  const response = JSON.parse(data);

  if (response.type === 'ack') {
    console.log('Subscription acknowledged:', response.message);
  } else if (response.type === 'data') {
    console.log('New measurement:', response.data);
  } else if (response.type === 'error') {
    console.error('Error:', response.error);
  }
});

ws.on('close', () => {
  console.log('Disconnected from streaming API');
});

ws.on('error', (error) => {
  console.error('WebSocket error:', error);
});
```

### Python

```python
import asyncio
import websockets
import json

async def subscribe_to_measurements():
    uri = "ws://localhost:8080/api/v1/measurements/subscribe"

    async with websockets.connect(uri) as websocket:
        print("Connected to streaming API")

        # Subscribe to measurements
        subscription = {
            "action": "subscribe",
            "sensor_names": ["sensor1", "sensor2"],
            "type_names": ["temperature"],
            "spatial_filter": {
                "type": "bbox",
                "coordinates": [11.0, 46.0, 12.0, 47.0]
            }
        }
        await websocket.send(json.dumps(subscription))

        # Receive messages
        async for message in websocket:
            response = json.loads(message)

            if response["type"] == "ack":
                print(f"Subscription acknowledged: {response['message']}")
            elif response["type"] == "data":
                print(f"New measurement: {response['data']}")
            elif response["type"] == "error":
                print(f"Error: {response['error']}")

# Run the client
asyncio.run(subscribe_to_measurements())
```

### cURL (Testing WebSocket upgrade)

```bash
curl -i -N \
  -H "Connection: Upgrade" \
  -H "Upgrade: websocket" \
  -H "Host: localhost:8080" \
  -H "Origin: http://localhost:8080" \
  http://localhost:8080/api/v1/measurements/subscribe
```

## How It Works

### 1. PostgreSQL Logical Replication

PostgreSQL is configured with `wal_level=logical` to enable logical replication. A publication is created that includes all tables in the `intimev3` schema:

```sql
CREATE PUBLICATION timeseries_publication FOR ALL TABLES IN SCHEMA intimev3;
```

### 2. Materialize Source

Materialize subscribes to the PostgreSQL publication and creates local copies of all tables:

```sql
CREATE SOURCE pg_source
  FROM POSTGRES
  CONNECTION pg_connection (PUBLICATION 'timeseries_publication')
  FOR TABLES (
    intimev3.sensors AS sensors,
    intimev3.types AS types,
    intimev3.timeseries AS timeseries,
    intimev3.measurements_numeric AS measurements_numeric,
    -- ... other measurement tables
  );
```

### 3. Materialized Views

Materialize maintains real-time views of the latest measurements using window functions:

```sql
CREATE MATERIALIZED VIEW latest_measurements_numeric AS
SELECT DISTINCT ON (m.timeseries_id)
    m.timeseries_id,
    m.timestamp,
    m.value,
    -- ... other fields
FROM measurements_numeric m
JOIN timeseries ts ON m.timeseries_id = ts.id
JOIN sensors s ON ts.sensor_id = s.id
JOIN types t ON ts.type_id = t.id
WHERE s.is_active = true
ORDER BY m.timeseries_id, m.timestamp DESC;
```

### 4. TAIL Subscriptions

The Go service uses Materialize's `SUBSCRIBE` feature to receive incremental updates:

```sql
SUBSCRIBE TO (
  SELECT * FROM latest_measurements_all
  WHERE sensor_name IN ('sensor1', 'sensor2')
);
```

Materialize returns a stream of updates with:
- **timestamp**: Materialize's internal timestamp
- **diff**: 1 for inserts, -1 for deletes
- **data**: The row data

### 5. Geospatial Filtering

The Go service applies additional geospatial filtering in-memory for geo-type measurements:

- **Bounding box**: Checks if point falls within min/max lat/lon
- **Radius**: Uses Haversine formula to calculate distance from center point

### 6. WebSocket Push

Filtered updates are pushed to connected WebSocket clients in real-time.

## Data Replication Flow

When a new measurement is inserted into PostgreSQL:

1. **PostgreSQL** → Writes to WAL (Write-Ahead Log)
2. **WAL** → Logical replication slot streams change
3. **Materialize** → Receives change and updates source tables
4. **Materialized View** → Incrementally updates based on new data
5. **TAIL Subscription** → Emits update to Go service
6. **Go Service** → Applies filters and forwards to WebSocket clients
7. **WebSocket Clients** → Receive real-time update

## Performance Considerations

### Materialize

- Views are incrementally maintained - only affected rows are recomputed
- Memory usage scales with the number of distinct timeseries
- For very large deployments, consider using temporal filters

### WebSocket Connections

- Each subscription creates a goroutine for handling updates
- Memory usage: ~4KB per connection + buffered channels
- Recommended limit: 10,000 concurrent connections per instance

### Geospatial Filtering

- Applied in-memory in Go for maximum flexibility
- For very high throughput, consider moving filters to Materialize views
- Currently uses simple Haversine formula for radius filtering

## Monitoring

### Check Materialize Status

```sql
-- Connect to Materialize
psql -h localhost -p 6875 -U materialize -d materialize

-- Check source status
SELECT name, status FROM mz_sources WHERE name = 'pg_source';

-- Check view refresh status
SELECT name, status FROM mz_materialized_views;

-- Check current data count
SELECT COUNT(*) FROM latest_measurements_all;
```

### Check WebSocket Connections

The Go service logs connection events:

```
INFO: New WebSocket connection established
INFO: Subscription created successfully
INFO: WebSocket connection closed
```

## Troubleshooting

### Materialize Not Starting

Check Docker logs:
```bash
docker-compose logs materialize
```

Common issues:
- PostgreSQL not ready yet
- Network connectivity issues

### PostgreSQL Publication Not Created

Ensure user has replication permissions:
```sql
ALTER USER bdp WITH REPLICATION;
```

### No Data in Materialize Views

Check if source is running:
```sql
SELECT * FROM mz_sources WHERE name = 'pg_source';
```

Check source errors:
```sql
SELECT * FROM mz_source_statuses WHERE name = 'pg_source';
```

### WebSocket Connection Drops

- Check network stability
- Verify server is not overloaded
- Check client timeout settings
- Review Go service logs for errors

## Limitations

1. **No PostGIS in Materialize**: Geospatial operations are performed in Go
2. **Memory Requirements**: All latest measurements kept in memory in Materialize
3. **Single Materialize Instance**: No built-in HA for Materialize (consider using Materialize Cloud)
4. **No Historical Streaming**: Only latest measurements are streamed

## Future Enhancements

- [ ] Add authentication/authorization for WebSocket connections
- [ ] Support for historical data streaming with time ranges
- [ ] Advanced geospatial operations (polygon containment, etc.)
- [ ] Rate limiting per connection
- [ ] Metrics and monitoring dashboard
- [ ] Horizontal scaling with load balancer
- [ ] Snapshot isolation for initial data load

## References

- [Materialize Documentation](https://materialize.com/docs/)
- [PostgreSQL Logical Replication](https://www.postgresql.org/docs/current/logical-replication.html)
- [WebSocket Protocol](https://datatracker.ietf.org/doc/html/rfc6455)
- [Gorilla WebSocket](https://github.com/gorilla/websocket)
