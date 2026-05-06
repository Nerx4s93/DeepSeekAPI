## 1.2.2
- - Добавлена обработка rate limit ошибок: ответы `MetaEvent` с `finish_reason = rate_limit_reached` теперь автоматически распознаются и преобразуются в исключение `RateLimitError`.

## 1.2.1
- Исправлена ошибка неверной типизации в методе `ChatCompletion`: `string? parentMessageId` -> `long? parentMessageId`

## 1.2.0
- Добавлена поддержка экспертного режима (`ModelType.Expert`) через `ChatSettings`.
- Добавлена типизированная модель `ChatSession` для представления сессии чата.
- Добавлена конфигурация запроса `ChatSettings`, включающая выбор модели, а также параметры `Thinking` и `Search`.
- Обновлена сигнатура метода `ChatCompletion` — параметры запроса вынесены в `ChatSettings`.
- Обновлена сигнатура метода `ChatCompletionAllChunksAsync` с использованием `ChatSettings
- Полностью переписан и переработан парсер потоковых ответов (SSE), добавлена событийная модель (DeepSeekEvent) с поддержкой `MessageInitEvent, TextEvent, PatchEvent, SearchEvent, StatusEvent и MetaEvent`.

## 1.1.1
- Добавлен метод `ChatCompletionAllChunksAsync`, который возвращает все чанки генерации сразу, а не потоково.

## 1.1.0
- Исправлена ошибка, связанная с отсутствием файла `sha3_wasm_bg.7b9ca65ddd.wasm`. Теперь он загружается напрямую из ресурсов проекта.
- Изменено имя пространства имён: `POW` → `PoW`.

## 1.0.0
- Релиз.
