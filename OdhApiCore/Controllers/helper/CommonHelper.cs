// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Helper;
using SqlKata.Execution;

namespace OdhApiCore.Controllers
{
    public class CommonHelper
    {
        public List<string> idlist;
        public List<string> languagelist;
        public List<string> smgtaglist;
        public List<string> sourcelist;
        public bool? visibleinsearch;
        public string? lastchange;
        public bool? active;
        public bool? smgactive;
        public IDictionary<string, List<string>> tagdict;

        //New Publishedonlist
        public List<string> publishedonlist;

        public static Task<CommonHelper> CreateAsync(
            QueryFactory queryFactory,
            string? idfilter,
            string? languagefilter,
            bool? visibleinsearch,
            string? tagfilter,
            string? sourcefilter,
            bool? activefilter,
            bool? smgactivefilter,
            string? smgtags,
            string? lastchange,
            string? publishedonfilter,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(
                new CommonHelper(
                    idfilter: idfilter,
                    languagefilter: languagefilter,
                    activefilter: activefilter,
                    smgactivefilter: smgactivefilter,
                    visibleinsearch: visibleinsearch,
                    sourcefilter: sourcefilter,
                    smgtags: smgtags,
                    tagfilter: tagfilter,
                    lastchange: lastchange,
                    publishedonfilter: publishedonfilter
                )
            );
        }

        private CommonHelper(
            string? idfilter,
            string? languagefilter,
            bool? activefilter,
            bool? smgactivefilter,
            bool? visibleinsearch,
            string? sourcefilter,
            string? smgtags,
            string? tagfilter,
            string? lastchange,
            string? publishedonfilter
        )
        {
            idlist = Helper.CommonListCreator.CreateIdList(idfilter?.ToUpper());

            languagelist = Helper.CommonListCreator.CreateIdList(languagefilter?.ToLower());
            sourcelist = Helper.CommonListCreator.CreateIdList(sourcefilter);

            smgtaglist = CommonListCreator.CreateIdList(smgtags);
            //active
            active = activefilter;

            //smgactive
            smgactive = smgactivefilter;

            //smgactive
            this.visibleinsearch = visibleinsearch;

            this.lastchange = lastchange;

            publishedonlist = Helper.CommonListCreator.CreateIdList(publishedonfilter?.ToLower());

            tagdict = GenericHelper.RetrieveTagFilter(tagfilter);
        }
    }
}
