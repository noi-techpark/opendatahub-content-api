// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using DIGIWAY.Model.GeoJsonReadModel;
using Helper;
using Helper.Geo;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DIGIWAY
{
    public class ParseEuregioGeoJsonDataToSpatialData
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

        /// <summary>
        /// Attributes which, together with the geometry, identify a feature. The source delivers no feature id.
        /// All other attributes (surface, difficulty, closures, DATA_STATUS ...) may change and then update the same record.
        /// routes: one record per route on a path segment, so the route attributes are part of the key.
        /// roadnetwork: one record per segment, the geometry alone is unique.
        /// </summary>
        private static readonly IDictionary<string, string[]> idattributes = new Dictionary<string, string[]>()
        {
            { "euregio.routes", new[] { "PATH_CODE", "PATH_D", "RESPORGPATH_D" } },
            { "euregio.roadnetwork", Array.Empty<string>() },
        };

        /// <summary>
        /// Creates a deterministic id (SHA1 of the source coordinates and the identifying attributes, shortened to 32 hex chars),
        /// so a reimport of the same feature updates the existing record instead of creating a new one.
        /// </summary>
        public static string CreateStableId(GeoJsonFeature digiwaydata, string identifier, string source)
        {
            var key = new StringBuilder();

            foreach (var coordinate in digiwaydata.Geometry?.Coordinates ?? Array.Empty<NetTopologySuite.Geometries.Coordinate>())
            {
                key.Append(coordinate.X.ToString("R", CultureInfo.InvariantCulture))
                    .Append(' ')
                    .Append(coordinate.Y.ToString("R", CultureInfo.InvariantCulture))
                    .Append(',');
            }

            if (idattributes.TryGetValue(identifier, out var attributes))
            {
                foreach (var attribute in attributes)
                {
                    digiwaydata.Attributes.TryGetValue(attribute, out var value);
                    key.Append('|').Append(attribute).Append('=').Append(value?.ToString());
                }
            }

            //First 32 hex chars (128 bit) of the SHA1, collisions are practically excluded (and handled by the importer anyway)
            var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(key.ToString())))[..32];

            return ("urn:" + source + ":" + identifier + ":" + hash).ToLower();
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

            spatialdata.Id = CreateStableId(digiwaydata, identifier, source);
            spatialdata.Active = true;
            spatialdata.FirstImport = spatialdata.FirstImport == null ? DateTime.Now : spatialdata.FirstImport;
            spatialdata.LastChange =
                spatialdata.LastChange =
                digiwaydata.Attributes["DATA_STATUS"] != null &&
                DateTime.TryParse(digiwaydata.Attributes["DATA_STATUS"].ToString(), out var dt)
                    ? dt
                    : DateTime.Now;
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

            spatialdata.TagIds = EuregioTagHelper.GetTags(digiwaydata, identifier);

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

            spatialdata.Id = CreateStableId(digiwaydata, identifier, source);
            spatialdata.Active = true;
            spatialdata.FirstImport = spatialdata.FirstImport == null ? DateTime.Now : spatialdata.FirstImport;

            spatialdata.LastChange =
                digiwaydata.Attributes["DATA_STATUS"] != null &&
                DateTime.TryParse(digiwaydata.Attributes["DATA_STATUS"].ToString(), out var dt)
                    ? dt
                    : DateTime.Now;

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

            spatialdata.TagIds = EuregioTagHelper.GetTags(digiwaydata, identifier);


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
