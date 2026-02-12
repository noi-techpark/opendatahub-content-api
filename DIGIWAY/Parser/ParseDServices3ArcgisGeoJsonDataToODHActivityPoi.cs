// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using CoordinateSharp;
using DataModel;
using DIGIWAY.Model.GeoJsonReadModel;
using Helper;
using Helper.Extensions;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Densify;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGIWAY
{
    public class ParseDServices3ArcgisGeoJsonDataToODHActivityPoi
    {
        public static (ODHActivityPoiLinked, GeoShapeJson) ParseToODHActivityPoi(
            ODHActivityPoiLinked? odhactivitypoi,
            GeoJsonFeature digiwaydata,
            string identifier,
            string source,
            string srid
        )
        {
            var result = identifier switch
            {
                "accessibletrails_austria" => ParseAccessibleTrailsAustriaToODHActivityPoi(odhactivitypoi, digiwaydata, identifier, source, srid),
                "_" => (null, null)
            };

            return result;
        }

        private static (GeoShapeJson, GpsInfo) ParseGeoServerGeodataToGeoShapeJson(GeoJsonFeature digiwaydata, string name, string identifier, string geoshapetype, string source, int? altitude, string srid)
        {
            GeoShapeJson geoshape = new GeoShapeJson();
            geoshape.Id = "urn:digiway:dservices3arcgiscom:" + identifier + ":" + digiwaydata.Attributes["OBJECTID"].ToString().ToLower();
            geoshape.Name = name;
            geoshape.Type = geoshapetype;
            geoshape.Source = source;
            geoshape.Geometry = digiwaydata.Geometry;

            //get first point of geometry
            var geomfactory = new GeometryFactory();
            var point = geoshape.Geometry.Coordinates.FirstOrDefault();

            var gpsinfo = new GpsInfo()
            {
                Altitude = altitude,
                AltitudeUnitofMeasure = "m",
                Gpstype = "position",
                //Use only first digits otherwise point and track will differ
                Latitude = point.Y,
                Longitude = point.X
            };

            return (geoshape, gpsinfo);
        }


        private static (ODHActivityPoiLinked, GeoShapeJson) ParseAccessibleTrailsAustriaToODHActivityPoi(
            ODHActivityPoiLinked? odhactivitypoi,
            GeoJsonFeature digiwaydata,
            string identifier,
            string source,
            string srid
        )
        {
            if (odhactivitypoi == null)
                odhactivitypoi = new ODHActivityPoiLinked();

            odhactivitypoi.Id = "urn:digiway:dservices3arcgiscom:" + identifier + ":" + digiwaydata.Attributes["OBJECTID"].ToString().ToLower();

            odhactivitypoi.Active = true;
            odhactivitypoi.FirstImport = digiwaydata.Attributes["UPDATETIMESTAMP"] != null ? Convert.ToDateTime(digiwaydata.Attributes["UPDATETIMESTAMP"].ToString()) : odhactivitypoi == null ? DateTime.Now : odhactivitypoi.FirstImport;
            odhactivitypoi.LastChange = digiwaydata.Attributes["UPDATETIMESTAMP"] != null ? Convert.ToDateTime(digiwaydata.Attributes["UPDATETIMESTAMP"].ToString()) : DateTime.Now;
            odhactivitypoi.HasLanguage = new List<string>() { "de" };
            odhactivitypoi.Shortname = digiwaydata.Attributes["NAME"] != null ? digiwaydata.Attributes["NAME"].ToString() : null;
            odhactivitypoi.Detail = new Dictionary<string, Detail>();
            
            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("de", new Detail()
            {
                Title = digiwaydata.Attributes["NAME"].ToString() != null ? digiwaydata.Attributes["NAME"].ToString() : null,
                AdditionalText = digiwaydata.Attributes["ROUTENNUMMER"] != null ? digiwaydata.Attributes["ROUTENNUMMER"].ToString() : null,                
                Language = "it"
            });

            odhactivitypoi.Number = digiwaydata.Attributes["ROUTENNUMMER"] != null ? digiwaydata.Attributes["ROUTENNUMMER"].ToString() : null;
            odhactivitypoi.Difficulty = digiwaydata.Attributes["SCHWIERIGKEITSGRAD"] != null ? TransformDifficulty(digiwaydata.Attributes["SCHWIERIGKEITSGRAD"].ToString()) : null;
            odhactivitypoi.Ratings = new Ratings() { Difficulty = odhactivitypoi.Difficulty };

            odhactivitypoi.Source = source;
            odhactivitypoi.SyncSourceInterface = source + "." + identifier;

            //Add Tags
            odhactivitypoi.TagIds = new List<string>();
            odhactivitypoi.TagIds.Add("accessibletrailroute"); 
            

            var georesult = ParseGeoServerGeodataToGeoShapeJson(
                digiwaydata,
                odhactivitypoi.Shortname,
                identifier,
                "accessibletrailroute",
                source,                 
                null,
                srid
                );


            //Add each Geojson Featurecollection to Mapping
            odhactivitypoi.Mapping = new Dictionary<string, IDictionary<string, string>>();

            Dictionary<string, string> additionalvalues = new Dictionary<string, string>();
            foreach (var feature in digiwaydata.Attributes)
            {
                additionalvalues.Add(feature.Key, feature.Value?.ToString());
            }
            odhactivitypoi.Mapping.TryAddOrUpdate(source, additionalvalues);


            //Add Starting GPS Coordinate as GPS Point 
            odhactivitypoi.GpsInfo = new List<GpsInfo>() { georesult.Item2 };

            return (odhactivitypoi, georesult.Item1);
        }

        private static string? TransformDifficulty(string? difficulty)
        {
            if (difficulty == null) { return null; }
            else
            {
                switch (difficulty)
                {
                    case "Schwer": return "6";
                    case "Mittel": return "3";
                    case "Leicht": return "1";
                    default: return null;
                }
            }
        }
    }
}
