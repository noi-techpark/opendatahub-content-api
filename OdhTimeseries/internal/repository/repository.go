package repository

import (
	"database/sql"
	"encoding/json"
	"fmt"
	"strconv"
	"strings"
	"time"

	"timeseries-api/internal/filter"
	"timeseries-api/internal/models"
	"timeseries-api/pkg/database"

	"github.com/google/uuid"
	"github.com/paulmach/orb"
	"github.com/paulmach/orb/encoding/wkt"
)

type Repository struct {
	db *database.DB
}

func New(db *database.DB) *Repository {
	return &Repository{db: db}
}

func (r *Repository) GetOrCreateProvenance(lineage, dataCollector, dataCollectorVersion string) (*models.Provenance, error) {
	var p models.Provenance
	query := fmt.Sprintf(`
		SELECT id, uuid, lineage, data_collector, data_collector_version 
		FROM %s.provenance 
		WHERE lineage = $1 AND data_collector = $2 AND COALESCE(data_collector_version, '') = COALESCE($3, '')`,
		r.db.Schema)

	err := r.db.QueryRow(query, lineage, dataCollector, dataCollectorVersion).Scan(
		&p.ID, &p.UUID, &p.Lineage, &p.DataCollector, &p.DataCollectorVersion)

	if err == sql.ErrNoRows {
		uuid := uuid.New().String()
		insertQuery := fmt.Sprintf(`
			INSERT INTO %s.provenance (uuid, lineage, data_collector, data_collector_version) 
			VALUES ($1, $2, $3, $4) RETURNING id`,
			r.db.Schema)

		err = r.db.QueryRow(insertQuery, uuid, lineage, dataCollector, dataCollectorVersion).Scan(&p.ID)
		if err != nil {
			return nil, fmt.Errorf("failed to create provenance: %w", err)
		}

		p.UUID = uuid
		p.Lineage = lineage
		p.DataCollector = dataCollector
		p.DataCollectorVersion = dataCollectorVersion
	} else if err != nil {
		return nil, fmt.Errorf("failed to get provenance: %w", err)
	}

	return &p, nil
}

func (r *Repository) GetOrCreateSensor(name string, parentID *int64, metadata json.RawMessage) (*models.Sensor, error) {
	var s models.Sensor
	query := fmt.Sprintf(`
		SELECT id, name, parent_id, metadata, created_on, is_active, is_available
		FROM %s.sensors
		WHERE name = $1`,
		r.db.Schema)

	err := r.db.QueryRow(query, name).Scan(
		&s.ID, &s.Name, &s.ParentID, &s.Metadata, &s.CreatedOn, &s.IsActive, &s.IsAvailable)

	if err == sql.ErrNoRows {
		insertQuery := fmt.Sprintf(`
			INSERT INTO %s.sensors (name, parent_id, metadata)
			VALUES ($1, $2, $3) RETURNING id, created_on`,
			r.db.Schema)

		err = r.db.QueryRow(insertQuery, name, parentID, metadata).Scan(&s.ID, &s.CreatedOn)
		if err != nil {
			return nil, fmt.Errorf("failed to create sensor: %w", err)
		}

		s.Name = name
		s.ParentID = parentID
		s.Metadata = metadata
		s.IsActive = true
		s.IsAvailable = true
	} else if err != nil {
		return nil, fmt.Errorf("failed to get sensor: %w", err)
	}

	return &s, nil
}

func (r *Repository) GetOrCreateType(name, description, unit string, dataType models.DataType, metadata json.RawMessage) (*models.Type, error) {
	var t models.Type
	query := fmt.Sprintf(`
		SELECT id, name, description, unit, data_type, metadata 
		FROM %s."types" 
		WHERE name = $1`,
		r.db.Schema)

	err := r.db.QueryRow(query, name).Scan(
		&t.ID, &t.Name, &t.Description, &t.Unit, &t.DataType, &t.Metadata)

	if err == sql.ErrNoRows {
		insertQuery := fmt.Sprintf(`
			INSERT INTO %s."types" (name, description, unit, data_type, metadata) 
			VALUES ($1, $2, $3, $4, $5) RETURNING id`,
			r.db.Schema)

		err = r.db.QueryRow(insertQuery, name, description, unit, dataType, metadata).Scan(&t.ID)
		if err != nil {
			return nil, fmt.Errorf("failed to create type: %w", err)
		}

		t.Name = name
		t.Description = description
		t.Unit = unit
		t.DataType = dataType
		t.Metadata = metadata
	} else if err != nil {
		return nil, fmt.Errorf("failed to get type: %w", err)
	}

	return &t, nil
}

func (r *Repository) GetOrCreateTimeseries(sensorID, typeID int64) (*models.Timeseries, error) {
	var ts models.Timeseries
	query := fmt.Sprintf(`
		SELECT id, sensor_id, type_id, created_on
		FROM %s.timeseries
		WHERE sensor_id = $1 AND type_id = $2`,
		r.db.Schema)

	err := r.db.QueryRow(query, sensorID, typeID).Scan(
		&ts.ID, &ts.SensorID, &ts.TypeID, &ts.CreatedOn)

	if err == sql.ErrNoRows {
		ts.ID = uuid.New()
		insertQuery := fmt.Sprintf(`
			INSERT INTO %s.timeseries (id, sensor_id, type_id)
			VALUES ($1, $2, $3) RETURNING created_on`,
			r.db.Schema)

		err = r.db.QueryRow(insertQuery, ts.ID, sensorID, typeID).Scan(&ts.CreatedOn)
		if err != nil {
			return nil, fmt.Errorf("failed to create timeseries: %w", err)
		}

		ts.SensorID = sensorID
		ts.TypeID = typeID
	} else if err != nil {
		return nil, fmt.Errorf("failed to get timeseries: %w", err)
	}

	return &ts, nil
}

