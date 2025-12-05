// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel.Annotations;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataModel
{
    #region Generic Datamodel

    public class DetailGeneric : ILanguage
    {
        public string? BaseText { get; set; }
        public string? Title { get; set; }

        public string? Language { get; set; }
    }

    public class Generic :
        IIdentifiable,
        IMetaData,
        IMappingAware,
        ILicenseInfo,
        ISource,
        IShortName,
        IHasLanguage,
        IImportDateassigneable,
        IHasAdditionalProperties
    {
        public string? Id { get; set; }
        public Metadata? _Meta { get; set; }
        public LicenseInfo? LicenseInfo { get; set; }

        public string? Shortname { get; set; }

        public DateTime? FirstImport { get; set; }
        public DateTime? LastChange { get; set; }
        public ICollection<string>? HasLanguage { get; set; }

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

        public ICollection<string>? TagIds { get; set; }
    }

    #endregion

    #region EventFlattened Datamodel
    public class EventFlattened
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
        public EventFlattened()
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

    #region VenueFlattened Datamodel

    public class VenueFlattened
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
        public VenueFlattened()
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
        public LocationInfoLinked? LocationInfo { get; set; }
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

    public class AccommodationV2 : 
        AccommodationLinked, 
        IHasTagInfo,
        IHasAdditionalProperties,
        IRelatedContentAware
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
        public new bool? IsBookable
        {
            get
            {
                return this.AccoProperties != null ? this.AccoProperties.IsBookable : null;
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

        //Related Content
        public ICollection<RelatedContent>? RelatedContent { get; set; }

        //Additional Properties
        public IDictionary<string, dynamic>? AdditionalProperties { get; set; }
    }

    public class AccommodationRoomV2 : AccommodationRoomLinked, 
        IHasTagInfo,
        IHasAdditionalProperties,
        IRelatedContentAware
    {
        //Overwrites The Features
        public new ICollection<AccoFeatureLinked>? Features { get; set; }

        //New Price From per Unit
        public Nullable<double> PriceFromPerUnit { get; set; }

        //New Accommodation Room Properties
        public AccommodationRoomProperties? Properties { get; set; }

        //Related Content
        public ICollection<RelatedContent>? RelatedContent { get; set; }

        //Additional Properties
        public IDictionary<string, dynamic>? AdditionalProperties { get; set; }

        //Tags
        //All Categorization is done via Tags
        public ICollection<Tags>? Tags { get; set; }
        public ICollection<string>? TagIds { get; set; }

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

    //NOT USED at the moment
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

    #region MeasuringPoints

    public class MeasuringpointV2
       : Generic, IIdentifiable,
            IShortName,
            IActivateable,
            ILicenseInfo,
            IImportDateassigneable,
            ISource,
            IMappingAware,
            IDistanceInfoAware,
            IPublishedOn,
            IMetaData,
            IGPSPointsAware,
            IGPSInfoAware,
            IHasLocationInfoLinked,
            IHasTagInfo,
            IHasAdditionalProperties,
            IRelatedContentAware
    {
        public MeasuringpointV2()
        {
            Detail = new Dictionary<string, DetailGeneric>();
            Mapping = new Dictionary<string, IDictionary<string, string>>();
        }

        public IDictionary<string, DetailGeneric> Detail { get; set; }
        public ICollection<string>? PublishedOn { get; set; }

        public DateTime? LastUpdate { get; set; }       
        public bool Active { get; set; }                
        public DistanceInfo? DistanceInfo { get; set; }

        //Observation
        public string? SnowHeight { get; set; }
        public string? newSnowHeight { get; set; }
        public string? Temperature { get; set; }
        public DateTime? LastSnowDate { get; set; }
        public List<WeatherObservation>? WeatherObservation { get; set; }

        //Location
        public LocationInfoLinked? LocationInfo { get; set; }        

        public List<string>? AreaIds { get; set; }
        
        
        public IEnumerable<string>? SkiAreaIds { get; set; }

        public ICollection<GpsInfo>? GpsInfo { get; set; }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(); }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get
            {
                return this.Id != null
                    ? "Weather/Measuringpoint/" + Uri.EscapeDataString(this.Id)
                    : null;
            }
        }
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<AreaLink>? Areas
        {
            get
            {
                return this.AreaIds != null
                    ? this
                        .AreaIds.Select(x => new AreaLink() { Id = x, Self = "Area/" + x })
                        .ToList()
                    : null;
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<SkiAreaLink>? SkiAreas
        {
            get
            {
                return this.SkiAreaIds != null
                    ? this
                        .SkiAreaIds.Select(x => new SkiAreaLink() { Id = x, Self = "SkiArea/" + x })
                        .ToList()
                    : null;
            }
        }

        public ICollection<Tags>? Tags { get; set; }

        //Related Content
        public ICollection<RelatedContent>? RelatedContent { get; set; }
    }

    #endregion

    #region Venues

    public class VenueV2 :
        Generic,
        IIdentifiable,
        IShortName,
        IActivateable,
        IHasLanguage,
        IImportDateassigneable,
        ILicenseInfo,
        ISource,
        IMappingAware,
        IDistanceInfoAware,
        IGPSInfoAware,
        IPublishedOn,
        IImageGalleryAware,
        IDetailInfosAware,
        IContactInfosAware,
        IMetaData,
        IHasLocationInfoLinked,
        IHasTagInfo,
        IHasDistrictId,
        IHasAdditionalProperties,
        IRelatedContentAware
    {
        public VenueV2()
        {
            //Mapping New
            Mapping = new Dictionary<string, IDictionary<string, string>>();
            Detail = new Dictionary<string, Detail>();
            ContactInfos = new Dictionary<string, ContactInfos>();
        }

        public bool Active { get; set; }

        public ICollection<GpsInfo>? GpsInfo { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(true); }
        }

        public IDictionary<string, Detail> Detail { get; set; }

        public IDictionary<string, ContactInfos> ContactInfos { get; set; }

        public ICollection<ImageGallery>? ImageGallery { get; set; }

        public ICollection<string>? PublishedOn { get; set; }
        
        public DistanceInfo? DistanceInfo { get; set; }        

        public ICollection<OperationSchedule>? OperationSchedule { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "Venue/" + Uri.EscapeDataString(this.Id) : null; }
        }
        public LocationInfoLinked? LocationInfo { get; set; }

        public ICollection<Tags>? Tags { get; set; }
        
        public ICollection<VenueRoomDetailsV2>? RoomDetails { get; set; }

        //DistrictId
        public string? DistrictId
        {
            get
            {
                return this.LocationInfo != null && this.LocationInfo.DistrictInfo != null && this.LocationInfo.DistrictInfo.Id != null ? this.LocationInfo.DistrictInfo.Id : null;
            }
        }

        //Related Content
        public ICollection<RelatedContent>? RelatedContent { get; set; }
    }

    public class VenueRoomDetailsV2 : IHasTagInfo
    {
        public VenueRoomDetailsV2()
        {
            Detail = new Dictionary<string, Detail>();
            Tags = new List<Tags>();
        }

        public string? Id { get; set; }
        public string? Shortname { get; set; }

        public ICollection<string> TagIds { get; set; }

        public ICollection<Tags> Tags { get; set; }
        
        public IDictionary<string, Detail> Detail { get; set; }

        public ICollection<ImageGallery>? ImageGallery { get; set; }

        public VenueRoomProperties VenueRoomProperties { get; set; }

        public string Placement { get; set; }
    }

    public class VenueRoomProperties
    {
        public int? SquareMeters { get; set; }
        public int? RoomWidthInMeters { get; set; }
        public int? RoomHeightInCentimeters { get; set; }
        public int? RoomDepthInMeters { get; set; }
        public int? DoorWidthInCentimeters { get; set; }
        public int? DoorHeightInCentimeters { get; set; }
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

        public IDictionary<string, string>? OpeningtimesWineshop { get; set; }
        public IDictionary<string, string>? OpeningtimesGuides { get; set; }
        public IDictionary<string, string>? OpeningtimesGastronomie { get; set; }
        public IDictionary<string, string>? CompanyHoliday { get; set; }
        public bool? HasVisits { get; set; }
        public bool? HasOvernights { get; set; }
        public bool? HasBiowine { get; set; }
        public bool? HasAccommodation { get; set; }
        public bool? IsVinumHotel { get; set; }
        public bool? IsAnteprima { get; set; }
        public bool? IsWineStories { get; set; }
        public bool? IsWineSummit { get; set; }
        public bool? IsSparklingWineassociation { get; set; }
        public bool? IsWinery { get; set; }
        public bool? HasOnlineshop { get; set; }
        public bool? HasDeliveryservice { get; set; }
        public bool? HasDirectSales { get; set; }
        public bool? IsSkyalpsPartner { get; set; }        

        public ICollection<string>? Wines { get; set; }
     
       
        public IDictionary<string, string>? OnlineShopurl { get; set; }
        public IDictionary<string, string>? DeliveryServiceUrl { get; set; }
        public IDictionary<string,string>? H1 { get; set; }
        public IDictionary<string, string>? H2 { get; set; }
        public IDictionary<string, string>? Quote { get; set; }
        public IDictionary<string, string>? QuoteAuthor { get; set; }
        public IDictionary<string, string>? DescriptionSparklingWineproducer { get; set; }
        public IDictionary<string, string>? H1SparklingWineproducer { get; set; }
        public IDictionary<string, string>? H2SparklingWineproducer { get; set; }

        public string? ImageSparklingWineproducer { get; set; }
        public string? SocialsFacebook { get; set; }
        public string? SocialsInstagram { get; set; }
        public string? SocialsLinkedIn { get; set; }
        public string? SocialsPinterest { get; set; }
        public string? SocialsTiktok { get; set; }
        public string? SocialsYoutube { get; set; }
        public string? SocialsTwitter { get; set; }        
    }

    public class SiagMuseumDataProperties
    {
        //Check if Dictionary outside is supported
        public IDictionary<string,string> OpeningTimes { get; set; }
        public IDictionary<string, string> Entry { get; set; }        
        public IDictionary<string, string> Supporter { get; set; }
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

    public class Announcement : Generic, IActivateable, IGeoAware, IRelatedContentAware
    {        
        public bool Active { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public IDictionary<string, DetailGeneric> Detail { get; set; }
        public ICollection<RelatedContent>? RelatedContent { get; set; }      

        public IDictionary<string, GpsInfo> Geo { get; set; }
    }

    public class RoadIncidentProperties
    {
        // The top-level array of roads involved in the incident.
        public List<RoadInvolved>? RoadsInvolved { get; set; }

        // The delay as a numeric value.
        public int? ExpectedDelayMinutes { get; set; }

        // The delay as a human-readable string.
        public string? ExpectedDelayString { get; set; }

        public class RoadInvolved
        {
            // The name of the road (e.g., "Highway A1").
            public string? Name { get; set; }

            // The code of the road (e.g., "A1").
            public string? Code { get; set; }

            // The array of lanes affected on this road.
            public List<LaneInfo>? Lanes { get; set; }
            
            public class LaneInfo
            {
                // The lane number (e.g., 1, 2, 3).
                public int? Lane { get; set; }

                // The specific name or description of the lane (e.g., "Left Lane").
                public string? LaneName { get; set; }

                // The direction of travel (e.g., "North", "Southbound").
                public string? Direction { get; set; }
            }
        }
    }

    #endregion        
}
