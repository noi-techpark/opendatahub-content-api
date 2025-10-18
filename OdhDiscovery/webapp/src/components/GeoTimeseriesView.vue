<template>
  <div class="geo-timeseries-view">
    <div class="view-toggle">
      <button
        @click="viewMode = 'all'"
        class="btn btn-sm"
        :class="{ 'btn-primary': viewMode === 'all', 'btn-outline': viewMode !== 'all' }"
      >
        All Timeseries
      </button>
      <button
        @click="viewMode = 'instant'"
        class="btn btn-sm"
        :class="{ 'btn-primary': viewMode === 'instant', 'btn-outline': viewMode !== 'instant' }"
      >
        Instant View
      </button>
    </div>

    <!-- All Timeseries View -->
    <div v-if="viewMode === 'all'" class="map-view">
      <div ref="mapContainerAll" class="map-container"></div>
    </div>

    <!-- Instant View with Slider -->
    <div v-else class="instant-view">
      <div class="slider-controls">
        <div class="slider-header">
          <span class="slider-label">Time:</span>
          <span class="slider-value">{{ formatTimestamp(currentTimestamp) }}</span>
        </div>
        <input
          v-model="sliderIndex"
          type="range"
          min="0"
          :max="timestamps.length - 1"
          step="1"
          class="time-slider"
        />
        <div class="slider-info">
          <span>{{ sliderIndex + 1 }} / {{ timestamps.length }}</span>
          <div class="slider-controls-buttons">
            <button @click="prevTimestamp" :disabled="sliderIndex === 0" class="btn btn-sm btn-outline">
              ← Prev
            </button>
            <button @click="nextTimestamp" :disabled="sliderIndex === timestamps.length - 1" class="btn btn-sm btn-outline">
              Next →
            </button>
          </div>
        </div>
      </div>

      <div ref="mapContainerInstant" class="map-container"></div>

      <div class="instant-measurements">
        <h4>Measurements at this timestamp:</h4>
        <div class="measurements-grid">
          <div v-for="(m, idx) in measurementsAtCurrentTime" :key="idx" class="measurement-card">
            <div class="measurement-sensor">{{ m.sensor_name }}</div>
            <div class="measurement-value">{{ formatGeoValue(m.value) }}</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch, nextTick } from 'vue'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import Wkt from 'wicket'

const props = defineProps({
  measurements: {
    type: Array,
    required: true
  }
})

const viewMode = ref('all')
const sliderIndex = ref(0)
const mapContainerAll = ref(null)
const mapContainerInstant = ref(null)
let mapAll = null
let mapInstant = null
let allMarkersLayer = null
let instantMarkersLayer = null
let sensorLayers = {} // Track individual sensor layers for incremental updates

// Configuration
const ENABLE_POLYGON_PROJECTIONS = true  // Set to false to disable projection fill rendering for performance
const POINT_TRAJECTORY_OPACITY = 0.3
const POLYGON_PROJECTION_FILL_OPACITY = 0.15
const POLYGON_PROJECTION_BORDER_OPACITY = 0.4
const HISTORICAL_SHAPE_OPACITY = 0.4

const sortedMeasurements = computed(() => {
  return [...props.measurements].sort((a, b) => {
    return new Date(a.timestamp) - new Date(b.timestamp)
  })
})

const allTimestamps = computed(() => {
  // Collect all timestamps from all measurements (including duplicates)
  const timestamps = []
  sortedMeasurements.value.forEach(m => {
    timestamps.push(m.timestamp)
  })
  // Get unique timestamps sorted
  const uniqueTimestamps = [...new Set(timestamps)]
  return uniqueTimestamps.sort()
})

const timestamps = computed(() => {
  return allTimestamps.value
})

const currentTimestamp = computed(() => {
  return timestamps.value[sliderIndex.value] || null
})

const measurementsUpToCurrentTime = computed(() => {
  if (!currentTimestamp.value) return []

  // Get all measurements up to and including current timestamp
  const targetTime = new Date(currentTimestamp.value).getTime()
  return sortedMeasurements.value.filter(m => {
    return new Date(m.timestamp).getTime() <= targetTime
  })
})

const measurementsAtCurrentTime = computed(() => {
  if (!currentTimestamp.value) return []
  return sortedMeasurements.value.filter(m => m.timestamp === currentTimestamp.value)
})

