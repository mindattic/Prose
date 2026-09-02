using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Prose.Mcp;

/// <summary>
/// Thin HTTP client tools for the Prose Hub (v3/Prose.Hub) — the standalone always-on
/// process holding the resident, shared UniverseGraphService/DocContextStack/
/// EntityContextStack state. Unlike the rest of this MCP server (which holds its own
/// in-process copy of these singletons), these tools read/write the Hub's ONE shared
/// copy directly, so edits made through the Hub (or by another Prose.Mcp/Prose.Cli
/// session hitting the Hub) are visible here without waiting on this process's own
/// staleness probe. Requires the Hub to be running (see .claude/hooks/start-prose-hub.ps1,
/// which auto-starts it on SessionStart) — falls back to a clear error if it isn't.
/// </summary>
[McpServerToolType]
public class HubTools(IHttpClientFactory httpFactory)
{
    private readonly HttpClient http = httpFactory.CreateClient("ProseHub");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    [McpServerTool, Description(
        "Create a relationship edge between two entities via the Prose Hub - the generic edge-creation " +
        "capability missing from RelationshipDiscoveryService's auto-link path (which only covers " +
        "Character/CorpoNation/District/Faction/Weaponry/Equipment/Technology, not e.g. Transportation). " +
        "Writes to the SQL Edges table and updates the Hub's resident graph immediately. relationType is " +
        "normalized (trim/lowercase/underscore) and resolved against the RelationTypeAliases registry " +
        "before writing, so a registered wording (e.g. 'has' -> 'owns') collapses onto the same canonical " +
        "RelationType automatically. The response's 'possibleDuplicate' field, when present, means a live " +
        "edge already exists for this (source, target) pair under a DIFFERENT RelationType wording - check " +
        "it before assuming this call created a new fact: if it's the same relationship reworded, prefer " +
        "reusing the existing edge (or run prose --merge-edge --keep <id> --dedupe <id> --register-alias " +
        "afterward) rather than leaving two edges for one relationship. validFromBeatId/validUntilBeatId " +
        "(2026-09-02) bound this edge's truth to a reading-order span within ONE book (e.g. 'Lyra has the " +
        "Oculus starting at this beat' / 'Kyle no longer has the motorcycle from this beat on') - replaces " +
        "the dead legacy DateTime story-time mechanism. Omit both for an edge that's just always true. To " +
        "close/adjust the window on an edge that ALREADY exists (the common case - most facts start out " +
        "unbounded, then later the story establishes when they end), use prose --set-edge-validity instead " +
        "of calling this again - a repeat call with different bounds does NOT change the existing edge (see " +
        "the response's 'validityNote' when that happens). Requires the Prose Hub to be running on " +
        "http://127.0.0.1:5900.")]
    public async Task<string> LinkEntities(
        [Description("Source entity GUID.")] string source,
        [Description("Target entity GUID.")] string target,
        [Description("Relation type, e.g. 'made_by', 'owns', 'based_in'. Normalized + alias-resolved server-side.")] string relationType,
        [Description("Optional: 'positive' | 'negative' | 'neutral' (default).")] string? sentiment = null,
        [Description("Optional free-text description of the relationship.")] string? description = null,
        [Description("Optional universe slug to scope the write (glmz|scry|gspl). Uses the Hub's default if omitted.")] string? universe = null,
        [Description("Optional beat GUID: this edge is valid starting at this beat (inclusive), within that beat's own book. Omit for valid-from-the-start.")] string? validFromBeatId = null,
        [Description("Optional beat GUID: this edge is valid up to (exclusive of) this beat, within that beat's own book. Omit for valid-to-the-end.")] string? validUntilBeatId = null)
    {
        try
        {
            var resp = await http.PostAsJsonAsync("api/edges", new
            {
                source, target, relationType, sentiment, description, universe = universe,
                validFromBeatId, validUntilBeatId,
            });
            var body = await resp.Content.ReadAsStringAsync();
            return resp.IsSuccessStatusCode ? body : JsonSerializer.Serialize(new { error = "hub_error", status = (int)resp.StatusCode, body }, JsonOpts);
        }
        catch (HttpRequestException ex)
        {
            return JsonSerializer.Serialize(new { error = "hub_unreachable", detail = ex.Message, hint = "Is Prose.Hub running on port 5900?" }, JsonOpts);
        }
    }

    [McpServerTool, Description(
        "Pull a graph snapshot from the Prose Hub for one universe - either the whole graph " +
        "(scope='all') or just what's currently pertinent to ProseWriter right now (scope='active', " +
        "the DocContextStack-driven default). Returns {nodes, edges}. Requires the Prose Hub running " +
        "on http://127.0.0.1:5900.")]
    public async Task<string> GraphSnapshot(
        [Description("Universe slug: glmz | scry | gspl.")] string universe,
        [Description("'active' (default, what's pertinent right now) or 'all' (whole universe).")] string scope = "active",
        [Description("Node CODE (e.g. 'BCODA') to scope the 'active' set to, if applicable.")] string? nodeCode = null)
    {
        try
        {
            var url = $"api/universes/{Uri.EscapeDataString(universe)}/snapshot?scope={Uri.EscapeDataString(scope)}";
            if (!string.IsNullOrWhiteSpace(nodeCode)) url += $"&nodeCode={Uri.EscapeDataString(nodeCode)}";
            var resp = await http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            return resp.IsSuccessStatusCode ? body : JsonSerializer.Serialize(new { error = "hub_error", status = (int)resp.StatusCode, body }, JsonOpts);
        }
        catch (HttpRequestException ex)
        {
            return JsonSerializer.Serialize(new { error = "hub_unreachable", detail = ex.Message, hint = "Is Prose.Hub running on port 5900?" }, JsonOpts);
        }
    }

    [McpServerTool, Description(
        "Get node/edge counts for one universe's resident graph from the Prose Hub. Cheap sanity check " +
        "that the Hub is up and its graph is loaded. Requires the Prose Hub running on http://127.0.0.1:5900.")]
    public async Task<string> GraphStats(
        [Description("Universe slug: glmz | scry | gspl.")] string universe)
    {
        try
        {
            var resp = await http.GetAsync($"api/universes/{Uri.EscapeDataString(universe)}/stats");
            var body = await resp.Content.ReadAsStringAsync();
            return resp.IsSuccessStatusCode ? body : JsonSerializer.Serialize(new { error = "hub_error", status = (int)resp.StatusCode, body }, JsonOpts);
        }
        catch (HttpRequestException ex)
        {
            return JsonSerializer.Serialize(new { error = "hub_unreachable", detail = ex.Message, hint = "Is Prose.Hub running on port 5900?" }, JsonOpts);
        }
    }
}
