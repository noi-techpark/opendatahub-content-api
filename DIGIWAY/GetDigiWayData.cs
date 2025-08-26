// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using DIGIWAY.Model;
using Newtonsoft.Json;

namespace DIGIWAY
{
    public class GetDigiwayData
    {
        private static async Task<HttpResponseMessage> GetDigiwayDataFromService(
            string user,
            string pass,
            string serviceurl
        )
        {
            using (var client = new HttpClient())
            {
                var myresponse = await client.GetAsync(serviceurl);

                return myresponse;
            }
        }
        
        public static async Task<IGeoserverCivisResult> GetDigiWayDataAsync(
            string user,
            string pass,
            string serviceurl,
            string identifier
        )  
        {
            //Request
            HttpResponseMessage response = await GetDigiwayDataFromService(user, pass, serviceurl);
            //Parse JSON Response to
            var responsetask = await response.Content.ReadAsStringAsync();

            if (responsetask != null && !String.IsNullOrEmpty(identifier))
            {
                //IGeoserverCivisResult result = identifier switch
                //{
                //    "cyclewaystyrol" => JsonConvert.DeserializeObject<GeoserverCivisResultCycleWay>(responsetask),
                //    "mountainbikeroutes" => JsonConvert.DeserializeObject<GeoserverCivisResultMountainbike>(responsetask),
                //    "hikingtrails" => JsonConvert.DeserializeObject<GeoserverCivisResultHikingTrail>(responsetask),
                //    "intermunicipalcyclingroutes" => JsonConvert.DeserializeObject<GeoserverCivisResultIntermunicipalPaths>(responsetask),
                //    _ => null
                //};

                return JsonConvert.DeserializeObject<GeoserverCivisResult>(responsetask);
            }
            else
                return null;
            
        }

        public static async Task<WFSResult> GetDigiWayWfsDataFromXmlAsync(
            string user,
            string pass,
            string serviceurl,
            string identifier
        )
        {
            //Request
            HttpResponseMessage response = await GetDigiwayDataFromService(user, pass, serviceurl);
            //Parse JSON Response to
            var responsetask = await response.Content.ReadAsStringAsync();

            if (responsetask != null && !String.IsNullOrEmpty(identifier))
            {
                var parser = new WfsMountainBikeRouteParser();

                var mtblist = parser.ParseXml(responsetask);

                return new WFSResult() { Results = mtblist };

            }
            else
                return null;

        }
    }
}
