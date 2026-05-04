using DeepSeekAPI.Exceptions;
using DeepSeekAPI.POW;
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

    public DeepSeekClient(string authToken)
    {
        if (string.IsNullOrWhiteSpace(authToken))
        {
            throw new AuthenticationError("Invalid auth token");
        }

        _authToken = authToken;
        _deepSeekPow = new DeepSeekPOW("wasm\\sha3_wasm_bg.7b9ca65ddd.wasm");
        _httpClient = new HttpClient();
    }

    public async Task<string> CreateChatSession()
    {
        var res = await PostAsync("/chat_session/create", new { character_id = (string?)null });

        var json = JsonDocument.Parse(res);

        return json.RootElement
            .GetProperty("data")
            .GetProperty("biz_data")
            .GetProperty("id")
            .GetString()!;
    }

    public async IAsyncEnumerable<DeepSeekChunk> ChatCompletion(
        string sessionId,
        string prompt,
        string? parentMessageId = null,
        bool thinking = true,
        bool search = false)
    {
        var pow = _deepSeekPow.SolveChallenge(GetPowChallenge("/api/v0/chat/completion"));

        var body = new
        {
            chat_session_id = sessionId,
            parent_message_id = parentMessageId,
            prompt,
            ref_file_ids = new string[0],
            thinking_enabled = thinking,
            search_enabled = search
        };

        var request = CreateRequest(HttpMethod.Post, BaseUrl + "/chat/completion", body, pow);
        var result = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!result.IsSuccessStatusCode)
        {
            throw new APIError("Request failed", (int)result.StatusCode);
        }

        using var stream = await result.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!line.StartsWith("data: "))
            {
                continue;
            }

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
                continue;
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
                continue;
            }

            if (root.TryGetProperty("p", out var p3) &&
                p3.GetString() == "response/content")
            {
                var op = root.TryGetProperty("o", out var o)
                    ? o.GetString()
                    : null;

                var text = root.GetProperty("v").GetString();

                yield return new DeepSeekChunk
                {
                    Type = DeepSeekChunkType.Text,
                    Text = text,
                    Path = p3.GetString(),
                    Operation = op
                };

                continue;
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
                    continue;
                }

                yield return new DeepSeekChunk
                {
                    Type = DeepSeekChunkType.State,
                    Raw = root
                };
            }

            yield return new DeepSeekChunk
            {
                Type = DeepSeekChunkType.Unknown,
                Raw = root
            };
        }
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
        req.Headers.Add("x-client-version", "1.0.0-always");

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