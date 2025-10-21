<template>
  <div class="dataset-inspector">
    <div class="container">
      <div class="page-header">
        <h1>{{ datasetName }}</h1>
        <p>Inspect dataset entries and apply filters</p>
      </div>

      <!-- cURL Display -->
      <CurlDisplay :curlCommand="curlCommand" />

      <!-- Dataset Statistics -->
      <div v-if="loading" class="loading-placeholder card">
        <div class="spinner"></div>
        <p>Loading dataset analysis...</p>
      </div>
      <DatasetStats
        v-else-if="analysis"
        :analysis="analysis"
        :total-entries="totalResults"
        :timeseries-analysis="timeseriesAnalysis"
      />

      <!-- Filters Section -->
      <div class="filters-section card">
        <h3>Filters</h3>

        <div class="filter-group">
          <label>Search</label>
          <input
            v-model="searchfilter"
            type="text"
            class="input"
            placeholder="Search in titles..."
          />
        </div>

        <div class="filter-group">
          <label>Guided Filter Builder</label>
          <button @click="openFilterBuilder" class="btn btn-outline btn-block">
            Open Filter Builder
          </button>
          <small>Build filters step-by-step with a guided interface</small>
        </div>

        <div class="filter-group">
          <label>
            Field Presence Filters (not null)
            <span v-if="hasUnappliedChanges" class="unapplied-badge">Unapplied changes</span>
          </label>
          <div v-if="loading" class="loading-placeholder-small">
            <div class="spinner-small"></div>
            <p>Loading fields...</p>
          </div>
          <div v-else class="field-list">
            <div
              v-for="field in allFields"
              :key="field.path"
              class="field-item"
              :class="{ 'field-selected': presenceFilters.includes(field.path) }"
              @click="togglePresenceFilter(field.path)"
            >
              <div class="field-header">
                <div class="field-checkbox-wrapper">
                  <input
                    type="checkbox"
                    :checked="presenceFilters.includes(field.path)"
                    @change.stop="togglePresenceFilter(field.path)"
                  />
                  <span class="field-path">{{ field.path }}</span>
                </div>
                <span
                  class="field-completeness"
                  :class="getCompletenessClass(field.completeness)"
                >
                  {{ field.completeness }}%
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
          <button v-if="analysis && analysis.fields.length > 20" @click="showAllPresenceFilters = !showAllPresenceFilters" class="btn btn-outline btn-sm" style="margin-top: 1rem;">
            {{ showAllPresenceFilters ? 'Show Less' : `Show All ${analysis.fields.length} Fields` }}
          </button>
        </div>

        <div class="filter-group">
          <label>Generated Filter Expression</label>
          <input
            :value="generatedRawFilter"
            type="text"
            class="input"
            readonly
            placeholder="Auto-generated from presence filters..."
          />
          <small>This filter is auto-generated from the selected presence filters above</small>
        </div>

        <div class="filter-actions">
          <button
            @click="applyFilters"
            class="btn btn-primary"
            :disabled="!hasUnappliedChanges"
          >
            Apply Filters{{ hasUnappliedChanges ? ' *' : '' }}
          </button>
          <button @click="clearFilters" class="btn btn-outline">Clear All</button>
        </div>
      </div>

      <!-- Filter Builder Modal -->
      <FilterBuilder
        :is-open="filterBuilderOpen"
        :fields="analysis?.fields || []"
        @close="filterBuilderOpen = false"
        @apply="applyFilterFromBuilder"
      />

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
          <button
            @click="view = 'distinct'"
            class="tab"
            :class="{ active: view === 'distinct' }"
          >
            Distinct Values
          </button>
          <button
            @click="view = 'timeseries'"
            class="tab"
            :class="{ active: view === 'timeseries' }"
          >
            Timeseries
          </button>
        </div>

        <div class="bulk-actions">
          <button
            @click="openBulkTimeseries"
            class="btn btn-primary"
            :disabled="selectedEntries.length === 0"
          >
            Bulk Inspect Selected ({{ selectedEntries.length }})
          </button>
          <button
            @click="openBulkTimeseriesAll"
            class="btn btn-secondary"
            :disabled="totalResults === 0"
          >
            Bulk Inspect All Filtered ({{ totalResults }})
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
          <div class="pagination-info">
            Showing {{ (page - 1) * pagesize + 1 }} -
            {{ Math.min(page * pagesize, totalResults) }} of {{ totalResults }} entries
          </div>
          <div class="pagination">
            <button @click="previousPage" :disabled="page === 1" class="btn btn-outline btn-sm">
              Previous
            </button>
            <span class="page-info">Page {{ page }} of {{ totalPages }}</span>
            <button @click="nextPage" :disabled="page >= totalPages" class="btn btn-outline btn-sm">
              Next
            </button>
          </div>
        </div>

        <div class="table-wrapper">
          <table class="table">
            <thead>
              <tr>
                <th class="checkbox-col">
                  <input type="checkbox" @change="toggleSelectAll" :checked="allSelected" />
                </th>
                <th class="id-col">ID</th>
                <th v-for="field in displayFields" :key="field" class="field-col">{{ field }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="entry in entries" :key="entry.Id">
                <td class="checkbox-col">
                  <input
                    type="checkbox"
                    :checked="isSelected(entry.Id)"
                    @change="toggleSelection(entry)"
                  />
                </td>
                <td class="id-cell">{{ entry.Id }}</td>
                <td v-for="field in displayFields" :key="field" class="field-cell">
                  <span class="cell-value">{{ formatCellValue(getValueAtPath(entry, field)) }}</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="pagination">
          <button @click="previousPage" :disabled="page === 1" class="btn btn-outline">
            Previous
          </button>
          <span class="page-info">Page {{ page }} of {{ totalPages }}</span>
          <button @click="nextPage" :disabled="page >= totalPages" class="btn btn-outline">
            Next
          </button>
        </div>
      </div>

      <!-- Raw JSON View -->
      <div v-else-if="view === 'raw'" class="raw-view">
        <JsonViewer :data="{ Items: entries, TotalResults: totalResults, CurrentPage: page }" />
      </div>

      <!-- Distinct Values View -->
      <div v-else-if="view === 'distinct'" class="distinct-view">
        <div v-if="loading" class="loading-placeholder card">
          <div class="spinner"></div>
          <p>Loading distinct values analysis...</p>
        </div>
        <DistinctValuesAnalyzer
          v-else
          :fields="analysis?.fields || []"
          :dataset-name="datasetName"
          :current-filters="{ searchfilter, rawfilter: generatedRawFilter }"
          :total-entries="totalResults"
          @fetch-all-data="handleFetchAllData"
        />
      </div>

      <!-- Timeseries View -->
      <div v-else-if="view === 'timeseries'" class="timeseries-view">
        <div v-if="loading" class="loading-placeholder card">
          <div class="spinner"></div>
          <p>Loading timeseries analysis...</p>
        </div>
        <TimeseriesAnalyzer
          v-else
          :timeseries-analysis="timeseriesAnalysis"
          :dataset-name="datasetName"
          :current-filters="{ searchfilter, rawfilter: generatedRawFilter }"
          :total-entries="totalResults"
          @fetch-all-data="handleFetchAllData"
        />
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useDatasetStore } from '../stores/datasetStore'
import { useSelectionStore } from '../stores/selectionStore'
import { useDatasetUrlState } from '../composables/useUrlState'
import { buildCurlCommand, getAllFilteredIds, getAllFilteredEntries } from '../api/contentApi'
import { getValueAtPath } from '../utils/dataAnalyzer'
import { ContentFilterBuilder } from '../utils/filterBuilder'
import CurlDisplay from '../components/CurlDisplay.vue'
import JsonViewer from '../components/JsonViewer.vue'
import DatasetStats from '../components/DatasetStats.vue'
import FilterBuilder from '../components/FilterBuilder.vue'
import DistinctValuesAnalyzer from '../components/DistinctValuesAnalyzer.vue'
import TimeseriesAnalyzer from '../components/TimeseriesAnalyzer.vue'

