import { defineStore } from 'pinia'
import { ref } from 'vue'

/**
 * Store for managing selected entries and timeseries types
 * Used for passing selections between pages (e.g., to Bulk Timeseries Inspector)
 */
export const useSelectionStore = defineStore('selection', () => {
  // State
  const selectedEntries = ref([])
  const selectedSensors = ref([])
  const selectedTimeseriesTypes = ref([])
  const sourceDataset = ref(null)

  // Actions
  function setSelectedEntries(entries, datasetName = null) {
    selectedEntries.value = entries
    sourceDataset.value = datasetName
  }

  function setSelectedSensors(sensors) {
    selectedSensors.value = sensors
  }

  function toggleTimeseriesType(typeName) {
    const index = selectedTimeseriesTypes.value.indexOf(typeName)
    if (index > -1) {
      selectedTimeseriesTypes.value.splice(index, 1)
    } else {
      selectedTimeseriesTypes.value.push(typeName)
    }
  }

  function setTimeseriesTypes(types) {
    selectedTimeseriesTypes.value = types
  }

  function clearSelections() {
    selectedEntries.value = []
    selectedSensors.value = []
    selectedTimeseriesTypes.value = []
    sourceDataset.value = null
  }

  return {
    // State
    selectedEntries,
    selectedSensors,
    selectedTimeseriesTypes,
    sourceDataset,

    // Actions
    setSelectedEntries,
    setSelectedSensors,
    toggleTimeseriesType,
    setTimeseriesTypes,
    clearSelections
  }
})
