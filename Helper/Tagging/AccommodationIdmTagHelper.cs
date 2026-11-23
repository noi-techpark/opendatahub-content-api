// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Helper.Tagging
{
    /// <summary>
    /// Tags (Source idm) for the IDM Accommodation Types, Badges, Themes and SpecialFeatures (accommodationtypes table)
    /// The Tags are created by DataModify AccommodationTypesToTags, the Mapping.idm (type, key) links the Tag to the value on the Accommodation
    /// (AccoTypeId, BadgeIds, ThemeIds, SpecialFeaturesIds)
    /// </summary>
    public static class AccommodationIdmTagHelper
    {
        //accommodationtypes table Type → Tag Type
        //Board and Category are not converted, they are already present as LTS Tags (accommodationmealplans, accommodationcategory)
        //Type uses the same Tag Type as the LTS Accommodation Types, distinguished by Source (idm/lts).
        //The LTS sync (LTSApiAccommodationTypesImportHelper) only deactivates Tags with _Meta.Source lts, so the idm Tags are not affected
        public static readonly Dictionary<string, string> TagTypes = new Dictionary<string, string>()
        {
            { "Type", "accommodationtypes" },
            { "Badge", "accommodationbadge" },
            { "Theme", "accommodationtheme" },
            { "SpecialFeature", "accommodationspecialfeature" },
        };

        /// <summary>
        /// Loads all active idm Accommodation Tags and returns a lookup "{type}|{key}" → Tag Id (case insensitive)
        /// Load this once per import run
        /// </summary>
        public static async Task<IDictionary<string, string>> GetTagLookup(QueryFactory queryFactory)
        {
            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var tags = await queryFactory
                .Query()
                .SelectRaw("data")
                .From("tags")
                .SourceFilter_GeneratedColumn(new List<string>() { "idm" })
                .TagTypesFilter(TagTypes.Values.ToList())
                .GetObjectListAsync<TagLinked>();

            foreach (var tag in tags)
            {
                if (
                    tag.Id == null
                    || tag.Active != true
                    || tag.Mapping == null
                    || !tag.Mapping.TryGetValue("idm", out var idmmapping)
                    || !idmmapping.TryGetValue("type", out var type)
                    || !idmmapping.TryGetValue("key", out var key)
                    || String.IsNullOrEmpty(type)
                    || String.IsNullOrEmpty(key)
                    || !TagTypes.ContainsKey(type)
                )
                    continue;

                lookup.TryAdd(type + "|" + key, tag.Id);
            }

            return lookup;
        }

        /// <summary>
        /// Assigns the idm Accommodation Tags to TagIds based on AccoTypeId, BadgeIds, ThemeIds and SpecialFeaturesIds.
        /// Only the TagIds of the idm Accommodation Tags (values of the lookup) are removed/readded, all other TagIds are not touched.
        /// </summary>
        public static void AssignTags(AccommodationV2 accommodation, IDictionary<string, string> taglookup)
        {
            if (accommodation == null || taglookup == null || taglookup.Count == 0)
                return;

            var idmtagids = taglookup.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);

            //Tags to assign
            var tagidstoassign = new List<string>();

            void AddTags(string type, IEnumerable<string>? keys)
            {
                if (keys == null)
                    return;

                foreach (var key in keys)
                {
                    if (!String.IsNullOrEmpty(key) && taglookup.TryGetValue(type + "|" + key, out var tagid))
                        tagidstoassign.Add(tagid);
                }
            }

            AddTags("Type", accommodation.AccoTypeId != null ? new List<string>() { accommodation.AccoTypeId } : null);
            AddTags("Badge", accommodation.BadgeIds);
            AddTags("Theme", accommodation.ThemeIds);
            AddTags("SpecialFeature", accommodation.SpecialFeaturesIds);

            //Remove previously assigned idm Accommodation Tags (no longer valid ones), keep all other TagIds
            var tagids = (accommodation.TagIds ?? new List<string>())
                .Where(x => !idmtagids.Contains(x))
                .ToList();

            tagids.AddRange(tagidstoassign);

            if (tagids.Count > 0 || accommodation.TagIds != null)
                accommodation.TagIds = tagids.Distinct().ToList();
        }
    }
}
