<template>
  <div class="string-boolean-timeseries-view">
    <div class="table-wrapper">
      <table class="table table-sm">
        <thead>
          <tr>
            <th>Sensor</th>
            <th>Timestamp</th>
            <th>Value</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(m, idx) in sortedMeasurements" :key="idx">
            <td class="sensor-cell">{{ m.sensor_name }}</td>
            <td class="timestamp-cell">{{ formatTimestamp(m.timestamp) }}</td>
            <td class="value-cell">{{ formatValue(m.value) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
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

function formatTimestamp(timestamp) {
  if (!timestamp) return '-'
  return new Date(timestamp).toLocaleString()
}

function formatValue(value) {
  if (value === null || value === undefined) return '-'
  if (typeof value === 'boolean') return value ? '✓' : '✗'
  return String(value)
}
</script>

<style scoped>
.string-boolean-timeseries-view {
  display: flex;
  flex-direction: column;
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

.value-cell {
  font-weight: 500;
}
</style>
