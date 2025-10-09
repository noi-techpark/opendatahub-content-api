# Timeseries Federation Integration Guide

## Overview

This guide explains how to integrate timeseries-based filtering into the Content API, allowing sensors to be filtered by their timeseries data (datasets, measurement types, measurement values) while preserving pagination and performance.

## Architecture

### Problem Statement
- **Content Service**: Manages sensor metadata (tags, images, descriptions)
- **Timeseries Service**: Manages sensor timeseries data (measurements, datasets, types)
- **Challenge**: Filter sensors in Content API based on timeseries constraints while maintaining pagination

### Solution: Iterative Verification Pattern

```
┌─────────────────┐
│  Client Request │  (page=1, pagesize=100, tsdatasetids=abc123)
└────────┬────────┘
         │
         ▼
┌─────────────────────────────────────┐
│  Content API (SensorController)      │
│  1. Apply standard filters           │
│  2. Fetch 100 sensor IDs             │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│  Timeseries API (/sensors/verify)   │
│  Verify which sensors satisfy:       │
│  - Dataset membership                │
│  - Required/optional measurement types│
│  - Measurement value conditions      │
└────────┬────────────────────────────┘
         │
         ▼ (90 verified, 10 excluded)
┌─────────────────────────────────────┐
│  Content API (Federation Helper)    │
│  - Need 10 more sensors              │
│  - Fetch next batch (offset=100)     │
│  - Send to /verify again              │
│  - Repeat until pagesize met          │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────┐
│  Return Results │  (100 verified sensors)
└─────────────────┘
```

## Implementation Details

### Phase 1: Timeseries API Enhancement ✓

The `/sensors/verify` endpoint already supports dataset filtering:

```json
POST /api/v1/sensors/verify
{
  "sensor_names": ["urn:odh:temp:sensor1", "urn:odh:hum:sensor2"],
  "timeseries_filter": {
    "dataset_ids": ["abc-123", "def-456"],
    "required_types": ["temperature", "humidity"],
    "optional_types": ["pressure"]
  },
  "measurement_filter": {
    "latest_only": true,
    "expression": "and(temperature.gteq.20, temperature.lteq.30)",
    "time_range": {
      "start_time": "2025-01-01T00:00:00Z",
      "end_time": "2025-12-31T23:59:59Z"
    }
  }
}
```

### Phase 2: Content API Federation Infrastructure ✓

Created the following classes:

#### `TimeseriesFilterConfig.cs`
Configuration class defining:
- API base URL
- Dataset IDs to filter by
- Required/optional measurement types
- Measurement expressions
- Performance tuning parameters

#### `TimeseriesFederationHelper.cs`
Core federation logic implementing:
- `FetchAndVerifySensorsAsync()`: Iterative fetch-verify loop
- Automatic pagination adjustment
- Safeguards against infinite loops
- Detailed logging

#### `TimeseriesFederationExtensions.cs`
Extension methods providing:
- Easy integration into any controller
- Parameter parsing from query strings
- Graceful fallback if federation fails

### Phase 3: SensorController Integration ✓

#### Query Parameters Added to GetSensorList

New query parameters for timeseries filtering:
- `tsdatasetids`: Comma-separated dataset IDs
- `tsrequiredtypes`: Comma-separated required types
- `tsoptionaltypes`: Comma-separated optional types
- `tsmeasurementexpr`: Measurement filter expression
- `tslatestonly`: Latest measurements only flag
- `tsstarttime`: Start time (RFC3339)
- `tsendtime`: End time (RFC3339)

**Note**: The timeseries API URL is configured via environment variable (`ASPNETCORE_TimeseriesConfig__ServiceUrl`), not as a query parameter.

#### GetFiltered Method Integration

The `GetFiltered` method now includes timeseries federation logic:

