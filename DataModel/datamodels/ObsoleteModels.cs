// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel.Annotations;
using DataModel.validation;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModel
{

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

        public IDictionary<string, Detail> Detail { get; set; }

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

}
