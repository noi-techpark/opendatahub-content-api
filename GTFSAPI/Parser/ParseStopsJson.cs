// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFSAPI
{
    public class ParseGtfsApi
    {
        public static List<StaTimeTableStopsCsv> GetParsetStaTimeTableStops(Stream stream)
        {
            List<StaTimeTableStopsCsv> result = new List<StaTimeTableStopsCsv>();

            StreamReader sr = new StreamReader(stream);
            char[] separator = { ',' };
            string[] read;

            if (sr != null)
            {
                string data = sr.ReadLine();
                //Read first line
                string headerLine = sr.ReadLine();

                while ((data = sr.ReadLine()) != null)
                {                    
                    read = data.Split(separator, StringSplitOptions.None);

                    StaTimeTableStopsCsv parsedstop = new StaTimeTableStopsCsv();

                    parsedstop.stop_id = read[0];
                    parsedstop.stop_name = read[1];
                    parsedstop.stop_lat = float.Parse(read[2]);
                    parsedstop.stop_lon = float.Parse(read[3]);
                    parsedstop.location_type = read[4];
                    parsedstop.parent_station = read[5];

                    result.Add(parsedstop);                    
                }
            }

            return result;
        }

        public static ODHActivityPoiLinked ParseStaTimeTableStopsToODHActivityPoi(ODHActivityPoiLinked? parsedobject, StaTimeTableStopsCsv statimetablestops)
        {

            //Parse and add to list 
            if(parsedobject == null)
                parsedobject = new ODHActivityPoiLinked();

            parsedobject.Id = statimetablestops.stop_id;

            return parsedobject;
        }

    }

    public class StaTimeTableStopsCsv
    {
        public string stop_id { get; set; }
        public string stop_name { get; set; }
        public double stop_lat { get; set; }
        public double stop_lon { get; set; }        
        public string location_type { get; set; }
        public string parent_station { get; set; }

    }
}
