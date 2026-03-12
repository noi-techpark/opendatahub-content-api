// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Amazon.Runtime.Internal.Transform;
using DataModel;
using DIGIWAY.Model.GeoJsonReadModel;
using Helper;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DIGIWAY
{
    public class ParseMapServicesDataToAnnouncement
    {
        public static IEnumerable<Announcement> ParseList(ICollection<GeoJsonFeature> objectlist)
        {
            List<Announcement> announcementlist = new List<Announcement>();

            foreach(var data in objectlist)
            {
                var announcement = Parse(data);

                if(announcement != null)
                    announcementlist.Add(announcement);
            }

            return announcementlist;
        }

        public static Announcement Parse(GeoJsonFeature data)
        {
            if (data == null)
                return null;

            Announcement announcement = new Announcement();

            //Mapping Object
            announcement.Mapping = new Dictionary<string, IDictionary<string,string>>();
            var mapping = new Dictionary<string, string>();
            
            //Go trough all Attributes ensure id is on first place
            foreach(var value in data.Attributes.OrderBy(a => a.Key == "id" ? 0 : 1)) 
            {
                if (value.Value != null)
                {
                    //Check if data is nested and create .
                    if (value.Value is NetTopologySuite.Features.AttributesTable nestedTable)
                    {
                        foreach (var nestedKey in nestedTable.GetNames())
                        {
                            var nestedValue = nestedTable[nestedKey];
                            if (nestedValue != null)
                            {
                                mapping.Add($"{value.Key}.{nestedKey}", nestedValue.ToString());
                            }
                        }
                    }
                    else
                    {
                        mapping.Add(value.Key, value.Value.ToString());
                    }
                        
                }
            }
          
            announcement.Mapping.TryAddOrUpdate("tirol.mapservices.eu", mapping);

            ////TO Check add ContactInfos?

            announcement.Id = "urn:announcements:tirol.mapservices.eu:" + data.Attributes["id"].ToString();
            announcement.Source = "tirol.mapservices.eu";            

            announcement.Active = true;
            announcement.Shortname = data.Attributes["name"].ToString();

            announcement.StartTime = data.Attributes.ContainsKey("startDate") && data.Attributes["startDate"] != null ? Convert.ToDateTime(data.Attributes["startDate"].ToString()) : null;
             
            announcement.EndTime = data.Attributes.ContainsKey("endDate") && data.Attributes["endDate"]!= null ? Convert.ToDateTime(data.Attributes["endDate"].ToString()) : null;

            announcement.Detail = new Dictionary<string, DetailGeneric>();

            DetailGeneric detail = new DetailGeneric() { 
                Language = "de", 
                Title = data.Attributes["name"].ToString(), 
                BaseText = data.Attributes["description"].ToString() + data.Attributes["diversionDescription"] != null ? " " + data.Attributes["diversionDescription"].ToString() : "" };

            announcement.Detail.TryAddOrUpdate("de", detail);

            announcement.TagIds = new List<string>();
            announcement.TagIds.Add("announcement:trail-closure");

            Dictionary<string, GpsInfo> gpsinfolist = new Dictionary<string, GpsInfo>();

            if (data.Geometry != null && data.Geometry.Length > 0)
            {              
                gpsinfolist.TryAddOrUpdate("track", new GpsInfo()
                {
                    Default = true,
                    Geometry = data.Geometry.AsText()
                });

                //IS this needed?

                //get first point of geometry
                var point = data.Geometry.Coordinates.FirstOrDefault();
                if (point != null)
                {
                    gpsinfolist.TryAddOrUpdate("position", new GpsInfo()
                    {
                        Default = false,
                        Altitude = null,
                        AltitudeUnitofMeasure = "m",
                        Gpstype = "position",
                        //Use only first digits otherwise point and track will differ
                        Latitude = point.Y,
                        Longitude = point.X
                    });
                }
            }

            announcement.Geo = gpsinfolist;

            announcement.HasLanguage = new List<string>() { "de" };

            announcement._Meta = new Metadata() { Id = announcement.Id, Source = announcement.Source, Reduced = false, Type = "announcement" };

            return announcement;
        }
    }
}
