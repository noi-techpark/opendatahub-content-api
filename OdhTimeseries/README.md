# Timeseries API

A Go REST API server for managing timeseries data using Gin framework and PostgreSQL with a simplified, performance-optimized schema.

## Features

- **Real-Time Streaming Subscriptions** - WebSocket subscriptions with Materialize for live measurement updates
- **Advanced Discovery Subscriptions** - Use DiscoverSensors-style filters to auto-subscribe to matching sensors
- **High-Performance Batch Processing** - Optimized batch inserts with configurable batch sizes
- **Multiple Data Types** - Support for numeric, string, JSON, geoposition, geoshape, boolean
- **Flexible Dataset Management** - Relational dataset-type associations (no JSON storage)
- **Efficient Queries** - Latest and historical measurements with optimized joins
- **Sensor Discovery** - Advanced filtering and geospatial queries with PostGIS
- **Data Provenance** - Complete lineage tracking for all measurements
- **Idempotent Operations** - Duplicate-safe data insertion with conflict handling
- **Partitioned Storage** - Type-based table partitioning for optimal performance
- **Simplified Schema** - Clean relational design focused on sensor → type → dataset relationships

## Architecture

- **Gin** HTTP framework with comprehensive middleware
- **PostgreSQL 15+** with PostGIS extension for geospatial data
- **Optimized SQL** - Direct queries with batch processing, no ORM overhead
- **Simplified Schema** - Clean relational design with proper foreign keys
- **Type-Based Partitioning** - Measurement tables partitioned by data type
- **Clean Architecture** - handlers → repository layers with proper separation
- **Comprehensive Logging** - Structured logging with error tracking and request tracing

### Schema Overview

**Core Tables:**
- `sensors` - Physical/logical sensor registry (simplified: name, metadata)
- `types` - Measurement type definitions (name, unit, data_type)
- `datasets` - Collection definitions for grouping timeseries
- `dataset_types` - Many-to-many relationship (datasets ↔ types)
- `timeseries` - Unique data streams (sensor + type + optional dataset)
- `measurements_*` - Partitioned by data type for optimal performance
- `provenance` - Data lineage and source tracking

## Quick Start

### Prerequisites
- Go 1.21+
- PostgreSQL 15+ with PostGIS extension
- `jq` (for testing script)

### Database Setup
1. Create database and user:
   ```sql
   CREATE DATABASE timeseries;
   CREATE USER timeseries_user WITH PASSWORD 'your_password';
   GRANT ALL PRIVILEGES ON DATABASE timeseries TO timeseries_user;
   ```

2. Enable PostGIS and initialize schema:
   ```sql
   \c timeseries
   CREATE EXTENSION IF NOT EXISTS postgis;
   \i sql-scripts/init-new.sql
   ```

### Application Setup
1. Copy environment configuration:
   ```bash
   cp example.env .env
   # Edit .env with your database credentials
   ```

2. Install dependencies:
   ```bash
   go mod download
   ```

3. Build and run:
   ```bash
   go build -o timeseries-api cmd/server/main.go
   ./timeseries-api
   ```

   Or run directly:
   ```bash
   source .env && go run cmd/server/main.go
   ```

## Configuration

### Environment Variables

**Database Configuration:**
- `DB_HOST` - Database host (default: localhost)
- `DB_PORT` - Database port (default: 5432) 
- `DB_NAME` - Database name (default: timeseries)
- `DB_USER` - Database user (required)
- `DB_PASSWORD` - Database password (required)
- `DB_SCHEMA` - Database schema (default: intimev3)
- `DB_SSL_MODE` - SSL mode (default: disable)

**Server Configuration:**
- `SERVER_PORT` - Server port (default: 8080)
- `SERVER_READ_TIMEOUT` - Read timeout (default: 10s)
- `SERVER_WRITE_TIMEOUT` - Write timeout (default: 10s)
- `SERVER_SHUTDOWN_TIMEOUT` - Graceful shutdown timeout (default: 5s)

**Logging Configuration:**
- `LOG_LEVEL` - Log level: debug, info, warn, error (default: info)

## API Endpoints

