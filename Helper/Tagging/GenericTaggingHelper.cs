// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataModel;
using Helper;
using Newtonsoft.Json;

namespace Helper
{
    public class GenericTaggingHelper
    {
        public static async Task AddTagIdsToODHActivityPoi(IIdentifiable mypgdata, List<TagLinked>? myalltaglist)
        {
            try
            {
                //Special get all Taglist and traduce it on import
                //var myalltaglist = await GetAllGenericTagsfromJson(jsondir);

                if (myalltaglist != null && ((ODHActivityPoiLinked)mypgdata).SmgTags != null)
                {
                    if (((ODHActivityPoiLinked)mypgdata).TagIds == null)
                        ((ODHActivityPoiLinked)mypgdata).TagIds = new List<string>();

                    foreach (
                        var translatedtag in GenerateNewTagIds(
                            ((ODHActivityPoiLinked)mypgdata).SmgTags ?? new List<string>(),
                            myalltaglist
                        )
                    )
                    {
                        if (!((ODHActivityPoiLinked)mypgdata).TagIds.Contains(translatedtag))
                            ((ODHActivityPoiLinked)mypgdata).TagIds.Add(translatedtag);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    JsonConvert.SerializeObject(
                        new UpdateResult
                        {
                            operation = "Tagging object creation",
                            updatetype = "single",
                            otherinfo = "",
                            id = mypgdata.Id,
                            message = "Tagging conversion failed",
                            created = 0,
                            updated = 0,
                            deleted = 0,
                            error = 1,
                            exception = ex.Message
                        }
                    )
                );
            }
        }

        //Translates OLD Tags with german keys to new English Tags
        public static ICollection<string> GenerateNewTagIds(
            ICollection<string> currenttags,
            List<TagLinked> alltaglist
        )
        {
            var returnList = new HashSet<string>();

            foreach (var tag in currenttags)
            {
                var tagstranslated = alltaglist
                    .Where(x =>
                        x.ODHTagIds != null && x.Source == "idm" && x.ODHTagIds.Any(y => y == tag)
                    )
                    .ToList();

                if (tagstranslated != null)
                {
                    foreach (var tagtranslated in tagstranslated)
                        returnList.Add(tagtranslated.Id);
                }
            }

            return returnList;
        }

        //GETS all generic tags from json as object to avoid DB call on each Tag update
        public static async Task<List<TagLinked>> GetAllGenericTagsfromJson(string jsondir)
        {
            using (StreamReader r = new StreamReader(Path.Combine(jsondir, $"GenericTags.json")))
            {
                string json = await r.ReadToEndAsync();

                return JsonConvert.DeserializeObject<List<TagLinked>>(json) ?? new();
            }
        }

        //public static async Task<List<ODHTagLinked>> GetAllGeneratedOdhTagsfromJson(string jsondir)
        //{
        //    using (StreamReader r = new StreamReader(Path.Combine(jsondir, $"ODHTagsSourceIDMLTS.json")))
        //    {
        //        string json = await r.ReadToEndAsync();

        //        return JsonConvert.DeserializeObject<List<ODHTagLinked>>(json) ?? new();
        //    }
        //}

        //GETS all generic tags from json as object to avoid DB call on each Tag update
        public static async Task<List<AllowedTags>> GetAllAutoPublishTagsfromJson(string jsondir)
        {
            using (
                StreamReader r = new StreamReader(Path.Combine(jsondir, $"AutoPublishTags.json"))
            )
            {
                string json = await r.ReadToEndAsync();

                return JsonConvert.DeserializeObject<List<AllowedTags>>(json) ?? new();
            }
        }


        //OBSOLETE
        //public static async Task AddTagsToODHActivityPoi(IIdentifiable mypgdata, string jsondir)
        //{
        //    try
        //    {
        //        //Special get all Taglist and traduce it on import
        //        var myalltaglist = await GetAllGenericTagsfromJson(jsondir);
        //        if (myalltaglist != null && ((ODHActivityPoiLinked)mypgdata).SmgTags != null)
        //        {
        //            ((ODHActivityPoiLinked)mypgdata).Tags = GenerateNewTags(
        //                ((ODHActivityPoiLinked)mypgdata).SmgTags ?? new List<string>(),
        //                myalltaglist
        //            );
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(
        //            JsonConvert.SerializeObject(
        //                new UpdateResult
        //                {
        //                    operation = "Tagging object creation",
        //                    updatetype = "single",
        //                    otherinfo = "",
        //                    id = mypgdata.Id,
        //                    message = "Tagging conversion failed: " + ex.Message,
        //                    recordsmodified = 0,
        //                    created = 0,
        //                    updated = 0,
        //                    deleted = 0,
        //                    success = false,
        //                }
        //            )
        //        );
        //    }
        //}





        ////Translates OLD Tags with german keys to new English Tags
        //public static List<Tags> GenerateNewTags(
        //    ICollection<string> currenttags,
        //    List<TagLinked> alltaglist
        //)
        //{
        //    var returnDict = new List<Tags>();

        //    foreach (var tag in currenttags)
        //    {
        //        var resultdict = TranslateMappingKey(tag, alltaglist);

        //        foreach (var kvp in resultdict)
        //        {
        //            var listtoadd = new List<Tags>();

        //            string type = kvp.Key == "lts" ? "ltscategory" : "odhcategory";
        //            string name =
        //                kvp.Value.TagName != null
        //                    ? kvp.Value.TagName.ContainsKey("en")
        //                        ? kvp.Value.TagName["en"]
        //                        : kvp.Value.TagName.FirstOrDefault().Value
        //                    : "";

        //            var tagtoadd = new Tags()
        //            {
        //                Id = kvp.Value.Id,
        //                Source = kvp.Key,
        //                Type = type,
        //                Name = name,
        //            };

        //            if (!listtoadd.Select(x => x.Id).Contains(tagtoadd.Id))
        //                listtoadd.Add(tagtoadd);

        //            //returnDict.TryAddOrUpdate(kvp.Key, listtoadd);
        //            returnDict.AddRange(listtoadd);
        //        }
        //    }

        //    return returnDict;
        //}



        //private static IDictionary<string, TagLinked> TranslateMappingKey(
        //    string germankey,
        //    List<TagLinked> alltaglist
        //)
        //{
        //    var returnDict = new Dictionary<string, TagLinked>();

        //    var tagen = alltaglist
        //        .Where(x => x.ODHTagIds != null && x.ODHTagIds.Any(y => y == germankey))
        //        .FirstOrDefault();

        //    if (tagen?.Id != null)
        //    {
        //        if (tagen.Types.Contains("ODHCategory") || tagen.Types.Contains("odhcategory"))
        //        {
        //            returnDict.Add("idm", tagen);
        //        }

        //        if (tagen.Types.Contains("LTSCategory") || tagen.Types.Contains("ltscategory"))
        //        {
        //            returnDict.Add("lts", tagen);
        //        }
        //    }

        //    return returnDict;
        //}

        ////Removes all special chars for the tag id
        //private static string RemoveSpecialCharsRegex(string id)
        //{
        //    var toreturn = id;

        //    //Change special chars hack
        //    toreturn = toreturn.Replace("é", "e");
        //    toreturn = toreturn.Replace("á", "a");
        //    toreturn = toreturn.Replace("í", "i");
        //    toreturn = toreturn.Replace("ó", "o");
        //    toreturn = toreturn.Replace("ú", "u");

        //    toreturn = toreturn.Replace("ä", "a");
        //    toreturn = toreturn.Replace("ö", "o");
        //    toreturn = toreturn.Replace("ü", "u");

        //    //Exclude all characters that does not match this pattern
        //    toreturn = Regex.Replace(toreturn, @"[^0-9a-zA-Z_ ]+", "");
        //    //Hack replace all double spaces
        //    toreturn = Regex.Replace(toreturn, @"  +", " ");

        //    return toreturn.Trim();
        //}


    }
}
