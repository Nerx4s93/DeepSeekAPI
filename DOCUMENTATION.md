### [-----EN-----] [[-----RU-----]](docs/DOCUMENTATION_RU.md)

# Documentation

The DeepSeekAPI project is built on top of [APIEngine](https://github.com/Nerx4s93/APIEngine).

The basic HTTP request implementation is entirely delegated to APIEngine, including:
- sending requests
- response handling
- error handling
- proxy and cancellation support (CancellationToken)

DeepSeekAPI is only responsible for:
- constructing payloads
- setting required headers
- working with the DeepSeek API
- Proof-of-Work (PoW) generation and signing

---

### GetUserProfileAsync
Returns information about the current user.
``` C#
public async Task<UserProfile> GetUserProfileAsync(CancellationToken cancellationToken = default)
```

Parameters:
- `cancellationToken` — cancellation token

Returns:
``` C#
public record UserProfile(
    string Id,
    string Email,
    string MobileNumber);
```

---

## Chat sessions API

### CreateChatSessionAsync
Creates a new chat session.
``` C#
public async Task<ChatSession> CreateChatSessionAsync(CancellationToken cancellationToken = default)
```

Parameters:
- `cancellationToken` — cancellation token

Returns:
- `ChatSession` (with a populated `Id`)

``` C#
class ChatSettings
{
    ModelType ModelType;
    bool Thinking;
    bool Search;
}
```

---

### DeleteChatSessionAsync
Udelnie chat session.
``` C#
public async Task<List<ChatSession>> DeleteChatSessionAsync(
    ChatSession chatSession,
    CancellationToken cancellationToken = default)
```

Parameters:
- `chatSession` — chat session
- `cancellationToken` — cancellation token

Returns:
- `bool` — success of request execution

---

### GetChatSessionsAsync
Retrieves the user's list of chat sessions.
``` C#
public async Task<List<ChatSession>> GetChatSessionsAsync(
    double? updateAt = null,
    CancellationToken cancellationToken = default)
```

Parameters:
- `updateAt` — filter by update time
- `cancellationToken` — cancellation token

Returns:
- `List<ChatSession>`

---

## Messaging API

### StopGenerationAsync

``` C#
public async Task<bool> StopGenerationAsync(
    ChatSession chatSession,
    long messageId,
    CancellationToken cancellationToken = default)
```

Parameters:
- `chatSession` — chat session
- `parentMessageId` — message id
- `cancellationToken` — cancellation token

Returns:
- `bool` — success of request execution

---

### UploadFileAsync
Uploads a file to DeepSeek.
``` C#
public async Task<string> UploadFileAsync(
    string filePath,
    CancellationToken cancellationToken = default)
```

Parameters:
- `filePath` — path to the file
- `cancellationToken` — cancellation token

Returns:
- `string` - `file_id`

---

### SendMessageAsync
Sends a message and returns the **full response as a string**. The built-in parser does not return thoughts or search results.
``` C#
public async Task<string> SendMessageAsync(
    ChatSession chatSession,
    string prompt,
    ChatSettings chatSettings,
    long? parentMessageId = null,
    List<string>? refFileIds = null,
    CancellationToken cancellationToken = default)
```

Parameters:
- `chatSession` — chat session
- `prompt` — prompt text
- `chatSettings` — generation settings
- `parentMessageId` — for continuing a conversation
- `refFileIds` — list of files
- `cancellationToken` — cancellation token

Returns:
- `string` — the model's full response

---

### SendMessageStream
Streaming version of sending a message.
``` C#
public async IAsyncEnumerable<StreamToken> SendMessageStream(...)
```

Returns:
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
Collects all stream events into a list.
``` C#
public async Task<List<DeepSeekEvent>> ChatCompletionAllChunksAsync(...)
```

---

### ChatCompletion
Low-level method for working with the event stream. For full control or if the library's built-in parser is not suitable.
``` C#
public async IAsyncEnumerable<DeepSeekEvent> ChatCompletion(...)
```

Returns:
- `IAsyncEnumerable<DeepSeekEvent>`

Events:
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
