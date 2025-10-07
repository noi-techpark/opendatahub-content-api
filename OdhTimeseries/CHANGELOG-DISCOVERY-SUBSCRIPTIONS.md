# Changelog: Advanced Discovery-Based Streaming Subscriptions

**Date:** 2025-10-07
**Feature:** Discovery-Based WebSocket Subscriptions
**Status:** ✅ Implemented and Documented

## Overview

Extended the WebSocket streaming subscription endpoint to support DiscoverSensors-style filter parameters. Users can now subscribe to sensors using advanced criteria instead of manually specifying sensor names.

## What's New

### 1. Discovery-Based Subscriptions

Instead of providing explicit sensor names, users can now use the same powerful filters from the DiscoverSensors API:

```json
{
  "action": "subscribe",
  "timeseries_filter": {
    "required_types": ["temperature", "humidity"],
    "optional_types": ["pressure"],
    "dataset_ids": ["weather_stations"]
  },
  "measurement_filter": {
    "latest_only": true,
    "expression": "temperature.gteq.20"
  },
  "spatial_filter": {
    "type": "bbox",
    "coordinates": [10.5, 46.2, 12.5, 47.2]
  },
  "limit": 100
}
```

### 2. Automatic Sensor Discovery

The server automatically:
1. Executes sensor discovery based on provided filters
2. Extracts sensor names from discovered sensors
3. Creates subscription for all matching sensors
4. Returns the list of discovered sensors in acknowledgment

### 3. Filter Parameters Supported

#### Timeseries Filter
- `required_types`: Sensors must have ALL of these measurement types
- `optional_types`: Sensors may have ANY of these measurement types
- `dataset_ids`: Filter by dataset membership

#### Measurement Filter
- `latest_only`: Only consider latest measurements
- `expression`: Value filter expressions (e.g., `"temperature.gteq.20"`)
- `time_range`: Time constraints with start_time and end_time

#### Spatial Filter
- `type: "bbox"`: Bounding box with [minLon, minLat, maxLon, maxLat]
- `type: "radius"`: Radius with [centerLon, centerLat, radiusMeters]

#### Other Parameters
- `limit`: Maximum number of sensors to subscribe to

## Implementation Changes

### Files Modified

1. **internal/streaming/websocket.go**
   - Added `repo *repository.Repository` field to `WebSocketManager`
   - Updated `NewWebSocketManager()` to accept repository parameter
   - Extended `SubscriptionRequest` with discovery filter fields
   - Modified `handleSubscribe()` to perform sensor discovery when filters provided

2. **cmd/server/main.go**
   - Updated WebSocketManager initialization to pass repository

3. **internal/handlers/streaming.go**
   - Updated Swagger documentation to include both subscription methods

### Files Added

1. **test_streaming_discovery.py**
   - Comprehensive test suite for discovery-based subscriptions
   - Tests for required_types, measurement filters, spatial filters
   - Comparison tests between simple and discovery subscriptions

### Documentation Updated

1. **STREAMING.md**
   - Added "Advanced Discovery-Based Subscriptions" section
   - Documented all filter parameters
   - Provided use case examples

2. **QUICKSTART-STREAMING.md**
   - Added discovery subscription examples
   - Reference to test script

3. **README.md**
   - Added streaming features to features list
   - Added WebSocket endpoint to API endpoints section

## Use Cases

### 1. Dynamic Sensor Groups
Subscribe to all sensors matching criteria without maintaining explicit lists:
```json
{
  "action": "subscribe",
  "timeseries_filter": {
    "required_types": ["power_generation"]
  },
  "limit": 50
}
```

### 2. Value-Based Subscriptions
Subscribe only to sensors with specific measurement values:
```json
{
  "action": "subscribe",
  "timeseries_filter": {
    "required_types": ["temperature"]
  },
  "measurement_filter": {
    "latest_only": true,
    "expression": "temperature.gteq.30"
  }
}
```

### 3. Geographic Subscriptions
Subscribe to sensors in specific areas:
```json
{
  "action": "subscribe",
  "timeseries_filter": {
    "required_types": ["location", "occupancy"]
  },
  "spatial_filter": {
    "type": "bbox",
    "coordinates": [11.3, 46.4, 11.4, 46.5]
  }
}
```

## Backwards Compatibility

✅ **Fully backwards compatible** - Simple subscriptions with `sensor_names` continue to work exactly as before.

## Testing

### Test Script
```bash
python3 test_streaming_discovery.py
```

### Test Coverage
- ✅ Discovery with required_types
- ✅ Discovery with measurement value filters
- ✅ Discovery with spatial filters
- ✅ Comparison of simple vs discovery subscriptions
- ✅ Error handling for invalid filters

## Performance Considerations

1. **Discovery Overhead**: Discovery query runs once per subscription (not per update)
2. **Update Delivery**: Same performance as simple subscriptions after discovery
3. **Scalability**: Discovery limited by `limit` parameter to prevent excessive subscriptions

## Benefits

1. **Dynamic Groups**: Subscribe to sensors without knowing their names in advance
2. **Reduced Coupling**: Client doesn't need to maintain sensor name lists
3. **Flexible Criteria**: Complex multi-dimensional filtering (type + value + location)
4. **Automatic Updates**: New sensors matching criteria are discovered on each subscription
5. **Code Reuse**: Leverages existing DiscoverSensors infrastructure

## Example Response

Discovery acknowledgment includes discovered sensor list:

```json
{
  "type": "ack",
  "message": "Subscription created successfully",
  "data": {
    "sensor_count": 15,
    "sensor_names": [
      "TEMP_Station_001",
      "TEMP_Station_002",
      "..."
    ],
    "type_names": null
  }
}
```

## Migration Guide

### From Simple to Discovery Subscriptions

**Before (Simple):**
```json
{
  "action": "subscribe",
  "sensor_names": ["TEMP_001", "TEMP_002", "TEMP_003"]
}
```

**After (Discovery):**
```json
{
  "action": "subscribe",
  "timeseries_filter": {
    "required_types": ["temperature"]
  },
  "limit": 10
}
```

## Related Documentation

- [STREAMING.md](STREAMING.md) - Complete streaming documentation
- [QUICKSTART-STREAMING.md](QUICKSTART-STREAMING.md) - Quick start guide
- [SETUP-AND-TEST-GUIDE.md](SETUP-AND-TEST-GUIDE.md) - Comprehensive setup guide
- [TEST_REPORT.md](TEST_REPORT.md) - Streaming test results

## Future Enhancements

Potential future improvements:
- [ ] Real-time re-discovery (periodic or trigger-based)
- [ ] Subscription templates
- [ ] Discovery result caching
- [ ] Incremental subscription updates
- [ ] Subscription analytics/monitoring
