<template>
  <div class="dataset-browser">
    <div class="container">
      <div class="page-header">
        <h1>Dataset Browser</h1>
        <p>Explore {{ totalDatasets }} available datasets from the Open Data Hub</p>
      </div>

      <!-- Search and Filters -->
      <div class="filters-bar card">
        <!-- Search Box -->
        <div class="search-box">
          <input
            v-model="searchQuery"
            type="text"
            class="input"
            placeholder="Search datasets by name or description..."
          />
        </div>

        <div class="filter-section">
          <div class="filter-label">Filter by Dataspace:</div>
          <div class="filter-chips">
            <button
              @click="selectedDataspace = null"
              class="chip"
              :class="{ active: selectedDataspace === null }"
            >
              All ({{ totalDatasets }})
            </button>
            <button
              v-for="(count, dataspace) in dataspaceStats"
              :key="dataspace"
              @click="selectedDataspace = dataspace"
              class="chip"
              :class="{ active: selectedDataspace === dataspace }"
            >
              {{ dataspace }} ({{ count }})
            </button>
          </div>
        </div>

        <div class="filter-section">
          <div class="filter-label">Filter by API Type:</div>
          <div class="filter-chips">
            <button
              @click="selectedApiType = null"
              class="chip"
              :class="{ active: selectedApiType === null }"
            >
              All Types
            </button>
            <button
              v-for="(count, apiType) in apiTypeStats"
              :key="apiType"
              @click="selectedApiType = apiType"
              class="chip chip-type"
              :class="{ active: selectedApiType === apiType }"
            >
              {{ apiType }} ({{ count }})
            </button>
          </div>
        </div>

        <div class="filter-section">
          <div class="filter-label">
            Filter by Dataset Names
            <span v-if="selectedDatasetNames.length > 0" class="filter-count">
              ({{ selectedDatasetNames.length }} selected)
            </span>
          </div>
          <div class="dataset-names-filter">
            <div class="selected-datasets" v-if="selectedDatasetNames.length > 0">
              <span
                v-for="name in selectedDatasetNames"
                :key="name"
                class="selected-dataset-chip"
              >
                {{ name }}
                <button @click="toggleDatasetName(name)" class="remove-btn">×</button>
              </span>
              <button @click="clearDatasetNamesFilter" class="btn btn-sm btn-outline">
                Clear All
              </button>
            </div>
            <details class="dataset-picker">
              <summary class="picker-toggle">
                {{ selectedDatasetNames.length > 0 ? 'Add more datasets...' : 'Select datasets...' }}
              </summary>
              <div class="picker-dropdown">
                <div class="picker-search">
                  <input
                    type="text"
                    class="input input-sm"
                    placeholder="Search dataset names..."
                    @click.stop
                  />
                </div>
                <div class="picker-list">
                  <label
                    v-for="name in allDatasetNames"
                    :key="name"
                    class="picker-item"
                  >
                    <input
                      type="checkbox"
                      :checked="selectedDatasetNames.includes(name)"
                      @change="toggleDatasetName(name)"
                    />
                    <span>{{ name }}</span>
                  </label>
                </div>
              </div>
            </details>
          </div>
        </div>
      </div>

      <div v-if="loading" class="loading">
        <div class="spinner"></div>
      </div>

      <div v-else>
        <div class="results-header">
          <p>
            Showing {{ (currentPage - 1) * PAGE_SIZE + 1 }}-{{ Math.min(currentPage * PAGE_SIZE, filteredDatasets.length) }}
            of {{ filteredDatasets.length }} filtered datasets ({{ totalDatasets }} total)
          </p>
        </div>

        <div class="datasets-grid grid grid-3">
          <router-link
            v-for="dataset in paginatedDatasets"
            :key="dataset.name"
            :to="`/datasets/${dataset.name}`"
            class="dataset-card card"
          >
            <div class="card-header">
              <h3>{{ dataset.name }}</h3>
              <div class="badges">
                <span class="badge badge-dataspace">{{ dataset.dataspace }}</span>
                <span class="badge badge-api-type" :class="`badge-${dataset.apiType}`">
                  {{ dataset.apiType }}
                </span>
              </div>
            </div>

            <p v-if="dataset.description" class="dataset-description">
              {{ dataset.description }}
            </p>

            <div v-if="dataset.apiFilter && dataset.apiFilter.length > 0" class="dataset-filters">
              <div class="filter-label">Default Filters:</div>
              <div class="filter-tags">
                <span v-for="(filter, idx) in dataset.apiFilter.slice(0, 2)" :key="idx" class="filter-tag">
                  {{ filter }}
                </span>
                <span v-if="dataset.apiFilter.length > 2" class="filter-tag-more">
                  +{{ dataset.apiFilter.length - 2 }} more
                </span>
              </div>
            </div>

            <div v-if="dataset.loadedMetadata" class="dataset-meta">
              <div class="meta-item">
                <span class="meta-label">Total Entries:</span>
                <span class="meta-value">{{ formatNumber(dataset.loadedMetadata.totalResults) }}</span>
              </div>
              <div class="meta-item">
                <span class="meta-label">Pages:</span>
                <span class="meta-value">{{ formatNumber(dataset.loadedMetadata.totalPages) }}</span>
              </div>
            </div>
            <div v-else-if="dataset.loading" class="dataset-loading">
              Loading count...
            </div>

            <div class="dataset-actions">
              <span class="action-link">Inspect Dataset →</span>
            </div>
          </router-link>
        </div>

        <!-- Pagination Controls -->
        <div v-if="showPagination" class="pagination-controls">
          <div class="pagination-info">
            Page {{ currentPage }} of {{ totalPages }}
          </div>
          <div class="pagination-buttons">
            <button
              @click="previousPage"
              :disabled="currentPage === 1"
              class="btn btn-outline"
            >
              ← Previous
            </button>
            <div class="page-numbers">
              <button
                v-for="page in visiblePages"
                :key="page"
                @click="goToPage(page)"
                class="btn btn-page"
                :class="{ active: page === currentPage }"
              >
                {{ page }}
              </button>
            </div>
            <button
              @click="nextPage"
              :disabled="currentPage === totalPages"
              class="btn btn-outline"
            >
              Next →
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useDatasetStore } from '../stores/datasetStore'
import { useUrlState } from '../composables/useUrlState'
import * as contentApi from '../api/contentApi'

