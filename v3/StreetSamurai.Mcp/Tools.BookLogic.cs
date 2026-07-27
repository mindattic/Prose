using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Story Logic / Outline tools ───────────────────────────────────────────────
//
//   write_outline — generate a beat-by-beat narrative outline + adversarial
//                   logic audit (plot holes, canon violations, prop errors,
//                   causality breaks, contradictions). Core sanity check for
//                   catching "the straightness of a katana"-level errors before
//                   they accumulate across a node.

[McpServerToolType]
public class BookLogicTools(
    BookLogicAuditService auditService,
    IDbContextFactory<StreetSamuraiDbContext> dbFactory)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    /// <summary>Generate a beat-by-beat outline and run an adversarial logic audit. Use this before reviewing a node to catch plot holes, canon violations, impossible actions, prop errors (e.g. a curved sword described as straight), causality breaks, and contradictions. Returns the outline (act-grouped narrative summary) and a findings list. Each finding has beat_number, severity (critical/major/minor), category, problem description, and a concrete fix suggestion. Pass skip_audit=true for outline only.</summary>
    [McpServerTool, Description("Generate a narrative outline and adversarial logic audit for a node. Finds plot holes, canon violations, prop errors, causality breaks, and contradictions. Returns outline (beat-by-beat narrative summary grouped by act) + findings list with severity/category/problem/suggestion per issue. Pass skip_audit=true for outline only (faster). Accepts node id (GUID) or slug.")]
    public async Task<string> write_outline(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug,
        [Description("Skip the logic audit and return outline only. Default false.")] bool skip_audit = false)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        try
        {
            var result = await auditService.AuditAsync(nodeId.Value, includeLogicCheck: !skip_audit);
            return JsonSerializer.Serialize(new
            {
                node_id    = result.NodeId,
                title        = result.Title,
                beat_count   = result.BeatCount,
                outline      = result.Outline,
                has_critical = result.HasCritical,
                has_major    = result.HasMajor,
                findings     = result.Findings.Select(f => new
                {
                    beat       = f.BeatNumber,
                    severity   = f.Severity,
                    category   = f.Category,
                    problem    = f.Problem,
                    suggestion = f.Suggestion,
                }),
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    async Task<Guid?> ResolveNodeAsync(string idOrSlug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (Guid.TryParse(idOrSlug, out var g))
            return (await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == g))?.Id;
        return (await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == idOrSlug || s.NodeCode == idOrSlug))?.Id;
    }
}
