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
        public static EventLinked ParseMomentusEvent(MomentusEvent mevent, IEnumerable<MomentusFunction> functionlist, EventLinked? eventlinked, VenueV2? venuelinked)
        {
            if (eventlinked == null)
                eventlinked = new EventLinked();

            // Preserve manually-managed fields from existing record
            var imagegallery = eventlinked.ImageGallery?.ToList();
            var tagids = eventlinked.TagIds?.ToList();
            var documents = eventlinked.Documents;
            var videoitems = eventlinked.VideoItems;
            var firstimport = eventlinked.FirstImport;

            // Identity
            eventlinked.Id = "urn:event:momentus:" + mevent.Id;
            eventlinked.Shortname = mevent.Name;
            eventlinked.Source = "momentus";
            eventlinked.Active = mevent.IsActive && !mevent.IsCanceled;
            eventlinked.LastChange = DateTime.Now;
            eventlinked.FirstImport = firstimport ?? DateTime.Now;

            // Date range: start from event-level dates, then refine from non-private booked spaces
            eventlinked.DateBegin = mevent.Start?.ToDateTime(TimeOnly.MinValue);
            eventlinked.DateEnd = mevent.End?.ToDateTime(TimeOnly.MinValue);

            // Multilingual detail: titles/subtitles from named functions, description from event
            BuildDetailFromFunctions(eventlinked, functionlist, mevent.Description);

            // Venue reference
            if (venuelinked?.Id != null)
                eventlinked.VenueIds = [venuelinked.Id];

            // EventDates from function list (one entry per day, rooms resolved via venue mapping)
            eventlinked.EventDate = BuildEventDates(functionlist, venuelinked);

            // Recalculate root DateBegin/DateEnd from non-private booked spaces
            var (nonPrivateBegin, nonPrivateEnd) = ComputeDateRangeFromNonPrivateSpaces(mevent);
            if (nonPrivateBegin != null) eventlinked.DateBegin = nonPrivateBegin;
            if (nonPrivateEnd != null) eventlinked.DateEnd = nonPrivateEnd;

            // ContactInfos from first contact role
            if (mevent.ContactRoles != null && mevent.ContactRoles.Count > 0)
            {
                var contact = BuildContactInfo(mevent.ContactRoles.First());
                eventlinked.ContactInfos = new Dictionary<string, ContactInfos>() { { "en", contact } };
            }

            // OrganizerInfos from account name
            if (!string.IsNullOrEmpty(mevent.AccountName))
            {
                eventlinked.OrganizerInfos = new Dictionary<string, ContactInfos>()
                {
                    { "en", new ContactInfos() { Language = "en", CompanyName = mevent.AccountName } }
                };
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

            // PublishedOn is derived from UsageType and venue organization
            eventlinked.PublishedOn = DeterminePublishedOn(mevent);

            // Restore preserved fields
            eventlinked.ImageGallery = imagegallery;
            eventlinked.Documents = documents;
            eventlinked.VideoItems = videoitems;

            // Merge event-location tag from venue into TagIds, preserving existing tags
            var locationTags = new[] { "eventlocation:noi", "eventlocation:eurac" };
            var venueLocationTag = venuelinked?.TagIds?.FirstOrDefault(t => locationTags.Contains(t));

            tagids ??= [];
            if (venueLocationTag != null && !tagids.Contains(venueLocationTag))
                tagids.Add(venueLocationTag);

            eventlinked.TagIds = tagids.Count > 0 ? tagids : null;

            return eventlinked;
        }

        private static List<string> DeterminePublishedOn(MomentusEvent mevent)
        {
            var usageType = mevent.BookedSpaces?
                .Where(b => !string.IsNullOrEmpty(b.UsageType))
                .Select(b => b.UsageType)
                .FirstOrDefault()
                ?.Trim().ToUpperInvariant();

            if (usageType == null || usageType == "PRIVATE")
                return [];

            bool isEurac = mevent.VenueNames != null &&
                mevent.VenueNames.Any(v => v.Contains("Eurac", StringComparison.OrdinalIgnoreCase));

            return usageType switch
            {
                "PUBLIC" when isEurac => ["eurac-videowall", "eurac-seminarroom"],
                "PUBLIC"              => ["noi-totem", "today.noi.bz.it"],
                var u when u.Contains("VIDEOWALL") && isEurac  => ["eurac-videowall"],
                var u when u.Contains("VIDEOWALL")             => ["today.noi.bz.it"],
                var u when u.Contains("ROOM") && isEurac       => ["eurac-seminarroom"],
                var u when u.Contains("ROOM")                  => ["noi-totem"],
                _ => []
            };
        }

        private static (DateTime? begin, DateTime? end) ComputeDateRangeFromNonPrivateSpaces(MomentusEvent mevent)
        {
            var spaces = mevent.BookedSpaces?
                .Where(b => b.StartDate != null &&
                            !string.Equals(b.UsageType, "PRIVATE", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (spaces == null || spaces.Count == 0)
                return (null, null);

            var starts = spaces
                .Select(b =>
                {
                    var date = b.StartDate!.Value.ToDateTime(TimeOnly.MinValue);
                    return TimeSpan.TryParse(b.StartTime, out var t) ? date + t : date;
                })
                .ToList();

            var ends = spaces
                .Where(b => b.EndDate != null)
                .Select(b =>
                {
                    var date = b.EndDate!.Value.ToDateTime(TimeOnly.MinValue);
                    return TimeSpan.TryParse(b.EndTime, out var t) ? date + t : date;
                })
                .ToList();

            return (starts.Count > 0 ? starts.Min() : null,
                    ends.Count > 0   ? ends.Max()   : null);
        }

        private static void BuildDetailFromFunctions(EventLinked eventlinked, IEnumerable<MomentusFunction> functionlist, string? description)
        {
            foreach (var lang in new[] { "de", "it", "en" })
            {
                if (!eventlinked.Detail.ContainsKey(lang))
                    eventlinked.Detail[lang] = new Detail() { Language = lang };

                if (!string.IsNullOrEmpty(description))
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

        private static List<EventDate> BuildEventDates(IEnumerable<MomentusFunction> functionlist, VenueV2? venuelinked)
        {
            var eventdates = new List<EventDate>();

            if (functionlist == null)
                return eventdates;

            var functions = functionlist.Where(f => f.StartDate != null).ToList();
            if (functions.Count == 0)
                return eventdates;

            foreach (var daygroup in functions.GroupBy(f => f.StartDate!.Value))
            {
                var eventdate = new EventDate
                {
                    From = daygroup.Key.ToDateTime(TimeOnly.MinValue),
                    To = daygroup.Key.ToDateTime(TimeOnly.MinValue),
                    Active = true
                };

                var starts = daygroup
                    .Where(f => !string.IsNullOrEmpty(f.StartTime))
                    .Select(f => TimeSpan.TryParse(f.StartTime, out var ts) ? ts : (TimeSpan?)null)
                    .Where(ts => ts != null).Select(ts => ts!.Value).ToList();

                var ends = daygroup
                    .Where(f => !string.IsNullOrEmpty(f.EndTime))
                    .Select(f => TimeSpan.TryParse(f.EndTime, out var ts) ? ts : (TimeSpan?)null)
                    .Where(ts => ts != null).Select(ts => ts!.Value).ToList();

                if (starts.Count > 0) eventdate.Begin = starts.Min();
                if (ends.Count > 0) eventdate.End = ends.Max();

                if (venuelinked?.RoomDetails != null)
                {
                    var roomIds = daygroup
                        .Where(f => f.RoomId != null)
                        .Select(f => f.RoomId!)
                        .Distinct()
                        .Select(rid => venuelinked.RoomDetails.FirstOrDefault(r =>
                            r.Mapping != null &&
                            r.Mapping.ContainsKey("momentus") &&
                            r.Mapping["momentus"].ContainsKey("id") &&
                            r.Mapping["momentus"]["id"] == rid))
                        .Where(r => r?.Id != null)
                        .Select(r => r!.Id!)
                        .ToList();

                    if (roomIds.Count > 0)
                        eventdate.VenueRoomDetailsIds = roomIds;
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
    }
}
