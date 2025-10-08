package handlers

import (
	"net/http"

	"timeseries-api/internal/streaming"

	"github.com/gin-gonic/gin"
	"github.com/gorilla/websocket"
	"github.com/sirupsen/logrus"
)

type StreamingHandler struct {
	wsManager *streaming.WebSocketManager
	upgrader  websocket.Upgrader
}

func NewStreamingHandler(wsManager *streaming.WebSocketManager) *StreamingHandler {
	return &StreamingHandler{
		wsManager: wsManager,
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

// SubscribeToMeasurements establishes a WebSocket connection for simple streaming (specific sensors)
// @Summary Simple WebSocket subscription (GraphQL-style)
// @Description Establishes a WebSocket connection for real-time measurement streaming from specific sensors.
// @Description
// @Description **Protocol Flow (GraphQL-style):**
// @Description 1. Client connects to WebSocket (GET /api/v1/measurements/subscribe)
// @Description 2. Client immediately sends `connection_init` message with sensor_names
// @Description 3. Server validates configuration and responds with `connection_ack` or `error`
// @Description 4. Server streams measurement updates as `data` messages
// @Description 5. Client closes connection to unsubscribe
// @Description
// @Description **Client Message (connection_init):**
// @Description ```json
// @Description {
// @Description   "type": "connection_init",
// @Description   "payload": {
// @Description     "sensor_names": ["sensor1", "sensor2"],
// @Description     "type_names": ["temperature", "humidity"]
// @Description   }
// @Description }
// @Description ```
// @Description
// @Description **Server Response (connection_ack):**
// @Description ```json
// @Description {
// @Description   "type": "connection_ack",
// @Description   "payload": {
// @Description     "mode": "simple"
// @Description   }
// @Description }
// @Description ```
// @Description
// @Description **Server Data Messages:**
// @Description ```json
// @Description {
// @Description   "type": "data",
// @Description   "payload": {
// @Description     "timeseries_id": "uuid",
// @Description     "sensor_name": "sensor1",
// @Description     "type_name": "temperature",
// @Description     "timestamp": "2024-01-01T00:00:00Z",
// @Description     "value": "23.5"
// @Description   }
// @Description }
// @Description ```
// @Description
// @Description **Server Error Messages:**
// @Description ```json
// @Description {
// @Description   "type": "error",
// @Description   "payload": {
// @Description     "message": "Invalid configuration"
// @Description   }
// @Description }
// @Description ```
// @Tags measurements, streaming
// @Success 101 {string} string "Switching Protocols - WebSocket connection established"
// @Router /measurements/subscribe [get]
func (h *StreamingHandler) SubscribeToMeasurements(c *gin.Context) {
	// Upgrade HTTP connection to WebSocket
	conn, err := h.upgrader.Upgrade(c.Writer, c.Request, nil)
	if err != nil {
		logrus.WithError(err).Error("Failed to upgrade WebSocket connection")
		return // Response already sent by upgrader
	}

	// Handle the WebSocket connection (simple mode only)
	h.wsManager.HandleConnectionSimple(conn)
}

// SubscribeToMeasurementsAdvanced establishes a WebSocket connection for advanced streaming (discovery filters)
// @Summary Advanced WebSocket subscription with discovery filters (GraphQL-style)
// @Description Establishes a WebSocket connection for real-time measurement streaming using discovery filters.
// @Description
// @Description **Protocol Flow (GraphQL-style):**
// @Description 1. Client connects to WebSocket (GET /api/v1/measurements/subscribe/advanced)
// @Description 2. Client immediately sends `connection_init` message with filters
// @Description 3. Server validates configuration and responds with `connection_ack` or `error`
// @Description 4. Server streams measurement updates as `data` messages
// @Description 5. Client closes connection to unsubscribe
// @Description
// @Description **Client Message (connection_init):**
// @Description ```json
// @Description {
// @Description   "type": "connection_init",
// @Description   "payload": {
// @Description     "timeseries_filter": {
// @Description       "required_types": ["temperature"],
// @Description       "optional_types": ["humidity"],
// @Description       "dataset_ids": ["weather_stations"]
// @Description     },
// @Description     "measurement_filter": {
// @Description       "expression": "temperature.gt.20",
// @Description       "latest_only": true
// @Description     },
// @Description     "limit": 100
// @Description   }
// @Description }
// @Description ```
// @Description
// @Description **Server Response (connection_ack):**
// @Description ```json
// @Description {
// @Description   "type": "connection_ack",
// @Description   "payload": {
// @Description     "mode": "advanced"
// @Description   }
// @Description }
// @Description ```
// @Description
// @Description **Server Data Messages:**
// @Description ```json
// @Description {
// @Description   "type": "data",
// @Description   "payload": {
// @Description     "timeseries_id": "uuid",
// @Description     "sensor_name": "sensor1",
// @Description     "type_name": "temperature",
// @Description     "timestamp": "2024-01-01T00:00:00Z",
// @Description     "value": "23.5"
// @Description   }
// @Description }
// @Description ```
// @Description
// @Description **Server Error Messages:**
// @Description ```json
// @Description {
// @Description   "type": "error",
// @Description   "payload": {
// @Description     "message": "Invalid configuration"
// @Description   }
// @Description }
// @Description ```
// @Description
// @Description **Filter Expression Syntax:**
// @Description - Simple: `temperature.gt.20` (temperature > 20)
// @Description - AND: `and(temperature.gt.20, humidity.lt.80)`
// @Description - OR: `or(temperature.gt.30, pm25.gt.90)`
// @Description - Operators: eq, neq, gt, gte/gteq, lt, lte/lteq, re (regex)
// @Tags measurements, streaming
// @Success 101 {string} string "Switching Protocols - WebSocket connection established"
// @Router /measurements/subscribe/advanced [get]
func (h *StreamingHandler) SubscribeToMeasurementsAdvanced(c *gin.Context) {
	// Upgrade HTTP connection to WebSocket
	conn, err := h.upgrader.Upgrade(c.Writer, c.Request, nil)
	if err != nil {
		logrus.WithError(err).Error("Failed to upgrade WebSocket connection")
		return // Response already sent by upgrader
	}

	// Handle the WebSocket connection (advanced mode only)
	h.wsManager.HandleConnectionAdvanced(conn)
}
