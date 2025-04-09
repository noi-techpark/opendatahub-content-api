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

            //Blaklisted OwnerRIDs
            Dictionary<string, List<string>> notallowedownerrids = new Dictionary<string, List<string>>()
            {
                {
                    "odhactivitypoi",
                    new List<string>()
                    {
                        "0E8FFB31CCFC31D92C6F396134D2F1FC", // Tourismusverband Tiroler Oberland - Infobüro Nauders                      
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
                            publishedonlist.TryAddOrUpdateOnList("suedtirol.info");
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    //Accommodation Room publishedon
                    case "accommodationroom":

                        //TO check add publishedon logic only for rooms with source hgv? for online bookable accommodations?

                        publishedonlist.TryAddOrUpdateOnList("suedtirol.info");

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
                            //EVENTS LTS
                            if (
                                (mydata as EventLinked).Active
                                && allowedsourcesMP[mydata._Meta.Type].Contains(mydata._Meta.Source)
                            )
                            {
                                //not needed anymore
                                //if (
                                //    (mydata as EventLinked).SmgActive
                                //    && mydata._Meta.Source == "lts"
                                //)
                                //    publishedonlist.TryAddOrUpdateOnList("suedtirol.info");

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
                        else if (mydata is EventV2)
                        {
                            //EVENTS LTS
                            if (
                                (mydata as EventV2).Active
                                && allowedsourcesMP[mydata._Meta.Type].Contains(mydata._Meta.Source)
                            )
                            {
                                //Marketplace Events only ClassificationRID
                                var validclassificationrids = new List<string>()
                                {
                                    "CE212B488FA14954BE91BBCFA47C0F06",
                                };

                                if ((mydata as EventV2).Mapping.ContainsKey("lts"))
                                {
                                    if (
                                        (mydata as EventV2)
                                            .Mapping["lts"]
                                            .ContainsKey("ClassificationRID")
                                    )
                                    {
                                        if (
                                            validclassificationrids.Contains(
                                                (mydata as EventV2).Mapping["lts"][
                                                    "ClassificationRID"
                                                ]
                                            )
                                            && mydata._Meta.Source == "lts"
                                        )
                                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                                    }
                                }

                                //Events DRIN CENTROTREVI
                                if (
                                    (mydata as EventV2).Active
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

                        if ((mydata as ODHActivityPoiLinked).SmgActive)
                            publishedonlist.TryAddOrUpdateOnList("suedtirol.info");
                        if (
                            (mydata as ODHActivityPoiLinked).SmgActive
                            && mydata._Meta.Source == "suedtirolwein"
                        )
                            publishedonlist.TryAddOrUpdateOnList("suedtirolwein.com");

                        if (
                            (mydata as ODHActivityPoiLinked).Active
                            && allowedsourcesMP[mydata._Meta.Type].Contains(mydata._Meta.Source)
                        )
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

                            bool ownerallowed = true;
                            if ((mydata as ODHActivityPoiLinked).OwnerRid != null)
                                ownerallowed =
                                    notallowedownerrids[mydata._Meta.Type]
                                        .Where(x =>
                                            x.Contains(
                                                (
                                                    mydata as ODHActivityPoiLinked
                                                ).OwnerRid
                                            )
                                        )
                                        .Count() > 0
                                        ? false
                                        : true;

                            //IF category is white or blacklisted find an intersection
                            var tagintersection = allowedtags
                                .Select(x => x.Id)
                                .ToList()
                                .Intersect((mydata as ODHActivityPoiLinked).SmgTags);

                            if (tagintersection.Count() > 0 && tvallowed && ownerallowed)
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
                        //today.noi.bz.it
                        if ((mydata as EventShort).Display3 == "Y")
                            publishedonlist.TryAddOrUpdateOnList("today.noi.bz.it");
                        if ((mydata as EventShort).Display3 == "N")
                            publishedonlist.TryRemoveOnList("today.noi.bz.it");
                        //Eurac Videowall
                        if ((mydata as EventShort).Display4 == "Y")
                            publishedonlist.TryAddOrUpdateOnList("noi-totem");
                        if ((mydata as EventShort).Display4 == "N")
                            publishedonlist.TryRemoveOnList("noi-totem");

                        //Readd publishers that were there before not assigned automatically
                        if(mydata.PublishedOn != null &&
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
                        if ((mydata as MeasuringpointLinked).Active)
                        {
                            publishedonlist.TryAddOrUpdateOnList("suedtirol.info");
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "venue":
                        if (mydata is VenueLinked)
                        {
                            if ((mydata as VenueLinked).Active == true)
                            {
                                publishedonlist.TryAddOrUpdateOnList("suedtirol.info");
                                publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                            }
                        }
                        else if (mydata is VenueV2)
                        {
                            if ((mydata as VenueV2).Active == true)
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

                    //TO CHECK, import all and set it active on Marketplace?
                    case "webcam":
                        if ((mydata as WebcamInfoLinked).SmgActive == true)
                        {
                            publishedonlist.TryAddOrUpdateOnList("suedtirol.info");
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "wineaward":
                        if ((mydata as WineLinked).Active == true)
                        {
                            publishedonlist.TryAddOrUpdateOnList("suedtirol.info");
                            publishedonlist.TryAddOrUpdateOnList("suedtirolwein.com");
                            //publishedonlist.TryAddOrUpdateOnList("idm-marketplace"); /??
                        }
                        break;

                    case "region":
                        if ((mydata as RegionLinked).Active)
                        {
                            publishedonlist.TryAddOrUpdateOnList("suedtirol.info");
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "tourismassociation":
                        if ((mydata as TourismvereinLinked).Active)
                        {
                            publishedonlist.TryAddOrUpdateOnList("suedtirol.info");
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "district":
                        if ((mydata as DistrictLinked).Active)
                        {
                            publishedonlist.TryAddOrUpdateOnList("suedtirol.info");
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "municipality":
                        if ((mydata as MunicipalityLinked).Active)
                        {
                            publishedonlist.TryAddOrUpdateOnList("suedtirol.info");
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "metaregion":
                        if ((mydata as MetaRegionLinked).Active)
                        {
                            publishedonlist.TryAddOrUpdateOnList("suedtirol.info");
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "area":
                        if ((mydata as AreaLinked).Active)
                        {
                            publishedonlist.TryAddOrUpdateOnList("suedtirol.info");
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "skiarea":
                        if ((mydata as SkiAreaLinked).Active)
                        {
                            publishedonlist.TryAddOrUpdateOnList("suedtirol.info");
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "skiregion":
                        if ((mydata as SkiRegionLinked).Active)
                        {
                            publishedonlist.TryAddOrUpdateOnList("suedtirol.info");
                            publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
                        }
                        break;

                    case "experiencearea":
                        if ((mydata as ExperienceAreaLinked).Active)
                        {
                            publishedonlist.TryAddOrUpdateOnList("suedtirol.info");
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
                            publishedonlist.TryAddOrUpdateOnList("suedtirol.info");
                            //publishedonlist.TryAddOrUpdateOnList("idm-marketplace");
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