const datasetStore = useDatasetStore()
const loading = ref(true)
const datasetsWithMeta = ref([])

// URL state management
const { syncMultiple, serializers } = useUrlState()
const urlState = syncMultiple({
  search: {
    initial: '',
    ...serializers.string
  },
  dataspace: {
    initial: null,
    ...serializers.string
  },
  apiType: {
    initial: null,
    ...serializers.string
  },
  datasets: {
    initial: [],
    ...serializers.array
  },
  page: {
    initial: 1,
    ...serializers.number
  }
})

const searchQuery = urlState.search
const selectedDataspace = urlState.dataspace
const selectedApiType = urlState.apiType
const selectedDatasetNames = urlState.datasets
const currentPage = urlState.page

const PAGE_SIZE = 20

// Reset page to 1 when filters change
watch([searchQuery, selectedDataspace, selectedApiType, selectedDatasetNames], () => {
  currentPage.value = 1
})

onMounted(async () => {
  await loadDatasets()
})

async function loadDatasets() {
  loading.value = true

  try {
    // Load dataset types from metadata API
    await datasetStore.loadDatasetTypes()

    // Initialize datasets with loading state
    datasetsWithMeta.value = datasetStore.datasets.map(ds => ({
      ...ds,
      loadedMetadata: null,
      loading: true,
      error: null
    }))

    // Load entry counts for each dataset in parallel (limited concurrency)
    const batchSize = 10
    for (let i = 0; i < datasetsWithMeta.value.length; i += batchSize) {
      const batch = datasetsWithMeta.value.slice(i, i + batchSize)

      const metadataPromises = batch.map(async (dataset) => {
        const index = datasetsWithMeta.value.findIndex(d => d.name === dataset.name)
        try {
          // Use metadata to get dataset counts
          // const metadata = await contentApi.getDatasetMetadata(dataset.name, dataset.metadata)
          // datasetsWithMeta.value[index].loadedMetadata = metadata
          datasetsWithMeta.value[index].loading = false
        } catch (err) {
          console.error(`Error loading metadata for ${dataset.name}:`, err)
          datasetsWithMeta.value[index].error = err.message
          datasetsWithMeta.value[index].loading = false
        }
      })

      await Promise.allSettled(metadataPromises)
    }
  } catch (err) {
    console.error('Error loading datasets:', err)
  } finally {
    loading.value = false
  }
}

