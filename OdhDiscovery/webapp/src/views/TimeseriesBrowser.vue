<template>
  <div class="timeseries-browser">
    <div class="container">
      <div class="page-header">
        <h1>Timeseries Browser</h1>
        <p>Explore available timeseries measurement types</p>
      </div>

      <div v-if="loading" class="loading">
        <div class="spinner"></div>
      </div>

      <div v-else class="types-grid grid grid-3">
        <router-link
          v-for="type in types"
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
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useTimeseriesStore } from '../stores/timeseriesStore'

const timeseriesStore = useTimeseriesStore()
const loading = ref(false)
const types = ref([])

onMounted(async () => {
  await loadTypes()
})

async function loadTypes() {
  loading.value = true
  try {
    const result = await timeseriesStore.loadTypes({ include_sensors: true, limit: 100 })
    types.value = result.types || []
  } finally {
    loading.value = false
  }
}

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
}

.page-header p {
  color: var(--text-secondary);
  font-size: 1.125rem;
}

.types-grid {
  margin-top: 2rem;
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
</style>
