// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using System;
using System.Collections.Generic;
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
                //TODO! use the Tags logic since this Types are no more used
                var maintype = smgpoi.Type;
                var subtype = smgpoi.SubType;
                var poitype = smgpoi.PoiType;

                List<string> subtypestonotupdate = new List<string>() {
                            "Architektur",
                            "Talradwege",
                            "Downhill",
                            "Architektur",
                            "Ohne Zuordnung",
                            "Familienurlaub",
                            "Klettertour",
                            "Stadtrundgang",
                            "Allergiefreier Urlaub",
                            "Wellnessbehandlungen",
                            "Skigebiete"
                        };

                if (!subtypestonotupdate.Contains(subtype))
                {
                    if (subtype == "Museen")
                        poitype = "";
                    if (subtype == "Wandern")
                        poitype = "";
                    if (subtype == "Weihnachtsmärkte")
                        poitype = "";
                    if (subtype == "Rodeln")
                        poitype = "";
                    if (subtype == "Langlaufen")
                        poitype = "";
                    if (subtype == "Hütten & Almen")
                        subtype = "";
                    if (poitype == "Downhill")
                        poitype = "Freeride";

                    foreach (var language in smgpoi.HasLanguage)
                    {
                        var metainfolanguage = metainfo.Metainfos[language];

                        var rightmetainfo = metainfolanguage.Where(x => x["Main Type"].ToString() == maintype && x["Sub-Type"].ToString() == subtype && x["POI Type"].ToString() == poitype).FirstOrDefault();

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
                }
            }
            catch (Exception ex)
            {

            }
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

        #endregion
    }
}
