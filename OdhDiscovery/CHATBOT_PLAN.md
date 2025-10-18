# OdhDiscovery Chatbot - Design & Implementation Plan

## Executive Summary

This document outlines a comprehensive plan to build an AI-powered chatbot agent for the OdhDiscovery webapp. The chatbot will:
- Answer questions about datasets and timeseries using live API analysis
- Perform data analysis tasks on behalf of users
- Navigate the webapp to appropriate pages with correct URL configurations
- Show and justify results through webapp visualizations

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Technology Stack](#technology-stack)
3. [System Components](#system-components)
4. [Agent Design](#agent-design)
5. [Tool Definitions](#tool-definitions)
6. [Data Preprocessing & Response Management](#data-preprocessing--response-management) ⭐ NEW
7. [Reasoning Flow](#reasoning-flow)
8. [Frontend Integration](#frontend-integration)
9. [Vector Database & Knowledge Base](#vector-database--knowledge-base)
10. [Implementation Phases](#implementation-phases)
11. [Example Conversations](#example-conversations)
12. [Security & Performance](#security--performance)
13. [Deployment](#deployment)

---

## Architecture Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Frontend (Vue 3)                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ Chat Widget  │  │ Main Views   │  │ URL Router   │     │
│  └──────┬───────┘  └──────────────┘  └──────┬───────┘     │
│         │                                     │             │
│         │ WebSocket                          │ Navigation  │
└─────────┼─────────────────────────────────────┼─────────────┘
          │                                     │
          ▼                                     ▼
┌─────────────────────────────────────────────────────────────┐
│              Agent Backend Service (Python)                  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  LangGraph Agent (State Machine + Reasoning)          │  │
│  │  ┌─────────┐  ┌──────────┐  ┌──────────────────┐    │  │
│  │  │ Planner │→ │ Executor │→ │ Response Builder │    │  │
│  │  └─────────┘  └──────────┘  └──────────────────┘    │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Tool Registry                                        │  │
│  │  • Content API Tools  • Timeseries API Tools         │  │
│  │  • Analysis Tools     • Navigation Tools              │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Vector Database (ChromaDB)                          │  │
│  │  • API Documentation  • Example Queries              │  │
│  │  • Dataset Schemas    • Conversation History         │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────┬───────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────┐
│              External APIs (via proxies)                     │
│  ┌──────────────────┐      ┌──────────────────┐            │
│  │  Content API     │      │  Timeseries API  │            │
│  │  /api/v1/content │      │  /api/v1/timeseries│          │
│  └──────────────────┘      └──────────────────┘            │
└─────────────────────────────────────────────────────────────┘
```

### Communication Flow

1. **User → Frontend**: User types question in chat widget
2. **Frontend → Backend**: WebSocket sends message to agent service
3. **Backend → LLM**: Agent processes with GPT-4/Claude via LangChain
4. **Backend → APIs**: Agent calls tools (Content/Timeseries APIs)
5. **Backend → Frontend**: Streams response chunks + navigation commands
6. **Frontend → Router**: Executes navigation to show results
7. **Frontend → User**: Displays chat response + visual results

---

## Technology Stack

### Backend (Agent Service)

**Core Framework**:
- **Python 3.11+** - Main language
- **LangGraph** - Agent orchestration (state machine for complex reasoning)
- **LangChain** - LLM integration, tool calling, chains
- **FastAPI** - HTTP/WebSocket server
- **Pydantic** - Data validation and serialization

**LLM Provider** (choose one):
- **OpenAI GPT-4 Turbo** - Best reasoning, function calling
- **Anthropic Claude 3.5 Sonnet** - Excellent reasoning, long context
- **Azure OpenAI** - Enterprise option

**Vector Database**:
- **ChromaDB** - Lightweight, embedded, perfect for this use case
- Alternative: **Qdrant** - More scalable if needed

**HTTP Client**:
- **httpx** - Async HTTP client for API calls

**Other Libraries**:
- **websockets** - WebSocket support
- **redis** (optional) - Session/conversation storage
- **tiktoken** - Token counting for context management

### Frontend Integration

**Existing Stack**:
- Vue 3, Pinia, Vue Router (already in use)

**New Components**:
- **Chat Widget Component** - Floating chat interface
- **WebSocket Client** - Communication with agent backend
- **Message Renderer** - Display chat messages with rich content

**Libraries to Add**:
- **socket.io-client** or native WebSocket - Real-time communication
- **marked** or **markdown-it** - Render markdown responses
- **prism.js** - Code syntax highlighting (for showing API examples)

### Deployment

**Backend**:
- **Docker** - Containerized service
- **Uvicorn** - ASGI server
- **Nginx** - Reverse proxy

**Hosting Options**:
- Local development: Docker Compose
- Production: Kubernetes, AWS ECS, or simple VPS

---

## System Components

### 1. Agent Backend Service

**File Structure**:
```
OdhDiscovery/chatbot-backend/
├── app/
│   ├── __init__.py
│   ├── main.py                 # FastAPI app, WebSocket endpoint
│   ├── config.py               # Environment variables, API keys
│   ├── models.py               # Pydantic models for messages, state
│   ├── agent/
│   │   ├── __init__.py
│   │   ├── graph.py            # LangGraph state machine definition
│   │   ├── nodes.py            # Graph nodes (planner, executor, etc.)
│   │   ├── state.py            # Agent state schema
│   │   └── prompts.py          # System prompts, templates
│   ├── tools/
│   │   ├── __init__.py
│   │   ├── base.py             # Base tool class
│   │   ├── content_api.py      # Content API tools
│   │   ├── timeseries_api.py   # Timeseries API tools
│   │   ├── analysis.py         # Analysis tools
│   │   └── navigation.py       # Navigation command generation
│   ├── vectordb/
│   │   ├── __init__.py
│   │   ├── store.py            # ChromaDB wrapper
│   │   ├── embeddings.py       # Embedding generation
│   │   └── indexing.py         # Document indexing utilities
│   └── utils/
│       ├── __init__.py
│       ├── api_client.py       # HTTP client for Content/Timeseries APIs
│       └── logger.py           # Logging configuration
├── data/
│   ├── embeddings/             # Vector database storage
│   └── knowledge/              # Static documentation to embed
│       ├── api_docs.md
│       ├── dataset_schemas.json
│       └── example_queries.json
├── tests/
│   ├── test_tools.py
│   ├── test_agent.py
│   └── test_api.py
├── Dockerfile
├── docker-compose.yml
├── requirements.txt
└── README.md
```

### 2. Frontend Chat Component

**File Structure**:
```
webapp/src/
├── components/
│   ├── chatbot/
│   │   ├── ChatWidget.vue          # Main chat interface
│   │   ├── ChatMessage.vue         # Single message component
│   │   ├── ChatInput.vue           # User input area
│   │   ├── TypingIndicator.vue    # "Agent is thinking..."
│   │   └── NavigationAction.vue   # Show navigation suggestions
├── composables/
│   ├── useChatbot.js               # Chat state management
│   └── useAgentWebSocket.js        # WebSocket connection
├── stores/
│   └── chatbotStore.js             # Pinia store for chat history
└── services/
    └── agentService.js             # Agent backend API client
```

---

## Agent Design

### Agent Architecture: LangGraph State Machine

LangGraph allows us to build a **stateful, multi-step agent** with explicit control flow.

**Graph Structure**:

```
                    ┌──────────────┐
                    │   START      │
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
                    │  Classifier  │──┐ Simple question
                    └──────┬───────┘  │
                           │          │
          Complex question │          │
                           ▼          │
                    ┌──────────────┐  │
                    │   Planner    │  │
                    └──────┬───────┘  │
                           │          │
                           ▼          │
                    ┌──────────────┐  │
                    │   Executor   │  │
                    └──────┬───────┘  │
                           │          │
                           ▼          │
                    ┌──────────────┐  │
                    │   Analyzer   │  │
                    └──────┬───────┘  │
                           │          │
                           ├──────────┘
                           ▼
                    ┌──────────────┐
                    │   Responder  │
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
                    │ Navigation   │
                    │  Generator   │
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
                    │     END      │
                    └──────────────┘
```

**Node Descriptions**:

1. **Classifier**: Determines if question is simple (direct answer) or complex (needs planning)
2. **Planner**: Breaks down complex questions into steps
3. **Executor**: Executes tools based on plan
4. **Analyzer**: Analyzes tool results and determines if more steps needed
5. **Responder**: Generates natural language response
6. **Navigation Generator**: Creates navigation command if appropriate

### Agent State Schema

```python
from typing import TypedDict, Annotated, Sequence
from langchain_core.messages import BaseMessage
from operator import add

class AgentState(TypedDict):
    """State of the agent during conversation."""

    # Conversation
    messages: Annotated[Sequence[BaseMessage], add]
    user_question: str

    # Planning
    question_type: str  # "simple" | "complex"
    plan: list[str]     # List of steps to execute
    current_step: int

    # Execution
    tool_calls: list[dict]
    tool_results: list[dict]

    # Analysis
    analysis_summary: str
    found_answer: bool

    # Response
    response_text: str
    navigation_command: dict | None  # { "route": "/datasets/Accommodation", "query": {...} }

    # Context
    conversation_id: str
    retrieved_context: list[str]  # From vector DB
```

---

## Tool Definitions

Tools are the agent's **interface to the real world**. Each tool should:
- Have clear description for LLM to understand when to use it
- Validate inputs with Pydantic
- Return structured results
- Handle errors gracefully

### Tool Categories

#### 1. Content API Tools

**Tool: `get_dataset_entries`**
```python
from langchain.tools import StructuredTool
from pydantic import BaseModel, Field

class GetDatasetEntriesInput(BaseModel):
    dataset_name: str = Field(description="Name of dataset: Accommodation, Activity, Poi, etc.")
    page: int = Field(default=1, description="Page number")
    pagesize: int = Field(default=50, description="Entries per page")
    raw_filter: str | None = Field(default=None, description="ODH filter expression like 'Active eq true'")

async def get_dataset_entries(
    dataset_name: str,
    page: int = 1,
    pagesize: int = 50,
    raw_filter: str | None = None
) -> dict:
    """Get entries from a specific dataset with optional filtering.

    Use this to:
    - Browse accommodations, activities, POIs, events, etc.
    - Filter datasets by conditions
    - Count total entries matching criteria
    """
    # Implementation: Call Content API via httpx
    async with httpx.AsyncClient() as client:
        response = await client.get(
            f"{CONTENT_API_BASE}/{dataset_name}",
            params={
                "pagenumber": page,
                "pagesize": pagesize,
                "rawfilter": raw_filter
            }
        )
        return response.json()

get_dataset_entries_tool = StructuredTool.from_function(
    func=get_dataset_entries,
    name="get_dataset_entries",
    description="Get entries from a dataset (Accommodation, Activity, Poi, etc.) with optional filters",
    args_schema=GetDatasetEntriesInput
)
```

**Other Content API Tools**:
- `get_dataset_entry_by_id` - Get single entry details
- `count_dataset_entries` - Get total count with filters
- `analyze_dataset_fields` - Get field distributions and distinct values
- `search_datasets` - Full-text search across datasets

#### 2. Timeseries API Tools

**Tool: `get_timeseries_types`**
```python
class GetTimeseriesTypesInput(BaseModel):
    search_query: str | None = Field(default=None, description="Search term to filter types")
    include_sensors: bool = Field(default=False, description="Include sensor counts")

async def get_timeseries_types(
    search_query: str | None = None,
    include_sensors: bool = False
) -> dict:
    """Get available timeseries measurement types.

    Use this to:
    - Discover what types of measurements are available
    - Search for specific measurement types (temperature, humidity, etc.)
    - Get sensor counts per type
    """
    # Implementation
    pass
```

**Tool: `get_sensors_for_type`**
```python
async def get_sensors_for_type(type_name: str) -> dict:
    """Get all sensors that measure a specific type.

    Use this to:
    - Find all temperature sensors
    - List sensors measuring air quality
    - Get sensor metadata
    """
    pass
```

**Tool: `get_latest_measurements`**
```python
class GetLatestMeasurementsInput(BaseModel):
    sensor_names: list[str] = Field(description="List of sensor names to query")
    type_names: list[str] | None = Field(default=None, description="Filter by measurement types")

async def get_latest_measurements(
    sensor_names: list[str],
    type_names: list[str] | None = None
) -> dict:
    """Get the most recent measurements from sensors.

    Use this to:
    - Check current temperature from specific sensors
    - Get latest readings from air quality monitors
    - Compare current values across sensors
    """
    pass
```

**Tool: `get_historical_measurements`**
```python
class GetHistoricalMeasurementsInput(BaseModel):
    sensor_names: list[str]
    type_names: list[str]
    from_time: str = Field(description="ISO 8601 timestamp")
    to_time: str = Field(description="ISO 8601 timestamp")

async def get_historical_measurements(
    sensor_names: list[str],
    type_names: list[str],
    from_time: str,
    to_time: str
) -> dict:
    """Get historical measurements within a time range.

    Use this to:
    - Analyze trends over time
    - Compare measurements across periods
    - Generate time-series visualizations
    """
    pass
```

**Tool: `find_timeseries_for_entities`**
```python
async def find_timeseries_for_entities(entry_ids: list[str]) -> dict:
    """Find which dataset entries have timeseries data attached.

    Use this to:
    - Check if accommodations have temperature sensors
    - Discover what measurements are available for POIs
    """
    pass
```

#### 3. Analysis Tools

**Tool: `calculate_statistics`**
```python
async def calculate_statistics(values: list[float]) -> dict:
    """Calculate statistical summary of numeric values.

    Returns: mean, median, std, min, max, percentiles
    """
    import numpy as np
    return {
        "mean": np.mean(values),
        "median": np.median(values),
        "std": np.std(values),
        "min": np.min(values),
        "max": np.max(values),
        "percentiles": {
            "25": np.percentile(values, 25),
            "75": np.percentile(values, 75),
            "90": np.percentile(values, 90)
        }
    }
```

**Tool: `compare_sensor_trends`**
```python
async def compare_sensor_trends(
    sensor_data: dict[str, list[dict]]
) -> dict:
    """Compare trends across multiple sensors.

    Analyzes:
    - Which sensors have increasing/decreasing trends
    - Correlation between sensors
    - Anomaly detection
    """
    pass
```

**Tool: `analyze_geographic_distribution`**
```python
async def analyze_geographic_distribution(
    entries: list[dict]
) -> dict:
    """Analyze geographic distribution of entries.

    Returns:
    - Region clustering
    - Geographic density
    - Distance calculations
    """
    pass
```

#### 4. Navigation Tools

**Tool: `generate_navigation_url`**
```python
class NavigationInput(BaseModel):
    route: str = Field(description="Route path like /datasets/Accommodation")
    query_params: dict = Field(default_factory=dict, description="URL query parameters")

async def generate_navigation_url(
    route: str,
    query_params: dict = {}
) -> dict:
    """Generate navigation command to show results in webapp.

    Use this to:
    - Navigate to dataset inspector with filters applied
    - Open timeseries inspector with specific types
    - Show bulk measurements view

    Returns navigation command for frontend to execute.
    """
    return {
        "action": "navigate",
        "route": route,
        "query": query_params,
        "reason": "Show results in webapp"
    }
```

---

## Data Preprocessing & Response Management

### The Problem: Large API Responses

API responses can be **extremely large** and must be preprocessed before sending to the LLM:

**Real-world examples**:
- **Content API**: 5,000 accommodations × 50KB each = **250MB** of JSON
- **Timeseries API**: 10 sensors × 1000 measurements × 500 bytes = **5MB** per query
- **Dataset entries**: Deeply nested objects with 50+ fields, many unused

**Consequences of sending raw data to LLM**:
- 💰 **Cost explosion**: GPT-4 Turbo charges per token (~$10/1M input tokens)
- 🚫 **Context limits**: Even GPT-4 Turbo's 128K tokens ≈ 100K words ≈ 50 complex JSON entries
- 🐌 **Slow responses**: Processing 50K tokens takes 10-30 seconds
- 🎯 **Poor quality**: LLM gets overwhelmed with irrelevant data, hallucinates more

**Solution**: Implement a **preprocessing layer** that cleans, filters, aggregates, and summarizes data BEFORE it reaches the LLM.

---

### Standard Approaches & Patterns

#### 1. Strategic Filtering (Field Projection)

**When to use**: When you only need specific fields from large objects.

**Implementation**:
```python
def extract_relevant_fields(entries: list[dict], fields: list[str]) -> list[dict]:
    """Extract only necessary fields from dataset entries."""
    return [
        {field: entry.get(field) for field in fields if field in entry}
        for entry in entries
    ]

# Example usage in tool
async def get_dataset_entries(dataset_name: str, ...) -> dict:
    response = await api_client.get(...)
    raw_entries = response['Items']

    # Only extract fields needed for the question
    cleaned_entries = extract_relevant_fields(
        raw_entries,
        fields=['Id', 'Shortname', 'Active', 'LocationInfo.Municipality']
    )

    return {
        'total': response['TotalResults'],
        'entries': cleaned_entries  # Much smaller!
    }
```

**Token reduction**: 50KB → 2KB per entry (96% reduction)

---

#### 2. Aggregation & Statistics

**When to use**: When the user asks for summaries, counts, or trends rather than individual entries.

**Implementation**:
```python
import pandas as pd
from collections import Counter

def aggregate_dataset(entries: list[dict]) -> dict:
    """Aggregate dataset entries into statistical summary."""

    df = pd.DataFrame(entries)

    return {
        'total_count': len(entries),
        'active_count': len(df[df['Active'] == True]) if 'Active' in df else 0,
        'by_type': dict(Counter(df['Type'].dropna())) if 'Type' in df else {},
        'by_region': dict(Counter(df.get('LocationInfo', {}).get('RegionInfo', {}).get('Name', {}).get('en')).dropna()) if 'LocationInfo' in df else {},
        'sample_entries': entries[:5],  # Show only first 5 as examples
        'unique_fields': list(df.columns)
    }

# In tool
async def analyze_dataset_summary(dataset_name: str, filters: str = None):
    """Get aggregated summary instead of raw entries."""
    # Fetch all pages (internally)
    all_entries = await fetch_all_pages(dataset_name, filters)

    # Aggregate before returning to LLM
    summary = aggregate_dataset(all_entries)

    return summary
```

**Example output to LLM**:
```json
{
  "total_count": 5432,
  "active_count": 5124,
  "by_type": {"Hotel": 3200, "BedBreakfast": 1500, "Apartment": 732},
  "by_region": {"Bolzano": 2100, "Merano": 1800, "Bressanone": 1532},
  "sample_entries": [
    {"Id": "hotel-123", "Shortname": "Grand Hotel Bolzano", "Active": true},
    {"Id": "hotel-456", "Shortname": "Alpine Retreat", "Active": true}
  ]
}
```

**Token reduction**: 5000 entries × 50KB = 250MB → 2KB summary (99.9999% reduction)

---

#### 3. Map-Reduce Pattern (LangChain Built-in)

**When to use**: When analyzing large documents or datasets that exceed context window.

**How it works**:
1. **Map**: Break data into chunks, process each chunk with LLM
2. **Reduce**: Combine chunk summaries into final answer

**Implementation with LangChain**:
```python
from langchain.chains import MapReduceDocumentsChain, LLMChain
from langchain.chains.combine_documents.stuff import StuffDocumentsChain
from langchain.prompts import PromptTemplate
from langchain.schema import Document

# Map prompt: Summarize each chunk
map_template = """Analyze this subset of accommodations:
{docs}

Extract:
- Total count
- Common characteristics
- Geographic distribution
"""
map_prompt = PromptTemplate.from_template(map_template)
map_chain = LLMChain(llm=llm, prompt=map_prompt)

# Reduce prompt: Combine summaries
reduce_template = """Combine these summaries into a final answer:
{doc_summaries}

Provide overall statistics and insights.
"""
reduce_prompt = PromptTemplate.from_template(reduce_template)
reduce_chain = LLMChain(llm=llm, prompt=reduce_prompt)

# Combine into Map-Reduce chain
combine_chain = StuffDocumentsChain(
    llm_chain=reduce_chain,
    document_variable_name="doc_summaries"
)

map_reduce_chain = MapReduceDocumentsChain(
    llm_chain=map_chain,
    reduce_documents_chain=combine_chain,
    document_variable_name="docs"
)

# Use it
async def analyze_large_dataset(entries: list[dict]) -> str:
    """Analyze dataset that's too large for single LLM call."""

    # Convert entries to LangChain Documents (chunks of 50 entries each)
    docs = [
        Document(page_content=json.dumps(entries[i:i+50]))
        for i in range(0, len(entries), 50)
    ]

    # Run map-reduce
    result = await map_reduce_chain.arun(input_documents=docs)
    return result
```

**Token usage**: 5000 entries → 100 chunks of 50 → 100 LLM calls (map) + 1 LLM call (reduce) = 101 calls but each is small

**Cost**: 101 × 2K tokens = 202K tokens instead of 50M tokens (99.6% reduction)

---

#### 4. Semantic Chunking with Summarization

**When to use**: For time-series data or measurement analysis over large time ranges.

**Implementation**:
```python
from langchain.text_splitter import RecursiveCharacterTextSplitter

async def summarize_measurements(measurements: list[dict]) -> dict:
    """Summarize large measurement datasets."""

    # Group by sensor
    by_sensor = {}
    for m in measurements:
        sensor = m['sensor_name']
        if sensor not in by_sensor:
            by_sensor[sensor] = []
        by_sensor[sensor].append(m)

    # Aggregate per sensor
    summaries = {}
    for sensor, data in by_sensor.items():
        values = [m['value'] for m in data if isinstance(m['value'], (int, float))]

        summaries[sensor] = {
            'measurement_count': len(data),
            'time_range': {
                'from': min(m['timestamp'] for m in data),
                'to': max(m['timestamp'] for m in data)
            },
            'statistics': {
                'mean': np.mean(values) if values else None,
                'min': np.min(values) if values else None,
                'max': np.max(values) if values else None,
                'std': np.std(values) if values else None
            },
            'sample_values': data[:5]  # First 5 measurements as examples
        }

    return summaries
```

**Example**:
- Input: 10,000 measurements × 500 bytes = 5MB
- Output: 10 sensor summaries × 200 bytes = 2KB (99.96% reduction)

---

#### 5. Intelligent Tool Design with Pre-aggregation

**Pattern**: Create specialized tools that return pre-aggregated data instead of raw data.

**Bad Tool** (returns raw data):
```python
async def get_all_accommodations() -> list[dict]:
    """Get all accommodations."""
    # Returns 5000 entries × 50KB = 250MB
    return await api.get('/Accommodation?pagesize=10000')
```

**Good Tool** (returns aggregated data):
```python
async def count_accommodations_by_region(
    active_only: bool = True
) -> dict:
    """Count accommodations grouped by region.

    Returns aggregated statistics instead of raw entries.
    """
    # Fetch data
    entries = await api.get(
        '/Accommodation',
        params={'rawfilter': 'Active eq true' if active_only else None}
    )

    # Aggregate BEFORE returning to LLM
    by_region = Counter(
        e.get('LocationInfo', {}).get('RegionInfo', {}).get('Name', {}).get('en', 'Unknown')
        for e in entries['Items']
    )

    return {
        'total': entries['TotalResults'],
        'by_region': dict(by_region),
        'top_5_regions': dict(by_region.most_common(5))
    }
```

---

### Tool Implementation Strategy

Each tool should implement a **preprocessing pipeline**:

```python
from typing import Callable, Any

class SmartTool:
    """Base class for tools with built-in preprocessing."""

    def __init__(
        self,
        name: str,
        api_call: Callable,
        preprocessor: Callable[[Any], dict],
        max_tokens: int = 2000
    ):
        self.name = name
        self.api_call = api_call
        self.preprocessor = preprocessor
        self.max_tokens = max_tokens

    async def execute(self, **kwargs) -> dict:
        """Execute tool with automatic preprocessing."""

        # 1. Call API
        raw_response = await self.api_call(**kwargs)

        # 2. Preprocess (clean, filter, aggregate)
        processed_response = self.preprocessor(raw_response)

        # 3. Verify token count
        token_count = count_tokens(json.dumps(processed_response))

        if token_count > self.max_tokens:
            # Apply further summarization if still too large
            processed_response = await self.emergency_summarize(processed_response)

        return processed_response

    async def emergency_summarize(self, data: dict) -> dict:
        """Use LLM to summarize if data is still too large."""

        summary_prompt = f"""Summarize this data concisely:
        {json.dumps(data, indent=2)}

        Extract:
        - Key statistics
        - Top 5 important items
        - Overall insights
        """

        summary = await llm.ainvoke(summary_prompt)
        return {'summary': summary, 'token_warning': 'Data was too large, summarized'}


# Example usage
get_accommodations_tool = SmartTool(
    name="get_accommodations",
    api_call=lambda **kw: content_api.get('/Accommodation', **kw),
    preprocessor=lambda r: {
        'total': r['TotalResults'],
        'active_count': sum(1 for e in r['Items'] if e.get('Active')),
        'sample': r['Items'][:5],
        'by_type': Counter(e.get('Type') for e in r['Items'])
    },
    max_tokens=2000
)
```

---

### Libraries & Utilities

**Essential Libraries**:

```python
# requirements.txt additions
pandas>=2.0.0          # Data aggregation, analysis
numpy>=1.24.0          # Statistical calculations
tiktoken>=0.5.0        # Token counting (OpenAI)
langchain-text-splitters>=0.0.1  # Smart chunking
```

**Utility Functions**:

```python
import tiktoken
import json

# Token counting
def count_tokens(text: str, model: str = "gpt-4") -> int:
    """Count tokens in text using tiktoken."""
    encoding = tiktoken.encoding_for_model(model)
    return len(encoding.encode(text))

# Smart truncation
def truncate_to_token_limit(data: dict, max_tokens: int = 2000) -> dict:
    """Truncate data to fit within token limit."""
    text = json.dumps(data, indent=2)
    tokens = count_tokens(text)

    if tokens <= max_tokens:
        return data

    # Progressively remove less important fields
    if 'Items' in data and isinstance(data['Items'], list):
        # Keep only first N items that fit
        for n in range(len(data['Items']), 0, -1):
            truncated = {**data, 'Items': data['Items'][:n]}
            if count_tokens(json.dumps(truncated)) <= max_tokens:
                truncated['_truncated'] = True
                truncated['_original_count'] = len(data['Items'])
                return truncated

    return {'error': 'Data too large to process', 'summary': str(data)[:1000]}

# Nested field extraction
def extract_nested_field(obj: dict, path: str, default=None):
    """Extract value from nested dict using dot notation.

    Example: extract_nested_field(entry, 'LocationInfo.RegionInfo.Name.en')
    """
    keys = path.split('.')
    current = obj

    for key in keys:
        if isinstance(current, dict):
            current = current.get(key, default)
        else:
            return default

    return current
```

---

### Decision Tree: Which Strategy to Use?

```
User asks question
│
├─ Needs count/aggregate?
│  └─→ Use aggregation tool (count_accommodations_by_region)
│
├─ Needs specific entry details?
│  ├─ Single entry? → Fetch by ID, extract relevant fields
│  └─ Multiple entries? → Fetch with pagesize limit, extract fields
│
├─ Needs statistical analysis?
│  └─→ Fetch data, aggregate with pandas/numpy, return stats
│
├─ Needs trend analysis over time?
│  └─→ Fetch measurements, group by time windows, aggregate
│
└─ Needs full analysis of large dataset?
   ├─ <100 entries? → Fetch all, send to LLM
   ├─ 100-1000 entries? → Fetch all, pre-aggregate, send summary
   └─ >1000 entries? → Use Map-Reduce pattern
```

---

### Example: Preprocessing in Action

**User Question**: "What are the most common accommodation types in Bolzano?"

**Without Preprocessing** (❌ Bad):
```python
async def bad_approach():
    # Fetch all accommodations
    response = await content_api.get('/Accommodation?pagesize=5000')
    # Send entire 250MB response to LLM
    return response  # LLM cost: $500+, takes 60 seconds, might exceed context
```

**With Preprocessing** (✅ Good):
```python
async def good_approach():
    # Fetch accommodations in Bolzano
    response = await content_api.get(
        '/Accommodation',
        params={
            'rawfilter': "LocationInfo.MunicipalityInfo.Name.en eq 'Bolzano'",
            'pagesize': 1000  # Reasonable limit
        }
    )

    entries = response['Items']

    # Aggregate by type
    type_counts = Counter(e.get('AccoTypeId') for e in entries if e.get('AccoTypeId'))

    # Return minimal summary
    return {
        'total_in_bolzano': len(entries),
        'by_type': dict(type_counts.most_common(10)),
        'sample_entries': [
            {'name': e.get('Shortname'), 'type': e.get('AccoTypeId')}
            for e in entries[:5]
        ]
    }
    # LLM cost: $0.01, takes 2 seconds, fits easily
```

**Token Comparison**:
- Bad: 250MB JSON ≈ 62.5M tokens ≈ $625 cost
- Good: 2KB JSON ≈ 500 tokens ≈ $0.005 cost
- **Savings**: 99.9992% reduction

---

### Integration with LangGraph

**Add preprocessing node to the graph**:

```python
from langgraph.graph import StateGraph, END

# Define graph
workflow = StateGraph(AgentState)

# Add preprocessing node BEFORE executor
async def preprocess_tool_results(state: AgentState) -> AgentState:
    """Preprocess tool results to reduce token usage."""

    processed_results = []

    for result in state['tool_results']:
        # Count tokens
        tokens = count_tokens(json.dumps(result))

        if tokens > 2000:
            # Too large, apply strategy
            if 'Items' in result:
                # Aggregate list results
                processed = aggregate_items(result['Items'])
            elif 'measurements' in result:
                # Summarize measurements
                processed = summarize_measurements(result['measurements'])
            else:
                # Generic truncation
                processed = truncate_to_token_limit(result)

            processed_results.append(processed)
        else:
            # Small enough, pass through
            processed_results.append(result)

    state['tool_results'] = processed_results
    return state

# Add to workflow
workflow.add_node("executor", execute_tools)
workflow.add_node("preprocessor", preprocess_tool_results)  # NEW
workflow.add_node("analyzer", analyze_results)

# Connect: executor → preprocessor → analyzer
workflow.add_edge("executor", "preprocessor")
workflow.add_edge("preprocessor", "analyzer")
```

---

### Performance Impact

**Metrics** (from production LangChain applications):

| Strategy | Token Reduction | Cost Reduction | Speed Improvement |
|----------|----------------|----------------|-------------------|
| Field projection | 70-90% | 70-90% | 2-3x faster |
| Aggregation | 95-99.9% | 95-99.9% | 5-10x faster |
| Map-Reduce | 90-99% | 80-95% (multiple calls) | 2-4x faster |
| Summarization | 80-95% | 80-95% (extra LLM call) | 1-2x faster |
| Combined | 99-99.99% | 99-99.99% | 10-50x faster |

---

### Best Practices Summary

1. ✅ **Always count tokens** before sending to LLM
2. ✅ **Extract only needed fields** from API responses
3. ✅ **Aggregate when possible** (counts, stats, summaries)
4. ✅ **Use pandas/numpy** for efficient data processing
5. ✅ **Implement emergency summarization** as fallback
6. ✅ **Cache preprocessed results** for common queries
7. ✅ **Set token limits per tool** (default: 2000 tokens)
8. ✅ **Log preprocessing metrics** to optimize over time
9. ✅ **Test with real data sizes** during development
10. ✅ **Prefer specialized tools** over generic ones

---

## Reasoning Flow

### Prompt Engineering Strategy

**System Prompt** (stored in `agent/prompts.py`):

```python
SYSTEM_PROMPT = """You are an intelligent assistant for the OdhDiscovery platform,
which provides access to Open Data Hub tourism datasets and timeseries sensor data.

Your capabilities:
- Query and analyze tourism datasets (Accommodations, Activities, POIs, Events, etc.)
- Access timeseries sensor data and measurements
- Perform statistical analysis and comparisons
- Navigate the webapp to show results visually

Available APIs:
1. Content API - Tourism datasets with 27+ types of entities
2. Timeseries API - Sensor measurements (temperature, air quality, etc.)

Key concepts:
- Dataset entries can have timeseries attached (entry ID = sensor name)
- Use filters to narrow down results before analysis
- Always justify your findings with data
- When appropriate, generate navigation commands to show results visually

Response guidelines:
- Be concise but informative
- Show your reasoning process
- Suggest visualizations when helpful
- Provide specific numbers and statistics
- If you need clarification, ask the user

Current date: {current_date}
"""
```

**Few-Shot Examples** (for in-context learning):

```python
FEW_SHOT_EXAMPLES = [
    {
        "user": "How many active accommodations are in South Tyrol?",
        "assistant_reasoning": """
        1. Need to query Accommodation dataset
        2. Apply filter: Active eq true
        3. Get total count
        """,
        "tool_calls": [
            {"tool": "get_dataset_entries", "args": {"dataset_name": "Accommodation", "raw_filter": "Active eq true", "pagesize": 1}}
        ],
        "response": "There are 5,432 active accommodations in South Tyrol based on the latest data."
    },
    {
        "user": "What's the current temperature at sensor XYZ?",
        "assistant_reasoning": """
        1. Get latest measurements for sensor XYZ
        2. Filter by type 'temperature'
        3. Return current value
        """,
        "tool_calls": [
            {"tool": "get_latest_measurements", "args": {"sensor_names": ["XYZ"], "type_names": ["temperature"]}}
        ],
        "response": "The current temperature at sensor XYZ is 18.5°C (measured at 2025-01-15 14:30 UTC)."
    }
]
```

### ReAct Pattern Implementation

**ReAct Loop** (Reasoning + Acting):

```python
async def react_loop(state: AgentState, max_iterations: int = 5) -> AgentState:
    """Execute ReAct pattern: Thought → Action → Observation → repeat."""

    for iteration in range(max_iterations):
        # 1. THOUGHT: LLM decides what to do next
        thought_prompt = f"""
        Question: {state['user_question']}

        Previous actions: {state['tool_calls']}
        Previous observations: {state['tool_results']}

        What should you do next? Think step by step:
        1. Do you have enough information to answer?
        2. If not, what tool should you use?
        3. What are the parameters?
        """

        thought = await llm.ainvoke(thought_prompt)

        # 2. ACTION: Execute tool if needed
        if thought.tool_calls:
            action = thought.tool_calls[0]
            tool_result = await execute_tool(action)

            state['tool_calls'].append(action)
            state['tool_results'].append(tool_result)
        else:
            # No more actions needed, ready to respond
            state['found_answer'] = True
            break

    return state
```

### Planning for Complex Questions

For multi-step questions like "Compare average temperatures in January vs July for mountain sensors":

```python
async def plan_complex_question(question: str) -> list[str]:
    """Break down complex question into executable steps."""

    planning_prompt = f"""
    Break down this question into specific steps:
    Question: {question}

    Available tools:
    - get_timeseries_types
    - get_sensors_for_type
    - get_historical_measurements
    - calculate_statistics
    - compare_sensor_trends

    Create a numbered plan:
    """

    plan = await llm.ainvoke(planning_prompt)
    return parse_plan(plan)

# Example output:
# 1. Find all sensors with 'mountain' in location
# 2. Get historical temperature data for January (01-01 to 01-31)
# 3. Get historical temperature data for July (07-01 to 07-31)
# 4. Calculate average temperature for each month
# 5. Compare and report difference
```

---

## Frontend Integration

### Chat Widget Component

**ChatWidget.vue**:

```vue
<template>
  <div class="chat-widget" :class="{ open: isOpen }">
    <!-- Chat Header -->
    <div class="chat-header" @click="toggleChat">
      <div class="chat-title">
        <IconBot />
        <span>OdhDiscovery Assistant</span>
      </div>
      <button class="minimize-btn" v-if="isOpen">−</button>
    </div>

    <!-- Chat Body -->
    <div v-if="isOpen" class="chat-body">
      <div class="messages-container" ref="messagesContainer">
        <ChatMessage
          v-for="message in messages"
          :key="message.id"
          :message="message"
          @navigate="handleNavigation"
        />

        <TypingIndicator v-if="isAgentTyping" />
      </div>

      <!-- Input Area -->
      <ChatInput
        @send="handleSendMessage"
        :disabled="isAgentTyping"
      />
    </div>

    <!-- Floating Button (when closed) -->
    <button
      v-if="!isOpen"
      class="chat-fab"
      @click="toggleChat"
    >
      <IconBot />
    </button>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useChatbot } from '../composables/useChatbot'
import { useAgentWebSocket } from '../composables/useAgentWebSocket'

const router = useRouter()
const { messages, addMessage, clearMessages } = useChatbot()
const { connect, disconnect, sendMessage, onMessage, isConnected } = useAgentWebSocket()

const isOpen = ref(false)
const isAgentTyping = ref(false)

onMounted(async () => {
  await connect()

  // Listen for agent responses
  onMessage((data) => {
    if (data.type === 'message') {
      addMessage({
        id: Date.now(),
        role: 'assistant',
        content: data.content,
        timestamp: new Date()
      })
      isAgentTyping.value = false
    }

    if (data.type === 'navigation') {
      // Agent wants to navigate the app
      handleNavigation(data.command)
    }

    if (data.type === 'typing') {
      isAgentTyping.value = data.isTyping
    }
  })
})

onUnmounted(() => {
  disconnect()
})

function toggleChat() {
  isOpen.value = !isOpen.value
}

async function handleSendMessage(text) {
  // Add user message to chat
  addMessage({
    id: Date.now(),
    role: 'user',
    content: text,
    timestamp: new Date()
  })

  // Send to agent
  isAgentTyping.value = true
  await sendMessage({
    type: 'question',
    content: text,
    context: {
      currentRoute: router.currentRoute.value.path,
      currentParams: router.currentRoute.value.query
    }
  })
}

function handleNavigation(command) {
  // Navigate to route specified by agent
  router.push({
    path: command.route,
    query: command.query
  })

  // Show notification
  addMessage({
    id: Date.now(),
    role: 'system',
    content: `Navigated to ${command.route}`,
    timestamp: new Date()
  })
}
</script>

<style scoped>
.chat-widget {
  position: fixed;
  bottom: 20px;
  right: 20px;
  width: 400px;
  max-height: 600px;
  background: white;
  border-radius: 12px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
  display: flex;
  flex-direction: column;
  z-index: 1000;
}

.chat-fab {
  position: fixed;
  bottom: 20px;
  right: 20px;
  width: 60px;
  height: 60px;
  border-radius: 50%;
  background: var(--primary-color);
  color: white;
  border: none;
  cursor: pointer;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
  transition: transform 0.2s;
}

.chat-fab:hover {
  transform: scale(1.1);
}
</style>
```

### WebSocket Composable

**useAgentWebSocket.js**:

```javascript
import { ref } from 'vue'

const AGENT_WS_URL = import.meta.env.VITE_AGENT_WS_URL || 'ws://localhost:8001/ws'

export function useAgentWebSocket() {
  const socket = ref(null)
  const isConnected = ref(false)
  const messageHandlers = []

  async function connect() {
    return new Promise((resolve, reject) => {
      socket.value = new WebSocket(AGENT_WS_URL)

      socket.value.onopen = () => {
        console.log('Connected to agent')
        isConnected.value = true
        resolve()
      }

      socket.value.onerror = (error) => {
        console.error('WebSocket error:', error)
        reject(error)
      }

      socket.value.onmessage = (event) => {
        const data = JSON.parse(event.data)
        messageHandlers.forEach(handler => handler(data))
      }

      socket.value.onclose = () => {
        console.log('Disconnected from agent')
        isConnected.value = false
      }
    })
  }

  function disconnect() {
    if (socket.value) {
      socket.value.close()
      socket.value = null
    }
  }

  async function sendMessage(message) {
    if (!isConnected.value) {
      throw new Error('Not connected to agent')
    }

    socket.value.send(JSON.stringify(message))
  }

  function onMessage(handler) {
    messageHandlers.push(handler)
    return () => {
      const index = messageHandlers.indexOf(handler)
      if (index > -1) {
        messageHandlers.splice(index, 1)
      }
    }
  }

  return {
    connect,
    disconnect,
    sendMessage,
    onMessage,
    isConnected
  }
}
```

### Message Types

**User → Agent**:
```json
{
  "type": "question",
  "content": "How many active hotels are in Bolzano?",
  "context": {
    "currentRoute": "/datasets/Accommodation",
    "currentParams": { "page": "1", "pagesize": "50" }
  },
  "conversationId": "uuid-123"
}
```

**Agent → User** (streaming response):
```json
{
  "type": "message_chunk",
  "content": "I found ",
  "conversationId": "uuid-123"
}
```

**Agent → User** (navigation command):
```json
{
  "type": "navigation",
  "command": {
    "route": "/datasets/Accommodation",
    "query": {
      "rawfilter": "Active eq true and LocationInfo.MunicipalityInfo.Name.en eq 'Bolzano'",
      "pagesize": "50"
    }
  },
  "reason": "Showing filtered accommodations in Bolzano",
  "conversationId": "uuid-123"
}
```

---

## Vector Database & Knowledge Base

### What to Store in Vector DB

1. **API Documentation**
   - Content API endpoints and parameters
   - Timeseries API endpoints and parameters
   - Filter syntax examples
   - Sort syntax examples

2. **Dataset Schemas**
   - Field descriptions for each dataset type
   - Data types and example values
   - Common filters per dataset

3. **Example Queries**
   - User question → Tool calls mapping
   - Common patterns and solutions

4. **Domain Knowledge**
   - Tourism terminology
   - Geographic regions in South Tyrol
   - Sensor types and their typical ranges

### Embedding & Retrieval Strategy

**Document Structure**:
```json
{
  "id": "doc_001",
  "content": "To filter active accommodations in Bolzano, use rawfilter: 'Active eq true and LocationInfo.MunicipalityInfo.Name.en eq Bolzano'",
  "metadata": {
    "category": "filter_example",
    "dataset": "Accommodation",
    "operation": "filtering"
  },
  "embedding": [0.123, 0.456, ...]  // Generated by OpenAI or sentence-transformers
}
```

**Retrieval Process**:

```python
async def retrieve_relevant_context(question: str, top_k: int = 5) -> list[str]:
    """Retrieve relevant context from vector DB."""

    # Generate embedding for question
    question_embedding = await embedding_model.aembed_query(question)

    # Query ChromaDB
    results = vector_store.query(
        query_embeddings=[question_embedding],
        n_results=top_k,
        where={"category": {"$in": ["filter_example", "api_doc"]}}  # Optional filtering
    )

    return results['documents'][0]
```

**ChromaDB Setup**:

```python
import chromadb
from chromadb.config import Settings
from langchain.embeddings import OpenAIEmbeddings

# Initialize ChromaDB
chroma_client = chromadb.PersistentClient(
    path="./data/embeddings",
    settings=Settings(anonymized_telemetry=False)
)

# Create collection
collection = chroma_client.get_or_create_collection(
    name="odh_knowledge",
    metadata={"description": "OdhDiscovery knowledge base"}
)

# Index documents
embeddings_model = OpenAIEmbeddings()

def index_documents(documents: list[dict]):
    """Add documents to vector store."""

    for doc in documents:
        embedding = embeddings_model.embed_query(doc['content'])

        collection.add(
            ids=[doc['id']],
            embeddings=[embedding],
            documents=[doc['content']],
            metadatas=[doc['metadata']]
        )
```

---

## Implementation Phases

### Phase 1: Foundation (Week 1-2)

**Backend**:
- [x] Set up FastAPI project structure
- [x] Configure environment variables (API keys, endpoints)
- [x] Implement basic WebSocket endpoint
- [x] Create HTTP client for Content/Timeseries APIs
- [x] Write first 3-5 tools (get_dataset_entries, get_timeseries_types, get_latest_measurements)
- [x] Set up ChromaDB and embed basic API documentation
- [x] Create simple LangChain chain (no graph yet)

**Frontend**:
- [x] Create ChatWidget.vue component
- [x] Implement WebSocket composable
- [x] Add chat to main App.vue
- [x] Basic message rendering (text only)

**Testing**:
- Simple question: "How many accommodations are there?"
- Tool calling: Verify API calls work

**Deliverable**: Working chatbot that can answer basic questions using 3-5 tools.

---

### Phase 2: Agent Intelligence (Week 3-4)

**Backend**:
- [x] Implement LangGraph state machine
- [x] Add all remaining tools (10-15 total)
- [x] Implement ReAct loop
- [x] Add planning node for complex questions
- [x] Improve prompt engineering
- [x] Add conversation history tracking

**Frontend**:
- [x] Enhanced message rendering (markdown, code blocks)
- [x] Typing indicator
- [x] Message timestamps
- [x] Error handling and retry

**Testing**:
- Complex question: "Compare temperature trends in January vs July"
- Multi-step reasoning verification
- Edge cases and error handling

**Deliverable**: Intelligent agent that can handle complex, multi-step questions.

---

### Phase 3: Navigation & Visualization (Week 5-6)

**Backend**:
- [x] Implement navigation command generation
- [x] Add navigation_generator node to graph
- [x] Create logic to determine when navigation is appropriate
- [x] Add URL parameter construction

**Frontend**:
- [x] Handle navigation commands from agent
- [x] Show navigation suggestions in chat
- [x] Add "View in App" buttons for results
- [x] Sync chat state with router

**Testing**:
- Question: "Show me active hotels in Bolzano" → Should navigate to filtered view
- Verify URL parameters are correct
- Test navigation from different pages

**Deliverable**: Agent can navigate webapp to show results visually.

---

### Phase 4: Advanced Analysis (Week 7-8)

**Backend**:
- [x] Implement statistical analysis tools
- [x] Add trend analysis capabilities
- [x] Geographic analysis tools
- [x] Comparison and correlation tools
- [x] Anomaly detection

**Frontend**:
- [x] Rich message components (charts in chat)
- [x] Interactive suggestions
- [x] Quick action buttons

**Testing**:
- Statistical questions: "What's the average temperature?"
- Trend questions: "Is air quality improving?"
- Comparison: "Which region has more hotels?"

**Deliverable**: Agent can perform complex data analysis and present insights.

---

### Phase 5: Polish & Optimization (Week 9-10)

**Backend**:
- [x] Optimize token usage (summarization, pruning)
- [x] Add caching for frequent queries
- [x] Improve error messages
- [x] Add rate limiting
- [x] Performance monitoring

**Frontend**:
- [x] UI polish and animations
- [x] Mobile responsive design
- [x] Accessibility (keyboard navigation)
- [x] User feedback collection

**Testing**:
- Load testing (concurrent users)
- Token usage analysis
- User acceptance testing

**Deliverable**: Production-ready chatbot with polished UX.

---

## Example Conversations

### Example 1: Simple Data Query

**User**: "How many active accommodations are in South Tyrol?"

**Agent Internal Process**:
```
1. Classifier: Simple question, direct tool call
2. Tool: get_dataset_entries
   - dataset_name: "Accommodation"
   - raw_filter: "Active eq true"
   - pagesize: 1 (only need count)
3. Result: { TotalResults: 5432 }
4. Response: Generate natural language
```

**Agent Response**:
```
There are 5,432 active accommodations in South Tyrol based on the latest data in the system.

Would you like me to show you these accommodations or filter them further (e.g., by location, type)?
```

---

### Example 2: Complex Analysis with Navigation

**User**: "What's the average temperature in Bolzano this month?"

**Agent Internal Process**:
```
1. Classifier: Complex question, needs planning
2. Plan:
   a. Find sensors in Bolzano measuring temperature
   b. Get current month date range
   c. Fetch historical measurements for this month
   d. Calculate average
   e. Generate navigation to show data

3. Execute:
   a. Tool: get_sensors_for_type("temperature")
      Filter by location: Bolzano
   b. Tool: get_historical_measurements
      from: 2025-01-01, to: 2025-01-15
      sensor_names: [sensor1, sensor2, sensor3]
      type_names: ["temperature"]
   c. Tool: calculate_statistics
      values: [all temperature values]
   d. Tool: generate_navigation_url
      route: /bulk-measurements
      query: { sensors: "sensor1,sensor2,sensor3", types: "temperature" }

4. Response with navigation
```

**Agent Response**:
```
I analyzed temperature data from 3 sensors in Bolzano for January 2025.

Average temperature: 4.2°C
- Minimum: -2.1°C
- Maximum: 12.5°C
- Standard deviation: 3.8°C

[View Detailed Measurements] ← (navigation button)
```

**Navigation Action**:
- Route: `/bulk-measurements?sensors=bolzano-temp-1,bolzano-temp-2,bolzano-temp-3&types=temperature&view=pretty`
- User sees chart with temperature trend for the month

---

### Example 3: Dataset Discovery

**User**: "I'm planning a hiking trip. What outdoor activities are available near Merano?"

**Agent Internal Process**:
```
1. Classifier: Complex query, needs filtering and presentation
2. Plan:
   a. Query Activity dataset
   b. Filter by location (Merano area)
   c. Filter by outdoor/hiking categories
   d. Navigate to show results

3. Execute:
   a. Tool: get_dataset_entries
      dataset_name: "Activity"
      raw_filter: "LocationInfo.MunicipalityInfo.Name.en eq 'Merano' and Type eq 'Wandern'"
   b. Result: 87 activities found
   c. Tool: generate_navigation_url
      route: /datasets/Activity
      query: { rawfilter: "...", pagesize: 50 }

4. Response with navigation
```

**Agent Response**:
```
I found 87 hiking and outdoor activities in the Merano area! These include:
- Mountain hikes (42)
- Nature trails (28)
- Via ferratas (12)
- Other outdoor activities (5)

I've prepared a filtered view showing all these activities. Click below to explore:

[View All Activities in Merano] ← (navigation button)

You can further filter by difficulty level, duration, or elevation gain in the webapp.
```

---

## Security & Performance

### Security Considerations

1. **API Key Protection**
   - Store LLM API keys in environment variables
   - Never expose keys to frontend
   - Rotate keys regularly

2. **Rate Limiting**
   - Limit messages per user per minute
   - Prevent expensive tool calls abuse
   - Implement token budgets per conversation

3. **Input Validation**
   - Sanitize user inputs before sending to LLM
   - Validate tool parameters with Pydantic
   - Prevent injection attacks

4. **Access Control**
   - (Optional) Implement authentication
   - Track usage per user
   - Audit trail for sensitive queries

### Performance Optimization

1. **Caching**
   ```python
   from functools import lru_cache

   @lru_cache(maxsize=100)
   async def get_dataset_metadata(dataset_name: str):
       """Cache dataset metadata (doesn't change often)."""
       pass
   ```

2. **Token Management**
   - Summarize long conversations
   - Prune old messages from context
   - Use sliding window for history

3. **Async Operations**
   - All API calls use httpx async
   - Parallel tool execution when possible
   - Stream responses to frontend

4. **Response Streaming**
   ```python
   async def stream_response(message: str):
       """Stream response chunks to frontend."""
       for chunk in message.split():
           yield {
               "type": "message_chunk",
               "content": chunk + " "
           }
           await asyncio.sleep(0.05)  # Simulate typing
   ```

---

## Deployment

### Development Setup

**Backend**:
```bash
cd chatbot-backend
python -m venv venv
source venv/bin/activate
pip install -r requirements.txt

# Set environment variables
export OPENAI_API_KEY="sk-..."
export CONTENT_API_BASE="http://localhost:8080/api/v1/content"
export TIMESERIES_API_BASE="http://localhost:8080/api/v1/timeseries"

# Run server
uvicorn app.main:app --reload --port 8001
```

**Frontend**:
```bash
cd webapp
npm install

# Add to .env.local
VITE_AGENT_WS_URL=ws://localhost:8001/ws

npm run dev
```

### Docker Deployment

**docker-compose.yml**:
```yaml
version: '3.8'

services:
  chatbot-backend:
    build: ./chatbot-backend
    ports:
      - "8001:8001"
    environment:
      - OPENAI_API_KEY=${OPENAI_API_KEY}
      - CONTENT_API_BASE=http://content-api:8080/api/v1/content
      - TIMESERIES_API_BASE=http://timeseries-api:8080/api/v1/timeseries
    volumes:
      - ./data/embeddings:/app/data/embeddings
    depends_on:
      - content-api
      - timeseries-api

  webapp:
    build: ./webapp
    ports:
      - "5173:5173"
    environment:
      - VITE_AGENT_WS_URL=ws://localhost:8001/ws
```

### Production Considerations

1. **Scaling**
   - Use Redis for conversation state (multi-instance support)
   - Load balance WebSocket connections
   - Consider serverless for bursty workloads

2. **Monitoring**
   - Track token usage per conversation
   - Monitor API call latencies
   - Alert on error rates
   - User satisfaction metrics

3. **Cost Management**
   - Set daily token budgets
   - Cache frequent queries aggressively
   - Use cheaper models for simple questions (GPT-3.5 vs GPT-4)
   - Implement prompt compression

---

## Cost Estimation

### LLM Usage Costs (OpenAI GPT-4 Turbo)

**Assumptions**:
- 1000 messages per day
- Average 500 tokens per conversation (input + output)
- GPT-4 Turbo: $10/1M input tokens, $30/1M output tokens
- Average split: 300 input, 200 output per message

**Monthly Cost**:
```
Input:  1000 msg/day × 30 days × 300 tokens × $10/1M = $90
Output: 1000 msg/day × 30 days × 200 tokens × $30/1M = $180
Total: ~$270/month
```

**Cost Reduction Strategies**:
- Use GPT-3.5 for simple questions: 10x cheaper
- Cache common answers
- Implement smart routing (simple → 3.5, complex → 4)
- Optimize prompts to reduce tokens

---

## Future Enhancements

### Phase 6+: Advanced Features

1. **Multi-Modal Input**
   - Voice input (speech-to-text)
   - Image upload (e.g., "What's this POI?")

2. **Proactive Suggestions**
   - Analyze current page and suggest questions
   - "You're viewing Accommodations. Want to see nearby restaurants?"

3. **Personalization**
   - Learn user preferences over time
   - Remember previous searches
   - Suggest based on history

4. **Collaborative Features**
   - Share conversations
   - Export analysis reports
   - Scheduled reports ("Send me weekly accommodation statistics")

5. **Advanced Visualizations**
   - Generate custom charts in chat
   - Interactive maps in chat widget
   - Export data as CSV/Excel

---

## Getting Started Checklist

### Backend Setup
- [ ] Create Python virtual environment
- [ ] Install dependencies (LangChain, LangGraph, FastAPI)
- [ ] Set up OpenAI/Anthropic API key
- [ ] Create project structure
- [ ] Implement first tool (get_dataset_entries)
- [ ] Test tool manually
- [ ] Set up ChromaDB
- [ ] Embed initial documentation
- [ ] Create basic LangChain chain
- [ ] Implement WebSocket endpoint
- [ ] Test end-to-end flow

### Frontend Setup
- [ ] Install socket.io-client or WebSocket library
- [ ] Create ChatWidget.vue component
- [ ] Implement useAgentWebSocket composable
- [ ] Add chat widget to App.vue
- [ ] Test WebSocket connection
- [ ] Implement message rendering
- [ ] Add typing indicator
- [ ] Test sending/receiving messages

### Testing
- [ ] Test simple question: "How many accommodations?"
- [ ] Verify API calls are made correctly
- [ ] Test error handling
- [ ] Test navigation command execution
- [ ] Measure response latency
- [ ] Check token usage

### Deployment
- [ ] Create Dockerfile for backend
- [ ] Create docker-compose.yml
- [ ] Test in Docker environment
- [ ] Set up environment variables
- [ ] Deploy to staging
- [ ] Run integration tests
- [ ] Deploy to production

---

## Conclusion

This plan provides a comprehensive roadmap for building an intelligent chatbot agent for the OdhDiscovery webapp. The agent will:

✅ **Understand** user questions using GPT-4/Claude
✅ **Execute** API calls using LangChain tools
✅ **Analyze** data and generate insights
✅ **Navigate** the webapp to show results
✅ **Explain** its reasoning and findings

**Key Success Factors**:
1. Start simple (Phase 1) and iterate
2. Test each component thoroughly
3. Optimize prompts based on real usage
4. Monitor costs and performance
5. Gather user feedback early

**Timeline**: 10 weeks to production-ready chatbot
**Estimated Cost**: ~$270/month for 1000 daily conversations
**Expected Impact**: Dramatically improved user experience and data discoverability

Ready to build this! 🚀
