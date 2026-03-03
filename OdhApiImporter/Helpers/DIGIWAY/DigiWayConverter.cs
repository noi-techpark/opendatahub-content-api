using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Triangulate;
using SqlKata.Execution;
using System.Threading.Tasks;
using System.Linq;

namespace OdhApiImporter.Helpers.DIGIWAY
{
    public class DigiWayConverter
    {
        public static async Task<Geometry> ConvertGeometryWithPostGIS(QueryFactory queryfactory, Geometry geometry, string from_srid, string to_srid)
        {
            var wktquery = queryfactory.Query()
                .SelectRaw($"ST_AsText(ST_Transform(ST_GeomFromText('{geometry.AsText()}', {from_srid}), {to_srid}), 8) as geom");
           
            var wkttransformed = await wktquery.GetAsync<string>();

            var reader = new WKTReader();
            return reader.Read(wkttransformed.FirstOrDefault());            
        }
    }
}
