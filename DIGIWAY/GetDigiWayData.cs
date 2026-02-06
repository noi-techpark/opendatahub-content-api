// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using DIGIWAY.Model;
using DIGIWAY.Model.GeoJsonReadModel;
using Newtonsoft.Json;
using System;
using System.IO.Compression;

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

        #region CivisJsonResponse

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

        #endregion

        #region WFSResponse

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
                switch(identifier)
                {
                    case "radrouten_tirol":
                        var rparser = new WfsMountainBikeRouteParser();

                        var mtblist = rparser.ParseXml(responsetask);

                        return new WFSResult() { Results = mtblist };
                    case "hikintrail_e5":
                        var e5parser = new WfsE5TrailParser();

                        var e5list = e5parser.ParseXml(responsetask);

                        return new WFSResult() { Results = e5list };
                    default:
                        return null;
                }
            }
            else
                return null;

        }

        #endregion

        #region SHPResponse        

        public static async Task<ICollection<GeoJsonFeature>?> GetDigiWayGeoJsonDataFromSHPAsync(
            string user,
            string pass,
            string serviceurl,
            bool createfromurl = true
        )
        {
            if (createfromurl)
            {
                //Request
                HttpResponseMessage response = await GetDigiwayDataFromService(user, pass, serviceurl);                

                //Unzip File
                using (var zipstream = await response.Content.ReadAsStreamAsync())
                {
                    using (ZipArchive archive = new ZipArchive(zipstream))
                    {
                        //To check if this works
                        ZipArchiveEntry entry = archive.Entries.Where(x => x.FullName == "*.shp").FirstOrDefault();

                        if (entry != null)
                        {
                            var stopsstream = entry.Open();

                            //TODO Convert result to GeoJson
                            //result = ParseGtfsApi.GetParsetStaTimeTableStops(stopsstream);
                        }
                    }
                }

                return null;
            }
            else
            {                
                var reader = new GeoJsonFileReader();
                var fileInfo = reader.GetFileInfo(serviceurl);

                // Read the actual features
                var featureCollection = await reader.ReadGeoJsonFileAsync(serviceurl);
                
                // Convert to simple features for easier access
                var features = reader.GetFeatures(featureCollection);

                return features;
            }

            // Read GeoJSON file asynchronously
            //var featureCollection = await converter.ReadGeoJsonFileAsync("path/to/file.geojson");


            return null;
        }

        #endregion

        #region GeoJson

        public static async Task<ICollection<GeoJsonFeature>?> GetDigiWayGeoJsonDataFromFileAsync(
            string user,
            string pass,
            string serviceurl,
            bool createfromurl = true
        )
        {            
            var reader = new GeoJsonFileReader();
            var fileInfo = reader.GetFileInfo(serviceurl);

            // Read the actual features
            var featureCollection = await reader.ReadGeoJsonFileAsync(serviceurl);

            // Convert to simple features for easier access
            var features = reader.GetFeatures(featureCollection);

            return features;           
        }

        #endregion
    }
}
