// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGIWAY
{
    public interface IGeoserverCivisResult
    {
        string type { get; set; }
        
        //optional
        int? totalFeatures { get; set; }
        int? numberMatched { get; set; }
        int? numberReturned { get; set; }
        DateTime? timeStamp { get; set; }
        GeoserverCrs? crs { get; set; }
        float[]? bbox { get; set; }
    }

    public class GeoserverCivisResult : IGeoserverCivisResult
    {
        public string type { get; set; }
        
        //optional
        public int? totalFeatures { get; set; }
        public int? numberMatched { get; set; }
        public int? numberReturned { get; set; }
        public DateTime? timeStamp { get; set; }
        public GeoserverCrs? crs { get; set; }
        public float[]? bbox { get; set; }
    }

    public class GeoserverCivisResultCycleWay : GeoserverCivisResult
    {     
        public ICollection<GeoserverCivisDataCycleWay>? features { get; set; }
    }

    public class GeoserverCivisResultHikingTrail : GeoserverCivisResult
    {
        public ICollection<GeoserverCivisDataHikingTrail>? features { get; set; }
    }

    public class GeoserverCivisResultMountainbike : GeoserverCivisResult
    {
        public ICollection<GeoserverCivisDataMountainBike>? features { get; set; }
    }

    public class GeoserverCivisResultIntermunicipalPaths : GeoserverCivisResult
    {
        public ICollection<GeoserverCivisDataIntermunicipalPaths>? features { get; set; }
    }


    public class GeoserverCrs
    {
        public string type { get; set; }
        public GeoserverCrsProperties properties { get; set; }
    }

    public class GeoserverCrsProperties
    {
        public string name { get; set; }
    }
    
    public class GeoserverCivisGeometry
    {
        public string type { get; set; }
        public float[][][] coordinates { get; set; }
    }

    public interface IGeoServerCivisData
    {
        string type { get; set; }
        string id { get; set; }
        GeoserverCivisGeometry geometry { get; set; }
        string geometry_name { get; set; }        
        float[] bbox { get; set; }
    }

    public class GeoserverCivisData : IGeoServerCivisData
    {
        public string type { get; set; }
        public string id { get; set; }
        public GeoserverCivisGeometry geometry { get; set; }
        public string geometry_name { get; set; }        
        public float[] bbox { get; set; }
    }

    public class GeoserverCivisDataCycleWay : GeoserverCivisData
    {
        public GeoserverCivisPropertiesCycleWay properties { get; set; }
    }

    public class GeoserverCivisDataMountainBike : GeoserverCivisData
    {
        public GeoserverCivisPropertiesMountainBike properties { get; set; }
    }

    public class GeoserverCivisDataHikingTrail : GeoserverCivisData
    {
        public GeoserverCivisPropertiesHikingTrail properties { get; set; }
    }

    public class GeoserverCivisDataIntermunicipalPaths : GeoserverCivisData
    {        
        public GeoserverCivisPropertiesCyclewaysIntermunicipalPaths properties { get; set; }        
    }

    public class GeoserverCivisPropertiesCycleWay
    {
        public int ID { get; set; }
        public string OBJECT { get; set; }
        public string ROUTE_NUMBER { get; set; }
        public string ROUTE_NAME { get; set; }
        public string ROUTE_TYPE { get; set; }
        public string ROUTE_DESC { get; set; }
        public string ROUTE_START { get; set; }
        public string ROUTE_END { get; set; }
        public string MUNICIPALITY { get; set; }
        public string REGION { get; set; }
        public string RUNNING_TIME { get; set; }
        public string DIFFICULTY { get; set; }
        public string STATUS { get; set; }
        public string STATUS_DATE { get; set; }
        public int? DOWNHILL_METERS { get; set; }
        public int? UPHILL_METERS { get; set; }
        public int? START_HEIGHT { get; set; }
        public int? END_HEIGHT { get; set; }
        public float? LENGTH { get; set; }
        public string CREATE_DATE { get; set; }
        public string UPDATE_DATE { get; set; }
    }
  
    public class GeoserverCivisPropertiesMountainBike
    {
        public int MTB_ID { get; set; }
        public string MTB_CODE { get; set; }
        public string MTB_NAME_IT { get; set; }
        public string MTB_NAME_DE { get; set; }
        public string MTB_SINGLE_DE { get; set; }
        public string MTB_SINGLE_IT { get; set; }
        public string MTB_DIFF { get; set; }
        public string MTB_DIFF_DE { get; set; }
        public string MTB_DIFF_IT { get; set; }
        public string MTB_LTS_RID { get; set; }
        public string MTB_TEXT_DE { get; set; }
        public string MTB_TEXT_IT { get; set; }
        public string MTB_LINK_DE { get; set; }
        public string MTB_LINK_IT { get; set; }
        public int LENGTH_GEOM { get; set; }
    }

    public class GeoserverCivisPropertiesHikingTrail
    {
        public int ID { get; set; }
        public string CODE { get; set; }
        public string NAME { get; set; }
        public string CODE_NAME { get; set; }
        public int LENGTH_GEOM { get; set; }
    }

    public class GeoserverCivisPropertiesCyclewaysIntermunicipalPaths
    {
        public int ID { get; set; }
        public string CODE { get; set; }
        public string NAME_IT { get; set; }
        public string NAME_DE { get; set; }
        public string DISTRICT_IT { get; set; }
        public string DISTRICT_DE { get; set; }
        public int LENGTH_GEOM { get; set; }
    }

}
