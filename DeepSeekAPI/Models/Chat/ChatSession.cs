using System.Text.Json.Serialization;

namespace DeepSeekAPI.Models.Chat;

public class ChatSession
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}