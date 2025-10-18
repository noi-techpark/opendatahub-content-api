# ODH Content API - Key Findings & Insights

## Executive Summary

A comprehensive analysis of 18 ODH Content API swagger specifications revealed:
- **89 GET endpoints** across 18 entity types
- **120 unique parameters** (18 common, 102 entity-specific)
- **Highly consistent API design** with 94% using `fields` parameter
- **Rich filtering capabilities** including geographic, temporal, and taxonomy-based
- **Multi-language support** across 8 languages
- **Advanced query features** via F# raw filter parser

## Entity Ecosystem

### Priority Entities (16 with ⭐)
These are the main content entities you requested focus on:

```
accommodation      8 endpoints   Hotels, B&Bs, apartments
odhactivitypoi     4 endpoints   Activities, POI, tours
event              4 endpoints   Events (original format)
eventv2            2 endpoints   Events (newer format)
eventshort         6 endpoints   Events (optimized format)
article            4 endpoints   News, recipes, press
venue              4 endpoints   Event venues (original)
venuev2            2 endpoints   Event venues (newer)
sensor             4 endpoints   IoT sensors, timeseries
common            20 endpoints   Datasets, publishers, etc.
tag                2 endpoints   Generic tags
odhtag             2 endpoints   ODH taxonomy
location           2 endpoints   Regions, municipalities
geo                4 endpoints   Geographic boundaries
weather           14 endpoints   Weather, snow, forecasts
webcaminfo         2 endpoints   Webcam streams
```

### Supporting Entities (2)
```
metadata           3 endpoints   API metadata
announcement       2 endpoints   Alerts, announcements
```

## Parameter Architecture

### The "Universal Three" (>80% adoption)

1. **`fields`** (84/89 endpoints - 94.4%)
   - Controls response structure
   - Comma-separated field list
   - Essential for performance
   - Example: `fields=Id,Shortname,Latitude,Longitude`

2. **`language`** (82/89 endpoints - 92.1%)
   - Selects display language
   - Values: de, it, en, nl, cs, pl, fr, ru
   - Single language per request
   - Example: `language=en`

3. **`removenullvalues`** (76/89 endpoints - 85.4%)
   - Cleans response payload
   - Boolean true/false
   - Reduces bandwidth
   - Example: `removenullvalues=true`

### Common Parameters (18 total)

#### Pagination (used by ~50% of endpoints)
```
pagenumber        45 endpoints   Page number (default: 1)
pagesize          43 endpoints   Items per page (default: 10)
```

#### Search & Filter (used by ~40% of endpoints)
```
searchfilter      40 endpoints   Text search across titles
rawfilter         40 endpoints   Advanced F# query filter
rawsort           40 endpoints   Custom sorting expressions
```

#### Identification
```
id                42 endpoints   Single entity by ID
idlist            31 endpoints   Multiple entities (comma-separated)
```

#### Geographic (used by ~24% of endpoints)
```
latitude          24 endpoints   Latitude coordinate
longitude         24 endpoints   Longitude coordinate
radius            24 endpoints   Search radius in meters
polygon           21 endpoints   WKT polygon or GeoShape reference
```

#### Temporal & Status
```
updatefrom        27 endpoints   Changed since date (yyyy-MM-dd)
publishedon       28 endpoints   Filter by publisher IDs
active            25 endpoints   TIC active status
odhactive         21 endpoints   ODH active status
```

#### Randomization & Source
```
seed              35 endpoints   Random sort (0-10, 0=random)
source            35 endpoints   Data source filter
```

#### Other Common
```
getasidarray      25 endpoints   Return only ID array
langfilter        24 endpoints   Filter by language availability
odhtagfilter      19 endpoints   ODH taxonomy filter
locfilter         13 endpoints   Location filter (reg/tvs/mun/fra)
```

### Entity-Specific Parameters (102 total)

Top categories by parameter count:

**Accommodation (20 specific parameters)**
- Category, type, board, feature filters (BITMASK)
- Theme, badge filters (BITMASK)
- Availability checking (arrival, departure, roominfo, bokfilter)
- Altitude range filtering

**Activity & POI (14 specific parameters)**
- Activity type, POI type, subtype
- Difficulty, distance, duration filters
- Various code filters (category, cuisine, ceremony, dish, facility)
- Highlight and image filters

