<template>
  <div class="json-timeseries-view">
    <div class="view-toggle">
      <button
        @click="viewMode = 'table'"
        class="btn btn-sm"
        :class="{ 'btn-primary': viewMode === 'table', 'btn-outline': viewMode !== 'table' }"
      >
        Table
      </button>
      <button
        @click="viewMode = 'detailed'"
        class="btn btn-sm"
        :class="{ 'btn-primary': viewMode === 'detailed', 'btn-outline': viewMode !== 'detailed' }"
      >
        Detailed View
      </button>
    </div>

    <!-- Table View -->
    <div v-if="viewMode === 'table'" class="table-view">
      <div class="table-wrapper">
        <table class="table table-sm">
          <thead>
            <tr>
              <th>Sensor</th>
              <th>Timestamp</th>
              <th>JSON Value</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(m, idx) in sortedMeasurements" :key="idx">
              <td class="sensor-cell">{{ m.sensor_name }}</td>
              <td class="timestamp-cell">{{ formatTimestamp(m.timestamp) }}</td>
              <td class="json-cell">
                <pre class="json-preview">{{ formatJson(m.value) }}</pre>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Detailed View -->
    <div v-else class="detailed-view">
      <div class="keys-section">
        <h4>Select JSON Keys to Explore:</h4>
        <div class="keys-list">
          <div
            v-for="key in distinctKeys"
            :key="key"
            class="key-item"
            @click="toggleKey(key)"
          >
            <span class="key-toggle">{{ expandedKeys.has(key) ? '▼' : '▶' }}</span>
            <span class="key-name">{{ key }}</span>
            <span class="key-count">{{ keyOccurrences[key] }} occurrences</span>
          </div>
        </div>
      </div>

      <div v-if="expandedKeys.size > 0" class="expanded-keys">
        <div v-for="key in Array.from(expandedKeys)" :key="key" class="expanded-key-section">
          <div class="expanded-key-header">
            <h4>{{ key }}</h4>
            <button @click="toggleKey(key)" class="btn btn-sm btn-outline">Close</button>
          </div>

          <!-- Recursive visualization based on value type -->
          <div class="key-visualization">
            <JsonKeyVisualization
              :keyName="key"
              :measurements="measurementsWithKey(key)"
            />
          </div>
        </div>
      </div>

      <div v-else class="empty-selection">
        <p class="text-secondary">Click on a key above to explore its values</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import JsonKeyVisualization from './JsonKeyVisualization.vue'

const props = defineProps({
  measurements: {
    type: Array,
    required: true
  }
})

const viewMode = ref('table')
const expandedKeys = ref(new Set())

const sortedMeasurements = computed(() => {
  return [...props.measurements].sort((a, b) => {
    return new Date(b.timestamp) - new Date(a.timestamp)
  })
})

// Get all distinct keys from JSON values
const distinctKeys = computed(() => {
  const keys = new Set()
  props.measurements.forEach(m => {
    if (m.value && typeof m.value === 'object') {
      Object.keys(m.value).forEach(key => keys.add(key))
    }
  })
  return Array.from(keys).sort()
})

// Count occurrences of each key
const keyOccurrences = computed(() => {
  const counts = {}
  props.measurements.forEach(m => {
    if (m.value && typeof m.value === 'object') {
      Object.keys(m.value).forEach(key => {
        counts[key] = (counts[key] || 0) + 1
      })
    }
  })
  return counts
})

function formatTimestamp(timestamp) {
  if (!timestamp) return '-'
  return new Date(timestamp).toLocaleString()
}

function formatJson(value) {
  if (value === null || value === undefined) return '-'
  if (typeof value === 'object') {
    return JSON.stringify(value, null, 2)
  }
  return String(value)
}

function toggleKey(key) {
  if (expandedKeys.value.has(key)) {
    expandedKeys.value.delete(key)
  } else {
    expandedKeys.value.add(key)
  }
  // Trigger reactivity
  expandedKeys.value = new Set(expandedKeys.value)
}

function measurementsWithKey(key) {
  return props.measurements
    .filter(m => m.value && typeof m.value === 'object' && key in m.value)
    .map(m => ({
      sensor_name: m.sensor_name,
      timestamp: m.timestamp,
      value: m.value[key]
    }))
}
</script>

<style scoped>
.json-timeseries-view {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.view-toggle {
  display: flex;
  gap: 0.5rem;
}

.table-wrapper {
  max-height: 500px;
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

.json-cell {
  max-width: 600px;
}

.json-preview {
  font-family: 'Monaco', 'Menlo', monospace;
  font-size: 0.75rem;
  margin: 0;
  padding: 0.5rem;
  background: var(--bg-color);
  border-radius: 0.25rem;
  overflow-x: auto;
  max-height: 200px;
  overflow-y: auto;
}

.keys-section {
  padding: 1rem;
  background: var(--surface-color);
  border: 1px solid var(--border-color);
  border-radius: 0.375rem;
  margin-bottom: 1rem;
}

.keys-section h4 {
  margin-bottom: 1rem;
  font-size: 1rem;
}

.keys-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  max-height: 300px;
  overflow-y: auto;
}

.key-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem;
  background: var(--bg-color);
  border: 1px solid var(--border-color);
  border-radius: 0.25rem;
  cursor: pointer;
  transition: all 0.2s;
}

.key-item:hover {
  background: var(--surface-color);
  border-color: var(--primary-color);
}

.key-toggle {
  font-size: 0.75rem;
  color: var(--text-secondary);
  width: 16px;
}

.key-name {
  flex: 1;
  font-family: monospace;
  font-weight: 600;
  color: var(--text-primary);
}

.key-count {
  font-size: 0.75rem;
  color: var(--text-secondary);
  padding: 0.25rem 0.5rem;
  background: var(--surface-color);
  border-radius: 0.25rem;
}

.expanded-keys {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.expanded-key-section {
  padding: 1.5rem;
  background: var(--surface-color);
  border: 1px solid var(--border-color);
  border-radius: 0.375rem;
}

.expanded-key-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--border-color);
}

.expanded-key-header h4 {
  margin: 0;
  font-family: monospace;
  font-size: 1.125rem;
}

.key-visualization {
  /* This will recursively render based on value type */
}

.empty-selection {
  padding: 3rem 2rem;
  text-align: center;
}

.text-secondary {
  color: var(--text-secondary);
}
</style>
