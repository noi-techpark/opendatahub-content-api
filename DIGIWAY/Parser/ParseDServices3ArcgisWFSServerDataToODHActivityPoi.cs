// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Amazon.Runtime.Internal.Transform;
using CoordinateSharp;
using DataModel;
using DIGIWAY.Model;
using Helper;
using Helper.Extensions;
using Helper.Geo;
using MongoDB.Bson.Serialization.IdGenerators;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using Npgsql.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using ProjNet.CoordinateSystems;
//using NetTopologySuite.CoordinateSystems.Transformations

namespace DIGIWAY
{
    public class ParseDServices3ArcgisWFSServerDataToODHActivityPoi
    {
        public static (ODHActivityPoiLinked, GeoShapeJson) ParseToODHActivityPoi(
            ODHActivityPoiLinked? odhactivitypoi,
            IWFSRoute digiwaydata,
            string type,
            string srid
        )
        {
            var result = type switch
            {
                "radrouten_tirol" or "Radrouten_Tirol:TN_WALD_Radrouten_Tirol_CDBD5BC5-8635-418A-BC13-52A99900D008" => ParseCyclingRoutesTyrolToODHActivityPoi(odhactivitypoi, digiwaydata as MountainBikeRoute, type, srid),
                "hikintrail_e5" => ParseHikingRouteE5TODHActivityPoi(odhactivitypoi, digiwaydata as E5TrailRoute, type, srid),
                "_" => (null,null)
            };

            return result;
        }
   
        private static (GeoShapeJson, GpsInfo) ParseGeoServerGeodataToGeoShapeJson(IWFSRoute digiwaydata, string name, string identifier, string geoshapetype, string source, int? altitude, string srid)
        {
            GeoShapeJson geoshape = new GeoShapeJson();
            geoshape.Id = (identifier + "_" + digiwaydata.ObjectId.ToString()).ToLower();
            geoshape.Name = name;
            geoshape.Type = geoshapetype;
            geoshape.Source = source;

            geoshape.Geometry = digiwaydata.Geometry;

            //get first point of geometry
            var firstCoord = geoshape.Geometry.Coordinates.FirstOrDefault();

            if(srid == "31254")
            {
                var converter = new EPSG31254ToEPSG4326Converter();
                var (longitude, latitude) = converter.ConvertToWGS84(firstCoord.X, firstCoord.Y);

                var gpsinfo = new GpsInfo()
                {
                    Altitude = altitude,
                    AltitudeUnitofMeasure = "m",
                    Gpstype = "position",
                    //Use only first digits otherwise point and track will differ
                    Latitude = latitude,
                    Longitude = longitude
                };

                return (geoshape, gpsinfo);
            }
            else if (srid == "3857")
            {
                var wsg84coordinate = EPSG3857ToEPSG4326Converter.ConvertWebMercatorToWGS84(firstCoord.X, firstCoord.Y);

                var gpsinfo = new GpsInfo()
                {
                    Altitude = altitude,
                    AltitudeUnitofMeasure = "m",
                    Gpstype = "position",
                    //Use only first digits otherwise point and track will differ
                    Latitude = wsg84coordinate.Latitude,
                    Longitude = wsg84coordinate.Longitude
                };

                return (geoshape, gpsinfo);
            }
            else
                return (geoshape, new GpsInfo());
        }
        
