// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SqlKata;

namespace Helper
{
    public static class PostgresSQLWhereBuilder
    {
        private static readonly string[] _languagesToSearchFor = new[]
        {
            "de",
            "it",
            "en",
            "nl",
            "cs",
            "pl",
            "fr",
            "pl",
        };

        /// <summary>
        /// Provide title fields as JsonPath
        /// </summary>
        /// <param name="language">
        /// If provided only the fields with the
        /// specified language get returned
        /// </param>
        public static string[] TitleFieldsToSearchFor(string? language) =>
            _languagesToSearchFor
                .Where(lang => language != null ? lang == language : true)
                .Select(lang => $"Detail.{lang}.Title")
                .ToArray()
                .AddToStringArray("Id");

        public static string[] TitleFieldsToSearchFor(
            string? language,
            IReadOnlyCollection<string>? haslanguage
        ) =>
            _languagesToSearchFor
                .Where(lang =>
                    language != null ? lang == language
                    : true && haslanguage != null && haslanguage.Count > 0
                        ? haslanguage.Contains(lang)
                    : true
                )
                .Select(lang => $"Detail.{lang}.Title")
                .ToArray();

        public static string[] AccoTitleFieldsToSearchFor(string? language) =>
            _languagesToSearchFor
                .Where(lang => language != null ? lang == language : true)
                .Select(lang => $"AccoDetail.{lang}.Name")
                .ToArray();

        public static string[] AccoTitleFieldsToSearchFor(
            string? language,
            IReadOnlyCollection<string>? haslanguage
        ) =>
            _languagesToSearchFor
                .Where(lang =>
                    language != null ? lang == language
                    : true && haslanguage != null && haslanguage.Count > 0
                        ? haslanguage.Contains(lang)
                    : true
                )
                .Select(lang => $"AccoDetail.{lang}.Name")
                .ToArray();

        public static string[] AccoRoomNameFieldsToSearchFor(string? language) =>
            _languagesToSearchFor
                .Where(lang => language != null ? lang == language : true)
                .Select(lang => $"AccoRoomDetail.{lang}.Name")
                .ToArray();

        public static string[] AccoRoomNameFieldsToSearchFor(
            string? language,
            IReadOnlyCollection<string>? haslanguage
        ) =>
            _languagesToSearchFor
                .Where(lang =>
                    language != null ? lang == language
                    : true && haslanguage != null && haslanguage.Count > 0
                        ? haslanguage.Contains(lang)
                    : true
                )
                .Select(lang => $"AccoRoomDetail.{lang}.Name")
                .ToArray();

        public static string[] EventShortTitleFieldsToSearchFor(string? language) =>
            _languagesToSearchFor
                .Where(lang => language != null ? lang == language : true)
                .Select(lang => $"EventTitle.{lang}")
                .ToArray();

        public static string[] EventShortTitleFieldsToSearchFor(
            string? language,
            IReadOnlyCollection<string>? haslanguage
        ) =>
            _languagesToSearchFor
                .Where(lang =>
                    language != null ? lang == language
                    : true && haslanguage != null && haslanguage.Count > 0
                        ? haslanguage.Contains(lang)
                    : true
                )
                .Select(lang => $"EventTitle.{lang}")
                .ToArray();

        public static string[] TourismMetaDataTitleFieldsToSearchFor(string? language) =>
            _languagesToSearchFor
                .Where(lang => language != null ? lang == language : true)
                .Select(lang => $"ApiDescription.{lang}")
                .ToArray();

        //TODO TRANSFORM LANGUAGE to deu,eng,ita
        //public static string[] VenueTitleFieldsToSearchFor(string? language) =>
        //   _languagesToSearchFor.Where(lang =>
        //       language != null ? lang == language : true
        //   ).Select(lang =>
        //       $"attributes.name.{TransformLanguagetoDDStandard(lang)}"
        //   ).ToArray();

        //public static string TransformLanguagetoDDStandard(string language) => language switch
        //{
        //    "de" =>  "deu",
        //    "it" =>  "ita",
        //    "en" =>  "eng",
        //    _ => language
        //};


        //TODO search name example
        //name: {
        //    deu: "Akademie deutsch-italienischer Studien",
        //    ita: "Accademia di studi italo-tedeschi",
        //    eng: "Academy of German-Italian Studies"
        //    },
        //private static string[] VenueTitleFieldsToSearchFor(string? language) =>
        // _languagesToSearchFor.Where(lang =>
        //     language != null ? lang == language : true
        // ).Select(lang =>
        //     $"odhdata.Detail.{lang}.Name"
        // ).ToArray();

        //Public for use in Controllers directly
        public static string[] TypeDescFieldsToSearchFor(string? language) =>
            _languagesToSearchFor
                .Where(lang => language != null ? lang == language : true)
                .Select(lang => $"TypeDesc.{lang}")
                .ToArray();

        public static string[] TagNameFieldsToSearchFor(string? language) =>
            _languagesToSearchFor
                .Where(lang => language != null ? lang == language : true)
                .Select(lang => $"TagName.{lang}")
                .ToArray();

        public static string[] NameFieldsToSearchFor(string? language) =>
            _languagesToSearchFor
                .Where(lang => language != null ? lang == language : true)
                .Select(lang => $"Name.{lang}")
                .ToArray()
                .AddToStringArray("Id");

        public static string[] WeatherHistoryFieldsToSearchFor(string? language) =>
            _languagesToSearchFor
                .Where(lang => language != null ? lang == language : true)
                .Select(lang => $"Weather.{lang}.evolutiontitle")
                .ToArray();

        public static void CheckPassedLanguage(
            ref string language,
            IEnumerable<string> availablelanguages
        )
        {
            language = language.ToLower();

            if (!availablelanguages.Contains(language))
                throw new Exception("passed language not available or passed incorrect string");
        }

        //Return where and Parameters
        [System.Diagnostics.Conditional("TRACE")]
        private static void LogMethodInfo(
            System.Reflection.MethodBase m,
            params object?[] parameters
        )
        {
            var parameterInfo = m.GetParameters()
                .Zip(parameters)
                .Select((x, _) => (x.First.Name, x.Second));
            Serilog.Log.Debug("{method}({@parameters})", m.Name, parameterInfo);
        }

