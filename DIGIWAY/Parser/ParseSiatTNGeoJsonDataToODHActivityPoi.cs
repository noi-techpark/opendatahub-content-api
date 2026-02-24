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
    public class ParseSiatTNGeoJsonDataToODHActivityPoi
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
                "elementi_cicloviari_v" => ParseCyclingRoutesTyrolToODHActivityPoi(odhactivitypoi, digiwaydata, identifier, source, srid),
                "sentieri_della_sat" => ParseHikeRoutesTyrolToODHActivityPoi(odhactivitypoi, digiwaydata, identifier, source, srid),
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
            //odhactivitypoi.WayNumber = digiwaydata.Attributes["numero"] != null ? Convert.ToInt16(digiwaydata.Attributes["numero"]) : null;

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

        private static (ODHActivityPoiLinked, GeoShapeJson) ParseHikeRoutesTyrolToODHActivityPoi(
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

            //if denominazione null use the way number
            odhactivitypoi.Shortname = digiwaydata.Attributes["denominaz"] != null ? digiwaydata.Attributes["denominaz"].ToString() : digiwaydata.Attributes["numero"] != null ? digiwaydata.Attributes["numero"].ToString() : null;
            
            odhactivitypoi.Detail = new Dictionary<string, Detail>();

            string gettheretext = digiwaydata.Attributes["loc_inizio"] != null ? "inizio: " + digiwaydata.Attributes["loc_inizio"].ToString() + " " : "" +
                                        digiwaydata.Attributes["loc_fine"] != null ? "fine: " + digiwaydata.Attributes["loc_fine"].ToString() : "";

            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("it", new Detail()
            {
                Title = digiwaydata.Attributes["denominaz"] != null ? digiwaydata.Attributes["denominaz"].ToString() : digiwaydata.Attributes["numero"] != null ? digiwaydata.Attributes["numero"].ToString() : null,
                AdditionalText = digiwaydata.Attributes["competenza"] != null ? digiwaydata.Attributes["competenza"].ToString() : null + " numero: " +digiwaydata.Attributes["numero"] != null ? digiwaydata.Attributes["numero"].ToString() : null
                + " gruppo_montagna: " + digiwaydata.Attributes["gr_mont"] != null ? digiwaydata.Attributes["gr_mont"].ToString() : null,
                GetThereText = gettheretext,
                Language = "it"
            });

            odhactivitypoi.ContactInfos = new Dictionary<string, ContactInfos>();
            odhactivitypoi.ContactInfos.TryAddOrUpdate<string, ContactInfos>("it", new ContactInfos()
            {
                City = digiwaydata.Attributes["comuni_toc"] != null ? digiwaydata.Attributes["comuni_toc"].ToString() : null,
                CountryCode = "IT",
                CountryName = "Italia",
                CompanyName = "CAI: " + digiwaydata.Attributes["competenza"] != null ? digiwaydata.Attributes["competenza"].ToString() : null,
                Language = "it"
            });

            //TODO add also	"comuni_toc": "ALA",


            odhactivitypoi.Number = digiwaydata.Attributes["numero"] != null ? digiwaydata.Attributes["numero"].ToString() : null;
            

            odhactivitypoi.AltitudeHighestPoint = digiwaydata.Attributes["quota_max"] != null ? Convert.ToDouble(digiwaydata.Attributes["quota_max"]) : null;
            odhactivitypoi.AltitudeLowestPoint = digiwaydata.Attributes["quota_min"] != null ? Convert.ToDouble(digiwaydata.Attributes["quota_min"]) : null;

            odhactivitypoi.DistanceLength = digiwaydata.Attributes["lun_inclin"] != null ? Convert.ToDouble(digiwaydata.Attributes["lun_inclin"]) : null;            
            odhactivitypoi.DistanceDuration = digiwaydata.Attributes["t_andata"] != null ? TransformDuration(digiwaydata.Attributes["t_andata"].ToString()) : null;
            odhactivitypoi.Difficulty = digiwaydata.Attributes["difficolta"] != null ? TransformHikeDifficulty(digiwaydata.Attributes["difficolta"].ToString()) : null;


            if (odhactivitypoi.Difficulty != null)
                odhactivitypoi.Ratings = new Ratings() { Difficulty = odhactivitypoi.Difficulty };

            
            
            




            odhactivitypoi.Source = source;
            odhactivitypoi.SyncSourceInterface = source + "." + identifier;




            //Add Tags
            odhactivitypoi.TagIds = new List<string>();
            odhactivitypoi.TagIds.Add("978F89296ACB4DB4B6BD1C269341802F"); //LTS Hiking Tag
            odhactivitypoi.TagIds.Add("hiking");
            odhactivitypoi.TagIds.Add("B702CF3773CF4A47AFEBC291618A7B7E"); //LTS Other hikes Tag
            odhactivitypoi.TagIds.Add("other hikes");

            Dictionary<string, string> additionalvalues = new Dictionary<string, string>();
            if (digiwaydata.Attributes["classid"] != null)
                additionalvalues.Add("classid", Convert.ToString(digiwaydata.Attributes["classid"]));
            if (digiwaydata.Attributes["numero"] != null)
                additionalvalues.Add("numero", Convert.ToString(digiwaydata.Attributes["numero"]));
            if (digiwaydata.Attributes["competenza"] != null)
                additionalvalues.Add("competenza", Convert.ToString(digiwaydata.Attributes["competenza"]));
            if (digiwaydata.Attributes["comuni_toc"] != null)
                additionalvalues.Add("comuni_toc", Convert.ToString(digiwaydata.Attributes["comuni_toc"]));
            if (digiwaydata.Attributes["gr_mont"] != null)
                additionalvalues.Add("gr_mont", Convert.ToString(digiwaydata.Attributes["gr_mont"]));
            if (digiwaydata.Attributes["gr_mont"] != null)
                additionalvalues.Add("difficolta", Convert.ToString(digiwaydata.Attributes["difficolta"]));
            if (digiwaydata.Attributes["lun_planim"] != null)
                additionalvalues.Add("lun_planim", Convert.ToString(digiwaydata.Attributes["lun_planim"]));
            if (digiwaydata.Attributes["quota_fine"] != null)
                additionalvalues.Add("quota_fine", Convert.ToString(digiwaydata.Attributes["quota_fine"]));
            if (digiwaydata.Attributes["quota_iniz"] != null)
                additionalvalues.Add("quota_iniz", Convert.ToString(digiwaydata.Attributes["quota_iniz"]));
            if (digiwaydata.Attributes["t_andata"] != null)
                additionalvalues.Add("t_andata", Convert.ToString(digiwaydata.Attributes["t_andata"]));
            if (digiwaydata.Attributes["t_ritorno"] != null)
                additionalvalues.Add("t_ritorno", Convert.ToString(digiwaydata.Attributes["t_ritorno"]));
            if (digiwaydata.Attributes["objectid"] != null)
                additionalvalues.Add("objectid", Convert.ToString(digiwaydata.Attributes["objectid"]));

            var georesult = ParseGeoServerGeodataToGeoShapeJson(
                digiwaydata,
                odhactivitypoi.Shortname,
                identifier,
                "hikingtrail",
                source,
                null,
                srid
                );

            odhactivitypoi.Mapping.TryAddOrUpdate(source, additionalvalues);

            //Add Starting GPS Coordinate as GPS Point 
            odhactivitypoi.GpsInfo = new List<GpsInfo>() { georesult.Item2 };

            return (odhactivitypoi, georesult.Item1);
        }

        private static (ODHActivityPoiLinked, GeoShapeJson) ParseCyclingRoutesTyrolToODHActivityPoi(
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
            odhactivitypoi.Shortname = digiwaydata.Attributes["tratto"] != null ? digiwaydata.Attributes["tratto"].ToString() : null;
            odhactivitypoi.Detail = new Dictionary<string, Detail>();

            string additionaltext = digiwaydata.Attributes["pavimentaz"] != null ? "pavimentaz: " + digiwaydata.Attributes["pavimentaz"].ToString() + " " : "" +
                                    digiwaydata.Attributes["eurovelo"] != null ? "eurovelo: " + digiwaydata.Attributes["eurovelo"].ToString() + " " : "" +
                                    digiwaydata.Attributes["livello"] != null ? "livello: " + digiwaydata.Attributes["livello"].ToString() + " " : "" +
                                    digiwaydata.Attributes["scala"] != null ? "scala: " + digiwaydata.Attributes["scala"].ToString() + " " : "" +
                                    digiwaydata.Attributes["posizione"] != null ? "posizione: " + digiwaydata.Attributes["posizione"].ToString() + " " : "" +
                                    digiwaydata.Attributes["sede"] != null ? "sede: " + digiwaydata.Attributes["sede"].ToString() + " " : "" +
                                    digiwaydata.Attributes["gestione"] != null ? "gestione: " + digiwaydata.Attributes["gestione"].ToString() + " " : "" +                                    
                                    digiwaydata.Attributes["fonte"] != null ? "fonte: " + digiwaydata.Attributes["fonte"].ToString() + " " : "";

            odhactivitypoi.Detail.TryAddOrUpdate<string, Detail>("it", new Detail()
            {
                Title = digiwaydata.Attributes["tratto"].ToString() != null ? digiwaydata.Attributes["tratto"].ToString() : null,
                AdditionalText = digiwaydata.Attributes["pista"] != null ? digiwaydata.Attributes["pista"].ToString() : null + additionaltext,                 
                Language = "it"
            });

            odhactivitypoi.Source = source;
            odhactivitypoi.SyncSourceInterface = source + "." + identifier;

            //Add Tags
            odhactivitypoi.TagIds = new List<string>();
            odhactivitypoi.TagIds.Add("9DE2F99EA67E4278A558755E093DB0ED"); //LTS Others bike Tag
            odhactivitypoi.TagIds.Add("cycling");
            odhactivitypoi.TagIds.Add("others bike");
            odhactivitypoi.TagIds.Add("B015F1EA92494EB1B6E32170269000B0");  //LTS RAdtouren Tag     

            Dictionary<string, string> additionalvalues = new Dictionary<string, string>();
            if (digiwaydata.Attributes["classid"] != null)
                additionalvalues.Add("classid", Convert.ToString(digiwaydata.Attributes["classid"]));
            if (digiwaydata.Attributes["objectid"] != null)
                additionalvalues.Add("objectid", Convert.ToString(digiwaydata.Attributes["objectid"]));
            if (digiwaydata.Attributes["id"] != null)
                additionalvalues.Add("id", Convert.ToString(digiwaydata.Attributes["id"]));
            if (digiwaydata.Attributes["livello"] != null)
                additionalvalues.Add("livello", Convert.ToString(digiwaydata.Attributes["livello"]));
            if (digiwaydata.Attributes["scala"] != null)
                additionalvalues.Add("scala", Convert.ToString(digiwaydata.Attributes["scala"]));
            if (digiwaydata.Attributes["posizione"] != null)
                additionalvalues.Add("posizione", Convert.ToString(digiwaydata.Attributes["posizione"]));
            if (digiwaydata.Attributes["fonte"] != null)
                additionalvalues.Add("fonte", Convert.ToString(digiwaydata.Attributes["fonte"]));
            if (digiwaydata.Attributes["sede"] != null)
                additionalvalues.Add("sede", Convert.ToString(digiwaydata.Attributes["sede"]));
            if (digiwaydata.Attributes["gestione"] != null)
                additionalvalues.Add("gestione", Convert.ToString(digiwaydata.Attributes["gestione"]));
            if (digiwaydata.Attributes["pavimentaz"] != null)
                additionalvalues.Add("pavimentaz", Convert.ToString(digiwaydata.Attributes["pavimentaz"]));
            if (digiwaydata.Attributes["eurovelo"] != null)
                additionalvalues.Add("eurovelo", Convert.ToString(digiwaydata.Attributes["eurovelo"]));
            if (digiwaydata.Attributes["datafine"] != null)
                additionalvalues.Add("datafine", Convert.ToString(digiwaydata.Attributes["datafine"]));
            if (digiwaydata.Attributes["dataini"] != null)
                additionalvalues.Add("dataini", Convert.ToString(digiwaydata.Attributes["dataini"]));

            var georesult = ParseGeoServerGeodataToGeoShapeJson(
                digiwaydata,
                odhactivitypoi.Shortname,
                identifier,
                "cycleway",
                source,
                null,
                srid
                );

            odhactivitypoi.Mapping.TryAddOrUpdate(source, additionalvalues);

            //Add Starting GPS Coordinate as GPS Point 
            odhactivitypoi.GpsInfo = new List<GpsInfo>() { georesult.Item2 };

            return (odhactivitypoi, georesult.Item1);
        }

        public static double? TransformDuration(string? durationstr)
        {
            if (durationstr == null) { return null; }
            else
            {
                var durationts = TimeSpan.Parse(durationstr);

                return durationts.TotalHours;
            }
        }

        public static string? TransformHikeDifficulty(string? difficulty)
        {
            if (difficulty == null) { return null; }
            else
            {
                switch (difficulty)
                {
                    //To check
                    case "T": return "1";
                    case "E": return "2";
                    case "EE": return "3";
                    case "EEA-F": return "4";
                    case "EEA-PD": return "4";                    
                    case "EEA-D": return "5";
                    case "EEA-MD": return "6";
                    case "EEA-ED": return "6";
                    default: return null;
                }
            }
        }

    }
}
