using DataModel;
using Geo.Gps;
using Helper;
using LTSAPI;
using LTSAPI.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OdhApiCore.Controllers.helper
{
    public class SnowReportHelper
    {
        public static async Task<IEnumerable<MeasuringpointReduced>> GetMeasuringPoints(List<string> areas, ISettings settings)
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
            List<LTSWeatherSnows> weathersnowdata = new List<LTSWeatherSnows>();

            foreach (var ltsdatasingle in ltsdata)
            {
                weathersnowdata.Add(
                    ltsdatasingle.ToObject<LTSWeatherSnows>()
                );
            }

            List<MeasuringpointReduced> measuringpointreducedlist = new List<MeasuringpointReduced>();

            foreach(var data in weathersnowdata)
            {
                var measuringpointparsed = MeasuringpointParser.ParseLTSMeasuringpoint(data.data, false);

                measuringpointreducedlist.Add(new MeasuringpointReduced()
                {
                    Id = measuringpointparsed.Id,
                    LastSnowDate = measuringpointparsed.LastSnowDate ?? DateTime.MinValue,
                    LastUpdate = measuringpointparsed.LastUpdate ?? DateTime.MinValue,
                    newSnowHeight = measuringpointparsed.newSnowHeight,
                    Shortname = measuringpointparsed.Shortname,
                    SnowHeight = measuringpointparsed.SnowHeight,
                    Source = measuringpointparsed.Source ?? "",
                    Temperature = measuringpointparsed.Temperature,
                    WeatherObservation = measuringpointparsed.WeatherObservation
                }
                );
            }

            return measuringpointreducedlist;
        }

        public static SnowReportBaseData ParseMySnowReportData(
            string lang,
            SkiArea skiarea,
            IEnumerable<WebcamInfo> webcams,
            List<ODHActivityPoiLinked> snowdatalts,
            List<MeasuringpointReduced> measuringpoints
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
                mysnowreport.contactgpseast = skiarea.Longitude.ToString();
                mysnowreport.contactgpsnorth = skiarea.Latitude.ToString();

                mysnowreport.SkiAreaSlopeKm = skiarea.TotalSlopeKm;
                mysnowreport.SkiMapUrl = skiarea.SkiAreaMapURL;

                //TO CHECK RANDOM?
                mysnowreport.WebcamUrl = webcams.Select(x => x.WebCamProperties.WebcamUrl).ToList();

                //Summaries??? does not exist anymore
                //var myliftsummary = snowdatalts.Filters.EnumCodes.Item.Where(x => x.OrderID == "1").FirstOrDefault();

                //var myliftsummary = snowdatalts
                //    .Filters.Tagging.Tags.FirstOrDefault()
                //    .Item.Where(x =>
                //        x.ItemValue.FirstOrDefault().RID == "E23AA37B2AE3477F96D1C0782195AFDF"
                //    )
                //    .FirstOrDefault();

                //activityresponse.Root.Elements("Filters").Elements("EnumCodes").Elements("Item").Where(x => x.Attribute("OrderID").Value.Equals("1")).FirstOrDefault();
                //if (myliftsummary != null)
                //{
                //    mysnowreport.openskilift =
                //        myliftsummary.CountIsOpen != null
                //            ? myliftsummary.CountIsOpen.ToString()
                //            : noinfotext;
                //    //myliftsummary != null ? myliftsummary.Attribute("CountIsOpen").Value : noinfotext;
                //    mysnowreport.totalskilift =
                //        myliftsummary.Count != null ? myliftsummary.Count.ToString() : noinfotext;
                //    //myliftsummary != null ? myliftsummary.Attribute("Count").Value : noinfotext;
                //    mysnowreport.openskiliftkm =
                //        myliftsummary.SumLenghtOpen != null
                //            ? myliftsummary.SumLenghtOpen.ToString()
                //            : noinfotext;
                //    //myliftsummary != null ? myliftsummary.Attribute("SumLengthOpen").Value : noinfotext;
                //    mysnowreport.totalskiliftkm =
                //        myliftsummary.SumLenght != null
                //            ? myliftsummary.SumLenght.ToString()
                //            : noinfotext;
                //    //myliftsummary != null ? myliftsummary.Attribute("SumLength").Value : noinfotext;
                //}
                
                //Read Slope infos from Summaries

                ////var mypistensummary = snowdatalts.Filters.EnumCodes.Item.Where(x => x.OrderID == "2").FirstOrDefault();
                //var mypistensummary = snowdatalts
                //    .Filters.Tagging.Tags.FirstOrDefault()
                //    .Item.Where(x =>
                //        x.ItemValue.FirstOrDefault().RID == "EB5D6F10C0CB4797A2A04818088CD6AB"
                //    )
                //    .FirstOrDefault();
                ////activityresponse.Root.Elements("Filters").Elements("EnumCodes").Elements("Item").Where(x => x.Attribute("OrderID").Value.Equals("2")).FirstOrDefault();
                //if (mypistensummary != null)
                //{
                //    mysnowreport.openskislopes =
                //        mypistensummary.CountIsOpen != null
                //            ? mypistensummary.CountIsOpen.ToString()
                //            : noinfotext;
                //    mysnowreport.totalskislopes =
                //        mypistensummary.Count != null
                //            ? mypistensummary.Count.ToString()
                //            : noinfotext;
                //    //mysnowreport.openskislopeskm = mypistensummary.SumLenghtOpenSpecified ? myliftsummary.SumLenghtOpen.ToString() : noinfotext;
                //    //mysnowreport.totalskislopeskm = mypistensummary.SumLenghtSpecified ? myliftsummary.SumLenght.ToString() : noinfotext;

                //    double openskislopeskmdb =
                //        mypistensummary != null
                //            ? Convert.ToDouble(mypistensummary.SumLenghtOpen)
                //            : 0;
                //    double tempopenskislopeskmdb = openskislopeskmdb / 1000;
                //    mysnowreport.openskislopeskm = String.Format("{0:0}", tempopenskislopeskmdb);

                //    double totalskislopeskmdb =
                //        mypistensummary != null ? Convert.ToDouble(mypistensummary.SumLenght) : 0;
                //    double temptotalskislopeskmdb = totalskislopeskmdb / 1000;
                //    mysnowreport.totalskislopeskm = String.Format("{0:0}", temptotalskislopeskmdb);
                //}


                //Read Cross country skiing infos from Summaries
                //var mylonglafsummary = snowdatalts
                //    .Filters.Tagging.Tags.FirstOrDefault()
                //    .Item.Where(x =>
                //        x.ItemValue.FirstOrDefault().RID == "D544A6312F8A47CF80CC4DFF8833FE50"
                //    )
                //    .FirstOrDefault();

                //if (mylonglafsummary != null)
                //{
                //    //activityresponse.Root.Elements("Filters").Elements("EnumCodes").Elements("Item").Where(x => x.Attribute("OrderID").Value.Equals("3")).FirstOrDefault();
                //    mysnowreport.opentracks =
                //        mylonglafsummary.CountIsOpen != null
                //            ? mylonglafsummary.CountIsOpen.ToString()
                //            : noinfotext;
                //    mysnowreport.totaltracks =
                //        mylonglafsummary.Count != null
                //            ? mylonglafsummary.Count.ToString()
                //            : noinfotext;
                //    //mysnowreport.opentrackskm = mylonglafsummary != null ? mylonglafsummary.Attribute("SumLengthOpen").Value : noinfotext;
                //    //mysnowreport.totaltrackskm = mylonglafsummary != null ? mylonglafsummary.Attribute("SumLength").Value : noinfotext;

                //    double openskitrackkmdb =
                //        mylonglafsummary != null
                //            ? Convert.ToDouble(mylonglafsummary.SumLenghtOpen)
                //            : 0;
                //    double tempopenskitrackkmdb = openskitrackkmdb / 1000;
                //    mysnowreport.opentrackskm = String.Format("{0:0}", tempopenskitrackkmdb);

                //    double totalskitrackkmdb =
                //        mylonglafsummary != null ? Convert.ToDouble(mylonglafsummary.SumLenght) : 0;
                //    double temptotalskitrackkmdb = totalskitrackkmdb / 1000;
                //    mysnowreport.totaltrackskm = String.Format("{0:0}", temptotalskitrackkmdb);
                //}

                //Read Sledge infos from Summaries
                //var myrodelsummary = snowdatalts
                //    .Filters.Tagging.Tags.FirstOrDefault()
                //    .Item.Where(x =>
                //        x.ItemValue.FirstOrDefault().RID == "F3B08D06569646F38462EDCA506D81D4"
                //    )
                //    .FirstOrDefault();
                ////activityresponse.Root.Elements("Filters").Elements("EnumCodes").Elements("Item").Where(x => x.Attribute("OrderID").Value.Equals("4")).FirstOrDefault();
                //if (myrodelsummary != null)
                //{
                //    mysnowreport.opentslides =
                //        myrodelsummary.CountIsOpen != null
                //            ? myrodelsummary.CountIsOpen.ToString()
                //            : noinfotext;
                //    mysnowreport.totalslides =
                //        myrodelsummary.Count != null ? myrodelsummary.Count.ToString() : noinfotext;
                //    mysnowreport.opentslideskm =
                //        myrodelsummary.SumLenghtOpen != null
                //            ? myrodelsummary.SumLenghtOpen.ToString()
                //            : noinfotext;
                //    mysnowreport.totalslideskm =
                //        myrodelsummary.SumLenght != null
                //            ? myrodelsummary.SumLenght.ToString()
                //            : noinfotext;
                //}


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
