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

            spatialdata.HasLanguage = new List<string>() { "de", "it", "en" };
            spatialdata.Detail = new Dictionary<string, DetailGeneric>();

            foreach (var (language, suffix) in new[] { ("de", "D"), ("it", "I"), ("en", "E") })
            {
                spatialdata.Detail.TryAddOrUpdate<string, DetailGeneric>(language, new DetailGeneric()
                {
                    Title = GetRouteTitle(digiwaydata, suffix),
                    Language = language
                });
            }

            spatialdata.Shortname = spatialdata.Detail["de"].Title;

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

        /// <summary>
        /// Route title as delivered by the source, suffix D/I/E is the language of the attribute:
        /// "PATH_x (PATH_CODE)", without name "PATH_CODE - MUNICIPALITY_NAME", without code "TYPE_x - MUNICIPALITY_NAME"
        /// </summary>
        private static string? GetRouteTitle(GeoJsonFeature digiwaydata, string suffix)
        {
            var name = GetTextAttribute(digiwaydata, "PATH_" + suffix);
            var code = GetTextAttribute(digiwaydata, "PATH_CODE");

            if (!string.IsNullOrEmpty(name))
                return string.IsNullOrEmpty(code) ? name : name + " (" + code + ")";

            var title = !string.IsNullOrEmpty(code) ? code : GetTextAttribute(digiwaydata, "TYPE_" + suffix);

            return AppendMunicipality(digiwaydata, title);
        }

        /// <summary>
        /// Road segment title as delivered by the source, suffix D/I/E is the language of the attribute:
        /// "STREETNAME - MUNICIPALITY_NAME", without street name "CATEGORY_x - MUNICIPALITY_NAME" (or DRAWCLASS_x if there is no category)
        /// </summary>
        private static string? GetRoadNetworkTitle(GeoJsonFeature digiwaydata, string suffix)
        {
            var title = GetTextAttribute(digiwaydata, "STREETNAME");

            if (string.IsNullOrEmpty(title))
                title = GetTextAttribute(digiwaydata, "CATEGORY_" + suffix);
            if (string.IsNullOrEmpty(title))
                title = GetTextAttribute(digiwaydata, "DRAWCLASS_" + suffix);

            return AppendMunicipality(digiwaydata, title);
        }

        private static string? AppendMunicipality(GeoJsonFeature digiwaydata, string? title)
        {
            var municipality = GetTextAttribute(digiwaydata, "MUNICIPALITY_NAME");

            if (string.IsNullOrEmpty(municipality))
                return title;

            return string.IsNullOrEmpty(title) ? municipality : title + " - " + municipality;
        }

        /// <summary>
        /// Attribute value for titles/texts, placeholders without any letter or digit (e.g. "." or "-") are treated as empty
        /// </summary>
        private static string? GetTextAttribute(GeoJsonFeature digiwaydata, string key)
        {
            var value = EuregioTagHelper.GetAttribute(digiwaydata, key);

            return value != null && value.Any(char.IsLetterOrDigit) ? value : null;
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

            spatialdata.HasLanguage = new List<string>() { "de", "it", "en" };
            spatialdata.Detail = new Dictionary<string, DetailGeneric>();

            foreach (var (language, suffix) in new[] { ("de", "D"), ("it", "I"), ("en", "E") })
            {
                spatialdata.Detail.TryAddOrUpdate<string, DetailGeneric>(language, new DetailGeneric()
                {
                    Title = GetRoadNetworkTitle(digiwaydata, suffix),
                    //Trail closure description is delivered only in german
                    BaseText = language == "de" ? GetTextAttribute(digiwaydata, "TRAILCLOSURE_DESCRIPTION") : null,
                    Language = language
                });
            }

            spatialdata.Shortname = spatialdata.Detail["de"].Title;

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
