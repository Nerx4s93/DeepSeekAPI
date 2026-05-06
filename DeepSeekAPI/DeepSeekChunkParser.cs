using DeepSeekAPI.Streaming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace DeepSeekAPI;

public class DeepSeekChunkParser
{
    public DeepSeekEvent? Parse(string line)
    {
        if (!line.StartsWith("data: "))
        {
            return null;
        }    

        var json = line.Substring(6);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("p", out var p))
        {
            var path = p.GetString();
            var op = root.TryGetProperty("o", out var o)? o.GetString() : null;
            var value = root.TryGetProperty("v", out var v) ? v.ToString() : "";
            return new PatchEvent(path, op, value);
        }

        if (root.TryGetProperty("v", out var vOnly))
        {
            return ParseV(vOnly);
        }

        return ParseMeta(root);
    }

    private DeepSeekEvent? ParseV(JsonElement v)
    {
        if (v.ValueKind == JsonValueKind.String)
        {
            return new TextEvent(v.GetString()!);
        }

        if (v.ValueKind == JsonValueKind.Object)
        {
            if (v.TryGetProperty("response", out var response))
            {
                var firstContent = response.GetProperty("fragments")
                        .EnumerateArray()
                        .FirstOrDefault()
                        .GetProperty("content")
                        .GetString() ?? "";

                return new MessageInitEvent(
                    response.GetProperty("message_id").GetInt64(),
                    response.GetProperty("parent_id").GetInt64(),
                    response.GetProperty("role").GetString() ?? "",
                    response.GetProperty("thinking_enabled").GetBoolean(),
                    response.GetProperty("search_enabled").GetBoolean(),
                    response.GetProperty("status").GetString() ?? "",
                    firstContent
                );
            }
        }

        return null;
    }

    private DeepSeekEvent? ParseMeta(JsonElement root)
    {
        if (root.TryGetProperty("p", out var p))
        {
            var path = p.GetString();

            if (path == "response/status")
            {
                var status = root.GetProperty("v").GetString();
                return new StatusEvent(status ?? "");
            }

            if (path == "response/fragments/-1/content")
            {
                return new PatchEvent(
                    path,
                    root.GetProperty("o").GetString(),
                    root.GetProperty("v").ToString()
                );
            }

            if (path == "update_session")
            {
                return new MetaEvent("update_session", root.GetProperty("v").ToString());
            }
        }

        return new MetaEvent("unknown", root.ToString());
    }
}