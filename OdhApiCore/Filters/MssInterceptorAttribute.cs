﻿using Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdhApiCore.Controllers;
using OdhApiCore.Controllers.helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OdhApiCore.Filters
{
    public class MssInterceptorAttribute : ActionFilterAttribute
    {
        private readonly IHttpClientFactory httpClientFactory;

        public MssInterceptorAttribute(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var query = context.HttpContext.Request.Query;

            string idsource = (string?)query["idsource"] ?? "lts";
            var availabilitychecklegacy = (string?)query["availabilitycheck"];
            bool.TryParse(availabilitychecklegacy, out bool availabilitycheck);

            if (availabilitycheck && idsource == "lts")
            {
                string language = (string?)query["language"] ?? "de";
                string availabilitychecklanguage = (string?)query["availabilitychecklanguage"] ?? "en";
                string boardfilter = (string?)query["boardfilter"] ?? "0";
                string? arrival = (string?)query["arrival"] ?? null;
                string? departure = (string?)query["departure"] ?? null;
                string roominfo = (string?)query["roominfo"] ?? "1-18,18";
                string bokfilter = (string?)query["bokfilter"] ?? "hgv";
                string source = (string?)query["source"] ?? "sinfo";

                List<string> bokfilterlist = bokfilter != null ? bokfilter.Split(',').ToList() : new List<string>();
                var bookableAccoIds = new List<string>()
                {
                    "0A140305173D4B7A972E43EFA7A67AF3"
                };

                if (context.Result is OkObjectResult okObject && okObject.Value is JsonRaw jRaw)
                {
                    string json = jRaw.Value;
                    var jtoken = JToken.Parse(json);
                    if (jtoken is JObject jObject)
                    {
                        var mssResponseShort = jObject.Property("MssResponseShort");
                        if (mssResponseShort is JProperty mssResponseShortProperty)
                        {
                            MssResult result = await GetMSSAvailability(
                                language: language, arrival: arrival, departure: departure, boardfilter: boardfilter,
                                roominfo: roominfo, bokfilter: bokfilter, detail: null, bookableaccoIDs: bookableAccoIds, source = source);
                            if (result != null)
                            {
                                var resultJson = JsonConvert.SerializeObject(result.MssResponseShort);
                                mssResponseShortProperty.Value = new JRaw(resultJson);
                            }
                        }
                    }
                    okObject.Value = jtoken;
                }
            }
            await base.OnResultExecutionAsync(context, next);

        }

        private async Task<MssResult> GetMSSAvailability(string language, string arrival, string departure, string boardfilter, string roominfo, string bokfilter, string detail, List<string> bookableaccoIDs, string source, bool withoutmssids = false, string mssversion = "2")
        {
            int? offerdetail = null;
            int hoteldetail = 524288;

            if (detail == "1")
            {
                offerdetail = 33081;
                hoteldetail = 524800;
            }

            MssHelper myhelper = MssHelper.Create(bookableaccoIDs, null, bokfilter, language, roominfo, boardfilter, arrival, departure, hoteldetail, offerdetail, source, mssversion);
                       
            //Achtung muassi no schaugn!
            if (bookableaccoIDs.Count > 0)
            {
                //0 MSS Method Olle channels affamol mit IDList
                var myparsedresponse = await GetMssData.GetMssResponse(
                    httpClientFactory.CreateClient("mss"),
                    lang: myhelper.mssrequestlanguage, A0Ridlist: myhelper.a0ridlist, mybookingchannels: myhelper.mybokchannels,
                    myroomdata: myhelper.myroomdata, arrival: myhelper.arrival.Value, departure: myhelper.departure.Value, service: myhelper.service.Value,
                    hgvservicecode: myhelper.hgvservicecode, offerdetails: myhelper.xoffertype, hoteldetails: myhelper.xhoteldetails,
                    rooms: myhelper.rooms.Value, source: myhelper.source, version: myhelper.mssversion, mssuser: "", msspswd: "", withoutmssids: withoutmssids
                    );
               
                return myparsedresponse;
            }
            else
                return new MssResult() { bookableHotels = 0, CheapestChannel = "", Cheapestprice = 0, ResultId = "", MssResponseShort = new List<MssResponseShort>() };
        }

        //private async Task<MssResult> GetLCSAvailability(string language, string arrival, string departure, string boardfilter, string roominfo, List<string> bookableaccoIDs, string source)
        //{
        //    //-------------------------------------------------MSSREQUEST------------------------------------------------------------

        //    var service = Common.AccoListCreator.CreateBoardListLCSfromFlag(boardfilter);
        //    var myroomdata = GetAccommodationDataLCS.RoomstayTransformer(roominfo);

        //    //Achtung muassi no schaugn!
        //    if (bookableaccoIDs.Count > 0)
        //    {
        //        var accosearchrequest = GetAccommodationDataLCS.GetAccommodationDataSearchRequest("", "1", "10000", "de", "1", "", "", "0", "0", arrival, departure, "1", "0", service, bookableaccoIDs, new List<string>(), new List<string>(), new List<string>(), myroomdata, source, ltsmessagepswd);

        //        var myaccosearchlcs = new GetAccommodationDataLCS(ltsuser, ltspswd);
        //        var response = await myaccosearchlcs.GetAccommodationDataSearchAsync(accosearchrequest);
        //        var myparsedresponse = ParseAccoSearchResult.ParsemyLCSResponse(language, response, myroomdata.Count);

        //        return myparsedresponse;
        //    }
        //    else
        //        return new MssResult() { bookableHotels = 0, CheapestChannel = "", Cheapestprice = 0, ResultId = "", MssResponseShort = new List<MssResponseShort>() };
        //}

        private bool CheckArrivalAndDeparture(string arrival, string departure)
        {
            DateTime now = DateTime.Now;
            DateTime arrivaldt = DateTime.Parse(arrival);
            DateTime departuredt = DateTime.Parse(departure);

            if (arrivaldt.Date == departuredt.Date)
                return false;

            if (arrivaldt <= now.Date.AddDays(-1) || departuredt <= now.Date.AddDays(-1))
                return false;
            else
                return true;
        }

    }
}
