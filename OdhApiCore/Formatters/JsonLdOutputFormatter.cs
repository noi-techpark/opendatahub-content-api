// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DataModel;
using Helper.JsonHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace OdhApiCore.Formatters
{
    public class JsonLdOutputFormatter : TextOutputFormatter
    {
        public JsonLdOutputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/ld+json"));
            // Hack because Output formatter Mapping does not work with + inside
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/ldjson"));

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        private List<object>? Transform(
            PathString path,
            JsonRaw jsonRaw,
            string language,
            string currentroute
        )
        {
            //TODO: extract language
            var settings = new JsonSerializerSettings
            {
                ContractResolver = GetOnlyContractResolver.Instance,
            };

            if (path.StartsWithSegments("/v1/Accommodation"))
            {
                var acco = JsonConvert.DeserializeObject<Accommodation>(jsonRaw.Value, settings);
                return JsonLDTransformer.TransformToSchemaNet.TransformDataToSchemaNet(
                    acco,
                    currentroute,
                    "accommodation",
                    language
                );
            }
            else if (path.StartsWithSegments("/v1/Gastronomy"))
            {
                var gastro = JsonConvert.DeserializeObject<ODHActivityPoi>(jsonRaw.Value, settings);
                return JsonLDTransformer.TransformToSchemaNet.TransformDataToSchemaNet(
                    gastro,
                    currentroute,
                    "gastronomy",
                    language
                );
            }
            else if (path.StartsWithSegments("/v1/Event"))
            {
                var @event = JsonConvert.DeserializeObject<Event>(jsonRaw.Value, settings);
                return JsonLDTransformer.TransformToSchemaNet.TransformDataToSchemaNet(
                    @event,
                    currentroute,
                    "event",
                    language
                );
            }
            else if (path.StartsWithSegments("/v1/ODHActivityPoi"))
            {
                var poi = JsonConvert.DeserializeObject<ODHActivityPoi>(jsonRaw.Value, settings);

                //check if it is Gastronomy
                if (poi != null && poi.SmgTags != null && poi.SmgTags.Contains("gastronomy"))
                    return JsonLDTransformer.TransformToSchemaNet.TransformDataToSchemaNet(
                        poi,
                        currentroute,
                        "gastronomy",
                        language
                    );
                else
                    return JsonLDTransformer.TransformToSchemaNet.TransformDataToSchemaNet(
                        poi,
                        currentroute,
                        "poi",
                        language
                    );
            }
            else if (path.StartsWithSegments("/v1/Region"))
            {
                var region = JsonConvert.DeserializeObject<Region>(jsonRaw.Value, settings);
                return JsonLDTransformer.TransformToSchemaNet.TransformDataToSchemaNet(
                    region,
                    currentroute,
                    "region",
                    language
                );
            }
            else if (path.StartsWithSegments("/v1/TourismAssociation"))
            {
                var tv = JsonConvert.DeserializeObject<Tourismverein>(jsonRaw.Value, settings);
                return JsonLDTransformer.TransformToSchemaNet.TransformDataToSchemaNet(
                    tv,
                    "tv",
                    currentroute,
                    language
                );
            }
            else if (path.StartsWithSegments("/v1/Municipality"))
            {
                var municipality = JsonConvert.DeserializeObject<Municipality>(
                    jsonRaw.Value,
                    settings
                );
                return JsonLDTransformer.TransformToSchemaNet.TransformDataToSchemaNet(
                    municipality,
                    currentroute,
                    "municipality",
                    language
                );
            }
            else if (path.StartsWithSegments("/v1/District"))
            {
                var district = JsonConvert.DeserializeObject<District>(jsonRaw.Value, settings);
                return JsonLDTransformer.TransformToSchemaNet.TransformDataToSchemaNet(
                    district,
                    currentroute,
                    "district",
                    language
                );
            }
            else if (path.StartsWithSegments("/v1/SkiArea"))
            {
                var skiarea = JsonConvert.DeserializeObject<SkiArea>(jsonRaw.Value, settings);
                return JsonLDTransformer.TransformToSchemaNet.TransformDataToSchemaNet(
                    skiarea,
                    currentroute,
                    "skiarea",
                    language
                );
            }
            else if (path.StartsWithSegments("/v1/Article"))
            {
                //TODO Ensure that the article is of type Recipe or SpecialAnnouncement!

                var article = JsonConvert.DeserializeObject<Article>(jsonRaw.Value, settings);

                if (article != null && article.Type == "rezeptartikel")
                    return JsonLDTransformer.TransformToSchemaNet.TransformDataToSchemaNet(
                        article,
                        currentroute,
                        "recipe",
                        language
                    );
                else if (article != null && article.Type == "specialannouncement")
                    return JsonLDTransformer.TransformToSchemaNet.TransformDataToSchemaNet(
                        article,
                        currentroute,
                        "specialannouncement",
                        language
                    );
                else
                    return null;
            }
            else
            {
                return null;
            }
        }

        public override async Task WriteResponseBodyAsync(
            OutputFormatterWriteContext context,
            Encoding selectedEncoding
        )
        {
            if (context.Object is JsonRaw jsonRaw)
            {
                //Get the requested language
                var query = context.HttpContext.Request.Query;
                string language = (string?)query["language"] ?? "en";

                //Get the route
                var location = new Uri(
                    $"{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}/{context.HttpContext.Request.Path}"
                );
                var currentroute = location.AbsoluteUri;

                var transformed = Transform(
                    context.HttpContext.Request.Path,
                    jsonRaw,
                    language,
                    currentroute
                );
                if (transformed != null)
                {
                    var jsonLD = "";
                    var options = new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                    };

                    if (transformed.Count == 1)
                    {
                        jsonLD = System.Text.Json.JsonSerializer.Serialize(
                            transformed.FirstOrDefault(),
                            options
                        ); //TODO REMOVE NULL VALUES
                        //JsonConvert.SerializeObject(transformed.FirstOrDefault(), Newtonsoft.Json.Formatting.None, jsonsettings);
                    }
                    else if (transformed.Count > 1)
                    {
                        jsonLD = System.Text.Json.JsonSerializer.Serialize(transformed, options);
                    }

                    await context.HttpContext.Response.WriteAsync(jsonLD);
                    //await context.HttpContext.Response.WriteAsync(transformed);
                }
                else
                {
                    await OutputFormatterHelper.NotImplemented(context);
                }
            }
            else
            {
                await OutputFormatterHelper.BadRequest(context);
            }
        }
    }
}
