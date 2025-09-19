package handlers

import (
	"net/http"
	"strconv"
	"strings"
	"time"

	"timeseries-api/internal/models"
	"timeseries-api/internal/repository"

	"github.com/gin-gonic/gin"
	"github.com/sirupsen/logrus"
)

type QueryHandler struct {
	repo *repository.Repository
}

func NewQueryHandler(repo *repository.Repository) *QueryHandler {
	return &QueryHandler{repo: repo}
}

// GetLatestMeasurements gets the latest measurement for specified sensors and timeseries
// @Summary Get latest measurements (JSON body)
// @Description Retrieve the latest measurements for specified sensors and measurement types using JSON request body
// @Tags measurements
// @Accept json
// @Produce json
// @Param request body models.LatestMeasurementsRequest true "Latest measurements request"
// @Success 200 {object} map[string]interface{} "Latest measurements data"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /measurements/latest [post]
func (h *QueryHandler) GetLatestMeasurements(c *gin.Context) {
	var req models.LatestMeasurementsRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid request format", "details": err.Error()})
		return
	}

	if len(req.SensorNames) == 0 {
		c.JSON(http.StatusBadRequest, gin.H{"error": "At least one sensor name must be provided"})
		return
	}

	measurements, err := h.repo.GetLatestMeasurements(req.SensorNames, req.TypeNames)
	if err != nil {
		logrus.WithError(err).Error("Failed to get latest measurements")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to retrieve measurements"})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"measurements": measurements,
		"count":        len(measurements),
	})
}

// GetLatestMeasurementsQuery gets the latest measurement using query parameters
// @Summary Get latest measurements (query params)
// @Description Retrieve the latest measurements for specified sensors and measurement types using query parameters
// @Tags measurements
// @Produce json
// @Param sensor_names query string true "Comma-separated list of sensor names"
// @Param type_names query string false "Comma-separated list of measurement type names"
// @Success 200 {object} map[string]interface{} "Latest measurements data"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /measurements/latest [get]
func (h *QueryHandler) GetLatestMeasurementsQuery(c *gin.Context) {
	sensorNamesStr := c.Query("sensor_names")
	typeNamesStr := c.Query("type_names")

	if sensorNamesStr == "" {
		c.JSON(http.StatusBadRequest, gin.H{"error": "sensor_names parameter is required"})
		return
	}

	sensorNames := parseCommaSeparated(sensorNamesStr)
	typeNames := parseCommaSeparated(typeNamesStr)

	measurements, err := h.repo.GetLatestMeasurements(sensorNames, typeNames)
	if err != nil {
		logrus.WithError(err).Error("Failed to get latest measurements")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to retrieve measurements"})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"measurements": measurements,
		"count":        len(measurements),
	})
}

// GetHistoricalMeasurements gets historical measurements for specified sensors and timeseries
// @Summary Get historical measurements (JSON body)
// @Description Retrieve historical measurements for specified sensors and measurement types with time range filtering using JSON request body
// @Tags measurements
// @Accept json
// @Produce json
// @Param request body models.HistoricalMeasurementsRequest true "Historical measurements request"
// @Success 200 {object} map[string]interface{} "Historical measurements data"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /measurements/historical [post]
func (h *QueryHandler) GetHistoricalMeasurements(c *gin.Context) {
	var req models.HistoricalMeasurementsRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid request format", "details": err.Error()})
		return
	}

	if len(req.SensorNames) == 0 {
		c.JSON(http.StatusBadRequest, gin.H{"error": "At least one sensor name must be provided"})
		return
	}

	measurements, err := h.repo.GetHistoricalMeasurements(
		req.SensorNames,
		req.TypeNames,
		req.StartTime,
		req.EndTime,
		req.Limit,
	)
	if err != nil {
		logrus.WithError(err).Error("Failed to get historical measurements")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to retrieve measurements"})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"measurements": measurements,
		"count":        len(measurements),
	})
}

// GetHistoricalMeasurementsQuery gets historical measurements using query parameters
// @Summary Get historical measurements (query params)
// @Description Retrieve historical measurements for specified sensors and measurement types with time range filtering using query parameters
// @Tags measurements
// @Produce json
// @Param sensor_names query string true "Comma-separated list of sensor names"
// @Param type_names query string false "Comma-separated list of measurement type names"
// @Param start_time query string false "Start time in RFC3339 format"
// @Param end_time query string false "End time in RFC3339 format"
// @Param limit query int false "Maximum number of results"
// @Success 200 {object} map[string]interface{} "Historical measurements data"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /measurements/historical [get]
func (h *QueryHandler) GetHistoricalMeasurementsQuery(c *gin.Context) {
	sensorNamesStr := c.Query("sensor_names")
	typeNamesStr := c.Query("type_names")
	startTimeStr := c.Query("start_time")
	endTimeStr := c.Query("end_time")
	limitStr := c.Query("limit")

	if sensorNamesStr == "" {
		c.JSON(http.StatusBadRequest, gin.H{"error": "sensor_names parameter is required"})
		return
	}

	sensorNames := parseCommaSeparated(sensorNamesStr)
	typeNames := parseCommaSeparated(typeNamesStr)

	var startTime, endTime *time.Time
	var err error

	if startTimeStr != "" {
		t, err := time.Parse(time.RFC3339, startTimeStr)
		if err != nil {
			c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid start_time format, use RFC3339"})
			return
		}
		startTime = &t
	}

	if endTimeStr != "" {
		t, err := time.Parse(time.RFC3339, endTimeStr)
		if err != nil {
			c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid end_time format, use RFC3339"})
			return
		}
		endTime = &t
	}

	limit := 0
	if limitStr != "" {
		limit, err = strconv.Atoi(limitStr)
		if err != nil || limit < 0 {
			c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid limit parameter"})
			return
		}
	}

	measurements, err := h.repo.GetHistoricalMeasurements(
		sensorNames,
		typeNames,
		startTime,
		endTime,
		limit,
	)
	if err != nil {
		logrus.WithError(err).Error("Failed to get historical measurements")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to retrieve measurements"})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"measurements": measurements,
		"count":        len(measurements),
	})
}

// Health check endpoint
// @Summary Health check
// @Description Check the health status of the API
// @Tags system
// @Produce json
// @Success 200 {object} map[string]interface{} "API is healthy"
// @Router /health [get]
func (h *QueryHandler) Health(c *gin.Context) {
	c.JSON(http.StatusOK, gin.H{
		"status":    "healthy",
		"timestamp": time.Now().UTC(),
		"service":   "timeseries-api",
	})
}

// Helper function to parse comma-separated values
func parseCommaSeparated(s string) []string {
	if s == "" {
		return nil
	}

	var result []string
	for _, item := range strings.Split(s, ",") {
		item = strings.TrimSpace(item)
		if item != "" {
			result = append(result, item)
		}
	}
	return result
}
