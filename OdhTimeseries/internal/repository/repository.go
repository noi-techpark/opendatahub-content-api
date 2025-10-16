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
	"github.com/lib/pq"
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
		VALUES %s
		ON CONFLICT DO NOTHING;`,
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

func (r *Repository) GetDataset(datasetName string) (*models.Dataset, error) {
	var dataset models.Dataset
	query := fmt.Sprintf(`
		SELECT id, name, description, created_on
		FROM %s.datasets
		WHERE name = $1`,
		r.db.Schema)

	err := r.db.QueryRow(query, datasetName).Scan(
		&dataset.ID, &dataset.Name, &dataset.Description, &dataset.CreatedOn)
	if err != nil {
		return nil, fmt.Errorf("failed to get dataset: %w", err)
	}

	return &dataset, nil
}

// ListDatasets fetches all datasets, optionally including their associated types.
func (r *Repository) ListDatasets(withTypes bool) ([]models.DatasetResponse, error) {
	// 1. Fetch all base datasets
	datasetQuery := fmt.Sprintf(`
        SELECT id, name, description, created_on
        FROM %s.datasets
        ORDER BY name`,
		r.db.Schema)

	rows, err := r.db.Query(datasetQuery)
	if err != nil {
		return nil, fmt.Errorf("failed to list datasets: %w", err)
	}
	defer rows.Close()

	// Map to hold results: dataset ID -> DatasetResponse
	datasetsMap := make(map[string]models.DatasetResponse)
	var datasetIDs []string

	for rows.Next() {
		var ds models.Dataset
		if err := rows.Scan(&ds.ID, &ds.Name, &ds.Description, &ds.CreatedOn); err != nil {
			return nil, fmt.Errorf("failed to scan dataset: %w", err)
		}
		datasetsMap[ds.ID.String()] = models.DatasetResponse{Dataset: ds, Types: []models.TypeInDataset{}}
		datasetIDs = append(datasetIDs, ds.ID.String())
	}

	// 2. Conditionally fetch types
	if withTypes && len(datasetIDs) > 0 {

		typeQuery := fmt.Sprintf(`
            SELECT
                dt.dataset_id, t.id, t.name, t.description, t.unit, t.data_type, t.metadata, dt.is_required
            FROM %s.dataset_types dt
            JOIN %s."types" t ON dt.type_id = t.id
            WHERE dt.dataset_id = ANY($1::uuid[])
            ORDER BY dt.dataset_id, t.name`,
			r.db.Schema, r.db.Schema)

		typeRows, err := r.db.Query(typeQuery, pq.Array(datasetIDs))
		if err != nil {
			// Updated error message to include the SQL error correctly
			return nil, fmt.Errorf("failed to list dataset types: %w", err)
		}
		defer typeRows.Close()

		for typeRows.Next() {
			var datasetID uuid.UUID
			var typeInDataset models.TypeInDataset

			err := typeRows.Scan(
				&datasetID, &typeInDataset.Type.ID, &typeInDataset.Type.Name,
				&typeInDataset.Type.Description, &typeInDataset.Type.Unit,
				&typeInDataset.Type.DataType, &typeInDataset.Type.Metadata,
				&typeInDataset.IsRequired)
			if err != nil {
				return nil, fmt.Errorf("failed to scan dataset type during list: %w", err)
			}

			// Append the type to the correct dataset in the map
			dsResponse := datasetsMap[datasetID.String()]
			dsResponse.Types = append(dsResponse.Types, typeInDataset)
			datasetsMap[datasetID.String()] = dsResponse
		}
	}

	// 3. Convert map values back to a slice
	var finalResponse []models.DatasetResponse
	// Iterate over the sorted original IDs to maintain order
	for _, id := range datasetIDs {
		finalResponse = append(finalResponse, datasetsMap[id])
	}

	return finalResponse, nil
}

func (r *Repository) GetDatasetWithTypes(datasetName string) (*models.DatasetResponse, error) {
	dataset, err := r.GetDataset(datasetName)
	if err != nil {
		return nil, err
	}

	query := fmt.Sprintf(`
		SELECT t.id, t.name, t.description, t.unit, t.data_type, t.metadata, dt.is_required
		FROM %s.dataset_types dt
		JOIN %s."types" t ON dt.type_id = t.id
		JOIN %s.datasets d ON dt.dataset_id = d.id
		WHERE d.name = $1
		ORDER BY t.name`,
		r.db.Schema, r.db.Schema, r.db.Schema)

	rows, err := r.db.Query(query, datasetName)
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

func (r *Repository) FindSensorsByDatasetUpdated(datasetName string) ([]models.Sensor, error) {
	query := fmt.Sprintf(`
		SELECT DISTINCT s.id, s.name, s.parent_id, s.metadata, s.created_on, s.is_active, s.is_available
		FROM %s.sensors s
		JOIN %s.timeseries ts ON s.id = ts.sensor_id
		JOIN %s.types t ON ts.type_id = t.id
		JOIN %s.dataset_types dt ON t.id = dt.type_id
		JOIN %s.datasets d ON d.id = dt.dataset_id
		WHERE d.name = $1 AND s.is_active = true`,
		r.db.Schema, r.db.Schema, r.db.Schema, r.db.Schema, r.db.Schema)

	rows, err := r.db.Query(query, datasetName)
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
			// Treat tf.DatasetIDs as a list of Dataset Names for this logic.
			datasetNames := tf.DatasetIDs

			placeholders := make([]string, len(datasetNames))
			for i, datasetName := range datasetNames {
				placeholders[i] = fmt.Sprintf("$%d", argIndex)
				args = append(args, datasetName)
				argIndex++
			}

			// Join with dataset_types (dt) AND datasets (d) to filter by dataset name
			joins = append(joins, fmt.Sprintf("JOIN %s.dataset_types dt ON t.id = dt.type_id", r.db.Schema))
			joins = append(joins, fmt.Sprintf("JOIN %s.datasets d ON dt.dataset_id = d.id", r.db.Schema))

			// Filter using the dataset name
			datasetClause := fmt.Sprintf("d.name IN (%s)", strings.Join(placeholders, ","))
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

// Type management methods

// ListTypes fetches all types with optional pagination and sensor inclusion
func (r *Repository) ListTypes(offset, limit int, includeSensors bool) ([]models.TypeWithSensors, int, error) {
	// Get total count
	countQuery := fmt.Sprintf(`SELECT COUNT(*) FROM %s."types"`, r.db.Schema)
	var total int
	if err := r.db.QueryRow(countQuery).Scan(&total); err != nil {
		return nil, 0, fmt.Errorf("failed to count types: %w", err)
	}

	// Build query for types
	typeQuery := fmt.Sprintf(`
		SELECT id, name, description, unit, data_type, metadata
		FROM %s."types"
		ORDER BY name
		LIMIT $1 OFFSET $2`,
		r.db.Schema)

	rows, err := r.db.Query(typeQuery, limit, offset)
	if err != nil {
		return nil, 0, fmt.Errorf("failed to list types: %w", err)
	}
	defer rows.Close()

	var results []models.TypeWithSensors
	typeIDs := []int64{}

	for rows.Next() {
		var t models.Type
		if err := rows.Scan(&t.ID, &t.Name, &t.Description, &t.Unit, &t.DataType, &t.Metadata); err != nil {
			return nil, 0, fmt.Errorf("failed to scan type: %w", err)
		}
		results = append(results, models.TypeWithSensors{
			Type:    t,
			Sensors: []models.SensorTimeseriesInfo{},
		})
		typeIDs = append(typeIDs, t.ID)
	}

	// If includeSensors is true, fetch sensor-timeseries info for each type
	if includeSensors && len(typeIDs) > 0 {
		// Build query to get sensors and timeseries for these types
		placeholders := make([]string, len(typeIDs))
		args := make([]interface{}, len(typeIDs))
		for i, id := range typeIDs {
			placeholders[i] = fmt.Sprintf("$%d", i+1)
			args[i] = id
		}

		sensorQuery := fmt.Sprintf(`
			SELECT t.id, s.name, ts.id
			FROM %s."types" t
			JOIN %s.timeseries ts ON t.id = ts.type_id
			JOIN %s.sensors s ON ts.sensor_id = s.id
			WHERE t.id IN (%s) AND s.is_active = true
			ORDER BY t.id, s.name`,
			r.db.Schema, r.db.Schema, r.db.Schema, strings.Join(placeholders, ","))

		sensorRows, err := r.db.Query(sensorQuery, args...)
		if err != nil {
			return nil, 0, fmt.Errorf("failed to fetch sensors for types: %w", err)
		}
		defer sensorRows.Close()

		// Map type ID to index in results
		typeIndexMap := make(map[int64]int)
		for i, typeID := range typeIDs {
			typeIndexMap[typeID] = i
		}

		for sensorRows.Next() {
			var typeID int64
			var sensorName string
			var timeseriesID uuid.UUID

			if err := sensorRows.Scan(&typeID, &sensorName, &timeseriesID); err != nil {
				return nil, 0, fmt.Errorf("failed to scan sensor info: %w", err)
			}

			if idx, ok := typeIndexMap[typeID]; ok {
				results[idx].Sensors = append(results[idx].Sensors, models.SensorTimeseriesInfo{
					SensorName:   sensorName,
					TimeseriesID: timeseriesID,
				})
			}
		}
	}

	return results, total, nil
}

// GetTypeByName fetches a type by name with all sensors that have timeseries for this type
func (r *Repository) GetTypeByName(typeName string) (*models.TypeWithSensors, error) {
	// Get the type
	var t models.Type
	typeQuery := fmt.Sprintf(`
		SELECT id, name, description, unit, data_type, metadata
		FROM %s."types"
		WHERE name = $1`,
		r.db.Schema)

	err := r.db.QueryRow(typeQuery, typeName).Scan(
		&t.ID, &t.Name, &t.Description, &t.Unit, &t.DataType, &t.Metadata)
	if err != nil {
		if err == sql.ErrNoRows {
			return nil, fmt.Errorf("type '%s' not found", typeName)
		}
		return nil, fmt.Errorf("failed to get type: %w", err)
	}

	// Get all sensors that have timeseries for this type
	sensorQuery := fmt.Sprintf(`
		SELECT s.name, ts.id
		FROM %s.timeseries ts
		JOIN %s.sensors s ON ts.sensor_id = s.id
		WHERE ts.type_id = $1 AND s.is_active = true
		ORDER BY s.name`,
		r.db.Schema, r.db.Schema)

	rows, err := r.db.Query(sensorQuery, t.ID)
	if err != nil {
		return nil, fmt.Errorf("failed to get sensors for type: %w", err)
	}
	defer rows.Close()

	var sensors []models.SensorTimeseriesInfo
	for rows.Next() {
		var sensorName string
		var timeseriesID uuid.UUID

		if err := rows.Scan(&sensorName, &timeseriesID); err != nil {
			return nil, fmt.Errorf("failed to scan sensor info: %w", err)
		}

		sensors = append(sensors, models.SensorTimeseriesInfo{
			SensorName:   sensorName,
			TimeseriesID: timeseriesID,
		})
	}

	return &models.TypeWithSensors{
		Type:    t,
		Sensors: sensors,
	}, nil
}

// Sensor timeseries methods

// GetSensorTimeseriesByName fetches all timeseries for a sensor by name, optionally filtered by type names
func (r *Repository) GetSensorTimeseriesByName(sensorName string, typeNames []string) (*models.SensorTimeseriesResponse, error) {
	// First get the sensor
	var sensor models.Sensor
	sensorQuery := fmt.Sprintf(`
		SELECT id, name, parent_id, metadata, created_on, is_active, is_available
		FROM %s.sensors
		WHERE name = $1`,
		r.db.Schema)

	err := r.db.QueryRow(sensorQuery, sensorName).Scan(
		&sensor.ID, &sensor.Name, &sensor.ParentID, &sensor.Metadata,
		&sensor.CreatedOn, &sensor.IsActive, &sensor.IsAvailable)
	if err != nil {
		if err == sql.ErrNoRows {
			return nil, fmt.Errorf("sensor '%s' not found", sensorName)
		}
		return nil, fmt.Errorf("failed to get sensor: %w", err)
	}

	// Build query for timeseries
	var timeseriesQuery string
	var args []interface{}
	args = append(args, sensor.ID)

	if len(typeNames) > 0 {
		// Filter by type names
		placeholders := make([]string, len(typeNames))
		for i, typeName := range typeNames {
			placeholders[i] = fmt.Sprintf("$%d", i+2)
			args = append(args, typeName)
		}
		timeseriesQuery = fmt.Sprintf(`
			SELECT ts.id, t.name, t.id, t.description, t.unit, t.data_type, t.metadata
			FROM %s.timeseries ts
			JOIN %s."types" t ON ts.type_id = t.id
			WHERE ts.sensor_id = $1 AND t.name IN (%s)
			ORDER BY t.name`,
			r.db.Schema, r.db.Schema, strings.Join(placeholders, ","))
	} else {
		// Get all timeseries
		timeseriesQuery = fmt.Sprintf(`
			SELECT ts.id, t.name, t.id, t.description, t.unit, t.data_type, t.metadata
			FROM %s.timeseries ts
			JOIN %s."types" t ON ts.type_id = t.id
			WHERE ts.sensor_id = $1
			ORDER BY t.name`,
			r.db.Schema, r.db.Schema)
	}

	rows, err := r.db.Query(timeseriesQuery, args...)
	if err != nil {
		return nil, fmt.Errorf("failed to get timeseries for sensor: %w", err)
	}
	defer rows.Close()

	var timeseries []models.TimeseriesInfo
	for rows.Next() {
		var ts models.TimeseriesInfo
		err := rows.Scan(
			&ts.TimeseriesID, &ts.TypeName,
			&ts.TypeInfo.ID, &ts.TypeInfo.Description, &ts.TypeInfo.Unit,
			&ts.TypeInfo.DataType, &ts.TypeInfo.Metadata)
		if err != nil {
			return nil, fmt.Errorf("failed to scan timeseries: %w", err)
		}
		ts.TypeInfo.Name = ts.TypeName
		timeseries = append(timeseries, ts)
	}

	return &models.SensorTimeseriesResponse{
		SensorName: sensor.Name,
		SensorID:   sensor.ID,
		Timeseries: timeseries,
		Total:      len(timeseries),
	}, nil
}

// GetBatchSensorTimeseries fetches timeseries for multiple sensors
func (r *Repository) GetBatchSensorTimeseries(sensorNames []string, typeNames []string) (*models.BatchSensorTimeseriesResponse, error) {
	if len(sensorNames) == 0 {
		return &models.BatchSensorTimeseriesResponse{
			Sensors: []models.SensorTimeseriesResponse{},
			Total:   0,
		}, nil
	}

	// First get all sensors
	placeholders := make([]string, len(sensorNames))
	args := make([]interface{}, len(sensorNames))
	for i, name := range sensorNames {
		placeholders[i] = fmt.Sprintf("$%d", i+1)
		args[i] = name
	}

	sensorQuery := fmt.Sprintf(`
		SELECT id, name, parent_id, metadata, created_on, is_active, is_available
		FROM %s.sensors
		WHERE name IN (%s)
		ORDER BY name`,
		r.db.Schema, strings.Join(placeholders, ","))

	rows, err := r.db.Query(sensorQuery, args...)
	if err != nil {
		return nil, fmt.Errorf("failed to get sensors: %w", err)
	}
	defer rows.Close()

	var sensors []models.Sensor
	sensorIDMap := make(map[int64]int) // Map sensor ID to index in result array
	for rows.Next() {
		var s models.Sensor
		err := rows.Scan(&s.ID, &s.Name, &s.ParentID, &s.Metadata, &s.CreatedOn, &s.IsActive, &s.IsAvailable)
		if err != nil {
			return nil, fmt.Errorf("failed to scan sensor: %w", err)
		}
		sensorIDMap[s.ID] = len(sensors)
		sensors = append(sensors, s)
	}

	// Build query for timeseries
	if len(sensors) == 0 {
		return &models.BatchSensorTimeseriesResponse{
			Sensors: []models.SensorTimeseriesResponse{},
			Total:   0,
		}, nil
	}

	sensorIDs := make([]interface{}, len(sensors))
	sensorPlaceholders := make([]string, len(sensors))
	for i, s := range sensors {
		sensorPlaceholders[i] = fmt.Sprintf("$%d", i+1)
		sensorIDs[i] = s.ID
	}

	var timeseriesQuery string
	var timeseriesArgs []interface{}
	timeseriesArgs = append(timeseriesArgs, sensorIDs...)

	if len(typeNames) > 0 {
		// Filter by type names
		typePlaceholders := make([]string, len(typeNames))
		for i, typeName := range typeNames {
			typePlaceholders[i] = fmt.Sprintf("$%d", len(sensors)+i+1)
			timeseriesArgs = append(timeseriesArgs, typeName)
		}
		timeseriesQuery = fmt.Sprintf(`
			SELECT ts.sensor_id, ts.id, t.name, t.id, t.description, t.unit, t.data_type, t.metadata
			FROM %s.timeseries ts
			JOIN %s."types" t ON ts.type_id = t.id
			WHERE ts.sensor_id IN (%s) AND t.name IN (%s)
			ORDER BY ts.sensor_id, t.name`,
			r.db.Schema, r.db.Schema, strings.Join(sensorPlaceholders, ","), strings.Join(typePlaceholders, ","))
	} else {
		// Get all timeseries
		timeseriesQuery = fmt.Sprintf(`
			SELECT ts.sensor_id, ts.id, t.name, t.id, t.description, t.unit, t.data_type, t.metadata
			FROM %s.timeseries ts
			JOIN %s."types" t ON ts.type_id = t.id
			WHERE ts.sensor_id IN (%s)
			ORDER BY ts.sensor_id, t.name`,
			r.db.Schema, r.db.Schema, strings.Join(sensorPlaceholders, ","))
	}

	tsRows, err := r.db.Query(timeseriesQuery, timeseriesArgs...)
	if err != nil {
		return nil, fmt.Errorf("failed to get timeseries: %w", err)
	}
	defer tsRows.Close()

	// Initialize result array
	results := make([]models.SensorTimeseriesResponse, len(sensors))
	for i, s := range sensors {
		results[i] = models.SensorTimeseriesResponse{
			SensorName: s.Name,
			SensorID:   s.ID,
			Timeseries: []models.TimeseriesInfo{},
			Total:      0,
		}
	}

	// Add timeseries to corresponding sensors
	for tsRows.Next() {
		var sensorID int64
		var ts models.TimeseriesInfo
		err := tsRows.Scan(
			&sensorID, &ts.TimeseriesID, &ts.TypeName,
			&ts.TypeInfo.ID, &ts.TypeInfo.Description, &ts.TypeInfo.Unit,
			&ts.TypeInfo.DataType, &ts.TypeInfo.Metadata)
		if err != nil {
			return nil, fmt.Errorf("failed to scan timeseries: %w", err)
		}
		ts.TypeInfo.Name = ts.TypeName

		if idx, ok := sensorIDMap[sensorID]; ok {
			results[idx].Timeseries = append(results[idx].Timeseries, ts)
			results[idx].Total++
		}
	}

	return &models.BatchSensorTimeseriesResponse{
		Sensors: results,
		Total:   len(results),
	}, nil
}

// GetBatchSensorTypes fetches types for multiple sensors with optional distinct filtering
func (r *Repository) GetBatchSensorTypes(sensorNames []string, distinct bool) (*models.BatchSensorTypesResponse, error) {
	if len(sensorNames) == 0 {
		return &models.BatchSensorTypesResponse{
			Sensors: []models.SensorTypesResponse{},
			Types:   []models.Type{},
			Total:   0,
		}, nil
	}

	// First get all sensors
	placeholders := make([]string, len(sensorNames))
	args := make([]interface{}, len(sensorNames))
	for i, name := range sensorNames {
		placeholders[i] = fmt.Sprintf("$%d", i+1)
		args[i] = name
	}

	sensorQuery := fmt.Sprintf(`
		SELECT id, name, parent_id, metadata, created_on, is_active, is_available
		FROM %s.sensors
		WHERE name IN (%s)
		ORDER BY name`,
		r.db.Schema, strings.Join(placeholders, ","))

	rows, err := r.db.Query(sensorQuery, args...)
	if err != nil {
		return nil, fmt.Errorf("failed to get sensors: %w", err)
	}
	defer rows.Close()

	var sensors []models.Sensor
	sensorIDMap := make(map[int64]int) // Map sensor ID to index in result array
	for rows.Next() {
		var s models.Sensor
		err := rows.Scan(&s.ID, &s.Name, &s.ParentID, &s.Metadata, &s.CreatedOn, &s.IsActive, &s.IsAvailable)
		if err != nil {
			return nil, fmt.Errorf("failed to scan sensor: %w", err)
		}
		sensorIDMap[s.ID] = len(sensors)
		sensors = append(sensors, s)
	}

	if len(sensors) == 0 {
		return &models.BatchSensorTypesResponse{
			Sensors: []models.SensorTypesResponse{},
			Types:   []models.Type{},
			Total:   0,
		}, nil
	}

	// Build query for types and timeseries
	sensorIDs := make([]interface{}, len(sensors))
	sensorPlaceholders := make([]string, len(sensors))
	for i, s := range sensors {
		sensorPlaceholders[i] = fmt.Sprintf("$%d", i+1)
		sensorIDs[i] = s.ID
	}

	// If distinct mode, we only need the types, not sensor-specific info
	if distinct {
		typesQuery := fmt.Sprintf(`
			SELECT DISTINCT t.id, t.name, t.description, t.unit, t.data_type, t.metadata
			FROM %s.timeseries ts
			JOIN %s."types" t ON ts.type_id = t.id
			WHERE ts.sensor_id IN (%s)
			ORDER BY t.name`,
			r.db.Schema, r.db.Schema, strings.Join(sensorPlaceholders, ","))

		typeRows, err := r.db.Query(typesQuery, sensorIDs...)
		if err != nil {
			return nil, fmt.Errorf("failed to get distinct types: %w", err)
		}
		defer typeRows.Close()

		var distinctTypes []models.Type
		for typeRows.Next() {
			var t models.Type
			err := typeRows.Scan(&t.ID, &t.Name, &t.Description, &t.Unit, &t.DataType, &t.Metadata)
			if err != nil {
				return nil, fmt.Errorf("failed to scan type: %w", err)
			}
			distinctTypes = append(distinctTypes, t)
		}

		return &models.BatchSensorTypesResponse{
			Types: distinctTypes,
			Total: len(distinctTypes),
		}, nil
	}

	// Non-distinct mode: return detailed sensor-type breakdown
	typesQuery := fmt.Sprintf(`
		SELECT ts.sensor_id, s.name as sensor_name, ts.id as timeseries_id,
			   t.id, t.name, t.description, t.unit, t.data_type, t.metadata
		FROM %s.timeseries ts
		JOIN %s."types" t ON ts.type_id = t.id
		JOIN %s.sensors s ON ts.sensor_id = s.id
		WHERE ts.sensor_id IN (%s)
		ORDER BY s.name, t.name`,
		r.db.Schema, r.db.Schema, r.db.Schema, strings.Join(sensorPlaceholders, ","))

	typeRows, err := r.db.Query(typesQuery, sensorIDs...)
	if err != nil {
		return nil, fmt.Errorf("failed to get types: %w", err)
	}
	defer typeRows.Close()

	// Initialize result array
	results := make([]models.SensorTypesResponse, len(sensors))
	for i, s := range sensors {
		results[i] = models.SensorTypesResponse{
			SensorName: s.Name,
			SensorID:   s.ID,
			Types:      []models.TypeWithTimeseries{},
			Total:      0,
		}
	}

	// Add types to corresponding sensors
	for typeRows.Next() {
		var sensorID int64
		var sensorName string
		var typeWithTs models.TypeWithTimeseries

		err := typeRows.Scan(
			&sensorID, &sensorName, &typeWithTs.TimeseriesID,
			&typeWithTs.TypeInfo.ID, &typeWithTs.TypeInfo.Name, &typeWithTs.TypeInfo.Description,
			&typeWithTs.TypeInfo.Unit, &typeWithTs.TypeInfo.DataType, &typeWithTs.TypeInfo.Metadata)
		if err != nil {
			return nil, fmt.Errorf("failed to scan type: %w", err)
		}
		typeWithTs.SensorName = sensorName

		if idx, ok := sensorIDMap[sensorID]; ok {
			results[idx].Types = append(results[idx].Types, typeWithTs)
			results[idx].Total++
		}
	}

	return &models.BatchSensorTypesResponse{
		Sensors: results,
		Total:   len(results),
	}, nil
}
