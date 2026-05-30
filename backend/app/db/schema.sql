-- MemoryWeave Database Schema (Updated with Personality Support)
-- SQLite database for storing facts, entities, relations, and configuration
-- Supports multiple personalities (users/identities)
-- Run this script on first application startup

-- Enable foreign keys
PRAGMA foreign_keys = ON;

-- ============================================================================
-- PERSONALITIES TABLE
-- Stores multiple user identities/personalities
-- Each personality has isolated data: contacts, events, messages, style
-- ============================================================================
CREATE TABLE IF NOT EXISTS personalities (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,                      -- "Я", "Мама", "Папа"
    description TEXT,                               -- Description of personality
    
    -- Telegram configuration
    telegram_phone TEXT,                            -- Phone for Telegram auth
    telegram_api_id TEXT,                           -- API ID from telegram.org
    telegram_api_hash TEXT,                         -- API Hash from telegram.org
    telegram_session_file TEXT,                     -- Path to session file
    
    -- Obsidian configuration
    obsidian_folder TEXT,                           -- Folder path "Me/", "Contacts/Mom/"
    obsidian_vault_path TEXT,                       -- Full path to vault
    
    -- Settings
    style_description TEXT,                         -- Description of communication style
    is_active BOOLEAN DEFAULT 1,
    
    -- System fields
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_personalities_active ON personalities(is_active);

-- ============================================================================
-- ENTITIES TABLE
-- Stores all identified entities: people, places, organizations, projects
-- Now scoped to a specific personality
-- ============================================================================
CREATE TABLE IF NOT EXISTS entities (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    personality_id INTEGER NOT NULL,                -- Which personality owns this entity
    
    type TEXT NOT NULL,                             -- 'person', 'place', 'org', 'project', 'date'
    name TEXT NOT NULL,                             -- Original name
    normalized_name TEXT NOT NULL,                  -- Normalized for deduplication
    description TEXT,                               -- Optional description
    
    -- Fields for people
    telegram_username TEXT,                         -- e.g., "@ivan_p"
    phone_number TEXT,                              -- e.g., "+79999999999"
    
    -- Fields for places
    address TEXT,                                   -- Address or coordinates
    
    -- Temporal metadata
    first_seen DATETIME NOT NULL,                   -- When first mentioned
    last_seen DATETIME NOT NULL,                    -- When last mentioned
    mention_count INTEGER DEFAULT 1,                -- How many times mentioned
    significance_score INTEGER DEFAULT 3,           -- 1-5, importance ranking
    
    -- System fields
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign keys and constraints
    FOREIGN KEY (personality_id) REFERENCES personalities(id) ON DELETE CASCADE,
    UNIQUE(personality_id, normalized_name)         -- Unique per personality
);

CREATE INDEX idx_entities_personality ON entities(personality_id);
CREATE INDEX idx_entities_type ON entities(type);
CREATE INDEX idx_entities_name ON entities(normalized_name);
CREATE INDEX idx_entities_significance ON entities(significance_score DESC);

-- ============================================================================
-- RELATIONS TABLE
-- Stores connections/relationships between entities within a personality
-- ============================================================================
CREATE TABLE IF NOT EXISTS relations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    personality_id INTEGER NOT NULL,                -- Which personality owns this relation
    
    source_entity_id INTEGER NOT NULL,
    target_entity_id INTEGER NOT NULL,
    relation_type TEXT NOT NULL,                    -- 'met', 'discussed', 'collaborated', 'visited', 'called'
    
    first_date DATETIME NOT NULL,                   -- First occurrence
    last_date DATETIME NOT NULL,                    -- Last occurrence
    occurrence_count INTEGER DEFAULT 1,             -- How many times this relation occurred
    
    description TEXT,                               -- Context/note about the relation
    significance_score INTEGER DEFAULT 3,           -- 1-5 importance
    
    -- System fields
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign keys
    FOREIGN KEY (personality_id) REFERENCES personalities(id) ON DELETE CASCADE,
    FOREIGN KEY (source_entity_id) REFERENCES entities(id) ON DELETE CASCADE,
    FOREIGN KEY (target_entity_id) REFERENCES entities(id) ON DELETE CASCADE,
    
    CHECK (source_entity_id != target_entity_id)
);

CREATE INDEX idx_relations_personality ON relations(personality_id);
CREATE INDEX idx_relations_source ON relations(source_entity_id);
CREATE INDEX idx_relations_target ON relations(target_entity_id);
CREATE INDEX idx_relations_type ON relations(relation_type);
CREATE INDEX idx_relations_date ON relations(first_date, last_date);
CREATE INDEX idx_relations_significance ON relations(significance_score DESC);

