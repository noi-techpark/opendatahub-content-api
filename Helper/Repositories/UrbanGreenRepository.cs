// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using DataModel;

namespace Helper
{
    public class UpsertableUrbanGreen: Helper.QueryFactoryExtension.Upsertable<UrbanGreen>
    {
        public UpsertableUrbanGreen(UrbanGreen data)
        : base(
            data,
            new Dictionary<string, object>
            {
                {
                    "geo",
                    new SqlKata.UnsafeLiteral(
                        $"ST_SetSRID(ST_GeomFromText('{data.Geo.GetDefaultGeoInfo()!.Geometry}'), 4326)",
                        false
                    )
                }
            }
        ) {}
    }
}
