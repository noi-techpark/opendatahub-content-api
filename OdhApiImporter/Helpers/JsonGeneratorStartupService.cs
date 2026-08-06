// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers
{
    /// <summary>
    /// Generates the json files the importers depend on (LTSTagsAndTins, AutoPublishTags, ...) as soon
    /// as the pod boots, instead of relying on an external post-deploy curl step. Combined with
    /// JsonFilesReadyHealthCheck on "/ready", this keeps Kubernetes from routing traffic (including
    /// import trigger calls) to the pod until generation has actually finished - on every pod start,
    /// not just the ones that happen to follow a CI/CD deploy.
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

                    await JsonGeneratorHelper.GenerateJSONTaglist(queryFactory, jsondir, "GenericTags");
                    await JsonGeneratorHelper.GenerateJSONODHTagAutoPublishlist(
                        queryFactory,
                        jsondir,
                        "AutoPublishTags"
                    );
                    await JsonGeneratorHelper.GenerateJSONODHTagSourceIDMLTSList(
                        queryFactory,
                        jsondir,
                        "ODHTagsSourceIDMLTS"
                    );
                    await JsonGeneratorHelper.GenerateJSONGastronomyTagCategoriesList(
                        queryFactory,
                        jsondir,
                        "CategoryCodes",
                        new List<string>() { "gastronomycategory" }
                    );
                    await JsonGeneratorHelper.GenerateJSONGastronomyTagCategoriesList(
                        queryFactory,
                        jsondir,
                        "DishRates",
                        new List<string>() { "gastronomydishcodes" }
                    );
                    await JsonGeneratorHelper.GenerateJSONGastronomyTagCategoriesList(
                        queryFactory,
                        jsondir,
                        "Facilities",
                        new List<string>() { "gastronomyfacilities" }
                    );
                    await JsonGeneratorHelper.GenerateJSONGastronomyTagCategoriesList(
                        queryFactory,
                        jsondir,
                        "CapacityCeremonies",
                        new List<string>() { "gastronomyceremonycodes" }
                    );
                    await JsonGeneratorHelper.GenerateJSONODHTagsDisplayAsCategoryList(
                        queryFactory,
                        jsondir,
                        "GastronomyDisplayAsCategory",
                        new List<string>() { "essen trinken" }
                    );
                    await JsonGeneratorHelper.GenerateJSONLTSTagsList(
                        queryFactory,
                        jsondir,
                        "LTSTagsAndTins",
                        new List<string>() { "tagsactivity", "ltstagproperties", "tagspointofinterest" }
                    );
                    await JsonGeneratorHelper.GenerateJSONODHTagsDisplayAsCategoryList(
                        queryFactory,
                        jsondir,
                        "ActivityPoiDisplayAsCategory",
                        new List<string>() { "odhactivitypoi" }
                    );
                    await JsonGeneratorHelper.GenerateJSONLTSAccoFeaturesList(queryFactory, jsondir, "Features");

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