-- ============================================================================
-- MESSAGES TABLE
-- Stores raw messages from Telegram and Obsidian with processing status
-- Scoped to personality
-- ============================================================================
CREATE TABLE IF NOT EXISTS messages (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    personality_id INTEGER NOT NULL,                -- Which personality owns this message
    
    source TEXT NOT NULL,                           -- 'telegram' or 'obsidian'
    source_id TEXT NOT NULL,                        -- chat_id (Telegram) or file_path (Obsidian)
    source_name TEXT,                               -- Display name of source
    
    text TEXT NOT NULL,                             -- Full message/note content
    timestamp DATETIME NOT NULL,                    -- When the message was created
    
    -- Telegram-specific fields
    sender_id INTEGER,                              -- Telegram user_id
    sender_name TEXT,                               -- Sender's name
    telegram_chat_id INTEGER,                       -- Telegram chat ID
    message_id INTEGER,                             -- Telegram message ID
    
    -- Processing status
    processed BOOLEAN DEFAULT 0,                    -- Whether NLP processing completed
    processing_error TEXT,                          -- Error message if processing failed
    
    -- Extracted data (stored as JSON)
    extracted_entities TEXT,                        -- JSON: [{"type":"person","name":"Ivan"}]
    extracted_events TEXT,                          -- JSON: [{"type":"meeting","date":"2026-05-25"}]
    
    -- Assessment
    significance_score INTEGER DEFAULT 3,           -- 1-5 importance ranking
    
    -- Deduplication
    message_hash TEXT UNIQUE,                       -- Hash for duplicate detection
    
    -- System fields
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign keys
    FOREIGN KEY (personality_id) REFERENCES personalities(id) ON DELETE CASCADE,
    UNIQUE(personality_id, source, message_id)     -- Unique per personality per source
);

CREATE INDEX idx_messages_personality ON messages(personality_id);
CREATE INDEX idx_messages_source ON messages(source);
CREATE INDEX idx_messages_timestamp ON messages(timestamp DESC);
CREATE INDEX idx_messages_processed ON messages(processed);
CREATE INDEX idx_messages_significance ON messages(significance_score DESC);
CREATE INDEX idx_messages_hash ON messages(message_hash);

-- ============================================================================
-- EVENTS TABLE
-- Structured events extracted from messages
-- Scoped to personality
-- ============================================================================
CREATE TABLE IF NOT EXISTS events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    personality_id INTEGER NOT NULL,                -- Which personality owns this event
    
    title TEXT NOT NULL,                            -- "Meeting with Ivan"
    description TEXT,                               -- Full description
    event_type TEXT NOT NULL,                       -- 'meeting', 'agreement', 'milestone', 'birthday'
    
    event_date DATETIME NOT NULL,                   -- When the event occurred/will occur
    
    -- Participants (stored as JSON array of entity IDs)
    participants_json TEXT,                         -- JSON: [1, 2, 3]
    
    -- Location
    location_entity_id INTEGER,                     -- Foreign key to place entity
    
    -- Importance
    significance_score INTEGER DEFAULT 3,           -- 1-5 ranking
    
    -- Source messages
    source_message_ids TEXT,                        -- JSON: [1, 2, 3]
    
    -- System fields
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign keys
    FOREIGN KEY (personality_id) REFERENCES personalities(id) ON DELETE CASCADE,
    FOREIGN KEY (location_entity_id) REFERENCES entities(id) ON DELETE SET NULL
);

CREATE INDEX idx_events_personality ON events(personality_id);
CREATE INDEX idx_events_type ON events(event_type);
CREATE INDEX idx_events_date ON events(event_date DESC);
CREATE INDEX idx_events_significance ON events(significance_score DESC);

-- ============================================================================
-- STYLE_PATTERNS TABLE
-- User's communication patterns for AI response style mimicking
-- Scoped to personality
-- ============================================================================
CREATE TABLE IF NOT EXISTS style_patterns (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    personality_id INTEGER NOT NULL,                -- Which personality owns this pattern
    
    pattern TEXT NOT NULL,                          -- The phrase or emoji
    category TEXT NOT NULL,                         -- 'greeting', 'closing', 'filler', 'emoji', 'curse', 'expression'
    
    frequency INTEGER DEFAULT 1,                    -- How many times encountered
    context TEXT,                                   -- Where/when used
    
    -- System fields
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign keys
    FOREIGN KEY (personality_id) REFERENCES personalities(id) ON DELETE CASCADE,
    UNIQUE(personality_id, pattern)                 -- Unique per personality
);

CREATE INDEX idx_style_patterns_personality ON style_patterns(personality_id);
CREATE INDEX idx_style_patterns_category ON style_patterns(category);
CREATE INDEX idx_style_patterns_frequency ON style_patterns(frequency DESC);

