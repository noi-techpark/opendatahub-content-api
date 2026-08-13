// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using SqlKata;

namespace Helper
{
    public static class PostgresSQLHelper
    {
        #region Geo Helpers

        // Disabled: cube/earthdistance based geo helpers, superseded by the PostGIS gen_position
        // based "_GeneratedColumns" helpers below. All former callers (LocationInfoHelper.GetNearestDistrictbyGPS,
        // AvailabilitySearchInterceptorAttribute, PostgresSQLOrderByBuilder.ApplyOrdering) have been
        // migrated to the PostGIS versions - kept commented out for reference, safe to delete later.
        /*

        //For Activities Pois and Smgpois

        public static string GetGeoWhereSimple(double latitude, double longitude, int radius)
        {
            return $"earth_distance(ll_to_earth({latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}),ll_to_earth((data#>>'\\{{Latitude\\}}')::double precision, (data#>>'\\{{Longitude\\}}')::double precision)) < {radius.ToString()}";
        }

        //public static string GetGeoWhereSimple(string latitude, string longitude, string radius)
        //{
        //    return "earth_distance(ll_to_earth(" + latitude + ", " + longitude + "),ll_to_earth((data->>'Latitude')::double precision, (data->>'Longitude')::double precision)) < " + radius;
        //}

        public static string GetGeoOrderBySimple(double latitude, double longitude)
        {
            return $"earth_distance(ll_to_earth({latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}),ll_to_earth((data#>>'\\{{Latitude\\}}')::double precision, (data#>>'\\{{Longitude\\}}')::double precision))";
        }

        //public static string GetGeoOrderBySimple(string latitude, string longitude)
        //{
        //    return "earth_distance(ll_to_earth(" + latitude + ", " + longitude + "),ll_to_earth((data->>'Latitude')::double precision, (data->>'Longitude')::double precision))";
        //}

        public static string GetGeoWhereExtended(double latitude, double longitude, int radius)
        {
            return $"earth_distance(ll_to_earth({latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}),ll_to_earth((data#>>'\\{{GpsPoints,position,Latitude\\}}')::double precision, (data#>>'\\{{GpsPoints,position,Longitude\\}}')::double precision)) < {radius.ToString()}";
        }

        public static string GetGeoWhereExtendedGpsInfo(
            double latitude,
            double longitude,
            int radius
        )
        {
            return $"earth_distance(ll_to_earth({latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}),ll_to_earth((data#>>'\\{{GpsInfo,0,Latitude\\}}')::double precision, (data#>>'\\{{GpsInfo,0,Longitude\\}}')::double precision)) < {radius.ToString()}";
        }

        //public static string GetGeoWhereExtended(string latitude, string longitude, string radius)
        //{
        //    return "earth_distance(ll_to_earth(" + latitude + ", " + longitude + "),ll_to_earth((data->'GpsPoints'->'position'->>'Latitude')::double precision, (data->'GpsPoints'->'position'->>'Longitude')::double precision)) < " + radius;
        //}

        public static string GetGeoOrderByExtended(double latitude, double longitude)
        {
            return $"earth_distance(ll_to_earth({latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}),ll_to_earth((data#>>'\\{{GpsPoints,position,Latitude\\}}')::double precision, (data#>>'\\{{GpsPoints,position,Longitude\\}}')::double precision))";
        }

        public static string GetGeoOrderByExtended(string latitude, string longitude)
        {
            return $"earth_distance(ll_to_earth({latitude}, {longitude}),ll_to_earth((data->'GpsPoints'->'position'#>>'\\{{Latitude\\}}')::double precision, (data#>>'\\{{GpsPoints,position,Longitude\\}}')::double precision))";
        }

        public static string GetGeoWhereBoundingBoxes(
            string latitude,
            string longitude,
            string radius
        )
        {
            return $"earth_box(ll_to_earth({latitude}, {longitude}), {radius}) @> ll_to_earth((data#>>'\\{{Latitude\\}}')::double precision, (data#>>'\\{{Longitude\\}}')::double precision) and earth_distance(ll_to_earth({latitude}, {longitude}), ll_to_earth((data#>>'\\{{Latitude\\}}')::double precision, (data#>>'\\{{Longitude\\}}')::double precision)) < {radius}";
        }

        public static string GetGeoWhereBoundingBoxes(double latitude, double longitude, int radius)
        {
            return $"earth_box(ll_to_earth({latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}), {radius.ToString()}) @> ll_to_earth((data#>>'\\{{Latitude\\}}')::double precision, (data#>>'\\{{Longitude\\}}')::double precision) and earth_distance(ll_to_earth({latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}), ll_to_earth((data#>>'\\{{Latitude\\}}')::double precision, (data#>>'\\{{Longitude\\}}')::double precision)) < {radius.ToString()}";
        }

        public static string GetGeoWhereBoundingBoxesExtended(
            string latitude,
            string longitude,
            string radius
        )
        {
            return $"earth_box(ll_to_earth({latitude}, {longitude}), {radius}) @> ll_to_earth((data#>>'\\{{GpsPoints,position,Latitude\\}}')::double precision, (data#>>'\\{{GpsPoints,position,Longitude\\}}')::double precision) and earth_distance(ll_to_earth({latitude}, {longitude}), ll_to_earth((data#>>'\\{{GpsPoints,position,Latitude\\}}')::double precision, (data#>>'\\{{GpsPoints,position,Longitude\\}}')::double precision)) < {radius}";
        }

        public static string GetGeoWhereBoundingBoxesExtended(
            double latitude,
            double longitude,
            int radius
        )
        {
            return $"earth_box(ll_to_earth({latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}), {radius.ToString()}) @> ll_to_earth((data#>>'\\{{GpsPoints,position,Latitude\\}}')::double precision, (data#>>'\\{{GpsPoints,position,Longitude\\}}')::double precision) and earth_distance(ll_to_earth({latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}), ll_to_earth((data#>>'\\{{GpsPoints,position,Latitude\\}}')::double precision, (data#>>'\\{{GpsPoints,position,Longitude\\}}')::double precision)) < {radius.ToString()}";
        }

        //For Accommodations
        public static void ApplyGeoSearchWhereOrderbySimple(
            ref string where,
            ref string orderby,
            PGGeoSearchResult geosearchresult
        )
        {
            if (geosearchresult != null)
            {
                if (geosearchresult.geosearch)
                {
                    if (!String.IsNullOrEmpty(where))
                        where += " AND ";

                    where += PostgresSQLHelper.GetGeoWhereSimple(
                        geosearchresult.latitude,
                        geosearchresult.longitude,
                        geosearchresult.radius
                    );
                    orderby = PostgresSQLHelper.GetGeoOrderBySimple(
                        geosearchresult.latitude,
                        geosearchresult.longitude
                    );
                }
            }
        }

        public static Query GeoSearchFilterAndOrderby(
            this Query query,
            PGGeoSearchResult? geosearchresult
        )
        {
            if (geosearchresult == null || !geosearchresult.geosearch)
                return query;

            return query
                .WhereRaw(
                    GetGeoWhereExtended(
                        geosearchresult.latitude,
                        geosearchresult.longitude,
                        geosearchresult.radius
                    )
                )
                .OrderByRaw(
                    GetGeoOrderByExtended(geosearchresult.latitude, geosearchresult.longitude)
                );
        }

        //For Activities Pois and GBActivityPoi
        public static void ApplyGeoSearchWhereOrderby(
            ref string where,
            ref string orderby,
            PGGeoSearchResult geosearchresult
        )
        {
            if (geosearchresult != null)
            {
                if (geosearchresult.geosearch)
                {
                    if (!String.IsNullOrEmpty(where))
                        where += " AND ";

                    where += PostgresSQLHelper.GetGeoWhereExtended(
                        geosearchresult.latitude,
                        geosearchresult.longitude,
                        geosearchresult.radius
                    );
                    orderby = PostgresSQLHelper.GetGeoOrderByExtended(
                        geosearchresult.latitude,
                        geosearchresult.longitude
                    );
                }
            }
        }

        */

