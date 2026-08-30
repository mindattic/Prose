using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Book Health tool ────────────────────────────────────────────────────────
// The single "does this book work" battery + score — the MCP mirror of
// `prose --audit-book`. Both wrap Prose.Core.Services.BookHealthService directly
// (no CLI dependency), so the battery and the SII arithmetic live in exactly one
// place. See BookHealthService.cs remarks for the full design rationale.

[McpServerToolType]
public class BookHealthTools(
    BookHealthService bookHealth,
    IDbContextFactory<ProseDbContext> dbFactory,
    SettingsService settings,
    HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    /// <summary>Run the full book-health battery and return one Structural Integrity Index
    /// (SII, 0-100) built from a fixed, documented formula over open Findings plus a small
    /// number of deterministic rate metrics — NOT an LLM opinion vote (SS-A44). Includes the
    /// full category-by-severity breakdown and which checks ran underneath the headline
    /// number, so a client can never quote the score without also seeing what it's built from.</summary>
    [McpServerTool, Description(
        "Run the full book-health battery and return one Structural Integrity Index (SII, 0-100) " +
        "built from a fixed, documented formula over open Findings + a small number of deterministic " +
        "rate metrics (Swain scene/sequel compliance, CraftChecklist DELIGHT-landing rate, StoryScope " +
        "readiness) — NOT an LLM opinion vote (SS-A44). Every point of the score traces to a specific " +
        "Findings category or rate metric in the response; there is no bare number. " +
        "tier=free (default) runs only deterministic/near-zero-cost checks (plant-audit, prose-check, " +
        "noun-consistency, timeline-check, beat-verification, outline-coordination). " +
        "tier=deep adds one-LLM-call-per-check whole-node audits (examine-emotion, book-audit, " +
        "diagnose-book, check-fidelity, logic-sweep, craft-checklist, check-canon, altitude-audit, " +
        "reader-qa comprehension). tier=full adds the heaviest multi-call audits (storyscope-audit, " +
        "swain-audit, chekhov-audit) — cost scales with book length. The SII itself is always computed " +
        "from whatever is currently in the Findings table regardless of tier — a free-tier run still " +
        "reflects a prior full-tier run's findings, it just won't refresh them.")]
    public Task<string> book_health(
        [Description("Node id (GUID) or slug — a book or a lone chapter.")] string nodeIdOrSlug,
        [Description("free | deep | full")] string tier = "free",
        [Description("Optional model override for the deep/full tier's LLM calls.")] string? model = null) =>
        hub.InvokeAsync(nameof(BookHealthTools), nameof(book_healthImpl), new { nodeIdOrSlug, tier, model });

    public async Task<string> book_healthImpl(
        string nodeIdOrSlug,
        string tier = "free",
        string? model = null)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        if (!Enum.TryParse<BookHealthTier>(tier, ignoreCase: true, out var parsedTier))
            return JsonSerializer.Serialize(new { error = "invalid_tier", tier, valid = new[] { "free", "deep", "full" } }, JsonOpts);

        string? savedModel = null;
        if (model != null) { savedModel = settings.Model; settings.Model = model; }
        try
        {
            var report = await bookHealth.RunAsync(nodeId.Value, parsedTier);
            return JsonSerializer.Serialize(new
            {
                node_id    = report.NodeId,
                node_slug  = report.Slug,
                node_title = report.Title,
                tier_run   = report.Tier,
                generated_at = report.GeneratedAt,
                sii          = report.Sii,
                grade        = report.Grade,
                checks = report.Checks.Select(c => new { name = c.Name, success = c.Success, note = c.Note }),
                findings_breakdown = report.FindingsDeduction.Select(d => new
                {
                    category = d.Category, high = d.High, medium = d.Medium, low = d.Low,
                    raw_points = d.RawPoints, capped_points = d.CappedPoints,
                }),
                rate_adjustments = report.RateAdjustments.Select(r => new
                {
                    metric = r.Metric, value = r.Value, adjustment = r.Adjustment,
                }),
                excluded_from_score = report.ExcludedFromScore,
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message, nodeIdOrSlug }, JsonOpts);
        }
        finally
        {
            if (savedModel != null) settings.Model = savedModel;
        }
    }

    /// <summary>docs/LOGIC.md §9's five-point publish-readiness convergence gate as one answer
    /// (2026-08-30) — see BookHealthService.PublishReadinessAsync and its CLI mirror
    /// PublishReadinessCli.cs. Read-only, no LLM calls.</summary>
    [McpServerTool, Description(
        "docs/LOGIC.md §9's five-point publish-readiness convergence gate, computed as one answer: " +
        "(1) zero open BLOCKER/MODERATE logic-sweep findings, (2) zero open CONTRADICTED fact-ledger " +
        "claims, (3) two consecutive dry logic-sweep rounds against the book's current text, " +
        "(4) blast-radius recheck clean on every beat, (5) zero open High/BLOCKER Reader-Proxy QA " +
        "findings. Read-only — makes no LLM calls and runs no new checks, only reads what earlier " +
        "sweep/audit/ledger runs already filed or persisted.")]
    public Task<string> publish_readiness(
        [Description("Node id (GUID) or slug — a book or a lone chapter.")] string nodeIdOrSlug) =>
        hub.InvokeAsync(nameof(BookHealthTools), nameof(publish_readinessImpl), new { nodeIdOrSlug });

    public async Task<string> publish_readinessImpl(string nodeIdOrSlug)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        var report = await bookHealth.PublishReadinessAsync(nodeId.Value);
        return JsonSerializer.Serialize(new
        {
            node_id = report.NodeId, node_slug = report.Slug, ready = report.Ready,
            checks = report.Checks.Select(c => new { name = c.Name, pass = c.Pass, detail = c.Detail }),
        }, JsonOpts);
    }

    /// <summary>
    /// 2026-08-24 consolidation — see the note on <c>BookAuditTools.ResolveNodeAsync</c>. This
    /// copy had no <c>IgnoreQueryFilters()</c> on either branch, so <c>book_health</c> could not
    /// reach any book outside the ambient universe by slug. Delegates to
    /// <see cref="NodeRefResolver"/>.
    /// </summary>
    Task<Guid?> ResolveNodeAsync(string idOrSlug) =>
        NodeRefResolver.ResolveAsync(dbFactory, idOrSlug);
}
