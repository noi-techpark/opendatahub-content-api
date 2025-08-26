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
    public class WFSResult
    {
        public IEnumerable<IWFSRoute> Results { get; set; }
    }

    public interface IWFSRoute
    {
        int? ObjectId { get; set;}
    }

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
        public List<MtbCoordinate> Coordinates { get; set; }
        public Geometry Geometry { get; set; }

        public MountainBikeRoute()
        {
            Coordinates = new List<MtbCoordinate>();
        }
    }

    // Coordinate data model
    public class MtbCoordinate
    {
        public double X { get; set; }
        public double Y { get; set; }

        public MtbCoordinate(double x, double y)
        {
            X = x;
            Y = y;
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
                    route.ObjectId = GetIntValue(routeElement, routeNamespace + "OBJECTID");
                    route.Object = GetStringValue(routeElement, routeNamespace + "OBJEKT");
                    route.RouteType = GetStringValue(routeElement, routeNamespace + "ROUTEN_TYP");
                    route.RouteNumber = GetStringValue(routeElement, routeNamespace + "ROUTENNUMMER");
                    route.RouteName = GetStringValue(routeElement, routeNamespace + "ROUTENNAME");
                    route.RouteStart = GetStringValue(routeElement, routeNamespace + "ROUTENSTART");
                    route.RouteEnd = GetStringValue(routeElement, routeNamespace + "ROUTENZIEL");
                    route.StartElevation = GetIntValue(routeElement, routeNamespace + "HOEHE_START");
                    route.EndElevation = GetIntValue(routeElement, routeNamespace + "HOEHE_ZIEL");
                    route.ElevationUp = GetIntValue(routeElement, routeNamespace + "HM_BERGAUF");
                    route.ElevationDown = GetIntValue(routeElement, routeNamespace + "HM_BERGAB");
                    route.RidingTime = GetStringValue(routeElement, routeNamespace + "FAHRZEIT");
                    route.RouteDescription = GetStringValue(routeElement, routeNamespace + "ROUTENBESCHREIBUNG");
                    route.Status = GetStringValue(routeElement, routeNamespace + "STATUS");
                    route.UpdateTimestamp = GetDateTimeValue(routeElement, routeNamespace + "UPDATETIMESTAMP");
                    route.Difficulty = GetStringValue(routeElement, routeNamespace + "ROUTEN_SCHWIERIGKEIT");
                    route.SectionType = GetStringValue(routeElement, routeNamespace + "ROUTENSEKTION_TYP");
                    route.RouteStartEn = GetStringValue(routeElement, routeNamespace + "ROUTENSTART_EN");
                    route.RouteEndEn = GetStringValue(routeElement, routeNamespace + "ROUTENZIEL_EN");
                    route.LengthKm = GetDoubleValue(routeElement, routeNamespace + "LAENGE_KM");
                    route.RouteDescriptionEn = GetStringValue(routeElement, routeNamespace + "ROUTENBESCHREIBUNG_EN");
                    route.ShapeLength = GetDoubleValue(routeElement, routeNamespace + "Shape__Length");

                    // Parse coordinates from Shape element
                    var shapeElement = routeElement.Element(routeNamespace + "Shape");
                    if (shapeElement != null)
                    {
                        route.Coordinates = ParseCoordinates(shapeElement, gmlNamespace);
                    }

                    routes.Add(route);

                    foreach (var myroute in routes)
                    {
                        // Create LineString geometry from coordinates
                        route.Geometry = GeometryHelper.CreateLineString(myroute.Coordinates);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing XML: {ex.Message}", ex);
            }

            return routes;
        }

        private List<MtbCoordinate> ParseCoordinates(XElement shapeElement, XNamespace gmlNamespace)
        {
            var coordinates = new List<MtbCoordinate>();

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
                                        coordinates.Add(new MtbCoordinate(y, x));
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

        private string GetStringValue(XElement parent, XName elementName)
        {
            return parent.Element(elementName)?.Value ?? string.Empty;
        }

        private int? GetIntValue(XElement parent, XName elementName)
        {
            var value = parent.Element(elementName)?.Value;
            return int.TryParse(value, out int result) ? result : null;
        }

        private double? GetDoubleValue(XElement parent, XName elementName)
        {
            var value = parent.Element(elementName)?.Value;
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ? result : null;
        }

        private DateTime GetDateTimeValue(XElement parent, XName elementName)
        {
            var value = parent.Element(elementName)?.Value;
            return DateTime.TryParse(value, out DateTime result) ? result : DateTime.MinValue;
        }
    }


    // Geometry factory and helper methods
    public static class GeometryHelper
    {
        // Create a geometry factory (you might want to specify SRID if known)
        private static readonly GeometryFactory GeometryFactory = new GeometryFactory(new PrecisionModel(), 31254); // EPSG:31254 from your XML

        /// <summary>
        /// Creates a LineString geometry from a list of MtbCoordinates
        /// </summary>
        public static LineString CreateLineString(List<MtbCoordinate> coordinates)
        {
            if (coordinates == null || coordinates.Count < 2)
                return null;

            var coordinateArray = coordinates.Select(c => new Coordinate(c.X, c.Y)).ToArray();
            return GeometryFactory.CreateLineString(coordinateArray);
        }

        /// <summary>
        /// Creates a Point geometry from a single MtbCoordinate
        /// </summary>
        public static Point CreatePoint(MtbCoordinate coordinate)
        {
            if (coordinate == null)
                return null;

            return GeometryFactory.CreatePoint(new Coordinate(coordinate.X, coordinate.Y));
        }

        /// <summary>
        /// Creates a MultiPoint geometry from a list of MtbCoordinates
        /// </summary>
        public static MultiPoint CreateMultiPoint(List<MtbCoordinate> coordinates)
        {
            if (coordinates == null || coordinates.Count == 0)
                return null;

            var points = coordinates.Select(c => CreatePoint(c)).ToArray();
            return GeometryFactory.CreateMultiPoint(points);
        }

        /// <summary>
        /// Creates a Polygon geometry from a list of MtbCoordinates (if they form a closed ring)
        /// </summary>
        public static Polygon CreatePolygon(List<MtbCoordinate> coordinates)
        {
            if (coordinates == null || coordinates.Count < 4)
                return null;

            var coordinateArray = coordinates.Select(c => new Coordinate(c.X, c.Y)).ToArray();

            // Ensure the ring is closed
            if (!coordinateArray[0].Equals2D(coordinateArray[coordinateArray.Length - 1]))
            {
                var closedArray = new Coordinate[coordinateArray.Length + 1];
                Array.Copy(coordinateArray, closedArray, coordinateArray.Length);
                closedArray[coordinateArray.Length] = new Coordinate(coordinateArray[0]);
                coordinateArray = closedArray;
            }

            var linearRing = GeometryFactory.CreateLinearRing(coordinateArray);
            return GeometryFactory.CreatePolygon(linearRing);
        }

        /// <summary>
        /// Gets the start point of a route
        /// </summary>
        public static Point GetStartPoint(List<MtbCoordinate> coordinates)
        {
            return coordinates?.Count > 0 ? CreatePoint(coordinates.First()) : null;
        }

        /// <summary>
        /// Gets the end point of a route
        /// </summary>
        public static Point GetEndPoint(List<MtbCoordinate> coordinates)
        {
            return coordinates?.Count > 0 ? CreatePoint(coordinates.Last()) : null;
        }
    }
}
