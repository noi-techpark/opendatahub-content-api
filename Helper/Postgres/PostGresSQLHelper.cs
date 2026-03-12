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
        #region To Deprecate cube/earthdistance Distance Filter and OrderBy

        //TODO use POSTGIS instead of cube/earthdistance

        //Creates the Where if position is present as gen_latitude, gen_longitude
        public static string GetGeoWhere_GeneratedColumns(
            double latitude,
            double longitude,
            int radius
        )
        {
            return $"earth_distance(ll_to_earth({latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}),ll_to_earth((gen_latitude)::double precision, (gen_longitude)::double precision)) < {radius.ToString()}";
        }

        //Creates the OrderBy if position is present as gen_latitude, gen_longitude
        public static string GetGeoOrderBy_GeneratedColumns(
            double latitude, 
            double longitude
        )
        {
            return $"earth_distance(ll_to_earth({latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}),ll_to_earth((gen_latitude)::double precision, (gen_longitude)::double precision))";
        }

        //Apply the geosearch
        public static void ApplyGeoSearchWhereOrderby_GeneratedColumns(
            ref string where,
            ref string orderby,
            PGGeoSearchResult geosearchresult
        )
        {
            if (geosearchresult != null && geosearchresult.geosearch)
            {
                if (!String.IsNullOrEmpty(where))
                    where += " AND ";

                where += PostgresSQLHelper.GetGeoWhere_GeneratedColumns(
                    geosearchresult.latitude,
                    geosearchresult.longitude,
                    geosearchresult.radius
                );
                orderby = PostgresSQLHelper.GetGeoOrderBy_GeneratedColumns(
                    geosearchresult.latitude,
                    geosearchresult.longitude
                );
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
                    GetGeoWhere_GeneratedColumns(
                        geosearchresult.latitude,
                        geosearchresult.longitude,
                        geosearchresult.radius
                    )
                )
                .OrderByRaw(
                    GetGeoOrderBy_GeneratedColumns(
                        geosearchresult.latitude,
                        geosearchresult.longitude
                    )
                );
        }

        #endregion

        #region Polygon Filter

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
        
        public static string GetGeoWhereBoundingBoxes_GeneratedColumns(
            string latitude,
            string longitude,
            string radius
        )
        {
            return $"earth_box(ll_to_earth({latitude}, {longitude}), {radius}) @> ll_to_earth((gen_latitude)::double precision, (gen_longitude)::double precision) and earth_distance(ll_to_earth({latitude}, {longitude}), ll_to_earth((gen_latitude)::double precision, (gen_longitude)::double precision)) < {radius}";
        }

        public static string GetGeoWhereBoundingBoxes_GeneratedColumns(
            double latitude,
            double longitude,
            int radius
        )
        {
            return $"earth_box(ll_to_earth({latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}), {radius.ToString()}) @> ll_to_earth((gen_latitude)::double precision, (gen_longitude)::double precision) and earth_distance(ll_to_earth({latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}), ll_to_earth((gen_latitude)::double precision, (gen_longitude)::double precision)) < {radius.ToString()}";
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
