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
        public void AssignTags_ObsoleteAndUnknownKeysAreNotAssigned()
        {
            var acco = new AccommodationV2()
            {
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
            Assert.Null(AccommodationIdmTagMapping.GetTagId("Board", "Halbpension"));
            Assert.Null(AccommodationIdmTagMapping.GetTagId("SpecialFeature", "Guestcard"));
        }

        [Fact]
        public void TagKeys_EnglishKeysAreUnique()
        {
            var tagids = AccommodationIdmTagMapping.TagKeys
                .Where(x => x.Value != null)
                .Select(x => AccommodationIdmTagMapping.GetTagId(x.Key.Split('|')[0], x.Key.Split('|')[1]))
                .ToList();

            Assert.Equal(45, tagids.Count);
            Assert.Equal(tagids.Count, tagids.Distinct().Count());
        }
    }
}
