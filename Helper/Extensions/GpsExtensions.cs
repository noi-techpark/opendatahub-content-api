// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataModel;

namespace Helper
{
    public static class GpsExtensions
    {
        public static Dictionary<string, GpsInfo> ConvertGpsInfoToGpsPoints(
            this List<GpsInfo> gpsinfolist
        )
        {
            Dictionary<string, GpsInfo> gpspoints = new Dictionary<string, GpsInfo>();
            foreach (var gpsinfo in gpsinfolist)
            {
                if (gpsinfo.Gpstype != null)
                    gpspoints.Add(gpsinfo.Gpstype, gpsinfo);
            }

            //TODO LINQ SELECT

            return gpspoints;
        }

        public static Dictionary<string, GpsInfo> ConvertGpsInfoToGpsPointsLinq(
            this List<GpsInfo> gpsinfolist
        )
        {
            if (gpsinfolist != null && gpsinfolist.Count > 0)
                return gpsinfolist
                    .Distinct()
                    .Where(x => x.Gpstype != null)
                    .ToDictionary(x => x.Gpstype!, x => x);
            else
                return new Dictionary<string, GpsInfo>();
        }
    }

    public static class GpsInfoExtensions
    {
        public static void ExtendGpsInfoToDistanceCalculation<T>(
            this T mydata,
            string type,
            double latitude,
            double longitude
        )
            where T : IDistanceInfoAware
        {
            if (mydata != null && (mydata as IGpsInfo) != null && (mydata as IGpsInfo).Longitude > 0 && (mydata as IGpsInfo).Latitude > 0)
            {
                var distanceresult = DistanceCalculator.Distance(
                    (mydata as IGpsInfo).Latitude,
                    (mydata as IGpsInfo).Longitude,
                    latitude,
                    longitude,
                    'K'
                );

                if (mydata.DistanceInfo == null)
                    mydata.DistanceInfo = new DistanceInfo();

                //Calculate Distance
                if (type == "district")
                {
                    mydata.DistanceInfo.DistanceToDistrict = Math.Round(distanceresult * 1000, 0);
                }
                else if (type == "municipality")
                {
                    mydata.DistanceInfo.DistanceToMunicipality = Math.Round(
                        distanceresult * 1000,
                        0
                    );
                }
            }
        }

        public static void ExtendGpsInfoToDistanceCalculationList<T>(
            this T mydata,
            string type,
            double latitude,
            double longitude
        )
            where T : IDistanceInfoAware
        {
            if (mydata != null && (mydata as IGPSInfoAware) != null && (mydata as IGPSInfoAware).GpsInfo != null)
            {
                var mygpsdata = (mydata as IGPSInfoAware).GpsInfo.Where(x => x.Gpstype == "position").FirstOrDefault();

                if (mygpsdata != null && mygpsdata.Longitude > 0 && mygpsdata.Latitude > 0)
                {
                    var distanceresult = DistanceCalculator.Distance(
                        mygpsdata.Latitude,
                        mygpsdata.Longitude,
                        latitude,
                        longitude,
                        'K'
                    );

                    if (mydata.DistanceInfo == null)
                        mydata.DistanceInfo = new DistanceInfo();

                    //Calculate Distance
                    if (type == "district")
                    {
                        mydata.DistanceInfo.DistanceToDistrict = Math.Round(
                            distanceresult * 1000,
                            0
                        );
                    }
                    else if (type == "municipality")
                    {
                        mydata.DistanceInfo.DistanceToMunicipality = Math.Round(
                            distanceresult * 1000,
                            0
                        );
                    }
                }
            }
        }
    }
}
