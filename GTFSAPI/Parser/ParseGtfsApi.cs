// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace GTFSAPI
{
    public class ParseGtfsApi
    {
        public static List<StopsCsv> GetParsetStaTimeTableStops(Stream stream)
        {
            List<StopsCsv> result = new List<StopsCsv>();
            CultureInfo enculture = new CultureInfo("en");
            string[] read;

            using (TextFieldParser textfieldparser = new TextFieldParser(stream))
            {
                textfieldparser.HasFieldsEnclosedInQuotes = true;
                textfieldparser.SetDelimiters(",");

                var headline = textfieldparser.ReadFields();

                while (!textfieldparser.EndOfData)
                {
                    read = textfieldparser.ReadFields();

                    if (read != null)
                    {
                        StopsCsv parsedstop = new StopsCsv();

                        parsedstop.stop_id = read[0];
                        parsedstop.stop_name = read[1];
                        parsedstop.stop_lat = float.Parse(read[2], enculture);
                        parsedstop.stop_lon = float.Parse(read[3], enculture);
                        parsedstop.location_type = read[4];
                        parsedstop.parent_station = read[5];

                        result.Add(parsedstop);
                    }
                }
            }

            //StreamReader sr = new StreamReader(stream);
            //char[] separator = { ',' };
            //Regex csvParserRegex = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");


            //if (sr != null)
            //{                
            //    //Read first line
            //    string data = sr.ReadLine();

            //    while ((data = sr.ReadLine()) != null)
            //    {
            //        read = csvParserRegex.Split(data);

            //        //read = data.Split(separator, StringSplitOptions.None);

            //        StaTimeTableStopsCsv parsedstop = new StaTimeTableStopsCsv();

            //        parsedstop.stop_id = read[0];
            //        parsedstop.stop_name = read[1];
            //        parsedstop.stop_lat = float.Parse(read[2]);
            //        parsedstop.stop_lon = float.Parse(read[3]);
            //        parsedstop.location_type = read[4];
            //        parsedstop.parent_station = read[5];

            //        result.Add(parsedstop);                    
            //    }
            //}

            return result;
        }

        public static ODHActivityPoiLinked ParseStaTimeTableStopsToODHActivityPoi(ODHActivityPoiLinked? parsedobject, StopsCsv statimetablestops)
        {

            //Parse and add to list 
            if(parsedobject == null)
                parsedobject = new ODHActivityPoiLinked();

            parsedobject.Id = statimetablestops.stop_id;
            Dictionary<string, string> mapping = new Dictionary<string, string>();
            mapping.TryAddOrUpdate("stop_id", statimetablestops.stop_id);
            if (!String.IsNullOrEmpty(statimetablestops.parent_station.Trim()))
                mapping.TryAddOrUpdate("parent_station", statimetablestops.parent_station);
            if(!String.IsNullOrEmpty(statimetablestops.location_type))
                mapping.TryAddOrUpdate("location_type", statimetablestops.location_type);             

            parsedobject.Mapping.TryAddOrUpdate("sta", mapping);

            //GPS
            if (parsedobject.GpsInfo == null)
                parsedobject.GpsInfo = new List<GpsInfo>();

            parsedobject.GpsInfo.Clear();

            parsedobject.GpsInfo.Add(new GpsInfo() { Gpstype = "position", Altitude = null, AltitudeUnitofMeasure = null, Latitude = statimetablestops.stop_lat, Longitude = statimetablestops.stop_lon });

            parsedobject.Shortname = statimetablestops.stop_name;
            //Detail object
            if(parsedobject.Detail == null)
                parsedobject.Detail = new Dictionary<string, Detail>();
            parsedobject.Detail.TryAddOrUpdate("it", new Detail() { Language = "it", Title = statimetablestops.stop_name });

            parsedobject.HasLanguage = new List<string>() { "it" };

            parsedobject.Source = "sta";
            parsedobject.SyncSourceInterface = "gtfsapi";
            parsedobject.Active = true;

            return parsedobject;
        }

    }

    
}
