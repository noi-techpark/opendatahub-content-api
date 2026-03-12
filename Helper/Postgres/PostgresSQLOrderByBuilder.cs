// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Runtime.CompilerServices;
using SqlKata;

namespace Helper
{
    public static class PostgresSQLOrderByBuilder
    {
        /// <summary>
        /// Build Orderby with Seed, if a seed "null" is passed an orderby query with sortfield and direction will be created
        /// </summary>
        /// <param name="orderby"></param>
        /// <param name="seed"></param>
        /// <param name="sortfield"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static void BuildSeedOrderBy(
            ref string orderby,
            ref string? seed,
            string sortifseednull
        )
        {
            //string? myseed = seed;

            if (seed != null)
            {
                seed = Helper.CreateSeed.GetSeed(seed);
                orderby = $"md5(id || '{seed}')";
            }
            else
            {
                orderby = sortifseednull;
            }
        }

        public static Query OrderBySeed(this Query query, ref string? seed, string sortifseednull)
        {
            string orderby = "";
            BuildSeedOrderBy(ref orderby, ref seed, sortifseednull);
            return query.OrderByRaw(orderby);
        }

        public static Query OrderByRawIfNotNull(this Query query, string? rawsort)
        {
            if (!string.IsNullOrEmpty(rawsort))
            {
                var splitted = rawsort.Split(",");

                var returnvalue = "";

                foreach (var split in splitted)
                {
                    string direction = "ASC";
                    if (split.StartsWith("-"))
                        direction = "DESC";

                    returnvalue =
                        returnvalue
                        + "\""
                        + split.Replace("-", "").Replace("[", "\\[").Replace("]", "\\]")
                        + "\" "
                        + direction
                        + ",";
                }

                returnvalue = returnvalue.Substring(0, returnvalue.Length - 1);

                return query.OrderByRaw(returnvalue);
            }
            else
                return query;
        }

        public static Query OrderOnlyByRawSortIfNotNull(this Query query, string? rawsort)
        {
            if (!string.IsNullOrEmpty(rawsort))
                return query.OrderByRaw(RawQueryParser.Transformer.TransformSort(rawsort));
            else
                return query;
        }

        public static Query ApplyOrdering_GeneratedColumns(
            this Query query,
            ref string? seed,
            PGGeoSearchResult geosearchresult,
            string? rawsort,
            string? overwritestandardorder = null
        ) =>
            (geosearchresult, rawsort) switch
            {
                (PGGeoSearchResult geosr, _) when geosr.geosearch =>
                    query.GeoSearchFilterAndOrderby_GeneratedColumns(geosr),
                (_, string raw) => query.OrderByRaw(RawQueryParser.Transformer.TransformSort(raw)),
                _ => query.OrderBySeed(
                    ref seed,
                    overwritestandardorder != null ? overwritestandardorder : "gen_shortname ASC"
                ),
            };

        public static Query ApplyOrdering_GeneratedColumns(
            this Query query,
            PGGeoSearchResult geosearchresult,
            string? rawsort,
            string? overwritestandardorder = null
        ) =>
            (geosearchresult, rawsort) switch
            {
                (PGGeoSearchResult geosr, _) when geosr.geosearch =>
                    query.GeoSearchFilterAndOrderby_GeneratedColumns(geosr),
                (_, string raw) => query.OrderByRaw(RawQueryParser.Transformer.TransformSort(raw)),
                _ => query.OrderByRaw(
                    overwritestandardorder != null
                        ? overwritestandardorder
                        : "data#>>'\\{Shortname\\}' ASC"
                ),
            };

        public static Query ApplyRawFilter(this Query query, string? rawFilter)
        {
            static string jsonSerializer(object value) =>
                Newtonsoft.Json.JsonConvert.SerializeObject(value);
            return rawFilter != null
                ? query.WhereRaw(
                    RawQueryParser.Transformer.TransformFilter(jsonSerializer, rawFilter)
                )
                : query;
        }
    }
}
