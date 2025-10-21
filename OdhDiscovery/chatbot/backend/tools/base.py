"""
SmartTool base class
Tools with built-in preprocessing to manage token usage
"""
import json
import logging
from typing import Any, Callable, Awaitable
from preprocessing.utils import count_tokens, truncate_to_token_limit
from preprocessing.strategies import emergency_summarize
from config import settings

logger = logging.getLogger(__name__)


class SmartTool:
    """
    Base class for agent tools with automatic preprocessing
    Ensures tool responses stay within token limits
    """

    def __init__(
        self,
        name: str,
        description: str,
        func: Callable[..., Awaitable[Any]],
        preprocessor: Callable[[Any], dict] | None = None,
        max_tokens: int | None = None,
        return_direct: bool = False
    ):
        """
        Initialize SmartTool

        Args:
            name: Tool name (used by agent)
            description: Tool description for LLM
            func: Async function to execute
            preprocessor: Optional preprocessing function to apply to results
            max_tokens: Maximum tokens for response (default from settings)
            return_direct: Whether to return result directly to user
        """
        self.name = name
        self.description = description
        self.func = func
        self.preprocessor = preprocessor
        self.max_tokens = max_tokens or settings.max_tokens_per_tool
        self.return_direct = return_direct

    async def execute(self, **kwargs) -> dict[str, Any]:
        """
        Execute the tool with preprocessing

        Args:
            **kwargs: Tool-specific arguments

        Returns:
            Preprocessed tool result
        """
        logger.info(f"Executing tool: {self.name} with args: {kwargs}")

        try:
            # 1. Execute the underlying function
            raw_result = await self.func(**kwargs)

            # 2. Apply custom preprocessor if provided
            if self.preprocessor:
                logger.debug(f"Applying preprocessor for {self.name}")
                processed_result = self.preprocessor(raw_result)
            else:
                processed_result = raw_result

            # 3. Check token count
            result_json = json.dumps(processed_result, indent=2)
            token_count = count_tokens(result_json)

            logger.debug(f"Tool {self.name} result: {token_count} tokens")

            # 4. Apply emergency measures if still too large
            if token_count > self.max_tokens:
                logger.warning(
                    f"Tool {self.name} result exceeds token limit "
                    f"({token_count} > {self.max_tokens}), applying emergency measures"
                )

                # Try truncation first
                truncated = truncate_to_token_limit(
                    processed_result,
                    max_tokens=self.max_tokens
                )

                if truncated.get('emergency'):
                    # If still too large, apply emergency summarization
                    processed_result = emergency_summarize(processed_result)
                else:
                    processed_result = truncated['data']

            return {
                'tool': self.name,
                'success': True,
                'result': processed_result,
                'tokens': count_tokens(json.dumps(processed_result)),
            }

        except Exception as e:
            logger.error(f"Tool {self.name} execution failed: {e}", exc_info=True)
            return {
                'tool': self.name,
                'success': False,
                'error': str(e),
                'error_type': type(e).__name__
            }

    def to_langchain_tool(self):
        """
        Convert to LangChain tool format

        Returns:
            Dictionary compatible with LangChain tool creation
        """
        return {
            'name': self.name,
            'description': self.description,
            'func': self.execute,
            'return_direct': self.return_direct
        }