// Get the latest measurement for each sensor up to current time
const latestMeasurementsPerSensor = computed(() => {
  if (!currentTimestamp.value) return []

  const targetTime = new Date(currentTimestamp.value).getTime()
  const sensorMap = {}

  // Iterate through sorted measurements up to target time
  sortedMeasurements.value.forEach(m => {
    const mTime = new Date(m.timestamp).getTime()
    if (mTime <= targetTime) {
      // Keep updating with latest for each sensor
      sensorMap[m.sensor_name] = m
    }
  })

  return Object.values(sensorMap)
})

const uniqueSensors = computed(() => {
  return new Set(props.measurements.map(m => m.sensor_name)).size
})

const uniqueSensorsAtCurrentTime = computed(() => {
  return new Set(measurementsAtCurrentTime.value.map(m => m.sensor_name)).size
})

// Generate a consistent color based on sensor name hash
function getSensorColor(sensorName) {
  // Simple hash function
  let hash = 0
  for (let i = 0; i < sensorName.length; i++) {
    hash = ((hash << 5) - hash) + sensorName.charCodeAt(i)
    hash = hash & hash // Convert to 32bit integer
  }

  // Use hash to pick from a larger color palette
  const colors = [
    '#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#a855f7', '#ec4899',
    '#14b8a6', '#f97316', '#06b6d4', '#8b5cf6', '#84cc16', '#f43f5e',
    '#0ea5e9', '#6366f1', '#22c55e', '#eab308', '#d946ef', '#64748b'
  ]

  const index = Math.abs(hash) % colors.length
  return colors[index]
}

onMounted(() => {
  initializeAllMap()
})

watch(viewMode, async (newMode) => {
  await nextTick()
  if (newMode === 'all') {
    initializeAllMap()
  } else {
    // Initialize slider to last timestamp when entering instant view
    if (timestamps.value.length > 0) {
      sliderIndex.value = timestamps.value.length - 1
    }
    initializeInstantMap()
  }
})

watch(sliderIndex, () => {
  renderInstantMapAtTime()
})

function initializeAllMap() {
  if (!mapContainerAll.value) return

  // Clean up existing map
  if (mapAll) {
    mapAll.remove()
  }

  // Create map
  mapAll = L.map(mapContainerAll.value).setView([46.5, 11.35], 8)

  // Add OpenStreetMap tiles
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© OpenStreetMap contributors'
  }).addTo(mapAll)

  // Add all measurements as markers
  allMarkersLayer = L.layerGroup().addTo(mapAll)

  const bounds = []

  // Group measurements by sensor to create paths
  const sensorPaths = {}

  sortedMeasurements.value.forEach(m => {
    const geometry = extractGeometry(m.value)
    const coords = extractCoordinates(m.value)

    if (coords) {
      const [lat, lng] = coords
      bounds.push([lat, lng])

      const color = getSensorColor(m.sensor_name)

      // Group coordinates by sensor for path drawing
      if (!sensorPaths[m.sensor_name]) {
        sensorPaths[m.sensor_name] = []
      }
      sensorPaths[m.sensor_name].push({
        coords: [lat, lng],
        timestamp: m.timestamp,
        value: m.value,
        geometry: geometry
      })

      // Render full geometry (polygon, point, etc.)
      const popup = `
        <strong>${m.sensor_name}</strong><br>
        ${formatTimestamp(m.timestamp)}<br>
        ${formatGeoValue(m.value)}
      `
      renderGeometry(geometry, allMarkersLayer, color, popup)
    }
  })

  // Draw polylines connecting points for each sensor
  Object.entries(sensorPaths).forEach(([sensorName, points]) => {
    if (points.length > 1) {
      const latLngs = points.map(p => p.coords)
      const color = getSensorColor(sensorName)
      const polyline = L.polyline(latLngs, {
        color: color,
        weight: 2,
        opacity: POINT_TRAJECTORY_OPACITY,
        smoothFactor: 1
      }).addTo(allMarkersLayer)

      polyline.bindPopup(`
        <strong>${sensorName}</strong><br>
        Path with ${points.length} points<br>
        From: ${formatTimestamp(points[0].timestamp)}<br>
        To: ${formatTimestamp(points[points.length - 1].timestamp)}
      `)
    }
  })

  // Fit bounds if we have markers
  if (bounds.length > 0) {
    mapAll.fitBounds(bounds, { padding: [50, 50] })
  }
}

function initializeInstantMap() {
  if (!mapContainerInstant.value) return

  // Clean up existing map
  if (mapInstant) {
    mapInstant.remove()
    sensorLayers = {}
  }

  // Create map
  mapInstant = L.map(mapContainerInstant.value).setView([46.5, 11.35], 8)
  mapInstant._boundsSet = false

  // Add OpenStreetMap tiles
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© OpenStreetMap contributors'
  }).addTo(mapInstant)

  instantMarkersLayer = L.layerGroup().addTo(mapInstant)

  // Initialize sensor layers
  sensorLayers = {}

  renderInstantMapAtTime()
}

