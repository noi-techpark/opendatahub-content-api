// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DataModel;
using Helper;
using Helper.Factories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OdhApiImporter.Helpers;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiImporter.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [ApiController]
    public class TestController : Controller
    {
        private readonly ISettings settings;
        private readonly QueryFactory QueryFactory;
        private readonly ILogger<TestController> logger;
        private readonly IWebHostEnvironment env;
        private IOdhPushNotifier OdhPushnotifier;
        private readonly IMongoDBFactory MongoDBFactory;

        public TestController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<TestController> logger,
            QueryFactory queryFactory,
            IMongoDBFactory mongoDBFactory,
            IOdhPushNotifier odhpushnotifier
        )
        {
            this.env = env;
            this.settings = settings;
            this.logger = logger;
            this.QueryFactory = queryFactory;
            this.MongoDBFactory = mongoDBFactory;
            this.OdhPushnotifier = odhpushnotifier;
        }

        [HttpGet, Route("Test")]
        public IActionResult Get()
        {
            return Ok("importer alive");
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("TestAuthorized")]
        public IActionResult GetAuthorized()
        {
            return Ok("importer alive");
        }

        [HttpGet, Route("TestSomething")]
        public async Task<IActionResult> TestSomething()
        {
            //var test = MongoDBFactory.GetDocumentById<BsonDocument>("TestDB", "TestDB", "63cfa30278b2fc0eda271a28");

            //var districtgps = await QueryFactory
            //.Query()
            //.Select("gen_latitude","gen_longitude")
            //.From("districts")
            //.Where("id", "69710B855B094FAB8FD2AAED0E16E7E0")
            //.GetAsync<Coordinate>();

            var query = QueryFactory
              .Query("events")
              .Select("data")
              .Where("id", "08A54F0371634DAAB43600341CDE980E");


            var result = await query.GetObjectSingleAsync<EventLinked>();

            return Ok(result);
        }
    }
}
