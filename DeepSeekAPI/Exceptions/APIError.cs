namespace DeepSeekAPI.Exceptions;

public class APIError(string message, int? statusCode = null) : DeepSeekError(message)
{
    public int? StatusCode => statusCode;
}