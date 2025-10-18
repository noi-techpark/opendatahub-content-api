<template>
  <div class="timeseries-inspector">
    <div class="container">
      <div class="page-header">
        <h1>{{ typeName }}</h1>
        <p>Inspect sensors and measurements for this timeseries type</p>
      </div>

      <div v-if="currentType" class="type-info card">
        <h3>Type Information</h3>
        <div class="info-grid grid grid-2">
          <div class="info-item">
            <span class="info-label">Description:</span>
            <span class="info-value">{{ currentType.type.description || 'N/A' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">Unit:</span>
            <span class="info-value">{{ currentType.type.unit || 'N/A' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">Data Type:</span>
            <span class="info-value">{{ currentType.type.data_type }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">Total Sensors:</span>
            <span class="info-value">{{ sensors.length }}</span>
          </div>
        </div>
      </div>

      <!-- View Toggle and Actions -->
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
        </div>

        <div class="bulk-actions">
          <button
            @click="showMeasurementsForSelected"
            class="btn btn-primary"
            :disabled="selectedSensors.length === 0"
          >
            Show Measurements for Selected ({{ selectedSensors.length }})
          </button>
          <button
            @click="showMeasurementsForAll"
            class="btn btn-secondary"
            :disabled="sensors.length === 0"
          >
            Show Measurements for All ({{ sensors.length }})
          </button>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="loading">
        <div class="spinner"></div>
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="error-message">
        {{ error }}
      </div>

      <!-- Table View -->
      <div v-else-if="view === 'table'" class="table-view">
        <div class="table-controls">
          <div class="info-text">
            Showing {{ sensors.length }} sensors
          </div>
        </div>

        <div class="table-wrapper">
          <table class="table">
            <thead>
              <tr>
                <th class="checkbox-col">
                  <input type="checkbox" @change="toggleSelectAll" :checked="allSelected" />
                </th>
                <th class="sensor-name-col">Sensor Name</th>
                <th v-for="field in displayFields" :key="field" class="field-col">{{ field }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="sensor in sensors" :key="sensor.timeseries_id">
                <td class="checkbox-col">
                  <input
                    type="checkbox"
                    :checked="isSelected(sensor.timeseries_id)"
                    @change="toggleSelection(sensor)"
                  />
                </td>
                <td class="sensor-name-cell">{{ sensor.sensor_name }}</td>
                <td v-for="field in displayFields" :key="field" class="field-cell">
                  <span class="cell-value">{{ formatCellValue(sensor[field]) }}</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Raw JSON View -->
      <div v-else-if="view === 'raw'" class="raw-view">
        <JsonViewer :data="{ sensors, total: sensors.length }" />
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useTimeseriesStore } from '../stores/timeseriesStore'
import { useSelectionStore } from '../stores/selectionStore'
import JsonViewer from '../components/JsonViewer.vue'

const props = defineProps({
  typeName: {
    type: String,
    required: true
  }
})

const router = useRouter()
const timeseriesStore = useTimeseriesStore()
const selectionStore = useSelectionStore()

const loading = ref(false)
const error = ref(null)
const currentType = ref(null)
const view = ref('table')
const selectedSensors = ref([])

const sensors = computed(() => {
  return currentType.value?.sensors || []
})

const allSelected = computed(() => {
  return sensors.value.length > 0 && selectedSensors.value.length === sensors.value.length
})

const displayFields = computed(() => {
  if (sensors.value.length === 0) return []

  // Get all keys from first sensor, excluding sensor_name (already shown)
  const firstSensor = sensors.value[0]
  const fields = Object.keys(firstSensor).filter(key => key !== 'sensor_name')

  return fields
})

onMounted(async () => {
  await loadType()
})

async function loadType() {
  loading.value = true
  error.value = null
  try {
    const result = await timeseriesStore.loadTypeByName(props.typeName)
    currentType.value = result
  } catch (err) {
    error.value = err.message
  } finally {
    loading.value = false
  }
}

function toggleSelection(sensor) {
  const index = selectedSensors.value.findIndex(s => s.timeseries_id === sensor.timeseries_id)
  if (index > -1) {
    selectedSensors.value.splice(index, 1)
  } else {
    selectedSensors.value.push(sensor)
  }
}

function toggleSelectAll() {
  if (allSelected.value) {
    selectedSensors.value = []
  } else {
    selectedSensors.value = [...sensors.value]
  }
}

function isSelected(timeseriesId) {
  return selectedSensors.value.some(s => s.timeseries_id === timeseriesId)
}

function showMeasurementsForSelected() {
  if (selectedSensors.value.length === 0) return

  // Store selections
  selectionStore.setSelectedSensors(selectedSensors.value, props.typeName)

  // Navigate to bulk measurements view
  router.push({
    path: '/bulk-measurements',
    query: {
      sensors: selectedSensors.value.map(s => s.sensor_name).join(','),
      type: props.typeName
    }
  })
}

function showMeasurementsForAll() {
  if (sensors.value.length === 0) return

  // Store all sensors
  selectionStore.setSelectedSensors(sensors.value, props.typeName)

  // Navigate to bulk measurements view
  router.push({
    path: '/bulk-measurements',
    query: {
      sensors: sensors.value.map(s => s.sensor_name).join(','),
      type: props.typeName
    }
  })
}

function formatCellValue(value) {
  if (value === null || value === undefined) return '-'
  if (typeof value === 'object') return JSON.stringify(value, null, 2)
  if (typeof value === 'boolean') return value ? '✓' : '✗'
  return String(value)
}
</script>

<style scoped>
.timeseries-inspector {
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

.type-info {
  margin-bottom: 2rem;
  padding: 1.5rem;
}

.type-info h3 {
  margin-bottom: 1rem;
}

.info-grid {
  gap: 1rem;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.info-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.info-value {
  font-size: 1rem;
  color: var(--text-primary);
}

.controls {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.bulk-actions {
  display: flex;
  gap: 0.75rem;
  align-items: center;
}

.loading {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 3rem;
}

.error-message {
  padding: 2rem;
  text-align: center;
  color: var(--danger-color);
  background: rgba(239, 68, 68, 0.1);
  border-radius: 0.5rem;
}

.table-controls {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.info-text {
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.table-wrapper {
  overflow-x: auto;
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  margin-bottom: 1rem;
  max-width: 100%;
}

.table {
  min-width: max-content;
  width: 100%;
}

.checkbox-col {
  width: 40px;
  min-width: 40px;
  position: sticky;
  left: 0;
  background: var(--surface-color);
  z-index: 1;
}

.sensor-name-col {
  min-width: 200px;
  position: sticky;
  left: 40px;
  background: var(--surface-color);
  z-index: 1;
  border-right: 2px solid var(--border-color);
}

.sensor-name-cell {
  font-family: monospace;
  font-size: 0.875rem;
  white-space: pre-wrap;
  word-break: break-word;
}

.field-col {
  min-width: 150px;
  white-space: pre-wrap;
}

.field-cell {
  white-space: pre-wrap;
  word-break: break-word;
  vertical-align: top;
}

.cell-value {
  display: block;
  white-space: pre-wrap;
  word-break: break-word;
}

.raw-view {
  margin-top: 1rem;
}

.btn-sm {
  padding: 0.375rem 0.75rem;
  font-size: 0.875rem;
}
</style>