const props = defineProps({
  datasetName: {
    type: String,
    required: true
  }
})

const router = useRouter()
const datasetStore = useDatasetStore()
const selectionStore = useSelectionStore()

// URL-synced state
const urlState = useDatasetUrlState()
const page = urlState.page
const pagesize = urlState.pagesize
const view = urlState.view
const searchfilter = urlState.searchfilter
const selectedIds = urlState.selectedIds
const presenceFilters = urlState.presenceFilters

// Local state
const loading = ref(false)
const error = ref(null)
const showAllPresenceFilters = ref(false)
const selectedEntries = ref([])
const filterBuilderOpen = ref(false)
const appliedPresenceFilters = ref([]) // Actually applied filters (after clicking Apply)

// Computed raw filter from APPLIED presence filters (not the current selection)
const generatedRawFilter = computed(() => {
  if (appliedPresenceFilters.value.length === 0) return ''

  const builder = new ContentFilterBuilder()
  appliedPresenceFilters.value.forEach(field => {
    builder.isNotNull(field)
  })

  return builder.build()
})

// Check if there are unapplied filter changes
const hasUnappliedChanges = computed(() => {
  const current = [...presenceFilters.value].sort().join(',')
  const applied = [...appliedPresenceFilters.value].sort().join(',')
  return current !== applied
})

