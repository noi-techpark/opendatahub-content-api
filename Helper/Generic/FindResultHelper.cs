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
            new[] { "Id", "Name", "Type", "Self" },
            StringComparer.OrdinalIgnoreCase
        );

        /// <summary>
        /// Maps a dataset of any odhtype to the same FindResult shape (Id, Name, Type, Self)
        /// </summary>
        public static FindResult ToFindResult(
            JToken data,
            string odhtype,
            string language,
            string[] additionalfields,
            bool removenullvalues
        )
        {
            var result = new FindResult()
            {
                Id = AsString(data.SelectToken("Id")),
                Name = GetName(data, odhtype, language),
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
        /// </summary>
        public static string? GetName(JToken data, string odhtype, string language)
        {
            foreach (var lang in new[] { language }.Concat(fallbacklanguages).Distinct())
            {
                var name = AsString(data.SelectToken(ODHTypeHelper.TranslateTypeToTitleField(odhtype, lang)));
                if (!String.IsNullOrEmpty(name))
                    return name;
            }

            var shortname = AsString(data.SelectToken("Shortname"));
            return String.IsNullOrEmpty(shortname) ? null : shortname;
        }

        private static string? AsString(JToken? token) =>
            token is JValue value && value.Value != null ? value.ToString() : null;
    }
}
