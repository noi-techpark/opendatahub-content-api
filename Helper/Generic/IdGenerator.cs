// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper
{
    public class IdGenerator
    {
        /// <summary>
        /// Translates a ODH Type Object to the Type (Metadata) as String
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="odhtype"></param>
        /// <returns></returns>
        public static string GenerateIDFromType<T>(T odhtype)
        {
            return CreateGUID(GetIDStyle(odhtype));
        }

        public static void CheckIdFromType<T>(T odhtype)
            where T : IIdentifiable
        {
            var style = GetIDStyle(odhtype);

            if (style == IDStyle.uppercase)
                odhtype.Id = odhtype.Id.ToUpper();
            else if (style == IDStyle.lowercase)
                odhtype.Id = odhtype.Id.ToLower();
        }

        public static string CheckIdFromType<T>(string id)
            where T : IIdentifiable
        {
            var style = GetIDStyle(typeof(T));

            if (style == IDStyle.uppercase)
                return id.ToUpper();
            else if (style == IDStyle.lowercase)
                return id.ToLower();

            return id;
        }

        private static string CreateGUID(IDStyle style)
        {
            var id = System.Guid.NewGuid().ToString();

            if (style == IDStyle.uppercase)
                id = id.ToUpper();
            else if (style == IDStyle.lowercase)
                id = id.ToLower();

            return id;
        }

        public static IDStyle GetIDStyle<T>(T odhtype)
        {
            return odhtype switch
            {
                Accommodation or AccommodationLinked or AccommodationV2 => IDStyle.uppercase,
                AccoRoom or AccommodationRoomLinked => IDStyle.uppercase,
                //LTSActivityLinked => IDStyle.uppercase,
                //LTSPoiLinked => IDStyle.uppercase,
                //GastronomyLinked => IDStyle.uppercase,
                Event or EventLinked or EventFlattened => IDStyle.uppercase,
                ODHActivityPoi or ODHActivityPoiLinked => IDStyle.lowercase,
                Package or PackageLinked => IDStyle.uppercase,
                Measuringpoint or MeasuringpointLinked or MeasuringpointV2 => IDStyle.uppercase,
                WebcamInfo or WebcamInfoLinked => IDStyle.uppercase,
                Article or ArticlesLinked => IDStyle.uppercase,
                DDVenue => IDStyle.uppercase,
                Venue or VenueLinked or VenueV2 or VenueFlattened => IDStyle.uppercase,
                EventShort or EventShortLinked => IDStyle.lowercase,
                ExperienceArea or ExperienceAreaLinked => IDStyle.uppercase,
                MetaRegion or MetaRegionLinked => IDStyle.uppercase,
                Region or RegionLinked => IDStyle.uppercase,
                Tourismverein or TourismvereinLinked => IDStyle.uppercase,
                Municipality or MunicipalityLinked => IDStyle.uppercase,
                District or DistrictLinked => IDStyle.uppercase,
                SkiArea or SkiAreaLinked => IDStyle.uppercase,
                SkiRegion or SkiRegionLinked => IDStyle.uppercase,
                Area or AreaLinked => IDStyle.uppercase,
                Wine or WineLinked => IDStyle.lowercase,
                ODHTagLinked => IDStyle.lowercase,
                Publisher or PublisherLinked => IDStyle.lowercase,
                Source or SourceLinked => IDStyle.lowercase,
                TourismMetaData => IDStyle.lowercase,
                TagLinked => IDStyle.mixed,
                WeatherHistoryLinked => IDStyle.mixed,
                Announcement => IDStyle.lowercase,
                UrbanGreen => IDStyle.lowercase,
                Trip => IDStyle.lowercase,
                SpatialData => IDStyle.lowercase,
                _ => throw new Exception("not known odh type"),
            };
        }

        public static IDStyle GetIDStyle(Type odhtype)
        {
            return odhtype switch
            {
                Type _
                    when odhtype == typeof(Accommodation)
                        || odhtype == typeof(AccommodationLinked)
                        || odhtype == typeof(AccommodationV2) => IDStyle.uppercase,
                Type _
                    when odhtype == typeof(AccoRoom)
                        || odhtype == typeof(AccommodationRoomLinked) => IDStyle.uppercase,
                //Type _ when odhtype == typeof(LTSActivityLinked) => IDStyle.uppercase,
                //Type _ when odhtype == typeof(LTSPoiLinked) => IDStyle.uppercase,
                //Type _ when odhtype == typeof(GastronomyLinked) => IDStyle.uppercase,
                Type _ when odhtype == typeof(Event) || odhtype == typeof(EventLinked) =>
                    IDStyle.uppercase,
                Type _
                    when odhtype == typeof(ODHActivityPoi)
                        || odhtype == typeof(ODHActivityPoiLinked) => IDStyle.lowercase,
                Type _ when odhtype == typeof(Package) || odhtype == typeof(PackageLinked) =>
                    IDStyle.uppercase,
                Type _
                    when odhtype == typeof(Measuringpoint) || odhtype == typeof(MeasuringpointLinked)
                        || odhtype == typeof(MeasuringpointV2) => IDStyle.uppercase,
                Type _ when odhtype == typeof(WebcamInfo) || odhtype == typeof(WebcamInfoLinked) =>
                    IDStyle.uppercase,
                Type _ when odhtype == typeof(Article) || odhtype == typeof(ArticlesLinked) =>
                    IDStyle.uppercase,
                Type _ when odhtype == typeof(DDVenue) => IDStyle.uppercase,
                Type _ when odhtype == typeof(Venue) || odhtype == typeof(VenueLinked) || odhtype == typeof(VenueFlattened) || odhtype == typeof(VenueV2) =>
                    IDStyle.uppercase,
                Type _ when odhtype == typeof(EventShort) || odhtype == typeof(EventShortLinked) =>
                    IDStyle.lowercase,
                Type _
                    when odhtype == typeof(ExperienceArea)
                        || odhtype == typeof(ExperienceAreaLinked) => IDStyle.uppercase,
                Type _ when odhtype == typeof(MetaRegion) || odhtype == typeof(MetaRegionLinked) =>
                    IDStyle.uppercase,
                Type _ when odhtype == typeof(Region) || odhtype == typeof(RegionLinked) =>
                    IDStyle.uppercase,
                Type _
                    when odhtype == typeof(Tourismverein)
                        || odhtype == typeof(TourismvereinLinked) => IDStyle.uppercase,
                Type _
                    when odhtype == typeof(Municipality) || odhtype == typeof(MunicipalityLinked) =>
                    IDStyle.uppercase,
                Type _ when odhtype == typeof(District) || odhtype == typeof(DistrictLinked) =>
                    IDStyle.uppercase,
                Type _ when odhtype == typeof(SkiArea) || odhtype == typeof(SkiAreaLinked) =>
                    IDStyle.uppercase,
                Type _ when odhtype == typeof(SkiRegion) || odhtype == typeof(SkiRegionLinked) =>
                    IDStyle.uppercase,
                Type _ when odhtype == typeof(Area) || odhtype == typeof(AreaLinked) =>
                    IDStyle.uppercase,
                Type _ when odhtype == typeof(Wine) || odhtype == typeof(WineLinked) =>
                    IDStyle.lowercase,
                Type _ when odhtype == typeof(ODHTagLinked) => IDStyle.lowercase,
                Type _ when odhtype == typeof(TagLinked) => IDStyle.mixed,
                Type _ when odhtype == typeof(Publisher) || odhtype == typeof(PublisherLinked) =>
                    IDStyle.lowercase,
                Type _ when odhtype == typeof(Source) || odhtype == typeof(SourceLinked) =>
                    IDStyle.lowercase,
                Type _ when odhtype == typeof(TourismMetaData) => IDStyle.lowercase,
                Type _ when odhtype == typeof(WeatherHistoryLinked) => IDStyle.mixed,
                Type _ when odhtype == typeof(Announcement) => IDStyle.lowercase,
                Type _ when odhtype == typeof(UrbanGreen) => IDStyle.lowercase,
                Type _ when odhtype == typeof(Trip) => IDStyle.lowercase,
                Type _ when odhtype == typeof(SpatialData) => IDStyle.lowercase,
                _ => throw new Exception("not known odh type"),
            };
        }

        public static string TransformIDbyIdStyle(string id, IDStyle idstyle)
        {
            if (idstyle == IDStyle.uppercase)
                return id.ToUpper();
            else if (idstyle == IDStyle.lowercase)
                return id.ToLower();
            else
                return id;
        }
    }

    public enum IDStyle
    {
        uppercase,
        lowercase,
        mixed,
    }
}
