// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DataModel;
using Helper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using OdhApiCore.Swagger;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiCore.Controllers.api
{
    /// <summary>
    /// <a href="https://github.com/noi-techpark/odh-docs/wiki/Getting-Data-as-JsonLD---schema.org" target="_blank">Wiki Json LD Api</a>
    /// </summary>
    [SwaggerTags("Z9 JsonLD")]
    public class JsonLDController : OdhController
    {
        public JsonLDController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<JsonLDController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier,
            IHttpClientFactory httpClientFactory
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        /// <summary>
        /// GET Detail Data in JSON LD Format (Schema.org Datatypes as output)
        /// </summary>
        /// <param name="type">Type to transform currently available: ('accommodation', 'gastronomy', 'event', 'recipe', 'poi', 'region', 'tv', 'municipality', 'district', 'skiarea') required <a href="https://github.com/noi-techpark/odh-docs/wiki/Dataset-Type" target="_blank">Wiki Dataset Type</a></param>
        /// <param name="Id">ID of the data to transform, required</param>
        /// <param name="language">Output Language, standard EN</param>
        /// <param name="idtoshow">ID to show on Json LD @id, not provided Id of ODH api call is taken</param>
        /// <param name="imageurltoshow">image url to show on Json LD @image, not provided image url of data is taken</param>
        /// <param name="urltoshow">url to show on Json LD @id, not provided idtoshow is taken, idtoshow not provided url is filled with url of the data</param>
        /// <param name="showid">Show the @id property in Json LD default value true</param>
        /// <returns></returns>
        //[Authorize(Roles = "DataReader")]
        [HttpGet, Route("JsonLD/DetailInLD")]
        public async Task<IActionResult> GetDetailInLD(
            string type,
            string Id,
            string? language = "en",
            string? idtoshow = "",
            string? urltoshow = "",
            string? imageurltoshow = "",
            bool showid = true
        )
        {
            try
            {
                var location = new Uri(
                    $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/{HttpContext.Request.Path}"
                );
                var currentroute = location.AbsoluteUri;

                var myobject = default(List<object>);

                switch (type.ToLower())
                {
                    case "accommodation":
                        myobject = await LoadFromRavenDBSchemaNet<Accommodation>(
                            Id,
                            currentroute + "/Accommodation/" + Id,
                            language,
                            idtoshow,
                            urltoshow,
                            imageurltoshow,
                            type.ToLower(),
                            showid,
                            "accommodations"
                        );
                        break;
                    case "gastronomy":
                        myobject = await LoadFromRavenDBSchemaNet<ODHActivityPoi>(
                            Id,
                            currentroute + "/ODHActivityPoi/" + Id,
                            language,
                            idtoshow,
                            urltoshow,
                            imageurltoshow,
                            type.ToLower(),
                            showid,
                            "smgpois"
                        );
                        break;
                    case "event":
                        myobject = await LoadFromRavenDBSchemaNet<Event>(
                            Id,
                            currentroute + "/Event/" + Id,
                            language,
                            idtoshow,
                            urltoshow,
                            imageurltoshow,
                            type.ToLower(),
                            showid,
                            "events"
                        );
                        break;
                    case "recipe":
                        myobject = await LoadFromRavenDBSchemaNet<Article>(
                            Id,
                            currentroute + "/Article/" + Id,
                            language,
                            idtoshow,
                            urltoshow,
                            imageurltoshow,
                            type.ToLower(),
                            showid,
                            "articles"
                        );
                        break;
                    case "specialannouncement":
                        myobject = await LoadFromRavenDBSchemaNet<Article>(
                            Id,
                            currentroute + "/Article/" + Id,
                            language,
                            idtoshow,
                            urltoshow,
                            imageurltoshow,
                            type.ToLower(),
                            showid,
                            "articles"
                        );
                        break;
                    case "poi":
                        myobject = await LoadFromRavenDBSchemaNet<ODHActivityPoi>(
                            Id,
                            currentroute + "/ODHActivityPoi/" + Id,
                            language,
                            idtoshow,
                            urltoshow,
                            imageurltoshow,
                            type.ToLower(),
                            showid,
                            "smgpois"
                        );
                        break;
                    case "region":
                        myobject = await LoadFromRavenDBSchemaNet<Region>(
                            Id,
                            currentroute + "/Region/" + Id,
                            language,
                            idtoshow,
                            urltoshow,
                            imageurltoshow,
                            type.ToLower(),
                            showid,
                            "regions"
                        );
                        break;
                    case "tv":
                        myobject = await LoadFromRavenDBSchemaNet<Tourismverein>(
                            Id,
                            currentroute + "/TourismAssociation/" + Id,
                            language,
                            idtoshow,
                            urltoshow,
                            imageurltoshow,
                            type.ToLower(),
                            showid,
                            "tvs"
                        );
                        break;
                    case "municipality":
                        myobject = await LoadFromRavenDBSchemaNet<Municipality>(
                            Id,
                            currentroute + "/Municipality/" + Id,
                            language,
                            idtoshow,
                            urltoshow,
                            imageurltoshow,
                            type.ToLower(),
                            showid,
                            "municipalities"
                        );
                        break;
                    case "district":
                        myobject = await LoadFromRavenDBSchemaNet<District>(
                            Id,
                            currentroute + "/District/" + Id,
                            language,
                            idtoshow,
                            urltoshow,
                            imageurltoshow,
                            type.ToLower(),
                            showid,
                            "districts"
                        );
                        break;
                    case "skiarea":
                        myobject = await LoadFromRavenDBSchemaNet<SkiArea>(
                            Id,
                            currentroute + "/SkiArea/" + Id,
                            language,
                            idtoshow,
                            urltoshow,
                            imageurltoshow,
                            type.ToLower(),
                            showid,
                            "skiareas"
                        );
                        break;
                    default:
                        myobject = new List<object>();
                        myobject.Add(new { message = "JSON LD not available" });
                        break;
                }

                var myjson = "";

                if (myobject != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                    };

                    if (type.ToLower() == "event")
                        myjson = System.Text.Json.JsonSerializer.Serialize(myobject, options);
                    else
                        myjson = System.Text.Json.JsonSerializer.Serialize(
                            myobject.FirstOrDefault(),
                            options
                        );

                    return Ok(myjson);
                }
                else
                    return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<List<object>> LoadFromRavenDBSchemaNet<T>(
            string Id,
            string currentroute,
            string language,
            string idtoshow,
            string urltoshow,
            string imagetoshow,
            string type,
            bool showid,
            string table
        )
        {
            //TO CHECK
            var query = QueryFactory
                .Query(table)
                .Select("data")
                .Where("gen_id", Id)
                //.Anonymous_Logged_UserRule_GeneratedColumn(FilterClosedData, !ReducedData);
                //.When(FilterClosedData, q => q.FilterClosedData());
                .FilterDataByAccessRoles(UserRolesToFilter);

            var myobject = await query.FirstOrDefaultAsync<JsonRaw?>();

            if (myobject != null)
            {
                var myparsedobject = JsonConvert.DeserializeObject<T>(myobject.Value);
                if (myparsedobject is { })
                    return JsonLDTransformer.TransformToSchemaNet.TransformDataToSchemaNet<T>(
                        myparsedobject,
                        currentroute,
                        type,
                        language,
                        null,
                        idtoshow,
                        urltoshow,
                        imagetoshow,
                        showid
                    );
            }

            return new();
        }
    }
}
