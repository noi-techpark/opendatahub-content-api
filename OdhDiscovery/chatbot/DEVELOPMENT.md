# Development Guide

Quick reference for developers working on the ODH Chatbot.

## Quick Start

```bash
# 1. Setup environment
cp .env.example .env
# Edit .env and add your API key

# 2. Start everything
./scripts/setup.sh

# Or manually:
docker-compose up -d
docker-compose exec backend python scripts/ingest_docs.py /docs --clear
```

## Project Structure

```
chatbot/
├── backend/              # Python backend code
│   ├── main.py          # FastAPI server entry point
│   ├── config.py        # Configuration management
│   ├── agent/           # LangGraph agent
│   ├── tools/           # Smart tools
│   ├── clients/         # API clients
│   ├── preprocessing/   # Data processing
│   └── vector_store/    # ChromaDB integration
├── docs/                # Documentation to ingest
├── scripts/             # Utility scripts
└── docker-compose.yml   # Docker orchestration
```

## Common Tasks

### Add a New Tool

1. Create tool in `backend/tools/`:

```python
# backend/tools/my_custom_tool.py
from tools.base import SmartTool
from clients.content_client import ContentAPIClient

content_client = ContentAPIClient()

async def _my_tool_function(**kwargs) -> dict:
    # Your logic here
    result = await content_client.some_method()
    return result

my_custom_tool = SmartTool(
    name="my_tool",
    description="What this tool does and when to use it",
    func=_my_tool_function,
    max_tokens=2000
)
```

2. Register in `backend/tools/__init__.py`:

```python
from .my_custom_tool import my_custom_tool

__all__ = [..., "my_custom_tool"]
```

3. Add to agent in `backend/agent/graph.py`:

```python
from tools import (..., my_custom_tool)

tools = [
    ...,
    my_custom_tool,
]
```

### Change API Client

Modify the client in `backend/clients/`:

```python
# backend/clients/content_client.py
class ContentAPIClient:
    async def get_dataset_entries(self, dataset_name, **kwargs):
        # Change API structure here
        url = f"{self.base_url}/v2/{dataset_name}"  # New version
        # ...
```

The tools will automatically use the updated client.

### Add Custom Preprocessing

Create a strategy in `backend/preprocessing/strategies.py`:

```python
def my_custom_aggregation(data: dict) -> dict:
    """Custom data aggregation logic"""
    # Your logic here
    return aggregated_data
```

Use in tools:

```python
my_tool = SmartTool(
    name="my_tool",
    func=_my_function,
    preprocessor=my_custom_aggregation  # Custom preprocessor
)
```

### Switch LLM Provider

Update `.env` or `docker-compose.yml`:

```env
# TogetherAI
LLM_PROVIDER=togetherai
LLM_MODEL=meta-llama/Meta-Llama-3.1-70B-Instruct-Turbo
TOGETHER_API_KEY=your_key

# OpenAI
LLM_PROVIDER=openai
LLM_MODEL=gpt-4-turbo-preview
LLM_API_KEY=your_openai_key

# Anthropic
LLM_PROVIDER=anthropic
LLM_MODEL=claude-3-5-sonnet-20241022
LLM_API_KEY=your_anthropic_key
```

Restart:
```bash
docker-compose restart backend
```

### Update System Prompt

Edit `backend/agent/prompts.py`:

```python
SYSTEM_PROMPT = """
Your updated instructions here...
"""
```

### Ingest New Documentation

```bash
# Add markdown files to docs/
# Then reingest
docker-compose exec backend python scripts/ingest_docs.py /docs --clear
```

## Testing

### Test WebSocket Locally

Using `wscat`:
```bash
npm install -g wscat
wscat -c ws://localhost:8001/ws

# Send query
> {"type": "query", "content": "How many hotels are there?"}
```

Using Python:
```python
import asyncio
import websockets
import json

async def test():
    async with websockets.connect("ws://localhost:8001/ws") as ws:
        await ws.send(json.dumps({
            "type": "query",
            "content": "Show me active hotels"
        }))

        while True:
            response = await ws.recv()
            print(json.loads(response))

asyncio.run(test())
```

### Test Tools Directly

