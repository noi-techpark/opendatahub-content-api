# Batch Insert Measurements

## Overview
Inserts a batch of measurements for multiple sensors and timeseries. Supports all data types and ensures idempotency.

## Endpoint
- **Method**: `POST`
- **Path**: `/api/v1/measurements/batch`
- **Content-Type**: `application/json`

## Approach
1. Validates the batch request format
2. Creates or retrieves provenance record if provided
3. For each measurement:
   - Gets or creates the sensor record
   - Infers data type from value
   - Gets or creates the type record
   - Gets or creates the timeseries record
   - Converts value to appropriate format
   - Inserts measurement with idempotency (ON CONFLICT DO NOTHING)
4. Returns success count and any failures

## Request Body

```json
{
  "provenance": {
    "lineage": "sensor-network-01",
    "data_collector": "iot-collector",
    "data_collector_version": "1.2.3"
  },
  "measurements": [
    {
      "sensor_code": "TEMP_001",
      "sensor_type": "temperature",
      "type_name": "air_temperature",
      "timeseries_name": "temperature",
      "timestamp": "2025-01-15T10:30:00Z",
      "value": 23.5
    },
    {
      "sensor_code": "GPS_001",
      "sensor_type": "gps",
      "type_name": "position",
      "timeseries_name": "location",
      "timestamp": "2025-01-15T10:30:00Z",
      "value": {
        "type": "Point",
        "coordinates": [11.3547, 46.4983]
      }
    }
  ]
}
```

### Request Fields
- **provenance** (optional): Data lineage information
  - `lineage`: Source system identifier
  - `data_collector`: Collector service name
  - `data_collector_version`: Version of collector
- **measurements** (required): Array of measurements
  - `sensor_code`: Unique sensor identifier
  - `sensor_type`: Type/category of sensor
  - `type_name`: Measurement type name
  - `timeseries_name`: Name of timeseries within sensor
  - `timestamp`: ISO 8601 timestamp
  - `value`: Measurement value (any supported type)

### Supported Value Types
- **Numeric**: Numbers (int, float)
- **String**: Text values
- **Boolean**: true/false
- **JSON**: Objects or arrays
- **GeoPosition**: GeoJSON Point
- **GeoShape**: GeoJSON Polygon

## Response

### Success (200 OK)
```json
{
  "processed": 2,
  "total": 2
}
```

### Partial Success (200 OK)
```json
{
  "processed": 1,
  "total": 2,
  "warning": "Some measurements failed to process",
  "failed": 1
}
```

### Error (400 Bad Request)
```json
{
  "error": "No measurements provided"
}
```

### Error (500 Internal Server Error)
```json
{
  "error": "Failed to process any measurements",
  "details": "database connection failed"
}
```

## Examples

### Temperature Measurement
```bash
curl -X POST http://localhost:8080/api/v1/measurements/batch \
  -H "Content-Type: application/json" \
  -d '{
    "measurements": [{
      "sensor_code": "TEMP_SOUTH_001",
      "sensor_type": "temperature",
      "type_name": "air_temperature_celsius",
      "timeseries_name": "temperature",
      "timestamp": "2025-01-15T14:30:00Z",
      "value": 18.7
    }]
  }'
```

### GPS Location
```bash
curl -X POST http://localhost:8080/api/v1/measurements/batch \
  -H "Content-Type: application/json" \
  -d '{
    "measurements": [{
      "sensor_code": "GPS_VEHICLE_42",
      "sensor_type": "gps_tracker",
      "type_name": "location",
      "timeseries_name": "position",
      "timestamp": "2025-01-15T14:35:22Z",
      "value": {
        "type": "Point",
        "coordinates": [11.3547, 46.4983]
      }
    }]
  }'
```

### Mixed Data Types
```bash
curl -X POST http://localhost:8080/api/v1/measurements/batch \
  -H "Content-Type: application/json" \
  -d '{
    "provenance": {
      "lineage": "smart-city-network",
      "data_collector": "iot-gateway",
      "data_collector_version": "2.1.0"
    },
    "measurements": [
      {
        "sensor_code": "ENV_001",
        "sensor_type": "environmental",
        "type_name": "temperature",
        "timeseries_name": "air_temp",
        "timestamp": "2025-01-15T15:00:00Z",
        "value": 22.3
      },
      {
        "sensor_code": "ENV_001", 
        "sensor_type": "environmental",
        "type_name": "humidity",
        "timeseries_name": "air_humidity",
        "timestamp": "2025-01-15T15:00:00Z",
        "value": 65.2
      },
      {
        "sensor_code": "ENV_001",
        "sensor_type": "environmental", 
        "type_name": "air_quality",
        "timeseries_name": "aqi_data",
        "timestamp": "2025-01-15T15:00:00Z",
        "value": {
          "aqi": 42,
          "pm25": 8.5,
          "pm10": 15.2,
          "co2": 410
        }
      }
    ]
  }'
```