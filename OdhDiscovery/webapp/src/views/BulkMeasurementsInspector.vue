<template>
  <div class="bulk-measurements-inspector">
    <div class="container">
      <div class="page-header">
        <h1>Bulk Measurements Inspector</h1>
        <p>View measurements for multiple sensors and types</p>
      </div>

      <!-- Sensor Selection Info -->
      <div class="selection-info card">
        <div class="sensors-header">
          <h3>Selected Sensors ({{ enabledSensors.length }} / {{ sensorNames.length }} enabled)</h3>
        </div>
        <div class="sensor-chips">
          <label
            v-for="sensor in sensorNames"
            :key="sensor"
            class="sensor-chip"
            :class="{ 'sensor-disabled': isSensorDisabled(sensor) }"
          >
            <input
              type="checkbox"
              :checked="!isSensorDisabled(sensor)"
              @change="toggleSensor(sensor)"
            />
            <span class="sensor-name">{{ sensor }}</span>
          </label>
        </div>
      </div>

      <!-- Type Selection -->
      <div class="type-selection card">
        <h3>Measurement Types</h3>
        <p>Select one or more measurement types to load</p>

        <div v-if="loadingTypes" class="loading-inline">
          <div class="spinner spinner-sm"></div>
          <span>Loading available types...</span>
        </div>

        <div v-else-if="availableTypes.length > 0" class="type-checkboxes">
          <label
            v-for="type in availableTypes"
            :key="type"
            class="type-checkbox"
          >
            <input
              type="checkbox"
              :value="type"
              v-model="selectedTypes"
            />
            <span>{{ type }}</span>
          </label>
        </div>

        <div v-else class="empty-types">
          <p class="text-secondary">No measurement types available for these sensors</p>
        </div>

        <div v-if="selectedTypes.length > 0" class="selected-types-info">
          <span class="badge badge-primary">{{ selectedTypes.length }} type(s) selected</span>
        </div>
      </div>

      <!-- Date Range Selection -->
      <div class="controls-section card">
        <h3>Measurement Parameters</h3>
        <div class="controls-grid">
          <div class="control-group">
            <label>From Date</label>
            <input
              v-model="fromDate"
              type="datetime-local"
              class="input"
            />
          </div>
          <div class="control-group">
            <label>To Date</label>
            <input
              v-model="toDate"
              type="datetime-local"
              class="input"
            />
          </div>
        </div>
        <div class="control-actions">
          <button
            @click="loadLatestMeasurements"
            class="btn btn-primary"
            :disabled="loading || selectedTypes.length === 0"
          >
            Load Latest Measurements
          </button>
          <button
            @click="loadHistoricalMeasurements"
            class="btn btn-secondary"
            :disabled="loading || selectedTypes.length === 0"
          >
            Load Historical Range
          </button>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="loading">
        <div class="spinner"></div>
        <p>Loading measurements...</p>
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="error-message">
        {{ error }}
      </div>

      <!-- View Tabs -->
      <div v-else-if="measurements.length > 0" class="view-section">
        <div class="controls">
          <div class="tabs">
            <button
              @click="view = 'table'"
              class="tab"
              :class="{ active: view === 'table' }"
            >
              Table View
            </button>
            <button
              @click="view = 'raw'"
              class="tab"
              :class="{ active: view === 'raw' }"
            >
              Raw JSON
            </button>
            <button
              @click="view = 'pretty'"
              class="tab"
              :class="{ active: view === 'pretty' }"
            >
              Pretty View
            </button>
          </div>
          <div class="measurement-count">
            {{ measurements.length }} measurements loaded
          </div>
        </div>

        <!-- Table View -->
        <div v-if="view === 'table'" class="table-view">
          <div class="table-wrapper">
            <table class="table">
              <thead>
                <tr>
                  <th class="sensor-col">Sensor</th>
                  <th class="timestamp-col">Timestamp</th>
                  <th class="value-col">Value</th>
                  <th class="type-col">Type</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(measurement, index) in sortedMeasurements" :key="index">
                  <td class="sensor-cell">{{ measurement.sensor_name }}</td>
                  <td class="timestamp-cell">{{ formatTimestamp(measurement.timestamp) }}</td>
                  <td class="value-cell">{{ formatValue(measurement.value) }}</td>
                  <td class="type-cell">
                    <span class="type-badge">{{ measurement.type_name }}</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Raw JSON View -->
        <div v-else-if="view === 'raw'" class="raw-view">
          <JsonViewer :data="{ measurements, total: measurements.length, sensors: enabledSensors, allSensors: sensorNames, disabledSensors, types: selectedTypes }" />
        </div>

        <!-- Pretty View -->
        <div v-else-if="view === 'pretty'" class="pretty-view">
          <div v-for="type in measurementTypeGroups" :key="type.name" class="type-group card">
            <div class="type-header">
              <h3>{{ type.name }}</h3>
              <span class="badge badge-primary">{{ type.measurements.length }} measurements</span>
            </div>

            <div class="type-visualization">
              <!-- Numeric timeseries: chart with toggle to table -->
              <NumericTimeseriesView v-if="type.dataType === 'numeric'" :measurements="type.measurements" />

              <!-- String timeseries: table -->
              <StringBooleanTimeseriesView v-else-if="type.dataType === 'string'" :measurements="type.measurements" />

              <!-- Boolean timeseries: table -->
              <StringBooleanTimeseriesView v-else-if="type.dataType === 'boolean'" :measurements="type.measurements" />

              <!-- Geographic timeseries: map with instant slider -->
              <GeoTimeseriesView v-else-if="type.dataType === 'geoposition' || type.dataType === 'geoshape' || type.dataType === 'coverage_area'" :measurements="type.measurements" />

              <!-- JSON timeseries: table with detailed view -->
              <JsonTimeseriesView v-else-if="type.dataType === 'json'" :measurements="type.measurements" />

              <!-- Fallback for unknown types -->
              <StringBooleanTimeseriesView v-else :measurements="type.measurements" />
            </div>
          </div>
        </div>
      </div>

      <!-- Empty State -->
      <div v-else-if="!loading" class="empty-state">
        <p>No measurements loaded yet.</p>
        <p class="text-secondary">Select measurement types and click "Load Latest Measurements" or "Load Historical Range" to fetch data.</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useTimeseriesStore } from '../stores/timeseriesStore'
