# CONTENT API CONTEXT - LLM INSTRUCTIONS

## OVERVIEW

The Content API (ODH Tourism API) serves static content and metadata about tourism entities in South Tyrol. It provides comprehensive information about accommodations, activities, events, articles, venues, weather, and more.

**Base API Information:**
- **Primary Server URL**: `https://tourism.opendatahub.com`
- **Local Sensor Server**: `http://localhost:8082` (for sensor endpoints)
- **Base Path**: `/v1`
- **Version**: `v1`
- **Content Type**: `application/json`

---

## COMMON QUERY PARAMETERS

Most endpoints in the Content API support these common parameters for filtering, sorting, and field selection.

### Field Selection (`fields`)

Select specific fields to include in the response. The `Id` field is always included automatically.

**Syntax:**
- Single field: `fields=Shortname`
- Multiple fields: `fields=Shortname,Detail,ImageGallery` (comma-separated)
- Nested object fields: `fields=Detail.en.Title` (dot notation)
- Array elements: `fields=ODHTags.[0]` (first element)
- Array element properties: `fields=ODHTags.[0].Id`
- All array element properties: `fields=ODHTags.[*].Id` (returns array)

**Examples:**
```
?fields=Shortname                    # Include Shortname field
?fields=Detail                       # Include entire Detail object
?fields=Detail.en.Title              # Include only English title
?fields=Detail,ImageGallery          # Include multiple fields
?fields=ODHTags.[0].Id               # First tag's ID
?fields=ODHTags.[*].Id               # All tag IDs as array
```

### Language Parameters

**`language`** - Language selector (crops JSON to show only selected language)
- Only one language supported per request
- Examples: `language=en`, `language=de`, `language=it`

**`langfilter`** - Language filter (returns only content available in the specified language)
- Filters by the `HasLanguage` field
- Examples: `langfilter=en` (only returns items with English content)

### Search Filter (`searchfilter`)

Search through **title fields** of the dataset.
- Searches all available languages if no language parameter is provided
- Can also search by ID (automatically detects and searches matching IDs)
- Recommended to add `language` parameter for better performance

**Examples:**
```
?searchfilter=hotel&language=en      # Search for 'hotel' in English titles
?searchfilter=ABC123                 # Search by ID
```

### Pagination

**`pagenumber`** (integer, default: 1)
- Page number for pagination

**`pagesize`** (integer, default: 10)
- Number of elements per page
- Note: Some endpoints may ignore pagesize when certain filters are active

### Other Common Parameters

**`removenullvalues`** (boolean, default: false)
- Remove all `null` values from JSON output
- Useful for reducing response size

**`updatefrom`** (date, format: yyyy-MM-dd)
- Get data updated after the specified date
- Example: `updatefrom=2024-01-01`

**`seed`** (string)
- Random sorting seed
- Values: '1-10' for specific seed, '0' generates random seed, 'null' disables random sorting
- Default: null (disabled)

---

## CUSTOM FILTERING AND SORTING

### Raw Filter (`rawfilter`)

Custom filtering allows you to filter data on fields not covered by predefined filters.

**Syntax:** `?rawfilter=<filter_expression>`

**Operators:**
- `eq(field, value)` - Equal to
- `ne(field, value)` - Not equal to
- `gt(field, value)` - Greater than
- `ge(field, value)` - Greater than or equal
- `lt(field, value)` - Less than
- `le(field, value)` - Less than or equal
- `like(field, value)` - String pattern matching
- `in(field, value)` - Value in array
- `nin(field, value)` - Value not in array
- `likein(field, value)` - Like pattern in array
- `isnull(field)` - Field is NULL
- `isnotnull(field)` - Field is not NULL
- `and(condition1, condition2, ...)` - Logical AND
- `or(condition1, condition2, ...)` - Logical OR

**Field Syntax:**
- Flat field: `Active`, `Type`
- Nested field: `Detail.ru.Title` (dot notation)
- Array element: `Features.[0].Id` (specific index)
- Array element: `Features.[n].Id` (nth element)
- All array elements: `Features.[*].Id` or `Features.[].Id`

**Value Types:**
- Boolean: `true`, `false`
- Number: `1`, `1.12` (always interpreted as floating point)
- String: `'value'` or `"value"` (must be quoted)
- Empty array: `[]`

**Examples:**
```javascript
// All active entries
?rawfilter=eq(Active,true)

// Entries of specific type
?rawfilter=eq(Type,'Wandern')

// Type name contains 'ander'
?rawfilter=like(Type,'ander')

// Empty array check
?rawfilter=eq(ODHTags,[])

// Field exists (not null)
?rawfilter=isnotnull(Detail.ru.Title)

// Altitude range
?rawfilter=and(ge(GpsInfo.0.Altitude,200),le(GpsInfo.0.Altitude,400))

// Array contains specific ID
?rawfilter=in(Features.[*].Id,'a3067617-771a-4b84-b85e-206e5cf4402b')

// Array does NOT contain ID
?rawfilter=nin(Features.[].Id,'a3067617-771a-4b84-b85e-206e5cf4402b')

// Array element like pattern
?rawfilter=likein(Tags.[*].Id,'inter')

// Non-empty array
?rawfilter=ne(Features,[])

// At least one feature exists
?rawfilter=isnotnull(Features.0)

// Complex AND/OR logic
?rawfilter=and(eq(Active,true),or(eq(Type,'Hotel'),eq(Type,'Pension')))
```

**Important Notes:**
- NULL fields require special syntax: use `isnull()` or `isnotnull()`
- To avoid NULL casting errors, use `isnotnull()` condition first:
  ```
  ?rawfilter=and(isnotnull(DisplayAsCategory),eq(DisplayAsCategory,true))
  ```
- ImageGallery field: Anonymous access applies transformer that filters non-CC0 images (rawfilter may produce unexpected results)
- No automatic type conversion - `eq(Active,1)` will fail on boolean field with error: `22023: cannot cast jsonb boolean to type double precision`

### Raw Sort (`rawsort`)

Custom sorting allows you to sort results by any field.

**Syntax:** `?rawsort=<field>[,<field>,...]`

- Ascending: `<field>` (e.g., `Detail.de.Title`)
- Descending: `-<field>` (prepend with minus, e.g., `-Geo.0.Altitude`)
- Multiple fields: comma-separated (e.g., `-Geo.0.Altitude,Detail.de.Title`)

**Field Syntax:** Same as rawfilter (dot notation, array indices)

**Examples:**
```
?rawsort=Detail.de.Title               # Sort by German title (ascending)
?rawsort=-Geo.0.Altitude               # Sort by altitude (descending)
?rawsort=-Geo.0.Altitude,Detail.de.Title  # Sort by altitude desc, then title asc
```

**Note:** Active geofilter (latitude/longitude/radius) overwrites rawsort parameter

---

## GEOGRAPHIC FILTERING

### Geo Sorting / Radius Filter

Filter and sort results by geographic proximity to a point.

**Parameters:**
- `latitude` (decimal) - GPS latitude (e.g., '46.624975')
- `longitude` (decimal) - GPS longitude (e.g., '11.369909')
- `radius` (integer) - Search radius in meters

**Behavior:**
- Returns only objects within the specified point and radius
- Results are automatically sorted by distance from the point
- Active geofilter overwrites any `rawsort` parameter

**Example:**
```
?latitude=46.624975&longitude=11.369909&radius=2000
```

### Polygon Filter

Filter results by geographic polygon or bounding box.

**Parameter:** `polygon`

**Format 1: GeoShapes API Reference**
```
?polygon=Country.Type.Id
?polygon=Country.Type.Name

# Example:
?polygon=it.municipality.Bolzano/Bozen
```
*Check available shapes: https://tourism.api.opendatahub.com/v1/GeoShapes*

**Format 2: WKT (Well-Known Text)**
```
# POLYGON
?polygon=POLYGON((11.026805 46.688285,11.083110 46.690169,11.081394 46.660723,11.035045 46.655775,11.026805 46.688285))

# LINESTRING
?polygon=LINESTRING(11.026805 46.688285,11.083110 46.690169,11.081394 46.660723,11.035045 46.655775,11.026805 46.688285)

# MULTIPOLYGON
?polygon=MULTIPOLYGON(((11.026805 46.688285,11.083110 46.690169,11.081394 46.660723,11.035045 46.655775,11.026805 46.688285)),((11.483016 46.537154,11.582580 46.517785,11.557174 46.481863,11.483016 46.537154)))

# With custom SRID
?polygon=POLYGON((11.026805 46.688285,11.083110 46.690169,11.081394 46.660723,11.026805 46.688285));SRID=4326
```

**Format 3: BBC/BBI Syntax (from Timeseries API)**
```
# BBC format
?polygon=bbc(11.026805 46.688285,11.083110 46.690169,11.081394 46.660723,11.035045 46.655775,11.026805 46.688285,11.026805 46.688285)

# With custom SRID
?polygon=bbc(11.026805 46.688285,11.083110 46.690169,11.081394 46.660723,11.035045 46.655775,11.026805 46.688285,11.026805 46.688285);SRID=4326
```

**Important:** Large polygons may hit GET parameter max length limit

### Location Filter (`locfilter`)

Filter by predefined location/region IDs.

**Get available locations:**
```
GET https://tourism.opendatahub.com/v1/Location?showall=true
```

**Usage:** Use `typ` + `id` from location response
```
# Single location
?locfilter=tvs522822D451CA11D18F1400A02427D15E

# Multiple locations (OR logic)
?locfilter=tvs5228229B51CA11D18F1400A02427D15E,mun99A8B1D4A8D64303B1B965AA7C20FA60,fra79CBD63151C911D18F1400A02427D15E
```

**Examples:**
```
# Get all ODHActivityPois in tourism area Ritten
GET /v1/ODHActivityPoi?locfilter=tvs522822D451CA11D18F1400A02427D15E

# Get accommodations in multiple locations
GET /v1/Accommodation?locfilter=tvs5228229B51CA11D18F1400A02427D15E,mun99A8B1D4A8D64303B1B965AA7C20FA60
```

---

## COMMON RESPONSE STRUCTURES

### Paginated List Response

Most list endpoints return a paginated response with this structure:

```json
{
  "TotalResults": <integer>,
  "TotalPages": <integer>,
  "CurrentPage": <integer>,
  "PreviousPage": <string?>,      // URL to previous page or null
  "NextPage": <string?>,          // URL to next page or null
  "Seed": <string?>,              // Random seed if used
  "Items": [<entity_objects>]     // Array of actual data items
}
```

### Single Item Response

Endpoints that return a single item by ID typically return the entity object directly (not wrapped).

---

## API ENDPOINTS BY ENTITY

### ACCOMMODATION

**Server:** `https://tourism.opendatahub.com`

