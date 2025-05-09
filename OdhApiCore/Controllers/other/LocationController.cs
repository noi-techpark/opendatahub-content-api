// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using DataModel.Annotations;
using Helper;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OdhApiCore.Filters;
using OdhApiCore.Responses;
using OdhNotifier;
using Schema.NET;
using SqlKata;
using SqlKata.Execution;

namespace OdhApiCore.Controllers.api
{
    /// <summary>
    /// <a href='https://github.com/noi-techpark/odh-docs/wiki/Location-Endpoint' target="_blank">Wiki Location Api</a>
    /// </summary>
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class LocationController : OdhController
    {
        private readonly IHttpClientFactory httpClientFactory;

        public LocationController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<LocationController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier,
            IHttpClientFactory httpClientFactory
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier)
        {
            this.httpClientFactory = httpClientFactory;
        }

        #region SWAGGER Exposed Api

        /// <summary>
        /// GET Location List (Use in locfilter)
        /// </summary>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default 'en'), if 'null' is passed all languages are returned as Dictionary</param>
        /// <param name="type">Type ('mta','reg','tvs','mun','fra') Separator ',' : 'null' returns all Location Objects (default)</param>
        /// <param name="showall">Show all Data (true = all, false = show only data marked as visible)</param>
        /// <param name="locfilter">Locfilter (Separator ',') possible values: mta + MetaREGIONID = (Filter by MetaRegion), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), (default:'null')</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="rawfilter"><a href='https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter' target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href='https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter' target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Reduced List of Locations Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<LocHelperclass>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OdhCacheOutput(
            ClientTimeSpan = 0,
            ServerTimeSpan = 7200,
            CacheKeyGenerator = typeof(CustomCacheKeyGenerator),
            MustRevalidate = true
        )]
        [HttpGet, Route("Location")]
        public async Task<IActionResult> GetTheLocationList(
            string? language,
            uint? pagenumber = null,
            [SwaggerEnum(new[] { "mta", "reg", "tvs", "mun", "fra" })] string? type = "null",
            bool showall = true,
            string? locfilter = null,
            CancellationToken cancellationToken = default
        )
        {
            return await GetLocationInfoFiltered(
                pagenumber,
                language,
                locfilter,
                showall,
                type,
                cancellationToken
            );
        }

        /// <summary>
        /// GET Skiarea List (Use in locfilter as "ska")
        /// </summary>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default 'en'), if 'null' is passed all languages are returned as Dictionary</param>
        /// <param name="locfilter">Locfilter (Separator ',') possible values: mta + MetaREGIONID = (Filter by MetaRegion), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), (default:'null')</param>
        /// <returns>Reduced List of Locations Objects</returns>
        [ProducesResponseType(typeof(IEnumerable<LocHelperclass>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,GastroReader")]
        [HttpGet, Route("Location/Skiarea")]
        public async Task<IActionResult> GetTheSkiareaList(
            string? language,
            uint? pagenumber = null,
            string? locfilter = null,
            CancellationToken cancellationToken = default
        )
        {
            return await GetSkiAreaInfoFiltered(
                pagenumber,
                language, 
                locfilter, 
                cancellationToken);
        }

        #endregion

        #region Private GETTER

        /// <summary>
        /// Get Filtered LocationList based on TV &amp; TVBs
        /// </summary>
        /// <param name="lang">Language</param>
        /// <param name="locfilter">Location Filter</param>
        /// <returns>Collection of Reduced Location Objects</returns>
        private async Task<IActionResult> GetLocationInfoFiltered(
            uint? pagenumber,
            string? lang,
            string? locfilter,
            bool allactivedata,
            string? type,
            CancellationToken cancellationToken
        )
        {
            List<LocHelperclass> mylocationlist = new List<LocHelperclass>();
            List<Tuple<string, string>> loclist = new List<Tuple<string, string>>();

            //Split the string
            if (locfilter != null && locfilter != "null")
            {
                if (locfilter.Substring(locfilter.Length - 1, 1) == ",")
                    locfilter = locfilter.Substring(0, locfilter.Length - 1);

                var splittedfilter = locfilter.Split(',');

                foreach (var filter in splittedfilter)
                {
                    string loctype = filter.ToLower().Substring(0, 3);
                    string locid = filter.Substring(3).ToUpper();

                    loclist.Add(Tuple.Create(locid, loctype));
                }
            }

            string defaultmunfrafilter = "data->'VisibleInSearch'='true'";
            if (allactivedata == true)
                defaultmunfrafilter = "data->'Active'='true'";

            List<string> locationtypes = new List<string>() { "mta", "reg", "tvs", "mun", "fra" };

            if (type != null)
                if (type != "null")
                    locationtypes = type.ToLower().Split(',').ToList();

            var mymetaregionlistreduced = default(IEnumerable<LocHelperclassDynamic>);
            var myregionlistreduced = default(IEnumerable<LocHelperclassDynamic>);
            var mylocalitylistreduced = default(IEnumerable<LocHelperclassDynamic>);
            var mytvlistreduced = default(IEnumerable<LocHelperclassDynamic>);
            var myfractionlistreduced = default(IEnumerable<LocHelperclassDynamic>);

            if (loclist.Count > 0)
            {
                foreach (var locitem in loclist)
                {
                    var loctype = locitem.Item2;
                    var locid = locitem.Item1;

                    if (loctype == "mta")
                    {
                        var mymetaregionlist =
                            await LocationListCreator.GetLocationFromDB<MetaRegion>(
                                QueryFactory,
                                "metaregions",
                                Tuple.Create("id", locid)
                            );

                        if (mymetaregionlist != null && mymetaregionlist.Count() > 0)
                        {
                            var regionlist =
                                mymetaregionlist.FirstOrDefault()?.RegionIds
                                ?? Enumerable.Empty<string>();

                            string regionlistwhere =
                                "data->>'Id' IN ("
                                + Helper.StringHelpers.JoinStringListForPG(",", regionlist, "'")
                                + ") AND "
                                + defaultmunfrafilter;
                            var myregionlist = await LocationListCreator.GetLocationFromDB<Region>(
                                QueryFactory,
                                "regions",
                                regionlistwhere
                            );

                            string tvlistwhere =
                                "data->>'RegionId' IN ("
                                + Helper.StringHelpers.JoinStringListForPG(",", regionlist, "'")
                                + ") AND "
                                + defaultmunfrafilter;
                            var mytvlist =
                                await LocationListCreator.GetLocationFromDB<Tourismverein>(
                                    QueryFactory,
                                    "tvs",
                                    tvlistwhere
                                );

                            string localitylistwhere =
                                "data->>'RegionId' IN ("
                                + Helper.StringHelpers.JoinStringListForPG(",", regionlist, "'")
                                + ") AND "
                                + defaultmunfrafilter;
                            var mylocalitylist =
                                await LocationListCreator.GetLocationFromDB<Municipality>(
                                    QueryFactory,
                                    "municipalities",
                                    localitylistwhere
                                );

                            string fractionlistwhere =
                                "data->>'RegionId' IN ("
                                + Helper.StringHelpers.JoinStringListForPG(",", regionlist, "'")
                                + ") AND "
                                + defaultmunfrafilter;
                            var myfractionlist =
                                await LocationListCreator.GetLocationFromDB<District>(
                                    QueryFactory,
                                    "districts",
                                    fractionlistwhere
                                );

                            mymetaregionlistreduced =
                                LocationListCreator.CreateLocHelperClassDynamic<MetaRegion>(
                                    "mta",
                                    mymetaregionlist,
                                    lang
                                );
                            myregionlistreduced =
                                LocationListCreator.CreateLocHelperClassDynamic<Region>(
                                    "reg",
                                    myregionlist,
                                    lang
                                );
                            mylocalitylistreduced =
                                LocationListCreator.CreateLocHelperClassDynamic<Municipality>(
                                    "mun",
                                    mylocalitylist,
                                    lang
                                );
                            mytvlistreduced =
                                LocationListCreator.CreateLocHelperClassDynamic<Tourismverein>(
                                    "tvs",
                                    mytvlist,
                                    lang
                                );
                            myfractionlistreduced =
                                LocationListCreator.CreateLocHelperClassDynamic<District>(
                                    "fra",
                                    myfractionlist,
                                    lang
                                );
                        }
                    }
                    else if (loctype == "reg")
                    {
                        var myregionlist = await LocationListCreator.GetLocationFromDB<Region>(
                            QueryFactory,
                            "regions",
                            Tuple.Create("id", locid)
                        );

                        string tvlistwhere =
                            "data->>'RegionId' = '" + locid + "' AND " + defaultmunfrafilter;
                        var mytvlist = await LocationListCreator.GetLocationFromDB<Tourismverein>(
                            QueryFactory,
                            "tvs",
                            tvlistwhere
                        );

                        string localitylistwhere =
                            "data->>'RegionId' = '" + locid + "' AND " + defaultmunfrafilter;
                        var mylocalitylist =
                            await LocationListCreator.GetLocationFromDB<Municipality>(
                                QueryFactory,
                                "municipalities",
                                localitylistwhere
                            );

                        string fractionlistwhere =
                            "data->>'RegionId' = '" + locid + "' AND " + defaultmunfrafilter;
                        var myfractionlist = await LocationListCreator.GetLocationFromDB<District>(
                            QueryFactory,
                            "districts",
                            fractionlistwhere
                        );

                        myregionlistreduced =
                            LocationListCreator.CreateLocHelperClassDynamic<Region>(
                                "reg",
                                myregionlist,
                                lang
                            );
                        mylocalitylistreduced =
                            LocationListCreator.CreateLocHelperClassDynamic<Municipality>(
                                "mun",
                                mylocalitylist,
                                lang
                            );
                        mytvlistreduced =
                            LocationListCreator.CreateLocHelperClassDynamic<Tourismverein>(
                                "tvs",
                                mytvlist,
                                lang
                            );
                        myfractionlistreduced =
                            LocationListCreator.CreateLocHelperClassDynamic<District>(
                                "fra",
                                myfractionlist,
                                lang
                            );
                    }
                    else if (loctype == "tvs")
                    {
                        var mytvlist = await LocationListCreator.GetLocationFromDB<Tourismverein>(
                            QueryFactory,
                            "tvs",
                            Tuple.Create("id", locid)
                        );

                        string localitylistwhere =
                            "data->>'TourismvereinId' = '" + locid + "' AND " + defaultmunfrafilter;
                        var mylocalitylist =
                            await LocationListCreator.GetLocationFromDB<Municipality>(
                                QueryFactory,
                                "municipalities",
                                localitylistwhere
                            );

                        string fractionlistwhere =
                            "data->>'TourismvereinId' = '" + locid + "' AND " + defaultmunfrafilter;
                        var myfractionlist = await LocationListCreator.GetLocationFromDB<District>(
                            QueryFactory,
                            "districts",
                            fractionlistwhere
                        );

                        mytvlistreduced =
                            LocationListCreator.CreateLocHelperClassDynamic<Tourismverein>(
                                "tvs",
                                mytvlist,
                                lang
                            );
                        mylocalitylistreduced =
                            LocationListCreator.CreateLocHelperClassDynamic<Municipality>(
                                "mun",
                                mylocalitylist,
                                lang
                            );
                        myfractionlistreduced =
                            LocationListCreator.CreateLocHelperClassDynamic<District>(
                                "fra",
                                myfractionlist,
                                lang
                            );
                    }
                    else if (loctype == "mun")
                    {
                        var mylocalitylist =
                            await LocationListCreator.GetLocationFromDB<Municipality>(
                                QueryFactory,
                                "municipalities",
                                Tuple.Create("id", locid)
                            );

                        string fractionlistwhere =
                            "data->>'MunicipalityId' = '" + locid + "' AND " + defaultmunfrafilter;
                        var myfractionlist = await LocationListCreator.GetLocationFromDB<District>(
                            QueryFactory,
                            "districts",
                            fractionlistwhere
                        );

                        mylocalitylistreduced =
                            LocationListCreator.CreateLocHelperClassDynamic<Municipality>(
                                "mun",
                                mylocalitylist,
                                lang
                            );
                        myfractionlistreduced =
                            LocationListCreator.CreateLocHelperClassDynamic<District>(
                                "fra",
                                myfractionlist,
                                lang
                            );
                    }
                    else if (loctype == "fra")
                    {
                        var myfractionlist = await LocationListCreator.GetLocationFromDB<District>(
                            QueryFactory,
                            "districts",
                            Tuple.Create("id", locid)
                        );
                        myfractionlistreduced =
                            LocationListCreator.CreateLocHelperClassDynamic<District>(
                                "fra",
                                myfractionlist,
                                lang
                            );
                    }

                    if (locationtypes.Contains("mta") && mymetaregionlistreduced != null)
                        mylocationlist.AddRange(mymetaregionlistreduced);
                    if (locationtypes.Contains("reg") && myregionlistreduced != null)
                        mylocationlist.AddRange(myregionlistreduced);
                    if (locationtypes.Contains("tvs") && mytvlistreduced != null)
                        mylocationlist.AddRange(mytvlistreduced);
                    if (locationtypes.Contains("mun") && mylocalitylistreduced != null)
                        mylocationlist.AddRange(mylocalitylistreduced);
                    if (locationtypes.Contains("fra") && myfractionlistreduced != null)
                        mylocationlist.AddRange(myfractionlistreduced);
                }
            }
            else
            {
                if (locationtypes.Contains("mta"))
                {
                    var mymetaregionlist = await LocationListCreator.GetLocationFromDB<MetaRegion>(
                        QueryFactory,
                        "metaregions",
                        "data->'Active'='true'"
                    );
                    mymetaregionlistreduced =
                        LocationListCreator.CreateLocHelperClassDynamic<MetaRegion>(
                            "mta",
                            mymetaregionlist,
                            lang
                        );
                }
                if (locationtypes.Contains("reg"))
                {
                    var myregionlist = await LocationListCreator.GetLocationFromDB<Region>(
                        QueryFactory,
                        "regions",
                        "data->'Active'='true'"
                    );
                    myregionlistreduced = LocationListCreator.CreateLocHelperClassDynamic<Region>(
                        "reg",
                        myregionlist,
                        lang
                    );
                }
                if (locationtypes.Contains("tvs"))
                {
                    var mytvlist = await LocationListCreator.GetLocationFromDB<Tourismverein>(
                        QueryFactory,
                        "tvs",
                        "data->'Active'='true'"
                    );
                    mytvlistreduced =
                        LocationListCreator.CreateLocHelperClassDynamic<Tourismverein>(
                            "tvs",
                            mytvlist,
                            lang
                        );
                }
                if (locationtypes.Contains("mun"))
                {
                    var mylocalitylist = await LocationListCreator.GetLocationFromDB<Municipality>(
                        QueryFactory,
                        "municipalities",
                        defaultmunfrafilter
                    );
                    mylocalitylistreduced =
                        LocationListCreator.CreateLocHelperClassDynamic<Municipality>(
                            "mun",
                            mylocalitylist,
                            lang
                        );
                }
                if (locationtypes.Contains("fra"))
                {
                    var myfractionlist = await LocationListCreator.GetLocationFromDB<District>(
                        QueryFactory,
                        "districts",
                        defaultmunfrafilter
                    );
                    myfractionlistreduced =
                        LocationListCreator.CreateLocHelperClassDynamic<District>(
                            "fra",
                            myfractionlist,
                            lang
                        );
                }

                if (locationtypes.Contains("mta") && mymetaregionlistreduced != null)
                    mylocationlist.AddRange(mymetaregionlistreduced);
                if (locationtypes.Contains("reg") && myregionlistreduced != null)
                    mylocationlist.AddRange(myregionlistreduced);
                if (locationtypes.Contains("tvs") && mytvlistreduced != null)
                    mylocationlist.AddRange(mytvlistreduced);
                if (locationtypes.Contains("mun") && mylocalitylistreduced != null)
                    mylocationlist.AddRange(mylocalitylistreduced);
                if (locationtypes.Contains("fra") && myfractionlistreduced != null)
                    mylocationlist.AddRange(myfractionlistreduced);
            }

            //Transform to JsonRAW List
            var jsonrawlist = mylocationlist.Select(x => new JsonRaw(x)).ToList();

            //Hack Databrowser Display
            if (pagenumber.HasValue)
            {
                uint totalpages = 1;
                uint totalcount = (uint)jsonrawlist.Count;

                return Ok(ResponseHelpers.GetResult(
                    pagenumber.Value,
                    totalpages,
                    totalcount,
                    null,
                    jsonrawlist,
                    Url
                ));
            }

            return Ok(jsonrawlist);
        }

        /// <summary>
        /// Get Filtered SkiAreaList based on TV &amp; TVBs
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="locfilter"></param>
        /// <returns>Collection of Reduced Location Objects</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        private async Task<IActionResult> GetSkiAreaInfoFiltered(
            uint? pagenumber,
            string? lang,
            string? locfilter,
            CancellationToken cancellationToken
        )
        {
            List<LocHelperclass> mylocationlist = new List<LocHelperclass>();

            List<Tuple<string, string>> loclist = new List<Tuple<string, string>>();

            var myskiarealistreduced = default(IEnumerable<LocHelperclassDynamic>);

            //Split the string
            if (locfilter != null && locfilter != "null")
            {
                if (locfilter.Substring(locfilter.Length - 1, 1) == ",")
                    locfilter = locfilter.Substring(0, locfilter.Length - 1);

                var splittedfilter = locfilter.Split(',');

                foreach (var filter in splittedfilter)
                {
                    string loctype = filter.ToLower().Substring(0, 3);
                    string locid = filter.Substring(3).ToUpper();

                    loclist.Add(Tuple.Create(locid, loctype));
                }
            }

            if (loclist.Count > 0)
            {
                foreach (var locitem in loclist)
                {
                    var loctype = locitem.Item2;
                    var locid = locitem.Item1;

                    if (loctype == "tvs")
                    {
                        //string skiarealistwhere = "data @> '{ \"TourismvereinIds\": [\"" + locid + "\"]}'";
                        //var myskiarealist = PostgresSQLHelper.SelectFromTableDataAsObject<SkiArea>(conn, "skiareas", "*", skiarealistwhere, "", 0, null);

                        var myskiarealistquery = QueryFactory
                            .Query()
                            .Select("data")
                            .From("skiareas")
                            .WhereInJsonb(
                                new List<string>() { locid },
                                tvid => new { TourismvereinIds = new[] { tvid.ToUpper() } }
                            );

                        var myskiarealist = await myskiarealistquery.GetObjectListAsync<SkiArea>();
                        myskiarealistreduced =
                            LocationListCreator.CreateLocHelperClassDynamic<SkiArea>(
                                "ska",
                                myskiarealist,
                                lang
                            );
                    }
                    else if (loctype == "reg")
                    {
                        //string skiarealistwhere = "data @> '{ \"RegionIds\": [\"" + locid + "\"]}'";

                        var myskiarealistquery = QueryFactory
                            .Query()
                            .Select("data")
                            .From("skiareas")
                            .WhereInJsonb(
                                new List<string>() { locid },
                                regid => new { RegionIds = new[] { regid.ToUpper() } }
                            );

                        var myskiarealist = await myskiarealistquery.GetObjectListAsync<SkiArea>();
                        myskiarealistreduced =
                            LocationListCreator.CreateLocHelperClassDynamic<SkiArea>(
                                "ska",
                                myskiarealist,
                                lang
                            );
                    }
                    else if (loctype == "mta")
                    {
                        var mymetaregion = await QueryFactory
                            .Query()
                            .Select("data")
                            .From("metaregions")
                            .Where("id", locid)
                            .GetObjectSingleAsync<MetaRegion>();

                        var myskiarealistquery = QueryFactory
                            .Query()
                            .Select("data")
                            .From("skiareas")
                            .WhereInJsonb(
                                mymetaregion.RegionIds?.ToList() ?? new List<string>(),
                                regid => new { RegionIds = new[] { regid.ToUpper() } }
                            );

                        var myskiarealist = await myskiarealistquery.GetObjectListAsync<SkiArea>();
                        myskiarealistreduced =
                            LocationListCreator.CreateLocHelperClassDynamic<SkiArea>(
                                "ska",
                                myskiarealist,
                                lang
                            );
                    }

                    if (myskiarealistreduced != null && myskiarealistreduced.Count() > 0)
                        mylocationlist.AddRange(myskiarealistreduced);
                }
            }
            else
            {
                var myskiarealistquery = QueryFactory.Query().Select("data").From("skiareas");

                var myskiarealist = await myskiarealistquery.GetObjectListAsync<SkiArea>();

                myskiarealistreduced = LocationListCreator.CreateLocHelperClassDynamic<SkiArea>(
                    "ska",
                    myskiarealist,
                    lang
                );
                mylocationlist.AddRange(myskiarealistreduced);
            }

            //TO TEST
            List<JsonRaw> mylocationlistraw = mylocationlist
                .Select(x => new JsonRaw(JsonConvert.SerializeObject(x)))
                .ToList();

            //Hack Databrowser Display
            if (pagenumber.HasValue)
            {
                var jsonrawlist = mylocationlist.Select(x => new JsonRaw(x)).ToList();
                uint totalpages = 1;
                uint totalcount = (uint)jsonrawlist.Count;

                return Ok(ResponseHelpers.GetResult(
                    pagenumber.Value,
                    totalpages,
                    totalcount,
                    null,
                    jsonrawlist,
                    Url
                ));
            }

            //Transform to JsonRAW List
            return Ok(mylocationlist.Select(x => new JsonRaw(x)).ToList());
        }

        #endregion
    }
}
