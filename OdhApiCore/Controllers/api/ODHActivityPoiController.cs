// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using Helper;
using Helper.AdditionalProperties;
using Helper.Generic;
using Helper.Identity;
using Helper.Location;
using Helper.Tagging;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OdhApiCore.Responses;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiCore.Controllers.api
{
    /// <summary>
    /// ODH Activity Poi Api (data provided by various data providers) SOME DATA Available as OPENDATA
    /// </summary>
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]    
    public class ODHActivityPoiController : OdhController
    {
        private readonly ISettings settings;

        public ODHActivityPoiController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<ODHActivityPoiController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier)
        {
            this.settings = settings;
        }

        #region SWAGGER Exposed API

        //Standard GETTER

        /// <summary>
        /// GET ODHActivityPoi List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null') </param>
        /// <param name="type">Type of the ODHActivityPoi ('null' = Filter disabled, possible values: BITMASK: 1 = Wellness, 2 = Winter, 4 = Summer, 8 = Culture, 16 = Other, 32 = Gastronomy, 64 = Mobility, 128 = Shops and services), (default: 255 == ALL), refers to <a href='https://tourism.opendatahub.com/v1/ODHActivityPoiTypes?rawfilter=eq(Type,%27Type%27)' target="_blank">ODHActivityPoi Types</a>, Type: Type</param>
        /// <param name="activitytype">Filtering by Activity Type defined by LTS ('null' = Filter disabled, possible values: BITMASK: 'Mountains = 1','Cycling = 2','Local tours = 4','Horses = 8','Hiking = 16','Running and fitness = 32','Cross-country ski-track = 64','Tobbogan run = 128','Slopes = 256','Lifts = 512'), (default:'1023' == ALL), , refers to <a href='https://tourism.opendatahub.com/v1/ActivityTypes?rawfilter=eq(Type,%27Type%27)' target="_blank">ActivityTypes</a>, Type: Type</param>
        /// <param name="poitype">Filtering by Poi Type defined by LTS ('null' = Filter disabled, possible values: BITMASK 'Doctors, Pharmacies = 1','Shops = 2','Culture and sights= 4','Nightlife and entertainment = 8','Public institutions = 16','Sports and leisure = 32','Traffic and transport = 64', 'Service providers' = 128, 'Craft' = 256, 'Associations' = 512, 'Companies' = 1024), (default:'2047' == ALL), , refers to <a href='https://tourism.opendatahub.com/v1/PoiTypes?rawfilter=eq(Type,%27Type%27)' target="_blank">PoiTypes</a>, Type: Type</param>
        /// <param name="subtype">Subtype of the ODHActivityPoi ('null' = Filter disabled, BITMASK Filter, available SubTypes depends on the selected Maintype) <a href='https://tourism.opendatahub.com/v1/ODHActivityPoiTypes?rawfilter=eq(Type,%27SubType%27)' target="_blank">ODHActivityPoi SubTypes</a>, or <a href='https://tourism.opendatahub.com/v1/ActivityTypes?rawfilter=eq(Type,%27SubType%27)' target="_blank">Activity SubTypes</a>, or <a href='https://tourism.opendatahub.com/v1/PoiTypes?rawfilter=eq(Type,%27SubType%27)' target="_blank">Poi SubTypes</a>, Type: SubType</param>
        /// <param name="level3type">Additional Type of Level 3 the ODHActivityPoi ('null' = Filter disabled, BITMASK Filter, available SubTypes depends on the selected Maintype, SubType reference to ODHActivityPoiTypes)</param>
        /// <param name="idlist">IDFilter (Separator ',' List of ODHActivityPoi IDs), (default:'null')</param>
        /// <param name="locfilter">Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a></param>
        /// <param name="areafilter">AreaFilter (Alternate Locfilter, can be combined with locfilter) (Separator ',' possible values: reg + REGIONID = (Filter by Region), tvs + TOURISMASSOCIATIONID = (Filter by Tourismassociation), skr + SKIREGIONID = (Filter by Skiregion), ska + SKIAREAID = (Filter by Skiarea), are + AREAID = (Filter by LTS Area), 'null' = No Filter), (default:'null')</param>
        /// <param name="langfilter">ODHActivityPoi Langfilter (returns only SmgPois available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="highlight">Hightlight Filter (possible values: 'false' = only ODHActivityPoi with Highlight false, 'true' = only ODHActivityPoi with Highlight true), (default:'null')</param>
        /// <param name="source">Source Filter (possible Values: 'null' Displays all ODHActivityPoi, 'None', 'ActivityData', 'PoiData', 'GastronomicData', 'MuseumData', 'Magnolia', 'Content', 'SuedtirolWein', 'ArchApp'), (default:'null')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="odhtagfilter">ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible (OR FILTER), available Tags reference to 'v1/ODHTag?validforentity=odhactivitypoi'), (default:'null')</param>
        /// <param name="odhtagfilter_and">ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible (AND FILTER), available Tags reference to 'v1/ODHTag?validforentity=odhactivitypoi'), (default:'null')</param>
        /// <param name="active">Active ODHActivityPoi Filter (possible Values: 'true' only active ODHActivityPoi, 'false' only not active ODHActivityPoi), (default:'null')</param>
        /// <param name="odhactive">ODH Active (Published) ODHActivityPoi Filter (Refers to field OdhActive) (possible Values: 'true' only published ODHActivityPoi, 'false' only not published ODHActivityPoi), (default:'null')</param>
        /// <param name="categorycodefilter">CategoryCode Filter (Only for ODHActivityTypes of type Gastronomy) (BITMASK) refers to <a href='https://tourism.opendatahub.com/v1/GastronomyTypes?rawfilter=eq(Type,\"CategoryCodes\")' target="_blank">GastronomyTypes</a>, Type: CategoryCodes</param>
        /// <param name="dishcodefilter">DishCode Filter (Only for ODHActivityTypes of type Gastronomy) (BITMASK) refers to <a href='https://tourism.opendatahub.com/v1/GastronomyTypes' target="_blank">GastronomyTypes</a>, Type: DishCodes</param>
        /// <param name="ceremonycodefilter">CeremonyCode Filter (Only for ODHActivityTypes of type Gastronomy) (BITMASK) refers to <a href='https://tourism.opendatahub.com/v1/GastronomyTypes' target="_blank">GastronomyTypes</a>, Type: CeremonyCodes</param>
        /// <param name="facilitycodefilter">FacilityCode Filter (Only for ODHActivityTypes of type Gastronomy) (BITMASK) refers to <a href='https://tourism.opendatahub.com/v1/GastronomyTypes' target="_blank">GastronomyTypes</a>, Type: with FacilityCodes_ prefix</param>
        /// <param name="cuisinecodefilter">CuisineCode Filter (Only for ODHActivityTypes of type Gastronomy) (BITMASK) refers to <a href='https://tourism.opendatahub.com/v1/GastronomyTypes' target="_blank">GastronomyTypes</a>, Type: CuisineCodes</param>
        /// <param name="distancefilter">Distance Range Filter (Separator ',' example Value: 15,40 Distance from 15 up to 40 Km), (default:'null')</param>
        /// <param name="altitudefilter">Altitude Range Filter (Separator ',' example Value: 500,1000 Altitude from 500 up to 1000 metres), (default:'null')</param>
        /// <param name="durationfilter">Duration Range Filter (Separator ',' example Value: 1,3 Duration from 1 to 3 hours), (default:'null')</param>
        /// <param name="difficultyfilter">Difficulty Filter (possible values: '1' = easy, '2' = medium, '3' = difficult), (default:'null')</param>
        /// <param name="hascc0image">Image CC0 Filter (Return only Data with at least one Image tagged with a CC0 License, possible Values: 'true', 'false'), (default:'null')</param>
        /// <param name="tagfilter">Filter on Tags. (Endpoint on v1/Tag) Syntax =and/or(Tag.Id,Tag.Id,Tag.Id) example or(summer,hiking) - and(themed hikes,family hikings) - or(hiking) - and(summer) - Combining and/or is not supported at the moment, default: 'null')</param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of ODH Activity Poi Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<ODHActivityPoiLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [TypeFilter(typeof(Filters.RequestInterceptorAttribute))]
        //[OdhCacheOutput(ClientTimeSpan = 0, ServerTimeSpan = 3600, CacheKeyGenerator = typeof(CustomCacheKeyGenerator), MustRevalidate = true)]
        [HttpGet, Route("ODHActivityPoi", Name = "GetODHActivityPoiList")]
        public async Task<IActionResult> Get(
            string? language = null,
            uint pagenumber = 1,
            PageSize pagesize = null!,
            string? type = "255",
            string? activitytype = null,
            string? poitype = null,
            string? subtype = null,
            string? level3type = null,
            string? idlist = null,
            string? locfilter = null,
            string? langfilter = null,
            string? areafilter = null,
            LegacyBool highlight = null!,
            string? source = null,
            string? odhtagfilter = null,
            string? odhtagfilter_and = null,
            LegacyBool odhactive = null!,
            LegacyBool active = null!,
            string? categorycodefilter = null,
            string? dishcodefilter = null,
            string? ceremonycodefilter = null,
            string? facilitycodefilter = null,
            string? cuisinecodefilter = null,
            string? difficultyfilter = null,
            string? distancefilter = null,
            string? altitudefilter = null,
            string? durationfilter = null,
            LegacyBool hasimage = null!,
            string? tagfilter = null,
            string? publishedon = null,
            string? updatefrom = null,
            string? seed = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            var polygonsearchresult = await Helper.GeoSearchHelper.GetPolygon(
                polygon,
                QueryFactory
            );

            return await GetFiltered(
                fields: fields ?? Array.Empty<string>(),
                language: language,
                pagenumber: pagenumber,
                pagesize: pagesize,
                type: type,
                subtypefilter: subtype,
                level3typefilter: level3type,
                searchfilter: searchfilter,
                idfilter: idlist,
                languagefilter: langfilter,
                sourcefilter: source,
                locfilter: locfilter,
                areafilter: areafilter,
                highlightfilter: highlight?.Value,
                active: active?.Value,
                smgactive: odhactive?.Value,
                smgtags: odhtagfilter,
                smgtagsand: odhtagfilter_and,
                categorycodefilter: categorycodefilter,
                dishcodefilter: dishcodefilter,
                ceremonycodefilter: ceremonycodefilter,
                facilitycodefilter: facilitycodefilter,
                cuisinecodefilter: cuisinecodefilter,
                activitytypefilter: activitytype,
                poitypefilter: poitype,
                difficultyfilter: difficultyfilter,
                distancefilter: distancefilter,
                altitudefilter: altitudefilter,
                durationfilter: durationfilter,
                hasimage: hasimage?.Value,
                tagfilter: tagfilter,
                publishedon: publishedon,
                seed: seed,
                lastchange: updatefrom,
                polygonsearchresult,
                geosearchresult,
                rawfilter: rawfilter,
                rawsort: rawsort,
                removenullvalues: removenullvalues,
                getasidarray: getasidarray,
                cancellationToken
            );
        }

        /// <summary>
        /// GET ODHActivityPoi Single
        /// </summary>
        /// <param name="id">ID of the Poi</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>ODHActivityPoi Object</returns>
        /// <response code="200">Object created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(ODHActivityPoiLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("ODHActivityPoi/{id}", Name = "SingleODHActivityPoi")]
        public async Task<IActionResult> GetODHActivityPoiSingle(
            string id,
            string? language,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetSingle(
                id,
                language,
                fields: fields ?? Array.Empty<string>(),
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        //Special GETTER

        /// <summary>
        /// GET ODHActivityPoi Types List
        /// </summary>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of ActivityPoiType Object</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [Obsolete("Deprecated, Use the Tags api (https://tourism.api.opendatahub.com/v1/Tag?validforentity=odhactivitypoi)")]
        [ProducesResponseType(typeof(IEnumerable<SmgPoiTypes>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("ODHActivityPoiTypes")]
        public async Task<IActionResult> GetAllODHActivityPoiTypesList(
            string? language,
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? idlist = null,
            string? seed = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetSmgPoiTypesList(
                pagenumber,
                pagesize,
                idlist,
                seed,
                language,
                fields: fields ?? Array.Empty<string>(),
                searchfilter,
                rawfilter,
                rawsort,
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        /// <summary>
        /// GET ODHActivityPoi Types Single
        /// </summary>
        /// <param name="id">ID of the ODHActivityPoi Type</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>ActivityPoiType Object</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [Obsolete("Deprecated, Use the Tags api (https://tourism.api.opendatahub.com/v1/Tag/{id})")]
        [ProducesResponseType(typeof(SmgPoiTypes), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("ODHActivityPoiTypes/{*id}", Name = "SingleODHActivityPoiTypes")]
        public async Task<IActionResult> GetAllODHActivityPoiTypesSingle(
            string id,
            string? language,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetSmgPoiTypesSingle(
                id,
                language,
                fields: fields ?? Array.Empty<string>(),
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        #endregion

        #region GETTER

        private Task<IActionResult> GetFiltered(
            string[] fields,
            string? language,
            uint pagenumber,
            int? pagesize,
            string? type,
            string? subtypefilter,
            string? level3typefilter,
            string? searchfilter,
            string? idfilter,
            string? languagefilter,
            string? sourcefilter,
            string? locfilter,
            string? areafilter,
            bool? highlightfilter,
            bool? active,
            bool? smgactive,
            string? smgtags,
            string? smgtagsand,
            string? categorycodefilter,
            string? dishcodefilter,
            string? ceremonycodefilter,
            string? facilitycodefilter,
            string? cuisinecodefilter,
            string? activitytypefilter,
            string? poitypefilter,
            string? difficultyfilter,
            string? distancefilter,
            string? altitudefilter,
            string? durationfilter,
            bool? hasimage,
            string? tagfilter,
            string? publishedon,
            string? seed,
            string? lastchange,
            GeoPolygonSearchResult? polygonsearchresult,
            PGGeoSearchResult geosearchresult,
            string? rawfilter,
            string? rawsort,
            bool removenullvalues,
            bool getasidarray,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                ODHActivityPoiHelper myodhactivitypoihelper =
                    await ODHActivityPoiHelper.CreateAsync(
                        queryFactory: QueryFactory,
                        typefilter: type,
                        subtypefilter: subtypefilter,
                        level3typefilter: level3typefilter,
                        idfilter: idfilter,
                        locfilter: locfilter,
                        areafilter: areafilter,
                        languagefilter: languagefilter,
                        sourcefilter: sourcefilter,
                        highlightfilter: highlightfilter,
                        activefilter: active,
                        smgactivefilter: smgactive,
                        smgtags: smgtags,
                        smgtagsand: smgtagsand,
                        lastchange: lastchange,
                        categorycodefilter: categorycodefilter,
                        dishcodefilter: dishcodefilter,
                        ceremonycodefilter: ceremonycodefilter,
                        facilitycodefilter: facilitycodefilter,
                        cuisinecodefilter: cuisinecodefilter,
                        activitytypefilter: activitytypefilter,
                        poitypefilter: poitypefilter,
                        distancefilter: distancefilter,
                        altitudefilter: altitudefilter,
                        durationfilter: durationfilter,
                        difficultyfilter: difficultyfilter,
                        hasimagefilter: hasimage,
                        tagfilter: tagfilter,
                        publishedonfilter: publishedon,
                        cancellationToken
                    );

                var query = QueryFactory
                    .Query()
                    .When(getasidarray, x => x.Select("id"))
                    .When(!getasidarray, x => x.SelectRaw("data"))
                    .From("smgpois")
                    .ODHActivityPoiWhereExpression(
                        idlist: myodhactivitypoihelper.idlist,
                        typelist: myodhactivitypoihelper.typelist,
                        subtypelist: myodhactivitypoihelper.subtypelist,
                        level3typelist: myodhactivitypoihelper.level3typelist,
                        smgtaglist: myodhactivitypoihelper.smgtaglist,
                        smgtaglistand: myodhactivitypoihelper.smgtaglistand,
                        districtlist: myodhactivitypoihelper.districtlist,
                        municipalitylist: myodhactivitypoihelper.municipalitylist,
                        tourismvereinlist: myodhactivitypoihelper.tourismvereinlist,
                        regionlist: myodhactivitypoihelper.regionlist,
                        arealist: myodhactivitypoihelper.arealist,
                        sourcelist: myodhactivitypoihelper.sourcelist,
                        languagelist: myodhactivitypoihelper.languagelist,
                        highlight: myodhactivitypoihelper.highlight,
                        activefilter: myodhactivitypoihelper.active,
                        smgactivefilter: myodhactivitypoihelper.smgactive,
                        categorycodeslist: myodhactivitypoihelper.categorycodesids,
                        dishcodeslist: myodhactivitypoihelper.dishcodesids,
                        ceremonycodeslist: myodhactivitypoihelper.ceremonycodesids,
                        facilitycodeslist: myodhactivitypoihelper.facilitycodesids,
                        activitytypelist: myodhactivitypoihelper.activitytypelist,
                        poitypelist: myodhactivitypoihelper.poitypelist,
                        difficultylist: myodhactivitypoihelper.difficultylist,
                        distance: myodhactivitypoihelper.distance,
                        distancemin: myodhactivitypoihelper.distancemin,
                        distancemax: myodhactivitypoihelper.distancemax,
                        duration: myodhactivitypoihelper.duration,
                        durationmin: myodhactivitypoihelper.durationmin,
                        durationmax: myodhactivitypoihelper.durationmax,
                        altitude: myodhactivitypoihelper.altitude,
                        altitudemin: myodhactivitypoihelper.altitudemin,
                        altitudemax: myodhactivitypoihelper.altitudemax,
                        hasimage: myodhactivitypoihelper.hasimage,
                        tagdict: myodhactivitypoihelper.tagdict,
                        publishedonlist: myodhactivitypoihelper.publishedonlist,
                        searchfilter: searchfilter,
                        language: language,
                        lastchange: myodhactivitypoihelper.lastchange,
                        additionalfilter: additionalfilter,
                        userroles: UserRolesToFilter
                    )
                    .When(
                        polygonsearchresult != null,
                        x =>
                            x.WhereRaw(
                                PostgresSQLHelper.GetGeoWhereInPolygon_GeneratedColumns(
                                    polygonsearchresult.wktstring,
                                    polygonsearchresult.polygon,
                                    polygonsearchresult.srid,
                                    polygonsearchresult.operation,
                                    polygonsearchresult.reduceprecision
                                )
                            )
                    )
                    .ApplyRawFilter(rawfilter)
                    .ApplyOrdering_GeneratedColumns(ref seed, geosearchresult, rawsort);

                //IF getasidarray set simply return array of ids
                if (getasidarray)
                {
                    return await query.GetAsync<string>();
                }

                // Get paginated data
                var data = await query.PaginateAsync<JsonRaw>(
                    page: (int)pagenumber,
                    perPage: pagesize ?? 25
                );

                var dataTransformed = data.List.Select(raw =>
                    raw.TransformRawData(
                        language,
                        fields,
                        filteroutNullValues: removenullvalues,
                        urlGenerator: UrlGenerator,
                        fieldstohide: null
                    )
                );

                uint totalpages = (uint)data.TotalPages;
                uint totalcount = (uint)data.Count;

                return ResponseHelpers.GetResult(
                    pagenumber,
                    totalpages,
                    totalcount,
                    seed,
                    dataTransformed,
                    Url
                );
            });
        }

        private Task<IActionResult> GetSingle(
            string id,
            string? language,
            string[] fields,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                var query = QueryFactory
                    .Query("smgpois")
                    .Select("data")
                    .Where("gen_id", id.ToLower())
                    .When(
                        !String.IsNullOrEmpty(additionalfilter),
                        q => q.FilterAdditionalDataByCondition(additionalfilter)
                    )
                    .FilterDataByAccessRoles(UserRolesToFilter)
                    .FilterReducedDataByRoles(UserRolesToFilter);

                var data = await query.FirstOrDefaultAsync<JsonRaw?>();

                return data?.TransformRawData(
                    language,
                    fields,
                    filteroutNullValues: removenullvalues,
                    urlGenerator: UrlGenerator,
                    fieldstohide: null
                );
            });
        }

        #endregion

        #region CATEGORIES

        private Task<IActionResult> GetSmgPoiTypesList(
            uint? pagenumber,
            int? pagesize,
            string? idfilter,
            string? seed,
            string? language,
            string[] fields,
            string? searchfilter,
            string? rawfilter,
            string? rawsort,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                var query = QueryFactory
                    .Query("smgpoitypes")
                    .SelectRaw("data")
                    .When(idfilter != null, q => q.IdLowerFilter(CommonListCreator.CreateIdList(idfilter?.ToLower())))
                    .SearchFilter(
                        PostgresSQLWhereBuilder.TypeDescFieldsToSearchFor(language),
                        searchfilter
                    )
                    .ApplyRawFilter(rawfilter)
                    .OrderOnlyByRawSortIfNotNull(rawsort);

                if (pagenumber.HasValue)
                {
                    // Get paginated data
                    var data = await query.PaginateAsync<JsonRaw>(
                        page: (int)pagenumber,
                        perPage: pagesize ?? 25
                    );

                    var dataTransformed = data.List.Select(raw =>
                        raw.TransformRawData(
                            language,
                            fields,
                            filteroutNullValues: removenullvalues,
                            urlGenerator: UrlGenerator,
                            fieldstohide: null
                        )
                    );

                    uint totalpages = (uint)data.TotalPages;
                    uint totalcount = (uint)data.Count;

                    return ResponseHelpers.GetResult(
                        pagenumber.Value,
                        totalpages,
                        totalcount,
                        seed,
                        dataTransformed,
                        Url
                    );
                }
                else
                {
                    var data = await query.GetAsync<JsonRaw?>();

                    return data.Select(raw =>
                        raw?.TransformRawData(
                            language,
                            fields,
                            filteroutNullValues: removenullvalues,
                            urlGenerator: UrlGenerator,
                            fieldstohide: null
                        )
                    );
                }
            });
        }

        private Task<IActionResult> GetSmgPoiTypesSingle(
            string id,
            string? language,
            string[] fields,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                var query = QueryFactory
                    .Query("smgpoitypes")
                    .Select("data")
                    .Where("id", id.ToLower());

                var data = await query.FirstOrDefaultAsync<JsonRaw?>();

                return data?.TransformRawData(
                    language,
                    fields,
                    filteroutNullValues: removenullvalues,
                    urlGenerator: UrlGenerator,
                    fieldstohide: null
                );
            });
        }

        #endregion

        #region POST PUT DELETE

        /// <summary>
        /// POST Insert new ODHActivityPoi
        /// </summary>
        /// <param name="odhactivitypoi">ODHActivityPoi Object</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[InvalidateCacheOutput(typeof(ODHActivityPoiController), nameof(GetODHActivityPoiList))]
        //[Authorize(Roles = "DataWriter,DataCreate,ODHActivityPoiWriter,ODHActivityPoiManager,ODHActivityPoiCreate,SmgPoiManager,SmgPoiCreate")]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [AuthorizeODH(PermissionAction.Create)]
        [HttpPost, Route("ODHActivityPoi")]
        public Task<IActionResult> Post([FromBody] ODHActivityPoiLinked odhactivitypoi, bool generateid = true)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Create", out var additionalfilter);
              
                //Generate Id or use the assigned
                if (generateid)
                    odhactivitypoi.Id = Helper.IdGenerator.GenerateIDFromType(odhactivitypoi);
                else if (String.IsNullOrEmpty(odhactivitypoi.Id))
                    throw new Exception("Id is null");

                //GENERATE HasLanguage
                odhactivitypoi.CheckMyInsertedLanguages(
                    new List<string> { "de", "en", "it", "nl", "cs", "pl", "ru", "fr" }
                );

                //POPULATE LocationInfo
                odhactivitypoi.LocationInfo = await odhactivitypoi.UpdateLocationInfoExtension(
                    QueryFactory
                );

                //POPULATE Automatic Assigned ODHTags
                ODHTagHelper.SetMainCategorizationForODHActivityPoi(odhactivitypoi);

                //Add english Translations for TagIds 
                await GenericTaggingHelper.AddTagIdsToODHActivityPoi(odhactivitypoi, await GenericTaggingHelper.GetAllGenericTagsfromJson(settings.JsonConfig.Jsondir));
                //Populate Tags (Id/Source/Type)
                await odhactivitypoi.UpdateTagsExtension(QueryFactory);

                //POPULATE Categories
                await ODHTagHelper.GetCategoriesFromAssignedODHTags(
                    odhactivitypoi,
                    settings.JsonConfig.Jsondir
                );

                //CHECK AdditionalProperties Schema
                var errordict = await AdditionalPropertiesHelper.CheckAdditionalProperties(
                    odhactivitypoi
                );
                //Return Badrequest if Type is not matching
                if (errordict != null && errordict.Count > 0)
                    return BadRequest(String.Join(",", errordict.Values));

                //DistanceCalculation
                await odhactivitypoi.UpdateDistanceCalculation(QueryFactory);

                //TODO check for Reduced Data

                //Trim all strings
                odhactivitypoi.TrimStringProperties();

                return await UpsertData<ODHActivityPoiLinked>(
                    odhactivitypoi,
                    new DataInfo("smgpois", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing ODHActivityPoi
        /// </summary>
        /// <param name="id">ODHActivityPoi Id</param>
        /// <param name="odhactivitypoi">ODHActivityPoi Object</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[InvalidateCacheOutput(typeof(ODHActivityPoiController), nameof(GetODHActivityPoiList))]
        //[Authorize(Roles = "DataWriter,DataModify,ODHActivityPoiWriter,ODHActivityPoiManager,ODHActivityPoiModify,ODHActivityPoiUpdate,SmgPoiManager,SmgPoiModify")]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [AuthorizeODH(PermissionAction.Update)]
        [HttpPut, Route("ODHActivityPoi/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] ODHActivityPoiLinked odhactivitypoi)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Update", out var additionalfilter);

                //Check ID uppercase lowercase
                Helper.IdGenerator.CheckIdFromType(odhactivitypoi);

                //GENERATE HasLanguage
                odhactivitypoi.CheckMyInsertedLanguages(
                    new List<string> { "de", "en", "it", "nl", "cs", "pl", "ru", "fr" }
                );

                //POPULATE LocationInfo
                odhactivitypoi.LocationInfo = await odhactivitypoi.UpdateLocationInfoExtension(
                    QueryFactory
                );


                //POPULATE Automatic Assigned ODHTags
                ODHTagHelper.SetMainCategorizationForODHActivityPoi(odhactivitypoi);

                //Add english Translations for TagIds 
                await GenericTaggingHelper.AddTagIdsToODHActivityPoi(odhactivitypoi, await GenericTaggingHelper.GetAllGenericTagsfromJson(settings.JsonConfig.Jsondir));

                //Populate Tags (Id/Source/Type)
                await odhactivitypoi.UpdateTagsExtension(QueryFactory);


                //POPULATE Categories
                await ODHTagHelper.GetCategoriesFromAssignedODHTags(
                    odhactivitypoi,
                    settings.JsonConfig.Jsondir
                );

                //CHECK AdditionalProperties Schema
                var errordict = await AdditionalPropertiesHelper.CheckAdditionalProperties(
                    odhactivitypoi
                );
                //Return Badrequest if Type is not matching
                if (errordict != null && errordict.Count > 0)
                    return BadRequest(String.Join(",", errordict.Values));

                //DistanceCalculation
                await odhactivitypoi.UpdateDistanceCalculation(QueryFactory);

                //TODO check for Reduced Data

                //Trim all strings
                odhactivitypoi.TrimStringProperties();

                return await UpsertData<ODHActivityPoiLinked>(
                    odhactivitypoi,
                    new DataInfo("smgpois", CRUDOperation.Update, true),
                    new CompareConfig(true, true),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE ODHActivityPoi by Id
        /// </summary>
        /// <param name="id">ODHActivityPoi Id</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[InvalidateCacheOutput(typeof(ODHActivityPoiController), nameof(GetODHActivityPoiList))]
        //[Authorize(Roles = "DataWriter,DataDelete,ODHActivityPoiWriter,ODHActivityPoiManager,ODHActivityPoiDelete,SmgPoiManager,SmgPoiDelete")]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [AuthorizeODH(PermissionAction.Delete)]
        [HttpDelete, Route("ODHActivityPoi/{id}")]
        public Task<IActionResult> Delete(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Delete", out var additionalfilter);

                //Check ID uppercase lowercase
                id = Helper.IdGenerator.CheckIdFromType<ODHActivityPoiLinked>(id);

                return await DeleteData<ODHActivityPoiLinked>(
                    id,
                    new DataInfo("smgpois", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        #endregion
    }
}
