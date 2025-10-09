// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Helper.Generic;
using Helper.Timeseries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OdhApiCore.Responses;
using SqlKata;
using SqlKata.Execution;

namespace OdhApiCore.Controllers.helper
{
    /// <summary>
    /// Extension methods for integrating timeseries federation into content API controllers
    /// </summary>
    public static class TimeseriesFederationExtensions
    {
        /// <summary>
        /// Parses timeseries filter configuration from query parameters
        /// </summary>
        public static TimeseriesFilterConfig? ParseTimeseriesConfig(
            string? timeseriesApiUrl,
            string? tsDatasetIds,
            string? tsRequiredTypes,
            string? tsOptionalTypes,
            string? tsMeasurementExpression,
            bool? tsLatestOnly,
            string? tsStartTime,
            string? tsEndTime,
            int fetchBatchSize,
            ILogger logger
        )
        {
            // If no timeseries API URL is provided, filtering is disabled
            if (string.IsNullOrEmpty(timeseriesApiUrl))
            {
                return null;
            }

            // If no filter criteria specified, don't enable filtering
            if (string.IsNullOrEmpty(tsDatasetIds) &&
                string.IsNullOrEmpty(tsRequiredTypes) &&
                string.IsNullOrEmpty(tsOptionalTypes) &&
                string.IsNullOrEmpty(tsMeasurementExpression))
            {
                return null;
            }

            var config = new TimeseriesFilterConfig
            {
                Enabled = true,
                TimeseriesApiBaseUrl = timeseriesApiUrl,
                DatasetIds = ParseCommaSeparatedList(tsDatasetIds),
                RequiredTypes = ParseCommaSeparatedList(tsRequiredTypes),
                OptionalTypes = ParseCommaSeparatedList(tsOptionalTypes),
                MeasurementExpression = tsMeasurementExpression,
                LatestOnly = tsLatestOnly,
                StartTime = tsStartTime,
                EndTime = tsEndTime,
                FetchBatchSize = fetchBatchSize
            };

            logger.LogInformation(
                "Timeseries filtering enabled: datasets={DatasetCount}, requiredTypes={RequiredCount}, optionalTypes={OptionalCount}",
                config.DatasetIds?.Count ?? 0,
                config.RequiredTypes?.Count ?? 0,
                config.OptionalTypes?.Count ?? 0
            );

            return config;
        }

        private static List<string>? ParseCommaSeparatedList(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var items = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            return items.Count > 0 ? items : null;
        }
    }
}
