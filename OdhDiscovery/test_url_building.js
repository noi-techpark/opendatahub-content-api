// Test URL building from metadata
// This script tests the buildFullUrlFromMetadata logic

function buildFullUrlFromMetadata(metadata, params = {}) {
  if (!metadata) {
    return null
  }

  // Get ApiUrl from metadata - this already includes base filters
  const apiUrl = metadata.ApiUrl || metadata.metadata?.ApiUrl
  if (!apiUrl) {
    return null
  }

  // Parse existing URL to separate base URL and existing params
  const url = new URL(apiUrl)

  // Merge existing params with new params (new params take precedence)
  const mergedParams = new URLSearchParams(url.search)
  Object.entries(params).forEach(([key, value]) => {
    if (value !== null && value !== undefined && value !== '') {
      mergedParams.set(key, value)
    }
  })

  // Build final URL
  const queryString = mergedParams.toString()
  return `${url.origin}${url.pathname}${queryString ? `?${queryString}` : ''}`
}

// Test cases based on real metadata

// Test 1: Bicycle dataset (timeseries API)
const bicycleMetadata = {
  ApiUrl: "https://mobility.api.opendatahub.com/v2/flat/Bicycle?limit=200&offset=0&shownull=false&distinct=true"
}

console.log("Test 1: Bicycle dataset")
console.log("Expected: https://mobility.api.opendatahub.com/v2/flat/Bicycle with merged params")
const bicycleUrl = buildFullUrlFromMetadata(bicycleMetadata, {
  pagenumber: 1,
  pagesize: 50
})
console.log("Result:", bicycleUrl)
console.log()

// Test 2: Museums dataset (content API with tagfilter)
const museumsMetadata = {
  ApiUrl: "https://tourism.api.opendatahub.com/v1/ODHActivityPoi?tagfilter=museums&source=lts"
}

console.log("Test 2: Museums dataset")
console.log("Expected: https://tourism.api.opendatahub.com/v1/ODHActivityPoi with tagfilter and source preserved")
const museumsUrl = buildFullUrlFromMetadata(museumsMetadata, {
  pagenumber: 1,
  pagesize: 50,
  searchfilter: "test"
})
console.log("Result:", museumsUrl)
console.log()

// Test 3: Dataset with no filters in ApiUrl
const simpleMetadata = {
  ApiUrl: "https://tourism.opendatahub.com/v1/MetaData"
}

console.log("Test 3: MetaData dataset (no default filters)")
console.log("Expected: https://tourism.opendatahub.com/v1/MetaData with only new params")
const simpleUrl = buildFullUrlFromMetadata(simpleMetadata, {
  pagenumber: 1,
  pagesize: 100,
  removenullvalues: true
})
console.log("Result:", simpleUrl)
console.log()

// Test 4: Override existing param
const overrideMetadata = {
  ApiUrl: "https://mobility.api.opendatahub.com/v2/flat/Bicycle?limit=200&offset=0"
}

console.log("Test 4: Override existing params")
console.log("Expected: limit should be overridden to 50")
const overrideUrl = buildFullUrlFromMetadata(overrideMetadata, {
  limit: 50,
  offset: 100
})
console.log("Result:", overrideUrl)
console.log()

console.log("All tests completed!")
