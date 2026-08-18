using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Voice harvest tools — expose VoiceHarvestService over MCP ─────────────
// Distill voice rules from winning nodes into the DB-backed rule store the
// generator reads. Propose-then-approve workflow: nothing touches the live
// rules until an admin applies a proposed entry.

/// <summary>
/// MCP surface for <see cref="VoiceHarvestService"/>: harvest voice rules from
/// winning nodes, list pending proposals, and apply or reject them.
/// </summary>
[McpServerToolType]
public class VoiceTools
{
    private readonly VoiceHarvestService harvest;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    public VoiceTools(VoiceHarvestService harvest, IDbContextFactory<ProseDbContext> dbFactory)
    {
        this.harvest = harvest;
        this.dbFactory = dbFactory;
    }

    /// <summary>Harvest voice rules from a single node. The node must have a score ≥80 (or pass --force). Returns proposed change-log entries; nothing is written to the live rule store until you call apply_voice_proposal.</summary>
    [McpServerTool, Description("Distill voice rules from a winning node (score ≥80) into proposed change-log entries. Nothing touches the live rule store until apply_voice_proposal is called. Pass force=true to harvest even if the node scored below 80. Returns the list of proposed entries with their ids, rule targets, descriptions, and evidence.")]
    public async Task<string> HarvestVoice(
        [Description("Node id (GUID) or slug to harvest from.")] string nodeIdOrSlug,
        [Description("Set to true to harvest even if the node scored below 80%.")] bool force = false)
    {
        var nodeId = await ResolveNodeIdAsync(nodeIdOrSlug);
        if (nodeId == null) return Error("node_not_found", nodeIdOrSlug);

        var r = await harvest.HarvestNodeAsync(nodeId.Value, force);
        return JsonSerializer.Serialize(new
        {
            slug           = r.Slug,
            title          = r.Title,
            score          = r.Score,
            edit_count     = r.EditCount,
            directive_count = r.DirectiveCount,
            proposals      = SerializeProposals(r.Proposals),
        }, CanonTools.JsonOpts);
    }

    /// <summary>Harvest voice rules from all nodes scored ≥threshold and return the combined proposals grouped by node.
    /// NOTE (SS-A44, 2026-08-03): the 0-100 score-panel gates were retired project-wide, so almost no node carries a
    /// Score anymore (verified: 22/421 nodes have any Score at all, 2/421 are ≥80) — this tool will return an empty
    /// result against most of the live corpus regardless of threshold. Prefer harvest_voice_canon (selects by
    /// Node.IsCanon, the current recommended gate) or harvest_voice_node with force=true for a specific book.</summary>
    [McpServerTool, Description("Distill voice rules from every node scored >=threshold (default 80). Score gates were retired project-wide (SS-A44) so almost no node has a Score anymore — this will likely return empty. Prefer harvest_voice_canon or harvest_voice_node(force:true) instead. Returns proposals grouped by node slug. Nothing is written to the live rule store until apply_voice_proposal is called.")]
    public async Task<string> HarvestVoiceAll(
        [Description("Minimum Node.Score to include (0-100). Default 80. Only affects nodes that HAVE a Score — most nodes have none post-SS-A44.")] double threshold = 80)
    {
        var results = await harvest.HarvestAllAboveAsync(threshold);
        return JsonSerializer.Serialize(results.Select(r => new
        {
            slug           = r.Slug,
            title          = r.Title,
            score          = r.Score,
            edit_count     = r.EditCount,
            directive_count = r.DirectiveCount,
            proposal_count = r.Proposals.Count,
            proposals      = SerializeProposals(r.Proposals),
        }), CanonTools.JsonOpts);
    }

