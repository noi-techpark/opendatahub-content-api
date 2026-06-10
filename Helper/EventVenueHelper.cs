// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Newtonsoft.Json;
using NGuid;
using SqlKata.Execution;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Helper
{
    public static class EventVenueHelper
    {
        private static readonly Regex ConsecutiveDashes = new("-{2,}");

        public static string SlugifyRoomName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            name = name
                .Replace("ä", "ae").Replace("Ä", "ae")
                .Replace("ö", "oe").Replace("Ö", "oe")
                .Replace("ü", "ue").Replace("Ü", "ue")
                .Replace("ß", "ss");

            name = name
                .Replace("&", "-")
                .Replace("/", "-")
                .Replace("→", "-")
                .Replace(",", "")
                .Replace(" ", "-");

            return ConsecutiveDashes.Replace(name, "-").Trim('-').ToLower();
        }

        public static async Task AddVenueSourceToTagIds(this EventLinked odhevent, QueryFactory queryFactory)
        {
            if (odhevent.VenueIds == null || odhevent.VenueIds.Count == 0)
                return;

            var venueId = odhevent.VenueIds.First();

            var venueRaw = await queryFactory
                .Query("venues")
                .Select("data")
                .Where("id", "ILIKE", venueId)
                .FirstOrDefaultAsync<JsonRaw?>();

            if (venueRaw == null)
                return;

            var venue = JsonConvert.DeserializeObject<VenueV2>(venueRaw.Value);

            if (venue == null)
                return;

            if (venue.Mapping == null ||
                !venue.Mapping.TryGetValue("tag", out var tagMapping) ||
                !tagMapping.TryGetValue("eventlocation", out var eventLocationValue) ||
                string.IsNullOrEmpty(eventLocationValue))
                return;

            if (odhevent.TagIds != null)
                odhevent.TagIds = odhevent.TagIds.Where(t => t != eventLocationValue).ToList();

            odhevent.TagIds ??= [];
            if (!odhevent.TagIds.Contains(eventLocationValue))
                odhevent.TagIds = odhevent.TagIds.Append(eventLocationValue).ToList();
        }

        public static void GenerateRoomDetailIds(this VenueV2 venue)
        {
            if (venue.RoomDetails == null)
                return;

            foreach (var room in venue.RoomDetails)
            {
                if (!string.IsNullOrEmpty(room.Id))
                    continue;

                var slug = SlugifyRoomName(room.Shortname);
                if (string.IsNullOrEmpty(slug))
                    continue;

                room.Id = "urn:venueroomid:"
                    + (venue.Source ?? "unknown")
                    + ":"
                    + GuidHelpers.CreateFromName(Guid.Empty, slug);
            }
        }
    }
}
