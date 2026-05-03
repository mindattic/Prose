using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace StreetSamurai.Core.Services;

public class OllamaOptions
{
    public string BaseUrl    { get; set; } = "http://localhost:11434";
    public string ChatModel  { get; set; } = "qwen3:14b";
    public string EmbedModel { get; set; } = "bge-m3";

    /// <summary>
    /// Context window in tokens. Default 8192 — fits 8 retrieved chunks + system + question
    /// without truncation while staying inside a 3080 Ti's 12GB VRAM (model ~9GB + KV ~2GB).
    /// Bumping to 16384 spills to CPU on a 3080 Ti, dropping throughput ~6×.
    /// </summary>
    public int NumCtx { get; set; } = 8192;

    /// <summary>
    /// How long Ollama keeps the model loaded after the last request. Default "30m".
    /// "-1" pins the model in VRAM forever; "0" unloads immediately.
    /// </summary>
    public string KeepAlive { get; set; } = "30m";
}

/// <summary>
/// Thin wrapper for a local Ollama server. Used by EmbeddingIndexService for
/// embeddings (/api/embed) and by /ask for streaming chat (/api/chat).
/// Non-streaming voting calls go through Legion's OpenAI-compatible path
/// against Ollama's /v1/chat/completions endpoint.
/// </summary>
public class OllamaClient
{
    private readonly HttpClient http;
    private readonly OllamaOptions opts;

    public OllamaClient(HttpClient http, OllamaOptions opts)
    {
        this.http = http;
        this.opts = opts;
    }

    public string EmbedModel => opts.EmbedModel;
    public string ChatModel  => opts.ChatModel;
    public string BaseUrl    => opts.BaseUrl;

    public async Task<bool> IsReachableAsync(CancellationToken ct = default)
    {
        try
        {
            using var res = await http.GetAsync("/api/tags", ct);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> inputs, CancellationToken ct = default)
    {
        if (inputs.Count == 0) return Array.Empty<float[]>();
        var payload = new { model = opts.EmbedModel, input = inputs, keep_alive = opts.KeepAlive };
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/embed")
        {
            Content = JsonContent.Create(payload),
        };
        using var res = await http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement.GetProperty("embeddings");
        var result = new float[arr.GetArrayLength()][];
        int i = 0;
        foreach (var vec in arr.EnumerateArray())
        {
            var v = new float[vec.GetArrayLength()];
            int j = 0;
            foreach (var el in vec.EnumerateArray())
                v[j++] = el.GetSingle();
            result[i++] = v;
        }
        return result;
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        IEnumerable<(string Role, string Content)> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var payload = new
        {
            model = opts.ChatModel,
            stream = true,
            keep_alive = opts.KeepAlive,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            options = new { num_ctx = opts.NumCtx },
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(payload),
        };
        using var res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        res.EnsureSuccessStatusCode();
        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (true)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;
            string? piece = null;
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("message", out var msg)
                    && msg.TryGetProperty("content", out var content)
                    && content.ValueKind == JsonValueKind.String)
                {
                    piece = content.GetString();
                }
            }
            catch (JsonException) { continue; }
            if (!string.IsNullOrEmpty(piece))
                yield return piece;
        }
    }
}
