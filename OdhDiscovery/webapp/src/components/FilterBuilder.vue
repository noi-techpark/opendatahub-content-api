<template>
  <div v-if="isOpen" class="modal-overlay" @click.self="close">
    <div class="modal-content">
      <div class="modal-header">
        <h3>Build Filter</h3>
        <button @click="close" class="btn-close">&times;</button>
      </div>

      <div class="modal-body">
        <div class="filter-rules">
          <div v-if="rules.length === 0" class="empty-state">
            <p>No filter rules yet. Click "Add Rule" to start building your filter.</p>
          </div>

          <div v-for="(rule, index) in rules" :key="index" class="filter-rule">
            <div class="rule-header">
              <span class="rule-number">Rule {{ index + 1 }}</span>
              <button @click="removeRule(index)" class="btn btn-sm btn-outline">Remove</button>
            </div>

            <div class="rule-content">
              <div class="form-group">
                <label>Field</label>
                <select v-model="rule.field" class="input" @change="onFieldChange(rule)">
                  <option value="">Select a field...</option>
                  <optgroup v-for="group in groupedFields" :key="group.label" :label="group.label">
                    <option v-for="field in group.fields" :key="field.path" :value="field.path">
                      {{ field.path }} ({{ field.types.join(', ') }})
                    </option>
                  </optgroup>
                </select>
              </div>

              <div class="form-group">
                <label>Operation</label>
                <select v-model="rule.operation" class="input">
                  <option value="">Select operation...</option>
                  <optgroup label="Comparison">
                    <option value="eq">Equals (eq)</option>
                    <option value="ne">Not Equals (ne)</option>
                    <option value="gt">Greater Than (gt)</option>
                    <option value="gte">Greater or Equal (gte)</option>
                    <option value="lt">Less Than (lt)</option>
                    <option value="lte">Less or Equal (lte)</option>
                  </optgroup>
                  <optgroup label="Text">
                    <option value="like">Contains (like)</option>
                    <option value="sw">Starts With (sw)</option>
                    <option value="ew">Ends With (ew)</option>
                  </optgroup>
                  <optgroup label="Existence">
                    <option value="isnull">Is Null</option>
                    <option value="isnotnull">Is Not Null</option>
                  </optgroup>
                  <optgroup label="List">
                    <option value="in">In (in)</option>
                    <option value="nin">Not In (nin)</option>
                  </optgroup>
                </select>
              </div>

              <div v-if="!isNullOperation(rule.operation)" class="form-group">
                <label>Value</label>
                <input
                  v-if="rule.fieldType === 'boolean'"
                  type="checkbox"
                  v-model="rule.value"
                  class="checkbox"
                />
                <input
                  v-else-if="rule.fieldType === 'number'"
                  type="number"
                  v-model="rule.value"
                  class="input"
                  :placeholder="getValuePlaceholder(rule)"
                />
                <textarea
                  v-else-if="isListOperation(rule.operation)"
                  v-model="rule.value"
                  class="input"
                  rows="3"
                  :placeholder="getValuePlaceholder(rule)"
                ></textarea>
                <input
                  v-else
                  type="text"
                  v-model="rule.value"
                  class="input"
                  :placeholder="getValuePlaceholder(rule)"
                />
                <small v-if="rule.examples && rule.examples.length > 0" class="field-hint">
                  Examples: {{ rule.examples.slice(0, 3).join(', ') }}
                </small>
              </div>

              <div v-if="index < rules.length - 1" class="form-group">
                <label>Combine with next rule using</label>
                <div class="radio-group">
                  <label class="radio-label">
                    <input type="radio" :name="`combinator-${index}`" value="and" v-model="rule.combinator" />
                    AND
                  </label>
                  <label class="radio-label">
                    <input type="radio" :name="`combinator-${index}`" value="or" v-model="rule.combinator" />
                    OR
                  </label>
                </div>
              </div>
            </div>
          </div>
        </div>

        <button @click="addRule" class="btn btn-outline btn-block">
          + Add Rule
        </button>

        <div v-if="rules.length > 0" class="preview-section">
          <h4>Filter Preview</h4>
          <div class="code-block">
            <code>{{ buildFilterExpression() }}</code>
          </div>
        </div>
      </div>

      <div class="modal-footer">
        <button @click="close" class="btn btn-outline">Cancel</button>
        <button @click="apply" class="btn btn-primary" :disabled="!isValid">
          Apply Filter
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'

const props = defineProps({
  isOpen: {
    type: Boolean,
    default: false
  },
  fields: {
    type: Array,
    default: () => []
  },
  initialFilter: {
    type: String,
    default: ''
  }
})

const emit = defineEmits(['close', 'apply'])

const rules = ref([])

// Group fields by category
const groupedFields = computed(() => {
  if (!props.fields || props.fields.length === 0) return []

  const topLevel = props.fields.filter(f => !f.path.includes('.') && !f.path.includes('[]'))
  const nested = props.fields.filter(f => f.path.includes('.') && !f.path.includes('[]'))
  const arrays = props.fields.filter(f => f.path.includes('[]'))

  const groups = []

  if (topLevel.length > 0) {
    groups.push({ label: 'Top Level Fields', fields: topLevel })
  }

  if (nested.length > 0) {
    groups.push({ label: 'Nested Fields', fields: nested })
  }

  if (arrays.length > 0) {
    groups.push({ label: 'Array Fields', fields: arrays })
  }

  return groups
})

