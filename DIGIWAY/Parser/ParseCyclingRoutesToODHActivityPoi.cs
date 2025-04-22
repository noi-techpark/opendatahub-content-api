// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helper.Extensions;
using Helper;
using System.Globalization;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.IO;
using NetTopologySuite.Algorithm;
using Newtonsoft.Json;
using CoordinateSharp;
//using ProjNet.CoordinateSystems;
//using NetTopologySuite.CoordinateSystems.Transformations

namespace DIGIWAY
{
    public class ParseGeoServerDataToODHActivityPoi
    {
        public static (ODHActivityPoiLinked, GeoShapeJson) ParseToODHActivityPoi(
            ODHActivityPoiLinked? odhactivitypoi,
            GeoserverCivisData digiwaydata,
            string type
        )
        {
            var result = type switch
            {
                "cyclewaystyrol" => ParseCyclingRoutesTyrolToODHActivityPoi(odhactivitypoi, digiwaydata),
                "mountainbikeroutes" => ParseMTBRoutesToODHActivityPoi(odhactivitypoi, digiwaydata),
                "hikingtrails" => (null, null),
                "intermunicipalcyclingroutes" => (null, null),
                "_" => (null,null)
            };

            return result;
        }

        private static (GeoShapeJson, GpsInfo) ParseGeoServerGeodataToGeoShapeJson(GeoserverCivisData digiwaydata, string type)
        {
            GeoShapeJson geoshape = new GeoShapeJson();
            geoshape.Id = digiwaydata.id.ToLower();
            geoshape.Name = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).ROUTE_NAME;
            geoshape.Type = type;
            geoshape.Source = "civis.geoserver";

            //Transform to geometry
            var geoJson = JsonConvert.SerializeObject(digiwaydata.geometry);

            var serializer = GeoJsonSerializer.Create();
            using (var stringReader = new StringReader(geoJson))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                geoshape.Geometry = serializer.Deserialize<Geometry>(jsonReader);
            }            

            //get first point of geometry
            var geomfactory = new GeometryFactory();
            var point = geomfactory.WithSRID(32632).CreatePoint(geoshape.Geometry.Coordinates.FirstOrDefault());

            UniversalTransverseMercator utm = new UniversalTransverseMercator("32N", point.X, point.Y);
            CoordinateSharp.Coordinate latlong = UniversalTransverseMercator.ConvertUTMtoLatLong(utm);

