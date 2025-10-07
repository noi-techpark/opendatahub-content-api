package streaming

import (
	"context"
	"fmt"
	"math"
	"net/http"
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
	conn          *websocket.Conn
	sensorNames   []string
	typeNames     []string
	spatialFilter *SpatialFilter
	updatesChan   chan MeasurementUpdate
	ctx           context.Context
	cancel        context.CancelFunc
	mu            sync.Mutex
}

// SpatialFilter defines geospatial filtering parameters
type SpatialFilter struct {
	Type        string    `json:"type"` // "bbox", "radius", "polygon"
	Coordinates []float64 `json:"coordinates"`
	// For bbox: [minLon, minLat, maxLon, maxLat]
	// For radius: [lon, lat, radius_meters]
	// For polygon: [lon1, lat1, lon2, lat2, ...]
}

// SubscriptionRequest represents a WebSocket subscription request
type SubscriptionRequest struct {
	Action      string   `json:"action"` // "subscribe" or "unsubscribe"
	SensorNames []string `json:"sensor_names,omitempty"`
	TypeNames   []string `json:"type_names,omitempty"`

	// Advanced discovery filters (alternative to sensor_names)
	// When using discovery mode, spatial_filter is also available
	TimeseriesFilter  *filter.TimeseriesFilter  `json:"timeseries_filter,omitempty"`
	MeasurementFilter *filter.MeasurementFilter `json:"measurement_filter,omitempty"`
	SpatialFilter     *SpatialFilter            `json:"spatial_filter,omitempty"` // Only for discovery mode
	Limit             int                       `json:"limit,omitempty"`
}

