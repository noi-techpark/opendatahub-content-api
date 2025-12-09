// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using Helper;
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
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class PushResponseController : OdhControllerWithSearch
    {
        public PushResponseController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<PushResponseController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        #region SWAGGER Exposed API

        /// <summary>
        /// GET PushResponse List
        /// </summary>
        /// <param name="idlist">IDFilter (Separator ',' List of IDs, 'null' = No Filter), (default:'null')</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="publisher">publisher Filter (Separator ',' List of IDs, 'null' = No Filter), (default:'null')</param>
        /// <param name="begindate">BeginDate of Events (Format: yyyy-MM-dd), (default: 'null')</param>
        /// <param name="enddate">EndDate of Events (Format: yyyy-MM-dd), (default: 'null')</param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of PushResponse Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(IEnumerable<PushResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("PushResponse")]
        public async Task<IActionResult> Get(
            uint? pagenumber = 1,
            PageSize pagesize = null!,
            string? idlist = null,
            string? publisher = null,
            string? begindate = null,
            string? enddate = null,
            string? objectidlist = null,
            string? objecttypelist = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetList(
                pagenumber,
                pagesize,
                idlist,
                publisher,
                begindate,
                enddate,
                objectidlist,
                objecttypelist,
                fields: fields ?? Array.Empty<string>(),
                rawfilter,
                rawsort,
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        /// <summary>
        /// GET PushResponse Single
        /// </summary>
        /// <param name="id">ID of the PushResponse</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>PushResponse Object</returns>
        /// <response code="200">Object created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(PushResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("PushResponse/{id}", Name = "SinglePushResponse")]
        public async Task<IActionResult> GetPushResultSingle(
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

        #endregion

        #region GETTER

        private Task<IActionResult> GetList(
            uint? pagenumber,
            int? pagesize,
            string? idfilter,
            string? publisherfilter,
            string? begindate,
            string? enddate,
            string? objectidfilter,
            string? objecttypefilter,
            string[] fields,
            string? rawfilter,
            string? rawsort,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                var idlist = Helper.CommonListCreator.CreateIdList(idfilter);
                var objectidlist = Helper.CommonListCreator.CreateIdList(objectidfilter);
                var objecttypelist = Helper.CommonListCreator.CreateIdList(objecttypefilter);

                var publisherlist = Helper.CommonListCreator.CreateIdList(publisherfilter);

                DateTime begin = DateTime.MinValue;
                DateTime end = DateTime.MaxValue;

                if (!String.IsNullOrEmpty(begindate))
                    if (begindate != "null")
                        begin = Convert.ToDateTime(begindate);

                if (!String.IsNullOrEmpty(enddate))
                    if (enddate != "null")
                        end = Convert.ToDateTime(enddate);

                var query = QueryFactory
                    .Query()
                    .SelectRaw("data")
                    .From("pushresults")
                    .PushResultWhereExpression(
                        idlist: idlist,
                        publisherlist: publisherlist,
                        begin: begin,
                        end: end,
                        objectidlist: objectidlist,
                        objecttypelist: objecttypelist,
                        additionalfilter: additionalfilter
                    )
                    .ApplyRawFilter(rawfilter)
                    .ApplyOrdering(
                        new PGGeoSearchResult() { geosearch = false },
                        rawsort,
                        "gen_lastchange DESC"
                    );

                // Get paginated data
                var data = await query.PaginateAsync<JsonRaw>(
                    page: (int)pagenumber,
                    perPage: pagesize ?? 25
                );

                var dataTransformed = data.List.Select(raw =>
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
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                var data = await QueryFactory
                    .Query("pushresults")
                    .Select("data")
                    .Where("id", id)
                    .When(
                        !String.IsNullOrEmpty(additionalfilter),
                        q => q.FilterAdditionalDataByCondition(additionalfilter)
                    )
                    .FirstOrDefaultAsync<JsonRaw>();

                return data?.TransformRawData(
                    null,
                    fields,
                    filteroutNullValues: removenullvalues,
                    urlGenerator: UrlGenerator,
                    fieldstohide: null
                );
            });
        }

        #endregion
    }
}
