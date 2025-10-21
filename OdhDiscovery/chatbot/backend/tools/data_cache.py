"""
Data Cache for Large Responses
Stores large data temporarily so agent can aggregate it in subsequent calls
"""
import logging
from typing import Any
from datetime import datetime, timedelta
import uuid

logger = logging.getLogger(__name__)


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


# Global cache instance
_data_cache = DataCache(ttl_minutes=5)


def get_cache() -> DataCache:
    """Get the global data cache instance"""
    return _data_cache
