// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataModel;
using Microsoft.AspNetCore.Identity;

namespace Helper
{
    public class MetadataHelper
    {
        //Simple Method to reset the Metainfo
        public static Metadata GetMetadata(
            string id,
            string type,
            string? source,
            DateTime? lastupdated = null,
            bool reduced = false
        )
        {
            return new Metadata()
            {
                Id = id,
                Type = type,
                LastUpdate = lastupdated,
                Source = source,
                Reduced = reduced,
            };
        }

        public static Metadata GetMetadata<T>(
            T data,
            string source,
            //DateTime? lastupdated = null,
            bool reduced = false
        )
            where T : IIdentifiable, IMetaData
        {
            string type = ODHTypeHelper.TranslateType2TypeString<T>(data);

            //If source is already set use the old source
            //if (data._Meta != null && !string.IsNullOrEmpty(data._Meta.Source))
            //    source = data._Meta.Source;

            return new Metadata()
            {
                Id = data.Id,
                Type = type,
                //LastUpdate = lastupdated,
                LastUpdate = DateTime.Now,
                Source = source,
                Reduced = reduced,
            };
        }

        public static Metadata GetMetadataobject<T>(T myobject, Func<T, Metadata> metadataganerator)
        {
            return metadataganerator(myobject);
        }

        //Discriminator if the function is not passed
        public static Metadata GetMetadataobject<T>(T myobject, bool reduced = false)
        {
            return myobject switch
            {
                AccommodationV2 al => GetMetadataforAccommodation(al),
                AccommodationRoomLinked al => GetMetadataforAccommodationRoom(al),
                LTSActivityLinked ltsal => GetMetadataforActivity(ltsal),
                LTSPoiLinked ltspl => GetMetadataforPoi(ltspl),
                GastronomyLinked gl => GetMetadataforGastronomy(gl),
                EventLinked el => GetMetadataforEvent(el, reduced),
                ODHActivityPoiLinked odhapl => GetMetadataforOdhActivityPoi(odhapl),
                PackageLinked pl => GetMetadataforPackage(pl),
                MeasuringpointLinked ml => GetMetadataforMeasuringpoint(ml),
                WebcamInfoLinked wil => GetMetadataforWebcam(wil),
                ArticlesLinked al => GetMetadataforArticle(al),
                DDVenue ddv => GetMetadataforDDVenue(ddv),
                VenueLinked vl => GetMetadataforVenue(vl),
                EventShortLinked esl => GetMetadataforEventShort(esl),
                ExperienceAreaLinked eal => GetMetadataforExperienceArea(eal),
                MetaRegionLinked mrl => GetMetadataforMetaRegion(mrl),
                RegionLinked rl => GetMetadataforRegion(rl),
                TourismvereinLinked tvl => GetMetadataforTourismverein(tvl),
                MunicipalityLinked ml => GetMetadataforMunicipality(ml),
                DistrictLinked dl => GetMetadataforDistrict(dl),
                SkiAreaLinked sal => GetMetadataforSkiArea(sal),
                SkiRegionLinked srl => GetMetadataforSkiRegion(srl),
                AreaLinked al => GetMetadataforArea(al),
                WineLinked wl => GetMetadataforWineAward(wl),
                ODHTagLinked odhtl => GetMetadataforOdhTag(odhtl),
                TagLinked tgl => GetMetadataforTag(tgl),
                PublisherLinked pbl => GetMetadataforPublisher(pbl),
                SourceLinked pbl => GetMetadataforSource(pbl),
                WeatherHistoryLinked wh => GetMetaDataForWeatherHistory(wh),
                WeatherLinked we => GetMetaDataForWeather(we),
                WeatherDistrictLinked wd => GetMetaDataForWeatherDistrict(wd),
                WeatherRealTimeLinked wr => GetMetaDataForWeatherRealTime(wr),
                WeatherForecastLinked wf => GetMetaDataForWeatherForecast(wf),
                SnowReportBaseData sb => GetMetaDataForSnowReport(sb),
                TourismMetaData tm => GetMetaDataForMetaData(tm),
                EventV2 ev => GetMetadataforEvent(ev),
                VenueV2 ev => GetMetadataforVenue(ev),
                GeoShapeJson gj => GetMetadataForGeoShapeJson(gj),
                GeoShapeJsonTest gj => GetMetadataForGeoShapeJson(gj),
                _ => throw new Exception("not known odh type"),
            };
        }

