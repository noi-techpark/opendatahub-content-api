// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataModel;

namespace Helper
{
    public static class ODHActivityPoiHelper
    {
        //TO CHECK IF THIS WAS USED?
        public static void SetSustainableHikingTag(
            ODHActivityPoiLinked mysmgpoi,
            List<string> languagelist
        )
        {
            //NEW if the field "PublicTransportationInfo" is not empty, set the new ODHTag "sustainable-hiking".

            if (mysmgpoi.SmgTags != null && mysmgpoi.SmgTags.Contains("Wandern"))
            {
                bool haspublictrasportationinfo = false;
                if (mysmgpoi.Detail != null)
                {
                    foreach (var languagecategory in languagelist)
                    {
                        if (
                            mysmgpoi.Detail.ContainsKey(languagecategory)
                            && !String.IsNullOrEmpty(
                                mysmgpoi.Detail[languagecategory].PublicTransportationInfo
                            )
                        )
                        {
                            haspublictrasportationinfo = true;
                        }
                    }
                }
                if (haspublictrasportationinfo)
                {
                    if (mysmgpoi.SmgTags == null)
                        mysmgpoi.SmgTags = new List<string>();

                    if (!mysmgpoi.SmgTags.Contains("sustainable-hiking"))
                        mysmgpoi.SmgTags.Add("sustainable-hiking");
                }
                else if (!haspublictrasportationinfo)
                {
                    if (mysmgpoi.SmgTags != null)
                    {
                        if (mysmgpoi.SmgTags.Contains("sustainable-hiking"))
                            mysmgpoi.SmgTags.Remove("sustainable-hiking");
                    }
                }
            }
        }

        public static void SetMainCategorizationForODHActivityPoi(ODHActivityPoiLinked smgpoi)
        {
            //Add LTS Id as Mapping
            var maintype = "Poi";

            if (smgpoi.SyncSourceInterface.ToLower() == "activitydata")
                maintype = "activity";
            if (smgpoi.SyncSourceInterface.ToLower() == "poidata")
                maintype = "poi";
            if (smgpoi.SyncSourceInterface.ToLower() == "gastronomicdata")
                maintype = "gastronomy";
            if (smgpoi.SyncSourceInterface.ToLower() == "beacondata")
                maintype = "poi";
            if (smgpoi.SyncSourceInterface.ToLower() == "archapp")
                maintype = "poi";
            if (smgpoi.SyncSourceInterface.ToLower() == "museumdata")
                maintype = "poi";
            if (smgpoi.SyncSourceInterface.ToLower() == "suedtirolwein")
                maintype = "gastronomy";
            if (smgpoi.SyncSourceInterface.ToLower() == "common")
                maintype = "activity";
            if (smgpoi.SyncSourceInterface.ToLower() == "none")
                maintype = "poi";
            if (smgpoi.SyncSourceInterface.ToLower() == "magnolia")
                maintype = "poi";

            if (
                !smgpoi.SmgTags.Contains("activity")
                && !smgpoi.SmgTags.Contains("poi")
                && !smgpoi.SmgTags.Contains("gastronomy")
            )
            {
                //Assign to SMGTags if not there
                if (!smgpoi.SmgTags.Contains(maintype))
                    smgpoi.SmgTags.Add(maintype);
            }
        }

        /// <summary>
        /// ReAdds all Tags that where not assigned from a list of sources
        /// </summary>
        /// <param name="poiNew"></param>
        /// <param name="poiOld"></param>
        /// <returns></returns>
        public static async Task MergeActivityTags(ODHActivityPoiLinked poiNew, ODHActivityPoiLinked poiOld, List<string> tagsourcestoremove)
        {
            if (poiOld != null)
            {
                //Readd all Redactional Tags to check if this query fits
                var redactionalassignedTags = poiOld.Tags != null ? poiOld.Tags.Where(x => tagsourcestoremove.Contains(x.Source)).ToList() : null;
                if (redactionalassignedTags != null)
                {
                    foreach (var tag in redactionalassignedTags)
                    {
                        poiNew.TagIds.Add(tag.Id);
                    }
                }
            }

            //TODO import the Redactional Tags from SmgTags into Tags?

            //TODO same procedure on Tags? (Remove all Tags that come from the sync and readd the redactional assigned Tags)
        }

    }
}
