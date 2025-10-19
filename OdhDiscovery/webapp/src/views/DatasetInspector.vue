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
      <DatasetStats
        v-if="analysis"
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
          <label>Field Presence Filters (not null)</label>
          <div class="presence-filters">
            <div
              v-for="field in allFields"
              :key="field.path"
              class="filter-checkbox"
            >
              <label>
                <input
                  type="checkbox"
                  :checked="presenceFilters.includes(field.path)"
                  @change="togglePresenceFilter(field.path)"
                />
                {{ field.path }}
                <span class="completeness-badge" :class="getCompletenessClass(field.completeness)">
                  {{ field.completeness }}%
                </span>
              </label>
            </div>
          </div>
          <button v-if="analysis && analysis.fields.length > 20" @click="showAllPresenceFilters = !showAllPresenceFilters" class="btn btn-outline btn-sm" style="margin-top: 1rem;">
            {{ showAllPresenceFilters ? 'Show Less' : `Show All ${analysis.fields.length} Fields` }}
          </button>
        </div>

        <div class="filter-group">
          <label>Raw Filter Expression (Advanced)</label>
          <input
            v-model="rawfilter"
            type="text"
            class="input"
            placeholder="e.g., eq(Active,true)"
          />
          <small>Use Content API rawfilter syntax or use the Filter Builder above</small>
        </div>

        <div class="filter-actions">
          <button @click="applyFilters" class="btn btn-primary">Apply Filters</button>
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
        <DistinctValuesAnalyzer
          :fields="analysis?.fields || []"
          :dataset-name="datasetName"
          :current-filters="{ searchfilter, rawfilter }"
          :total-entries="totalResults"
          @fetch-all-data="handleFetchAllData"
        />
      </div>

      <!-- Timeseries View -->
      <div v-else-if="view === 'timeseries'" class="timeseries-view">
        <TimeseriesAnalyzer
          :timeseries-analysis="timeseriesAnalysis"
          :dataset-name="datasetName"
          :current-filters="{ searchfilter, rawfilter }"
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
const rawfilter = urlState.rawfilter
const searchfilter = urlState.searchfilter
const selectedIds = urlState.selectedIds

// Local state
const loading = ref(false)
const error = ref(null)
const presenceFilters = ref([])
const showAllPresenceFilters = ref(false)
const selectedEntries = ref([])
const filterBuilderOpen = ref(false)

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
    rawfilter: rawfilter.value || undefined
  }, 'GET', datasetStore.currentMetadata)
})

// Watch for filter changes
watch([page, pagesize, searchfilter, rawfilter], () => {
  loadData()
})

// Initialize
onMounted(() => {
  // Restore selected entries from URL
  if (selectedIds.value && selectedIds.value.length > 0) {
    // Will be populated after data loads
  }

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
      rawfilter: rawfilter.value || undefined
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
    presenceFilters.value.splice(index, 1)
  } else {
    presenceFilters.value.push(fieldPath)
  }
}

function applyFilters() {
  // Build rawfilter from presence filters
  if (presenceFilters.value.length > 0) {
    const builder = new ContentFilterBuilder()
    presenceFilters.value.forEach(field => {
      builder.isNotNull(field)
    })

    const presenceFilter = builder.build()

    // Combine with existing rawfilter
    if (rawfilter.value) {
      rawfilter.value = `and(${presenceFilter},${rawfilter.value})`
    } else {
      rawfilter.value = presenceFilter
    }
  }

  // Reset to page 1
  page.value = 1
}

function clearFilters() {
  rawfilter.value = null
  searchfilter.value = null
  presenceFilters.value = []
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
      rawfilter: rawfilter.value || undefined
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
  if (filter) {
    rawfilter.value = filter
    page.value = 1
  }
}

async function handleFetchAllData(callback) {
  try {
    const allData = await getAllFilteredEntries(
      props.datasetName,
      {
        searchfilter: searchfilter.value || undefined,
        rawfilter: rawfilter.value || undefined
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

.presence-filters {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 0.75rem;
  margin-top: 0.5rem;
}

.filter-checkbox label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: normal;
  cursor: pointer;
}

.completeness-badge {
  margin-left: auto;
  font-size: 0.75rem;
  padding: 0.125rem 0.5rem;
  border-radius: 9999px;
}

.completeness-badge.high {
  background: rgba(16, 185, 129, 0.1);
  color: var(--success-color);
}

.completeness-badge.medium {
  background: rgba(245, 158, 11, 0.1);
  color: var(--warning-color);
}

.completeness-badge.low {
  background: rgba(239, 68, 68, 0.1);
  color: var(--danger-color);
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
</style>
