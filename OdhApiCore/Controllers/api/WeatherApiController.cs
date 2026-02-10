// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Generic;
using Helper.Identity;
using Helper.Location;
using Helper.Tagging;
using LCS;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OdhApiCore.Filters;
using OdhApiCore.Responses;
using OdhNotifier;
using SIAG;
using SIAG.Model;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiCore.Controllers
{
    /// <summary>
    /// Weather Api (data provided by SIAG, LTS) SOME DATA Available as OPENDATA
    /// </summary>
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class WeatherController : OdhController
    {
        private readonly ISettings settings;

        public WeatherController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<WeatherController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier)
        {
            this.settings = settings;
        }

        #region SwaggerExposed API

        /// <summary>
        /// GET Current Suedtirol Weather LIVE
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="locfilter">Locfilter (possible values: filter by StationData 1 = Schlanders, 2 = Meran, 3 = Bozen, 4 = Sterzing, 5 = Brixen, 6 = Bruneck | filter nearest Station to Region,TV,Municipality,Fraction reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), '' = No Filter). IF a Locfilter is set, only Stationdata is provided.</param>
        /// <returns>Weather Object</returns>
        [ProducesResponseType(typeof(Weather), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Weather")]
        public async Task<IActionResult> GetWeather(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? language = "en",
            string? locfilter = null,
            bool extended = true,
            string? source = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                return await GetWeatherLive(
                    pagenumber,
                    pagesize,
                    language ?? "en",
                    locfilter,
                    extended,
                    source ?? "opendata",
                    null,
                    fields: fields ?? Array.Empty<string>(),
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
            }
        }

        /// <summary>
        /// GET Current Suedtirol Weather LIVE Single
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="id">ID</param>
        /// <returns>Weather Object</returns>
        [ProducesResponseType(typeof(Weather), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Weather/{id}", Name = "SingleWeather")]
        public async Task<IActionResult> GetWeatherSingle(
            string id,
            string? language = "en",
            string? source = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                return await GetWeatherLive(
                    null,
                    null,
                    language ?? "en",
                    null,
                    true,
                    source ?? "opendata",
                    id,
                    fields: fields ?? Array.Empty<string>(),
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
            }
        }

        /// <summary>
        /// GET Suedtirol Weather HISTORY
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="language">Language</param>
        /// <param name="datefilter">DateFilter Format dd/MM/yyyy</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null)<a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param> /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>WeatherHistory Object</returns>
        [ProducesResponseType(typeof(WeatherHistory), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("WeatherHistory")]
        public async Task<IActionResult> GetWeatherHistory(
            uint pagenumber = 1,
            PageSize pagesize = null!,
            string? language = null,
            string? idlist = null,
            string? locfilter = null,
            string? datefrom = null,
            string? dateto = null,
            string? seed = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? lastchange = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                return await GetWeatherHistoryList(
                    pagenumber: pagenumber,
                    pagesize: pagesize,
                    language: language,
                    idfilter: idlist,
                    locfilter: locfilter,
                    datefrom: datefrom,
                    dateto: dateto,
                    lastchange: lastchange,
                    searchfilter: searchfilter,
                    seed: seed,
                    fields: fields ?? Array.Empty<string>(),
                    null,
                    new PGGeoSearchResult(),
                    rawfilter,
                    rawsort,
                    removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
            }
        }

        /// <summary>
        /// GET Suedtirol Weather HISTORY SINGLE
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>WeatherHistory Object</returns>
        [ProducesResponseType(typeof(WeatherHistory), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("WeatherHistory/{id}", Name = "SingleWeatherHistory")]
        public async Task<IActionResult> GetWeatherSingleHistory(
            string id,
            string? language = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetWeatherHistorySingle(
                id,
                language,
                fields: fields ?? Array.Empty<string>(),
                removenullvalues,
                cancellationToken
            );
        }

        /// <summary>
        /// GET District Weather LIVE
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="locfilter">Locfilter (possible values: filter by District 1 = Etschtal/Überetsch/Unterland, 2 = Burggrafenamt, 3 = Vinschgau, 4 = Eisacktal und Sarntal, 5 = Wipptal, 6 = Pustertal/Dolomiten, 7 = Ladinien-Dolomiten | filter nearest DistrictWeather to Region,TV,Municipality,Fraction reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction))</param>
        /// <returns>Bezirks Weather Object</returns>
        [ProducesResponseType(typeof(BezirksWeather), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Weather/District")]
        public async Task<IActionResult> GetDistrictWeather(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? locfilter = null,
            string? language = "en",
            string? source = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                return await GetBezirksWetter(
                    pagenumber,
                    pagesize,
                    language ?? "en",
                    locfilter,
                    source ?? "opendata",
                    fields: fields ?? Array.Empty<string>(),
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
            }
        }

        /// <summary>
        /// GET District Weather LIVE SINGLE
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="language">Language</param>
        /// <returns>District Weather Object</returns>
        [ProducesResponseType(typeof(BezirksWeather), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Weather/District/{id}", Name = "SingleWeatherDistrict")]
        public async Task<IActionResult> GetDistrictWeatherSingle(
            string id,
            string? language = "en",
            string? source = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                return await GetBezirksWetter(
                    null,
                    null,
                    language ?? "en",
                    id,
                    source ?? "opendata",
                    fields: fields ?? Array.Empty<string>(),
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
            }
        }

        /// <summary>
        /// GET Current Realtime Weather LIVE
        /// </summary>
        /// <param name="language">Language</param>
        /// <returns>WeatherRealTime Object</returns>
        [ProducesResponseType(typeof(IEnumerable<WeatherRealTime>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Weather/Realtime")]
        public async Task<IActionResult> GetRealtimeWeather(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? language = "en",
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                    latitude,
                    longitude,
                    radius
                );

                return await GetRealTimeWeather(
                    pagenumber,
                    pagesize,
                    language ?? "en",
                    null,
                    fields: fields ?? Array.Empty<string>(),
                    geosearchresult,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
            }
        }

        /// <summary>
        /// GET Current Realtime Weather LIVE Single
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="id">id</param>
        /// <returns>WeatherRealTime Object</returns>
        [ProducesResponseType(typeof(IEnumerable<WeatherRealTime>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Weather/Realtime/{id}", Name = "SingleWeatherRealtime")]
        public async Task<IActionResult> GetRealtimeWeatherSingle(
            string id,
            string? language = "en",
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                return await GetRealTimeWeather(
                    null,
                    null,
                    language ?? "en",
                    id,
                    fields: fields ?? Array.Empty<string>(),
                    null,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
            }
        }

        /// <summary>
        /// GET Weather Forecast
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="locfilter">Locfilter (possible values: filter on reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction))</param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <returns>WeatherForecast Object</returns>
        [ProducesResponseType(typeof(IEnumerable<WeatherForecastLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Weather/Forecast")]
        public async Task<IActionResult> GetWeatherForecast(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? locfilter = null,
            string? language = "en",
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            CancellationToken cancellationToken = default
        )
        {
            try
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

                return await GetWeatherForecastFromFile(
                    pagenumber,
                    pagesize,
                    fields: fields ?? Array.Empty<string>(),
                    language ?? "en",
                    null,
                    locfilter,
                    polygonsearchresult,
                    geosearchresult,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
            }
        }

        /// <summary>
        /// GET Weather Forecast Single
        /// </summary>
        /// <param name="language">Language</param>
        /// <returns>WeatherForecast Object</returns>
        [ProducesResponseType(typeof(WeatherForecastLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Weather/Forecast/{id}", Name = "SingleWeatherForecast")]
        public async Task<IActionResult> GetWeatherForecastSingle(
            string id,
            string? language = "en",
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                return await GetWeatherForecastFromFile(
                    null,
                    null,
                    fields: fields ?? Array.Empty<string>(),
                    language ?? "en",
                    id,
                    null,
                    null,
                    null,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
            }
        }

        /// <summary>
        /// GET Measuringpoint LIST
        /// </summary>
        /// <param name="idlist">IDFilter (Separator ',' List of Gastronomy IDs), (default:'null')</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null') </param>
        /// <param name="locfilter">Locfilter (Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null')</param>
        /// <param name="areafilter">Area ID (multiple IDs possible, separated by ",")</param>
        /// <param name="skiareafilter">Skiarea ID</param>
        /// <param name="active">Active Filter (possible Values: 'true' only Active Measuringpoints, 'false' only Disabled Measuringpoints), (default:'null')</param>
        /// <param name="tagfilter">Filter on Tags. (Endpoint on v1/Tag) Syntax =and/or(Tag.Id,Tag.Id,Tag.Id) example or(summer,hiking) - and(themed hikes,family hikings) - or(hiking) - and(summer) - Combining and/or is not supported at the moment, default: 'null')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter' target="_blank">Wiki geosort</a></param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>List of Measuringpoint Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<MeasuringpointV2>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[OdhCacheOutput(ClientTimeSpan = 0, ServerTimeSpan = 3600, CacheKeyGenerator = typeof(CustomCacheKeyGenerator), MustRevalidate = true)]
        [HttpGet, Route("Weather/Measuringpoint")]
        public async Task<IActionResult> GetMeasuringPoints(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? idlist = null,
            string? locfilter = null,
            string? tagfilter = null,
            string? areafilter = null,
            string? skiareafilter = null,
            string? language = null,
            string? source = null,
            LegacyBool active = null!,            
            string? publishedon = null,
            string? updatefrom = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            string? seed = null,
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

            return await GetMeasuringPointList(
                pagenumber,
                pagesize,
                fields: fields ?? Array.Empty<string>(),
                language: language,
                idfilter: idlist,
                searchfilter: searchfilter,
                locfilter: locfilter,
                areafilter: areafilter,
                skiareafilter: skiareafilter,
                source: source,
                active: active,
                tagfilter: tagfilter,
                publishedon: publishedon,                
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
        /// GET Measuringpoint SINGLE
        /// </summary>
        /// <param name="id">Measuringpoint ID</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Measuringpoint Object</returns>
        [ProducesResponseType(typeof(MeasuringpointV2), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Weather/Measuringpoint/{id}", Name = "SingleWeatherMeasuringpoint")]
        public async Task<IActionResult> GetMeasuringPoint(
            string id,
            string? language = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetMeasuringPointSingle(
                id,
                language,
                fields: fields ?? Array.Empty<string>(),
                removenullvalues,
                cancellationToken
            );
        }

        /// <summary>
        /// GET Snowreport Data LIVE
        /// </summary>
        /// <param name="lang">Language</param>
        /// <param name="skiareaid">Skiarea ID</param>
        /// <returns>Snowreport BaseData Object</returns>
        [ProducesResponseType(typeof(SnowReportBaseData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OdhCacheOutput(
            ClientTimeSpan = 0,
            ServerTimeSpan = 3600,
            CacheKeyGenerator = typeof(CustomCacheKeyGenerator)
        )]
        [HttpGet, Route("Weather/SnowReport")]
        public async Task<ActionResult> GetSnowReportBase(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? skiareaid = null,
            string? lang = "en",
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                if (!String.IsNullOrEmpty(skiareaid))
                {
                    var snowreport = await GetSnowReportBaseData(
                        lang,
                        skiareaid,
                        cancellationToken
                    );

                    if (pagenumber != null)
                    {
                        return Ok(
                            ResponseHelpers.GetResult(
                                pagenumber.Value,
                                1,
                                1,
                                null,
                                new List<SnowReportBaseData>() { snowreport },
                                Url
                            )
                        );
                    }
                    else
                    {
                        return Ok(snowreport);
                    }
                }
                else
                {
                    //Get all skiareaids
                    var query = QueryFactory
                        .Query()
                        .Select("id")
                        .From("skiareas")
                        .Where("gen_active", true);

                    var skiareaids = await query.GetAsync<string>();

                    List<SnowReportBaseData> snowreportbasedatalist =
                        new List<SnowReportBaseData>();

                    //Fall 1 Getter auf ALL
                    foreach (var myskiareaid in skiareaids)
                    {
                        var result = await GetSnowReportBaseData(
                            lang,
                            myskiareaid,
                            cancellationToken
                        );

                        if (result != null)
                            snowreportbasedatalist.Add(result);
                    }

                    if (pagenumber != null)
                    {
                        return Ok(
                            ResponseHelpers.GetResult(
                                pagenumber.Value,
                                1,
                                (uint)snowreportbasedatalist.Count,
                                null,
                                snowreportbasedatalist,
                                Url
                            )
                        );
                    }
                    else
                    {
                        return Ok(snowreportbasedatalist);
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
            }
        }

        /// <summary>
        /// GET Snowreport Data LIVE Single
        /// </summary>
        /// <param name="lang">Language</param>
        /// <param name="id">Skiarea ID</param>
        /// <returns>Snowreport BaseData Object</returns>
        [ProducesResponseType(typeof(SnowReportBaseData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OdhCacheOutput(
            ClientTimeSpan = 0,
            ServerTimeSpan = 3600,
            CacheKeyGenerator = typeof(CustomCacheKeyGenerator)
        )]
        [HttpGet, Route("Weather/SnowReport/{id}", Name = "SingleWeatherSnowReport")]
        public async Task<ActionResult> GetSnowReportBaseSingle(
            string id,
            string? lang = "en",
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var snowreport = await GetSnowReportBaseData(
                    lang,
                    id.ToUpper().Replace("_SNOWREPORT", ""),
                    cancellationToken
                );

                return Ok(snowreport);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
            }
        }

        #endregion

        #region Weather

        /// GET Current Suedtirol Weather LIVE Request
        private async Task<IActionResult> GetWeatherLive(
            uint? pagenumber,
            int? pagesize,
            string language,
            string? locfilter,
            bool extended,
            string source,
            string? id,
            string[] fields,
            CancellationToken cancellationToken
        )
        {
            var weatherresult = default(WeatherLinked);

            if (String.IsNullOrEmpty(locfilter))
            {
                //Get Weather General from Siag and Parse it to ODH Format
                var weatherresponsetask = await SIAG.GetWeatherData.GetSiagWeatherData(
                    language,
                    settings.SiagConfig.Username,
                    settings.SiagConfig.Password,
                    extended,
                    source,
                    id
                );
                weatherresult = await SIAG.GetWeatherData.ParseSiagWeatherDataToODHWeather(
                    language,
                    settings.XmlConfig.XmldirWeather,
                    weatherresponsetask,
                    extended,
                    source
                );
            }
            else
            {
                string stationidtype = "";
                int n;
                if (int.TryParse(locfilter, out n))
                    stationidtype = "Id";
                else
                {
                    //Locfilter kann hier sein TV, Municipality und Fraktion
                    if (locfilter.Contains("reg"))
                    {
                        stationidtype = "Region";
                        locfilter = locfilter.Replace("reg", "");
                    }
                    if (locfilter.Contains("tvs"))
                    {
                        stationidtype = "TV";
                        locfilter = locfilter.Replace("tvs", "");
                    }
                    if (locfilter.Contains("mun"))
                    {
                        stationidtype = "Municipality";
                        locfilter = locfilter.Replace("mun", "");
                    }
                    if (locfilter.Contains("fra"))
                    {
                        stationidtype = "Fraktion";
                        locfilter = locfilter.Replace("fra", "");
                    }
                }

                weatherresult = await SIAG.GetWeatherData.GetCurrentStationWeatherAsync(
                    language,
                    locfilter,
                    stationidtype,
                    settings.XmlConfig.XmldirWeather,
                    settings.SiagConfig.Username,
                    settings.SiagConfig.Password,
                    true,
                    source
                );
            }

            if (weatherresult != null)
            {
                weatherresult._Meta = MetadataHelper.GetMetadataobject<WeatherLinked>(
                    weatherresult
                );

                var data = new List<JsonRaw>() { new JsonRaw(weatherresult) };
                var dataTransformed = data.Select(raw =>
                    raw.TransformRawData(
                        language,
                        fields,
                        filteroutNullValues: false,
                        urlGenerator: UrlGenerator,
                        fieldstohide: null
                    )
                );

                if (pagenumber != null)
                {
                    return Ok(
                        ResponseHelpers.GetResult(
                            pagenumber.Value,
                            1,
                            1,
                            null,
                            dataTransformed,
                            Url
                        )
                    );
                }
                else
                {
                    return Ok(dataTransformed.FirstOrDefault());
                }
            }
            else
                return BadRequest("something went wrong");
        }

        /// GET Bezirkswetter by LocFilter LIVE Request
        private async Task<IActionResult> GetBezirksWetter(
            uint? pagenumber,
            int? pagesize,
            string language,
            string? locfilter,
            string source,
            string[] fields,
            CancellationToken cancellationToken
        )
        {
            string bezirksid = "";
            string tvrid = "";
            string regid = "";

            if (!String.IsNullOrEmpty(locfilter))
            {
                int n;
                if (int.TryParse(locfilter, out n))
                    bezirksid = n.ToString();
                else
                {
                    //Locfilter kann hier sein Region TV, Municipality und Fraktion
                    if (locfilter.Contains("reg"))
                    {
                        regid = locfilter.Replace("reg", "");
                    }

                    if (locfilter.Contains("tvs"))
                    {
                        tvrid = locfilter.Replace("tvs", "");
                    }
                    if (locfilter.Contains("mun"))
                    {
                        var query = QueryFactory
                            .Query()
                            .SelectRaw("data ->>'TourismvereinId'")
                            .From("municipalities")
                            .Where("id", locfilter.Replace("mun", "").ToUpper());

                        tvrid = await query.FirstOrDefaultAsync<string>();
                    }
                    if (locfilter.Contains("fra"))
                    {
                        var query = QueryFactory
                            .Query()
                            .SelectRaw("data ->>'TourismvereinId'")
                            .From("districts")
                            .Where("id", locfilter.Replace("fra", "").ToUpper());

                        tvrid = await query.FirstOrDefaultAsync<string>();
                    }
                }
            }

            var weatherresult = await GetWeatherData.GetCurrentBezirkWeatherAsync(
                language,
                bezirksid,
                tvrid,
                regid,
                settings.XmlConfig.XmldirWeather,
                settings.SiagConfig.Username,
                settings.SiagConfig.Password,
                true,
                source
            );
            var weatherparsed = await GetWeatherData.ParseSiagBezirkWeatherDataToODHWeather(
                weatherresult,
                language,
                true,
                source
            );

            var data = new List<JsonRaw>();

            foreach (var wresult in weatherparsed)
            {
                if (wresult != null)
                {
                    wresult._Meta = MetadataHelper.GetMetadataobject<WeatherDistrictLinked>(
                        wresult
                    );

                    data.Add(new JsonRaw(wresult));
                }
            }

            var dataTransformed = data.Select(raw =>
                raw.TransformRawData(
                    language,
                    fields,
                    filteroutNullValues: false,
                    urlGenerator: UrlGenerator,
                    fieldstohide: null
                )
            );

            if (pagenumber != null)
            {
                return Ok(
                    ResponseHelpers.GetResult(
                        pagenumber.Value,
                        1,
                        (uint)dataTransformed.Count(),
                        null,
                        dataTransformed,
                        Url
                    )
                );
            }
            else
            {
                //Compatibility Hack
                if (weatherresult.Count() == 1)
                    return Ok(dataTransformed.FirstOrDefault());
                else
                    return Ok(dataTransformed);
            }
        }

        /// GET Current Suedtirol Weather Realtime LIVE Request
        private async Task<IActionResult> GetRealTimeWeather(
            uint? pagenumber,
            int? pagesize,
            string language,
            string? id,
            string[] fields,
            PGGeoSearchResult geosearchresult,
            CancellationToken cancellationToken
        )
        {
            var weatherresult = await GetWeatherData.GetCurrentRealTimeWEatherAsync(language);
            foreach (var weather in weatherresult)
            {
                weather._Meta = MetadataHelper.GetMetadataobject<WeatherRealTimeLinked>(weather);
            }
            var data = new List<JsonRaw>();

            if (!String.IsNullOrEmpty(id))
            {
                if (weatherresult.Where(x => x.id == id).Count() > 0)
                    data.Add(new JsonRaw(weatherresult.Where(x => x.id == id).FirstOrDefault()));
            }
            else
            {
                if (geosearchresult.geosearch)
                {
                    Dictionary<double, WeatherRealTimeLinked> ordereddistance =
                        new Dictionary<double, WeatherRealTimeLinked>();
                    //TODO calculate distance and order by it NOT WORKING
                    foreach (var weatherealtime in weatherresult)
                    {
                        var distance = DistanceCalculator.Distance(
                            geosearchresult.latitude,
                            geosearchresult.longitude,
                            weatherealtime.latitude.GetValueOrDefault(0),
                            weatherealtime.longitude.GetValueOrDefault(0),
                            'K'
                        );

                        double radiusdistancem = distance * 1000;

                        if (radiusdistancem < geosearchresult.radius)
                            ordereddistance.Add(distance, weatherealtime);
                    }

                    weatherresult = ordereddistance
                        .OrderBy(x => x.Key)
                        .Select(x => x.Value)
                        .ToList();
                }

                data = JsonRawUtils.ConvertObjectToJsonRaw(weatherresult).ToList();
            }

            var dataTransformed = data.Select(raw =>
                raw.TransformRawData(
                    language,
                    fields,
                    filteroutNullValues: false,
                    urlGenerator: UrlGenerator,
                    fieldstohide: null
                )
            );

            if (pagenumber != null)
            {
                return Ok(
                    ResponseHelpers.GetResult(
                        pagenumber.Value,
                        1,
                        (uint)dataTransformed.Count(),
                        null,
                        dataTransformed,
                        Url
                    )
                );
            }
            else
            {
                //Compatibility Hack
                if (dataTransformed.Count() == 1)
                    return Ok(dataTransformed.FirstOrDefault());
                else
                    return Ok(dataTransformed);
            }
        }

        /// GET Bezirkswetter by LocFilter LIVE Request
        private async Task<IActionResult> GetWeatherForecastFromFile(
            uint? pagenumber,
            int? pagesize,
            string[] fields,
            string language,
            string? id,
            string? locfilter,
            GeoPolygonSearchResult? polygonsearchresult,
            PGGeoSearchResult geosearchresult,
            CancellationToken cancellationToken
        )
        {
            IEnumerable<MunicipalityIdIstatNumber> municipalities =
                new List<MunicipalityIdIstatNumber>();

            List<string> municipalitycodes = new List<string>();
            List<string> municipalityids = new List<string>();
            List<string> tvids = new List<string>();
            List<string> regids = new List<string>();

            if (id != null)
                municipalitycodes.Add(id.Replace("forecast_", ""));

            //Locfilter stuff
            if (locfilter != null)
            {
                foreach (var locfiltersplitted in locfilter.Split(","))
                {
                    if (locfiltersplitted.Contains("mun"))
                        municipalityids.AddRange(
                            CommonListCreator.CreateDistrictIdList(locfiltersplitted, "mun")
                        );
                    else if (locfiltersplitted.Contains("tvs"))
                        regids.AddRange(
                            CommonListCreator.CreateDistrictIdList(locfiltersplitted, "tvs")
                        );
                    else if (locfiltersplitted.Contains("reg"))
                        regids.AddRange(
                            CommonListCreator.CreateDistrictIdList(locfiltersplitted, "reg")
                        );
                }
            }

            //Generate from GPS
            var query = QueryFactory
                .Query()
                .SelectRaw(
                    $"id, data#>>'\\{{IstatNumber\\}}' as \"istatnumber\", data#>>'\\{{Detail,{language},Title\\}}' as \"name\""
                )
                .From("municipalities")
                .When(municipalityids.Count > 0, x => x.WhereIn("id", municipalityids))
                .When(
                    municipalitycodes.Count > 0,
                    x => x.WhereInJsonb(municipalitycodes, "IstatNumber")
                )
                .When(tvids.Count > 0, x => x.WhereInJsonb(tvids, "TourismvereinId"))
                .When(regids.Count > 0, x => x.WhereInJsonb(regids, "RegionId"))
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
                .GeoSearchFilterAndOrderby_GeneratedColumns(geosearchresult);

            municipalities = await query.GetAsync<MunicipalityIdIstatNumber>();

            //Get Data and parse
            var parsed = await GetWeatherData.GetWeatherForeCastAsync(
                language,
                municipalitycodes,
                await GetWeatherForecastFromS3()
            );
            var data = new List<JsonRaw>();

            foreach (var forecast in parsed)
            {
                var municipality = municipalities
                    .Where(x => x.istatnumber == forecast.MunicipalityIstatCode)
                    .FirstOrDefault();

                if (forecast != null && municipality != null)
                {
                    //Todo filter Municipalities

                    forecast._Meta = MetadataHelper.GetMetadataobject<WeatherForecastLinked>(
                        forecast
                    );

                    if (municipality != null)
                    {
                        forecast.LocationInfo = new LocationInfoLinked()
                        {
                            MunicipalityInfo = new MunicipalityInfoLinked()
                            {
                                Id = municipality.id,
                                Name = new Dictionary<string, string>()
                                {
                                    { language, municipality.name },
                                },
                            },
                        };
                    }

                    data.Add(new JsonRaw(forecast));
                }
            }

            //var data = JsonRawUtils.ConvertObjectToJsonRaw(parsed);

            var dataTransformed = data.Select(raw =>
                raw.TransformRawData(
                    language,
                    fields,
                    filteroutNullValues: false,
                    urlGenerator: UrlGenerator,
                    fieldstohide: null
                )
            );

            if (pagenumber != null)
            {
                return Ok(
                    ResponseHelpers.GetResult(
                        pagenumber.Value,
                        1,
                        (uint)dataTransformed.Count(),
                        null,
                        dataTransformed,
                        Url
                    )
                );
            }
            else
            {
                //Compatibility Hack
                if (parsed.Count() == 1)
                    return Ok(dataTransformed.FirstOrDefault());
                else
                    return Ok(dataTransformed);
            }
        }

        private async Task<SiagWeatherForecastModel> GetWeatherForecastFromS3()
        {
            if (!settings.S3Config.ContainsKey("dc-meteorology-province-forecast"))
                throw new Exception("No weatherforecast file found");

            //var s3bucket = settings.S3Config["dc-meteorology-province-forecast"];

            //TransferUtility fileTransferUtility =
            //    new TransferUtility(new AmazonS3Client(s3bucket.AccessKey, s3bucket.AccessSecretKey, Amazon.RegionEndpoint.EUWest1));

            //var request = new TransferUtilityDownloadRequest()
            //{
            //    BucketName = s3bucket.Bucket,
            //    Key = s3bucket.Filename,
            //    FilePath = settings.JsonConfig.Jsondir + s3bucket.Filename
            //};
            //await fileTransferUtility.DownloadAsync(request);


            string jsonString = System.IO.File.ReadAllText(settings.JsonConfig.Jsondir + settings.S3Config["dc-meteorology-province-forecast"].Filename, Encoding.UTF8);
            
            if(jsonString != null)
                return JsonConvert.DeserializeObject<SiagWeatherForecastModel>(Regex.Unescape(jsonString));
            else
                throw new Exception("Unable to parse file");

            //using (
            //    StreamReader r = new StreamReader(
            //        settings.JsonConfig.Jsondir
            //            + settings.S3Config["dc-meteorology-province-forecast"].Filename, Encoding.ASCII
            //    )
            //)
            //{
            //    string json = r.ReadToEnd();

                //    //Encoding latinEncoding = Encoding.GetEncoding("iso-8859-1");
                //    //Encoding utf8Encoding = Encoding.UTF8;

                //    //byte[] latinBytes = latinEncoding.GetBytes(json);
                //    //byte[] utf8Bytes = Encoding.Convert(latinEncoding, utf8Encoding, latinBytes);

                //    //var utf8String = Encoding.UTF8.GetString(utf8Bytes);



                //    if (json != null)
                //        return System.Text.Json.JsonSerializer.Deserialize<SiagWeatherForecastModel>(json);
                //    //return JsonConvert.DeserializeObject<SiagWeatherForecastModel>(json);
                //    else
                //        throw new Exception("Unable to parse file");
                //}
        }

        #endregion

        #region WeatherHistory

        /// GET Suedtirol Weather from History Table
        private Task<IActionResult> GetWeatherHistoryList(
            uint pagenumber,
            int? pagesize,
            string? language,
            string? idfilter,
            string? locfilter,
            string? datefrom,
            string? dateto,
            string? lastchange,
            string? searchfilter,
            string? seed,
            string[] fields,
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

                WeatherHelper myweatherhelper = await WeatherHelper.CreateAsync(
                    QueryFactory,
                    idfilter,
                    locfilter,
                    language,
                    datefrom,
                    dateto,
                    lastchange,
                    cancellationToken
                );

                var query = QueryFactory
                    .Query()
                    .When(getasidarray, x => x.Select("id"))
                    .When(!getasidarray, x => x.SelectRaw("data"))
                    .From("weatherdatahistory")
                    .WeatherHistoryWhereExpression(
                        languagelist: myweatherhelper.languagelist,
                        idlist: myweatherhelper.idlist,
                        sourcelist: new List<string>(),
                        begindate: myweatherhelper.datefrom,
                        enddate: myweatherhelper.dateto,
                        searchfilter: searchfilter,
                        language: language,
                        lastchange: myweatherhelper.lastchange,
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
                    .ApplyOrdering(ref seed, geosearchresult, rawsort);
                //.ApplyOrdering_GeneratedColumns(ref seed, geosearchresult, rawsort);//

                //IF getasidarray set simply return array of ids
                if (getasidarray)
                {
                    return await query.GetAsync<string>();
                }

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

                //return dataTransformed;

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

        /// GET Weather History SINGLE by ID
        private Task<IActionResult> GetWeatherHistorySingle(
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
                    .Query("weatherdatahistory")
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

        #region Measuringpoint

        /// GET Measuringpoints LIST
        private Task<IActionResult> GetMeasuringPointList(
            uint? pagenumber,
            int? pagesize,
            string? language,
            string? idfilter,
            string? locfilter,
            string? areafilter,
            string? skiareafilter,
            string? source,
            bool? active,
            string? tagfilter,
            string? lastchange,
            string? publishedon,
            string? searchfilter,
            string? seed,
            string[] fields,
            GeoPolygonSearchResult? polygonsearchresult,
            PGGeoSearchResult geosearchresult,
            string? rawfilter,
            string? rawsort,
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAddEndpoint("Weather/Measuringpoint")
                    .TryGetValue("Read", out var additionalfilter);

                //Fix add are to every arefilter item
                string? areafilterwithprefix = String.IsNullOrEmpty(areafilter)
                    ? ""
                    : "are" + areafilter;
                //string? skiarefilterwithprefix = String.IsNullOrEmpty(skiareafilter) ? "" : "ska" + skiareafilter;

                MeasuringPointsHelper mymeasuringpointshelper = await MeasuringPointsHelper.Create(
                    QueryFactory,
                    idfilter,
                    locfilter,
                    areafilterwithprefix,
                    skiareafilter,
                    source,
                    active,
                    tagfilter,
                    lastchange,
                    publishedon,
                    cancellationToken
                );

                var query = QueryFactory
                    .Query()
                    .When(getasidarray, x => x.Select("id"))
                    .When(!getasidarray, x => x.SelectRaw("data"))
                    .From("measuringpoints")
                    .MeasuringpointWhereExpression(
                        idlist: mymeasuringpointshelper.idlist,
                        districtlist: mymeasuringpointshelper.districtlist,
                        municipalitylist: mymeasuringpointshelper.municipalitylist,
                        tourismvereinlist: mymeasuringpointshelper.tourismvereinlist,
                        regionlist: mymeasuringpointshelper.regionlist,
                        arealist: mymeasuringpointshelper.arealist,
                        skiarealist: mymeasuringpointshelper.skiarealist,
                        activefilter: mymeasuringpointshelper.active,
                        tagdict: mymeasuringpointshelper.tagdict,
                        publishedonlist: mymeasuringpointshelper.publishedonlist,
                        sourcelist: mymeasuringpointshelper.sourcelist,
                        searchfilter: searchfilter,
                        language: language,
                        lastchange: mymeasuringpointshelper.lastchange,
                        additionalfilter: additionalfilter,
                        userroles: UserRolesToFilterEndpoint("Weather/Measuringpoint")
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
                    .ApplyOrdering_GeneratedColumns(ref seed, geosearchresult, rawsort); //.ApplyOrdering(ref seed, geosearchresult, rawsort);

                //Hack Paging on Measuringpoints
                if (pagenumber != null)
                {
                    //IF getasidarray set simply return array of ids
                    if (getasidarray)
                    {
                        return await query.GetAsync<string>();
                    }

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
                    //IF getasidarray set simply return array of ids
                    if (getasidarray)
                    {
                        return await query.GetAsync<string>();
                    }

                    var data = await query.GetAsync<JsonRaw>();

                    return data.Select(raw =>
                        raw.TransformRawData(
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

        /// GET Measuringpoint SINGLE by ID
        private Task<IActionResult> GetMeasuringPointSingle(
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
                AdditionalFiltersToAddEndpoint("Weather/Measuringpoint")
                    .TryGetValue("Read", out var additionalfilter);

                var query = QueryFactory
                    .Query("measuringpoints")
                    .Select("data")
                    //.Where("id", id.ToUpper())
                    .Where("gen_id", id.ToUpper())
                    .When(
                        !String.IsNullOrEmpty(additionalfilter),
                        q => q.FilterAdditionalDataByCondition(additionalfilter)
                    )
                    .FilterDataByAccessRoles(UserRolesToFilterEndpoint("Weather/Measuringpoint"));

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

        #region SnowReport

        /// GET Snowreport Data by SkiareaID LIVE
        private async Task<SnowReportBaseData> GetSnowReportBaseData(
            string? lang,
            string skiareaid,
            CancellationToken cancellationToken
        )
        {
            if (lang == null)
                throw new Exception("parameter lang is null");

            var query = QueryFactory
                .Query()
                .SelectRaw("data")
                .From("skiareas")
                .Where("id", skiareaid);

            var skiarearaw = await query.FirstOrDefaultAsync<JsonRaw>();

            if (skiarearaw == null)
                throw new Exception("skiarea not found!");

            var skiarea = JsonConvert.DeserializeObject<SkiArea>(skiarearaw.Value);

            List<WebcamInfo> webcamlist = new List<WebcamInfo>();

            if (
                skiarea.RelatedContent != null
                && skiarea.RelatedContent.Where(x => x.Type == "webcam").Count() > 0
            )
            {
                var webcamidlist = skiarea
                    .RelatedContent.Where(x => x.Type == "webcam")
                    .ToList()
                    .Select(y => y.Id)
                    .ToList();
                if (webcamidlist != null && webcamidlist.Count > 0)
                {
                    var webcamquery = QueryFactory
                        .Query()
                        .SelectRaw("data")
                        .From("webcams")
                        .IdUpperFilter(webcamidlist);

                    var webcamsraw = await webcamquery.GetAsync<JsonRaw>();

                    foreach (var webcamraw in webcamsraw)
                    {
                        webcamlist.Add(JsonConvert.DeserializeObject<WebcamInfo>(webcamraw.Value));
                    }
                }
            }

            //Schoffts net afn SkiArea object zu bringen kriag do ollm a laars object
            //var skiarea2 = await query.FirstOrDefaultAsync<SkiArea>();

            //Des hingegen geat
            //var skiareastring = await query.FirstOrDefaultAsync<string>();
            //var skiareaobject = JsonConvert.DeserializeObject<SkiArea>(skiareastring);

            var mysnowreport = GetSnowReport.GetLiveSnowReport(
                lang,
                skiarea!,
                webcamlist,
                "SMG",
                settings.LcsConfig.ServiceUrl,
                settings.LcsConfig.Username,
                settings.LcsConfig.Password,
                settings.LcsConfig.MessagePassword
            );

            mysnowreport.Id = mysnowreport.RID + "_SNOWREPORT";

            return mysnowreport;
        }

        #endregion

        #region POST PUT DELETE Measuringpoint

        /// <summary>
        /// POST Insert new Measuringpoint
        /// </summary>
        /// <param name="measuringpoint">Measuringpoint Object</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        [AuthorizeODH(PermissionAction.Create)]
        [HttpPost, Route("Weather/Measuringpoint")]
        public Task<IActionResult> Post([FromBody] MeasuringpointV2 measuringpoint, bool generateid = true)
        {
            measuringpoint.LicenseInfo = LicenseHelper.GetLicenseforMeasuringpoint(measuringpoint);

            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAddEndpoint("Weather/Measuringpoint")
                    .TryGetValue("Create", out var additionalfilter);
                
                if (generateid)
                    measuringpoint.Id = Helper.IdGenerator.GenerateIDFromType(measuringpoint);
                else if (String.IsNullOrEmpty(measuringpoint.Id))
                    throw new Exception("Id is null");

                //POPULATE LocationInfo
                measuringpoint.LocationInfo = await measuringpoint.UpdateLocationInfoExtension(QueryFactory);
                //DistanceCalculation
                await measuringpoint.UpdateDistanceCalculation(QueryFactory);

                //TRIM all strings
                measuringpoint.TrimStringProperties();

                //Populate Tags (Id/Source/Type)
                await measuringpoint.UpdateTagsExtension(QueryFactory);

                return await UpsertData<MeasuringpointV2>(
                    measuringpoint,
                    new DataInfo("measuringpoints", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing Measuringpoint
        /// </summary>
        /// <param name="id">Measuringpoint Id</param>
        /// <param name="measuringpoint">Measuringpoint Object</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        [AuthorizeODH(PermissionAction.Update)]
        [HttpPut, Route("Weather/Measuringpoint/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] MeasuringpointV2 measuringpoint)
        {
            measuringpoint.LicenseInfo = LicenseHelper.GetLicenseforMeasuringpoint(measuringpoint);

            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAddEndpoint("Weather/Measuringpoint")
                    .TryGetValue("Update", out var additionalfilter);

                measuringpoint.Id = Helper.IdGenerator.CheckIdFromType<MeasuringpointV2>(id);

                //POPULATE LocationInfo
                measuringpoint.LocationInfo = await measuringpoint.UpdateLocationInfoExtension(QueryFactory);
                //DistanceCalculation
                await measuringpoint.UpdateDistanceCalculation(QueryFactory);

                //TRIM all strings
                measuringpoint.TrimStringProperties();

                //Populate Tags (Id/Source/Type)
                await measuringpoint.UpdateTagsExtension(QueryFactory);

                return await UpsertData<MeasuringpointV2>(
                    measuringpoint,
                    new DataInfo("measuringpoints", CRUDOperation.Update, true),
                    new CompareConfig(true, true),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE Measuringpoint by Id
        /// </summary>
        /// <param name="id">Measuringpoint Id</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        [AuthorizeODH(PermissionAction.Delete)]
        [HttpDelete, Route("Weather/Measuringpoint/{id}")]
        public Task<IActionResult> Delete(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAddEndpoint("Weather/Measuringpoint")
                    .TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<MeasuringpointV2>(id);

                return await DeleteData<MeasuringpointV2>(
                    id,
                    new DataInfo("measuringpoints", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        #endregion
    }

    public class MunicipalityIdIstatNumber
    {
        public string id { get; set; }
        public string istatnumber { get; set; }
        public string name { get; set; }
    }
}
