/**
 * Recursively get all field paths from an object
 * @param {object} obj - Object to analyze
 * @param {string} prefix - Path prefix
 * @returns {Set<string>} Set of field paths
 */
function getAllFieldPaths(obj, prefix = '') {
  const paths = new Set()

  if (obj === null || obj === undefined) {
    return paths
  }

  if (typeof obj !== 'object') {
    return paths
  }

  if (Array.isArray(obj)) {
    paths.add(prefix)
    if (obj.length > 0) {
      const itemPaths = getAllFieldPaths(obj[0], `${prefix}[]`)
      itemPaths.forEach(path => paths.add(path))
    }
  } else {
    Object.keys(obj).forEach(key => {
      const fullPath = prefix ? `${prefix}.${key}` : key
      paths.add(fullPath)

      const childPaths = getAllFieldPaths(obj[key], fullPath)
      childPaths.forEach(path => paths.add(path))
    })
  }

  return paths
}

/**
 * Get value at a specific path in an object
 * @param {object} obj - Object to traverse
 * @param {string} path - Dot notation path
 * @returns {*} Value at path or undefined
 */
export function getValueAtPath(obj, path) {
  if (!path || !obj) return undefined

  const parts = path.split(/\.|\[\]\.?/)
  let current = obj

  for (const part of parts) {
    if (part === '') continue

    if (Array.isArray(current)) {
      current = current[0]
    }

    if (current && typeof current === 'object' && part in current) {
      current = current[part]
    } else {
      return undefined
    }
  }

  return current
}

/**
 * Analyze dataset structure and completeness
 * @param {Array} items - Array of dataset items
 * @returns {object} Analysis result
 */
export function analyzeDataset(items) {
  if (!items || items.length === 0) {
    return {
      totalItems: 0,
      fields: [],
      structure: {}
    }
  }

  // Collect all possible field paths from all items
  const allPaths = new Set()
  items.forEach(item => {
    const paths = getAllFieldPaths(item)
    paths.forEach(path => allPaths.add(path))
  })

  // Analyze each field
  const fields = []
  allPaths.forEach(path => {
    const fieldAnalysis = analyzeField(items, path)
    fields.push({
      path,
      ...fieldAnalysis
    })
  })

  // Sort fields by path
  fields.sort((a, b) => a.path.localeCompare(b.path))

  return {
    totalItems: items.length,
    fields,
    structure: buildStructure(fields)
  }
}

/**
 * Analyze a specific field across all items
 * @param {Array} items - Array of items
 * @param {string} path - Field path
 * @returns {object} Field analysis
 */
function analyzeField(items, path) {
  let presentCount = 0
  let nullCount = 0
  let emptyCount = 0
  const examples = []
  const types = new Set()
  const uniqueValues = new Set()

  items.forEach(item => {
    const value = getValueAtPath(item, path)

    if (value === undefined) {
      // Field doesn't exist
      return
    }

    presentCount++

    if (value === null) {
      nullCount++
      return
    }

    if (value === '' || (Array.isArray(value) && value.length === 0)) {
      emptyCount++
      return
    }

    // Track type
    const type = Array.isArray(value) ? 'array' : typeof value
    types.add(type)

    // Collect examples (limit to 5 unique)
    if (examples.length < 5) {
      const valueStr = typeof value === 'object' ? JSON.stringify(value) : String(value)
      if (!uniqueValues.has(valueStr)) {
        uniqueValues.add(valueStr)
        examples.push(value)
      }
    }
  })

  const completenessPercentage = items.length > 0
    ? ((presentCount - nullCount - emptyCount) / items.length * 100).toFixed(2)
    : 0

  return {
    presentCount,
    nullCount,
    emptyCount,
    completeness: parseFloat(completenessPercentage),
    types: Array.from(types),
    examples: examples.slice(0, 3), // Limit to 3 examples
    sampleValues: examples.length
  }
}

/**
 * Build hierarchical structure from flat field list
 * @param {Array} fields - Flat list of fields
 * @returns {object} Hierarchical structure
 */
function buildStructure(fields) {
  const structure = {}

  fields.forEach(field => {
    const parts = field.path.split(/\.|\[\]\.?/).filter(p => p)
    let current = structure

    parts.forEach((part, index) => {
      if (!current[part]) {
        current[part] = {
          path: field.path,
          completeness: field.completeness,
          types: field.types,
          isLeaf: index === parts.length - 1,
          children: {}
        }
      }
      current = current[part].children
    })
  })

  return structure
}

/**
 * Analyze timeseries attachment to dataset entries
 * @param {Array} entries - Dataset entries
 * @param {string} timeseriesField - Field containing timeseries data
 * @returns {object} Timeseries analysis
 */
export function analyzeTimeseriesAttachment(entries, timeseriesField = 'Mapping') {
  if (!entries || entries.length === 0) {
    return {
      attachmentRate: 0,
      totalWithTimeseries: 0,
      timeseriesTypes: [],
      typeFrequency: {}
    }
  }

  let totalWithTimeseries = 0
  const typeFrequency = {}

  entries.forEach(entry => {
    const mapping = getValueAtPath(entry, timeseriesField)
    if (mapping && typeof mapping === 'object') {
      const hasTimeseries = Object.keys(mapping).some(key =>
        key.toLowerCase().includes('timeseries') ||
        key.toLowerCase().includes('sensor')
      )

      if (hasTimeseries) {
        totalWithTimeseries++
        // Count specific types if available
        Object.keys(mapping).forEach(key => {
          typeFrequency[key] = (typeFrequency[key] || 0) + 1
        })
      }
    }
  })

  const attachmentRate = (totalWithTimeseries / entries.length * 100).toFixed(2)

  return {
    attachmentRate: parseFloat(attachmentRate),
    totalWithTimeseries,
    timeseriesTypes: Object.keys(typeFrequency),
    typeFrequency
  }
}

/**
 * Format value for display
 * @param {*} value - Value to format
 * @returns {string} Formatted value
 */
export function formatValue(value) {
  if (value === null) return 'null'
  if (value === undefined) return 'undefined'
  if (value === '') return '(empty string)'
  if (Array.isArray(value)) return `Array[${value.length}]`
  if (typeof value === 'object') return JSON.stringify(value, null, 2)
  return String(value)
}

/**
 * Determine if a field is likely to be a timeseries-related field
 * @param {string} fieldPath - Field path
 * @returns {boolean} True if likely timeseries-related
 */
export function isTimeseriesField(fieldPath) {
  const lower = fieldPath.toLowerCase()
  return lower.includes('timeseries') ||
         lower.includes('sensor') ||
         lower.includes('measurement') ||
         lower.includes('mapping')
}
