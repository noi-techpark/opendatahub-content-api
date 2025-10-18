# TIMESERIES API CONTEXT - LLM INSTRUCTIONS

## OVERVIEW

The Timeseries API manages and serves time-series measurement data from sensors. It provides endpoints for discovering sensors, querying measurements, accessing datasets, and real-time streaming.

**Base API Information:**
- **Server URL**: `localhost:8080` (local development)
- **Base Path**: `/api/v1`
- **Version**: `1.0.0`
- **Content Type**: `application/json`
- **Protocol**: Supports both HTTP and HTTPS

**Core Concepts:**
- **Sensors**: Devices or entities that produce measurements (e.g., temperature sensor, air quality monitor)
- **Types**: Measurement types (e.g., temperature, humidity, PM2.5) with metadata (unit, description, data type)
- **Timeseries**: A specific sensor-type pair that produces measurements over time (identified by timeseries_id)
- **Measurements**: Individual data points with timestamp and value
- **Datasets**: Collections of related timeseries (e.g., "weather_stations", "air_quality")

---

## VALUE FILTER SYNTAX

### FILTER EXPRESSION FORMAT

Filter expressions allow you to query sensors based on their measurement values. The syntax follows this pattern:

```
timeseries.operator.value_or_list
```

For JSON measurements with nested fields:
```
timeseries.nested1.nested2.operator.value_or_list
```

### OPERATORS

| Operator | Meaning | Example |
|----------|---------|---------|
| `eq` | Equal to | `temperature.eq.20` |
| `neq` | Not equal to | `status.neq.offline` |
| `lt` | Less than | `temperature.lt.30` |
| `gt` | Greater than | `humidity.gt.50` |
| `lteq` | Less than or equal | `pm25.lteq.100` |
| `gteq` | Greater than or equal | `co2.gteq.400` |
| `re` | Regular expression match | `sensor_id.re.^TEMP.*` |
| `ire` | Case-insensitive regex | `name.ire.weather` |
| `nre` | Negated regex | `type.nre.test` |
| `nire` | Negated case-insensitive regex | `status.nire.error` |
| `bbi` | Bounding box intersecting | `coordinate.bbi.(11,46,12,47,4326)` |
| `bbc` | Bounding box containing | `coordinate.bbc.(11,46,12,47,4326)` |
| `dlt` | Distance less than (meters) | `coordinate.dlt.(5000,11.2,46.7,4326)` |
| `in` | Value in list | `status.in.(active,pending)` |
| `nin` | Value not in list | `status.nin.(error,failed)` |

### LOGICAL OPERATORS

Combine multiple filter conditions using logical operators:

**AND Conjunction** (all conditions must be true):
```
and(temperature.gt.20, humidity.lt.80)
```

**OR Disjunction** (at least one condition must be true):
```
or(temperature.gt.30, pm25.gt.90)
```

**Nesting** (complex logic):
```
and(temperature.gt.15, or(humidity.lt.30, humidity.gt.80))
```

### VALUE TYPES

