using System.Collections.Generic;
using System.Text.Json;

namespace DeepSeekAPI;

public class DeepSeekChunk
{
    public DeepSeekChunkType Type { get; init; }

    public string? Text { get; init; }

    public string? Path { get; init; }

    public string? Operation { get; init; }

    public List<SearchResult>? SearchResults { get; init; }

    public JsonElement? Raw { get; init; }
}