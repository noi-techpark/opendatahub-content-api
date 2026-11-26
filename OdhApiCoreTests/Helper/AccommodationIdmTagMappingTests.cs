// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using LTSAPI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OdhApiCoreTests.Helper
{
    public class AccommodationIdmTagMappingTests
    {
        [Fact]
        public void AssignTags_AddsTagsAndKeepsOtherTagIds()
        {
            var acco = new AccommodationV2()
            {
                AccoTypeId = "HotelPension",
                AccoCategoryId = "3sstars",
                BoardIds = new List<string>() { "breakfast", "half board" },
                BadgeIds = new List<string>() { "Familienhotel", "Südtirol Privat" },
                ThemeIds = new List<string>() { "In der Höhe", "Wandern" },
                SpecialFeaturesIds = new List<string>() { "Sauna", "Guestcard" },
                TagIds = new List<string>() { "5228203A51CA11D18F1400A02427D15E", "guestcard" },
            };

            AccommodationIdmTagMapping.AssignTags(acco);

            Assert.Equal(
                new[]
                {
                    "5228203A51CA11D18F1400A02427D15E",
                    "accommodationbadge:family-hotel",
                    "accommodationbadge:private-rooms",
                    "accommodationcategory:3s-stars",
                    "accommodationmealplans:breakfast",
                    "accommodationmealplans:half-board",
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
                AccoCategoryId = "Not categorized",
                TagIds = new List<string>()
                {
                    "accommodationtypes:hotel-boardinghouse",
                    "accommodationcategory:3-stars",
                    "accommodationbadge:family-hotel",
                    "52281FAA51CA11D18F1400A02427D15E",
                    "guestcard",
                },
            };

            AccommodationIdmTagMapping.AssignTags(acco);

            Assert.Equal(
                new[]
                {
                    "52281FAA51CA11D18F1400A02427D15E",
                    "accommodationcategory:not-categorized",
                    "accommodationtypes:farm-holiday",
                    "guestcard",
                },
                acco.TagIds!.OrderBy(x => x, StringComparer.Ordinal)
            );
        }

        [Fact]
        public void AssignTags_DiscoverSwiss()
        {
            //discoverswiss sets AccoTypeId and AccoCategoryId (also SwissLodge) and puts the raw values to TagIds
            var acco = new AccommodationV2()
            {
                AccoTypeId = "SwissLodge",
                AccoCategoryId = "4stars",
                TagIds = new List<string>() { "SwissLodge", "4stars" },
            };

            AccommodationIdmTagMapping.AssignTags(acco);

            Assert.Equal(
                new[] { "4stars", "SwissLodge", "accommodationcategory:4-stars", "accommodationtypes:swiss-lodge" },
                acco.TagIds!.OrderBy(x => x, StringComparer.Ordinal)
            );
        }

        [Fact]
        public void AssignTags_ObsoleteAndUnknownKeysAreNotAssigned()
        {
            var acco = new AccommodationV2()
            {
                BoardIds = new List<string>() { "all boards" },
                ThemeIds = new List<string>() { "Bonus Vacanze", "Alle", "Unknown Theme" },
                SpecialFeaturesIds = new List<string>() { "Ruhig gelegen" },
                TagIds = null,
            };

            AccommodationIdmTagMapping.AssignTags(acco);

            Assert.Null(acco.TagIds);
        }

        [Fact]
        public void AssignTags_NoDuplicates()
        {
            var acco = new AccommodationV2()
            {
                AccoTypeId = "Farm",
                TagIds = new List<string>() { "accommodationtypes:farm-holiday" },
            };

            AccommodationIdmTagMapping.AssignTags(acco);

            Assert.Equal(new[] { "accommodationtypes:farm-holiday" }, acco.TagIds);
        }

        [Fact]
        public void GetTagId_ReturnsPrefixedEnglishId()
        {
            Assert.Equal("accommodationtypes:bed-and-breakfast", AccommodationIdmTagMapping.GetTagId("Type", "BedBreakfast"));
            Assert.Equal("accommodationbadge:sustainability-level-2", AccommodationIdmTagMapping.GetTagId("Badge", "SustainabilityLevel2"));
            Assert.Equal("accommodationmealplans:all-inclusive", AccommodationIdmTagMapping.GetTagId("Board", "All Inclusive"));
            Assert.Equal("accommodationcategory:not-categorized", AccommodationIdmTagMapping.GetTagId("Category", "Not categorized"));
            Assert.Null(AccommodationIdmTagMapping.GetTagId("Board", "all boards"));
            Assert.Null(AccommodationIdmTagMapping.GetTagId("SpecialFeature", "Guestcard"));
        }

        [Fact]
        public void IsIdmTagId_OnlyPrefixedIds()
        {
            Assert.True(AccommodationIdmTagMapping.IsIdmTagId("accommodationcategory:3-stars"));
            Assert.False(AccommodationIdmTagMapping.IsIdmTagId("52281FAA51CA11D18F1400A02427D15E"));
            Assert.False(AccommodationIdmTagMapping.IsIdmTagId("guestcard"));
            Assert.False(AccommodationIdmTagMapping.IsIdmTagId(null));
        }

        [Fact]
        public void TagKeys_EnglishKeysAreUnique()
        {
            var tagids = AccommodationIdmTagMapping.TagKeys
                .Where(x => x.Value != null)
                .Select(x => AccommodationIdmTagMapping.GetTagId(x.Key.Split('|')[0], x.Key.Split('|')[1]))
                .ToList();

            //Type 9, Category 18, Board 5, Badge 10, Theme 17, SpecialFeature 10
            Assert.Equal(69, tagids.Count);
            Assert.Equal(tagids.Count, tagids.Distinct().Count());
        }
    }
}
