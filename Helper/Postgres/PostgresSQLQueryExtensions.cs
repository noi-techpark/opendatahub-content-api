// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Auth.AccessControlPolicy;
using DataModel;
using Newtonsoft.Json;
using SqlKata;
using SqlKata.Execution;

namespace Helper
{
    public static class PostgresSQLQueryExtensions
    {
        /// <summary>
        /// Convert a (simple) JsonPath path to a Postgres array,
        /// which can be used in the #>> operator.<br />
        /// E.g. Detail.de.Title => Detail,de,Title
        /// </summary>
        public static string JsonPathToPostgresArray(string field) => field.Replace('.', ',');

        public static Query WhereJsonb(
            this Query query,
            string jsonPath,
            string comparisonOperator,
            string value
        ) =>
            query.WhereRaw(
                $"data#>>'\\{{{JsonPathToPostgresArray(jsonPath)}\\}}' {comparisonOperator} $$",
                value
            );

        public static Query WhereJsonb(this Query query, string jsonPath, string value) =>
            query.WhereJsonb(jsonPath, "=", value);

        public static Query WhereJsonb(this Query query, string jsonPath, int value) =>
            query.WhereRaw(
                $"data#>>'\\{{{JsonPathToPostgresArray(jsonPath)}\\}}' = $$",
                value.ToString()
            );

        public static Query WhereJsonb(this Query query, string jsonPath, bool value) =>
            query.WhereRaw(
                $"data#>>'\\{{{JsonPathToPostgresArray(jsonPath)}\\}}' = $$",
                value ? "true" : "false"
            );

        public static Query OrWhereJsonb(this Query query, string jsonPath, string value) =>
            query.OrWhereRaw($"data#>>'\\{{{JsonPathToPostgresArray(jsonPath)}\\}}' = $$", value);

        public static Query OrWhereJsonb(this Query query, string jsonPath, int value) =>
            query.OrWhereRaw(
                $"data#>>'\\{{{JsonPathToPostgresArray(jsonPath)}\\}}' = $$",
                value.ToString()
            );

        public static Query OrWhereJsonb(this Query query, string jsonPath, bool value) =>
            query.OrWhereRaw(
                $"data#>>'\\{{{JsonPathToPostgresArray(jsonPath)}\\}}' = $$",
                value ? "true" : "false"
            );

        [Obsolete]
        public static Query WhereJsonb<T>(
            this Query query,
            T value,
            Func<T, object> jsonObjectConstructor
        ) =>
            query.WhereRaw(
                "data @> jsonb($$)",
                JsonConvert.SerializeObject(jsonObjectConstructor(value))
            );

        //[Obsolete]
        public static Query OrWhereJsonb<T>(
            this Query query,
            T value,
            Func<T, object> jsonObjectConstructor
        ) =>
            query.OrWhereRaw(
                "data @> jsonb($$)",
                JsonConvert.SerializeObject(jsonObjectConstructor(value))
            );

        //[Obsolete]
        //https://github.com/sqlkata/querybuilder/issues/356
        public static Query WhereInJsonb<T>(
            this Query query,
            IReadOnlyCollection<T> list,
            Func<T, object> jsonObjectConstructor
        ) =>
            query.Where(q =>
            {
                foreach (var item in list)
                {
                    q = q.OrWhereJsonb(value: item, jsonObjectConstructor);
                }
                return q;
            });

        public static Query WhereInJsonb<T>(
            this Query query,
            IReadOnlyCollection<T> list,
            string jsonPath,
            Func<T, string> jsonObjectConstructor
        ) =>
            query.Where(q =>
            {
                foreach (var item in list)
                {
                    q = q.OrWhereJsonb(jsonPath, jsonObjectConstructor(item));
                }
                return q;
            });

        public static Query WhereInJsonb<T>(
            this Query query,
            IReadOnlyCollection<T> list,
            string jsonPath
        )
        {
            if (list.Count == 0)
            {
                return query;
            }
            else
            {
                return query.WhereRaw(
                    $"data#>>'\\{{{JsonPathToPostgresArray(jsonPath)}\\}}' = ANY($$)",
                    new[] { new[] { list } }
                );
            }
        }

        [Obsolete]
        public static Query WhereAllInJsonb<T>(
            this Query query,
            IReadOnlyCollection<T> list,
            Func<T, object> jsonObjectConstructor
        ) =>
            query.Where(q =>
            {
                foreach (var item in list)
                {
                    q = q.WhereJsonb(value: item, jsonObjectConstructor);
                }
                return q;
            });

        public static Query DistrictFilter(
            this Query query,
            IReadOnlyCollection<string> districtlist
        ) => query.WhereInJsonb(list: districtlist, "DistrictId", id => id.ToUpper());

        public static Query IdUpperFilter(this Query query, IReadOnlyCollection<string> idlist) =>
            query.Where(q =>
            {
                foreach (var id in idlist)
                {
                    q = q.OrWhere("id", "=", id.ToUpper());
                }
                return q;
            });

        public static Query IdUpperFilterGenId(this Query query, IReadOnlyCollection<string> idlist) =>
            query.Where(q =>
            {
                foreach (var id in idlist)
                {
                    q = q.OrWhere("gen_id", "=", id.ToUpper());
                }
                return q;
            });

        public static Query IdUpperFilterJsonId(this Query query, IReadOnlyCollection<string> idlist) =>
           query.Where(q =>
           {
               foreach (var id in idlist)
               {
                   q = q.OrWhereRaw("data#>>'\\{Id\\}' = $$", id.ToUpper());
               }
               return q;
           });

        public static Query IdLowerFilter(this Query query, IReadOnlyCollection<string> idlist) =>
            query.Where(q =>
            {
                foreach (var id in idlist)
                {
                    q = q.OrWhere("id", "=", id.ToLower());
                }
                return q;
            });

        public static Query IdLowerFilterGenId(this Query query, IReadOnlyCollection<string> idlist) =>
            query.Where(q =>
            {
                foreach (var id in idlist)
                {
                    q = q.OrWhere("gen_id", "=", id.ToLower());
                }
                return q;
            });

        public static Query IdLowerFilterJsonId(this Query query, IReadOnlyCollection<string> idlist) =>
            query.Where(q =>
            {
                foreach (var id in idlist)
                {
                    q = q.OrWhereRaw("data#>>'\\{Id\\}' = $$", id.ToLower());
                }
                return q;
            });

        public static Query IdIlikeFilter(this Query query, IReadOnlyCollection<string> idlist) =>
            query.Where(q =>
            {
                foreach (var id in idlist)
                {
                    q = q.OrWhere("id", "ILIKE", id);
                }
                return q;
            });

        public static Query IdIlikeFilterGenId(this Query query, IReadOnlyCollection<string> idlist) =>
            query.Where(q =>
            {
                foreach (var id in idlist)
                {
                    q = q.OrWhere("gen_id", "ILIKE", id);
                }
                return q;
            });

        public static Query LastChangedFilter(this Query query, string? updatefrom) =>
            query.When(
                updatefrom != null,
                query =>
                    query.WhereRaw(
                        "to_date(data#>>'\\{LastChange\\}', 'YYYY-MM-DD') > date($$)",
                        updatefrom
                    )
            );

        public static Query LocFilterDistrictFilter(
            this Query query,
            IReadOnlyCollection<string> districtlist
        ) => query.WhereInJsonb(list: districtlist, jsonPath: "LocationInfo.DistrictInfo.Id");

        public static Query LocFilterMunicipalityFilter(
            this Query query,
            IReadOnlyCollection<string> municipalitylist
        ) =>
            query.WhereInJsonb(
                list: municipalitylist,
                jsonPath: "LocationInfo.MunicipalityInfo.Id"
            );

        public static Query LocFilterTvsFilter(
            this Query query,
            IReadOnlyCollection<string> tourismvereinlist
        ) => query.WhereInJsonb(list: tourismvereinlist, jsonPath: "LocationInfo.TvInfo.Id");

        public static Query LocFilterRegionFilter(
            this Query query,
            IReadOnlyCollection<string> regionlist
        ) => query.WhereInJsonb(list: regionlist, jsonPath: "LocationInfo.RegionInfo.Id");

        public static Query AreaFilter(this Query query, IReadOnlyCollection<string> arealist) =>
            query.WhereInJsonb(arealist, areaid => new { AreaId = new[] { areaid } });

        public static Query AreaFilterMeasuringpoints(
            this Query query,
            IReadOnlyCollection<string> arealist
        ) => query.WhereInJsonb(arealist, areaid => new { AreaIds = new[] { areaid } });

        public static Query SkiAreaFilterMeasuringpoints(
            this Query query,
            IReadOnlyCollection<string> skiarealist
        ) => query.WhereInJsonb(skiarealist, skiareaid => new { SkiAreaIds = new[] { skiareaid } });

        public static Query HighlightFilter(this Query query, bool? highlight) =>
            query.When(
                highlight != null,
                query => query.WhereJsonb("Highlight", highlight ?? false)
            );

        public static Query ActiveFilter(this Query query, bool? active) =>
            query.When(active != null, query => query.WhereJsonb("Active", active ?? false));

        public static Query SmgActiveFilter(this Query query, bool? smgactive) =>
            query.When(
                smgactive != null,
                query => query.WhereJsonb("SmgActive", smgactive ?? false)
            );

