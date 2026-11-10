// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers
{
    /// <summary>
    /// Reprocesses the failed single imports stored in importfailures on a configurable schedule.
    /// Config section ImportFailureConfig (env ASPNETCORE_ImportFailureConfig__...):
    /// Enabled (default true), IntervalMinutes (default 60), ElementsToProcess (default 100), MaxRetries (default 5)
    /// </summary>
    public class ImportFailureRetryService : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ISettings settings;
        private readonly ILogger<ImportFailureRetryService> logger;

        private readonly bool enabled;
        private readonly TimeSpan interval;
        private readonly int elementstoprocess;
        private readonly int maxretries;

        public ImportFailureRetryService(
            IServiceScopeFactory scopeFactory,
            ISettings settings,
            IConfiguration configuration,
            ILogger<ImportFailureRetryService> logger
        )
        {
            this.scopeFactory = scopeFactory;
            this.settings = settings;
            this.logger = logger;

            var config = configuration.GetSection("ImportFailureConfig");
            this.enabled = config.GetValue<bool>("Enabled", true);
            this.interval = TimeSpan.FromMinutes(Math.Max(1, config.GetValue<int>("IntervalMinutes", 60)));
            this.elementstoprocess = config.GetValue<int>("ElementsToProcess", 100);
            this.maxretries = config.GetValue<int>("MaxRetries", 5);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!enabled)
            {
                logger.LogInformation("ImportFailureRetryService disabled");
                return;
            }

            logger.LogInformation(
                "ImportFailureRetryService started, interval {interval}, elements {elements}, max retries {maxretries}",
                interval,
                elementstoprocess,
                maxretries
            );

            using var timer = new PeriodicTimer(interval);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        using var scope = scopeFactory.CreateScope();
                        var queryFactory = scope.ServiceProvider.GetRequiredService<QueryFactory>();
                        var odhpushnotifier = scope.ServiceProvider.GetRequiredService<IOdhPushNotifier>();

                        var results = await ImportFailureQueue.RetryOpenFailures(
                            settings,
                            queryFactory,
                            odhpushnotifier,
                            elementstoprocess,
                            maxretries,
                            stoppingToken
                        );

                        if (results.Count > 0)
                            logger.LogInformation(
                                "ImportFailureRetryService processed {count} failures, {succeeded} succeeded",
                                results.Count,
                                results.Values.Count(x => x == "succeeded")
                            );
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                    {
                        //Keep the service running, next tick tries again
                        logger.LogError(ex, "ImportFailureRetryService run failed");
                    }
                }
            }
            catch (OperationCanceledException) { }
        }
    }
}
