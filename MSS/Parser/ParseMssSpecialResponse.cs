// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using DataModel;

namespace MSS
{
    public class ParseMssSpecialResponse
    {
        public static List<Package> ParseMySpecialResponse(string lang, XElement mssresponse)
        {
            try
            {
                var myresult = mssresponse.Elements("result").Elements("special");

                List<Package> mypackageslist = new List<Package>();

                foreach (var myhotelresult in myresult)
                {
                    mypackageslist.Add(ResponseSpecialParser(myhotelresult, lang));
                }

                return mypackageslist;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static MssResult ParsemyMssSpecialResponse(
            string lang,
            string servicecode,
            XElement mssresponse,
            List<MssRoom> myroompersons
        )
        {
            try
            {
                var resultid = mssresponse.Element("header").Element("result_id").Value;

                var myresult = mssresponse.Elements("result").Elements("special");

                return ResponseSpecialParser(myresult, servicecode, myroompersons, resultid, lang);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Package ResponseSpecialParser(XElement myresult, string language)
        {
            try
            {
                Package myparsedpackage = new Package();
                CultureInfo culturede = CultureInfo.CreateSpecificCulture("de");

                myparsedpackage.Id = "Package" + myresult.Element("offer_id").Value;

                myparsedpackage.OfferId = myresult.Element("offer_id").Value;
                myparsedpackage.Offertyp = Convert.ToInt32(myresult.Element("offer_typ").Value);

                myparsedpackage.Shortname = myresult.Element("title").Value;

                myparsedpackage.Active = true;

                myparsedpackage.ChildrenMin = Convert.ToInt32(
                    myresult.Element("children_min").Value
                );
                myparsedpackage.Specialtyp = Convert.ToInt32(myresult.Element("special_typ").Value);
                myparsedpackage.Premiumtyp = Convert.ToInt32(
                    myresult.Element("special_premium").Value
                );
                myparsedpackage.DaysArrival = Convert.ToInt32(
                    myresult.Element("days_arrival").Value
                );
                myparsedpackage.DaysDeparture = Convert.ToInt32(
                    myresult.Element("days_departure").Value
                );
                myparsedpackage.DaysDurMin = Convert.ToInt32(
                    myresult.Element("days_dur_min").Value
                );
                myparsedpackage.DaysDurMax = Convert.ToInt32(
                    myresult.Element("days_dur_max").Value
                );
                myparsedpackage.DaysArrivalMin = Convert.ToInt32(
                    myresult.Element("days_arrival_min").Value
                );
                myparsedpackage.DaysArrivalMax = Convert.ToInt32(
                    myresult.Element("days_arrival_max").Value
                );
                myparsedpackage.ValidStart =
                    myresult.Element("valid_start").Value == "0000-00-00"
                        ? DateTime.MinValue
                        : Convert.ToDateTime(myresult.Element("valid_start").Value);
                myparsedpackage.ValidStop =
                    myresult.Element("valid_end").Value == "0000-00-00"
                        ? DateTime.MinValue
                        : Convert.ToDateTime(myresult.Element("valid_end").Value);

                //Shortstay Longstay Unterscheidung
                if (myparsedpackage.DaysDurMin <= 4)
                    myparsedpackage.ShortStay = true;
                else
                    myparsedpackage.LongStay = true;

                PackageDetail mydetail = new PackageDetail();
                mydetail.Desc = myresult.Element("description").Value;
                mydetail.Title = myresult.Element("title").Value;
                mydetail.Language = language;

                myparsedpackage.PackageDetail.Add(language, mydetail);

                if (myresult.Element("pictures") != null)
                {
                    List<ImageGallery> myimagegallerylist = new List<ImageGallery>();
                    foreach (XElement image in myresult.Element("pictures").Elements("picture"))
                    {
                        ImageGallery myimggallery = new ImageGallery();
                        myimggallery.ImageUrl = image.Element("url").Value;
                        myimggallery.ImageSource = "Hgv";

                        myimagegallerylist.Add(myimggallery);
                    }

                    myparsedpackage.ImageGallery = myimagegallerylist.ToList();
                }

                if (myresult.Element("hotels") != null)
                {
                    List<string> hotelhgvidlist = new List<string>();
                    foreach (XElement hotel in myresult.Element("hotels").Elements("hotel"))
                    {
                        string hotelhgvid = hotel.Element("id").Value;

                        hotelhgvidlist.Add(hotelhgvid);

                        string channelid = hotel.Element("channel").Element("channel_id").Value;
                        string channelprice = hotel.Element("channel").Element("from_price").Value;

                        myparsedpackage.ChannelInfo.Add(channelid, channelprice);
                    }
                    myparsedpackage.HotelHgvId = hotelhgvidlist.ToList();
                }

                if (myresult.Element("seasons") != null)
                {
                    List<Season> myseasonlist = new List<Season>();
                    foreach (XElement season in myresult.Element("seasons").Elements("season"))
                    {
                        Season myseason = new Season();
                        myseason.Start = Convert.ToDateTime(season.Element("date_start").Value);
                        myseason.End = Convert.ToDateTime(season.Element("date_end").Value);

                        myseasonlist.Add(myseason);
                    }

                    myparsedpackage.Season = myseasonlist.ToList();
                }

                if (myresult.Element("services") != null)
                {
                    List<string> servicelist = new List<string>();
                    foreach (XElement service in myresult.Element("services").Elements("service"))
                    {
                        servicelist.Add(service.Value);
                    }
                    myparsedpackage.Services = servicelist.ToList();
                }

                if (myresult.Element("inclusive") != null)
                {
                    foreach (XElement inclusive in myresult.Element("inclusive").Elements("price"))
                    {
                        Inclusive myinclusive = new Inclusive();

                        int priceid = Convert.ToInt32(inclusive.Element("price_id").Value);

                        myinclusive.PriceId = priceid;
                        myinclusive.PriceTyp = Convert.ToInt32(
                            inclusive.Element("price_typ").Value
                        );

                        PackageDetail myinclusivedetail = new PackageDetail();
                        myinclusivedetail.Desc = inclusive.Element("description").Value;
                        myinclusivedetail.Title = inclusive.Element("title").Value;
                        myinclusivedetail.Language = language;

                        myinclusive.PackageDetail.Add(language, myinclusivedetail);

                        if (inclusive.Element("pictures") != null)
                        {
                            List<ImageGallery> mypackagedetailimggallerylist =
                                new List<ImageGallery>();
                            foreach (
                                XElement image in inclusive.Element("pictures").Elements("picture")
                            )
                            {
                                ImageGallery myinclusiveimg = new ImageGallery();
                                myinclusiveimg.ImageUrl = image.Element("url").Value;
                                myinclusiveimg.ImageSource = "Hgv";

                                mypackagedetailimggallerylist.Add(myinclusiveimg);
                            }
                            myinclusive.ImageGallery = mypackagedetailimggallerylist.ToList();
                        }
                        myparsedpackage.Inclusive.Add(priceid.ToString(), myinclusive);
                    }
                }

                if (myresult.Element("themes") != null)
                {
                    List<PackageTheme> packagedetaillist = new List<PackageTheme>();
                    List<string> packagethemelist = new List<string>();

                    foreach (XElement theme in myresult.Element("themes").Elements("theme"))
                    {
                        PackageTheme mytheme = new PackageTheme();

                        mytheme.ThemeId = Convert.ToInt32(theme.Element("id").Value);

                        ThemeDetail mythemedetail = new ThemeDetail();

                        mythemedetail.Title = theme.Element("title").Value;
                        mythemedetail.Language = language;

                        mytheme.ThemeDetail.Add(language, mythemedetail);
                        //myparsedpackage.PackageTheme.Add(mytheme.Title, mytheme);

                        packagedetaillist.Add(mytheme);
                        packagethemelist.Add(theme.Element("title").Value);
                    }

                    myparsedpackage.PackageThemeDetail = packagedetaillist.ToList();
                    myparsedpackage.PackageThemeList = packagethemelist.ToList();
                }

                return myparsedpackage;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static MssResult ResponseSpecialParser(
            IEnumerable<XElement> myresult,
            string servicecode,
            List<MssRoom> roompersons,
            string resultid,
            string lang
        )
        {
            try
            {
                MssResult myparsedresponselist = new MssResult();
                myparsedresponselist.ResultId = resultid;

                CultureInfo culturede = CultureInfo.CreateSpecificCulture("de");

                int rooms = roompersons.Count;

                foreach (
                    var myhotelresult in myresult
                        .Elements("hotels")
                        .Elements("hotel")
                        .Where(x => x.Elements("channel").Count() > 0)
                )
                {
                    //Nur wenn ein Angebot eines Channels vorhanden ist
                    if (myhotelresult.Elements("channel").Count() > 0)
                    {
                        //Nur wenn kein Ab Preis vorhanden ist
                        if (myhotelresult.Element("channel").Element("from_price") == null)
                        {
                            List<Tuple<int, double>> cheapestofferlist =
                                new List<Tuple<int, double>>();

                            List<Tuple<int, double>> cheapestofferlist_ws =
                                new List<Tuple<int, double>>();
                            List<Tuple<int, double>> cheapestofferlist_bb =
                                new List<Tuple<int, double>>();
                            List<Tuple<int, double>> cheapestofferlist_hb =
                                new List<Tuple<int, double>>();
                            List<Tuple<int, double>> cheapestofferlist_fb =
                                new List<Tuple<int, double>>();
                            List<Tuple<int, double>> cheapestofferlist_ai =
                                new List<Tuple<int, double>>();

                            var mychanneloffers = myhotelresult.Elements("channel");

                            //double cheapestchanneloffertemp = Double.MaxValue;
                            //double cheapestchanneloffer = Double.MaxValue;

                            foreach (var mychanneloffer in mychanneloffers)
                            {
                                MssResponseShort myresp = new MssResponseShort();

                                myresp.HotelId = Convert.ToInt32(myhotelresult.Element("id").Value);
                                //Magari speicher i mer do die offerid o


                                myresp.Bookable = true;

                                myresp.ChannelID = mychanneloffer.Element("channel_id").Value;

                                //if (mychanneloffer.Element("channel_link") != null)
                                //    myresp.Channellink = mychanneloffer.Element("channel_link").Value;

                                //spezialfall Booking Südtirol Buchungslink selbst zusammenstellen
                                if (
                                    myresp.ChannelID == "esy"
                                    || myresp.ChannelID == "lts"
                                    || myresp.ChannelID == "hgv"
                                )
                                {
                                    //http://www.bookingsuedtirol.com/index.php?action=view&src=sinfo&id=10096&result_id=ec373a3d116ce01806a1c5f9f110c16b&room_qty_0=1&room_occ_0=18&room_rid_0=3124
                                    //http://www.bookingsuedtirol.com/index.php?action=view&src=sinfo&id=10096&result_id=fba3a11c1a9cf7c9ce636e10188507bd&room_qty_0=1&room_occ_0=18&room_rid_0=3124&room_qty_1=1&room_occ_1=18,18&room_rid_1=3125

                                    string roomstring = "";

                                    int roomcounter = 0;
                                    foreach (var room in roompersons)
                                    {
                                        string person = String.Join(",", room.Person.ToArray());

                                        roomstring = roomstring + "room_qty_" + roomcounter + "=1&";
                                        roomstring =
                                            roomstring
                                            + "room_occ_"
                                            + roomcounter
                                            + "="
                                            + person
                                            + "&";

                                        roomcounter++;
                                    }

                                    string bookingurl = "https://www.bookingsuedtirol.com";

                                    if (lang.ToLower() == "it")
                                        bookingurl = "https://www.bookingaltoadige.com";
                                    else if (
                                        lang.ToLower() == "en"
                                        || lang.ToLower() == "nl"
                                        || lang.ToLower() == "fr"
                                        || lang.ToLower() == "ru"
                                        || lang.ToLower() == "pl"
                                        || lang.ToLower() == "cs"
                                    )
                                        bookingurl = "https://www.bookingsouthtyrol.com";

                                    string bookinglink =
                                        bookingurl
                                        + "/index.php?action=view&src=sbalance&id="
                                        + myresp.HotelId
                                        + "&result_id="
                                        + resultid
                                        + "&"
                                        + roomstring;

                                    bookinglink = bookinglink.Substring(0, bookinglink.Length - 1);

                                    string myofferidforlink = "";

                                    if (
                                        mychanneloffer
                                            .Element("offer_description")
                                            .Element("offer")
                                            .Element("offer_id")
                                            .Value != null
                                    )
                                        myofferidforlink = mychanneloffer
                                            .Element("offer_description")
                                            .Element("offer")
                                            .Element("offer_id")
                                            .Value;

                                    myresp.Channellink = bookinglink + "#_pid=" + myofferidforlink;
                                }

                                var myofferdescription = mychanneloffer.Element(
                                    "offer_description"
                                );

                                if (myofferdescription != null)
                                {
                                    var myofferdetails = myofferdescription.Element("offer");
                                    if (myofferdetails != null)
                                    {
                                        int offershow = 0;
                                        if (myofferdetails.Element("offer_gid") != null)
                                            myresp.OfferGid = !String.IsNullOrEmpty(
                                                myofferdetails.Element("offer_gid").Value
                                            )
                                                ? myofferdetails.Element("offer_gid").Value
                                                : "";
                                        if (myofferdetails.Element("offer_id") != null)
                                            myresp.OfferId = !String.IsNullOrEmpty(
                                                myofferdetails.Element("offer_id").Value
                                            )
                                                ? myofferdetails.Element("offer_id").Value
                                                : "";
                                        if (myofferdetails.Element("offer_show") != null)
                                            myresp.OfferShow = Int32.TryParse(
                                                myofferdetails.Element("offer_show").Value,
                                                out offershow
                                            )
                                                ? offershow
                                                : 0;
                                        int offertyp = 0;
                                        if (myofferdetails.Element("offer_typ") != null)
                                            myresp.OfferTyp = Int32.TryParse(
                                                myofferdetails.Element("offer_typ").Value,
                                                out offertyp
                                            )
                                                ? offertyp
                                                : 0;

                                        if (myofferdetails.Element("offer_id") != null)
                                            myresp.A0RID =
                                                "Package"
                                                + myofferdetails.Element("offer_id").Value;
                                    }
                                }

                                var myroomdetails = mychanneloffer
                                    .Element("room_price")
                                    .Elements("price");
                                foreach (var myroomdetail in myroomdetails)
                                {
                                    RoomDetails myroom = new RoomDetails();

                                    string roomid = myroomdetail.Element("room_id").Value;

                                    myroom.RoomId = roomid;

                                    int roomseq = 0;
                                    if (myroomdetail.Element("room_seq") != null)
                                        myroom.RoomSeq = Int32.TryParse(
                                            myroomdetail.Element("room_seq").Value,
                                            out roomseq
                                        )
                                            ? roomseq
                                            : 0;

                                    //Mal kucken wiamer do tian wenn olle verpflegungsorten ausgwählt sein gazzo

                                    var splittedservice = servicecode.Split(',').ToList();
                                    foreach (var requestedservice in splittedservice)
                                    {
                                        if (requestedservice == "price_ws")
                                        {
                                            double mycurrentpricews =
                                                myroomdetail
                                                    .Element("price_total")
                                                    .Element(requestedservice) != null
                                                    ? Convert.ToDouble(
                                                        myroomdetail
                                                            .Element("price_total")
                                                            .Element(requestedservice)
                                                            .Value,
                                                        CultureInfo.InvariantCulture.NumberFormat
                                                    )
                                                    : 0;
                                            myroom.Price_ws = mycurrentpricews;

                                            if (mycurrentpricews > 0)
                                            {
                                                var mytuplews = new Tuple<int, double>(
                                                    (int)myroom.RoomSeq,
                                                    mycurrentpricews
                                                );
                                                cheapestofferlist_ws.Add(mytuplews);
                                            }
                                        }
                                        if (requestedservice == "price_bb")
                                        {
                                            double mycurrentpricebb =
                                                myroomdetail
                                                    .Element("price_total")
                                                    .Element(requestedservice) != null
                                                    ? Convert.ToDouble(
                                                        myroomdetail
                                                            .Element("price_total")
                                                            .Element(requestedservice)
                                                            .Value,
                                                        CultureInfo.InvariantCulture.NumberFormat
                                                    )
                                                    : 0;
                                            myroom.Price_bb = mycurrentpricebb;

                                            if (mycurrentpricebb > 0)
                                            {
                                                var mytuplebb = new Tuple<int, double>(
                                                    (int)myroom.RoomSeq,
                                                    mycurrentpricebb
                                                );
                                                cheapestofferlist_bb.Add(mytuplebb);
                                            }
                                        }
                                        if (requestedservice == "price_hb")
                                        {
                                            double mycurrentpricehb =
                                                myroomdetail
                                                    .Element("price_total")
                                                    .Element(requestedservice) != null
                                                    ? Convert.ToDouble(
                                                        myroomdetail
                                                            .Element("price_total")
                                                            .Element(requestedservice)
                                                            .Value,
                                                        CultureInfo.InvariantCulture.NumberFormat
                                                    )
                                                    : 0;
                                            myroom.Price_hb = mycurrentpricehb;

                                            if (mycurrentpricehb > 0)
                                            {
                                                var mytuplehb = new Tuple<int, double>(
                                                    (int)myroom.RoomSeq,
                                                    mycurrentpricehb
                                                );
                                                cheapestofferlist_hb.Add(mytuplehb);
                                            }
                                        }
                                        if (requestedservice == "price_fb")
                                        {
                                            double mycurrentpricefb =
                                                myroomdetail
                                                    .Element("price_total")
                                                    .Element(requestedservice) != null
                                                    ? Convert.ToDouble(
                                                        myroomdetail
                                                            .Element("price_total")
                                                            .Element(requestedservice)
                                                            .Value,
                                                        CultureInfo.InvariantCulture.NumberFormat
                                                    )
                                                    : 0;
                                            myroom.Price_fb = mycurrentpricefb;

                                            if (mycurrentpricefb > 0)
                                            {
                                                var mytuplefb = new Tuple<int, double>(
                                                    (int)myroom.RoomSeq,
                                                    mycurrentpricefb
                                                );
                                                cheapestofferlist_fb.Add(mytuplefb);
                                            }
                                        }
                                        if (requestedservice == "price_ai")
                                        {
                                            double mycurrentpriceai =
                                                myroomdetail
                                                    .Element("price_total")
                                                    .Element(requestedservice) != null
                                                    ? Convert.ToDouble(
                                                        myroomdetail
                                                            .Element("price_total")
                                                            .Element(requestedservice)
                                                            .Value,
                                                        CultureInfo.InvariantCulture.NumberFormat
                                                    )
                                                    : 0;
                                            myroom.Price_ai = mycurrentpriceai;

                                            if (mycurrentpriceai > 0)
                                            {
                                                var mytupleai = new Tuple<int, double>(
                                                    (int)myroom.RoomSeq,
                                                    mycurrentpriceai
                                                );
                                                cheapestofferlist_ai.Add(mytupleai);
                                            }
                                        }
                                    }

                                    //Spezial falls mehrere Services angefragt werden
                                    if (splittedservice.Count == 1)
                                    {
                                        double mycurrentprice = !String.IsNullOrEmpty(
                                            myroomdetail
                                                .Element("price_total")
                                                .Element(servicecode)
                                                .Value
                                        )
                                            ? Convert.ToDouble(
                                                myroomdetail
                                                    .Element("price_total")
                                                    .Element(servicecode)
                                                    .Value,
                                                CultureInfo.InvariantCulture.NumberFormat
                                            )
                                            : 0;

                                        var mytuple = new Tuple<int, double>(
                                            (int)myroom.RoomSeq,
                                            mycurrentprice
                                        );
                                        cheapestofferlist.Add(mytuple);

                                        myroom.TotalPrice = mycurrentprice;
                                        myroom.TotalPriceString = String.Format(
                                            culturede,
                                            "{0:0,0.00}",
                                            mycurrentprice
                                        );
                                    }
                                    else
                                    {
                                        //billigsten preis suchen


                                        myroom.TotalPrice = 0;
                                        myroom.TotalPriceString = String.Format(
                                            culturede,
                                            "{0:0,0.00}",
                                            0
                                        );
                                    }

                                    //myroom.Service = servicecode;

                                    //if (mycurrentprice < cheapestpricetemp)
                                    //{
                                    //    cheapestchannel = mychanneloffer.Element("channel_id").Value;
                                    //    cheapestprice = mycurrentprice;
                                    //    cheapestpricetemp = mycurrentprice;
                                    //}
                                    //if (mycurrentprice < cheapestchanneloffertemp)
                                    //{
                                    //    cheapestchanneloffer = mycurrentprice;
                                    //    cheapestchanneloffertemp = mycurrentprice;
                                    //}

                                    var myroomdesc = mychanneloffer
                                        .Element("room_description")
                                        .Elements("room")
                                        .Where(x => x.Element("room_id").Value == roomid)
                                        .FirstOrDefault();
                                    if (myroomdesc != null)
                                    {
                                        if (myroomdesc.Element("title") != null)
                                            myroom.Roomtitle = myroomdesc.Element("title").Value;
                                        if (myroomdesc.Element("description") != null)
                                            myroom.Roomdesc = myroomdesc
                                                .Element("description")
                                                .Value;

                                        if (myroomdesc.Element("room_type") != null)
                                            myroom.Roomtype = !String.IsNullOrEmpty(
                                                myroomdesc.Element("room_type").Value
                                            )
                                                ? Convert.ToInt32(
                                                    myroomdesc.Element("room_type").Value
                                                )
                                                : 0;
                                        if (myroomdesc.Element("room_free") != null)
                                            myroom.Roomfree = !String.IsNullOrEmpty(
                                                myroomdesc.Element("room_free").Value
                                            )
                                                ? Convert.ToInt32(
                                                    myroomdesc.Element("room_free").Value
                                                )
                                                : 0;

                                        //Zimmerbilder
                                        var roompics = myroomdesc.Elements("pictures");
                                        List<RoomPictures> myroompiclist = new List<RoomPictures>();
                                        foreach (var myroompic in roompics.Elements("picture"))
                                        {
                                            if (myroompic.Element("url") != null)
                                            {
                                                RoomPictures mypicture = new RoomPictures()
                                                {
                                                    Pictureurl = myroompic.Element("url").Value,
                                                };
                                                //myroom.RoomPictures.Add(mypicture);

                                                myroompiclist.Add(mypicture);
                                            }
                                        }
                                        myroom.RoomPictures = myroompiclist.ToList();
                                    }
                                    //myroom.MssResponseShort = myresp;

                                    myresp.RoomDetails.Add(myroom);
                                }

                                double cheapestchanneloffer = 0;
                                //Billigstes Angebot holen
                                for (int i = 1; i <= rooms; i++)
                                {
                                    var cheapestchanneloffertemp = (
                                        from x in cheapestofferlist
                                        where x.Item1 == i
                                        orderby x.Item2 ascending
                                        select x.Item2
                                    ).FirstOrDefault();

                                    //if (cheapestchanneloffertemp == null)
                                    //    cheapestchanneloffertemp = 0;

                                    cheapestchanneloffer =
                                        cheapestchanneloffer + cheapestchanneloffertemp;
                                }

                                myresp.CheapestOffer = cheapestchanneloffer;
                                myresp.CheapestOfferString = String.Format(
                                    culturede,
                                    "{0:0,0.00}",
                                    cheapestchanneloffer
                                );

                                //Billigstes Angebot für die einzelnen Typen

                                double cheapestchanneloffer_ws = 0;
                                for (int i = 1; i <= rooms; i++)
                                {
                                    var cheapestchanneloffertemp = (
                                        from x in cheapestofferlist_ws
                                        where x.Item1 == i
                                        orderby x.Item2 ascending
                                        select x.Item2
                                    ).FirstOrDefault();

                                    cheapestchanneloffer_ws =
                                        cheapestchanneloffer_ws + cheapestchanneloffertemp;
                                }
                                myresp.CheapestOffer_ws = cheapestchanneloffer_ws;

                                double cheapestchanneloffer_bb = 0;
                                for (int i = 1; i <= rooms; i++)
                                {
                                    var cheapestchanneloffertemp = (
                                        from x in cheapestofferlist_bb
                                        where x.Item1 == i
                                        orderby x.Item2 ascending
                                        select x.Item2
                                    ).FirstOrDefault();

                                    cheapestchanneloffer_bb =
                                        cheapestchanneloffer_bb + cheapestchanneloffertemp;
                                }
                                myresp.CheapestOffer_bb = cheapestchanneloffer_bb;

                                double cheapestchanneloffer_hb = 0;
                                for (int i = 1; i <= rooms; i++)
                                {
                                    var cheapestchanneloffertemp = (
                                        from x in cheapestofferlist_hb
                                        where x.Item1 == i
                                        orderby x.Item2 ascending
                                        select x.Item2
                                    ).FirstOrDefault();

                                    cheapestchanneloffer_hb =
                                        cheapestchanneloffer_hb + cheapestchanneloffertemp;
                                }
                                myresp.CheapestOffer_hb = cheapestchanneloffer_hb;

                                double cheapestchanneloffer_fb = 0;
                                for (int i = 1; i <= rooms; i++)
                                {
                                    var cheapestchanneloffertemp = (
                                        from x in cheapestofferlist_fb
                                        where x.Item1 == i
                                        orderby x.Item2 ascending
                                        select x.Item2
                                    ).FirstOrDefault();

                                    cheapestchanneloffer_fb =
                                        cheapestchanneloffer_fb + cheapestchanneloffertemp;
                                }
                                myresp.CheapestOffer_fb = cheapestchanneloffer_fb;

                                double cheapestchanneloffer_ai = 0;
                                for (int i = 1; i <= rooms; i++)
                                {
                                    var cheapestchanneloffertemp = (
                                        from x in cheapestofferlist_ai
                                        where x.Item1 == i
                                        orderby x.Item2 ascending
                                        select x.Item2
                                    ).FirstOrDefault();

                                    cheapestchanneloffer_ai =
                                        cheapestchanneloffer_ai + cheapestchanneloffertemp;
                                }
                                myresp.CheapestOffer_ai = cheapestchanneloffer_ai;

                                myparsedresponselist.MssResponseShort.Add(myresp);
                            }
                        }
                    }
                }
                ;

                //myparsedresponselist.CheapestChannel = cheapestchannel;
                //myparsedresponselist.Cheapestprice = cheapestprice;

                myparsedresponselist.CheapestChannel = "";
                myparsedresponselist.Cheapestprice = 0;

                return myparsedresponselist;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