function renderInstantMapAtTime() {
  if (!mapInstant || !instantMarkersLayer) return

  const targetTime = new Date(currentTimestamp.value).getTime()
  const bounds = []

  // Group all measurements up to current time by sensor
  const sensorData = {}

  measurementsUpToCurrentTime.value.forEach(m => {
    const mTime = new Date(m.timestamp).getTime()
    if (!sensorData[m.sensor_name]) {
      sensorData[m.sensor_name] = {
        measurements: [],
        latest: null
      }
    }

    sensorData[m.sensor_name].measurements.push(m)

    // Track the latest for this sensor
    if (!sensorData[m.sensor_name].latest || mTime > new Date(sensorData[m.sensor_name].latest.timestamp).getTime()) {
      sensorData[m.sensor_name].latest = m
    }
  })

  // For each sensor, update or create its layers
  Object.entries(sensorData).forEach(([sensorName, data]) => {
    const color = getSensorColor(sensorName)
    const latest = data.latest
    const measurements = data.measurements

    // Initialize sensor layer if it doesn't exist
    if (!sensorLayers[sensorName]) {
      sensorLayers[sensorName] = {
        historicalGeometries: [],
        polyline: null,
        currentMarker: null,
        projectionLines: []
      }
    }

    const sensorLayer = sensorLayers[sensorName]

    // Determine if this is a point-based or shape-based timeseries
    const latestGeometry = latest ? extractGeometry(latest.value) : null
    const isPointBased = latestGeometry && latestGeometry.type === 'Point'

    // Clear old historical geometries
    sensorLayer.historicalGeometries.forEach(g => g.remove())
    sensorLayer.historicalGeometries = []

    // Clear old projection lines
    sensorLayer.projectionLines.forEach(l => l.remove())
    sensorLayer.projectionLines = []

    if (isPointBased && measurements.length > 1) {
      // For points: Draw connecting line (path)
      const pathCoords = []
      measurements.forEach(m => {
        const coords = extractCoordinates(m.value)
        if (coords) {
          pathCoords.push(coords)
          bounds.push(coords)
        }
      })

      if (pathCoords.length > 1) {
        // Update or create polyline
        if (sensorLayer.polyline) {
          sensorLayer.polyline.setLatLngs(pathCoords)
        } else {
          sensorLayer.polyline = L.polyline(pathCoords, {
            color: color,
            weight: 2,
            opacity: POINT_TRAJECTORY_OPACITY,
            smoothFactor: 1
          }).addTo(instantMarkersLayer)
        }
      }
    } else if (!isPointBased && measurements.length > 1) {
      // For polygons/shapes: Show historical shapes with equal opacity
      // Remove the polyline if it exists (not needed for shapes)
      if (sensorLayer.polyline) {
        sensorLayer.polyline.remove()
        sensorLayer.polyline = null
      }

      // Render all historical shapes except the latest with equal opacity
      const historicalMeasurements = measurements.slice(0, -1)

      historicalMeasurements.forEach((m) => {
        const geometry = extractGeometry(m.value)
        if (geometry) {
          const coords = extractCoordinates(m.value)
          if (coords) bounds.push(coords)

          // All historical shapes have equal opacity
          const historicalLayer = renderGeometry(
            geometry,
            instantMarkersLayer,
            color,
            null,
            HISTORICAL_SHAPE_OPACITY
          )
          if (historicalLayer) {
            sensorLayer.historicalGeometries.push(historicalLayer)
          }
        }
      })

      // Draw projection area between consecutive shapes
      if (ENABLE_POLYGON_PROJECTIONS) {
        for (let i = 0; i < historicalMeasurements.length; i++) {
          const currentGeometry = i === historicalMeasurements.length - 1
            ? extractGeometry(latest.value)
            : extractGeometry(historicalMeasurements[i + 1].value)
          const previousGeometry = extractGeometry(historicalMeasurements[i].value)

          if (previousGeometry && currentGeometry && previousGeometry.type === 'Polygon' && currentGeometry.type === 'Polygon') {
            const prevCoords = previousGeometry.coordinates[0]
            const currCoords = currentGeometry.coordinates[0]

            // Draw filled projection quads between corresponding edges
            const minLength = Math.min(prevCoords.length, currCoords.length)
            for (let j = 0; j < minLength - 1; j++) {
              // Create a quad from two consecutive points on each polygon
              const quadCoords = [
                [prevCoords[j][1], prevCoords[j][0]],
                [prevCoords[j + 1][1], prevCoords[j + 1][0]],
                [currCoords[j + 1][1], currCoords[j + 1][0]],
                [currCoords[j][1], currCoords[j][0]]
              ]

              const projectionQuad = L.polygon(quadCoords, {
                fillColor: color,
                color: color,
                weight: 1,
                opacity: POLYGON_PROJECTION_BORDER_OPACITY,
                fillOpacity: POLYGON_PROJECTION_FILL_OPACITY
              }).addTo(instantMarkersLayer)

              sensorLayer.projectionLines.push(projectionQuad)
            }
          }
        }
      }
    } else {
      // Only one measurement or no latest - remove polyline if exists
      if (sensorLayer.polyline) {
        sensorLayer.polyline.remove()
        sensorLayer.polyline = null
      }
    }

    // Render the latest position/geometry for this sensor
    if (latest) {
      const geometry = extractGeometry(latest.value)
      const coords = extractCoordinates(latest.value)

      if (coords) {
        bounds.push(coords)

        const popup = `
          <strong>${latest.sensor_name}</strong><br>
          ${formatTimestamp(latest.timestamp)}<br>
          ${formatGeoValue(latest.value)}
        `

        // Remove old current marker if exists
        if (sensorLayer.currentMarker) {
          sensorLayer.currentMarker.remove()
        }

        // Render new geometry with full opacity
        sensorLayer.currentMarker = renderGeometry(geometry, instantMarkersLayer, color, popup, 1.0)
      }
    }
  })

  // Remove sensors that no longer exist at this time
  Object.keys(sensorLayers).forEach(sensorName => {
    if (!sensorData[sensorName]) {
      const layer = sensorLayers[sensorName]
      if (layer.polyline) layer.polyline.remove()
      if (layer.currentMarker) layer.currentMarker.remove()
      layer.historicalGeometries.forEach(g => g.remove())
      layer.projectionLines.forEach(l => l.remove())
      delete sensorLayers[sensorName]
    }
  })

  // Fit bounds on first render
  if (bounds.length > 0 && !mapInstant._boundsSet) {
    mapInstant.fitBounds(bounds, { padding: [50, 50] })
    mapInstant._boundsSet = true
  }
}