import { useBulkMeasurementsUrlState } from '../composables/useUrlState'
import JsonViewer from '../components/JsonViewer.vue'
import NumericTimeseriesView from '../components/NumericTimeseriesView.vue'
import StringBooleanTimeseriesView from '../components/StringBooleanTimeseriesView.vue'
import GeoTimeseriesView from '../components/GeoTimeseriesView.vue'
import JsonTimeseriesView from '../components/JsonTimeseriesView.vue'

const route = useRoute()
const timeseriesStore = useTimeseriesStore()
const urlState = useBulkMeasurementsUrlState()

// URL-synced state
const selectedTypes = urlState.types
const disabledSensors = urlState.disabledSensors
const view = urlState.view

// Local state
const sensorNames = ref([])
const availableTypes = ref([])
const measurements = ref([])
const loading = ref(false)
const loadingTypes = ref(false)
const error = ref(null)

// Date range controls
const fromDate = ref('')
const toDate = ref('')

// Computed list of enabled sensors
const enabledSensors = computed(() => {
  return sensorNames.value.filter(sensor => !disabledSensors.value.includes(sensor))
})

// Initialize from URL params
onMounted(async () => {
  // Load sensors from URL state
  if (urlState.sensors.value && urlState.sensors.value.length > 0) {
    sensorNames.value = urlState.sensors.value
  }

  // Set default date range (last 24 hours)
  const now = new Date()
  const yesterday = new Date(now.getTime() - 24 * 60 * 60 * 1000)

  toDate.value = formatDateTimeLocal(now)
  fromDate.value = formatDateTimeLocal(yesterday)

  // Load available types for these sensors
  await loadAvailableTypes()
})

