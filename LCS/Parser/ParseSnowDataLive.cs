// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DataModel;
using Helper;
using ServiceReferenceLCS;

namespace LCS
{
    public class ParseSnowDataLive
    {
        public static CultureInfo myculture = new CultureInfo("en");

        public static List<MeasuringpointReduced> ParseMyMeasuringPoint(
            ServiceReferenceLCS.WeatherSnowObservationSearchRS measuringpoints
        )
        {
            List<MeasuringpointReduced> mymeasuringpointlist = new List<MeasuringpointReduced>();

            if (measuringpoints.MeasuringPoints.MeasuringPoint != null)
            {
                foreach (var measuringpoint in measuringpoints.MeasuringPoints.MeasuringPoint)
                {
                    var mymeasuringpoint = new MeasuringpointReduced();

                    mymeasuringpoint.Id = measuringpoint.RID;
                    mymeasuringpoint.Shortname = measuringpoint.Name.FirstOrDefault().InnerText;

                    mymeasuringpoint.LastUpdate = Convert.ToDateTime(
                        measuringpoint.News.Status.LastChange
                    );

                    mymeasuringpoint.SnowHeight =
                        measuringpoint.Observation.Snow.Height != null
                            ? measuringpoint.Observation.Snow.Height.ToString()
                            : "0";
                    mymeasuringpoint.newSnowHeight =
                        measuringpoint.Observation.Snow.NewHeight != null
                            ? measuringpoint.Observation.Snow.NewHeight.ToString()
                            : "-";
                    mymeasuringpoint.Temperature =
                        measuringpoint.Observation.Temperature != null
                            ? measuringpoint.Observation.Temperature.ToString() + " °"
                            : "-";
                    mymeasuringpoint.LastSnowDate =
                        measuringpoint.Observation.Snow.DateLastSnow != null
                            ? Convert.ToDateTime(measuringpoint.Observation.Snow.DateLastSnow)
                            : DateTime.MinValue;
                    mymeasuringpoint.Source = "lts";

                    List<WeatherObservation> myweatherobservationlist =
                        new List<WeatherObservation>();

                    if (measuringpoint.Observation.EnumCodes != null)
                    {
                        if (measuringpoint.Observation.EnumCodes.EnumCode != null)
                        {
                            foreach (
                                var weatherobservation in measuringpoint
                                    .Observation.EnumCodes.EnumCode.FirstOrDefault()
                                    .Code
                            )
                            {
                                WeatherObservation myobservation = new WeatherObservation();
                                //myobservation.Level = weatherobservation.Level;
                                //myobservation.LevelId = weatherobservation.ID;
                                myobservation.Id = weatherobservation.RID;
                                myobservation.WeatherStatus["de"] = weatherobservation
                                    .Name.FirstOrDefault()
                                    .InnerText;

                                myweatherobservationlist.Add(myobservation);
                            }
                        }
                    }
                    mymeasuringpoint.WeatherObservation = myweatherobservationlist.ToList();

                    //NEU add the Measuringpoint only if DateTime is actual
                    if (mymeasuringpoint.LastUpdate > DateTime.Now.AddYears(-1))
                        mymeasuringpointlist.Add(mymeasuringpoint);
                }

                //NEU Bergstation - Talstation / Mittelstation fix
                bool hasbergstation = false;
                bool hasmittelstation = false;

                //Check if measuringpointlist has Bergstation OR Talstation / Mittelstation
                if (mymeasuringpointlist.Where(x => x.Shortname.Contains("Berg")).Count() > 0)
                    hasbergstation = true;

                if (
                    mymeasuringpointlist
                        .Where(x => x.Shortname.Contains("Tal") || x.Shortname.Contains("Mittel"))
                        .Count() > 0
                )
                    hasmittelstation = true;

                //IF only 1 available --> Bergstation
                if (!hasbergstation && !hasmittelstation && mymeasuringpointlist.Count == 1)
                {
                    mymeasuringpointlist.FirstOrDefault().Shortname =
                        "Berg " + mymeasuringpointlist.FirstOrDefault().Shortname;
                }
                else if (!hasbergstation || !hasmittelstation && mymeasuringpointlist.Count > 1)
                {
                    //Fall 2 kein Berg und keine Mittel mehr als 1 MEsspunkt
                    if (!hasbergstation && !hasmittelstation && mymeasuringpointlist.Count > 1)
                    {
                        var mypointtomodify = mymeasuringpointlist
                            .Where(x =>
                                !x.Shortname.Contains("Berg")
                                && !x.Shortname.Contains("Mittel")
                                && !x.Shortname.Contains("Tal")
                            )
                            .OrderByDescending(x => Convert.ToInt32(x.SnowHeight));

                        if (mypointtomodify.Count() > 0)
                            mypointtomodify.FirstOrDefault().Shortname =
                                "Berg " + mypointtomodify.FirstOrDefault().Shortname;
                        if (mypointtomodify.Count() > 1)
                            mypointtomodify.LastOrDefault().Shortname =
                                "Tal " + mypointtomodify.LastOrDefault().Shortname;
                    }
                    //Fall 3 keine Mittel mehr als 1 Messpunkt
                    else if (hasbergstation && !hasmittelstation && mymeasuringpointlist.Count > 1)
                    {
                        var mypointtomodify = mymeasuringpointlist
                            .Where(x => !x.Shortname.Contains("Berg"))
                            .OrderBy(x => Convert.ToInt32(x.SnowHeight));
                        if (mypointtomodify.Count() > 0)
                            mypointtomodify.FirstOrDefault().Shortname =
                                "Tal " + mypointtomodify.FirstOrDefault().Shortname;
                    }
                    //Fall 4 keine Berg mehr als 1 Messpunkt
                    else if (!hasbergstation && hasmittelstation && mymeasuringpointlist.Count > 1)
                    {
                        var mypointtomodify = mymeasuringpointlist
                            .Where(x =>
                                !x.Shortname.Contains("Tal") && !x.Shortname.Contains("Mittel")
                            )
                            .OrderByDescending(x => Convert.ToInt32(x.SnowHeight));
                        if (mypointtomodify.Count() > 0)
                            mypointtomodify.FirstOrDefault().Shortname =
                                "Berg " + mypointtomodify.FirstOrDefault().Shortname;
                    }

                    //Fall 5 beide vorhanden mache nix
                }
            }
            return mymeasuringpointlist;
        }

