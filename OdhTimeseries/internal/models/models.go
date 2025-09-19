package models

import (
	"encoding/json"
	"time"

	"github.com/google/uuid"
	"github.com/paulmach/orb"
)

type DataType string

const (
	DataTypeNumeric     DataType = "numeric"
	DataTypeString      DataType = "string"
	DataTypeJSON        DataType = "json"
	DataTypeGeoposition DataType = "geoposition"
	DataTypeGeoshape    DataType = "geoshape"
	DataTypeBoolean     DataType = "boolean"
)

type TimeseriesType string

const (
	TimeseriesTypeScheduled   TimeseriesType = "scheduled"
	TimeseriesTypePrediction  TimeseriesType = "prediction"
	TimeseriesTypeAggregation TimeseriesType = "aggregation"
	TimeseriesTypeRealtime    TimeseriesType = "realtime"
)

type Provenance struct {
	ID                   int64  `json:"id"`
	UUID                 string `json:"uuid"`
	Lineage              string `json:"lineage"`
	DataCollector        string `json:"data_collector"`
	DataCollectorVersion string `json:"data_collector_version,omitempty"`
}

type Sensor struct {
	ID          int64           `json:"id"`
	Name        string          `json:"name"`
	ParentID    *int64          `json:"parent_id,omitempty"`
	Metadata    json.RawMessage `json:"metadata,omitempty" swaggertype:"string" example:"{\"location\":\"downtown\"}"`
	CreatedOn   time.Time       `json:"created_on"`
	IsActive    bool            `json:"is_active"`
	IsAvailable bool            `json:"is_available"`
}

type Type struct {
	ID          int64           `json:"id"`
	Name        string          `json:"name"`
	Description string          `json:"description,omitempty"`
	Unit        string          `json:"unit,omitempty"`
	DataType    DataType        `json:"data_type"`
	Metadata    json.RawMessage `json:"metadata,omitempty" swaggertype:"string" example:"{\"category\":\"environmental\"}"`
}

type Dataset struct {
	ID          uuid.UUID `json:"id"`
	Name        string    `json:"name"`
	Description string    `json:"description,omitempty"`
	CreatedOn   time.Time `json:"created_on"`
}

type DatasetType struct {
	ID         int64     `json:"id"`
	DatasetID  uuid.UUID `json:"dataset_id"`
	TypeID     int64     `json:"type_id"`
	IsRequired bool      `json:"is_required"`
	CreatedOn  time.Time `json:"created_on"`
}

type Timeseries struct {
	ID        uuid.UUID `json:"id"`
	SensorID  int64     `json:"sensor_id"`
	TypeID    int64     `json:"type_id"`
	CreatedOn time.Time `json:"created_on"`
}

type Measurement struct {
	TimeseriesID uuid.UUID   `json:"timeseries_id"`
	Timestamp    time.Time   `json:"timestamp"`
	Value        interface{} `json:"value"`
	ProvenanceID *int64      `json:"provenance_id,omitempty"`
	CreatedOn    time.Time   `json:"created_on"`
}

type NumericMeasurement struct {
	TimeseriesID uuid.UUID `json:"timeseries_id"`
	Timestamp    time.Time `json:"timestamp"`
	Value        float64   `json:"value"`
	ProvenanceID *int64    `json:"provenance_id,omitempty"`
	CreatedOn    time.Time `json:"created_on"`
}

type StringMeasurement struct {
	TimeseriesID uuid.UUID `json:"timeseries_id"`
	Timestamp    time.Time `json:"timestamp"`
	Value        string    `json:"value"`
	ProvenanceID *int64    `json:"provenance_id,omitempty"`
	CreatedOn    time.Time `json:"created_on"`
}

type JSONMeasurement struct {
	TimeseriesID uuid.UUID       `json:"timeseries_id"`
	Timestamp    time.Time       `json:"timestamp"`
	Value        json.RawMessage `json:"value"`
	ProvenanceID *int64          `json:"provenance_id,omitempty"`
	CreatedOn    time.Time       `json:"created_on"`
}

type GeopositionMeasurement struct {
	TimeseriesID uuid.UUID  `json:"timeseries_id"`
	Timestamp    time.Time  `json:"timestamp"`
	Value        *orb.Point `json:"value"`
	ProvenanceID *int64     `json:"provenance_id,omitempty"`
	CreatedOn    time.Time  `json:"created_on"`
}

type GeoshapeMeasurement struct {
	TimeseriesID uuid.UUID    `json:"timeseries_id"`
	Timestamp    time.Time    `json:"timestamp"`
	Value        *orb.Polygon `json:"value"`
	ProvenanceID *int64       `json:"provenance_id,omitempty"`
	CreatedOn    time.Time    `json:"created_on"`
}

type BooleanMeasurement struct {
	TimeseriesID uuid.UUID `json:"timeseries_id"`
	Timestamp    time.Time `json:"timestamp"`
	Value        bool      `json:"value"`
	ProvenanceID *int64    `json:"provenance_id,omitempty"`
	CreatedOn    time.Time `json:"created_on"`
}

// Request/Response DTOs

type BatchDataRequest struct {
	Provenance   *Provenance           `json:"provenance,omitempty"`
	Measurements []MeasurementWithMeta `json:"measurements"`
}

type MeasurementWithMeta struct {
	SensorName string      `json:"sensor_name"`
	TypeName   string      `json:"type_name"`
	Timestamp  time.Time   `json:"timestamp"`
	Value      interface{} `json:"value"`
}

type DeleteMeasurementsRequest struct {
	SensorNames []string   `json:"sensor_names,omitempty"`
	TypeNames   []string   `json:"type_names,omitempty"`
	StartTime   *time.Time `json:"start_time,omitempty"`
	EndTime     *time.Time `json:"end_time,omitempty"`
}

type LatestMeasurementsRequest struct {
	SensorNames []string `json:"sensor_names"`
	TypeNames   []string `json:"type_names,omitempty"`
}

type HistoricalMeasurementsRequest struct {
	SensorNames []string   `json:"sensor_names"`
	TypeNames   []string   `json:"type_names,omitempty"`
	StartTime   *time.Time `json:"start_time,omitempty"`
	EndTime     *time.Time `json:"end_time,omitempty"`
	Limit       int        `json:"limit,omitempty"`
}

type SensorDiscoveryRequest struct {
	DatasetID  *uuid.UUID `json:"dataset_id,omitempty"`
	TypeName   *string    `json:"type_name,omitempty"`
	SensorType *string    `json:"sensor_type,omitempty"`
	Location   *orb.Point `json:"location,omitempty"`
	Radius     *float64   `json:"radius,omitempty"`
}

type MeasurementResponse struct {
	TimeseriesID uuid.UUID   `json:"timeseries_id"`
	SensorName   string      `json:"sensor_name"`
	TypeName     string      `json:"type_name"`
	DataType     DataType    `json:"data_type"`
	Timestamp    time.Time   `json:"timestamp"`
	Value        interface{} `json:"value"`
}

// Dataset management DTOs

type CreateDatasetRequest struct {
	Name        string   `json:"name"`
	Description string   `json:"description,omitempty"`
	TypeNames   []string `json:"type_names,omitempty"`
}

type DatasetResponse struct {
	Dataset Dataset         `json:"dataset"`
	Types   []TypeInDataset `json:"types"`
}

type TypeInDataset struct {
	Type       Type `json:"type"`
	IsRequired bool `json:"is_required"`
}

type AddTypesToDatasetRequest struct {
	TypeNames  []string `json:"type_names"`
	IsRequired bool     `json:"is_required"`
}
