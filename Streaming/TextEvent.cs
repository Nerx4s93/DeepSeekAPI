namespace DeepSeekAPI.Streaming;

public record TextEvent(string Text) : DeepSeekEvent;