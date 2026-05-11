using APIEngine;
using APIEngine.Exceptions;
using DeepSeekAPI.Exceptions;
using DeepSeekAPI.Models;
using DeepSeekAPI.Models.Chat;
using DeepSeekAPI.PoW;
using DeepSeekAPI.Streaming;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DeepSeekAPI;

public class DeepSeekClient : HttpApiClient
{
    private readonly string _authToken;
    private readonly DeepSeekPOW _deepSeekPow;
    private readonly DeepSeekChunkParser _chunkParser;

    private string? _pow;

    public DeepSeekClient(string authToken, HttpClient httpClient)
    : base(httpClient, "https://chat.deepseek.com/api/v0")
    {
        _authToken = authToken;

        if (string.IsNullOrWhiteSpace(authToken))
        {
            throw new AuthenticationError("Invalid auth token");
        }

        var bytes = ResourcesDataLoader.GetDataBytes(
            "wasm.sha3_wasm_bg.7b9ca65ddd.wasm")
            ?? throw new InvalidOperationException("Failed to load WASM module");

        _deepSeekPow = new DeepSeekPOW(bytes);
        _chunkParser = new DeepSeekChunkParser();
    }

    public async Task<UserProfile> GetUserProfileAsync()
    {
        var response = await GetAsync("/users/current");

        using var json = JsonDocument.Parse(response);

        var id = json.RootElement.GetByPathOrThrow("data.biz_data.id").GetString()!;
        var email = json.RootElement.GetProperty("data.biz_data.email").GetString()!;
        var mobileNumber = json.RootElement.GetProperty("data.biz_data.mobile_number").GetString()!;

        return new UserProfile(id, email, mobileNumber);
    }

    public async Task<List<ChatSession>> GetChatSessionsAsync(double? updateAt = null)
    {
        var query = QueryParametersBuilder.Create()
            .AddParameter("lte_cursor.pinned", false)
            .AddParameterIf(updateAt.HasValue, "lte_cursor.updated_at", updateAt.ToString()!.Replace(",", "."))
            .Build();

        var endpoint = $"/chat_session/fetch_page{query}";
        var response = await GetAsync(endpoint);

        using var json = JsonDocument.Parse(response);

        var result = new List<ChatSession>();

        var items = json.RootElement
            .GetProperty("data")
            .GetProperty("biz_data")
            .GetProperty("chat_sessions");

        foreach (var item in items.EnumerateArray())
        {
            var id = item.GetProperty("id").GetString()!;
            var title = item.GetProperty("title").GetString()!;
            var titleType = item.GetProperty("title_type").GetString()!;
            var pinned = item.GetProperty("pinned").GetBoolean()!;
            var modelType = item.GetProperty("model_type").GetString()! == "default" ?
                ModelType.Default : ModelType.Expert; ;
            var updatedAt = item.GetProperty("updated_at").GetDouble()!;

            var session = new ChatSession(id, title, titleType, pinned, modelType, updatedAt);

            result.Add(session);
        }

        return result;
    }

    public async Task<ChatSession> CreateChatSessionAsync()
    {
        var response = await PostAsync("/chat_session/create", new { character_id = (string?)null });

        using var json = JsonDocument.Parse(response);

        var id = json.RootElement
            .GetByPathOrThrow("data.biz_data.chat_session.id")
            .GetString()!;

        return new ChatSession(id);
    }

    public async Task<string> UploadFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var powChallenge = await GetPowChallenge("/api/v0/file/upload_file");
        _pow = _deepSeekPow.SolveChallenge(powChallenge);

        using var request = CreateRequest(HttpMethod.Post, "file/upload_file");
        await ConfigureRequestAsync(request);

        var fileInfo = new FileInfo(filePath);
        var fileName = fileInfo.Name;
        var stream = File.OpenRead(filePath);
        var fileContent = new StreamContent(stream);

        var content = new MultipartFormDataContent();

        var encodedFileName = Uri.EscapeDataString(fileName);
        var contentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = encodedFileName,
            FileName = encodedFileName
        };
        fileContent.Headers.ContentDisposition = contentDisposition;

        content.Add(fileContent);
        request.Content = content;

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new APIError(text, (int)response.StatusCode);
        }

        using var json = JsonDocument.Parse(text);
        var id = json.RootElement.GetByPathOrThrow("data.biz_data.id").GetString()!;

        return id;
    }

    #region Отправка сообщения

    public async Task<string> SendMessageAsync(
        ChatSession chatSession,
        string prompt,
        ChatSettings chatSettings,
        long? parentMessageId = null)
    {
        var response = new StringBuilder();

        await foreach (var token in SendMessageStream(
            chatSession,
            prompt,
            chatSettings,
            parentMessageId))
        {
            response.Append(token.Text);
        }

        return response.ToString();
    }

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
        var powChallenge = await GetPowChallenge("/api/v0/chat/completion");
        _pow = _deepSeekPow.SolveChallenge(powChallenge);

        var body = new
        {
            chat_session_id = chatSession.Id,
            parent_message_id = parentMessageId,
            model_type = chatSettings.ModelType.ToString().ToLower(),
            prompt,
            ref_file_ids = new string[0],
            thinking_enabled = chatSettings.Thinking,
            search_enabled = chatSettings.Search
        };

        var result = await PostRawAsync("/chat/completion", body);

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

            throw reason switch
            {
                "rate_limit_reached" => new RateLimitError(content ?? ""),
                _ => new APIError(content ?? "Unknown API error", 0),
            };
        }
        catch (JsonException) { }
    }

    #endregion

    private async Task<PowRequest> GetPowChallenge(string targetpath)
    {
        var request = new
        {
            target_path = targetpath
        };

        var response = await PostAsync("/chat/create_pow_challenge", request);

        var json = JsonDocument.Parse(response);
        var challenge = json.RootElement
            .GetProperty("data")
            .GetProperty("biz_data")
            .GetProperty("challenge");

        return JsonSerializer.Deserialize<PowRequest>(challenge.GetRawText())!;
    }

    protected override Task ConfigureRequestAsync(HttpRequestMessage request)
    {
        request.Headers.Add("authorization", $"{_authToken}");
        request.Headers.Add("x-client-platform", "web");
        request.Headers.Add("x-client-version", "2.0.0");

        if (_pow != null)
        {
            request.Headers.Add("x-ds-pow-response", _pow);
            _pow = null;
        }

        return Task.CompletedTask;
    }

    private static string GetMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        return ext switch
        {
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".csv" => "text/csv",
            _ => "application/octet-stream"
        };
    }
}