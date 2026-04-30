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
        public static EventLinked ParseMomentusEvent(MomentusEvent mevent, IEnumerable<MomentusFunction> functionlist, EventLinked? eventlinked, VenueV2 venuelinked) 
        {
            if(eventlinked == null)
                eventlinked = new EventLinked();

            //check what data to preserve


            eventlinked.Id = "urn:event:momentus:" + mevent.Id;
            eventlinked.Shortname = mevent.Name;

            //WRITE THE PARSERS LOGIC HERE



            return eventlinked;
        }
    }
}
