// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Newtonsoft.Json;
using SqlKata.Execution;
using System.Linq;
using System.Threading.Tasks;

namespace Helper
{
    public static class EventVenueGpsHelper
    {
        /// <summary>
        /// If the Event has no GpsInfo and a Venue is assigned, loads that Venue and takes its GpsInfo,
        /// so the Event ends up with a usable location (e.g. for LocationInfo/DistanceInfo calculation)
        /// even when the client didn't send GPS coordinates itself.
        /// </summary>
        public static async Task AssignVenueGpsIfMissing(
            this EventLinked odhevent,
            QueryFactory queryFactory
        )
        {
            if (odhevent.GpsInfo != null && odhevent.GpsInfo.Count > 0)
                return;

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

            if (venue?.GpsInfo == null || venue.GpsInfo.Count == 0)
                return;

            odhevent.GpsInfo = venue.GpsInfo;
        }
    }
}