// SubscriptionResponse represents a WebSocket response
type SubscriptionResponse struct {
	Type    string      `json:"type"` // "ack", "error", "data"
	Message string      `json:"message,omitempty"`
	Data    interface{} `json:"data,omitempty"`
	Error   string      `json:"error,omitempty"`
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

// HandleConnection handles a new WebSocket connection
func (wsm *WebSocketManager) HandleConnection(conn *websocket.Conn) {
	logrus.Info("New WebSocket connection established")

	// Send welcome message
	welcome := SubscriptionResponse{
		Type:    "ack",
		Message: "Connected to timeseries streaming API",
	}
	if err := conn.WriteJSON(welcome); err != nil {
		logrus.WithError(err).Error("Failed to send welcome message")
		conn.Close()
		return
	}

	// Handle incoming messages
	for {
		var req SubscriptionRequest
		err := conn.ReadJSON(&req)
		if err != nil {
			if websocket.IsUnexpectedCloseError(err, websocket.CloseGoingAway, websocket.CloseAbnormalClosure) {
				logrus.WithError(err).Warn("WebSocket connection closed unexpectedly")
			}
			break
		}

		switch req.Action {
		case "subscribe":
			wsm.handleSubscribe(conn, &req)
		case "unsubscribe":
			wsm.handleUnsubscribe(conn)
		default:
			resp := SubscriptionResponse{
				Type:  "error",
				Error: fmt.Sprintf("Unknown action: %s", req.Action),
			}
			conn.WriteJSON(resp)
		}
	}

	// Clean up on disconnect
	wsm.handleUnsubscribe(conn)
	conn.Close()
	logrus.Info("WebSocket connection closed")
}

// handleSubscribe handles a subscription request
func (wsm *WebSocketManager) handleSubscribe(conn *websocket.Conn, req *SubscriptionRequest) {
	var sensorNames []string

	// Determine mode and validate
	isSimpleMode := len(req.SensorNames) > 0
	isDiscoveryMode := req.TimeseriesFilter != nil || req.MeasurementFilter != nil

	// Validate: spatial_filter only allowed in discovery mode
	if req.SpatialFilter != nil && isSimpleMode && !isDiscoveryMode {
		resp := SubscriptionResponse{
			Type:  "error",
			Error: "spatial_filter is only supported in discovery mode (use timeseries_filter or measurement_filter)",
		}
		conn.WriteJSON(resp)
		return
	}

	// Determine sensor names: either from direct list or via discovery
	if isSimpleMode && !isDiscoveryMode {
		// Simple mode: direct sensor name list (mirrors /latest endpoint)
		sensorNames = req.SensorNames
	} else if isDiscoveryMode {
		// Discovery-based subscription
		if wsm.repo == nil {
			resp := SubscriptionResponse{
				Type:  "error",
				Error: "Discovery-based subscriptions not available (repository not initialized)",
			}
			conn.WriteJSON(resp)
			return
		}

		// Perform sensor discovery
		discoveryReq := &filter.SensorDiscoveryRequest{
			TimeseriesFilter:  req.TimeseriesFilter,
			MeasurementFilter: req.MeasurementFilter,
			Limit:             req.Limit,
		}

		sensors, err := wsm.repo.DiscoverSensorsByConditions(discoveryReq)
		if err != nil {
			logrus.WithError(err).Error("Failed to discover sensors for subscription")
			resp := SubscriptionResponse{
				Type:  "error",
				Error: fmt.Sprintf("Failed to discover sensors: %v", err),
			}
			conn.WriteJSON(resp)
			return
		}

		// Extract sensor names
		sensorNames = make([]string, len(sensors))
		for i, sensor := range sensors {
			sensorNames[i] = sensor.Name
		}

		logrus.WithFields(logrus.Fields{
			"discovered_count": len(sensorNames),
			"limit":            req.Limit,
		}).Info("Discovered sensors for subscription")
	} else {
		resp := SubscriptionResponse{
			Type:  "error",
			Error: "Either sensor_names or discovery filters (timeseries_filter/measurement_filter) must be provided",
		}
		conn.WriteJSON(resp)
		return
	}

	if len(sensorNames) == 0 {
		resp := SubscriptionResponse{
			Type:  "error",
			Error: "No sensors found matching the criteria",
		}
		conn.WriteJSON(resp)
		return
	}

	// Cancel existing subscription if any
	wsm.handleUnsubscribe(conn)

	// Create new subscription
	ctx, cancel := context.WithCancel(context.Background())
	sub := &Subscription{
		conn:          conn,
		sensorNames:   sensorNames,
		typeNames:     req.TypeNames,
		spatialFilter: req.SpatialFilter,
		updatesChan:   make(chan MeasurementUpdate, 100),
		ctx:           ctx,
		cancel:        cancel,
	}

	wsm.mu.Lock()
	wsm.connections[conn] = sub
	wsm.mu.Unlock()

	// Send acknowledgment
	resp := SubscriptionResponse{
		Type:    "ack",
		Message: "Subscription created successfully",
		Data: map[string]interface{}{
			"sensor_count": len(sensorNames),
			"sensor_names": sensorNames,
			"type_names":   req.TypeNames,
		},
	}
	if err := conn.WriteJSON(resp); err != nil {
		logrus.WithError(err).Error("Failed to send subscription ack")
		wsm.handleUnsubscribe(conn)
		return
	}

	// Start listening for updates from Materialize
	go wsm.listenForUpdates(sub)

	// Start sending updates to client
	go wsm.sendUpdatesToClient(sub)
}

// handleUnsubscribe handles an unsubscribe request
func (wsm *WebSocketManager) handleUnsubscribe(conn *websocket.Conn) {
	wsm.mu.Lock()
	sub, exists := wsm.connections[conn]
	if exists {
		delete(wsm.connections, conn)
	}
	wsm.mu.Unlock()

	if exists {
		sub.cancel()
		close(sub.updatesChan)

		resp := SubscriptionResponse{
			Type:    "ack",
			Message: "Subscription cancelled",
		}
		conn.WriteJSON(resp)
	}
}

// listenForUpdates listens for updates from Materialize
func (wsm *WebSocketManager) listenForUpdates(sub *Subscription) {
	err := wsm.materialize.SubscribeToLatestMeasurements(
		sub.ctx,
		sub.sensorNames,
		sub.typeNames,
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

			// Apply spatial filtering if configured
			if sub.spatialFilter != nil {
				if !wsm.applySpatialFilter(&update, sub.spatialFilter) {
					continue
				}
			}

			// Send update to client
			resp := SubscriptionResponse{
				Type: "data",
				Data: update,
			}

			sub.mu.Lock()
			err := sub.conn.WriteJSON(resp)
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
