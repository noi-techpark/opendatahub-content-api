// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Helper;
using SqlKata.Execution;

namespace OdhApiCore.Controllers
{
    public class EventHelper
    {
        public List<string> idlist;
        public List<string> orgidlist;
        public List<Int32> rancidlist;
        public List<string> topicrids;
        public List<string> smgtaglist;
        public List<string> districtlist;
        public List<string> municipalitylist;
        public List<string> tourismvereinlist;
        public List<string> regionlist;
        public List<string> languagelist;
        public List<string> sourcelist;
        public bool? active;
        public bool? smgactive;
        public DateTime? begin;
        public DateTime? end;
        public string? lastchange;

        //New Publishedonlist
        public List<string> publishedonlist;
        public IDictionary<string, List<string>> tagdict;

        public static async Task<EventHelper> CreateAsync(
            QueryFactory queryFactory,
            string? idfilter,
            string? locfilter,
            string? rancfilter,
            string? topicfilter,
            string? orgfilter,
            string? begindate,
            string? enddate,
            bool? activefilter,
            bool? smgactivefilter,
            string? smgtags,
            string? tagfilter,
            string? lastchange,
            string? langfilter,
            string? source,
            string? publishedonfilter,
            CancellationToken cancellationToken
        )
        {
            IEnumerable<string>? tourismusvereinids = null;
            if (locfilter != null && locfilter.Contains("mta"))
            {
                List<string> metaregionlist = CommonListCreator.CreateDistrictIdList(
                    locfilter,
                    "mta"
                );
                tourismusvereinids = await GenericHelper.RetrieveLocFilterDataAsync(
                    queryFactory,
                    metaregionlist,
                    cancellationToken
                );
            }

            return new EventHelper(
                idfilter: idfilter,
                locfilter: locfilter,
                rancfilter: rancfilter,
                topicfilter: topicfilter,
                orgfilter: orgfilter,
                begindate: begindate,
                enddate: enddate,
                activefilter: activefilter,
                smgactivefilter: smgactivefilter,
                smgtags: smgtags,
                tagfilter: tagfilter,
                lastchange: lastchange,
                sourcefilter: source,
                languagefilter: langfilter,
                publishedonfilter: publishedonfilter,
                tourismusvereinids: tourismusvereinids
            );
        }

        private EventHelper(
            string? idfilter,
            string? locfilter,
            string? rancfilter,
            string? topicfilter,
            string? orgfilter,
            string? begindate,
            string? enddate,
            bool? activefilter,
            bool? smgactivefilter,
            string? smgtags,
            string? tagfilter,
            string? lastchange,
            string? languagefilter,
            string? sourcefilter,
            string? publishedonfilter,
            IEnumerable<string>? tourismusvereinids
        )
        {
            idlist = CommonListCreator.CreateIdList(idfilter?.ToUpper());

            smgtaglist = CommonListCreator.CreateIdList(smgtags);
            sourcelist = Helper.CommonListCreator.CreateSourceList(sourcefilter);
            languagelist = Helper.CommonListCreator.CreateIdList(languagefilter);

            orgidlist = CommonListCreator.CreateIdList(orgfilter);
            rancidlist = CommonListCreator.CreateNumericIdList(rancfilter);

            topicrids = EventListCreator.CreateEventTopicRidListfromFlag(topicfilter);

            tourismvereinlist = new List<string>();
            regionlist = new List<string>();
            municipalitylist = new List<string>();
            districtlist = new List<string>();

            if (locfilter != null && locfilter.Contains("reg"))
                regionlist = CommonListCreator.CreateDistrictIdList(locfilter, "reg");
            if (locfilter != null && locfilter.Contains("tvs"))
                tourismvereinlist = CommonListCreator.CreateDistrictIdList(locfilter, "tvs");
            if (locfilter != null && locfilter.Contains("mun"))
                municipalitylist = CommonListCreator.CreateDistrictIdList(locfilter, "mun");
            if (locfilter != null && locfilter.Contains("fra"))
                districtlist = CommonListCreator.CreateDistrictIdList(locfilter, "fra");

            if (tourismusvereinids != null)
                tourismvereinlist.AddRange(tourismusvereinids);

            //active
            active = activefilter;

            //smgactive
            smgactive = smgactivefilter;

            this.lastchange = lastchange;

            begin = DateTime.MinValue;
            end = DateTime.MaxValue;

            if (!String.IsNullOrEmpty(begindate))
                if (begindate != "null")
                    begin = Convert.ToDateTime(begindate);

            if (!String.IsNullOrEmpty(enddate))
                if (enddate != "null")
                    end = Convert.ToDateTime(enddate);

            publishedonlist = Helper.CommonListCreator.CreateIdList(publishedonfilter?.ToLower());

            tagdict = GenericHelper.RetrieveTagFilter(tagfilter);
        }

