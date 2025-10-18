import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as contentApi from '../api/contentApi'
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
        timeseriesAnalysis.value = analyzeTimeseriesAttachment(entries.value)
      }
    } catch (err) {
      error.value = err.message
      console.error('Error loading dataset entries:', err)
    } finally {
      loading.value = false
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