func (r *Repository) InsertMeasurement(timeseriesID uuid.UUID, timestamp time.Time, value interface{}, dataType models.DataType, provenanceID *int64) error {
	tableName := fmt.Sprintf("%s.measurements_%s", r.db.Schema, dataType)

	query := fmt.Sprintf(`
		INSERT INTO %s (timeseries_id, timestamp, value, provenance_id)
		VALUES ($1, $2, $3, $4)
		ON CONFLICT (timeseries_id, timestamp) DO NOTHING`,
		tableName)

	var formattedValue interface{}
	switch dataType {
	case models.DataTypeGeoposition:
		if point, ok := value.(*orb.Point); ok && point != nil {
			formattedValue = fmt.Sprintf("ST_GeomFromText('POINT(%f %f)', 4326)", point.X(), point.Y())
		}
	case models.DataTypeGeoshape:
		if polygon, ok := value.(*orb.Polygon); ok && polygon != nil {
			formattedValue = fmt.Sprintf("ST_GeomFromText('%s', 4326)", string(wkt.Marshal(*polygon)))
		}
	default:
		formattedValue = value
	}

	_, err := r.db.Exec(query, timeseriesID, timestamp, formattedValue, provenanceID)
	if err != nil {
		return fmt.Errorf("failed to insert %s measurement: %w", dataType, err)
	}

	return nil
}

// BatchInsertMeasurements inserts multiple measurements in batches for better performance
func (r *Repository) BatchInsertMeasurements(measurements map[models.DataType][]models.Measurement, batchSize int) error {
	if batchSize <= 0 {
		batchSize = 1000 // Default batch size
	}

	for dataType, measurementList := range measurements {
		if len(measurementList) == 0 {
			continue
		}

		tableName := fmt.Sprintf("%s.measurements_%s", r.db.Schema, dataType)

		for i := 0; i < len(measurementList); i += batchSize {
			end := i + batchSize
			if end > len(measurementList) {
				end = len(measurementList)
			}

			batch := measurementList[i:end]
			if err := r.insertMeasurementBatch(tableName, batch, dataType); err != nil {
				return fmt.Errorf("failed to insert batch for %s: %w", dataType, err)
			}
		}
	}

	return nil
}

func (r *Repository) insertMeasurementBatch(tableName string, measurements []models.Measurement, dataType models.DataType) error {
	if len(measurements) == 0 {
		return nil
	}

	// Build the VALUES part of the query
	valueStrings := make([]string, 0, len(measurements))
	valueArgs := make([]interface{}, 0, len(measurements)*4)

	for i, m := range measurements {
		valueStrings = append(valueStrings, fmt.Sprintf("($%d, $%d, $%d, $%d)",
			i*4+1, i*4+2, i*4+3, i*4+4))

		valueArgs = append(valueArgs, m.TimeseriesID, m.Timestamp, m.Value, m.ProvenanceID)
	}

	query := fmt.Sprintf(`
		INSERT INTO %s (timeseries_id, timestamp, value, provenance_id)
		VALUES %s`,
		tableName, strings.Join(valueStrings, ", "))

	_, err := r.db.Exec(query, valueArgs...)
	if err != nil {
		return fmt.Errorf("failed to execute batch insert: %w", err)
	}

	return nil
}

func (r *Repository) DeleteMeasurements(sensorNames []string, typeNames []string, startTime, endTime *time.Time) error {
	conditions := []string{}
	args := []interface{}{}
	argIndex := 1

	whereClause := "WHERE 1=1"

	if len(sensorNames) > 0 {
		placeholders := make([]string, len(sensorNames))
		for i, name := range sensorNames {
			placeholders[i] = fmt.Sprintf("$%d", argIndex)
			args = append(args, name)
			argIndex++
		}
		conditions = append(conditions, fmt.Sprintf("s.name IN (%s)", strings.Join(placeholders, ",")))
	}

	if len(typeNames) > 0 {
		placeholders := make([]string, len(typeNames))
		for i, name := range typeNames {
			placeholders[i] = fmt.Sprintf("$%d", argIndex)
			args = append(args, name)
			argIndex++
		}
		conditions = append(conditions, fmt.Sprintf("t.name IN (%s)", strings.Join(placeholders, ",")))
	}

	if startTime != nil {
		conditions = append(conditions, fmt.Sprintf("timestamp >= $%d", argIndex))
		args = append(args, *startTime)
		argIndex++
	}

	if endTime != nil {
		conditions = append(conditions, fmt.Sprintf("timestamp <= $%d", argIndex))
		args = append(args, *endTime)
		argIndex++
	}

	if len(conditions) > 0 {
		whereClause += " AND " + strings.Join(conditions, " AND ")
	}

	dataTypes := []models.DataType{
		models.DataTypeNumeric, models.DataTypeString, models.DataTypeJSON,
		models.DataTypeGeoposition, models.DataTypeGeoshape, models.DataTypeBoolean,
	}

	for _, dataType := range dataTypes {
		query := fmt.Sprintf(`
			DELETE FROM %s.measurements_%s m
			USING %s.timeseries ts, %s.sensors s, %s."types" t
			WHERE m.timeseries_id = ts.id 
			  AND ts.sensor_id = s.id 
			  AND ts.type_id = t.id
			  %s`,
			r.db.Schema, dataType, r.db.Schema, r.db.Schema, r.db.Schema, whereClause)

		_, err := r.db.Exec(query, args...)
		if err != nil {
			return fmt.Errorf("failed to delete %s measurements: %w", dataType, err)
		}
	}

	return nil
}

