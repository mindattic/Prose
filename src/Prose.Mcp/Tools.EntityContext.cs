using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

/// <summary>
/// MCP tools for inspecting the self-referential entity context stack.
/// The stack is populated by EntityContextService during prose generation via ProseWriterRouter.
/// Use these to debug what entities are in working memory for a node and check for canon conflicts.
/// </summary>
[McpServerToolType]
public class EntityContextTools(
    EntityContextService entityContext,
    EntityMentionService mentionService,
    EntityLookupService entityLookup,
    EntityRenameService entityRename,
    IDbContextFactory<ProseDbContext> dbFactory,
    HubInvoker hub)
{
    [McpServerTool, Description("Find canonical entities by a case-insensitive partial name or character alias. Returns name, GUID7 id, slug, entity type, and alias match when applicable.")]
    public Task<string> FindEntities(
        [Description("Partial canonical name or character alias.")] string query,
        [Description("Optional entity type, for example character or place.")] string? entityType = null,
        [Description("Maximum results, 1-200; default 40.")] int limit = 40) =>
        hub.InvokeAsync(nameof(EntityContextTools), nameof(FindEntitiesImpl), new { query, entityType, limit });

    public async Task<string> FindEntitiesImpl(string query, string? entityType = null, int limit = 40)
    {
        var matches = await entityLookup.FindAsync(query, entityType, limit);
        return JsonSerializer.Serialize(new { ok = true, count = matches.Count, matches }, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Preview a deterministic entity rename. Finds exact full-name references in one book's hand-authored outline and descendant beats, plus linked Story Ledger claims. Does not write.")]
    public Task<string> PreviewEntityRename(
        [Description("Canonical entity GUID7 or slug.")] string entityIdOrSlug,
        [Description("Book GUID, slug, or NodeCode that scopes outline and beats.")] string nodeIdOrSlug,
        [Description("New canonical full name.")] string newName) =>
        hub.InvokeAsync(nameof(EntityContextTools), nameof(PreviewEntityRenameImpl), new { entityIdOrSlug, nodeIdOrSlug, newName });

    public async Task<string> PreviewEntityRenameImpl(string entityIdOrSlug, string nodeIdOrSlug, string newName) =>
        JsonSerializer.Serialize(await entityRename.PreviewAsync(entityIdOrSlug, nodeIdOrSlug, newName), new JsonSerializerOptions { WriteIndented = true });

    [McpServerTool, Description("Apply a reviewed deterministic entity rename. Requires confirmed=true and an explicit active universe. Replaces exact full-name references in the selected book's outline and beats, relabels linked Story Ledger claims, and registers the old name as deprecated.")]
    public Task<string> ApplyEntityRename(
        [Description("Canonical entity GUID7 or slug.")] string entityIdOrSlug,
        [Description("Book GUID, slug, or NodeCode that scopes outline and beats.")] string nodeIdOrSlug,
        [Description("New canonical full name.")] string newName,
        [Description("Must be true after reviewing preview_entity_rename.")] bool confirmed = false,
        [Description("Optional audit note.")] string? note = null) =>
        hub.InvokeAsync(nameof(EntityContextTools), nameof(ApplyEntityRenameImpl), new { entityIdOrSlug, nodeIdOrSlug, newName, confirmed, note });

    public async Task<string> ApplyEntityRenameImpl(string entityIdOrSlug, string nodeIdOrSlug, string newName, bool confirmed = false, string? note = null)
    {
        if (!confirmed) return JsonSerializer.Serialize(new { ok = false, error = "approval_required", message = "Preview the rename and call again with confirmed=true." });
        return JsonSerializer.Serialize(await entityRename.ApplyAsync(entityIdOrSlug, nodeIdOrSlug, newName, note), new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Inspect the entity working memory currently active for a node. Shows depth-0 (directly named), depth-1 (semantic neighbors), and depth-2 (neighbors of neighbors) entities with their canon descriptions. Call after generating beats to see what was in scope.")]
    public Task<string> get_entity_context(
        [Description("Node slug (e.g. 'ATTE', 'BCODA')")] string slug) =>
        hub.InvokeAsync(nameof(EntityContextTools), nameof(get_entity_contextImpl), new { slug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> get_entity_contextImpl(
        string slug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.AsNoTracking()
            .Where(s => s.Slug == slug || s.NodeCode == slug)
            .Select(s => new { s.Id, s.Title })
            .FirstOrDefaultAsync();
        if (node == null) return $"Node not found: {slug}";

        var entries = entityContext.GetActiveEntities(node.Id);
        if (entries.Count == 0)
            return $"Entity context stack is empty for '{slug}'. Generate beats via ProseWriterRouter to populate it.";

        var result = entries.Select(e => new
        {
            e.EntityId,
            e.Name,
            e.EntityType,
            e.Depth,
            Score       = Math.Round(e.Score, 3),
            Description = e.Description.Length > 200 ? e.Description[..200] + "…" : e.Description,
            e.LastMentionedBeat,
            e.PushedAtBeat,
        });

        return JsonSerializer.Serialize(new
        {
            Node     = node.Title,
            NodeId   = node.Id,
            EntryCount = entries.Count,
            Entries    = result,
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Run the entity context scanner on a text snippet and return the formatted context block that would be injected into the beat prompt. Useful for testing what entities the scanner picks up from a given passage or beat goal.")]
    public Task<string> scan_entity_context(
        [Description("Node slug — context is keyed per node")] string slug,
        [Description("Text to scan (beat goal, prose excerpt, or entity name)")] string text) =>
        hub.InvokeAsync(nameof(EntityContextTools), nameof(scan_entity_contextImpl), new { slug, text });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> scan_entity_contextImpl(
        string slug,
        string text)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.AsNoTracking()
            .Where(s => s.Slug == slug || s.NodeCode == slug)
            .Select(s => new { s.Id })
            .FirstOrDefaultAsync();
        if (node == null) return $"Node not found: {slug}";

        var block = await entityContext.PrepareContextAsync(
            nodeId: node.Id,
            beatId:   Guid.Empty,
            beatGoal: text,
            sceneSoFar: "",
            ct: default);

        return string.IsNullOrWhiteSpace(block)
            ? "No entities detected in that text."
            : block;
    }

    [McpServerTool, Description("Find every beat in the narrative where a specific entity is mentioned. Returns a list grouped by node with beat number, beat handle, and a short excerpt. Useful for auditing entity coverage, finding canon moments, and reverse-navigating from entity to story.")]
    public Task<string> get_entity_beat_mentions(
        [Description("Entity ID (GUID) or entity slug")] string entityId,
        [Description("Maximum results to return (default 50)")] int limit = 50) =>
        hub.InvokeAsync(nameof(EntityContextTools), nameof(get_entity_beat_mentionsImpl), new { entityId, limit });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> get_entity_beat_mentionsImpl(
        string entityId,
        int limit = 50)
    {
        var entity = await mentionService.ResolveEntityAsync(entityId);
        if (entity == null) return $"Entity not found: {entityId}";

        var mentions = await mentionService.GetBeatsForEntityAsync(entity.Value.Id, limit);
        if (mentions.Count == 0)
            return $"No beat mentions found for '{entity.Value.Name}'. Run `prose --scan-entity-mentions` to index beat text.";

        var sb = new StringBuilder();
        sb.AppendLine($"**{entity.Value.Name}** — {mentions.Count} beat mention(s)\n");
        sb.AppendLine("| Node | Beat# | Handle | Excerpt |");
        sb.AppendLine("|--------|-------|--------|---------|");
        foreach (var m in mentions)
            sb.AppendLine($"| {m.NodeTitle} | {m.BeatNumber} | `{m.Handle}` | {m.Excerpt.Replace("|", "\\|")} |");

        return sb.ToString();
    }

    [McpServerTool, Description("Clear the entity context stack for a node. Use when starting a new writing session for a node to reset the LRU working memory.")]
    public Task<string> clear_entity_context(
        [Description("Node slug")] string slug) =>
        hub.InvokeAsync(nameof(EntityContextTools), nameof(clear_entity_contextImpl), new { slug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> clear_entity_contextImpl(
        string slug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.AsNoTracking()
            .Where(s => s.Slug == slug || s.NodeCode == slug)
            .Select(s => new { s.Id })
            .FirstOrDefaultAsync();
        if (node == null) return $"Node not found: {slug}";

        entityContext.ClearContext(node.Id);
        return $"Entity context stack cleared for '{slug}'.";
    }
}
