using DataModel;
using Helper;
using SqlKata;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper
{
    public class GeoShapeInsertHelper
    {
        public static async Task<PGCRUDResult> InsertDataInShapesDB(QueryFactory queryFactory,
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
                var shapeid = await queryFactory.Query("geoshapes").Select("id").Where("id", data.Id.ToLower()).FirstOrDefaultAsync<string>();

                int insert = 0;
                int update = 0;

                PGCRUDResult result = default(PGCRUDResult);
                if (String.IsNullOrEmpty(shapeid))
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
                       geometry = new UnsafeLiteral("ST_GeometryFromText('" + data.Geometry.ToString() + "', \" + srid + \")", false),                       
                   });
                }

                return new PGCRUDResult()
                {
                    id = data.Id,
                    odhtype = data._Meta.Type,
                    created = insert,
                    updated = update,
                    deleted = 0,
                    error = 0,
                    errorreason = null,
                    operation = "insert shape",
                    changes = null,
                    compareobject = false,
                    objectchanged = 0,
                    objectimagechanged = 0,
                    pushchannels = null,
                };
            }
            catch (Exception ex)
            {
                return new PGCRUDResult()
                {
                    id = "",
                    odhtype = data._Meta.Type,
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    error = 1,
                    errorreason = ex.Message,
                    operation = "insert shape",
                    changes = null,
                    compareobject = false,
                    objectchanged = 0,
                    objectimagechanged = 0,
                    pushchannels = null,
                };
            }
        }


    }
}
