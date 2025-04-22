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
    public class GeoserverCivisResult
    {
        public string type { get; set; }

	    public ICollection<GeoserverCivisData> features { get; set; }

        //optional
        public int? totalFeatures { get; set; }
        public int? numberMatched { get; set; }
        public int? numberReturned { get; set; }
        public DateTime? timeStamp { get; set; }
        public GeoserverCrs? crs { get; set; }
        public float[]? bbox { get; set; }
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

    public class GeoserverCivisData
    {
        public string type { get; set; }
        public string id { get; set; }
        public GeoserverCivisGeometry geometry { get; set; }
        public string geometry_name { get; set; }
        public GeoserverCivisProperties properties { get; set; }
        public float[] bbox { get; set; }
    }

    public class GeoserverCivisGeometry
    {
        public string type { get; set; }
        public float[][][] coordinates { get; set; }
    }

    public class GeoserverCivisProperties
    {
        
    }

    public class GeoserverCivisPropertiesCycleWay : GeoserverCivisProperties
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
  
    public class GeoserverCivisPropertiesMountainBike : GeoserverCivisProperties
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

    public class GeoserverCivisPropertiesHikingTrail : GeoserverCivisProperties
    {
        public int ID { get; set; }
        public string CODE { get; set; }
        public string NAME { get; set; }
        public string CODE_NAME { get; set; }
        public int LENGTH_GEOM { get; set; }
    }

    public class GeoserverCivisPropertiesCyclewaysIntermunicipalPaths : GeoserverCivisProperties
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