- **Single value**: `temperature.eq.23.5`
- **List**: `status.in.(active,pending,running)`
- **Regular expressions**: `sensor_name.re.^TEMP.*` (escape `,'"` with `\`)
- **URL encoding**: Use encoded values for special characters

### GEOGRAPHIC FILTERS

**Bounding Box Intersecting (bbi)**:
```
coordinate.bbi.(left_x,left_y,right_x,right_y,SRID)
```
Example: `coordinate.bbi.(11,46,12,47,4326)` - finds objects partially or fully within box

**Bounding Box Containing (bbc)**:
```
coordinate.bbc.(left_x,left_y,right_x,right_y,SRID)
```
Example: `coordinate.bbc.(11,46,12,47,4326)` - finds objects completely within box

**Distance Filter (dlt)**:
```
coordinate.dlt.(distance_meters,point_x,point_y,SRID)
```
Example: `coordinate.dlt.(5000,11.2,46.7,4326)` - finds objects within 5km of point

SRID is optional (default: 4326 for WGS84 coordinates)

### FILTER EXAMPLES

```javascript
// Single condition
"temperature.gt.20"

// AND condition
"and(temperature.gt.20, humidity.lt.80)"

// OR condition
"or(temperature.gt.30, pm25.gt.90)"

// Complex nested logic
"and(temperature.gteq.20, temperature.lteq.30, or(o2.eq.2, humidity.gt.70))"

// Geographic filter
"and(temperature.gt.15, coordinate.dlt.(10000,11.35,46.5,4326))"

// List membership
"status.in.(active,online,running)"

// Regular expression
"sensor_name.re.^WEATHER_.*_2024$"
```

---

## API ENDPOINTS

### 1. SYSTEM ENDPOINTS

#### 1.1 Health Check
**Endpoint**: `GET /api/v1/health`

**Purpose**: Check if the API is running and healthy

**Parameters**: None

**Response** (200):
```json
{
  "status": "healthy"
}
```

---

### 3. TYPES ENDPOINTS

#### 3.1 List All Types
**Endpoint**: `GET /api/v1/types`

**Purpose**: Get a paginated list of all measurement types, optionally including sensors

**Query Parameters**:
- `offset` (integer, optional, default: 0): Offset for pagination
- `limit` (integer, optional, default: 50): Limit for pagination
- `include_sensors` (boolean, optional): Include sensors with timeseries for each type

**Response** (200):
```json
{
  "types": [
    {
      "type": {
        "id": integer,
        "name": "string",
        "description": "string",
        "unit": "string",
        "data_type": "numeric|string|json|geoposition|geoshape|boolean",
        "metadata": "string (JSON)"
      },
      "sensors": [  // Only if include_sensors=true
        {
          "sensor_name": "string",
          "timeseries_id": "string (UUID)"
        }
      ]
    }
  ],
  "total": integer,
  "offset": integer,
  "limit": integer
}
```

**Example Usage**:
```
GET /api/v1/types?offset=0&limit=50&include_sensors=true
```

#### 3.2 Get Type by Name
**Endpoint**: `GET /api/v1/types/{name}`

**Purpose**: Get a specific measurement type by name with all sensors that have this type

**Path Parameters**:
- `name` (string, required): Type name (e.g., "temperature", "humidity")

**Response** (200):
```json
{
  "type": {
    "id": integer,
    "name": "string",
    "description": "string",
    "unit": "string",
    "data_type": "numeric|string|json|geoposition|geoshape|boolean",
    "metadata": "string (JSON)"
  },
  "sensors": [
    {
      "sensor_name": "string",
      "timeseries_id": "string (UUID)"
    }
  ]
}
```

**Example Usage**:
```
GET /api/v1/types/temperature
```

---

### 4. SENSORS ENDPOINTS

#### 4.1 Discover Sensors (POST - Recommended)
**Endpoint**: `POST /api/v1/sensors`

**Purpose**: Find sensors that satisfy conditions over their timeseries and measurements

**Request Body**:
```json
{
  "timeseries_filter": {
    "required_types": ["string"],      // Sensors MUST have ALL these types
    "optional_types": ["string"],      // Sensors MAY have ANY of these types
    "dataset_ids": ["string"]          // Filter by dataset membership
  },
  "measurement_filter": {
    "expression": "string",            // Filter expression (see VALUE FILTER SYNTAX)
    "latest_only": boolean,            // Only consider latest measurements
    "time_range": {
      "start_time": "string (RFC3339)",
      "end_time": "string (RFC3339)"
    }
  },
  "limit": integer                     // Maximum number of results
}
```

**Response** (200):
```json
{
  "sensors": [
    {
      "sensor_name": "string",
      "sensor_id": integer,
      "timeseries": [
        {
          "timeseries_id": "string (UUID)",
          "type_name": "string",
          "type_info": {
            "id": integer,
            "name": "string",
            "description": "string",
            "unit": "string",
            "data_type": "string",
            "metadata": "string (JSON)"
          }
        }
      ]
    }
  ],
  "total": integer
}
```

**Example Usage**:
```json
POST /api/v1/sensors
{
  "timeseries_filter": {
    "required_types": ["temperature", "humidity"],
    "dataset_ids": ["weather_stations"]
  },
  "measurement_filter": {
    "expression": "and(temperature.gt.20, humidity.lt.80)",
    "latest_only": true
  },
  "limit": 100
}
```

#### 4.2 Discover Sensors (GET - Legacy)
**Endpoint**: `GET /api/v1/sensors/discover`

**Purpose**: Find sensors using query parameters (legacy, use POST endpoint for complex filters)

**Query Parameters**:
- `type_names` (string, optional): Required types (comma-separated)
- `optional_types` (string, optional): Optional types (comma-separated)
- `dataset_ids` (string, optional): Dataset IDs (comma-separated)
- `value_filter` (string, optional): Value filter expression (type.operator.value)
- `start_time` (string, optional): Start time (RFC3339)
- `end_time` (string, optional): End time (RFC3339)
- `latest_only` (boolean, optional): Only consider latest measurements
- `limit` (integer, optional): Maximum results

**Response** (200): Same as POST /api/v1/sensors

**Example Usage**:
```
GET /api/v1/sensors/discover?type_names=temperature,humidity&value_filter=temperature.gt.20&latest_only=true&limit=50
```

#### 4.3 Get Sensor Timeseries
**Endpoint**: `GET /api/v1/sensors/{name}`

**Purpose**: Get all timeseries for a specific sensor, optionally filtered by type names

**Path Parameters**:
- `name` (string, required): Sensor name

**Query Parameters**:
- `type_names` (string, optional): Filter by type names (comma-separated)

**Response** (200):
```json
{
  "sensor_id": integer,
  "sensor_name": "string",
  "total": integer,
  "timeseries": [
    {
      "timeseries_id": "string (UUID)",
      "type_name": "string",
      "type_info": {
        "id": integer,
        "name": "string",
        "description": "string",
        "unit": "string",
        "data_type": "string",
        "metadata": "string (JSON)"
      }
    }
  ]
}
```

**Example Usage**:
```
GET /api/v1/sensors/weather_station_01
GET /api/v1/sensors/weather_station_01?type_names=temperature,humidity
```

#### 4.4 Get Timeseries for Multiple Sensors (Batch)
**Endpoint**: `POST /api/v1/sensors/timeseries`

**Purpose**: Get timeseries for multiple sensors in a single request

**Request Body**:
```json
{
  "sensor_names": ["string"],  // Required
  "type_names": ["string"]     // Optional: filter by types
}
```

**Response** (200):
```json
{
  "sensors": [
    {
      "sensor_id": integer,
      "sensor_name": "string",
      "total": integer,
      "timeseries": [
        {
          "timeseries_id": "string (UUID)",
          "type_name": "string",
          "type_info": { /* Type object */ }
        }
      ]
    }
  ],
  "total": integer
}
```

**Example Usage**:
```json
POST /api/v1/sensors/timeseries
{
  "sensor_names": ["sensor1", "sensor2", "sensor3"],
  "type_names": ["temperature", "humidity"]
}
```

#### 4.5 Get Types for Multiple Sensors
**Endpoint**: `POST /api/v1/sensors/types`

**Purpose**: Get all measurement types for multiple sensors with their timeseries IDs

**Request Body**:
```json
{
  "sensor_names": ["string"],  // Required
  "distinct": boolean          // Optional: get unique types across all sensors
}
```

**Response** (200):
```json
{
  "sensors": [
    {
      "sensor_id": integer,
      "sensor_name": "string",
      "total": integer,
      "types": [
        {
          "type_info": { /* Type object */ },
          "timeseries_id": "string (UUID)",
          "sensor_name": "string"
        }
      ]
    }
  ],
  "total": integer,
  "types": [  // Only if distinct=true
    {
      "type_info": { /* Type object */ },
      "timeseries_id": "string (UUID)",
      "sensor_name": "string"
    }
  ]
}
```

**Example Usage**:
```json
POST /api/v1/sensors/types
{
  "sensor_names": ["sensor1", "sensor2"],
  "distinct": true
}
```

#### 4.6 Verify Sensors Against Filters
**Endpoint**: `POST /api/v1/sensors/verify`

**Purpose**: Verify if a list of sensor names satisfy the same filters used in sensor discovery

**Request Body**:
```json
{
  "sensor_names": ["string"],  // Sensors to verify
  "timeseries_filter": {
    "required_types": ["string"],
    "optional_types": ["string"],
    "dataset_ids": ["string"]
  },
  "measurement_filter": {
    "expression": "string",
    "latest_only": boolean,
    "time_range": {
      "start_time": "string (RFC3339)",
      "end_time": "string (RFC3339)"
    }
  }
}
```

**Response** (200):
```json
{
  "ok": boolean,              // True if all sensors satisfy filters
  "verified": ["string"],     // Sensors that satisfy filters
  "unverified": ["string"],   // Sensors that don't satisfy filters
  "request": { /* Original request */ }
}
```

**Example Usage**:
```json
POST /api/v1/sensors/verify
{
  "sensor_names": ["sensor1", "sensor2", "sensor3"],
  "timeseries_filter": {
    "required_types": ["temperature"]
  },
  "measurement_filter": {
    "expression": "temperature.gt.20",
    "latest_only": true
  }
}
```

---

### 5. MEASUREMENTS ENDPOINTS

#### 5.1 Get Latest Measurements (GET)
**Endpoint**: `GET /api/v1/measurements/latest`

**Purpose**: Retrieve the latest measurements for specified sensors and types using query parameters

**Query Parameters**:
- `sensor_names` (string, required): Comma-separated list of sensor names
- `type_names` (string, optional): Comma-separated list of measurement type names

**Response** (200):
```json
{
  "measurements": [
    {
      "timeseries_id": "string (UUID)",
      "sensor_name": "string",
      "type_name": "string",
      "timestamp": "string (RFC3339)",
      "value": "string|number|object"  // Type depends on data_type
    }
  ]
}
```

**Example Usage**:
```
GET /api/v1/measurements/latest?sensor_names=sensor1,sensor2&type_names=temperature,humidity
```

#### 5.2 Get Latest Measurements (POST)
**Endpoint**: `POST /api/v1/measurements/latest`

**Purpose**: Retrieve the latest measurements using JSON request body (preferred for many sensors)

**Request Body**:
```json
{
  "sensor_names": ["string"],  // Required
  "type_names": ["string"]     // Optional
}
```

**Response** (200): Same as GET endpoint

**Example Usage**:
```json
POST /api/v1/measurements/latest
{
  "sensor_names": ["sensor1", "sensor2", "sensor3"],
  "type_names": ["temperature", "humidity"]
}
```

#### 5.3 Get Historical Measurements (GET)
**Endpoint**: `GET /api/v1/measurements/historical`

**Purpose**: Retrieve historical measurements with time range filtering using query parameters

**Query Parameters**:
- `sensor_names` (string, required): Comma-separated list of sensor names
- `type_names` (string, optional): Comma-separated list of measurement type names
- `start_time` (string, optional): Start time in RFC3339 format
- `end_time` (string, optional): End time in RFC3339 format
- `limit` (integer, optional): Maximum number of results

**Response** (200):
```json
{
  "measurements": [
    {
      "timeseries_id": "string (UUID)",
      "sensor_name": "string",
      "type_name": "string",
      "timestamp": "string (RFC3339)",
      "value": "string|number|object"
    }
  ]
}
```

**Example Usage**:
```
GET /api/v1/measurements/historical?sensor_names=sensor1&type_names=temperature&start_time=2024-01-01T00:00:00Z&end_time=2024-01-31T23:59:59Z&limit=1000
```

#### 5.4 Get Historical Measurements (POST)
**Endpoint**: `POST /api/v1/measurements/historical`

**Purpose**: Retrieve historical measurements using JSON request body (preferred for complex queries)

**Request Body**:
```json
{
  "sensor_names": ["string"],         // Required
  "type_names": ["string"],           // Optional
  "start_time": "string (RFC3339)",   // Optional
  "end_time": "string (RFC3339)",     // Optional
  "limit": integer                    // Optional
}
```

**Response** (200): Same as GET endpoint

**Example Usage**:
```json
POST /api/v1/measurements/historical
{
  "sensor_names": ["sensor1", "sensor2"],
  "type_names": ["temperature", "humidity"],
  "start_time": "2024-01-01T00:00:00Z",
  "end_time": "2024-01-31T23:59:59Z",
  "limit": 10000
}
```

---

## DATA TYPES

The Timeseries API supports the following data types for measurements:

| Data Type | Description | Value Format |
|-----------|-------------|--------------|
| `numeric` | Numeric values (integer or float) | `23.5`, `100`, `-15.2` |
| `string` | Text values | `"active"`, `"offline"` |
| `json` | JSON objects or arrays | `{"temp": 23.5, "unit": "C"}` |
| `geoposition` | Geographic coordinates | `{"lat": 46.5, "lon": 11.35}` |
| `geoshape` | Geographic shapes (polygons, etc.) | GeoJSON format |
| `boolean` | True/false values | `true`, `false` |

---

## COMMON PATTERNS AND BEST PRACTICES

### Pattern 1: Discover Sensors by Type and Value
```json
POST /api/v1/sensors
{
  "timeseries_filter": {
    "required_types": ["temperature", "humidity"]
  },
  "measurement_filter": {
    "expression": "and(temperature.gteq.20, temperature.lteq.30)",
    "latest_only": true
  },
  "limit": 50
}
```

### Pattern 2: Get Latest Measurements for Discovered Sensors
```json
// Step 1: Discover sensors
POST /api/v1/sensors
{ "timeseries_filter": { "required_types": ["temperature"] } }

// Step 2: Extract sensor_names from response, then:
POST /api/v1/measurements/latest
{
  "sensor_names": ["sensor1", "sensor2", "sensor3"],
  "type_names": ["temperature", "humidity"]
}
```

### Pattern 3: Get Historical Data for Geographic Area
```json
POST /api/v1/sensors
{
  "measurement_filter": {
    "expression": "coordinate.bbc.(11,46,12,47,4326)",
    "time_range": {
      "start_time": "2024-01-01T00:00:00Z",
      "end_time": "2024-01-31T23:59:59Z"
    }
  }
}
```

### Pattern 4: Real-time Monitoring with Filters
```javascript
// WebSocket connection to advanced endpoint
const ws = new WebSocket('ws://localhost:8080/api/v1/measurements/subscribe/advanced');

ws.onopen = () => {
  ws.send(JSON.stringify({
    type: 'connection_init',
    payload: {
      timeseries_filter: {
        required_types: ['temperature'],
        dataset_ids: ['weather_stations']
      },
      measurement_filter: {
        expression: 'and(temperature.gt.25, coordinate.dlt.(10000,11.35,46.5,4326))',
        latest_only: true
      }
    }
  }));
};

ws.onmessage = (event) => {
  const msg = JSON.parse(event.data);
  if (msg.type === 'data') {
    console.log('New measurement:', msg.payload);
  }
};
```

### Pattern 5: Batch Operations for Multiple Sensors
```json
// Get all types for multiple sensors
POST /api/v1/sensors/types
{
  "sensor_names": ["sensor1", "sensor2", "sensor3"],
  "distinct": true
}

// Get timeseries info for multiple sensors
POST /api/v1/sensors/timeseries
{
  "sensor_names": ["sensor1", "sensor2", "sensor3"],
  "type_names": ["temperature", "humidity"]
}
```

---

## ERROR HANDLING

All endpoints return standard HTTP status codes:

| Status Code | Meaning |
|-------------|---------|
| 200 | Success |
| 400 | Bad request (invalid parameters or filter syntax) |
| 404 | Resource not found (sensor, type, or dataset) |
| 500 | Internal server error |

Error responses include a JSON body with error details:
```json
{
  "error": "string",
  "message": "string",
  "details": "string"
}
```

---

## IMPLEMENTATION NOTES FOR LLM

**When building API calls:**

1. **For sensor discovery**: Use POST /api/v1/sensors with filter objects - it's more powerful than GET endpoint
2. **For measurements**: Use POST endpoints when dealing with multiple sensors or types
3. **For real-time data**: Use WebSocket endpoints with appropriate filters
4. **For geographic queries**: Use bbc/bbi/dlt operators in measurement_filter.expression
5. **For time-based queries**: Always use RFC3339 format for timestamps (e.g., "2024-01-01T00:00:00Z")
6. **For complex filters**: Nest and() and or() operators as needed
7. **For pagination**: Use offset/limit parameters where available

**Filter Expression Construction Tips:**
- Escape special characters (`,'"`) with backslash in regex and string values
- Use URL encoding for special characters in GET requests
- SRID parameter is optional (defaults to 4326 for WGS84)
- Values are automatically cast to numeric if possible (quote them to prevent casting)
- For geographic filters, ensure coordinates are in correct order: (x/lon, y/lat)

**Common Use Cases:**
1. Find all weather stations with temperature > 25°C → Use sensor discovery with value filter
2. Get latest readings from specific sensors → Use measurements/latest endpoint
3. Stream real-time data matching criteria → Use WebSocket with advanced filters
4. Get historical data for time range → Use measurements/historical with time range
5. Find sensors in geographic area → Use coordinate filters (bbc/bbi/dlt)

