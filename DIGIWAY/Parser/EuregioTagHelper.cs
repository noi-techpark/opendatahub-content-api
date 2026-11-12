// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DIGIWAY.Model.GeoJsonReadModel;
using System.Globalization;

namespace DIGIWAY
{
    /// <summary>
    /// Assigns TagIds to EUREGIO features (FC2 euregio.routes, FC1 euregio.roadnetwork) based on their attributes.
    /// Existing ODH tags are referenced by their idm key, tags not present in the ODH Tag endpoint are marked as new.
    /// </summary>
    public static class EuregioTagHelper
    {
        //Existing ODH tags (idm keys)
        public const string Hiking = "hiking";
        public const string Cycling = "cycling";
        public const string MountainBike = "mountain bike";
        public const string SingletrailFreeride = "singletrail freeride";
        public const string LongDistanceHikingPaths = "longdistance hiking paths";
        public const string ViaFerrata = "vie ferrate";
        public const string AlpineRidgeways = "alpine ridgeways";
        public const string Waalwege = "waalwege";
        public const string Lifts = "lifts";

        //New tags (not present in the ODH Tag endpoint)
        public const string SouthTyrol = "south tyrol";
        public const string Tyrol = "tyrol";
        public const string Trentino = "trentino";
        public const string ForestryAccessRoad = "forestry access road";
        public const string TrailClosure = "trail closure";
        public const string CaiDifficultyPrefix = "cai difficulty "; // + t, e, ee, eea

        public static List<string> GetTags(GeoJsonFeature digiwaydata, string identifier)
        {
            var tags = new List<string>() { identifier };

            switch (identifier)
            {
                case "euregio.routes":
                    AddRoutesTags(digiwaydata, tags);
                    break;
                case "euregio.roadnetwork":
                    AddRoadNetworkTags(digiwaydata, tags);
                    break;
            }

            return tags.Distinct().ToList();
        }

        //FC2 Hiking Paths
        private static void AddRoutesTags(GeoJsonFeature digiwaydata, List<string> tags)
        {
            //All features are TYPE_CODE 013 Hiking Route
            tags.Add(Hiking);

            var region = GetAttribute(digiwaydata, "RESPORGDIGIWAY_CODE");
            if (region == "ST")
                tags.Add(SouthTyrol);
            else if (region == "TN")
                tags.Add(Trentino);

            //E5 European long-distance path (in Trentino codes like E5xx are SAT trail numbers, therefore only South Tyrol and exact match)
            if (region == "ST" && GetAttribute(digiwaydata, "PATH_CODE") == "E5")
                tags.Add(LongDistanceHikingPaths);

            //CAI difficulty, e.g. "T - Turistico (CAI)", "EE - Experienced hikers (CAI)", "EEA_PD - ...", "EEA _D - ..."
            var difficulty = GetAttribute(digiwaydata, "DIFFCLASS_E");
            if (!string.IsNullOrWhiteSpace(difficulty))
            {
                var code = difficulty.Trim().Split(' ', '_')[0].ToLower();
                if (code is "t" or "e" or "ee" or "eea")
                    tags.Add(CaiDifficultyPrefix + code);
                if (code == "eea")
                    tags.Add(ViaFerrata);
            }

            var pathname = GetAttribute(digiwaydata, "PATH_D");
            if (!string.IsNullOrWhiteSpace(pathname))
            {
                if (pathname.Contains("höhenweg", StringComparison.OrdinalIgnoreCase))
                    tags.Add(AlpineRidgeways);
                if (pathname.Contains("waalweg", StringComparison.OrdinalIgnoreCase))
                    tags.Add(Waalwege);
            }
        }

        //FC1 Road Network
        private static void AddRoadNetworkTags(GeoJsonFeature digiwaydata, List<string> tags)
        {
            //No region attribute in FC1, derived from the municipality code
            var municipalitycode = GetAttribute(digiwaydata, "MUNICIPALITY_CODE") ?? "";
            if (municipalitycode.StartsWith("AT"))
                tags.Add(Tyrol);
            else if (municipalitycode.StartsWith("IT-21"))
                tags.Add(SouthTyrol);
            else if (municipalitycode.StartsWith("IT-022"))
                tags.Add(Trentino);

            //DRAWCLASS is always filled and the most reliable classification
            switch (GetAttribute(digiwaydata, "DRAWCLASS_E"))
            {
                case "Hiking trail":
                    tags.Add(Hiking);
                    break;
                case "Bicycle/pedestrian path":
                    tags.Add(Cycling);
                    break;
                case "Mountainbike path":
                    tags.Add(MountainBike);
                    break;
                case "Lift system":
                    tags.Add(Lifts);
                    break;
                case "Forestry access road":
                    tags.Add(ForestryAccessRoad);
                    break;
            }

            switch (GetAttribute(digiwaydata, "CATEGORY_CODE"))
            {
                case "RW": //Bicycle Path
                    tags.Add(Cycling);
                    break;
                case "M": //Mountain Bike Trail
                    tags.Add(MountainBike);
                    break;
                case "WA": //Hiking Trail
                    tags.Add(Hiking);
                    break;
            }

            //FUNCTIONALROADCLASS 010 (Bicycle/pedestrian path) is not used, it is also set on many Tyrol hiking trails
            switch (GetAttribute(digiwaydata, "FUNCTIONALROADCLASS_CODE"))
            {
                case "200": //Single trail (MTB)
                    tags.Add(MountainBike);
                    tags.Add(SingletrailFreeride);
                    break;
                case "201": //Shared trail
                    tags.Add(MountainBike);
                    break;
            }

            if (HasActiveTrailClosure(digiwaydata))
                tags.Add(TrailClosure);
        }

        //Closure is present and not yet ended (tags are recalculated on every import)
        private static bool HasActiveTrailClosure(GeoJsonFeature digiwaydata)
        {
            if (
                string.IsNullOrWhiteSpace(GetAttribute(digiwaydata, "TRAILCLOSURE_DESCRIPTION")) &&
                string.IsNullOrWhiteSpace(GetAttribute(digiwaydata, "TRAILCLOSURE_STARTDATE"))
            )
                return false;

            var enddate = GetAttribute(digiwaydata, "TRAILCLOSURE_ENDDATE");

            return enddate == null ||
                !DateTime.TryParse(enddate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end) ||
                end >= DateTime.Now;
        }

        internal static string? GetAttribute(GeoJsonFeature digiwaydata, string key) =>
            digiwaydata.Attributes.TryGetValue(key, out var value) && value != null
                ? value.ToString()?.Trim()
                : null;
    }
}
