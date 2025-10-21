"""
LangGraph agent workflow
Provider-agnostic implementation supporting multiple LLM providers
"""
import logging
import json
from typing import Literal
from langgraph.graph import StateGraph, END
from langgraph.prebuilt import ToolNode
from langchain_core.messages import HumanMessage, AIMessage, SystemMessage, ToolMessage
from langchain_core.language_models import BaseChatModel
from langchain_openai import ChatOpenAI
from langchain_core.tools import StructuredTool

from agent.state import AgentState
from agent.prompts import SYSTEM_PROMPT
from tools.data_cache import set_session_cache, clear_session_cache
from tools import (
    get_datasets_tool,
    get_dataset_entries_tool,
    count_entries_tool,
    get_entry_by_id_tool,
    inspect_api_structure_tool,
    aggregate_data_tool,
    flatten_data_tool,
    dataframe_query_tool,
    get_types_tool,
    get_sensors_tool,
    get_timeseries_tool,
    get_latest_measurements_tool,
    navigate_to_page_tool,
    search_documentation_tool,
)
from config import settings, get_llm_config

logger = logging.getLogger(__name__)


def create_llm() -> BaseChatModel:
    """
    Create LLM instance based on configured provider
    Provider-agnostic - supports TogetherAI, OpenAI, Anthropic, or custom
    """
    llm_config = get_llm_config()
    provider = settings.llm_provider

    logger.info(f"Creating LLM with provider: {provider}, model: {llm_config['model']}")

    if provider in ["togetherai", "openai", "custom"]:
        # Use ChatOpenAI with custom base_url for TogetherAI or custom providers
        # TogetherAI is OpenAI-compatible
        return ChatOpenAI(
            model=llm_config['model'],
            temperature=llm_config['temperature'],
            max_tokens=llm_config['max_tokens'],
            api_key=llm_config['api_key'],
            base_url=llm_config.get('base_url'),
        )
    elif provider == "anthropic":
        from langchain_anthropic import ChatAnthropic
        return ChatAnthropic(
            model=llm_config['model'],
            temperature=llm_config['temperature'],
            max_tokens=llm_config['max_tokens'],
            api_key=llm_config['api_key'],
        )
    else:
        raise ValueError(f"Unsupported LLM provider: {provider}")


