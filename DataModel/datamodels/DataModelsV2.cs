// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using DataModel.Annotations;
using Swashbuckle.AspNetCore.Annotations;

namespace DataModel
{
    #region Generic Datamodel

    public class Generic : 
        IIdentifiable, 
        IActivateable, 
        IPublishedOn, 
        IMetaData, 
        IMappingAware, 
        IGPSInfoAware, 
        ILicenseInfo, 
        ISource, 
        IHasTagInfo, 
        IShortName,
        IHasLanguage,
        IImportDateassigneable
    {
        public string? Id { get; set; }
        public Metadata? _Meta { get; set; }
        public LicenseInfo? LicenseInfo { get; set; }

        public string? Shortname { get; set; }
        public bool Active { get; set; }

        public DateTime? FirstImport { get; set; }
        public DateTime? LastChange { get; set; }
        public ICollection<string>? HasLanguage { get; set; }

        public ICollection<string>? PublishedOn { get; set; }
        public IDictionary<string, IDictionary<string, string>>? Mapping { get; set; }

        //We define what classes this Additionalproperties can be
        [PolymorphicDictionary(
            "EchargingDataProperties", typeof(EchargingDataProperties),
            "ActivityLtsDataProperties", typeof(ActivityLtsDataProperties),
            "PoiLtsDataProperties", typeof(PoiLtsDataProperties),
            "GastronomyLtsDataProperties", typeof(GastronomyLtsDataProperties),
            "PoiAgeDataProperties", typeof(PoiAgeDataProperties),
            "SuedtirolWeinCompanyDataProperties", typeof(SuedtirolWeinCompanyDataProperties),
            "RoadIncidentProperties", typeof(RoadIncidentProperties)
        )]
        public IDictionary<string, dynamic>? AdditionalProperties { get; set; }
        public string? Source { get; set; }

        public ICollection<Tags>? Tags { get; set; }
        public ICollection<string>? TagIds { get; set; }
        public ICollection<GpsInfo>? GpsInfo { get; set; }


    }

    #endregion

