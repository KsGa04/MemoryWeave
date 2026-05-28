# Development Guide

## Getting Started

### Prerequisites Installation

#### Python 3.11+
```bash
# Ubuntu/Debian
sudo apt-get install python3.11 python3.11-venv

# macOS
brew install python@3.11

# Windows
# Download from python.org
```

#### .NET 8 SDK
```bash
# Ubuntu/Debian
sudo apt-get install dotnet-sdk-8.0

# macOS
brew install dotnet

# Windows
# Download from microsoft.com
```

#### Ollama (optional, for local LLM)
```bash
# Visit https://ollama.ai
# Download and install

# After install, download a model
ollama pull llama2
# or
ollama pull mistral
```

### Initial Setup

```bash
# 1. Clone repo
git clone https://github.com/KsGa04/MemoryWeave.git
cd MemoryWeave

# 2. Python backend setup
cd backend
python -m venv venv

# Activate venv
# Linux/macOS
source venv/bin/activate
# Windows
venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt

# 3. C# frontend setup
cd ../frontend
dotnet restore

# 4. Configuration
cd ..
cp .env.example .env
# Edit .env with your settings
```

## Project Structure Details

### Backend (`backend/`)

```
backend/
├── app/
│   ├── __init__.py
│   ├── main.py                 # FastAPI app creation
│   ├── config.py               # Settings from .env
│   ├── schemas/
│   │   ├── __init__.py
│   │   ├── message.py          # Pydantic models for messages
│   │   ├── entity.py           # Entity models
│   │   └── chat.py             # Chat request/response
│   ├── models/
│   │   ├── __init__.py
│   │   ├── database.py         # SQLAlchemy models
│   │   ├── entity.py
│   │   ├── relation.py
│   │   └── message.py
│   ├── api/
│   │   ├── __init__.py
│   │   ├── routes.py           # API endpoints
│   │   ├── sync.py             # /sync endpoint
│   │   ├── chat.py             # /chat endpoint
│   │   └── health.py           # /health endpoint
│   ├── core/
│   │   ├── __init__.py
│   │   ├── ner.py              # Named Entity Recognition
│   │   ├── relations.py        # Relation extraction
│   │   ├── event_detector.py   # Event detection
│   │   └── graph_builder.py    # Memory graph operations
│   ├── services/
│   │   ├── __init__.py
│   │   ├── embedding_service.py    # Vector generation
│   │   ├── rag_service.py          # RAG pipeline
│   │   ├── llm_service.py          # LLM wrapper
│   │   ├── obsidian_service.py     # Obsidian API integration
│   │   ├── deduplication.py        # Duplicate detection
│   │   └── style_extractor.py      # User style patterns
│   ├── db/
│   │   ├── __init__.py
│   │   ├── database.py         # DB connection
│   │   └── queries.py          # Common queries
│   └── utils/
│       ├── __init__.py
│       ├── logger.py           # Logging setup
│       └── helpers.py          # Utility functions
├── requirements.txt
├── main.py                     # Entry point
└── .env
```

### Frontend (`frontend/`)

```
frontend/
├── MemoryWeave.Client/
│   ├── MemoryWeave.Client.csproj
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── Models/
│   │   ├── ChatMessage.cs
│   │   ├── ConnectionSettings.cs
│   │   ├── SyncStatus.cs
│   │   └── MemoryStats.cs
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   ├── ChatViewModel.cs
│   │   ├── ConnectionsViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── Views/
│   │   ├── ConnectionsView.xaml
│   │   ├── ChatView.xaml
│   │   ├── SettingsView.xaml
│   │   └── MainView.xaml
│   ├── Services/
│   │   ├── ApiClient.cs        # HTTP client to Python server
│   │   ├── TelegramService.cs  # Telegram integration
│   │   ├── ObsidianMonitor.cs  # File system watcher
│   │   └── TrayService.cs      # System tray
│   └── Resources/
│       └── Styles/
└── MemoryWeave.sln
```

## Running the Application

### Terminal 1: Start Python Backend

```bash
cd backend
source venv/bin/activate  # or venv\Scripts\activate on Windows
python -m uvicorn main:app --reload --host 127.0.0.1 --port 8000
```

You should see:
```
INFO:     Started server process [12345]
INFO:     Uvicorn running on http://127.0.0.1:8000
```

API docs available at: http://127.0.0.1:8000/docs

### Terminal 2: Start C# Frontend

```bash
cd frontend
dotnet run
```

## Development Workflow

### 1. Adding a New NLP Module

