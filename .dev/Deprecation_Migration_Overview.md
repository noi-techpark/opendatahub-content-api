<!--
SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>

SPDX-License-Identifier: CC0-1.0
-->

# Deprecation / Migration Overview

Single overview of everything the team has documented as deprecated/obsolete on the wiki, cross-checked against
whether it is actually marked deprecated in this repo's C# code (`[SwaggerDeprecated("...")]` and/or `[Obsolete]`
attributes — this repo's convention for surfacing deprecation in Swagger and to clients via `GET /Deprecated`,
see `OdhApiCore/Controllers/generic/DeprecatedController.cs`).

## Sources

- [Generic Datamodel changes](https://github.com/noi-techpark/opendatahub-docs/wiki/Generic-Datamodel-changes)
- [Events Datamodel changes](https://github.com/noi-techpark/opendatahub-docs/wiki/Events-Datamodel-changes)
- [ODHActivityPoi Datamodel changes](https://github.com/noi-techpark/opendatahub-docs/wiki/ODHActivityPoi-Datamodel-changes)
- [Accommodation Datamodel changes](https://github.com/noi-techpark/opendatahub-docs/wiki/Accommodation-Datamodel-changes)
- [Suedtirol Wine Pois and WineAwards Datamodel changes](https://github.com/noi-techpark/opendatahub-docs/wiki/Suedtirol-Wine-Pois-and-WineAwards-Datamodel-changes)

## Legend

| Status | Meaning |
|---|---|
| ✅ Marked | `[SwaggerDeprecated(...)]` and/or `[Obsolete]` found on the field in the API-facing (`*Linked`/`*V2`) model |
| ⚠️ Not marked | Field still exists, unmarked, on the currently-used API model — clients get no deprecation signal |
| ❌ Not found | Field doesn't exist on the current live model (already removed, or only ever existed on dead/commented-out/legacy code, e.g. `EventRaven`) |

Datatype names below are the `odhtype` keys from `Helper/Generic/ODHTypeHelper.cs`.

---

## Endpoints

Whole controllers or individual actions marked obsolete in code. All of the below **are** correctly marked
(`[Obsolete("...")]`) — listed here for the overview, not as gaps.

### Whole controllers

| Controller | Route base | Obsolete message | Location |
|---|---|---|---|
| `EventShortController` | `EventShort` | "Obsolete use Event Endpoint" | `OdhApiCore/Controllers/deprecated/EventShortApiController.cs:37` |
| `ODHTagController` | `ODHTag` | "Obsolete use Tag Endpoint" | `OdhApiCore/Controllers/deprecated/OdhTagController.cs:30` |
| `ActivityController` | `Activity` | "Please use ODHActivityPoi Endpoint" | `OdhApiCore/Controllers/compatibility/ActivityApiController.cs:28` |
| `GastronomyController` | `Gastronomy` | "Please use ODHActivityPoi Endpoint" | `OdhApiCore/Controllers/compatibility/GastronomyApiController.cs:28` |
| `PoiController` | `Poi` | "Please use ODHActivityPoi Endpoint" | `OdhApiCore/Controllers/compatibility/PoiApiController.cs:29` |

### Individual actions

| Action | Route | Obsolete message | Location |
|---|---|---|---|
| `GET ODHActivityPoiChanged` | compatibility | "Deprecated, use the ODHActivityPoi Endpoint" | `CompatiblityApiController.cs:996` |
| `GET EventChanged` | compatibility | "Deprecated, use the Event Endpoint" | `CompatiblityApiController.cs:1207` |
| `GET ArticleChanged` | compatibility | "Deprecated, use the Article Endpoint" | `CompatiblityApiController.cs:1388` |
| `GET AccommodationChanged` | compatibility | "Deprecated, use the Accommodation Endpoint" | `CompatiblityApiController.cs:1611` |
| `GET AccommodationTypes` | `AccommodationTypes` | "Use the Tags api (types=accommodationcategory,accommodationmealplans,accommodationtypes)" | `AccommodationApiController.cs:387` |
| `GET AccommodationTypes/{id}` | `AccommodationTypes/{id}` | same replacement | `AccommodationApiController.cs:434` |
| `GET AccommodationFeatures` | `AccommodationFeatures` | "Use the Tags api (types=accommodationoption,accommodationtitle)" | `AccommodationApiController.cs:472` |
| `GET AccommodationFeatures/{id}` | `AccommodationFeatures/{id}` | same replacement | `AccommodationApiController.cs:523` |
| `GET EventTopics` | `EventTopics` | "Use the Tags api (types=eventtopic)" | `EventApiController.cs:221` |
| `GET EventTopics/{id}` | `EventTopics/{id}` | "Use the Tags api (/v1/Tag/{id})" | `EventApiController.cs:266` |
| `GET VenueTypes` | `VenueTypes` | "Use the Tags api (validforentity=venue)" | `VenueApiController.cs:204` |
| `GET VenueTypes/{id}` | `VenueTypes/{id}` | "Use the Tags api (/v1/Tag/{id})" | `VenueApiController.cs:250` |
| `GET ODHActivityPoiTypes` | `ODHActivityPoiTypes` | "Use the Tags api (validforentity=odhactivitypoi)" | `ODHActivityPoiController.cs:257` |
| `GET ODHActivityPoiTypes/{*id}` | `ODHActivityPoiTypes/{id}` | "Use the Tags api (/v1/Tag/{id})" | `ODHActivityPoiController.cs:302` |
| `POST STA/ImportVendingPoints` | `STA/ImportVendingPoints` | "Moved to ODHImporter" | `STAController.cs:127` |

**Dead code, not active endpoints**: `GET PoiChanged` / `ActivityChanged` / `GastronomyChanged` in `CompatiblityApiController.cs` are fully commented out (not obsolete-marked, simply unreachable).

**Not an obsolete endpoint** — `GET Deprecated` (`generic/DeprecatedController.cs`) is the meta endpoint that lets clients query which fields are marked `[SwaggerDeprecated]`/`[Obsolete]` across the whole API.

---

## Fields

### Generic (odhtype: most types — accommodation, event, odhactivitypoi, article, venue, webcam, ...)

Pattern lives in `DataModel/datamodels/DataModelsLinked.cs` / `DataModelsV2.cs`, on every `*Linked`/`*V2` class.

| Field | Status | Replacement | Notes |
|---|---|---|---|
| `Gpstype` (root) | ✅ Marked | `GpsInfo` | "Deprecated, use GpsInfo" — consistent across ~15 Linked classes |
| `Latitude` (root) | ✅ Marked | `GpsInfo` | same pattern |
| `Longitude` (root) | ✅ Marked | `GpsInfo` | same pattern |
| `Altitude` (root) | ✅ Marked | `GpsInfo` | same pattern |
| `AltitudeUnitofMeasure` (root) | ✅ Marked | `GpsInfo` | same pattern |
| `GpsPoints` | ✅ Marked | `GpsInfo` | marked both on base `ODHActivityPoi` and via the computed `GpsInfo.ToGpsPointsDictionary()` getter on every Linked class |
| `SmgTags` | ⚠️ **Not marked** | `Tags`/`TagIds` | attribute exists only **commented out** everywhere (e.g. Event, Article, Accommodation base classes) — field is fully live and unmarked |
| `ODHTags` | ⚠️ **Not marked** | `Tags`/`TagIds` | computed getter present on nearly every Linked class, only carries `[SwaggerSchema(generated field)]`, no `[SwaggerDeprecated]` anywhere, including EventLinked/ODHActivityPoiLinked |
| `SmgActive` | ✅ Marked | `PublishedOn` | "Obsolete, use PublishedOn" — consistent on base classes (Area, Accommodation, Event, Article, Venue, PoiBaseInfos, Wine) |
| `OdhActive` / `ODHActive` | ⚠️ **Inconsistent** | `PublishedOn` | ✅ marked on `EventLinked` and `ODHActivityPoiLinked` only. ⚠️ **not marked** on `AccommodationLinked`, `WineLinked`, and roughly a dozen other Linked classes |

### Event (odhtype: `event`)

`DataModel/datamodels/DataModels.cs` (Event/EventDate/EventBooking) + `DataModelsLinked.cs` (`EventLinked`)

| Field | Status | Replacement | Notes |
|---|---|---|---|
| `Pdf` | ❌ Not found | — | only in fully commented-out `EventRaven` |
| `Ranc` | ⚠️ **Not marked** | Publisher | exists on `EventPublisher`; sibling `Publish` field *is* marked, `Ranc` isn't |
| `PayMet` | ❌ Not found | — | not present anywhere in `DataModel/` |
| `Type` (Event root) | ❌ Not found | — | no such property on `Event` |
| `GrpEvent` | ❌ Not found | — | only in commented-out `EventRaven` |
| `LTSTags` (on Event) | ❌ Not found | Tags (`eventcategory`) | `Event` has no `LTSTags` property; only exists on ODHActivityPoi |
| `Hashtag` | ❌ Not found | Tags (`eventtag`) | only in commented-out `EventRaven` |
| `EventDate.GpsEast` | ❌ Not found | — | no such property on `EventDate` |
| `EventDate.GpsNorth` | ❌ Not found | — | no such property on `EventDate` |
| `EventDate.InscriptionTill` | ❌ Not found | — | not present |
| `EventDate.EventDateAdditionalTime` | ❌ Not found | — | type `EventDateAdditionalTime` exists but is orphaned, never wired as a property |
| `EventDate.EventCalculatedDay` | ✅ Marked | `EventCalculatedDays` | "Deprecated use EventCalculatedDays" |
| `EventPrices` | ❌ Not found | `EventVariants` | only in commented-out `EventRaven` |
| `EventPrice` | ✅ Marked | `EventVariants` | "Obsolete, use EventVariants" |
| `EventBenefit` | ❌ Not found | — | only in commented-out `EventRaven` |
| `EventCrossSelling` | ❌ Not found | — | not present anywhere |
| `EventDescAdditional` | ❌ Not found | — | only referenced in a comment |
| `EventOperationScheduleOverview` | ❌ Not found | — | type exists but not wired as a live property |
| `NextBeginDate` | ❌ Not found | `EventDates`/`EventDatesBegin` | commented out entirely |
| `ODHTags` | ⚠️ Not marked | `Tags` | see Generic section |
| `OdhActive` | ✅ Marked | `PublishedOn` | "Deprecated use PublishedOn" |
| `EventBooking.BookableTo` | ⚠️ **Not marked** | `EventUrls` (`bookingUrl`) | field exists, unmarked |
| `EventBooking.BookableFrom` | ⚠️ **Not marked** | `EventUrls` (`bookingUrl`) | field exists, unmarked |
| `EventBooking.AccommodationAssignment` | ❌ Not found | `EventUrls` (`bookingUrl`) | no such property on `EventBooking` |
| `ClassificationRID` | ✅ Marked | `Mapping.lts.ClassificationRID`, `EventProperty.EventClassificationId` | |
| `Topics` | ✅ Marked | Tags (`eventcategory`) | marked on both base `Event.Topics` and `EventLinked.Topics` override |
| `TopicRids` (`TopicRIDs`) | ✅ Marked | Tags (`eventcategory`) | |
| `Ticket` | ✅ Marked | `EventProperty.TicketRequired` | (there's also a separately-marked `EventDate.Ticket`) |
| `SignOn` | ✅ Marked | `EventProperty.RegistrationRequired` | |
| `OrgRID` | ✅ Marked | `EventProperty.EventOrganizerId` | |
| `EventAdditionalInfos.Mplace` | ✅ Marked | `Meetingplace` | |
| `EventAdditionalInfos.Reg` | ✅ Marked | `Registration` | |

**EventShort → Event**: `EventShort` model class is `[Obsolete("Obsolete use Event")]` (`DataModel/datamodels/ObsoleteModels.cs:19`), `EventShortLinked` inherits it. Controller-level obsolete marker listed under Endpoints above. ✅ Correctly marked end-to-end.

### ODHActivityPoi (odhtype: `odhactivitypoi`)

`ODHActivityPoi`/`PoiBaseInfos` base classes in `DataModels.cs`, `ODHActivityPoiLinked` in `DataModelsLinked.cs`.
Note: `ODHActivityPoiV2` (`DataModelsV2.cs`) exists but is explicitly commented `//NOT USED at the moment`.

| Field | Status | Replacement | Notes |
|---|---|---|---|
| `CustomId` | ✅ Marked | `Mapping` | "Obsolete use Mapping" |
| `SmgId` | ✅ Marked | `Mapping` | "Use Mappings" |
| `Type` | ✅ Marked | `Tags` | "Deprecated, Use Tags instead" |
| `SubType` | ✅ Marked | `Tags` | same |
| `PoiType` | ✅ Marked | `Tags` | same |
| `AdditionalPoiInfos.MainType` | ✅ Marked | `Tags` | |
| `AdditionalPoiInfos.SubType` | ✅ Marked | `Tags` | |
| `AdditionalPoiInfos.PoiType` | ✅ Marked | `Tags` | |
| `Highlight` | ⚠️ **Not marked** | `AdditionalProperties` | plain field on `PoiBaseInfos` |
| `OwnerRid` | ⚠️ **Not marked** | `Mapping` | |
| `ChildPoiIds` | ⚠️ **Not marked** | `Mapping` | |
| `MasterPoiIds` | ⚠️ **Not marked** | `Mapping` | |
| `PoiServices` | ✅ Marked | — | "Obsolete" |
| `ODHActivityPoiTypes` | ✅ Marked | `Tags` | computed getter on `ODHActivityPoiLinked` |
| `LocationInfo.AreaInfo` | ⚠️ **Not marked** | `AreaId` array | `AreaInfoLinked?` override, no attribute |
| `PoiProperty` | ⚠️ **Not marked** | — | its neighbors `SyncSourceInterface`/`SyncUpdateMode` *are* marked, `PoiProperty` isn't |
| `SyncSourceInterface` | ✅ Marked | — | "Obsolete" |
| `SyncUpdateMode` | ✅ Marked | — | "Obsolete" |
| `GpsPoints` | ✅ Marked | `GpsInfo` | |
| `OutdooractiveID` | ✅ Marked | `Mapping` | "Use Mappings" |
| `OutdooractiveElevationID` | ✅ Marked | `Mapping` | "Use Mappings" |
| `Difficulty` | ✅ Marked | `Ratings.Difficulty` | |
| `LTSTags` | ⚠️ **Not marked** | `Tags` | neither on `PoiBaseInfos` nor the `ODHActivityPoiLinked` override |
| `CopyrightChecked` | ⚠️ **Not marked** | — | |
| `SmgTags` | ⚠️ Not marked | `Tags` | see Generic section |
| `SmgActive` | ✅ Marked | `PublishedOn` | "Deprecated, Use PublishedOn field" |
| `CategoryCodes` | ✅ Marked | Tags (`gastronomycategory`) | marked on base + Linked override |
| `DishRates` | ✅ Marked | Tags (`gastronomydishcodes`) | marked on base + Linked override |
| `CapacityCeremony` | ✅ Marked | Tags (`gastronomyceremonycodes`) | marked on base + Linked override |
| `Facilities` | ✅ Marked | Tags (`gastronomyfacilities`) | marked on base + Linked override |
| `MaxSeatingCapacity` | ✅ Marked | `Mapping` | |
| `AltitudeDifference`, `AltitudeHighestPoint`, `AltitudeLowestPoint`, `AltitudeSumUp`, `AltitudeSumDown`, `DistanceDuration`, `DistanceLength`, `IsOpen`, `IsPrepared`, `RunToValley`, `IsWithLigth`, `HasRentals`, `LiftAvailable`, `FeetClimb`, `BikeTransport`, `WayNumber`, `Number` (→ `ActivityLtsDataProperties`) | ⚠️ **Not marked** (any) | `AdditionalProperties.ActivityLtsDataProperties` | all still plain root fields on `PoiBaseInfos`; redeclared unmarked on the unused `ODHActivityPoiV2` too — planned migration not wired up or flagged yet |
| `AgeFrom`, `AgeTo`, `HasFreeEntrance` (→ `PoiLtsDataProperties`) | ⚠️ **Not marked** | `AdditionalProperties.PoiLtsDataProperties` | same as above |

### Accommodation (odhtype: `accommodation`)

`Accommodation` base in `DataModels.cs`, `AccommodationLinked` in `DataModelsLinked.cs` (the class actually served),
`AccommodationV2` in `DataModelsV2.cs`.

**Key finding: the wiki-intended deprecations are only actually marked on `AccommodationV2` — which is a second,
parallel model. The live `Accommodation`/`AccommodationLinked` classes that the API actually serves still expose
nearly all of these fields completely unmarked.**

| Field | Status (root/Linked) | Status (V2) | Replacement | Notes |
|---|---|---|---|---|
| `TrustYouID` | ⚠️ Not marked | ✅ Marked | `Review.trustyou` | |
| `TrustYouScore` | ⚠️ Not marked | ✅ Marked | `Review.trustyou` | |
| `TrustYouResults` | ⚠️ Not marked | ✅ Marked | `Review.trustyou` | |
| `TrustYouActive` | ⚠️ Not marked | ✅ Marked | `Review.trustyou` | |
| `TrustYouState` | ⚠️ Not marked | ✅ Marked | `Review.trustyou` | |
| `HasApartment` | ⚠️ Not marked | ✅ Marked | `AccoProperties` | |
| `HasRoom` | ⚠️ Not marked | ✅ Marked | `AccoProperties` | |
| `IsCamping` | ⚠️ Not marked | ✅ Marked | `AccoProperties` | |
| `IsGastronomy` | ⚠️ Not marked | ✅ Marked | `AccoProperties` | |
| `IsBookable` | ⚠️ Not marked | ✅ Marked | `AccoProperties` | |
| `IsAccommodation` | ⚠️ Not marked | ✅ Marked | `AccoProperties` | |
| `TVMember` | ⚠️ Not marked | ✅ Marked | `AccoProperties` | |
| `HgvId` | ✅ Marked | — | `Mapping` | marked on root `Accommodation` already |
| `Representation` | ⚠️ **Not marked** | — | `Mapping.lts.representationMode` | |
| `MainLanguage` | ⚠️ **Not marked** | — | — | code comment says "not used todo remove" but no attribute |
| `GastronomyId` | ⚠️ **Not marked** | — | — | code comment says "not used to remove" but no attribute |
| `SmgTags` | ⚠️ Not marked | | `Tags` | see Generic section |
| `ODHTags` | ⚠️ **Not marked** | | `Tags` | computed getter, no attribute |
| `SmgActive` | ✅ Marked | | `PublishedOn` | |
| `ODHActive`/`OdhActive` | ⚠️ **Not marked** | | `PublishedOn` | see Generic section — inconsistent |
| `TourismVereinId` | ⚠️ **Not marked** | | `Mapping.lts.tourismOrganization.rid` | |
| `AccoType`/`AccoTypeId` | ⚠️ **Not marked** | | `Tags` | |
| `AccoCategory`/`AccoCategoryId` | ⚠️ **Not marked** | | `Tags` | |
| `AccoBoards`/`BoardIds` | ⚠️ **Not marked** | | `Tags` | |
| `AccoFeatures`/`Features` | ⚠️ **Not marked** | | `Tags`/`AdditionalProperties` | |
| `AccoBadges`/`BadgeIds` | ⚠️ **Not marked** | | `Tags`/`AdditionalProperties` | |
| `AccoThemes`/`ThemeIds` | ⚠️ **Not marked** | | `Tags`/`AdditionalProperties` | |
| `SpecialFeaturesIds` | ⚠️ **Not marked** | | `Tags`/`AdditionalProperties` | |
| `MarketingGroupIds` | ⚠️ **Not marked** | | `Tags`/`AdditionalProperties` | |
| `IndependentData` | ⚠️ **Not marked** | | `AdditionalProperties` | |
| `AccoHGVInfo` | ⚠️ **Not marked** | | `AdditionalProperties` | |
| `AccoLTSInfo` | ⚠️ **Not marked** | | `AdditionalProperties` | |

### WineAward (odhtype: `wineaward`) & Suedtirol Wine POIs (odhtype: `odhactivitypoi`)

There is no separate "WineAward" C# class — `WineLinked` (`DataModel/datamodels/DataModelsLinked.cs:2277`, `Self`
link `"WineAward/" + Id`) **is** the WineAward model, table `wines`.

Wine POIs use the standard `odhactivitypoi` model — all findings from the ODHActivityPoi section above apply
unchanged (`OdhActive`, `ODHTags`, `CustomId`, `Type`, `SubType`, `PoiType`, `SmgActive`, `SmgTags`, `PoiProperty`,
`PoiServices` all carry the same status as listed there). `AdditionalPoiInfos` as a whole is documented as removed/
no-longer-needed on the wiki, but the individual `MainType`/`SubType`/`PoiType` sub-fields are the only parts
actually marked (see above) — the container itself has no deprecation marker.

| Field (on `WineLinked`/`Wine`) | Status | Replacement | Notes |
|---|---|---|---|
| `OdhActive` | ⚠️ **Not marked** | `PublishedOn` | |
| `CustomId` | ⚠️ **Not marked** | `Mapping` | only has `[SwaggerSchema("Id on the primary data Source")]` |
| `SmgActive` | ✅ Marked | `PublishedOn` | "Obsolete, use PublishedOn" |

---

## Wiki accuracy notes

A number of fields the wiki lists as deprecated could not be found in the current model at all (`❌ Not found`
above) — mostly under **Event**. These aren't gaps to fix in code; they indicate either the wiki is describing
fields that were already fully removed (not just deprecated), or fields that only ever existed on dead/commented-out
code (`EventRaven`, an unused `EventOperationScheduleOverview`/`EventDateAdditionalTime` type). Worth a pass to
confirm with whoever maintains the wiki whether those entries should be reworded from "deprecated" to "removed", or
dropped from the page entirely.

## Summary of gaps to close

The clearest, highest-value gaps (fields the team clearly intended to deprecate per the wiki, that carry zero
deprecation signal in the live API today):

1. **`SmgTags`/`ODHTags` across essentially every datatype** — attribute is commented out everywhere, never live.
2. **`OdhActive`/`ODHActive` inconsistency** — marked on Event/ODHActivityPoi, unmarked on Accommodation, Wine, and ~a dozen other types.
3. **`AccommodationV2` vs `AccommodationLinked`** — all the Accommodation-specific deprecations (TrustYou*, Has*/Is* flags) only exist on the parallel, apparently-unused `AccommodationV2` class; the live `AccommodationLinked` model clients actually get has none of them marked.
4. **ODHActivityPoi's LTS-property-to-AdditionalProperties migration** (Altitude*/Distance*/Age*/etc.) — fields still live at root, unmarked, redeclared unmarked on the also-unused `ODHActivityPoiV2`.
5. Assorted individually-unmarked fields per datatype table above (`Highlight`, `OwnerRid`, `ChildPoiIds`, `MasterPoiIds`, `LTSTags`, `CopyrightChecked`, `PoiProperty` on ODHActivityPoi; `Representation`, `MainLanguage`, `GastronomyId`, `TourismVereinId`, `AccoType`/`Category`/`Boards`/`Features`/`Badges`/`Themes` on Accommodation; `Ranc`, `EventBooking.BookableTo`/`BookableFrom` on Event; `OdhActive`/`CustomId` on WineAward).