```python
# backend/test_tools.py
import asyncio
from tools.content_api import get_datasets_tool

async def test():
    result = await get_datasets_tool.execute()
    print(result)

asyncio.run(test())
```

### Test API Clients

```python
# backend/test_clients.py
import asyncio
from clients.content_client import ContentAPIClient

async def test():
    client = ContentAPIClient()
    result = await client.get_datasets()
    print(result)

asyncio.run(test())
```

## Debugging

### View Logs

```bash
# Backend logs
docker-compose logs -f backend

# ChromaDB logs
docker-compose logs -f chromadb

# All logs
docker-compose logs -f
```

### Enable Debug Logging

In `.env`:
```env
LOG_LEVEL=DEBUG
```

### Attach to Backend Container

```bash
docker-compose exec backend bash

# Now you're inside the container
python
>>> from agent import get_agent
>>> agent = get_agent()
```

### Test Preprocessing

```python
from preprocessing.strategies import aggregate_dataset_entries

# Test data
entries = [{"Id": "1", "Type": "Hotel"}, {"Id": "2", "Type": "Hotel"}]
result = aggregate_dataset_entries(entries)
print(result)
```

## Common Issues

### ChromaDB Not Connecting

```bash
# Check ChromaDB is running
curl http://localhost:8000/api/v1/heartbeat

# Restart ChromaDB
docker-compose restart chromadb

# Check logs
docker-compose logs chromadb
```

### Backend Won't Start

```bash
# Check environment variables
docker-compose exec backend env | grep LLM

# Check Python errors
docker-compose logs backend

# Rebuild
docker-compose build backend
docker-compose up -d
```

### API Connection Issues

```bash
# Test from inside container
docker-compose exec backend curl http://host.docker.internal:5000/api/v1/content

# On Linux, use actual IP instead of host.docker.internal
# Or add to docker-compose.yml:
# network_mode: host
```

### Tools Not Preprocessing

Check settings in `.env`:
```env
ENABLE_AUTO_AGGREGATION=true
MAX_TOKENS_PER_TOOL=2000
```

## Performance Optimization

### Reduce Token Usage

1. Increase aggregation:
```env
DEFAULT_PAGE_SIZE=20  # Smaller default
MAX_TOKENS_PER_TOOL=1500  # Stricter limit
```

2. Use field projection in tools:
```python
fields = ["Id", "Shortname", "Active"]  # Only needed fields
```

3. Add custom preprocessors for your data types

### Speed Up Responses

1. Use smaller models:
```env
LLM_MODEL=meta-llama/Meta-Llama-3.1-8B-Instruct-Turbo  # Faster, cheaper
```

2. Reduce max_tokens:
```env
LLM_MAX_TOKENS=2048  # Faster generation
```

3. Parallel tool execution (already implemented in LangGraph)

## Code Style

### Python
- Follow PEP 8
- Use type hints
- Async/await for I/O operations
- Docstrings for all public functions

### Example
```python
async def my_function(
    param1: str,
    param2: int | None = None
) -> dict[str, Any]:
    """
    Brief description.

    Args:
        param1: Description
        param2: Description

    Returns:
        Description
    """
    # Implementation
    pass
```

## Git Workflow

```bash
# Create feature branch
git checkout -b feature/my-feature

# Make changes, commit
git add .
git commit -m "feat: add new tool for X"

# Push and create PR
git push origin feature/my-feature
```

## Deployment

### Production Checklist

- [ ] Update CORS settings in `main.py`
- [ ] Set proper `BACKEND_HOST` and `BACKEND_PORT`
- [ ] Use production-grade secrets management
- [ ] Enable authentication
- [ ] Set up monitoring
- [ ] Configure rate limiting
- [ ] Use environment-specific configs
- [ ] Set up log aggregation

### Environment-Specific Configs

```bash
# .env.production
LOG_LEVEL=WARNING
ENABLE_CORS=false
# ...
```

## Resources

- LangChain Docs: https://python.langchain.com/
- LangGraph Docs: https://langchain-ai.github.io/langgraph/
- FastAPI Docs: https://fastapi.tiangolo.com/
- ChromaDB Docs: https://docs.trychroma.com/
- TogetherAI Docs: https://docs.together.ai/
