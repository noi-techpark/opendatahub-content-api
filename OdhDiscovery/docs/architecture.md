# ODH Chatbot Architecture

## Overview

The ODH Chatbot is built using a modern, modular architecture designed for flexibility, scalability, and maintainability.

## Technology Stack

### Backend
- **Python 3.11+**: Core language
- **FastAPI**: WebSocket server and HTTP endpoints
- **LangGraph**: Agent orchestration and workflow
- **LangChain**: LLM integration and tool calling
- **ChromaDB**: Vector database for documentation
- **Pandas/NumPy**: Data preprocessing and aggregation

### LLM Providers (Provider-Agnostic)
- **TogetherAI**: Default provider (cost-effective, fast)
- **OpenAI**: GPT-4 Turbo support
- **Anthropic**: Claude support
- **Custom**: Any OpenAI-compatible API

### Infrastructure
- **Docker Compose**: Orchestration
- **ChromaDB Container**: Persistent vector storage
- **uvicorn**: ASGI server

## Architecture Diagram

```
┌──────────────────────────────────────────────────────────┐
│                      Vue.js Webapp                       │
│  (DatasetInspector, TimeseriesInspector, etc.)          │
└────────────────────────┬─────────────────────────────────┘
                         │ WebSocket (bidirectional)
                         │ - Queries from user
                         │ - Responses from agent
                         │ - Navigation commands
                         ▼
┌──────────────────────────────────────────────────────────┐
│              FastAPI WebSocket Server                    │
│  - WebSocket endpoint (/ws)                             │
│  - Health check endpoint (/health)                      │
│  - Documentation ingestion (/ingest-docs)               │
└────────────────────────┬─────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────┐
│                   LangGraph Agent                        │
│                                                          │
│  ┌────────────┐    ┌──────────────┐   ┌──────────────┐ │
│  │   Router   │───→│   Executor   │──→│   Analyzer   │ │
│  └────────────┘    └──────────────┘   └──────────────┘ │
│         │                  │                   │        │
│         │                  ▼                   │        │
│         │          ┌──────────────┐            │        │
│         │          │ Preprocessor │            │        │
│         │          └──────────────┘            │        │
│         │                  │                   │        │
│         └──────────────────┴───────────────────┘        │
└────────────────────────┬─────────────────────────────────┘
                         │
        ┌────────────────┼────────────────┐
        │                │                │
        ▼                ▼                ▼
┌──────────────┐  ┌──────────┐  ┌──────────────┐
│     LLM      │  │  Tools   │  │ Vector Store │
│  (Provider)  │  │ Registry │  │  (ChromaDB)  │
└──────────────┘  └────┬─────┘  └──────────────┘
                       │
        ┌──────────────┼──────────────┐
        │              │              │
        ▼              ▼              ▼
┌──────────────┐ ┌──────────┐ ┌──────────────┐
│  Content API │ │Timeseries│ │  Navigation  │
│    Client    │ │   API    │ │    Tool      │
│              │ │  Client  │ │              │
└──────────────┘ └──────────┘ └──────────────┘
```

## Component Details

### 1. FastAPI WebSocket Server (`main.py`)

**Responsibilities**:
- Accept WebSocket connections from frontend
- Parse incoming queries
- Invoke LangGraph agent
- Stream responses back to frontend
- Handle navigation commands

**Endpoints**:
- `WS /ws`: WebSocket endpoint for chat
- `GET /health`: Health check
- `POST /ingest-docs`: Trigger documentation ingestion

### 2. LangGraph Agent (`agent/graph.py`)

**Workflow**:
1. **Process Query**: Receive user query, prepare messages
2. **Execute Tools**: Call appropriate tools based on query
3. **Preprocess Results**: Apply data reduction strategies
4. **Analyze & Respond**: Generate natural language response
5. **Navigation**: Optionally generate navigation commands

**State** (`agent/state.py`):
- `messages`: Conversation history
- `query`: Current user query
- `tool_results`: Results from tool executions
- `navigation_commands`: Commands to control webapp
- `iterations`: Loop counter

### 3. Smart Tools (`tools/`)

**Base Class** (`base.py`):
- Automatic preprocessing
- Token counting and limiting
- Error handling
- Metadata tracking

**Tool Categories**:

**Content API Tools** (`content_api.py`):
- `get_datasets`: List available datasets
- `get_dataset_entries`: Query with filtering/pagination
- `count_entries`: Fast counting
- `get_entry_by_id`: Single entry retrieval

**Timeseries API Tools** (`timeseries_api.py`):
- `get_types`: List timeseries types
- `get_sensors`: Get sensors by type
- `get_timeseries`: Measurements with statistics
- `get_latest`: Current sensor values

**Navigation Tool** (`navigation.py`):
- `navigate_webapp`: Generate navigation commands

### 4. API Clients (`clients/`)

**Design Principles**:
- Clean, simple interface
- Configurable via environment variables
- Easy to adapt to API changes
- Async/await for performance

**Content Client** (`content_client.py`):
```python
async def get_dataset_entries(
    dataset_name: str,
    page: int = 1,
    pagesize: int = 50,
    raw_filter: str | None = None,
    ...
) -> dict
```

**Timeseries Client** (`timeseries_client.py`):
```python
async def get_timeseries_bulk(
    sensor_names: list[str],
    from_date: datetime | str | None = None,
    ...
) -> dict
```

