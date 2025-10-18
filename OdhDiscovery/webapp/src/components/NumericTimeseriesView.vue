<template>
  <div class="numeric-timeseries-view">
    <div class="view-toggle">
      <button
        @click="viewMode = 'chart'"
        class="btn btn-sm"
        :class="{ 'btn-primary': viewMode === 'chart', 'btn-outline': viewMode !== 'chart' }"
      >
        Chart
      </button>
      <button
        @click="viewMode = 'table'"
        class="btn btn-sm"
        :class="{ 'btn-primary': viewMode === 'table', 'btn-outline': viewMode !== 'table' }"
      >
        Table
      </button>
      <button
        v-if="viewMode === 'chart'"
        @click="showLegend = !showLegend"
        class="btn btn-sm"
        :class="{ 'btn-primary': showLegend, 'btn-outline': !showLegend }"
      >
        Legend
      </button>
    </div>

    <!-- Chart View -->
    <div v-if="viewMode === 'chart'" class="chart-view">
      <div class="chart-container" :style="{ height: chartContainerHeight }">
        <Line :data="chartData" :options="chartOptions" />
      </div>
    </div>

    <!-- Table View -->
    <div v-else class="table-view">
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
              <td class="value-cell">{{ formatNumericValue(m.value) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { Line } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  TimeScale
} from 'chart.js'
import 'chartjs-adapter-date-fns'

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  TimeScale
)

const props = defineProps({
  measurements: {
    type: Array,
    required: true
  }
})

const viewMode = ref('chart')
const showLegend = ref(false)

const sortedMeasurements = computed(() => {
  return [...props.measurements].sort((a, b) => {
    return new Date(a.timestamp) - new Date(b.timestamp)
  })
})

const uniqueSensors = computed(() => {
  return new Set(props.measurements.map(m => m.sensor_name)).size
})

const sensorList = computed(() => {
  const sensors = Array.from(new Set(props.measurements.map(m => m.sensor_name)))
  if (sensors.length > 3) {
    return sensors.slice(0, 3).join(', ') + ` and ${sensors.length - 3} more`
  }
  return sensors.join(', ')
})

const chartContainerHeight = computed(() => {
  const baseHeight = 400
  if (!showLegend.value) {
    return `${baseHeight}px`
  }

  // Calculate additional height needed for legend
  // Estimate ~30px per row, with approximately 3-4 items per row
  const sensorCount = uniqueSensors.value
  const estimatedRows = Math.ceil(sensorCount / 3)
  const legendHeight = 40 + (estimatedRows * 30) // 40px base padding + rows

  return `${baseHeight + legendHeight}px`
})

// Group measurements by sensor
const chartData = computed(() => {
  const sensorGroups = {}

  sortedMeasurements.value.forEach(m => {
    const sensor = m.sensor_name
    if (!sensorGroups[sensor]) {
      sensorGroups[sensor] = []
    }

    const value = typeof m.value === 'number' ? m.value : parseFloat(m.value)
    if (!isNaN(value)) {
      sensorGroups[sensor].push({
        x: new Date(m.timestamp),
        y: value
      })
    }
  })

  // Generate colors for each sensor
  const colors = [
    'rgb(59, 130, 246)',   // blue
    'rgb(16, 185, 129)',   // green
    'rgb(245, 158, 11)',   // amber
    'rgb(239, 68, 68)',    // red
    'rgb(168, 85, 247)',   // purple
    'rgb(236, 72, 153)',   // pink
    'rgb(14, 165, 233)',   // sky
    'rgb(34, 197, 94)',    // emerald
  ]

  const datasets = Object.entries(sensorGroups).map(([sensor, data], index) => ({
    label: sensor,
    data: data,
    borderColor: colors[index % colors.length],
    backgroundColor: colors[index % colors.length].replace('rgb', 'rgba').replace(')', ', 0.1)'),
    borderWidth: 2,
    pointRadius: 3,
    pointHoverRadius: 5,
    tension: 0.1
  }))

  return {
    datasets
  }
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  interaction: {
    mode: 'index',
    intersect: false
  },
  plugins: {
    legend: showLegend.value ? {
      position: 'top',
      labels: {
        usePointStyle: true,
        padding: 15,
        font: {
          size: 12
        }
      }
    } : false,
    title: {
      display: true,
      text: `Time Series (${props.measurements.length} measurements from ${uniqueSensors.value} sensor(s))`,
      font: {
        size: 14,
        weight: 'normal'
      }
    },
    tooltip: {
      callbacks: {
        label: function(context) {
          let label = context.dataset.label || ''
          if (label) {
            label += ': '
          }
          if (context.parsed.y !== null) {
            label += context.parsed.y.toFixed(2)
          }
          return label
        }
      }
    }
  },
  scales: {
    x: {
      type: 'time',
      time: {
        displayFormats: {
          millisecond: 'HH:mm:ss.SSS',
          second: 'HH:mm:ss',
          minute: 'HH:mm',
          hour: 'HH:mm',
          day: 'MMM dd',
          week: 'MMM dd',
          month: 'MMM yyyy',
          quarter: 'MMM yyyy',
          year: 'yyyy'
        }
      },
      title: {
        display: true,
        text: 'Time'
      },
      grid: {
        display: true,
        color: 'rgba(0, 0, 0, 0.05)'
      }
    },
    y: {
      title: {
        display: true,
        text: 'Value'
      },
      grid: {
        display: true,
        color: 'rgba(0, 0, 0, 0.05)'
      }
    }
  }
}))

function formatTimestamp(timestamp) {
  if (!timestamp) return '-'
  return new Date(timestamp).toLocaleString()
}

function formatNumericValue(value) {
  if (value === null || value === undefined) return '-'
  const num = typeof value === 'number' ? value : parseFloat(value)
  return isNaN(num) ? String(value) : num.toFixed(2)
}
</script>

<style scoped>
.numeric-timeseries-view {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.view-toggle {
  display: flex;
  gap: 0.5rem;
}

.chart-container {
  padding: 1rem;
  background: var(--bg-color);
  border-radius: 0.375rem;
  border: 1px solid var(--border-color);
}

.text-secondary {
  color: var(--text-secondary);
  margin-top: 0.5rem;
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
  font-weight: 600;
  color: var(--primary-color);
  text-align: right;
}
</style>
