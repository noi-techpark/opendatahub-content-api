# 🧪 Test Payloads for Sensor Discovery with Complex Expressions

This file contains comprehensive test payloads for testing the sensor discovery endpoint with various complex filter expressions, including geoposition, geoshape, JSON, and logical conjunctions (AND/OR).

## 1. Basic Numeric Tests

### Find sensors with high air temperature
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "air_temperature.gt.10",
      "latest_only": true
    }
  }'
```

## 2. JSON Field Tests

### JSON path filtering - sampling interval greater than 20
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "sensor_config.sampling_interval.gt.20",
      "latest_only": true
    }
  }'
```

### JSON boolean field
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "sensor_config.filter_enabled.eq.true",
      "latest_only": true
    }
  }'
```

### JSON nested numeric field
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "sensor_config.threshold.gteq.60",
      "latest_only": true
    }
  }'
```

## 3. String Field Tests

### String equality
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "device_status.eq.LOW_BATTERY",
      "latest_only": true
    }
  }'
```

### String regex pattern
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "device_status.re.^LOW_.*",
      "latest_only": true
    }
  }'
```

## 4. Geospatial Tests

### Geoposition - point within bounding box
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "location.bbi.(11.5,46.7,11.6,46.8)",
      "latest_only": true
    }
  }'
```

### Geoshape - polygon intersects with bounding box
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "coverage_area.bbi.(11.6,46.4,11.65,46.5)",
      "latest_only": true
    }
  }'
```

## 5. AND Conjunction Tests

### Multiple numeric conditions with AND
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "and(air_temperature.gt.10, pm25.lt.100)",
      "latest_only": true
    }
  }'
```

### Mixed data types with AND
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "and(air_temperature.gteq.6, device_status.eq.LOW_BATTERY)",
      "latest_only": true
    }
  }'
```

### JSON and numeric with AND
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "and(sensor_config.threshold.gt.50, air_temperature.lt.15)",
      "latest_only": true
    }
  }'
```

## 6. OR Conjunction Tests

### Multiple numeric conditions with OR
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "or(air_temperature.gt.15, pm25.gt.90)",
      "latest_only": true
    }
  }'
```

### String OR conditions
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "or(device_status.eq.LOW_BATTERY, device_status.eq.OFFLINE)",
      "latest_only": true
    }
  }'
```

## 7. Nested AND/OR Tests

### Complex nested expression
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "and(or(air_temperature.gt.10, pm25.gt.80), sensor_config.threshold.lt.70)",
      "latest_only": true
    }
  }'
```

### Multiple nested levels
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "or(and(air_temperature.gteq.6, pm25.lteq.100), and(device_status.eq.LOW_BATTERY, sensor_config.sampling_interval.gt.20))",
      "latest_only": true
    }
  }'
```

## 8. Combined Timeseries + Measurement Filtering

### Combine dataset filtering with measurement conditions
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "timeseries_filter": {
      "required_types": ["air_temperature", "sensor_config"]
    },
    "measurement_filter": {
      "expression": "and(air_temperature.gt.5, sensor_config.threshold.gt.60)",
      "latest_only": true
    }
  }'
```

### Optional types with complex measurement filter
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "timeseries_filter": {
      "optional_types": ["coverage_area", "location"]
    },
    "measurement_filter": {
      "expression": "or(coverage_area.bbi.(11.6,46.4,11.7,46.5), device_status.eq.LOW_BATTERY)",
      "latest_only": true
    },
    "limit": 10
  }'
```

## 9. Time Range Tests

### Historical data with complex expressions
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "and(air_temperature.gt.8, or(pm25.gt.50, sensor_config.filter_enabled.eq.false))",
      "latest_only": false,
      "time_range": {
        "start_time": "2025-09-20T14:00:00Z",
        "end_time": "2025-09-20T16:30:00Z"
      }
    }
  }'
```

## 10. Edge Case Tests

### Many conditions in OR
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "or(air_temperature.gt.12, pm25.gt.95, noise_level.gt.75, power_generation.gt.15)",
      "latest_only": true
    }
  }'
```

### Deeply nested expression
```bash
curl -X POST 'http://localhost:8080/api/v1/sensors' \
  -H 'Content-Type: application/json' \
  -d '{
    "measurement_filter": {
      "expression": "and(or(air_temperature.gt.10, pm25.gt.80), or(sensor_config.threshold.gt.65, and(device_status.eq.LOW_BATTERY, power_generation.lt.20)))",
      "latest_only": true
    }
  }'
```

## 🔍 Expected Test Results

Based on the sample data provided, here's what you should expect:

### JSON Tests
- **sensor_config.threshold.gteq.60**: Should find `PARK_Downtown_029` (threshold: 67.56)
- **sensor_config.filter_enabled.eq.false**: Should find `CO2_University_017` (filter_enabled: false)
- **sensor_config.sampling_interval.gt.20**: Should find `PARK_Downtown_029` (sampling_interval: 24)

### String Tests
- **device_status.eq.LOW_BATTERY**: Should find `PARK_Downtown_033`
- **device_status.re.^LOW_.***: Should match any device with status starting with "LOW_"

### Geoshape Tests
- **coverage_area.bbi.(11.6,46.4,11.65,46.5)**: Should find sensors with coverage areas intersecting the specified bounding box
- Look for sensors like `SOLAR_Hospital_045` with polygon coordinates around (11.622173, 46.455523)

### Geoposition Tests
- **location.bbi.(11.5,46.7,11.6,46.8)**: Should find sensors with points within the bounding box
- Look for sensors like `RAIN_Station_077` with point (11.581751, 46.765908)

### Numeric Tests
- **air_temperature.gt.10**: Should find sensors with temperature > 10°C (like `PARK_Downtown_033`: 12.1°C, `PARK_Downtown_029`: 11.4°C)
- **pm25.gt.90**: Should find sensors with high PM2.5 values (like `CO2_University_017`: 95.6)

### Combined Tests
- Multiple conditions will return sensors meeting all specified criteria
- Nested AND/OR expressions will demonstrate complex logical filtering

## 📝 Notes

- Replace `http://localhost:8080` with your actual server URL
- Adjust numeric thresholds based on your actual data ranges
- Some geospatial operators may need implementation in your filter parser
- Test with both `latest_only: true` and `latest_only: false` to see different behaviors
- Use the `limit` parameter to control result set size

## 🚀 Usage

Save this file and run the commands to test your sensor discovery endpoint with complex expressions!