// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataModel;
using Helper;

namespace HDS
{
    public class ParseHDSPois
    {
        public static ODHActivityPoiLinked ParseHDSMarketToODHActivityPoi(
            HDSMarket market
        )
        {           
            var mypoi = new ODHActivityPoiLinked();

            //ID
            var id = "hds:market:" + System.Guid.NewGuid();
            mypoi.Id = id;

            //GPSData
            var commaCulture = new CultureInfo("de")
            {
                NumberFormat = { NumberDecimalSeparator = "." },
            };

            if(!String.IsNullOrEmpty(market.Geoloc))
            {
                var splitted = market.Geoloc.Split(",");
                double gpslat = !String.IsNullOrEmpty(splitted[0].Trim())
                    ? Convert.ToDouble(splitted[0].Trim(), commaCulture)
                    : 0;
                double gpslong = !String.IsNullOrEmpty(splitted[1].Trim())
                    ? Convert.ToDouble(splitted[1].Trim(), commaCulture)
                    : 0;

                if (gpslat != 0 && gpslong != 0)
                {
                    GpsInfo gpsinfo = new GpsInfo();
                    gpsinfo.Gpstype = "position";
                    gpsinfo.Latitude = gpslat;
                    gpsinfo.Longitude = gpslong;

                    mypoi.GpsPoints.TryAddOrUpdate(gpsinfo.Gpstype, gpsinfo);

                    if (mypoi.GpsInfo == null)
                        mypoi.GpsInfo = new List<GpsInfo>();
                    mypoi.GpsInfo.Add(gpsinfo);
                }
            }

            ////END GPsData

            ////DETAIL Parsing

            var namesplitted = market.Municipality.Split("-");

            Detail detailde = new Detail();
            detailde.Language = "de";
            detailde.Title = namesplitted[0].Trim();
            
            mypoi.Detail.TryAddOrUpdate("de", detailde);

            Detail detailit = new Detail();
            detailit.Language = "it";
            detailit.Title = namesplitted.Length > 1 ? namesplitted[1].Trim() : namesplitted[0].Trim();

            mypoi.Detail.TryAddOrUpdate("it", detailit);

            ////End DETAIL Parsing

            /// Image Parsing

            mypoi.ImageGallery.Add(
                new ImageGallery()
                {
                    ImageSource = "hds",
                    ImageUrl = market.Foto,
                    ListPosition = 0,
                    ImageName = market.Municipality,
                    //License,
                    //LicenseHolder
                });

            
            /// End Image Parsing



            ////OpeningTimes Parsing

            var operationschedule = ParseOperationScheduleFromCSV(market.Weekday, market.Frequency, market.Seasonality);

            if (operationschedule != null)
            {
                mypoi.OperationSchedule = new List<OperationSchedule>();
                mypoi.OperationSchedule.Add(operationschedule);
            }

            ////END Openingtimes Parsing

            ////TODO
            ////Categorization

            ////ODH Tags
            //mypoi.SmgTags = new List<string>();
            //mypoi.SmgTags.Add("mobilität");
            //mypoi.SmgTags.Add("verkaufstellen ticket oeffentliche verkehrsmittel");

            //TagIDs
            
            mypoi.SyncSourceInterface = "market";
            mypoi.SyncUpdateMode = "full";
            mypoi.Source = "hds";

            mypoi.Active = true;
            
            mypoi.HasLanguage = new List<string>() { "de", "it" };

            mypoi.Shortname = mypoi.Detail["de"].Title;

            mypoi.LastChange = DateTime.Now;

            return mypoi;
        }

        public static ODHActivityPoiLinked ParseHDSYearMarketToODHActivityPoi(
            HDSYearMarket market
        )
        {
            var mypoi = new ODHActivityPoiLinked();

            //ID
            var id = "hds:yearmarket:" + System.Guid.NewGuid();
            mypoi.Id = id;

            return mypoi;
        }


