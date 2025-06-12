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
        /// Extension Method to update the Tags
        /// </summary>
        /// <param name="queryFactory"></param>
        /// <returns></returns>
        public static async Task UpdateTagsExtension<T>(this T data, QueryFactory queryFactory)
            where T : IHasTagInfo
        {
            Dictionary<string, IDictionary<string, string>>? tagEntrysTopreserve = null;

            //If there are TagEntrys pass this
            if (data.Tags != null)
            {
                if(data.Tags.Where(x => x.TagEntry != null).Count() > 0)
                {
                    tagEntrysTopreserve = new Dictionary<string, IDictionary<string, string>>();
                    foreach (var tagEntry in data.Tags.Where(x => x.TagEntry != null))
                    {
                        tagEntrysTopreserve.TryAddOrUpdate(tagEntry.Id, tagEntry.TagEntry);
                    }                    
                }
            }

            data.Tags = await UpdateTags(data.TagIds, queryFactory, tagEntrysTopreserve);
        }

        private static async Task<ICollection<Tags>> UpdateTags(
            ICollection<string> tagIds,
            QueryFactory queryFactory,
            Dictionary<string, IDictionary<string,string>>? tagEntrysTopreserve
        )
        {
            ICollection<Tags> tags = new HashSet<Tags>();

            if (tagIds != null && tagIds.Count > 0)
            {
                //Load Tags from DB
                var query = queryFactory.Query("tags").Select("data").WhereIn("id", tagIds);

                var assignedtags = await query.GetObjectListAsync<TagLinked>();

                //Create Tags object
                foreach (var tag in assignedtags)
                {
                    IDictionary<string, string>? tagentry = null;
                    if(tagEntrysTopreserve != null && tagEntrysTopreserve.ContainsKey(tag.Id))
                    {
                        tagentry = tagEntrysTopreserve[tag.Id];
                    }

                    tags.Add(
                        new Tags()
                        {
                            Id = tag.Id,
                            Source = tag.Source,
                            Type = GetTypeFromTagTypes(tag.Types),
                            Name = GetTagName(tag.TagName),
                            TagEntry = tagentry
                        }
                    );
                }
            }

            return tags;
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
