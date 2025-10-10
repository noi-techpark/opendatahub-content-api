package handlers

import (
	"net/http"
	"strconv"

	"timeseries-api/internal/models"
	"timeseries-api/internal/repository"

	"github.com/gin-gonic/gin"
	"github.com/sirupsen/logrus"
)

type TypeHandler struct {
	repo *repository.Repository
}

func NewTypeHandler(repo *repository.Repository) *TypeHandler {
	return &TypeHandler{repo: repo}
}

// ListTypes lists all types with pagination and optional sensor inclusion
// @Summary List all types
// @Description Get a paginated list of all types, optionally including sensors that have timeseries for each type
// @Tags types
// @Produce json
// @Param offset query int false "Offset for pagination" default(0)
// @Param limit query int false "Limit for pagination" default(50)
// @Param include_sensors query boolean false "Include sensors with timeseries for each type"
// @Success 200 {object} models.ListTypesResponse "List of types with pagination info"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /types [get]
func (h *TypeHandler) ListTypes(c *gin.Context) {
	// Parse pagination parameters
	offset := 0
	limit := 50

	if offsetStr := c.Query("offset"); offsetStr != "" {
		if val, err := strconv.Atoi(offsetStr); err == nil && val >= 0 {
			offset = val
		} else {
			c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid offset parameter"})
			return
		}
	}

	if limitStr := c.Query("limit"); limitStr != "" {
		if val, err := strconv.Atoi(limitStr); err == nil && val > 0 {
			limit = val
		} else {
			c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid limit parameter"})
			return
		}
	}

	// Check if sensors should be included
	includeSensors := c.Query("include_sensors") == "true"

	logrus.WithFields(logrus.Fields{
		"offset":          offset,
		"limit":           limit,
		"include_sensors": includeSensors,
	}).Info("Listing types")

	// Fetch types from repository
	types, total, err := h.repo.ListTypes(offset, limit, includeSensors)
	if err != nil {
		logrus.WithError(err).Error("Failed to list types")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to list types", "details": err.Error()})
		return
	}

	response := models.ListTypesResponse{
		Types:  types,
		Total:  total,
		Offset: offset,
		Limit:  limit,
	}

	c.JSON(http.StatusOK, response)
}

// GetType retrieves a specific type by name with all sensors having timeseries for this type
// @Summary Get a type by name
// @Description Get a specific type by name along with all sensors that have timeseries for this type
// @Tags types
// @Produce json
// @Param name path string true "Type name"
// @Success 200 {object} models.TypeWithSensors "Type with sensors and timeseries info"
// @Failure 404 {object} map[string]interface{} "Type not found"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /types/{name} [get]
func (h *TypeHandler) GetType(c *gin.Context) {
	typeName := c.Param("name")

	logrus.WithField("type_name", typeName).Info("Getting type by name")

	// Fetch type with sensors from repository
	typeWithSensors, err := h.repo.GetTypeByName(typeName)
	if err != nil {
		if err.Error() == "type '"+typeName+"' not found" {
			c.JSON(http.StatusNotFound, gin.H{"error": "Type not found", "type_name": typeName})
			return
		}
		logrus.WithError(err).Error("Failed to get type")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get type", "details": err.Error()})
		return
	}

	c.JSON(http.StatusOK, typeWithSensors)
}