        //Return Where and Parameters for Activity
        public static Query ActivityWhereExpression(
            this Query query,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> activitytypelist,
            IReadOnlyCollection<string> subtypelist,
            IReadOnlyCollection<string> difficultylist,
            IReadOnlyCollection<string> smgtaglist,
            IReadOnlyCollection<string> districtlist,
            IReadOnlyCollection<string> municipalitylist,
            IReadOnlyCollection<string> tourismvereinlist,
            IReadOnlyCollection<string> regionlist,
            IReadOnlyCollection<string> arealist,
            bool distance,
            int distancemin,
            int distancemax,
            bool duration,
            int durationmin,
            int durationmax,
            bool altitude,
            int altitudemin,
            int altitudemax,
            bool? highlight,
            bool? activefilter,
            bool? smgactivefilter,
            string? searchfilter,
            string? language,
            string? lastchange,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                idlist,
                activitytypelist,
                subtypelist,
                difficultylist,
                smgtaglist,
                districtlist,
                municipalitylist,
                tourismvereinlist,
                regionlist,
                arealist,
                distance,
                distancemin,
                distancemax,
                duration,
                durationmin,
                durationmax,
                altitude,
                altitudemin,
                altitudemax,
                highlight,
                activefilter,
                smgactivefilter,
                searchfilter,
                language,
                lastchange
            );

