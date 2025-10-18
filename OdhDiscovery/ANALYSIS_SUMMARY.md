# ODH Content API - Swagger Analysis Summary

## Overview

This analysis covers all GET operations across 18 entity types in the ODH Content API swagger specifications located in `/home/mroggia/git/opendatahub-content-api/OdhDiscovery/context/content_swaggers/`.

## Generated Files

1. **SWAGGER_DOCUMENTATION.md** - Comprehensive human-readable documentation of all endpoints
2. **swagger_structured.json** - Machine-readable JSON format for programmatic use
3. **swagger_analysis.json** - Raw analysis data (710 KB)
4. **parameter_frequency.json** - Parameter usage statistics across all endpoints
5. **ANALYSIS_SUMMARY.md** - This summary document

## Key Statistics

- **Total Entities Analyzed**: 18
- **Total GET Endpoints**: 89
- **Server URL**: https://tourism.opendatahub.com (primary)

## Entity Breakdown (by endpoint count)

1. **common** - 20 endpoints (datasets, metaregions, wine, publishers, etc.)
2. **weather** - 14 endpoints (forecast, districts, snow reports, measurements)
3. **accommodation** - 8 endpoints (hotels, pensions, apartments)
4. **eventshort** - 6 endpoints (event listings with date optimization)
5. **odhactivitypoi** - 4 endpoints (activities and points of interest)
6. **event** - 4 endpoints (full event details)
7. **article** - 4 endpoints (news articles, recipes, press releases)
8. **venue** - 4 endpoints (event locations and venues)
9. **sensor** - 4 endpoints (IoT sensor data and timeseries)
10. **geo** - 4 endpoints (geographic shapes and boundaries)
11. **metadata** - 3 endpoints (API metadata)
12. **eventv2** - 2 endpoints (newer event format)
13. **venuev2** - 2 endpoints (newer venue format)
14. **tag** - 2 endpoints (tag management)
15. **odhtag** - 2 endpoints (ODH-specific tags)
16. **location** - 2 endpoints (municipalities, regions, fractions)
17. **webcaminfo** - 2 endpoints (webcam data)
18. **announcement** - 2 endpoints (announcements and alerts)

## Common Parameters (used in 20+ endpoints)

### Universal Parameters (>80% coverage)

- **`fields`** - 84 endpoints (94.4%) - Field selector for response
- **`language`** - 82 endpoints (92.1%) - Language selection (de/it/en/nl/cs/pl/fr/ru)
- **`removenullvalues`** - 76 endpoints (85.4%) - Clean null values from response

### Pagination Parameters (~50% coverage)

- **`pagenumber`** - 45 endpoints (50.6%) - Page number (default: 1)
- **`pagesize`** - 43 endpoints (48.3%) - Items per page (default: 10)

### Search & Filter Parameters (~40-45% coverage)

- **`searchfilter`** - 40 endpoints (44.9%) - Text search across titles
- **`rawfilter`** - 40 endpoints (44.9%) - Advanced filtering using F# query parser
- **`rawsort`** - 40 endpoints (44.9%) - Custom sorting

### Identification Parameters

- **`id`** - 42 endpoints (47.2%) - Single ID lookup
- **`idlist`** - 31 endpoints (34.8%) - Multiple IDs (comma-separated)

### Randomization & Source

- **`seed`** - 35 endpoints (39.3%) - Random sorting (1-10, 0=random, null=disabled)
- **`source`** - 35 endpoints (39.3%) - Data source filter

### Publishing & Updates

- **`publishedon`** - 28 endpoints (31.5%) - Filter by publisher IDs
- **`updatefrom`** - 27 endpoints (30.3%) - Changed after date (yyyy-MM-dd)

### Activity Filters

- **`active`** - 25 endpoints (28.1%) - TIC active status
- **`odhactive`** - 21 endpoints (23.6%) - ODH active status

### Geospatial Parameters (~24-27% coverage)

- **`latitude`** - 24 endpoints (27.0%) - Geographic latitude
- **`longitude`** - 24 endpoints (27.0%) - Geographic longitude
- **`radius`** - 24 endpoints (27.0%) - Search radius in meters
- **`polygon`** - 21 endpoints (23.6%) - WKT polygon or GeoShape reference

### Other Common Parameters

- **`getasidarray`** - 25 endpoints (28.1%) - Return only ID array
- **`langfilter`** - 24 endpoints (27.0%) - Filter by available languages
- **`odhtagfilter`** - 19 endpoints (21.3%) - ODH tag filtering
- **`locfilter`** - 13 endpoints (14.6%) - Location filter (region/municipality/fraction)

## Entity-Specific Highlights

