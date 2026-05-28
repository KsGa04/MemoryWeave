-- MemoryWeave Database Schema
-- SQLite database for storing facts, entities, relations, and configuration
-- Run this script on first application startup

-- Enable foreign keys
PRAGMA foreign_keys = ON;

-- ============================================================================
-- ENTITIES TABLE
-- Stores all identified entities: people, places, organizations, projects, dates
-- ============================================================================
CREATE TABLE IF NOT EXISTS entities (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    type TEXT NOT NULL,              -- 'person', 'place', 'org', 'project', 'date'
    name TEXT NOT NULL,              -- Original name as mentioned (e.g., "Иван Петров")
    normalized_name TEXT UNIQUE NOT NULL,  -- Normalized for deduplication (e.g., "ivan_petrov")
    description TEXT,                -- Optional description
    
    -- Fields for people
    telegram_username TEXT,          -- e.g., "@ivan_p"
    phone_number TEXT,               -- e.g., "+79999999999"
    
    -- Fields for places
    address TEXT,                    -- Address or coordinates
    
    -- Temporal metadata
    first_seen DATETIME NOT NULL,    -- When first mentioned
    last_seen DATETIME NOT NULL,     -- When last mentioned
    mention_count INTEGER DEFAULT 1, -- How many times mentioned
    significance_score INTEGER DEFAULT 3,  -- 1-5, importance ranking
    
    -- System fields
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(normalized_name)
);

CREATE INDEX idx_entities_type ON entities(type);
CREATE INDEX idx_entities_name ON entities(normalized_name);
CREATE INDEX idx_entities_significance ON entities(significance_score DESC);

-- ============================================================================
-- RELATIONS TABLE
-- Stores connections/relationships between entities
-- ============================================================================
CREATE TABLE IF NOT EXISTS relations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    source_entity_id INTEGER NOT NULL,
    target_entity_id INTEGER NOT NULL,
    relation_type TEXT NOT NULL,
    
    first_date DATETIME NOT NULL,
    last_date DATETIME NOT NULL,
    occurrence_count INTEGER DEFAULT 1,
    
    description TEXT,
    significance_score INTEGER DEFAULT 3,
    
    FOREIGN KEY (source_entity_id) REFERENCES entities(id) ON DELETE CASCADE,
    FOREIGN KEY (target_entity_id) REFERENCES entities(id) ON DELETE CASCADE,
    
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    CHECK (source_entity_id != target_entity_id)
);

CREATE INDEX idx_relations_source ON relations(source_entity_id);
CREATE INDEX idx_relations_target ON relations(target_entity_id);
CREATE INDEX idx_relations_type ON relations(relation_type);
CREATE INDEX idx_relations_date ON relations(first_date, last_date);
CREATE INDEX idx_relations_significance ON relations(significance_score DESC);

-- ============================================================================
-- MESSAGES TABLE
-- Stores raw messages from Telegram and Obsidian with processing status
-- ============================================================================
CREATE TABLE IF NOT EXISTS messages (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    source TEXT NOT NULL,
    source_id TEXT NOT NULL,
    source_name TEXT,
    
    text TEXT NOT NULL,
    timestamp DATETIME NOT NULL,
    
    sender_id INTEGER,
    sender_name TEXT,
    telegram_chat_id INTEGER,
    message_id INTEGER,
    
    processed BOOLEAN DEFAULT 0,
    processing_error TEXT,
    
    extracted_entities TEXT,
    extracted_events TEXT,
    
    significance_score INTEGER DEFAULT 3,
    
    message_hash TEXT UNIQUE,
    
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(source, message_id)
);

CREATE INDEX idx_messages_source ON messages(source);
CREATE INDEX idx_messages_timestamp ON messages(timestamp DESC);
CREATE INDEX idx_messages_processed ON messages(processed);
CREATE INDEX idx_messages_significance ON messages(significance_score DESC);
CREATE INDEX idx_messages_hash ON messages(message_hash);

-- ============================================================================
-- EVENTS TABLE
-- Structured events extracted from messages
-- ============================================================================
CREATE TABLE IF NOT EXISTS events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL,
    description TEXT,
    event_type TEXT NOT NULL,
    
    event_date DATETIME NOT NULL,
    
    participants_json TEXT,
    
    location_entity_id INTEGER,
    
    significance_score INTEGER DEFAULT 3,
    
    source_message_ids TEXT,
    
    FOREIGN KEY (location_entity_id) REFERENCES entities(id) ON DELETE SET NULL,
    
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_events_type ON events(event_type);
CREATE INDEX idx_events_date ON events(event_date DESC);
CREATE INDEX idx_events_significance ON events(significance_score DESC);

-- ============================================================================
-- STYLE_PATTERNS TABLE
-- User's communication patterns for AI response style mimicking
-- ============================================================================
CREATE TABLE IF NOT EXISTS style_patterns (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    pattern TEXT NOT NULL UNIQUE,
    category TEXT NOT NULL,
    
    frequency INTEGER DEFAULT 1,
    context TEXT,
    
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_style_patterns_category ON style_patterns(category);
CREATE INDEX idx_style_patterns_frequency ON style_patterns(frequency DESC);

-- ============================================================================
-- CONVERSATION_SESSIONS TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS conversation_sessions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT UNIQUE NOT NULL,
    
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN DEFAULT 1,
    
    UNIQUE(session_id)
);

