"""
Vector store for document retrieval
Uses ChromaDB for storing and querying documentation
"""
from .chroma_client import get_vector_store, search_docs
from .ingestion import ingest_markdown_files, ingest_markdown_files_async

__all__ = ["get_vector_store", "search_docs", "ingest_markdown_files", "ingest_markdown_files_async"]
