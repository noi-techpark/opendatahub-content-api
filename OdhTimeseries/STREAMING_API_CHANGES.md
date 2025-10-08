# Streaming API - GraphQL-style WebSocket Subscriptions

## Summary

The WebSocket subscription API uses a GraphQL-style `connection_init` protocol for a simplified, intuitive subscription flow.

## Protocol Flow

### GraphQL-style Connection Flow

1. **Client connects** to WebSocket endpoint (GET)
2. **Client sends** `connection_init` message immediately with subscription configuration
3. **Server validates** configuration
4. **Server responds** with `connection_ack` (success) or `error` (failure + closes connection)
5. **Server streams** `data` messages with measurement updates
6. **Client closes** connection to unsubscribe (no explicit unsubscribe message needed)

## Endpoints

### Simple Subscription
**GET** `/api/v1/measurements/subscribe`

Subscribe to specific sensors by name. Enforces simple mode (requires `sensor_names` in payload).

### Advanced Subscription
**GET** `/api/v1/measurements/subscribe/advanced`

Subscribe using discovery filters and measurement expressions. Enforces advanced mode (requires `timeseries_filter` or `measurement_filter` in payload).

## Message Protocol

### Client → Server: connection_init

Sent immediately after WebSocket connection is established.

**Simple Mode Example:**
```json
{
  "type": "connection_init",
  "payload": {
    "sensor_names": ["sensor1", "sensor2"],
    "type_names": ["temperature", "humidity"]
  }
}
```

**Advanced Mode Example:**
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
    "limit": 100
  }
}
```

### Server → Client: connection_ack

Sent when subscription is successfully established.

```json
{
  "type": "connection_ack",
  "payload": {
    "mode": "simple"
  }
}
```

### Server → Client: data

Measurement update message.

```json
{
  "type": "data",
  "payload": {
    "timeseries_id": "uuid",
    "sensor_name": "sensor1",
    "type_name": "temperature",
    "data_type": "numeric",
    "timestamp": "2024-01-01T00:00:00Z",
    "value": "23.5"
  }
}
```

### Server → Client: error

Error message (connection will be closed after sending).

```json
{
  "type": "error",
  "payload": {
    "message": "Invalid configuration: sensor_names is required"
  }
}
```

## Configuration Options

### Simple Mode (sensor_names)

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sensor_names` | string[] | Yes | Array of sensor names to subscribe to |
| `type_names` | string[] | No | Optional filter for specific measurement types |

### Advanced Mode (Discovery)

#### Timeseries Filter

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `required_types` | string[] | No | Sensors must have ALL of these types |
| `optional_types` | string[] | No | Sensors may have ANY of these types |
| `dataset_ids` | string[] | No | Filter by dataset IDs |

#### Measurement Filter

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `expression` | string | No | Value filter expression (e.g., `temperature.gt.20`) |
| `latest_only` | boolean | No | Only stream the latest measurement per sensor/type |

#### General Options

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `limit` | number | No | Maximum number of sensors to subscribe to |

### Filter Expression Syntax

Measurement filter expressions allow filtering by value:

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

**Examples:**
- `temperature.gteq.20` - Temperature >= 20
- `pm25.gt.90` - PM2.5 > 90
- `and(temperature.gt.20, humidity.lt.80)` - Temperature > 20 AND Humidity < 80

## Python Client Example

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
                "sensor_names": ["sensor1", "sensor2"]
            }
        }
        await websocket.send(json.dumps(init_msg))

        # Wait for connection_ack
        ack = await websocket.recv()
        ack_data = json.loads(ack)

        if ack_data['type'] == 'connection_ack':
            print("Subscription established!")

            # Receive data updates
            async for message in websocket:
                data = json.loads(message)
                if data['type'] == 'data':
                    payload = data['payload']
                    print(f"{payload['sensor_name']}: {payload['value']}")
        elif ack_data['type'] == 'error':
            print(f"Error: {ack_data['payload']['message']}")

asyncio.run(subscribe_to_measurements())
```

## JavaScript Client Example (Node.js)

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

## Browser WebSocket Example

```javascript
const ws = new WebSocket('ws://localhost:8080/api/v1/measurements/subscribe');

ws.onopen = () => {
    // Send connection_init immediately
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

## Benefits

1. **Simpler Protocol** - No multi-step handshake, just connect and send `connection_init`
2. **Better Error Handling** - Errors result in `error` message + connection close
3. **Clear Endpoint Separation** - `/subscribe` for simple, `/subscribe/advanced` for discovery
4. **GraphQL-like** - Familiar pattern for developers who know GraphQL subscriptions
5. **Stateless After Init** - Configuration is final at connection time
6. **Standard WebSocket** - Works with all WebSocket clients (no POST required)

## Comparison with GraphQL Subscriptions

This protocol is inspired by [graphql-ws](https://github.com/enisdenjo/graphql-ws):

| Feature | GraphQL-WS | This API |
|---------|------------|----------|
| Protocol | `connection_init` → `connection_ack` → `subscribe` → `next` | `connection_init` → `connection_ack` → `data` |
| Transport | WebSocket (GET) | WebSocket (GET) |
| Configuration | Sent in separate `subscribe` message | Sent in `connection_init` payload |
| Unsubscribe | Send `complete` message | Close WebSocket connection |
| Errors | `error` message | `error` message + close connection |

## Migration from Old Protocol

If you were using the old message-based protocol:

**Old:**
```python
await ws.send(json.dumps({"action": "subscribe", "sensor_names": ["sensor1"]}))
# Wait for ack...
```

**New:**
```python
await ws.send(json.dumps({
    "type": "connection_init",
    "payload": {"sensor_names": ["sensor1"]}
}))
# Wait for connection_ack...
```

Key differences:
- `action` → `type`
- `"subscribe"` → `"connection_init"`
- Configuration is now nested in `payload`
- Ack message has changed from `{type: "ack"}` to `{type: "connection_ack", payload: {mode: "..."}}`
- Data messages now have `payload` field: `{type: "data", payload: {...measurement...}}`
- No more `unsubscribe` action - just close the connection

## Testing

See `test-streaming-client.html` for an interactive web-based test client, or run:

```bash
# Simple test script
python3 test_streaming.py

# Comprehensive test suite
python3 test_streaming_comprehensive.py
```
