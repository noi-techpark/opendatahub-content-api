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
    public class UrbanGreenHelper
    {
        public List<string> idlist;
        public List<string> sourcelist;
        public List<string> languagelist;
        public List<string> greencodelist;
        public List<string> greencodeversionlist;
        public List<string> greencodetypelist;
        public List<string> greencodesubtypelist;
        public bool? activefilter;
        public IDictionary<string, List<string>> tagdict;

        public static async Task<UrbanGreenHelper> CreateAsync(
            QueryFactory queryFactory,
            string? idfilter,
            string? languagefilter,
            string? sourcefilter,
            string? greencodefilter,
            string? greencodeversionfilter,
            string? greencodetypefilter,
            string? greencodesubtypefilter,
            bool? activefilter,
            string? tagfilter,
            CancellationToken cancellationToken
        )
        {
            return new UrbanGreenHelper(
                idfilter,
                languagefilter,
                sourcefilter,
                greencodefilter,
                greencodeversionfilter,
                greencodetypefilter,
                greencodesubtypefilter,
                activefilter,
                tagfilter
            );
        }

        private UrbanGreenHelper(
            string? idfilter,
            string? languagefilter,
            string? sourcefilter,
            string? greencodefilter,
            string? greencodeversionfilter,
            string? greencodetypefilter,
            string? greencodesubtypefilter,
            bool? activefilter,
            string? tagfilter
        )
        {
            // urbangreen id are forced to be lowercase
            idlist = Helper.CommonListCreator.CreateIdList(idfilter?.ToLower());
            sourcelist = Helper.CommonListCreator.CreateSourceList(sourcefilter);
            languagelist = Helper.CommonListCreator.CreateIdList(languagefilter);
            greencodelist = Helper.CommonListCreator.CreateIdList(greencodefilter);
            greencodeversionlist = Helper.CommonListCreator.CreateIdList(greencodeversionfilter);
            greencodetypelist = Helper.CommonListCreator.CreateIdList(greencodetypefilter);
            greencodesubtypelist = Helper.CommonListCreator.CreateIdList(greencodesubtypefilter);
            this.activefilter = activefilter;
            //tagfilter
            tagdict = GenericHelper.RetrieveTagFilter(tagfilter);
        }
    }
}
