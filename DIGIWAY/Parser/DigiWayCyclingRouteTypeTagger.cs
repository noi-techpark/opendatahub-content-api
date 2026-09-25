// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace DIGIWAY
{
    /// <summary>
    /// Assigns the TagIds of the Tyrol cycling routes (civis.geoserver cyclewaystyrol, dservices3.arcgis.com radrouten_tirol)
    /// based on their route type. Used by the importers and by the DataModify cleanup, so both give the same result.
    /// </summary>
    public static class DigiWayCyclingRouteTypeTagger
    {
        //Route types which are mountain bike routes
        private static readonly string[] mountainbikeroutetypes = new[] { "Single Trail", "Mountainbikestrecke" };

        //Route types which are cycling tours
        private static readonly string[] bikingtourroutetypes = new[] { "Radroute", "Themenradweg" };

        /// <summary>
        /// Single Trail / Mountainbikestrecke: add "mountain bike", remove "cycling"
        /// Radroute / Themenradweg: add "biking biking tours" if missing
        /// Any other route type: TagIds are not touched
        /// </summary>
        /// <returns>true if the TagIds were changed</returns>
        public static bool AssignRouteTypeTags(ICollection<string> tagids, string? routetype)
        {
            if (tagids == null || String.IsNullOrWhiteSpace(routetype))
                return false;

            var type = routetype.Trim();
            bool changed = false;

            if (mountainbikeroutetypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            {
                if (!tagids.Contains("mountain bike"))
                {
                    tagids.Add("mountain bike");
                    changed = true;
                }

                while (tagids.Remove("cycling"))
                    changed = true;
            }
            else if (bikingtourroutetypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            {
                if (!tagids.Contains("biking biking tours"))
                {
                    tagids.Add("biking biking tours");
                    changed = true;
                }
            }

            return changed;
        }
    }
}
