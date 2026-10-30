using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── CorpoNation hierarchies (PRS-T819) ─────────────────────────────────────────
// Every CorpoNation has a Tier 1-4 (1 = ground-level work, 4 = royalty; 5 is a rumor nobody holds),
// and an owner always sits on a higher Tier than what it owns. Who owns what is recorded all the way
// back down to ground level, and every CorpoNation can call up its own forces plus every controlled
// subsidiary's. Tier itself is written with set_entity_fields {"tier": N}.
//   get_corp_chain / get_corp_tree   — owners up to the ultimate parent / everything owned, down to the ground
//   get_corp_forces                  — every force a CorpoNation can call up, each with who keeps it
//   set_corp_ownership / remove_corp_ownership — record or drop one ownership edge
//   add_corp_force                   — record (or update by name) a force a CorpoNation keeps
//   corp_hierarchy_audit             — tier-order breaks, loops, untiered corponations and parents
// CLI twin: prose --universe <u> --corp tier|own|force|chain|tree|forces|audit|backfill-ownership.

/// <summary>MCP surface of <see cref="CorporateHierarchyService"/>. Forwarded to the Hub. CLI twin: <c>prose --corp</c>.</summary>
[McpServerToolType]
public class CorporateTools(CorporateHierarchyService corp, HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    static string Fail(Exception ex) => JsonSerializer.Serialize(new { ok = false, error = ex.Message }, JsonOpts);

    [McpServerTool, Description("Who owns this entity, up to its ultimate parent: every owner edge above it, nearest first, with depth (1 = direct owner), kind, stake and each end's Tier. An entity is its id, slug or exact name.")]
    public Task<string> get_corp_chain([Description("Entity id, slug or exact name.")] string entity) =>
        hub.InvokeAsync(nameof(CorporateTools), nameof(GetCorpChainImpl), new { entity });

    public async Task<string> GetCorpChainImpl(string entity)
    {
        try
        {
            var (e, chain) = await corp.ChainAsync(entity);
            return JsonSerializer.Serialize(new { ok = true, entity = e, chain }, JsonOpts);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return Fail(ex); }
    }

    [McpServerTool, Description("Everything a corponation or subsidiary owns, recursively, down to ground level (subsidiaries, factions, places, transit — any entity), with depth (1 = owned directly), kind, stake and Tier.")]
    public Task<string> get_corp_tree([Description("Corponation or subsidiary id, slug or exact name.")] string entity) =>
        hub.InvokeAsync(nameof(CorporateTools), nameof(GetCorpTreeImpl), new { entity });

    public async Task<string> GetCorpTreeImpl(string entity)
    {
        try
        {
            var (e, tree) = await corp.TreeAsync(entity);
            return JsonSerializer.Serialize(new { ok = true, entity = e, tree }, JsonOpts);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return Fail(ex); }
    }

    [McpServerTool, Description("Every force a CorpoNation can call up: its own private military plus the forces of every subsidiary beneath it through wholly-owned or controlling edges (a minority stake, lease or licence buys no troops). Each force lists the entity that keeps it and how far down that keeper sits (0 = its own).")]
    public Task<string> get_corp_forces([Description("Corponation or subsidiary id, slug or exact name.")] string entity) =>
        hub.InvokeAsync(nameof(CorporateTools), nameof(GetCorpForcesImpl), new { entity });

    public async Task<string> GetCorpForcesImpl(string entity)
    {
        try
        {
            var (e, forces) = await corp.CallableForcesAsync(entity);
            return JsonSerializer.Serialize(new { ok = true, corponation = e, forces }, JsonOpts);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return Fail(ex); }
    }

    [McpServerTool, Description("Record that a corponation or subsidiary owns an entity (any type), or update the edge if it exists (omitted fields keep their values). Refused: an owner that is not a corponation/subsidiary, self-ownership, different universes, an ownership loop, and an owner not on a higher Tier than the owned when both have one. Returns the edge as stored.")]
    public Task<string> set_corp_ownership(
        [Description("Owner: corponation or subsidiary id, slug or exact name.")] string owner,
        [Description("Owned: any entity's id, slug or exact name.")] string owned,
        [Description("wholly-owned (default for a new edge) | controlling | minority | leased | licensed")] string? kind = null,
        [Description("Percent held, 0-100.")] decimal? stakePercent = null,
        [Description("In-world year the ownership began.")] int? sinceYear = null,
        [Description("Notes (empty string clears).")] string? notes = null) =>
        hub.InvokeAsync(nameof(CorporateTools), nameof(SetCorpOwnershipImpl), new { owner, owned, kind, stakePercent, sinceYear, notes });

    public async Task<string> SetCorpOwnershipImpl(string owner, string owned, string? kind = null, decimal? stakePercent = null, int? sinceYear = null, string? notes = null)
    {
        try
        {
            var (link, created) = await corp.SetOwnershipAsync(new OwnershipDraft(owner, owned, kind, stakePercent, sinceYear, notes));
            return JsonSerializer.Serialize(new { ok = true, created, link }, JsonOpts);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return Fail(ex); }
    }

    [McpServerTool, Description("Remove the ownership edge owner → owned. removed is false when there was none.")]
    public Task<string> remove_corp_ownership(
        [Description("Owner id, slug or exact name.")] string owner,
        [Description("Owned id, slug or exact name.")] string owned) =>
        hub.InvokeAsync(nameof(CorporateTools), nameof(RemoveCorpOwnershipImpl), new { owner, owned });

    public async Task<string> RemoveCorpOwnershipImpl(string owner, string owned)
    {
        try { return JsonSerializer.Serialize(new { ok = true, removed = await corp.RemoveOwnershipAsync(owner, owned) }, JsonOpts); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return Fail(ex); }
    }

    [McpServerTool, Description("Record a force a corponation or subsidiary keeps (its private military, civil security, strike teams, navy, drones, orbital assets), or update the one it already has by that name. entity optionally names the faction/subsidiary that IS the force.")]
    public Task<string> add_corp_force(
        [Description("Keeper: corponation or subsidiary id, slug or exact name.")] string corponation,
        [Description("The force's name.")] string name,
        [Description("private-military | civil-security | strike | naval | drone | orbital | other")] string kind,
        [Description("Optional: the entity (faction, subsidiary, …) that is this force.")] string? entity = null,
        [Description("Free text, e.g. \"two divisions\".")] string? strength = null,
        [Description("Notes (empty string clears).")] string? notes = null) =>
        hub.InvokeAsync(nameof(CorporateTools), nameof(AddCorpForceImpl), new { corponation, name, kind, entity, strength, notes });

    public async Task<string> AddCorpForceImpl(string corponation, string name, string kind, string? entity = null, string? strength = null, string? notes = null)
    {
        try
        {
            var (force, created) = await corp.SetForceAsync(new ForceDraft(corponation, name, kind, entity, strength, notes));
            return JsonSerializer.Serialize(new { ok = true, created, force }, JsonOpts);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return Fail(ex); }
    }

    [McpServerTool, Description("Audit the current universe's CorpoNation hierarchy: owners not on a higher Tier than what they own (tier-order), ownership loops (cycle), corponations with no Tier (untiered-corponation), and subsidiaries whose parent has no Tier (untiered-parent).")]
    public Task<string> corp_hierarchy_audit() =>
        hub.InvokeAsync(nameof(CorporateTools), nameof(CorpHierarchyAuditImpl));

    public async Task<string> CorpHierarchyAuditImpl()
    {
        var findings = await corp.AuditAsync();
        return JsonSerializer.Serialize(new
        {
            ok = true,
            count = findings.Count,
            byKind = findings.GroupBy(f => f.Kind).ToDictionary(g => g.Key, g => g.Count()),
            findings,
        }, JsonOpts);
    }
}
