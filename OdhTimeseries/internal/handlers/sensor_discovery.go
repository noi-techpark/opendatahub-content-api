package handlers

import (
	"net/http"
	"strconv"
	"strings"

	"timeseries-api/internal/filter"
	"timeseries-api/internal/repository"

	"github.com/gin-gonic/gin"
	"github.com/sirupsen/logrus"
)

type SensorDiscoveryHandler struct {
	repo *repository.Repository
}

func NewSensorDiscoveryHandler(repo *repository.Repository) *SensorDiscoveryHandler {
	return &SensorDiscoveryHandler{repo: repo}
}

// DiscoverSensors finds sensors based on their timeseries data and measurement conditions
// @Summary Discover sensors by measurement conditions
// @Description Find sensors that satisfy conditions over their timeseries measurements
// @Tags sensors
// @Accept json
// @Produce json
// @Param request body filter.SensorDiscoveryRequest true "Sensor discovery criteria"
// @Success 200 {object} map[string]interface{} "Discovered sensors"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /sensors [post]
func (h *SensorDiscoveryHandler) DiscoverSensors(c *gin.Context) {
	var req filter.SensorDiscoveryRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid request format", "details": err.Error()})
		return
	}

	logrus.WithFields(logrus.Fields{
		"timeseries_filter":  req.TimeseriesFilter != nil,
		"measurement_filter": req.MeasurementFilter != nil,
		"limit":             req.Limit,
	}).Info("Processing sensor discovery request")

	// Build and execute the discovery query
	sensors, err := h.repo.DiscoverSensorsByConditions(&req)
	if err != nil {
		logrus.WithError(err).Error("Failed to discover sensors")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to discover sensors", "details": err.Error()})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"sensors": sensors,
		"count":   len(sensors),
		"request": req,
	})
}

// DiscoverSensorsLegacy provides backward compatibility with query parameters
// @Summary Discover sensors (legacy query params)
// @Description Find sensors using query parameters for backward compatibility
// @Tags sensors
// @Produce json
// @Param type_names query string false "Required types (comma-separated)"
// @Param optional_types query string false "Optional types (comma-separated)"
// @Param dataset_ids query string false "Dataset IDs (comma-separated)"
// @Param value_filter query string false "Value filter expression (type.operator.value)"
// @Param start_time query string false "Start time (RFC3339)"
// @Param end_time query string false "End time (RFC3339)"
// @Param latest_only query boolean false "Only consider latest measurements"
// @Param limit query int false "Maximum results"
// @Success 200 {object} map[string]interface{} "Discovered sensors"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /sensors/discover [get]
func (h *SensorDiscoveryHandler) DiscoverSensorsLegacy(c *gin.Context) {
	req := &filter.SensorDiscoveryRequest{}

	// Initialize timeseries filter if needed
	if typeNamesStr := c.Query("type_names"); typeNamesStr != "" {
		if req.TimeseriesFilter == nil {
			req.TimeseriesFilter = &filter.TimeseriesFilter{}
		}
		req.TimeseriesFilter.RequiredTypes = h.parseCommaSeparated(typeNamesStr)
	}

	if optionalTypesStr := c.Query("optional_types"); optionalTypesStr != "" {
		if req.TimeseriesFilter == nil {
			req.TimeseriesFilter = &filter.TimeseriesFilter{}
		}
		req.TimeseriesFilter.OptionalTypes = h.parseCommaSeparated(optionalTypesStr)
	}

	if datasetIDsStr := c.Query("dataset_ids"); datasetIDsStr != "" {
		if req.TimeseriesFilter == nil {
			req.TimeseriesFilter = &filter.TimeseriesFilter{}
		}
		req.TimeseriesFilter.DatasetIDs = h.parseCommaSeparated(datasetIDsStr)
	}

	// Initialize measurement filter if needed
	if valueFilterStr := c.Query("value_filter"); valueFilterStr != "" {
		if req.MeasurementFilter == nil {
			req.MeasurementFilter = &filter.MeasurementFilter{}
		}
		req.MeasurementFilter.Expression = valueFilterStr
	}

	// Parse time range
	if startTimeStr := c.Query("start_time"); startTimeStr != "" {
		if req.MeasurementFilter == nil {
			req.MeasurementFilter = &filter.MeasurementFilter{}
		}
		if req.MeasurementFilter.TimeRange == nil {
			req.MeasurementFilter.TimeRange = &filter.TimeRange{}
		}
		req.MeasurementFilter.TimeRange.StartTime = startTimeStr
	}

	if endTimeStr := c.Query("end_time"); endTimeStr != "" {
		if req.MeasurementFilter == nil {
			req.MeasurementFilter = &filter.MeasurementFilter{}
		}
		if req.MeasurementFilter.TimeRange == nil {
			req.MeasurementFilter.TimeRange = &filter.TimeRange{}
		}
		req.MeasurementFilter.TimeRange.EndTime = endTimeStr
	}

	// Parse latest only flag
	if latestStr := c.Query("latest_only"); latestStr != "" {
		if req.MeasurementFilter == nil {
			req.MeasurementFilter = &filter.MeasurementFilter{}
		}
		req.MeasurementFilter.LatestOnly = latestStr == "true"
	}

	// Parse limit
	if limitStr := c.Query("limit"); limitStr != "" {
		if limit, err := strconv.Atoi(limitStr); err == nil && limit > 0 {
			req.Limit = limit
		}
	}

	// Execute discovery
	sensors, err := h.repo.DiscoverSensorsByConditions(req)
	if err != nil {
		logrus.WithError(err).Error("Failed to discover sensors")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to discover sensors", "details": err.Error()})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"sensors": sensors,
		"count":   len(sensors),
	})
}

