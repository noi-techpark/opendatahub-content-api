// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using DIGIWAY.Model;
using DIGIWAY.Model.GeoJsonReadModel;
using MongoDB.Driver.GeoJsonObjectModel;
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

        public static async Task<IGeoserverCivisResult> GetCivisDataAsync(
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

        #region GeoJsonFromUrl        

        public static async Task<ICollection<GeoJsonFeature>?> GetDigiWayGeoJsonDataFromUrlAsync(
            string user,
            string pass,
            string serviceurl
        )
        {
            //Request
            HttpResponseMessage response = await GetDigiwayDataFromService(user, pass, serviceurl);

            var responsetask = await response.Content.ReadAsStringAsync();

            var reader = new GeoJsonFileReader();

            var featureCollection = reader.ReadGeoJsonString(responsetask);

            // Convert to simple features for easier access
            return reader.GetFeatures(featureCollection);

            //return JsonConvert.DeserializeObject<GeoJsonFeatureCollection>(responsetask);                       
        }

        #endregion

        #region GeoJsonFromSHPResponse        

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
                //var fileInfo = reader.GetFileInfo(serviceurl);

                // Read the actual features
                var featureCollection = await reader.ReadGeoJsonFileAsync(serviceurl);
                
                // Convert to simple features for easier access
                var features = reader.GetFeatures(featureCollection);

                return features;
            }

            // Read GeoJSON file asynchronously
            //var featureCollection = await converter.ReadGeoJsonFileAsync("path/to/file.geojson");
            //return null;
        }

        #endregion

        #region GeoJsonFromFile

        public static async Task<ICollection<GeoJsonFeature>?> GetDigiWayGeoJsonDataFromFileAsync(
            string user,
            string pass,
            string serviceurl,
            bool createfromurl = true
        )
        {            
            var reader = new GeoJsonFileReader();

            //var fileInfo = reader.GetFileInfo(serviceurl);

            // Read the actual features
            //var featureCollection = await reader.ReadGeoJsonFileAsync(serviceurl);
            var featureCollection = await reader.ReadGeoJsonFileAsStreamAsync(serviceurl);

            // Convert to simple features for easier access
            var features = reader.GetFeatures(featureCollection);

            return features;           
        }

        #endregion
    }
}
