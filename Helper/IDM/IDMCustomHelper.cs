// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper.IDM
{
    public class IDMCustomHelper
    {
        #region IDM Activity Poi MetaInfo Generator

        public static void SetMetaInfoForActivityPoi(ODHActivityPoiLinked smgpoi, MetaInfosOdhActivityPoi metainfo)
        {
            try
            {
                ////TODO! use the Tags logic since this Types are no more used
                //var maintype = smgpoi.Type;
                //var subtype = smgpoi.SubType;
                //var poitype = smgpoi.PoiType;

                //List<string> subtypestonotupdate = new List<string>() {
                //            "Architektur",
                //            "Talradwege",
                //            "Downhill",
                //            "Architektur",
                //            "Ohne Zuordnung",
                //            "Familienurlaub",
                //            "Klettertour",
                //            "Stadtrundgang",
                //            "Allergiefreier Urlaub",
                //            "Wellnessbehandlungen",
                //            "Skigebiete"
                //        };

                //if (!subtypestonotupdate.Contains(subtype))
                //{
                //if (subtype == "Museen")
                //    poitype = "";
                //if (subtype == "Wandern")
                //    poitype = "";
                //if (subtype == "Weihnachtsmärkte")
                //    poitype = "";
                //if (subtype == "Rodeln")
                //    poitype = "";
                //if (subtype == "Langlaufen")
                //    poitype = "";
                //if (subtype == "Hütten & Almen")
                //    subtype = "";
                //if (poitype == "Downhill")
                //    poitype = "Freeride";

                foreach (var language in smgpoi.HasLanguage)
                {
                    var metainfolanguage = metainfo.Metainfos[language];

                    var mylisttocheck = GetTheODHTagsToCheckFor(smgpoi);

                    var rightmetainfo = FindFirstMetaInfoODHActivityPoiMatch(metainfolanguage, mylisttocheck);

                        //metainfolanguage.Where(x => x["Main Type"].ToString() == maintype && x["Sub-Type"].ToString() == subtype && x["POI Type"].ToString() == poitype).FirstOrDefault();

                    if (rightmetainfo != null)
                    {
                        if (smgpoi.HasLanguage.Contains(language))
                        {
                            //Meta Title
                            string metatitle1 = rightmetainfo["Title mit Platzhalter DE"].ToString();
                            string metatitle2 = rightmetainfo["Title mit Platzhalter DE 2"].ToString();
                            string metatitle3 = rightmetainfo["Title mit Platzhalter DE 3"].ToString();
                            string metatitle4 = rightmetainfo["Title mit Platzhalter DE 4"].ToString();

                            string generatedmeta = "";

                            if (!String.IsNullOrEmpty(metatitle1))
                            {
                                if (metatitle1.StartsWith("["))
                                {
                                    generatedmeta = GetTheRightFieldInfo(metatitle1, smgpoi, language);
                                }
                                else
                                {
                                    generatedmeta = metatitle1;
                                }
                            }
                            if (!String.IsNullOrEmpty(metatitle2))
                            {
                                if (metatitle2.StartsWith("["))
                                {
                                    generatedmeta = generatedmeta + GetTheRightFieldInfo(metatitle2, smgpoi, language);
                                }
                                else
                                {
                                    generatedmeta = generatedmeta + metatitle2;
                                }
                            }
                            if (!String.IsNullOrEmpty(metatitle3))
                            {
                                if (metatitle3.StartsWith("["))
                                {
                                    generatedmeta = generatedmeta + GetTheRightFieldInfo(metatitle3, smgpoi, language);
                                }
                                else
                                {
                                    generatedmeta = generatedmeta + metatitle3;
                                }
                            }
                            if (!String.IsNullOrEmpty(metatitle4))
                            {
                                if (metatitle4.StartsWith("["))
                                {
                                    generatedmeta = generatedmeta + GetTheRightFieldInfo(metatitle4, smgpoi, language);
                                }
                                else
                                {
                                    generatedmeta = generatedmeta + metatitle4;
                                }
                            }

                            smgpoi.Detail[language].MetaTitle = generatedmeta;

                            //Meta Desc
                            string metadesc1 = rightmetainfo["Description mit Platzhalter DE"].ToString();
                            string metadesc2 = rightmetainfo["Description mit Platzhalter DE 2"].ToString();
                            string metadesc3 = rightmetainfo["Description mit Platzhalter DE 3"].ToString();
                            string metadesc4 = rightmetainfo["Description mit Platzhalter DE 4"].ToString();
                            string metadesc5 = rightmetainfo["Description mit Platzhalter DE 5"].ToString();
                            string metadesc6 = rightmetainfo["Description mit Platzhalter DE 6"].ToString();
                            string metadesc7 = rightmetainfo["Description mit Platzhalter DE 7"].ToString();

                            string generatedmetadesc = "";

                            if (!String.IsNullOrEmpty(metadesc1))
                            {
                                if (metadesc1.StartsWith("["))
                                {
                                    generatedmetadesc = GetTheRightFieldInfo(metadesc1, smgpoi, language);
                                }
                                else
                                {
                                    generatedmetadesc = metadesc1;
                                }
                            }
                            if (!String.IsNullOrEmpty(metadesc2))
                            {
                                if (metadesc2.StartsWith("["))
                                {
                                    generatedmetadesc = generatedmetadesc + GetTheRightFieldInfo(metadesc2, smgpoi, language);
                                }
                                else
                                {
                                    generatedmetadesc = generatedmetadesc + metadesc2;
                                }
                            }
                            if (!String.IsNullOrEmpty(metadesc3))
                            {
                                if (metadesc3.StartsWith("["))
                                {
                                    generatedmetadesc = generatedmetadesc + GetTheRightFieldInfo(metadesc3, smgpoi, language);
                                }
                                else
                                {
                                    generatedmetadesc = generatedmetadesc + metadesc3;
                                }
                            }
                            if (!String.IsNullOrEmpty(metadesc4))
                            {
                                if (metadesc4.StartsWith("["))
                                {
                                    generatedmetadesc = generatedmetadesc + GetTheRightFieldInfo(metadesc4, smgpoi, language);
                                }
                                else
                                {
                                    generatedmetadesc = generatedmetadesc + metadesc4;
                                }
                            }
                            if (!String.IsNullOrEmpty(metadesc5))
                            {
                                if (metadesc5.StartsWith("["))
                                {
                                    generatedmetadesc = generatedmetadesc + GetTheRightFieldInfo(metadesc5, smgpoi, language);
                                }
                                else
                                {
                                    generatedmetadesc = generatedmetadesc + metadesc5;
                                }
                            }
                            if (!String.IsNullOrEmpty(metadesc6))
                            {
                                if (metadesc6.StartsWith("["))
                                {
                                    generatedmetadesc = generatedmetadesc + GetTheRightFieldInfo(metadesc6, smgpoi, language);
                                }
                                else
                                {
                                    generatedmetadesc = generatedmetadesc + metadesc6;
                                }
                            }
                            if (!String.IsNullOrEmpty(metadesc7))
                            {
                                if (metadesc7.StartsWith("["))
                                {
                                    generatedmetadesc = generatedmetadesc + GetTheRightFieldInfo(metadesc7, smgpoi, language);
                                }
                                else
                                {
                                    generatedmetadesc = generatedmetadesc + metadesc7;
                                }
                            }

                            smgpoi.Detail[language].MetaDesc = generatedmetadesc;

                        }
                    }

                }
                //}
            }
            catch (Exception ex)
            {

            }
        }

        public static Dictionary<string, object>? FindFirstMetaInfoODHActivityPoiMatch(List<Dictionary<string, object>> metainfoslanguage, List<string>? tags)
        {
            if (tags == null || metainfoslanguage == null)
                return null;

            //return metainfoslanguage.FirstOrDefault(dict =>
            //{
            //    // Extract values for the specified keys, filter out empty ones, and convert to lowercase
            //    var valuesToCheck = new[] { "Main Type", "Sub-Type", "POI Type" }
            //        .Where(key => dict.ContainsKey(key) && !string.IsNullOrWhiteSpace(dict[key].ToString()))
            //        .Select(key => dict[key].ToString().ToLower());

            //    // Check if all non-empty values exist in the second list
            //    return valuesToCheck.Any() && valuesToCheck.All(value => tags.Contains(value));
            //});

            var matchedmetainfos = metainfoslanguage.Where(dict =>
            {
                // Extract values for the specified keys, filter out empty ones, and convert to lowercase
                var valuesToCheck = new[] { "Main Type", "Sub-Type", "POI Type" }
                    .Where(key => dict.ContainsKey(key) && !string.IsNullOrWhiteSpace(dict[key].ToString()))
                    .Select(key => dict[key].ToString().ToLower());

                // Check if all non-empty values exist in the second list
                return valuesToCheck.Any() && valuesToCheck.All(value => tags.Contains(value));
            }).ToList();

            //If more are found how to get the right.
            if(matchedmetainfos.Count() >1)
            {
                List<Tuple<int, Dictionary<string, object>?>> matchedwithintersectcount = new List<Tuple<int, Dictionary<string, object>?>>();

                //We count how many matches are there
                foreach (var dictentry in matchedmetainfos)
                {
                    List<string> arraytocompare = new List<string>();
                    if (!String.IsNullOrEmpty(dictentry["Main Type"].ToString()))
                        arraytocompare.Add(dictentry["Main Type"].ToString().ToLower());
                    if (!String.IsNullOrEmpty(dictentry["Sub-Type"].ToString()))
                        arraytocompare.Add(dictentry["Sub-Type"].ToString().ToLower());
                    if (!String.IsNullOrEmpty(dictentry["POI Type"].ToString()))
                        arraytocompare.Add(dictentry["POI Type"].ToString().ToLower());

                    var intersectcount = tags.Intersect(arraytocompare).Count();

                    matchedwithintersectcount.Add(Tuple.Create(intersectcount, dictentry));
                }

                return matchedwithintersectcount.OrderByDescending(x => x.Item1).FirstOrDefault().Item2;
            }
            else
                return matchedmetainfos.FirstOrDefault();
        }

        private static string GetTheRightFieldInfo(string fieldtoretrieve, ODHActivityPoiLinked currentpoi, string lang)
        {

            switch (fieldtoretrieve.Trim())
            {
                case "[title]":
                    return currentpoi.Detail[lang].Title;

                case "[city]":

                    bool returncontactcity = false;
                    string returnstring = "";

                    //Wenn Locationinfo Gemeinde existiert nimm diese
                    if (lang != "ru")
                    {
                        if (currentpoi.LocationInfo != null)
                        {
                            if (currentpoi.LocationInfo.MunicipalityInfo != null)
                            {
                                if (currentpoi.LocationInfo.MunicipalityInfo.Name[lang] != null)
                                {
                                    returnstring = currentpoi.LocationInfo.MunicipalityInfo.Name[lang];
                                }
                                else
                                {
                                    returncontactcity = true;
                                }
                            }
                            else
                                returncontactcity = true;
                        }
                        else
                            returncontactcity = true;

                        if (returncontactcity)
                        {
                            //Falls nichts vorhanden ist nehmen wir die contactinfo city

                            if (lang == "en" || lang == "nl" || lang == "cs" || lang == "pl" || lang == "fr" || lang == "ru")
                            {
                                if (currentpoi.ContactInfos.ContainsKey("it"))
                                    returnstring = currentpoi.ContactInfos["it"].City;
                            }
                            else
                            {
                                if (currentpoi.ContactInfos.ContainsKey(lang))
                                    returnstring = currentpoi.ContactInfos[lang].City;
                            }
                        }



                    }
                    else if (lang == "ru")
                    {
                        //NEU
                        bool returncontactcityru = false;


                        if (currentpoi.LocationInfo.MunicipalityInfo != null)
                        {
                            if (currentpoi.LocationInfo.MunicipalityInfo.Name[lang] != null)
                            {
                                returnstring = currentpoi.LocationInfo.MunicipalityInfo.Name[lang] + "/" + currentpoi.LocationInfo.MunicipalityInfo.Name["de"];
                            }
                            else
                            {
                                returncontactcityru = true;
                            }
                        }
                        else
                            returncontactcityru = true;

                        if (returncontactcityru)
                        {
                            //Falls nichts vorhanden ist nehmen wir die contactinfo city
                            if (currentpoi.ContactInfos.ContainsKey("de"))
                                returnstring = currentpoi.ContactInfos["de"].City;
                        }
                    }

                    return returnstring;

                case "[höchstgelegener punkt]":
                    return currentpoi.AltitudeHighestPoint.ToString() + " m";

                default:
                    return "";
            }

        }

        private static List<string> GetTheODHTagsToCheckFor(ODHActivityPoiLinked smgpoi)
        {
            var taglistcleared = smgpoi.SmgTags.ToList();            

            //Add here all Exceptions
            if (smgpoi.Source == "suedtirolwein")
                taglistcleared = new List<string>() { "essen trinken", "weinkellereien" };


            return taglistcleared;
        }

        #endregion

        #region IDM Accommodation MetaInfo Generator

        public static void SetMetaInfoForAccommodation(AccommodationV2 accommodation, MetaInfosOdhActivityPoi metainfo)
        {
            try
            {
                //Setting MetaInfo
                BuildAccoMetaTitle(accommodation, metainfo);
                BuildAccoMetaDescription(accommodation, metainfo);
            }
            catch (Exception ex)
            {

            }
        }

        //RULE Title: [unterkunft], [ort] buchen | suedtirol.info (Vale sempre)
        public static void BuildAccoMetaTitle(AccommodationV2 myacco, MetaInfosOdhActivityPoi metainfoacco)
        {
            foreach (var language in myacco.HasLanguage)
            {
                if (metainfoacco != null)
                {
                    if (myacco.AccoDetail.ContainsKey(language))
                    {
                        var title = metainfoacco.Metainfos[language].FirstOrDefault()["Title"].ToString();

                        var acconame = myacco.AccoDetail[language].Name;
                        var accoplace = myacco.AccoDetail[language].City;

                        title = title.Replace("[AccoDetail." + language + ".Name]", acconame).Replace("[AccoDetail." + language + ".City]", accoplace);

                        myacco.AccoDetail[language].MetaTitle = title;
                    }
                }
            }
        }

        //RULE (Solo se il campo “short description” è compilato) Default Description: [short description]
        //     (Se il campo „short description“ non è compilato) Description fallback: Erfahren Sie mehr zu unserem Angebot für Ihren Urlaub in [unterkunft], [ort] buchen auf ► suedtirol.info
        public static void BuildAccoMetaDescription(AccommodationV2 myacco, MetaInfosOdhActivityPoi metainfoacco)
        {
            foreach (var language in myacco.HasLanguage)
            {
                if (metainfoacco != null)
                {
                    if (myacco.AccoDetail.ContainsKey(language))
                    {
                        //Hack Longdesc and Shortdesc are the opposite
                        if (!String.IsNullOrEmpty(myacco.AccoDetail[language].Shortdesc))
                            myacco.AccoDetail[language].MetaDesc = myacco.AccoDetail[language].Shortdesc;
                        else
                        {
                            //Erfahren Sie mehr zu unserem Angebot für Ihren Urlaub in [unterkunft], [ort] buchen auf ► suedtirol.info
                            //Description1 [unterkunft], [ort] Description2

                            var desc = metainfoacco.Metainfos[language].FirstOrDefault()["Description"].ToString();

                            var acconame = myacco.AccoDetail[language].Name;
                            var accoplace = myacco.AccoDetail[language].City;

                            desc = desc.Replace("[AccoDetail." + language + ".Name]", acconame).Replace("[AccoDetail." + language + ".City]", accoplace);

                            myacco.AccoDetail[language].MetaDesc = desc;
                        }
                    }
                }
            }
        }


        #endregion
    }
}