function extractGeometry(value) {
  if (!value) return null

  // Try to parse WKT format first if it's a string
  if (typeof value === 'string') {
    try {
      const wkt = new Wkt.Wkt()
      wkt.read(value)
      const geojson = wkt.toJson()
      return geojson
    } catch (wktError) {
      // Not WKT, try JSON parsing
      try {
        value = JSON.parse(value)
      } catch {
        return null
      }
    }
  }

  if (typeof value !== 'object') return null

  // Already GeoJSON
  if (value.type && value.coordinates) {
    return value
  }

  // GeoJSON Feature
  if (value.type === 'Feature' && value.geometry) {
    return value.geometry
  }

  // lat/lon to Point
  if (value.lat !== undefined && value.lon !== undefined) {
    return {
      type: 'Point',
      coordinates: [value.lon, value.lat]
    }
  }
  if (value.latitude !== undefined && value.longitude !== undefined) {
    return {
      type: 'Point',
      coordinates: [value.longitude, value.latitude]
    }
  }

  return null
}

function extractCoordinates(value) {
  const geom = extractGeometry(value)
  if (!geom) return null

  // Convert geometry to a single coordinate point
  if (geom.type === 'Point' && geom.coordinates) {
    return [geom.coordinates[1], geom.coordinates[0]] // [lat, lng]
  }

  // For Polygon/MultiPolygon, use the centroid of first ring
  if (geom.type === 'Polygon' && geom.coordinates?.[0]) {
    const centroid = calculateCentroid(geom.coordinates[0])
    return centroid
  }

  if (geom.type === 'MultiPolygon' && geom.coordinates?.[0]?.[0]) {
    const centroid = calculateCentroid(geom.coordinates[0][0])
    return centroid
  }

  return null
}