```csharp
private Task<IActionResult> GetFiltered(
    // ... existing parameters ...
    string? tsdatasetids = null,
    string? tsrequiredtypes = null,
    string? tsoptionaltypes = null,
    string? tsmeasurementexpr = null,
    bool? tslatestonly = null,
    string? tsstarttime = null,
    string? tsendtime = null,
    CancellationToken cancellationToken = default)
{
    return DoAsyncReturn(async () =>
    {
        // ... existing filter building logic ...

        // Build base query (existing logic)
        var query = QueryFactory
            .Query()
            .SelectRaw("data")
            .From("sensors")
            .SensorWhereExpression(...)
            .ApplyRawFilter(rawfilter)
            .ApplyOrdering(ref seed, geosearchresult, rawsort);

        // Check if timeseries filtering should be applied
        var timeseriesConfig = TimeseriesFederationExtensions.ParseTimeseriesConfig(
            settings.TimeseriesConfig.ServiceUrl, tsdatasetids, tsrequiredtypes, tsoptionaltypes,
            tsmeasurementexpr, tslatestonly, tsstarttime, tsendtime,
            settings.TimeseriesConfig.FetchBatchSize, Logger);

        if (timeseriesConfig != null && timeseriesConfig.Enabled)
        {
            // Apply timeseries federation
            var federationHelper = new TimeseriesFederationHelper(httpClient, Logger, timeseriesConfig);

            // Create ID-only query for verification
            var idQuery = query.Clone().Select("id");

            // Fetch and verify
            var federationResult = await federationHelper.FetchAndVerifySensorsAsync(
                idQuery, (int)pagenumber, pagesize, cancellationToken);

            if (federationResult.VerifiedIds.Count == 0)
            {
                return ResponseHelpers.GetResult(pagenumber, 0, 0, seed,
                    Array.Empty<object>(), Url);
            }

            // Fetch full data for verified sensors only
            var verifiedData = await query
                .Clone()
                .WhereIn("id", federationResult.VerifiedIds)
                .GetAsync<JsonRaw>(cancellationToken: cancellationToken);

            var dataTransformed = verifiedData.Select(raw =>
                raw.TransformRawData(language, fields?.Split(',') ?? Array.Empty<string>(),
                    filteroutNullValues: removenullvalues, urlGenerator: UrlGenerator,
                    fieldstohide: null));

            uint totalcount = (uint)federationResult.VerifiedIds.Count;
            uint totalpages = pagenumber; // Conservative estimate

            return ResponseHelpers.GetResult(pagenumber, totalpages, totalcount,
                seed, dataTransformed, Url);
        }

        // Normal pagination (no timeseries filtering)
        var data = await query.PaginateAsync<JsonRaw>(
            page: (int)pagenumber, perPage: pagesize);

        // ... existing transformation and return logic ...
    });
}
```

### Phase 4: Environment Configuration

Configure timeseries API in `.env`:

```bash
ASPNETCORE_TimeseriesConfig__ServiceUrl=http://localhost:8080
ASPNETCORE_TimeseriesConfig__FetchBatchSize=100
```

HttpClient is registered automatically in `Startup.cs` and uses the configured settings.

## Usage Examples

### Example 1: Filter by Dataset

```bash
GET /api/Sensor?tsdatasetids=abc-123,def-456
```

Returns only sensors that have timeseries in datasets `abc-123` OR `def-456`.

**Note**: The timeseries API URL is configured via environment variable, not as a query parameter.

### Example 2: Filter by Required Types

```bash
GET /api/Sensor?tsrequiredtypes=temperature,humidity
```

Returns only sensors that have BOTH temperature AND humidity timeseries.

### Example 3: Complex Measurement Filter

```bash
GET /api/Sensor?tsrequiredtypes=temperature&tsmeasurementexpr=and(temperature.gteq.20,temperature.lteq.30)&tslatestonly=true
```

Returns sensors that:
- Have temperature timeseries
- Latest temperature measurement is between 20 and 30

### Example 4: Combined Content + Timeseries Filters

```bash
GET /api/Sensor?sensortype=TEMP&manufacturer=SensorTech&tsdatasetids=weather-stations&tsrequiredtypes=temperature,humidity
```

Returns sensors that:
- Have sensor type "TEMP" (content filter)
- Are manufactured by "SensorTech" (content filter)
- Have timeseries in "weather-stations" dataset (timeseries filter)
- Have both temperature and humidity measurements (timeseries filter)

## Performance Considerations

### Tuning Parameters

1. **FetchBatchSize** (default: 100, configurable via environment variable)
   - Higher = fewer API calls, more data transferred
   - Lower = more API calls, less waste if many sensors excluded
   - Recommended: Set to 2-3x expected pagesize
   - Configured via `ASPNETCORE_TimeseriesConfig__FetchBatchSize` environment variable

