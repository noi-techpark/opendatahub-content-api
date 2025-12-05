// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;

namespace DataModel
{
    public class MssResult
    {
        public string? ResultId { get; set; }
        public string? CheapestChannel { get; set; }
        public double? Cheapestprice { get; set; }
        public int? bookableHotels { get; set; }
        public virtual ICollection<MssResponseShort> MssResponseShort { get; set; } =
            new HashSet<MssResponseShort>();
    }

    public class MssResponseShort
    {
        public MssResponseShort()
        {
            this.RoomDetails = new HashSet<RoomDetails>();
            this.CheapestOfferDetail = new List<CheapestRoomCombination>();
        }

        public int HotelId { get; set; }
        public string? A0RID { get; set; }
        public bool Bookable { get; set; }
        public string? ChannelID { get; set; }
        public string? Channellink { get; set; }
        public string? OfferId { get; set; }
        public string? OfferGid { get; set; }
        public int? OfferTyp { get; set; }
        public int? OfferShow { get; set; }
        public double CheapestOffer { get; set; }
        public string? CheapestOfferString { get; set; }

        //neu zusatz
        public string? OnlinepaymentMethods { get; set; }
        public string? OnlinepaymentPrepayment { get; set; }
        public string? OnlinepaymentCCards { get; set; }

        //magari die Cheapestoffers pro Service ospeichern??
        public double? CheapestOffer_ws { get; set; }
        public double? CheapestOffer_bb { get; set; }
        public double? CheapestOffer_hb { get; set; }
        public double? CheapestOffer_fb { get; set; }
        public double? CheapestOffer_ai { get; set; }

        public virtual ICollection<RoomDetails> RoomDetails { get; set; } = new List<RoomDetails>();

        public virtual ICollection<CheapestRoomCombination> CheapestOfferDetail { get; set; }
    }

    public class RoomDetails
    {
        public string? RoomId { get; set; }
        public int? RoomSeq { get; set; }
        public double TotalPrice { get; set; }
        public string? OfferId { get; set; }

        public double? Price_ws { get; set; }
        public double? Price_bb { get; set; }
        public double? Price_hb { get; set; }
        public double? Price_fb { get; set; }
        public double? Price_ai { get; set; }

        public int? Roomtype { get; set; }
        public int? Roomfree { get; set; }

        //Zusatz RoomMax
        public int? Roommax { get; set; }

        //Zusatz RoomMin
        public int? Roommin { get; set; }

        //Zusatz RoomStd
        public int? Roomstd { get; set; }

        public string? Roomtitle { get; set; }
        public string? Roomdesc { get; set; }
        public string? RoomChannelLink { get; set; }

        //public string Service { get; set; }

        public string? TotalPriceString { get; set; }

        //public virtual ICollection<RoomPictures> RoomPictures => new List<RoomPictures>();
        public virtual ICollection<RoomPictures> RoomPictures { get; set; }

        ///NEU MSS Umstellung FELDER

        public PaymentTerm? PaymentTerm { get; set; }
        public CancelPolicy? CancelPolicy { get; set; }
    }

    public class RoomPictures
    {
        public string? Pictureurl { get; set; }
    }

    public class PaymentTerm
    {
        public string? Id { get; set; }
        public int Methods { get; set; }
        public int Prepayment { get; set; }
        public int Ccards { get; set; }
        public int Priority { get; set; }
        public string? Description { get; set; }

        public Bank? Bank { get; set; }
    }

    public class Bank
    {
        public string? Name { get; set; }
        public string? Iban { get; set; }
        public string? Swift { get; set; }
    }

    public class CancelPolicy
    {
        public string? Id { get; set; }
        public int? Refundable { get; set; }
        public DateTime? RefundableUntil { get; set; }
        public string? Description { get; set; }

        public List<Penalty> Penalties { get; set; } //=> new List<Penalty>();
    }

    public class Penalty
    {
        public int? Percent { get; set; }
        public DateTime? Datefrom { get; set; }
        public int? Daysarrival { get; set; }
    }

    public class CheapestOffer
    {
        public string RoomId { get; set; }
        public int RoomSeq { get; set; }
        public double Price { get; set; }

        public int? RoomFree { get; set; }
    }

    public class CheapestRoomCombination
    {
        public CheapestRoomCombination()
        {
            CheapestRoomCombinationDetail = new List<CheapestOffer>();
        }

        public IList<CheapestOffer> CheapestRoomCombinationDetail { get; set; }

        public string Service { get; set; }
        public double Price
        {
            get
            {
                return
                    this.CheapestRoomCombinationDetail != null
                    && this.CheapestRoomCombinationDetail.Count > 0
                    ? this.CheapestRoomCombinationDetail.Sum(x => x.Price)
                    : 0;
            }
        }
    }

    //TODO
    public class MssResponseComplete { }


    public class MssResponseBaseSearch
    {
        public string id { get; set; }
        public string id_lts { get; set; }
        public string bookable { get; set; }
        public string currency { get; set; }
        public string name { get; set; }
        public string cin { get; set; }
        public string type { get; set; }
        public string stars { get; set; }
        public string price_engine { get; set; }
        public string price_from { get; set; }
        public string available_from { get; set; }
        public string board { get; set; }
        public string board_tq { get; set; }
        public string bookable_until { get; set; }
        public string prices_changed_at { get; set; }
        public string availability_changed_at { get; set; }
        public string pers_age_min { get; set; }
        public string child_age_min { get; set; }
        public string child_age_max { get; set; }
        public string adult_age_min { get; set; }
        public string adult_cnt_max { get; set; }
        public string child_cnt_max { get; set; }
        public string informal_mail { get; set; }
    }
}
