// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Helper;
using ServiceReferenceLCS;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiCore.Controllers
{
    public class AccommodationHelper
    {
        public List<string> boardlist;
        public List<string> categorylist;
        public List<string> accotypelist;
        public Dictionary<string, bool> featurelist;
        public List<string> featureidlist;
        public Dictionary<string, bool> themelist;
        public List<string> badgelist;
        public List<string> idlist;
        public List<string> smgtaglist;
        public List<string> districtlist;
        public List<string> municipalitylist;
        public List<string> tourismvereinlist;
        public List<string> regionlist;
        public List<string> languagelist;
        public List<string> sourcelist;
        public bool? bookable;
        public bool? apartment;
        public bool altitude;
        public int altitudemin;
        public int altitudemax;
        public bool? active;
        public bool? smgactive;
        public string? lastchange;

        //New Publishedonlist
        public List<string> publishedonlist;

        public IDictionary<string, List<string>> tagdict;

        public static async Task<AccommodationHelper> CreateAsync(
            QueryFactory queryFactory,
            string? idfilter,
            string? locfilter,
            string? boardfilter,
            string? categoryfilter,
            string? typefilter,
            string? featurefilter,
            string? featureidfilter,
            string? badgefilter,
            string? themefilter,
            string? altitudefilter,
            string? smgtags,
            string? tagfilter,
            bool? activefilter,
            bool? smgactivefilter,
            bool? bookablefilter,
            string? sourcefilter,
            string? lastchange,
            string? langfilter,
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

            return new AccommodationHelper(
                idfilter: idfilter,
                locfilter: locfilter,
                boardfilter: boardfilter,
                categoryfilter: categoryfilter,
                typefilter: typefilter,
                featurefilter: featurefilter,
                featureidfilter: featureidfilter,
                badgefilter: badgefilter,
                themefilter: themefilter,
                altitudefilter: altitudefilter,
                bookablefilter: bookablefilter,
                activefilter: activefilter,
                smgactivefilter: smgactivefilter,
                smgtags: smgtags,
                tagfilter: tagfilter,
                sourcefilter: sourcefilter,
                lastchange: lastchange,
                languagefilter: langfilter,
                publishedonfilter: publishedonfilter,
                tourismusvereinids: tourismusvereinids
            );
        }

        private AccommodationHelper(
            string? idfilter,
            string? locfilter,
            string? boardfilter,
            string? categoryfilter,
            string? typefilter,
            string? featurefilter,
            string? featureidfilter,
            string? badgefilter,
            string? themefilter,
            string? altitudefilter,
            bool? bookablefilter,
            bool? activefilter,
            bool? smgactivefilter,
            string? smgtags,
            string? tagfilter,
            string? sourcefilter,
            string? lastchange,
            string? languagefilter,
            string? publishedonfilter,
            IEnumerable<string>? tourismusvereinids
        )
        {
            boardlist = AccoListCreator.CreateBoardListFromFlag(boardfilter);
            categorylist = AccoListCreator.CreateCategoryListfromFlag(categoryfilter);
            accotypelist = AccoListCreator.CreateAccoTypeListfromFlag(typefilter);
            featurelist = AccoListCreator.CreateFeatureListDictfromFlag(featurefilter);
            badgelist = AccoListCreator.CreateBadgeListfromFlag(badgefilter);
            themelist = AccoListCreator.CreateThemeListDictfromFlag(themefilter);
            idlist = String.IsNullOrEmpty(idfilter)
                ? new List<string>()
                : CommonListCreator.CreateIdList(idfilter.ToUpper());
            smgtaglist = CommonListCreator.CreateIdList(smgtags);
            sourcelist = Helper.CommonListCreator.CreateSourceList(sourcefilter);

            featureidlist = String.IsNullOrEmpty(featureidfilter)
                ? new List<string>()
                : CommonListCreator.CreateIdList(featureidfilter.ToUpper());

            districtlist = new List<string>();
            municipalitylist = new List<string>();
            tourismvereinlist = new List<string>();
            regionlist = new List<string>();
            if (locfilter != null && locfilter.Contains("reg"))
                regionlist = CommonListCreator.CreateDistrictIdList(locfilter, "reg");
            if (locfilter != null && locfilter.Contains("tvs"))
                tourismvereinlist = CommonListCreator.CreateDistrictIdList(locfilter, "tvs");
            if (locfilter != null && locfilter.Contains("mun"))
                municipalitylist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "mun");
            if (locfilter != null && locfilter.Contains("fra"))
                districtlist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "fra");

            if (tourismusvereinids != null)
                tourismvereinlist.AddRange(tourismusvereinids);

            //Special Case Apartment Setting Filter to HasApartment
            apartment = null;
            if (accotypelist.Contains("Apartment"))
            {
                apartment = true;
                accotypelist.Remove("Apartment");
            }

            //Altitude
            altitude = altitudefilter != null;
            if (altitude)
                (altitudemin, altitudemax) = CommonListCreator.CreateRangeString(altitudefilter);

            languagelist = Helper.CommonListCreator.CreateIdList(languagefilter);

            bookable = bookablefilter;

            //active
            active = activefilter;

            //smgactive
            smgactive = smgactivefilter;

            this.lastchange = lastchange;

            publishedonlist = Helper.CommonListCreator.CreateIdList(publishedonfilter?.ToLower());

            tagdict = GenericHelper.RetrieveTagFilter(tagfilter);
        }
    }
}
