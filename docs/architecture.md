# MemoryWeave Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                   Desktop Client (C# / .NET 8)              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ WPF/Avalonia UI                                      │  │
│  │  ├─ Connections Tab (Telegram, Obsidian setup)      │  │
│  │  ├─ Chat Tab (AI Assistant)                         │  │
│  │  └─ Settings Tab (Memory, AI config)                │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ Data Collectors                                      │  │
│  │  ├─ TelegramCollector (WTelegramClient)             │  │
│  │  └─ ObsidianMonitor (FileSystemWatcher)            │  │
│  └──────────────────────────────────────────────────────┘  │
│                         ↓ HTTP (localhost:8000)             │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│              NLP Server (Python / FastAPI)                  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ API Routes                                           │  │
│  │  ├─ POST /sync (receive raw messages)               │  │
│  │  ├─ POST /chat (query assistant)                    │  │
│  │  └─ GET /status (health check)                      │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ Memory Core                                          │  │
│  │  ├─ NLP Pipeline                                    │  │
│  │  │  ├─ Text preprocessing                           │  │
│  │  │  ├─ NER (Named Entity Recognition)               │  │
│  │  │  ├─ Relation extraction                          │  │
│  │  │  └─ Event detection                              │  │
│  │  ├─ Memory Graph Builder                            │  │
│  │  │  ├─ Deduplication                                │  │
│  │  │  └─ Graph updates                                │  │
│  │  ├─ Obsidian Integration                            │  │
│  │  │  └─ Auto-note generation & linking               │  │
│  │  └─ Vector Indexing                                 │  │
│  │     └─ Embedding generation & storage               │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ RAG & Chat Pipeline                                 │  │
│  │  ├─ Query encoding                                  │  │
│  │  ├─ Vector search (top-K retrieval)                 │  │
│  │  ├─ Context building                                │  │
│  │  ├─ Prompt engineering (with style)                 │  │
│  │  └─ LLM generation (Ollama/OpenAI/YandexGPT)       │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ Storage Layer                                        │  │
│  │  ├─ SQLite (fact graph, entities, relations)        │  │
│  │  ├─ ChromaDB (embeddings & vectors)                 │  │
│  │  └─ Cache (style patterns, conversation context)    │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              ↓ HTTP API
                      ┌───────────────────┐
                      │ Obsidian REST API │
                      │ (Local REST API)  │
                      └───────────────────┘
                              ↓
                    ┌────────────────────┐
                    │ Obsidian Vault     │
                    │ Memory markdown    │
                    └────────────────────┘
```

## Data Flow

### 1. Data Collection Flow
```
Telegram API → WTelegramClient → C# Client
                                      ↓
                            Raw message objects
                                      ↓
                    POST /api/sync → Python Server
                                      ↓
                    Store in SQLite (raw messages)
                         + Queue for processing

Obsidian Vault → FileSystemWatcher → File change events
                                           ↓
                                   C# Client reads file
                                           ↓
                            POST /api/sync → Python Server
                                           ↓
                            Store in SQLite + Queue
```

### 2. NLP Processing Flow
```
Raw messages from queue
         ↓
[Text Preprocessing]
  - Cleanup, tokenization, normalization
         ↓
[NER - Named Entity Recognition]
  - Extract: people, places, organizations, dates
         ↓
[Relation Extraction]
  - Extract: conversations, meetings, collaborations
         ↓
[Event Detection]
  - Score significance: 1-5
         ↓
[Deduplication]
  - Check hash + vector similarity
         ↓
[Update Memory Graph]
  - Insert/update entities in SQLite
  - Link relations
         ↓
[Generate Embeddings]
  - Convert to vectors using sentence-transformers
         ↓
[Store in ChromaDB]
  - Save vectors with metadata
         ↓
[Generate Obsidian Notes]
  - Create/update contact notes with [[links]]
  - POST to Obsidian Local REST API
```

### 3. Chat Query Flow
```
User question in Desktop Chat
         ↓
POST /api/chat {question}
         ↓
[Query Encoding]
  - Convert question to embedding
         ↓
[Vector Search]
  - ChromaDB similarity search (top-K)
         ↓
[Context Building]
  - Extract facts from search results
  - Get related entities from memory graph
  - Add user style patterns from cache
         ↓
[Prompt Construction]
  - System prompt + context + question
  - Instruction to answer in first person
  - Style patterns for mimicking
         ↓
