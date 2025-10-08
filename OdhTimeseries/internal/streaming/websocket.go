package streaming

import (
	"context"
	"encoding/json"
	"fmt"
	"math"
	"net/http"
	"strings"
	"sync"

	"timeseries-api/internal/filter"
	"timeseries-api/internal/repository"

	"github.com/gorilla/websocket"
	"github.com/sirupsen/logrus"
)

// WebSocketManager manages WebSocket connections and subscriptions
type WebSocketManager struct {
	materialize *MaterializeClient
	repo        *repository.Repository // For sensor discovery
	connections map[*websocket.Conn]*Subscription
	mu          sync.RWMutex
	upgrader    websocket.Upgrader
}

// Subscription represents a client's subscription with filters
type Subscription struct {
	conn              *websocket.Conn
	sensorNames       []string
	typeNames         []string
	timeseriesFilter  *filter.TimeseriesFilter
	measurementFilter *filter.MeasurementFilter
	updatesChan       chan MeasurementUpdate
	ctx               context.Context
	cancel            context.CancelFunc
	mu                sync.Mutex
}

// SpatialFilter defines geospatial filtering parameters
type SpatialFilter struct {
	Type        string    `json:"type"` // "bbox", "radius", "polygon"
	Coordinates []float64 `json:"coordinates"`
	// For bbox: [minLon, minLat, maxLon, maxLat]
	// For radius: [lon, lat, radius_meters]
	// For polygon: [lon1, lat1, lon2, lat2, ...]
}

// WebSocketMessage represents any WebSocket message (GraphQL-style)
type WebSocketMessage struct {
	Type    string      `json:"type"`              // "connection_init", "connection_ack", "data", "error"
	Payload interface{} `json:"payload,omitempty"` // Message-specific payload
}

// ConnectionInitPayload represents the payload for connection_init message
type ConnectionInitPayload struct {
	// Simple mode: specific sensors
	SensorNames []string `json:"sensor_names,omitempty"`
	TypeNames   []string `json:"type_names,omitempty"`

	// Advanced mode: discovery filters
	TimeseriesFilter  *filter.TimeseriesFilter  `json:"timeseries_filter,omitempty"`
	MeasurementFilter *filter.MeasurementFilter `json:"measurement_filter,omitempty"`
	Limit             int                       `json:"limit,omitempty"`
}

// NewWebSocketManager creates a new WebSocket manager
func NewWebSocketManager(materialize *MaterializeClient, repo *repository.Repository) *WebSocketManager {
	return &WebSocketManager{
		materialize: materialize,
		repo:        repo,
		connections: make(map[*websocket.Conn]*Subscription),
		upgrader: websocket.Upgrader{
			ReadBufferSize:  1024,
			WriteBufferSize: 1024,
			CheckOrigin: func(r *http.Request) bool {
				// TODO: Implement proper origin checking in production
				return true
			},
		},
	}
}

// HandleConnection handles a new WebSocket connection (GraphQL-style flow)
// Client must send connection_init message immediately after connecting
// Accepts both simple mode (sensor_names) and advanced mode (filters)
func (wsm *WebSocketManager) HandleConnection(conn *websocket.Conn) {
	wsm.handleConnectionWithMode(conn, "any")
}

// HandleConnectionSimple handles a WebSocket connection for simple mode only
func (wsm *WebSocketManager) HandleConnectionSimple(conn *websocket.Conn) {
	wsm.handleConnectionWithMode(conn, "simple")
}

// HandleConnectionAdvanced handles a WebSocket connection for advanced mode only
func (wsm *WebSocketManager) HandleConnectionAdvanced(conn *websocket.Conn) {
	wsm.handleConnectionWithMode(conn, "advanced")
}

