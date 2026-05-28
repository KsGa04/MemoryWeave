# MemoryWeave Database Schema

## 📊 Обзор системы хранения данных

MemoryWeave использует **две разные БД** для разных задач:

```
┌─────────────────────────────────────────────────────────────────┐
│                    MemoryWeave Data Storage                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  SQLite (./data/memory.db)          ChromaDB (./data/chroma/)  │
│  Реляционная БД                     Векторная БД               │
│  Структурированные данные           Эмбеддинги для поиска      │
│                                                                 │
│  ┌─────────────────────────────┐   ┌─────────────────────────┐ │
│  │ ФАКТЫ И СВЯЗИ               │   │ ВЕКТОРНЫЕ КОЛЛЕКЦИИ     │ │
│  ├─────────────────────────────┤   ├─────────────────────────┤ │
│  │ • Люди                      │   │ • message_embeddings    │ │
│  │ • Места                     │   │   - текст сообщения     │ │
│  │ • Организации               │   │   - дата, отправитель   │ │
│  │ • События                   │   │   - сущности            │ │
│  │ • Взаимосвязи               │   │                         │ │
│  │ • Сырые сообщения          │   │ • style_examples        │ │
│  │ • Стиль общения            │   │   - примеры фраз        │ │
│  │                             │   │   - категория           │ │
│  │ БЫСТРЫЕ ПОИСКИ:             │   │                         │ │
│  │ - По имени человека         │   │ СЕМАНТИЧЕСКИЙ ПОИСК:    │ │
│  │ - По дате события           │   │ - Найти похожие факты   │ │
│  │ - По связям между людьми    │   │   по смыслу             │ │
│  │ - По значимости события     │   │ - Косинусная близость   │ │
│  └─────────────────────────────┘   └─────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🗄️ SQLite (`./data/memory.db`)

### Назначение
- Хранит **структурированные факты** о жизни пользователя
- Построение **графа памяти** (кто с кем, когда встречался)
- Быстрые SQL-запросы (поиск по имени, дате, отношениям)
- Конфигурационные данные

### Таблицы

#### 1️⃣ **entities** — Сущности (люди, места, организации, даты)

```sql
CREATE TABLE entities (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    type TEXT NOT NULL,              -- 'person', 'place', 'org', 'date', 'project'
    name TEXT NOT NULL,              -- Оригинальное имя (как в сообщении)
    normalized_name TEXT UNIQUE,     -- Нормализованное (для дедупликации)
    description TEXT,                -- Описание (опционально)
    
    -- Для людей
    telegram_username TEXT,          -- @ivan_p
    phone_number TEXT,               -- +7XXXXXXXXXX (если известен)
    
    -- Для мест
    address TEXT,                    -- Адрес или координаты
    
    -- Временные метаданные
    first_seen DATETIME NOT NULL,    -- Когда впервые упомянут
    last_seen DATETIME NOT NULL,     -- Когда в последний раз упомянут
    mention_count INTEGER DEFAULT 1, -- Сколько раз упомянут
    significance_score INTEGER,      -- 1-5, как важна эта сущность
    
    -- Метаинформация
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Примеры данных:
-- id=1, type='person', name='Иван Петров', normalized_name='ivan_petrov', 
--       telegram_username='@ivan_p', first_seen='2026-05-20', mention_count=47
-- id=2, type='place', name='Кофемания на Тверской', normalized_name='kofemanya_tverskey', 
--       address='Москва, ул.Тверская', first_seen='2026-05-18'
-- id=3, type='project', name='Проект Альфа', normalized_name='project_alpha', 
--       description='Новое мобильное приложение'
```

**Зачем**: Чтобы узнать, кто такой Иван Петров, как давно его первый раз упомянули, сколько раз о нём говорили

---

#### 2️⃣ **relations** — Связи между сущностями

```sql
CREATE TABLE relations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    source_entity_id INTEGER NOT NULL,    -- Кто (например, Иван)
    target_entity_id INTEGER NOT NULL,    -- С кем/где/над чем (Мария, Москва, Проект)
    relation_type TEXT NOT NULL,          -- 'met', 'discussed', 'collaborated', 'visited'
    
    -- Когда это произошло
    first_date DATETIME NOT NULL,         -- Первый раз
    last_date DATETIME NOT NULL,          -- Последний раз
    occurrence_count INTEGER DEFAULT 1,   -- Сколько раз было
    
    -- Контекст
    description TEXT,                     -- "Договорились встретиться в 14:30"
    significance_score INTEGER,           -- 1-5, насколько важная эта связь
    
    -- Внешние ключи
    FOREIGN KEY (source_entity_id) REFERENCES entities(id),
    FOREIGN KEY (target_entity_id) REFERENCES entities(id),
    
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Примеры данных:
-- id=1, source=1 (Иван), target=2 (Кофемания), relation_type='visited',
--       first_date='2026-05-20 14:30', last_date='2026-05-25 15:45', 
--       occurrence_count=3, description='Встреча в кофейне'
-- id=2, source=1 (Иван), target=3 (Мария), relation_type='discussed',
--       description='Обсуждали Проект Альфа'
-- id=3, source=1 (Иван), target=5 (Проект Альфа), relation_type='collaborated',
--       first_date='2026-05-10', last_date='2026-05-26'
```

**Зачем**: Построить граф — "Иван был в кофейне с Марией, обсуждали Проект Альфа"

---

#### 3️⃣ **messages** — Сырые сообщения

```sql
CREATE TABLE messages (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    source TEXT NOT NULL,          -- 'telegram' или 'obsidian'
    source_id TEXT NOT NULL,       -- chat_id (для Telegram) или file_path (для Obsidian)
    source_name TEXT,              -- 'Иван Петров' или 'Daily Notes'
    
    text TEXT NOT NULL,            -- Полный текст сообщения
    timestamp DATETIME NOT NULL,   -- Когда это было
    
    -- Для Telegram
    sender_id INTEGER,             -- Telegram user_id
    sender_name TEXT,              -- Имя отправителя
    telegram_chat_id INTEGER,      -- ID чата в Telegram
    message_id INTEGER,            -- ID сообщения в Telegram
    
    -- Статус обработки
    processed BOOLEAN DEFAULT 0,   -- Была ли обработана NLP?
    processing_error TEXT,         -- Если что-то пошло не так
    
    -- Извлеченные данные
    extracted_entities TEXT,       -- JSON: [{"type":"person","name":"Иван"}]
    extracted_events TEXT,         -- JSON: [{"type":"meeting","date":"2026-05-25"}]
    
    -- Оценка
    significance_score INTEGER,    -- 1-5, важное ли это сообщение
    
    -- Дедупликация
    message_hash TEXT UNIQUE,      -- Хеш сообщения для проверки дубликатов
    
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(source, message_id)     -- Уникальность в рамках источника
);

-- Примеры данных:
-- id=1, source='telegram', source_name='Иван Петров', 
--       text='Привет! Встретимся в субботу в кофейне на Тверской в 14:30?',
--       timestamp='2026-05-25 10:05', processed=1,
--       extracted_entities='[{"type":"person","name":"Иван"},{"type":"place","name":"Тверская"}]',
--       extracted_events='[{"type":"meeting","date":"2026-05-27","time":"14:30"}]',
--       significance_score=5
-- id=2, source='obsidian', source_name='Daily Notes',
--       text='Сегодня сходили в новый ресторан с Марией. Нам понравилось!',
--       timestamp='2026-05-26 20:15', processed=1,
--       significance_score=3
```

**Зачем**: Сохранить оригинальное сообщение и результаты его NLP-обработки

---

#### 4️⃣ **events** — Более структурированная информация о событиях

```sql
CREATE TABLE events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL,           -- "Встреча с Иваном"
    description TEXT,              -- Полное описание
    event_type TEXT NOT NULL,      -- 'meeting', 'agreement', 'project_milestone', 'birthday'
    
    event_date DATETIME NOT NULL,  -- Когда произойдет/произошло
    
    -- Участники
    participants_json TEXT,        -- JSON: [1, 2, 3] (entity IDs)
    
    -- Место
    location_entity_id INTEGER,    -- Foreign key to place entity
    
    -- Значимость
    significance_score INTEGER,    -- 1-5
    
    -- Внешние ключи
    FOREIGN KEY (location_entity_id) REFERENCES entities(id),
    
    -- Источники
    source_message_ids TEXT,       -- JSON: [1, 2, 3] (message IDs)
    
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Примеры данных:
-- id=1, title='Встреча с Иваном в кофейне', event_type='meeting',
--       event_date='2026-05-27 14:30', participants_json='[1,2]', 
--       location_entity_id=2, significance_score=5
-- id=2, title='Запуск Проекта Альфа', event_type='project_milestone',
--       event_date='2026-06-01 09:00', significance_score=4
```

**Зачем**: Хранить структурированные события для быстрого поиска ("когда я встречался с Иваном?")

---

#### 5️⃣ **style_patterns** — Стиль общения пользователя

```sql
CREATE TABLE style_patterns (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    pattern TEXT NOT NULL,         -- "Привет, как дела?" или "👋"
    category TEXT NOT NULL,        -- 'greeting', 'closing', 'filler', 'emoji', 'curse'
    
    frequency INTEGER DEFAULT 1,   -- Сколько раз встречалось
    context TEXT,                  -- В каком контексте (опционально)
    
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Примеры данных:
-- id=1, pattern='Привет, как дела?', category='greeting', frequency=23
-- id=2, pattern='👋', category='emoji', frequency=47
-- id=3, pattern='Кстати,', category='filler', frequency=15
-- id=4, pattern='Кажется', category='filler', frequency=8
```

**Зачем**: Когда AI отвечает, он использует эти паттерны, чтобы ответ звучал как от пользователя

---

#### 6️⃣ **conversation_sessions** — Сеансы общения в чате

```sql
CREATE TABLE conversation_sessions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT UNIQUE NOT NULL,  -- UUID сеанса
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN DEFAULT 1
);

-- Примеры данных:
-- id=1, session_id='550e8400-e29b-41d4-a716-446655440000', created_at='2026-05-28 15:00'
```

**Зачем**: Отслеживать сеансы чата для сохранения контекста в рамках одной беседы

---

#### 7️⃣ **conversation_messages** — Сообщения внутри сеанса

```sql
CREATE TABLE conversation_messages (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT NOT NULL,         -- Foreign key to session
    turn_number INTEGER NOT NULL,     -- 1-я, 2-я, 3-я реплика в беседе
    
    user_message TEXT NOT NULL,       -- Что спросил пользователь
    assistant_response TEXT NOT NULL, -- Что ответил AI
    
    -- Контекст
    used_facts_json TEXT,             -- JSON массив fact IDs, использованные в ответе
    retrieved_documents INT,          -- Сколько фактов было найдено
    
    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (session_id) REFERENCES conversation_sessions(session_id)
);

-- Примеры данных:
-- id=1, session_id='550e...', turn_number=1, 
--       user_message='О чем мы говорили с Иваном на прошлой неделе?',
--       assistant_response='На прошлой неделе вы встречались с Иваном...',
--       used_facts_json='[3, 5, 7]', retrieved_documents=5
```

**Зачем**: Сохранить историю диалога пользователя с AI (для контекста и логирования)

---

#### 8️⃣ **configuration** — Конфигурация системы

```sql
CREATE TABLE configuration (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Примеры данных:
-- key='last_sync_telegram', value='2026-05-28T15:30:45Z'
-- key='last_sync_obsidian', value='2026-05-28T15:25:00Z'
-- key='total_entities_count', value='247'
-- key='total_relations_count', value='1523'
-- key='obsidian_vault_path', value='/Users/user/Documents/MyVault'
-- key='embedding_model', value='sentence-transformers/multilingual-e5-large'
```

**Зачем**: Хранить глобальные настройки и метаинформацию о состоянии системы

---

### SQLite диаграмма связей

```
┌────────────────┐
│   entities     │ ◄──┐
│ (люди, места)  │    │
└────────────────┘    │
      ▲               │
      │ (1)           │ (2)
      └─────┬────────┘
            │
       ┌────┴─────┐
       │ relations │
       │ (связи)   │
       └─────┬─────┘
             │
      ┌──────┴──────┐
      │              │
  ┌─────────┐   ┌────────┐
  │ messages│   │ events │
  │ (сырые) │   │(значит)│
  └─────────┘   └────────┘
       │              │
       └──────┬───────┘
              │
       ┌──────┴──────────┐
       │                 │
  ┌────────────┐  ┌────────────────┐
  │    style   │  │ conversation   │
  │  patterns  │  │   messages     │
  └────────────┘  └────────────────┘
```

---

## 🔍 ChromaDB (`./data/chroma/`)

### Назначение
- Хранит **векторные представления** (эмбеддинги) текстов
- Быстрый **семантический поиск** (поиск по смыслу, не по ключевым словам)
- Готов к RAG (Retrieval-Augmented Generation)
- Легко масштабируется

### Структура

ChromaDB — это **vector database**, которая хранит данные в специальном формате для быстрого поиска похожих векторов.

**Не SQL**, а JSON + бинарные векторы.

#### 📌 Коллекция: `message_embeddings`

```python
# Как это выглядит в коде Python:

collection = chroma_client.get_collection("message_embeddings")

# Пример добавления документа:
collection.add(
    documents=[
        "Встретились с Иваном в кофейне на Тверской. Обсуждали Проект Альфа."
    ],
    metadatas=[{
        "source": "telegram",
        "sender": "Иван Петров",
        "timestamp": "2026-05-25 14:30",
        "entities": ["person:Иван", "place:Тверская", "project:Альфа"],
        "significance": 5,
        "message_id": 42,
        "obsidian_note_url": "obsidian://open?path=Contacts/Иван%20Петров"
    }],
    ids=["msg_42"],
    embeddings=[[0.123, 0.456, -0.789, ...]]  # 768-мерный вектор
)

# Поиск похожих документов:
results = collection.query(
    query_texts=["О чем мы говорили с Иваном?"],
    n_results=5  # Вернуть топ-5 похожих
)
# Результат:
# {
#   "ids": [["msg_42", "msg_38", "msg_55", ...]],
#   "documents": [["Встретились с Иваном...", "Иван прислал...", ...]],
#   "metadatas": [[{...}, {...}, ...]],
#   "distances": [[0.15, 0.42, 0.67, ...]]  # Косинусное расстояние (меньше = более похоже)
# }
```

**Структура документа в `message_embeddings`:**

| Поле | Тип | Пример | Описание |
|------|-----|--------|---------|
| `id` | string | `msg_42` | Уникальный ID (связан с `messages.id` в SQLite) |
| `document` | string | `"Встретились с Иваном..."` | Текст сообщения или фрагмент |
| `embedding` | vector | `[0.123, 0.456, ...]` | 768-мерный вектор от sentence-transformers |
| **Метаданные:** | | | |
| `source` | string | `telegram` | Откуда пришло (telegram/obsidian) |
| `sender` | string | `Иван Петров` | Кто написал |
| `timestamp` | string | `2026-05-25T14:30:00Z` | Когда это было |
| `entities` | array | `["person:Иван", "place:Тверская"]` | Что упомянуто |
| `significance` | int | `5` | Оценка важности (1-5) |
| `message_id` | int | `42` | Foreign key на `messages.id` |

**Зачем**: Чтобы быстро найти все факты, относящиеся к вопросу пользователя по смыслу

---

#### 📌 Коллекция: `style_examples`

```python
collection = chroma_client.get_collection("style_examples")

# Примеры фраз пользователя:
collection.add(
    documents=[
        "Привет, как дела?",
        "Кстати, совершенно забыл...",
        "На самом деле...",
        "👋",
        "Спасибо за помощь!"
    ],
    metadatas=[
        {"category": "greeting", "context": "начало разговора"},
        {"category": "filler", "context": "переход между темами"},
        {"category": "filler", "context": "уточнение"},
        {"category": "emoji", "context": "приветствие"},
        {"category": "closing", "context": "конец разговора"}
    ],
    ids=["style_1", "style_2", "style_3", "style_4", "style_5"],
    embeddings=[
        # 768-мерные векторы для каждой фразы
    ]
)

# При генерации ответа AI используется эта коллекция
# для подбора похожего стиля
```

**Зачем**: Когда AI генерирует ответ, он смотрит на стиль пользователя и использует похожие фразы

---

## 📊 Полная диаграмма потока данных

```
┌──────────────────────────────────────────────────────────┐
│         ВХОДЯЩИЕ ДАННЫЕ                                  │
├──────────────────────────────────────────────────────────┤
│  Telegram Messages          │  Obsidian Notes            │
│  "Встреча с Иваном"         │  "# Daily Notes 2026-05-28"│
│  дата: 2026-05-25 14:30     │  содержимое файла         │
└──────────────────────────────────────────────────────────┘
                    ↓
          ┌─────────────────────┐
          │   NLP PIPELINE      │
          ├─────────────────────┤
          │ 1. Очистка текста   │
          │ 2. NER              │
          │ 3. Relation extract │
          │ 4. Event detection  │
          │ 5. Scoring          │
          └─────────────────────┘
                    ↓
        ┌───────────────────────────┐
        │   ДВОЙНОЕ СОХРАНЕНИЕ      │
        ├───────────────────────────┤
        │                           │
    ┌───────────────────┐     ┌──────────────────┐
    │    SQLite         │     │   ChromaDB       │
    ├───────────────────┤     ├──────────────────┤
    │ messages:         │     │ message_embeddings:
    │  (сырой текст)    │     │  (вектор + мета) │
    │                   │     │                  │
    │ entities:         │     │ style_examples:  │
    │  Иван, Тверская   │     │  (стиль фразы)   │
    │                   │     │                  │
    │ relations:        │     │                  │
    │  Иван-Тверская    │     │                  │
    │  Иван-Проект      │     │                  │
    │                   │     │                  │
    │ events:           │     │                  │
    │  встреча 14:30    │     │                  │
    │                   │     │                  │
    │ [БЫСТРЫЙ ПОИСК]   │     │ [СЕМАНТИЧЕСКИЙ   │
    │ По имени, дате    │     │  ПОИСК] По смыслу│
    └───────────────────┘     └──────────────────┘
            ↓                           ↓
    ┌─────────────────┐        ┌──────────────┐
    │ Граф памяти:    │        │ RAG поиск:   │
    │ Кто с кем       │        │ Топ-5 фактов │
    │ когда встречался│        │ по релевант. │
    └─────────────────┘        └──────────────┘
                    ↓
        ┌──────────────────────────┐
        │ Obsidian REST API        │
        └──────────────────────────┘
                    ↓
        ┌──────────────────────────┐
        │ Создать заметку:         │
        │ # Иван Петров            │
        │ - Встреча 25 мая в 14:30 │
        │   обсуждали [[Проект А]] │
        │   место: [[Тверская]]    │
        └──────────────────────────┘
```

---

## 💾 Размер данных

### Примерные объемы для пользователя с 1 годом истории:

**SQLite:**
- 10,000 сообщений = ~5 MB
- 200 entities = ~0.1 MB
- 500 relations = ~0.2 MB
- **Итого: ~5-10 MB**

**ChromaDB:**
- 10,000 эмбеддингов × 768 чисел = ~30-40 MB
- **Итого: ~40-50 MB**

**ВСЕГО: ~50-60 MB** на диске за год данных

---

## 🔄 Как работает запрос пользователя

### Пример: "О чем мы говорили с Иваном на прошлой неделе?"

```
1. ПАРСИНГ ВОПРОСА (SQL запрос)
   ↓
   SELECT * FROM entities WHERE name LIKE '%Иван%'
   → Найти ID Ивана (например, id=1)
   
2. SQL ЗАПРОС К ФАКТАМ
   ↓
   SELECT * FROM relations 
   WHERE source_entity_id=1 
   AND last_date > DATE_SUB(NOW(), INTERVAL 7 DAY)
   → Найти все события с Иваном на прошлой неделе
   
3. ВЕКТОРНЫЙ ПОИСК (ChromaDB)
   ↓
   query_text = "О чем мы говорили с Иваном на прошлой неделе?"
   embeddings = encode(query_text)  # превратить в вектор
   results = chroma.query(embeddings, n_results=5)
   → Найти 5 самых похожих сообщений
   
4. СЛИЯНИЕ РЕЗУЛЬТАТОВ
   ↓
   - Факты из SQLite (точные события)
   - Контекст из ChromaDB (похожие сообщения)
   - Стиль пользователя (из style_patterns)
   
5. ГЕНЕРАЦИЯ ОТВЕТА LLM
   ↓
   Prompt: "Ты помощник пользователя. Ответь на его вопрос.
           Используй такой стиль: [примеры фраз пользователя]
           Факты: [найденные события]
           Вопрос: О чем мы говорили с Иваном на прошлой неделе?"
   → LLM генерирует ответ как от пользователя
```

---

## 🛡️ Безопасность и приватность

- ✅ Все данные локальные (на компьютере пользователя)
- ✅ Никаких облачных сервисов
- ✅ Шифрование .env файла (не коммитится)
- ✅ SQLite и ChromaDB работают локально

---

## 📈 Миграции и обновления

Когда схема БД меняется:

```python
# backend/app/db/migrations.py
def migrate_v1_to_v2():
    """Добавить новую колонку в entities"""
    db.execute("""
        ALTER TABLE entities 
        ADD COLUMN importance_tags TEXT
    """)
```

Версия БД хранится в `configuration` таблице.

---

## 🚀 Инициализация БД

При первом запуске:

```python
# backend/app/db/init_db.py
def init_database():
    """Создать все таблицы"""
    # Выполнить SQL скрипт с CREATE TABLE
    # Инициализировать ChromaDB коллекции
    # Установить версию БД
```
