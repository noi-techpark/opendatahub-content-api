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
    public class TripHelper
    {
        public List<string> idlist;
        public List<string> sourcelist;
        public List<string> languagelist;
        public DateTimeOffset? begin;
        public DateTimeOffset? end;
        public IDictionary<string, List<string>> tagdict;

        //New Publishedonlist

        public static async Task<TripHelper> CreateAsync(
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
            return new TripHelper(
                idfilter,
                languagefilter,
                sourcefilter,
                tagfilter,
                begindate,
                enddate
            );
        }

        private TripHelper(
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

            begin = DateTimeOffset.MinValue;
            end = DateTimeOffset.MaxValue;

            if (!String.IsNullOrEmpty(begindate) && begindate != "null")
            {
                if (DateTimeOffset.TryParse(begindate, out DateTimeOffset parsedBegin))
                {
                    begin = parsedBegin;
                }
            }

            if (!String.IsNullOrEmpty(enddate) && enddate != "null")
            {
                if (DateTimeOffset.TryParse(enddate, out DateTimeOffset parsedEnd))
                {
                    end = parsedEnd;
                }
            }
        }
    }
}
