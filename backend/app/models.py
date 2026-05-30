"""SQLAlchemy ORM models for MemoryWeave database."""

from datetime import datetime
from typing import Optional, List

from sqlalchemy import Column, Integer, String, Text, DateTime, Boolean, ForeignKey, JSON
from sqlalchemy.orm import relationship

from .database import Base


class Personality(Base):
    """Represents a user personality/identity.
    
    Example:
    - User (me)
    - Mom
    - Friend John
    
    Each personality has:
    - Own Obsidian folder in vault
    - Own Telegram account connection
    - Own set of entities, relations, messages
    - Own communication style patterns
    """
    __tablename__ = "personalities"
    
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(255), unique=True, nullable=False)  # "Я", "Мама", "Папа"
    description = Column(Text, nullable=True)
    
    # Telegram connection
    telegram_phone = Column(String(20), nullable=True)
    telegram_api_id = Column(String(50), nullable=True)
    telegram_api_hash = Column(String(100), nullable=True)
    telegram_session_file = Column(String(255), nullable=True)  # Path to session
    
    # Obsidian configuration
    obsidian_folder = Column(String(255), nullable=True)  # "Me/", "Contacts/Mom/" etc.
    obsidian_vault_path = Column(String(500), nullable=True)
    
    # Style/personality settings
    style_description = Column(Text, nullable=True)  # Description of communication style
    is_active = Column(Boolean, default=True)
    
    # Metadata
    created_at = Column(DateTime, default=datetime.utcnow, nullable=False)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow, nullable=False)
    
    # Relationships
    entities = relationship("Entity", back_populates="personality", cascade="all, delete-orphan")
    relations = relationship("Relation", back_populates="personality", cascade="all, delete-orphan")
    messages = relationship("Message", back_populates="personality", cascade="all, delete-orphan")
    style_patterns = relationship("StylePattern", back_populates="personality", cascade="all, delete-orphan")
    conversations = relationship("ConversationSession", back_populates="personality", cascade="all, delete-orphan")
    obsidian_notes = relationship("ObsidianNote", back_populates="personality", cascade="all, delete-orphan")
    sync_logs = relationship("SyncLog", back_populates="personality", cascade="all, delete-orphan")
    
    def __repr__(self):
        return f"<Personality(id={self.id}, name='{self.name}', active={self.is_active})>"


class Entity(Base):
    """Represents an entity: person, place, organization, project, etc.
    
    Belongs to a specific personality (their memory).
    Examples:
    - For "Я": entities are people I know, places I visit
    - For "Мама": entities are people she knows, places she visits
    """
    __tablename__ = "entities"
    
    id = Column(Integer, primary_key=True, index=True)
    personality_id = Column(Integer, ForeignKey("personalities.id", ondelete="CASCADE"), nullable=False)
    
    type = Column(String(50), nullable=False)  # 'person', 'place', 'org', 'project', 'date'
    name = Column(String(255), nullable=False)
    normalized_name = Column(String(255), nullable=False)
    description = Column(Text, nullable=True)
    
    # Person-specific fields
    telegram_username = Column(String(100), nullable=True)
    phone_number = Column(String(20), nullable=True)
    
    # Place-specific fields
    address = Column(String(500), nullable=True)
    
    # Temporal metadata
    first_seen = Column(DateTime, nullable=False)
    last_seen = Column(DateTime, nullable=False)
    mention_count = Column(Integer, default=1)
    significance_score = Column(Integer, default=3)  # 1-5
    
    # Metadata
    created_at = Column(DateTime, default=datetime.utcnow, nullable=False)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow, nullable=False)
    
    # Relationships
    personality = relationship("Personality", back_populates="entities")
    relations_as_source = relationship("Relation", foreign_keys="Relation.source_entity_id", back_populates="source_entity")
    relations_as_target = relationship("Relation", foreign_keys="Relation.target_entity_id", back_populates="target_entity")
    obsidian_notes = relationship("ObsidianNote", back_populates="entity", cascade="all, delete-orphan")
    events_location = relationship("Event", back_populates="location_entity")
    
    __table_args__ = (
        # Unique per personality
        # ('personality_id', 'normalized_name'),
    )
    
    def __repr__(self):
        return f"<Entity(id={self.id}, type='{self.type}', name='{self.name}', personality_id={self.personality_id})>"


