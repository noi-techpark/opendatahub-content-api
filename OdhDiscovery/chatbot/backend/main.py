"""
FastAPI WebSocket Server for ODH Chatbot
Main entry point for the backend service
"""
import logging
import json
import uvicorn
import asyncio
from datetime import timedelta
from fastapi import FastAPI, WebSocket, WebSocketDisconnect, HTTPException, Depends, Query
from fastapi.middleware.cors import CORSMiddleware
from fastapi.security import OAuth2PasswordRequestForm
from contextlib import asynccontextmanager
from pydantic import BaseModel
from typing import Optional
from langchain_core.messages import HumanMessage, AIMessage, SystemMessage
from agent.prompts import SYSTEM_PROMPT

from config import settings
from agent import get_agent, AgentState
from vector_store import ingest_markdown_files_async
from conversation_memory import get_memory_store
from auth import (
    Token, User, authenticate_user, create_access_token,
    get_current_user, verify_token
)

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
    session_id: Optional[str] = None
    include_debug: bool = False


class QueryResponse(BaseModel):
    """Response model for direct query endpoint"""
    response: str
    session_id: str
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


# Authentication endpoints
@app.post("/auth/login", response_model=Token)
async def login(form_data: OAuth2PasswordRequestForm = Depends()):
    """
    Login endpoint - returns JWT token

    Accepts form data with username and password (OAuth2 standard format)
    """
    user = authenticate_user(form_data.username, form_data.password)
    if not user:
        raise HTTPException(
            status_code=401,
            detail="Incorrect username or password",
            headers={"WWW-Authenticate": "Bearer"},
        )

    access_token_expires = timedelta(minutes=settings.jwt_expire_minutes)
    access_token = create_access_token(
        data={"sub": user.username}, expires_delta=access_token_expires
    )

    return Token(
        access_token=access_token,
        token_type="bearer",
        expires_in=settings.jwt_expire_minutes * 60
    )


@app.get("/auth/me", response_model=User)
async def get_me(current_user: User = Depends(get_current_user)):
    """Get current authenticated user info"""
    return current_user


@app.post("/auth/verify")
async def verify_auth(current_user: User = Depends(get_current_user)):
    """Verify if token is valid"""
    return {"valid": True, "username": current_user.username}


