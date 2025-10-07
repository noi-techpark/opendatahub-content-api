package handlers

import (
	"encoding/json"
	"fmt"
	"net/http"
	"strconv"

	"timeseries-api/internal/models"
	"timeseries-api/internal/repository"

	"github.com/gin-gonic/gin"
	"github.com/paulmach/orb"
	"github.com/paulmach/orb/encoding/wkt"
	"github.com/paulmach/orb/geojson"
	"github.com/sirupsen/logrus"
)

type MutationHandler struct {
	repo *repository.Repository
}

func NewMutationHandler(repo *repository.Repository) *MutationHandler {
	return &MutationHandler{repo: repo}
}

// BatchInsert handles batch insertion of measurements
// @Summary Batch insert measurements
// @Description Insert multiple measurements in batch for optimal performance with configurable batch sizes
// @Tags measurements
// @Accept json
// @Produce json
// @Param request body models.BatchDataRequest true "Batch measurements request"
// @Success 200 {object} map[string]interface{} "Batch insert successful"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /measurements/batch [post]
func (h *MutationHandler) BatchInsert(c *gin.Context) {
	var req models.BatchDataRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid request format", "details": err.Error()})
		return
	}

	if len(req.Measurements) == 0 {
		c.JSON(http.StatusBadRequest, gin.H{"error": "No measurements provided"})
		return
	}

	var provenance *models.Provenance
	var err error

	if req.Provenance != nil {
		provenance, err = h.repo.GetOrCreateProvenance(
			req.Provenance.Lineage,
			req.Provenance.DataCollector,
			req.Provenance.DataCollectorVersion,
		)
		if err != nil {
			logrus.WithError(err).Error("Failed to get or create provenance")
			c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to process provenance"})
			return
		}
	}

	// Process measurements in batches for better performance
	err = h.processBatchMeasurements(req.Measurements, provenance)
	if err != nil {
		logrus.WithError(err).Error("Failed to process batch measurements")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to process measurements", "details": err.Error()})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"processed": len(req.Measurements),
		"total":     len(req.Measurements),
		"message":   "All measurements processed successfully",
	})
}

func (h *MutationHandler) processBatchMeasurements(measurements []models.MeasurementWithMeta, provenance *models.Provenance) error {
	// Group measurements by data type for efficient batch processing
	measurementsByType := make(map[models.DataType][]models.Measurement)

	for _, measurement := range measurements {
		// Get or create sensor
		sensor, err := h.repo.GetOrCreateSensor(
			measurement.SensorName,
			nil,                   // parent_id
			json.RawMessage("{}"), // metadata (empty JSON object)
		)
		if err != nil {
			return fmt.Errorf("failed to get or create sensor %s: %w", measurement.SensorName, err)
		}

		// Determine data type from value
		dataType, err := h.inferDataType(measurement.Value)
		if err != nil {
			return fmt.Errorf("failed to infer data type for sensor %s: %w", measurement.SensorName, err)
		}

		// Get or create type
		typeRecord, err := h.repo.GetOrCreateType(
			measurement.TypeName,
			"", // description
			"", // unit
			dataType,
			json.RawMessage("{}"), // metadata (empty JSON object)
		)
		if err != nil {
			return fmt.Errorf("failed to get or create type %s: %w", measurement.TypeName, err)
		}

		// Get or create timeseries
		timeseries, err := h.repo.GetOrCreateTimeseries(
			sensor.ID,
			typeRecord.ID,
		)
		if err != nil {
			return fmt.Errorf("failed to get or create timeseries for sensor %s, type %s: %w", measurement.SensorName, measurement.TypeName, err)
		}

		// Convert value based on data type
		convertedValue, err := h.convertValue(measurement.Value, dataType)
		if err != nil {
			return fmt.Errorf("failed to convert value for sensor %s: %w", measurement.SensorName, err)
		}

		var provenanceID *int64
		if provenance != nil {
			provenanceID = &provenance.ID
		}

		// Add to batch
		batchMeasurement := models.Measurement{
			TimeseriesID: timeseries.ID,
			Timestamp:    measurement.Timestamp,
			Value:        convertedValue,
			ProvenanceID: provenanceID,
		}

		measurementsByType[dataType] = append(measurementsByType[dataType], batchMeasurement)
	}

	// Insert measurements in batches
	return h.repo.BatchInsertMeasurements(measurementsByType, 1000)
}