class Relation(Base):
    """Represents a relationship between two entities.
    
    Examples:
    - Ivan met Maria (at coffee shop)
    - John discussed Project Alpha
    - Ivan visited Moscow
    """
    __tablename__ = "relations"
    
    id = Column(Integer, primary_key=True, index=True)
    personality_id = Column(Integer, ForeignKey("personalities.id", ondelete="CASCADE"), nullable=False)
    
    source_entity_id = Column(Integer, ForeignKey("entities.id", ondelete="CASCADE"), nullable=False)
    target_entity_id = Column(Integer, ForeignKey("entities.id", ondelete="CASCADE"), nullable=False)
    
    relation_type = Column(String(50), nullable=False)  # 'met', 'discussed', 'collaborated', 'visited', 'called'
    
    # Temporal
    first_date = Column(DateTime, nullable=False)
    last_date = Column(DateTime, nullable=False)
    occurrence_count = Column(Integer, default=1)
    
    # Context
    description = Column(Text, nullable=True)
    significance_score = Column(Integer, default=3)  # 1-5
    
    # Metadata
    created_at = Column(DateTime, default=datetime.utcnow, nullable=False)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow, nullable=False)
    
    # Relationships
    personality = relationship("Personality", back_populates="relations")
    source_entity = relationship("Entity", foreign_keys=[source_entity_id], back_populates="relations_as_source")
    target_entity = relationship("Entity", foreign_keys=[target_entity_id], back_populates="relations_as_target")
    
    def __repr__(self):
        return f"<Relation(id={self.id}, {self.source_entity_id}-{self.relation_type}-{self.target_entity_id})>"


class Message(Base):
    """Raw message from Telegram or Obsidian.
    
    Stores both raw text and NLP processing results.
    """
    __tablename__ = "messages"
    
    id = Column(Integer, primary_key=True, index=True)
    personality_id = Column(Integer, ForeignKey("personalities.id", ondelete="CASCADE"), nullable=False)
    
    source = Column(String(20), nullable=False)  # 'telegram' or 'obsidian'
    source_id = Column(String(255), nullable=False)  # chat_id or file_path
    source_name = Column(String(255), nullable=True)  # Display name
    
    text = Column(Text, nullable=False)
    timestamp = Column(DateTime, nullable=False)
    
    # Telegram-specific
    sender_id = Column(Integer, nullable=True)
    sender_name = Column(String(255), nullable=True)
    telegram_chat_id = Column(Integer, nullable=True)
    message_id = Column(Integer, nullable=True)
    
    # Processing status
    processed = Column(Boolean, default=False)
    processing_error = Column(Text, nullable=True)
    
    # Extracted data (JSON)
    extracted_entities = Column(JSON, nullable=True)  # [{type, name, normalized_name}]
    extracted_events = Column(JSON, nullable=True)    # [{type, date, description}]
    
    # Assessment
    significance_score = Column(Integer, default=3)  # 1-5
    
    # Deduplication
    message_hash = Column(String(64), nullable=True, unique=True)
    
    # Metadata
    created_at = Column(DateTime, default=datetime.utcnow, nullable=False)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow, nullable=False)
    
    # Relationships
    personality = relationship("Personality", back_populates="messages")
    events_sources = relationship("Event", back_populates="source_messages", secondary="event_message_association")
    
    def __repr__(self):
        return f"<Message(id={self.id}, source='{self.source}', timestamp={self.timestamp})>"


class Event(Base):
    """Structured event extracted from messages.
    
    Examples:
    - Meeting with Ivan on 2026-05-25 at 14:30
    - Project Alpha launch on 2026-06-01
    - Birthday party
    """
    __tablename__ = "events"
    
    id = Column(Integer, primary_key=True, index=True)
    personality_id = Column(Integer, ForeignKey("personalities.id", ondelete="CASCADE"), nullable=False)
    
    title = Column(String(255), nullable=False)
    description = Column(Text, nullable=True)
    event_type = Column(String(50), nullable=False)  # 'meeting', 'agreement', 'milestone', 'birthday'
    
    event_date = Column(DateTime, nullable=False)
    
    # Participants (entity IDs stored as JSON)
    participants_json = Column(JSON, nullable=True)  # [1, 2, 3]
    
    # Location
    location_entity_id = Column(Integer, ForeignKey("entities.id", ondelete="SET NULL"), nullable=True)
    
    # Importance
    significance_score = Column(Integer, default=3)  # 1-5
    
    # Source messages (IDs stored as JSON)
    source_message_ids = Column(JSON, nullable=True)  # [1, 2, 3]
    
    # Metadata
    created_at = Column(DateTime, default=datetime.utcnow, nullable=False)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow, nullable=False)
    
    # Relationships
    location_entity = relationship("Entity", back_populates="events_location")
    source_messages = relationship("Message", back_populates="events_sources", secondary="event_message_association")
    
    def __repr__(self):
        return f"<Event(id={self.id}, title='{self.title}', date={self.event_date})>"