#### GET Accommodation List
**Endpoint:** `GET /v1/Accommodation`
**Operation ID:** `AccommodationList`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No | Pagenumber (default: 1) |
| pagesize | integer | query | No | Elements per Page (If availabilitycheck set, pagesize has no effect all Accommodations are returned), (default:10) |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| categoryfilter | string | query | No | Categoryfilter BITMASK values: 1 = (not categorized), 2 = (1star), 4 = (1flower), 8 = (1sun), 14 = (1star/1flower/1sun), 16 = (2stars), 32 = (2flowers), 64 = (2suns), 112 = (2stars/2flowers/2suns), 128 = (3stars), 256 = (3flowers), 512 = (3suns), 1024 = (3sstars), 1920 = (3stars/3flowers/3suns/3sstars), 2048 = (4stars), 4096 = (4flowers), 8192 = (4suns), 16384 = (4sstars), 30720 = (4stars/4flowers/4suns/4sstars), 32768 = (5stars), 65536 = (5flowers), 131072 = (5suns), 229376 = (5stars/5flowers/5suns), 'null' = (No Filter), (default:'null') |
| typefilter | string | query | No | Typefilter BITMASK values: 1 = (HotelPension), 2 = (BedBreakfast), 4 = (Farm), 8 = (Camping), 16 = (Youth), 32 = (Mountain), 64 = (Apartment), 128 = (Not defined),'null' = (No Filter), (default:'null') |
| boardfilter | string | query | No | Boardfilter BITMASK values: 0 = (all boards), 1 = (without board), 2 = (breakfast), 4 = (half board), 8 = (full board), 16 = (All inclusive), 'null' = (No Filter), (default:'null') |
| featurefilter | string | query | No | FeatureFilter BITMASK values: 1 = (Group-friendly), 2 = (Meeting rooms), 4 = (Swimming pool), 8 = (Sauna), 16 = (Garage), 32 = (Pick-up service), 64 = (WLAN), 128 = (Barrier-free), 256 = (Special menus for allergy sufferers), 512 = (Pets welcome), 'null' = (No Filter), (default:'null') |
| featureidfilter | string | query | No | Feature Id Filter, LIST filter over ALL Features available. Separator ',' List of Feature IDs, 'null' = (No Filter), (default:'null') |
| themefilter | string | query | No | Themefilter BITMASK values: 1 = (Gourmet), 2 = (At altitude), 4 = (Regional wellness offerings), 8 = (on the wheels), 16 = (With family), 32 = (Hiking), 64 = (In the vineyards), 128 = (Urban vibe), 256 = (At the ski resort), 512 = (Mediterranean), 1024 = (In the Dolomites), 2048 = (Alpine), 4096 = (Small and charming), 8192 = (Huts and mountain inns), 16384 = (Rural way of life), 32768 = (Balance), 65536 = (Christmas markets), 131072 = (Sustainability), 'null' = (No Filter), (default:'null') |
| badgefilter | string | query | No | BadgeFilter BITMASK values: 1 = (Belvita Wellness Hotel), 2 = (Familyhotel), 4 = (Bikehotel), 8 = (Red Rooster Farm), 16 = (Barrier free certificated), 32 = (Vitalpina Hiking Hotel), 64 = (Private Rooms in South Tyrol), 128 = (Vinum Hotels), 'null' = (No Filter), (default:'null') |
| idfilter | string | query | No | IDFilter LIST Separator ',' List of Accommodation IDs, 'null' = (No Filter), (default:'null') |
| locfilter | string | query | No | Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a> |
| altitudefilter | string | query | No | Altitude Range Filter SPECIAL (Separator ',' example Value: 500,1000 Altitude from 500 up to 1000 metres), (default:'null') |
| odhtagfilter | string | query | No | ODHTag Filter LIST (refers to Array SmgTags) (String, Separator ',' more ODHTags possible, 'null' = No Filter, available ODHTags reference to 'v1/ODHTag?validforentity=accommodation'), (default:'null') |
| source | string | query | No |  |
| odhactive | boolean | query | No | ODHActive Filter BOOLEAN (refers to field SmgActive) (possible Values: 'null' Displays all Accommodations, 'true' only ODH Active Accommodations, 'false' only ODH Disabled Accommodations), (default:'null') |
| active | boolean | query | No | TIC Active Filter BOOLEAN (possible Values: 'null' Displays all Accommodations, 'true' only TIC Active Accommodations, 'false' only TIC Disabled Accommodations), (default:'null') |
| bookablefilter | boolean | query | No |  |
| arrival | string | query | No | Arrival DATE (yyyy-MM-dd) REQUIRED ON Availabilitycheck = true, (default:'Today's date') |
| departure | string | query | No | Departure DATE (yyyy-MM-dd) REQUIRED ON Availabilitycheck = true, (default:'Tomorrow's date') |
| roominfo | string | query | No | Roominfo Filter REQUIRED ON Availabilitycheck = true (Splitter for Rooms '\|' Splitter for Persons Ages ',') (Room Types: 0=notprovided, 1=room, 2=apartment, 4=pitch/tent(onlyLTS), 8=dorm(onlyLTS)) possible Values Example 1-18,10\|1-18 = 2 Rooms, Room 1 for 2 person Age 18 and Age 10, Room 2 for 1 Person Age 18), (default:'1-18,18') (default: 1-18,18) |
| bokfilter | string | query | No | Booking Channels Filter REQUIRED ON Availabilitycheck = true (Separator ',' possible values: hgv = (Booking Südtirol), htl = (Hotel.de), exp = (Expedia), bok = (Booking.com), lts = (LTS Availability check)), (default:'hgv') (default: hgv) |
| msssource | string | query | No | Source for MSS availability check, (default:'sinfo') (default: sinfo) |
| availabilitychecklanguage | string | query | No | Language of the Availability Response (possible values: 'de','it','en') (default: en) |
| detail | string | query | No | Detail of the Availablity check (string, 1 = full Details, 0 = basic Details (default)) (default: 0) |
| availabilitycheck | boolean | query | No | Availability Check BOOLEAN (possible Values: 'true', 'false), (default Value: 'false') NOT AVAILABLE AS OPEN DATA, IF Availabilty Check is true certain filters are Required |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| language | string | query | No | Language field selector, displays data and fields in the selected language, possible values: 'de\|it\|en\|nl\|cs\|pl\|fr\|ru' only one language supported (default:'null' all languages are displayed) |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `AccommodationV2JsonResult`*

---

#### GET Accommodation Single
**Endpoint:** `GET /v1/Accommodation/{id}`
**Operation ID:** `SingleAccommodation`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the Accommodation |
| idsource | string | query | No | ID Source Filter (possible values:'lts','hgv','a0r_id'), (default:'lts') (default: lts) |
| availabilitychecklanguage | string | query | No | Language of the Availability Response (possible values: 'de','it','en') (default: en) |
| boardfilter | string | query | No | Boardfilter BITMASK values: 0 = (all boards), 1 = (without board), 2 = (breakfast), 4 = (half board), 8 = (full board), 16 = (All inclusive), 'null' = (No Filter), (default:'null') |
| arrival | string | query | No | Arrival Date (yyyy-MM-dd) REQUIRED, (default:'Today') |
| departure | string | query | No | Departure Date (yyyy-MM-dd) REQUIRED, (default:'Tomorrow') |
| roominfo | string | query | No | Roominfo Filter REQUIRED (Splitter for Rooms '\|' Splitter for Persons Ages ',') (Room Types: 0=notprovided, 1=room, 2=apartment, 4=pitch/tent(onlyLTS), 8=dorm(onlyLTS)) possible Values Example 1-18,10\|1-18 = 2 Rooms, Room 1 for 2 person Age 18 and Age 10, Room 2 for 1 Person Age 18), (default:'1-18,18') (default: 1-18,18) |
| bokfilter | string | query | No | Booking Channels Filter REQUIRED (Separator ',' possible values: hgv = (Booking Südtirol), htl = (Hotel.de), exp = (Expedia), bok = (Booking.com), lts = (LTS Availability check)), (default:'hgv') (default: hgv) |
| msssource | string | query | No |  (default: sinfo) |
| availabilitycheck | boolean | query | No | Availability Check enabled/disabled (possible Values: 'true', 'false), (default Value: 'false') NOT AVAILABLE AS OPEN DATA |
| detail | string | query | No | Detail of the Availablity check (string, 1 = full Details, 0 = basic Details (default)) (default: 0) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Review": {
    ...
  },
  "OperationSchedule": [],
  "RatePlan": [],
  "TrustYouID": <string?>,
  "TrustYouScore": <number(double)?>,
  "TrustYouResults": <integer(int32)?>,
  "TrustYouActive": <boolean?>,
  "TrustYouState": <integer(int32)?>,
  "HasApartment": <boolean>,
  "HasRoom": <boolean?>,
  "IsCamping": <boolean?>,
  "IsGastronomy": <boolean?>,
  "IsBookable": <boolean>,
  "IsAccommodation": <boolean?>,
  "TVMember": <boolean?>,
  "Tags": [],
  "TagIds": [],
  "Self": <string?>,
  "OdhActive": <boolean>,
  "ODHTags": [],
  "AccoBoards": [],
  "AccoBadges": [],
  "AccoThemes": [],
  "AccoSpecialFeatures": [],
  "Features": [],
  "AccoRoomInfo": [],
  "GpsInfo": [],
  "Gpstype": <string?>,
  "Latitude": <number(double)>,
  "Longitude": <number(double)>,
  "Altitude": <number(double)?>,
  "AltitudeUnitofMeasure": <string?>,
  "GpsPoints": {
    ...
  },
  "Id": <string?>,
  "Active": <boolean>,
  "HgvId": <string?>,
  "Shortname": <string?>,
  "Representation": <integer(int32)?>,
  "SmgActive": <boolean>,
  "TourismVereinId": <string?>,
  "MainLanguage": <string?>,
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "AccoCategoryId": <string?>,
  "AccoTypeId": <string?>,
  "DistrictId": <string?>,
  "BoardIds": [],
  "MarketingGroupIds": [],
  "BadgeIds": [],
  "ThemeIds": [],
  "SpecialFeaturesIds": [],
  "AccoDetail": {
    ...
  },
  "AccoBookingChannel": [],
  "ImageGallery": [],
  "GastronomyId": <string?>,
  "SmgTags": [],
  "HasLanguage": [],
  "MssResponseShort": [],
  "PublishedOn": [],
  "Source": <string?>,
  "Mapping": {
    ...
  }
}
```

*Schema: `AccommodationV2`*

---

#### GET Accommodation Types List
**Endpoint:** `GET /v1/AccommodationTypes`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields in the selected language, possible values: 'de\|it\|en\|nl\|cs\|pl\|fr\|ru' only one language supported (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No |  |
| pagesize | integer | query | No |  |
| idlist | string | query | No |  |
| seed | string | query | No |  |
| type | string | query | No | Type to filter for ('Board','Type','Theme','Category','Badge','SpecialFeature') |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET Accommodation Types Single
**Endpoint:** `GET /v1/AccommodationTypes/{id}`
**Operation ID:** `SingleAccommodationTypes`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the AccommodationType |
| language | string | query | No | Language field selector, displays data and fields in the selected language, possible values: 'de\|it\|en\|nl\|cs\|pl\|fr\|ru' only one language supported (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Id": <string?>,
  "Bitmask": <integer(int32)>,
  "Type": <string?>,
  "Key": <string?>,
  "TypeDesc": {
    ...
  },
  "CustomId": <string?>
}
```

*Schema: `AccoTypes`*

---

#### GET Accommodation Feature List (LTS Features)
**Endpoint:** `GET /v1/AccommodationFeatures`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields in the selected language, possible values: 'de\|it\|en\|nl\|cs\|pl\|fr\|ru' only one language supported (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No |  |
| pagesize | integer | query | No |  |
| idlist | string | query | No |  |
| seed | string | query | No |  |
| ltst0idfilter | string | query | No | Filtering by LTS T0ID, filter behaviour is "startswith" so it is possible to send only one character, (default: blank) |
| source | string | query | No | IF source = "lts" the Features list is returned in XML Format directly from LTS, (default: blank) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET Accommodation Feature Single (LTS Features)
**Endpoint:** `GET /v1/AccommodationFeatures/{id}`
**Operation ID:** `SingleAccommodationFeatures`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the AccommodationFeature |
| language | string | query | No | Language field selector, displays data and fields in the selected language, possible values: 'de\|it\|en\|nl\|cs\|pl\|fr\|ru' only one language supported (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "ClusterId": <string?>,
  "ClusterCustomId": <string?>,
  "Id": <string?>,
  "Bitmask": <integer(int32)>,
  "Type": <string?>,
  "Key": <string?>,
  "TypeDesc": {
    ...
  },
  "CustomId": <string?>
}
```

*Schema: `AccoFeatures`*

---

#### GET Accommodation Room Info by Accommodation
**Endpoint:** `GET /v1/AccommodationRoom`
**Operation ID:** `AccommodationRoomList`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| accoid | string | query | No | Accommodation ID |
| idsource | string | query | No | HGV ID or LTS ID of the Accommodation (possible values:'lts','hgv','a0r_id'), (default:'lts') (default: lts) |
| source | string | query | No | Source Filter (possible values:'lts','hgv'), (default:null) |
| pagenumber | integer (int32) | query | No |  |
| pagesize | integer | query | No |  |
| idlist | string | query | No |  |
| getall | boolean | query | No | Get Rooms from all sources (If an accommodation is bookable on Booking Southtyrol, rooms from this source are returned, setting getall to true returns also LTS Rooms), (default:false) (default: False) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| seed | string | query | No |  |
| publishedon | string | query | No |  |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET Accommodation Room Info Single
**Endpoint:** `GET /v1/AccommodationRoom/{id}`
**Operation ID:** `SingleAccommodationRoom`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | AccommodationRoom ID |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "Features": [],
  "Id": <string?>,
  "Shortname": <string?>,
  "A0RID": <string?>,
  "Roomtype": <string?>,
  "AccoRoomDetail": {
    ...
  },
  "ImageGallery": [],
  "HasLanguage": [],
  "LTSId": <string?>,
  "HGVId": <string?>,
  "Source": <string?>,
  "RoomCode": <string?>,
  "Roommax": <integer(int32)?>,
  "Roommin": <integer(int32)?>,
  "Roomstd": <integer(int32)?>,
  "PriceFrom": <number(double)?>,
  "RoomQuantity": <integer(int32)?>,
  "RoomNumbers": [],
  "RoomClassificationCodes": <integer(int32)?>,
  "RoomtypeInt": <integer(int32)?>,
  "LastChange": <string(date-time)?>,
  "FirstImport": <string(date-time)?>,
  "PublishedOn": [],
  "Mapping": {
    ...
  },
  "Active": <boolean>
}
```

*Schema: `AccommodationRoomLinked`*

---

### ACTIVITIES & POINTS OF INTEREST (POI)

**Server:** `https://tourism.opendatahub.com`

#### GET ODHActivityPoi List
**Endpoint:** `GET /v1/ODHActivityPoi`
**Operation ID:** `GetODHActivityPoiList`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No | Pagenumber (default: 1) |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| type | string | query | No | Type of the ODHActivityPoi ('null' = Filter disabled, possible values: BITMASK: 1 = Wellness, 2 = Winter, 4 = Summer, 8 = Culture, 16 = Other, 32 = Gastronomy, 64 = Mobility, 128 = Shops and services), (default: 255 == ALL), refers to <a href="https://tourism.opendatahub.com/v1/ODHActivityPoiTypes?rawfilter=eq(Type,%27Type%27)" target="_blank">ODHActivityPoi Types</a>, Type: Type (default: 255) |
| activitytype | string | query | No | Filtering by Activity Type defined by LTS ('null' = Filter disabled, possible values: BITMASK: 'Mountains = 1','Cycling = 2','Local tours = 4','Horses = 8','Hiking = 16','Running and fitness = 32','Cross-country ski-track = 64','Tobbogan run = 128','Slopes = 256','Lifts = 512'), (default:'1023' == ALL), , refers to <a href="https://tourism.opendatahub.com/v1/ActivityTypes?rawfilter=eq(Type,%27Type%27)" target="_blank">ActivityTypes</a>, Type: Type |
| poitype | string | query | No | Filtering by Poi Type defined by LTS ('null' = Filter disabled, possible values: BITMASK 'Doctors, Pharmacies = 1','Shops = 2','Culture and sights= 4','Nightlife and entertainment = 8','Public institutions = 16','Sports and leisure = 32','Traffic and transport = 64', 'Service providers' = 128, 'Craft' = 256, 'Associations' = 512, 'Companies' = 1024), (default:'2047' == ALL), , refers to <a href="https://tourism.opendatahub.com/v1/PoiTypes?rawfilter=eq(Type,%27Type%27)" target="_blank">PoiTypes</a>, Type: Type |
| subtype | string | query | No | Subtype of the ODHActivityPoi ('null' = Filter disabled, BITMASK Filter, available SubTypes depends on the selected Maintype) <a href="https://tourism.opendatahub.com/v1/ODHActivityPoiTypes?rawfilter=eq(Type,%27SubType%27)" target="_blank">ODHActivityPoi SubTypes</a>, or <a href="https://tourism.opendatahub.com/v1/ActivityTypes?rawfilter=eq(Type,%27SubType%27)" target="_blank">Activity SubTypes</a>, or <a href="https://tourism.opendatahub.com/v1/PoiTypes?rawfilter=eq(Type,%27SubType%27)" target="_blank">Poi SubTypes</a>, Type: SubType |
| level3type | string | query | No | Additional Type of Level 3 the ODHActivityPoi ('null' = Filter disabled, BITMASK Filter, available SubTypes depends on the selected Maintype, SubType reference to ODHActivityPoiTypes) |
| idlist | string | query | No | IDFilter (Separator ',' List of ODHActivityPoi IDs), (default:'null') |
| locfilter | string | query | No | Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a> |
| langfilter | string | query | No | ODHActivityPoi Langfilter (returns only SmgPois available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| areafilter | string | query | No | AreaFilter (Alternate Locfilter, can be combined with locfilter) (Separator ',' possible values: reg + REGIONID = (Filter by Region), tvs + TOURISMASSOCIATIONID = (Filter by Tourismassociation), skr + SKIREGIONID = (Filter by Skiregion), ska + SKIAREAID = (Filter by Skiarea), are + AREAID = (Filter by LTS Area), 'null' = No Filter), (default:'null') |
| highlight | boolean | query | No | Hightlight Filter (possible values: 'false' = only ODHActivityPoi with Highlight false, 'true' = only ODHActivityPoi with Highlight true), (default:'null') |
| source | string | query | No | Source Filter (possible Values: 'null' Displays all ODHActivityPoi, 'None', 'ActivityData', 'PoiData', 'GastronomicData', 'MuseumData', 'Magnolia', 'Content', 'SuedtirolWein', 'ArchApp'), (default:'null') |
| odhtagfilter | string | query | No | ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible (OR FILTER), available Tags reference to 'v1/ODHTag?validforentity=odhactivitypoi'), (default:'null') |
| odhtagfilter_and | string | query | No | ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible (AND FILTER), available Tags reference to 'v1/ODHTag?validforentity=odhactivitypoi'), (default:'null') |
| odhactive | boolean | query | No | ODH Active (Published) ODHActivityPoi Filter (Refers to field OdhActive) (possible Values: 'true' only published ODHActivityPoi, 'false' only not published ODHActivityPoi), (default:'null') |
| active | boolean | query | No | Active ODHActivityPoi Filter (possible Values: 'true' only active ODHActivityPoi, 'false' only not active ODHActivityPoi), (default:'null') |
| categorycodefilter | string | query | No | CategoryCode Filter (Only for ODHActivityTypes of type Gastronomy) (BITMASK) refers to <a href="https://tourism.opendatahub.com/v1/GastronomyTypes?rawfilter=eq(Type,\"CategoryCodes\")" target="_blank">GastronomyTypes</a>, Type: CategoryCodes |
| dishcodefilter | string | query | No | DishCode Filter (Only for ODHActivityTypes of type Gastronomy) (BITMASK) refers to <a href="https://tourism.opendatahub.com/v1/GastronomyTypes" target="_blank">GastronomyTypes</a>, Type: DishCodes |
| ceremonycodefilter | string | query | No | CeremonyCode Filter (Only for ODHActivityTypes of type Gastronomy) (BITMASK) refers to <a href="https://tourism.opendatahub.com/v1/GastronomyTypes" target="_blank">GastronomyTypes</a>, Type: CeremonyCodes |
| facilitycodefilter | string | query | No | FacilityCode Filter (Only for ODHActivityTypes of type Gastronomy) (BITMASK) refers to <a href="https://tourism.opendatahub.com/v1/GastronomyTypes" target="_blank">GastronomyTypes</a>, Type: with FacilityCodes_ prefix |
| cuisinecodefilter | string | query | No | CuisineCode Filter (Only for ODHActivityTypes of type Gastronomy) (BITMASK) refers to <a href="https://tourism.opendatahub.com/v1/GastronomyTypes" target="_blank">GastronomyTypes</a>, Type: CuisineCodes |
| difficultyfilter | string | query | No | Difficulty Filter (possible values: '1' = easy, '2' = medium, '3' = difficult), (default:'null') |
| distancefilter | string | query | No | Distance Range Filter (Separator ',' example Value: 15,40 Distance from 15 up to 40 Km), (default:'null') |
| altitudefilter | string | query | No | Altitude Range Filter (Separator ',' example Value: 500,1000 Altitude from 500 up to 1000 metres), (default:'null') |
| durationfilter | string | query | No | Duration Range Filter (Separator ',' example Value: 1,3 Duration from 1 to 3 hours), (default:'null') |
| hasimage | boolean | query | No |  |
| tagfilter | string | query | No | Filter on Tags. Syntax =and/or(TagSource.TagId,TagSource.TagId,TagId) example or(idm.summer,lts.hiking) - and(idm.themed hikes,lts.family hikings) - or(hiking) - and(idm.summer) - Combining and/or is not supported at the moment, default: 'null') |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null') |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `ODHActivityPoiLinkedJsonResult`*