// Watch sensorNames and update URL
watch(sensorNames, (newSensors) => {
  urlState.sensors.value = newSensors
}, { deep: true })

// Watch disabledSensors and reload available types
watch(disabledSensors, async () => {
  await loadAvailableTypes()
}, { deep: true })

// Helper functions for sensor toggling
function isSensorDisabled(sensor) {
  return disabledSensors.value.includes(sensor)
}

function toggleSensor(sensor) {
  const index = disabledSensors.value.indexOf(sensor)
  if (index > -1) {
    // Enable sensor (remove from disabled list)
    disabledSensors.value = disabledSensors.value.filter(s => s !== sensor)
  } else {
    // Disable sensor (add to disabled list)
    disabledSensors.value = [...disabledSensors.value, sensor]
  }
}

const sortedMeasurements = computed(() => {
  return [...measurements.value].sort((a, b) => {
    return new Date(b.timestamp) - new Date(a.timestamp)
  })
})

const measurementTypeGroups = computed(() => {
  if (measurements.value.length === 0) return []

  const groups = {}

  measurements.value.forEach(m => {
    const typeName = m.type_name || 'unknown'
    if (!groups[typeName]) {
      groups[typeName] = []
    }
    groups[typeName].push(m)
  })

  const a = Object.entries(groups).map(([name, meas]) => {
    const dataType = detectDataType(meas)

    return {
      name,
      measurements: meas,
      dataType
    }
  })
  console.log(a)
  return a
})

function detectDataType(measurements) {
  if (measurements.length === 0) return 'unknown'

  // Check if we have a data_type field from the API
  const firstMeasurement = measurements[0]
  if (firstMeasurement.data_type) {
    return firstMeasurement.data_type
  }

  // Otherwise, analyze the values to determine the data type
  const typeCounts = {
    numeric: 0,
    string: 0,
    boolean: 0,
    geographic: 0,
    json: 0
  }

  measurements.forEach(m => {
    const value = m.value

    if (value === null || value === undefined) {
      return
    }

    if (typeof value === 'number') {
      typeCounts.numeric++
    } else if (typeof value === 'boolean') {
      typeCounts.boolean++
    } else if (typeof value === 'string') {
      // Check if it's a parseable number
      if (!isNaN(parseFloat(value)) && isFinite(value)) {
        typeCounts.numeric++
      } else {
        typeCounts.string++
      }
    } else if (typeof value === 'object') {
      // Check if it's geographic
      if (isGeographic(value)) {
        typeCounts.geographic++
      } else {
        typeCounts.json++
      }
    }
  })

  // Return the type with the most occurrences
  const maxEntry = Object.entries(typeCounts).reduce((a, b) => a[1] > b[1] ? a : b)
  return maxEntry[0]
}

function isGeographic(value) {
  // Handle null/undefined
  if (!value) return false

  // If it's a string, try to parse it as JSON
  if (typeof value === 'string') {
    try {
      const parsed = JSON.parse(value)
      return isGeographic(parsed)
    } catch {
      return false
    }
  }

  // Must be an object
  if (typeof value !== 'object') return false

  // Check for GeoJSON geometry types
  const geoJsonTypes = [
    'Point', 'LineString', 'Polygon',
    'MultiPoint', 'MultiLineString', 'MultiPolygon',
    'GeometryCollection', 'Feature', 'FeatureCollection'
  ]

  if (value.type && geoJsonTypes.includes(value.type)) {
    return true
  }

  // Check for geometry property (Feature or FeatureCollection)
  if (value.geometry && typeof value.geometry === 'object') {
    return true
  }

  // Check for coordinates array (any geometry)
  if (value.coordinates !== undefined && Array.isArray(value.coordinates)) {
    return true
  }

  // Check for common lat/lon patterns
  if ((value.lat !== undefined && value.lon !== undefined) ||
      (value.latitude !== undefined && value.longitude !== undefined)) {
    return true
  }

  // Check for properties that suggest geographic data
  if (value.features && Array.isArray(value.features)) {
    return true
  }

  return false
}