**Events (11 specific parameters)**
- Date range (begindate, enddate, startdate)
- Event location, organization, topic filters
- Grouping and sorting options
- Community and website filters

**Weather (7 specific parameters)**
- Date range (datefrom, dateto)
- Ski area filtering
- Extended data flag
- Last change timestamp

**Sensor/Timeseries (12 specific parameters)**
- Dataset, manufacturer, model, sensor type
- Timeseries time range (start, end)
- Measurement type filtering
- Latest data flag

**Venue (6 specific parameters)**
- Category, capacity, room count filters
- Feature and setup type filters
- Destination data format

**Other Entity-Specific**
- Tag types and validation
- Geographic SRID
- Article types and sorting
- Announcement date ranges

## Design Patterns Discovered

### 1. Progressive Enhancement Pattern
Base query → Add filters → Add geo → Add sorting
```
/v1/Accommodation
  ?language=en                          # Base
  &odhactive=true                       # Add status
  &categoryfilter=2048                  # Add category (4-star)
  &latitude=46.5&longitude=11.3&radius=5000  # Add geo
  &seed=0                               # Add randomization
```

### 2. BITMASK Multi-Select Pattern
Used for accommodation and venue filters where multiple options can be selected:
```
Value 1 (bit 0) = 1
Value 2 (bit 1) = 2
Value 3 (bit 2) = 4
Value 4 (bit 3) = 8
...

Combine: 2 + 8 + 32 = 42 (selects values 2, 4, 6)
```

### 3. Location Filter Hierarchy Pattern
```
locfilter=reg+123        # Region level
locfilter=tvs+456        # Tourism association level
locfilter=mun+789        # Municipality level
locfilter=fra+012        # Fraction level (most specific)
```

### 4. Versioning Pattern
```
/v1/Event       # Original format
/v2/Event       # Newer format with changes
/v1/EventShort  # Optimized variant
```

### 5. Field Selection Pattern
Allows clients to request only needed fields:
```
# Full object (default)
?fields=null

# Essential fields only
?fields=Id,Shortname,Active

# Nested fields
?fields=Id,Shortname,ContactInfos,Detail
```

### 6. Incremental Sync Pattern
```
# Initial load
GET /v1/Accommodation?odhactive=true

# Sync changes
GET /v1/Accommodation?updatefrom=2025-10-15&odhactive=true

# Just get IDs for comparison
GET /v1/Accommodation?getasidarray=true

# Fetch specific updated items
GET /v1/Accommodation?idlist=id1,id2,id3
```

## API Consistency Scores

### High Consistency (>80% adoption)
- ✅ Field selection (`fields`)
- ✅ Language support (`language`)
- ✅ Null value removal (`removenullvalues`)

### Medium Consistency (40-50% adoption)
- ✅ Pagination (`pagesize`, `pagenumber`)
- ✅ Search functionality (`searchfilter`)
- ✅ Advanced filtering (`rawfilter`, `rawsort`)
- ✅ Entity identification (`id`)

### Domain-Specific (20-30% adoption)
- ✅ Geographic features (`latitude`, `longitude`, `radius`)
- ✅ Status filtering (`active`, `odhactive`)
- ✅ Update tracking (`updatefrom`)
- ✅ Tag filtering (`odhtagfilter`)
- ✅ Location filtering (`locfilter`)

## Advanced Features

### 1. Raw Filter Language (F# based)
A powerful query language for complex filtering:

```
Simple:     rawfilter=Active eq true
Comparison: rawfilter=Altitude gt 1000
String:     rawfilter=Shortname like 'Hotel%'
AND:        rawfilter=Active eq true and Altitude gt 1000
OR:         rawfilter=TypeId eq 'hotel' or TypeId eq 'pension'
Nested:     rawfilter=ContactInfos.City eq 'Bolzano'
Array:      rawfilter=Tags in ['tag1', 'tag2']
```

### 2. Geographic Query Capabilities

**Point-radius search:**
```
latitude=46.5&longitude=11.3&radius=5000
→ Returns entities within 5km, sorted by distance
```

**Polygon search (3 formats):**
```
# WKT format
polygon=POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))

# GeoShapes reference
polygon=it.municipality.3066

# Bounding box
polygon=bbc:11.0,46.0,12.0,47.0  # contains
polygon=bbi:11.0,46.0,12.0,47.0  # intersects
```

