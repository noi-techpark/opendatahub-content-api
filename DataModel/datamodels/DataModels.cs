// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel.Annotations;
using DataModel.validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Geometries.Utilities;
// using System.Text.Json.Serialization; // this is not the package used to serialize
using Newtonsoft.Json;

namespace DataModel
{
    #region District Municipality Region

    public class Region : BaseInfos, IImageGalleryAware, IPublishedOn, IRelatedContentAware
    {
        public Region()
        {
            DetailThemed = new Dictionary<string, DetailThemed>();
        }

        public IDictionary<string, DetailThemed> DetailThemed { get; set; }

        public ICollection<GpsPolygon>? GpsPolygon { get; set; }

        //Logic shifted to RelatedContent
        //public ICollection<Webcam>? Webcam { get; set; }

        public bool VisibleInSearch { get; set; }

        public ICollection<string>? SkiareaIds { get; set; }

        public ICollection<RelatedContent>? RelatedContent { get; set; }
    }

    public class MetaRegion : BaseInfos, IImageGalleryAware, IRelatedContentAware
    {
        public MetaRegion()
        {
            DetailThemed = new Dictionary<string, DetailThemed>();
        }

        public IDictionary<string, DetailThemed> DetailThemed { get; set; }
        public ICollection<string>? DistrictIds { get; set; }
        public ICollection<string>? TourismvereinIds { get; set; }
        public ICollection<string>? RegionIds { get; set; }
        public ICollection<GpsPolygon>? GpsPolygon { get; set; }

        //Logic shifted to RelatedContent
        //public ICollection<Webcam>? Webcam { get; set; }
        public bool VisibleInSearch { get; set; }

        public ICollection<RelatedContent>? RelatedContent { get; set; }
    }

    public class ExperienceArea : BaseInfos, IImageGalleryAware, IRelatedContentAware
    {
        public ICollection<string>? DistrictIds { get; set; }
        public ICollection<string>? TourismvereinIds { get; set; }
        public ICollection<GpsPolygon>? GpsPolygon { get; set; }
        public bool VisibleInSearch { get; set; }
        public ICollection<RelatedContent>? RelatedContent { get; set; }
    }

    public class Tourismverein : BaseInfos, IImageGalleryAware, IRelatedContentAware
    {
        public string? RegionId { get; set; }

        public ICollection<GpsPolygon>? GpsPolygon { get; set; }

        //Logic shifted to RelatedContent
        //public ICollection<Webcam>? Webcam { get; set; }
        public bool VisibleInSearch { get; set; }
        public ICollection<string>? SkiareaIds { get; set; }

        public ICollection<RelatedContent>? RelatedContent { get; set; }
    }

    public class Municipality : BaseInfos, IImageGalleryAware, IRelatedContentAware
    {
        public string? Plz { get; set; }

        public string? RegionId { get; set; }
        public string? TourismvereinId { get; set; }
        public string? SiagId { get; set; }

        public ICollection<GpsPolygon>? GpsPolygon { get; set; }

        //Logic shifted to RelatedContent
        //public ICollection<Webcam>? Webcam { get; set; }
        public bool VisibleInSearch { get; set; }

        public int Inhabitants { get; set; }
        public string? IstatNumber { get; set; }

        public ICollection<RelatedContent>? RelatedContent { get; set; }
    }

    public class District : BaseInfos, IImageGalleryAware, IRelatedContentAware
    {
        public Nullable<bool> IsComune { get; set; }

        [SwaggerReference(typeof(MunicipalityLinked), "TESTRegionID")]
        public string? RegionId { get; set; }
        public string? TourismvereinId { get; set; }
        public string? MunicipalityId { get; set; }
        public string? SiagId { get; set; }
        public ICollection<GpsPolygon>? GpsPolygon { get; set; }

        //Logic shifted to RelatedContent
        //public ICollection<Webcam>? Webcam { get; set; }
        public bool VisibleInSearch { get; set; }
        public ICollection<RelatedContent>? RelatedContent { get; set; }
    }

    public class Area
        : IIdentifiable,
            IActivateable,
            IImportDateassigneable,
            IMappingAware,
            ISource,
            ISmgActive,
            IPublishedOn,
            IShortName
    {
        public Area()
        {
            Mapping = new Dictionary<string, IDictionary<string, string>>();
            Detail = new Dictionary<string, Detail>();
        }

        public LicenseInfo? LicenseInfo { get; set; }
        public string? Id { get; set; }
        public bool Active { get; set; }

        //[SwaggerDeprecatedV2Attribute("Obsolete, use PublishedOn", "2024-01-01", "")]
        [SwaggerDeprecated("Obsolete, use PublishedOn")]
        public bool SmgActive { get; set; }
        public string? Shortname { get; set; }
        public string? CustomId { get; set; }
        public string? RegionId { get; set; }
        public string? TourismvereinId { get; set; }
        public string? MunicipalityId { get; set; }
        public string? SkiAreaID { get; set; }
        public string? GID { get; set; }
        public string? LtsID { get; set; }
        public string? AreaType { get; set; }
        public DateTime? LastChange { get; set; }
        public DateTime? FirstImport { get; set; }
        public IDictionary<string, IDictionary<string, string>> Mapping { get; set; }
        public string Source { get; set; }
        public IDictionary<string, Detail> Detail { get; set; }

        public ICollection<string>? PublishedOn { get; set; }
    }

    public class SkiArea : BaseInfos, IImageGalleryAware, IContactInfosAware, IRelatedContentAware
    {
        public SkiArea()
        {
            SkiRegionName = new Dictionary<string, string>();
        }

        public string? SkiRegionId { get; set; }
        public string? SkiAreaMapURL { get; set; }
        public string? TotalSlopeKm { get; set; }

        public string? SlopeKmBlue { get; set; }
        public string? SlopeKmRed { get; set; }
        public string? SlopeKmBlack { get; set; }

        public string? LiftCount { get; set; }

        public string? AreaRadius { get; set; }

        public int? AltitudeFrom { get; set; }
        public int? AltitudeTo { get; set; }

        public IDictionary<string, string> SkiRegionName { get; set; }

        [SwaggerDeprecated("Deprecated use AreaIds")]
        public HashSet<string>? AreaId { get; set; }

        [GetOnlyJsonProperty]
        public ICollection<string>? AreaIds
        {
            get { return this.AreaId; }
        }

        //Logic shifted to RelatedContent
        //public ICollection<Webcam>? Webcam { get; set; }

        public LocationInfo? LocationInfo { get; set; }

        public ICollection<OperationSchedule>? OperationSchedule { get; set; }

        public ICollection<string>? TourismvereinIds { get; set; }
        public ICollection<string>? RegionIds { get; set; }

        //New Municipality and District Ids
        public ICollection<string> MunicipalityIds { get; set; }
        public ICollection<string> DistrictIds { get; set; }

        public ICollection<GpsPolygon>? GpsPolygon { get; set; }

        public ICollection<RelatedContent>? RelatedContent { get; set; }
    }

    public class SkiAreaRaven : SkiArea
    {
        public new LocationInfoLinked? LocationInfo { get; set; }
    }

    public class SkiRegion : BaseInfos, IImageGalleryAware, IGpsPolygonAware, IRelatedContentAware
    {
        public ICollection<GpsPolygon>? GpsPolygon { get; set; }

        //Logic shifted to RelatedContent
        //public ICollection<Webcam>? Webcam { get; set; }

        public ICollection<RelatedContent>? RelatedContent { get; set; }
    }

    #endregion

    #region Activities & POIs