        public static Query DistanceFilter(
            this Query query,
            bool distance,
            int distancemin,
            int distancemax
        ) =>
            query.When(
                distance,
                query =>
                    query.WhereRaw(
                        "(data#>>'\\{DistanceLength\\}')::numeric > $$ AND (data#>>'\\{DistanceLength\\}')::numeric < $$",
                        distancemin,
                        distancemax
                    )
            );

        public static Query DurationFilter(
            this Query query,
            bool duration,
            double durationmin,
            double durationmax
        ) =>
            query.When(
                duration,
                query =>
                    query.WhereRaw(
                        "(data#>>'\\{DistanceDuration\\}')::numeric > $$ AND (data#>>'\\{DistanceDuration\\}')::numeric < $$",
                        durationmin,
                        durationmax
                    )
            );

        public static Query AltitudeFilter(
            this Query query,
            bool altitude,
            int altitudemin,
            int altitudemax
        ) =>
            query.When(
                altitude,
                query =>
                    query.WhereRaw(
                        "(data#>>'\\{AltitudeDifference\\}')::numeric > $$ AND (data#>>'\\{AltitudeDifference\\}')::numeric < $$",
                        altitudemin,
                        altitudemax
                    )
            );

        public static Query SmgTagFilter(
            this Query query,
            IReadOnlyCollection<string> smgtaglist
        ) => query.WhereInJsonb(smgtaglist, tag => new { SmgTags = new[] { tag.ToLower() } });

        //NOT WORKING
        //public static Query SmgTagFilter(this Query query, IReadOnlyCollection<string> smgtaglist) =>
        //    query.WhereInJsonb(
        //        smgtaglist,
        //        "SmgTags",
        //        tag => tag.ToLower()
        //);

        public static Query SearchFilter(this Query query, string[] fields, string? searchfilter) =>
            query.When(
                searchfilter != null && fields.Length > 0,
                query =>
                    query.Where(q =>
                    {
                        foreach (var field in fields)
                        {
                            q = q.OrWhereRaw(
                                $"data#>>'\\{{{JsonPathToPostgresArray(field)}\\}}' ILIKE $$",
                                $"%{searchfilter}%"
                            );
                        }
                        return q.OrWhereRaw("id ILIKE $$", searchfilter);
                    })
            );

        //Better Version of the Search Filter
        public static Query SearchFilterWithGenId(this Query query, string[] fields, string? searchfilter) =>
            query.When(
                searchfilter != null && fields.Length > 0,
                query =>
                    query.Where(q =>
                    {
                        foreach (var field in fields)
                        {
                            q = q.OrWhereRaw(
                                $"data#>>'\\{{{JsonPathToPostgresArray(field)}\\}}' ILIKE $$",
                                $"%{searchfilter}%"
                            );
                        }
                        return q.OrWhereRaw("gen_id ILIKE $$", searchfilter);
                    })
            );

        public static Query ActivityTypeFilter(
            this Query query,
            IReadOnlyCollection<string> activitytypelist
        ) => query.WhereInJsonb(list: activitytypelist, "Type", type => type);

        public static Query ActivityTypeFilterOnTags(
            this Query query,
            IReadOnlyCollection<string> activitytypelist
        ) =>
            query.WhereInJsonb(
                list: activitytypelist,
                tag => new { SmgTags = new[] { tag.ToLower() } }
            );

        public static Query ActivitySubTypeFilterOnTags(
            this Query query,
            IReadOnlyCollection<string> subtypelist
        ) =>
            query.WhereInJsonb(list: subtypelist, tag => new { SmgTags = new[] { tag.ToLower() } });

        public static Query DifficultyFilter(
            this Query query,
            IReadOnlyCollection<string> difficultylist
        ) => query.WhereInJsonb(list: difficultylist, jsonPath: "Difficulty");

        public static Query PoiTypeFilterOnTags(
            this Query query,
            IReadOnlyCollection<string> poitypelist
        ) =>
            query.WhereInJsonb(
                poitypelist,
                poitype => new { SmgTags = new[] { poitype.ToLower() } }
            );

        public static Query PoiSubTypeFilterOnTags(
            this Query query,
            IReadOnlyCollection<string> subtypelist
        ) =>
            query.WhereInJsonb(
                subtypelist,
                poitype => new { SmgTags = new[] { poitype.ToLower() } }
            );

        public static Query MetaRegionFilter(
            this Query query,
            IReadOnlyCollection<string> metaregionlist
        ) => query.WhereInJsonb(metaregionlist, "Id", id => id.ToUpper());

        // TODO Add correct filters
        public static Query CuisineCodeFilter(
            this Query query,
            IReadOnlyCollection<string> cuisinecodelist
        ) =>
            query.WhereInJsonb(
                cuisinecodelist,
                tag => new { Facilities = new[] { new { Id = tag.ToUpper() } } }
            );

        public static Query CeremonyCodeFilter(
            this Query query,
            IReadOnlyCollection<string> ceremonycodelist
        ) =>
            query.WhereInJsonb(
                ceremonycodelist,
                tag => new { CapacityCeremony = new[] { new { Id = tag.ToUpper() } } }
            );

        public static Query CategoryCodeFilter(
            this Query query,
            IReadOnlyCollection<string> categorycodelist
        ) =>
            query.WhereInJsonb(
                categorycodelist,
                tag => new { CategoryCodes = new[] { new { Id = tag.ToUpper() } } }
            );

        public static Query DishCodeFilter(
            this Query query,
            IReadOnlyCollection<string> dishcodelist
        ) =>
            query.WhereInJsonb(
                dishcodelist,
                tag => new { DishRates = new[] { new { Id = tag.ToUpper() } } }
            );

        public static Query SourceFilter(
            this Query query,
            IReadOnlyCollection<string> sourcelist
        ) => query.WhereInJsonb(list: sourcelist, "Source", id => id.ToUpper());

        //For Alpinebits
        public static Query SourceFilterAlpineBits(
            this Query query,
            IReadOnlyCollection<string> sourcelist
        ) => query.WhereInJsonb(list: sourcelist, "Source", id => id.ToLower());

        public static Query SyncSourceInterfaceFilter(
            this Query query,
            IReadOnlyCollection<string> sourcelist
        ) => query.WhereInJsonb(list: sourcelist, "SyncSourceInterface", id => id.ToUpper());

        public static Query SourceFilterMeta(
            this Query query,
            IReadOnlyCollection<string> sourcelist
        ) => query.WhereInJsonb(list: sourcelist, "_Meta.Source", id => id.ToLower());

        //not working
        //public static Query HasLanguageFilter(this Query query, IReadOnlyCollection<string> languagelist) =>
        //    query.WhereInJsonb(
        //        list: languagelist,
        //        "HasLanguage",
        //        id => id.ToUpper()
        //    );

        public static Query HasLanguageFilter(
            this Query query,
            IReadOnlyCollection<string> languagelist
        ) =>
            query.WhereInJsonb(
                languagelist,
                lang => new { HasLanguage = new[] { lang.ToLower() } }
            );

        public static Query ODHActivityPoiTypeFilter(
            this Query query,
            IReadOnlyCollection<string> typelist
        ) => query.WhereInJsonb(list: typelist, "Type", type => type);

        public static Query ODHActivityPoiSubTypeFilter(
            this Query query,
            IReadOnlyCollection<string> subtypelist
        ) => query.WhereInJsonb(list: subtypelist, "SubType", type => type);

        public static Query ODHActivityPoiPoiTypeFilter(
            this Query query,
            IReadOnlyCollection<string> poitypelist
        ) => query.WhereInJsonb(list: poitypelist, "PoiType", type => type);

        //To change, ODH Type Filtering based on
        public static Query ODHActivityPoiTypeFilterOnTags(
            this Query query,
            IReadOnlyCollection<string> typelist
        ) => query.WhereInJsonb(typelist, type => new { SmgTags = new[] { type.ToLower() } });

        public static Query ODHActivityPoiSubTypeFilterOnTags(
            this Query query,
            IReadOnlyCollection<string> subtypelist
        ) =>
            query.WhereInJsonb(
                subtypelist,
                subtype => new { SmgTags = new[] { subtype.ToLower() } }
            );

        public static Query ODHActivityPoiPoiTypeFilterOnTags(
            this Query query,
            IReadOnlyCollection<string> poitypelist
        ) =>
            query.WhereInJsonb(
                poitypelist,
                poitype => new { SmgTags = new[] { poitype.ToLower() } }
            );

        public static Query EventTopicFilter(
            this Query query,
            IReadOnlyCollection<string> eventtopiclist
        ) =>
            query.WhereInJsonb(
                eventtopiclist,
                topic => new { TopicRIDs = new[] { topic.ToUpper() } }
            );

        public static Query EventTypeFilter(
            this Query query,
            IReadOnlyCollection<string> eventtypelist
        ) => query.WhereInJsonb(list: eventtypelist, "Type", type => type);

        //Obsolete
        public static Query EventRancFilter(
            this Query query,
            IReadOnlyCollection<string> eventrancfilterlist
        ) => query.WhereInJsonb(list: eventrancfilterlist, "Ranc", ranc => ranc);

        public static Query EventPublisherRancFilter(
            this Query query,
            IReadOnlyCollection<Int32> eventrancfilterlist
        ) =>
            query.WhereInJsonb(
                eventrancfilterlist,
                ranc => new { EventPublisher = new[] { new { Ranc = ranc } } }
            );

