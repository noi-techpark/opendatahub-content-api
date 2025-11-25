// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;

namespace HDS
{
    public class HDSMarket
    {
        [Index(0)]
        [Name("Gemeinde - Comune")]
        public string? Municipality { get; set; }

        [Index(1)]
        [Name("Wochentag - Giorno della settimana")]
        public string? Weekday { get; set; }

        [Index(2)]
        [Name("Frequenz - Cadenza")]
        public string? Frequency { get; set; }

        [Index(3)]
        [Name("Standplätze - Numero banchi")]
        public string? Standsnumber { get; set; }

        [Index(4)]
        [Name("Bezirk - Comunità di Valle")]
        public string? Area { get; set; }

        [Index(5)]
        [Name("Foto")]
        public string? Foto { get; set; }

        [Index(6)]
        [Name("Geoloc")]
        public string? Geoloc { get; set; }

        [Index(7)]
        [Name("Ganzjährig/sainsonal - Annuale/stagionale")]
        public string? Seasonality { get; set; }
     
    }

    public class HDSYearMarket
    {
        [Index(0)]
        [Name("Monat -Mese")]
        public string? Month { get; set; }

        [Index(1)]
        [Name("Giorno")]
        public string? Weekday { get; set; }

        [Index(2)]
        [Name("Data")]
        public string? DateBegin { get; set; }

        [Index(3)]
        [Name("Gemeinde - Comune")]
        public string? Municipality { get; set; }

        [Index(4)]
        [Name("MONATSMARKT - MENSILE")]
        public string? Modality { get; set; }

        [Index(5)]
        [Name("Link")]
        public string? Foto { get; set; }

        [Index(6)]
        [Name("Bezirksgemeinschaft")]
        public string? OrganizatedfromCommunity { get; set; }

        [Index(7)]
        [Name("Geolocation")]
        public string? Geoloc { get; set; }
    }
}
