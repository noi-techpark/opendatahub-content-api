# Open Data Hub Discovery Web Application

Vue3-based data discovery application for exploring the Open Data Hub platform's datasets and timeseries.

## Features

### Dataset Discovery
- **Browse Datasets**: View all available dataset types with metadata
- **Dataset Inspector**: Deep dive into dataset entries with:
  - Table and Raw JSON views
  - Field presence filtering (isnotnull)
  - Value filtering using rawfilter syntax
  - Real-time statistics and completeness analysis
  - URL persistence for sharing and history
  - Bulk selection for timeseries inspection

### Timeseries Discovery
- **Browse Timeseries Types**: Explore available measurement types
- **Timeseries Inspector**: View sensors and measurements
- **Bulk Timeseries Inspector**: Multi-entry visualization with:
  - Table, Raw JSON, and Formatted views
  - Chart visualization for numeric data
  - Map visualization for geographic data with time slider
  - Detailed JSON inspection with recursive structure

### Technical Features
- **URL State Persistence**: All filters, selections, and views are persisted in URL
- **Shareable Links**: Every configuration can be shared via URL
- **API Reproducibility**: cURL commands generated for all requests
- **Responsive Design**: Works on desktop and mobile

## Installation

```bash
cd OdhDiscovery/webapp
npm install
```

## Development

```bash
npm run dev
```

The app will be available at `http://localhost:3000`

### API Proxying

The dev server proxies API requests:
- `/api/v1/content/*` → `https://tourism.opendatahub.com/v1/*`
- `/api/v1/timeseries/*` → `http://localhost:8080/api/v1/*`

Make sure the timeseries API is running locally on port 8080.

## Build

```bash
npm run build
```

## Project Structure

```
webapp/
├── src/
│   ├── api/              # API client modules
│   │   ├── client.js     # Axios instances
│   │   ├── contentApi.js # Content API methods
│   │   └── timeseriesApi.js # Timeseries API methods
│   ├── stores/           # Pinia stores
│   │   ├── datasetStore.js
│   │   ├── timeseriesStore.js
│   │   └── selectionStore.js
│   ├── composables/      # Vue composables
│   │   └── useUrlState.js # URL state management
│   ├── utils/            # Utility functions
│   │   ├── dataAnalyzer.js # Data analysis utilities
│   │   └── filterBuilder.js # Filter expression builders
│   ├── components/       # Reusable components
│   │   ├── CurlDisplay.vue
│   │   ├── JsonViewer.vue
│   │   └── DatasetStats.vue
│   ├── views/            # Page components
│   │   ├── Home.vue
│   │   ├── DatasetBrowser.vue
│   │   ├── DatasetInspector.vue
│   │   ├── TimeseriesBrowser.vue
│   │   ├── TimeseriesInspector.vue
│   │   └── BulkTimeseriesInspector.vue
│   ├── router/           # Vue Router config
│   ├── assets/           # CSS and static assets
│   ├── App.vue           # Root component
│   └── main.js           # Entry point
├── public/               # Static files
├── index.html
├── vite.config.js
└── package.json
```

## URL State Parameters

### Dataset Inspector
- `page`: Current page number
- `pagesize`: Items per page
- `view`: 'table' or 'raw'
- `rawfilter`: Raw filter expression
- `searchfilter`: Search query
- `selectedIds`: Comma-separated entry IDs

### Timeseries Inspector
- `view`: 'table' or 'raw'
- `filter`: JSON filter object
- `selectedSensors`: Comma-separated sensor names

### Bulk Timeseries Inspector
- `entries`: Comma-separated entry IDs
- `sensors`: Comma-separated sensor names
- `types`: Comma-separated timeseries types
- `view`: 'table', 'raw', or 'formatted'
- `startTime`: ISO 8601 timestamp
- `endTime`: ISO 8601 timestamp
- `timeIndex`: Current time slider position

## Usage Examples

### Browse Datasets
Navigate to `/datasets` to see all available datasets with their entry counts.

### Inspect a Dataset
Click on any dataset to inspect its entries. Use filters to narrow down results:

**Field Presence Filter**:
Select checkboxes to filter entries that have non-null values for specific fields.

**Raw Filter**:
Use Content API syntax:
- `eq(Active,true)` - Active entries only
- `gt(Altitude,1000)` - Altitude greater than 1000
- `and(eq(Active,true),isnotnull(Detail.en.Title))` - Combine conditions

### Bulk Timeseries Inspection
1. Select entries in Dataset Inspector
2. Click "Bulk Inspect Timeseries"
3. Select timeseries types to load
4. View data in multiple formats:
   - **Table**: All measurements in tabular format
   - **Raw**: JSON response
   - **Formatted**: Type-specific visualizations

## Architecture

### State Management
- **Pinia stores** for global state
- **URL composables** for persistent state
- **Reactive bindings** for automatic UI updates

### API Integration
- **Axios clients** with request/response interceptors
- **Type-safe** method signatures
- **cURL generation** for reproducibility

### Data Analysis
- **Real-time analysis** of dataset completeness
- **Field statistics** and examples
- **Timeseries attachment** detection

## Contributing

This application is part of the Open Data Hub project.

## License

See the project root for license information.