# Documentation ingestion endpoint
@app.post("/ingest-docs")
async def ingest_docs(
    docs_dir: str = "/docs",
    clear_existing: bool = False,
    current_user: User = Depends(get_current_user)
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
async def query_endpoint(
    request: QueryRequest,
    current_user: User = Depends(get_current_user)
):
    """
    Direct query endpoint for testing

    Executes agent and returns response with debug information
    Supports conversation memory via session_id
    """
    try:
        logger.info(f"Processing query: {request.query}")

        # Get or create session
        memory_store = get_memory_store()
        session_id, session = memory_store.get_or_create_session(request.session_id)
        logger.info(f"Using session: {session_id} (messages: {len(session.messages)})")

        # Get agent
        agent = get_agent()

        # Get conversation history
        conversation_messages = session.get_messages()

        # Add new user message
        user_message = HumanMessage(content=request.query)
        conversation_messages.append(user_message)

        # Create initial state with conversation history and session cache
        initial_state: AgentState = {
            "messages": conversation_messages,
            "query": request.query,
            "tool_results": [],
            "navigation_commands": [],
            "iterations": 0,
            "should_continue": True,
            "session_cache": session.cache  # Session-specific cache for multi-user isolation
        }

        # Execute agent
        result = await agent.ainvoke(initial_state)

        # Extract response
        messages = result.get("messages", [])
        final_message = messages[-1] if messages else None
        navigation_commands = result.get("navigation_commands", [])

        # Store updated conversation history
        # Navigation commands are now stored in message metadata
        session.messages = messages

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
            session_id=session_id,
            navigation_commands=navigation_commands,
            iterations=result.get("iterations", 0),
            tool_calls=tool_calls,
            debug_info={
                "message_count": len(messages),
                "tool_result_count": len(result.get("tool_results", [])),
                "conversation_length": len(session.messages)
            } if request.include_debug else None
        )

        logger.info(f"Query complete: {response.iterations} iterations, {len(tool_calls)} tool calls")
        return response

    except Exception as e:
        logger.error(f"Query execution failed: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))


# WebSocket endpoint for chat with streaming
@app.websocket("/ws")
async def websocket_endpoint(
    websocket: WebSocket,
    token: Optional[str] = Query(None)
):
    """
    WebSocket endpoint for chatbot interaction with streaming responses

    Authentication: Pass JWT token as query parameter: /ws?token=<jwt_token>

    Message format (from client):
    {
        "type": "query",
        "content": "user question here",
        "session_id": "optional-session-id"
    }

    OR

    {
        "type": "clear_history",
        "session_id": "session-id"
    }

    Response format (to client):
    {
        "type": "message" | "chunk" | "navigation" | "error" | "status" | "session",
        "content": "...",
        "data": {...}
    }

    Streaming chunks:
    {
        "type": "chunk",
        "content": "partial text...",
        "done": false
    }
    """
    # Validate token before accepting connection
    if not token:
        await websocket.close(code=4001, reason="Authentication required")
        return

    token_data = verify_token(token)
    if not token_data:
        await websocket.close(code=4001, reason="Invalid or expired token")
        return

    await websocket.accept()
    logger.info(f"WebSocket connection established for user: {token_data.username}")

    # Track session for this WebSocket connection
    current_session_id: Optional[str] = None
    memory_store = get_memory_store()

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
                requested_session_id = message.get("session_id")

                if not query:
                    await websocket.send_json({
                        "type": "error",
                        "content": "Empty query"
                    })
                    continue

                try:
                    # Get or create session
                    session_id, session = memory_store.get_or_create_session(
                        requested_session_id or current_session_id
                    )
                    current_session_id = session_id

                    # Send session ID to client
                    await websocket.send_json({
                        "type": "session",
                        "session_id": session_id,
                        "message_count": len(session.messages)
                    })

                    logger.info(f"Processing query for session {session_id} (history: {len(session.messages)} messages)")

                    # Get conversation history
                    conversation_messages = session.get_messages()

                     # Add system message if not present
                    if not conversation_messages or len(conversation_messages) == 0:
                        conversation_messages.append(SystemMessage(content=SYSTEM_PROMPT))
                        logger.info(f"📥 ADEDD SYSTEM PROMPT")

                    # Add new user message
                    conversation_messages.append(HumanMessage(content=query))
                    logger.info(f"📥 USER QUERY: {query}")

                    # Send acknowledgment
                    await websocket.send_json({
                        "type": "status",
                        "content": "Processing your question..."
                    })

                    # Get agent
                    agent = get_agent()

                    # Create initial state with conversation history and session cache
                    initial_state: AgentState = {
                        "messages": conversation_messages,
                        "query": query,
                        "tool_results": [],
                        "navigation_commands": [],
                        "iterations": 0,
                        "should_continue": True,
                        "session_cache": session.cache  # Session-specific cache for multi-user isolation
                    }

                    # Execute agent
                    logger.info(f"Executing agent for query: {query}")
                    result = await agent.ainvoke(initial_state)

                    # Extract response
                    messages = result.get("messages", [])
                    final_message = messages[-1] if messages else None
                    navigation_commands = result.get("navigation_commands", [])

                    # Store updated conversation history
                    # Navigation commands are now stored in message metadata
                    session.messages = messages

                    # Stream the response
                    if final_message:
                        response_text = final_message.content if hasattr(final_message, 'content') else str(final_message)

                        # Stream response in chunks (simulating typing effect)
                        words = response_text.split(" ")
                        chunk_size = 3  # words per chunk

                        for i in range(0, len(words), chunk_size):
                            chunk = " ".join(words[i:i+chunk_size])
                            if i + chunk_size < len(words):
                                chunk += " "

                            await websocket.send_json({
                                "type": "chunk",
                                "content": chunk,
                                "done": False
                            })
                            # Small delay for streaming effect
                            await asyncio.sleep(0.05)

                        # Send final chunk marker
                        await websocket.send_json({
                            "type": "chunk",
                            "content": "",
                            "done": True
                        })

                    # Send navigation commands
                    logger.info(f"dispatching navigation commands: len = {len(navigation_commands)}")
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

            elif message.get("type") == "clear_history":
                # Clear conversation history for session
                session_id = message.get("session_id") or current_session_id

                if session_id:
                    success = memory_store.clear_session(session_id)
                    await websocket.send_json({
                        "type": "status",
                        "content": "Conversation history cleared" if success else "Session not found"
                    })
                else:
                    await websocket.send_json({
                        "type": "error",
                        "content": "No active session to clear"
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
        logger.info(f"WebSocket connection closed (session: {current_session_id})")
    except Exception as e:
        logger.error(f"WebSocket error: {e}", exc_info=True)
        try:
            await websocket.send_json({
                "type": "error",
                "content": "Internal server error"
            })
        except Exception:
            pass


# Session management endpoints
@app.get("/sessions/{session_id}")
async def get_session_info(
    session_id: str,
    current_user: User = Depends(get_current_user)
):
    """Get information about a specific session"""
    memory_store = get_memory_store()
    session_info = memory_store.get_session_info(session_id)

    if not session_info:
        raise HTTPException(status_code=404, detail="Session not found")

    return session_info


@app.delete("/sessions/{session_id}")
async def delete_session(
    session_id: str,
    current_user: User = Depends(get_current_user)
):
    """Delete a session and its conversation history"""
    memory_store = get_memory_store()
    success = memory_store.delete_session(session_id)

    if not success:
        raise HTTPException(status_code=404, detail="Session not found")

    return {"message": "Session deleted", "session_id": session_id}


@app.post("/sessions/{session_id}/clear")
async def clear_session_history(
    session_id: str,
    current_user: User = Depends(get_current_user)
):
    """Clear conversation history for a session (keeps session alive)"""
    memory_store = get_memory_store()
    success = memory_store.clear_session(session_id)

    if not success:
        raise HTTPException(status_code=404, detail="Session not found")

    return {"message": "Session history cleared", "session_id": session_id}


@app.get("/sessions")
async def list_sessions(current_user: User = Depends(get_current_user)):
    """Get statistics about active sessions"""
    memory_store = get_memory_store()
    memory_store.cleanup_expired_sessions()

    return {
        "active_sessions": memory_store.get_active_session_count(),
        "max_age_hours": memory_store._max_age_hours
    }


@app.get("/sessions/{session_id}/messages")
async def get_session_messages(
    session_id: str,
    current_user: User = Depends(get_current_user)
):
    """
    Get conversation history for a session

    Returns messages in format suitable for re-populating chat UI
    Only returns user questions and final assistant responses (no system prompts or tool calls)
    """
    memory_store = get_memory_store()
    session = memory_store.get_session(session_id)

    if not session:
        raise HTTPException(status_code=404, detail="Session not found")

    # Convert LangChain messages to simple format for frontend
    # Filter out system messages and tool messages, keep only user/assistant conversation
    # Extract navigation commands from message metadata
    messages = []

    for msg in session.messages:
        msg_class = msg.__class__.__name__

        # Skip system messages (usually the initial prompt)
        if msg_class == "SystemMessage":
            continue

        # Skip tool messages (intermediate results)
        if msg_class == "ToolMessage":
            continue

        # Skip empty assistant messages
        if msg_class == "AIMessage" and (not hasattr(msg, 'content') or not msg.content or msg.content.strip() == ""):
            continue

        # Include user and assistant messages with content
        if msg_class == "HumanMessage":
            messages.append({
                "role": "user",
                "content": msg.content if hasattr(msg, 'content') else str(msg),
                "timestamp": None
            })
        elif msg_class == "AIMessage" and hasattr(msg, 'content') and msg.content:
            # Skip messages that are just tool call instructions (have tool_calls but no meaningful content)
            if hasattr(msg, 'tool_calls') and msg.tool_calls and not msg.content.strip():
                continue

            # Extract navigation commands from message metadata
            nav_commands = []
            if hasattr(msg, 'additional_kwargs') and 'navigation_commands' in msg.additional_kwargs:
                nav_commands = msg.additional_kwargs['navigation_commands']

            messages.append({
                "role": "assistant",
                "content": msg.content,
                "timestamp": None,
                "navigationCommands": nav_commands  # Include navigation commands from message metadata
            })

    return {
        "session_id": session_id,
        "messages": messages,
        "message_count": len(messages),
        "created_at": session.created_at.isoformat(),
        "last_activity": session.last_activity.isoformat()
    }


# Main entry point
if __name__ == "__main__":
    uvicorn.run(
        "main:app",
        host=settings.backend_host,
        port=settings.backend_port,
        reload=False,  
        log_level=settings.log_level.lower()
    )
