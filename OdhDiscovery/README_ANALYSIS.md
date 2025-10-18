# ODH Content API - Swagger Analysis

## Overview

This directory contains a comprehensive analysis of all GET operations across 18 entity types in the ODH Content API. The analysis extracted detailed information about 89 endpoints, including parameters, response schemas, and usage patterns.

## Generated Files

### 📖 Documentation (Human-Readable)

1. **[QUICK_REFERENCE.md](./QUICK_REFERENCE.md)** (14 KB)
   - **Start here!** Quick reference guide with examples
   - Common query patterns and use cases
   - Entity overview and parameter cheat sheet
   - BITMASK filter explanations
   - Performance tips and best practices

2. **[SWAGGER_DOCUMENTATION.md](./SWAGGER_DOCUMENTATION.md)** (278 KB)
   - Complete endpoint documentation
   - All parameters with detailed descriptions
   - Response schemas and content types
   - Organized by entity type
   - Includes deprecation warnings

3. **[ANALYSIS_SUMMARY.md](./ANALYSIS_SUMMARY.md)** (14 KB)
   - Executive summary of the analysis
   - Statistics and metrics
   - Common parameter analysis
   - Entity-specific highlights
   - Integration points and relationships

### 💾 Data Files (Machine-Readable)

4. **[swagger_structured.json](./swagger_structured.json)** (540 KB)
   - **Use this for your discovery app!**
   - Clean, structured JSON format
   - Parameters categorized as common vs entity-specific
   - Ready for programmatic consumption
   - Includes priority entity flags

5. **[parameter_frequency.json](./parameter_frequency.json)** (16 KB)
   - Parameter usage statistics
   - Shows which entities use each parameter
   - Useful for UI design decisions
   - Identifies common vs rare parameters

6. **[swagger_analysis.json](./swagger_analysis.json)** (710 KB)
   - Raw analysis data
   - Complete parameter schemas
   - Full response information
   - Backup/reference data

### 🛠️ Scripts

7. **[analyze_swagger.py](./analyze_swagger.py)** (4.2 KB)
   - Python script to extract data from swagger files
   - Parses OpenAPI/Swagger JSON
   - Extracts GET operations and parameters

8. **[generate_docs.py](./generate_docs.py)** (9.9 KB)
   - Generates SWAGGER_DOCUMENTATION.md
   - Creates human-readable markdown
   - Groups endpoints by entity

9. **[generate_structured_output.py](./generate_structured_output.py)** (6.1 KB)
   - Generates swagger_structured.json
   - Categorizes parameters
   - Creates parameter frequency analysis

## Quick Stats

- **18 entities** analyzed
- **89 GET endpoints** documented
- **Common parameters**: 94.4% use `fields`, 92.1% use `language`, 85.4% use `removenullvalues`
- **Top entities by endpoint count**: common (20), weather (14), accommodation (8)

## Entity Types

### Tourism Content
- **Accommodation** (8 endpoints) - Hotels, B&Bs, apartments, camping
- **Activity & POI** (4 endpoints) - Activities, tours, points of interest
- **Event** (4 + 2 + 6 endpoints) - Events in multiple formats
- **Article** (4 endpoints) - News, recipes, press releases
- **Venue** (4 + 2 endpoints) - Event locations and facilities

### Location & Geography
- **Location** (2 endpoints) - Regions, municipalities, fractions
- **Geo** (4 endpoints) - Geographic shapes and boundaries

### Weather & Environment
- **Weather** (14 endpoints) - Forecasts, snow reports, measurements
- **Webcam** (2 endpoints) - Live webcam feeds

### Data Infrastructure
- **Sensor** (4 endpoints) - IoT sensors and timeseries data
- **Tag** (2 + 2 endpoints) - Tags and taxonomy
- **Common** (20 endpoints) - Datasets, publishers, metadata
- **Metadata** (3 endpoints) - API metadata
- **Announcement** (2 endpoints) - Alerts and announcements

## How to Use These Files

### For Building a Discovery Web Application

1. **Start with QUICK_REFERENCE.md**
   - Understand the API structure
   - Learn common query patterns
   - See practical examples

2. **Use swagger_structured.json as your data source**
   ```javascript
   // Load the structured data
   const apiData = require('./swagger_structured.json');

   // Get all entities
   const entities = Object.values(apiData.entities);

   // Get priority entities only
   const priorityEntities = entities.filter(e => e.priority);

   // Get endpoints for accommodation
   const accoEndpoints = apiData.entities.accommodation.endpoints;

   // Get common parameters for an endpoint
   const commonParams = accoEndpoints[0].parameters.common;
   ```

3. **Reference parameter_frequency.json for UI decisions**
   ```javascript
   // See which parameters are most common
   const paramFreq = require('./parameter_frequency.json');

   // Check if a parameter is widely used
   const fieldsUsage = paramFreq.fields.count; // 84 endpoints

   // Get entities that use 'latitude'
   const geoEntities = paramFreq.latitude.entities;
   ```

4. **Consult SWAGGER_DOCUMENTATION.md for details**
   - When implementing specific endpoints
   - To understand parameter constraints
   - For response schema information

### For API Exploration

