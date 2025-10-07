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

// SubscribeToMeasurements establishes a WebSocket connection for streaming measurement updates
// @Summary Subscribe to measurement updates (WebSocket)
// @Description Establish a WebSocket connection to receive real-time measurement updates.
// @Description
// @Description After connecting, send a JSON subscription request using one of two methods:
// @Description
// @Description **Method 1: Simple subscription (mirrors /latest endpoint)**
// @Description ```json
// @Description {
// @Description   "action": "subscribe",
// @Description   "sensor_names": ["sensor1", "sensor2"],
// @Description   "type_names": ["temperature", "humidity"]
// @Description }
// @Description ```
// @Description Note: Spatial filtering is NOT available in simple mode.
// @Description
// @Description **Method 2: Discovery-based subscription (advanced with spatial filtering)**
// @Description ```json
// @Description {
// @Description   "action": "subscribe",
// @Description   "timeseries_filter": {
// @Description     "required_types": ["temperature", "humidity"],
// @Description     "optional_types": ["pressure"],
// @Description     "dataset_ids": ["dataset1"]
// @Description   },
// @Description   "measurement_filter": {
// @Description     "latest_only": true,
// @Description     "expression": "temperature.gteq.20"
// @Description   },
// @Description   "spatial_filter": {
// @Description     "type": "bbox",
// @Description     "coordinates": [10.5, 46.2, 12.5, 47.2]
// @Description   },
// @Description   "limit": 100
// @Description }
// @Description ```
// @Description
// @Description Spatial filter types:
// @Description - "bbox": Bounding box filter with [minLon, minLat, maxLon, maxLat]
// @Description - "radius": Radius filter with [centerLon, centerLat, radiusMeters]
// @Description
// @Description Response Format:
// @Description ```json
// @Description {
// @Description   "type": "data",
// @Description   "data": {
// @Description     "timeseries_id": "uuid",
// @Description     "sensor_name": "sensor1",
// @Description     "type_name": "temperature",
// @Description     "timestamp": "2024-01-01T00:00:00Z",
// @Description     "value": "23.5"
// @Description   }
// @Description }
// @Description ```
// @Tags measurements, streaming
// @Success 101 {string} string "Switching Protocols"
// @Failure 400 {object} map[string]interface{} "Bad request"
// @Router /measurements/subscribe [get]
func (h *StreamingHandler) SubscribeToMeasurements(c *gin.Context) {
	// Upgrade HTTP connection to WebSocket
	conn, err := h.upgrader.Upgrade(c.Writer, c.Request, nil)
	if err != nil {
		logrus.WithError(err).Error("Failed to upgrade WebSocket connection")
		c.JSON(http.StatusBadRequest, gin.H{"error": "Failed to upgrade to WebSocket"})
		return
	}

	// Handle the WebSocket connection
	h.wsManager.HandleConnection(conn)
}
