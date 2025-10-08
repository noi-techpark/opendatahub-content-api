// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DataModel;
using Helper;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using static OdhApiCoreTests.IntegrationTets.Helpers;

namespace OdhApiCoreTests.IntegrationTets
{
    [Trait("Category", "Integration")]
    public class ODHActivityPoiApiControllerTests
        : IClassFixture<CustomWebApplicationFactory<OdhApiCore.Startup>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<OdhApiCore.Startup> _factory;

        public ODHActivityPoiApiControllerTests(CustomWebApplicationFactory<OdhApiCore.Startup> factory)
        {
            _factory = factory;
            _client = factory.CreateClient(
                new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
            );
        }

        [Theory]
        [InlineData("/v1/ODHActivityPoi")]
        [InlineData("/v1/ODHActivityPoi?pagesize=1")]
        [InlineData("/v1/ODHActivityPoi?activitytype=12")]
        [InlineData("/v1/ODHActivityPoi?language=de")]
        [InlineData("/v1/ODHActivityPoi?language=en")]
        //[InlineData("/v1/Activity?pagenumber=1&pagesize=100&activitytype=1023&areafilter=skaSKIC57DA31F859141A1802E86B410FEBD70&active=true&seed=null")]
        //[InlineData("/v1/Activity?pagenumber=1&pagesize=100&activitytype=1023&areafilter=skaSKIEC3B49365C47477B83D124D9AE6C3259&active=true&seed=null")]
        [InlineData(
            "/v1/ODHActivityPoi?pagenumber=1&pagesize=10&activitytype=11&locfilter=tvs5228229651CA11D18F1400A02427D15E&odhactive=true&active=true&seed=null"
        )]
        [InlineData("/v1/ODHActivityPoi?pagenumber=1&pagesize=10&activitytype=511&seed=null")]
        [InlineData(
            "/v1/ODHActivityPoi?pagenumber=1&pagesize=20&activitytype=Berg&subtype=null&idlist=null&locfilter=null&areafilter=null&distancefilter=null&altitudefilter=null&durationfilter=null&highlight=null&difficultyfilter=null&active=null&odhactive=null&odhtagfilter=null&seed=null"
        )]
        public async Task Get_Activities(string url)
        {
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            Assert.Equal(
                "application/json; charset=utf-8",
                response.Content.Headers.ContentType?.ToString()
            );
            string json = await response.Content.ReadAsStringAsync();
            dynamic? data = JsonConvert.DeserializeObject(json);
            Assert.NotNull(data);
            if (data != null)
            {
                Assert.IsType<JObject>(data);
                JsonIsType<long>(data.TotalResults);
                Assert.NotEqual(0, (long)data.TotalResults);
                JsonIsType<long>(data.TotalPages);
                Assert.NotEqual(0, (long)data.TotalPages);
                JsonIsType<long>(data.CurrentPage);
                Assert.Equal(1, (long)data.CurrentPage);
                JsonIsType<string>(data.Seed);
                Assert.Empty(data.Seed);
                Assert.IsType<JArray>(data.Items);
                Assert.NotEmpty(data.Items);
            }
        }

        [Theory]
        [InlineData("/v1/ODHActivityPoi/smgpoi078883A95FF002AA246B5B99DA5BB9D7")]
        [InlineData("/v1/ODHActivityPoi/smgpoi107")]
        public async Task Get_SingleActivity(string url)
        {
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            Assert.Equal(
                "application/json; charset=utf-8",
                response.Content.Headers.ContentType?.ToString()
            );
            string json = await response.Content.ReadAsStringAsync();
            dynamic? data = JsonConvert.DeserializeObject(json);
            Assert.NotNull(data);
            if (data != null)
            {
                Assert.IsType<JObject>(data);
                JsonIsType<string>(data.Id);
                JsonIsType<string>(data.Type);
                JsonIsType<string?>(data.SmgId);
                JsonIsType<bool>(data.Active);
                //Assert.IsType<JArray>(data.AreaId);
                //Assert.NotEmpty(data.AreaId);
                Assert.IsType<JObject>(data.Detail);
                Assert.IsType<JObject>(data.Detail.de);
                JsonIsType<string>(data.Detail.de.Title);
                JsonIsType<bool>(data.IsOpen);
                Assert.IsType<JArray>(data.GpsInfo);
                //JsonIsType<bool>(data.Highlight);
                Assert.IsType<JArray>(data.HasLanguage);
                Assert.IsType<JObject>(data.ContactInfos);
                Assert.IsType<JObject>(data.ContactInfos.de);
                JsonIsType<string>(data.TourismorganizationId);
            }
        }

        [Theory]
        [InlineData("/v1/ODHActivityPoi/smgpoi878883A95FF002AA246B5B99DA5BB9D7")]
        [InlineData("/v1/ODHActivityPoi/smgpoiEC73E28E771A21C431A7F8B5B931007V")]
        public async Task Get_SingleNonExistentActivity(string url)
        {
            var response = await _client.GetAsync(url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        //[Fact]
        //public async Task Get_ActivityTypes()
        //{
        //    var response = await _client.GetAsync("/v1/ActivityTypes");
        //    response.EnsureSuccessStatusCode();
        //    Assert.Equal(
        //        "application/json; charset=utf-8",
        //        response.Content.Headers.ContentType?.ToString()
        //    );
        //    string json = await response.Content.ReadAsStringAsync();
        //    dynamic? data = JsonConvert.DeserializeObject<ActivityTypes[]>(json);
        //    Assert.NotEmpty(data);
        //}
    }
}