// Computed
const entries = computed(() => datasetStore.entries)
const analysis = computed(() => datasetStore.analysis)
const timeseriesAnalysis = computed(() => datasetStore.timeseriesAnalysis)
const totalResults = computed(() => datasetStore.totalResults)
const totalPages = computed(() => datasetStore.totalPages)

const allFields = computed(() => {
  if (!analysis.value?.fields) return []

  const fields = analysis.value.fields
    .filter(f => !f.path.includes('[]')) // Filter out array item fields for cleaner view
    .sort((a, b) => b.completeness - a.completeness)

  return showAllPresenceFilters.value ? fields : fields.slice(0, 20)
})

const displayFields = computed(() => {
  if (!analysis.value?.fields) return []

  // Get all top-level fields (not nested, not array items)
  const topLevel = analysis.value.fields
    .filter(f => !f.path.includes('.') && !f.path.includes('[]'))
    .sort((a, b) => b.completeness - a.completeness)
    .map(f => f.path)

  return topLevel
})

const allSelected = computed(() => {
  return entries.value.length > 0 && selectedEntries.value.length === entries.value.length
})

const curlCommand = computed(() => {
  return buildCurlCommand(props.datasetName, {
    pagenumber: page.value,
    pagesize: pagesize.value,
    searchfilter: searchfilter.value || undefined,
    rawfilter: generatedRawFilter.value || undefined
  }, 'GET', datasetStore.currentMetadata)
})

// Watch for filter changes
watch([page, pagesize, searchfilter, generatedRawFilter], () => {
  loadData()
})

// Initialize
onMounted(() => {
  // Restore selected entries from URL
  if (selectedIds.value && selectedIds.value.length > 0) {
    // Will be populated after data loads
  }

  // Initialize applied filters from URL state
  appliedPresenceFilters.value = [...presenceFilters.value]

  loadData()
})

async function loadData() {
  try {
    loading.value = true
    error.value = null

    await datasetStore.loadDatasetEntries(props.datasetName, {
      pagenumber: page.value,
      pagesize: pagesize.value,
      searchfilter: searchfilter.value || undefined,
      rawfilter: generatedRawFilter.value || undefined
    })

    // Restore selections after data loads
    if (selectedIds.value && selectedIds.value.length > 0) {
      selectedEntries.value = entries.value.filter(e => selectedIds.value.includes(e.Id))
    }
  } catch (err) {
    error.value = err.message
  } finally {
    loading.value = false
  }
}

function togglePresenceFilter(fieldPath) {
  const index = presenceFilters.value.indexOf(fieldPath)
  if (index > -1) {
    presenceFilters.value = presenceFilters.value.filter((_, i) => i !== index)
  } else {
    presenceFilters.value = [...presenceFilters.value, fieldPath]
  }
  // Don't apply immediately - wait for user to click Apply button
}

function applyFilters() {
  // Apply the selected filters
  appliedPresenceFilters.value = [...presenceFilters.value]
  // Reset to page 1 when filters are applied
  page.value = 1
}

function clearFilters() {
  searchfilter.value = null
  presenceFilters.value = []
  appliedPresenceFilters.value = []
  page.value = 1
}

function toggleSelection(entry) {
  const index = selectedEntries.value.findIndex(e => e.Id === entry.Id)
  if (index > -1) {
    selectedEntries.value.splice(index, 1)
  } else {
    selectedEntries.value.push(entry)
  }

  // Update URL
  selectedIds.value = selectedEntries.value.map(e => e.Id)
}

function toggleSelectAll() {
  if (allSelected.value) {
    selectedEntries.value = []
  } else {
    selectedEntries.value = [...entries.value]
  }

  selectedIds.value = selectedEntries.value.map(e => e.Id)
}

function isSelected(id) {
  return selectedEntries.value.some(e => e.Id === id)
}

function nextPage() {
  if (page.value < totalPages.value) {
    page.value++
  }
}

function previousPage() {
  if (page.value > 1) {
    page.value--
  }
}

function openBulkTimeseries() {
  // Store selections in the selection store
  selectionStore.setSelectedEntries(selectedEntries.value, props.datasetName)

  // Navigate to bulk measurements inspector with URL params
  router.push({
    path: '/bulk-measurements',
    query: {
      sensors: selectedEntries.value.map(e => e.Id).join(','),
    }
  })
}

async function openBulkTimeseriesAll() {
  try {
    loading.value = true
    error.value = null

    // Get all IDs matching the current filter
    const ids = await getAllFilteredIds(props.datasetName, {
      searchfilter: searchfilter.value || undefined,
      rawfilter: generatedRawFilter.value || undefined
    }, datasetStore.currentMetadata)

    if (!ids || ids.length === 0) {
      error.value = 'No entries found matching the filter'
      return
    }

    // Navigate to bulk measurements inspector with all filtered IDs
    router.push({
      path: '/bulk-measurements',
      query: {
        sensors: ids.join(','),
      }
    })
  } catch (err) {
    error.value = `Failed to fetch all IDs: ${err.message}`
  } finally {
    loading.value = false
  }
}

