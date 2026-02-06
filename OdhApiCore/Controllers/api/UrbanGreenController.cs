// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Generic;
using Helper.Identity;
using Helper.Tagging;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OdhApiCore.Controllers.api;
using OdhApiCore.Responses;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace OdhApiCore.Controllers
{
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class UrbanGreenController : OdhController
    {
        public UrbanGreenController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<UrbanGreenController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        #region SWAGGER Exposed API

        /// <summary>
        /// GET UrbanGreen List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Langfilter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="idlist">IDFilter (Separator ',' List of IDs, 'null' = No Filter), (default:'null')</param>
        /// <param name="source">Source Filter, (default:'null')</param>
        /// <param name="greencode">GreenCode Filter (Separator ',' List of GreenCodes), (default:'null')</param>
        /// <param name="greencodeversion">GreenCodeVersion Filter (Separator ',' List of GreenCodeVersions), (default:'null')</param>
        /// <param name="greencodetype">GreenCodeType Filter (Separator ',' List of GreenCodeTypes), (default:'null')</param>
        /// <param name="greencodesubtype">GreenCodeSubtype Filter (Separator ',' List of GreenCodeSubtypes), (default:'null')</param>
        /// <param name="activefilter">Active Filter (possible values: 'true' only Active data, 'false' only Disabled data), (default:'null')</param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="tagfilter">Filter on Tags. Syntax =and/or(TagId,TagId,TagId) example or(urbangreen:tree,urbangreen:bush) - Combining and/or is not supported at the moment. (default: 'null')</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of UrbanGreen Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<UrbanGreen>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("UrbanGreen")]
        public async Task<IActionResult> Get(
            uint? pagenumber = 1,
            PageSize pagesize = null!,
            string? language = null,
            string? langfilter = null,
            string? idlist = null,
            string? source = null,
            string? greencode = null,
            string? greencodeversion = null,
            string? greencodetype = null,
            string? greencodesubtype = null,
            LegacyBool activefilter = null!,
            string? tagfilter = null,
            string? seed = null,
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
            var polygonsearchresult = await Helper.GeoSearchHelper.GetPolygon(
                polygon,
                QueryFactory
            );

            return await GetList(
                pagenumber,
                pagesize,
                language,
                langfilter,
                idlist,
                source,
                greencode,
                greencodeversion,
                greencodetype,
                greencodesubtype,
                activefilter?.Value,
                tagfilter,
                seed,
                polygonsearchresult,
                fields: fields ?? Array.Empty<string>(),
                searchfilter,
                rawfilter,
                rawsort,
                removenullvalues: removenullvalues,
                getasidarray: getasidarray,
                cancellationToken
            );
        }

        /// <summary>
        /// GET UrbanGreen Single
        /// </summary>
        /// <param name="id">ID of the UrbanGreen</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>UrbanGreen Object</returns>
        /// <response code="200">Object created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(UrbanGreen), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("UrbanGreen/{id}", Name = "SingleUrbanGreen")]
        public async Task<IActionResult> GetUrbanGreenSingle(
            string id,
            string? language = null,
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

        #endregion

        #region GETTER

        private Task<IActionResult> GetList(
            uint? pagenumber,
            int? pagesize,
            string? language,
            string? languagefilter,
            string? idfilter,
            string? source,
            string? greencode,
            string? greencodeversion,
            string? greencodetype,
            string? greencodesubtype,
            bool? activefilter,
            string? tagfilter,
            string? seed,
            GeoPolygonSearchResult? polygonsearchresult,
            string[] fields,
            string? searchfilter,
            string? rawfilter,
            string? rawsort,
            bool removenullvalues,
            bool getasidarray,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {

                var sw = Stopwatch.StartNew();
                long lastCheckpoint = 0;
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                UrbanGreenHelper helper =
                            await UrbanGreenHelper.CreateAsync(
                                queryFactory: QueryFactory,
                                idfilter: idfilter,
                                languagefilter: languagefilter,
                                sourcefilter: source,
                                greencodefilter: greencode,
                                greencodeversionfilter: greencodeversion,
                                greencodetypefilter: greencodetype,
                                greencodesubtypefilter: greencodesubtype,
                                activefilter: activefilter,
                                tagfilter: tagfilter,
                                cancellationToken
                            );

                Console.WriteLine($"Query helper tool: {sw.ElapsedMilliseconds}ms");
                lastCheckpoint = sw.ElapsedMilliseconds;

                var query = QueryFactory
                    .Query()
                    .When(getasidarray, x => x.Select("id"))
                    .When(!getasidarray, x => x.SelectRaw("data"))
                    .From("urbangreens")
                    .UrbanGreenWhereExpression(
                        languagelist: helper.languagelist,
                        idlist: helper.idlist,
                        sourcelist: helper.sourcelist,
                        greencodelist: helper.greencodelist,
                        greencodeversionlist: helper.greencodeversionlist,
                        greencodetypelist: helper.greencodetypelist,
                        greencodesubtypelist: helper.greencodesubtypelist,
                        activefilter: helper.activefilter,
                        searchfilter: searchfilter,
                        language: language,
                        tagdict: helper.tagdict,
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
                                    polygonsearchresult.reduceprecision,
                                    "geo"
                                )
                            )
                    )
                    .ApplyRawFilter(rawfilter)
                    .ApplyOrdering_GeneratedColumns(ref seed, new PGGeoSearchResult() { geosearch = false }, rawsort);

                long step2Time = sw.ElapsedMilliseconds - lastCheckpoint;
                Console.WriteLine($"Query build took: {step2Time}ms");
                lastCheckpoint = sw.ElapsedMilliseconds;
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

                long step3Time = sw.ElapsedMilliseconds - lastCheckpoint;
                Console.WriteLine($"Query took: {step3Time}ms");
                lastCheckpoint = sw.ElapsedMilliseconds;

                var dataTransformed = data.List.Select(raw =>
                    raw.TransformRawData(
                        language,
                        fields,
                        filteroutNullValues: removenullvalues,
                        urlGenerator: UrlGenerator,
                        fieldstohide: null
                    )
                );

                long step4Time = sw.ElapsedMilliseconds - lastCheckpoint;
                Console.WriteLine($"Transform took: {step4Time}ms");
                lastCheckpoint = sw.ElapsedMilliseconds;
                sw.Stop();

                uint totalpages = (uint)data.TotalPages;
                uint totalcount = (uint)data.Count;

                return ResponseHelpers.GetResult(
                    (uint)pagenumber,
                    totalpages,
                    totalcount,
                    null,
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

                var data = await QueryFactory
                    .Query("urbangreens")
                    .Select("data")
                    .Where("id", id.ToLower())
                    .When(
                        !String.IsNullOrEmpty(additionalfilter),
                        q => q.FilterAdditionalDataByCondition(additionalfilter)
                    )
                    .FilterDataByAccessRoles(UserRolesToFilter)
                    .FirstOrDefaultAsync<JsonRaw>();

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
        /// POST Insert new UrbanGreen
        /// </summary>
        /// <param name="urbangreen">UrbanGreen Object</param>
        /// <returns>Http Response</returns>
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [AuthorizeODH(PermissionAction.Create)]
        [HttpPost, Route("UrbanGreen")]
        public Task<IActionResult> Post([FromBody] UrbanGreen urbangreen)
        {
            return DoAsyncReturn(async () =>
            {
                if (!urbangreen.Geo.GeoInfoIsValid())
                {
                    return BadRequest(new { error = "Exactly one default GeoInfo must be present" });
                }
                foreach (var kvp in urbangreen.Geo)
                {
                    if (!kvp.Value.IsValidGeometry)
                    {
                        return BadRequest(new { error = $"Geo Info <{kvp.Key}> is invalid" });
                    }
                }

                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Create", out var createFilter);

                urbangreen.Id = Helper.IdGenerator.GenerateIDFromType(urbangreen);

                if (urbangreen.LicenseInfo == null)
                    urbangreen.LicenseInfo = new LicenseInfo() { ClosedData = false };

                //TRIM all strings
                urbangreen.TrimStringProperties();

                return await UpsertData<UrbanGreen>(
                    new UpsertableUrbanGreen(urbangreen),
                    new DataInfo("urbangreens", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(createFilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Upsert array of UrbanGreens with well known ids
        /// </summary>
        /// <param name="urbangreens">List of UrbanGreen Objects</param>
        /// <returns>Http Response with batch results</returns>
        [ProducesResponseType(typeof(BatchCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [AuthorizeODH(new[] { PermissionAction.Create, PermissionAction.Update })]
        [HttpPut, Route("UrbanGreen")]
        public Task<IActionResult> Put(List<UrbanGreen> urbangreens)
        {
            return DoAsync(async () =>
            {
                if (urbangreens == null || urbangreens.Count == 0)
                {
                    ModelState.AddModelError("urbangreens", "No urbangreens provided");
                    return ValidationProblem(ModelState);
                }

                // Get both Create and Update filters for batch operation
                AdditionalFiltersToAdd.TryGetValue("Create", out var createFilter);
                AdditionalFiltersToAdd.TryGetValue("Update", out var updateFilter);

                // Validate all urbangreens and collect errors
                for (int i = 0; i < urbangreens.Count; i++)
                {
                    var urbangreen = urbangreens[i];
                    if (!urbangreen.Geo.GeoInfoIsValid())
                    {
                        ModelState.AddModelError($"[{i}].Geo", "Exactly one default GeoInfo must be present");
                    }

                    foreach (var kv in urbangreen.Geo)
                    {
                        if (!kv.Value.IsValidGeometry)
                        {
                            ModelState.AddModelError($"[{i}].Geo[{kv.Key}].Geometry", "Invalid WKT geometry");
                        }
                    }

                    if (urbangreen.Id == null)
                        ModelState.AddModelError($"[{i}].Id", "Id is required");

                    if (urbangreen.LicenseInfo == null)
                        urbangreen.LicenseInfo = new LicenseInfo() { ClosedData = false };
                }

                // If there are validation errors, return them in standard format
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                // Trim all strings for all urbangreens
                foreach (var urbangreen in urbangreens)
                {
                    urbangreen.TrimStringProperties();
                }

                return await UpsertDataArray<UrbanGreen>(
                    urbangreens.Select(a => new UpsertableUrbanGreen(a)),
                    new DataInfo("urbangreens", CRUDOperation.CreateAndUpdate, true),
                    new CompareConfig(true, false), // Enable comparison to detect unchanged
                    new CRUDConstraints(createFilter, UserRolesToFilter),
                    new CRUDConstraints(updateFilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing UrbanGreen
        /// </summary>
        /// <param name="id">UrbanGreen Id</param>
        /// <param name="urbangreen">UrbanGreen Object</param>
        /// <returns>Http Response</returns>
        [AuthorizeODH(PermissionAction.Update)]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut, Route("UrbanGreen/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] UrbanGreen urbangreen)
        {
            return DoAsyncReturn(async () =>
            {
                if (!urbangreen.Geo.GeoInfoIsValid())
                {
                    return BadRequest(new { error = "Exactly one default GeoInfo must be present" });
                }
                foreach (var kvp in urbangreen.Geo)
                {
                    if (!kvp.Value.IsValidGeometry)
                    {
                        return BadRequest(new { error = $"Geo Info <{kvp.Key}> is invalid" });
                    }
                }

                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Update", out var updateFilter);

                urbangreen.Id = Helper.IdGenerator.CheckIdFromType<UrbanGreen>(id);

                //TRIM all strings
                urbangreen.TrimStringProperties();

                return await UpsertData<UrbanGreen>(
                    new UpsertableUrbanGreen(urbangreen),
                    new DataInfo("urbangreens", CRUDOperation.Update, true),
                    new CompareConfig(true, false),
                    new CRUDConstraints(updateFilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE UrbanGreen by Id
        /// </summary>
        /// <param name="id">UrbanGreen Id</param>
        /// <returns>Http Response</returns>
        [AuthorizeODH(PermissionAction.Delete)]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpDelete, Route("UrbanGreen/{id}")]
        public Task<IActionResult> Delete(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<UrbanGreen>(id);

                return await DeleteData<UrbanGreen>(
                    id,
                    new DataInfo("urbangreens", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        #endregion
    }
}
