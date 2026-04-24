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
        public static EventLinked ParseMomentusEvent(MomentusEvent mevent) 
        {
            EventLinked eventLinked = new EventLinked();

            eventLinked.Id = "urn:event:momentus:" + mevent.Id;
            eventLinked.Shortname = mevent.Name;


            return eventLinked;
        }

        //To check if one room for noi with subrooms is returned
        public static VenueV2 ParseMomentusRoom(MomentusRoom mroom)
        {
            //This is always assigned
            //"venueId": "venue-1-A",
            //"venueName": "Eurac Research",



            // "group": "NOI TECHPARK"
            // "group": "EURAC RESEARCH HQ"



            VenueV2 venue = new VenueV2();

            return venue;
        }

        public static IEnumerable<EventLinked> ParseMomentusEvents(IEnumerable<MomentusEvent> mevents)
        {
            return mevents.Select(x => ParseMomentusEvent(x)).ToList();
        }

        public static IEnumerable<VenueV2> ParseMomentusRooms(IEnumerable<MomentusRoom> mrooms)
        {
            return mrooms.Select(x => ParseMomentusRoom(x)).ToList();
        }
    }
}
