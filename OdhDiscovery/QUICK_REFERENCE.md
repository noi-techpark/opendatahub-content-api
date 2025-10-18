# ODH Content API - Quick Reference Guide

## Entity Overview

| Entity | Primary Endpoint | Description | Count |
|--------|------------------|-------------|-------|
| **Accommodation** | `/v1/Accommodation` | Hotels, B&Bs, apartments, camping | 8 endpoints |
| **Activity & POI** | `/v1/ODHActivityPoi` | Activities, points of interest, tours | 4 endpoints |
| **Event** | `/v1/Event` | Events, exhibitions, performances | 4 endpoints |
| **Event v2** | `/v2/Event` | Newer event format | 2 endpoints |
| **Event Short** | `/v1/EventShort` | Optimized event listings | 6 endpoints |
| **Article** | `/v1/Article` | News, recipes, press releases | 4 endpoints |
| **Venue** | `/v1/Venue` | Event locations and facilities | 4 endpoints |
| **Venue v2** | `/v2/Venue` | Newer venue format | 2 endpoints |
| **Weather** | `/v1/Weather` | Forecasts, snow reports, measurements | 14 endpoints |
| **Webcam** | `/v1/WebcamInfo` | Live webcam feeds | 2 endpoints |
| **Sensor** | `/v1/Sensor` | IoT sensors and timeseries data | 4 endpoints |
| **Location** | `/v1/Location` | Regions, municipalities, fractions | 2 endpoints |
| **Geo** | `/v1/GeoShapes` | Geographic boundaries | 4 endpoints |
| **Tag** | `/v1/Tag` | Generic tags | 2 endpoints |
| **ODH Tag** | `/v1/ODHTag` | ODH-specific taxonomy | 2 endpoints |
| **Common** | `/v1/*` | Datasets, publishers, wines, etc. | 20 endpoints |
| **Metadata** | `/v1/MetaData` | API metadata | 3 endpoints |
| **Announcement** | `/v1/Announcement` | Alerts and announcements | 2 endpoints |

## Quick Start Examples

### Basic List Query
```
GET /v1/Accommodation?pagesize=10&pagenumber=1&language=en
```

### Get Single Entity
```
GET /v1/Accommodation/{id}?language=en
```

### Search by Text
```
GET /v1/Accommodation?searchfilter=mountain&language=en
```

### Filter by Location
```
GET /v1/Accommodation?locfilter=reg.123,tvs.456&language=en
```

### Geographic Search (within radius)
```
GET /v1/Accommodation?latitude=46.624975&longitude=11.369909&radius=5000
```

### Filter by Tags
```
GET /v1/Accommodation?odhtagfilter=tag1,tag2&language=en
```

### Get Only Active Items
```
GET /v1/Accommodation?odhactive=true&active=true
```

### Changed Since Date
```
GET /v1/Accommodation?updatefrom=2025-01-01
```

### Select Specific Fields
```
GET /v1/Accommodation?fields=Id,Shortname,AccoTypeId,Latitude,Longitude
```

### Get Only IDs
```
GET /v1/Accommodation?getasidarray=true
```

## Common Query Patterns

### Pattern 1: Paginated List with Language
```
GET /v1/{entity}?pagesize=20&pagenumber=1&language=en&removenullvalues=true
```
**Use case**: Display entity list in UI with pagination

### Pattern 2: Geographic Discovery
```
GET /v1/{entity}?latitude={lat}&longitude={lng}&radius={meters}&language=en
```
**Use case**: "Find near me" or map-based search

### Pattern 3: Filter by Multiple Criteria
```
GET /v1/{entity}?odhactive=true&odhtagfilter=tag1,tag2&locfilter=mun.123&language=en
```
**Use case**: Advanced filtering with multiple conditions

### Pattern 4: Date Range Events
```
GET /v1/Event?begindate=2025-10-01&enddate=2025-10-31&language=en
```
**Use case**: Event calendar for specific date range

### Pattern 5: Search with Auto-complete
```
GET /v1/{entity}?searchfilter={userInput}&pagesize=10&fields=Id,Shortname
```
**Use case**: Search suggestions as user types