        private static (ODHActivityPoiLinked, GeoShapeJson) ParseCyclingRoutesTyrolToODHActivityPoi(
            ODHActivityPoiLinked? odhactivitypoi,
            MountainBikeRoute digiwaydata,
            string type,
            string srid
        )
        {
            if(odhactivitypoi == null)
                odhactivitypoi = new ODHActivityPoiLinked();

            odhactivitypoi.Id = type + "_" + digiwaydata.ObjectId.ToString();
            odhactivitypoi.Active = true;
            odhactivitypoi.FirstImport = odhactivitypoi.FirstImport != null ? digiwaydata.UpdateTimestamp : odhactivitypoi.FirstImport;
            odhactivitypoi.LastChange = Convert.ToDateTime(digiwaydata.UpdateTimestamp);
            odhactivitypoi.HasLanguage = new List<string>() { "de","en" };
            odhactivitypoi.Shortname = digiwaydata.RouteName != null ? digiwaydata.RouteName : null;
            odhactivitypoi.Detail = new Dictionary<string, Detail>();

            List<string> keywords = new List<string>();
            if (digiwaydata.RouteType != null)
                keywords.Add(digiwaydata.RouteType);
            if (digiwaydata.RouteNumber != null)
                keywords.Add(digiwaydata.RouteNumber);
            if (digiwaydata.SectionType != null)
                keywords.Add(digiwaydata.SectionType);

            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("de", new Detail()
            {
                Title = digiwaydata.RouteName != null ? digiwaydata.RouteName : null,
                BaseText = digiwaydata.RouteDescription != null ? digiwaydata.RouteDescription : null,
                Header = digiwaydata.RouteType != null ? digiwaydata.RouteType : null,
                AdditionalText = "Start: " + digiwaydata.RouteStart + " Ende: " + digiwaydata.RouteEnd,
                Keywords = keywords,
                Language = "de"
            });
            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("en", new Detail()
            {
                Title = digiwaydata.RouteName != null ? digiwaydata.RouteName : null,
                BaseText = digiwaydata.RouteDescriptionEn != null ? digiwaydata.RouteDescriptionEn : null,
                Header = digiwaydata.RouteType != null ? digiwaydata.RouteType : null,
                AdditionalText = "start: " + digiwaydata.RouteStartEn + " end: " + digiwaydata.RouteEndEn,
                Keywords = keywords,
                Language = "en"
            });


            odhactivitypoi.DistanceDuration = digiwaydata.RidingTime != null ? TransformDuration(digiwaydata.RidingTime) : null;
            odhactivitypoi.Difficulty = digiwaydata.Difficulty != null ?TransformMTBDifficulty(digiwaydata.Difficulty) : null;
            
            odhactivitypoi.Ratings = new Ratings() { Difficulty = odhactivitypoi.Difficulty };

            odhactivitypoi.AltitudeSumDown = digiwaydata.ElevationDown != null ? Convert.ToDouble(digiwaydata.ElevationDown) : null;
            odhactivitypoi.AltitudeSumUp = digiwaydata.ElevationUp != null ? Convert.ToDouble(digiwaydata.ElevationUp) : null;
            odhactivitypoi.Source = "dservices3.arcgis.com";
            odhactivitypoi.SyncSourceInterface = "dservices3.arcgis.com." + type.ToLower();
            odhactivitypoi.DistanceLength = digiwaydata.LengthKm != null ? digiwaydata.LengthKm : null;

            //Number
            odhactivitypoi.Number = digiwaydata.RouteNumber;

            //Status
            odhactivitypoi.IsOpen = TransformStatus(digiwaydata.Status);

            //Add Tags
            odhactivitypoi.TagIds = new List<string>();
            odhactivitypoi.TagIds.Add("1B9AF4DA6E3A414798890E6723E71EC8"); //LTS MTB Tag
            odhactivitypoi.TagIds.Add("cycling");
            odhactivitypoi.TagIds.Add("mountain bike");
            odhactivitypoi.TagIds.Add("mountain bikes");

            Dictionary<string, string> additionalvalues = new Dictionary<string, string>();
            if(digiwaydata.ObjectId != null)
                additionalvalues.Add("objectid", digiwaydata.ObjectId.ToString());
            if (digiwaydata.Object != null)
                additionalvalues.Add("object", digiwaydata.Object);
            if (digiwaydata.RouteNumber != null)
                additionalvalues.Add("routenumber", digiwaydata.RouteNumber);
            if (digiwaydata.SectionType != null)
                additionalvalues.Add("sectiontype", digiwaydata.SectionType);
            if (digiwaydata.RouteType != null)
                additionalvalues.Add("route_type", digiwaydata.RouteType);
            if (digiwaydata.EndElevation != null)
                additionalvalues.Add("elevation_start", digiwaydata.EndElevation.ToString());
            if (digiwaydata.StartElevation != null)
                additionalvalues.Add("elevation_end", digiwaydata.StartElevation.ToString());


            if (digiwaydata.Status != null)
                additionalvalues.Add("status", digiwaydata.Status);
            

            var georesult = ParseGeoServerGeodataToGeoShapeJson(
                digiwaydata,
                digiwaydata.RouteName,
                type,
                "mountainbikeroute",
                "dservices3.arcgis.com",
                digiwaydata.StartElevation,
                srid
                );

            odhactivitypoi.Mapping.TryAddOrUpdate("dservices3.arcgis.com", additionalvalues);

            //Add Starting GPS Coordinate as GPS Point 
            odhactivitypoi.GpsInfo = new List<GpsInfo>() { georesult.Item2 };

            return (odhactivitypoi, georesult.Item1);
        }