// handleConnectionWithMode handles WebSocket connection with mode enforcement
func (wsm *WebSocketManager) handleConnectionWithMode(conn *websocket.Conn, expectedMode string) {
	logrus.Info("New WebSocket connection established")

	// Wait for connection_init message
	var initMsg WebSocketMessage
	if err := conn.ReadJSON(&initMsg); err != nil {
		logrus.WithError(err).Error("Failed to read connection_init message")
		wsm.sendError(conn, "Expected connection_init message")
		conn.Close()
		return
	}

	if initMsg.Type != "connection_init" {
		wsm.sendError(conn, fmt.Sprintf("Expected connection_init, got: %s", initMsg.Type))
		conn.Close()
		return
	}

	// Parse payload
	payloadBytes, err := json.Marshal(initMsg.Payload)
	if err != nil {
		wsm.sendError(conn, "Invalid connection_init payload")
		conn.Close()
		return
	}

	var payload ConnectionInitPayload
	if err := json.Unmarshal(payloadBytes, &payload); err != nil {
		wsm.sendError(conn, fmt.Sprintf("Invalid connection_init payload: %v", err))
		conn.Close()
		return
	}

	// Validate configuration
	isSimpleMode := len(payload.SensorNames) > 0
	isAdvancedMode := payload.TimeseriesFilter != nil || payload.MeasurementFilter != nil

	// Enforce mode if specified
	if expectedMode == "simple" && !isSimpleMode {
		wsm.sendError(conn, "This endpoint requires sensor_names in the payload (simple mode)")
		conn.Close()
		return
	}

	if expectedMode == "advanced" && !isAdvancedMode {
		wsm.sendError(conn, "This endpoint requires timeseries_filter or measurement_filter in the payload (advanced mode)")
		conn.Close()
		return
	}

	// General validation for "any" mode
	if expectedMode == "any" && !isSimpleMode && !isAdvancedMode {
		wsm.sendError(conn, "Either sensor_names or filters (timeseries_filter/measurement_filter) must be provided")
		conn.Close()
		return
	}

	// Create subscription
	ctx, cancel := context.WithCancel(context.Background())
	sub := &Subscription{
		conn:              conn,
		sensorNames:       payload.SensorNames,
		typeNames:         payload.TypeNames,
		timeseriesFilter:  payload.TimeseriesFilter,
		measurementFilter: payload.MeasurementFilter,
		updatesChan:       make(chan MeasurementUpdate, 100),
		ctx:               ctx,
		cancel:            cancel,
	}

	wsm.mu.Lock()
	wsm.connections[conn] = sub
	wsm.mu.Unlock()

	// Send connection_ack
	ackMsg := WebSocketMessage{
		Type: "connection_ack",
		Payload: map[string]interface{}{
			"mode": map[bool]string{true: "simple", false: "advanced"}[isSimpleMode],
		},
	}
	if err := conn.WriteJSON(ackMsg); err != nil {
		logrus.WithError(err).Error("Failed to send connection_ack")
		wsm.cleanup(conn, sub)
		return
	}

	logrus.WithFields(logrus.Fields{
		"mode":          map[bool]string{true: "simple", false: "advanced"}[isSimpleMode],
		"sensor_count":  len(payload.SensorNames),
		"has_ts_filter": payload.TimeseriesFilter != nil,
	}).Info("Subscription initialized")

	// Start listening for updates from Materialize
	go wsm.listenForUpdates(sub)

	// Start goroutine to send updates to client
	go wsm.sendUpdatesToClient(sub)

	// Wait for connection to close (ignore any incoming messages)
	for {
		if _, _, err := conn.ReadMessage(); err != nil {
			if websocket.IsUnexpectedCloseError(err, websocket.CloseGoingAway, websocket.CloseAbnormalClosure) {
				logrus.WithError(err).Warn("WebSocket connection closed unexpectedly")
			}
			break
		}
		// Ignore any messages from client after connection_init
	}

	// Clean up on disconnect
	wsm.cleanup(conn, sub)
	logrus.Info("WebSocket connection closed")
}

// sendError sends an error message and logs it
func (wsm *WebSocketManager) sendError(conn *websocket.Conn, errorMsg string) {
	logrus.Error(errorMsg)
	msg := WebSocketMessage{
		Type:    "error",
		Payload: map[string]string{"message": errorMsg},
	}
	conn.WriteJSON(msg) // Ignore error, connection will be closed anyway
}

// cleanup removes subscription and closes channels
func (wsm *WebSocketManager) cleanup(conn *websocket.Conn, sub *Subscription) {
	wsm.mu.Lock()
	defer wsm.mu.Unlock()

	if _, exists := wsm.connections[conn]; exists {
		sub.cancel()
		close(sub.updatesChan)
		delete(wsm.connections, conn)
	}
	conn.Close()
}

// listenForUpdates listens for updates from Materialize
func (wsm *WebSocketManager) listenForUpdates(sub *Subscription) {
	err := wsm.materialize.SubscribeWithFilters(
		sub.ctx,
		sub.sensorNames,
		sub.typeNames,
		sub.timeseriesFilter,
		sub.measurementFilter,
		sub.updatesChan,
	)

	if err != nil && err != context.Canceled {
		logrus.WithError(err).Error("Materialize subscription error")
	}
}

// sendUpdatesToClient sends updates to the WebSocket client
func (wsm *WebSocketManager) sendUpdatesToClient(sub *Subscription) {
	for {
		select {
		case <-sub.ctx.Done():
			return
		case update, ok := <-sub.updatesChan:
			if !ok {
				return
			}

			// Apply value filtering from measurementFilter if present
			// This is done here because Materialize has issues with CAST in UNION views
			if sub.measurementFilter != nil && sub.measurementFilter.Expression != "" {
				if !wsm.applyValueFilter(&update, sub.measurementFilter.Expression) {
					continue // Skip this update
				}
			}

			// TODO: Apply geometric filtering from measurementFilter if present
			// Geometric conditions are extracted from measurementFilter and applied here
			// since Materialize doesn't support PostGIS functions

			// Send update to client (GraphQL-style message)
			msg := WebSocketMessage{
				Type:    "data",
				Payload: update,
			}

			sub.mu.Lock()
			err := sub.conn.WriteJSON(msg)
			sub.mu.Unlock()

			if err != nil {
				logrus.WithError(err).Warn("Failed to send update to client")
				sub.cancel()
				return
			}
		}
	}
}