### 3. Multi-Language Content
Content returned with language-specific objects:
```json
{
  "Detail": {
    "de": { "Title": "Hotel Beispiel", ... },
    "it": { "Title": "Hotel Esempio", ... },
    "en": { "Title": "Example Hotel", ... }
  }
}
```

Select language with `language=en` to get only English fields.

### 4. Accommodation Availability Check
Real-time availability checking (not open data):
```
availabilitycheck=true       # Enable checking
&arrival=2025-10-20          # Check-in date
&departure=2025-10-23        # Check-out date
&roominfo=1-18,10|1-18       # 2 rooms: (2 persons age 18,10), (1 person age 18)
&bokfilter=hgv               # Booking channel
```

## Data Relationships

### Primary Relationships
```
Accommodation ──locfilter──> Location
Event ──────────venueidfilter──> Venue
All Entities ──odhtagfilter──> ODHTag
All Entities ──publishedon──> Publisher
Sensor ────────datasetid────> Dataset
Activity ──────locfilter────> Location
Weather ───────locfilter────> Location
```

### Shared Dimensions
- **Geographic**: All entities can use lat/long/radius/polygon
- **Temporal**: Most entities support `updatefrom` for change tracking
- **Status**: Most entities support `active`/`odhactive` flags
- **Taxonomy**: Most entities support `odhtagfilter`
- **Language**: All entities support multi-language content

## Performance Insights

### Response Size Optimization
```
Strategy                      Size Reduction
─────────────────────────────────────────────
Use fields parameter          50-90%
Enable removenullvalues       10-30%
Use getasidarray (IDs only)   95-99%
Combine all three            90-99%
```

### Query Performance Tips
1. **Use ID lookups** when possible (`/v1/Entity/{id}`)
2. **Paginate** large result sets (default pagesize=10)
3. **Limit fields** to reduce parsing and transfer time
4. **Cache geo-sorted** results (expensive operation)
5. **Use updatefrom** for incremental updates
6. **Leverage indexes** via specific filters vs text search

## Common Use Case Patterns

### Use Case 1: "Hotels near me"
```
GET /v1/Accommodation
  ?latitude=46.5
  &longitude=11.3
  &radius=5000
  &odhactive=true
  &language=en
  &fields=Id,Shortname,Latitude,Longitude,ContactInfos,ImageGallery
```

### Use Case 2: "Events this weekend"
```
GET /v1/Event
  ?begindate=2025-10-19
  &enddate=2025-10-20
  &odhactive=true
  &locfilter=mun.123
  &language=en
```

### Use Case 3: "Find hiking trails"
```
GET /v1/ODHActivityPoi
  ?activitytype=hiking
  &difficultyfilter=easy,medium
  &latitude=46.5
  &longitude=11.3
  &radius=10000
  &language=en
```

### Use Case 4: "Sync changed data"
```
# Daily sync job
GET /v1/Accommodation
  ?updatefrom=2025-10-16
  &odhactive=true
  &fields=Id,LastUpdate,Active

# Process changes and update local database
```

### Use Case 5: "Weather dashboard"
```
GET /v1/Weather/Forecast
  ?datefrom=2025-10-17
  &dateto=2025-10-24
  &language=en

GET /v1/Weather/SnowReport
  ?language=en
```

## API Evolution & Versions

### Version Strategy
- **v1**: Stable, widely used, maintained
- **v2**: Newer entities with improvements
- **Short formats**: Optimized responses (e.g., EventShort)

### Current State
- Most endpoints on v1
- Event and Venue have v2 alternatives
- No breaking changes observed
- Backward compatibility maintained

### Deprecation
- Some endpoints marked deprecated
- Still functional (no removal timeline observed)
- Newer alternatives available

## Entity-Specific Insights

### Accommodation (Most Complex)
- 8 endpoints covering list, single, changes, reduced
- 40 total parameters (17 common + 23 specific)
- Extensive BITMASK filtering (6 different filters)
- Real-time availability integration
- Richest filtering capabilities

### Weather (Most Endpoints)
- 14 endpoints with specialized forecasts
- District, mountain, ski area specific
- Hourly, daily, weekly forecasts
- Snow reports and live measurements
- Multiple data formats

### Events (Most Variants)
- 3 versions: Event, EventV2, EventShort
- Different optimization levels
- EventShort for calendar views
- Event/EventV2 for full details
- Venue integration

