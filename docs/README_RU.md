### [[-----EN-----]](../README.md) [-----RU-----]

# DeepSeekAPI
.NET клиент для DeepSeek Chat API с поддержкой стриминга, поиска, экспертного режима и автоматического обхода Proof-of-Work (PoW).

Подходит для автоматизации, CLI-инструментов и кастомных клиентов.

[Список изменений](CHANGELOG_RU.md)
[Документация](DOCUMENTATION_RU.md)

## Установка
``` bash
dotnet add package DeepSeekAPI --version 1.5.1
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
var client = new DeepSeekClient("YOUR_USER_TOKEN");

// получить информацию о пользователе
var profile = await client.GetUserProfileAsync();
Console.WriteLine($"{profile.Id} {profile.Email} {profile.MobileNumber}");

// получить чат-сессии
var chats = await client.GetChatSessionsAsync();
foreach (var chat in chats)
{
    Console.WriteLine($"{chat.Id} {chat.Title} {chat.TitleType} {chat.Pinned} {chat.ModelType} {chat.UpdatedAt}");
}

// создать чат
ChatSession chatSession = await client.CreateChatSession();

// настройки запроса
var chatSettings = new ChatSettings
{
    ModelType = ModelType.Expert,
    Thinking = true,
    Search = false
};

// отправка сообщения
await foreach (var token in client.SendMessageStream(
    chatSession,
    "Привет",
    chatSettings))
{
    Console.Write(token.Text);
}
```

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
