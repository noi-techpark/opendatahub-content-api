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
    public class ParseGeoJsonDataToODHActivityPoi
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
                "mtb_percorsi_v" => ParseMTBRoutesTyrolToODHActivityPoi(odhactivitypoi, digiwaydata, identifier, source, srid),
                //"elementi_cicloviari_v" => ParseMTBRoutesToODHActivityPoi(odhactivitypoi, digiwaydata, identifier, source, srid),
                //"sentieri_della_sat" => ParseHikingTrailsToODHActivityPoi(odhactivitypoi, digiwaydata, identifier, source, srid),                
                "_" => (null, null)
            };

            return result;
        }

        private static (GeoShapeJson, GpsInfo) ParseGeoServerGeodataToGeoShapeJson(GeoJsonFeature digiwaydata, string name, string identifier, string geoshapetype, string source, int? altitude, string srid)
        {
            GeoShapeJson geoshape = new GeoShapeJson();
            geoshape.Id = (identifier + "_" + digiwaydata.Attributes["classid"].ToString()).ToLower();
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


        private static (ODHActivityPoiLinked, GeoShapeJson) ParseMTBRoutesTyrolToODHActivityPoi(
            ODHActivityPoiLinked? odhactivitypoi,
            GeoJsonFeature digiwaydata,
            string identifier,
            string source,
            string srid
        )
        {
            if (odhactivitypoi == null)
                odhactivitypoi = new ODHActivityPoiLinked();

            odhactivitypoi.Id = (identifier + "_" + digiwaydata.Attributes["classid"].ToString()).ToLower();
            odhactivitypoi.Active = true;
            odhactivitypoi.FirstImport = digiwaydata.Attributes["dataagg"] != null ? Convert.ToDateTime(digiwaydata.Attributes["dataagg"].ToString()) : odhactivitypoi == null ? DateTime.Now : odhactivitypoi.FirstImport;
            odhactivitypoi.LastChange = digiwaydata.Attributes["dataagg"] != null ? Convert.ToDateTime(digiwaydata.Attributes["dataagg"].ToString()) : DateTime.Now;
            odhactivitypoi.HasLanguage = new List<string>() { "it" };
            odhactivitypoi.Shortname = digiwaydata.Attributes["denominazi"] != null ? digiwaydata.Attributes["denominazi"].ToString() : null;
            odhactivitypoi.Detail = new Dictionary<string, Detail>();

            string gettheretext = digiwaydata.Attributes["loc_ini"] != null ? "inizio: " + digiwaydata.Attributes["loc_ini"].ToString() + " " : "" +
                                      digiwaydata.Attributes["loc_fine"] != null ? "fine: " + digiwaydata.Attributes["loc_fine"].ToString() : "";

            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("it", new Detail()
            {
                Title = digiwaydata.Attributes["denominazi"].ToString() != null ? digiwaydata.Attributes["denominazi"].ToString() : null,
                AdditionalText = digiwaydata.Attributes["numero"] != null ? digiwaydata.Attributes["numero"].ToString() : null,
                GetThereText = gettheretext,
                Language = "it"
            });

            odhactivitypoi.Number = digiwaydata.Attributes["numero"] != null ? digiwaydata.Attributes["numero"].ToString() : null;
            odhactivitypoi.WayNumber = digiwaydata.Attributes["numero"] != null ? Convert.ToInt16(digiwaydata.Attributes["numero"]) : null;

            odhactivitypoi.DistanceLength = digiwaydata.Attributes["lunghezza"] != null ? Convert.ToDouble(digiwaydata.Attributes["lunghezza"]) : null;

            odhactivitypoi.Source = source;
            odhactivitypoi.SyncSourceInterface = source + "." + identifier;

            //Add Tags
            odhactivitypoi.TagIds = new List<string>();
            odhactivitypoi.TagIds.Add("1B9AF4DA6E3A414798890E6723E71EC8"); //LTS MTB Tag
            odhactivitypoi.TagIds.Add("cycling");
            odhactivitypoi.TagIds.Add("mountain bike");
            odhactivitypoi.TagIds.Add("mountain bikes");

            Dictionary<string, string> additionalvalues = new Dictionary<string, string>();
            if (digiwaydata.Attributes["classid"] != null)
                additionalvalues.Add("classid", Convert.ToString(digiwaydata.Attributes["classid"]));
            if (digiwaydata.Attributes["numero"] != null)
                additionalvalues.Add("numero", Convert.ToString(digiwaydata.Attributes["numero"]));
            if (digiwaydata.Attributes["tipo"] != null)
                additionalvalues.Add("tipo", Convert.ToString(digiwaydata.Attributes["tipo"]));
            if (digiwaydata.Attributes["id_ambito"] != null)
                additionalvalues.Add("id_ambito", Convert.ToString(digiwaydata.Attributes["id_ambito"]));
            if (digiwaydata.Attributes["objectid"] != null)
                additionalvalues.Add("objectid", Convert.ToString(digiwaydata.Attributes["objectid"]));

            var georesult = ParseGeoServerGeodataToGeoShapeJson(
                digiwaydata,
                odhactivitypoi.Shortname,
                identifier,
                "mountainbikeroute",
                source,                 
                null,
                srid
                );

            odhactivitypoi.Mapping.TryAddOrUpdate(source, additionalvalues);

            //Add Starting GPS Coordinate as GPS Point 
            odhactivitypoi.GpsInfo = new List<GpsInfo>() { georesult.Item2 };

            return (odhactivitypoi, georesult.Item1);
        }
    }
}
