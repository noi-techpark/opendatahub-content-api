// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using DataModel.Annotations;
using Swashbuckle.AspNetCore.Annotations;

namespace DataModel
{
    /// <summary>
    /// This class contains the classes used by Open Data Hub PG Instance with linked data
    /// </summary>
    #region Linked Sub Classes

    public class AccoFeatureLinked : AccoFeature
    {
        public string? Self
        {
            get { return "AccommodationFeatures/" + Uri.EscapeDataString(this.Id); }
        }
    }

    public class AccoRoomInfoLinked : AccoRoomInfo
    {
        public string? Self
        {
            get { return "AccommodationRoom/" + Uri.EscapeDataString(this.Id); }
        }
    }

    public class TopicLinked : Topic
    {
        public string? Self
        {
            get { return "EventTopics/" + Uri.EscapeDataString(this.TopicRID); }
        }
    }

    public class CategoryCodesLinked : CategoryCodes
    {
        public string? Self
        {
            get
            {
                return String.IsNullOrEmpty(this.Id)
                    ? null
                    : "GastronomyTypes/" + Uri.EscapeDataString(this.Id);
            }
        }
    }

    public class FacilitiesLinked : Facilities
    {
        public string? Self
        {
            get
            {
                return String.IsNullOrEmpty(this.Id)
                    ? null
                    : "GastronomyTypes/" + Uri.EscapeDataString(this.Id);
            }
        }
    }

    public class CapacityCeremonyLinked : CapacityCeremony
    {
        public string? Self
        {
            get
            {
                return String.IsNullOrEmpty(this.Id)
                    ? null
                    : "GastronomyTypes/" + Uri.EscapeDataString(this.Id);
            }
        }
    }

    public class DishRatesLinked : DishRates
    {
        public string? Self
        {
            get
            {
                return String.IsNullOrEmpty(this.Id)
                    ? null
                    : "GastronomyTypes/" + Uri.EscapeDataString(this.Id);
            }
        }
    }

    public class RegionInfoLinked : RegionInfo
    {
        public string? Self
        {
            get
            {
                return String.IsNullOrEmpty(this.Id)
                    ? null
                    : "Region/" + Uri.EscapeDataString(this.Id);
            }
        }
    }

    public class TvInfoLinked : TvInfo
    {
        public string? Self
        {
            get
            {
                return String.IsNullOrEmpty(this.Id)
                    ? null
                    : "TourismAssociation/" + Uri.EscapeDataString(this.Id);
            }
        }
    }

    public class MunicipalityInfoLinked : MunicipalityInfo
    {
        public string? Self
        {
            get
            {
                return String.IsNullOrEmpty(this.Id)
                    ? null
                    : "Municipality/" + Uri.EscapeDataString(this.Id);
            }
        }
    }

    public class DistrictInfoLinked : DistrictInfo
    {
        public string? Self
        {
            get
            {
                return String.IsNullOrEmpty(this.Id)
                    ? null
                    : "District/" + Uri.EscapeDataString(this.Id);
            }
        }
    }

    public class AreaInfoLinked : AreaInfo
    {
        public string? Self
        {
            get
            {
                return String.IsNullOrEmpty(this.Id)
                    ? null
                    : "Area/" + Uri.EscapeDataString(this.Id);
            }
        }
    }

    public class LocationInfoLinked : LocationInfo
    {
        public new RegionInfoLinked? RegionInfo { get; set; }
        public new TvInfoLinked? TvInfo { get; set; }
        public new MunicipalityInfoLinked? MunicipalityInfo { get; set; }
        public new DistrictInfoLinked? DistrictInfo { get; set; }
        public new AreaInfoLinked? AreaInfo { get; set; }
    }

    public class LTSTagsLinked : LTSTags
    {
        public string? Self
        {
            get { return String.IsNullOrEmpty(this.Id) ? null : "ODHTag/" + this.Id.ToLower(); }
        }
    }

    public class ODHTags
    {
        public string Id { get; set; }
        public string? Self { get; set; }
    }

    //NEW Tags GENERIC
    public class Tags
    {
        public string Id { get; set; }

        //Source of the Tag (from /Tags.Source)
        public string Source { get; set; }
        public string? Self
        {
            get { return "Tag/" + this.Id; }
        }

        //Type of the Tag (from /Tags.Source)
        public string? Type { get; set; }

        public string? Name { get; set; }
    }

    public class ODHActivityPoiTypesLink
    {
        public string Id { get; set; }
        public string? Self { get; set; }
        public string Type { get; set; }
    }

    public class AccoCategory
    {
        public string Id { get; set; }
        public string? Self { get; set; }
    }

    public class AccoType
    {
        public string Id { get; set; }
        public string? Self { get; set; }
    }

    public class AccoBoards
    {
        public string Id { get; set; }
        public string? Self { get; set; }
    }

    public class AccoBadges
    {
        public string Id { get; set; }
        public string? Self { get; set; }
    }

    public class AccoThemes
    {
        public string Id { get; set; }
        public string? Self { get; set; }
    }

    public class AccoSpecialFeatures
    {
        public string Id { get; set; }
        public string? Self { get; set; }
    }

    public class DistrictLink
    {
        public string Id { get; set; }
        public string? Self { get; set; }
    }

