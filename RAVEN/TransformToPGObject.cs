// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using DataModel;
using Helper;
using Helper.Extensions;

namespace RAVEN
{
    public class TransformToPGObject
    {
        //HOF applies all the rules
        public static V GetPGObject<T, V>(T myobject, Func<T, V> pgmodelgenerator)
        {
            return pgmodelgenerator(myobject);
        }

        //public static AccommodationV2 GetAccommodationPGObject(AccommodationV2 data)
        //{
        //    data.Id = data.Id.ToUpper();

        //    if (data.SmgTags != null && data.SmgTags.Count > 0)
        //        data.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

        //    //Shortdesc Longdesc fix TODO
        //    foreach (var detail in data.AccoDetail)
        //    {
        //        var shortdesc = detail.Value.Longdesc;
        //        var longdesc = detail.Value.Shortdesc;

        //        detail.Value.Shortdesc = shortdesc;
        //        detail.Value.Longdesc = longdesc;
        //    }

        //    if (String.IsNullOrEmpty(data.Source))
        //        data.Source = "lts";

        //    data.Source = data.Source.ToLower();

        //    data._Meta = MetadataHelper.GetMetadataobject<AccommodationV2>(data, MetadataHelper.GetMetadataforAccommodation);  //GetMetadata(data.Id, "accommodation", "lts", data.LastChange);
        //    //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("accommodation", data.SmgActive);


        //    return data;
        //}

        //public static AccommodationLinked GetAccommodationPGObject(AccommodationRaven data)
        //{
        //    AccommodationLinked acco = new AccommodationLinked();

        //    acco.Id = data.Id.ToUpper();

        //    if (data.SmgTags != null && data.SmgTags.Count > 0)
        //        acco.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

        //    //Shortdesc Longdesc fix
        //    foreach (var detail in data.AccoDetail)
        //    {
        //        var shortdesc = detail.Value.Longdesc;
        //        var longdesc = detail.Value.Shortdesc;

        //        detail.Value.Shortdesc = shortdesc;
        //        detail.Value.Longdesc = longdesc;
        //    }


        //    acco.AccoBookingChannel = data.AccoBookingChannel;
        //    acco.AccoDetail = data.AccoDetail;
        //    acco.AccoCategoryId = data.AccoCategoryId;
        //    acco.AccoRoomInfo = data.AccoRoomInfo;
        //    acco.AccoTypeId = data.AccoTypeId;
        //    acco.Active = data.Active;

        //    acco.GpsInfo = data.ConvertGpsInfoOnRootToGpsInfoArray();
        //    acco.BadgeIds = data.BadgeIds;
        //    acco.BoardIds = data.BoardIds;
        //    acco.DistanceInfo = data.DistanceInfo;
        //    acco.DistrictId = data.DistrictId;
        //    acco.Features = data.Features;
        //    acco.FirstImport = data.FirstImport;
        //    acco.GastronomyId = data.GastronomyId;
        //    acco.HasApartment = data.HasApartment;
        //    acco.HasLanguage = data.HasLanguage;
        //    acco.HasRoom      = data.HasRoom;
        //    acco.HgvId = data.HgvId;
        //    acco.ImageGallery = data.ImageGallery;
        //    acco.IndependentData = data.IndependentData;
        //    acco.IsAccommodation = data.IsAccommodation;
        //    acco.IsBookable = data.IsBookable;
        //    acco.IsCamping = data.IsCamping;
        //    acco.IsGastronomy = data.IsGastronomy;
        //    acco.LastChange = data.LastChange;
        //    acco.LicenseInfo = data.LicenseInfo;
        //    acco.LocationInfo = data.LocationInfo;
        //    acco.MainLanguage = data.MainLanguage;
        //    acco.Mapping = data.Mapping;
        //    acco.MarketingGroupIds = data.MarketingGroupIds;
        //    acco.MssResponseShort = data.MssResponseShort;
        //    acco.Representation = data.Representation;
        //    acco.Shortname = data.Shortname;
        //    acco.SmgActive = data.SmgActive;
        //    acco.SmgTags = data.SmgTags;
        //    acco.SpecialFeaturesIds = data.SpecialFeaturesIds;
        //    acco.Source = String.IsNullOrEmpty(data.Source) ? "lts" : data.Source.ToLower();
        //    acco.ThemeIds = data.ThemeIds;
        //    acco.TourismVereinId = data.TourismVereinId;
        //    acco.TrustYouActive = data.TrustYouActive;
        //    acco.TrustYouID = data.TrustYouID;
        //    acco.TrustYouResults = data.TrustYouResults;
        //    acco.TrustYouScore = data.TrustYouScore;
        //    acco.TrustYouState = data.TrustYouState;
        //    acco.TVMember = data.TVMember;

        //    acco.AccoHGVInfo = data.AccoHGVInfo;
        //    acco.AccoOverview = data.AccoOverview;

        //    acco._Meta = MetadataHelper.GetMetadataobject<AccommodationLinked>(acco, MetadataHelper.GetMetadataforAccommodation);  //GetMetadata(data.Id, "accommodation", "lts", data.LastChange);
        //    //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("accommodation", data.SmgActive);

        //    return acco;
        //}

        public static AccommodationV2 GetAccommodationPGObjectV2(AccommodationRaven data)
        {
            AccommodationV2 acco = new AccommodationV2();

            acco.Id = data.Id.ToUpper();

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                acco.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            //Shortdesc Longdesc fix
            foreach (var detail in data.AccoDetail)
            {
                var shortdesc = detail.Value.Longdesc;
                var longdesc = detail.Value.Shortdesc;

                detail.Value.Shortdesc = shortdesc;
                detail.Value.Longdesc = longdesc;
            }

            acco.AccoBookingChannel = data.AccoBookingChannel;
            acco.AccoDetail = data.AccoDetail;
            acco.AccoCategoryId = data.AccoCategoryId;
            acco.AccoRoomInfo = data.AccoRoomInfo;
            acco.AccoTypeId = data.AccoTypeId;
            acco.Active = data.Active;

            acco.GpsInfo = data.ConvertGpsInfoOnRootToGpsInfoArray();
            acco.BadgeIds = data.BadgeIds;
            acco.BoardIds = data.BoardIds;
            acco.DistanceInfo = data.DistanceInfo;
            acco.DistrictId = data.DistrictId;
            acco.Features = data.Features;
            acco.FirstImport = data.FirstImport;
            acco.GastronomyId = data.GastronomyId;

            var accoproperties = new AccoProperties();
            accoproperties.HasApartment = data.HasApartment;
            accoproperties.HasRoom = data.HasRoom;
            accoproperties.IsAccommodation = data.IsAccommodation;
            accoproperties.IsBookable = data.IsBookable;
            accoproperties.IsCamping = data.IsCamping;
            accoproperties.IsGastronomy = data.IsGastronomy;
            accoproperties.TVMember = data.TVMember;

            acco.AccoProperties = accoproperties;

            acco.HasLanguage = data.HasLanguage;

            acco.HgvId = data.HgvId;
            acco.ImageGallery = data.ImageGallery;
            acco.IndependentData = data.IndependentData;
            acco.LastChange = data.LastChange;
            acco.LicenseInfo = data.LicenseInfo;
            acco.LocationInfo = data.LocationInfo;
            acco.MainLanguage = data.MainLanguage;
            acco.Mapping = data.Mapping;
            acco.MarketingGroupIds = data.MarketingGroupIds;
            acco.MssResponseShort = data.MssResponseShort;
            acco.Representation = data.Representation;
            acco.Shortname = data.Shortname;
            acco.SmgActive = data.SmgActive;
            acco.SmgTags = data.SmgTags;
            acco.SpecialFeaturesIds = data.SpecialFeaturesIds;
            acco.Source = String.IsNullOrEmpty(data.Source) ? "lts" : data.Source.ToLower();
            acco.ThemeIds = data.ThemeIds;
            acco.TourismVereinId = data.TourismVereinId;

            Review review = new Review();
            review.ReviewId = data.TrustYouID;
            review.Results = data.TrustYouResults;
            review.Provider = "trustyou";
            review.Active = data.TrustYouActive;
            review.Score = data.TrustYouScore;
            review.StateInteger = data.TrustYouState;

            if (acco.Review == null)
                acco.Review = new Dictionary<string, DataModel.Review>();

            if (!String.IsNullOrEmpty(data.TrustYouID))
                acco.Review.TryAddOrUpdate("trustyou", review);

            acco.AccoHGVInfo = data.AccoHGVInfo;
            acco.AccoOverview = data.AccoOverview;

            acco._Meta = MetadataHelper.GetMetadataobject<AccommodationV2>(
                acco,
                MetadataHelper.GetMetadataforAccommodation
            ); //GetMetadata(data.Id, "accommodation", "lts", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("accommodation", data.SmgActive);

            return acco;
        }

