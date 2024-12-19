﻿// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper.Generic
{
    //Remove because of the hardcoded stuff, instead doing a filter on both syncsourceinterface + source
    //public static class SourceFilterHelper
    //{
    //    public static List<string> ExtendSourceFilterODHActivityPois(List<string> sourcelist)
    //    {
    //        List<string> sourcelistnew = new();

    //        foreach (var source in sourcelist)
    //        {
    //            sourcelistnew.Add(source);

    //            if (source == "idm")
    //            {
    //                if (!sourcelistnew.Contains("none"))
    //                    sourcelistnew.Add("none");
    //                if (!sourcelistnew.Contains("magnolia"))
    //                    sourcelistnew.Add("magnolia");
    //                if (!sourcelistnew.Contains("common"))
    //                    sourcelistnew.Add("common");
    //            }
    //            else if (source == "lts")
    //            {
    //                if (!sourcelistnew.Contains("activitydata"))
    //                    sourcelistnew.Add("activitydata");
    //                if (!sourcelistnew.Contains("poidata"))
    //                    sourcelistnew.Add("poidata");
    //                if (!sourcelistnew.Contains("beacondata"))
    //                    sourcelistnew.Add("beacondata");
    //                if (!sourcelistnew.Contains("gastronomicdata"))
    //                    sourcelistnew.Add("gastronomicdata");
    //                if (!sourcelistnew.Contains("beacondata"))
    //                    sourcelistnew.Add("beacondata");
    //            }
    //            else if (source == "siag")
    //            {
    //                if (!sourcelistnew.Contains("museumdata"))
    //                    sourcelistnew.Add("museumdata");
    //            }
    //            else if (source == "dss")
    //            {
    //                if (!sourcelistnew.Contains("dssliftbase"))
    //                    sourcelistnew.Add("dssliftbase");
    //                if (!sourcelistnew.Contains("dssslopebase"))
    //                    sourcelistnew.Add("dssslopebase");
    //            }
    //            else if (source == "content")
    //            {
    //                if (!sourcelistnew.Contains("none"))
    //                    sourcelistnew.Add("none");
    //            }
    //            else if (source == "a22")
    //            {
    //                if (!sourcelistnew.Contains("tollstation"))
    //                    sourcelistnew.Add("tollstation");
    //                if (!sourcelistnew.Contains("servicearea"))
    //                    sourcelistnew.Add("servicearea");
    //            }
    //            else if (source == "iit")
    //            {
    //                if (!sourcelistnew.Contains("h2 center"))
    //                    sourcelistnew.Add("h2 center");
    //            }
    //            else if (source == "alperia")
    //            {
    //                if (!sourcelistnew.Contains("neogy"))
    //                    sourcelistnew.Add("neogy");
    //            }
    //            else if (source == "echargingspreadsheet")
    //            {
    //                if (!sourcelistnew.Contains("ecogy gmbh"))
    //                    sourcelistnew.Add("ecogy gmbh");
    //                if (!sourcelistnew.Contains("leitner energy"))
    //                    sourcelistnew.Add("leitner energy");
    //                if (!sourcelistnew.Contains("officina elettrica san vigilio di marebbe spa"))
    //                    sourcelistnew.Add("officina elettrica san vigilio di marebbe spa");
    //                if (!sourcelistnew.Contains("ötzi genossenschaft"))
    //                    sourcelistnew.Add("ötzi genossenschaft");
    //                if (!sourcelistnew.Contains("pension erlacher"))
    //                    sourcelistnew.Add("pension erlacher");
    //                if (!sourcelistnew.Contains("vek"))
    //                    sourcelistnew.Add("vek");
    //            }
    //        }

    //        return sourcelistnew;
    //    }
    //}
}
