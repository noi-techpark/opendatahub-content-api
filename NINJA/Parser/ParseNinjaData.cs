// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Amazon.Auth.AccessControlPolicy;
using DataModel;
using Helper;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.FSharp.Control;
using Newtonsoft.Json.Linq;
using static Dapper.SqlMapper;

namespace NINJA.Parser
{
    public class ParseNinjaData
    {
        #region EventV1Parsing

        //TODO Redefine Mapping ODH - Centro Trevi / Drin
        public static List<TopicLinked> GetTopicRid(string ninjaeventtype)
        {
            TopicLinked topic = new TopicLinked();

            switch (ninjaeventtype)
            {
                case "Convegni/conferenze":
                    topic = new TopicLinked
                    {
                        TopicRID = "0D25868CC23242D6AC97AEB2973CB3D6",
                        TopicInfo = "Tagungen/Vorträge",
                    };
                    break;
                case "Sport":
                    topic = new TopicLinked
                    {
                        TopicRID = "162C0067811B477DA725D2F5F2D98398",
                        TopicInfo = "Sport",
                    };
                    break;
                case "Enogastronomia/prodotti":
                    topic = new TopicLinked
                    {
                        TopicRID = "252200A028C8449D9A6205369A6D0D36",
                        TopicInfo = "Gastronomie/Typische Produkte",
                    };
                    break;
                case "Artigianato/tradizioni":
                    topic = new TopicLinked
                    {
                        TopicRID = "33BDC54BD39946F4852B3394B00610AE",
                        TopicInfo = "Handwerk/Brauchtum",
                    };
                    break;
                case "Fiere/mercati":
                    topic = new TopicLinked
                    {
                        TopicRID = "4C4961D9FC5B48EEB73067BEB9D4402A",
                        TopicInfo = "Messen/Märkte",
                    };
                    break;
                case "Teatro/cinema":
                    topic = new TopicLinked
                    {
                        TopicRID = "6884FE362C88434B9F49725E3328112B",
                        TopicInfo = "Theater/Vorführungen",
                    };
                    break;
                case "Corsi/lezioni":
                    topic = new TopicLinked
                    {
                        TopicRID = "767F6F43FC394CE9A3C8A9725C6FF134",
                        TopicInfo = "Kurse/Bildung",
                    };
                    break;
                case "Musica/danza":
                    topic = new TopicLinked
                    {
                        TopicRID = "7E048074BA004EC58E29E330A9AA476B",
                        TopicInfo = "Musik/Tanz",
                    };
                    break;
                case "Sagre/feste":
                    topic = new TopicLinked
                    {
                        TopicRID = "9C3449EE278C4D94AA5A7C286729DEA0",
                        TopicInfo = "Volksfeste/Festivals",
                    };
                    break;
                case "Gite/escursioni":
                    topic = new TopicLinked
                    {
                        TopicRID = "ACE8B613F2074A7BB59C0B1DD40A43CD",
                        TopicInfo = "Wanderungen/Ausflüge",
                    };
                    break;
                case "Visite guidate":
                    topic = new TopicLinked
                    {
                        TopicRID = "B5467FEFE5C74FA5AD32B83793A76165",
                        TopicInfo = "Führungen/Besichtigungen",
                    };
                    break;
                case "Mostre/arte":
                    topic = new TopicLinked
                    {
                        TopicRID = "C72CE969B98947FABC99CBC7B033F28E",
                        TopicInfo = "Ausstellungen/Kunst",
                    };
                    break;
                case "Famiglia":
                    topic = new TopicLinked
                    {
                        TopicRID = "D98B49DF24C342D09A8161836435CF86",
                        TopicInfo = "Familie",
                    };
                    break;
                default:
                    topic = new TopicLinked
                    {
                        TopicRID = "C72CE969B98947FABC99CBC7B033F28E",
                        TopicInfo = "Ausstellungen/Kunst",
                    };
                    break;
            }

            return new List<TopicLinked>() { topic };
        }

