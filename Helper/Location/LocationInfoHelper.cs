// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataModel;
using Microsoft.AspNetCore.Mvc.Formatters;
using SqlKata.Execution;

namespace Helper.Location
{
    public static class LocationInfoHelper
    {
        /// <summary>
        /// Recreate LocationInfo by passing a complete list of Regions/TVs/Municipalities/Districts
        /// </summary>
        /// <param name="oldlocationinfo"></param>
        /// <param name="locationlist"></param>
        /// <returns></returns>
        public static LocationInfoLinked UpdateLocationInfo(
            LocationInfoLinked oldlocationinfo,
            IEnumerable<LocHelperclassDynamic> locationlist
        )
        {
            LocationInfoLinked locationInfoLinked = new LocationInfoLinked();

            //Updating Region
            if (
                oldlocationinfo.RegionInfo != null
                && !String.IsNullOrEmpty(oldlocationinfo.RegionInfo.Id)
            )
            {
                locationInfoLinked.RegionInfo = oldlocationinfo.RegionInfo;
                var regioninfoname = locationlist
                    .Where(x => x.id == oldlocationinfo.RegionInfo.Id)
                    .FirstOrDefault();
                if (regioninfoname != null)
                    locationInfoLinked.RegionInfo.Name = regioninfoname.name;
            }

            //Updating TV
            if (oldlocationinfo.TvInfo != null && !String.IsNullOrEmpty(oldlocationinfo.TvInfo.Id))
            {
                locationInfoLinked.TvInfo = oldlocationinfo.TvInfo;
                var tvinfoname = locationlist
                    .Where(x => x.id == oldlocationinfo.TvInfo.Id)
                    .FirstOrDefault();
                if (tvinfoname != null)
                    locationInfoLinked.TvInfo.Name = tvinfoname.name;
            }

            //Updating Municipality
            if (
                oldlocationinfo.MunicipalityInfo != null
                && !String.IsNullOrEmpty(oldlocationinfo.MunicipalityInfo.Id)
            )
            {
                locationInfoLinked.MunicipalityInfo = oldlocationinfo.MunicipalityInfo;
                var municipalityinfoname = locationlist
                    .Where(x => x.id == oldlocationinfo.MunicipalityInfo.Id)
                    .FirstOrDefault();
                if (municipalityinfoname != null)
                    locationInfoLinked.MunicipalityInfo.Name = municipalityinfoname.name;
            }

            //Updating District
            if (
                oldlocationinfo.DistrictInfo != null
                && !String.IsNullOrEmpty(oldlocationinfo.DistrictInfo.Id)
            )
            {
                locationInfoLinked.DistrictInfo = oldlocationinfo.DistrictInfo;
                var districtinfoname = locationlist
                    .Where(x => x.id == oldlocationinfo.DistrictInfo.Id)
                    .FirstOrDefault();
                if (districtinfoname != null)
                    locationInfoLinked.DistrictInfo.Name = districtinfoname.name;
            }

            //Updating Area
            if (
                oldlocationinfo.AreaInfo != null
                && !String.IsNullOrEmpty(oldlocationinfo.AreaInfo.Id)
            )
            {
                locationInfoLinked.AreaInfo = oldlocationinfo.AreaInfo;
                var areainfoname = locationlist
                    .Where(x => x.id == oldlocationinfo.AreaInfo.Id)
                    .FirstOrDefault();
                if (areainfoname != null)
                    locationInfoLinked.AreaInfo.Name = areainfoname.name;
            }

            return locationInfoLinked;
        }