// applySpatialFilter applies geospatial filtering to an update
func (wsm *WebSocketManager) applySpatialFilter(update *MeasurementUpdate, filter *SpatialFilter) bool {
	// Only apply spatial filter to geoposition and geoshape data types
	if update.DataType != "geoposition" && update.DataType != "geoshape" {
		return true
	}

	// Parse the geometry value
	// Value is in WKT format like "POINT(11.123 46.456)"
	coords, err := parseWKT(update.Value)
	if err != nil {
		logrus.WithError(err).Warn("Failed to parse geometry value")
		return false
	}

	switch filter.Type {
	case "bbox":
		if len(filter.Coordinates) != 4 {
			return false
		}
		return isPointInBBox(coords, filter.Coordinates)

	case "radius":
		if len(filter.Coordinates) != 3 {
			return false
		}
		return isPointWithinRadius(coords, filter.Coordinates)

	default:
		logrus.WithField("type", filter.Type).Warn("Unknown spatial filter type")
		return true
	}
}

// parseWKT parses WKT format to extract coordinates
func parseWKT(wkt string) ([]float64, error) {
	// Simple parser for POINT(lon lat) format
	// For production, use a proper WKT parser library
	var lon, lat float64
	n, err := fmt.Sscanf(wkt, "POINT(%f %f)", &lon, &lat)
	if err != nil || n != 2 {
		return nil, fmt.Errorf("failed to parse WKT: %s", wkt)
	}
	return []float64{lon, lat}, nil
}

// isPointInBBox checks if a point is within a bounding box
func isPointInBBox(point []float64, bbox []float64) bool {
	if len(point) < 2 {
		return false
	}
	lon, lat := point[0], point[1]
	minLon, minLat, maxLon, maxLat := bbox[0], bbox[1], bbox[2], bbox[3]

	return lon >= minLon && lon <= maxLon && lat >= minLat && lat <= maxLat
}

// isPointWithinRadius checks if a point is within a radius
func isPointWithinRadius(point []float64, radiusFilter []float64) bool {
	if len(point) < 2 {
		return false
	}
	centerLon, centerLat, radius := radiusFilter[0], radiusFilter[1], radiusFilter[2]
	pointLon, pointLat := point[0], point[1]

	// Haversine formula for distance
	const earthRadius = 6371000 // meters

	dLat := (pointLat - centerLat) * (math.Pi / 180)
	dLon := (pointLon - centerLon) * (math.Pi / 180)

	a := math.Sin(dLat/2)*math.Sin(dLat/2) +
		math.Cos(centerLat*(math.Pi/180))*math.Cos(pointLat*(math.Pi/180))*
			math.Sin(dLon/2)*math.Sin(dLon/2)
	c := 2 * math.Atan2(math.Sqrt(a), math.Sqrt(1-a))

	distance := earthRadius * c

	return distance <= radius
}

// applyValueFilter applies value expression filter to an update
// Expression format: "type.operator.value" (e.g., "temperature.gt.20")
func (wsm *WebSocketManager) applyValueFilter(update *MeasurementUpdate, expression string) bool {
	parts := strings.Split(expression, ".")
	if len(parts) != 3 {
		logrus.WithField("expression", expression).Warn("Invalid filter expression format")
		return true // Don't filter if expression is invalid
	}

	typeName := parts[0]
	operator := parts[1]
	filterValue := parts[2]

	// Check if this update matches the type
	if update.TypeName != typeName {
		return true // This update is not for this type, let it pass
	}

	// Only apply to numeric data types
	if update.DataType != "numeric" {
		return true
	}

	// Parse the numeric value from the update
	var updateVal float64
	if _, scanErr := fmt.Sscanf(update.Value, "%f", &updateVal); scanErr != nil {
		logrus.WithError(scanErr).WithField("value", update.Value).Warn("Failed to parse numeric value")
		return true // Can't parse, let it pass
	}

	// Parse the filter value
	var filterVal float64
	if _, scanErr := fmt.Sscanf(filterValue, "%f", &filterVal); scanErr != nil {
		logrus.WithError(scanErr).WithField("filterValue", filterValue).Warn("Failed to parse filter value")
		return true
	}

	// Apply the operator
	switch operator {
	case "eq":
		return updateVal == filterVal
	case "neq":
		return updateVal != filterVal
	case "gt":
		return updateVal > filterVal
	case "gte", "gteq":
		return updateVal >= filterVal
	case "lt":
		return updateVal < filterVal
	case "lte", "lteq":
		return updateVal <= filterVal
	default:
		logrus.WithField("operator", operator).Warn("Unknown operator in filter expression")
		return true
	}
}
