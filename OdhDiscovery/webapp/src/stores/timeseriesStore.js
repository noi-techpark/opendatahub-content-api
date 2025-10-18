import { defineStore } from 'pinia'
import { ref } from 'vue'
import * as timeseriesApi from '../api/timeseriesApi'

export const useTimeseriesStore = defineStore('timeseries', () => {
  // State
  const types = ref([])
  const currentType = ref(null)
  const sensors = ref([])
  const measurements = ref([])
  const loading = ref(false)
  const error = ref(null)

  // Actions
  async function loadTypes(params = {}) {
    try {
      loading.value = true
      error.value = null
      const result = await timeseriesApi.getTypes({
        include_sensors: true,
        limit: 100,
        ...params
      })
      types.value = result.types || []
      return result
    } catch (err) {
      error.value = err.message
      console.error('Error loading types:', err)
    } finally {
      loading.value = false
    }
  }

  async function loadTypeByName(name) {
    try {
      loading.value = true
      error.value = null
      const result = await timeseriesApi.getTypeByName(name)
      currentType.value = result
      return result
    } catch (err) {
      error.value = err.message
      console.error('Error loading type:', err)
      return null
    } finally {
      loading.value = false
    }
  }

  async function discoverSensors(payload) {
    try {
      loading.value = true
      error.value = null
      const result = await timeseriesApi.discoverSensors(payload)
      sensors.value = result.sensors || []
      return result
    } catch (err) {
      error.value = err.message
      console.error('Error discovering sensors:', err)
      return null
    } finally {
      loading.value = false
    }
  }

  async function loadSensorTimeseries(sensorName, params = {}) {
    try {
      loading.value = true
      error.value = null
      const result = await timeseriesApi.getSensorTimeseries(sensorName, params)
      return result
    } catch (err) {
      error.value = err.message
      console.error('Error loading sensor timeseries:', err)
      return null
    } finally {
      loading.value = false
    }
  }

  async function loadLatestMeasurements(sensorNames, typeNames = null) {
    try {
      loading.value = true
      error.value = null
      const result = await timeseriesApi.getLatestMeasurementsPost({
        sensor_names: sensorNames,
        type_names: typeNames
      })
      measurements.value = result.measurements || []
      return result
    } catch (err) {
      error.value = err.message
      console.error('Error loading latest measurements:', err)
      return null
    } finally {
      loading.value = false
    }
  }

  async function loadHistoricalMeasurements(payload) {
    try {
      loading.value = true
      error.value = null
      const result = await timeseriesApi.getHistoricalMeasurementsPost(payload)
      measurements.value = result.measurements || []
      return result
    } catch (err) {
      error.value = err.message
      console.error('Error loading historical measurements:', err)
      return null
    } finally {
      loading.value = false
    }
  }

  async function getTypesForSensors(sensorNames, distinct = false) {
    try {
      loading.value = true
      error.value = null
      const result = await timeseriesApi.getTypesForSensors({
        sensor_names: sensorNames,
        distinct
      })
      return result
    } catch (err) {
      error.value = err.message
      console.error('Error getting types for sensors:', err)
      return null
    } finally {
      loading.value = false
    }
  }

  function reset() {
    types.value = []
    currentType.value = null
    sensors.value = []
    measurements.value = []
    error.value = null
  }

  return {
    // State
    types,
    currentType,
    sensors,
    measurements,
    loading,
    error,

    // Actions
    loadTypes,
    loadTypeByName,
    discoverSensors,
    loadSensorTimeseries,
    loadLatestMeasurements,
    loadHistoricalMeasurements,
    getTypesForSensors,
    reset
  }
})
