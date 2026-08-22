using System.Net.Http.Json;
using System.Text.Json;
using Prose.Hub.Contracts;

namespace Prose.ObserverUi;

/// <summary>
/// Plain REST calls against Prose.Hub — the Phase 4 dedicated endpoints
/// (<c>/api/logs/recent</c>, <c>/api/dcm/runs</c>) plus the generic MCP dispatch
/// (<c>/api/mcp-invoke</c>) for everything that already exists as a tool (Beats,
/// Repositories, the Command/Decision Ledger reads) — the same execution surface Claude
/// Code and Prose.Cli use, not a parallel read path. Registered alongside
/// <see cref="HubApiClient"/> by <c>AddProseObserverUi</c>.
/// </summary>
public sealed class ObserverHttpClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<List<LogLineDto>> GetRecentLogsAsync(int take = 200) =>
        await http.GetFromJsonAsync<List<LogLineDto>>($"api/logs/recent?take={take}") ?? [];

    public async Task<List<DcmRunDto>> GetDcmRunsAsync(int take = 50) =>
        await http.GetFromJsonAsync<List<DcmRunDto>>($"api/dcm/runs?take={take}") ?? [];

    public async Task<List<DcmBeatDto>> GetDcmRunBeatsAsync(Guid runId) =>
        await http.GetFromJsonAsync<List<DcmBeatDto>>($"api/dcm/runs/{runId}/beats") ?? [];

    /// <summary>Raw JSON — the same <c>DcmVisualizationService.VizPayload</c> shape the live
    /// SignalR push sends, handed straight to the JS renderer unchanged. Null if the run
    /// doesn't exist (404).</summary>
    public async Task<string?> GetDcmRunPayloadAsync(Guid runId)
    {
        var resp = await http.GetAsync($"api/dcm/runs/{runId}/payload");
        return resp.IsSuccessStatusCode ? await resp.Content.ReadAsStringAsync() : null;
    }

    /// <summary>Durable log search (Serilog daily files, not the live tail) — Logs tab's
    /// History mode. <c>since</c> is an ISO-8601 string or null (defaults to 1 day ago).</summary>
    public Task<List<LogSearchResultDto>> SearchLogsAsync(string? since, string? severity, string? text, int take = 200) =>
        InvokeMcpAsync<List<LogSearchResultDto>>("LedgerTools", "SearchLogs", new { since, severity, text, take });

    public Task<List<CommandLedgerDto>> GetCommandLogAsync(int take = 20) =>
        InvokeMcpAsync<List<CommandLedgerDto>>("LedgerTools", "CommandLog", new { take });

    public Task<List<DecisionLedgerDto>> GetDecisionLogAsync(int take = 20) =>
        InvokeMcpAsync<List<DecisionLedgerDto>>("LedgerTools", "DecisionLog", new { take });

    /// <summary>Null on failure (node not found, etc.) rather than throwing — callers show
    /// a plain "not found" message instead of a crash.</summary>
    public async Task<ReadBeatsResultDto?> ReadBeatsAsync(string idOrSlug, int? from, int? to)
    {
        var raw = await InvokeMcpRawAsync("NodeTools", "ReadBeats", new { idOrSlug, from, to });
        return TryDeserialize<ReadBeatsResultDto>(raw, requiredProperty: "total");
    }

    public async Task<List<RepositoryTypeCountDto>> ListRepositoryTypesAsync()
    {
        var raw = await InvokeMcpRawAsync("RepositoryTools", "BrowseRepository", new { });
        return TryDeserialize<List<RepositoryTypeCountDto>>(raw) ?? [];
    }

    public async Task<BrowseRepositoryResultDto?> BrowseRepositoryAsync(string type, string? search, int page, int pageSize)
    {
        var raw = await InvokeMcpRawAsync("RepositoryTools", "BrowseRepository", new { type, search, page, pageSize });
        return TryDeserialize<BrowseRepositoryResultDto>(raw, requiredProperty: "total");
    }

    /// <summary>Null on failure (bad/unknown beat id) rather than throwing.</summary>
    public async Task<BeatArchiveDto?> GetBeatArchiveAsync(string beatId)
    {
        var raw = await InvokeMcpRawAsync("BeatArchiveTools", "GetBeatArchive", new { beatId });
        return TryDeserialize<BeatArchiveDto>(raw, requiredProperty: "beat");
    }

    // ── Generic MCP dispatch plumbing ────────────────────────────────────────

    private async Task<T> InvokeMcpAsync<T>(string toolClass, string method, object? args)
    {
        var raw = await InvokeMcpRawAsync(toolClass, method, args);
        return JsonSerializer.Deserialize<T>(raw, JsonOpts) ?? throw new InvalidOperationException($"{toolClass}.{method} returned no data.");
    }

    private async Task<string> InvokeMcpRawAsync(string toolClass, string method, object? args)
    {
        var resp = await http.PostAsJsonAsync("api/mcp-invoke", new { toolClass, method, args });
        return await resp.Content.ReadAsStringAsync();
    }

    /// <summary>Deserializes only if the JSON doesn't look like an error payload (no "error"
    /// field, and — when given — the expected success field IS present). Returns null rather
    /// than throwing on a tool-reported failure (e.g. node_not_found).</summary>
    private static T? TryDeserialize<T>(string raw, string? requiredProperty = null)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("error", out _)) return default;
                if (requiredProperty != null && !doc.RootElement.TryGetProperty(requiredProperty, out _)) return default;
            }
            return JsonSerializer.Deserialize<T>(raw, JsonOpts);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