        /// <summary>
        /// Recreate LocationInfo by passing queryFactory and querying the DB for each
        /// </summary>
        /// <param name="oldlocationinfo"></param>
        /// <param name="queryFactory"></param>
        /// <returns></returns>
        public static async Task<LocationInfoLinked> UpdateLocationInfo(
            LocationInfoLinked? oldlocationinfo,
            QueryFactory queryFactory
        )
        {
            try
            {
                if (oldlocationinfo != null)
                {
                    LocationInfoLinked locationInfoLinked = new LocationInfoLinked();

                    //Updating Region
                    if (
                        oldlocationinfo.RegionInfo != null
                        && !String.IsNullOrEmpty(oldlocationinfo.RegionInfo.Id)
                    )
                    {
                        locationInfoLinked.RegionInfo = oldlocationinfo.RegionInfo;

                        var regioninfo = await LocationListCreator.GetLocationFromDB<Region>(
                            queryFactory,
                            "regions",
                            Tuple.Create("id", oldlocationinfo.RegionInfo.Id)
                        );

                        if (regioninfo != null && regioninfo.Count() > 0)
                        {
                            var regioninfoname = regioninfo
                                .FirstOrDefault()
                                .Detail.ToDictionary(x => x.Key, x => x.Value.Title);
                            if (regioninfoname != null)
                                locationInfoLinked.RegionInfo.Name = regioninfoname;
                        }
                    }

                    //Updating TV
                    if (
                        oldlocationinfo.TvInfo != null
                        && !String.IsNullOrEmpty(oldlocationinfo.TvInfo.Id)
                    )
                    {
                        locationInfoLinked.TvInfo = oldlocationinfo.TvInfo;

                        var tvinfo = await LocationListCreator.GetLocationFromDB<Tourismverein>(
                            queryFactory,
                            "tvs",
                            Tuple.Create("id", oldlocationinfo.TvInfo.Id)
                        );

                        if (tvinfo != null && tvinfo.Count() > 0)
                        {
                            var tvinfoname = tvinfo
                                .FirstOrDefault()
                                .Detail.ToDictionary(x => x.Key, x => x.Value.Title);
                            if (tvinfoname != null)
                                locationInfoLinked.TvInfo.Name = tvinfoname;
                        }
                    }

                    //Updating Municipality
                    if (
                        oldlocationinfo.MunicipalityInfo != null
                        && !String.IsNullOrEmpty(oldlocationinfo.MunicipalityInfo.Id)
                    )
                    {
                        locationInfoLinked.MunicipalityInfo = oldlocationinfo.MunicipalityInfo;

                        var muninfo = await LocationListCreator.GetLocationFromDB<Municipality>(
                            queryFactory,
                            "municipalities",
                            Tuple.Create("id", oldlocationinfo.MunicipalityInfo.Id)
                        );

                        if (muninfo != null && muninfo.Count() > 0)
                        {
                            var municipalityinfoname = muninfo
                                .FirstOrDefault()
                                .Detail.ToDictionary(x => x.Key, x => x.Value.Title);
                            if (municipalityinfoname != null)
                                locationInfoLinked.MunicipalityInfo.Name = municipalityinfoname;
                        }
                    }

                    //Updating District
                    if (
                        oldlocationinfo.DistrictInfo != null
                        && !String.IsNullOrEmpty(oldlocationinfo.DistrictInfo.Id)
                    )
                    {
                        locationInfoLinked.DistrictInfo = oldlocationinfo.DistrictInfo;

                        var distinfo = await LocationListCreator.GetLocationFromDB<Tourismverein>(
                            queryFactory,
                            "districts",
                            Tuple.Create("id", oldlocationinfo.DistrictInfo.Id)
                        );

                        if (distinfo != null && distinfo.Count() > 0)
                        {
                            var districtinfoname = distinfo
                                .FirstOrDefault()
                                .Detail.ToDictionary(x => x.Key, x => x.Value.Title);
                            if (districtinfoname != null)
                                locationInfoLinked.DistrictInfo.Name = districtinfoname;
                        }
                    }

                    //Updating Area
                    if (
                        oldlocationinfo.AreaInfo != null
                        && !String.IsNullOrEmpty(oldlocationinfo.AreaInfo.Id)
                    )
                    {
                        locationInfoLinked.AreaInfo = oldlocationinfo.AreaInfo;

                        var areainfo = await LocationListCreator.GetLocationFromDB<Tourismverein>(
                            queryFactory,
                            "areas",
                            Tuple.Create("id", oldlocationinfo.AreaInfo.Id)
                        );

                        if (areainfo != null && areainfo.Count() > 0)
                        {
                            var areainfoname = areainfo
                                .FirstOrDefault()
                                .Detail.ToDictionary(x => x.Key, x => x.Value.Title);
                            if (areainfoname != null)
                                locationInfoLinked.AreaInfo.Name = areainfoname;
                        }
                    }

                    return locationInfoLinked;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Extension Method to update the locationinfo
        /// </summary>
        /// <param name="oldlocationinfo"></param>
        /// <param name="queryFactory"></param>
        /// <returns></returns>
        public static async Task<LocationInfoLinked> UpdateLocationInfoExtension<T>(
            this T data,
            QueryFactory queryFactory
        )
            where T : IHasLocationInfoLinked
        {
            LocationInfoLinked? oldlocationinfo = data.LocationInfo;

            //Check if Locationinfo is already there //Test ObjectNullCheck
            if (oldlocationinfo == null || ObjectAndStringNullCheck(oldlocationinfo))
            {
                //IF a DistrictId is there use this
                if (data is IDistrictId && !String.IsNullOrEmpty((data as IDistrictId).DistrictId))
                    return await GetTheLocationInfoDistrict(
                        queryFactory,
                        (data as IDistrictId).DistrictId as string
                    );
                //Else use the GPS Point
                else if (
                    data is IGPSInfoAware
                    && (data as IGPSInfoAware).GpsInfo != null
                    && (data as IGPSInfoAware).GpsInfo.Count > 0
                )
                {
                    var gps = (data as IGPSInfoAware)
                        .GpsInfo.Where(x => x.Gpstype == "position")
                        .FirstOrDefault();
                    if (gps == null)
                        gps = (data as IGPSInfoAware).GpsInfo.FirstOrDefault();

                    //Check if the Gps Point is in South Tyrol

                    //var southtyrol = await queryFactory.Query()
                    //    .SelectRaw("ST_AsText(geometry)")
                    //    .From("shapes")
                    //    .Where("name", "Bolzano")
                    //    .FirstOrDefaultAsync<string>();
                    CultureInfo culture = CultureInfo.InvariantCulture;

                    var isinsouthtyrol = await queryFactory
                        .Query()
                        .SelectRaw(
                            $"ST_Contains((select geometry from shapes where name = 'Bolzano'), st_setsrid(st_makepoint(({gps.Longitude.ToString(culture)})::double precision, ({gps.Latitude.ToString(culture)})::double precision), 4326))"
                        )
                        .FirstOrDefaultAsync<bool>();

                    if (isinsouthtyrol)
                    {
                        var district = await LocationInfoHelper.GetNearestDistrictbyGPS(
                            queryFactory,
                            gps.Latitude,
                            gps.Longitude,
                            30000
                        );
                        return await GetTheLocationInfoDistrict(queryFactory, district.Id);
                    }
                    else
                        return null;
                }
                else
                    return null;
                //TODO Use Area, use TV use SIAG Methods
            }
            else
                return await UpdateLocationInfo(oldlocationinfo, queryFactory);
        }

        public static async Task<IEnumerable<District>> GetNearestDistrict(
            QueryFactory QueryFactory,
            PGGeoSearchResult geosearchresult,
            int limitto
        )
        {
            var districtquery = QueryFactory
                .Query("districts")
                .Select("data")
                .ApplyOrdering_GeneratedColumns(geosearchresult, null)
                .Limit(limitto);

            var data = await districtquery.GetObjectListAsync<District>();

            return data;
        }

        public static async Task<District?> GetNearestDistrictbyGPS(
            QueryFactory QueryFactory,
            double latitude,
            double longitude,
            int radius = 30000
        )
        {
            string wheregeo = PostgresSQLHelper.GetGeoWhereSimple(latitude, longitude, radius);
            string orderbygeo = PostgresSQLHelper.GetGeoOrderBySimple(latitude, longitude);

            var query = QueryFactory
                .Query("districts")
                .Select("data")
                .WhereRaw(wheregeo)
                .OrderByRaw(orderbygeo);

            return await query.GetObjectSingleAsync<District>();
        }

        public static async Task<LocationInfoLinked> GetTheLocationInfoDistrict(
            QueryFactory QueryFactory,
            string? districtid
        )
        {
            if (districtid == null)
                return new LocationInfoLinked();

            LocationInfoLinked mylocinfo = new LocationInfoLinked();

            //Wenn nicht District nicht definiert ist oder Livinallongo/Arabba/Gebiet Pieve - Digonera - Pordoijoch - nicht südtirol ;)
            if (
                districtid != "79CBD63051C911D18F1400A02427D15E"
                && districtid != "53DF587C2BF74853B9DF3429089587E3"
                && districtid != "43C0E6789C4046718B70DAA56CF4332C"
                && districtid != "52B456D784854FB5A77F87C0CF4AFADF"
                && districtid != "C17DC9768C1A4DC5BA1592ED5C1D591B"
                && districtid != "79CBAAA0513331D18F1400A02427D15E"
            )
            {
                var districtquery = QueryFactory
                    .Query("districts")
                    .Select("data")
                    .Where("id", districtid.ToUpper());
                var district = await districtquery.GetObjectSingleAsync<District>();

                var districtnames = (from x in district?.Detail select x).ToDictionary(
                    x => x.Key,
                    x => x.Value.Title
                );

                var munquery = QueryFactory
                    .Query("municipalities")
                    .Select("data")
                    .Where("id", district?.MunicipalityId?.ToUpper());
                var municipality = await munquery.GetObjectSingleAsync<Municipality>();

                var municipalitynames = (from x in municipality?.Detail select x).ToDictionary(
                    x => x.Key,
                    x => x.Value.Title
                );

                var tvquery = QueryFactory
                    .Query("tvs")
                    .Select("data")
                    .Where("id", district?.TourismvereinId?.ToUpper());
                var tourismverein = await tvquery.GetObjectSingleAsync<Tourismverein>();

                var tourismvereinnames = (from x in tourismverein?.Detail select x).ToDictionary(
                    x => x.Key,
                    x => x.Value.Title
                );

                var regquery = QueryFactory
                    .Query("regions")
                    .Select("data")
                    .Where("id", district?.RegionId?.ToUpper());
                var region = await regquery.GetObjectSingleAsync<Region>();
                var regionnames = new Dictionary<string, string?>();

                if (region != null)
                {
                    regionnames = (from x in region?.Detail select x).ToDictionary(
                        x => x.Key,
                        x => x.Value.Title
                    );
                }

                mylocinfo.DistrictInfo = new DistrictInfoLinked()
                {
                    Id = district?.Id,
                    Name = districtnames,
                };
                mylocinfo.MunicipalityInfo = new MunicipalityInfoLinked()
                {
                    Id = municipality?.Id,
                    Name = municipalitynames,
                };
                mylocinfo.TvInfo = new TvInfoLinked()
                {
                    Id = tourismverein?.Id,
                    Name = tourismvereinnames,
                };
                mylocinfo.RegionInfo = new RegionInfoLinked()
                {
                    Id = region?.Id,
                    Name = regionnames,
                };
            }
            return mylocinfo;
        }

        public static async Task<LocationInfoLinked> GetTheLocationInfoMunicipality(
            QueryFactory QueryFactory,
            string municipalityid
        )
        {
            if (municipalityid == null)
                return new LocationInfoLinked();

            LocationInfoLinked mylocinfo = new LocationInfoLinked();

            var munquery = QueryFactory
                .Query("municipalities")
                .Select("data")
                .Where("id", municipalityid.ToUpper());
            var municipality = await munquery.GetObjectSingleAsync<Municipality>();

            var municipalitynames = (from x in municipality?.Detail select x).ToDictionary(
                x => x.Key,
                x => x.Value.Title
            );

            var tvquery = QueryFactory
                .Query("tvs")
                .Select("data")
                .Where("id", municipality?.TourismvereinId?.ToUpper());
            var tourismverein = await tvquery.GetObjectSingleAsync<Tourismverein>();

            var tourismvereinnames = (from x in tourismverein?.Detail select x).ToDictionary(
                x => x.Key,
                x => x.Value.Title
            );

            var regquery = QueryFactory
                .Query("regions")
                .Select("data")
                .Where("id", municipality?.RegionId?.ToUpper());
            var region = await regquery.GetObjectSingleAsync<Region>();
            var regionnames = new Dictionary<string, string?>();

            if (region != null)
            {
                regionnames = (from x in region?.Detail select x).ToDictionary(
                    x => x.Key,
                    x => x.Value.Title
                );
            }

            mylocinfo.DistrictInfo = new DistrictInfoLinked();
            mylocinfo.MunicipalityInfo = new MunicipalityInfoLinked()
            {
                Id = municipality?.Id,
                Name = municipalitynames,
            };
            mylocinfo.TvInfo = new TvInfoLinked()
            {
                Id = tourismverein?.Id,
                Name = tourismvereinnames,
            };
            mylocinfo.RegionInfo = new RegionInfoLinked() { Id = region?.Id, Name = regionnames };

            return mylocinfo;
        }

        public static async Task<LocationInfoLinked> GetTheLocationInfoTourismAssociation(
            QueryFactory QueryFactory,
            string tvid
        )
        {
            if (tvid == null)
                return new LocationInfoLinked();

            LocationInfoLinked mylocinfo = new LocationInfoLinked();

            var tvquery = QueryFactory.Query("tvs").Select("data").Where("id", tvid.ToUpper());
            var tourismverein = await tvquery.GetObjectSingleAsync<Tourismverein>();

            var tourismvereinnames = (from x in tourismverein?.Detail select x).ToDictionary(
                x => x.Key,
                x => x.Value.Title
            );

            var regquery = QueryFactory
                .Query("regions")
                .Select("data")
                .Where("id", tourismverein?.RegionId?.ToUpper());
            var region = await regquery.GetObjectSingleAsync<Region>();
            var regionnames = new Dictionary<string, string?>();

            if (region != null)
            {
                regionnames = (from x in region?.Detail select x).ToDictionary(
                    x => x.Key,
                    x => x.Value.Title
                );
            }

            mylocinfo.DistrictInfo = new DistrictInfoLinked();
            mylocinfo.MunicipalityInfo = new MunicipalityInfoLinked();
            mylocinfo.TvInfo = new TvInfoLinked()
            {
                Id = tourismverein?.Id,
                Name = tourismvereinnames,
            };
            mylocinfo.RegionInfo = new RegionInfoLinked() { Id = region?.Id, Name = regionnames };

            return mylocinfo;
        }

        public static async Task<LocationInfoLinked> GetTheLocationInfoRegion(
            QueryFactory QueryFactory,
            string regionid
        )
        {
            if (regionid == null)
                return new LocationInfoLinked();

            LocationInfoLinked mylocinfo = new LocationInfoLinked();

            var regquery = QueryFactory
                .Query("regions")
                .Select("data")
                .Where("id", regionid.ToUpper());
            var region = await regquery.GetObjectSingleAsync<Region>();
            var regionnames = new Dictionary<string, string?>();

            if (region != null)
            {
                regionnames = (from x in region?.Detail select x).ToDictionary(
                    x => x.Key,
                    x => x.Value.Title
                );
            }

            mylocinfo.DistrictInfo = new DistrictInfoLinked();
            mylocinfo.MunicipalityInfo = new MunicipalityInfoLinked();
            mylocinfo.TvInfo = new TvInfoLinked();
            mylocinfo.RegionInfo = new RegionInfoLinked() { Id = region?.Id, Name = regionnames };

            return mylocinfo;
        }

        public static async Task<LocationInfoLinked> GetTheLocationInfoArea(
            QueryFactory QueryFactory,
            string areaid
        )
        {
            if (areaid == null)
                return new LocationInfoLinked();

            LocationInfoLinked mylocinfo = new LocationInfoLinked();

            var areaquery = QueryFactory
                .Query("areas")
                .Select("data")
                .Where("id", areaid.ToUpper());
            var area = await areaquery.GetObjectSingleAsync<Area>();

            var tvquery = QueryFactory
                .Query("tvs")
                .Select("data")
                .Where("id", area?.TourismvereinId.ToUpper());
            var tourismverein = await tvquery.GetObjectSingleAsync<Tourismverein>();

            var tourismvereinnames = (from x in tourismverein?.Detail select x).ToDictionary(
                x => x.Key,
                x => x.Value.Title
            );

            var regquery = QueryFactory
                .Query("regions")
                .Select("data")
                .Where("id", area?.RegionId?.ToUpper());
            var region = await regquery.GetObjectSingleAsync<Region>();
            var regionnames = new Dictionary<string, string?>();

            if (region != null)
            {
                regionnames = (from x in region?.Detail select x).ToDictionary(
                    x => x.Key,
                    x => x.Value.Title
                );
            }

            mylocinfo.DistrictInfo = new DistrictInfoLinked();
            mylocinfo.MunicipalityInfo = new MunicipalityInfoLinked();
            mylocinfo.TvInfo = new TvInfoLinked()
            {
                Id = tourismverein?.Id,
                Name = tourismvereinnames,
            };
            mylocinfo.RegionInfo = new RegionInfoLinked() { Id = region?.Id, Name = regionnames };

            return mylocinfo;
        }

        public static async Task<LocationInfoLinked> GetTheLocationInfoDistrict_Siag(
            QueryFactory QueryFactory,
            string? districtid_siag
        )
        {
            if (districtid_siag == null)
                return new LocationInfoLinked();

            LocationInfoLinked mylocinfo = new LocationInfoLinked();

            var districtquery = QueryFactory
                .Query("districts")
                .Select("data")
                .WhereRaw("data->>'SiagId' = $$", districtid_siag);

            var district = await districtquery.GetObjectSingleAsync<District>();

            if (district != null)
            {
                var districtnames = (from x in district?.Detail select x).ToDictionary(
                    x => x.Key,
                    x => x.Value.Title
                );

                var munquery = QueryFactory
                    .Query("municipalities")
                    .Select("data")
                    .Where("id", district?.MunicipalityId?.ToUpper());
                var municipality = await munquery.GetObjectSingleAsync<Municipality>();

                var municipalitynames = (from x in municipality?.Detail select x).ToDictionary(
                    x => x.Key,
                    x => x.Value.Title
                );

                var tvquery = QueryFactory
                    .Query("tvs")
                    .Select("data")
                    .Where("id", district?.TourismvereinId?.ToUpper());
                var tourismverein = await tvquery.GetObjectSingleAsync<Tourismverein>();

                var tourismvereinnames = (from x in tourismverein?.Detail select x).ToDictionary(
                    x => x.Key,
                    x => x.Value.Title
                );

                var regquery = QueryFactory
                    .Query("regions")
                    .Select("data")
                    .Where("id", district?.RegionId?.ToUpper());
                var region = await regquery.GetObjectSingleAsync<Region>();
                var regionnames = new Dictionary<string, string?>();

                if (region != null)
                {
                    regionnames = (from x in region?.Detail select x).ToDictionary(
                        x => x.Key,
                        x => x.Value.Title
                    );
                }

                mylocinfo.DistrictInfo = new DistrictInfoLinked()
                {
                    Id = district?.Id,
                    Name = districtnames,
                };
                mylocinfo.MunicipalityInfo = new MunicipalityInfoLinked()
                {
                    Id = municipality?.Id,
                    Name = municipalitynames,
                };
                mylocinfo.TvInfo = new TvInfoLinked()
                {
                    Id = tourismverein?.Id,
                    Name = tourismvereinnames,
                };
                mylocinfo.RegionInfo = new RegionInfoLinked()
                {
                    Id = region?.Id,
                    Name = regionnames,
                };
            }

            return mylocinfo;
        }

        public static async Task<LocationInfoLinked> GetTheLocationInfoMunicipality_Siag(
            QueryFactory QueryFactory,
            string? municipalityid_siag
        )
        {
            if (municipalityid_siag == null)
                return new LocationInfoLinked();

            LocationInfoLinked mylocinfo = new LocationInfoLinked();

            var municipalityquery = QueryFactory
                .Query("municipalities")
                .Select("data")
                .WhereRaw("data->>'SiagId' = $$", municipalityid_siag);

            var municipality = await municipalityquery.GetObjectSingleAsync<District>();

            if (municipality != null)
            {
                var municipalitynames = (from x in municipality?.Detail select x).ToDictionary(
                    x => x.Key,
                    x => x.Value.Title
                );

                var tvquery = QueryFactory
                    .Query("tvs")
                    .Select("data")
                    .Where("id", municipality?.TourismvereinId?.ToUpper());
                var tourismverein = await tvquery.GetObjectSingleAsync<Tourismverein>();

                var tourismvereinnames = (from x in tourismverein?.Detail select x).ToDictionary(
                    x => x.Key,
                    x => x.Value.Title
                );

                var regquery = QueryFactory
                    .Query("regions")
                    .Select("data")
                    .Where("id", municipality?.RegionId?.ToUpper());
                var region = await regquery.GetObjectSingleAsync<Region>();

                var regionnames = new Dictionary<string, string?>();

                if (region != null)
                {
                    regionnames = (from x in region?.Detail select x).ToDictionary(
                        x => x.Key,
                        x => x.Value.Title
                    );
                }

                //mylocinfo.DistrictInfo = new DistrictInfoLinked() { Id = district?.Id, Name = districtnames };
                mylocinfo.MunicipalityInfo = new MunicipalityInfoLinked()
                {
                    Id = municipality?.Id,
                    Name = municipalitynames,
                };
                mylocinfo.TvInfo = new TvInfoLinked()
                {
                    Id = tourismverein?.Id,
                    Name = tourismvereinnames,
                };
                mylocinfo.RegionInfo = new RegionInfoLinked()
                {
                    Id = region?.Id,
                    Name = regionnames,
                };
            }

            return mylocinfo;
        }

        public static bool ObjectAndStringNullCheck(object myObject)
        {
            bool isnull = true;
            foreach (var myobject in myObject.GetType().GetProperties())
            {
                if (myobject.PropertyType == typeof(string))
                {
                    if (!string.IsNullOrEmpty((string)myobject.GetValue(myObject)))
                        isnull = false;
                }
                else if (myobject.GetValue(myObject) != null)
                    isnull = false;
            }

            return isnull;
        }




        /// <summary>
        /// Recreate LocationInfo by passing queryFactory and querying the DB for each
        /// </summary>
        /// <param name="oldlocationinfo"></param>
        /// <param name="queryFactory"></param>
        /// <returns></returns>
        public static async Task UpdateDistanceCalculation<T>(this T mydata, QueryFactory queryFactory, 
            string? districtid = null, 
            string? municipalityid = null            
        ) where T : IDistanceInfoAware
        {
            try
            {
                if (mydata != null)
                {
                    if(districtid == null && municipalityid == null && mydata is IHasLocationInfoLinked)
                    {
                        if((mydata as IHasLocationInfoLinked).LocationInfo != null && (mydata as IHasLocationInfoLinked).LocationInfo.DistrictInfo != null)
                            districtid = (mydata as IHasLocationInfoLinked).LocationInfo.DistrictInfo.Id;
                        if ((mydata as IHasLocationInfoLinked).LocationInfo != null && (mydata as IHasLocationInfoLinked).LocationInfo.MunicipalityInfo != null)
                            municipalityid = (mydata as IHasLocationInfoLinked).LocationInfo.MunicipalityInfo.Id;
                    }


                    if(!String.IsNullOrEmpty(districtid))
                    {
                        var districtgps = await queryFactory
                        .Query()
                        .Select("gen_latitude", "gen_longitude")
                        .From("districts")
                        .Where("id", districtid)
                        .GetAsync<GenCoordinate>();

                        if(districtgps.FirstOrDefault() != null && districtgps.FirstOrDefault().gen_latitude != null && districtgps.FirstOrDefault().gen_longitude != null)
                        {
                            if(mydata is IGpsInfo)
                                mydata.ExtendGpsInfoToDistanceCalculation("district", districtgps.FirstOrDefault().gen_latitude, districtgps.FirstOrDefault().gen_longitude);
                            if (mydata is IGPSInfoAware)
                                mydata.ExtendGpsInfoToDistanceCalculationList("district", districtgps.FirstOrDefault().gen_latitude, districtgps.FirstOrDefault().gen_longitude);
                        }
                    }
                    if (!String.IsNullOrEmpty(municipalityid))
                    {
                        var mungps = await queryFactory
                        .Query()
                        .Select("gen_latitude","gen_longitude")
                        .From("municipalities")
                        .Where("id", municipalityid)
                        .GetAsync<GenCoordinate>();

                        if (mungps.FirstOrDefault() != null && mungps.FirstOrDefault().gen_latitude != null && mungps.FirstOrDefault().gen_longitude != null)
                        {
                            if (mydata is IGpsInfo)
                                mydata.ExtendGpsInfoToDistanceCalculation("municipality", mungps.FirstOrDefault().gen_latitude, mungps.FirstOrDefault().gen_longitude);
                            if (mydata is IGPSInfoAware)
                                mydata.ExtendGpsInfoToDistanceCalculationList("municipality", mungps.FirstOrDefault().gen_latitude, mungps.FirstOrDefault().gen_longitude);
                        }
                    }
                }                
            }
            catch (Exception ex)
            {
                
            }
        }

        ////Sonderfall mehrere Areas
        //public static LocationInfo GetTheLocationInfoAreas(NpgsqlConnection conn, List<string> areaids, string owner)
        //{
        //    LocationInfo mylocinfo = new LocationInfo();

        //    foreach (string areaid in areaids)
        //    {

        //        //Query auf District mit include auf alle
        //        if (areaid != null)
        //        {

        //            var area = PostgresSQLHelper.SelectFromTableDataAsObject<Area>(conn, "areas", "*", "id LIKE '" + areaid + "'", "", 0, null).FirstOrDefault();

        //            if (area != null)
        //            {
        //                if (area.RegionId == owner || area.TourismvereinId == owner)
        //                {

        //                    Dictionary<string, string> areanames = new Dictionary<string, string>();
        //                    areanames.Add("de", area.Shortname);
        //                    areanames.Add("it", area.Shortname);
        //                    areanames.Add("en", area.Shortname);
        //                    areanames.Add("nl", area.Shortname);
        //                    areanames.Add("cs", area.Shortname);
        //                    areanames.Add("pl", area.Shortname);
        //                    areanames.Add("fr", area.Shortname);
        //                    areanames.Add("ru", area.Shortname);


        //                    mylocinfo.AreaInfo = new AreaInfo() { Id = area.Id, Name = areanames };

        //                    //var municipality = session.Load<Municipality>(area.MunicipalityId);
        //                    //var municipalitynames = (from x in municipality.Detail
        //                    //                         select x).ToDictionary(x => x.Key, x => x.Value.Title);

        //                    if (area.TourismvereinId != null)
        //                    {
        //                        var tourismverein = PostgresSQLHelper.SelectFromTableDataAsObject<Tourismverein>(conn, "tvs", "*", "id LIKE '" + area.TourismvereinId.ToUpper() + "'", "", 0, null).FirstOrDefault();
        //                        var tourismvereinnames = (from x in tourismverein.Detail
        //                                                  select x).ToDictionary(x => x.Key, x => x.Value.Title);

        //                        mylocinfo.TvInfo = new TvInfo() { Id = tourismverein.Id, Name = tourismvereinnames };
        //                    }


        //                    if (area.RegionId != null)
        //                    {
        //                        var region = PostgresSQLHelper.SelectFromTableDataAsObject<Region>(conn, "regions", "*", "id LIKE '" + area.RegionId.ToUpper() + "'", "", 0, null).FirstOrDefault();
        //                        var regionnames = (from x in region.Detail
        //                                           select x).ToDictionary(x => x.Key, x => x.Value.Title);

        //                        mylocinfo.RegionInfo = new RegionInfo() { Id = region.Id, Name = regionnames };
        //                    }

        //                    //mylocinfo.MunicipalityInfo = new MunicipalityInfo() { Id = municipality.Id, Name = municipalitynames };
        //                }
        //            }
        //        }
        //    }
        //    return mylocinfo;
        //}
    }

    public class GenCoordinate
    {
        public double gen_latitude { get; set; }
        public double gen_longitude { get; set; }
    }
}