### Pattern 6: Full Details by ID
```
GET /v1/{entity}/{id}?language=en&removenullvalues=true
```
**Use case**: Detail page for specific entity

### Pattern 7: Changed Data Sync
```
GET /v1/{entity}?updatefrom=2025-10-15T00:00:00&odhactive=true
```
**Use case**: Incremental data synchronization

### Pattern 8: Random Selection
```
GET /v1/{entity}?seed=0&pagesize=10&odhactive=true
```
**Use case**: "Discover something new" feature

## Parameter Quick Reference

### Essential Parameters (use in most queries)

| Parameter | Type | Purpose | Example |
|-----------|------|---------|---------|
| `language` | string | UI language | `language=en` |
| `fields` | array | Optimize response | `fields=Id,Shortname,ContactInfos` |
| `removenullvalues` | boolean | Clean response | `removenullvalues=true` |

### Pagination

| Parameter | Type | Default | Purpose |
|-----------|------|---------|---------|
| `pagesize` | integer | 10 | Items per page |
| `pagenumber` | integer | 1 | Current page |

### Filtering

| Parameter | Type | Purpose | Example |
|-----------|------|---------|---------|
| `searchfilter` | string | Text search | `searchfilter=hotel` |
| `rawfilter` | string | Advanced filter | `rawfilter=Active eq true` |
| `odhactive` | boolean | ODH status | `odhactive=true` |
| `active` | boolean | TIC status | `active=true` |
| `odhtagfilter` | string | Tag filter | `odhtagfilter=tag1,tag2` |
| `locfilter` | string | Location filter | `locfilter=mun.123` |
| `updatefrom` | string | Changed since | `updatefrom=2025-01-01` |
| `publishedon` | string | Publisher filter | `publishedon=pub1,pub2` |
| `source` | string | Source filter | `source=lts` |
| `idlist` | string | Multiple IDs | `idlist=id1,id2,id3` |

### Geographic

| Parameter | Type | Purpose | Example |
|-----------|------|---------|---------|
| `latitude` | string | Latitude | `latitude=46.624975` |
| `longitude` | string | Longitude | `longitude=11.369909` |
| `radius` | string | Radius (meters) | `radius=5000` |
| `polygon` | string | Polygon boundary | `polygon=it.municipality.3066` |

### Sorting

| Parameter | Type | Purpose | Example |
|-----------|------|---------|---------|
| `seed` | string | Random sort | `seed=0` (random) or `seed=5` (consistent) |
| `rawsort` | string | Custom sort | `rawsort=Shortname asc` |

### Response Format

| Parameter | Type | Purpose | Example |
|-----------|------|---------|---------|
| `getasidarray` | boolean | Only IDs | `getasidarray=true` |
| `langfilter` | string | Language availability | `langfilter=de,it,en` |

## Entity-Specific Parameters

### Accommodation Filters

```
categoryfilter    - Star/flower/sun ratings (BITMASK)
typefilter        - Hotel/B&B/Farm/Camping/etc (BITMASK)
boardfilter       - Breakfast/half-board/etc (BITMASK)
featurefilter     - Pool/sauna/WLAN/etc (BITMASK)
themefilter       - Gourmet/wellness/family/etc (BITMASK)
badgefilter       - Belvita/Familyhotel/etc (BITMASK)
altitudefilter    - Altitude range (e.g., 500,1000)
```

**Example**: Find 4-star family hotels with pool
```
GET /v1/Accommodation?categoryfilter=2048&badgefilter=2&featurefilter=4&language=en
```

### Activity & POI Filters

```
activitytype      - Activity type
poitype           - POI type
subtype           - Activity subtype
difficultyfilter  - Difficulty level
distancefilter    - Distance/length
durationfilter    - Duration
hasimage          - Has images
highlight         - Featured items
```

**Example**: Find easy hiking trails under 10km
```
GET /v1/ODHActivityPoi?activitytype=hiking&difficultyfilter=easy&distancefilter=0,10000
```

### Event Filters

