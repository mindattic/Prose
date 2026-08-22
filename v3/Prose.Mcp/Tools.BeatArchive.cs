using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Beat Context Archive (observability plan Part F5, 2026-08-21) ─────────────
// The durable, per-beat answer to "what actually fed this beat": prose, per-service trace,
// the full LLM prompt/response, the entity roster resolved to that exact moment's canon
// (psychology/speech fields included), the DCM doc list resolved to that moment's content,
// and the bible section active then. All assembly logic lives in BeatArchiveService — this
// class only forwards to the Hub and serializes the result, same shape as every other tool.

[McpServerToolType]
public class BeatArchiveTools
{
    private readonly BeatArchiveService archiveService;
    private readonly HubInvoker hub;

    public BeatArchiveTools(BeatArchiveService archiveService, HubInvoker hub)
    {
        this.archiveService = archiveService;
        this.hub = hub;
    }

    [McpServerTool, Description(
        "Get the Beat Context Archive for one beat — everything that fed it, resolved as of " +
        "that beat's own trace timestamp: prose, per-service coverage trace, the full LLM " +
        "system/user prompt and response, the entity roster resolved to that moment's canon " +
        "(including psychology/speech fields), the DCM doc list resolved to that moment's " +
        "content, and the bible section active then. Use this to audit exactly what the prose " +
        "engine saw and did for a specific beat, after the fact.")]
    public Task<string> GetBeatArchive(
        [Description("The beat's Guid id.")] string beatId) =>
        hub.InvokeAsync(nameof(BeatArchiveTools), nameof(GetBeatArchiveImpl), new { beatId });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> GetBeatArchiveImpl(string beatId)
    {
        if (!Guid.TryParse(beatId, out var id))
            return JsonSerializer.Serialize(new { error = "invalid_beat_id", beatId });

        var archive = await archiveService.BuildArchiveAsync(id);
        return archive == null
            ? JsonSerializer.Serialize(new { error = "beat_not_found", beatId })
            : JsonSerializer.Serialize(archive, JsonOpts);
    }

    // camelCase over the wire — matches every other UI-facing tool's convention
    // (see e.g. NodeTools.ReadBeatsImpl's hand-written lowercase anonymous type).
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}
