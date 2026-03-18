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
    public class SpatialDataHelper
    {
        public List<string> idlist;
        public List<string> sourcelist;
        public List<string> languagelist;
        public IDictionary<string, List<string>> tagdict;

        //New Publishedonlist

        public static async Task<SpatialDataHelper> CreateAsync(
            QueryFactory queryFactory,
            string? idfilter,
            string? languagefilter,
            string? sourcefilter,
            string? tagfilter,
            CancellationToken cancellationToken
        )
        {            
            return new SpatialDataHelper(
                idfilter,
                languagefilter,
                sourcefilter,
                tagfilter
            );
        }

        private SpatialDataHelper(
            string? idfilter,
            string? languagefilter,
            string? sourcefilter,
            string? tagfilter
        )
        {   
            // announcements id are forced to be lowercase
            idlist = Helper.CommonListCreator.CreateIdList(idfilter?.ToLower());
            sourcelist = Helper.CommonListCreator.CreateSourceList(sourcefilter);
            languagelist = Helper.CommonListCreator.CreateIdList(languagefilter);
            //tagfilter
            tagdict = GenericHelper.RetrieveTagFilter(tagfilter);
        }
    }
}
