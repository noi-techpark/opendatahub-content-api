// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

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

        public static async Task<GeoserverCivisResult?> GetDigiWayRouteDataAsync(
            string user,
            string pass,
            string serviceurl
        )
        {
            //Request
            HttpResponseMessage response = await GetDigiwayDataFromService(user, pass, serviceurl);
            //Parse JSON Response to
            var responsetask = await response.Content.ReadAsStringAsync();
            GeoserverCivisResult? responseobject = JsonConvert.DeserializeObject<GeoserverCivisResult>(responsetask);

            return responseobject;
        }
    }
}
