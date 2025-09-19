#!/bin/bash

# Test script for Timeseries API
BASE_URL="http://localhost:8080"

echo "=== Timeseries API Test Script ==="
echo "Base URL: $BASE_URL"
echo

# Test health check
echo "1. Testing health endpoint..."
curl -s "$BASE_URL/api/v1/health" | jq .
echo -e "\n"

# Test info endpoint
echo "2. Testing info endpoint..."
curl -s "$BASE_URL/api/v1/info" | jq .
echo -e "\n"

# Test batch insert
echo "3. Testing batch insert..."
curl -s -X POST "$BASE_URL/api/v1/measurements/batch" \
  -H "Content-Type: application/json" \
  -d '{
    "provenance": {
      "lineage": "test-system",
      "data_collector": "test-script",
      "data_collector_version": "1.0.0"
    },
    "measurements": [
      {
        "sensor_code": "TEST_TEMP_001",
        "sensor_type": "temperature",
        "type_name": "air_temperature",
        "timeseries_name": "temperature",
        "timestamp": "2025-01-15T10:30:00Z",
        "value": 23.5
      },
      {
        "sensor_code": "TEST_GPS_001",
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
  }' | jq .
echo -e "\n"

# Test get latest measurements
echo "4. Testing get latest measurements..."
curl -s -X POST "$BASE_URL/api/v1/measurements/latest" \
  -H "Content-Type: application/json" \
  -d '{
    "sensor_codes": ["TEST_TEMP_001", "TEST_GPS_001"]
  }' | jq .
echo -e "\n"

# Test get historical measurements
echo "5. Testing get historical measurements..."
curl -s -X POST "$BASE_URL/api/v1/measurements/historical" \
  -H "Content-Type: application/json" \
  -d '{
    "sensor_codes": ["TEST_TEMP_001"],
    "start_time": "2025-01-01T00:00:00Z",
    "limit": 10
  }' | jq .
echo -e "\n"

echo "=== Test completed ==="