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
  const currentMetadata = ref(null) // Store metadata for current dataset
  const currentAnalysisFilters = ref(null) // Track filters used for current analysis

  // Paginated entries (for table view)
  const entries = ref([])

  // Full dataset (for analysis) - cached to avoid duplicate fetches
  const allEntries = ref([])
  const allEntriesLoading = ref(false)

  const analysis = ref(null)
  const timeseriesAnalysis = ref(null)
  const loading = ref(false)
  const error = ref(null)
  const totalResults = ref(0)
  const currentPage = ref(1)
  const pageSize = ref(50)

  // Getters
  const totalPages = computed(() => Math.ceil(totalResults.value / pageSize.value))

  async function reset() {
    allEntries.value = []
    analysis.value = null
    timeseriesAnalysis.value = null
    currentMetadata.value = null
    currentAnalysisFilters.value = null
  }

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

      // Check if dataset has changed (need to reload metadata and analysis)
      const datasetChanged = currentDatasetName.value !== datasetName

      // Clear cached data when dataset changes
      if (datasetChanged) {
        reset();
      }

      currentDatasetName.value = datasetName

      // Check if filters have changed (need to reload analysis)
      const currentFilters = JSON.stringify({
        searchfilter: params.searchfilter || '',
        rawfilter: params.rawfilter || ''
      })
      const filtersChanged = currentAnalysisFilters.value !== currentFilters

      // Fetch metadata for this dataset (if not already loaded or dataset changed)
      if (!currentMetadata.value || currentMetadata.value.Shortname !== datasetName) {
        try {
          currentMetadata.value = await contentApi.getMetadataByShortname(datasetName)
        } catch (metaErr) {
          console.warn('Could not fetch metadata for dataset:', datasetName, metaErr)
          currentMetadata.value = null
        }
      }

      // Call API with metadata - the URL will be built from metadata.BaseUrl + metadata.PathParam
      // datasetName is used as fallback if metadata is not available
      const result = await contentApi.getDatasetEntries(datasetName, {
        pagenumber: currentPage.value,
        pagesize: pageSize.value,
        ...params
      }, currentMetadata.value)

      // Handle different response formats
      entries.value = result.Items || result.data || []
      currentDataset.value = result

      // Load analysis when:
      // 1. Dataset changes
      // 2. Filters change (searchfilter or rawfilter)
      // 3. Analysis is not available
      // This prevents re-analyzing on page-only changes
      if (datasetChanged || filtersChanged || !analysis.value) {
        currentAnalysisFilters.value = currentFilters
        await loadDatasetAnalysis(datasetName, params)
      }

      // Set totalResults from analysis (which has the complete dataset count)
      // Fallback to result.TotalResults if analysis not available
      if (analysis.value && analysis.value.totalItems !== undefined) {
        totalResults.value = analysis.value.totalItems
      } else {
        totalResults.value = result.TotalResults || (result.data?.length || 0)
      }
    } catch (err) {
      error.value = err.message
      console.error('Error loading dataset entries:', err)
    } finally {
      loading.value = false
    }
  }

  async function loadAllEntries(datasetName, params = {}) {
    try {
      allEntriesLoading.value = true
      console.log('Loading all entries for dataset:', datasetName)

      // Fetch ALL entries (cached for both dataset analysis and distinct analysis)
      allEntries.value = await contentApi.getAllFilteredEntries(
        datasetName,
        {
          searchfilter: params.searchfilter || undefined,
          rawfilter: params.rawfilter || undefined
        },
        (progress) => {
          console.log(`Fetching all entries: page ${progress.current} of ${progress.total}`)
        },
        currentMetadata.value
      )

      console.log(`Loaded ${allEntries.value.length} entries into cache`)
      return allEntries.value
    } catch (err) {
      console.error('Error loading all entries:', err)
      throw err
    } finally {
      allEntriesLoading.value = false
    }
  }

  async function loadDatasetAnalysis(datasetName, params = {}) {
    try {
      console.log('Loading analysis for entire dataset:', datasetName, params)

      // Use cached allEntries if available, otherwise fetch
      let allData = allEntries.value
      if (!allData || allData.length === 0) {
        console.log('FRASH FETCH! Loading analysis for entire dataset:', datasetName, params)
        allData = await loadAllEntries(datasetName, params)
      }

      console.log(`Analyzing ${allData.length} entries`)

      // Analyze the complete dataset
      if (allData.length > 0) {
        analysis.value = analyzeDataset(allData)

        // Extract all entry Ids to use as potential sensor names
        const entryIds = allData.map(entry => entry.Id).filter(id => id)

        // Fetch timeseries data for all entry Ids (no sampling)
        if (entryIds.length > 0) {
          try {
            console.log(`Fetching timeseries data for ${entryIds.length} sensors`)
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
      } else {
        // No entries found
        analysis.value = {
          totalItems: 0,
          fields: [],
          structure: {}
        }
        timeseriesAnalysis.value = {
          attachmentRate: 0,
          totalWithTimeseries: 0,
          sensorNames: [],
          timeseriesData: {},
          timeseriesTypes: [],
          typeFrequency: {}
        }
      }
    } catch (err) {
      console.error('Error loading dataset analysis:', err)
      // Set empty analysis on error
      analysis.value = null
      timeseriesAnalysis.value = null
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
    currentMetadata.value = null
    currentAnalysisFilters.value = null
    entries.value = []
    allEntries.value = []
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
    currentMetadata,
    entries,
    allEntries,
    allEntriesLoading,
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
    loadAllEntries,
    loadDatasetEntry,
    loadDatasetMetadata,
    setPage,
    setPageSize,
    reset
  }
})
