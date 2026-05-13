### [[-----EN-----]](../DOCUMENTATION.md) [-----RU-----]

# Документация

Проект DeepSeekAPI построен на базе APIEngine.

Базовая реализация HTTP-запросов полностью вынесена в APIEngine, включая:
- отправку запросов
- обработку ответов
- обработку ошибок
- поддержку прокси и отмены (CancellationToken)

DeepSeekAPI отвечает только за:
- формирование payload'ов
- установку необходимых заголовков
- работу с API DeepSeek
- генерацию и подпись Proof-of-Work (PoW)

---

### GetUserProfileAsync
Возвращает информацию о текущем пользователе.
``` C#
public async Task<UserProfile> GetUserProfileAsync(CancellationToken cancellationToken = default)
```

Параметры:
- `cancellationToken` — токен отмены

Возвращает:
``` C#
public record UserProfile(
    string Id,
    string Email,
    string MobileNumber);
```

---

### GetChatSessionsAsync
Получает список чат-сессий пользователя.
``` C#
public async Task<List<ChatSession>> GetChatSessionsAsync(
    double? updateAt = null,
    CancellationToken cancellationToken = default)
```

Параметры:
- `updateAt` — фильтр по времени обновления
- `cancellationToken` — токен отмены

Возвращает:
- `List<ChatSession>`

---

### CreateChatSessionAsync
Создаёт новую чат-сессию.
``` C#
public async Task<ChatSession> CreateChatSessionAsync(CancellationToken cancellationToken = default)
```

Параметры:
- `cancellationToken` — токен отмены

Возвращает:
- `ChatSession` (с заполненным `Id`)

``` C#
class ChatSettings
{
    ModelType ModelType;
    bool Thinking;
    bool Search;
}
```

---

### UploadFileAsync
Загружает файл в DeepSeek.
``` C#
public async Task<string> UploadFileAsync(
    string filePath,
    CancellationToken cancellationToken = default)
```

Параметры:
- `filePath` — путь к файлу
- `cancellationToken` — токен отмены

Возвращает:
- `string` - `file_id`

---

## Messaging API

### SendMessageAsync
Отправляет сообщение и возвращает **полный ответ как строку**. Встроенный парсер не возвращает мысли и результаты поиска.
``` C#
public async Task<string> SendMessageAsync(
    ChatSession chatSession,
    string prompt,
    ChatSettings chatSettings,
    long? parentMessageId = null,
    List<string>? refFileIds = null,
    CancellationToken cancellationToken = default)
```

Параметры:
- `chatSession` — чат
- `prompt` — текст запроса
- `chatSettings` — настройки генерации
- `parentMessageId` — для продолжения диалога
- `refFileIds` — список файлов
- `cancellationToken` — токен отмены

Возвращает:
- `string` — полный ответ модели

---

### SendMessageStream
Стриминговая версия отправки сообщения.
``` C#
public async IAsyncEnumerable<StreamToken> SendMessageStream(...)
```

Возвращает:
- `IAsyncEnumerable<StreamToken>`

``` C#
class StreamToken
{
    long MessageId;
    string Text;
}
```

---

### ChatCompletionAllChunksAsync
Собирает все события стрима в список.
``` C#
public async Task<List<DeepSeekEvent>> ChatCompletionAllChunksAsync(...)
```

---

### ChatCompletion
Низкоуровневый метод работы со стримом событий. Для полного контроля или если не устраивает реализация парсера в библиотеке.
``` C#
public async IAsyncEnumerable<DeepSeekEvent> ChatCompletion(...)
```

Вовзращает:
- `IAsyncEnumerable<DeepSeekEvent>`

События:
``` C#
public record MessageInitEvent(
    long MessageId,
    long ParentId,
    string Role,
    bool ThinkingEnabled,
    bool SearchEnabled,
    string Status,
    string? Content
) : DeepSeekEvent;

public record MetaEvent(string Key, string? Value) : DeepSeekEvent;

public record PatchEvent(
    string? Path,
    string? Operation,
    string Value
) : DeepSeekEvent;

public record SearchEvent(
    List<string> Queries,
    List<SearchResult>? Results
) : DeepSeekEvent;

public record StatusEvent(string Status) : DeepSeekEvent;

public record TextEvent(string Text) : DeepSeekEvent;
```
