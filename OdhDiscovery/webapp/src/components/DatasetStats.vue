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

    <div v-if="analysis && analysis.fields.length > 0" class="field-analysis">
      <h4>Field Completeness</h4>
      <div class="field-list">
        <div
          v-for="field in topFields"
          :key="field.path"
          class="field-item"
        >
          <div class="field-header">
            <span class="field-path">{{ field.path }}</span>
            <span
              class="field-completeness"
              :class="getCompletenessClass(field.completeness)"
            >
              {{ field.completeness }}%
            </span>
          </div>
          <div class="field-meta">
            <span class="field-type">{{ field.types.join(', ') }}</span>
            <span v-if="field.examples.length > 0" class="field-examples">
              Examples: {{ formatExamples(field.examples) }}
            </span>
          </div>
          <div class="progress-bar">
            <div
              class="progress-fill"
              :style="{ width: `${field.completeness}%` }"
              :class="getCompletenessClass(field.completeness)"
            ></div>
          </div>
        </div>
      </div>

      <button
        v-if="analysis.fields.length > 20"
        @click="showAllFields = !showAllFields"
        class="btn btn-outline"
      >
        {{ showAllFields ? 'Show Less' : `Show All ${analysis.fields.length} Fields` }}
      </button>
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
import { ref, computed } from 'vue'
import { formatValue } from '../utils/dataAnalyzer'

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

const showAllFields = ref(false)

const topFields = computed(() => {
  if (!props.analysis?.fields) return []

  const fields = props.analysis.fields
    .filter(f => !f.path.includes('[]')) // Filter out array item fields for cleaner view
    .sort((a, b) => b.completeness - a.completeness)

  return showAllFields.value ? fields : fields.slice(0, 20)
})

const avgCompleteness = computed(() => {
  if (!props.analysis?.fields || props.analysis.fields.length === 0) return 0

  const sum = props.analysis.fields.reduce((acc, field) => acc + field.completeness, 0)
  return (sum / props.analysis.fields.length).toFixed(2)
})

function formatNumber(num) {
  return new Intl.NumberFormat().format(num)
}

function getCompletenessClass(completeness) {
  if (completeness >= 80) return 'high'
  if (completeness >= 50) return 'medium'
  return 'low'
}

function formatExamples(examples) {
  return examples
    .slice(0, 2)
    .map(ex => formatValue(ex))
    .join(', ')
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

.field-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-bottom: 1rem;
}

.field-item {
  padding: 0.75rem;
  background: var(--bg-color);
  border-radius: 0.375rem;
}

.field-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
}

.field-path {
  font-family: 'Monaco', 'Menlo', monospace;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-primary);
}

.field-completeness {
  font-size: 0.875rem;
  font-weight: 600;
  padding: 0.25rem 0.5rem;
  border-radius: 0.25rem;
}

.field-completeness.high {
  background: rgba(16, 185, 129, 0.1);
  color: var(--success-color);
}

.field-completeness.medium {
  background: rgba(245, 158, 11, 0.1);
  color: var(--warning-color);
}

.field-completeness.low {
  background: rgba(239, 68, 68, 0.1);
  color: var(--danger-color);
}

.field-meta {
  display: flex;
  gap: 1rem;
  font-size: 0.75rem;
  color: var(--text-secondary);
  margin-bottom: 0.5rem;
}

.field-type {
  font-weight: 600;
}

.progress-bar {
  height: 4px;
  background: var(--border-color);
  border-radius: 2px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  transition: width 0.3s ease;
}

.progress-fill.high {
  background: var(--success-color);
}

.progress-fill.medium {
  background: var(--warning-color);
}

.progress-fill.low {
  background: var(--danger-color);
}

.type-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}
</style>
