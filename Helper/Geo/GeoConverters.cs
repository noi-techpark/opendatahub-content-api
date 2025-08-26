// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using NetTopologySuite.Geometries;
using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace Helper.Geo
{
    public class EPSG31254ToEPSG4326Converter
    {
        private readonly ICoordinateTransformation _transformation;

        public EPSG31254ToEPSG4326Converter()
        {
            // Create coordinate system factory
            var csFactory = new CoordinateSystemFactory();
            var ctFactory = new CoordinateTransformationFactory();

            // Define EPSG:31254 - MGI / Austria GK West
            // Using Well-Known Text (WKT) definition
            string epsg31254Wkt = @"PROJCS[""MGI / Austria GK West"",
                GEOGCS[""MGI"",
                    DATUM[""Militar_Geographische_Institut"",
                        SPHEROID[""Bessel 1841"",6377397.155,299.1528128,
                            AUTHORITY[""EPSG"",""7004""]],
                        TOWGS84[577.326,90.129,463.919,5.137,1.474,5.297,2.4232],
                        AUTHORITY[""EPSG"",""6312""]],
                    PRIMEM[""Greenwich"",0,
                        AUTHORITY[""EPSG"",""8901""]],
                    UNIT[""degree"",0.0174532925199433,
                        AUTHORITY[""EPSG"",""9122""]],
                    AUTHORITY[""EPSG"",""4312""]],
                PROJECTION[""Transverse_Mercator""],
                PARAMETER[""latitude_of_origin"",0],
                PARAMETER[""central_meridian"",10.33333333333333],
                PARAMETER[""scale_factor"",1],
                PARAMETER[""false_easting"",0],
                PARAMETER[""false_northing"",-5000000],
                UNIT[""metre"",1,
                    AUTHORITY[""EPSG"",""9001""]],
                AUTHORITY[""EPSG"",""31254""]]";

            // Define EPSG:4326 - WGS 84
            string epsg4326Wkt = @"GEOGCS[""WGS 84"",
                DATUM[""WGS_1984"",
                    SPHEROID[""WGS 84"",6378137,298.257223563,
                        AUTHORITY[""EPSG"",""7030""]],
                    AUTHORITY[""EPSG"",""6326""]],
                PRIMEM[""Greenwich"",0,
                    AUTHORITY[""EPSG"",""8901""]],
                UNIT[""degree"",0.0174532925199433,
                    AUTHORITY[""EPSG"",""9122""]],
                AUTHORITY[""EPSG"",""4326""]]";

            // Create coordinate systems from WKT
            var sourceCS = csFactory.CreateFromWkt(epsg31254Wkt);
            var targetCS = csFactory.CreateFromWkt(epsg4326Wkt);

            // Create transformation
            _transformation = ctFactory.CreateFromCoordinateSystems(sourceCS, targetCS);
        }

        /// <summary>
        /// Converts coordinates from EPSG:31254 to EPSG:4326
        /// </summary>
        /// <param name="x">X coordinate (Easting) in EPSG:31254</param>
        /// <param name="y">Y coordinate (Northing) in EPSG:31254</param>
        /// <returns>Tuple containing (Longitude, Latitude) in decimal degrees</returns>
        public (double Longitude, double Latitude) ConvertToWGS84(double x, double y)
        {
            try
            {
                // Transform the coordinates
                double[] point = { x, y };
                double[] result = _transformation.MathTransform.Transform(point);

                return (Longitude: result[0], Latitude: result[1]);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error converting coordinates: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Converts multiple coordinate pairs from EPSG:31254 to EPSG:4326
        /// </summary>
        /// <param name="coordinates">Array of coordinate pairs (x, y)</param>
        /// <returns>Array of converted coordinates (longitude, latitude)</returns>
        public (double Longitude, double Latitude)[] ConvertMultipleToWGS84((double X, double Y)[] coordinates)
        {
            var results = new (double Longitude, double Latitude)[coordinates.Length];

            for (int i = 0; i < coordinates.Length; i++)
            {
                results[i] = ConvertToWGS84(coordinates[i].X, coordinates[i].Y);
            }

            return results;
        }
    }
}
