# ODH Content API - Swagger Analysis

This document provides comprehensive documentation of all GET endpoints in the ODH Content API.

## Summary

- **Total entities analyzed:** 18
- **Total GET endpoints:** 89

### Entities and Endpoint Counts

- **common**: 20 endpoints
- **weather**: 14 endpoints
- **accommodation**: 8 endpoints
- **eventshort**: 6 endpoints
- **odhactivitypoi**: 4 endpoints
- **event**: 4 endpoints
- **article**: 4 endpoints
- **venue**: 4 endpoints
- **sensor**: 4 endpoints
- **geo**: 4 endpoints
- **metadata**: 3 endpoints
- **eventv2**: 2 endpoints
- **venuev2**: 2 endpoints
- **tag**: 2 endpoints
- **odhtag**: 2 endpoints
- **location**: 2 endpoints
- **webcaminfo**: 2 endpoints
- **announcement**: 2 endpoints

### Common Parameters Across All Endpoints

Parameters that appear in 5 or more endpoints:

- **`fields`**: Used in 84 endpoints (94.4%)
- **`language`**: Used in 82 endpoints (92.1%)
- **`removenullvalues`**: Used in 76 endpoints (85.4%)
- **`pagenumber`**: Used in 45 endpoints (50.6%)
- **`pagesize`**: Used in 43 endpoints (48.3%)
- **`id`**: Used in 42 endpoints (47.2%)
- **`searchfilter`**: Used in 40 endpoints (44.9%)
- **`rawfilter`**: Used in 40 endpoints (44.9%)
- **`rawsort`**: Used in 40 endpoints (44.9%)
- **`seed`**: Used in 35 endpoints (39.3%)
- **`source`**: Used in 35 endpoints (39.3%)
- **`idlist`**: Used in 31 endpoints (34.8%)
- **`publishedon`**: Used in 28 endpoints (31.5%)
- **`updatefrom`**: Used in 27 endpoints (30.3%)
- **`active`**: Used in 25 endpoints (28.1%)
- **`getasidarray`**: Used in 25 endpoints (28.1%)
- **`latitude`**: Used in 24 endpoints (27.0%)
- **`longitude`**: Used in 24 endpoints (27.0%)
- **`radius`**: Used in 24 endpoints (27.0%)
- **`langfilter`**: Used in 24 endpoints (27.0%)
- **`odhactive`**: Used in 21 endpoints (23.6%)
- **`polygon`**: Used in 21 endpoints (23.6%)
- **`odhtagfilter`**: Used in 19 endpoints (21.3%)
- **`locfilter`**: Used in 13 endpoints (14.6%)
- **`type`**: Used in 6 endpoints (6.7%)
- **`enddate`**: Used in 5 endpoints (5.6%)

---

## ACCOMMODATION

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (8 total)

#### 1. GET Accommodation List

**Path:** `GET /v1/Accommodation`

**Operation ID:** `AccommodationList`

**Tags:** `Accommodation`

