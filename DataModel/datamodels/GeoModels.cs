// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace DataModel
{
    public class GeoShapeJsonBData
    {
        public string? id { get; set; }
        public JsonRaw? data { get; set; }
    }
    
    public class GeoShapeJson
    {
        public int Id { get; set; }
        public string Country { get; set; }
        public int? Code_Rip { get; set; }
        public int? Code_Reg { get; set; }
        public int? Code_Prov { get; set; }
        public int? Code_Cm { get; set; }
        public int? Code_Uts { get; set; }

        public string Istatnumber { get; set; }

        public string Abbrev { get; set; }

        public string Type_Uts { get; set; }

        public string Name { get; set; }

        public string Name_Alternative { get; set; }

        public float? Shape_length { get; set; }

        public float? Shape_area { get; set; }

        public string Source { get; set; }

        public string Type { get; set; }

        public Metadata _Meta { get; set; }

        public LicenseInfo LicenseInfo { get; set; }

        public IDictionary<string, IDictionary<string, string>> Mapping { get; set; }

        public Geometry Geometry { get; set; }
    }

    public class GeoShapeDB
    {
        public int id { get; set; }
        public string country { get; set; }
        public int? code_rip { get; set; }
        public int? code_reg { get; set; }
        public int? code_prov { get; set; }
        public int? code_cm { get; set; }
        public int? code_uts { get; set; }

        public string istatnumber { get; set; }

        public string abbrev { get; set; }

        public string type_uts { get; set; }

        public string name { get; set; }

        public string name_alternative { get; set; }

        public float? shape_leng { get; set; }

        public float? shape_area { get; set; }

        public string source { get; set; }

        public string type { get; set; }

        public JsonRaw meta { get; set; }

        public JsonRaw licenseinfo { get; set; }

        public JsonRaw mapping { get; set; }

        public string srid { get; set; }

        public string idstring { get; set; }
    }

    public class GeoShapeDB<T>
    {
        public int id { get; set; }
        public string country { get; set; }
        public int? code_rip { get; set; }
        public int? code_reg { get; set; }
        public int? code_prov { get; set; }
        public int? code_cm { get; set; }
        public int? code_uts { get; set; }

        public string istatnumber { get; set; }

        public string abbrev { get; set; }

        public string type_uts { get; set; }

        public string name { get; set; }

        public string name_alternative { get; set; }

        public float? shape_leng { get; set; }

        public float? shape_area { get; set; }

        public string source { get; set; }

        public string type { get; set; }

        public JsonRaw meta { get; set; }

        public JsonRaw licenseinfo { get; set; }

        public JsonRaw mapping { get; set; }

        public T geometry { get; set; }

        public T geom { get; set; }

        public string srid { get; set; }

        public string idstring { get; set; }
    }

    public class GeoShapeDBTest
    {
        public string id { get; set; }
        public string? name { get; set; }

        public string? country { get; set; }

        //Table
        public string type { get; set; }

        public JsonRaw? licenseinfo { get; set; }

        public JsonRaw? meta { get; set; }

        public JsonRaw? mapping { get; set; }

        public string source { get; set; }

        public string srid { get; set; }
    }

    public class GeoShapeDBTest<T>
    {
        public string id { get; set; }
        public string? name { get; set; }

        public string? country { get; set; }

        //public float shape_leng { get; set; }
        //public float shape_area { get; set; }


        public T geometry { get; set; }

        //Table
        public string type { get; set; }

        public JsonRaw? licenseinfo { get; set; }

        public JsonRaw? meta { get; set; }

        public JsonRaw? mapping { get; set; }

        //public T geojson { get; set; }

        public string source { get; set; }

        public string srid { get; set; }
    }

    public class GeoShapeJsonTest
    {
        public GeoShapeJsonTest()
        {
            Mapping = new Dictionary<string, IDictionary<string, string>>();            
        }

        public string Id { get; set; }
        public string Country { get; set; }        

        public string Name { get; set; }
        
        public string Source { get; set; }

        public string Type { get; set; }

        public Metadata _Meta { get; set; }

        public LicenseInfo LicenseInfo { get; set; }

        public string SRid { get; set; }

        public Geometry Geometry { get; set; }

        public IDictionary<string, IDictionary<string, string>> Mapping { get; set; }
    }

    //not needed
    //public class PGGeometryRaw : ICustomQueryParameter
    //{        
    //    public PGGeometryRaw(Geometry data)
    //    {
    //        Value = data;
    //    }        

    //    public Geometry Value { get; }

    //    public void AddParameter(IDbCommand command, string name)
    //    {
    //        var parameter = new NpgsqlParameter(name, NpgsqlDbType.Geometry);
    //        parameter.Value = Value;
    //        command.Parameters.Add(parameter);
    //    }

    //    public override string? ToString()
    //    {
    //        throw new InvalidOperationException(
    //            "ToString on PGGeometryRaw shouldn't be called, there is somewhere an implicit ToString() happening (maybe from a manual JSON serialization)."
    //        );
    //    }

    //    public static explicit operator PGGeometryRaw(Geometry x) => new PGGeometryRaw(x);
    //}


      
    //public class PGLineStringRaw : ICustomQueryParameter
    //{
    //    public PGLineStringRaw(LineString data)
    //    {
    //        Value = data;
    //    }

    //    public LineString Value { get; }

    //    public void AddParameter(IDbCommand command, string name)
    //    {
    //        var parameter = new NpgsqlParameter(name, NpgsqlDbType.Geometry);
    //        parameter.Value = Value;
    //        command.Parameters.Add(parameter);
    //    }

    //    public static explicit operator PGLineStringRaw(LineString x) => new PGLineStringRaw(x);
    //}
}
