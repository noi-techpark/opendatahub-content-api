<template>
  <div class="json-key-visualization">
    <!-- Numeric values -->
    <NumericTimeseriesView v-if="dataType === 'numeric'" :measurements="measurements" />

    <!-- String values -->
    <StringBooleanTimeseriesView v-else-if="dataType === 'string'" :measurements="measurements" />

    <!-- Boolean values -->
    <StringBooleanTimeseriesView v-else-if="dataType === 'boolean'" :measurements="measurements" />

    <!-- Geographic values -->
    <GeoTimeseriesView v-else-if="dataType === 'geographic'" :measurements="measurements" />

    <!-- Nested JSON values (recursive) -->
    <JsonTimeseriesView v-else-if="dataType === 'json'" :measurements="measurements" />

    <!-- Fallback for mixed/unknown types -->
    <div v-else class="mixed-type-view">
      <p class="info-text">Mixed or unknown data types detected</p>
      <div class="table-wrapper">
        <table class="table table-sm">
          <thead>
            <tr>
              <th>Sensor</th>
              <th>Timestamp</th>
              <th>Value</th>
              <th>Type</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(m, idx) in sortedMeasurements" :key="idx">
              <td class="sensor-cell">{{ m.sensor_name }}</td>
              <td class="timestamp-cell">{{ formatTimestamp(m.timestamp) }}</td>
              <td class="value-cell">{{ formatValue(m.value) }}</td>
              <td class="type-cell">{{ typeof m.value }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import NumericTimeseriesView from './NumericTimeseriesView.vue'
import StringBooleanTimeseriesView from './StringBooleanTimeseriesView.vue'
import GeoTimeseriesView from './GeoTimeseriesView.vue'
import JsonTimeseriesView from './JsonTimeseriesView.vue'

const props = defineProps({
  keyName: {
    type: String,
    required: true
  },
  measurements: {
    type: Array,
    required: true
  }
})

const sortedMeasurements = computed(() => {
  return [...props.measurements].sort((a, b) => {
    return new Date(b.timestamp) - new Date(a.timestamp)
  })
})

// Determine the predominant data type
const dataType = computed(() => {
  if (props.measurements.length === 0) return 'unknown'

  const types = {
    numeric: 0,
    string: 0,
    boolean: 0,
    geographic: 0,
    json: 0
  }

  props.measurements.forEach(m => {
    const value = m.value

    if (value === null || value === undefined) {
      // Skip nulls
      return
    }

    if (typeof value === 'number') {
      types.numeric++
    } else if (typeof value === 'boolean') {
      types.boolean++
    } else if (typeof value === 'string') {
      // Check if it's a parseable number
      if (!isNaN(parseFloat(value)) && isFinite(value)) {
        types.numeric++
      } else {
        types.string++
      }
    } else if (typeof value === 'object') {
      // Check if it's geographic
      if (isGeographic(value)) {
        types.geographic++
      } else {
        types.json++
      }
    }
  })

  // Return the type with the most occurrences
  const maxType = Object.entries(types).reduce((a, b) => a[1] > b[1] ? a : b)
  return maxType[0]
})

function isGeographic(value) {
  if (!value || typeof value !== 'object') return false

  // Check for common geographic patterns
  return (
    (value.lat !== undefined && value.lon !== undefined) ||
    (value.latitude !== undefined && value.longitude !== undefined) ||
    (value.coordinates !== undefined && Array.isArray(value.coordinates)) ||
    (value.type === 'Point' && value.coordinates) ||
    value.geometry !== undefined
  )
}

function formatTimestamp(timestamp) {
  if (!timestamp) return '-'
  return new Date(timestamp).toLocaleString()
}

function formatValue(value) {
  if (value === null || value === undefined) return '-'
  if (typeof value === 'object') return JSON.stringify(value)
  if (typeof value === 'boolean') return value ? '✓' : '✗'
  return String(value)
}
</script>

<style scoped>
.json-key-visualization {
  /* Container for recursive visualizations */
}

.mixed-type-view {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.info-text {
  color: var(--text-secondary);
  font-style: italic;
}

.table-wrapper {
  max-height: 400px;
  overflow-y: auto;
  border: 1px solid var(--border-color);
  border-radius: 0.375rem;
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
  font-family: monospace;
  font-size: 0.875rem;
  max-width: 300px;
  overflow: hidden;
  text-overflow: ellipsis;
}

.type-cell {
  font-size: 0.75rem;
  color: var(--text-secondary);
}
</style>
