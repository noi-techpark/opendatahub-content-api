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
    public class ReduceDataTransformer
    {
        public static V GetReducedObject<T, V>(T myobject, Func<T, V> reducedmodelgenerator)
        {
            return reducedmodelgenerator(myobject);
        }

        //Check if data should be reduced
        public static bool ReduceDataCheck<T>(T myobject)
            where T : ISource, ILicenseInfo
        {
            return myobject switch
            {
                Accommodation or AccommodationLinked or AccommodationV2 => myobject.Source != null
                    ? myobject.Source.ToLower() == "lts" && !myobject.LicenseInfo.ClosedData
                        ? true
                        : false
                    : false,
                LTSActivityLinked => myobject.Source != null
                    ? myobject.Source.ToLower() == "lts" && !myobject.LicenseInfo.ClosedData
                        ? true
                        : false
                    : false,
                LTSPoiLinked => myobject.Source != null
                    ? myobject.Source.ToLower() == "lts" && !myobject.LicenseInfo.ClosedData
                        ? true
                        : false
                    : false,
                GastronomyLinked => myobject.Source != null
                    ? myobject.Source.ToLower() == "lts" && !myobject.LicenseInfo.ClosedData
                        ? true
                        : false
                    : false,
                Event or EventLinked => myobject.Source != null
                    ? myobject.Source.ToLower() == "lts" && !myobject.LicenseInfo.ClosedData
                        ? true
                        : false
                    : false,
                ODHActivityPoi or ODHActivityPoiLinked => myobject.Source != null
                    ? myobject.Source.ToLower() == "lts" && !myobject.LicenseInfo.ClosedData
                        ? true
                        : false
                    : false,
                Measuringpoint or MeasuringpointLinked => myobject.Source != null
                    ? myobject.Source.ToLower() == "lts" && !myobject.LicenseInfo.ClosedData
                        ? true
                        : false
                    : false,
                WebcamInfo or WebcamInfoLinked => myobject.Source != null
                    ? myobject.Source.ToLower() == "lts" && !myobject.LicenseInfo.ClosedData
                        ? true
                        : false
                    : false,
                VenueLinked => myobject.Source != null
                    ? myobject.Source.ToLower() == "lts" && !myobject.LicenseInfo.ClosedData
                        ? true
                        : false
                    : false,
                DDVenue => myobject.Source != null
                    ? myobject.Source.ToLower() == "lts" && !myobject.LicenseInfo.ClosedData
                        ? true
                        : false
                    : false,
                _ => false,
            };
        }

        //LTS ActivityData
        public static LTSActivityLinkedReduced CopyLTSActivityToReducedObject(
            LTSActivityLinked myactivity
        )
        {
            var reduced = new LTSActivityLinkedReduced();

            reduced.Id = myactivity.Id + "_REDUCED";
            //Activity/Number
            reduced.WayNumber = myactivity.WayNumber;
            //Features/IsWithLight
            reduced.IsWithLigth = myactivity.IsWithLigth;
            //Features/HasRentals
            reduced.HasRentals = myactivity.HasRentals;
            //Features/LiftAvailable
            reduced.LiftAvailable = myactivity.LiftAvailable;
            //Features/FeetClimb
            reduced.FeetClimb = myactivity.FeetClimb;
            //Features/BikeTransport
            reduced.BikeTransport = myactivity.BikeTransport;
            //Features/isOpen
            reduced.IsOpen = myactivity.IsOpen;

            //Position/Longitude,Latitude,Altitude
            //reduced.GpsPoints = myactivity.GpsPoints;
            reduced.GpsInfo = myactivity.GpsInfo;

            //Name
            reduced.Detail = ReducedDataHelper.ReduceDetailInfo(myactivity.Detail);
            //ContactInfo/URL
            reduced.ContactInfos = ReducedDataHelper.ReduceContactInfoForActivity(
                myactivity.ContactInfos
            );

            //Tag
            reduced.SmgTags = myactivity.SmgTags;
            reduced.LTSTags =
                myactivity.LTSTags != null
                    ? ReducedDataHelper.ReduceLtsTags(myactivity.LTSTags).ToList()
                    : null;

            //ODH Fields
            reduced.Shortname = myactivity.Shortname;
            reduced.Type = myactivity.Type;
            reduced.SubType = myactivity.SubType;
            reduced.PoiType = myactivity.PoiType;
            reduced.AdditionalPoiInfos = myactivity.AdditionalPoiInfos;
            reduced.Source = myactivity.Source;

            //ODH Fields
            reduced.Active = myactivity.Active;
            reduced.SmgActive = myactivity.SmgActive;
            reduced.LastChange = myactivity.LastChange;
            reduced.FirstImport = myactivity.FirstImport;
            reduced.HasLanguage = myactivity.HasLanguage;

            ///LocationInfo, ODH Object calculated with
            reduced.LocationInfo = ReducedDataHelper.RemoveAreafromLocationInfo(
                myactivity.LocationInfo
            );

            //License + Meta
            reduced.LicenseInfo = myactivity.LicenseInfo;
            reduced._Meta = MetadataHelper.GetMetadata(
                reduced.Id,
                "ltsactivity",
                "lts",
                reduced.LastChange,
                true
            );
            //reduced.PublishedOn = HelperClass.GetPublishenOnList("ltsactivity", reduced.SmgActive);

            //ImageGallery
            reduced.ImageGallery = ReducedDataHelper.ReduceImagesToCC0Only(myactivity.ImageGallery);

            return reduced;
        }

        //LTS PoiData
        public static LTSPoiLinkedReduced CopyLTSPoiToReducedObject(LTSPoiLinked mypoi)
        {
            var reduced = new LTSPoiLinkedReduced();

            //TODO
            reduced.Id = mypoi.Id + "_REDUCED";
            reduced.HasFreeEntrance = mypoi.HasFreeEntrance; //Features/HasFreeEntrance
            //Position/Longitude,Latitude,Altitude
            reduced.GpsInfo = mypoi.GpsInfo;
            //reduced.GpsPoints = mypoi.GpsPoints;

            reduced.Detail = ReducedDataHelper.ReduceDetailInfo(mypoi.Detail);
            reduced.ContactInfos = ReducedDataHelper.ReduceContactInfoForPoi(mypoi.ContactInfos);

            //Tag
            reduced.SmgTags = mypoi.SmgTags;
            reduced.LTSTags =
                mypoi.LTSTags != null
                    ? ReducedDataHelper.ReduceLtsTags(mypoi.LTSTags).ToList()
                    : null;

            //ODH Fields
            reduced.Active = mypoi.Active;
            reduced.SmgActive = mypoi.SmgActive;
            reduced.LastChange = mypoi.LastChange;
            reduced.FirstImport = mypoi.FirstImport;
            reduced.HasLanguage = mypoi.HasLanguage;
            reduced.Shortname = mypoi.Shortname;
            reduced.AdditionalPoiInfos = mypoi.AdditionalPoiInfos;
            reduced.Type = mypoi.Type;
            reduced.SubType = mypoi.SubType;
            reduced.PoiType = mypoi.PoiType;
            reduced.Source = mypoi.Source;

            ///LocationInfo, ODH Object calculated with
            reduced.LocationInfo = ReducedDataHelper.RemoveAreafromLocationInfo(mypoi.LocationInfo);

            //License + Meta
            reduced.LicenseInfo = mypoi.LicenseInfo;
            reduced._Meta = MetadataHelper.GetMetadata(
                reduced.Id,
                "ltspoi",
                "lts",
                reduced.LastChange,
                true
            );
            //reduced.PublishedOn = HelperClass.GetPublishenOnList("ltspoi", reduced.SmgActive);

            //ImageGallery
            reduced.ImageGallery = ReducedDataHelper.ReduceImagesToCC0Only(mypoi.ImageGallery);

            return reduced;
        }

        //LTS Gastronomic Data ACTIVE: (IsEnabled=1 and RepresentationRestricition=1)
        public static GastronomyLinkedReduced CopyLTSGastronomyToReducedObject(
            GastronomyLinked mypoi
        )
        {
            var reduced = new GastronomyLinkedReduced();

            reduced.Id = mypoi.Id + "_REDUCED";
            //reduced.GpsPoints = mypoi.GpsPoints; read only
            //reduced.Latitude = mypoi.Latitude;
            //reduced.Longitude = mypoi.Longitude;
            //reduced.Gpstype = mypoi.Gpstype;
            //reduced.Altitude = mypoi.Altitude;
            //reduced.AltitudeUnitofMeasure = mypoi.AltitudeUnitofMeasure;
            reduced.GpsInfo = mypoi.GpsInfo;

            //ContactInfo/CompanyName
            reduced.Detail = ReducedDataHelper.ReduceDetailInfo(mypoi.Detail);
            //ContactInfo/CompanyName
            reduced.ContactInfos = ReducedDataHelper.ReduceContactInfoForGastronomy(
                mypoi.ContactInfos
            );

            //CategoryCodes/GastronomicCategory
            reduced.CategoryCodes = mypoi.CategoryCodes;

            //ODH Fields
            reduced.Active = mypoi.Active;
            reduced.SmgActive = mypoi.SmgActive;
            reduced.LastChange = mypoi.LastChange;
            reduced.FirstImport = mypoi.FirstImport;
            reduced.HasLanguage = mypoi.HasLanguage;
            reduced.Shortname = mypoi.Shortname;
            reduced.Source = mypoi.Source;

            ///LocationInfo, ODH Object calculated with
            reduced.LocationInfo = ReducedDataHelper.RemoveAreafromLocationInfo(mypoi.LocationInfo);

            //License + Meta
            reduced.LicenseInfo = mypoi.LicenseInfo;
            reduced._Meta = MetadataHelper.GetMetadata(
                reduced.Id,
                "ltsgastronomy",
                "lts",
                reduced.LastChange,
                true
            );
            //reduced.PublishedOn = HelperClass.GetPublishenOnList("ltsgastronomy", reduced.SmgActive);

            //ImageGallery
            reduced.ImageGallery = ReducedDataHelper.ReduceImagesToCC0Only(mypoi.ImageGallery);

            return reduced;
        }

        //LTS PoiData ActivityData
        public static LTSODHActivityPoiReduced CopyLTSODHActivtyPoiToReducedObject(
            ODHActivityPoiLinked mypoi
        )
        {
            var reduced = new LTSODHActivityPoiReduced();

            reduced.Id = mypoi.Id + "_reduced";

            //If Activity
            if (mypoi.SyncSourceInterface == "activitydata")
            {
                //Activity/Number
                reduced.WayNumber = mypoi.WayNumber;
                //Features/IsWithLight
                reduced.IsWithLigth = mypoi.IsWithLigth;
                //Features/HasRentals
                reduced.HasRentals = mypoi.HasRentals;
                //Features/LiftAvailable
                reduced.LiftAvailable = mypoi.LiftAvailable;
                //Features/FeetClimb
                reduced.FeetClimb = mypoi.FeetClimb;
                //Features/BikeTransport
                reduced.BikeTransport = mypoi.BikeTransport;
                //Features/isOpen
                reduced.IsOpen = mypoi.IsOpen;
            }

            //If Poi
            if (mypoi.SyncSourceInterface == "poidata")
            {
                reduced.HasFreeEntrance = mypoi.HasFreeEntrance; //Features/HasFreeEntrance
            }

            //If Gastronomy
            if (mypoi.SyncSourceInterface == "gastronomicdata")
            {
                reduced.CategoryCodes = mypoi.CategoryCodes;
            }

            //Tag
            reduced.SmgTags = mypoi.SmgTags;
            reduced.LTSTags =
                mypoi.LTSTags != null
                    ? ReducedDataHelper.ReduceLtsTags(mypoi.LTSTags).ToList()
                    : null;

            //Tagging
            reduced.Tags = mypoi.Tags;
            reduced.TagIds = mypoi.TagIds;

            reduced.Detail = ReducedDataHelper.ReduceDetailInfo(mypoi.Detail);
            reduced.ContactInfos = ReducedDataHelper.ReduceContactInfoForODHActivityPoi(
                mypoi.ContactInfos,
                mypoi.SyncSourceInterface
            );

            //Position/Longitude,Latitude,Altitude
            //reduced.GpsPoints = mypoi.GpsPoints;
            reduced.GpsInfo = mypoi.GpsInfo;

            //ODH Fields
            reduced.Shortname = mypoi.Shortname;
            reduced.Type = mypoi.Type;
            reduced.SubType = mypoi.SubType;
            reduced.PoiType = mypoi.PoiType;
            reduced.AdditionalPoiInfos = mypoi.AdditionalPoiInfos;

            //ODH Fields
            reduced.Active = mypoi.Active;
            reduced.SmgActive = mypoi.SmgActive;
            reduced.LastChange = mypoi.LastChange;
            reduced.FirstImport = mypoi.FirstImport;
            reduced.HasLanguage = mypoi.HasLanguage;
            reduced.Source = mypoi.Source;
            reduced.SyncSourceInterface = mypoi.SyncSourceInterface;
            reduced.SyncUpdateMode = mypoi.SyncUpdateMode;

            ///LocationInfo, ODH Object calculated with
            reduced.LocationInfo = ReducedDataHelper.RemoveAreafromLocationInfo(mypoi.LocationInfo);

            //License + Meta
            reduced.LicenseInfo = mypoi.LicenseInfo;
            reduced._Meta = MetadataHelper.GetMetadata(
                reduced.Id,
                "odhactivitypoi",
                reduced.Source?.ToLower(),
                reduced.LastChange,
                true
            );
            reduced.PublishedOn = mypoi.PublishedOn;

            //ImageGallery
            reduced.ImageGallery = ReducedDataHelper.ReduceImagesToCC0Only(mypoi.ImageGallery);

            return reduced;
        }

        //LTS Accommodation OK ACTIVE (IDMActive=1)
        public static AccommodationLinkedReduced CopyLTSAccommodationToReducedObject(
            AccommodationV2 myacco
        )
        {
            var reduced = new AccommodationLinkedReduced();

            reduced.Id = myacco.Id + "_REDUCED";

            reduced.AccoDetail = ReducedDataHelper.ReduceAccoDetail(
                myacco.AccoDetail ?? new Dictionary<string, AccoDetail>()
            );

            //A1GEP, A1GNP, A0Alt
            //reduced.Gpstype = myacco.Gpstype;
            //reduced.Latitude = myacco.Latitude;
            //reduced.Longitude = myacco.Longitude;
            //reduced.Altitude = myacco.Altitude;
            //reduced.AltitudeUnitofMeasure = myacco.AltitudeUnitofMeasure;
            reduced.GpsInfo = myacco.GpsInfo;

            reduced.AccoProperties = new AccoProperties();

            //A0Roo
            reduced.AccoProperties.HasRoom = myacco.HasRoom;
            //A0App
            reduced.AccoProperties.HasApartment = myacco.HasApartment;

            //fix
            reduced.AccoProperties.IsBookable = false;

            //T6RID
            reduced.AccoCategoryId = myacco.AccoCategoryId;
            //T4RID
            reduced.AccoTypeId = myacco.AccoTypeId;

            //ODH Fields
            reduced.Active = myacco.Active;
            reduced.SmgActive = myacco.SmgActive;
            reduced.LastChange = myacco.LastChange;
            reduced.FirstImport = myacco.FirstImport;
            reduced.HasLanguage = myacco.HasLanguage;
            reduced.Shortname = myacco.Shortname;
            reduced.Source = myacco.Source;

            ///LocationInfo, ODH Object calculated with
            reduced.LocationInfo = ReducedDataHelper.RemoveAreafromLocationInfo(
                myacco.LocationInfo
            );

            //License + Meta
            reduced.LicenseInfo = myacco.LicenseInfo;
            reduced._Meta = MetadataHelper.GetMetadata(
                reduced.Id,
                "accommodation",
                "lts",
                reduced.LastChange,
                true
            );
            reduced.PublishedOn = myacco.PublishedOn;

            //ImageGallery
            reduced.ImageGallery = ReducedDataHelper.ReduceImagesToCC0Only(myacco.ImageGallery);

            return reduced;
        }

        //LTS Event
        public static EventLinkedReduced CopyLTSEventToReducedObject(EventLinked myevent)
        {
            var reduced = new EventLinkedReduced();

            //LTS ID(RID) (EvRID)
            reduced.Id = myevent.Id + "_REDUCED";

            //Definition/GEP, GNP
            //reduced.Gpstype = myevent.Gpstype;
            //reduced.Latitude = myevent.Latitude;
            //reduced.Longitude = myevent.Longitude;
            //reduced.Altitude = myevent.Altitude;
            //reduced.AltitudeUnitofMeasure = myevent.AltitudeUnitofMeasure;
            reduced.GpsInfo = myevent.GpsInfo;

            //DefinitionLng/Title
            reduced.Detail = ReducedDataHelper.ReduceDetailInfo(myevent.Detail);

            reduced.ContactInfos = ReducedDataHelper.ReduceContactInfoForEvent(
                myevent.ContactInfos
            );

            //Day/Date, DateTo, SingleDays, Begin, End
            reduced.DateBegin = myevent.DateBegin;
            reduced.DateEnd = myevent.DateEnd;
            //reduced.NextBeginDate = myevent.NextBeginDate;

            reduced.EventProperty = myevent.EventProperty;

            //Definition/Ticket
            reduced.EventProperty.RegistrationRequired = null;
            reduced.EventProperty.IncludedInSuedtirolGuestPass = null;
            reduced.EventProperty.EventClassificationId = null;
            reduced.EventProperty.EventOrganizerId = null;            

            //BookingData/BookingUrl
            reduced.EventBooking =
                myevent.EventBooking != null
                    ? ReducedDataHelper.ReduceEventBooking(myevent.EventBooking)
                    : null;

            //Evendate
            reduced.EventDate = ReducedDataHelper.ReduceEventDateCollection(
                myevent.EventDate ?? new List<EventDate>()
            );

            //ODH Fields
            reduced.Active = myevent.Active;
            reduced.SmgActive = myevent.SmgActive;
            reduced.LastChange = myevent.LastChange;
            reduced.FirstImport = myevent.FirstImport;
            reduced.HasLanguage = myevent.HasLanguage;
            reduced.Shortname = myevent.Shortname;
            reduced.Source = myevent.Source;

            ///LocationInfo, ODH Object calculated with
            reduced.LocationInfo = ReducedDataHelper.RemoveAreafromLocationInfo(
                myevent.LocationInfo
            );

            //License + Meta
            reduced.LicenseInfo = myevent.LicenseInfo;
            reduced._Meta = MetadataHelper.GetMetadata(
                reduced.Id,
                "event",
                "lts",
                reduced.LastChange,
                true
            );
            reduced.PublishedOn = myevent.PublishedOn;

            //ImageGallery
            reduced.ImageGallery = ReducedDataHelper.ReduceImagesToCC0Only(myevent.ImageGallery);

            return reduced;
        }

        //LTS Measuringpoint (Status/IsEnabled=1)
        public static MeasuringpointLinkedReduced CopyLTSMeasuringpointToReducedObject(
            MeasuringpointLinked measuringpoint
        )
        {
            var reduced = new MeasuringpointLinkedReduced();

            //LTS ID(RID) (EvRID)
            reduced.Id = measuringpoint.Id + "_REDUCED";

            //Name
            reduced.Shortname = measuringpoint.Shortname;
            //GeoData/Position/Longitude,Latitude,Altitude
            //reduced.Gpstype = measuringpoint.Gpstype;
            //reduced.Latitude = measuringpoint.Latitude;
            //reduced.Longitude = measuringpoint.Longitude;
            //reduced.Altitude = measuringpoint.Altitude;
            //reduced.AltitudeUnitofMeasure = measuringpoint.AltitudeUnitofMeasure;
            reduced.GpsInfo = measuringpoint.GpsInfo;

            //Observation / Temperature
            reduced.Temperature = measuringpoint.Temperature;

            //Snow / Height
            reduced.SnowHeight = measuringpoint.SnowHeight;

            //Snow / NewHeight
            reduced.newSnowHeight = measuringpoint.newSnowHeight;

            //Snow / DateLastSnow
            reduced.LastSnowDate = measuringpoint.LastSnowDate;

            //ODH Fields
            reduced.Active = measuringpoint.Active;
            reduced.SmgActive = measuringpoint.SmgActive;
            reduced.LastChange = measuringpoint.LastChange;
            reduced.LastUpdate = measuringpoint.LastUpdate;
            reduced.FirstImport = measuringpoint.FirstImport;
            reduced.LocationInfo = ReducedDataHelper.RemoveAreafromLocationInfo(
                measuringpoint.LocationInfo
            );
            reduced.Source = measuringpoint.Source;

            //License + Meta
            reduced.LicenseInfo = measuringpoint.LicenseInfo;
            reduced._Meta = MetadataHelper.GetMetadata(
                reduced.Id,
                "measuringpoint",
                "lts",
                reduced.LastChange,
                true
            );
            reduced.PublishedOn = measuringpoint.PublishedOn;

            reduced.SkiAreaIds = measuringpoint.SkiAreaIds;

            return reduced;
        }

        //LTS Venue
        public static DDVenueReduced CopyLTSVenueToReducedObject(DDVenue venue)
        {
            var reduced = new DDVenueReduced();

            //LTS ID(RID) (EvRID)
            reduced.Id = venue.Id + "_REDUCED";
            reduced.attributes = ReducedDataHelper.ReduceVenueAttributes(venue.attributes);
            reduced.meta = venue.meta;
            reduced.LastChange = venue.LastChange;
            reduced.Source = venue.Source;
            reduced.LicenseInfo = venue.odhdata?.LicenseInfo;
            reduced.links = venue.links;

            if (reduced.relationships != null)
            {
                reduced.relationships.multimediaDescriptions = null;
                reduced.relationships.subVenues =
                    venue.relationships?.subVenues != null
                        ? ReducedDataHelper.ReduceSubVenues(venue.relationships.subVenues)
                        : null;
            }

            reduced.odhdata = new ODHData();

            if (venue.odhdata is { })
            {
                //ODH Fields TODO
                reduced.odhdata.Active = venue.odhdata.Active;
                reduced.odhdata.ODHActive = venue.odhdata.ODHActive;
                reduced.odhdata.Shortname = venue.odhdata.Shortname;
                reduced.odhdata.SmgTags = venue.odhdata.SmgTags;
                reduced.odhdata.HasLanguage = venue.odhdata.HasLanguage;
                reduced.odhdata.Source = venue.odhdata.Source;
                reduced.odhdata.GpsInfo = ReducedDataHelper.ReduceGpsInfo(venue.odhdata.GpsInfo);
                //reduced.odhdata.GpsPoints = venue.odhdata.GpsPoints;
                reduced.odhdata.RoomCount = venue.odhdata.RoomCount;
                reduced.odhdata.SyncSourceInterface = venue.odhdata.SyncSourceInterface;
                reduced.odhdata.VenueCategory = venue.odhdata.VenueCategory;
                reduced.odhdata.RoomDetails = ReducedDataHelper.ReduceVenueRoomDetails(
                    venue.odhdata.RoomDetails
                );
            }

            ///LocationInfo, ODH Object calculated with
            reduced.odhdata.LocationInfo = ReducedDataHelper.RemoveAreafromLocationInfo(
                reduced.odhdata.LocationInfo
            );

            //License + Meta
            reduced.odhdata.LicenseInfo = venue.odhdata?.LicenseInfo;
            reduced._Meta = MetadataHelper.GetMetadata(
                reduced.Id,
                "venue",
                "lts",
                reduced.LastChange,
                true
            );
            reduced.odhdata.PublishedOn = venue.odhdata?.PublishedOn;

            return reduced;
        }

        public static VenueReduced CopyLTSVenueToReducedObject(VenueLinked venue)
        {
            var reduced = new VenueReduced();

            //LTS ID(RID) (EvRID)
            reduced.Id = venue.Id + "_REDUCED";
            reduced.LastChange = venue.LastChange;
            reduced.Source = venue.Source;

            //TODO
            //REDUCE DETAIL
            reduced.Detail =
                venue.Detail != null ? ReducedDataHelper.ReduceDetailInfo(venue.Detail) : null;
            //REDUCE CONTACTINFO
            reduced.ContactInfos =
                venue.ContactInfos != null
                    ? ReducedDataHelper.ReduceContactInfoForVenue(venue.ContactInfos)
                    : null;

            //if (reduced.relationships != null)
            //{
            //    reduced.relationships.multimediaDescriptions = null;
            //    reduced.relationships.subVenues = venue.relationships.subVenues != null ? ReduceSubVenues(venue.relationships.subVenues) : null;
            //}

            //ODH Fields TODO
            reduced.Active = venue.Active;
            reduced.SmgActive = venue.SmgActive;
            //reduced.ODHActive = venue.ODHActive;
            reduced.Shortname = venue.Shortname;
            reduced.SmgTags = venue.SmgTags;
            reduced.HasLanguage = venue.HasLanguage;
            reduced.Source = venue.Source;
            reduced.GpsInfo = ReducedDataHelper.ReduceGpsInfo(venue.GpsInfo);
            //reduced.GpsPoints = venue.odhdata.GpsPoints;
            reduced.RoomCount = venue.RoomCount;
            reduced.SyncSourceInterface = venue.SyncSourceInterface;
            reduced.VenueCategory = venue.VenueCategory;
            //REDUCE SUBVENUES
            reduced.RoomDetails = ReducedDataHelper.ReduceVenueRoomDetails(venue.RoomDetails);

            ///LocationInfo, ODH Object calculated with
            reduced.LocationInfo = ReducedDataHelper.RemoveAreafromLocationInfo(venue.LocationInfo);

            //License + Meta
            reduced.LicenseInfo = venue.LicenseInfo;
            reduced._Meta = MetadataHelper.GetMetadata(
                reduced.Id,
                "venue",
                "lts",
                reduced.LastChange,
                true
            );
            reduced.PublishedOn = venue.PublishedOn;

            //ImageGallery
            reduced.ImageGallery = ReducedDataHelper.ReduceImagesToCC0Only(venue.ImageGallery);

            return reduced;
        }

        //LTS WebcamInfo
        public static WebcamInfoLinkedReduced CopyLTSWebcamInfoToReducedObject(
            WebcamInfoLinked webcam
        )
        {
            var reduced = new WebcamInfoLinkedReduced();

            //LTS ID(RID) (EvRID)
            reduced.Id = webcam.Id + "_REDUCED";

            //URL, StreamURL
            reduced.WebCamProperties = new WebcamProperties();
            reduced.WebCamProperties.WebcamUrl = webcam.WebCamProperties.WebcamUrl;
            reduced.WebCamProperties.StreamUrl = webcam.WebCamProperties.StreamUrl;

            //ODH Fields
            reduced.Active = webcam.Active;
            reduced.SmgActive = webcam.SmgActive;
            reduced.LastChange = webcam.LastChange;
            reduced.FirstImport = webcam.FirstImport;
            reduced.Source = webcam.Source;
            //TO ASK? Webcamname?

            reduced.Detail = webcam.Detail;
            reduced.ImageGallery = webcam.ImageGallery;
            reduced.ContactInfos = webcam.ContactInfos;

            //License + Meta
            reduced.LicenseInfo = webcam.LicenseInfo;
            reduced._Meta = MetadataHelper.GetMetadata(
                reduced.Id,
                "webcam",
                "lts",
                reduced.LastChange,
                true
            );
            reduced.PublishedOn = webcam.PublishedOn;

            return reduced;
        }
    }

    public class ReducedDataHelper
    {
        public static IDictionary<string, Detail> ReduceDetailInfo(
            IDictionary<string, Detail> mydetail
        )
        {
            foreach (var value in mydetail.Values)
            {
                //value.Title = null;
                //value.Language = null;
                //value.MetaDesc = null;
                //value.MetaTitle = null;

                value.AdditionalText = null;
                value.AuthorTip = null;
                value.GetThereText = null;
                value.PublicTransportationInfo = null;
                value.SafetyInfo = null;
                value.BaseText = null;
                value.EquipmentInfo = null;
                value.ParkingInfo = null;

                value.Header = null;
                value.SubHeader = null;
                value.Keywords = null;
                value.IntroText = null;
            }

            return mydetail;
        }

        public static IDictionary<string, ContactInfos> ReduceContactInfoForActivity(
            IDictionary<string, ContactInfos> mycontactinfo
        )
        {
            foreach (var value in mycontactinfo.Values)
            {
                value.Address = null;
                value.City = null;
                value.CompanyName = null;
                value.CountryCode = null;
                value.CountryName = null;
                value.Email = null;
                value.Faxnumber = null;
                value.Givenname = null;
                value.LogoUrl = null;
                value.NamePrefix = null;
                value.Phonenumber = null;
                value.Surname = null;
                value.Tax = null;
                //value.Url = null;   //ContactInfo/URL
                value.Vat = null;
                value.ZipCode = null;
            }

            return mycontactinfo;
        }

        public static IDictionary<string, ContactInfos> ReduceContactInfoForPoi(
            IDictionary<string, ContactInfos> mycontactinfo
        )
        {
            foreach (var value in mycontactinfo.Values)
            {
                value.Address = null;
                value.City = null;
                value.CompanyName = null;
                value.CountryCode = null;
                value.CountryName = null;
                value.Email = null;
                value.Faxnumber = null;
                value.Givenname = null;
                value.LogoUrl = null;
                value.NamePrefix = null;
                value.Phonenumber = null;
                value.Surname = null;
                value.Tax = null;
                //value.Url = null;  //ContactInfo/URL
                value.Vat = null;
                value.ZipCode = null;
            }

            return mycontactinfo;
        }

        public static IDictionary<string, ContactInfos> ReduceContactInfoForODHActivityPoi(
            IDictionary<string, ContactInfos> mycontactinfo,
            string? source
        )
        {
            foreach (var value in mycontactinfo.Values)
            {
                if (source != "gastronomicdata")
                    value.CompanyName = null;

                value.Address = null;
                value.City = null;
                value.CountryCode = null;
                value.CountryName = null;
                value.Email = null;
                value.Faxnumber = null;
                value.Givenname = null;
                value.LogoUrl = null;
                value.NamePrefix = null;
                value.Phonenumber = null;
                value.Surname = null;
                value.Tax = null;
                //value.Url = null;  URL allowed
                value.Vat = null;
                value.ZipCode = null;
            }

            return mycontactinfo;
        }

        public static IDictionary<string, ContactInfos> ReduceContactInfoForEvent(
            IDictionary<string, ContactInfos> mycontactinfo
        )
        {
            foreach (var value in mycontactinfo.Values)
            {
                //value.Address = null;      DefinitionLng/Street
                //value.City = null;         DefinitionLng/City
                value.CompanyName = null;
                //value.CountryCode = null;  Definition / NatID
                //value.CountryName = null;
                value.Email = null;
                value.Faxnumber = null;
                value.Givenname = null;
                value.LogoUrl = null;
                value.NamePrefix = null;
                value.Phonenumber = null;
                value.Surname = null;
                value.Tax = null;
                //value.Url = null;        DefinitionLng/Web
                value.Vat = null;
                //value.ZipCode = null;     Definition/Zip
            }

            return mycontactinfo;
        }

        public static IDictionary<string, ContactInfos> ReduceContactInfoForGastronomy(
            IDictionary<string, ContactInfos> mycontactinfo
        )
        {
            foreach (var value in mycontactinfo.Values)
            {
                //value.Address = null;       //ContactInfo/Address/AddressLine
                //value.City = null;          //ContactInfo/Address/CityName
                //value.CompanyName = null;   //ContactInfo/CompanyName
                //value.CountryCode = null;   //ContactInfo/Address/CountryCode
                //value.CountryName = null;   //ContactInfo/Address/CountryName
                value.Email = null;
                value.Faxnumber = null;
                value.Givenname = null;
                value.LogoUrl = null;
                value.NamePrefix = null;
                //value.Phonenumber = null;   //ContactInfo/Phone/PhoneNumber of PhoneTechType = 1  (TO Verifiy)
                value.Surname = null;
                value.Tax = null;
                //value.Url = null;          //ContactInfo/URL
                value.Vat = null;
                //value.ZipCode = null;      //ContactInfo/Address/PostalCode
            }

            return mycontactinfo;
        }

        public static IDictionary<string, ContactInfos> ReduceContactInfoForVenue(
            IDictionary<string, ContactInfos> mycontactinfo
        )
        {
            foreach (var value in mycontactinfo.Values)
            {
                value.Address = null;
                //value.City = null;
                value.CompanyName = null;
                //value.CountryCode = null;
                //value.CountryName = null;
                value.Email = null;
                value.Faxnumber = null;
                value.Givenname = null;
                value.LogoUrl = null;
                value.NamePrefix = null;
                value.Phonenumber = null;
                value.Surname = null;
                value.Tax = null;
                //value.Url = null;  //ContactInfo/URL
                value.Vat = null;
                value.ZipCode = null;
            }

            return mycontactinfo;
        }

        public static IDictionary<string, AccoDetail> ReduceAccoDetail(
            IDictionary<string, AccoDetail> mydetail
        )
        {
            foreach (var value in mydetail.Values)
            {
                //value.City = null;            //A2Cit
                //value.CountryCode = null;     //NatID
                value.Email = null;
                value.Fax = null;
                value.Firstname = null;
                value.Lastname = null;
                value.Longdesc = null;
                value.Mobile = null;
                //value.Name = null;            //A2Nam
                value.NameAddition = null;
                //value.Phone = null;           //A1Pho
                value.Shortdesc = null;
                //value.Street = null;          //A2Str
                value.Vat = null;
                //value.Website = null;         //A3Web
                //value.Zip = null;             //A1ZIP
            }

            return mydetail;
        }

        public static ICollection<GpsInfo> ReduceGpsInfo(ICollection<GpsInfo> gpsinfos)
        {
            //Remove all GPSPoints which cannot be publicated

            return gpsinfos;
        }

        public static ICollection<LTSTagsLinked> ReduceLtsTags(ICollection<LTSTagsLinked>? ltstags)
        {
            if (ltstags != null)
            {
                //Remove all TIN Infos
                foreach (var ltstag in ltstags)
                {
                    ltstag.LTSTins = null;
                }
            }
            return ltstags ?? new List<LTSTagsLinked>();
        }

        public static LocationInfoLinked RemoveAreafromLocationInfo(LocationInfoLinked? locinfo)
        {
            if (locinfo != null)
                locinfo.AreaInfo = null;

            return locinfo ?? new();
        }

        public static EventBooking ReduceEventBooking(EventBooking eventBooking)
        {
            eventBooking.BookableFrom = null;
            eventBooking.BookableTo = null;
            eventBooking.AccommodationAssignment = null;

            return eventBooking;
        }

        public static ICollection<EventDate> ReduceEventDateCollection(
            ICollection<EventDate> eventdates
        )
        {
            foreach (var eventdate in eventdates)
            {
                //eventdate.From = null;    Day/Date
                //eventdate.To = null;      Day/DateTo
                //eventdate.SingleDays = null;  Day/SingleDays
                eventdate.MinPersons = null;
                eventdate.MaxPersons = null;
                //eventdate.Ticket = null;
                //eventdate.GpsNorth = null;
                //eventdate.GpsEast = null;
                //eventdate.Begin = null;   Day/Begin
                //eventdate.End = null;     Day/End
                eventdate.Entrance = null;
                //eventdate.InscriptionTill = null;
                eventdate.Active = null;
                eventdate.DayRID = null;
                eventdate.EventDateAdditionalInfo = null;
                //eventdate.EventDateAdditionalTime = null;
                eventdate.EventCalculatedDays = null;
                eventdate.Cancelled = null;                
                eventdate.EventDateOpeningInfo = null;
                eventdate.EventDateTicketInfo = null;
                eventdate.EventVariantIDs = null;
                eventdate.PriceFrom = null;                
            }

            return eventdates;
        }

        public static DDAttributes ReduceVenueAttributes(DDAttributes attributes)
        {
            attributes.address = ReduceVenueAttributesAddress(
                attributes.address ?? new List<DDAddress>()
            );
            attributes.beds = null;
            //attributes.categories = null; //attributes.categories
            attributes.description = null;
            attributes.geometries = ReduceVenueAttributesGeometries(
                attributes.geometries ?? new List<DDGeometry>()
            );
            attributes.howToArrive = null;

            //attributes.name = null;   attributes.name
            attributes.openingHours = null;
            attributes.shortName = null;
            //attributes.url = null;    //attributes.url

            return attributes;
        }

        public static ICollection<DDAddress> ReduceVenueAttributesAddress(
            ICollection<DDAddress> addresses
        )
        {
            foreach (var address in addresses)
            {
                address.categories = null;
                //address.city = null;        //attributes.address.city
                address.complement = null;
                address.contactPoint = null;
                //address.country = null;     //attributes.address.country
                address.region = null;
                //address.street = null;    //attributes.address.street
                address.zipcode = null; //attributes.address.zipcode
            }

            return addresses;
        }

        public static ICollection<DDGeometry> ReduceVenueAttributesGeometries(
            ICollection<DDGeometry> geometries
        )
        {
            List<DDGeometry> allowedgeometries = new List<DDGeometry>();

            foreach (var geometry in geometries)
            {
                //attributes.geometries/type=Point/coordinates
                if (geometry.type == "Point")
                    allowedgeometries.Add(geometry);
            }

            return allowedgeometries;
        }

        public static ICollection<DDSubVenue> ReduceSubVenues(ICollection<DDSubVenue> subvenues)
        {
            foreach (var subvenue in subvenues)
            {
                subvenue.attributes.url = null;
                subvenue.attributes.beds = null;
                //subvenue.attributes.name = null;      //subVenues.attributes.name
                subvenue.attributes.address = null;
                subvenue.attributes._abstract = null;
                subvenue.attributes.dimension = null;
                subvenue.attributes.shortName = null;
                subvenue.attributes.categories = null;
                subvenue.attributes.geometries = null;
                subvenue.attributes.description = null;
                subvenue.attributes.howToArrive = null;
                subvenue.attributes.openingHours = null;
                //subvenue.attributes.availableSetups = null;   //subVenues.availableSetups

                subvenue.FirstImport = null;
                subvenue.Id = null;
                subvenue.LastChange = null;
                subvenue.LicenseInfo = null;
                subvenue.links = null;
                subvenue.meta = null;
                subvenue.odhdata = null;
                subvenue.relationships = null;
                subvenue.Shortname = null;
                subvenue.type = null;
                subvenue._Meta = null;
            }

            return subvenues;
        }

        public static ICollection<VenueRoomDetails> ReduceVenueRoomDetails(
            ICollection<VenueRoomDetails> roomdetails
        )
        {
            foreach (var roomdetail in roomdetails)
            {
                roomdetail.VenueFeatures = null;
                roomdetail.SquareMeters = null;
                roomdetail.Indoor = null;
                roomdetail.Id = null;
                //roomdetail.VenueSetup = null;
                //roomdetail.Shortname = null;
                //

                roomdetail.Detail =
                    roomdetail.Detail != null ? ReduceDetailInfo(roomdetail.Detail) : null;
                roomdetail.ImageGallery = ReduceImagesToCC0Only(roomdetail.ImageGallery);
            }

            return roomdetails;
        }

        public static ICollection<ImageGallery> ReduceImagesToCC0Only(
            ICollection<ImageGallery> imagegallery
        )
        {
            if (imagegallery != null)
            {
                var imagescc0 = imagegallery.Where(x => x.License == "CC0").ToList();

                if (imagescc0.Count == 0)
                    return null;
                else
                    return imagescc0;
            }
            else
                return null;
        }
    }
}
