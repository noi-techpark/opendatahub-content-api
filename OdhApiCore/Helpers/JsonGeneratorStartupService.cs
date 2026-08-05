// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Threading;
using System.Threading.Tasks;
using Helper;
using Helper.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OdhApiCore.GenericHelpers;
using SqlKata.Execution;

namespace OdhApiCore.Helpers
{
    /// <summary>
    /// Generates the json files the frontends/STA depend on (Taglist, AccommodationBooklist, ...) as soon
    /// as the pod boots, instead of relying on an external post-deploy curl step. Combined with
    /// JsonFilesReadyHealthCheck on "/ready", this keeps Kubernetes from routing traffic to the pod
    /// until generation has actually finished - on every pod start, not just the ones that happen to
    /// follow a CI/CD deploy.
    /// </summary>
    public class JsonGeneratorStartupService : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ISettings settings;
        private readonly ILogger<JsonGeneratorStartupService> logger;

        public JsonGeneratorStartupService(
            IServiceScopeFactory scopeFactory,
            ISettings settings,
            ILogger<JsonGeneratorStartupService> logger
        )
        {
            this.scopeFactory = scopeFactory;
            this.settings = settings;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var jsondir = settings.JsonConfig.Jsondir;

            //Retry until the DB is reachable and generation succeeds, the pod is not marked ready before that
            var attempt = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                attempt++;
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var queryFactory = scope.ServiceProvider.GetRequiredService<QueryFactory>();

                    await JsonGeneratorHelper.GenerateJSONAccommodationsForBooklist(
                        queryFactory,
                        jsondir,
                        true,
                        "AccosBookable"
                    );
                    await JsonGeneratorHelper.GenerateJSONAccommodationsForBooklist(
                        queryFactory,
                        jsondir,
                        null,
                        "AccosAll"
                    );
                    await JsonGeneratorHelper.GenerateJSONTaglist(queryFactory, jsondir, "GenericTags");
                    await JsonGeneratorHelper.GenerateJSONODHTagAutoPublishlist(
                        queryFactory,
                        jsondir,
                        "AutoPublishTags"
                    );
                    await JsonGeneratorHelper.GenerateJSONODHTagCategoriesList(
                        queryFactory,
                        jsondir,
                        "TagsForCategories"
                    );
                    await STARequestHelper.GenerateJSONODHActivityPoiForSTA(
                        queryFactory,
                        jsondir,
                        settings.XmlConfig.Xmldir
                    );
                    await STARequestHelper.GenerateJSONAccommodationsForSTA(queryFactory, jsondir);

                    if (settings.S3Config.ContainsKey("dc-meteorology-province-forecast"))
                    {
                        await GetDataFromS3.GetFileFromS3(
                            "dc-meteorology-province-forecast",
                            settings.S3Config["dc-meteorology-province-forecast"].AccessKey,
                            settings.S3Config["dc-meteorology-province-forecast"].AccessSecretKey,
                            settings.S3Config["dc-meteorology-province-forecast"].Filename,
                            jsondir
                        );
                    }

                    logger.LogInformation("Startup json generation succeeded after {Attempt} attempt(s)", attempt);
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Startup json generation failed (attempt {Attempt}), retrying in 10s",
                        attempt
                    );

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }
    }
}
