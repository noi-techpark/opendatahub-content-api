<template>
  <div class="bulk-timeseries-inspector">
    <div class="container">
      <div class="page-header">
        <h1>Bulk Timeseries Inspector</h1>
        <p>View timeseries data for multiple entries</p>
      </div>

      <!-- Entry Selection -->
      <div class="selection-info card">
        <h3>Selected Entries ({{ selectedEntries.length }})</h3>
        <div class="entry-chips">
          <span
            v-for="entry in selectedEntries"
            :key="entry.Id"
            class="badge badge-primary"
          >
            {{ entry.Shortname || entry.Id }}
          </span>
        </div>
      </div>

      <!-- Timeseries Type Selection -->
      <div class="type-selection card">
        <h3>Select Timeseries Types</h3>
        <p>Choose which timeseries types to load for the selected entries</p>

        <div v-if="loadingTypes" class="loading">
          <div class="spinner"></div>
        </div>

        <div v-else class="type-checkboxes">
          <label
            v-for="type in availableTypes"
            :key="type"
            class="type-checkbox"
          >
            <input
              type="checkbox"
              :checked="selectedTypes.includes(type)"
              @change="toggleType(type)"
            />
            {{ type }}
          </label>
        </div>

        <button
          @click="loadMeasurements"
          class="btn btn-primary"
          :disabled="selectedTypes.length === 0"
        >
          Load Measurements
        </button>
      </div>

      <!-- View Tabs -->
      <div v-if="measurements.length > 0" class="view-section">
        <div class="tabs">
          <button
            @click="view.value = 'table'"
            class="tab"
            :class="{ active: view.value === 'table' }"
          >
            Table View
          </button>
          <button
            @click="view.value = 'raw'"
            class="tab"
            :class="{ active: view.value === 'raw' }"
          >
            Raw JSON
          </button>
          <button
            @click="view.value = 'formatted'"
            class="tab"
            :class="{ active: view.value === 'formatted' }"
          >
            Formatted View
          </button>
        </div>

        <!-- Table View -->
        <div v-if="view.value === 'table'" class="table-view">
          <div class="table-wrapper">
            <table class="table">
              <thead>
                <tr>
                  <th>Sensor Name</th>
                  <th>Type</th>
                  <th>Timestamp</th>
                  <th>Value</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(measurement, index) in measurements" :key="index">
                  <td>{{ measurement.sensor_name }}</td>
                  <td>{{ measurement.type_name }}</td>
                  <td>{{ formatTimestamp(measurement.timestamp) }}</td>
                  <td>{{ formatValue(measurement.value) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Raw JSON View -->
        <div v-else-if="view.value === 'raw'" class="raw-view">
          <JsonViewer :data="{ measurements, total: measurements.length }" />
        </div>

        <!-- Formatted View -->
        <div v-else-if="view.value === 'formatted'" class="formatted-view">
          <div v-for="type in selectedTypes" :key="type" class="type-section card">
            <h3>{{ type }}</h3>
            <div class="type-measurements">
              <p>Formatted visualization for {{ type }} would go here</p>
              <p class="text-secondary">
                Charts for numeric types, maps for geographic types, tables for others
              </p>
            </div>
          </div>
        </div>
      </div>

      <div v-else-if="!loadingMeasurements" class="empty-state">
        <p>Select timeseries types and click "Load Measurements" to view data</p>
      </div>

      <div v-if="loadingMeasurements" class="loading">
        <div class="spinner"></div>
        <p>Loading measurements...</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useSelectionStore } from '../stores/selectionStore'
import { useTimeseriesStore } from '../stores/timeseriesStore'
import { useBulkTimeseriesUrlState } from '../composables/useUrlState'
import JsonViewer from '../components/JsonViewer.vue'

const route = useRoute()
const selectionStore = useSelectionStore()
const timeseriesStore = useTimeseriesStore()

// URL-synced state
const { view, types: urlTypes } = useBulkTimeseriesUrlState()

// Local state
const selectedEntries = ref([])
const availableTypes = ref([])
const selectedTypes = ref([])
const measurements = ref([])
const loadingTypes = ref(false)
const loadingMeasurements = ref(false)

// Initialize
onMounted(async () => {
  // Get selected entries from selection store
  selectedEntries.value = selectionStore.selectedEntries

  // Restore types from URL
  if (urlTypes.value && urlTypes.value.length > 0) {
    selectedTypes.value = urlTypes.value
  }

  // Load available types for the selected entries
  await loadAvailableTypes()
})

async function loadAvailableTypes() {
  if (selectedEntries.value.length === 0) return

  loadingTypes.value = true
  try {
    // TODO: Get timeseries types available for these entries
    // This would require mapping entries to sensors via Mapping field
    availableTypes.value = [
      'temperature',
      'humidity',
      'coordinate',
      'air_quality'
    ]
  } finally {
    loadingTypes.value = false
  }
}

function toggleType(type) {
  const index = selectedTypes.value.indexOf(type)
  if (index > -1) {
    selectedTypes.value.splice(index, 1)
  } else {
    selectedTypes.value.push(type)
  }

  // Update URL
  urlTypes.value = selectedTypes.value
}

async function loadMeasurements() {
  if (selectedTypes.value.length === 0) return

  loadingMeasurements.value = true
  try {
    // TODO: Load measurements for selected entries and types
    // This would require getting sensor names from entries
    // and then loading measurements for those sensors

    const sensorNames = selectedEntries.value.map(e => `sensor_${e.Id}`)

    const result = await timeseriesStore.loadLatestMeasurements(
      sensorNames,
      selectedTypes.value
    )

    measurements.value = result?.measurements || []
  } catch (err) {
    console.error('Error loading measurements:', err)
  } finally {
    loadingMeasurements.value = false
  }
}

function formatTimestamp(timestamp) {
  return new Date(timestamp).toLocaleString()
}

function formatValue(value) {
  if (typeof value === 'object') return JSON.stringify(value)
  return String(value)
}
</script>

<style scoped>
.bulk-timeseries-inspector {
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
.type-section {
  margin-bottom: 2rem;
}

.selection-info h3,
.type-selection h3,
.type-section h3 {
  margin-bottom: 1rem;
}

.type-selection p {
  color: var(--text-secondary);
  margin-bottom: 1rem;
}

.entry-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.type-checkboxes {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 1rem;
  margin: 1.5rem 0;
}

.type-checkbox {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
}

.type-checkbox input {
  cursor: pointer;
}

.view-section {
  margin-top: 2rem;
}

.table-wrapper {
  overflow-x: auto;
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  margin-top: 1rem;
}

.type-measurements {
  padding: 2rem;
  text-align: center;
  background: var(--bg-color);
  border-radius: 0.375rem;
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
</style>
