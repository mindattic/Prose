using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

/// <summary>
/// Read-only connection questions over entity tags in beats and live Edges (see GraphQueryService).
/// CLI twin: prose --graph-query.
/// </summary>
[McpServerToolType]
public class GraphQueryTools(IDbContextFactory<ProseDbContext> dbFactory, HubInvoker hub)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    [McpServerTool, Description("Every beat where two entities are both tagged (e.g. two characters sharing a scene, or a character and a motif). With a book, results are in that book's reading order. Read-only; derived from the entity tags the prose carries.")]
    public Task<string> co_occurrence(
        [Description("First entity GUID or slug.")] string a,
        [Description("Second entity GUID or slug.")] string b,
        [Description("Optional book GUID, slug or NodeCode to restrict to and order by.")] string? book = null,
        [Description("Maximum beats (default 100).")] int limit = 100) =>
        hub.InvokeAsync(nameof(GraphQueryTools), nameof(co_occurrenceImpl), new { a, b, book, limit });

    public async Task<string> co_occurrenceImpl(string a, string b, string? book = null, int limit = 100)
    {
        var (ea, eb, error) = await ResolvePairAsync(a, b);
        if (error != null) return error;
        Guid? bookId = null;
        if (!string.IsNullOrWhiteSpace(book))
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var node = await NodeRefResolver.ResolveNodeAsync(db, book);
            if (node == null) return Err("book_not_found", book);
            bookId = node.Id;
        }
        var hits = await new GraphQueryService(dbFactory).CoOccurrencesAsync(ea.Id, eb.Id, bookId, limit);
        return JsonSerializer.Serialize(new { a = ea.Name, b = eb.Name, readingOrder = bookId != null, count = hits.Count, beats = hits }, Json);
    }

    [McpServerTool, Description("Entities that have a live relationship edge to BOTH given entities (common allies, shared employer, a place both frequent), with each side's relation type. Read-only.")]
    public Task<string> shared_neighbors(
        [Description("First entity GUID or slug.")] string a,
        [Description("Second entity GUID or slug.")] string b) =>
        hub.InvokeAsync(nameof(GraphQueryTools), nameof(shared_neighborsImpl), new { a, b });

    public async Task<string> shared_neighborsImpl(string a, string b)
    {
        var (ea, eb, error) = await ResolvePairAsync(a, b);
        if (error != null) return error;
        var shared = await new GraphQueryService(dbFactory).SharedNeighborsAsync(ea.Id, eb.Id);
        return JsonSerializer.Serialize(new { a = ea.Name, b = eb.Name, count = shared.Count, neighbors = shared }, Json);
    }

    [McpServerTool, Description("Shortest chain of live relationship edges connecting two entities (either direction), e.g. how a minor character is tied to the antagonist. Returns null path when none exists within maxHops. Read-only.")]
    public Task<string> path_between(
        [Description("Start entity GUID or slug.")] string a,
        [Description("End entity GUID or slug.")] string b,
        [Description("Maximum hops (default 4).")] int maxHops = 4) =>
        hub.InvokeAsync(nameof(GraphQueryTools), nameof(path_betweenImpl), new { a, b, maxHops });

    public async Task<string> path_betweenImpl(string a, string b, int maxHops = 4)
    {
        var (ea, eb, error) = await ResolvePairAsync(a, b);
        if (error != null) return error;
        var path = await new GraphQueryService(dbFactory).PathBetweenAsync(ea.Id, eb.Id, maxHops);
        return JsonSerializer.Serialize(new { a = ea.Name, b = eb.Name, maxHops, hops = path?.Count, path }, Json);
    }

    [McpServerTool, Description("Who and what is in a beat or a whole chapter: every tagged entity (with how many beats it appears in) and every live relationship edge among them. Read-only.")]
    public Task<string> beat_cast(
        [Description("A beat GUID, or a chapter GUID/slug/NodeCode.")] string beatOrChapter) =>
        hub.InvokeAsync(nameof(GraphQueryTools), nameof(beat_castImpl), new { beatOrChapter });

    public async Task<string> beat_castImpl(string beatOrChapter)
    {
        var graph = new GraphQueryService(dbFactory);
        List<Guid> beatIds;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            if (Guid.TryParse(beatOrChapter, out var id) && await db.Beats.AsNoTracking().AnyAsync(b => b.Id == id))
                beatIds = [id];
            else
            {
                var node = await NodeRefResolver.ResolveNodeAsync(db, beatOrChapter);
                if (node == null) return Err("not_found", beatOrChapter);
                beatIds = await graph.BeatIdsOfNodeAsync(node.Id);
            }
        }
        var (cast, links) = await graph.CastAsync(beatIds);
        return JsonSerializer.Serialize(new { beats = beatIds.Count, cast, links }, Json);
    }

    private async Task<((Guid Id, string Name) A, (Guid Id, string Name) B, string? Error)> ResolvePairAsync(string a, string b)
    {
        var mentions = new EntityMentionService(dbFactory);
        var ea = await mentions.ResolveEntityAsync(a);
        if (ea == null) return (default, default, Err("entity_not_resolved", a));
        var eb = await mentions.ResolveEntityAsync(b);
        if (eb == null) return (default, default, Err("entity_not_resolved", b));
        return (ea.Value, eb.Value, null);
    }

    private static string Err(string error, string value) =>
        JsonSerializer.Serialize(new { ok = false, error, value, hint = "pass the GUID when a slug is shared or missing" });
}
