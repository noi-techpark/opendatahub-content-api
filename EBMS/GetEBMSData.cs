// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DataModel;
using Helper;
using Newtonsoft.Json;

namespace EBMS
{
    public class GetEBMSData
    {
        public static async Task<string> GetEBMSEventsFromService(
            string serviceurl,
            string user,
            string pass
        )
        {
            CredentialCache wrCache = new CredentialCache();
            wrCache.Add(new Uri(serviceurl), "Basic", new NetworkCredential(user, pass));

            using (var handler = new HttpClientHandler { Credentials = wrCache })
            {
                using (var client = new HttpClient(handler))
                {
                    var myresponse = await client.GetAsync(serviceurl);

                    var myresponsestring = await myresponse.Content.ReadAsStringAsync();

                    return myresponsestring;
                }
            }
        }

        public static List<Tuple<EventShortLinked, EBMSEventREST>> GetEbmsEvents(
            string serviceurl,
            string user,
            string pass
        )
        {
            List<Tuple<EventShortLinked, EBMSEventREST>> myeventshortlist =
                new List<Tuple<EventShortLinked, EBMSEventREST>>();

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var response = GetEBMSEventsFromService(serviceurl, user, pass).Result;

            if (response == null)
                throw new Exception("No data from Interface");

            var eventarray = JsonConvert.DeserializeObject<List<EBMSEventREST>>(response);

            foreach (var myevent in eventarray)
            {
                var eventtosave = new EventShortLinked();

                //CUSTOM Props

                eventtosave.Id = "eventshort-" + myevent.EventId;
                eventtosave.Source = "ebms";

                //ADD MAPPING
                var ebmsrid = new Dictionary<string, string>()
                {
                    { "id", myevent.EventId.ToString() },
                };
                eventtosave.Mapping.TryAddOrUpdate("ebms", ebmsrid);

                //Interface Props

                eventtosave.EventId = myevent.EventId;
                //Hauptbeschreibung
                eventtosave.EventDescription = myevent.EventDescription ?? "";
                //Beschreibung DE
                if (!String.IsNullOrEmpty(myevent.EventDescriptionAlt1))
                    eventtosave.EventTitle.TryAddOrUpdate("de", myevent.EventDescriptionAlt1);
                //Beschreibung IT
                if (!String.IsNullOrEmpty(myevent.EventDescriptionAlt2))
                    eventtosave.EventTitle.TryAddOrUpdate("it", myevent.EventDescriptionAlt2);
                //Beschreibung EN
                if (!String.IsNullOrEmpty(myevent.EventDescriptionAlt3))
                    eventtosave.EventTitle.TryAddOrUpdate("en", myevent.EventDescriptionAlt3);

                //Sonderfall
                if (
                    !String.IsNullOrEmpty(myevent.EventDescription)
                    && !eventtosave.EventTitle.ContainsKey("de")
                )
                {
                    eventtosave.EventTitle.TryAddOrUpdate("de", myevent.EventDescription);
                }
                //Sonderfall
                if (
                    !String.IsNullOrEmpty(myevent.EventDescription)
                    && !eventtosave.EventTitle.ContainsKey("en")
                )
                {
                    eventtosave.EventTitle.TryAddOrUpdate("en", myevent.EventDescription);
                }

                ////Beschreibung DE
                //eventtosave.EventDescriptionDE = myevent.EventDescriptionAlt1 ?? "";
                ////Beschreibung IT
                //eventtosave.EventDescriptionIT = myevent.EventDescriptionAlt2 ?? "";
                ////Beschreibung EN
                //eventtosave.EventDescriptionEN = myevent.EventDescriptionAlt3 ?? "";


                eventtosave.Shortname = eventtosave.EventDescription;

                //Hauptsaal/ort
                eventtosave.AnchorVenue = myevent.AnchorVenue;
                //Hauptsaal/ort soll für die Ausgabe verwendet werden
                eventtosave.AnchorVenueShort = myevent.AnchorVenueShort ?? "";
                //letzte Änderung
                eventtosave.ChangedOn = myevent.ChangedOn;
                eventtosave.LastChange = myevent.ChangedOn;
                //Beginndatum
                eventtosave.StartDate = new DateTime(
                    myevent.StartDate.Year,
                    myevent.StartDate.Month,
                    myevent.StartDate.Day,
                    myevent.StartTime.Hour,
                    myevent.StartTime.Minute,
                    myevent.StartTime.Second
                );
                //Beginnzeit
                //eventtosave.StartTime = myevent.StartTime;
                //Ende Datum
                eventtosave.EndDate = new DateTime(
                    myevent.EndDate.Year,
                    myevent.EndDate.Month,
                    myevent.EndDate.Day,
                    myevent.EndTime.Hour,
                    myevent.EndTime.Minute,
                    myevent.EndTime.Second
                );
                //Endzeit

                //UTC
                //Int32 unixTimestampEStart = (Int32)(DateTime.UtcNow.Subtract(eventtosave.StartDate)).TotalSeconds;
                //Int32 unixTimestampEEnd = (Int32)(DateTime.UtcNow.Subtract()).TotalSeconds;
                eventtosave.EndDateUTC = Helper.DateTimeHelper.DateTimeToUnixTimestampMilliseconds(
                    eventtosave.EndDate
                );
                eventtosave.StartDateUTC =
                    Helper.DateTimeHelper.DateTimeToUnixTimestampMilliseconds(
                        eventtosave.StartDate
                    );

                //eventtosave.EndTime = myevent.EndTime;
                //URL für externe Webseite (noch nicht ausgefüllt)
                eventtosave.WebAddress = myevent.WebAddress ?? "";
                //Spezialfelder

                //Videowall Reception EURAC with default “yes” 
                eventtosave.Display1 = myevent.Display1;
                //Videowall in front of rooms EURAC with default “yes” 
                eventtosave.Display2 = myevent.Display2;
                //NOI Videowall with default “no” 
                eventtosave.Display3 = myevent.Display3;
                //NOI Totem in front of rooms with default “no”
                eventtosave.Display4 = myevent.Display4;

                //not used yet
                eventtosave.Display5 = myevent.Display5;
                eventtosave.Display6 = myevent.Display6;
                eventtosave.Display7 = myevent.Display7;
                eventtosave.Display8 = myevent.Display8;
                eventtosave.Display9 = myevent.Display9;
                
                eventtosave.Active = true;

                if (myevent.Company != null)
                {
                    var myeventcompany = myevent.Company.FirstOrDefault();

                    //CRM Modul Account (Firma) interessiert uns nicht
                    eventtosave.CompanyName = myeventcompany.Name ?? "";
                    eventtosave.CompanyId = myeventcompany.Code ?? "";
                    eventtosave.CompanyAddressLine1 = myeventcompany.AddressLine1 ?? "";
                    eventtosave.CompanyAddressLine2 = myeventcompany.AddressLine2 ?? "";
                    eventtosave.CompanyAddressLine3 = myeventcompany.AddressLine3 ?? "";
                    eventtosave.CompanyPostalCode = myeventcompany.PostalCode ?? "";
                    eventtosave.CompanyCity = myeventcompany.City ?? "";
                    eventtosave.CompanyCountry = myeventcompany.Country ?? "";
                    eventtosave.CompanyPhone = myeventcompany.Phone ?? "";
                    eventtosave.CompanyFax = myeventcompany.Fax ?? "";
                    eventtosave.CompanyMail = myeventcompany.Mail ?? "";
                    eventtosave.CompanyUrl = myeventcompany.Url ?? "";
                }
                else
                {
                    eventtosave.CompanyName = "";
                    eventtosave.CompanyId = "";
                    eventtosave.CompanyAddressLine1 = "";
                    eventtosave.CompanyAddressLine2 = "";
                    eventtosave.CompanyAddressLine3 = "";
                    eventtosave.CompanyPostalCode = "";
                    eventtosave.CompanyCity = "";
                    eventtosave.CompanyCountry = "";
                    eventtosave.CompanyPhone = "";
                    eventtosave.CompanyFax = "";
                    eventtosave.CompanyMail = "";
                    eventtosave.CompanyUrl = "";
                }

                if (myevent.Contact != null)
                {
                    var myeventcontact = myevent.Contact.FirstOrDefault();

                    //Person aus Modul CRM (interessiert uns nicht)
                    eventtosave.ContactCode = myeventcontact.Code ?? "";
                    eventtosave.ContactFirstName = myeventcontact.FirstName ?? "";
                    eventtosave.ContactLastName = myeventcontact.LastName ?? "";
                    eventtosave.ContactPhone = myeventcontact.Phone ?? "";
                    eventtosave.ContactCell = myeventcontact.Cell ?? "";
                    eventtosave.ContactFax = myeventcontact.Fax ?? "";
                    eventtosave.ContactEmail = myeventcontact.Email ?? "";
                    eventtosave.ContactAddressLine1 = myeventcontact.AddressLine1 ?? "";
                    eventtosave.ContactAddressLine2 = myeventcontact.AddressLine2 ?? "";
                    eventtosave.ContactAddressLine3 = myeventcontact.AddressLine3 ?? "";
                    eventtosave.ContactPostalCode = myeventcontact.PostalCode ?? "";
                    eventtosave.ContactCity = myeventcontact.City ?? "";
                    eventtosave.ContactCountry = myeventcontact.Country ?? "";
                }
                else
                {
                    //To be compatible
                    eventtosave.ContactCode = "";
                    eventtosave.ContactFirstName = "";
                    eventtosave.ContactLastName = "";
                    eventtosave.ContactPhone = "";
                    eventtosave.ContactCell = "";
                    eventtosave.ContactFax = "";
                    eventtosave.ContactEmail = "";
                    eventtosave.ContactAddressLine1 = "";
                    eventtosave.ContactAddressLine2 = "";
                    eventtosave.ContactAddressLine3 = "";
                    eventtosave.ContactPostalCode = "";
                    eventtosave.ContactCity = "";
                    eventtosave.ContactCountry = "";
                }

                //gebuchten Sääle von spezifischen Event
                //Space : Code für Raum von DB
                //SpaceDesc: Beschreibung --> zu nehmen
                //SpaceAbbrev: Abgekürzte Beschreibung
                //SoaceType : EC = Eurac, NO = Noi
                //Comnment: entweder x oder leer --> x bedeutet bitte nicht anzeigen!!!!!!!
                //Subtitle: Untertitel vom Saal (anzeigen)
                //Zeiten (diese sind relevant, diese anzeigen)

                List<RoomBooked> mybookingroomlist = new List<RoomBooked>();

                string eventlocation = "";

                if (myevent.Bookings != null)
                {
                    foreach (var bookingroom in myevent.Bookings)
                    {
                        RoomBooked myroom = new RoomBooked();
                        myroom.Comment = bookingroom.Comment ?? "";
                        myroom.EndDate = new DateTime(
                            bookingroom.EndDate.Year,
                            bookingroom.EndDate.Month,
                            bookingroom.EndDate.Day,
                            bookingroom.EndTime.Hour,
                            bookingroom.EndTime.Minute,
                            bookingroom.EndTime.Second
                        );
                        myroom.StartDate = new DateTime(
                            bookingroom.StartDate.Year,
                            bookingroom.StartDate.Month,
                            bookingroom.StartDate.Day,
                            bookingroom.StartTime.Hour,
                            bookingroom.StartTime.Minute,
                            bookingroom.StartTime.Second
                        );

                        //Int32 unixTimestampStart = (Int32)(DateTime.UtcNow.Subtract(myroom.StartDate)).TotalSeconds;
                        //Int32 unixTimestampEnd = (Int32)(DateTime.UtcNow.Subtract(myroom.EndDate)).TotalSeconds;

                        myroom.EndDateUTC =
                            Helper.DateTimeHelper.DateTimeToUnixTimestampMilliseconds(
                                myroom.EndDate
                            );
                        myroom.StartDateUTC =
                            Helper.DateTimeHelper.DateTimeToUnixTimestampMilliseconds(
                                myroom.StartDate
                            );

                        myroom.Space = bookingroom.Space ?? "";
                        myroom.SpaceAbbrev = bookingroom.SpaceAbbrev ?? "";
                        myroom.SpaceDesc = bookingroom.SpaceDesc ?? "";
                        myroom.SpaceType = bookingroom.SpaceType ?? "";
                        myroom.Subtitle = bookingroom.Subtitle ?? "";

                        if (bookingroom.SpaceType == "EC")
                            eventlocation = "EC";
                        else if (bookingroom.SpaceType == "NO")
                            eventlocation = "NOI";

                        mybookingroomlist.Add(myroom);
                    }

                    eventtosave.RoomBooked = mybookingroomlist;

                    eventtosave.EventLocation = eventlocation;

                    //Remove this special rule not needed anymore
                    //if (eventlocation == "NOI")
                    //    eventtosave.Display1 = eventtosave.Display4;
                    
                    myeventshortlist.Add(Tuple.Create(eventtosave, myevent));
                }
            }

            return myeventshortlist;
        }
    }
}
