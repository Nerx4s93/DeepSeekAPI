## 1.4.0
- Added the SendMessageAsync method with stream response support
- Added CancellationToken support in all public client methods
- Added HTTP Proxy support
- Moved HTTP handling to a separate APIEngine project
- Reworked the client architecture and separated responsibilities between the API layer and the HTTP layer
- Removed unnecessary and duplicate HTTP headers in requests

## 1.3.1
- Added support for recognition mode (`ModelType.Vision`) via `ChatSettings`

## 1.3.0
- Added a method for retrieving user information
- Added a method for retrieving a list of chat sessions

## 1.2.4
- Added a mode for streaming token output as strings
- The mode for streaming tokens as events has been retained and continues to be supported without changes
- Added support for net8.0 and net9.0

## 1.2.3
- Added a project icon

## 1.2.2
- Added rate limit error handling: `MetaEvent` responses with `finish_reason = rate_limit_reached` are now detected and converted into a `RateLimitError` exception

## 1.2.1
- Fixed a typing error in the `ChatCompletion` method: `string? parentMessageId` -> `long? parentMessageId`

## 1.2.0
- Added support for expert mode (`ModelType.Expert`) via `ChatSettings`
- Introduced a typed `ChatSession` model to represent a chat session
- Added `ChatSettings` request configuration, including model selection as well as `Thinking` and `Search` parameters
- Updated the `ChatCompletion` method signature — request parameters are now encapsulated in `ChatSettings`
- Updated the `ChatCompletionAllChunksAsync` method signature to use `ChatSettings`
- Completely rewritten and refactored the streaming response (SSE) parser, introduced an event-based model (`DeepSeekEvent`) with support for `MessageInitEvent`, `TextEvent`, `PatchEvent`, `SearchEvent`, `StatusEvent`, and `MetaEvent`

## 1.1.1
- Added the `ChatCompletionAllChunksAsync` method, which returns all generation chunks at once instead of streaming them

## 1.1.0
- Fixed an issue with the missing `sha3_wasm_bg.7b9ca65ddd.wasm` file. It is now loaded directly from project resources
- Renamed namespace: `POW` → `PoW`

## 1.0.0
- Initial release