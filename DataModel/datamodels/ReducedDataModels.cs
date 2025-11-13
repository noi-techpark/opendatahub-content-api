// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModel
{
    #region ReducedClasses

    //public class LTSActivityLinkedReduced : LTSActivityLinked { }

    //public class LTSPoiLinkedReduced : LTSPoiLinked { }

    public class LTSODHActivityPoiReduced : ODHActivityPoiLinked { }

    public class AccommodationLinkedReduced : AccommodationV2 { }

    public class EventLinkedReduced : EventLinked { }

    //public class GastronomyLinkedReduced : GastronomyLinked { }

    public class MeasuringpointLinkedReduced : MeasuringpointLinked { }

    public class WebcamInfoLinkedReduced : WebcamInfoLinked { }

    public class DDVenueReduced : DDVenue { }

    public class VenueReduced : VenueLinked { }

    #endregion
}