        public static Query EventOrgFilter(
            this Query query,
            IReadOnlyCollection<string> eventorgfilter
        ) => query.WhereInJsonb(list: eventorgfilter, "OrgRID", org => org);

        //Only Begindate given
        public static Query EventDateFilterBegin(
            this Query query,
            DateTime? begin,
            DateTime? end
        ) =>
            query.When(
                begin != DateTime.MinValue && end == DateTime.MaxValue,
                query =>
                    query.WhereRaw(
                        "((begindate >= '"
                            + String.Format("{0:yyyy-MM-dd}", begin)
                            + "' AND begindate < '"
                            + String.Format("{0:yyyy-MM-dd}", end)
                            + "') OR(enddate >= '"
                            + String.Format("{0:yyyy-MM-dd}", begin)
                            + "' AND enddate < '"
                            + String.Format("{0:yyyy-MM-dd}", end)
                            + "'))"
                    )
            );

        //Only Enddate given
        public static Query EventDateFilterEnd(this Query query, DateTime? begin, DateTime? end) =>
            query.When(
                begin == DateTime.MinValue && end != DateTime.MaxValue,
                query =>
                    query.WhereRaw(
                        "((begindate > '"
                            + String.Format("{0:yyyy-MM-dd}", begin)
                            + "' AND begindate < '"
                            + String.Format("{0:yyyy-MM-dd}", end?.AddDays(1))
                            + "') OR (enddate > '"
                            + String.Format("{0:yyyy-MM-dd}", begin)
                            + "' AND enddate < '"
                            + String.Format("{0:yyyy-MM-dd}", end?.AddDays(1))
                            + "'))"
                    )
            );

        //Both Begin and Enddate given
        public static Query EventDateFilterBeginEnd(
            this Query query,
            DateTime? begin,
            DateTime? end
        ) =>
            query.When(
                begin != DateTime.MinValue && end != DateTime.MaxValue,
                query =>
                    query.WhereRaw(
                        "((begindate >= '"
                            + String.Format("{0:yyyy-MM-dd}", begin)
                            + "' AND begindate < '"
                            + String.Format("{0:yyyy-MM-dd}", end?.AddDays(1))
                            + "') OR (enddate >= '"
                            + String.Format("{0:yyyy-MM-dd}", begin)
                            + "' AND enddate < '"
                            + String.Format("{0:yyyy-MM-dd}", end?.AddDays(1))
                            + "'))"
                    )
            );

        public static Query VisibleInSearchFilter(this Query query, bool? visibleinsearch) =>
            query.When(
                visibleinsearch != null,
                query => query.WhereJsonb("VisibleInSearch", visibleinsearch ?? false)
            );

        public static Query CompanyIdFilter(
            this Query query,
            IReadOnlyCollection<string> companyidfilter
        ) => query.WhereInJsonb(list: companyidfilter, "CompanyId", compid => compid);

        public static Query WineIdFilter(
            this Query query,
            IReadOnlyCollection<string> wineidfilter
        ) => query.WhereInJsonb(list: wineidfilter, "CustomId", wineid => wineid);

        public static Query AccoAltitudeFilter(
            this Query query,
            bool altitude,
            int altitudemin,
            int altitudemax
        ) =>
            query.When(
                altitude,
                query =>
                    query.WhereRaw(
                        "(data#>>'\\{Altitude\\}')::numeric > $$ AND (data#>>'\\{Altitude\\}')::numeric < $$",
                        altitudemin,
                        altitudemax
                    )
            );

        public static Query AccoTypeFilter(
            this Query query,
            IReadOnlyCollection<string> accotypefilter
        ) => query.WhereInJsonb(list: accotypefilter, "AccoTypeId", wineid => wineid);

        public static Query AccoBoardFilter(
            this Query query,
            IReadOnlyCollection<string> boardlist
        ) => query.WhereInJsonb(boardlist, board => new { BoardIds = new[] { board.ToLower() } });

        public static Query AccoBadgeFilter(
            this Query query,
            IReadOnlyCollection<string> badgelist
        ) => query.WhereInJsonb(badgelist, badge => new { BadgeIds = new[] { badge.ToLower() } });

        public static Query AccoLTSFeatureFilter(
            this Query query,
            IReadOnlyCollection<string> ltsfeaturelist
        ) =>
            query.WhereInJsonb(
                ltsfeaturelist,
                feature => new { Features = new[] { new { Id = feature.ToUpper() } } }
            );

        public static Query AccoMarketinggroupFilter(
            this Query query,
            IReadOnlyCollection<string> marketinggrouplist
        ) =>
            query.WhereInJsonb(
                marketinggrouplist,
                marketinggroup => new { MarketingGroupIds = new[] { marketinggroup } }
            );

        public static Query AccoBookingPortalFeatureFilter(
            this Query query,
            IReadOnlyCollection<string> bookingportallist
        ) =>
            query.WhereInJsonb(
                bookingportallist,
                bookingportal => new { AccoBookingChannel = new[] { new { Id = bookingportal } } }
            );

        //THEMEFILTER is AND connected
        public static Query AccoThemeFilter(
            this Query query,
            IReadOnlyCollection<string> themelist
        ) => query.WhereAllInJsonb(themelist, theme => new { ThemeIds = new[] { theme } });

        //TODO THEMEFILTER is AND connected
        public static Query AccoFeatureFilter(
            this Query query,
            IReadOnlyCollection<string> featurelist
        ) =>
            query.WhereAllInJsonb(
                featurelist,
                feature => new { SpecialFeaturesIds = new[] { feature } }
            );

        public static Query AccoCategoryFilter(
            this Query query,
            IReadOnlyCollection<string> categorylist
        ) => query.WhereInJsonb(list: categorylist, "AccoCategoryId", category => category);

        public static Query AccoFeatureIdFilter(
            this Query query,
            IReadOnlyCollection<string> featureidlist
        ) =>
            query.WhereInJsonb(
                featureidlist,
                tag => new { Features = new[] { new { Id = tag.ToUpper() } } }
            );

        public static Query AccoApartmentFilter(this Query query, bool? apartment) =>
            query.When(
                apartment != null,
                query => query.WhereJsonb("HasApartment", apartment ?? false)
            );

        public static Query AccoBookableFilter(this Query query, bool? bookable) =>
            query.When(
                bookable != null,
                query => query.WhereJsonb("IsBookable", bookable ?? false)
            );

        public static Query EventShortLocationFilter(
            this Query query,
            IReadOnlyCollection<string> eventlocationlist
        ) => query.WhereInJsonb(list: eventlocationlist, "EventLocation", id => id.ToUpper());

        public static Query EventShortWebaddressFilter(
            this Query query,
            IReadOnlyCollection<string> webaddresslist
        ) => query.WhereInJsonb(list: webaddresslist, "WebAddress", id => id);

        public static Query EventShortTodayActiveFilter(this Query query, string? active) =>
            query.When(active != null, query => query.WhereJsonb("Display1", active ?? ""));

        public static Query EventShortWebsiteActiveFilter(this Query query, bool? websiteactive) =>
            query.When(
                websiteactive != null,
                query => query.WhereJsonb("ActiveWeb", websiteactive ?? false)
            );

        public static Query EventShortCommunityActiveFilter(
            this Query query,
            bool? communityactive
        ) =>
            query.When(
                communityactive != null,
                query => query.WhereJsonb("ActiveCommunityApp", communityactive ?? false)
            );

        ////Only Begindate given
        //public static Query EventShortDateFilterBegin(this Query query, DateTime? start, DateTime? end, bool active) =>
        //    query.When(
        //        start != DateTime.MinValue && end == DateTime.MaxValue && active,
        //        query => query.WhereRaw(
        //            "((to_timestamp(data ->> 'EndDate', 'YYYY-MM-DD T HH24:MI:SS') >= '" + String.Format("{0:yyyy-MM-dd HH:mm:ss}", start) + "'))"
        //        )
        //    );

        ////Only Enddate given
        //public static Query EventShortDateFilterEnd(this Query query, DateTime? start, DateTime? end, bool active) =>
        //    query.When(
        //        start == DateTime.MinValue && end != DateTime.MaxValue && active,
        //        query => query.WhereRaw(
        //            "((to_timestamp(data->> 'EndDate', 'YYYY-MM-DD T HH24:MI:SS') <= '" + String.Format("{0:yyyy-MM-dd HH:mm:ss}", end) + "'))"
        //        )
        //    );

