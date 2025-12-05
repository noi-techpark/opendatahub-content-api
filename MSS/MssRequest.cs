// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DataModel;

namespace MSS
{
    public class MssRequest
    {
        public static async Task<HttpResponseMessage> RequestAsync(
            string serviceurl,
            HttpClient httpClient,
            XDocument request
        )
        {
            string gethotellist = @"?function=getHotelList&mode=1";

            try
            {
                //HttpClient myclient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
                //myclient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate");
                var myresponse = await httpClient.PostAsync(
                    serviceurl + gethotellist,
                    new StringContent(request.ToString(), Encoding.UTF8, "text/xml")
                );

                return myresponse;
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(ex.Message),
                };
            }
        }

        public static async Task<HttpResponseMessage> RequestSpecialAsync(
            string serviceurl,
            HttpClient httpClient,
            XDocument request
        )
        {
            string getspeciallist = @"?function=getSpecialList&mode=1";

            try
            {
                //HttpClient myclient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
                //myclient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate");
                var myresponse = await httpClient.PostAsync(
                    serviceurl + getspeciallist,
                    new StringContent(request.ToString(), Encoding.UTF8, "text/xml")
                );

                return myresponse;
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(ex.Message),
                };
            }
        }

        public static async Task<HttpResponseMessage> RequestRoomAsync(
            string serviceurl,
            HttpClient httpClient,
            XDocument request
        )
        {
            string getroomlist = @"?function=getRoomList&mode=1";

            try
            {
                //HttpClient myclient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
                //myclient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate");
                var myresponse = await httpClient.PostAsync(
                    serviceurl + getroomlist,
                    new StringContent(request.ToString(), Encoding.UTF8, "text/xml")
                );

                return myresponse;
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(ex.Message),
                };
            }
        }

        public static HttpResponseMessage RequestRoom(
            string serviceurl,
            HttpClient httpClient,
            XDocument request
        )
        {
            string getroomlist = @"?function=getRoomList&mode=1";

            try
            {
                //HttpClient myclient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
                //myclient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate");
                var myresponse = httpClient
                    .PostAsync(
                        serviceurl + getroomlist,
                        new StringContent(request.ToString(), Encoding.UTF8, "text/xml")
                    )
                    .Result;

                return myresponse;
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(ex.Message),
                };
            }
        }

        public static XDocument BuildPostData(
            XElement idlist,
            XElement channel,
            XElement roomlist,
            DateTime arrival,
            DateTime departure,
            XElement offerdetails,
            XElement hoteldetails,
            XElement type,
            int service,
            string lang,
            string idofchannel,
            string source,
            string version,
            string mssuser,
            string msspswd
        )
        {
            XElement myroot = new XElement(
                "root",
                new XElement("version", version + ".0"),
                new XElement(
                    "header",
                    new XElement(
                        "credentials",
                        new XElement("user", mssuser),
                        new XElement("password", msspswd),
                        new XElement("source", source)
                    ),
                    new XElement("method", "getHotelList"),
                    new XElement("paging", new XElement("start", "0"), new XElement("limit", "0"))
                ),
                new XElement(
                    "request",
                    new XElement(
                        "search",
                        new XElement("lang", lang),
                        idlist.Elements("id"),
                        new XElement("id_ofchannel", idofchannel),
                        new XElement(
                            "search_offer",
                            channel.Elements("channel_id"),
                            new XElement("arrival", String.Format("{0:yyyy-MM-dd}", arrival)),
                            new XElement("departure", String.Format("{0:yyyy-MM-dd}", departure)),
                            new XElement("service", service),
                            roomlist.Elements("room"),
                            type
                        )
                    ),
                    new XElement("options", offerdetails, hoteldetails),
                    new XElement("order"),
                    new XElement("logging", new XElement("step"))
                )
            );

            XDocument encodedDoc8 = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), myroot);

            return encodedDoc8;
        }

        public static XDocument BuildPostData(
            XElement channel,
            XElement roomlist,
            DateTime arrival,
            DateTime departure,
            XElement offerdetails,
            XElement hoteldetails,
            XElement type,
            int service,
            string lang,
            string source,
            string version,
            string mssuser,
            string msspswd
        )
        {
            XElement myroot = new XElement(
                "root",
                new XElement("version", version + ".0"),
                new XElement(
                    "header",
                    new XElement(
                        "credentials",
                        new XElement("user", mssuser),
                        new XElement("password", msspswd),
                        new XElement("source", source)
                    ),
                    new XElement("method", "getHotelList"),
                    new XElement("paging", new XElement("start", "0"), new XElement("limit", "0"))
                ),
                new XElement(
                    "request",
                    new XElement(
                        "search",
                        new XElement("lang", lang),
                        new XElement(
                            "search_offer",
                            channel,
                            new XElement("arrival", String.Format("{0:yyyy-MM-dd}", arrival)),
                            new XElement("departure", String.Format("{0:yyyy-MM-dd}", departure)),
                            new XElement("service", service),
                            roomlist.Elements("room"),
                            type
                        )
                    ),
                    new XElement("options", offerdetails, hoteldetails),
                    new XElement("order"),
                    new XElement("logging", new XElement("step"))
                )
            );

            XDocument encodedDoc8 = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), myroot);

            return encodedDoc8;
        }

        public static XDocument BuildPostData2(
            XElement channels,
            XElement roomlist,
            DateTime arrival,
            DateTime departure,
            XElement offerdetails,
            XElement hoteldetails,
            XElement type,
            int service,
            string lang,
            string source,
            string version,
            string mssuser,
            string msspswd
        )
        {
            XElement myroot = new XElement(
                "root",
                new XElement("version", version + ".0"),
                new XElement(
                    "header",
                    new XElement(
                        "credentials",
                        new XElement("user", mssuser),
                        new XElement("password", msspswd),
                        new XElement("source", source)
                    ),
                    new XElement("method", "getHotelList"),
                    new XElement("paging", new XElement("start", "0"), new XElement("limit", "0"))
                ),
                new XElement(
                    "request",
                    new XElement(
                        "search",
                        new XElement("lang", lang),
                        new XElement(
                            "search_offer",
                            channels.Elements("channel_id"),
                            new XElement("arrival", String.Format("{0:yyyy-MM-dd}", arrival)),
                            new XElement("departure", String.Format("{0:yyyy-MM-dd}", departure)),
                            new XElement("service", service),
                            roomlist.Elements("room"),
                            type
                        )
                    ),
                    new XElement("options", offerdetails, hoteldetails),
                    new XElement("order"),
                    new XElement("logging", new XElement("step"))
                )
            );

            XDocument encodedDoc8 = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), myroot);

            return encodedDoc8;
        }

        public static XDocument BuildBaseSearchPostData(
            XElement idlist,            
            XElement offerdetails, 
            XElement hoteldetails, 
            string lang, 
            string idofchannel, 
            string version, 
            string source, 
            string mssuser, 
            string msspswd)
        {
            XElement myroot =
                new XElement("root",
                new XElement("version", version + ".0"),
                new XElement("header",
                    new XElement("credentials",
                        new XElement("user", mssuser),
                        new XElement("password", msspswd),
                        new XElement("source", source)
                        ),
                    new XElement("method", "getHotelList"),
                    new XElement("paging",
                        new XElement("start", "0"),
                        new XElement("limit", "0")
                        )),
                new XElement("request",
                    new XElement("search",
                        new XElement("lang", lang),
                        idlist.Elements("id"),
                        new XElement("id_ofchannel", idofchannel),
                    new XElement("options",
                        offerdetails,
                        hoteldetails
                        ),
                    new XElement("order"),
                    new XElement("logging",
                        new XElement("step")
                        )
                    )
                );

            XDocument encodedDoc8 = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                myroot
            );

            return encodedDoc8;
        }


        public static XDocument BuildSpecialPostData(
            XElement offerid,
            XElement roomlist,
            DateTime arrival,
            DateTime departure,
            XElement specialdetails,
            int typ,
            int service,
            string lang,
            string source,
            string version,
            string mssuser,
            string msspswd
        )
        {
            XElement myroot = new XElement(
                "root",
                new XElement("version", version + ".0"),
                new XElement(
                    "header",
                    new XElement(
                        "credentials",
                        new XElement("user", mssuser),
                        new XElement("password", msspswd),
                        new XElement("source", source)
                    ),
                    new XElement("method", "getSpecialList"),
                    new XElement("paging", new XElement("start", "0"), new XElement("limit", "0"))
                ),
                new XElement(
                    "request",
                    new XElement(
                        "search",
                        new XElement("lang", lang),
                        new XElement(
                            "search_special",
                            offerid.Elements("offer_id"),
                            new XElement("date_from", String.Format("{0:yyyy-MM-dd}", arrival)),
                            new XElement("date_to", String.Format("{0:yyyy-MM-dd}", departure)),
                            new XElement("typ", typ),
                            new XElement(
                                "validity",
                                new XElement("valid", "0"),
                                new XElement("offers", "0"),
                                new XElement("service", service),
                                roomlist.Elements("room")
                            )
                        )
                    ),
                    new XElement("options", specialdetails),
                    new XElement("order", new XElement("field", "date"), new XElement("dir", "asc"))
                )
            );

            XDocument encodedDoc8 = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), myroot);

            return encodedDoc8;
        }

        public static XDocument BuildRoomlistPostData(
            XElement roomdetails,
            string hotelid,
            string idofchannel,
            string lang,
            string source,
            string version,
            string mssuser,
            string msspswd
        )
        {
            XElement myroot = new XElement(
                "root",
                new XElement("version", version + ".0"),
                new XElement(
                    "header",
                    new XElement(
                        "credentials",
                        new XElement("user", mssuser),
                        new XElement("password", msspswd),
                        new XElement("source", source)
                    ),
                    new XElement("method", "getRoomList"),
                    new XElement("paging", new XElement("start", "0"), new XElement("limit", "0"))
                ),
                new XElement(
                    "request",
                    new XElement(
                        "search",
                        new XElement("lang", lang),
                        new XElement("id", hotelid),
                        new XElement("id_ofchannel", idofchannel)
                    ),
                    new XElement("options", roomdetails)
                )
            );

            XDocument encodedDoc8 = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), myroot);

            return encodedDoc8;
        }

        //Premium includiert
        public static XDocument BuildSpecialPostDatawithPremium(
            XElement offerid,
            XElement roomlist,
            DateTime arrival,
            DateTime departure,
            XElement specialdetails,
            int typ,
            int premium,
            int service,
            string lang,
            string source,
            string version,
            string mssuser,
            string msspswd
        )
        {
            XElement myroot = new XElement(
                "root",
                new XElement("version", version + ".0"),
                new XElement(
                    "header",
                    new XElement(
                        "credentials",
                        new XElement("user", mssuser),
                        new XElement("password", msspswd),
                        new XElement("source", source)
                    ),
                    new XElement("method", "getSpecialList"),
                    new XElement("paging", new XElement("start", "0"), new XElement("limit", "0"))
                ),
                new XElement(
                    "request",
                    new XElement(
                        "search",
                        new XElement("lang", lang),
                        new XElement(
                            "search_special",
                            offerid.Elements("offer_id"),
                            new XElement("date_from", String.Format("{0:yyyy-MM-dd}", arrival)),
                            new XElement("date_to", String.Format("{0:yyyy-MM-dd}", departure)),
                            new XElement("typ", typ),
                            new XElement("premium", premium),
                            new XElement(
                                "validity",
                                new XElement("valid", "0"),
                                new XElement("offers", "0"),
                                new XElement("service", service),
                                roomlist.Elements("room")
                            )
                        )
                    ),
                    new XElement("options", specialdetails),
                    new XElement("order", new XElement("field", "date"), new XElement("dir", "asc"))
                )
            );

            XDocument encodedDoc8 = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), myroot);

            return encodedDoc8;
        }

        public static XDocument BuildSpecialPostDataCheckAvailability(
            XElement idlist,
            XElement offerid,
            XElement roomlist,
            DateTime arrival,
            DateTime departure,
            XElement specialdetails,
            XElement hoteldetails,
            int typ,
            int service,
            string lang,
            string source,
            string version,
            string mssuser,
            string msspswd
        )
        {
            XElement myroot = new XElement(
                "root",
                new XElement("version", version + ".0"),
                new XElement(
                    "header",
                    new XElement(
                        "credentials",
                        new XElement("user", mssuser),
                        new XElement("password", msspswd),
                        new XElement("source", source)
                    ),
                    new XElement("method", "getSpecialList"),
                    new XElement("paging", new XElement("start", "0"), new XElement("limit", "0")),
                    new XElement("result_id")
                ),
                new XElement(
                    "request",
                    new XElement(
                        "search",
                        new XElement("lang", lang),
                        idlist.Elements("id"),
                        //new XElement("id_ofchannel", "lts"),
                        new XElement(
                            "search_special",
                            //new XElement("date_from", String.Format("{0:yyyy-MM-dd}", arrival)),
                            //new XElement("date_to", String.Format("{0:yyyy-MM-dd}", departure)),
                            offerid.Elements("offer_id"),
                            new XElement("typ", typ),
                            new XElement(
                                "validity",
                                new XElement("valid", "0"),
                                new XElement("offers", "1"),
                                new XElement("arrival", String.Format("{0:yyyy-MM-dd}", arrival)),
                                new XElement(
                                    "departure",
                                    String.Format("{0:yyyy-MM-dd}", departure)
                                ),
                                new XElement("service", service),
                                roomlist.Elements("room")
                            )
                        )
                    ),
                    new XElement("options", specialdetails, hoteldetails),
                    new XElement("order", new XElement("field", "date"), new XElement("dir", "asc"))
                )
            );

            XDocument encodedDoc8 = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), myroot);

            return encodedDoc8;
        }

        //Premium includiert
        public static XDocument BuildSpecialPostDataCheckAvailabilitywithPremium(
            XElement idlist,
            XElement offerid,
            XElement roomlist,
            DateTime arrival,
            DateTime departure,
            XElement specialdetails,
            XElement hoteldetails,
            int typ,
            int premium,
            int service,
            string lang,
            string source,
            string version,
            string mssuser,
            string msspswd
        )
        {
            XElement myroot = new XElement(
                "root",
                new XElement("version", version + ".0"),
                new XElement(
                    "header",
                    new XElement(
                        "credentials",
                        new XElement("user", mssuser),
                        new XElement("password", msspswd),
                        new XElement("source", source)
                    ),
                    new XElement("method", "getSpecialList"),
                    new XElement("paging", new XElement("start", "0"), new XElement("limit", "0")),
                    new XElement("result_id")
                ),
                new XElement(
                    "request",
                    new XElement(
                        "search",
                        new XElement("lang", lang),
                        idlist.Elements("id"),
                        //new XElement("id_ofchannel", "lts"),
                        new XElement(
                            "search_special",
                            //new XElement("date_from", String.Format("{0:yyyy-MM-dd}", arrival)),
                            //new XElement("date_to", String.Format("{0:yyyy-MM-dd}", departure)),
                            offerid.Elements("offer_id"),
                            new XElement("typ", typ),
                            new XElement("premium", premium),
                            new XElement(
                                "validity",
                                new XElement("valid", "0"),
                                new XElement("offers", "1"),
                                new XElement("arrival", String.Format("{0:yyyy-MM-dd}", arrival)),
                                new XElement(
                                    "departure",
                                    String.Format("{0:yyyy-MM-dd}", departure)
                                ),
                                new XElement("service", service),
                                roomlist.Elements("room")
                            )
                        )
                    ),
                    new XElement("options", specialdetails, hoteldetails),
                    new XElement("order", new XElement("field", "date"), new XElement("dir", "asc"))
                )
            );

            XDocument encodedDoc8 = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), myroot);

            return encodedDoc8;
        }

        public static XElement BuildRoomData(List<MssRoom> myroompropertys)
        {
            XElement myroomlist = new XElement("roomlist");

            foreach (var room in myroompropertys)
            {
                XElement roomroot = new XElement("room");

                foreach (var myperson in room.Person)
                {
                    roomroot.Add(new XElement("person", myperson));
                }
                roomroot.Add(
                    new XElement("room_seq", room.RoomSeq),
                    new XElement("room_type", room.RoomType)
                );

                myroomlist.Add(roomroot);
            }

            return myroomlist;
        }

        public static XElement BuildIDList(List<string> A0RIdList)
        {
            XElement myidlist = new XElement("idlist");

            foreach (string a0rid in A0RIdList)
            {
                myidlist.Add(new XElement("id", a0rid));
            }

            return myidlist;
        }

        public static XElement BuildOfferIDList(List<string> offeridlist)
        {
            XElement myidlist = new XElement("idlist");

            foreach (string offerid in offeridlist)
            {
                myidlist.Add(new XElement("offer_id", offerid));
            }

            return myidlist;
        }

        public static XElement BuildType(string typ)
        {
            XElement typxelement = new XElement("typ", typ);

            return typxelement;
        }

        public static XElement BuildChannelList(List<string> channels)
        {
            XElement mychannellist = new XElement("channellist");

            foreach (string channel in channels)
            {
                mychannellist.Add(new XElement("channel_id", channel));
            }

            return mychannellist;
        }

        public static XElement BuildChannelList(string channel)
        {
            XElement mychannellist = new XElement("channellist");
            mychannellist.Add(new XElement("channel_id", channel));

            return mychannellist;
        }

        public static XElement BuildChannelList(string[] channels)
        {
            XElement mychannellist = new XElement("channellist");

            foreach (string channel in channels)
            {
                mychannellist.Add(new XElement("channel_id", channel));
            }

            return mychannellist;
        }
    }
}