### Measurement Operations
- `POST /api/v1/measurements/batch` - **Batch insert measurements** (optimized with configurable batch sizes)
- `DELETE /api/v1/measurements` - Delete measurements by sensor/type/time filters
- `GET /api/v1/measurements/latest` - Get latest measurements (query params)
- `POST /api/v1/measurements/latest` - Get latest measurements (JSON body)
- `GET /api/v1/measurements/historical` - Get historical measurements (query params)
- `POST /api/v1/measurements/historical` - Get historical measurements (JSON body)

### Dataset Management
- `POST /api/v1/datasets` - Create dataset with optional type associations
- `GET /api/v1/datasets/{id}` - Get dataset with all associated types
- `POST /api/v1/datasets/{id}/types` - Add types to dataset (required/optional)
- `DELETE /api/v1/datasets/{id}/types` - Remove types from dataset
- `GET /api/v1/datasets/{id}/sensors` - Find all sensors with timeseries in dataset

### Sensor Discovery
- `GET /api/v1/sensors/dataset/{id}` - Find sensors by dataset (updated query)
- `POST /api/v1/sensors/search` - Search sensors by measurement value conditions
- **`POST /api/v1/sensors/types`** - Find sensors with specific measurement types (NEW)
- **`GET /api/v1/sensors/types`** - Find sensors with specific measurement types (query params) (NEW)

### Real-Time Streaming (WebSocket)
- `GET /api/v1/measurements/subscribe` - **WebSocket endpoint for real-time measurement updates**
  - **Simple subscriptions**: Specify sensor names directly
  - **Discovery subscriptions**: Use DiscoverSensors-style filters to auto-subscribe
  - **Geospatial filtering**: Bounding box and radius filters
  - See [STREAMING.md](STREAMING.md) and [QUICKSTART-STREAMING.md](QUICKSTART-STREAMING.md) for details

### System Endpoints
- `GET /api/v1/health` - Health check

### API Documentation
- **Swagger UI**: Interactive API documentation available at `http://localhost:8080/api/swagger/index.html`
- **OpenAPI Spec**: Machine-readable specification at `/docs/swagger.json` and `/docs/swagger.yaml`

#### Generating Swagger Documentation
To regenerate the Swagger documentation after making changes to API handlers:

```bash
# Install swag tool (if not already installed)
go install github.com/swaggo/swag/cmd/swag@latest

# Generate documentation
swag init --generalInfo cmd/server/main.go --output docs --parseInternal
```

The generated files will be updated in the `docs/` directory:
- `docs/docs.go` - Go code for embedding Swagger spec
- `docs/swagger.json` - OpenAPI 3.0 JSON specification
- `docs/swagger.yaml` - OpenAPI 3.0 YAML specification

**Note**: The server automatically serves the Swagger UI at `/api/swagger/` when running.

## Testing

### Comprehensive API Testing
The sensor discovery endpoint has been thoroughly tested with 100% success rate. See `TEST_RESULTS.md` for detailed test results including:
- Basic numeric, JSON, string, and geospatial filtering
- Complex AND/OR logical operations
- Nested expressions (4+ levels deep)
- Combined timeseries and measurement filtering
- Time range queries and edge cases

All tests from `test-discovery-payloads.md` are confirmed working.

### Automated Testing
```bash
# Ensure server is running, then:
./test-api.sh
```

### Manual Testing
```bash
# Health check
curl http://localhost:8080/api/v1/health

# API info
curl http://localhost:8080/api/v1/info

# Insert sample data (note simplified schema)
curl -X POST http://localhost:8080/api/v1/measurements/batch \
  -H "Content-Type: application/json" \
  -d @- <<EOF
{
  "measurements": [{
    "sensor_name": "TEMP_001",
    "type_name": "air_temperature",
    "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "value": 23.5
  }]
}
EOF

# Create a dataset
curl -X POST http://localhost:8080/api/v1/datasets \
  -H "Content-Type: application/json" \
  -d '{
    "name": "weather_station",
    "description": "Weather monitoring dataset",
    "type_names": ["air_temperature", "humidity", "pressure"]
  }'

# Query latest measurements
curl "http://localhost:8080/api/v1/measurements/latest?sensor_names=TEMP_001&type_names=air_temperature"

# Find sensors with specific measurement types (ANY)
curl -X POST http://localhost:8080/api/v1/sensors/types \
  -H "Content-Type: application/json" \
  -d '{
    "type_names": ["air_temperature", "humidity"],
    "require_all": false
  }'

# Find sensors with ALL specified measurement types
curl -X POST http://localhost:8080/api/v1/sensors/types \
  -H "Content-Type: application/json" \
  -d '{
    "type_names": ["air_temperature", "humidity", "pressure"],
    "require_all": true
  }'

# Query parameters version
curl "http://localhost:8080/api/v1/sensors/types?type_names=air_temperature,humidity&require_all=false"
```

