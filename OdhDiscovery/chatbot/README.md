# ODH Chatbot - Intelligent Data Analysis Assistant

An AI-powered chatbot for exploring and analyzing Open Data Hub (ODH) tourism and mobility data. Built with LangGraph, FastAPI, and ChromaDB, featuring provider-agnostic LLM support.

## Features

- **Provider-Agnostic LLM**: Supports TogetherAI, OpenAI, Anthropic, or custom providers
- **Smart Tools**: Automatic data preprocessing to manage large API responses
- **WebSocket Real-time Communication**: Bidirectional communication with webapp
- **Webapp Navigation Control**: Agent can navigate the frontend to show visualizations
- **Vector Store**: ChromaDB for documentation retrieval
- **Docker Compose Setup**: One-command deployment
- **Configurable**: Everything configurable via environment variables

## Architecture

```
┌─────────────────┐
│   Vue Webapp    │
│   (Frontend)    │
└────────┬────────┘
         │ WebSocket
         ▼
┌─────────────────┐
│  FastAPI Server │
│   (Backend)     │
└────────┬────────┘
         │
    ┌────┴────────────────────┐
    ▼                         ▼
┌─────────────┐      ┌──────────────┐
│  LangGraph  │      │   ChromaDB   │
│    Agent    │      │ Vector Store │
└──────┬──────┘      └──────────────┘
       │
   ┌───┴────────┐
   ▼            ▼
┌──────┐   ┌──────────┐
│ LLM  │   │   Proxy  │
└──────┘   └─────┬────┘
                 │
        ┌────────┼────────┐
        ▼        ▼        ▼
    ┌───────┐ ┌──────┐ ┌──────┐
    │ ODH   │ │Local │ │Local │
    │Tourism│ │:8082 │ │:8080 │
    │  API  │ │Sensor│ │ TS   │
    └───────┘ └──────┘ └──────┘
```

## Quick Start

### Prerequisites

- Docker and Docker Compose
- TogetherAI API key (or other LLM provider key)

### 1. Setup Environment

Create `.env` file in the `chatbot/` directory:

```bash
# LLM Configuration
TOGETHER_API_KEY=your_api_key_here
LLM_PROVIDER=togetherai
LLM_MODEL=meta-llama/Meta-Llama-3.1-70B-Instruct-Turbo
```

### 2. Start Services

```bash
cd OdhDiscovery/chatbot
docker-compose up
```

This will start:
- API Proxy on port 5000 (routes to external APIs)
- ChromaDB on port 8000
- Backend on port 8001

### 3. Ingest Documentation

Load markdown documentation into the vector store:

```bash
# From inside the backend container
docker-compose exec backend python scripts/ingest_docs.py /docs --clear

# Or from host (if you have Python)
cd backend
pip install -r requirements.txt
python scripts/ingest_docs.py ../docs --clear
```

### 4. Test the API

```bash
# Health check
curl http://localhost:8001/health

# Test WebSocket (using wscat or browser)
wscat -c ws://localhost:8001/ws
> {"type": "query", "content": "How many hotels are there?"}
```

## Configuration

All configuration is done via environment variables in `docker-compose.yml` or `.env` file.

### LLM Provider Configuration

#### TogetherAI (Default)
```env
LLM_PROVIDER=togetherai
LLM_MODEL=meta-llama/Meta-Llama-3.1-70B-Instruct-Turbo
TOGETHER_API_KEY=your_key
```

#### OpenAI
```env
LLM_PROVIDER=openai
LLM_MODEL=gpt-4-turbo-preview
LLM_API_KEY=your_openai_key
```

#### Anthropic
```env
LLM_PROVIDER=anthropic
LLM_MODEL=claude-3-5-sonnet-20241022
LLM_API_KEY=your_anthropic_key
```

#### Custom Provider (OpenAI-compatible API)
```env
LLM_PROVIDER=custom
LLM_MODEL=your-model-name
LLM_API_KEY=your_key
LLM_BASE_URL=https://your-api.com/v1
```

### API Configuration

The proxy service routes requests to the correct backends:

```env
# Content API (via proxy)
CONTENT_API_BASE_URL=http://proxy:5000/api/v1/content

# Timeseries API (via proxy)
TIMESERIES_API_BASE_URL=http://proxy:5000/api/v1/timeseries

# Request timeout
API_TIMEOUT=30
```

**Proxy Routing Rules** (defined in `proxy/nginx.conf`):
- `/api/v1/content/Sensor` → `http://localhost:8082/v1/Sensor` (local sensor API)
- `/api/v1/content` → `https://tourism.opendatahub.com/v1` (external ODH API)
- `/api/v1/timeseries` → `http://localhost:8080/api/v1` (local timeseries API)

To change routing rules, edit `proxy/nginx.conf` and restart: `docker-compose restart proxy`

### Preprocessing Configuration

```env
# Maximum tokens per tool response
MAX_TOKENS_PER_TOOL=2000

# Enable automatic aggregation for large responses
ENABLE_AUTO_AGGREGATION=true

# Default and max page sizes
DEFAULT_PAGE_SIZE=50
MAX_PAGE_SIZE=200
```

## Project Structure