func (r *Repository) GetLatestMeasurements(sensorNames []string, typeNames []string) ([]models.MeasurementResponse, error) {
	return r.getMeasurements(sensorNames, typeNames, nil, nil, 0, true)
}

func (r *Repository) GetHistoricalMeasurements(sensorNames []string, typeNames []string, startTime, endTime *time.Time, limit int) ([]models.MeasurementResponse, error) {
	return r.getMeasurements(sensorNames, typeNames, startTime, endTime, limit, false)
}

func (r *Repository) getMeasurements(sensorNames []string, typeNames []string, startTime, endTime *time.Time, limit int, latest bool) ([]models.MeasurementResponse, error) {
	var results []models.MeasurementResponse

	conditions := []string{}
	args := []interface{}{}
	argIndex := 1

	if len(sensorNames) > 0 {
		placeholders := make([]string, len(sensorNames))
		for i, name := range sensorNames {
			placeholders[i] = fmt.Sprintf("$%d", argIndex)
			args = append(args, name)
			argIndex++
		}
		conditions = append(conditions, fmt.Sprintf("s.name IN (%s)", strings.Join(placeholders, ",")))
	}

	if len(typeNames) > 0 {
		placeholders := make([]string, len(typeNames))
		for i, name := range typeNames {
			placeholders[i] = fmt.Sprintf("$%d", argIndex)
			args = append(args, name)
			argIndex++
		}
		conditions = append(conditions, fmt.Sprintf("t.name IN (%s)", strings.Join(placeholders, ",")))
	}

	timeConditions := ""
	if startTime != nil {
		timeConditions += fmt.Sprintf(" AND m.timestamp >= $%d", argIndex)
		args = append(args, *startTime)
		argIndex++
	}
	if endTime != nil {
		timeConditions += fmt.Sprintf(" AND m.timestamp <= $%d", argIndex)
		args = append(args, *endTime)
		argIndex++
	}

	whereClause := ""
	if len(conditions) > 0 {
		whereClause = " AND " + strings.Join(conditions, " AND ")
	}

	dataTypes := []models.DataType{
		models.DataTypeNumeric, models.DataTypeString, models.DataTypeJSON,
		models.DataTypeGeoposition, models.DataTypeGeoshape, models.DataTypeBoolean,
	}

	for _, dataType := range dataTypes {
		var valueSelect string
		switch dataType {
		case models.DataTypeGeoposition, models.DataTypeGeoshape:
			valueSelect = "ST_AsText(m.value) as value"
		default:
			valueSelect = "m.value"
		}

		var query string
		if latest {
			// For latest measurements, use ROW_NUMBER() to get only the most recent per timeseries
			query = fmt.Sprintf(`
				SELECT ts.id, s.name, t.name, t.data_type, m.timestamp, %s
				FROM (
					SELECT m.*,
						   ROW_NUMBER() OVER (PARTITION BY m.timeseries_id ORDER BY m.timestamp DESC) as rn
					FROM %s.measurements_%s m
					WHERE 1=1 %s
				) m
				JOIN %s.timeseries ts ON m.timeseries_id = ts.id
				JOIN %s.sensors s ON ts.sensor_id = s.id
				JOIN %s."types" t ON ts.type_id = t.id
				WHERE m.rn = 1 %s
				ORDER BY m.timestamp DESC`,
				valueSelect,
				r.db.Schema, dataType,
				timeConditions,
				r.db.Schema,
				r.db.Schema,
				r.db.Schema,
				whereClause)
		} else {
			// For historical measurements, use regular query with ordering and optional limit
			orderBy := "ORDER BY m.timestamp DESC"
			limitClause := ""
			if limit > 0 {
				limitClause = fmt.Sprintf("LIMIT %d", limit)
			}

			query = fmt.Sprintf(`
				SELECT ts.id, s.name, t.name, t.data_type, m.timestamp, %s
				FROM %s.measurements_%s m
				JOIN %s.timeseries ts ON m.timeseries_id = ts.id
				JOIN %s.sensors s ON ts.sensor_id = s.id
				JOIN %s."types" t ON ts.type_id = t.id
				WHERE 1=1 %s %s
				%s %s`,
				valueSelect,
				r.db.Schema, dataType,
				r.db.Schema,
				r.db.Schema,
				r.db.Schema,
				whereClause, timeConditions,
				orderBy, limitClause)
		}

		rows, err := r.db.Query(query, args...)
		if err != nil {
			return nil, fmt.Errorf("failed to query %s measurements: %w", dataType, err)
		}
		defer rows.Close()

		for rows.Next() {
			var result models.MeasurementResponse
			var valueStr string

			err := rows.Scan(&result.TimeseriesID, &result.SensorName, &result.TypeName,
				&result.DataType, &result.Timestamp, &valueStr)
			if err != nil {
				return nil, fmt.Errorf("failed to scan %s measurement: %w", dataType, err)
			}

			switch dataType {
			case models.DataTypeNumeric:
				// Convert string to float64
				if floatVal, err := strconv.ParseFloat(valueStr, 64); err == nil {
					result.Value = floatVal
				} else {
					result.Value = valueStr // fallback to string if conversion fails
				}
			case models.DataTypeString:
				result.Value = valueStr
			case models.DataTypeBoolean:
				// Convert string to boolean
				if boolVal, err := strconv.ParseBool(valueStr); err == nil {
					result.Value = boolVal
				} else {
					result.Value = valueStr // fallback to string if conversion fails
				}
			case models.DataTypeJSON:
				result.Value = json.RawMessage(valueStr)
			case models.DataTypeGeoposition, models.DataTypeGeoshape:
				result.Value = valueStr // WKT format
			}

			results = append(results, result)
		}
	}

	return results, nil
}

