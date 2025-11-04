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
    public class AnnouncementHelper
    {
        public List<string> idlist;
        public List<string> sourcelist;
        public List<string> languagelist;
        public DateTime? begin;
        public DateTime? end;
        public IDictionary<string, List<string>> tagdict;

        //New Publishedonlist

        public static async Task<AnnouncementHelper> CreateAsync(
            QueryFactory queryFactory,
            string? idfilter,
            string? languagefilter,
            string? sourcefilter,
            string? tagfilter,
            string? begindate,
            string? enddate,
            CancellationToken cancellationToken
        )
        {            
            return new AnnouncementHelper(
                idfilter,
                languagefilter,
                sourcefilter,
                tagfilter,
                begindate,
                enddate
            );
        }

        private AnnouncementHelper(
            string? idfilter,
            string? languagefilter,
            string? sourcefilter,
            string? tagfilter,
            string? begindate,
            string? enddate
        )
        {   
            // announcements id are forced to be lowercase
            idlist = Helper.CommonListCreator.CreateIdList(idfilter?.ToLower());
            sourcelist = Helper.CommonListCreator.CreateSourceList(sourcefilter);
            languagelist = Helper.CommonListCreator.CreateIdList(languagefilter);
            //tagfilter
            tagdict = GenericHelper.RetrieveTagFilter(tagfilter);

            begin = DateTime.MinValue;
            end = DateTime.MaxValue;

            if (!String.IsNullOrEmpty(begindate))
                if (begindate != "null")
                    begin = Convert.ToDateTime(begindate);

            if (!String.IsNullOrEmpty(enddate))
                if (enddate != "null")
                    end = Convert.ToDateTime(enddate);
        }
    }
}