    public class TourismAssociationLink
    {
        public string Id { get; set; }
        public string? Self { get; set; }
    }

    public class RegionLink
    {
        public string Id { get; set; }
        public string? Self { get; set; }
    }

    public class MunicipalityLink
    {
        public string Id { get; set; }
        public string? Self { get; set; }
    }

    public class AreaLink
    {
        public string Id { get; set; }
        public string? Self { get; set; }
    }

    public class SkiAreaLink
    {
        public string Id { get; set; }
        public string? Self { get; set; }
    }

    public class SkiRegionLink
    {
        public string Id { get; set; }
        public string? Self { get; set; }
    }

    public class CompanyLink
    {
        public string Id { get; set; }
        public string? Self { get; set; }
    }

    #endregion

    #region Linked Main Classes

    public class GastronomyLinked : Gastronomy, IMetaData, IGPSInfoAware, IGPSPointsAware
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "Gastronomy/" + Uri.EscapeDataString(this.Id) : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        //Overwriting Categorycodes etc...
        public new ICollection<CategoryCodesLinked>? CategoryCodes { get; set; }
        public new ICollection<DishRatesLinked>? DishRates { get; set; }
        public new ICollection<CapacityCeremonyLinked>? CapacityCeremony { get; set; }
        public new ICollection<FacilitiesLinked>? Facilities { get; set; }

        //Overwrites The LocationInfo
        public new LocationInfoLinked? LocationInfo { get; set; }

        public ICollection<GpsInfo> GpsInfo { get; set; }