---

#### GET ODHActivityPoi Single
**Endpoint:** `GET /v1/ODHActivityPoi/{id}`
**Operation ID:** `SingleODHActivityPoi`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the Poi |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "OdhActive": <boolean>,
  "ODHTags": [],
  "ODHActivityPoiTypes": [],
  "Areas": [],
  "CategoryCodes": [],
  "DishRates": [],
  "CapacityCeremony": [],
  "Facilities": [],
  "LTSTags": [],
  "TagIds": [],
  "DistrictId": <string?>,
  "CustomId": <string?>,
  "PoiProperty": {
    ...
  },
  "PoiServices": [],
  "SyncSourceInterface": <string?>,
  "SyncUpdateMode": <string?>,
  "AgeFrom": <integer(int32)?>,
  "AgeTo": <integer(int32)?>,
  "MaxSeatingCapacity": <integer(int32)?>,
  "RelatedContent": [],
  "AdditionalContact": {
    ...
  },
  "GpsPoints": {
    ...
  },
  "AdditionalProperties": {
  },
  "Id": <string?>,
  "OutdooractiveID": <string?>,
  "OutdooractiveElevationID": <string?>,
  "SmgId": <string?>,
  "CopyrightChecked": <boolean?>,
  "Active": <boolean>,
  "Shortname": <string?>,
  "Difficulty": <string?>,
  "Type": <string?>,
  "SubType": <string?>,
  "PoiType": <string?>,
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "SmgActive": <boolean>,
  "TourismorganizationId": <string?>,
  "AreaId": [],
  "AreaIds": [],
  "AltitudeDifference": <number(double)?>,
  "AltitudeHighestPoint": <number(double)?>,
  "AltitudeLowestPoint": <number(double)?>,
  "AltitudeSumUp": <number(double)?>,
  "AltitudeSumDown": <number(double)?>,
  "DistanceDuration": <number(double)?>,
  "DistanceLength": <number(double)?>,
  "Highlight": <boolean?>,
  "IsOpen": <boolean?>,
  "IsPrepared": <boolean?>,
  "RunToValley": <boolean?>,
  "IsWithLigth": <boolean?>,
  "HasRentals": <boolean?>,
  "HasFreeEntrance": <boolean?>,
  "LiftAvailable": <boolean?>,
  "FeetClimb": <boolean?>,
  "BikeTransport": <boolean?>,
  "OperationSchedule": [],
  "GpsInfo": [],
  "GpsTrack": [],
  "ImageGallery": [],
  "Detail": {
    ...
  },
  "ContactInfos": {
    ...
  },
  "AdditionalPoiInfos": {
    ...
  },
  "SmgTags": [],
  "HasLanguage": [],
  "Exposition": [],
  "OwnerRid": <string?>,
  "ChildPoiIds": [],
  "MasterPoiIds": [],
  "WayNumber": <integer(int32)?>,
  "Number": <string?>,
  "PublishedOn": [],
  "Source": <string?>,
  "Mapping": {
    ...
  },
  "Tags": [],
  "VideoItems": {
    ...
  }
}
```

*Schema: `ODHActivityPoiLinked`*

---

#### GET ODHActivityPoi Types List
**Endpoint:** `GET /v1/ODHActivityPoiTypes`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No |  |
| pagesize | integer | query | No |  |
| idlist | string | query | No |  |
| seed | string | query | No |  |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET ODHActivityPoi Types Single
**Endpoint:** `GET /v1/ODHActivityPoiTypes/{id}`
**Operation ID:** `SingleODHActivityPoiTypes`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the ODHActivityPoi Type |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Id": <string?>,
  "Bitmask": <integer(int64)>,
  "Type": <string?>,
  "Parent": <string?>,
  "Key": <string?>,
  "TypeDesc": {
    ...
  }
}
```

*Schema: `SmgPoiTypes`*

---

### EVENTS

**Server:** `https://tourism.opendatahub.com`

#### GET Event List
**Endpoint:** `GET /v1/Event`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No | Pagenumber (default: 1) |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| idlist | string | query | No | IDFilter (Separator ',' List of Event IDs, 'null' = No Filter), (default:'null') |
| locfilter | string | query | No | Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a> |
| rancfilter | string | query | No | Rancfilter, Return only Events with this Ranc assigned (1 = not visible, 3 = visible, 4 = important, 5 = top-event),(default: 'null') |
| topicfilter | string | query | No | Topic ID Filter (Filter by Topic ID) BITMASK refers to 'v1/EventTopics',(default: 'null') |
| orgfilter | string | query | No | Organization Filter (Filter by Organizer RID) |
| odhtagfilter | string | query | No | ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=event'), (default:'null') |
| active | boolean | query | No | Active Events Filter (possible Values: 'true' only Active Events, 'false' only Disabled Events), (default:'null') |
| odhactive | boolean | query | No | ODH Active (Published) Events Filter (Refers to field OdhActive) Events Filter (possible Values: 'true' only published Events, 'false' only not published Events), (default:'null') |
| begindate | string | query | No | BeginDate of Events (Format: yyyy-MM-dd), (default: 'null') |
| enddate | string | query | No | EndDate of Events (Format: yyyy-MM-dd), (default: 'null') |
| sort | string | query | No | Sorting Mode of Events ('asc': Ascending simple sort by next begindate, 'desc': simple descent sorting by next begindate, 'upcoming': Sort Events by next EventDate matching passed startdate, 'upcomingspecial': Sort Events by next EventDate matching passed startdate, multiple day events are showed at bottom, default: if no sort mode passed, sort by shortname ) |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| source | string | query | No | Filter by Source (Separator ','), (Sources available 'lts','trevilab','drin'),(default: 'null') |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `EventLinkedJsonResult`*

---

#### GET Event Single
**Endpoint:** `GET /v1/Event/{id}`
**Operation ID:** `SingleEvent`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the Event |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "EventDatesBegin": [],
  "EventDatesEnd": [],
  "EventDateCounter": <integer(int32)?>,
  "Districts": [],
  "ODHTags": [],
  "OdhActive": <boolean?>,
  "Topics": [],
  "GpsInfo": [],
  "Gpstype": <string?>,
  "Latitude": <number(double)>,
  "Longitude": <number(double)>,
  "Altitude": <number(double)?>,
  "AltitudeUnitofMeasure": <string?>,
  "GpsPoints": {
    ...
  },
  "Tags": [],
  "TagIds": [],
  "Id": <string?>,
  "Active": <boolean>,
  "Shortname": <string?>,
  "HasLanguage": [],
  "Source": <string?>,
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "ImageGallery": [],
  "Detail": {
    ...
  },
  "ContactInfos": {
    ...
  },
  "PublishedOn": [],
  "Mapping": {
    ...
  },
  "DateBegin": <string(date-time)?>,
  "DateEnd": <string(date-time)?>,
  "DistrictId": <string?>,
  "DistrictIds": [],
  "TopicRIDs": [],
  "EventPublisher": [],
  "OrganizerInfos": {
    ...
  },
  "EventDate": [],
  "EventAdditionalInfos": {
    ...
  },
  "EventVariants": [],
  "EventUrls": [],
  "ClassificationRID": <string?>,
  "Ticket": <string?>,
  "SignOn": <string?>,
  "OrgRID": <string?>,
  "EventPrice": {
    ...
  },
  "SmgTags": [],
  "SmgActive": <boolean>
}
```

*Schema: `EventLinked`*

---

#### GET Event Topic List
**Endpoint:** `GET /v1/EventTopics`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No |  |
| pagesize | integer | query | No |  |
| idlist | string | query | No |  |
| seed | string | query | No |  |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET Event Topic Single
**Endpoint:** `GET /v1/EventTopics/{id}`
**Operation ID:** `SingleEventTopics`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the Event |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Id": <string?>,
  "Bitmask": <integer(int32)>,
  "Type": <string?>,
  "TypeDesc": {
    ...
  }
}
```

*Schema: `EventTypes`*

---

### EVENTS V2

**Server:** `https://tourism.opendatahub.com`

#### GET Event List
**Endpoint:** `GET /v2/Event`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No | Pagenumber (default: 1) |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| idlist | string | query | No | IDFilter (Separator ',' List of Event IDs, 'null' = No Filter), (default:'null') |
| venueidfilter | string | query | No |  |
| locfilter | string | query | No | Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a> |
| tagfilter | string | query | No |  |
| active | boolean | query | No | Active Events Filter (possible Values: 'true' only Active Events, 'false' only Disabled Events), (default:'null') |
| begindate | string | query | No | BeginDate of Events (Format: yyyy-MM-dd), (default: 'null') |
| enddate | string | query | No | EndDate of Events (Format: yyyy-MM-dd), (default: 'null') |
| sort | string | query | No | Sorting Mode of Events ('asc': Ascending simple sort by next begindate, 'desc': simple descent sorting by next begindate, 'upcoming': Sort Events by next EventDate matching passed startdate, 'upcomingspecial': Sort Events by next EventDate matching passed startdate, multiple day events are showed at bottom, default: if no sort mode passed, sort by shortname ) |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| source | string | query | No | Filter by Source (Separator ','), (Sources available 'lts','trevilab','drin'),(default: 'null') |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `EventV2JsonResult`*

---

#### GET Event Single
**Endpoint:** `GET /v2/Event/{id}`
**Operation ID:** `SingleEventV2`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the Event |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "Id": <string?>,
  "Shortname": <string?>,
  "Active": <boolean>,
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "Source": <string?>,
  "HasLanguage": [],
  "PublishedOn": [],
  "Mapping": {
    ...
  },
  "RelatedContent": [],
  "IsRoot": <boolean?>,
  "EventGroupId": <string?>,
  "AdditionalProperties": {
    ...
  },
  "Tags": [],
  "TagIds": [],
  "Detail": {
    ...
  },
  "ContactInfos": {
    ...
  },
  "ImageGallery": [],
  "VideoItems": {
    ...
  },
  "Documents": {
    ...
  },
  "Begin": <string(date-time)>,
  "End": <string(date-time)>,
  "BeginUTC": <number(double)>,
  "EndUTC": <number(double)>,
  "VenueId": <string?>,
  "Capacity": <integer(int32)?>
}
```

*Schema: `EventV2`*

---

### EVENTS SHORT

**Server:** `https://tourism.opendatahub.com`