            return query
                .IdUpperFilter(idlist)
                .When(
                    activitytypelist.Count > 0,
                    q => q.SmgTagFilterOr_GeneratedColumn(activitytypelist)
                ) //.ActivityTypeFilterOnTags(activitytypelist)
                .When(subtypelist.Count > 0, q => q.SmgTagFilterOr_GeneratedColumn(subtypelist)) //.ActivitySubTypeFilterOnTags(subtypelist)
                .ActiveFilter_GeneratedColumn(activefilter) //.ActiveFilter(activefilter)
                .When(smgtaglist.Count > 0, q => q.SmgTagFilterOr_GeneratedColumn(smgtaglist)) //OK GENERATED COLUMNS //.SmgTagFilter(smgtaglist)
                .OdhActiveFilter_GeneratedColumn(smgactivefilter) //OK GENERATED COLUMNS //.SmgActiveFilter(smgactivefilter)
                .LastChangedFilter_GeneratedColumn(lastchange) //.LastChangedFilter(lastchange)
                .When(
                    languagelist.Count > 0,
                    q => q.HasLanguageFilterAnd_GeneratedColumn(languagelist)
                ) //OK GENERATED COLUMNS
                .DistrictFilter(districtlist)
                .LocFilterMunicipalityFilter(municipalitylist)
                .LocFilterTvsFilter(tourismvereinlist)
                .LocFilterRegionFilter(regionlist)
                .AreaFilter(arealist)
                .DifficultyFilter(difficultylist)
                .DistanceFilter(distance, distancemin, distancemax)
                .DurationFilter(duration, durationmin, durationmax)
                .AltitudeFilter(altitude, altitudemin, altitudemax)
                .HighlightFilter(highlight)
                .SearchFilter(TitleFieldsToSearchFor(language, languagelist), searchfilter)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles)
                .FilterReducedDataByRoles(userroles);
        }

        //Return Where and Parameters for Poi
        public static Query PoiWhereExpression(
            this Query query,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> poitypelist,
            IReadOnlyCollection<string> subtypelist,
            IReadOnlyCollection<string> smgtaglist,
            IReadOnlyCollection<string> districtlist,
            IReadOnlyCollection<string> municipalitylist,
            IReadOnlyCollection<string> tourismvereinlist,
            IReadOnlyCollection<string> regionlist,
            IReadOnlyCollection<string> arealist,
            bool? highlight,
            bool? activefilter,
            bool? smgactivefilter,
            string? searchfilter,
            string? language,
            string? lastchange,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>",
                idlist,
                poitypelist,
                subtypelist,
                smgtaglist,
                districtlist,
                municipalitylist,
                tourismvereinlist,
                regionlist,
                arealist,
                highlight,
                activefilter,
                smgactivefilter,
                searchfilter,
                language,
                lastchange
            );

            return query
                .IdUpperFilter(idlist)
                .When(poitypelist.Count > 0, q => q.SmgTagFilterOr_GeneratedColumn(poitypelist)) //OK GENERATED COLUMNS //.PoiTypeFilterOnTags(poitypelist)
                .When(subtypelist.Count > 0, q => q.SmgTagFilterOr_GeneratedColumn(subtypelist)) //OK GENERATED COLUMNS //.PoiSubTypeFilterOnTags(subtypelist)
                .When(smgtaglist.Count > 0, q => q.SmgTagFilterOr_GeneratedColumn(smgtaglist)) //OK GENERATED COLUMNS //.SmgTagFilter(smgtaglist)
                .ActiveFilter_GeneratedColumn(activefilter) //OK GENERATED COLUMNS //.ActiveFilter(activefilter)
                .OdhActiveFilter_GeneratedColumn(smgactivefilter) //OK GENERATED COLUMNS //.SmgActiveFilter(smgactivefilter)
                .LastChangedFilter_GeneratedColumn(lastchange)
                .When(
                    languagelist.Count > 0,
                    q => q.HasLanguageFilterAnd_GeneratedColumn(languagelist)
                ) //OK GENERATED COLUMNS
                .DistrictFilter(districtlist) //Use generated columns also here?
                .LocFilterMunicipalityFilter(municipalitylist) //Use generated columns also here?
                .LocFilterTvsFilter(tourismvereinlist) //Use generated columns also here?
                .LocFilterRegionFilter(regionlist) //Use generated columns also here?
                .AreaFilter(arealist) //Use generated columns also here?
                .HighlightFilter(highlight)
                .SearchFilter(TitleFieldsToSearchFor(language, languagelist), searchfilter)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles)
                .FilterReducedDataByRoles(userroles);
        }

        //Return Where and Parameters for Gastronomy
        public static Query GastronomyWhereExpression(
            this Query query,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> dishcodeslist,
            IReadOnlyCollection<string> ceremonycodeslist,
            IReadOnlyCollection<string> categorycodeslist,
            IReadOnlyCollection<string> facilitycodeslist,
            IReadOnlyCollection<string> smgtaglist,
            IReadOnlyCollection<string> districtlist,
            IReadOnlyCollection<string> municipalitylist,
            IReadOnlyCollection<string> tourismvereinlist,
            IReadOnlyCollection<string> regionlist,
            bool? activefilter,
            bool? smgactivefilter,
            string? searchfilter,
            string? language,
            string? lastchange,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                idlist,
                dishcodeslist,
                ceremonycodeslist,
                categorycodeslist,
                facilitycodeslist,
                smgtaglist,
                districtlist,
                municipalitylist,
                tourismvereinlist,
                regionlist,
                activefilter,
                smgactivefilter,
                searchfilter,
                language,
                lastchange
            );

            return query
                .IdUpperFilter(idlist)
                .When(smgtaglist.Count > 0, q => q.SmgTagFilterOr_GeneratedColumn(smgtaglist)) //OK GENERATED COLUMNS //.SmgTagFilter(smgtaglist)
                .ActiveFilter_GeneratedColumn(activefilter) //OK GENERATED COLUMNS //.ActiveFilter(activefilter)
                .OdhActiveFilter_GeneratedColumn(smgactivefilter) //OK GENERATED COLUMNS //.SmgActiveFilter(smgactivefilter)
                .LastChangedFilter_GeneratedColumn(lastchange)
                .When(
                    languagelist.Count > 0,
                    q => q.HasLanguageFilterAnd_GeneratedColumn(languagelist)
                ) //.HasLanguageFilter(languagelist)
                .DistrictFilter(districtlist)
                .LocFilterMunicipalityFilter(municipalitylist)
                .LocFilterTvsFilter(tourismvereinlist)
                .LocFilterRegionFilter(regionlist)
                .CeremonyCodeFilter(ceremonycodeslist)
                .CategoryCodeFilter(categorycodeslist)
                .CuisineCodeFilter(facilitycodeslist)
                .DishCodeFilter(dishcodeslist)
                .SearchFilter(TitleFieldsToSearchFor(language, languagelist), searchfilter)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles)
                .FilterReducedDataByRoles(userroles);
        }

        //Return Where and Parameters for Activity
        public static Query ODHActivityPoiWhereExpression(
            this Query query,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> typelist,
            IReadOnlyCollection<string> subtypelist,
            IReadOnlyCollection<string> level3typelist,
            IReadOnlyCollection<string> sourcelist,
            IReadOnlyCollection<string> smgtaglist,
            IReadOnlyCollection<string> smgtaglistand,
            IReadOnlyCollection<string> districtlist,
            IReadOnlyCollection<string> municipalitylist,
            IReadOnlyCollection<string> tourismvereinlist,
            IReadOnlyCollection<string> regionlist,
            IReadOnlyCollection<string> arealist,
            bool? highlight,
            bool? activefilter,
            bool? smgactivefilter,
            IReadOnlyCollection<string> categorycodeslist,
            IReadOnlyCollection<string> dishcodeslist,
            IReadOnlyCollection<string> ceremonycodeslist,
            IReadOnlyCollection<string> facilitycodeslist,
            IReadOnlyCollection<string> activitytypelist,
            IReadOnlyCollection<string> poitypelist,
            IReadOnlyCollection<string> difficultylist,
            bool distance,
            int distancemin,
            int distancemax,
            bool duration,
            int durationmin,
            int durationmax,
            bool altitude,
            int altitudemin,
            int altitudemax,
            IDictionary<string, List<string>>? tagdict,
            bool? hasimage,
            IReadOnlyCollection<string> publishedonlist,
            string? searchfilter,
            string? language,
            string? lastchange,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                idlist,
                typelist,
                subtypelist,
                level3typelist,
                poitypelist,
                languagelist,
                sourcelist,
                smgtaglist,
                districtlist,
                municipalitylist,
                tourismvereinlist,
                regionlist,
                arealist,
                tagdict,
                highlight,
                activefilter,
                smgactivefilter,
                hasimage,
                searchfilter,
                language,
                lastchange
            );

            return query
                .IdLowerFilter(idlist)
                .When(typelist.Count > 0, q => q.SmgTagFilterOr_GeneratedColumn(typelist)) //.ODHActivityPoiTypeFilterOnTags(typelist)
                .When(subtypelist.Count > 0, q => q.SmgTagFilterOr_GeneratedColumn(subtypelist)) //.ODHActivityPoiSubTypeFilterOnTags(subtypelist)
                .When(
                    level3typelist.Count > 0,
                    q => q.SmgTagFilterOr_GeneratedColumn(level3typelist)
                ) //.ODHActivityPoiSubTypeFilterOnTags(subtypelist)
                //.SyncSourceInterfaceFilter_GeneratedColumn(sourcelist)
                //.SourceFilter_GeneratedColumn(sourcelist)
                .SourceOrSyncSourceInterfaceFilter_GeneratedColumn(sourcelist)
                .When(
                    languagelist.Count > 0,
                    q => q.HasLanguageFilterAnd_GeneratedColumn(languagelist)
                ) //.HasLanguageFilter(languagelist)
                .ActiveFilter_GeneratedColumn(activefilter) //OK GENERATED COLUMNS //.ActiveFilter(activefilter)
                .OdhActiveFilter_GeneratedColumn(smgactivefilter) //OK GENERATED COLUMNS //.SmgActiveFilter(smgactivefilter)
                .HasImage_GeneratedColumn(hasimage) //OK GENERATED COLUMNS //.SmgActiveFilter(smgactivefilter)
                //New ActivityFilters
                .When(
                    activitytypelist.Count > 0,
                    q => q.SmgTagFilterOr_GeneratedColumn(activitytypelist)
                ) //.ActivityTypeFilterOnTags(activitytypelist)
                //Changed PoiTypeFilter
                .When(poitypelist.Count > 0, q => q.SmgTagFilterOr_GeneratedColumn(poitypelist)) //.ODHActivityPoiPoiTypeFilterOnTags(poitypelist)
                .PublishedOnFilter_GeneratedColumn(publishedonlist) //.PublishedOnFilter(publishedonlist)
                .When(
                    tagdict != null && tagdict.Count > 0,
                    q => q.TaggingFilter_GeneratedColumn(tagdict)
                )
                .When(smgtaglist.Count > 0, q => q.SmgTagFilterOr_GeneratedColumn(smgtaglist)) //OK GENERATED COLUMNS //.SmgTagFilter(smgtaglist)
                .When(
                    smgtaglistand.Count > 0,
                    q => q.SmgTagFilterAnd_GeneratedColumn(smgtaglistand)
                ) //OK GENERATED COLUMNS //.SmgTagFilter(smgtaglist)                                                                                                //New GastronomyFilters
                .LastChangedFilter_GeneratedColumn(lastchange)
                .LocFilterDistrictFilter(districtlist)
                .LocFilterMunicipalityFilter(municipalitylist)
                .LocFilterTvsFilter(tourismvereinlist)
                .LocFilterRegionFilter(regionlist)
                .AreaFilter(arealist)
                .HighlightFilter(highlight)
                .CeremonyCodeFilter(ceremonycodeslist)
                .CategoryCodeFilter(categorycodeslist)
                .CuisineCodeFilter(facilitycodeslist)
                .DishCodeFilter(dishcodeslist)
                .DifficultyFilter(difficultylist)
                .DistanceFilter(distance, distancemin, distancemax)
                .DurationFilter(duration, durationmin, durationmax)
                .AltitudeFilter(altitude, altitudemin, altitudemax)
                //.When(tagdict != null && tagdict.ContainsKey("and") && tagdict["and"].Any(), q => q.TaggingFilter_AND(tagdict!["and"]))
                //.When(tagdict != null && tagdict.ContainsKey("or") && tagdict["or"].Any(), q => q.TaggingFilter_OR(tagdict!["or"]))
                .SearchFilter(TitleFieldsToSearchFor(language, languagelist), searchfilter)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles)
                .FilterReducedDataByRoles(userroles);
        }

        //Return Where and Parameters for Article
        public static Query ArticleWhereExpression(
            this Query query,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> typelist,
            IReadOnlyCollection<string> subtypelist,
            IReadOnlyCollection<string> smgtaglist,
            bool? highlight,
            bool? activefilter,
            bool? smgactivefilter,
            DateTime? articledate,
            DateTime? articledateto,
            IReadOnlyCollection<string> sourcelist,
            IReadOnlyCollection<string> publishedonlist,
            string? searchfilter,
            string? language,
            string? lastchange,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                idlist,
                typelist,
                subtypelist,
                languagelist,
                smgtaglist,
                highlight,
                activefilter,
                smgactivefilter,
                articledate,
                articledateto,
                sourcelist,
                publishedonlist,
                searchfilter,
                language,
                lastchange
            );

            return query
                .IdUpperFilter(idlist)
                //.ODHActivityPoiTypeFilter(typelist)
                //.ODHActivityPoiSubTypeFilter(subtypelist)
                .When(typelist.Count > 0, q => q.ArticleTypeFilterOr_GeneratedColumn(typelist))
                .When(
                    subtypelist.Count > 0,
                    q => q.ArticleTypeFilterOr_GeneratedColumn(subtypelist)
                )
                .When(
                    languagelist.Count > 0,
                    q => q.HasLanguageFilterAnd_GeneratedColumn(languagelist)
                ) //.HasLanguageFilter(languagelist)
                .ActiveFilter_GeneratedColumn(activefilter) //OK GENERATED COLUMNS //.ActiveFilter(activefilter)
                .OdhActiveFilter_GeneratedColumn(smgactivefilter) //OK GENERATED COLUMNS //.SmgActiveFilter(smgactivefilter)
                .When(smgtaglist.Count > 0, q => q.SmgTagFilterOr_GeneratedColumn(smgtaglist)) //OK GENERATED COLUMNS //.SmgTagFilter(smgtaglist)
                //.ArticleDateNewsFilterBeginWithIN_GeneratedColumn(articledate, articledateto)
                //.ArticleDateNewsFilterEndWithIN_GeneratedColumn(articledate, articledateto)
                .ArticleDateNewsFilterBeginEndWithIN_GeneratedColumn(articledate, articledateto)
                .PublishedOnFilter_GeneratedColumn(publishedonlist) //.PublishedOnFilter(publishedonlist)
                //.SyncSourceInterfaceFilter_GeneratedColumn(sourcelist)
                .SourceFilter_GeneratedColumn(sourcelist)
                .LastChangedFilter_GeneratedColumn(lastchange)
                .HighlightFilter(highlight)
                .SearchFilter(TitleFieldsToSearchFor(language, languagelist), searchfilter)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles);
        }

        //Return Where and Parameters for Event
        public static Query EventWhereExpression(
            this Query query,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> topiclist,
            IReadOnlyCollection<Int32> ranclist,
            IReadOnlyCollection<string> smgtaglist,
            IReadOnlyCollection<string> districtlist,
            IReadOnlyCollection<string> municipalitylist,
            IReadOnlyCollection<string> tourismvereinlist,
            IReadOnlyCollection<string> regionlist,
            IReadOnlyCollection<string> orglist,
            IReadOnlyCollection<string> sourcelist,
            DateTime? begindate,
            DateTime? enddate,
            bool? activefilter,
            bool? smgactivefilter,
            IReadOnlyCollection<string> publishedonlist,
            string? searchfilter,
            string? language,
            string? lastchange,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                idlist,
                topiclist,
                ranclist,
                smgtaglist,
                districtlist,
                municipalitylist,
                tourismvereinlist,
                regionlist,
                orglist,
                sourcelist,
                languagelist,
                begindate,
                enddate,
                activefilter,
                smgactivefilter,
                searchfilter,
                language,
                lastchange
            );

            return query
                .IdUpperFilter(idlist)
                .EventTopicFilter_GeneratedColumn(topiclist)
                //.EventDateFilterEnd_GeneratedColumn(begindate, enddate)
                //.EventDateFilterBegin_GeneratedColumn(begindate, enddate)
                //.EventDateFilterBeginEnd_GeneratedColumn(begindate, enddate)
                //TSMULTIRANGE Filter Both Dates given
                .When(
                    begindate != DateTime.MinValue && enddate != DateTime.MaxValue, 
                    q => q.DateFilter_GeneratedColumn(begindate, enddate, "")
                )
                //TSMULTIRANGE Filter Only Begindate given
                .When(
                    begindate != DateTime.MinValue && enddate == DateTime.MaxValue,
                    q => q.DateFilter_GeneratedColumn(begindate, null, "")
                )
                //TSMULTIRANGE Filter Only Enddate given
                .When(
                    begindate == DateTime.MinValue && enddate != DateTime.MaxValue,
                    q => q.DateFilter_GeneratedColumn(null, enddate, "")
                )
                .When(
                    languagelist.Count > 0,
                    q => q.HasLanguageFilterAnd_GeneratedColumn(languagelist)
                ) //.HasLanguageFilter(languagelist)
                .SyncSourceInterfaceFilter_GeneratedColumn(sourcelist)
                .ActiveFilter_GeneratedColumn(activefilter) //OK GENERATED COLUMNS //.ActiveFilter(activefilter)
                .OdhActiveFilter_GeneratedColumn(smgactivefilter) //OK GENERATED COLUMNS //.SmgActiveFilter(smgactivefilter)
                .When(smgtaglist.Count > 0, q => q.SmgTagFilterOr_GeneratedColumn(smgtaglist)) //OK GENERATED COLUMNS //.SmgTagFilter(smgtaglist)
                .PublishedOnFilter_GeneratedColumn(publishedonlist) //.PublishedOnFilter(publishedonlist)
                .LastChangedFilter_GeneratedColumn(lastchange)
                .DistrictFilter(districtlist)
                .LocFilterMunicipalityFilter(municipalitylist)
                .LocFilterTvsFilter(tourismvereinlist)
                .LocFilterRegionFilter(regionlist)
                .EventPublisherRancFilter(ranclist)
                .EventOrgFilter(orglist)
                .SearchFilterWithGenId(TitleFieldsToSearchFor(language, languagelist), searchfilter)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles)
                .FilterReducedDataByRoles(userroles);
        }

        //Return Where and Parameters for EventV2
        public static Query EventV2WhereExpression(
            this Query query,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> venueidlist,
            IDictionary<string, List<string>>? tagdict,
            IReadOnlyCollection<string> districtlist,
            IReadOnlyCollection<string> municipalitylist,
            IReadOnlyCollection<string> tourismvereinlist,
            IReadOnlyCollection<string> regionlist,
            IReadOnlyCollection<string> sourcelist,
            DateTime? begindate,
            DateTime? enddate,
            bool? activefilter,
            IReadOnlyCollection<string> publishedonlist,
            string? searchfilter,
            string? language,
            string? lastchange,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                idlist,
                venueidlist,
                districtlist,
                municipalitylist,
                tourismvereinlist,
                regionlist,
                sourcelist,
                languagelist,
                begindate,
                enddate,
                activefilter,
                searchfilter,
                language,
                lastchange
            );

            return query
                .IdUpperFilter(idlist)
                //.DateFilter_GeneratedColumn(begindate, enddate, "")
                .EventDateFilterBeginEnd_GeneratedColumn(begindate, enddate)
                .When(
                    languagelist.Count > 0,
                    q => q.HasLanguageFilterAnd_GeneratedColumn(languagelist)
                ) //.HasLanguageFilter(languagelist)
                .SyncSourceInterfaceFilter_GeneratedColumn(sourcelist)
                .ActiveFilter_GeneratedColumn(activefilter) //OK GENERATED COLUMNS //.ActiveFilter(activefilter)
                .PublishedOnFilter_GeneratedColumn(publishedonlist) //.PublishedOnFilter(publishedonlist)
                .LastChangedFilter_GeneratedColumn(lastchange)
                .DistrictFilter(districtlist)
                .LocFilterMunicipalityFilter(municipalitylist)
                .LocFilterTvsFilter(tourismvereinlist)
                .LocFilterRegionFilter(regionlist)
                .When(
                    tagdict != null && tagdict.Count > 0,
                    q => q.TaggingFilter_GeneratedColumn(tagdict)
                )
                .SearchFilter(TitleFieldsToSearchFor(language, languagelist), searchfilter)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles)
                .FilterReducedDataByRoles(userroles);
        }

        //Return Where and Parameters for Accommodation
        public static Query AccommodationWhereExpression(
            this Query query,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> accotypelist,
            IReadOnlyCollection<string> categorylist,
            Dictionary<string, bool> featurelist,
            IReadOnlyCollection<string> featureidlist,
            IReadOnlyCollection<string> badgelist,
            Dictionary<string, bool> themelist,
            IReadOnlyCollection<string> boardlist,
            IReadOnlyCollection<string> smgtaglist,
            IReadOnlyCollection<string> districtlist,
            IReadOnlyCollection<string> municipalitylist,
            IReadOnlyCollection<string> tourismvereinlist,
            IReadOnlyCollection<string> regionlist,
            bool? apartmentfilter,
            bool? bookable,
            bool altitude,
            int altitudemin,
            int altitudemax,
            bool? activefilter,
            bool? smgactivefilter,
            IReadOnlyCollection<string> publishedonlist,
            IReadOnlyCollection<string> sourcelist,
            string? searchfilter,
            string? language,
            string? lastchange,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                idlist,
                accotypelist,
                categorylist,
                featurelist,
                featureidlist,
                badgelist,
                languagelist,
                themelist,
                boardlist,
                smgtaglist,
                districtlist,
                municipalitylist,
                tourismvereinlist,
                regionlist,
                altitude,
                altitudemin,
                altitudemax,
                activefilter,
                smgactivefilter,
                searchfilter,
                apartmentfilter,
                bookable,
                language,
                lastchange,
                sourcelist
            );

            return query
                .IdUpperFilter(idlist)
                .AccoTypeFilter_GeneratedColumn(accotypelist)
                .AccoCategoryFilter_GeneratedColumn(categorylist) //.AccoCategoryFilter(categorylist)
                .AccoSpecialFeatureIdsFilterOr_GeneratedColumn(
                    featurelist.Where(x => x.Value == true).Select(x => x.Key).ToList()
                ) //.AccoFeatureFilter(featurelist.Where(x => x.Value == true).Select(x => x.Key).ToList())
                .AccoFeatureIdsFilterOr_GeneratedColumn(featureidlist) //.AccoFeatureIdFilter(featureidlist)
                .AccoBadgeIdsFilterOr_GeneratedColumn(badgelist) //.AccoBadgeFilter(badgelist)
                .AccoThemeIdsFilterOr_GeneratedColumn(
                    themelist.Where(x => x.Value == true).Select(x => x.Key).ToList()
                ) // .AccoThemeFilter(themelist.Where(x => x.Value == true).Select(x => x.Key).ToList())
                .AccoBoardIdsFilterOr_GeneratedColumn(boardlist) //.AccoBoardFilter(boardlist)
                .AccoApartmentFilter_GeneratedColumn(apartmentfilter) //.AccoApartmentFilter(apartmentfilter)
                .AccoIsBookableFilter_GeneratedColumn(bookable)
                // FILTERS Available Marketinggroup, LTSFeature, BookingPortal
                .When(
                    languagelist.Count > 0,
                    q => q.HasLanguageFilterAnd_GeneratedColumn(languagelist)
                ) //.HasLanguageFilter(languagelist)
                .ActiveFilter_GeneratedColumn(activefilter) //OK GENERATED COLUMNS //.ActiveFilter(activefilter)
                .OdhActiveFilter_GeneratedColumn(smgactivefilter) //OK GENERATED COLUMNS //.SmgActiveFilter(smgactivefilter)
                .When(smgtaglist.Count > 0, q => q.SmgTagFilterOr_GeneratedColumn(smgtaglist)) //OK GENERATED COLUMNS //.SmgTagFilter(smgtaglist)
                .PublishedOnFilter_GeneratedColumn(publishedonlist) //.PublishedOnFilter(publishedonlist)
                .LastChangedFilter_GeneratedColumn(lastchange)
                .SourceFilter_GeneratedColumn(sourcelist)
                .DistrictFilter(districtlist)
                .LocFilterMunicipalityFilter(municipalitylist)
                .LocFilterTvsFilter(tourismvereinlist)
                .LocFilterRegionFilter(regionlist)
                .AccoAltitudeFilter(altitude, altitudemin, altitudemax)
                .SearchFilter(AccoTitleFieldsToSearchFor(language, languagelist), searchfilter)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles)
                .FilterReducedDataByRoles(userroles);
        }

        //Return Where and Parameters for Common
        public static Query CommonWhereExpression(
            this Query query,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> languagelist,
            bool? visibleinsearch,
            IReadOnlyCollection<string> smgtaglist,
            bool? activefilter,
            bool? odhactivefilter,
            IReadOnlyCollection<string> publishedonlist,
            IReadOnlyCollection<string> sourcelist,
            string? searchfilter,
            string? language,
            string? lastchange,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                idlist,
                languagelist,
                searchfilter,
                language,
                lastchange,
                activefilter,
                odhactivefilter,
                lastchange,
                sourcelist
            );

            return query
                .IdUpperFilter(idlist)
                .PublishedOnFilter_GeneratedColumn(publishedonlist) //.PublishedOnFilter(publishedonlist)
                .LastChangedFilter_GeneratedColumn(lastchange)
                .ActiveFilter_GeneratedColumn(activefilter) //OK GENERATED COLUMNS //.ActiveFilter(activefilter)
                .OdhActiveFilter_GeneratedColumn(odhactivefilter) //OK GENERATED COLUMNS //.SmgActiveFilter(smgactivefilter)
                .When(smgtaglist.Count > 0, q => q.SmgTagFilterOr_GeneratedColumn(smgtaglist)) //OK GENERATED COLUMNS //.SmgTagFilter(smgtaglist)
                .When(
                    languagelist.Count > 0,
                    q => q.HasLanguageFilterAnd_GeneratedColumn(languagelist)
                ) //.HasLanguageFilter(languagelist)
                .SourceFilter_GeneratedColumn(sourcelist)
                .VisibleInSearchFilter(visibleinsearch)
                .SearchFilter(TitleFieldsToSearchFor(language, languagelist), searchfilter)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles);
        }

        //Return Where and Parameters for Wine
        public static Query WineWhereExpression(
            this Query query,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> companyid,
            IReadOnlyCollection<string> wineid,
            bool? activefilter,
            bool? odhactivefilter,
            IReadOnlyCollection<string> sourcelist,
            string? searchfilter,
            string? language,
            string? lastchange,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                searchfilter,
                language,
                lastchange,
                sourcelist
            );

            return query
                .When(
                    languagelist.Count > 0,
                    q => q.HasLanguageFilterAnd_GeneratedColumn(languagelist)
                ) //.HasLanguageFilter(languagelist)
                .SearchFilter(TitleFieldsToSearchFor(language, languagelist), searchfilter)
                .LastChangedFilter_GeneratedColumn(lastchange)
                .ActiveFilter_GeneratedColumn(activefilter) //OK GENERATED COLUMNS //.ActiveFilter(activefilter)
                .OdhActiveFilter_GeneratedColumn(odhactivefilter) //OK GENERATED COLUMNS //.SmgActiveFilter(smgactivefilter)
                .SourceFilter_GeneratedColumn(sourcelist)
                .CompanyIdFilter(companyid)
                .WineIdFilter(wineid)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles);
        }

        //Return Where and Parameters for WebCamInfo
        public static Query WebCamInfoWhereExpression(
            this Query query,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> sourcelist,
            bool? activefilter,
            bool? smgactivefilter,
            IReadOnlyCollection<string> publishedonlist,
            string? searchfilter,
            string? language,
            string? lastchange,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                idlist,
                sourcelist,
                activefilter,
                smgactivefilter,
                searchfilter,
                language,
                lastchange
            );

            return query
                .IdUpperFilter(idlist)
                .SyncSourceInterfaceFilter_GeneratedColumn(sourcelist)
                .ActiveFilter_GeneratedColumn(activefilter) //OK GENERATED COLUMNS //.ActiveFilter(activefilter)
                .OdhActiveFilter_GeneratedColumn(smgactivefilter) //OK GENERATED COLUMNS //.SmgActiveFilter(smgactivefilter)
                .PublishedOnFilter_GeneratedColumn(publishedonlist) //.PublishedOnFilter(publishedonlist)
                .LastChangedFilter_GeneratedColumn(lastchange)
                //.When(filterClosedData, q => q.FilterClosedData_GeneratedColumn());
                .SearchFilter(TitleFieldsToSearchFor(language, languagelist), searchfilter)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles)
                .FilterReducedDataByRoles(userroles);
        }

        public static Query WeatherHistoryWhereExpression(
            this Query query,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> sourcelist,
            DateTime? begindate,
            DateTime? enddate,
            string? searchfilter,
            string? language,
            string? lastchange,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                idlist,
                sourcelist,
                begindate,
                enddate,
                searchfilter,
                language,
                lastchange
            );

            return query
                .IdUpperFilter(idlist)
                .SyncSourceInterfaceFilter_GeneratedColumn(sourcelist)
                .When(
                    languagelist.Count > 0,
                    q => q.HasLanguageFilterAnd_GeneratedColumn(languagelist)
                )
                .LastChangedFilter_GeneratedColumn(begindate, enddate)
                .LastChangedFilter_GeneratedColumn(lastchange)
                .SearchFilter(WeatherHistoryFieldsToSearchFor(language), searchfilter)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles);
        }

        //Return Where and Parameters for Measuringpoint
        public static Query MeasuringpointWhereExpression(
            this Query query,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> districtlist,
            IReadOnlyCollection<string> municipalitylist,
            IReadOnlyCollection<string> tourismvereinlist,
            IReadOnlyCollection<string> regionlist,
            IReadOnlyCollection<string> arealist,
            IReadOnlyCollection<string> skiarealist,
            bool? activefilter,
            bool? smgactivefilter,
            IReadOnlyCollection<string> publishedonlist,
            IReadOnlyCollection<string> sourcelist,
            string? searchfilter,
            string? language,
            string? lastchange,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                idlist,
                districtlist,
                municipalitylist,
                tourismvereinlist,
                regionlist,
                arealist,
                activefilter,
                smgactivefilter,
                searchfilter,
                language,
                lastchange
            );

            return query
                .IdUpperFilter(idlist)
                .ActiveFilter_GeneratedColumn(activefilter) //OK GENERATED COLUMNS //.ActiveFilter(activefilter)
                .OdhActiveFilter_GeneratedColumn(smgactivefilter) //OK GENERATED COLUMNS //.SmgActiveFilter(smgactivefilter)
                .PublishedOnFilter_GeneratedColumn(publishedonlist) //.PublishedOnFilter(publishedonlist)
                .LastChangedFilter_GeneratedColumn(lastchange) //.LastChangedFilter(lastchange)
                .SourceFilter_GeneratedColumn(sourcelist)
                .LocFilterDistrictFilter(districtlist)
                .LocFilterMunicipalityFilter(municipalitylist)
                .LocFilterTvsFilter(tourismvereinlist)
                .LocFilterRegionFilter(regionlist)
                .AreaFilterMeasuringpoints(arealist)
                .SkiAreaFilterMeasuringpoints(skiarealist)
                .SearchFilter(new string[1] { $"Shortname" }, searchfilter) //Search only Shortname Field
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles)
                .FilterReducedDataByRoles(userroles);
        }

        //Return Where and Parameters for Eventshort
        public static Query EventShortWhereExpression(
            this Query query,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> sourcelist,
            IReadOnlyCollection<string> eventlocationlist,
            IReadOnlyCollection<string> webaddresslist,
            bool? activefilter,
            string? todayactivefilter,
            bool? websiteactivefilter,
            bool? communityactivefilter,
            DateTime? start,
            DateTime? end,
            IReadOnlyCollection<string> publishedonlist,
            string? searchfilter,
            string? language,
            string? lastchange,
            string? additionalfilter,
            IEnumerable<string> userroles,
            bool getbyrooms = false
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                idlist,
                sourcelist,
                eventlocationlist,
                webaddresslist,
                todayactivefilter,
                start,
                end,
                searchfilter,
                activefilter,
                language,
                lastchange
            );

            //TODO USE GENERATED COLUMNS ??

            return query
                .IdLowerFilter(idlist)
                .SyncSourceInterfaceFilter_GeneratedColumn(sourcelist)
                .EventShortLocationFilter(eventlocationlist)
                .EventShortWebaddressFilter(webaddresslist)
                .ActiveFilter_GeneratedColumn(activefilter)
                //.OdhActiveFilter_GeneratedColumn(smgactivefilter)   //OK GENERATED COLUMNS //.SmgActiveFilter(smgactivefilter)
                .EventShortTodayActiveFilter(todayactivefilter)
                .EventShortWebsiteActiveFilter(websiteactivefilter)
                .EventShortCommunityActiveFilter(communityactivefilter)
                //.EventShortDateFilterEnd(start, end, !getbyrooms)
                //.EventShortDateFilterBegin(start, end, !getbyrooms)
                .When(
                    !getbyrooms,
                    q => q.EventShortDateFilter_GeneratedColumn(start, end, true, true)
                )
                //.EventShortDateFilterEndByRoom(start, end, getbyrooms)
                //.EventShortDateFilterBeginByRoom(start, end, getbyrooms)
                .When(
                    getbyrooms,
                    q => q.EventShortDateFilter_GeneratedColumn(start, end, true, false)
                )
                .PublishedOnFilter_GeneratedColumn(publishedonlist) //.PublishedOnFilter(publishedonlist)
                .When(
                    languagelist.Count > 0,
                    q => q.HasLanguageFilterAnd_GeneratedColumn(languagelist)
                ) //.HasLanguageFilter(languagelist)
                .SearchFilter(
                    EventShortTitleFieldsToSearchFor(language, languagelist),
                    searchfilter
                ) //TODO here the title is in another field
                .LastChangedFilter_GeneratedColumn(lastchange)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles);
        }

        //Return Where and Parameters for Venue
        public static Query VenueWhereExpression(
            this Query query,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> categorylist,
            IReadOnlyCollection<string> featurelist,
            IReadOnlyCollection<string> setuptypelist,
            IReadOnlyCollection<string> smgtaglist,
            IReadOnlyCollection<string> districtlist,
            IReadOnlyCollection<string> municipalitylist,
            IReadOnlyCollection<string> tourismvereinlist,
            IReadOnlyCollection<string> regionlist,
            IReadOnlyCollection<string> sourcelist,
            bool capacity,
            int capacitymin,
            int capacitymax,
            bool roomcount,
            int roomcountmin,
            int roomcountmax,
            bool? activefilter,
            bool? smgactivefilter,
            IReadOnlyCollection<string> publishedonlist,
            string? searchfilter,
            string? language,
            string? lastchange,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                idlist,
                categorylist,
                featurelist,
                setuptypelist,
                smgtaglist,
                districtlist,
                municipalitylist,
                tourismvereinlist,
                regionlist,
                sourcelist,
                languagelist,
                capacity,
                capacitymin,
                capacitymax,
                roomcount,
                roomcountmin,
                roomcountmax,
                activefilter,
                smgactivefilter,
                searchfilter,
                language,
                lastchange
            );

            //TODO
            return query
                .IdUpperFilter(idlist)
                .ActiveFilter_GeneratedColumn(activefilter) //OK GENERATED COLUMNS //.VenueActiveFilter(activefilter)
                .OdhActiveFilter_GeneratedColumn(smgactivefilter) //OK GENERATED COLUMNS //.VenueODHActiveFilter(smgactivefilter)
                .When(smgtaglist.Count > 0, q => q.SmgTagFilterOr_GeneratedColumn(smgtaglist)) //OK GENERATED COLUMNS //.VenueODHTagFilter(smgtaglist)
                .LastChangedFilter_GeneratedColumn(lastchange) //.VenueLastChangedFilter(lastchange)
                .SourceFilter_GeneratedColumn(sourcelist)
                .When(
                    languagelist.Count > 0,
                    q => q.HasLanguageFilterAnd_GeneratedColumn(languagelist)
                ) //.VenueHasLanguageFilter(languagelist)
                .PublishedOnFilter_GeneratedColumn(publishedonlist) //.PublishedOnFilter(publishedonlist)
                .LocFilterDistrictFilter(districtlist) //.VenueLocFilterDistrictFilter(districtlist)
                .LocFilterMunicipalityFilter(municipalitylist) //.VenueLocFilterMunicipalityFilter(municipalitylist)
                .LocFilterTvsFilter(tourismvereinlist) //.VenueLocFilterTvsFilter(tourismvereinlist)
                .LocFilterRegionFilter(regionlist) //.VenueLocFilterRegionFilter(regionlist)
                .VenueCategoryFilter(categorylist)
                .VenueFeatureFilter(featurelist)
                .VenueSetupTypeFilter(setuptypelist)
                .VenueRoomCountFilter(roomcount, roomcountmin, roomcountmax)
                //TODO
                //.VenueCapacityFilter(capacity, capacitymin, capacitymax)
                .SearchFilter(TitleFieldsToSearchFor(language, languagelist), searchfilter)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles)
                .FilterReducedDataByRoles(userroles);
        }

        //Return Where and Parameters for AlpineBits
        public static Query AlpineBitsWhereExpression(
            this Query query,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> sourcelist,
            IReadOnlyCollection<string> accommodationIds,
            IReadOnlyCollection<string> messagetypelist,
            string? requestdate
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                idlist,
                sourcelist,
                accommodationIds,
                messagetypelist,
                requestdate
            );

            return query
                .IdIlikeFilter(idlist)
                .SourceFilterAlpineBits_GeneratedColumn(sourcelist) //.SourceFilterAlpineBits(sourcelist)
                .AlpineBitsMessageFilter_GeneratedColumn(messagetypelist) //AlpineBitsMessageFilter(messagetypelist)
                .AlpineBitsAccommodationIdFilter_GeneratedColumn(accommodationIds); //AlpineBitsAccommodationIdFilter(accommodationIds);
        }

        //Return Where and Parameters for OdhTag and Tag
        public static Query ODHTagWhereExpression(
            this Query query,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> mainentitylist,
            IReadOnlyCollection<string> validforentitylist,
            IReadOnlyCollection<string> sourcelist,
            bool? displayascategory,
            IReadOnlyCollection<string> publishedonlist,
            string? searchfilter,
            string? language,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                searchfilter,
                language,
                validforentitylist,
                mainentitylist,
                displayascategory
            );

            return query
                .SearchFilter(TagNameFieldsToSearchFor(language), searchfilter)
                //.SourceFilter_GeneratedColumn(sourcelist)
                .ODHTagSourcesFilter_GeneratedColumn(sourcelist) //Using custom source filter
                .PublishedOnFilter_GeneratedColumn(publishedonlist)
                .ODHTagValidForEntityFilter(mainentitylist)
                .ODHTagValidForEntityFilter(validforentitylist)
                .ODHTagDisplayAsCategoryFilter(displayascategory)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles);
        }

        //Return Where and Parameters for OdhTag and Tag
        public static Query TagWhereExpression(
            this Query query,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> typelist,
            IReadOnlyCollection<string> validforentitylist,
            IReadOnlyCollection<string> sourcelist,
            bool? displayascategory,
            IReadOnlyCollection<string> publishedonlist,
            string? searchfilter,
            string? language,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                searchfilter,
                language,
                validforentitylist,
                typelist,
                displayascategory
            );

            return query
                .SearchFilter(TagNameFieldsToSearchFor(language), searchfilter)
                .SourceFilter_GeneratedColumn(sourcelist)
                .PublishedOnFilter_GeneratedColumn(publishedonlist)
                .TagTypesFilter(typelist)
                .ODHTagValidForEntityFilter(validforentitylist)
                .ODHTagDisplayAsCategoryFilter(displayascategory)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles);
        }

        //Return Where and Parameters for OdhTag and Tag
        public static Query PublishersWhereExpression(
            this Query query,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> sourcelist,
            string? searchfilter,
            string? language,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                searchfilter,
                language,
                sourcelist
            );

            return query
                .SearchFilter(NameFieldsToSearchFor(language), searchfilter)
                .SourceFilter_GeneratedColumn(sourcelist)
                .When(idlist != null && idlist.Count > 0, q => query.WhereIn("id", idlist))
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles);
        }

        public static Query SourcesWhereExpression(
            this Query query,
            IReadOnlyCollection<string> languagelist,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> typeslist,
            string? searchfilter,
            string? language,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                searchfilter,
                language,
                idlist
            );

            return query
                .SearchFilter(NameFieldsToSearchFor(language), searchfilter)
                .When(idlist != null && idlist.Count > 0, q => query.WhereIn("id", idlist))
                .When(
                    typeslist != null && typeslist.Count > 0,
                    q => query.SourceTypeFilter(typeslist)
                )
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles);
        }

        public static Query PushResultWhereExpression(
            this Query query,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> publisherlist,
            DateTime? begin,
            DateTime? end,
            IReadOnlyCollection<string> objectidlist,
            IReadOnlyCollection<string> objecttypelist,
            string? additionalfilter
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                publisherlist,
                begin,
                end,
                idlist
            );

            return query
                .When(idlist != null && idlist.Count > 0, q => query.WhereIn("id", idlist))
                .When(
                    publisherlist != null && publisherlist.Count > 0,
                    q => query.WhereIn("gen_publisher", publisherlist)
                )
                .When(
                    objectidlist != null && objectidlist.Count > 0,
                    q => query.WhereIn("gen_objectid", objectidlist)
                )
                .When(
                    objecttypelist != null && objecttypelist.Count > 0,
                    q => query.WhereIn("gen_objecttype", objecttypelist)
                )
                .LastChangedFilter_GeneratedColumn(begin, end)
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                );
        }

        //Return Where and Parameters for Rawdata
        public static Query RawdataWhereExpression(
            this Query query,
            IReadOnlyCollection<string> idlist,
            IReadOnlyCollection<string> sourceidlist,
            IReadOnlyCollection<string> typelist,
            IReadOnlyCollection<string> sourcelist,
            string? additionalfilter,
            IEnumerable<string> userroles
        )
        {
            LogMethodInfo(
                System.Reflection.MethodBase.GetCurrentMethod()!,
                "<query>", // not interested in query
                idlist,
                sourceidlist,
                typelist,
                sourcelist
            );

            return query
                .When(typelist != null, q => query.WhereIn("type", typelist))
                .When(sourcelist != null, q => query.WhereIn("datasource", sourcelist))
                .When(sourceidlist != null, q => query.WhereIn("sourceid", sourceidlist))
                .When(idlist != null, q => query.WhereIn("id", idlist))
                //.When(latest, )
                //.When(filterClosedData, q => q.FilterClosedData_Raw());
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .FilterDataByAccessRoles(userroles);
            //TODO future opendata rules on
            //.Anonymous_Logged_UserRule_GeneratedColumn(filterClosedData, !reducedData);
        }
    }
}