        //Overwrite Latitude/Longitude/
        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? Gpstype
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Gpstype
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Latitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Latitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Longitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Longitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new Nullable<double> Altitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Altitude
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? AltitudeUnitofMeasure
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().AltitudeUnitofMeasure
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(); }
        }
    }

    public class AccommodationLinked
        : Accommodation,
            IMetaData,
            IGPSInfoAware,
            IGPSPointsAware,
            IHasLocationInfoLinked
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get
            {
                return this.Id != null ? "Accommodation/" + Uri.EscapeDataString(this.Id) : null;
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        //Taglist
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public AccoType? AccoType
        {
            get
            {
                return String.IsNullOrEmpty(this.AccoTypeId)
                    ? null
                    : new AccoType()
                    {
                        Id = this.AccoTypeId,
                        Self = "AccommodationTypes/" + Uri.EscapeDataString(this.AccoTypeId),
                    };
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public AccoCategory? AccoCategory
        {
            get
            {
                return String.IsNullOrEmpty(this.AccoCategoryId)
                    ? null
                    : new AccoCategory()
                    {
                        Id = this.AccoCategoryId,
                        Self = "AccommodationTypes/" + Uri.EscapeDataString(this.AccoCategoryId),
                    };
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<AccoBoards> AccoBoards
        {
            get
            {
                return this.BoardIds != null
                    ? this
                        .BoardIds.Select(x => new AccoBoards()
                        {
                            Id = x,
                            Self = "AccommodationTypes/" + x,
                        })
                        .ToList()
                    : new List<AccoBoards>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<AccoBadges> AccoBadges
        {
            get
            {
                return this.BadgeIds != null
                    ? this
                        .BadgeIds.Select(x => new AccoBadges()
                        {
                            Id = x,
                            Self = "AccommodationTypes/" + x,
                        })
                        .ToList()
                    : new List<AccoBadges>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<AccoThemes> AccoThemes
        {
            get
            {
                return this.ThemeIds != null
                    ? this
                        .ThemeIds.Select(x => new AccoThemes()
                        {
                            Id = x,
                            Self = "AccommodationTypes/" + x,
                        })
                        .ToList()
                    : new List<AccoThemes>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<AccoSpecialFeatures> AccoSpecialFeatures
        {
            get
            {
                return this.SpecialFeaturesIds != null
                    ? this
                        .SpecialFeaturesIds.Select(x => new AccoSpecialFeatures()
                        {
                            Id = x,
                            Self = "AccommodationTypes/" + x,
                        })
                        .ToList()
                    : new List<AccoSpecialFeatures>();
            }
        }

        //Overwrites The Features
        public new ICollection<AccoFeatureLinked>? Features { get; set; }

        //Overwrites The Features
        public new ICollection<AccoRoomInfoLinked>? AccoRoomInfo { get; set; }

        //Overwrites The LocationInfo
        public new LocationInfoLinked? LocationInfo { get; set; }

        public ICollection<GpsInfo>? GpsInfo { get; set; }

        //Overwrite Latitude/Longitude/
        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? Gpstype
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Gpstype
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Latitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Latitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Longitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Longitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new Nullable<double> Altitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Altitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? AltitudeUnitofMeasure
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().AltitudeUnitofMeasure
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(); }
        }
    }

    public class AccommodationRoomLinked : AccoRoom, IMetaData
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get
            {
                return this.Id != null
                    ? "AccommodationRoom/" + Uri.EscapeDataString(this.Id)
                    : null;
            }
        }

        //Overwrites The Features
        public new ICollection<AccoFeatureLinked>? Features { get; set; }
    }

    public class EventLinked
        : Event,
            IMetaData,
            IGPSInfoAware,
            IGPSPointsAware,
            IHasLocationInfoLinked,
            IHasTagInfo
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "Event/" + Uri.EscapeDataString(this.Id) : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        [SwaggerDeprecated("Obsolete")]
        public List<DateTime> EventDatesBegin
        {
            get
            {
                return this.EventDate != null ? this.EventDate.Select(x => x.From).ToList() : null;
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        [SwaggerDeprecated("Obsolete")]
        public List<DateTime> EventDatesEnd
        {
            get
            {
                return this.EventDate != null ? this.EventDate.Select(x => x.To).ToList() : null;
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        [SwaggerDeprecated("Obsolete")]
        public int EventDateCounter
        {
            get { return this.EventDate != null ? this.EventDate.Count : 0; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<DistrictLink> Districts
        {
            get
            {
                return this.DistrictIds != null
                    ? this
                        .DistrictIds.Select(x => new DistrictLink()
                        {
                            Id = x,
                            Self = "District/" + x,
                        })
                        .ToList()
                    : new List<DistrictLink>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        [SwaggerDeprecated("Deprecated use PublishedOn")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        [SwaggerDeprecated("Deprecated use Tags of type eventcategory")]
        public new ICollection<TopicLinked>? Topics { get; set; }

        //Overwrites The LocationInfo
        public new LocationInfoLinked? LocationInfo { get; set; }

        //Overwrites LTSTags
        //public new List<LTSTagsLinked>? LTSTags { get; set; }

        public ICollection<GpsInfo>? GpsInfo { get; set; }

        //Overwrite Latitude/Longitude/GpsType/Altitude/AltitudeUnitofMeasure and set it to obsolete and readonly

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? Gpstype
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Gpstype
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Latitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Latitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Longitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Longitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new Nullable<double> Altitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Altitude
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? AltitudeUnitofMeasure
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().AltitudeUnitofMeasure
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(); }
        }

        //New Tags
        public ICollection<Tags> Tags { get; set; }

        public ICollection<string> TagIds { get; set; }
    }

    public class VenueLinked : Venue, IMetaData, IHasLocationInfoLinked
    {
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "Venue/" + Uri.EscapeDataString(this.Id) : null; }
        }

        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        //Overwrites The LocationInfo
        public new LocationInfoLinked? LocationInfo { get; set; }
    }

    public class PackageLinked : Package, IMetaData
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "Package/" + Uri.EscapeDataString(this.Id) : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }
    }

    public class ODHActivityPoiLinked
        : ODHActivityPoi,
            IMetaData,
            IGPSInfoAware,
            IGPSPointsAware,
            IHasLocationInfoLinked,
            IHasTagInfo
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get
            {
                return this.Id != null ? "ODHActivityPoi/" + Uri.EscapeDataString(this.Id) : null;
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHActivityPoiTypesLink>? ODHActivityPoiTypes
        {
            get
            {
                var returnlist = new List<ODHActivityPoiTypesLink>();
                if (!String.IsNullOrEmpty(this.Type))
                    returnlist.Add(
                        new ODHActivityPoiTypesLink()
                        {
                            Id = this.Type,
                            Self = "OdhActivityPoiTypes/" + this.Type,
                            Type = "Type",
                        }
                    );
                if (!String.IsNullOrEmpty(this.SubType))
                    returnlist.Add(
                        new ODHActivityPoiTypesLink()
                        {
                            Id = this.SubType,
                            Self = "OdhActivityPoiTypes/" + this.SubType,
                            Type = "SubType",
                        }
                    );
                if (!String.IsNullOrEmpty(this.PoiType))
                    returnlist.Add(
                        new ODHActivityPoiTypesLink()
                        {
                            Id = this.PoiType,
                            Self = "OdhActivityPoiTypes/" + this.PoiType,
                            Type = "PoiType",
                        }
                    );

                return returnlist;
            }
        }

        //TO CHECK on GetObjectSingleAsync<T>() Areas is populated twice

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<AreaLink>? Areas
        {
            get
            {
                return this.AreaId != null
                    ? this
                        .AreaId.Select(x => new AreaLink() { Id = x, Self = "Area/" + x })
                        .ToList()
                    : new List<AreaLink>();
            }
        }

        public new ICollection<CategoryCodesLinked>? CategoryCodes { get; set; }
        public new ICollection<DishRatesLinked>? DishRates { get; set; }
        public new ICollection<CapacityCeremonyLinked>? CapacityCeremony { get; set; }
        public new ICollection<FacilitiesLinked>? Facilities { get; set; }

        //Overwrites The LocationInfo
        public new LocationInfoLinked? LocationInfo { get; set; }

        //Overwrites LTSTags
        public new List<LTSTagsLinked>? LTSTags { get; set; }
        public ICollection<string>? TagIds { get; set; }
    }

    public class LTSPoiLinked
        : PoiBaseInfos,
            IMetaData,
            IGPSInfoAware,
            IGPSPointsAware,
            IHasLocationInfoLinked
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "Poi/" + Uri.EscapeDataString(this.Id) : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "PoiTypes/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHActivityPoiTypesLink> PoiTypes
        {
            get
            {
                var returnlist = new List<ODHActivityPoiTypesLink>();
                returnlist.Add(
                    new ODHActivityPoiTypesLink()
                    {
                        Id = this.Type,
                        Self = "PoiTypes/" + Uri.EscapeDataString(this.Type),
                        Type = "Type",
                    }
                );
                if (
                    !String.IsNullOrEmpty(this.SubType)
                    && this.SubType != "no Subtype"
                    && this.SubType != "Essen Trinken"
                )
                    returnlist.Add(
                        new ODHActivityPoiTypesLink()
                        {
                            Id = this.SubType,
                            Self = "PoiTypes/" + Uri.EscapeDataString(this.SubType),
                            Type = "SubType",
                        }
                    );
                if (!String.IsNullOrEmpty(this.PoiType))
                    returnlist.Add(
                        new ODHActivityPoiTypesLink()
                        {
                            Id = this.PoiType,
                            Self = "PoiTypes/" + Uri.EscapeDataString(this.PoiType),
                            Type = "PoiType",
                        }
                    );

                return returnlist;
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<AreaLink> Areas
        {
            get
            {
                return this.AreaId != null
                    ? this
                        .AreaId.Select(x => new AreaLink() { Id = x, Self = "Area/" + x })
                        .ToList()
                    : new List<AreaLink>();
            }
        }

        //Overwrites The LocationInfo
        public new LocationInfoLinked? LocationInfo { get; set; }

        //Overwrites LTSTags
        public new List<LTSTagsLinked>? LTSTags { get; set; }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(true); }
        }
    }

    public class LTSActivityLinked
        : PoiBaseInfos,
            IMetaData,
            IGPSInfoAware,
            IGPSPointsAware,
            IHasLocationInfoLinked
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "Activity/" + Uri.EscapeDataString(this.Id) : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ActivityTypes/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHActivityPoiTypesLink> ActivityTypes
        {
            get
            {
                var returnlist = new List<ODHActivityPoiTypesLink>();
                returnlist.Add(
                    new ODHActivityPoiTypesLink()
                    {
                        Id = this.Type,
                        Self = "ActivityTypes/" + Uri.EscapeDataString(this.Type),
                        Type = "Type",
                    }
                );
                if (
                    !String.IsNullOrEmpty(this.SubType)
                    && this.SubType != "no Subtype"
                    && this.SubType != "Essen Trinken"
                )
                    returnlist.Add(
                        new ODHActivityPoiTypesLink()
                        {
                            Id = this.SubType,
                            Self = "ActivityTypes/" + Uri.EscapeDataString(this.SubType),
                            Type = "SubType",
                        }
                    );
                if (!String.IsNullOrEmpty(this.PoiType))
                    returnlist.Add(
                        new ODHActivityPoiTypesLink()
                        {
                            Id = this.PoiType,
                            Self = "ActivityTypes/" + Uri.EscapeDataString(this.PoiType),
                            Type = "PoiType",
                        }
                    );

                return returnlist;
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<AreaLink> Areas
        {
            get
            {
                return this.AreaId != null
                    ? this
                        .AreaId.Select(x => new AreaLink() { Id = x, Self = "Area/" + x })
                        .ToList()
                    : new List<AreaLink>();
            }
        }

        //Overwrites The LocationInfo
        public new LocationInfoLinked? LocationInfo { get; set; }

        //Overwrites LTSTags
        public new List<LTSTagsLinked>? LTSTags { get; set; }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(true); }
        }
    }

    public class ArticlesLinked : Article, IMetaData, IHasTagInfo
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "Article/" + Uri.EscapeDataString(this.Id) : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHActivityPoiTypesLink>? ArticleTypes
        {
            get
            {
                var returnlist = new List<ODHActivityPoiTypesLink>();
                if (!String.IsNullOrEmpty(this.Type))
                    returnlist.Add(
                        new ODHActivityPoiTypesLink()
                        {
                            Id = this.Type,
                            Self = "ArticleTypes/" + Uri.EscapeDataString(this.Type),
                            Type = "ArticleType",
                        }
                    );
                if (
                    !String.IsNullOrEmpty(this.SubType)
                    && this.SubType != "no Subtype"
                    && this.SubType != "Essen Trinken"
                )
                    returnlist.Add(
                        new ODHActivityPoiTypesLink()
                        {
                            Id = this.SubType,
                            Self = "ArticleTypes/" + Uri.EscapeDataString(this.SubType),
                            Type = "ArticleSubType",
                        }
                    );

                return returnlist;
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<string>? ArticleTypeList
        {
            get
            {
                var returnlist = new List<string>();
                returnlist.Add(this.Type);
                if (
                    !String.IsNullOrEmpty(this.SubType)
                    && this.SubType != "no Subtype"
                    && this.SubType != "Essen Trinken"
                )
                    returnlist.Add(this.SubType);

                return returnlist;
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        public ICollection<Tags>? Tags { get; set; }

        public ICollection<string>? TagIds { get; set; }
    }

    public class DistrictLinked : District, IMetaData, IGPSPointsAware, IGPSInfoAware
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "District/" + Uri.EscapeDataString(this.Id) : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public RegionLink Region
        {
            get
            {
                return String.IsNullOrEmpty(this.RegionId)
                    ? null
                    : new RegionLink()
                    {
                        Id = this.RegionId,
                        Self = "Region/" + Uri.EscapeDataString(this.RegionId),
                    };
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public MunicipalityLink Municipality
        {
            get
            {
                return String.IsNullOrEmpty(this.MunicipalityId)
                    ? null
                    : new MunicipalityLink()
                    {
                        Id = this.MunicipalityId,
                        Self = "Municipality/" + Uri.EscapeDataString(this.MunicipalityId),
                    };
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public TourismAssociationLink Tourismassociation
        {
            get
            {
                return String.IsNullOrEmpty(this.TourismvereinId)
                    ? null
                    : new TourismAssociationLink()
                    {
                        Id = this.TourismvereinId,
                        Self = "TourismAssociation/" + Uri.EscapeDataString(this.TourismvereinId),
                    };
            }
        }

        //GpsInfo
        public ICollection<GpsInfo> GpsInfo { get; set; }

        //Overwrite Latitude/Longitude/GpsType/Altitude/AltitudeUnitofMeasure and set it to obsolete and readonly
        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? Gpstype
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Gpstype
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Latitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Latitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Longitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Longitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new Nullable<double> Altitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Altitude
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? AltitudeUnitofMeasure
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().AltitudeUnitofMeasure
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(); }
        }
    }

    public class MunicipalityLinked : Municipality, IMetaData, IGPSPointsAware, IGPSInfoAware
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "Municipality/" + Uri.EscapeDataString(this.Id) : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public RegionLink Region
        {
            get
            {
                return String.IsNullOrEmpty(this.RegionId)
                    ? null
                    : new RegionLink()
                    {
                        Id = this.RegionId,
                        Self = "Region/" + Uri.EscapeDataString(this.RegionId),
                    };
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public TourismAssociationLink Tourismassociation
        {
            get
            {
                return String.IsNullOrEmpty(this.TourismvereinId)
                    ? null
                    : new TourismAssociationLink()
                    {
                        Id = this.TourismvereinId,
                        Self = "TourismAssociation/" + Uri.EscapeDataString(this.TourismvereinId),
                    };
            }
        }

        //GpsInfo
        public ICollection<GpsInfo> GpsInfo { get; set; }

        //Overwrite Latitude/Longitude/GpsType/Altitude/AltitudeUnitofMeasure and set it to obsolete and readonly
        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? Gpstype
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Gpstype
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Latitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Latitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Longitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Longitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new Nullable<double> Altitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Altitude
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? AltitudeUnitofMeasure
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().AltitudeUnitofMeasure
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(); }
        }
    }

    public class TourismvereinLinked : Tourismverein, IMetaData, IGPSPointsAware, IGPSInfoAware
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get
            {
                return this.Id != null
                    ? "TourismAssociation/" + Uri.EscapeDataString(this.Id)
                    : null;
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public RegionLink Region
        {
            get
            {
                return String.IsNullOrEmpty(this.RegionId)
                    ? null
                    : new RegionLink()
                    {
                        Id = this.RegionId,
                        Self = "Region/" + Uri.EscapeDataString(this.RegionId),
                    };
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<SkiAreaLink> SkiAreas
        {
            get
            {
                return this.SkiareaIds != null
                    ? this
                        .SkiareaIds.Select(x => new SkiAreaLink() { Id = x, Self = "SkiArea/" + x })
                        .ToList()
                    : new List<SkiAreaLink>();
            }
        }

        //GpsInfo
        public ICollection<GpsInfo> GpsInfo { get; set; }

        //Overwrite Latitude/Longitude/GpsType/Altitude/AltitudeUnitofMeasure and set it to obsolete and readonly
        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? Gpstype
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Gpstype
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Latitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Latitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Longitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Longitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new Nullable<double> Altitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Altitude
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? AltitudeUnitofMeasure
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().AltitudeUnitofMeasure
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(); }
        }
    }

    public class RegionLinked : Region, IMetaData, IGPSPointsAware, IGPSInfoAware
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "Region/" + Uri.EscapeDataString(this.Id) : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<SkiAreaLink> SkiAreas
        {
            get
            {
                return this.SkiareaIds != null
                    ? this
                        .SkiareaIds.Select(x => new SkiAreaLink() { Id = x, Self = "SkiArea/" + x })
                        .ToList()
                    : new List<SkiAreaLink>();
            }
        }

        //GpsInfo
        public ICollection<GpsInfo> GpsInfo { get; set; }

        //Overwrite Latitude/Longitude/GpsType/Altitude/AltitudeUnitofMeasure and set it to obsolete and readonly
        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? Gpstype
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Gpstype
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Latitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Latitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Longitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Longitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new Nullable<double> Altitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Altitude
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? AltitudeUnitofMeasure
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().AltitudeUnitofMeasure
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(); }
        }
    }

    public class MetaRegionLinked : MetaRegion, IMetaData, IGPSPointsAware, IGPSInfoAware
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "MetaRegion/" + Uri.EscapeDataString(this.Id) : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<DistrictLink> Districts
        {
            get
            {
                return this.DistrictIds != null
                    ? this
                        .DistrictIds.Select(x => new DistrictLink()
                        {
                            Id = x,
                            Self = "District/" + x,
                        })
                        .ToList()
                    : new List<DistrictLink>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<TourismAssociationLink> TourismAssociations
        {
            get
            {
                return this.TourismvereinIds != null
                    ? this
                        .TourismvereinIds.Select(x => new TourismAssociationLink()
                        {
                            Id = x,
                            Self = "TourismAssociation/" + x,
                        })
                        .ToList()
                    : new List<TourismAssociationLink>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<RegionLink> Regions
        {
            get
            {
                return this.RegionIds != null
                    ? this
                        .RegionIds.Select(x => new RegionLink() { Id = x, Self = "Region/" + x })
                        .ToList()
                    : new List<RegionLink>();
            }
        }

        //GpsInfo
        public ICollection<GpsInfo> GpsInfo { get; set; }

        //Overwrite Latitude/Longitude/GpsType/Altitude/AltitudeUnitofMeasure and set it to obsolete and readonly
        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? Gpstype
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Gpstype
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Latitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Latitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Longitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Longitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new Nullable<double> Altitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Altitude
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? AltitudeUnitofMeasure
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().AltitudeUnitofMeasure
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(); }
        }
    }

    public class ExperienceAreaLinked : ExperienceArea, IMetaData, IGPSPointsAware, IGPSInfoAware
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get
            {
                return this.Id != null ? "ExperienceArea/" + Uri.EscapeDataString(this.Id) : null;
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<TourismAssociationLink> TourismAssociations
        {
            get
            {
                return this.TourismvereinIds != null
                    ? this
                        .TourismvereinIds.Select(x => new TourismAssociationLink()
                        {
                            Id = x,
                            Self = "TourismAssociation/" + x,
                        })
                        .ToList()
                    : new List<TourismAssociationLink>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<DistrictLink> Districts
        {
            get
            {
                return this.DistrictIds != null
                    ? this
                        .DistrictIds.Select(x => new DistrictLink()
                        {
                            Id = x,
                            Self = "District/" + x,
                        })
                        .ToList()
                    : new List<DistrictLink>();
            }
        }

        //GpsInfo
        public ICollection<GpsInfo> GpsInfo { get; set; }

        //Overwrite Latitude/Longitude/GpsType/Altitude/AltitudeUnitofMeasure and set it to obsolete and readonly
        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? Gpstype
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Gpstype
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Latitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Latitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Longitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Longitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new Nullable<double> Altitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Altitude
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? AltitudeUnitofMeasure
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().AltitudeUnitofMeasure
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(); }
        }
    }

    public class AreaLinked : Area, IMetaData
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "Area/" + Uri.EscapeDataString(this.Id) : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public RegionLink Region
        {
            get
            {
                return String.IsNullOrEmpty(this.RegionId)
                    ? null
                    : new RegionLink()
                    {
                        Id = this.RegionId,
                        Self = "Region/" + Uri.EscapeDataString(this.RegionId),
                    };
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public MunicipalityLink Municipality
        {
            get
            {
                return String.IsNullOrEmpty(this.MunicipalityId)
                    ? null
                    : new MunicipalityLink()
                    {
                        Id = this.MunicipalityId,
                        Self = "Municipality/" + Uri.EscapeDataString(this.MunicipalityId),
                    };
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public TourismAssociationLink Tourismassociation
        {
            get
            {
                return String.IsNullOrEmpty(this.TourismvereinId)
                    ? null
                    : new TourismAssociationLink()
                    {
                        Id = this.TourismvereinId,
                        Self = "TourismAssociation/" + Uri.EscapeDataString(this.TourismvereinId),
                    };
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public SkiAreaLink SkiArea
        {
            get
            {
                return String.IsNullOrEmpty(this.SkiAreaID)
                    ? null
                    : new SkiAreaLink()
                    {
                        Id = this.SkiAreaID,
                        Self = "SkiArea/" + Uri.EscapeDataString(this.SkiAreaID),
                    };
            }
        }
    }

    public class SkiAreaLinked : SkiArea, IMetaData, IGPSPointsAware, IGPSInfoAware
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "SkiArea/" + Uri.EscapeDataString(this.Id) : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public SkiRegionLink SkiRegion
        {
            get
            {
                return String.IsNullOrEmpty(this.SkiRegionId)
                    ? null
                    : new SkiRegionLink()
                    {
                        Id = this.SkiRegionId,
                        Self = "SkiRegion/" + Uri.EscapeDataString(this.SkiRegionId),
                    };
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<AreaLink> Areas
        {
            get
            {
                return this.AreaId != null
                    ? this
                        .AreaId.Select(x => new AreaLink() { Id = x, Self = "Area/" + x })
                        .ToList()
                    : new List<AreaLink>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<TourismAssociationLink> TourismAssociations
        {
            get
            {
                return this.TourismvereinIds != null
                    ? this
                        .TourismvereinIds.Select(x => new TourismAssociationLink()
                        {
                            Id = x,
                            Self = "TourismAssociation/" + x,
                        })
                        .ToList()
                    : new List<TourismAssociationLink>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<RegionLink> Regions
        {
            get
            {
                return this.RegionIds != null
                    ? this
                        .RegionIds.Select(x => new RegionLink() { Id = x, Self = "Region/" + x })
                        .ToList()
                    : new List<RegionLink>();
            }
        }

        public new LocationInfoLinked? LocationInfo { get; set; }

        //GpsInfo
        public ICollection<GpsInfo> GpsInfo { get; set; }

        //Overwrite Latitude/Longitude/GpsType/Altitude/AltitudeUnitofMeasure and set it to obsolete and readonly
        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? Gpstype
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Gpstype
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Latitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Latitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Longitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Longitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new Nullable<double> Altitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Altitude
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? AltitudeUnitofMeasure
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().AltitudeUnitofMeasure
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(); }
        }
    }

    public class SkiRegionLinked : SkiRegion, IMetaData, IGPSPointsAware, IGPSInfoAware
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "SkiRegion/" + Uri.EscapeDataString(this.Id) : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return this.SmgActive; }
        }

        //GpsInfo
        public ICollection<GpsInfo> GpsInfo { get; set; }

        //Overwrite Latitude/Longitude/GpsType/Altitude/AltitudeUnitofMeasure and set it to obsolete and readonly
        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? Gpstype
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Gpstype
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Latitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Latitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Longitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Longitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new Nullable<double> Altitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Altitude
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? AltitudeUnitofMeasure
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().AltitudeUnitofMeasure
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(); }
        }
    }

    public class WebcamInfoLinked : WebcamInfo, IMetaData
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "WebcamInfo/" + Uri.EscapeDataString(this.Id) : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return (bool)this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<AreaLink> Areas
        {
            get
            {
                return this.AreaIds != null
                    ? this
                        .AreaIds.Select(x => new AreaLink() { Id = x, Self = "Area/" + x })
                        .ToList()
                    : new List<AreaLink>();
            }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.SmgTags != null
                    ? this
                        .SmgTags.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }
    }

    public class MeasuringpointLinked
        : Measuringpoint,
            IMetaData,
            IGPSPointsAware,
            IGPSInfoAware,
            IHasLocationInfoLinked
    {
        public Metadata? _Meta { get; set; }

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
        public bool OdhActive
        {
            get { return (bool)this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public ICollection<AreaLink> Areas
        {
            get
            {
                return this.AreaIds != null
                    ? this
                        .AreaIds.Select(x => new AreaLink() { Id = x, Self = "Area/" + x })
                        .ToList()
                    : new List<AreaLink>();
            }
        }

        //Overwrites The LocationInfo
        public new LocationInfoLinked? LocationInfo { get; set; }

        //GpsInfo
        public ICollection<GpsInfo> GpsInfo { get; set; }

        //Overwrite Latitude/Longitude/GpsType/Altitude/AltitudeUnitofMeasure and set it to obsolete and readonly
        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? Gpstype
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Gpstype
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Latitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Latitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new double Longitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Longitude
                    : 0;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new Nullable<double> Altitude
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().Altitude
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new string? AltitudeUnitofMeasure
        {
            get
            {
                return this.GpsInfo != null && this.GpsInfo.Count > 0
                    ? this.GpsInfo.FirstOrDefault().AltitudeUnitofMeasure
                    : null;
            }
        }

        [SwaggerDeprecated("Deprecated, use GpsInfo")]
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public IDictionary<string, GpsInfo> GpsPoints
        {
            get { return this.GpsInfo.ToGpsPointsDictionary(); }
        }
    }

    public class WineLinked : Wine, IMetaData
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "WineAward/" + Uri.EscapeDataString(this.Id) : null; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public bool OdhActive
        {
            get { return (bool)this.SmgActive; }
        }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public CompanyLink Company
        {
            get
            {
                return String.IsNullOrEmpty(this.CompanyId)
                    ? null
                    : new CompanyLink()
                    {
                        Id = this.CompanyId,
                        Self = "ODHActivityPoi/" + this.CompanyId,
                    };
            }
        }
    }

    public class EventShortLinked : EventShort, IMetaData, IHasTagInfo
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "EventShort/" + Uri.EscapeDataString(this.Id) : null; }
        }

        //Adding Tags also to EventShort
        //TODO move all Technologyfields + CustomTagging to Tags
        //TODO make Technologyfields & CustomTagging readonly
        public ICollection<Tags>? Tags { get; set; }

        public ICollection<string>? TagIds { get; set; }

        //Setting old TechnologyFields to readonly
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new List<string>? TechnologyFields
        {
            get
            {
                if (this.Tags != null)
                {
                    return this.Tags.Where(x => x.Type == "technologyfields").Count() > 0
                        ? this
                            .Tags.Where(x => x.Type == "technologyfields")
                            .Select(x => CompatibilityHelper.GetOldEventShortTagnames(x.Id))
                            .ToList()
                        : null;
                }
                else
                    return null;
            }
        }

        //Setting old CustomTagging to readonly
        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public new List<string>? CustomTagging
        {
            get
            {
                if (this.Tags != null)
                {
                    return this.Tags.Where(x => x.Type == "customtagging").Count() > 0
                        ? this
                            .Tags.Where(x => x.Type == "customtagging")
                            .Select(x => CompatibilityHelper.GetOldEventShortTagnames(x.Id))
                            .ToList()
                        : null;
                }
                else
                    return null;
            }
        }
    }

    public class WeatherLinked : Weather, IMetaData
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return "Weather/" + this.Id; }
        }
    }

    public class WeatherHistoryLinked : WeatherHistory, IMetaData
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return "WeatherHistory/" + this.Id; }
        }
    }

    public class WeatherDistrictLinked : BezirksWeather, IMetaData
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return "Weather/District/" + this.Id; }
        }
    }

    public class WeatherRealTimeLinked : WeatherRealTime, IMetaData
    {
        public Metadata? _Meta { get; set; }
    }

    public class WeatherForecastLinked : WeatherForecast, IMetaData
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return "Weather/Forecast/" + this.Id; }
        }

        public LocationInfoLinked LocationInfo { get; set; }
    }

    public class ODHTagLinked : SmgTags, IMetaData
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return "ODHTag/" + this.Id; }
        }
    }

    public class TagLinked : SmgTags, IMetaData, ISource, IActivateable
    {
        public TagLinked()
        {
            Description = new Dictionary<string, string>();
        }

        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return "Tag/" + this.Id; }
        }
        public List<string>? ODHTagIds { get; set; }

        public ICollection<string> Types { get; set; }
        public new string Source { get; set; }
        public bool Active { get; set; }

        public IDictionary<string, string> Description { get; set; }
    }

    public class PublisherLinked : Publisher, IMetaData
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "Publisher/" + Uri.EscapeDataString(this.Id) : null; }
        }
    }

    public class SourceLinked : Source, IMetaData
    {
        public Metadata? _Meta { get; set; }

        [SwaggerSchema(Description = "generated field", ReadOnly = true)]
        public string? Self
        {
            get { return this.Id != null ? "Source/" + Uri.EscapeDataString(this.Id) : null; }
        }
    }

    //TODO ADD Linked + Meta
    //EventTopics
    //ActivityTypes
    //PoiTypes
    //AccommodationTypes
    //ODHActivityPoiTypes
    //ODHTag
    //AccommodationFeatures
    //ArticleTypes
    //EventShort
    //GastronomyTypes
    //VenueTypes
    //Location??

    public class TourismMetaData
        : IMetaData,
            IImportDateassigneable,
            IIdentifiable,
            IPublishedOn,
            ILicenseInfo
    {
        //openapi
        //Procudes non nullable string not required
        //[Newtonsoft.Json.JsonProperty(Required = Newtonsoft.Json.Required.DisallowNull)]
        //Produces string non nullable minlength = 1
        //[Required]
        //Produces non nullable string required
        //[Newtonsoft.Json.JsonProperty(Required = Newtonsoft.Json.Required.Always)]

        public TourismMetaData()
        {
            ApiFilter = new List<string>();
        }

        public string? Id { get; set; }
        public string? Self
        {
            get { return "v1/MetaData/" + this.Id; }
        }

        public Metadata? _Meta { get; set; }
        public DateTime? FirstImport { get; set; }
        public DateTime? LastChange { get; set; }

        [SwaggerSchema("Base Url of the Api")]
        public string? BaseUrl { get; set; }

        [SwaggerSchema("optional Filter for the dataset")]
        public ICollection<string>? ApiFilter { get; set; }

        [Newtonsoft.Json.JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public ICollection<string> PathParam { get; set; }

        [SwaggerSchema("Full Api Url generated from BaseUrl, PathParam and ApiFilter")]
        public string ApiUrl
        {
            get
            {
                return String.Format(
                    "{0}/{1}{2}",
                    this.BaseUrl != null ? this.BaseUrl : "",
                    String.Join("/", this.PathParam),
                    this.ApiFilter != null && this.ApiFilter.Count > 0
                        ? "?" + String.Join("&", this.ApiFilter)
                        : ""
                );
                ;
            }
        }

        [SwaggerDeprecated("Obsolete use Type")]
        public string? OdhType { get; set; }

        [SwaggerSchema("Optional _Meta.Type of the dataset (valid for the content domain)")]
        public string? Type { get; set; }

        [SwaggerSchema("Swagger Url where the api call is described")]
        public string? SwaggerUrl { get; set; }

        public bool Deprecated { get; set; }

        [Newtonsoft.Json.JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public string Shortname { get; set; }

        [SwaggerSchema("Sources (_Meta.Source) of the data (valid for the content domain)")]
        public ICollection<string>? Sources { get; set; }

        public IDictionary<string, int>? RecordCount { get; set; }

        [SwaggerSchema("Content Types the Api returns")]
        public IDictionary<string, string>? Output { get; set; }

        [SwaggerSchema("Description of the dataset and api")]
        public IDictionary<string, string>? ApiDescription { get; set; }

        public ICollection<string>? PublishedOn { get; set; }

        public IDictionary<string, string>? ApiAccess { get; set; }

        public ICollection<ImageGallery>? ImageGallery { get; set; }

        public ICollection<string>? OdhTagIds { get; set; }

        public ICollection<ODHTags> ODHTags
        {
            get
            {
                return this.OdhTagIds != null
                    ? this
                        .OdhTagIds.Select(x => new ODHTags() { Id = x, Self = "ODHTag/" + x })
                        .ToList()
                    : new List<ODHTags>();
            }
        }

        [SwaggerSchema("Dataspace, semantic description of the domain the dataset belongs to")]
        public string? Dataspace { get; set; }

        public ICollection<string>? Category { get; set; }

        [SwaggerSchema("Data Provider of the dataset")]
        public ICollection<string>? DataProvider { get; set; }

        public LicenseInfo? LicenseInfo { get; set; }

        [SwaggerSchema("Technical Domain of the api, content/timeseries")]
        public string? ApiType { get; set; }
    }

    #endregion

    #region interfaces

    public interface IHasLocationInfoLinked
    {
        LocationInfoLinked? LocationInfo { get; set; }
    }

    #endregion
}
