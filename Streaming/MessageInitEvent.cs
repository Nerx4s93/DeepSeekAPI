namespace DeepSeekAPI.Streaming;

public record MessageInitEvent(
    long MessageId,
    long ParentId,
    string Role,
    bool ThinkingEnabled,
    bool SearchEnabled,
    string Status,
    string? Content
) : DeepSeekEvent;