func (r *Repository) FindSensorsByDataset(datasetID uuid.UUID) ([]models.Sensor, error) {
	// Note: In the simplified schema, datasets are not directly linked to timeseries
	// This function now returns all sensors instead
	query := fmt.Sprintf(`
		SELECT id, name, parent_id, metadata, created_on, is_active, is_available
		FROM %s.sensors
		WHERE is_active = true`,
		r.db.Schema)

	rows, err := r.db.Query(query)
	if err != nil {
		return nil, fmt.Errorf("failed to find sensors: %w", err)
	}
	defer rows.Close()

	var sensors []models.Sensor
	for rows.Next() {
		var s models.Sensor

		err := rows.Scan(&s.ID, &s.Name, &s.ParentID, &s.Metadata, &s.CreatedOn, &s.IsActive, &s.IsAvailable)
		if err != nil {
			return nil, fmt.Errorf("failed to scan sensor: %w", err)
		}

		sensors = append(sensors, s)
	}

	return sensors, nil
}

// Schema returns the database schema name
func (r *Repository) Schema() string {
	return r.db.Schema
}

// ExecuteCustomSearch executes a custom search query with parameters
func (r *Repository) ExecuteCustomSearch(query string, parameters map[string]interface{}) ([]map[string]interface{}, error) {
	// This is a placeholder implementation
	// In a real implementation, you'd execute the parameterized query
	return []map[string]interface{}{}, fmt.Errorf("custom search not yet implemented")
}

// SearchSensorsAdvanced performs advanced sensor search with complex filters
func (r *Repository) SearchSensorsAdvanced(req interface{}) ([]models.Sensor, error) {
	// This is a placeholder implementation
	// In a real implementation, you'd build and execute the search query
	return []models.Sensor{}, fmt.Errorf("advanced sensor search not yet implemented")
}

// SearchMeasurementsAdvanced performs advanced measurement search with complex filters
func (r *Repository) SearchMeasurementsAdvanced(req interface{}) ([]models.MeasurementResponse, error) {
	// This is a placeholder implementation
	// In a real implementation, you'd build and execute the search query
	return []models.MeasurementResponse{}, fmt.Errorf("advanced measurement search not yet implemented")
}

// Dataset management methods

func (r *Repository) CreateDataset(name, description string) (*models.Dataset, error) {
	var dataset models.Dataset
	query := fmt.Sprintf(`
		INSERT INTO %s.datasets (name, description)
		VALUES ($1, $2) RETURNING id, name, description, created_on`,
		r.db.Schema)

	err := r.db.QueryRow(query, name, description).Scan(
		&dataset.ID, &dataset.Name, &dataset.Description, &dataset.CreatedOn)
	if err != nil {
		return nil, fmt.Errorf("failed to create dataset: %w", err)
	}

	return &dataset, nil
}

func (r *Repository) GetDataset(datasetID uuid.UUID) (*models.Dataset, error) {
	var dataset models.Dataset
	query := fmt.Sprintf(`
		SELECT id, name, description, created_on
		FROM %s.datasets
		WHERE id = $1`,
		r.db.Schema)

	err := r.db.QueryRow(query, datasetID).Scan(
		&dataset.ID, &dataset.Name, &dataset.Description, &dataset.CreatedOn)
	if err != nil {
		return nil, fmt.Errorf("failed to get dataset: %w", err)
	}

	return &dataset, nil
}

func (r *Repository) GetDatasetWithTypes(datasetID uuid.UUID) (*models.DatasetResponse, error) {
	dataset, err := r.GetDataset(datasetID)
	if err != nil {
		return nil, err
	}

	query := fmt.Sprintf(`
		SELECT t.id, t.name, t.description, t.unit, t.data_type, t.metadata, dt.is_required
		FROM %s.dataset_types dt
		JOIN %s."types" t ON dt.type_id = t.id
		WHERE dt.dataset_id = $1
		ORDER BY t.name`,
		r.db.Schema, r.db.Schema)

	rows, err := r.db.Query(query, datasetID)
	if err != nil {
		return nil, fmt.Errorf("failed to get dataset types: %w", err)
	}
	defer rows.Close()

	var types []models.TypeInDataset
	for rows.Next() {
		var typeInDataset models.TypeInDataset
		err := rows.Scan(
			&typeInDataset.Type.ID, &typeInDataset.Type.Name, &typeInDataset.Type.Description,
			&typeInDataset.Type.Unit, &typeInDataset.Type.DataType, &typeInDataset.Type.Metadata,
			&typeInDataset.IsRequired)
		if err != nil {
			return nil, fmt.Errorf("failed to scan dataset type: %w", err)
		}
		types = append(types, typeInDataset)
	}

	return &models.DatasetResponse{
		Dataset: *dataset,
		Types:   types,
	}, nil
}

