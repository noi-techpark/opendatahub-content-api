"""
Conversation Memory Management
In-memory storage for chat history per session
"""
import logging
from typing import Dict, List, Any, Optional
from datetime import datetime, timedelta
from dataclasses import dataclass, field
from collections import defaultdict
import uuid

logger = logging.getLogger(__name__)


@dataclass
class ConversationSession:
    """A single conversation session with isolated cache"""
    session_id: str
    messages: List[Any] = field(default_factory=list)
    created_at: datetime = field(default_factory=datetime.now)
    last_activity: datetime = field(default_factory=datetime.now)
    metadata: Dict[str, Any] = field(default_factory=dict)
    _cache: Optional[Any] = field(default=None, init=False, repr=False)  # Lazy-loaded DataCache

    @property
    def cache(self):
        """Get session-specific cache (lazy initialization)"""
        if self._cache is None:
            # Import here to avoid circular dependency
            from tools.data_cache import DataCache
            self._cache = DataCache(ttl_minutes=30)  # 30 min TTL per session
            logger.debug(f"Created cache for session {self.session_id}")
        return self._cache

    def add_message(self, message: Any):
        """Add a message to the conversation"""
        self.messages.append(message)
        self.last_activity = datetime.now()

    def get_messages(self) -> List[Any]:
        """Get all messages in the conversation"""
        return self.messages.copy()

    def clear(self):
        """Clear conversation history"""
        self.messages = []
        self.last_activity = datetime.now()

    def is_expired(self, max_age_hours: int = 24) -> bool:
        """Check if session is expired"""
        return datetime.now() - self.last_activity > timedelta(hours=max_age_hours)


class ConversationMemoryStore:
    """
    In-memory storage for conversation sessions
    Thread-safe, auto-cleanup expired sessions
    """

    def __init__(self, max_age_hours: int = 24):
        """
        Initialize conversation memory store

        Args:
            max_age_hours: Maximum age for inactive sessions before cleanup
        """
        self._sessions: Dict[str, ConversationSession] = {}
        self._max_age_hours = max_age_hours
        logger.info(f"Conversation memory initialized (max age: {max_age_hours}h)")

    def create_session(self, session_id: Optional[str] = None) -> str:
        """
        Create a new conversation session

        Args:
            session_id: Optional session ID (auto-generated if not provided)

        Returns:
            Session ID
        """
        if not session_id:
            session_id = str(uuid.uuid4())

        if session_id in self._sessions:
            logger.warning(f"Session {session_id} already exists, returning existing")
            return session_id

        self._sessions[session_id] = ConversationSession(session_id=session_id)
        logger.info(f"Created new session: {session_id}")
        return session_id

    def get_session(self, session_id: str) -> Optional[ConversationSession]:
        """
        Get a conversation session by ID

        Args:
            session_id: Session identifier

        Returns:
            ConversationSession or None if not found
        """
        session = self._sessions.get(session_id)

        if session and session.is_expired(self._max_age_hours):
            logger.info(f"Session {session_id} expired, removing")
            del self._sessions[session_id]
            return None

        return session

    def get_or_create_session(self, session_id: Optional[str] = None) -> tuple[str, ConversationSession]:
        """
        Get existing session or create new one

        Args:
            session_id: Optional session ID

        Returns:
            Tuple of (session_id, session)
        """
        if session_id:
            session = self.get_session(session_id)
            if session:
                return session_id, session

        # Create new session
        new_session_id = self.create_session(session_id)
        return new_session_id, self._sessions[new_session_id]

    def add_message(self, session_id: str, message: Any) -> bool:
        """
        Add a message to a session

        Args:
            session_id: Session identifier
            message: Message to add

        Returns:
            True if added successfully, False if session not found
        """
        session = self.get_session(session_id)
        if not session:
            logger.warning(f"Session {session_id} not found, cannot add message")
            return False

        session.add_message(message)
        return True

    def get_messages(self, session_id: str) -> List[Any]:
        """
        Get all messages for a session

        Args:
            session_id: Session identifier

        Returns:
            List of messages (empty if session not found)
        """
        session = self.get_session(session_id)
        if not session:
            return []

        return session.get_messages()

    def clear_session(self, session_id: str) -> bool:
        """
        Clear conversation history for a session

        Args:
            session_id: Session identifier

        Returns:
            True if cleared, False if session not found
        """
        session = self.get_session(session_id)
        if not session:
            logger.warning(f"Session {session_id} not found, cannot clear")
            return False

        session.clear()
        logger.info(f"Cleared session: {session_id}")
        return True

    def delete_session(self, session_id: str) -> bool:
        """
        Delete a session completely

        Args:
            session_id: Session identifier

        Returns:
            True if deleted, False if not found
        """
        if session_id in self._sessions:
            del self._sessions[session_id]
            logger.info(f"Deleted session: {session_id}")
            return True

        logger.warning(f"Session {session_id} not found, cannot delete")
        return False

    def cleanup_expired_sessions(self):
        """Remove expired sessions"""
        expired = [
            sid for sid, session in self._sessions.items()
            if session.is_expired(self._max_age_hours)
        ]

        for session_id in expired:
            del self._sessions[session_id]

        if expired:
            logger.info(f"Cleaned up {len(expired)} expired sessions")

    def get_active_session_count(self) -> int:
        """Get number of active sessions"""
        self.cleanup_expired_sessions()
        return len(self._sessions)

    def get_session_info(self, session_id: str) -> Optional[Dict[str, Any]]:
        """
        Get session metadata

        Args:
            session_id: Session identifier

        Returns:
            Dict with session info or None if not found
        """
        session = self.get_session(session_id)
        if not session:
            return None

        return {
            "session_id": session.session_id,
            "message_count": len(session.messages),
            "created_at": session.created_at.isoformat(),
            "last_activity": session.last_activity.isoformat(),
            "age_hours": (datetime.now() - session.created_at).total_seconds() / 3600,
            "metadata": session.metadata
        }


# Global memory store instance
_memory_store: Optional[ConversationMemoryStore] = None


def get_memory_store() -> ConversationMemoryStore:
    """Get global conversation memory store (singleton)"""
    global _memory_store
    if _memory_store is None:
        _memory_store = ConversationMemoryStore()
    return _memory_store