        private static (ODHActivityPoiLinked, GeoShapeJson) ParseHikingRouteE5TODHActivityPoi(
           ODHActivityPoiLinked? odhactivitypoi,
           E5TrailRoute digiwaydata,
           string type,
           string srid
       )
        {
            if (odhactivitypoi == null)
                odhactivitypoi = new ODHActivityPoiLinked();

            odhactivitypoi.Id = type + "_" + digiwaydata.ObjectId.ToString();
            odhactivitypoi.Active = true;
            odhactivitypoi.FirstImport = odhactivitypoi.FirstImport != null ? DateTime.Now : odhactivitypoi.FirstImport;
            odhactivitypoi.LastChange = DateTime.Now;
            odhactivitypoi.HasLanguage = new List<string>() { "de", "it", "es" };
            odhactivitypoi.Shortname = digiwaydata.PathDe != null ? digiwaydata.PathDe : null;
            odhactivitypoi.Detail = new Dictionary<string, Detail>();

            List<string> keywords = new List<string>();
            if (digiwaydata.PathCode != null)
                keywords.Add(digiwaydata.PathCode);
            if (digiwaydata.ResporgDigiwayCode != null)
                keywords.Add(digiwaydata.ResporgDigiwayCode);

            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("de", new Detail()
            {
                Title = digiwaydata.PathDe != null ? digiwaydata.PathDe : null,
                BaseText = digiwaydata.ResporgDigiwayDe != null ? digiwaydata.ResporgDigiwayDe : null,
                Keywords = keywords,
                Language = "de"
            });
            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("it", new Detail()
            {
                Title = digiwaydata.PathIt != null ? digiwaydata.PathIt : null,
                BaseText = digiwaydata.ResporgDigiwayIt != null ? digiwaydata.ResporgDigiwayIt : null,
                Keywords = keywords,
                Language = "it"
            });
            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("es", new Detail()
            {
                Title = digiwaydata.PathEs != null ? digiwaydata.PathEs : null,
                BaseText = digiwaydata.ResporgDigiwayEs != null ? digiwaydata.ResporgDigiwayEs : null,
                Keywords = keywords,
                Language = "es"
            });

            odhactivitypoi.Source = "dservices3.arcgis.com";
            odhactivitypoi.SyncSourceInterface = "dservices3.arcgis.com." + type.ToLower();
            
            //Number
            odhactivitypoi.Number = digiwaydata.PathCode;

            //Status
            //odhactivitypoi.IsOpen = TransformStatus(digiwaydata.Status);

            //Add Tags
            odhactivitypoi.TagIds = new List<string>();
            odhactivitypoi.TagIds.Add("978F89296ACB4DB4B6BD1C269341802F"); //LTS Hiking Tag
            odhactivitypoi.TagIds.Add("hiking");
            odhactivitypoi.TagIds.Add("C99701BC34C4659B4A82F320E48CFAE"); //LTS Long-distance hiking trails
            odhactivitypoi.TagIds.Add("longdistance hiking paths");


            Dictionary<string, string> additionalvalues = new Dictionary<string, string>();
            if (digiwaydata.ObjectId != null)
                additionalvalues.Add("objectid", digiwaydata.ObjectId.ToString());
            if (digiwaydata.PathCode != null)
                additionalvalues.Add("pathcode", digiwaydata.PathCode);
            if (digiwaydata.ResporgDigiwayCode != null)
                additionalvalues.Add("resporgdigiwaycode", digiwaydata.ResporgDigiwayCode);
            if (digiwaydata.GlobalId != null)
                additionalvalues.Add("globalid", digiwaydata.GlobalId);
          

            var georesult = ParseGeoServerGeodataToGeoShapeJson(
                digiwaydata,
                digiwaydata.PathDe,
                type,
                "hikingpathe5route",
                "dservices3.arcgis.com",
                0,
                srid
                );

            odhactivitypoi.Mapping.TryAddOrUpdate("dservices3.arcgis.com", additionalvalues);

            //Add Starting GPS Coordinate as GPS Point 
            odhactivitypoi.GpsInfo = new List<GpsInfo>() { georesult.Item2 };

            return (odhactivitypoi, georesult.Item1);
        }

        public static double? TransformDuration(string? duration)
        {
            if (duration == null) { return null; }
            else
            {
                //RUNNING_TIME 3h11min
                var hour = duration.Split("h");
                //3h11min
                if (hour.Length == 2)
                {
                    var minute = hour[1].Replace("min", "").Replace(":","");

                    //transform minute from 60 to 100
                    double hourd = Convert.ToDouble(hour[0]);
                    if (!String.IsNullOrEmpty(minute))
                    {
                        double minuted = Convert.ToDouble(minute);
                        double minutedconv = minuted / 60;

                        return hourd + minutedconv;
                    }
                    //2h
                    else
                        return hourd;
                }
                //40min
                else if(hour.Length == 1 && hour[0].Contains("min"))
                {
                    var minute = hour[0].Replace("min", "");

                    //transform minute from 60 to 100
                    double minuted = Convert.ToDouble(minute);
                    double minutedconv = minuted / 60;

                    return minutedconv;
                }
                else { return null; }
            }
        }

        public static string? TransformMTBDifficulty(string? difficulty)
        {
            if (difficulty == null) { return null; }
            else
            {
                switch (difficulty)
                {
                    case "schwierig": return "6";
                    case "mittelschwierig": return "3";
                    case "leicht": return "1";
                    default: return null;
                }
            }
        }

        public static bool? TransformStatus(string? status)
        {
            if (status == null) { return null; }
            else
            {
                switch (status)
                {
                    case "offen": return true;
                    case "gesperrt": return false;
                    default: return null;
                }
            }
        }
    }
}
