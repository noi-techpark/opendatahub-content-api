# Delete Measurements

## Overview
Deletes measurements based on specified filters. Supports deletion by sensor codes, type names, timeseries names, and time ranges.

## Endpoint
- **Method**: `DELETE`
- **Path**: `/api/v1/measurements`
- **Content-Type**: `application/json`

## Approach
1. Validates that at least one filter is provided
2. Builds delete conditions based on provided filters
3. Executes deletion across all measurement tables (by data type)
4. Uses JOIN with timeseries, sensors, and types tables for filtering
5. Returns success confirmation

## Request Body

```json
{
  "sensor_codes": ["TEMP_001", "TEMP_002"],
  "type_names": ["air_temperature"],
  "timeseries_names": ["temperature"],
  "start_time": "2025-01-01T00:00:00Z",
  "end_time": "2025-01-31T23:59:59Z"
}
```

### Request Fields (All Optional, but at least one required)
- **sensor_codes**: Array of sensor codes to delete measurements for
- **type_names**: Array of measurement type names to delete
- **timeseries_names**: Array of timeseries names to delete
- **start_time**: Delete measurements from this time (inclusive)
- **end_time**: Delete measurements until this time (inclusive)

## Response

### Success (200 OK)
```json
{
  "message": "Measurements deleted successfully"
}
```

### Error (400 Bad Request)
```json
{
  "error": "At least one filter must be provided"
}
```

### Error (500 Internal Server Error)
```json
{
  "error": "Failed to delete measurements"
}
```

## Examples

### Delete by Sensor Code
```bash
curl -X DELETE http://localhost:8080/api/v1/measurements \
  -H "Content-Type: application/json" \
  -d '{
    "sensor_codes": ["TEMP_SOUTH_001", "TEMP_NORTH_001"]
  }'
```

### Delete by Time Range
```bash
curl -X DELETE http://localhost:8080/api/v1/measurements \
  -H "Content-Type: application/json" \
  -d '{
    "start_time": "2025-01-01T00:00:00Z",
    "end_time": "2025-01-07T23:59:59Z"
  }'
```

### Delete by Type and Time Range
```bash
curl -X DELETE http://localhost:8080/api/v1/measurements \
  -H "Content-Type: application/json" \
  -d '{
    "type_names": ["air_temperature", "humidity"],
    "start_time": "2025-01-15T00:00:00Z",
    "end_time": "2025-01-15T23:59:59Z"
  }'
```

### Delete Specific Timeseries
```bash
curl -X DELETE http://localhost:8080/api/v1/measurements \
  -H "Content-Type: application/json" \
  -d '{
    "sensor_codes": ["ENV_001"],
    "timeseries_names": ["temperature", "humidity"]
  }'
```

### Complex Filter Combination
```bash
curl -X DELETE http://localhost:8080/api/v1/measurements \
  -H "Content-Type: application/json" \
  -d '{
    "sensor_codes": ["SENSOR_A", "SENSOR_B"],
    "type_names": ["temperature"],
    "timeseries_names": ["air_temp"],
    "start_time": "2025-01-10T00:00:00Z",
    "end_time": "2025-01-20T23:59:59Z"
  }'
```

## Safety Notes

- **Irreversible**: Deleted measurements cannot be recovered
- **Filter Required**: At least one filter must be provided to prevent accidental deletion of all data
- **Cross-Table**: Deletion occurs across all data type tables (numeric, string, json, geoposition, geoshape, boolean)
- **Performance**: Large deletions may take time; consider using time-based filtering for better performance