#### GET EventShort List
**Endpoint:** `GET /v1/EventShort`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No | Pagenumber (Integer) (default: 1) |
| pagesize | integer | query | No | Pagesize (Integer), (default: 'null') |
| startdate | string | query | No | Format (yyyy-MM-dd HH:mm) default or Unix Timestamp |
| enddate | string | query | No | Format (yyyy-MM-dd HH:mm) default or Unix Timestamp |
| datetimeformat | string | query | No | not provided, use default format, for unix timestamp pass "uxtimestamp" |
| source | string | query | No | Source of the data, (possible values 'Content' or 'EBMS') |
| eventlocation | string | query | No | <p>Members:</p><ul><li><i>NOI</i> - NOI Techpark</li> <li><i>EC</i> - Eurac</li> <li><i>VV</i> - Virtual Village</li> <li><i>OUT</i> - Other Location</li> </ul> (values: NOI, EC, VV, OUT) |
| onlyactive | boolean | query | No | 'true' if only Events marked as Active for today.noi.bz.it should be returned |
| websiteactive | boolean | query | No | 'true' if only Events marked as Active for noi.bz.it should be returned |
| communityactive | boolean | query | No | 'true' if only Events marked as Active for Noi community should be returned |
| active | boolean | query | No | Active Events Filter (possible Values: 'true' only Active Events, 'false' only Disabled Events), (default:'true') (default: True) |
| eventids | string | query | No | comma separated list of event ids |
| webaddress | string | query | No | Searches the webaddress |
| sortorder | string | query | No | ASC or DESC by StartDate (default: ASC) |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| optimizedates | boolean | query | No | Optimizes dates, cuts out all Rooms with Comment "x", revisits and corrects start + enddate (default: False) |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| lastchange | string | query | No |  |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "OnlineResults": <integer(int32)?>,
  "ResultId": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `EventShortResult`*

---

#### GET EventShort Single
**Endpoint:** `GET /v1/EventShort/Detail/{id}`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | Id of the Event |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| optimizedates | boolean | query | No | Optimizes dates, cuts out all Rooms with Comment "x", revisits and corrects start + enddate (default: False) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Id": <string?>,
  "Source": <string?>,
  "Detail": {
    ...
  },
  "EventLocation": <string?>,
  "EventId": <integer(int32)?>,
  "EventText": {
    ...
  },
  "EventTitle": {
    ...
  },
  "EventTextDE": <string?>,
  "EventTextIT": <string?>,
  "EventTextEN": <string?>,
  "EventDescription": <string?>,
  "EventDescriptionDE": <string?>,
  "EventDescriptionIT": <string?>,
  "EventDescriptionEN": <string?>,
  "AnchorVenue": <string?>,
  "AnchorVenueShort": <string?>,
  "ChangedOn": <string(date-time)>,
  "StartDate": <string(date-time)>,
  "EndDate": <string(date-time)>,
  "StartDateUTC": <number(double)>,
  "EndDateUTC": <number(double)>,
  "WebAddress": <string?>,
  "Display1": <string?>,
  "Display2": <string?>,
  "Display3": <string?>,
  "Display4": <string?>,
  "Display5": <string?>,
  "Display6": <string?>,
  "Display7": <string?>,
  "Display8": <string?>,
  "Display9": <string?>,
  "CompanyName": <string?>,
  "CompanyId": <string?>,
  "CompanyAddressLine1": <string?>,
  "CompanyAddressLine2": <string?>,
  "CompanyAddressLine3": <string?>,
  "CompanyPostalCode": <string?>,
  "CompanyCity": <string?>,
  "CompanyCountry": <string?>,
  "CompanyPhone": <string?>,
  "CompanyFax": <string?>,
  "CompanyMail": <string?>,
  "CompanyUrl": <string?>,
  "ContactCode": <string?>,
  "ContactFirstName": <string?>,
  "ContactLastName": <string?>,
  "ContactPhone": <string?>,
  "ContactCell": <string?>,
  "ContactFax": <string?>,
  "ContactEmail": <string?>,
  "ContactAddressLine1": <string?>,
  "ContactAddressLine2": <string?>,
  "ContactAddressLine3": <string?>,
  "ContactPostalCode": <string?>,
  "ContactCity": <string?>,
  "ContactCountry": <string?>,
  "RoomBooked": [],
  "ImageGallery": [],
  "VideoUrl": <string?>,
  "TechnologyFields": [],
  "CustomTagging": [],
  "EventDocument": [],
  "Documents": {
    ...
  },
  "ExternalOrganizer": <boolean?>,
  "Shortname": <string?>,
  "PublishedOn": [],
  "AnchorVenueRoomMapping": <string?>,
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "SoldOut": <boolean?>,
  "ActiveToday": <boolean?>,
  "ActiveWeb": <boolean?>,
  "ActiveCommunityApp": <boolean?>,
  "HasLanguage": [],
  "Mapping": {
    ...
  },
  "GpsInfo": [],
  "GpsPoints": {
    ...
  },
  "Active": <boolean?>,
  "VideoItems": {
    ...
  }
}
```

*Schema: `EventShort`*

---

#### GET EventShort Single
**Endpoint:** `GET /v1/EventShort/{id}`
**Operation ID:** `SingleEventShort`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | Id of the Event |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| optimizedates | boolean | query | No | Optimizes dates, cuts out all Rooms with Comment "x", revisits and corrects start + enddate (default: False) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Id": <string?>,
  "Source": <string?>,
  "Detail": {
    ...
  },
  "EventLocation": <string?>,
  "EventId": <integer(int32)?>,
  "EventText": {
    ...
  },
  "EventTitle": {
    ...
  },
  "EventTextDE": <string?>,
  "EventTextIT": <string?>,
  "EventTextEN": <string?>,
  "EventDescription": <string?>,
  "EventDescriptionDE": <string?>,
  "EventDescriptionIT": <string?>,
  "EventDescriptionEN": <string?>,
  "AnchorVenue": <string?>,
  "AnchorVenueShort": <string?>,
  "ChangedOn": <string(date-time)>,
  "StartDate": <string(date-time)>,
  "EndDate": <string(date-time)>,
  "StartDateUTC": <number(double)>,
  "EndDateUTC": <number(double)>,
  "WebAddress": <string?>,
  "Display1": <string?>,
  "Display2": <string?>,
  "Display3": <string?>,
  "Display4": <string?>,
  "Display5": <string?>,
  "Display6": <string?>,
  "Display7": <string?>,
  "Display8": <string?>,
  "Display9": <string?>,
  "CompanyName": <string?>,
  "CompanyId": <string?>,
  "CompanyAddressLine1": <string?>,
  "CompanyAddressLine2": <string?>,
  "CompanyAddressLine3": <string?>,
  "CompanyPostalCode": <string?>,
  "CompanyCity": <string?>,
  "CompanyCountry": <string?>,
  "CompanyPhone": <string?>,
  "CompanyFax": <string?>,
  "CompanyMail": <string?>,
  "CompanyUrl": <string?>,
  "ContactCode": <string?>,
  "ContactFirstName": <string?>,
  "ContactLastName": <string?>,
  "ContactPhone": <string?>,
  "ContactCell": <string?>,
  "ContactFax": <string?>,
  "ContactEmail": <string?>,
  "ContactAddressLine1": <string?>,
  "ContactAddressLine2": <string?>,
  "ContactAddressLine3": <string?>,
  "ContactPostalCode": <string?>,
  "ContactCity": <string?>,
  "ContactCountry": <string?>,
  "RoomBooked": [],
  "ImageGallery": [],
  "VideoUrl": <string?>,
  "TechnologyFields": [],
  "CustomTagging": [],
  "EventDocument": [],
  "Documents": {
    ...
  },
  "ExternalOrganizer": <boolean?>,
  "Shortname": <string?>,
  "PublishedOn": [],
  "AnchorVenueRoomMapping": <string?>,
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "SoldOut": <boolean?>,
  "ActiveToday": <boolean?>,
  "ActiveWeb": <boolean?>,
  "ActiveCommunityApp": <boolean?>,
  "HasLanguage": [],
  "Mapping": {
    ...
  },
  "GpsInfo": [],
  "GpsPoints": {
    ...
  },
  "Active": <boolean?>,
  "VideoItems": {
    ...
  }
}
```

*Schema: `EventShort`*

---

#### GET EventShort List by Room Occupation
**Endpoint:** `GET /v1/EventShort/GetbyRoomBooked`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| startdate | string | query | No | Format (yyyy-MM-dd HH:mm) default or Unix Timestamp |
| enddate | string | query | No | Format (yyyy-MM-dd HH:mm) default or Unix Timestamp |
| datetimeformat | string | query | No | not provided, use default format, for unix timestamp pass "uxtimestamp" |
| source | string | query | No | Source of the data, (possible values 'Content' or 'EBMS') |
| eventlocation | string | query | No | <p>Members:</p><ul><li><i>NOI</i> - NOI Techpark</li> <li><i>EC</i> - Eurac</li> <li><i>VV</i> - Virtual Village</li> <li><i>OUT</i> - Other Location</li> </ul> (values: NOI, EC, VV, OUT) |
| onlyactive | boolean | query | No | 'true' if only Events marked as Active for today.noi.bz.it should be returned |
| websiteactive | boolean | query | No | 'true' if only Events marked as Active for noi.bz.it should be returned |
| communityactive | boolean | query | No | 'true' if only Events marked as Active for Noi community should be returned |
| active | boolean | query | No | Active Events Filter (possible Values: 'true' only Active Events, 'false' only Disabled Events), (default:'true') (default: True) |
| eventids | string | query | No | comma separated list of event ids |
| webaddress | string | query | No | Filter by WebAddress Field |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| lastchange | string | query | No |  |
| updatefrom | string | query | No |  |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| eventgrouping | boolean | query | No | Groups Events with the Same Date/Id/Name and adds all Rooms to the SpaceDesc List (default: True) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET EventShort Types
**Endpoint:** `GET /v1/EventShortTypes`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No |  |
| pagesize | integer | query | No |  |
| idlist | string | query | No |  |
| seed | string | query | No |  |
| type | string | query | No | Type to filter for ('TechnologyFields','CustomTagging') |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET EventShort Type Single
**Endpoint:** `GET /v1/EventShortTypes/{id}`
**Operation ID:** `SingleEventShortTypes`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the EventShort Type |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Id": <string?>,
  "Bitmask": <integer(int64)>,
  "Type": <string?>,
  "Parent": <string?>,
  "Key": <string?>,
  "TypeDesc": {
    ...
  }
}
```

*Schema: `EventShortTypes`*

---

### ARTICLES

**Server:** `https://tourism.opendatahub.com`

#### GET Article List
**Endpoint:** `GET /v1/Article`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No | Pagenumber (default: 1) |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| articletype | string | query | No | Type of the Article ('null' = Filter disabled, possible values: BITMASK values: 1 = basearticle, 2 = book article, 4 = contentarticle, 8 = eventarticle, 16 = pressarticle, 32 = recipe, 64 = touroperator , 128 = b2b, 256  = idmarticle, 512 = specialannouncement, 1024 = newsfeednoi), (also possible for compatibily reasons: basisartikel, buchtippartikel, contentartikel, veranstaltungsartikel, presseartikel, rezeptartikel, reiseveranstalter, b2bartikel ) (default:'255' == ALL), REFERENCE TO: GET /api/ArticleTypes |
| articlesubtype | string | query | No | Sub Type of the Article (depends on the Maintype of the Article 'null' = Filter disabled) |
| idlist | string | query | No | IDFilter (Separator ',' List of Article IDs), (default:'null') |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| sortbyarticledate | boolean | query | No | Sort By Articledate ('true' sorts Articles by Articledate) |
| odhtagfilter | string | query | No | ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=article'), (default:'null') |
| odhactive | boolean | query | No | ODH Active (Published) Articles Filter (Refers to field OdhActive) (possible Values: 'true' only published Article, 'false' only not published Articles), (default:'null') |
| active | boolean | query | No | Active Articles Filter (possible Values: 'true' only Active Articles, 'false' only Disabled Articles), (default:'null') |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| startdate | string | query | No | Filter by ArticleDate Format (yyyy-MM-dd HH:mm) |
| enddate | string | query | No | Filter by ArticleDate Format (yyyy-MM-dd HH:mm) |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| source | string | query | No | Filter by Source (Separator ','), (Sources available 'idm','noi'...),(default: 'null') |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `ArticlesLinkedJsonResult`*

---

#### GET Article Single
**Endpoint:** `GET /v1/Article/{id}`
**Operation ID:** `SingleArticle`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the Article |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "OdhActive": <boolean>,
  "ArticleTypes": [],
  "ArticleTypeList": [],
  "ODHTags": [],
  "Tags": [],
  "TagIds": [],
  "Id": <string?>,
  "Active": <boolean>,
  "Shortname": <string?>,
  "Highlight": <boolean?>,
  "Type": <string?>,
  "SubType": <string?>,
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "SmgActive": <boolean>,
  "ArticleDate": <string(date-time)?>,
  "ArticleDateTo": <string(date-time)?>,
  "OperationSchedule": [],
  "GpsInfo": [],
  "GpsTrack": [],
  "ImageGallery": [],
  "Detail": {
    ...
  },
  "ContactInfos": {
    ...
  },
  "AdditionalArticleInfos": {
    ...
  },
  "ArticleLinkInfo": {
    ...
  },
  "SmgTags": [],
  "HasLanguage": [],
  "ExpirationDate": <string(date-time)?>,
  "GpsPoints": {
    ...
  },
  "PublishedOn": [],
  "Source": <string?>,
  "Mapping": {
    ...
  },
  "VideoItems": {
    ...
  }
}
```

*Schema: `ArticlesLinked`*

---

#### GET Article Types List
**Endpoint:** `GET /v1/ArticleTypes`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No |  |
| pagesize | integer | query | No |  |
| idlist | string | query | No |  |
| seed | string | query | No |  |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET Article Types Single
**Endpoint:** `GET /v1/ArticleTypes/{id}`
**Operation ID:** `SingleArticleTypes`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the Article Type |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Id": <string?>,
  "Bitmask": <integer(int64)>,
  "Type": <string?>,
  "Parent": <string?>,
  "Key": <string?>,
  "TypeDesc": {
    ...
  }
}
```