Example: Adding event detection

```python
# backend/app/core/event_detector.py
from typing import List
from app.schemas.entity import EventSchema

class EventDetector:
    def __init__(self, llm_service):
        self.llm = llm_service
    
    def extract_events(self, text: str) -> List[EventSchema]:
        """Extract events from text."""
        # Implementation here
        pass
    
    def score_significance(self, event: EventSchema) -> int:
        """Rate event importance 1-5."""
        pass
```

Then integrate in `memory_core.py`:
```python
from app.core.event_detector import EventDetector

self.event_detector = EventDetector(self.llm_service)
events = self.event_detector.extract_events(text)
```

### 2. Adding a New API Endpoint

```python
# backend/app/api/routes.py
from fastapi import APIRouter, HTTPException
from app.schemas.message import MessageRequest

router = APIRouter(prefix="/api")

@router.post("/my-endpoint")
async def my_endpoint(request: MessageRequest):
    """My new endpoint description."""
    try:
        result = await process_request(request)
        return {"status": "success", "data": result}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
```

Register in `main.py`:
```python
from app.api.routes import router
app.include_router(router)
```

### 3. Adding a View in C# Frontend

```csharp
// frontend/MemoryWeave.Client/Views/MyView.xaml
<UserControl xmlns="https://github.com/avaloniaui">
    <StackPanel>
        <TextBlock>My View</TextBlock>
    </StackPanel>
</UserControl>

// frontend/MemoryWeave.Client/ViewModels/MyViewModel.cs
public class MyViewModel : ViewModelBase
{
    public void DoSomething()
    {
        // Implementation
    }
}
```

## Testing

### Python Backend Tests

```bash
cd backend
pip install pytest pytest-asyncio
pytest tests/
```

Example test:
```python
# backend/tests/test_ner.py
import pytest
from app.core.ner import NER

@pytest.mark.asyncio
async def test_extract_entities():
    ner = NER()
    text = "Иван встретил Марию в Москве в пятницу"
    entities = await ner.extract(text)
    
    assert len(entities) > 0
    assert any(e.type == "person" for e in entities)
    assert any(e.type == "place" for e in entities)
```

### C# Frontend Tests

```bash
cd frontend
dotnet test
```

## Debugging

### Python Debugging

Using VS Code:
1. Install Python extension
2. Create `.vscode/launch.json`:
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Python: FastAPI",
            "type": "python",
            "request": "launch",
            "module": "uvicorn",
            "args": ["main:app", "--reload"],
            "cwd": "${workspaceFolder}/backend"
        }
    ]
}
```
3. Press F5 to start debugging

### C# Debugging

Using Visual Studio:
1. Open `MemoryWeave.sln`
2. Set breakpoints (F9)
3. Press F5 to start debugging

## Common Issues

### Issue: Python `ModuleNotFoundError`
**Solution**: Ensure venv is activated and dependencies installed
```bash
source venv/bin/activate
pip install -r requirements.txt
```

### Issue: Port 8000 already in use
**Solution**: Change port in `.env` or kill process
```bash
lsof -i :8000  # Find process
kill -9 <PID>  # Kill it
```

### Issue: Ollama not connecting
**Solution**: Ensure Ollama is running
```bash
ollama serve
# In another terminal
ollama pull llama2
```

## Code Style

### Python
- Follow PEP 8
- Use type hints
- Docstrings for functions

```python
def process_message(text: str) -> Dict[str, Any]:
    """Process raw message and extract entities.
    
    Args:
        text: Raw message text
    
    Returns:
        Dictionary with extracted entities and relations
    """
    pass
```

### C#
- Follow Microsoft naming conventions
- Use async/await patterns
- XML documentation comments

```csharp
/// <summary>
/// Processes a chat message and sends it to the server.
/// </summary>
/// <param name="message">The message text</param>
/// <returns>Server response</returns>
public async Task<ChatResponse> ProcessMessageAsync(string message)
{
    // Implementation
}
```

## Version Control

### Branching Strategy

```
main
├── feature/telegram-sync
├── feature/ner-module
├── feature/chat-ui
└── bugfix/duplicate-detection
```

### Commit Messages

```
[backend] Add NER module for entity extraction
[frontend] Implement chat view UI
[test] Add tests for duplicate detection
[docs] Update API documentation
```

## Contributing

1. Create feature branch from `main`
2. Make changes with tests
3. Push to GitHub
4. Create Pull Request with description
5. Code review
6. Merge to main

See [CONTRIBUTING.md](../CONTRIBUTING.md) for details.