### Accommodation Parameters
- **Category filters**: Star ratings, flower ratings, sun ratings (BITMASK)
- **Type filters**: Hotel/Pension, B&B, Farm, Camping, Youth, Mountain, Apartment (BITMASK)
- **Board filters**: Breakfast, half board, full board, all inclusive (BITMASK)
- **Feature filters**: Swimming pool, sauna, garage, WLAN, barrier-free, pets (BITMASK)
- **Theme filters**: Gourmet, wellness, family, hiking, skiing, etc. (BITMASK)
- **Badge filters**: Belvita, Family hotel, Bike hotel, Red Rooster, etc. (BITMASK)
- **Availability check**: Real-time availability (requires arrival, departure, roominfo, bokfilter)
- **Altitude filter**: Range filter (e.g., 500,1000 for 500-1000m)

### Activity & POI Parameters
- **Activity type**: Specific activity types
- **POI type**: Point of interest categories
- **Difficulty filter**: Activity difficulty levels
- **Distance filter**: Distance/length filters
- **Duration filter**: Time duration filters
- **Category code filter**: Detailed categorization
- **Cuisine code filter**: Restaurant/food categorization
- **Ceremony code filter**: Special events
- **Dish code filter**: Menu categorization
- **Facility code filter**: Facility features
- **Highlight**: Featured/highlighted items
- **Has image**: Filter by image availability
- **Level3 type**: Detailed type classification
- **Subtype**: Activity subtypes

### Event Parameters
- **Begin date / End date**: Date range filtering
- **Start date**: Event start filter
- **Event location**: Location filter
- **Event IDs**: Specific event selection
- **Event grouping**: Group related events
- **Topic filter**: Event topics
- **Org filter**: Organization filter
- **Ranc filter**: RANC classification
- **Sort**: Event sorting options
- **Datetime format**: Date format preference
- **Optimize dates**: Date optimization
- **Only active**: Active events only
- **Community active**: Community event filter
- **Web address**: Website filter
- **Website active**: Website status

### Venue Parameters
- **Category filter**: Venue categories
- **Capacity filter**: Venue capacity range
- **Feature filter**: Venue features
- **Room count filter**: Number of rooms
- **Setup type filter**: Room setup configurations
- **Destination data format**: Data format options

### Weather Parameters
- **Date from / Date to**: Date range
- **Ski area filter**: Ski area selection
- **Ski area ID**: Specific ski area
- **Area filter**: Geographic area
- **Extended**: Extended weather data
- **Lang**: Language for weather text
- **Last change**: Last update timestamp

### Sensor Parameters (Timeseries)
- **Dataset ID**: Timeseries dataset identifier
- **Manufacturer**: Sensor manufacturer
- **Model**: Sensor model
- **Sensor type**: Type of sensor
- **Measurement type name**: Type of measurement
- **TS dataset IDs**: Multiple dataset IDs
- **TS start time**: Timeseries start time
- **TS end time**: Timeseries end time
- **TS latest only**: Latest measurements only
- **TS required types**: Required measurement types
- **TS optional types**: Optional measurement types
- **TS measurement expr**: Measurement expressions

### Tag Parameters
- **Display as category**: Show as category
- **Types**: Tag types
- **Valid for entity**: Entity validation
- **Localization language**: Tag translation language
- **Main entity**: Primary entity filter

### Geographic Parameters
- **Type**: Geographic type (region, municipality, etc.)
- **SRID**: Spatial Reference System ID
- **Show all**: Display all records

### Common Entity Parameters
- **Company ID**: Specific company filter
- **Wine ID**: Wine-related data
- **Visible in search**: Search visibility

## Data Response Patterns

### Common Response Characteristics

1. **Content Types**: Primarily `application/json`, some endpoints support `application/json+hal`, `text/csv`
2. **Response Codes**:
   - 200: Success
   - 400: Bad Request
   - 500: Internal Server Error

### Response Data Structures

Most entity endpoints return:
- **List endpoints**: Array of entities with pagination metadata
- **Single entity endpoints**: Full entity object by ID
- **Changed endpoint**: Entities modified since specific date

### Field Selection

The `fields` parameter allows selecting specific fields to reduce response size:
- Comma-separated list: `fields=Id,Active,Shortname`
- Default (null): All fields returned
- Referenced in Wiki: https://github.com/noi-techpark/odh-docs/wiki/Common-parameters

## Advanced Query Features

### Raw Filter
- F# based query language for complex filtering
- Supports nested object queries
- Boolean logic (AND, OR, NOT)
- Comparison operators (<, >, <=, >=, ==, !=)
- Wiki: https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api

### Raw Sort
- Custom sorting expressions
- Multiple sort criteria
- Ascending/descending order
- Works with rawfilter

