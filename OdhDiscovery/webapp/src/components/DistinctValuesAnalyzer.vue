<template>
  <div class="distinct-values-analyzer">
    <div class="analyzer-header card">
      <h3>Distinct Values Analysis</h3>
      <p>Select one or more properties to analyze their distinct values across all filtered entries.</p>

      <div class="property-selector">
        <div class="selector-header">
          <label>Select Properties ({{ selectedProperties.length }} selected)</label>
          <div class="selector-actions">
            <button @click="selectAll" class="btn btn-outline btn-sm">Select All</button>
            <button @click="clearSelection" class="btn btn-outline btn-sm">Clear</button>
          </div>
        </div>

        <div class="property-list">
          <div
            v-for="field in leafFields"
            :key="field.path"
            class="property-item"
          >
            <label class="property-label">
              <input
                type="checkbox"
                :value="field.path"
                v-model="selectedProperties"
              />
              <span class="property-name">{{ field.path }}</span>
              <span class="property-type">{{ field.types.join(', ') }}</span>
              <span class="property-completeness" :class="getCompletenessClass(field.completeness)">
                {{ field.completeness }}%
              </span>
            </label>
          </div>
        </div>
      </div>

      <div class="analyzer-actions">
        <button
          @click="analyzeDistinctValues"
          class="btn btn-primary"
          :disabled="selectedProperties.length === 0 || analyzing"
        >
          {{ analyzing ? 'Analyzing...' : 'Analyze Distinct Values' }}
        </button>
      </div>
    </div>

    <div v-if="analyzing" class="loading-state">
      <div class="spinner"></div>
      <p>Fetching and analyzing {{ totalEntries }} entries...</p>
      <p class="progress-text">{{ analysisProgress }}</p>
    </div>

    <div v-if="error" class="error-message">
      {{ error }}
    </div>

    <div v-if="analysisResults" class="results-section">
      <div class="results-header">
        <h3>Analysis Results</h3>
        <div class="results-actions">
          <button @click="exportToCsv" class="btn btn-outline btn-sm">Export to CSV</button>
          <button @click="clearResults" class="btn btn-outline btn-sm">Clear Results</button>
        </div>
      </div>

      <div class="results-grid">
        <div
          v-for="(result, property) in analysisResults"
          :key="property"
          class="result-card card"
        >
          <div class="result-header">
            <h4>{{ property }}</h4>
            <span class="distinct-count">{{ result.distinctCount }} distinct values</span>
          </div>

          <div class="result-stats">
            <div class="stat-item">
              <span class="stat-label">Total Values:</span>
              <span class="stat-value">{{ result.totalCount }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">Null/Undefined:</span>
              <span class="stat-value">{{ result.nullCount }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">Fill Rate:</span>
              <span class="stat-value">{{ result.fillRate }}%</span>
            </div>
          </div>

          <div class="value-search">
            <input
              v-model="result.searchQuery"
              type="text"
              class="input input-sm"
              :placeholder="`Search in ${result.distinctCount} values...`"
            />
          </div>

          <div class="value-list">
            <div class="value-list-header">
              <span>Value</span>
              <span>Count</span>
              <span>Percentage</span>
            </div>
            <div class="value-list-items">
              <div
                v-for="(item, index) in getPaginatedValues(result)"
                :key="index"
                class="value-item"
              >
                <span class="value-text" :title="formatValue(item.value)">
                  {{ formatValue(item.value) }}
                </span>
                <span class="value-count">{{ item.count }}</span>
                <span class="value-percentage">{{ item.percentage }}%</span>
                <div class="value-bar" :style="{ width: `${item.percentage}%` }"></div>
              </div>
            </div>

            <div class="pagination-controls" v-if="shouldShowPagination(result)">
              <div class="pagination-info">
                Showing {{ getPaginationStart(result) + 1 }}-{{ getPaginationEnd(result) }} of {{ getFilteredValuesCount(result) }} values
              </div>
              <div class="pagination-buttons">
                <button
                  @click="previousPage(result)"
                  :disabled="result.currentPage === 0"
                  class="btn btn-sm btn-outline"
                >
                  ← Previous
                </button>
                <span class="page-indicator">
                  Page {{ result.currentPage + 1 }} of {{ getTotalPages(result) }}
                </span>
                <button
                  @click="nextPage(result)"
                  :disabled="result.currentPage >= getTotalPages(result) - 1"
                  class="btn btn-sm btn-outline"
                >
                  Next →
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { getValueAtPath } from '../utils/dataAnalyzer'

const props = defineProps({
  fields: {
    type: Array,
    required: true
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

const emit = defineEmits(['fetch-all-data'])

const selectedProperties = ref([])
const analyzing = ref(false)
const analysisResults = ref(null)
const error = ref(null)
const analysisProgress = ref('')

const PAGE_SIZE = 1000

// Watch for search query changes and reset pagination
watch(
  () => analysisResults.value,
  (newResults) => {
    if (newResults) {
      for (const result of Object.values(newResults)) {
        watch(
          () => result.searchQuery,
          () => {
            result.currentPage = 0
          }
        )
      }
    }
  },
  { immediate: true, deep: true }
)

// Filter fields for distinct value analysis
// Exclude array item fields (those with []) but allow array fields themselves
// Arrays will be stringified for comparison
const leafFields = computed(() => {
  return props.fields.filter(field => {
    // Exclude array item fields (e.g., "ApiFilter[]", "items[].name")
    // but allow array fields themselves (e.g., "ApiFilter", "Sources")
    if (field.path.includes('[]')) return false

    // Allow all other fields (primitives, arrays, objects)
    // Non-primitive values will be stringified for comparison
    return true
  }).sort((a, b) => b.completeness - a.completeness)
})

function selectAll() {
  selectedProperties.value = leafFields.value.map(f => f.path)
}

function clearSelection() {
  selectedProperties.value = []
}

async function analyzeDistinctValues() {
  if (selectedProperties.value.length === 0) return

  try {
    analyzing.value = true
    error.value = null
    analysisProgress.value = 'Fetching data...'

    // Emit event to parent to fetch all data
    const allData = await new Promise((resolve, reject) => {
      const handler = (data) => {
        resolve(data)
      }
      emit('fetch-all-data', handler)
    })

    if (!allData || allData.length === 0) {
      error.value = 'No data available for analysis'
      return
    }

    analysisProgress.value = 'Computing distinct values...'

    // Compute distinct values for each selected property
    const results = {}

    for (const property of selectedProperties.value) {
      const valueMap = new Map()
      let nullCount = 0
      let totalCount = allData.length

      for (const entry of allData) {
        const value = getValueAtPath(entry, property)

        if (value === null || value === undefined) {
          nullCount++
        } else {
          const key = JSON.stringify(value)
          valueMap.set(key, (valueMap.get(key) || 0) + 1)
        }
      }

      // Convert to array and sort by count
      const values = Array.from(valueMap.entries())
        .map(([key, count]) => ({
          value: JSON.parse(key),
          count,
          percentage: ((count / totalCount) * 100).toFixed(2)
        }))
        .sort((a, b) => b.count - a.count)

      results[property] = {
        distinctCount: valueMap.size,
        totalCount,
        nullCount,
        fillRate: (((totalCount - nullCount) / totalCount) * 100).toFixed(2),
        values,
        searchQuery: '',
        currentPage: 0
      }
    }

    analysisResults.value = results
    analysisProgress.value = 'Analysis complete!'
  } catch (err) {
    error.value = `Analysis failed: ${err.message}`
  } finally {
    analyzing.value = false
  }
}

function getFilteredValues(result) {
  if (!result.searchQuery) {
    return result.values
  }

  const query = result.searchQuery.toLowerCase()
  return result.values.filter(item => {
    const valueStr = formatValue(item.value).toLowerCase()
    return valueStr.includes(query)
  })
}

function getFilteredValuesCount(result) {
  return getFilteredValues(result).length
}

function getPaginatedValues(result) {
  const filtered = getFilteredValues(result)
  const start = result.currentPage * PAGE_SIZE
  const end = start + PAGE_SIZE
  return filtered.slice(start, end)
}

function getTotalPages(result) {
  const filtered = getFilteredValues(result)
  return Math.ceil(filtered.length / PAGE_SIZE)
}

function shouldShowPagination(result) {
  return getFilteredValuesCount(result) > PAGE_SIZE
}

function getPaginationStart(result) {
  return result.currentPage * PAGE_SIZE
}

function getPaginationEnd(result) {
  const filtered = getFilteredValues(result)
  return Math.min((result.currentPage + 1) * PAGE_SIZE, filtered.length)
}

function nextPage(result) {
  if (result.currentPage < getTotalPages(result) - 1) {
    result.currentPage++
  }
}

function previousPage(result) {
  if (result.currentPage > 0) {
    result.currentPage--
  }
}

function formatValue(value) {
  if (value === null) return 'null'
  if (value === undefined) return 'undefined'
  if (typeof value === 'boolean') return value ? 'true' : 'false'
  if (typeof value === 'object') return JSON.stringify(value)
  return String(value)
}

function getCompletenessClass(completeness) {
  if (completeness >= 80) return 'high'
  if (completeness >= 50) return 'medium'
  return 'low'
}

function exportToCsv() {
  if (!analysisResults.value) return

  let csv = 'Property,Value,Count,Percentage\n'

  for (const [property, result] of Object.entries(analysisResults.value)) {
    for (const item of result.values) {
      const value = formatValue(item.value).replace(/"/g, '""')
      csv += `"${property}","${value}",${item.count},${item.percentage}\n`
    }
  }

  const blob = new Blob([csv], { type: 'text/csv' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `distinct-values-${props.datasetName}-${Date.now()}.csv`
  a.click()
  URL.revokeObjectURL(url)
}

function clearResults() {
  analysisResults.value = null
  selectedProperties.value = []
}
</script>

<style scoped>
.distinct-values-analyzer {
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

.property-selector {
  margin-bottom: 1.5rem;
}

.selector-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.selector-header label {
  font-weight: 600;
  color: var(--text-primary);
}

.selector-actions {
  display: flex;
  gap: 0.5rem;
}

.property-list {
  max-height: 300px;
  overflow-y: auto;
  border: 1px solid var(--border-color);
  border-radius: 0.375rem;
  padding: 0.5rem;
  background: var(--bg-color);
}

.property-item {
  padding: 0.5rem;
  border-bottom: 1px solid var(--border-color);
}

.property-item:last-child {
  border-bottom: none;
}

.property-label {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  cursor: pointer;
  font-weight: normal;
}

.property-name {
  flex: 1;
  font-family: 'Monaco', 'Menlo', monospace;
  font-size: 0.875rem;
}

.property-type {
  font-size: 0.75rem;
  color: var(--text-secondary);
  padding: 0.125rem 0.5rem;
  background: var(--surface-color);
  border-radius: 0.25rem;
}

.property-completeness {
  font-size: 0.75rem;
  padding: 0.125rem 0.5rem;
  border-radius: 0.25rem;
  font-weight: 600;
}

.property-completeness.high {
  background: rgba(16, 185, 129, 0.1);
  color: var(--success-color);
}

.property-completeness.medium {
  background: rgba(245, 158, 11, 0.1);
  color: var(--warning-color);
}

.property-completeness.low {
  background: rgba(239, 68, 68, 0.1);
  color: var(--danger-color);
}

.analyzer-actions {
  display: flex;
  gap: 1rem;
}

.loading-state {
  text-align: center;
  padding: 3rem;
}

.loading-state .spinner {
  margin: 0 auto 1rem;
}

.progress-text {
  color: var(--text-secondary);
  font-size: 0.875rem;
  margin-top: 0.5rem;
}

.results-section {
  margin-top: 1.5rem;
}

.results-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.results-header h3 {
  margin: 0;
}

.results-actions {
  display: flex;
  gap: 0.5rem;
}

.results-grid {
  display: grid;
  gap: 1.5rem;
}

.result-card {
  padding: 1.5rem;
}

.result-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--border-color);
}

.result-header h4 {
  margin: 0;
  font-family: 'Monaco', 'Menlo', monospace;
  font-size: 1rem;
}

.distinct-count {
  font-size: 0.875rem;
  color: var(--primary-color);
  font-weight: 600;
}

.result-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 1rem;
  margin-bottom: 1rem;
  padding: 1rem;
  background: var(--bg-color);
  border-radius: 0.375rem;
}

.stat-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.stat-label {
  font-size: 0.75rem;
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.stat-value {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--text-primary);
}

.value-search {
  margin-bottom: 1rem;
}

.input-sm {
  padding: 0.5rem;
  font-size: 0.875rem;
}

.value-list {
  border: 1px solid var(--border-color);
  border-radius: 0.375rem;
  overflow: hidden;
}

.value-list-header {
  display: grid;
  grid-template-columns: 1fr 100px 100px;
  gap: 1rem;
  padding: 0.75rem 1rem;
  background: var(--bg-color);
  border-bottom: 1px solid var(--border-color);
  font-weight: 600;
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.value-list-items {
  max-height: 400px;
  overflow-y: auto;
}

.value-item {
  display: grid;
  grid-template-columns: 1fr 100px 100px;
  gap: 1rem;
  padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--border-color);
  position: relative;
  align-items: center;
}

.value-item:last-child {
  border-bottom: none;
}

.value-item:hover {
  background: var(--bg-color);
}

.value-text {
  font-family: 'Monaco', 'Menlo', monospace;
  font-size: 0.875rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.value-count {
  text-align: right;
  font-weight: 600;
}

.value-percentage {
  text-align: right;
  color: var(--text-secondary);
}

.value-bar {
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  background: var(--primary-color);
  opacity: 0.05;
  z-index: 0;
  pointer-events: none;
}

.show-more {
  padding: 0.75rem 1rem;
  text-align: center;
  font-size: 0.875rem;
  color: var(--text-secondary);
  background: var(--bg-color);
}

.pagination-controls {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem 1rem;
  background: var(--bg-color);
  border-top: 1px solid var(--border-color);
}

.pagination-info {
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.pagination-buttons {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.page-indicator {
  font-size: 0.875rem;
  color: var(--text-primary);
  font-weight: 600;
}

.error-message {
  padding: 2rem;
  text-align: center;
  color: var(--danger-color);
  background: rgba(239, 68, 68, 0.1);
  border-radius: 0.5rem;
}
</style>
