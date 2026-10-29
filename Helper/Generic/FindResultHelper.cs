// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using DataModel;
using Newtonsoft.Json.Linq;

namespace Helper
{
    public static class FindResultHelper
    {
        //Languages tried for the Name when the requested language has no title
        private static readonly string[] fallbacklanguages = new[]
        {
            "en",
            "de",
            "it",
            "nl",
            "cs",
            "pl",
            "fr",
            "ru",
            "ld",
        };

        private static readonly HashSet<string> findresultfields = new HashSet<string>(
            new[] { "Id", "Name", "NameField", "Type", "Self" },
            StringComparer.OrdinalIgnoreCase
        );

        /// <summary>
        /// Maps a dataset of any odhtype to the same FindResult shape (Id, Name, NameField, Type, Self)
        /// </summary>
        public static FindResult ToFindResult(
            JToken data,
            string odhtype,
            string language,
            string[] additionalfields,
            bool removenullvalues
        )
        {
            var (name, namefield) = GetName(data, odhtype, language);

            var result = new FindResult()
            {
                Id = AsString(data.SelectToken("Id")),
                Name = name,
                NameField = namefield,
                Type = AsString(data.SelectToken("_Meta.Type")) ?? odhtype,
                Self = AsString(data.SelectToken("Self")),
            };

            //Add the fields requested with the fields parameter
            if (additionalfields.Length > 0 && data.FilterByFields(additionalfields, language) is JObject extra)
            {
                var additional = extra
                    .Properties()
                    .Where(x => !findresultfields.Contains(x.Name))
                    .Where(x => !removenullvalues || x.Value.Type != JTokenType.Null)
                    .ToDictionary(x => x.Name, x => x.Value);

                if (additional.Count > 0)
                    result.AdditionalFields = additional;
            }

            return result;
        }

        /// <summary>
        /// Gets the title of the dataset in the requested language, falls back to other languages and to Shortname
        /// Returns the title and the path of the field it was taken from
        /// </summary>
        public static (string? name, string? namefield) GetName(JToken data, string odhtype, string language)
        {
            foreach (var lang in new[] { language }.Concat(fallbacklanguages).Distinct())
            {
                var field = ODHTypeHelper.TranslateTypeToTitleField(odhtype, lang);
                var name = AsString(data.SelectToken(field));
                if (!String.IsNullOrEmpty(name))
                    return (name, field);
            }

            var shortname = AsString(data.SelectToken("Shortname"));
            return String.IsNullOrEmpty(shortname) ? (null, null) : (shortname, "Shortname");
        }

        private static string? AsString(JToken? token) =>
            token is JValue value && value.Value != null ? value.ToString() : null;
    }
}
