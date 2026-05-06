## 1.2.0
- Added support for expert mode (`ModelType.Expert`) via `ChatSettings`.
- Introduced a typed `ChatSession` model to represent a chat session.
- Added `ChatSettings` request configuration, including model selection as well as `Thinking` and `Search` parameters.
- Updated the `ChatCompletion` method signature — request parameters are now encapsulated in `ChatSettings`.
- Updated the `ChatCompletionAllChunksAsync` method signature to use `ChatSettings`.
- Completely rewritten and refactored the streaming response (SSE) parser, introduced an event-based model (`DeepSeekEvent`) with support for `MessageInitEvent`, `TextEvent`, `PatchEvent`, `SearchEvent`, `StatusEvent`, and `MetaEvent`.

## 1.1.1
- Added the `ChatCompletionAllChunksAsync` method, which returns all generation chunks at once instead of streaming them.

## 1.1.0
- Fixed an issue with the missing `sha3_wasm_bg.7b9ca65ddd.wasm` file. It is now loaded directly from project resources.
- Renamed namespace: `POW` → `PoW`.

## 1.0.0
- Initial release.