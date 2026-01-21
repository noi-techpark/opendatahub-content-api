// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Transform;
using DataModel;
using Helper.Extensions;

namespace Helper
{
    public static class PublishedOnHelper
    {
        /// TO CHECK: Create a Generic PublishedOnCreator that is executed in Upsert Method. Means that manual assignment is no more possible on not imported data?
        
        /// <summary>
        /// Create the publishedon List
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mydata"></param>
        /// <param name="allowedtags"></param>
        /// <param name="activatesourceonly"></param>
        public static void CreatePublishedOnList<T>(
            this T mydata,
            ICollection<AllowedTags>? allowedtags = null,
            Tuple<string, bool>? activatesourceonly = null
        )
            where T : IIdentifiable, IMetaData, ISource, IPublishedOn
        {
            //alowedsources  Dictionary<odhtype, sourcelist> TODO Export in Config
            Dictionary<string, List<string>> allowedsourcesMP = new Dictionary<
                string,
                List<string>
            >()
            {
                {
                    "event",
                    new List<string>() { "lts", "drin", "trevilab" }
                },
                {
                    "accommodation",
                    new List<string>() { "lts" }
                },
                {
                    "odhactivitypoi",
                    new List<string>() { "lts", "suedtirolwein", "archapp" }
                },
            };

            //Blacklist for exceptions Dictionary<string, Tuple<string,string> TODO Export in Config
            Dictionary<string, Tuple<string, string>> blacklistsourcesandtagsMP = new Dictionary<
                string,
                Tuple<string, string>
            >()
            {
                { "odhactivitypoi", Tuple.Create("lts", "weinkellereien") },
            };

            //Whitelist on Types Deprecated? TODO Export in Config
            Dictionary<string, List<string>> allowedtypesMP = new Dictionary<string, List<string>>()
            {
                {
                    "article",
                    new List<string>() { "rezeptartikel" }
                },
            };

            //Blacklist TVs
            Dictionary<string, List<string>> notallowedtvs = new Dictionary<string, List<string>>()
            {                
                {
                    "odhactivitypoi",
                    new List<string>()
                    {
                        "F68D877B11916F39E6413DFB744259EB", //Obertilliach
                        "3063A07EFE5EC4D357FCB6C5128E81F0", //Cortina
                        "3629935C546A49328842D3E0E9150CE8", //Osttirol
                        "E9D7583EECBA480EA073C4F8C030E83C", //Sappada
                        "9FA380DE9937C1BB64844076674968E2", //Lorenzago Auronzo Misurina 
                        "6B39D0B4DD4CCAE6477F7013B090784C", //Cadore
                        "F7D7AAEC0313487B9CE8EC9067E43B73", //Arabba
                        "E1407CED66C14AABBF49532AA49C76A6", //Alleghe
                        "7D208AA1374F1484A2483829207C9421", //TEST Owner
                        "0E8FFB31CCFC31D92C6F396134D2F1FC ", //Tourismusverband Tiroler Oberland
                        "959F253373BE9B97A753EA19D274ECAE ", //Engadin Scuol Zernez
                        "876CF2F5FEBF5D82619BF10D91A1F8D3"   //Mischuns
                    }
                },
            };

            //Blaklisted Areas
            Dictionary<string, List<string>> notallowedarearids = new Dictionary<string, List<string>>()
            {
                {
                    "odhactivitypoi",
                    new List<string>()
                    {                        
                        "CF0F2EE94A23ED0CEE50EFE674EB1B2C", //Obertilliach
                        "78DC6AA57BCC4647AF3925BD1197C418", // Osttirol
                        "EB08528A2DBA406188A4C85012A28195", // PelmoSkiCivetta
                        "2F327A4A8AAB82A867234D9A7C2F6605", //Veneto
                        "98910F5EBED441F986F2B19833C28B10", //Marmolada
                        "3629935C546A49328842D3E0E9150CE8", //Auronzo - Misurina - Lorenzago
                        "B6021F468FD24DCE92E3B6BEC93FBD83", //Cortina
                        "0646D169CCA842E38B981790A08D3AF5", //Arabba
                        "43E671500E21477F8D682E8767D812A8", //Comelico–Sappada
                        "FDC26773609E139FCC76F4C6FA6A2F72", //Testarea LTS
                        "94187AC0AB734AC087BA54A9E3C910E4", //Auronzo–Misurina–Lorenzago
                        "C5CA26AD0DCA477E8011FE12A6DDF02C", //Forni di Sopra
                        "835FD7E072ED47FCA0F27F6EE053AD9F", //Engadin Scuol Zernez
                        "79178AB1F14E4701B96127DE73F5A8A0", //Minschuns (Müstair)
                        "2861C7E0C4C04BDF942B99417D44FDA2", //Arabba
                    }
                },
            };


            List<string> publishedonlist = new List<string>();

            var typeswitcher = ODHTypeHelper.TranslateType2TypeString<T>(mydata);

            if (mydata != null)
            {
                switch (typeswitcher)
                {
                    //Accommodations smgactive (Source LTS IDMActive)
                    case "accommodation":
                        if (
                            (mydata as AccommodationLinked).SmgActive
                            && allowedsourcesMP[mydata._Meta.Type].Contains(mydata._Meta.Source)
                        )
                        {                            
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    //Accommodation Room publishedon
                    case "accommodationroom":

                        //TO check add publishedon logic only for rooms with source hgv? for online bookable accommodations?                        

                        if (activatesourceonly != null && activatesourceonly.Item2 == true)
                        {
                            if (
                                activatesourceonly.Item1
                                == (mydata as AccommodationRoomLinked)._Meta.Source
                            )
                            {
                                publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                            }
                        }
                        else
                        {
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }

                        break;
                    
                    //Event Add all Active Events from now
                    case "event":

                        if (mydata is EventLinked)
                        {
                            bool validranc = true;
                            bool validclassification = false;

                            //EVENTS LTS
                            if (
                                (mydata as EventLinked).Active
                                && allowedsourcesMP[mydata._Meta.Type].Contains(mydata._Meta.Source)
                            )
                            {
                                //Publisher Rule if C9475CF585664B2887DE543481182A2D with Ranc 1 is there do not publish
                                if((mydata as EventLinked).EventPublisher != null &&
                                    (mydata as EventLinked).EventPublisher.Where(x => x.PublisherRID == "C9475CF585664B2887DE543481182A2D").Count() > 0)
                                {
                                    if ((mydata as EventLinked).EventPublisher.Where(x => x.PublisherRID == "C9475CF585664B2887DE543481182A2D").FirstOrDefault().Ranc == 1)
                                        validranc = false;
                                }

                                //Marketplace Events only ClassificationRID
                                var validclassificationrids = new List<string>()
                                {
                                    "CE212B488FA14954BE91BBCFA47C0F06",
                                };
                                if (
                                    validclassificationrids.Contains(
                                        (mydata as EventLinked).ClassificationRID
                                    )
                                    && mydata._Meta.Source == "lts"
                                )
                                    validclassification = true;



                                if(validclassification && validranc)
                                    publishedonlist.TryAddOrUpdateOnList("idm-marketplace");

                                //Events DRIN CENTROTREVI
                                if (
                                    (mydata as EventLinked).Active
                                    && (
                                        mydata._Meta.Source == "trevilab"
                                        || mydata._Meta.Source == "drin"
                                    )
                                )
                                {
                                    if ((mydata as EventLinked).SmgActive)
                                    {
                                        if (mydata._Meta.Source == "drin")
                                            publishedonlist.TryAddOrUpdateOnList(
                                                "centro-trevi.drin"
                                            );
                                        if (mydata._Meta.Source == "trevilab")
                                            publishedonlist.TryAddOrUpdateOnList(
                                                "centro-trevi.trevilab"
                                            );
                                    }
                                }
                            }
                        }
                        else if (mydata is EventFlattened)
                        {
                            bool validranc = true;
                            bool validclassification = false;

                            //EVENTS LTS
                            if (
                                (mydata as EventFlattened).Active
                                && allowedsourcesMP[mydata._Meta.Type].Contains(mydata._Meta.Source)
                            )
                            {
                                //Marketplace Events only ClassificationRID CE212B488FA14954BE91BBCFA47C0F06
                                var validclassificationrids = new List<string>()
                                {
                                    "CE212B488FA14954BE91BBCFA47C0F06",
                                };

                                if (mydata._Meta.Source == "lts" && 
                                    (mydata as EventFlattened).Mapping.ContainsKey("lts") && 
                                    (mydata as EventFlattened).Mapping["lts"].ContainsKey("ClassificationRID") &&
                                    validclassificationrids.Contains((mydata as EventFlattened).Mapping["lts"]["ClassificationRID"]))
                                {
                                    validclassification = true;                                            
                                }

                                //Publisher Rule if C9475CF585664B2887DE543481182A2D with Ranc 1 is there do not publish
                                //if (mydata._Meta.Source == "lts" &&
                                //    (mydata as EventV2)..ContainsKey("lts") &&
                                //    (mydata as EventV2).Mapping["lts"].ContainsKey("ClassificationRID") &&
                                //    validclassificationrids.Contains((mydata as EventV2).Mapping["lts"]["ClassificationRID"]))
                                //{
                                //    validranc = false;
                                //}

                                if (validranc && validclassification)
                                    publishedonlist.TryAddOrUpdateOnList("idm-marketplace");


                                //Events DRIN CENTROTREVI
                                if (
                                    (mydata as EventFlattened).Active
                                    && (
                                        mydata._Meta.Source == "trevilab"
                                        || mydata._Meta.Source == "drin"
                                    )
                                )
                                {
                                    if (mydata._Meta.Source == "drin")
                                        publishedonlist.TryAddOrUpdateOnList("centro-trevi.drin");
                                    if (mydata._Meta.Source == "trevilab")
                                        publishedonlist.TryAddOrUpdateOnList(
                                            "centro-trevi.trevilab"
                                        );
                                }
                            }
                        }

                        break;

                    //ODHActivityPoi
                    case "odhactivitypoi":

                        if (
                            (mydata as ODHActivityPoiLinked).Active
                            && mydata._Meta.Source == "suedtirolwein"
                        )
                            publishedonlist.TryAddOrUpdateOnList("suedtirolwein.com");


                        if (
                            (mydata as ODHActivityPoiLinked).Active
                            && allowedsourcesMP[mydata._Meta.Type].Contains(mydata._Meta.Source)
                        )
                        {
                            //ODHActivityPoi Rules
                            //Check if TV is allowed, Check if Owner is allowed, Check if Tag is allowed
                            if((mydata as ODHActivityPoiLinked).SyncSourceInterface != "gastronomicdata")
                            {
                                //Check if LocationInfo is in one of the blacklistedtv
                                bool tvallowed = true;
                                if ((mydata as ODHActivityPoiLinked).TourismorganizationId != null)
                                    tvallowed =
                                        notallowedtvs[mydata._Meta.Type]
                                            .Where(x =>
                                                x.Contains(
                                                    (
                                                        mydata as ODHActivityPoiLinked
                                                    ).TourismorganizationId
                                                )
                                            )
                                            .Count() > 0
                                            ? false
                                            : true;

                                //If there are only blacklisted areas assigned TEST
                                bool hasmorethanblacklistedarea = true;
                                if (mydata._Meta.Source == "lts" && (mydata as ODHActivityPoiLinked).AreaId != null)
                                {
                                    hasmorethanblacklistedarea = (mydata as ODHActivityPoiLinked).AreaId.Except(notallowedarearids[mydata._Meta.Type]).Count() > 0 ? true : false;
                                }                                    

                                //bool locinfonotemptyandsourcelts = true;
                                //if (mydata._Meta.Source == "lts")
                                //{
                                //    //IF data is from Source LTS and has no Locationinfo (outside of South Tyrol) deactivate it
                                //    if((mydata as ODHActivityPoiLinked).LocationInfo == null ||
                                //        (
                                //            (mydata as ODHActivityPoiLinked).LocationInfo.TvInfo == null &&
                                //            (mydata as ODHActivityPoiLinked).LocationInfo.RegionInfo == null &&
                                //            (mydata as ODHActivityPoiLinked).LocationInfo.DistrictInfo == null &&
                                //            (mydata as ODHActivityPoiLinked).LocationInfo.MunicipalityInfo == null
                                //        ))
                                //    {
                                //        locinfonotemptyandsourcelts = false;
                                //    }                                    
                                //}

                                //IF category is white or blacklisted find an intersection
                                var tagintersection = (mydata as ODHActivityPoiLinked).SmgTags != null ? allowedtags                                    
                                    .Select(x => x.Id)
                                    .ToList()
                                    .Intersect((mydata as ODHActivityPoiLinked).SmgTags) : new List<string>();

                                if (tagintersection.Count() > 0 && tvallowed && hasmorethanblacklistedarea)
                                {
                                    var blacklistedpublisher = new List<string>();

                                    List<string> publisherstoadd = new List<string>();

                                    foreach (var intersectedtag in tagintersection)
                                    {
                                        var myallowedtag = allowedtags
                                            .Where(x => x.Id == intersectedtag)
                                            .FirstOrDefault();

                                        foreach (var publishon in myallowedtag.PublishDataWithTagOn)
                                        {
                                            //Marked as blacklist overwrites whitelist
                                            if (publishon.Value == false)
                                                blacklistedpublisher.Add(publishon.Key);

                                            if (
                                                blacklistsourcesandtagsMP[mydata._Meta.Type] != null
                                                && blacklistsourcesandtagsMP[mydata._Meta.Type].Item1
                                                    == mydata._Meta.Source
                                                && (mydata as ODHActivityPoiLinked).SmgTags.Contains(
                                                    blacklistsourcesandtagsMP[mydata._Meta.Type].Item2
                                                )
                                            )
                                                blacklistedpublisher.Add("idm-marketplace");

                                            if (!blacklistedpublisher.Contains(publishon.Key))
                                            {
                                                if (!publisherstoadd.Contains(publishon.Key))
                                                {
                                                    publisherstoadd.Add(publishon.Key);
                                                }
                                            }
                                        }
                                    }

                                    foreach (var publishertoadd in publisherstoadd)
                                    {
                                        publishedonlist.TryAddOrUpdateOnList(publishertoadd);
                                    }
                                }
                            }

                            //Gastronomy Rules
                            //If this is a gastronomy and representationmode is not set to full activate it for MP                                                        
                            if ((mydata as ODHActivityPoiLinked).SyncSourceInterface == "gastronomicdata")
                            {
                                if ((mydata as ODHActivityPoiLinked).Mapping != null &&
                                    (mydata as ODHActivityPoiLinked).Mapping.ContainsKey("lts") &&
                                    (mydata as ODHActivityPoiLinked).Mapping["lts"] != null &&
                                    (mydata as ODHActivityPoiLinked).Mapping["lts"].ContainsKey("representationMode") &&
                                    (mydata as ODHActivityPoiLinked).Mapping["lts"]["representationMode"] == "full"
                                    )
                                {
                                    publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                                }
                            }                            
                        }

                        break;

                    //EventShort
                    case "eventshort":

                        //Set Publishers in base of Displays
                        //Eurac Videowall
                        if ((mydata as EventShort).Display1 == "Y")
                            publishedonlist.TryAddOrUpdateOnList("eurac-videowall");
                        if ((mydata as EventShort).Display1 == "N")
                            publishedonlist.TryRemoveOnList("eurac-videowall");
                        //Eurac Videowall
                        if ((mydata as EventShort).Display2 == "Y")
                            publishedonlist.TryAddOrUpdateOnList("eurac-seminarroom");
                        if ((mydata as EventShort).Display2 == "N")
                            publishedonlist.TryRemoveOnList("eurac-seminarroom");
                         //Eurac Videowall
                        if ((mydata as EventShort).Display3 == "Y")
                            publishedonlist.TryAddOrUpdateOnList("noi-totem");
                        if ((mydata as EventShort).Display3 == "N")
                            publishedonlist.TryRemoveOnList("noi-totem");
                        //today.noi.bz.it
                        if ((mydata as EventShort).Display4 == "Y")
                            publishedonlist.TryAddOrUpdateOnList("today.noi.bz.it");
                        if ((mydata as EventShort).Display4 == "N")
                            publishedonlist.TryRemoveOnList("today.noi.bz.it");



                        //Readd publishers that were there before not assigned automatically
                        if (mydata.PublishedOn != null &&
                            mydata.PublishedOn.Except(
                                new List<string> {
                                    "eurac-videowall",
                                    "eurac-seminarroom",
                                    "today.noi.bz.it",
                                    "noi-totem" }).Count() > 0)
                        {
                            publishedonlist.AddRange(mydata.PublishedOn.Except(
                                new List<string> {
                                    "eurac-videowall",
                                    "eurac-seminarroom",
                                    "today.noi.bz.it",
                                    "noi-totem" }));
                        }
                            
                        break;

                    case "measuringpoint":
                        if (mydata is MeasuringpointLinked)
                        {
                            if ((mydata as MeasuringpointLinked).Active)
                            {
                                publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                            }
                        }
                        else if (mydata is MeasuringpointV2)
                        { 
                            if ((mydata as MeasuringpointV2).Active)
                            {
                                publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                            }                        
                        }
                        break;

                    case "venue":
                        if (mydata is VenueLinked)
                        {
                            if ((mydata as VenueLinked).Active == true)
                            {
                                publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                            }
                        }
                        else if (mydata is VenueV2)
                        {
                            if ((mydata as VenueV2).Active == true)
                            {
                                publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                            }
                        }
                        else if (mydata is VenueFlattened)
                        {
                            if ((mydata as VenueFlattened).Active == true)
                            {
                                //Venues LTS
                                //Venues NOI
                                //Venues DRIN CENTROTREVI
                                if (
                                    mydata._Meta.Source == "trevilab"
                                    || mydata._Meta.Source == "drin"
                                )
                                {
                                    if (mydata._Meta.Source == "drin")
                                        publishedonlist.TryAddOrUpdateOnList("centro-trevi.drin");
                                    if (mydata._Meta.Source == "trevilab")
                                        publishedonlist.TryAddOrUpdateOnList(
                                            "centro-trevi.trevilab"
                                        );
                                }
                            }
                        }
                        break;

                    case "webcam":
                        //Currently no idm-marketplace needed
                        //if ((mydata as WebcamInfoLinked).Active == true)
                        //{                            
                        //    publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        //}
                        break;

                    case "wineaward":
                        if ((mydata as WineLinked).Active == true)
                        {                            
                            publishedonlist.TryAddOrUpdateOnList("suedtirolwein.com");
                            //publishedonlist.TryAddOrUpdateOnList("idm-marketplace"); /??
                        }
                        break;

                    case "region":
                        if ((mydata as RegionLinked).Active)
                        {                            
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "tourismassociation":
                        if ((mydata as TourismvereinLinked).Active)
                        {                            
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "district":
                        if ((mydata as DistrictLinked).Active)
                        {                            
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "municipality":
                        if ((mydata as MunicipalityLinked).Active)
                        {                            
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "metaregion":
                        if ((mydata as MetaRegionLinked).Active)
                        {                            
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "area":
                        if ((mydata as AreaLinked).Active)
                        {                            
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "skiarea":
                        if ((mydata as SkiAreaLinked).Active)
                        {                            
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "skiregion":
                        if ((mydata as SkiRegionLinked).Active)
                        {                            
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "experiencearea":
                        if ((mydata as ExperienceAreaLinked).Active)
                        {                            
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "article":
                        var article = (mydata as ArticlesLinked);

                        if (
                            article.SmgActive
                            && allowedtypesMP[mydata._Meta.Type].Contains(article.Type.ToLower())
                        )
                        {                            
                        }
                        break;

                    case "odhtag":
                        var odhtag = (mydata as ODHTagLinked);
                        if (
                            odhtag != null
                            && odhtag.DisplayAsCategory != null
                            && odhtag.DisplayAsCategory.Value == true
                        )
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        break;

                    //obsolete do nothing

                    case "ltsactivity":
                        break;

                    case "ltspoi":
                        break;

                    case "ltsgastronomy":
                        break;

                    default:

                        break;
                }

                mydata.PublishedOn = publishedonlist;
            }
        }
    }
}
