# Geo Column State of Datatypes

Maps every `odhtype` handled by `Helper/Generic/ODHTypeHelper.cs` (`TranslateTypeString2Type` / `TranslateTypeString2Table`) to its geo-related model fields and the Postgres geometry column actually used for distance sort and polygon search.

Background: as part of moving geo search/sort off the `cube`/`earthdistance` extensions and onto PostGIS (`Helper/Postgres/PostGresSQLHelper.cs`), the `*_GeneratedColumns` geo helpers now default to a `gen_position` geometry column unless a table explicitly overrides `geometryColumn`. This table records, per type, whether that default is actually correct for the table's schema/data.

| type | table | GpsInfo | GpsPoints | Geo | Lat/Long on root | Distance sort by which column | Polygon functionality by which column |
|---|---|---|---|---|---|---|---|
| region | regions | Yes | No (commented out) | **Yes** | Yes (computed) | gen_position (default) | gen_position (default) |
| tourismassociation | tvs | Yes | No (commented out) | **Yes** | Yes (computed) | gen_position (default) | gen_position (default) |
| municipality | municipalities | Yes | No (commented out) | **Yes** | Yes (computed) | gen_position (default) | gen_position (default) |
| district | districts | Yes | No (commented out) | **Yes** | Yes (computed) | gen_position (default) | gen_position (default) |
| announcement | announcements | No | No | **Yes** | No | N/A (geosearch hardcoded false) | **geo** (explicit override) |
| urbangreen | urbangreens | No | No | **Yes** | No | N/A (geosearch hardcoded false) | **geo** (explicit override) |
| trip | trips | No | No | **Yes** | No | N/A (geosearch hardcoded false) | **geo** (explicit override) |
| spatialdata | spatialdatas | No | No | **Yes** | No | N/A (ordering call commented out) | **geo** (explicit override) |
| accommodation | accommodations | Yes | Yes (computed) | No | Yes (computed) | gen_position (default) | gen_position (default) |
| event | events | Yes | Yes (computed) | No | Yes (computed) | gen_position (default) | gen_position (default) |
| odhactivitypoi | smgpois | Yes | Yes (computed) | No | No | gen_position (default) | gen_position (default) |
| measuringpoint | measuringpoints | Yes | Yes (computed) | No | No | gen_position (default) | gen_position (default) |
| webcam | webcams | Yes | Yes (computed) | No | No | gen_position (default) | gen_position (default) |
| article | articles | Yes | Yes (computed) | No | No | gen_position (default) | N/A (no polygon call) |
| venue | venues | Yes | Yes (computed) | No | No | gen_position (default) | gen_position (default) |
| metaregion | metaregions | Yes | No (commented out) | No (commented out) | Yes (computed) | gen_position (default) | gen_position (default) |
| skiarea | skiareas | Yes | Yes | No | Yes (computed) | gen_position (default) | gen_position (default) |
| skiregion | skiregions | Yes | Yes | No | Yes (computed) | gen_position (default) | gen_position (default) |
| geoshape | geoshapes | No | No | No | No | N/A | N/A |

## Without geo info

These types carry no GPS/geo data in their model at all, so the geo-sort/polygon plumbing (where wired up) is dead code for them.

| type | table | GpsInfo | GpsPoints | Geo | Lat/Long on root | Distance sort by which column | Polygon functionality by which column |
|---|---|---|---|---|---|---|---|
| accommodationroom | accommodationrooms | No | No | No | No | N/A | N/A |
| area | areas | No | No | No | No | gen_position (default, likely dead) | gen_position (default) |
| wineaward | wines | No | No | No | No | gen_position (default, likely dead) | gen_position (default) |
| publisher | publishers | No | No | No | No | N/A | N/A |
| source | sources | No | No | No | No | N/A | N/A |
| weatherhistory | weatherdatahistory | No | No | No | No | N/A (commented out) | gen_position (default, unused - no geo data) |
| weather | *(no table entry)* | No | No | No | No | N/A | N/A |
| weatherdistrict | *(no table entry)* | No | No | No | No | N/A | N/A |
| weatherforecast | *(no table entry)* | No | No | No | No | N/A | N/A |
| weatherrealtime | *(no table entry)* | No | No | No | No | N/A | N/A |
| snowreport | *(no table entry)* | No | No | No | No | N/A | N/A |
| odhmetadata | metadata | No | No | No | No | N/A | N/A |
| tag | tags | No | No | No | No | N/A | N/A |

## Deprecated

| type | table | GpsInfo | GpsPoints | Geo | Lat/Long on root | Distance sort by which column | Polygon functionality by which column |
|---|---|---|---|---|---|---|---|
| eventshort | eventeuracnoi | No | No | No | No | N/A (geo code commented out) | N/A (commented out) |
| package | packages | No | No | No | No | N/A | N/A |
| experiencearea | experienceareas | Yes | No (commented out) | No (commented out) | Yes (computed) | gen_position (default) | gen_position (default) |
| odhtag | smgtags | No | No | No | No | N/A | N/A |

## Notes

- **`region`, `tourismassociation`, `municipality`, `district`** all have `Geo` (`IGeoAware`) and their repositories write a `geo` polygon column (presumably backed by a `gen_center_position` centroid, same pattern as municipality). Their controllers are routed generically through `CommonApiController` and call the geo-sort/polygon helpers **without overriding `geometryColumn`**, so they default to `gen_position` — same discrepancy across all four, not just municipality.
- **`district`** previously had `Geo`/`IGeoAware` and the `DistrictRepository` `geo`-column writer commented out/inactive. Re-enabled to mirror `municipality` exactly: `DistrictLinked` now implements `IGeoAware` (`DataModel/datamodels/DataModelsLinked.cs`), `DistrictRepository.cs`'s `UpsertableDistrict` writes the `geo` column via `ST_GeomFromText` on the default `GpsInfo`, and the `District` POST/PUT endpoints in `CommonApiController.cs` now validate `Geo` and use `UpsertableDistrict`, same as `Municipality`.
- **`announcement`, `urbangreen`, `trip`, `spatialdata`** already correctly pass `geometryColumn: "geo"` explicitly for polygon search, and have distance-sort disabled entirely (`geosearch` hardcoded `false`, or the ordering call commented out) — not affected by the `gen_position` default mismatch.
- `ltsactivity`, `ltspoi`, `ltsgastronomy` are commented out in `ODHTypeHelper` and excluded from this table.
- "Computed" Lat/Long means a deprecated getter like `public new double Latitude { get { ... GpsInfo.FirstOrDefault() ... } }`, not a stored column.