        public static ODHActivityPoiLinked GetODHActivityPoiPGObject(ODHActivityPoiLinked data)
        {
            data.Id = data.Id.ToLower();

            if (data.Source != null)
                data.Source = data.Source.ToLower();

            //Hack for Source magnolia, content, common
            if (data.Source == "magnolia" || data.Source == "content" || data.Source == "common")
                data.Source = "idm";

            if (data.SyncSourceInterface != null)
                data.SyncSourceInterface = data.SyncSourceInterface.ToLower();

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                data.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (!String.IsNullOrEmpty(data.CustomId))
                data.CustomId = data.CustomId.ToUpper();

            //TODO
            //Write Webcam Objects to Related Content

            //Related Content
            if (data.RelatedContent != null)
            {
                List<RelatedContent> relcontentlist = new List<RelatedContent>();
                foreach (var relatedcontent in data.RelatedContent)
                {
                    RelatedContent relatedcontenttotransform = relatedcontent;

                    if (
                        relatedcontent.Type == "odhactivitypoi"
                        || relatedcontent.Type == "eventshort"
                    )
                    {
                        relatedcontenttotransform.Id = relatedcontenttotransform.Id.ToLower();
                    }
                    else
                    {
                        relatedcontenttotransform.Id = relatedcontenttotransform.Id.ToUpper();
                    }

                    relcontentlist.Add(relatedcontenttotransform);
                }

                data.RelatedContent = relcontentlist;
            }

            if (data.GpsInfo != null)
            {
                int i = 2;
                foreach (var gpsinfo in data.GpsInfo)
                {
                    if (!data.GpsPoints.ContainsKey(gpsinfo.Gpstype))
                    {
                        data.GpsPoints.Add(gpsinfo.Gpstype, gpsinfo);
                    }
                    else
                    {
                        data.GpsPoints.Add("position" + i, gpsinfo);
                        i++;
                    }
                }
            }

            string sourcemeta = data.Source.ToLower();

            if (data.LTSTags != null)
            {
                if (data.TagIds == null)
                    data.TagIds = new List<string>();

                foreach (var myltstag in data.LTSTags)
                {
                    myltstag.Id = myltstag.Id.ToLower();
                    //Check populate TagIDs
                    data.TagIds.Add(myltstag.LTSRID);
                }
            }

            //Remove empty dictionary keys
            data.PoiProperty =
                data.PoiProperty == null
                    ? new Dictionary<string, List<PoiProperty>>()
                    : data
                        .PoiProperty.Where(f => f.Value.Count > 0)
                        .ToDictionary(x => x.Key, x => x.Value);

            data._Meta = MetadataHelper.GetMetadataobject<ODHActivityPoiLinked>(
                data,
                MetadataHelper.GetMetadataforOdhActivityPoi
            ); //GetMetadata(data.Id, "odhactivitypoi", sourcemeta, data.LastChange);

            ODHTagHelper.SetMainCategorizationForODHActivityPoi(data);

            return data;
        }

        public static AccommodationRoomLinked GetAccommodationRoomPGObject(
            AccommodationRoomLinked data
        )
        {
            data.Id = data.Id.ToUpper();
            data.A0RID = data.A0RID.ToUpper();
            if (!String.IsNullOrEmpty(data.LTSId))
                data.LTSId = data.LTSId.ToUpper();

            //Shortdesc Longdesc fix, Data is imported wrong on the ravendb instance
            foreach (var detail in data.AccoRoomDetail)
            {
                var shortdesc = detail.Value.Longdesc;
                var longdesc = detail.Value.Shortdesc;

                detail.Value.Shortdesc = shortdesc;
                detail.Value.Longdesc = longdesc;
            }

            //fix if source is null
            string datasource = data.Source.ToLower();

            if (datasource == null)
            {
                if (data.Id.Contains("hgv"))
                    datasource = "hgv";
                else
                    datasource = "lts";
            }
            else
            {
                datasource = datasource.ToLower();
            }

            data.Source = datasource.ToLower();
            data.Active = true;

            data._Meta = MetadataHelper.GetMetadataobject<AccommodationRoomLinked>(
                data,
                MetadataHelper.GetMetadataforAccommodationRoom
            ); //GetMetadata(data.Id, "accommodationroom", datasource, data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("accommodationroom", true);

            return data;
        }

        public static LTSActivityLinked GetActivityPGObject(LTSActivityLinked data)
        {
            LTSActivityLinked data2 = new LTSActivityLinked();
            data2.Active = data.Active;
            data2.AdditionalPoiInfos = data.AdditionalPoiInfos;
            data2.AltitudeDifference = data.AltitudeDifference;
            data2.AltitudeHighestPoint = data.AltitudeHighestPoint;
            data2.AltitudeLowestPoint = data.AltitudeLowestPoint;
            data2.AltitudeSumDown = data.AltitudeSumDown;
            data2.AltitudeSumUp = data.AltitudeSumUp;
            data2.AreaId = data.AreaId;
            data2.BikeTransport = data.BikeTransport;
            data2.ContactInfos = data.ContactInfos;
            data2.Detail = data.Detail;
            data2.Difficulty = data.Difficulty;
            data2.DistanceDuration = data.DistanceDuration;
            data2.DistanceLength = data.DistanceLength;
            data2.Exposition = data.Exposition;
            data2.FeetClimb = data.FeetClimb;
            data2.FirstImport = data.FirstImport;
            data2.GpsInfo = data.GpsInfo;
            //data2.GpsPoints = new Dictionary<string, GpsInfo>();
            data2.GpsTrack = data.GpsTrack;
            data2.HasFreeEntrance = data.HasFreeEntrance;
            data2.HasLanguage = data.HasLanguage;
            data2.HasRentals = data.HasRentals;
            data2.Highlight = data.Highlight;
            data2.Id = data.Id;
            data2.ImageGallery = data.ImageGallery;
            data2.IsOpen = data.IsOpen;
            data2.IsPrepared = data.IsPrepared;
            data2.IsWithLigth = data.IsWithLigth;
            data2.LastChange = data.LastChange;
            data2.LiftAvailable = data.LiftAvailable;
            data2.LocationInfo = data.LocationInfo;
            data2.LTSTags = data.LTSTags;
            data2.OperationSchedule = data.OperationSchedule;
            data2.OutdooractiveID = data.OutdooractiveID;
            data2.PoiType = data.PoiType;
            data2.Ratings = data.Ratings;
            data2.RunToValley = data.RunToValley;
            data2.Shortname = data.Shortname;
            data2.SmgActive = data.SmgActive;
            data2.SmgId = data.SmgId;
            data2.SmgTags = data.SmgTags;
            data2.SubType = data.SubType;
            data2.TourismorganizationId = data.TourismorganizationId;
            data2.Type = data.Type;
            data2.OwnerRid = data.OwnerRid;
            data2.ChildPoiIds = data.ChildPoiIds;
            data2.MasterPoiIds = data.MasterPoiIds;
            data2.CopyrightChecked = data.CopyrightChecked;
            data2.OutdooractiveElevationID = data.OutdooractiveElevationID;
            data2.WayNumber = data.WayNumber;
            data2.Number = data.Number;
            data2.LicenseInfo = data.LicenseInfo;

            if (String.IsNullOrEmpty(data.Source))
                data2.Source = "lts";
            else
                data2.Source = data.Source.ToLower();

            data2.Id = data2.Id.ToUpper();

            if (data2.SmgTags != null && data2.SmgTags.Count > 0)
                data2.SmgTags = data2.SmgTags.Select(x => x.ToLower()).ToList();

            if (data2.LTSTags != null)
            {
                foreach (var myltstag in data2.LTSTags)
                {
                    myltstag.Id = myltstag.Id.ToLower();
                }
            }

            if (String.IsNullOrEmpty(data2.Source))
                data2.Source = "lts";

            data2._Meta = MetadataHelper.GetMetadataobject<LTSActivityLinked>(
                data2,
                MetadataHelper.GetMetadataforActivity
            ); //GetMetadata(data.Id, "ltsactivity", "lts", data.LastChange);
            data2.PublishedOn = new List<string>();

            return data2;
        }