```
begindate         - Event start date (yyyy-MM-dd)
enddate           - Event end date (yyyy-MM-dd)
eventlocation     - Location filter
topicfilter       - Event topics
orgfilter         - Organization
sort              - Sort order
```

**Example**: Find events this month
```
GET /v1/Event?begindate=2025-10-01&enddate=2025-10-31&odhactive=true&language=en
```

### Weather Parameters

```
datefrom          - Forecast start date
dateto            - Forecast end date
skiareaid         - Specific ski area
skiareafilter     - Ski area filter
areafilter        - Geographic area
extended          - Extended data
```

**Example**: Get week weather forecast
```
GET /v1/Weather/Forecast?datefrom=2025-10-17&dateto=2025-10-24&language=en
```

### Sensor Parameters (Timeseries)

```
datasetid         - Dataset identifier
sensortype        - Type of sensor
manufacturer      - Sensor manufacturer
model             - Sensor model
tsstarttime       - Timeseries start
tsendtime         - Timeseries end
tslatestonly      - Latest only
```

**Example**: Get latest sensor readings
```
GET /v1/Sensor?datasetid=123&tslatestonly=true
```

### Venue Filters

```
categoryfilter    - Venue category
capacityfilter    - Capacity range
featurefilter     - Venue features
roomcountfilter   - Number of rooms
setuptypefilter   - Setup type
```

**Example**: Find venues with 100+ capacity
```
GET /v1/Venue?capacityfilter=100,999999&language=en
```

## BITMASK Filters Explained

Some filters use BITMASK values where each option has a power-of-2 value:

| Value | Meaning | Binary |
|-------|---------|--------|
| 1 | Option 1 | 00001 |
| 2 | Option 2 | 00010 |
| 4 | Option 3 | 00100 |
| 8 | Option 4 | 01000 |
| 16 | Option 5 | 10000 |

**To combine options, add the values:**
- Option 2 + Option 3 = 2 + 4 = 6
- Option 1 + Option 3 + Option 5 = 1 + 4 + 16 = 21

**Example - Accommodation Category Filter:**
```
2 = 1 star
4 = 1 flower
14 = 1 star + 1 flower + 1 sun (2 + 4 + 8)
16 = 2 stars
128 = 3 stars
2048 = 4 stars
32768 = 5 stars
```

## Response Formats

### Standard JSON Response (List)
```json
{
  "TotalResults": 250,
  "TotalPages": 25,
  "CurrentPage": 1,
  "PageSize": 10,
  "Items": [
    { "Id": "...", "Shortname": "...", ... }
  ]
}
```

### Single Entity Response
```json
{
  "Id": "abc123",
  "Active": true,
  "Shortname": "Entity Name",
  "Detail": { "de": {...}, "it": {...}, "en": {...} },
  ...
}
```

### ID Array Response
```json
["id1", "id2", "id3", ...]
```

## Location Filter Format

| Prefix | Type | Example |
|--------|------|---------|
| `reg+` | Region | `locfilter=reg.123` |
| `tvs+` | Tourism Association | `locfilter=tvs.456` |
| `mun+` | Municipality | `locfilter=mun.789` |
| `fra+` | Fraction | `locfilter=fra.012` |

**Multiple locations**: `locfilter=reg.123,mun.456,fra.789`

## Polygon Filter Formats

### WKT (Well-Known Text)
```
polygon=POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))
```

### GeoShapes API Reference
```
polygon=it.municipality.3066
```

### Bounding Box
```
polygon=bbc:lon1,lat1,lon2,lat2,lon3,lat3,lon4,lat4  (contains)
polygon=bbi:lon1,lat1,lon2,lat2,lon3,lat3,lon4,lat4  (intersects)
```

## Raw Filter Examples

The `rawfilter` parameter uses F# query syntax:

### Simple Equality
```
rawfilter=Active eq true
```

### Comparison
```
rawfilter=Altitude gt 1000
```

### String Contains
```
rawfilter=Shortname like 'Hotel%'
```

### Logical AND
```
rawfilter=Active eq true and Altitude gt 1000
```

### Logical OR
```
rawfilter=AccoTypeId eq 'hotel' or AccoTypeId eq 'pension'
```

