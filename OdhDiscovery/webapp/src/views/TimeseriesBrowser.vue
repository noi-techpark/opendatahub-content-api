<template>
  <div class="timeseries-browser">
    <div class="container">
      <div class="page-header">
        <h1>Timeseries Browser</h1>
        <p>Explore {{ totalTypes }} available timeseries measurement types</p>
      </div>

      <!-- Search and Filters -->
      <div class="filters-bar card">
        <div class="filter-section">
          <div class="filter-label">Filter by Data Type:</div>
          <div class="filter-chips">
            <button
              @click="selectedDataType = null"
              class="chip"
              :class="{ active: selectedDataType === null }"
            >
              All Types ({{ totalTypes }})
            </button>
            <button
              v-for="(count, dataType) in dataTypeStats"
              :key="dataType"
              @click="selectedDataType = dataType"
              class="chip chip-type"
              :class="{ active: selectedDataType === dataType }"
            >
              {{ dataType }} ({{ count }})
            </button>
          </div>
        </div>

        <div class="filter-section">
          <div class="filter-label">
            Filter by Timeseries Names
            <span v-if="selectedTimeseriesNames.length > 0" class="filter-count">
              ({{ selectedTimeseriesNames.length }} selected)
            </span>
          </div>
          <div class="timeseries-names-filter">
            <div class="selected-timeseries" v-if="selectedTimeseriesNames.length > 0">
              <span
                v-for="name in selectedTimeseriesNames"
                :key="name"
                class="selected-timeseries-chip"
              >
                {{ name }}
                <button @click="toggleTimeseriesName(name)" class="remove-btn">×</button>
              </span>
              <button @click="clearTimeseriesNamesFilter" class="btn btn-sm btn-outline">
                Clear All
              </button>
            </div>
            <details class="timeseries-picker">
              <summary class="picker-toggle">
                {{ selectedTimeseriesNames.length > 0 ? 'Add more timeseries...' : 'Select timeseries...' }}
              </summary>
              <div class="picker-dropdown">
                <div class="picker-search">
                  <input
                    type="text"
                    class="input input-sm"
                    placeholder="Search timeseries names..."
                    @click.stop
                  />
                </div>
                <div class="picker-list">
                  <label
                    v-for="name in allTimeseriesNames"
                    :key="name"
                    class="picker-item"
                  >
                    <input
                      type="checkbox"
                      :checked="selectedTimeseriesNames.includes(name)"
                      @change="toggleTimeseriesName(name)"
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
            Showing {{ (currentPage - 1) * PAGE_SIZE + 1 }}-{{ Math.min(currentPage * PAGE_SIZE, filteredTypes.length) }}
            of {{ filteredTypes.length }} filtered timeseries ({{ totalTypes }} total)
          </p>
        </div>

        <div class="types-grid grid grid-3">
          <router-link
            v-for="type in paginatedTypes"
            :key="type.type.name"
            :to="`/timeseries/${type.type.name}`"
            class="type-card card"
          >
          <h3>{{ type.type.name }}</h3>
          <div class="type-meta">
            <div class="meta-row">
              <span class="meta-label">Description:</span>
              <span class="meta-value">{{ type.type.description || 'N/A' }}</span>
            </div>
            <div class="meta-row">
              <span class="meta-label">Unit:</span>
              <span class="meta-value">{{ type.type.unit || 'N/A' }}</span>
            </div>
            <div class="meta-row">
              <span class="meta-label">Data Type:</span>
              <span class="badge" :class="`badge-${getDataTypeBadge(type.type.data_type)}`">
                {{ type.type.data_type }}
              </span>
            </div>
            <div class="meta-row">
              <span class="meta-label">Sensors:</span>
              <span class="meta-value">{{ type.sensors?.length || 0 }}</span>
            </div>
          </div>
          <div class="type-actions">
            <span class="action-link">Inspect Timeseries →</span>
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
import { useTimeseriesStore } from '../stores/timeseriesStore'
import { useUrlState } from '../composables/useUrlState'

const timeseriesStore = useTimeseriesStore()
const loading = ref(false)
const types = ref([])

