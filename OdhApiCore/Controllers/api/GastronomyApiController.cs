// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.CacheOutput;
using DataModel;
using Helper;
using Helper.Generic;
using Helper.Identity;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OdhApiCore.Responses;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiCore.Controllers
{
    /// <summary>
    /// Gastronomy Api (data provided by LTS GastronomicData) Deprecated! Please use Endpoint ODHActivityPoi?type=32
    /// </summary>
    [Obsolete("Please use ODHActivityPoi Endpoint")]
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class GastronomyController : OdhController
    {
        public GastronomyController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<GastronomyController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        #region SWAGGER Exposed API

        /// <summary>
        /// GET Gastronomy List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="idlist">IDFilter (Separator ',' List of Gastronomy IDs), (default:'null')</param>
        /// <param name="locfilter">Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a></param>
        /// <param name="dishcodefilter">Dish Code Filter (BITMASK values: 1 = (Speisen), 2 = (Vorspeise), 4 = (Hauptspeise), 8 = (Nachspeise), 16 = (Tagesgericht), 32 = (Menü), 64 = (Degustationsmenü), 128 = (Kindermenüs), 256 = (Mittagsmenüs)</param>
        /// <param name="ceremonycodefilter">Ceremony Code Filter (BITMASK  values: 1 = (Familienfeiern), 2 = (Hochzeiten), 4 = (Geburtstagsfeiern), 8 = (Firmenessen), 16 = (Weihnachtsessen), 32 = (Sylvestermenü), 64 = (Seminare / Tagungen), 128 = (Versammlungen)</param>
        /// <param name="categorycodefilter">Category Code Filter (BITMASK  values: 1 = (Restaurant), 2 = (Bar / Café / Bistro), 4 = (Pub / Disco), 8 = (Apres Ski), 16 = (Jausenstation), 32 = (Pizzeria), 64 = (Bäuerlicher Schankbetrieb), 128 = (Buschenschank), 256 = (Hofschank), 512 = (Törggele Lokale), 1024 = (Schnellimbiss), 2048 = (Mensa), 4096 = (Vinothek /Weinhaus / Taverne), 8192 = (Eisdiele), 16348 = (Gasthaus), 32768 = (Gasthof), 65536 = (Braugarten), 131072 = (Schutzhütte), 262144 = (Alm), 524288 = (Skihütte)</param>
        /// <param name="facilitycodefilter">Facility Code Filter (BITMASK  values: 1 = (American Express), 2 = (Diners Club), 4 = (Eurocard / Mastercard), 8 = (Visa), 16 = (Hunde erlaubt), 32 = (Geeignet für Busse), 64 = (Garten), 128 = (Garagen), 256 = (Bierbar), 512 = (Kinderspielplatz), 1024 = (Spielzimmer), 2048 = (Spielplatz), 4096 = (Parkplätze), 8192 = (Raucherräume), 16348 = (Terrasse), 32768 = (Behindertengerecht), 65536 = (Biergarten), 131072 = (Aussichtsterrasse), 262144 = (Wintergarten), 524288 = (Gault Millau Südtirol), 1048576 = (Guida Espresso), 2097152 = (Gambero Rosso), 4194304 = (Feinschmecker), 8388608 = (Aral Schlemmer Atlas), 16777216 = (Varta Führer), 33554432 = (Bertelsmann), 67108864 = (Preis für Südtiroler Weinkultur), 134217728 = (Michelin), 268435456 = (Roter Hahn), 536870912 = (Tafelspitz))</param>
        /// <param name="cuisinecodefilter">Cuisine Code Filter (BITMASK  values: 1 = (Vegetarische Küche), 2 = (Glutenfreie Küche), 4 = (Laktosefreie Kost), 8 = (Warme Küche), 16 = (Südtiroler Spezialitäten), 32 = (Gourmet Küche), 64 = (Italienische Küche), 128 = (Internationale Küche), 256 = (Pizza), 512 = (Fischspezialitäten), 1024 = (Asiatische Küche), 2048 = (Wildspezialitäten), 4096 = (Produkte eigener Erzeugung), 8192 = (Diätküche), 16348 = (Grillspezialitäten), 32768 = (Ladinische Küche), 65536 = (Kleine Karte), 131072 = (Fischwochen), 262144 = (Spargelwochen), 524288 = (Lammwochen), 1048576 = (Wildwochen), 2097152 = (Vorspeisewochen), 4194304 = (Nudelwochen), 8388608 = (Kräuterwochen), 16777216 = (Kindermenüs), 33554432 = (Mittagsmenüs))</param>
        /// <param name="odhtagfilter">ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=gastronomy'), (default:'null')</param>
        /// <param name="active">Active Gastronomies Filter (possible Values: 'true' only Active Gastronomies, 'false' only Disabled Gastronomies), (default:'null'</param>
        /// <param name="odhactive">ODH Active (Published) Gastronomies Filter (Refers to field OdhActive) (possible Values: 'true' only published Gastronomies, 'false' only not published Gastronomies), (default:'null')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of Gastronomy Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<GastronomyLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[OdhCacheOutput(ClientTimeSpan = 0, ServerTimeSpan = 3600, CacheKeyGenerator = typeof(CustomCacheKeyGenerator), MustRevalidate = true)]
        [HttpGet, Route("Gastronomy")]
        public async Task<IActionResult> GetGastronomyList(
            string? language = null,
            uint pagenumber = 1,
            PageSize pagesize = null!,
            string? idlist = null,
            string? locfilter = null,
            string? dishcodefilter = null,
            string? ceremonycodefilter = null,
            string? categorycodefilter = null,
            string? facilitycodefilter = null,
            string? cuisinecodefilter = null,
            string? odhtagfilter = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? updatefrom = null,
            string? seed = null,
            string? langfilter = null,
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
                dishcodefilter: dishcodefilter,
                categorycodefilter: categorycodefilter,
                facilitycodefilter: facilitycodefilter,
                cuisinecodefilter: cuisinecodefilter,
                ceremonycodefilter: ceremonycodefilter,
                idfilter: idlist,
                searchfilter: searchfilter,
                locfilter: locfilter,
                active: active,
                smgactive: odhactive,
                langfilter: langfilter,
                smgtags: odhtagfilter,
                seed: seed,
                lastchange: updatefrom,
                polygonsearchresult: polygonsearchresult,
                geosearchresult: geosearchresult,
                rawfilter: rawfilter,
                rawsort: rawsort,
                removenullvalues: removenullvalues,
                getasidarray: getasidarray,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// GET Gastronomy Single
        /// </summary>
        /// <param name="id">ID of the Gastronomy</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Gastronomy Object</returns>
        [ProducesResponseType(typeof(GastronomyLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Gastronomy/{id}", Name = "SingleGastronomy")]
        public async Task<IActionResult> GetGastronomySingle(
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

        /// <summary>
        /// GET Gastronomy Types List
        /// </summary>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of GastronomyTypes Object</returns>
        [ProducesResponseType(typeof(IEnumerable<GastronomyTypes>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,GastroReader")]
        [HttpGet, Route("GastronomyTypes")]
        public async Task<IActionResult> GetAllGastronomyTypesList(
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
            return await GetGastronomyTypesListAsync(
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
        /// GET Gastronomy Types Single
        /// </summary>
        /// <param name="id">ID of the Gastronomy Type</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>GastronomyTypes Object</returns>
        [ProducesResponseType(typeof(GastronomyTypes), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,GastroReader")]
        [HttpGet, Route("GastronomyTypes/{id}", Name = "SingleGastronomyTypes")]
        public async Task<IActionResult> GetAllGastronomyTypesList(
            string id,
            string? language,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetGastronomyTypesSingleAsync(
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
            string? categorycodefilter,
            string? dishcodefilter,
            string? ceremonycodefilter,
            string? facilitycodefilter,
            string? cuisinecodefilter,
            string? idfilter,
            string? searchfilter,
            string? locfilter,
            bool? active,
            bool? smgactive,
            string? langfilter,
            string? smgtags,
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

                GastronomyHelper mygastronomyhelper = await GastronomyHelper.CreateAsync(
                    QueryFactory,
                    idfilter,
                    locfilter,
                    categorycodefilter,
                    dishcodefilter,
                    ceremonycodefilter,
                    facilitycodefilter,
                    cuisinecodefilter,
                    active,
                    smgactive,
                    smgtags,
                    lastchange,
                    langfilter,
                    cancellationToken
                );

                var query = QueryFactory
                    .Query()
                    .When(getasidarray, x => x.Select("id"))
                    .When(!getasidarray, x => x.SelectRaw("data"))
                    .From("gastronomies")
                    .GastronomyWhereExpression(
                        idlist: mygastronomyhelper.idlist,
                        dishcodeslist: mygastronomyhelper.dishcodesids,
                        ceremonycodeslist: mygastronomyhelper.ceremonycodesids,
                        categorycodeslist: mygastronomyhelper.categorycodesids,
                        facilitycodeslist: mygastronomyhelper.facilitycodesids,
                        smgtaglist: mygastronomyhelper.smgtaglist,
                        districtlist: mygastronomyhelper.districtlist,
                        municipalitylist: mygastronomyhelper.municipalitylist,
                        tourismvereinlist: mygastronomyhelper.tourismvereinlist,
                        regionlist: mygastronomyhelper.regionlist,
                        activefilter: mygastronomyhelper.active,
                        smgactivefilter: mygastronomyhelper.smgactive,
                        searchfilter: searchfilter,
                        language: language,
                        lastchange: mygastronomyhelper.lastchange,
                        languagelist: mygastronomyhelper.languagelist,
                        additionalfilter: additionalfilter,
                        userroles: UserRolesToFilter
                    )
                    .ApplyRawFilter(rawfilter)
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
                    .Query("gastronomies")
                    .Select("data")
                    .Where("id", id.ToUpper())
                    .When(
                        !String.IsNullOrEmpty(additionalfilter),
                        q => q.FilterAdditionalDataByCondition(additionalfilter)
                    )
                    .FilterDataByAccessRoles(UserRolesToFilter);

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

        /// <summary>
        /// GET Gastronomy Types List
        /// </summary>
        /// <returns>Collection of GastronomyTypes Object</returns>
        private Task<IActionResult> GetGastronomyTypesListAsync(
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
                    .Query("gastronomytypes")
                    .SelectRaw("data")
                     .When(idfilter != null, q => q.IdUpperFilter(CommonListCreator.CreateIdList(idfilter?.ToUpper())))
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

        /// <summary>
        /// GET Gastronomy Types Single
        /// </summary>
        /// <returns>GastronomyTypes Object</returns>
        private Task<IActionResult> GetGastronomyTypesSingleAsync(
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
                    .Query("gastronomytypes")
                    .Select("data")
                    .Where("id", id.ToUpper());

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
        /// POST Insert new Gastronomy
        /// </summary>
        /// <param name="gastronomy">Gastronomy Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [InvalidateCacheOutput(nameof(GetGastronomyList))]
        //[Authorize(Roles = "DataWriter,DataCreate,GastronomyManager,GastronomyCreate")]
        [AuthorizeODH(PermissionAction.Create)]
        [HttpPost, Route("Gastronomy")]
        public Task<IActionResult> Post([FromBody] GastronomyLinked gastronomy)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Create", out var additionalfilter);

                gastronomy.Id = Helper.IdGenerator.GenerateIDFromType(gastronomy);
                gastronomy.CheckMyInsertedLanguages(new List<string> { "de", "en", "it" });

                return await UpsertData<GastronomyLinked>(
                    gastronomy,
                    new DataInfo("gastronomies", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing Gastronomy
        /// </summary>
        /// <param name="id">Gastronomy Id</param>
        /// <param name="gastronomy">Gastronomy Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [InvalidateCacheOutput(nameof(GetGastronomyList))]
        //[Authorize(Roles = "DataWriter,DataModify,GastronomyManager,GastronomyModify,GastronomyUpdate")]
        [AuthorizeODH(PermissionAction.Update)]
        [HttpPut, Route("Gastronomy/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] GastronomyLinked gastronomy)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Update", out var additionalfilter);

                gastronomy.Id = Helper.IdGenerator.CheckIdFromType<GastronomyLinked>(id);
                gastronomy.CheckMyInsertedLanguages(new List<string> { "de", "en", "it" });

                return await UpsertData<GastronomyLinked>(
                    gastronomy,
                    new DataInfo("gastronomies", CRUDOperation.Update, true),
                    new CompareConfig(true, true),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE Gastronomy by Id
        /// </summary>
        /// <param name="id">Gastronomy Id</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [InvalidateCacheOutput(nameof(GetGastronomyList))]
        //[Authorize(Roles = "DataWriter,DataDelete,GastronomyManager,GastronomyDelete")]
        [AuthorizeODH(PermissionAction.Delete)]
        [HttpDelete, Route("Gastronomy/{id}")]
        public Task<IActionResult> Delete(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<GastronomyLinked>(id);

                return await DeleteData<GastronomyLinked>(
                    id,
                    new DataInfo("gastronomies", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        #endregion
    }
}
