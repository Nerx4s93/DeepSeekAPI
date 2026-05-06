using DeepSeekAPI.Exceptions;
using DeepSeekAPI.Models.Chat;
using DeepSeekAPI.PoW;
using DeepSeekAPI.Streaming;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeepSeekAPI;

public class DeepSeekClient
{
    private const string BaseUrl = "https://chat.deepseek.com/api/v0";

    private readonly string _authToken;
    private readonly DeepSeekPOW _deepSeekPow;
    private readonly HttpClient _httpClient;
    private readonly DeepSeekChunkParser _chunkParser;

    public DeepSeekClient(string authToken)
    {
        if (string.IsNullOrWhiteSpace(authToken))
        {
            throw new AuthenticationError("Invalid auth token");
        }

        _authToken = authToken;

        var bytes = ResourcesDataLoader.GetDataBytes("wasm.sha3_wasm_bg.7b9ca65ddd.wasm");

        if (bytes == null)
        {
            throw new InvalidOperationException("Failed to load WASM module");
        }

        _deepSeekPow = new DeepSeekPOW(bytes);
        _httpClient = new HttpClient();
        _chunkParser = new DeepSeekChunkParser();
    }

    public async Task<ChatSession> CreateChatSession()
    {
        var response = await PostAsync("/chat_session/create", new { character_id = (string?)null });

        var json = JsonDocument.Parse(response);

        return new ChatSession()
        {
            Id = json.RootElement
            .GetProperty("data")
            .GetProperty("biz_data")
            .GetProperty("chat_session")
            .GetProperty("id")
            .GetString()!
        };
    }

    public async IAsyncEnumerable<DeepSeekEvent> ChatCompletion(
        ChatSession chatSession,
        string prompt,
        ChatSettings chatSettings,
        long? parentMessageId = null)
    {
        var pow = _deepSeekPow.SolveChallenge(GetPowChallenge("/api/v0/chat/completion"));

        var body = new
        {
            chat_session_id = chatSession.Id,
            parent_message_id = parentMessageId,
            model_type = chatSettings.ModelType == ModelType.Default ? null : "expert",
            prompt,
            ref_file_ids = new string[0],
            thinking_enabled = chatSettings.Thinking,
            search_enabled = chatSettings.Search
        };

        var request = CreateRequest(HttpMethod.Post, BaseUrl + "/chat/completion", body, pow);
        var result = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!result.IsSuccessStatusCode)
        {
            throw new APIError("Request failed", (int)result.StatusCode);
        }

        using var stream = await result.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (true)
        {
            var line = await reader.ReadLineAsync();

            if (line is null)
            {
                break;
            }    

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!line.StartsWith("data: "))
            {
                continue;
            }

            var deepSeekEvent = _chunkParser.Parse(line);
            
            if (deepSeekEvent != null)
            {
                GenerateException(deepSeekEvent);
                yield return deepSeekEvent;
            }
        }
    }

    private void GenerateException(DeepSeekEvent deepSeekEvent)
    {
        if (deepSeekEvent is not MetaEvent metaEvent || string.IsNullOrEmpty(metaEvent.Value))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(metaEvent.Value);
            var root = document.RootElement;

            if (!root.TryGetProperty("type", out var typeProp))
            {
                return;
            }

            var type = typeProp.GetString();

            if (type != "error")
            {
                return;
            }

            var content = root.TryGetProperty("content", out var contentProp)
                ? contentProp.GetString()
                : "Unknown error";

            var reason = root.TryGetProperty("finish_reason", out var reasonProp)
                ? reasonProp.GetString()
                : null;

            switch (reason)
            {
                case "rate_limit_reached":
                    throw new RateLimitError(content ?? "");
                default:
                    throw new APIError(content ?? "Unknown API error", 0);
            }
        }
        catch (JsonException) { }
    }

    public async Task<List<DeepSeekEvent>> ChatCompletionAllChunksAsync(
        ChatSession chatSession,
        string prompt,
        ChatSettings chatSettings,
        long? parentMessageId = null)
    {
        var chunks = new List<DeepSeekEvent>();

        await foreach (var chunk in ChatCompletion(
            chatSession,
            prompt,
            chatSettings,
            parentMessageId))
        {
            chunks.Add(chunk);
        }

        return chunks;
    }

    private async Task<string> PostAsync(string endpoint, object body)
    {
        var req = CreateRequest(HttpMethod.Post, BaseUrl + endpoint, body);

        var res = await _httpClient.SendAsync(req);
        var text = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
        {
            throw new APIError(text, (int)res.StatusCode);
        }

        return text;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, object? body = null, string? pow = null)
    {
        var req = new HttpRequestMessage(method, url);

        req.Headers.Add("accept", "*/*");
        req.Headers.Add("authorization", $"Bearer {_authToken}");
        req.Headers.Add("origin", "https://chat.deepseek.com");
        req.Headers.Add("referer", "https://chat.deepseek.com/");
        req.Headers.Add("user-agent", "Mozilla/5.0");
        req.Headers.Add("x-app-version", "20241129.1");
        req.Headers.Add("x-client-locale", "en_US");
        req.Headers.Add("x-client-platform", "web");
        req.Headers.Add("x-client-version", "2.0.0");

        if (pow != null)
        {
            req.Headers.Add("x-ds-pow-response", pow);
        }

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return req;
    }

    private PowRequest GetPowChallenge(string targetpath)
    {
        var request = new
        {
            target_path = targetpath
        };

        var response = PostAsync("/chat/create_pow_challenge", request).GetAwaiter().GetResult();

        var json = JsonDocument.Parse(response);
        var challenge = json.RootElement
            .GetProperty("data")
            .GetProperty("biz_data")
            .GetProperty("challenge");

        return JsonSerializer.Deserialize<PowRequest>(challenge.GetRawText())!;
    }
}