            var gpsinfo = new GpsInfo()
            {
                Altitude = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).START_HEIGHT,
                AltitudeUnitofMeasure = "m",
                Gpstype = "position",
                //Use only first digits otherwise point and track will differ
                Latitude = Math.Round(latlong.Latitude.DecimalDegree, 9),
                Longitude = Math.Round(latlong.Longitude.DecimalDegree, 9)
            };

            return (geoshape, gpsinfo);
        }

        private static (ODHActivityPoiLinked, GeoShapeJson) ParseCyclingRoutesTyrolToODHActivityPoi(
            ODHActivityPoiLinked? odhactivitypoi,
            GeoserverCivisData digiwaydata
        )
        {
            if(odhactivitypoi == null)
                odhactivitypoi = new ODHActivityPoiLinked();

            odhactivitypoi.Id = digiwaydata.id.ToLower();
            odhactivitypoi.Active = true;
            odhactivitypoi.FirstImport = Convert.ToDateTime(((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).CREATE_DATE);
            odhactivitypoi.LastChange = Convert.ToDateTime(((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).UPDATE_DATE);
            odhactivitypoi.HasLanguage = new List<string>() { "de" };
            odhactivitypoi.Shortname = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).ROUTE_NAME;
            odhactivitypoi.Detail = new Dictionary<string, Detail>();
            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("de", new Detail()
            {
                Title = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).ROUTE_NAME,
                BaseText = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).ROUTE_DESC,
                Header = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).ROUTE_TYPE,
                AdditionalText = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).ROUTE_NUMBER,
                Language = "de"
            });
            odhactivitypoi.ContactInfos = new Dictionary<string, ContactInfos>();
            odhactivitypoi.ContactInfos.TryAddOrUpdate<string, ContactInfos>("de", new ContactInfos()
            {
                City = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).MUNICIPALITY,
                Region = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).REGION,
                Language = "de"
            });
            odhactivitypoi.DistanceLength = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).LENGTH;
            odhactivitypoi.Difficulty = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).DIFFICULTY;
            odhactivitypoi.AltitudeSumDown = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).DOWNHILL_METERS;
            odhactivitypoi.AltitudeSumUp = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).UPHILL_METERS;
            odhactivitypoi.Source = "civis.geoserver";
            odhactivitypoi.SyncSourceInterface = "geoservices1.civis.bz.it";
            odhactivitypoi.DistanceDuration = TransformDuration(((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).RUNNING_TIME);

            //Add Tags
            odhactivitypoi.TagIds = new List<string>();
            odhactivitypoi.TagIds.Add("2C1D1E0CA4E849229DA90775CBBF2312"); //LTS Cycling Tag
            odhactivitypoi.TagIds.Add("cycling");
            odhactivitypoi.TagIds.Add("biking biking tours");
           
            Dictionary<string, string> additionalvalues = new Dictionary<string, string>();
            additionalvalues.Add("object", ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).OBJECT);
            additionalvalues.Add("route_number", ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).ROUTE_NUMBER);
            additionalvalues.Add("id", ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).ID.ToString());
            additionalvalues.Add("route_type", ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).ROUTE_TYPE);
            var bboxformatted = digiwaydata.bbox.Select(d => d.ToString(CultureInfo.InvariantCulture)).ToList();

            additionalvalues.Add("bbox", "[" + String.Join(",", bboxformatted) + "]");


            var georesult = ParseGeoServerGeodataToGeoShapeJson(digiwaydata, "cycleway");

            var geoshape = georesult.Item1;
            geoshape.Mapping.TryAddOrUpdate("civis.geoserver", additionalvalues);          
          

            //Add Starting GPS Coordinate as GPS Point 
            odhactivitypoi.GpsInfo = new List<GpsInfo>();
            odhactivitypoi.GpsInfo.Add(georesult.Item2);

            return (odhactivitypoi, geoshape);
        }

        private static (ODHActivityPoiLinked, GeoShapeJson) ParseMTBRoutesToODHActivityPoi(
            ODHActivityPoiLinked? odhactivitypoi,
            GeoserverCivisData digiwaydata
        )
        {
            if (odhactivitypoi == null)
                odhactivitypoi = new ODHActivityPoiLinked();

            odhactivitypoi.Id = digiwaydata.id.ToLower();
            odhactivitypoi.Active = true;
            odhactivitypoi.FirstImport = odhactivitypoi != null ? odhactivitypoi.FirstImport : DateTime.Now;
            odhactivitypoi.LastChange = DateTime.Now;
            odhactivitypoi.HasLanguage = new List<string>() { "de","it" };
            odhactivitypoi.Shortname = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_NAME_DE;
            odhactivitypoi.Number = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_CODE;
            //odhactivitypoi.WayNumber = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_CODE;
            odhactivitypoi.Detail = new Dictionary<string, Detail>();
            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("de", new Detail()
            {
                Title = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_NAME_DE,
                BaseText = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_TEXT_DE,                
                AdditionalText = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_LINK_DE,
                SafetyInfo = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_DIFF_DE,
                Language = "de"
            });
            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("it", new Detail()
            {
                Title = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_NAME_IT,
                BaseText = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_TEXT_IT,
                AdditionalText = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_LINK_IT,
                SafetyInfo = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_DIFF_IT,
                Language = "it"
            });
            

            //odhactivitypoi.ContactInfos = new Dictionary<string, ContactInfos>();
            //odhactivitypoi.ContactInfos.TryAddOrUpdate<string, ContactInfos>("de", new ContactInfos()
            //{
            //    City = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MUNICIPALITY,
            //    Region = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).REGION
            //});
            //odhactivitypoi.DistanceLength = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).LENGTH;
            //odhactivitypoi.Difficulty = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).DIFFICULTY;
            //odhactivitypoi.AltitudeSumDown = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).DOWNHILL_METERS;
            //odhactivitypoi.AltitudeSumUp = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).UPHILL_METERS;
            //odhactivitypoi.DistanceDuration = TransformDuration(((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).RUNNING_TIME);



            odhactivitypoi.Difficulty = TransformMTBDifficulty(((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_DIFF);
            odhactivitypoi.Ratings = new Ratings() { Difficulty = odhactivitypoi.Difficulty };

            odhactivitypoi.Source = "civis.geoserver";
            odhactivitypoi.SyncSourceInterface = "geoservices1.civis.bz.it";
            

            //Add Tags
            odhactivitypoi.TagIds = new List<string>();
            odhactivitypoi.TagIds.Add("1B9AF4DA6E3A414798890E6723E71EC8"); //LTS MTB Tag
            odhactivitypoi.TagIds.Add("cycling");
            odhactivitypoi.TagIds.Add("mountain bike"); 
            odhactivitypoi.TagIds.Add("mountain bikes");

            //Add Related Content if there is a LTS ID
            if (!String.IsNullOrEmpty(((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_LTS_RID))
            {
                odhactivitypoi.RelatedContent = new List<RelatedContent>();
                RelatedContent relatedContent = new RelatedContent();
                relatedContent.Id = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_LTS_RID;
                relatedContent.Type = "odhactivitypoi";
                odhactivitypoi.RelatedContent.Add(relatedContent);
            }

            Dictionary<string, string> additionalvalues = new Dictionary<string, string>();
            additionalvalues.Add("code", ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_CODE);
            additionalvalues.Add("diff", ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_DIFF);
            additionalvalues.Add("id", ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_ID.ToString());
            additionalvalues.Add("lts_rid", ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_LTS_RID);            
            additionalvalues.Add("length_geom", ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).LENGTH_GEOM.ToString());
            additionalvalues.Add("single", ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_SINGLE_DE.ToString() == "JA" ? "true" : "false" );
            var bboxformatted = digiwaydata.bbox.Select(d => d.ToString(CultureInfo.InvariantCulture)).ToList();

            additionalvalues.Add("bbox", "[" + String.Join(",", bboxformatted) + "]");

           

            var georesult = ParseGeoServerGeodataToGeoShapeJson(digiwaydata, "mountainbikeroute");

            var geoshape = georesult.Item1;
            geoshape.Mapping.TryAddOrUpdate("civis.geoserver", additionalvalues);

            //Add Starting GPS Coordinate as GPS Point 
            odhactivitypoi.GpsInfo = new List<GpsInfo>();
            odhactivitypoi.GpsInfo.Add(georesult.Item2);

            return (odhactivitypoi, geoshape);
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
                    var minute = hour[1].Replace("min", "");

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
                    case "MTB_S": return "6";
                    case "MTB_M": return "3";
                    case "MTB_L": return "1";
                    default: return null;
                }
            }
        }
    }

    #region obsolete code

    //PArsing errors
    //List<Coordinate> coordinates = new List<Coordinate>();

    //string coordinatesstr = "MULTILINESTRING((";
    //foreach(var coordinate1 in digiwaydata.geometry.coordinates)
    //{
    //    foreach (var coordinate2 in coordinate1)
    //    {
    //        //List<Coordinate> coordinates = new List<Coordinate>();

    //        List<double> coords = new List<double>();

    //        foreach (var coordinate in coordinate2)
    //        {
    //            coords.Add(coordinate);
    //            coordinatesstr = coordinatesstr + coordinate.ToString(CultureInfo.InvariantCulture) + " ";
    //        }

    //        if(coords.Count == 2)
    //            coordinates.Add(new Coordinate(coords[0], coords[1]));

    //        coordinatesstr = coordinatesstr.Remove(coordinatesstr.Length - 1);
    //        coordinatesstr = coordinatesstr + ",";                                       
    //    }
    //    coordinatesstr = coordinatesstr.Remove(coordinatesstr.Length - 1);
    //}
    //coordinatesstr = coordinatesstr + "))";

    //WKTReader reader = new WKTReader();
    //geoshape.Geometry = reader.Read(coordinatesstr);

    //var geomfactory = new GeometryFactory();
    //var point = geomfactory.WithSRID(32632).CreatePoint(coordinates.FirstOrDefault());
    //var SourceCoordSystem = new CoordinateSystemFactory().

    ////Convert the coordinate system to WGS84.
    //var transform = new ProjNet.CoordinateSystems.Transformations.CoordinateTransformationFactory().CreateFromCoordinateSystems(
    //          ProjNet.CoordinateSystems.GeocentricCoordinateSystem.WGS84,
    //       ProjNet.CoordinateSystems.GeographicCoordinateSystem.WGS84);

    //var wgs84Point = transform.MathTransform.Transform(new double[] { point.Coordinate.X, point.Coordinate.Y });

    //var lat = wgs84Point[1];
    //var lon = wgs84Point[0];


    #endregion
}
