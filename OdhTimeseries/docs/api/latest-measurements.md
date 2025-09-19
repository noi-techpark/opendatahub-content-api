# Get Latest Measurements

## Overview
Retrieves the most recent measurement for specified sensors and timeseries. Supports both query parameters and JSON request body.

## Endpoints

### Query Parameters Endpoint
- **Method**: `GET`
- **Path**: `/api/v1/measurements/latest`
- **Parameters**: Query string

### JSON Body Endpoint
- **Method**: `POST`
- **Path**: `/api/v1/measurements/latest`
- **Content-Type**: `application/json`

## Approach
1. Validates that at least one sensor code is provided
2. Queries all measurement tables (by data type) for latest timestamps
3. Joins with timeseries, sensors, and types tables for metadata
4. Returns measurements with type information and formatted values
5. Orders by timestamp DESC to get latest measurements

## Query Parameters (GET)

- **sensor_codes** (required): Comma-separated list of sensor codes
- **type_names** (optional): Comma-separated list of type names to filter by
- **timeseries_names** (optional): Comma-separated list of timeseries names to filter by

## Request Body (POST)

```json
{
  "sensor_codes": ["TEMP_001", "GPS_001"],
  "type_names": ["air_temperature", "position"],
  "timeseries_names": ["temperature", "location"]
}
```

### Request Fields
- **sensor_codes** (required): Array of sensor codes
- **type_names** (optional): Array of measurement type names
- **timeseries_names** (optional): Array of timeseries names

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
      "timeseries_id": "650e8400-e29b-41d4-a716-446655440000",
      "sensor_code": "GPS_001",
      "type_name": "position",
      "timeseries_name": "location",
      "data_type": "geoposition",
      "timestamp": "2025-01-15T14:29:45Z",
      "value": "POINT(11.3547 46.4983)"
    }
  ],
  "count": 2
}
```

### Error (400 Bad Request)
```json
{
  "error": "sensor_codes parameter is required"
}
```

### Error (500 Internal Server Error)
```json
{
  "error": "Failed to retrieve measurements"
}
```

## Examples

### GET - Single Sensor
```bash
curl "http://localhost:8080/api/v1/measurements/latest?sensor_codes=TEMP_001"
```

### GET - Multiple Sensors
```bash
curl "http://localhost:8080/api/v1/measurements/latest?sensor_codes=TEMP_001,GPS_001,ENV_001"
```

### GET - Filter by Type
```bash
curl "http://localhost:8080/api/v1/measurements/latest?sensor_codes=ENV_001&type_names=temperature,humidity"
```

### GET - Filter by Timeseries
```bash
curl "http://localhost:8080/api/v1/measurements/latest?sensor_codes=WEATHER_STATION_01&timeseries_names=air_temp,soil_temp"
```

### POST - JSON Request
```bash
curl -X POST http://localhost:8080/api/v1/measurements/latest \
  -H "Content-Type: application/json" \
  -d '{
    "sensor_codes": ["TEMP_001", "HUMID_001"],
    "type_names": ["air_temperature", "humidity"]
  }'
```

### POST - Complex Filter
```bash
curl -X POST http://localhost:8080/api/v1/measurements/latest \
  -H "Content-Type: application/json" \
  -d '{
    "sensor_codes": ["ENV_STATION_01", "ENV_STATION_02", "ENV_STATION_03"],
    "type_names": ["temperature", "humidity", "air_pressure"],
    "timeseries_names": ["hourly_avg", "current"]
  }'
```

## Response Value Formats

- **Numeric**: Number value (e.g., `23.5`)
- **String**: Text value (e.g., `"sunny"`)
- **Boolean**: Boolean value (e.g., `true`)
- **JSON**: Object or array (e.g., `{"temp": 20, "unit": "C"}`)
- **GeoPosition**: WKT Point format (e.g., `"POINT(11.3547 46.4983)"`)
- **GeoShape**: WKT Polygon format (e.g., `"POLYGON((...))"`)

## Notes

- Returns only the most recent measurement for each timeseries
- If no measurements found for a sensor, it won't appear in results
- Geospatial values are returned in Well-Known Text (WKT) format
- All timestamps are in UTC ISO 8601 format