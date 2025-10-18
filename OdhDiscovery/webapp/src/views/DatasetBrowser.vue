<template>
  <div class="dataset-browser">
    <div class="container">
      <div class="page-header">
        <h1>Dataset Browser</h1>
        <p>Explore available datasets in the Open Data Hub Content API</p>
      </div>

      <div v-if="loading" class="loading">
        <div class="spinner"></div>
      </div>

      <div v-else class="datasets-grid grid grid-3">
        <router-link
          v-for="dataset in datasetsWithMeta"
          :key="dataset.name"
          :to="`/datasets/${dataset.name}`"
          class="dataset-card card"
        >
          <h3>{{ dataset.name }}</h3>
          <div v-if="dataset.metadata" class="dataset-meta">
            <div class="meta-item">
              <span class="meta-label">Total Entries:</span>
              <span class="meta-value">{{ formatNumber(dataset.metadata.totalResults) }}</span>
            </div>
            <div class="meta-item">
              <span class="meta-label">Pages:</span>
              <span class="meta-value">{{ formatNumber(dataset.metadata.totalPages) }}</span>
            </div>
          </div>
          <div v-else-if="dataset.loading" class="dataset-loading">
            Loading metadata...
          </div>
          <div v-else-if="dataset.error" class="dataset-error">
            Unable to load metadata
          </div>
          <div class="dataset-actions">
            <span class="action-link">Inspect Dataset →</span>
          </div>
        </router-link>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useDatasetStore } from '../stores/datasetStore'
import * as contentApi from '../api/contentApi'

const datasetStore = useDatasetStore()
const loading = ref(true)
const datasetsWithMeta = ref([])

onMounted(async () => {
  await loadDatasets()
})

async function loadDatasets() {
  loading.value = true

  try {
    await datasetStore.loadDatasetTypes()

    // Initialize datasets with loading state
    datasetsWithMeta.value = datasetStore.datasets.map(ds => ({
      ...ds,
      metadata: null,
      loading: true,
      error: null
    }))

    // Load metadata for each dataset in parallel
    const metadataPromises = datasetsWithMeta.value.map(async (dataset, index) => {
      try {
        const metadata = await contentApi.getDatasetMetadata(dataset.name)
        datasetsWithMeta.value[index].metadata = metadata
        datasetsWithMeta.value[index].loading = false
      } catch (err) {
        console.error(`Error loading metadata for ${dataset.name}:`, err)
        datasetsWithMeta.value[index].error = err.message
        datasetsWithMeta.value[index].loading = false
      }
    })

    await Promise.allSettled(metadataPromises)
  } catch (err) {
    console.error('Error loading datasets:', err)
  } finally {
    loading.value = false
  }
}

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

.datasets-grid {
  margin-top: 2rem;
}

.dataset-card {
  text-decoration: none;
  color: inherit;
  transition: all 0.3s ease;
  cursor: pointer;
  position: relative;
  overflow: hidden;
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

.dataset-card h3 {
  font-size: 1.25rem;
  margin-bottom: 1rem;
  color: var(--text-primary);
}

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
  padding: 1rem;
  text-align: center;
  color: var(--text-secondary);
  font-size: 0.875rem;
}

.dataset-error {
  color: var(--danger-color);
}

.dataset-actions {
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

.dataset-card:hover .action-link {
  transform: translateX(4px);
}
</style>
