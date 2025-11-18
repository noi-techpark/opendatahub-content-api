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

            //ADD MAPPING
            //if (market.STA_ID is { })
            //{
            //    var staid = new Dictionary<string, string>() { { "sta_id", vendingpoint.STA_ID } };
            //    mypoi.Mapping.TryAddOrUpdate("sta", staid);
            //}

            //GPSData
            //var commaCulture = new CultureInfo("en")
            //{
            //    NumberFormat = { NumberDecimalSeparator = "," },
            //};

            //double gpslat = !String.IsNullOrEmpty(vendingpoint.latitude)
            //    ? Convert.ToDouble(vendingpoint.latitude, commaCulture)
            //    : 0;
            //double gpslong = !String.IsNullOrEmpty(vendingpoint.longitude)
            //    ? Convert.ToDouble(vendingpoint.longitude, commaCulture)
            //    : 0;

            //if (gpslat != 0 && gpslong != 0)
            //{
            //    GpsInfo gpsinfo = new GpsInfo();
            //    gpsinfo.Gpstype = "position";
            //    gpsinfo.Latitude = gpslat;
            //    gpsinfo.Longitude = gpslong;

            //    mypoi.GpsPoints.TryAddOrUpdate(gpsinfo.Gpstype, gpsinfo);

            //    if (mypoi.GpsInfo == null)
            //        mypoi.GpsInfo = new List<GpsInfo>();
            //    mypoi.GpsInfo.Add(gpsinfo);
            //}

            ////END GPsData

            ////TODO

            ////Properties Parsing
            //List<PoiProperty> mypoipropertylist = new List<PoiProperty>();

            //PoiProperty pPhasWebsite = new PoiProperty()
            //{
            //    Name = "haswebsite",
            //    Value = vendingpoint.Website == "1" ? "True" : "False",
            //};
            //mypoipropertylist.Add(pPhasWebsite);

            //PoiProperty pPsuedtirolpassdienste = new PoiProperty()
            //{
            //    Name = "suedtirolpass_services",
            //    Value = vendingpoint.SuedtirolPassDienste == "1" ? "True" : "False",
            //};
            //mypoipropertylist.Add(pPsuedtirolpassdienste);

            //PoiProperty pPsuedtirolpassover65 = new PoiProperty()
            //{
            //    Name = "suedtirolpass_over65_apply",
            //    Value = vendingpoint.SuedtirolPass65PlusBeantragung == "1" ? "True" : "False",
            //};
            //mypoipropertylist.Add(pPsuedtirolpassover65);

            //PoiProperty pPduplicate = new PoiProperty()
            //{
            //    Name = "duplicate",
            //    Value = vendingpoint.Duplikat == "1" ? "True" : "False",
            //};
            //mypoipropertylist.Add(pPduplicate);

            //PoiProperty pPHwertkarte = new PoiProperty()
            //{
            //    Name = "chargecard",
            //    Value = vendingpoint.Wertkarte == "1" ? "True" : "False",
            //};
            //mypoipropertylist.Add(pPHwertkarte);

            //PoiProperty pPstadtfahrkartecitybus = new PoiProperty()
            //{
            //    Name = "city_card_bus",
            //    Value = vendingpoint.StadtfahrkarteoCitybus == "1" ? "True" : "False",
            //};
            //mypoipropertylist.Add(pPstadtfahrkartecitybus);

            //PoiProperty pPmobilcard = new PoiProperty()
            //{
            //    Name = "mobilecard",
            //    Value = vendingpoint.Mobilcard == "1" ? "True" : "False",
            //};
            //mypoipropertylist.Add(pPmobilcard);

            //PoiProperty pPbikemobilcard = new PoiProperty()
            //{
            //    Name = "bike_mobilecard",
            //    Value = vendingpoint.bikemobilCard == "1" ? "True" : "False",
            //};
            //mypoipropertylist.Add(pPbikemobilcard);

            //PoiProperty pPmobilecard = new PoiProperty()
            //{
            //    Name = "museum_mobilecard",
            //    Value = vendingpoint.MuseumobilCard == "1" ? "True" : "False",
            //};
            //mypoipropertylist.Add(pPmobilecard);

            //mypoi.PoiProperty.TryAddOrUpdate("de", mypoipropertylist);
            //mypoi.PoiProperty.TryAddOrUpdate("it", mypoipropertylist);
            //mypoi.PoiProperty.TryAddOrUpdate("en", mypoipropertylist);
            //mypoi.PoiProperty.TryAddOrUpdate("ld", mypoipropertylist);

            ////End Properties Parsing

            ////DETAIL Parsing

            //Detail detailde = new Detail();
            //detailde.Language = "de";
            //detailde.Title = vendingpoint.Salepoint_Name_STA_DE;

            //if (!String.IsNullOrEmpty(vendingpoint.Zusatzinfo_DE))
            //    detailde.AdditionalText = vendingpoint.Zusatzinfo_DE;

            //mypoi.Detail.TryAddOrUpdate("de", detailde);

            //Detail detailit = new Detail();
            //detailit.Language = "it";
            //detailit.Title = vendingpoint.Salepoint_Name_STA_IT;

            //if (!String.IsNullOrEmpty(vendingpoint.Zusatzinfo_IT))
            //    detailde.AdditionalText = vendingpoint.Zusatzinfo_IT;

            //mypoi.Detail.TryAddOrUpdate("it", detailit);

            //Detail detailen = new Detail();
            //detailen.Language = "en";
            //detailen.Title = vendingpoint.Salepoint_Name_STA_EN;

            //if (!String.IsNullOrEmpty(vendingpoint.Zusatzinfo_EN))
            //    detailde.AdditionalText = vendingpoint.Zusatzinfo_EN;

            //mypoi.Detail.TryAddOrUpdate("en", detailen);

            //Detail detaillad = new Detail();
            //detaillad.Language = "ld"; //ISO 639-3
            //detaillad.Title = vendingpoint.Salepoint_Name_STA_LAD;

            //if (!String.IsNullOrEmpty(vendingpoint.Zusatzinfo_LAD))
            //    detailde.AdditionalText = vendingpoint.Zusatzinfo_LAD;

            //mypoi.Detail.TryAddOrUpdate("ld", detaillad);

            ////End DETAIL Parsing

            ////Address Parsing
            //ContactInfos contactInfosde = new ContactInfos();
            //contactInfosde.CompanyName = vendingpoint.Salepoint_Name_STA_DE;
            //contactInfosde.ZipCode = vendingpoint.CAP;
            //contactInfosde.Address = vendingpoint.Adresse_DE;
            //contactInfosde.City = vendingpoint.Stadt;
            //contactInfosde.Phonenumber = vendingpoint.Tel;
            //contactInfosde.Email = vendingpoint.E_Mail;
            //contactInfosde.Language = "de";
            ////contactInfosde.Vat = vendingpoint.Parita_IVA; TO CHECK WITH ROBERTO
            //mypoi.ContactInfos.TryAddOrUpdate("de", contactInfosde);

            //ContactInfos contactInfosit = new ContactInfos();
            //contactInfosit.CompanyName = vendingpoint.Salepoint_Name_STA_IT;
            //contactInfosit.ZipCode = vendingpoint.CAP;
            //contactInfosit.Address = vendingpoint.Adresse_IT_EN_LAD;
            //contactInfosit.City = vendingpoint.cittaIT_EN_LAD;
            //contactInfosit.Phonenumber = vendingpoint.Tel;
            //contactInfosit.Email = vendingpoint.E_Mail;
            //contactInfosit.Language = "it";
            ////contactInfosit.Vat = vendingpoint.Parita_IVA; TO CHECK WITH ROBERTO
            //mypoi.ContactInfos.TryAddOrUpdate("it", contactInfosit);

            //ContactInfos contactInfosen = new ContactInfos();
            //contactInfosen.CompanyName = vendingpoint.Salepoint_Name_STA_EN;
            //contactInfosen.ZipCode = vendingpoint.CAP;
            //contactInfosen.Address = vendingpoint.Adresse_IT_EN_LAD;
            //contactInfosen.City = vendingpoint.cittaIT_EN_LAD;
            //contactInfosen.Phonenumber = vendingpoint.Tel;
            //contactInfosen.Email = vendingpoint.E_Mail;
            //contactInfosen.Language = "en";
            ////contactInfosen.Vat = vendingpoint.Parita_IVA; TO CHECK WITH ROBERTO
            //mypoi.ContactInfos.TryAddOrUpdate("en", contactInfosen);

            //ContactInfos contactInfoslad = new ContactInfos();
            //contactInfoslad.CompanyName = vendingpoint.Salepoint_Name_STA_LAD;
            //contactInfoslad.ZipCode = vendingpoint.CAP;
            //contactInfoslad.Address = vendingpoint.Adresse_IT_EN_LAD;
            //contactInfoslad.City = vendingpoint.cittaIT_EN_LAD;
            //contactInfoslad.Phonenumber = vendingpoint.Tel;
            //contactInfoslad.Email = vendingpoint.E_Mail;
            //contactInfoslad.Language = "ld";
            ////contactInfoslad.Vat = vendingpoint.Parita_IVA; TO CHECK WITH ROBERTO
            //mypoi.ContactInfos.TryAddOrUpdate("ld", contactInfoslad);

            ////END Address Parsing

            ////TODO
            ////OpeningTimes Parsing

            ////Standard open all year? Make use of DB with Festive days and add them as closed?
            //var operationschedule = ParseOperationScheduleFromCSV(vendingpoint);

            //if (operationschedule != null)
            //{
            //    mypoi.OperationSchedule = new List<OperationSchedule>();

            //    mypoi.OperationSchedule.Add(operationschedule);
            //}

            ////END Openingtimes Parsing

            ////TODO
            ////Categorization

            ////ODH Tags
            //mypoi.SmgTags = new List<string>();
            //mypoi.SmgTags.Add("mobilität");
            //mypoi.SmgTags.Add("verkaufstellen ticket oeffentliche verkehrsmittel");

            ////ODH Categorizations
            ////TODO LOAD Categorizations
            //mypoi.Type = "Mobilität";
            //mypoi.SubType = "Verkaufstellen Ticket Oeffentliche Verkehrsmittel";

            //mypoi.SyncSourceInterface = "sta";
            //mypoi.SyncUpdateMode = "Full";
            //mypoi.Source = "sta";

            //mypoi.Active = true;
            //mypoi.SmgActive = true;

            //mypoi.HasLanguage = new List<string>() { "de", "it", "en", "ld" };

            //mypoi.Shortname = mypoi.Detail["de"].Title;

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
            HDSMarket market
        )
        {
            //if (
            //    !String.IsNullOrEmpty(vendingpoint.Wochentags_Beginn)
            //    || !String.IsNullOrEmpty(vendingpoint.Samstag_Beginn)
            //    || !String.IsNullOrEmpty(vendingpoint.Sonntag_Beginn)
            //)
            //{
            //    OperationSchedule myoperationschedule = new OperationSchedule();
            //    myoperationschedule.OperationscheduleName = new Dictionary<string, string>()
            //    {
            //        { "de", "Öffnungszeiten" },
            //        { "it", "orario d'apertura" },
            //        { "en", "Opening time" },
            //    };
            //    myoperationschedule.Start = new DateTime(DateTime.Now.Year, 1, 1);
            //    myoperationschedule.Stop = new DateTime(DateTime.Now.Year, 12, 31);
            //    myoperationschedule.Type = "2";

            //    ////Try Parsing Columns
            //    TimeSpan beginweekday = default(TimeSpan);
            //    TimeSpan.TryParse(
            //        vendingpoint.Wochentags_Beginn,
            //        CultureInfo.InvariantCulture,
            //        out beginweekday
            //    );

            //    TimeSpan endweekdaynoon = default(TimeSpan);
            //    TimeSpan.TryParse(
            //        vendingpoint.Pause_Start,
            //        CultureInfo.InvariantCulture,
            //        out endweekdaynoon
            //    );

            //    TimeSpan beginweekdaynoon = default(TimeSpan);
            //    TimeSpan.TryParse(
            //        vendingpoint.Pause_Ende,
            //        CultureInfo.InvariantCulture,
            //        out beginweekdaynoon
            //    );

            //    TimeSpan endweekday = default(TimeSpan);
            //    TimeSpan.TryParse(
            //        vendingpoint.Wochentags_Ende,
            //        CultureInfo.InvariantCulture,
            //        out endweekday
            //    );

            //    //Saturday

            //    TimeSpan beginsaturday = default(TimeSpan);
            //    TimeSpan.TryParse(
            //        vendingpoint.Samstag_Beginn,
            //        CultureInfo.InvariantCulture,
            //        out beginsaturday
            //    );

            //    TimeSpan endsaturdaynoon = default(TimeSpan);
            //    TimeSpan.TryParse(
            //        vendingpoint.Pause_Samstag_Beginn,
            //        CultureInfo.InvariantCulture,
            //        out endsaturdaynoon
            //    );

            //    TimeSpan beginsaturdaynoon = default(TimeSpan);
            //    TimeSpan.TryParse(
            //        vendingpoint.Pause_Samstag_Ende,
            //        CultureInfo.InvariantCulture,
            //        out beginsaturdaynoon
            //    );

            //    TimeSpan endsaturday = default(TimeSpan);
            //    TimeSpan.TryParse(
            //        vendingpoint.Samstag_Ende,
            //        CultureInfo.InvariantCulture,
            //        out endsaturday
            //    );

            //    //Sunday

            //    TimeSpan beginsunday = default(TimeSpan);
            //    TimeSpan.TryParse(
            //        vendingpoint.Sonntag_Beginn,
            //        CultureInfo.InvariantCulture,
            //        out beginsunday
            //    );

            //    TimeSpan endsundaynoon = default(TimeSpan);
            //    TimeSpan.TryParse(
            //        vendingpoint.Pause_Sonntag_Beginn,
            //        CultureInfo.InvariantCulture,
            //        out endsundaynoon
            //    );

            //    TimeSpan beginsundaynoon = default(TimeSpan);
            //    TimeSpan.TryParse(
            //        vendingpoint.Pause_Sonntag_Ende,
            //        CultureInfo.InvariantCulture,
            //        out beginsundaynoon
            //    );

            //    TimeSpan endsunday = default(TimeSpan);
            //    TimeSpan.TryParse(
            //        vendingpoint.Sonntag_Ende,
            //        CultureInfo.InvariantCulture,
            //        out endsunday
            //    );

            //    myoperationschedule.OperationScheduleTime = new List<OperationScheduleTime>();

            //    //Add openingtime for Weekday, Saturday and Sunday

            //    if (beginweekday != TimeSpan.Zero && endweekday != TimeSpan.Zero)
            //    {
            //        //If no pause
            //        if (beginweekdaynoon == TimeSpan.Zero || endweekdaynoon == TimeSpan.Zero)
            //        {
            //            OperationScheduleTime myoptime = new OperationScheduleTime();
            //            myoptime.Monday = true;
            //            myoptime.Tuesday = true;
            //            myoptime.Wednesday = true;
            //            myoptime.Thursday = true;
            //            //myoptime.Thursday = true;
            //            myoptime.Friday = true;
            //            myoptime.Saturday = false;
            //            myoptime.Sunday = false;

            //            myoptime.Timecode = 1;
            //            myoptime.State = 2;

            //            myoptime.Start = beginweekday;
            //            myoptime.End = endweekday;

            //            myoperationschedule.OperationScheduleTime.Add(myoptime);
            //        }
            //        else
            //        {
            //            OperationScheduleTime myoptime1 = new OperationScheduleTime();
            //            myoptime1.Monday = true;
            //            myoptime1.Tuesday = true;
            //            myoptime1.Wednesday = true;
            //            myoptime1.Thursday = true;
            //            //myoptime1.Thursday = true;
            //            myoptime1.Friday = true;
            //            myoptime1.Saturday = false;
            //            myoptime1.Sunday = false;

            //            myoptime1.Timecode = 1;
            //            myoptime1.State = 2;

            //            myoptime1.Start = beginweekday;
            //            myoptime1.End = endweekdaynoon;

            //            myoperationschedule.OperationScheduleTime.Add(myoptime1);

            //            OperationScheduleTime myoptime2 = new OperationScheduleTime();
            //            myoptime2.Monday = true;
            //            myoptime2.Tuesday = true;
            //            myoptime2.Wednesday = true;
            //            myoptime2.Thursday = true;
            //            //myoptime2.Thursday = true;
            //            myoptime2.Friday = true;
            //            myoptime2.Saturday = false;
            //            myoptime2.Sunday = false;

            //            myoptime2.Timecode = 1;
            //            myoptime2.State = 2;

            //            myoptime2.Start = beginweekdaynoon;
            //            myoptime2.End = endweekday;

            //            myoperationschedule.OperationScheduleTime.Add(myoptime2);
            //        }
            //    }
            //    //SATURDAY
            //    if (beginsaturday != TimeSpan.Zero && endsaturday != TimeSpan.Zero)
            //    {
            //        //If no pause
            //        if (beginsaturdaynoon == TimeSpan.Zero || endsaturdaynoon == TimeSpan.Zero)
            //        {
            //            OperationScheduleTime myoptime = new OperationScheduleTime();
            //            myoptime.Monday = false;
            //            myoptime.Tuesday = false;
            //            myoptime.Wednesday = false;
            //            myoptime.Thursday = false;
            //            //myoptime.Thursday = false;
            //            myoptime.Friday = false;
            //            myoptime.Saturday = true;
            //            myoptime.Sunday = false;

            //            myoptime.Timecode = 1;
            //            myoptime.State = 2;

            //            myoptime.Start = beginsaturday;
            //            myoptime.End = endsaturday;

            //            myoperationschedule.OperationScheduleTime.Add(myoptime);
            //        }
            //        else
            //        {
            //            OperationScheduleTime myoptime1 = new OperationScheduleTime();
            //            myoptime1.Monday = false;
            //            myoptime1.Tuesday = false;
            //            myoptime1.Wednesday = false;
            //            myoptime1.Thursday = false;
            //            //myoptime1.Thursday = false;
            //            myoptime1.Friday = false;
            //            myoptime1.Saturday = true;
            //            myoptime1.Sunday = false;

            //            myoptime1.Timecode = 1;
            //            myoptime1.State = 2;

            //            myoptime1.Start = beginsaturday;
            //            myoptime1.End = endsaturdaynoon;

            //            myoperationschedule.OperationScheduleTime.Add(myoptime1);

            //            OperationScheduleTime myoptime2 = new OperationScheduleTime();
            //            myoptime2.Monday = false;
            //            myoptime2.Tuesday = false;
            //            myoptime2.Wednesday = false;
            //            myoptime2.Thursday = false;
            //            //myoptime2.Thursday = false;
            //            myoptime2.Friday = false;
            //            myoptime2.Saturday = true;
            //            myoptime2.Sunday = false;

            //            myoptime2.Timecode = 1;
            //            myoptime2.State = 2;

            //            myoptime2.Start = beginsaturdaynoon;
            //            myoptime2.End = endsaturday;

            //            myoperationschedule.OperationScheduleTime.Add(myoptime2);
            //        }
            //    }

            //    if (beginsunday != TimeSpan.Zero && endsunday != TimeSpan.Zero)
            //    {
            //        //If no pause
            //        if (beginsaturdaynoon == TimeSpan.Zero || endsaturdaynoon == TimeSpan.Zero)
            //        {
            //            OperationScheduleTime myoptime = new OperationScheduleTime();
            //            myoptime.Monday = false;
            //            myoptime.Tuesday = false;
            //            myoptime.Wednesday = false;
            //            myoptime.Thursday = false;
            //            //myoptime.Thursday = false;
            //            myoptime.Friday = false;
            //            myoptime.Saturday = true;
            //            myoptime.Sunday = false;

            //            myoptime.Timecode = 1;
            //            myoptime.State = 2;

            //            myoptime.Start = beginsunday;
            //            myoptime.End = endsunday;

            //            myoperationschedule.OperationScheduleTime.Add(myoptime);
            //        }
            //        else
            //        {
            //            OperationScheduleTime myoptime1 = new OperationScheduleTime();
            //            myoptime1.Monday = false;
            //            myoptime1.Tuesday = false;
            //            myoptime1.Wednesday = false;
            //            myoptime1.Thursday = false;
            //            //myoptime1.Thursday = false;
            //            myoptime1.Friday = false;
            //            myoptime1.Saturday = false;
            //            myoptime1.Sunday = true;

            //            myoptime1.Timecode = 1;
            //            myoptime1.State = 2;

            //            myoptime1.Start = beginsunday;
            //            myoptime1.End = endsundaynoon;

            //            myoperationschedule.OperationScheduleTime.Add(myoptime1);

            //            OperationScheduleTime myoptime2 = new OperationScheduleTime();
            //            myoptime2.Monday = false;
            //            myoptime2.Tuesday = false;
            //            myoptime2.Wednesday = false;
            //            myoptime2.Thursday = false;
            //            //myoptime2.Thursday = false;
            //            myoptime2.Friday = false;
            //            myoptime2.Saturday = false;
            //            myoptime2.Sunday = true;

            //            myoptime2.Timecode = 1;
            //            myoptime2.State = 2;

            //            myoptime2.Start = beginsundaynoon;
            //            myoptime2.End = endsunday;

            //            myoperationschedule.OperationScheduleTime.Add(myoptime2);
            //        }
            //    }

            //    //Exceptions, Closed on a certain day
            //    if (!String.IsNullOrEmpty(vendingpoint.Zusatzinfo_EN))
            //    {
            //        if (vendingpoint.Zusatzinfo_EN.EndsWith("closed"))
            //        {
            //            //Find out the day
            //            var myday = vendingpoint.Zusatzinfo_EN.Replace(" closed", "");

            //            //var validdays = new List<string>() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            //            var validdays = new List<string>()
            //            {
            //                "Monday",
            //                "Tuesday",
            //                "Wednesday",
            //                "Thursday",
            //                "Friday",
            //            }; //Excluding Saturday and Sunday because they can set separately

            //            if (validdays.Contains(myday))
            //            {
            //                //Add closed Schedule
            //                OperationScheduleTime myoptimeclosed = new OperationScheduleTime();
            //                myoptimeclosed.Monday = myday == "Monday" ? true : false;
            //                myoptimeclosed.Tuesday = myday == "Tuesday" ? true : false;
            //                myoptimeclosed.Wednesday = myday == "Wednesday" ? true : false;
            //                myoptimeclosed.Thursday = myday == "Thursday" ? true : false;
            //                //myoptimeclosed.Thursday = myday == "Thursday" ? true : false;
            //                myoptimeclosed.Friday = myday == "Friday" ? true : false;
            //                myoptimeclosed.Saturday = myday == "Saturday" ? true : false;
            //                myoptimeclosed.Sunday = myday == "Sunday" ? true : false;

            //                myoptimeclosed.Timecode = 1;
            //                myoptimeclosed.State = 1;

            //                myoptimeclosed.Start = new TimeSpan(0, 0, 1);
            //                myoptimeclosed.End = new TimeSpan(23, 59, 59);

            //                myoperationschedule.OperationScheduleTime.Add(myoptimeclosed);
            //            }
            //        }
            //    }

            //    return myoperationschedule;
            //}
            //else
                return null;
        }
    }
}