// Computed properties for filtering
const filteredDatasets = computed(() => {
  let filtered = datasetsWithMeta.value

  // Filter by search query
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    filtered = filtered.filter(ds =>
      ds.name?.toLowerCase().includes(query)/* ||
      ds.description?.toLowerCase().includes(query)*/
    )
  }

  // Filter by dataspace
  if (selectedDataspace.value) {
    const v = selectedDataspace.value.toLowerCase();
    filtered = filtered.filter(ds => ds.dataspace?.toLowerCase() === v)
  }

  // Filter by API type
  if (selectedApiType.value) {
    const v = selectedApiType.value.toLowerCase();
    filtered = filtered.filter(ds => ds.apiType?.toLowerCase() === v)
  }

  // Filter by selected dataset names
  if (selectedDatasetNames.value && selectedDatasetNames.value.length > 0) {
    const v = selectedDatasetNames.value.map(v => v.toLowerCase());
    filtered = filtered.filter(ds => v.includes(ds.name.toLowerCase()))
  }

  return filtered
})

// Paginated datasets
const paginatedDatasets = computed(() => {
  const start = (currentPage.value - 1) * PAGE_SIZE
  const end = start + PAGE_SIZE
  return filteredDatasets.value.slice(start, end)
})

const totalPages = computed(() => Math.ceil(filteredDatasets.value.length / PAGE_SIZE))

const showPagination = computed(() => filteredDatasets.value.length > PAGE_SIZE)

// Visible page numbers for pagination (show max 7 pages)
const visiblePages = computed(() => {
  const total = totalPages.value
  const current = currentPage.value
  const maxVisible = 7

  if (total <= maxVisible) {
    return Array.from({ length: total }, (_, i) => i + 1)
  }

  // Always show first page, last page, and pages around current
  const pages = new Set([1, total])
  const rangeStart = Math.max(2, current - 2)
  const rangeEnd = Math.min(total - 1, current + 2)

  for (let i = rangeStart; i <= rangeEnd; i++) {
    pages.add(i)
  }

  return Array.from(pages).sort((a, b) => a - b)
})

const totalDatasets = computed(() => datasetsWithMeta.value.length)

const dataspaceStats = computed(() => {
  const stats = {}
  datasetsWithMeta.value.forEach(ds => {
    const dataspace = ds.dataspace || 'unknown'
    stats[dataspace] = (stats[dataspace] || 0) + 1
  })
  // Sort by count descending
  return Object.fromEntries(
    Object.entries(stats).sort((a, b) => b[1] - a[1])
  )
})

const apiTypeStats = computed(() => {
  const stats = {}
  datasetsWithMeta.value.forEach(ds => {
    const apiType = ds.apiType || 'unknown'
    stats[apiType] = (stats[apiType] || 0) + 1
  })
  return stats
})

// All available dataset names for multifilter
const allDatasetNames = computed(() => {
  return datasetsWithMeta.value.map(ds => ds.name).sort()
})

function formatNumber(num) {
  if (!num) return '0'
  return new Intl.NumberFormat().format(num)
}

// Pagination functions
function goToPage(page) {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page
    // Scroll to top
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
}

function nextPage() {
  goToPage(currentPage.value + 1)
}

function previousPage() {
  goToPage(currentPage.value - 1)
}

// Dataset names filter functions
function toggleDatasetName(datasetName) {
  const current = selectedDatasetNames.value || []
  const index = current.indexOf(datasetName)
  if (index > -1) {
    selectedDatasetNames.value = current.filter(name => name !== datasetName)
  } else {
    selectedDatasetNames.value = [...current, datasetName]
  }
}

function clearDatasetNamesFilter() {
  selectedDatasetNames.value = []
}
</script>

<style scoped>
.dataset-browser {
  padding: 2rem 0;
}

.page-header {
  margin-bottom: 2rem;
}

.page-header h1 {
  font-size: 2rem;
  margin-bottom: 0.5rem;
  color: var(--text-primary);
}

.page-header p {
  color: var(--text-secondary);
  font-size: 1.125rem;
}

/* Filters Bar */
.filters-bar {
  margin-bottom: 2rem;
  padding: 1.5rem;
}

