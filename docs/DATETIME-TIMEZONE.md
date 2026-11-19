# DateTime / Time Zone: Analysis and TODOs

Status: analysis, nothing implemented yet. Based on the code in this repository and on samples from the public API (`https://tourism.api.opendatahub.com/v1`, 2026-11-18).

## Summary

- **Technical fields** (`FirstImport`, `LastChange`, `_Meta.LastUpdate`, `UpdateHistory[].LastUpdate`) mostly hold UTC. There are 3 different spellings and some old records without any time zone. Date libraries can read all of them, but the spelling should be unified.
- **Content dates** (event dates, operation schedules, article dates, weather) are stored as **local time of South Tyrol (Europe/Rome) without any time zone information**. Data model, DB generated columns and filters are consistent with each other (local against local), so nothing is wrong today. A client just cannot tell from the data which time zone is meant.
- **Decision:** existing fields stay unchanged (no offset appended, no switch to UTC, see [section 3](#3-strategy)). Instead:
  - add a `TimeZone` field (IANA name, `"Europe/Rome"`) where local dates are stored;
  - add calculated UTC fields only where the value is a real point in time;
  - document the time zone in the filter descriptions.
- **To verify:** two places where the time zone is probably interpreted incorrectly today (see [Possible bugs](#possible-bugs-to-verify)).

## 1. Technical fields (FirstImport, LastChange, _Meta.LastUpdate)

Observed spellings for the same kind of value:

| Spelling | Example | Origin |
|---|---|---|
| `+00:00` | `2026-11-16T09:09:57.5639932+00:00` | `DateTime.Now` (Kind = Local) on a server running in UTC, serialized by Newtonsoft |
| `Z` | `2026-10-09T20:20:38.3256062Z` | `DateTime.UtcNow` (Kind = Utc) |
| no time zone | `2015-10-23T15:03:12.3671905` | Kind = Unspecified, mostly old records (`FirstImport` from 2015–2022; Article, Gastronomy, Tag, Measuringpoint, Event) |

Notes:

- There are ~197 `DateTime.Now` usages in the code and only 3 `DateTime.UtcNow`. The `+00:00` values are UTC only because the containers run with TZ=UTC. If a container ever runs in Europe/Rome, the same code writes `+01:00` / `+02:00`.
- The old values without a time zone are ambiguous. They were most likely written on the old server in local time, so their time zone cannot be determined reliably.
- Some records mix spellings, e.g. Article: `FirstImport` and `LastChange` without a time zone, `UpdateHistory[].LastUpdate` with `+00:00`.

TODO:

- [ ] Use `DateTime.UtcNow` for technical timestamps (`FirstImport`, `LastChange`, `_Meta.LastUpdate`). Start with the central places, e.g. the metadata/update-info helpers and the importers' `FirstImport`/`LastChange` assignments.
- [ ] Decide on one output spelling, preferably `Z`. Optionally enforce it centrally in the serializer settings (`DateTimeZoneHandling.Utc`). **Careful:** this setting would also convert local content dates without a time zone, so only do it after content dates are clearly separated, or apply it only to the technical fields.
- [ ] Old values without a time zone: decide whether to leave them as they are (recommended, the value is not reliable anyway) or to mark them as UTC once via a DataModify method. Observed on:
  - Article: `FirstImport`, `LastChange`, `_Meta.LastUpdate` on 15 of 20 sampled records.
  - Gastronomy, Tag, Measuringpoint, Event, ODHActivityPoi: `FirstImport`, mostly 2015–2022.
- [ ] Weather `_Meta.LastUpdate` is set from the local time of the weather data on every record (not legacy). Fix it in the code going forward.

## 2. Content dates without time zone

All of these are local time Europe/Rome, written without an offset. Times of day are stored as `TimeSpan` (`"09:00:00"`).

### Events (`EventLinked`, table `events`)

| Field | Example | Note |
|---|---|---|
| `DateBegin`, `DateEnd` (calculated) | `2027-06-19T10:00:00` | first `From`+`Begin`, last `To`+`End` |
| `DateBeginUTC`, `DateEndUTC` (calculated) | `1796371200000` | Unix ms, correct UTC (conversion via `DateTimeHelper`, "W. Europe Standard Time") |
| `EventDate[].From`, `To` | `2027-06-19T00:00:00` | date only |
| `EventDate[].Begin`, `End`, `Entrance` | `10:00:00` | `TimeSpan` |
| `EventDate[].FromUTC`, `ToUTC` (calculated) | `1796371200000` | Unix ms, correct UTC |
| `EventDate[].EventCalculatedDays[].Day`, `Begin` | `0001-01-01T00:00:00` | see possible bugs |
| `EventDatesBegin[]`, `EventDatesEnd[]` | `2027-06-19T00:00:00` | date only |
| `EventBooking.BookableFrom`, `BookableTo` | | `DateTime?` from LTS |
| `EventDateOpeningHour`, `EventDateAdditionalTime` (`Begin1`, `End1`, ...) | | `TimeSpan` |

Sources: LTS (`EventParser`: `startDate` + `startTime` as strings), NOI EventShort converter, Ninja. None of them delivers a time zone.

DB: the generated columns `gen_eventdates` (`tsmultirange`) and `gen_eventdatearray` (`tsrange[]`) are built with `(From)::timestamp + (Begin)::time` ([Events.sql](../Helper/PGDBScripts/Events.sql)), so they are local without a time zone. The filter (`gen_eventdates && tsrange(...)`) and the sort (`get_nearest_tsrange_distance`) compare local against local. That is consistent.

TODO:

- [ ] Add `TimeZone` (`"Europe/Rome"`) on `Event`. It applies to `DateBegin`/`DateEnd`, and `DateBeginUTC`/`DateEndUTC` already carry the UTC value.
- [ ] Add `TimeZone` on `EventDate`. It applies to `From`/`To` + `Begin`/`End`/`Entrance`, and `FromUTC`/`ToUTC` carry the UTC value. Also add it on `IEventDate`.
- [ ] Set `TimeZone` in the parsers: the LTS `EventParser` (opendatahub-content-importer), `EventEventShortConverter` and `ParseNinjaData`.
- [ ] Filter documentation ([EventApiController.cs](../OdhApiCore/Controllers/api/EventApiController.cs) `begindate`/`enddate`): add that `yyyy-MM-dd` / `yyyy-MM-dd HH:mm` are interpreted as **local time Europe/Rome**, and that a Unix timestamp is converted to Europe/Rome (`DateTimeHelper.UnixTimeStampToDateTimeMilliseconds`).
- [ ] Optional: a calculated field with an offset (e.g. `"2027-06-19T10:00:00+02:00"`) if clients need ISO 8601 with an offset. The offset has to be calculated per date (summer time). An `EventDate` spanning several days can have different offsets for `From` and `To`.

### OperationSchedule

Used by: `PoiBaseInfos` (ODHActivityPoi, Gastronomy), `SkiArea`, `Venue`, `VenueV2`, `Article`, `AccommodationV2`, `RouteCalendar` (Trip). Also in the AlpineBits `DestinationData` model.

| Field | Example | Note |
|---|---|---|
| `OperationSchedule[].Start`, `Stop` | `2025-12-04T00:00:00` | date only, no time zone (Trip: with `Z`, see possible bugs) |
| `OperationSchedule[].OperationScheduleTime[].Start`, `End` | `09:00:00` | `TimeSpan` |

Additional note: depending on `Type` (2 = day+month recurring, 3 = month recurring), the year is not significant. This should be considered when calculating any offset or UTC value; a `TimeZone` field is enough there.

TODO:

- [ ] Add `TimeZone` (`"Europe/Rome"`) on `OperationSchedule` (applies to `Start`/`Stop` and to the `OperationScheduleTime` entries). Also add it on `IOperationSchedules`.
- [ ] Set it in the parsers that create OperationSchedules (LTS, HDS, STA, Ninja, SIAG, ...).
- [ ] Check filters that work on opening times and document their time zone.

### Articles

| Field | Example | Note |
|---|---|---|
| `ArticleDate`, `ArticleDateTo` | `2015-09-14T00:00:00` | mostly date only |
| `ExpirationDate` | | `DateTime?` |
| `OperationSchedule` | | see above |
| `AdditionalArticleInfos.<lang>.Elements.zeit` | `01:00` | free-text time |

Filter: `startdate`/`enddate` "Format (yyyy-MM-dd HH:mm)" ([ArticleApiController.cs](../OdhApiCore/Controllers/api/ArticleApiController.cs)), filters on `gen_begindate`/`gen_enddate` (`ArticleDateNewsFilterBeginEndWithIN_GeneratedColumn`).

TODO:

- [ ] Add `TimeZone` on `Article` (applies to `ArticleDate`, `ArticleDateTo`, `ExpirationDate`).
- [ ] Document the time zone of the `startdate`/`enddate` filters.

### Other fields without time zone (lower priority)

| Model | Fields | Note |
|---|---|---|
| `Weather`, `Conditions`, `Forecast`, `Mountain`, `Stationdata`, `BezirksWeather`, `WeatherForecast` | `Date` (+ `date`), `_Meta.LastUpdate` | local time of the provider, e.g. `2026-11-18T11:00:00`. Weather `_Meta.LastUpdate` is local here too, unlike all other endpoints |
| `MeasuringpointV2` / `MeasuringpointReduced` | `LastUpdate`, `LastSnowDate` | local, e.g. `2026-10-21T17:21:47.357` |
| `ImageGallery` | `ValidFrom`, `ValidTo` | date only |
| `Season` | `Start`, `End` | date only (MSS) |
| `Package` | `ValidStart`, `ValidStop` | date only |
| `UrbanGreen` | `PutOnSite`, `RemovedFromSite` | |
| `AccoOverview` | `CheckInFrom` ... `BaggageServiceTo` | `TimeSpan`, local times of day |
| `EchargingDataProperties` | `SurveyDate` | |
| `EventShort` (obsolete) | `StartDate`, `EndDate`, `RoomBooked.*` | local |

TODO:

- [ ] Decide per model whether a `TimeZone` field is useful. For pure dates (`ValidFrom`, `Season`), documentation is probably enough.

## 3. Strategy

### 3.1 Why existing values are not changed (no offset appended)

A value without an offset (`2027-06-19T10:00:00`) is read as "10:00 on the clock in South Tyrol". With an offset (`+02:00`) it becomes a point in time, which most libraries convert into their own time zone. `From`/`To` are pure dates at 00:00, so they move to the **previous day** outside Europe/Rome. Tested on 2026-11-18:

| Layer | Result with appended offset |
|---|---|
| Our .NET code (Newtonsoft, containers in UTC) | `From` 19.06. 00:00+02:00 is read as 18.06. 22:00, so `From.Date + Begin` and `FromUTC` are **one day off**. When the record is saved again, it is stored as `"2027-06-18T22:00:00+00:00"`. On a server in Europe/Rome it looks correct, so it does not show up on a developer laptop. No serializer setting fixes this; only switching the model type to `DateTimeOffset` would. |
| Affected code paths | LTS import (`LoadDataFromDB` + `MergeEventDates`), API POST/PUT, API GET with `denormalize=true`, JSON-LD output, Ninja import, all DataModify resave methods |
| PostgreSQL | `::timestamp` (events generated columns, `rawfilter` date comparisons) ignores the offset: OK. Plain string comparisons no longer match. |
| JavaScript consumers | Browser in Italy: no change. Server-side rendering in UTC or a browser abroad: `From` shows the previous day, `DateBegin` shows 08:00 instead of 10:00. |
| Python consumers | Values with an offset are timezone-aware; comparing them with old values or `datetime.now()` raises `TypeError`. |
| Concept | `Begin`/`End`/`OperationScheduleTime` are times without a date and cannot carry an offset. Pure dates have no meaningful offset. |

Consumers that handle time zones correctly already get the exact point in time from the `...UTC` fields.

### 3.2 TimeZone field

- `TimeZone = "Europe/Rome"` (IANA name), next to the local values. It is enough to calculate the correct offset for any date (+01:00 / +02:00).
- On: `Event`, `EventDate`, `OperationSchedule`, `Article` (+ interfaces `IEventDate`, `IOperationSchedules`); see section 2 for further candidates.

### 3.3 UTC fields

Only where the value is a real point in time. Same pattern as Events: name `<Field>UTC`, Unix milliseconds, calculated read-only property in the model (calculated with `DateTimeHelper`, never read from input).

| Model / field | What the value means | UTC field? |
|---|---|---|
| Event `DateBegin`/`DateEnd`, `EventDate.From+Begin` / `To+End` | concrete start/end | already there (`DateBeginUTC`, `FromUTC`, ...) |
| Weather `Date`, `Stationdata.Date`, `Conditions.Date` | measurement or forecast time | **yes** |
| Measuringpoint `LastUpdate` | measurement time | **yes** |
| Measuringpoint `LastSnowDate` | date only | no |
| Article `ArticleDate`, `ArticleDateTo`, `ExpirationDate` | publication date, validity end | **yes** (decided, for consistency) |
| OperationSchedule `Start`/`Stop` | validity period (dates) | only for Type 1; impossible for Type 2/3 (year not significant) |
| OperationScheduleTime `Start`/`End` | recurring weekly opening time | **no**: 09:00 local is 08:00Z in winter and 07:00Z in summer, so one UTC value would be wrong for half the season |
| ImageGallery `ValidFrom`/`ValidTo`, Season, Package | calendar dates | no |
| AccoOverview check-in/out times | recurring time of day | no |
| Announcement `StartTime`/`EndTime`, Trip `StopTimes` | real points in time | no extra field; store them correctly with an offset or as UTC (see possible bugs) |

Notes:

- End-of-day rule for date-only end values (`ArticleDateTo`, `ExpirationDate`, `OperationSchedule.Stop`): if the time is 00:00, the value means the whole day, so the UTC value is **23:59:59 local**. This matches the filters and the Events model:
  - the Event filter adds a day to an end date without a time;
  - the Article filter compares on the day boundary;
  - `EventDate` without `End` uses 23:59:59.
- `DateTimeHelper` currently uses `"W. Europe Standard Time"`. Switch it to `"Europe/Rome"` or to the record's `TimeZone`.

### 3.4 Writes via API (Databrowser, data collectors) and importers

Rule: **the input is local time, the server sets `TimeZone`.**

- `TimeZone` missing: the server sets `"Europe/Rome"`.
- `TimeZone` sent: accept it after validating it as an IANA name (open for data from other time zones later).
- Input with an offset or `Z` (e.g. `2027-06-19T08:00:00Z`): convert it to local time in `TimeZone` and store it **without an offset**. Do this centrally in the model (setters of `From`/`To`/`Start`/`Stop`, ...) or in a helper called before every save, so API writes, importers and DataModify methods behave the same.
- `...UTC` fields are calculated and ignored on input.
- [ ] Check what the Databrowser and the data collectors send today. Values sent with an offset are already stored shifted today (see 3.1). Search the events table for `From` values with an offset or not ending in `T00:00:00`.

## Possible bugs to verify

1. **Announcement filter possibly off by 1–2 hours.**
   - `Announcement.StartTime`/`EndTime` are stored without a time zone (`2024-08-05T01:00:00`, parsed with `Convert.ToDateTime` in [ParseMapServicesDataToAnnouncement.cs](../DIGIWAY/Parser/ParseMapServicesDataToAnnouncement.cs)).
   - The generated columns `gen_begindate`/`gen_enddate` are `timestamptz` and are built with `text2tstz()` ([03-Announcements.sql](../.dev/content-api-db-init/03-Announcements.sql)). For a value without an offset, `text2tstz()` uses the **session time zone of the DB** (probably UTC). The function is declared `IMMUTABLE`, although the result depends on the session.
   - The filter documentation asks for RFC 3339 with `Z`.
   - If the source value is local time, every announcement is shifted by 1–2 hours in the filter.
   - [ ] Check what the source delivers (local or UTC) and what `SHOW timezone` returns on the prod DB.
   - [ ] Then either write `StartTime`/`EndTime` with an offset, or build the column with `AT TIME ZONE 'Europe/Rome'`.
2. **Trip: `Z` on values that look like local time.**
   - `StopTimes[].ArrivalTime`/`DepartureTime` and `Route.Calendar.OperationSchedule.Start`/`Stop` carry `Z` (e.g. `2027-05-18T19:15:00Z`).
   - The `OperationScheduleTime.End` of the same trip is `19:15:00`. This looks like local time labelled as UTC.
   - `gen_begindate`/`gen_enddate` (`timestamptz`) take the value as UTC.
   - [ ] Check against the source (SkyAlps) whether the times are local or UTC.
3. **`EventCalculatedDays[].Day` = `0001-01-01T00:00:00` (confirmed).**
   - On the live events (2026-11-19), 1,900 of 2,080 calculated days have `Day` = `0001-01-01`.
   - Cause: in the LTS `EventParser` (opendatahub-content-importer, `eventcalculatedday.Day = Convert.ToDateTime(day.startDate)`), `Convert.ToDateTime(null)` returns `DateTime.MinValue` when LTS delivers no `startDate`.
   - Only the singular `EventDate.EventCalculatedDay` is deprecated (it returns the first entry of `EventCalculatedDays`); `EventCalculatedDays[].Day` is not.
   - [ ] Set `Day` to null if `startDate` is missing (optionally use `From` of a single-day EventDate), then reimport or fix the existing records via DataModify.

## Updating existing data (DataModifyApiController)

New fields are only visible after a record is saved again: the API returns the JSON stored in the DB directly (`JsonRaw`) and does not re-serialize it through the model. So every new `TimeZone` field needs a method in [DataModifyApiController.cs](../OdhApiImporter/Controllers/DataModifyApiController.cs) plus [CustomDataOperation.cs](../OdhApiImporter/Helpers/CUSTOM/CustomDataOperation.cs), following the existing pattern:

- `[Authorize(Roles = "DataPush")]`, `HttpGet`
- parameters `id`, `forceupdate`, `takethefirst` (like `ModifyODHActivityPoi`)
- load the records, set `TimeZone` where it is null, save again as `JsonBData`, return `UpdateResult` with `recordsmodified`

Planned methods:

- [ ] `FillEventTimeZone`: table `events`; sets `Event.TimeZone` and `EventDate[].TimeZone`.
- [ ] `FillOperationScheduleTimeZone`: tables of all models with an `OperationSchedule` (ODHActivityPoi, Gastronomy, SkiArea, Venue, Article, Accommodation, Trip); sets `OperationSchedule[].TimeZone`.
- [ ] `FillArticleTimeZone`: table `articles`; sets `Article.TimeZone` (can be combined with the OperationSchedule update).
- [ ] Resave Weather / Measuringpoint (and Article, if decided) so the new `...UTC` fields are written.
- [ ] Optional: `NormalizeTechnicalTimestamps`, if the decision in section 1 is to convert old technical fields.

Regular imports also fill the new fields over time, as records are updated. The DataModify methods are needed for records that are no longer updated by a source.

## Plan

1. **Verify first:** the possible bugs (Announcement, Trip, `EventCalculatedDays`) and what the Databrowser and data collectors send (3.4).
2. **TimeZone fields** (`"Europe/Rome"`) on Event, EventDate, OperationSchedule, Article + interfaces; set them in all parsers (3.2).
3. **UTC fields** (`<Field>UTC`, Unix ms, calculated) on Weather, Measuringpoint `LastUpdate` and Article `ArticleDate`/`ArticleDateTo`/`ExpirationDate`, with the end-of-day rule for date-only end values (3.3).
4. **Normalization on write:** input with an offset is converted to local time, `TimeZone` is set by the server (3.4).
5. **DataModify methods** to fill the new fields in existing records.
6. **Filter documentation:** state the time zone of all date filters (Event, Article, Announcement, Trip).
7. **Technical timestamps:** `DateTime.UtcNow` and one spelling (`Z`) for `FirstImport`/`LastChange`/`_Meta.LastUpdate` (section 1).

Existing fields are not changed at any step, so nothing breaks for current consumers.

## Open decisions

- [x] Article: UTC fields for `ArticleDate`, `ArticleDateTo`, `ExpirationDate`: yes, for consistency.
- [x] Date-only end values: 00:00 means the whole day, so the UTC value is 23:59:59 local.
- [ ] Optional calculated ISO fields with an offset (e.g. `EventDate.BeginDateTime = "2027-06-19T10:00:00+02:00"`) as an opt-in for consumers, yes or no?
- [ ] Old technical timestamps without a time zone: leave as they are or convert (section 1).
- [ ] `EventCalculatedDays[].Day` = `0001-01-01` (see possible bugs, 3): fix in the LTS `EventParser` (null instead of `DateTime.MinValue`, or `From` of a single-day EventDate), and how to correct the existing records (reimport or DataModify).
