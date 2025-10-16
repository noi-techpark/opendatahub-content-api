package main

import (
	"context"
	"fmt"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	_ "timeseries-api/docs" // swagger docs
	"timeseries-api/internal/config"
	"timeseries-api/internal/handlers"
	"timeseries-api/internal/middleware"
	"timeseries-api/internal/repository"
	"timeseries-api/internal/streaming"
	"timeseries-api/pkg/database"

	"github.com/gin-gonic/gin"
	"github.com/joho/godotenv"
	"github.com/sirupsen/logrus"
	swaggerFiles "github.com/swaggo/files"
	ginSwagger "github.com/swaggo/gin-swagger"
)

// @title Timeseries API
// @version 1.0.0
// @description A Go REST API server for managing timeseries data using Gin framework and PostgreSQL with a simplified, performance-optimized schema.
// @termsOfService http://swagger.io/terms/

// @contact.name API Support
// @contact.url http://www.example.com/support
// @contact.email support@example.com

// @license.name MIT
// @license.url https://opensource.org/licenses/MIT

// @host localhost:8080
// @BasePath /api/v1

// @schemes http https
// @produce json
// @consumes json

func main() {
	// Load .env file
	if err := godotenv.Load(); err != nil {
		logrus.WithError(err).Warn("No .env file found, using environment variables")
	}

	// Load configuration
	cfg, err := config.Load()
	if err != nil {
		logrus.WithError(err).Fatal("Failed to load configuration")
	}

	// Setup logging
	setupLogging(cfg.Logging.Level)

	logrus.Info("Starting timeseries API server")

	// Connect to database
	db, err := database.Connect(cfg.Database)
	if err != nil {
		logrus.WithError(err).Fatal("Failed to connect to database")
	}
	defer db.Close()

	// Initialize repository
	repo := repository.New(db)

	// Initialize Materialize client
	materializeClient, err := streaming.NewMaterializeClient(streaming.MaterializeConfig{
		Host:     "localhost",
		Port:     6875,
		User:     "materialize",
		Password: "",
		Database: "materialize",
	})
	if err != nil {
		logrus.WithError(err).Warn("Failed to connect to Materialize, streaming features will be unavailable")
		materializeClient = nil
	}
	if materializeClient != nil {
		defer materializeClient.Close()

		// Wait for initial sync
		syncCtx, syncCancel := context.WithTimeout(context.Background(), 30*time.Second)
		defer syncCancel()
		if err := materializeClient.WaitForInitialSync(syncCtx); err != nil {
			logrus.WithError(err).Warn("Materialize initial sync incomplete, continuing anyway")
		}
	}

	// Initialize handlers
	mutationHandler := handlers.NewMutationHandler(repo)
	queryHandler := handlers.NewQueryHandler(repo)
	datasetHandler := handlers.NewDatasetHandler(repo)
	sensorDiscoveryHandler := handlers.NewSensorDiscoveryHandler(repo)
	typeHandler := handlers.NewTypeHandler(repo)

	// Initialize streaming handler if Materialize is available
	var streamingHandler *handlers.StreamingHandler
	if materializeClient != nil {
		wsManager := streaming.NewWebSocketManager(materializeClient, repo)
		streamingHandler = handlers.NewStreamingHandler(wsManager)
	}

	// Setup Gin router
	router := setupRouter(mutationHandler, queryHandler, datasetHandler, sensorDiscoveryHandler, typeHandler, streamingHandler)

	// Setup HTTP server
	srv := &http.Server{
		Addr:         fmt.Sprintf(":%d", cfg.Server.Port),
		Handler:      router,
		ReadTimeout:  cfg.Server.ReadTimeout,
		WriteTimeout: cfg.Server.WriteTimeout,
	}

	// Start server in a goroutine
	go func() {
		logrus.WithField("port", cfg.Server.Port).Info("Starting HTTP server")
		if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			logrus.WithError(err).Fatal("Failed to start HTTP server")
		}
	}()

	// Wait for interrupt signal to gracefully shutdown the server
	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	logrus.Info("Shutting down server...")

	// Create a context with timeout for graceful shutdown
	ctx, cancel := context.WithTimeout(context.Background(), cfg.Server.ShutdownTimeout)
	defer cancel()

	// Shutdown server
	if err := srv.Shutdown(ctx); err != nil {
		logrus.WithError(err).Fatal("Server forced to shutdown")
	}

	logrus.Info("Server exited")
}