class StylePattern(Base):
    """User's communication style patterns.
    
    Examples:
    - Greeting: "Привет, как дела?"
    - Emoji: "👋"
    - Filler: "Кстати,", "На самом деле"
    
    Used to mimic user's style in AI responses.
    """
    __tablename__ = "style_patterns"
    
    id = Column(Integer, primary_key=True, index=True)
    personality_id = Column(Integer, ForeignKey("personalities.id", ondelete="CASCADE"), nullable=False)
    
    pattern = Column(String(255), nullable=False)
    category = Column(String(50), nullable=False)  # 'greeting', 'closing', 'filler', 'emoji', 'curse', 'expression'
    
    frequency = Column(Integer, default=1)
    context = Column(Text, nullable=True)
    
    # Metadata
    created_at = Column(DateTime, default=datetime.utcnow, nullable=False)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow, nullable=False)
    
    # Relationships
    personality = relationship("Personality", back_populates="style_patterns")
    
    def __repr__(self):
        return f"<StylePattern(id={self.id}, pattern='{self.pattern}', category='{self.category}')>"


class ConversationSession(Base):
    """Chat session for context preservation.
    
    One session = one conversation with the AI assistant.
    Stores all turns (user message + AI response) to maintain context.
    """
    __tablename__ = "conversation_sessions"
    
    id = Column(Integer, primary_key=True, index=True)
    personality_id = Column(Integer, ForeignKey("personalities.id", ondelete="CASCADE"), nullable=False)
    
    session_id = Column(String(36), unique=True, nullable=False)  # UUID format
    
    created_at = Column(DateTime, default=datetime.utcnow, nullable=False)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow, nullable=False)
    is_active = Column(Boolean, default=True)
    
    # Relationships
    personality = relationship("Personality", back_populates="conversations")
    messages = relationship("ConversationMessage", back_populates="session", cascade="all, delete-orphan")
    
    def __repr__(self):
        return f"<ConversationSession(id={self.id}, session_id='{self.session_id}', active={self.is_active})>"


class ConversationMessage(Base):
    """Individual message turn in a conversation session.
    
    Stores:
    - User's question
    - AI's response
    - Retrieved facts
    - Performance metrics
    """
    __tablename__ = "conversation_messages"
    
    id = Column(Integer, primary_key=True, index=True)
    session_id = Column(Integer, ForeignKey("conversation_sessions.id", ondelete="CASCADE"), nullable=False)
    
    turn_number = Column(Integer, nullable=False)  # 1, 2, 3...
    
    user_message = Column(Text, nullable=False)
    assistant_response = Column(Text, nullable=False)
    
    # Context tracking
    used_facts_json = Column(JSON, nullable=True)  # [1, 5, 7] entity/relation IDs
    retrieved_documents_count = Column(Integer, nullable=True)
    retrieval_method = Column(String(50), nullable=True)  # 'sqlite', 'chromadb', 'hybrid'
    
    # Performance metrics
    generation_time_ms = Column(Integer, nullable=True)
    retrieval_time_ms = Column(Integer, nullable=True)
    
    # Metadata
    timestamp = Column(DateTime, default=datetime.utcnow, nullable=False)
    
    # Relationships
    session = relationship("ConversationSession", back_populates="messages")
    
    def __repr__(self):
        return f"<ConversationMessage(session_id={self.session_id}, turn={self.turn_number})>"


