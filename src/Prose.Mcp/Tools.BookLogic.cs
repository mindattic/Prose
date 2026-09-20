using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.Audit;

namespace Prose.Mcp;

// ── Narrative Synopsis + Logic Sweep tools ─────────────────────────────────────
//
//   write_synopsis — generate a beat-by-beat narrative synopsis (act-grouped) FROM
//                    the written prose (renamed from write_outline 2026-08-29;
//                    "outline" now names the per-book pre-writing plan).
//   logic_sweep    — docs/LOGIC.md's six-dimension sweep (SS-A44) as a single-pass
//                    LLM-per-dimension check. For a large book or a thorough pass,
//                    prefer the /logic-sweep Claude Code skill instead (range-scoped
//                    subagents + quote verification + fix + re-verify).

[McpServerToolType]
public class BookLogicTools(
    NarrativeSynopsisService synopsisService,
    LogicSweepService logicSweepService,
    IDbContextFactory<ProseDbContext> dbFactory,
    HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    [McpServerTool, Description("Generate a beat-by-beat narrative synopsis (act-grouped) of a node's written prose. " +
        "For a real logic check (causality/knowledge-states/timeline/plant-payoff/orphan-refs/outline-agreement), " +
        "call logic_sweep instead. Accepts node id (GUID) or slug.")]
    public Task<string> write_synopsis(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug) =>
        hub.InvokeAsync(nameof(BookLogicTools), nameof(write_synopsisImpl), new { nodeIdOrSlug });

    public async Task<string> write_synopsisImpl(string nodeIdOrSlug)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        try
        {
            var result = await synopsisService.GenerateAsync(nodeId.Value);
            return JsonSerializer.Serialize(new
            {
                node_id    = result.NodeId,
                title      = result.Title,
                beat_count = result.BeatCount,
                synopsis   = result.Synopsis,
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
        }
    }

    [McpServerTool, Description("Run docs/LOGIC.md's six-dimension logic sweep on a node: causality chain, " +
        "knowledge states, timeline, plant/payoff (two-way), orphan references, bible agreement. " +
        "This is a single LLM call per dimension over the whole node's prose — a coarse, automatable gate, " +
        "NOT a replacement for the full /logic-sweep Claude Code skill on a large book (that skill splits " +
        "the book across range-scoped subagents, verifies quotes, and does a separate fix + re-verify pass). " +
        "Findings persist to the Findings table and auto-heal on re-run. Accepts node id (GUID) or slug.")]
    public Task<string> logic_sweep(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug) =>
        hub.InvokeAsync(nameof(BookLogicTools), nameof(logic_sweepImpl), new { nodeIdOrSlug });

    public async Task<string> logic_sweepImpl(string nodeIdOrSlug)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        try
        {
            var report = await logicSweepService.RunAsync(nodeId.Value);
            return JsonSerializer.Serialize(new
            {
                node_id        = report.NodeId,
                title          = report.NodeTitle,
                beat_count     = report.BeatCount,
                clean          = report.Clean,
                blocker_count  = report.BlockerCount,
                moderate_count = report.ModerateCount,
                minor_count    = report.MinorCount,
                findings       = report.Findings.Select(f => new
                {
                    dimension = f.RuleKey,
                    severity  = f.Severity,
                    evidence  = f.Evidence,
                    fix       = f.Fix,
                }),
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
        }
    }

    [McpServerTool, Description("Run ONE round of a loop-until-dry logic-sweep convergence campaign (2026-08-14) — " +
        "replaces 'run the sweep N times regardless of what it found' with an actual convergence criterion: " +
        "stops after 2 consecutive rounds that found nothing new, persisted across sessions in NodeConvergenceStates. " +
        "Call this again after each fix pass. Returns skipped=true (already converged, nothing changed, no LLM call " +
        "made) or converged=true (2 consecutive clean rounds reached) or hit_safety_cap=true (8 rounds without " +
        "converging — filed as its own finding; the book likely needs a structural rewrite, not another fix pass). " +
        "Accepts node id (GUID) or slug.")]
    public Task<string> logic_sweep_until_dry(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug,
        [Description("Consecutive clean rounds required to call it converged. Default 2.")] int requiredDryRounds = LogicSweepService.DefaultRequiredDryRounds,
        [Description("Safety cap on total rounds before escalating 'not converging' as its own finding. Default 8.")] int maxTotalRounds = LogicSweepService.DefaultMaxTotalRounds) =>
        hub.InvokeAsync(nameof(BookLogicTools), nameof(logic_sweep_until_dryImpl), new { nodeIdOrSlug, requiredDryRounds, maxTotalRounds });

    public async Task<string> logic_sweep_until_dryImpl(
        string nodeIdOrSlug,
        int requiredDryRounds = LogicSweepService.DefaultRequiredDryRounds,
        int maxTotalRounds = LogicSweepService.DefaultMaxTotalRounds)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        try
        {
            var round = await logicSweepService.RunConvergenceRoundAsync(nodeId.Value, requiredDryRounds, maxTotalRounds);
            return JsonSerializer.Serialize(new
            {
                node_id                = round.NodeId,
                skipped                = round.Skipped,
                converged              = round.Converged,
                hit_safety_cap         = round.HitSafetyCap,
                consecutive_dry_rounds = round.ConsecutiveDryRounds,
                total_rounds_run       = round.TotalRoundsRun,
                message                = round.Message,
                findings_this_round    = round.Report?.Findings.Select(f => new
                {
                    dimension = f.RuleKey,
                    severity  = f.Severity,
                    evidence  = f.Evidence,
                    fix       = f.Fix,
                }),
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Fixed 2026-08-24 (the 2026-08-17 pass had added <c>IgnoreQueryFilters()</c> to the id branch
    /// only, so <c>logic_sweep</c> / <c>logic_sweep_until_dry</c> returned node_not_found for every
    /// book outside the ambient universe when addressed by slug — including VIGL, whose own sweep
    /// reports repeatedly called <c>--until-dry</c> "unreliable" for this book), then folded into
    /// <see cref="NodeRefResolver"/> the same day with the other eleven copies of this helper.
    /// </summary>
    Task<Guid?> ResolveNodeAsync(string idOrSlug) =>
        NodeRefResolver.ResolveAsync(dbFactory, idOrSlug);
}
