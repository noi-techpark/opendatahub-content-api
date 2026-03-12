// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using Helper;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiCore.Controllers
{
    /// <summary>
    /// <a href="https://github.com/noi-techpark/odh-docs/wiki/Search-over-all-Entities-in-ODH-Tourism-api" target="_blank">Wiki Search Api</a>
    /// </summary>
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class SearchController : OdhController
    {
        public SearchController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<SearchController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        #region SWAGGER Exposed API

        /// <summary>
        /// GET Search over all Entities
        /// </summary>
        /// <param name="type">Restrict search to Entities (accommodation, odhactivitypoi, event, webcam, measuringpoint, ltsactivity, ltspoi, ltsgastronomy, article ..... ) <a href="https://github.com/noi-techpark/odh-docs/wiki/Dataset-Type" target="_blank">Wiki Dataset Type</a></param>
        /// <param name="term">Term to Search for <a href="https://github.com/noi-techpark/odh-docs/wiki/Search-over-all-Entities-in-ODH-Tourism-api" target="_blank">Wiki</a></param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language ('null' all languages are displayed)</param>
        /// <param name="limitto">Limit search to n items per entity</param>
        /// <param name="searchbasetext">Search also trough base text (true/false), caution can slow down the search significantly</param>
        /// <param name="filteronfields">Search also on this fields, syntax analog to the fields filter</param>
        /// <param name="locfilter">Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a></param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/opendatahub-docs/wiki/Using-rawfilter-and-rawsort-on-the-Open-Data-Hub-Content-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/opendatahub-docs/wiki/Using-rawfilter-and-rawsort-on-the-Open-Data-Hub-Content-Api#rawfilter" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of ODHTag Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(IEnumerable<JsonRaw>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[OdhCacheOutput(ClientTimeSpan = 0, ServerTimeSpan = 3600, CacheKeyGenerator = typeof(CustomCacheKeyGenerator), MustRevalidate = true)]
        [HttpGet, Route("Find")]
        [HttpGet, Route("Filter")] //Filter
        //[Authorize(Roles = "DataReader,CommonReader,AccoReader,ActivityReader,PoiReader,ODHPoiReader,PackageReader,GastroReader,EventReader,ArticleReader")]
        public async Task<IActionResult> GetSearchAsync(
            string term,
            string? language = "en",
            string? odhtype = null,
            string? type = null,
            bool searchbasetext = false,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? filteronfields = null,
            string? locfilter = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? rawfilter = null,
            string? rawsort = null,
            int? limitto = 5,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            var fieldstodisplay = fields ?? Array.Empty<string>();
            var fieldstosearchon = filteronfields ?? Array.Empty<string>();

            return await Get(
                language: language ?? "en",
                validforentity: odhtype ?? type,
                fields: fieldstodisplay,
                searchfilter: term,
                searchontext: searchbasetext,
                passedfieldstosearchon: fieldstosearchon,
                locfilter: locfilter,
                rawfilter: rawfilter,
                rawsort: rawsort,
                limitto: limitto,
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        //TODO EXTEND THE FILTER with the possibility to add fields for search


        #endregion

        #region GETTER

        private Task<IActionResult> Get(
            string language,
            string? validforentity,
            string[] fields,
            string? searchfilter,
            bool searchontext,
            string[] passedfieldstosearchon,
            string? locfilter,
            string? rawfilter,
            string? rawsort,
            int? limitto,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            var myentitytypelist = (validforentity ?? "").Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries
            );

            if (myentitytypelist.Count() == 0)
                myentitytypelist = ODHTypeHelper.GetAllSearchableODHTypes(false);

            var searchresult = new List<JsonRaw>();
            var searchresultpertype = new Dictionary<string, uint>();

            return DoAsyncReturn(async () =>
            {
                foreach (var entitytype in myentitytypelist)
                {
                    var customfields = new string[]
                    {
                        "Id",
                        ODHTypeHelper.TranslateTypeToTitleField(entitytype, language),
                        "_Meta.Type",
                        "Self",
                    };

                    foreach (var field in fields)
                    {
                        customfields = customfields.AddToStringArray(field);
                    }

                    var result = await SearchTroughEntity(
                        entitytype,
                        ODHTypeHelper.TranslateTypeToSearchField(entitytype),
                        ODHTypeHelper.TranslateTypeString2Table(entitytype),
                        language,
                        customfields,
                        searchfilter,
                        searchontext,
                        passedfieldstosearchon,
                        locfilter,
                        rawfilter,
                        rawsort,
                        limitto,
                        removenullvalues,
                        cancellationToken
                    );

                    if (result != null)
                    {
                        searchresult.AddRange(result);
                        searchresultpertype.Add(entitytype, (uint)result.Count());
                    }
                }

                return new SearchResult<JsonRaw>
                {
                    Items = searchresult,
                    searchTerm = searchfilter,
                    totalResults = (uint)searchresult.Count,
                    detailedResults = searchresultpertype,
                };
            });
        }

        private async Task<IEnumerable<JsonRaw>> SearchTroughEntity(
            string entitytype,
            Func<string, string[]> fieldsearchfunc,
            string table,
            string language,
            string[] fields,
            string? searchfilter,
            bool searchontext,
            string[] passedfieldstosearchon,
            string? locfilter,
            string? rawfilter,
            string? rawsort,
            int? limitto,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            string endpoint = ODHTypeHelper.TranslateTypeToEndPoint(entitytype);

            //check if there are additionalfilters to add
            AdditionalFiltersToAddEndpoint(endpoint).TryGetValue("Read", out var additionalfilter);

            var searchonfields = fieldsearchfunc(language);

            //Add Textfields to search on if searchontext = true
            if (searchontext)
            {
                var textsearchfields = ODHTypeHelper.TranslateTypeToBaseTextField(
                    entitytype,
                    language
                );
                searchonfields = searchonfields.AddToStringArray(textsearchfields);
            }

            //Add selected Fields to search on
            if (passedfieldstosearchon.Length > 0)
            {
                searchonfields = searchonfields.AddToStringArray(passedfieldstosearchon);
            }

            //Locfilter extensions
            var tourismvereinlist = new List<string>();
            var regionlist = new List<string>();
            var municipalitylist = new List<string>();
            var districtlist = new List<string>();

            if (locfilter != null && locfilter.Contains("reg"))
                regionlist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "reg");
            if (locfilter != null && locfilter.Contains("tvs"))
                tourismvereinlist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "tvs");
            if (locfilter != null && locfilter.Contains("mun"))
                municipalitylist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "mun");
            if (locfilter != null && locfilter.Contains("fra"))
                districtlist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "fra");
            if (locfilter != null && locfilter.Contains("mta"))
            {
                List<string> metaregionlist = CommonListCreator.CreateDistrictIdList(
                    locfilter,
                    "mta"
                );
                tourismvereinlist.AddRange(
                    await GenericHelper.RetrieveLocFilterDataAsync(
                        QueryFactory,
                        metaregionlist,
                        cancellationToken
                    )
                );
            }
            //end locfilter extensions

            var query = QueryFactory
                .Query()
                .SelectRaw("data")
                .From(table)
                .SearchFilter(searchonfields, searchfilter)
                .LocFilterDistrictFilter(districtlist)
                .LocFilterMunicipalityFilter(municipalitylist)
                .LocFilterTvsFilter(tourismvereinlist)
                .LocFilterRegionFilter(regionlist)
                .FilterDataByAccessRoles(UserRolesToFilterEndpoint(endpoint))
                .When(
                    !String.IsNullOrEmpty(additionalfilter),
                    q => q.FilterAdditionalDataByCondition(additionalfilter)
                )
                .ApplyRawFilter(rawfilter)
                .ApplyOrdering_GeneratedColumns(
                    new PGGeoSearchResult() { geosearch = false },
                    rawsort,
                    "data#>>'\\{Shortname\\}'"
                )
                .Limit(limitto ?? int.MaxValue);

            var data = await query.GetAsync<JsonRaw>();

            return data.Select(raw =>
                    raw.TransformRawData(
                        language,
                        fields,
                        filteroutNullValues: removenullvalues,
                        urlGenerator: UrlGenerator,
                        fieldstohide: null
                    )
                )
                .Where(json => json != null)
                .Select(json => json!);
        }

        #endregion
    }
}
