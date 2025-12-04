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
using Helper.Tagging;
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
    /// WebcamInfo Api (data provided by LTS ActivityData) SOME DATA Available as OPENDATA
    /// </summary>
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class WebcamInfoController : OdhController
    {
        public WebcamInfoController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<WebcamInfoController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        #region SWAGGER Exposed API

        /// <summary>
        /// GET Webcam List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null') </param>
        /// <param name="idlist">IDFilter (Separator ',' List of Gastronomy IDs), (default:'null')</param>
        /// <param name="source">Source Filter (Separator ',' available sources 'lts','content'), (default:'null')</param>
        /// <param name="active">Active Webcam Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')</param>
        /// <param name="odhactive">ODH Active (Published) Webcam Filter (possible Values: 'true' only published data, 'false' only not published data), (default:'null')</param>
        /// <param name="tagfilter">Filter on Tags. (Endpoint on v1/Tag) Syntax =and/or(Tag.Id,Tag.Id,Tag.Id) example or(summer,hiking) - and(themed hikes,family hikings) - or(hiking) - and(summer) - Combining and/or is not supported at the moment, default: 'null')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Id OR Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null)<a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param> /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of WebcamInfo Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<WebcamInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[OdhCacheOutput(ClientTimeSpan = 0, ServerTimeSpan = 3600, CacheKeyGenerator = typeof(CustomCacheKeyGenerator), MustRevalidate = true)]
        [HttpGet, Route("WebcamInfo")]
        public async Task<IActionResult> Get(
            string? language = null,
            uint pagenumber = 1,
            PageSize pagesize = null!,
            string? source = null,
            string? idlist = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? tagfilter = null,
            string? seed = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            string? updatefrom = null,
            string? publishedon = null,
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

            return await GetList(
                fields: fields ?? Array.Empty<string>(),
                language,
                pagenumber,
                pagesize,
                source,
                idlist,
                searchfilter,
                active?.Value,
                odhactive?.Value,
                tagfilter: tagfilter,
                publishedon,
                seed,
                updatefrom,
                polygonsearchresult: polygonsearchresult,
                geosearchresult,
                rawfilter: rawfilter,
                rawsort: rawsort,
                removenullvalues: removenullvalues,
                getasidarray: getasidarray,
                cancellationToken
            );
        }

        /// <summary>
        /// GET Webcam Single
        /// </summary>
        /// <param name="id">ID of the Webcam</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>WebcamInfo Object</returns>
        [ProducesResponseType(typeof(WebcamInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("WebcamInfo/{id}", Name = "SingleWebcamInfo")]
        public Task<IActionResult> GetWebcamSingle(
            string id,
            string? language = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return GetSingleAsync(
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
            string[] fields,
            string? language,
            uint pagenumber,
            int? pagesize,
            string? source,
            string? idfilter,
            string? searchfilter,
            bool? active,
            bool? smgactive,
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

                WebcamInfoHelper mywebcaminfohelper = WebcamInfoHelper.Create(
                    source,
                    idfilter,
                    tagfilter,
                    active,
                    smgactive,
                    lastchange,
                    publishedon
                );

                var query = QueryFactory
                    .Query()
                    .When(getasidarray, x => x.Select("id"))
                    .When(!getasidarray, x => x.SelectRaw("data"))
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
                    .ApplyOrdering_GeneratedColumns(ref seed, geosearchresult, rawsort); //.ApplyOrdering(ref seed, geosearchresult, rawsort);

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

        private Task<IActionResult> GetSingleAsync(
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
                    .Query("webcams")
                    .Select("data")
                    //.Where("id", id.ToUpper())
                    .Where("gen_id", id.ToUpper())
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

        #region POST PUT DELETE

        /// <summary>
        /// POST Insert new WebcamInfo
        /// </summary>
        /// <param name="webcam">WebcamInfo Object</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        [AuthorizeODH(PermissionAction.Create)]
        //[Authorize(Roles = "DataWriter,DataCreate,WebcamManager,WebcamCreate")]
        //[InvalidateCacheOutput(typeof(WebcamInfoController), nameof(Get))]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost, Route("WebcamInfo")]
        public Task<IActionResult> Post([FromBody] WebcamInfoLinked webcam)
        {
            //TODO IGNORE Fields
            //AreaIds, LicenseInfo, SmgTags, WebcamAssignedOn, Meta
            //TO check if this is needed
            webcam.LicenseInfo = LicenseHelper.GetLicenseforWebcam(webcam);

            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Create", out var additionalfilter);

                webcam.Id = Helper.IdGenerator.GenerateIDFromType(webcam);

                //GENERATE HasLanguage
                webcam.CheckMyInsertedLanguages(null);
                //TRIM all strings
                webcam.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await webcam.UpdateTagsExtension(QueryFactory);

                return await UpsertData<WebcamInfoLinked>(
                    webcam,
                    new DataInfo("webcams", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing WebcamInfo
        /// </summary>
        /// <param name="id">WebcamInfo Id</param>
        /// <param name="webcam">WebcamInfo Object</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        [AuthorizeODH(PermissionAction.Update)]
        //[Authorize(Roles = "DataWriter,DataModify,WebcamManager,WebcamModify,WebcamUpdate")]
        //[InvalidateCacheOutput(typeof(WebcamInfoController), nameof(Get))]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut, Route("WebcamInfo/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] WebcamInfoLinked webcam)
        {
            webcam.LicenseInfo = LicenseHelper.GetLicenseforWebcam(webcam);

            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Update", out var additionalfilter);

                webcam.Id = Helper.IdGenerator.CheckIdFromType<WebcamInfoLinked>(id);

                //GENERATE HasLanguage
                webcam.CheckMyInsertedLanguages(null);
                //TRIM all strings
                webcam.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await webcam.UpdateTagsExtension(QueryFactory);

                return await UpsertData<WebcamInfoLinked>(
                    webcam,
                    new DataInfo("webcams", CRUDOperation.Update, true),
                    new CompareConfig(true, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE WebcamInfo by Id
        /// </summary>
        /// <param name="id">WebcamInfo Id</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        [AuthorizeODH(PermissionAction.Delete)]
        //[Authorize(Roles = "DataWriter,DataDelete,WebcamManager,WebcamDelete")]
        //[InvalidateCacheOutput(typeof(WebcamInfoController), nameof(Get))]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpDelete, Route("WebcamInfo/{id}")]
        public Task<IActionResult> Delete(string id)
        {
            //TODO Invalidate Cache after each Operation!!

            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<WebcamInfoLinked>(id);

                return await DeleteData<WebcamInfoLinked>(
                    id,
                    new DataInfo("webcams", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        #endregion
    }
}
