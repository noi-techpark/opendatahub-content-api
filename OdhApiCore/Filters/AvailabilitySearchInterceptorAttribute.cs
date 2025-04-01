// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using Helper;
using LTSAPI;
using LTSAPI.Parser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MSS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdhApiCore.Controllers;
using OdhApiCore.Controllers.helper;
using SqlKata.Execution;
using static RawQueryParser.Filtering;

namespace OdhApiCore.Filters
{
    public class AvailabilitySearchInterceptorAttribute : ActionFilterAttribute
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ISettings settings;
        protected QueryFactory QueryFactory { get; }
        private static readonly List<string> roles = new() { "IDM", "LTS" };

        public bool CheckAvailabilitySearch(System.Security.Claims.ClaimsPrincipal User)
        {
            foreach (var role in roles)
            {
                if (User.IsInRole(role))
                    return true;
            }

            return false;
        }

        public AvailabilitySearchInterceptorAttribute(
            QueryFactory queryFactory,
            IHttpClientFactory httpClientFactory,
            ISettings settings
        )
        {
            this.httpClientFactory = httpClientFactory;
            this.settings = settings;
            this.QueryFactory = queryFactory;
        }

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next
        )
        {
            var availabilitysearchavailable = CheckAvailabilitySearch(context.HttpContext.User);

            // TODO: if Availability Requested and CheckAvailabilitySearch gives false, return a 401 Unauthorized

            // Getting Action name
            context.ActionDescriptor.RouteValues.TryGetValue("action", out string? actionid);

            // Only if Action ID is GetAccommodations perform the Availability Check before
            if (
                (
                    actionid == "GetAccommodations"
                    || actionid == "PostAvailableAccommodations"
                    || actionid == "PostAvailableAccommodationsOnlyMssResult"
                ) && availabilitysearchavailable
            )
            {
                // Getting the Querystrings
                var actionarguments = context.ActionArguments;

                bool availabilitycheck = false;

                if (
                    actionid == "PostAvailableAccommodations"
                    || actionid == "PostAvailableAccommodationsOnlyMssResult"
                )
                    availabilitycheck = true;
                if (actionid == "GetAccommodations")
                    availabilitycheck =
                        ((LegacyBool?)actionarguments["availabilitycheck"])?.Value
                        ?? availabilitycheck;

                if (availabilitycheck == true)
                {
                    string? categoryfilter = actionarguments.ContainsKey("categoryfilter")
                        ? (string?)actionarguments["categoryfilter"]
                        : null;
                    string? typefilter = actionarguments.ContainsKey("typefilter")
                        ? (string?)actionarguments["typefilter"]
                        : null;
                    string? featurefilter = actionarguments.ContainsKey("featurefilter")
                        ? (string?)actionarguments["featurefilter"]
                        : null;
                    string? featureidfilter = actionarguments.ContainsKey("featureidfilter")
                        ? (string?)actionarguments["featureidfilter"]
                        : null;
                    string? themefilter = actionarguments.ContainsKey("themefilter")
                        ? (string?)actionarguments["themefilter"]
                        : null;
                    string? badgefilter = actionarguments.ContainsKey("badgefilter")
                        ? (string?)actionarguments["badgefilter"]
                        : null;
                    string? idfilter = actionarguments.ContainsKey("idfilter")
                        ? (string?)actionarguments["idfilter"]
                        : null;
                    string? locfilter = actionarguments.ContainsKey("locfilter")
                        ? (string?)actionarguments["locfilter"]
                        : null;
                    string? altitudefilter = actionarguments.ContainsKey("altitudefilter")
                        ? (string?)actionarguments["altitudefilter"]
                        : null;
                    string? odhtagfilter = actionarguments.ContainsKey("odhtagfilter")
                        ? (string?)actionarguments["odhtagfilter"]
                        : null;
                    bool? active = actionarguments.ContainsKey("active")
                        ? ((LegacyBool?)actionarguments["active"])?.Value
                        : true;
                    bool? odhactive = actionarguments.ContainsKey("odhactive")
                        ? ((LegacyBool?)actionarguments["odhactive"])?.Value
                        : null;
                    bool? bookablefilter = actionarguments.ContainsKey("bookablefilter")
                        ? ((LegacyBool?)actionarguments["bookablefilter"])?.Value
                        : null;
                    string? updatefrom = actionarguments.ContainsKey("updatefrom")
                        ? (string?)actionarguments["updatefrom"]
                        : null;
                    string? seed = actionarguments.ContainsKey("seed")
                        ? (string?)actionarguments["seed"]
                        : null;
                    string? searchfilter = actionarguments.ContainsKey("searchfilter")
                        ? (string?)actionarguments["searchfilter"]
                        : null;
                    string? latitude = actionarguments.ContainsKey("latitude")
                        ? (string?)actionarguments["latitude"]
                        : null;
                    string? longitude = actionarguments.ContainsKey("longitude")
                        ? (string?)actionarguments["longitude"]
                        : null;
                    string? radius = actionarguments.ContainsKey("radius")
                        ? (string?)actionarguments["radius"]
                        : null;

                    string? publishedon = actionarguments.ContainsKey("publishedon")
                        ? (string?)actionarguments["publishedon"]
                        : "idm-marketplace";

                    string language = actionarguments.ContainsKey("language")
                        ? (string)actionarguments["language"]!
                        : "de";
                    string? langfilter = actionarguments.ContainsKey("langfilter")
                        ? (string?)actionarguments!["langfilter"]
                        : null;

                    string boardfilter = actionarguments.ContainsKey("boardfilter")
                        ? (string)actionarguments["boardfilter"]!
                        : "0";
                    string arrival = actionarguments.ContainsKey("arrival")
                        ? (string)actionarguments["arrival"]!
                        : String.Format("{0:yyyy-MM-dd}", DateTime.Now);
                    string departure = actionarguments.ContainsKey("departure")
                        ? (string)actionarguments["departure"]!
                        : String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(1));
                    string roominfo = actionarguments.ContainsKey("roominfo")
                        ? (string)actionarguments["roominfo"]!
                        : "1-18,18";
                    string msssource = actionarguments.ContainsKey("msssource")
                        ? (string)actionarguments["msssource"]!
                        : "sinfo";
                    string detail = actionarguments.ContainsKey("detail")
                        ? (string)actionarguments["detail"]!
                        : "0";
                    string bokfilter = actionarguments.ContainsKey("bokfilter")
                        ? (string)actionarguments["bokfilter"]!
                        : "hgv";
                    string idsource = actionarguments.ContainsKey("idsource")
                        ? (string)actionarguments["idsource"]!
                        : "lts";
                    string? sourcefilter = actionarguments.ContainsKey("source")
                        ? (string)actionarguments["source"]!
                        : null;

                    // Only needed for PostAvailableAccommodations
                    bool? availabilityonly = actionarguments.ContainsKey("availabilityonly")
                        ? (bool)actionarguments["availabilityonly"]!
                        : false;
                    // If no option of caching is given let's decide on runtime
                    bool? msscache = actionarguments.ContainsKey("usemsscache")
                        ? (bool)actionarguments["usemsscache"]!
                        : null;
                    bool? lcscache = actionarguments.ContainsKey("uselcscache")
                        ? (bool)actionarguments["uselcscache"]!
                        : null;

                    string? removeduplicatesfrom = actionarguments.ContainsKey(
                        "removeduplicatesfrom"
                    )
                        ? (string?)actionarguments!["removeduplicatesfrom"]
                        : null;

                    string? ltsapiversion = actionarguments.ContainsKey(
                        "ltsapiversion"
                    )
                        ? (string?)actionarguments!["ltsapiversion"]
                        : "v1";

                    if (CheckArrivalAndDeparture(arrival, departure))
                    {
                        var booklist = new List<string>();
                        var bokfilterlist =
                            bokfilter != null ? bokfilter.Split(',').ToList() : new List<string>();

                        bool allowwithoutids = false;

                        if (actionid == "GetAccommodations")
                        {
                            bool? customisbookablefilter = bookablefilter;
                            if (idfilter != null && idfilter.Count() > 0)
                                customisbookablefilter = null;

                            AccommodationHelper myhelper = await AccommodationHelper.CreateAsync(
                                QueryFactory,
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
                                smgtags: odhtagfilter,
                                activefilter: active,
                                smgactivefilter: odhactive,
                                bookablefilter: customisbookablefilter,
                                sourcefilter: sourcefilter,
                                lastchange: updatefrom,
                                langfilter: langfilter,
                                publishedonfilter: publishedon,
                                (CancellationToken?)context.ActionArguments["cancellationToken"]
                                    ?? new()
                            );

                            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                                latitude,
                                longitude,
                                radius
                            );

                            // Get Accommodations IDlist
                            var idlist = await GetAccommodationBookList(
                                myhelper,
                                language,
                                seed,
                                searchfilter,
                                geosearchresult
                            );

                            booklist =
                                idlist
                                    .Where(x => x.Id != null)
                                    .Select(x => x.Id!.ToUpper())
                                    .ToList() ?? new List<string>();

                            //What if here the id was not found return nothing
                        }
                        else if (
                            actionid == "PostAvailableAccommodations"
                            || actionid == "PostAvailableAccommodationsOnlyMssResult"
                        )
                        {
                            allowwithoutids = true;

                            //If no Ids are passed and no locfilter is passed search over all IDs
                            if (
                                actionarguments.ContainsKey("idfilter")
                                && (string)actionarguments["idfilter"] != null
                            )
                            {
                                //add the parsed ids
                                string passedids = actionarguments.ContainsKey("idfilter")
                                    ? (string)actionarguments["idfilter"]!
                                    : "";
                                booklist = passedids.Split(",").ToList();

                                allowwithoutids = false;
                            }
                            else if (locfilter != null)
                            {
                                AccommodationHelper myhelper =
                                    await AccommodationHelper.CreateAsync(
                                        QueryFactory,
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
                                        smgtags: odhtagfilter,
                                        activefilter: active,
                                        smgactivefilter: odhactive,
                                        bookablefilter: bookablefilter,
                                        sourcefilter: sourcefilter,
                                        lastchange: updatefrom,
                                        langfilter: langfilter,
                                        publishedonfilter: publishedon,
                                        (CancellationToken?)
                                            context.ActionArguments["cancellationToken"] ?? new()
                                    );

                                var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                                    latitude,
                                    longitude,
                                    radius
                                );

                                // Get Accommodations IDlist
                                var idlist = await GetAccommodationBookList(
                                    myhelper,
                                    language,
                                    seed,
                                    searchfilter,
                                    geosearchresult
                                );

                                booklist =
                                    idlist
                                        .Where(x => x.Id != null)
                                        .Select(x => x.Id!.ToUpper())
                                        .ToList() ?? new List<string>();

                                allowwithoutids = false;
                            }
                        }

                        context.HttpContext.Items.Add("accobooklist", booklist);

                        if (allowwithoutids || booklist.Count > 0)
                        {
                            //TODO Refine Logic here on a certain amount of Accommodations use the cache
                            var usecache = false;
                            if (booklist.Count == 0)
                                usecache = true;

                            await Parallel.ForEachAsync(
                                bokfilterlist,
                                async (value, cancellationToken) =>
                                {
                                    if (value == "hgv")
                                    {
                                        context.HttpContext.Items.Add(
                                            "mssavailablity",
                                            await GetMSSAvailability(
                                                language: language,
                                                arrival: arrival,
                                                departure: departure,
                                                boardfilter: boardfilter,
                                                roominfo: roominfo,
                                                bokfilter: bokfilter,
                                                detail: Convert.ToInt32(detail),
                                                bookableaccoIDs: booklist,
                                                idsofchannel: idsource,
                                                requestsource: msssource,
                                                msscache: msscache ?? usecache
                                            )
                                        );
                                    }

                                    if (value == "lts")
                                    {
                                        context.HttpContext.Items.Add(
                                            "lcsavailablity",
                                            await GetLTSAvailability(
                                                language: language,
                                                arrival: arrival,
                                                departure: departure,
                                                boardfilter: boardfilter,
                                                roominfo: roominfo,
                                                bookableaccoIDs: booklist,
                                                requestsource: msssource,
                                                lcscache: lcscache ?? usecache,
                                                ltsapiversion
                                            )
                                        );
                                    }
                                }
                            );
                        }
                    }
                }

                await base.OnActionExecutionAsync(context, next);
            }
            else
            {
                await base.OnActionExecutionAsync(context, next);
            }
        }

        public override async Task OnResultExecutionAsync(
            ResultExecutingContext context,
            ResultExecutionDelegate next
        )
        {
            bool availabilitysearchavailable = CheckAvailabilitySearch(context.HttpContext.User);

            // Getting Action name
            context.ActionDescriptor.RouteValues.TryGetValue("action", out string? actionid);

            var query = context.HttpContext.Request.Query;

            string idsource = (string?)query["idsource"] ?? "lts";

            bool availabilitycheck = false;

            if (actionid == "PostAvailableAccommodations")
                availabilitycheck = true;
            if (actionid == "GetAccommodations" || actionid == "GetAccommodation")
            {
                var availabilitychecklegacy = (string?)query["availabilitycheck"];
                bool.TryParse(availabilitychecklegacy, out availabilitycheck);
            }

            string bokfilter = (string?)query["bokfilter"] ?? "hgv";
            var bokfilterlist =
                bokfilter != null ? bokfilter.Split(',').ToList() : new List<string>();

            var availabilityonlychecklegacy = (string?)query["availabilityonly"];
            bool.TryParse(availabilityonlychecklegacy, out bool availabilityonly);

            if (availabilitycheck && availabilitysearchavailable && !availabilityonly)
            {
                string language = (string?)query["language"] ?? "de";
                string boardfilter = (string?)query["boardfilter"] ?? "0";
                string arrival =
                    (string?)query["arrival"] ?? String.Format("{0:yyyy-MM-dd}", DateTime.Now);
                string departure =
                    (string?)query["departure"]
                    ?? String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(1));
                string roominfo = (string?)query["roominfo"] ?? "1-18,18";
                string msssource = (string?)query["msssource"] ?? "sinfo";
                string detail = (string?)query["detail"] ?? "0";

                if (CheckArrivalAndDeparture(arrival, departure))
                {
                    List<string>? bookableAccoIds = new();
                    if (context.RouteData.Values.TryGetValue("id", out object? id) && id != null)
                        bookableAccoIds.Add((string)id);

                    if (context.Result is OkObjectResult okObject && okObject.Value is JsonRaw jRaw)
                    {
                        string json = jRaw.Value;
                        var jtoken = JToken.Parse(json);
                        if (jtoken is JObject jObject)
                        {
                            var mssResponseShort = jObject.Property("MssResponseShort");
                            if (mssResponseShort is JProperty mssResponseShortProperty)
                            {
                                List<MssResult> result = new();

                                await Parallel.ForEachAsync(
                                    bokfilterlist,
                                    async (value, cancellationToken) =>
                                    {
                                        if (value == "hgv")
                                        {
                                            MssResult? mssresult = default(MssResult);

                                            if (
                                                actionid == "GetAccommodations"
                                                || actionid == "PostAvailableAccommodations"
                                            )
                                            {
                                                mssresult = (MssResult?)
                                                    context.HttpContext.Items["mssavailablity"];
                                            }
                                            else if (actionid == "GetAccommodation")
                                            {
                                                mssresult = await GetMSSAvailability(
                                                    language: language,
                                                    arrival: arrival,
                                                    departure: departure,
                                                    boardfilter: boardfilter,
                                                    roominfo: roominfo,
                                                    bokfilter: bokfilter,
                                                    detail: Convert.ToInt32(detail),
                                                    bookableaccoIDs: bookableAccoIds,
                                                    idsofchannel: idsource,
                                                    requestsource: msssource
                                                );
                                            }

                                            if (mssresult != null)
                                            {
                                                result.Add(mssresult);
                                            }
                                        }
                                        if (value == "lts")
                                        {
                                            MssResult? lcsresult = default(MssResult);

                                            if (
                                                actionid == "GetAccommodations"
                                                || actionid == "PostAvailableAccommodations"
                                            )
                                            {
                                                lcsresult = (MssResult?)
                                                    context.HttpContext.Items["lcsavailablity"];
                                            }
                                            else if (actionid == "GetAccommodation")
                                            {
                                                lcsresult = await GetLCSAvailability(
                                                    language: language,
                                                    arrival: arrival,
                                                    departure: departure,
                                                    boardfilter: boardfilter,
                                                    roominfo: roominfo,
                                                    bookableaccoIDs: bookableAccoIds,
                                                    requestsource: msssource
                                                );
                                            }

                                            if (lcsresult != null)
                                            {
                                                result.Add(lcsresult);
                                            }
                                        }
                                    }
                                );

                                if (result.Count > 0)
                                {
                                    var resultJson = JsonConvert.SerializeObject(
                                        result.SelectMany(x => x.MssResponseShort)
                                    );
                                    mssResponseShortProperty.Value = new JRaw(resultJson);
                                }
                            }
                        }
                        okObject.Value = jtoken;
                    }
                    else if (
                        context.Result is OkObjectResult okObjectlist
                        && okObjectlist.Value is JsonResult<JsonRaw> jRawList
                    )
                    {
                        List<JToken> myRawList = new();

                        foreach (var myjRaw in jRawList.Items)
                        {
                            string json = myjRaw.Value;
                            var jtoken = JToken.Parse(json);
                            if (jtoken is JObject jObject)
                            {
                                var mssResponseShort = jObject.Property("MssResponseShort");
                                if (mssResponseShort is JProperty mssResponseShortProperty)
                                {
                                    List<MssResult> result = new();

                                    await Parallel.ForEachAsync(
                                        bokfilterlist,
                                        async (value, cancellationToken) =>
                                        {
                                            if (value == "hgv")
                                            {
                                                MssResult? mssresult = null;

                                                if (
                                                    actionid == "GetAccommodations"
                                                    || actionid == "PostAvailableAccommodations"
                                                )
                                                {
                                                    mssresult = (MssResult?)
                                                        context.HttpContext.Items["mssavailablity"];
                                                }
                                                else if (actionid == "GetAccommodation")
                                                {
                                                    mssresult = await GetMSSAvailability(
                                                        language: language,
                                                        arrival: arrival,
                                                        departure: departure,
                                                        boardfilter: boardfilter,
                                                        roominfo: roominfo,
                                                        bokfilter: bokfilter,
                                                        detail: Convert.ToInt32(detail),
                                                        bookableaccoIDs: bookableAccoIds,
                                                        idsofchannel: idsource,
                                                        requestsource: msssource
                                                    );
                                                }

                                                if (mssresult != null)
                                                {
                                                    result.Add(mssresult);
                                                }
                                            }
                                            if (value == "lts")
                                            {
                                                MssResult? lcsresult = null;

                                                if (
                                                    actionid == "GetAccommodations"
                                                    || actionid == "PostAvailableAccommodations"
                                                )
                                                {
                                                    lcsresult = (MssResult?)
                                                        context.HttpContext.Items["lcsavailablity"];
                                                }
                                                else if (actionid == "GetAccommodation")
                                                {
                                                    lcsresult = await GetLCSAvailability(
                                                        language: language,
                                                        arrival: arrival,
                                                        departure: departure,
                                                        boardfilter: boardfilter,
                                                        roominfo: roominfo,
                                                        bookableaccoIDs: bookableAccoIds,
                                                        requestsource: msssource
                                                    );
                                                }

                                                if (lcsresult != null)
                                                {
                                                    result.Add(lcsresult);
                                                }
                                            }
                                        }
                                    );

                                    if (result.Count > 0)
                                    {
                                        string? accid = jObject.Property("Id")?.Value.ToString();
                                        var data = result
                                            .SelectMany(x => x.MssResponseShort)
                                            .Where(x => x.A0RID == accid);
                                        mssResponseShortProperty.Value = new JRaw(
                                            JsonConvert.SerializeObject(data)
                                        );
                                    }
                                }
                            }
                            myRawList.Add(jtoken);
                        }

                        jRawList.Items = myRawList.Select(jtoken => new JsonRaw(jtoken.ToString()));
                    }
                }
            }

            // TODO: Return paginated result on list https://localhost:5001/api/Accommodation?pagenumber=1&pagesize=10&categoryfilter=30720&typefilter=1&arrival=01-02-2021&departure=05-02-2021&roominfo=1-18%2C18&bokfilter=hgv&source=sinfo&availabilitychecklanguage=en&availabilitycheck=true
            await base.OnResultExecutionAsync(context, next);
        }

        private async Task<MssResult> GetMSSAvailability(
            string language,
            string arrival,
            string departure,
            string boardfilter,
            string roominfo,
            string bokfilter,
            int? detail,
            List<string> bookableaccoIDs,
            string idsofchannel,
            string requestsource,
            bool msscache = false,
            string mssversion = "2"
        )
        {
            // Edge Case No Ids Provided, and use caching disabled
            if ((bookableaccoIDs.Count == 0) && !msscache)
            {
                using var r = new StreamReader(
                    Path.Combine(settings.JsonConfig.Jsondir, $"AccosBookable.json")
                );
                string json = await r.ReadToEndAsync();
                bookableaccoIDs = JsonConvert.DeserializeObject<List<string>>(json) ?? new();
            }

            MssHelper myhelper = MssHelper.Create(
                bookableaccoIDs,
                idsofchannel,
                bokfilter,
                language,
                roominfo,
                boardfilter,
                arrival,
                departure,
                detail,
                requestsource,
                mssversion
            );

            //Filters the Request after 
            if (bookableaccoIDs.Count > 0 || msscache)
            {
                // 0 MSS Method Olle channels affamol mit IDList
                var myparsedresponse = await GetMssData.GetMssResponse(
                    httpClientFactory.CreateClient("mss"),
                    lang: myhelper.mssrequestlanguage,
                    idlist: myhelper.accoidlist,
                    idsofchannel: myhelper.idsofchannel,
                    mybookingchannels: myhelper.mybokchannels,
                    myroomdata: myhelper.myroomdata,
                    arrival: myhelper.arrival,
                    departure: myhelper.departure,
                    service: myhelper.service,
                    hgvservicecode: myhelper.hgvservicecode,
                    offerdetails: myhelper.xoffertype,
                    hoteldetails: myhelper.xhoteldetails,
                    rooms: myhelper.rooms,
                    requestsource: myhelper.requestsource,
                    version: myhelper.mssversion,
                    serviceurl: settings.MssConfig.ServiceUrl,
                    mssuser: settings.MssConfig.Username,
                    msspswd: settings.MssConfig.Password,
                    withoutmssids: msscache
                );

                if (myparsedresponse != null)
                    return myparsedresponse;
            }
            return new MssResult()
            {
                bookableHotels = 0,
                CheapestChannel = "",
                Cheapestprice = 0,
                ResultId = "",
                MssResponseShort = new List<MssResponseShort>(),
            };
        }

        private async Task<MssResult> GetLTSAvailability(
            string language,
            string arrival,
            string departure,
            string boardfilter,
            string roominfo,
            List<string> bookableaccoIDs,
            string requestsource,
            bool lcscache = false,
            string apiversion = "v1"
            )
        {
            if(apiversion == "v2")
            {
                return await GetLTSApiAvailability(language, arrival, departure, boardfilter, roominfo, bookableaccoIDs, requestsource, lcscache);
            }
            else
                return await GetLCSAvailability(language, arrival, departure, boardfilter, roominfo, bookableaccoIDs, requestsource, lcscache);
        }

        private async Task<MssResult> GetLCSAvailability(
            string language,
            string arrival,
            string departure,
            string boardfilter,
            string roominfo,
            List<string> bookableaccoIDs,
            string requestsource,
            bool lcscache = false
        )
        {
            LcsHelper myhelper = LcsHelper.Create(
                bookableaccoIDs,
                language,
                roominfo,
                boardfilter,
                arrival,
                departure,
                requestsource
            );

            // Edge Case No Ids Provided, load all of them
            if ((bookableaccoIDs.Count == 0) && !lcscache)
            {
                using var r = new StreamReader(
                    Path.Combine(settings.JsonConfig.Jsondir, $"AccosAll.json")
                );
                string json = await r.ReadToEndAsync();
                bookableaccoIDs = JsonConvert.DeserializeObject<List<string>>(json) ?? new();
            }

            if (bookableaccoIDs.Count > 0 || lcscache)
            {
                var accosearchrequest =
                    LCS.GetAccommodationDataLCS.GetAccommodationDataSearchRequest(
                        resultRID: "",
                        pageNr: "1",
                        pageSize: "10000",
                        language: myhelper.lcsrequestlanguage,
                        sortingcriterion: "1",
                        sortingorder: "",
                        sortingpromotebookable: "",
                        request: "0",
                        filters: "0",
                        timespanstart: myhelper.arrival,
                        timespanend: myhelper.departure,
                        checkavailabilitystatus: "1",
                        onlybookableresults: "0",
                        mealplans: myhelper.service,
                        accommodationrids: myhelper.accoidlist,
                        tourismorg: new List<string>(),
                        districts: new List<string>(),
                        marketinggroups: new List<string>(),
                        lcsroomstay: myhelper.myroomdata,
                        requestor: requestsource,
                        messagepswd: settings.LcsConfig.MessagePassword
                    );

                var myaccosearchlcs = new LCS.GetAccommodationDataLCS(
                    settings.LcsConfig.ServiceUrl,
                    settings.LcsConfig.Username,
                    settings.LcsConfig.Password
                );
                var response = await myaccosearchlcs.GetAccommodationDataSearchAsync(
                    accosearchrequest
                );
                var myparsedresponse = LCS.ParseAccoSearchResult.ParsemyLCSResponse(
                    language,
                    response,
                    myhelper.rooms
                );

                if (myparsedresponse != null)
                    return myparsedresponse;
            }
            return new MssResult()
            {
                bookableHotels = 0,
                CheapestChannel = "",
                Cheapestprice = 0,
                ResultId = "",
                MssResponseShort = new List<MssResponseShort>(),
            };
        }

        private async Task<MssResult> GetLTSApiAvailability(
            string language,
            string arrival,
            string departure,
            string boardfilter,
            string roominfo,
            List<string> bookableaccoIDs,
            string requestsource,
            bool lcscache = false
)
        {            
            // Edge Case No Ids Provided, load all of them
            if ((bookableaccoIDs.Count == 0) && !lcscache)
            {
                using var r = new StreamReader(
                    Path.Combine(settings.JsonConfig.Jsondir, $"AccosAll.json")
                );
                string json = await r.ReadToEndAsync();
                bookableaccoIDs = JsonConvert.DeserializeObject<List<string>>(json) ?? new();
            }

            if (bookableaccoIDs.Count > 0 || lcscache)
            {
                LtsApi ltsapi = new LtsApi(settings.LtsCredentials.serviceurl,
                        settings.LtsCredentials.username,
                        settings.LtsCredentials.password,
                        settings.LtsCredentials.ltsclientid,
                        false);

                var myroomdata = AccommodationSearchUtilities.RoomstringtoAvailabilitySearchRequestRoomoption(roominfo);


                var accoidlist =
                    bookableaccoIDs != null && bookableaccoIDs.Count > 0
                        ? bookableaccoIDs.Select(x => x.ToUpper()).ToList()
                        : bookableaccoIDs ?? new();


                LTSAvailabilitySearchRequestBody body = new LTSAvailabilitySearchRequestBody()
                {
                    accommodationRids = accoidlist,
                    //marketingGroupRids = new List<string>() { "" },
                    startDate = arrival,
                    endDate = departure,
                    paging = new LTSAvailabilitySearchRequestPaging() { pageNumber = 1, pageSize = 10000 },
                    cacheLifeTimeInSeconds = 300,
                    onlySuedtirolInfoActive = true,
                    //mealplans: myhelper.service,
                    roomOptions = myroomdata
                };
                
                //What about language

                var ltsavailablilitysearch = await ltsapi.AccommodationAvailabilitySearchRequest(null, body);
                var myparsedresponse = AccommodationSearchResultParser.ParseLTSAccommodation(ltsavailablilitysearch.FirstOrDefault(), 1);

                if (myparsedresponse != null)
                    return myparsedresponse;
            }
            return new MssResult()
            {
                bookableHotels = 0,
                CheapestChannel = "",
                Cheapestprice = 0,
                ResultId = "",
                MssResponseShort = new List<MssResponseShort>(),
            };
        }

        private bool CheckArrivalAndDeparture(string arrival, string departure)
        {
            DateTime now = DateTime.Now;
            DateTime arrivaldt = DateTime.Parse(arrival);
            DateTime departuredt = DateTime.Parse(departure);

            if (arrivaldt.Date == departuredt.Date)
                return false;

            if (arrivaldt.Date > departuredt.Date)
                return false;

            if (arrivaldt <= now.Date.AddDays(-1) || departuredt <= now.Date.AddDays(-1))
                return false;
            else
                return true;
        }

        private async Task<IEnumerable<AccoBookListRaw>> GetAccommodationBookList(
            AccommodationHelper myhelper,
            string language,
            string? seed,
            string? searchfilter,
            PGGeoSearchResult geosearchresult
        )
        {
            string select =
                $"data#>>'\\{{Id\\}}' as Id, data#>>'\\{{IsBookable\\}}' as IsBookable, data#>>'\\{{AccoBookingChannel\\}}' as AccoBookingChannel";

            var query = QueryFactory
                .Query()
                .SelectRaw(select)
                .From("accommodations")
                .AccommodationWhereExpression(
                    idlist: myhelper.idlist,
                    accotypelist: myhelper.accotypelist,
                    categorylist: myhelper.categorylist,
                    featurelist: myhelper.featurelist,
                    featureidlist: myhelper.featureidlist,
                    badgelist: myhelper.badgelist,
                    themelist: myhelper.themelist,
                    boardlist: myhelper.boardlist,
                    smgtaglist: myhelper.smgtaglist,
                    districtlist: myhelper.districtlist,
                    municipalitylist: myhelper.municipalitylist,
                    tourismvereinlist: myhelper.tourismvereinlist,
                    regionlist: myhelper.regionlist,
                    apartmentfilter: myhelper.apartment,
                    bookable: myhelper.bookable,
                    altitude: myhelper.altitude,
                    altitudemin: myhelper.altitudemin,
                    altitudemax: myhelper.altitudemax,
                    activefilter: myhelper.active,
                    smgactivefilter: myhelper.smgactive,
                    publishedonlist: myhelper.publishedonlist,
                    sourcelist: myhelper.sourcelist,
                    searchfilter: searchfilter,
                    language: language,
                    lastchange: myhelper.lastchange,
                    languagelist: new List<string>(),
                    additionalfilter: null,
                    userroles: new List<string>() { "IDM" }
                ) //Availability Search only for IDM Users therefore no filter Closed Data, no reduced data
                .OrderBySeed(ref seed, "data #>>'\\{Shortname\\}' ASC")
                .GeoSearchFilterAndOrderby(geosearchresult);

            return await query.GetAsync<AccoBookListRaw>();
        }
    }
}
