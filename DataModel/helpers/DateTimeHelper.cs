// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace DataModel.helpers
{
    public class DateTimeHelper
    {
        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            var tzinfo = TZConvert.GetTimeZoneInfo("W. Europe Standard Time");

            return (
                TimeZoneInfo.ConvertTimeToUtc(dateTime, tzinfo)
                - new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)
            ).TotalSeconds;
        }

        public static double DateTimeToUnixTimestampMilliseconds(DateTime dateTime)
        {
            var tzinfo = TZConvert.GetTimeZoneInfo("W. Europe Standard Time");

            return (
                TimeZoneInfo.ConvertTimeToUtc(dateTime, tzinfo)
                - new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)
            ).TotalMilliseconds;
        }

        ////TEST without conversion to UTC simply convert to UTC what is passed
        //public static double DateTimeToUnixTimestampMillisecondsWithoutUTCConvert(DateTime dateTime)
        //{
        //    return (dateTime -
        //           new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalMilliseconds;
        //}

        //TODO UnixTimeStampToDateTime

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            var tzinfo = TZConvert.GetTimeZoneInfo("W. Europe Standard Time");

            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(
                1970,
                1,
                1,
                0,
                0,
                0,
                0,
                System.DateTimeKind.Utc
            );
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToUniversalTime();

            return TimeZoneInfo.ConvertTimeFromUtc(dtDateTime, tzinfo);
        }

        public static DateTime UnixTimeStampToDateTimeMilliseconds(double unixTimeStamp)
        {
            var tzinfo = TZConvert.GetTimeZoneInfo("W. Europe Standard Time");

            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(
                1970,
                1,
                1,
                0,
                0,
                0,
                0,
                System.DateTimeKind.Utc
            );
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToUniversalTime();
            return TimeZoneInfo.ConvertTimeFromUtc(dtDateTime, tzinfo);
        }

        public static bool CompareValidStartEnddate(DateTime start, DateTime end)
        {
            return end >= start;
        }
    }
}
