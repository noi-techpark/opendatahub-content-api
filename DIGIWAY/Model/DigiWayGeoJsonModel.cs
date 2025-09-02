// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGIWAY.Model
{
    /// <summary>
    /// Root GeoJSON object that can be either FeatureCollection, Feature, or Geometry
    /// </summary>
    public class DWGeoJson
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("crs", NullValueHandling = NullValueHandling.Ignore)]
        public DWGeoJsonCoordinateReferenceSystem Crs { get; set; }
    }

    /// <summary>
    /// GeoJSON FeatureCollection with dynamic properties
    /// </summary>
    public class DWGeoJsonFeatureCollection : DWGeoJson
    {
        [JsonProperty("type")]
        public new string Type => "FeatureCollection";

        [JsonProperty("features")]
        public List<DWGeoJsonFeature> Features { get; set; } = new List<DWGeoJsonFeature>();

        // Dynamic properties for custom FeatureCollection metadata
        [JsonExtensionData]
        public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();

        public DWGeoJsonFeatureCollection()
        {
            base.Type = "FeatureCollection";
        }
    }

    /// <summary>
    /// GeoJSON Feature with dynamic properties
    /// </summary>
    public class DWGeoJsonFeature : DWGeoJson
    {
        [JsonProperty("type")]
        public new string Type => "Feature";

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public object Id { get; set; }

        [JsonProperty("geometry")]
        public DWGeometry Geometry { get; set; }

        [JsonProperty("properties")]
        public dynamic Properties { get; set; } = new ExpandoObject();

        // Additional feature-level properties (bbox, etc.)
        [JsonExtensionData]
        public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();

        public DWGeoJsonFeature()
        {
            base.Type = "Feature";
        }

        public DWGeoJsonFeature(DWGeometry geometry, dynamic properties = null, object id = null)
        {
            base.Type = "Feature";
            Geometry = geometry;
            Properties = properties ?? new ExpandoObject();
            Id = id;
        }
    }

    // <summary>
    /// Base GeoJSON Geometry class
    /// </summary>
    public abstract class DWGeometry : DWGeoJson
    {
        [JsonProperty("coordinates")]
        public abstract object Coordinates { get; set; }
    }

    /// <summary>
    /// Point geometry
    /// </summary>
    public class DWGeoJsonPoint : DWGeometry
    {
        [JsonProperty("type")]
        public new string Type => "Point";

        [JsonProperty("coordinates")]
        public override object Coordinates { get; set; } // [longitude, latitude] or [x, y]

        public DWGeoJsonPoint()
        {
            base.Type = "Point";
        }

        public DWGeoJsonPoint(double longitude, double latitude)
        {
            base.Type = "Point";
            Coordinates = new[] { longitude, latitude };
        }

        public DWGeoJsonPoint(double x, double y, double z)
        {
            base.Type = "Point";
            Coordinates = new[] { x, y, z };
        }
    }

    /// <summary>
    /// LineString geometry
    /// </summary>
    public class DWGeoJsonLineString : DWGeometry
    {
        [JsonProperty("type")]
        public new string Type => "LineString";

        [JsonProperty("coordinates")]
        public override object Coordinates { get; set; } // Array of [longitude, latitude] arrays

        public DWGeoJsonLineString()
        {
            base.Type = "LineString";
        }

        public DWGeoJsonLineString(double[][] coordinates)
        {
            base.Type = "LineString";
            Coordinates = coordinates;
        }
    }

    /// <summary>
    /// Polygon geometry
    /// </summary>
    public class DWGeoJsonPolygon : DWGeometry
    {
        [JsonProperty("type")]
        public new string Type => "Polygon";

        [JsonProperty("coordinates")]
        public override object Coordinates { get; set; } // Array of LinearRing coordinate arrays

        public DWGeoJsonPolygon()
        {
            base.Type = "Polygon";
        }

        public DWGeoJsonPolygon(double[][][] coordinates)
        {
            base.Type = "Polygon";
            Coordinates = coordinates;
        }
    }

    /// <summary>
    /// MultiPoint geometry
    /// </summary>
    public class DWGeoJsonMultiPoint : DWGeometry
    {
        [JsonProperty("type")]
        public new string Type => "MultiPoint";

        [JsonProperty("coordinates")]
        public override object Coordinates { get; set; }

        public DWGeoJsonMultiPoint()
        {
            base.Type = "MultiPoint";
        }
    }

    /// <summary>
    /// MultiLineString geometry
    /// </summary>
    public class DWGeoJsonMultiLineString : DWGeometry
    {
        [JsonProperty("type")]
        public new string Type => "MultiLineString";

        [JsonProperty("coordinates")]
        public override object Coordinates { get; set; }

        public DWGeoJsonMultiLineString()
        {
            base.Type = "MultiLineString";
        }
    }

    /// <summary>
    /// MultiPolygon geometry
    /// </summary>
    public class DWGeoJsonMultiPolygon : DWGeometry
    {
        [JsonProperty("type")]
        public new string Type => "MultiPolygon";

        [JsonProperty("coordinates")]
        public override object Coordinates { get; set; }

        public DWGeoJsonMultiPolygon()
        {
            base.Type = "MultiPolygon";
        }
    }

    /// <summary>
    /// GeometryCollection
    /// </summary>
    public class DWGeoJsonGeometryCollection : DWGeometry
    {
        [JsonProperty("type")]
        public new string Type => "GeometryCollection";

        [JsonProperty("geometries")]
        public List<DWGeometry> Geometries { get; set; } = new List<DWGeometry>();

        [JsonProperty("coordinates")]
        public override object Coordinates { get; set; } = null; // Not used in GeometryCollection

        public DWGeoJsonGeometryCollection()
        {
            base.Type = "GeometryCollection";
        }
    }

    /// <summary>
    /// Coordinate Reference System
    /// </summary>
    public class DWGeoJsonCoordinateReferenceSystem
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("properties")]
        public dynamic Properties { get; set; } = new ExpandoObject();
    }

    // =============================================================================
    // HELPER CLASSES FOR DYNAMIC PROPERTY MANAGEMENT
    // =============================================================================

    /// <summary>
    /// Helper class for working with dynamic properties
    /// </summary>
    public static class DWGeoJsonHelpers
    {
        /// <summary>
        /// Creates a feature with dynamic properties from a dictionary
        /// </summary>
        public static DWGeoJsonFeature CreateFeature(DWGeometry geometry, Dictionary<string, object> properties, object id = null)
        {
            var feature = new DWGeoJsonFeature(geometry, null, id);

            // Convert dictionary to ExpandoObject for dynamic access
            var expando = new ExpandoObject();
            var expandoDict = (IDictionary<string, object>)expando;

            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    expandoDict[kvp.Key] = kvp.Value;
                }
            }

            feature.Properties = expando;
            return feature;
        }

        /// <summary>
        /// Creates a feature with dynamic properties from an anonymous object
        /// </summary>
        public static DWGeoJsonFeature CreateFeature(DWGeometry geometry, object properties, object id = null)
        {
            var feature = new DWGeoJsonFeature(geometry, null, id);

            if (properties != null)
            {
                var json = JsonConvert.SerializeObject(properties);
                feature.Properties = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());
            }

            return feature;
        }

        /// <summary>
        /// Gets a property value from dynamic properties (with type conversion)
        /// </summary>
        public static T GetProperty<T>(DWGeoJsonFeature feature, string propertyName, T defaultValue = default(T))
        {
            try
            {
                if (feature.Properties == null)
                    return defaultValue;

                var dict = (IDictionary<string, object>)feature.Properties;

                if (dict.ContainsKey(propertyName))
                {
                    var value = dict[propertyName];

                    if (value is T directValue)
                        return directValue;

                    // Try to convert
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting property {propertyName}: {ex.Message}");
            }

            return defaultValue;
        }

        /// <summary>
        /// Sets a property value in dynamic properties
        /// </summary>
        public static void SetProperty(DWGeoJsonFeature feature, string propertyName, object value)
        {
            if (feature.Properties == null)
                feature.Properties = new ExpandoObject();

            var dict = (IDictionary<string, object>)feature.Properties;
            dict[propertyName] = value;
        }

        /// <summary>
        /// Gets all property names from a feature
        /// </summary>
        public static List<string> GetPropertyNames(DWGeoJsonFeature feature)
        {
            var names = new List<string>();

            if (feature.Properties != null)
            {
                var dict = (IDictionary<string, object>)feature.Properties;
                names.AddRange(dict.Keys);
            }

            return names;
        }

        /// <summary>
        /// Converts properties to a regular dictionary
        /// </summary>
        public static Dictionary<string, object> GetPropertiesAsDictionary(DWGeoJsonFeature feature)
        {
            var result = new Dictionary<string, object>();

            if (feature.Properties != null)
            {
                var dict = (IDictionary<string, object>)feature.Properties;
                foreach (var kvp in dict)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }
    }

    // =============================================================================
    // CUSTOM JSON CONVERTER FOR GEOMETRY
    // =============================================================================

    /// <summary>
    /// Custom JSON converter to handle different geometry types during deserialization
    /// </summary>
    public class GeometryConverter : JsonConverter<DWGeometry>
    {
        public override DWGeometry ReadJson(JsonReader reader, Type objectType, DWGeometry existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = serializer.Deserialize<dynamic>(reader);
            string type = obj.type;

            switch (type?.ToLower())
            {
                case "point":
                    return JsonConvert.DeserializeObject<DWGeoJsonPoint>(obj.ToString());
                case "linestring":
                    return JsonConvert.DeserializeObject<DWGeoJsonLineString>(obj.ToString());
                case "polygon":
                    return JsonConvert.DeserializeObject<DWGeoJsonPolygon>(obj.ToString());
                case "multipoint":
                    return JsonConvert.DeserializeObject<DWGeoJsonMultiPoint>(obj.ToString());
                case "multilinestring":
                    return JsonConvert.DeserializeObject<DWGeoJsonMultiLineString>(obj.ToString());
                case "multipolygon":
                    return JsonConvert.DeserializeObject<DWGeoJsonMultiPolygon>(obj.ToString());
                case "geometrycollection":
                    return JsonConvert.DeserializeObject<DWGeoJsonGeometryCollection>(obj.ToString());
                default:
                    throw new JsonSerializationException($"Unknown geometry type: {type}");
            }
        }

        public override void WriteJson(JsonWriter writer, DWGeometry value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}


//var featureCollection = JsonConvert.DeserializeObject<FeatureCollection>(featureCollectionGeoJson, settings);