function formatCellValue(value) {
  if (value === null || value === undefined) return '-'
  if (typeof value === 'object') return JSON.stringify(value, null, 2)
  if (typeof value === 'boolean') return value ? '✓' : '✗'
  return String(value)
}

function getCompletenessClass(completeness) {
  if (completeness >= 80) return 'high'
  if (completeness >= 50) return 'medium'
  return 'low'
}

function openFilterBuilder() {
  filterBuilderOpen.value = true
}

function applyFilterFromBuilder(filter) {
  // For now, the filter builder generates rawfilter syntax
  // We could enhance this later to parse the filter and extract field presence filters
  // For now, we'll just log it or ignore it since we're focusing on presence filters
  console.log('Filter from builder:', filter)
  // TODO: Parse filter builder output to extract presence filters
}

async function handleFetchAllData(callback) {
  try {
    const allData = await getAllFilteredEntries(
      props.datasetName,
      {
        searchfilter: searchfilter.value || undefined,
        rawfilter: generatedRawFilter.value || undefined
      },
      (progress) => {
        // Progress updates can be handled here if needed
        console.log(`Fetching page ${progress.current} of ${progress.total}`)
      },
      datasetStore.currentMetadata
    )

    callback(allData)
  } catch (err) {
    console.error('Failed to fetch all data:', err)
    callback([])
  }
}
</script>

<style scoped>
.dataset-inspector {
  padding: 2rem 0;
}

.page-header {
  margin-bottom: 2rem;
}

.page-header h1 {
  font-size: 2rem;
  margin-bottom: 0.5rem;
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

.filters-section {
  margin-bottom: 2rem;
}

.filters-section h3 {
  margin-bottom: 1rem;
}

.filter-group {
  margin-bottom: 1.5rem;
}

.filter-group label {
  display: block;
  font-weight: 600;
  margin-bottom: 0.5rem;
  color: var(--text-primary);
}

.filter-group small {
  display: block;
  margin-top: 0.25rem;
  color: var(--text-secondary);
  font-size: 0.875rem;
}

.filter-actions {
  display: flex;
  gap: 1rem;
}

.table-controls {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.pagination-info {
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.pagination {
  display: flex;
  gap: 1rem;
  align-items: center;
  justify-content: center;
  margin: 1.5rem 0;
}

.page-info {
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

.id-col {
  min-width: 150px;
  position: sticky;
  left: 40px;
  background: var(--surface-color);
  z-index: 1;
  border-right: 2px solid var(--border-color);
}

.id-cell {
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

.error-message {
  padding: 2rem;
  text-align: center;
  color: var(--danger-color);
  background: rgba(239, 68, 68, 0.1);
  border-radius: 0.5rem;
}

.btn-sm {
  padding: 0.375rem 0.75rem;
  font-size: 0.875rem;
}

.btn-block {
  width: 100%;
  margin-top: 0.5rem;
}

.distinct-view {
  margin-top: 1rem;
}

/* Loading placeholders */
.loading-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem;
  background: var(--surface-color);
  border-radius: 0.5rem;
  margin-bottom: 1.5rem;
}

.loading-placeholder p {
  margin-top: 1rem;
  color: var(--text-secondary);
  font-size: 0.875rem;
}

.loading-placeholder-small {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  padding: 2rem;
  background: var(--bg-color);
  border-radius: 0.375rem;
}

.loading-placeholder-small p {
  color: var(--text-secondary);
  font-size: 0.875rem;
  margin: 0;
}

.spinner-small {
  width: 20px;
  height: 20px;
  border: 2px solid var(--border-color);
  border-top-color: var(--primary-color);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

/* Field list for presence filters */
.field-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin-top: 0.5rem;
}

.field-item {
  padding: 0.75rem;
  background: var(--bg-color);
  border-radius: 0.375rem;
  cursor: pointer;
  transition: all 0.2s ease;
  border: 2px solid transparent;
}

.field-item:hover {
  background: var(--surface-color);
  border-color: var(--border-color);
}

.field-item.field-selected {
  background: var(--surface-color);
  border-color: var(--primary-color);
}

.field-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
}

.field-checkbox-wrapper {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex: 1;
}

.field-checkbox-wrapper input[type="checkbox"] {
  cursor: pointer;
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

/* Unapplied changes badge */
.unapplied-badge {
  margin-left: 0.5rem;
  font-size: 0.75rem;
  font-weight: 600;
  padding: 0.25rem 0.5rem;
  border-radius: 0.25rem;
  background: rgba(245, 158, 11, 0.1);
  color: var(--warning-color);
}

/* Disabled button state */
.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