        //Both Begin and Enddate given
        public static Query EventShortDateFilterBeginEnd(
            this Query query,
            DateTime? start,
            DateTime? end,
            bool active
        ) =>
            //begin and enddate given
            query
                .When(
                    start != DateTime.MinValue && end != DateTime.MaxValue && active,
                    query =>
                        query.WhereRaw(
                            "(((to_timestamp(data ->> 'StartDate', 'YYYY-MM-DD T HH24:MI:SS') >= '"
                                + String.Format("{0:yyyy-MM-dd HH:mm:ss}", start)
                                + "') AND (to_timestamp(data->> 'EndDate', 'YYYY-MM-DD T HH24:MI:SS') <= '"
                                + String.Format("{0:yyyy-MM-dd HH:mm:ss}", end)
                                + "')))"
                        )
                )
                //only begindate given
                .When(
                    start != DateTime.MinValue && end == DateTime.MaxValue && active,
                    query =>
                        query.WhereRaw(
                            "((to_timestamp(data ->> 'EndDate', 'YYYY-MM-DD T HH24:MI:SS') >= '"
                                + String.Format("{0:yyyy-MM-dd HH:mm:ss}", start)
                                + "'))"
                        )
                )
                //only enddate given
                .When(
                    start == DateTime.MinValue && end != DateTime.MaxValue && active,
                    query =>
                        query.WhereRaw(
                            "((to_timestamp(data->> 'EndDate', 'YYYY-MM-DD T HH24:MI:SS') <= '"
                                + String.Format("{0:yyyy-MM-dd HH:mm:ss}", end)
                                + "'))"
                        )
                );

        //Both Begin and Enddate given which allows Today Query (In behaviour)
        public static Query EventShortDateFilterBeginEndWithInBehaviour(
            this Query query,
            DateTime? start,
            DateTime? end,
            bool active
        ) =>
            query.When(
                start != DateTime.MinValue && end != DateTime.MaxValue && active,
                query =>
                    query.WhereRaw(
                        "(((to_timestamp(data ->> 'EndDate', 'YYYY-MM-DD T HH24:MI:SS') >= '"
                            + String.Format("{0:yyyy-MM-dd HH:mm:ss}", start)
                            + "') AND (to_timestamp(data->> 'StartDate', 'YYYY-MM-DD T HH24:MI:SS') <= '"
                            + String.Format("{0:yyyy-MM-dd HH:mm:ss}", end)
                            + "')))"
                    )
            );

        ////Only Begindate given
        //public static Query EventShortDateFilterBeginByRoom(this Query query, DateTime? start, DateTime? end, bool active) =>
        //    query.When(
        //        start != DateTime.MinValue && end == DateTime.MaxValue && active,
        //        query => query.WhereRaw(
        //            "((to_date(data ->> 'EndDate', 'YYYY-MM-DD') >= '" + String.Format("{0:yyyy-MM-dd}", start) + "'))"
        //        )
        //    );

        ////Only Enddate given
        //public static Query EventShortDateFilterEndByRoom(this Query query, DateTime? start, DateTime? end, bool active) =>
        //    query.When(
        //        start == DateTime.MinValue && end != DateTime.MaxValue && active,
        //        query => query.WhereRaw(
        //            "((to_date(data->> 'EndDate', 'YYYY-MM-DD') <= '" + String.Format("{0:yyyy-MM-dd}", end) + "'))"
        //        )
        //    );

        //Both Begin and Enddate given
        public static Query EventShortDateFilterBeginEndByRoom(
            this Query query,
            DateTime? start,
            DateTime? end,
            bool active
        ) =>
            //begindate and enddate given
            query
                .When(
                    start != DateTime.MinValue && end != DateTime.MaxValue && active,
                    query =>
                        query.WhereRaw(
                            "(((to_date(data ->> 'EndDate', 'YYYY-MM-DD') >= '"
                                + String.Format("{0:yyyy-MM-dd}", start)
                                + "') AND (to_date(data->> 'StartDate', 'YYYY-MM-DD') <= '"
                                + String.Format("{0:yyyy-MM-dd}", end)
                                + "')))"
                        )
                )
                //only begindate given
                .When(
                    start != DateTime.MinValue && end == DateTime.MaxValue && active,
                    query =>
                        query.WhereRaw(
                            "((to_date(data ->> 'EndDate', 'YYYY-MM-DD') >= '"
                                + String.Format("{0:yyyy-MM-dd}", start)
                                + "'))"
                        )
                )
                //only enddate given
                .When(
                    start == DateTime.MinValue && end != DateTime.MaxValue && active,
                    query =>
                        query.WhereRaw(
                            "((to_date(data->> 'EndDate', 'YYYY-MM-DD') <= '"
                                + String.Format("{0:yyyy-MM-dd}", end)
                                + "'))"
                        )
                );

        public static Query ODHTagMainEntityFilter(
            this Query query,
            IReadOnlyCollection<string> mainentitylist
        ) => query.WhereInJsonb(list: mainentitylist, "MainEntity", id => id.ToLower());

        public static Query ODHTagValidForEntityFilter(
            this Query query,
            IReadOnlyCollection<string> validforentitylist
        ) =>
            query.WhereInJsonb(
                validforentitylist,
                validforentity => new { ValidForEntity = new[] { validforentity.ToLower() } }
            );

        public static Query TagTypesFilter(
            this Query query,
            IReadOnlyCollection<string> tagtypeslist
        ) =>
            query.WhereInJsonb(
                tagtypeslist,
                tagtype => new { Types = new[] { tagtype.ToLower() } }
            );

        public static Query ODHTagDisplayAsCategoryFilter(
            this Query query,
            bool? displayascategory
        ) =>
            query.When(
                displayascategory != null,
                query => query.WhereJsonb("DisplayAsCategory", displayascategory ?? false)
            );

        //AlpineBits
        public static Query AlpineBitsAccommodationIdFilter(
            this Query query,
            IReadOnlyCollection<string> accommodationids
        ) => query.WhereInJsonb(list: accommodationids, "AccommodationId", id => id);

        //AlpineBits
        public static Query AlpineBitsMessageFilter(
            this Query query,
            IReadOnlyCollection<string> messagetypelist
        ) => query.WhereInJsonb(list: messagetypelist, "MessageType", id => id);

        //Venue Filters (Special case)

        //public static Query VenueLocFilterDistrictFilter(this Query query, IReadOnlyCollection<string> districtlist) =>
        //  query.WhereInJsonb(
        //      list: districtlist,
        //      jsonPath: "odhdata.LocationInfo.DistrictInfo.Id"
        //  );

        //public static Query VenueLocFilterMunicipalityFilter(this Query query, IReadOnlyCollection<string> municipalitylist) =>
        //    query.WhereInJsonb(
        //        list: municipalitylist,
        //        jsonPath: "odhdata.LocationInfo.MunicipalityInfo.Id"
        //    );

        //public static Query VenueLocFilterTvsFilter(this Query query, IReadOnlyCollection<string> tourismvereinlist) =>
        //    query.WhereInJsonb(
        //        list: tourismvereinlist,
        //        jsonPath: "odhdata.LocationInfo.TvInfo.Id"
        //    );

        //public static Query VenueLocFilterRegionFilter(this Query query, IReadOnlyCollection<string> regionlist) =>
        //    query.WhereInJsonb(
        //        list: regionlist,
        //        jsonPath: "odhdata.LocationInfo.RegionInfo.Id"
        //    );

        //public static Query VenueActiveFilter(this Query query, bool? active) =>
        //    query.When(
        //        active != null,
        //        query => query.WhereJsonb(
        //            "odhdata.Active",
        //            active ?? false
        //        )
        //    );

        //public static Query VenueODHActiveFilter(this Query query, bool? odhactive) =>
        //    query.When(
        //        odhactive != null,
        //        query => query.WhereJsonb(
        //            "odhdata.ODHActive",
        //            odhactive ?? false
        //        )
        //    );

        public static Query VenueCategoryFilter(
            this Query query,
            IReadOnlyCollection<string> categorylist
        ) =>
            query.WhereInJsonb(
                categorylist,
                tag => new { VenueCategory = new[] { new { Id = tag.ToUpper() } } }
            );

        public static Query VenueFeatureFilter(
            this Query query,
            IReadOnlyCollection<string> featurelist
        ) =>
            query.WhereInJsonb(
                featurelist,
                tag => new
                {
                    RoomDetails = new[]
                    {
                        new { VenueFeatures = new[] { new { Id = tag.ToUpper() } } },
                    },
                }
            );

        public static Query VenueSetupTypeFilter(
            this Query query,
            IReadOnlyCollection<string> setuptypelist
        ) =>
            query.WhereInJsonb(
                setuptypelist,
                tag => new
                {
                    RoomDetails = new[]
                    {
                        new { VenueSetup = new[] { new { Id = tag.ToUpper() } } },
                    },
                }
            );

        public static Query VenueRoomCountFilter(
            this Query query,
            bool roomcount,
            int roomcountmin,
            int roomcountmax
        ) =>
            query.When(
                roomcount,
                query =>
                    query.WhereRaw(
                        "(data#>>'\\{RoomCount\\}')::numeric > ? AND (data#>>'\\{RoomCount\\}')::numeric < ?",
                        roomcountmin,
                        roomcountmax
                    )
            );

        //public static Query VenueODHTagFilter(this Query query, IReadOnlyCollection<string> smgtaglist) =>
        //  query.WhereInJsonb(
        //      smgtaglist,
        //      tag => new { odhdata = new { ODHTags = new[] { tag.ToLower() } } }
        //  );

        //public static Query VenueLastChangedFilter(this Query query, string? updatefrom) =>
        //   query.When(
        //       updatefrom != null,
        //       query => query.WhereRaw(
        //           "to_date(data#>>'\\{meta,lastUpdate\\}', 'YYYY-MM-DD') > date($$)",
        //           updatefrom
        //       )
        //   );

        public static Query VenueSourceFilter(
            this Query query,
            IReadOnlyCollection<string> sourcelist
        ) => query.WhereInJsonb(list: sourcelist, "SyncSourceInterface", id => id.ToUpper());