        public static Metadata GetMetadataforAccommodation(AccommodationV2 data)
        {
            bool reduced = false;
            if (data._Meta != null)
                reduced = (bool)data._Meta.Reduced;

            //fix if source is null
            string? datasource = data.Source;

            if (datasource == null)
            {
                datasource = "unknown";
            }
            else
            {
                datasource = datasource.ToLower();
            }

            return GetMetadata(data, datasource, reduced);
        }

        public static Metadata GetMetadataforAccommodationRoom(AccommodationRoomLinked data)
        {
            //fix if source is null
            string? datasource = data.Source;

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

            return GetMetadata(data, datasource, false);
        }

        public static Metadata GetMetadataforActivity(LTSActivityLinked data)
        {
            bool reduced = false;
            if (data._Meta != null)
                reduced = (bool)data._Meta.Reduced;

            var sourcemeta = "lts";
            if (data.Source != null)
                sourcemeta = data.Source.ToLower();

            return GetMetadata(data, sourcemeta, reduced);
        }

        public static Metadata GetMetadataforPoi(LTSPoiLinked data)
        {
            bool reduced = false;
            if (data._Meta != null)
                reduced = (bool)data._Meta.Reduced;

            return GetMetadata(data, "lts", reduced);
        }

        public static Metadata GetMetadataforGastronomy(GastronomyLinked data)
        {
            bool reduced = false;
            if (data._Meta != null)
                reduced = (bool)data._Meta.Reduced;

            return GetMetadata(data, "lts", reduced);
        }

        public static Metadata GetMetadataforEvent(EventLinked data, bool reduced = false)
        {
            string sourcemeta = data.Source.ToLower();
            
            //if (data._Meta != null)
            //    reduced = (bool)data._Meta.Reduced;

            return GetMetadata(data, sourcemeta, reduced);
        }

        public static Metadata GetMetadataforEvent(EventV2 data)
        {
            string sourcemeta = data.Source.ToLower();
            bool reduced = false;
            if (data._Meta != null)
                reduced = (bool)data._Meta.Reduced;

            return GetMetadata(data, sourcemeta, reduced);
        }

        public static Metadata GetMetadataforOdhActivityPoi(ODHActivityPoiLinked data)
        {
            string? sourcemeta = data.Source?.ToLower();

            if (sourcemeta == "common" || sourcemeta == "magnolia" || sourcemeta == "content")
                sourcemeta = "idm";

            bool reduced = false;
            if (data._Meta != null)
                reduced = (bool)data._Meta.Reduced;

            return GetMetadata(data, sourcemeta ?? "", reduced);
        }

        public static Metadata GetMetadataforOdhTag(ODHTagLinked data)
        {
            string sourcemeta = "idm";

            if (data.Source != null && data.Source.Contains("LTSCategory"))
                sourcemeta = "lts";

            return GetMetadata(data, sourcemeta);
        }

        public static Metadata GetMetadataforTag(TagLinked data)
        {
            string sourcemeta = data.Source.ToLower();

            return GetMetadata(data, sourcemeta);
        }

        public static Metadata GetMetadataforPublisher(PublisherLinked data)
        {
            string sourcemeta = "noi";

            return GetMetadata(data, sourcemeta);
        }

        public static Metadata GetMetadataforSource(SourceLinked data)
        {
            string sourcemeta = "noi";

            return GetMetadata(data, sourcemeta);
        }

        public static Metadata GetMetadataforPackage(PackageLinked data)
        {
            return GetMetadata(data, "hgv", false);
        }

        public static Metadata GetMetadataforMeasuringpoint(MeasuringpointLinked data)
        {
            bool reduced = false;
            if (data._Meta != null)
                reduced = (bool)data._Meta.Reduced;

            return GetMetadata(data, "lts", reduced);
        }

        public static Metadata GetMetadataforWebcam(WebcamInfoLinked data)
        {
            string sourcemeta = data.Source?.ToLower() ?? "";
            if (sourcemeta == "content")
                sourcemeta = "idm";

            bool reduced = false;
            if (data._Meta != null)
                reduced = (bool)data._Meta.Reduced;

            return GetMetadata(data, sourcemeta, reduced);
        }

