# DeepSeekAPI
.NET клиент для DeepSeek Chat API с поддержкой стриминга, поиска и обхода Proof-of-Work (PoW) защиты.
Подходит для автоматизации, CLI-инструментов и кастомных клиентов.

📄 [Changelog](CHANGELOG.md)

## 🚀 Возможности
- 💬 Отправка сообщений в DeepSeek Chat
- ⚡ Стриминг ответа (чанками)
- 🔍 Поддержка поиска (search mode)
- 🧠 Поддержка thinking режима
- 🧩 Парсинг SSE-ответов в удобные типы
- 🔐 Автоматическое решение PoW через WASM
- 📦 Минимум зависимостей (HttpClient + Wasmtime)
- 🪶 Полностью типизированные модели

## 📦 Установка
``` bash
dotnet add package DeepSeekAPI --version 1.2.0
```

## 🔑 Аутентификация
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

## ⚡ Быстрый старт
```csharp
using DeepSeekAPI;

var client = new DeepSeekClient("YOUR_TOKEN");

// создать новый чат
var chatId = await client.CreateChatSession();
Console.WriteLine("Chat id: " + chatId);

// отправить сообщение
await foreach (var chunk in client.ChatCompletion(chatId, "Привет"))
{
 if (chunk.Type == DeepSeekChunkType.Text)
 {
     Console.Write(chunk.Text);
 }
}
```

## 🧠 Работа с чанками
DeepSeek отдаёт ответ как поток (SSE), который парсится в DeepSeekChunk.
Типы:
- Text — текст ответа
- SearchStatus — статус поиска
- SearchResults — результаты поиска
- State — служебные данные
- Unknown — всё остальное

## 🔐 Proof-of-Work (PoW)
DeepSeek требует PoW для некоторых запросов.

В библиотеке:
- автоматически запрашивается challenge
- решается через WASM (`wasm_solve`)
- результат кодируется в Base64
- добавляется в header: `x-ds-pow-response`

Ничего делать не нужно — всё внутри DeepSeekClient.

## ⚙️ Конфигурация запроса
``` C#
ChatCompletion(
    sessionId,
    prompt,
    parentMessageId: null,
    thinking: true,
    search: false
);
```
thinking — включает reasoning
search — включает веб-поиск

## ⚠️ Отказ от ответственности
Этот проект не связан с DeepSeek.
Использование приватного API может нарушать условия сервиса.
Вы используете библиотеку на свой риск.
