// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using EBMS;
using Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NINJA;
using NINJA.Parser;
using OdhApiImporter.Helpers;
using OdhNotifier;
using RAVEN;
using SqlKata.Execution;

namespace OdhApiImporter.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [ApiController]
    public class PushDataApiController : Controller
    {
        private readonly ISettings settings;
        private readonly QueryFactory QueryFactory;
        private readonly ILogger<PushDataApiController> logger;
        private readonly IWebHostEnvironment env;
        private IOdhPushNotifier OdhPushnotifier;

        public PushDataApiController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<PushDataApiController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
        {
            this.env = env;
            this.settings = settings;
            this.logger = logger;
            this.QueryFactory = queryFactory;
            this.OdhPushnotifier = odhpushnotifier;
        }

        #region CustomPush

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("PushODHActivityPoisByTag")]
        public async Task<IActionResult> PushODHActivityPoisByTag(
            string datatype,
            string? ids,
            string? tags,
            string? notificationchannel,
            CancellationToken cancellationToken
        )
        {
            var type = datatype.ToLower();

            try
            {
                type = ODHTypeHelper.TranslateType2Table(datatype);

                List<string> taglist =
                    tags != null ? tags.ToLower().Split(',').ToList() : new List<string>();
                List<string> idlist =
                    ids != null ? ids.ToLower().Split(',').ToList() : new List<string>();

                PushDataOperation customdataoperation = new PushDataOperation(
                    settings,
                    QueryFactory,
                    OdhPushnotifier
                );
                var results = await customdataoperation.PushAllODHActivityPoiwithTags(
                    type,
                    idlist,
                    taglist
                );

                List<UpdateDetail> updates = new List<UpdateDetail>();
                foreach (var result in results)
                {
                    updates.Add(
                        new UpdateDetail
                        {
                            created = 0,
                            deleted = 0,
                            id = result.Key,
                            updated = 0,
                            error = 0,
                            pushed = result.Value,
                            pushchannels = result.Value.Keys,
                            objectcompared = 0,
                            changes = null,
                            objectchanged = 0,
                            objectimagechanged = 0,
                        }
                    );
                }

                return Ok(GenericResultsHelper.GetUpdateResult(
                    null, "api", type + ".push." + notificationchannel, "custom", "Done", "", updates, null, true
                    ));
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetUpdateResult(
                    ids ?? tags,
                    "api",
                    type + ".push." + notificationchannel,
                    "custom",
                    "Push to Marketplace failed",
                    "",
                    new List<UpdateDetail>(){ new UpdateDetail() { error = 1 } },
                    ex,
                    true
                );
                return BadRequest(errorResult);
            }
        }

        #endregion
    }
}