        public static Metadata GetMetadataforArticle(ArticlesLinked data)
        {
            string sourcemeta = "noi";

            if (!String.IsNullOrEmpty(data.Source))
                sourcemeta = data.Source.ToLower();

            if (sourcemeta == "content")
                sourcemeta = "idm";

            return GetMetadata(data, sourcemeta, false);
        }

        public static Metadata GetMetadataforDDVenue(DDVenue data)
        {
            bool reduced = false;
            if (data._Meta != null)
                reduced = (bool)data._Meta.Reduced;

            return data._Meta = GetMetadata(data, "lts", reduced);
        }

        public static Metadata GetMetadataforVenue(VenueLinked data)
        {
            bool reduced = false;
            if (data._Meta != null)
                reduced = (bool)data._Meta.Reduced;

            return data._Meta = GetMetadata(data, "lts", reduced);
        }

        public static Metadata GetMetadataforVenue(VenueV2 data)
        {
            bool reduced = false;
            if (data._Meta != null)
                reduced = (bool)data._Meta.Reduced;

            var sourcemeta = "lts";
            if (data.Source != null)
                sourcemeta = data.Source.ToLower();

            return data._Meta = GetMetadata(data, sourcemeta, reduced);
        }

        public static Metadata GetMetadataforEventShort(EventShortLinked data)
        {
            string sourcemeta = data.Source != null ? data.Source.ToLower() : "noi";

            string sourcestr = sourcemeta switch
            {
                "content" => "noi",
                "ebms" => "eurac",
                "nobis" => "nobis",
                _ => sourcemeta,
            };
            return GetMetadata(data, sourcestr, false);
        }

        public static Metadata GetMetadataforExperienceArea(ExperienceAreaLinked data)
        {
            return GetMetadata(data, "idm", false);
        }

        public static Metadata GetMetadataforMetaRegion(MetaRegionLinked data)
        {
            return GetMetadata(data, "idm", false);
        }

        public static Metadata GetMetadataforRegion(RegionLinked data)
        {
            return GetMetadata(data, "idm", false);
        }

        public static Metadata GetMetadataforTourismverein(TourismvereinLinked data)
        {
            return GetMetadata(data, "idm", false);
        }

        public static Metadata GetMetadataforMunicipality(MunicipalityLinked data)
        {
            return GetMetadata(data, "idm", false);
        }

        public static Metadata GetMetadataforDistrict(DistrictLinked data)
        {
            return GetMetadata(data, "idm", false);
        }

        public static Metadata GetMetadataforSkiArea(SkiAreaLinked data)
        {
            return GetMetadata(data, "idm", false);
        }

        public static Metadata GetMetadataforSkiRegion(SkiRegionLinked data)
        {
            return GetMetadata(data, "idm",  false);
        }

        public static Metadata GetMetadataforArea(AreaLinked data)
        {
            return GetMetadata(data, "lts", false);
        }

        public static Metadata GetMetadataforWineAward(WineLinked data)
        {
            return GetMetadata(data, "suedtirolwein", false);
        }

        public static Metadata GetMetaDataForWeatherHistory(WeatherHistoryLinked data)
        {
            return GetMetadata(data, "siag", false);
        }

        //Hack because WeatherLinked is not IIdentifiable so return directly
        public static Metadata GetMetaDataForWeather(WeatherLinked data)
        {
            string type = ODHTypeHelper.TranslateType2TypeString<WeatherLinked>(data);

            return new Metadata()
            {
                Id = data.Id.ToString(),
                Type = type,
                LastUpdate = data.Date,
                Source = "siag",
                Reduced = false,
            };
        }

        //Hack because WeatherLinked is not IIdentifiable so return directly
        public static Metadata GetMetaDataForWeatherDistrict(WeatherDistrictLinked data)
        {
            string type = ODHTypeHelper.TranslateType2TypeString<WeatherDistrictLinked>(data);

            return new Metadata()
            {
                Id = data.Id.ToString(),
                Type = type,
                LastUpdate = data.Date,
                Source = "siag",
                Reduced = false,
            };
        }

        public static Metadata GetMetaDataForWeatherRealTime(WeatherRealTimeLinked data)
        {
            string type = ODHTypeHelper.TranslateType2TypeString<WeatherRealTimeLinked>(data);

            return new Metadata()
            {
                Id = data.Id.ToString(),
                Type = type,
                LastUpdate = data.lastUpdated,
                Source = "siag",
                Reduced = false,
            };
        }

