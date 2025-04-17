// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.IO.Compression;

namespace GTFSAPI
{
    public class GetTimeTablesData
    {
        private static async Task<HttpResponseMessage> GetDataFromService(
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

        public static async Task<List<StaTimeTableStopsCsv>?> GetTimeTablesDataAsync(
            string user,
            string pass,
            string serviceurl
        )
        {
            //Request
            HttpResponseMessage response = await GetDataFromService(user, pass, serviceurl);
            //Unzip File

            List<StaTimeTableStopsCsv?> result = default(List<StaTimeTableStopsCsv?>);

            using (var zipstream = await response.Content.ReadAsStreamAsync())
            {
                using (ZipArchive archive = new ZipArchive(zipstream))
                {
                    ZipArchiveEntry entry = archive.Entries.Where(x => x.FullName == "stops.txt").FirstOrDefault();

                    if (entry != null)
                    {
                        var stopsstream = entry.Open();

                        result = ParseStopsJson.GetParsetStaTimeTableStops(stopsstream);
                    }
                    
                }
            }
            
            return result;
        }
    }
}