.search-box {
  margin-bottom: 1.5rem;
}

.search-box .input {
  width: 100%;
  font-size: 1rem;
}

.filter-section {
  margin-bottom: 1.5rem;
}

.filter-section:last-child {
  margin-bottom: 0;
}

.filter-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 0.75rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.filter-count {
  font-weight: 500;
  color: var(--primary-color);
  font-size: 0.875rem;
}

.filter-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.chip {
  padding: 0.5rem 1rem;
  border: 1px solid var(--border-color);
  border-radius: 9999px;
  background: transparent;
  color: var(--text-secondary);
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.chip:hover {
  border-color: var(--primary-color);
  color: var(--primary-color);
  background: rgba(59, 130, 246, 0.05);
}

.chip.active {
  border-color: var(--primary-color);
  background: var(--primary-color);
  color: white;
}

.chip-type.active {
  background: #10b981;
  border-color: #10b981;
}

/* Dataset Names Filter */
.dataset-names-filter {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.selected-datasets {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  align-items: center;
}

.selected-dataset-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.375rem 0.625rem;
  background: var(--primary-color);
  color: white;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  font-weight: 500;
}

.remove-btn {
  background: none;
  border: none;
  color: white;
  font-size: 1.25rem;
  line-height: 1;
  cursor: pointer;
  padding: 0;
  margin: 0;
  width: 1.25rem;
  height: 1.25rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  transition: background-color 0.2s;
}

.remove-btn:hover {
  background: rgba(255, 255, 255, 0.2);
}

.dataset-picker {
  position: relative;
  width: fit-content;
}

.picker-toggle {
  padding: 0.5rem 1rem;
  background: var(--bg-color);
  border: 1px solid var(--border-color);
  border-radius: 0.375rem;
  cursor: pointer;
  font-size: 0.875rem;
  color: var(--text-primary);
  list-style: none;
  transition: all 0.2s;
}

.picker-toggle:hover {
  border-color: var(--primary-color);
  background: rgba(59, 130, 246, 0.05);
}

.picker-toggle::-webkit-details-marker {
  display: none;
}

.picker-dropdown {
  position: absolute;
  top: 100%;
  left: 0;
  margin-top: 0.5rem;
  background: white;
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  box-shadow: var(--shadow-lg);
  min-width: 300px;
  max-width: 500px;
  z-index: 100;
}

.picker-search {
  padding: 0.75rem;
  border-bottom: 1px solid var(--border-color);
}

.picker-list {
  max-height: 300px;
  overflow-y: auto;
  padding: 0.5rem;
}

.picker-item {
  display: flex;
  align-items: center;
  gap: 0.625rem;
  padding: 0.625rem;
  cursor: pointer;
  border-radius: 0.375rem;
  transition: background-color 0.15s;
  font-size: 0.875rem;
}

.picker-item:hover {
  background: var(--bg-color);
}

.picker-item input[type="checkbox"] {
  cursor: pointer;
}

/* Results Header */
.results-header {
  margin-bottom: 1rem;
}

.results-header p {
  color: var(--text-secondary);
  font-size: 0.875rem;
}

/* Datasets Grid */
.datasets-grid {
  margin-top: 1rem;
}

.dataset-card {
  text-decoration: none;
  color: inherit;
  transition: all 0.3s ease;
  cursor: pointer;
  position: relative;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.dataset-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 3px;
  background: linear-gradient(90deg, var(--primary-color), #7c3aed);
  transform: scaleX(0);
  transition: transform 0.3s ease;
}

.dataset-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-lg);
}

.dataset-card:hover::before {
  transform: scaleX(1);
}

.card-header {
  margin-bottom: 0.75rem;
}

.dataset-card h3 {
  font-size: 1.125rem;
  margin-bottom: 0.5rem;
  color: var(--text-primary);
  font-weight: 600;
}