    /// <summary>Harvest voice rules from every node the author has marked Canon (Node.IsCanon) —
    /// the SS-A44-era recommended gate now that 0-100 score panels are retired. Canon is an
    /// explicit author trust decision, so every canon node is harvested unconditionally.</summary>
    [McpServerTool, Description("Distill voice rules from every node the author has marked Canon (IsCanon=true) — the recommended harvest gate post-SS-A44, since almost no node carries a Score anymore. Returns proposals grouped by node slug. Nothing is written to the live rule store until apply_voice_proposal is called.")]
    public async Task<string> HarvestVoiceCanon()
    {
        var results = await harvest.HarvestCanonAsync();
        return JsonSerializer.Serialize(results.Select(r => new
        {
            slug           = r.Slug,
            title          = r.Title,
            score          = r.Score,
            edit_count     = r.EditCount,
            directive_count = r.DirectiveCount,
            proposal_count = r.Proposals.Count,
            proposals      = SerializeProposals(r.Proposals),
        }), CanonTools.JsonOpts);
    }

    /// <summary>List voice proposals by status. Status values: "proposed" (awaiting decision), "applied", "rejected", "observed".</summary>
    [McpServerTool, Description("List voice change-log entries filtered by status. Use status='proposed' to see pending proposals awaiting a decision. Each entry shows its id (use for apply/reject), rule target, description, evidence, and source node.")]
    public async Task<string> ListVoiceProposals(
        [Description("Filter by status: 'proposed' | 'applied' | 'rejected' | 'observed'. Default 'proposed'.")] string status = "proposed")
    {
        var rows = await harvest.GetByStatusAsync(status);
        return JsonSerializer.Serialize(SerializeProposals(rows), CanonTools.JsonOpts);
    }

    /// <summary>Apply a proposed voice change-log entry, writing the rule to the live voice store.</summary>
    [McpServerTool, Description("Apply a proposed voice rule to the live voice store (the DB-backed rules the generator reads). Pass the entry id returned by harvest_voice or list_voice_proposals. The entry status changes to 'applied'. Returns ok=true on success, or error if the entry was not found or already resolved.")]
    public async Task<string> ApplyVoiceProposal(
        [Description("The voice change-log entry GUID to apply.")] string entryId)
    {
        if (!Guid.TryParse(entryId, out var guid))
            return Error("invalid_guid", entryId);
        var ok = await harvest.ApplyAsync(guid);
        return ok
            ? JsonSerializer.Serialize(new { ok = true, applied = entryId }, CanonTools.JsonOpts)
            : Error("not_found_or_already_resolved", entryId);
    }

    /// <summary>Reject a proposed voice change-log entry. The entry is kept in the audit trail as "rejected".</summary>
    [McpServerTool, Description("Reject a proposed voice rule. The entry stays in the audit trail (status = 'rejected') so the decision is traceable. Pass the entry id returned by harvest_voice or list_voice_proposals.")]
    public async Task<string> RejectVoiceProposal(
        [Description("The voice change-log entry GUID to reject.")] string entryId)
    {
        if (!Guid.TryParse(entryId, out var guid))
            return Error("invalid_guid", entryId);
        var ok = await harvest.RejectAsync(guid);
        return ok
            ? JsonSerializer.Serialize(new { ok = true, rejected = entryId }, CanonTools.JsonOpts)
            : Error("not_found_or_already_resolved", entryId);
    }

    private static object[] SerializeProposals(IEnumerable<Prose.Core.Data.Entities.VoiceChangeLogEntry> rows) =>
        rows.Select(e => (object)new
        {
            id          = e.Id,
            source      = e.Source,
            node_id   = e.NodeId,
            rule_target = e.RuleTarget,
            description = e.Description,
            evidence    = e.Evidence,
            status      = e.Status,
            created_at  = e.CreatedAt,
        }).ToArray();

    private async Task<Guid?> ResolveNodeIdAsync(string idOrSlug)
    {
        if (string.IsNullOrWhiteSpace(idOrSlug)) return null;
        await using var db = await dbFactory.CreateDbContextAsync();
        if (Guid.TryParse(idOrSlug, out var guid))
        {
            // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
            var byId = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.Id == guid);
            if (byId != null) return byId.Id;
        }
        // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
        var bySlug = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.Slug == idOrSlug || s.NodeCode == idOrSlug);
        return bySlug?.Id;
    }

    private static string Error(string code, string detail) =>
        JsonSerializer.Serialize(new { error = code, detail }, CanonTools.JsonOpts);
}
