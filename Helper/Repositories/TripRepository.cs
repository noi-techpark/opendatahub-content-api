// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using DataModel;

namespace Helper
{
    public class UpsertableTrip : QueryFactoryExtension.Upsertable<Trip>
    {
        public UpsertableTrip(Trip data)
        : base(
            data,
            BuildAdditionalColumns(data)
        )
        { }

        private static Dictionary<string, object> BuildAdditionalColumns(Trip data)
        {
            var columns = new Dictionary<string, object>();

            if (data.StopTimes != null && data.StopTimes.Any())
            {
                // Collect all stops' default WKT geometries (can be POINT, POLYGON, etc.)
                var geometries = data.StopTimes
                    .Where(st => st.Geo != null)
                    .Select(st => st.Geo.GetDefaultGeoInfo())
                    .Where(g => g != null && !string.IsNullOrEmpty(g.Geometry))
                    .Select(g => $"ST_SetSRID(ST_GeomFromText('{g.Geometry}'), 4326)")
                    .ToList();

                if (geometries.Any())
                {
                    // ST_Collect aggregates any geometry types into a GeometryCollection
                    var sql = geometries.Count == 1
                        ? geometries[0]
                        : $"ST_Collect(ARRAY[{string.Join(",", geometries)}])";

                    columns["geo"] = new SqlKata.UnsafeLiteral(sql, false);
                }
            }

            return columns;
        }
    }
}
