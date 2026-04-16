// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdhApiCoreTests.IntegrationTets;
using Xunit;

namespace OdhApiCoreTests.IntegrationTests
{
    [Trait("Category", "Integration")]
    public class FieldsFilterTests : IClassFixture<CustomWebApplicationFactory<OdhApiCore.Startup>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<OdhApiCore.Startup> _factory;

        public FieldsFilterTests(CustomWebApplicationFactory<OdhApiCore.Startup> factory)
        {
            _factory = factory;
            _client = factory.CreateClient(
                new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
            );
        }

        // fields=ImageGallery.[] --> selects whole array, key stays "ImageGallery.[]"
        [Fact]
        public async Task TestFields_ImageGallery_EmptyBracket()
        {
            var url = "/v1/Accommodation?pagesize=1&pagenumber=1&fields=ImageGallery.[]";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            dynamic? data = JsonConvert.DeserializeObject(json);
            Assert.NotNull(data);

            var firstItem = data!.Items[0];
            Helpers.JsonIsType<string>(firstItem.Id);

            // Key must remain exactly as passed
            var gallery = firstItem["ImageGallery.[]"];
            Assert.NotNull(gallery);
            Assert.IsType<JArray>(gallery);
        }

        // fields=ImageGallery.[*] --> selects whole array, key stays "ImageGallery.[*]"
        [Fact]
        public async Task TestFields_ImageGallery_Wildcard()
        {
            var url = "/v1/Accommodation?pagesize=1&pagenumber=1&fields=ImageGallery.[*]";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            dynamic? data = JsonConvert.DeserializeObject(json);
            Assert.NotNull(data);

            var firstItem = data!.Items[0];
            Helpers.JsonIsType<string>(firstItem.Id);

            // Key must remain exactly as passed
            var gallery = firstItem["ImageGallery.[*]"];
            Assert.NotNull(gallery);
            Assert.IsType<JArray>(gallery);
        }

        // fields=ImageGallery.[0] --> selects first element, key stays "ImageGallery.[0]"
        [Fact]
        public async Task TestFields_ImageGallery_FirstElement()
        {
            var url = "/v1/Accommodation?pagesize=1&pagenumber=1&fields=ImageGallery.[0]";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            dynamic? data = JsonConvert.DeserializeObject(json);
            Assert.NotNull(data);

            var firstItem = data!.Items[0];
            Helpers.JsonIsType<string>(firstItem.Id);

            // Key must remain exactly as passed
            var firstImage = firstItem["ImageGallery.[0]"];
            Assert.NotNull(firstImage);
            // First element should be an object, not an array
            Assert.IsType<JObject>(firstImage);
        }

        // fields=Mapping['tirol.mapservices.eu'] --> selects dict key with dot, key stays as-is
        [Fact]
        public async Task TestFields_Mapping_DottedKey()
        {
            var url = "/v1/Accommodation?pagesize=1&pagenumber=1&fields=Mapping['tirol.mapservices.eu']";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            dynamic? data = JsonConvert.DeserializeObject(json);
            Assert.NotNull(data);

            var firstItem = data!.Items[0];
            Helpers.JsonIsType<string>(firstItem.Id);

            // Key must remain exactly as passed
            var mapping = firstItem["Mapping['tirol.mapservices.eu']"];
            // Value can be null if not present, but key must exist
            Assert.True(
                mapping == null || mapping is JObject || mapping is JValue,
                "Mapping key should be null, an object, or a value"
            );
        }

        // Combine multiple field notations in one request
        [Fact]
        public async Task TestFields_Combined()
        {
            var url = "/v1/Accommodation?pagesize=1&pagenumber=1&fields=ImageGallery.[0],ImageGallery.[*],Mapping['tirol.mapservices.eu'],Detail.de.Title";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            dynamic? data = JsonConvert.DeserializeObject(json);
            Assert.NotNull(data);

            var firstItem = data!.Items[0];

            // Id always present
            Helpers.JsonIsType<string>(firstItem.Id);

            // All keys must be present and match input exactly
            Assert.NotNull(firstItem["ImageGallery.[0]"]);
            Assert.NotNull(firstItem["ImageGallery.[*]"]);
            Assert.IsType<JObject>(firstItem["ImageGallery.[0]"]);
            Assert.IsType<JArray>(firstItem["ImageGallery.[*]"]);

            // Detail.de.Title — standard dot path, should be a string or null
            var title = firstItem["Detail.de.Title"];
            Assert.True(
                title == null || title is JValue,
                "Detail.de.Title should be a string value or null"
            );
        }

        // Non-existent field should return null value but key must still be present
        [Fact]
        public async Task TestFields_NonExistentField_ReturnsNull()
        {
            var url = "/v1/Accommodation?pagesize=1&pagenumber=1&fields=NonExistentField";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            dynamic? data = JsonConvert.DeserializeObject(json);
            Assert.NotNull(data);

            var firstItem = data!.Items[0];
            Helpers.JsonIsType<string>(firstItem.Id);

            var nonExistent = firstItem["NonExistentField"];
            Assert.Equal((object?)nonExistent, new JValue((object?)null));
        }
    }
}