        public static EventLinked ParseNinjaEventToODHEvent(
            string id,
            NinjaEvent ninjaevent,
            NinjaData<NinjaPlaceRoom> place,
            NinjaData<NinjaPlaceRoom> room
        )
        {
            try
            {
                if (id == "------" && place == null)
                    throw new Exception("incomplete data, no id");

                EventLinked myevent = new EventLinked();
                myevent.Id = id.ToUpper();

                //ADD MAPPING
                var ninjaid = new Dictionary<string, string>() { { "id", id } };
                myevent.Mapping.TryAddOrUpdate("culture", ninjaid);

                string source = !String.IsNullOrEmpty(place.sname)
                    ? place.sname.ToLower()
                    : "ninja";

                Metadata metainfo = new Metadata()
                {
                    Id = id,
                    LastUpdate = DateTime.Now,
                    Source = source,
                    Type = "event",
                };
                myevent._Meta = metainfo;

                myevent.Source = source;

                LicenseInfo licenseInfo = new LicenseInfo()
                {
                    ClosedData = false,
                    Author = "",
                    License = "CC0",
                    LicenseHolder = source,
                };
                myevent.LicenseInfo = licenseInfo;

                myevent.EventProperty = new EventProperty();

                //Maybe needeed by DD Transformer
                //myevent.Ranc = 0;
                //myevent.Type = "1";
                myevent.EventProperty.RegistrationRequired = false;

                //Take only Languages that are defined on title
                var languages = ninjaevent.title.Keys;

                //Detail Info
                foreach (var language in languages)
                {
                    Detail mydetail = new Detail();
                    mydetail.Language = language;
                    mydetail.Title =
                        ninjaevent.title != null
                            ? ninjaevent.title.ContainsKey(language)
                                ? ninjaevent.title[language]
                                : "no title"
                            : "no title";
                    mydetail.BaseText =
                        ninjaevent.decription != null
                            ? ninjaevent.decription.ContainsKey(language)
                                ? ninjaevent.decription[language]
                                : ""
                            : "";

                    myevent.Detail.TryAddOrUpdate(language, mydetail);
                }

                bool ticket = false;

                //Ticket and Price Info
                if (ninjaevent.ticket == "Yes")
                {
                    //myevent.Ticket = ninjaevent.price;
                    ticket = true;
                }

                //Try to convert price to double
                if (Double.TryParse(ninjaevent.price, out var pricedouble))
                {
                    if (pricedouble > 0)
                    {
                        foreach (var language in languages)
                        {
                            EventPrice myeventprice = new EventPrice();
                            myeventprice.Language = language;
                            myeventprice.Price = pricedouble;
                            myeventprice.Type = ninjaevent.event_type_key;

                            myevent.EventPrice.TryAddOrUpdate(language, myeventprice);
                        }
                    }
                }

                //Add Type info
                myevent.Topics = GetTopicRid(ninjaevent.event_type_key);
                myevent.TopicRIDs = myevent.Topics.Select(x => x.TopicRID).ToList();

                //Console.WriteLine("Parsing: " + ninjaevent.begin_date + " " + ninjaevent.begin_time);

                //TODO PARSING FAILS IF format of datetime is not exactly as described
                //TODO Resolve this "exception": "String '04/04/2022 9:00' was not recognized as a valid DateTime.",


                //Date Info
                //myevent.DateBegin = DateTime.ParseExact(ninjaevent.begin_date + " " + ninjaevent.begin_time, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                //myevent.DateEnd = DateTime.ParseExact(ninjaevent.end_date + " " + ninjaevent.end_time, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

                myevent.DateBegin = TryParsingToDateTime(
                    ninjaevent.begin_date + " " + ninjaevent.begin_time
                );
                myevent.DateEnd = TryParsingToDateTime(
                    ninjaevent.end_date + " " + ninjaevent.end_time
                );

                //DateTime.TryParse(ninjaevent.begin_date + " " + ninjaevent.begin_time, CultureInfo.InvariantCulture, out evendatebegin);
                //DateTime.TryParse(ninjaevent.end_date + " " + ninjaevent.end_time, CultureInfo.InvariantCulture, out evendateend);

                //CultureInfo myculture = new CultureInfo("en-GB");
                //string begindate = ninjaevent.begin_date + " " + ninjaevent.begin_time + ":00";
                //string enddate = ninjaevent.end_date + " " + ninjaevent.end_time + ":00";
                //myevent.DateBegin = Convert.ToDateTime(begindate, myculture);
                //myevent.DateEnd = Convert.ToDateTime(enddate, myculture);

                //myevent.NextBeginDate = myevent.DateBegin;

                myevent.EventDate = new List<EventDate>()
                {
                    new EventDate()
                    {
                        Begin = TimeSpan.Parse(ninjaevent.begin_time),
                        From = DateTime.ParseExact(
                            ninjaevent.begin_date,
                            "dd/MM/yyyy",
                            CultureInfo.InvariantCulture
                        ),
                        End = TimeSpan.Parse(ninjaevent.end_time),
                        To = DateTime.ParseExact(
                            ninjaevent.end_date,
                            "dd/MM/yyyy",
                            CultureInfo.InvariantCulture
                        ),
                        Ticket = ticket,
                        MaxPersons =
                            !String.IsNullOrEmpty(ninjaevent.number_of_seats)
                            && int.TryParse(ninjaevent.number_of_seats, out var numberofseatsint)
                                ? numberofseatsint
                                : 0,
                    },
                };

                myevent.EventProperty.TicketRequired = ticket;
                //myevent.PayMet = paymet;

                myevent.Shortname = myevent.Detail.FirstOrDefault().Value.Title;
                myevent.LastChange = DateTime.Now;
                myevent._Meta.LastUpdate = myevent.LastChange;

                //Gps Info
                GpsInfo eventgpsinfo = new GpsInfo();
                eventgpsinfo.Latitude = place != null ? place.scoordinate.y : 0;
                eventgpsinfo.Longitude = place != null ? place.scoordinate.x : 0;
                eventgpsinfo.Gpstype = "position";

                myevent.GpsInfo = new List<GpsInfo>();
                myevent.GpsInfo.Add(eventgpsinfo);

                IDictionary<string, string> floor = new Dictionary<string, string>();
                floor.Add(new KeyValuePair<string, string>("de", "Stock"));
                floor.Add(new KeyValuePair<string, string>("it", "piano"));
                floor.Add(new KeyValuePair<string, string>("en", "floor"));

                //Contact Info
                foreach (var language in languages)
                {
                    if (room != null)
                    {
                        string floorstr = " ";
                        if (
                            !String.IsNullOrEmpty(room.smetadata.floor.ToString())
                            && floor.ContainsKey(language)
                        )
                            floorstr = floorstr + room.smetadata.floor + " " + floor[language];

                        ContactInfos mycontact = new ContactInfos();
                        mycontact.Language = language;
                        mycontact.Address =
                            room.smetadata.address != null
                                ? room.smetadata.address.ContainsKey(language)
                                    ? room.smetadata.address[language] + floorstr
                                    : ""
                                : "";
                        mycontact.City =
                            room.smetadata.city != null
                                ? room.smetadata.city.ContainsKey(language)
                                    ? room.smetadata.city[language]
                                    : ""
                                : "";
                        mycontact.CompanyName =
                            room.smetadata.name != null
                                ? room.smetadata.name.ContainsKey(language)
                                    ? room.smetadata.name[language]
                                    : ""
                                : "";
                        mycontact.Phonenumber = room.smetadata.phone;
                        mycontact.Email = room.smetadata.email;
                        mycontact.ZipCode = room.smetadata.zipcode;
                        mycontact.Email = room.smetadata.email;
                        mycontact.CountryCode = "IT";
                        myevent.ContactInfos.TryAddOrUpdate(language, mycontact);
                    }
                }

                //Organizer Info
                foreach (var language in languages)
                {
                    if (place != null)
                    {
                        string floorstr = " ";
                        if (
                            !String.IsNullOrEmpty(place.smetadata.floor.ToString())
                            && floor.ContainsKey(language)
                        )
                            floorstr = floorstr + place.smetadata.floor + " " + floor[language];

                        ContactInfos orgcontact = new ContactInfos();
                        orgcontact.Language = language;
                        orgcontact.Address =
                            place.smetadata.address != null
                                ? place.smetadata.address.ContainsKey(language)
                                    ? place.smetadata.address[language] + floorstr
                                    : ""
                                : "";
                        orgcontact.City =
                            place.smetadata.city != null
                                ? place.smetadata.city.ContainsKey(language)
                                    ? place.smetadata.city[language]
                                    : ""
                                : "";
                        orgcontact.CompanyName =
                            place.smetadata.name != null
                                ? place.smetadata.name.ContainsKey(language)
                                    ? place.smetadata.name[language]
                                    : ""
                                : "";
                        orgcontact.Phonenumber = place.smetadata.phone;
                        orgcontact.Email = place.smetadata.email;
                        orgcontact.ZipCode = place.smetadata.zipcode;
                        orgcontact.CountryCode = "IT";

                        myevent.OrganizerInfos.TryAddOrUpdate(language, orgcontact);
                    }
                }

                myevent.EventProperty.EventOrganizerId = place.sname;

                //Event Additional Infos
                foreach (var language in languages)
                {
                    EventAdditionalInfos eventadditionalinfo = new EventAdditionalInfos();
                    eventadditionalinfo.Language = language;
                    eventadditionalinfo.Location =
                        room != null
                            ? room.smetadata.name.ContainsKey(language)
                                ? room.smetadata.name[language]
                                : ""
                            : "";
                    eventadditionalinfo.Registration = ninjaevent.link_to_ticket_info;

                    myevent.EventAdditionalInfos.TryAddOrUpdate(language, eventadditionalinfo);
                }

                myevent.EventPublisher = new List<EventPublisher>()
                {
                    new EventPublisher()
                    {
                        Publish = 1,
                        PublisherRID = ninjaevent.place,
                        Ranc = 0,
                    },
                };

                myevent.HasLanguage = languages;

                myevent.ImageGallery = new List<ImageGallery>();

                return myevent;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static DateTime TryParsingToDateTime(string datetimetoparse)
        {
            if (
                DateTime.TryParseExact(
                    datetimetoparse,
                    "dd/MM/yyyy HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime datetimetoreturn
                )
            )
                return datetimetoreturn;
            else if (
                DateTime.TryParseExact(
                    datetimetoparse,
                    "dd/MM/yyyy H:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime datetimetoreturn2
                )
            )
                return datetimetoreturn2;
            else
                throw new Exception("DateTime Parsing failed  input:" + datetimetoparse);
        }

        #endregion

        #region EventV2Parsing

        //V2 Parsing
        public static VenueV2 ParseNinjaEventPlaceToVenueV2(
            string id,
            NinjaData<NinjaPlaceRoom> place,
            string? rootvenueid
        )
        {
            bool isroom = true;

            if (String.IsNullOrEmpty(rootvenueid))
                isroom = false;

            VenueV2 venue = new VenueV2();
            venue.Id = "VEN_CULTURE_" + id.ToUpper();

            string source = !String.IsNullOrEmpty(place.smetadata.id)
                ? place.smetadata.id.ToLower()
                : "ninja";

            var ninjaid = new Dictionary<string, string>() { { "id", id } };
            venue.Mapping.TryAddOrUpdate("culture", ninjaid);

            venue.IsRoot = true;
            venue.VenueGroupId = venue.Id;
            venue.Active = true;

            if (isroom)
            {
                venue.IsRoot = false;
                venue.VenueGroupId = "VEN_CULTURE_" + rootvenueid.ToUpper();
                var parentid = new Dictionary<string, string>()
                {
                    { "parentid", "VEN_CULTURE_" + rootvenueid.ToUpper() },
                };
                venue.Mapping.TryAddOrUpdate("venue", parentid);
                source = !String.IsNullOrEmpty(place.smetadata.place)
                    ? place.smetadata.place.ToLower()
                    : "ninja";
            }

            venue.Source = source;

            Metadata metainfo = new Metadata()
            {
                Id = id,
                LastUpdate = DateTime.Now,
                Source = source,
                Type = "venuev2",
            };
            venue._Meta = metainfo;

            LicenseInfo licenseInfo = new LicenseInfo()
            {
                ClosedData = false,
                Author = "",
                License = "CC0",
                LicenseHolder = source,
            };
            venue.LicenseInfo = licenseInfo;

            var languages = place.smetadata.name.Keys;

            //Venue Name it/de/en:Name it/de/en:Description
            foreach (var language in languages)
            {
                Detail mydetail = new Detail();
                mydetail.Language = language;
                mydetail.Title =
                    place.smetadata.name != null
                        ? place.smetadata.name.ContainsKey(language)
                            ? place.smetadata.name[language]
                            : "no title"
                        : "no title";
                mydetail.BaseText =
                    place.smetadata.decription != null
                        ? place.smetadata.decription.ContainsKey(language)
                            ? place.smetadata.decription[language]
                            : ""
                        : "";

                venue.Detail.TryAddOrUpdate(language, mydetail);
            }

            venue.Shortname = venue.Detail.FirstOrDefault().Value.Title;

            venue.HasLanguage = venue.Detail.Keys;

            //Venue Address it/de/en:Address
            foreach (var language in languages)
            {
                ContactInfos contactinfo = new ContactInfos();
                contactinfo.Language = language;
                contactinfo.Address =
                    place.smetadata.address != null
                        ? place.smetadata.address.ContainsKey(language)
                            ? place.smetadata.address[language]
                            : null
                        : null;
                contactinfo.City =
                    place.smetadata.city != null
                        ? place.smetadata.city.ContainsKey(language)
                            ? place.smetadata.city[language]
                            : null
                        : null;
                contactinfo.ZipCode =
                    place.smetadata.zipcode != null ? place.smetadata.zipcode : null;
                contactinfo.Region =
                    place.smetadata.province != null ? place.smetadata.province : null;
                contactinfo.RegionCode =
                    place.smetadata.province != null ? place.smetadata.province : null;
                contactinfo.CountryCode = "IT";
                contactinfo.CountryName =
                    language == "en" ? "Italy"
                    : language == "it" ? "Italia"
                    : "Italien";

                contactinfo.CompanyName =
                    source == "trevilab" ? "Centro Trevi - TreviLab"
                    : source == "drin" ? "DRIN"
                    : null;

                contactinfo.Phonenumber =
                    place.smetadata.phone != null ? place.smetadata.phone : null;
                contactinfo.Email = place.smetadata.email != null ? place.smetadata.email : null;

                venue.ContactInfos.TryAddOrUpdate(language, contactinfo);
            }

            if (
                !String.IsNullOrEmpty(place.smetadata.open_time)
                && !String.IsNullOrEmpty(place.smetadata.closing_time)
            )
            {
                //Openingtimes
                OperationSchedule operationschedule = new OperationSchedule();
                operationschedule.OperationscheduleName = new Dictionary<string, string>()
                {
                    { "de", "Öffnungszeiten" },
                    { "it", "Orari d'apertura" },
                    { "en", "Opening time" },
                };
                operationschedule.Type = "2";
                operationschedule.Start = new DateTime(2020, 1, 1);
                operationschedule.Stop = new DateTime(2020, 12, 31);

                OperationScheduleTime ostime = new OperationScheduleTime();
                ostime.State = 2;
                ostime.Timecode = 1;
                ostime.Start = TimeSpan.Parse(place.smetadata.open_time);
                ostime.End = TimeSpan.Parse(place.smetadata.closing_time);

                string[] closingdays = Array.Empty<string>();

                if (!String.IsNullOrEmpty(place.smetadata.closing_days))
                    closingdays = place.smetadata.closing_days.Replace(" ", "").Split(",");

                ostime.Sunday = !closingdays.Contains("Sunday");
                ostime.Monday = !closingdays.Contains("Monday");
                ostime.Tuesday = !closingdays.Contains("Tuesday");
                ostime.Wednesday = !closingdays.Contains("Wednesday");
                ostime.Thursday = !closingdays.Contains("Thursday");
                ostime.Friday = !closingdays.Contains("Friday");
                ostime.Saturday = !closingdays.Contains("Saturday");

                operationschedule.OperationScheduleTime = new List<OperationScheduleTime>()
                {
                    ostime,
                };

                venue.OperationSchedule = new List<OperationSchedule>() { operationschedule };
            }

            //Gps Info
            GpsInfo eventgpsinfo = new GpsInfo();
            eventgpsinfo.Latitude = place != null ? place.scoordinate.y : 0;
            eventgpsinfo.Longitude = place != null ? place.scoordinate.x : 0;
            eventgpsinfo.Gpstype = "position";

            venue.GpsInfo = new List<GpsInfo>();
            venue.GpsInfo.Add(eventgpsinfo);

            if (isroom)
            {
                if (place.smetadata.max_seats.HasValue)
                {
                    VenueSetupV2 venuesetup = new VenueSetupV2();
                    venuesetup.Capacity = place.smetadata.max_seats.Value;
                    venuesetup.TagId = "notdefined";

                    venuesetup.Tag = new Tags() { Id = "notdefined", Source = "opendatahub" };

                    venue.Capacity = new List<VenueSetupV2>() { venuesetup };
                }
            }

            //PublishedOn
            //Check Source

            return venue;
        }

        public static List<Tags> GetTags(string ninjaeventtype)
        {
            Tags tag = new Tags();
            tag.Source = "culture";
            tag.Type = "Topic";

            switch (ninjaeventtype)
            {
                case "Convegni/conferenze":
                    tag.Id = "0D25868CC23242D6AC97AEB2973CB3D6";
                    break;
                case "Sport":
                    tag.Id = "162C0067811B477DA725D2F5F2D98398";
                    break;
                case "Enogastronomia/prodotti":
                    tag.Id = "252200A028C8449D9A6205369A6D0D36";
                    break;
                case "Artigianato/tradizioni":
                    tag.Id = "33BDC54BD39946F4852B3394B00610AE";
                    break;
                case "Fiere/mercati":
                    tag.Id = "4C4961D9FC5B48EEB73067BEB9D4402A";
                    break;
                case "Teatro/cinema":
                    tag.Id = "6884FE362C88434B9F49725E3328112B";
                    break;
                case "Corsi/lezioni":
                    tag.Id = "767F6F43FC394CE9A3C8A9725C6FF134";
                    break;
                case "Musica/danza":
                    tag.Id = "7E048074BA004EC58E29E330A9AA476B";
                    break;
                case "Sagre/feste":
                    tag.Id = "9C3449EE278C4D94AA5A7C286729DEA0";
                    break;
                case "Gite/escursioni":
                    tag.Id = "ACE8B613F2074A7BB59C0B1DD40A43CD";
                    break;
                case "Visite guidate":
                    tag.Id = "B5467FEFE5C74FA5AD32B83793A76165";
                    break;
                case "Mostre/arte":
                    tag.Id = "C72CE969B98947FABC99CBC7B033F28E";
                    break;
                case "Famiglia":
                    tag.Id = "D98B49DF24C342D09A8161836435CF86";
                    break;
                default:
                    tag.Id = "C72CE969B98947FABC99CBC7B033F28E";
                    break;
            }

            return new List<Tags>() { tag };
        }

        public static EventV2 ParseNinjaEventToODHEventV2(
            string id,
            NinjaEvent ninjaevent,
            string venueId
        )
        {
            try
            {
                if (id == "------")
                    throw new Exception("incomplete data, no id");

                EventV2 myevent = new EventV2();
                myevent.Id = "EV_CULTURE_" + id.ToUpper();

                myevent.LastChange = DateTime.Now;

                //We add the room id
                myevent.VenueId = "VEN_CULTURE_" + venueId.ToUpper();

                //ADD MAPPING
                var ninjaid = new Dictionary<string, string>() { { "id", id } };
                myevent.Mapping.TryAddOrUpdate("culture", ninjaid);

                myevent.IsRoot = true;
                myevent.EventGroupId = null;

                string source = !String.IsNullOrEmpty(ninjaevent.place)
                    ? ninjaevent.place.ToLower()
                    : "ninja";

                Metadata metainfo = new Metadata()
                {
                    Id = id,
                    LastUpdate = DateTime.Now,
                    Source = source,
                    Type = "eventv2",
                };
                myevent._Meta = metainfo;

                myevent._Meta.LastUpdate = myevent.LastChange;

                myevent.Source = source;

                LicenseInfo licenseInfo = new LicenseInfo()
                {
                    ClosedData = false,
                    Author = "",
                    License = "CC0",
                    LicenseHolder = source,
                };
                myevent.LicenseInfo = licenseInfo;

                //Take only Languages that are defined on title
                var languages = ninjaevent.title.Keys;

                //Detail Info
                foreach (var language in languages)
                {
                    Detail mydetail = new Detail();
                    mydetail.Language = language;
                    mydetail.Title =
                        ninjaevent.title != null
                            ? ninjaevent.title.ContainsKey(language)
                                ? ninjaevent.title[language]
                                : "no title"
                            : "no title";
                    mydetail.BaseText =
                        ninjaevent.decription != null
                            ? ninjaevent.decription.ContainsKey(language)
                                ? ninjaevent.decription[language]
                                : ""
                            : "";

                    myevent.Detail.TryAddOrUpdate(language, mydetail);
                }

                myevent.HasLanguage = myevent.Detail.Keys;

                myevent.Shortname = myevent.Detail.FirstOrDefault().Value.Title;

                //Date Info
                myevent.Begin = TryParsingToDateTime(
                    ninjaevent.begin_date + " " + ninjaevent.begin_time
                );
                myevent.End = TryParsingToDateTime(ninjaevent.end_date + " " + ninjaevent.end_time);

                myevent.BeginUTC = Helper.DateTimeHelper.DateTimeToUnixTimestampMilliseconds(
                    myevent.Begin
                );
                myevent.EndUTC = Helper.DateTimeHelper.DateTimeToUnixTimestampMilliseconds(
                    myevent.End
                );

                if (
                    !String.IsNullOrEmpty(ninjaevent.number_of_seats)
                    && int.TryParse(ninjaevent.number_of_seats, out var numberofseatsint)
                )
                {
                    myevent.Capacity = numberofseatsint;
                }

                //Add Type info
                myevent.Tags = GetTags(ninjaevent.event_type_key);
                myevent.TagIds = myevent.Tags.Select(x => x.Id).ToList();

                //AdditionalProps

                AdditionalInfosCentroTrevi additionalinfos = new AdditionalInfosCentroTrevi();

                bool ticket = false;

                //Ticket and Price Info
                if (ninjaevent.ticket == "Yes")
                    ticket = true;

                additionalinfos.Ticket = ticket;

                //Try to convert price to double
                if (Double.TryParse(ninjaevent.price, out var pricedouble))
                {
                    additionalinfos.Price = pricedouble;
                }
                if (!String.IsNullOrEmpty(ninjaevent.link_to_ticket_info))
                {
                    additionalinfos.TicketInfo = ninjaevent.link_to_ticket_info;
                }

                myevent.AdditionalProperties.Add(
                    typeof(AdditionalInfosCentroTrevi).Name,
                    additionalinfos
                );

                //PublishedOn

                return myevent;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        #endregion


        #region E-ChargingData Parsing

        public static ODHActivityPoiLinked ParseNinjaEchargingToODHActivityPoi(
            string id,
            NinjaDataWithParent<NinjaEchargingPlug, NinjaEchargingStation> data,
            ODHActivityPoiLinked echargingpoi,
            ODHTagLinked echargingstationtag
        )
        {
            try
            {
                if (echargingpoi == null)
                    echargingpoi = new ODHActivityPoiLinked();

                //Adding Tags
                echargingpoi.SmgTags = new List<string>()
                {
                    "poi",
                    "mobilität",
                    "e-auto ladestation",
                };

                //echargingpoi.Tags = new List<Tags>()
                //{
                //    new Tags(){ Id = "poi", Source = "idm", Type = "odhcategory", Name = "Poi" },
                //    new Tags(){ Id = "mobility", Source = "idm", Type = "odhcategory", Name = "Mobility" },
                //    new Tags(){ Id = "electric charging stations", Source = "idm", Type = "ltscategory", Name = "Electric charging stations" }
                //};

                //echargingpoi.TagIds = echargingpoi.Tags.Select(x => x.Id).ToList();


                //Adding Category
                if (echargingstationtag.DisplayAsCategory == true)
                {
                    echargingpoi.AdditionalPoiInfos = new Dictionary<string, AdditionalPoiInfos>();
                    foreach (var lang in new List<string>() { "de", "it", "en" })
                    {
                        echargingpoi.AdditionalPoiInfos.Add(
                            lang,
                            new AdditionalPoiInfos()
                            {
                                Language = lang,
                                Categories = new List<string>()
                                {
                                    echargingstationtag.TagName[lang],
                                },
                            }
                        );
                    }
                }

                echargingpoi.Id = id;

                //Detail
                var detail = default(Detail);

                string chargingstationname = data.sname;

                //If data is from static spreadsheet use scode
                if (data.porigin.ToLower() == "1uccqzavgmvyrpeq-lipffalqawcg4lfpakc2mjt79fy")
                    chargingstationname = data.scode;

                if (echargingpoi.Detail != null && echargingpoi.Detail.ContainsKey("en"))
                {
                    //sname
                    echargingpoi.Detail["en"].Title = chargingstationname;

                    echargingpoi.Detail["en"].AdditionalText = data.pname;
                    echargingpoi.Detail.TryAddOrUpdate("en", echargingpoi.Detail["en"]);
                }
                else
                    echargingpoi.Detail.TryAddOrUpdate(
                        "en",
                        new Detail()
                        {
                            Title = chargingstationname,
                            AdditionalText = data.pname,
                            Language = "en",
                        }
                    );

                //ContactInfo TO CHECK!! if Contactinfo is modified by Independent?
                echargingpoi.ContactInfos.TryAddOrUpdate(
                    "en",
                    new ContactInfos()
                    {
                        Address = data.pmetadata.address,
                        City = data.pmetadata.city,
                        Language = "en",
                    }
                );

                echargingpoi.Shortname =
                    echargingpoi.Detail != null
                        ? echargingpoi.Detail.FirstOrDefault().Value.Title
                        : data.sname;

                //GpsInfo
                //"pcoordinate": {
                //    "x": 10.800483,
                //    "y": 46.313481,
                //    "srid": 4326
                //    },
                if (data.pcoordinate != null && data.pcoordinate.x > 0 && data.pcoordinate.y > 0)
                {
                    echargingpoi.GpsInfo = new List<GpsInfo>()
                    {
                        new GpsInfo()
                        {
                            Gpstype = "position",
                            Altitude = null,
                            AltitudeUnitofMeasure = "m",
                            Latitude = data.pcoordinate.y,
                            Longitude = data.pcoordinate.x,
                        },
                    };
                }

                //Properties Echargingstation
                EchargingDataProperties properties = new EchargingDataProperties();

                properties.AccessType = data.pmetadata.accessType;

                //properties.AccessTypeInfo = new Dictionary<string, string>();
                //no multilingual field
                properties.AccessTypeInfo = data.pmetadata.accessInfo;
                properties.State = data.pmetadata.state;
                properties.Capacity = data.pmetadata?.capacity;
                properties.ChargingStationAccessible =
                    data.pmetadata.accessType != null
                    && (
                        data.pmetadata.accessType.ToLower() == "public"
                        || data.pmetadata.accessType.ToLower() == "private_withpublicaccess"
                    )
                        ? true
                        : false;
                properties.PaymentInfo = data.pmetadata?.paymentInfo;

                //properties.ChargingPlugCount = 1;
                properties.ChargingPistolTypes =
                    data.smetadata.outlets != null
                        ? data.smetadata.outlets.Select(y => y.outletTypeCode).Distinct().ToList()
                        : null;

                //TODO do not overwrite the old values
                var additionalpropertieskey = typeof(EchargingDataProperties).Name;

                if (
                    echargingpoi.AdditionalProperties != null
                    && echargingpoi.AdditionalProperties.ContainsKey(additionalpropertieskey)
                )
                {
                    var propstonotoverwrite = (
                        (JObject)echargingpoi.AdditionalProperties[additionalpropertieskey]
                    ).ToObject<EchargingDataProperties>();

                    properties.HorizontalFloorRoadSign =
                        propstonotoverwrite.HorizontalFloorRoadSign;
                    properties.SurveyDate = propstonotoverwrite.SurveyDate;
                    properties.Barrierfree = propstonotoverwrite.Barrierfree;
                    properties.CarParkingSpaceNextToEachOther =
                        propstonotoverwrite.CarParkingSpaceNextToEachOther;
                    properties.CarParkingSpaceBehindEachOther =
                        propstonotoverwrite.CarParkingSpaceBehindEachOther;
                    properties.ChargingCableLength = propstonotoverwrite.ChargingCableLength;
                    properties.ChargingPistolOperationHeight =
                        propstonotoverwrite.ChargingPistolOperationHeight;
                    properties.ChargingStationAccessible =
                        propstonotoverwrite.ChargingStationAccessible;
                    properties.Covered = propstonotoverwrite.Covered;
                    properties.DisplayOrCardReaderOperationHeight =
                        propstonotoverwrite.DisplayOrCardReaderOperationHeight;
                    properties.SteplessSidewalkConnection =
                        propstonotoverwrite.SteplessSidewalkConnection;
                    properties.SurveyAnnotations = propstonotoverwrite.SurveyAnnotations;
                    properties.SurveyDate = propstonotoverwrite.SurveyDate;
                    properties.SurveyType = propstonotoverwrite.SurveyType;
                    properties.VerticalRoadSign = propstonotoverwrite.VerticalRoadSign;
                    properties.ShieldingPostInFrontOfStation =
                        propstonotoverwrite.ShieldingPostInFrontOfStation;
                }

                //if (echargingpoi.AdditionalProperties == null)
                //    echargingpoi.AdditionalProperties = new Dictionary<string, dynamic>();

                //if (echargingpoi.AdditionalProperties.ContainsKey(additionalpropertieskey))
                //    echargingpoi.AdditionalProperties[additionalpropertieskey] = (IAdditionalProperties)properties;
                //else
                //    echargingpoi.AdditionalProperties.Add(additionalpropertieskey, (IAdditionalProperties)properties);

                echargingpoi.AdditionalProperties.TryAddOrUpdate(
                    additionalpropertieskey,
                    properties
                );

                //Mapping Object
                //ADD MAPPING
                var ninjaid = new Dictionary<string, string>()
                {
                    { "sname", data.sname },
                    { "scode", data.scode },
                };
                echargingpoi.Mapping.TryAddOrUpdate("mobility", ninjaid);

                //Source, SyncSourceInterface
                echargingpoi.Source = data.porigin.ToLower();
                echargingpoi.SyncSourceInterface = !String.IsNullOrEmpty(data.pmetadata.provider) ? data.pmetadata.provider.ToLower() : data.porigin.ToLower();
                echargingpoi.SyncUpdateMode = "partial";

                //Hack spreadsheet
                if (echargingpoi.Source == "1uccqzavgmvyrpeq-lipffalqawcg4lfpakc2mjt79fy")
                    echargingpoi.Source = "echargingspreadsheet";

                if (echargingpoi.HasLanguage == null)
                    echargingpoi.HasLanguage = new List<string>() { "en" };

                return echargingpoi;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        #endregion
    }
}
