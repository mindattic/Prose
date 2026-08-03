using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Reader-Proxy QA tools (docs/READER-QA.md) ────────────────────────────────
//
//   reader_qa_comprehension — Instrument 1: cheap-model comprehension probes per
//     chapter, diffed against the Sonnet synopsis ground truth, Sonnet-arbitrated,
//     filed as ComprehensionDefect findings. A MEASUREMENT, not a vote — no
//     allowVotes parameter by design (SS-A44 exemption, same as craft_audit /
//     logic_sweep). Emits no scores, ever.

[McpServerToolType]
public class ReaderQaTools(
    ComprehensionProbeService probes,
    IDbContextFactory<StreetSamuraiDbContext> dbFactory)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    [McpServerTool, Description("Reader-Proxy QA comprehension probes: a cheap model reads each chapter cold (rolling recap " +
        "only) and its GENUINE reading is diffed against the fidelity-strict Sonnet synopsis; a Sonnet arbiter confirms which " +
        "mismatches the chapter text itself plausibly supports (reader-plausible confusion vs probe hallucination). Confirmed " +
        "defects are filed as ComprehensionDefect findings (see list_findings) and auto-supersede on re-run. Hash-cached per " +
        "chapter — unchanged chapters never re-bill. Emits NO scores: this is the default reader-facing QA, replacing persona " +
        "score panels. Accepts node id (GUID) or slug.")]
    public async Task<string> reader_qa_comprehension(
        [Description("Book node id (GUID) or slug.")] string nodeIdOrSlug,
        [Description("Re-probe every chapter even if unchanged (default false).")] bool force = false)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        try
        {
            var r = await probes.RunAsync(nodeId.Value, force);
            return JsonSerializer.Serialize(new
            {
                node_id = r.NodeId,
                slug = r.Slug,
                title = r.Title,
                chapters_probed = r.ChaptersProbed,
                chapters_from_cache = r.ChaptersFromCache,
                findings_filed = r.FindingsFiled,
                chapters = r.Chapters.Select(c => new
                {
                    index = c.ChapterIndex,
                    title = c.ChapterTitle,
                    status = c.Status,
                    confusions = c.Confusions,
                    defects = c.Defects
                        .Where(d => d.ReaderPlausible && d.Kind != "hallucination")
                        .Select(d => new { d.Kind, d.Severity, d.Description, d.Evidence }),
                    probe_hallucinations_discarded = c.Defects.Count(d => !d.ReaderPlausible || d.Kind == "hallucination"),
                }),
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
        }
    }

    async Task<Guid?> ResolveNodeAsync(string idOrSlug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (Guid.TryParse(idOrSlug, out var g))
            return (await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == g))?.Id;
        return (await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == idOrSlug || s.NodeCode == idOrSlug))?.Id;
    }
}
