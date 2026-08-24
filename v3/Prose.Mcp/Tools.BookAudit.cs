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
    NodeWorkbenchService workbench,
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

        // Write-gate Phase 1 (2026-08-22): was a raw PreviousNodeId write; now the sanctioned
        // NodeWorkbenchService.SetPreviousNodeAsync (built write-gate Phase 0) so the
        // WriteSubject.NodeStructure classification has one call site to observe.
        string nodeSlug;
        try
        {
            await workbench.SetPreviousNodeAsync(nodeId.Value, prevId);
            await using var db = await dbFactory.CreateDbContextAsync();
            nodeSlug = await db.Nodes.AsNoTracking().Where(n => n.Id == nodeId.Value).Select(n => n.Slug).FirstAsync();
        }
        catch (InvalidOperationException ex)
        {
            return JsonSerializer.Serialize(new { error = "set_previous_failed", message = ex.Message }, JsonOpts);
        }

        return JsonSerializer.Serialize(new
        {
            status            = "updated",
            node_slug       = nodeSlug,
            mode              = prevId.HasValue ? "sequel" : "gateway",
            previous_node_id = prevId,
        }, JsonOpts);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 2026-08-24 consolidation. This file carried its own copy of the node resolver, and that
    /// copy was broken two ways: no <c>IgnoreQueryFilters()</c> on EITHER branch (so every book
    /// outside the ambient universe returned node_not_found when addressed by slug or NodeCode —
    /// e.g. VIGL, universe scry), and a GUID branch that returned the parsed value without
    /// checking any node has that id. A corpus audit found twelve copies of this helper and six
    /// broken ones; the "GUID branch fixed, slug branch missed" split alone had been re-found and
    /// re-patched four times in eight days, which is what a duplicated helper guarantees.
    /// Delegates to <see cref="NodeRefResolver"/> — the one sanctioned resolver, which also
    /// accepts a unique GUID prefix.
    /// </summary>
    Task<Guid?> ResolveNodeAsync(string idOrSlug) =>
        NodeRefResolver.ResolveAsync(dbFactory, idOrSlug);
}