2. **TimeoutSeconds** (default: 30)
   - Timeout for each /verify API call
   - Increase if datasets are very large

### Loop Behavior

The federation helper iterates through sensors until:
- The requested page size is met, OR
- No more sensors are available in the content database

There is no artificial limit on total fetches - the loop naturally terminates when sensors are exhausted.

### Optimization Strategies

1. **Caching**: Consider caching verification results for short periods
2. **Batch Size**: Tune based on exclusion rate in production
3. **Early Exit**: Stop fetching once pagesize is met
4. **Parallel Verification**: Could verify multiple batches in parallel (future enhancement)

## Configuration Best Practices

### Environment Variables (.env)

```bash
# Timeseries API Configuration
ASPNETCORE_TimeseriesConfig__ServiceUrl=http://localhost:8080
ASPNETCORE_TimeseriesConfig__FetchBatchSize=100
```

### Environment-Specific Configuration

- **Development**: Use localhost URL (http://localhost:8080), batch size 100
- **Production**: Use production timeseries API URL, tune batch size based on observed exclusion rates

## Error Handling

The federation helper includes comprehensive error handling:

1. **Network Failures**: Falls back to non-federated results
2. **Timeout**: Configurable per-request timeout
3. **Invalid Responses**: Logs errors, continues with available data
4. **No Timeseries API**: Gracefully degrades to content-only filtering

## Monitoring & Logging

Key metrics to monitor:

- **Total Fetched**: Number of sensors fetched from content DB
- **Total Verified**: Number passing timeseries filters
- **Exclusion Rate**: (Fetched - Verified) / Fetched
- **API Call Count**: Number of /verify calls per request
- **Response Time**: End-to-end request time

## Testing

### Unit Tests

```csharp
[Fact]
public async Task FetchAndVerify_WithDatasetFilter_ReturnsOnlyMatchingSensors()
{
    var config = new TimeseriesFilterConfig
    {
        Enabled = true,
        TimeseriesApiBaseUrl = "http://test-api",
        DatasetIds = new List<string> { "dataset1" }
    };

    var helper = new TimeseriesFederationHelper(mockHttpClient, mockLogger, config);
    var result = await helper.FetchAndVerifySensorsAsync(query, 1, 10);

    Assert.Equal(10, result.VerifiedIds.Count);
    Assert.True(result.VerificationPerformed);
}
```

### Integration Tests

1. Populate both databases with test data
2. Query with various timeseries filters
3. Verify results match expected sensors
4. Test pagination edge cases (e.g., all sensors excluded)

## Migration Path

### Phase 1: Deploy Infrastructure ✓
- Federation classes deployed
- No behavior change (filtering disabled by default)

### Phase 2: Enable for Testing
- Add timeseries parameters to ONE controller
- Test with subset of users
- Monitor performance and exclusion rates

### Phase 3: Roll Out
- Enable for all controllers that need it
- Document for API users
- Provide examples in Swagger UI

### Phase 4: Optimize
- Tune batch sizes based on production metrics
- Consider caching strategies
- Add parallel verification if needed

## Extensibility

The federation pattern can be extended to:

1. **Other Controllers**: PoiController, ActivityController, etc.
2. **Other Data Sources**: Weather data, traffic data, etc.
3. **Complex Joins**: Federate across multiple external services
4. **Real-time Updates**: WebSocket-based live filtering

## Troubleshooting

### Issue: All sensors excluded
**Cause**: Filter too restrictive or misconfigured
**Solution**: Check dataset IDs, measurement types exist

### Issue: Slow performance
**Cause**: High exclusion rate requiring many fetches
**Solution**: Increase FetchBatchSize, add content filters to pre-filter

### Issue: Inconsistent results
**Cause**: Timeseries data changes between batches
**Solution**: Consider snapshot isolation or time-based caching

## Summary

This federation approach provides:
- ✅ **Maintainability**: Clean separation of concerns
- ✅ **Configurability**: Per-request configuration via query params
- ✅ **Performance**: Batched verification, configurable tuning
- ✅ **Pluggability**: Easy to add to any controller
- ✅ **Robustness**: Graceful degradation on failures

The pattern successfully bridges the Content and Timeseries services while preserving pagination semantics and maintaining acceptable performance.
