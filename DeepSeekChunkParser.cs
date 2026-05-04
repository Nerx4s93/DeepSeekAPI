using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DeepSeekAPI;

public class DeepSeekChunkParser
{
    public IEnumerable<DeepSeekChunk> Parse(string line)
    {
        var json = line[6..];

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("p", out var p) &&
            p.GetString() == "response/search_status")
        {
            yield return new DeepSeekChunk
            {
                Type = DeepSeekChunkType.SearchStatus,
                Text = root.GetProperty("v").GetString()
            };
            yield break;
        }

        if (root.TryGetProperty("p", out var p2) &&
            p2.GetString() == "response/search_results")
        {
            var list = new List<SearchResult>();

            foreach (var item in root.GetProperty("v").EnumerateArray())
            {
                list.Add(new SearchResult
                {
                    Url = item.GetProperty("url").GetString() ?? "",
                    Title = item.GetProperty("title").GetString() ?? "",
                    Snippet = item.GetProperty("snippet").GetString() ?? "",
                    SiteName = item.GetProperty("site_name").GetString() ?? ""
                });
            }

            yield return new DeepSeekChunk
            {
                Type = DeepSeekChunkType.SearchResults,
                SearchResults = list,
                Raw = root
            };
            yield break;
        }

        if (root.TryGetProperty("p", out var p3) &&
            p3.GetString() == "response/content")
        {
            var op = root.TryGetProperty("o", out var o)
                ? o.GetString()
                : null;

            yield return new DeepSeekChunk
            {
                Type = DeepSeekChunkType.Text,
                Text = root.GetProperty("v").GetString(),
                Path = p3.GetString(),
                Operation = op
            };
            yield break;
        }

        if (root.TryGetProperty("v", out var v))
        {
            if (v.ValueKind == JsonValueKind.String)
            {
                yield return new DeepSeekChunk
                {
                    Type = DeepSeekChunkType.Text,
                    Text = v.GetString()
                };
                yield break;
            }

            // Колхоз
            if (v.ValueKind == JsonValueKind.Object &&
                v.TryGetProperty("response", out var response) &&
                response.TryGetProperty("content", out var content))
            {
                yield return new DeepSeekChunk
                {
                    Type = DeepSeekChunkType.Text,
                    Text = content.GetString()
                };
                yield break;
            }

            yield return new DeepSeekChunk
            {
                Type = DeepSeekChunkType.State,
                Raw = root
            };
            yield break;
        }

        yield return new DeepSeekChunk
        {
            Type = DeepSeekChunkType.Unknown,
            Raw = root
        };
    }
}