1. Read **QUICK_REFERENCE.md** for practical examples
2. Try queries in your browser or Postman
3. Check **SWAGGER_DOCUMENTATION.md** for full parameter details
4. Refer to **ANALYSIS_SUMMARY.md** for relationships between entities

### For Development Planning

1. Review **ANALYSIS_SUMMARY.md** for overview
2. Identify common parameters to implement first
3. Use **parameter_frequency.json** to prioritize features
4. Plan entity-specific UIs based on parameter categories

## Common Query Patterns

### Basic List
```
GET /v1/Accommodation?pagesize=10&language=en&removenullvalues=true
```

### Geographic Search
```
GET /v1/Accommodation?latitude=46.5&longitude=11.3&radius=5000
```

### Text Search
```
GET /v1/Accommodation?searchfilter=mountain hotel&language=en
```

### Advanced Filtering
```
GET /v1/Accommodation?odhactive=true&odhtagfilter=tag1,tag2&locfilter=mun.123
```

### Date Range (Events)
```
GET /v1/Event?begindate=2025-10-01&enddate=2025-10-31
```

### Field Selection
```
GET /v1/Accommodation?fields=Id,Shortname,Latitude,Longitude
```

## Key Features Discovered

### Universal Parameters (>80% adoption)
- `fields` - Response field selection
- `language` - Language selection (de/it/en/nl/cs/pl/fr/ru)
- `removenullvalues` - Clean null values

### Powerful Filtering
- `rawfilter` - F# query language for complex filters
- `odhtagfilter` - Taxonomy-based filtering
- `locfilter` - Geographic location filtering
- `polygon` - WKT polygon filtering

### Geographic Capabilities
- Lat/long/radius search with automatic distance sorting
- Polygon and bounding box filters
- GeoShapes API integration

### Performance Features
- Field selection to reduce payload
- ID-only responses (`getasidarray`)
- Incremental updates (`updatefrom`)
- Pagination support

### Advanced Features
- BITMASK filters for multi-select criteria
- Random sorting with seeds
- Multi-language support
- Publisher filtering
- Source filtering

## Data Model Highlights

### Common Patterns
- **Active status**: `active` (TIC) and `odhactive` (ODH)
- **Identifiers**: `Id`, `idlist` for bulk operations
- **Temporal**: `updatefrom` for change tracking
- **Localization**: Multi-language with `Detail` objects
- **Tags**: `SmgTags` array for categorization

### Entity Relationships
- Accommodation ↔ Location (via `locfilter`)
- Event ↔ Venue (via `venueidfilter`)
- All entities ↔ ODH Tags (via `odhtagfilter`)
- All entities ↔ Publishers (via `publishedon`)

## Server Information

- **Base URL**: https://tourism.opendatahub.com
- **API Version**: v1 (primary), v2 (some entities)
- **Swagger UI**: https://tourism.opendatahub.com/swagger

## Source Files Location

Swagger files analyzed from:
```
/home/mroggia/git/opendatahub-content-api/OdhDiscovery/context/content_swaggers/
```

Files analyzed:
- accommodation.json
- odhactivitypoi.json
- event.json, eventv2.json, eventshort.json
- article.json
- venue.json, venuev2.json
- sensor.json
- common.json
- tag.json, odhtag.json
- location.json
- geo.json
- weather.json
- webcaminfo.json
- metadata.json
- announcement.json

## Regenerating the Analysis

If swagger files are updated, regenerate the analysis:

```bash
cd /home/mroggia/git/opendatahub-content-api/OdhDiscovery

# Step 1: Extract data from swagger files
python3 analyze_swagger.py > swagger_analysis.json 2>/dev/null

# Step 2: Generate documentation
python3 generate_docs.py

# Step 3: Generate structured output and frequency analysis
python3 generate_structured_output.py
```

## Next Steps for Discovery Application

### Phase 1: Basic UI
- [ ] Entity selector (18 entities)
- [ ] Endpoint selector per entity
- [ ] Common parameter inputs (language, fields, pagesize)
- [ ] Execute query and display results

### Phase 2: Advanced Filtering
- [ ] Entity-specific parameter forms
- [ ] Geographic map interface (lat/long/radius)
- [ ] Tag browser (using ODHTag endpoints)
- [ ] Location filter UI (region/municipality selection)
- [ ] Date range pickers for temporal queries

### Phase 3: Power Features
- [ ] Raw filter query builder
- [ ] Field selection tree
- [ ] Response preview with formatting
- [ ] Query save/share functionality
- [ ] BITMASK filter helpers

### Phase 4: Polish
- [ ] Parameter validation and hints
- [ ] Auto-complete for search
- [ ] Query history
- [ ] Export results (JSON/CSV)
- [ ] API documentation integration

## Support & References

- **ODH Docs**: https://github.com/noi-techpark/odh-docs/wiki
- **Common Parameters**: https://github.com/noi-techpark/odh-docs/wiki/Common-parameters
- **Raw Filter**: https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api
- **Geo Sorting**: https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage

## Contributing

To add more analysis or improve documentation:

1. Update the Python scripts
2. Regenerate all files
3. Review changes
4. Update this README if needed

## License

This analysis follows the same license as the ODH Content API project.

---

**Generated**: 2025-10-17
**ODH Content API Version**: v1
**Total Endpoints Analyzed**: 89
**Analysis Status**: Complete ✅
