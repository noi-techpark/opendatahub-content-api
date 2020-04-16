﻿using Helper;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiCore.Controllers.api
{
    public class PoiHelper
    {
        public List<string> poitypelist;
        public List<string> subtypelist;
        public List<string> idlist;
        public List<string> arealist;
        public List<string> smgtaglist;        
        public List<string> tourismvereinlist;
        public List<string> regionlist;
        public bool? highlight;
        public bool? active;
        public bool? smgactive;
        public string? lastchange;
    
        public static async Task<PoiHelper> CreateAsync(
        IPostGreSQLConnectionFactory connectionFactory, string? poitype, string? subtypefilter, string? idfilter, string? locfilter,
        string? areafilter, bool? highlightfilter, bool? activefilter, bool? smgactivefilter,
        string? smgtags, string? lastchange, CancellationToken cancellationToken, QueryFactory queryFactory)
        {
            var arealist = await GenericHelper.RetrieveAreaFilterDataAsync(queryFactory, areafilter, cancellationToken);

            IEnumerable<string>? tourismusvereinids = null;
            if (locfilter != null && locfilter.Contains("mta"))
            {
                List<string> metaregionlist = CommonListCreator.CreateDistrictIdList(locfilter, "mta");
                tourismusvereinids = await GenericHelper.RetrieveLocFilterDataAsync(queryFactory, metaregionlist, cancellationToken);
            }

            return new PoiHelper(
                poitype, subtypefilter, idfilter, locfilter, arealist, highlightfilter, activefilter, smgactivefilter, smgtags, lastchange, tourismusvereinids);
        }

        private PoiHelper(
            string? poitype, string? subtypefilter, string? idfilter, string? locfilter, IEnumerable<string> arealist,
            bool? highlightfilter, bool? activefilter, bool? smgactivefilter, string? smgtags, string? lastchange, IEnumerable<string>? tourismusvereinids)
        {
            poitypelist = new List<string>();
            if (poitype != null)
            {
                if (int.TryParse(poitype, out int typeinteger))
                {
                    //Sonderfall wenn alles abgefragt wird um keine unnötige Where zu erzeugen
                    if (typeinteger != 511)
                        poitypelist = Helper.ActivityPoiListCreator.CreatePoiTypefromFlag(poitype);
                }
                else
                {
                    poitypelist.Add(poitype);
                }
            }

            if (poitypelist.Count > 0)
                subtypelist = Helper.ActivityPoiListCreator.CreatePoiSubTypefromFlag(poitypelist.FirstOrDefault(), subtypefilter);
            else
                subtypelist = new List<string>();


            idlist = Helper.CommonListCreator.CreateIdList(idfilter?.ToUpper());

            this.arealist = arealist.ToList();

            smgtaglist = Helper.CommonListCreator.CreateIdList(smgtags);

            tourismvereinlist = new List<string>();
            regionlist = new List<string>();

            if (locfilter != null && locfilter.Contains("reg"))
                regionlist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "reg");
            if (locfilter != null && locfilter.Contains("tvs"))
                tourismvereinlist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "tvs");

            if (tourismusvereinids != null)
                tourismvereinlist.AddRange(tourismusvereinids);

            //highlight
            highlight = highlightfilter;
            //active
            active = activefilter;           
            //smgactive
            smgactive = smgactivefilter;

            this.lastchange = lastchange;
        }
    }
}