async function loadAvailableTypes() {
  loadingTypes.value = true
  error.value = null

  try {
    // Use enabled sensors for querying types
    const sensorsToQuery = enabledSensors.value.length > 0 ? enabledSensors.value : []

    const result = await timeseriesStore.getTypesForSensors(sensorsToQuery, true)
    if (result && result.types) {
      availableTypes.value = result.types.map(t => t.type_name || t.name || t).filter(Boolean)
    } else {
      availableTypes.value = []
    }
  } catch (err) {
    console.error('Error loading types:', err)
    error.value = `Failed to load available types: ${err.message}`
  } finally {
    loadingTypes.value = false
  }
}

async function loadLatestMeasurements() {
  if (selectedTypes.value.length === 0) {
    error.value = 'Please select at least one measurement type'
    return
  }

  loading.value = true
  error.value = null

  try {
    // Use only enabled sensors
    const result = await timeseriesStore.loadLatestMeasurements(
      enabledSensors.value,
      selectedTypes.value
    )

    measurements.value = result?.measurements || []

    if (measurements.value.length === 0) {
      error.value = 'No measurements found for the selected sensors and types'
    }
  } catch (err) {
    error.value = `Failed to load measurements: ${err.message}`
    console.error('Error loading measurements:', err)
  } finally {
    loading.value = false
  }
}

async function loadHistoricalMeasurements() {
  if (selectedTypes.value.length === 0) {
    error.value = 'Please select at least one measurement type'
    return
  }

  if (!fromDate.value || !toDate.value) {
    error.value = 'Please select both from and to dates'
    return
  }

  loading.value = true
  error.value = null

  try {
    // Use only enabled sensors
    const payload = {
      sensor_names: enabledSensors.value,
      type_names: selectedTypes.value,
      from: new Date(fromDate.value).toISOString(),
      to: new Date(toDate.value).toISOString(),
      limit: -1
    }

    const result = await timeseriesStore.loadHistoricalMeasurements(payload)

    measurements.value = result?.measurements || []

    if (measurements.value.length === 0) {
      error.value = 'No measurements found in the selected date range'
    }
  } catch (err) {
    error.value = `Failed to load measurements: ${err.message}`
    console.error('Error loading measurements:', err)
  } finally {
    loading.value = false
  }
}

function formatTimestamp(timestamp) {
  if (!timestamp) return '-'
  return new Date(timestamp).toLocaleString()
}

function formatValue(value) {
  if (value === null || value === undefined) return '-'
  if (typeof value === 'object') return JSON.stringify(value)
  if (typeof value === 'number') return value.toFixed(2)
  return String(value)
}

function formatDateTimeLocal(date) {
  const pad = (n) => n.toString().padStart(2, '0')
  const year = date.getFullYear()
  const month = pad(date.getMonth() + 1)
  const day = pad(date.getDate())
  const hours = pad(date.getHours())
  const minutes = pad(date.getMinutes())

  return `${year}-${month}-${day}T${hours}:${minutes}`
}
</script>

<style scoped>
.bulk-measurements-inspector {
  padding: 2rem 0;
}

.page-header {
  margin-bottom: 2rem;
}

.page-header h1 {
  font-size: 2rem;
  margin-bottom: 0.5rem;
}

.page-header p {
  color: var(--text-secondary);
}

.selection-info,
.type-selection,
.controls-section,
.view-section {
  margin-bottom: 2rem;
}

.selection-info h3,
.type-selection h3,
.controls-section h3 {
  margin-bottom: 1rem;
}

