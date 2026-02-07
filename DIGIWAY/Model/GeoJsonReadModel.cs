// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGIWAY.Model.GeoJsonReadModel
{
    /// <summary>
    /// Simple class for reading GeoJSON files using NetTopologySuite
    /// </summary>
    public class GeoJsonFileReader
    {
        private readonly GeoJsonReader _geoJsonReader;

        public GeoJsonFileReader()
        {
            _geoJsonReader = new GeoJsonReader();
        }

        /// <summary>
        /// Reads a GeoJSON file and returns a FeatureCollection
        /// </summary>
        /// <param name="filePath">Path to the GeoJSON file</param>
        /// <returns>NetTopologySuite FeatureCollection</returns>
        public FeatureCollection ReadGeoJsonFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"GeoJSON file not found: {filePath}");

            try
            {
                string geoJsonContent = File.ReadAllText(filePath, Encoding.UTF8);
                return _geoJsonReader.Read<FeatureCollection>(geoJsonContent);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error reading GeoJSON file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reads a GeoJSON file asynchronously
        /// </summary>
        /// <param name="filePath">Path to the GeoJSON file</param>
        /// <returns>NetTopologySuite FeatureCollection</returns>
        public async Task<FeatureCollection> ReadGeoJsonFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"GeoJSON file not found: {filePath}");

            try
            {
                string geoJsonContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                return _geoJsonReader.Read<FeatureCollection>(geoJsonContent);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error reading GeoJSON file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reads a GeoJSON file As Stream asynchronously
        /// </summary>
        /// <param name="filePath">Path to the GeoJSON file</param>
        /// <returns>NetTopologySuite FeatureCollection</returns>
        public async Task<FeatureCollection> ReadGeoJsonFileAsStreamAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"GeoJSON file not found: {filePath}");

            try
            {
                var features = new List<IFeature>();
                var geoJsonReader = new GeoJsonReader();

                using var fs = File.OpenRead(filePath);
                using var sr = new StreamReader(fs);
                using var reader = new JsonTextReader(sr);

                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName &&
                        (string)reader.Value == "features")
                    {
                        reader.Read(); // StartArray

                        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                        {
                            var featureToken = JToken.ReadFrom(reader);
                            var featureJson = featureToken.ToString();

                            var feature = geoJsonReader.Read<Feature>(featureJson);

                            // optional: filtern / vereinfachen / reprojizieren
                            features.Add(feature);
                        }
                    }
                }

                var featureCollection = new FeatureCollection();
                foreach (var f in features)
                    featureCollection.Add(f);

                return featureCollection;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error reading GeoJSON file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reads GeoJSON from a string
        /// </summary>
        /// <param name="geoJsonString">GeoJSON string content</param>
        /// <returns>NetTopologySuite FeatureCollection</returns>
        public FeatureCollection ReadGeoJsonString(string geoJsonString)
        {
            try
            {
                return _geoJsonReader.Read<FeatureCollection>(geoJsonString);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing GeoJSON string: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets basic information about the GeoJSON file
        /// </summary>
        /// <param name="filePath">Path to the GeoJSON file</param>
        /// <returns>File information</returns>
        public GeoJsonInfo GetFileInfo(string filePath)
        {
            var info = new GeoJsonInfo
            {
                FilePath = filePath,
                FileExists = File.Exists(filePath)
            };

            if (!info.FileExists)
                return info;

            try
            {
                var featureCollection = ReadGeoJsonFile(filePath);

                info.FeatureCount = featureCollection.Count;
                info.GeometryTypes = GetUniqueGeometryTypes(featureCollection);
                info.AttributeNames = GetUniqueAttributeNames(featureCollection);
                info.BoundingBox = CalculateBoundingBox(featureCollection);

                var fileInfo = new FileInfo(filePath);
                info.FileSizeBytes = fileInfo.Length;
            }
            catch (Exception ex)
            {
                info.Error = ex.Message;
            }

            return info;
        }

        /// <summary>
        /// Gets all features as a simple list for easy iteration
        /// </summary>
        /// <param name="featureCollection">FeatureCollection to process</param>
        /// <returns>List of features with geometry and attributes</returns>
        public List<GeoJsonFeature> GetFeatures(FeatureCollection featureCollection)
        {
            var features = new List<GeoJsonFeature>();

            foreach (var feature in featureCollection)
            {
                var geoJsonFeature = new GeoJsonFeature
                {
                    Geometry = feature.Geometry,
                    GeometryType = feature.Geometry?.GeometryType ?? "Unknown",
                    Attributes = new Dictionary<string, object>()
                };

                if (feature.Attributes != null)
                {
                    foreach (string attributeName in feature.Attributes.GetNames())
                    {
                        geoJsonFeature.Attributes[attributeName] = feature.Attributes[attributeName];
                    }
                }

                features.Add(geoJsonFeature);
            }

            return features;
        }

        /// <summary>
        /// Filters features by attribute value
        /// </summary>
        /// <param name="featureCollection">Source FeatureCollection</param>
        /// <param name="attributeName">Attribute name to filter by</param>
        /// <param name="attributeValue">Value to match</param>
        /// <returns>Filtered FeatureCollection</returns>
        public FeatureCollection FilterByAttribute(FeatureCollection featureCollection, string attributeName, object attributeValue)
        {
            var filteredCollection = new FeatureCollection();

            foreach (var feature in featureCollection)
            {
                if (feature.Attributes != null &&
                    feature.Attributes.Exists(attributeName) &&
                    feature.Attributes[attributeName]?.Equals(attributeValue) == true)
                {
                    filteredCollection.Add(feature);
                }
            }

            return filteredCollection;
        }

        #region Private Helper Methods

        private List<string> GetUniqueGeometryTypes(FeatureCollection featureCollection)
        {
            return featureCollection
                .Where(f => f.Geometry != null)
                .Select(f => f.Geometry.GeometryType)
                .Distinct()
                .ToList();
        }

        private List<string> GetUniqueAttributeNames(FeatureCollection featureCollection)
        {
            var attributeNames = new HashSet<string>();

            foreach (var feature in featureCollection)
            {
                if (feature.Attributes != null)
                {
                    foreach (string name in feature.Attributes.GetNames())
                    {
                        attributeNames.Add(name);
                    }
                }
            }

            return attributeNames.ToList();
        }

        private Envelope CalculateBoundingBox(FeatureCollection featureCollection)
        {
            var envelope = new Envelope();

            foreach (var feature in featureCollection)
            {
                if (feature.Geometry != null)
                {
                    envelope.ExpandToInclude(feature.Geometry.EnvelopeInternal);
                }
            }

            return envelope.IsNull ? null : envelope;
        }

        #endregion
    }

    /// <summary>
    /// Information about a GeoJSON file
    /// </summary>
    public class GeoJsonInfo
    {
        public string FilePath { get; set; }
        public bool FileExists { get; set; }
        public int FeatureCount { get; set; }
        public List<string> GeometryTypes { get; set; } = new List<string>();
        public List<string> AttributeNames { get; set; } = new List<string>();
        public Envelope BoundingBox { get; set; }
        public long FileSizeBytes { get; set; }
        public string Error { get; set; }

        public double FileSizeKB => FileSizeBytes / 1024.0;
        public double FileSizeMB => FileSizeBytes / (1024.0 * 1024.0);
    }

    /// <summary>
    /// Simplified representation of a GeoJSON feature
    /// </summary>
    public class GeoJsonFeature
    {
        public Geometry Geometry { get; set; }
        public string GeometryType { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
    }
}