        public static Metadata GetMetaDataForWeatherForecast(WeatherForecastLinked data)
        {
            string type = ODHTypeHelper.TranslateType2TypeString<WeatherForecastLinked>(data);

            return new Metadata()
            {
                Id = data.Id.ToString(),
                Type = type,
                LastUpdate = data.Date,
                Source = "siag",
                Reduced = false,
            };
        }

        public static Metadata GetMetaDataForSnowReport(SnowReportBaseData data)
        {
            string type = ODHTypeHelper.TranslateType2TypeString<SnowReportBaseData>(data);

            return new Metadata()
            {
                Id = data.Id.ToString(),
                Type = type,
                LastUpdate = data.LastUpdate,
                Source = "lts",
                Reduced = false,
            };
        }

        
        public static Metadata GetMetadataForGeoShapeJson(GeoShapeJson data)
        {
            string type = ODHTypeHelper.TranslateType2TypeString<GeoShapeJson>(data);

            return new Metadata()
            {
                Id = data.Id.ToString(),
                Type = type,
                LastUpdate = DateTime.Now,
                Source = data.Source,
                Reduced = false,
            };
        }

        public static Metadata GetMetadataForGeoShapeJson(GeoShapeJsonTest data)
        {
            string type = ODHTypeHelper.TranslateType2TypeString<GeoShapeJsonTest>(data);

            return new Metadata()
            {
                Id = data.Id.ToString(),
                Type = type,
                LastUpdate = DateTime.Now,
                Source = data.Source,
                Reduced = false,
            };
        }

        public static Metadata GetMetaDataForMetaData(TourismMetaData data)
        {
            return GetMetadata(data, "noi", false);
        }


        public static void SetUpdateHistory(Metadata? oldmetadata, Metadata newmetadata)
        {
            if (oldmetadata == null && newmetadata.UpdateInfo != null && !String.IsNullOrEmpty(newmetadata.UpdateInfo.UpdatedBy))
            {
                //New dataset
                newmetadata.UpdateInfo.UpdateHistory =
                [
                    new UpdateHistory() { LastUpdate = newmetadata.LastUpdate, UpdateSource = newmetadata.UpdateInfo.UpdateSource, UpdatedBy = newmetadata.UpdateInfo.UpdatedBy },
                ];
            }
            else if (oldmetadata != null)
            {
                //Compatibility Update Info not present
                if (oldmetadata.UpdateInfo == null)
                    newmetadata.UpdateInfo = new UpdateInfo();

                //Compatibility UpdateHistory not present
                if (oldmetadata.UpdateInfo.UpdateHistory == null)
                    newmetadata.UpdateInfo.UpdateHistory = new List<UpdateHistory>();
                else
                    newmetadata.UpdateInfo.UpdateHistory = oldmetadata.UpdateInfo.UpdateHistory;

                if (!String.IsNullOrEmpty(newmetadata.UpdateInfo.UpdatedBy))
                {
                    if(newmetadata.UpdateInfo.UpdateHistory.Where(x => x.UpdatedBy == newmetadata.UpdateInfo.UpdatedBy && x.UpdateSource == newmetadata.UpdateInfo.UpdateSource).Count() > 0)
                    {
                        var updatehistorytoupdate = newmetadata.UpdateInfo.UpdateHistory.Where(x => x.UpdatedBy == newmetadata.UpdateInfo.UpdatedBy && x.UpdateSource == newmetadata.UpdateInfo.UpdateSource).FirstOrDefault();
                        updatehistorytoupdate.LastUpdate = newmetadata.LastUpdate;
                    }
                    else
                    {
                        newmetadata.UpdateInfo.UpdateHistory.Add(new UpdateHistory() { LastUpdate = newmetadata.LastUpdate, UpdateSource = newmetadata.UpdateInfo.UpdateSource, UpdatedBy = newmetadata.UpdateInfo.UpdatedBy });
                    }
                }
                    //newmetadata.UpdateInfo.UpdateHistory.  TryAddOrUpdate(, );
            }
            else
                newmetadata.UpdateInfo.UpdateHistory = null;
        }
    }
}