        public static LTSPoiLinked GetPoiPGObject(LTSPoiLinked data)
        {
            data.Id = data.Id.ToUpper();

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                data.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (data.LTSTags != null)
            {
                foreach (var myltstag in data.LTSTags)
                {
                    myltstag.Id = myltstag.Id.ToLower();
                }
            }

            if (String.IsNullOrEmpty(data.Source))
                data.Source = "lts";
            else
                data.Source = data.Source.ToLower();

            data.PublishedOn = new List<string>();

            data._Meta = MetadataHelper.GetMetadataobject<LTSPoiLinked>(
                data,
                MetadataHelper.GetMetadataforPoi
            ); //GetMetadata(data.Id, "ltspoi", "lts", data.LastChange);

            return data;
        }

        public static ArticlesLinked GetArticlePGObject(ArticlesLinked data)
        {
            data.Id = data.Id.ToUpper();

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                data.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                data.Source = "idm";
            else
                data.Source = data.Source.ToLower();

            data._Meta = MetadataHelper.GetMetadataobject<ArticlesLinked>(
                data,
                MetadataHelper.GetMetadataforArticle
            ); //GetMetadata(data.Id, "article", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("article", data.SmgActive);

            return data;
        }

        public static PackageLinked GetPackagePGObject(PackageLinked data)
        {
            data.Id = data.Id.ToUpper();
            data.HotelId = data.HotelId.Select(x => x.ToUpper()).ToList();

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                data.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                data.Source = "hgv";
            else
                data.Source = data.Source.ToLower();

            data._Meta = MetadataHelper.GetMetadataobject<PackageLinked>(
                data,
                MetadataHelper.GetMetadataforPackage
            ); //GetMetadata(data.Id, "package", "hgv", data.LastUpdate);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("package", data.SmgActive);

            return data;
        }

        public static EventLinked GetEventPGObject(EventLinked data)
        {
            data.Id = data.Id.ToUpper();

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                data.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            data.Source = "lts";

            data._Meta = MetadataHelper.GetMetadataforEvent(data);

            //    MetadataHelper.GetMetadataobject<EventLinked>(
            //    data,                
            //    MetadataHelper.GetMetadataforEvent
            //); 
            //GetMetadata(data.Id, "event", "lts", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("event", data.SmgActive);

            return data;
        }

        public static EventLinked GetEventPGObject(EventRaven data)
        {
            EventLinked eventlinked = new EventLinked();

            eventlinked.Id = data.Id.ToUpper();

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                eventlinked.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            eventlinked.Source = "lts";

            eventlinked.Active = data.Active;
            eventlinked.GpsInfo = data.ConvertGpsInfoOnRootToGpsInfoArray();

            eventlinked.EventProperty = new EventProperty();

            eventlinked.EventProperty.EventClassificationId = data.ClassificationRID;
            eventlinked.ContactInfos = data.ContactInfos;
            eventlinked.DateBegin = data.DateBegin;
            eventlinked.DateEnd = data.DateEnd;
            eventlinked.Detail = data.Detail;
            eventlinked.DistanceInfo = data.DistanceInfo;
            eventlinked.DistrictId = data.DistrictId;
            eventlinked.DistrictIds = data.DistrictIds;
            eventlinked.EventAdditionalInfos = data.EventAdditionalInfos;
            //eventlinked.EventBenefit = data.EventBenefit;
            eventlinked.EventBooking = data.EventBooking;
            //eventlinked.EventCrossSelling = data.EventCrossSelling;
            eventlinked.EventDate = data.EventDate;
            //eventlinked.EventDescAdditional = data.EventDescAdditional;
            //eventlinked.EventOperationScheduleOverview = data.EventOperationScheduleOverview;
            eventlinked.EventPrice = data.EventPrice;
            //eventlinked.EventPrices = data.EventPrices;
            eventlinked.EventPublisher = data.EventPublisher;
            eventlinked.FirstImport = data.FirstImport;
            //eventlinked.GrpEvent = data.GrpEvent;
            //eventlinked.Hashtag = data.Hashtag;
            eventlinked.HasLanguage = data.HasLanguage;
            eventlinked.ImageGallery = data.ImageGallery;
            eventlinked.LastChange = data.LastChange;
            eventlinked.LicenseInfo = data.LicenseInfo;
            eventlinked.LocationInfo = data.LocationInfo;
            //eventlinked.LTSTags = data.LTSTags;
            eventlinked.Mapping = data.Mapping;
            //eventlinked.NextBeginDate = data.NextBeginDate;
            eventlinked.OrganizerInfos = data.OrganizerInfos;
            eventlinked.EventProperty.EventOrganizerId = data.OrgRID;
            //eventlinked.PayMet = data.PayMet;
            //eventlinked.Pdf = data.Pdf;
            //eventlinked.Ranc = data.Ranc;
            eventlinked.Shortname = data.Shortname;
            eventlinked.EventProperty.RegistrationRequired = !String.IsNullOrEmpty(data.Ticket) ? data.SignOn == "1" ? true : false : false;
            eventlinked.SmgActive = data.SmgActive;
            eventlinked.SmgTags = data.SmgTags;
            eventlinked.EventProperty.TicketRequired = !String.IsNullOrEmpty(data.Ticket) ? data.Ticket == "1" ? true : false : false;
            eventlinked.TopicRIDs = data.TopicRIDs;
            eventlinked.Topics = data.Topics;
            //eventlinked.Type = data.Type;

            if (eventlinked.TagIds == null)
                eventlinked.TagIds = new List<string>();

            if (eventlinked.Tags == null)
                eventlinked.Tags = new List<Tags>();

            //Compatibility
            if (eventlinked.SmgTags == null)
                eventlinked.SmgTags = new List<string>();

            //New Add Topics to Tags
            foreach (var topic in data.Topics)
            {
                eventlinked.TagIds.Add(topic.TopicRID);

                Tags tag = new Tags();
                tag.Source = "lts";
                tag.Id = topic.TopicRID;
                tag.Type = "eventcategory";
                tag.Name = topic.TopicInfo;

                eventlinked.Tags.Add(tag);
            }

            //New Add Tags to Tags
            if (data.LTSTags != null)
            {
                foreach (var ltstag in data.LTSTags)
                {
                    eventlinked.TagIds.Add(ltstag.LTSRID);
                    //Compatibility
                    eventlinked.SmgTags.Add(ltstag.LTSRID);

                    Tags tag = new Tags();
                    tag.Source = "lts";
                    tag.Id = ltstag.LTSRID;
                    tag.Type = "eventtag";
                    tag.Name = ltstag.TagName.ContainsKey("en")
                        ? ltstag.TagName["en"]
                        : ltstag.TagName.FirstOrDefault().Value;

                    eventlinked.Tags.Add(tag);
                }
            }

            //New Add Classification to Tags
            if (data.ClassificationRID != null)
            {
                eventlinked.TagIds.Add(data.ClassificationRID);

                Tags tag = new Tags();
                tag.Source = "lts";
                tag.Id = data.ClassificationRID;
                tag.Type = "eventclassification";
                if (data.ClassificationRID == "4650BDEF28D545CE8AB37138E3C45B80")
                    tag.Name = "Service";
                else if (data.ClassificationRID == "CE212B488FA14954BE91BBCFA47C0F06")
                    tag.Name = "Event";
                else if (data.ClassificationRID == "D8F5FF743D5741D1BF1F5D61671F552B")
                    tag.Name = "Approval";
                else if (data.ClassificationRID == "E9F80CE8CB3F481ABC7E548CF34A8C1C")
                    tag.Name = "Reservation";
                else
                    tag.Name = "";

                eventlinked.Tags.Add(tag);
            }

            //TODO make some props obsolete

            //eventlinked._Meta = MetadataHelper.GetMetadataobject<EventLinked>(
            //    eventlinked,
            //    MetadataHelper.GetMetadataforEvent
            //);

            eventlinked._Meta = MetadataHelper.GetMetadataforEvent(eventlinked);


            //GetMetadata(data.Id, "event", "lts", data.LastChange);

            return eventlinked;
        }

