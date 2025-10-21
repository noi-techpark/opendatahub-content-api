"""
Documentation ingestion for vector store
Loads markdown files and stores them in ChromaDB
"""
import os
import logging
from pathlib import Path
from langchain_text_splitters import MarkdownHeaderTextSplitter, RecursiveCharacterTextSplitter
from langchain_core.documents import Document
from vector_store.chroma_client import get_vector_store

logger = logging.getLogger(__name__)


def ingest_markdown_files(
    docs_dir: str | Path,
    chunk_size: int = 1000,
    chunk_overlap: int = 200,
    clear_existing: bool = False
) -> dict:
    """
    Ingest markdown files from a directory into vector store

    Args:
        docs_dir: Directory containing markdown files
        chunk_size: Size of text chunks
        chunk_overlap: Overlap between chunks
        clear_existing: Whether to clear existing documents first

    Returns:
        Dictionary with ingestion statistics
    """
    docs_dir = Path(docs_dir)
    logger.info(f"Ingesting markdown files from: {docs_dir}")

    if not docs_dir.exists():
        logger.error(f"Directory does not exist: {docs_dir}")
        return {'error': 'Directory not found', 'files_processed': 0}

    # Get vector store
    vector_store = get_vector_store()

    # Clear existing documents if requested
    if clear_existing:
        logger.warning("Clearing existing documents from vector store")
        try:
            # Delete and recreate collection
            from vector_store.chroma_client import get_chroma_client
            client = get_chroma_client()
            from config import settings
            try:
                client.delete_collection(settings.chroma_collection)
                logger.info("Existing collection deleted")
            except Exception:
                pass
            # Recreate vector store
            vector_store = get_vector_store()
        except Exception as e:
            logger.error(f"Failed to clear collection: {e}")

    # Markdown splitter with header-aware chunking
    headers_to_split_on = [
        ("#", "Header 1"),
        ("##", "Header 2"),
        ("###", "Header 3"),
    ]
    markdown_splitter = MarkdownHeaderTextSplitter(
        headers_to_split_on=headers_to_split_on,
        strip_headers=False
    )

    # Text splitter for further chunking
    text_splitter = RecursiveCharacterTextSplitter(
        chunk_size=chunk_size,
        chunk_overlap=chunk_overlap,
        separators=["\n\n", "\n", " ", ""]
    )

    # Process all markdown files
    all_documents = []
    files_processed = 0
    files_failed = 0

    for md_file in docs_dir.rglob("*.md"):
        try:
            logger.info(f"Processing: {md_file}")

            # Read file
            with open(md_file, 'r', encoding='utf-8') as f:
                content = f.read()

            # Split by headers first
            header_splits = markdown_splitter.split_text(content)

            # Further split large chunks
            documents = []
            for split in header_splits:
                # Add metadata
                metadata = {
                    'source': str(md_file),
                    'filename': md_file.name,
                    'type': 'documentation',
                }

                # Add header information from split metadata
                if hasattr(split, 'metadata'):
                    metadata.update(split.metadata)

                # Create document
                doc_content = split.page_content if hasattr(split, 'page_content') else str(split)

                # Further split if too large
                if len(doc_content) > chunk_size:
                    sub_docs = text_splitter.create_documents(
                        texts=[doc_content],
                        metadatas=[metadata]
                    )
                    documents.extend(sub_docs)
                else:
                    documents.append(Document(
                        page_content=doc_content,
                        metadata=metadata
                    ))

            # Add to vector store
            if documents:
                vector_store.add_documents(documents)
                all_documents.extend(documents)
                logger.info(f"Added {len(documents)} chunks from {md_file.name}")

            files_processed += 1

        except Exception as e:
            logger.error(f"Failed to process {md_file}: {e}", exc_info=True)
            files_failed += 1

    logger.info(
        f"Ingestion complete: {files_processed} files, "
        f"{len(all_documents)} chunks, {files_failed} failures"
    )

    return {
        'files_processed': files_processed,
        'files_failed': files_failed,
        'total_chunks': len(all_documents),
        'success': files_failed == 0
    }


async def ingest_markdown_files_async(
    docs_dir: str | Path,
    chunk_size: int = 1000,
    chunk_overlap: int = 200,
    clear_existing: bool = False
) -> dict:
    """
    Async wrapper for markdown ingestion

    Args:
        docs_dir: Directory containing markdown files
        chunk_size: Size of text chunks
        chunk_overlap: Overlap between chunks
        clear_existing: Whether to clear existing documents first

    Returns:
        Dictionary with ingestion statistics
    """
    # Run synchronous ingestion in thread pool
    import asyncio
    return await asyncio.to_thread(
        ingest_markdown_files,
        docs_dir,
        chunk_size,
        chunk_overlap,
        clear_existing
    )
