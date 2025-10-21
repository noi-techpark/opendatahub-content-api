# Getting Started with ODH Data

This document provides an introduction to the Open Data Hub (ODH) tourism and mobility data platform.

## What is ODH?

The Open Data Hub is a comprehensive data platform for South Tyrol (Alto Adige) tourism and mobility data. It provides access to various datasets including:

- **Accommodations**: Hotels, B&Bs, apartments, and other lodging facilities
- **Activities**: Outdoor activities, sports, events, and experiences
- **Gastronomy**: Restaurants, cafes, bars, and food establishments
- **POIs**: Points of interest including attractions, museums, and landmarks
- **Events**: Cultural events, festivals, concerts, and gatherings
- **Timeseries**: Real-time sensor data including parking occupancy, weather, and traffic

## Available Datasets

### Tourism Datasets

Tourism data is accessible through the Content API at `/api/v1/content/{datasetName}`.

Common datasets include:
- `accommodation` - Lodging facilities
- `activity` - Activities and experiences
- `gastronomy` - Food and beverage establishments
- `poi` - Points of interest
- `event` - Events and festivals
- `webcam` - Live webcam feeds
- `museum` - Museums and cultural sites

### Timeseries Data

Sensor measurements are accessible through the Timeseries API at `/api/v1/timeseries`.

Common types include:
- `parking` - Parking occupancy sensors
- `temperature` - Weather temperature sensors
- `traffic` - Traffic flow and speed sensors
- `weather` - Complete weather stations

## Data Relationships

Many tourism entities (from Content API) have associated timeseries data (from Timeseries API).

**Key Pattern**: Entry IDs from Content API are used as sensor names in Timeseries API.

Example:
1. Query `accommodation` dataset to get parking facilities
2. Extract entry IDs from results
3. Use entry IDs as `sensor_names` to query timeseries API
4. Get real-time occupancy data for those parking facilities

## Filtering Data

### Raw Filters

The Content API supports OData-style filtering:

```
rawfilter: "Active eq true"
rawfilter: "Type eq 'Hotel'"
rawfilter: "Active eq true and Type eq 'Hotel'"
```

Common operators:
- `eq` - equals
- `ne` - not equals
- `and` - logical AND
- `or` - logical OR
- `gt`, `lt` - greater than, less than

### Field Projection

Request only the fields you need:

```
fields: ["Id", "Shortname", "Active", "Type"]
```

This reduces response size and improves performance.

## Pagination

All dataset endpoints support pagination:

- `pagenumber`: Page number (starting from 1)
- `pagesize`: Entries per page (max 200)

The response includes:
- `Items`: Array of entries
- `TotalResults`: Total count of matching entries
- `TotalPages`: Total number of pages
- `CurrentPage`: Current page number

## Example Queries

### Get Active Hotels
```
GET /api/v1/content/accommodation
  ?rawfilter=Active eq true and Type eq 'Hotel'
  &pagesize=50
  &fields=Id,Shortname,GpsInfo
```

### Get Latest Parking Occupancy
```
POST /api/v1/timeseries/sensors/latest
{
  "sensor_names": ["parking-1", "parking-2", "parking-3"]
}
```

### Get Temperature Trend
```
POST /api/v1/timeseries/sensors/timeseries
{
  "sensor_names": ["weather-bolzano"],
  "from": "2024-01-01T00:00:00Z",
  "to": "2024-01-07T00:00:00Z",
  "interval": "1h"
}
```

## Best Practices

1. **Use Filters**: Always filter data to reduce response size
2. **Project Fields**: Request only needed fields
3. **Paginate**: Use appropriate page sizes
4. **Cache**: Cache static data locally
5. **Rate Limit**: Be respectful of API rate limits

## Need Help?

Use the chatbot to explore the data! Ask questions like:
- "How many active hotels are there?"
- "Show me restaurants in Bolzano"
- "What's the current parking occupancy?"
- "Find activities near coordinates X, Y"

The chatbot will query the APIs, analyze the data, and show you relevant visualizations in the webapp.
