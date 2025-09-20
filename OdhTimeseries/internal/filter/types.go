package filter

import (
	"fmt"
	"strings"
	"time"
)

// FilterOperator represents the type of operation to perform
type FilterOperator string

const (
	// Comparison operators
	OpEqual              FilterOperator = "eq"
	OpNotEqual           FilterOperator = "neq"
	OpLessThan          FilterOperator = "lt"
	OpGreaterThan       FilterOperator = "gt"
	OpLessThanOrEqual   FilterOperator = "lteq"
	OpGreaterThanOrEqual FilterOperator = "gteq"

	// Pattern matching operators
	OpRegex             FilterOperator = "re"
	OpInsensitiveRegex  FilterOperator = "ire"
	OpNotRegex          FilterOperator = "nre"
	OpNotInsensitiveRegex FilterOperator = "nire"

	// List operators
	OpIn                FilterOperator = "in"
	OpNotIn             FilterOperator = "nin"

	// Geospatial operators
	OpBoundingBoxIntersect FilterOperator = "bbi"
	OpBoundingBoxContain   FilterOperator = "bbc"
	OpDistanceLessThan     FilterOperator = "dlt"

	// Logical operators
	OpAnd               FilterOperator = "and"
	OpOr                FilterOperator = "or"
)

// ValueType represents the type of value in a filter condition
type ValueType string

const (
	ValueTypeString   ValueType = "string"
	ValueTypeNumber   ValueType = "number"
	ValueTypeBoolean  ValueType = "boolean"
	ValueTypeNull     ValueType = "null"
	ValueTypeList     ValueType = "list"
	ValueTypeGeopoint ValueType = "geopoint"
	ValueTypeJSON     ValueType = "json"
)

// FilterCondition represents a single filter condition
type FilterCondition struct {
	Alias     string         `json:"alias"`     // The field to filter on (can be empty for measurement values)
	JSONPath  []string       `json:"json_path"` // Path for JSON field access (e.g., ["location", "city"])
	Operator  FilterOperator `json:"operator"`  // The operation to perform
	Value     interface{}    `json:"value"`     // The value(s) to compare against
	ValueType ValueType      `json:"value_type"` // The type of the value
}

// LogicalGroup represents a group of conditions with logical operations
type LogicalGroup struct {
	Operator   FilterOperator     `json:"operator"`   // "and" or "or"
	Conditions []FilterCondition  `json:"conditions"` // Individual conditions
	Groups     []LogicalGroup     `json:"groups"`     // Nested logical groups
}

// SensorDiscoveryRequest represents a request to find sensors based on their timeseries and measurements
type SensorDiscoveryRequest struct {
	// Filter sensors by the timeseries types they "own"
	TimeseriesFilter *TimeseriesFilter `json:"timeseries_filter,omitempty"`

	// Filter sensors by their measurement values
	MeasurementFilter *MeasurementFilter `json:"measurement_filter,omitempty"`

	// Result options
	Limit int `json:"limit,omitempty"`
}

// SensorVerifyRequest represents a request to verify sensors against discovery filters
type SensorVerifyRequest struct {
	// Filter sensors by the timeseries types they "own"
	TimeseriesFilter *TimeseriesFilter `json:"timeseries_filter,omitempty"`

	// Filter sensors by their measurement values
	MeasurementFilter *MeasurementFilter `json:"measurement_filter,omitempty"`

	// List of sensor names to verify against the filters
	SensorNames []string `json:"sensor_names"`
}

// SensorVerifyResponse represents the response from sensor verification
type SensorVerifyResponse struct {
	// Whether all sensors satisfy the filters
	OK bool `json:"ok"`

	// List of sensor names that satisfy the filters
	Verified []string `json:"verified"`

	// List of sensor names that do not satisfy the filters
	Unverified []string `json:"unverified"`

	// Original request for reference
	Request *SensorVerifyRequest `json:"request"`
}

