using DeepSeekAPI.Exceptions;
using DeepSeekAPI.Models;
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
    

    public async Task<UserProfile> GetUserProfileAsync()
    {
        var response = await GetAsync("/users/current");

        var json = JsonDocument.Parse(response);

        var id = json.RootElement
            .GetProperty("data")
            .GetProperty("biz_data")
            .GetProperty("id")
            .GetString()!;
        var email = json.RootElement
            .GetProperty("data")
            .GetProperty("biz_data")
            .GetProperty("email")
            .GetString()!;
        var mobileNumber = json.RootElement
            .GetProperty("data")
            .GetProperty("biz_data")
            .GetProperty("mobile_number")
            .GetString()!;

        return new UserProfile(id, email, mobileNumber);
    }
    public async Task<ChatSession> CreateChatSessionAsync()
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

    #region Отправка сообщения

    public async IAsyncEnumerable<StreamToken> SendMessageStream(
        ChatSession chatSession,
        string prompt,
        ChatSettings chatSettings,
        long? parentMessageId = null)
    {
        var messageId = 0L;
        var thinkingAnswerStart = false;
        var thinking = false;

        await foreach (var chunk in ChatCompletion(
            chatSession,
            prompt,
            chatSettings,
            parentMessageId))
        {
            if (chunk is MessageInitEvent messageInitEvent)
            {
                messageId = messageInitEvent.MessageId;
                thinking = messageInitEvent.ThinkingEnabled;

                if (!thinking && !string.IsNullOrEmpty(messageInitEvent.Content))
                {
                    yield return new StreamToken(messageId, messageInitEvent.Content);
                }
            }
            else if (chunk is TextEvent textEvent)
            {
                if (!thinking || thinkingAnswerStart)
                {
                    yield return new StreamToken(messageId, textEvent.Text);
                }
            }
            else if (chunk is PatchEvent patchEvent)
            {
                var token = HandlePatch(patchEvent, thinking, ref thinkingAnswerStart);

                if (token != null)
                {
                    yield return new StreamToken(messageId, token);
                }
            }
        }
    }

    private string? HandlePatch(PatchEvent patchEvent, bool thinking, ref bool thinkingAnswerStart)
    {
        if (patchEvent.Path == "response/fragments/-1/elapsed_secs")
        {
            thinkingAnswerStart = true;
        }

        if (!thinking || thinkingAnswerStart)
        {
            switch (patchEvent.Path)
            {
                case "response/fragments":
                    {
                        var json = patchEvent.Value?.ToString();

                        if (string.IsNullOrEmpty(json))
                        {
                            return null;
                        }

                        using var document = JsonDocument.Parse(json);
                        var stringBuilder = new StringBuilder();

                        var content = document.RootElement[0]
                            .GetProperty("content")
                            .GetString();

                        return content;
                    }

                case "response/fragments/-1/content":
                    return patchEvent.Value?.ToString();
            }
        }

        return null;
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

    #endregion

    private async Task<string> PostAsync(string endpoint, object? body = null)
    {
        var request = CreateRequest(HttpMethod.Post, BaseUrl + endpoint, body);

        var response = await _httpClient.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new APIError(text, (int)response.StatusCode);
        }

        return text;
    }

    private async Task<string> GetAsync(string endpoint, object? body = null)
    {
        var request = CreateRequest(HttpMethod.Get, BaseUrl + endpoint, body);

        var response = await _httpClient.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new APIError(text, (int)response.StatusCode);
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