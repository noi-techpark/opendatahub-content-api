"""
Data Cache for Large Responses
Stores large data temporarily so agent can aggregate it in subsequent calls
"""
import logging
from typing import Any, Optional
from datetime import datetime, timedelta
from contextvars import ContextVar
import uuid

logger = logging.getLogger(__name__)

# Context variable for session-specific cache (multi-user isolation)
_session_cache: ContextVar[Optional['DataCache']] = ContextVar('session_cache', default=None)


class DataCache:
    """
    Simple in-memory cache for storing large data between tool calls
    Allows agent to fetch full data, then aggregate it in next call
    """

    def __init__(self, ttl_minutes: int = 5):
        """
        Initialize cache

        Args:
            ttl_minutes: Time-to-live for cached items in minutes
        """
        self._cache: dict[str, dict] = {}
        self._ttl = timedelta(minutes=ttl_minutes)

    def store(self, data: Any, key: str | None = None) -> str:
        """
        Store data in cache and return key

        Args:
            data: Data to store
            key: Optional custom key (auto-generated if not provided)

        Returns:
            Cache key to retrieve data later
        """
        if key is None:
            key = f"cache_{uuid.uuid4().hex[:8]}"

        self._cache[key] = {
            "data": data,
            "stored_at": datetime.now(),
            "accessed_count": 0
        }

        logger.info(f"💾 Stored data in cache with key: {key}")
        self._cleanup_expired()

        return key

    def get(self, key: str) -> Any | None:
        """
        Retrieve data from cache

        Args:
            key: Cache key

        Returns:
            Cached data or None if not found/expired
        """
        if key not in self._cache:
            logger.warning(f"❌ Cache miss: key '{key}' not found")
            return None

        entry = self._cache[key]

        # Check expiration
        if datetime.now() - entry["stored_at"] > self._ttl:
            logger.warning(f"⏰ Cache expired: key '{key}'")
            del self._cache[key]
            return None

        # Update access count
        entry["accessed_count"] += 1
        logger.info(f"✓ Cache hit: key '{key}' (accessed {entry['accessed_count']} times)")

        return entry["data"]

    def delete(self, key: str) -> bool:
        """
        Delete data from cache

        Args:
            key: Cache key

        Returns:
            True if deleted, False if not found
        """
        if key in self._cache:
            del self._cache[key]
            logger.info(f"🗑️  Deleted cache key: {key}")
            return True
        return False

    def _cleanup_expired(self):
        """Remove expired entries"""
        now = datetime.now()
        expired_keys = [
            key for key, entry in self._cache.items()
            if now - entry["stored_at"] > self._ttl
        ]

        for key in expired_keys:
            del self._cache[key]

        if expired_keys:
            logger.info(f"🧹 Cleaned up {len(expired_keys)} expired cache entries")

    def clear(self):
        """Clear all cached data"""
        count = len(self._cache)
        self._cache.clear()
        logger.info(f"🧹 Cleared {count} cache entries")

    def stats(self) -> dict:
        """Get cache statistics"""
        return {
            "total_entries": len(self._cache),
            "entries": [
                {
                    "key": key,
                    "stored_at": entry["stored_at"].isoformat(),
                    "accessed_count": entry["accessed_count"],
                    "age_seconds": (datetime.now() - entry["stored_at"]).total_seconds()
                }
                for key, entry in self._cache.items()
            ]
        }


# Global cache instance (fallback for non-session contexts)
_data_cache = DataCache(ttl_minutes=5)


def get_cache() -> DataCache:
    """
    Get the appropriate data cache instance

    Returns session-specific cache if set (multi-user isolation),
    otherwise returns global cache (fallback for non-session contexts)
    """
    session_cache = _session_cache.get()
    if session_cache is not None:
        return session_cache

    # Fallback to global cache (for tests, direct API calls, etc.)
    logger.debug("Using global cache (no session context set)")
    return _data_cache


def set_session_cache(cache: DataCache):
    """Set the session-specific cache for current context"""
    _session_cache.set(cache)
    logger.debug(f"Set session cache in context")


def clear_session_cache():
    """Clear the session-specific cache from current context"""
    _session_cache.set(None)
    logger.debug(f"Cleared session cache from context")
