// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using MSS;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MSS
{
    public static class GetMssData
    {
        /// <summary>
        /// MSS Availability Search by passing Hotel Ids parallel request on the bookingchannels are done
        /// </summary>
        /// <param name="lang">language</param>
        /// <param name="A0Ridlist">list of all Hotel LTS Ids</param>
        /// <param name="mybookingchannels">List of bookingchannels to search on</param>
        /// <param name="myroomdata">Roomdata </param>
        /// <param name="arrival">arrival date</param>
        /// <param name="departure">departure date</param>
        /// <param name="service">service</param>
        /// <returns></returns>
        public static async Task<MssResult?> GetMssResponse(
            HttpClient httpClient,
            string lang,
            List<string> idlist,
            string idsofchannel,
            string[] mybookingchannels,
            List<Tuple<string, string, List<string>>> myroomdata,
            DateTime arrival,
            DateTime departure,
            int service,
            string hgvservicecode,
            XElement offerdetails,
            XElement hoteldetails,
            int rooms,
            string requestsource,
            string version,
            string serviceurl,
            string mssuser,
            string msspswd,
            bool withoutmssids = false
        )
        {
            try
            {
                List<MssRoom> myroompersons = (
                    from x in myroomdata
                    select new MssRoom
                    {
                        RoomSeq = x.Item1,
                        RoomType = x.Item2,
                        Person = x.Item3,
                    }
                ).ToList();

                var myroomlist = MssRequest.BuildRoomData(myroompersons);

                XElement mychannels = MssRequest.BuildChannelList(mybookingchannels);

                XElement myidlist = withoutmssids
                    ? MssRequest.BuildIDList(new List<string>())
                    : myidlist = MssRequest.BuildIDList(idlist);

                //XElement mytyp = MssRequest.BuildType("10");

                ////Add Logging
                //if (A0Ridlist != null)
                //{
                //    var tracesource = new TraceSource("MssData");
                //    tracesource.TraceEvent(TraceEventType.Information, 0, "MSS Request Hotel ID Count: " + A0Ridlist.Count + " Period: " + arrival.ToShortDateString() + " " + departure.ToShortDateString() + " Service: " + service.ToString() + " Rooms: " + myroompersons.Count + " Result from Cache: " + withoutmssids.ToString());
                //}

                XDocument myrequest = MssRequest.BuildPostData(
                    myidlist,
                    mychannels,
                    myroomlist,
                    arrival,
                    departure,
                    offerdetails,
                    hoteldetails,
                    //mytyp,
                    null,
                    service,
                    lang,
                    idsofchannel,
                    requestsource,
                    version,
                    mssuser,
                    msspswd
                );

                var myresponses = await MssRequest.RequestAsync(serviceurl, httpClient, myrequest);

                var activityresponsecontent = await myresponses.Content.ReadAsStringAsync();

                XElement allmyresponses = XElement.Parse(activityresponsecontent);

                List<XElement> allmyoffers = (
                    from xy in allmyresponses.Element("result").Elements("hotel")
                    where xy.Elements("channel").Count() > 0
                    select xy
                ).ToList();

                XElement myresult = new XElement("root");
                myresult.Add(allmyresponses.Element("header"), new XElement("result", allmyoffers));

                //Und iatz no parsen
                MssResult myparsedresponse = ParseMssResponse.ParsemyMssResponse(
                    lang,
                    hgvservicecode,
                    myresult,
                    idlist,
                    myroompersons,
                    requestsource,
                    version,
                    withoutmssids
                );

                return myparsedresponse;
            }
            catch (Exception ex)
            {
                //var tracesource = new TraceSource("MssData");
                //tracesource.TraceEvent(TraceEventType.Error, 0, "MSS Request Error: " + ex.Message);

                Log.Logger.Error(
                    ex,
                    "Error while retrieving MSS information for {A0Ridlist}",
                    idlist
                );

                return null;
            }
        }

        /// <summary>
        /// Gets MSS Base Hotel Search without availability search
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="offerdetails"></param>
        /// <param name="hoteldetails"></param>
        /// <param name="source"></param>
        /// <param name="version"></param>
        /// <param name="mssuser"></param>
        /// <param name="msspswd"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<MssResponseBaseSearch>> GetMssBaseDataResponse(
            HttpClient httpClient,
            List<string> hotelids,
            string idsofchannel,
            string lang, 
            XElement offerdetails, 
            XElement hoteldetails, 
            string source, 
            string version,
            string serviceurl,
            string mssuser, 
            string msspswd)
        {
            try
            {
                XElement myidlist = hotelids == null || hotelids.Count == 0
                    ? MssRequest.BuildIDList(new List<string>())
                    : myidlist = MssRequest.BuildIDList(hotelids);

                XDocument myrequest = MssRequest.BuildBaseSearchPostData(myidlist, offerdetails, hoteldetails, lang, idsofchannel, version, source, mssuser, msspswd);

                var myresponses = await MssRequest.RequestAsync(serviceurl, httpClient, myrequest);                

                Task<string> activityresponsecontent = myresponses.Content.ReadAsStringAsync();

                await Task.WhenAll(activityresponsecontent);

                XElement allmyresponses = XElement.Parse(activityresponsecontent.Result);

                return ParseMssResponse.ParseBaseSearchResponse(allmyresponses);
            }
            catch (Exception ex)
            {
                var tracesource = new TraceSource("MssData");
                tracesource.TraceEvent(TraceEventType.Error, 0, "MSS Request Error: " + ex.Message);

                return null;
            }
        }

    }
}
