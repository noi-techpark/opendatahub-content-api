// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqlKata;
using SqlKata.Execution;

namespace Helper.Timeseries
{
    /// <summary>
    /// Helper class for federating content service queries with timeseries API
    /// Handles iterative verification of sensors against timeseries constraints
    /// </summary>
    public class TimeseriesFederationHelper
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly TimeseriesFilterConfig _config;

        public TimeseriesFederationHelper(
            HttpClient httpClient,
            ILogger logger,
            TimeseriesFilterConfig config)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        }

        /// <summary>
        /// Fetches and verifies sensors iteratively until pagination requirements are met
        /// </summary>
        /// <param name="baseQuery">Base SQL query to fetch sensors</param>
        /// <param name="pageNumber">Requested page number</param>
        /// <param name="pageSize">Requested page size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Verified sensor IDs that satisfy timeseries constraints</returns>
        public async Task<FederationResult> FetchAndVerifySensorsAsync(
            Query baseQuery,
            CancellationToken cancellationToken = default)
        {
            var verifiedIds = new List<string>();
            _logger.LogInformation("Starting timeseries federation");

            // Fetch next batch of sensor IDs (explicitly select only ID column)
            var batch = await baseQuery
                .Clone()
                .Select("id")
                .GetAsync<string>(cancellationToken: cancellationToken);

            var idList = batch.ToList();

            // Verify this batch against timeseries constraints
            var verifyResponse = await VerifySensorsAsync(idList, cancellationToken);

            if (verifyResponse?.Verified != null)
            {
                verifiedIds.AddRange(verifyResponse.Verified);
            }

            _logger.LogInformation(
                "Federation complete: verified={Verified}", verifiedIds.Count);

            return new FederationResult
            {
                VerifiedIds = verifiedIds,
            };
        }

        /// <summary>
        /// Verifies a list of sensor names against timeseries constraints
        /// </summary>
        private async Task<TimeseriesVerifyResponse?> VerifySensorsAsync(
            List<string> sensorNames,
            CancellationToken cancellationToken)
        {
            try
            {
                var request = new TimeseriesVerifyRequest
                {
                    SensorNames = sensorNames,
                    TimeseriesFilter = BuildTimeseriesFilter(),
                    MeasurementFilter = BuildMeasurementFilter()
                };

                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var apiUrl = $"{_config.TimeseriesApiBaseUrl.TrimEnd('/')}/api/v1/sensors/verify";
                var response = await _httpClient.PostAsync(apiUrl, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Timeseries verify API returned error: {StatusCode} - {Reason}",
                        response.StatusCode, response.ReasonPhrase);
                    return null;
                }

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<TimeseriesVerifyResponse>(responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling timeseries verify API");
                return null;
            }
        }

        private TimeseriesFilter? BuildTimeseriesFilter()
        {
            if (_config.RequiredTypes == null &&
                _config.OptionalTypes == null &&
                _config.DatasetIds == null)
            {
                return null;
            }

            return new TimeseriesFilter
            {
                RequiredTypes = _config.RequiredTypes,
                OptionalTypes = _config.OptionalTypes,
                DatasetIds = _config.DatasetIds
            };
        }

        private MeasurementFilter? BuildMeasurementFilter()
        {
            if (string.IsNullOrEmpty(_config.MeasurementExpression) &&
                _config.LatestOnly == null &&
                string.IsNullOrEmpty(_config.StartTime) &&
                string.IsNullOrEmpty(_config.EndTime))
            {
                return null;
            }

            MeasurementFilter? filter = null;

            if (!string.IsNullOrEmpty(_config.MeasurementExpression) || _config.LatestOnly == true)
            {
                filter = new MeasurementFilter
                {
                    Expression = _config.MeasurementExpression,
                    LatestOnly = _config.LatestOnly
                };
            }

            if (!string.IsNullOrEmpty(_config.StartTime) || !string.IsNullOrEmpty(_config.EndTime))
            {
                filter ??= new MeasurementFilter();
                filter.TimeRange = new TimeRange
                {
                    StartTime = _config.StartTime,
                    EndTime = _config.EndTime
                };
            }

            return filter;
        }

        /// <summary>
        /// Gets sensor types from timeseries API
        /// </summary>
        public async Task<TimeseriesTypesResponse?> GetSensorTypesAsync(
            List<string> sensorNames,
            bool distinct,
            CancellationToken cancellationToken)
        {
            try
            {
                var request = new TimeseriesTypesRequest
                {
                    SensorNames = sensorNames,
                    Distinct = distinct
                };

                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var apiUrl = $"{_config.TimeseriesApiBaseUrl.TrimEnd('/')}/api/v1/sensors/types";
                var response = await _httpClient.PostAsync(apiUrl, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Timeseries types API returned error: {StatusCode} - {Reason}",
                        response.StatusCode, response.ReasonPhrase);
                    return null;
                }

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<TimeseriesTypesResponse>(responseJson,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling timeseries types API");
                return null;
            }
        }
    }

    #region DTOs for Timeseries API

    public class TimeseriesVerifyRequest
    {
        public List<string> SensorNames { get; set; } = new();
        public TimeseriesFilter? TimeseriesFilter { get; set; }
        public MeasurementFilter? MeasurementFilter { get; set; }
    }

    public class TimeseriesVerifyResponse
    {
        public bool Ok { get; set; }
        public List<string> Verified { get; set; } = new();
        public List<string> Unverified { get; set; } = new();
    }

    public class TimeseriesTypesRequest
    {
        public List<string> SensorNames { get; set; } = new();
        public bool Distinct { get; set; }
    }

    public class TimeseriesTypesResponse
    {
        public List<SensorTypesInfo>? Sensors { get; set; }
        public List<TypeInfo>? Types { get; set; }
        public int Total { get; set; }
    }

    public class SensorTypesInfo
    {
        public string SensorName { get; set; } = string.Empty;
        public long SensorId { get; set; }
        public List<TypeWithTimeseries> Types { get; set; } = new();
        public int Total { get; set; }
    }

    public class TypeWithTimeseries
    {
        public TypeInfo TypeInfo { get; set; } = new();
        public string TimeseriesId { get; set; } = string.Empty;
        public string SensorName { get; set; } = string.Empty;
    }

    public class TypeInfo
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Unit { get; set; }
        public string DataType { get; set; } = string.Empty;
        public System.Text.Json.JsonElement? Metadata { get; set; }
    }

    public class TimeseriesFilter
    {
        public List<string>? RequiredTypes { get; set; }
        public List<string>? OptionalTypes { get; set; }
        public List<string>? DatasetIds { get; set; }
    }

    public class MeasurementFilter
    {
        public bool? LatestOnly { get; set; }
        public TimeRange? TimeRange { get; set; }
        public string? Expression { get; set; }
    }

    public class TimeRange
    {
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
    }

    #endregion

    /// <summary>
    /// Result of federation operation
    /// </summary>
    public class FederationResult
    {
        /// <summary>
        /// Final list of verified sensor IDs
        /// </summary>
        public List<string> VerifiedIds { get; set; } = new();
    }
}
