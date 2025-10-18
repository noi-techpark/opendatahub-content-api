# Getting Started with ODH Discovery Web App

## Quick Start

### 1. Install Dependencies

```bash
cd OdhDiscovery/webapp
npm install
```

### 2. Start Development Server

```bash
npm run dev
```

The app will be available at **http://localhost:3000**

### 3. API Configuration

The development server proxies API requests:

- **Content API**: `/api/v1/content/*` → `https://tourism.opendatahub.com/v1/*`
- **Timeseries API**: `/api/v1/timeseries/*` → `http://localhost:8080/api/v1/*`

**Important**: Make sure your timeseries API is running on `localhost:8080` or update the proxy in `vite.config.js`.

## Application Structure

### Main Pages

1. **Home** (`/`)
   - Landing page with overview
   - Links to dataset and timeseries browsers

2. **Dataset Browser** (`/datasets`)
   - Browse all available dataset types
   - View metadata (total entries, pages)
   - Click to inspect individual datasets

3. **Dataset Inspector** (`/datasets/:datasetName`)
   - **Full URL state persistence** - all filters, page, view mode saved in URL
   - Table view and Raw JSON view
   - Field presence filters (checkboxes for isnotnull)
   - Raw filter expressions
   - Search functionality
   - Real-time statistics
   - Entry selection for bulk timeseries
   - cURL command display

4. **Timeseries Browser** (`/timeseries`)
   - Browse all timeseries types
   - View type metadata
   - See sensor counts

5. **Timeseries Inspector** (`/timeseries/:typeName`)
   - View all sensors for a specific type
   - Type information display
   - Sensor list with IDs

6. **Bulk Timeseries Inspector** (`/bulk-timeseries`)
   - **Full URL state persistence**
   - Multi-entry timeseries visualization
   - Type selection
   - Three view modes: Table, Raw JSON, Formatted
   - Ready for chart and map integration

## Key Features

### URL State Persistence ✅

All pages save their state to the URL:

```
Example Dataset Inspector URL:
/datasets/Accommodation?page=2&pagesize=50&view=table&rawfilter=eq(Active,true)&searchfilter=hotel

Example Bulk Timeseries URL:
/bulk-timeseries?entries=id1,id2,id3&types=temperature,humidity&view=formatted
```

**Benefits**:
- Share exact views with colleagues
- Bookmark specific configurations
- Browser back/forward works correctly
- Deep linking to specific states

### cURL Command Generation ✅

Every API request shows the equivalent cURL command:

```bash
curl -X GET "https://tourism.opendatahub.com/v1/Accommodation?pagenumber=1&pagesize=50" \
  -H "Content-Type: application/json"
```

**Benefits**:
- Easy API integration
- Request reproducibility
- Learning tool for API usage

### Real-time Data Analysis ✅

- Field completeness calculation
- Structure discovery
- Example value extraction
- Statistics update with filters

### Flexible Filtering ✅

**Field Presence Filtering**:
- Checkboxes for each field
- Filter entries where field is not null
- Combined with AND logic

**Raw Filter Expressions**:
```
eq(Active,true)
and(eq(Active,true),gt(Altitude,1000))
isnotnull(Detail.en.Title)
```

**Search Filter**:
- Search across title fields
- All languages supported

## Usage Examples

### 1. Browse and Inspect Datasets

1. Go to `/datasets`
2. Click on "Accommodation"
3. View entries in table format
4. Select field presence filters (e.g., "ImageGallery")
5. Add raw filter: `eq(Active,true)`
6. Click "Apply Filters"
7. View updated statistics
8. Copy URL to share this exact view

### 2. Select Entries for Timeseries

1. In Dataset Inspector, check entries
2. Click "Bulk Inspect Timeseries"
3. Select timeseries types to load
4. Click "Load Measurements"
5. Switch between Table, Raw, and Formatted views

### 3. Explore Timeseries Types

1. Go to `/timeseries`
2. Browse available types
3. Click on "temperature"
4. View all sensors with temperature measurements
5. See sensor names and timeseries IDs

## Development Tips

### Adding New Features

The application structure is ready for enhancement:

**Add Chart Components**:
```javascript
// src/components/charts/LineChart.vue
import { Line } from 'vue-chartjs'
import { Chart as ChartJS, LineElement, PointElement, ... } from 'chart.js'

// Already installed: chart.js, vue-chartjs
```

**Add Map Components**:
```javascript
// src/components/maps/TimeseriesMap.vue
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'

// Already installed: leaflet
```

### Extending URL State

To add new URL parameters:

```javascript
// In your component
import { useUrlState } from '@/composables/useUrlState'

const { syncWithUrl, serializers } = useUrlState()

const myState = syncWithUrl(
  'myParam',
  'defaultValue',
  serializers.string.serialize,
  serializers.string.deserialize
)

// myState is now reactive and synced with URL
```

### Adding New Dataset Types

Update the `DATASET_TYPES` array in `src/api/contentApi.js`:

```javascript
export const DATASET_TYPES = [
  'Accommodation',
  'Activity',
  // ... add your new type here
]
```

## Testing

### Manual Testing Checklist

**Dataset Inspector**:
- [ ] Page navigation works
- [ ] Filters apply correctly
- [ ] Statistics update with filters
- [ ] URL updates on changes
- [ ] Shared URLs load correctly
- [ ] Entry selection persists

**Timeseries Browser**:
- [ ] All types load
- [ ] Sensor counts display
- [ ] Navigation to inspector works

**Bulk Timeseries**:
- [ ] Receives selections from Dataset Inspector
- [ ] Type selection works
- [ ] Measurements load
- [ ] View switching works
- [ ] URL state persists

## Troubleshooting

### API Not Responding

**Content API**:
- Check if `https://tourism.opendatahub.com` is accessible
- Look for CORS errors in browser console
- Verify proxy configuration in `vite.config.js`

**Timeseries API**:
- Ensure timeseries API is running on `localhost:8080`
- Check API logs for errors
- Try accessing `http://localhost:8080/api/v1/health` directly

### URL State Not Persisting

- Check browser console for errors
- Verify composable is properly initialized in `onMounted`
- Ensure Vue Router is configured correctly
- Check that ref values are being updated

### Build Errors

```bash
# Clear node_modules and reinstall
rm -rf node_modules package-lock.json
npm install

# Clear Vite cache
rm -rf node_modules/.vite
npm run dev
```

## Next Steps

1. **Add Charts**: Implement LineChart component for numeric timeseries
2. **Add Maps**: Implement TimeseriesMap with Leaflet and time slider
3. **Enhance Filters**: Add filter presets and advanced search
4. **Performance**: Add virtual scrolling for large datasets
5. **Export**: Add CSV/JSON export functionality

## Resources

- **Vue 3 Docs**: https://vuejs.org/
- **Pinia Docs**: https://pinia.vuejs.org/
- **Vue Router Docs**: https://router.vuejs.org/
- **Chart.js Docs**: https://www.chartjs.org/
- **Leaflet Docs**: https://leafletjs.com/

## Support

For issues or questions:
- Check `IMPLEMENTATION_SUMMARY.md` for architecture details
- Review `README.md` for project overview
- See ODH API documentation in `CONTEXT_CONTENT.md` and `CONTEXT_TIMESERIES.md`

Happy exploring! 🚀