-- ============================================================================
-- CONVERSATION_SESSIONS TABLE
-- Chat session management for context preservation
-- Scoped to personality
-- ============================================================================
CREATE TABLE IF NOT EXISTS conversation_sessions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    personality_id INTEGER NOT NULL,                -- Which personality owns this session
    
    session_id TEXT UNIQUE NOT NULL,                -- UUID format
    
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN DEFAULT 1,
    
    -- Foreign keys
    FOREIGN KEY (personality_id) REFERENCES personalities(id) ON DELETE CASCADE
);

CREATE INDEX idx_conversation_sessions_personality ON conversation_sessions(personality_id);
CREATE INDEX idx_conversation_sessions_active ON conversation_sessions(is_active);

-- ============================================================================
-- CONVERSATION_MESSAGES TABLE
-- Individual turns in a conversation session
-- ============================================================================
CREATE TABLE IF NOT EXISTS conversation_messages (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id INTEGER NOT NULL,                    -- Foreign key to session
    
    turn_number INTEGER NOT NULL,                   -- 1, 2, 3... (turn in conversation)
    
    user_message TEXT NOT NULL,                     -- What user asked
    assistant_response TEXT NOT NULL,               -- What AI responded
    
    -- Context tracking
    used_facts_json TEXT,                           -- JSON: [1, 5, 7] (fact IDs used)
    retrieved_documents_count INTEGER,              -- How many documents retrieved
    retrieval_method TEXT,                          -- 'sqlite', 'chromadb', 'hybrid'
    
    -- Performance metrics
    generation_time_ms INTEGER,                     -- How long LLM took
    retrieval_time_ms INTEGER,                      -- How long search took
    
    -- System fields
    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign key
    FOREIGN KEY (session_id) REFERENCES conversation_sessions(id) ON DELETE CASCADE
);

CREATE INDEX idx_conversation_messages_session ON conversation_messages(session_id);
CREATE INDEX idx_conversation_messages_turn ON conversation_messages(session_id, turn_number);

-- ============================================================================
-- OBSIDIAN_NOTES TABLE
-- Track generated Obsidian notes for update management
-- Scoped to personality
-- ============================================================================
CREATE TABLE IF NOT EXISTS obsidian_notes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    personality_id INTEGER NOT NULL,                -- Which personality owns this note
    entity_id INTEGER NOT NULL,                     -- Which entity this note is about
    
    note_path TEXT NOT NULL,                        -- "Contacts/Ivan Petrov.md"
    note_title TEXT NOT NULL,                       -- "Ivan Petrov"
    
    last_updated DATETIME,                          -- When last updated in Obsidian
    last_sync DATETIME DEFAULT CURRENT_TIMESTAMP,   -- When last synced from our system
    
    memory_entries_count INTEGER DEFAULT 0,         -- How many memory entries in the note
    sync_status TEXT DEFAULT 'pending',             -- 'pending', 'synced', 'error'
    sync_error TEXT,                                -- Last sync error if any
    
    -- System fields
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign keys
    FOREIGN KEY (personality_id) REFERENCES personalities(id) ON DELETE CASCADE,
    FOREIGN KEY (entity_id) REFERENCES entities(id) ON DELETE CASCADE,
    UNIQUE(personality_id, note_path)               -- Unique per personality
);

CREATE INDEX idx_obsidian_notes_personality ON obsidian_notes(personality_id);
CREATE INDEX idx_obsidian_notes_entity ON obsidian_notes(entity_id);
CREATE INDEX idx_obsidian_notes_sync_status ON obsidian_notes(sync_status);

-- ============================================================================
-- SYNC_LOG TABLE
-- Log of all synchronization operations for debugging and recovery
-- Scoped to personality
-- ============================================================================
CREATE TABLE IF NOT EXISTS sync_log (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    personality_id INTEGER NOT NULL,                -- Which personality this sync is for
    
    sync_type TEXT NOT NULL,                        -- 'telegram', 'obsidian', 'nlp_process', 'obsidian_write'
    status TEXT NOT NULL,                           -- 'started', 'completed', 'failed'
    
    start_time DATETIME NOT NULL,
    end_time DATETIME,
    duration_ms INTEGER,                            -- How long it took
    
    records_processed INTEGER,                      -- How many items processed
    records_new INTEGER,                            -- How many new items
    records_updated INTEGER,                        -- How many updated
    records_failed INTEGER,                         -- How many failed
    
    error_message TEXT,                             -- Error details if failed
    metadata TEXT,                                  -- JSON with additional details
    
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign key
    FOREIGN KEY (personality_id) REFERENCES personalities(id) ON DELETE CASCADE
);

CREATE INDEX idx_sync_log_personality ON sync_log(personality_id);
CREATE INDEX idx_sync_log_type ON sync_log(sync_type);
CREATE INDEX idx_sync_log_status ON sync_log(status);
CREATE INDEX idx_sync_log_start_time ON sync_log(start_time DESC);

