// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using DataModel.Annotations;
using Helper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OdhApiCore.Filters;
using OdhApiCore.Responses;
using OdhNotifier;
using ServiceReferenceLCS;
using SqlKata;
using SqlKata.Execution;

namespace OdhApiCore.Controllers.api
{
    //[ApiExplorerSettings(IgnoreApi = true)]
    public class RawChangesApiController : OdhController
    {
        public RawChangesApiController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<RawdataApiController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        /// <summary>
        /// GET Rawchanges Data List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="type">Type Filter (Separator ',' List of rawdata types, 'null' = No Filter), (default:'null')</param>
        /// <param name="source">Source Filter (Separator ',' List of rawdata sources, 'null' = No Filter), (default:'null')</param>
        /// <param name="sourceinterface">SourceInterface Filter (Separator ',' List of Sourceinterfaces, 'null' = No Filter), (default:'null')</param>
        /// <param name="sourceid">SourceIDFilter (Separator ',' List of Rawdata SourceIDs, 'null' = No Filter), (default:'null')</param>
        /// <param name="idlist">IDFilter (Separator ',' List of Rawchanges IDs, 'null' = No Filter), (default:'null')</param>
        /// <param name="latest">Get only latest data</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="rawfilter">Currently not working on rawdata<a href="https://github.com/noi-techpark/opendatahub-docs/wiki/Using-rawfilter-and-rawsort-on-the-Open-Data-Hub-Content-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort">Currently not working on rawdata<a href="https://github.com/noi-techpark/opendatahub-docs/wiki/Using-rawfilter-and-rawsort-on-the-Open-Data-Hub-Content-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of Raw Data Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(Result<RawChangesStoreJson>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet, Route("Rawchanges")]
        public async Task<IActionResult> GetRawchangesAsync(
            uint pagenumber = 1,
            PageSize pagesize = null!,
            string? type = null,
            string? source = null,
            string? sourceid = null,
            string? idlist = null,
            bool latest = true,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await Get(
                pagenumber,
                pagesize,
                type,
                source,
                idlist,
                sourceid,
                latest,
                fields: fields ?? Array.Empty<string>(),
                searchfilter,
                rawfilter,
                rawsort,
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        /// <summary>
        /// GET Rawchanges Single
        /// </summary>
        /// <param name="id">ID of rawchanges</param>        
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Rawdata Object</returns>
        /// <response code="200">Object created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(RawChangesStoreJson), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet, Route("Rawchanges/{id}", Name = "SingleRawchanges")]
        public async Task<IActionResult> GetRawchangesSingle(
            string id,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetSingle(
                id,                
                fields: fields ?? Array.Empty<string>(),
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        private Task<IActionResult> Get(
            uint pagenumber,
            int? pagesize,
            string type,
            string source,
            string ids,
            string sourceids,
            bool latest,
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
                var typelist = !String.IsNullOrEmpty(type) ? type.Split(",").ToList() : null;
                var sourcelist = !String.IsNullOrEmpty(source) ? source.Split(",").ToList() : null;
                var idlist = !String.IsNullOrEmpty(ids) ? ids.Split(",").ToList() : null;
                var sourceidlist = !String.IsNullOrEmpty(sourceids)
                    ? sourceids.Split(",").ToList()
                    : null;

                //Example latest records
                //select*
                //from rawdata
                //where id in (SELECT max(id)  FROM rawdata where type = 'ejob' group by sourceid)
                
                var query = QueryFactory
                    .Query()
                    .When(latest, q => q.SelectRaw("max(id)"))
                    .From("rawchanges")
                    .RawdataWhereExpression(
                        idlist,
                        sourceidlist,
                        typelist,
                        sourcelist,
                        additionalfilter: null,
                        userroles: UserRolesToFilter
                    )
                    .ApplyRawFilter(rawfilter)
                    .OrderOnlyByRawSortIfNotNull(rawsort)
                    .When(latest, q => q.GroupBy("sourceid"));

                //rawfilter and rawsort could be extended with a generated column "data" which only applies when rawtype is json

                if (latest)
                {
                    var query2 = QueryFactory.Query().From("rawchanges").WhereIn("id", query);

                    // Get paginated data
                    var data = await query2.PaginateAsync<RawChangesStoreWithId>(
                        page: (int)pagenumber,
                        perPage: pagesize ?? 25
                    );

                    var jsonrawdata = data
                        .List.Select(raw => new JsonRaw(raw.UseJsonRaw()))
                        .ToList();

                    var dataTransformed = jsonrawdata.Select(raw =>
                        raw.TransformRawData(
                            null,
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
                    // Get paginated data
                    var data = await query.PaginateAsync<RawChangesStoreWithId>(
                        page: (int)pagenumber,
                        perPage: pagesize ?? 25
                    );

                    var jsonrawdata = data
                        .List.Select(raw => new JsonRaw(raw.UseJsonRaw()))
                        .ToList();

                    var dataTransformed = jsonrawdata.Select(raw =>
                        raw.TransformRawData(
                            null,
                            fields,
                            filteroutNullValues: removenullvalues,
                            null,
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
            });
        }

        private Task<IActionResult> GetSingle(
            string id,
            string[] fields,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                if (Int32.TryParse(id, out var numericid))
                {
                    var query = QueryFactory.Query("rawchanges").Where("id", numericid)
                    .FilterDataByAccessRoles(UserRolesToFilter);
                    
                    var data = await query.FirstOrDefaultAsync<RawChangesStoreWithId?>();

                    if (data != null)
                        return new JsonRaw(data.UseJsonRaw()).TransformRawData(null, fields, filteroutNullValues: removenullvalues, null, fieldstohide: null);
                    else
                        return null;
                }
                else
                    return null;
            });
        }
    }
}
