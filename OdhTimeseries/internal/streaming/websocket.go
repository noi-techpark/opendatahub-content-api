package streaming

import (
	"context"
	"encoding/json"
	"fmt"
	"math"
	"net/http"
	"sync"

	"timeseries-api/internal/filter"
	"timeseries-api/internal/repository"

	"github.com/gorilla/websocket"
	"github.com/paulmach/orb"
	"github.com/paulmach/orb/encoding/wkt"
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
	conn                *websocket.Conn
	sensorNames         []string
	typeNames           []string
	timeseriesFilter    *filter.TimeseriesFilter
	measurementFilter   *filter.MeasurementFilter
	skipInitialSnapshot bool                     // 🎯 SNAPSHOT CONTROL: Whether to skip existing records
	spatialFilters      []SpatialFilterCondition // Extracted from measurementFilter.Expression
	updatesChan         chan MeasurementUpdate
	ctx                 context.Context
	cancel              context.CancelFunc
	mu                  sync.Mutex
}

// SpatialFilterCondition is imported from streaming.MaterializeClient
// type SpatialFilterCondition struct {
// 	TypeName    string
// 	Operator    filter.FilterOperator
// 	Coordinates []float64
// }

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

	// 🎯 SNAPSHOT CONTROL: Set to true to skip existing records and only receive new updates
	// When false (default), you'll receive all existing records plus new updates
	// When true, you'll only receive updates that occur AFTER subscription
	SkipInitialSnapshot bool `json:"skip_initial_snapshot,omitempty"`
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
		conn:                conn,
		sensorNames:         payload.SensorNames,
		typeNames:           payload.TypeNames,
		timeseriesFilter:    payload.TimeseriesFilter,
		measurementFilter:   payload.MeasurementFilter,
		skipInitialSnapshot: payload.SkipInitialSnapshot, // 🎯 SNAPSHOT CONTROL
		updatesChan:         make(chan MeasurementUpdate, 100),
		ctx:                 ctx,
		cancel:              cancel,
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
// Captures spatial filters returned by SubscribeWithFilters for application-layer filtering
func (wsm *WebSocketManager) listenForUpdates(sub *Subscription) {
	spatialFilters, err := wsm.materialize.SubscribeWithFilters(
		sub.ctx,
		sub,
		sub.sensorNames,
		sub.typeNames,
		sub.timeseriesFilter,
		sub.measurementFilter,
		sub.updatesChan,
	)

	if err != nil && err != context.Canceled {
		logrus.WithError(err).Error("Materialize subscription error")
		return
	}

	if len(spatialFilters) > 0 {
		logrus.WithField("count", len(spatialFilters)).Info("Extracted spatial filters for application-layer filtering")
	}
}

// sendUpdatesToClient sends updates to the WebSocket client
// Applies spatial filters at application layer (since Materialize doesn't support PostGIS)
func (wsm *WebSocketManager) sendUpdatesToClient(sub *Subscription) {
	logrus.Debug("sendUpdatesToClient goroutine started")
	for {
		select {
		case <-sub.ctx.Done():
			logrus.Debug("sendUpdatesToClient context done, exiting")
			return
		case update, ok := <-sub.updatesChan:
			if !ok {
				logrus.Debug("sendUpdatesToClient channel closed, exiting")
				return
			}

			logrus.WithFields(logrus.Fields{
				"sensor_name": update.SensorName,
				"type_name":   update.TypeName,
				"value":       update.Value,
			}).Debug("Received update from channel in sendUpdatesToClient")

			// Apply spatial filtering at application layer
			// All non-spatial filters are already applied at DB level in the TAIL query
			if len(sub.spatialFilters) > 0 {
				if !wsm.applySpatialFilters(&update, sub.spatialFilters) {
					logrus.Debug("Update filtered out by spatial filter")
					continue // Skip this update
				}
			}

			// Send update to client (GraphQL-style message)
			msg := WebSocketMessage{
				Type:    "data",
				Payload: update,
			}

			logrus.Debug("Attempting to send update to WebSocket client")
			sub.mu.Lock()
			err := sub.conn.WriteJSON(msg)
			sub.mu.Unlock()

			if err != nil {
				logrus.WithError(err).Warn("Failed to send update to client")
				sub.cancel()
				return
			}
			logrus.Debug("Successfully sent update to WebSocket client")
		}
	}
}

