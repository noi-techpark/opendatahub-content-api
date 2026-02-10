// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper.Generic
{
    public static class PathStringExtensions
    {
        public static string GetPathNextTo2(
            this PathString pathString,
            string splitter,
            string nextto
        )
        {
            if (pathString != null && pathString.HasValue)
            {
                var splitted = pathString.Value.Split(splitter);
                bool next = false;

                foreach (var item in splitted)
                {
                    if (next)
                        return item;

                    if (item == nextto)
                        next = true;
                }
            }

            return "";
        }

        public static string GetPathNextToCombinedRoutes(
            this PathString pathString,
            string splitter,
            string nextto
        )
        {
            if (pathString != null && pathString.HasValue)
            {               
                var splitted = pathString.Value.Split(splitter);
                bool next = false;

                foreach (var item in splitted)
                {
                    if (next)
                    {
                        //Hack TO Optimize
                        if (item == "Weather")
                            return "Weather/Measuringpoint";

                        return item;
                    }
                        

                    if (item == nextto)
                        next = true;
                }
            }

            return "";
        }
    }
}
