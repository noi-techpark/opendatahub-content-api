# Real-Time Streaming Subscriptions

**Last Updated**: 2025-10-21

This document describes the real-time streaming subscription system for the Timeseries API, featuring WebSocket subscriptions with GraphQL-style protocol.

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Setup Instructions](#setup-instructions)
4. [WebSocket Protocol](#websocket-protocol)
5. [Subscription Modes](#subscription-modes)
6. [Client Examples](#client-examples)
7. [How It Works](#how-it-works)
8. [Performance & Monitoring](#performance--monitoring)
9. [Migration Guide](#migration-guide)

---

## Overview

The streaming system allows clients to subscribe to measurement updates in real-time using WebSockets with a GraphQL-inspired protocol.

### Key Features

- ✅ **GraphQL-style Protocol** - Simple `connection_init` → `connection_ack` → `data` flow
- ✅ **Two Subscription Modes** - Simple (by sensor names) and Advanced (discovery-based)
- ✅ **Real-time Updates** - Sub-second latency via Materialize streaming
- ✅ **Geospatial Filtering** - Bounding box and radius filters
- ✅ **Value Filtering** - Subscribe only to sensors matching measurement criteria
- ✅ **Discovery Integration** - Use DiscoverSensors-style filters

### Technology Stack

- **PostgreSQL**: Primary database with logical replication
- **Materialize**: Streaming database maintaining real-time views
- **WebSocket**: Real-time bidirectional communication
- **Go Service**: WebSocket management and filtering

---

## Architecture

```
┌─────────────┐
│   Client    │
│ (WebSocket) │
└──────┬──────┘
       │
       │ 1. Connect
       │ 2. connection_init
       │ 3. connection_ack
       │ 4. data (stream)
       │
┌──────▼──────────────────────────────────┐
│  Go Service (Port 8080)                  │
│  ┌────────────────────────────────────┐ │
│  │ WebSocket Manager                  │ │
│  │ - Connection handling              │ │
│  │ - Geospatial filtering             │ │
│  │ - Discovery integration            │ │
│  └────────┬───────────────────────────┘ │
│           │                              │
│  ┌────────▼───────────────────────────┐ │
│  │ Materialize Client                 │ │
│  │ - SUBSCRIBE (TAIL) to views        │ │
│  │ - Incremental update processing    │ │
│  └────────┬───────────────────────────┘ │
└───────────┼──────────────────────────────┘
            │
            │ SUBSCRIBE
            │
┌───────────▼──────────────────────────────┐
│  Materialize (Port 6875)                  │
│  ┌────────────────────────────────────┐  │
│  │ Materialized Views (Incrementally  │  │
│  │ maintained)                        │  │
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
│  │ (Logical Replication)              │  │
│  └────────▲───────────────────────────┘  │
└───────────┼───────────────────────────────┘
            │
            │ Logical Replication
            │
┌───────────▼───────────────────────────────┐
│  PostgreSQL (Port 5556)                    │
│  - sensors, types, timeseries             │
│  - measurements_* (partitioned)           │
└────────────────────────────────────────────┘
```

---

## Setup Instructions

### 1. Start Docker Services

```bash
cd OdhTimeseries
docker-compose up -d
```

This starts:
- **PostgreSQL** on port 5556 (with `wal_level=logical`)
- **Materialize** on port 6875

### 2. Initialize Database Schema

```bash
PGPASSWORD=password psql -h localhost -p 5556 -U bdp -d timeseries -f sql-scripts/init-new.sql
```

### 3. Setup Streaming Infrastructure

```bash
./setup-streaming.sh
```

This script:
1. Creates PostgreSQL publication for logical replication
2. Waits for Materialize to be ready
3. Creates Materialize source from PostgreSQL
4. Creates materialized views for latest measurements

### 4. Start the Go Service

```bash
go run cmd/server/main.go
```

The service will:
- Connect to PostgreSQL and Materialize
- Wait for initial sync to complete
- Start HTTP server on port 8080

---

## WebSocket Protocol

### Endpoints

| Endpoint | Mode | Description |
|----------|------|-------------|
| `/api/v1/measurements/subscribe` | Simple | Subscribe by sensor names |
| `/api/v1/measurements/subscribe/advanced` | Advanced | Subscribe using discovery filters |

### Protocol Flow

1. **Client** → Connects to WebSocket endpoint (HTTP GET upgrade)
2. **Client** → Sends `connection_init` with subscription configuration
3. **Server** → Validates configuration
4. **Server** → Responds with `connection_ack` (success) or `error` (failure + close)
5. **Server** → Streams `data` messages with measurement updates
6. **Client** → Closes connection to unsubscribe (no explicit unsubscribe needed)

### Message Types

#### Client → Server: `connection_init`

Sent **immediately** after WebSocket connection is established.

**Simple Mode:**
```json
{
  "type": "connection_init",
  "payload": {
    "sensor_names": ["sensor1", "sensor2"],
    "type_names": ["temperature", "humidity"]
  }
}
```

**Advanced Mode:**
```json
{
  "type": "connection_init",
  "payload": {
    "timeseries_filter": {
      "required_types": ["temperature"],
      "optional_types": ["humidity"],
      "dataset_ids": ["weather_stations"]
    },
    "measurement_filter": {
      "expression": "temperature.gteq.20",
      "latest_only": true
    },
    "spatial_filter": {
      "type": "bbox",
      "coordinates": [11.0, 46.0, 12.0, 47.0]
    },
    "limit": 100
  }
}
```

#### Server → Client: `connection_ack`

Confirms subscription is established.

```json
{
  "type": "connection_ack",
  "payload": {
    "mode": "simple",
    "sensor_count": 2
  }
}
```

#### Server → Client: `data`

Measurement update (streamed continuously).

```json
{
  "type": "data",
  "payload": {
    "timeseries_id": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2024-01-01T12:00:00Z",
    "value": "23.5",
    "sensor_name": "sensor1",
    "type_name": "temperature",
    "data_type": "numeric",
    "unit": "°C",
    "sensor_id": 123,
    "type_id": 456,
    "created_on": "2024-01-01T12:00:01Z",
    "diff": 1
  }
}
```

#### Server → Client: `error`

Error message (connection will be closed after sending).

```json
{
  "type": "error",
  "payload": {
    "message": "Invalid configuration: sensor_names is required"
  }
}
```

---

## Subscription Modes

### Simple Mode

Subscribe to specific sensors by name.

**Required:** `sensor_names`

**Optional:** `type_names`

**Example:**
```json
{
  "type": "connection_init",
  "payload": {
    "sensor_names": ["AIRQ01", "AIRQ02"],
    "type_names": ["temperature", "pm25"]
  }
}
```

### Advanced Mode (Discovery-Based)

Subscribe using discovery filters to automatically find sensors matching complex criteria.

#### Timeseries Filter

| Field | Type | Description |
|-------|------|-------------|
| `required_types` | string[] | Sensors must have ALL of these measurement types |
| `optional_types` | string[] | Sensors may have ANY of these measurement types |
| `dataset_ids` | string[] | Filter by dataset membership |

#### Measurement Filter

| Field | Type | Description |
|-------|------|-------------|
| `expression` | string | Value filter expression (e.g., `temperature.gteq.20`) |
| `latest_only` | boolean | Only consider latest measurements |
| `time_range` | object | Time constraints for measurements |

#### Spatial Filter

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

#### Filter Expression Syntax

**Format:** `<type_name>.<operator>.<value>`

**Operators:**
- `eq` - Equal to
- `neq` - Not equal to
- `gt` - Greater than
- `gte`, `gteq` - Greater than or equal to
- `lt` - Less than
- `lte`, `lteq` - Less than or equal to

**Complex Expressions:**
- AND: `and(temperature.gt.20, humidity.lt.80)`
- OR: `or(temperature.gt.30, pm25.gt.90)`

#### Advanced Examples

**1. High temperature weather stations:**
```json
{
  "type": "connection_init",
  "payload": {
    "timeseries_filter": {
      "required_types": ["temperature"],
      "dataset_ids": ["weather_stations"]
    },
    "measurement_filter": {
      "latest_only": true,
      "expression": "temperature.gteq.30"
    }
  }
}
```

**2. Parking sensors in area:**
```json
{
  "type": "connection_init",
  "payload": {
    "timeseries_filter": {
      "required_types": ["occupancy", "location"]
    },
    "spatial_filter": {
      "type": "bbox",
      "coordinates": [11.3, 46.4, 11.4, 46.5]
    }
  }
}
```

**3. Power generation sensors:**
```json
{
  "type": "connection_init",
  "payload": {
    "timeseries_filter": {
      "required_types": ["power_generation"]
    },
    "limit": 50
  }
}
```

---

## Client Examples

### Python

```python
import asyncio
import websockets
import json

async def subscribe_to_measurements():
    uri = "ws://localhost:8080/api/v1/measurements/subscribe"

    async with websockets.connect(uri) as websocket:
        # Send connection_init immediately
        init_msg = {
            "type": "connection_init",
            "payload": {
                "sensor_names": ["sensor1", "sensor2"],
                "type_names": ["temperature"]
            }
        }
        await websocket.send(json.dumps(init_msg))

        # Wait for connection_ack
        ack = await websocket.recv()
        ack_data = json.loads(ack)

        if ack_data['type'] == 'connection_ack':
            print(f"Subscription established! Mode: {ack_data['payload']['mode']}")

            # Receive data updates
            async for message in websocket:
                data = json.loads(message)
                if data['type'] == 'data':
                    payload = data['payload']
                    print(f"{payload['sensor_name']}: {payload['value']} {payload.get('unit', '')}")
        elif ack_data['type'] == 'error':
            print(f"Error: {ack_data['payload']['message']}")

asyncio.run(subscribe_to_measurements())
```

### JavaScript (Node.js)

```javascript
const WebSocket = require('ws');

const ws = new WebSocket('ws://localhost:8080/api/v1/measurements/subscribe');

ws.on('open', () => {
    // Send connection_init immediately
    const initMsg = {
        type: 'connection_init',
        payload: {
            sensor_names: ['sensor1', 'sensor2'],
            type_names: ['temperature']
        }
    };
    ws.send(JSON.stringify(initMsg));
});

ws.on('message', (data) => {
    const msg = JSON.parse(data);

    if (msg.type === 'connection_ack') {
        console.log('Subscription established! Mode:', msg.payload.mode);
    } else if (msg.type === 'data') {
        const payload = msg.payload;
        console.log(`${payload.sensor_name}: ${payload.value}`);
    } else if (msg.type === 'error') {
        console.error('Error:', msg.payload.message);
        ws.close();
    }
});

ws.on('close', () => {
    console.log('Connection closed');
});
```

### JavaScript (Browser)

```javascript
const ws = new WebSocket('ws://localhost:8080/api/v1/measurements/subscribe');

ws.onopen = () => {
    const initMsg = {
        type: 'connection_init',
        payload: {
            sensor_names: ['sensor1', 'sensor2']
        }
    };
    ws.send(JSON.stringify(initMsg));
};

ws.onmessage = (event) => {
    const msg = JSON.parse(event.data);

    if (msg.type === 'connection_ack') {
        console.log('Subscription established!');
    } else if (msg.type === 'data') {
        const payload = msg.payload;
        console.log(`${payload.sensor_name}: ${payload.value}`);
    } else if (msg.type === 'error') {
        console.error('Error:', msg.payload.message);
    }
};

ws.onclose = () => {
    console.log('Disconnected');
};
```

---

## How It Works

### 1. PostgreSQL Logical Replication

PostgreSQL is configured with `wal_level=logical`:

```sql
-- docker-compose.yml sets these automatically
wal_level = logical
max_wal_senders = 10
max_replication_slots = 10
```

Publication for all tables in `intimev3` schema:

```sql
CREATE PUBLICATION timeseries_publication
FOR ALL TABLES IN SCHEMA intimev3;
```

### 2. Materialize Source

Materialize subscribes to PostgreSQL publication:

```sql
CREATE SOURCE pg_source
  FROM POSTGRES
  CONNECTION pg_connection (PUBLICATION 'timeseries_publication')
  FOR TABLES (
    intimev3.sensors AS sensors,
    intimev3.types AS types,
    intimev3.timeseries AS timeseries,
    intimev3.measurements_numeric AS measurements_numeric,
    intimev3.measurements_string AS measurements_string,
    intimev3.measurements_json AS measurements_json,
    intimev3.measurements_geoposition AS measurements_geoposition,
    intimev3.measurements_geoshape AS measurements_geoshape,
    intimev3.measurements_boolean AS measurements_boolean
  );
```

### 3. Materialized Views

Materialize maintains real-time views of latest measurements:

```sql
CREATE MATERIALIZED VIEW latest_measurements_numeric AS
SELECT DISTINCT ON (m.timeseries_id)
    m.timeseries_id,
    m.timestamp,
    m.value,
    s.name AS sensor_name,
    t.name AS type_name,
    -- ... other fields
FROM measurements_numeric m
JOIN timeseries ts ON m.timeseries_id = ts.id
JOIN sensors s ON ts.sensor_id = s.id
JOIN types t ON ts.type_id = t.id
WHERE s.is_active = true
ORDER BY m.timeseries_id, m.timestamp DESC;
```

### 4. SUBSCRIBE (TAIL)

Go service subscribes to Materialize views:

```sql
SUBSCRIBE TO (
  SELECT * FROM latest_measurements_all
  WHERE sensor_name IN ('sensor1', 'sensor2')
);
```

Materialize streams:
- **timestamp**: Internal timestamp
- **diff**: `1` for inserts, `-1` for deletes
- **data**: Row data

### 5. Data Flow

```
PostgreSQL INSERT
  ↓
WAL (Write-Ahead Log)
  ↓
Logical Replication
  ↓
Materialize Source Tables
  ↓
Materialized Views (incremental update)
  ↓
SUBSCRIBE (streaming changes)
  ↓
Go Service (filtering)
  ↓
WebSocket Clients (real-time)
```

---

## Performance & Monitoring

### Performance Characteristics

**Materialize:**
- Views incrementally maintained (only affected rows recomputed)
- Memory usage scales with distinct timeseries count
- Sub-second update latency

**WebSocket:**
- ~4KB per connection + channel buffers
- Recommended limit: 10,000 concurrent connections per instance
- Each subscription runs in separate goroutine

**Geospatial Filtering:**
- Applied in-memory in Go
- Uses Haversine formula for radius calculations
- For high throughput, consider moving to Materialize views

### Monitoring

**Check Materialize Status:**
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

**Go Service Logs:**
```
INFO: New WebSocket connection established
INFO: Subscription created successfully (mode: simple, sensors: 2)
INFO: WebSocket connection closed
```

### Troubleshooting

**Materialize Not Starting:**
```bash
docker-compose logs materialize
```

**No Data in Views:**
```sql
-- Check if source is running
SELECT * FROM mz_sources WHERE name = 'pg_source';

-- Check for errors
SELECT * FROM mz_source_statuses WHERE name = 'pg_source';
```

**WebSocket Drops:**
- Check network stability
- Verify server not overloaded
- Check client timeout settings
- Review Go service logs

---

## Migration Guide

### From Old Protocol (action/subscribe)

**Old Protocol (Deprecated):**
```python
await ws.send(json.dumps({
    "action": "subscribe",
    "sensor_names": ["sensor1"]
}))
```

**New Protocol (Current):**
```python
await ws.send(json.dumps({
    "type": "connection_init",
    "payload": {
        "sensor_names": ["sensor1"]
    }
}))
```

### Key Changes

| Aspect | Old | New |
|--------|-----|-----|
| Initial message | `{action: "subscribe", ...}` | `{type: "connection_init", payload: {...}}` |
| Ack message | `{type: "ack", ...}` | `{type: "connection_ack", payload: {...}}` |
| Data message | `{type: "data", data: {...}}` | `{type: "data", payload: {...}}` |
| Unsubscribe | `{action: "unsubscribe"}` | Close WebSocket connection |
| Error handling | `{type: "error", error: "..."}` | `{type: "error", payload: {message: "..."}}` + close |

### Protocol Inspiration

This protocol is inspired by [graphql-ws](https://github.com/enisdenjo/graphql-ws), providing:

✅ **Simpler Flow** - No multi-step handshake
✅ **Better Error Handling** - Errors close connection
✅ **Clear Separation** - Simple vs Advanced endpoints
✅ **Familiar Pattern** - GraphQL-like for developers
✅ **Stateless After Init** - Configuration finalized at connection

---

## Limitations

1. **No PostGIS in Materialize**: Geospatial operations performed in Go
2. **Memory Requirements**: All latest measurements kept in Materialize memory
3. **Single Instance**: No built-in HA (use Materialize Cloud for production)
4. **No Historical Streaming**: Only latest measurements are streamed

---

## Future Enhancements

- [ ] Authentication/authorization for WebSocket connections
- [ ] Historical data streaming with time ranges
- [ ] Advanced geospatial operations (polygon containment, etc.)
- [ ] Rate limiting per connection
- [ ] Metrics and monitoring dashboard
- [ ] Horizontal scaling with load balancer
- [ ] Snapshot isolation for initial data load

---

## References

- [Materialize Documentation](https://materialize.com/docs/)
- [PostgreSQL Logical Replication](https://www.postgresql.org/docs/current/logical-replication.html)
- [WebSocket Protocol (RFC 6455)](https://datatracker.ietf.org/doc/html/rfc6455)
- [GraphQL-WS Protocol](https://github.com/enisdenjo/graphql-ws)
- [Gorilla WebSocket](https://github.com/gorilla/websocket)
