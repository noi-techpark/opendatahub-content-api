// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataModel;
using Helper.Location;
using SqlKata.Execution;

namespace Helper.Tagging
{
    public static class FillTagsObject
    {

        #region Update Tag Object

        /// <summary>
        /// Shared helper to load tags from database and create Tags objects
        /// </summary>
        private static async Task<Dictionary<string, Tags>> LoadTagsFromDatabase(
            HashSet<string> tagIds,
            QueryFactory queryFactory,
            Dictionary<string, IDictionary<string, string>>? tagEntrysTopreserve
        )
        {
            var tagsDictionary = new Dictionary<string, Tags>();

            if (tagIds == null || tagIds.Count == 0)
                return tagsDictionary;

            // Build compound filters for split tags (same logic)
            List<string> allIds = tagIds.ToList();

            // Build the query
            var query = queryFactory.Query("tags").Select("data").WhereIn("id", allIds);
            var assignedtags = await query.GetObjectListAsync<TagLinked>();

            // Build dictionary for fast lookup and create Tags objects
            foreach (var tag in assignedtags)
            {
                IDictionary<string, string>? tagentry = null;
                if (tagEntrysTopreserve != null && tagEntrysTopreserve.ContainsKey(tag.Id))
                {
                    tagentry = tagEntrysTopreserve[tag.Id];
                }

                var tagsObject = new Tags()
                {
                    Id = tag.Id,
                    Source = tag.Source,
                    Type = GetTypeFromTagTypes(tag.Types),
                    Name = GetTagName(tag.TagName),
                    TagEntry = tagentry
                };

                // Add by tag.Id as key
                if (!tagsDictionary.ContainsKey(tag.Id))
                {
                    tagsDictionary.Add(tag.Id, tagsObject);
                }

                // Also add compound key if it exists (source.id format)
                if (!string.IsNullOrEmpty(tag.Source))
                {
                    var compoundKey = $"{tag.Source}.{tag.Id}";
                    if (!tagsDictionary.ContainsKey(compoundKey))
                    {
                        tagsDictionary.Add(compoundKey, tagsObject);
                    }
                }
            }

            return tagsDictionary;
        }

        /// <summary>
        /// Extension Method to update the Tags for a single entity
        /// </summary>
        public static async Task UpdateTagsExtension<T>(
            this T data,
            QueryFactory queryFactory,
            Dictionary<string, IDictionary<string, string>>? tagEntrysTopreserve = null
        )
            where T : IHasTagInfo
        {
            //Resort TagIds
            if (data.TagIds != null)
                data.TagIds = data.TagIds.Distinct().OrderBy(x => x).ToList();

            // Load tags using shared helper
            var tagIds = data.TagIds != null ? new HashSet<string>(data.TagIds) : new HashSet<string>();
            var tagsDictionary = await LoadTagsFromDatabase(tagIds, queryFactory, tagEntrysTopreserve);

            // Assign tags to entity
            var tags = new HashSet<Tags>();
            if (data.TagIds != null)
            {
                foreach (var tagId in data.TagIds)
                {
                    if (tagsDictionary.ContainsKey(tagId))
                    {
                        tags.Add(tagsDictionary[tagId]);
                    }
                }
            }

            data.Tags = tags;
        }

        /// <summary>
        /// Batch extension method to update tags for multiple entities in one database call
        /// </summary>
        public static async Task UpdateTagsExtensionBatch<T>(
            this IEnumerable<T> dataList,
            QueryFactory queryFactory
        )
            where T : IHasTagInfo
        {
            // Collect all unique tag IDs from all entities
            var allTagIds = new HashSet<string>();
            var tagEntrysToPreserve = new Dictionary<string, IDictionary<string, string>>();

            foreach (var data in dataList)
            {
                // Resort TagIds
                if (data.TagIds != null)
                {
                    data.TagIds = data.TagIds.Distinct().OrderBy(x => x).ToList();
                    foreach (var tagId in data.TagIds)
                    {
                        allTagIds.Add(tagId);
                    }
                }

                // Collect tag entries to preserve
                if (data.Tags != null)
                {
                    foreach (var tag in data.Tags.Where(x => x.TagEntry != null))
                    {
                        if (!tagEntrysToPreserve.ContainsKey(tag.Id))
                        {
                            tagEntrysToPreserve.TryAddOrUpdate(tag.Id, tag.TagEntry);
                        }
                    }
                }
            }

            // Load all tags in one query using shared helper
            var tagsDictionary = await LoadTagsFromDatabase(allTagIds, queryFactory, tagEntrysToPreserve);

            // Assign tags to each entity
            foreach (var data in dataList)
            {
                if (data.TagIds != null && data.TagIds.Count > 0)
                {
                    var entityTags = new HashSet<Tags>();
                    foreach (var tagId in data.TagIds)
                    {
                        if (tagsDictionary.ContainsKey(tagId))
                        {
                            entityTags.Add(tagsDictionary[tagId]);
                        }
                    }
                    data.Tags = entityTags;
                }
                else
                {
                    data.Tags = new HashSet<Tags>();
                }
            }
        }

