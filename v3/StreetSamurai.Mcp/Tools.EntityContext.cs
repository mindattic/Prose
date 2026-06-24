using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

/// <summary>
/// MCP tools for inspecting the self-referential entity context stack.
/// The stack is populated by EntityContextService during prose generation via ProseWriterRouter.
/// Use these to debug what entities are in working memory for a strand and check for canon conflicts.
/// </summary>
[McpServerToolType]
public class EntityContextTools(
    EntityContextService entityContext,
    IDbContextFactory<StreetSamuraiDbContext> dbFactory)
{
    [McpServerTool, Description("Inspect the entity working memory currently active for a strand. Shows depth-0 (directly named), depth-1 (semantic neighbors), and depth-2 (neighbors of neighbors) entities with their canon descriptions. Call after generating beats to see what was in scope.")]
    public async Task<string> get_entity_context(
        [Description("Strand slug (e.g. 'ATTE', 'BCODA')")] string slug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var strand = await db.Strands.AsNoTracking()
            .Where(s => s.Slug == slug)
            .Select(s => new { s.Id, s.Title })
            .FirstOrDefaultAsync();
        if (strand == null) return $"Strand not found: {slug}";

        var entries = entityContext.GetActiveEntities(strand.Id);
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
            Strand     = strand.Title,
            StrandId   = strand.Id,
            EntryCount = entries.Count,
            Entries    = result,
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Run the entity context scanner on a text snippet and return the formatted context block that would be injected into the beat prompt. Useful for testing what entities the scanner picks up from a given passage or beat goal.")]
    public async Task<string> scan_entity_context(
        [Description("Strand slug — context is keyed per strand")] string slug,
        [Description("Text to scan (beat goal, prose excerpt, or entity name)")] string text)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var strand = await db.Strands.AsNoTracking()
            .Where(s => s.Slug == slug)
            .Select(s => new { s.Id })
            .FirstOrDefaultAsync();
        if (strand == null) return $"Strand not found: {slug}";

        var block = await entityContext.PrepareContextAsync(
            strandId: strand.Id,
            beatId:   Guid.Empty,
            beatGoal: text,
            sceneSoFar: "",
            ct: default);

        return string.IsNullOrWhiteSpace(block)
            ? "No entities detected in that text."
            : block;
    }

    [McpServerTool, Description("Clear the entity context stack for a strand. Use when starting a new writing session for a strand to reset the LRU working memory.")]
    public async Task<string> clear_entity_context(
        [Description("Strand slug")] string slug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var strand = await db.Strands.AsNoTracking()
            .Where(s => s.Slug == slug)
            .Select(s => new { s.Id })
            .FirstOrDefaultAsync();
        if (strand == null) return $"Strand not found: {slug}";

        entityContext.ClearContext(strand.Id);
        return $"Entity context stack cleared for '{slug}'.";
    }
}