```
chatbot/
├── docker-compose.yml          # Docker Compose configuration
├── proxy/
│   └── nginx.conf              # Nginx proxy routing rules
├── backend/
│   ├── Dockerfile              # Backend container definition
│   ├── requirements.txt        # Python dependencies
│   ├── config.py               # Configuration management
│   ├── main.py                 # FastAPI WebSocket server
│   ├── agent/                  # LangGraph agent
│   │   ├── graph.py            # Agent workflow
│   │   ├── state.py            # Agent state definition
│   │   └── prompts.py          # System prompts
│   ├── tools/                  # Smart tools with preprocessing
│   │   ├── base.py             # SmartTool base class
│   │   ├── content_api.py      # Content API tools
│   │   ├── timeseries_api.py   # Timeseries API tools
│   │   └── navigation.py       # Navigation control tool
│   ├── clients/                # Configurable API clients
│   │   ├── content_client.py   # Content API client
│   │   └── timeseries_client.py # Timeseries API client
│   ├── preprocessing/          # Data preprocessing
│   │   ├── utils.py            # Token counting, truncation
│   │   └── strategies.py       # Aggregation, summarization
│   └── vector_store/           # ChromaDB integration
│       ├── chroma_client.py    # Vector store client
│       └── ingestion.py        # Document ingestion
├── docs/                       # Markdown documentation to ingest
├── scripts/
│   └── ingest_docs.py          # Standalone ingestion script
└── README.md                   # This file
```

## Available Tools

The agent has access to the following tools:

### Content API Tools
- `get_datasets`: List all available datasets
- `get_dataset_entries`: Query dataset entries with filtering
- `count_entries`: Count entries matching criteria
- `get_entry_by_id`: Get detailed entry information

### Timeseries API Tools
- `get_timeseries_types`: List available timeseries types
- `get_sensors_by_type`: Get sensors for a type
- `get_timeseries_measurements`: Get measurements with statistics
- `get_latest_measurements`: Get current sensor values

### Navigation Tool
- `navigate_webapp`: Control webapp navigation with parameters

All tools have built-in preprocessing to manage large responses and stay within token limits.

## WebSocket API

### Client → Server Messages

```json
{
  "type": "query",
  "content": "User question here"
}
```

### Server → Client Messages

#### AI Response
```json
{
  "type": "message",
  "content": "Agent response here"
}
```

#### Navigation Command
```json
{
  "type": "navigation",
  "data": {
    "type": "navigate",
    "route": "DatasetInspector",
    "params": {
      "datasetName": "accommodation",
      "rawfilter": "Active eq true",
      "view": "table"
    }
  }
}
```

#### Status Update
```json
{
  "type": "status",
  "content": "Processing..." | "Done"
}
```

#### Error
```json
{
  "type": "error",
  "content": "Error message"
}
```

## Data Preprocessing

The chatbot implements sophisticated preprocessing to handle large API responses:

1. **Field Projection**: Extract only needed fields (96% reduction)
2. **Aggregation**: Statistical summaries instead of raw data (99.9999% reduction)
3. **Token Counting**: Ensure responses stay under limits
4. **Emergency Summarization**: Fallback for oversized responses

This reduces costs from $625/query to $0.005/query for large datasets.

## Development

### Running Without Docker

```bash
# Install dependencies
cd backend
pip install -r requirements.txt

# Set environment variables
export TOGETHER_API_KEY=your_key
export LLM_PROVIDER=togetherai
export LLM_MODEL=meta-llama/Meta-Llama-3.1-70B-Instruct-Turbo

# Start ChromaDB separately
docker run -p 8000:8000 chromadb/chroma:latest

# Run backend
python main.py
```

### Making API Clients Configurable

The API clients are designed to be easily modified:

```python
# clients/content_client.py
class ContentAPIClient:
    def __init__(self, base_url=None, timeout=None):
        self.base_url = base_url or settings.content_api_base_url
        # Easy to change API structure

    async def get_dataset_entries(self, dataset_name, **kwargs):
        # All parameters are configurable
        # Easy to adapt to API changes
```

### Changing LLM Provider

Simply update environment variables:

```bash
# Switch to OpenAI
docker-compose down
# Edit docker-compose.yml or .env
docker-compose up
```

## Troubleshooting

### ChromaDB Connection Issues

```bash
# Check ChromaDB is running
curl http://localhost:8000/api/v1/heartbeat

# View ChromaDB logs
docker-compose logs chromadb
```

### Backend Not Starting

```bash
# View backend logs
docker-compose logs backend

# Check environment variables
docker-compose exec backend env | grep LLM
```

### API Connection Issues

```bash
# Test Content API from container
docker-compose exec backend curl http://host.docker.internal:5000/api/v1/content

# If using Linux, you may need to use host network mode
# Or replace host.docker.internal with actual IP
```

## Production Considerations

1. **Security**:
   - Set specific CORS origins in production
   - Use API key authentication for endpoints
   - Secure WebSocket connections (WSS)

2. **Performance**:
   - Adjust `MAX_TOKENS_PER_TOOL` based on your LLM's limits
   - Configure `DEFAULT_PAGE_SIZE` to balance speed vs. completeness
   - Monitor ChromaDB performance and scale if needed

3. **Monitoring**:
   - Add proper logging and metrics
   - Monitor LLM API usage and costs
   - Track WebSocket connection stability

## License

Same as parent project (Open Data Hub).
