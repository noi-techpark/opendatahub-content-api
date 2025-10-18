<template>
  <div class="json-viewer">
    <div class="json-controls">
      <input
        v-model="searchQuery"
        type="text"
        class="input"
        placeholder="Search in JSON..."
      />
      <div class="control-buttons">
        <button @click="expandAll" class="btn btn-outline btn-sm">Expand All</button>
        <button @click="collapseAll" class="btn btn-outline btn-sm">Collapse All</button>
        <button @click="copyJson" class="btn btn-outline btn-sm">
          {{ copied ? 'Copied!' : 'Copy JSON' }}
        </button>
      </div>
    </div>
    <div class="json-content">
      <JsonNode
        :data="data"
        :name="'root'"
        :path="''"
        :depth="0"
        :expandedPaths="expandedPaths"
        :searchQuery="searchQuery"
        @toggle="togglePath"
      />
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import JsonNode from './JsonNode.vue'

const props = defineProps({
  data: {
    type: [Object, Array],
    required: true
  },
  initialDepth: {
    type: Number,
    default: 2
  }
})

const searchQuery = ref('')
const copied = ref(false)
const expandedPaths = ref(new Set())

// Initialize with paths expanded to initialDepth
function initializeExpandedPaths(obj, currentPath = '', currentDepth = 0) {
  if (currentDepth >= props.initialDepth) return

  if (obj && typeof obj === 'object') {
    expandedPaths.value.add(currentPath || 'root')

    const keys = Array.isArray(obj) ? obj.map((_, i) => i) : Object.keys(obj)
    keys.forEach(key => {
      const newPath = currentPath ? `${currentPath}.${key}` : String(key)
      initializeExpandedPaths(obj[key], newPath, currentDepth + 1)
    })
  }
}

// Initialize on mount
initializeExpandedPaths(props.data)

function togglePath(path) {
  if (expandedPaths.value.has(path)) {
    expandedPaths.value.delete(path)
  } else {
    expandedPaths.value.add(path)
  }
  // Trigger reactivity
  expandedPaths.value = new Set(expandedPaths.value)
}

function getAllPaths(obj, currentPath = '') {
  const paths = new Set()

  if (obj && typeof obj === 'object') {
    paths.add(currentPath || 'root')

    const keys = Array.isArray(obj) ? obj.map((_, i) => i) : Object.keys(obj)
    keys.forEach(key => {
      const newPath = currentPath ? `${currentPath}.${key}` : String(key)
      const childPaths = getAllPaths(obj[key], newPath)
      childPaths.forEach(p => paths.add(p))
    })
  }

  return paths
}

function expandAll() {
  expandedPaths.value = getAllPaths(props.data)
}

function collapseAll() {
  expandedPaths.value = new Set()
}

async function copyJson() {
  try {
    await navigator.clipboard.writeText(JSON.stringify(props.data, null, 2))
    copied.value = true
    setTimeout(() => {
      copied.value = false
    }, 2000)
  } catch (err) {
    console.error('Failed to copy:', err)
  }
}

// Reset expanded paths when data changes
watch(() => props.data, () => {
  expandedPaths.value = new Set()
  initializeExpandedPaths(props.data)
})
</script>

<style scoped>
.json-viewer {
  background: var(--surface-color);
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  overflow: hidden;
}

.json-controls {
  display: flex;
  gap: 1rem;
  padding: 1rem;
  border-bottom: 1px solid var(--border-color);
  background: var(--bg-color);
}

.json-controls input {
  flex: 1;
  max-width: 300px;
}

.control-buttons {
  display: flex;
  gap: 0.5rem;
  margin-left: auto;
}

.btn-sm {
  padding: 0.375rem 0.75rem;
  font-size: 0.875rem;
}

.json-content {
  padding: 1rem;
  overflow: auto;
  max-height: 600px;
  font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
  font-size: 0.875rem;
}
</style>
