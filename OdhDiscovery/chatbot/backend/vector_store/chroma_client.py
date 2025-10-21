"""
ChromaDB client for vector storage and retrieval
"""
import logging
import chromadb
from chromadb.config import Settings
from langchain_chroma import Chroma
from langchain_community.embeddings import HuggingFaceEmbeddings
from config import settings, get_chroma_url

logger = logging.getLogger(__name__)


def get_vector_store() -> Chroma:
    """
    Get or create ChromaDB vector store

    Returns:
        LangChain Chroma vector store instance
    """
    logger.info(f"Connecting to ChromaDB at {get_chroma_url()}")

    # Use HuggingFace embeddings (free, no API key required)
    embeddings = HuggingFaceEmbeddings(
        model_name="sentence-transformers/all-MiniLM-L6-v2",
        model_kwargs={'device': 'cpu'},
        encode_kwargs={'normalize_embeddings': True}
    )

    # Create ChromaDB client
    client = chromadb.HttpClient(
        host=settings.chroma_host,
        port=settings.chroma_port,
        settings=Settings(
            anonymized_telemetry=False,
            allow_reset=True
        )
    )

    # Create LangChain vector store
    vector_store = Chroma(
        client=client,
        collection_name=settings.chroma_collection,
        embedding_function=embeddings,
    )

    logger.info(f"Vector store connected, collection: {settings.chroma_collection}")
    return vector_store


async def search_docs(
    query: str,
    k: int = 5,
    filter_dict: dict | None = None
) -> list[dict]:
    """
    Search documentation in vector store

    Args:
        query: Search query
        k: Number of results to return
        filter_dict: Optional metadata filters

    Returns:
        List of matching documents with metadata
    """
    logger.info(f"Searching docs for: {query}")

    try:
        vector_store = get_vector_store()

        # Perform similarity search
        if filter_dict:
            results = vector_store.similarity_search(
                query,
                k=k,
                filter=filter_dict
            )
        else:
            results = vector_store.similarity_search(query, k=k)

        # Convert to dict format
        docs = []
        for doc in results:
            docs.append({
                'content': doc.page_content,
                'metadata': doc.metadata,
            })

        logger.info(f"Found {len(docs)} matching documents")
        return docs

    except Exception as e:
        logger.error(f"Document search failed: {e}", exc_info=True)
        return []


def get_chroma_client() -> chromadb.HttpClient:
    """
    Get raw ChromaDB client for admin operations

    Returns:
        ChromaDB HTTP client
    """
    return chromadb.HttpClient(
        host=settings.chroma_host,
        port=settings.chroma_port,
        settings=Settings(
            anonymized_telemetry=False,
            allow_reset=True
        )
    )
