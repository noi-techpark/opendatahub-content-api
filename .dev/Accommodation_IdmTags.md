<!--
SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>

SPDX-License-Identifier: CC0-1.0
-->

# Accommodation idm Tags (Type, Category, Board, Badge, Theme, SpecialFeature)

Status: implemented, uncommitted, not yet run on test/prod (2026-11-26).

## Goal

Move the accommodation classification fields (`AccoTypeId`, `AccoCategoryId`, `BoardIds`, `BadgeIds`, `ThemeIds`, `SpecialFeaturesIds`) to Tags.

- Use Source `idm` so the Tags are valid for accommodations of all sources (lts, discoverswiss, ...). This works like the idm Tags on ODHActivityPoi.
- LTS accommodations also keep their LTS Tags (RIDs) for type, category and board.
- In the future the old fields can be deprecated in favour of TagIds/Tags.

## Tag ids

The Tag id has the form `{tagtype}:{english key}`. Type, Category and Board use the same Tag types as the LTS Tags and are told apart by Source.

| accommodationtypes Type | Tag type | Example | Count |
|---|---|---|---|
| Type | `accommodationtypes` | `accommodationtypes:hotel-boardinghouse`, `accommodationtypes:swiss-lodge` | 9 |
| Category | `accommodationcategory` | `accommodationcategory:3s-stars`, `accommodationcategory:not-categorized` | 18 |
| Board | `accommodationmealplans` | `accommodationmealplans:half-board` | 5 |
| Badge | `accommodationbadge` | `accommodationbadge:red-rooster-farm` | 10 |
| Theme | `accommodationtheme` | `accommodationtheme:at-altitude` | 17 |
| SpecialFeature | `accommodationspecialfeature` | `accommodationspecialfeature:pets-welcome` | 10 |

**Not converted on purpose:**
- `Theme|alle` and `Board|all boards` are filter placeholders.
- `Theme|bonus vacanze` is obsolete.
- `SpecialFeature|guestcard`: the `guestcard` Tag already exists and is maintained by the Guestpass import.

**Mapping details:**
- The mapping lives in the importer package, in `LTSAPI/Utils/AccommodationIdmTagMapping.cs` (opendatahub-content-importer). It is the single source of truth.
- Keys match case-insensitively against the values stored on the accommodation, e.g. `HotelPension`, `3sstars`, `half board`, `Familienhotel`.

**accommodationtypes table, prod vs test:** both have 72 entries.
- Test has `Type|SwissLodge` (discoverswiss); prod doesn't.
- Prod has `SpecialFeature|Barrierefrei`; test doesn't.

Each environment only creates the Tags that are in its own table: 68 on prod, 68 on test.

**Checks done on prod data:**
- `AccoCategoryId` ↔ LTS category Tag (rid) is 1:1 for all 10,251 active LTS accommodations.
- `BoardIds` ↔ LTS mealplan Tags is 1:1 by code. Both come from `mealPlans`, and the 5 rids in `Boards.xml` match the 5 LTS `accommodationmealplans` Tags.
- The full records were not compared, because that needs a token with full-data access.

**LTS syncs are safe:**
- `LTSApiAccommodationTypesImportHelper`, `...CategoriesImportHelper` and `...MealplansImportHelper` only deactivate Tags found by `GetAllDataBySourceAndType(["lts"], ...)`.
- So the idm Tags with the same Tag type are not touched.

## Changes

### opendatahub-content-importer (package `opendatahub-ltsapi-importer`)

| Version | Change |
|---|---|
| 5.0.1 | `AccommodationIdmTagMapping` added (Type, Badge, Theme, SpecialFeature). The parser calls `AssignTags` after `UpdateBadges`. |
| 5.0.2 | Open data (reduced) records: `ThemeIds = null` before `AssignTags`, so they get no themes and no theme Tags. Published. |
| 5.0.3 | Category and Board groups added, plus `Type|swisslodge`. New `IsIdmTagId()`. `AssignTags` now also removes idm Tags that no longer apply (recognized by the `{tagtype}:` prefix). LTS RIDs, `guestcard` and other Tags are not touched. **Not yet published.** |

- `LTSAPI/Utils/AccommodationIdmTagMapping.cs`
- `LTSAPI/Parser/accommodation/AccommodationParser.cs`: the call to `AssignTags` and the reduced ThemeIds handling.
- `LTSAPI/LTSAPI.csproj`: Version 5.0.3.

Publishing:

```bash
cd opendatahub-content-importer
dotnet pack LTSAPI/LTSAPI.csproj -c Release -o nupkg
dotnet nuget push nupkg/opendatahub-ltsapi-importer.5.0.3.nupkg --api-key <KEY> --source https://api.nuget.org/v3/index.json
# afterwards in content-api, nuget.org needs a few minutes to index:
dotnet restore OdhApiCore.sln --no-http-cache
```

### opendatahub-content-api

**Package reference:** OdhApiCore and OdhApiImporter now reference `opendatahub-ltsapi-importer` 5.0.3.

**`AccommodationApiController.Post/Put`:**
- They call `global::LTSAPI.Utils.AccommodationIdmTagMapping.AssignTags(accommodation)` before `UpdateTagsExtension`.
- This gives discoverswiss and every other accommodation written via the API the idm Tags.
- It also corrects them after a manual edit, e.g. in the databrowser.

**`CustomDataOperation.AccommodationTypesToTags()`** (DataModify `GET /DataModify/AccommodationTypesToTags`):
- It creates or updates the idm Tags from the `accommodationtypes` table.
- Tag fields:
  - `Mapping.idm = {key, bitmask, type}`;
  - TagName = TypeDesc;
  - ValidForEntity/MainEntity = accommodation;
  - CC0, LicenseHolder idm.
