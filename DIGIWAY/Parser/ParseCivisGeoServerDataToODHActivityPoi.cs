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

namespace DIGIWAY
{
    public class ParseCivisGeoServerDataToODHActivityPoi
    {
        public static (ODHActivityPoiLinked, GeoShapeJson) ParseToODHActivityPoi(
            ODHActivityPoiLinked? odhactivitypoi,
            IGeoServerCivisData digiwaydata,
            string identifier,
            string source
        )
        {
            var result = identifier switch
            {
                "cyclewaystyrol" => ParseCyclingRoutesTyrolToODHActivityPoi(odhactivitypoi, digiwaydata as GeoServerCivisData, identifier, source),
                "mountainbikeroutes" => ParseMTBRoutesToODHActivityPoi(odhactivitypoi, digiwaydata as GeoServerCivisData, identifier, source),
                "hikingtrails" => ParseHikingTrailsToODHActivityPoi(odhactivitypoi, digiwaydata as GeoServerCivisData, identifier, source),
                "intermunicipalcyclingroutes" => ParseInterMunicipalCyclingRoutesToODHActivityPoi(odhactivitypoi, digiwaydata as GeoServerCivisData, identifier, source),
                "_" => (null,null)
            };

            return result;
        }
   
        private static (GeoShapeJson, GpsInfo) ParseGeoServerGeodataToGeoShapeJson(IGeoServerCivisData digiwaydata, string name, string identifier, string geoshapetype, string source, int? altitude)
        {
            GeoShapeJson geoshape = new GeoShapeJson();
            geoshape.Id = digiwaydata.id.ToLower();
            geoshape.Name = name;
            geoshape.Type = geoshapetype;
            geoshape.Source = source;
            geoshape.Geometry = digiwaydata.geometry;
            

            //get first point of geometry
            var geomfactory = new GeometryFactory();
            var point = geomfactory.WithSRID(32632).CreatePoint(geoshape.Geometry.Coordinates.FirstOrDefault());

            UniversalTransverseMercator utm = new UniversalTransverseMercator("32N", point.X, point.Y);
            CoordinateSharp.Coordinate latlong = UniversalTransverseMercator.ConvertUTMtoLatLong(utm);

            var gpsinfo = new GpsInfo()
            {
                Altitude = altitude,
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
            IGeoServerCivisData digiwaydata, 
            string identifier,
            string source
        )
        {
            if(odhactivitypoi == null)
                odhactivitypoi = new ODHActivityPoiLinked();

            odhactivitypoi.Id = digiwaydata.id.ToLower();
            odhactivitypoi.Active = true;
            odhactivitypoi.FirstImport = digiwaydata.properties.CREATE_DATE != null ? Convert.ToDateTime(digiwaydata.properties.CREATE_DATE) : odhactivitypoi == null ? DateTime.Now : odhactivitypoi.FirstImport;
            odhactivitypoi.LastChange = digiwaydata.properties.UPDATE_DATE != null ? Convert.ToDateTime(digiwaydata.properties.UPDATE_DATE) : DateTime.Now;
            odhactivitypoi.HasLanguage = new List<string>() { "de" };
            odhactivitypoi.Shortname = digiwaydata.properties.ROUTE_NAME != null ? Convert.ToString(digiwaydata.properties.ROUTE_NAME) : null;
            odhactivitypoi.Detail = new Dictionary<string, Detail>();
            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("de", new Detail()
            {
                Title = digiwaydata.properties.ROUTE_NAME != null ? Convert.ToString(digiwaydata.properties.ROUTE_NAME) : null,
                BaseText = digiwaydata.properties.ROUTE_DESC != null ? Convert.ToString(digiwaydata.properties.ROUTE_DESC) : null,
                Header = digiwaydata.properties.ROUTE_TYPE != null ? Convert.ToString(digiwaydata.properties.ROUTE_TYPE) : null,
                AdditionalText = digiwaydata.properties.ROUTE_NUMBER != null ? Convert.ToString(digiwaydata.properties.ROUTE_NUMBER) : null,
                Language = "de"
            });
            odhactivitypoi.ContactInfos = new Dictionary<string, ContactInfos>();
            odhactivitypoi.ContactInfos.TryAddOrUpdate<string, ContactInfos>("de", new ContactInfos()
            {
                City = digiwaydata.properties.MUNICIPALITY != null ? Convert.ToString(digiwaydata.properties.MUNICIPALITY) : null,
                Region = digiwaydata.properties.REGION != null ? Convert.ToString(digiwaydata.properties.REGION) : null,
                Language = "de"
            });
            odhactivitypoi.DistanceLength = digiwaydata.properties.LENGTH != null ? Convert.ToDouble(digiwaydata.properties.LENGTH) : null;
            odhactivitypoi.Difficulty = digiwaydata.properties.DIFFICULTY != null ? Convert.ToString(digiwaydata.properties.DIFFICULTY) : null;

            if(odhactivitypoi.Difficulty != null)
                odhactivitypoi.Ratings = new Ratings() { Difficulty = odhactivitypoi.Difficulty };

            odhactivitypoi.AltitudeSumDown = digiwaydata.properties.DOWNHILL_METERS != null ? Convert.ToDouble(digiwaydata.properties.DOWNHILL_METERS) : null;
            odhactivitypoi.AltitudeSumUp = digiwaydata.properties.UPHILL_METERS != null ? Convert.ToDouble(digiwaydata.properties.UPHILL_METERS) : null;
            odhactivitypoi.Source = source;
            odhactivitypoi.SyncSourceInterface = source + "." + identifier;
            odhactivitypoi.DistanceDuration = digiwaydata.properties.RUNNING_TIME != null ? TransformDuration(Convert.ToString(digiwaydata.properties.RUNNING_TIME)) : null;

            //Add Tags
            odhactivitypoi.TagIds = new List<string>();
            odhactivitypoi.TagIds.Add("2C1D1E0CA4E849229DA90775CBBF2312"); //LTS Cycling Tag
            odhactivitypoi.TagIds.Add("cycling");
            odhactivitypoi.TagIds.Add("biking biking tours");
           
            Dictionary<string, string> additionalvalues = new Dictionary<string, string>();
            if(digiwaydata.properties.OBJECT != null)
                additionalvalues.Add("object", Convert.ToString(digiwaydata.properties.OBJECT));
            if (digiwaydata.properties.ROUTE_NUMBER != null)
                additionalvalues.Add("route_number", Convert.ToString(digiwaydata.properties.ROUTE_NUMBER));
            if (digiwaydata.properties.ID != null)
                additionalvalues.Add("id", Convert.ToString(digiwaydata.properties.ID));
            if (digiwaydata.properties.ROUTE_TYPE != null)
                additionalvalues.Add("route_type", Convert.ToString(digiwaydata.properties.ROUTE_TYPE));


            if (digiwaydata.properties.ROUTE_START != null)
                additionalvalues.Add("route_start", Convert.ToString(digiwaydata.properties.ROUTE_START));
            if (digiwaydata.properties.ROUTE_END != null)
                additionalvalues.Add("route_end", Convert.ToString(digiwaydata.properties.ROUTE_END));
            if (digiwaydata.properties.STATUS != null)
                additionalvalues.Add("status", Convert.ToString(digiwaydata.properties.STATUS));
            if (digiwaydata.properties.STATUS_DATE != null)
                additionalvalues.Add("status_date", Convert.ToString(digiwaydata.properties.STATUS_DATE));

            //Add ROUTE_START / ROUTE_END

            var bboxformatted = digiwaydata.bbox.Select(d => d.ToString(CultureInfo.InvariantCulture)).ToList();

            additionalvalues.Add("bbox", "[" + String.Join(",", bboxformatted) + "]");


            var georesult = ParseGeoServerGeodataToGeoShapeJson(
                digiwaydata,                
                digiwaydata.properties.ROUTE_NAME != null ? Convert.ToString(digiwaydata.properties.ROUTE_NAME) : null,
                identifier,
                "cycleway",
                source,
                digiwaydata.properties.START_HEIGHT != null ? Convert.ToInt16(digiwaydata.properties.START_HEIGHT) : null
                );

            odhactivitypoi.Mapping.TryAddOrUpdate(source, additionalvalues);

            //Add Starting GPS Coordinate as GPS Point 
            odhactivitypoi.GpsInfo = new List<GpsInfo>() { georesult.Item2 };

            return (odhactivitypoi, georesult.Item1);
        }

        private static (ODHActivityPoiLinked, GeoShapeJson) ParseMTBRoutesToODHActivityPoi(
            ODHActivityPoiLinked? odhactivitypoi,            
            IGeoServerCivisData digiwaydata,
            string identifier,
            string source
        )
        {
            if (odhactivitypoi == null)
                odhactivitypoi = new ODHActivityPoiLinked();

            string name = digiwaydata.properties.MTB_NAME_DE != null ? Convert.ToString(digiwaydata.properties.MTB_NAME_DE) : digiwaydata.properties.MTB_CODE != null ? Convert.ToString(digiwaydata.properties.MTB_CODE) : "";

            odhactivitypoi.Id = digiwaydata.id.ToLower();
            odhactivitypoi.Active = true;
            odhactivitypoi.FirstImport = odhactivitypoi != null ? odhactivitypoi.FirstImport : DateTime.Now;
            odhactivitypoi.LastChange = DateTime.Now;
            odhactivitypoi.HasLanguage = new List<string>() { "de","it" };
            odhactivitypoi.Shortname = name;
            odhactivitypoi.Number = digiwaydata.properties.MTB_CODE != null ? Convert.ToString(digiwaydata.properties.MTB_CODE) : null;
            //odhactivitypoi.WayNumber = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_CODE;
            odhactivitypoi.Detail = new Dictionary<string, Detail>();
            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("de", new Detail()
            {
                Title = digiwaydata.properties.MTB_NAME_DE != null ? Convert.ToString(digiwaydata.properties.MTB_NAME_DE) : null,
                BaseText = digiwaydata.properties.MTB_TEXT_DE != null ? Convert.ToString(digiwaydata.properties.MTB_TEXT_DE) : null,                
                AdditionalText = digiwaydata.properties.MTB_LINK_DE != null ? Convert.ToString(digiwaydata.properties.MTB_LINK_DE) : null,
                SafetyInfo = digiwaydata.properties.MTB_DIFF_DE != null ? Convert.ToString(digiwaydata.properties.MTB_DIFF_DE) : null,
                Language = "de"
            });
            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("it", new Detail()
            {
                Title = digiwaydata.properties.MTB_NAME_IT != null ? Convert.ToString(digiwaydata.properties.MTB_NAME_IT) : null,
                BaseText = digiwaydata.properties.MTB_TEXT_IT != null ? Convert.ToString(digiwaydata.properties.MTB_TEXT_IT) : null,
                AdditionalText = digiwaydata.properties.MTB_LINK_IT != null ? Convert.ToString(digiwaydata.properties.MTB_LINK_IT) : null,
                SafetyInfo = digiwaydata.properties.MTB_DIFF_IT != null ? Convert.ToString(digiwaydata.properties.MTB_DIFF_IT) : null,
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



            odhactivitypoi.Difficulty = digiwaydata.properties.MTB_NAME_DE != null ? TransformMTBDifficulty(Convert.ToString(digiwaydata.properties.MTB_DIFF)) : null;
            odhactivitypoi.Ratings = new Ratings() { Difficulty = odhactivitypoi.Difficulty };

            odhactivitypoi.Source = source;
            odhactivitypoi.SyncSourceInterface = source + "." + identifier;
            

            //Add Tags
            odhactivitypoi.TagIds = new List<string>();
            odhactivitypoi.TagIds.Add("1B9AF4DA6E3A414798890E6723E71EC8"); //LTS MTB Tag
            odhactivitypoi.TagIds.Add("cycling");
            odhactivitypoi.TagIds.Add("mountain bike"); 
            odhactivitypoi.TagIds.Add("mountain bikes");

            //Add Related Content if there is a LTS ID
            if (digiwaydata.properties.MTB_LTS_RID != null && !String.IsNullOrEmpty(Convert.ToString(digiwaydata.properties.MTB_LTS_RID)))
            {
                odhactivitypoi.RelatedContent = new List<RelatedContent>();
                RelatedContent relatedContent = new RelatedContent();
                relatedContent.Id = Convert.ToString(digiwaydata.properties.MTB_LTS_RID);
                relatedContent.Type = "odhactivitypoi";
                odhactivitypoi.RelatedContent.Add(relatedContent);
            }

            Dictionary<string, string> additionalvalues = new Dictionary<string, string>();
            if(digiwaydata.properties.MTB_CODE != null)
                additionalvalues.Add("code", Convert.ToString(digiwaydata.properties.MTB_CODE));
            if (digiwaydata.properties.MTB_DIFF != null)
                additionalvalues.Add("diff", Convert.ToString(digiwaydata.properties.MTB_DIFF));
            if (digiwaydata.properties.MTB_ID != null)
                additionalvalues.Add("id", Convert.ToString(digiwaydata.properties.MTB_ID));
            if (digiwaydata.properties.MTB_LTS_RID != null)
                additionalvalues.Add("lts_rid", Convert.ToString(digiwaydata.properties.MTB_LTS_RID));
            if (digiwaydata.properties.LENGTH_GEOM != null)
                additionalvalues.Add("length_geom", Convert.ToString(digiwaydata.properties.LENGTH_GEOM));
            if (digiwaydata.properties.MTB_SINGLE_DE != null)
                additionalvalues.Add("single", Convert.ToString(digiwaydata.properties.MTB_SINGLE_DE) == "JA" ? "true" : "false" );
            
            var bboxformatted = digiwaydata.bbox.Select(d => d.ToString(CultureInfo.InvariantCulture)).ToList();
            additionalvalues.Add("bbox", "[" + String.Join(",", bboxformatted) + "]");

           
            var georesult = ParseGeoServerGeodataToGeoShapeJson(
                digiwaydata,                
                name,
                identifier,
                 "mountainbikeroute",
                source,
                null
                );

            odhactivitypoi.Mapping.TryAddOrUpdate(source, additionalvalues);

            //Add Starting GPS Coordinate as GPS Point 
            odhactivitypoi.GpsInfo = new List<GpsInfo>();
            odhactivitypoi.GpsInfo.Add(georesult.Item2);

            return (odhactivitypoi, georesult.Item1);
        }

        private static (ODHActivityPoiLinked, GeoShapeJson) ParseHikingTrailsToODHActivityPoi(
            ODHActivityPoiLinked? odhactivitypoi,
            IGeoServerCivisData digiwaydata,
            string identifier,
            string source
        )
        {
            if (odhactivitypoi == null)
                odhactivitypoi = new ODHActivityPoiLinked();

            string name = digiwaydata.properties.NAME == null ? Convert.ToString(digiwaydata.properties.CODE_NAME) : Convert.ToString(digiwaydata.properties.NAME);

            odhactivitypoi.Id = digiwaydata.id.ToLower();
            odhactivitypoi.Active = true;
            odhactivitypoi.FirstImport = odhactivitypoi != null ? odhactivitypoi.FirstImport : DateTime.Now;
            odhactivitypoi.LastChange = DateTime.Now;
            odhactivitypoi.HasLanguage = new List<string>() { "de" };
            odhactivitypoi.Shortname = name;
            odhactivitypoi.Number = digiwaydata.properties.CODE;
            //odhactivitypoi.WayNumber = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_CODE;
            odhactivitypoi.Detail = new Dictionary<string, Detail>();
            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("de", new Detail()
            {
                Title = name,                
                AdditionalText = Convert.ToString(digiwaydata.properties.CODE_NAME),
                Language = "de"
            });
           
            //odhactivitypoi.ContactInfos = new Dictionary<string, ContactInfos>();
            //odhactivitypoi.ContactInfos.TryAddOrUpdate<string, ContactInfos>("de", new ContactInfos()
            //{
            //    City = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MUNICIPALITY,
            //    Region = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).REGION
            //});
            odhactivitypoi.DistanceLength = Convert.ToDouble(digiwaydata.properties.LENGTH_GEOM);
            //odhactivitypoi.Difficulty = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).DIFFICULTY;
            //odhactivitypoi.AltitudeSumDown = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).DOWNHILL_METERS;
            //odhactivitypoi.AltitudeSumUp = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).UPHILL_METERS;
            //odhactivitypoi.DistanceDuration = TransformDuration(((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).RUNNING_TIME);
            
            odhactivitypoi.Source = source;
            odhactivitypoi.SyncSourceInterface = source + "." + identifier;


            //Add Tags
            odhactivitypoi.TagIds = new List<string>();
            odhactivitypoi.TagIds.Add("978F89296ACB4DB4B6BD1C269341802F"); //LTS Hiking Tag
            odhactivitypoi.TagIds.Add("hiking");
            odhactivitypoi.TagIds.Add("B702CF3773CF4A47AFEBC291618A7B7E"); //LTS Other hikes Tag
            odhactivitypoi.TagIds.Add("other hikes");
            

            Dictionary<string, string> additionalvalues = new Dictionary<string, string>();
            if(digiwaydata.properties.CODE != null)
                additionalvalues.Add("code",  Convert.ToString(digiwaydata.properties.CODE));
            if(digiwaydata.properties.CODE_NAME != null)
                additionalvalues.Add("code_name", Convert.ToString(digiwaydata.properties.CODE_NAME));
            if(digiwaydata.properties.ID != null)
                additionalvalues.Add("id",  Convert.ToString(digiwaydata.properties.ID));
            if(digiwaydata.properties.LENGTH_GEOM != null)
                additionalvalues.Add("length_geom", Convert.ToString(digiwaydata.properties.LENGTH_GEOM));

            var bboxformatted = digiwaydata.bbox.Select(d => d.ToString(CultureInfo.InvariantCulture)).ToList();

            additionalvalues.Add("bbox", "[" + String.Join(",", bboxformatted) + "]");

            var georesult = ParseGeoServerGeodataToGeoShapeJson(
                digiwaydata,                
                name,
                identifier,
                "hikingtrail",
                source,
                null
                );

            odhactivitypoi.Mapping.TryAddOrUpdate(source, additionalvalues);

            //Add Starting GPS Coordinate as GPS Point 
            odhactivitypoi.GpsInfo = new List<GpsInfo>() { georesult.Item2 };            

            return (odhactivitypoi, georesult.Item1);
        }

        private static (ODHActivityPoiLinked, GeoShapeJson) ParseInterMunicipalCyclingRoutesToODHActivityPoi(
            ODHActivityPoiLinked? odhactivitypoi,
            IGeoServerCivisData digiwaydata, 
            string identifier,
            string source
        )
        {
            if (odhactivitypoi == null)
                odhactivitypoi = new ODHActivityPoiLinked();

            string name = digiwaydata.properties.NAME_DE != null ? Convert.ToString(digiwaydata.properties.NAME_DE) : digiwaydata.properties.CODE != null ? Convert.ToString(digiwaydata.properties.CODE) : "";

            odhactivitypoi.Id = digiwaydata.id.ToLower();
            odhactivitypoi.Active = true;
            odhactivitypoi.FirstImport = odhactivitypoi != null ? odhactivitypoi.FirstImport : DateTime.Now;
            odhactivitypoi.LastChange = DateTime.Now;
            odhactivitypoi.HasLanguage = new List<string>() { "de", "it" };
            odhactivitypoi.Shortname = digiwaydata.properties.NAME_DE != null ? Convert.ToString(digiwaydata.properties.NAME_DE) : null;
            odhactivitypoi.Number = digiwaydata.properties.CODE !=null ? Convert.ToString(digiwaydata.properties.CODE) : null;
            //odhactivitypoi.WayNumber = ((GeoserverCivisPropertiesMountainBike)digiwaydata.properties).MTB_CODE;
            odhactivitypoi.Detail = new Dictionary<string, Detail>();
            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("de", new Detail()
            {
                Title = digiwaydata.properties.NAME_DE != null ? Convert.ToString(digiwaydata.properties.NAME_DE) : null,                
                AdditionalText = digiwaydata.properties.DISTRICT_DE != null ? Convert.ToString(digiwaydata.properties.DISTRICT_DE) : null,                
                Language = "de"
            });
            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("it", new Detail()
            {
                Title = digiwaydata.properties.NAME_IT != null ? Convert.ToString(digiwaydata.properties.NAME_IT) : null,                
                AdditionalText = digiwaydata.properties.DISTRICT_IT != null ? Convert.ToString(digiwaydata.properties.DISTRICT_IT) : null,                
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



            //odhactivitypoi.Difficulty = TransformMTBDifficulty(digiwaydata.properties.MTB_DIFF);
            //odhactivitypoi.Ratings = new Ratings() { Difficulty = odhactivitypoi.Difficulty };

            odhactivitypoi.Source = source;
            odhactivitypoi.SyncSourceInterface = source + "." + identifier;


            //Add Tags
            odhactivitypoi.TagIds = new List<string>();
            odhactivitypoi.TagIds.Add("9DE2F99EA67E4278A558755E093DB0ED"); //LTS Others bike Tag
            odhactivitypoi.TagIds.Add("cycling");
            odhactivitypoi.TagIds.Add("others bike");
            odhactivitypoi.TagIds.Add("B015F1EA92494EB1B6E32170269000B0");  //LTS RAdtouren Tag     

            Dictionary<string, string> additionalvalues = new Dictionary<string, string>();
            if(digiwaydata.properties.CODE != null)
                additionalvalues.Add("code", Convert.ToString(digiwaydata.properties.CODE));
            if (digiwaydata.properties.ID != null)
                additionalvalues.Add("id", Convert.ToString(digiwaydata.properties.ID));
            if (digiwaydata.properties.LENGTH_GEOM != null)
                additionalvalues.Add("length_geom", Convert.ToString(digiwaydata.properties.LENGTH_GEOM));

            var bboxformatted = digiwaydata.bbox.Select(d => d.ToString(CultureInfo.InvariantCulture)).ToList();

            additionalvalues.Add("bbox", "[" + String.Join(",", bboxformatted) + "]");

            var georesult = ParseGeoServerGeodataToGeoShapeJson(
                digiwaydata,                
                name,
                identifier,
                "intermunicipalcycleway",
                source,
                null
                );

            odhactivitypoi.Mapping.TryAddOrUpdate(source, additionalvalues);

            //Add Starting GPS Coordinate as GPS Point 
            odhactivitypoi.GpsInfo = new List<GpsInfo>();
            odhactivitypoi.GpsInfo.Add(georesult.Item2);

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
}