        #endregion

        #region Geo Helpers Generated Columns

        //For Activities Pois and Smgpois

        /// <summary>
        /// WKT point (lon/lat order, matching ST_MakePoint) for the given coordinates, SRID 4326 (WGS84).
        /// </summary>
        private static string GetGeoPoint_GeneratedColumns(double latitude, double longitude)
        {
            return $"ST_SetSRID(ST_MakePoint({longitude.ToString(CultureInfo.InvariantCulture)}, {latitude.ToString(CultureInfo.InvariantCulture)}), 4326)";
        }

        private static string GetGeoPoint_GeneratedColumns(string latitude, string longitude)
        {
            return $"ST_SetSRID(ST_MakePoint({longitude}, {latitude}), 4326)";
        }

        public static string GetGeoWhereSimple_GeneratedColumns(
            double latitude,
            double longitude,
            int radius,
            string geometryColumn = "gen_position"
        )
        {
            //ST_DWithin on a geography cast computes the great-circle distance in meters and can still use
            //a GiST index on the underlying geometry column, replacing the old cube/earthdistance based
            //earth_distance()/ll_to_earth() combo.
            return $"ST_DWithin({geometryColumn}::geography, {GetGeoPoint_GeneratedColumns(latitude, longitude)}::geography, {radius.ToString(CultureInfo.InvariantCulture)})";
        }

