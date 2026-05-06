namespace DeepSeekAPI.Exceptions;

public class RateLimitError(string message) : DeepSeekError(message);