        public static GastronomyLinked GetGastronomyPGObject(GastronomyLinked data)
        {
            data.Id = data.Id.ToUpper();
            if (!String.IsNullOrEmpty(data.AccommodationId))
                data.AccommodationId = data.AccommodationId.ToUpper();

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                data.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                data.Source = "lts";
            else
                data.Source = data.Source.ToLower();

            data.PublishedOn = new List<string>();

            data._Meta = MetadataHelper.GetMetadataobject<GastronomyLinked>(
                data,
                MetadataHelper.GetMetadataforGastronomy
            ); //GetMetadata(data.Id, "ltsgastronomy", "lts", data.LastChange);

            return data;
        }

        public static GastronomyLinked GetGastronomyPGObject(GastronomyRaven data)
        {
            GastronomyLinked gastro = new GastronomyLinked();

            gastro.Id = data.Id.ToUpper();
            gastro.Active = data.Active;
            gastro.CapacityCeremony = data.CapacityCeremony;
            gastro.CategoryCodes = data.CategoryCodes;
            gastro.ContactInfos = data.ContactInfos;
            gastro.Detail = data.Detail;
            gastro.DishRates = data.DishRates;
            gastro.DistanceInfo = data.DistanceInfo;
            gastro.DistrictId = data.DistrictId;
            gastro.Facilities = data.Facilities;
            gastro.FirstImport = data.FirstImport;
            gastro.GpsInfo = data.ConvertGpsInfoOnRootToGpsInfoArray();
            gastro.HasLanguage = data.HasLanguage;
            gastro.ImageGallery = data.ImageGallery;
            gastro.LastChange = data.LastChange;
            gastro.LicenseInfo = data.LicenseInfo;
            gastro.LocationInfo = data.LocationInfo;
            gastro.Mapping = data.Mapping;
            gastro.MarketinggroupId = data.MarketinggroupId;
            gastro.MaxSeatingCapacity = data.MaxSeatingCapacity;
            gastro.OperationSchedule = data.OperationSchedule;
            gastro.RepresentationRestriction = data.RepresentationRestriction;
            gastro.Shortname = data.Shortname;
            gastro.SmgActive = data.SmgActive;
            gastro.Source = data.Source;
            gastro.SmgTags = data.SmgTags;
            gastro.Type = data.Type;

            if (!String.IsNullOrEmpty(data.AccommodationId))
                gastro.AccommodationId = data.AccommodationId.ToUpper();

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                gastro.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                gastro.Source = "lts";
            else
                gastro.Source = data.Source.ToLower();

            gastro.PublishedOn = new List<string>();

            gastro._Meta = MetadataHelper.GetMetadataobject<GastronomyLinked>(
                gastro,
                MetadataHelper.GetMetadataforGastronomy
            ); //GetMetadata(data.Id, "ltsgastronomy", "lts", data.LastChange);

            return gastro;
        }

        public static WebcamInfoLinked GetWebcamInfoPGObject(WebcamInfoLinked data)
        {
            data.Id = data.Id.ToUpper();

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                data.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (data.Active == null)
                data.Active = false;

            if (data.SmgActive == null)
                data.SmgActive = false;

            data.WebCamProperties = new WebcamProperties();
            data.WebCamProperties.WebcamUrl = data.Webcamurl;
            data.WebCamProperties.PreviewUrl = data.Previewurl;
            data.WebCamProperties.StreamUrl = data.Streamurl;

            if (!String.IsNullOrEmpty(data.WebCamProperties.WebcamUrl))
            {
                //Hack add as ImageGallery
                data.ImageGallery = new List<ImageGallery>();
                ImageGallery image = new ImageGallery();
                image.ImageUrl = data.WebCamProperties.WebcamUrl;

                data.ImageGallery.Add(image);
            }

            if (String.IsNullOrEmpty(data.Source) || data.Source.ToLower() == "content")
                data.Source = "idm";
            else
                data.Source = data.Source.ToLower();

            data._Meta = MetadataHelper.GetMetadataobject<WebcamInfoLinked>(
                data,
                MetadataHelper.GetMetadataforWebcam
            ); //GetMetadata(data.Id, "webcam", sourcemeta, data.LastChange);
            var webcampublished =
                data.WebcamAssignedOn != null && data.WebcamAssignedOn.Count > 0 ? true : false;

            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("webcam", webcampublished);

            return data;
        }

        public static WebcamInfoLinked GetWebcamInfoPGObject(WebcamInfoRaven data)
        {
            WebcamInfoLinked webcam = new WebcamInfoLinked();

            webcam.Id = data.Id.ToUpper();

            webcam.Active = data.Active;
            webcam.AreaIds = data.AreaIds;
            webcam.FirstImport = data.FirstImport;
            webcam.GpsInfo =
                data.GpsInfo != null ? new List<GpsInfo>() { data.GpsInfo } : new List<GpsInfo>();
            webcam.LastChange = data.LastChange;
            webcam.LicenseInfo = data.LicenseInfo;
            webcam.ListPosition = data.ListPosition;
            webcam.Mapping = data.Mapping;
            webcam.SmgActive = data.SmgActive;
            webcam.Shortname = data.Shortname;
            webcam.SmgTags = data.SmgTags;
            webcam.WebcamAssignedOn = data.WebcamAssignedOn;
            webcam.WebcamId = data.WebcamId;
            //Webcamproperties
            webcam.WebCamProperties = new WebcamProperties();
            webcam.WebCamProperties.WebcamUrl = data.Webcamurl;
            webcam.WebCamProperties.PreviewUrl = data.Previewurl;
            webcam.WebCamProperties.StreamUrl = data.Streamurl;

            //ImageGallery
            if (!String.IsNullOrEmpty(data.Webcamurl))
            {
                //Hack add as ImageGallery
                webcam.ImageGallery = new List<ImageGallery>();
                ImageGallery image = new ImageGallery();
                image.ImageUrl = data.Webcamurl;

                webcam.ImageGallery.Add(image);
            }

            //Detail
            foreach (var kvp in data.Webcamname)
            {
                Detail detail = new Detail();
                detail.Language = kvp.Key;
                detail.Title = kvp.Value;

                webcam.Detail.TryAddOrUpdate(kvp.Key, detail);
            }

            webcam.HasLanguage = webcam.Detail.Keys;

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                webcam.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source) || data.Source.ToLower() == "content")
                webcam.Source = "idm";
            else
                webcam.Source = data.Source.ToLower();

