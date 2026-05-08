namespace DeepSeekAPI.Models.Chat;

public record ChatSession(
    string Id,
    string? Title = null,
    string? TitleType = null,
    bool? Pinned = false,
    ModelType? ModelType = null,
    double? UpdatedAt = null);