// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFSAPI
{
    public class StopsCsv
    {
        public string stop_id { get; set; }
        public string stop_name { get; set; }
        public double stop_lat { get; set; }
        public double stop_lon { get; set; }
        public string location_type { get; set; }
        public string parent_station { get; set; }

    }

    public class TripsCsv
    {
        //To define
        //public string stop_id { get; set; }
        //public string stop_name { get; set; }
        //public double stop_lat { get; set; }
        //public double stop_lon { get; set; }
        //public string location_type { get; set; }
        //public string parent_station { get; set; }

    }

    public class CalendarCsv
    {
        //To define
        //public string stop_id { get; set; }
        //public string stop_name { get; set; }
        //public double stop_lat { get; set; }
        //public double stop_lon { get; set; }
        //public string location_type { get; set; }
        //public string parent_station { get; set; }

    }
}