        private static OperationSchedule? ParseOperationScheduleFromCSV(
            string weekday, string frequency, string seasonality
        )
        {
            OperationSchedule myoperationschedule = new OperationSchedule();
            myoperationschedule.Type = "2";

            //JÄHRLICH - ANNUALE || SAISONAL - STAGIONALE
            if (seasonality == "JÄHRLICH - ANNUALE")
            {
                //SET Start and End to whole year
                myoperationschedule.Start = new DateTime(2025, 01, 01);
                myoperationschedule.Stop = new DateTime(2025, 12, 31);
            }
            else if (seasonality == "SAISONAL - STAGIONALE")
            {
                //Parse frequency WÖCHENTLICH (APRIL-OKTOBER) - SETTIMANALE (APRILE-OTTOBRE) || WÖCHENTLICH (JULI-SEPTEMBER) - SETTIMANALE (LUGLIO-SETTEMBRE)
                var splittedfrequency = frequency.Replace(" - ", "|").Split('|');
                string parsedfrequency = "";
                if (splittedfrequency != null && splittedfrequency.Length > 0)
                    parsedfrequency = splittedfrequency[0].Trim().Replace("WÖCHENTLICH", "").Replace("(","").Replace(")", "");

                var parsedfrequencysplitted = parsedfrequency.Split("-");

                if(parsedfrequencysplitted != null && parsedfrequencysplitted.Length > 1)
                {
                    var startmonth = GetMonthByName(parsedfrequencysplitted[0]);
                    var endmonth = GetMonthByName(parsedfrequencysplitted[1]);

                    if(startmonth > 0)
                        myoperationschedule.Start = new DateTime(2025, startmonth, 01);
                    if (endmonth > 0)
                        myoperationschedule.Stop = GetLastDateOfMonth(2025, endmonth);
                }
            }

            if (weekday != null)
            {
                myoperationschedule.OperationScheduleTime = new List<OperationScheduleTime>();

                var splittedweekday = weekday.Split("-");
                if (splittedweekday.Length > 0)
                {
                    var parsedweekday = splittedweekday[0].Trim();

                    //Add closed Schedule
                    OperationScheduleTime myopeningtime = new OperationScheduleTime();
                    myopeningtime.Monday = parsedweekday == "MONTAG" ? true : false;
                    myopeningtime.Tuesday = parsedweekday == "DIENSTAG" ? true : false;
                    myopeningtime.Wednesday = parsedweekday == "MITTWOCH" ? true : false;
                    myopeningtime.Thursday = parsedweekday == "DONNERSTAG" ? true : false;
                    //myoptimeclosed.Thursday = myday == "Thursday" ? true : false;
                    myopeningtime.Friday = parsedweekday == "FREITAG" ? true : false;
                    myopeningtime.Saturday = parsedweekday == "SAMSTAG" ? true : false;
                    myopeningtime.Sunday = parsedweekday == "SONNTAG" ? true : false;

                    myopeningtime.Timecode = 1;
                    myopeningtime.State = 2;

                    myopeningtime.Start = new TimeSpan(0, 0, 1);
                    myopeningtime.End = new TimeSpan(23, 59, 59);

                    myoperationschedule.OperationScheduleTime.Add(myopeningtime);
                }
            }

            return myoperationschedule;
        }

        private static int GetMonthByName(string name)
        {
            return name switch
            {
                "JÄNNER" => 1,
                "FEBRUAR" => 2,
                "MÄRZ" => 3,
                "APRIL" => 4,
                "MAI" => 5,
                "JUNI" => 6,
                "JULI" => 7,
                "AUGUST" => 8,
                "SEPTEMBER" => 9,
                "OKTOBER" => 10,
                "NOVEMBER" => 11,
                "DEZEMBER" => 12,
                _ => 0
            };
        }

        private static DateTime GetLastDateOfMonth(int year, int month)
        {
            int lastDay = DateTime.DaysInMonth(year, month);
            return new DateTime(year, month, lastDay);
        }
    }
}
