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
    /// Traffic Incident Api - Traffic incidents with geometric shapes
    /// </summary>
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class TrafficIncidentController : OdhController
    {
        private readonly ISettings settings;

        public TrafficIncidentController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<TrafficIncidentController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier)
        {
            this.settings = settings;
        }

        #region SWAGGER Exposed API

        /// <summary>
        /// GET Traffic Incident List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null') </param>
        /// <param name="incidenttype">Type of traffic incident (e.g., 'accident', 'construction', 'weather', 'closure'), (default:'null')</param>
        /// <param name="severity">Severity level filter ('Low', 'Medium', 'High', 'Critical'), (default:'null')</param>
        /// <param name="status">Current status filter ('Active', 'Resolved', 'Investigating'), (default:'null')</param>
        /// <param name="roadclosure">Filter by road closure status (true/false), (default:'null')</param>
        /// <param name="affectedroutes">Filter by affected routes (comma-separated list), (default:'null')</param>
        /// <param name="source">Source Filter (possible Values: 'null' Displays all Traffic Incidents), (default:'null')</param>
        /// <param name="idlist">IDFilter (Separator ',' List of Traffic Incident IDs), (default:'null')</param>
        /// <param name="langfilter">Language filter (returns only Traffic Incidents available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="active">Active Traffic Incident Filter (possible Values: 'true' only active Traffic Incidents, 'false' only not active Traffic Incidents), (default:'null')</param>
        /// <param name="odhactive">ODH Active (Published) Traffic Incident Filter (Refers to field SmgActive) (possible Values: 'true' only published Traffic Incidents, 'false' only not published Traffic Incidents), (default:'null')</param>
        /// <param name="startdatefrom">Filter by start time from (Format: yyyy-MM-dd), (default:'null')</param>
        /// <param name="startdateto">Filter by start time to (Format: yyyy-MM-dd), (default:'null')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="polygon">Polygon filter for geometry intersection. valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed)</param>
        /// <param name="searchfilter">String to search for, searches in incident type, status, and details, (default: null)</param>
        /// <param name="rawfilter">Raw filter for advanced querying</param>
        /// <param name="rawsort">Raw sort for advanced sorting</param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false.</param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)</param>
        /// <returns>Collection of Traffic Incident Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<TrafficIncidentLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [TypeFilter(typeof(Filters.RequestInterceptorAttribute))]
        [HttpGet, Route("TrafficIncident")]
        [Produces("application/json", "application/geo+json")]
        public async Task<IActionResult> GetTrafficIncidentList(
            string? language = null,
            uint pagenumber = 1,
            int pagesize = 10,
            string? seed = null,
            string? incidenttype = null,
            string? severity = null,
            string? status = null,
            LegacyBool roadclosure = null!,
            string? affectedroutes = null,
            string? source = null,
            string? idlist = null,
            string? langfilter = null,
            string? odhtagfilter = null,
            string? publishedon = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            DateTime? startdatefrom = null,
            DateTime? startdateto = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
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
                    incidenttype: incidenttype, 
                    severity: severity, 
                    status: status, 
                    roadclosure: roadclosure, 
                    affectedroutes: affectedroutes,
                    source: source, 
                    idlist: idlist, 
                    langfilter: langfilter, 
                    odhtagfilter: odhtagfilter,
                    publishedon: publishedon,
                    active: active, 
                    odhactive: odhactive,
                    startdatefrom: startdatefrom, 
                    startdateto: startdateto, 
                    latitude: latitude,
                    longitude: longitude,
                    radius: radius,
                    polygon: polygon, 
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
        /// GET Traffic Incident Single
        /// </summary>
        /// <param name="id">ID of the Traffic Incident</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed)</param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false.</param>
        /// <returns>Traffic Incident Object</returns>
        /// <response code="200">Traffic Incident found</response>
        /// <response code="400">Request Error</response>
        /// <response code="404">Traffic Incident not found</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(TrafficIncidentLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("TrafficIncident/{id}", Name = "SingleTrafficIncident")]
        [Produces("application/json", "application/geo+json")]
        public async Task<IActionResult> GetTrafficIncident(
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
        /// POST Insert new Traffic Incident
        /// </summary>
        /// <param name="trafficincident">Traffic Incident Object</param>
        /// <returns>Http Response</returns>
        [AuthorizeODH(PermissionAction.Create)]
        [InvalidateCacheOutput(nameof(GetTrafficIncidentList))]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost, Route("TrafficIncident")]
        public async Task<IActionResult> Post([FromBody] TrafficIncidentLinked trafficincident)
        {
            AdditionalFiltersToAdd.TryGetValue("Create", out var additionalfilter);

            try
            {
                if (trafficincident != null)
                {
                    if (String.IsNullOrEmpty(trafficincident.Source))
                        trafficincident.Source = "api";

                    trafficincident.Active = true;
                    trafficincident.FirstImport = DateTime.Now;
                    trafficincident.LastChange = DateTime.Now;

                    if (trafficincident._Meta == null)
                        trafficincident._Meta = new Metadata() { Type = "trafficincident", LastUpdate = DateTime.Now, Source = trafficincident.Source };
                    
                    trafficincident._Meta.LastUpdate = DateTime.Now;

                    if (trafficincident.Geometry == null)
                        throw new Exception("Geometry is required for Traffic Incidents");

                    return await UpsertData<TrafficIncidentLinked>(
                        trafficincident,
                        new DataInfo("trafficincidents", CRUDOperation.Create),
                        new CompareConfig(false, false),
                        new CRUDConstraints(additionalfilter, UserRolesToFilter)
                    );
                }
                else
                {
                    return BadRequest("No Traffic Incident data provided");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// PUT Modify existing Traffic Incident
        /// </summary>
        /// <param name="id">Traffic Incident Id</param>
        /// <param name="trafficincident">Traffic Incident Object</param>
        /// <returns>Http Response</returns>
        [AuthorizeODH(PermissionAction.Update)]
        [InvalidateCacheOutput(nameof(GetTrafficIncidentList))]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut, Route("TrafficIncident/{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] TrafficIncidentLinked trafficincident)
        {
            AdditionalFiltersToAdd.TryGetValue("Update", out var additionalfilter);

            try
            {
                if (trafficincident != null)
                {
                    trafficincident.Id = id;
                    trafficincident.LastChange = DateTime.Now;

                    if (trafficincident._Meta == null)
                        trafficincident._Meta = new Metadata() { Type = "trafficincident", LastUpdate = DateTime.Now, Source = trafficincident.Source };
                    
                    trafficincident._Meta.LastUpdate = DateTime.Now;

                    if (trafficincident.Geometry == null)
                        throw new Exception("Geometry is required for Traffic Incidents");

                    return await UpsertData<TrafficIncidentLinked>(
                        trafficincident,
                        new DataInfo("trafficincidents", CRUDOperation.Update, true),
                        new CompareConfig(false, false),
                        new CRUDConstraints(additionalfilter, UserRolesToFilter)
                    );
                }
                else
                {
                    return BadRequest("No Traffic Incident data provided");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// DELETE Traffic Incident by ID
        /// </summary>
        /// <param name="id">Traffic Incident Id</param>
        /// <returns>Http Response</returns>
        [AuthorizeODH(PermissionAction.Delete)]
        [InvalidateCacheOutput(nameof(GetTrafficIncidentList))]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpDelete, Route("TrafficIncident/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            AdditionalFiltersToAdd.TryGetValue("Delete", out var additionalfilter);

            try
            {
                return await DeleteData<TrafficIncidentLinked>(
                    id,
                    new DataInfo("trafficincidents", CRUDOperation.Delete),
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
            string? incidenttype, 
            string? severity, 
            string? status, 
            LegacyBool roadclosure, 
            string? affectedroutes,
            string? source, 
            string? idlist, 
            string? langfilter, 
            string? odhtagfilter,
            string? publishedon,
            LegacyBool active, 
            LegacyBool odhactive,
            DateTime? startdatefrom, 
            DateTime? startdateto,
            string? latitude,
            string? longitude,
            string? radius, 
            string? polygon, 
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
                var polygonsearchresult = await Helper.GeoSearchHelper.GetPolygon(polygon, QueryFactory);

                TrafficIncidentHelper myhelper = TrafficIncidentHelper.Create(
                    QueryFactory,
                    idfilter: idlist,
                    incidenttypefilter: incidenttype,
                    severityfilter: severity,
                    statusfilter: status,
                    affectedroutesfilter: affectedroutes,
                    roadclosurefilter: roadclosure?.Value,
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
                    .From("trafficincidents")
                    .TrafficIncidentWhereExpression(
                        idlist: myhelper.idlist,
                        incidenttypelist: myhelper.incidenttypelist,
                        severitylist: myhelper.severitylist,
                        statuslist: myhelper.statuslist,
                        affectedrouteslist: myhelper.affectedrouteslist,
                        smgtaglist: myhelper.smgtaglist,
                        sourcelist: myhelper.sourcelist,
                        languagelist: myhelper.languagelist,
                        publishedonlist: myhelper.publishedonlist,
                        roadclosure: myhelper.roadclosure,
                        active: myhelper.active,
                        smgactive: myhelper.smgactive,
                        searchfilter: searchfilter,
                        language: language,
                        lastchange: myhelper.lastchange,
                        startdatefrom: startdatefrom,
                        startdateto: startdateto,
                        additionalfilter: additionalfilter,
                        userroles: UserRolesToFilter
                    )
                    .ApplyRawFilter(rawfilter)
                    .When(
                        polygonsearchresult != null,
                        x =>
                            x.WhereRaw(
                                PostgresSQLHelper.GetGeoWhereInPolygon_GeneratedColumns(
                                    polygonsearchresult!.wktstring,
                                    polygonsearchresult.polygon,
                                    polygonsearchresult.srid,
                                    polygonsearchresult.operation,
                                    polygonsearchresult.reduceprecision,
                                    "gen_geometry"
                                )
                            )
                    )
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

                // Check if client wants GeoJSON
                var acceptHeader = HttpContext.Request.Headers["Accept"].FirstOrDefault();
                var isGeoJsonRequest = acceptHeader?.Contains("application/geo+json") == true;

                if (isGeoJsonRequest)
                {
                    // Transform data to GeoJSON format
                    var geoJsonFeatures = dataTransformed.Select(item => new
                    {
                        type = "Feature",
                        geometry = ((dynamic)item)?.Geometry,
                        properties = item
                    });

                    var geoJsonCollection = new
                    {
                        type = "FeatureCollection",
                        features = geoJsonFeatures.ToArray()
                    };

                    return Ok(geoJsonCollection);
                }
                else
                {
                    return ResponseHelpers.GetResult(
                        pagenumber,
                        totalpages,
                        totalcount,
                        seed,
                        dataTransformed,
                        Url
                    );
                }
            });
        }

        private Task<IActionResult> GetSingle(string id, string? language, string? fields, bool removenullvalues, CancellationToken cancellationToken)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                var query = QueryFactory
                    .Query("trafficincidents")
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

                // Check if client wants GeoJSON
                var acceptHeader = HttpContext.Request.Headers["Accept"].FirstOrDefault();
                var isGeoJsonRequest = acceptHeader?.Contains("application/geo+json") == true;

                if (isGeoJsonRequest)
                {
                    // Return GeoJSON format
                    var geoJsonFeature = new
                    {
                        type = "Feature",
                        geometry = ((dynamic)result)?.Geometry,
                        properties = result
                    };
                    
                    return Ok(geoJsonFeature);
                }
                else
                {
                    // Return standard JSON format
                    return Ok(result);
                }
            });
        }

        #endregion
    }
}