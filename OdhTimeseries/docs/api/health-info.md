# Health and Info Endpoints

## Overview
Provides health check and API information endpoints for monitoring and discovery.

## Endpoints

### Health Check
- **Method**: `GET`
- **Path**: `/api/v1/health`

### API Information
- **Method**: `GET`
- **Path**: `/api/v1/info`

### Root Redirect
- **Method**: `GET`
- **Path**: `/`
- **Action**: Redirects to `/api/v1/info`

## Health Check

### Approach
Returns basic health status and timestamp for monitoring systems.

### Response (200 OK)
```json
{
  "status": "healthy",
  "timestamp": "2025-01-15T14:30:00Z",
  "service": "timeseries-api"
}
```

## API Information

### Approach
Provides comprehensive API documentation including all available endpoints.

### Response (200 OK)
```json
{
  "service": "timeseries-api",
  "version": "1.0.0",
  "description": "Timeseries data storage and retrieval API",
  "endpoints": {
    "POST /api/v1/measurements/batch": "Insert batch measurements",
    "DELETE /api/v1/measurements": "Delete measurements",
    "GET /api/v1/measurements/latest": "Get latest measurements (query params)",
    "POST /api/v1/measurements/latest": "Get latest measurements (JSON body)",
    "GET /api/v1/measurements/historical": "Get historical measurements (query params)",
    "POST /api/v1/measurements/historical": "Get historical measurements (JSON body)",
    "GET /api/v1/sensors/dataset/:id": "Find sensors by dataset",
    "POST /api/v1/sensors/search": "Search sensors by measurement conditions",
    "GET /api/v1/health": "Health check",
    "GET /api/v1/info": "API information"
  }
}
```

## Examples

### Health Check
```bash
curl "http://localhost:8080/api/v1/health"
```

### API Info
```bash
curl "http://localhost:8080/api/v1/info"
```

### Root Access
```bash
curl "http://localhost:8080/"
# Redirects to /api/v1/info
```

## Usage

### Monitoring
Use the health endpoint for:
- Load balancer health checks
- Kubernetes liveness/readiness probes
- Monitoring system checks
- Service discovery verification

### Documentation
Use the info endpoint for:
- API discovery
- Development reference
- Integration documentation
- Endpoint validation

## Response Fields

### Health Endpoint
- **status**: Always "healthy" when service responds
- **timestamp**: Current UTC timestamp in ISO 8601 format
- **service**: Service identifier

### Info Endpoint
- **service**: Service name
- **version**: API version
- **description**: Service description
- **endpoints**: Map of available endpoints with descriptions

## Integration Examples

### Docker Health Check
```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/api/v1/health || exit 1
```

### Kubernetes Liveness Probe
```yaml
livenessProbe:
  httpGet:
    path: /api/v1/health
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10
```

### Monitoring Script
```bash
#!/bin/bash
response=$(curl -s http://localhost:8080/api/v1/health)
status=$(echo $response | jq -r '.status')
if [ "$status" != "healthy" ]; then
  echo "Service unhealthy: $response"
  exit 1
fi
echo "Service healthy"
```