.badges {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.badge {
  display: inline-block;
  padding: 0.25rem 0.625rem;
  border-radius: 0.25rem;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: capitalize;
}

.badge-dataspace {
  background: rgba(59, 130, 246, 0.1);
  color: var(--primary-color);
}

.badge-api-type {
  background: rgba(16, 185, 129, 0.1);
  color: #10b981;
}

.badge-content {
  background: rgba(59, 130, 246, 0.1);
  color: var(--primary-color);
}

.badge-timeseries {
  background: rgba(245, 158, 11, 0.1);
  color: #f59e0b;
}

.dataset-description {
  color: var(--text-secondary);
  font-size: 0.875rem;
  line-height: 1.5;
  margin-bottom: 1rem;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

/* Dataset Filters */
.dataset-filters {
  margin-bottom: 1rem;
  padding: 0.75rem;
  background: var(--bg-color);
  border-radius: 0.375rem;
  border: 1px solid var(--border-color);
}

.filter-label {
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--text-secondary);
  margin-bottom: 0.5rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.filter-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.375rem;
}

.filter-tag {
  display: inline-block;
  padding: 0.25rem 0.5rem;
  background: rgba(59, 130, 246, 0.1);
  color: var(--primary-color);
  border-radius: 0.25rem;
  font-size: 0.75rem;
  font-family: monospace;
}

.filter-tag-more {
  display: inline-block;
  padding: 0.25rem 0.5rem;
  background: rgba(107, 114, 128, 0.1);
  color: var(--text-secondary);
  border-radius: 0.25rem;
  font-size: 0.75rem;
  font-style: italic;
}

/* Dataset Meta */
.dataset-meta {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin: 1rem 0;
}

.meta-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.5rem;
  background: var(--bg-color);
  border-radius: 0.25rem;
}

.meta-label {
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.meta-value {
  font-size: 1rem;
  font-weight: 600;
  color: var(--primary-color);
}

.dataset-loading,
.dataset-error {
  padding: 0.75rem;
  text-align: center;
  color: var(--text-secondary);
  font-size: 0.875rem;
  font-style: italic;
}

.dataset-error {
  color: var(--danger-color);
}

/* Dataset Actions */
.dataset-actions {
  margin-top: auto;
  padding-top: 1rem;
  border-top: 1px solid var(--border-color);
}

.action-link {
  color: var(--primary-color);
  font-weight: 600;
  font-size: 0.875rem;
  transition: transform 0.2s ease;
  display: inline-block;
}

.dataset-card:hover .action-link {
  transform: translateX(4px);
}

/* Pagination Controls */
.pagination-controls {
  margin-top: 2rem;
  padding: 1.5rem;
  background: white;
  border-radius: 0.5rem;
  box-shadow: var(--shadow-sm);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.pagination-info {
  font-size: 0.875rem;
  color: var(--text-secondary);
  font-weight: 500;
}

.pagination-buttons {
  display: flex;
  gap: 0.5rem;
  align-items: center;
  flex-wrap: wrap;
  justify-content: center;
}

.btn {
  padding: 0.5rem 1rem;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  border: 1px solid var(--border-color);
  background: white;
  color: var(--text-primary);
}

.btn:hover:not(:disabled) {
  border-color: var(--primary-color);
  color: var(--primary-color);
  background: rgba(59, 130, 246, 0.05);
}

.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-outline {
  border: 1px solid var(--border-color);
  background: transparent;
}

.btn-sm {
  padding: 0.375rem 0.75rem;
  font-size: 0.8125rem;
}

.page-numbers {
  display: flex;
  gap: 0.375rem;
}

.btn-page {
  min-width: 2.5rem;
  padding: 0.5rem;
  text-align: center;
}

.btn-page.active {
  background: var(--primary-color);
  color: white;
  border-color: var(--primary-color);
}

.btn-page.active:hover {
  background: var(--primary-color);
  color: white;
  border-color: var(--primary-color);
  opacity: 0.9;
}

/* Input styles */
.input {
  padding: 0.625rem;
  border: 1px solid var(--border-color);
  border-radius: 0.375rem;
  font-size: 1rem;
  transition: all 0.2s;
}

.input:focus {
  outline: none;
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.input-sm {
  padding: 0.5rem;
  font-size: 0.875rem;
}

/* Responsive */
@media (max-width: 768px) {
  .filter-section {
    margin-bottom: 1.25rem;
  }

  .filter-chips {
    font-size: 0.8rem;
  }

  .chip {
    padding: 0.375rem 0.75rem;
  }

  .datasets-grid {
    grid-template-columns: 1fr;
  }

  .picker-dropdown {
    min-width: 250px;
  }

  .pagination-buttons {
    flex-direction: column;
    width: 100%;
  }

  .page-numbers {
    order: 1;
  }
}
</style>
