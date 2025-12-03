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
    public class MeasuringPointsHelper
    {
        public List<string> idlist;
        public List<string> districtlist;
        public List<string> municipalitylist;
        public List<string> tourismvereinlist;
        public List<string> regionlist;
        public List<string> arealist;
        public List<string> skiarealist;
        public List<string> sourcelist;
        public bool? active;        
        public string? lastchange;

        //New Publishedonlist
        public List<string> publishedonlist;
        public IDictionary<string, List<string>> tagdict;

        public static async Task<MeasuringPointsHelper> Create(
            QueryFactory queryFactory,
            string? idfilter,
            string? locfilter,
            string? areafilter,
            string? skiareafilter,
            string? sourcefilter,
            bool? activefilter,
            string? tagfilter,
            string? lastchange,
            string? publishedonfilter,
            CancellationToken cancellationToken
        )
        {
            //TODO SKIAREAFILTER AND AREAFILTER not like on Activity Endpoint!

            //var arealistfromskiarea = await GenericHelper.RetrieveAreaFilterDataAsync(queryFactory, skiareafilter, cancellationToken);
            var arealistfromarea = await GenericHelper.RetrieveAreaFilterDataAsync(
                queryFactory,
                areafilter,
                cancellationToken
            );

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

            return new MeasuringPointsHelper(
                idfilter: idfilter,
                locfilter: locfilter,
                skiareafilter: skiareafilter,
                areafilterlist: arealistfromarea,
                sourcefilter: sourcefilter,
                activefilter: activefilter,
                tagfilter : tagfilter,
                lastchange: lastchange,
                publishedonfilter: publishedonfilter,
                tourismusvereinids: tourismusvereinids
            );
        }

        private MeasuringPointsHelper(
            string? idfilter,
            string? locfilter,
            string? skiareafilter,
            IEnumerable<string> areafilterlist,
            string? sourcefilter,
            bool? activefilter,
            string? tagfilter,
            string? lastchange,
            string? publishedonfilter,
            IEnumerable<string>? tourismusvereinids
        )
        {
            idlist = Helper.CommonListCreator.CreateIdList(idfilter?.ToUpper());

            sourcelist = Helper.CommonListCreator.CreateIdList(sourcefilter);

            arealist = new List<string>();

            if (areafilterlist != null)
                arealist.AddRange(areafilterlist);
            //if (skiareafilterlist != null)
            //    arealist.AddRange(skiareafilterlist);

            tourismvereinlist = new List<string>();
            regionlist = new List<string>();
            municipalitylist = new List<string>();
            districtlist = new List<string>();

            if (locfilter != null && locfilter.Contains("reg"))
                regionlist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "reg");
            if (locfilter != null && locfilter.Contains("tvs"))
                tourismvereinlist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "tvs");
            if (locfilter != null && locfilter.Contains("mun"))
                municipalitylist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "mun");
            if (locfilter != null && locfilter.Contains("fra"))
                districtlist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "fra");

            if (tourismusvereinids != null)
                tourismvereinlist.AddRange(tourismusvereinids);

            //active
            active = activefilter;            

            //skiarea filter
            skiarealist = Helper.CommonListCreator.CreateIdList(skiareafilter?.ToUpper());

            this.lastchange = lastchange;
            publishedonlist = Helper.CommonListCreator.CreateIdList(publishedonfilter?.ToLower());

            tagdict = GenericHelper.RetrieveTagFilter(tagfilter);
        }
    }
}