### Nested Object
```
rawfilter=ContactInfos.City eq 'Bolzano'
```

### Array Contains
```
rawfilter=SmgTags in ['tag1', 'tag2']
```

## Performance Tips

1. **Always use field selection** when you don't need all fields
   ```
   fields=Id,Shortname,ContactInfos
   ```

2. **Enable null value removal** to reduce response size
   ```
   removenullvalues=true
   ```

3. **Use getasidarray** when you only need IDs for a second query
   ```
   getasidarray=true
   ```

4. **Paginate large result sets** instead of fetching everything
   ```
   pagesize=20&pagenumber=1
   ```

5. **Use specific endpoints** instead of filtering
   - Better: `GET /v1/Accommodation/{id}`
   - Worse: `GET /v1/Accommodation?idlist={id}`

6. **Leverage updatefrom** for incremental updates
   ```
   updatefrom=2025-10-15
   ```

7. **Cache geo-sorted results** since they're expensive
   ```
   latitude=46.5&longitude=11.3&radius=10000
   ```

## Common Use Cases

### Use Case 1: Hotel Search App
```
1. List hotels in region:
   GET /v1/Accommodation?locfilter=reg.123&typefilter=1&odhactive=true

2. Filter by amenities:
   Add: &featurefilter=68 (WLAN + Pool = 64 + 4)

3. Show on map:
   Get coordinates via fields=Id,Shortname,Latitude,Longitude

4. Detail page:
   GET /v1/Accommodation/{id}?language=en
```

### Use Case 2: Event Calendar
```
1. Get this month's events:
   GET /v1/Event?begindate=2025-10-01&enddate=2025-10-31&odhactive=true

2. Filter by location:
   Add: &locfilter=mun.456

3. Search by keyword:
   Add: &searchfilter=concert

4. Show event details:
   GET /v1/Event/{id}?language=en
```

### Use Case 3: Activity Finder
```
1. Find activities nearby:
   GET /v1/ODHActivityPoi?latitude=46.5&longitude=11.3&radius=10000

2. Filter by type:
   Add: &activitytype=hiking

3. Filter by difficulty:
   Add: &difficultyfilter=easy

4. Show details with route:
   GET /v1/ODHActivityPoi/{id}?language=en
```

### Use Case 4: Weather Dashboard
```
1. Get today's weather:
   GET /v1/Weather/Forecast?datefrom=2025-10-17&dateto=2025-10-17

2. Get ski conditions:
   GET /v1/Weather/SkiArea/{id}

3. Get snow report:
   GET /v1/Weather/SnowReport

4. Get live measurements:
   GET /v1/Weather/Measurement
```

### Use Case 5: Data Synchronization
```
1. Get list of datasets:
   GET /v1/Dataset

2. Sync changed accommodations:
   GET /v1/Accommodation?updatefrom=2025-10-15&odhactive=true

3. Get only IDs for comparison:
   GET /v1/Accommodation?getasidarray=true

4. Fetch specific changed items:
   GET /v1/Accommodation?idlist=id1,id2,id3
```

## Error Handling

Common HTTP status codes:
- **200**: Success
- **400**: Bad Request (invalid parameters)
- **404**: Not Found (invalid ID or endpoint)
- **500**: Internal Server Error

Validate parameters before sending:
- Date format: `yyyy-MM-dd`
- Boolean values: `true` / `false` (lowercase)
- Null value: `null` (literal string)
- Numeric values: No quotes
- String values: Properly URL-encoded

## Rate Limiting

Check API documentation for:
- Request limits per minute/hour
- Batch request guidelines
- Caching recommendations

## Support Resources

- **Wiki**: https://github.com/noi-techpark/odh-docs/wiki
- **Swagger UI**: https://tourism.opendatahub.com/swagger
- **Issue Tracker**: https://github.com/noi-techpark/odh-api-core

## Next Steps

1. Review **SWAGGER_DOCUMENTATION.md** for complete endpoint details
2. Use **swagger_structured.json** for programmatic access
3. Check **parameter_frequency.json** for parameter usage patterns
4. Read **ANALYSIS_SUMMARY.md** for comprehensive overview
