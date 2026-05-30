# Обновленное техническое задание
# MemoryWeave с поддержкой множественных личностей (Personalities)

## 🎯 Ключевое изменение: Многопользовательская система с отдельными личностями

### Концепция "Personality" (Личность)

Система поддерживает несколько независимых личностей, каждая из которых:

```
Personality = User Identity in the System

Примеры:
┌─────────────────────────────────────────────────────────┐
│ Personality 1: "Я" (User)                               │
├─────────────────────────────────────────────────────────┤
│ • Telegram: мой аккаунт (@myusername)                  │
│ • Obsidian: папка "/Me" в моем хранилище               │
│ • Контакты: люди, которых я знаю                       │
│ • События: мои встречи, мои договорённости             │
│ • Стиль: как я пишу, мои эмодзи, фразы                │
│                                                         │
│ При вопросе "О чем мы говорили?":                      │
│ → ищет в МОИХ сообщениях                               │
│ → создает МОИ заметки в "/Me/Contacts/..."             │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ Personality 2: "Мама"                                   │
├─────────────────────────────────────────────────────────┤
│ • Telegram: отдельное подключение мамы                 │
│   (можно даже от другого девайса или номера)           │
│ • Obsidian: папка "/Contacts/Mom" в ХЕР хранилище     │
│ • Контакты: люди, которых ЗНАЕТ МАМА                  │
│ • События: встречи МАМЫ, её договорённости            │
│ • Стиль: как пишет МАМА, её манера речи               │
│                                                         │
│ При вопросе "Что было с подругой Светой?":            │
│ → ищет в МАМИНЫХ сообщениях                           │
│ → создает заметки в "/Contacts/Mom/..."                │
│ → отвечает как МАМА (её стиль)                         │
└─────────────────────────────────────────────────────────┘

Каждая личность имеет ИЗОЛИРОВАННУЮ память:
- Свои контакты
- Свои события
- Свой стиль общения
- Своё подключение к Telegram
```

---

## 📊 Структура данных с Personalities

### База данных

```
personalities (новая таблица)
├── id
├── name              # "Я", "Мама", "Папа", "Подруга Маша"
├── description       # Описание личности
├── telegram_phone    # Номер телефона для Telegram
├── obsidian_folder   # Путь в Obsidian "Me/", "Contacts/Mom/"
├── is_active         # Активна ли личность
└── created_at, updated_at

Каждая таблица теперь имеет personality_id:

entities (контакты, места)
├── id
├── personality_id    # ← ключевое поле!
├── type, name, ...
└── (остальные поля)

relations (связи)
├── id
├── personality_id    # ← все связи в контексте личности
├── source_entity_id, target_entity_id
└── (остальные поля)

messages (сообщения)
├── id
├── personality_id    # ← каждому сообщению принадлежит личность
├── source, text, ...
└── (остальные поля)

И ТАК ДЛЯ ВСЕХ ТАБЛИЦ
```

---

## 🔄 Как это работает на практике

### Пример 1: Добавление новой личности

```python
# Backend API
POST /api/personalities
{
    "name": "Мама",
    "description": "Моя мама",
    "telegram_phone": "+7 (999) 123-45-67",
    "obsidian_folder": "Contacts/Mom"
}

Response:
{
    "id": 2,
    "name": "Мама",
    "obsidian_folder": "Contacts/Mom",
    "created_at": "2026-05-30T10:00:00Z"
}

# Это создает:
# 1. Запись в таблице personalities
# 2. Осдельный сеанс Telegram подключения
# 3. Отдельное "хранилище" всех данных для этой личности
```

### Пример 2: Синхронизация сообщений

```
Telegram получает сообщение от @ivan_p:
"Встречу тебя в кофейне?"

Система спрашивает: "Это для какой личности?"

Вариант A:
Backend → Desktop Client: "Это ваше сообщение? (Я)"
Desktop Client → Backend: да, personality_id=1

POST /api/sync/telegram
{
    "personality_id": 1,
    "messages": [{
        "text": "Встречу тебя в кофейне?",
        "sender": "Ivan Petrov",
        "timestamp": "2026-05-30T14:30:00Z"
    }]
}

Результат:
- Message добавляется с personality_id=1
- Entities (Ivan, Coffee shop) добавляются с personality_id=1
- Заметка создается в "Me/Contacts/Ivan Petrov.md"

Вариант B (если добавлена личность "Мама"):
Сообщение от @mama_number:
"Приходи в 5, готовлю борщ!"

POST /api/sync/telegram
{
    "personality_id": 2,
    "messages": [{
        "text": "Приходи в 5, готовлю борщ!",
        "sender": "Mom",
        "timestamp": "2026-05-30T15:00:00Z"
    }]
}

Результат:
- Message добавляется с personality_id=2
- Заметка создается в "Contacts/Mom/..." (отдельное место!)
- У "Мамы" появляется контакт "Я"
```

