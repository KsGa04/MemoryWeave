# MemoryWeave 🧠

**Personal Information System with Adaptive Dialog Agent, Long-term Memory and Semantic Network Building**

## Overview

MemoryWeave is a local-first personal knowledge management system that:
- 📱 Automatically collects messages from Telegram and notes from Obsidian
- 🧩 Extracts facts, events, people, places and relationships
- 🕸️ Builds a semantic network of memories in Obsidian
- 💬 Provides an AI-powered chat assistant with long-term memory context
- 🎭 Mimics your personal communication style

## Quick Start

### Prerequisites
- Python 3.11+
- .NET 8 SDK
- Ollama (for local LLM) or OpenAI API key
- Obsidian with Local REST API plugin

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/KsGa04/MemoryWeave.git
   cd MemoryWeave
   ```

2. **Setup Python backend**
   ```bash
   cd backend
   python -m venv venv
   source venv/bin/activate  # On Windows: venv\Scripts\activate
   pip install -r requirements.txt
   ```

3. **Setup C# frontend**
   ```bash
   cd ../frontend
   dotnet restore
   ```

4. **Configure environment**
   ```bash
   cp .env.example .env
   # Edit .env with your settings
   ```

5. **Run**
   ```bash
   # Terminal 1: Start Python server
   cd backend
   python -m uvicorn main:app --reload --host 127.0.0.1 --port 8000
   
   # Terminal 2: Start C# client
   cd frontend
   dotnet run
   ```

## Project Structure

```
MemoryWeave/
├── backend/                 # Python NLP server
│   ├── app/
│   │   ├── core/           # Core NLP modules
│   │   ├── api/            # FastAPI routes
│   │   └── services/       # Business logic
│   ├── requirements.txt
│   └── main.py
├── frontend/                # C# Desktop client
│   ├── MemoryWeave.Client/
│   └── MemoryWeave.sln
├── docs/                    # Documentation
├── .env.example
└── README.md
```

## Architecture

Hybrid architecture: .NET desktop client ↔ Python FastAPI server

- **Frontend (C#/.NET)**: WPF/Avalonia UI, Telegram integration, file monitoring
- **Backend (Python)**: NLP processing, RAG pipeline, LLM integration, vector DB
- **Communication**: REST API over localhost
- **Storage**: SQLite (facts graph) + ChromaDB (vectors)

## Features

### Data Collection
- ✅ Telegram message sync (personal & group chats)
- ✅ Obsidian notes monitoring
- ✅ Automatic daily sync

### Memory Core
- ✅ Named entity extraction (people, places, dates)
- ✅ Event detection and significance scoring
- ✅ Memory graph building
- ✅ Automatic Obsidian note generation
- ✅ Duplicate detection

### AI Assistant
- ✅ RAG-powered Q&A
- ✅ Context-aware responses
- ✅ Style mimicking
- ✅ Source citation

### Desktop UI
- ✅ Connections tab (Telegram, Obsidian setup)
- ✅ Chat tab (dialog interface)
- ✅ Settings tab (AI config, memory management)
- ✅ System tray mode

## Technology Stack

| Component | Technology |
|-----------|------------|
| Frontend | C#, .NET 8, Avalonia UI |
| Backend | Python 3.11, FastAPI |
| LLM | Ollama / OpenAI / YandexGPT |
| Embeddings | sentence-transformers |
| Vector DB | ChromaDB |
| Relational DB | SQLite + SQLAlchemy |
| Telegram | WTelegramClient |
| Obsidian | Local REST API |

## Development Timeline

- Week 1-2: Analysis & Domain Research
- Week 3-5: Data Collector Module
- Week 6-8: Memory Core & Graph Building
- Week 9-11: Chat & RAG Pipeline
- Week 12-14: Desktop Client UI
- Week 15-16: Integration & Testing
- Week 17-18: Experiments & Documentation

## Documentation

- [Architecture Design](docs/architecture.md)
- [API Reference](docs/api.md)
- [Development Guide](docs/development.md)
- [User Manual](docs/user_guide.md)

## License

MIT

## Author

Bachelor's thesis project