        public static SnowReportBaseData ParseMySnowReportData(
            string lang,
            SkiArea skiarea,
            IEnumerable<WebcamInfo> webcams,
            ServiceReferenceLCS.ActivitySearchRS snowdatalts,
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

                //Zufälliges webcam URL fischen muassi erst schaugn ob zuafällig
                //var randomwebcam = new Random();
                mysnowreport.WebcamUrl = webcams.Select(x => x.WebCamProperties.WebcamUrl).ToList();

                //Lese Lift infos aus Summaries
                //var myliftsummary = snowdatalts.Filters.EnumCodes.Item.Where(x => x.OrderID == "1").FirstOrDefault();

                var myliftsummary = snowdatalts
                    .Filters.Tagging.Tags.FirstOrDefault()
                    .Item.Where(x =>
                        x.ItemValue.FirstOrDefault().RID == "E23AA37B2AE3477F96D1C0782195AFDF"
                    )
                    .FirstOrDefault();

                //activityresponse.Root.Elements("Filters").Elements("EnumCodes").Elements("Item").Where(x => x.Attribute("OrderID").Value.Equals("1")).FirstOrDefault();
                if (myliftsummary != null)
                {
                    mysnowreport.openskilift =
                        myliftsummary.CountIsOpen != null
                            ? myliftsummary.CountIsOpen.ToString()
                            : noinfotext;
                    //myliftsummary != null ? myliftsummary.Attribute("CountIsOpen").Value : noinfotext;
                    mysnowreport.totalskilift =
                        myliftsummary.Count != null ? myliftsummary.Count.ToString() : noinfotext;
                    //myliftsummary != null ? myliftsummary.Attribute("Count").Value : noinfotext;
                    mysnowreport.openskiliftkm =
                        myliftsummary.SumLenghtOpen != null
                            ? myliftsummary.SumLenghtOpen.ToString()
                            : noinfotext;
                    //myliftsummary != null ? myliftsummary.Attribute("SumLengthOpen").Value : noinfotext;
                    mysnowreport.totalskiliftkm =
                        myliftsummary.SumLenght != null
                            ? myliftsummary.SumLenght.ToString()
                            : noinfotext;
                    //myliftsummary != null ? myliftsummary.Attribute("SumLength").Value : noinfotext;
                }
                //Lese Pisten infos aus Summaries

                //var mypistensummary = snowdatalts.Filters.EnumCodes.Item.Where(x => x.OrderID == "2").FirstOrDefault();
                var mypistensummary = snowdatalts
                    .Filters.Tagging.Tags.FirstOrDefault()
                    .Item.Where(x =>
                        x.ItemValue.FirstOrDefault().RID == "EB5D6F10C0CB4797A2A04818088CD6AB"
                    )
                    .FirstOrDefault();
                //activityresponse.Root.Elements("Filters").Elements("EnumCodes").Elements("Item").Where(x => x.Attribute("OrderID").Value.Equals("2")).FirstOrDefault();
                if (mypistensummary != null)
                {
                    mysnowreport.openskislopes =
                        mypistensummary.CountIsOpen != null
                            ? mypistensummary.CountIsOpen.ToString()
                            : noinfotext;
                    mysnowreport.totalskislopes =
                        mypistensummary.Count != null
                            ? mypistensummary.Count.ToString()
                            : noinfotext;
                    //mysnowreport.openskislopeskm = mypistensummary.SumLenghtOpenSpecified ? myliftsummary.SumLenghtOpen.ToString() : noinfotext;
                    //mysnowreport.totalskislopeskm = mypistensummary.SumLenghtSpecified ? myliftsummary.SumLenght.ToString() : noinfotext;

                    double openskislopeskmdb =
                        mypistensummary != null
                            ? Convert.ToDouble(mypistensummary.SumLenghtOpen)
                            : 0;
                    double tempopenskislopeskmdb = openskislopeskmdb / 1000;
                    mysnowreport.openskislopeskm = String.Format("{0:0}", tempopenskislopeskmdb);

                    double totalskislopeskmdb =
                        mypistensummary != null ? Convert.ToDouble(mypistensummary.SumLenght) : 0;
                    double temptotalskislopeskmdb = totalskislopeskmdb / 1000;
                    mysnowreport.totalskislopeskm = String.Format("{0:0}", temptotalskislopeskmdb);
                }
                //Lese Longlaf infos aus Summaries
                //var mylonglafsummary = snowdatalts.Filters.EnumCodes.Item.Where(x => x.OrderID == "3").FirstOrDefault();
                var mylonglafsummary = snowdatalts
                    .Filters.Tagging.Tags.FirstOrDefault()
                    .Item.Where(x =>
                        x.ItemValue.FirstOrDefault().RID == "D544A6312F8A47CF80CC4DFF8833FE50"
                    )
                    .FirstOrDefault();

                if (mylonglafsummary != null)
                {
                    //activityresponse.Root.Elements("Filters").Elements("EnumCodes").Elements("Item").Where(x => x.Attribute("OrderID").Value.Equals("3")).FirstOrDefault();
                    mysnowreport.opentracks =
                        mylonglafsummary.CountIsOpen != null
                            ? mylonglafsummary.CountIsOpen.ToString()
                            : noinfotext;
                    mysnowreport.totaltracks =
                        mylonglafsummary.Count != null
                            ? mylonglafsummary.Count.ToString()
                            : noinfotext;
                    //mysnowreport.opentrackskm = mylonglafsummary != null ? mylonglafsummary.Attribute("SumLengthOpen").Value : noinfotext;
                    //mysnowreport.totaltrackskm = mylonglafsummary != null ? mylonglafsummary.Attribute("SumLength").Value : noinfotext;

                    double openskitrackkmdb =
                        mylonglafsummary != null
                            ? Convert.ToDouble(mylonglafsummary.SumLenghtOpen)
                            : 0;
                    double tempopenskitrackkmdb = openskitrackkmdb / 1000;
                    mysnowreport.opentrackskm = String.Format("{0:0}", tempopenskitrackkmdb);

                    double totalskitrackkmdb =
                        mylonglafsummary != null ? Convert.ToDouble(mylonglafsummary.SumLenght) : 0;
                    double temptotalskitrackkmdb = totalskitrackkmdb / 1000;
                    mysnowreport.totaltrackskm = String.Format("{0:0}", temptotalskitrackkmdb);
                }

                //Lese Rodel infos aus Summaries
                //var myrodelsummary = snowdatalts.Filters.EnumCodes.Item.Where(x => x.OrderID == "4").FirstOrDefault();
                var myrodelsummary = snowdatalts
                    .Filters.Tagging.Tags.FirstOrDefault()
                    .Item.Where(x =>
                        x.ItemValue.FirstOrDefault().RID == "F3B08D06569646F38462EDCA506D81D4"
                    )
                    .FirstOrDefault();
                //activityresponse.Root.Elements("Filters").Elements("EnumCodes").Elements("Item").Where(x => x.Attribute("OrderID").Value.Equals("4")).FirstOrDefault();
                if (myrodelsummary != null)
                {
                    mysnowreport.opentslides =
                        myrodelsummary.CountIsOpen != null
                            ? myrodelsummary.CountIsOpen.ToString()
                            : noinfotext;
                    mysnowreport.totalslides =
                        myrodelsummary.Count != null ? myrodelsummary.Count.ToString() : noinfotext;
                    mysnowreport.opentslideskm =
                        myrodelsummary.SumLenghtOpen != null
                            ? myrodelsummary.SumLenghtOpen.ToString()
                            : noinfotext;
                    mysnowreport.totalslideskm =
                        myrodelsummary.SumLenght != null
                            ? myrodelsummary.SumLenght.ToString()
                            : noinfotext;
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
