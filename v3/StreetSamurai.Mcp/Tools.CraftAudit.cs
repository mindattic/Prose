using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services.Audit;

namespace Prose.Mcp;

// ── Craft Audit tool ──────────────────────────────────────────────────────────
//
//   craft_audit — audit a node's live prose against docs/CRAFT.md §8 (Banned
//                 Mannerisms), parsed live from CanonDocumentSections so an edit
//                 to §8 via set_canon_section is picked up on the next run with
//                 no code change.

[McpServerToolType]
public class CraftAuditTools(
    CraftRuleAuditService craftAudit,
    IDbContextFactory<ProseDbContext> dbFactory)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    [McpServerTool, Description("Audit a node's live prose against docs/CRAFT.md §8 (Banned Mannerisms — associative chains, " +
        "cognitive-architecture tics, the observation tic, mood-soup, purple prose at the peak, italic-thought crutch, " +
        "over-explanation, jargon front-loading). Each numbered item is parsed live from CanonDocumentSections, so editing " +
        "§8 via set_canon_section changes what's checked on the next run — no code change needed. Findings persist to the " +
        "Findings table and auto-heal on re-run. Accepts node id (GUID) or slug.")]
    public async Task<string> craft_audit(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        try
        {
            var report = await craftAudit.RunAsync(nodeId.Value);
            return JsonSerializer.Serialize(new
            {
                node_id  = report.NodeId,
                title      = report.NodeTitle,
                clean      = report.Clean,
                findings   = report.Findings.Select(f => new
                {
                    mannerism = f.Title,
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

    // ── helper ────────────────────────────────────────────────────────────────

    async Task<Guid?> ResolveNodeAsync(string idOrSlug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (Guid.TryParse(idOrSlug, out var g))
            return (await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == g))?.Id;
        return (await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == idOrSlug || s.NodeCode == idOrSlug))?.Id;
    }
}