func (r *Repository) AddTypesToDataset(datasetID uuid.UUID, typeNames []string, isRequired bool) error {
	if len(typeNames) == 0 {
		return nil
	}

	// First, get the type IDs for the given names
	placeholders := make([]string, len(typeNames))
	args := make([]interface{}, len(typeNames))
	for i, name := range typeNames {
		placeholders[i] = fmt.Sprintf("$%d", i+1)
		args[i] = name
	}

	typeQuery := fmt.Sprintf(`
		SELECT id, name FROM %s."types" WHERE name IN (%s)`,
		r.db.Schema, strings.Join(placeholders, ","))

	rows, err := r.db.Query(typeQuery, args...)
	if err != nil {
		return fmt.Errorf("failed to get types: %w", err)
	}
	defer rows.Close()

	var typeIDs []int64
	foundTypes := make(map[string]bool)
	for rows.Next() {
		var typeID int64
		var typeName string
		if err := rows.Scan(&typeID, &typeName); err != nil {
			return fmt.Errorf("failed to scan type: %w", err)
		}
		typeIDs = append(typeIDs, typeID)
		foundTypes[typeName] = true
	}

	// Check if all types were found
	for _, name := range typeNames {
		if !foundTypes[name] {
			return fmt.Errorf("type '%s' not found", name)
		}
	}

	// Insert the dataset-type relationships
	for _, typeID := range typeIDs {
		insertQuery := fmt.Sprintf(`
			INSERT INTO %s.dataset_types (dataset_id, type_id, is_required)
			VALUES ($1, $2, $3)
			ON CONFLICT (dataset_id, type_id) DO UPDATE SET is_required = EXCLUDED.is_required`,
			r.db.Schema)

		_, err = r.db.Exec(insertQuery, datasetID, typeID, isRequired)
		if err != nil {
			return fmt.Errorf("failed to add type %d to dataset: %w", typeID, err)
		}
	}

	return nil
}

func (r *Repository) RemoveTypesFromDataset(datasetID uuid.UUID, typeNames []string) error {
	if len(typeNames) == 0 {
		return nil
	}

	placeholders := make([]string, len(typeNames))
	args := make([]interface{}, len(typeNames)+1)
	args[0] = datasetID
	for i, name := range typeNames {
		placeholders[i] = fmt.Sprintf("$%d", i+2)
		args[i+1] = name
	}

	deleteQuery := fmt.Sprintf(`
		DELETE FROM %s.dataset_types
		WHERE dataset_id = $1 AND type_id IN (
			SELECT id FROM %s."types" WHERE name IN (%s)
		)`,
		r.db.Schema, r.db.Schema, strings.Join(placeholders, ","))

	_, err := r.db.Exec(deleteQuery, args...)
	if err != nil {
		return fmt.Errorf("failed to remove types from dataset: %w", err)
	}

	return nil
}

func (r *Repository) FindSensorsByDatasetUpdated(datasetID uuid.UUID) ([]models.Sensor, error) {
	query := fmt.Sprintf(`
		SELECT DISTINCT s.id, s.name, s.parent_id, s.metadata, s.created_on, s.is_active, s.is_available
		FROM %s.sensors s
		JOIN %s.timeseries ts ON s.id = ts.sensor_id
		JOIN %s.types t ON ts.type_id = t.id
		JOIN %s.dataset_types dt ON t.id = dt.type_id
		WHERE dt.dataset_id = $1 AND s.is_active = true`,
		r.db.Schema, r.db.Schema, r.db.Schema, r.db.Schema)

	rows, err := r.db.Query(query, datasetID)
	if err != nil {
		return nil, fmt.Errorf("failed to find sensors by dataset: %w", err)
	}
	defer rows.Close()

	var sensors []models.Sensor
	for rows.Next() {
		var s models.Sensor

		err := rows.Scan(&s.ID, &s.Name, &s.ParentID, &s.Metadata, &s.CreatedOn, &s.IsActive, &s.IsAvailable)
		if err != nil {
			return nil, fmt.Errorf("failed to scan sensor: %w", err)
		}

		sensors = append(sensors, s)
	}

	return sensors, nil
}