func (h *MutationHandler) inferDataType(value interface{}) (models.DataType, error) {
	switch v := value.(type) {
	case bool:
		return models.DataTypeBoolean, nil
	case float64, int, int64:
		return models.DataTypeNumeric, nil
	case string:
		return models.DataTypeString, nil
	case map[string]interface{}:
		// Check if it's a GeoJSON point
		if v["type"] == "Point" && v["coordinates"] != nil {
			return models.DataTypeGeoposition, nil
		}
		// Check if it's a GeoJSON polygon
		if v["type"] == "Polygon" && v["coordinates"] != nil {
			return models.DataTypeGeoshape, nil
		}
		return models.DataTypeJSON, nil
	case []interface{}:
		return models.DataTypeJSON, nil
	default:
		return models.DataTypeString, nil
	}
}

func (h *MutationHandler) convertValue(value interface{}, dataType models.DataType) (interface{}, error) {
	switch dataType {
	case models.DataTypeGeoposition:
		if geoMap, ok := value.(map[string]interface{}); ok {
			geoJSON, err := json.Marshal(geoMap)
			if err != nil {
				return nil, err
			}

			feature, err := geojson.UnmarshalGeometry(geoJSON)
			if err != nil {
				return nil, err
			}

			if point, ok := feature.Geometry().(orb.Point); ok {
				// Correctly return the WKT as a string and a nil error
				return string(wkt.Marshal(point)), nil
			}
		}
		return nil, fmt.Errorf("invalid geoposition format")

	case models.DataTypeGeoshape:
		if geoMap, ok := value.(map[string]interface{}); ok {
			geoJSON, err := json.Marshal(geoMap)
			if err != nil {
				return nil, err
			}

			feature, err := geojson.UnmarshalGeometry(geoJSON)
			if err != nil {
				return nil, err
			}

			if polygon, ok := feature.Geometry().(orb.Polygon); ok {
				// Correctly return the WKT as a string and a nil error
				return string(wkt.Marshal(polygon)), nil
			}
		}
		return nil, fmt.Errorf("invalid geoshape format")

	case models.DataTypeJSON:
		jsonBytes, err := json.Marshal(value)
		if err != nil {
			return nil, err
		}
		return json.RawMessage(jsonBytes), nil
	case models.DataTypeNumeric:
		switch v := value.(type) {
		case float64:
			return v, nil
		case int:
			return float64(v), nil
		case int64:
			return float64(v), nil
		case string:
			if f, err := strconv.ParseFloat(v, 64); err == nil {
				return f, nil
			}
		}
		return 0.0, nil
	case models.DataTypeBoolean:
		if b, ok := value.(bool); ok {
			return b, nil
		}
		if s, ok := value.(string); ok {
			return s == "true" || s == "1" || s == "yes", nil
		}
		return false, nil
	case models.DataTypeString:
		if s, ok := value.(string); ok {
			return s, nil
		}
		jsonBytes, _ := json.Marshal(value)
		return string(jsonBytes), nil
	}

	return value, nil
}

// Delete handles deletion of measurements
// @Summary Delete measurements by filters
// @Description Delete measurements based on sensor names, type names, and time range filters
// @Tags measurements
// @Accept json
// @Produce json
// @Param request body models.DeleteMeasurementsRequest true "Delete measurements request"
// @Success 200 {object} map[string]interface{} "Measurements deleted successfully"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /measurements [delete]
func (h *MutationHandler) Delete(c *gin.Context) {
	var req models.DeleteMeasurementsRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid request format", "details": err.Error()})
		return
	}

	// Validate that at least one filter is provided
	if len(req.SensorNames) == 0 && len(req.TypeNames) == 0 &&
		req.StartTime == nil && req.EndTime == nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "At least one filter must be provided"})
		return
	}

	err := h.repo.DeleteMeasurements(
		req.SensorNames,
		req.TypeNames,
		req.StartTime,
		req.EndTime,
	)
	if err != nil {
		logrus.WithError(err).Error("Failed to delete measurements")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to delete measurements"})
		return
	}

	c.JSON(http.StatusOK, gin.H{"message": "Measurements deleted successfully"})
}