            webcam.CheckMyInsertedLanguages();

            webcam._Meta = MetadataHelper.GetMetadataobject<WebcamInfoLinked>(
                webcam,
                MetadataHelper.GetMetadataforWebcam
            ); //GetMetadata(data.Id, "webcam", sourcemeta, data.LastChange);
            var webcampublished =
                data.WebcamAssignedOn != null && data.WebcamAssignedOn.Count > 0 ? true : false;

            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("webcam", webcampublished);

            return webcam;
        }

        public static MeasuringpointLinked GetMeasuringpointPGObject(MeasuringpointLinked data)
        {
            data.Id = data.Id.ToUpper();

            if (String.IsNullOrEmpty(data.Source))
                data.Source = "lts";
            else
                data.Source = data.Source.ToLower();

            data._Meta = MetadataHelper.GetMetadataobject<MeasuringpointLinked>(
                data,
                MetadataHelper.GetMetadataforMeasuringpoint
            ); //GetMetadata(data.Id, "measuringpoint", "lts", data.LastChange);

            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("mesuringpoint", data.SmgActive);

            return data;
        }

        public static MeasuringpointLinked GetMeasuringpointPGObject(MeasuringpointRaven data)
        {
            MeasuringpointLinked measuringpoint = new MeasuringpointLinked();

            measuringpoint.Id = data.Id.ToUpper();

            if (String.IsNullOrEmpty(data.Source))
                measuringpoint.Source = "lts";
            else
                measuringpoint.Source = data.Source.ToLower();

            measuringpoint.Active = data.Active;
            measuringpoint.AreaIds = data.AreaIds;
            measuringpoint.DistanceInfo = data.DistanceInfo;
            measuringpoint.FirstImport = data.FirstImport;
            measuringpoint.GpsInfo = data.ConvertGpsInfoOnRootToGpsInfoArray();
            measuringpoint.LastChange = data.LastChange;
            measuringpoint.LastSnowDate = data.LastSnowDate;
            measuringpoint.LastUpdate = data.LastUpdate;
            measuringpoint.LicenseInfo = data.LicenseInfo;
            measuringpoint.LocationInfo = data.LocationInfo;
            measuringpoint.Mapping = data.Mapping;
            measuringpoint.newSnowHeight = data.newSnowHeight;
            measuringpoint.OwnerId = data.OwnerId;
            measuringpoint.Shortname = data.Shortname;
            measuringpoint.SkiAreaIds = data.SkiAreaIds;
            measuringpoint.SmgActive = data.SmgActive;
            measuringpoint.SnowHeight = data.SnowHeight;
            measuringpoint.Temperature = data.Temperature;
            measuringpoint.WeatherObservation = data.WeatherObservation;

            measuringpoint._Meta = MetadataHelper.GetMetadataobject<MeasuringpointLinked>(
                measuringpoint,
                MetadataHelper.GetMetadataforMeasuringpoint
            ); //GetMetadata(data.Id, "measuringpoint", "lts", data.LastChange);

            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("measuringpoint", data.SmgActive);

            return measuringpoint;
        }

        public static DDVenue GetVenuePGObject(DDVenue data)
        {
            data.Id = data.Id.ToUpper();

            data.LastChange = data.meta.lastUpdate;
            data.Shortname =
                data.attributes.name != null && data.attributes.name.Keys.Count > 0
                    ? data.attributes.name.FirstOrDefault().Value
                    : "";
            data.LicenseInfo = data.odhdata.LicenseInfo;

            if (data.odhdata.GpsInfo != null)
            {
                int i = 2;
                foreach (var gpsinfo in data.odhdata.GpsInfo)
                {
                    if (!data.odhdata.GpsPoints.ContainsKey(gpsinfo.Gpstype))
                    {
                        data.odhdata.GpsPoints.Add(gpsinfo.Gpstype, gpsinfo);
                    }
                    else
                    {
                        data.odhdata.GpsPoints.Add("position" + i, gpsinfo);
                        i++;
                    }
                }
            }

            data.odhdata.ODHActive =
                !data.attributes.categories.Contains("lts/visi_unpublishedOnODH")
                && data.attributes.categories.Contains("lts/visi_publishedOnODH")
                    ? true
                    : false;
            data.links.self = "Venue/" + data.Id;

            data._Meta = MetadataHelper.GetMetadataobject<DDVenue>(
                data,
                MetadataHelper.GetMetadataforDDVenue
            );
            //data.odhdata.PublishedOn = PublishedOnHelper.GetPublishenOnList("venue", data.odhdata.ODHActive);

            //fixes
            data.odhdata.Source = data.odhdata.Source.ToLower();
            data.Source = data.odhdata.Source;

            data.LicenseInfo = data.odhdata.LicenseInfo;

            return data;
        }

        public static VenueLinked GetVenuePGObjectV2(DDVenue destinationdata)
        {
            //Transform to Venuelinked
            var venueLinked = new VenueLinked();
            venueLinked.Active = destinationdata.odhdata.Active;
            venueLinked.ContactInfos = destinationdata.odhdata.ContactInfos;
            venueLinked.Detail = destinationdata.odhdata.Detail;
            venueLinked.DistanceInfo = destinationdata.odhdata.DistanceInfo;
            venueLinked.FirstImport = destinationdata.FirstImport;
            venueLinked.GpsInfo = destinationdata.odhdata.GpsInfo;
            //venueLinked.GpsPoints = destinationdata.odhdata.GpsPoints;
            venueLinked.HasLanguage = destinationdata.odhdata.HasLanguage;
            venueLinked.Id = destinationdata.Id;
            venueLinked.ImageGallery = destinationdata.odhdata.ImageGallery;
            venueLinked.LastChange = destinationdata.LastChange;
            venueLinked.LicenseInfo = destinationdata.LicenseInfo;
            venueLinked.LocationInfo = destinationdata.odhdata.LocationInfo;
            venueLinked.Mapping = destinationdata.Mapping;
            venueLinked.SmgActive = destinationdata.odhdata.ODHActive;
            //venueLinked.ODHActive = destinationdata.odhdata.ODHActive;
            //venueLinked.ODHTags = destinationdata.odhdata.ODHTags;
            venueLinked.RoomCount = destinationdata.odhdata.RoomCount;
            venueLinked.RoomDetails = destinationdata.odhdata.RoomDetails;
            venueLinked.Beds = destinationdata.odhdata.Beds;
            venueLinked.OperationSchedule = destinationdata.odhdata.OperationSchedule;
            venueLinked.Shortname = destinationdata.odhdata.Shortname;
            venueLinked.SmgTags = destinationdata.odhdata.SmgTags;
            venueLinked.Source = destinationdata.odhdata.Source.ToLower();
            venueLinked.SyncSourceInterface = destinationdata.odhdata.SyncSourceInterface;
            venueLinked.VenueCategory = destinationdata.odhdata.VenueCategory;
            venueLinked._Meta = destinationdata._Meta;
            venueLinked.PublishedOn = destinationdata.odhdata.PublishedOn;

            return venueLinked;
        }

        public static MetaRegionLinked GetMetaRegionPGObject(MetaRegionLinked data)
        {
            data.Id = data.Id.ToUpper();
            if (data.SmgTags != null && data.SmgTags.Count > 0)
                data.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                data.Source = "idm";
            else
                data.Source = data.Source.ToLower();

            data._Meta = MetadataHelper.GetMetadataobject<MetaRegionLinked>(
                data,
                MetadataHelper.GetMetadataforMetaRegion
            ); //GetMetadata(data.Id, "metaregion", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("metaregion", data.SmgActive);

            return data;
        }

        public static MetaRegionLinked GetMetaRegionPGObject(MetaRegion data)
        {
            MetaRegionLinked linked = new MetaRegionLinked();

            linked.Id = data.Id.ToUpper();

            linked.Active = data.Active;
            linked.ContactInfos = data.ContactInfos;
            linked.CustomId = data.CustomId;
            linked.Detail = data.Detail;
            linked.DetailThemed = data.DetailThemed;
            linked.DistanceInfo = data.DistanceInfo;
            linked.DistrictIds = data.DistrictIds;
            linked.FirstImport = data.FirstImport;
            linked.GpsInfo = data.ConvertGpsInfoOnRootToGpsInfoArray();
            linked.GpsPolygon = data.GpsPolygon;
            linked.HasLanguage = data.HasLanguage;
            linked.ImageGallery = data.ImageGallery;
            linked.LastChange = data.LastChange;
            linked.LicenseInfo = data.LicenseInfo;
            linked.Mapping = data.Mapping;
            linked.RegionIds = data.RegionIds;
            linked.RelatedContent = data.RelatedContent;
            linked.Shortname = data.Shortname;
            linked.SmgActive = data.SmgActive;
            linked.SmgTags = data.SmgTags;
            linked.Source = data.Source;
            linked.TourismvereinIds = data.TourismvereinIds;
            linked.VisibleInSearch = data.VisibleInSearch;

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                linked.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                linked.Source = "idm";
            else
                linked.Source = data.Source.ToLower();

            linked._Meta = MetadataHelper.GetMetadataobject<MetaRegionLinked>(
                linked,
                MetadataHelper.GetMetadataforMetaRegion
            ); //GetMetadata(data.Id, "metaregion", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("metaregion", data.SmgActive);

            return linked;
        }

        public static RegionLinked GetRegionPGObject(RegionLinked data)
        {
            data.Id = data.Id.ToUpper();
            if (data.SmgTags != null && data.SmgTags.Count > 0)
                data.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                data.Source = "idm";
            else
                data.Source = data.Source.ToLower();

            data._Meta = MetadataHelper.GetMetadataobject<RegionLinked>(
                data,
                MetadataHelper.GetMetadataforRegion
            ); //GetMetadata(data.Id, "region", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("region", data.SmgActive);

            return data;
        }

        public static RegionLinked GetRegionPGObject(Region data)
        {
            RegionLinked linked = new RegionLinked();

            linked.Id = data.Id.ToUpper();

            linked.Active = data.Active;
            linked.ContactInfos = data.ContactInfos;
            linked.CustomId = data.CustomId;
            linked.Detail = data.Detail;
            linked.DetailThemed = data.DetailThemed;
            linked.DistanceInfo = data.DistanceInfo;
            linked.FirstImport = data.FirstImport;
            linked.GpsInfo = data.ConvertGpsInfoOnRootToGpsInfoArray();
            linked.GpsPolygon = data.GpsPolygon;
            linked.HasLanguage = data.HasLanguage;
            linked.ImageGallery = data.ImageGallery;
            linked.LastChange = data.LastChange;
            linked.LicenseInfo = data.LicenseInfo;
            linked.Mapping = data.Mapping;
            linked.RelatedContent = data.RelatedContent;
            linked.Shortname = data.Shortname;
            linked.SmgActive = data.SmgActive;
            linked.SmgTags = data.SmgTags;
            linked.Source = data.Source;
            linked.VisibleInSearch = data.VisibleInSearch;
            linked.SkiareaIds = data.SkiareaIds;

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                linked.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                linked.Source = "idm";
            else
                linked.Source = data.Source.ToLower();

            linked._Meta = MetadataHelper.GetMetadataobject<RegionLinked>(
                linked,
                MetadataHelper.GetMetadataforRegion
            ); //GetMetadata(data.Id, "region", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("region", data.SmgActive);

            return linked;
        }

        public static TourismvereinLinked GetTourismAssociationPGObject(TourismvereinLinked data)
        {
            data.Id = data.Id.ToUpper();
            if (data.SmgTags != null && data.SmgTags.Count > 0)
                data.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                data.Source = "idm";
            else
                data.Source = data.Source.ToLower();

            data._Meta = MetadataHelper.GetMetadataobject<TourismvereinLinked>(
                data,
                MetadataHelper.GetMetadataforTourismverein
            ); //GetMetadata(data.Id, "tourismassociation", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("tourismassociation", data.SmgActive);

            return data;
        }

        public static TourismvereinLinked GetTourismAssociationPGObject(Tourismverein data)
        {
            TourismvereinLinked linked = new TourismvereinLinked();

            linked.Id = data.Id.ToUpper();

            linked.Active = data.Active;
            linked.ContactInfos = data.ContactInfos;
            linked.CustomId = data.CustomId;
            linked.Detail = data.Detail;
            linked.DistanceInfo = data.DistanceInfo;
            linked.FirstImport = data.FirstImport;
            linked.GpsInfo = data.ConvertGpsInfoOnRootToGpsInfoArray();
            linked.GpsPolygon = data.GpsPolygon;
            linked.HasLanguage = data.HasLanguage;
            linked.ImageGallery = data.ImageGallery;
            linked.LastChange = data.LastChange;
            linked.LicenseInfo = data.LicenseInfo;
            linked.Mapping = data.Mapping;
            linked.RelatedContent = data.RelatedContent;
            linked.Shortname = data.Shortname;
            linked.SmgActive = data.SmgActive;
            linked.SmgTags = data.SmgTags;
            linked.Source = data.Source;
            linked.VisibleInSearch = data.VisibleInSearch;
            linked.RegionId = data.RegionId;
            linked.SkiareaIds = data.SkiareaIds;

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                linked.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                linked.Source = "idm";
            else
                linked.Source = data.Source.ToLower();

            linked._Meta = MetadataHelper.GetMetadataobject<TourismvereinLinked>(
                linked,
                MetadataHelper.GetMetadataforTourismverein
            ); //GetMetadata(data.Id, "tourismassociation", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("tourismassociation", data.SmgActive);

            return linked;
        }

        public static MunicipalityLinked GetMunicipalityPGObject(MunicipalityLinked data)
        {
            data.Id = data.Id.ToUpper();
            if (data.SmgTags != null && data.SmgTags.Count > 0)
                data.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                data.Source = "idm";
            else
                data.Source = data.Source.ToLower();

            data._Meta = MetadataHelper.GetMetadataobject<MunicipalityLinked>(
                data,
                MetadataHelper.GetMetadataforMunicipality
            ); //GetMetadata(data.Id, "municipality", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("municipality", data.SmgActive);

            return data;
        }

        public static MunicipalityLinked GetMunicipalityPGObject(Municipality data)
        {
            MunicipalityLinked linked = new MunicipalityLinked();

            linked.Id = data.Id.ToUpper();

            linked.Active = data.Active;
            linked.ContactInfos = data.ContactInfos;
            linked.CustomId = data.CustomId;
            linked.Detail = data.Detail;
            linked.DistanceInfo = data.DistanceInfo;
            linked.FirstImport = data.FirstImport;
            linked.GpsInfo = data.ConvertGpsInfoOnRootToGpsInfoArray();
            linked.GpsPolygon = data.GpsPolygon;
            linked.HasLanguage = data.HasLanguage;
            linked.ImageGallery = data.ImageGallery;
            linked.LastChange = data.LastChange;
            linked.LicenseInfo = data.LicenseInfo;
            linked.Mapping = data.Mapping;
            linked.RelatedContent = data.RelatedContent;
            linked.Shortname = data.Shortname;
            linked.SmgActive = data.SmgActive;
            linked.SmgTags = data.SmgTags;
            linked.Source = data.Source;
            linked.VisibleInSearch = data.VisibleInSearch;
            linked.Inhabitants = data.Inhabitants;
            linked.IstatNumber = data.IstatNumber;
            linked.Plz = data.Plz;
            linked.RegionId = data.RegionId;
            linked.SiagId = data.SiagId;
            linked.TourismvereinId = data.TourismvereinId;

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                linked.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                linked.Source = "idm";
            else
                linked.Source = data.Source.ToLower();

            linked._Meta = MetadataHelper.GetMetadataobject<MunicipalityLinked>(
                linked,
                MetadataHelper.GetMetadataforMunicipality
            ); //GetMetadata(data.Id, "municipality", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("municipality", data.SmgActive);

            return linked;
        }

        public static DistrictLinked GetDistrictPGObject(DistrictLinked data)
        {
            data.Id = data.Id.ToUpper();
            if (data.SmgTags != null && data.SmgTags.Count > 0)
                data.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                data.Source = "idm";
            else
                data.Source = data.Source.ToLower();

            data._Meta = MetadataHelper.GetMetadataobject<DistrictLinked>(
                data,
                MetadataHelper.GetMetadataforDistrict
            ); //GetMetadata(data.Id, "district", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("district", data.SmgActive);

            return data;
        }

        public static DistrictLinked GetDistrictPGObject(District data)
        {
            DistrictLinked linked = new DistrictLinked();

            linked.Id = data.Id.ToUpper();

            linked.Active = data.Active;
            linked.ContactInfos = data.ContactInfos;
            linked.CustomId = data.CustomId;
            linked.Detail = data.Detail;
            linked.DistanceInfo = data.DistanceInfo;
            linked.FirstImport = data.FirstImport;
            linked.GpsInfo = data.ConvertGpsInfoOnRootToGpsInfoArray();
            linked.GpsPolygon = data.GpsPolygon;
            linked.HasLanguage = data.HasLanguage;
            linked.ImageGallery = data.ImageGallery;
            linked.LastChange = data.LastChange;
            linked.LicenseInfo = data.LicenseInfo;
            linked.Mapping = data.Mapping;
            linked.RelatedContent = data.RelatedContent;
            linked.Shortname = data.Shortname;
            linked.SmgActive = data.SmgActive;
            linked.SmgTags = data.SmgTags;
            linked.Source = data.Source;
            linked.VisibleInSearch = data.VisibleInSearch;
            linked.IsComune = data.IsComune;
            linked.MunicipalityId = data.MunicipalityId;
            linked.RegionId = data.RegionId;
            linked.SiagId = data.SiagId;
            linked.TourismvereinId = data.TourismvereinId;

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                linked.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                linked.Source = "idm";
            else
                linked.Source = data.Source.ToLower();

            linked._Meta = MetadataHelper.GetMetadataobject<DistrictLinked>(
                linked,
                MetadataHelper.GetMetadataforDistrict
            ); //GetMetadata(data.Id, "district", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("district", data.SmgActive);

            return linked;
        }

        public static ExperienceAreaLinked GetExperienceAreaPGObject(ExperienceAreaLinked data)
        {
            data.Id = data.Id.ToUpper();
            if (data.SmgTags != null && data.SmgTags.Count > 0)
                data.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                data.Source = "idm";
            else
                data.Source = data.Source.ToLower();

            data._Meta = MetadataHelper.GetMetadataobject<ExperienceAreaLinked>(
                data,
                MetadataHelper.GetMetadataforExperienceArea
            ); //GetMetadata(data.Id, "experiencearea", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("experiencearea", data.SmgActive);

            return data;
        }

        public static ExperienceAreaLinked GetExperienceAreaPGObject(ExperienceArea data)
        {
            ExperienceAreaLinked linked = new ExperienceAreaLinked();

            linked.Id = data.Id.ToUpper();

            linked.Active = data.Active;
            linked.ContactInfos = data.ContactInfos;
            linked.CustomId = data.CustomId;
            linked.Detail = data.Detail;
            linked.DistanceInfo = data.DistanceInfo;
            linked.DistrictIds = data.DistrictIds;
            linked.FirstImport = data.FirstImport;
            linked.GpsInfo = data.ConvertGpsInfoOnRootToGpsInfoArray();
            linked.GpsPolygon = data.GpsPolygon;
            linked.HasLanguage = data.HasLanguage;
            linked.ImageGallery = data.ImageGallery;
            linked.LastChange = data.LastChange;
            linked.LicenseInfo = data.LicenseInfo;
            linked.Mapping = data.Mapping;
            linked.RelatedContent = data.RelatedContent;
            linked.Shortname = data.Shortname;
            linked.SmgActive = data.SmgActive;
            linked.SmgTags = data.SmgTags;
            linked.Source = data.Source;
            linked.TourismvereinIds = data.TourismvereinIds;
            linked.VisibleInSearch = data.VisibleInSearch;

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                linked.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                linked.Source = "idm";
            else
                linked.Source = data.Source.ToLower();

            linked._Meta = MetadataHelper.GetMetadataobject<ExperienceAreaLinked>(
                linked,
                MetadataHelper.GetMetadataforExperienceArea
            ); //GetMetadata(data.Id, "experiencearea", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("experiencearea", data.SmgActive);

            return linked;
        }

        public static AreaLinked GetAreaPGObject(AreaLinked data)
        {
            data.Id = data.Id.ToUpper();
            data._Meta = MetadataHelper.GetMetadataobject<AreaLinked>(
                data,
                MetadataHelper.GetMetadataforArea
            ); //GetMetadata(data.Id, "area", "lts", data.LastChange);

            if (String.IsNullOrEmpty(data.Source))
                data.Source = "lts";
            else
                data.Source = data.Source.ToLower();

            return data;
        }

        public static SkiAreaLinked GetSkiAreaPGObject(SkiAreaLinked data)
        {
            data.Id = data.Id.ToUpper();
            if (data.SmgTags != null && data.SmgTags.Count > 0)
                data.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                data.Source = "idm";
            else
                data.Source = data.Source.ToLower();

            data._Meta = MetadataHelper.GetMetadataobject<SkiAreaLinked>(
                data,
                MetadataHelper.GetMetadataforSkiArea
            ); //GetMetadata(data.Id, "skiarea", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("skiarea", data.SmgActive);

            return data;
        }

        public static SkiAreaLinked GetSkiAreaPGObject(SkiAreaRaven data)
        {
            SkiAreaLinked linked = new SkiAreaLinked();

            linked.Id = data.Id.ToUpper();

            linked.Active = data.Active;
            linked.ContactInfos = data.ContactInfos;
            linked.CustomId = data.CustomId;
            linked.Detail = data.Detail;
            linked.DistanceInfo = data.DistanceInfo;
            linked.DistrictIds = data.DistrictIds;
            linked.FirstImport = data.FirstImport;
            linked.GpsInfo = data.ConvertGpsInfoOnRootToGpsInfoArray();
            linked.GpsPolygon = data.GpsPolygon;
            linked.HasLanguage = data.HasLanguage;
            linked.ImageGallery = data.ImageGallery;
            linked.LastChange = data.LastChange;
            linked.LicenseInfo = data.LicenseInfo;
            linked.Mapping = data.Mapping;
            linked.RegionIds = data.RegionIds;
            linked.RelatedContent = data.RelatedContent;
            linked.Shortname = data.Shortname;
            linked.SmgActive = data.SmgActive;
            linked.SmgTags = data.SmgTags;
            linked.Source = data.Source;
            linked.TourismvereinIds = data.TourismvereinIds;
            linked.AltitudeFrom = data.AltitudeFrom;
            linked.AltitudeTo = data.AltitudeTo;
            linked.AreaId = data.AreaId;
            linked.AreaRadius = data.AreaRadius;
            linked.LiftCount = data.LiftCount;
            linked.LocationInfo = data.LocationInfo;
            linked.MunicipalityIds = data.MunicipalityIds;
            linked.OperationSchedule = data.OperationSchedule;
            linked.SkiAreaMapURL = data.SkiAreaMapURL;
            linked.SkiRegionId = data.SkiRegionId;
            linked.SkiRegionName = data.SkiRegionName;
            linked.SlopeKmBlack = data.SlopeKmBlack;
            linked.SlopeKmBlue = data.SlopeKmBlue;
            linked.SlopeKmRed = data.SlopeKmRed;
            linked.TotalSlopeKm = data.TotalSlopeKm;

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                linked.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                linked.Source = "idm";
            else
                linked.Source = data.Source.ToLower();

            linked._Meta = MetadataHelper.GetMetadataobject<SkiAreaLinked>(
                linked,
                MetadataHelper.GetMetadataforSkiArea
            ); //GetMetadata(data.Id, "skiarea", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("skiarea", data.SmgActive);

            return linked;
        }

        public static SkiRegionLinked GetSkiRegionPGObject(SkiRegionLinked data)
        {
            data.Id = data.Id.ToUpper();

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                data.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                data.Source = "idm";
            else
                data.Source = data.Source.ToLower();

            data._Meta = MetadataHelper.GetMetadataobject<SkiRegionLinked>(
                data,
                MetadataHelper.GetMetadataforSkiRegion
            ); //GetMetadata(data.Id, "skiregion", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("skiregion", data.SmgActive);

            return data;
        }

        public static SkiRegionLinked GetSkiRegionPGObject(SkiRegion data)
        {
            SkiRegionLinked linked = new SkiRegionLinked();

            linked.Id = data.Id.ToUpper();

            linked.Active = data.Active;
            linked.ContactInfos = data.ContactInfos;
            linked.CustomId = data.CustomId;
            linked.Detail = data.Detail;
            linked.DistanceInfo = data.DistanceInfo;
            linked.FirstImport = data.FirstImport;
            linked.GpsInfo = data.ConvertGpsInfoOnRootToGpsInfoArray();
            linked.GpsPolygon = data.GpsPolygon;
            linked.HasLanguage = data.HasLanguage;
            linked.ImageGallery = data.ImageGallery;
            linked.LastChange = data.LastChange;
            linked.LicenseInfo = data.LicenseInfo;
            linked.Mapping = data.Mapping;
            linked.RelatedContent = data.RelatedContent;
            linked.Shortname = data.Shortname;
            linked.SmgActive = data.SmgActive;
            linked.SmgTags = data.SmgTags;
            linked.Source = data.Source;

            if (data.SmgTags != null && data.SmgTags.Count > 0)
                linked.SmgTags = data.SmgTags.Select(x => x.ToLower()).ToList();

            if (String.IsNullOrEmpty(data.Source))
                linked.Source = "idm";
            else
                linked.Source = data.Source.ToLower();

            linked._Meta = MetadataHelper.GetMetadataobject<SkiRegionLinked>(
                linked,
                MetadataHelper.GetMetadataforSkiRegion
            ); //GetMetadata(data.Id, "skiregion", "idm", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("skiregion", data.SmgActive);

            return linked;
        }

        public static WineLinked GetWinePGObject(WineLinked data)
        {
            data.Id = data.Id.ToUpper();

            if (String.IsNullOrEmpty(data.Source))
                data.Source = "suedtirolwein";
            else
                data.Source = data.Source.ToLower();

            data._Meta = MetadataHelper.GetMetadataobject<WineLinked>(
                data,
                MetadataHelper.GetMetadataforWineAward
            ); //GetMetadata(data.Id, "wineaward", "suedtirolwein", data.LastChange);
            //data.PublishedOn = PublishedOnHelper.GetPublishenOnList("wine", data.SmgActive);

            return data;
        }

        public static ODHTagLinked GetODHTagPGObject(ODHTagLinked data)
        {
            data.Id = data.Id.ToLower();

            data.MainEntity = data.MainEntity.ToLower();

            List<string> validforentitynew = new List<string>();
            foreach (var validforentity in data.ValidForEntity)
            {
                if (validforentity.ToLower() == "smgpoi")
                    validforentitynew.Add("odhactivitypoi");
                else
                    validforentitynew.Add(validforentity.ToLower());
            }

            data.ValidForEntity = validforentitynew;

            if (!data.ValidForEntity.Contains(data.MainEntity))
                data.ValidForEntity.Add(data.MainEntity);

            if (data.MainEntity == "smgpoi" && !data.ValidForEntity.Contains("odhactivitypoi"))
                validforentitynew.Add("odhactivitypoi");

            //Lowercase Mappings
            if (data.MappedTagIds != null && data.MappedTagIds.Count > 0)
            {
                data.MappedTagIds = data.MappedTagIds.ConverListToLowerCase().ToList();
            }

            //Lowercase Sources
            if (data.Source != null && data.Source.Count > 0)
            {
                data.Source = data.Source.ConverListToLowerCase().ToList();
            }

            //Lowercase MappedTagIds
            if (data.Mapping != null && data.Mapping.Count > 0)
            {
                IDictionary<string, IDictionary<string, string>> mappingdict =
                    new Dictionary<string, IDictionary<string, string>>();

                foreach (var dictkvp in data.Mapping)
                {
                    mappingdict.Add(dictkvp.Key, dictkvp.Value.ConvertToLowercase(false, true));
                }

                data.Mapping = mappingdict;
            }

            data._Meta = MetadataHelper.GetMetadataobject<ODHTagLinked>(
                data,
                MetadataHelper.GetMetadataforOdhTag
            ); //GetMetadata(data.Id, "wineaward", "suedtirolwein", data.LastChange);

            if (data.PublishedOn == null)
                data.PublishedOn = new List<string>();

            //Adding LicenseInfo to ODHTag (not present on sinfo instance)
            data.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<ODHTagLinked>(
                data,
                Helper.LicenseHelper.GetLicenseforODHTag
            );

            //Change, Get the Publishedon directly from raven instance

            ////Hack Publishedon because ODHTag not implementing ISource
            //if (data.DisplayAsCategory != null && data.DisplayAsCategory.Value == true)
            //{
            //    //IF list is null instantiate it otherwise it will be empty
            //    data.PublishedOn = data.PublishedOn ?? new List<string>();

            //    data.PublishedOn.TryAddOrUpdateOnList("idm-marketplace");
            //}

            ////If Redactional Tag activate it
            //if (data.Source != null && data.Source.Contains("IDMRedactionalCategory"))
            //{
            //    //IF list is null instantiate it otherwise it will be empty
            //    data.PublishedOn = data.PublishedOn ?? new List<string>();

            //    data.PublishedOn.TryAddOrUpdateOnList("idm-marketplace");
            //}

            return data;
        }
    }
}
