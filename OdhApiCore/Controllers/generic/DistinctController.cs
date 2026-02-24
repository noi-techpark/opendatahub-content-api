// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    /// <summary>
    /// <a href="https://github.com/noi-techpark/odh-docs/wiki/Distinct-Search-in-ODH-Tourism-api" target="_blank">Wiki Distinct Api</a>
    /// </summary>
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class DistinctController : OdhController
    {
        public DistinctController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<DistinctController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        #region SWAGGER Exposed API

        /// <summary>
        /// GET Generic Distinct search of fields
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="type">Mandatory search trough Entities (metadata, accommodation, odhactivitypoi, event, webcam, measuringpoint, ltsactivity, ltspoi, ltsgastronomy, article ..... ) <a href="https://github.com/noi-techpark/odh-docs/wiki/Dataset-Type" target="_blank">Wiki Dataset Type</a></param>
        /// <param name="fields">Mandatory Select a field for the Distinct Query, example fields=Source, arrays are selected with a [*] example HasLanguage[*] / Features[*].Id  (Only one field supported). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/opendatahub-docs/wiki/Using-rawfilter-and-rawsort-on-the-Open-Data-Hub-Content-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/opendatahub-docs/wiki/Using-rawfilter-and-rawsort-on-the-Open-Data-Hub-Content-Api#rawfilter" target="_blank">Wiki rawsort</a></param>
        /// <param name="getasarray">Get only first selected field as simple string Array</param>
        /// <param name="excludenulloremptyvalues">Exclude empty and null values from output</param>
        /// <returns>Array of string/object</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[OdhCacheOutput(ClientTimeSpan = 0, ServerTimeSpan = 3600, CacheKeyGenerator = typeof(CustomCacheKeyGenerator), MustRevalidate = true)]
        [HttpGet, Route("Distinct")]
        //[Authorize(Roles = "DataReader,CommonReader,AccoReader,ActivityReader,PoiReader,ODHPoiReader,PackageReader,GastroReader,EventReader,ArticleReader")]
        public async Task<IActionResult> GetDistinctAsync(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? odhtype = null,
            string? type = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? seed = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool getasarray = false,
            bool excludenulloremptyvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            var fieldstodisplay = fields ?? Array.Empty<string>();

            if (fieldstodisplay.Count() == 0)
                return BadRequest("please add a field");

            if (fieldstodisplay.Count() > 1 && getasarray)
                return BadRequest("Get As Array only possible with 1 selected field");

            //let more non array fields pass, if arrays are selected then only one field is allowed
            if (
                fieldstodisplay.Count() > 1
                && (
                    fieldstodisplay.Any(x => x.Contains("[*]"))
                    || fieldstodisplay.Any(x => x.Contains("[]"))
                )
            )
                return BadRequest("On Array fields, currently only one field supported");

            return await GetDistinct(
                pagenumber,
                pagesize,
                odhtype ?? type,
                fieldstodisplay,
                seed,
                rawfilter,
                rawsort,
                getasarray,
                excludenulloremptyvalues,
                null,
                cancellationToken
            );
        }

        #endregion

        #region GETTER

        private Task<IActionResult> GetDistinct(
            uint? pagenumber,
            int? pagesize,
            string? odhtype,
            string[] fields,
            string? seed,
            string? rawfilter,
            string? rawsort,
            bool? getasarray,
            bool excludenullvalues,
            PGGeoSearchResult geosearchresult,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                if (odhtype == null)
                    return BadRequest("odhtype missing");

                var table = ODHTypeHelper.TranslateTypeString2Table(odhtype);

                string nullexclude = "";
                if (excludenullvalues)
                    nullexclude = " ? (@ <> null && @ <> \"\")";

                List<string> selects = new List<string>();

                var fieldschecked = JsonPathCompatibilitycheck(fields);

                foreach (var field in fieldschecked)
                {
                    string kataField = field.Item1.Replace("[", "\\[").Replace("]", "\\]");
                    string asField = field.Item2.Replace("[", "\\[").Replace("]", "\\]");

                    string select =
                        $@"jsonb_path_query(data, '$.{kataField}{nullexclude}')#>>'\{{\}}' as ""{asField}""";

                    selects.Add(select);
                }

                string endpoint = ODHTypeHelper.TranslateTypeToEndPoint(odhtype);

                var query = QueryFactory
                    .Query()
                    .Distinct()
                    .SelectRaw(String.Join(",", selects))
                    .From(table)
                    .ApplyRawFilter(rawfilter)
                    // .Anonymous_Logged_UserRule_GeneratedColumn(FilterClosedData, !ReducedData)
                    .OrderByRawIfNotNull(rawsort)
                    //.ApplyOrdering_GeneratedColumns(null, null, rawsort)
                    .FilterDataByAccessRoles(UserRolesToFilterEndpoint(endpoint));

                //TODOS Metadata api support

                if (getasarray.HasValue && getasarray.Value)
                {
                    //TODOS GetAsarray returns values simple
                    var data = await query.GetAsync<string>();
                    return data;
                }
                else
                {
                    if (!pagenumber.HasValue || getasarray.HasValue)
                    {
                        return await query.GetAsync(cancellationToken: cancellationToken);
                    }
                    else
                    {
                        var data = await query.PaginateAsync(
                            page: (int)pagenumber,
                            perPage: pagesize ?? 25,
                            cancellationToken: cancellationToken
                        );

                        uint totalpages = (uint)data.TotalPages;
                        uint totalcount = (uint)data.Count;

                        return ResponseHelpers.GetResult<dynamic>(
                            pagenumber.Value,
                            totalpages,
                            totalcount,
                            seed,
                            data.List,
                            Url
                        );
                    }
                }
            });
        }

        public static List<Tuple<string, string>> JsonPathCompatibilitycheck(string[] fields)
        {
            List<Tuple<string, string>> result = new List<Tuple<string, string>>();

            foreach (var field in fields)
            {
                Tuple<string, string> tp = default(Tuple<string, string>);

                //Fix support also .[*] and .[] Notation
                if (field.Contains(".[*]") || field.Contains(".[]"))
                    tp = Tuple.Create(field.Replace(".[*]", "[*]").Replace(".[]", "[*]"), field);
                else
                    tp = Tuple.Create(field, field);

                result.Add(tp);
            }

            return result;
        }

        #endregion
    }
}
