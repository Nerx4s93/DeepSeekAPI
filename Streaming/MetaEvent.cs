namespace DeepSeekAPI.Streaming;

public record MetaEvent(string Key, string? Value) : DeepSeekEvent;