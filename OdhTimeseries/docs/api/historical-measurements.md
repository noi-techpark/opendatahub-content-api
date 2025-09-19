# Get Historical Measurements

## Overview
Retrieves historical measurements for specified sensors and timeseries within optional time ranges. Supports both query parameters and JSON request body with pagination.

## Endpoints

### Query Parameters Endpoint
- **Method**: `GET`
- **Path**: `/api/v1/measurements/historical`
- **Parameters**: Query string

### JSON Body Endpoint
- **Method**: `POST`
- **Path**: `/api/v1/measurements/historical`
- **Content-Type**: `application/json`

## Approach
1. Validates that at least one sensor code is provided
2. Applies time range filters if provided
3. Queries all measurement tables (by data type) with timestamp ordering
4. Joins with timeseries, sensors, and types tables for metadata
5. Applies limit for pagination
6. Returns measurements ordered by timestamp DESC (newest first)

## Query Parameters (GET)

- **sensor_codes** (required): Comma-separated list of sensor codes
- **type_names** (optional): Comma-separated list of type names to filter by
- **timeseries_names** (optional): Comma-separated list of timeseries names to filter by
- **start_time** (optional): Start time in RFC3339 format (ISO 8601)
- **end_time** (optional): End time in RFC3339 format (ISO 8601)
- **limit** (optional): Maximum number of results (default: no limit)

## Request Body (POST)

```json
{
  "sensor_codes": ["TEMP_001", "GPS_001"],
  "type_names": ["air_temperature", "position"],
  "timeseries_names": ["temperature", "location"],
  "start_time": "2025-01-01T00:00:00Z",
  "end_time": "2025-01-31T23:59:59Z",
  "limit": 100
}
```

### Request Fields
- **sensor_codes** (required): Array of sensor codes
- **type_names** (optional): Array of measurement type names
- **timeseries_names** (optional): Array of timeseries names
- **start_time** (optional): Start timestamp (inclusive)
- **end_time** (optional): End timestamp (inclusive)
- **limit** (optional): Maximum number of results

## Response

### Success (200 OK)
```json
{
  "measurements": [
    {
      "timeseries_id": "550e8400-e29b-41d4-a716-446655440000",
      "sensor_code": "TEMP_001",
      "type_name": "air_temperature",
      "timeseries_name": "temperature",
      "data_type": "numeric",
      "timestamp": "2025-01-15T14:30:00Z",
      "value": 23.5
    },
    {
      "timeseries_id": "550e8400-e29b-41d4-a716-446655440000",
      "sensor_code": "TEMP_001",
      "type_name": "air_temperature",
      "timeseries_name": "temperature",
      "data_type": "numeric",
      "timestamp": "2025-01-15T14:25:00Z",
      "value": 23.2
    },
    {
      "timeseries_id": "650e8400-e29b-41d4-a716-446655440000",
      "sensor_code": "GPS_001",
      "type_name": "position",
      "timeseries_name": "location",
      "data_type": "geoposition",
      "timestamp": "2025-01-15T14:29:45Z",
      "value": "POINT(11.3547 46.4983)"
    }
  ],
  "count": 3
}
```

### Error (400 Bad Request)
```json
{
  "error": "sensor_codes parameter is required"
}
```

```json
{
  "error": "Invalid start_time format, use RFC3339"
}
```

### Error (500 Internal Server Error)
```json
{
  "error": "Failed to retrieve measurements"
}
```

## Examples

### GET - Single Sensor, Last 24 Hours
```bash
curl "http://localhost:8080/api/v1/measurements/historical?sensor_codes=TEMP_001&start_time=2025-01-14T14:30:00Z"
```

### GET - Multiple Sensors with Time Range
```bash
curl "http://localhost:8080/api/v1/measurements/historical?sensor_codes=TEMP_001,GPS_001&start_time=2025-01-01T00:00:00Z&end_time=2025-01-31T23:59:59Z"
```

### GET - With Limit
```bash
curl "http://localhost:8080/api/v1/measurements/historical?sensor_codes=ENV_001&type_names=temperature&limit=50"
```

### POST - Time Range Query
```bash
curl -X POST http://localhost:8080/api/v1/measurements/historical \
  -H "Content-Type: application/json" \
  -d '{
    "sensor_codes": ["WEATHER_01", "WEATHER_02"],
    "type_names": ["temperature", "humidity", "pressure"],
    "start_time": "2025-01-10T00:00:00Z",
    "end_time": "2025-01-15T23:59:59Z",
    "limit": 1000
  }'
```

### POST - Specific Timeseries
```bash
curl -X POST http://localhost:8080/api/v1/measurements/historical \
  -H "Content-Type: application/json" \
  -d '{
    "sensor_codes": ["TRAFFIC_CAM_01"],
    "timeseries_names": ["vehicle_count", "avg_speed"],
    "start_time": "2025-01-15T06:00:00Z",
    "end_time": "2025-01-15T18:00:00Z"
  }'
```

### POST - GPS Track History
```bash
curl -X POST http://localhost:8080/api/v1/measurements/historical \
  -H "Content-Type: application/json" \
  -d '{
    "sensor_codes": ["GPS_VEHICLE_42"],
    "type_names": ["location"],
    "start_time": "2025-01-15T08:00:00Z",
    "end_time": "2025-01-15T17:00:00Z",
    "limit": 500
  }'
```

## Response Value Formats

- **Numeric**: Number value (e.g., `23.5`)
- **String**: Text value (e.g., `"clear sky"`)
- **Boolean**: Boolean value (e.g., `false`)
- **JSON**: Object or array (e.g., `{"status": "active", "battery": 87}`)
- **GeoPosition**: WKT Point format (e.g., `"POINT(11.3547 46.4983)"`)
- **GeoShape**: WKT Polygon format (e.g., `"POLYGON((...))"`)

## Notes

- Results are ordered by timestamp DESC (newest first)
- Time range filtering is inclusive on both ends
- If no time range specified, returns all historical data (use limit to control)
- Empty results return `{"measurements": [], "count": 0}`
- Timestamps are in UTC ISO 8601 format
- Geospatial values returned in Well-Known Text (WKT) format

## Pagination

For large datasets, use the `limit` parameter to control response size:
- Start with recent data using `limit` only
- Use `start_time` and `end_time` with `limit` for time-windowed pagination
- Consider the timestamp of the last result to fetch next batch