*Schema: `ArticleTypes`*

---

### ANNOUNCEMENTS

**Server:** `https://tourism.opendatahub.com`

#### GET Announcement List
**Endpoint:** `GET /v1/Announcement`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No | Pagenumber (default: 1) |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| langfilter | string | query | No | Langfilter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| idlist | string | query | No | IDFilter (Separator ',' List of IDs, 'null' = No Filter), (default:'null') |
| source | string | query | No | Source Filter (possible Values: 'lts','idm'), (default:'null') |
| active | boolean | query | No | Active Filter (possible Values: 'true' only active data, 'false' only not active data), (default:'null') |
| begin | string | query | No | Begin Filter (Format: yyyy-MM-dd HH:MM), (default: 'null') |
| end | string | query | No | End Filter (Format: yyyy-MM-dd HH:MM), (default: 'null') |
| tagfilter | string | query | No | Filter on Tags. Syntax =and/or(TagSource.TagId,TagSource.TagId,TagId) example or(idm.summer,lts.hiking) - and(idm.themed hikes,lts.family hikings) - or(hiking) - and(idm.summer) - Combining and/or is not supported at the moment, default: 'null') |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET Announcement Single
**Endpoint:** `GET /v1/Announcement/{id}`
**Operation ID:** `SingleAnnouncement`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the Announcement |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "StartTime": <string(date-time)?>,
  "EndTime": <string(date-time)?>,
  "Detail": {
    ...
  },
  "RelatedContent": [],
  "GpsPoints": {
    ...
  },
  "Id": <string?>,
  "Shortname": <string?>,
  "Active": <boolean>,
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "HasLanguage": [],
  "PublishedOn": [],
  "Mapping": {
    ...
  },
  "AdditionalProperties": {
  },
  "Source": <string?>,
  "Tags": [],
  "TagIds": [],
  "GpsInfo": []
}
```

*Schema: `Announcement`*

---

### VENUES

**Server:** `https://tourism.opendatahub.com`

#### GET Venue List
**Endpoint:** `GET /v1/Venue`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No | Pagenumber (default: 1) |
| pagesize | integer | query | No | Elements per Page (max 1024), (default:10) |
| categoryfilter | string | query | No | Venue Category Filter (BITMASK) (Separator ',' List of Venuetype Bitmasks, refer to api/VenueTypes type:category), (default:'null') |
| capacityfilter | string | query | No | Capacity Range Filter (Separator ',' example Value: 50,100 All Venues with rooms from 50 to 100 people), (default:'null') |
| roomcountfilter | string | query | No | Room Count Range Filter (Separator ',' example Value: 2,5 All Venues with 2 to 5 rooms), (default:'null') |
| idlist | string | query | No | IDFilter (Separator ',' List of Venue IDs), (default:'null') |
| locfilter | string | query | No | Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a> |
| featurefilter | string | query | No | Venue Features Filter (BITMASK) (Separator ',' List of Venuetype Bitmasks, refer to api/VenueTypes type:feature), (default:'null') |
| setuptypefilter | string | query | No | Venue SetupType Filter (BITMASK) (Separator ',' List of Venuetype Bitmasks, refer to api/VenueTypes type:seatType), (default:'null') |
| odhtagfilter | string | query | No | ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=venue'), (default:'null') |
| source | string | query | No | Source Filter(String, ), (default:'null') |
| active | boolean | query | No | Active Venue Filter (possible Values: 'true' only Active Venues, 'false' only Disabled Venues), (default:'null') |
| odhactive | boolean | query | No | ODH Active (Published) Venue Filter (possible Values: 'true' only published Venue, 'false' only not published Venue), (default:'null') |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null') |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null)<a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| destinationdataformat | boolean | query | No | If set to true, data will be returned in AlpineBits Destinationdata Format (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `VenueLinkedJsonResult`*

---

#### GET Venue Single
**Endpoint:** `GET /v1/Venue/{id}`
**Operation ID:** `SingleVenue`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the Venue |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| destinationdataformat | boolean | query | No | If set to true, data will be returned in AlpineBits Destinationdata Format (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "OdhActive": <boolean>,
  "ODHTags": [],
  "Id": <string?>,
  "Shortname": <string?>,
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "Active": <boolean>,
  "SmgActive": <boolean>,
  "SmgTags": [],
  "HasLanguage": [],
  "VenueCategory": [],
  "GpsInfo": [],
  "Source": <string?>,
  "SyncSourceInterface": <string?>,
  "RoomCount": <integer(int32)?>,
  "RoomDetails": [],
  "GpsPoints": {
    ...
  },
  "Detail": {
    ...
  },
  "ContactInfos": {
    ...
  },
  "ImageGallery": [],
  "PublishedOn": [],
  "Mapping": {
    ...
  },
  "Beds": <integer(int32)?>,
  "OperationSchedule": []
}
```

*Schema: `VenueLinked`*

---

#### GET Venue Types List
**Endpoint:** `GET /v1/VenueTypes`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No |  |
| pagesize | integer | query | No |  |
| idlist | string | query | No |  |
| seed | string | query | No |  |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null)<a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET Venue Types Single
**Endpoint:** `GET /v1/VenueTypes/{id}`
**Operation ID:** `SingleVenueTypes`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the VenueType |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname. Select also Dictionary fields, example Detail.de.Title, or Elements of Arrays example ImageGallery[0].ImageUrl. (default:'null' all fields are displayed) |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Id": <string?>,
  "Code": <string?>,
  "TypeDesc": {
    ...
  },
  "Name": {
    ...
  },
  "Bitmask": <integer(int32)>,
  "Type": <string?>
}
```

*Schema: `DDVenueCodes`*

---

### VENUES V2

**Server:** `https://tourism.opendatahub.com`

#### GET Venue V2 List
**Endpoint:** `GET /v2/Venue`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No | Pagenumber (default: 1) |
| pagesize | integer | query | No | Elements per Page (max 1024), (default:10) |
| categoryfilter | string | query | No | Venue Category Filter (BITMASK) (Separator ',' List of Venuetype Bitmasks, refer to api/VenueTypes type:category), (default:'null') |
| capacityfilter | string | query | No | Capacity Range Filter (Separator ',' example Value: 50,100 All Venues with rooms from 50 to 100 people), (default:'null') |
| roomcountfilter | string | query | No | Room Count Range Filter (Separator ',' example Value: 2,5 All Venues with 2 to 5 rooms), (default:'null') |
| idlist | string | query | No | IDFilter (Separator ',' List of Venue IDs), (default:'null') |
| locfilter | string | query | No | Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a> |
| featurefilter | string | query | No | Venue Features Filter (BITMASK) (Separator ',' List of Venuetype Bitmasks, refer to api/VenueTypes type:feature), (default:'null') |
| setuptypefilter | string | query | No | Venue SetupType Filter (BITMASK) (Separator ',' List of Venuetype Bitmasks, refer to api/VenueTypes type:seatType), (default:'null') |
| odhtagfilter | string | query | No | ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=venue'), (default:'null') |
| source | string | query | No | Source Filter(String, ), (default:'null') |
| active | boolean | query | No | Active Venue Filter (possible Values: 'true' only Active Venues, 'false' only Disabled Venues), (default:'null') |
| odhactive | boolean | query | No | ODH Active (Published) Venue Filter (possible Values: 'true' only published Venue, 'false' only not published Venue), (default:'null') |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null') |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null)<a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `VenueV2JsonResult`*

---

#### GET Venue Single
**Endpoint:** `GET /v2/Venue/{id}`
**Operation ID:** `SingleVenueV2`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the Venue |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| destinationdataformat | boolean | query | No | If set to true, data will be returned in AlpineBits Destinationdata Format (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "Id": <string?>,
  "Shortname": <string?>,
  "Active": <boolean>,
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "Source": <string?>,
  "HasLanguage": [],
  "PublishedOn": [],
  "Mapping": {
    ...
  },
  "RelatedContent": [],
  "IsRoot": <boolean?>,
  "VenueGroupId": <string?>,
  "AdditionalProperties": {
    ...
  },
  "Detail": {
    ...
  },
  "ContactInfos": {
    ...
  },
  "ImageGallery": [],
  "VideoItems": {
    ...
  },
  "GpsInfo": [],
  "OperationSchedule": [],
  "Capacity": [],
  "Tags": [],
  "TagIds": [],
  "GpsPoints": {
    ...
  }
}
```

*Schema: `VenueV2`*

---

### WEATHER

**Server:** `https://tourism.opendatahub.com`

#### GET Current Suedtirol Weather LIVE
**Endpoint:** `GET /v1/Weather`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No |  |
| pagesize | integer | query | No |  |
| language | string | query | No | Language (default: en) |
| locfilter | string | query | No | Locfilter (possible values: filter by StationData 1 = Schlanders, 2 = Meran, 3 = Bozen, 4 = Sterzing, 5 = Brixen, 6 = Bruneck \| filter nearest Station to Region,TV,Municipality,Fraction reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), '' = No Filter). IF a Locfilter is set, only Stationdata is provided. |
| extended | boolean | query | No |  (default: True) |
| source | string | query | No |  |
| fields | array[string] | query | No |  |

**Response Structure:**

```json
{
  "date": <string(date-time)>,
  "evolutiontitle": <string?>,
  "evolution": <string?>,
  "language": <string?>,
  "Date": <string(date-time)>,
  "EvolutionTitle": <string?>,
  "Evolution": <string?>,
  "Language": <string?>,
  "Id": <integer(int32)>,
  "Conditions": [],
  "Forecast": [],
  "Mountain": [],
  "Stationdata": []
}
```

*Schema: `Weather`*

---

#### GET Current Suedtirol Weather LIVE Single
**Endpoint:** `GET /v1/Weather/{id}`
**Operation ID:** `SingleWeather`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID |
| language | string | query | No | Language (default: en) |
| source | string | query | No |  |
| fields | array[string] | query | No |  |

**Response Structure:**

```json
{
  "date": <string(date-time)>,
  "evolutiontitle": <string?>,
  "evolution": <string?>,
  "language": <string?>,
  "Date": <string(date-time)>,
  "EvolutionTitle": <string?>,
  "Evolution": <string?>,
  "Language": <string?>,
  "Id": <integer(int32)>,
  "Conditions": [],
  "Forecast": [],
  "Mountain": [],
  "Stationdata": []
}
```

*Schema: `Weather`*

---

#### GET Suedtirol Weather HISTORY
**Endpoint:** `GET /v1/WeatherHistory`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No | Pagenumber (default: 1) |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| language | string | query | No | Language |
| idlist | string | query | No |  |
| locfilter | string | query | No |  |
| datefrom | string | query | No |  |
| dateto | string | query | No |  |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| latitude | string | query | No |  |
| longitude | string | query | No |  |
| radius | string | query | No |  |
| polygon | string | query | No |  |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null)<a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| lastchange | string | query | No |  |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Weather": {
    ...
  },
  "WeatherForecast": [],
  "WeatherDistrict": {
    ...
  },
  "HasLanguage": [],
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "Shortname": <string?>,
  "Id": <string?>
}
```

*Schema: `WeatherHistory`*

---

#### GET Suedtirol Weather HISTORY SINGLE
**Endpoint:** `GET /v1/WeatherHistory/{id}`
**Operation ID:** `SingleWeatherHistory`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Weather": {
    ...
  },
  "WeatherForecast": [],
  "WeatherDistrict": {
    ...
  },
  "HasLanguage": [],
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "Shortname": <string?>,
  "Id": <string?>
}
```

*Schema: `WeatherHistory`*

---

#### GET District Weather LIVE
**Endpoint:** `GET /v1/Weather/District`
**Operation ID:** `SingleWeatherDistrict`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No |  |
| pagesize | integer | query | No |  |
| locfilter | string | query | No | Locfilter (possible values: filter by District 1 = Etschtal/Überetsch/Unterland, 2 = Burggrafenamt, 3 = Vinschgau, 4 = Eisacktal und Sarntal, 5 = Wipptal, 6 = Pustertal/Dolomiten, 7 = Ladinien-Dolomiten \| filter nearest DistrictWeather to Region,TV,Municipality,Fraction reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction)) |
| language | string | query | No | Language (default: en) |
| source | string | query | No |  |
| fields | array[string] | query | No |  |

**Response Structure:**

```json
{
  "Id": <integer(int32)>,
  "Language": <string?>,
  "DistrictName": <string?>,
  "date": <string(date-time)>,
  "Date": <string(date-time)>,
  "TourismVereinIds": [],
  "BezirksForecast": []
}
```

*Schema: `BezirksWeather`*

---

#### GET District Weather LIVE SINGLE
**Endpoint:** `GET /v1/Weather/District/{id}`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID |
| language | string | query | No | Language (default: en) |
| source | string | query | No |  |
| fields | array[string] | query | No |  |

**Response Structure:**

```json
{
  "Id": <integer(int32)>,
  "Language": <string?>,
  "DistrictName": <string?>,
  "date": <string(date-time)>,
  "Date": <string(date-time)>,
  "TourismVereinIds": [],
  "BezirksForecast": []
}
```

*Schema: `BezirksWeather`*

---

