<template>
  <div class="dataset-stats">
    <h3>Dataset Statistics</h3>

    <div class="stat-grid grid grid-4">
      <div class="stat-card card">
        <div class="stat-label">Total Entries</div>
        <div class="stat-value">{{ formatNumber(totalEntries) }}</div>
      </div>

      <div class="stat-card card">
        <div class="stat-label">Total Fields</div>
        <div class="stat-value">{{ analysis?.fields?.length || 0 }}</div>
      </div>

      <div class="stat-card card">
        <div class="stat-label">Avg Completeness</div>
        <div class="stat-value">{{ avgCompleteness }}%</div>
      </div>

      <div v-if="timeseriesAnalysis" class="stat-card card">
        <div class="stat-label">Timeseries Attachment</div>
        <div class="stat-value">{{ timeseriesAnalysis.attachmentRate }}%</div>
      </div>
    </div>

    <div v-if="timeseriesAnalysis && timeseriesAnalysis.timeseriesTypes.length > 0" class="timeseries-info">
      <h4>Available Timeseries Types</h4>
      <div class="type-list">
        <span
          v-for="type in timeseriesAnalysis.timeseriesTypes"
          :key="type"
          class="badge badge-primary"
        >
          {{ type }} ({{ timeseriesAnalysis.typeFrequency[type] }})
        </span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  analysis: {
    type: Object,
    default: null
  },
  totalEntries: {
    type: Number,
    required: true
  },
  timeseriesAnalysis: {
    type: Object,
    default: null
  }
})

const avgCompleteness = computed(() => {
  if (!props.analysis?.fields || props.analysis.fields.length === 0) return 0

  const sum = props.analysis.fields.reduce((acc, field) => acc + field.completeness, 0)
  return (sum / props.analysis.fields.length).toFixed(2)
})

function formatNumber(num) {
  return new Intl.NumberFormat().format(num)
}

</script>

<style scoped>
.dataset-stats {
  background: var(--surface-color);
  border-radius: 0.5rem;
  padding: 1.5rem;
  border: 1px solid var(--border-color);
}

.dataset-stats h3 {
  margin-bottom: 1rem;
  font-size: 1.25rem;
  color: var(--text-primary);
}

.dataset-stats h4 {
  margin: 1.5rem 0 1rem;
  font-size: 1rem;
  color: var(--text-primary);
}

.stat-grid {
  margin-bottom: 1.5rem;
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

.type-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}
</style>