class ObsidianNote(Base):
    """Track generated Obsidian notes for update management.
    
    When system creates notes in Obsidian (e.g., contact notes),
    we track which notes we created and their status.
    """
    __tablename__ = "obsidian_notes"
    
    id = Column(Integer, primary_key=True, index=True)
    personality_id = Column(Integer, ForeignKey("personalities.id", ondelete="CASCADE"), nullable=False)
    entity_id = Column(Integer, ForeignKey("entities.id", ondelete="CASCADE"), nullable=False)
    
    note_path = Column(String(500), nullable=False)  # "Contacts/Ivan Petrov.md"
    note_title = Column(String(255), nullable=False)  # "Ivan Petrov"
    
    # Sync tracking
    last_updated = Column(DateTime, nullable=True)  # When updated in Obsidian
    last_sync = Column(DateTime, default=datetime.utcnow)  # When we last synced
    
    # Content tracking
    memory_entries_count = Column(Integer, default=0)
    sync_status = Column(String(20), default='pending')  # 'pending', 'synced', 'error'
    sync_error = Column(Text, nullable=True)
    
    # Metadata
    created_at = Column(DateTime, default=datetime.utcnow, nullable=False)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow, nullable=False)
    
    # Relationships
    personality = relationship("Personality", back_populates="obsidian_notes")
    entity = relationship("Entity", back_populates="obsidian_notes")
    
    def __repr__(self):
        return f"<ObsidianNote(id={self.id}, note_path='{self.note_path}', status='{self.sync_status}')>"


class SyncLog(Base):
    """Log of all synchronization operations.
    
    Tracks:
    - When syncs happened
    - How many items processed
    - Errors if any
    
    Useful for debugging and recovery.
    """
    __tablename__ = "sync_log"
    
    id = Column(Integer, primary_key=True, index=True)
    personality_id = Column(Integer, ForeignKey("personalities.id", ondelete="CASCADE"), nullable=False)
    
    sync_type = Column(String(50), nullable=False)  # 'telegram', 'obsidian', 'nlp_process', 'obsidian_write'
    status = Column(String(20), nullable=False)  # 'started', 'completed', 'failed'
    
    start_time = Column(DateTime, nullable=False)
    end_time = Column(DateTime, nullable=True)
    duration_ms = Column(Integer, nullable=True)
    
    # Counters
    records_processed = Column(Integer, nullable=True)
    records_new = Column(Integer, nullable=True)
    records_updated = Column(Integer, nullable=True)
    records_failed = Column(Integer, nullable=True)
    
    # Error tracking
    error_message = Column(Text, nullable=True)
    metadata = Column(JSON, nullable=True)
    
    # Metadata
    created_at = Column(DateTime, default=datetime.utcnow, nullable=False)
    
    # Relationships
    personality = relationship("Personality", back_populates="sync_logs")
    
    def __repr__(self):
        return f"<SyncLog(sync_type='{self.sync_type}', status='{self.status}', duration={self.duration_ms}ms)>"


class Configuration(Base):
    """Global system configuration.
    
    Stores key-value pairs for system settings that apply to all personalities.
    Examples:
    - db_version
    - embedding_model
    - default_llm_provider
    """
    __tablename__ = "configuration"
    
    key = Column(String(255), primary_key=True)
    value = Column(Text, nullable=False)
    value_type = Column(String(20), default='string')  # 'string', 'int', 'bool', 'json'
    
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow, nullable=False)
    
    def __repr__(self):
        return f"<Configuration(key='{self.key}', value='{self.value}')>"


class DeduplicationCache(Base):
    """Cache for message deduplication.
    
    Prevents processing the same message multiple times.
    """
    __tablename__ = "deduplication_cache"
    
    id = Column(Integer, primary_key=True, index=True)
    message_hash = Column(String(64), unique=True, nullable=False)
    entity_hash = Column(String(64), nullable=True)
    
    first_seen = Column(DateTime, default=datetime.utcnow, nullable=False)
    last_seen = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow, nullable=False)
    duplicate_count = Column(Integer, default=1)
    
    def __repr__(self):
        return f"<DeduplicationCache(hash='{self.message_hash}', duplicates={self.duplicate_count})>"


# Association table for Event-Message relationship (many-to-many)
from sqlalchemy import Table

event_message_association = Table(
    'event_message_association',
    Base.metadata,
    Column('event_id', Integer, ForeignKey('events.id', ondelete='CASCADE')),
    Column('message_id', Integer, ForeignKey('messages.id', ondelete='CASCADE'))
)