    public class ODHActivityPoi
        : PoiBaseInfos,
            ILicenseInfo,
            IGPSPointsAware,
            IHasAdditionalProperties,
            IRelatedContentAware
    {
        public ODHActivityPoi()
        {
            PoiProperty = new Dictionary<string, List<PoiProperty>>();
            AdditionalProperties = new Dictionary<string, dynamic>();
        }

        [SwaggerSchema("Id on the primary data Source")]
        [SwaggerDeprecated("Obsolete use Mapping")]
        public string? CustomId { get; set; }

        //[SwaggerDeprecated("Use Related Content")]
        //Logic shifted to RelatedContent
        //public ICollection<Webcam>? Webcam { get; set; }

        public IDictionary<string, List<PoiProperty>> PoiProperty { get; set; }

        [SwaggerDeprecated("Obsolete")]
        public ICollection<string>? PoiServices { get; set; }

        [SwaggerDeprecated("Obsolete")]
        public string? SyncSourceInterface { get; set; }
        
        [SwaggerDeprecated("Obsolete")]
        public string? SyncUpdateMode { get; set; }

        public int? AgeFrom { get; set; }
        public int? AgeTo { get; set; }

        //Gastronomy Infos
        [SwaggerDeprecated("Deprecated, Use Mapping instead")]
        public int? MaxSeatingCapacity { get; set; }

        [SwaggerDeprecated("Deprecated, Use Tags instead")]
        public ICollection<CategoryCodes>? CategoryCodes { get; set; }
        
        [SwaggerDeprecated("Deprecated, Use Tags instead")]
        public ICollection<DishRates>? DishRates { get; set; }
        
        [SwaggerDeprecated("Deprecated, Use Tags instead")]
        public ICollection<CapacityCeremony>? CapacityCeremony { get; set; }
        
        [SwaggerDeprecated("Deprecated, Use Tags instead")]
        public ICollection<Facilities>? Facilities { get; set; }

        //End Gastronomy Infos

        public ICollection<RelatedContent>? RelatedContent { get; set; }

        public IDictionary<string, List<AdditionalContact>>? AdditionalContact { get; set; }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(); }
        }

        //We define what classes this Additionalproperties can be
        [PolymorphicDictionary(
            "EchargingDataProperties", typeof(EchargingDataProperties),
            "ActivityLtsDataProperties", typeof(ActivityLtsDataProperties),
            "PoiLtsDataProperties", typeof(PoiLtsDataProperties),
            "GastronomyLtsDataProperties", typeof(GastronomyLtsDataProperties),
            "PoiAgeDataProperties", typeof(PoiAgeDataProperties),
            "SuedtirolWeinCompanyDataProperties", typeof(SuedtirolWeinCompanyDataProperties)
        )]
        public IDictionary<string, dynamic>? AdditionalProperties { get; set; }        
    }
    
    public class PoiProperty
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
    }

    public class AdditionalContact
    {
        public string Type { get; set; }
        public ContactInfos ContactInfos { get; set; }
        public string Description { get; set; }
    }

    public class RelatedContent
    {
        public string? Id { get; set; }

        //[SwaggerDeprecated("Use the name of the referenced data")]
        //public string? Name { get; set; }

        [SwaggerEnum(
            new[]
            {
                "accommodation",
                "event",
                "odhactivitypoi",
                "measuringpoint",
                "webcam",
                "article",
                "venue",
                "wineaward",
                "skiarea",
                "skiregion",
            }
        )]
        public string? Type { get; set; }

        //public string? Link
        //public string? Link
        //{
        //    get
        //    {
        //        return DataModelHelpers.TranslateTypeString2EndPoint(this.OdhType.ToLower()) + "/" + this.Id;
        //    }
        //}

        public string Self
        {
            get
            {
                return DataModelHelpers.TranslateTypeString2EndPointForRelatedContent(
                        this.Type.ToLower()
                    )
                    + "/"
                    + this.Id;
            }
        }
    }

    public class RatingSources
    {
        public string? Source { get; set; }
        public string? Objectid { get; set; }
    }


    #endregion

    #region Accommodations

    public class Accommodation
        : IIdentifiable,
            IShortName,
            IActivateable,            
            IImageGalleryAware,
            ISmgActive,
            IHasLanguage,
            IImportDateassigneable,
            ILicenseInfo,
            ISource,
            IMappingAware,
            IDistanceInfoAware,
            IPublishedOn,
            IDistrictId
    {
        public LicenseInfo? LicenseInfo { get; set; }

        public Accommodation()
        {
            AccoDetail = new Dictionary<string, AccoDetail>();
            MssResponseShort = new List<MssResponseShort>();
            Mapping = new Dictionary<string, IDictionary<string, string>>();
        }

        public string? Id { get; set; }

        [SwaggerSchema("Data is marked as Active by the data provider")]
        public bool Active { get; set; }

        [SwaggerDeprecated("Accommodation ID on the HGV System, use Mapping instead")]
        public string? HgvId { get; set; }

        [SwaggerDeprecated("Use AccoDetail.lang.Name")]
        public string? Shortname { get; set; }

        //public int Units { get; set; }
        //public int Beds { get; set; }
        public int? Representation { get; set; }
        public bool? HasApartment { get; set; }
        public bool? HasRoom { get; set; }
        public bool? IsCamping { get; set; }

        public bool? IsGastronomy { get; set; }
        public bool? IsBookable { get; set; }
        public bool? IsAccommodation { get; set; }

        [SwaggerDeprecated("Obsolete, use PublishedOn")]
        public bool SmgActive { get; set; }
        public bool? TVMember { get; set; }
        public string? TourismVereinId { get; set; }

        //not used todo remove
        public string? MainLanguage { get; set; }
        public DateTime? FirstImport { get; set; }
        public DateTime? LastChange { get; set; }
        public string? Gpstype { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Altitude { get; set; }
        public string? AltitudeUnitofMeasure { get; set; }

        public DistanceInfo? DistanceInfo { get; set; }

        public string? AccoCategoryId { get; set; }
        public string? AccoTypeId { get; set; }
        public string? DistrictId { get; set; }

        public ICollection<string>? BoardIds { get; set; }
        public ICollection<string>? MarketingGroupIds { get; set; }
        public ICollection<AccoFeature>? Features { get; set; }

        public ICollection<string>? BadgeIds { get; set; }
        public ICollection<string>? ThemeIds { get; set; }
        public ICollection<string>? SpecialFeaturesIds { get; set; }

        public IDictionary<string, AccoDetail>? AccoDetail { get; set; }
        public ICollection<AccoBookingChannel>? AccoBookingChannel { get; set; }
        public ICollection<ImageGallery>? ImageGallery { get; set; }

        public LocationInfo? LocationInfo { get; set; }

        //not used to remove
        public string? GastronomyId { get; set; }
        public ICollection<string>? SmgTags { get; set; }

        public ICollection<string>? HasLanguage { get; set; }

        public ICollection<MssResponseShort>? MssResponseShort { get; set; }

        [SwaggerSchema(
            "Independent Data <a href='https://www.independent.it/it/cooperativa-independent' target='_blank'>cooperative Independent</a>"
        )]
        public IndependentData? IndependentData { get; set; }

        public ICollection<AccoRoomInfo>? AccoRoomInfo { get; set; }

        public ICollection<string>? PublishedOn { get; set; }

        public string Source { get; set; }

        public IDictionary<string, IDictionary<string, string>> Mapping { get; set; }

        public AccoHGVInfo? AccoHGVInfo { get; set; }

        public AccoLTSInfo? AccoLTSInfo { get; set; }

        public AccoOverview? AccoOverview { get; set; }

        [SwaggerSchema("Accommodation Id on Trust You")]
        public string? TrustYouID { get; set; }

        [SwaggerSchema("Review Score on Trust You")]
        public double? TrustYouScore { get; set; }

        [SwaggerSchema("Number of Ratings in Trust You")]
        public int? TrustYouResults { get; set; }

        [SwaggerSchema("Active in Trust You")]
        public bool? TrustYouActive { get; set; }

        [SwaggerSchema("Trust You State on LTS")]
        public int? TrustYouState { get; set; }
    }

    public class AccommodationRaven : Accommodation
    {
        public new ICollection<AccoFeatureLinked>? Features { get; set; }

        //Overwrites The Features
        public new ICollection<AccoRoomInfoLinked>? AccoRoomInfo { get; set; }

        //Overwrites The LocationInfo
        public new LocationInfoLinked? LocationInfo { get; set; }
    }

    public class AccoRoomInfo
    {
        public string Id { get; set; }

        public string Source { get; set; }
    }

    public class AccoDetail : ILanguage
    {
        public string? Language { get; set; }
        public string? Name { get; set; }
        public string? NameAddition { get; set; }
        public string? Street { get; set; }
        public string? Zip { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Fax { get; set; }
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? City { get; set; }
        public string? Shortdesc { get; set; }
        public string? Longdesc { get; set; }
        public string? Vat { get; set; }

        public string? CountryCode { get; set; }
    }

    public class AccoFeature
    {
        public string? Id { get; set; }

        [SwaggerSchema(
            "Use the Id/Selflink to retrieve correct names from the appropriate Endpoint"
        )]
        public string? Name { get; set; }

        public string? HgvId { get; set; }
        public string? OtaCodes { get; set; }

        public List<int>? RoomAmenityCodes { get; set; }
    }

    public class AccoBookingChannel
    {
        public string? Id { get; set; }
        public string? Pos1ID { get; set; }
        public string? Portalname { get; set; }
        public string? BookingId { get; set; }
    }

    public class AccoRoom
        : IIdentifiable,
            IShortName,
            IImageGalleryAware,
            IHasLanguage,
            IImportDateassigneable,
            ILicenseInfo,
            ISource,
            IMappingAware,
            IPublishedOn,
            IActivateable
    {
        public LicenseInfo? LicenseInfo { get; set; }

        public AccoRoom()
        {
            AccoRoomDetail = new Dictionary<string, AccoRoomDetail>();
            Mapping = new Dictionary<string, IDictionary<string, string>>();
        }

        public string Id { get; set; }
        public string? Shortname { get; set; }

        public string? A0RID { get; set; }

        public string? Roomtype { get; set; }

        public ICollection<AccoFeature>? Features { get; set; }
        public IDictionary<string, AccoRoomDetail> AccoRoomDetail { get; set; }
        public ICollection<ImageGallery>? ImageGallery { get; set; }

        public ICollection<string>? HasLanguage { get; set; }

        public string? LTSId { get; set; }
        public string? HGVId { get; set; }
        public string Source { get; set; }
        public string? RoomCode { get; set; }
        public Nullable<int> Roommax { get; set; }
        public Nullable<int> Roommin { get; set; }
        public Nullable<int> Roomstd { get; set; }
        public Nullable<double> PriceFrom { get; set; }
        public Nullable<int> RoomQuantity { get; set; }

        public List<string>? RoomNumbers { get; set; }
        public Nullable<int> RoomClassificationCodes { get; set; }
        public Nullable<int> RoomtypeInt { get; set; }

        public Nullable<DateTime> LastChange { get; set; }
        public Nullable<DateTime> FirstImport { get; set; }

        public ICollection<string>? PublishedOn { get; set; }

        public IDictionary<string, IDictionary<string, string>> Mapping { get; set; }

        public bool Active { get; set; }
    }

    public class AccoRoomDetail : ILanguage
    {
        public string? Language { get; set; }

        public string? Name { get; set; }
        public string? Longdesc { get; set; }
        public string? Shortdesc { get; set; }
    }

    public class IndependentData
    {
        public IndependentData()
        {
            IndependentDescription = new Dictionary<string, IndependentDescription>();
        }

        public Dictionary<string, IndependentDescription> IndependentDescription { get; set; }
        public Int32? IndependentRating { get; set; }
        public bool? Enabled { get; set; }
    }

    public class IndependentDescription : ILanguage
    {
        public string? Language { get; set; }

        public string? Description { get; set; }
        public string? BacklinkUrl { get; set; }

        public string? CommitmentToAccessibilityUrl { get; set; }
    }

    public class AccoHGVInfo
    {
        public bool? Bookable { get; set; }
        public string? AvailableFrom { get; set; }
        public int? PriceFrom { get; set; }
    }

    public class AccoLTSInfo
    {
        public int? PriceFrom { get; set; }
        public int? PriceFromPerUnit { get; set; }
    }

    public class AccoOverview
    {
        public int? TotalRooms { get; set; }
        public int? SingleRooms { get; set; }
        public int? DoubleRooms { get; set; }
        public int? TripleRooms { get; set; }
        public int? QuadrupleRooms { get; set; }
        public int? Apartments { get; set; }
        public int? ApartmentBeds { get; set; }
        public int? MaxPersons { get; set; }
        public int? OutdoorParkings { get; set; }
        public int? GarageParkings { get; set; }

        public int? CampingUnits { get; set; }
        public int? CampingWashrooms { get; set; }
        public int? CampingDouches { get; set; }
        public int? CampingToilettes { get; set; }
        public int? CampingWashingstands { get; set; }
        public int? ApartmentRoomSize { get; set; }

        public TimeSpan? CheckInFrom { get; set; }
        public TimeSpan? CheckInTo { get; set; }
        public TimeSpan? CheckOutFrom { get; set; }
        public TimeSpan? CheckOutTo { get; set; }

        public TimeSpan? ReceptionOpenFrom { get; set; }
        public TimeSpan? ReceptionOpenTo { get; set; }
        public TimeSpan? RoomServiceFrom { get; set; }
        public TimeSpan? RoomServiceTo { get; set; }
        public TimeSpan? BaggageServiceFrom { get; set; }
        public TimeSpan? BaggageServiceTo { get; set; }
    }

    #endregion

    #region Gastronomy

    //public abstract class Gastronomy
    //    : IIdentifiable,
    //        IActivateable,
    //        IGpsInfo,
    //        IImageGalleryAware,
    //        IContactInfosAware,
    //        ISmgTags,
    //        ISmgActive,
    //        IImportDateassigneable,
    //        IDetailInfosAware,
    //        ISource,
    //        IMappingAware,
    //        IDistanceInfoAware,
    //        ILicenseInfo,
    //        IPublishedOn,
    //        IDistrictId
    //{
    //    public LicenseInfo? LicenseInfo { get; set; }

    //    public Gastronomy()
    //    {
    //        Detail = new Dictionary<string, Detail>();
    //        ContactInfos = new Dictionary<string, ContactInfos>();
    //        //Mapping New
    //        Mapping = new Dictionary<string, IDictionary<string, string>>();
    //    }

    //    public string? Id { get; set; }
    //    public bool Active { get; set; }
    //    public string? Shortname { get; set; }

    //    public string? Type { get; set; }

    //    //Region Fraktion
    //    public string? DistrictId { get; set; }

    //    //public string MunicipalityId { get; set; }
    //    //public string RegionId { get; set; }
    //    //public string TourismorganizationId { get; set; }

    //    public DateTime? FirstImport { get; set; }
    //    public DateTime? LastChange { get; set; }

    //    //GPS Info
    //    public string? Gpstype { get; set; }
    //    public double? Latitude { get; set; }
    //    public double? Longitude { get; set; }
    //    public Nullable<double> Altitude { get; set; }
    //    public string? AltitudeUnitofMeasure { get; set; }

    //    //OperationSchedule
    //    //public string OperationscheduleName { get; set; }
    //    //public DateTime Start { get; set; }
    //    //public DateTime Stop { get; set; }
    //    //public bool? ClosedonPublicHolidays { get; set; }
    //    //public ICollection<OperationScheduleTime> OperationScheduleTime { get; set; }
    //    //Wenn mearere sein aso
    //    public ICollection<OperationSchedule>? OperationSchedule { get; set; }

    //    //CapacityCeremony
    //    public int? MaxSeatingCapacity { get; set; }

    //    //public ICollection<GpsInfo> GpsInfo { get; set; }
    //    public ICollection<ImageGallery>? ImageGallery { get; set; }
    //    public IDictionary<string, Detail> Detail { get; set; }
    //    public IDictionary<string, ContactInfos> ContactInfos { get; set; }

    //    public ICollection<CategoryCodes>? CategoryCodes { get; set; }
    //    public ICollection<DishRates>? DishRates { get; set; }
    //    public ICollection<CapacityCeremony>? CapacityCeremony { get; set; }
    //    public ICollection<Facilities>? Facilities { get; set; }

    //    public ICollection<string>? MarketinggroupId { get; set; }

    //    //NEU Region TV Municipality Fraktion NEU LocationInfo Classe
    //    public LocationInfo? LocationInfo { get; set; }

    //    public string? AccommodationId { get; set; }

    //    public ICollection<string>? SmgTags { get; set; }

    //    [SwaggerDeprecated("Obsolete, use PublishedOn")]
    //    public bool SmgActive { get; set; }

    //    public ICollection<string>? HasLanguage { get; set; }

    //    //NEW
    //    public Nullable<int> RepresentationRestriction { get; set; }

    //    //New published on List
    //    public ICollection<string>? PublishedOn { get; set; }

    //    public string Source { get; set; }

    //    //New Mapping
    //    public IDictionary<string, IDictionary<string, string>> Mapping { get; set; }

    //    public DistanceInfo? DistanceInfo { get; set; }
    //}

    //public class GastronomyRaven : Gastronomy
    //{
    //    public new ICollection<CategoryCodesLinked>? CategoryCodes { get; set; }
    //    public new ICollection<DishRatesLinked>? DishRates { get; set; }
    //    public new ICollection<CapacityCeremonyLinked>? CapacityCeremony { get; set; }
    //    public new ICollection<FacilitiesLinked>? Facilities { get; set; }
    //    public new LocationInfoLinked? LocationInfo { get; set; }
    //}

    #endregion

    #region Events

    public class Event
        : IIdentifiable,
            IShortName,
            IActivateable,
            IImageGalleryAware,            
            IContactInfosAware,
            ISmgTags,
            ISmgActive,
            IImportDateassigneable,
            IDetailInfosAware,
            ISource,
            IMappingAware,
            IDistanceInfoAware,
            ILicenseInfo,
            IPublishedOn,
            IDistrictId
    {
        public LicenseInfo? LicenseInfo { get; set; }

        public Event()
        {
            Detail = new Dictionary<string, Detail>();
            ContactInfos = new Dictionary<string, ContactInfos>();
            OrganizerInfos = new Dictionary<string, ContactInfos>();
            EventAdditionalInfos = new Dictionary<string, EventAdditionalInfos>();
            EventPrice = new Dictionary<string, EventPrice>();
            //EventPrices = new Dictionary<string, ICollection<EventPrice>>();
            //EventVariants = new Dictionary<string, ICollection<EventVariant>>();
            //Hashtag = new Dictionary<string, ICollection<string>>();
            //EventDescAdditional = new Dictionary<string, EventDescAdditional>();
            Mapping = new Dictionary<string, IDictionary<string, string>>();
        }

        public string? Id { get; set; }
        public bool Active { get; set; }
        public string? Shortname { get; set; }

        public ICollection<string>? HasLanguage { get; set; }
        public string Source { get; set; }

        public DateTime? FirstImport { get; set; }
        public DateTime? LastChange { get; set; }

        public ICollection<ImageGallery>? ImageGallery { get; set; }
        public IDictionary<string, Detail> Detail { get; set; }
        public IDictionary<string, ContactInfos> ContactInfos { get; set; }


        public LocationInfo? LocationInfo { get; set; }
        public ICollection<string>? PublishedOn { get; set; }
        public IDictionary<string, IDictionary<string, string>>? Mapping { get; set; }
        public DistanceInfo? DistanceInfo { get; set; }


        [SwaggerSchema("First Date when the Event occurs, Detailed Eventdates in Section EventDates")]
        public DateTime? DateBegin { get; set; }
        [SwaggerSchema("Last Date when the Event occurs, Detailed Eventdates in Section EventDates")]
        public DateTime? DateEnd { get; set; }


        //GPS Info
        public string? Gpstype { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Altitude { get; set; }
        public string? AltitudeUnitofMeasure { get; set; }
        public string? DistrictId { get; set; }
        public ICollection<string>? DistrictIds { get; set; }

        [SwaggerDeprecated("Deprecated use Tags of type eventcategory")]
        public ICollection<string>? TopicRIDs { get; set; }
        [SwaggerDeprecated("Deprecated use Tags of type eventcategory")]
        public ICollection<Topic>? Topics { get; set; }

        public ICollection<EventPublisher>? EventPublisher { get; set; }
    
        public IDictionary<string, ContactInfos>? OrganizerInfos { get; set; }

    
        public ICollection<EventDate>? EventDate { get; set; }
        public IDictionary<string, EventAdditionalInfos>? EventAdditionalInfos { get; set; }        
        public ICollection<EventVariant>? EventVariants { get; set; }
        public EventProperty? EventProperty { get; set; }

        public ICollection<EventUrls>? EventUrls { get; set; }

        public EventBooking? EventBooking { get; set; }   


        //[SwaggerDeprecated("Obsolete, Dates are stored into EventDates Object Array")]
        //public DateTime? NextBeginDate { get; set; }

        [SwaggerDeprecated("Deprecated use Mapping.lts.ClassificationRID, EventProperty.EventClassificationId")]
        public string? ClassificationRID
        {
            get
            {
                return this.EventProperty != null ? this.EventProperty.EventClassificationId : "";
            }
        }
       
        [SwaggerDeprecated("Obsolete, use EventProperty.TicketRequired")]
        public string? Ticket
        {
            get
            {
                return this.EventProperty != null ? this.EventProperty.TicketRequired != null ? this.EventProperty.TicketRequired.Value ? "1" : "0" : null : null;
            }
        }
        [SwaggerDeprecated("Obsolete, use EventProperty.RegistrationRequired")]
        public string? SignOn
        {
            get
            {
                return this.EventProperty != null ? this.EventProperty.RegistrationRequired != null ? this.EventProperty.RegistrationRequired.Value ? "1" : "0" : null : null;
            }
        }
        [SwaggerDeprecated("Obsolete, use EventProperty.EventOrganizerId")]
        public string? OrgRID { 
            get {
                return this.EventProperty != null ? this.EventProperty.EventOrganizerId : "";            
            } 
        }
        
        [SwaggerDeprecated("Obsolete, use EventVariants")]
        public IDictionary<string, EventPrice>? EventPrice { get; set; } //Stays here for compatibility reasons used in Centro Trevi Sync
        

        //[SwaggerDeprecated("Obsolete, use Tags")] Temporary remove to show it in databrowser
        public ICollection<string>? SmgTags { get; set; }
        
        [SwaggerDeprecated("Obsolete, use PublishedOn")]
        public bool SmgActive { get; set; }      
    }

    public class EventRaven : Event
    {
        //Overwrites The Features
        public new ICollection<TopicLinked> Topics { get; set; }

        //Overwrites The LocationInfo
        public new LocationInfoLinked? LocationInfo { get; set; }

        //Overwrites LTSTags
        public List<LTSTagsLinked>? LTSTags { get; set; }

        public string? Type { get; set; }
        public string? Pdf { get; set; }
        public IDictionary<string, EventPrice> EventPrice { get; set; }
        public IDictionary<string, ICollection<EventPrice>> EventPrices { get; set; }
        public EventOperationScheduleOverview? EventOperationScheduleOverview { get; set; }
        public IDictionary<string, ICollection<string>> Hashtag { get; set; }
        public bool? GrpEvent { get; set; }
        public bool? EventBenefit { get; set; }
    }

    //Includes registration, meetingPoint, location
    public class EventAdditionalInfos : IEventAdditionalInfos, ILanguage
    {
        [SwaggerDeprecated("Deprecated use Meetingplace")]
        public string? Mplace { get { return this.MeetingPoint; } }        
        [SwaggerDeprecated("Deprecated use Registration")]
        public string? Reg { get { return this.Registration; } }

        public string? MeetingPoint { get; set; }
        public string? Registration { get; set; }
        public string? Location { get; set; }

        public string? ServiceDescription { get; set; }
        public string? WhatToBring { get; set; }
        public string? CancellationModality { get; set; }

        public string? Language { get; set; }
    }

    //Includes shopConfiguration, urlAlias, urls
    public class EventUrls
    {
        public string? Type { get; set; }
        public IDictionary<string,string>? Url { get; set; }
        public bool? Active { get; set; }
    }

    //Includes isTicketRequired, isRegistrationRequired, isIncludedInSuedtirolGuestPass, classification, organizer
    public class EventProperty
    {
        public bool? TicketRequired { get; set; }
        public bool? RegistrationRequired { get; set; }
        public bool? IncludedInSuedtirolGuestPass { get; set; }

        public string? EventClassificationId { get; set; }
        public string? EventOrganizerId { get; set; }

        public bool? IsBookable { get; set; }
    }

    //Includes variants
    public class EventVariant
    {
        public int? Order { get; set; }
        public double? Price { get; set; }
        public string? VariantId { get; set; }
        public string? VariantCategoryId { get; set; }
        public IDictionary<string,string>? Name { get; set; }

        public bool? IsIgnoredInAvailability { get; set; }
        public bool? IsStandard { get; set; }

        public List<string>? CombinedSalesIds { get; set; }
        public string? TaxRateId { get; set; }

    }

    //includes publisherSettings
    public class EventPublisher
    {
        public string? PublisherRID { get; set; }
        public int? Ranc { get; set; }
        [SwaggerDeprecated("Old numeric representation of PublicationStatus")]
        public int? Publish { get; set; }

        public string? PublicationStatus { get; set; }
    }

    public class EventDate : IEventDate
    {
        public string? EventDateId { get; set; }

        [SwaggerDeprecated("Deprecated use EventDateId")]
        public string? DayRID { get; set; }

        [SwaggerDeprecated("Deprecated use EventCalculatedDays")]
        public EventDateCalculatedDay? EventCalculatedDay { get
            {
                return this.EventCalculatedDays != null ? EventCalculatedDays.FirstOrDefault() : null;
            }        
        }

        public ICollection<EventDateCalculatedDay>? EventCalculatedDays { get; set; }
        
        //includes cancellationDescription, guide, description, registrationWithin
        public Dictionary<string, EventDateAdditionalInfo>? EventDateAdditionalInfo { get; set; }
        [SwaggerSchema("refers to LTS field: minAmount")]
        public double? PriceFrom { get; set; }
        //startDate, endDate
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public TimeSpan? Begin { get; set; }
        public TimeSpan? End { get; set; }
        public TimeSpan? Entrance { get; set; }
        public bool? Active { get; set; }
        [SwaggerSchema("refers to LTS field: isEachDayOwnEvent")]
        public bool? SingleDays { get; set; }
        public bool? IsCancelled { get; set; }
        public bool? IsBookable { get; set; }

        public int? MinPersons { get; set; }
        public int? MaxPersons { get; set; }

        [SwaggerSchema("refers to LTS field: ticketSale")]
        public EventDateTicketInfo? EventDateTicketInfo { get; set; }

        public ICollection<string>? EventVariantIds { get; set; }

        //public ICollection<EventDateOpeningHour>? EventDateOpeningInfo { get; set; }

        [SwaggerDeprecated("Deprecated")]
        public bool? Ticket { get; set; }
        [SwaggerDeprecated("Deprecated use isCancelled")]
        public string? Cancelled { get; set; }
    }

    public class EventDateCalculatedDay
    {
        public string? CalculatedDayId { get; set; }
        [SwaggerDeprecated("Deprecated use CalculatedDayId")]
        public string? CDayRID { get; set; }
        public DateTime? Day { get; set; }
        public TimeSpan? Begin { get; set; }

        public bool? AvailabilityLow { get; set; }
        public int? AvailabilityCalculatedValue { get; set; }
        public bool? SoldOut { get; set; }

        public ICollection<EventDateCalculatedDayVariant>? EventDateCalculatedDayVariant { get; set; }
    }

    public class EventDateCalculatedDayVariant
    {
        public string? VariantId { get; set; }
        public bool? AvailabilityLow { get; set; }
        public int? AvailabilityCalculatedValue { get; set; }
    }

    public class EventDateAdditionalInfo : ILanguage
    {
        public string? Description { get; set; }
        public string? Guide { get; set; }
        public string? RegistrationWithin { get; set; }
        public string? Language { get; set; }
        public string? CancellationDescription { get; set; }
    }

    public class EventDateTicketInfo
    {
        public bool? Active { get; set; }
        //Number of minutes before the Event before which ticket sale is allowed        
        public int? OnlineSaleUntil { get; set; }
        //Maximum number of tickets sellable online
        public int? OnlineContingent { get; set; }
    }

    public class EventDateOpeningHour
    {
        public bool? MondayOpen { get; set; }
        public bool? TuesdayOpen { get; set; }
        public bool? WednesdayOpen { get; set; }
        public bool? ThursdayOpen { get; set; }
        public bool? FridayOpen { get; set; }
        public bool? SaturdayOpen { get; set; }
        public bool? SundayOpen { get; set; }        
        public TimeSpan? Entrance1 { get; set; }
        public TimeSpan? Begin1 { get; set; }
        public TimeSpan? End1 { get; set; }
        public TimeSpan? Entrance2 { get; set; }
        public TimeSpan? Begin2 { get; set; }
        public TimeSpan? End2 { get; set; }
    }

    
    //DEPRECATED Classes used on the Raven Event Model

    public class Topic
    {
        public string? TopicRID { get; set; }
        public string? TopicInfo { get; set; }
    }

    public class EventDateAdditionalTime
    {
        public string? Days { get; set; }
        public TimeSpan? Entrance1 { get; set; }
        public TimeSpan? Begin1 { get; set; }
        public TimeSpan? End1 { get; set; }
        public TimeSpan? Entrance2 { get; set; }
        public TimeSpan? Begin2 { get; set; }
        public TimeSpan? End2 { get; set; }
    }

    public class EventBooking
    {
        public EventBooking()
        {
            BookingUrl = new Dictionary<string, EventBookingDetail>();
        }

        public DateTime? BookableFrom { get; set; }
        public DateTime? BookableTo { get; set; }
        public Dictionary<string, EventBookingDetail>? BookingUrl { get; set; }

        public bool? BookingActive { get; set; }
    }

    public class EventBookingDetail
    {
        public string? Url { get; set; }
    }

    public class EventPrice : IEventPrice, ILanguage
    {
        public double Price { get; set; }
        public string? Type { get; set; }
        public string? Pstd { get; set; }
        public string? ShortDesc { get; set; }
        public string? LongDesc { get; set; }
        public string? Description { get; set; }

        public string? Language { get; set; }
        public string? PriceRID { get; set; }
        public string? VarRID { get; set; }
    }

    public class EventOperationScheduleOverview
    {
        public bool? Monday { get; set; }
        public bool? Tuesday { get; set; }
        public bool? Wednesday { get; set; }
        public bool? Thursday { get; set; }
        public bool? Friday { get; set; }
        public bool? Saturday { get; set; }
        public bool? Sunday { get; set; }
    }

    #endregion

    #region Venues

    public class Venue
        : IIdentifiable,
            IShortName,
            IActivateable,
            ISmgTags,
            IHasLanguage,
            IImportDateassigneable,
            ILicenseInfo,
            ISource,
            IMappingAware,
            IDistanceInfoAware,
            IGPSInfoAware,
            IPublishedOn,
            IImageGalleryAware,
            ISmgActive,
            IDetailInfosAware,
            IContactInfosAware
    {
        public Venue()
        {
            //Mapping New
            Mapping = new Dictionary<string, IDictionary<string, string>>();
            Detail = new Dictionary<string, Detail>();
            ContactInfos = new Dictionary<string, ContactInfos>();
        }

        public LicenseInfo? LicenseInfo { get; set; }

        public string? Id { get; set; }
        public string? Shortname { get; set; }

        public DateTime? FirstImport { get; set; }
        public DateTime? LastChange { get; set; }

        public bool Active { get; set; }

        [SwaggerDeprecated("Obsolete, use PublishedOn")]
        public bool SmgActive { get; set; }

        public ICollection<string>? SmgTags { get; set; }

        public LocationInfo? LocationInfo { get; set; }
        public ICollection<string>? HasLanguage { get; set; }

        public ICollection<VenueType>? VenueCategory { get; set; }

        public ICollection<GpsInfo>? GpsInfo { get; set; }

        public string Source { get; set; }
        public string? SyncSourceInterface { get; set; }

        //New Details
        public int? RoomCount { get; set; }
        public ICollection<VenueRoomDetails>? RoomDetails { get; set; }

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

        //New Mapping
        public IDictionary<string, IDictionary<string, string>> Mapping { get; set; }

        public DistanceInfo? DistanceInfo { get; set; }

        public int? Beds { get; set; }

        public ICollection<OperationSchedule>? OperationSchedule { get; set; }
    }

    #endregion

    #region Articles

    //BaseInfo Article
    public abstract class Article
        : IIdentifiable,
            IShortName,
            IActivateable,
            IImageGalleryAware,
            IContactInfosAware,
            IAdditionalArticleInfosAware,
            ISmgTags,
            ISmgActive,
            IImportDateassigneable,
            ILicenseInfo,
            IDetailInfosAware,
            ISource,
            IMappingAware,
            IGPSInfoAware,
            IDistanceInfoAware,
            IPublishedOn,
            IGPSPointsAware,
            IHasLanguage,
            IVideoItemsAware
    {
        public LicenseInfo? LicenseInfo { get; set; }

        public Article()
        {
            Detail = new Dictionary<string, Detail>();
            ContactInfos = new Dictionary<string, ContactInfos>();
            AdditionalArticleInfos = new Dictionary<string, AdditionalArticleInfos>();
            ArticleLinkInfo = new Dictionary<string, ArticleLinkInfo>();
            //Mapping New
            Mapping = new Dictionary<string, IDictionary<string, string>>();
            VideoItems = new Dictionary<string, ICollection<VideoItems>>();
        }

        public string? Id { get; set; }
        public bool Active { get; set; }
        public string? Shortname { get; set; }
        public bool? Highlight { get; set; }

        //Activity SubType
        public string? Type { get; set; }

        //[SwaggerDeprecated("Obsolete, use Tags")]
        public string? SubType { get; set; }

        //für BaseArticle
        //public string SubType2 { get; set; }

        //NEU SMG Infos
        public DateTime? FirstImport { get; set; }
        public DateTime? LastChange { get; set; }

        [SwaggerDeprecated("Obsolete, use PublishedOn")]
        public bool SmgActive { get; set; }

        public DateTime? ArticleDate { get; set; }

        //Mochmer des?
        public DateTime? ArticleDateTo { get; set; }

        //OperationSchedule
        //public string OperationscheduleName { get; set; }
        //public DateTime Start { get; set; }
        //public DateTime Stop { get; set; }
        //public bool? ClosedonPublicHolidays { get; set; }
        //public ICollection<OperationScheduleTime> OperationScheduleTime { get; set; }
        //Wenn mearere sein aso:
        public ICollection<OperationSchedule>? OperationSchedule { get; set; }

        public ICollection<GpsInfo>? GpsInfo { get; set; }
        public ICollection<GpsTrack>? GpsTrack { get; set; }

        public ICollection<ImageGallery>? ImageGallery { get; set; }
        public IDictionary<string, Detail> Detail { get; set; }
        public IDictionary<string, ContactInfos> ContactInfos { get; set; }
        public IDictionary<string, AdditionalArticleInfos> AdditionalArticleInfos { get; set; }

        //NEW Link Info
        public IDictionary<string, ArticleLinkInfo> ArticleLinkInfo { get; set; }

        //[SwaggerDeprecated("Deprecated, use Tags")]
        public ICollection<string>? SmgTags { get; set; }

        public ICollection<string>? HasLanguage { get; set; }

        //New Article Expiration date
        public DateTime? ExpirationDate { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get
            {
                if (this.GpsInfo != null && this.GpsInfo.Count > 0)
                {
                    return this.GpsInfo.ToDictionary(x => x.Gpstype, x => x);
                }
                else
                {
                    return new Dictionary<string, GpsInfo> { };
                }
            }
        }

        public ICollection<string>? PublishedOn { get; set; }

        public string Source { get; set; }

        //New Mapping
        public IDictionary<string, IDictionary<string, string>> Mapping { get; set; }

        public DistanceInfo? DistanceInfo { get; set; }

        //Video Items
        public IDictionary<string, ICollection<VideoItems>>? VideoItems { get; set; }
    }

    #endregion

    #region Weather

    public class Weather
    {
        public Weather()
        {
            this.Forecast = new HashSet<Forecast>();
            this.Mountain = new HashSet<Mountain>();
            this.Conditions = new HashSet<Conditions>();
            this.Stationdata = new HashSet<Stationdata>();
        }

        [SwaggerDeprecated("Obsolete, use Date", "2024-02-28", "2024-12-31")]
        public DateTime date
        {
            get { return this.Date; }
        }

        [SwaggerDeprecated("Obsolete, use EvolutionTitle", "2024-02-28", "2024-12-31")]
        public string? evolutiontitle
        {
            get { return this.EvolutionTitle; }
        }

        [SwaggerDeprecated("Obsolete, use Evolution", "2024-02-28", "2024-12-31")]
        public string? evolution
        {
            get { return this.Evolution; }
        }

        [SwaggerDeprecated("Obsolete, use Language", "2024-02-28", "2024-12-31")]
        public string? language
        {
            get { return this.Language; }
        }

        public DateTime Date { get; set; }
        public string? EvolutionTitle { get; set; }
        public string? Evolution { get; set; }
        public string? Language { get; set; }

        public int Id { get; set; }
        public ICollection<Conditions> Conditions { get; set; }
        public ICollection<Forecast> Forecast { get; set; }
        public ICollection<Mountain> Mountain { get; set; }
        public ICollection<Stationdata> Stationdata { get; set; }
        public LicenseInfo? LicenseInfo { get; set; }
    }

    public class Conditions
    {
        [SwaggerDeprecated("Obsolete, use Date", "2024-02-28", "2024-12-31")]
        public DateTime date
        {
            get { return this.Date; }
        }

        [SwaggerDeprecated("Obsolete, use WeatherDesc", "2024-02-28", "2024-12-31")]
        public string? Weatherdesc
        {
            get { return this.WeatherDesc; }
        }

        [SwaggerDeprecated("Obsolete, use WeatherImgUrl", "2024-02-28", "2024-12-31")]
        public string? WeatherImgurl
        {
            get { return this.WeatherImgUrl; }
        }

        [SwaggerDeprecated("Obsolete, use BulletinStatus", "2024-02-28", "2024-12-31")]
        public int bulletinStatus
        {
            get { return this.BulletinStatus; }
        }

        public DateTime Date { get; set; }
        public string? Title { get; set; }
        public string? WeatherCondition { get; set; }
        public string? Temperatures { get; set; }
        public string? WeatherDesc { get; set; }
        public string? WeatherImgUrl { get; set; }
        public string? Reliability { get; set; }
        public int TempMaxmax { get; set; }
        public int TempMaxmin { get; set; }
        public int TempMinmax { get; set; }
        public int TempMinmin { get; set; }
        public int BulletinStatus { get; set; }
    }

    public class Forecast
    {
        [SwaggerDeprecated("Obsolete, use Date", "2024-02-28", "2024-12-31")]
        public DateTime date
        {
            get { return this.Date; }
        }

        [SwaggerDeprecated("Obsolete, use WeatherDesc", "2024-02-28", "2024-12-31")]
        public string? Weatherdesc
        {
            get { return this.WeatherDesc; }
        }

        [SwaggerDeprecated("Obsolete, use WeatherCode", "2024-02-28", "2024-12-31")]
        public string? Weathercode
        {
            get { return this.WeatherCode; }
        }

        [SwaggerDeprecated("Obsolete, use WeatherImgUrl", "2024-02-28", "2024-12-31")]
        public string? WeatherImgurl
        {
            get { return this.WeatherImgUrl; }
        }

        public DateTime Date { get; set; }

        public int TempMaxmax { get; set; }
        public int TempMaxmin { get; set; }
        public int TempMinmax { get; set; }
        public int TempMinmin { get; set; }

        public string? WeatherCode { get; set; }
        public string? WeatherDesc { get; set; }
        public string? WeatherImgUrl { get; set; }

        public string? Reliability { get; set; }
    }

    public class Mountain
    {
        [SwaggerDeprecated("Obsolete, use Date", "2024-02-28", "2024-12-31")]
        public DateTime date
        {
            get { return this.Date; }
        }

        [SwaggerDeprecated("Obsolete, use WeatherDesc", "2024-02-28", "2024-12-31")]
        public string? Weatherdesc
        {
            get { return this.WeatherDesc; }
        }

        [SwaggerDeprecated("Obsolete, use MountainImgUrl", "2024-02-28", "2024-12-31")]
        public string? MountainImgurl
        {
            get { return this.MountainImgUrl; }
        }

        [SwaggerDeprecated("Obsolete, use NorthCode", "2024-02-28", "2024-12-31")]
        public string? Northcode
        {
            get { return this.NorthCode; }
        }

        [SwaggerDeprecated("Obsolete, use NorthDesc", "2024-02-28", "2024-12-31")]
        public string? Northdesc
        {
            get { return this.NorthDesc; }
        }

        [SwaggerDeprecated("Obsolete, use NorthImgUrl", "2024-02-28", "2024-12-31")]
        public string? Northimgurl
        {
            get { return this.NorthImgUrl; }
        }

        [SwaggerDeprecated("Obsolete, use SouthCode", "2024-02-28", "2024-12-31")]
        public string? Southcode
        {
            get { return this.SouthCode; }
        }

        [SwaggerDeprecated("Obsolete, use SouthDesc", "2024-02-28", "2024-12-31")]
        public string? Southdesc
        {
            get { return this.SouthDesc; }
        }

        [SwaggerDeprecated("Obsolete, use SouthImgUrl", "2024-02-28", "2024-12-31")]
        public string? Southimgurl
        {
            get { return this.SouthImgUrl; }
        }

        [SwaggerDeprecated("Obsolete, use WindCode", "2024-02-28", "2024-12-31")]
        public string? Windcode
        {
            get { return this.WindCode; }
        }

        [SwaggerDeprecated("Obsolete, use WindDesc", "2024-02-28", "2024-12-31")]
        public string? Winddesc
        {
            get { return this.WindDesc; }
        }

        [SwaggerDeprecated("Obsolete, use WindImgUrl", "2024-02-28", "2024-12-31")]
        public string? WindImgurl
        {
            get { return this.WindImgUrl; }
        }

        public DateTime Date { get; set; }

        public string? Title { get; set; }
        public string? WeatherDesc { get; set; }
        public string? Conditions { get; set; }
        public string? Zerolimit { get; set; }

        public string? Reliability { get; set; }

        public string? Sunrise { get; set; }
        public string? Sunset { get; set; }
        public string? Moonrise { get; set; }
        public string? Moonset { get; set; }

        public string? MountainImgUrl { get; set; }

        public int Temp1000 { get; set; }
        public int Temp2000 { get; set; }
        public int Temp3000 { get; set; }
        public int Temp4000 { get; set; }

        public string? NorthCode { get; set; }
        public string? NorthDesc { get; set; }
        public string? NorthImgUrl { get; set; }
        public string? SouthCode { get; set; }
        public string? SouthDesc { get; set; }
        public string? SouthImgUrl { get; set; }
        public string? WindCode { get; set; }
        public string? WindDesc { get; set; }
        public string? WindImgUrl { get; set; }

        public List<string> Snowlimit { get; set; }
    }

    public class Stationdata
    {
        [SwaggerDeprecated("Obsolete, use Date", "2024-02-28", "2024-12-31")]
        public DateTime date
        {
            get { return this.Date; }
        }

        [SwaggerDeprecated("Obsolete, use MaxTemp", "2024-02-28", "2024-12-31")]
        public int Maxtemp
        {
            get { return this.MaxTemp; }
        }

        [SwaggerDeprecated("Obsolete, use WeatherCode", "2024-02-28", "2024-12-31")]
        public string? Weathercode
        {
            get { return this.WeatherCode; }
        }

        [SwaggerDeprecated("Obsolete, use WeatherDesc", "2024-02-28", "2024-12-31")]
        public string? Weatherdesc
        {
            get { return this.WeatherDesc; }
        }

        [SwaggerDeprecated("Obsolete, use WeatherImgUrl", "2024-02-28", "2024-12-31")]
        public string? WeatherImgurl
        {
            get { return this.WeatherImgUrl; }
        }

        public DateTime Date { get; set; }
        public int Id { get; set; }
        public string? CityName { get; set; }

        public string? WeatherCode { get; set; }
        public string? WeatherDesc { get; set; }
        public string? WeatherImgUrl { get; set; }

        public int MinTemp { get; set; }
        public int MaxTemp { get; set; }
    }

    public class BezirksWeather
    {
        public BezirksWeather()
        {
            this.BezirksForecast = new HashSet<BezirksForecast>();
        }

        public int Id { get; set; }

        public string? Language { get; set; }

        public string? DistrictName { get; set; }

        //public Dictionary<string, string>? District { get; set; }


        [SwaggerDeprecated("Obsolete, use Date", "2024-02-28", "2024-12-31")]
        public DateTime date
        {
            get { return this.Date; }
        }

        public DateTime Date { get; set; }

        public List<string>? TourismVereinIds { get; set; }

        public ICollection<BezirksForecast> BezirksForecast { get; set; }

        public LicenseInfo? LicenseInfo { get; set; }
    }

    public class BezirksForecast
    {
        [SwaggerDeprecated("Obsolete, use Date", "2024-02-28", "2024-12-31")]
        public DateTime date
        {
            get { return this.Date; }
        }

        public DateTime Date { get; set; }

        public string? WeatherCode { get; set; }
        public string? WeatherDesc { get; set; }

        //public Dictionary<string, string>? WeatherDescription { get; set; }

        public string? WeatherImgUrl { get; set; }

        //Compatibility
        [SwaggerDeprecated("Obsolete, use WeatherCode", "2024-02-28", "2024-12-31")]
        public string? Weathercode
        {
            get { return this.WeatherCode; }
        }

        [SwaggerDeprecated("Obsolete, use WeatherDesc", "2024-02-28", "2024-12-31")]
        public string? Weatherdesc
        {
            get { return this.WeatherDesc; }
        }

        [SwaggerDeprecated("Obsolete, use WeatherImgUrl", "2024-02-28", "2024-12-31")]
        public string? WeatherImgurl
        {
            get { return this.WeatherImgUrl; }
        }

        public int MaxTemp { get; set; }
        public int MinTemp { get; set; }

        public int Freeze { get; set; }

        public int RainFrom { get; set; }
        public int RainTo { get; set; }

        public int Part1 { get; set; }
        public int Part2 { get; set; }
        public int Part3 { get; set; }
        public int Part4 { get; set; }

        public int Thunderstorm { get; set; }

        public int SymbolId { get; set; }

        public int? Reliability { get; set; }
    }

    public class WeatherRealTime : IIdentifiable
    {
        public string Id { get; set; }

        public double altitude { get; set; }

        [SwaggerSchema(
            "Indicates whether the weather stations are: [1] in the valley, [2] gauge stations, [3] on the mountain"
        )]
        public int categoryId { get; set; }
        public string? code { get; set; }
        public string? id { get; set; }

        [SwaggerSchema("Wind direction")]
        public string? dd { get; set; }

        [SwaggerSchema("Wind speed (km/h)")]
        public string? ff { get; set; }

        [SwaggerSchema("Snow depth (cm)")]
        public string? hs { get; set; }
        public DateTime lastUpdated { get; set; }
        public double? latitude { get; set; }
        public double? longitude { get; set; }

        [SwaggerSchema(
            "Only for weather stations on the mountain: [1] if the weather station is a snow measuring field, [2] if the weather station is a wind station, [null] otherwise"
        )]
        public string? lwdType { get; set; }

        [SwaggerSchema("Precipitation from midnight (mm)")]
        public string? n { get; set; }
        public string? name { get; set; }

        [SwaggerSchema("Atmospheric pressure (hPa)")]
        public string? p { get; set; }

        [SwaggerSchema("Flow rate (m³/s)")]
        public string? q { get; set; }

        [SwaggerSchema("Relative humidity (rH)")]
        public string? rh { get; set; }

        [SwaggerSchema("Air temperature (°C)")]
        public string? t { get; set; }
        public string? vaxcode { get; set; }

        [SwaggerSchema("Water level (cm)")]
        public string? w { get; set; }

        [SwaggerSchema("Wind gust")]
        public string? wMax { get; set; }

        [SwaggerSchema("Sunshine duration (h)")]
        public string? sd { get; set; }

        [SwaggerSchema("Global radiation")]
        public string? gs { get; set; }

        [SwaggerSchema("Water temperature (°C)")]
        public string? wt { get; set; }
        public string? visibility { get; set; }
        public string? zoomLevel { get; set; }

        public ICollection<RealTimeMeasurements>? measurements { get; set; }
    }

    public class RealTimeMeasurements
    {
        public string? code { get; set; }
        public string? description { get; set; }
        public string? imageUrl { get; set; }
    }

    public class WeatherHistory : IIdentifiable, IShortName, ILicenseInfo, IImportDateassigneable
    {
        public WeatherHistory()
        {
            Weather = new Dictionary<string, Weather>();
            WeatherDistrict = new Dictionary<string, IEnumerable<BezirksWeather>>();
        }

        public IDictionary<string, Weather> Weather { get; set; }

        public IEnumerable<WeatherForecast> WeatherForecast { get; set; }

        public IDictionary<string, IEnumerable<BezirksWeather>> WeatherDistrict { get; set; }

        public LicenseInfo? LicenseInfo { get; set; }

        public List<string> HasLanguage { get; set; }
        public DateTime? FirstImport { get; set; }
        public DateTime? LastChange { get; set; }

        public string? Shortname { get; set; }
        public string Id { get; set; }
    }

    public class WeatherForecast : IIdentifiable, IImportDateassigneable, ILicenseInfo, IShortName
    {
        public DateTime Date { get; set; }

        //"absTempMin": -10,
        //"absTempMax": 20,
        //"absPrecMin": 0,
        //"absPrecMax": 10

        public string Id { get; set; }
        public string? Language { get; set; }
        public LicenseInfo? LicenseInfo { get; set; }
        public DateTime? FirstImport { get; set; }
        public DateTime? LastChange { get; set; }

        public IEnumerable<GpsInfo> GpsInfo { get; set; }

        public string MunicipalityIstatCode { get; set; }

        public string Shortname { get; set; }

        public Dictionary<string, string> MunicipalityName { get; set; }

        public ICollection<Forecast24Hours> ForeCastDaily { get; set; }

        public ICollection<Forecast3Hours> Forecast3HoursInterval { get; set; }
    }

    public class Forecast24Hours
    {
        public DateTime Date { get; set; }

        [SwaggerSchema("Minimum Temperature in °C")]
        public int? MinTemp { get; set; }

        [SwaggerSchema("Maximum Temperature in °C")]
        public int? MaxTemp { get; set; }

        [SwaggerSchema("Sunshine Duration in Hours h")]
        public int? SunshineDuration { get; set; }

        [SwaggerSchema("Maximimum Precipitation Probability in Percent %")]
        public int? PrecipitationProbability { get; set; }

        [SwaggerSchema("24 hour Precipitation sum in mm")]
        public int? Precipitation { get; set; }

        [SwaggerSchema("Weather Code")]
        public string? WeatherCode { get; set; }

        [SwaggerSchema("Weather Desciption")]
        public string? WeatherDesc { get; set; }

        [SwaggerSchema("Weather Desciption multi language")]
        public Dictionary<string, string>? WeatherDescription { get; set; }

        [SwaggerSchema("Weather ImageUrl")]
        public string? WeatherImgUrl { get; set; }
    }

    public class Forecast3Hours
    {
        public DateTime Date { get; set; }

        [SwaggerSchema("Temperature in °C")]
        public float? Temperature { get; set; }

        [SwaggerSchema("Precipitation Probability in Percent %")]
        public int? PrecipitationProbability { get; set; }

        [SwaggerSchema("Precipitation in mm")]
        public float? Precipitation { get; set; }

        [SwaggerSchema("Weather Code")]
        public string? WeatherCode { get; set; }

        [SwaggerSchema("Weather Code")]
        public string? WeatherDesc { get; set; }

        [SwaggerSchema("Weather Desciption multi language")]
        public Dictionary<string, string>? WeatherDescription { get; set; }

        [SwaggerSchema("Weather ImageUrl")]
        public string? WeatherImgUrl { get; set; }

        [SwaggerSchema("WindDirection in °")]
        public int? WindDirection { get; set; }

        [SwaggerSchema("WindSpeed in m/s")]
        public int? WindSpeed { get; set; }
    }

    #endregion

    #region Packages

    public class Package
        : IIdentifiable,
            IShortName,
            IActivateable,
            ISmgActive,
            ISmgTags,
            IImageGalleryAware,
            IHasLanguage,
            ISource
    {
        public LicenseInfo? LicenseInfo { get; set; }

        public Package()
        {
            PackageDetail = new Dictionary<string, PackageDetail>();
            Inclusive = new Dictionary<string, Inclusive>();
            ChannelInfo = new Dictionary<string, string>();
        }

        public string? Id { get; set; }

        //Infos zum Import
        public DateTime FirstImport { get; set; }
        public DateTime LastUpdate { get; set; }

        public bool Active { get; set; }

        [SwaggerDeprecated("Obsolete, use PublishedOn")]
        public bool SmgActive { get; set; }
        public string? Shortname { get; set; }

        public ICollection<string>? SmgTags { get; set; }

        public DateTime ValidStart { get; set; }
        public DateTime ValidStop { get; set; }

        public string? OfferId { get; set; }

        public List<string>? HotelHgvId { get; set; }
        public List<string>? HotelId { get; set; }

        public int Offertyp { get; set; }
        public int Specialtyp { get; set; }

        public Nullable<int> Premiumtyp { get; set; }

        public int DaysArrival { get; set; }
        public int DaysDeparture { get; set; }

        public int DaysDurMin { get; set; }
        public int DaysDurMax { get; set; }

        public int DaysArrivalMin { get; set; }
        public int DaysArrivalMax { get; set; }

        public int ChildrenMin { get; set; }

        public bool ShortStay { get; set; }
        public bool LongStay { get; set; }

        public ICollection<ImageGallery>? ImageGallery { get; set; }
        public IDictionary<string, PackageDetail> PackageDetail { get; set; }

        public IDictionary<string, string> ChannelInfo { get; set; }

        public IDictionary<string, Inclusive> Inclusive { get; set; }
        public ICollection<PackageTheme>? PackageThemeDetail { get; set; }

        public ICollection<string>? PackageThemeList { get; set; }

        public ICollection<Season>? Season { get; set; }

        public List<string>? Services { get; set; }

        public ICollection<string>? HasLanguage { get; set; }

        //Location Geschichte
        public LocationInfo? LocationInfo { get; set; }
        public string? DistrictId { get; set; }

        //public string HGVLink
        public string? HgvLink { get; set; }

        //MSS Result
        public ICollection<MssResponseShort>? MssResponseShort { get; set; }

        public EvalancheMapping? EvalancheMapping { get; set; }

        public ICollection<string>? PublishedOn { get; set; }

        public string Source { get; set; }
    }

    public class Season
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public class PackageDetail : ILanguage
    {
        public string? Language { get; set; }
        public string? Title { get; set; }
        public string? Desc { get; set; }
    }

    public class Inclusive
    {
        public Inclusive()
        {
            PackageDetail = new Dictionary<string, PackageDetail>();
        }

        public int PriceId { get; set; }
        public int PriceTyp { get; set; }

        public IDictionary<string, PackageDetail> PackageDetail { get; set; }
        public ICollection<ImageGallery>? ImageGallery { get; set; }
    }

    public class PackageTheme
    {
        public PackageTheme()
        {
            ThemeDetail = new Dictionary<string, ThemeDetail>();
        }

        public int ThemeId { get; set; }
        public IDictionary<string, ThemeDetail> ThemeDetail { get; set; }
    }

    public class ThemeDetail : ILanguage
    {
        public string? Title { get; set; }
        public string? Language { get; set; }
    }

    #endregion

    #region Measuringpoints

    public class Measuringpoint
        : IIdentifiable,
            IShortName,
            IActivateable,
            ISmgActive,            
            ILicenseInfo,
            IImportDateassigneable,
            ISource,
            IMappingAware,
            IDistanceInfoAware,
            IPublishedOn
    {
        public Measuringpoint()
        {
            Mapping = new Dictionary<string, IDictionary<string, string>>();
        }

        public LicenseInfo? LicenseInfo { get; set; }

        public string? Id { get; set; }

        public DateTime? FirstImport { get; set; }
        public DateTime LastUpdate { get; set; }
        public DateTime? LastChange { get; set; }

        public bool Active { get; set; }

        [SwaggerDeprecated("Obsolete, use PublishedOn")]
        public bool SmgActive { get; set; }
        public string? Shortname { get; set; }

        //GPS
        public string? Gpstype { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public Nullable<double> Altitude { get; set; }
        public string? AltitudeUnitofMeasure { get; set; }
        public DistanceInfo? DistanceInfo { get; set; }

        //Observation
        public string? SnowHeight { get; set; }
        public string? newSnowHeight { get; set; }
        public string? Temperature { get; set; }
        public DateTime LastSnowDate { get; set; }
        public List<WeatherObservation>? WeatherObservation { get; set; }

        //Location
        public LocationInfo? LocationInfo { get; set; }
        public string? OwnerId { get; set; }

        public List<string>? AreaIds { get; set; }

        public ICollection<string>? PublishedOn { get; set; }

        public string Source { get; set; }

        public IDictionary<string, IDictionary<string, string>> Mapping { get; set; }

        public IEnumerable<string>? SkiAreaIds { get; set; }
    }

    public class MeasuringpointRaven : Measuringpoint
    {
        public new LocationInfoLinked? LocationInfo { get; set; }
    }

    public class WeatherObservation
    {
        public WeatherObservation()
        {
            WeatherStatus = new Dictionary<string, string>();
        }

        public string? Id { get; set; }
        //public string? Level { get; set; }
        //public string? LevelId { get; set; }
        public Dictionary<string, string> WeatherStatus { get; set; }

        //New
        public string? IconID { get; set; }
        public DateTime? Date { get; set; }
    }

    //SnowReport Base Data
    public class SnowReportBaseData
    {
        public SnowReportBaseData() { }

        public string? Id { get; set; }
        public string? RID { get; set; }
        public string? Skiregion { get; set; }
        public string? Areaname { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? lang { get; set; }

        public string? SkiAreaSlopeKm { get; set; }
        public string? SkiMapUrl { get; set; }

        //Snow Data
        public ICollection<MeasuringpointReduced>? Measuringpoints { get; set; }
        public ICollection<string>? WebcamUrl { get; set; }

        //Webcam
        //public ICollection<Webcam> Webcams { get; set; }

        //public string snowheight { get; set; }
        //public string newsnowheigtht { get; set; }
        //public string lastsnow { get; set; }
        //public string weather { get; set; }
        //public string weatherimg { get; set; }
        //public string weathercode { get; set; }
        //public string observationtemperature { get; set; }

        //Slopes Lifts
        public string? totalskilift { get; set; }
        public string? openskilift { get; set; }
        public string? totalskiliftkm { get; set; }
        public string? openskiliftkm { get; set; }
        public string? totalskislopes { get; set; }
        public string? openskislopes { get; set; }
        public string? totalskislopeskm { get; set; }
        public string? openskislopeskm { get; set; }
        public string? totaltracks { get; set; }
        public string? opentracks { get; set; }
        public string? totaltrackskm { get; set; }
        public string? opentrackskm { get; set; }
        public string? totalslides { get; set; }
        public string? opentslides { get; set; }
        public string? totalslideskm { get; set; }
        public string? opentslideskm { get; set; }
        public string? totaliceskating { get; set; }
        public string? openiceskating { get; set; }

        //Contact
        public string? contactadress { get; set; }
        public string? contacttel { get; set; }
        public string? contactcap { get; set; }
        public string? contactcity { get; set; }
        public string? contactfax { get; set; }
        public string? contactweburl { get; set; }
        public string? contactmail { get; set; }
        public string? contactlogo { get; set; }

        //Gps
        public string? contactgpsnorth { get; set; }
        public string? contactgpseast { get; set; }

        //public virtual ICollection<Slopes> Slopes { get; set; }
        //public virtual ICollection<Tracks> Tracks { get; set; }
        //public virtual ICollection<Lifts> Lifts { get; set; }
        //public virtual ICollection<Slides> Slides { get; set; }
        //public virtual ICollection<Iceskating> Iceskating { get; set; }
        //public virtual ICollection<Measuringpoints> Measuringpoints { get; set; }

        //public virtual ICollection<IActivity> IActivity { get; set; }
    }

    //Reduced Measuringpoint for Snow Report
    public class MeasuringpointReduced : ISource
    {
        public string? Id { get; set; }

        public DateTime LastUpdate { get; set; }
        public string? Shortname { get; set; }
        public string? SnowHeight { get; set; }
        public string? newSnowHeight { get; set; }
        public string? Temperature { get; set; }
        public DateTime LastSnowDate { get; set; }
        public List<WeatherObservation>? WeatherObservation { get; set; }

        public string Source { get; set; }

        ////GPS
        //public string Gpstype { get; set; }
        //public double? Latitude { get; set; }
        //public double? Longitude { get; set; }
        //public Nullable<double> Altitude { get; set; }
        //public string AltitudeUnitofMeasure { get; set; }

        //Observation

        ////Location Geschichte
        //public LocationInfo LocationInfo { get; set; }
        //public string OwnerId { get; set; }

        //public List<string> AreaIds { get; set; }
    }

    #endregion

    #region EventShort

    public class EventShort
        : IIdentifiable,
            IShortName,
            IImportDateassigneable,
            ISource,
            IMappingAware,
            ILicenseInfo,
            IPublishedOn,
            IGPSPointsAware,
            IImageGalleryAware,
            IVideoItemsAware,
            IHasLanguage,
            IDetailInfosAware
    {
        public EventShort()
        {
            Mapping = new Dictionary<string, IDictionary<string, string>>();
            //EventText = new Dictionary<string, string>();
            //EventTitle = new Dictionary<string, string>();
            Documents = new Dictionary<string, List<Document>?>();
            VideoItems = new Dictionary<string, ICollection<VideoItems>>();
            Detail = new Dictionary<string, Detail>();
        }

        public LicenseInfo? LicenseInfo { get; set; }

        public string? Id { get; set; }
        public string Source { get; set; }

        public IDictionary<string,Detail> Detail { get; set; }

        [SwaggerEnum(new[] { "NOI", "EC" })]
        public string EventLocation { get; set; }
        public int? EventId { get; set; }

        [SwaggerDeprecated("Deprecated, use Detail BaseText")]
        public IDictionary<string, string> EventText
        {
            get { return this.Detail != null ? this.Detail.ToDictionary(x => x.Key, x => x.Value.BaseText) : null; }
        }
        //public IDictionary<string, string> EventText { get; set; }

        [SwaggerDeprecated("Deprecated, use Detail Title")]
        public IDictionary<string, string> EventTitle
        {
            get { return this.Detail != null ? this.Detail.ToDictionary(x => x.Key, x => x.Value.Title) : null; }
        }
        //public IDictionary<string, string> EventTitle { get; set; }


        [SwaggerDeprecated("Deprecated, use Detail BaseText")]
        public string? EventTextDE
        {
            get { return Detail != null && Detail.ContainsKey("de") ? Detail["de"].BaseText : null; }
        }

        [SwaggerDeprecated("Deprecated, use Detail BaseText")]
        public string? EventTextIT
        {
            get { return Detail != null && Detail.ContainsKey("it") ? Detail["it"].BaseText : null; }
        }

        [SwaggerDeprecated("Deprecated, use Detail BaseText")]
        public string? EventTextEN
        {
            get { return Detail != null && Detail.ContainsKey("en") ? Detail["en"].BaseText : null; }
        }

        //Hauptbeschreibung
        [SwaggerDeprecated("Deprecated, use EventTitle")]
        public string? EventDescription { get; set; }

        //Beschreibung DE
        [SwaggerDeprecated("Deprecated, use EventTitle")]
        public string? EventDescriptionDE
        {
            get { return Detail != null && Detail.ContainsKey("de") ? Detail["de"].Title : ""; }
        }

        //Beschreibung IT
        [SwaggerDeprecated("Deprecated, use EventTitle")]
        public string? EventDescriptionIT
        {
            get { return Detail != null && Detail.ContainsKey("it") ? Detail["it"].Title : ""; }
        }

        //Beschreibung EN
        [SwaggerDeprecated("Deprecated, use EventTitle")]
        public string? EventDescriptionEN
        {
            get { return Detail != null && Detail.ContainsKey("en") ? Detail["en"].Title : ""; }
        }

        //Hauptsaal/ort
        public string? AnchorVenue { get; set; }

        //Hauptsaal/ort soll für die Ausgabe verwendet werden
        public string? AnchorVenueShort { get; set; }

        //letzte Änderung
        public DateTime ChangedOn { get; set; }

        //Beginndatum
        public DateTime StartDate { get; set; }

        //Beginnzeit
        //public string StartTime { get; set; }
        ////Ende Datum
        public DateTime EndDate { get; set; }

        //Endzeit
        //public string EndTime { get; set; }

        public double StartDateUTC { get; set; }
        public double EndDateUTC { get; set; }

        //URL für externe Webseite (noch nicht ausgefüllt)
        [UrlPrefixAttribute]
        public string? WebAddress { get; set; }

        //Spezialfelder

        [SwaggerDeprecated("Deprecated")]
        [RegularExpression("Y|N", ErrorMessage = "Only Y and N allowed")]
        [SwaggerEnum(new[] { "Y", "N" })]
        [SwaggerSchema("Active Eurac Videowall")]
        public string? Display1 { get; set; }

        [SwaggerDeprecated("Deprecated")]
        [SwaggerEnum(new[] { "Y", "N" })]
        [SwaggerSchema("Active Eurac Seminarroom")]
        public string? Display2 { get; set; }

        [SwaggerDeprecated("Deprecated")]
        [SwaggerEnum(new[] { "Y", "N" })]
        [SwaggerSchema("Active Today.noi.bz (Totem)")]        
        public string? Display3 { get; set; }

        [SwaggerDeprecated("Deprecated")]
        [SwaggerEnum(new[] { "Y", "N" })]
        [SwaggerSchema("Active Today.noi.bz (Videowall)")]
        public string? Display4 { get; set; }

        [SwaggerDeprecated("Deprecated")]
        [SwaggerEnum(new[] { "Y", "N" })]
        public string? Display5 { get; set; }

        [SwaggerDeprecated("Deprecated")]
        [SwaggerEnum(new[] { "Y", "N" })]
        public string? Display6 { get; set; }

        [SwaggerDeprecated("Deprecated")]
        [SwaggerEnum(new[] { "Y", "N" })]
        public string? Display7 { get; set; }

        [SwaggerDeprecated("Deprecated")]
        [SwaggerEnum(new[] { "Y", "N" })]
        public string? Display8 { get; set; }

        [SwaggerDeprecated("Deprecated")]
        [SwaggerEnum(new[] { "Y", "N" })]
        public string? Display9 { get; set; }

        public string? CompanyName { get; set; }
        public string? CompanyId { get; set; }
        public string? CompanyAddressLine1 { get; set; }
        public string? CompanyAddressLine2 { get; set; }
        public string? CompanyAddressLine3 { get; set; }
        public string? CompanyPostalCode { get; set; }
        public string? CompanyCity { get; set; }
        public string? CompanyCountry { get; set; }
        public string? CompanyPhone { get; set; }
        public string? CompanyFax { get; set; }
        public string? CompanyMail { get; set; }
        public string? CompanyUrl { get; set; }

        //Person aus Modul CRM (interessiert uns nicht)
        public string? ContactCode { get; set; }
        public string? ContactFirstName { get; set; }
        public string? ContactLastName { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactCell { get; set; }
        public string? ContactFax { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactAddressLine1 { get; set; }
        public string? ContactAddressLine2 { get; set; }
        public string? ContactAddressLine3 { get; set; }
        public string? ContactPostalCode { get; set; }
        public string? ContactCity { get; set; }
        public string? ContactCountry { get; set; }

        //gebuchten Sääle von spezifischen Event
        //Space : Code für Raum von DB
        //SpaceDesc: Beschreibung --> zu nehmen
        //SpaceAbbrev: Abgekürzte Beschreibung
        //SoaceType : EC = Eurac, NO = Noi
        //Comnment: entweder x oder leer --> x bedeutet bitte nicht anzeigen!!!!!!!
        //Subtitle: Untertitel vom Saal (anzeigen)
        //Zeiten (diese sind relevant, diese anzeigen)
        public List<RoomBooked>? RoomBooked { get; set; }

        public ICollection<ImageGallery>? ImageGallery { get; set; }
        public string? VideoUrl { get; set; }

        //[SwaggerDeprecated("Deprecated, use GpsInfo")]
        public List<string>? TechnologyFields { get; set; }

        //[SwaggerDeprecated("Deprecated, use GpsInfo")]
        public List<string>? CustomTagging { get; set; }

        [SwaggerDeprecated("Deprecated, use Documents")]
        public List<DocumentPDF>? EventDocument
        {
            get
            {
                if (this.Documents != null && this.Documents.Count > 0)
                {
                    return this
                        .Documents.SelectMany(x => x.Value)
                        .Select(y => new DocumentPDF()
                        {
                            DocumentURL = y.DocumentURL,
                            Language = y.Language,
                        })
                        .ToList();
                }
                else
                    return null;
            }
        }

        public IDictionary<string, List<Document>?> Documents { get; set; }

        public bool? ExternalOrganizer { get; set; }

        public string? Shortname { get; set; }

        public ICollection<string>? PublishedOn { get; set; }

        public string? AnchorVenueRoomMapping
        {
            get
            {
                return !String.IsNullOrEmpty(this.AnchorVenue)
                    ? (
                        this.AnchorVenue.StartsWith("NOI ")
                        || this.AnchorVenue.StartsWith("Noi ")
                        || this.AnchorVenue.StartsWith("noi ")
                    )
                        ? this.AnchorVenue.Remove(0, 3).Trim()
                        : this.AnchorVenue
                    : this.AnchorVenue;
            }
        }

        public DateTime? FirstImport { get; set; }
        public DateTime? LastChange { get; set; }

        public bool? SoldOut { get; set; }

        [SwaggerDeprecated("Deprecated, use PublishedOn: today.noi.bz.it")]
        [SwaggerSchema(" ActiveToday Indicates if Event is shown on the today NOI Website")]
        public bool? ActiveToday
        {
            get
            {
                if (this.PublishedOn != null && this.PublishedOn.Count > 0)
                {
                    return this.PublishedOn.Contains("today.noi.bz.it");
                }
                else
                {
                    return false;
                }
            }
        }

        [SwaggerDeprecated("Deprecated, use PublishedOn: noi.bz.it")]
        [SwaggerSchema(
            " ActiveWeb Indicates if Event is shown on the Noi Website Section Events at NOI"
        )]
        public bool? ActiveWeb
        {
            get
            {
                if (this.PublishedOn != null && this.PublishedOn.Count > 0)
                {
                    return this.PublishedOn.Contains("noi.bz.it");
                }
                else
                {
                    return false;
                }
            }
        }

        [SwaggerDeprecated("Deprecated, use PublishedOn: noi-communityapp")]
        [SwaggerSchema("ActiveCommunityApp Indicates if Event is shown on the Noi Community App")]
        public bool? ActiveCommunityApp
        {
            get
            {
                if (this.PublishedOn != null && this.PublishedOn.Count > 0)
                {
                    return this.PublishedOn.Contains("noi-communityapp");
                }
                else
                {
                    return false;
                }
            }
        }

        public ICollection<string>? HasLanguage { get; set; }

        public IDictionary<string, IDictionary<string, string>> Mapping { get; set; }

        public ICollection<GpsInfo>? GpsInfo { get; set; }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get
            {
                if (this.GpsInfo != null && this.GpsInfo.Count > 0)
                {
                    return this.GpsInfo.ToDictionary(x => x.Gpstype, x => x);
                }
                else
                {
                    return new Dictionary<string, GpsInfo> { };
                }
            }
        }

        public AgeRange? TypicalAgeRange { get; set; }

        //Use Active for filtering out not active events
        public bool? Active { get; set; }

        //Video Items
        public IDictionary<string, ICollection<VideoItems>>? VideoItems { get; set; }
    }

    public class RoomBooked
    {
        public string? Space { get; set; }
        public string? SpaceDesc { get; set; }
        public string? SpaceAbbrev { get; set; }
        public string? SpaceType { get; set; }
        public string? Subtitle { get; set; }
        public string? Comment { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public double StartDateUTC { get; set; }
        public double EndDateUTC { get; set; }

        public string? SpaceDescRoomMapping
        {
            get
            {
                return !String.IsNullOrEmpty(this.SpaceDesc)
                    ? (
                        this.SpaceDesc.StartsWith("NOI ")
                        || this.SpaceDesc.StartsWith("Noi ")
                        || this.SpaceDesc.StartsWith("noi ")
                    )
                        ? this.SpaceDesc.Remove(0, 3).Trim()
                        : this.SpaceDesc
                    : this.SpaceDesc;
            }
        }
    }

    public class EventShortByRoom
    {
        public EventShortByRoom()
        {
            SpaceDescList = new List<string>();
            TechnologyFields = new List<string>();
            CustomTagging = new List<string>();
            EventTitle = new Dictionary<string, string>();
            EventText = new Dictionary<string, string>();
            EventDescription = new Dictionary<string, string>();
            EventDocument = new Dictionary<string, string>();
            Detail = new Dictionary<string, Detail>();
        }

        //Room Infos

        public List<string> SpaceDescList { get; set; }

        public string? SpaceDesc { get; set; }
        public string? SpaceType { get; set; }
        public string? Subtitle { get; set; }

        public DateTime RoomStartDate { get; set; }
        public DateTime RoomEndDate { get; set; }

        public double RoomStartDateUTC { get; set; }
        public double RoomEndDateUTC { get; set; }

        //Event Infos

        public int? EventId { get; set; }

        public IDictionary<string, Detail> Detail { get; set; }

        [SwaggerDeprecated("Deprecated, use Detail.Title")]
        public Dictionary<string, string> EventTitle { get; set; }

        [SwaggerDeprecated("Deprecated, use Detail.BaseText")]
        public Dictionary<string, string> EventText { get; set; }

        [SwaggerDeprecated("Deprecated, use Detail.Title")]
        public Dictionary<string, string> EventDescription { get; set; }

        [SwaggerDeprecated("Deprecated, use Detail.Title")]
        public string? EventDescriptionDE { get; set; }

        [SwaggerDeprecated("Deprecated, use Detail.Title")]
        public string? EventDescriptionIT { get; set; }

        [SwaggerDeprecated("Deprecated, use Detail.Title")]
        public string? EventDescriptionEN { get; set; }

        public string? EventAnchorVenue { get; set; }
        public string? EventAnchorVenueShort { get; set; }

        public DateTime EventStartDate { get; set; }
        public DateTime EventEndDate { get; set; }
        public double EventStartDateUTC { get; set; }
        public double EventEndDateUTC { get; set; }

        public string? EventWebAddress { get; set; }
        public string? Id { get; set; }
        public string? EventSource { get; set; }
        public string? EventLocation { get; set; }

        public string? CompanyName { get; set; }
        public ICollection<ImageGallery>? ImageGallery { get; set; }
        public string? VideoUrl { get; set; }
        public Nullable<bool> ActiveWeb { get; set; }

        [SwaggerDeprecated("Deprecated, use EventText")]
        public string? EventTextDE { get; set; }

        [SwaggerDeprecated("Deprecated, use EventText")]
        public string? EventTextIT { get; set; }

        [SwaggerDeprecated("Deprecated, use EventText")]
        public string? EventTextEN { get; set; }

        public List<string>? TechnologyFields { get; set; }
        public List<string>? CustomTagging { get; set; }
        public bool? SoldOut { get; set; }

        public Dictionary<string, string> EventDocument { get; set; }

        public bool? ExternalOrganizer { get; set; }

        public ICollection<string>? PublishedOn { get; set; }
    }

    public class DocumentPDF
    {
        public string? DocumentURL { get; set; }
        public string? Language { get; set; }
    }

    public class Document
    {
        public string? DocumentName { get; set; }
        public string? DocumentURL { get; set; }
        public string? Language { get; set; }
    }

    public class AgeRange
    {
        public int AgeFrom { get; set; }
        public int AgeTo { get; set; }
    }

    #endregion

    #region Webcam

    public class Webcam : IWebcam
    {
        public Webcam()
        {
            Webcamname = new Dictionary<string, string>();
        }

        public string? WebcamId { get; set; }

        public IDictionary<string, string> Webcamname { get; set; }

        public string? Webcamurl { get; set; }

        public GpsInfo? GpsInfo { get; set; }

        [Obsolete("Use Publishedon")]
        public int? ListPosition { get; set; }
        public string? Streamurl { get; set; }
        public string? Previewurl { get; set; }

        public string Source { get; set; }

        ////NEW Webcam Properties
        //public string Streamurl { get; set; }
        //public string Previewurl { get; set; }
        //public DateTime? LastChange { get; set; }
        //public DateTime? FirstImport { get; set; }

        //public bool? Active { get; set; }

        //public string Source { get; set; }
    }

    public class WebcamInfoRaven
        : Webcam,
            IIdentifiable,
            IShortName,
            IImportDateassigneable,
            ISource,
            ILicenseInfo,
            IMappingAware,
            IPublishedOn,
            IGPSPointsAware,
            IActivateable,
            ISmgActive
    {
        public WebcamInfoRaven()
        {
            Mapping = new Dictionary<string, IDictionary<string, string>>();
        }

        public LicenseInfo? LicenseInfo { get; set; }

        //NEW Webcam Properties
        public string? Id { get; set; }

        //public new string? Streamurl { get; set; }

        //public new string? Previewurl { get; set; }
        public DateTime? LastChange { get; set; }
        public DateTime? FirstImport { get; set; }
        public string? Shortname { get; set; }
        public bool Active { get; set; }

        [SwaggerDeprecated("Obsolete, use PublishedOn")]
        public bool SmgActive { get; set; }
        public ICollection<PublishedonObject>? WebcamAssignedOn { get; set; }

        public ICollection<string>? AreaIds { get; set; }

        public ICollection<string>? SmgTags { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get
            {
                if (this.GpsInfo != null)
                {
                    return new Dictionary<string, GpsInfo>
                    {
                        { this.GpsInfo.Gpstype, this.GpsInfo },
                    };
                }
                else
                {
                    return new Dictionary<string, GpsInfo> { };
                }
            }
        }

        public ICollection<string>? PublishedOn { get; set; }

        public IDictionary<string, IDictionary<string, string>> Mapping { get; set; }

        ////Temporary Hack because GpsInfo here is a object instead of object list
        //public ICollection<GpsInfo> GpsInfos
        //{
        //    get
        //    {
        //        return this.GpsInfo != null ? new List<GpsInfo> { this.GpsInfo } : new List<GpsInfo>();
        //    }
        //}
    }

    public class WebcamInfo
        : WebcamInfoRaven,
            IHasLanguage,
            IImageGalleryAware,
            IContactInfosAware,
            IDetailInfosAware,
            IGPSInfoAware,
            ISmgTags,
            IVideoItemsAware
    {
        public WebcamInfo()
        {
            WebCamProperties = new WebcamProperties();
            Detail = new Dictionary<string, Detail>();
            ContactInfos = new Dictionary<string, ContactInfos>();
            VideoItems = new Dictionary<string, ICollection<VideoItems>>();
        }

        public new ICollection<GpsInfo> GpsInfo { get; set; }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        public new IDictionary<string, GpsInfo> GpsPoints
        {
            get
            {
                if (this.GpsInfo != null && this.GpsInfo.Count > 0)
                {
                    return this.GpsInfo.ToDictionary(x => x.Gpstype, x => x);
                }
                else
                {
                    return new Dictionary<string, GpsInfo> { };
                }
            }
        }

        //New Webcam fields Feratel, Panomax, Panocloud Integration

        public IDictionary<string, ContactInfos> ContactInfos { get; set; }

        public ICollection<ImageGallery> ImageGallery { get; set; }

        //Video Items
        public IDictionary<string, ICollection<VideoItems>>? VideoItems { get; set; }

        public IDictionary<string, Detail> Detail { get; set; }

        public WebcamProperties WebCamProperties { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        [Obsolete("Use Detail.Title")]
        public new IDictionary<string, string> Webcamname
        {
            get { return this.Detail.ToDictionary(x => x.Key, x => x.Value.Title); }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        [Obsolete("Use WebcamProperties.Webcamurl")]
        public new string? Webcamurl
        {
            get { return this.WebCamProperties != null ? this.WebCamProperties.WebcamUrl : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        [Obsolete("Use WebcamProperties.Streamurl")]
        public new string? Streamurl
        {
            get { return this.WebCamProperties != null ? this.WebCamProperties.StreamUrl : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        [Obsolete("Use WebcamProperties.Previewurl")]
        public new string? Previewurl
        {
            get { return this.WebCamProperties != null ? this.WebCamProperties.PreviewUrl : null; }
        }

        public ICollection<string> HasLanguage { get; set; }
    }

    public class WebcamProperties
    {
        public string? WebcamUrl { get; set; }
        public string? StreamUrl { get; set; }
        public string? PreviewUrl { get; set; }

        public string? ViewAngleDegree { get; set; }
        public string? ZeroDirection { get; set; }
        public string? HtmlEmbed { get; set; }
        public bool? TourCam { get; set; }
        public bool? HasVR { get; set; }
        public string? ViewerType { get; set; }
    }

    #endregion

    #region Wine

    public class Wine
        : IIdentifiable,
            IShortName,
            IImportDateassigneable,
            ILicenseInfo,
            ISource,
            IMappingAware,
            IPublishedOn,
            IActivateable,
            ISmgActive
    {
        public LicenseInfo? LicenseInfo { get; set; }

        public Wine()
        {
            Detail = new Dictionary<string, Detail>();
            Mapping = new Dictionary<string, IDictionary<string, string>>();
        }

        public string? Id { get; set; }

        public string? Shortname { get; set; }

        public IDictionary<string, Detail> Detail { get; set; }

        //public Dictionary<string, string> Title { get; set; }
        //public Dictionary<string, string> WineName { get; set; }

        public int Vintage { get; set; }
        public int Awardyear { get; set; }

        [SwaggerSchema("Id on the primary data Source")]
        public string? CustomId { get; set; }
        public string? CompanyId { get; set; }

        public ICollection<ImageGallery>? ImageGallery { get; set; }
        public ICollection<string>? Awards { get; set; }

        public DateTime? LastChange { get; set; }
        public DateTime? FirstImport { get; set; }

        public bool Active { get; set; }

        [SwaggerDeprecated("Obsolete, use PublishedOn")]
        public bool SmgActive { get; set; }

        public ICollection<string>? HasLanguage { get; set; }

        public string Source { get; set; }

        public IDictionary<string, IDictionary<string, string>> Mapping { get; set; }

        public ICollection<string> PublishedOn { get; set; }
    }

    #endregion

    #region TypeInfos

    public class GastronomyTypes
    {
        public GastronomyTypes()
        {
            TypeDesc = new Dictionary<string, string>();
        }

        public string? Id { get; set; }
        public long Bitmask { get; set; }

        public string? Type { get; set; }

        public string? Key { get; set; }

        public Dictionary<string, string> TypeDesc { get; set; }
    }

    //For Types Api
    public class AccoTypes
    {
        public AccoTypes()
        {
            TypeDesc = new Dictionary<string, string>();
        }

        public string? Id { get; set; }
        public int Bitmask { get; set; }

        public string? Type { get; set; }

        public string? Key { get; set; }

        public Dictionary<string, string> TypeDesc { get; set; }

        public string? CustomId { get; set; }
    }

    //For Types Api
    public class AccoFeatures : AccoTypes
    {
        public string? ClusterId { get; set; }

        public string? ClusterCustomId { get; set; }
    }

    //For Types Api
    public class SmgPoiTypes
    {
        public SmgPoiTypes()
        {
            TypeDesc = new Dictionary<string, string>();
        }

        public string? Id { get; set; }
        public long Bitmask { get; set; }
        public string? Type { get; set; }
        public string? Parent { get; set; }
        public string Key { get; set; }

        public IDictionary<string, string>? TypeDesc { get; set; }
    }

    //For Types Api
    public class EventTypes
    {
        public EventTypes()
        {
            TypeDesc = new Dictionary<string, string>();
        }

        public string? Id { get; set; }
        public int Bitmask { get; set; }
        public string? Type { get; set; }
        public Dictionary<string, string> TypeDesc { get; set; }
    }

    //For Types Api
    public class ActivityTypes : SmgPoiTypes { }

    //For Types Api
    public class ArticleTypes : SmgPoiTypes { }

    //For Types Api
    public class PoiTypes : SmgPoiTypes { }

    //For Types Api
    public class ODHActivityPoiTypes : SmgPoiTypes { }

    //For Types Api
    public class EventShortTypes : SmgPoiTypes { }


    #endregion

    #region CommonInfos

    public class Metadata
    {
        public string Id { get; set; }

        [SwaggerEnum(
            new[]
            {
                "accommodation",
                "accommodationroom",
                "event",
                "odhactivitypoi",
                "measuringpoint",
                "webcam",
                "article",
                "venue",
                "eventshort",
                "experiencearea",
                "region",
                "metaregion",
                "tourismassociation",
                "municipality",
                "district",
                "area",
                "wineaward",
                "skiarea",
                "skiregion",
                "odhtag",
                "publisher",
                "tag",
                "weatherhistory",
                "weather",
                "weatherdistrict",
                "weatherforecast",
                "weatherrealtime",
                "snowreport",
                "odhmetadata",
                "package",
                "ltsactivity",
                "ltspoi",
                "ltsgastronomy",
            }
        )]
        public string Type { get; set; }
        [SwaggerSchema(Description = "Date when Data was lastly saved/updated")]
        public DateTime? LastUpdate { get; set; }        
        public string Source { get; set; }
        public bool Reduced { get; set; }
        public UpdateInfo? UpdateInfo { get; set; }
    }

    public class UpdateInfo
    {
        public string? UpdatedBy { get; set; }
        public string? UpdateSource { get; set; }

        public ICollection<UpdateHistory>? UpdateHistory { get; set; }
    }

    public class UpdateHistory
    {
        public DateTime? LastUpdate { get; set; }
        public string? UpdateSource { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class LicenseInfo
    {
        [SwaggerEnum(new[] { "CC0", "CC-BY", "Closed" })]
        public string? License { get; set; }
        public string? LicenseHolder { get; set; }
        public string? Author { get; set; }

        [SwaggerSchema(Description = "readonly field", ReadOnly = true)]
        public bool ClosedData { get; set; }
    }

    public class SmgTags
        : IIdentifiable,
            IShortName,
            IImportDateassigneable,
            ILicenseInfo,
            IPublishedOn
    {
        public LicenseInfo? LicenseInfo { get; set; }

        public SmgTags()
        {
            TagName = new Dictionary<string, string>();
            ValidForEntity = new List<string>();

            Mapping = new Dictionary<string, IDictionary<string, string>>();
        }

        public string? Id { get; set; }

        [SwaggerDeprecated("Deprecated, refer to TagName")]
        public string? Shortname { get; set; }

        public IDictionary<string, string> TagName { get; set; }
        public ICollection<string>? ValidForEntity { get; set; }

        public ICollection<string> Source { get; set; }

        public DateTime? FirstImport { get; set; }
        public DateTime? LastChange { get; set; }

        [SwaggerDeprecated("Deprecated")]
        public Nullable<bool> DisplayAsCategory { get; set; }

        [SwaggerDeprecated("Deprecated, use Mapping")]
        public IDictionary<string, string>? IDMCategoryMapping { get; set; }

        [SwaggerSchema("Deprecated, use Mapping")]
        [SwaggerDeprecated("Deprecated, use Mapping")]
        [Obsolete("Deprecated, use Mapping")]
        public LTSTaggingInfo? LTSTaggingInfo { get; set; }

        [SwaggerDeprecated("Deprecated, use ValidForEntity")]
        public string? MainEntity { get; set; }

        //Generic Mapping Object
        public IDictionary<string, IDictionary<string, string>>? Mapping { get; set; }

        [SwaggerDeprecated("Deprecated, use Mapping")]
        public ICollection<string>? MappedTagIds { get; set; }

        //obsolete replaced by PublishDataWithTagOn to be more generic
        //public ICollection<string>? AutoPublishOn { get; set; }

        //If this Tag is set whitelist for publisher true/false (Whitelist / Blacklist logic)
        public IDictionary<string, bool>? PublishDataWithTagOn { get; set; }

        public ICollection<string>? PublishedOn { get; set; }
    }

    public class Publisher : IIdentifiable, IImportDateassigneable, ILicenseInfo
    {
        public LicenseInfo? LicenseInfo { get; set; }

        public Publisher()
        {
            Name = new Dictionary<string, string>();            
        }

        public string? Id { get; set; }

        public string Key { get; set; }

        public IDictionary<string, string> Name { get; set; }

        public DateTime? FirstImport { get; set; }
        public DateTime? LastChange { get; set; }

        public string? Url { get; set; }

        //New fields to store Information on Push
        public ICollection<PushConfig>? PushConfig { get; set; }
    }

    public class PushConfig
    {
        public ICollection<string>? PathParam { get; set; }

        public string? BaseUrl { get; set; }

        public string? PushApiUrl
        {
            get
            {
                return String.Format(
                    "{0}/{1}",
                    this.BaseUrl != null ? this.BaseUrl : "",
                    String.Join("/", this.PathParam)
                );
            }
        }
    }

    public class Source : IIdentifiable, IImportDateassigneable, ILicenseInfo
    {
        public LicenseInfo? LicenseInfo { get; set; }

        public Source()
        {
            Name = new Dictionary<string, string>();
            Description = new Dictionary<string, string>();
        }

        public string? Id { get; set; }

        public string Key { get; set; }

        public IDictionary<string, string> Name { get; set; }
        public IDictionary<string, string> Description { get; set; }

        public DateTime? FirstImport { get; set; }
        public DateTime? LastChange { get; set; }

        public string? Url { get; set; }

        [SwaggerSchema("Interfaces that are offered by the source")]
        public ICollection<string>? Interfaces { get; set; }

        public ICollection<string> Types { get; set; }

        //New fields to store Information on Sync
        public ICollection<SyncDataConfig>? SyncDataConfig { get; set; }
    }

    public class SyncDataConfig
    {
        public ICollection<string>? PathParam { get; set; }

        public string? BaseUrl { get; set; }

        public string? SyncDataApiUrl
        {
            get
            {
                return String.Format(
                    "{0}/{1}",
                    this.BaseUrl != null ? this.BaseUrl : "",
                    String.Join("/", this.PathParam)
                );
            }
        }
    }

    public class LTSTaggingType
    {
        public LTSTaggingType()
        {
            TypeNames = new Dictionary<string, string>();
            TypeDescriptions = new Dictionary<string, string>();
        }

        public string? Id { get; set; }
        public string? Key { get; set; }
        public string? Entity { get; set; }
        public string? TypeParent { get; set; }
        public int Level { get; set; }
        public IDictionary<string, string> TypeNames { get; set; }
        public IDictionary<string, string> TypeDescriptions { get; set; }
    }

    public class LTSTaggingInfo
    {
        //NEW LTS RID
        public string LTSRID { get; set; }
        public string ParentLTSRID { get; set; }
    }

    public class LTSTin
    {
        public LTSTin()
        {
            Name = new Dictionary<string, string>();
            Description = new Dictionary<string, string>();
            LTSTinAssignment = new List<LTSTinAssignment>();
        }

        public string Id { get; set; }

        public IDictionary<string, string> Name { get; set; }
        public IDictionary<string, string> Description { get; set; }

        public ICollection<LTSTinAssignment> LTSTinAssignment { get; set; }
    }

    public class LTSTinAssignment
    {
        public string RID { get; set; }
        public string ODHTagId { get; set; }
        public string LTSTagRID { get; set; }
    }

    //BaseInfos for Districts / Regions / Municipalities ...
    public abstract class BaseInfos
        : IIdentifiable,
            IActivateable,
            ISmgTags,
            ISmgActive,
            IHasLanguage,
            IImportDateassigneable,
            ILicenseInfo,
            IDetailInfosAware,
            IContactInfosAware,
            ISource,
            IMappingAware,
            IDistanceInfoAware,
            IPublishedOn, 
            IHasTagInfo
    {
        public LicenseInfo? LicenseInfo { get; set; }

        public BaseInfos()
        {
            Detail = new Dictionary<string, Detail>();
            ContactInfos = new Dictionary<string, ContactInfos>();
            //Mapping New
            Mapping = new Dictionary<string, IDictionary<string, string>>();
        }

        public string? Id { get; set; }
        public bool Active { get; set; }
        public string? CustomId { get; set; }
        public string? Shortname { get; set; }

        public string? Gpstype { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Altitude { get; set; }
        public string? AltitudeUnitofMeasure { get; set; }

        public IDictionary<string, Detail> Detail { get; set; }
        public IDictionary<string, ContactInfos> ContactInfos { get; set; }
        public ICollection<ImageGallery>? ImageGallery { get; set; }

        public ICollection<string>? SmgTags { get; set; }

        //public DateTime FirstImport { get; set; }
        //public DateTime LastChange { get; set; }

        [SwaggerDeprecated("Obsolete, use PublishedOn")]
        public bool SmgActive { get; set; }

        public ICollection<string>? HasLanguage { get; set; }

        public DateTime? LastChange { get; set; }
        public DateTime? FirstImport { get; set; }

        public ICollection<string>? PublishedOn { get; set; }

        public string Source { get; set; }

        public IDictionary<string, IDictionary<string, string>> Mapping { get; set; }

        public DistanceInfo? DistanceInfo { get; set; }

        public ICollection<Tags>? Tags { get; set; }

        public ICollection<string>? TagIds { get; set; }
    }

    //BaseInfos for ODHActivityPois / Activities / Pois
    public class PoiBaseInfos
        : IIdentifiable,
            IActivateable,
            IGeoDataInfoAware,
            IActivityStatus,
            IImageGalleryAware,
            IContactInfosAware,
            IAdditionalPoiInfosAware,
            ISmgTags,
            ISmgActive,
            IHasLanguage,
            IImportDateassigneable,
            ILicenseInfo,
            IDetailInfosAware,
            ISource,
            IMappingAware,
            IDistanceInfoAware,
            IGPSInfoAware,
            IPublishedOn,
            IVideoItemsAware
    {
        public LicenseInfo? LicenseInfo { get; set; }

        public PoiBaseInfos()
        {
            Detail = new Dictionary<string, Detail>();
            ContactInfos = new Dictionary<string, ContactInfos>();
            AdditionalPoiInfos = new Dictionary<string, AdditionalPoiInfos>();
            Mapping = new Dictionary<string, IDictionary<string, string>>();
            Tags = new List<Tags>();
            VideoItems = new Dictionary<string, ICollection<VideoItems>>();
        }

        public string? Id { get; set; }

        [SwaggerDeprecated("Use Mappings")]
        public string? OutdooractiveID { get; set; }
        [SwaggerDeprecated("Use Mappings")]
        public string? OutdooractiveElevationID { get; set; }

        [SwaggerDeprecated("Use Mappings")]
        public string? SmgId { get; set; }

        public bool? CopyrightChecked { get; set; }

        [SwaggerSchema("Active on Source")]
        public bool Active { get; set; }
        public string? Shortname { get; set; }

        [SwaggerDeprecated("Deprecated, Use Ratings.Difficulty")]
        public string? Difficulty { get; set; }

        [SwaggerDeprecated("Deprecated, Use Tags instead")]
        public string? Type { get; set; }

        [SwaggerDeprecated("Deprecated, Use Tags instead")]
        public string? SubType { get; set; }

        [SwaggerDeprecated("Deprecated, Use Tags instead")]
        public string? PoiType { get; set; }

        public DateTime? FirstImport { get; set; }
        public DateTime? LastChange { get; set; }

        [SwaggerDeprecated("Deprecated, Use PublishedOn field")]
        public bool SmgActive { get; set; }

        public LocationInfo? LocationInfo { get; set; }

        public string? TourismorganizationId { get; set; }

        [SwaggerDeprecated("Deprecated, use AreaIds")]
        public HashSet<string>? AreaId { get; set; }

        [GetOnlyJsonProperty]
        public ICollection<string>? AreaIds
        {
            get { return this.AreaId; }
        }

        public double? AltitudeDifference { get; set; }
        public double? AltitudeHighestPoint { get; set; }
        public double? AltitudeLowestPoint { get; set; }
        public double? AltitudeSumUp { get; set; }
        public double? AltitudeSumDown { get; set; }

        public double? DistanceDuration { get; set; }
        public double? DistanceLength { get; set; }

        public bool? Highlight { get; set; }
        public bool? IsOpen { get; set; }
        public bool? IsPrepared { get; set; }
        public bool? RunToValley { get; set; }
        public bool? IsWithLigth { get; set; }
        public bool? HasRentals { get; set; }
        public bool? HasFreeEntrance { get; set; }
        public bool? LiftAvailable { get; set; }
        public bool? FeetClimb { get; set; }

        public bool? BikeTransport { get; set; }

        public ICollection<OperationSchedule>? OperationSchedule { get; set; }

        public ICollection<GpsInfo>? GpsInfo { get; set; }
        public ICollection<GpsTrack>? GpsTrack { get; set; }

        public ICollection<ImageGallery>? ImageGallery { get; set; }
        public IDictionary<string, Detail> Detail { get; set; }
        public IDictionary<string, ContactInfos> ContactInfos { get; set; }
        public IDictionary<string, AdditionalPoiInfos> AdditionalPoiInfos { get; set; }

        public ICollection<string>? SmgTags { get; set; }
        public ICollection<string>? HasLanguage { get; set; }

        public Ratings? Ratings { get; set; }
        public ICollection<string>? Exposition { get; set; }

        public string? OwnerRid { get; set; }

        public List<string>? ChildPoiIds { get; set; }
        public List<string>? MasterPoiIds { get; set; }

        public int? WayNumber { get; set; }

        public string? Number { get; set; }

        public List<LTSTags>? LTSTags { get; set; }

        public ICollection<string>? PublishedOn { get; set; }

        public string Source { get; set; }

        public IDictionary<string, IDictionary<string, string>> Mapping { get; set; }

        public DistanceInfo? DistanceInfo { get; set; }

        public ICollection<Tags> Tags { get; set; }

        public IDictionary<string, ICollection<VideoItems>>? VideoItems { get; set; }
    }

    public class Detail : IDetailInfos, ILanguage
    {
        public string? Header { get; set; }

        //public string SiteHeader { get; set; }
        public string? SubHeader { get; set; }
        public string? IntroText { get; set; }
        public string? BaseText { get; set; }
        public string? Title { get; set; }

        //OLT
        //public string Alttext { get; set; }
        public string? AdditionalText { get; set; }

        //NEW
        public string? MetaTitle { get; set; }
        public string? MetaDesc { get; set; }

        public string? GetThereText { get; set; }
        public string? Language { get; set; }

        public ICollection<string>? Keywords { get; set; }

        //New LTS Fields
        public string? ParkingInfo { get; set; }
        public string? PublicTransportationInfo { get; set; }
        public string? AuthorTip { get; set; }
        public string? SafetyInfo { get; set; }
        public string? EquipmentInfo { get; set; }
    }

    //Special Element for Themed Content
    public class DetailThemed : ILanguage
    {
        public DetailThemed()
        {
            DetailsThemed = new Dictionary<string, DetailsThemed>();
        }

        public IDictionary<string, DetailsThemed> DetailsThemed { get; set; }
        public string? Language { get; set; }
    }

    public class DetailsThemed
    {
        public string? Title { get; set; }
        public string? Intro { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDesc { get; set; }
    }

    public class GpsInfo : IGpsInfo
    {
        //[DefaultValue("position")]
        //[JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [SwaggerEnum(
            new[]
            {
                "position",
                "viewpoint",
                "startingandarrivalpoint",
                "startingpoint",
                "arrivalpoint",
                "carparking",
                "halfwaypoint",
                "valleystationpoint",
                "middlestationpoint",
                "mountainstationpoint",
            }
        )]
        public string? Gpstype { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Altitude { get; set; }
        public string? AltitudeUnitofMeasure { get; set; }
        public string? Geometry { get; set; }
        public bool? Default { get; set; }

        [JsonIgnore]
        public Geometry? Geometry4326
        {
            get
            {
                if (string.IsNullOrEmpty(Geometry)) return null;
                try
                {
                    // Use NTS to parse the WKT string
                    var reader = new WKTReader();
                    var rawGeo = reader.Read(Geometry);
                    // Attempt to repair the geometry automatically
                    var geo = GeometryFixer.Fix(rawGeo);
                    geo.SRID = 4326;
                    return geo;
                }
                catch (ParseException)
                {
                    return null;
                }
            }
        }

        [JsonIgnore]
        public bool IsValidGeometry 
        {
            get
            {
                var geo = Geometry4326;
                
                if (geo == null) return false;
                if (!geo.IsValid) return false;
                
                return true;
            }
        }
    }

    public class DistanceInfo : IDistanceInfo
    {
        public Nullable<double> DistanceToMunicipality { get; set; }
        public Nullable<double> DistanceToDistrict { get; set; }
    }

    public class GpsTrack : IGpsTrack
    {
        public GpsTrack()
        {
            GpxTrackDesc = new Dictionary<string, string>();
        }

        public string? Id { get; set; }
        public IDictionary<string, string>? GpxTrackDesc { get; set; }
        public string? GpxTrackUrl { get; set; }
        public string? Type { get; set; }
        public string? Format { get; set; }

        ////To have self link
        //public string? Self
        //{
        //    get
        //    {
        //        return this.GpxTrackUrl != null && this.GpxTrackUrl.StartsWith("GeoShapes") ? this.GpxTrackUrl : null;
        //    }
        //}
    }

    public class GpsPolygon : IGpsPolygon
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class PublishedonObject
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public DateTime LastChange { get; set; }
    }

    public class ImageGallery : IImageGallery
    {
        public ImageGallery()
        {
            ImageTitle = new Dictionary<string, string>();
            ImageDesc = new Dictionary<string, string>();
            ImageAltText = new Dictionary<string, string>();
        }

        public string? ImageName { get; set; }
        public string? ImageUrl { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string? ImageSource { get; set; }

        public IDictionary<string, string>? ImageTitle { get; set; }
        public IDictionary<string, string>? ImageDesc { get; set; }
        public IDictionary<string, string>? ImageAltText { get; set; }

        public bool? IsInGallery { get; set; }
        public int? ListPosition { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }

        //NEU
        public string? CopyRight { get; set; }
        public string? License { get; set; }
        public string? LicenseHolder { get; set; }
        public ICollection<string>? ImageTags { get; set; }
    }

    public class VideoItems : IVideoItems
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? VideoSource { get; set; }
        public string? VideoType { get; set; }
        public string? StreamingSource { get; set; }
        public string? VideoTitle { get; set; }
        public string? VideoDesc { get; set; }
        public bool? Active { get; set; }
        public string? CopyRight { get; set; }
        public string? License { get; set; }
        public string? LicenseHolder { get; set; }
        public string? Language { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }

        //NEW
        public string? Definition { get; set; }
        public double? Duration { get; set; }

        //To Remove (at the moment here is stored panomax 360 / 720, this info is already into Definition with SD/HD
        public int? Resolution { get; set; }
        public int? Bitrate { get; set; }
    }

    public class ContactInfos : IContactInfos, ILanguage
    {
        [SwaggerSchema(Description = "Street Address")]
        public string? Address { get; set; }

        [SwaggerSchema(Description = "Region (Province / State / Departement / Canton etc...")]
        public string? Region { get; set; }

        [SwaggerSchema(Description = "Regioncode")]
        public string? RegionCode { get; set; }

        [SwaggerSchema(Description = "Area (Additional Area Name)")]
        public string? Area { get; set; }
        public string? City { get; set; }
        public string? ZipCode { get; set; }
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }
        public string? Surname { get; set; }
        public string? Givenname { get; set; }
        public string? NamePrefix { get; set; }
        public string? Email { get; set; }
        public string? Phonenumber { get; set; }
        public string? Faxnumber { get; set; }
        public string? Url { get; set; }
        public string? Language { get; set; }
        public string? CompanyName { get; set; }
        public string? Vat { get; set; }
        public string? Tax { get; set; }
        public string? LogoUrl { get; set; }
    }

    public class AdditionalPoiInfos : IAdditionalPoiInfos, ILanguage
    {
        //public string Difficulty { get; set; }
        public string? Novelty { get; set; }

        [SwaggerDeprecated("Deprecated, Use Tags instead")]
        public string? MainType { get; set; }

        [SwaggerDeprecated("Deprecated, Use Tags instead")]
        public string? SubType { get; set; }

        [SwaggerDeprecated("Deprecated, Use Tags instead")]
        public string? PoiType { get; set; }

        public string? Language { get; set; }

        [SwaggerDeprecated("Deprecated, Use Tags instead")]
        public List<string>? Categories { get; set; }
    }

    public class Ratings : IRatings
    {
        public string? Stamina { get; set; }
        public string? Experience { get; set; }
        public string? Landscape { get; set; }
        public string? Difficulty { get; set; }
        public string? Technique { get; set; }
    }

    public class LTSTags
    {
        public LTSTags()
        {
            TagName = new Dictionary<string, string>();
            LTSTins = new List<LTSTins>();
        }

        public string? Id { get; set; }
        public int Level { get; set; }
        public string? LTSRID { get; set; }
        public IDictionary<string, string> TagName { get; set; }

        public ICollection<LTSTins>? LTSTins { get; set; }
    }

    public class LTSTins
    {
        public LTSTins()
        {
            TinName = new Dictionary<string, string>();
        }

        public string? Id { get; set; }
        public string? LTSRID { get; set; }
        public IDictionary<string, string> TinName { get; set; }
    }

    public class AdditionalArticleInfos : ILanguage
    {
        public AdditionalArticleInfos()
        {
            Elements = new Dictionary<string, string>();
        }

        public string? Language { get; set; }
        public IDictionary<string, string> Elements { get; set; }
    }

    public class ArticleLinkInfo
    {
        public ArticleLinkInfo()
        {
            Elements = new Dictionary<string, string>();
        }

        public string? Language { get; set; }
        public IDictionary<string, string> Elements { get; set; }
    }

    public class CategoryCodes : IIdentifiable
    {
        public string? Id { get; set; }
        public string? Shortname { get; set; }
        //public string Language { get; set; }
    }

    public class Facilities : IIdentifiable
    {
        public string? Id { get; set; }
        public string? Shortname { get; set; }
        //public string Language { get; set; }
    }

    public class CapacityCeremony : IIdentifiable
    {
        public string? Id { get; set; }
        public string? Shortname { get; set; }
        public int MaxSeatingCapacity { get; set; }
        //public string Language { get; set; }
    }

    public class DishRates : IIdentifiable
    {
        public string? Id { get; set; }
        public string? Shortname { get; set; }
        public double MinAmount { get; set; }
        public double MaxAmount { get; set; }
        public string? CurrencyCode { get; set; }
        //public string Language { get; set; }
    }

    [SwaggerSchema(
        "Wiki Article on <a href='https://github.com/noi-techpark/odh-docs/wiki/Operationschedule-Format' target='_blank'>Wiki Article</a>"
    )]
    public class OperationSchedule : IOperationSchedules
    {
        public OperationSchedule()
        {
            OperationscheduleName = new Dictionary<string, string>();
        }

        public IDictionary<string, string>? OperationscheduleName { get; set; }
        public DateTime Start { get; set; }
        public DateTime Stop { get; set; }

        /// <summary>
        /// Type: 1 - Standard, 2 - Only day + month recurring (year not to consider) 3 - only month recurring (season: year and day not to consider)
        /// </summary>
        [SwaggerSchema(
            "1 - Standard, 2 - Only day + month recurring (year not to consider) 3 - only month recurring (season: year and day not to consider), Wiki Article on <a href='https://github.com/noi-techpark/odh-docs/wiki/Operationschedule-Format' target='_blank'>Wiki Article</a>"
        )]
        public string? Type { get; set; }

        public ICollection<OperationScheduleTime>? OperationScheduleTime { get; set; }
    }

    public class OperationScheduleTime : IOperationScheduleTime
    {
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }

        // Here for compatibility reasons
        [SwaggerDeprecated("Will be removed within 2023-12-31")]
        public bool Thuresday
        {
            get { return Thursday; }
        }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }

        /// <summary>
        /// //1 = closed, 2 = open, 0 = undefined
        /// </summary>
        [SwaggerSchema(
            "1 = closed, 2 = open, 0 = undefined, Wiki Article on <a href='https://github.com/noi-techpark/odh-docs/wiki/Operationschedule-Format' target='_blank'>Wiki Article</a>"
        )]
        public int State { get; set; }

        /// <summary>
        /// 1 = General Opening Time, 2 = time range for warm meals, 3 = time range for pizza, 4 = time range for snack’s
        /// </summary>
        [SwaggerSchema(
            "1 = General Opening Time, 2 = time range for warm meals, 3 = time range for pizza, 4 = time range for snack’s, Wiki Article on <a href='https://github.com/noi-techpark/odh-docs/wiki/Operationschedule-Format' target='_blank'>Wiki Article</a>"
        )]
        public int Timecode { get; set; }
    }
  

    public class LocationInfo : ILocationInfoAware
    {
        public RegionInfo? RegionInfo { get; set; }
        public TvInfo? TvInfo { get; set; }
        public MunicipalityInfo? MunicipalityInfo { get; set; }
        public DistrictInfo? DistrictInfo { get; set; }
        public AreaInfo? AreaInfo { get; set; }
    }

    public class RegionInfo
    {
        public string? Id { get; set; }

        [SwaggerSchema(
            "This Name field is a Helper field and may contain not updated LocationInfo Names, Use the Id/Selflink to retrieve correct names from the appropriate Endpoint"
        )]
        public IDictionary<string, string?>? Name { get; set; }
    }

    public class TvInfo
    {
        public string? Id { get; set; }

        [SwaggerSchema(
            "This Name field is a Helper field and may contain not updated LocationInfo Names, Use the Id/Selflink to retrieve correct names from the appropriate Endpoint"
        )]
        public IDictionary<string, string?>? Name { get; set; }
    }

    public class MunicipalityInfo
    {
        public string? Id { get; set; }

        [SwaggerSchema(
            "This Name field is a Helper field and may contain not updated LocationInfo Names, Use the Id/Selflink to retrieve correct names from the appropriate Endpoint"
        )]
        public IDictionary<string, string?>? Name { get; set; }
    }

    public class DistrictInfo
    {
        public string? Id { get; set; }

        [SwaggerSchema(
            "This Name field is a Helper field and may contain not updated LocationInfo Names, Use the Id/Selflink to retrieve correct names from the appropriate Endpoint"
        )]
        public IDictionary<string, string?>? Name { get; set; }
    }

    public class AreaInfo
    {
        public string? Id { get; set; }

        [SwaggerSchema(
            "This Name field is a Helper field and may contain not updated LocationInfo Names, Use the Id/Selflink to retrieve correct names from the appropriate Endpoint"
        )]
        public IDictionary<string, string>? Name { get; set; }
    }

    #endregion

    #region Type2Endpoint

    public class GenericODHData : IIdentifiable, IPublishedOn, IActivateable
    {
        public string Id { get; set; }
        public string? Shortname { get; set; }
        public ICollection<string>? PublishedOn { get; set; }
        public bool Active { get; set; }
    }

    public class DataModelHelpers
    {
        /// <summary>
        /// Translates Type (Metadata) as String to PG Table Name
        /// </summary>
        /// <param name="odhtype"></param>
        /// <returns></returns>
        public static string TranslateTypeString2EndPoint(string odhtype)
        {
            return odhtype switch
            {
                "accommodation" => "Accommodation",
                "acco" => "Accommodation",
                "accommodationroom" => "AccommodationRoom",
                "ltsactivity" => "Activity",
                "ltspoi" => "Poi",
                "ltsgastronomy" => "Gastronomy",
                "event" => "Event",
                "odhactivitypoi" => "ODHActivityPoi",
                "package" => "Package",
                "webcam" => "WebcamInfo",
                "article" => "Article",
                "venue" => "Venue",
                "eventshort" => "EventShort",
                "experiencearea" => "ExperienceArea",
                "metaregion" => "MetaRegion",
                "region" => "Region",
                "tourismassociation" => "TourismAssociation",
                "municipality" => "Municipality",
                "district" => "District",
                "skiarea" => "SkiArea",
                "skiregion" => "SkiRegion",
                "area" => "Area",
                "wineaward" => "WineAward",
                "odhtag" => "ODHTag",
                "publisher" => "Publisher",
                "source" => "Source",
                "weather" => "Weather",
                "weatherhistory" => "WeatherHistory",
                "measuringpoint" => "Weather/Measuringpoint",
                "weatherdistrict" => "Weather/District",
                "weatherforecast" => "Weather/Forecast",
                "weatherrealtime" => "Weather/Realtime",
                "snowreport" => "Weather/Snowreport",
                "odhmetadata" => "MetaData",
                "tag" => "Tag",
                _ => throw new Exception("not known odh type"),
            };
        }

        //Temporary Hack for Related Content Exceptions
        public static string TranslateTypeString2EndPointForRelatedContent(string odhtype)
        {
            try
            {
                if (odhtype == "acco")
                    odhtype = "accommodation";

                return TranslateTypeString2EndPoint(odhtype);
            }
            catch (Exception)
            {
                return "ODHActivityPoi";
            }
        }
    }

    #endregion

    public class PushResponse
    {
        public string Id { get; set; }
        public string Publisher { get; set; }
        public DateTime Date { get; set; }
        public dynamic Result { get; set; }

        public PushObject? PushObject { get; set; }
    }

    public class PushObject
    {
        public string Id { get; set; }

        //Add the info for the pushed object
        public string Type { get; set; }
    }

    public class PushResult
    {
        public int? Messages { get; set; }
        public bool Success { get; set; }
        public string? Response { get; set; }
        public string? Error { get; set; }

        public static PushResult MergeMultipleFCMPushNotificationResponses(
            IEnumerable<PushResult> responses
        )
        {
            return new PushResult()
            {
                Messages = responses.Sum(x => x.Messages),
                Error = String.Join("|", responses.Select(x => x.Error)),
                Success = responses.Any(x => x.Success == true),
                Response = String.Join("|", responses.Select(x => x.Response)),
            };
        }
    }
}