// DiscoverSensorsByConditions discovers sensors based on their timeseries data and measurement conditions
func (r *Repository) DiscoverSensorsByConditions(req *filter.SensorDiscoveryRequest) ([]models.Sensor, error) {
	var args []interface{}
	argIndex := 1

	// Base query for sensors with timeseries
	baseQuery := fmt.Sprintf(`
		SELECT DISTINCT s.id, s.name, s.parent_id, s.metadata, s.created_on, s.is_active, s.is_available
		FROM %s.sensors s`, r.db.Schema)

	var joins []string
	var whereClauses []string
	whereClauses = append(whereClauses, "s.is_active = true")

	// Always join with timeseries for basic filtering
	joins = append(joins, fmt.Sprintf("JOIN %s.timeseries ts ON s.id = ts.sensor_id", r.db.Schema))
	joins = append(joins, fmt.Sprintf("JOIN %s.\"types\" t ON ts.type_id = t.id", r.db.Schema))

	// Handle timeseries filter if specified
	if req.TimeseriesFilter != nil {
		tf := req.TimeseriesFilter

		// Handle required types - sensors must have ALL of these types
		if len(tf.RequiredTypes) > 0 {
			placeholders := make([]string, len(tf.RequiredTypes))
			for i, typeName := range tf.RequiredTypes {
				placeholders[i] = fmt.Sprintf("$%d", argIndex)
				args = append(args, typeName)
				argIndex++
			}

			// Use a subquery to ensure sensor has ALL required types
			requiredTypeClause := fmt.Sprintf(`
				s.id IN (
					SELECT ts_req.sensor_id
					FROM %s.timeseries ts_req
					JOIN %s."types" t_req ON ts_req.type_id = t_req.id
					WHERE t_req.name IN (%s)
					GROUP BY ts_req.sensor_id
					HAVING COUNT(DISTINCT t_req.id) = %d
				)`, r.db.Schema, r.db.Schema, strings.Join(placeholders, ","), len(tf.RequiredTypes))
			whereClauses = append(whereClauses, requiredTypeClause)
		}

		// Handle optional types - sensors may have ANY of these types
		if len(tf.OptionalTypes) > 0 {
			placeholders := make([]string, len(tf.OptionalTypes))
			for i, typeName := range tf.OptionalTypes {
				placeholders[i] = fmt.Sprintf("$%d", argIndex)
				args = append(args, typeName)
				argIndex++
			}
			optionalTypeClause := fmt.Sprintf("t.name IN (%s)", strings.Join(placeholders, ","))
			whereClauses = append(whereClauses, optionalTypeClause)
		}

		// Handle dataset membership filter
		if len(tf.DatasetIDs) > 0 {
			placeholders := make([]string, len(tf.DatasetIDs))
			for i, datasetID := range tf.DatasetIDs {
				placeholders[i] = fmt.Sprintf("$%d", argIndex)
				args = append(args, datasetID)
				argIndex++
			}
			// Join with dataset_types to filter by dataset membership
			joins = append(joins, fmt.Sprintf("JOIN %s.dataset_types dt ON t.id = dt.type_id", r.db.Schema))
			datasetClause := fmt.Sprintf("dt.dataset_id::text IN (%s)", strings.Join(placeholders, ","))
			whereClauses = append(whereClauses, datasetClause)
		}
	}

	// Handle measurement filter if specified
	if req.MeasurementFilter != nil && req.MeasurementFilter.Expression != "" {
		mf := req.MeasurementFilter

		// Parse the filter expression
		parser := filter.NewFilterExpressionParser()
		expr, err := parser.ParseExpression(mf.Expression)
		if err != nil {
			return nil, fmt.Errorf("failed to parse measurement filter expression: %w", err)
		}

		// Convert to value conditions for SQL generation
		valueConditions, err := parser.ConvertToValueConditions(expr)
		if err != nil {
			return nil, fmt.Errorf("failed to convert filter expression: %w", err)
		}

		// Build measurement conditions
		measurementConditions, err := r.buildMeasurementConditions(valueConditions, mf, &args, &argIndex)
		if err != nil {
			return nil, fmt.Errorf("failed to build measurement conditions: %w", err)
		}

		if len(measurementConditions.joins) > 0 {
			joins = append(joins, measurementConditions.joins...)
		}
		if len(measurementConditions.whereClauses) > 0 {
			whereClauses = append(whereClauses, measurementConditions.whereClauses...)
		}
	}

	// Combine query parts
	query := baseQuery
	if len(joins) > 0 {
		query += " " + strings.Join(joins, " ")
	}
	if len(whereClauses) > 0 {
		query += " WHERE " + strings.Join(whereClauses, " AND ")
	}

	query += " ORDER BY s.name"

	// Apply limit
	if req.Limit > 0 {
		query += fmt.Sprintf(" LIMIT $%d", argIndex)
		args = append(args, req.Limit)
	}

	// Execute query
	rows, err := r.db.Query(query, args...)
	if err != nil {
		return nil, fmt.Errorf("failed to discover sensors: %w", err)
	}
	defer rows.Close()

	var sensors []models.Sensor
	for rows.Next() {
		var s models.Sensor
		err := rows.Scan(&s.ID, &s.Name, &s.ParentID, &s.Metadata, &s.CreatedOn, &s.IsActive, &s.IsAvailable)
		if err != nil {
			return nil, fmt.Errorf("failed to scan sensor: %w", err)
		}
		sensors = append(sensors, s)
	}

	return sensors, nil
}

// VerifyDiscoveredSensors verifies if given sensor names match the discovery filters
func (r *Repository) VerifyDiscoveredSensors(req *filter.SensorVerifyRequest) (*filter.SensorVerifyResponse, error) {
	// If no filters are specified, all sensors are considered verified
	if req.TimeseriesFilter == nil && req.MeasurementFilter == nil {
		return &filter.SensorVerifyResponse{
			OK:         true,
			Verified:   req.SensorNames,
			Unverified: []string{},
			Request:    req,
		}, nil
	}

	// Convert verify request to discovery request to reuse existing logic
	discoveryReq := &filter.SensorDiscoveryRequest{
		TimeseriesFilter:  req.TimeseriesFilter,
		MeasurementFilter: req.MeasurementFilter,
		Limit:             0, // No limit for verification
	}

	// Discover all sensors that match the filters
	matchingSensors, err := r.DiscoverSensorsByConditions(discoveryReq)
	if err != nil {
		return nil, fmt.Errorf("failed to discover sensors for verification: %w", err)
	}

	// Create a set of matching sensor names for quick lookup
	matchingNames := make(map[string]bool)
	for _, sensor := range matchingSensors {
		matchingNames[sensor.Name] = true
	}

	// Categorize the requested sensors
	var verified []string
	var unverified []string

	for _, sensorName := range req.SensorNames {
		if matchingNames[sensorName] {
			verified = append(verified, sensorName)
		} else {
			unverified = append(unverified, sensorName)
		}
	}

	// All sensors are OK if unverified list is empty
	allOK := len(unverified) == 0

	return &filter.SensorVerifyResponse{
		OK:         allOK,
		Verified:   verified,
		Unverified: unverified,
		Request:    req,
	}, nil
}

type measurementConditionResult struct {
	joins        []string
	whereClauses []string
}

