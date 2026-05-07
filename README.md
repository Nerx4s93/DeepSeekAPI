### [-----EN-----] [[-----RU-----]](docs/README_RU.md)

# DeepSeekAPI
.NET client for the DeepSeek Chat API with support for streaming, search, expert mode, and automatic Proof-of-Work (PoW) handling.

Suitable for automation, CLI tools, and custom clients.

[Changelog](docs/CHANGELOG_EN.md)

## Installation
``` bash
dotnet add package DeepSeekAPI --version 1.2.3
```

## Authentication
An auth token from DeepSeek is required.
How to obtain it:
1. Open https://chat.deepseek.com
2. Open DevTools (**F12**)
3. Go to **Application → Local Storage**
4. Find the `userToken` key
5. Copy its `value`

### ⚠ Important
**Never commit your token to Git**
The token provides full access to your account

## Quick Start
```csharp
using DeepSeekAPI;
using DeepSeekAPI.Models.Chat;

var client = new DeepSeekClient("YOUR_TOKEN");

// create chat
ChatSession chat = await client.CreateChatSession();

// request settings
var settings = new ChatSettings
{
    ModelType = ModelType.Expert,
    Thinking = false,
    Search = false
};

// send message
await foreach (var chunk in client.ChatCompletion(
    chat,
    settings,
    "Hello"))
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

## Request Configuration
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

## Event Model (Streaming API)
The response is delivered as a stream of events:	
- MessageInitEvent — message initialization (metadata + first chunk)
- TextEvent — text stream
- PatchEvent — partial updates (append / update)
- SearchEvent — search queries and results
- MetaEvent — service metadata
- StatusEvent — generation state (WIP / FINISHED)

## Proof-of-Work (PoW)
DeepSeek requires PoW for generating responses.
The library automatically:
- retrieves the challenge
- solves it via WASM
- attaches x-ds-pow-response

## ⚠️ Disclaimer
This project is not affiliated with DeepSeek.

Using a private API may violate the service terms.
You use this library at your own risk.
