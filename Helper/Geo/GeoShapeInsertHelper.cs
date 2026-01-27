// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using MongoDB.Driver;
using SqlKata;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper
{
    public class GeoShapeInsertHelper
    {
        public static async Task<UpdateDetail> InsertDataInShapesDB(QueryFactory queryFactory,
              GeoShapeJson data,
              string source,
              string srid
        )
        {
            try
            {
                //Set LicenseInfo
                data.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<GeoShapeJson>(
                    data,
                    Helper.LicenseHelper.GetLicenseforGeoShape
                );

                //Set Meta
                data._Meta = MetadataHelper.GetMetadataobject<GeoShapeJson>(data);

                //Check if data is there by Name
                var shapeid = await queryFactory.Query("geoshapes").Select("id").Where("id", data.Id.ToLower()).GetAsync<string>();

                int insert = 0;
                int update = 0;

                UpdateDetail result = default(UpdateDetail);
                if (shapeid == null || shapeid.ToList().Count == 0 )
                {
                    insert = await queryFactory
                   .Query("geoshapes")
                   .InsertAsync(new GeoShapeDB<UnsafeLiteral>()
                   {
                       id = data.Id.ToLower(),
                       licenseinfo = new JsonRaw(data.LicenseInfo),
                       meta = new JsonRaw(data._Meta),
                       mapping = new JsonRaw(data.Mapping),
                       name = data.Name,
                       country = data.Country,
                       type = data.Type,
                       source = source,
                       srid = srid,
                       geometry = new UnsafeLiteral("ST_GeometryFromText('" + data.Geometry.ToString() + "', " + srid + ")", false),                       
                   });
                }
                else
                {
                    update = await queryFactory
                   .Query("geoshapes")
                   .Where("id", data.Id.ToLower())
                   .UpdateAsync(new GeoShapeDB<UnsafeLiteral>()
                   {
                       id = data.Id.ToLower(),
                       licenseinfo = new JsonRaw(data.LicenseInfo),
                       meta = new JsonRaw(data._Meta),
                       mapping = new JsonRaw(data.Mapping),
                       name = data.Name,
                       country = data.Country,
                       type = data.Type,
                       source = source,
                       srid = srid,                          
                       geometry = new UnsafeLiteral("ST_GeometryFromText('" + data.Geometry.ToString() + "', " + srid + ")", false),
                   });
                }

                return new UpdateDetail()
                {
                    id = data.Id,
                    type = data._Meta.Type,
                    created = insert,
                    updated = update,
                    deleted = 0,
                    error = 0,
                    exception = null,
                    operation = "insert shape",
                    changes = null,
                    objectcompared = 0,
                    objectchanged = 0,
                    objectimagechanged = 0,
                    pushchannels = null,
                };
            }
            catch (Exception ex)
            {
                return new UpdateDetail()
                {
                    id = "",
                    type = data._Meta.Type,
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    error = 1,
                    exception = ex.Message,
                    operation = "insert shape",
                    changes = null,
                    objectcompared = 0,
                    objectchanged = 0,
                    objectimagechanged = 0,
                    pushchannels = null,
                };
            }
        }

        public static async Task<int> DeleteFromShapesDB(QueryFactory queryFactory,
            string id)
        {
            return await queryFactory.Query("geoshapes").Where("id", id).DeleteAsync();
        }
    }
}