**Parameters:** (40 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber
  - Default: `1`

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page (If availabilitycheck set, pagesize has no effect all Accommodations are returned), (default:10)

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`categoryfilter`** (query): string - Optional
  - Description: Categoryfilter BITMASK values: 1 = (not categorized), 2 = (1star), 4 = (1flower), 8 = (1sun), 14 = (1star/1flower/1sun), 16 = (2stars), 32 = (2flowers), 64 = (2suns), 112 = (2stars/2flowers/2suns), 128 = (3stars), 256 = (3flowers), 512 = (3suns), 1024 = (3sstars), 1920 = (3stars/3flowers/3suns/3sstars), 2048 = (4stars), 4096 = (4flowers), 8192 = (4suns), 16384 = (4sstars), 30720 = (4stars/4flowers/4suns/4sstars), 32768 = (5stars), 65536 = (5flowers), 131072 = (5suns), 229376 = (5stars/5flowers/5suns), 'null' = (No Filter), (default:'null')

- **`typefilter`** (query): string - Optional
  - Description: Typefilter BITMASK values: 1 = (HotelPension), 2 = (BedBreakfast), 4 = (Farm), 8 = (Camping), 16 = (Youth), 32 = (Mountain), 64 = (Apartment), 128 = (Not defined),'null' = (No Filter), (default:'null')

- **`boardfilter`** (query): string - Optional
  - Description: Boardfilter BITMASK values: 0 = (all boards), 1 = (without board), 2 = (breakfast), 4 = (half board), 8 = (full board), 16 = (All inclusive), 'null' = (No Filter), (default:'null')

- **`featurefilter`** (query): string - Optional
  - Description: FeatureFilter BITMASK values: 1 = (Group-friendly), 2 = (Meeting rooms), 4 = (Swimming pool), 8 = (Sauna), 16 = (Garage), 32 = (Pick-up service), 64 = (WLAN), 128 = (Barrier-free), 256 = (Special menus for allergy sufferers), 512 = (Pets welcome), 'null' = (No Filter), (default:'null')

- **`featureidfilter`** (query): string - Optional
  - Description: Feature Id Filter, LIST filter over ALL Features available. Separator ',' List of Feature IDs, 'null' = (No Filter), (default:'null')

- **`themefilter`** (query): string - Optional
  - Description: Themefilter BITMASK values: 1 = (Gourmet), 2 = (At altitude), 4 = (Regional wellness offerings), 8 = (on the wheels), 16 = (With family), 32 = (Hiking), 64 = (In the vineyards), 128 = (Urban vibe), 256 = (At the ski resort), 512 = (Mediterranean), 1024 = (In the Dolomites), 2048 = (Alpine), 4096 = (Small and charming), 8192 = (Huts and mountain inns), 16384 = (Rural way of life), 32768 = (Balance), 65536 = (Christmas markets), 131072 = (Sustainability), 'null' = (No Filter), (default:'null')

- **`badgefilter`** (query): string - Optional
  - Description: BadgeFilter BITMASK values: 1 = (Belvita Wellness Hotel), 2 = (Familyhotel), 4 = (Bikehotel), 8 = (Red Rooster Farm), 16 = (Barrier free certificated), 32 = (Vitalpina Hiking Hotel), 64 = (Private Rooms in South Tyrol), 128 = (Vinum Hotels), 'null' = (No Filter), (default:'null')

- **`idfilter`** (query): string - Optional
  - Description: IDFilter LIST Separator ',' List of Accommodation IDs, 'null' = (No Filter), (default:'null')

- **`locfilter`** (query): string - Optional
  - Description: Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a>

- **`altitudefilter`** (query): string - Optional
  - Description: Altitude Range Filter SPECIAL (Separator ',' example Value: 500,1000 Altitude from 500 up to 1000 metres), (default:'null')

- **`odhtagfilter`** (query): string - Optional
  - Description: ODHTag Filter LIST (refers to Array SmgTags) (String, Separator ',' more ODHTags possible, 'null' = No Filter, available ODHTags reference to 'v1/ODHTag?validforentity=accommodation'), (default:'null')

- **`source`** (query): string - Optional

- **`odhactive`** (query): boolean - Optional
  - Description: ODHActive Filter BOOLEAN (refers to field SmgActive) (possible Values: 'null' Displays all Accommodations, 'true' only ODH Active Accommodations, 'false' only ODH Disabled Accommodations), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: TIC Active Filter BOOLEAN (possible Values: 'null' Displays all Accommodations, 'true' only TIC Active Accommodations, 'false' only TIC Disabled Accommodations), (default:'null')

- **`bookablefilter`** (query): boolean - Optional

- **`arrival`** (query): string - Optional
  - Description: Arrival DATE (yyyy-MM-dd) REQUIRED ON Availabilitycheck = true, (default:'Today's date')

- **`departure`** (query): string - Optional
  - Description: Departure DATE (yyyy-MM-dd) REQUIRED ON Availabilitycheck = true, (default:'Tomorrow's date')

- **`roominfo`** (query): string - Optional
  - Description: Roominfo Filter REQUIRED ON Availabilitycheck = true (Splitter for Rooms '|' Splitter for Persons Ages ',') (Room Types: 0=notprovided, 1=room, 2=apartment, 4=pitch/tent(onlyLTS), 8=dorm(onlyLTS)) possible Values Example 1-18,10|1-18 = 2 Rooms, Room 1 for 2 person Age 18 and Age 10, Room 2 for 1 Person Age 18), (default:'1-18,18')
  - Default: `1-18,18`

- **`bokfilter`** (query): string - Optional
  - Description: Booking Channels Filter REQUIRED ON Availabilitycheck = true (Separator ',' possible values: hgv = (Booking Südtirol), htl = (Hotel.de), exp = (Expedia), bok = (Booking.com), lts = (LTS Availability check)), (default:'hgv')
  - Default: `hgv`

- **`msssource`** (query): string - Optional
  - Description: Source for MSS availability check, (default:'sinfo')
  - Default: `sinfo`

- **`availabilitychecklanguage`** (query): string - Optional
  - Description: Language of the Availability Response (possible values: 'de','it','en')
  - Default: `en`

- **`detail`** (query): string - Optional
  - Description: Detail of the Availablity check (string, 1 = full Details, 0 = basic Details (default))
  - Default: `0`

- **`availabilitycheck`** (query): boolean - Optional
  - Description: Availability Check BOOLEAN (possible Values: 'true', 'false), (default Value: 'false') NOT AVAILABLE AS OPEN DATA, IF Availabilty Check is true certain filters are Required

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language, possible values: 'de|it|en|nl|cs|pl|fr|ru' only one language supported (default:'null' all languages are displayed)

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **401**: Unauthorized
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET Accommodation Single

**Path:** `GET /v1/Accommodation/{id}`

**Operation ID:** `SingleAccommodation`

**Tags:** `Accommodation`

**Parameters:** (14 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the Accommodation

**Query Parameters:**

- **`idsource`** (query): string - Optional
  - Description: ID Source Filter (possible values:'lts','hgv','a0r_id'), (default:'lts')
  - Default: `lts`

- **`availabilitychecklanguage`** (query): string - Optional
  - Description: Language of the Availability Response (possible values: 'de','it','en')
  - Default: `en`

- **`boardfilter`** (query): string - Optional
  - Description: Boardfilter BITMASK values: 0 = (all boards), 1 = (without board), 2 = (breakfast), 4 = (half board), 8 = (full board), 16 = (All inclusive), 'null' = (No Filter), (default:'null')

- **`arrival`** (query): string - Optional
  - Description: Arrival Date (yyyy-MM-dd) REQUIRED, (default:'Today')

- **`departure`** (query): string - Optional
  - Description: Departure Date (yyyy-MM-dd) REQUIRED, (default:'Tomorrow')

- **`roominfo`** (query): string - Optional
  - Description: Roominfo Filter REQUIRED (Splitter for Rooms '|' Splitter for Persons Ages ',') (Room Types: 0=notprovided, 1=room, 2=apartment, 4=pitch/tent(onlyLTS), 8=dorm(onlyLTS)) possible Values Example 1-18,10|1-18 = 2 Rooms, Room 1 for 2 person Age 18 and Age 10, Room 2 for 1 Person Age 18), (default:'1-18,18')
  - Default: `1-18,18`

- **`bokfilter`** (query): string - Optional
  - Description: Booking Channels Filter REQUIRED (Separator ',' possible values: hgv = (Booking Südtirol), htl = (Hotel.de), exp = (Expedia), bok = (Booking.com), lts = (LTS Availability check)), (default:'hgv')
  - Default: `hgv`

- **`msssource`** (query): string - Optional
  - Default: `sinfo`

- **`availabilitycheck`** (query): boolean - Optional
  - Description: Availability Check enabled/disabled (possible Values: 'true', 'false), (default Value: 'false') NOT AVAILABLE AS OPEN DATA

- **`detail`** (query): string - Optional
  - Description: Detail of the Availablity check (string, 1 = full Details, 0 = basic Details (default))
  - Default: `0`

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **401**: Unauthorized
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 3. GET Accommodation Types List

**Path:** `GET /v1/AccommodationTypes`

**Tags:** `Accommodation`

**Parameters:** (11 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language, possible values: 'de|it|en|nl|cs|pl|fr|ru' only one language supported (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional

- **`pagesize`** (query): integer - Optional

- **`idlist`** (query): string - Optional

- **`seed`** (query): string - Optional

- **`type`** (query): string - Optional
  - Description: Type to filter for ('Board','Type','Theme','Category','Badge','SpecialFeature')

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 4. GET Accommodation Types Single

**Path:** `GET /v1/AccommodationTypes/{id}`

**Operation ID:** `SingleAccommodationTypes`

**Tags:** `Accommodation`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the AccommodationType

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language, possible values: 'de|it|en|nl|cs|pl|fr|ru' only one language supported (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 5. GET Accommodation Feature List (LTS Features)

**Path:** `GET /v1/AccommodationFeatures`

**Tags:** `Accommodation`

**Parameters:** (12 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language, possible values: 'de|it|en|nl|cs|pl|fr|ru' only one language supported (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional

- **`pagesize`** (query): integer - Optional

- **`idlist`** (query): string - Optional

- **`seed`** (query): string - Optional

- **`ltst0idfilter`** (query): string - Optional
  - Description: Filtering by LTS T0ID, filter behaviour is "startswith" so it is possible to send only one character, (default: blank)

- **`source`** (query): string - Optional
  - Description: IF source = "lts" the Features list is returned in XML Format directly from LTS, (default: blank)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 6. GET Accommodation Feature Single (LTS Features)

**Path:** `GET /v1/AccommodationFeatures/{id}`

**Operation ID:** `SingleAccommodationFeatures`

**Tags:** `Accommodation`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the AccommodationFeature

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language, possible values: 'de|it|en|nl|cs|pl|fr|ru' only one language supported (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 7. GET Accommodation Room Info by Accommodation

**Path:** `GET /v1/AccommodationRoom`

**Operation ID:** `AccommodationRoomList`

**Tags:** `Accommodation`

**Parameters:** (17 total)

**Query Parameters:**

- **`accoid`** (query): string - Optional
  - Description: Accommodation ID

- **`idsource`** (query): string - Optional
  - Description: HGV ID or LTS ID of the Accommodation (possible values:'lts','hgv','a0r_id'), (default:'lts')
  - Default: `lts`

- **`source`** (query): string - Optional
  - Description: Source Filter (possible values:'lts','hgv'), (default:null)

- **`pagenumber`** (query): integer (int32) - Optional

- **`pagesize`** (query): integer - Optional

- **`idlist`** (query): string - Optional

- **`getall`** (query): boolean - Optional
  - Description: Get Rooms from all sources (If an accommodation is bookable on Booking Southtyrol, rooms from this source are returned, setting getall to true returns also LTS Rooms), (default:false)
  - Default: `False`

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`seed`** (query): string - Optional

- **`publishedon`** (query): string - Optional

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 8. GET Accommodation Room Info Single

**Path:** `GET /v1/AccommodationRoom/{id}`

**Operation ID:** `SingleAccommodationRoom`

**Tags:** `Accommodation`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: AccommodationRoom ID

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## ODHACTIVITYPOI

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (4 total)

#### 1. GET ODHActivityPoi List

**Path:** `GET /v1/ODHActivityPoi`

**Operation ID:** `GetODHActivityPoiList`

**Tags:** `ODHActivityPoi`

**Parameters:** (42 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber
  - Default: `1`

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`type`** (query): string - Optional
  - Description: Type of the ODHActivityPoi ('null' = Filter disabled, possible values: BITMASK: 1 = Wellness, 2 = Winter, 4 = Summer, 8 = Culture, 16 = Other, 32 = Gastronomy, 64 = Mobility, 128 = Shops and services), (default: 255 == ALL), refers to <a href="https://tourism.opendatahub.com/v1/ODHActivityPoiTypes?rawfilter=eq(Type,%27Type%27)" target="_blank">ODHActivityPoi Types</a>, Type: Type
  - Default: `255`

- **`activitytype`** (query): string - Optional
  - Description: Filtering by Activity Type defined by LTS ('null' = Filter disabled, possible values: BITMASK: 'Mountains = 1','Cycling = 2','Local tours = 4','Horses = 8','Hiking = 16','Running and fitness = 32','Cross-country ski-track = 64','Tobbogan run = 128','Slopes = 256','Lifts = 512'), (default:'1023' == ALL), , refers to <a href="https://tourism.opendatahub.com/v1/ActivityTypes?rawfilter=eq(Type,%27Type%27)" target="_blank">ActivityTypes</a>, Type: Type

- **`poitype`** (query): string - Optional
  - Description: Filtering by Poi Type defined by LTS ('null' = Filter disabled, possible values: BITMASK 'Doctors, Pharmacies = 1','Shops = 2','Culture and sights= 4','Nightlife and entertainment = 8','Public institutions = 16','Sports and leisure = 32','Traffic and transport = 64', 'Service providers' = 128, 'Craft' = 256, 'Associations' = 512, 'Companies' = 1024), (default:'2047' == ALL), , refers to <a href="https://tourism.opendatahub.com/v1/PoiTypes?rawfilter=eq(Type,%27Type%27)" target="_blank">PoiTypes</a>, Type: Type

- **`subtype`** (query): string - Optional
  - Description: Subtype of the ODHActivityPoi ('null' = Filter disabled, BITMASK Filter, available SubTypes depends on the selected Maintype) <a href="https://tourism.opendatahub.com/v1/ODHActivityPoiTypes?rawfilter=eq(Type,%27SubType%27)" target="_blank">ODHActivityPoi SubTypes</a>, or <a href="https://tourism.opendatahub.com/v1/ActivityTypes?rawfilter=eq(Type,%27SubType%27)" target="_blank">Activity SubTypes</a>, or <a href="https://tourism.opendatahub.com/v1/PoiTypes?rawfilter=eq(Type,%27SubType%27)" target="_blank">Poi SubTypes</a>, Type: SubType

- **`level3type`** (query): string - Optional
  - Description: Additional Type of Level 3 the ODHActivityPoi ('null' = Filter disabled, BITMASK Filter, available SubTypes depends on the selected Maintype, SubType reference to ODHActivityPoiTypes)

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of ODHActivityPoi IDs), (default:'null')

- **`locfilter`** (query): string - Optional
  - Description: Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a>

- **`langfilter`** (query): string - Optional
  - Description: ODHActivityPoi Langfilter (returns only SmgPois available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`areafilter`** (query): string - Optional
  - Description: AreaFilter (Alternate Locfilter, can be combined with locfilter) (Separator ',' possible values: reg + REGIONID = (Filter by Region), tvs + TOURISMASSOCIATIONID = (Filter by Tourismassociation), skr + SKIREGIONID = (Filter by Skiregion), ska + SKIAREAID = (Filter by Skiarea), are + AREAID = (Filter by LTS Area), 'null' = No Filter), (default:'null')

- **`highlight`** (query): boolean - Optional
  - Description: Hightlight Filter (possible values: 'false' = only ODHActivityPoi with Highlight false, 'true' = only ODHActivityPoi with Highlight true), (default:'null')

- **`source`** (query): string - Optional
  - Description: Source Filter (possible Values: 'null' Displays all ODHActivityPoi, 'None', 'ActivityData', 'PoiData', 'GastronomicData', 'MuseumData', 'Magnolia', 'Content', 'SuedtirolWein', 'ArchApp'), (default:'null')

- **`odhtagfilter`** (query): string - Optional
  - Description: ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible (OR FILTER), available Tags reference to 'v1/ODHTag?validforentity=odhactivitypoi'), (default:'null')

- **`odhtagfilter_and`** (query): string - Optional
  - Description: ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible (AND FILTER), available Tags reference to 'v1/ODHTag?validforentity=odhactivitypoi'), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: ODH Active (Published) ODHActivityPoi Filter (Refers to field OdhActive) (possible Values: 'true' only published ODHActivityPoi, 'false' only not published ODHActivityPoi), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active ODHActivityPoi Filter (possible Values: 'true' only active ODHActivityPoi, 'false' only not active ODHActivityPoi), (default:'null')

- **`categorycodefilter`** (query): string - Optional
  - Description: CategoryCode Filter (Only for ODHActivityTypes of type Gastronomy) (BITMASK) refers to <a href="https://tourism.opendatahub.com/v1/GastronomyTypes?rawfilter=eq(Type,\"CategoryCodes\")" target="_blank">GastronomyTypes</a>, Type: CategoryCodes

- **`dishcodefilter`** (query): string - Optional
  - Description: DishCode Filter (Only for ODHActivityTypes of type Gastronomy) (BITMASK) refers to <a href="https://tourism.opendatahub.com/v1/GastronomyTypes" target="_blank">GastronomyTypes</a>, Type: DishCodes

- **`ceremonycodefilter`** (query): string - Optional
  - Description: CeremonyCode Filter (Only for ODHActivityTypes of type Gastronomy) (BITMASK) refers to <a href="https://tourism.opendatahub.com/v1/GastronomyTypes" target="_blank">GastronomyTypes</a>, Type: CeremonyCodes

- **`facilitycodefilter`** (query): string - Optional
  - Description: FacilityCode Filter (Only for ODHActivityTypes of type Gastronomy) (BITMASK) refers to <a href="https://tourism.opendatahub.com/v1/GastronomyTypes" target="_blank">GastronomyTypes</a>, Type: with FacilityCodes_ prefix

- **`cuisinecodefilter`** (query): string - Optional
  - Description: CuisineCode Filter (Only for ODHActivityTypes of type Gastronomy) (BITMASK) refers to <a href="https://tourism.opendatahub.com/v1/GastronomyTypes" target="_blank">GastronomyTypes</a>, Type: CuisineCodes

- **`difficultyfilter`** (query): string - Optional
  - Description: Difficulty Filter (possible values: '1' = easy, '2' = medium, '3' = difficult), (default:'null')

- **`distancefilter`** (query): string - Optional
  - Description: Distance Range Filter (Separator ',' example Value: 15,40 Distance from 15 up to 40 Km), (default:'null')

- **`altitudefilter`** (query): string - Optional
  - Description: Altitude Range Filter (Separator ',' example Value: 500,1000 Altitude from 500 up to 1000 metres), (default:'null')

- **`durationfilter`** (query): string - Optional
  - Description: Duration Range Filter (Separator ',' example Value: 1,3 Duration from 1 to 3 hours), (default:'null')

- **`hasimage`** (query): boolean - Optional

- **`tagfilter`** (query): string - Optional
  - Description: Filter on Tags. Syntax =and/or(TagSource.TagId,TagSource.TagId,TagId) example or(idm.summer,lts.hiking) - and(idm.themed hikes,lts.family hikings) - or(hiking) - and(idm.summer) - Combining and/or is not supported at the moment, default: 'null')

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null')

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET ODHActivityPoi Single

**Path:** `GET /v1/ODHActivityPoi/{id}`

**Operation ID:** `SingleODHActivityPoi`

**Tags:** `ODHActivityPoi`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the Poi

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 3. GET ODHActivityPoi Types List

**Path:** `GET /v1/ODHActivityPoiTypes`

**Tags:** `ODHActivityPoi`

**Parameters:** (10 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional

- **`pagesize`** (query): integer - Optional

- **`idlist`** (query): string - Optional

- **`seed`** (query): string - Optional

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 4. GET ODHActivityPoi Types Single

**Path:** `GET /v1/ODHActivityPoiTypes/{id}`

**Operation ID:** `SingleODHActivityPoiTypes`

**Tags:** `ODHActivityPoi`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the ODHActivityPoi Type

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## EVENT

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (4 total)

#### 1. GET Event List

**Path:** `GET /v1/Event`

**Tags:** `Event`

**Parameters:** (29 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber
  - Default: `1`

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of Event IDs, 'null' = No Filter), (default:'null')

- **`locfilter`** (query): string - Optional
  - Description: Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a>

- **`rancfilter`** (query): string - Optional
  - Description: Rancfilter, Return only Events with this Ranc assigned (1 = not visible, 3 = visible, 4 = important, 5 = top-event),(default: 'null')

- **`topicfilter`** (query): string - Optional
  - Description: Topic ID Filter (Filter by Topic ID) BITMASK refers to 'v1/EventTopics',(default: 'null')

- **`orgfilter`** (query): string - Optional
  - Description: Organization Filter (Filter by Organizer RID)

- **`odhtagfilter`** (query): string - Optional
  - Description: ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=event'), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active Events Filter (possible Values: 'true' only Active Events, 'false' only Disabled Events), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: ODH Active (Published) Events Filter (Refers to field OdhActive) Events Filter (possible Values: 'true' only published Events, 'false' only not published Events), (default:'null')

- **`begindate`** (query): string - Optional
  - Description: BeginDate of Events (Format: yyyy-MM-dd), (default: 'null')

- **`enddate`** (query): string - Optional
  - Description: EndDate of Events (Format: yyyy-MM-dd), (default: 'null')

- **`sort`** (query): string - Optional
  - Description: Sorting Mode of Events ('asc': Ascending simple sort by next begindate, 'desc': simple descent sorting by next begindate, 'upcoming': Sort Events by next EventDate matching passed startdate, 'upcomingspecial': Sort Events by next EventDate matching passed startdate, multiple day events are showed at bottom, default: if no sort mode passed, sort by shortname )

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`source`** (query): string - Optional
  - Description: Filter by Source (Separator ','), (Sources available 'lts','trevilab','drin'),(default: 'null')

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET Event Single

**Path:** `GET /v1/Event/{id}`

**Operation ID:** `SingleEvent`

**Tags:** `Event`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the Event

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 3. GET Event Topic List

**Path:** `GET /v1/EventTopics`

**Tags:** `Event`

**Parameters:** (10 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional

- **`pagesize`** (query): integer - Optional

- **`idlist`** (query): string - Optional

- **`seed`** (query): string - Optional

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 4. GET Event Topic Single

**Path:** `GET /v1/EventTopics/{id}`

**Operation ID:** `SingleEventTopics`

**Tags:** `Event`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the Event

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## EVENTV2

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (2 total)

#### 1. GET Event List

**Path:** `GET /v2/Event`

**Tags:** `EventV2`

**Parameters:** (25 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber
  - Default: `1`

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of Event IDs, 'null' = No Filter), (default:'null')

- **`venueidfilter`** (query): string - Optional

- **`locfilter`** (query): string - Optional
  - Description: Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a>

- **`tagfilter`** (query): string - Optional

- **`active`** (query): boolean - Optional
  - Description: Active Events Filter (possible Values: 'true' only Active Events, 'false' only Disabled Events), (default:'null')

- **`begindate`** (query): string - Optional
  - Description: BeginDate of Events (Format: yyyy-MM-dd), (default: 'null')

- **`enddate`** (query): string - Optional
  - Description: EndDate of Events (Format: yyyy-MM-dd), (default: 'null')

- **`sort`** (query): string - Optional
  - Description: Sorting Mode of Events ('asc': Ascending simple sort by next begindate, 'desc': simple descent sorting by next begindate, 'upcoming': Sort Events by next EventDate matching passed startdate, 'upcomingspecial': Sort Events by next EventDate matching passed startdate, multiple day events are showed at bottom, default: if no sort mode passed, sort by shortname )

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`source`** (query): string - Optional
  - Description: Filter by Source (Separator ','), (Sources available 'lts','trevilab','drin'),(default: 'null')

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET Event Single

**Path:** `GET /v2/Event/{id}`

**Operation ID:** `SingleEventV2`

**Tags:** `EventV2`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the Event

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## EVENTSHORT

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (6 total)

#### 1. GET EventShort List

**Path:** `GET /v1/EventShort`

**Tags:** `EventShort`

**Parameters:** (30 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber (Integer)
  - Default: `1`

- **`pagesize`** (query): integer - Optional
  - Description: Pagesize (Integer), (default: 'null')

- **`startdate`** (query): string - Optional
  - Description: Format (yyyy-MM-dd HH:mm) default or Unix Timestamp

- **`enddate`** (query): string - Optional
  - Description: Format (yyyy-MM-dd HH:mm) default or Unix Timestamp

- **`datetimeformat`** (query): string - Optional
  - Description: not provided, use default format, for unix timestamp pass "uxtimestamp"

- **`source`** (query): string - Optional
  - Description: Source of the data, (possible values 'Content' or 'EBMS')

- **`eventlocation`** (query): string - Optional
  - Description: <p>Members:</p><ul><li><i>NOI</i> - NOI Techpark</li> <li><i>EC</i> - Eurac</li> <li><i>VV</i> - Virtual Village</li> <li><i>OUT</i> - Other Location</li> </ul>
  - Allowed values: `NOI`, `EC`, `VV`, `OUT`

- **`onlyactive`** (query): boolean - Optional
  - Description: 'true' if only Events marked as Active for today.noi.bz.it should be returned

- **`websiteactive`** (query): boolean - Optional
  - Description: 'true' if only Events marked as Active for noi.bz.it should be returned

- **`communityactive`** (query): boolean - Optional
  - Description: 'true' if only Events marked as Active for Noi community should be returned

- **`active`** (query): boolean - Optional
  - Description: Active Events Filter (possible Values: 'true' only Active Events, 'false' only Disabled Events), (default:'true')
  - Default: `True`

- **`eventids`** (query): string - Optional
  - Description: comma separated list of event ids

- **`webaddress`** (query): string - Optional
  - Description: Searches the webaddress

- **`sortorder`** (query): string - Optional
  - Description: ASC or DESC by StartDate
  - Default: `ASC`

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`optimizedates`** (query): boolean - Optional
  - Description: Optimizes dates, cuts out all Rooms with Comment "x", revisits and corrects start + enddate
  - Default: `False`

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`lastchange`** (query): string - Optional

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET EventShort Single

**Path:** `GET /v1/EventShort/Detail/{id}`

**Tags:** `EventShort`

**Parameters:** (5 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: Id of the Event

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`optimizedates`** (query): boolean - Optional
  - Description: Optimizes dates, cuts out all Rooms with Comment "x", revisits and corrects start + enddate
  - Default: `False`

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 3. GET EventShort Single

**Path:** `GET /v1/EventShort/{id}`

**Operation ID:** `SingleEventShort`

**Tags:** `EventShort`

**Parameters:** (5 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: Id of the Event

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`optimizedates`** (query): boolean - Optional
  - Description: Optimizes dates, cuts out all Rooms with Comment "x", revisits and corrects start + enddate
  - Default: `False`

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 4. GET EventShort List by Room Occupation

**Path:** `GET /v1/EventShort/GetbyRoomBooked`

**Tags:** `EventShort`

**Parameters:** (22 total)

**Query Parameters:**

- **`startdate`** (query): string - Optional
  - Description: Format (yyyy-MM-dd HH:mm) default or Unix Timestamp

- **`enddate`** (query): string - Optional
  - Description: Format (yyyy-MM-dd HH:mm) default or Unix Timestamp

- **`datetimeformat`** (query): string - Optional
  - Description: not provided, use default format, for unix timestamp pass "uxtimestamp"

- **`source`** (query): string - Optional
  - Description: Source of the data, (possible values 'Content' or 'EBMS')

- **`eventlocation`** (query): string - Optional
  - Description: <p>Members:</p><ul><li><i>NOI</i> - NOI Techpark</li> <li><i>EC</i> - Eurac</li> <li><i>VV</i> - Virtual Village</li> <li><i>OUT</i> - Other Location</li> </ul>
  - Allowed values: `NOI`, `EC`, `VV`, `OUT`

- **`onlyactive`** (query): boolean - Optional
  - Description: 'true' if only Events marked as Active for today.noi.bz.it should be returned

- **`websiteactive`** (query): boolean - Optional
  - Description: 'true' if only Events marked as Active for noi.bz.it should be returned

- **`communityactive`** (query): boolean - Optional
  - Description: 'true' if only Events marked as Active for Noi community should be returned

- **`active`** (query): boolean - Optional
  - Description: Active Events Filter (possible Values: 'true' only Active Events, 'false' only Disabled Events), (default:'true')
  - Default: `True`

- **`eventids`** (query): string - Optional
  - Description: comma separated list of event ids

- **`webaddress`** (query): string - Optional
  - Description: Filter by WebAddress Field

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`lastchange`** (query): string - Optional

- **`updatefrom`** (query): string - Optional

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`eventgrouping`** (query): boolean - Optional
  - Description: Groups Events with the Same Date/Id/Name and adds all Rooms to the SpaceDesc List
  - Default: `True`

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 5. GET EventShort Types

**Path:** `GET /v1/EventShortTypes`

**Tags:** `EventShort`

**Parameters:** (11 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional

- **`pagesize`** (query): integer - Optional

- **`idlist`** (query): string - Optional

- **`seed`** (query): string - Optional

- **`type`** (query): string - Optional
  - Description: Type to filter for ('TechnologyFields','CustomTagging')

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 6. GET EventShort Type Single

**Path:** `GET /v1/EventShortTypes/{id}`

**Operation ID:** `SingleEventShortTypes`

**Tags:** `EventShort`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the EventShort Type

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## ARTICLE

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (4 total)

#### 1. GET Article List

**Path:** `GET /v1/Article`

**Tags:** `Article`

**Parameters:** (23 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber
  - Default: `1`

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`articletype`** (query): string - Optional
  - Description: Type of the Article ('null' = Filter disabled, possible values: BITMASK values: 1 = basearticle, 2 = book article, 4 = contentarticle, 8 = eventarticle, 16 = pressarticle, 32 = recipe, 64 = touroperator , 128 = b2b, 256  = idmarticle, 512 = specialannouncement, 1024 = newsfeednoi), (also possible for compatibily reasons: basisartikel, buchtippartikel, contentartikel, veranstaltungsartikel, presseartikel, rezeptartikel, reiseveranstalter, b2bartikel ) (default:'255' == ALL), REFERENCE TO: GET /api/ArticleTypes

- **`articlesubtype`** (query): string - Optional
  - Description: Sub Type of the Article (depends on the Maintype of the Article 'null' = Filter disabled)

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of Article IDs), (default:'null')

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`sortbyarticledate`** (query): boolean - Optional
  - Description: Sort By Articledate ('true' sorts Articles by Articledate)

- **`odhtagfilter`** (query): string - Optional
  - Description: ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=article'), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: ODH Active (Published) Articles Filter (Refers to field OdhActive) (possible Values: 'true' only published Article, 'false' only not published Articles), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active Articles Filter (possible Values: 'true' only Active Articles, 'false' only Disabled Articles), (default:'null')

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`startdate`** (query): string - Optional
  - Description: Filter by ArticleDate Format (yyyy-MM-dd HH:mm)

- **`enddate`** (query): string - Optional
  - Description: Filter by ArticleDate Format (yyyy-MM-dd HH:mm)

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`source`** (query): string - Optional
  - Description: Filter by Source (Separator ','), (Sources available 'idm','noi'...),(default: 'null')

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET Article Single

**Path:** `GET /v1/Article/{id}`

**Operation ID:** `SingleArticle`

**Tags:** `Article`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the Article

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 3. GET Article Types List

**Path:** `GET /v1/ArticleTypes`

**Tags:** `Article`

**Parameters:** (10 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional

- **`pagesize`** (query): integer - Optional

- **`idlist`** (query): string - Optional

- **`seed`** (query): string - Optional

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 4. GET Article Types Single

**Path:** `GET /v1/ArticleTypes/{id}`

**Operation ID:** `SingleArticleTypes`

**Tags:** `Article`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the Article Type

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## VENUE

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (4 total)

#### 1. GET Venue List

**Path:** `GET /v1/Venue`

**Tags:** `Venue`

**Parameters:** (29 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber
  - Default: `1`

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page (max 1024), (default:10)

- **`categoryfilter`** (query): string - Optional
  - Description: Venue Category Filter (BITMASK) (Separator ',' List of Venuetype Bitmasks, refer to api/VenueTypes type:category), (default:'null')

- **`capacityfilter`** (query): string - Optional
  - Description: Capacity Range Filter (Separator ',' example Value: 50,100 All Venues with rooms from 50 to 100 people), (default:'null')

- **`roomcountfilter`** (query): string - Optional
  - Description: Room Count Range Filter (Separator ',' example Value: 2,5 All Venues with 2 to 5 rooms), (default:'null')

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of Venue IDs), (default:'null')

- **`locfilter`** (query): string - Optional
  - Description: Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a>

- **`featurefilter`** (query): string - Optional
  - Description: Venue Features Filter (BITMASK) (Separator ',' List of Venuetype Bitmasks, refer to api/VenueTypes type:feature), (default:'null')

- **`setuptypefilter`** (query): string - Optional
  - Description: Venue SetupType Filter (BITMASK) (Separator ',' List of Venuetype Bitmasks, refer to api/VenueTypes type:seatType), (default:'null')

- **`odhtagfilter`** (query): string - Optional
  - Description: ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=venue'), (default:'null')

- **`source`** (query): string - Optional
  - Description: Source Filter(String, ), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active Venue Filter (possible Values: 'true' only Active Venues, 'false' only Disabled Venues), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: ODH Active (Published) Venue Filter (possible Values: 'true' only published Venue, 'false' only not published Venue), (default:'null')

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null')

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null)<a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`destinationdataformat`** (query): boolean - Optional
  - Description: If set to true, data will be returned in AlpineBits Destinationdata Format
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET Venue Single

**Path:** `GET /v1/Venue/{id}`

**Operation ID:** `SingleVenue`

**Tags:** `Venue`

**Parameters:** (5 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the Venue

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`destinationdataformat`** (query): boolean - Optional
  - Description: If set to true, data will be returned in AlpineBits Destinationdata Format
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 3. GET Venue Types List

**Path:** `GET /v1/VenueTypes`

**Tags:** `Venue`

**Parameters:** (10 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional

- **`pagesize`** (query): integer - Optional

- **`idlist`** (query): string - Optional

- **`seed`** (query): string - Optional

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null)<a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 4. GET Venue Types Single

**Path:** `GET /v1/VenueTypes/{id}`

**Operation ID:** `SingleVenueTypes`

**Tags:** `Venue`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the VenueType

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname. Select also Dictionary fields, example Detail.de.Title, or Elements of Arrays example ImageGallery[0].ImageUrl. (default:'null' all fields are displayed)
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## VENUEV2

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (2 total)

#### 1. GET Venue V2 List

**Path:** `GET /v2/Venue`

**Tags:** `VenueV2`

**Parameters:** (27 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber
  - Default: `1`

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page (max 1024), (default:10)

- **`categoryfilter`** (query): string - Optional
  - Description: Venue Category Filter (BITMASK) (Separator ',' List of Venuetype Bitmasks, refer to api/VenueTypes type:category), (default:'null')

- **`capacityfilter`** (query): string - Optional
  - Description: Capacity Range Filter (Separator ',' example Value: 50,100 All Venues with rooms from 50 to 100 people), (default:'null')

- **`roomcountfilter`** (query): string - Optional
  - Description: Room Count Range Filter (Separator ',' example Value: 2,5 All Venues with 2 to 5 rooms), (default:'null')

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of Venue IDs), (default:'null')

- **`locfilter`** (query): string - Optional
  - Description: Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a>

- **`featurefilter`** (query): string - Optional
  - Description: Venue Features Filter (BITMASK) (Separator ',' List of Venuetype Bitmasks, refer to api/VenueTypes type:feature), (default:'null')

- **`setuptypefilter`** (query): string - Optional
  - Description: Venue SetupType Filter (BITMASK) (Separator ',' List of Venuetype Bitmasks, refer to api/VenueTypes type:seatType), (default:'null')

- **`odhtagfilter`** (query): string - Optional
  - Description: ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=venue'), (default:'null')

- **`source`** (query): string - Optional
  - Description: Source Filter(String, ), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active Venue Filter (possible Values: 'true' only Active Venues, 'false' only Disabled Venues), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: ODH Active (Published) Venue Filter (possible Values: 'true' only published Venue, 'false' only not published Venue), (default:'null')

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null')

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null)<a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET Venue Single

**Path:** `GET /v2/Venue/{id}`

**Operation ID:** `SingleVenueV2`

**Tags:** `VenueV2`

**Parameters:** (5 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the Venue

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`destinationdataformat`** (query): boolean - Optional
  - Description: If set to true, data will be returned in AlpineBits Destinationdata Format
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## SENSOR

### Server URLs
- http://localhost:8082

### Endpoints (4 total)

#### 1. GET Sensor List

**Path:** `GET /v1/Sensor`

**Tags:** `Sensor`

**Parameters:** (34 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber
  - Default: `1`

- **`pagesize`** (query): integer (int32) - Optional
  - Description: Elements per Page, (default:10)
  - Default: `10`

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null')

- **`sensortype`** (query): string - Optional
  - Description: Type of sensor (e.g., 'TEMP', 'HUM', 'PRESS', 'PM25'), (default:'null')

- **`manufacturer`** (query): string - Optional
  - Description: Manufacturer filter (comma-separated list), (default:'null')

- **`model`** (query): string - Optional
  - Description: Model filter (comma-separated list), (default:'null')

- **`datasetid`** (query): string - Optional
  - Description: Filter by dataset ID (comma-separated list), (default:'null')

- **`measurementtypename`** (query): string - Optional
  - Description: Filter by measurement type name (comma-separated list), (default:'null')

- **`source`** (query): string - Optional
  - Description: Source Filter (possible Values: 'null' Displays all Sensors), (default:'null')

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of Sensor IDs), (default:'null')

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only Sensors available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`odhtagfilter`** (query): string - Optional

- **`publishedon`** (query): string - Optional

- **`active`** (query): boolean - Optional
  - Description: Active Sensor Filter (possible Values: 'true' only active Sensors, 'false' only not active Sensors), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: ODH Active (Published) Sensor Filter (Refers to field SmgActive) (possible Values: 'true' only published Sensors, 'false' only not published Sensors), (default:'null')

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`updatefrom`** (query): string (date-time) - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed)
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, searches in sensor name, type, and details, (default: null)

- **`rawfilter`** (query): string - Optional
  - Description: Raw filter for advanced querying

- **`rawsort`** (query): string - Optional
  - Description: Raw sort for advanced sorting

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false.
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)
  - Default: `False`

- **`tsdatasetids`** (query): string - Optional
  - Description: Timeseries dataset filter (comma-separated dataset IDs), (default:'null')

- **`tsrequiredtypes`** (query): string - Optional
  - Description: Timeseries required types filter (comma-separated type names, sensor must have ALL), (default:'null')

- **`tsoptionaltypes`** (query): string - Optional
  - Description: Timeseries optional types filter (comma-separated type names, sensor may have ANY), (default:'null')

- **`tsmeasurementexpr`** (query): string - Optional
  - Description: Timeseries measurement expression (e.g., 'or(o2.eq.2, and(temp.gteq.20, temp.lteq.30))'), (default:'null')

- **`tslatestonly`** (query): boolean - Optional
  - Description: Timeseries latest only filter (only consider latest measurements), (default:null)

- **`tsstarttime`** (query): string - Optional
  - Description: Timeseries start time filter (RFC3339 format), (default:'null')

- **`tsendtime`** (query): string - Optional
  - Description: Timeseries end time filter (RFC3339 format), (default:'null')

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET Sensor Single

**Path:** `GET /v1/Sensor/{id}`

**Operation ID:** `SingleSensor`

**Tags:** `Sensor`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the Sensor

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed)
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false.
  - Default: `False`

**Responses:**

- **200**: Sensor found
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **404**: Sensor not found
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 3. GET Distinct Types from Sensor Discovery

**Path:** `GET /v1/Sensor/discovery/types/distinct`

**Tags:** `Sensor`

**Parameters:** (28 total)

**Query Parameters:**

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null')

- **`sensortype`** (query): string - Optional
  - Description: Type of sensor (e.g., 'TEMP', 'HUM', 'PRESS', 'PM25'), (default:'null')

- **`manufacturer`** (query): string - Optional
  - Description: Manufacturer filter (comma-separated list), (default:'null')

- **`model`** (query): string - Optional
  - Description: Model filter (comma-separated list), (default:'null')

- **`datasetid`** (query): string - Optional
  - Description: Filter by dataset ID (comma-separated list), (default:'null')

- **`measurementtypename`** (query): string - Optional
  - Description: Filter by measurement type name (comma-separated list), (default:'null')

- **`source`** (query): string - Optional
  - Description: Source Filter (possible Values: 'null' Displays all Sensors), (default:'null')

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of Sensor IDs), (default:'null')

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only Sensors available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`odhtagfilter`** (query): string - Optional

- **`publishedon`** (query): string - Optional

- **`active`** (query): boolean - Optional
  - Description: Active Sensor Filter (possible Values: 'true' only active Sensors, 'false' only not active Sensors), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: ODH Active (Published) Sensor Filter (Refers to field SmgActive) (possible Values: 'true' only published Sensors, 'false' only not published Sensors), (default:'null')

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null')

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null')

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, (default:'null')

- **`updatefrom`** (query): string (date-time) - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, searches in sensor name, type, and details, (default: null)

- **`rawfilter`** (query): string - Optional
  - Description: Raw filter for advanced querying

- **`rawsort`** (query): string - Optional
  - Description: Raw sort for advanced sorting

- **`tsdatasetids`** (query): string - Optional
  - Description: Timeseries dataset filter (comma-separated dataset IDs), (default:'null')

- **`tsrequiredtypes`** (query): string - Optional
  - Description: Timeseries required types filter (comma-separated type names, sensor must have ALL), (default:'null')

- **`tsoptionaltypes`** (query): string - Optional
  - Description: Timeseries optional types filter (comma-separated type names, sensor may have ANY), (default:'null')

- **`tsmeasurementexpr`** (query): string - Optional
  - Description: Timeseries measurement expression (e.g., 'or(o2.eq.2, and(temp.gteq.20, temp.lteq.30))'), (default:'null')

- **`tslatestonly`** (query): boolean - Optional
  - Description: Timeseries latest only filter (only consider latest measurements), (default:null)

- **`tsstarttime`** (query): string - Optional
  - Description: Timeseries start time filter (RFC3339 format), (default:'null')

- **`tsendtime`** (query): string - Optional
  - Description: Timeseries end time filter (RFC3339 format), (default:'null')

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 4. GET Sensor Discovery with Types

**Path:** `GET /v1/Sensor/discovery/types`

**Tags:** `Sensor`

**Parameters:** (33 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber
  - Default: `1`

- **`pagesize`** (query): integer (int32) - Optional
  - Description: Elements per Page, (default:10)
  - Default: `10`

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null')

- **`sensortype`** (query): string - Optional
  - Description: Type of sensor (e.g., 'TEMP', 'HUM', 'PRESS', 'PM25'), (default:'null')

- **`manufacturer`** (query): string - Optional
  - Description: Manufacturer filter (comma-separated list), (default:'null')

- **`model`** (query): string - Optional
  - Description: Model filter (comma-separated list), (default:'null')

- **`datasetid`** (query): string - Optional
  - Description: Filter by dataset ID (comma-separated list), (default:'null')

- **`measurementtypename`** (query): string - Optional
  - Description: Filter by measurement type name (comma-separated list), (default:'null')

- **`source`** (query): string - Optional
  - Description: Source Filter (possible Values: 'null' Displays all Sensors), (default:'null')

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of Sensor IDs), (default:'null')

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only Sensors available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`odhtagfilter`** (query): string - Optional

- **`publishedon`** (query): string - Optional

- **`active`** (query): boolean - Optional
  - Description: Active Sensor Filter (possible Values: 'true' only active Sensors, 'false' only not active Sensors), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: ODH Active (Published) Sensor Filter (Refers to field SmgActive) (possible Values: 'true' only published Sensors, 'false' only not published Sensors), (default:'null')

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null')

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null')

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, (default:'null')

- **`updatefrom`** (query): string (date-time) - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed)
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, searches in sensor name, type, and details, (default: null)

- **`rawfilter`** (query): string - Optional
  - Description: Raw filter for advanced querying

- **`rawsort`** (query): string - Optional
  - Description: Raw sort for advanced sorting

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false.
  - Default: `False`

- **`tsdatasetids`** (query): string - Optional
  - Description: Timeseries dataset filter (comma-separated dataset IDs), (default:'null')

- **`tsrequiredtypes`** (query): string - Optional
  - Description: Timeseries required types filter (comma-separated type names, sensor must have ALL), (default:'null')

- **`tsoptionaltypes`** (query): string - Optional
  - Description: Timeseries optional types filter (comma-separated type names, sensor may have ANY), (default:'null')

- **`tsmeasurementexpr`** (query): string - Optional
  - Description: Timeseries measurement expression (e.g., 'or(o2.eq.2, and(temp.gteq.20, temp.lteq.30))'), (default:'null')

- **`tslatestonly`** (query): boolean - Optional
  - Description: Timeseries latest only filter (only consider latest measurements), (default:null)

- **`tsstarttime`** (query): string - Optional
  - Description: Timeseries start time filter (RFC3339 format), (default:'null')

- **`tsendtime`** (query): string - Optional
  - Description: Timeseries end time filter (RFC3339 format), (default:'null')

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## COMMON

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (20 total)

#### 1. GET MetaRegion List

**Path:** `GET /v1/MetaRegion`

**Tags:** `Common`

**Parameters:** (22 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of data IDs), (default:'null')

- **`odhtagfilter`** (query): string - Optional
  - Description: Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')

- **`source`** (query): string - Optional

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET MetaRegion Single

**Path:** `GET /v1/MetaRegion/{id}`

**Operation ID:** `SingleMetaRegion`

**Tags:** `Common`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the requested data

**Query Parameters:**

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 3. GET Experiencearea List

**Path:** `GET /v1/ExperienceArea`

**Tags:** `Common`

**Parameters:** (23 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of data IDs), (default:'null')

- **`odhtagfilter`** (query): string - Optional
  - Description: Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')

- **`source`** (query): string - Optional

- **`visibleinsearch`** (query): boolean - Optional
  - Description: Filter only Elements flagged with visibleinsearch: (possible values: 'true','false'), (default:'false')

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 4. GET ExperienceArea Single

**Path:** `GET /v1/ExperienceArea/{id}`

**Operation ID:** `SingleExperienceArea`

**Tags:** `Common`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the requested data

**Query Parameters:**

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 5. GET Region List

**Path:** `GET /v1/Region`

**Tags:** `Common`

**Parameters:** (22 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of data IDs), (default:'null')

- **`odhtagfilter`** (query): string - Optional
  - Description: Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')

- **`source`** (query): string - Optional

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 6. GET Region Single

**Path:** `GET /v1/Region/{id}`

**Operation ID:** `SingleRegion`

**Tags:** `Common`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the requested data

**Query Parameters:**

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 7. GET TourismAssociation List

**Path:** `GET /v1/TourismAssociation`

**Tags:** `Common`

**Parameters:** (22 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of data IDs), (default:'null')

- **`odhtagfilter`** (query): string - Optional
  - Description: Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')

- **`source`** (query): string - Optional

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 8. GET TourismAssociation Single

**Path:** `GET /v1/TourismAssociation/{id}`

**Operation ID:** `SingleTourismAssociation`

**Tags:** `Common`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the requested data

**Query Parameters:**

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 9. GET Municipality List

**Path:** `GET /v1/Municipality`

**Tags:** `Common`

**Parameters:** (23 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`visibleinsearch`** (query): boolean - Optional
  - Description: Filter only Elements flagged with visibleinsearch: (possible values: 'true','false'), (default:'false')

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of data IDs), (default:'null')

- **`odhtagfilter`** (query): string - Optional
  - Description: Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')

- **`source`** (query): string - Optional

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 10. GET Municipality Single

**Path:** `GET /v1/Municipality/{id}`

**Operation ID:** `SingleMunicipality`

**Tags:** `Common`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the requested data

**Query Parameters:**

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 11. GET District List

**Path:** `GET /v1/District`

**Tags:** `Common`

**Parameters:** (23 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of data IDs), (default:'null')

- **`odhtagfilter`** (query): string - Optional
  - Description: Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')

- **`source`** (query): string - Optional

- **`visibleinsearch`** (query): boolean - Optional
  - Description: Filter only Elements flagged with visibleinsearch: (possible values: 'true','false'), (default:'false')

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 12. GET District Single

**Path:** `GET /v1/District/{id}`

**Operation ID:** `SingleDistrict`

**Tags:** `Common`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the requested data

**Query Parameters:**

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 13. GET Area List

**Path:** `GET /v1/Area`

**Tags:** `Common`

**Parameters:** (18 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of data IDs), (default:'null')

- **`odhtagfilter`** (query): string - Optional
  - Description: Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')

- **`source`** (query): string - Optional

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 14. GET Area Single

**Path:** `GET /v1/Area/{id}`

**Operation ID:** `SingleArea`

**Tags:** `Common`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the requested data

**Query Parameters:**

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 15. GET SkiRegion List

**Path:** `GET /v1/SkiRegion`

**Tags:** `Common`

**Parameters:** (22 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of data IDs), (default:'null')

- **`odhtagfilter`** (query): string - Optional
  - Description: Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')

- **`source`** (query): string - Optional

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 16. GET SkiRegion Single

**Path:** `GET /v1/SkiRegion/{id}`

**Operation ID:** `SingleSkiRegion`

**Tags:** `Common`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the requested data

**Query Parameters:**

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 17. GET SkiArea List

**Path:** `GET /v1/SkiArea`

**Tags:** `Common`

**Parameters:** (22 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of data IDs), (default:'null')

- **`odhtagfilter`** (query): string - Optional
  - Description: Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')

- **`source`** (query): string - Optional

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 18. GET SkiArea Single

**Path:** `GET /v1/SkiArea/{id}`

**Operation ID:** `SingleSkiArea`

**Tags:** `Common`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the requested data

**Query Parameters:**

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 19. GET Wine Awards List

**Path:** `GET /v1/WineAward`

**Tags:** `Common`

**Parameters:** (20 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of data IDs), (default:'null')

- **`odhtagfilter`** (query): string - Optional
  - Description: Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')

- **`source`** (query): string - Optional

- **`wineid`** (query): string - Optional
  - Description: Filter by Wine Id, (default:'null')

- **`companyid`** (query): string - Optional
  - Description: Filter by Company Id, (default:'null')

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`langfilter`** (query): string - Optional
  - Description: Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 20. GET Wine Award Single

**Path:** `GET /v1/WineAward/{id}`

**Operation ID:** `SingleWineAward`

**Tags:** `Common`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the requested data

**Query Parameters:**

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## TAG

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (2 total)

#### 1. GET Tag List

**Path:** `GET /v1/Tag`

**Tags:** `Tag`

**Parameters:** (14 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Default: `1`

- **`pagesize`** (query): integer - Optional

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`validforentity`** (query): string - Optional
  - Description: Filter on Tags valid on Entities (accommodation, activity, poi, odhactivitypoi, package, gastronomy, event, article, common .. etc..),(Separator ',' List of odhtypes) (default:'null')

- **`types`** (query): string - Optional
  - Description: Filter on Tags with this Types (Separator ',' List of types), (default:'null')

- **`displayascategory`** (query): boolean - Optional
  - Description: true = returns only Tags which are marked as DisplayAsCategory true

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`source`** (query): string - Optional
  - Description: Source Filter (possible Values: 'lts','idm), (default:'null')

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET Tag Single

**Path:** `GET /v1/Tag/{id}`

**Operation ID:** `SingleTag`

**Tags:** `Tag`

**Parameters:** (5 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the Tag

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`localizationlanguage`** (query): string - Optional

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## ODHTAG

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (2 total)

#### 1. GET ODHTag List

**Path:** `GET /v1/ODHTag`

**Tags:** `ODHTag`

**Parameters:** (15 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional

- **`pagesize`** (query): integer - Optional

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`validforentity`** (query): string - Optional
  - Description: Filter on Tags valid on Entities (accommodation, activity, poi, odhactivitypoi, package, gastronomy, event, article, common .. etc..)

- **`mainentity`** (query): string - Optional
  - Description: Filter on Tags with MainEntity set to (accommodation, activity, poi, odhactivitypoi, package, gastronomy, event, article, common .. etc..)

- **`displayascategory`** (query): boolean - Optional
  - Description: true = returns only Tags which are marked as DisplayAsCategory true

- **`source`** (query): string - Optional
  - Description: Source Filter (possible Values: 'lts','idm'), (default:'null')

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`localizationlanguage`** (query): string - Optional
  - Description: here for Compatibility Reasons, replaced by language parameter

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET ODHTag Single

**Path:** `GET /v1/ODHTag/{id}`

**Operation ID:** `SingleODHTag`

**Tags:** `ODHTag`

**Parameters:** (5 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the Odhtags

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`localizationlanguage`** (query): string - Optional

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## LOCATION

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (2 total)

#### 1. GET Location List (Use in locfilter)

**Path:** `GET /v1/Location`

**Tags:** `Location`

**Parameters:** (5 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default 'en'), if 'null' is passed all languages are returned as Dictionary

- **`pagenumber`** (query): integer (int32) - Optional

- **`type`** (query): string - Optional
  - Description: Type ('mta','reg','tvs','mun','fra') Separator ',' : 'null' returns all Location Objects (default)
  - Default: `null`
  - Allowed values: `mta`, `reg`, `tvs`, `mun`, `fra`

- **`showall`** (query): boolean - Optional
  - Description: Show all Data (true = all, false = show only data marked as visible)
  - Default: `True`

- **`locfilter`** (query): string - Optional
  - Description: Locfilter (Separator ',') possible values: mta + MetaREGIONID = (Filter by MetaRegion), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), (default:'null')

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET Skiarea List (Use in locfilter as "ska")

**Path:** `GET /v1/Location/Skiarea`

**Tags:** `Location`

**Parameters:** (3 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default 'en'), if 'null' is passed all languages are returned as Dictionary

- **`pagenumber`** (query): integer (int32) - Optional

- **`locfilter`** (query): string - Optional
  - Description: Locfilter (Separator ',') possible values: mta + MetaREGIONID = (Filter by MetaRegion), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), (default:'null')

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## GEO

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (4 total)

#### 1. GET GeoShapes List

**Path:** `GET /v1/GeoShapes`

**Tags:** `Geo`

**Parameters:** (10 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Default: `1`

- **`pagesize`** (query): integer - Optional

- **`srid`** (query): string - Optional
  - Description: Spatial Reference Identifier, Coordinate System of the geojson, available formats(epsg:4362,epsg:32632,epsg:3857)
  - Default: `epsg:4362`

- **`source`** (query): string - Optional

- **`type`** (query): string - Optional

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET GeoShapes List

**Path:** `GET /v1/GeoShape`

**Tags:** `Geo`

**Parameters:** (10 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Default: `1`

- **`pagesize`** (query): integer - Optional

- **`srid`** (query): string - Optional
  - Description: Spatial Reference Identifier, Coordinate System of the geojson, available formats(epsg:4362,epsg:32632,epsg:3857)
  - Default: `epsg:4362`

- **`source`** (query): string - Optional

- **`type`** (query): string - Optional

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 3. GET GeoShape Single

**Path:** `GET /v1/GeoShapes/{id}`

**Operation ID:** `SingleGeoShapes`

**Tags:** `Geo`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the Tag

**Query Parameters:**

- **`srid`** (query): string - Optional
  - Default: `epsg:4362`

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 4. GET GeoShape Single

**Path:** `GET /v1/GeoShape/{id}`

**Operation ID:** `SingleGeoShape`

**Tags:** `Geo`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the Tag

**Query Parameters:**

- **`srid`** (query): string - Optional
  - Default: `epsg:4362`

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## WEATHER

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (14 total)

#### 1. GET Current Suedtirol Weather LIVE

**Path:** `GET /v1/Weather`

**Tags:** `Weather`

**Parameters:** (7 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional

- **`pagesize`** (query): integer - Optional

- **`language`** (query): string - Optional
  - Description: Language
  - Default: `en`

- **`locfilter`** (query): string - Optional
  - Description: Locfilter (possible values: filter by StationData 1 = Schlanders, 2 = Meran, 3 = Bozen, 4 = Sterzing, 5 = Brixen, 6 = Bruneck | filter nearest Station to Region,TV,Municipality,Fraction reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), '' = No Filter). IF a Locfilter is set, only Stationdata is provided.

- **`extended`** (query): boolean - Optional
  - Default: `True`

- **`source`** (query): string - Optional

- **`fields`** (query): array - Optional
  - Array item type: string

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET Current Suedtirol Weather LIVE Single

**Path:** `GET /v1/Weather/{id}`

**Operation ID:** `SingleWeather`

**Tags:** `Weather`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language
  - Default: `en`

- **`source`** (query): string - Optional

- **`fields`** (query): array - Optional
  - Array item type: string

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 3. GET Suedtirol Weather HISTORY

**Path:** `GET /v1/WeatherHistory`

**Tags:** `Weather`

**Parameters:** (19 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber
  - Default: `1`

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`language`** (query): string - Optional
  - Description: Language

- **`idlist`** (query): string - Optional

- **`locfilter`** (query): string - Optional

- **`datefrom`** (query): string - Optional

- **`dateto`** (query): string - Optional

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`latitude`** (query): string - Optional

- **`longitude`** (query): string - Optional

- **`radius`** (query): string - Optional

- **`polygon`** (query): string - Optional

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null)<a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`lastchange`** (query): string - Optional

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 4. GET Suedtirol Weather HISTORY SINGLE

**Path:** `GET /v1/WeatherHistory/{id}`

**Operation ID:** `SingleWeatherHistory`

**Tags:** `Weather`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 5. GET District Weather LIVE

**Path:** `GET /v1/Weather/District`

**Operation ID:** `SingleWeatherDistrict`

**Tags:** `Weather`

**Parameters:** (6 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional

- **`pagesize`** (query): integer - Optional

- **`locfilter`** (query): string - Optional
  - Description: Locfilter (possible values: filter by District 1 = Etschtal/Überetsch/Unterland, 2 = Burggrafenamt, 3 = Vinschgau, 4 = Eisacktal und Sarntal, 5 = Wipptal, 6 = Pustertal/Dolomiten, 7 = Ladinien-Dolomiten | filter nearest DistrictWeather to Region,TV,Municipality,Fraction reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction))

- **`language`** (query): string - Optional
  - Description: Language
  - Default: `en`

- **`source`** (query): string - Optional

- **`fields`** (query): array - Optional
  - Array item type: string

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 6. GET District Weather LIVE SINGLE

**Path:** `GET /v1/Weather/District/{id}`

**Tags:** `Weather`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language
  - Default: `en`

- **`source`** (query): string - Optional

- **`fields`** (query): array - Optional
  - Array item type: string

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 7. GET Current Realtime Weather LIVE

**Path:** `GET /v1/Weather/Realtime`

**Tags:** `Weather`

**Parameters:** (7 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional

- **`pagesize`** (query): integer - Optional

- **`language`** (query): string - Optional
  - Description: Language
  - Default: `en`

- **`latitude`** (query): string - Optional

- **`longitude`** (query): string - Optional

- **`radius`** (query): string - Optional

- **`fields`** (query): array - Optional
  - Array item type: string

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 8. GET Current Realtime Weather LIVE Single

**Path:** `GET /v1/Weather/Realtime/{id}`

**Operation ID:** `SingleWeatherRealtime`

**Tags:** `Weather`

**Parameters:** (3 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: id

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language
  - Default: `en`

- **`fields`** (query): array - Optional
  - Array item type: string

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 9. GET Weather Forecast

**Path:** `GET /v1/Weather/Forecast`

**Tags:** `Weather`

**Parameters:** (9 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional

- **`pagesize`** (query): integer - Optional

- **`locfilter`** (query): string - Optional
  - Description: Locfilter (possible values: filter on reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction))

- **`language`** (query): string - Optional
  - Description: Language
  - Default: `en`

- **`fields`** (query): array - Optional
  - Array item type: string

- **`latitude`** (query): string - Optional

- **`longitude`** (query): string - Optional

- **`radius`** (query): string - Optional

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 10. GET Weather Forecast Single

**Path:** `GET /v1/Weather/Forecast/{id}`

**Operation ID:** `SingleWeatherForecast`

**Tags:** `Weather`

**Parameters:** (3 total)

**Path Parameters:**

- **`id`** (path): string - **Required**

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language
  - Default: `en`

- **`fields`** (query): array - Optional
  - Array item type: string

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 11. GET Measuringpoint LIST

**Path:** `GET /v1/Weather/Measuringpoint`

**Tags:** `Weather`

**Parameters:** (23 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional

- **`pagesize`** (query): integer - Optional

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of Gastronomy IDs), (default:'null')

- **`locfilter`** (query): string - Optional
  - Description: Locfilter (Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null')

- **`areafilter`** (query): string - Optional
  - Description: Area ID (multiple IDs possible, separated by ",")

- **`skiareafilter`** (query): string - Optional
  - Description: Skiarea ID

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`source`** (query): string - Optional

- **`active`** (query): boolean - Optional
  - Description: Active Filter (possible Values: 'true' only Active Measuringpoints, 'false' only Disabled Measuringpoints), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: ODH Active Filter Measuringpoints Filter (possible Values: 'true' only published Measuringpoints, 'false' only not published Measuringpoints), (default:'null')

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null')

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 12. GET Measuringpoint SINGLE

**Path:** `GET /v1/Weather/Measuringpoint/{id}`

**Operation ID:** `SingleMeasuringpoint`

**Tags:** `Weather`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: Measuringpoint ID

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 13. GET Snowreport Data LIVE

**Path:** `GET /v1/Weather/SnowReport`

**Tags:** `Weather`

**Parameters:** (4 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional

- **`pagesize`** (query): integer - Optional

- **`skiareaid`** (query): string - Optional
  - Description: Skiarea ID

- **`lang`** (query): string - Optional
  - Description: Language
  - Default: `en`

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 14. GET Snowreport Data LIVE Single

**Path:** `GET /v1/Weather/SnowReport/{id}`

**Operation ID:** `SingleSnowReport`

**Tags:** `Weather`

**Parameters:** (2 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: Skiarea ID

**Query Parameters:**

- **`lang`** (query): string - Optional
  - Description: Language
  - Default: `en`

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## WEBCAMINFO

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (2 total)

#### 1. GET Webcam List

**Path:** `GET /v1/WebcamInfo`

**Tags:** `WebcamInfo`

**Parameters:** (20 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber
  - Default: `1`

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page (default:10)

- **`source`** (query): string - Optional
  - Description: Source Filter (Separator ',' available sources 'lts','content'), (default:'null')

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of Gastronomy IDs), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active Webcam Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')

- **`odhactive`** (query): boolean - Optional
  - Description: ODH Active (Published) Webcam Filter (possible Values: 'true' only published data, 'false' only not published data), (default:'null')

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null')

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Id OR Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null)<a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET Webcam Single

**Path:** `GET /v1/WebcamInfo/{id}`

**Operation ID:** `SingleWebcamInfo`

**Tags:** `WebcamInfo`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the Webcam

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: OK
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Bad Request
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## ANNOUNCEMENT

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (2 total)

#### 1. GET Announcement List

**Path:** `GET /v1/Announcement`

**Tags:** `Announcement`

**Parameters:** (23 total)

**Query Parameters:**

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber
  - Default: `1`

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`langfilter`** (query): string - Optional
  - Description: Langfilter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)

- **`idlist`** (query): string - Optional
  - Description: IDFilter (Separator ',' List of IDs, 'null' = No Filter), (default:'null')

- **`source`** (query): string - Optional
  - Description: Source Filter (possible Values: 'lts','idm'), (default:'null')

- **`active`** (query): boolean - Optional
  - Description: Active Filter (possible Values: 'true' only active data, 'false' only not active data), (default:'null')

- **`begin`** (query): string - Optional
  - Description: Begin Filter (Format: yyyy-MM-dd HH:MM), (default: 'null')

- **`end`** (query): string - Optional
  - Description: End Filter (Format: yyyy-MM-dd HH:MM), (default: 'null')

- **`tagfilter`** (query): string - Optional
  - Description: Filter on Tags. Syntax =and/or(TagSource.TagId,TagSource.TagId,TagId) example or(idm.summer,lts.hiking) - and(idm.themed hikes,lts.family hikings) - or(hiking) - and(idm.summer) - Combining and/or is not supported at the moment, default: 'null')

- **`publishedon`** (query): string - Optional
  - Description: Published On Filter (Separator ',' List of publisher IDs), (default:'null')

- **`updatefrom`** (query): string - Optional
  - Description: Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`latitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`longitude`** (query): string - Optional
  - Description: GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`radius`** (query): string - Optional
  - Description: Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality" target="_blank">Wiki geosort</a>

- **`polygon`** (query): string - Optional
  - Description: valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality" target="_blank">Wiki geosort</a>

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

- **`getasidarray`** (query): boolean - Optional
  - Description: Get result only as Array of Ids, (default:false)  Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET Announcement Single

**Path:** `GET /v1/Announcement/{id}`

**Operation ID:** `SingleAnnouncement`

**Tags:** `Announcement`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the Announcement

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

## METADATA

### Server URLs
- https://tourism.opendatahub.com

### Endpoints (3 total)

#### 1. GET Tourism MetaData List

**Path:** `GET /v1`

**Operation ID:** `TourismApi`

**Tags:** `MetaData`

**Parameters:** (10 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber
  - Default: `1`

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`updatefrom`** (query): string - Optional
  - Description: [not implemented] Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Shortname and ApiDescription in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 2. GET Tourism MetaData List

**Path:** `GET /v1/MetaData`

**Operation ID:** `TourismApiMetaData`

**Tags:** `MetaData`

**Parameters:** (10 total)

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`pagenumber`** (query): integer (int32) - Optional
  - Description: Pagenumber
  - Default: `1`

- **`pagesize`** (query): integer - Optional
  - Description: Elements per Page, (default:10)

- **`seed`** (query): string - Optional
  - Description: Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)

- **`updatefrom`** (query): string - Optional
  - Description: [not implemented] Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`searchfilter`** (query): string - Optional
  - Description: String to search for, Shortname and ApiDescription in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a>

- **`rawfilter`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a>

- **`rawsort`** (query): string - Optional
  - Description: <a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a>

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: List created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---

#### 3. GET TourismMetaData Single

**Path:** `GET /v1/MetaData/{id}`

**Operation ID:** `SingleMetaData`

**Tags:** `MetaData`

**Parameters:** (4 total)

**Path Parameters:**

- **`id`** (path): string - **Required**
  - Description: ID of the MetaData

**Query Parameters:**

- **`language`** (query): string - Optional
  - Description: Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)

- **`fields`** (query): array - Optional
  - Description: Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a>
  - Array item type: string

- **`removenullvalues`** (query): boolean - Optional
  - Description: Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues" target="_blank">Opendatahub Wiki</a>
  - Default: `False`

**Responses:**

- **200**: Object created
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **400**: Request Error
  - Content types: `text/plain`, `application/json`, `text/json`, `text/csv`, `application/ld+json`, `application/ldjson`, `application/rawdata`
- **500**: Internal Server Error

---
