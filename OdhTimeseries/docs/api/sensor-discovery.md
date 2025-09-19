# Sensor Discovery

## Overview
Discovers sensors based on dataset membership or value-based conditions. Provides two main discovery methods.

## Endpoints

### Discover Sensors by Dataset
- **Method**: `GET`
- **Path**: `/api/v1/sensors/dataset/{dataset_id}`

### Search Sensors by Condition
- **Method**: `POST`
- **Path**: `/api/v1/sensors/search`
- **Content-Type**: `application/json`

## Dataset Discovery

### Approach
1. Validates UUID format for dataset_id
2. Queries sensors that have timeseries belonging to the specified dataset
3. Returns sensor metadata including geospatial information

### Request
- **dataset_id** (path parameter): UUID of the dataset

### Response (200 OK)
```json
{
  "sensors": [
    {
      "id": 1,
      "name": "Temperature Sensor South",
      "station_code": "TEMP_SOUTH_001",
      "station_type": "temperature",
      "parent_id": null,
      "geospatial_location": [11.3547, 46.4983],
      "metadata": {"manufacturer": "SensorTech", "model": "T300"},
      "created_on": "2025-01-10T10:00:00Z",
      "is_active": true,
      "is_available": true
    }
  ],
  "count": 1
}
```

## Condition-Based Search

### Approach
1. Validates type_name and condition parameters
2. Determines data type for the measurement type
3. Queries measurement tables for values matching the condition
4. Returns list of sensor codes that match the criteria
5. Supports time range filtering

### Request Body
```json
{
  "type_name": "air_temperature",
  "condition": ">",
  "value": 25.0,
  "start_time": "2025-01-01T00:00:00Z",
  "end_time": "2025-01-31T23:59:59Z"
}
```

### Request Fields
- **type_name** (required): Name of the measurement type
- **condition** (required): SQL comparison operator (`>`, `<`, `=`, `>=`, `<=`, `!=`)
- **value** (required): Value to compare against
- **start_time** (optional): Start of time range to search
- **end_time** (optional): End of time range to search

### Response (200 OK)
```json
{
  "sensor_codes": ["TEMP_001", "TEMP_005", "TEMP_012"],
  "count": 3
}
```

## Examples

### Find Sensors in Dataset
```bash
curl "http://localhost:8080/api/v1/sensors/dataset/550e8400-e29b-41d4-a716-446655440000"
```

### Find Hot Temperature Sensors
```bash
curl -X POST http://localhost:8080/api/v1/sensors/search \
  -H "Content-Type: application/json" \
  -d '{
    "type_name": "air_temperature",
    "condition": ">",
    "value": 30.0
  }'
```

### Find Sensors with Low Battery
```bash
curl -X POST http://localhost:8080/api/v1/sensors/search \
  -H "Content-Type: application/json" \
  -d '{
    "type_name": "battery_level",
    "condition": "<",
    "value": 20.0,
    "start_time": "2025-01-15T00:00:00Z"
  }'
```

### Find Sensors in Geographic Area
```bash
curl -X POST http://localhost:8080/api/v1/sensors/search \
  -H "Content-Type: application/json" \
  -d '{
    "type_name": "position",
    "condition": "ST_Intersects",
    "value": "POLYGON((11.0 46.0, 12.0 46.0, 12.0 47.0, 11.0 47.0, 11.0 46.0))",
    "start_time": "2025-01-14T00:00:00Z",
    "end_time": "2025-01-15T23:59:59Z"
  }'
```

### Find Offline Sensors
```bash
curl -X POST http://localhost:8080/api/v1/sensors/search \
  -H "Content-Type: application/json" \
  -d '{
    "type_name": "status",
    "condition": "=",
    "value": "offline",
    "start_time": "2025-01-15T00:00:00Z"
  }'
```

## Error Responses

### Invalid Dataset ID (400 Bad Request)
```json
{
  "error": "Invalid dataset ID format"
}
```

### Missing Required Fields (400 Bad Request)
```json
{
  "error": "type_name is required"
}
```

### Server Error (500 Internal Server Error)
```json
{
  "error": "Failed to retrieve sensors"
}
```

## Supported Condition Operators

### Numeric Types
- `>` - Greater than
- `<` - Less than  
- `>=` - Greater than or equal
- `<=` - Less than or equal
- `=` - Equal to
- `!=` - Not equal to

### String Types
- `=` - Exact match
- `!=` - Not equal
- `LIKE` - Pattern matching with wildcards
- `ILIKE` - Case-insensitive pattern matching

### Boolean Types
- `=` - Exact match
- `!=` - Not equal

### Geospatial Types
- `ST_Intersects` - Geometry intersection
- `ST_Contains` - Geometry containment
- `ST_Within` - Within geometry
- `ST_DWithin` - Within distance

## Notes

- Dataset discovery returns full sensor objects with metadata
- Condition search returns only sensor codes for performance
- Geospatial conditions require WKT (Well-Known Text) format values
- Time range filtering is optional but recommended for performance
- All timestamps use UTC ISO 8601 format