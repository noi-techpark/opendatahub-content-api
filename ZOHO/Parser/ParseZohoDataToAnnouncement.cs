using DataModel;
using Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZOHO
{
    public class ParseZohoDataToAnnouncement
    {
        public static IEnumerable<Announcement> ParseList(IEnumerable<ZohoRootobject> zohoobjectlist)
        {
            List<Announcement> announcementlist = new List<Announcement>();

            foreach(var zohoobject in zohoobjectlist)
            {
                var announcement = Parse(zohoobject);

                if(announcement != null)
                    announcementlist.Add(announcement);
            }

            return announcementlist;
        }

        public static Announcement Parse(ZohoRootobject zohoobject)
        {
            if (zohoobject == null)
                return null;

            Announcement announcement = new Announcement();

            //Mapping Object
            announcement.Mapping = new Dictionary<string, IDictionary<string,string>>();
            var zohomapping = new Dictionary<string, string>();
            zohomapping.Add("id", zohoobject.ID);
            if (String.IsNullOrEmpty(zohoobject.Note))
                zohomapping.Add("note", zohoobject.Note);
            if (String.IsNullOrEmpty(zohoobject.Numero_sentiero))
                zohomapping.Add("numero_sentiero", zohoobject.Numero_sentiero);
            if (String.IsNullOrEmpty(zohoobject.Denominazione_sentiero))
                zohomapping.Add("denominazione_sentiero", zohoobject.Denominazione_sentiero);
            if (String.IsNullOrEmpty(zohoobject.Posizione.display_value))
                zohomapping.Add("display_value", zohoobject.Posizione.display_value);
            if (String.IsNullOrEmpty(zohoobject.Posizione.country))
                zohomapping.Add("country", zohoobject.Posizione.country);
            if (String.IsNullOrEmpty(zohoobject.Posizione.district_city))
                zohomapping.Add("district_city", zohoobject.Posizione.district_city);
            if (String.IsNullOrEmpty(zohoobject.Stato_StatoStato))
                zohomapping.Add("stato_stato.stato", zohoobject.Stato_StatoStato);

            announcement.Mapping.TryAddOrUpdate("zoho", zohomapping);

            //TO Check add ContactInfos?

            announcement.Id = "urn:announcements:zoho:" + zohoobject.ID;
            announcement.Source = "digiway.zoho";            

            announcement.Active = true;
            announcement.Detail = new Dictionary<string, DetailGeneric>();

            DetailGeneric detail = new DetailGeneric() { Language = "it", Title = zohoobject.Codice_sentiero, BaseText = $"Stato: {zohoobject.Stato_StatoStato}"};

            announcement.Detail.TryAddOrUpdate("it", detail);

            announcement.Shortname = zohoobject.Codice_sentiero;

            announcement.Geo = new Dictionary<string, GpsInfo>()
            {
                { "position", new GpsInfo() { Latitude = double.Parse(zohoobject.Posizione.latitude, CultureInfo.InvariantCulture), Longitude = double.Parse(zohoobject.Posizione.longitude, CultureInfo.InvariantCulture), Default = true, Gpstype = "position", Geometry = $"POINT ({zohoobject.Posizione.longitude} {zohoobject.Posizione.latitude})"  } }
            };

            announcement.HasLanguage = new List<string>() { "it" };

            return announcement;
        }
    }
}