## Development

### Project Structure
```
timeseries-api/
├── cmd/server/          # Main application entry point
├── internal/
│   ├── config/          # Configuration management
│   ├── handlers/        # HTTP request handlers (query, mutation, dataset)
│   ├── middleware/      # HTTP middleware
│   ├── models/          # Data models and DTOs (simplified schema)
│   └── repository/      # Database layer with batch operations
├── pkg/database/        # Database connection utilities
├── docs/api/           # API documentation
└── sql-scripts/        # Database schema (init-new.sql)
```

### Key Changes in Latest Version

**Schema Improvements:**
- Simplified sensor model (removed station_code, station_type fields)
- Relational dataset-type associations (replaced JSON schema)
- Clean timeseries model (sensor + type + optional dataset)
- Enhanced constraints and proper foreign keys

**Performance Optimizations:**
- Batch insert operations with configurable batch sizes (default: 1000)
- Efficient dataset queries with proper joins
- Optimized sensor discovery by dataset

**API Enhancements:**
- Complete dataset management endpoints
- Simplified measurement request format
- Better error handling and validation
- Updated parameter names (sensor_names vs sensor_codes)

### Development Commands
```bash
# Format code
go fmt ./...

# Run linter (if available)
golangci-lint run

# Build
go build ./cmd/server

# Run with environment
source .env && go run cmd/server/main.go

# Build for production
CGO_ENABLED=0 GOOS=linux go build -a -installsuffix cgo -o timeseries-api cmd/server/main.go
```

### Docker Support
Create `Dockerfile`:
```dockerfile
FROM golang:1.21-alpine AS builder
WORKDIR /app
COPY go.mod go.sum ./
RUN go mod download
COPY . .
RUN CGO_ENABLED=0 go build -o timeseries-api cmd/server/main.go

FROM alpine:latest
RUN apk --no-cache add ca-certificates
WORKDIR /root/
COPY --from=builder /app/timeseries-api .
EXPOSE 8080
CMD ["./timeseries-api"]
```

## Supported Data Types

- **Numeric**: Integers and floating-point numbers
- **String**: Text values
- **Boolean**: True/false values
- **JSON**: Objects and arrays stored as JSONB
- **GeoPosition**: Geographic points (PostGIS geometry)
- **GeoShape**: Geographic shapes/polygons (PostGIS geometry)

## Performance Considerations

### Database Optimizations
- **Type-Based Partitioning** - Measurement tables partitioned by data type
- **Strategic Indexing** - Timestamp, sensor, type, and geospatial indexes
- **Batch Processing** - Configurable batch sizes for high-throughput inserts
- **Efficient Joins** - Simplified schema reduces join complexity
- **Connection Pooling** - Go's database/sql handles connection management

### Query Performance
- **Prepared Statements** - All queries use parameters to prevent SQL injection
- **Conflict Handling** - ON CONFLICT DO NOTHING for idempotent operations
- **Optimized Discovery** - Dataset-sensor relationships use proper foreign keys
- **Selective Querying** - Type-specific measurement tables reduce scan overhead

### Batch Insert Performance
```go
// Batch sizes are configurable per operation
BatchInsertMeasurements(measurements, batchSize) // default: 1000
```

**Benchmark Results** (example):
- Single inserts: ~100 ops/sec
- Batch inserts (1000): ~50,000 ops/sec
- Memory usage: <100MB for 1M measurements

## Security Features

- SQL injection prevention through parameterized queries
- CORS headers configuration
- Security headers (X-Content-Type-Options, X-Frame-Options, etc.)
- Request logging and error tracking
- Graceful shutdown handling