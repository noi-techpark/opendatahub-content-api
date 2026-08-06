// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Helper;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OdhApiImporter.Helpers
{
    /// <summary>
    /// Reports Unhealthy until all json files needed by the importers (LTSTags, AutoPublishTags, ...)
    /// are present on disk. Wired into the "/ready" endpoint (tag "services") so Kubernetes only
    /// routes traffic to a pod once it can actually process import requests.
    /// </summary>
    public class JsonFilesReadyHealthCheck : IHealthCheck
    {
        //Filenames (without extension) that JsonGeneratorStartupService.RequiredJsonFiles generates
        public static readonly IReadOnlyCollection<string> RequiredJsonFiles = new[]
        {
            "GenericTags",
            "AutoPublishTags",
            "ODHTagsSourceIDMLTS",
            "CategoryCodes",
            "DishRates",
            "Facilities",
            "CapacityCeremonies",
            "GastronomyDisplayAsCategory",
            "LTSTagsAndTins",
            "ActivityPoiDisplayAsCategory",
            "Features",
        };

        private readonly ISettings settings;

        public JsonFilesReadyHealthCheck(ISettings settings)
        {
            this.settings = settings;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default
        )
        {
            var jsondir = settings.JsonConfig.Jsondir;
            var missing = new List<string>();

            foreach (var jsonname in RequiredJsonFiles)
            {
                var filepath = Path.Combine(jsondir, $"{jsonname}.json");
                if (!File.Exists(filepath))
                    missing.Add(jsonname);
            }

            if (missing.Count > 0)
            {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy(
                        "Missing json files: " + string.Join(", ", missing)
                    )
                );
            }

            return Task.FromResult(HealthCheckResult.Healthy("All required json files are present"));
        }
    }
}
