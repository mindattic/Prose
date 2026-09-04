using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Entity tags (the EntityTags bridge) ──────────────────────────────────────
// The MCP half of `prose --entity-tags`. Both surfaces call EntityTagService so they cannot drift.
//
// Why these exist (2026-09-03): tags could be ADDED and never removed. create_character's `tags`
// parameter MERGES (like `aliases`), so passing a shorter list silently keeps the old entries, and
// --tag-entities is a different thing entirely (inline entity-GUID markup inside beat text). A
// wrong tag was therefore permanent — found when a character cut from a book could not have that
// book's tag removed, which is not cosmetic: a stale book tag can pull her into that book's
// context loads.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Tools to inspect, add and remove an entity's tags.</summary>
[McpServerToolType]
public class EntityTagTools(EntityTagService tags, IDbContextFactory<ProseDbContext> dbFactory, HubInvoker hub)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    [McpServerTool, Description(
        "List an entity's tags. Pass an entity GUID or its exact name.")]
    public Task<string> ListEntityTags(
        [Description("Entity GUID or exact name.")] string entity) =>
        hub.InvokeAsync(nameof(EntityTagTools), nameof(ListEntityTagsImpl), new { entity });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection.</summary>
    public async Task<string> ListEntityTagsImpl(string entity)
    {
        var who = await ResolveAsync(entity);
        if (who == null) return JsonSerializer.Serialize(new { error = "entity_not_found", entity }, JsonOpts);
        var list = await tags.ListAsync(who.Value.Id);
        return JsonSerializer.Serialize(new
        {
            ok = true, entity = who.Value.Name, entityId = who.Value.Id.ToString("N"),
            count = list.Count, tags = list,
        }, JsonOpts);
    }

    [McpServerTool, Description(
        "REMOVE tags from an entity — the only path that can take a tag off, since create_character's " +
        "tags parameter merges and never deletes. Comma-separated, case-insensitive. Removing a tag the " +
        "entity does not carry is a no-op, not an error. Only the entity's link is removed; the shared " +
        "tag vocabulary row is left intact for every other entity using it.")]
    public Task<string> RemoveEntityTags(
        [Description("Entity GUID or exact name.")] string entity,
        [Description("Comma-separated tag names to remove, e.g. 'vigl,battle-rig'.")] string tagNames) =>
        hub.InvokeAsync(nameof(EntityTagTools), nameof(RemoveEntityTagsImpl), new { entity, tagNames });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection.</summary>
    public async Task<string> RemoveEntityTagsImpl(string entity, string tagNames)
    {
        var who = await ResolveAsync(entity);
        if (who == null) return JsonSerializer.Serialize(new { error = "entity_not_found", entity }, JsonOpts);

        var removed = await tags.RemoveAsync(who.Value.Id, Split(tagNames));
        var now = await tags.ListAsync(who.Value.Id);
        return JsonSerializer.Serialize(new
        {
            ok = true, entity = who.Value.Name, removed, remaining = now,
        }, JsonOpts);
    }

    [McpServerTool, Description(
        "Add tags to an entity, creating any tag vocabulary row that does not exist yet. " +
        "Comma-separated. Tags already present are skipped.")]
    public Task<string> AddEntityTags(
        [Description("Entity GUID or exact name.")] string entity,
        [Description("Comma-separated tag names to add.")] string tagNames) =>
        hub.InvokeAsync(nameof(EntityTagTools), nameof(AddEntityTagsImpl), new { entity, tagNames });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection.</summary>
    public async Task<string> AddEntityTagsImpl(string entity, string tagNames)
    {
        var who = await ResolveAsync(entity);
        if (who == null) return JsonSerializer.Serialize(new { error = "entity_not_found", entity }, JsonOpts);

        var added = await tags.AddAsync(who.Value.Id, Split(tagNames));
        var now = await tags.ListAsync(who.Value.Id);
        return JsonSerializer.Serialize(new
        {
            ok = true, entity = who.Value.Name, added, tags = now,
        }, JsonOpts);
    }

    private static List<string> Split(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private async Task<(Guid Id, string Name)?> ResolveAsync(string who)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (Guid.TryParse(who, out var parsed))
        {
            var byId = await db.Entities.IgnoreQueryFilters().AsNoTracking()
                .Where(e => e.Id == parsed).Select(e => new { e.Id, e.Name }).FirstOrDefaultAsync();
            if (byId != null) return (byId.Id, byId.Name);
        }
        var byName = await db.Entities.AsNoTracking()
            .Where(e => e.Name == who).Select(e => new { e.Id, e.Name }).FirstOrDefaultAsync();
        return byName == null ? null : (byName.Id, byName.Name);
    }
}
