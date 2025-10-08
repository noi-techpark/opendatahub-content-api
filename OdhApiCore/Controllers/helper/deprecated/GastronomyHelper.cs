//// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
////
//// SPDX-License-Identifier: AGPL-3.0-or-later

//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using Helper;
//using SqlKata.Execution;

//namespace OdhApiCore.Controllers
//{
//    public class GastronomyHelper
//    {
//        public List<string> idlist;
//        public List<string> dishcodesids;
//        public List<string> ceremonycodesids;
//        public List<string> categorycodesids;
//        public List<string> facilitycodesids;
//        public List<string> cuisinecodesids;
//        public List<string> smgtaglist;
//        public List<string> districtlist;
//        public List<string> municipalitylist;
//        public List<string> tourismvereinlist;
//        public List<string> regionlist;
//        public List<string> languagelist;
//        public bool? active;
//        public bool? smgactive;
//        public string? lastchange;

//        public static async Task<GastronomyHelper> CreateAsync(
//            QueryFactory queryFactory,
//            string? idfilter,
//            string? locfilter,
//            string? categorycodefilter,
//            string? dishcodefilter,
//            string? ceremonycodefilter,
//            string? facilitycodefilter,
//            string? cuisinecodefilter,
//            bool? activefilter,
//            bool? smgactivefilter,
//            string? smgtags,
//            string? lastchange,
//            string? langfilter,
//            CancellationToken cancellationToken
//        )
//        {
//            IEnumerable<string>? tourismusvereinids = null;
//            if (locfilter != null && locfilter.Contains("mta"))
//            {
//                List<string> metaregionlist = CommonListCreator.CreateDistrictIdList(
//                    locfilter,
//                    "mta"
//                );
//                tourismusvereinids = await GenericHelper.RetrieveLocFilterDataAsync(
//                    queryFactory,
//                    metaregionlist,
//                    cancellationToken
//                );
//            }

//            return new GastronomyHelper(
//                idfilter: idfilter,
//                locfilter: locfilter,
//                dishcodefilter: dishcodefilter,
//                ceremonycodefilter: ceremonycodefilter,
//                categorycodefilter: categorycodefilter,
//                facilitycodefilter: facilitycodefilter,
//                cuisinecodefilter: cuisinecodefilter,
//                activefilter: activefilter,
//                smgactivefilter: smgactivefilter,
//                smgtags: smgtags,
//                lastchange: lastchange,
//                languagefilter: langfilter,
//                tourismusvereinids: tourismusvereinids
//            );
//        }

//        private GastronomyHelper(
//            string? idfilter,
//            string? locfilter,
//            string? dishcodefilter,
//            string? ceremonycodefilter,
//            string? categorycodefilter,
//            string? facilitycodefilter,
//            string? cuisinecodefilter,
//            bool? activefilter,
//            bool? smgactivefilter,
//            string? smgtags,
//            string? lastchange,
//            string? languagefilter,
//            IEnumerable<string>? tourismusvereinids
//        )
//        {
//            idlist = CommonListCreator.CreateIdList(idfilter?.ToUpper());

//            smgtaglist = CommonListCreator.CreateIdList(smgtags);

//            dishcodesids = GastronomyListCreator.CreateGastroDishCodeListfromFlag(dishcodefilter);
//            ceremonycodesids = GastronomyListCreator.CreateGastroCeremonyCodeListfromFlag(
//                ceremonycodefilter
//            );
//            categorycodesids = GastronomyListCreator.CreateGastroCategoryCodeListfromFlag(
//                categorycodefilter
//            );
//            facilitycodesids = GastronomyListCreator.CreateGastroFacilityCodeListfromFlag(
//                facilitycodefilter
//            );
//            cuisinecodesids = GastronomyListCreator.CreateGastroCusineCodeListfromFlag(
//                cuisinecodefilter
//            );
//            facilitycodesids.AddRange(cuisinecodesids);

//            languagelist = Helper.CommonListCreator.CreateIdList(languagefilter);

//            tourismvereinlist = new List<string>();
//            regionlist = new List<string>();
//            municipalitylist = new List<string>();
//            districtlist = new List<string>();

//            if (locfilter != null && locfilter.Contains("reg"))
//                regionlist = CommonListCreator.CreateDistrictIdList(locfilter, "reg");
//            if (locfilter != null && locfilter.Contains("tvs"))
//                tourismvereinlist = CommonListCreator.CreateDistrictIdList(locfilter, "tvs");
//            if (locfilter != null && locfilter.Contains("mun"))
//                municipalitylist = CommonListCreator.CreateDistrictIdList(locfilter, "mun");
//            if (locfilter != null && locfilter.Contains("fra"))
//                districtlist = CommonListCreator.CreateDistrictIdList(locfilter, "fra");

//            if (tourismusvereinids != null)
//                tourismvereinlist.AddRange(tourismusvereinids);

//            //active
//            active = activefilter;

//            //smgactive
//            smgactive = smgactivefilter;

//            this.lastchange = lastchange;
//        }
//    }
//}
