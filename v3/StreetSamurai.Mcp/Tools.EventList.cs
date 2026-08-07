using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Per-beat plot-event list — "what happened", not why it matters ────────
// Distinct from Beat.Description (authorial-intent register, filled by backfill_meaning).
// generate_event_list fills Beat.EventSummary, hash-gated so unchanged beats cost nothing on
// re-run; get_event_list is the fast, in-session, no-LLM-call read path. Deliberately kept
// out of the node bible / DCM prose-generation context — this is a human-readable QA
// artifact for checking a book's plot flow, not a story-generation input.

[McpServerToolType]
public class BeatEventListTools
{
    private readonly BeatEventSummaryService eventSummaries;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    public BeatEventListTools(BeatEventSummaryService eventSummaries, IDbContextFactory<ProseDbContext> dbFactory)
    {
        this.eventSummaries = eventSummaries;
        this.dbFactory = dbFactory;
    }

    private async Task<string?> ResolveSlugAsync(string nodeIdOrSlug)
    {
        if (!Guid.TryParse(nodeIdOrSlug, out var gid)) return nodeIdOrSlug;
        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == gid);
        return node?.Slug;
    }

    [McpServerTool, Description(
        "Generate/refresh the per-beat plot-event list (Beat.EventSummary) for a node — terse, " +
        "present-tense, name-anchored 'what happened' lines (e.g. 'Thieves steal Relic.'), hash-gated " +
        "so unchanged beats cost nothing on re-run. Distinct from Description (authorial-intent " +
        "register — 'why this beat exists'). Accepts node id (GUID) or slug. force=true regenerates " +
        "every beat's line regardless of cache.")]
    public async Task<string> generate_event_list(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug,
        [Description("Regenerate every beat's line even if its TextHash hasn't changed.")] bool force = false)
    {
        var slug = await ResolveSlugAsync(nodeIdOrSlug);
        if (slug == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);

        var r = await eventSummaries.GenerateAsync(slug, force: force);
        return JsonSerializer.Serialize(new
        {
            node_code = r.NodeCode, candidates = r.Candidates, generated = r.Generated,
            failed = r.Failed, skipped_from_cache = r.SkippedFromCache,
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Return the current per-beat plot-event list for a node as ordered structured data — one " +
        "entry per enabled beat with its SortKey, title, POV, and EventSummary line. Reads DB state " +
        "only, no LLM call, no disk write — the fast, in-session way to read a whole book's plot " +
        "flow without opening the exported {CODE}-Events.txt or reading the raw prose. Accepts node id " +
        "(GUID) or slug.")]
    public async Task<string> get_event_list(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug)
    {
        var slug = await ResolveSlugAsync(nodeIdOrSlug);
        if (slug == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);

        var (nodeCode, title, entries) = await eventSummaries.GetEventListAsync(slug);
        return JsonSerializer.Serialize(new
        {
            node_code = nodeCode, title, count = entries.Count,
            entries = entries.Select(e => new { sk = e.SortKey, title = e.Title, pov = e.Pov, @event = e.EventSummary }),
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Export the current per-beat plot-event list for a node to {CODE}-Events.txt in the node's " +
        "publish-export folder (same layout as description.txt / {CODE}-dcm-viz.htm — not docs/nodes; " +
        "deliberately .txt, not .md, so it's never picked up by sync_markdown_files / DCM). No LLM " +
        "call — reads current DB state only.")]
    public async Task<string> export_event_list(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug)
    {
        var slug = await ResolveSlugAsync(nodeIdOrSlug);
        if (slug == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);

        var path = await eventSummaries.ExportTxtAsync(slug);
        return JsonSerializer.Serialize(new { ok = true, path }, CanonTools.JsonOpts);
    }
}
