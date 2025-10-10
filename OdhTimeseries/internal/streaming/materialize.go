package streaming

import (
	"context"
	"database/sql"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"strconv"
	"strings"
	"sync"
	"time"

	"timeseries-api/internal/filter"
	"timeseries-api/internal/models"

	"github.com/google/uuid"
	_ "github.com/lib/pq"
	"github.com/paulmach/orb/encoding/wkb"
	"github.com/paulmach/orb/encoding/wkt"
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
	Value          interface{}     `json:"value"` // Native type from database (float64 for numeric, string for string, json.RawMessage for json, []byte for geoposition/geoshape, bool for boolean)
	ProvenanceID   *int64          `json:"provenance_id"`
	CreatedOn      time.Time       `json:"created_on"`
	SensorID       int64           `json:"sensor_id"`
	TypeID         int64           `json:"type_id"`
	SensorName     string          `json:"sensor_name"`
	TypeName       string          `json:"type_name"`
	DataType       models.DataType `json:"data_type"`
	Unit           string          `json:"unit"`
	SensorMetadata json.RawMessage `json:"sensor_metadata"`
	Diff           int             `json:"-"` // Internal field: 1 for insert, -1 for delete (not sent to client)
}

// SpatialFilterCondition represents a spatial filter extracted from expression
type SpatialFilterCondition struct {
	TypeName    string
	Operator    filter.FilterOperator // OpBoundingBoxIntersect, OpBoundingBoxContain, OpDistanceLessThan
	Coordinates []float64
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

	// Check if source is running and healthy by checking one of the typed views
	maxAttempts := 60 // Wait up to 60 seconds
	for attempt := 0; attempt < maxAttempts; attempt++ {
		var count int
		err := mc.db.QueryRowContext(ctx,
			"SELECT COUNT(*) FROM latest_measurements_numeric",
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

// getDataTypeForTypeName queries Materialize to get the data type for a type name
func (mc *MaterializeClient) getDataTypeForTypeName(ctx context.Context, typeName string) (models.DataType, error) {
	var dataType models.DataType
	err := mc.db.QueryRowContext(ctx,
		`SELECT data_type FROM types WHERE name = $1`,
		typeName,
	).Scan(&dataType)

	if err != nil {
		return "", fmt.Errorf("failed to get data type for %s: %w", typeName, err)
	}

	return dataType, nil
}

// getDataTypesForTypeNames gets data types for multiple type names
// Returns a map of type_name -> data_type
func (mc *MaterializeClient) getDataTypesForTypeNames(ctx context.Context, typeNames []string) (map[string]models.DataType, error) {
	if len(typeNames) == 0 {
		return map[string]models.DataType{}, nil
	}

	placeholders := make([]string, len(typeNames))
	args := make([]interface{}, len(typeNames))
	for i, name := range typeNames {
		placeholders[i] = fmt.Sprintf("$%d", i+1)
		args[i] = name
	}

	query := fmt.Sprintf(`SELECT name, data_type FROM types WHERE name IN (%s)`, strings.Join(placeholders, ","))
	rows, err := mc.db.QueryContext(ctx, query, args...)
	if err != nil {
		return nil, fmt.Errorf("failed to query data types: %w", err)
	}
	defer rows.Close()

	result := make(map[string]models.DataType)
	for rows.Next() {
		var name string
		var dataType models.DataType
		if err := rows.Scan(&name, &dataType); err != nil {
			return nil, fmt.Errorf("failed to scan data type: %w", err)
		}
		result[name] = dataType
	}

	return result, nil
}

// SubscribeWithFilters subscribes to measurements using discovery filters
// Filters are applied directly in the Materialize TAIL query for real-time evaluation
// Returns extracted spatial filters that must be applied at application layer
// 🎯 SNAPSHOT CONTROL: If sub.skipInitialSnapshot is true, skips existing records
func (mc *MaterializeClient) SubscribeWithFilters(
	ctx context.Context,
	sub *Subscription,
	sensorNames []string,
	typeNames []string,
	timeseriesFilter *filter.TimeseriesFilter,
	measurementFilter *filter.MeasurementFilter,
	updatesChan chan<- MeasurementUpdate,
) ([]SpatialFilterCondition, error) {
	// Parse measurement filter expression to extract conditions
	var valueConditions []filter.ValueCondition
	var spatialFilters []SpatialFilterCondition

	if measurementFilter != nil && measurementFilter.Expression != "" {
		parser := filter.NewFilterExpressionParser()
		expr, err := parser.ParseExpression(measurementFilter.Expression)
		if err != nil {
			return nil, fmt.Errorf("failed to parse measurement filter expression: %w", err)
		}

		valueConditions, err = parser.ConvertToValueConditions(expr)
		if err != nil {
			return nil, fmt.Errorf("failed to convert filter expression: %w", err)
		}

		// Separate spatial from non-spatial conditions
		var nonSpatialConditions []filter.ValueCondition
		for _, cond := range valueConditions {
			if cond.Operator == filter.OpBoundingBoxIntersect ||
				cond.Operator == filter.OpBoundingBoxContain ||
				cond.Operator == filter.OpDistanceLessThan {
				// Extract spatial filter
				if coords, ok := cond.Value.([]float64); ok {
					spatialFilters = append(spatialFilters, SpatialFilterCondition{
						TypeName:    cond.TypeName,
						Operator:    cond.Operator,
						Coordinates: coords,
					})
				}
			} else {
				nonSpatialConditions = append(nonSpatialConditions, cond)
			}
		}
		valueConditions = nonSpatialConditions
	}

	// Determine which type names we need to query
	var allTypeNames []string

	// Add explicitly specified type names
	allTypeNames = append(allTypeNames, typeNames...)

	// Add type names from timeseries filter
	if timeseriesFilter != nil {
		allTypeNames = append(allTypeNames, timeseriesFilter.RequiredTypes...)
		allTypeNames = append(allTypeNames, timeseriesFilter.OptionalTypes...)
	}

	// Add type names from value conditions
	for _, cond := range valueConditions {
		allTypeNames = append(allTypeNames, cond.TypeName)
	}

	// Remove duplicates
	typeNameSet := make(map[string]bool)
	var uniqueTypeNames []string
	for _, name := range allTypeNames {
		if !typeNameSet[name] {
			typeNameSet[name] = true
			uniqueTypeNames = append(uniqueTypeNames, name)
		}
	}

	// Get data types for all type names
	var typeDataTypes map[string]models.DataType
	var err error
	if len(uniqueTypeNames) > 0 {
		typeDataTypes, err = mc.getDataTypesForTypeNames(ctx, uniqueTypeNames)
		if err != nil {
			return spatialFilters, fmt.Errorf("failed to get data types: %w", err)
		}
	} else {
		// If no type names specified, we need to handle all data types
		typeDataTypes = map[string]models.DataType{}
	}

	// Group type names by data type to build separate TAIL queries
	dataTypeGroups := make(map[models.DataType][]string)
	for typeName, dataType := range typeDataTypes {
		dataTypeGroups[dataType] = append(dataTypeGroups[dataType], typeName)
	}

	// If no types specified, subscribe to all data types
	if len(dataTypeGroups) == 0 && len(sensorNames) > 0 {
		// Simple mode: subscribe based on sensor names only
		dataTypeGroups = map[models.DataType][]string{
			models.DataTypeNumeric:     {},
			models.DataTypeString:      {},
			models.DataTypeJSON:        {},
			models.DataTypeBoolean:     {},
			models.DataTypeGeoposition: {},
			models.DataTypeGeoshape:    {},
		}
	}

	if len(dataTypeGroups) == 0 {
		return spatialFilters, fmt.Errorf("no data types to subscribe to")
	}

	logrus.WithFields(logrus.Fields{
		"sensorNames":       sensorNames,
		"typeNames":         typeNames,
		"dataTypes":         len(dataTypeGroups),
		"spatialFilters":    len(spatialFilters),
		"nonSpatialFilters": len(valueConditions),
	}).Info("Starting parallel Materialize TAIL subscriptions (one per data type)")

	// Store spatial filters in subscription for application-layer filtering
	sub.mu.Lock()
	sub.spatialFilters = spatialFilters
	sub.mu.Unlock()

	// Start a separate TAIL subscription for each data type in parallel
	// This avoids UNION type casting issues and maintains typed values
	var wg sync.WaitGroup
	errChan := make(chan error, len(dataTypeGroups))

	for dataType, typeNamesForDataType := range dataTypeGroups {
		wg.Add(1)
		go func(dt models.DataType, dtTypeNames []string) {
			defer wg.Done()
			err := mc.subscribeSingleDataType(
				ctx,
				dt,
				sensorNames,
				dtTypeNames,
				timeseriesFilter,
				valueConditions,
				sub.skipInitialSnapshot, // 🎯 SNAPSHOT CONTROL
				updatesChan,
			)
			if err != nil && err != context.Canceled {
				errChan <- fmt.Errorf("subscription for %s failed: %w", dt, err)
			}
		}(dataType, typeNamesForDataType)
	}

	// Wait for all subscriptions to finish (will block until context is cancelled)
	go func() {
		wg.Wait()
		close(errChan)
	}()

	// Check for errors from any subscription
	for err := range errChan {
		if err != nil {
			return spatialFilters, err
		}
	}

	return spatialFilters, ctx.Err()
}

// subscribeSingleDataType handles TAIL subscription for a single data type
// Runs in its own goroutine and transaction, sending updates to the shared channel
// 🎯 SNAPSHOT CONTROL: If skipInitialSnapshot is true, only sends new updates (not existing records)
func (mc *MaterializeClient) subscribeSingleDataType(
	ctx context.Context,
	dataType models.DataType,
	sensorNames []string,
	typeNames []string,
	timeseriesFilter *filter.TimeseriesFilter,
	valueConditions []filter.ValueCondition,
	skipInitialSnapshot bool,
	updatesChan chan<- MeasurementUpdate,
) error {
	tableName := fmt.Sprintf("latest_measurements_%s", dataType)

	// Build query for this specific data type (no UNION, no casting)
	query, args := mc.buildSingleDataTypeQuery(
		tableName,
		dataType,
		sensorNames,
		typeNames,
		timeseriesFilter,
		valueConditions,
		skipInitialSnapshot, // 🎯 SNAPSHOT CONTROL
	)

	logrus.WithFields(logrus.Fields{
		"dataType":  dataType,
		"tableName": tableName,
	}).Debug("Starting TAIL subscription for data type")

	// Start transaction for this subscription
	tx, err := mc.db.BeginTx(ctx, nil)
	if err != nil {
		return fmt.Errorf("failed to begin transaction: %w", err)
	}
	defer tx.Rollback()

	// Declare cursor
	_, err = tx.ExecContext(ctx, query, args...)
	if err != nil {
		return fmt.Errorf("failed to declare cursor: %w", err)
	}

	// Fetch updates in loop (blocking)
	for {
		select {
		case <-ctx.Done():
			logrus.WithField("dataType", dataType).Debug("Stopping TAIL subscription due to context cancellation")
			return ctx.Err()
		default:
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
				var valueStr string

				err := rows.Scan(
					&mzTimestamp,
					&mzDiff,
					&update.TimeseriesID,
					&update.Timestamp,
					&valueStr,
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

				// 🎯 FILTER DELETIONS: Only send insertions (diff=1), skip deletions (diff=-1)
				// Materialize sends both when a view updates, but we only want new values
				if mzDiff != 1 {
					continue // Skip this update - it's a deletion
				}

				// Convert value based on data type (same pattern as repository.go getMeasurements)
				switch dataType {
				case models.DataTypeNumeric:
					// Convert string to float64
					if floatVal, err := strconv.ParseFloat(valueStr, 64); err == nil {
						update.Value = floatVal
					} else {
						update.Value = valueStr // fallback to string if conversion fails
					}
				case models.DataTypeString:
					update.Value = valueStr
				case models.DataTypeBoolean:
					// Convert string to boolean
					if boolVal, err := strconv.ParseBool(valueStr); err == nil {
						update.Value = boolVal
					} else {
						update.Value = valueStr // fallback to string if conversion fails
					}
				case models.DataTypeJSON:
					// Parse JSON string into a map/slice so it's marshaled correctly
					var jsonValue interface{}
					if err := json.Unmarshal([]byte(valueStr), &jsonValue); err == nil {
						update.Value = jsonValue
					} else {
						update.Value = valueStr // fallback to string if parsing fails
					}
				case models.DataTypeGeoposition, models.DataTypeGeoshape:
					// --- 🎯 GEOSPATIAL DESERIALIZATION (WKB to WKT) ---
					// 1. Decode the hex string into raw binary WKB bytes.
					wkbBytes, err := hex.DecodeString(valueStr)
					if err != nil {
						logrus.WithError(err).WithField("wkbHex", valueStr).Error("failed to decode WKB hex string")
						update.Value = valueStr // Fallback: keep the raw hex string
						break                   // Skip WKB parsing but proceed with update
					}

					// 2. Decode the WKB bytes into a geom.T (geometry object).
					geom, err := wkb.Unmarshal(wkbBytes)
					if err != nil {
						logrus.WithError(err).WithField("wkbHex", valueStr).Error("failed to unmarshal WKB bytes")
						update.Value = valueStr // Fallback: keep the raw hex string
						break                   // Skip WKT conversion but proceed with update
					}

					// 3. Encode the geometry object to WKT string using the WKT encoder.
					wktString := wkt.Marshal(geom)   // <-- CORRECT WKT MARSHALING
					update.Value = string(wktString) // Assign the human-readable WKT string
					// --- END GEOSPATIAL DESERIALIZATION ---
				default:
					update.Value = valueStr
				}

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

// buildSingleDataTypeQuery builds a DECLARE CURSOR FOR SUBSCRIBE query for a single data type
// 🎯 SNAPSHOT CONTROL: If skipInitialSnapshot is true, uses "AS OF mz_now()" to skip existing records
func (mc *MaterializeClient) buildSingleDataTypeQuery(
	tableName string,
	dataType models.DataType,
	sensorNames []string,
	typeNames []string,
	timeseriesFilter *filter.TimeseriesFilter,
	valueConditions []filter.ValueCondition,
	skipInitialSnapshot bool,
) (string, []interface{}) {
	args := []interface{}{}
	argIndex := 1
	whereClauses := []string{}

	// Base SELECT (no casting needed - single data type!)
	// For geo types, convert to WKT text format for consistency with REST API
	valueColumn := "value"
	// if dataType == models.DataTypeGeoposition || dataType == models.DataTypeGeoshape {
	// 	valueColumn = "ST_AsText(value)"
	// }

	query := fmt.Sprintf(`DECLARE c CURSOR FOR SUBSCRIBE TO (
SELECT
	timeseries_id,
	timestamp,
	%s as value,
	provenance_id,
	created_on,
	sensor_id,
	type_id,
	sensor_name,
	type_name,
	data_type,
	unit,
	sensor_metadata
FROM %s`, valueColumn, tableName)

	// Filter by sensor names
	if len(sensorNames) > 0 {
		placeholders := make([]string, len(sensorNames))
		for i, name := range sensorNames {
			placeholders[i] = fmt.Sprintf("$%d", argIndex)
			args = append(args, name)
			argIndex++
		}
		whereClauses = append(whereClauses, fmt.Sprintf("sensor_name IN (%s)", strings.Join(placeholders, ",")))
	}

	// Filter by type names
	if len(typeNames) > 0 {
		placeholders := make([]string, len(typeNames))
		for i, name := range typeNames {
			placeholders[i] = fmt.Sprintf("$%d", argIndex)
			args = append(args, name)
			argIndex++
		}
		whereClauses = append(whereClauses, fmt.Sprintf("type_name IN (%s)", strings.Join(placeholders, ",")))
	}

	// Handle timeseries filter
	if timeseriesFilter != nil {
		if len(timeseriesFilter.RequiredTypes) > 0 {
			placeholders := make([]string, len(timeseriesFilter.RequiredTypes))
			for i, typeName := range timeseriesFilter.RequiredTypes {
				placeholders[i] = fmt.Sprintf("$%d", argIndex)
				args = append(args, typeName)
				argIndex++
			}
			whereClauses = append(whereClauses, fmt.Sprintf("type_name IN (%s)", strings.Join(placeholders, ",")))
		}

		// ! At the moment this do not work because we need at least one type, and honetly the usefulness of this feature is doubtful
		// Handle dataset membership filter (using DatasetIDs field for names)
		// if len(timeseriesFilter.DatasetIDs) > 0 {
		// 	// Treat timeseriesFilter.DatasetIDs as a list of Dataset Names for the filter
		// 	datasetNames := timeseriesFilter.DatasetIDs

		// 	// The MV already contains the 'dataset_names' array.
		// 	// We'll use the 'ANY' operator to check if any of the filter names
		// 	// are present in the MV's dataset_names array.

		// 	placeholders := make([]string, len(datasetNames))
		// 	for i, datasetName := range datasetNames {
		// 		placeholders[i] = fmt.Sprintf("$%d", argIndex)
		// 		args = append(args, datasetName)
		// 		argIndex++
		// 	}

		// 	// Check if the dataset_names array in the MV INTERSECTS with the input array
		// 	datasetClause := fmt.Sprintf("ARRAY[%s] <@ m.dataset_names", strings.Join(placeholders, ","))
		// 	whereClauses = append(whereClauses, datasetClause)
		// }
	}

	// Handle value conditions (DB-level filtering on typed columns!)
	for _, cond := range valueConditions {
		valueClause, valueArgs := mc.buildValueConditionForDataType(cond, dataType, argIndex)
		if valueClause != "" {
			whereClauses = append(whereClauses, valueClause)
			args = append(args, valueArgs...)
			argIndex += len(valueArgs)
		}
	}

	// Add WHERE clause
	if len(whereClauses) > 0 {
		query += "\nWHERE " + strings.Join(whereClauses, " AND ")
	}

	query += "\n)"

	// 🎯 SNAPSHOT CONTROL: Skip initial snapshot using WITH (SNAPSHOT = false)
	// When skipInitialSnapshot is true, don't send existing records, only new updates
	// When false (default), receive all existing records in the view plus new updates
	if skipInitialSnapshot {
		query += " WITH (SNAPSHOT = false)"
	}

	return query, args
}

// buildValueConditionForDataType builds a WHERE clause for a value condition on a typed column
func (mc *MaterializeClient) buildValueConditionForDataType(
	cond filter.ValueCondition,
	dataType models.DataType,
	startArgIndex int,
) (string, []interface{}) {
	// Only apply numeric comparisons to numeric data type
	if dataType == models.DataTypeNumeric {
		switch cond.Operator {
		case filter.OpEqual:
			return fmt.Sprintf("value = $%d", startArgIndex), []interface{}{cond.Value}
		case filter.OpNotEqual:
			return fmt.Sprintf("value != $%d", startArgIndex), []interface{}{cond.Value}
		case filter.OpGreaterThan:
			return fmt.Sprintf("value > $%d", startArgIndex), []interface{}{cond.Value}
		case filter.OpGreaterThanOrEqual:
			return fmt.Sprintf("value >= $%d", startArgIndex), []interface{}{cond.Value}
		case filter.OpLessThan:
			return fmt.Sprintf("value < $%d", startArgIndex), []interface{}{cond.Value}
		case filter.OpLessThanOrEqual:
			return fmt.Sprintf("value <= $%d", startArgIndex), []interface{}{cond.Value}
		}
	}

	// Handle IN/NOT IN for all types
	switch cond.Operator {
	case filter.OpIn:
		if list, ok := cond.Value.([]interface{}); ok {
			placeholders := make([]string, len(list))
			args := make([]interface{}, len(list))
			for i, val := range list {
				placeholders[i] = fmt.Sprintf("$%d", startArgIndex+i)
				args[i] = val
			}
			return fmt.Sprintf("value IN (%s)", strings.Join(placeholders, ",")), args
		}
	case filter.OpNotIn:
		if list, ok := cond.Value.([]interface{}); ok {
			placeholders := make([]string, len(list))
			args := make([]interface{}, len(list))
			for i, val := range list {
				placeholders[i] = fmt.Sprintf("$%d", startArgIndex+i)
				args[i] = val
			}
			return fmt.Sprintf("value NOT IN (%s)", strings.Join(placeholders, ",")), args
		}
	}

	return "", nil
}