        public static Query SourceTypeFilter(
            this Query query,
            IReadOnlyCollection<string> typeslist
        ) =>
            query.WhereInJsonb(
                typeslist,
                sourcetype => new { Types = new[] { sourcetype.ToLower() } }
            );

        //public static Query VenueHasLanguageFilter(this Query query, IReadOnlyCollection<string> languagelist) =>
        //    query.WhereInJsonb(
        //       languagelist,
        //       lang => new { odhdata = new { HasLanguage = new[] { lang.ToLower() } } }
        //   );

        //private static Query VenueCapacityFilterWhere(bool capacity, int capacitymin, int capacitymax)
        //{
        //    //TODO!!!
        //    if (capacity)
        //    {
        //        //if (!String.IsNullOrEmpty(whereexpression))
        //        //    whereexpression = whereexpression + " AND ";

        //        //whereexpression = whereexpression + "(data ->'odhdata' ->> 'RoomCount')::numeric > @roomcountmin AND (data ->'odhdata' ->> 'RoomCount')::numeric < @roomcountmax";
        //        //parameters.Add(new PGParameters() { Name = "roomcountmin", Type = NpgsqlTypes.NpgsqlDbType.Numeric, Value = roomcountmin.ToString() });
        //        //parameters.Add(new PGParameters() { Name = "roomcountmax", Type = NpgsqlTypes.NpgsqlDbType.Numeric, Value = roomcountmax.ToString() });
        //    }

        //}

        //public static Query PublishedOnFilter(this Query query, IReadOnlyCollection<string> publishedonlist) =>
        //   query.WhereInJsonb(
        //       publishedonlist,
        //       publishedon => new { PublishedOn = new[] { publishedon.ToLower() } }
        //   );


        //Standard JSON Filter
        //public static Query FilterClosedData(this Query query) =>
        //    query.Where(q =>
        //        q.WhereRaw(
        //            "data#>>'\\{LicenseInfo,ClosedData\\}' IS NULL"
        //        ).OrWhereRaw(
        //            "data#>>'\\{LicenseInfo,ClosedData\\}' = 'false'"
        //        )
        //    );

        //public static Query FilterClosedDataVenues(this Query query) =>
        //    query.Where(q =>
        //        q.WhereRaw(
        //            "data#>>'\\{odhdata,LicenseInfo,ClosedData\\}' IS NULL"
        //        ).OrWhereRaw(
        //            "data#>>'\\{odhdata,LicenseInfo,ClosedData\\}' = 'false'"
        //        )
        //    );

        #region Tagging

        public static Query TaggingFilter_OR(
            this Query query,
            string tagkey,
            IReadOnlyCollection<string> taglist
        ) =>
            query.Where(q =>
                taglist.Aggregate(
                    q,
                    (q, tag) =>
                        q.OrWhereRaw(
                            @$"(data->>'Tags')::jsonb @? '$.{tagkey}\[*\] ? (@.Id == ""{tag}"")'"
                        )
                )
            );

        public static Query TaggingFilter_AND(
            this Query query,
            string tagkey,
            IReadOnlyCollection<string> taglist
        ) =>
            query.Where(q =>
                taglist.Aggregate(
                    q,
                    (q, tag) =>
                        q.WhereRaw(
                            @$"(data->>'Tags')::jsonb @? '$.{tagkey}\[*\] ? (@.Id == ""{tag}"")'"
                        )
                )
            );

        public static Query TaggingFilter_OR(
            this Query query,
            IDictionary<string, string> tagdict
        ) =>
            query.Where(q =>
                tagdict.Aggregate(
                    q,
                    (q, tag) =>
                        q.OrWhereRaw(
                            @$"(data->>'Tags')::jsonb @? '$.{tag.Value}\[*\] ? (@.Id == ""{tag.Key}"")'"
                        )
                )
            );

        public static Query TaggingFilter_AND(
            this Query query,
            IDictionary<string, string> tagdict
        ) =>
            query.Where(q =>
                tagdict.Aggregate(
                    q,
                    (q, tag) =>
                        q.WhereRaw(
                            @$"(data->>'Tags')::jsonb @? '$.{tag.Value}\[*\] ? (@.Id == ""{tag.Key}"")'"
                        )
                )
            );

        #endregion

        #region Tagging Generated Column

        public static Query TaggingFilter_GeneratedColumn(
            this Query query,
            IDictionary<string, List<string>> tags
        ) =>
            query.Where(q =>
                tags.Aggregate(
                    q,
                    (q, tag) =>
                        q.When(
                            tag.Key == "and",
                            v => q.WhereArrayInListAnd(tag.Value, "gen_tags"),
                            v => q.WhereArrayInListOr(tag.Value, "gen_tags")
                        )
                )
            );

        //? q.WhereArrayInListAnd(tag.Value, "gen_tags") : q.WhereArrayInListOr(tag.Value, "gen_tags"))

        #endregion


        #region Generated Columns Basic

        //Where Array is in List OR
        public static Query WhereArrayInListOr(
            this Query query,
            IReadOnlyCollection<string> list,
            string generatedcolumn
        ) =>
            query.Where(q =>
            {
                foreach (var item in list)
                {
                    q = q.OrWhereRaw(generatedcolumn + " @> array\\[$$\\]", item.ToLower());
                }
                return q;
            });

        //Where Array is in List AND
        public static Query WhereArrayInListAnd(
            this Query query,
            IReadOnlyCollection<string> list,
            string generatedcolumn
        ) =>
            query.Where(q =>
                q.WhereRaw(generatedcolumn + " @> array\\[$$\\]", list.Select(x => x.ToLower()))
            );

        //Where Array is in List OR alternative
        public static Query WhereStringInListOr(
            this Query query,
            IReadOnlyCollection<string> list,
            string generatedcolumn
        ) =>
            query.Where(q =>
            {
                foreach (var item in list)
                {
                    q = q.OrWhereRaw(generatedcolumn + " = $$", item.ToLower());
                }
                return q;
            });

        #endregion

        #region Generated Columns Where Expressions

        //Filter on Generated Field gen_licenseinfo_closeddata
        public static Query FilterClosedData_GeneratedColumn(this Query query) =>
            query.Where(q =>
                q.WhereRaw("gen_licenseinfo_closeddata IS NULL")
                    .OrWhereRaw("gen_licenseinfo_closeddata = false")
            );

        //Filter on Generated Field gen_haslanguage AND
        public static Query HasLanguageFilterAnd_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> languagelist
        ) =>
            query.Where(q =>
                q.WhereRaw(
                    "gen_haslanguage @> array\\[$$\\]",
                    languagelist.Select(x => x.ToLower())
                )
            );

