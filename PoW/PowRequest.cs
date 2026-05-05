using System.Text.Json.Serialization;

namespace DeepSeekAPI.PoW;

public class PowRequest
{
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;

    [JsonPropertyName("challenge")]
    public string Challenge { get; set; } = string.Empty;

    [JsonPropertyName("salt")]
    public string Salt { get; set; } = string.Empty;

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    [JsonPropertyName("difficulty")]
    public int Difficulty { get; set; }

    [JsonPropertyName("expire_at")]
    public long ExpireAt { get; set; }

    [JsonPropertyName("target_path")]
    public string TargetPath { get; set; } = string.Empty;
}