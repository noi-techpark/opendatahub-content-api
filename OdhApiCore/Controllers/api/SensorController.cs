// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.CacheOutput;
using DataModel;
using Helper;
using Helper.Generic;
using Helper.Identity;
using Helper.Timeseries;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OdhApiCore.Controllers.helper;
using OdhApiCore.Responses;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiCore.Controllers.api
{
    /// <summary>
    /// Sensor Api - Sensors with metadata and location information
    /// </summary>
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class SensorController : OdhController
    {
        private readonly ISettings settings;
        private readonly HttpClient httpClient;

        public SensorController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<SensorController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier,
            IHttpClientFactory httpClientFactory
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier)
        {
            this.settings = settings;
            this.httpClient = httpClientFactory.CreateClient("TimeseriesAPI");
        }

        #region SWAGGER Exposed API

        /// <summary>
        /// GET Sensor List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null') </param>
        /// <param name="sensortype">Type of sensor (e.g., 'TEMP', 'HUM', 'PRESS', 'PM25'), (default:'null')</param>
        /// <param name="manufacturer">Manufacturer filter (comma-separated list), (default:'null')</param>
        /// <param name="model">Model filter (comma-separated list), (default:'null')</param>
        /// <param name="datasetid">Filter by dataset ID (comma-separated list), (default:'null')</param>
        /// <param name="measurementtypename">Filter by measurement type name (comma-separated list), (default:'null')</param>
        /// <param name="source">Source Filter (possible Values: 'null' Displays all Sensors), (default:'null')</param>
        /// <param name="idlist">IDFilter (Separator ',' List of Sensor IDs), (default:'null')</param>
        /// <param name="langfilter">Language filter (returns only Sensors available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="active">Active Sensor Filter (possible Values: 'true' only active Sensors, 'false' only not active Sensors), (default:'null')</param>
        /// <param name="odhactive">ODH Active (Published) Sensor Filter (Refers to field SmgActive) (possible Values: 'true' only published Sensors, 'false' only not published Sensors), (default:'null')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed)</param>
        /// <param name="searchfilter">String to search for, searches in sensor name, type, and details, (default: null)</param>
        /// <param name="rawfilter">Raw filter for advanced querying</param>
        /// <param name="rawsort">Raw sort for advanced sorting</param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false.</param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)</param>
        /// <param name="tsdatasetids">Timeseries dataset filter (comma-separated dataset IDs), (default:'null')</param>
        /// <param name="tsrequiredtypes">Timeseries required types filter (comma-separated type names, sensor must have ALL), (default:'null')</param>
        /// <param name="tsoptionaltypes">Timeseries optional types filter (comma-separated type names, sensor may have ANY), (default:'null')</param>
        /// <param name="tsmeasurementexpr">Timeseries measurement expression (e.g., 'or(o2.eq.2, and(temp.gteq.20, temp.lteq.30))'), (default:'null')</param>
        /// <param name="tslatestonly">Timeseries latest only filter (only consider latest measurements), (default:null)</param>
        /// <param name="tsstarttime">Timeseries start time filter (RFC3339 format), (default:'null')</param>
        /// <param name="tsendtime">Timeseries end time filter (RFC3339 format), (default:'null')</param>
        /// <returns>Collection of Sensor Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<SensorLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [TypeFilter(typeof(Filters.RequestInterceptorAttribute))]
        [HttpGet, Route("Sensor")]
        public async Task<IActionResult> GetSensorList(
            string? language = null,
            uint pagenumber = 1,
            int pagesize = 10,
            string? seed = null,
            string? sensortype = null,
            string? manufacturer = null,
            string? model = null,
            string? datasetid = null,
            string? measurementtypename = null,
            string? source = null,
            string? idlist = null,
            string? langfilter = null,
            string? odhtagfilter = null,
            string? publishedon = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            DateTime? updatefrom = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            bool getasidarray = false,
            string? tsdatasetids = null,
            string? tsrequiredtypes = null,
            string? tsoptionaltypes = null,
            string? tsmeasurementexpr = null,
            bool? tslatestonly = null,
            string? tsstarttime = null,
            string? tsendtime = null,
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

            try
            {
                return await GetFiltered(
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    pagenumber: pagenumber,
                    pagesize: pagesize,
                    seed: seed,
                    searchfilter: searchfilter,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    removenullvalues: removenullvalues,
                    sensortype: sensortype,
                    manufacturer: manufacturer,
                    model: model,
                    datasetid: datasetid,
                    measurementtypename: measurementtypename,
                    source: source,
                    idlist: idlist,
                    langfilter: langfilter,
                    odhtagfilter: odhtagfilter,
                    publishedon: publishedon,
                    active: active,
                    odhactive: odhactive,
                    polygonsearchresult,
                    geosearchresult,
                    updatefrom: updatefrom,
                    getasidarray: getasidarray,
                    tsdatasetids: tsdatasetids,
                    tsrequiredtypes: tsrequiredtypes,
                    tsoptionaltypes: tsoptionaltypes,
                    tsmeasurementexpr: tsmeasurementexpr,
                    tslatestonly: tslatestonly,
                    tsstarttime: tsstarttime,
                    tsendtime: tsendtime,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// GET Sensor Single
        /// </summary>
        /// <param name="id">ID of the Sensor</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed)</param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false.</param>
        /// <returns>Sensor Object</returns>
        /// <response code="200">Sensor found</response>
        /// <response code="400">Request Error</response>
        /// <response code="404">Sensor not found</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(SensorLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Sensor/{id}", Name = "SingleSensor")]
        public async Task<IActionResult> GetSensor(
            string id,
            string? language = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                return await GetSingle(id, language, fields, removenullvalues, cancellationToken);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// GET Distinct Types from Sensor Discovery
        /// </summary>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null') </param>
        /// <param name="sensortype">Type of sensor (e.g., 'TEMP', 'HUM', 'PRESS', 'PM25'), (default:'null')</param>
        /// <param name="manufacturer">Manufacturer filter (comma-separated list), (default:'null')</param>
        /// <param name="model">Model filter (comma-separated list), (default:'null')</param>
        /// <param name="datasetid">Filter by dataset ID (comma-separated list), (default:'null')</param>
        /// <param name="measurementtypename">Filter by measurement type name (comma-separated list), (default:'null')</param>
        /// <param name="source">Source Filter (possible Values: 'null' Displays all Sensors), (default:'null')</param>
        /// <param name="idlist">IDFilter (Separator ',' List of Sensor IDs), (default:'null')</param>
        /// <param name="langfilter">Language filter (returns only Sensors available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="active">Active Sensor Filter (possible Values: 'true' only active Sensors, 'false' only not active Sensors), (default:'null')</param>
        /// <param name="odhactive">ODH Active (Published) Sensor Filter (Refers to field SmgActive) (possible Values: 'true' only published Sensors, 'false' only not published Sensors), (default:'null')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="searchfilter">String to search for, searches in sensor name, type, and details, (default: null)</param>
        /// <param name="rawfilter">Raw filter for advanced querying</param>
        /// <param name="rawsort">Raw sort for advanced sorting</param>
        /// <param name="tsdatasetids">Timeseries dataset filter (comma-separated dataset IDs), (default:'null')</param>
        /// <param name="tsrequiredtypes">Timeseries required types filter (comma-separated type names, sensor must have ALL), (default:'null')</param>
        /// <param name="tsoptionaltypes">Timeseries optional types filter (comma-separated type names, sensor may have ANY), (default:'null')</param>
        /// <param name="tsmeasurementexpr">Timeseries measurement expression (e.g., 'or(o2.eq.2, and(temp.gteq.20, temp.lteq.30))'), (default:'null')</param>
        /// <param name="tslatestonly">Timeseries latest only filter (only consider latest measurements), (default:null)</param>
        /// <param name="tsstarttime">Timeseries start time filter (RFC3339 format), (default:'null')</param>
        /// <param name="tsendtime">Timeseries end time filter (RFC3339 format), (default:'null')</param>
        /// <returns>Collection of distinct Type objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [TypeFilter(typeof(Filters.RequestInterceptorAttribute))]
        [HttpGet, Route("Sensor/discovery/types/distinct")]
        public async Task<IActionResult> GetSensorDiscoveryTypesDistinct(
            string? seed = null,
            string? sensortype = null,
            string? manufacturer = null,
            string? model = null,
            string? datasetid = null,
            string? measurementtypename = null,
            string? source = null,
            string? idlist = null,
            string? langfilter = null,
            string? odhtagfilter = null,
            string? publishedon = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            DateTime? updatefrom = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            string? tsdatasetids = null,
            string? tsrequiredtypes = null,
            string? tsoptionaltypes = null,
            string? tsmeasurementexpr = null,
            bool? tslatestonly = null,
            string? tsstarttime = null,
            string? tsendtime = null,
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

            try
            {
                return await GetDistinctTypes(
                    seed: seed,
                    searchfilter: searchfilter,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    sensortype: sensortype,
                    manufacturer: manufacturer,
                    model: model,
                    datasetid: datasetid,
                    measurementtypename: measurementtypename,
                    source: source,
                    idlist: idlist,
                    langfilter: langfilter,
                    odhtagfilter: odhtagfilter,
                    publishedon: publishedon,
                    active: active,
                    odhactive: odhactive,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    updatefrom: updatefrom,
                    tsdatasetids: tsdatasetids,
                    tsrequiredtypes: tsrequiredtypes,
                    tsoptionaltypes: tsoptionaltypes,
                    tsmeasurementexpr: tsmeasurementexpr,
                    tslatestonly: tslatestonly,
                    tsstarttime: tsstarttime,
                    tsendtime: tsendtime,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// GET Sensor Discovery with Types
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null') </param>
        /// <param name="sensortype">Type of sensor (e.g., 'TEMP', 'HUM', 'PRESS', 'PM25'), (default:'null')</param>
        /// <param name="manufacturer">Manufacturer filter (comma-separated list), (default:'null')</param>
        /// <param name="model">Model filter (comma-separated list), (default:'null')</param>
        /// <param name="datasetid">Filter by dataset ID (comma-separated list), (default:'null')</param>
        /// <param name="measurementtypename">Filter by measurement type name (comma-separated list), (default:'null')</param>
        /// <param name="source">Source Filter (possible Values: 'null' Displays all Sensors), (default:'null')</param>
        /// <param name="idlist">IDFilter (Separator ',' List of Sensor IDs), (default:'null')</param>
        /// <param name="langfilter">Language filter (returns only Sensors available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="active">Active Sensor Filter (possible Values: 'true' only active Sensors, 'false' only not active Sensors), (default:'null')</param>
        /// <param name="odhactive">ODH Active (Published) Sensor Filter (Refers to field SmgActive) (possible Values: 'true' only published Sensors, 'false' only not published Sensors), (default:'null')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null')</param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null')</param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null')</param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed)</param>
        /// <param name="searchfilter">String to search for, searches in sensor name, type, and details, (default: null)</param>
        /// <param name="rawfilter">Raw filter for advanced querying</param>
        /// <param name="rawsort">Raw sort for advanced sorting</param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false.</param>
        /// <param name="tsdatasetids">Timeseries dataset filter (comma-separated dataset IDs), (default:'null')</param>
        /// <param name="tsrequiredtypes">Timeseries required types filter (comma-separated type names, sensor must have ALL), (default:'null')</param>
        /// <param name="tsoptionaltypes">Timeseries optional types filter (comma-separated type names, sensor may have ANY), (default:'null')</param>
        /// <param name="tsmeasurementexpr">Timeseries measurement expression (e.g., 'or(o2.eq.2, and(temp.gteq.20, temp.lteq.30))'), (default:'null')</param>
        /// <param name="tslatestonly">Timeseries latest only filter (only consider latest measurements), (default:null)</param>
        /// <param name="tsstarttime">Timeseries start time filter (RFC3339 format), (default:'null')</param>
        /// <param name="tsendtime">Timeseries end time filter (RFC3339 format), (default:'null')</param>
        /// <returns>Collection of Sensor Objects with Types</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [TypeFilter(typeof(Filters.RequestInterceptorAttribute))]
        [HttpGet, Route("Sensor/discovery/types")]
        public async Task<IActionResult> GetSensorDiscoveryTypes(
            string? language = null,
            uint pagenumber = 1,
            int pagesize = 10,
            string? seed = null,
            string? sensortype = null,
            string? manufacturer = null,
            string? model = null,
            string? datasetid = null,
            string? measurementtypename = null,
            string? source = null,
            string? idlist = null,
            string? langfilter = null,
            string? odhtagfilter = null,
            string? publishedon = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            DateTime? updatefrom = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            string? tsdatasetids = null,
            string? tsrequiredtypes = null,
            string? tsoptionaltypes = null,
            string? tsmeasurementexpr = null,
            bool? tslatestonly = null,
            string? tsstarttime = null,
            string? tsendtime = null,
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

            try
            {
                return await GetPaginatedTypesWithSensors(
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    pagenumber: pagenumber,
                    pagesize: pagesize,
                    seed: seed,
                    searchfilter: searchfilter,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    removenullvalues: removenullvalues,
                    sensortype: sensortype,
                    manufacturer: manufacturer,
                    model: model,
                    datasetid: datasetid,
                    measurementtypename: measurementtypename,
                    source: source,
                    idlist: idlist,
                    langfilter: langfilter,
                    odhtagfilter: odhtagfilter,
                    publishedon: publishedon,
                    active: active,
                    odhactive: odhactive,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    updatefrom: updatefrom,
                    tsdatasetids: tsdatasetids,
                    tsrequiredtypes: tsrequiredtypes,
                    tsoptionaltypes: tsoptionaltypes,
                    tsmeasurementexpr: tsmeasurementexpr,
                    tslatestonly: tslatestonly,
                    tsstarttime: tsstarttime,
                    tsendtime: tsendtime,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region POST PUT DELETE

        /// <summary>
        /// POST Insert new Sensor
        /// </summary>
        /// <param name="sensor">Sensor Object</param>
        /// <returns>Http Response</returns>
        [AuthorizeODH(PermissionAction.Create)]
        [InvalidateCacheOutput(nameof(GetSensorList))]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost, Route("Sensor")]
        public async Task<IActionResult> Post([FromBody] SensorLinked sensor)
        {
            AdditionalFiltersToAdd.TryGetValue("Create", out var additionalfilter);

            try
            {
                if (sensor != null)
                {
                    if (String.IsNullOrEmpty(sensor.Source))
                        sensor.Source = "api";

                    sensor.Active = true;
                    sensor.FirstImport = DateTime.Now;
                    sensor.LastChange = DateTime.Now;

                    if (sensor._Meta == null)
                        sensor._Meta = new Metadata() { Type = "sensor", LastUpdate = DateTime.Now, Source = sensor.Source };

                    sensor._Meta.LastUpdate = DateTime.Now;

                    return await UpsertData<SensorLinked>(
                        sensor,
                        new DataInfo("sensors", CRUDOperation.Create),
                        new CompareConfig(false, false),
                        new CRUDConstraints(additionalfilter, UserRolesToFilter)
                    );
                }
                else
                {
                    return BadRequest("No Sensor data provided");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// PUT Modify existing Sensor
        /// </summary>
        /// <param name="id">Sensor Id</param>
        /// <param name="sensor">Sensor Object</param>
        /// <returns>Http Response</returns>
        [AuthorizeODH(PermissionAction.Update)]
        [InvalidateCacheOutput(nameof(GetSensorList))]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut, Route("Sensor/{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] SensorLinked sensor)
        {
            AdditionalFiltersToAdd.TryGetValue("Update", out var additionalfilter);

            try
            {
                if (sensor != null)
                {
                    sensor.Id = id;
                    sensor.LastChange = DateTime.Now;

                    if (sensor._Meta == null)
                        sensor._Meta = new Metadata() { Type = "sensor", LastUpdate = DateTime.Now, Source = sensor.Source };

                    sensor._Meta.LastUpdate = DateTime.Now;

                    return await UpsertData<SensorLinked>(
                        sensor,
                        new DataInfo("sensors", CRUDOperation.Update, true),
                        new CompareConfig(false, false),
                        new CRUDConstraints(additionalfilter, UserRolesToFilter)
                    );
                }
                else
                {
                    return BadRequest("No Sensor data provided");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// DELETE Sensor by ID
        /// </summary>
        /// <param name="id">Sensor Id</param>
        /// <returns>Http Response</returns>
        [AuthorizeODH(PermissionAction.Delete)]
        [InvalidateCacheOutput(nameof(GetSensorList))]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpDelete, Route("Sensor/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            AdditionalFiltersToAdd.TryGetValue("Delete", out var additionalfilter);

            try
            {
                return await DeleteData<SensorLinked>(
                    id,
                    new DataInfo("sensors", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region HELPERS

        private Task<IActionResult> GetFiltered(
            string[] fields,
            string? language,
            uint pagenumber,
            int pagesize,
            string? seed,
            string? searchfilter,
            string? rawfilter,
            string? rawsort,
            bool removenullvalues,
            string? sensortype,
            string? manufacturer,
            string? model,
            string? datasetid,
            string? measurementtypename,
            string? source,
            string? idlist,
            string? langfilter,
            string? odhtagfilter,
            string? publishedon,
            LegacyBool active,
            LegacyBool odhactive,
            GeoPolygonSearchResult? polygonsearchresult,
            PGGeoSearchResult geosearchresult,
            DateTime? updatefrom,
            bool getasidarray,
            string? tsdatasetids,
            string? tsrequiredtypes,
            string? tsoptionaltypes,
            string? tsmeasurementexpr,
            bool? tslatestonly,
            string? tsstarttime,
            string? tsendtime,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                SensorHelper myhelper = SensorHelper.Create(
                    QueryFactory,
                    idfilter: idlist,
                    sensortypefilter: sensortype,
                    manufacturerfilter: manufacturer,
                    modelfilter: model,
                    datasetidfilter: datasetid,
                    measurementtypenamefilter: measurementtypename,
                    sourcefilter: source,
                    activefilter: active?.Value,
                    smgactivefilter: odhactive?.Value,
                    smgtags: odhtagfilter,
                    lastchange: updatefrom?.ToString("yyyy-MM-dd"),
                    langfilter: langfilter,
                    publishedonfilter: publishedon,
                    cancellationToken
                );

                // Build base query (without Select, without pagination)
                var baseQuery = QueryFactory
                    .Query()
                    .From("sensors")
                    .SensorWhereExpression(
                        idlist: myhelper.idlist,
                        sensortypelist: myhelper.sensortypelist,
                        manufacturerlist: myhelper.manufacturerlist,
                        modellist: myhelper.modellist,
                        datasetidlist: myhelper.datasetidlist,
                        measurementtypenamelist: myhelper.measurementtypenamelist,
                        smgtaglist: myhelper.smgtaglist,
                        sourcelist: myhelper.sourcelist,
                        languagelist: myhelper.languagelist,
                        publishedonlist: myhelper.publishedonlist,
                        active: myhelper.active,
                        smgactive: myhelper.smgactive,
                        searchfilter: searchfilter,
                        language: language,
                        lastchange: myhelper.lastchange,
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

                // Check if timeseries filtering should be applied
                var timeseriesConfig = TimeseriesFederationExtensions.ParseTimeseriesConfig(
                    settings.TimeseriesConfig.ServiceUrl, tsdatasetids, tsrequiredtypes, tsoptionaltypes,
                    tsmeasurementexpr, tslatestonly, tsstarttime, tsendtime,
                    settings.TimeseriesConfig.FetchBatchSize, Logger);

                if (timeseriesConfig != null && timeseriesConfig.Enabled)
                {
                    // Apply timeseries federation
                    var federationHelper = new TimeseriesFederationHelper(httpClient, Logger, timeseriesConfig);

                    // Fetch and verify (FetchAndVerifySensorsAsync handles pagination internally)
                    var federationResult = await federationHelper.FetchAndVerifySensorsAsync(baseQuery, cancellationToken);
                    if (federationResult.VerifiedIds.Count == 0)
                    {
                        return ResponseHelpers.GetResult(pagenumber, 0, 0, seed,
                            Array.Empty<object>(), Url);
                    }

                    // Fetch full data for verified sensors only
                    baseQuery = baseQuery
                        .Clone()
                        .When(getasidarray, x => x.Select("id"))
                        .When(!getasidarray, x => x.SelectRaw("data"))
                        .WhereIn("id", federationResult.VerifiedIds);
                }
                else
                {
                    baseQuery = baseQuery
                        .Clone()
                        .When(getasidarray, x => x.Select("id"))
                        .When(!getasidarray, x => x.SelectRaw("data"));
                }

                ///IF getasidarray set simply return array of ids
                if (getasidarray)
                {
                    return await baseQuery.GetAsync<string>();
                }

                // Get paginated data
                var data = await baseQuery.PaginateAsync<JsonRaw>(
                    page: (int)pagenumber,
                    perPage: pagesize
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

                return ResponseHelpers.GetResult(pagenumber, totalpages, totalcount,
                    seed, dataTransformed, Url);
            });
        }

        private Task<IActionResult> GetSingle(string id, string? language, string[] fields, bool removenullvalues, CancellationToken cancellationToken)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                var query = QueryFactory
                    .Query("sensors")
                    .Select("data")
                    .Where("id", id)
                    .FilterDataByAccessRoles(UserRolesToFilter)
                    .When(
                        !String.IsNullOrEmpty(additionalfilter),
                        q => q.FilterAdditionalDataByCondition(additionalfilter)
                    );

                var data = await query.FirstOrDefaultAsync<JsonRaw?>(
                    cancellationToken: cancellationToken
                );

                if (data == null)
                {
                    return NotFound();
                }

                var result = data.TransformRawData(
                    language,
                    fields,
                    filteroutNullValues: removenullvalues,
                    urlGenerator: UrlGenerator,
                    fieldstohide: null
                );

                // Return standard JSON format
                return Ok(result);
            });
        }

        private Task<IActionResult> GetDistinctTypes(
            string? seed,
            string? searchfilter,
            string? rawfilter,
            string? rawsort,
            string? sensortype,
            string? manufacturer,
            string? model,
            string? datasetid,
            string? measurementtypename,
            string? source,
            string? idlist,
            string? langfilter,
            string? odhtagfilter,
            string? publishedon,
            LegacyBool active,
            LegacyBool odhactive,
            GeoPolygonSearchResult? polygonsearchresult,
            PGGeoSearchResult geosearchresult,
            DateTime? updatefrom,
            string? tsdatasetids,
            string? tsrequiredtypes,
            string? tsoptionaltypes,
            string? tsmeasurementexpr,
            bool? tslatestonly,
            string? tsstarttime,
            string? tsendtime,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                SensorHelper myhelper = SensorHelper.Create(
                    QueryFactory,
                    idfilter: idlist,
                    sensortypefilter: sensortype,
                    manufacturerfilter: manufacturer,
                    modelfilter: model,
                    datasetidfilter: datasetid,
                    measurementtypenamefilter: measurementtypename,
                    sourcefilter: source,
                    activefilter: active?.Value,
                    smgactivefilter: odhactive?.Value,
                    smgtags: odhtagfilter,
                    lastchange: updatefrom?.ToString("yyyy-MM-dd"),
                    langfilter: langfilter,
                    publishedonfilter: publishedon,
                    cancellationToken
                );

                // Build base query (without Select, without pagination)
                var baseQuery = QueryFactory
                    .Query()
                    .From("sensors")
                    .SensorWhereExpression(
                        idlist: myhelper.idlist,
                        sensortypelist: myhelper.sensortypelist,
                        manufacturerlist: myhelper.manufacturerlist,
                        modellist: myhelper.modellist,
                        datasetidlist: myhelper.datasetidlist,
                        measurementtypenamelist: myhelper.measurementtypenamelist,
                        smgtaglist: myhelper.smgtaglist,
                        sourcelist: myhelper.sourcelist,
                        languagelist: myhelper.languagelist,
                        publishedonlist: myhelper.publishedonlist,
                        active: myhelper.active,
                        smgactive: myhelper.smgactive,
                        searchfilter: searchfilter,
                        language: null,
                        lastchange: myhelper.lastchange,
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

                // Check if timeseries filtering should be applied
                var timeseriesConfig = TimeseriesFederationExtensions.ParseTimeseriesConfig(
                    settings.TimeseriesConfig.ServiceUrl, tsdatasetids, tsrequiredtypes, tsoptionaltypes,
                    tsmeasurementexpr, tslatestonly, tsstarttime, tsendtime,
                    settings.TimeseriesConfig.FetchBatchSize, Logger);

                List<string> verifiedIds;

                if (timeseriesConfig != null && timeseriesConfig.Enabled)
                {
                    // Apply timeseries federation to get FULL list of verified IDs (no pagination)
                    var federationHelper = new TimeseriesFederationHelper(httpClient, Logger, timeseriesConfig);

                    // Fetch and verify all sensors
                    var federationResult = await federationHelper.FetchAndVerifySensorsAsync(baseQuery, cancellationToken);
                    if (federationResult.VerifiedIds.Count == 0)
                    {
                        return Ok(new { types = Array.Empty<object>(), total = 0 });
                    }

                    verifiedIds = federationResult.VerifiedIds;
                }
                else
                {
                    // No timeseries filtering - get all sensor IDs
                    verifiedIds = (await baseQuery
                        .Clone()
                        .Select("id")
                        .GetAsync<string>(cancellationToken: cancellationToken)).ToList();
                }

                if (verifiedIds.Count == 0)
                {
                    return Ok(new { types = Array.Empty<object>(), total = 0 });
                }

                // Call timeseries API to get distinct types
                var federationHelper2 = new TimeseriesFederationHelper(httpClient, Logger,
                    timeseriesConfig ?? new TimeseriesFilterConfig
                    {
                        TimeseriesApiBaseUrl = settings.TimeseriesConfig.ServiceUrl,
                        Enabled = true
                    });

                var typesResponse = await federationHelper2.GetSensorTypesAsync(verifiedIds, distinct: true, cancellationToken);

                if (typesResponse == null || typesResponse.Types == null)
                {
                    return Ok(new { types = Array.Empty<object>(), total = 0 });
                }

                return Ok(new { types = typesResponse.Types, total = typesResponse.Total });
            });
        }

        private Task<IActionResult> GetPaginatedTypesWithSensors(
            string[] fields,
            string? language,
            uint pagenumber,
            int pagesize,
            string? seed,
            string? searchfilter,
            string? rawfilter,
            string? rawsort,
            bool removenullvalues,
            string? sensortype,
            string? manufacturer,
            string? model,
            string? datasetid,
            string? measurementtypename,
            string? source,
            string? idlist,
            string? langfilter,
            string? odhtagfilter,
            string? publishedon,
            LegacyBool active,
            LegacyBool odhactive,
            GeoPolygonSearchResult? polygonsearchresult,
            PGGeoSearchResult geosearchresult,
            DateTime? updatefrom,
            string? tsdatasetids,
            string? tsrequiredtypes,
            string? tsoptionaltypes,
            string? tsmeasurementexpr,
            bool? tslatestonly,
            string? tsstarttime,
            string? tsendtime,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                SensorHelper myhelper = SensorHelper.Create(
                    QueryFactory,
                    idfilter: idlist,
                    sensortypefilter: sensortype,
                    manufacturerfilter: manufacturer,
                    modelfilter: model,
                    datasetidfilter: datasetid,
                    measurementtypenamefilter: measurementtypename,
                    sourcefilter: source,
                    activefilter: active?.Value,
                    smgactivefilter: odhactive?.Value,
                    smgtags: odhtagfilter,
                    lastchange: updatefrom?.ToString("yyyy-MM-dd"),
                    langfilter: langfilter,
                    publishedonfilter: publishedon,
                    cancellationToken
                );

                // Build base query (without Select, without pagination)
                var baseQuery = QueryFactory
                    .Query()
                    .From("sensors")
                    .SensorWhereExpression(
                        idlist: myhelper.idlist,
                        sensortypelist: myhelper.sensortypelist,
                        manufacturerlist: myhelper.manufacturerlist,
                        modellist: myhelper.modellist,
                        datasetidlist: myhelper.datasetidlist,
                        measurementtypenamelist: myhelper.measurementtypenamelist,
                        smgtaglist: myhelper.smgtaglist,
                        sourcelist: myhelper.sourcelist,
                        languagelist: myhelper.languagelist,
                        publishedonlist: myhelper.publishedonlist,
                        active: myhelper.active,
                        smgactive: myhelper.smgactive,
                        searchfilter: searchfilter,
                        language: language,
                        lastchange: myhelper.lastchange,
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

                // Check if timeseries filtering should be applied
                var timeseriesConfig = TimeseriesFederationExtensions.ParseTimeseriesConfig(
                    settings.TimeseriesConfig.ServiceUrl, tsdatasetids, tsrequiredtypes, tsoptionaltypes,
                    tsmeasurementexpr, tslatestonly, tsstarttime, tsendtime,
                    settings.TimeseriesConfig.FetchBatchSize, Logger);

                List<string> verifiedIds;

                if (timeseriesConfig != null && timeseriesConfig.Enabled)
                {
                    // Apply timeseries federation
                    var federationHelper = new TimeseriesFederationHelper(httpClient, Logger, timeseriesConfig);

                    // Fetch and verify (FetchAndVerifySensorsAsync handles pagination internally)
                    var federationResult = await federationHelper.FetchAndVerifySensorsAsync(baseQuery, cancellationToken);
                    if (federationResult.VerifiedIds.Count == 0)
                    {
                        return ResponseHelpers.GetResult(pagenumber, 0, 0, seed,
                            Array.Empty<object>(), Url);
                    }

                    verifiedIds = federationResult.VerifiedIds;

                    // Apply verified IDs filter to base query
                    baseQuery = baseQuery
                        .Clone()
                        .SelectRaw("data")
                        .WhereIn("id", verifiedIds);
                }
                else
                {
                    baseQuery = baseQuery
                        .Clone()
                        .SelectRaw("data");
                }

                // Get paginated data
                var data = await baseQuery.PaginateAsync<JsonRaw>(
                    page: (int)pagenumber,
                    perPage: pagesize
                );

                if (!data.List.Any())
                {
                    return ResponseHelpers.GetResult(pagenumber, 0, 0, seed,
                        Array.Empty<object>(), Url);
                }

                // Extract sensor IDs from paginated results for types lookup
                var paginatedSensorIds = data.List.Select(raw =>
                {
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(raw.Value);
                    if (jsonDoc.RootElement.TryGetProperty("Id", out var idProp))
                    {
                        return idProp.GetString() ?? "";
                    }
                    return "";
                }).Where(id => !string.IsNullOrEmpty(id)).ToList();

                // Call timeseries API to get types for paginated sensors
                var federationHelper2 = new TimeseriesFederationHelper(httpClient, Logger,
                    timeseriesConfig ?? new TimeseriesFilterConfig
                    {
                        TimeseriesApiBaseUrl = settings.TimeseriesConfig.ServiceUrl,
                        Enabled = true
                    });

                var typesResponse = await federationHelper2.GetSensorTypesAsync(paginatedSensorIds, distinct: false, cancellationToken);

                // Build types lookup dictionary
                var typesBySensorName = new Dictionary<string, object>();
                if (typesResponse != null && typesResponse.Sensors != null)
                {
                    foreach (var sensor in typesResponse.Sensors)
                    {
                        typesBySensorName[sensor.SensorName] = sensor.Types;
                    }
                }

                // Transform sensor data and merge types
                var sensorsTransformed = data.List.Select(raw =>
                {
                    // Parse the JSON to inject types
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(raw.Value);
                    var sensorId = jsonDoc.RootElement.TryGetProperty("Id", out var idProp)
                        ? idProp.GetString() ?? ""
                        : "";

                    // Create a mutable dictionary from the JSON
                    var sensorDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                        raw.Value,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    // Add timeseries types if available
                    if (sensorDict != null && !string.IsNullOrEmpty(sensorId))
                    {
                        if (typesBySensorName.TryGetValue(sensorId, out var types))
                        {
                            sensorDict["TimeseriesTypes"] = types;
                        }
                        else
                        {
                            sensorDict["TimeseriesTypes"] = Array.Empty<object>();
                        }

                        // Re-create JsonRaw with modified JSON
                        var modifiedJson = System.Text.Json.JsonSerializer.Serialize(sensorDict);
                        var modifiedRaw = new JsonRaw(modifiedJson);

                        // Apply transformations
                        return modifiedRaw.TransformRawData(
                            language,
                            fields,
                            filteroutNullValues: removenullvalues,
                            urlGenerator: UrlGenerator,
                            fieldstohide: null
                        );
                    }

                    // Fallback: transform without types
                    return raw.TransformRawData(
                        language,
                        fields,
                        filteroutNullValues: removenullvalues,
                        urlGenerator: UrlGenerator,
                        fieldstohide: null
                    );
                }).ToList();

                uint totalpages = (uint)data.TotalPages;
                uint totalcount = (uint)data.Count;

                return ResponseHelpers.GetResult(pagenumber, totalpages, totalcount,
                    seed, sensorsTransformed, Url);
            });
        }

        #endregion
    }
}