        public static string GetGeoWhereInPolygon_GeneratedColumns(
            string? wkt,
            List<Tuple<double, double>>? polygon,
            string srid,
            string? operation = null,
            bool reduceprecision = false,
            string geometryColumn = "gen_position"
        )
        {
            if (String.IsNullOrEmpty(wkt))
                return GetGeoWhereInPolygon_GeneratedColumns(polygon, srid, operation, reduceprecision, geometryColumn);
            else
                return GetGeoWhereInPolygon_GeneratedColumns(wkt, srid, operation, reduceprecision, geometryColumn);
        }

        public static string GetGeoWhereInPolygon_GeneratedColumns(
            List<Tuple<double, double>> polygon,
            string srid = "4326",
            string? operation = "intersects",
            bool reduceprecision = false,
            string geometryColumn = "gen_position"
        )
        {
            string wkt = $"POLYGON(({String.Join(",", polygon.Select(t => string.Format("{0} {1}", t.Item1.ToString(CultureInfo.InvariantCulture), t.Item2.ToString(CultureInfo.InvariantCulture))))}))";
            return GetGeoWhereInPolygon_GeneratedColumns(wkt, srid, operation, reduceprecision, geometryColumn);

            //if (srid != "4326")
            //    return $"{GetPolygonOperator(operation)}(ST_GeometryFromText('POLYGON(({String.Join(",", polygon.Select(t => string.Format("{0} {1}", t.Item1.ToString(CultureInfo.InvariantCulture), t.Item2.ToString(CultureInfo.InvariantCulture))))}))', {srid}), ST_Transform(gen_position,{srid}))";
            //else
            //    return $"{GetPolygonOperator(operation)}(ST_GeometryFromText('POLYGON(({String.Join(",", polygon.Select(t => string.Format("{0} {1}", t.Item1.ToString(CultureInfo.InvariantCulture), t.Item2.ToString(CultureInfo.InvariantCulture))))}))', 4326), gen_position)";
        }

        public static string GetGeoWhereInPolygon_GeneratedColumns(
            string wkt,
            string srid = "4326",
            string? operation = "intersects",
            bool reduceprecision = false,
            string geometryColumn = "gen_position"
        )
        {
            if (reduceprecision)
            {
                if (srid != "4326")
                    return $"{GetPolygonOperator(operation)}(ST_ReducePrecision(ST_GeometryFromText('{wkt}', {srid}),0.00000001), ST_ReducePrecision(ST_Transform({geometryColumn},{srid}),0.00000001))";
                else
                    return $"{GetPolygonOperator(operation)}(ST_ReducePrecision(ST_GeometryFromText('{wkt}', 4326),0.00000001), ST_ReducePrecision({geometryColumn},0.00000001))";
            }
            else
            {
                if (srid != "4326")
                    return $"{GetPolygonOperator(operation)}(ST_GeometryFromText('{wkt}', {srid}), ST_Transform({geometryColumn},{srid}))";
                else
                    return $"{GetPolygonOperator(operation)}(ST_GeometryFromText('{wkt}', 4326), {geometryColumn})";
            }
        }

        /// <summary>
        /// Adding ST_ReducePrecision if Points - Linestrings are given, 
        /// </summary>
        /// <param name="wkt"></param>
        /// <param name="srid"></param>
        /// <param name="operation"></param>
        /// <returns></returns>
        public static string GetGeoWhereInPolygon_GeneratedColumns(
            string wkt,
            string srid = "4326",
            string? operation = "intersects",
            string geometryColumn = "gen_position"
        )
        {
            if (srid != "4326")
                return $"{GetPolygonOperator(operation)}(ST_GeometryFromText('{wkt}', {srid}), ST_Transform({geometryColumn},{srid}))";
            else
                return $"{GetPolygonOperator(operation)}(ST_GeometryFromText('{wkt}', 4326), {geometryColumn})";
        }

        public static string GetPolygonOperator(string? operation) =>
            operation switch
            {
                //"contains" => "ST_Contains",
                "contains" => "ST_Covers",
                "intersects" => "ST_Intersects",
                _ => "ST_Contains",
            };

        public static string GetGeoOrderBySimple_GeneratedColumns(
            double latitude,
            double longitude,
            string geometryColumn = "gen_position"
        )
        {
            //Distance in meters, same unit earth_distance() returned
            return $"ST_Distance({geometryColumn}::geography, {GetGeoPoint_GeneratedColumns(latitude, longitude)}::geography)";
        }

        public static string GetGeoWhereExtended_GeneratedColumns(
            double latitude,
            double longitude,
            int radius,
            string geometryColumn = "gen_position"
        )
        {
            //Kept as a separate overload for source compatibility - "Simple" and "Extended" used to read
            //Latitude/Longitude from different jsonb paths, both are now backed by the same gen_position column
            return GetGeoWhereSimple_GeneratedColumns(latitude, longitude, radius, geometryColumn);
        }

