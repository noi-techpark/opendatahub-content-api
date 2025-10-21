"""
Preprocessing utility functions
Token counting, truncation, and field extraction
"""
import json
import tiktoken
import logging
from typing import Any

logger = logging.getLogger(__name__)


def count_tokens(text: str, model: str = "gpt-4") -> int:
    """
    Count tokens in text using tiktoken

    Args:
        text: Text to count tokens for
        model: Model to use for encoding (default: gpt-4)

    Returns:
        Number of tokens
    """
    try:
        encoding = tiktoken.encoding_for_model(model)
        return len(encoding.encode(text))
    except Exception:
        # Fallback: rough estimate (4 chars per token)
        return len(text) // 4


def truncate_to_token_limit(
    data: dict | list,
    max_tokens: int = 2000,
    model: str = "gpt-4"
) -> dict:
    """
    Truncate data to fit within token limit

    Args:
        data: Data to truncate
        max_tokens: Maximum tokens allowed
        model: Model to use for token counting

    Returns:
        Truncated data with metadata
    """
    text = json.dumps(data, indent=2)
    tokens = count_tokens(text, model)

    if tokens <= max_tokens:
        return {"data": data, "tokens": tokens, "truncated": False}

    logger.warning(f"Data exceeds token limit ({tokens} > {max_tokens}), truncating...")

    # Strategy 1: If it's a dict with 'Items', reduce Items array
    if isinstance(data, dict) and 'Items' in data and isinstance(data['Items'], list):
        items = data['Items']
        for n in range(len(items), 0, -1):
            truncated = {**data, 'Items': items[:n]}
            truncated_text = json.dumps(truncated, indent=2)
            if count_tokens(truncated_text, model) <= max_tokens:
                logger.info(f"Truncated Items from {len(items)} to {n} entries")
                return {
                    "data": truncated,
                    "tokens": count_tokens(truncated_text, model),
                    "truncated": True,
                    "original_count": len(items),
                    "truncated_count": n
                }

    # Strategy 2: If it's a list, reduce list size
    if isinstance(data, list):
        for n in range(len(data), 0, -1):
            truncated = data[:n]
            truncated_text = json.dumps(truncated, indent=2)
            if count_tokens(truncated_text, model) <= max_tokens:
                logger.info(f"Truncated list from {len(data)} to {n} entries")
                return {
                    "data": truncated,
                    "tokens": count_tokens(truncated_text, model),
                    "truncated": True,
                    "original_count": len(data),
                    "truncated_count": n
                }

    # Strategy 3: Emergency fallback - convert to summary string
    summary = {
        "error": "Data too large to process",
        "summary": str(data)[:1000],
        "original_tokens": tokens,
        "max_tokens": max_tokens
    }

    return {
        "data": summary,
        "tokens": count_tokens(json.dumps(summary), model),
        "truncated": True,
        "emergency": True
    }


def extract_fields(
    entries: list[dict],
    fields: list[str]
) -> list[dict]:
    """
    Extract only specified fields from entries (field projection)

    Args:
        entries: List of entry objects
        fields: List of field names to extract

    Returns:
        List of entries with only specified fields
    """
    extracted = []
    for entry in entries:
        projected = {}
        for field in fields:
            if field in entry:
                projected[field] = entry[field]
            elif '.' in field:
                # Support nested fields like 'Detail.en.Title'
                value = entry
                for part in field.split('.'):
                    if isinstance(value, dict) and part in value:
                        value = value[part]
                    else:
                        value = None
                        break
                if value is not None:
                    projected[field] = value
        extracted.append(projected)

    logger.debug(f"Extracted {len(fields)} fields from {len(entries)} entries")
    return extracted


def simplify_entry(entry: dict, max_depth: int = 2) -> dict:
    """
    Simplify an entry by removing deeply nested structures

    Args:
        entry: Entry object to simplify
        max_depth: Maximum nesting depth to preserve

    Returns:
        Simplified entry
    """
    def _simplify(obj: Any, depth: int = 0) -> Any:
        if depth >= max_depth:
            if isinstance(obj, dict):
                return f"<object with {len(obj)} keys>"
            elif isinstance(obj, list):
                return f"<array with {len(obj)} items>"
            else:
                return obj

        if isinstance(obj, dict):
            return {k: _simplify(v, depth + 1) for k, v in obj.items()}
        elif isinstance(obj, list):
            if len(obj) > 5:
                # Truncate long arrays
                return [_simplify(item, depth + 1) for item in obj[:5]] + [f"... and {len(obj) - 5} more"]
            else:
                return [_simplify(item, depth + 1) for item in obj]
        else:
            return obj

    return _simplify(entry)
