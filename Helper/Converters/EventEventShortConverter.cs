// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NGuid;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Helper.Converters
{   
    public class EventEventShortConverter
    {

        //Convert EventShort to EventV2
        private static EventLinked ConvertEventShortToEvent(
            EventShortLinked eventshort,
            bool removeinactiverooms = false
        )
        {
            EventLinked eventv1 = new EventLinked();

            eventv1.Id = "urn:event:" + eventshort.Source + ":" + eventshort.Id.Replace("eventshort-", "");
            eventv1.ImageGallery = eventshort.ImageGallery;
            eventv1.TagIds = eventshort.TagIds;
            eventv1.Tags = eventshort.Tags;
            eventv1.Active = eventshort.Active != null ? eventshort.Active.Value : false;
            eventv1.PublishedOn = eventshort.PublishedOn;

            eventv1.Detail = eventshort.Detail;
            eventv1.Source = eventshort.Source;

            eventv1.GpsInfo = eventshort.GpsInfo;
            eventv1.Mapping = eventshort.Mapping;

            eventv1.DateBegin = eventshort.StartDate;
            eventv1.DateEnd = eventshort.EndDate;

            eventv1.FirstImport = eventshort.FirstImport;
            eventv1.LastChange = eventshort.LastChange;
            eventv1.Shortname = eventshort.Shortname;

            eventv1.HasLanguage = eventshort.HasLanguage;

            eventv1.LicenseInfo = eventshort.LicenseInfo;

            eventv1.RelatedContent = eventshort.RelatedContent;

            eventv1.Mapping = eventshort.Mapping;

            if (eventv1.Mapping == null)
                eventv1.Mapping = new Dictionary<string, IDictionary<string, string>>();

            var mappingtoadd = eventv1.Mapping.ContainsKey("ebms") ? eventv1.Mapping["ebms"] : new Dictionary<string, string>();
            if (eventshort.EventId != null)
                mappingtoadd.Add("eventid", eventshort.EventId.ToString());

            if (eventshort.CompanyId != null)
                mappingtoadd.Add("companyid", eventshort.CompanyId.ToString());

            eventv1.Mapping.TryAddOrUpdate("ebms", mappingtoadd);

            eventv1.TagIds = new List<string>();

            if (eventshort.CustomTagging != null)
            {
                foreach (var tag in eventshort.CustomTagging)
                {
                    eventv1.TagIds.Add(tag.ToLower()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("&", "")
                        );
                }
            }

            if (eventshort.TechnologyFields != null)
            {
                foreach (var tag in eventshort.TechnologyFields)
                {
                    eventv1.TagIds.Add(tag.ToLower()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("&", "")
                        );
                }
            }

            //Add Location as Tag
            if (!String.IsNullOrEmpty(eventshort.EventLocation))
                eventv1.TagIds.Add("eventlocation:" + eventshort.EventLocation.ToLower());

            //To check Add each Room as Tag?


            //Test if this works
            if (eventshort.Documents != null)
            {
                eventv1.Documents = eventshort.Documents?
                                    .ToDictionary(
                                        kvp => kvp.Key,
                                        kvp => (ICollection<Document>)kvp.Value
                                    );
            }
            eventv1.VideoItems = eventshort.VideoItems;

            if(eventshort.WebAddress != null)
                eventv1.EventUrls = new List<EventUrls>() { new EventUrls() { Active = true, Type = "default", Url = new Dictionary<string, string>() { { "en", eventshort.WebAddress } } } };

            //Fields to map
            //EventId               --> Mapping
            //CompanyId             --> Mapping            
            //Display1 - Display9   --> Dismiss         
            //ActiveWeb             --> Dismiss
            //ActiveToday           --> Dismiss            
            //EventText             --> in Detail.Basetext
            //EventTitle            --> in Detail.Title
            //EventDescription      --> in Detail
            //EventDescriptionDE    --> in Detail
            //EventDescriptionEN    --> in Detail
            //EventDescriptionIT    --> in Detail
            //EventTextDE           --> in Detail
            //EventTextEN           --> in Detail
            //EventTextIT           --> in Detail
            //GpsPoints             --> Autogenerated
            //CustomTagging         --> Migrated to Tags


            //Documents             
            //VideoUrl
            //VideoItems
            //WebAddress            --> EventUrls
            //AnchorVenue           --> EventAdditionalInfos.Location
            //AnchorVenueShort
            //AnchorVenueRoomMapping
            //EventDocument
            //EventLocation         --> Tag!
            //AdditionalProperties

            //RoomBooked.SpaceDesc  -->
            //RoomBooked.SpaceType
            //RoomBooked.SpaceAbbrev
            
            //TODO THIS IS NOT WORKING AS IT SHOULD!
            EventEuracNoiDataProperties additionalprops = new EventEuracNoiDataProperties();
            additionalprops.ExternalOrganizer = eventshort.ExternalOrganizer;
            additionalprops.SoldOut = eventshort.SoldOut;
            additionalprops.TypicalAgeRange = eventshort.TypicalAgeRange;
            additionalprops.EventLocation = eventshort.EventLocation;

            //find a better key here
            eventv1.AdditionalProperties = new Dictionary<string, dynamic>();
            eventv1.AdditionalProperties.Add("EventEuracNoiDataProperties", additionalprops);


            if (eventshort.RoomBooked != null)
            {
                foreach (var roombooked in eventshort.RoomBooked)
                {
                    if (eventv1.EventDate == null)
                        eventv1.EventDate = new List<EventDate>();

                    EventDate eventdate = new EventDate();

                    //Comment
                    if (roombooked.Comment != null && roombooked.Comment.ToLower() == "x")
                        eventdate.Active = false;
                    else
                        eventdate.Active = true;

                    //StartDate
                    eventdate.From = roombooked.StartDate.Date;
                    eventdate.To = roombooked.EndDate.Date;

                    //StartDateUTC
                    //EndDateUTC

                    eventdate.Begin = roombooked.StartDate.TimeOfDay;
                    eventdate.End = roombooked.EndDate.TimeOfDay;

                    //Subtitle
                    if (!String.IsNullOrEmpty(roombooked.Subtitle))
                    {
                        if (eventdate.EventDateAdditionalInfo == null)
                            eventdate.EventDateAdditionalInfo = new Dictionary<string, EventDateAdditionalInfo>();

                        eventdate.EventDateAdditionalInfo.Add("en", new EventDateAdditionalInfo() { Language = "en", Description = roombooked.Subtitle });
                    }

                    //Space
                    //SpaceDesc
                    //SpaceType                    
                    //SpaceAbbrev

                    //Venue
                    if (eventdate.VenueRoomDetailsIds == null)
                        eventdate.VenueRoomDetailsIds = new List<string>();

                    if (!String.IsNullOrEmpty(roombooked.SpaceDesc))
                        eventdate.VenueRoomDetailsIds.Add(GetRoomBookedVenueId(eventshort, roombooked).Item1);

                    if(removeinactiverooms)
                    {
                        if(eventdate.Active.Value)
                            eventv1.EventDate.Add(eventdate);
                    }
                    else
                        eventv1.EventDate.Add(eventdate);
                }
            }


            //ContactInfo
            //Only if some data is provided

            if(!String.IsNullOrEmpty(eventshort.ContactFirstName))
            {
                var contactinfo = new ContactInfos();
                contactinfo.Faxnumber = eventshort.ContactFax;
                contactinfo.City = eventshort.ContactCity;

                contactinfo.ZipCode = eventshort.ContactPostalCode;
                contactinfo.Address = eventshort.ContactAddressLine1;
                contactinfo.Email = eventshort.ContactEmail;
                contactinfo.CountryName = eventshort.ContactCountry;
                contactinfo.Phonenumber = eventshort.ContactPhone;
                contactinfo.Surname = eventshort.ContactLastName;
                contactinfo.Givenname = eventshort.ContactFirstName;
                contactinfo.Area = eventshort.ContactAddressLine2;
                contactinfo.Region = eventshort.ContactAddressLine3;
                contactinfo.Url = eventshort.WebAddress;

                eventv1.ContactInfos = new Dictionary<string, ContactInfos>();
                eventv1.ContactInfos.Add("en", contactinfo);
            }
            

            //OrganizerInfos
            if(!String.IsNullOrEmpty(eventshort.CompanyName))
            {
                var organizerinfo = new ContactInfos();
                organizerinfo.Faxnumber = eventshort.CompanyFax;
                organizerinfo.City = eventshort.CompanyCity;

                // Split ZipCode and Address from "ZipCode Address"            
                organizerinfo.Address = eventshort.CompanyAddressLine1;
                organizerinfo.ZipCode = eventshort.CompanyPostalCode;

                organizerinfo.Email = eventshort.CompanyMail;
                organizerinfo.CountryName = eventshort.CompanyCountry;
                organizerinfo.Phonenumber = eventshort.CompanyPhone;
                organizerinfo.CompanyName = eventshort.CompanyName;
                organizerinfo.Area = eventshort.CompanyAddressLine2;
                organizerinfo.Region = eventshort.CompanyAddressLine3;
                organizerinfo.Url = eventshort.CompanyUrl;

                eventv1.OrganizerInfos = new Dictionary<string, ContactInfos>();
                eventv1.OrganizerInfos.Add("en", organizerinfo);
            }


            //Venue
            if (eventv1.VenueIds == null)
                eventv1.VenueIds = new List<string>();

            if (!String.IsNullOrEmpty(eventshort.AnchorVenue))
                eventv1.VenueIds.Add(GetVenueId(eventshort).Item1);

            //_Meta generation
            var meta = eventshort._Meta;
            meta.Type = "event";
            meta.Id = eventv1.Id;

            eventv1.LicenseInfo = eventv1.LicenseInfo;
            eventv1._Meta = meta;

            return eventv1;
        }

        private static IEnumerable<VenueV2> ConvertEventShortToVenue(
            IEnumerable<EventShortLinked> eventshortlist
            )
        {
            List<VenueV2> venuelist = new List<VenueV2>();

            foreach (var eventshort in eventshortlist)
            {
                //Exclude EventLocation Virtual Village and Empty String

                if (!new List<string>() { "VV", "" }.Contains(eventshort.EventLocation))
                {
                    var (venueid, eventlocation, source) = GetVenueId(eventshort);                    

                    VenueV2 venue = new VenueV2();
                    venue.Id = venueid;
                    venue.Shortname = eventshort.AnchorVenue;
                    venue.Active = true;
                    venue.FirstImport = DateTime.Now;
                    venue.LastChange = DateTime.Now;
                    venue._Meta = new Metadata() { Id = venue.Id, LastUpdate = DateTime.Now, Reduced = false, Source = "noi", Type = "venue" };
                    venue.Source = source;
                    venue.LicenseInfo = new LicenseInfo() { License = "CC0", LicenseHolder = "https://noi.bz.it" };

                    venue.Detail.TryAddOrUpdate("en", new Detail(){ Title = eventshort.AnchorVenue, Language = "en" });

                    //Fill manually
                    //venue.Detail.Add("de", new Detail() { Language = "de", Title =  })

                    foreach (var room in eventshort.RoomBooked)
                    {
                        if (venue.RoomDetails == null)
                            venue.RoomDetails = new List<VenueRoomDetailsV2>();

                        if (!String.IsNullOrEmpty(room.SpaceDesc))
                        //Add as Venue
                        {
                            var (venueroomid, space) = GetRoomBookedVenueId(eventshort, room);

                            VenueRoomDetailsV2 venueroom = new VenueRoomDetailsV2();
                            venueroom.Id = venueroomid;
                            venueroom.Detail.TryAddOrUpdate("en", new DetailGeneric() { Title = room.SpaceDesc, Language = "en" });
                            
                            venue.RoomDetails.Add(venueroom);
                        }
                    }

                    venuelist.Add(venue);
                }           
            }

            var merged = venuelist
                .GroupBy(v => v.Id)
                .Select(g => new VenueV2
                {
                    Id = g.Key,
                    Active = g.First().Active,
                    Shortname = g.First().Shortname,
                    Source = g.First().Source,
                    FirstImport = DateTime.Now,
                    LastChange = DateTime.Now,
                    Detail = g.First().Detail,
                    LicenseInfo = g.First().LicenseInfo,
                    _Meta = g.First()._Meta,
                    RoomDetails = g.SelectMany(v => v.RoomDetails ?? Enumerable.Empty<VenueRoomDetailsV2>())
                                   .DistinctBy(r => r.Id)
                                   .ToList()
                })
                .ToList();

            return merged;
        }

        private static (string, string, string) GetVenueId(EventShort eventshort)
        {
            var eventlocation = eventshort.EventLocation.ToLower();
            var source = eventshort.EventLocation.ToLower();

            if (eventlocation == "ec")
            {
                eventlocation = "eurac";
                source = "eurac";
            }

            if (eventlocation == "noi")
            {
                eventlocation = "noi";
                source = "noi";
            }

            if (eventlocation == "out")
            {
                eventlocation = "out";
                source = "noi";
            }

            if (eventlocation == "noibruneck")
            {
                eventlocation = "noibruneck";
                source = "nobis";
            }

            var venueid = "urn:venue:" + eventlocation + ":" + GuidHelpers.CreateFromName(Guid.Empty, eventlocation);

            return (venueid, eventlocation, source);
        }

        private static (string, string) GetRoomBookedVenueId(EventShort eventshort, RoomBooked room)
        {
            var space = !String.IsNullOrEmpty(room.SpaceType) ? room.SpaceType.ToLower() : eventshort.EventLocation.ToLower();

            if (space == "no")
                space = "noi";
            if (space == "noi")
                space = "noi";
            if (space == "ec")
                space = "eurac";
            if (space == "vi")
                space = eventshort.EventLocation.ToLower() == "ec" ? "eurac" : "noi";
            
            var venueroomid = "urn:venueroomid:" + space + ":" + GuidHelpers.CreateFromName(Guid.Empty, EventVenueHelper.SlugifyRoomName(room.SpaceDesc));

            return (venueroomid, space);
        }

        public static IEnumerable<EventLinked> ConvertEventShortToEventByType(
            EventShortLinked eventshort,
            bool denormalized,
            string? denormalizedatetimecheck,
            bool removeinactiverooms
        )
        {
            var eventLinked = ConvertEventShortToEvent(eventshort, removeinactiverooms);

            if (denormalized)
            {
                if (DateTime.TryParse(denormalizedatetimecheck, out var denormalizetdatetimedt))
                {
                    // Denormalize by EventDate and add only Elements with datetime higher than the provided
                    var byEventDate = eventLinked.DenormalizeBy(
                        e => e.EventDate,
                        (e, val) => e.EventDate = val,
                        item => item.From >= denormalizetdatetimedt
                    );
                    return byEventDate;
                }
                else
                {
                    // Denormalize by EventDate
                    var byEventDate = eventLinked.DenormalizeBy(
                        e => e.EventDate,
                        (e, val) => e.EventDate = val
                    );
                    return byEventDate;
                }
                
            }
            else
                return new List<EventLinked>() { eventLinked };
        }

        public static IEnumerable<VenueV2> ConvertEventShortsToVenueList(
            IEnumerable<EventShortLinked> eventshortlist
            )
        {
            //Eventlocation can be
            //NOI           NOI
            //NOI Bruneck   NOIBRUNECK
            //Other         OUT
            //VirtualVillageVV

            return ConvertEventShortToVenue(eventshortlist);
        }
  }
}