// TimeseriesFilter filters sensors by the timeseries types they have
type TimeseriesFilter struct {
	// Required timeseries types - sensors must have ALL of these types
	RequiredTypes []string `json:"required_types,omitempty"`

	// Optional timeseries types - sensors may have ANY of these types
	OptionalTypes []string `json:"optional_types,omitempty"`

	// Dataset membership filter
	DatasetIDs []string `json:"dataset_ids,omitempty"`
}

// MeasurementFilter filters sensors by their measurement values using legacy filter syntax
type MeasurementFilter struct {
	// Only consider latest measurements for each timeseries
	LatestOnly bool `json:"latest_only,omitempty"`

	// Time constraints for measurements
	TimeRange *TimeRange `json:"time_range,omitempty"`

	// Filter expression using legacy syntax (e.g., "or(o2.eq.2, and(temp.gteq.20, temp.lteq.30))")
	Expression string `json:"expression,omitempty"`
}

// ValueCondition represents a condition on measurement values for a specific type
type ValueCondition struct {
	TypeName    string         `json:"type_name"`              // The measurement type to check
	Operator    FilterOperator `json:"operator"`               // The comparison operator
	Value       interface{}    `json:"value"`                  // The value to compare against
	JSONPath    []string       `json:"json_path,omitempty"`    // Path for JSON measurements (e.g., ["foo", "bar"])
}

// TimeRange represents a time constraint for measurements
type TimeRange struct {
	StartTime string `json:"start_time,omitempty"`
	EndTime   string `json:"end_time,omitempty"`
}

// BoundingBox represents a geographic bounding box
type BoundingBox struct {
	MinX float64 `json:"min_x"` // Left longitude
	MinY float64 `json:"min_y"` // Bottom latitude
	MaxX float64 `json:"max_x"` // Right longitude
	MaxY float64 `json:"max_y"` // Top latitude
	SRID int     `json:"srid"`  // Spatial Reference System ID (default 4326)
}

// NearPoint represents a point with distance for proximity searches
type NearPoint struct {
	X        float64 `json:"x"`        // Longitude
	Y        float64 `json:"y"`        // Latitude
	Distance float64 `json:"distance"` // Distance in meters
	SRID     int     `json:"srid"`     // Spatial Reference System ID (default 4326)
}

// SQLClause represents a generated SQL clause with parameters
type SQLClause struct {
	SQL        string                 `json:"sql"`
	Parameters map[string]interface{} `json:"parameters"`
	JoinTables []string              `json:"join_tables"`
}

// String returns a human-readable representation of the filter condition
func (fc FilterCondition) String() string {
	if len(fc.JSONPath) > 0 {
		return fmt.Sprintf("%s.%s.%s.%v", fc.Alias, strings.Join(fc.JSONPath, "."), fc.Operator, fc.Value)
	}
	if fc.Alias != "" {
		return fmt.Sprintf("%s.%s.%v", fc.Alias, fc.Operator, fc.Value)
	}
	return fmt.Sprintf("%s.%v", fc.Operator, fc.Value)
}

// String returns a human-readable representation of the logical group
func (lg LogicalGroup) String() string {
	parts := make([]string, 0)

	for _, condition := range lg.Conditions {
		parts = append(parts, condition.String())
	}

	for _, group := range lg.Groups {
		parts = append(parts, fmt.Sprintf("(%s)", group.String()))
	}

	return strings.Join(parts, fmt.Sprintf(" %s ", lg.Operator))
}

// SearchRequest represents a general search request (placeholder for compatibility)
type SearchRequest struct {
	Filter       *LogicalGroup `json:"filter,omitempty"`
	SensorNames  []string      `json:"sensor_names,omitempty"`
	TypeNames    []string      `json:"type_names,omitempty"`
	DatasetIDs   []string      `json:"dataset_ids,omitempty"`
	StartTime    *time.Time    `json:"start_time,omitempty"`
	EndTime      *time.Time    `json:"end_time,omitempty"`
	Latest       bool          `json:"latest,omitempty"`
	BoundingBox  *BoundingBox  `json:"bounding_box,omitempty"`
	NearPoint    *NearPoint    `json:"near_point,omitempty"`
	Limit        int           `json:"limit,omitempty"`
}