        public static string? GetEventSortExpression(
            string? sort,
            string? begindate,
            string? enddate,
            ref string? seed
        )
        {
            string? sortifseednull = null;

            if (sort != null)
            {
                //simple sort bz datebegin
                if (sort.ToLower() == "asc")
                    sortifseednull = "gen_nextbegindate ASC";
                else if (sort.ToLower() == "desc")
                    sortifseednull = "gen_nextbegindate DESC";
                else if (sort.ToLower().StartsWith("upcoming"))
                {
                    var sortfromdate = "2000-01-01";

                    if (begindate != null)
                        sortfromdate = begindate;
                    else if (enddate != null)
                        sortfromdate = enddate;

                    //TO CHECK Events with Eventdate interval vs singledays, how do we sort here?
                    if (sort.ToLower() == "upcoming")
                    {
                        sortifseednull =
                            "get_nearest_tsrange_distance(gen_eventdatearray, ('"
                            + sortfromdate
                            + "')::timestamp, 'asc', false),lower(get_nearest_tsrange(gen_eventdatearray, ('"
                            + sortfromdate
                            + "')::timestamp))";

                        //if (sort.ToLower() == "asc")
                        //    sortifseednull = "get_nearest_tsrange_distance(gen_eventdatearray, ('" + sortfromdate + "')::timestamp, 'asc'),get_nearest_tsrange(gen_eventdatearray, ('" + sortfromdate + "')::timestamp) DESC";
                        //else
                        //    sortifseednull = "get_nearest_tsrange_distance(gen_eventdatearray, ('" + sortfromdate + "')::timestamp, 'desc') DESC,get_nearest_tsrange(gen_eventdatearray, ('" + sortfromdate + "')::timestamp) ASC";
                    }
                    if (sort.ToLower() == "upcomingspecial")
                    {
                        sortifseednull =
                            "get_nearest_tsrange_distance(gen_eventdatearray, ('"
                            + sortfromdate
                            + "')::timestamp, 'asc', true),lower(get_nearest_tsrange(gen_eventdatearray, ('"
                            + sortfromdate
                            + "')::timestamp))";

                        //if (sort.ToLower() == "asc")
                        //    sortifseednull = "get_nearest_tsrange_distance(gen_eventdatearray, ('" + sortfromdate + "')::timestamp, 'asc', true),get_nearest_tsrange(gen_eventdatearray, ('" + sortfromdate + "')::timestamp) DESC";
                        //else
                        //    sortifseednull = "get_nearest_tsrange_distance(gen_eventdatearray, ('" + sortfromdate + "')::timestamp, 'desc', true) DESC,get_nearest_tsrange(gen_eventdatearray, ('" + sortfromdate + "')::timestamp) ASC";
                    }
                    if (sort.ToLower() == "upcomingspecial")
                    {
                        sortifseednull =
                            "get_nearest_tsrange_distance(gen_eventdatearray, ('"
                            + sortfromdate
                            + "')::timestamp, 'asc', true),lower(get_nearest_tsrange(gen_eventdatearray, ('"
                            + sortfromdate
                            + "')::timestamp))";

                        //if (sort.ToLower() == "asc")
                        //    sortifseednull = "get_nearest_tsrange_distance(gen_eventdatearray, ('" + sortfromdate + "')::timestamp, 'asc', true),get_nearest_tsrange(gen_eventdatearray, ('" + sortfromdate + "')::timestamp) DESC";
                        //else
                        //    sortifseednull = "get_nearest_tsrange_distance(gen_eventdatearray, ('" + sortfromdate + "')::timestamp, 'desc', true) DESC,get_nearest_tsrange(gen_eventdatearray, ('" + sortfromdate + "')::timestamp) ASC";
                    }
                    if (sort.ToLower() == "upcomingspecialdesc")
                    {
                        sortifseednull =
                            "get_nearest_tsrange_distance(gen_eventdatearray, ('"
                            + sortfromdate
                            + "')::timestamp, 'asc', true),lower(get_nearest_tsrange(gen_eventdatearray, ('"
                            + sortfromdate
                            + "')::timestamp)) DESC";

                        //if (sort.ToLower() == "asc")
                        //    sortifseednull = "get_nearest_tsrange_distance(gen_eventdatearray, ('" + sortfromdate + "')::timestamp, 'asc', true),get_nearest_tsrange(gen_eventdatearray, ('" + sortfromdate + "')::timestamp) DESC";
                        //else
                        //    sortifseednull = "get_nearest_tsrange_distance(gen_eventdatearray, ('" + sortfromdate + "')::timestamp, 'desc', true) DESC,get_nearest_tsrange(gen_eventdatearray, ('" + sortfromdate + "')::timestamp) ASC";
                    }
                }

                if (sortifseednull != null)
                    seed = null;

                //Set Rawfilter to this RTHOENI rawfilter can overwrite this ;)
                //rawfilter = sortifseednull;
            }

            return sortifseednull;
        }
    }
}