// buildMeasurementConditions builds SQL conditions for measurement filtering
func (r *Repository) buildMeasurementConditions(valueConditions []filter.ValueCondition, mf *filter.MeasurementFilter, args *[]interface{}, argIndex *int) (*measurementConditionResult, error) {
	result := &measurementConditionResult{
		joins:        []string{},
		whereClauses: []string{},
	}

	processedTypes := make(map[string]bool)

	for _, condition := range valueConditions {
		// Skip if we've already processed this type
		if processedTypes[condition.TypeName] {
			continue
		}
		processedTypes[condition.TypeName] = true

		// Determine measurement table based on type
		var dataType models.DataType
		typeQuery := fmt.Sprintf(`SELECT data_type FROM %s."types" WHERE name = $1`, r.db.Schema)

		err := r.db.QueryRow(typeQuery, condition.TypeName).Scan(&dataType)
		if err != nil {
			return nil, fmt.Errorf("failed to get data type for %s: %w", condition.TypeName, err)
		}

		// Build measurement alias
		measurementAlias := fmt.Sprintf("m_%s", strings.ReplaceAll(condition.TypeName, ".", "_"))
		timeseriesAlias := fmt.Sprintf("ts_%s", strings.ReplaceAll(condition.TypeName, ".", "_"))

		// Build timeseries join for this type
		*args = append(*args, condition.TypeName)
		typeParamIndex := *argIndex
		*argIndex++

		timeseriesJoin := fmt.Sprintf(`
			JOIN %s.timeseries %s ON s.id = %s.sensor_id AND %s.type_id = (
				SELECT id FROM %s."types" WHERE name = $%d
			)`,
			r.db.Schema, timeseriesAlias, timeseriesAlias, timeseriesAlias,
			r.db.Schema, typeParamIndex)

		// Build measurement join based on latest_only flag
		var measurementJoin string
		if mf.LatestOnly {
			// For latest only, we need to use a different approach that works with value conditions
			// Use ROW_NUMBER() window function to get latest per timeseries and apply conditions
			measurementJoin = fmt.Sprintf(`
				JOIN (
					SELECT timeseries_id, value, timestamp,
						   ROW_NUMBER() OVER (PARTITION BY timeseries_id ORDER BY timestamp DESC) as rn
					FROM %s.measurements_%s
				) %s ON %s.timeseries_id = %s.id AND %s.rn = 1`,
				r.db.Schema, dataType, measurementAlias, measurementAlias, timeseriesAlias, measurementAlias)
		} else {
			// Check all measurements within time range
			measurementJoin = fmt.Sprintf(`
				JOIN %s.measurements_%s %s ON %s.timeseries_id = %s.id`,
				r.db.Schema, dataType, measurementAlias, measurementAlias, timeseriesAlias)

			// Add time range conditions if specified
			if mf.TimeRange != nil {
				if mf.TimeRange.StartTime != "" {
					result.whereClauses = append(result.whereClauses, fmt.Sprintf("%s.timestamp >= $%d", measurementAlias, *argIndex))
					*args = append(*args, mf.TimeRange.StartTime)
					*argIndex++
				}
				if mf.TimeRange.EndTime != "" {
					result.whereClauses = append(result.whereClauses, fmt.Sprintf("%s.timestamp <= $%d", measurementAlias, *argIndex))
					*args = append(*args, mf.TimeRange.EndTime)
					*argIndex++
				}
			}
		}

		result.joins = append(result.joins, timeseriesJoin, measurementJoin)
	}

	// Build value conditions for all conditions with the same type
	for _, condition := range valueConditions {
		measurementAlias := fmt.Sprintf("m_%s", strings.ReplaceAll(condition.TypeName, ".", "_"))

		// Build value condition based on operator and JSON path
		var valueCondition string
		if len(condition.JSONPath) > 0 {
			// JSON path filtering
			jsonPath := strings.Join(condition.JSONPath, ".")
			switch condition.Operator {
			case filter.OpEqual:
				valueCondition = fmt.Sprintf("%s.value->>'%s' = $%d", measurementAlias, jsonPath, *argIndex)
			case filter.OpNotEqual:
				valueCondition = fmt.Sprintf("%s.value->>'%s' != $%d", measurementAlias, jsonPath, *argIndex)
			case filter.OpLessThan:
				valueCondition = fmt.Sprintf("(%s.value->>'%s')::numeric < $%d", measurementAlias, jsonPath, *argIndex)
			case filter.OpGreaterThan:
				valueCondition = fmt.Sprintf("(%s.value->>'%s')::numeric > $%d", measurementAlias, jsonPath, *argIndex)
			case filter.OpLessThanOrEqual:
				valueCondition = fmt.Sprintf("(%s.value->>'%s')::numeric <= $%d", measurementAlias, jsonPath, *argIndex)
			case filter.OpGreaterThanOrEqual:
				valueCondition = fmt.Sprintf("(%s.value->>'%s')::numeric >= $%d", measurementAlias, jsonPath, *argIndex)
			case filter.OpRegex:
				valueCondition = fmt.Sprintf("%s.value->>'%s' ~ $%d", measurementAlias, jsonPath, *argIndex)
			case filter.OpInsensitiveRegex:
				valueCondition = fmt.Sprintf("%s.value->>'%s' ~* $%d", measurementAlias, jsonPath, *argIndex)
			case filter.OpNotRegex:
				valueCondition = fmt.Sprintf("%s.value->>'%s' !~ $%d", measurementAlias, jsonPath, *argIndex)
			case filter.OpNotInsensitiveRegex:
				valueCondition = fmt.Sprintf("%s.value->>'%s' !~* $%d", measurementAlias, jsonPath, *argIndex)
			case filter.OpIn:
				if list, ok := condition.Value.([]interface{}); ok {
					placeholders := make([]string, len(list))
					for i, val := range list {
						placeholders[i] = fmt.Sprintf("$%d", *argIndex)
						*args = append(*args, val)
						*argIndex++
					}
					valueCondition = fmt.Sprintf("%s.value->>'%s' IN (%s)", measurementAlias, jsonPath, strings.Join(placeholders, ","))
					continue
				}
			case filter.OpNotIn:
				if list, ok := condition.Value.([]interface{}); ok {
					placeholders := make([]string, len(list))
					for i, val := range list {
						placeholders[i] = fmt.Sprintf("$%d", *argIndex)
						*args = append(*args, val)
						*argIndex++
					}
					valueCondition = fmt.Sprintf("%s.value->>'%s' NOT IN (%s)", measurementAlias, jsonPath, strings.Join(placeholders, ","))
					continue
				}
			default:
				return nil, fmt.Errorf("unsupported operator for JSON path: %s", condition.Operator)
			}
		} else {
			// Direct value filtering
			switch condition.Operator {
			case filter.OpEqual:
				valueCondition = fmt.Sprintf("%s.value = $%d", measurementAlias, *argIndex)
			case filter.OpNotEqual:
				valueCondition = fmt.Sprintf("%s.value != $%d", measurementAlias, *argIndex)
			case filter.OpLessThan:
				valueCondition = fmt.Sprintf("(%s.value)::numeric < $%d", measurementAlias, *argIndex)
			case filter.OpGreaterThan:
				valueCondition = fmt.Sprintf("(%s.value)::numeric > $%d", measurementAlias, *argIndex)
			case filter.OpLessThanOrEqual:
				valueCondition = fmt.Sprintf("(%s.value)::numeric <= $%d", measurementAlias, *argIndex)
			case filter.OpGreaterThanOrEqual:
				valueCondition = fmt.Sprintf("(%s.value)::numeric >= $%d", measurementAlias, *argIndex)
			case filter.OpIn:
				if list, ok := condition.Value.([]interface{}); ok {
					placeholders := make([]string, len(list))
					for i, val := range list {
						placeholders[i] = fmt.Sprintf("$%d", *argIndex)
						*args = append(*args, val)
						*argIndex++
					}
					valueCondition = fmt.Sprintf("%s.value IN (%s)", measurementAlias, strings.Join(placeholders, ","))
					// Don't increment argIndex again since we already did it in the loop
					continue
				}
			case filter.OpNotIn:
				if list, ok := condition.Value.([]interface{}); ok {
					placeholders := make([]string, len(list))
					for i, val := range list {
						placeholders[i] = fmt.Sprintf("$%d", *argIndex)
						*args = append(*args, val)
						*argIndex++
					}
					valueCondition = fmt.Sprintf("%s.value NOT IN (%s)", measurementAlias, strings.Join(placeholders, ","))
					continue
				}
			case filter.OpRegex:
				valueCondition = fmt.Sprintf("%s.value ~ $%d", measurementAlias, *argIndex)
			case filter.OpInsensitiveRegex:
				valueCondition = fmt.Sprintf("%s.value ~* $%d", measurementAlias, *argIndex)
			case filter.OpNotRegex:
				valueCondition = fmt.Sprintf("%s.value !~ $%d", measurementAlias, *argIndex)
			case filter.OpNotInsensitiveRegex:
				valueCondition = fmt.Sprintf("%s.value !~* $%d", measurementAlias, *argIndex)
			case filter.OpBoundingBoxIntersect:
				// For geospatial data, handle both geoposition and geoshape
				if coords, ok := condition.Value.([]float64); ok && len(coords) == 4 {
					valueCondition = fmt.Sprintf("ST_Intersects(%s.value::geometry, ST_MakeEnvelope($%d, $%d, $%d, $%d, 4326))", measurementAlias, *argIndex, *argIndex+1, *argIndex+2, *argIndex+3)
					*args = append(*args, coords[0], coords[1], coords[2], coords[3])
					*argIndex += 4
					result.whereClauses = append(result.whereClauses, valueCondition)
					continue
				} else {
					return nil, fmt.Errorf("invalid coordinates for bounding box intersect: %v", condition.Value)
				}
			case filter.OpBoundingBoxContain:
				if coords, ok := condition.Value.([]float64); ok && len(coords) == 4 {
					valueCondition = fmt.Sprintf("ST_Contains(ST_MakeEnvelope($%d, $%d, $%d, $%d, 4326), %s.value::geometry)", *argIndex, *argIndex+1, *argIndex+2, *argIndex+3, measurementAlias)
					*args = append(*args, coords[0], coords[1], coords[2], coords[3])
					*argIndex += 4
					result.whereClauses = append(result.whereClauses, valueCondition)
					continue
				} else {
					return nil, fmt.Errorf("invalid coordinates for bounding box contain: %v", condition.Value)
				}
			case filter.OpDistanceLessThan:
				// Extract point and distance from value - format should be (lon,lat,distance)
				if coords, ok := condition.Value.([]float64); ok && len(coords) == 3 {
					valueCondition = fmt.Sprintf("ST_DWithin(%s.value::geometry, ST_SetSRID(ST_MakePoint($%d, $%d), 4326), $%d)", measurementAlias, *argIndex, *argIndex+1, *argIndex+2)
					*args = append(*args, coords[0], coords[1], coords[2])
					*argIndex += 3
					result.whereClauses = append(result.whereClauses, valueCondition)
					continue
				} else {
					return nil, fmt.Errorf("invalid coordinates for distance less than: %v", condition.Value)
				}
			default:
				return nil, fmt.Errorf("unsupported operator: %s", condition.Operator)
			}
		}

		result.whereClauses = append(result.whereClauses, valueCondition)
		*args = append(*args, condition.Value)
		*argIndex++
	}

	return result, nil
}
