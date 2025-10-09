// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;

namespace Helper.Timeseries
{
    /// <summary>
    /// Configuration for timeseries-based filtering
    /// Defines which timeseries constraints to apply when federating with the timeseries API
    /// </summary>
    public class TimeseriesFilterConfig
    {
        /// <summary>
        /// Enable timeseries filtering for this controller
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Base URL of the timeseries API (e.g., "http://localhost:8080")
        /// </summary>
        public string? TimeseriesApiBaseUrl { get; set; }

        /// <summary>
        /// Required measurement types - sensors must have ALL of these types
        /// </summary>
        public List<string>? RequiredTypes { get; set; }

        /// <summary>
        /// Optional measurement types - sensors may have ANY of these types
        /// </summary>
        public List<string>? OptionalTypes { get; set; }

        /// <summary>
        /// Dataset IDs to filter by - sensors must be in these datasets
        /// </summary>
        public List<string>? DatasetIds { get; set; }

        /// <summary>
        /// Measurement filter expression (e.g., "or(o2.eq.2, and(temp.gteq.20, temp.lteq.30))")
        /// </summary>
        public string? MeasurementExpression { get; set; }

        /// <summary>
        /// Only consider latest measurements
        /// </summary>
        public bool? LatestOnly { get; set; }

        /// <summary>
        /// Time range start (RFC3339 format)
        /// </summary>
        public string? StartTime { get; set; }

        /// <summary>
        /// Time range end (RFC3339 format)
        /// </summary>
        public string? EndTime { get; set; }

        /// <summary>
        /// Maximum number of sensors to fetch per batch during verification (performance tuning)
        /// Higher values = fewer API calls but more data transferred
        /// </summary>
        public int FetchBatchSize { get; set; } = 100;

        /// <summary>
        /// HTTP timeout for timeseries API calls in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }
}