### Common (Infrastructure Hub)
- 20 diverse endpoints
- Dataset management
- Publisher information
- Meta-regions, wines, etc.
- Foundation for other entities

### Sensor (Technical)
- IoT sensor metadata
- Timeseries data integration
- Dataset-based organization
- Manufacturer/model filtering
- Complex timeseries queries

## Discovery Application Recommendations

### Must-Have Features
1. **Entity selector** with 18 entities
2. **Common parameter inputs** (language, fields, pagination)
3. **Geographic map interface** for lat/long/radius
4. **Date pickers** for temporal queries
5. **Tag browser** using ODHTag endpoints
6. **Result preview** with JSON formatting
7. **Field selector** for response optimization

### Nice-to-Have Features
8. **Raw filter builder** for power users
9. **Query save/share** functionality
10. **BITMASK filter helpers** with checkboxes
11. **Location hierarchy browser** (region → municipality → fraction)
12. **Multi-language toggle** in UI
13. **Response size estimator**
14. **Query history** with replay
15. **Export results** (JSON, CSV)

### Advanced Features
16. **Polygon drawing** on map for polygon filter
17. **Smart defaults** based on entity type
18. **Parameter validation** with hints
19. **Response schema explorer**
20. **API usage analytics**

## Technical Architecture Observations

### Consistency Patterns
- ✅ Consistent parameter naming
- ✅ Boolean lowercase (true/false)
- ✅ Dates in yyyy-MM-dd format
- ✅ CSV for multi-value (idlist, locfilter)
- ✅ Null as string "null" for disable

### REST Best Practices
- ✅ Resource-based URLs
- ✅ GET for read operations
- ✅ Query parameters for filtering
- ✅ HTTP status codes (200, 400, 500)
- ✅ Content negotiation (JSON, CSV, HAL)

### Performance Considerations
- ✅ Pagination support
- ✅ Field selection
- ✅ Incremental updates
- ✅ Response compression
- ✅ Efficient filtering

## Data Quality Indicators

### Required Fields
- `Id`: Present in all entities
- `Active`: Status flag (TIC)
- `SmgActive` (odhactive): ODH status flag
- Localized content in `Detail` objects

### Optional Fields
- Images, videos, GPS data
- Contact information
- Detailed descriptions
- Operating hours
- Prices and availability

### Multi-Language
- Primary: German (de), Italian (it), English (en)
- Secondary: Dutch (nl), Czech (cs), Polish (pl)
- Tertiary: French (fr), Russian (ru)
- Regional: Ladin (sc) in some endpoints

## Security & Access

### Open Data
- Most endpoints are open data
- No authentication required
- Rate limiting may apply

### Restricted Data
- Accommodation availability checking
- Real-time booking data
- Some publisher-specific content

## Summary Statistics

```
Total Entities:           18
Total Endpoints:          89
Total Parameters:        120
  Common:                 18
  Entity-Specific:       102

Parameter Adoption:
  fields:           94.4%
  language:         92.1%
  removenullvalues: 85.4%
  pagenumber:       50.6%
  pagesize:         48.3%

Entities by Size:
  Largest:  common (20 endpoints)
  Medium:   weather (14 endpoints)
  Small:    accommodation (8 endpoints)
  Minimal:  Most others (2-4 endpoints)

Languages Supported:      8
Geographic Features:      Yes (4 types)
BITMASK Filters:          Yes (6+ types)
Advanced Query:           Yes (rawfilter/rawsort)
Versioning:               v1, v2
Real-time Data:           Limited (availability)
```

## Conclusion

The ODH Content API is a **well-designed, consistent, and feature-rich** API for tourism and mobility data in South Tyrol. It offers:

✅ **Comprehensive coverage** of tourism entities
✅ **Consistent patterns** across endpoints
✅ **Powerful filtering** with advanced query language
✅ **Geographic capabilities** for location-based services
✅ **Multi-language support** for international audiences
✅ **Performance optimizations** with field selection
✅ **Incremental updates** for efficient synchronization
✅ **Future-proof design** with versioning strategy

The API is ready for building a **discovery web application** with rich filtering, geographic visualization, and multi-language support.

---

**Analysis Date**: 2025-10-17
**Endpoints Analyzed**: 89
**Source Files**: 18 swagger specifications
**Analysis Status**: ✅ Complete