// parseCommaSeparated splits a comma-separated string and trims whitespace
func (h *SensorDiscoveryHandler) parseCommaSeparated(input string) []string {
	parts := strings.Split(input, ",")
	result := make([]string, 0, len(parts))
	for _, part := range parts {
		if trimmed := strings.TrimSpace(part); trimmed != "" {
			result = append(result, trimmed)
		}
	}
	return result
}

// VerifySensors verifies if given sensor names match the discovery filters
// @Summary Verify sensors against discovery filters
// @Description Verify if a list of sensor names satisfy the same filters used in sensor discovery
// @Tags sensors
// @Accept json
// @Produce json
// @Param request body filter.SensorVerifyRequest true "Sensor verification request with filters and sensor names"
// @Success 200 {object} filter.SensorVerifyResponse "Verification results with ok status and verified/unverified lists"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /sensors/verify [post]
func (h *SensorDiscoveryHandler) VerifySensors(c *gin.Context) {
	var req filter.SensorVerifyRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid request format", "details": err.Error()})
		return
	}

	// Validate that sensor names are provided
	if len(req.SensorNames) == 0 {
		c.JSON(http.StatusBadRequest, gin.H{"error": "sensor_names list cannot be empty"})
		return
	}

	logrus.WithFields(logrus.Fields{
		"timeseries_filter":  req.TimeseriesFilter != nil,
		"measurement_filter": req.MeasurementFilter != nil,
		"sensor_count":      len(req.SensorNames),
		"sensors":           req.SensorNames,
	}).Info("Processing sensor verification request")

	// Execute verification using repository method
	response, err := h.repo.VerifyDiscoveredSensors(&req)
	if err != nil {
		logrus.WithError(err).Error("Failed to verify sensors")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to verify sensors", "details": err.Error()})
		return
	}

	logrus.WithFields(logrus.Fields{
		"all_ok":           response.OK,
		"verified_count":   len(response.Verified),
		"unverified_count": len(response.Unverified),
	}).Info("Sensor verification completed")

	c.JSON(http.StatusOK, response)
}

// GetSensorTimeseries retrieves all timeseries for a specific sensor
// @Summary Get timeseries for a sensor
// @Description Get all timeseries associated with a sensor, optionally filtered by type names
// @Tags sensors
// @Produce json
// @Param name path string true "Sensor name"
// @Param type_names query string false "Type names to filter (comma-separated)"
// @Success 200 {object} models.SensorTimeseriesResponse "Sensor with timeseries info"
// @Failure 404 {object} map[string]interface{} "Sensor not found"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /sensors/{name} [get]
func (h *SensorDiscoveryHandler) GetSensorTimeseries(c *gin.Context) {
	sensorName := c.Param("name")

	// Parse optional type_names query parameter
	var typeNames []string
	if typeNamesStr := c.Query("type_names"); typeNamesStr != "" {
		typeNames = h.parseCommaSeparated(typeNamesStr)
	}

	logrus.WithFields(logrus.Fields{
		"sensor_name": sensorName,
		"type_names":  typeNames,
	}).Info("Getting sensor timeseries")

	// Fetch sensor timeseries from repository
	result, err := h.repo.GetSensorTimeseriesByName(sensorName, typeNames)
	if err != nil {
		if err.Error() == "sensor '"+sensorName+"' not found" {
			c.JSON(http.StatusNotFound, gin.H{"error": "Sensor not found", "sensor_name": sensorName})
			return
		}
		logrus.WithError(err).Error("Failed to get sensor timeseries")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get sensor timeseries", "details": err.Error()})
		return
	}

	c.JSON(http.StatusOK, result)
}

// BatchSensorTimeseriesRequest is the request body for batch sensor timeseries
type BatchSensorTimeseriesRequest struct {
	SensorNames []string `json:"sensor_names" binding:"required"`
	TypeNames   []string `json:"type_names,omitempty"`
}

// GetBatchSensorTimeseries retrieves timeseries for multiple sensors
// @Summary Get timeseries for multiple sensors
// @Description Get timeseries for a batch of sensors, optionally filtered by type names
// @Tags sensors
// @Accept json
// @Produce json
// @Param request body BatchSensorTimeseriesRequest true "Batch request with sensor names and optional type names"
// @Success 200 {object} models.BatchSensorTimeseriesResponse "Batch response with sensors and their timeseries"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /sensors/timeseries [post]
func (h *SensorDiscoveryHandler) GetBatchSensorTimeseries(c *gin.Context) {
	var req BatchSensorTimeseriesRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid request format", "details": err.Error()})
		return
	}

	if len(req.SensorNames) == 0 {
		c.JSON(http.StatusBadRequest, gin.H{"error": "sensor_names array cannot be empty"})
		return
	}

	logrus.WithFields(logrus.Fields{
		"sensor_count": len(req.SensorNames),
		"type_names":   req.TypeNames,
	}).Info("Getting batch sensor timeseries")

	// Fetch batch sensor timeseries from repository
	result, err := h.repo.GetBatchSensorTimeseries(req.SensorNames, req.TypeNames)
	if err != nil {
		logrus.WithError(err).Error("Failed to get batch sensor timeseries")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get batch sensor timeseries", "details": err.Error()})
		return
	}

	c.JSON(http.StatusOK, result)
}