#### GET Current Realtime Weather LIVE
**Endpoint:** `GET /v1/Weather/Realtime`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No |  |
| pagesize | integer | query | No |  |
| language | string | query | No | Language (default: en) |
| latitude | string | query | No |  |
| longitude | string | query | No |  |
| radius | string | query | No |  |
| fields | array[string] | query | No |  |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET Current Realtime Weather LIVE Single
**Endpoint:** `GET /v1/Weather/Realtime/{id}`
**Operation ID:** `SingleWeatherRealtime`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | id |
| language | string | query | No | Language (default: en) |
| fields | array[string] | query | No |  |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET Weather Forecast
**Endpoint:** `GET /v1/Weather/Forecast`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No |  |
| pagesize | integer | query | No |  |
| locfilter | string | query | No | Locfilter (possible values: filter on reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction)) |
| language | string | query | No | Language (default: en) |
| fields | array[string] | query | No |  |
| latitude | string | query | No |  |
| longitude | string | query | No |  |
| radius | string | query | No |  |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET Weather Forecast Single
**Endpoint:** `GET /v1/Weather/Forecast/{id}`
**Operation ID:** `SingleWeatherForecast`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes |  |
| language | string | query | No | Language (default: en) |
| fields | array[string] | query | No |  |

**Response Structure:**

```json
{
  "Self": <string?>,
  "Date": <string(date-time)>,
  "Id": <string?>,
  "Language": <string?>,
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "GpsInfo": [],
  "MunicipalityIstatCode": <string?>,
  "Shortname": <string?>,
  "MunicipalityName": {
    ...
  },
  "ForeCastDaily": [],
  "Forecast3HoursInterval": []
}
```

*Schema: `WeatherForecastLinked`*

---

#### GET Measuringpoint LIST
**Endpoint:** `GET /v1/Weather/Measuringpoint`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No |  |
| pagesize | integer | query | No |  |
| idlist | string | query | No | IDFilter (Separator ',' List of Gastronomy IDs), (default:'null') |
| locfilter | string | query | No | Locfilter (Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') |
| areafilter | string | query | No | Area ID (multiple IDs possible, separated by ",") |
| skiareafilter | string | query | No | Skiarea ID |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| source | string | query | No |  |
| active | boolean | query | No | Active Filter (possible Values: 'true' only Active Measuringpoints, 'false' only Disabled Measuringpoints), (default:'null') |
| odhactive | boolean | query | No | ODH Active Filter Measuringpoints Filter (possible Values: 'true' only published Measuringpoints, 'false' only not published Measuringpoints), (default:'null') |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null') |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET Measuringpoint SINGLE
**Endpoint:** `GET /v1/Weather/Measuringpoint/{id}`
**Operation ID:** `SingleMeasuringpoint`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | Measuringpoint ID |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Id": <string?>,
  "FirstImport": <string(date-time)?>,
  "LastUpdate": <string(date-time)>,
  "LastChange": <string(date-time)?>,
  "Active": <boolean>,
  "SmgActive": <boolean>,
  "Shortname": <string?>,
  "Gpstype": <string?>,
  "Latitude": <number(double)>,
  "Longitude": <number(double)>,
  "Altitude": <number(double)?>,
  "AltitudeUnitofMeasure": <string?>,
  "SnowHeight": <string?>,
  "newSnowHeight": <string?>,
  "Temperature": <string?>,
  "LastSnowDate": <string(date-time)>,
  "WeatherObservation": [],
  "OwnerId": <string?>,
  "AreaIds": [],
  "PublishedOn": [],
  "Source": <string?>,
  "Mapping": {
    ...
  },
  "SkiAreaIds": []
}
```

*Schema: `Measuringpoint`*

---

#### GET Snowreport Data LIVE
**Endpoint:** `GET /v1/Weather/SnowReport`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No |  |
| pagesize | integer | query | No |  |
| skiareaid | string | query | No | Skiarea ID |
| lang | string | query | No | Language (default: en) |

**Response Structure:**

```json
{
  "Id": <string?>,
  "RID": <string?>,
  "Skiregion": <string?>,
  "Areaname": <string?>,
  "LastUpdate": <string(date-time)>,
  "lang": <string?>,
  "SkiAreaSlopeKm": <string?>,
  "SkiMapUrl": <string?>,
  "Measuringpoints": [],
  "WebcamUrl": [],
  "totalskilift": <string?>,
  "openskilift": <string?>,
  "totalskiliftkm": <string?>,
  "openskiliftkm": <string?>,
  "totalskislopes": <string?>,
  "openskislopes": <string?>,
  "totalskislopeskm": <string?>,
  "openskislopeskm": <string?>,
  "totaltracks": <string?>,
  "opentracks": <string?>,
  "totaltrackskm": <string?>,
  "opentrackskm": <string?>,
  "totalslides": <string?>,
  "opentslides": <string?>,
  "totalslideskm": <string?>,
  "opentslideskm": <string?>,
  "totaliceskating": <string?>,
  "openiceskating": <string?>,
  "contactadress": <string?>,
  "contacttel": <string?>,
  "contactcap": <string?>,
  "contactcity": <string?>,
  "contactfax": <string?>,
  "contactweburl": <string?>,
  "contactmail": <string?>,
  "contactlogo": <string?>,
  "contactgpsnorth": <string?>,
  "contactgpseast": <string?>
}
```

*Schema: `SnowReportBaseData`*

---

#### GET Snowreport Data LIVE Single
**Endpoint:** `GET /v1/Weather/SnowReport/{id}`
**Operation ID:** `SingleSnowReport`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | Skiarea ID |
| lang | string | query | No | Language (default: en) |

**Response Structure:**

```json
{
  "Id": <string?>,
  "RID": <string?>,
  "Skiregion": <string?>,
  "Areaname": <string?>,
  "LastUpdate": <string(date-time)>,
  "lang": <string?>,
  "SkiAreaSlopeKm": <string?>,
  "SkiMapUrl": <string?>,
  "Measuringpoints": [],
  "WebcamUrl": [],
  "totalskilift": <string?>,
  "openskilift": <string?>,
  "totalskiliftkm": <string?>,
  "openskiliftkm": <string?>,
  "totalskislopes": <string?>,
  "openskislopes": <string?>,
  "totalskislopeskm": <string?>,
  "openskislopeskm": <string?>,
  "totaltracks": <string?>,
  "opentracks": <string?>,
  "totaltrackskm": <string?>,
  "opentrackskm": <string?>,
  "totalslides": <string?>,
  "opentslides": <string?>,
  "totalslideskm": <string?>,
  "opentslideskm": <string?>,
  "totaliceskating": <string?>,
  "openiceskating": <string?>,
  "contactadress": <string?>,
  "contacttel": <string?>,
  "contactcap": <string?>,
  "contactcity": <string?>,
  "contactfax": <string?>,
  "contactweburl": <string?>,
  "contactmail": <string?>,
  "contactlogo": <string?>,
  "contactgpsnorth": <string?>,
  "contactgpseast": <string?>
}
```

*Schema: `SnowReportBaseData`*

---

### WEBCAM INFO

**Server:** `https://tourism.opendatahub.com`

#### GET Webcam List
**Endpoint:** `GET /v1/WebcamInfo`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No | Pagenumber (default: 1) |
| pagesize | integer | query | No | Elements per Page (default:10) |
| source | string | query | No | Source Filter (Separator ',' available sources 'lts','content'), (default:'null') |
| idlist | string | query | No | IDFilter (Separator ',' List of Gastronomy IDs), (default:'null') |
| active | boolean | query | No | Active Webcam Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null') |
| odhactive | boolean | query | No | ODH Active (Published) Webcam Filter (possible Values: 'true' only published data, 'false' only not published data), (default:'null') |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null') |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Id OR Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null)<a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `WebcamInfoJsonResult`*

---

#### GET Webcam Single
**Endpoint:** `GET /v1/WebcamInfo/{id}`
**Operation ID:** `SingleWebcamInfo`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the Webcam |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "GpsInfo": [],
  "GpsPoints": {
    ...
  },
  "ContactInfos": {
    ...
  },
  "ImageGallery": [],
  "VideoItems": {
    ...
  },
  "Detail": {
    ...
  },
  "Webcamname": {
    ...
  },
  "Webcamurl": <string?>,
  "Streamurl": <string?>,
  "Previewurl": <string?>,
  "HasLanguage": [],
  "Id": <string?>,
  "LastChange": <string(date-time)?>,
  "FirstImport": <string(date-time)?>,
  "Shortname": <string?>,
  "Active": <boolean>,
  "SmgActive": <boolean>,
  "WebcamAssignedOn": [],
  "AreaIds": [],
  "SmgTags": [],
  "PublishedOn": [],
  "Mapping": {
    ...
  },
  "WebcamId": <string?>,
  "ListPosition": <integer(int32)?>,
  "Source": <string?>
}
```

*Schema: `WebcamInfo`*

---

### TAGS

**Server:** `https://tourism.opendatahub.com`

#### GET Tag List
**Endpoint:** `GET /v1/Tag`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No |  (default: 1) |
| pagesize | integer | query | No |  |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| validforentity | string | query | No | Filter on Tags valid on Entities (accommodation, activity, poi, odhactivitypoi, package, gastronomy, event, article, common .. etc..),(Separator ',' List of odhtypes) (default:'null') |
| types | string | query | No | Filter on Tags with this Types (Separator ',' List of types), (default:'null') |
| displayascategory | boolean | query | No | true = returns only Tags which are marked as DisplayAsCategory true |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| source | string | query | No | Source Filter (possible Values: 'lts','idm), (default:'null') |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET Tag Single
**Endpoint:** `GET /v1/Tag/{id}`
**Operation ID:** `SingleTag`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the Tag |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| localizationlanguage | string | query | No |  |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "ODHTagIds": [],
  "Types": [],
  "Source": <string?>,
  "Active": <boolean>,
  "Description": {
    ...
  },
  "Id": <string?>,
  "Shortname": <string?>,
  "TagName": {
    ...
  },
  "ValidForEntity": [],
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "DisplayAsCategory": <boolean?>,
  "IDMCategoryMapping": {
    ...
  },
  "MainEntity": <string?>,
  "Mapping": {
    ...
  },
  "MappedTagIds": [],
  "PublishDataWithTagOn": {
    ...
  },
  "PublishedOn": []
}
```

*Schema: `TagLinked`*

---

### ODH TAGS

**Server:** `https://tourism.opendatahub.com`

#### GET ODHTag List
**Endpoint:** `GET /v1/ODHTag`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No |  |
| pagesize | integer | query | No |  |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| validforentity | string | query | No | Filter on Tags valid on Entities (accommodation, activity, poi, odhactivitypoi, package, gastronomy, event, article, common .. etc..) |
| mainentity | string | query | No | Filter on Tags with MainEntity set to (accommodation, activity, poi, odhactivitypoi, package, gastronomy, event, article, common .. etc..) |
| displayascategory | boolean | query | No | true = returns only Tags which are marked as DisplayAsCategory true |
| source | string | query | No | Source Filter (possible Values: 'lts','idm'), (default:'null') |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| localizationlanguage | string | query | No | here for Compatibility Reasons, replaced by language parameter |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET ODHTag Single
**Endpoint:** `GET /v1/ODHTag/{id}`
**Operation ID:** `SingleODHTag`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the Odhtags |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| localizationlanguage | string | query | No |  |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "Id": <string?>,
  "Shortname": <string?>,
  "TagName": {
    ...
  },
  "ValidForEntity": [],
  "Source": [],
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "DisplayAsCategory": <boolean?>,
  "IDMCategoryMapping": {
    ...
  },
  "MainEntity": <string?>,
  "Mapping": {
    ...
  },
  "MappedTagIds": [],
  "PublishDataWithTagOn": {
    ...
  },
  "PublishedOn": []
}
```

*Schema: `ODHTagLinked`*

---

### LOCATIONS

**Server:** `https://tourism.opendatahub.com`

#### GET Location List (Use in locfilter)
**Endpoint:** `GET /v1/Location`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default 'en'), if 'null' is passed all languages are returned as Dictionary |
| pagenumber | integer (int32) | query | No |  |
| type | string | query | No | Type ('mta','reg','tvs','mun','fra') Separator ',' : 'null' returns all Location Objects (default) (default: null) (values: mta, reg, tvs, mun, fra) |
| showall | boolean | query | No | Show all Data (true = all, false = show only data marked as visible) (default: True) |
| locfilter | string | query | No | Locfilter (Separator ',') possible values: mta + MetaREGIONID = (Filter by MetaRegion), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), (default:'null') |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET Skiarea List (Use in locfilter as "ska")
**Endpoint:** `GET /v1/Location/Skiarea`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default 'en'), if 'null' is passed all languages are returned as Dictionary |
| pagenumber | integer (int32) | query | No |  |
| locfilter | string | query | No | Locfilter (Separator ',') possible values: mta + MetaREGIONID = (Filter by MetaRegion), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), (default:'null') |

**Response Structure:**

```json
[
  {
  }
]
```

---

### GEOGRAPHIC DATA

**Server:** `https://tourism.opendatahub.com`

#### GET GeoShapes List
**Endpoint:** `GET /v1/GeoShapes`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No |  (default: 1) |
| pagesize | integer | query | No |  |
| srid | string | query | No | Spatial Reference Identifier, Coordinate System of the geojson, available formats(epsg:4362,epsg:32632,epsg:3857) (default: epsg:4362) |
| source | string | query | No |  |
| type | string | query | No |  |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET GeoShapes List
**Endpoint:** `GET /v1/GeoShape`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No |  (default: 1) |
| pagesize | integer | query | No |  |
| srid | string | query | No | Spatial Reference Identifier, Coordinate System of the geojson, available formats(epsg:4362,epsg:32632,epsg:3857) (default: epsg:4362) |
| source | string | query | No |  |
| type | string | query | No |  |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
[
  {
  }
]
```

---

#### GET GeoShape Single
**Endpoint:** `GET /v1/GeoShapes/{id}`
**Operation ID:** `SingleGeoShapes`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the Tag |
| srid | string | query | No |  (default: epsg:4362) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Id": <string?>,
  "Country": <string?>,
  "Name": <string?>,
  "Source": <string?>,
  "Type": <string?>,
  "SRid": <string?>,
  "Mapping": {
    ...
  }
}
```

*Schema: `GeoShapeJson`*

---

#### GET GeoShape Single
**Endpoint:** `GET /v1/GeoShape/{id}`
**Operation ID:** `SingleGeoShape`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the Tag |
| srid | string | query | No |  (default: epsg:4362) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Id": <string?>,
  "Country": <string?>,
  "Name": <string?>,
  "Source": <string?>,
  "Type": <string?>,
  "SRid": <string?>,
  "Mapping": {
    ...
  }
}
```

*Schema: `GeoShapeJson`*

---

### SENSORS (LOCAL)

**Server:** `http://localhost:8082`

