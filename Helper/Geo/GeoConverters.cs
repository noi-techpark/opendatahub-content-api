// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using NetTopologySuite.Geometries;
using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace Helper.Geo
{
    public static class CoordinateConverterEPSG31254ToWGS84
    {
        // Method 1: Using ProjNet (Recommended)
        private static readonly Lazy<ICoordinateTransformation> _transformation = new Lazy<ICoordinateTransformation>(CreateTransformation);

        private static ICoordinateTransformation CreateTransformation()
        {
            var csFactory = new CoordinateSystemFactory();
            var ctFactory = new CoordinateTransformationFactory();

            // EPSG:31254 - MGI / Austria GK West
            var source = csFactory.CreateFromWkt(@"
                PROJCS[""MGI / Austria GK West"",
                    GEOGCS[""MGI"",
                        DATUM[""Militar_Geographische_Institut"",
                            SPHEROID[""Bessel 1841"",6377397.155,299.1528128]],
                        PRIMEM[""Greenwich"",0],
                        UNIT[""degree"",0.0174532925199433]],
                    PROJECTION[""Transverse_Mercator""],
                    PARAMETER[""latitude_of_origin"",0],
                    PARAMETER[""central_meridian"",10.33333333333333],
                    PARAMETER[""scale_factor"",1],
                    PARAMETER[""false_easting"",0],
                    PARAMETER[""false_northing"",-5000000],
                    UNIT[""metre"",1]]");

            // EPSG:4326 - WGS84
            var target = csFactory.CreateFromWkt(@"
                GEOGCS[""WGS 84"",
                    DATUM[""WGS_1984"",
                        SPHEROID[""WGS 84"",6378137,298.257223563]],
                    PRIMEM[""Greenwich"",0],
                    UNIT[""degree"",0.0174532925199433]]");

            return ctFactory.CreateFromCoordinateSystems(source, target);
        }

        /// <summary>
        /// Converts EPSG:31254 to WGS84 using ProjNet
        /// </summary>
        public static (double latitude, double longitude) ConvertWithProjNet(double x, double y)
        {
            try
            {
                var sourcePoint = new double[] { x, y };
                var targetPoint = _transformation.Value.MathTransform.Transform(sourcePoint);

                // ProjNet returns [longitude, latitude], but we want [latitude, longitude]
                return (targetPoint[1], targetPoint[0]);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert coordinates from EPSG:31254 to WGS84. X={x}, Y={y}. Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Converts a NetTopologySuite Coordinate from EPSG:31254 to WGS84
        /// </summary>
        public static (double latitude, double longitude) ConvertWithProjNet(Coordinate coordinate)
        {
            if (coordinate == null)
                throw new ArgumentNullException(nameof(coordinate));

            return ConvertWithProjNet(coordinate.X, coordinate.Y);
        }

        /// <summary>
        /// Converts the first coordinate from a Geometry
        /// </summary>
        public static (double latitude, double longitude) ConvertFirstCoordinateWithProjNet(Geometry geometry)
        {
            if (geometry?.Coordinates?.Length > 0)
            {
                return ConvertWithProjNet(geometry.Coordinates[0]);
            }
            throw new ArgumentException("Geometry is null or has no coordinates", nameof(geometry));
        }

        /// <summary>
        /// Method 2: Manual conversion using Austrian GK formulas
        /// This is specific to EPSG:31254 (MGI / Austria GK West)
        /// </summary>
        public static (double latitude, double longitude) ConvertWithManualFormulas(double x, double y)
        {
            // Constants for Austrian GK West (EPSG:31254)
            const double a = 6377397.155; // Bessel ellipsoid semi-major axis
            const double f = 1.0 / 299.1528128; // Bessel ellipsoid flattening
            const double e2 = 2 * f - f * f; // First eccentricity squared
            const double lambda0 = 10.33333333333333 * Math.PI / 180.0; // Central meridian in radians
            const double k0 = 1.0; // Scale factor
            const double falseNorthing = -5000000.0; // False northing

            // Convert to true northing
            double N = y - falseNorthing;
            double E = x;

            // Intermediate calculations
            double M = N / k0;
            double mu = M / (a * (1 - e2 / 4 - 3 * e2 * e2 / 64 - 5 * Math.Pow(e2, 3) / 256));

            double e1 = (1 - Math.Sqrt(1 - e2)) / (1 + Math.Sqrt(1 - e2));
            double phi1 = mu + (3 * e1 / 2 - 27 * Math.Pow(e1, 3) / 32) * Math.Sin(2 * mu)
                          + (21 * e1 * e1 / 16 - 55 * Math.Pow(e1, 4) / 32) * Math.Sin(4 * mu)
                          + (151 * Math.Pow(e1, 3) / 96) * Math.Sin(6 * mu);

            double nu1 = a / Math.Sqrt(1 - e2 * Math.Sin(phi1) * Math.Sin(phi1));
            double rho1 = a * (1 - e2) / Math.Pow(1 - e2 * Math.Sin(phi1) * Math.Sin(phi1), 1.5);
            double T1 = Math.Tan(phi1) * Math.Tan(phi1);
            double C1 = e2 * Math.Cos(phi1) * Math.Cos(phi1) / (1 - e2);
            double D = E / (nu1 * k0);

            // Latitude calculation
            double phi = phi1 - (nu1 * Math.Tan(phi1) / rho1) *
                         (D * D / 2 - (5 + 3 * T1 + 10 * C1 - 4 * C1 * C1 - 9 * e2) * Math.Pow(D, 4) / 24
                          + (61 + 90 * T1 + 298 * C1 + 45 * T1 * T1 - 252 * e2 - 3 * C1 * C1) * Math.Pow(D, 6) / 720);

            // Longitude calculation
            double lambda = lambda0 + (D - (1 + 2 * T1 + C1) * Math.Pow(D, 3) / 6
                                      + (5 - 2 * C1 + 28 * T1 - 3 * C1 * C1 + 8 * e2 + 24 * T1 * T1) * Math.Pow(D, 5) / 120) / Math.Cos(phi1);

            // Convert to degrees
            double latitude = phi * 180.0 / Math.PI;
            double longitude = lambda * 180.0 / Math.PI;

            return (latitude, longitude);
        }

        /// <summary>
        /// Method 3: Using approximate conversion (less accurate but simpler)
        /// </summary>
        public static (double latitude, double longitude) ConvertApproximate(double x, double y)
        {
            // This is a simplified conversion for Austrian coordinates
            // Note: This is approximate and should only be used if high precision isn't needed

            // Convert false northing
            double adjustedY = y + 5000000.0; // Remove false northing

            // Approximate conversion factors (derived empirically for Austria)
            double lat = (adjustedY * 0.00000898311) + 46.0;
            double lon = (x * 0.00001396263) + 9.5;

            return (lat, lon);
        }
    }

    // Usage examples and testing
    public class CoordinateConversionTesting
    {
        public static void TestConversions()
        {
            // Your original coordinate
            double x = 255135.99770000;
            double y = 46884.24260000;

            Console.WriteLine($"Original coordinates (EPSG:31254): X={x}, Y={y}");
            Console.WriteLine();

            // Method 1: ProjNet (Most accurate)
            try
            {
                var (lat1, lon1) = CoordinateConverterEPSG31254ToWGS84.ConvertWithProjNet(x, y);
                Console.WriteLine($"ProjNet conversion:");
                Console.WriteLine($"Latitude: {lat1:F14}");
                Console.WriteLine($"Longitude: {lon1:F14}");
                Console.WriteLine($"Combined: {lat1:F14}, {lon1:F14}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ProjNet conversion failed: {ex.Message}");
            }

            // Method 2: Manual formulas
            try
            {
                var (lat2, lon2) = CoordinateConverterEPSG31254ToWGS84.ConvertWithManualFormulas(x, y);
                Console.WriteLine($"Manual formula conversion:");
                Console.WriteLine($"Latitude: {lat2:F14}");
                Console.WriteLine($"Longitude: {lon2:F14}");
                Console.WriteLine($"Combined: {lat2:F14}, {lon2:F14}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Manual conversion failed: {ex.Message}");
            }

            // Method 3: Approximate
            var (lat3, lon3) = CoordinateConverterEPSG31254ToWGS84.ConvertApproximate(x, y);
            Console.WriteLine($"Approximate conversion:");
            Console.WriteLine($"Latitude: {lat3:F14}");
            Console.WriteLine($"Longitude: {lon3:F14}");
            Console.WriteLine($"Combined: {lat3:F14}, {lon3:F14}");
            Console.WriteLine();

            // Show why UTM doesn't work
            Console.WriteLine("Why UTM conversion is wrong:");
            Console.WriteLine("- EPSG:31254 is Austrian Gauss-Krüger, not UTM");
            Console.WriteLine("- Different projection, different parameters");
            Console.WriteLine("- UTM zones don't apply to Austrian coordinate system");
        }

        // Extension method for your existing code
        public static (double latitude, double longitude) GetLatLongFromGeometry(Geometry geometry)
        {
            if (geometry?.Coordinates?.Length > 0)
            {
                var firstCoord = geometry.Coordinates[0];
                return CoordinateConverterEPSG31254ToWGS84.ConvertWithProjNet(firstCoord.X, firstCoord.Y);
            }
            return (0, 0);
        }        
    }
}
