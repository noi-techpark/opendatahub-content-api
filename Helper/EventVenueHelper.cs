// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using NGuid;
using System;
using System.Text.RegularExpressions;

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