[LLM Generation]
  - Call Ollama (local) / OpenAI / YandexGPT
         ↓
[Post-processing]
  - Add source citations (optional)
  - Apply style strength modifier
         ↓
Return JSON response
         ↓
Display in Chat UI
```

## Database Schema

### SQLite Tables

```sql
-- Entities (people, places, organizations)
CREATE TABLE entities (
    id INTEGER PRIMARY KEY,
    type TEXT,           -- 'person', 'place', 'org', 'date'
    name TEXT UNIQUE,
    normalized_name TEXT,
    first_seen DATETIME,
    last_seen DATETIME,
    mention_count INTEGER
);

-- Relations (connections between entities)
CREATE TABLE relations (
    id INTEGER PRIMARY KEY,
    source_id INTEGER,
    target_id INTEGER,
    relation_type TEXT,  -- 'met', 'discussed', 'collaborated'
    first_date DATETIME,
    last_date DATETIME,
    context TEXT,
    FOREIGN KEY (source_id) REFERENCES entities(id),
    FOREIGN KEY (target_id) REFERENCES entities(id)
);

-- Messages (raw data with processing status)
CREATE TABLE messages (
    id INTEGER PRIMARY KEY,
    source TEXT,         -- 'telegram' or 'obsidian'
    source_id TEXT,      -- chat_id or file_path
    text TEXT,
    timestamp DATETIME,
    sender TEXT,
    processed BOOLEAN,
    significance_score INTEGER,
    hash TEXT UNIQUE
);

-- Style patterns (user communication style)
CREATE TABLE style_patterns (
    id INTEGER PRIMARY KEY,
    pattern TEXT,        -- frequent phrase or emoji
    frequency INTEGER,
    category TEXT        -- 'greeting', 'closing', 'emoji', 'tic'
);

-- Conversation context (for session management)
CREATE TABLE conversation_context (
    id INTEGER PRIMARY KEY,
    session_id TEXT,
    turn_number INTEGER,
    user_message TEXT,
    assistant_response TEXT,
    timestamp DATETIME,
    relevant_facts TEXT   -- JSON array of fact IDs
);
```

### ChromaDB Collections

```
"messages_embeddings"
  - Documents: text fragments
  - Metadatas: {source, timestamp, sender, entities, significance}
  - Distance: cosine

"style_examples"
  - Documents: example sentences from user
  - Metadatas: {category, context}
  - Distance: cosine
```

## API Endpoints

### Data Collection
```
POST /api/sync
  Body: {
    source: "telegram" | "obsidian",
    messages: [{text, timestamp, sender_id, sender_name, chat_id, ...}]
  }
  Response: {status, processed_count, new_facts_count}
```

### Chat
```
POST /api/chat
  Body: {
    message: string,
    session_id: string (optional),
    context_length: int (default: 5),
    style_strength: float (0.0-1.0, default: 0.5)
  }
  Response: {
    response: string,
    sources: [{text, date, source}],
    facts_used: [id1, id2, ...]
  }
```

### Memory Management
```
GET /api/memory/stats
  Response: {
    total_entities: int,
    total_relations: int,
    total_messages: int,
    last_sync: datetime
  }

DELETE /api/memory/purge
  Body: {older_than_days: int (optional)}
  Response: {deleted_count}
```

## Technology Rationale

### Why Hybrid Architecture?
- **C#/.NET**: Native Windows UI, better for desktop integration (Telegram API, file watching)
- **Python**: NLP ecosystem maturity, LangChain/LlamaIndex, easy LLM integration
- **REST API**: Clean separation of concerns, independent scaling

### Why SQLite + ChromaDB?
- **SQLite**: Relational structure for fact graph, lightweight, local
- **ChromaDB**: Vector operations, semantic search, simple embedding management

### Why sentence-transformers?
- Open source, multilingual (ru+en), runs locally, no API dependency

### Why Ollama?
- Local execution (privacy), no cloud costs, customizable models
- Fallback to OpenAI/YandexGPT for better quality

## Deployment

All components run locally on user's machine:
1. Desktop client executable (.exe or cross-platform)
2. Python server (packaged as standalone or run from source)
3. All data stored in `./data/` directory
4. Communication only via localhost

## Security Considerations

1. **Data Storage**: All data local, no cloud transmission
2. **Credentials**: Encrypted in config (DPAPI on Windows, AES elsewhere)
3. **API Communication**: localhost only (no network exposure)
4. **Obsidian Integration**: Uses Local REST API plugin (local only)
