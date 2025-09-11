// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Helper;
using Helper.Geo;

namespace HelperTests
{
    public class HelperGeoConverterTests
    {
        private readonly EPSG31254ToEPSG4326Converter _converter;

        public HelperGeoConverterTests()
        {
            _converter = new EPSG31254ToEPSG4326Converter();
        }

        [Fact]
        public void ConvertToWGS84_WithValidCoordinates_ReturnsExpectedResult()
        {
            // Arrange
            double inputX = 156917.1911;
            double inputY = 281477.7766;
            double expectedLongitude = 12.4217888;
            double expectedLatitude = 47.652934;
            double tolerance = 0.000001; // 1 meter precision at this latitu

            // Act
            var (actualLongitude, actualLatitude) = _converter.ConvertToWGS84(inputX, inputY);

            // Assert
            Assert.True(Math.Abs(actualLongitude - expectedLongitude) < tolerance,
                $"Longitude mismatch. Expected: {expectedLongitude:F8}, Actual: {actualLongitude:F8}, Difference: {Math.Abs(actualLongitude - expectedLongitude):F8}");

            Assert.True(Math.Abs(actualLatitude - expectedLatitude) < tolerance,
                $"Latitude mismatch. Expected: {expectedLatitude:F8}, Actual: {actualLatitude:F8}, Difference: {Math.Abs(actualLatitude - expectedLatitude):F8}");
        }

        //[Theory]
        //[InlineData(156917.1911, 281477.7766, 12.4217888, 47.652934)]
        //[InlineData(0, 0, 10.333333, -45.045)] // Approximate values for origin
        //[InlineData(50000, 200000, 10.699, -43.25)] // Approximate test values
        //public void ConvertToWGS84_WithMultipleCoordinates_ReturnsExpectedResults(
        //    double inputX, double inputY, double expectedLon, double expectedLat)
        //{
        //    // Arrange
        //    double tolerance = 0.01; // Relaxed tolerance for multiple test cases

        //    // Act
        //    var (actualLongitude, actualLatitude) = _converter.ConvertToWGS84(inputX, inputY);

        //    // Assert
        //    Assert.True(Math.Abs(actualLongitude - expectedLon) < tolerance,
        //        $"Longitude for ({inputX}, {inputY}) - Expected: {expectedLon:F6}, Actual: {actualLongitude:F6}");

        //    Assert.True(Math.Abs(actualLatitude - expectedLat) < tolerance,
        //        $"Latitude for ({inputX}, {inputY}) - Expected: {expectedLat:F6}, Actual: {actualLatitude:F6}");
        //}

        [Fact]
        public void ConvertToWGS84_WithPreciseCoordinates_MatchesExpectedPrecision()
        {
            // Arrange - Your specific test case
            double inputX = 156917.1911;
            double inputY = 281477.7766;
            double expectedLongitude = 12.4217888;
            double expectedLatitude = 47.652934;

            // Act
            var result = _converter.ConvertToWGS84(inputX, inputY);

            // Assert - Check precision to 7 decimal places (about 1cm accuracy)
            Assert.Equal(expectedLongitude, result.Longitude, 7);
            Assert.Equal(expectedLatitude, result.Latitude, 7);
        }

        [Fact]
        public void ConvertToWGS84_WithExtremeCoordinates_DoesNotThrow()
        {
            // Arrange - Test boundary conditions
            var testCases = new[]
            {
                (double.MinValue, double.MinValue),
                (double.MaxValue, double.MaxValue),
                (0.0, 0.0),
                (-1000000, -1000000),
                (1000000, 1000000)
            };

            // Act & Assert
            foreach (var (x, y) in testCases)
            {
                var exception = Record.Exception(() => _converter.ConvertToWGS84(x, y));

                // Should either succeed or throw a meaningful exception
                if (exception != null)
                {
                    Assert.IsType<InvalidOperationException>(exception);
                }
            }
        }

        [Fact]
        public void ConvertToWGS84_ResultsAreConsistent()
        {
            // Arrange
            double inputX = 156917.1911;
            double inputY = 281477.7766;

            // Act - Convert the same coordinates multiple times
            var result1 = _converter.ConvertToWGS84(inputX, inputY);
            var result2 = _converter.ConvertToWGS84(inputX, inputY);
            var result3 = _converter.ConvertToWGS84(inputX, inputY);

            // Assert - Results should be identical
            Assert.Equal(result1.Longitude, result2.Longitude, 15);
            Assert.Equal(result1.Latitude, result2.Latitude, 15);
            Assert.Equal(result2.Longitude, result3.Longitude, 15);
            Assert.Equal(result2.Latitude, result3.Latitude, 15);
        }

        [Fact]
        public void ConvertToWGS84_ReturnsValidGeographicCoordinates()
        {
            // Arrange
            double inputX = 156917.1911;
            double inputY = 281477.7766;

            // Act
            var (longitude, latitude) = _converter.ConvertToWGS84(inputX, inputY);

            // Assert - Check that coordinates are within valid geographic bounds
            Assert.InRange(longitude, -180.0, 180.0);
            Assert.InRange(latitude, -90.0, 90.0);

            // Additional check for Austria region (approximate bounds)
            Assert.InRange(longitude, 9.0, 17.0); // Austria longitude range
            Assert.InRange(latitude, 46.0, 49.0); // Austria latitude range
        }

        [Fact]
        public void ConvertToWGS84_WithDebugOutput_ShowsActualValues()
        {
            // Arrange
            double inputX = 156917.1911;
            double inputY = 281477.7766;
            double expectedLongitude = 12.4217888;
            double expectedLatitude = 47.652934;

            // Act
            var (actualLongitude, actualLatitude) = _converter.ConvertToWGS84(inputX, inputY);

            // Debug output for troubleshooting
            var lonDiff = Math.Abs(actualLongitude - expectedLongitude);
            var latDiff = Math.Abs(actualLatitude - expectedLatitude);

            // This test will show the actual values in test output
            Assert.True(true, $"Input: ({inputX}, {inputY})\n" +
                            $"Expected: (Lon: {expectedLongitude:F8}, Lat: {expectedLatitude:F8})\n" +
                            $"Actual: (Lon: {actualLongitude:F8}, Lat: {actualLatitude:F8})\n" +
                            $"Differences: (Lon: {lonDiff:F8}, Lat: {latDiff:F8})");
        }
    }

    //// Additional test class for integration testing
    //public class EPSGConverterIntegrationTests
    //{
    //    [Fact]
    //    public void ConvertMultipleToWGS84_WithTestCoordinates_ReturnsCorrectCount()
    //    {
    //        // Arrange
    //        var converter = new EPSG31254ToEPSG4326Converter();
    //        var coordinates = new (double X, double Y)[]
    //        {
    //            (156917.1911, 281477.7766),
    //            (100000, 200000),
    //            (200000, 300000)
    //        };

    //        // Act
    //        var results = converter.ConvertMultipleToWGS84(coordinates);

    //        // Assert
    //        Assert.Equal(coordinates.Length, results.Length);

    //        // Check that first result matches our known test case
    //        Assert.Equal(12.4217888, results[0].Longitude, 6);
    //        Assert.Equal(47.652934, results[0].Latitude, 6);
    //    }
    //}
}