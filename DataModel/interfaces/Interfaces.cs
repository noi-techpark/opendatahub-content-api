// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Swashbuckle.AspNetCore.Annotations;

namespace DataModel
{
    #region Interfaces

    //Common Interfaces (shared between all Entities)

    public interface IIdentifiable
    {
        string Id { get; set; }
    }

    public interface IShortName
    {
        string? Shortname { get; set; }
    }

    public interface IMetaData
    {
        Metadata _Meta { get; set; }
    }

    public interface ILicenseInfo
    {
        LicenseInfo LicenseInfo { get; set; }
    }

    public interface ISource
    {
        [SwaggerSchema("Source of the Data")]
        string Source { get; set; }
    }

    public interface IImportDateassigneable
    {
        DateTime? FirstImport { get; set; }
        DateTime? LastChange { get; set; }
    }

    public interface IDistrictId
    {
        string? DistrictId { get; set; }
    }

    public interface IMappingAware
    {
        IDictionary<string, IDictionary<string, string>> Mapping { get; set; }
    }

    public interface IActivateable
    {
        bool Active { get; set; }
    }

    public interface ILanguage
    {
        string? Language { get; set; }
    }

    public interface IHasLanguage
    {
        ICollection<string>? HasLanguage { get; set; }
    }

    public interface IDetailInfosAware
    {
        IDictionary<string, Detail> Detail { get; set; }
    }

    public interface IDetailInfos
    {
        string? Header { get; set; }
        string? IntroText { get; set; }
        string? BaseText { get; set; }
        string? Title { get; set; }

        string? MetaTitle { get; set; }
        string? MetaDesc { get; set; }

        string? AdditionalText { get; set; }
        string? GetThereText { get; set; }
    }

    public interface IContactInfos
    {
        string? Address { get; set; }
        string? City { get; set; }
        string? ZipCode { get; set; }
        string? CountryCode { get; set; }
        string? CountryName { get; set; }
        string? Surname { get; set; }
        string? Givenname { get; set; }
        string? NamePrefix { get; set; }
        string? Email { get; set; }
        string? Phonenumber { get; set; }
        string? Faxnumber { get; set; }
        string? Url { get; set; }
        string? Vat { get; set; }
        string? Tax { get; set; }
    }

    public interface IContactInfosAware
    {
        IDictionary<string, ContactInfos> ContactInfos { get; set; }
    }

    public interface IImageGallery
    {
        string? ImageName { get; set; }
        string? ImageUrl { get; set; }
        int? Width { get; set; }
        int? Height { get; set; }
        string? ImageSource { get; set; }

        IDictionary<string, string> ImageTitle { get; set; }
        IDictionary<string, string> ImageDesc { get; set; }

        bool? IsInGallery { get; set; }
        int? ListPosition { get; set; }
        DateTime? ValidFrom { get; set; }
        DateTime? ValidTo { get; set; }
    }

    public interface IImageGalleryAware
    {
        ICollection<ImageGallery>? ImageGallery { get; set; }
    }

    public interface IVideoItems
    {
        string? Name { get; set; }
        string? Url { get; set; }
        string? VideoSource { get; set; }
        string? VideoType { get; set; }

        string? StreamingSource { get; set; }

        string VideoTitle { get; set; }
        string VideoDesc { get; set; }

        bool? Active { get; set; }
        string? CopyRight { get; set; }
        string? License { get; set; }
        string? LicenseHolder { get; set; }
    }

    public interface IVideoItemsAware
    {
        IDictionary<string, ICollection<VideoItems>>? VideoItems { get; set; }
    }

    public interface ILocationInfoAware
    {
        RegionInfo? RegionInfo { get; set; }
        TvInfo? TvInfo { get; set; }
        MunicipalityInfo? MunicipalityInfo { get; set; }
        DistrictInfo? DistrictInfo { get; set; }
    }

    public interface IGpsInfo
    {
        string? Gpstype { get; set; }
        double Latitude { get; set; }
        double Longitude { get; set; }
        double? Altitude { get; set; }
        string? AltitudeUnitofMeasure { get; set; }
    }

    public interface IGPSInfoAware
    {
        ICollection<GpsInfo> GpsInfo { get; set; }
    }

    public interface IGPSPointsAware
    {
        IDictionary<string, GpsInfo> GpsPoints { get; }
    }

    public interface IDistanceInfoAware
    {
        DistanceInfo? DistanceInfo { get; set; }
    }

    public interface IDistanceInfo
    {
        Nullable<double> DistanceToDistrict { get; set; }

        Nullable<double> DistanceToMunicipality { get; set; }
    }