        //Get the Tag entries to preserve if update
        public static async Task<Dictionary<string, IDictionary<string, string>>?> GetTagEntrysToPreserve<T>(T data)
            where T : IHasTagInfo
        {
            Dictionary<string, IDictionary<string, string>>? tagEntrysTopreserve = null;

            //If there are TagEntrys pass this
            if (data.Tags != null)
            {
                if (data.Tags.Where(x => x.TagEntry != null).Count() > 0)
                {
                    tagEntrysTopreserve = new Dictionary<string, IDictionary<string, string>>();
                    foreach (var tagEntry in data.Tags.Where(x => x.TagEntry != null))
                    {
                        tagEntrysTopreserve.TryAddOrUpdate(tagEntry.Id, tagEntry.TagEntry);
                    }
                }
            }

            return tagEntrysTopreserve;
        }

        #endregion

        #region Add Tag by Name

        /// <summary>
        /// Extension Method to add to Tags by knowing Tag Name, adds it only if its not assigned
        /// </summary>
        /// <param name="queryFactory"></param>
        /// <returns></returns>
        public static async Task AddTagsByNameExtension<T>(this T data, 
            string tagnametocheck,
            List<string>? typelist,
            List<string>? sourcelist,
            QueryFactory queryFactory)
            where T : IHasTagInfo
        {
            var tags = await CheckTagsByName(tagnametocheck, typelist, sourcelist, queryFactory);

            if (data.Tags == null)
                data.Tags = new List<Tags>();
            if (data.TagIds == null)
                data.TagIds = new List<string>();

            foreach(var tag in tags)
            {
                if(!data.TagIds.Contains(tag.Id))
                {
                    data.Tags.Add(tag);
                    data.TagIds.Add(tag.Id);
                }                
            }            
        }

        private static async Task<ICollection<Tags>> CheckTagsByName(
            string tagNameToCheck,
            List<string>? typelist,
            List<string>? sourcelist,
            QueryFactory queryFactory
        )
        {
            ICollection<Tags> tags = new HashSet<Tags>();

            if (!String.IsNullOrEmpty(tagNameToCheck))
            {
                //Load Tags from DB
                var query = queryFactory.Query("tags").Select("data")
                    .TagWhereExpression(
                        languagelist: new List<string>(),
                        idlist: new List<string>(),
                        typelist: typelist == null ? new List<string>() : typelist,
                        validforentitylist: new List<string>(),
                        sourcelist: sourcelist == null ? new List<string>() : sourcelist,
                        publishedonlist: new List<string>(),
                        displayascategory: null,
                        searchfilter: tagNameToCheck,
                        language: null,
                        additionalfilter: null,
                        userroles: new List<string>()
                    );

                var foundtags = await query.GetObjectListAsync<TagLinked>();

                if(foundtags != null )
                {
                    //Create Tags object
                    foreach (var tag in foundtags)
                    {
                        tags.Add(
                            new Tags()
                            {
                                Id = tag.Id,
                                Source = tag.Source,
                                Type = GetTypeFromTagTypes(tag.Types),
                                Name = GetTagName(tag.TagName),
                            }
                        );
                    }
                }                
            }

            return tags;
        }

        #endregion

        //TODO REFINE
        public static string? GetTypeFromTagTypes(ICollection<string> tagtypes)
        {
            if (tagtypes == null || tagtypes.Count == 0)
                return null;
            else
            {
                if (tagtypes.Count == 1)
                {
                    return tagtypes.FirstOrDefault();
                }
                else
                {
                    if (tagtypes.Contains("ltscategory"))
                        return "ltscategory";
                    else
                        return tagtypes.FirstOrDefault();
                }
            }
        }

        public static string? GetTagName(IDictionary<string, string> tagnames)
        {
            if (tagnames == null || tagnames.Count == 0)
                return null;
            else
            {
                if (tagnames.ContainsKey("en"))
                {
                    return tagnames["en"];
                }
                else
                {
                    return tagnames.Values.Where(x => x != null).FirstOrDefault();
                }
            }
        }
    }
}
