// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using LTSAPI;
using LTSAPI.Parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace OdhApiCore.Controllers.helper
{
    public class SnowReportHelper
    {
        public static async Task<LTSActivitySearchResult?> GetSummaries(ICollection<string> areas, ISettings settings)
        {
            var ltsapi = new LtsApi(
                settings.LtsCredentialsOpen.serviceurl,
                settings.LtsCredentialsOpen.username,
                settings.LtsCredentialsOpen.password,
                settings.LtsCredentialsOpen.ltsclientid,
                true);

            //Construct the post body
            LTSActivitySearchRequestBody body = new LTSActivitySearchRequestBody();
            body.areaRids = areas;
            body.onlyActive = true;
            body.paging = new LTSAvailabilitySearchRequestPaging() { pageNumber = 1, pageSize = 25 };
            LTSActivitySearchBodyFilterAndSummaryGroups filterandsummarygroups = new LTSActivitySearchBodyFilterAndSummaryGroups();

            filterandsummarygroups.id = 0;
            filterandsummarygroups.type = "tag";
            filterandsummarygroups.filters.Add(new LTSActivitySearchBodyFilters()
            {
                id = 0,
                isSelected = false,
                rids = [
                    "EB5D6F10C0CB4797A2A04818088CD6AB", // Slopes
                    "1D273A84DBCA4709B68D295C89A003E4", // Circuit
                    "7CA6D68BF134495F865FDD47B94320C0", // Snowpark
                    "6285F49DBBE04393BAD29E6EF219EB03" // Other slopes
                       ]
            });

            filterandsummarygroups.filters.Add(new LTSActivitySearchBodyFilters()
            {
                id = 1,
                isSelected = false,
                rids = [
                    "D544A6312F8A47CF80CC4DFF8833FE50", // Cross-country ski-track
                    "379E895958FD4693B04F5734A9CFAFAB", // Classic
                    "E2B3F9B5B2F747A1968ECD033BED5D2B", // Skating
                    "835EF4A6853F414DA9782607F01EEE48" // Classic and skating
                       ]
            });

            filterandsummarygroups.filters.Add(new LTSActivitySearchBodyFilters()
            {
                id = 2,
                isSelected = false,
                rids = [
                     "E23AA37B2AE3477F96D1C0782195AFDF", // Lifts
                     "9CBAC00246A8467E93DD66F3A1A9C594" // Other Lifts
                       ]
            });

            filterandsummarygroups.filters.Add(new LTSActivitySearchBodyFilters()
            {
                id = 3,
                isSelected = false,
                rids = [
                                "F3B08D06569646F38462EDCA506D81D4", // Sledging
                                "0BCFF98DC9DB40FDA30957791B0B2A12", //Eisbahnen
                                "B8F8949440E340E7846BD75CED2F1F18", // Weitere Rodeln
                                "CE6149F7BACC4FC3A4787BC706A3925A", // Schienenrodelban?
                                "6A1CCB9E7D0D4B44A92DC748FF5991DE", //Schneebahnen
                                "3D57411EBB42401691AC7E967AD2299A" // Tobbogan Run                                
                            ]
            });

            body.resultSet = new LTSActivitySearchBodyResultSet()
            {
                filterAndSummaryGroups = new List<LTSActivitySearchBodyFilterAndSummaryGroups>() { filterandsummarygroups }
            };

            var snowreportsearch = await ltsapi.ActivitySearchRequest(null, new List<string>() { "rid" }, body);

            var parsedsnowreportsearch = snowreportsearch[0].ToObject<LTSActivitySearchResult>();

            return parsedsnowreportsearch;
        }

        public static async Task<IEnumerable<MeasuringpointReduced>> GetMeasuringPoints(ICollection<string> areas, string language, ISettings settings)
        {
            var ltsapi = new LtsApi(
                settings.LtsCredentialsOpen.serviceurl,
                settings.LtsCredentialsOpen.username,
                settings.LtsCredentialsOpen.password,
                settings.LtsCredentialsOpen.ltsclientid,
                true);

            var qs = new LTSQueryStrings() { page_size = 1, filter_onlyActive = true, filter_areaRids = string.Join(",", areas) };
            var dict = ltsapi.GetLTSQSDictionary(qs);

            var ltsdata = await ltsapi.WeatherSnowListRequest(dict, true);
            List<LTSWeatherSnowsList> weathersnowdata = new List<LTSWeatherSnowsList>();

            foreach (var ltsdatasingle in ltsdata)
            {
                
                weathersnowdata.Add(
                    ltsdatasingle.ToObject<LTSWeatherSnowsList>()
                );
            }

            List<MeasuringpointReduced> measuringpointreducedlist = new List<MeasuringpointReduced>();

            foreach (var datalist in weathersnowdata)
            {
                foreach (var data in datalist.data)
                {
                    var measuringpointparsed = MeasuringpointParser.ParseLTSMeasuringpoint(data, false);

                    measuringpointreducedlist.Add(new MeasuringpointReduced()
                    {
                        Id = measuringpointparsed.Id,
                        LastSnowDate = measuringpointparsed.LastSnowDate ?? DateTime.MinValue,
                        LastUpdate = measuringpointparsed.LastUpdate ?? DateTime.MinValue,
                        newSnowHeight = measuringpointparsed.newSnowHeight,
                        Shortname = measuringpointparsed.Detail.ContainsKey(language) && !String.IsNullOrEmpty(measuringpointparsed.Detail[language].Title) ? measuringpointparsed.Detail[language].Title : measuringpointparsed.Shortname,
                        SnowHeight = measuringpointparsed.SnowHeight,
                        Source = measuringpointparsed.Source ?? "",
                        Temperature = measuringpointparsed.Temperature,
                        WeatherObservation = measuringpointparsed.WeatherObservation
                    }
                    );
                }
            }

            return measuringpointreducedlist;
        }

        public static async Task<SnowReportBaseData> ParseMySnowReportData(
            string lang,
            SkiArea skiarea,
            IEnumerable<WebcamInfo> webcams,
            LTSActivitySearchResult summaries,
            IEnumerable<MeasuringpointReduced> measuringpoints
        )
        {
            SnowReportBaseData mysnowreport = new SnowReportBaseData();

            try
            {
                string noinfotext = "";

                mysnowreport.Areaname = skiarea.Detail[lang].Title;
                mysnowreport.RID = skiarea.Id;
                mysnowreport.lang = lang;
                mysnowreport.Skiregion = skiarea.SkiRegionName[lang];

                //mysnowreport.LastUpdate = skiarea.LastChange;

                mysnowreport.contactadress = skiarea.ContactInfos[lang].Address;
                mysnowreport.contactcap = skiarea.ContactInfos[lang].ZipCode;
                mysnowreport.contactcity = skiarea.ContactInfos[lang].City;
                mysnowreport.contacttel = skiarea.ContactInfos[lang].Phonenumber;
                mysnowreport.contactfax = skiarea.ContactInfos[lang].Faxnumber;
                mysnowreport.contactmail = skiarea.ContactInfos[lang].Email;
                mysnowreport.contactweburl = skiarea.ContactInfos[lang].Url;
                mysnowreport.contactlogo = skiarea.ContactInfos[lang].LogoUrl;
                mysnowreport.contactgpseast = skiarea.Longitude != null ? skiarea.Longitude.Value.ToString(CultureInfo.InvariantCulture) : null;
                mysnowreport.contactgpsnorth = skiarea.Latitude != null ? skiarea.Latitude.Value.ToString(CultureInfo.InvariantCulture) : null;

                mysnowreport.SkiAreaSlopeKm = skiarea.TotalSlopeKm;
                mysnowreport.SkiMapUrl = skiarea.SkiAreaMapURL;

                //TO CHECK RANDOM?
                mysnowreport.WebcamUrl = webcams.Select(x => x.WebCamProperties.WebcamUrl).ToList();                

                //Summaries??? does not exist anymore
                var myliftsummary = summaries.resultSet.filterAndSummaryGroups.Select(x => x.filters.Where(x => x.id == 2).FirstOrDefault()).FirstOrDefault();
                var myslopesummary = summaries.resultSet.filterAndSummaryGroups.Select(x => x.filters.Where(x => x.id == 0).FirstOrDefault()).FirstOrDefault();
                var mycrosscountryskisummary = summaries.resultSet.filterAndSummaryGroups.Select(x => x.filters.Where(x => x.id == 1).FirstOrDefault()).FirstOrDefault();
                var mysledgesummary = summaries.resultSet.filterAndSummaryGroups.Select(x => x.filters.Where(x => x.id == 3).FirstOrDefault()).FirstOrDefault();

                if (myliftsummary != null)
                {
                    mysnowreport.openskilift =
                        myliftsummary.quantityOpen != null
                            ? myliftsummary.quantityOpen.ToString()
                            : noinfotext;
  
                    mysnowreport.totalskilift =
                        myliftsummary.quantity != null ? myliftsummary.quantity.ToString() : noinfotext;
  
                }

                //Read Slope infos from Summaries

                if (myslopesummary != null)
                {
                    mysnowreport.openskislopes =
                        myslopesummary.quantityOpen != null
                            ? myslopesummary.quantityOpen.ToString()
                            : noinfotext;
                    mysnowreport.totalskislopes =
                        myslopesummary.quantity != null
                            ? myslopesummary.quantity.ToString()
                            : noinfotext;               
                }


                
                if (mycrosscountryskisummary != null)
                {
                    //activityresponse.Root.Elements("Filters").Elements("EnumCodes").Elements("Item").Where(x => x.Attribute("OrderID").Value.Equals("3")).FirstOrDefault();
                    mysnowreport.opentracks =
                        mycrosscountryskisummary.quantityOpen != null
                            ? mycrosscountryskisummary.quantityOpen.ToString()
                            : noinfotext;
                    mysnowreport.totaltracks =
                        mycrosscountryskisummary.quantity != null
                            ? mycrosscountryskisummary.quantity.ToString()
                            : noinfotext;                    
                }

                //Read Sledge infos from Summaries
                if (mysledgesummary != null)
                {
                    mysnowreport.openslides =
                        mysledgesummary.quantityOpen != null
                            ? mysledgesummary.quantityOpen.ToString()
                            : noinfotext;
                    mysnowreport.totalslides =
                        mysledgesummary.quantity != null ? mysledgesummary.quantity.ToString() : noinfotext;                    
                }


                mysnowreport.Measuringpoints = measuringpoints
                    .OrderByDescending(x => x.LastUpdate)
                    .ToList();

                return mysnowreport;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


    }
}
