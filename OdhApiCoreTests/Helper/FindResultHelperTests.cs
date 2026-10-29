// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using DataModel;
using Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdhApiCore.Responses;
using Xunit;

namespace OdhApiCoreTests.Helper
{
    public class FindResultHelperTests
    {
        private static readonly JToken accommodation = JToken.Parse(
            @"{
                ""Id"": ""ACCO1"",
                ""Shortname"": ""Hotel Shortname"",
                ""Active"": true,
                ""AccoDetail"": {
                    ""de"": { ""Name"": ""Hotel Bozen"" },
                    ""en"": { ""Name"": ""Hotel Bolzano"" }
                },
                ""_Meta"": { ""Type"": ""accommodation"" },
                ""Self"": ""https://api.example/v1/Accommodation/ACCO1""
            }"
        );

        private static readonly JToken poi = JToken.Parse(
            @"{
                ""Id"": ""POI1"",
                ""Shortname"": ""Poi Shortname"",
                ""Detail"": {
                    ""de"": { ""Title"": ""Wanderweg"" },
                    ""en"": { ""Title"": """" }
                },
                ""_Meta"": { ""Type"": ""odhactivitypoi"" },
                ""Self"": ""https://api.example/v1/ODHActivityPoi/POI1""
            }"
        );

        [Fact]
        public void ToFindResult_HasOnlySimpleKeys()
        {
            var result = FindResultHelper.ToFindResult(
                accommodation,
                "accommodation",
                "en",
                Array.Empty<string>(),
                false
            );

            var json = JObject.FromObject(result);

            Assert.Equal(
                new[] { "Id", "Name", "NameField", "Type", "Self" },
                json.Properties().Select(x => x.Name).ToArray()
            );
            Assert.Equal("ACCO1", result.Id);
            Assert.Equal("Hotel Bolzano", result.Name);
            Assert.Equal("AccoDetail.en.Name", result.NameField);
            Assert.Equal("accommodation", result.Type);
            Assert.Equal("https://api.example/v1/Accommodation/ACCO1", result.Self);
        }

        [Fact]
        public void ToFindResult_NameInRequestedLanguage()
        {
            var result = FindResultHelper.ToFindResult(
                accommodation,
                "accommodation",
                "de",
                Array.Empty<string>(),
                false
            );

            Assert.Equal("Hotel Bozen", result.Name);
            Assert.Equal("AccoDetail.de.Name", result.NameField);
        }

        [Fact]
        public void ToFindResult_NameFallsBackToOtherLanguage()
        {
            //en Title is empty, de is used
            var result = FindResultHelper.ToFindResult(
                poi,
                "odhactivitypoi",
                "en",
                Array.Empty<string>(),
                false
            );

            Assert.Equal("Wanderweg", result.Name);
            Assert.Equal("Detail.de.Title", result.NameField);
            Assert.Equal("odhactivitypoi", result.Type);
        }

        [Fact]
        public void ToFindResult_NameFallsBackToShortname()
        {
            var data = JToken.Parse(
                @"{ ""Id"": ""MP1"", ""Shortname"": ""Messpunkt"", ""_Meta"": { ""Type"": ""measuringpoint"" } }"
            );

            var result = FindResultHelper.ToFindResult(
                data,
                "measuringpoint",
                "en",
                Array.Empty<string>(),
                false
            );

            Assert.Equal("Messpunkt", result.Name);
            Assert.Equal("Shortname", result.NameField);
        }

        [Fact]
        public void ToFindResult_AddsRequestedFields()
        {
            var result = FindResultHelper.ToFindResult(
                accommodation,
                "accommodation",
                "en",
                new[] { "Active", "NotExisting" },
                false
            );

            var json = JObject.FromObject(result);

            Assert.Equal(
                new[] { "Id", "Name", "NameField", "Type", "Self", "Active", "NotExisting" },
                json.Properties().Select(x => x.Name).ToArray()
            );
            Assert.True(json["Active"]!.Value<bool>());
            Assert.Equal(JTokenType.Null, json["NotExisting"]!.Type);
        }

        [Fact]
        public void ToFindResult_RemoveNullValuesDropsEmptyRequestedFields()
        {
            var result = FindResultHelper.ToFindResult(
                accommodation,
                "accommodation",
                "en",
                new[] { "NotExisting" },
                true
            );

            Assert.Null(result.AdditionalFields);
        }

        [Theory]
        [InlineData(1u, 25, 3, 1u, 3)]
        [InlineData(1u, 2, 3, 2u, 2)]
        [InlineData(2u, 2, 3, 2u, 1)]
        [InlineData(5u, 2, 3, 2u, 0)]
        [InlineData(0u, 2, 3, 2u, 2)]
        [InlineData(1u, int.MaxValue, 3, 1u, 3)]
        [InlineData(1u, 25, 0, 0u, 0)]
        public void PageList_PagesInMemory(
            uint pagenumber,
            int pagesize,
            int count,
            uint expectedtotalpages,
            int expecteditems
        )
        {
            var list = Enumerable.Range(1, count).ToList();

            var (items, totalpages) = ResponseHelpers.PageList(list, pagenumber, pagesize);

            Assert.Equal(expectedtotalpages, totalpages);
            Assert.Equal(expecteditems, items.Count());
        }

        [Fact]
        public void SearchResult_HasStandardPagingFields()
        {
            var result = ResponseHelpers.GetSearchResult(
                1,
                1,
                1,
                "bozen",
                new Dictionary<string, uint>() { { "accommodation", 1 } },
                new[] { new FindResult() { Id = "ACCO1", Name = "Hotel", NameField = "AccoDetail.en.Name", Type = "accommodation", Self = "self" } },
                null
            );

            var json = JObject.Parse(JsonConvert.SerializeObject(result));

            foreach (var key in new[] { "TotalResults", "TotalPages", "CurrentPage", "PreviousPage", "NextPage", "Items", "SearchTerm", "DetailedResults" })
                Assert.True(json.ContainsKey(key), key);

            Assert.Equal(
                new[] { "Id", "Name", "NameField", "Type", "Self" },
                ((JObject)json["Items"]![0]!).Properties().Select(x => x.Name).ToArray()
            );
        }
    }
}