        //Filter on Generated Field gen_haslanguage AND
        public static Query HasLanguageFilterOr_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> languagelist
        ) =>
            query.Where(q =>
            {
                foreach (var item in languagelist)
                {
                    q = q.OrWhereRaw("gen_haslanguage @> array\\[$$\\]", item.ToLower());
                }
                return q;
            });

        //Filter on Generated Field gen_smgtags AND
        public static Query SmgTagFilterAnd_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> list
        ) =>
            query.Where(q =>
                q.WhereRaw("gen_smgtags @> array\\[$$\\]", list.Select(x => x.ToLower()))
            );

        //Filter on Generated Field gen_smgtags OR
        public static Query SmgTagFilterOr_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> list
        ) =>
            query.Where(q =>
            {
                foreach (var item in list)
                {
                    q = q.OrWhereRaw("gen_smgtags @> array\\[$$\\]", item.ToLower());
                }
                return q;
            });

        //Filter on Generated Field gen_articletype OR
        public static Query ArticleTypeFilterOr_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> list
        ) =>
            query.Where(q =>
            {
                foreach (var item in list)
                {
                    q = q.OrWhereRaw("gen_articletype @> array\\[$$\\]", item.ToLower());
                }
                return q;
            });

        //Filter on Generated Field gen_active
        public static Query ActiveFilter_GeneratedColumn(this Query query, bool? active) =>
            query.When(active != null, query => query.WhereRaw("gen_active = $$", active ?? false));

        //Filter on Generated Field gen_odhactive
        public static Query OdhActiveFilter_GeneratedColumn(this Query query, bool? odhactive) =>
            query.When(
                odhactive != null,
                query => query.WhereRaw("gen_odhactive = $$", odhactive ?? false)
            );

        //Filter on Generated Field gen_odhactive
        public static Query HasImage_GeneratedColumn(this Query query, bool? odhactive) =>
            query.When(
                odhactive != null,
                query => query.WhereRaw("gen_hasimage = $$", odhactive ?? false)
            );

        //Filter on Generated Field gen_eventtopic OR
        public static Query EventTopicFilter_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> eventtopiclist
        ) =>
            query.Where(q =>
            {
                foreach (var item in eventtopiclist)
                {
                    q = q.OrWhereRaw("gen_eventtopic @> array\\[$$\\]", item.ToUpper());
                }
                return q;
            });

        //Filter on Generated Field gen_boardids
        public static Query AccoBoardIdsFilterOr_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> boardids
        ) =>
            query.Where(q =>
            {
                foreach (var item in boardids)
                {
                    q = q.OrWhereRaw("gen_boardids @> array\\[$$\\]", item);
                }
                return q;
            });

        //Filter on Generated Field gen_featureids
        public static Query AccoFeatureIdsFilterOr_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> featureids
        ) =>
            query.Where(q =>
            {
                foreach (var item in featureids)
                {
                    q = q.OrWhereRaw("gen_featureids @> array\\[$$\\]", item);
                }
                return q;
            });

        //Filter on Generated Field gen_specialfeatureids
        public static Query AccoSpecialFeatureIdsFilterOr_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> specialfeatureids
        ) =>
            query.Where(q =>
            {
                foreach (var item in specialfeatureids)
                {
                    q = q.OrWhereRaw("gen_specialfeatureids @> array\\[$$\\]", item);
                }
                return q;
            });

        //Filter on Generated Field gen_badgeids
        public static Query AccoBadgeIdsFilterOr_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> boardids
        ) =>
            query.Where(q =>
            {
                foreach (var item in boardids)
                {
                    q = q.OrWhereRaw("gen_badgeids @> array\\[$$\\]", item);
                }
                return q;
            });

        //Filter on Generated Field gen_themeids
        public static Query AccoThemeIdsFilterOr_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> boardids
        ) =>
            query.Where(q =>
            {
                foreach (var item in boardids)
                {
                    q = q.OrWhereRaw("gen_themeids @> array\\[$$\\]", item);
                }
                return q;
            });

        //Filter on Generated Field gen_accotype
        public static Query AccoTypeFilter_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> accotype
        ) =>
            query.Where(q =>
            {
                foreach (var item in accotype)
                {
                    q = q.OrWhereRaw("gen_accotype = $$", item);
                }
                return q;
            });

        //Filter on Generated Field gen_accocategory
        public static Query AccoCategoryFilter_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> accocategory
        ) =>
            query.Where(q =>
            {
                foreach (var item in accocategory)
                {
                    q = q.OrWhereRaw("gen_accocategory = $$", item);
                }
                return q;
            });

        //Filter on Generated Field gen_hasapartment
        public static Query AccoApartmentFilter_GeneratedColumn(
            this Query query,
            bool? hasapartment
        ) =>
            query.When(
                hasapartment != null,
                query => query.WhereRaw("gen_hasapartment = $$", hasapartment ?? false)
            );

        //Filter on Generated Field gen_isbookable
        public static Query AccoIsBookableFilter_GeneratedColumn(
            this Query query,
            bool? isbookable
        ) =>
            query.When(
                isbookable != null,
                query => query.WhereRaw("gen_isbookable = $$", isbookable ?? false)
            );

        //Filter on Generated Field gen_altitude
        public static Query AccoAltitudeFilter_GeneratedColumn(
            this Query query,
            bool altitude,
            int altitudemin,
            int altitudemax
        ) =>
            query.When(
                altitude,
                query =>
                    query.WhereRaw(
                        "(gen_altitude)::numeric > $$ AND (gen_altitude)::numeric < $$",
                        altitudemin,
                        altitudemax
                    )
            );

        //Filter on Generated Field gen_lastchange
        public static Query LastChangedFilter_GeneratedColumn(
            this Query query,
            string? updatefrom
        ) =>
            query.When(
                //DISPLAY ERRROR OR IGNORE DATE?
                //updatefrom != null && DateTime.TryParseExact(updatefrom, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime updatefromparsed),
                //updatefrom != null && DateTime.TryParse(updatefrom, out DateTime updatefromparsed),
                updatefrom != null,
                query =>
                    query.WhereRaw(
                        //"to_date(gen_lastchange, 'YYYY-MM-DD') > date($$)",
                        "gen_lastchange > date($$)",
                        updatefrom
                    )
            );

        //Weatherhistory lastchangedBetween
        //Filter on Generated Field gen_lastchange
        public static Query LastChangedFilter_GeneratedColumn(
            this Query query,
            DateTime? updatefrom,
            DateTime? updateto
        ) =>
            query.When(
                updatefrom != null && updateto != null,
                query =>
                    query.WhereRaw(
                        //"to_date(gen_lastchange, 'YYYY-MM-DD') > date($$)",
                        "gen_lastchange > date($$) AND gen_lastchange < date($$)",
                        updatefrom,
                        updateto
                    )
            );

        //Source Filter (SyncSourceInterface)
        public static Query SyncSourceInterfaceFilter_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> sourcelist
        ) =>
            query.Where(q =>
            {
                foreach (var source in sourcelist)
                {
                    q = q.OrWhere("gen_syncsourceinterface", "ILIKE", source);
                }
                return q;
            });

        //Source Filter (SyncSourceInterface)
        public static Query SourceFilter_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> sourcelist
        ) =>
            query.Where(q =>
            {
                foreach (var source in sourcelist)
                {
                    q = q.OrWhere("gen_source", "ILIKE", source);
                }
                return q;
            });

        //Source Filter (SyncSourceInterface OR Source)
        public static Query SourceOrSyncSourceInterfaceFilter_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> sourcelist
        ) =>
            query.Where(q =>
            {
                foreach (var source in sourcelist)
                {
                    q = q.OrWhere("gen_syncsourceinterface", "ILIKE", source).OrWhere("gen_source", "ILIKE", source);
                }
                return q;
            });

        public static Query ODHTagSourcesFilter_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> sourcelist
        ) =>
            query.Where(q =>
            {
                foreach (var item in sourcelist)
                {
                    q = q.OrWhereRaw("gen_sources @> array\\[$$\\]", item)
                        .OrWhere("gen_source", "ILIKE", item);
                }
                return q;
            });

        //PublishedOn Filter (SyncSourceInterface)
        public static Query PublishedOnFilter_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> publishedonlist
        ) =>
            query.Where(q =>
            {
                foreach (var item in publishedonlist)
                {
                    q = q.OrWhereRaw("gen_publishedon @> array\\[$$\\]", item.ToLower());
                }
                return q;
            });

        //Source Filter for Alpinebits
        public static Query SourceFilterAlpineBits_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> sourcelist
        ) =>
            query.Where(q =>
            {
                foreach (var source in sourcelist)
                {
                    q = q.OrWhere("gen_source", "ILIKE", source);
                }
                return q;
            });

        //AlpineBits
        public static Query AlpineBitsAccommodationIdFilter_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> accommodationids
        ) =>
            query.Where(q =>
            {
                foreach (var item in accommodationids)
                {
                    q = q.OrWhere("gen_accommodation_id", "=", item);
                }
                return q;
            });

        //AlpineBits
        public static Query AlpineBitsMessageFilter_GeneratedColumn(
            this Query query,
            IReadOnlyCollection<string> messagetypelist
        ) =>
            query.Where(q =>
            {
                foreach (var item in messagetypelist)
                {
                    q = q.OrWhereRaw("gen_messagetype = $$", item);
                }
                return q;
            });

        //public static Query TaggingFilter_OR_GeneratedColumn(this Query query, IDictionary<string, string> tagdict) =>
        //   query.Where(q =>
        //       tagdict.Aggregate(q, (q, tag) =>
        //           q.OrWhereRaw(@$"(data->>'Tags')::jsonb @? '$.{tag.Value}\[*\] ? (@.Id == ""{tag.Key}"")'")));

        //public static Query TaggingFilter_AND_GeneratedColumn(this Query query, IDictionary<string, string> tagdict) =>
        //    query.Where(q =>
        //        tagdict.Aggregate(q, (q, tag) =>
        //            q.WhereRaw(@$"(data->>'Tags')::jsonb @? '$.{tag.Value}\[*\] ? (@.Id == ""{tag.Key}"")'")));

        #endregion

        #region Rawdata Where Experession

        public static Query FilterClosedData_Raw(this Query query) =>
            query.Where(q => q.Where("license", "open"));

        #endregion

        #region Date_Query_Helpers Event

        //Events
        //EVENTS Usecase to check

        //Integrated in Query
        //Only Begindate given
        //public static Query EventDateFilterBegin_GeneratedColumn(this Query query, DateTime? begin, DateTime? end) =>
        //    query.When(
        //        begin != DateTime.MinValue && end == DateTime.MaxValue,
        //        query => query.WhereRaw(
        //            "((gen_begindate >= '" + String.Format("{0:yyyy-MM-dd}", begin) + "' AND gen_begindate < '" + String.Format("{0:yyyy-MM-dd}", end) + "') OR(gen_enddate >= '" + String.Format("{0:yyyy-MM-dd}", begin) + "'))"
        //        )
        //    );

        //Integrated in Query
        //Only Enddate given
        //public static Query EventDateFilterEnd_GeneratedColumn(this Query query, DateTime? begin, DateTime? end) =>
        //    query.When(
        //        begin == DateTime.MinValue && end != DateTime.MaxValue,
        //        query => query.WhereRaw(
        //            "((gen_begindate > '" + String.Format("{0:yyyy-MM-dd}", begin) + "' AND gen_begindate < '" + String.Format("{0:yyyy-MM-dd}", end?.AddDays(1)) + "') OR (gen_enddate > '" + String.Format("{0:yyyy-MM-dd}", begin) + "' AND gen_enddate < '" + String.Format("{0:yyyy-MM-dd}", end?.AddDays(1)) + "'))"
        //        )
        //    );

        //Both Begin and Enddate given
        public static Query EventDateFilterBeginEnd_GeneratedColumn(
            this Query query,
            DateTime? begin,
            DateTime? end
        ) =>
            query
                //Begin and enddate given
                .When(
                    begin != DateTime.MinValue && end != DateTime.MaxValue,
                    query =>
                        query.WhereRaw(
                            "((gen_begindate >= '"
                                + String.Format("{0:yyyy-MM-dd}", begin)
                                + "' AND gen_begindate < '"
                                + String.Format("{0:yyyy-MM-dd}", end?.AddDays(1))
                                + "') OR (gen_enddate >= '"
                                + String.Format("{0:yyyy-MM-dd}", begin)
                                + "' AND gen_enddate < '"
                                + String.Format("{0:yyyy-MM-dd}", end?.AddDays(1))
                                + "'))"
                        )
                )
                //only Begindate given
                .When(
                    begin != DateTime.MinValue && end == DateTime.MaxValue,
                    query =>
                        query.WhereRaw(
                            "((gen_begindate >= '"
                                + String.Format("{0:yyyy-MM-dd}", begin)
                                + "' AND gen_begindate < '"
                                + String.Format("{0:yyyy-MM-dd}", end)
                                + "') OR(gen_enddate >= '"
                                + String.Format("{0:yyyy-MM-dd}", begin)
                                + "'))"
                        )
                )
                //only enddate given
                .When(
                    begin == DateTime.MinValue && end != DateTime.MaxValue,
                    query =>
                        query.WhereRaw(
                            "((gen_begindate > '"
                                + String.Format("{0:yyyy-MM-dd}", begin)
                                + "' AND gen_begindate < '"
                                + String.Format("{0:yyyy-MM-dd}", end?.AddDays(1))
                                + "') OR (gen_enddate > '"
                                + String.Format("{0:yyyy-MM-dd}", begin)
                                + "' AND gen_enddate < '"
                                + String.Format("{0:yyyy-MM-dd}", end?.AddDays(1))
                                + "'))"
                        )
                );

        ////EVENT Array of Tuples(begindate, enddate)
        ////Both Begin and Enddate given
        //public static Query EventDateFilterBeginEndArray_GeneratedColumn(this Query query, DateTime? begin, DateTime? end) =>
        //    query.When(
        //        begin != DateTime.MinValue && end != DateTime.MaxValue,
        //        query =>



        //        query.WhereRaw(
        //            "((gen_begindate >= '" + String.Format("{0:yyyy-MM-dd}", begin) + "' AND gen_begindate < '" + String.Format("{0:yyyy-MM-dd}", end?.AddDays(1)) + "') OR (gen_enddate >= '" + String.Format("{0:yyyy-MM-dd}", begin) + "' AND gen_enddate < '" + String.Format("{0:yyyy-MM-dd}", end?.AddDays(1)) + "'))"
        //        )
        //    );

        #endregion

        #region Date_Query_Helpers Article

        //Article
        //NEWS Usecase: Begindate filter shows all News that ends after requested date, and all News which begin before the requested date

        ////Only Begindate given
        //public static Query ArticleDateNewsFilterBeginWithIN_GeneratedColumn(this Query query, DateTime? begin, DateTime? end) =>
        //    query.When(
        //        begin != DateTime.MinValue && end == DateTime.MaxValue,
        //        query => query.WhereRaw(
        //            "(gen_enddate >= '" + String.Format("{0:yyyy-MM-dd}", begin) + "' AND gen_begindate <= '" + String.Format("{0:yyyy-MM-dd}", begin) + "')"
        //        )
        //    );

        ////Only Enddate given
        //public static Query ArticleDateNewsFilterEndWithIN_GeneratedColumn(this Query query, DateTime? begin, DateTime? end) =>
        //    query.When(
        //        begin == DateTime.MinValue && end != DateTime.MaxValue,
        //        query => query.WhereRaw(
        //            "(gen_enddate >= '" + String.Format("{0:yyyy-MM-dd}", end) + "' AND gen_begindate <= '" + String.Format("{0:yyyy-MM-dd}", end) + "')"
        //        )
        //    );

        //Both Begindate and Enddate given
        public static Query ArticleDateNewsFilterBeginEndWithIN_GeneratedColumn(
            this Query query,
            DateTime? begin,
            DateTime? end
        ) =>
            query
                //begindate and enddate given
                .When(
                    begin != DateTime.MinValue && end != DateTime.MaxValue,
                    query =>
                        query.WhereRaw(
                            "(gen_enddate >= '"
                                + String.Format("{0:yyyy-MM-dd}", begin)
                                + "' AND gen_begindate <= '"
                                + String.Format("{0:yyyy-MM-dd}", end)
                                + "')"
                        )
                )
                //only begindate given
                .When(
                    begin != DateTime.MinValue && end == DateTime.MaxValue,
                    query =>
                        query.WhereRaw(
                            "(gen_enddate >= '"
                                + String.Format("{0:yyyy-MM-dd}", begin)
                                + "' AND gen_begindate <= '"
                                + String.Format("{0:yyyy-MM-dd}", begin)
                                + "')"
                        )
                )
                //only enddate given
                .When(
                    begin == DateTime.MinValue && end != DateTime.MaxValue,
                    query =>
                        query.WhereRaw(
                            "(gen_enddate >= '"
                                + String.Format("{0:yyyy-MM-dd}", end)
                                + "' AND gen_begindate <= '"
                                + String.Format("{0:yyyy-MM-dd}", end)
                                + "')"
                        )
                );

        #endregion

        #region Date_Query_Helpers EventShort

        //EventShort
        //EVENTSHORT Usecase: all events which ends after the requested date
        //REGULAR Behaviour = all events that begins after(=) the startdate and ends before (=) the enddate
        //IN Behaviour = all events that ends after the startdate and begins before enddate (All events that are NOW)

        ////Only Begindate given
        //public static Query EventShortDateFilterBegin_GeneratedColumn(this Query query, DateTime? start, DateTime? end, bool active) =>
        //    query.When(
        //        start != DateTime.MinValue && end == DateTime.MaxValue && active,
        //        query => query.WhereRaw(
        //            "(gen_enddate >= '" + String.Format("{0:yyyy-MM-dd HH:mm:ss}", start) + "'))"
        //        )
        //    );

        ////Only Enddate given
        //public static Query EventShortDateFilterEnd_GeneratedColumn(this Query query, DateTime? start, DateTime? end, bool active) =>
        //    query.When(
        //        start == DateTime.MinValue && end != DateTime.MaxValue && active,
        //        query => query.WhereRaw(
        //            "(gen_enddate <= '" + String.Format("{0:yyyy-MM-dd HH:mm:ss}", end) + "'))"
        //        )
        //    );

        //Both Begin and Enddate given
        public static Query EventShortDateFilterBeginEnd_GeneratedColumn(
            this Query query,
            DateTime? start,
            DateTime? end,
            bool active
        ) =>
            query
                //begindate and enddate
                .When(
                    start != DateTime.MinValue && end != DateTime.MaxValue && active,
                    query =>
                        query.WhereRaw(
                            "((gen_begindate >= '"
                                + String.Format("{0:yyyy-MM-dd HH:mm:ss}", start)
                                + "') AND (gen_enddate <= '"
                                + String.Format("{0:yyyy-MM-dd HH:mm:ss}", end)
                                + "'))"
                        )
                )
                //only begindate
                .When(
                    start != DateTime.MinValue && end == DateTime.MaxValue && active,
                    query =>
                        query.WhereRaw(
                            "(gen_enddate >= '"
                                + String.Format("{0:yyyy-MM-dd HH:mm:ss}", start)
                                + "'))"
                        )
                )
                //only enddate
                .When(
                    start == DateTime.MinValue && end != DateTime.MaxValue && active,
                    query =>
                        query.WhereRaw(
                            "(gen_enddate <= '"
                                + String.Format("{0:yyyy-MM-dd HH:mm:ss}", end)
                                + "'))"
                        )
                );

        ////Only Begindate given
        //public static Query EventShortDateFilterBeginByRoom_GeneratedColumn(this Query query, DateTime? start, DateTime? end, bool active) =>
        //    query.When(
        //        start != DateTime.MinValue && end == DateTime.MaxValue && active,
        //        query => query.WhereRaw(
        //            "(gen_enddate >= '" + String.Format("{0:yyyy-MM-dd}", start) + "'))"
        //        )
        //    );

        ////Only Enddate given
        //public static Query EventShortDateFilterEndByRoom_GeneratedColumn(this Query query, DateTime? start, DateTime? end, bool active) =>
        //    query.When(
        //        start == DateTime.MinValue && end != DateTime.MaxValue && active,
        //        query => query.WhereRaw(
        //            "(gen_enddate <= '" + String.Format("{0:yyyy-MM-dd}", end) + "'))"
        //        )
        //    );

        //Both Begin and Enddate given which allows Today Query (In behaviour)
        public static Query EventShortDateFilter_GeneratedColumn(
            this Query query,
            DateTime? start,
            DateTime? end,
            bool inbehaviour,
            bool checktime
        ) =>
            query
                //begindate and enddate given normal behaviour
                .When(
                    start != DateTime.MinValue
                        && end != DateTime.MaxValue
                        && !inbehaviour
                        && checktime,
                    query =>
                        query.WhereRaw(
                            "((gen_begindate >= '"
                                + String.Format("{0:yyyy-MM-dd HH:mm:ss}", start)
                                + "') AND (gen_enddate <= '"
                                + String.Format("{0:yyyy-MM-dd HH:mm:ss}", end)
                                + "'))"
                        )
                )
                //begindate and enddate given IN behaviour
                .When(
                    start != DateTime.MinValue
                        && end != DateTime.MaxValue
                        && inbehaviour
                        && checktime,
                    query =>
                        query.WhereRaw(
                            "((gen_enddate >= '"
                                + String.Format("{0:yyyy-MM-dd HH:mm:ss}", start)
                                + "') AND (gen_begindate <= '"
                                + String.Format("{0:yyyy-MM-dd HH:mm:ss}", end)
                                + "'))"
                        )
                )
                //only begindate given normal + IN behaviour is the same
                .When(
                    start != DateTime.MinValue && end == DateTime.MaxValue && checktime,
                    query =>
                        query.WhereRaw(
                            "(gen_enddate >= '"
                                + String.Format("{0:yyyy-MM-dd HH:mm:ss}", start)
                                + "')"
                        )
                )
                //only enddate given normal + IN behaviour is the same
                .When(
                    start == DateTime.MinValue && end != DateTime.MaxValue && checktime,
                    query =>
                        query.WhereRaw(
                            "(gen_enddate <= '"
                                + String.Format("{0:yyyy-MM-dd HH:mm:ss}", end)
                                + "')"
                        )
                )
                //begindate and enddate given normal behaviour without time check
                .When(
                    start != DateTime.MinValue
                        && end != DateTime.MaxValue
                        && !inbehaviour
                        && !checktime,
                    query =>
                        query.WhereRaw(
                            "((gen_begindate >= '"
                                + String.Format("{0:yyyy-MM-dd}", start)
                                + "') AND (gen_enddate <= '"
                                + String.Format("{0:yyyy-MM-dd}", end)
                                + "'))"
                        )
                )
                //begindate and enddate given IN behaviour without time check
                .When(
                    start != DateTime.MinValue
                        && end != DateTime.MaxValue
                        && inbehaviour
                        && !checktime,
                    query =>
                        query.WhereRaw(
                            "((gen_enddate >= '"
                                + String.Format("{0:yyyy-MM-dd}", start)
                                + "') AND (gen_begindate <= '"
                                + String.Format("{0:yyyy-MM-dd}", end)
                                + "'))"
                        )
                )
                //only begindate given
                .When(
                    start != DateTime.MinValue && end == DateTime.MaxValue && !checktime,
                    query =>
                        query.WhereRaw(
                            "(gen_enddate >= '" + String.Format("{0:yyyy-MM-dd}", start) + "')"
                        )
                )
                //only enddate given
                .When(
                    start == DateTime.MinValue && end != DateTime.MaxValue && !checktime,
                    query =>
                        query.WhereRaw(
                            "(gen_enddate <= '" + String.Format("{0:yyyy-MM-dd}", end) + "')"
                        )
                );

        #endregion

        #region DateTime_Query_Helpers_tsmultirange

        //Both Begin and Enddate given IN Behaviour

        public static Query DateFilter_GeneratedColumn(
            this Query query,
            DateTime? start,
            DateTime? end,
            string gen_tsrangecolumn
        ) =>
            query
                .When(
                    //Both given, Adds a Day to End, if Endday has no time given add a day
                    start != null
                        && end != null,
                    query =>
                        query.WhereRaw(
                            String.Format(
                                "gen_eventdates && tsrange('\\[{0},{1}\\]')",
                                String.Format("{0:yyyy-MM-dd HH:mm}", start),
                                String.Format(
                                    "{0:yyyy-MM-dd HH:mm}",
                                    end.Value.TimeOfDay == TimeSpan.Zero
                                        ? end.Value.AddDays(1)
                                        : end
                                )
                            )
                        )
                )
                .When(
                    //Only End given setting start to datetime min, if Endday has no time given add a day
                    start == null
                        && end != null,
                    query =>
                        query.WhereRaw(
                            String.Format(
                                "gen_eventdates && tsrange('\\[{0},{1}\\]')",
                                String.Format("{0:yyyy-MM-dd HH:mm}", DateTime.MinValue),
                                String.Format(
                                    "{0:yyyy-MM-dd HH:mm}",
                                    end.Value.TimeOfDay == TimeSpan.Zero
                                        ? end.Value.AddDays(1)
                                        : end
                                )
                            )
                        )
                )
                .When(
                    //Addds a Day to End
                    start != null
                        && end == null,
                    query =>
                        query.WhereRaw(
                            String.Format(
                                "gen_eventdates && tsrange('\\[{0},{1}\\]')",
                                String.Format("{0:yyyy-MM-dd HH:mm}", start),
                                String.Format("{0:yyyy-MM-dd HH:mm}", DateTime.MaxValue)
                            )
                        )
                );

        #endregion

        #region Opendata_LTS_Rules

        //anonymous -> where (closeddata = false and source != lts) OR (reduced = true and source = lts and cc0 = true)
        //logged -> where (source != lts) OR (reduced = true and source = lts)
        //idmuser -> where (source != lts) OR (reduced = false and source = lts)
        //public static Query Anonymous_Logged_UserRule_GeneratedColumn(this Query query, bool closeddatafilter, bool idmuser) =>
        //    idmuser ? query.FilterSourceReducedLogged(false) : closeddatafilter ? query.FilterSourceReducedAnonymous() : query.FilterSourceReducedLogged(true);

        //public static Query FilterSourceReducedLogged(this Query query, bool reduced) =>
        //    query.Where(q =>
        //        q.WhereRaw(
        //            "(gen_source <> 'lts')"
        //        ).OrWhereRaw(
        //            "(gen_source = 'lts' AND gen_reduced = $$)", reduced
        //        )
        //    );

        //public static Query FilterSourceReducedAnonymous(this Query query) =>
        //    query.Where(q =>
        //        q.WhereRaw(
        //            "(gen_source <> 'lts' AND (gen_licenseinfo_closeddata IS NULL OR gen_licenseinfo_closeddata = $$))", false
        //        ).OrWhereRaw(
        //            "(gen_source = 'lts' AND gen_reduced = true AND ((gen_licenseinfo_closeddata IS NULL OR gen_licenseinfo_closeddata = $$)))", false
        //        )
        //    );

        #endregion

        #region Filter by Roles


        //TO REVISIT
        //public static Query FilterDataByAccessRoles(this Query query, IEnumerable<string> roles) =>
        //    query.When(roles.Contains("ANONYMOUS"), q => q.Where(q =>
        //    {
        //        foreach (var item in roles)
        //        {
        //            q = q.OrWhereRaw(
        //                "gen_access_role @> array\\[$$\\]", item.ToUpper()
        //            );
        //        }

        //        return q;
        //    }));

        public static Query FilterDataByAccessRoles(this Query query, IEnumerable<string> roles) =>
            query.Where(q =>
            {
                foreach (var item in roles)
                {
                    q = q.OrWhereRaw("gen_access_role @> array\\[$$\\]", item.ToUpper());
                }

                return q;
            });

        ////Filter Out Reduced
        public static Query FilterReducedDataByRoles(this Query query, IEnumerable<string> roles) =>
            //Special Rule, if Role IDM is there filter out all reduced data
            query.When(roles.Any(x => x == "IDM"), q => q.Where("gen_reduced", false));

        #endregion

        #region AdditionalFilter

        public static Query FilterAdditionalDataByCondition(this Query query, string? condition)
        {
            if (condition != null && condition.Contains("="))
            {
                var splittedcondition = condition
                    .Split("&")
                    .Select(x => x.Split("="))
                    .Select(x => new ReadCondition() { Column = x[0], Value = x[1] });

                var itemstoadd = splittedcondition
                    .GroupBy(x => x.Column)
                    .Where(g => g.Count() == 1)
                    .ToList();
                foreach (var item in itemstoadd)
                {
                    query.GetAdditionalFilterQueryByInput(
                        item.Key,
                        item.Select(x => x.Value).FirstOrDefault()
                    );
                }

                var itemstomerge = splittedcondition
                    .GroupBy(x => x.Column)
                    .Where(g => g.Count() > 1)
                    .ToList();
                foreach (var item in itemstomerge)
                {
                    query.GetAdditionalFilterQueryByInput(
                        item.Key,
                        item.Select(x => x.Value).ToList()
                    );
                }
            }

            return query;
        }

        public static Query GetAdditionalFilterQueryByInput(
            this Query query,
            string input,
            string value
        )
        {
            switch (input)
            {
                case "source":
                    return query.Where("gen_source", value);
                case "accessrole":
                    return query.WhereRaw("gen_access_role @> array\\[$$\\]", value);
                default:
                    return query.Where("data->>'" + input + "'", value);
            }
        }

        public static Query GetAdditionalFilterQueryByInput(
            this Query query,
            string input,
            List<string> values
        )
        {
            switch (input)
            {
                case "source":
                    return query.WhereIn("gen_source", values);
                case "accessrole":
                    return query.FilterDataByAccessRoles(values);
                default:
                    return query.WhereIn("data->>'" + input + "'", values);
            }
        }

        #endregion
    }

    public class ReadCondition
    {
        public string Column { get; set; }
        public string Query { get; set; }
        public string Value { get; set; }
    }
}
