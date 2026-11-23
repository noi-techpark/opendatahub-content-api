// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper.Tagging;

namespace HelperTests
{
    public class AccommodationIdmTagHelperTests
    {
        private static IDictionary<string, string> Lookup() =>
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Type|HotelPension", "accommodationtypes:hotel-boardinghouse" },
                { "Type|Farm", "accommodationtypes:farm-holiday" },
                { "Badge|Familienhotel", "accommodationbadge:family-hotel" },
                { "Badge|Bauernhof", "accommodationbadge:red-rooster-farm" },
                { "Theme|In der Höhe", "accommodationtheme:at-altitude" },
                { "Theme|Wandern", "accommodationtheme:hiking" },
                { "SpecialFeature|Sauna", "accommodationspecialfeature:sauna" },
            };

        [Fact]
        public void AssignTags_AddsTagsAndKeepsOtherTagIds()
        {
            var acco = new AccommodationV2()
            {
                AccoTypeId = "HotelPension",
                BadgeIds = new List<string>() { "Familienhotel" },
                ThemeIds = new List<string>() { "In der Höhe", "Wandern" },
                SpecialFeaturesIds = new List<string>() { "Sauna", "Guestcard" },
                TagIds = new List<string>() { "5228203A51CA11D18F1400A02427D15E", "guestcard" },
            };

            AccommodationIdmTagHelper.AssignTags(acco, Lookup());

            Assert.Equal(
                new[]
                {
                    "5228203A51CA11D18F1400A02427D15E",
                    "accommodationbadge:family-hotel",
                    "accommodationspecialfeature:sauna",
                    "accommodationtheme:at-altitude",
                    "accommodationtheme:hiking",
                    "accommodationtypes:hotel-boardinghouse",
                    "guestcard",
                },
                acco.TagIds!.OrderBy(x => x, StringComparer.Ordinal)
            );
        }

        [Fact]
        public void AssignTags_RemovesNoLongerValidIdmTags()
        {
            var acco = new AccommodationV2()
            {
                AccoTypeId = "Farm",
                BadgeIds = new List<string>(),
                TagIds = new List<string>()
                {
                    "accommodationtypes:hotel-boardinghouse",
                    "accommodationbadge:family-hotel",
                    "guestcard",
                },
            };

            AccommodationIdmTagHelper.AssignTags(acco, Lookup());

            Assert.Equal(
                new[] { "accommodationtypes:farm-holiday", "guestcard" },
                acco.TagIds!.OrderBy(x => x, StringComparer.Ordinal)
            );
        }

        [Fact]
        public void AssignTags_UnknownKeysAndCaseInsensitiveMatch()
        {
            var acco = new AccommodationV2()
            {
                AccoTypeId = "hotelpension",
                ThemeIds = new List<string>() { "Unknown Theme" },
                TagIds = null,
            };

            AccommodationIdmTagHelper.AssignTags(acco, Lookup());

            Assert.Equal(new[] { "accommodationtypes:hotel-boardinghouse" }, acco.TagIds);
        }

        [Fact]
        public void AssignTags_NothingToAssign_TagIdsStayNull()
        {
            var acco = new AccommodationV2() { AccoTypeId = null, TagIds = null };

            AccommodationIdmTagHelper.AssignTags(acco, Lookup());

            Assert.Null(acco.TagIds);
        }

        [Fact]
        public void AssignTags_EmptyLookup_DoesNotTouchTagIds()
        {
            var acco = new AccommodationV2()
            {
                AccoTypeId = "HotelPension",
                TagIds = new List<string>() { "accommodationtypes:hotel-boardinghouse", "guestcard" },
            };

            AccommodationIdmTagHelper.AssignTags(acco, new Dictionary<string, string>());

            Assert.Equal(new[] { "accommodationtypes:hotel-boardinghouse", "guestcard" }, acco.TagIds);
        }
    }
}
