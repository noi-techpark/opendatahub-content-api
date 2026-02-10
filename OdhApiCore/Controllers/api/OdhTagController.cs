// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

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
using Schema.NET;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiCore.Controllers
{
    //[Route("ODHTag")]
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class ODHTagController : OdhController
    {
        public ODHTagController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<ODHTagController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        #region SWAGGER Exposed API

        /// <summary>
        /// GET ODHTag List
        /// </summary>
        /// <param name="validforentity">Filter on Tags valid on Entities (accommodation, activity, poi, odhactivitypoi, package, gastronomy, event, article, common .. etc..)</param>
        /// <param name="mainentity">Filter on Tags with MainEntity set to (accommodation, activity, poi, odhactivitypoi, package, gastronomy, event, article, common .. etc..)</param>
        /// <param name="displayascategory">true = returns only Tags which are marked as DisplayAsCategory true</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="localizationlanguage">here for Compatibility Reasons, replaced by language parameter</param>
        /// <param name="source">Source Filter (possible Values: 'lts','idm'), (default:'null')</param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of ODHTag Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(IEnumerable<ODHTagLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[OdhCacheOutput(ClientTimeSpan = 0, ServerTimeSpan = 3600, CacheKeyGenerator = typeof(CustomCacheKeyGenerator))]
        [HttpGet, Route("ODHTag")]
        //[Authorize(Roles = "DataReader,CommonReader,AccoReader,ActivityReader,PoiReader,ODHPoiReader,PackageReader,GastroReader,EventReader,ArticleReader")]
        public async Task<IActionResult> Get(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? language = null,
            string? validforentity = null,
            string? mainentity = null,
            bool? displayascategory = null,
            string? source = null,
            string? publishedon = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            string? localizationlanguage = null, //TODO ignore this in swagger
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            //Compatibility
            if (String.IsNullOrEmpty(language) && !String.IsNullOrEmpty(localizationlanguage))
                language = localizationlanguage;

            return await GetList(
                pagenumber,
                pagesize,
                language,
                mainentity,
                validforentity,
                displayascategory,
                source,
                publishedon,
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
        /// GET ODHTag Single
        /// </summary>
        /// <param name="id">ID of the Odhtags</param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>ODHTag Object</returns>
        /// <response code="200">Object created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(ODHTagLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("ODHTag/{id}", Name = "SingleODHTag")]
        //[Authorize(Roles = "DataReader,CommonReader,AccoReader,ActivityReader,PoiReader,ODHPoiReader,PackageReader,GastroReader,EventReader,ArticleReader")]
        public async Task<IActionResult> GetODHTagSingle(
            string id,
            string? language = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? localizationlanguage = null, //TODO ignore this in swagger
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            //Compatibility
            if (String.IsNullOrEmpty(language) && !String.IsNullOrEmpty(localizationlanguage))
                language = localizationlanguage;

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
            string? maintype,
            string? validforentity,
            bool? displayascategory,
            string? source,
            string? publishedonfilter,
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
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                var validforentitytypeslist = (validforentity ?? "").Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries
                );
                var maintypeslist = (maintype ?? "").Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries
                );
                var sourcelist = Helper.CommonListCreator.CreateIdList(source);
                var publishedonlist = Helper.CommonListCreator.CreateIdList(
                    publishedonfilter?.ToLower()
                );

                var query = QueryFactory
                    .Query()
                    .When(getasidarray, x => x.Select("id"))
                    .When(!getasidarray, x => x.SelectRaw("data"))
                    .From("smgtags")
                    .ODHTagWhereExpression(
                        languagelist: new List<string>(),
                        mainentitylist: maintypeslist,
                        validforentitylist: validforentitytypeslist,
                        sourcelist: sourcelist,
                        publishedonlist: publishedonlist,
                        displayascategory: displayascategory,
                        searchfilter: searchfilter,
                        language: language,
                        additionalfilter: additionalfilter,
                        userroles: UserRolesToFilter
                    )
                    .ApplyRawFilter(rawfilter)
                    .ApplyOrdering(
                        new PGGeoSearchResult() { geosearch = false },
                        rawsort,
                        "data #>>'\\{MainEntity\\}', data#>>'\\{Shortname\\}'"
                    );

                if (pagenumber != null)
                {
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
                        (uint)pagenumber,
                        totalpages,
                        totalcount,
                        null,
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
                    .Query("smgtags")
                    .Select("data")
                    //.Where("id", id.ToLower())
                    .Where("id", "ILIKE", id)
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
        /// POST Insert new ODHTag
        /// </summary>
        /// <param name="odhtag">ODHTag Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [InvalidateCacheOutput(nameof(Get))]
        //[Authorize(Roles = "DataWriter,DataCreate,ODHTagManager,ODHTagCreate")]
        [AuthorizeODH(PermissionAction.Create)]
        [HttpPost, Route("ODHTag")]
        public Task<IActionResult> Post([FromBody] ODHTagLinked odhtag, bool generateid = true)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Create", out var additionalfilter);
                
                //Generate Id or use the assigned
                if (generateid)
                    odhtag.Id = Helper.IdGenerator.GenerateIDFromType(odhtag);
                else if (String.IsNullOrEmpty(odhtag.Id))
                    throw new Exception("Id is null");

                return await UpsertData<ODHTagLinked>(
                    odhtag,
                    new DataInfo("smgtags", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing ODHTag
        /// </summary>
        /// <param name="id">ODHTag Id</param>
        /// <param name="odhtag">ODHTag Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [InvalidateCacheOutput(nameof(Get))]
        //[Authorize(Roles = "DataWriter,DataModify,ODHTagManager,ODHTagModify,ODHTagUpdate")]
        [AuthorizeODH(PermissionAction.Update)]
        [HttpPut, Route("ODHTag/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] ODHTagLinked odhtag)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Update", out var additionalfilter);

                odhtag.Id = Helper.IdGenerator.CheckIdFromType<ODHTagLinked>(id);

                return await UpsertData<ODHTagLinked>(
                    odhtag,
                    new DataInfo("smgtags", CRUDOperation.Update, true),
                    new CompareConfig(true, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE ODHTag by Id
        /// </summary>
        /// <param name="id">ODHTag Id</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [InvalidateCacheOutput(nameof(Get))]
        //[Authorize(Roles = "DataWriter,DataDelete,ODHTagManager,ODHTagDelete")]
        [AuthorizeODH(PermissionAction.Delete)]
        [HttpDelete, Route("ODHTag/{id}")]
        public Task<IActionResult> Delete(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<ODHTagLinked>(id);

                return await DeleteData<ODHTagLinked>(
                    id,
                    new DataInfo("smgtags", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        #endregion
    }
}
