// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Helper
{
    public static class JsonTransformerMethods
    {
        private static readonly HashSet<string> Languages = new HashSet<string>
        {
            "de",
            "it",
            "en",
            "cs",
            "fr",
            "nl",
            "pl",
            "ru",
            "ld",
        };

        private static bool IsLanguageKey(string key) => Languages.Contains(key);

        //Cutting out all not requested Language Dictionary Objects
        public static JToken? FilterByLanguage(this JToken? token, string language)
        {
            if (token == null)
                return null;
            JToken Walk(JToken token) =>
                token switch
                {
                    JObject obj => new JObject(
                        obj.Properties()
                            .Where(x =>
                                // Check if property name is a language identifier
                                // and if it is the same as the provided language argument
                                !(IsLanguageKey(x.Name) && x.Name != language)
                            )
                            .Select(Walk)
                    ),
                    JProperty prop => new JProperty(prop.Name, Walk(prop.Value)),
                    JArray arr => new JArray(arr.Select(Walk)),
                    _ => token,
                };
            return Walk(token);
        }

        //Cutting out all images where the License is not CC0
        public static JToken? FilterImagesByCC0License(this JToken? token)
        {
            if (token == null)
                return null;
            static JObject? TransformObj(JObject obj)
            {
                // Get the License property of an object
                var licenseProp = obj.Property("License");
                // If License property exists and it's value isn't CC0 return null,
                // which filters away the whole object
                return
                    licenseProp != null
                    && (
                        licenseProp.Value == null
                        || (
                            !licenseProp.Value.Equals(new JValue("CC0"))
                            && !licenseProp.Path.StartsWith("LicenseInfo")
                        )
                    )
                    ? null
                    : new JObject(obj.Properties().Select(x => Walk(x)).Where(x => x != null));
            }
            ;
            static JProperty? TransformProp(JProperty prop)
            {
                var value = Walk(prop.Value);
                return value == null ? null : new JProperty(prop.Name, value);
            }
            static JToken? Walk(JToken token) =>
                token switch
                {
                    JObject obj => TransformObj(obj),
                    JProperty prop => TransformProp(prop),
                    JArray arr => new JArray(
                        arr.Select(x => Walk(x))
                            // Filter away empty content
                            .Where(x => x != null)
                    ),
                    _ => token,
                };
            return Walk(token);
        }

        //Cutting out all Rooms where Source is not HGV
        public static JToken? FilterAccoRoomInfoByHGVSource(this JToken? token)
        {
            if (token == null)
                return null;
            static JObject? TransformObj(JObject obj)
            {
                // Get the AccoRoomInfo property of an object which has to be an array
                var accoRoomInfo = obj.Property("AccoRoomInfo");
                if (accoRoomInfo is not null && accoRoomInfo.Value is JArray)
                {
                    var props = accoRoomInfo
                        .Value.ToArray()
                        .Select(prop =>
                        {
                            // The prop needs to be an object
                            if (prop is JObject roomInfo)
                            {
                                // Get the Source property of an object
                                var sourceProp = roomInfo.Property("Source");
                                // If Source property exists and it's value is hgv return null,
                                // which filters away the whole object
                                return
                                    sourceProp is not null
                                    && (
                                        sourceProp.Value is null
                                        || sourceProp.Value.Equals(new JValue("hgv"))
                                    )
                                    ? null
                                    : new JObject(
                                        roomInfo
                                            .Properties()
                                            .Select(x => Walk(x))
                                            .Where(x => x != null)
                                    );
                            }
                            else
                            {
                                return prop;
                            }
                        })
                        .Where(prop => prop is not null);
                    obj.TryAddOrUpdate("AccoRoomInfo", new JArray(props));
                    return obj;
                }
                else
                {
                    return obj;
                }
            }
            ;
            static JProperty? TransformProp(JProperty prop)
            {
                var value = Walk(prop.Value);
                return value == null ? null : new JProperty(prop.Name, value);
            }
            static JToken? Walk(JToken token) =>
                token switch
                {
                    JObject obj => TransformObj(obj),
                    JProperty prop => TransformProp(prop),
                    JArray arr => new JArray(
                        arr.Select(x => Walk(x))
                            // Filter away empty content
                            .Where(x => x != null)
                    ),
                    _ => token,
                };
            return Walk(token);
        }

        //Cutting out Property passed by List
        public static JToken? FilterOutProperties(this JToken? token, List<string> propstocut)
        {
            if (token == null)
                return null;

            static JObject TransformByPropList(JObject obj, List<string> propstocut)
            {
                // Get the TVMember property of an object which has to be an property
                //var accoTVMember = obj.Property("TVMember");
                //if (accoTVMember is not null && accoTVMember is JProperty)
                //    //Cut out this property
                //    return new JObject(obj.Properties().Where(x => !propstocut.Contains(x.Name)));
                //else
                //    return obj;
                return new JObject(obj.Properties().Where(x => !propstocut.Contains(x.Name)));
            }
            static JToken Walk(JToken token, List<string> propstocut) =>
                token switch
                {
                    JObject obj => TransformByPropList(obj, propstocut),
                    _ => token,
                };

            return Walk(token, propstocut);
        }

        //Cutting out all properties with null
        public static JToken? FilterOutNullProperties(this JToken? token)
        {
            if (token == null)
                return null;

            static JObject RemoveNullProps(JObject obj)
            {
                return new JObject(
                    obj.Properties()
                        .Where(x => !x.Value.IsNullOrEmpty(true, true))
                        .Select(x => new JProperty(x.Name, FilterOutNullProperties(x.Value)))
                );
            }
            static JToken Walk(JToken token) =>
                token switch
                {
                    JObject obj => RemoveNullProps(obj),
                    JArray objs => new JArray(objs.Select(FilterOutNullProperties)),
                    //JProperty prop => new JProperty(prop.Name, Walk(prop.Value)),
                    _ => token,
                };

            return Walk(token);
        }

        //Cutting out Property _Meta
        public static JToken? FilterMetaInformations(this JToken? token)
        {
            if (token == null)
                return null;
            static JObject TransformMetaObj(JObject obj) =>
                new JObject(obj.Properties().Where(x => x.Name != "_Meta"));
            static JToken Walk(JToken token) =>
                token switch
                {
                    JObject obj => TransformMetaObj(obj),
                    _ => token,
                };
            return Walk(token);
        }

        //Cutting out all Properties where Meta Contains the information Closeddata is true
        public static JToken? FilterClosedData(this JToken? token)
        {
            if (token == null)
                return null;
            static JObject? TransformObj(JObject obj)
            {
                // Get the Metadata property
                var metaProp = obj.Property("_Meta");
                if (metaProp != null && metaProp.Value is JObject metaObj)
                {
                    // Get the ClosedData property of an object
                    var closedDataProp = metaObj.Property("ClosedData");
                    // If ClosedData property exists and it's value is true,
                    // which filters away the whole object
                    if (closedDataProp != null && closedDataProp.Value.Equals(new JValue(true)))
                    {
                        return null;
                    }
                }
                return new JObject(obj.Properties().Select(x => Walk(x)));
            }
            ;
            static JProperty? TransformProp(JProperty prop)
            {
                var value = Walk(prop.Value);
                return value == null ? null : new JProperty(prop.Name, value);
            }
            static JToken? Walk(JToken token) =>
                token switch
                {
                    JObject obj => TransformObj(obj),
                    JProperty prop => TransformProp(prop),
                    JArray arr => new JArray(
                        arr.Select(x => Walk(x))
                            // Filter away empty content
                            .Where(x => x != null)
                    ),
                    _ => token,
                };
            return Walk(token);
        }

        //Cuts out all fields that are not requested only Id is active by default
        public static JToken? FilterByFields(
            this JToken? token,
            string[] fieldsFromQueryString,
            string? languageParam
        )
        {
            if (token == null)
                return null;

            var language = languageParam ?? "en";

            var fields = new List<(string name, string path)> { ("Id", "Id") };
            fields.AddRange(fieldsFromQueryString.Select(field => (field, field)));
            //fields.AddRange(fieldsFromQueryString.Select(field => 
            //{
            //    var cleanName = field; //string.Join(".", SplitPath(field)); 
            //    var jsonPath = ToJsonPath(field);                 
            //    return (cleanName, jsonPath);
            //}));

            if (token is JObject obj)
            {
                return new JObject(
                    fields
                        .Distinct(new DistinctComparer())
                        .Select(x =>
                        {
                            try
                            {
                                try
                                {
                                    return new JProperty(
                                        x.name,
                                        token.SelectToken(x.path, errorWhenNoMatch: true)
                                    );
                                }
                                catch (JsonException)
                                {
                                    return new JProperty(
                                        x.name,
                                        token.SelectTokens(x.path, errorWhenNoMatch: true)
                                    );
                                }
                            }
                            catch (JsonException)
                            {
                                return new JProperty(x.name, (object?)null);
                            }
                        })
                );
            }
            return token;
        }

        //Transforms URL of the self link to the right domain
        public static JToken? TransformSelfLink(
            this JToken? token,
            Func<string, string> urlGenerator
        )
        {
            if (token == null)
                return null;
            static JObject? TransformObj(JObject obj, Func<string, string> urlGenerator) =>
                new JObject(obj.Properties().Select(x => Walk(x, urlGenerator)));
            static string? TransformSelf(string? self, Func<string, string> urlGenerator)
            {
                if (self == null)
                    return null;
                // FIXME: Temporary workaround
                if (self.StartsWith("https://tourism.opendatahub.bz.it/api/"))
                    self = self.Substring(38);
                if (self.StartsWith("https://tourism.opendatahub.com/api/"))
                    self = self.Substring(36);
                return urlGenerator(self);
            }
            static JProperty? TransformProp(JProperty prop, Func<string, string> urlGenerator)
            {
                if (prop.Name == "Self")
                {
                    string? value = TransformSelf(prop.Value.Value<string?>(), urlGenerator);
                    return new JProperty(prop.Name, value);
                }
                //Temporary Remove
                if (prop.Name == "GpxTrackUrl" && prop.Value.Value<string?>().StartsWith("GeoShape"))
                {
                    string? value = TransformSelf(prop.Value.Value<string?>(), urlGenerator);
                    return new JProperty(prop.Name, value);
                }
                else
                {
                    var value = Walk(prop.Value, urlGenerator);
                    return value == null ? null : new JProperty(prop.Name, value);
                }
            }
            static JToken? Walk(JToken token, Func<string, string> urlGenerator) =>
                token switch
                {
                    JObject obj => TransformObj(obj, urlGenerator),
                    JProperty prop => TransformProp(prop, urlGenerator),
                    JArray arr => new JArray(
                        arr.Select(x => Walk(x, urlGenerator))
                            // Filter away empty content
                            .Where(x => x != null)
                    ),
                    _ => token,
                };
            return Walk(token, urlGenerator);
        }

        private static string EscapeJsonPath(string field)
        {
            // Split only on dots that are path separators, then re-wrap
            // segments containing dots in bracket notation
            var segments = field.Split('.');
            var escaped = segments.Select(segment =>
                segment.Contains('.') || segment.Any(c => !char.IsLetterOrDigit(c) && c != '_')
                    ? $"['{segment}']"
                    : segment
            );
            return string.Join(".", escaped)
                         .Replace(".[", "["); // fix double dot before bracket: "Mapping.['tirol']" --> "Mapping['tirol']"
        }

        //private static string ToJsonPath(string field)
        //{
        //    // Split the field path respecting that some segments may contain dots
        //    // Input:  "Mapping.tirol.mapservices.eu.id"
        //    // Output: "Mapping['tirol.mapservices.eu']['id']"  (only if key exists with dots)
        //    // Better: escape ALL segments to be safe
        //    var segments = SplitPath(field);

        //    var sb = new StringBuilder();
        //    foreach (var segment in segments)
        //    {
        //        if (segment.Contains('.') || segment.Contains('-') || segment.Contains(' '))
        //            sb.Append($"['{segment}']");
        //        else if (sb.Length == 0)
        //            sb.Append(segment);
        //        else
        //            sb.Append($".{segment}");
        //    }
        //    return sb.ToString();
        //}

        //private static string ToJsonPath(string field)
        //{
        //    var segments = SplitPath(field);
        //    var sb = new StringBuilder();

        //    foreach (var segment in segments)
        //    {
        //        // Array index [0]
        //        if (int.TryParse(segment, out _))
        //        {
        //            sb.Append($"[{segment}]");
        //        }
        //        // Wildcard [*]
        //        else if (segment == "*")
        //        {
        //            sb.Append("[*]");
        //        }
        //        // Empty bracket []
        //        else if (segment == "")
        //        {
        //            sb.Append("[]");
        //        }
        //        // Keys with special chars → ['key']
        //        else if (segment.Contains('.') || segment.Contains('-') || segment.Contains(' '))
        //        {
        //            sb.Append($"['{segment}']");
        //        }
        //        // First normal segment — no leading dot
        //        else if (sb.Length == 0)
        //        {
        //            sb.Append(segment);
        //        }
        //        // Normal segment
        //        else
        //        {
        //            sb.Append($".{segment}");
        //        }
        //    }

        //    return sb.ToString();
        //}

        //private static IEnumerable<string> SplitPath(string field)
        //{
        //    // Handles:
        //    // ['key.with.dots']  → quoted key
        //    // [0]                → numeric index
        //    // [*]                → wildcard
        //    // []                 → empty (all elements)
        //    // plain              → normal segment
        //    return Regex.Matches(field, @"\['([^']+)'\]|\[(\d+|\*|)\]|([^.\[]+)")
        //                .Select(m => m.Groups[1].Success ? m.Groups[1].Value   // ['key']
        //                           : m.Groups[2].Success ? m.Groups[2].Value   // [0], [*], []
        //                           : m.Groups[3].Value);                        // plain
        //}
    }

    public static class JsonExtensions
    {
        public static bool IsNullOrEmpty(
            this JToken token,
            bool filteremptyarrays,
            bool filteremptyobjects
        )
        {
            bool result = false;

            result =
                (token == null)
                || (token.Type == JTokenType.String && token.ToString() == String.Empty)
                || (token.Type == JTokenType.Null);

            if (filteremptyarrays)
                result =
                    result
                    || (token == null)
                    || (token.Type == JTokenType.Array && !token.HasValues); //filter out empty array

            if (filteremptyobjects)
                result =
                    result
                    || (token == null)
                    || (token.Type == JTokenType.Object && !token.HasValues); //filter out empty object

            //(token.Type == JTokenType.Undefined) || //not sure if needed

            return result;
        }
    }
}
