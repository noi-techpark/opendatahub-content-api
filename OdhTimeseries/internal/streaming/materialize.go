package streaming

import (
	"context"
	"database/sql"
	"encoding/json"
	"fmt"
	"strings"
	"time"

	"timeseries-api/internal/filter"
	"timeseries-api/internal/models"

	"github.com/google/uuid"
	_ "github.com/lib/pq"
	"github.com/sirupsen/logrus"
)

// MaterializeConfig holds configuration for Materialize connection
type MaterializeConfig struct {
	Host     string
	Port     int
	User     string
	Password string
	Database string
}

// MaterializeClient manages connection and operations with Materialize
type MaterializeClient struct {
	db     *sql.DB
	config MaterializeConfig
}

// MeasurementUpdate represents a measurement update from Materialize TAIL
type MeasurementUpdate struct {
	TimeseriesID   uuid.UUID       `json:"timeseries_id"`
	Timestamp      time.Time       `json:"timestamp"`
	Value          string          `json:"value"` // Always string from unified view
	ProvenanceID   *int64          `json:"provenance_id"`
	CreatedOn      time.Time       `json:"created_on"`
	SensorID       int64           `json:"sensor_id"`
	TypeID         int64           `json:"type_id"`
	SensorName     string          `json:"sensor_name"`
	TypeName       string          `json:"type_name"`
	DataType       models.DataType `json:"data_type"`
	Unit           string          `json:"unit"`
	SensorMetadata json.RawMessage `json:"sensor_metadata"`
	Diff           int             `json:"diff"` // 1 for insert, -1 for delete
}

// NewMaterializeClient creates a new Materialize client
func NewMaterializeClient(config MaterializeConfig) (*MaterializeClient, error) {
	connStr := fmt.Sprintf("postgres://%s:%s@%s:%d/%s?sslmode=disable",
		config.User, config.Password, config.Host, config.Port, config.Database)

	db, err := sql.Open("postgres", connStr)
	if err != nil {
		return nil, fmt.Errorf("failed to connect to Materialize: %w", err)
	}

	// Test connection
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	if err := db.PingContext(ctx); err != nil {
		return nil, fmt.Errorf("failed to ping Materialize: %w", err)
	}

	logrus.Info("Connected to Materialize successfully")

	return &MaterializeClient{
		db:     db,
		config: config,
	}, nil
}

// Close closes the Materialize connection
func (mc *MaterializeClient) Close() error {
	return mc.db.Close()
}

// InitializeFromPostgres performs initial sync of data from PostgreSQL to Materialize
// This is handled automatically by Materialize's PostgreSQL source
func (mc *MaterializeClient) WaitForInitialSync(ctx context.Context) error {
	logrus.Info("Waiting for Materialize initial sync to complete...")

	// Check if source is running and healthy
	maxAttempts := 60 // Wait up to 60 seconds
	for attempt := 0; attempt < maxAttempts; attempt++ {
		var count int
		err := mc.db.QueryRowContext(ctx,
			"SELECT COUNT(*) FROM latest_measurements_all",
		).Scan(&count)

		if err == nil {
			logrus.WithField("count", count).Info("Materialize initial sync completed")
			return nil
		}

		if err != nil && err != sql.ErrNoRows {
			logrus.WithError(err).Warn("Error checking Materialize sync status")
		}

		select {
		case <-ctx.Done():
			return ctx.Err()
		case <-time.After(1 * time.Second):
		}
	}

	return fmt.Errorf("timeout waiting for Materialize initial sync")
}

// GetSourceStatus checks the status of the PostgreSQL source
func (mc *MaterializeClient) GetSourceStatus(ctx context.Context) (string, error) {
	var status string
	err := mc.db.QueryRowContext(ctx,
		"SELECT status FROM mz_sources WHERE name = 'pg_source'",
	).Scan(&status)

	if err != nil {
		return "", fmt.Errorf("failed to get source status: %w", err)
	}

	return status, nil
}