function renderGeometry(geometry, layer, color, popup, opacityMultiplier = 1.0) {
  if (!geometry) return null

  let leafletLayer = null

  switch (geometry.type) {
    case 'Point':
      leafletLayer = L.circleMarker([geometry.coordinates[1], geometry.coordinates[0]], {
        radius: 6,
        fillColor: color,
        color: '#ffffff',
        weight: 2,
        opacity: 1 * opacityMultiplier,
        fillOpacity: 0.7 * opacityMultiplier
      })
      break

    case 'Polygon':
      const polygonCoords = geometry.coordinates[0].map(coord => [coord[1], coord[0]])
      leafletLayer = L.polygon(polygonCoords, {
        fillColor: color,
        color: color,
        weight: 2,
        opacity: 0.8 * opacityMultiplier,
        fillOpacity: 0.3 * opacityMultiplier
      })
      break

    case 'MultiPolygon':
      const multiPolygonCoords = geometry.coordinates.map(poly =>
        poly[0].map(coord => [coord[1], coord[0]])
      )
      leafletLayer = L.polygon(multiPolygonCoords, {
        fillColor: color,
        color: color,
        weight: 2,
        opacity: 0.8 * opacityMultiplier,
        fillOpacity: 0.3 * opacityMultiplier
      })
      break

    case 'LineString':
      const lineCoords = geometry.coordinates.map(coord => [coord[1], coord[0]])
      leafletLayer = L.polyline(lineCoords, {
        color: color,
        weight: 3,
        opacity: 0.8 * opacityMultiplier
      })
      break

    default:
      // Fallback to centroid marker
      const coords = extractCoordinates(geometry)
      if (coords) {
        leafletLayer = L.circleMarker(coords, {
          radius: 6,
          fillColor: color,
          color: '#ffffff',
          weight: 2,
          opacity: 1 * opacityMultiplier,
          fillOpacity: 0.7 * opacityMultiplier
        })
      }
  }

  if (leafletLayer) {
    leafletLayer.addTo(layer)
    if (popup) {
      leafletLayer.bindPopup(popup)
    }
  }

  return leafletLayer
}

function calculateCentroid(coordinates) {
  if (!coordinates || coordinates.length === 0) return null

  let latSum = 0
  let lngSum = 0

  coordinates.forEach(coord => {
    lngSum += coord[0]
    latSum += coord[1]
  })

  return [latSum / coordinates.length, lngSum / coordinates.length]
}

function formatTimestamp(timestamp) {
  if (!timestamp) return '-'
  return new Date(timestamp).toLocaleString()
}

function formatGeoValue(value) {
  if (value === null || value === undefined) return '-'
  if (typeof value === 'object') {
    // Handle GeoJSON or coordinate objects
    if (value.coordinates) {
      return `[${value.coordinates.join(', ')}]`
    }
    if (value.lat !== undefined && value.lon !== undefined) {
      return `${value.lat}, ${value.lon}`
    }
    return JSON.stringify(value)
  }
  return String(value)
}

function prevTimestamp() {
  if (sliderIndex.value > 0) {
    sliderIndex.value--
  }
}

function nextTimestamp() {
  if (sliderIndex.value < timestamps.value.length - 1) {
    sliderIndex.value++
  }
}
</script>

<style scoped>
.geo-timeseries-view {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.view-toggle {
  display: flex;
  gap: 0.5rem;
}

.map-container {
  height: 500px;
  width: 100%;
  border-radius: 0.375rem;
  border: 1px solid var(--border-color);
  overflow: hidden;
  margin-top: 1rem;
}

.text-secondary {
  color: var(--text-secondary);
  margin-top: 0.5rem;
}

.slider-controls {
  padding: 1.5rem;
  background: var(--surface-color);
  border: 1px solid var(--border-color);
  border-radius: 0.375rem;
}

.slider-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.slider-label {
  font-weight: 600;
  color: var(--text-primary);
}

.slider-value {
  font-family: monospace;
  font-size: 0.875rem;
  color: var(--primary-color);
}

.time-slider {
  width: 100%;
  height: 8px;
  border-radius: 4px;
  background: var(--border-color);
  outline: none;
  -webkit-appearance: none;
  appearance: none;
  margin-bottom: 1rem;
}

.time-slider::-webkit-slider-thumb {
  -webkit-appearance: none;
  appearance: none;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: var(--primary-color);
  cursor: pointer;
}

.time-slider::-moz-range-thumb {
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: var(--primary-color);
  cursor: pointer;
  border: none;
}

.slider-info {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.slider-controls-buttons {
  display: flex;
  gap: 0.5rem;
}

.instant-measurements {
  margin-top: 1rem;
}

.instant-measurements h4 {
  margin-bottom: 1rem;
  font-size: 1rem;
}

.measurements-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 1rem;
}

.measurement-card {
  padding: 1rem;
  background: var(--bg-color);
  border: 1px solid var(--border-color);
  border-radius: 0.375rem;
}

.measurement-sensor {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 0.5rem;
  font-family: monospace;
}

.measurement-value {
  font-size: 0.875rem;
  color: var(--primary-color);
}
</style>
