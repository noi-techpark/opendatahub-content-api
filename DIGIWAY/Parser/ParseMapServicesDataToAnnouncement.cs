// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using DIGIWAY.Model.GeoJsonReadModel;

namespace DIGIWAY
{
    public class ParseMapServicesDataToAnnouncement
    {
        public static IEnumerable<Announcement> ParseList(IEnumerable<GeoJsonFeatureMapServices> objectlist)
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

        public static Announcement Parse(GeoJsonFeatureMapServices data)
        {
            if (data == null)
                return null;

            Announcement announcement = new Announcement();

            //Mapping Object
            announcement.Mapping = new Dictionary<string, IDictionary<string,string>>();
            var mapping = new Dictionary<string, string>();
            mapping.Add("id", data.id);
            //if (String.IsNullOrEmpty(data.Note))
            //    mapping.Add("note", data.Note);
            //if (String.IsNullOrEmpty(data.Numero_sentiero))
            //    mapping.Add("numero_sentiero", data.Numero_sentiero);
            //if (String.IsNullOrEmpty(data.Denominazione_sentiero))
            //    mapping.Add("denominazione_sentiero", data.Denominazione_sentiero);
            //if (String.IsNullOrEmpty(data.Posizione.display_value))
            //    mapping.Add("display_value", data.Posizione.display_value);
            //if (String.IsNullOrEmpty(data.Posizione.country))
            //    mapping.Add("country", data.Posizione.country);
            //if (String.IsNullOrEmpty(data.Posizione.district_city))
            //    mapping.Add("district_city", data.Posizione.district_city);
            //if (String.IsNullOrEmpty(data.Stato))
            //    mapping.Add("stato_stato.stato", data.Stato);

            //announcement.Mapping.TryAddOrUpdate("zoho", mapping);

            ////TO Check add ContactInfos?

            //announcement.Id = "urn:announcements:zoho:" + data.ID;
            //announcement.Source = "digiway.zoho";            

            //announcement.Active = true;
            //announcement.Detail = new Dictionary<string, DetailGeneric>();

            //DetailGeneric detail = new DetailGeneric() { Language = "it", Title = data.Codice_sentiero, BaseText = $"Stato: {data.Stato}"};

            //announcement.Detail.TryAddOrUpdate("it", detail);

            //announcement.Shortname = data.Codice_sentiero;

            //announcement.Geo = new Dictionary<string, GpsInfo>()
            //{
            //    { "position", new GpsInfo() { Latitude = double.Parse(data.Posizione.latitude, CultureInfo.InvariantCulture), Longitude = double.Parse(data.Posizione.longitude, CultureInfo.InvariantCulture), Default = true, Gpstype = "position", Geometry = $"POINT ({data.Posizione.longitude} {data.Posizione.latitude})"  } }
            //};

            //announcement.HasLanguage = new List<string>() { "it" };

            announcement._Meta = new Metadata() { Id = announcement.Id, Source = announcement.Source, Reduced = false, Type = "announcement" };

            return announcement;
        }
    }
}
