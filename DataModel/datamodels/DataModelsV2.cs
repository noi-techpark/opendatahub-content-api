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

        public ICollection<string>? TagIds { get; set; }

        public ICollection<Tags>? Tags { get; set; }
        
        public IDictionary<string, Detail> Detail { get; set; }

        public ICollection<ImageGallery>? ImageGallery { get; set; }

        public VenueRoomProperties? VenueRoomProperties { get; set; }

        public string? Placement { get; set; }
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
        // // The top-level array of roads involved in the incident.
        // public List<RoadInvolved>? RoadsInvolved { get; set; }

        // // The delay as a numeric value.
        // public int? ExpectedDelayMinutes { get; set; }

        // // The delay as a human-readable string.
        // public string? ExpectedDelayString { get; set; }

        // public class RoadInvolved
        // {
        //     // The name of the road (e.g., "Highway A1").
        //     public string? Name { get; set; }

        //     // The code of the road (e.g., "A1").
        //     public string? Code { get; set; }

        //     // The array of lanes affected on this road.
        //     public List<LaneInfo>? Lanes { get; set; }
            
        //     public class LaneInfo
        //     {
        //         // The lane number (e.g., 1, 2, 3).
        //         public int? Lane { get; set; }

        //         // The specific name or description of the lane (e.g., "Left Lane").
        //         public string? LaneName { get; set; }

        //         // The direction of travel (e.g., "North", "Southbound").
        //         public string? Direction { get; set; }
        //     }
        // }
    }

    public class UrbanGreen : Generic, IActivateable, IGeoAware, IHasAdditionalProperties
    {        
        public bool Active { get; set; }

        public DateTime? PutOnSite { get; set; }
        public DateTime? RemovedFromSite { get; set; }

        public IDictionary<string, DetailGeneric> Detail { get; set; }    

        public IDictionary<string, GpsInfo> Geo { get; set; }

        public string GreenCode { get; set; }
        public string GreenCodeType { get; set; }
        public string GreenCodeSubtype { get; set; }

        public string GreenCodeVersion { get; set; }
    }

    public class UrbanGreenProperties
    {
        public IDictionary<string, string> Taxonomy { get; set; }
    }

    #endregion

    #region Transport

    public class Trip : Generic, IGeoAware, IActivateable
    {
        public bool Active { get; set; }
        
        //Route
        public Route Route { get; set;}

        //Agency
        public class TripAgency
        {
            public string Shortname { get; set; }
            public IDictionary<string, ContactInfos> ContactInfos { get; set; }
        }

        public Trip.TripAgency Agency { get; set; }

        //Stops
        public IEnumerable<StopTime> StopTimes { get; set; }

        //Computed from StopTimes: GEOMETRYCOLLECTION of all stop geometries + connecting LINESTRING, for visualization
        public IDictionary<string, GpsInfo> Geo { get; set; }
    }

    public class Route
    {
        //route.txt

        //https://gtfs.org/documentation/schedule/reference/#routestxt
        public string Shortname { get; set; }
        public IDictionary<string, DetailGeneric> Detail { get; set; } //route_long_name route_desc

        public ICollection<string>? TagIds { get; set; } //route_type

        //calendar.txt
        public class RouteCalendar
        {
            //calendar.txt

            public OperationSchedule OperationSchedule { get; set; }

            //calendar_dates.txt

            public IEnumerable<DateTime>? AdditionalDates { get; set; }
            public IEnumerable<DateTime>? ExcludedDates { get; set; }
        }

        public Route.RouteCalendar Calendar { get; set; }
    }
    public class StopTime
    {
        //stops.txt
        public string Shortname { get; set; } //stop_code

        public IDictionary<string, DetailGeneric> Detail { get; set; } //route_long_name route_desc

        public IDictionary<string, GpsInfo> Geo { get; set; }

        //public ICollection<string>? TagIds { get; set; } //location_type 

        //stoptimes.txt
        public DateTime ArrivalTime { get; set; }
        public DateTime DepartureTime { get; set; }
    }

    
    #endregion    

    #region SpatialData

    public class SpatialData : Generic, IActivateable, IGeoAware
    {
        public bool Active { get; set; }        
        public IDictionary<string, DetailGeneric> Detail { get; set; }        
        public IDictionary<string, GpsInfo> Geo { get; set; }
    }

    #endregion
}