const isValid = computed(() => {
  if (rules.value.length === 0) return false

  return rules.value.every(rule => {
    if (!rule.field || !rule.operation) return false
    if (isNullOperation(rule.operation)) return true
    return rule.value !== null && rule.value !== undefined && rule.value !== ''
  })
})

watch(() => props.isOpen, (newVal) => {
  if (newVal && rules.value.length === 0) {
    // Initialize with one empty rule
    addRule()
  }
})

function addRule() {
  rules.value.push({
    field: '',
    operation: '',
    value: '',
    combinator: 'and',
    fieldType: 'string',
    examples: []
  })
}

function removeRule(index) {
  rules.value.splice(index, 1)
}

function onFieldChange(rule) {
  const field = props.fields.find(f => f.path === rule.field)
  if (field) {
    rule.examples = field.examples || []
    // Determine field type from types array
    if (field.types.includes('number')) {
      rule.fieldType = 'number'
    } else if (field.types.includes('boolean')) {
      rule.fieldType = 'boolean'
      rule.value = false
    } else {
      rule.fieldType = 'string'
    }
  }
}

function isNullOperation(operation) {
  return operation === 'isnull' || operation === 'isnotnull'
}

function isListOperation(operation) {
  return operation === 'in' || operation === 'nin'
}

function getValuePlaceholder(rule) {
  if (isListOperation(rule.operation)) {
    return 'Enter comma-separated values, e.g., value1,value2,value3'
  }
  if (rule.fieldType === 'number') {
    return 'Enter a number'
  }
  return 'Enter a value'
}

function buildFilterExpression() {
  if (rules.value.length === 0) return ''

  const expressions = rules.value.map((rule, index) => {
    if (!rule.field || !rule.operation) return null

    let expr = ''

    if (isNullOperation(rule.operation)) {
      expr = `${rule.operation}(${rule.field})`
    } else if (isListOperation(rule.operation)) {
      // Split comma-separated values
      const values = rule.value.split(',').map(v => v.trim()).filter(v => v)
      const valueList = values.map(v => {
        if (rule.fieldType === 'number') return v
        return `"${v}"`
      }).join(',')
      expr = `${rule.operation}(${rule.field},[${valueList}])`
    } else {
      let value = rule.value
      if (rule.fieldType === 'boolean') {
        value = rule.value ? 'true' : 'false'
      } else if (rule.fieldType === 'number') {
        value = rule.value
      } else {
        value = `"${rule.value}"`
      }
      expr = `${rule.operation}(${rule.field},${value})`
    }

    return expr
  }).filter(e => e !== null)

  if (expressions.length === 0) return ''
  if (expressions.length === 1) return expressions[0]

  // Build expression with combinators
  let result = expressions[0]
  for (let i = 1; i < expressions.length; i++) {
    const combinator = rules.value[i - 1].combinator
    result = `${combinator}(${result},${expressions[i]})`
  }

  return result
}

function apply() {
  const filter = buildFilterExpression()
  emit('apply', filter)
  close()
}

function close() {
  emit('close')
}

function reset() {
  rules.value = []
}

defineExpose({ reset })
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-content {
  background: var(--surface-color);
  border-radius: 0.5rem;
  max-width: 800px;
  width: 100%;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid var(--border-color);
}

.modal-header h3 {
  margin: 0;
  font-size: 1.25rem;
  color: var(--text-primary);
}

.btn-close {
  background: none;
  border: none;
  font-size: 2rem;
  line-height: 1;
  color: var(--text-secondary);
  cursor: pointer;
  padding: 0;
  width: 2rem;
  height: 2rem;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-close:hover {
  color: var(--text-primary);
}

.modal-body {
  flex: 1;
  overflow-y: auto;
  padding: 1.5rem;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  padding: 1.5rem;
  border-top: 1px solid var(--border-color);
}

.filter-rules {
  margin-bottom: 1.5rem;
}

.empty-state {
  text-align: center;
  padding: 2rem;
  color: var(--text-secondary);
  background: var(--bg-color);
  border-radius: 0.375rem;
}

.filter-rule {
  background: var(--bg-color);
  border: 1px solid var(--border-color);
  border-radius: 0.375rem;
  padding: 1rem;
  margin-bottom: 1rem;
}

.rule-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.rule-number {
  font-weight: 600;
  color: var(--text-primary);
}

.rule-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group label {
  font-weight: 600;
  font-size: 0.875rem;
  color: var(--text-primary);
}

.field-hint {
  font-size: 0.75rem;
  color: var(--text-secondary);
  font-style: italic;
}

.radio-group {
  display: flex;
  gap: 1.5rem;
}

.radio-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: normal;
  cursor: pointer;
}

.radio-label input[type="radio"] {
  cursor: pointer;
}

.checkbox {
  width: 1.25rem;
  height: 1.25rem;
  cursor: pointer;
}

.btn-block {
  width: 100%;
}

.preview-section {
  margin-top: 2rem;
  padding-top: 1.5rem;
  border-top: 1px solid var(--border-color);
}

.preview-section h4 {
  margin-bottom: 0.75rem;
  font-size: 1rem;
  color: var(--text-primary);
}

.code-block {
  background: var(--bg-color);
  border: 1px solid var(--border-color);
  border-radius: 0.375rem;
  padding: 1rem;
  overflow-x: auto;
}

.code-block code {
  font-family: 'Monaco', 'Menlo', monospace;
  font-size: 0.875rem;
  color: var(--primary-color);
  word-break: break-all;
  white-space: pre-wrap;
}
</style>
