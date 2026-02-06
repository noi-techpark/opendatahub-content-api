// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System;



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

    // <summary>
    /// Coordinate converter for transforming between EPSG:3857 (Web Mercator) and EPSG:4326 (WGS84)
    /// </summary>
    public static class EPSG3857ToEPSG4326Converter
    {
        // Earth's radius in meters (WGS84 semi-major axis)
        private const double EarthRadius = 6378137.0;

        // Maximum latitude in Web Mercator projection (approximately 85.0511 degrees)
        private const double MaxLatitude = 85.05112877980659;

        /// <summary>
        /// Coordinate structure for EPSG:4326 (WGS84) coordinates
        /// </summary>
        public struct WGS84Coordinate
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }

            public WGS84Coordinate(double latitude, double longitude)
            {
                Latitude = latitude;
                Longitude = longitude;
            }

            /// <summary>
            /// Returns coordinates in the format "46.234234 11.334223"
            /// </summary>
            public override string ToString()
            {
                return $"{Latitude:F6} {Longitude:F6}";
            }

            /// <summary>
            /// Returns coordinates with custom precision
            /// </summary>
            public string ToString(int decimals)
            {
                var format = $"F{decimals}";
                return $"{Latitude.ToString(format)} {Longitude.ToString(format)}";
            }
        }

        /// <summary>
        /// Coordinate structure for EPSG:3857 (Web Mercator) coordinates
        /// </summary>
        public struct WebMercatorCoordinate
        {
            public double X { get; set; }
            public double Y { get; set; }

            public WebMercatorCoordinate(double x, double y)
            {
                X = x;
                Y = y;
            }

            public override string ToString()
            {
                return $"X: {X:F2}, Y: {Y:F2}";
            }
        }

        /// <summary>
        /// Converts EPSG:3857 (Web Mercator) coordinates to EPSG:4326 (WGS84)
        /// </summary>
        /// <param name="x">X coordinate in Web Mercator (meters)</param>
        /// <param name="y">Y coordinate in Web Mercator (meters)</param>
        /// <returns>WGS84 coordinate with latitude and longitude in decimal degrees</returns>
        public static WGS84Coordinate ConvertWebMercatorToWGS84(double x, double y)
        {
            // Convert X to longitude
            double longitude = x / EarthRadius * (180.0 / Math.PI);

            // Convert Y to latitude
            double latitude = Math.Atan(Math.Sinh(y / EarthRadius)) * (180.0 / Math.PI);

            // Clamp latitude to valid range
            latitude = Math.Max(-MaxLatitude, Math.Min(MaxLatitude, latitude));

            return new WGS84Coordinate(latitude, longitude);
        }

        /// <summary>
        /// Converts EPSG:3857 (Web Mercator) coordinates to EPSG:4326 (WGS84)
        /// </summary>
        /// <param name="webMercatorCoord">Web Mercator coordinate</param>
        /// <returns>WGS84 coordinate</returns>
        public static WGS84Coordinate ConvertWebMercatorToWGS84(WebMercatorCoordinate webMercatorCoord)
        {
            return ConvertWebMercatorToWGS84(webMercatorCoord.X, webMercatorCoord.Y);
        }

        /// <summary>
        /// Converts EPSG:4326 (WGS84) coordinates to EPSG:3857 (Web Mercator)
        /// </summary>
        /// <param name="latitude">Latitude in decimal degrees</param>
        /// <param name="longitude">Longitude in decimal degrees</param>
        /// <returns>Web Mercator coordinate in meters</returns>
        public static WebMercatorCoordinate ConvertWGS84ToWebMercator(double latitude, double longitude)
        {
            // Clamp latitude to valid range for Web Mercator
            latitude = Math.Max(-MaxLatitude, Math.Min(MaxLatitude, latitude));

            // Convert longitude to X
            double x = longitude * (Math.PI / 180.0) * EarthRadius;

            // Convert latitude to Y
            double latRadians = latitude * (Math.PI / 180.0);
            double y = Math.Log(Math.Tan((Math.PI / 4.0) + (latRadians / 2.0))) * EarthRadius;

            return new WebMercatorCoordinate(x, y);
        }

        /// <summary>
        /// Converts EPSG:4326 (WGS84) coordinates to EPSG:3857 (Web Mercator)
        /// </summary>
        /// <param name="wgs84Coord">WGS84 coordinate</param>
        /// <returns>Web Mercator coordinate</returns>
        public static WebMercatorCoordinate ConvertWGS84ToWebMercator(WGS84Coordinate wgs84Coord)
        {
            return ConvertWGS84ToWebMercator(wgs84Coord.Latitude, wgs84Coord.Longitude);
        }

        /// <summary>
        /// Batch converts multiple EPSG:3857 coordinates to EPSG:4326
        /// </summary>
        /// <param name="webMercatorCoords">Array of Web Mercator coordinates</param>
        /// <returns>Array of WGS84 coordinates</returns>
        public static WGS84Coordinate[] ConvertWebMercatorToWGS84Batch(WebMercatorCoordinate[] webMercatorCoords)
        {
            if (webMercatorCoords == null)
                return null;

            var results = new WGS84Coordinate[webMercatorCoords.Length];
            for (int i = 0; i < webMercatorCoords.Length; i++)
            {
                results[i] = ConvertWebMercatorToWGS84(webMercatorCoords[i]);
            }
            return results;
        }

        /// <summary>
        /// Validates if coordinates are within valid Web Mercator bounds
        /// </summary>
        /// <param name="x">X coordinate in meters</param>
        /// <param name="y">Y coordinate in meters</param>
        /// <returns>True if coordinates are valid for Web Mercator projection</returns>
        public static bool IsValidWebMercatorCoordinate(double x, double y)
        {
            // Web Mercator bounds
            double maxX = Math.PI * EarthRadius;  // ~20037508.34 meters
            double maxY = Math.Log(Math.Tan(Math.PI * (0.25 + MaxLatitude / 360.0))) * EarthRadius;

            return Math.Abs(x) <= maxX && Math.Abs(y) <= maxY;
        }

        /// <summary>
        /// Validates if WGS84 coordinates are within valid bounds
        /// </summary>
        /// <param name="latitude">Latitude in decimal degrees</param>
        /// <param name="longitude">Longitude in decimal degrees</param>
        /// <returns>True if coordinates are valid WGS84 coordinates</returns>
        public static bool IsValidWGS84Coordinate(double latitude, double longitude)
        {
            return latitude >= -90.0 && latitude <= 90.0 &&
                   longitude >= -180.0 && longitude <= 180.0;
        }
    }

    public static class EPSG3857ToEPSG4326
    {
        public static Geometry ConvertEPSG3857ToEPSG4326(Geometry geometry)
        {            
            Geometry geom3857 = geometry;

            var ctFactory = new CoordinateTransformationFactory();

            var transform = ctFactory.CreateFromCoordinateSystems(
                ProjectedCoordinateSystem.WebMercator, // EPSG:3857
                GeographicCoordinateSystem.WGS84       // EPSG:4326
            );

            var geom4326 = (Geometry)geom3857.Copy();
            geom4326.Apply(new ProjNetFilter(transform.MathTransform));
            geom4326.SRID = 4326;

            return geom4326;
        }
    }

    public sealed class ProjNetFilter : ICoordinateSequenceFilter
    {
        private readonly MathTransform _transform;

        public ProjNetFilter(MathTransform transform)
        {
            _transform = transform;
        }

        public void Filter(CoordinateSequence seq, int i)
        {
            var result = _transform.Transform(new[]
            {
            seq.GetX(i),
            seq.GetY(i)
        });

            seq.SetX(i, result[0]);
            seq.SetY(i, result[1]);
        }

        public bool Done => false;
        public bool GeometryChanged => true;
    }
}