### 5. Preprocessing (`preprocessing/`)

**Utilities** (`utils.py`):
- `count_tokens()`: Estimate token usage
- `truncate_to_token_limit()`: Smart truncation
- `extract_fields()`: Field projection
- `simplify_entry()`: Reduce nesting depth

**Strategies** (`strategies.py`):
- `field_projection()`: Extract specific fields (96% reduction)
- `aggregate_dataset_entries()`: Statistical summaries (99.9% reduction)
- `summarize_measurements()`: Timeseries aggregation (99.9% reduction)
- `emergency_summarize()`: Fallback for oversized data

**Token Reduction Example**:
```
Original:  250MB JSON → 62,500,000 tokens → $625 cost
After:     2KB JSON   → 500 tokens        → $0.005 cost
Reduction: 99.9992%
```

### 6. Vector Store (`vector_store/`)

**ChromaDB Client** (`chroma_client.py`):
- Connection management
- Document retrieval
- Similarity search

**Ingestion** (`ingestion.py`):
- Markdown file processing
- Header-aware chunking
- Metadata extraction
- Batch uploading

**Embedding Model**:
- HuggingFace `sentence-transformers/all-MiniLM-L6-v2`
- Free, no API key required
- Good balance of speed and quality

### 7. Configuration (`config.py`)

**Environment-Based**:
All configuration via environment variables using Pydantic Settings.

**Categories**:
- LLM provider and model settings
- API endpoints and timeouts
- Vector store connection
- Backend server configuration
- Preprocessing parameters

**Provider-Agnostic Design**:
```python
def create_llm() -> BaseChatModel:
    if provider == "togetherai":
        return ChatOpenAI(base_url="https://api.together.xyz/v1", ...)
    elif provider == "openai":
        return ChatOpenAI(...)
    elif provider == "anthropic":
        return ChatAnthropic(...)
```

## Data Flow

### Query Processing Flow

```
1. User sends query via WebSocket
   → Frontend: {"type": "query", "content": "How many hotels?"}

2. FastAPI receives message
   → Creates initial AgentState
   → Invokes LangGraph agent

3. LangGraph agent processes query
   → Analyzes query intent
   → Selects appropriate tools
   → Executes tools in parallel when possible

4. Tools execute with preprocessing
   → API client makes request
   → Preprocessor reduces data size
   → Returns within token limits

5. Agent generates response
   → LLM synthesizes tool results
   → Creates natural language response
   → Optionally generates navigation commands

6. FastAPI streams results back
   → Message: AI response
   → Navigation: Webapp navigation command
   → Status: "Done"

7. Frontend handles response
   → Displays AI message
   → Executes navigation (if present)
   → Updates UI
```

### Tool Execution with Preprocessing

```
1. Tool receives parameters
   ├─→ Content API call with filters

2. API returns large response (e.g., 5000 entries)
   ├─→ Raw size: 25MB, ~6M tokens

3. Preprocessor applies strategies
   ├─→ Field projection: Extract only needed fields
   ├─→ Aggregation: Calculate statistics
   ├─→ Sampling: Include 3-5 sample entries
   └─→ Result: 2KB, ~500 tokens (99.99% reduction)

4. Token validator checks size
   ├─→ If still too large → Emergency summarization
   └─→ If OK → Return to agent

5. Agent receives processed result
   └─→ Uses data to answer user query
```

## Scalability Considerations

### Horizontal Scaling
- **FastAPI**: Multiple backend instances with load balancer
- **ChromaDB**: Separate cluster for vector storage
- **LLM**: Provider handles scaling

### Vertical Scaling
- **Preprocessing**: Runs in-memory, scales with RAM
- **Tools**: Async execution, scales with CPU cores
- **WebSocket**: Each connection is lightweight

### Caching Opportunities
- Preprocessed aggregations
- Vector search results
- Common tool responses
- Dataset metadata

## Security Considerations

### Authentication
- WebSocket authentication (to be implemented)
- API key validation for LLM provider
- Environment variable protection

### Rate Limiting
- Tool execution limits
- WebSocket message rate limits
- LLM API usage monitoring

### Data Privacy
- No persistent storage of user queries (by default)
- Configurable logging levels
- Secure environment variable handling

## Error Handling

### Graceful Degradation
1. Tool execution fails → Agent tries alternative approach
2. LLM API error → Return cached/simple response
3. Preprocessing fails → Return raw data (truncated)
4. WebSocket disconnect → Clean up resources

### Logging
- Structured logging with levels
- Tool execution tracking
- Performance metrics
- Error stack traces

## Future Enhancements

### Planned Features
- [ ] Conversation memory (multi-turn)
- [ ] User authentication
- [ ] Response caching
- [ ] Analytics dashboard
- [ ] Multi-language support
- [ ] Voice interface
- [ ] Mobile app integration

### Extensibility Points
- **Custom Tools**: Add new tools by extending SmartTool
- **Custom Preprocessors**: Implement domain-specific strategies
- **Custom LLM Providers**: Add provider to config.py
- **Custom API Clients**: Replace or extend existing clients

## Performance Metrics

### Target Performance
- Query response time: < 5 seconds
- Token usage: < 2000 tokens per tool
- WebSocket latency: < 100ms
- Vector search: < 500ms

### Monitoring
- LLM API costs
- Tool execution times
- Preprocessing effectiveness
- WebSocket connection stability