.type-selection p {
  color: var(--text-secondary);
  margin-bottom: 1rem;
}

.sensors-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.sensor-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  max-height: 200px;
  overflow-y: auto;
  padding: 0.5rem;
  background: var(--bg-color);
  border-radius: 0.375rem;
}

.sensor-chip {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  background: var(--surface-color);
  border: 1px solid var(--border-color);
  border-radius: 0.375rem;
  cursor: pointer;
  transition: all 0.2s;
  font-size: 0.875rem;
}

.sensor-chip:hover {
  background: var(--bg-color);
  border-color: var(--primary-color);
}

.sensor-chip input {
  cursor: pointer;
}

.sensor-chip.sensor-disabled {
  opacity: 0.5;
  background: var(--bg-color);
}

.sensor-chip.sensor-disabled .sensor-name {
  text-decoration: line-through;
  color: var(--text-secondary);
}

.sensor-name {
  font-family: monospace;
  font-weight: 500;
  color: var(--text-primary);
}

.loading-inline {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem;
  background: var(--bg-color);
  border-radius: 0.375rem;
}

.spinner-sm {
  width: 20px;
  height: 20px;
  border-width: 2px;
}

.type-checkboxes {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 0.75rem;
  margin: 1rem 0;
  padding: 1rem;
  background: var(--bg-color);
  border-radius: 0.375rem;
  max-height: 300px;
  overflow-y: auto;
}

.type-checkbox {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  padding: 0.5rem;
  border-radius: 0.25rem;
}

.type-checkbox:hover {
  background: var(--surface-color);
}

.type-checkbox input {
  cursor: pointer;
}

.empty-types {
  padding: 2rem;
  text-align: center;
}

.selected-types-info {
  margin-top: 1rem;
}

.controls-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.control-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.control-group label {
  font-weight: 600;
  font-size: 0.875rem;
  color: var(--text-primary);
}

.control-actions {
  display: flex;
  gap: 1rem;
}

.controls {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.measurement-count {
  font-size: 0.875rem;
  color: var(--text-secondary);
  font-weight: 600;
}

.loading {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  padding: 3rem;
  gap: 1rem;
}

.error-message {
  padding: 2rem;
  text-align: center;
  color: var(--danger-color);
  background: rgba(239, 68, 68, 0.1);
  border-radius: 0.5rem;
  margin: 2rem 0;
}

.table-wrapper {
  overflow-x: auto;
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  margin-top: 1rem;
}

.table {
  width: 100%;
}

.sensor-col {
  min-width: 250px;
}

.timestamp-col {
  min-width: 180px;
}

.value-col {
  min-width: 120px;
}

.type-col {
  min-width: 150px;
}

.sensor-cell {
  font-family: monospace;
  font-size: 0.875rem;
}

.timestamp-cell {
  font-family: monospace;
  font-size: 0.875rem;
  white-space: nowrap;
}

.value-cell {
  font-weight: 600;
  color: var(--primary-color);
}

.type-cell {
  font-size: 0.875rem;
}

.type-badge {
  display: inline-block;
  padding: 0.25rem 0.75rem;
  background: var(--primary-color);
  color: white;
  border-radius: 9999px;
  font-size: 0.75rem;
  font-weight: 600;
}

.raw-view {
  margin-top: 1rem;
}

.pretty-view {
  margin-top: 1rem;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.type-group {
  padding: 1.5rem;
}

.type-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--border-color);
}

.type-header h3 {
  margin: 0;
  font-size: 1.25rem;
}

.text-secondary {
  color: var(--text-secondary);
  margin-top: 0.5rem;
}

.empty-state {
  padding: 4rem 2rem;
  text-align: center;
  color: var(--text-secondary);
  background: var(--bg-color);
  border-radius: 0.5rem;
  margin-top: 2rem;
}

.empty-state p {
  margin-bottom: 0.5rem;
}
</style>
