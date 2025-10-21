"""
Knowledge Base Tools
Tools for searching documentation in ChromaDB vector store
"""
import logging
from tools.base import SmartTool
from vector_store import search_docs

logger = logging.getLogger(__name__)


async def _search_documentation(
    query: str,
    max_results: int = 3,
    **kwargs
) -> dict:
    """
    Search documentation in the knowledge base

    Args:
        query: Search query
        max_results: Maximum number of results to return

    Returns:
        Dictionary with search results
    """
    logger.info(f"Searching documentation for: {query}")

    results = await search_docs(query=query, k=max_results)

    if not results:
        return {
            'found': False,
            'message': 'No relevant documentation found',
            'query': query
        }

    # Format results for LLM consumption
    formatted_results = []
    for result in results:
        formatted_results.append({
            'content': result['content'],
            'source': result['metadata'].get('source', 'unknown'),
            'filename': result['metadata'].get('filename', 'unknown')
        })

    return {
        'found': True,
        'query': query,
        'results': formatted_results,
        'count': len(formatted_results)
    }


search_documentation_tool = SmartTool(
    name="search_documentation",
    description="""Search the ODH documentation knowledge base for information.
    Use this tool when you need contextual information about:
    - How the ODH APIs work
    - What datasets and timeseries types are available
    - Data structures and field meanings
    - Best practices for querying data
    - API endpoints and parameters

    Parameters:
    - query (required): Search query describing what information you need
    - max_results: Maximum number of documentation chunks to return (default: 3)

    Examples:
    - "How do I filter datasets?"
    - "What timeseries types are available?"
    - "Explain the relationship between datasets and timeseries"
    - "What fields are available in accommodation dataset?"

    Returns documentation excerpts that are relevant to your query.""",
    func=_search_documentation,
    max_tokens=1500
)
