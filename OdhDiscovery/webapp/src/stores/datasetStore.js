import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as contentApi from '../api/contentApi'
import * as timeseriesApi from '../api/timeseriesApi'
import { analyzeDataset, analyzeTimeseriesAttachment } from '../utils/dataAnalyzer'

export const useDatasetStore = defineStore('dataset', () => {
  // State
  const datasets = ref([])
  const currentDataset = ref(null)
  const currentDatasetName = ref(null)
  const entries = ref([])
  const analysis = ref(null)
  const timeseriesAnalysis = ref(null)
  const loading = ref(false)
  const error = ref(null)
  const totalResults = ref(0)
  const currentPage = ref(1)
  const pageSize = ref(50)

  // Getters
  const totalPages = computed(() => Math.ceil(totalResults.value / pageSize.value))

  // Actions
  async function loadDatasetTypes() {
    try {
      loading.value = true
      error.value = null
      datasets.value = await contentApi.getDatasetTypes()
    } catch (err) {
      error.value = err.message
      console.error('Error loading dataset types:', err)
    } finally {
      loading.value = false
    }
  }

  async function loadDatasetEntries(datasetName, params = {}) {
    try {
      loading.value = true
      error.value = null
      currentDatasetName.value = datasetName

      const result = await contentApi.getDatasetEntries(datasetName, {
        pagenumber: currentPage.value,
        pagesize: pageSize.value,
        ...params
      })

      entries.value = result.Items || []
      totalResults.value = result.TotalResults || 0
      currentDataset.value = result

      // Analyze the dataset
      if (entries.value.length > 0) {
        analysis.value = analyzeDataset(entries.value)

        // Extract all entry Ids to use as potential sensor names
        const entryIds = entries.value.map(entry => entry.Id).filter(id => id)

        // Fetch timeseries data for all entry Ids
        if (entryIds.length > 0) {
          try {
            const timeseriesData = await fetchTimeseriesForSensors(entryIds)
            timeseriesAnalysis.value = {
              attachmentRate: timeseriesData.attachmentRate,
              totalWithTimeseries: timeseriesData.totalWithTimeseries,
              sensorNames: timeseriesData.sensorNames,
              timeseriesData: timeseriesData.timeseriesData,
              timeseriesTypes: timeseriesData.timeseriesTypes,
              typeFrequency: timeseriesData.typeFrequency
            }
          } catch (tsErr) {
            console.error('Error fetching timeseries data:', tsErr)
            // Fallback to empty analysis
            timeseriesAnalysis.value = {
              attachmentRate: 0,
              totalWithTimeseries: 0,
              sensorNames: [],
              timeseriesData: {},
              timeseriesTypes: [],
              typeFrequency: {}
            }
          }
        } else {
          timeseriesAnalysis.value = {
            attachmentRate: 0,
            totalWithTimeseries: 0,
            sensorNames: [],
            timeseriesData: {},
            timeseriesTypes: [],
            typeFrequency: {}
          }
        }
      }
    } catch (err) {
      error.value = err.message
      console.error('Error loading dataset entries:', err)
    } finally {
      loading.value = false
    }
  }

  async function fetchTimeseriesForSensors(sensorNames) {
    if (!sensorNames || sensorNames.length === 0) {
      return {
        attachmentRate: 0,
        totalWithTimeseries: 0,
        sensorNames: [],
        timeseriesData: {},
        timeseriesTypes: [],
        typeFrequency: {}
      }
    }

    try {
      // Call the timeseries API to get timeseries for these sensors
      const response = await timeseriesApi.getTimeseriesForSensors({
        sensor_names: sensorNames
      })

      const timeseriesData = {}
      const typeFrequency = {}
      const timeseriesTypesSet = new Set()
      const sensorsWithTimeseries = []

      // Process the response
      if (response.sensors) {
        response.sensors.forEach(sensor => {
          if (sensor.timeseries && sensor.timeseries.length > 0) {
            sensorsWithTimeseries.push(sensor.sensor_name)
            timeseriesData[sensor.sensor_name] = sensor.timeseries

            sensor.timeseries.forEach(ts => {
              const typeName = ts.type_name
              timeseriesTypesSet.add(typeName)

              if (!typeFrequency[typeName]) {
                typeFrequency[typeName] = 0
              }
              typeFrequency[typeName]++
            })
          }
        })
      }

      const totalWithTimeseries = sensorsWithTimeseries.length
      const attachmentRate = sensorNames.length > 0
        ? ((totalWithTimeseries / sensorNames.length) * 100).toFixed(2)
        : 0

      return {
        attachmentRate: parseFloat(attachmentRate),
        totalWithTimeseries,
        sensorNames: sensorsWithTimeseries,
        timeseriesData,
        timeseriesTypes: Array.from(timeseriesTypesSet),
        typeFrequency
      }
    } catch (err) {
      console.error('Error fetching timeseries for sensors:', err)
      return {
        attachmentRate: 0,
        totalWithTimeseries: 0,
        sensorNames: [],
        timeseriesData: {},
        timeseriesTypes: [],
        typeFrequency: {}
      }
    }
  }

  async function loadDatasetEntry(datasetName, id, params = {}) {
    try {
      loading.value = true
      error.value = null
      const entry = await contentApi.getDatasetEntry(datasetName, id, params)
      return entry
    } catch (err) {
      error.value = err.message
      console.error('Error loading dataset entry:', err)
      return null
    } finally {
      loading.value = false
    }
  }

  async function loadDatasetMetadata(datasetName) {
    try {
      const metadata = await contentApi.getDatasetMetadata(datasetName)
      return metadata
    } catch (err) {
      console.error('Error loading dataset metadata:', err)
      return null
    }
  }

  function setPage(page) {
    currentPage.value = page
  }

  function setPageSize(size) {
    pageSize.value = size
    currentPage.value = 1 // Reset to first page
  }

  function reset() {
    currentDataset.value = null
    currentDatasetName.value = null
    entries.value = []
    analysis.value = null
    timeseriesAnalysis.value = null
    currentPage.value = 1
    totalResults.value = 0
  }

  return {
    // State
    datasets,
    currentDataset,
    currentDatasetName,
    entries,
    analysis,
    timeseriesAnalysis,
    loading,
    error,
    totalResults,
    currentPage,
    pageSize,

    // Getters
    totalPages,

    // Actions
    loadDatasetTypes,
    loadDatasetEntries,
    loadDatasetEntry,
    loadDatasetMetadata,
    setPage,
    setPageSize,
    reset
  }
})