CREATE INDEX idx_conversation_sessions_active ON conversation_sessions(is_active);

-- ============================================================================
-- CONVERSATION_MESSAGES TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS conversation_messages (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT NOT NULL,
    turn_number INTEGER NOT NULL,
    
    user_message TEXT NOT NULL,
    assistant_response TEXT NOT NULL,
    
    used_facts_json TEXT,
    retrieved_documents_count INTEGER,
    retrieval_method TEXT,
    
    generation_time_ms INTEGER,
    retrieval_time_ms INTEGER,
    
    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (session_id) REFERENCES conversation_sessions(session_id) ON DELETE CASCADE
);

CREATE INDEX idx_conversation_messages_session ON conversation_messages(session_id);
CREATE INDEX idx_conversation_messages_turn ON conversation_messages(session_id, turn_number);

-- ============================================================================
-- CONFIGURATION TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS configuration (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL,
    value_type TEXT DEFAULT 'string',
    
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(key)
);

-- ============================================================================
-- OBSIDIAN_NOTES TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS obsidian_notes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entity_id INTEGER NOT NULL,
    note_path TEXT UNIQUE NOT NULL,
    note_title TEXT NOT NULL,
    
    last_updated DATETIME,
    last_sync DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    memory_entries_count INTEGER DEFAULT 0,
    sync_status TEXT DEFAULT 'pending',
    sync_error TEXT,
    
    FOREIGN KEY (entity_id) REFERENCES entities(id) ON DELETE CASCADE,
    
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_obsidian_notes_entity ON obsidian_notes(entity_id);
CREATE INDEX idx_obsidian_notes_sync_status ON obsidian_notes(sync_status);

-- ============================================================================
-- SYNC_LOG TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS sync_log (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    sync_type TEXT NOT NULL,
    status TEXT NOT NULL,
    
    start_time DATETIME NOT NULL,
    end_time DATETIME,
    duration_ms INTEGER,
    
    records_processed INTEGER,
    records_new INTEGER,
    records_updated INTEGER,
    records_failed INTEGER,
    
    error_message TEXT,
    metadata TEXT,
    
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_sync_log_type ON sync_log(sync_type);
CREATE INDEX idx_sync_log_status ON sync_log(status);
CREATE INDEX idx_sync_log_start_time ON sync_log(start_time DESC);

-- ============================================================================
-- DEDUPLICATION_CACHE TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS deduplication_cache (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    message_hash TEXT UNIQUE NOT NULL,
    entity_hash TEXT,
    first_seen DATETIME DEFAULT CURRENT_TIMESTAMP,
    last_seen DATETIME DEFAULT CURRENT_TIMESTAMP,
    duplicate_count INTEGER DEFAULT 1,
    
    UNIQUE(message_hash)
);

CREATE INDEX idx_dedup_cache_hash ON deduplication_cache(message_hash);

-- ============================================================================
-- DEFAULT CONFIGURATION VALUES
-- ============================================================================
INSERT OR IGNORE INTO configuration (key, value, value_type) VALUES
    ('db_version', '1.0', 'string'),
    ('app_name', 'MemoryWeave', 'string'),
    ('last_sync_telegram', NULL, 'string'),
    ('last_sync_obsidian', NULL, 'string'),
    ('last_nlp_process', NULL, 'string'),
    ('total_entities_count', '0', 'int'),
    ('total_relations_count', '0', 'int'),
    ('total_messages_processed', '0', 'int'),
    ('embedding_model', 'sentence-transformers/multilingual-e5-large', 'string'),
    ('chromadb_path', './data/chroma', 'string'),
    ('obsidian_vault_path', '', 'string'),
    ('memory_notes_folder', 'MemoryBot/Contacts', 'string'),
    ('enable_obsidian_sync', '1', 'bool'),
    ('enable_style_mimicking', '1', 'bool'),
    ('default_significance_threshold', '2', 'int'),
    ('nlp_batch_size', '10', 'int'),
    ('vector_search_top_k', '5', 'int');

-- ============================================================================
-- VIEWS FOR COMMON QUERIES
-- ============================================================================

CREATE VIEW IF NOT EXISTS vw_recent_interactions AS
SELECT 
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
LEFT JOIN messages m ON m.extracted_entities LIKE '%' || e.normalized_name || '%'
WHERE e.type = 'person'
ORDER BY m.timestamp DESC;

CREATE VIEW IF NOT EXISTS vw_unprocessed_messages AS
SELECT 
    id,
    source,
    source_name,
    text,
    timestamp,
    significance_score
FROM messages
WHERE processed = 0
ORDER BY significance_score DESC, timestamp DESC;

CREATE VIEW IF NOT EXISTS vw_important_events AS
SELECT 
    id,
    title,
    description,
    event_type,
    event_date,
    significance_score
FROM events
WHERE significance_score >= 4
ORDER BY event_date DESC;

CREATE VIEW IF NOT EXISTS vw_sync_statistics AS
SELECT 
    sync_type,
    COUNT(*) as total_syncs,
    SUM(CASE WHEN status = 'completed' THEN 1 ELSE 0 END) as successful,
    SUM(CASE WHEN status = 'failed' THEN 1 ELSE 0 END) as failed,
    AVG(duration_ms) as avg_duration_ms,
    SUM(records_processed) as total_records_processed,
    MAX(start_time) as last_sync
FROM sync_log
GROUP BY sync_type;