### Пример 3: Чат с AI

```
Desktop Client спрашивает (как пользователь 1 "Я"):
"О чем мы говорили с Иваном?"

Backend:
1. Узнает personality_id пользователя (= 1)
2. Ищет в таблице messages ГДЕ personality_id = 1
3. Находит сообщение: "Встречу тебя в кофейне?" от Ivan
4. Обогащает контекстом стиля пользователя 1
5. Генерирует ответ: "На прошлой неделе встречался с Иваном в кофейне..."

---

Если бы спросила мама (personality_id=2):
"О чем говорила с Иваном?"

Backend:
1. personality_id = 2
2. Ищет В МАМИНЫХ сообщениях
3. Если мама не общается с Иваном → "Не знаю, у вас нет общих сообщений"
4. Обогащает стилем МАМЫ (использует её pattern'ы, фразы)
```

---

## 📁 Структура Obsidian с несколькими личностями

### До (текущая структура):
```
My Vault/
├── MemoryBot/
│   └── Contacts/
│       ├── Ivan Petrov.md
│       ├── Maria Ivanova.md
│       └── ...
└── Daily Notes/
```

### После (с множественными личностями):
```
My Vault/
├── Me/
│   ├── Contacts/
│   │   ├── Ivan Petrov.md      # Мои встречи с Иваном
│   │   ├── Maria Ivanova.md
│   │   └── ...
│   ├── Projects/
│   ├── Places/
│   └── Daily Notes/
│
├── Contacts/
│   └── Mom/
│       ├── Contacts/
│       │   ├── Me.md           # Я в контактах у мамы
│       │   ├── Aunt Svetlana.md
│       │   └── ...
│       ├── Projects/
│       └── Daily Notes/
│
└── Contacts/
    └── Dad/
        ├── Contacts/
        │   ├── Me.md           # Я в контактах у папы
        │   └── ...
        └── ...
```

**Преимущества:**
- ✅ Каждая личность имеет **независимые заметки**
- ✅ Личности могут быть в контактах друг у друга
- ✅ Разные стили общения и контексты
- ✅ Можно увидеть сеть отношений между личностями

---

## 🎨 UI изменения в Desktop Client

### Вкладка "Личности" (новая)

```
╔════════════════════════════════════════╗
║         Мои Личности                   ║
╠════════════════════════════════════════╣
║                                        ║
║  [Я] ✓ активна                         ║
║  • Telegram: подключен                 ║
║  • Obsidian: /Me                       ║
║  • Контактов: 47                       ║
║  • Последняя синхронизация: сейчас    ║
║  [Редактировать] [Удалить]            ║
║                                        ║
║  [Мама]                                ║
║  • Telegram: подключен                 ║
║  • Obsidian: /Contacts/Mom             ║
║  • Контактов: 23                       ║
║  • Последняя синхронизация: 2ч назад  ║
║  [Редактировать] [Удалить]            ║
║                                        ║
║  [Папа]                                ║
║  • Telegram: не подключен              ║
║  • Obsidian: /Contacts/Dad             ║
║  • Контактов: 15                       ║
║  [Редактировать] [Удалить]            ║
║                                        ║
║  ┌──────────────────────────────────┐ ║
║  │ [+ Добавить новую личность]      │ ║
║  └──────────────────────────────────┘ ║
║                                        ║
╚════════════════════════════════════════╝
```

### Селектор личности в Chat вкладке

```
Текущая личность: [Я ▼]     ← Dropdown для выбора

Если выбрать "Мама":
╔════════════════════════════════════════╗
║  💬 Мамина Memory (как мама)          ║
║                                        ║
║  Бот: Привет, мама! Как дела?        ║
║  (в стиле мамы, используя её фразы)   ║
║                                        ║
║  Я:   ...                              ║
╚════════════════════════════════════════╝
```

---

## 🗄️ SQL изменения

### Новая таблица personalities

```sql
CREATE TABLE personalities (
    id INTEGER PRIMARY KEY,
    name TEXT UNIQUE NOT NULL,
    description TEXT,
    
    -- Telegram
    telegram_phone TEXT,
    telegram_api_id TEXT,
    telegram_api_hash TEXT,
    telegram_session_file TEXT,
    
    -- Obsidian
    obsidian_folder TEXT,
    obsidian_vault_path TEXT,
    
    -- Settings
    style_description TEXT,
    is_active BOOLEAN DEFAULT 1,
    
    created_at DATETIME,
    updated_at DATETIME
);
```

### Все остальные таблицы + personality_id

