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
    public class ParseCyclingRoutesToODHActivityPoi
    {

        public static (ODHActivityPoiLinked, GeoShapeJsonTest) ParseDigiWayCyclingRoutesToODHActivityPoi(
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
                AdditionalText = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).ROUTE_NUMBER
            });
            odhactivitypoi.ContactInfos = new Dictionary<string, ContactInfos>();
            odhactivitypoi.ContactInfos.TryAddOrUpdate<string, ContactInfos>("de", new ContactInfos()
            {
                City = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).MUNICIPALITY,
                Region = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).REGION                
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
           
            GeoShapeJsonTest geoshape = new GeoShapeJsonTest();            
            geoshape.Id = digiwaydata.id.ToLower();
            geoshape.Name = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).ROUTE_NAME;
            geoshape.Type = "cycleway";
            geoshape.Source = "civis.geoserver";

            Dictionary<string, string> additionalvalues = new Dictionary<string, string>();
            additionalvalues.Add("object", ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).OBJECT);
            additionalvalues.Add("route_number", ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).ROUTE_NUMBER);
            additionalvalues.Add("id", ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).ID.ToString());
            additionalvalues.Add("route_type", ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).ROUTE_TYPE);
            var bboxformatted = digiwaydata.bbox.Select(d => d.ToString(CultureInfo.InvariantCulture)).ToList();

            additionalvalues.Add("bbox", "[" + String.Join(",", bboxformatted) + "]");

            geoshape.Mapping.TryAddOrUpdate("civis.geoserver", additionalvalues);


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


            //var SourceCoordSystem = new CoordinateSystemFactory().

            ////Convert the coordinate system to WGS84.
            //var transform = new ProjNet.CoordinateSystems.Transformations.CoordinateTransformationFactory().CreateFromCoordinateSystems(
            //          ProjNet.CoordinateSystems.GeocentricCoordinateSystem.WGS84,
            //       ProjNet.CoordinateSystems.GeographicCoordinateSystem.WGS84);

            //var wgs84Point = transform.MathTransform.Transform(new double[] { point.Coordinate.X, point.Coordinate.Y });

            //var lat = wgs84Point[1];
            //var lon = wgs84Point[0];


            //Add Starting GPS Coordinate as GPS Point 
            odhactivitypoi.GpsInfo = new List<GpsInfo>();
            odhactivitypoi.GpsInfo.Add(new GpsInfo()
            {
                Altitude = ((GeoserverCivisPropertiesCycleWay)digiwaydata.properties).START_HEIGHT,
                AltitudeUnitofMeasure = "m",
                Gpstype = "position",
                //Use only first digits otherwise point and track will differ
                Latitude = Math.Round(latlong.Latitude.DecimalDegree,9), 
                Longitude = Math.Round(latlong.Longitude.DecimalDegree,9)
            });



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
    }
}
