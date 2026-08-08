using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Reader-Proxy QA tools (docs/READER-QA.md) ────────────────────────────────
//
//   reader_qa_comprehension — Instrument 1: cheap-model comprehension probes per
//     chapter, diffed against the Sonnet synopsis ground truth, Sonnet-arbitrated,
//     filed as ComprehensionDefect findings. A MEASUREMENT, not a vote — no
//     allowVotes parameter by design (SS-A44 exemption, same as craft_checklist /
//     logic_sweep). Emits no scores, ever.

[McpServerToolType]
public class ReaderQaTools(
    ComprehensionProbeService probes,
    BeatChecklistGateService checklist,
    GripePassService gripes,
    IDbContextFactory<ProseDbContext> dbFactory)
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

    [McpServerTool, Description("Reader-Proxy QA binary craft/delight checklist per beat, hash-gated on Beat.TextHash + " +
        "rule-set version — unchanged beats never re-bill; editing CRAFT.md §8 or a DELIGHT move re-evaluates the book. " +
        "DON'Ts = CRAFT §8 banned mannerisms (literal binaries); DO = '≥1 applicable DELIGHT move lands' (short connective " +
        "beats exempt); book level = move-monotony counters (DELIGHT §14 — a palette, not a stamp; never 'all 13 per beat'). " +
        "Findings persist as CraftChecklist and auto-supersede per run. Emits NO scores. Accepts node id (GUID) or slug.")]
    public async Task<string> beat_checklist_audit(
        [Description("Book node id (GUID) or slug.")] string nodeIdOrSlug,
        [Description("Re-evaluate every beat even if unchanged (default false).")] bool force = false)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        try
        {
            var r = await checklist.RunAsync(nodeId.Value, force);
            var flagged = r.Beats.Where(b => b.DontViolations.Count > 0).ToList();
            return JsonSerializer.Serialize(new
            {
                node_id = r.NodeId,
                slug = r.Slug,
                title = r.Title,
                beats = r.Beats.Count,
                evaluated = r.Evaluated,
                from_cache = r.FromCache,
                findings_filed = r.FindingsFiled,
                mean_pass_fraction = r.Beats.Count > 0 ? Math.Round(r.Beats.Average(b => b.PassFraction), 4) : 1.0,
                dont_hits = flagged.Select(b => new
                {
                    beat_number = b.BeatNumber,
                    beat_id = b.BeatId,
                    violations = b.DontViolations.Select(d => new { d.Key, d.Title, d.Evidence }),
                }),
                flat_beats = r.Beats.Where(b => b.MovesLanded.Count == 0 && b.WordCount >= 120)
                    .Select(b => new { beat_number = b.BeatNumber, word_count = b.WordCount, job = b.BeatJob }),
                book_level = r.BookLevelFindings,
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
        }
    }

    [McpServerTool, Description("Reader-Proxy QA findings-only gripe jury: a small cross-family jury full-reads the book " +
        "and emits ONLY page-anchored complaints (beat number + verbatim quote + what's wrong) — NO scores, ever. " +
        "Complaints are deduped, quote-grounded deterministically (hallucinated quotes die free), then Sonnet-arbitrated " +
        "against the actual beat text and triaged blocker/moderate/minor. Confirmed gripes persist as ReaderGripe findings " +
        "(see list_findings) and supersede on re-run. Report-only — applying a fix is a separate deliberate action " +
        "(update_beat_text, optionally gated by a duel). Accepts node id (GUID) or slug.")]
    public async Task<string> reader_qa_gripe_pass(
        [Description("Book node id (GUID) or slug.")] string nodeIdOrSlug,
        [Description("Jury size (default 4; one seat per live model family, Claude tiers fill in).")] int readers = 4)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        try
        {
            var r = await gripes.RunAsync(nodeId.Value, readers);
            return JsonSerializer.Serialize(new
            {
                node_id = r.NodeId,
                slug = r.Slug,
                title = r.Title,
                jury = r.ReaderSeats,
                raw_complaints = r.RawComplaints,
                quote_grounding_kills = r.QuoteGroundingKills,
                findings_filed = r.FindingsFiled,
                confirmed = r.Confirmed.Select(g => new
                { g.BeatNumber, g.BeatId, g.Severity, g.Voters, g.Complaint, g.Quote, g.ArbiterRationale }),
                rejected = r.Rejected.Select(g => new { g.BeatNumber, g.Complaint, g.ArbiterRationale }),
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