    public interface IGpsTrack
    {
        string? Id { get; set; }
        IDictionary<string, string> GpxTrackDesc { get; set; }
        string? GpxTrackUrl { get; set; }
        string? Type { get; set; }

        string? Format { get; set; }
    }

    public interface IGpsPolygon
    {
        double Latitude { get; set; }
        double Longitude { get; set; }
    }

    public interface IGpsPolygonAware
    {
        ICollection<GpsPolygon>? GpsPolygon { get; set; }
    }

    public interface IGeoDataInfoAware
    {
        double? AltitudeDifference { get; set; }
        double? AltitudeHighestPoint { get; set; }
        double? DistanceDuration { get; set; }
        double? DistanceLength { get; set; }

        ICollection<GpsInfo>? GpsInfo { get; set; }
        ICollection<GpsTrack>? GpsTrack { get; set; }
    }

    public interface IWebcam
    {
        string? WebcamId { get; set; }
        IDictionary<string, string> Webcamname { get; set; }
        GpsInfo? GpsInfo { get; set; }

        string? Webcamurl { get; set; }
    }

    public interface IWebcamAware
    {
        ICollection<Webcam>? Webcam { get; set; }
    }

    public interface IOperationSchedules
    {
        IDictionary<string, string> OperationscheduleName { get; set; }

        //string OperationscheduleName { get; set; }
        DateTime Start { get; set; }
        DateTime Stop { get; set; }

        //bool? ClosedonPublicHolidays { get; set; }

        ICollection<OperationScheduleTime>? OperationScheduleTime { get; set; }
    }

    public interface IOperationScheduleTime
    {
        TimeSpan Start { get; set; }
        TimeSpan End { get; set; }
        bool Monday { get; set; }
        bool Tuesday { get; set; }
        bool Wednesday { get; set; }
        bool Thursday { get; set; }
        bool Friday { get; set; }
        bool Saturday { get; set; }
        bool Sunday { get; set; }
        int State { get; set; }
        int Timecode { get; set; }
    }

    public interface ISmgTags
    {
        ICollection<string>? SmgTags { get; set; }
    }

    public interface IRatings
    {
        string? Stamina { get; set; }
        string? Experience { get; set; }
        string? Landscape { get; set; }
        string? Difficulty { get; set; }
        string? Technique { get; set; }
    }

    public interface ISmgActive
    {
        bool SmgActive { get; set; }
    }

    public interface ISuedtirolType
    {
        string? Id { get; set; }
        string? Key { get; set; }
        string? Entity { get; set; }
        string? TypeParent { get; set; }
        int Level { get; set; }
        IDictionary<string, string> TypeNames { get; set; }
    }

    public interface ITags
    {
        IDictionary<string, List<Tags>> Tags { get; set; }
    }

    public interface IPublishedOn
    {
        ICollection<string>? PublishedOn { get; set; }
    }

    //ODHActivityPoi

    public interface IAdditionalPoiInfosAware
    {
        IDictionary<string, AdditionalPoiInfos> AdditionalPoiInfos { get; set; }
    }

    public interface IAdditionalPoiInfos
    {
        string? Novelty { get; set; }
        string? MainType { get; set; }
        string? SubType { get; set; }
        string? PoiType { get; set; }

        List<string> Categories { get; set; }
    }

    public interface IActivityStatus
    {
        bool? IsOpen { get; set; }
        bool? IsPrepared { get; set; }
        bool? RunToValley { get; set; }
        bool? IsWithLigth { get; set; }
        bool? HasRentals { get; set; }
    }

    //Article

    public interface IAdditionalArticleInfosAware
    {
        IDictionary<string, AdditionalArticleInfos> AdditionalArticleInfos { get; set; }
    }

    //Event

    public interface IEventAdditionalInfos
    {
        string? Mplace { get;  }
        string? Reg { get;  }
        string? Location { get; set; }
    }

    public interface IEventPrice
    {
        double Price { get; set; }
        string? Type { get; set; }
        string? Pstd { get; set; }
        string? ShortDesc { get; set; }
        string? Description { get; set; }
    }

    public interface IEventDate
    {
        DateTime From { get; set; }
        DateTime To { get; set; }
        bool? SingleDays { get; set; }
        int? MinPersons { get; set; }
        int? MaxPersons { get; set; }
        TimeSpan? Begin { get; set; }
        TimeSpan? End { get; set; }
        TimeSpan? Entrance { get; set; }
    }

    public interface IHasTagInfo
    {
        ICollection<string> TagIds { get; set; }
        ICollection<Tags> Tags { get; set; }
    }

    public interface IHasAdditionalProperties
    {
        IDictionary<string, dynamic> AdditionalProperties { get; set; }
    }

    #endregion
}