#### GET Sensor List
**Endpoint:** `GET /v1/Sensor`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No | Pagenumber (default: 1) |
| pagesize | integer (int32) | query | No | Elements per Page, (default:10) (default: 10) |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null') |
| sensortype | string | query | No | Type of sensor (e.g., 'TEMP', 'HUM', 'PRESS', 'PM25'), (default:'null') |
| manufacturer | string | query | No | Manufacturer filter (comma-separated list), (default:'null') |
| model | string | query | No | Model filter (comma-separated list), (default:'null') |
| datasetid | string | query | No | Filter by dataset ID (comma-separated list), (default:'null') |
| measurementtypename | string | query | No | Filter by measurement type name (comma-separated list), (default:'null') |
| source | string | query | No | Source Filter (possible Values: 'null' Displays all Sensors), (default:'null') |
| idlist | string | query | No | IDFilter (Separator ',' List of Sensor IDs), (default:'null') |
| langfilter | string | query | No | Language filter (returns only Sensors available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| odhtagfilter | string | query | No |  |
| publishedon | string | query | No |  |
| active | boolean | query | No | Active Sensor Filter (possible Values: 'true' only active Sensors, 'false' only not active Sensors), (default:'null') |
| odhactive | boolean | query | No | ODH Active (Published) Sensor Filter (Refers to field SmgActive) (possible Values: 'true' only published Sensors, 'false' only not published Sensors), (default:'null') |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| updatefrom | string (date-time) | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed) |
| searchfilter | string | query | No | String to search for, searches in sensor name, type, and details, (default: null) |
| rawfilter | string | query | No | Raw filter for advanced querying |
| rawsort | string | query | No | Raw sort for advanced sorting |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false) (default: False) |
| tsdatasetids | string | query | No | Timeseries dataset filter (comma-separated dataset IDs), (default:'null') |
| tsrequiredtypes | string | query | No | Timeseries required types filter (comma-separated type names, sensor must have ALL), (default:'null') |
| tsoptionaltypes | string | query | No | Timeseries optional types filter (comma-separated type names, sensor may have ANY), (default:'null') |
| tsmeasurementexpr | string | query | No | Timeseries measurement expression (e.g., 'or(o2.eq.2, and(temp.gteq.20, temp.lteq.30))'), (default:'null') |
| tslatestonly | boolean | query | No | Timeseries latest only filter (only consider latest measurements), (default:null) |
| tsstarttime | string | query | No | Timeseries start time filter (RFC3339 format), (default:'null') |
| tsendtime | string | query | No | Timeseries end time filter (RFC3339 format), (default:'null') |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `SensorLinkedJsonResult`*

---

#### GET Sensor Single
**Endpoint:** `GET /v1/Sensor/{id}`
**Operation ID:** `SingleSensor`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the Sensor |
| language | string | query | No | Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed) |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. (default: False) |

**Response Structure:**

```json
{
  "Id": <string?>,
  "Active": <boolean>,
  "SmgActive": <boolean>,
  "Source": <string?>,
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "SensorType": <string?>,
  "SensorName": <string?>,
  "ParentId": <string?>,
  "Gpstype": <string?>,
  "Latitude": <number(double)>,
  "Longitude": <number(double)>,
  "Altitude": <number(double)?>,
  "AltitudeUnitofMeasure": <string?>,
  "Manufacturer": <string?>,
  "Model": <string?>,
  "FirmwareVersion": <string?>,
  "InstallationDate": <string(date-time)?>,
  "CalibrationDate": <string(date-time)?>,
  "Detail": {
    ...
  },
  "ContactInfos": {
    ...
  },
  "ImageGallery": [],
  "DatasetIds": [],
  "MeasurementTypeNames": [],
  "Mapping": {
    ...
  },
  "HasLanguage": [],
  "SmgTags": [],
  "PublishedOn": [],
  "AdditionalProperties": {
    ...
  }
}
```

*Schema: `SensorLinked`*

---

### COMMON / DATASET

**Server:** `https://tourism.opendatahub.com`

#### GET MetaRegion List
**Endpoint:** `GET /v1/MetaRegion`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No | Pagenumber |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| idlist | string | query | No | IDFilter (Separator ',' List of data IDs), (default:'null') |
| odhtagfilter | string | query | No | Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null') |
| active | boolean | query | No | Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null') |
| odhactive | boolean | query | No | Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null') |
| source | string | query | No |  |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `MetaRegionLinkedJsonResult`*

---

#### GET MetaRegion Single
**Endpoint:** `GET /v1/MetaRegion/{id}`
**Operation ID:** `SingleMetaRegion`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the requested data |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "ODHTags": [],
  "OdhActive": <boolean>,
  "Districts": [],
  "TourismAssociations": [],
  "Regions": [],
  "GpsInfo": [],
  "Gpstype": <string?>,
  "Latitude": <number(double)>,
  "Longitude": <number(double)>,
  "Altitude": <number(double)?>,
  "AltitudeUnitofMeasure": <string?>,
  "GpsPoints": {
    ...
  },
  "DetailThemed": {
    ...
  },
  "DistrictIds": [],
  "TourismvereinIds": [],
  "RegionIds": [],
  "GpsPolygon": [],
  "VisibleInSearch": <boolean>,
  "RelatedContent": [],
  "Id": <string?>,
  "Active": <boolean>,
  "CustomId": <string?>,
  "Shortname": <string?>,
  "Detail": {
    ...
  },
  "ContactInfos": {
    ...
  },
  "ImageGallery": [],
  "SmgTags": [],
  "SmgActive": <boolean>,
  "HasLanguage": [],
  "LastChange": <string(date-time)?>,
  "FirstImport": <string(date-time)?>,
  "PublishedOn": [],
  "Source": <string?>,
  "Mapping": {
    ...
  }
}
```

*Schema: `MetaRegionLinked`*

---

#### GET Experiencearea List
**Endpoint:** `GET /v1/ExperienceArea`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No | Pagenumber |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| idlist | string | query | No | IDFilter (Separator ',' List of data IDs), (default:'null') |
| odhtagfilter | string | query | No | Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null') |
| active | boolean | query | No | Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null') |
| odhactive | boolean | query | No | Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null') |
| source | string | query | No |  |
| visibleinsearch | boolean | query | No | Filter only Elements flagged with visibleinsearch: (possible values: 'true','false'), (default:'false') |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `ExperienceAreaLinkedJsonResult`*

---

#### GET ExperienceArea Single
**Endpoint:** `GET /v1/ExperienceArea/{id}`
**Operation ID:** `SingleExperienceArea`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the requested data |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "ODHTags": [],
  "OdhActive": <boolean>,
  "TourismAssociations": [],
  "Districts": [],
  "GpsInfo": [],
  "Gpstype": <string?>,
  "Latitude": <number(double)>,
  "Longitude": <number(double)>,
  "Altitude": <number(double)?>,
  "AltitudeUnitofMeasure": <string?>,
  "GpsPoints": {
    ...
  },
  "DistrictIds": [],
  "TourismvereinIds": [],
  "GpsPolygon": [],
  "VisibleInSearch": <boolean>,
  "RelatedContent": [],
  "Id": <string?>,
  "Active": <boolean>,
  "CustomId": <string?>,
  "Shortname": <string?>,
  "Detail": {
    ...
  },
  "ContactInfos": {
    ...
  },
  "ImageGallery": [],
  "SmgTags": [],
  "SmgActive": <boolean>,
  "HasLanguage": [],
  "LastChange": <string(date-time)?>,
  "FirstImport": <string(date-time)?>,
  "PublishedOn": [],
  "Source": <string?>,
  "Mapping": {
    ...
  }
}
```

*Schema: `ExperienceAreaLinked`*

---

#### GET Region List
**Endpoint:** `GET /v1/Region`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No | Pagenumber |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| idlist | string | query | No | IDFilter (Separator ',' List of data IDs), (default:'null') |
| odhtagfilter | string | query | No | Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null') |
| active | boolean | query | No | Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null') |
| odhactive | boolean | query | No | Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null') |
| source | string | query | No |  |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `RegionLinkedJsonResult`*

---

#### GET Region Single
**Endpoint:** `GET /v1/Region/{id}`
**Operation ID:** `SingleRegion`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the requested data |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "ODHTags": [],
  "OdhActive": <boolean>,
  "SkiAreas": [],
  "GpsInfo": [],
  "Gpstype": <string?>,
  "Latitude": <number(double)>,
  "Longitude": <number(double)>,
  "Altitude": <number(double)?>,
  "AltitudeUnitofMeasure": <string?>,
  "GpsPoints": {
    ...
  },
  "DetailThemed": {
    ...
  },
  "GpsPolygon": [],
  "VisibleInSearch": <boolean>,
  "SkiareaIds": [],
  "RelatedContent": [],
  "Id": <string?>,
  "Active": <boolean>,
  "CustomId": <string?>,
  "Shortname": <string?>,
  "Detail": {
    ...
  },
  "ContactInfos": {
    ...
  },
  "ImageGallery": [],
  "SmgTags": [],
  "SmgActive": <boolean>,
  "HasLanguage": [],
  "LastChange": <string(date-time)?>,
  "FirstImport": <string(date-time)?>,
  "PublishedOn": [],
  "Source": <string?>,
  "Mapping": {
    ...
  }
}
```

*Schema: `RegionLinked`*

---

#### GET TourismAssociation List
**Endpoint:** `GET /v1/TourismAssociation`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No | Pagenumber |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| idlist | string | query | No | IDFilter (Separator ',' List of data IDs), (default:'null') |
| odhtagfilter | string | query | No | Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null') |
| active | boolean | query | No | Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null') |
| odhactive | boolean | query | No | Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null') |
| source | string | query | No |  |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `TourismvereinLinkedJsonResult`*

---

#### GET TourismAssociation Single
**Endpoint:** `GET /v1/TourismAssociation/{id}`
**Operation ID:** `SingleTourismAssociation`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the requested data |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "ODHTags": [],
  "OdhActive": <boolean>,
  "SkiAreas": [],
  "GpsInfo": [],
  "Gpstype": <string?>,
  "Latitude": <number(double)>,
  "Longitude": <number(double)>,
  "Altitude": <number(double)?>,
  "AltitudeUnitofMeasure": <string?>,
  "GpsPoints": {
    ...
  },
  "RegionId": <string?>,
  "GpsPolygon": [],
  "VisibleInSearch": <boolean>,
  "SkiareaIds": [],
  "RelatedContent": [],
  "Id": <string?>,
  "Active": <boolean>,
  "CustomId": <string?>,
  "Shortname": <string?>,
  "Detail": {
    ...
  },
  "ContactInfos": {
    ...
  },
  "ImageGallery": [],
  "SmgTags": [],
  "SmgActive": <boolean>,
  "HasLanguage": [],
  "LastChange": <string(date-time)?>,
  "FirstImport": <string(date-time)?>,
  "PublishedOn": [],
  "Source": <string?>,
  "Mapping": {
    ...
  }
}
```

*Schema: `TourismvereinLinked`*

---

#### GET Municipality List
**Endpoint:** `GET /v1/Municipality`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No | Pagenumber |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| visibleinsearch | boolean | query | No | Filter only Elements flagged with visibleinsearch: (possible values: 'true','false'), (default:'false') |
| idlist | string | query | No | IDFilter (Separator ',' List of data IDs), (default:'null') |
| odhtagfilter | string | query | No | Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null') |
| active | boolean | query | No | Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null') |
| odhactive | boolean | query | No | Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null') |
| source | string | query | No |  |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `MunicipalityLinkedJsonResult`*

---

#### GET Municipality Single
**Endpoint:** `GET /v1/Municipality/{id}`
**Operation ID:** `SingleMunicipality`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the requested data |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "ODHTags": [],
  "OdhActive": <boolean>,
  "GpsInfo": [],
  "Gpstype": <string?>,
  "Latitude": <number(double)>,
  "Longitude": <number(double)>,
  "Altitude": <number(double)?>,
  "AltitudeUnitofMeasure": <string?>,
  "GpsPoints": {
    ...
  },
  "Plz": <string?>,
  "RegionId": <string?>,
  "TourismvereinId": <string?>,
  "SiagId": <string?>,
  "GpsPolygon": [],
  "VisibleInSearch": <boolean>,
  "Inhabitants": <integer(int32)>,
  "IstatNumber": <string?>,
  "RelatedContent": [],
  "Id": <string?>,
  "Active": <boolean>,
  "CustomId": <string?>,
  "Shortname": <string?>,
  "Detail": {
    ...
  },
  "ContactInfos": {
    ...
  },
  "ImageGallery": [],
  "SmgTags": [],
  "SmgActive": <boolean>,
  "HasLanguage": [],
  "LastChange": <string(date-time)?>,
  "FirstImport": <string(date-time)?>,
  "PublishedOn": [],
  "Source": <string?>,
  "Mapping": {
    ...
  }
}
```

*Schema: `MunicipalityLinked`*

---

#### GET District List
**Endpoint:** `GET /v1/District`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No | Pagenumber |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| idlist | string | query | No | IDFilter (Separator ',' List of data IDs), (default:'null') |
| odhtagfilter | string | query | No | Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null') |
| active | boolean | query | No | Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null') |
| odhactive | boolean | query | No | Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null') |
| source | string | query | No |  |
| visibleinsearch | boolean | query | No | Filter only Elements flagged with visibleinsearch: (possible values: 'true','false'), (default:'false') |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `DistrictLinkedJsonResult`*

---

#### GET District Single
**Endpoint:** `GET /v1/District/{id}`
**Operation ID:** `SingleDistrict`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the requested data |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "ODHTags": [],
  "OdhActive": <boolean>,
  "GpsInfo": [],
  "Gpstype": <string?>,
  "Latitude": <number(double)>,
  "Longitude": <number(double)>,
  "Altitude": <number(double)?>,
  "AltitudeUnitofMeasure": <string?>,
  "GpsPoints": {
    ...
  },
  "IsComune": <boolean?>,
  "RegionId": <string?>,
  "TourismvereinId": <string?>,
  "MunicipalityId": <string?>,
  "SiagId": <string?>,
  "GpsPolygon": [],
  "VisibleInSearch": <boolean>,
  "RelatedContent": [],
  "Id": <string?>,
  "Active": <boolean>,
  "CustomId": <string?>,
  "Shortname": <string?>,
  "Detail": {
    ...
  },
  "ContactInfos": {
    ...
  },
  "ImageGallery": [],
  "SmgTags": [],
  "SmgActive": <boolean>,
  "HasLanguage": [],
  "LastChange": <string(date-time)?>,
  "FirstImport": <string(date-time)?>,
  "PublishedOn": [],
  "Source": <string?>,
  "Mapping": {
    ...
  }
}
```

