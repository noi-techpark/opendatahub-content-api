// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Helper;
using Helper.Generic;
using SqlKata.Execution;

namespace OdhApiCore.Controllers.api
{
    public class RoadIncidentHelper
    {
        public List<string> idlist;
        public List<string> sourcelist;
        public List<string> languagelist;
        public bool? active;
        public string? lastchange;
        public DateTime? begin;
        public DateTime? end;
        public IDictionary<string, List<string>> tagdict;

        //New Publishedonlist
        public List<string> publishedonlist;

        public static async Task<RoadIncidentHelper> CreateAsync(
            QueryFactory queryFactory,
            string? idfilter,
            string? languagefilter,
            string? sourcefilter,
            bool? activefilter,
            string? lastchange,
            string? tagfilter,
            string? publishedonfilter,
            string? begindate,
            string? enddate,
            CancellationToken cancellationToken
        )
        {            
            return new RoadIncidentHelper(
                idfilter,
                languagefilter,
                sourcefilter,
                activefilter,
                tagfilter,
                publishedonfilter,
                lastchange,
                begindate,
                enddate
            );
        }

        private RoadIncidentHelper(
            string? idfilter,
            string? languagefilter,
            string? sourcefilter,
            bool? activefilter,
            string? tagfilter,
            string? publishedonfilter,
            string? lastchange,
            string? begindate,
            string? enddate
        )
        {            
            idlist = Helper.CommonListCreator.CreateIdList(idfilter?.ToUpper());
            sourcelist = Helper.CommonListCreator.CreateSourceList(sourcefilter);
            languagelist = Helper.CommonListCreator.CreateIdList(languagefilter);
            //active
            active = activefilter;
            //smgactive
            publishedonlist = Helper.CommonListCreator.CreateIdList(publishedonfilter?.ToLower());
            //tagfilter
            tagdict = GenericHelper.RetrieveTagFilter(tagfilter);

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
        }
    }
}
