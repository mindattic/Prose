using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Plant/Payoff tools ────────────────────────────────────────────────────────
// Four tools for managing narrative plants and their payoffs per node.
// Enforces: "reward re-reading without requiring it."
//
//   get_plant_payoffs      — list all registered pairs for a node
//   register_plant_payoff  — register a new plant/payoff pair
//   link_plant_beat        — bind a pair's plant to its actual beat
//   link_payoff_beat       — bind a pair's payoff to its actual beat
//   set_plant_transparency — record whether the payoff stands alone + what re-readers gain
//   audit_plant_payoffs    — find orphaned plants and transparency violations

[McpServerToolType]
public class PlantPayoffTools(
    PlantPayoffService plantPayoffs,
    IDbContextFactory<ProseDbContext> dbFactory)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    // ── get_plant_payoffs ─────────────────────────────────────────────────────

    /// <summary>List all registered plant/payoff pairs for a node. Plants are narrative details seeded early in the text that pay off later. Returns id, plant_description, payoff_description, category, is_transparent, transparency_note, status (planned/seeded/paid-off), and the beat ids when linked.</summary>
    [McpServerTool, Description("List all registered plant/payoff pairs for a node. A plant is a narrative detail seeded early (a behavioral tell, an object, a gloss) that resonates or resolves later — rewarding re-readers without requiring first-timers to catch it. Returns all pairs with their status (planned = not yet written, seeded = plant beat written but no payoff yet, paid-off = both beats written), is_transparent flag (must be true for the payoff to work for cold readers), and transparency_note (what the re-reader gains). Accepts node id (GUID) or slug.")]
    public async Task<string> get_plant_payoffs(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        var pairs = await plantPayoffs.GetByNodeAsync(nodeId.Value);
        return JsonSerializer.Serialize(pairs.Select(p => new
        {
            id                 = p.Id,
            plant_description  = p.PlantDescription,
            payoff_description = p.PayoffDescription,
            category           = p.Category,
            is_transparent     = p.IsTransparent,
            transparency_note  = p.TransparencyNote,
            status             = p.PayoffBeatId != null ? "paid-off" : p.PlantBeatId != null ? "seeded" : "planned",
            plant_beat_id      = p.PlantBeatId,
            payoff_beat_id     = p.PayoffBeatId,
            sort_key           = p.SortKey,
        }), JsonOpts);
    }

    // ── register_plant_payoff ─────────────────────────────────────────────────

    /// <summary>Register a new plant/payoff pair for a node. Use before or during writing to track what's seeded and where it pays off. Categories: detail (a fact or observation), echo (a mirrored scene with shifted meaning), irony (a line that reads differently knowing the outcome), motif (a recurring symbol), character-truth (a behavioral tell), structural (an architecture element). Returns the new pair's id.</summary>
    [McpServerTool, Description("Register a new plant/payoff pair for a node. Call this when you're about to write (or have just written) a detail that will pay off later. plant_description = what is seeded (the observable detail the cold reader sees but doesn't decode); payoff_description = what the re-reader gets (the deeper meaning on return). Category options: detail, echo, irony, motif, character-truth, structural. Optionally link to specific beats by their GUID ids (plant_beat_id, payoff_beat_id). Accepts node id (GUID) or slug.")]
    public async Task<string> register_plant_payoff(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug,
        [Description("What is seeded — the detail the cold reader encounters but doesn't decode. Example: 'Kyle's hand twitches when he mentions Seo.'")]
        string plantDescription,
        [Description("How it pays off — what the returning reader gets on re-read. Example: 'On re-read, the twitch reveals the mentor was fabricated long before Kyle admits it.'")]
        string payoffDescription,
        [Description("Category: detail | echo | irony | motif | character-truth | structural")] string category = "detail",
        [Description("Beat GUID where the plant is seeded (omit if not yet written).")] string? plantBeatId = null,
        [Description("Beat GUID where the payoff occurs (omit if not yet written).")] string? payoffBeatId = null)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        try
        {
            var pp = await plantPayoffs.RegisterAsync(
                nodeId.Value,
                plantDescription,
                payoffDescription,
                category,
                plantBeatId  != null && Guid.TryParse(plantBeatId,  out var pb)  ? pb  : null,
                payoffBeatId != null && Guid.TryParse(payoffBeatId, out var pob) ? pob : null);

            return JsonSerializer.Serialize(new { id = pp.Id, status = "registered", category = pp.Category }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
        }
    }

    // ── link_plant_beat / link_payoff_beat ────────────────────────────────────

    /// <summary>After writing the plant beat, link it to the registered pair so the beat-writing context can confirm it's been seeded. Pass the PlantPayoff id and the beat GUID.</summary>
    [McpServerTool, Description("Link the plant beat to a registered plant/payoff pair. Call after writing the beat that seeds the plant detail. plant_payoff_id = GUID returned by register_plant_payoff; beat_id = GUID of the beat containing the plant.")]
    public async Task<string> link_plant_beat(
        [Description("PlantPayoff id (GUID) from register_plant_payoff.")] string plantPayoffId,
        [Description("Beat GUID containing the plant.")] string beatId)
    {
        if (!Guid.TryParse(plantPayoffId, out var ppId) || !Guid.TryParse(beatId, out var bId))
            return JsonSerializer.Serialize(new { error = "invalid_guid" }, JsonOpts);
        try
        {
            await plantPayoffs.LinkPlantBeatAsync(ppId, bId);
            return JsonSerializer.Serialize(new { status = "linked", plant_payoff_id = ppId, beat_id = bId }, JsonOpts);
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts); }
    }

    /// <summary>After writing the payoff beat, link it to the registered pair. Pass the PlantPayoff id and the beat GUID. The pair is now marked paid-off.</summary>
    [McpServerTool, Description("Link the payoff beat to a registered plant/payoff pair. Call after writing the beat where the plant pays off. plant_payoff_id = GUID from register_plant_payoff; beat_id = GUID of the payoff beat.")]
    public async Task<string> link_payoff_beat(
        [Description("PlantPayoff id (GUID) from register_plant_payoff.")] string plantPayoffId,
        [Description("Beat GUID containing the payoff.")] string beatId)
    {
        if (!Guid.TryParse(plantPayoffId, out var ppId) || !Guid.TryParse(beatId, out var bId))
            return JsonSerializer.Serialize(new { error = "invalid_guid" }, JsonOpts);
        try
        {
            await plantPayoffs.LinkPayoffBeatAsync(ppId, bId);
            return JsonSerializer.Serialize(new { status = "linked", plant_payoff_id = ppId, beat_id = bId }, JsonOpts);
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts); }
    }

    // ── set_plant_transparency ────────────────────────────────────────────────

    /// <summary>Mark whether a payoff beat stands alone for cold readers, and record what re-readers gain. is_transparent must be true before a node passes gateway audit. note should describe specifically what the returning reader understands that the first-timer doesn't.</summary>
    [McpServerTool, Description("Record whether a payoff beat stands alone for cold readers (is_transparent) and what the re-reader gains (note). is_transparent=true means the payoff makes complete narrative sense without having read/remembered the plant. is_transparent=false is a writing bug — fix the payoff beat before marking the node gateway-ready. note should name the specific additional layer the returning reader receives.")]
    public async Task<string> set_plant_transparency(
        [Description("PlantPayoff id (GUID).")] string plantPayoffId,
        [Description("True = the payoff reads completely for a cold reader; false = it requires catching the plant (writing bug).")] bool isTransparent,
        [Description("What the re-reader gains that the first-timer doesn't. Required when is_transparent=true.")] string? note = null)
    {
        if (!Guid.TryParse(plantPayoffId, out var ppId))
            return JsonSerializer.Serialize(new { error = "invalid_guid" }, JsonOpts);
        try
        {
            await plantPayoffs.SetTransparencyAsync(ppId, isTransparent, note);
            return JsonSerializer.Serialize(new { status = "updated", is_transparent = isTransparent }, JsonOpts);
        }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts); }
    }

    // ── audit_plant_payoffs ───────────────────────────────────────────────────

    /// <summary>Audit all plant/payoff pairs for a node. Returns orphaned plants (seeded but no payoff written), transparency violations (payoff written but is_transparent=false), total coverage, and a gateway-ready verdict. A node is plant-ready when: all plants have payoffs, and all payoffs are transparent.</summary>
    [McpServerTool, Description("Audit all plant/payoff pairs for a node. Returns: total_pairs, planted (seeded in a beat), paid_off (payoff also written), orphaned (planted but no payoff), not_transparent (payoff exists but is_transparent=false), a gateway_plant_ready boolean (all planted pairs have transparent payoffs), and detail lists for each problem category. Fix orphaned plants and transparency issues before the node passes gateway audit. Accepts node id (GUID) or slug.")]
    public async Task<string> audit_plant_payoffs(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        try
        {
            var audit = await plantPayoffs.AuditAsync(nodeId.Value);
            return JsonSerializer.Serialize(new
            {
                node_slug          = audit.NodeSlug,
                node_title         = audit.NodeTitle,
                total_pairs          = audit.TotalPairs,
                planted              = audit.Planted,
                paid_off             = audit.PaidOff,
                orphaned             = audit.Orphaned,
                not_transparent      = audit.NotTransparentCount,
                gateway_plant_ready  = audit.Orphaned == 0 && audit.NotTransparentCount == 0,
                orphaned_plants      = audit.OrphanedPlants.Select(p => new { p.Id, p.PlantDescription, p.PayoffDescription }),
                transparency_issues  = audit.NotTransparentPayoffs.Select(p => new { p.Id, p.PlantDescription, p.PayoffDescription, p.TransparencyNote }),
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
        }
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
