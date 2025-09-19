package handlers

import (
	"net/http"

	"timeseries-api/internal/models"
	"timeseries-api/internal/repository"

	"github.com/gin-gonic/gin"
	"github.com/google/uuid"
	"github.com/sirupsen/logrus"
)

type DatasetHandler struct {
	repo *repository.Repository
}

func NewDatasetHandler(repo *repository.Repository) *DatasetHandler {
	return &DatasetHandler{repo: repo}
}

// CreateDataset creates a new dataset
// @Summary Create new dataset
// @Description Create a new dataset with optional measurement type associations
// @Tags datasets
// @Accept json
// @Produce json
// @Param request body models.CreateDatasetRequest true "Create dataset request"
// @Success 201 {object} models.DatasetResponse "Dataset created successfully"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /datasets [post]
func (h *DatasetHandler) CreateDataset(c *gin.Context) {
	var req models.CreateDatasetRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid request format", "details": err.Error()})
		return
	}

	if req.Name == "" {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Dataset name is required"})
		return
	}

	// Create the dataset
	dataset, err := h.repo.CreateDataset(req.Name, req.Description)
	if err != nil {
		logrus.WithError(err).Error("Failed to create dataset")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to create dataset"})
		return
	}

	// Add types to the dataset if specified
	if len(req.TypeNames) > 0 {
		err = h.repo.AddTypesToDataset(dataset.ID, req.TypeNames, true)
		if err != nil {
			logrus.WithError(err).Error("Failed to add types to dataset")
			c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to add types to dataset"})
			return
		}
	}

	// Return the complete dataset with types
	datasetResponse, err := h.repo.GetDatasetWithTypes(dataset.ID)
	if err != nil {
		logrus.WithError(err).Error("Failed to get created dataset with types")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to retrieve created dataset"})
		return
	}

	c.JSON(http.StatusCreated, datasetResponse)
}

// GetDataset retrieves a dataset with its types
// @Summary Get dataset details
// @Description Retrieve dataset details with all associated measurement types
// @Tags datasets
// @Produce json
// @Param id path string true "Dataset ID (UUID)"
// @Success 200 {object} models.DatasetResponse "Dataset details with types"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Failure 404 {object} map[string]interface{} "Dataset not found"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /datasets/{id} [get]
func (h *DatasetHandler) GetDataset(c *gin.Context) {
	datasetIDStr := c.Param("id")
	datasetID, err := uuid.Parse(datasetIDStr)
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid dataset ID format"})
		return
	}

	datasetResponse, err := h.repo.GetDatasetWithTypes(datasetID)
	if err != nil {
		logrus.WithError(err).Error("Failed to get dataset")
		c.JSON(http.StatusNotFound, gin.H{"error": "Dataset not found"})
		return
	}

	c.JSON(http.StatusOK, datasetResponse)
}

// AddTypesToDataset adds types to an existing dataset
// @Summary Add measurement types to dataset
// @Description Add measurement types to an existing dataset with required/optional flag
// @Tags datasets
// @Accept json
// @Produce json
// @Param id path string true "Dataset ID (UUID)"
// @Param request body models.AddTypesToDatasetRequest true "Add types request"
// @Success 200 {object} map[string]interface{} "Types added successfully"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Failure 404 {object} map[string]interface{} "Dataset not found"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /datasets/{id}/types [post]
func (h *DatasetHandler) AddTypesToDataset(c *gin.Context) {
	datasetIDStr := c.Param("id")
	datasetID, err := uuid.Parse(datasetIDStr)
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid dataset ID format"})
		return
	}

	var req models.AddTypesToDatasetRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid request format", "details": err.Error()})
		return
	}

	if len(req.TypeNames) == 0 {
		c.JSON(http.StatusBadRequest, gin.H{"error": "At least one type name must be provided"})
		return
	}

	err = h.repo.AddTypesToDataset(datasetID, req.TypeNames, req.IsRequired)
	if err != nil {
		logrus.WithError(err).Error("Failed to add types to dataset")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to add types to dataset"})
		return
	}

	c.JSON(http.StatusOK, gin.H{"message": "Types added to dataset successfully"})
}

// RemoveTypesFromDataset removes types from a dataset
// @Summary Remove measurement types from dataset
// @Description Remove measurement types from an existing dataset
// @Tags datasets
// @Accept json
// @Produce json
// @Param id path string true "Dataset ID (UUID)"
// @Param type_names query string true "Comma-separated list of type names to remove"
// @Success 200 {object} map[string]interface{} "Types removed successfully"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Failure 404 {object} map[string]interface{} "Dataset not found"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /datasets/{id}/types [delete]
func (h *DatasetHandler) RemoveTypesFromDataset(c *gin.Context) {
	datasetIDStr := c.Param("id")
	datasetID, err := uuid.Parse(datasetIDStr)
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid dataset ID format"})
		return
	}

	var req struct {
		TypeNames []string `json:"type_names"`
	}
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid request format", "details": err.Error()})
		return
	}

	if len(req.TypeNames) == 0 {
		c.JSON(http.StatusBadRequest, gin.H{"error": "At least one type name must be provided"})
		return
	}

	err = h.repo.RemoveTypesFromDataset(datasetID, req.TypeNames)
	if err != nil {
		logrus.WithError(err).Error("Failed to remove types from dataset")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to remove types from dataset"})
		return
	}

	c.JSON(http.StatusOK, gin.H{"message": "Types removed from dataset successfully"})
}

// GetSensorsByDataset finds all sensors that have timeseries in a dataset
// @Summary Get all sensors that have timeseries in dataset
// @Description Retrieve all sensors that have timeseries associated with a specific dataset
// @Tags datasets
// @Produce json
// @Param id path string true "Dataset ID (UUID)"
// @Success 200 {object} map[string]interface{} "Sensors in the dataset"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Failure 404 {object} map[string]interface{} "Dataset not found"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /datasets/{id}/sensors [get]
func (h *DatasetHandler) GetSensorsByDataset(c *gin.Context) {
	datasetIDStr := c.Param("id")
	datasetID, err := uuid.Parse(datasetIDStr)
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid dataset ID format"})
		return
	}

	sensors, err := h.repo.FindSensorsByDatasetUpdated(datasetID)
	if err != nil {
		logrus.WithError(err).Error("Failed to find sensors by dataset")
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to retrieve sensors"})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"sensors": sensors,
		"count":   len(sensors),
	})
}
