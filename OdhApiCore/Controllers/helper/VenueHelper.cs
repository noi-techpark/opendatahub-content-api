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
    public class VenueHelper
    {
        public List<string> categorylist;
        public List<string> featurelist;
        public List<string> setuptypelist;
        public List<string> idlist;        
        public List<string> sourcelist;
        public List<string> languagelist;
        public List<string> districtlist;
        public List<string> municipalitylist;
        public List<string> tourismvereinlist;
        public List<string> regionlist;
        public bool? active;        
        public string? lastchange;

        //public bool capacity;
        //public int capacitymin;
        //public int capacitymax;

        //public bool roomcount;
        //public int roomcountmin;
        //public int roomcountmax;

        //New Publishedonlist
        public List<string> publishedonlist;
        public IDictionary<string, List<string>> tagdict;

        public static async Task<VenueHelper> CreateAsync(
            QueryFactory queryFactory,
            string? idfilter,
            string? categoryfilter,
            string? featurefilter,
            string? setuptypefilter,
            string? locfilter,            
            string? languagefilter,
            string? sourcefilter,
            bool? activefilter,                        
            string? tagfilter,
            string? lastchange,
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

            return new VenueHelper(
                idfilter,
                categoryfilter,
                featurefilter,
                setuptypefilter,
                locfilter,                                
                languagefilter,
                sourcefilter,
                activefilter,
                tagfilter,
                lastchange,
                publishedonfilter,
                tourismusvereinids
            );
        }

        private VenueHelper(
            string? idfilter,
            string? categoryfilter,
            string? featurefilter,
            string? setuptypefilter,
            string? locfilter,            
            string? languagefilter,
            string? sourcefilter,
            bool? activefilter,
            string? tagfilter,
            string? lastchange,
            string? publishedonfilter,
            IEnumerable<string>? tourismusvereinids
        )
        {
            sourcelist = Helper.CommonListCreator.CreateSourceList(sourcefilter);

            setuptypelist = Helper.VenueListCreator.CreateVenueSeatTypeListfromFlag(
                setuptypefilter
            );
            featurelist = Helper.VenueListCreator.CreateVenueFeatureListfromFlag(featurefilter);
            categorylist = Helper.VenueListCreator.CreateVenueCategoryListfromFlag(categoryfilter);

            idlist = Helper.CommonListCreator.CreateIdList(idfilter?.ToUpper());
            sourcelist = Helper.CommonListCreator.CreateSourceList(sourcefilter);
            languagelist = Helper.CommonListCreator.CreateIdList(languagefilter);            

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

            this.lastchange = lastchange;

            publishedonlist = Helper.CommonListCreator.CreateIdList(publishedonfilter?.ToLower());
            
            tagdict = GenericHelper.RetrieveTagFilter(tagfilter);
        }
    }
}
