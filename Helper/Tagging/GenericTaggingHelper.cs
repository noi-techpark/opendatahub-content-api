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
                            message = "Tagging conversion failed: " + ex.Message,
                            recordsmodified = 0,
                            created = 0,
                            updated = 0,
                            deleted = 0,
                            success = false,
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
            var filePath = Path.Combine(jsondir, "GenericTags.json");

            if (!File.Exists(filePath))
                return new();

            try
            {
                using var r = new StreamReader(filePath);
                string json = await r.ReadToEndAsync();
                return JsonConvert.DeserializeObject<List<TagLinked>>(json) ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to read GenericTags.json: {ex.Message}");
                return new();
            }
        }

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
    }
}
