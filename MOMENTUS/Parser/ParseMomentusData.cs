// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using MOMENTUS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOMENTUS.Parser
{
    public class ParseMomentusData
    {
        public static EventLinked? ParseMomentusEvent(MomentusEvent mevent, IEnumerable<MomentusFunction> functionlist, IEnumerable<MomentusBookedSpaceExtended> bookedspacelist, EventLinked? eventlinked, VenueV2? venuelinked, bool optimizedays = false)
        {
            if (eventlinked == null)
                eventlinked = new EventLinked();

            // Preserve manually-managed fields from existing record
            var imagegallery = eventlinked.ImageGallery?.ToList();
            var tagids = eventlinked.TagIds?.ToList();
            var documents = eventlinked.Documents;
            var videoitems = eventlinked.VideoItems;
            var firstimport = eventlinked.FirstImport;

            //What to do with
            //WebAddress
            //ExternalOrganizer
            //SoldOut
            //        //MODIFIED
            //        eventshort.TagIds = AssignTechnologyfieldsautomatically(
            //            eventshort.CompanyName,
            //            eventshort.TechnologyFields
            //        );


            // Identity
            eventlinked.Id = "urn:event:momentus:" + mevent.Id;
            eventlinked.Shortname = mevent.Name;
            eventlinked.Source = "momentus";
            eventlinked.Active = mevent.IsActive && !mevent.IsCanceled;
            eventlinked.LastChange = DateTime.Now;
            eventlinked.FirstImport = firstimport ?? DateTime.Now;

            // Date range: start from event-level dates+times, then refine from non-private booked spaces
            eventlinked.DateBegin = mevent.Start?.ToDateTime(
                TimeSpan.TryParse(mevent.StartTime, out var st) ? TimeOnly.FromTimeSpan(st) : TimeOnly.MinValue);
            eventlinked.DateEnd = mevent.End?.ToDateTime(
                TimeSpan.TryParse(mevent.EndTime, out var et) ? TimeOnly.FromTimeSpan(et) : TimeOnly.MinValue);

            // Multilingual detail: titles/subtitles from named functions, description from event
            BuildDetailFromFunctions(eventlinked, functionlist, mevent.Description);

            // Skip events with no title in any language
            if (!eventlinked.Detail.Values.Any(d => !string.IsNullOrEmpty(d.Title)))
                return null;

            // Venue reference
            if (venuelinked?.Id != null)
                eventlinked.VenueIds = [venuelinked.Id];

            // EventDates from booked spaces (one entry per day, rooms resolved via venue mapping)
            eventlinked.EventDate = BuildEventDates(mevent, venuelinked, bookedspacelist);

            // Recalculate root DateBegin/DateEnd from active EventDates
            if (optimizedays)
                RefineRootDatesFromEventDates(eventlinked);

            // ContactInfos from first contact role
            if (mevent.ContactRoles != null && mevent.ContactRoles.Count > 0)
            {
                var contact = BuildContactInfo(mevent.ContactRoles.First());
                eventlinked.ContactInfos = new Dictionary<string, ContactInfos>() { { "en", contact } };
            }

            // OrganizerInfos from first contact role, with event AccountName as CompanyName
            if (mevent.ContactRoles != null && mevent.ContactRoles.Count > 0 && !string.IsNullOrEmpty(mevent.AccountName))
            {
                var organizer = BuildOrganizerInfo(mevent.ContactRoles.First(), mevent.AccountName);
                eventlinked.OrganizerInfos = new Dictionary<string, ContactInfos>() { { "en", organizer } };
            }

            // Mapping
            if (eventlinked.Mapping == null)
                eventlinked.Mapping = new Dictionary<string, IDictionary<string, string>>();

            var momentusMapping = new Dictionary<string, string>()
            {
                ["id"] = mevent.Id ?? "",
                ["eventTypeId"] = mevent.EventTypeId ?? "",
                ["eventTypeName"] = mevent.EventTypeName ?? "",
            };
            if (mevent.ExternalIds != null)
            {
                foreach (var extid in mevent.ExternalIds.Where(e => e.Key != null))
                    momentusMapping[extid.Key!] = extid.Value ?? "";
            }
            eventlinked.Mapping["momentus"] = momentusMapping;

            // EventUrls from website
            if (!string.IsNullOrEmpty(mevent.Website))
            {
                eventlinked.EventUrls =
                [
                    new EventUrls()
                    {
                        Url = new Dictionary<string, string>() { { "en", mevent.Website } },
                        Type = "default",
                        Active = true
                    }
                ];
            }

            // Get eventlocation value from venue Mapping["tag"]["eventlocation"] (e.g. "noi", "ec")
            string? venueEventLocation = null;
            if (venuelinked?.Mapping != null &&
                venuelinked.Mapping.TryGetValue("tag", out var venueTagMap) &&
                venueTagMap.TryGetValue("eventlocation", out var el) &&
                !string.IsNullOrEmpty(el))
                venueEventLocation = el;

            // PublishedOn is derived from SpaceUsageName (from extended booked spaces) and venue eventlocation
            eventlinked.PublishedOn = DeterminePublishedOn(mevent, bookedspacelist, venueEventLocation);

            // Restore preserved fields
            eventlinked.ImageGallery = imagegallery;
            eventlinked.Documents = documents;
            eventlinked.VideoItems = videoitems;

            // Merge venue eventlocation tag into TagIds, preserving existing tags
            tagids ??= [];
            if (venueEventLocation != null && !tagids.Contains(venueEventLocation))
                tagids.Add(venueEventLocation);

            eventlinked.TagIds = tagids.Count > 0 ? tagids : null;

            //New If CompanyName is Noi -  assign TechnologyField automatically
            var organizerName = eventlinked.OrganizerInfos?.FirstOrDefault().Value?.CompanyName;
            if (!string.IsNullOrEmpty(organizerName) && organizerName.StartsWith("NOI - "))
                tagids = AssignTechnologyfieldsautomatically(organizerName, tagids);


            return eventlinked;
        }

        private static List<string> DeterminePublishedOn(MomentusEvent mevent, IEnumerable<MomentusBookedSpaceExtended> bookedspacelist, string? venueEventLocation)
        {
            // Only consider rooms categorized as "event"
            var eventSpaceIds = mevent.BookedSpaces?
                .Where(b => string.Equals(b.UsageType, "event", StringComparison.OrdinalIgnoreCase) && b.BookedSpaceId != null)
                .Select(b => b.BookedSpaceId!)
                .ToHashSet();

            if (eventSpaceIds == null || eventSpaceIds.Count == 0)
                return [];

            // Join with extended list to get SpaceUsageName
            var spaceUsageNames = bookedspacelist
                .Where(b => b.Id != null && eventSpaceIds.Contains(b.Id) && !string.IsNullOrEmpty(b.SpaceUsageName))
                .Select(b => b.SpaceUsageName!.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();

            if (spaceUsageNames.Count == 0 || spaceUsageNames.All(u => u.Contains("PRIVATE")))
                return [];

            // PUBLIC wins if at least one space has it
            string effectiveType;
            if (spaceUsageNames.Any(u => u.Contains("PUBLIC")))
                effectiveType = "PUBLIC";
            else if (spaceUsageNames.Any(u => u.Contains("VIDEOWALL")))
                effectiveType = "VIDEOWALL";
            else if (spaceUsageNames.Any(u => u.Contains("ROOM")))
                effectiveType = "ROOM";
            else
                return [];

            bool isEurac = string.Equals(venueEventLocation, "ec", StringComparison.OrdinalIgnoreCase);
            bool isNoi = string.Equals(venueEventLocation, "noi", StringComparison.OrdinalIgnoreCase);

            var publishers = new List<string>();

            if (effectiveType == "PUBLIC")
            {
                if (isEurac) publishers.AddRange(["eurac-videowall", "eurac-seminarroom"]);
                if (isNoi)   publishers.AddRange(["noi-totem", "today.noi.bz.it"]);
            }
            else if (effectiveType == "VIDEOWALL")
            {
                if (isEurac) publishers.Add("eurac-videowall");
                if (isNoi)   publishers.Add("today.noi.bz.it");
            }
            else if (effectiveType == "ROOM")
            {
                if (isEurac) publishers.Add("eurac-seminarroom");
                if (isNoi)   publishers.Add("noi-totem");
            }

            return publishers;
        }

        private static void RefineRootDatesFromEventDates(EventLinked eventlinked)
        {
            var active = eventlinked.EventDate?.Where(ed => ed.Active == true).ToList();
            if (active == null || active.Count == 0)
                return;

            var first = active.OrderBy(ed => ed.From).First();
            eventlinked.DateBegin = first.Begin.HasValue
                ? first.From.Date + first.Begin.Value
                : first.From.Date;

            var last = active.OrderBy(ed => ed.To).Last();
            eventlinked.DateEnd = last.End.HasValue
                ? last.To.Date + last.End.Value
                : last.To.Date;
        }

        private static void BuildDetailFromFunctions(EventLinked eventlinked, IEnumerable<MomentusFunction> functionlist, string? description)
        {
            foreach (var lang in new[] { "de", "it", "en" })
            {
                if (!eventlinked.Detail.ContainsKey(lang))
                    eventlinked.Detail[lang] = new Detail() { Language = lang };

                if (!string.IsNullOrEmpty(description) && string.IsNullOrEmpty(eventlinked.Detail[lang].BaseText))
                    eventlinked.Detail[lang].BaseText = description;
            }

            if (functionlist == null)
                return;

            foreach (var function in functionlist.Where(f => !string.IsNullOrEmpty(f.FunctionTypeName)))
            {
                switch (function.FunctionTypeName!.Trim())
                {
                    case "EN Title":    eventlinked.Detail["en"].Title     = function.Name; break;
                    case "DE Title":    eventlinked.Detail["de"].Title     = function.Name; break;
                    case "IT Title":    eventlinked.Detail["it"].Title     = function.Name; break;
                    case "EN SUBtitle": eventlinked.Detail["en"].SubHeader = function.Name; break;
                    case "DE SUBtitle": eventlinked.Detail["de"].SubHeader = function.Name; break;
                    case "IT SUBtitle": eventlinked.Detail["it"].SubHeader = function.Name; break;
                }
            }
        }

        private static List<EventDate> BuildEventDates(MomentusEvent mevent, VenueV2? venuelinked, IEnumerable<MomentusBookedSpaceExtended> bookedspacelist)
        {
            var eventdates = new List<EventDate>();

            var spaces = mevent.BookedSpaces?
                .Where(b => b.StartDate != null && string.Equals(b.UsageType, "event", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (spaces == null || spaces.Count == 0)
                return eventdates;

            foreach (var space in spaces)
            {
                var extendedSpace = space.BookedSpaceId != null
                    ? bookedspacelist.FirstOrDefault(b => b.Id == space.BookedSpaceId)
                    : null;

                bool isPrivate = extendedSpace?.SpaceUsageName != null &&
                    extendedSpace.SpaceUsageName.Contains("PRIVATE", StringComparison.OrdinalIgnoreCase);

                var eventdate = new EventDate
                {
                    From = space.StartDate!.Value.ToDateTime(TimeOnly.MinValue),
                    To = space.EndDate.HasValue ? space.EndDate.Value.ToDateTime(TimeOnly.MinValue) : space.StartDate!.Value.ToDateTime(TimeOnly.MinValue),
                    Active = !isPrivate
                };

                if (TimeSpan.TryParse(space.StartTime, out var begin))
                    eventdate.Begin = begin;

                if (TimeSpan.TryParse(space.EndTime, out var end))
                    eventdate.End = end;

                if (venuelinked?.RoomDetails != null && space.RoomId != null)
                {
                    var room = venuelinked.RoomDetails.FirstOrDefault(r =>
                        r.Mapping != null &&
                        r.Mapping.ContainsKey("momentus") &&
                        r.Mapping["momentus"].ContainsKey("id") &&
                        r.Mapping["momentus"]["id"] == space.RoomId);

                    if (room?.Id != null)
                        eventdate.VenueRoomDetailsIds = [room.Id];
                }

                eventdates.Add(eventdate);
            }

            return eventdates;
        }

        private static ContactInfos BuildContactInfo(MomentusContactRole contact)
        {
            string? givenname = null;
            string? surname = null;

            if (!string.IsNullOrEmpty(contact.Name))
            {
                var parts = contact.Name.Trim().Split(' ', 2);
                givenname = parts[0];
                surname = parts.Length > 1 ? parts[1] : null;
            }

            return new ContactInfos()
            {
                Language = "en",
                Givenname = givenname,
                Surname = surname,
                CompanyName = contact.AccountName,
                Email = contact.Email,
                Phonenumber = contact.Phone,
                Address = contact.Address1,
                City = contact.AddressCity,
                ZipCode = contact.AddressPostalCode,
                CountryName = contact.AddressCountry
            };
        }

        private static ContactInfos BuildOrganizerInfo(MomentusContactRole contact, string? accountName)
        {
            string? givenname = null;
            string? surname = null;

            if (!string.IsNullOrEmpty(contact.Name))
            {
                var parts = contact.Name.Trim().Split(' ', 2);
                givenname = parts[0];
                surname = parts.Length > 1 ? parts[1] : null;
            }

            return new ContactInfos()
            {
                Language = "en",
                Givenname = givenname,
                Surname = surname,
                CompanyName = accountName,
                Email = contact.Email,
                Phonenumber = contact.Phone,
                Address = contact.Address1,
                City = contact.AddressCity,
                ZipCode = contact.AddressPostalCode,
                CountryName = contact.AddressCountry
            };
        }

         private static List<string>? AssignTechnologyfieldsautomatically(
            string companyname,
            List<string>? technologyfields
        )
        {
            if (technologyfields == null)
                technologyfields = new List<string>();

            //Digital, Alpine, Automotive/Automation, Food, Green

            AssignTechnologyFields(companyname, "digital", "digital", technologyfields);
            AssignTechnologyFields(companyname, "alpine", "alpine", technologyfields);
            AssignTechnologyFields(
                companyname,
                "automotive",
                "automotiveautomation",
                technologyfields
            );
            AssignTechnologyFields(companyname, "food", "food", technologyfields);
            AssignTechnologyFields(companyname, "green", "green", technologyfields);

            if (technologyfields.Count == 0)
                return null;
            else
                return technologyfields;
        }

        private static void AssignTechnologyFields(
            string companyname,
            string tocheck,
            string toassign,
            List<string> automatictechnologyfields
        )
        {
            if (companyname.Contains(tocheck, StringComparison.OrdinalIgnoreCase))
                if (!automatictechnologyfields.Contains(toassign))
                    automatictechnologyfields.Add(toassign);
        }
    }
}
