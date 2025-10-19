<template>
  <div class="dataset-browser">
    <div class="container">
      <div class="page-header">
        <h1>Dataset Browser</h1>
        <p>Explore {{ totalDatasets }} available datasets from the Open Data Hub</p>
      </div>

      <!-- Search and Filters -->
      <div class="filters-bar card">
        <div class="search-box">
          <input
            v-model="searchQuery"
            type="text"
            class="input"
            placeholder="Search datasets..."
          />
        </div>

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

      <div v-if="loading" class="loading">
        <div class="spinner"></div>
      </div>

      <div v-else>
        <div class="results-header">
          <p>Showing {{ filteredDatasets.length }} of {{ totalDatasets }} datasets</p>
        </div>

        <div class="datasets-grid grid grid-3">
          <router-link
            v-for="dataset in filteredDatasets"
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
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useDatasetStore } from '../stores/datasetStore'
import * as contentApi from '../api/contentApi'

const datasetStore = useDatasetStore()
const loading = ref(true)
const datasetsWithMeta = ref([])
const searchQuery = ref('')
const selectedDataspace = ref(null)
const selectedApiType = ref(null)

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
          datasetsWithMeta.value[index].loadedMetadata = metadata
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
      ds.name.toLowerCase().includes(query) ||
      (ds.description && ds.description.toLowerCase().includes(query)) ||
      (ds.dataspace && ds.dataspace.toLowerCase().includes(query))
    )
  }

  // Filter by dataspace
  if (selectedDataspace.value) {
    filtered = filtered.filter(ds => ds.dataspace === selectedDataspace.value)
  }

  // Filter by API type
  if (selectedApiType.value) {
    filtered = filtered.filter(ds => ds.apiType === selectedApiType.value)
  }

  return filtered
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

function formatNumber(num) {
  if (!num) return '0'
  return new Intl.NumberFormat().format(num)
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
  margin-bottom: 1rem;
}

.search-box .input {
  width: 100%;
  font-size: 1rem;
}

.filter-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.filter-chips:last-child {
  margin-bottom: 0;
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

/* Responsive */
@media (max-width: 768px) {
  .filter-chips {
    font-size: 0.8rem;
  }

  .chip {
    padding: 0.375rem 0.75rem;
  }

  .datasets-grid {
    grid-template-columns: 1fr;
  }
}
</style>