        public static string GetGeoOrderByExtended_GeneratedColumns(
            double latitude,
            double longitude,
            string geometryColumn = "gen_position"
        )
        {
            return GetGeoOrderBySimple_GeneratedColumns(latitude, longitude, geometryColumn);
        }

        public static string GetGeoOrderByExtended_GeneratedColumns(
            string latitude,
            string longitude,
            string geometryColumn = "gen_position"
        )
        {
            return $"ST_Distance({geometryColumn}::geography, {GetGeoPoint_GeneratedColumns(latitude, longitude)}::geography)";
        }

        public static string GetGeoWhereBoundingBoxes_GeneratedColumns(
            string latitude,
            string longitude,
            string radius,
            string geometryColumn = "gen_position"
        )
        {
            //ST_DWithin already narrows down candidates via the geometry column's GiST index before computing
            //the exact geography distance, so the separate earth_box() bounding-box pre-filter is no longer needed
            return $"ST_DWithin({geometryColumn}::geography, {GetGeoPoint_GeneratedColumns(latitude, longitude)}::geography, {radius})";
        }

        public static string GetGeoWhereBoundingBoxes_GeneratedColumns(
            double latitude,
            double longitude,
            int radius,
            string geometryColumn = "gen_position"
        )
        {
            return GetGeoWhereSimple_GeneratedColumns(latitude, longitude, radius, geometryColumn);
        }

        public static string GetGeoWhereBoundingBoxesExtended_GeneratedColumns(
            string latitude,
            string longitude,
            string radius,
            string geometryColumn = "gen_position"
        )
        {
            return GetGeoWhereBoundingBoxes_GeneratedColumns(latitude, longitude, radius, geometryColumn);
        }

        public static string GetGeoWhereBoundingBoxesExtended_GeneratedColumns(
            double latitude,
            double longitude,
            int radius,
            string geometryColumn = "gen_position"
        )
        {
            return GetGeoWhereSimple_GeneratedColumns(latitude, longitude, radius, geometryColumn);
        }

        //For Accommodations
        public static void ApplyGeoSearchWhereOrderbySimple_GeneratedColumns(
            ref string where,
            ref string orderby,
            PGGeoSearchResult geosearchresult
        )
        {
            if (geosearchresult != null)
            {
                if (geosearchresult.geosearch)
                {
                    if (!String.IsNullOrEmpty(where))
                        where += " AND ";

                    where += PostgresSQLHelper.GetGeoWhereSimple_GeneratedColumns(
                        geosearchresult.latitude,
                        geosearchresult.longitude,
                        geosearchresult.radius
                    );
                    orderby = PostgresSQLHelper.GetGeoOrderBySimple_GeneratedColumns(
                        geosearchresult.latitude,
                        geosearchresult.longitude
                    );
                }
            }
        }

        public static Query GeoSearchFilterAndOrderby_GeneratedColumns(
            this Query query,
            PGGeoSearchResult? geosearchresult
        )
        {
            if (geosearchresult == null || !geosearchresult.geosearch)
                return query;

            return query
                .WhereRaw(
                    GetGeoWhereExtended_GeneratedColumns(
                        geosearchresult.latitude,
                        geosearchresult.longitude,
                        geosearchresult.radius
                    )
                )
                .OrderByRaw(
                    GetGeoOrderByExtended_GeneratedColumns(
                        geosearchresult.latitude,
                        geosearchresult.longitude
                    )
                );
        }

        //For Activities Pois and GBActivityPoi
        public static void ApplyGeoSearchWhereOrderby_GeneratedColumns(
            ref string where,
            ref string orderby,
            PGGeoSearchResult geosearchresult
        )
        {
            if (geosearchresult != null)
            {
                if (geosearchresult.geosearch)
                {
                    if (!String.IsNullOrEmpty(where))
                        where += " AND ";

                    where += PostgresSQLHelper.GetGeoWhereExtended_GeneratedColumns(
                        geosearchresult.latitude,
                        geosearchresult.longitude,
                        geosearchresult.radius
                    );
                    orderby = PostgresSQLHelper.GetGeoOrderByExtended_GeneratedColumns(
                        geosearchresult.latitude,
                        geosearchresult.longitude
                    );
                }
            }
        }

        #endregion

        public static uint PGPagingHelper(uint totalcount, uint pagesize)
        {
            uint totalpages;
            if (totalcount % pagesize == 0)
                totalpages = totalcount / pagesize;
            else
                totalpages = (totalcount / pagesize) + 1;

            return totalpages;
        }
    }

    public class PGParameters
    {
        public string? Name { get; set; }
        public NpgsqlTypes.NpgsqlDbType Type { get; set; }
        public string? Value { get; set; }
    }
}