*Schema: `DistrictLinked`*

---

#### GET Area List
**Endpoint:** `GET /v1/Area`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No | Pagenumber |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| idlist | string | query | No | IDFilter (Separator ',' List of data IDs), (default:'null') |
| odhtagfilter | string | query | No | Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null') |
| active | boolean | query | No | Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null') |
| odhactive | boolean | query | No | Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null') |
| source | string | query | No |  |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `AreaLinkedJsonResult`*

---

#### GET Area Single
**Endpoint:** `GET /v1/Area/{id}`
**Operation ID:** `SingleArea`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the requested data |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "OdhActive": <boolean>,
  "Id": <string?>,
  "Active": <boolean>,
  "SmgActive": <boolean>,
  "Shortname": <string?>,
  "CustomId": <string?>,
  "RegionId": <string?>,
  "TourismvereinId": <string?>,
  "MunicipalityId": <string?>,
  "SkiAreaID": <string?>,
  "GID": <string?>,
  "LtsID": <string?>,
  "AreaType": <string?>,
  "LastChange": <string(date-time)?>,
  "FirstImport": <string(date-time)?>,
  "Mapping": {
    ...
  },
  "Source": <string?>,
  "Detail": {
    ...
  },
  "PublishedOn": []
}
```

*Schema: `AreaLinked`*

---

#### GET SkiRegion List
**Endpoint:** `GET /v1/SkiRegion`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No | Pagenumber |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| idlist | string | query | No | IDFilter (Separator ',' List of data IDs), (default:'null') |
| odhtagfilter | string | query | No | Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null') |
| active | boolean | query | No | Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null') |
| odhactive | boolean | query | No | Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null') |
| source | string | query | No |  |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `SkiRegionLinkedJsonResult`*

---

#### GET SkiRegion Single
**Endpoint:** `GET /v1/SkiRegion/{id}`
**Operation ID:** `SingleSkiRegion`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the requested data |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "ODHTags": [],
  "OdhActive": <boolean>,
  "GpsInfo": [],
  "Gpstype": <string?>,
  "Latitude": <number(double)>,
  "Longitude": <number(double)>,
  "Altitude": <number(double)?>,
  "AltitudeUnitofMeasure": <string?>,
  "GpsPoints": {
    ...
  },
  "GpsPolygon": [],
  "RelatedContent": [],
  "Id": <string?>,
  "Active": <boolean>,
  "CustomId": <string?>,
  "Shortname": <string?>,
  "Detail": {
    ...
  },
  "ContactInfos": {
    ...
  },
  "ImageGallery": [],
  "SmgTags": [],
  "SmgActive": <boolean>,
  "HasLanguage": [],
  "LastChange": <string(date-time)?>,
  "FirstImport": <string(date-time)?>,
  "PublishedOn": [],
  "Source": <string?>,
  "Mapping": {
    ...
  }
}
```

*Schema: `SkiRegionLinked`*

---

#### GET SkiArea List
**Endpoint:** `GET /v1/SkiArea`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No | Pagenumber |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| idlist | string | query | No | IDFilter (Separator ',' List of data IDs), (default:'null') |
| odhtagfilter | string | query | No | Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null') |
| active | boolean | query | No | Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null') |
| odhactive | boolean | query | No | Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null') |
| source | string | query | No |  |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| latitude | string | query | No | GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| longitude | string | query | No | GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| radius | string | query | No | Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a> |
| polygon | string | query | No | valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `SkiAreaLinkedJsonResult`*

---

#### GET SkiArea Single
**Endpoint:** `GET /v1/SkiArea/{id}`
**Operation ID:** `SingleSkiArea`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the requested data |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "ODHTags": [],
  "OdhActive": <boolean>,
  "Areas": [],
  "TourismAssociations": [],
  "Regions": [],
  "GpsInfo": [],
  "Gpstype": <string?>,
  "Latitude": <number(double)>,
  "Longitude": <number(double)>,
  "Altitude": <number(double)?>,
  "AltitudeUnitofMeasure": <string?>,
  "GpsPoints": {
    ...
  },
  "SkiRegionId": <string?>,
  "SkiAreaMapURL": <string?>,
  "TotalSlopeKm": <string?>,
  "SlopeKmBlue": <string?>,
  "SlopeKmRed": <string?>,
  "SlopeKmBlack": <string?>,
  "LiftCount": <string?>,
  "AreaRadius": <string?>,
  "AltitudeFrom": <integer(int32)?>,
  "AltitudeTo": <integer(int32)?>,
  "SkiRegionName": {
    ...
  },
  "AreaId": [],
  "AreaIds": [],
  "OperationSchedule": [],
  "TourismvereinIds": [],
  "RegionIds": [],
  "MunicipalityIds": [],
  "DistrictIds": [],
  "GpsPolygon": [],
  "RelatedContent": [],
  "Id": <string?>,
  "Active": <boolean>,
  "CustomId": <string?>,
  "Shortname": <string?>,
  "Detail": {
    ...
  },
  "ContactInfos": {
    ...
  },
  "ImageGallery": [],
  "SmgTags": [],
  "SmgActive": <boolean>,
  "HasLanguage": [],
  "LastChange": <string(date-time)?>,
  "FirstImport": <string(date-time)?>,
  "PublishedOn": [],
  "Source": <string?>,
  "Mapping": {
    ...
  }
}
```

*Schema: `SkiAreaLinked`*

---

#### GET Wine Awards List
**Endpoint:** `GET /v1/WineAward`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| pagenumber | integer (int32) | query | No | Pagenumber |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| idlist | string | query | No | IDFilter (Separator ',' List of data IDs), (default:'null') |
| odhtagfilter | string | query | No | Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null') |
| active | boolean | query | No | Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null') |
| odhactive | boolean | query | No | Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null') |
| source | string | query | No |  |
| wineid | string | query | No | Filter by Wine Id, (default:'null') |
| companyid | string | query | No | Filter by Company Id, (default:'null') |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| langfilter | string | query | No | Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled) |
| updatefrom | string | query | No | Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| publishedon | string | query | No | Published On Filter (Separator ',' List of publisher IDs), (default:'null') |
| searchfilter | string | query | No | String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |
| getasidarray | boolean | query | No | Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `WineLinkedJsonResult`*

---

#### GET Wine Award Single
**Endpoint:** `GET /v1/WineAward/{id}`
**Operation ID:** `SingleWineAward`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the requested data |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Self": <string?>,
  "OdhActive": <boolean>,
  "Id": <string?>,
  "Shortname": <string?>,
  "Detail": {
    ...
  },
  "Vintage": <integer(int32)>,
  "Awardyear": <integer(int32)>,
  "CustomId": <string?>,
  "CompanyId": <string?>,
  "ImageGallery": [],
  "Awards": [],
  "LastChange": <string(date-time)?>,
  "FirstImport": <string(date-time)?>,
  "Active": <boolean>,
  "SmgActive": <boolean>,
  "HasLanguage": [],
  "Source": <string?>,
  "Mapping": {
    ...
  },
  "PublishedOn": []
}
```

*Schema: `WineLinked`*

---

### METADATA

**Server:** `https://tourism.opendatahub.com`

#### GET Tourism MetaData List
**Endpoint:** `GET /v1`
**Operation ID:** `TourismApi`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No | Pagenumber (default: 1) |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| updatefrom | string | query | No | [not implemented] Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Shortname and ApiDescription in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `TourismMetaDataJsonResult`*

---

#### GET Tourism MetaData List
**Endpoint:** `GET /v1/MetaData`
**Operation ID:** `TourismApiMetaData`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| pagenumber | integer (int32) | query | No | Pagenumber (default: 1) |
| pagesize | integer | query | No | Elements per Page, (default:10) |
| seed | string | query | No | Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null) |
| updatefrom | string | query | No | [not implemented] Returns data changed after this date Format (yyyy-MM-dd), (default: 'null') |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| searchfilter | string | query | No | String to search for, Shortname and ApiDescription in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a> |
| rawfilter | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a> |
| rawsort | string | query | No | <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "TotalResults": <integer(int32)>,
  "TotalPages": <integer(int32)>,
  "CurrentPage": <integer(int32)>,
  "PreviousPage": <string?>,
  "NextPage": <string?>,
  "Seed": <string?>,
  "Items": []
}
```

*Schema: `TourismMetaDataJsonResult`*

---

#### GET TourismMetaData Single
**Endpoint:** `GET /v1/MetaData/{id}`
**Operation ID:** `SingleMetaData`

**Parameters:**

| Name | Type | In | Required | Description |
|------|------|-----|----------|-------------|
| id | string | path | Yes | ID of the MetaData |
| language | string | query | No | Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed) |
| fields | array[string] | query | No | Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a> |
| removenullvalues | boolean | query | No | Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a> (default: False) |

**Response Structure:**

```json
{
  "Id": <string?>,
  "Self": <string?>,
  "FirstImport": <string(date-time)?>,
  "LastChange": <string(date-time)?>,
  "BaseUrl": <string?>,
  "ApiFilter": [],
  "PathParam": [],
  "ApiUrl": <string?>,
  "OdhType": <string?>,
  "Type": <string?>,
  "SwaggerUrl": <string?>,
  "Deprecated": <boolean>,
  "Shortname": <string>,
  "Sources": [],
  "RecordCount": {
    ...
  },
  "Output": {
    ...
  },
  "ApiDescription": {
    ...
  },
  "PublishedOn": [],
  "ApiAccess": {
    ...
  },
  "ImageGallery": [],
  "OdhTagIds": [],
  "ODHTags": [],
  "Dataspace": <string?>,
  "Category": [],
  "DataProvider": [],
  "ApiType": <string?>
}
```

*Schema: `TourismMetaData`*

---

## BEST PRACTICES AND PATTERNS

### Pattern 1: Efficient Field Selection
```
# Get only the fields you need to reduce payload size
GET /v1/Accommodation?fields=Id,Shortname,Detail.en.Title,ImageGallery.[0]&pagesize=20
```

### Pattern 2: Combined Filtering
```
# Combine predefined filters with rawfilter for complex queries
GET /v1/Accommodation?active=true&rawfilter=and(isnotnull(ImageGallery),ne(ImageGallery,[]))&fields=Id,Shortname
```

### Pattern 3: Geographic Search
```
# Find active hotels within 5km of a point
GET /v1/Accommodation?active=true&latitude=46.5&longitude=11.35&radius=5000&typefilter=1
```

### Pattern 4: Language-Specific Content
```
# Get only English content and crop response to English
GET /v1/Article?langfilter=en&language=en&pagesize=10
```

### Pattern 5: Updated Content Tracking
```
# Get items updated after specific date
GET /v1/Event?updatefrom=2024-01-01&active=true
```

### Pattern 6: Complex Filtering with NULL Safety
```
# Always check for NULL before comparing values
GET /v1/ODHTag?rawfilter=and(isnotnull(DisplayAsCategory),eq(DisplayAsCategory,true))
```

---

## IMPLEMENTATION NOTES FOR LLM

**When building API calls:**

1. **Always use HTTPS** for production endpoints (https://tourism.opendatahub.com)
2. **Use field selection** to minimize payload size and improve performance
3. **Combine filters intelligently**:
   - Use predefined filters (active, type, category, etc.) when available
   - Use rawfilter for custom conditions not covered by predefined filters
   - Use rawsort for custom sorting (unless using geographic sorting)
4. **Handle NULL values properly**:
   - Always use `isnotnull()` before comparing fields that might be NULL
   - Use `isnull()` and `isnotnull()` instead of `eq(field, null)`
5. **Geographic filtering precedence**:
   - `latitude`/`longitude`/`radius` parameters override `rawsort`
   - Results are auto-sorted by distance when using geo filters
6. **Language handling**:
   - `langfilter` filters dataset (only items with that language)
   - `language` crops the response (shows only that language in response)
   - Use both for language-specific queries: `langfilter=en&language=en`
7. **Pagination**:
   - Default pagesize is usually 10
   - Some filters may override pagesize behavior
   - Use `pagenumber` and `pagesize` for pagination
   - Response includes `NextPage` and `PreviousPage` URLs
8. **Search performance**:
   - `searchfilter` searches across all title fields
   - Add `language` parameter to improve search performance
   - Can search by ID using searchfilter
9. **Array operations**:
   - Use `[0]`, `[1]`, etc. for specific array indices
   - Use `[*]` or `[]` for all array elements
   - Use `in()` and `nin()` for array membership checks
   - Use `likein()` for pattern matching in arrays
10. **Type casting**:
    - No automatic type conversion in rawfilter
    - Boolean fields require `true`/`false`, not `1`/`0`
    - Numbers are always interpreted as floating point
    - Strings must be quoted with single or double quotes
11. **Response structure**:
    - List endpoints return paginated wrapper with `Items` array
    - Single item endpoints return the entity directly
    - Check `TotalResults` and `TotalPages` for pagination info

**Common Pitfalls:**
- Comparing NULL values without `isnull()`/`isnotnull()` → causes PostgreSQL errors
- Using type='1' instead of type=1 for numeric comparisons
- Forgetting to URL-encode polygon parameters
- Using rawsort with geographic filters (geo filters override rawsort)
- Not quoting string values in rawfilter expressions
- Not checking response structure (list vs single item)

**Entity-Specific Notes:**

- **Accommodation**: Supports category/type/board/feature/theme filters (BITMASK values)
- **ODHActivityPoi**: Supports activity type, POI type, and area filters
- **Events**: Multiple versions (v1, v2, short) - use appropriate version for use case
- **Weather**: Extensive weather data endpoints for various locations
- **Sensors**: Use LOCAL server (localhost:8082) for sensor endpoints
- **Common**: Dataset list and metadata endpoints for discovering available data
