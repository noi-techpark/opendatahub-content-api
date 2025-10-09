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

        public SensorController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<SensorController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier)
        {
            this.settings = settings;
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
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed)</param>
        /// <param name="searchfilter">String to search for, searches in sensor name, type, and details, (default: null)</param>
        /// <param name="rawfilter">Raw filter for advanced querying</param>
        /// <param name="rawsort">Raw sort for advanced sorting</param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false.</param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)</param>
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
            DateTime? updatefrom = null,
            string? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var getheader = HttpContext.Request.Headers["fields"].FirstOrDefault();
                if (getheader != null && !String.IsNullOrEmpty(getheader))
                    fields = getheader;

                return await GetFiltered(
                    fields: fields ?? "",
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
                    latitude: latitude,
                    longitude: longitude,
                    radius: radius,
                    updatefrom: updatefrom,
                    getasidarray: getasidarray,
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
            string? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var getheader = HttpContext.Request.Headers["fields"].FirstOrDefault();
                if (getheader != null && !String.IsNullOrEmpty(getheader))
                    fields = getheader;

                return await GetSingle(id, language, fields, removenullvalues, cancellationToken);
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
            string fields,
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
            string? latitude,
            string? longitude,
            string? radius,
            DateTime? updatefrom,
            bool getasidarray,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(latitude, longitude, radius);

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

                var query = QueryFactory
                    .Query()
                    .When(getasidarray, x => x.Select("id"))
                    .When(!getasidarray, x => x.SelectRaw("data"))
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
                    .ApplyOrdering(ref seed, geosearchresult, rawsort);

                //IF getasidarray set simply return array of ids
                if(getasidarray)
                {
                    return await query.GetAsync<string>();
                }

                // Get paginated data
                var data = await query.PaginateAsync<JsonRaw>(
                    page: (int)pagenumber,
                    perPage: pagesize
                );

                var dataTransformed = data.List.Select(raw =>
                    raw.TransformRawData(
                        language,
                        fields?.Split(',') ?? Array.Empty<string>(),
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

        private Task<IActionResult> GetSingle(string id, string? language, string? fields, bool removenullvalues, CancellationToken cancellationToken)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                var query = QueryFactory
                    .Query("sensors")
                    .Select("data")
                    .Where("id", id.ToUpper())
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
                    fields?.Split(',') ?? Array.Empty<string>(),
                    filteroutNullValues: removenullvalues,
                    urlGenerator: UrlGenerator,
                    fieldstohide: null
                );

                // Return standard JSON format
                return Ok(result);
            });
        }

        #endregion
    }
}
