// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataModel;
using Newtonsoft.Json;
using SqlKata.Execution;

namespace Helper
{
    public class ODHTagHelper
    {
        public static async Task<IEnumerable<SmgTags>> GetODHTagsValidforCategories(
            QueryFactory QueryFactory,
            List<string> validforentity,
            List<string>? idlist = null
        )
        {
            try
            {
                List<SmgTags> validtags = new List<SmgTags>();

                var validtagquery = QueryFactory
                    .Query("smgtags")
                    .Select("data")
                    .When(
                        validforentity.Count > 0,
                        q =>
                            q.WhereInJsonb(
                                validforentity,
                                tag => new { ValidForEntity = new[] { tag.ToLower() } }
                            )
                    )
                    .When(
                        idlist != null,
                        w =>
                            w.WhereIn(
                                "id",
                                idlist?.Select(x => x.ToLower()) ?? Enumerable.Empty<string>()
                            )
                    )
                    .WhereRaw("data->>'DisplayAsCategory' = $$", "true");

                var validtagdata = await validtagquery.GetObjectListAsync<SmgTags>();

                return validtagdata;
            }
            catch (Exception)
            {
                return new List<SmgTags>();
            }
        }

        public static async Task<SmgTags?> GeODHTagByID(QueryFactory QueryFactory, string id)
        {
            try
            {
                var query = QueryFactory.Query("smgtags").Select("data").Where("id", id.ToLower());

                var result = await query.GetObjectSingleAsync<SmgTags>();

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string SetMainCategorizationForODHActivityPoi(ODHActivityPoiLinked smgpoi)
        {
            //Add LTS Id as Mapping
            var maintype = "poi";

            if (
                !String.IsNullOrEmpty(smgpoi.SyncSourceInterface)
                && smgpoi.SyncSourceInterface.ToLower() == "activitydata"
            )
                maintype = "activity";
            if (
                !String.IsNullOrEmpty(smgpoi.SyncSourceInterface)
                && smgpoi.SyncSourceInterface.ToLower() == "poidata"
            )
                maintype = "poi";
            if (
                !String.IsNullOrEmpty(smgpoi.SyncSourceInterface)
                && smgpoi.SyncSourceInterface.ToLower() == "gastronomicdata"
            )
                maintype = "gastronomy";
            if (
                !String.IsNullOrEmpty(smgpoi.SyncSourceInterface)
                && smgpoi.SyncSourceInterface.ToLower() == "beacondata"
            )
                maintype = "poi";
            if (
                !String.IsNullOrEmpty(smgpoi.SyncSourceInterface)
                && smgpoi.SyncSourceInterface.ToLower() == "archapp"
            )
                maintype = "poi";
            if (
                !String.IsNullOrEmpty(smgpoi.SyncSourceInterface)
                && smgpoi.SyncSourceInterface.ToLower() == "museumdata"
            )
                maintype = "poi";
            if (
                !String.IsNullOrEmpty(smgpoi.SyncSourceInterface)
                && smgpoi.SyncSourceInterface.ToLower() == "suedtirolwein"
            )
                maintype = "gastronomy";
            if (
                !String.IsNullOrEmpty(smgpoi.SyncSourceInterface)
                && smgpoi.SyncSourceInterface.ToLower() == "common"
            )
                maintype = "activity";
            if (
                !String.IsNullOrEmpty(smgpoi.SyncSourceInterface)
                && smgpoi.SyncSourceInterface.ToLower() == "none"
            )
                maintype = "poi";
            if (
                !String.IsNullOrEmpty(smgpoi.SyncSourceInterface)
                && smgpoi.SyncSourceInterface.ToLower() == "magnolia"
            )
                maintype = "poi";
            if (
                !String.IsNullOrEmpty(smgpoi.SyncSourceInterface)
                && smgpoi.SyncSourceInterface.ToLower() == "sta"
            )
                maintype = "poi";
            if (
                !String.IsNullOrEmpty(smgpoi.SyncSourceInterface)
                && smgpoi.SyncSourceInterface.ToLower() == "dssliftbase"
            )
                maintype = "activity";
            if (
                !String.IsNullOrEmpty(smgpoi.SyncSourceInterface)
                && smgpoi.SyncSourceInterface.ToLower() == "dssslopebase"
            )
                maintype = "activity";

            if (smgpoi.SmgTags == null)
            {
                smgpoi.SmgTags = new List<string>();
                smgpoi.SmgTags.Add(maintype);
            }
            else if (
                !smgpoi.SmgTags.Contains("activity")
                && !smgpoi.SmgTags.Contains("poi")
                && !smgpoi.SmgTags.Contains("gastronomy")
            )
            {
                //Assign to SMGTags if not there
                if (!smgpoi.SmgTags.Contains(maintype))
                    smgpoi.SmgTags.Add(maintype);
            }

            if (smgpoi.TagIds == null)
            {
                smgpoi.TagIds = new List<string>();
                smgpoi.TagIds.Add(maintype);
            }
            else if (
                !smgpoi.TagIds.Contains("activity")
                && !smgpoi.TagIds.Contains("poi")
                && !smgpoi.TagIds.Contains("gastronomy")
            )
            {
                //Assign to SMGTags if not there
                if (!smgpoi.TagIds.Contains(maintype))
                    smgpoi.TagIds.Add(maintype);
            }

            return maintype;
        }

        public static async Task GetCategoriesFromAssignedODHTags(
            ODHActivityPoiLinked smgpoi,
            string jsondir
        )
        {
            //All available languages
            List<string> languagelistcategories = new List<string>()
            {
                "de",
                "it",
                "en",
                "nl",
                "cs",
                "pl",
                "fr",
                "ru",
            };

            //Get Valid Categories live
            //var validtagsforcategories = await GetODHTagsValidforCategories(QueryFactory, new List<string>() { "smgpois" });
            //Get Valid Categories from json
            var validtagsforcategories = await GetAllODHTagsforCategorizationJson(jsondir);

            //Set the AdditionalPoiInfos if they are missing
            if (smgpoi.AdditionalPoiInfos == null)
                smgpoi.AdditionalPoiInfos = new Dictionary<string, AdditionalPoiInfos>();

            if (smgpoi.SmgTags != null && smgpoi.SmgTags.Count > 0)
            {
                //Setting Categorization by Valid Tags
                var currentcategories = validtagsforcategories.Where(x =>
                    smgpoi.SmgTags.Select(y => y.ToLower()).Contains(x.Id.ToLower())
                );

                //Resetting Categories
                foreach (var languagecategory in languagelistcategories)
                {
                    if (smgpoi.AdditionalPoiInfos.ContainsKey(languagecategory))
                    {
                        if (smgpoi.AdditionalPoiInfos[languagecategory].Categories == null)
                        {
                            smgpoi.AdditionalPoiInfos[languagecategory].Categories =
                                new List<string>();
                        }
                        else
                        {
                            smgpoi.AdditionalPoiInfos[languagecategory].Categories.Clear();
                        }
                    }
                    else
                    {
                        smgpoi.AdditionalPoiInfos.TryAddOrUpdate(
                            languagecategory,
                            new AdditionalPoiInfos()
                            {
                                Language = languagecategory,
                                Categories = new List<string>(),
                            }
                        );
                    }
                }
                //Reassigning Categories
                foreach (var smgtagtotranslate in currentcategories)
                {
                    foreach (var languagecategory in languagelistcategories)
                    {
                        if (
                            smgtagtotranslate.TagName.ContainsKey(languagecategory)
                            && !smgpoi
                                .AdditionalPoiInfos[languagecategory]
                                .Categories.Contains(
                                    smgtagtotranslate.TagName[languagecategory].Trim()
                                )
                        )
                            smgpoi
                                .AdditionalPoiInfos[languagecategory]
                                .Categories.Add(smgtagtotranslate.TagName[languagecategory].Trim());
                    }
                }
            }
        }

        //GETS all tags for categorization tags from json as object to avoid DB call on each Tag update
        public static async Task<List<TagLinked>> GetAllODHTagsforCategorizationJson(string jsondir)
        {
            using (
                StreamReader r = new StreamReader(Path.Combine(jsondir, $"TagsForCategories.json"))
            )
            {
                string json = await r.ReadToEndAsync();

                return JsonConvert.DeserializeObject<List<TagLinked>>(json) ?? new();
            }
        }
    }
}
