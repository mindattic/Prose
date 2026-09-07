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
}
