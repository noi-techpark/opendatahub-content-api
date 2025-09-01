// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DIGIWAY.Model
{
    #region Common

    public class WFSResult
    {
        public IEnumerable<IWFSRoute> Results { get; set; }
    }

    public interface IWFSRoute
    {
        int? ObjectId { get; set; }
        Geometry Geometry { get; set; }

    }

    // Coordinate data model
    public class WFSCoordinate
    {
        public double X { get; set; }
        public double Y { get; set; }

        public WFSCoordinate(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public class XmlDataHelper
    {
        public static string GetStringValue(XElement parent, XName elementName)
        {
            return parent.Element(elementName)?.Value ?? string.Empty;
        }

        public static int? GetIntValue(XElement parent, XName elementName)
        {
            var value = parent.Element(elementName)?.Value;
            return int.TryParse(value, out int result) ? result : null;
        }

        public static double? GetDoubleValue(XElement parent, XName elementName)
        {
            var value = parent.Element(elementName)?.Value;
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ? result : null;
        }

        public static DateTime GetDateTimeValue(XElement parent, XName elementName)
        {
            var value = parent.Element(elementName)?.Value;
            return DateTime.TryParse(value, out DateTime result) ? result : DateTime.MinValue;
        }
    }

    #endregion


    #region MTB Routes

    // Data model for mountain bike routes
    public class MountainBikeRoute : IWFSRoute
    {
        public int? ObjectId { get; set; }
        public string Object { get; set; }
        public string RouteType { get; set; }
        public string RouteNumber { get; set; }
        public string RouteName { get; set; }
        public string RouteStart { get; set; }
        public string RouteEnd { get; set; }
        public int? StartElevation { get; set; }
        public int? EndElevation { get; set; }
        public int? ElevationUp { get; set; }
        public int? ElevationDown { get; set; }
        public string RidingTime { get; set; }
        public string RouteDescription { get; set; }
        public string Status { get; set; }
        public DateTime UpdateTimestamp { get; set; }
        public string Difficulty { get; set; }
        public string SectionType { get; set; }
        public string RouteStartEn { get; set; }
        public string RouteEndEn { get; set; }
        public double? LengthKm { get; set; }
        public string RouteDescriptionEn { get; set; }
        public double? ShapeLength { get; set; }
        public List<WFSCoordinate> Coordinates { get; set; }
        public Geometry Geometry { get; set; }

        public MountainBikeRoute()
        {
            Coordinates = new List<WFSCoordinate>();
        }
    }

    // Parser class
    public class WfsMountainBikeRouteParser
    {
        public List<MountainBikeRoute> ParseXml(string xmlContent)
        {
            var routes = new List<MountainBikeRoute>();

            try
            {
                var doc = XDocument.Parse(xmlContent);

                // Define namespaces
                var wfsNamespace = XNamespace.Get("http://www.opengis.net/wfs/2.0");
                var gmlNamespace = XNamespace.Get("http://www.opengis.net/gml/3.2");
                var routeNamespace = XNamespace.Get("https://dservices3.arcgis.com/hG7UfxX49PQ8XkXh/arcgis/services/Radrouten_Tirol/WFSServer");

                // Find all wfs:member elements
                var members = doc.Descendants(wfsNamespace + "member");

                foreach (var member in members)
                {
                    var routeElement = member.Elements().FirstOrDefault();
                    if (routeElement == null) continue;

                    var route = new MountainBikeRoute();

                    // Parse basic route information
                    route.ObjectId = XmlDataHelper.GetIntValue(routeElement, routeNamespace + "OBJECTID");
                    route.Object = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "OBJEKT");
                    route.RouteType = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "ROUTEN_TYP");
                    route.RouteNumber = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "ROUTENNUMMER");
                    route.RouteName = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "ROUTENNAME");
                    route.RouteStart = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "ROUTENSTART");
                    route.RouteEnd = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "ROUTENZIEL");
                    route.StartElevation = XmlDataHelper.GetIntValue(routeElement, routeNamespace + "HOEHE_START");
                    route.EndElevation = XmlDataHelper.GetIntValue(routeElement, routeNamespace + "HOEHE_ZIEL");
                    route.ElevationUp = XmlDataHelper.GetIntValue(routeElement, routeNamespace + "HM_BERGAUF");
                    route.ElevationDown = XmlDataHelper.GetIntValue(routeElement, routeNamespace + "HM_BERGAB");
                    route.RidingTime = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "FAHRZEIT");
                    route.RouteDescription = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "ROUTENBESCHREIBUNG");
                    route.Status = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "STATUS");
                    route.UpdateTimestamp = XmlDataHelper.GetDateTimeValue(routeElement, routeNamespace + "UPDATETIMESTAMP");
                    route.Difficulty = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "ROUTEN_SCHWIERIGKEIT");
                    route.SectionType = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "ROUTENSEKTION_TYP");
                    route.RouteStartEn = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "ROUTENSTART_EN");
                    route.RouteEndEn = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "ROUTENZIEL_EN");
                    route.LengthKm = XmlDataHelper.GetDoubleValue(routeElement, routeNamespace + "LAENGE_KM");
                    route.RouteDescriptionEn = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "ROUTENBESCHREIBUNG_EN");
                    route.ShapeLength = XmlDataHelper.GetDoubleValue(routeElement, routeNamespace + "Shape__Length");

                    // Parse coordinates from Shape element
                    var shapeElement = routeElement.Element(routeNamespace + "Shape");
                    if (shapeElement != null)
                    {
                        route.Coordinates = ParseMTBCoordinates(shapeElement, gmlNamespace);
                    }

                    routes.Add(route);

                    foreach (var myroute in routes)
                    {
                        // Create LineString geometry from coordinates
                        route.Geometry = GeometryHelper.CreateLineString(myroute.Coordinates, GeometryHelper.CommonSRID.AustriaLambert);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing XML: {ex.Message}", ex);
            }

            return routes;
        }

        private List<WFSCoordinate> ParseMTBCoordinates(XElement shapeElement, XNamespace gmlNamespace)
        {
            var coordinates = new List<WFSCoordinate>();

            try
            {
                // Navigate through the GML structure: MultiCurve -> curveMember -> LineString -> posList
                var multiCurve = shapeElement.Element(gmlNamespace + "MultiCurve");
                if (multiCurve != null)
                {
                    var curveMember = multiCurve.Element(gmlNamespace + "curveMember");
                    if (curveMember != null)
                    {
                        var lineString = curveMember.Element(gmlNamespace + "LineString");
                        if (lineString != null)
                        {
                            var posList = lineString.Element(gmlNamespace + "posList");
                            if (posList != null)
                            {
                                var posValues = posList.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                // Coordinates come in pairs (X, Y)
                                for (int i = 0; i < posValues.Length - 1; i += 2)
                                {
                                    if (double.TryParse(posValues[i], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                                        double.TryParse(posValues[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                                    {
                                        coordinates.Add(new WFSCoordinate(y, x));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the entire parsing process
                Console.WriteLine($"Error parsing coordinates: {ex.Message}");
            }

            return coordinates;
        }
    }

    #endregion

    #region E5 Routes

    // Data model for mountain bike routes
    public class E5TrailRoute : IWFSRoute
    {
        public int? ObjectId { get; set; }
        public string PathCode { get; set; }
        public string PathDe { get; set; }
        public string PathIt { get; set; }
        public string PathEs { get; set; }
        public string ResporgDigiwayCode { get; set; }
        public string ResporgDigiwayDe { get; set; }
        public string ResporgDigiwayIt { get; set; }
        public string ResporgDigiwayEs { get; set; }
        public double? ObjectIdGip { get; set; }
        public string GlobalId { get; set; }
        public double? ShapeLength { get; set; }
        public List<WFSCoordinate> Coordinates { get; set; }
        public Geometry Geometry { get; set; }

        public E5TrailRoute()
        {
            Coordinates = new List<WFSCoordinate>();
        }
    }

    // Parser class
    public class WfsE5TrailParser
    {
        public List<E5TrailRoute> ParseXml(string xmlContent)
        {
            var routes = new List<E5TrailRoute>();

            try
            {
                var doc = XDocument.Parse(xmlContent);

                // Define namespaces
                var wfsNamespace = XNamespace.Get("http://www.opengis.net/wfs/2.0");
                var gmlNamespace = XNamespace.Get("http://www.opengis.net/gml/3.2");
                var routeNamespace = XNamespace.Get("https://dservices3.arcgis.com/hG7UfxX49PQ8XkXh/arcgis/services/E5_Hiking_Route/WFSServer");

                // Find all wfs:member elements
                var members = doc.Descendants(wfsNamespace + "member");

                foreach (var member in members)
                {
                    var routeElement = member.Elements().FirstOrDefault();
                    if (routeElement == null) continue;

                    var route = new E5TrailRoute();

                    // Parse basic route information
                    route.ObjectId = XmlDataHelper.GetIntValue(routeElement, routeNamespace + "OBJECTID");
                    route.PathCode = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "PATH_CODE");
                    route.PathDe = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "PATH_D");
                    route.PathIt = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "PATH_I");
                    route.PathEs = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "PATH_E");
                    route.ResporgDigiwayCode = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "RESPORGDIGIWAY_CODE");
                    route.ResporgDigiwayDe = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "RESPORGDIGIWAY_D");
                    route.ResporgDigiwayIt = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "RESPORGDIGIWAY_I");
                    route.ResporgDigiwayEs = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "RESPORGDIGIWAY_E");
                    route.ObjectIdGip = XmlDataHelper.GetDoubleValue(routeElement, routeNamespace + "OBJECTID_GIP");
                    route.GlobalId = XmlDataHelper.GetStringValue(routeElement, routeNamespace + "GlobalID");
                    route.ShapeLength = XmlDataHelper.GetDoubleValue(routeElement, routeNamespace + "Shape__Length");

                    // Parse coordinates from Shape element
                    var shapeElement = routeElement.Element(routeNamespace + "Shape");
                    if (shapeElement != null)
                    {
                        route.Coordinates = ParseTrailCoordinates(shapeElement, gmlNamespace);
                    }

                    routes.Add(route);

                    foreach (var myroute in routes)
                    {
                        // Create LineString geometry from coordinates
                        route.Geometry = GeometryHelper.CreateLineString(myroute.Coordinates, GeometryHelper.CommonSRID.WebMercator);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing XML: {ex.Message}", ex);
            }

            return routes;
        }

        private List<WFSCoordinate> ParseTrailCoordinates(XElement shapeElement, XNamespace gmlNamespace)
        {
            var coordinates = new List<WFSCoordinate>();

            try
            {
                // Navigate through the GML structure: MultiCurve -> curveMember -> LineString -> posList
                var multiCurve = shapeElement.Element(gmlNamespace + "MultiCurve");
                if (multiCurve != null)
                {
                    var curveMember = multiCurve.Element(gmlNamespace + "curveMember");
                    if (curveMember != null)
                    {
                        var lineString = curveMember.Element(gmlNamespace + "LineString");
                        if (lineString != null)
                        {
                            var posList = lineString.Element(gmlNamespace + "posList");
                            if (posList != null)
                            {
                                var posValues = posList.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                // Coordinates come in pairs (X, Y)
                                for (int i = 0; i < posValues.Length - 1; i += 2)
                                {
                                    if (double.TryParse(posValues[i], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                                        double.TryParse(posValues[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                                    {
                                        coordinates.Add(new WFSCoordinate(y, x));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the entire parsing process
                Console.WriteLine($"Error parsing coordinates: {ex.Message}");
            }

            return coordinates;
        }

       
    }

    #endregion

    #region Geometry

    // Geometry factory and helper methods
    public static class GeometryHelper
    {
        // Default SRID (EPSG:31254 from your original XML)
        private const int DefaultSRID = 31254;

        /// <summary>
        /// Creates a geometry factory with specified SRID
        /// </summary>
        /// <param name="srid">Spatial Reference System Identifier (EPSG code)</param>
        /// <returns>GeometryFactory configured with the specified SRID</returns>
        private static GeometryFactory CreateGeometryFactory(int srid = DefaultSRID)
        {
            return new GeometryFactory(new PrecisionModel(), srid);
        }

        /// <summary>
        /// Creates a LineString geometry from a list of WFSCoordinates
        /// </summary>
        /// <param name="coordinates">List of coordinates</param>
        /// <param name="srid">Spatial Reference System Identifier (optional, defaults to 31254)</param>
        /// <returns>LineString geometry or null if invalid input</returns>
        public static LineString CreateLineString(List<WFSCoordinate> coordinates, int srid = DefaultSRID)
        {
            if (coordinates == null || coordinates.Count < 2)
                return null;

            var geometryFactory = CreateGeometryFactory(srid);
            var coordinateArray = coordinates.Select(c => new Coordinate(c.X, c.Y)).ToArray();
            return geometryFactory.CreateLineString(coordinateArray);
        }
        /// <summary>
        /// Creates a Point geometry from a single WFSCoordinate
        /// </summary>
        /// <param name="coordinate">Single coordinate</param>
        /// <param name="srid">Spatial Reference System Identifier (optional, defaults to 31254)</param>
        /// <returns>Point geometry or null if invalid input</returns>
        public static Point CreatePoint(WFSCoordinate coordinate, int srid = DefaultSRID)
        {
            if (coordinate == null)
                return null;

            var geometryFactory = CreateGeometryFactory(srid);
            return geometryFactory.CreatePoint(new Coordinate(coordinate.X, coordinate.Y));
        }

        /// <summary>
        /// Creates a MultiPoint geometry from a list of WFSCoordinates
        /// </summary>
        /// <param name="coordinates">List of coordinates</param>
        /// <param name="srid">Spatial Reference System Identifier (optional, defaults to 31254)</param>
        /// <returns>MultiPoint geometry or null if invalid input</returns>
        public static MultiPoint CreateMultiPoint(List<WFSCoordinate> coordinates, int srid = DefaultSRID)
        {
            if (coordinates == null || coordinates.Count == 0)
                return null;

            var geometryFactory = CreateGeometryFactory(srid);
            var points = coordinates.Select(c => CreatePoint(c, srid)).ToArray();
            return geometryFactory.CreateMultiPoint(points);
        }

        /// <summary>
        /// Creates a Polygon geometry from a list of WFSCoordinates (if they form a closed ring)
        /// </summary>
        /// <param name="coordinates">List of coordinates forming the polygon boundary</param>
        /// <param name="srid">Spatial Reference System Identifier (optional, defaults to 31254)</param>
        /// <returns>Polygon geometry or null if invalid input</returns>
        public static Polygon CreatePolygon(List<WFSCoordinate> coordinates, int srid = DefaultSRID)
        {
            if (coordinates == null || coordinates.Count < 4)
                return null;

            var geometryFactory = CreateGeometryFactory(srid);
            var coordinateArray = coordinates.Select(c => new Coordinate(c.X, c.Y)).ToArray();

            // Ensure the ring is closed
            if (!coordinateArray[0].Equals2D(coordinateArray[coordinateArray.Length - 1]))
            {
                var closedArray = new Coordinate[coordinateArray.Length + 1];
                Array.Copy(coordinateArray, closedArray, coordinateArray.Length);
                closedArray[coordinateArray.Length] = new Coordinate(coordinateArray[0]);
                coordinateArray = closedArray;
            }

            var linearRing = geometryFactory.CreateLinearRing(coordinateArray);
            return geometryFactory.CreatePolygon(linearRing);
        }

        /// <summary>
        /// Gets the start point of a route
        /// </summary>
        /// <param name="coordinates">List of route coordinates</param>
        /// <param name="srid">Spatial Reference System Identifier (optional, defaults to 31254)</param>
        /// <returns>Point geometry representing the start point or null if invalid input</returns>
        public static Point GetStartPoint(List<WFSCoordinate> coordinates, int srid = DefaultSRID)
        {
            return coordinates?.Count > 0 ? CreatePoint(coordinates.First(), srid) : null;
        }

        /// <summary>
        /// Gets the end point of a route
        /// </summary>
        /// <param name="coordinates">List of route coordinates</param>
        /// <param name="srid">Spatial Reference System Identifier (optional, defaults to 31254)</param>
        /// <returns>Point geometry representing the end point or null if invalid input</returns>
        public static Point GetEndPoint(List<WFSCoordinate> coordinates, int srid = DefaultSRID)
        {
            return coordinates?.Count > 0 ? CreatePoint(coordinates.Last(), srid) : null;
        }

        /// <summary>
        /// Creates a geometry collection from multiple geometries with the same SRID
        /// </summary>
        /// <param name="geometries">Array of geometries to combine</param>
        /// <param name="srid">Spatial Reference System Identifier (optional, defaults to 31254)</param>
        /// <returns>GeometryCollection or null if invalid input</returns>
        public static GeometryCollection CreateGeometryCollection(Geometry[] geometries, int srid = DefaultSRID)
        {
            if (geometries == null || geometries.Length == 0)
                return null;

            var geometryFactory = CreateGeometryFactory(srid);
            return geometryFactory.CreateGeometryCollection(geometries);
        }

        /// <summary>
        /// Transforms geometry from one SRID to another (requires additional transformation logic)
        /// </summary>
        /// <param name="geometry">Source geometry</param>
        /// <param name="targetSrid">Target SRID</param>
        /// <returns>Transformed geometry with new SRID</returns>
        /// <remarks>This is a placeholder - actual coordinate transformation requires additional libraries like ProjNet</remarks>
        public static Geometry TransformSRID(Geometry geometry, int targetSrid)
        {
            if (geometry == null || geometry.SRID == targetSrid)
                return geometry;

            // TODO: Implement actual coordinate transformation using ProjNet or similar
            // For now, just update the SRID (coordinates remain unchanged)
            var newGeometry = geometry.Copy();
            newGeometry.SRID = targetSrid;
            return newGeometry;
        }

        /// <summary>
        /// Gets common EPSG codes for reference
        /// </summary>
        public static class CommonSRID
        {
            /// <summary>WGS84 Geographic (Latitude/Longitude)</summary>
            public const int WGS84 = 4326;

            /// <summary>WGS84 Web Mercator (Google Maps, OSM)</summary>
            public const int WebMercator = 3857;

            /// <summary>Austria MGI / Austria Lambert</summary>
            public const int AustriaLambert = 31254;

            /// <summary>UTM Zone 33N (Central Europe)</summary>
            public const int UTM33N = 32633;

            /// <summary>ETRS89 / UTM Zone 33N</summary>
            public const int ETRS89_UTM33N = 25833;

            /// <summary>ETRS89 / UTM Zone 32N</summary>
            public const int ETRS89_UTM32N = 25832;
        }
    }

    #endregion
}
