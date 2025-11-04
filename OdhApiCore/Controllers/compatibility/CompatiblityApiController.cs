// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DataModel;
using Helper;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiCore.Controllers.api
{
    /// <summary>
    /// Compatiblity Api (Old Routes &amp; methods for compatiblity reasons)
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class CompatiblityApiController : OdhController
    {
        public CompatiblityApiController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<CompatiblityApiController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        #region PoiController

        ////Reduced GETTER

        ///// <summary>
        ///// GET Poi List Reduced
        ///// </summary>
        ///// <param name="language">Localization Language, (default:'en')</param>
        ///// <param name="poitype">Type of the Poi ('null' = Filter disabled, possible values: BITMASK 'Doctors, Pharmacies = 1','Shops = 2','Culture and sights= 4','Nightlife and entertainment = 8','Public institutions = 16','Sports and leisure = 32','Traffic and transport = 64', 'Service providers' = 128, 'Craft' = 256), (default:'511' == ALL), REFERENCE TO: GET /api/PoiTypes </param>
        ///// <param name="subtype">Subtype of the Activity (BITMASK Filter = available SubTypes depends on the selected poiType), (default:'null')</param>
        ///// <param name="locfilter">Locfilter (Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMASSOCIATIONID = (Filter by Tourismassociation), 'null' = No Filter), (default:'null')</param>
        ///// <param name="areafilter">AreaFilter (Alternate Locfilter, can be combined with locfilter) (Separator ',' possible values: reg + REGIONID = (Filter by Region), tvs + TOURISMASSOCIATIONID = (Filter by Tourismassociation), skr + SKIREGIONID = (Filter by Skiregion), ska + SKIAREAID = (Filter by Skiarea), are + AREAID = (Filter by LTS Area), 'null' = No Filter), (default:'null')</param>
        ///// <param name="highlight">Hightlight Filter (possible values: 'false' = only Pois with Highlight false, 'true' = only Pois with Highlight true), (default:'null')</param>
        ///// <param name="odhtagfilter">ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=poi'), (default:'null')</param>
        ///// <param name="active">Active Pois Filter (possible Values: 'true' only Active Pois, 'false' only Disabled Pois</param>
        ///// <param name="odhactive">ODH Active (Published) Pois Filter (Refers to field SmgActive) Pois Filter (possible Values: 'true' only published Pois, 'false' only not published Pois, (default:'null')</param>
        ///// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        ///// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        ///// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        ///// <returns>Collection of Poi Reduced Objects</returns>
        //[ProducesResponseType(typeof(IEnumerable<ActivityPoiReduced>), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        ////[Authorize(Roles = "DataReader,PoiReader")]
        //[HttpGet, Route("PoiReduced")]
        //public async Task<IActionResult> GetPoiReduced(
        //    string? language = "en",
        //    string? poitype = "511",
        //    string? subtype = null,
        //    string? locfilter = null,
        //    string? areafilter = null,
        //    LegacyBool highlight = null!,
        //    string? odhtagfilter = null,
        //    LegacyBool active = null!,
        //    LegacyBool odhactive = null!,
        //    string? latitude = null,
        //    string? longitude = null,
        //    string? radius = null,
        //    [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
        //    string? searchfilter = null,
        //    string? rawfilter = null,
        //    string? rawsort = null,
        //    CancellationToken cancellationToken = default
        //)
        //{
        //    //TODO
        //    //CheckOpenData(User);

        //    var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
        //        latitude,
        //        longitude,
        //        radius
        //    );

        //    return await GetPoiReduced(
        //        language?.ToLower(),
        //        poitype,
        //        subtype,
        //        locfilter,
        //        areafilter,
        //        highlight,
        //        active,
        //        odhactive,
        //        odhtagfilter,
        //        fields: fields ?? Array.Empty<string>(),
        //        rawfilter,
        //        rawsort,
        //        searchfilter,
        //        geosearchresult,
        //        cancellationToken
        //    );
        //}

        ///// <summary>
        ///// GET Reduced POI List
        ///// </summary>
        ///// <param name="language">Localization Language</param>
        ///// <param name="poitype">Type of the Poi (possible values: STRINGS: 'Ärtze, Apotheken','Geschäfte und Dienstleister','Kultur und Sehenswürdigkeiten','Nachtleben und Unterhaltung','Öffentliche Einrichtungen','Sport und Freizeit','Verkehr und Transport' : BITMASK also possible: 'Ärtze, Apotheken = 1','Geschäfte und Dienstleister = 2','Kultur und Sehenswürdigkeiten = 4','Nachtleben und Unterhaltung = 8','Öffentliche Einrichtungen = 16','Sport und Freizeit = 32','Verkehr und Transport = 64')</param>
        ///// <param name="subtypefilter">Subtype of the Poi ('null' = Filter disabled, available Subtypes depends on the activitytype BITMASK)</param>
        ///// <param name="locfilter">Locfilter (Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), 'null' = No Filter)</param>
        ///// <param name="areafilter">AreaFilter (Separator ',' IDList of AreaIDs separated by ',', 'null' : Filter disabled)</param>
        ///// <param name="highlightfilter">Highlight Filter (Show only Highlights possible values: 'true' : show only Highlight Pois, 'null' Filter disabled)</param>
        ///// <param name="active">Active Filter (possible Values: 'null' Displays all Pois, 'true' only Active Pois, 'false' only Disabled Pois</param>
        ///// <param name="smgactive">SMGActive Filter (possible Values: 'null' Displays all Pois, 'true' only SMG Active Pois, 'false' only SMG Disabled Pois</param>
        ///// <param name="smgtags">SMGTag Filter (String, Separator ',' more SMGTags possible, 'null' = No Filter)</param>
        ///// <returns>Collection of Reduced Poi Objects</returns>
        //private Task<IActionResult> GetPoiReduced(
        //    string? language,
        //    string? poitype,
        //    string? subtypefilter,
        //    string? locfilter,
        //    string? areafilter,
        //    bool? highlightfilter,
        //    bool? active,
        //    bool? smgactive,
        //    string? smgtags,
        //    string[] fields,
        //    string? rawfilter,
        //    string? rawsort,
        //    string? searchfilter,
        //    PGGeoSearchResult geosearchresult,
        //    CancellationToken cancellationToken
        //)
        //{
        //    return DoAsyncReturn(async () =>
        //    {
        //        //Additional Read Filters to Add Check
        //        AdditionalFiltersToAddEndpoint("Poi")
        //            .TryGetValue("Read", out var additionalfilter);

        //        PoiHelper mypoihelper = await PoiHelper.CreateAsync(
        //            QueryFactory,
        //            poitype: poitype,
        //            subtypefilter: subtypefilter,
        //            idfilter: null,
        //            locfilter: locfilter,
        //            areafilter: areafilter,
        //            highlightfilter: highlightfilter,
        //            activefilter: active,
        //            smgactivefilter: smgactive,
        //            smgtags: smgtags,
        //            lastchange: null,
        //            langfilter: language,
        //            cancellationToken
        //        );

        //        string select =
        //            $"data#>>'\\{{Id\\}}' as \"Id\", data#>>'\\{{Detail,{language},Title\\}}' as \"Name\"";

        //        //Custom Fields filter
        //        if (fields.Length > 0)
        //            select += string.Join(
        //                "",
        //                fields
        //                    .Where(x => x != "Id")
        //                    .Select(field =>
        //                        $", data#>'\\{{{field.Replace(".", ",")}\\}}' as \"{field}\""
        //                    )
        //            );

        //        var query = (XQuery)
        //            QueryFactory
        //                .Query()
        //                .SelectRaw(select)
        //                .From("pois")
        //                .PoiWhereExpression(
        //                    idlist: mypoihelper.idlist,
        //                    poitypelist: mypoihelper.poitypelist,
        //                    subtypelist: mypoihelper.subtypelist,
        //                    smgtaglist: mypoihelper.smgtaglist,
        //                    districtlist: new List<string>(),
        //                    municipalitylist: new List<string>(),
        //                    tourismvereinlist: mypoihelper.tourismvereinlist,
        //                    regionlist: mypoihelper.regionlist,
        //                    arealist: mypoihelper.arealist,
        //                    highlight: mypoihelper.highlight,
        //                    activefilter: mypoihelper.active,
        //                    smgactivefilter: mypoihelper.smgactive,
        //                    searchfilter: searchfilter,
        //                    language: language,
        //                    lastchange: null,
        //                    languagelist: new List<string>(),
        //                    additionalfilter: additionalfilter,
        //                    userroles: UserRolesToFilterEndpoint("Poi")
        //                )
        //                .ApplyRawFilter(rawfilter)
        //                .ApplyOrdering_GeneratedColumns(geosearchresult, rawsort);

        //        var compiled = query.Compiler.Compile(query);

        //        var reader = await query.Connection.ExecuteReaderAsync(
        //            compiled.Sql,
        //            compiled.NamedBindings
        //        );

        //        return reader.ReadAndParseTOJson();
        //    });
        //}

        //[Obsolete("Deprecated, use the Poi Endpoint")]
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[HttpGet, Route("PoiChanged")]
        //public RedirectToActionResult GetAllPoisChanged(
        //    uint pagenumber = 1,
        //    uint pagesize = 10,
        //    string? seed = null,
        //    string? updatefrom = null,
        //    CancellationToken cancellationToken = default
        //)
        //{
        //    updatefrom ??= String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));

        //    return RedirectToAction(
        //        "Poi",
        //        "v1",
        //        new RouteValueDictionary
        //        {
        //            { "pagenumber", pagenumber },
        //            { "pagesize", pagesize },
        //            { "seed", seed },
        //            { "updatefrom", updatefrom },
        //        }
        //    );
        //}

        #endregion

        #region ActivityController

        ///// <summary>
        ///// GET Activity List Reduced
        ///// </summary>
        ///// <param name="language">Localization Language, (default:'en')</param>
        ///// <param name="activitytype">Type of the Activity ('null' = Filter disabled, possible values: BITMASK: 'Mountains = 1','Cycling = 2','Local tours = 4','Horses = 8','Hiking = 16','Running and fitness = 32','Cross-country ski-track = 64','Tobbogan run = 128','Slopes = 256','Lifts = 512'), (default:'1023' == ALL), REFERENCE TO: GET /api/ActivityTypes </param>
        ///// <param name="subtype">Subtype of the Activity (BITMASK Filter = available SubTypes depends on the selected Activity Type), (default:'null')</param>
        ///// <param name="locfilter">Locfilter (Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction)), (default:'null')</param>
        ///// <param name="areafilter">AreaFilter (Separator ',' IDList of AreaIDs separated by ','), (default:'null')</param>
        ///// <param name="distancefilter">Distance Range Filter (Separator ',' example Value: 15,40 Distance from 15 up to 40 Km), (default:'null')</param>
        ///// <param name="altitudefilter">Altitude Range Filter (Separator ',' example Value: 500,1000 Altitude from 500 up to 1000 metres), (default:'null')</param>
        ///// <param name="durationfilter">Duration Range Filter (Separator ',' example Value: 1,3 Duration from 1 to 3 hours), (default:'null')</param>
        ///// <param name="highlight">Hightlight Filter (possible values: 'false' = only Activities with Highlight false, 'true' = only Activities with Highlight true), (default:'null')</param>
        ///// <param name="difficultyfilter">Difficulty Filter (possible values: '1' = easy, '2' = medium, '3' = difficult), (default:'null')</param>
        ///// <param name="odhtagfilter">Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'api/SmgTag/ByMainEntity/Activity'), (default:'null')</param>
        ///// <param name="active">Active Activities Filter (possible Values: 'true' only Active Activities, 'false' only Disabled Activities</param>
        ///// <param name="odhactive"> odhactive (Published) Activities Filter (possible Values: 'true' only published Activities, 'false' only not published Activities, (default:'null')</param>
        ///// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        ///// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        ///// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        ///// <returns>Collection of Activity Reduced Objects</returns>
        //[ProducesResponseType(typeof(IEnumerable<ActivityPoiReduced>), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        ////[Authorize(Roles = "DataReader,ActivityReader")]
        //[HttpGet, Route("ActivityReduced")]
        //public async Task<IActionResult> GetActivityReduced(
        //    string? language = "en",
        //    string? activitytype = "1023",
        //    string? subtype = null,
        //    string? locfilter = null,
        //    string? areafilter = null,
        //    string? distancefilter = null,
        //    string? altitudefilter = null,
        //    string? durationfilter = null,
        //    LegacyBool highlight = null!,
        //    string? difficultyfilter = null,
        //    string? odhtagfilter = null,
        //    LegacyBool active = null!,
        //    LegacyBool odhactive = null!,
        //    string? latitude = null,
        //    string? longitude = null,
        //    string? radius = null,
        //    [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
        //    string? searchfilter = null,
        //    string? rawfilter = null,
        //    string? rawsort = null,
        //    CancellationToken cancellationToken = default
        //)
        //{
        //    var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
        //        latitude,
        //        longitude,
        //        radius
        //    );

        //    return await GetActivityReduced(
        //        language?.ToLower(),
        //        activitytype,
        //        subtype,
        //        locfilter,
        //        areafilter,
        //        distancefilter,
        //        altitudefilter,
        //        durationfilter,
        //        highlight,
        //        difficultyfilter,
        //        active,
        //        odhactive,
        //        odhtagfilter,
        //        fields: fields ?? Array.Empty<string>(),
        //        rawfilter,
        //        rawsort,
        //        searchfilter,
        //        geosearchresult,
        //        cancellationToken
        //    );
        //}

        //private Task<IActionResult> GetActivityReduced(
        //    string? language,
        //    string? activitytype,
        //    string? subtypefilter,
        //    string? locfilter,
        //    string? areafilter,
        //    string? distancefilter,
        //    string? altitudefilter,
        //    string? durationfilter,
        //    bool? highlightfilter,
        //    string? difficultyfilter,
        //    bool? active,
        //    bool? smgactive,
        //    string? smgtags,
        //    string[] fields,
        //    string? rawfilter,
        //    string? rawsort,
        //    string? searchfilter,
        //    PGGeoSearchResult geosearchresult,
        //    CancellationToken cancellationToken
        //)
        //{
        //    return DoAsyncReturn(async () =>
        //    {
        //        AdditionalFiltersToAddEndpoint("Activity")
        //            .TryGetValue("Read", out var additionalfilter);

        //        ActivityHelper myactivityhelper = await ActivityHelper.CreateAsync(
        //            QueryFactory,
        //            activitytype: activitytype,
        //            subtypefilter: subtypefilter,
        //            idfilter: null,
        //            locfilter: locfilter,
        //            areafilter: areafilter,
        //            distancefilter: distancefilter,
        //            altitudefilter: altitudefilter,
        //            durationfilter: durationfilter,
        //            highlightfilter: highlightfilter,
        //            difficultyfilter: difficultyfilter,
        //            activefilter: active,
        //            smgactivefilter: smgactive,
        //            smgtags: smgtags,
        //            lastchange: null,
        //            langfilter: language,
        //            cancellationToken: cancellationToken
        //        );

        //        string select =
        //            $"data#>>'\\{{Id\\}}' as \"Id\", data#>>'\\{{Detail,{language},Title\\}}' as \"Name\"";

        //        //Custom Fields filter
        //        if (fields.Length > 0)
        //            select += string.Join(
        //                "",
        //                fields
        //                    .Where(x => x != "Id")
        //                    .Select(field =>
        //                        $", data#>'\\{{{field.Replace(".", ",")}\\}}' as \"{field}\""
        //                    )
        //            );

        //        var query = (XQuery)
        //            QueryFactory
        //                .Query()
        //                .SelectRaw(select)
        //                .From("activities")
        //                .ActivityWhereExpression(
        //                    idlist: myactivityhelper.idlist,
        //                    activitytypelist: myactivityhelper.activitytypelist,
        //                    subtypelist: myactivityhelper.subtypelist,
        //                    difficultylist: myactivityhelper.difficultylist,
        //                    smgtaglist: myactivityhelper.smgtaglist,
        //                    districtlist: new List<string>(),
        //                    municipalitylist: new List<string>(),
        //                    tourismvereinlist: myactivityhelper.tourismvereinlist,
        //                    regionlist: myactivityhelper.regionlist,
        //                    arealist: myactivityhelper.arealist,
        //                    distance: myactivityhelper.distance,
        //                    distancemin: myactivityhelper.distancemin,
        //                    distancemax: myactivityhelper.distancemax,
        //                    duration: myactivityhelper.duration,
        //                    durationmin: myactivityhelper.durationmin,
        //                    durationmax: myactivityhelper.durationmax,
        //                    altitude: myactivityhelper.altitude,
        //                    altitudemin: myactivityhelper.altitudemin,
        //                    altitudemax: myactivityhelper.altitudemax,
        //                    highlight: myactivityhelper.highlight,
        //                    activefilter: myactivityhelper.active,
        //                    smgactivefilter: myactivityhelper.smgactive,
        //                    searchfilter: null,
        //                    language: language,
        //                    lastchange: null,
        //                    languagelist: new List<string>(),
        //                    additionalfilter: additionalfilter,
        //                    userroles: UserRolesToFilterEndpoint("Activity")
        //                )
        //                .ApplyRawFilter(rawfilter)
        //                .ApplyOrdering_GeneratedColumns(geosearchresult, rawsort);

        //        var compiled = query.Compiler.Compile(query);

        //        var reader = await query.Connection.ExecuteReaderAsync(
        //            compiled.Sql,
        //            compiled.NamedBindings
        //        );

        //        return reader.ReadAndParseTOJson();
        //    });
        //}

        //[Obsolete("Deprecated, use the Activity Endpoint")]
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[HttpGet, Route("ActivityChanged")]
        //public RedirectToActionResult GetAllActivityChanged(
        //    uint pagenumber = 1,
        //    uint pagesize = 10,
        //    string? seed = null,
        //    string? updatefrom = null
        //)
        //{
        //    updatefrom ??= String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));

        //    return RedirectToAction(
        //        "Activity",
        //        "v1",
        //        new RouteValueDictionary
        //        {
        //            { "pagenumber", pagenumber },
        //            { "pagesize", pagesize },
        //            { "seed", seed },
        //            { "updatefrom", updatefrom },
        //        }
        //    );
        //}

        #endregion

        #region GastronomyController

        ///// <summary>
        ///// GET Gastronomy Reduced List
        ///// </summary>
        ///// <param name="language">Localization Language, (default:'en')</param>
        ///// <param name="locfilter">Locfilter (Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction)), (default:'null')</param>
        ///// <param name="dishcodefilter">Dish Code Filter (BITMASK values: 1 = (Speisen), 2 = (Vorspeise), 4 = (Hauptspeise), 8 = (Nachspeise), 16 = (Tagesgericht), 32 = (Menü), 64 = (Degustationsmenü), 128 = (Kindermenüs), 256 = (Mittagsmenüs)</param>
        ///// <param name="ceremonycodefilter">Ceremony Code Filter (BITMASK  values: 1 = (Familienfeiern), 2 = (Hochzeiten), 4 = (Geburtstagsfeiern), 8 = (Firmenessen), 16 = (Weihnachtsessen), 32 = (Sylvestermenü), 64 = (Seminare / Tagungen), 128 = (Versammlungen)</param>
        ///// <param name="categorycodefilter">Category Code Filter (BITMASK  values: 1 = (Restaurant), 2 = (Bar / Café / Bistro), 4 = (Pub / Disco), 8 = (Apres Ski), 16 = (Jausenstation), 32 = (Pizzeria), 64 = (Bäuerlicher Schankbetrieb), 128 = (Buschenschank), 256 = (Hofschank), 512 = (Törggele Lokale), 1024 = (Schnellimbiss), 2048 = (Mensa), 4096 = (Vinothek /Weinhaus / Taverne), 8192 = (Eisdiele), 16348 = (Gasthaus), 32768 = (Gasthof), 65536 = (Braugarten), 131072 = (Schutzhütte), 262144 = (Alm), 524288 = (Skihütte)</param>
        ///// <param name="facilitycodefilter">Facility Code Filter (BITMASK  values: 1 = (American Express), 2 = (Diners Club), 4 = (Eurocard / Mastercard), 8 = (Visa), 16 = (Hunde erlaubt), 32 = (Geeignet für Busse), 64 = (Garten), 128 = (Garagen), 256 = (Bierbar), 512 = (Kinderspielplatz), 1024 = (Spielzimmer), 2048 = (Spielplatz), 4096 = (Parkplätze), 8192 = (Raucherräume), 16348 = (Terrasse), 32768 = (Behindertengerecht), 65536 = (Biergarten), 131072 = (Aussichtsterrasse), 262144 = (Wintergarten), 524288 = (Gault Millau Südtirol), 1048576 = (Guida Espresso), 2097152 = (Gambero Rosso), 4194304 = (Feinschmecker), 8388608 = (Aral Schlemmer Atlas), 16777216 = (Varta Führer), 33554432 = (Bertelsmann), 67108864 = (Preis für Südtiroler Weinkultur), 134217728 = (Michelin), 268435456 = (Roter Hahn), 536870912 = (Tafelspitz))</param>
        ///// <param name="cuisinecodefilter">Cuisine Code Filter (BITMASK  values: 1 = (Vegetarische Küche), 2 = (Glutenfreie Küche), 4 = (Laktosefreie Kost), 8 = (Warme Küche), 16 = (Südtiroler Spezialitäten), 32 = (Gourmet Küche), 64 = (Italienische Küche), 128 = (Internationale Küche), 256 = (Pizza), 512 = (Fischspezialitäten), 1024 = (Asiatische Küche), 2048 = (Wildspezialitäten), 4096 = (Produkte eigener Erzeugung), 8192 = (Diätküche), 16348 = (Grillspezialitäten), 32768 = (Ladinische Küche), 65536 = (Kleine Karte), 131072 = (Fischwochen), 262144 = (Spargelwochen), 524288 = (Lammwochen), 1048576 = (Wildwochen), 2097152 = (Vorspeisewochen), 4194304 = (Nudelwochen), 8388608 = (Kräuterwochen), 16777216 = (Kindermenüs), 33554432 = (Mittagsmenüs))</param>
        ///// <param name="odhtagfilter">ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=gastronomy'), (default:'null')</param>
        ///// <param name="active">Active Gastronomies Filter (possible Values: 'true' only Active Gastronomies, 'false' only Disabled Gastronomies</param>
        ///// <param name="odhactive">ODH Active (Published) Gastronomies Filter (Refers to field SmgActive) Gastronomies Filter (possible Values: 'true' only published Gastronomies, 'false' only not published Gastronomies, (default:'null')</param>
        ///// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        ///// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        ///// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        ///// <returns>Collection of GastronomyReduced Objects</returns>
        //[ProducesResponseType(typeof(IEnumerable<GastronomyReduced>), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[HttpGet, Route("GastronomyReduced")]
        //public async Task<IActionResult> GetGastronomyListReduced(
        //    string? language = "en",
        //    string? locfilter = null,
        //    string? dishcodefilter = null,
        //    string? ceremonycodefilter = null,
        //    string? categorycodefilter = null,
        //    string? facilitycodefilter = null,
        //    string? cuisinecodefilter = null,
        //    string? odhtagfilter = null,
        //    LegacyBool active = null!,
        //    LegacyBool odhactive = null!,
        //    string? latitude = null,
        //    string? longitude = null,
        //    string? radius = null,
        //    [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
        //    string? searchfilter = null,
        //    string? rawfilter = null,
        //    string? rawsort = null,
        //    CancellationToken cancellationToken = default
        //)
        //{
        //    var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
        //        latitude,
        //        longitude,
        //        radius
        //    );

        //    return await GetGastronomyReduced(
        //        language?.ToLower(),
        //        locfilter,
        //        dishcodefilter,
        //        ceremonycodefilter,
        //        categorycodefilter,
        //        facilitycodefilter,
        //        cuisinecodefilter,
        //        active?.Value,
        //        odhactive?.Value,
        //        odhtagfilter,
        //        fields: fields ?? Array.Empty<string>(),
        //        rawfilter,
        //        rawsort,
        //        searchfilter,
        //        geosearchresult,
        //        cancellationToken
        //    );
        //}

        //private Task<IActionResult> GetGastronomyReduced(
        //    string? language,
        //    string? locfilter,
        //    string? dishcodefilter,
        //    string? ceremonycodefilter,
        //    string? categorycodefilter,
        //    string? facilitycodefilter,
        //    string? cuisinecodefilter,
        //    bool? active,
        //    bool? smgactive,
        //    string? smgtagfilter,
        //    string[] fields,
        //    string? rawfilter,
        //    string? rawsort,
        //    string? searchfilter,
        //    PGGeoSearchResult geosearchresult,
        //    CancellationToken cancellationToken
        //)
        //{
        //    return DoAsyncReturn(async () =>
        //    {
        //        AdditionalFiltersToAddEndpoint("Gastronomy")
        //            .TryGetValue("Read", out var additionalfilter);

        //        GastronomyHelper mygastronomyhelper = await GastronomyHelper.CreateAsync(
        //            QueryFactory,
        //            idfilter: null,
        //            locfilter: locfilter,
        //            categorycodefilter: categorycodefilter,
        //            dishcodefilter: dishcodefilter,
        //            ceremonycodefilter: ceremonycodefilter,
        //            facilitycodefilter: facilitycodefilter,
        //            cuisinecodefilter: cuisinecodefilter,
        //            activefilter: active,
        //            smgactivefilter: smgactive,
        //            smgtags: smgtagfilter,
        //            lastchange: null,
        //            langfilter: language,
        //            cancellationToken
        //        );

        //        string select =
        //            $"data#>>'\\{{Id\\}}' as \"Id\", data#>>'\\{{Detail,{language},Title\\}}' as \"Name\"";

        //        //Custom Fields filter
        //        if (fields.Length > 0)
        //            select += string.Join(
        //                "",
        //                fields
        //                    .Where(x => x != "Id")
        //                    .Select(field =>
        //                        $", data#>'\\{{{field.Replace(".", ",")}\\}}' as \"{field}\""
        //                    )
        //            );

        //        var query = (XQuery)
        //            QueryFactory
        //                .Query()
        //                .SelectRaw(select)
        //                .From("gastronomies")
        //                .GastronomyWhereExpression(
        //                    idlist: mygastronomyhelper.idlist,
        //                    dishcodeslist: mygastronomyhelper.dishcodesids,
        //                    ceremonycodeslist: mygastronomyhelper.ceremonycodesids,
        //                    categorycodeslist: mygastronomyhelper.categorycodesids,
        //                    facilitycodeslist: mygastronomyhelper.facilitycodesids,
        //                    smgtaglist: mygastronomyhelper.smgtaglist,
        //                    districtlist: mygastronomyhelper.districtlist,
        //                    municipalitylist: mygastronomyhelper.municipalitylist,
        //                    tourismvereinlist: mygastronomyhelper.tourismvereinlist,
        //                    regionlist: mygastronomyhelper.regionlist,
        //                    activefilter: mygastronomyhelper.active,
        //                    smgactivefilter: mygastronomyhelper.smgactive,
        //                    searchfilter: searchfilter,
        //                    language: language,
        //                    lastchange: null,
        //                    languagelist: new List<string>(),
        //                    additionalfilter: additionalfilter,
        //                    userroles: UserRolesToFilterEndpoint("Gastronomy")
        //                )
        //                .ApplyRawFilter(rawfilter)
        //                .ApplyOrdering_GeneratedColumns(geosearchresult, rawsort);

        //        var compiled = query.Compiler.Compile(query);

        //        var reader = await query.Connection.ExecuteReaderAsync(
        //            compiled.Sql,
        //            compiled.NamedBindings
        //        );

        //        return reader.ReadAndParseTOJson();
        //    });
        //}

        //[Obsolete("Deprecated, use the Gastronomy Endpoint")]
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[HttpGet, Route("GastronomyChanged")]
        //public RedirectToActionResult GetAllGastronomyChanged(
        //    int pagenumber = 1,
        //    int pagesize = 10,
        //    string? seed = null,
        //    string? updatefrom = null
        //)
        //{
        //    updatefrom ??= String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));

        //    return RedirectToAction(
        //        "Gastronomy",
        //        "v1",
        //        new RouteValueDictionary
        //        {
        //            { "pagenumber", pagenumber },
        //            { "pagesize", pagesize },
        //            { "seed", seed },
        //            { "updatefrom", updatefrom },
        //        }
        //    );
        //}

        #endregion

        #region ODHTagController

        /// <summary>
        /// GET ODHTag List REDUCED
        /// </summary>
        /// <param name="validforentity"></param>
        /// <param name="localizationlanguage"></param>
        /// <returns></returns>
        //[SwaggerResponse(HttpStatusCode.OK, "Array of SmgTagReduced Objects", typeof(IEnumerable<SmgTagReduced>))]
        [HttpGet, Route("ODHTagReduced")]
        //[Authorize(Roles = "DataReader,CommonReader,AccoReader,ActivityReader,PoiReader,ODHPoiReader,PackageReader,GastroReader,EventReader,ArticleReader")]
        public async Task<IActionResult> GetODHTagsReduced(
            string? language = "en",
            string? localizationlanguage = null,
            string? validforentity = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            CancellationToken cancellationToken = default
        )
        {
            //Compatibility
            if (!String.IsNullOrEmpty(localizationlanguage))
                language = localizationlanguage;

            return await GetODHTagReduced(
                language?.ToLower(),
                validforentity,
                searchfilter,
                fields: fields ?? Array.Empty<string>(),
                rawfilter,
                rawsort,
                cancellationToken
            );
        }

        private Task<IActionResult> GetODHTagReduced(
            string? language,
            string? validforentity,
            string? searchfilter,
            string[] fields,
            string? rawfilter,
            string? rawsort,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                AdditionalFiltersToAddEndpoint("ODHTag")
                    .TryGetValue("Read", out var additionalfilter);

                string select =
                    $"data#>>'\\{{Id\\}}' as \"Id\", data#>>'\\{{TagName,{language}\\}}' as \"Name\"";

                //Hack
                if (validforentity == "odhactivitypoi")
                {
                    validforentity.Replace("odhactivitypoi", "smgpoi");
                }

                var validforentitytypeslist = (validforentity ?? "").Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries
                );
                //var maintypeslist = (validforentity ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);

                //Custom Fields filter
                if (fields.Length > 0)
                    select += string.Join(
                        "",
                        fields
                            .Where(x => x != "Id")
                            .Select(field =>
                                $", data#>'\\{{{field.Replace(".", ",")}\\}}' as \"{field}\""
                            )
                    );

                var query = (XQuery)
                    QueryFactory
                        .Query()
                        .SelectRaw(select)
                        .From("smgtags")
                        .ODHTagWhereExpression(
                            languagelist: new List<string>(),
                            mainentitylist: new List<string>(),
                            validforentitylist: validforentitytypeslist,
                            publishedonlist: new List<string>(),
                            sourcelist: new List<string>(),
                            displayascategory: null,
                            searchfilter: searchfilter,
                            language: language,
                            additionalfilter: additionalfilter,
                            userroles: UserRolesToFilterEndpoint("ODHTag")
                        )
                        .ApplyRawFilter(rawfilter)
                        .ApplyOrdering(
                            new PGGeoSearchResult() { geosearch = false },
                            rawsort,
                            "data #>>'\\{MainEntity\\}', data#>>'\\{Shortname\\}'"
                        );

                var compiled = query.Compiler.Compile(query);

                var reader = await query.Connection.ExecuteReaderAsync(
                    compiled.Sql,
                    compiled.NamedBindings
                );

                return reader.ReadAndParseTOJson();
            });
        }

        #endregion

        #region ODHActivityPoiController

        ////Reduced GETTER

        /// <summary>
        /// GET ODHActivityPoi List Reduced
        /// </summary>
        /// <param name="language">Localization Language, (default:'en')</param>
        /// <param name="type">Type of the ODHActivityPoi ('null' = Filter disabled, possible values: BITMASK: 1 = Wellness, 2 = Winter, 4 = Summer, 8 = Culture, 16 = Other, 32 = Gastronomy), (default: 63 == ALL), REFERENCE TO: GET /api/ODHActivityPoiTypes </param>
        /// <param name="subtype">Subtype of the ODHActivityPoi ('null' = Filter disabled, BITMASK Filter, available SubTypes depends on the selected Maintype reference to ODHActivityPoiTypes)</param>
        /// <param name="level3type">Additional Type of the ODHActivityPoi ('null' = Filter disabled, BITMASK Filter, available SubTypes depends on the selected Maintype, SubType reference to ODHActivityPoiTypes)</param>
        /// <param name="locfilter">Locfilter (Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMASSOCIATIONID = (Filter by Tourismassociation), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = No Filter), (default:'null')</param>
        /// <param name="areafilter">AreaFilter (Alternate Locfilter, can be combined with locfilter) (Separator ',' possible values: reg + REGIONID = (Filter by Region), tvs + TOURISMASSOCIATIONID = (Filter by Tourismassociation), skr + SKIREGIONID = (Filter by Skiregion), ska + SKIAREAID = (Filter by Skiarea), are + AREAID = (Filter by LTS Area), 'null' = No Filter), (default:'null')</param>
        /// <param name="highlight">Hightlight Filter (possible values: 'false' = only ODHActivityPoi with Highlight false, 'true' = only ODHActivityPoi with Highlight true), (default:'null')</param>
        /// <param name="source">Source Filter (possible Values: 'null' Displays all ODHActivityPoi, 'None', 'ActivityData', 'PoiData', 'GastronomicData', 'MuseumData', 'Magnolia', 'Content', 'SuedtirolWein', 'Archapp' (default:'null')</param>
        /// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <param name="odhtagfilter">ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=smgpoi'), (default:'null')</param>
        /// <param name="active">Active ODHActivityPoi Filter (possible Values: 'true' only active ODHActivityPoi, 'false' only not active ODHActivityPoi, (default:'null')</param>
        /// <param name="odhactive">ODH Active (Published) ODHActivityPoi Filter (Refers to field SmgActive) (possible Values: 'true' only published ODHActivityPoi, 'false' only not published ODHActivityPoi, (default:'null')</param>
        /// <returns>Collection of ActivityPoiReduced Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<ActivityPoiReduced>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("ODHActivityPoiReduced")]
        public async Task<IActionResult> GetODHActivityPoiListReduced(
            string? language = "en",
            string? type = "255",
            string? subtype = null,
            string? level3type = null,
            string? locfilter = null,
            //string langfilter = null,
            string? areafilter = null,
            LegacyBool highlight = null!,
            string? source = null,
            string? odhtagfilter = null,
            LegacyBool odhactive = null!,
            LegacyBool active = null!,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );

            return await GetODHActivityPoiReduced(
                language,
                type,
                subtype,
                level3type,
                locfilter,
                areafilter,
                highlight?.Value,
                active?.Value,
                odhactive?.Value,
                source,
                odhtagfilter,
                fields: fields ?? Array.Empty<string>(),
                rawfilter,
                rawsort,
                searchfilter,
                geosearchresult,
                cancellationToken
            );
        }

        /// <summary>
        /// GET Reduced ODHActivity Poi List
        /// </summary>
        /// <param name="language">Localization Language</param>
        /// <param name="poitype">Type of the Poi (possible values: STRINGS: 'Ärtze, Apotheken','Geschäfte und Dienstleister','Kultur und Sehenswürdigkeiten','Nachtleben und Unterhaltung','Öffentliche Einrichtungen','Sport und Freizeit','Verkehr und Transport' : BITMASK also possible: 'Ärtze, Apotheken = 1','Geschäfte und Dienstleister = 2','Kultur und Sehenswürdigkeiten = 4','Nachtleben und Unterhaltung = 8','Öffentliche Einrichtungen = 16','Sport und Freizeit = 32','Verkehr und Transport = 64')</param>
        /// <param name="subtypefilter">Subtype of the Poi ('null' = Filter disabled, available Subtypes depends on the activitytype BITMASK)</param>
        /// <param name="locfilter">Locfilter (Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), 'null' = No Filter)</param>
        /// <param name="areafilter">AreaFilter (Separator ',' IDList of AreaIDs separated by ',', 'null' : Filter disabled)</param>
        /// <param name="highlightfilter">Highlight Filter (Show only Highlights possible values: 'true' : show only Highlight Pois, 'null' Filter disabled)</param>
        /// <param name="active">Active Filter (possible Values: 'null' Displays all Pois, 'true' only Active Pois, 'false' only Disabled Pois</param>
        /// <param name="smgactive">SMGActive Filter (possible Values: 'null' Displays all Pois, 'true' only SMG Active Pois, 'false' only SMG Disabled Pois</param>
        /// <param name="smgtags">SMGTag Filter (String, Separator ',' more SMGTags possible, 'null' = No Filter)</param>
        /// <returns>Collection of Reduced Poi Objects</returns>
        private Task<IActionResult> GetODHActivityPoiReduced(
            string? language,
            string? type,
            string? subtype,
            string? level3type,
            string? locfilter,
            string? areafilter,
            bool? highlightfilter,
            bool? active,
            bool? smgactive,
            string? source,
            string? smgtags,
            string[] fields,
            string? rawfilter,
            string? rawsort,
            string? searchfilter,
            PGGeoSearchResult geosearchresult,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                AdditionalFiltersToAddEndpoint("Poi")
                    .TryGetValue("ODHActivityPoi", out var additionalfilter);

                ODHActivityPoiHelper helper = await ODHActivityPoiHelper.CreateAsync(
                    queryFactory: QueryFactory,
                    typefilter: type,
                    subtypefilter: subtype,
                    level3typefilter: level3type,
                    idfilter: null,
                    locfilter: locfilter,
                    areafilter: areafilter,
                    languagefilter: language,
                    sourcefilter: source,
                    highlightfilter: highlightfilter,
                    activefilter: active,
                    smgactivefilter: smgactive,
                    smgtags: smgtags,
                    smgtagsand: null,
                    lastchange: null,
                    categorycodefilter: null,
                    dishcodefilter: null,
                    ceremonycodefilter: null,
                    facilitycodefilter: null,
                    cuisinecodefilter: null,
                    activitytypefilter: null,
                    poitypefilter: null,
                    distancefilter: null,
                    altitudefilter: null,
                    durationfilter: null,
                    difficultyfilter: null,
                    null,
                    null,
                    publishedonfilter: null,
                    cancellationToken
                );

                string select =
                    $"data#>>'\\{{Id\\}}' as \"Id\", data#>>'\\{{Detail,{language},Title\\}}' as \"Name\"";
                //string orderby = "data#>>'\\{Shortname\\}' ASC";

                //Custom Fields filter Removes a twice selected Id Field
                if (fields.Length > 0)
                    select += string.Join(
                        "",
                        fields
                            .Where(x => x != "Id")
                            .Select(field =>
                                $", data#>'\\{{{field.Replace(".", ",")}\\}}' as \"{field}\""
                            )
                    );

                var query = (XQuery)
                    QueryFactory
                        .Query()
                        .SelectRaw(select)
                        .From("smgpois")
                        .ODHActivityPoiWhereExpression(
                            idlist: helper.idlist,
                            typelist: helper.typelist,
                            subtypelist: helper.subtypelist,
                            level3typelist: helper.level3typelist,
                            smgtaglist: helper.smgtaglist,
                            smgtaglistand: helper.smgtaglistand,
                            districtlist: helper.districtlist,
                            municipalitylist: helper.municipalitylist,
                            tourismvereinlist: helper.tourismvereinlist,
                            regionlist: helper.regionlist,
                            arealist: helper.arealist,
                            highlight: helper.highlight,
                            activefilter: helper.active,
                            smgactivefilter: helper.smgactive,
                            sourcelist: helper.sourcelist,
                            languagelist: helper.languagelist,
                            categorycodeslist: helper.categorycodesids,
                            dishcodeslist: helper.dishcodesids,
                            ceremonycodeslist: helper.ceremonycodesids,
                            facilitycodeslist: helper.facilitycodesids,
                            activitytypelist: helper.activitytypelist,
                            poitypelist: helper.poitypelist,
                            difficultylist: helper.difficultylist,
                            distance: helper.distance,
                            distancemin: helper.distancemin,
                            distancemax: helper.distancemax,
                            duration: helper.duration,
                            durationmin: helper.durationmin,
                            durationmax: helper.durationmax,
                            altitude: helper.altitude,
                            altitudemin: helper.altitudemin,
                            altitudemax: helper.altitudemax,
                            hasimage: helper.hasimage,
                            tagdict: helper.tagdict,
                            publishedonlist: helper.publishedonlist,
                            searchfilter: searchfilter,
                            language: language,
                            lastchange: null,
                            additionalfilter: additionalfilter,
                            userroles: UserRolesToFilterEndpoint("ODHActivityPoi")
                        )
                        .ApplyRawFilter(rawfilter)
                        .ApplyOrdering_GeneratedColumns(geosearchresult, rawsort);

                var compiled = query.Compiler.Compile(query);

                var reader = await query.Connection.ExecuteReaderAsync(
                    compiled.Sql,
                    compiled.NamedBindings
                );

                return reader.ReadAndParseTOJson();
            });
        }

        [Obsolete("Deprecated, use the ODHActivityPoi Endpoint")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet, Route("ODHActivityPoiChanged")]
        public RedirectToActionResult GetAllODHActivityPoiChanged(
            uint pagenumber = 1,
            uint pagesize = 10,
            string? seed = null,
            string? updatefrom = null,
            CancellationToken cancellationToken = default
        )
        {
            updatefrom ??= String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));

            return RedirectToAction(
                "ODHActivityPoi",
                "v1",
                new RouteValueDictionary
                {
                    { "pagenumber", pagenumber },
                    { "pagesize", pagesize },
                    { "seed", seed },
                    { "updatefrom", updatefrom },
                }
            );
        }

        #endregion

        #region EventController

        /// <summary>
        /// GET Event List Reduced
        /// </summary>
        /// <param name="language">Localization Language, (default:'en')</param>
        /// <param name="locfilter">Locfilter (Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = No Filter), (default:'null')</param>
        /// <param name="rancfilter">Rancfilter (Ranc 0-5 possible)</param>
        /// <param name="typefilter">Typefilter (Type of Event: not used yet)</param>
        /// <param name="topicfilter">Topic ID Filter (Filter by Topic ID) BITMASK</param>
        /// <param name="orgfilter">Organization Filter (Filter by Organizer RID)</param>
        /// <param name="odhtagfilter">ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=event'), (default:'null')</param>
        /// <param name="active">Active Events Filter (possible Values: 'true' only Active Events, 'false' only Disabled Events, (default:'null')</param>
        /// <param name="odhactive">ODH Active (Published) Events Filter (Refers to field SmgActive) Events Filter (possible Values: 'true' only published Events, 'false' only not published Events, (default:'null')</param>
        /// <param name="begindate">BeginDate of Events (Format: yyyy-MM-dd)</param>
        /// <param name="enddate">EndDate of Events (Format: yyyy-MM-dd)</param>
        /// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <returns>Collection of EventReduced Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<EventReduced>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("EventReduced")]
        public async Task<IActionResult> GetEventsReduced(
            string? language = "en",
            string? locfilter = null,
            string? rancfilter = null,
            string? typefilter = null,
            string? topicfilter = null,
            string? orgfilter = null,
            string? odhtagfilter = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? begindate = null,
            string? enddate = null,
            string? langfilter = null,
            string? source = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );

            return await GetEventReduced(
                language,
                locfilter,
                rancfilter,
                typefilter,
                topicfilter,
                orgfilter,
                odhactive?.Value,
                active?.Value,
                source,
                langfilter,
                begindate,
                enddate,
                odhtagfilter,
                fields: fields ?? Array.Empty<string>(),
                rawfilter,
                rawsort,
                searchfilter,
                geosearchresult,
                cancellationToken
            );
        }

        private Task<IActionResult> GetEventReduced(
            string? language,
            string? locfilter,
            string? rancfilter,
            string? typefilter,
            string? topicfilter,
            string? orgfilter,
            bool? smgactive,
            bool? active,
            string? source,
            string? langfilter,
            string? begindate,
            string? enddate,
            string? smgtagfilter,
            string[] fields,
            string? rawfilter,
            string? rawsort,
            string? searchfilter,
            PGGeoSearchResult geosearchresult,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                AdditionalFiltersToAddEndpoint("Event")
                    .TryGetValue("Read", out var additionalfilter);

                EventHelper helper = await EventHelper.CreateAsync(
                    QueryFactory,
                    null,
                    locfilter,
                    rancfilter,
                    topicfilter,
                    orgfilter,
                    begindate,
                    enddate,
                    active,
                    smgactive,
                    smgtagfilter,
                    null,
                    null,
                    langfilter,
                    source,
                    null,
                    cancellationToken
                );

                string select =
                    $"data#>>'\\{{Id\\}}' as \"Id\", data#>>'\\{{Detail,{language},Title\\}}' as \"Name\"";
                //string orderby = "data#>>'\\{Shortname\\}' ASC";

                //Custom Fields filter
                if (fields.Length > 0)
                    select += string.Join(
                        "",
                        fields
                            .Where(x => x != "Id")
                            .Select(field =>
                                $", data#>'\\{{{field.Replace(".", ",")}\\}}' as \"{field}\""
                            )
                    );

                var query = (XQuery)
                    QueryFactory
                        .Query()
                        .SelectRaw(select)
                        .From("events")
                        .EventWhereExpression(
                            idlist: helper.idlist,
                            topiclist: helper.topicrids,
                            ranclist: helper.rancidlist,
                            smgtaglist: helper.smgtaglist,
                            tagdict: helper.tagdict,
                            districtlist: helper.districtlist,
                            municipalitylist: helper.municipalitylist,
                            tourismvereinlist: helper.tourismvereinlist,
                            regionlist: helper.regionlist,
                            orglist: helper.orgidlist,
                            sourcelist: helper.sourcelist,
                            begindate: helper.begin,
                            enddate: helper.end,
                            activefilter: helper.active,
                            smgactivefilter: helper.smgactive,
                            languagelist: helper.languagelist,
                            publishedonlist: helper.publishedonlist,
                            searchfilter: searchfilter,
                            language: language,
                            lastchange: null,
                            additionalfilter: additionalfilter,
                            userroles: UserRolesToFilterEndpoint("Event")
                        )
                        .ApplyRawFilter(rawfilter)
                        .ApplyOrdering_GeneratedColumns(geosearchresult, rawsort);

                var compiled = query.Compiler.Compile(query);

                var reader = await query.Connection.ExecuteReaderAsync(
                    compiled.Sql,
                    compiled.NamedBindings
                );

                return reader.ReadAndParseTOJson();
            });
        }

        [Obsolete("Deprecated, use the Event Endpoint")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet, Route("EventChanged")]
        public RedirectToActionResult GetAllEventChanged(
            uint pagenumber = 1,
            uint pagesize = 10,
            string? seed = null,
            string? updatefrom = null,
            CancellationToken cancellationToken = default
        )
        {
            updatefrom ??= String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));

            return RedirectToAction(
                "Event",
                "v1",
                new RouteValueDictionary
                {
                    { "pagenumber", pagenumber },
                    { "pagesize", pagesize },
                    { "seed", seed },
                    { "updatefrom", updatefrom },
                }
            );
        }

        #endregion

        #region ArticleController

        /// <summary>
        /// GET Article List Reduced
        /// </summary>
        /// <param name="language">Localization Language, (default:'en')</param>
        /// <param name="articletype">Type of the Article ('null' = Filter disabled, possible values: BITMASK values: 1 = basearticle, 2 = book article, 4 = contentarticle, 8 = eventarticle, 16 = pressarticle, 32 = recipe, 64 = touroperator , 128 = b2b), (also possible for compatibily reasons: basisartikel, buchtippartikel, contentartikel, veranstaltungsartikel, presseartikel, rezeptartikel, reiseveranstalter, b2bartikel ) (default:'255' == ALL), REFERENCE TO: GET /api/ArticleTypes</param>
        /// <param name="articlesubtype">Sub Type of the Article (depends on the Maintype of the Article 'null' = Filter disabled)</param>
        /// <param name="odhtagfilter">ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=article'), (default:'null')</param>
        /// <param name="startdate">Filter by ArticleDate Format (yyyy-MM-dd HH:mm)</param>
        /// <param name="enddate">Filter by ArticleDate Format (yyyy-MM-dd HH:mm)</param>
        /// <param name="sortbyarticledate">Sort By Articledate ('true' sorts Articles by Articledate)</param>
        /// <param name="active">Active Articles Filter (possible Values: 'true' only Active Articles, 'false' only Disabled Articles</param>
        /// <param name="odhactive">ODH Active (Published) Activities Filter (Refers to field SmgActive) Article Filter (possible Values: 'true' only published Article, 'false' only not published Articles, (default:'null')</param>
        /// <param name="source">Filter by Source (Separator ','), (Sources available 'idm','noi'...),(default: 'null')</param>
        /// <returns>Collection of Article Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<ArticleReduced>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("ArticleReduced")]
        public Task<IActionResult> GetArticleReducedList(
            string? language = "en",
            string? articletype = "255",
            string? articlesubtype = null,
            string? startdate = null,
            string? enddate = null,
            string? publishedon = null,
            string? source = null,
            string? odhtagfilter = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            CancellationToken cancellationToken = default
        )
        {
            return GetArticleReduced(
                language,
                articletype,
                articlesubtype,
                startdate,
                enddate,
                source,
                null,
                publishedon,
                active?.Value,
                odhactive?.Value,
                odhtagfilter,
                fields: fields ?? Array.Empty<string>(),
                rawfilter,
                rawsort,
                searchfilter,
                cancellationToken
            );
        }

        private Task<IActionResult> GetArticleReduced(
            string? language,
            string? articletype,
            string? articlesubtype,
            string? articledate,
            string? articledateto,
            string? source,
            bool? sortbyarticledate,
            string? publishedon,
            bool? active,
            bool? smgactive,
            string? smgtags,
            string[] fields,
            string? rawfilter,
            string? rawsort,
            string? searchfilter,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                AdditionalFiltersToAddEndpoint("Article")
                    .TryGetValue("Read", out var additionalfilter);

                ArticleHelper helper = ArticleHelper.Create(
                    articletype,
                    articlesubtype,
                    null,
                    language,
                    null,
                    active,
                    smgactive,
                    smgtags,
                    null,
                    articledate,
                    articledateto,
                    source,
                    null,
                    publishedon
                );

                string select =
                    $"data#>>'\\{{Id\\}}' as \"Id\", data#>>'\\{{Detail,{language},Title\\}}' as \"Name\"";
                //string orderby = "data#>>'\\{Shortname\\}' ASC";

                //Custom Fields filter
                if (fields.Length > 0)
                    select += string.Join(
                        "",
                        fields
                            .Where(x => x != "Id")
                            .Select(field =>
                                $", data#>'\\{{{field.Replace(".", ",")}\\}}' as \"{field}\""
                            )
                    );

                var query = (XQuery)
                    QueryFactory
                        .Query()
                        .SelectRaw(select)
                        .From("articles")
                        .ArticleWhereExpression(
                            idlist: helper.idlist,
                            typelist: helper.typelist,
                            subtypelist: helper.subtypelist,
                            languagelist: helper.languagelist,
                            smgtaglist: helper.smgtaglist,
                            tagdict: helper.tagdict,
                            highlight: helper.highlight,
                            activefilter: helper.active,
                            smgactivefilter: helper.smgactive,
                            articledate: helper.articledate,
                            articledateto: helper.articledateto,
                            sourcelist: helper.sourcelist,
                            publishedonlist: helper.publishedonlist,
                            searchfilter: searchfilter,
                            language: language,
                            lastchange: null,
                            additionalfilter: additionalfilter,
                            userroles: UserRolesToFilterEndpoint("Article")
                        )
                        .ApplyRawFilter(rawfilter)
                        .ApplyOrdering(new PGGeoSearchResult() { geosearch = false }, rawsort);

                var compiled = query.Compiler.Compile(query);

                var reader = await query.Connection.ExecuteReaderAsync(
                    compiled.Sql,
                    compiled.NamedBindings
                );

                return reader.ReadAndParseTOJson();
            });
        }

        [Obsolete("Deprecated, use the Article Endpoint")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet, Route("ArticleChanged")]
        public RedirectToActionResult GetAllArticleChanged(
            uint pagenumber = 1,
            uint pagesize = 10,
            string? seed = null,
            string? updatefrom = null,
            CancellationToken cancellationToken = default
        )
        {
            updatefrom ??= String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));

            return RedirectToAction(
                "Article",
                "v1",
                new RouteValueDictionary
                {
                    { "pagenumber", pagenumber },
                    { "pagesize", pagesize },
                    { "seed", seed },
                    { "updatefrom", updatefrom },
                }
            );
        }

        #endregion

        #region AccommodationController

        /// <summary>
        /// GET Reduced Accommodation List
        /// </summary>
        /// <param name="language">Localization Language</param>
        /// <param name="categoryfilter">Categoryfilter (BITMASK values: 1 = (not categorized), 2 = (1star), 4 = (1flower), 8 = (1sun), 14 = (1star/1flower/1sun), 16 = (2stars), 32 = (2flowers), 64 = (2suns), 112 = (2stars/2flowers/2suns), 128 = (3stars), 256 = (3flowers), 512 = (3suns), 1024 = (3sstars), 1920 = (3stars/3flowers/3suns/3sstars), 2048 = (4stars), 4096 = (4flowers), 8192 = (4suns), 16384 = (4sstars), 30720 = (4stars/4flowers/4suns/4sstars), 32768 = (5stars), 65536 = (5flowers), 131072 = (5suns), 229376 = (5stars/5flowers/5suns), 'null' = No Filter), (default:'null')</param>
        /// <param name="typefilter">Typefilter (BITMASK values: 1 = (HotelPension), 2 = (BedBreakfast), 4 = (Farm), 8 = (Camping), 16 = (Youth), 32 = (Mountain), 64 = (Apartment), 128 = (Not defined),'null' = No Filter), (default:'null')</param>
        /// <param name="boardfilter">Boardfilter (BITMASK values: 0 = (all boards), 1 = (without board), 2 = (breakfast), 4 = (half board), 8 = (full board), 16 = (All inclusive), 'null' = No Filter), (default:'0')</param>
        /// <param name="featurefilter">FeatureFilter (BITMASK values: 1 = (Group-friendly), 2 = (Meeting rooms), 4 = (Swimming pool), 8 = (Sauna), 16 = (Garage), 32 = (Pick-up service), 64 = (WLAN), 128 = (Barrier-free), 256 = (Special menus for allergy sufferers), 512 = (Pets welcome), 'null' = No Filter), (default:'null')</param>
        /// <param name="featureidfilter">Feature Id Filter, filter over ALL Features vailable (Separator ',' List of Feature IDs, 'null' = No Filter), (default:'null')</param>
        /// <param name="themefilter">Themefilter (BITMASK values: 1 = (Gourmet), 2 = (At altitude), 4 = (Regional wellness offerings), 8 = (on the wheels), 16 = (With family), 32 = (Hiking), 64 = (In the vineyards), 128 = (Urban vibe), 256 = (At the ski resort), 512 = (Mediterranean), 1024 = (In the Dolomites), 2048 = (Alpine), 4096 = (Small and charming), 8192 = (Huts and mountain inns), 16384 = (Rural way of life), 32768 = (Balance), 65536 = (Christmas markets), 'null' = No Filter), (default:'null')</param>
        /// <param name="badgefilter">BadgeFilter (BITMASK values: 1 = (Belvita Wellness Hotel), 2 = (Familyhotel), 4 = (Bikehotel), 8 = (Red Rooster Farm), 16 = (Barrier free certificated), 32 = (Vitalpina Hiking Hotel), 64 = (Private Rooms in South Tyrol), 128 = (Vinum Hotels), 'null' = No Filter), (default:'null')</param>
        /// <param name="locfilter">Locfilter (Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = No Filter), (default:'null')</param>
        /// <param name="odhtagfilter">ODHTag Filter (refers to Array SmgTags) (String, Separator ',' more ODHTags possible, 'null' = No Filter, available ODHTags reference to 'v1/ODHTag?validforentity=accommodation'), (default:'null')</param>
        /// <param name="odhactive">ODHActive Filter (refers to field SmgActive) (possible Values: 'null' Displays all Accommodations, 'true' only ODH Active Accommodations, 'false' only ODH Disabled Accommodations, (default:'null')</param>
        /// <param name="active">TIC Active Filter (possible Values: 'null' Displays all Accommodations, 'true' only TIC Active Accommodations, 'false' only TIC Disabled Accommodations, (default:'null')</param>
        /// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <param name="fields">Select fields to display, by Default Title and Id are selected if fields filter is null More fields are indicated by separator ',' example fields=Id,Active,Shortname. Select also Dictionary fields, example Detail.de.Title, or Elements of Arrays example ImageGallery[0].ImageUrl. (default:'null' all fields are displayed)</param>
        /// <returns>Collection with Accommodation Reduced Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<AccommodationReduced>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("AccommodationReduced")]
        public async Task<IActionResult> GetReducedAccosAsync(
            string? language = "en",
            string? categoryfilter = null,
            string? typefilter = null,
            string? boardfilter = null,
            string? featurefilter = null,
            string? featureidfilter = null,
            string? themefilter = null,
            string? badgefilter = null,
            string? locfilter = null,
            string? odhtagfilter = null,
            LegacyBool odhactive = null!,
            LegacyBool active = null!,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool suedtirolmobil = false,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );

            return await GetAccommodationReduced(
                language,
                categoryfilter,
                typefilter,
                boardfilter,
                featurefilter,
                themefilter,
                badgefilter,
                locfilter,
                active?.Value,
                odhactive?.Value,
                odhtagfilter,
                featureidfilter,
                geosearchresult,
                fields: fields ?? Array.Empty<string>(),
                rawfilter,
                rawsort,
                searchfilter,
                suedtirolmobil,
                cancellationToken
            );
        }

        private Task<IActionResult> GetAccommodationReduced(
            string? language,
            string? categoryfilter,
            string? typefilter,
            string? boardfilter,
            string? featurefilter,
            string? themefilter,
            string? badgefilter,
            string? locfilter,
            bool? active,
            bool? smgactive,
            string? smgtagfilter,
            string? featureridfilter,
            PGGeoSearchResult geosearchresult,
            string[] fields,
            string? rawfilter,
            string? rawsort,
            string? searchfilter,
            bool stahack,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                AdditionalFiltersToAddEndpoint("Accommodation")
                    .TryGetValue("Read", out var additionalfilter);

                AccommodationHelper myhelper = await AccommodationHelper.CreateAsync(
                    QueryFactory,
                    idfilter: null,
                    locfilter: locfilter,
                    boardfilter: boardfilter,
                    categoryfilter: categoryfilter,
                    typefilter: typefilter,
                    featurefilter: featurefilter,
                    featureidfilter: featureridfilter,
                    badgefilter: badgefilter,
                    themefilter: themefilter,
                    altitudefilter: null,
                    smgtags: smgtagfilter,
                    tagfilter: null,
                    activefilter: active,
                    smgactivefilter: smgactive,
                    bookablefilter: null,
                    sourcefilter: null,
                    lastchange: null,
                    langfilter: language,
                    publishedonfilter: null,
                    cancellationToken
                );

                string select =
                    $"data#>>'\\{{Id\\}}' as \"Id\", data#>>'\\{{AccoDetail,{language},Name\\}}' as \"Name\"";
                //string orderby = "data#>>'\\{Shortname\\}' ASC";

                //Custom Fields filter
                if (fields.Length > 0)
                    select += string.Join(
                        "",
                        fields
                            .Where(x => x != "Id")
                            .Select(field =>
                                $", data#>'\\{{{field.Replace(".", ",")}\\}}' as \"{field}\""
                            )
                    );

                var query = (XQuery)
                    QueryFactory
                        .Query()
                        .SelectRaw(select)
                        .From("accommodations")
                        .AccommodationWhereExpression(
                            idlist: myhelper.idlist,
                            accotypelist: myhelper.accotypelist,
                            categorylist: myhelper.categorylist,
                            featurelist: myhelper.featurelist,
                            featureidlist: myhelper.featureidlist,
                            badgelist: myhelper.badgelist,
                            themelist: myhelper.themelist,
                            boardlist: myhelper.boardlist,
                            smgtaglist: myhelper.smgtaglist,
                            tagdict: myhelper.tagdict,
                            districtlist: myhelper.districtlist,
                            municipalitylist: myhelper.municipalitylist,
                            tourismvereinlist: myhelper.tourismvereinlist,
                            regionlist: myhelper.regionlist,
                            apartmentfilter: myhelper.apartment,
                            bookable: myhelper.bookable,
                            altitude: myhelper.altitude,
                            altitudemin: myhelper.altitudemin,
                            altitudemax: myhelper.altitudemax,
                            activefilter: myhelper.active,
                            smgactivefilter: myhelper.smgactive,
                            publishedonlist: myhelper.publishedonlist,
                            sourcelist: new List<string>(),
                            searchfilter: searchfilter,
                            language: language,
                            lastchange: myhelper.lastchange,
                            languagelist: new List<string>(),
                            additionalfilter: additionalfilter,
                            userroles: UserRolesToFilterEndpoint("Accommodation")
                        )
                        .ApplyRawFilter(rawfilter)
                        .ApplyOrdering_GeneratedColumns(geosearchresult, rawsort);

                var compiled = query.Compiler.Compile(query);

                var reader = await query.Connection.ExecuteReaderAsync(
                    compiled.Sql,
                    compiled.NamedBindings
                );

                return reader.ReadAndParseTOJson();
            });
        }

        [Obsolete("Deprecated, use the Accommodation Endpoint")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet, Route("AccommodationChanged")]
        public RedirectToActionResult GetAllAccommodationChanged(
            uint pagenumber = 1,
            uint pagesize = 10,
            string? seed = null,
            string? updatefrom = null,
            CancellationToken cancellationToken = default
        )
        {
            updatefrom ??= String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));

            return RedirectToAction(
                "Accommodation",
                "v1",
                new RouteValueDictionary
                {
                    { "pagenumber", pagenumber },
                    { "pagesize", pagesize },
                    { "seed", seed },
                    { "updatefrom", updatefrom },
                }
            );
        }

        #endregion

        #region EventShortController

        //TODO ADD EVENTSHORT REDUCED METHOD

        /// <summary>
        /// GET EventShort Reduced List
        /// </summary>
        /// <param name="language">Localization Language</param>
        /// <param name="startdate">Format (yyyy-MM-dd HH:mm) default or Unix Timestamp</param>
        /// <param name="enddate">Format (yyyy-MM-dd HH:mm) default or Unix Timestamp</param>
        /// <param name="datetimeformat">not provided, use default format, for unix timestamp pass "uxtimestamp"</param>
        /// <param name="source">Source of the data, (possible values 'Content' or 'EBMS')</param>
        /// <param name="eventlocation">Event Location, (possible values, 'NOI' or 'EC')</param>
        /// <param name="onlyactive">'true' if only Events marked as Active by Eurac should be displayed</param>
        /// <returns>List of EventShortReduced Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<ActivityPoiReduced>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("EventShort/Reduced")]
        [HttpGet, Route("EventShortReduced")]
        public Task<IActionResult> GetReducedAsync(
            string language = "en",
            string? startdate = null,
            string? enddate = null,
            string? datetimeformat = null,
            string? source = null,
            string? eventlocation = null,
            LegacyBool onlyactive = null!,
            LegacyBool websiteactive = null!,
            LegacyBool communityactive = null!,
            bool active = true,
            string? webaddress = null,
            string? sortorder = "ASC",
            string? lastchange = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            CancellationToken cancellationToken = default
        )
        {
            return GetEventShortReduced(
                fields: fields ?? Array.Empty<string>(),
                language,
                searchfilter,
                startdate,
                enddate,
                datetimeformat,
                source,
                eventlocation,
                webaddress,
                onlyactive.Value,
                websiteactive.Value,
                communityactive.Value,
                active,
                sortorder,
                null,
                rawfilter,
                rawsort,
                cancellationToken
            );
        }

        private Task<IActionResult> GetEventShortReduced(
            string[] fields,
            string language,
            string? searchfilter,
            string? startdate,
            string? enddate,
            string? datetimeformat,
            string? sourcefilter,
            string? eventlocationfilter,
            string? webaddressfilter,
            bool? todayactive,
            bool? websiteactive,
            bool? communityactive,
            bool active,
            string? sortorder,
            string? lastchange,
            string? rawfilter,
            string? rawsort,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                AdditionalFiltersToAddEndpoint("EventShort")
                    .TryGetValue("Read", out var additionalfilter);

                string select =
                    $"data#>>'\\{{Id\\}}' as \"Id\", data#>>'\\{{EventDescription{language.ToUpper()}\\}}' as \"Name\"";

                string orderby =
                    $"data#>>'\\{{EventDescription{language.ToUpper()}\\}}' {sortorder}";

                //Custom Fields filter
                if (fields.Length > 0)
                    select += string.Join(
                        "",
                        fields
                            .Where(x => x != "Id")
                            .Select(field =>
                                $", data#>'\\{{{field.Replace(".", ",")}\\}}' as \"{field}\""
                            )
                    );

                EventShortHelper myeventshorthelper = EventShortHelper.Create(
                    startdate,
                    enddate,
                    datetimeformat,
                    sourcefilter,
                    null,
                    eventlocationfilter,
                    todayactive,
                    websiteactive,
                    communityactive,
                    active,
                    null,
                    webaddressfilter,
                    lastchange,
                    language,
                    sortorder,
                    null
                );

                var query = (XQuery)
                    QueryFactory
                        .Query()
                        .SelectRaw(select)
                        .From("eventeuracnoi")
                        .EventShortWhereExpression(
                            idlist: myeventshorthelper.idlist,
                            languagelist: myeventshorthelper.languagelist,
                            sourcelist: myeventshorthelper.sourcelist,
                            tagdict: myeventshorthelper.tagdict,
                            eventlocationlist: myeventshorthelper.eventlocationlist,
                            webaddresslist: myeventshorthelper.webaddresslist,
                            start: myeventshorthelper.start,
                            end: myeventshorthelper.end,
                            todayactivefilter: myeventshorthelper.todayactivefilter,
                            websiteactivefilter: myeventshorthelper.websiteactivefilter,
                            communityactivefilter: myeventshorthelper.communityactivefilter,
                            activefilter: myeventshorthelper.activefilter,
                            publishedonlist: myeventshorthelper.publishedonlist,
                            searchfilter: searchfilter,
                            language: language,
                            lastchange: myeventshorthelper.lastchange,
                            additionalfilter: additionalfilter,
                            userroles: UserRolesToFilterEndpoint("EventShort"),
                            getbyrooms: false
                        )
                        .ApplyRawFilter(rawfilter)
                        .ApplyOrdering(
                            new PGGeoSearchResult() { geosearch = false },
                            rawsort,
                            orderby
                        );

                var compiled = query.Compiler.Compile(query);

                var reader = await query.Connection.ExecuteReaderAsync(
                    compiled.Sql,
                    compiled.NamedBindings
                );

                return reader.ReadAndParseTOJson();
            });
        }

        #endregion

        #region CommonController

        ////Reduced GETTER

        /// <summary>
        /// GET MetaRegion Reduced List
        /// </summary>
        /// <param name="language">Localization Language, (default:'en')</param>
        /// <param name="elements">Elements to retrieve (0 = Get All)</param>
        /// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <returns>Collection of CommonReduced Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<CommonReduced>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("MetaRegionReduced")]
        public async Task<IActionResult> GetMetaRegionsReduced(
            string language = "en",
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                null,
                language,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                cancellationToken
            );
            AdditionalFiltersToAddEndpoint("MetaRegion")
                .TryGetValue("Read", out var additionalfilter);

            return await GetCommonReduced(
                "metaregions",
                searchfilter,
                language,
                commonhelper,
                fields: fields ?? Array.Empty<string>(),
                rawfilter,
                rawsort,
                additionalfilter,
                "MetaRegion",
                geosearchresult,
                cancellationToken
            );
        }

        /// <summary>
        /// GET ExperienceArea Reduced List
        /// </summary>
        /// <param name="language">Localization Language, (default:'en')</param>
        /// <param name="elements">Elements to retrieve (0 = Get All)</param>
        /// <param name="visibleinsearch">Filter only Elements flagged with visibleinsearch: (possible values: 'true','false'), (default:'false')</param>
        /// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <returns>Collection of ExperienceAreaName Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<CommonReduced>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("ExperienceAreaReduced")]
        public async Task<IActionResult> GetExperienceAreasReduced(
            string language = "en",
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            bool? visibleinsearch = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                null,
                language,
                visibleinsearch,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                cancellationToken
            );
            AdditionalFiltersToAddEndpoint("ExperienceArea")
                .TryGetValue("Read", out var additionalfilter);

            return await GetCommonReduced(
                "experienceareas",
                searchfilter,
                language,
                commonhelper,
                fields: fields ?? Array.Empty<string>(),
                rawfilter,
                rawsort,
                additionalfilter,
                "ExperienceArea",
                geosearchresult,
                cancellationToken
            );
        }

        /// <summary>
        /// GET Region Reduced List
        /// </summary>
        /// <param name="language">Localization Language, (default:'en')</param>
        /// <param name="elements">Elements to retrieve (0 = Get All)</param>
        /// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <returns>Collection of CommonReduced Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<CommonReduced>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("api/RegionReduced")]
        public async Task<IActionResult> GetRegionsReduced(
            string language = "en",
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            //bool? visibleinsearch = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                null,
                language,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                cancellationToken
            );
            AdditionalFiltersToAddEndpoint("Region").TryGetValue("Read", out var additionalfilter);

            return await GetCommonReduced(
                "regions",
                searchfilter,
                language,
                commonhelper,
                fields: fields ?? Array.Empty<string>(),
                rawfilter,
                rawsort,
                additionalfilter,
                "Region",
                geosearchresult,
                cancellationToken
            );
        }

        /// <summary>
        /// GET TourismAssociation Reduced List
        /// </summary>
        /// <param name="language">Localization Language, (default:'en')</param>
        /// <param name="elements">Elements to retrieve (0 = Get All)</param>
        /// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <returns>Collection of CommonReduced Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<CommonReduced>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("TourismAssociationReduced")]
        public async Task<IActionResult> GetTourismvereinReduced(
            string language = "en",
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            //bool? visibleinsearch = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                null,
                language,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                cancellationToken
            );
            AdditionalFiltersToAddEndpoint("TourismOrganization")
                .TryGetValue("Read", out var additionalfilter);

            return await GetCommonReduced(
                "tvs",
                searchfilter,
                language,
                commonhelper,
                fields: fields ?? Array.Empty<string>(),
                rawfilter,
                rawsort,
                additionalfilter,
                "TourismOrganization",
                geosearchresult,
                cancellationToken
            );
        }

        /// <summary>
        /// GET Municipality Reduced List
        /// </summary>
        /// <param name="language">Localization Language, (default:'en')</param>
        /// <param name="elements">Elements to retrieve (0 = Get All)</param>
        /// <param name="visibleinsearch">Filter only Elements flagged with visibleinsearch: (possible values: 'true','false'), (default:'false')</param>
        /// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <returns>Collection of CommonReduced Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<CommonReduced>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("MunicipalityReduced")]
        public async Task<IActionResult> GetMunicipalityReduced(
            string language = "en",
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            bool? visibleinsearch = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                null,
                language,
                visibleinsearch,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                cancellationToken
            );
            AdditionalFiltersToAddEndpoint("Municipality")
                .TryGetValue("Read", out var additionalfilter);

            return await GetCommonReduced(
                "municipalities",
                searchfilter,
                language,
                commonhelper,
                fields: fields ?? Array.Empty<string>(),
                rawfilter,
                rawsort,
                additionalfilter,
                "Municipality",
                geosearchresult,
                cancellationToken
            );
        }

        /// <summary>
        /// GET District Reduced List
        /// </summary>
        /// <param name="language">Localization Language, (default:'en')</param>
        /// <param name="elements">Elements to retrieve (0 = Get All)</param>
        /// <param name="visibleinsearch">Filter only Elements flagged with visibleinsearch: (possible values: 'true','false'), (default:'false')</param>
        /// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <returns>Collection of CommonReduced Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<CommonReduced>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("DistrictReduced")]
        public async Task<IActionResult> GetDistrictReduced(
            string language = "en",
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            bool? visibleinsearch = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                null,
                language,
                visibleinsearch,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                cancellationToken
            );
            AdditionalFiltersToAddEndpoint("District")
                .TryGetValue("Read", out var additionalfilter);

            return await GetCommonReduced(
                "districts",
                searchfilter,
                language,
                commonhelper,
                fields: fields ?? Array.Empty<string>(),
                rawfilter,
                rawsort,
                additionalfilter,
                "District",
                geosearchresult,
                cancellationToken
            );
        }

        /// <summary>
        /// GET SkiRegion Reduced List
        /// </summary>
        /// <param name="language">Localization Language, (default:'en')</param>
        /// <param name="elements">Elements to retrieve (0 = Get All)</param>
        /// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <returns>Collection of CommonReduced Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<CommonReduced>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("SkiRegionReduced")]
        public async Task<IActionResult> GetSkiRegionReduced(
            string language = "en",
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            //bool? visibleinsearch = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                null,
                language,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                cancellationToken
            );
            AdditionalFiltersToAddEndpoint("SkiRegion")
                .TryGetValue("Read", out var additionalfilter);

            return await GetCommonReduced(
                "skiregions",
                searchfilter,
                language,
                commonhelper,
                fields: fields ?? Array.Empty<string>(),
                rawfilter,
                rawsort,
                additionalfilter,
                "SkiRegion",
                geosearchresult,
                cancellationToken
            );
        }

        /// <summary>
        /// GET SkiArea Reduced List
        /// </summary>
        /// <param name="language">Localization Language, (default:'en')</param>
        /// <param name="elements">Elements to retrieve (0 = Get All)</param>
        /// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <returns>Collection of CommonReduced Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<CommonReduced>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("SkiAreaReduced")]
        public async Task<IActionResult> GetSkiAreaReduced(
            string language = "en",
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            //bool? visibleinsearch = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                null,
                language,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                cancellationToken
            );
            AdditionalFiltersToAddEndpoint("SkiArea").TryGetValue("Read", out var additionalfilter);

            return await GetCommonReduced(
                "skiareas",
                searchfilter,
                language,
                commonhelper,
                fields: fields ?? Array.Empty<string>(),
                rawfilter,
                rawsort,
                additionalfilter,
                "SkiArea",
                geosearchresult,
                cancellationToken
            );
        }

        private Task<IActionResult> GetCommonReduced(
            string tablename,
            string? searchfilter,
            string? language,
            CommonHelper commonhelper,
            string[] fields,
            string? rawfilter,
            string? rawsort,
            string? additionalfilter,
            string? userrolestofilterpath,
            PGGeoSearchResult geosearchresult,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                string select =
                    $"data#>>'\\{{Id\\}}' as Id, data#>>'\\{{Detail,{language},Title\\}}' as Name";
                string orderby = "data#>>'\\{Shortname\\}' ASC";

                //Custom Fields filter
                if (fields.Length > 0)
                    select += string.Join(
                        "",
                        fields
                            .Where(x => x != "Id")
                            .Select(field =>
                                $", data#>'\\{{{field.Replace(".", ",")}\\}}' as \"{field}\""
                            )
                    );

                var query = (XQuery)
                    QueryFactory
                        .Query()
                        .SelectRaw(select)
                        .From(tablename)
                        .CommonWhereExpression(
                            idlist: commonhelper.idlist,
                            languagelist: commonhelper.languagelist,
                            visibleinsearch: commonhelper.visibleinsearch,
                            smgtaglist: commonhelper.smgtaglist,
                            activefilter: commonhelper.active,
                            odhactivefilter: commonhelper.smgactive,
                            publishedonlist: commonhelper.publishedonlist,
                            sourcelist: commonhelper.sourcelist,
                            tagdict: commonhelper.tagdict,
                            searchfilter: searchfilter,
                            language: language,
                            lastchange: commonhelper.lastchange,
                            additionalfilter: additionalfilter,
                            userroles: UserRolesToFilterEndpoint(userrolestofilterpath!)
                        )
                        .ApplyRawFilter(rawfilter)
                        .ApplyOrdering_GeneratedColumns(geosearchresult, rawsort, orderby);
                //.OrderByRaw(orderby)
                //.GeoSearchFilterAndOrderby(geosearchresult);

                var compiled = query.Compiler.Compile(query);

                var reader = await query.Connection.ExecuteReaderAsync(
                    compiled.Sql,
                    compiled.NamedBindings
                );

                return reader.ReadAndParseTOJson();
            });
        }

        #endregion

        #region WebcamInfoController

        /// <summary>
        /// GET Webcam Reduced List
        /// </summary>
        /// <param name="language">Localization Language, (default:'en')</param>
        /// <param name="source">Source Filter(String, ), (default:'null')</param>
        /// <param name="active">Active Webcam Filter (possible Values: 'true' only Active Gastronomies, 'false' only Disabled Gastronomies</param>
        /// <param name="odhactive">ODH Active (refers to field SmgActive) (Published) Webcam Filter (possible Values: 'true' only published Webcam, 'false' only not published Webcam, (default:'null')</param>
        /// <param name="latitude">GeoFilter Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius to Search in KM. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <param name="fields">Select fields to display, by Default Title and Id are selected if fields filter is null More fields are indicated by separator ',' example fields=Id,Active,Shortname. Select also Dictionary fields, example Detail.de.Title, or Elements of Arrays example ImageGallery[0].ImageUrl. (default:'null' all fields are displayed)</param>
        /// <param name="updatefrom">Date from Format (yyyy-MM-dd) (all GBActivityPoi with LastChange >= datefrom are passed), (default: DateTime.Now - 1 Day)</param>
        /// <returns>Collection of WebcamInfoReduced Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<WebcamInfoReduced>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("WebcamInfoReduced")]
        public async Task<IActionResult> GetReducedWebcamInfoAsync(
            string language = "en",
            string? source = null,
            string? updatefrom = null,
            LegacyBool odhactive = null!,
            LegacyBool active = null!,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool suedtirolmobil = false,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );

            return await GetWebcamInfoReduced(
                fields: fields ?? Array.Empty<string>(),
                language,
                source,
                searchfilter,
                active?.Value,
                odhactive?.Value,
                updatefrom,
                geosearchresult,
                rawfilter,
                rawsort,
                cancellationToken
            );
        }

        private Task<IActionResult> GetWebcamInfoReduced(
            string[] fields,
            string? language,
            string? source,
            string? searchfilter,
            bool? active,
            bool? smgactive,
            string? lastchange,
            PGGeoSearchResult geosearchresult,
            string? rawfilter,
            string? rawsort,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                AdditionalFiltersToAddEndpoint("WebcamInfo")
                    .TryGetValue("SkiArea", out var additionalfilter);

                WebcamInfoHelper mywebcaminfohelper = WebcamInfoHelper.Create(
                    source,
                    null,
                    null,
                    active,
                    smgactive,
                    lastchange,
                    null
                );

                string select =
                    $"data#>>'\\{{Id\\}}' as \"Id\", data#>>'\\{{Webcamname,{language}\\}}' as \"Name\"";
                //string orderby = "data#>>'\\{Shortname\\}' ASC";

                //Custom Fields filter
                if (fields.Length > 0)
                    select += string.Join(
                        "",
                        fields
                            .Where(x => x != "Id")
                            .Select(field =>
                                $", data#>'\\{{{field.Replace(".", ",")}\\}}' as \"{field}\""
                            )
                    );

                var query = (XQuery)
                    QueryFactory
                        .Query()
                        .SelectRaw(select)
                        .From("webcams")
                        .WebCamInfoWhereExpression(
                            idlist: mywebcaminfohelper.idlist,
                            sourcelist: mywebcaminfohelper.sourcelist,
                            activefilter: mywebcaminfohelper.active,
                            smgactivefilter: mywebcaminfohelper.smgactive,
                            tagdict: mywebcaminfohelper.tagdict,
                            publishedonlist: mywebcaminfohelper.publishedonlist,
                            searchfilter: searchfilter,
                            language: language,
                            lastchange: mywebcaminfohelper.lastchange,
                            languagelist: new List<string>(),
                            additionalfilter: additionalfilter,
                            userroles: UserRolesToFilterEndpoint("WebcamInfo")
                        )
                        .ApplyRawFilter(rawfilter)
                        .ApplyOrdering_GeneratedColumns(geosearchresult, rawsort);

                var compiled = query.Compiler.Compile(query);

                var reader = await query.Connection.ExecuteReaderAsync(
                    compiled.Sql,
                    compiled.NamedBindings
                );

                return reader.ReadAndParseTOJson();
            });
        }

        #endregion
    }

    class ResultReduced
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    public static class CompatibilityHelpers
    {
        public static bool ObjectIsNullOrEmpty(this object obj)
        {
            return obj == null || obj.ToString() == String.Empty;
        }

        public static List<Dictionary<string, object?>> ReadAndParseTOJson(this IDataReader reader)
        {
            var data = new List<Dictionary<string, object?>>();

            var names = Enumerable
                .Range(0, reader.FieldCount)
                .Select(i => (i, reader.GetName(i), reader.GetDataTypeName(i)));

            static bool IsJson(string typeName) =>
                typeName switch
                {
                    "json" => true,
                    "jsonb" => true,
                    _ => false,
                };

            while (reader.Read())
            {
                var dict = new Dictionary<string, object?>();
                foreach (var (i, name, typeName) in names)
                {
                    var value = reader.GetValue(i);
                    //var value = reader.GetString(i);
                    dict.Add(
                        name,
                        IsJson(typeName)
                            ? value.ObjectIsNullOrEmpty()
                                ? null
                                : new JRaw(value)
                            : value
                    );
                }
                data.Add(dict);
            }

            return data;
        }
    }
}
