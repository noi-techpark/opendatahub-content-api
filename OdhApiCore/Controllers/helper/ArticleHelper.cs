// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;

namespace OdhApiCore.Controllers.api
{
    public class ArticleHelper
    {
        public List<string> typelist;
        public List<string> subtypelist;
        public List<string> idlist;
        public List<string> smgtaglist;
        public List<string> languagelist;
        public bool? highlight;
        public bool? active;
        public bool? smgactive;
        public string? lastchange;
        public DateTime? articledate;
        public DateTime? articledateto;

        //New Publishedonlist
        public List<string> publishedonlist;
        public List<string> sourcelist;
        public IDictionary<string, List<string>> tagdict;

        public static ArticleHelper Create(
            string? typefilter,
            string? subtypefilter,
            string? idfilter,
            string? languagefilter,
            bool? highlightfilter,
            bool? activefilter,
            bool? smgactivefilter,
            string? smgtags,
            string? tagfilter,
            string? articledate,
            string? articledateto,
            string? source,
            string? lastchange,
            string? publishedonfilter
        )
        {
            return new ArticleHelper(
                typefilter,
                subtypefilter,
                idfilter,
                languagefilter,
                highlightfilter,
                activefilter,
                smgactivefilter,                
                smgtags,
                tagfilter,
                articledate,
                articledateto,
                source,
                lastchange,
                publishedonfilter
            );
        }

        private ArticleHelper(
            string? typefilter,
            string? subtypefilter,
            string? idfilter,
            string? languagefilter,
            bool? highlightfilter,
            bool? activefilter,
            bool? smgactivefilter,
            string? smgtags,
            string? tagfilter,
            string? articledate,
            string? articledateto,
            string? source,
            string? lastchange,
            string? publishedonfilter
        )
        {
            typelist = new List<string>();
            int typeinteger = 0;

            if (!String.IsNullOrEmpty(typefilter))
            {
                if (int.TryParse(typefilter, out typeinteger))
                {
                    //Sonderfall wenn alles abgefragt wird um keine unnötige Where zu erzeugen
                    if (typeinteger != 2047)
                        typelist = Helper.ArticleListCreator.CreateArticleTypefromFlag(typefilter);
                }
                else
                {
                    typelist.AddRange(Helper.CommonListCreator.CreateIdList(typefilter));
                }
            }

            int subtypeinteger = 0;

            if (!String.IsNullOrEmpty(subtypefilter))
            {
                if (subtypefilter != "null")
                {
                    if (int.TryParse(subtypefilter, out subtypeinteger))
                    {
                        subtypelist = Helper.ArticleListCreator.CreateArticleSubTypefromFlag(
                            typelist.FirstOrDefault(),
                            subtypefilter
                        );
                    }
                    else
                    {
                        subtypelist = Helper.ArticleListCreator.CreateArticleSubTypeList(
                            typelist.FirstOrDefault(),
                            subtypefilter
                        );
                    }
                }
                else
                    subtypelist = new List<string>();
            }
            else
                subtypelist = new List<string>();

            idlist = Helper.CommonListCreator.CreateIdList(idfilter?.ToUpper());
            languagelist = Helper.CommonListCreator.CreateIdList(languagefilter);
            sourcelist = Helper.CommonListCreator.CreateSourceList(source);
            smgtaglist = Helper.CommonListCreator.CreateIdList(smgtags);

            //highlight
            highlight = highlightfilter;
            //active
            active = activefilter;
            //smgactive
            smgactive = smgactivefilter;

            this.lastchange = lastchange;

            this.articledate = DateTime.MinValue;
            this.articledateto = DateTime.MaxValue;

            if (!String.IsNullOrEmpty(articledate))
                if (articledate != "null")
                    this.articledate = Convert.ToDateTime(articledate);

            if (!String.IsNullOrEmpty(articledateto))
                if (articledateto != "null")
                    this.articledateto = Convert.ToDateTime(articledateto);

            publishedonlist = Helper.CommonListCreator.CreateIdList(publishedonfilter?.ToLower());

            tagdict = GenericHelper.RetrieveTagFilter(tagfilter);
        }
    }
}
