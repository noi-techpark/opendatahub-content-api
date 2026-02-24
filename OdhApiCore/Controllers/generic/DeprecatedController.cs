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
using OdhApiCore.GenericHelpers;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiCore.Controllers
{
    /// <summary>
    /// <a href="https://github.com/noi-techpark/odh-docs/wiki/Deprecated-Api-in-ODH-Tourism-api" target="_blank">Wiki Deprecated Api</a>
    /// </summary>
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class DeprecatedController : OdhController
    {
        public DeprecatedController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<DeprecatedController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        #region SWAGGER Exposed API

        /// <summary>
        /// GET Generic Deprecated search of fields
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="type">Mandatory search trough Entities (metadata, accommodation, odhactivitypoi, event, webcam, measuringpoint, ltsactivity, ltspoi, ltsgastronomy, article ..... null = search trough all entities) <a href="https://github.com/noi-techpark/odh-docs/wiki/Dataset-Type" target="_blank">Wiki Dataset Type</a></param>
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
        [HttpGet, Route("Deprecated")]
        //[Authorize(Roles = "DataReader,CommonReader,AccoReader,ActivityReader,PoiReader,ODHPoiReader,PackageReader,GastroReader,EventReader,ArticleReader")]
        public async Task<IActionResult> GetDeprecatedAsync(
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

            return await GetDeprecated(
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

        ////TODO Get openapi file and parse trough an render to output
        //[HttpGet, Route("v1/DeprecatedTest")]
        //public async Task<IActionResult> Deprecated()
        //{
        //    var requesturl = string.Format("{0}://{1}{2}{3}", HttpContext.Request.Scheme, HttpContext.Request.Host, HttpContext.Request.Path, "swagger/v1/swagger.json");

        //    using (var client = new HttpClient())
        //    {
        //        var response = await client.GetAsync(requesturl);
        //        var responsecontent = await response.Content.ReadAsStringAsync();

        //        JObject? obj = JsonConvert.DeserializeObject<JObject>(responsecontent);

        //        return Ok(obj);
        //    }
        //}

        #endregion

        #region GETTER

        private Task<IActionResult> GetDeprecated(
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
                List<string> typestocheck = new List<string>();
                Dictionary<string, IEnumerable<DeprecationInfo>> resultdict =
                    new Dictionary<string, IEnumerable<DeprecationInfo>>();

                if (odhtype == null)
                    typestocheck = ODHTypeHelper.GetAllTypeStrings().ToList();
                else
                    typestocheck = odhtype.Split(",").ToList();

                foreach (var typetocheck in typestocheck)
                {
                    resultdict.Add(
                        typetocheck,
                        GetDeprecatedFieldsByAttributes.GetDeprecatedFields(
                            ODHTypeHelper.TranslateTypeString2Type(typetocheck)
                        )
                    );
                }

                return resultdict;
            });
        }

        #endregion
    }
}