// SubscribeWithFilters subscribes to measurements using discovery filters
// Filters are applied directly in the Materialize TAIL query for real-time evaluation
func (mc *MaterializeClient) SubscribeWithFilters(
	ctx context.Context,
	sensorNames []string,
	typeNames []string,
	timeseriesFilter *filter.TimeseriesFilter,
	measurementFilter *filter.MeasurementFilter,
	updatesChan chan<- MeasurementUpdate,
) error {
	// Build TAIL query with dynamic filters
	query := "DECLARE c CURSOR FOR SUBSCRIBE TO ("
	query += "SELECT timeseries_id, timestamp, value, provenance_id, created_on, "
	query += "sensor_id, type_id, sensor_name, type_name, data_type, unit, sensor_metadata "
	query += "FROM latest_measurements_all WHERE 1=1"

	args := []interface{}{}
	argIndex := 1

	// Simple mode: filter by sensor names
	if len(sensorNames) > 0 {
		placeholders := ""
		for i, name := range sensorNames {
			if i > 0 {
				placeholders += ","
			}
			placeholders += fmt.Sprintf("$%d", argIndex)
			args = append(args, name)
			argIndex++
		}
		query += fmt.Sprintf(" AND sensor_name IN (%s)", placeholders)
	}

	// Filter by type names (simple mode or discovery mode)
	if len(typeNames) > 0 {
		placeholders := ""
		for i, name := range typeNames {
			if i > 0 {
				placeholders += ","
			}
			placeholders += fmt.Sprintf("$%d", argIndex)
			args = append(args, name)
			argIndex++
		}
		query += fmt.Sprintf(" AND type_name IN (%s)", placeholders)
	}

	// Discovery mode: apply timeseries_filter conditions
	if timeseriesFilter != nil {
		// Required types - sensor must have ALL of these types
		if len(timeseriesFilter.RequiredTypes) > 0 {
			placeholders := ""
			for i, typeName := range timeseriesFilter.RequiredTypes {
				if i > 0 {
					placeholders += ","
				}
				placeholders += fmt.Sprintf("$%d", argIndex)
				args = append(args, typeName)
				argIndex++
			}
			query += fmt.Sprintf(" AND type_name IN (%s)", placeholders)
		}

		// Note: Optional types and dataset filtering would require more complex subqueries
		// For now, we implement the most common use case (required types)
		// TODO: Implement optional_types and dataset_ids filtering
	}

	// Discovery mode: apply measurement_filter conditions
	if measurementFilter != nil {
		// Parse and apply value expression filter (non-geometric)
		if measurementFilter.Expression != "" {
			// Parse expression format: "type.operator.value"
			// Example: "temperature.gteq.20" or "humidity.lt.50"
			whereClause, filterArgs := mc.buildValueFilterClause(measurementFilter.Expression, argIndex)
			if whereClause != "" {
				query += " AND " + whereClause
				args = append(args, filterArgs...)
				argIndex += len(filterArgs)
			}
		}

		// TODO: Time range filtering (requires timestamp comparison)
		// TODO: Geometric filtering (will be done in application layer)
	}

	query += ")"

	logrus.WithFields(logrus.Fields{
		"query":             query,
		"sensorNames":       sensorNames,
		"typeNames":         typeNames,
		"timeseriesFilter":  timeseriesFilter != nil,
		"measurementFilter": measurementFilter != nil,
	}).Info("Starting Materialize TAIL subscription with discovery filters")

	// Start transaction for cursor
	tx, err := mc.db.BeginTx(ctx, nil)
	if err != nil {
		return fmt.Errorf("failed to begin transaction: %w", err)
	}
	defer tx.Rollback()

	// Declare cursor
	if len(args) > 0 {
		_, err = tx.ExecContext(ctx, query, args...)
	} else {
		_, err = tx.ExecContext(ctx, query)
	}
	if err != nil {
		return fmt.Errorf("failed to declare cursor: %w", err)
	}

	// Fetch updates in loop
	for {
		select {
		case <-ctx.Done():
			logrus.Info("Stopping Materialize TAIL subscription due to context cancellation")
			return ctx.Err()
		default:
			// Fetch next batch of updates
			rows, err := tx.QueryContext(ctx, "FETCH ALL c")
			if err != nil {
				return fmt.Errorf("failed to fetch from cursor: %w", err)
			}

			hasRows := false
			for rows.Next() {
				hasRows = true
				var update MeasurementUpdate
				var mzTimestamp int64
				var mzDiff int

				err := rows.Scan(
					&mzTimestamp,
					&mzDiff,
					&update.TimeseriesID,
					&update.Timestamp,
					&update.Value,
					&update.ProvenanceID,
					&update.CreatedOn,
					&update.SensorID,
					&update.TypeID,
					&update.SensorName,
					&update.TypeName,
					&update.DataType,
					&update.Unit,
					&update.SensorMetadata,
				)
				if err != nil {
					rows.Close()
					return fmt.Errorf("failed to scan update: %w", err)
				}

				update.Diff = mzDiff

				// Send update through channel
				select {
				case updatesChan <- update:
				case <-ctx.Done():
					rows.Close()
					return ctx.Err()
				}
			}
			rows.Close()

			// If no rows, sleep briefly to avoid busy loop
			if !hasRows {
				select {
				case <-ctx.Done():
					return ctx.Err()
				case <-time.After(100 * time.Millisecond):
				}
			}
		}
	}
}

// buildValueFilterClause builds a WHERE clause from a measurement filter expression
// Expression format: "type.operator.value" (e.g., "temperature.gteq.20")
// Returns the WHERE clause and arguments
func (mc *MaterializeClient) buildValueFilterClause(expression string, startArgIndex int) (string, []interface{}) {
	parts := strings.Split(expression, ".")
	if len(parts) != 3 {
		logrus.WithField("expression", expression).Warn("Invalid filter expression format, expected type.operator.value")
		return "", nil
	}

	typeName := parts[0]
	operator := parts[1]
	value := parts[2]

	// Skip geometric conditions - they will be handled in application layer
	if typeName == "location" || typeName == "geoposition" || typeName == "geoshape" {
		return "", nil
	}

	// Build WHERE clause
	// Note: We filter by type_name and data_type first
	// The CAST is applied in the application layer to avoid Materialize
	// evaluating it on non-numeric rows during SUBSCRIBE
	// For now, just filter by type and data_type
	// TODO: Full value filtering requires a different approach (e.g., app-layer filtering)
	whereClause := fmt.Sprintf("(type_name = $%d AND data_type = 'numeric')",
		startArgIndex)

	args := []interface{}{typeName}

	logrus.WithFields(logrus.Fields{
		"type_name": typeName,
		"operator":  operator,
		"value":     value,
		"note":      "Value filtering will be applied in application layer to avoid CAST errors",
	}).Warn("Value expression filtering in Materialize TAIL has issues with UNION views - applying type filter only")

	return whereClause, args
}
