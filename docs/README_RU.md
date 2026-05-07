### [[-----EN-----]](../README.md) [-----RU-----]

# DeepSeekAPI
.NET клиент для DeepSeek Chat API с поддержкой стриминга, поиска, экспертного режима и автоматического обхода Proof-of-Work (PoW).

Подходит для автоматизации, CLI-инструментов и кастомных клиентов.

[Список изменений](CHANGELOG_RU.md)

## Установка
``` bash
dotnet add package DeepSeekAPI --version 1.2.3
```

## Аутентификация
Нужен auth token от DeepSeek.
Как получить:
1. Откройте https://chat.deepseek.com
2. Откройте DevTools (`F12`)
3. Вкладка **Application → Local Storage**
4. Найдите ключ `userToken`
5. Скопируйте значение `value`

### ⚠️ Важно
- **Никогда не коммитьте токен в Git**
- Токен даёт полный доступ к аккаунту

## Быстрый старт
```csharp
using DeepSeekAPI;
using DeepSeekAPI.Models.Chat;

var client = new DeepSeekClient("YOUR_TOKEN");

// создать чат
ChatSession chat = await client.CreateChatSession();

// настройки запроса
var settings = new ChatSettings
{
    ModelType = ModelType.Expert,
    Thinking = false,
    Search = false
};

// отправка сообщения
await foreach (var chunk in client.ChatCompletion(
    chat,
    settings,
    "Привет"))
{
    if (chunk is TextEvent text)
    {
        Console.Write(text.Text);
    }

    if (chunk is MessageInitEvent init)
    {
        Console.WriteLine($"MessageId: {init.MessageId}");
        Console.Write(init.Content);
    }
}
```

## Конфигурация запроса
``` C#
ChatCompletion(
    ChatSession chatSession,
    string prompt,
    ChatSettings chatSettings,
    string? parentMessageId = null
)

class ChatSettings
{
    ModelType ModelType;   // Default / Expert
    bool Thinking;
    bool Search;
}
```

## Модель событий (Streaming API)
Ответ приходит как поток событий:
- MessageInitEvent — инициализация сообщения (мета + первый фрагмент)
- TextEvent — поток текста
- PatchEvent — обновления фрагментов (append / update)
- SearchEvent — поиск и результаты
- MetaEvent — служебные данные
- StatusEvent — состояние генерации (WIP / FINISHED)

## Proof-of-Work (PoW)
DeepSeek требует PoW для генерации ответов.
Библиотека автоматически:
- получает challenge
- решает через WASM
- добавляет x-ds-pow-response

## ⚠️ Отказ от ответственности
Этот проект не связан с DeepSeek.

Использование приватного API может нарушать условия сервиса.
Вы используете библиотеку на свой риск.
