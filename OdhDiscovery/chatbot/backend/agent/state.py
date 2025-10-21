"""
Agent state definition for LangGraph
"""
from typing import TypedDict, Annotated, Sequence, Any
from langchain_core.messages import BaseMessage
import operator


class AgentState(TypedDict):
    """
    State for the ODH Chatbot agent

    LangGraph uses this state to track conversation and tool execution
    """
    # Conversation messages
    messages: Annotated[Sequence[BaseMessage], operator.add]

    # Current user query
    query: str

    # Tool results from execution
    tool_results: list[dict]

    # Navigation commands to send to frontend
    navigation_commands: Annotated[list[dict], operator.add]

    # Iteration count (to prevent infinite loops)
    iterations: int

    # Whether agent should continue or finish
    should_continue: bool

    # Session-specific data cache (for multi-user isolation)
    session_cache: Any  # DataCache instance from session
