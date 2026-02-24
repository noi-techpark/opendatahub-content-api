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
    public class ODHActivityPoiHelper
    {
        public List<string> typelist;
        public List<string> level3typelist;
        public List<string> subtypelist;
        public List<string> idlist;
        public List<string> arealist;
        public List<string> smgtaglist;
        public List<string> smgtaglistand;
        public List<string> sourcelist;
        public List<string> languagelist;
        public List<string> districtlist;
        public List<string> municipalitylist;
        public List<string> tourismvereinlist;
        public List<string> regionlist;
        public bool? highlight;
        public bool? active;
        public bool? smgactive;
        public bool? hasimage;
        public string? lastchange;

        //Gastronomy
        public List<string> dishcodesids;
        public List<string> ceremonycodesids;
        public List<string> categorycodesids;
        public List<string> facilitycodesids;
        public List<string> cuisinecodesids;

        //Activity
        public List<string> activitytypelist;
        public List<string> poitypelist;

        public List<string> difficultylist;
        public bool distance;
        public int distancemin;
        public int distancemax;
        public bool altitude;
        public int altitudemin;
        public int altitudemax;
        public bool duration;
        public int durationmin;
        public int durationmax;

        public IDictionary<string, List<string>> tagdict;

        //New Publishedonlist
        public List<string> publishedonlist;

        public static async Task<ODHActivityPoiHelper> CreateAsync(
            QueryFactory queryFactory,
            string? typefilter,
            string? subtypefilter,
            string? level3typefilter,
            string? idfilter,
            string? locfilter,
            string? areafilter,
            string? languagefilter,
            string? sourcefilter,
            bool? highlightfilter,
            bool? activefilter,
            bool? smgactivefilter,
            string? smgtags,
            string? smgtagsand,
            string? lastchange,
            string? categorycodefilter,
            string? dishcodefilter,
            string? ceremonycodefilter,
            string? facilitycodefilter,
            string? cuisinecodefilter,
            string? activitytypefilter,
            string? poitypefilter,
            string? distancefilter,
            string? altitudefilter,
            string? durationfilter,
            string? difficultyfilter,
            bool? hasimagefilter,
            string? tagfilter,
            string? publishedonfilter,
            CancellationToken cancellationToken
        )
        {
            var arealist = await GenericHelper.RetrieveAreaFilterDataAsync(
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

            return new ODHActivityPoiHelper(
                typefilter,
                subtypefilter,
                level3typefilter,
                idfilter,
                locfilter,
                arealist,
                languagefilter,
                sourcefilter,
                highlightfilter,
                activefilter,
                smgactivefilter,
                smgtags,
                smgtagsand,
                categorycodefilter,
                dishcodefilter,
                ceremonycodefilter,
                facilitycodefilter,
                cuisinecodefilter,
                activitytypefilter,
                poitypefilter,
                distancefilter,
                altitudefilter,
                durationfilter,
                difficultyfilter,
                hasimagefilter,
                tagfilter,
                publishedonfilter,
                lastchange,
                tourismusvereinids
            );
        }

        private ODHActivityPoiHelper(
            string? typefilter,
            string? subtypefilter,
            string? level3typefilter,
            string? idfilter,
            string? locfilter,
            IEnumerable<string> arealist,
            string? languagefilter,
            string? sourcefilter,
            bool? highlightfilter,
            bool? activefilter,
            bool? smgactivefilter,
            string? smgtags,
            string? smgtagsand,
            string? categorycodefilter,
            string? dishcodefilter,
            string? ceremonycodefilter,
            string? facilitycodefilter,
            string? cuisinecodefilter,
            string? activitytypefilter,
            string? poitypefilter,
            string? distancefilter,
            string? altitudefilter,
            string? durationfilter,
            string? difficultyfilter,
            bool? hasimagefilter,
            string? tagfilter,
            string? publishedonfilter,
            string? lastchange,
            IEnumerable<string>? tourismusvereinids
        )
        {
            typelist = new();

            if (!String.IsNullOrEmpty(typefilter))
            {
                if (int.TryParse(typefilter, out int typeinteger))
                {
                    //Sonderfall wenn alles abgefragt wird um keine unnötige Where zu erzeugen
                    if (typeinteger != 255)
                        typelist = Helper.ActivityPoiListCreator.CreateSmgPoiTypefromFlag(
                            typefilter
                        );
                }
                else
                {
                    if (typefilter.Contains(","))
                    {
                        typelist = typefilter.Split(',').ToList();
                    }
                    else if (typefilter != "null")
                        typelist.Add(typefilter);
                }
            }

            if (typelist.Count > 0)
                subtypelist = Helper.ActivityPoiListCreator.CreateSmgPoiSubTypefromFlag(
                    typelist.FirstOrDefault(),
                    subtypefilter
                );
            else
                subtypelist = new();

            if (subtypelist.Count > 0)
                level3typelist = Helper.ActivityPoiListCreator.CreateSmgPoiPoiTypefromFlag(
                    subtypelist.FirstOrDefault(),
                    level3typefilter
                );
            else
                level3typelist = new();

            idlist = Helper.CommonListCreator.CreateIdList(idfilter?.ToUpper());
            sourcelist = Helper.CommonListCreator.CreateSourceList(sourcefilter);
            languagelist = Helper.CommonListCreator.CreateIdList(languagefilter);

            //obsolete
            //sourcelist = SourceFilterHelper.ExtendSourceFilterODHActivityPois(sourcelisttemp);

            this.arealist = arealist.ToList();

            smgtaglist = Helper.CommonListCreator.CreateStringListFromStringParameter(smgtags);

            smgtaglistand = Helper.CommonListCreator.CreateStringListFromStringParameter(
                smgtagsand
            );

            tourismvereinlist = new();
            regionlist = new();
            municipalitylist = new();
            districtlist = new();

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

            //highlight
            highlight = highlightfilter;
            //active
            active = activefilter;
            //smgactive
            smgactive = smgactivefilter;
            //has cc0image
            hasimage = hasimagefilter;

            //Using Gastronomy Filters
            dishcodesids = GastronomyListCreator.CreateGastroDishCodeListfromFlag(dishcodefilter);
            ceremonycodesids = GastronomyListCreator.CreateGastroCeremonyCodeListfromFlag(
                ceremonycodefilter
            );
            categorycodesids = GastronomyListCreator.CreateGastroCategoryCodeListfromFlag(
                categorycodefilter
            );
            facilitycodesids = GastronomyListCreator.CreateGastroFacilityCodeListfromFlag(
                facilitycodefilter
            );
            cuisinecodesids = GastronomyListCreator.CreateGastroCusineCodeListfromFlag(
                cuisinecodefilter
            );
            facilitycodesids.AddRange(cuisinecodesids);

            //using Activity Filters
            activitytypelist = new();
            //using Poi Filters
            poitypelist = new();

            if (activitytypefilter != null)
            {
                if (int.TryParse(activitytypefilter, out int typeintegeractivity))
                {
                    activitytypelist = Helper.ActivityPoiListCreator.CreateActivityTypefromFlag(
                        activitytypefilter
                    );
                }
                else
                {
                    activitytypelist.Add(activitytypefilter);
                }
            }

            //ODHActivityPoi Typelist has priority
            if (typelist.Count == 0 && poitypelist.Count == 0 && activitytypelist.Count > 0)
                subtypelist = Helper.ActivityPoiListCreator.CreateActivitySubTypefromFlag(
                    activitytypelist.FirstOrDefault(),
                    subtypefilter
                );

            //using Poi Filters
            poitypelist = new();
            if (poitypefilter != null)
            {
                if (int.TryParse(poitypefilter, out int typeintegerpoi))
                {
                    poitypelist = Helper.ActivityPoiListCreator.CreatePoiTypefromFlag(
                        poitypefilter
                    );
                }
                else
                {
                    activitytypelist.Add(poitypefilter);
                }
            }

            //ODHActivityPoi Typelist has priority
            if (typelist.Count == 0 && activitytypelist.Count == 0 && poitypelist.Count > 0)
                subtypelist = Helper.ActivityPoiListCreator.CreatePoiSubTypefromFlag(
                    poitypelist.FirstOrDefault(),
                    subtypefilter
                );

            difficultylist = CommonListCreator.CreateDifficultyList(
                difficultyfilter,
                activitytypefilter
            );
            //Distance
            distance = distancefilter != null;
            if (distance)
            {
                var (min, max) = CommonListCreator.CreateRangeString(distancefilter);
                distancemin = min * 1000;
                distancemax = max * 1000;
            }

            //Altitude
            altitude = altitudefilter != null;
            if (altitude)
                (altitudemin, altitudemax) = CommonListCreator.CreateRangeString(altitudefilter);

            //Duration
            duration = durationfilter != null;
            if (duration)
                (durationmin, durationmax) = CommonListCreator.CreateRangeString(durationfilter);

            publishedonlist = Helper.CommonListCreator.CreateIdList(publishedonfilter?.ToLower());

            tagdict = GenericHelper.RetrieveTagFilter(tagfilter);

            this.lastchange = lastchange;
        }
    }
}
