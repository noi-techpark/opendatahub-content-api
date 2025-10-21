"""
FastAPI WebSocket Server for ODH Chatbot
Main entry point for the backend service
"""
import logging
import json
import uvicorn
from fastapi import FastAPI, WebSocket, WebSocketDisconnect, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from contextlib import asynccontextmanager
from pydantic import BaseModel
from typing import Optional

from config import settings
from agent import get_agent, AgentState
from vector_store import ingest_markdown_files_async

# Configure logging
logging.basicConfig(
    level=getattr(logging, settings.log_level),
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


# Lifespan context manager
@asynccontextmanager
async def lifespan(app: FastAPI):
    """
    Lifespan events for FastAPI application
    """
    # Startup
    logger.info("=" * 60)
    logger.info("ODH Chatbot Backend Starting")
    logger.info("=" * 60)
    logger.info(f"LLM Provider: {settings.llm_provider}")
    logger.info(f"LLM Model: {settings.llm_model}")
    logger.info(f"Content API: {settings.content_api_base_url}")
    logger.info(f"Timeseries API: {settings.timeseries_api_base_url}")
    logger.info(f"ChromaDB: {settings.chroma_host}:{settings.chroma_port}")
    logger.info("=" * 60)

    # Initialize agent (lazy loading)
    try:
        agent = get_agent()
        logger.info("✓ Agent initialized successfully")
    except Exception as e:
        logger.error(f"✗ Failed to initialize agent: {e}", exc_info=True)

    yield

    # Shutdown
    logger.info("ODH Chatbot Backend Shutting Down")


# Create FastAPI app
app = FastAPI(
    title="ODH Chatbot Backend",
    description="WebSocket server for ODH Chatbot with LangGraph agent",
    version="0.1.0",
    lifespan=lifespan
)

# Configure CORS
if settings.enable_cors:
    app.add_middleware(
        CORSMiddleware,
        allow_origins=["*"],  # Configure appropriately for production
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )


# Request/Response models
class QueryRequest(BaseModel):
    """Request model for direct query endpoint"""
    query: str
    include_debug: bool = False


class QueryResponse(BaseModel):
    """Response model for direct query endpoint"""
    response: str
    navigation_commands: list = []
    iterations: int = 0
    tool_calls: list = []
    debug_info: Optional[dict] = None


# Health check endpoint
@app.get("/health")
async def health_check():
    """Health check endpoint"""
    return {
        "status": "healthy",
        "provider": settings.llm_provider,
        "model": settings.llm_model
    }


# Documentation ingestion endpoint
@app.post("/ingest-docs")
async def ingest_docs(
    docs_dir: str = "/docs",
    clear_existing: bool = False
):
    """
    Ingest markdown documentation into vector store

    Args:
        docs_dir: Directory containing markdown files
        clear_existing: Whether to clear existing documents first
    """
    try:
        logger.info(f"Starting documentation ingestion from: {docs_dir}")
        result = await ingest_markdown_files_async(
            docs_dir=docs_dir,
            clear_existing=clear_existing
        )
        return result
    except Exception as e:
        logger.error(f"Documentation ingestion failed: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))


# Direct query endpoint (for testing)
@app.post("/query", response_model=QueryResponse)
async def query_endpoint(request: QueryRequest):
    """
    Direct query endpoint for testing

    Executes agent and returns response with debug information
    """
    try:
        logger.info(f"Processing query: {request.query}")

        # Get agent
        agent = get_agent()

        # Create initial state
        initial_state: AgentState = {
            "messages": [],
            "query": request.query,
            "tool_results": [],
            "navigation_commands": [],
            "iterations": 0,
            "should_continue": True
        }

        # Execute agent
        result = await agent.ainvoke(initial_state)

        # Extract response
        messages = result.get("messages", [])
        final_message = messages[-1] if messages else None

        # Extract tool calls for debugging
        tool_calls = []
        for msg in messages:
            if hasattr(msg, 'tool_calls') and msg.tool_calls:
                for tc in msg.tool_calls:
                    tool_calls.append({
                        "name": tc.get('name'),
                        "args": tc.get('args', {})
                    })

        # Build response
        response = QueryResponse(
            response=final_message.content if final_message and hasattr(final_message, 'content') else str(final_message),
            navigation_commands=result.get("navigation_commands", []),
            iterations=result.get("iterations", 0),
            tool_calls=tool_calls,
            debug_info={
                "message_count": len(messages),
                "tool_result_count": len(result.get("tool_results", []))
            } if request.include_debug else None
        )

        logger.info(f"Query complete: {response.iterations} iterations, {len(tool_calls)} tool calls")
        return response

    except Exception as e:
        logger.error(f"Query execution failed: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))


# WebSocket endpoint for chat
@app.websocket("/ws")
async def websocket_endpoint(websocket: WebSocket):
    """
    WebSocket endpoint for chatbot interaction

    Message format (from client):
    {
        "type": "query",
        "content": "user question here"
    }

    Response format (to client):
    {
        "type": "message" | "navigation" | "error",
        "content": "...",
        "data": {...}  # For navigation commands
    }
    """
    await websocket.accept()
    logger.info("WebSocket connection established")

    try:
        while True:
            # Receive message from client
            data = await websocket.receive_text()
            logger.info(f"Received message: {data[:100]}...")

            try:
                message = json.loads(data)
            except json.JSONDecodeError:
                await websocket.send_json({
                    "type": "error",
                    "content": "Invalid JSON format"
                })
                continue

            # Handle different message types
            if message.get("type") == "query":
                query = message.get("content", "")

                if not query:
                    await websocket.send_json({
                        "type": "error",
                        "content": "Empty query"
                    })
                    continue

                # Send acknowledgment
                await websocket.send_json({
                    "type": "status",
                    "content": "Processing your question..."
                })

                try:
                    # Get agent
                    agent = get_agent()

                    # Create initial state
                    initial_state: AgentState = {
                        "messages": [],
                        "query": query,
                        "tool_results": [],
                        "navigation_commands": [],
                        "iterations": 0,
                        "should_continue": True
                    }

                    # Execute agent
                    logger.info(f"Executing agent for query: {query}")
                    result = await agent.ainvoke(initial_state)

                    # Extract response
                    messages = result.get("messages", [])
                    final_message = messages[-1] if messages else None

                    # Send agent response
                    if final_message:
                        await websocket.send_json({
                            "type": "message",
                            "content": final_message.content if hasattr(final_message, 'content') else str(final_message)
                        })

                    # Send navigation commands
                    navigation_commands = result.get("navigation_commands", [])
                    for nav_cmd in navigation_commands:
                        await websocket.send_json({
                            "type": "navigation",
                            "data": nav_cmd
                        })

                    # Send completion status
                    await websocket.send_json({
                        "type": "status",
                        "content": "Done"
                    })

                except Exception as e:
                    logger.error(f"Agent execution failed: {e}", exc_info=True)
                    await websocket.send_json({
                        "type": "error",
                        "content": f"An error occurred: {str(e)}"
                    })

            elif message.get("type") == "ping":
                # Handle ping for keepalive
                await websocket.send_json({
                    "type": "pong"
                })

            else:
                await websocket.send_json({
                    "type": "error",
                    "content": f"Unknown message type: {message.get('type')}"
                })

    except WebSocketDisconnect:
        logger.info("WebSocket connection closed")
    except Exception as e:
        logger.error(f"WebSocket error: {e}", exc_info=True)
        try:
            await websocket.send_json({
                "type": "error",
                "content": "Internal server error"
            })
        except Exception:
            pass


# Main entry point
if __name__ == "__main__":
    uvicorn.run(
        "main:app",
        host=settings.backend_host,
        port=settings.backend_port,
        reload=True,  # Enable auto-reload for development
        log_level=settings.log_level.lower()
    )
