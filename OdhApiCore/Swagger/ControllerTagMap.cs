// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;

namespace OdhApiCore.Swagger
{
    public static class ControllerTagMap
    {
        private static readonly Dictionary<string, string> Map = new()
        {
            // Main endpoints
            { "MetaData",           "MetaData" },
            { "Accommodation",      "Accommodation" },
            { "Announcement",       "Announcement" },
            { "Article",            "Article" },
            { "Common",             "Common" },
            { "Event",              "Event" },
            { "ODHActivityPoi",     "ODHActivityPoi" },
            { "Publisher",          "Publisher" },
            { "Source",             "Source" },
            { "SpatialData",        "SpatialData" },
            { "Tag",                "Tag" },
            { "Trip",               "Trip" },
            { "UrbanGreen",         "UrbanGreen" },
            { "Venue",              "Venue" },
            { "Weather",            "Weather" },
            { "WebcamInfo",         "WebcamInfo" },
            // Secondary
            { "Search",             "Search" },
            { "Distinct",           "Distinct" },
            { "Deprecated",         "Deprecated" },
            { "PushResponse",       "PushResponse" },
            { "Geo",                "Geo" },
            { "GeoConverter",       "GeoConverter" },
            { "Location",           "Location" },
            { "FileUpload",         "FileUpload" },
            { "JsonLD",             "JsonLD" },
            // Obsolete
            { "EventShort",         "EventShort" },
            { "ODHTag",             "ODHTag" },
        };

        public static string GetTag(string controllerName) =>
            Map.TryGetValue(controllerName, out var tag) ? tag : controllerName;
    }
}