- It doesn't overwrite existing Tags that don't have the Tag type.
- The response lists entries that aren't in the mapping as `unmapped:...`.

**`CustomDataOperation.AccommodationAddIdmTags(id, takefirst)`** (DataModify `GET /DataModify/AccommodationAddIdmTags`):
- What it does:
  - sets the idm Tags on TagIds/Tags of all accommodations (all sources);
  - adds missing ones and removes ones that no longer apply;
  - leaves all other Tags untouched.
- On reduced records it removes `ThemeIds`, as the import does.
- It writes directly with `UpdateAsync`: no push, and `LastChange`/`_Meta` are unchanged.
  - The result is identical to what the import and the PUT produce, sorted like `UpdateTagsExtension`.
  - So the next import finds no change and does **not push all accommodations to the marketplace**.
- If a required Tag from its own `accommodationtypes` table is missing, it changes nothing and lists the missing ids in the response.
- It works in batches of 500.
- Reduced records are found via `_Meta.Reduced` (row id `{Id}_REDUCED`).

**Deleted:** `Helper/Tagging/AccommodationIdmTagHelper.cs` and `HelperTests/AccommodationIdmTagHelperTests.cs`. This was the earlier DB-lookup approach, now replaced by the static mapping in the package.

**Tests:** `OdhApiCoreTests/Helper/AccommodationIdmTagMappingTests.cs` (8 tests) cover:
- other TagIds are kept;
- stale idm Tags are removed;
- discoverswiss with SwissLodge;
- obsolete and unknown keys;
- no duplicates;
- `GetTagId` and `IsIdmTagId`;
- 69 unique Tag ids.

Run with `dotnet test OdhApiCoreTests --filter "FullyQualifiedName!~Integration"`.

**`Helper/JsonGeneratorHelper.cs`:**
- `GenerateJSONLTSAccoFeaturesList` now has `SourceFilter_GeneratedColumn(["lts"])`.
- The parser's Features.json therefore only contains LTS Tags.
- Output today is unchanged, because all entries are lts.

**DataModel deprecations:**
- `SmgTags` on 8 base classes and `ODHTags` on 15 Linked classes: `[SwaggerDeprecated("Deprecated, use Tags/TagIds", "2026-01-01", "2027-04-30")]`.
- The accommodation fields (AccoCategoryId, AccoTypeId, BoardIds, BadgeIds, ThemeIds, SpecialFeaturesIds) only have the attribute **commented out**, plus a TODO.
  - Reason: the databrowser hides deprecated fields by default.
  - The accommodation Categorization tab and the required AccoTypeId/AccoCategoryId fields in the edit view would disappear.
  - The API filters `typefilter`, `categoryfilter`, `boardfilter`, `badgefilter` and `themefilter` still work on these fields (`PostgresSQLWhereBuilder.cs`, generated columns).

## Steps on test / prod

1. Publish package 5.0.3 and restore content-api with it.
2. Deploy content-api (API + importer).
3. `GET /DataModify/AccommodationTypesToTags`: should create 68 Tags with nothing `unmapped`.
4. `GET /DataModify/AccommodationAddIdmTags?id=<one LTS id>`: check the full and the `_REDUCED` record.
5. `GET /DataModify/AccommodationAddIdmTags`: run for all.
6. Run an LTS accommodation update. `pushed` should stay empty (unless LTS data really changed).
7. Run a discoverswiss import (PUT). It should produce `accommodationtypes:*` and `accommodationcategory:*` Tags.

On **prod**, run steps 3–5 right before the importer with the new package goes live. Otherwise the first import changes and pushes every accommodation.

## What the records get

| Record | idm Tags |
|---|---|
| LTS full | Type, Category, Board, Badge, Theme, SpecialFeature, plus the LTS RIDs (type, category, mealplans, cardtypes) and `guestcard` |
| LTS open data (reduced) | Type and Category, plus the LTS RIDs. No ThemeIds, and no Badge/SpecialFeature/Board (LTS open data doesn't deliver them). |
| discoverswiss | Type and Category (via the PUT) |

## Open points / ideas

- **discoverswiss transformer** (opendatahub-collectors, `transformers/discoverswiss-lodging/src/odh-content-mapper/lodging.go:153`):
  - It sets `TagIds = [AccoTypeId, AccoCategoryId]`, e.g. `"HotelPension","3stars"`. These are not Tag ids.
  - Proposal: remove that line, since the API now assigns the idm Tags.
- **Deprecation of the accommodation fields** (still commented out in `DataModel/datamodels/DataModels.cs`). Before enabling it:
  - the databrowser Categorization tab has to show Tags filtered by Tag type;
  - the accommodation filters have to work on TagIds.
- **Barrier-free:** `Barrierefrei` is commented out in the parser. The Tag is created but no accommodation gets it.
- **Not converted:** MarketingGroupIds and Features (amenities) are not Tags yet. Amenities as Tags is a TODO in the import.
- **Possible cleanup in the tags table:**
  - 41 inactive idm facilitycodes duplicates with lowercase PK;
  - 38 inactive `VEN`+RID ids.
- **Accommodation TagIds/TagEntries:** TagIds added manually to LTS accommodations are overwritten on every import, because the parser rebuilds TagIds. TagEntries are also lost: `UpdateTagsExtension` is called without `tagEntrysToPreserve`. A preserve pattern like the one for SmgTags (`AssignODHTags`) would be possible.
