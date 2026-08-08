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
    IDbContextFactory<ProseDbContext> dbFactory)
{
    [McpServerTool, Description("Inspect the entity working memory currently active for a node. Shows depth-0 (directly named), depth-1 (semantic neighbors), and depth-2 (neighbors of neighbors) entities with their canon descriptions. Call after generating beats to see what was in scope.")]
    public async Task<string> get_entity_context(
        [Description("Node slug (e.g. 'ATTE', 'BCODA')")] string slug)
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
    public async Task<string> scan_entity_context(
        [Description("Node slug — context is keyed per node")] string slug,
        [Description("Text to scan (beat goal, prose excerpt, or entity name)")] string text)
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
    public async Task<string> get_entity_beat_mentions(
        [Description("Entity ID (GUID) or entity slug")] string entityId,
        [Description("Maximum results to return (default 50)")] int limit = 50)
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
    public async Task<string> clear_entity_context(
        [Description("Node slug")] string slug)
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