    #region EventsV2 Datamodel
    public class EventV2
        : IIdentifiable,
            IActivateable,
            IHasLanguage,
            IImageGalleryAware,
            IContactInfosAware,
            IMetaData,
            IMappingAware,
            IDetailInfosAware,
            ILicenseInfo,
            IPublishedOn,
            IVideoItemsAware,
            IImportDateassigneable,
            ISource,
            IHasTagInfo
    {
        public EventV2()
        {
            Detail = new Dictionary<string, Detail>();
            ContactInfos = new Dictionary<string, ContactInfos>();
            Mapping = new Dictionary<string, IDictionary<string, string>>();
            AdditionalProperties = new Dictionary<string, dynamic>();
            VideoItems = new Dictionary<string, ICollection<VideoItems>>();
        }

        //MetaData Information, Contains Source, LastUpdate
        public Metadata? _Meta { get; set; }

        //License Information
        public LicenseInfo? LicenseInfo { get; set; }

        //Self Link to this Data
        public string Self
        {
            get { return this.Id != null ? "EventV2/" + Uri.EscapeDataString(this.Id) : null; }
        }

        //Id Shortname and Active Info
        public string? Id { get; set; }
        public string? Shortname { get; set; }
        public bool Active { get; set; }

        //Firstimport and LastChange Section (Here for compatibility reasons could also be removed)
        public DateTime? FirstImport { get; set; }
        public DateTime? LastChange { get; set; }

        //Source
        public string? Source { get; set; }

        //HasLanguage, for which Languages the dataset has information
        public ICollection<string>? HasLanguage { get; set; }

        //Publishedon Array, Event is published for channel xy
        public ICollection<string>? PublishedOn { get; set; }

        //Mapping Section, to store Ids and other information of the data provider
        public IDictionary<string, IDictionary<string, string>> Mapping { get; set; }

        //RelatedContent, could be used to store Parent/Child Event Information
        public ICollection<RelatedContent>? RelatedContent { get; set; }

        //Indicates if this is a Parent Event
        public bool? IsRoot { get; set; }

        //Event Grouping Id, by flattening the Event here the same Id
        public string? EventGroupId { get; set; }

        //Dynamic AdditionalProperties field to store Provider Specific data that does not fit into the fields
        public IDictionary<string, dynamic> AdditionalProperties { get; set; }

        public ICollection<Tags> Tags { get; set; }

        public ICollection<string> TagIds { get; set; }

        //Description and Contactinfo
        public IDictionary<string, Detail> Detail { get; set; }
        public IDictionary<string, ContactInfos> ContactInfos { get; set; }

        //ImageGallery and Video Data
        public ICollection<ImageGallery>? ImageGallery { get; set; }
        public IDictionary<string, ICollection<VideoItems>>? VideoItems { get; set; }

        //Documents for this Event
        public IDictionary<string, List<DocumentDetailed>>? Documents { get; set; }

        //EventInfo Section contains all Infos about Event Dates, Venues etc....
        //public ICollection<EventInfo> EventInfo { get; set; }

        ////Each Event has a "main" Venue, to discuss if this
        //public List<string> VenueIds { get; set; }

        //[SwaggerSchema(Description = "generated field", ReadOnly = true)]
        //public ICollection<VenueLink> Venues
        //{
        //    get
        //    {
        //        return this.VenueIds != null ? this.VenueIds.Select(x => new VenueLink() { Id = x, Self = "VenueV2/" + x }).ToList() : new List<VenueLink>();
        //    }
        //}


        //Begin and Enddate
        public DateTime Begin { get; set; }
        public DateTime End { get; set; }

        //Begin and Enddate in UTC (could be created automatically)
        public double BeginUTC { get; set; }
        public double EndUTC { get; set; }

        //Each Event has a "main" Venue, to discuss if this
        public string VenueId { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public VenueLink Venue
        {
            get
            {
                return this.VenueId != null
                    ? new VenueLink() { Id = this.VenueId, Self = "VenueV2/" + this.VenueId }
                    : new VenueLink() { };
            }
        }

        //Capacity of the Event Venue Combination (not always the same as the Venue Capacity)
        public int? Capacity { get; set; }

        //TO Check, section for Event URLS?

        //TO Check, section for Booking Info

        //TO CHECK Add GpsInfo to have compatibility
        //public ICollection<GpsInfo>? GpsInfo { get; set; }
    }

    public class VenueLink
    {
        public string Id { get; set; }
        public string? Self { get; set; }
    }

    public class DocumentDetailed : Document
    {
        public string Description { get; set; }
        public string DocumentExtension { get; set; }
        public string DocumentMimeType { get; set; }
    }

    //SFSCon Specific

    public class EventDestinationDataInfo
    {
        public int InPersonCapacity { get; set; }
        public int OnlineCapacity { get; set; }
        public string ParticipationUrl { get; set; }
        public bool Recorded { get; set; }
        public string RegistrationUrl { get; set; }

        //series, sponsors, subEvents
    }

    //LTS Specific
    public class EventLTSInfo
    {
        public EventPublisher EventPublisher { get; set; }
        public bool SignOn { get; set; }
        public EventBooking EventBooking { get; set; }
        public EventPrice EventPrice { get; set; }
    }

    //EventShort Specific
    public class EventEuracNoiInfo
    {
        public bool? ExternalOrganizer { get; set; }
        public bool? SoldOut { get; set; }
        public AgeRange? TypicalAgeRange { get; set; }
        public string EventLocation { get; set; }
    }

    #endregion

    #region VenueV2 Datamodel

    public class VenueV2
        : IIdentifiable,
            IActivateable,
            IHasLanguage,
            IImageGalleryAware,
            IContactInfosAware,
            IMetaData,
            IMappingAware,
            IDetailInfosAware,
            ILicenseInfo,
            IPublishedOn,
            IVideoItemsAware,
            IImportDateassigneable,
            ISource,
            IHasTagInfo
    {
        public VenueV2()
        {
            Detail = new Dictionary<string, Detail>();
            ContactInfos = new Dictionary<string, ContactInfos>();
            Mapping = new Dictionary<string, IDictionary<string, string>>();
            AdditionalProperties = new Dictionary<string, dynamic>();
            VideoItems = new Dictionary<string, ICollection<VideoItems>>();
        }

        //MetaData Information, Contains Source, LastUpdate
        public Metadata? _Meta { get; set; }

        //License Information
        public LicenseInfo? LicenseInfo { get; set; }

        //Self Link to this Data
        public string Self
        {
            get { return this.Id != null ? "VenueV2/" + Uri.EscapeDataString(this.Id) : null; }
        }

        //Id Shortname and Active Info
        public string? Id { get; set; }
        public string? Shortname { get; set; }
        public bool Active { get; set; }
        public DateTime? FirstImport { get; set; }
        public DateTime? LastChange { get; set; }

        public string? Source { get; set; }

        //Language, Publishedon, Mapping and RelatedContent
        public ICollection<string>? HasLanguage { get; set; }
        public ICollection<string>? PublishedOn { get; set; }
        public IDictionary<string, IDictionary<string, string>> Mapping { get; set; }

        //We use RelatedContent to store Parent/Child Event Information
        public ICollection<RelatedContent>? RelatedContent { get; set; }

        //We only store the Info which is the Parent
        public bool? IsRoot { get; set; }
        public string? VenueGroupId { get; set; }

        public IDictionary<string, dynamic> AdditionalProperties { get; set; }

        //Description and Contactinfo
        public IDictionary<string, Detail> Detail { get; set; }
        public IDictionary<string, ContactInfos> ContactInfos { get; set; }

        //ImageGallery
        public ICollection<ImageGallery>? ImageGallery { get; set; }
        public IDictionary<string, ICollection<VideoItems>>? VideoItems { get; set; }

        public VenueInfo VenueInfo { get; set; }
        public LocationInfo? LocationInfo { get; set; }
        public ICollection<GpsInfo>? GpsInfo { get; set; }

        public DistanceInfo? DistanceInfo { get; set; }
        public ICollection<OperationSchedule>? OperationSchedule { get; set; }

        public ICollection<VenueSetupV2>? Capacity { get; set; }

        //All Categorization is done via Tags
        public ICollection<Tags> Tags { get; set; }
        public ICollection<string> TagIds { get; set; }

        //GpsPoints
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(true); }
        }
    }

    public class VenueSetupV2
    {
        public int Capacity { get; set; }

        //TODO Fill on Save
        public Tags Tag { get; set; }

        public string TagId { get; set; }
    }

    public class VenueInfo
    {
        public int? Beds { get; set; }
        public int? Rooms { get; set; }
        public int? SquareMeters { get; set; }
        public bool? Indoor { get; set; }
    }

    #endregion

    #region AccommodationV2 Datamodel

    public class AccommodationV2 : AccommodationLinked, IHasTagInfo
    {
        //New, holds all Infos of Trust You
        public IDictionary<string, Review>? Review { get; set; }

        //New, holds all Infos of Is/Has etc.. Properties
        public AccoProperties? AccoProperties { get; set; }

        //New, operationschedules also available on Accommodation
        public ICollection<OperationSchedule>? OperationSchedule { get; set; }

        //New Rateplans
        public ICollection<RatePlan>? RatePlan { get; set; }

        [SwaggerDeprecated("Deprecated, use Review.trustyou")]
        public new string? TrustYouID
        {
            get
            {
                return this.Review != null && this.Review.ContainsKey("trustyou")
                    ? this.Review["trustyou"].ReviewId
                    : "";
            }
        }

        [SwaggerDeprecated("Deprecated, use Review.trustyou")]
        public new double? TrustYouScore
        {
            get
            {
                return this.Review != null && this.Review.ContainsKey("trustyou")
                    ? this.Review["trustyou"].Score
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use Review.trustyou")]
        public new int? TrustYouResults
        {
            get
            {
                return this.Review != null && this.Review.ContainsKey("trustyou")
                    ? this.Review["trustyou"].Results
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use Review.trustyou")]
        public new bool? TrustYouActive
        {
            get
            {
                return this.Review != null && this.Review.ContainsKey("trustyou")
                    ? this.Review["trustyou"].Active
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use Review.trustyou")]
        public new int? TrustYouState
        {
            get
            {
                return this.Review != null && this.Review.ContainsKey("trustyou")
                    ? this.Review["trustyou"].StateInteger
                    : null;
            }
        }

        //Accommodation Properties

        [SwaggerDeprecated("Deprecated, use AccoProperties.HasApartment")]
        public new bool HasApartment
        {
            get
            {
                return this.AccoProperties != null ? this.AccoProperties.HasApartment.Value : false;
            }
        }

        [SwaggerDeprecated("Deprecated, use AccoProperties.HasRoom")]
        public new bool? HasRoom
        {
            get { return this.AccoProperties != null ? this.AccoProperties.HasRoom : null; }
        }

        [SwaggerDeprecated("Deprecated, use AccoProperties.IsCamping")]
        public new bool? IsCamping
        {
            get { return this.AccoProperties != null ? this.AccoProperties.IsCamping : null; }
        }

        [SwaggerDeprecated("Deprecated, use AccoProperties.IsGastronomy")]
        public bool? IsGastronomy
        {
            get { return this.AccoProperties != null ? this.AccoProperties.IsGastronomy : null; }
        }

        [SwaggerDeprecated("Deprecated, use AccoProperties.IsBookable")]
        public new bool IsBookable
        {
            get
            {
                return this.AccoProperties != null ? this.AccoProperties.IsBookable.Value : false;
            }
        }

        [SwaggerDeprecated("Deprecated, use AccoProperties.IsAccommodation")]
        public new bool? IsAccommodation
        {
            get { return this.AccoProperties != null ? this.AccoProperties.IsAccommodation : null; }
        }

        [SwaggerDeprecated("Deprecated, use AccoProperties.TVMember")]
        public new bool? TVMember
        {
            get { return this.AccoProperties != null ? this.AccoProperties.TVMember : null; }
        }

        //Tags
        //All Categorization is done via Tags
        public ICollection<Tags>? Tags { get; set; }
        public ICollection<string>? TagIds { get; set; }
    }

    public class AccommodationRoomV2 : AccommodationRoomLinked
    {
        //Overwrites The Features
        public new ICollection<AccoFeatureLinked>? Features { get; set; }

        //New Price From per Unit
        public Nullable<double> PriceFromPerUnit { get; set; }

        //New Accommodation Room Properties
        public AccommodationRoomProperties? Properties { get; set; }
    }

    //New Room Properties
    public class AccommodationRoomProperties
    {
        //New Properties
        public double? SquareMeters { get; set; }
        public int? SleepingRooms { get; set; }
        public int? Toilets { get; set; }
        public int? LivingRooms { get; set; }
        public int? DiningRooms { get; set; }
        public int? Baths { get; set; }
    }

    public class AccoProperties
    {
        public bool? HasApartment { get; set; }
        public bool? HasRoom { get; set; }
        public bool? IsCamping { get; set; }
        public bool? IsGastronomy { get; set; }
        public bool? IsBookable { get; set; }
        public bool? IsAccommodation { get; set; }
        public bool? HasDorm { get; set; }
        public bool? HasPitches { get; set; }
        public bool? TVMember { get; set; }

        //TO REMOVE?
        //public string? GastronomyId { get; set; }
        //public string? DistrictId { get; set; }
        //public string? TourismVereinId { get; set; }
        //public string? MainLanguage { get; set; }
    }

    //Shift Trust You To Reviews by using Dictionary
    public class Review
    {
        public string? ReviewId { get; set; }
        public double? Score { get; set; }
        public int? Results { get; set; }
        public bool? Active { get; set; }
        public string? State { get; set; }
        public int? StateInteger { get; set; }
        public string Provider { get; set; }
    }

    //Rateplans
    public class RatePlan
    {
        public RatePlan()
        {
            Name = new Dictionary<string, string>();
            ShortDescription = new Dictionary<string, string>();
            LongDescription = new Dictionary<string, string>();
        }

        public string ChargeType { get; set; }
        public string RatePlanId { get; set; }
        public string Code { get; set; }
        public IDictionary<string, string>? Name { get; set; }
        public IDictionary<string, string>? ShortDescription { get; set; }
        public IDictionary<string, string>? LongDescription { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? Visibility { get; set; }
    }

    public class AccommodationV2Helper
    {
        public static string GetTrustYouState(int trustyoustate)
        {
            //According to old LTS Documentation State (0=not rated, 1=do not display, 2=display)
            switch (trustyoustate)
            {
                case 2:
                    return "rated";
                case 1:
                    return "underValued";
                case 0:
                    return "notRated";
                default:
                    return "";
            }
        }
    }

    #endregion

    #region ODHActivityPoiV2

    public class ODHActivityPoiV2: ODHActivityPoiLinked
    {
        //Fits into Mapping
        public new string? OutdooractiveID { get; set; }
        public new string? OutdooractiveElevationID { get; set; }
        public new string? SmgId { get; set; }
        public new string? CustomId { get; set; }
        public new string? OwnerRid { get; set; }
        public new int? WayNumber { get; set; }
        public new string? Number { get; set; }

        //Properties to remove/rename/replace

        public new int? AgeFrom { get; set; }
        public new int? AgeTo { get; set; }
        public new int? MaxSeatingCapacity { get; set; }

        public new bool? CopyrightChecked { get; set; }
        public new string? Difficulty { get; set; }
        public new string? Type { get; set; }
        public new string? SubType { get; set; }
        public new string? PoiType { get; set; }
        public new bool SmgActive { get; set; }
        public new HashSet<string>? AreaId { get; set; }
        public new string? TourismorganizationId { get; set; }
        
        public new double? AltitudeDifference { get; set; }
        public new double? AltitudeHighestPoint { get; set; }
        public new double? AltitudeLowestPoint { get; set; }
        public new double? AltitudeSumUp { get; set; }
        public new double? AltitudeSumDown { get; set; }
        public new double? DistanceDuration { get; set; }
        public new double? DistanceLength { get; set; }


        public new bool? Highlight { get; set; }
        public new bool? IsOpen { get; set; }
        public new bool? IsPrepared { get; set; }
        public new bool? RunToValley { get; set; }
        public new bool? IsWithLigth { get; set; }
        public new bool? HasRentals { get; set; }
        public new bool? HasFreeEntrance { get; set; }
        public new bool? LiftAvailable { get; set; }
        public new bool? FeetClimb { get; set; }
        public new bool? BikeTransport { get; set; }

        
        //Related Content?
        public new List<string>? ChildPoiIds { get; set; }
        public new List<string>? MasterPoiIds { get; set; }

        public new string? SyncSourceInterface { get; set; }
        public new string? SyncUpdateMode { get; set; }
        public new ICollection<string>? PoiServices { get; set; }
        public new IDictionary<string, List<PoiProperty>> PoiProperty { get; set; }
    }

    #endregion

    #region AdditionalInfos

    //AdditionalInfos Centrotrevi
    public class AdditionalInfosCentroTrevi
    {
        public double Price { get; set; }
        public bool Ticket { get; set; }
        public string TicketInfo { get; set; }
    }

    #endregion

    #region ODHActivityPoiAdditionalProperties

    //To check if this is added as AdditionalProperty
    //ODHActivityPoiPropertiesLTSActivity
    //ODHActivityPoiPropertiesLTSPoi
    //ODHActivityPoiPropertiesLTSGastronomy
    //ODHActivityPoiPropertiesSuedtirolWein
    //ODHActivityPoiPropertiesEcharging

    public class PoiAgeDataProperties
    {        
        public int? AgeFrom { get; set; }
        public int? AgeTo { get; set; }
    }

    //LTS
    public class ActivityLtsDataProperties
    {
        public double? AltitudeDifference { get; set; }
        public double? AltitudeHighestPoint { get; set; }
        public double? AltitudeLowestPoint { get; set; }
        public double? AltitudeSumUp { get; set; }
        public double? AltitudeSumDown { get; set; }

        public double? DistanceDuration { get; set; }
        public double? DistanceLength { get; set; }

        public bool? IsOpen { get; set; }
        public bool? IsPrepared { get; set; }
        public bool? RunToValley { get; set; }
        public bool? IsWithLigth { get; set; }
        public bool? HasRentals { get; set; }        
        public bool? LiftAvailable { get; set; }
        public bool? FeetClimb { get; set; }

        public bool? BikeTransport { get; set; }

        public Ratings? Ratings { get; set; }
        public ICollection<string>? Exposition { get; set; }

        public int? WayNumber { get; set; }
        public string? Number { get; set; }

        public bool? MountainBikePermitted { get; set; }

        public string? LiftType { get; set; }
        public string? LiftCapacityType { get; set; }


        //rating.viaFerrataTechnique, rating.scaleUIAATechnique, rating.singletrackScale,
        //liftPointCard.pointsSingleTripUp, liftPointCard.pointsSingleTripDown
        //minRopeLength, quantityQuickDraws, snowType, snowPark.hasPipe, snowPark.linesNumber, snowPark.jumpsNumber, 
        //snowPark.isInground, snowPark.hasArtificiallySnow, snowPark.hasBoarderCross
    }

    public class PoiLtsDataProperties
    {        
        public bool? IsOpen { get; set; }
        public bool? HasFreeEntrance { get; set; }       
    }

    public class GastronomyLtsDataProperties
    {        
        public int? MaxSeatingCapacity { get; set; }
        public ICollection<CategoryCodes>? CategoryCodes { get; set; }
        public ICollection<DishRates>? DishRates { get; set; }
        public ICollection<CapacityCeremony>? CapacityCeremony { get; set; }
        public ICollection<Facilities>? Facilities { get; set; }
    }

    public class SuedtirolWeinCompanyDataProperties
    {        

        //TODO add Dictionary where it needs localization, categorize (socials) etc...

        public string OpeningtimesWineshop { get; set; }
        public string OpeningtimesGuides { get; set; }
        public string OpeningtimesGastronomie { get; set; }
        public string CcompanyHoliday { get; set; }
        public bool HasVisits { get; set; }
        public bool HasOvernights { get; set; }
        public bool HasBiowine { get; set; }

        public string Wines { get; set; }
        public bool HasAccommodation { get; set; }
        public bool IsVinumHotel { get; set; }
        public bool IsAnteprima { get; set; }
        public bool IsWineStories { get; set; }
        public bool IsWineSummit { get; set; }

        public bool issparklingwineassociation { get; set; }

        public bool iswinery { get; set; }
        public bool hasonlineshop { get; set; }
        public bool hasdeliveryservice { get; set; }
        public string onlineshopurl { get; set; }
        public string deliveryserviceurl { get; set; }
        public string h1 { get; set; }
        public string h2 { get; set; }
        public string quote { get; set; }
        public string quoteauthor { get; set; }
        public string descriptionsparklingwineproducer { get; set; }
        public string h1sparklingwineproducer { get; set; }
        public string h2sparklingwineproducer { get; set; }
        public string imagesparklingwineproducer { get; set; }
        public bool hasdirectsales { get; set; }
        public bool isskyalpspartner { get; set; }
        public bool socialsinstagram { get; set; }
        public string socialsfacebook { get; set; }
        public string socialslinkedIn { get; set; }
        public string socialspinterest { get; set; }
        public string socialstiktok { get; set; }
        public string socialsyoutube { get; set; }
        public string socialstwitter { get; set; }        
    }

    //Independent
    public class EchargingDataProperties
    {        
        //Mobility Provides
        //state (ACTIVE)
        //capacity (integer)
        //provider
        //accessInfo (FREE_PUBLICLY_ACCESSIBLE)
        //accessType (PUBLIC)
        //reservable (true/false)
        //paymentInfo
        //outlets [ id, maxPower, maxCurrent, minCurrent, outletTypeCode (Type2Mennekes, CHAdeMO, CCS, 700 bar small vehicles, )  ]

        public int? Capacity { get; set; }

        [SwaggerSchema(Description = "State of the E-chargingstation", ReadOnly = true)]
        [SwaggerEnum(
            new[]
            {
                "UNAVAILABLE",
                "ACTIVE",
                "TEMPORARYUNAVAILABLE",
                "AVAILABLE",
                "UNKNOWN",
                "FAULT",
                "PLANNED",
            }
        )]
        public string? State { get; set; }

        [SwaggerSchema(Description = "Information about Payment", ReadOnly = true)]
        public string? PaymentInfo { get; set; }

        [SwaggerSchema(Description = "Public or private access", ReadOnly = true)]
        [SwaggerEnum(new[] { "PUBLIC", "PRIVATE", "PRIVATE_WITHPUBLICACCESS" })]
        public string? AccessType { get; set; }

        [SwaggerSchema(Description = "Types of the Charging Pistols", ReadOnly = true)]
        [SwaggerEnum(
            new[]
            {
                "Typ 1-Stecker",
                "Typ 2-Stecker",
                "Combo-Stecker",
                "CHAdeMO-Stecker",
                "Tesla Supercharger",
            }
        )]
        public List<string>? ChargingPistolTypes { get; set; }

        [SwaggerSchema(Description = "AccessType Information", ReadOnly = true)]
        public string AccessTypeInfo { get; set; }

        public DateTime? SurveyDate { get; set; }
        public string? SurveyType { get; set; }

        public IDictionary<string, string> SurveyAnnotations { get; set; }

        public bool? Covered { get; set; }
        public bool? VerticalRoadSign { get; set; }
        public bool? HorizontalFloorRoadSign { get; set; }

        [SwaggerSchema(Description = "Charging Station Accessible", ReadOnly = true)]
        public bool? ChargingStationAccessible { get; set; }

        [SwaggerSchema("Maximum operation height in cm")]
        public int? DisplayOrCardReaderOperationHeight { get; set; }

        [SwaggerSchema("Maximum operation height in cm (barrierfree = 90-120 cm)")]
        public int? ChargingPistolOperationHeight { get; set; }

        public int? ChargingCableLength { get; set; }

        public bool? ShieldingPostInFrontOfStation { get; set; }

        [SwaggerSchema(
            "Stufenlose Gehsteiganbindung: zulässige maximale Steigung <5-8%) bodengleich an den Gehsteig angebunden"
        )]
        public bool? SteplessSidewalkConnection { get; set; }

        [SwaggerEnum(new[] { "Barrierefrei", "Bedingt zugänglich", "Nicht zugänglich" })]
        public string? Barrierfree { get; set; }

        //public ICollection<CarparkingArea> CarparkingArea { get; set; }

        public EchargingCarparkingArea? CarParkingSpaceNextToEachOther { get; set; }
        public EchargingCarparkingArea? CarParkingSpaceBehindEachOther { get; set; }
    }

    public class EchargingCarparkingArea
    {
        //[SwaggerEnum(new[] { "column", "row" })]
        //public string? Type { get; set; }

        [SwaggerSchema("Eben (wenn Steigung <5% und Querneigung <3%)")]
        public bool? Flat { get; set; }

        [SwaggerSchema("Steigung % (wenn Steigung >5%)")]
        public double? Gradient { get; set; }

        [SwaggerSchema("Querneigung % (wenn Querneigung >3%)")]
        public double? LateralInclination { get; set; }

        [SwaggerEnum(new[] { "Barrierefrei", "Bedingt zugänglich", "Nicht zugänglich" })]
        public string Pavement { get; set; }

        [SwaggerSchema("Width, (on column barrierfree = 350 cm), (on row barrierfree = 250 cm)")]
        public int? Width { get; set; }

        [SwaggerSchema("Length, (on column barrierfree = 500 cm), (on row barrierfree = 650 cm)")]
        public int? Length { get; set; }

        [SwaggerSchema("Barrier-free access space signage present")]
        public bool? ManeuvringSpaceSignagePresent { get; set; }

        [SwaggerSchema("Barrier-free access space to charging point(monitor / pistol)")]
        public bool? BarrierFreeAccessSpacetoChargingPoint { get; set; }
    }

    #endregion

    #region Announcements

    public class Announcement : Generic, IGPSPointsAware
    {        
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public IDictionary<string, Detail> Detail { get; set; }
        public ICollection<RelatedContent>? RelatedContent { get; set; }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(); }
        }
    }

    public class RoadIncidentProperties
    {
        //TODO Add the Properties for AdditionalProperties (direction, lanes, expected_delay, planned_incident)
    }

    #endregion        
}
