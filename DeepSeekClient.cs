using DeepSeekAPI.Exceptions;
using DeepSeekAPI.POW;
using System.Text;
using System.Text.Json;

namespace DeepSeekAPI;

public class DeepSeekClient
{
    private const string BaseUrl = "https://chat.deepseek.com/api/v0";

    private readonly string _authToken;
    private readonly DeepSeekPOW _deepSeekPow;
    private readonly HttpClient _httpClient;
    private Dictionary<string, string> _cookies;

    public DeepSeekClient(string authToken)
    {
        if (string.IsNullOrWhiteSpace(authToken))
        {
            throw new AuthenticationError("Invalid auth token");
        }

        _authToken = authToken;
        _deepSeekPow = new DeepSeekPOW("wasm\\sha3_wasm_bg.7b9ca65ddd.wasm");
        _httpClient = new HttpClient();

        _cookies = new Dictionary<string, string>();
        LoadCookies();
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

    public async IAsyncEnumerable<string> ChatCompletion(
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

        var req = CreateRequest(HttpMethod.Post, BaseUrl + "/chat/completion", body, pow);

        var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

        if (!res.IsSuccessStatusCode)
        {
            throw new APIError("Request failed", (int)res.StatusCode);
        }

        using var stream = await res.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("data: "))
            {
                var json = line[6..];

                if (json.Contains("finish_reason"))
                {
                    yield break;
                }    

                yield return json;
            }
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

    private void LoadCookies()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "cookies.json");

        if (!File.Exists(path))
        {
            return;
        }

        var json = File.ReadAllText(path);
        var doc = JsonDocument.Parse(json);

        _cookies = doc.RootElement
            .GetProperty("cookies")
            .EnumerateObject()
            .ToDictionary(x => x.Name, x => x.Value.GetString() ?? "");
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