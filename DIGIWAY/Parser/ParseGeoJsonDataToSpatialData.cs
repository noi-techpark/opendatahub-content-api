// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using CoordinateSharp;
using DataModel;
using DIGIWAY.Model.GeoJsonReadModel;
using Helper;
using Helper.Extensions;
using Helper.Geo;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Densify;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGIWAY
{
    public class ParseGeoJsonDataToSpatialData
    {
        public static SpatialData ParseToSpatialData(
            SpatialData? spatialdata,
            GeoJsonFeature digiwaydata,
            string identifier,
            string source,
            string srid
        )
        {
            var result = identifier switch
            {
                "euregio.routes" => ParseRoutesToSpatialData(spatialdata, digiwaydata, identifier, source, srid),
                "euregio.roadnetwork" => ParseRoadNetworksToSpatialData(spatialdata, digiwaydata, identifier, source, srid),
                "_" => null                
            };

            return result;
        }


        private static IDictionary<string, GpsInfo> ParseGeoServerGeodataToWKTAndPosition(GeoJsonFeature digiwaydata, string srid)
        {
            Dictionary<string, GpsInfo> gpsinfolist = new Dictionary<string, GpsInfo>();

            //get first point of geometry
            var point = digiwaydata.Geometry.Coordinates.FirstOrDefault();

            if (srid == "3857")
            {
                var wsg84coordinate = EPSG3857ToEPSG4326Converter.ConvertWebMercatorToWGS84(point.X, point.Y);

                var geom4326 = EPSG3857ToEPSG4326.ConvertEPSG3857ToEPSG4326(digiwaydata.Geometry);

                gpsinfolist.TryAddOrUpdate("track", new GpsInfo()
                {
                    Default = true,
                    Geometry = geom4326.AsText()
                });

                gpsinfolist.TryAddOrUpdate("position", new GpsInfo()
                {
                    Default = false,
                    Altitude = null,
                    AltitudeUnitofMeasure = "m",
                    Gpstype = "position",
                    //Use only first digits otherwise point and track will differ
                    Latitude = wsg84coordinate.Latitude,
                    Longitude = wsg84coordinate.Longitude,
                });
            }
            return gpsinfolist;
        }


        private static SpatialData ParseRoutesToSpatialData(
            SpatialData? spatialdata,
            GeoJsonFeature digiwaydata,
            string identifier,
            string source,
            string srid
        )
        {
            if (spatialdata == null)
                spatialdata = new SpatialData();

            spatialdata.Id = ("urn:" + identifier + ":" + digiwaydata.Attributes["TYPE_CODE"].ToString() + ":" + digiwaydata.Attributes["PATH_CODE"].ToString()).ToLower();
            spatialdata.Active = true;
            spatialdata.FirstImport = spatialdata.FirstImport == null ? DateTime.Now : spatialdata.FirstImport;
            spatialdata.LastChange = digiwaydata.Attributes["DATA_STATUS"] != null ? Convert.ToDateTime(digiwaydata.Attributes["DATA_STATUS"].ToString()) : DateTime.Now;
            //odhactivitypoi.HasLanguage = new List<string>() { "it" };
            //odhactivitypoi.Shortname = digiwaydata.Attributes["TYPE_E"] != null ? digiwaydata.Attributes["denominazi"].ToString() : null;
            //odhactivitypoi.Detail = new Dictionary<string, Detail>();

            //spatialdata.Detail.TryAddOrUpdate<string, Detail>("it", new Detail()
            //{
            //    Title = digiwaydata.Attributes["denominazi"].ToString() != null ? digiwaydata.Attributes["denominazi"].ToString() : null,
            //    AdditionalText = digiwaydata.Attributes["numero"] != null ? digiwaydata.Attributes["numero"].ToString() : null,
            //    GetThereText = gettheretext,
            //    Language = "it"
            //});

            spatialdata.Source = source;

            spatialdata.TagIds = new List<string>();
            spatialdata.TagIds.Add(identifier);


            ////Add Tags
            //odhactivitypoi.TagIds = new List<string>();
            //odhactivitypoi.TagIds.Add("1B9AF4DA6E3A414798890E6723E71EC8"); //LTS MTB Tag
            //odhactivitypoi.TagIds.Add("cycling");
            //odhactivitypoi.TagIds.Add("mountain bike");
            //odhactivitypoi.TagIds.Add("mountain bikes");

            //TODO Add each Geojson Featurecollection to Mapping
            spatialdata.Mapping = new Dictionary<string, IDictionary<string, string>>();

            Dictionary<string, string> additionalvalues = new Dictionary<string, string>();
            foreach (var feature in digiwaydata.Attributes)
            {                
                additionalvalues.Add(feature.Key, feature.Value == null ? feature.Value.ToString() : null);
            }
            spatialdata.Mapping.TryAddOrUpdate(source, additionalvalues);

            spatialdata.Geo = ParseGeoServerGeodataToWKTAndPosition(digiwaydata, "3857");


            return spatialdata;
        }

        private static SpatialData ParseRoadNetworksToSpatialData(
            SpatialData? spatialdata,
            GeoJsonFeature digiwaydata,
            string identifier,
            string source,
            string srid
        )
        {
            if (spatialdata == null)
                spatialdata = new SpatialData();

            spatialdata.Id = ("urn:" + identifier + ":" + System.Guid.NewGuid()).ToLower();
            spatialdata.Active = true;
            spatialdata.FirstImport = spatialdata.FirstImport == null ? DateTime.Now : spatialdata.FirstImport;
            spatialdata.LastChange = digiwaydata.Attributes["DATA_STATUS"] != null ? Convert.ToDateTime(digiwaydata.Attributes["DATA_STATUS"].ToString()) : DateTime.Now;
            //odhactivitypoi.HasLanguage = new List<string>() { "it" };
            //odhactivitypoi.Shortname = digiwaydata.Attributes["TYPE_E"] != null ? digiwaydata.Attributes["denominazi"].ToString() : null;
            //odhactivitypoi.Detail = new Dictionary<string, Detail>();

            //spatialdata.Detail.TryAddOrUpdate<string, Detail>("it", new Detail()
            //{
            //    Title = digiwaydata.Attributes["denominazi"].ToString() != null ? digiwaydata.Attributes["denominazi"].ToString() : null,
            //    AdditionalText = digiwaydata.Attributes["numero"] != null ? digiwaydata.Attributes["numero"].ToString() : null,
            //    GetThereText = gettheretext,
            //    Language = "it"
            //});

            spatialdata.Source = source;

            spatialdata.TagIds = new List<string>();
            spatialdata.TagIds.Add(identifier);


            ////Add Tags
            //odhactivitypoi.TagIds = new List<string>();
            //odhactivitypoi.TagIds.Add("1B9AF4DA6E3A414798890E6723E71EC8"); //LTS MTB Tag
            //odhactivitypoi.TagIds.Add("cycling");
            //odhactivitypoi.TagIds.Add("mountain bike");
            //odhactivitypoi.TagIds.Add("mountain bikes");

            //TODO Add each Geojson Featurecollection to Mapping
            spatialdata.Mapping = new Dictionary<string, IDictionary<string, string>>();

            Dictionary<string, string> additionalvalues = new Dictionary<string, string>();
            foreach (var feature in digiwaydata.Attributes)
            {
                additionalvalues.Add(feature.Key, feature.Value?.ToString());
            }
            spatialdata.Mapping.TryAddOrUpdate(source, additionalvalues);

            spatialdata.Geo = ParseGeoServerGeodataToWKTAndPosition(digiwaydata, "3857");


            return spatialdata;
        }

    }
}
