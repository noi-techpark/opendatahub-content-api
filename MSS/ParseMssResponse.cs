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
using Helper;

namespace MSS
{
    public class ParseMssResponse
    {
        public static MssResult ParsemyMssResponse(
            string lang,
            string servicecode,
            XDocument mssresponse,
            List<MssRoom> myroompersons,
            string requestsource,
            string version,
            bool withoutids
        )
        {
            try
            {
                var resultid = mssresponse.Root.Element("header").Element("result_id").Value;

                var myresult = mssresponse
                    .Root.Elements("root")
                    .Elements("result")
                    .Elements("hotel");

                return ResponseParser(
                    myresult,
                    servicecode,
                    myroompersons,
                    resultid,
                    requestsource,
                    lang,
                    version
                );
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static MssResult ParsemyMssResponse(
            string lang,
            string servicecode,
            XDocument mssresponse,
            List<string> A0Rids,
            List<MssRoom> myroompersons,
            string requestsource,
            string version,
            bool withoutids
        )
        {
            try
            {
                foreach (
                    XElement mytime in mssresponse
                        .Root.Elements("root")
                        .Elements("header")
                        .Elements("time")
                )
                {
                    Console.WriteLine("Requestdauer: " + mytime.Value);
                }

                var resultid = mssresponse.Root.Element("header").Element("result_id").Value;

                var myresult = mssresponse
                    .Root.Elements("root")
                    .Elements("result")
                    .Elements("hotel")
                    .Where(x =>
                        A0Rids.Contains(x.Element("id_lts").Value.ToLower())
                        && x.Elements("channel").Count() > 0
                    );

                if (A0Rids.Count == 0 && withoutids)
                    myresult = mssresponse
                        .Elements("result")
                        .Elements("hotel")
                        .Where(x => x.Elements("channel").Count() > 0);

                return ResponseParser(
                    myresult,
                    servicecode,
                    myroompersons,
                    resultid,
                    requestsource,
                    lang,
                    version
                );
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static MssResult ParsemyMssResponse(
            string lang,
            string servicecode,
            XElement mssresponse,
            List<string> A0Rids,
            List<MssRoom> myroompersons,
            string requestsource,
            string version,
            bool withoutids
        )
        {
            try
            {
                foreach (XElement mytime in mssresponse.Elements("header").Elements("time"))
                {
                    Console.WriteLine("Requestdauer: " + mytime.Value);
                }

                var resultid = mssresponse.Element("header").Element("result_id").Value;

                var myresult = mssresponse
                    .Elements("result")
                    .Elements("hotel")
                    .Where(x =>
                        A0Rids.Contains(x.Element("id_lts").Value.ToUpper())
                        && x.Elements("channel").Count() > 0
                    );

                if (A0Rids.Count == 0 && withoutids)
                    myresult = mssresponse
                        .Elements("result")
                        .Elements("hotel")
                        .Where(x => x.Elements("channel").Count() > 0);

                return ResponseParser(
                    myresult,
                    servicecode,
                    myroompersons,
                    resultid,
                    requestsource,
                    lang,
                    version
                );
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static MssResult ResponseParser(
            IEnumerable<XElement> myresult,
            string servicecode,
            List<MssRoom> roompersons,
            string resultid,
            string requestsource,
            string lang,
            string version
        )
        {
            try
            {
                MssResult myparsedresponselist = new MssResult();
                myparsedresponselist.ResultId = resultid;

                CultureInfo culturede = CultureInfo.CreateSpecificCulture("de");

                int rooms = roompersons.Count;

                List<XElement> poslist = myresult.Elements("pos").Elements("id_pos").ToList();

                foreach (
                    var myhotelresult in myresult.Where(x => x.Elements("channel").Count() > 0)
                )
                {
                    //Only if there is an offer, and a valid LTS RID
                    if (
                        myhotelresult.Elements("channel").Count() > 0
                        && !String.IsNullOrEmpty(myhotelresult.Element("id_lts").Value)
                    )
                    {
                        List<CheapestOffer> cheapestofferlist = new List<CheapestOffer>();

                        List<CheapestOffer> cheapestofferlist_ws = new List<CheapestOffer>();
                        List<CheapestOffer> cheapestofferlist_bb = new List<CheapestOffer>();
                        List<CheapestOffer> cheapestofferlist_hb = new List<CheapestOffer>();
                        List<CheapestOffer> cheapestofferlist_fb = new List<CheapestOffer>();
                        List<CheapestOffer> cheapestofferlist_ai = new List<CheapestOffer>();

                        var mychanneloffers = myhotelresult.Elements("channel");

                        foreach (var mychanneloffer in mychanneloffers)
                        {
                            MssResponseShort myresp = new MssResponseShort();

                            myresp.HotelId = Convert.ToInt32(myhotelresult.Element("id").Value);
                            myresp.A0RID = myhotelresult.Element("id_lts").Value;
                            if (myhotelresult.Element("bookable").Value == "1")
                                myresp.Bookable = true;
                            else
                                myresp.Bookable = false;

                            myresp.ChannelID = mychanneloffer.Element("channel_id").Value;

                            //Addition: funktioniert nur mit Version 1:
                            var myonlinepayment = myhotelresult.Element("online_payment");
                            if (myonlinepayment != null)
                            {
                                myresp.OnlinepaymentMethods =
                                    myonlinepayment.Element("methods") != null
                                        ? myonlinepayment.Element("methods").Value
                                        : "";
                                myresp.OnlinepaymentPrepayment =
                                    myonlinepayment.Element("prepayment") != null
                                        ? myonlinepayment.Element("prepayment").Value
                                        : "";
                                myresp.OnlinepaymentCCards =
                                    myonlinepayment.Element("ccards") != null
                                        ? myonlinepayment.Element("ccards").Value
                                        : "";
                            }

                            if (myresp.ChannelID == "pos")
                            {
                                string posid = poslist
                                    .Where(x =>
                                        x.Value != "sinfo"
                                        && x.Value != "bok"
                                        && x.Value != "esy"
                                        && x.Value != "lts"
                                        && x.Value != "htl"
                                        && x.Value != "hgv"
                                        && x.Value != "exp"
                                    )
                                    .FirstOrDefault()
                                    .Value;

                                switch (posid)
                                {
                                    //79490C3D4CE4864B0F1063ECEA9354D Alta Badia ??? 679490C3D4CE4864B0F1063ECEA9354D
                                    //DD6216B423FF4ABFA0F272B4B2343260 Kronplatz
                                    //A3F616771D0547D98496810C14F7FDE8 Hochpustertal
                                    //2313018779054362969E1A47B514678E Eggental
                                    //D396337B6995467693054ACB6E12D57D Seiser Alm
                                    //917D6175386A4EAEAB0564482E3F0728 Südtirols Süden
                                    //3C9A4171C7A84AB881FB07FCE3F972EF Val Gardena Marketing

                                    case "679490C3D4CE4864B0F1063ECEA9354D":
                                        myresp.ChannelID = "altabadia";
                                        break;
                                    case "DD6216B423FF4ABFA0F272B4B2343260":
                                        myresp.ChannelID = "kronplatz";
                                        break;
                                    case "A3F616771D0547D98496810C14F7FDE8":
                                        myresp.ChannelID = "hochpustertal";
                                        break;
                                    case "2313018779054362969E1A47B514678E":
                                        myresp.ChannelID = "eggental";
                                        break;
                                    case "D396337B6995467693054ACB6E12D57D":
                                        myresp.ChannelID = "seiseralm";
                                        break;
                                    case "917D6175386A4EAEAB0564482E3F0728":
                                        myresp.ChannelID = "suedtirolssueden";
                                        break;
                                    case "3C9A4171C7A84AB881FB07FCE3F972EF":
                                        myresp.ChannelID = "valgardena";
                                        break;
                                    case "0F41198A98244216B92C1FD6972412C3":
                                        myresp.ChannelID = "valgardenait";
                                        break;
                                }
                            }

                            if (mychanneloffer.Element("channel_link") != null)
                                myresp.Channellink = mychanneloffer.Element("channel_link").Value;

                            //Specialcase build the bookinglink for Booking Südtirol
                            if (
                                myresp.ChannelID == "esy"
                                || myresp.ChannelID == "lts"
                                || myresp.ChannelID == "hgv"
                            )
                            {
                                //http://www.bookingsuedtirol.com/index.php?action=view&src=" + + "&id=10096&result_id=ec373a3d116ce01806a1c5f9f110c16b&room_qty_0=1&room_occ_0=18&room_rid_0=3124
                                //http://www.bookingsuedtirol.com/index.php?action=view&src=sinfo&id=10096&result_id=fba3a11c1a9cf7c9ce636e10188507bd&room_qty_0=1&room_occ_0=18&room_rid_0=3124&room_qty_1=1&room_occ_1=18,18&room_rid_1=3125

                                string roomstring = "";

                                int roomcounter = 0;
                                foreach (var room in roompersons)
                                {
                                    string person = String.Join(",", room.Person.ToArray());

                                    roomstring = roomstring + "room_qty_" + roomcounter + "=1&";
                                    roomstring =
                                        roomstring + "room_occ_" + roomcounter + "=" + person + "&";

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
                                    + "/index.php?action=view&src="
                                    + requestsource
                                    + "&id="
                                    + myresp.HotelId
                                    + "&result_id="
                                    + resultid
                                    + "&"
                                    + roomstring;

                                myresp.Channellink = bookinglink.Substring(
                                    0,
                                    bookinglink.Length - 1
                                );
                            }

                            var myofferdescription = mychanneloffer.Element("offer_description");

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

                                string offerid = myroomdetail.Element("offer_id").Value;

                                myroom.OfferId = offerid;

                                int roomseq = 0;
                                if (myroomdetail.Element("room_seq") != null)
                                    myroom.RoomSeq = Int32.TryParse(
                                        myroomdetail.Element("room_seq").Value,
                                        out roomseq
                                    )
                                        ? roomseq
                                        : 0;

                                //NEU MSS VERSION 2.0
                                if (version == "2")
                                {
                                    string cancelpolicyid =
                                        myroomdetail.Element("cancel_policy_id") != null
                                            ? myroomdetail.Element("cancel_policy_id").Value
                                            : null;
                                    string paymenttermid =
                                        myroomdetail.Element("cancel_policy_id") != null
                                            ? myroomdetail.Element("payment_term_id").Value
                                            : null;

                                    if (!String.IsNullOrEmpty(paymenttermid))
                                    {
                                        var mypaymentterm = mychanneloffer
                                            .Element("payment_terms")
                                            .Elements("payment_term")
                                            .Where(x => x.Element("id").Value == paymenttermid)
                                            .FirstOrDefault();

                                        if (mypaymentterm != null)
                                        {
                                            PaymentTerm paymentterm = new PaymentTerm();
                                            paymentterm.Id =
                                                mypaymentterm.Element("id") != null
                                                    ? mypaymentterm.Element("id").Value
                                                    : "";
                                            paymentterm.Methods =
                                                mypaymentterm.Element("methods") != null
                                                    ? Convert.ToInt32(
                                                        mypaymentterm.Element("methods").Value
                                                    )
                                                    : 0;
                                            paymentterm.Prepayment =
                                                mypaymentterm.Element("prepayment") != null
                                                    ? Convert.ToInt32(
                                                        mypaymentterm.Element("prepayment").Value
                                                    )
                                                    : 0;
                                            paymentterm.Ccards =
                                                mypaymentterm.Element("ccards") != null
                                                    ? Convert.ToInt32(
                                                        mypaymentterm.Element("ccards").Value
                                                    )
                                                    : 0;
                                            paymentterm.Description =
                                                mypaymentterm.Element("description") != null
                                                    ? mypaymentterm.Element("description").Value
                                                    : "";
                                            paymentterm.Priority =
                                                mypaymentterm.Element("priority") != null
                                                    ? Convert.ToInt32(
                                                        mypaymentterm.Element("priority").Value
                                                    )
                                                    : 0;

                                            if (mypaymentterm.Element("bank") != null)
                                            {
                                                Bank bank = new Bank();
                                                bank.Name =
                                                    mypaymentterm.Element("bank").Element("name")
                                                    != null
                                                        ? mypaymentterm
                                                            .Element("bank")
                                                            .Element("name")
                                                            .Value
                                                        : "";
                                                bank.Iban =
                                                    mypaymentterm.Element("bank").Element("iban")
                                                    != null
                                                        ? mypaymentterm
                                                            .Element("bank")
                                                            .Element("iban")
                                                            .Value
                                                        : "";
                                                bank.Swift =
                                                    mypaymentterm.Element("bank").Element("swift")
                                                    != null
                                                        ? mypaymentterm
                                                            .Element("bank")
                                                            .Element("swift")
                                                            .Value
                                                        : "";

                                                paymentterm.Bank = bank;
                                            }

                                            myroom.PaymentTerm = paymentterm;
                                        }
                                    }
                                    if (!String.IsNullOrEmpty(cancelpolicyid))
                                    {
                                        var mycancelpolicy = mychanneloffer
                                            .Element("cancel_policies")
                                            .Elements("cancel_policy")
                                            .Where(x => x.Element("id").Value == cancelpolicyid)
                                            .FirstOrDefault();

                                        if (mycancelpolicy != null)
                                        {
                                            CancelPolicy cancelpolicy = new CancelPolicy();

                                            cancelpolicy.Id =
                                                mycancelpolicy.Element("id") != null
                                                    ? mycancelpolicy.Element("id").Value
                                                    : "";
                                            if (mycancelpolicy.Element("refundable") != null)
                                                cancelpolicy.Refundable = Convert.ToInt32(
                                                    mycancelpolicy.Element("refundable").Value
                                                );
                                            else
                                                cancelpolicy.Refundable = null;

                                            if (mycancelpolicy.Element("refundable_until") != null)
                                            {
                                                if (
                                                    !String.IsNullOrEmpty(
                                                        mycancelpolicy
                                                            .Element("refundable_until")
                                                            .Value
                                                    )
                                                )
                                                    cancelpolicy.RefundableUntil =
                                                        Convert.ToDateTime(
                                                            mycancelpolicy
                                                                .Element("refundable_until")
                                                                .Value
                                                        );
                                                else
                                                    cancelpolicy.RefundableUntil = null;
                                            }
                                            else
                                                cancelpolicy.RefundableUntil = null;

                                            cancelpolicy.Description =
                                                mycancelpolicy.Element("description") != null
                                                    ? mycancelpolicy.Element("description").Value
                                                    : "";

                                            if (mycancelpolicy.Element("penalties") != null)
                                            {
                                                cancelpolicy.Penalties = new List<Penalty>();

                                                foreach (
                                                    var penalty in mycancelpolicy
                                                        .Element("penalties")
                                                        .Elements("penalty")
                                                )
                                                {
                                                    Penalty mypenalty = new Penalty();

                                                    if (penalty.Element("percent") != null)
                                                        mypenalty.Percent = Convert.ToInt32(
                                                            penalty.Element("percent").Value
                                                        );
                                                    else
                                                        mypenalty.Percent = null;

                                                    if (penalty.Element("datefrom") != null)
                                                        if (
                                                            String.IsNullOrEmpty(
                                                                penalty.Element("datefrom").Value
                                                            )
                                                        )
                                                            mypenalty.Datefrom = null;
                                                        else
                                                            Convert.ToDateTime(
                                                                penalty.Element("datefrom").Value
                                                            );
                                                    else
                                                        mypenalty.Datefrom = null;

                                                    if (penalty.Element("daysarrival") != null)
                                                        mypenalty.Daysarrival = Convert.ToInt32(
                                                            penalty.Element("daysarrival").Value
                                                        );
                                                    else
                                                        mypenalty.Daysarrival = null;

                                                    cancelpolicy.Penalties.Add(mypenalty);
                                                }
                                            }

                                            myroom.CancelPolicy = cancelpolicy;
                                        }
                                    }
                                }

                                //ENDE NEU VERSION 2.0

                                //TODO Test and ensure functionality
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
                                            cheapestofferlist_ws.Add(
                                                new CheapestOffer()
                                                {
                                                    RoomId = myroom.RoomId,
                                                    RoomSeq = (int)myroom.RoomSeq,
                                                    Price = mycurrentpricews,
                                                    RoomFree = 0,
                                                }
                                            );
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
                                            cheapestofferlist_bb.Add(
                                                new CheapestOffer()
                                                {
                                                    RoomId = myroom.RoomId,
                                                    RoomSeq = (int)myroom.RoomSeq,
                                                    Price = mycurrentpricebb,
                                                    RoomFree = 0,
                                                }
                                            );
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
                                            cheapestofferlist_hb.Add(
                                                new CheapestOffer()
                                                {
                                                    RoomId = myroom.RoomId,
                                                    RoomSeq = (int)myroom.RoomSeq,
                                                    Price = mycurrentpricehb,
                                                    RoomFree = 0,
                                                }
                                            );
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
                                            cheapestofferlist_fb.Add(
                                                new CheapestOffer()
                                                {
                                                    RoomId = myroom.RoomId,
                                                    RoomSeq = (int)myroom.RoomSeq,
                                                    Price = mycurrentpricefb,
                                                    RoomFree = 0,
                                                }
                                            );
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
                                            cheapestofferlist_ai.Add(
                                                new CheapestOffer()
                                                {
                                                    RoomId = myroom.RoomId,
                                                    RoomSeq = (int)myroom.RoomSeq,
                                                    Price = mycurrentpriceai,
                                                    RoomFree = 0,
                                                }
                                            );
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

                                    cheapestofferlist.Add(
                                        new CheapestOffer()
                                        {
                                            RoomId = myroom.RoomId,
                                            RoomSeq = (int)myroom.RoomSeq,
                                            Price = mycurrentprice,
                                            RoomFree = 0,
                                        }
                                    );

                                    myroom.TotalPrice = mycurrentprice;
                                    myroom.TotalPriceString = String.Format(
                                        culturede,
                                        "{0:0,0.00}",
                                        mycurrentprice
                                    );
                                }
                                else
                                {
                                    //billigsten preis suchen TODO!
                                    myroom.TotalPrice = 0;
                                    myroom.TotalPriceString = String.Format(
                                        culturede,
                                        "{0:0,0.00}",
                                        0
                                    );
                                }

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
                                        myroom.Roomdesc = myroomdesc.Element("description").Value;

                                    if (myroomdesc.Element("room_type") != null)
                                        myroom.Roomtype = !String.IsNullOrEmpty(
                                            myroomdesc.Element("room_type").Value
                                        )
                                            ? Convert.ToInt32(myroomdesc.Element("room_type").Value)
                                            : 0;
                                    if (myroomdesc.Element("room_free") != null)
                                        myroom.Roomfree = !String.IsNullOrEmpty(
                                            myroomdesc.Element("room_free").Value
                                        )
                                            ? Convert.ToInt32(myroomdesc.Element("room_free").Value)
                                            : 0;

                                    //TODO add Roomfree also to cheapeast channel list
                                    if (myroom.Roomfree != null && myroom.Roomfree > 0)
                                    {
                                        foreach (
                                            var cheapeastoffersingle in cheapestofferlist
                                                .Where(x => x.RoomId == myroom.RoomId)
                                                .ToList()
                                        )
                                        {
                                            cheapeastoffersingle.RoomFree = myroom.Roomfree;
                                        }
                                        foreach (
                                            var cheapeastoffersingle in cheapestofferlist_ws
                                                .Where(x => x.RoomId == myroom.RoomId)
                                                .ToList()
                                        )
                                        {
                                            cheapeastoffersingle.RoomFree = myroom.Roomfree;
                                        }
                                        foreach (
                                            var cheapeastoffersingle in cheapestofferlist_bb
                                                .Where(x => x.RoomId == myroom.RoomId)
                                                .ToList()
                                        )
                                        {
                                            cheapeastoffersingle.RoomFree = myroom.Roomfree;
                                        }
                                        foreach (
                                            var cheapeastoffersingle in cheapestofferlist_hb
                                                .Where(x => x.RoomId == myroom.RoomId)
                                                .ToList()
                                        )
                                        {
                                            cheapeastoffersingle.RoomFree = myroom.Roomfree;
                                        }
                                        foreach (
                                            var cheapeastoffersingle in cheapestofferlist_fb
                                                .Where(x => x.RoomId == myroom.RoomId)
                                                .ToList()
                                        )
                                        {
                                            cheapeastoffersingle.RoomFree = myroom.Roomfree;
                                        }
                                        foreach (
                                            var cheapeastoffersingle in cheapestofferlist_ai
                                                .Where(x => x.RoomId == myroom.RoomId)
                                                .ToList()
                                        )
                                        {
                                            cheapeastoffersingle.RoomFree = myroom.Roomfree;
                                        }
                                    }

                                    //Zusatzinfo Maximal Minimal und Standardbelegung
                                    if (myroomdesc.Element("properties") != null)
                                    {
                                        var myroomprops = myroomdesc.Element("properties");

                                        if (myroomprops.Element("min") != null)
                                            myroom.Roommin = !String.IsNullOrEmpty(
                                                myroomprops.Element("min").Value
                                            )
                                                ? Convert.ToInt32(myroomprops.Element("min").Value)
                                                : 0;
                                        if (myroomprops.Element("max") != null)
                                            myroom.Roommax = !String.IsNullOrEmpty(
                                                myroomprops.Element("max").Value
                                            )
                                                ? Convert.ToInt32(myroomprops.Element("max").Value)
                                                : 0;
                                        if (myroomprops.Element("std") != null)
                                            myroom.Roomstd = !String.IsNullOrEmpty(
                                                myroomprops.Element("std").Value
                                            )
                                                ? Convert.ToInt32(myroomprops.Element("std").Value)
                                                : 0;
                                    }

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

                            var cheapestofferobj = RoomCalculationHelper.CalculateCheapestRooms(
                                cheapestofferlist,
                                rooms,
                                ""
                            );
                            double cheapestchanneloffer = cheapestofferobj.Price;

                            myresp.CheapestOffer = cheapestchanneloffer;
                            myresp.CheapestOfferString = String.Format(
                                culturede,
                                "{0:0,0.00}",
                                cheapestchanneloffer
                            );

                            //Cheapest offers calculation
                            var cheapestofferobj_ws = RoomCalculationHelper.CalculateCheapestRooms(
                                cheapestofferlist_ws,
                                rooms,
                                "ws"
                            );
                            myresp.CheapestOffer_ws =
                                cheapestofferobj_ws != null ? cheapestofferobj_ws.Price : 0;
                            if (cheapestofferobj_ws != null && cheapestofferobj_ws.Price > 0)
                                myresp.CheapestOfferDetail.Add(cheapestofferobj_ws);

                            var cheapestofferobj_bb = RoomCalculationHelper.CalculateCheapestRooms(
                                cheapestofferlist_bb,
                                rooms,
                                "bb"
                            );
                            myresp.CheapestOffer_bb =
                                cheapestofferobj_bb != null ? cheapestofferobj_bb.Price : 0;
                            if (cheapestofferobj_bb != null && cheapestofferobj_bb.Price > 0)
                                myresp.CheapestOfferDetail.Add(cheapestofferobj_bb);

                            var cheapestofferobj_hb = RoomCalculationHelper.CalculateCheapestRooms(
                                cheapestofferlist_hb,
                                rooms,
                                "hb"
                            );
                            myresp.CheapestOffer_hb =
                                cheapestofferobj_hb != null ? cheapestofferobj_hb.Price : 0;
                            if (cheapestofferobj_hb != null && cheapestofferobj_hb.Price > 0)
                                myresp.CheapestOfferDetail.Add(cheapestofferobj_hb);

                            var cheapestofferobj_fb = RoomCalculationHelper.CalculateCheapestRooms(
                                cheapestofferlist_fb,
                                rooms,
                                "fb"
                            );
                            myresp.CheapestOffer_fb =
                                cheapestofferobj_fb != null ? cheapestofferobj_fb.Price : 0;
                            if (cheapestofferobj_fb != null && cheapestofferobj_fb.Price > 0)
                                myresp.CheapestOfferDetail.Add(cheapestofferobj_fb);

                            var cheapestofferobj_ai = RoomCalculationHelper.CalculateCheapestRooms(
                                cheapestofferlist_ai,
                                rooms,
                                "ai"
                            );
                            myresp.CheapestOffer_ai =
                                cheapestofferobj_ai != null ? cheapestofferobj_ai.Price : 0;
                            if (cheapestofferobj_ai != null && cheapestofferobj_ai.Price > 0)
                                myresp.CheapestOfferDetail.Add(cheapestofferobj_ai);

                            //Neu Cheapest Offer General
                            if (myresp.CheapestOffer == 0)
                            {
                                List<double> cheapestoffertotal = new List<double>();
                                if (myresp.CheapestOffer_ai > 0)
                                    cheapestoffertotal.Add((double)myresp.CheapestOffer_ai);
                                if (myresp.CheapestOffer_bb > 0)
                                    cheapestoffertotal.Add((double)myresp.CheapestOffer_bb);
                                if (myresp.CheapestOffer_fb > 0)
                                    cheapestoffertotal.Add((double)myresp.CheapestOffer_fb);
                                if (myresp.CheapestOffer_hb > 0)
                                    cheapestoffertotal.Add((double)myresp.CheapestOffer_hb);
                                if (myresp.CheapestOffer_ws > 0)
                                    cheapestoffertotal.Add((double)myresp.CheapestOffer_ws);

                                if (cheapestoffertotal.Count > 0)
                                {
                                    var cheapestofferdouble = cheapestoffertotal
                                        .OrderBy(x => x)
                                        .FirstOrDefault();
                                    myresp.CheapestOffer = cheapestofferdouble;
                                    myresp.CheapestOfferString = String.Format(
                                        culturede,
                                        "{0:0,0.00}",
                                        cheapestofferdouble
                                    );
                                }
                            }

                            if (
                                myresp.CheapestOfferDetail != null
                                && myresp.CheapestOfferDetail.Count > 0
                            )
                                myparsedresponselist.MssResponseShort.Add(myresp);
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


        public static IEnumerable<MssResponseBaseSearch> ParseBaseSearchResponse(
            XElement mssresponse
            )
        {
            var myresult = mssresponse.Elements("result").Elements("hotel");

            List<MssResponseBaseSearch> basesearchlist = new List<MssResponseBaseSearch>();

            foreach (var result in myresult)
            {
                MssResponseBaseSearch basesearch = new MssResponseBaseSearch();
                basesearch.id = result.Element("id").Value;
                basesearch.id_lts = result.Element("id_lts").Value;
                basesearch.bookable = result.Element("bookable").Value;
                basesearch.currency = result.Element("currency").Value;
                basesearch.name = result.Element("name").Value;
                basesearch.cin = result.Element("cin").Value;
                basesearch.type = result.Element("type").Value;
                basesearch.stars = result.Element("stars").Value;
                basesearch.price_engine = result.Element("price_engine").Value;
                basesearch.price_from = result.Element("price_from").Value;
                basesearch.available_from = result.Element("available_from").Value;
                basesearch.board = result.Element("board").Value;
                basesearch.board_tq = result.Element("board_tq").Value;
                basesearch.bookable_until = result.Element("bookable_until").Value;
                basesearch.prices_changed_at = result.Element("prices_changed_at").Value;
                basesearch.availability_changed_at = result.Element("availability_changed_at").Value;
                basesearch.pers_age_min = result.Element("pers_age_min").Value;
                basesearch.child_age_min = result.Element("child_age_min").Value;
                basesearch.child_age_max = result.Element("child_age_max").Value;
                basesearch.adult_age_min = result.Element("adult_age_min").Value;
                basesearch.adult_cnt_max = result.Element("adult_cnt_max").Value;
                basesearch.child_cnt_max = result.Element("child_cnt_max").Value;
                basesearch.informal_mail = result.Element("informal_mail").Value;

                basesearchlist.Add(basesearch);
            }

            return basesearchlist;
        }
    }
}
