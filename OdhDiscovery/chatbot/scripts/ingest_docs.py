#!/usr/bin/env python3
"""
Documentation Ingestion Script
Loads markdown files into ChromaDB vector store

Usage:
    python scripts/ingest_docs.py /path/to/docs
    python scripts/ingest_docs.py /path/to/docs --clear  # Clear existing docs first
"""
import sys
import argparse
import logging
from pathlib import Path

# Add parent directory to path
sys.path.insert(0, str(Path(__file__).parent.parent))

from vector_store.ingestion import ingest_markdown_files

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


def main():
    """Main ingestion function"""
    parser = argparse.ArgumentParser(
        description='Ingest markdown documentation into ChromaDB vector store'
    )
    parser.add_argument(
        'docs_dir',
        type=str,
        help='Directory containing markdown files'
    )
    parser.add_argument(
        '--clear',
        action='store_true',
        help='Clear existing documents before ingestion'
    )
    parser.add_argument(
        '--chunk-size',
        type=int,
        default=1000,
        help='Size of text chunks (default: 1000)'
    )
    parser.add_argument(
        '--chunk-overlap',
        type=int,
        default=200,
        help='Overlap between chunks (default: 200)'
    )

    args = parser.parse_args()

    logger.info("=" * 60)
    logger.info("ODH Chatbot Documentation Ingestion")
    logger.info("=" * 60)
    logger.info(f"Docs directory: {args.docs_dir}")
    logger.info(f"Clear existing: {args.clear}")
    logger.info(f"Chunk size: {args.chunk_size}")
    logger.info(f"Chunk overlap: {args.chunk_overlap}")
    logger.info("=" * 60)

    # Run ingestion
    result = ingest_markdown_files(
        docs_dir=args.docs_dir,
        chunk_size=args.chunk_size,
        chunk_overlap=args.chunk_overlap,
        clear_existing=args.clear
    )

    # Print results
    logger.info("=" * 60)
    logger.info("INGESTION RESULTS")
    logger.info("=" * 60)
    logger.info(f"Files processed: {result.get('files_processed', 0)}")
    logger.info(f"Files failed: {result.get('files_failed', 0)}")
    logger.info(f"Total chunks: {result.get('total_chunks', 0)}")
    logger.info(f"Success: {result.get('success', False)}")
    logger.info("=" * 60)

    # Exit with appropriate code
    if result.get('success', False):
        logger.info("✓ Ingestion completed successfully!")
        sys.exit(0)
    else:
        logger.error("✗ Ingestion completed with errors")
        sys.exit(1)


if __name__ == '__main__':
    main()
