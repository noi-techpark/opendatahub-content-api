// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;

namespace OdhApiCore.Controllers.api
{
    public class EventShortHelper
    {
        public DateTime start;
        public DateTime end;
        public string? datetimeformat;
        public List<string> sourcelist;
        public List<string> eventlocationlist;
        public List<string> webaddresslist;
        public List<string> idlist;
        public List<string> languagelist;
        public string? todayactivefilter;
        public bool? websiteactivefilter;
        public bool? communityactivefilter;
        public bool activefilter;
        public string? lastchange;
        public string? sortorder;

        //New Publishedonlist
        public List<string> publishedonlist;
        public IDictionary<string, List<string>> tagdict;
        public static EventShortHelper Create(
            string? startdate,
            string? enddate,
            string? datetimeformat,
            string? source,
            string? tagfilter,
            string? eventlocation,
            bool? todayactive,
            bool? websiteactive,
            bool? communityactive,
            bool active,
            string? eventids,
            string? webaddress,
            string? lastchange,
            string? langfilter,
            string? sortorder,
            string? publishedonfilter
        )
        {
            return new EventShortHelper(
                startdate,
                enddate,
                datetimeformat,
                source,
                tagfilter,
                eventlocation,
                todayactive,
                websiteactive,
                communityactive,
                active,
                eventids,
                webaddress,
                lastchange,
                languagefilter: langfilter,
                sortorder,
                publishedonfilter
            );
        }

        private EventShortHelper(
            string? startdate,
            string? enddate,
            string? datetimeformat,
            string? source,
            string? tagfilter,
            string? eventlocation,
            bool? todayactive,
            bool? websiteactive,
            bool? communityactive,
            bool active,
            string? eventids,
            string? webaddress,
            string? lastchange,
            string? languagefilter,
            string? sortorder,
            string? publishedonfilter
        )
        {
            idlist = Helper.CommonListCreator.CreateIdList(eventids);
            var sourcelisttemp = Helper.CommonListCreator.CreateIdList(source);

            sourcelist = ExtendSourceFilterEventShort(sourcelisttemp);

            eventlocationlist = Helper.CommonListCreator.CreateIdList(eventlocation);
            webaddresslist = Helper.CommonListCreator.CreateIdList(webaddress);

            start = DateTime.MinValue;
            end = DateTime.MaxValue;

            if (String.IsNullOrEmpty(datetimeformat))
            {
                if (!String.IsNullOrEmpty(startdate))
                    start = Convert.ToDateTime(startdate);
                if (!String.IsNullOrEmpty(enddate))
                {
                    end = Convert.ToDateTime(enddate);
                    if (end.TimeOfDay == new TimeSpan(0, 0, 0))
                    {
                        end = end.AddDays(1);
                    }
                }
            }
            else if (datetimeformat == "uxtimestamp")
            {
                if (!String.IsNullOrEmpty(startdate))
                {
                    double startdatedb = Convert.ToDouble(startdate);
                    start = Helper.DateTimeHelper.UnixTimeStampToDateTimeMilliseconds(startdatedb);
                }

                if (!String.IsNullOrEmpty(enddate))
                {
                    double enddatedb = Convert.ToDouble(enddate);

                    end = Helper.DateTimeHelper.UnixTimeStampToDateTimeMilliseconds(enddatedb);
                    if (end.TimeOfDay == new TimeSpan(0, 0, 0))
                    {
                        end = end.AddDays(1);
                    }
                }
            }

            if (todayactive == true)
                todayactivefilter = "Y";
            else if (todayactive == false)
                todayactivefilter = "N";
            else
                todayactivefilter = null;

            websiteactivefilter = websiteactive;
            communityactivefilter = communityactive;
            activefilter = active;

            this.sortorder = sortorder;

            if (sortorder != null)
            {
                sortorder = sortorder.ToUpper();

                if (sortorder != "ASC" && sortorder != "DESC")
                    sortorder = "";
            }

            languagelist = Helper.CommonListCreator.CreateIdList(languagefilter);

            this.lastchange = lastchange;
            publishedonlist = Helper.CommonListCreator.CreateIdList(publishedonfilter?.ToLower());

            tagdict = GenericHelper.RetrieveTagFilter(tagfilter);
        }

        private List<string> ExtendSourceFilterEventShort(List<string> sourcelist)
        {
            List<string> sourcelistnew = new();

            foreach (var source in sourcelist)
            {
                sourcelistnew.Add(source);

                if (source == "noi")
                {
                    if (!sourcelistnew.Contains("content"))
                        sourcelistnew.Add("content");
                }
                else if (source == "eurac")
                {
                    if (!sourcelistnew.Contains("ebms"))
                        sourcelistnew.Add("ebms");
                }
            }

            return sourcelistnew;
        }
    }
}
