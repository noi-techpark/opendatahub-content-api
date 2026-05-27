// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using DataModel.Annotations;
using Helper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiCore.Controllers.api
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("[controller]")]
    [ApiController]
    public class TestController : OdhController
    {
        private readonly IWebHostEnvironment env;
        private readonly ISettings settings;
        private readonly IHttpClientFactory httpClientFactory;

        public TestController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<TestController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier,
            IHttpClientFactory httpClientFactory
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier)
        {
            this.httpClientFactory = httpClientFactory;
            this.settings = settings;
            this.env = env;
        }

        [HttpGet, Route("TestAppsettings")]
        public IActionResult GetTestappsettings()
        {
            return Ok(JsonConvert.SerializeObject(settings.Field2HideConfig));
        }

        [HttpGet, Route("TestQuotasettings")]
        public IActionResult GetTestquotasettings()
        {
            return Ok(JsonConvert.SerializeObject(settings.RateLimitConfig));
        }

        [HttpGet, Route("TestQuotaRoutes")]
        public IActionResult GetTestQuotaRoutes()
        {
            return Ok(JsonConvert.SerializeObject(settings.NoRateLimitConfig));
        }

        [HttpGet, Route("UrlHelper", Name = "UrlHelperTest")]
        public object GetUrl(CancellationToken cancellationToken)
        {
            var url = Url.Link("UrlHelperTest", new { });
            var remoteurl = RemoteIpHelper.GetRequestIP(this.HttpContext, true);

            var xforwardedforheader = RemoteIpHelper.GetHeaderValueAs<string>(
                "X-Forwarded-For",
                this.HttpContext
            );
            var xforwardedprotoheader = RemoteIpHelper.GetHeaderValueAs<string>(
                "X-Forwarded-Proto",
                this.HttpContext
            );
            var xforwardedhostheader = RemoteIpHelper.GetHeaderValueAs<string>(
                "X-Forwarded-Host",
                this.HttpContext
            );

            var xforwardedproto = this.HttpContext.Request.Scheme;
            var xforwardedhost = this.HttpContext.Request.Host;

            return new
            {
                URL = url,
                RemoteURL = remoteurl,
                ForwardedForHeader = xforwardedforheader,
                ForwardedProtoHeader = xforwardedprotoheader,
                ForwardedHostHeader = xforwardedhostheader,
                ForwardedProtoContext = xforwardedproto,
                ForwardedHostContext = xforwardedhost,
            };
        }

        //Not working
        [HttpGet, Route("TestDateTimeConversion1")]
        public IActionResult GetDatetimeConversion1()
        {
            var date = DateTime.ParseExact(
                "31/12/2020 18:00",
                "dd/MM/yyyy HH:mm",
                CultureInfo.InvariantCulture
            );

            return Ok(date);
        }

        //WORKS
        [HttpGet, Route("TestDateTimeConversion2")]
        public IActionResult GetDatetimeConversiont2()
        {
            var myculture = new CultureInfo("en-GB");
            var date = Convert.ToDateTime("31/12/2020 18:00", myculture);

            return Ok(date);
        }

        [HttpGet, Route("TestDateTimeConversion3")]
        public IActionResult GetDatetimeConversiont3()
        {
            var myculture = new CultureInfo("en-GB");
            var date = Convert.ToDateTime("31/12/2020 18:00:00", myculture);

            return Ok(date);
        }

        //Not working
        [HttpGet, Route("TestDateTimeConversion4")]
        public IActionResult GetDatetimeConversiont4()
        {
            var myculture = new CultureInfo("en-GB");
            var date = Convert.ToDateTime("31/12/2020T18:00", myculture);

            return Ok(date);
        }

        //Not working
        [HttpGet, Route("TestDateTimeConversion5")]
        public IActionResult GetDatetimeConversiont5()
        {
            var myculture = new CultureInfo("en-GB");
            var date = Convert.ToDateTime("31/12/2020T18:00:00", myculture);

            return Ok(date);
        }

        [HttpGet, Route("GetSystemTimezones")]
        public IActionResult GetSystemTimezones()
        {
            List<string> timezones = new List<string>();
            foreach (TimeZoneInfo z in TimeZoneInfo.GetSystemTimeZones())
            {
                timezones.Add(z.Id);
            }
            return Ok(timezones);
        }

        [HttpGet, Route("GetTimeZoneTest")]
        public IActionResult GetTimeZoneTest()
        {
            var currentdate = TimeZoneInfo.ConvertTime(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome")
            );
            var date = DateTime.Now;

            return Ok(new { date, currentdate });
        }

        [ProducesResponseType(typeof(IEnumerable<ObjectwithDeprecated>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("TestDeprecated")]
        public IActionResult GetDeprecated()
        {
            ObjectwithDeprecated test1 = new ObjectwithDeprecated()
            {
                name = "test",
                name2 = "test",
                namecol = new List<string>() { "test1", "test2" },
            };
            ObjectwithDeprecated test2 = new ObjectwithDeprecated()
            {
                name = "test2",
                name2 = "test2",
                namecol = new List<string>() { "test21", "test22" },
            };

            var toreturn = new List<ObjectwithDeprecated>();
            toreturn.Add(test1);
            toreturn.Add(test2);

            return Ok(toreturn);
        }

        [HttpGet, Route("TestQuery")]
        public async Task<IActionResult> GetTestRudi()
        {
            var validforentity = new List<string>();
            validforentity.Add("winter");

            var subcategories = await QueryFactory
                .Query()
                .SelectRaw("data")
                .From("smgtags")
                .ODHTagValidForEntityFilter(validforentity)
                .ODHTagMainEntityFilter(new List<string>() { "smgpoi" })
                .GetObjectListAsync<ODHTagLinked>();

            return Ok(subcategories);
        }

        [HttpGet, Route("TestLocReduced")]
        public async Task<IActionResult> GetTestLocReduced()
        {
            var municipalityreducedinfo = await GpsHelper.GetReducedWithGPSInfoList(
                QueryFactory,
                "municipalities"
            );

            return Ok(municipalityreducedinfo);
        }

        //[HttpGet, Route("TestFCMSendV2")]
        //public async Task<IActionResult> TestFCMSendV2()
        //{
        //    var pushserverconfig = settings.FCMConfig.Where(x => x.Identifier == "noi-communityapp").FirstOrDefault();

        //    string sendurl = $"https://fcm.googleapis.com/v1/projects/{pushserverconfig.ProjecTName}/messages:send";
        //    //var result = await FCMPushNotification.SendNotificationV2(new FCMessageV2() { }, sendurl, pushserverconfig.ServiceAccount);
        //    var cred = await FCMPushNotification.GetGoogleTokenServiceAccount(pushserverconfig.ServiceAccount);
        //    var token = await cred.UnderlyingCredential.GetAccessTokenForRequestAsync();

        //    return Ok(token);
        //}


        //Not working
        //[HttpGet, Route("TestSomething")]
        //public IActionResult GetTestSomething()
        //{
        //    return Ok(new { settings.S3Config, settings.NotifierConfig  });
        //}


        //[TypeFilter(typeof(Filters.RequestInterceptorAttribute))]
        //[HttpGet, Route("TestObject")]
        //public IActionResult GetTagObject(string language = null, CancellationToken cancellationToken = default)
        //{
        //    var tag = new SmgTags() { Id = "test", MainEntity = "Blah", Shortname = "hallo", TagName = null, ValidForEntity = new List<string>() { "hallo" } };

        //    return Ok(tag);
        //}


        //[HttpGet, Route("Anonymous")]
        //public IActionResult GetAnonymous(CancellationToken cancellationToken)
        //{
        //    return this.Content(User.Identity?.Name + " Anonymous working", "application/json", Encoding.UTF8);
        //}

        //[Authorize]
        //[HttpGet, Route("Restricted")]
        //public IActionResult GetRestricted(CancellationToken cancellationToken)
        //{
        //    return this.Content(User.Identity?.Name + " Restricted working", "application/json", Encoding.UTF8);
        //}

        //[Authorize(Roles = "DataReader")]
        //[HttpGet, Route("WithRole")]
        //public IActionResult GetWithRole(CancellationToken cancellationToken)
        //{
        //    return this.Content(User.Identity?.Name + " WithRole working", "application/json", Encoding.UTF8);
        //}

        //[Authorize(Roles = "Hallihallo")]
        //[HttpGet, Route("WithRole2")]
        //public IActionResult GetWithRole2(CancellationToken cancellationToken)
        //{
        //    return this.Content(User.Identity?.Name + " WithRole2 working", "application/json", Encoding.UTF8);
        //}

        //[HttpGet, Route("Environment")]
        //public IActionResult GetEnvironmentV(CancellationToken cancellationToken)
        //{
        //    var siaguser = settings.SiagConfig.Username;
        //    var mssuser = settings.MssConfig.Username;
        //    var xmldir = settings.XmlConfig.Xmldir;
        //    var xmldir2 = settings.XmlConfig.XmldirWeather;
        //    var pgconnection = settings.PostgresConnectionString;


        //    return this.Content(" mss user: " + mssuser + " siag user " + siaguser + " xmldir " + xmldir + " " + xmldir2  + " pgconnection " + pgconnection.Substring(0,50), "application/json", Encoding.UTF8);
        //}

        #region CacheTestController

        //// Cache for 100 seconds on the server, inform the client that response is valid for 100 seconds
        //[OdhCacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100, Private = true)]
        //[HttpGet, Route("Cached100")]
        //public IEnumerable<string> GetCached100()
        //{
        //    return new string[] { "value1", "value2", DateTime.Now.ToLongTimeString() };
        //}

        //// Cache for 100 seconds on the server, inform the client that response is valid for 100 seconds. Cache for anonymous users only.
        //[OdhCacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100, AnonymousOnly = true)]
        //[HttpGet, Route("Cached100Anonymous")]
        //public IEnumerable<string> GetCached100Anonymous()
        //{
        //    return new string[] { "value1", "value2", DateTime.Now.ToLongTimeString() };
        //}

        //// Inform the client that response is valid for 50 seconds. Force client to revalidate.
        //[OdhCacheOutput(ClientTimeSpan = 50, MustRevalidate = true)]
        //[HttpGet, Route("Cached50Revalidate")]
        //public IEnumerable<string> GetCached50Revalidate(string hello)
        //{
        //    return new string[] { "value1", "value2", DateTime.Now.ToLongTimeString() };
        //}

        //// Cache for 50 seconds on the server. Ignore querystring parameters when serving cached content.
        //[OdhCacheOutput(ServerTimeSpan = 50, ExcludeQueryStringFromCacheKey = true)]
        //[HttpGet, Route("Cached50WithoutQS")]
        //public IEnumerable<string> GetCached50WithoutQS(string hello)
        //{
        //    return new string[] { "value1", "value2", DateTime.Now.ToLongTimeString() };
        //}

        //TODO SEE if Cache distinguish between Authenticated and not authenticated user

        #endregion
    }

    public class ObjectwithDeprecated
    {
        public string? name { get; set; }

        [SwaggerDeprecated("Will be removed on 12-05-22, please use name instead.")]
        public string? name2 { get; set; }
        public ICollection<string>? namecol { get; set; }
    }
}
