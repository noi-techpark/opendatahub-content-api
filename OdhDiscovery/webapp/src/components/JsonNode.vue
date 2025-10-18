<template>
  <div class="json-node" :style="{ paddingLeft: `${depth * 16}px` }">
    <div v-if="isObject || isArray" class="node-collapsible">
      <span class="toggle-btn" @click="toggle">
        <span v-if="isExpanded">▼</span>
        <span v-else>▶</span>
      </span>
      <span class="node-key" v-if="name !== 'root'">{{ name }}:</span>
      <span class="node-bracket">{{ isArray ? '[' : '{' }}</span>
      <span v-if="!isExpanded" class="node-preview">
        {{ isArray ? `${itemCount} items` : `${itemCount} properties` }}
      </span>
      <span v-if="!isExpanded" class="node-bracket">{{ isArray ? ']' : '}' }}</span>
      <span v-if="!isExpanded && name !== 'root'" class="node-comma">,</span>
    </div>

    <div v-if="isExpanded && (isObject || isArray)" class="node-children">
      <JsonNode
        v-for="(value, key) in dataEntries"
        :key="key"
        :data="value"
        :name="String(key)"
        :path="childPath(key)"
        :depth="depth + 1"
        :expandedPaths="expandedPaths"
        :searchQuery="searchQuery"
        @toggle="$emit('toggle', $event)"
      />
      <div class="node-close" :style="{ paddingLeft: `${depth * 16}px` }">
        <span class="node-bracket">{{ isArray ? ']' : '}' }}</span>
        <span v-if="name !== 'root'" class="node-comma">,</span>
      </div>
    </div>

    <div v-if="!isObject && !isArray" class="node-primitive">
      <span class="node-key">{{ name }}:</span>
      <span class="node-value" :class="`value-${valueType}`">{{ formattedValue }}</span>
      <span class="node-comma">,</span>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  data: {
    type: [Object, Array, String, Number, Boolean, null],
    default: null
  },
  name: {
    type: String,
    required: true
  },
  path: {
    type: String,
    required: true
  },
  depth: {
    type: Number,
    default: 0
  },
  expandedPaths: {
    type: Set,
    required: true
  },
  searchQuery: {
    type: String,
    default: ''
  }
})

const emit = defineEmits(['toggle'])

const currentPath = computed(() => {
  return props.path || props.name
})

const isExpanded = computed(() => {
  return props.expandedPaths.has(currentPath.value)
})

const isObject = computed(() => {
  return props.data !== null && typeof props.data === 'object' && !Array.isArray(props.data)
})

const isArray = computed(() => {
  return Array.isArray(props.data)
})

const dataEntries = computed(() => {
  if (isArray.value) {
    return props.data
  }
  if (isObject.value) {
    return props.data
  }
  return {}
})

const itemCount = computed(() => {
  if (isArray.value) {
    return props.data.length
  }
  if (isObject.value) {
    return Object.keys(props.data).length
  }
  return 0
})

const valueType = computed(() => {
  if (props.data === null) return 'null'
  if (props.data === undefined) return 'undefined'
  if (typeof props.data === 'boolean') return 'boolean'
  if (typeof props.data === 'number') return 'number'
  if (typeof props.data === 'string') return 'string'
  return 'unknown'
})

const formattedValue = computed(() => {
  if (props.data === null) return 'null'
  if (props.data === undefined) return 'undefined'
  if (typeof props.data === 'boolean') return props.data ? 'true' : 'false'
  if (typeof props.data === 'number') return String(props.data)
  if (typeof props.data === 'string') return `"${props.data}"`
  return String(props.data)
})

function childPath(key) {
  return props.path ? `${props.path}.${key}` : String(key)
}

function toggle() {
  emit('toggle', currentPath.value)
}
</script>

<style scoped>
.json-node {
  line-height: 1.6;
  user-select: text;
}

.node-collapsible {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.toggle-btn {
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 16px;
  height: 16px;
  color: var(--text-secondary);
  user-select: none;
  font-size: 0.75rem;
}

.toggle-btn:hover {
  color: var(--primary-color);
}

.node-key {
  color: #9876aa;
  font-weight: 500;
  margin-right: 0.25rem;
}

.node-bracket {
  color: var(--text-secondary);
  font-weight: bold;
}

.node-preview {
  color: var(--text-secondary);
  font-style: italic;
  margin: 0 0.5rem;
  font-size: 0.85em;
}

.node-comma {
  color: var(--text-secondary);
}

.node-children {
  display: flex;
  flex-direction: column;
}

.node-close {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  padding-left: 16px;
}

.node-primitive {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.node-value {
  font-weight: 400;
}

.value-string {
  color: #00a67d;
}

.value-number {
  color: #dd7202;
}

.value-boolean {
  color: #0079f2;
}

.value-null {
  color: #999;
  font-style: italic;
}

.value-undefined {
  color: #999;
  font-style: italic;
}
</style>