def create_agent_graph():
    """
    Create the LangGraph workflow for the ODH Chatbot agent

    Returns:
        Compiled LangGraph workflow
    """
    # Initialize LLM
    llm = create_llm()

    # Prepare tools
    tools = [
        search_documentation_tool,  # Knowledge base first - agent should check docs first
        inspect_api_structure_tool,  # Structure inspection - use before fetching large data
        flatten_data_tool,  # Flatten nested JSON to tabular format for pandas
        dataframe_query_tool,  # Pandas operations for filter/sort/groupby
        aggregate_data_tool,  # Data aggregation - legacy tool, prefer flatten + dataframe_query
        get_datasets_tool,
        get_dataset_entries_tool,
        count_entries_tool,
        get_entry_by_id_tool,
        get_types_tool,
        get_sensors_tool,
        get_timeseries_tool,
        get_latest_measurements_tool,
        navigate_to_page_tool,
    ]

    # Convert SmartTools to LangChain-compatible format
    langchain_tools = []
    for tool in tools:
        langchain_tools.append(
            StructuredTool.from_function(
                coroutine=tool.execute,
                name=tool.name,
                description=tool.description,
                return_direct=tool.return_direct
            )
        )

    # Bind tools to LLM
    llm_with_tools = llm.bind_tools(langchain_tools)

    # Define graph nodes

    async def call_model(state: AgentState) -> AgentState:
        """Call LLM with tools"""
        iteration = state.get('iterations', 0) + 1
        logger.info(f"{'='*60}")
        logger.info(f"🤖 AGENT ITERATION {iteration}")
        logger.info(f"{'='*60}")

        messages = list(state.get("messages", []))
        new_messages = []

        # Add system message if not present
        if not messages or not isinstance(messages[0], SystemMessage):
            new_messages.insert(0, SystemMessage(content=SYSTEM_PROMPT))

        # Add user query if not in messages
        if state.get('query') and not any(isinstance(m, HumanMessage) and m.content == state['query'] for m in messages):
            new_messages.append(HumanMessage(content=state['query']))
            logger.info(f"📥 USER QUERY: {state['query']}")

        try:
            logger.info(f"🔮 Calling LLM with {len(messages+new_messages)} messages...")
            response = await llm_with_tools.ainvoke(messages+new_messages)
            new_messages.append(response)

            # Log what the agent decided to do
            if hasattr(response, 'tool_calls') and response.tool_calls:
                logger.info(f"🔧 AGENT DECISION: Call {len(response.tool_calls)} tool(s)")
                for i, tool_call in enumerate(response.tool_calls, 1):
                    tool_name = tool_call.get('name', 'unknown')
                    tool_args = tool_call.get('args', {})
                    # Truncate large args
                    args_str = str(tool_args)
                    if len(args_str) > 200:
                        args_str = args_str[:200] + "..."
                    logger.info(f"   {i}. {tool_name}({args_str})")
            else:
                logger.info(f"💬 AGENT DECISION: Respond to user (no tool calls)")

            return {
                **state,
                'messages': new_messages,
                'iterations': iteration
            }
        except Exception as e:
            logger.error(f"❌ LLM call failed: {e}", exc_info=True)
            error_msg = AIMessage(content=f"I encountered an error: {str(e)}")
            new_messages.append(error_msg)
            return {
                **state,
                'messages': new_messages,
                'should_continue': False
            }

    async def execute_tools(state: AgentState) -> AgentState:
        """Execute tools requested by LLM"""
        logger.info(f"{'─'*60}")
        logger.info(f"⚙️  EXECUTING TOOLS")
        logger.info(f"{'─'*60}")

        messages = list(state['messages'])
        last_message = messages[-1]
        message = None

        tool_results = []
        navigation_commands = []

        # Set session-specific cache for multi-user isolation
        session_cache = state.get('session_cache')
        if session_cache:
            set_session_cache(session_cache)
            logger.debug("Session cache context set for tool execution")

        try:
            # Check if last message has tool calls
            if hasattr(last_message, 'tool_calls') and last_message.tool_calls:
                for idx, tool_call in enumerate(last_message.tool_calls, 1):
                    tool_name = tool_call['name']
                    tool_args = tool_call['args']

                    logger.info(f"▶️  Tool {idx}/{len(last_message.tool_calls)}: {tool_name}")

                    # Log args (truncated if large)
                    args_str = str(tool_args)
                    if len(args_str) > 300:
                        logger.info(f"   Args: {args_str[:300]}...")
                    else:
                        logger.info(f"   Args: {args_str}")

                    # Find and execute the tool
                    tool_result = None
                    tool_found = False
                    for tool in tools:
                        if tool.name == tool_name:
                            tool_found = True
                            try:
                                result = await tool.execute(**tool_args)
                                tool_result = result

                                # Log result size
                                result_str = json.dumps(result)
                                result_size = len(result_str)
                                logger.info(f"   ✅ Result: {result_size} chars")

                                # Show result preview
                                if result_size > 500:
                                    logger.info(f"   Preview: {result_str[:500]}...")
                                else:
                                    logger.info(f"   Result: {result_str}")

                                # Check if it's a navigation command
                                if isinstance(result, dict) and result.get('result', {}).get('type') == 'navigate':
                                    navigation_commands.append(result['result'])

                                tool_results.append(result)

                                # Add tool result as message
                                message = ToolMessage(
                                        content=json.dumps(result),
                                        tool_call_id=tool_call['id']
                                    )
                            except Exception as e:
                                logger.error(f"   ❌ Tool execution failed: {e}", exc_info=True)
                                error_result = {"error": str(e), "tool": tool_name}
                                message = ToolMessage(
                                        content=json.dumps(error_result),
                                        tool_call_id=tool_call['id']
                                    )
                            break

                    if not tool_found:
                        logger.error(f"   ❌ Tool '{tool_name}' not found!")
        finally:
            # Always clear session cache context after tool execution
            clear_session_cache()

        logger.info(f"{'─'*60}")
        return {
            **state,
            'messages': [message],
            'tool_results': tool_results,
            'navigation_commands': navigation_commands
        }

    def should_continue(state: AgentState) -> Literal["tools", "end"]:
        """Determine if agent should continue or end"""
        messages = state['messages']
        last_message = messages[-1]

        # Check iteration limit
        if state.get('iterations', 0) >= 10:
            return "end"

        # If last message has tool calls, continue to tools
        if hasattr(last_message, 'tool_calls') and last_message.tool_calls:
            return "tools"

        return "end"

    # Build graph
    workflow = StateGraph(AgentState)

    # Add nodes
    workflow.add_node("agent", call_model)
    workflow.add_node("tools", execute_tools)

    # Set entry point
    workflow.set_entry_point("agent")

    # Add conditional edges
    workflow.add_conditional_edges(
        "agent",
        should_continue,
        {
            "tools": "tools",
            "end": END
        }
    )

    # After tools, go back to agent
    workflow.add_edge("tools", "agent")

    # Compile and return
    return workflow.compile()


# Global agent instance (created on first use)
_agent_graph = None


def get_agent():
    """Get or create the agent graph instance"""
    global _agent_graph
    if _agent_graph is None:
        _agent_graph = create_agent_graph()
    return _agent_graph