func setupLogging(level string) {
	logrus.SetFormatter(&logrus.JSONFormatter{
		TimestampFormat: time.RFC3339,
	})

	logLevel, err := logrus.ParseLevel(level)
	if err != nil {
		logrus.WithError(err).Warn("Invalid log level, using info")
		logLevel = logrus.InfoLevel
	}
	logrus.SetLevel(logLevel)
}

func setupRouter(mutationHandler *handlers.MutationHandler, queryHandler *handlers.QueryHandler, datasetHandler *handlers.DatasetHandler, sensorDiscoveryHandler *handlers.SensorDiscoveryHandler, typeHandler *handlers.TypeHandler, streamingHandler *handlers.StreamingHandler) *gin.Engine {
	// Set Gin mode based on log level
	if logrus.GetLevel() == logrus.DebugLevel {
		gin.SetMode(gin.DebugMode)
	} else {
		gin.SetMode(gin.ReleaseMode)
	}

	router := gin.New()

	// Apply middleware
	router.Use(middleware.Logger())
	router.Use(middleware.RequestLogger())
	router.Use(middleware.ErrorHandler())
	router.Use(middleware.CORS())
	router.Use(middleware.SecurityHeaders())

	// API v1 routes
	v1 := router.Group("/api/v1")
	{
		// Health endpoint
		v1.GET("/health", queryHandler.Health)

		// Measurement endpoints
		measurements := v1.Group("/measurements")
		{
			// Mutations
			measurements.POST("/batch", mutationHandler.BatchInsert)
			measurements.DELETE("", mutationHandler.Delete)

			// Queries
			measurements.GET("/latest", queryHandler.GetLatestMeasurementsQuery)
			measurements.POST("/latest", queryHandler.GetLatestMeasurements)
			measurements.GET("/historical", queryHandler.GetHistoricalMeasurementsQuery)
			measurements.POST("/historical", queryHandler.GetHistoricalMeasurements)

			// Streaming subscription (WebSocket) - GraphQL-style connection_init
			if streamingHandler != nil {
				measurements.GET("/subscribe", streamingHandler.SubscribeToMeasurements)
				measurements.GET("/subscribe/advanced", streamingHandler.SubscribeToMeasurementsAdvanced)
			}
		}

		// Sensor discovery endpoints
		sensors := v1.Group("/sensors")
		{
			// Main sensor discovery endpoint
			sensors.POST("", sensorDiscoveryHandler.DiscoverSensors)

			// Sensor verification endpoint
			sensors.POST("/verify", sensorDiscoveryHandler.VerifySensors)

			// Batch sensor timeseries endpoint (must be before /:name to avoid conflicts)
			sensors.POST("/timeseries", sensorDiscoveryHandler.GetBatchSensorTimeseries)

			// Batch sensor types endpoint
			sensors.POST("/types", sensorDiscoveryHandler.GetBatchSensorTypes)

			// Legacy compatibility endpoints
			sensors.GET("/discover", sensorDiscoveryHandler.DiscoverSensorsLegacy)

			// Single sensor timeseries endpoint
			sensors.GET("/:name", sensorDiscoveryHandler.GetSensorTimeseries)
		}

		// Dataset endpoints
		datasets := v1.Group("/datasets")
		{
			datasets.POST("", datasetHandler.CreateDataset)
			datasets.GET("", datasetHandler.ListDatasets)
			datasets.GET("/:name", datasetHandler.GetDataset)
			datasets.POST("/:id/types", datasetHandler.AddTypesToDataset)
			datasets.DELETE("/:id/types", datasetHandler.RemoveTypesFromDataset)
			datasets.GET("/:name/sensors", datasetHandler.GetSensorsByDataset)
		}

		// Type endpoints
		types := v1.Group("/types")
		{
			types.GET("", typeHandler.ListTypes)
			types.GET("/:name", typeHandler.GetType)
		}
	}

	// Swagger documentation
	router.GET("/api/swagger/*any", ginSwagger.WrapHandler(swaggerFiles.Handler))

	// Root endpoint
	router.GET("/", func(c *gin.Context) {
		c.JSON(http.StatusOK, gin.H{
			"service":       "timeseries-api",
			"version":       "1.0.0",
			"documentation": "/api/swagger/index.html",
		})
	})

	return router
}