// filterCoordsToBound converts a slice of filter coordinates [minLon, minLat, maxLon, maxLat]
// into an orb.Bound object.
func filterCoordsToBound(coords []float64) orb.Bound {
	return orb.Bound{
		Min: orb.Point{coords[0], coords[1]},
		Max: orb.Point{coords[2], coords[3]},
	}
}

// applySpatialFilters applies geospatial filtering to an update
// Checks if the update matches ANY of the spatial filter conditions
func (wsm *WebSocketManager) applySpatialFilters(update *MeasurementUpdate, filters []SpatialFilterCondition) bool {
	// Only apply spatial filter to geoposition and geoshape data types
	if update.DataType != "geoposition" && update.DataType != "geoshape" {
		return true
	}

	// Value is guaranteed to be a WKT string (from previous step's WKB conversion)
	wktString, ok := update.Value.(string)
	if !ok {
		logrus.WithField("type", fmt.Sprintf("%T", update.Value)).Warn("Expected WKT string for spatial filtering but received unexpected type.")
		return true // Cannot filter, let it through
	}

	// Unmarshal the WKT string into an orb.Geometry object
	g, err := wkt.Unmarshal(wktString)
	if err != nil {
		logrus.WithError(err).WithField("wkt", wktString).Warn("Failed to unmarshal WKT string for spatial filtering")
		return false // Failed to parse, filter out
	}

	// Get the bounding box (envelope) of the received geometry
	geomBound := g.Bound() // orb.Geometry.Bound() returns an orb.Bound

	// Check if this update matches any of the filter conditions
	for _, filter := range filters {
		// Only apply filter if it's for this type
		if filter.TypeName != update.TypeName {
			continue
		}

		// Apply the appropriate spatial check
		var matches bool
		switch filter.Operator {
		case "bbi": // OpBoundingBoxIntersect
			if len(filter.Coordinates) != 4 {
				logrus.Warn("Invalid bbox coordinates for 'bbi', expected 4")
				continue
			}
			filterBound := filterCoordsToBound(filter.Coordinates)
			// Use the built-in orb.Bound.Intersects method
			matches = geomBound.Intersects(filterBound)

		case "bbc": // OpBoundingBoxContain
			if len(filter.Coordinates) != 4 {
				logrus.Warn("Invalid bbox coordinates for 'bbc', expected 4")
				continue
			}
			filterBound := filterCoordsToBound(filter.Coordinates)
			// Check if the filter bound Contains the geometry's bound (Requires BBox logic)
			// Note: orb.Bound.Contains only checks points. We must use manual BBox check for 'bbc'
			matches = isBBoxContained(geomBound, filterBound)

		case "dlt": // OpDistanceLessThan
			if len(filter.Coordinates) != 3 {
				logrus.Warn("Invalid distance coordinates for 'dlt', expected 3 (lon, lat, radius)")
				continue
			}
			// Use the geometry's center point for distance check
			centerPoint := geomBound.Center()
			matches = isPointWithinRadius(centerPoint, filter.Coordinates)

		default:
			logrus.WithField("operator", filter.Operator).Warn("Unknown spatial filter operator")
			continue
		}

		if matches {
			return true // Found a matching filter
		}
	}

	// Final decision on filtering (original logic retained)
	hasFilterForType := false
	for _, filter := range filters {
		if filter.TypeName == update.TypeName {
			hasFilterForType = true
			break
		}
	}

	return !hasFilterForType
}

// isBBoxContained checks if geomBound is entirely contained within filterBound
// We redefine this to use orb.Bound types instead of slices.
func isBBoxContained(geomBound orb.Bound, filterBound orb.Bound) bool {
	// Check if the min point of the geometry is contained by the filter bound
	if !filterBound.Contains(geomBound.Min) {
		return false
	}
	// Check if the max point of the geometry is contained by the filter bound
	if !filterBound.Contains(geomBound.Max) {
		return false
	}
	return true
}

// isPointWithinRadius checks if a point is within a radius
// We redefine this to use orb.Point for the input point
func isPointWithinRadius(point orb.Point, radiusFilter []float64) bool {
	// radiusFilter: [centerLon, centerLat, radius]
	if len(radiusFilter) < 3 {
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