```sql
-- Например, entities раньше:
CREATE TABLE entities (
    id INTEGER PRIMARY KEY,
    type TEXT,
    name TEXT,
    ...
);

-- Теперь:
CREATE TABLE entities (
    id INTEGER PRIMARY KEY,
    personality_id INTEGER NOT NULL,  -- ← ДОБАВИЛИ!
    type TEXT,
    name TEXT,
    ...
    FOREIGN KEY (personality_id) REFERENCES personalities(id)
);

-- И так для всех таблиц:
-- relations, messages, events, style_patterns, conversation_sessions,
-- obsidian_notes, sync_log
```

---

## 🔐 Изоляция данных

Все запросы к базе должны ФИЛЬТРОВАТЬ по personality_id:

```python
# ✗ ПЛОХО (может получить данные других личностей):
users = db.query(Entity).filter(Entity.name == "Ivan").all()

# ✓ ХОРОШО (только данные текущей личности):
current_personality_id = 1
users = db.query(Entity).filter(
    Entity.personality_id == current_personality_id,
    Entity.name == "Ivan"
).all()
```

---

## 📝 Полный поток создания личности

```
1. Desktop Client: Нажимает "+ Добавить личность"
         ↓
2. Выбирает имя: "Мама"
         ↓
3. Frontend отправляет:
   POST /api/personalities
   {
     "name": "Мама",
     "obsidian_folder": "Contacts/Mom"
   }
         ↓
4. Backend создает:
   INSERT INTO personalities (name, obsidian_folder) VALUES
         ↓
5. Backend возвращает:
   {"id": 2, "name": "Мама"}
         ↓
6. Frontend спрашивает:
   "Подключить Telegram для Мамы?"
         ↓
7. Frontend запускает Telegram OAuth:
   POST /api/personalities/2/telegram/connect
   {
     "phone": "+7 (999) 123-45-67"
   }
         ↓
8. Backend:
   - Создает сеанс Telegram
   - Сохраняет session файл
   - Обновляет Personality: telegram_phone, telegram_session_file
         ↓
9. Frontend отправляет:
   POST /api/personalities/2/sync/telegram
         ↓
10. Backend:
    - Получает сообщения из Telegram сеанса мамы
    - Добавляет их в DB с personality_id=2
    - Обрабатывает NLP (все сущности, события → personality_id=2)
    - Создает заметки в /Contacts/Mom/
         ↓
11. Done! Мама добавлена в систему
```

---

## ⚙️ API Endpoints (новые)

```
-- Управление личностями
GET /api/personalities              # Список всех личностей
POST /api/personalities             # Добавить личность
GET /api/personalities/{id}         # Получить личность
PUT /api/personalities/{id}         # Редактировать личность
DELETE /api/personalities/{id}      # Удалить личность

-- Синхронизация для конкретной личности
POST /api/personalities/{id}/sync/telegram
POST /api/personalities/{id}/sync/obsidian

-- Чат для конкретной личности
POST /api/personalities/{id}/chat
GET /api/personalities/{id}/memory/stats

-- Переключение текущей личности (в Desktop Client)
PUT /api/current-personality/{id}
GET /api/current-personality
```

---

## 📊 Пример данных в БД

```
personalities:
├── id=1, name="Я", obsidian_folder="Me"
└── id=2, name="Мама", obsidian_folder="Contacts/Mom"

entities:
├── id=1, personality_id=1, type="person", name="Ivan"
├── id=2, personality_id=1, type="place", name="Coffee"
├── id=3, personality_id=2, type="person", name="Aunt Svetlana"
└── id=4, personality_id=2, type="person", name="Me" (я в контактах у мамы)

relations:
├── id=1, personality_id=1, source=1, target=2, relation_type="visited"
│   (Я встречалась с Иваном в кофейне)
├── id=2, personality_id=2, source=3, target=4, relation_type="called"
│   (Тетя Света звонила маме)

messages:
├── id=1, personality_id=1, text="Встречу тебя в кофейне", sender="Ivan"
├── id=2, personality_id=2, text="Приходи в 5", sender="Mom"

Когда я спрашиваю "О чем говорили?":
→ SELECT * FROM messages WHERE personality_id=1
→ Находит только СВОИ сообщения

Когда мама спрашивает (если когда-нибудь будет чат):
→ SELECT * FROM messages WHERE personality_id=2
→ Находит только МАМИНЫ сообщения
```

---

## 🎯 Преимущества новой архитектуры

✅ **Полная изоляция данных** — мамина память не смешивается с моей

✅ **Независимые Telegram аккаунты** — можно быть в разных чатах

✅ **Отдельные Obsidian папки** — каждая личность видит свои заметки

✅ **Разные стили общения** — AI отвечает как конкретная личность

✅ **Сетевые связи** — личности могут быть в контактах друг у друга

✅ **Масштабируемость** — легко добавлять новых людей

✅ **Приватность** — каждый видит только свою память

---

## 🔄 Обратная совместимость

При добавлении первой личности ("Я"):
- Все существующие данные получают personality_id=1
- Система работает как раньше
- Новые данные добавляются уже с personality_id

---