// URL state management
const { syncMultiple, serializers } = useUrlState()
const urlState = syncMultiple({
  search: {
    initial: '',
    ...serializers.string
  },
  dataType: {
    initial: null,
    ...serializers.string
  },
  timeseries: {
    initial: [],
    ...serializers.array
  },
  page: {
    initial: 1,
    ...serializers.number
  }
})

const selectedDataType = urlState.dataType
const selectedTimeseriesNames = urlState.timeseries
const currentPage = urlState.page

const PAGE_SIZE = 20

// Reset page to 1 when filters change
watch([selectedDataType, selectedTimeseriesNames], () => {
  currentPage.value = 1
})

onMounted(async () => {
  await loadTypes()
})

async function loadTypes() {
  loading.value = true
  try {
    const result = await timeseriesStore.loadTypes({ include_sensors: true, limit: 10000 })
    types.value = result.types || []
  } finally {
    loading.value = false
  }
}

// Computed properties for filtering
const filteredTypes = computed(() => {
  let filtered = types.value

  // Filter by data type
  if (selectedDataType.value) {
    filtered = filtered.filter(t => t.type.data_type === selectedDataType.value)
  }

  // Filter by selected timeseries names
  if (selectedTimeseriesNames.value && selectedTimeseriesNames.value.length > 0) {
    filtered = filtered.filter(t => selectedTimeseriesNames.value.includes(t.type.name))
  }

  return filtered
})

// Paginated types
const paginatedTypes = computed(() => {
  const start = (currentPage.value - 1) * PAGE_SIZE
  const end = start + PAGE_SIZE
  return filteredTypes.value.slice(start, end)
})

const totalPages = computed(() => Math.ceil(filteredTypes.value.length / PAGE_SIZE))

const showPagination = computed(() => filteredTypes.value.length > PAGE_SIZE)

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

const totalTypes = computed(() => types.value.length)

// Data type statistics
const dataTypeStats = computed(() => {
  const stats = {}
  types.value.forEach(t => {
    const dataType = t.type.data_type || 'unknown'
    stats[dataType] = (stats[dataType] || 0) + 1
  })
  return stats
})

// All available timeseries names for multifilter
const allTimeseriesNames = computed(() => {
  return types.value.map(t => t.type.name).sort()
})

function getDataTypeBadge(dataType) {
  const badges = {
    'numeric': 'primary',
    'string': 'secondary',
    'boolean': 'success',
    'json': 'warning',
    'geoposition': 'primary',
    'geoshape': 'primary'
  }
  return badges[dataType] || 'secondary'
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

// Timeseries names filter functions
function toggleTimeseriesName(name) {
  const current = selectedTimeseriesNames.value || []
  const index = current.indexOf(name)
  if (index > -1) {
    selectedTimeseriesNames.value = current.filter(n => n !== name)
  } else {
    selectedTimeseriesNames.value = [...current, name]
  }
}

function clearTimeseriesNamesFilter() {
  selectedTimeseriesNames.value = []
}
</script>

<style scoped>
.timeseries-browser {
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

/* Timeseries Names Filter */
.timeseries-names-filter {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.selected-timeseries {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  align-items: center;
}

.selected-timeseries-chip {
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

.timeseries-picker {
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

.types-grid {
  margin-top: 1rem;
}

.type-card {
  text-decoration: none;
  color: inherit;
  transition: all 0.3s ease;
  cursor: pointer;
  position: relative;
  overflow: hidden;
}

.type-card::before {
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

.type-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-lg);
}

.type-card:hover::before {
  transform: scaleX(1);
}

.type-card h3 {
  font-size: 1.25rem;
  margin-bottom: 1rem;
  color: var(--text-primary);
}

.type-meta {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin: 1rem 0;
}

.meta-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
}

.meta-label {
  font-weight: 600;
  color: var(--text-secondary);
  min-width: 100px;
}

.meta-value {
  color: var(--text-primary);
}

.type-actions {
  margin-top: 1.5rem;
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

.type-card:hover .action-link {
  transform: translateX(4px);
}

.badge-secondary {
  background: rgba(100, 116, 139, 0.1);
  color: var(--secondary-color);
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

  .types-grid {
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
