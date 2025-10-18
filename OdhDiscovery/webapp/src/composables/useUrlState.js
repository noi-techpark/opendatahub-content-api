import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

/**
 * Composable for syncing state with URL query parameters
 * Enables sharing, bookmarking, and history navigation
 */
export function useUrlState() {
  const route = useRoute()
  const router = useRouter()

  /**
   * Sync a ref with a URL query parameter
   * @param {string} key - Query parameter key
   * @param {*} initialValue - Initial value if not in URL
   * @param {Function} serialize - Function to convert value to string
   * @param {Function} deserialize - Function to convert string to value
   */
  function syncWithUrl(key, initialValue = null, serialize = String, deserialize = String) {
    // Initialize from URL if present
    const urlValue = route.query[key]
    const value = ref(urlValue ? deserialize(urlValue) : initialValue)

    // Watch for changes and update URL
    watch(value, (newValue) => {
      const query = { ...route.query }

      if (newValue === null || newValue === undefined || newValue === '' ||
          (Array.isArray(newValue) && newValue.length === 0)) {
        delete query[key]
      } else {
        query[key] = serialize(newValue)
      }

      router.replace({ query })
    })

    return value
  }

  /**
   * Sync multiple parameters at once
   * @param {Object} params - Object with key: { initial, serialize, deserialize }
   */
  function syncMultiple(params) {
    const result = {}

    Object.entries(params).forEach(([key, config]) => {
      result[key] = syncWithUrl(
        key,
        config.initial ?? null,
        config.serialize ?? String,
        config.deserialize ?? String
      )
    })

    return result
  }

  /**
   * Update multiple URL parameters at once
   * @param {Object} updates - Object with key-value pairs to update
   */
  function updateUrlParams(updates) {
    const query = { ...route.query }

    Object.entries(updates).forEach(([key, value]) => {
      if (value === null || value === undefined || value === '' ||
          (Array.isArray(value) && value.length === 0)) {
        delete query[key]
      } else {
        query[key] = typeof value === 'object' ? JSON.stringify(value) : String(value)
      }
    })

    router.replace({ query })
  }

  /**
   * Get current URL with all query parameters
   */
  function getCurrentUrl() {
    return window.location.href
  }

  /**
   * Common serializers/deserializers
   */
  const serializers = {
    json: {
      serialize: (val) => JSON.stringify(val),
      deserialize: (str) => {
        try {
          return JSON.parse(str)
        } catch {
          return null
        }
      }
    },
    number: {
      serialize: String,
      deserialize: (str) => {
        const num = Number(str)
        return isNaN(num) ? null : num
      }
    },
    boolean: {
      serialize: (val) => val ? 'true' : 'false',
      deserialize: (str) => str === 'true'
    },
    array: {
      serialize: (arr) => arr.join(','),
      deserialize: (str) => str ? str.split(',').filter(Boolean) : []
    },
    string: {
      serialize: String,
      deserialize: String
    }
  }

  return {
    syncWithUrl,
    syncMultiple,
    updateUrlParams,
    getCurrentUrl,
    serializers
  }
}

/**
 * Composable specifically for dataset inspector URL state
 */
export function useDatasetUrlState() {
  const { syncMultiple, serializers, updateUrlParams, getCurrentUrl } = useUrlState()

  const state = syncMultiple({
    page: {
      initial: 1,
      ...serializers.number
    },
    pagesize: {
      initial: 50,
      ...serializers.number
    },
    view: {
      initial: 'table',
      ...serializers.string
    },
    rawfilter: {
      initial: null,
      ...serializers.string
    },
    rawsort: {
      initial: null,
      ...serializers.string
    },
    fields: {
      initial: [],
      ...serializers.array
    },
    language: {
      initial: null,
      ...serializers.string
    },
    searchfilter: {
      initial: null,
      ...serializers.string
    },
    selectedIds: {
      initial: [],
      ...serializers.array
    }
  })

  return {
    ...state,
    updateUrlParams,
    getCurrentUrl
  }
}

/**
 * Composable specifically for timeseries inspector URL state
 */
export function useTimeseriesUrlState() {
  const { syncMultiple, serializers, updateUrlParams, getCurrentUrl } = useUrlState()

  const state = syncMultiple({
    view: {
      initial: 'table',
      ...serializers.string
    },
    filter: {
      initial: null,
      ...serializers.json
    },
    selectedSensors: {
      initial: [],
      ...serializers.array
    }
  })

  return {
    ...state,
    updateUrlParams,
    getCurrentUrl
  }
}

/**
 * Composable specifically for bulk timeseries inspector URL state
 */
export function useBulkTimeseriesUrlState() {
  const { syncMultiple, serializers, updateUrlParams, getCurrentUrl } = useUrlState()

  const state = syncMultiple({
    entries: {
      initial: [],
      ...serializers.array
    },
    sensors: {
      initial: [],
      ...serializers.array
    },
    types: {
      initial: [],
      ...serializers.array
    },
    view: {
      initial: 'formatted',
      ...serializers.string
    },
    startTime: {
      initial: null,
      ...serializers.string
    },
    endTime: {
      initial: null,
      ...serializers.string
    },
    timeIndex: {
      initial: 0,
      ...serializers.number
    }
  })

  return {
    ...state,
    updateUrlParams,
    getCurrentUrl
  }
}