-- ============================================================================
-- DEDUPLICATION_CACHE TABLE
-- Cache for deduplication to avoid processing same messages multiple times
-- ============================================================================
CREATE TABLE IF NOT EXISTS deduplication_cache (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    message_hash TEXT UNIQUE NOT NULL,              -- Hash of message content
    entity_hash TEXT,                               -- Hash of extracted entities
    first_seen DATETIME DEFAULT CURRENT_TIMESTAMP,
    last_seen DATETIME DEFAULT CURRENT_TIMESTAMP,
    duplicate_count INTEGER DEFAULT 1,              -- How many duplicates found
    
    UNIQUE(message_hash)
);

CREATE INDEX idx_dedup_cache_hash ON deduplication_cache(message_hash);

-- ============================================================================
-- CONFIGURATION TABLE
-- Global system configuration (not scoped to personality)
-- ============================================================================
CREATE TABLE IF NOT EXISTS configuration (
    key TEXT PRIMARY KEY,                           -- Configuration key
    value TEXT NOT NULL,                            -- Configuration value
    value_type TEXT DEFAULT 'string',               -- 'string', 'int', 'bool', 'json'
    
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================================
-- DEFAULT CONFIGURATION VALUES
-- Initialize with sensible defaults
-- ============================================================================
INSERT OR IGNORE INTO configuration (key, value, value_type) VALUES
    ('db_version', '2.0', 'string'),
    ('app_name', 'MemoryWeave', 'string'),
    ('embedding_model', 'sentence-transformers/multilingual-e5-large', 'string'),
    ('chromadb_path', './data/chroma', 'string'),
    ('enable_obsidian_sync', '1', 'bool'),
    ('enable_style_mimicking', '1', 'bool'),
    ('default_significance_threshold', '2', 'int'),
    ('nlp_batch_size', '10', 'int'),
    ('vector_search_top_k', '5', 'int'),
    ('max_personalities', '10', 'int');

-- ============================================================================
-- VIEWS FOR COMMON QUERIES
-- ============================================================================

-- View: Most recent interactions with each person (per personality)
CREATE VIEW IF NOT EXISTS vw_recent_interactions AS
SELECT 
    e.personality_id,
    e.id,
    e.name,
    e.telegram_username,
    m.timestamp as last_interaction,
    r.relation_type,
    r.description,
    e.mention_count,
    e.significance_score
FROM entities e
LEFT JOIN relations r ON e.id = r.source_entity_id OR e.id = r.target_entity_id
LEFT JOIN messages m ON m.personality_id = e.personality_id 
    AND m.extracted_entities LIKE '%' || e.normalized_name || '%'
WHERE e.type = 'person'
ORDER BY e.personality_id, m.timestamp DESC;

-- View: Unprocessed messages (per personality)
CREATE VIEW IF NOT EXISTS vw_unprocessed_messages AS
SELECT 
    personality_id,
    id,
    source,
    source_name,
    text,
    timestamp,
    significance_score
FROM messages
WHERE processed = 0
ORDER BY personality_id, significance_score DESC, timestamp DESC;

-- View: High-significance events (per personality)
CREATE VIEW IF NOT EXISTS vw_important_events AS
SELECT 
    personality_id,
    id,
    title,
    description,
    event_type,
    event_date,
    significance_score
FROM events
WHERE significance_score >= 4
ORDER BY personality_id, event_date DESC;

-- View: Sync statistics (per personality)
CREATE VIEW IF NOT EXISTS vw_sync_statistics AS
SELECT 
    personality_id,
    sync_type,
    COUNT(*) as total_syncs,
    SUM(CASE WHEN status = 'completed' THEN 1 ELSE 0 END) as successful,
    SUM(CASE WHEN status = 'failed' THEN 1 ELSE 0 END) as failed,
    AVG(duration_ms) as avg_duration_ms,
    SUM(records_processed) as total_records_processed,
    MAX(start_time) as last_sync
FROM sync_log
GROUP BY personality_id, sync_type;

-- View: Memory statistics for each personality
CREATE VIEW IF NOT EXISTS vw_personality_memory_stats AS
SELECT 
    p.id,
    p.name,
    (SELECT COUNT(*) FROM entities e WHERE e.personality_id = p.id) as entities_count,
    (SELECT COUNT(*) FROM relations r WHERE r.personality_id = p.id) as relations_count,
    (SELECT COUNT(*) FROM messages m WHERE m.personality_id = p.id) as messages_count,
    (SELECT COUNT(*) FROM events ev WHERE ev.personality_id = p.id) as events_count,
    (SELECT COUNT(*) FROM obsidian_notes on WHERE on.personality_id = p.id) as obsidian_notes_count,
    (SELECT MAX(timestamp) FROM messages m WHERE m.personality_id = p.id) as last_message_time
FROM personalities p;
