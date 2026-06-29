using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Voice harvest tools — expose VoiceHarvestService over MCP ─────────────
// Distill voice rules from winning strands into the DB-backed rule store the
// generator reads. Propose-then-approve workflow: nothing touches the live
// rules until an admin applies a proposed entry.

/// <summary>
/// MCP surface for <see cref="VoiceHarvestService"/>: harvest voice rules from
/// winning strands, list pending proposals, and apply or reject them.
/// </summary>
[McpServerToolType]
public class VoiceTools
{
    private readonly VoiceHarvestService harvest;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;

    public VoiceTools(VoiceHarvestService harvest, IDbContextFactory<StreetSamuraiDbContext> dbFactory)
    {
        this.harvest = harvest;
        this.dbFactory = dbFactory;
    }

    /// <summary>Harvest voice rules from a single strand. The strand must have a score ≥80 (or pass --force). Returns proposed change-log entries; nothing is written to the live rule store until you call apply_voice_proposal.</summary>
    [McpServerTool, Description("Distill voice rules from a winning strand (score ≥80) into proposed change-log entries. Nothing touches the live rule store until apply_voice_proposal is called. Pass force=true to harvest even if the strand scored below 80. Returns the list of proposed entries with their ids, rule targets, descriptions, and evidence.")]
    public async Task<string> HarvestVoice(
        [Description("Strand id (GUID) or slug to harvest from.")] string strandIdOrSlug,
        [Description("Set to true to harvest even if the strand scored below 80%.")] bool force = false)
    {
        var strandId = await ResolveStrandIdAsync(strandIdOrSlug);
        if (strandId == null) return Error("strand_not_found", strandIdOrSlug);

        var r = await harvest.HarvestStrandAsync(strandId.Value, force);
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

    /// <summary>Harvest voice rules from all strands scored ≥80% and return the combined proposals grouped by strand.</summary>
    [McpServerTool, Description("Distill voice rules from every strand scored ≥80%. Returns proposals grouped by strand slug. Nothing is written to the live rule store until apply_voice_proposal is called. Use list_voice_proposals to see all pending entries afterward.")]
    public async Task<string> HarvestVoiceAll()
    {
        var results = await harvest.HarvestAllAboveAsync();
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
    [McpServerTool, Description("List voice change-log entries filtered by status. Use status='proposed' to see pending proposals awaiting a decision. Each entry shows its id (use for apply/reject), rule target, description, evidence, and source strand.")]
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

    private static object[] SerializeProposals(IEnumerable<StreetSamurai.Core.Data.Entities.VoiceChangeLogEntry> rows) =>
        rows.Select(e => (object)new
        {
            id          = e.Id,
            source      = e.Source,
            strand_id   = e.StrandId,
            rule_target = e.RuleTarget,
            description = e.Description,
            evidence    = e.Evidence,
            status      = e.Status,
            created_at  = e.CreatedAt,
        }).ToArray();

    private async Task<Guid?> ResolveStrandIdAsync(string idOrSlug)
    {
        if (string.IsNullOrWhiteSpace(idOrSlug)) return null;
        await using var db = await dbFactory.CreateDbContextAsync();
        if (Guid.TryParse(idOrSlug, out var guid))
        {
            var byId = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Id == guid);
            if (byId != null) return byId.Id;
        }
        var bySlug = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == idOrSlug || s.StrandCode == idOrSlug);
        return bySlug?.Id;
    }

    private static string Error(string code, string detail) =>
        JsonSerializer.Serialize(new { error = code, detail }, CanonTools.JsonOpts);
}