### Location Filter (locfilter)
Special syntax for geographic filtering:
- `reg+REGIONID` - Filter by region
- `tvs+TOURISMVEREINID` - Filter by tourism association
- `mun+MUNICIPALITYID` - Filter by municipality
- `fra+FRACTIONID` - Filter by fraction
- Comma-separated for multiple values

### Polygon Filter
Multiple formats supported:
- WKT (Well-Known Text): `POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))`
- GeoShapes API reference: `it.municipality.3066`
- Bounding box: `bbc:` (contains) or `bbi:` (intersects) + coordinate tuples

### Geo Sorting
When latitude, longitude, and radius are provided:
- Results filtered within radius
- Sorted by distance from point
- Random sorting disabled
- Measured in meters

## API Versions & Evolution

### Version Patterns
- **v1**: Original endpoints (most common)
- **v2**: Newer entity formats (eventv2, venuev2)
- **Short formats**: Optimized versions (eventshort)

### Deprecation
Some endpoints marked as deprecated but still functional

## BITMASK Filters

Several accommodation and venue parameters use BITMASK values for filtering:
- Each option has a power-of-2 value (1, 2, 4, 8, 16, etc.)
- Combine values to filter multiple criteria
- Example: CategoryFilter 14 = 1star (2) + 1flower (4) + 1sun (8)
- Null value disables filter

## Language Support

### Language Codes
- **de**: German
- **it**: Italian
- **en**: English
- **nl**: Dutch
- **cs**: Czech
- **pl**: Polish
- **fr**: French
- **ru**: Russian
- **sc**: Ladin (some endpoints)

### Language Parameters
- **`language`**: Selects which language to display (single)
- **`langfilter`**: Filters entities by available languages (comma-separated)
- **`localizationlanguage`**: For tag translations

## Usage Recommendations

### For Discovery Web Application

1. **Start with Common Endpoints**: Use `/v1/Dataset` to list available data types
2. **Implement Field Selection**: Always use `fields` parameter to optimize response size
3. **Handle Pagination**: Most list endpoints support pagination
4. **Use Geo Features**: Implement map-based filtering with lat/long/radius
5. **Support Text Search**: Implement `searchfilter` for user queries
6. **Enable Language Selection**: Support multi-language with `language` parameter
7. **Filter Active Data**: Default to `odhactive=true` for published data
8. **Show Update Times**: Use `updatefrom` for change tracking
9. **Implement Tag Filtering**: Use `odhtagfilter` for categorization
10. **Advanced Users**: Expose `rawfilter` for power users

### Performance Optimization

1. **Use field selection** to reduce payload size
2. **Implement caching** based on `updatefrom` timestamps
3. **Paginate results** for large datasets
4. **Use `getasidarray`** when only IDs needed
5. **Leverage geo-sorting** for location-based queries
6. **Use specific endpoints** (by ID) rather than filtered lists when possible

## Integration Points

### Cross-Entity Relationships

1. **Accommodation ↔ Location**: Via `locfilter`
2. **Event ↔ Venue**: Via `venueidfilter`
3. **All Entities ↔ ODH Tags**: Via `odhtagfilter`
4. **All Entities ↔ Publisher**: Via `publishedon`
5. **Sensor ↔ Dataset**: Via `datasetid`
6. **Location ↔ Geo**: Via geographic filters

### External References

Many filters reference external IDs:
- Region IDs
- Municipality IDs
- Tourism Association IDs
- Fraction IDs
- Ski Area IDs
- Dataset IDs
- Publisher IDs
- ODH Tag IDs

## Documentation References

The API includes extensive wiki documentation:
- Common parameters: https://github.com/noi-techpark/odh-docs/wiki/Common-parameters
- Raw filter/sort: https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api
- Geo sorting: https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage
- Field selection: https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom

## Next Steps for Discovery Application

1. **Read swagger_structured.json** for programmatic entity/endpoint discovery
2. **Implement parameter validation** using the extracted parameter schemas
3. **Build entity-specific UI** based on entity-specific parameters
4. **Create parameter presets** for common query patterns
5. **Implement query builder** for rawfilter/rawsort
6. **Add geographic visualization** for geo-filtered queries
7. **Build date range pickers** for temporal queries
8. **Create tag browser** using ODHTag endpoints
9. **Implement save/share queries** feature
10. **Add API response preview** with field selection

## Files Location

All generated files are in:
```
/home/mroggia/git/opendatahub-content-api/OdhDiscovery/
```

- SWAGGER_DOCUMENTATION.md - Full endpoint documentation
- swagger_structured.json - Structured data for app
- swagger_analysis.json - Raw analysis data
- parameter_frequency.json - Parameter usage stats
- ANALYSIS_SUMMARY.md - This summary
