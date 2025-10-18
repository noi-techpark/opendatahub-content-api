<template>
  <div class="timeseries-analyzer">
    <div class="analyzer-header card">
      <h3>Timeseries Analysis</h3>
      <p>Explore timeseries attached to dataset entities. Each type shows all sensors with that measurement type.</p>

      <div v-if="!timeseriesAnalysis || timeseriesAnalysis.sensorNames.length === 0" class="no-timeseries">
        <p class="info-text">No timeseries sensors detected in this dataset.</p>
        <p class="help-text">
          Timeseries are detected by calling the timeseries API with all dataset entry IDs.
        </p>
      </div>

      <div v-else class="timeseries-stats">
        <div class="stat-grid grid grid-3">
          <div class="stat-card card">
            <div class="stat-label">Attachment Rate</div>
            <div class="stat-value">{{ timeseriesAnalysis.attachmentRate }}%</div>
          </div>

          <div class="stat-card card">
            <div class="stat-label">Unique Sensors</div>
            <div class="stat-value">{{ timeseriesAnalysis.sensorNames.length }}</div>
          </div>

          <div class="stat-card card">
            <div class="stat-label">Timeseries Types</div>
            <div class="stat-value">{{ timeseriesAnalysis.timeseriesTypes?.length || 0 }}</div>
          </div>
        </div>
      </div>
    </div>

    <div v-if="timeseriesAnalysis && timeseriesAnalysis.timeseriesTypes.length > 0" class="timeseries-content">
      <!-- Actions Bar -->
      <div class="actions-bar card">
        <div class="selection-info">
          <span class="selection-count">{{ selectedTypes.size }} type(s) selected</span>
          <div class="selection-actions">
            <button @click="selectAllTypes" class="btn btn-sm btn-outline">Select All</button>
            <button @click="clearSelection" class="btn btn-sm btn-outline">Clear</button>
          </div>
        </div>
        <button
          @click="openSelectedTypes"
          class="btn btn-primary"
          :disabled="selectedTypes.size === 0"
        >
          Open {{ selectedTypes.size > 0 ? selectedTypes.size : '' }} Selected Type{{ selectedTypes.size !== 1 ? 's' : '' }} in Inspector
        </button>
      </div>

      <!-- Type Sections (Accordion) -->
      <div
        v-for="typeName in timeseriesAnalysis.timeseriesTypes"
        :key="typeName"
        class="type-section card"
      >
        <div class="type-section-header" @click="toggleExpand(typeName)">
          <div class="header-left">
            <input
              type="checkbox"
              :checked="selectedTypes.has(typeName)"
              @click.stop
              @change="toggleTypeSelection(typeName)"
              class="type-checkbox"
            />
            <span class="expand-icon">{{ expandedTypes.has(typeName) ? '▼' : '▶' }}</span>
            <div class="type-info">
              <h4>{{ typeName }}</h4>
              <div class="type-meta">
                <span class="meta-badge" v-if="getTypeInfo(typeName)?.data_type">
                  {{ getTypeInfo(typeName).data_type }}
                </span>
                <span class="meta-badge" v-if="getTypeInfo(typeName)?.unit">
                  {{ getTypeInfo(typeName).unit }}
                </span>
                <span class="sensor-count-badge">
                  {{ getSensorsForType(typeName).length }} sensors
                </span>
              </div>
              <p class="type-description" v-if="getTypeInfo(typeName)?.description && expandedTypes.has(typeName)">
                {{ getTypeInfo(typeName).description }}
              </p>
            </div>
          </div>
        </div>

        <div v-if="expandedTypes.has(typeName)" class="sensors-table-wrapper">
          <table class="sensors-table">
            <thead>
              <tr>
                <th>Sensor Name</th>
                <th>Timeseries ID</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="sensor in getSensorsForType(typeName)"
                :key="sensor.timeseries_id"
              >
                <td class="sensor-name-cell">{{ sensor.sensor_name }}</td>
                <td class="timeseries-id-cell">{{ sensor.timeseries_id }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

const props = defineProps({
  timeseriesAnalysis: {
    type: Object,
    default: null
  },
  datasetName: {
    type: String,
    required: true
  },
  currentFilters: {
    type: Object,
    default: () => ({})
  },
  totalEntries: {
    type: Number,
    default: 0
  }
})

const selectedTypes = ref(new Set())
const expandedTypes = ref(new Set())

function getSensorsForType(typeName) {
  if (!props.timeseriesAnalysis?.timeseriesData) return []

  const sensors = []

  // Iterate through all sensors and find those with this type
  Object.entries(props.timeseriesAnalysis.timeseriesData).forEach(([sensorName, timeseries]) => {
    timeseries.forEach(ts => {
      if (ts.type_name === typeName) {
        sensors.push({
          sensor_name: sensorName,
          timeseries_id: ts.timeseries_id,
          type_info: ts.type_info
        })
      }
    })
  })

  return sensors
}

function getTypeInfo(typeName) {
  if (!props.timeseriesAnalysis?.timeseriesData) return null

  // Find the first occurrence of this type to get its info
  for (const sensorData of Object.values(props.timeseriesAnalysis.timeseriesData)) {
    const typeData = sensorData.find(t => t.type_name === typeName)
    if (typeData) {
      return typeData.type_info
    }
  }

  return null
}

function toggleTypeSelection(typeName) {
  if (selectedTypes.value.has(typeName)) {
    selectedTypes.value.delete(typeName)
  } else {
    selectedTypes.value.add(typeName)
  }
  // Trigger reactivity
  selectedTypes.value = new Set(selectedTypes.value)
}

function toggleExpand(typeName) {
  if (expandedTypes.value.has(typeName)) {
    expandedTypes.value.delete(typeName)
  } else {
    expandedTypes.value.add(typeName)
  }
  // Trigger reactivity
  expandedTypes.value = new Set(expandedTypes.value)
}

function selectAllTypes() {
  if (!props.timeseriesAnalysis?.timeseriesTypes) return
  selectedTypes.value = new Set(props.timeseriesAnalysis.timeseriesTypes)
}

function clearSelection() {
  selectedTypes.value = new Set()
}

function openSelectedTypes() {
  if (selectedTypes.value.size === 0) return

  // Get all unique sensors (entry IDs) across all selected types
  const allEntryIds = new Set()
  const typeNames = Array.from(selectedTypes.value)

  typeNames.forEach(typeName => {
    const sensors = getSensorsForType(typeName)
    // Sensor names are the entry IDs
    sensors.forEach(s => allEntryIds.add(s.sensor_name))
  })

  // Navigate to bulk measurements inspector with entry IDs
  router.push({
    path: '/bulk-measurements',
    query: {
      sensors: Array.from(allEntryIds).join(','),
      types: typeNames.join(','),
    }
  })
}
</script>

<style scoped>
.timeseries-analyzer {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.analyzer-header {
  padding: 1.5rem;
}

.analyzer-header h3 {
  margin-bottom: 0.5rem;
}

.analyzer-header p {
  color: var(--text-secondary);
  margin-bottom: 1.5rem;
}

.no-timeseries {
  padding: 2rem;
  text-align: center;
  background: var(--bg-color);
  border-radius: 0.375rem;
}

.info-text {
  font-size: 1rem;
  color: var(--text-secondary);
  margin-bottom: 1rem;
}

.help-text {
  font-size: 0.875rem;
  color: var(--text-secondary);
  line-height: 1.6;
}

.timeseries-stats {
  margin-top: 1rem;
}

.stat-grid {
  display: grid;
  gap: 1rem;
}

.stat-card {
  text-align: center;
  padding: 1rem;
}

.stat-label {
  font-size: 0.875rem;
  color: var(--text-secondary);
  margin-bottom: 0.5rem;
}

.stat-value {
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--primary-color);
}

.timeseries-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.actions-bar {
  padding: 1rem 1.5rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  background: var(--surface-color);
  border: 2px solid var(--primary-color);
}

.selection-info {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.selection-count {
  font-weight: 600;
  color: var(--text-primary);
}

.selection-actions {
  display: flex;
  gap: 0.5rem;
}

.type-section {
  padding: 0;
  overflow: hidden;
}

.type-section-header {
  padding: 1rem 1.5rem;
  cursor: pointer;
  transition: background-color 0.2s;
  user-select: none;
}

.type-section-header:hover {
  background: var(--bg-color);
}

.header-left {
  display: flex;
  align-items: flex-start;
  gap: 1rem;
  width: 100%;
}

.type-checkbox {
  flex-shrink: 0;
  width: 18px;
  height: 18px;
  margin-top: 0.25rem;
  cursor: pointer;
}

.expand-icon {
  flex-shrink: 0;
  font-size: 0.875rem;
  color: var(--text-secondary);
  width: 20px;
  margin-top: 0.25rem;
}

.type-info {
  flex: 1;
}

.type-info h4 {
  margin: 0 0 0.75rem 0;
  font-size: 1.25rem;
  font-family: monospace;
  color: var(--primary-color);
}

.type-meta {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
  flex-wrap: wrap;
}

.meta-badge {
  font-size: 0.75rem;
  padding: 0.25rem 0.75rem;
  background: var(--surface-color);
  border-radius: 0.25rem;
  color: var(--text-secondary);
  border: 1px solid var(--border-color);
}

.sensor-count-badge {
  font-size: 0.75rem;
  padding: 0.25rem 0.75rem;
  background: var(--primary-color);
  color: white;
  border-radius: 0.25rem;
  font-weight: 600;
}

.type-description {
  font-size: 0.875rem;
  color: var(--text-secondary);
  margin: 0;
  line-height: 1.6;
}

.sensors-table-wrapper {
  overflow-x: auto;
  border-top: 1px solid var(--border-color);
  margin: 0;
}

.sensors-table {
  width: 100%;
  border-collapse: collapse;
}

.sensors-table thead {
  background: var(--bg-color);
  border-bottom: 2px solid var(--border-color);
}

.sensors-table th {
  padding: 0.75rem 1rem;
  text-align: left;
  font-weight: 600;
  font-size: 0.875rem;
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.sensors-table tbody tr {
  border-bottom: 1px solid var(--border-color);
  transition: background-color 0.2s;
}

.sensors-table tbody tr:hover {
  background: var(--bg-color);
}

.sensors-table tbody tr:last-child {
  border-bottom: none;
}

.sensors-table td {
  padding: 0.75rem 1rem;
  font-size: 0.875rem;
}

.sensor-name-cell {
  font-family: monospace;
  font-weight: 600;
  color: var(--text-primary);
}

.timeseries-id-cell {
  font-family: monospace;
  font-size: 0.75rem;
  color: var(--text-secondary);
}
</style>
