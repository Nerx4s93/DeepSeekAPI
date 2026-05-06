namespace DeepSeekAPI.Streaming;

public record PatchEvent(
    string? Path,
    string? Operation,
    string Value
) : DeepSeekEvent;