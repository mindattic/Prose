using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Swain Scene/Sequel doctrine tools ──────────────────────────────────────────
// Classifies every enabled beat against Dwight Swain's Scene/Sequel doctrine
// (Scene: Goal→Conflict→Disaster; Sequel: Reaction→Dilemma→Decision). Previously
// only reachable via `prose --swain-audit` on the CLI — added here so an MCP-connected
// session can run and repair the audit without shelling out.

[McpServerToolType]
public class SwainTools(SwainAuditService swain, HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    // ── swain_audit ────────────────────────────────────────────────────────────

    /// <summary>Classify every enabled beat in a book against Dwight Swain's Scene/Sequel doctrine. Scene = Goal→Conflict→Disaster; Sequel = Reaction→Dilemma→Decision. Ambiguous (one element weak) = MODERATE; Deficient (pattern not executed) = BLOCKER.</summary>
    [McpServerTool, Description("Classify every enabled beat in a book against Dwight Swain's Scene/Sequel doctrine via a Haiku pass. Scene (Goal→Conflict→Disaster) and Sequel (Reaction→Dilemma→Decision) both pass; Ambiguous (one element weak/underwritten) is MODERATE; Deficient (neither pattern executes) is BLOCKER. Returns per-beat classification plus book-level pass/MODERATE/BLOCKER counts and compliance rate. Accepts node id (GUID) or slug/NodeCode.")]
    public Task<string> swain_audit(
        [Description("Book node id (GUID), slug, or NodeCode.")] string nodeIdOrSlug,
        [Description("Set true to use Opus instead of Haiku for classification (stubborn/ambiguous beats).")] bool useOpus = false) =>
        hub.InvokeAsync(nameof(SwainTools), nameof(swain_auditImpl), new { nodeIdOrSlug, useOpus });

    public async Task<string> swain_auditImpl(
        string nodeIdOrSlug,
        bool useOpus = false)
    {
        try
        {
            var model = useOpus ? "claude-opus-4-8" : null;
            var report = await swain.AuditAsync(nodeIdOrSlug, model);
            return JsonSerializer.Serialize(new
            {
                node_id         = report.NodeId,
                node_code       = report.NodeCode,
                title           = report.Title,
                total_beats     = report.TotalBeats,
                pass_count      = report.PassCount,
                moderate_count  = report.ModerateCount,
                blocker_count   = report.BlockerCount,
                compliance_rate = report.ComplianceRate,
                findings        = report.Results.Where(r => !r.IsPass).Select(r => new
                {
                    beat_id         = r.BeatId,
                    position        = r.Position,
                    title           = r.Title,
                    classification  = r.Classification.ToString(),
                    missing_element = r.MissingElement,
                    note            = r.Note,
                    severity        = r.Severity,
                }),
            }, JsonOpts);
        }
        catch (InvalidOperationException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message, nodeIdOrSlug }, JsonOpts);
        }
    }

    // ── swain_audit_all ────────────────────────────────────────────────────────

    /// <summary>Run the Swain Scene/Sequel audit across every book node. Returns a per-book summary table plus corpus totals — use this to find which books have BLOCKER findings before drilling into swain_audit on any single one.</summary>
    [McpServerTool, Description("Run the Swain Scene/Sequel doctrine audit across every book node in the current universe scope. Returns a per-book summary (beat count, pass/MODERATE/BLOCKER counts, compliance rate) plus corpus-wide totals. Use this first to see which books need attention before calling swain_audit on a specific one.")]
    public Task<string> swain_audit_all(
        [Description("Set true to use Opus instead of Haiku for classification (slower, costlier, more accurate on stubborn beats).")] bool useOpus = false) =>
        hub.InvokeAsync(nameof(SwainTools), nameof(swain_audit_allImpl), new { useOpus });

    public async Task<string> swain_audit_allImpl(bool useOpus = false)
    {
        var model = useOpus ? "claude-opus-4-8" : null;
        var reports = await swain.AuditAllAsync(model);

        return JsonSerializer.Serialize(new
        {
            books = reports.OrderBy(r => r.NodeCode).Select(r => new
            {
                node_id         = r.NodeId,
                node_code       = r.NodeCode,
                title           = r.Title,
                total_beats     = r.TotalBeats,
                pass_count      = r.PassCount,
                moderate_count  = r.ModerateCount,
                blocker_count   = r.BlockerCount,
                compliance_rate = r.ComplianceRate,
            }),
            totals = new
            {
                total_beats    = reports.Sum(r => r.TotalBeats),
                pass_count     = reports.Sum(r => r.PassCount),
                moderate_count = reports.Sum(r => r.ModerateCount),
                blocker_count  = reports.Sum(r => r.BlockerCount),
            },
        }, JsonOpts);
    }

    // ── swain_repair ───────────────────────────────────────────────────────────

    /// <summary>Auto-splice the missing structural element into every BLOCKER beat in a book (or one specific beat). Re-audits first, then for each BLOCKER: loads the beat text, asks Sonnet (or Opus) to add ONLY the missing element at the most natural point, and applies the splice via the workbench.</summary>
    [McpServerTool, Description("Repair Swain BLOCKER findings in a book by auto-splicing the missing structural element (disaster turn, decision, etc.) into each deficient beat. Re-runs the audit first, then for each BLOCKER (or just beatId if given): loads the beat's current text, asks an LLM to add ONLY the missing element without rewriting existing sentences, and applies the result via the workbench. Returns per-beat repair outcomes. Accepts node id (GUID) or slug/NodeCode.")]
    public Task<string> swain_repair(
        [Description("Book node id (GUID), slug, or NodeCode.")] string nodeIdOrSlug,
        [Description("Only repair this specific beat id (GUID), if given — otherwise every BLOCKER in the book.")] string? beatId = null,
        [Description("Set true to use Opus instead of Sonnet for the splice (stubborn beats that resist a Sonnet pass).")] bool useOpus = false) =>
        hub.InvokeAsync(nameof(SwainTools), nameof(swain_repairImpl), new { nodeIdOrSlug, beatId, useOpus });

    public async Task<string> swain_repairImpl(
        string nodeIdOrSlug,
        string? beatId = null,
        bool useOpus = false)
    {
        SwainAuditReport report;
        try { report = await swain.AuditAsync(nodeIdOrSlug); }
        catch (InvalidOperationException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message, nodeIdOrSlug }, JsonOpts);
        }

        var targetId = beatId != null && Guid.TryParse(beatId, out var g) ? g : (Guid?)null;
        var blockers = report.Results
            .Where(r => r.Severity == "BLOCKER" && (targetId == null || r.BeatId == targetId))
            .ToList();

        if (blockers.Count == 0)
            return JsonSerializer.Serialize(new { status = "nothing_to_repair", node_code = report.NodeCode, blocker_count = 0 }, JsonOpts);

        var spliceModel = useOpus ? "claude-opus-4-8" : null;
        var results = new List<object>();
        var repairedCount = 0;

        foreach (var finding in blockers)
        {
            var beatText = await swain.LoadBeatTextAsync(finding.BeatId);
            if (beatText == null)
            {
                results.Add(new { beat_id = finding.BeatId, position = finding.Position, status = "load_failed" });
                continue;
            }

            var before = beatText.Length;
            var spliced = await swain.SpliceAsync(finding, beatText, spliceModel);
            if (spliced == null)
            {
                results.Add(new { beat_id = finding.BeatId, position = finding.Position, status = "splice_failed" });
                continue;
            }

            var ok = await swain.ApplySpliceAsync(finding, spliced);
            if (ok) repairedCount++;
            results.Add(new
            {
                beat_id      = finding.BeatId,
                position     = finding.Position,
                status       = ok ? "repaired" : "apply_failed",
                chars_added  = ok ? spliced.Length - before : 0,
            });
        }

        return JsonSerializer.Serialize(new
        {
            node_code = report.NodeCode,
            attempted = blockers.Count,
            repaired  = repairedCount,
            results,
        }, JsonOpts);
    }
}
