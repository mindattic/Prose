using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Book Audit tools ─────────────────────────────────────────────────────────
// Two tools that audit a node against the appropriate commandment set.
//
//   audit_book_commandments — run all 7 gateway or sequel commandment checks
//                              (auto-detected from Node.PreviousNodeId)
//   set_previous_book      — link a node's predecessor to activate sequel mode

[McpServerToolType]
public class BookAuditTools(
    BookAuditService bookAudit,
    IDbContextFactory<ProseDbContext> dbFactory,
    HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    // ── audit_book_commandments ──────────────────────────────────────────────

    /// <summary>Audit a node against its 7 commandments. Gateway commandments apply when PreviousNodeId is null (standalone or first in series). Sequel commandments apply when PreviousNodeId is set. Each commandment returns pass/warn/fail with evidence and a fix suggestion. The gateway_ready boolean is true when no commandment fails.</summary>
    [McpServerTool, Description("Audit a node against all 7 commandments — gateway (for first/standalone books) or sequel (for books with a PreviousNodeId set). Auto-detected: null PreviousNodeId → gateway commandments; set → sequel commandments. Each commandment check returns status (pass/warn/fail), specific evidence from the prose, and a concrete one-sentence fix when not passing. Returns gateway_ready (no failing checks), blocking_count (failures), advisory_count (warnings), plus plant_count and orphaned_plants from the PlantPayoff registry (relevant for the 'reward re-reading' commandment). Accepts node id (GUID) or slug.")]
    public Task<string> audit_book_commandments(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug) =>
        hub.InvokeAsync(nameof(BookAuditTools), nameof(audit_book_commandmentsImpl), new { nodeIdOrSlug });

    public async Task<string> audit_book_commandmentsImpl(string nodeIdOrSlug)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        try
        {
            var report = await bookAudit.AuditAsync(nodeId.Value);
            return JsonSerializer.Serialize(new
            {
                node_slug      = report.NodeSlug,
                node_title     = report.NodeTitle,
                mode             = report.Mode,
                previous_node  = report.PreviousNode,
                gateway_ready    = report.GatewayReady,
                blocking_count   = report.BlockingCount,
                advisory_count   = report.AdvisoryCount,
                plant_count      = report.PlantCount,
                orphaned_plants  = report.OrphanedPlants,
                truncated        = report.Truncated,
                checks           = report.Checks.Select(c => new
                {
                    key      = c.Key,
                    title    = c.Title,
                    status   = c.Status,
                    evidence = c.Evidence,
                    fix      = c.Fix,
                }),
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message, nodeIdOrSlug }, JsonOpts);
        }
    }

    // ── set_previous_book ───────────────────────────────────────────────────

    /// <summary>Set or clear a node's PreviousNodeId to switch between gateway mode (null) and sequel mode (set). When PreviousNodeId is set, the book automatically uses sequel commandments in audit_book_commandments and in beat-writing context injection.</summary>
    [McpServerTool, Description("Link a node to its predecessor, switching it from gateway mode to sequel mode. When previous_node_id_or_slug is provided, Node.PreviousNodeId is set — the book will use sequel commandments in audits and beat-writing context. To clear (revert to gateway mode), pass clear=true. Accepts both node arguments as id (GUID) or slug.")]
    public Task<string> set_previous_book(
        [Description("The node to update — id (GUID) or slug.")] string nodeIdOrSlug,
        [Description("The preceding node — id (GUID) or slug. Omit or pass null to clear.")] string? previousNodeIdOrSlug = null,
        [Description("Set true to clear PreviousNodeId (revert to gateway mode).")] bool clear = false) =>
        hub.InvokeAsync(nameof(BookAuditTools), nameof(set_previous_bookImpl), new { nodeIdOrSlug, previousNodeIdOrSlug, clear });

    public async Task<string> set_previous_bookImpl(
        string nodeIdOrSlug,
        string? previousNodeIdOrSlug = null,
        bool clear = false)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        Guid? prevId = null;
        if (!clear && !string.IsNullOrWhiteSpace(previousNodeIdOrSlug))
        {
            prevId = await ResolveNodeAsync(previousNodeIdOrSlug);
            if (prevId == null)
                return JsonSerializer.Serialize(new { error = "previous_node_not_found", previousNodeIdOrSlug }, JsonOpts);
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.FindAsync(nodeId.Value);
        if (node == null)
            return JsonSerializer.Serialize(new { error = "node_not_found" }, JsonOpts);

        node.PreviousNodeId = prevId;
        node.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            status            = "updated",
            node_slug       = node.Slug,
            mode              = prevId.HasValue ? "sequel" : "gateway",
            previous_node_id = prevId,
        }, JsonOpts);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    async Task<Guid?> ResolveNodeAsync(string idOrSlug)
    {
        if (Guid.TryParse(idOrSlug, out var g)) return g;
        await using var db = await dbFactory.CreateDbContextAsync();
        var s = await db.Nodes.AsNoTracking()
            .Where(x => x.Slug == idOrSlug || x.NodeCode == idOrSlug)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
        return s == Guid.Empty ? null : s;
    }
}
