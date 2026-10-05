using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --universe &lt;u&gt; --graph-query co-occurrence --a &lt;id|slug&gt; --b &lt;id|slug&gt; [--book &lt;slug&gt;] [--limit N]
/// prose --universe &lt;u&gt; --graph-query shared-neighbors --a &lt;id|slug&gt; --b &lt;id|slug&gt;
/// prose --universe &lt;u&gt; --graph-query path --a &lt;id|slug&gt; --b &lt;id|slug&gt; [--max-hops N]
/// prose --universe &lt;u&gt; --graph-query cast (--beat &lt;guid&gt; | --chapter &lt;id|slug&gt;)
///
/// Read-only answers over entity tags in beats and live Edges. See <see cref="GraphQueryService"/>.
/// </summary>
public static class GraphQueryCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var mode = Flag(args, "--graph-query");
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var graph = new GraphQueryService(dbFactory);
        var mentions = new EntityMentionService(dbFactory);

        if (mode == "cast") return await CastAsync(args, services, graph);

        var a = await ResolveAsync(mentions, Flag(args, "--a"), "--a");
        var b = await ResolveAsync(mentions, Flag(args, "--b"), "--b");
        if (a == null || b == null) return Usage();

        switch (mode)
        {
            case "co-occurrence":
            {
                Guid? bookId = null;
                if (Flag(args, "--book") is { } bookRef)
                {
                    await using var db = await services.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
                    var book = await NodeRefResolver.ResolveNodeAsync(db, bookRef);
                    if (book == null) { Console.Error.WriteLine($"Book not found: {bookRef}"); return 1; }
                    bookId = book.Id;
                }
                var limit = int.TryParse(Flag(args, "--limit"), out var l) ? l : 100;
                var hits = await graph.CoOccurrencesAsync(a.Value.Id, b.Value.Id, bookId, limit);
                foreach (var h in hits)
                    Console.WriteLine($"  #{h.Ordinal,-5} {h.ChapterTitle}  [{h.BeatId}]\n         {h.Excerpt}");
                Console.WriteLine($"[graph-query] {hits.Count} beat(s) tag both {a.Value.Name} and {b.Value.Name}" +
                    (bookId != null ? " (reading order)." : "."));
                return 0;
            }
            case "shared-neighbors":
            {
                var shared = await graph.SharedNeighborsAsync(a.Value.Id, b.Value.Id);
                foreach (var n in shared)
                    Console.WriteLine($"  {n.Name} [{n.EntityType}]  {a.Value.Name}: {n.RelationFromA}  |  {b.Value.Name}: {n.RelationFromB}");
                Console.WriteLine($"[graph-query] {shared.Count} entit(ies) linked to both.");
                return 0;
            }
            case "path":
            {
                var maxHops = int.TryParse(Flag(args, "--max-hops"), out var m) ? m : 4;
                var path = await graph.PathBetweenAsync(a.Value.Id, b.Value.Id, maxHops);
                if (path == null)
                {
                    Console.WriteLine($"[graph-query] No path from {a.Value.Name} to {b.Value.Name} within {maxHops} hop(s).");
                    return 0;
                }
                foreach (var link in path)
                    Console.WriteLine($"  {link.SourceName} —{link.RelationType}→ {link.TargetName}" +
                        (string.IsNullOrWhiteSpace(link.Description) ? "" : $"  ({link.Description})"));
                Console.WriteLine($"[graph-query] {path.Count} hop(s).");
                return 0;
            }
            default:
                return Usage();
        }
    }

    static async Task<int> CastAsync(string[] args, IServiceProvider services, GraphQueryService graph)
    {
        List<Guid> beatIds;
        if (Guid.TryParse(Flag(args, "--beat"), out var beatId)) beatIds = [beatId];
        else if (Flag(args, "--chapter") is { } chapterRef)
        {
            await using var db = await services.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
            var chapter = await NodeRefResolver.ResolveNodeAsync(db, chapterRef);
            if (chapter == null) { Console.Error.WriteLine($"Chapter not found: {chapterRef}"); return 1; }
            beatIds = await graph.BeatIdsOfNodeAsync(chapter.Id);
        }
        else return Usage();

        var (cast, links) = await graph.CastAsync(beatIds);
        foreach (var c in cast) Console.WriteLine($"  {c.Name} [{c.EntityType}]  in {c.Beats} beat(s)");
        if (links.Count > 0) Console.WriteLine("  links:");
        foreach (var link in links) Console.WriteLine($"    {link.SourceName} —{link.RelationType}→ {link.TargetName}");
        Console.WriteLine($"[graph-query] {cast.Count} tagged entit(ies), {links.Count} link(s) among them, over {beatIds.Count} beat(s).");
        return 0;
    }

    static async Task<(Guid Id, string Name)?> ResolveAsync(EntityMentionService mentions, string? idOrSlug, string flag)
    {
        if (string.IsNullOrWhiteSpace(idOrSlug)) return null;
        var hit = await mentions.ResolveEntityAsync(idOrSlug);
        if (hit == null) Console.Error.WriteLine($"[graph-query] {flag} '{idOrSlug}' did not resolve to exactly one entity — pass the GUID.");
        return hit;
    }

    static int Usage()
    {
        Console.Error.WriteLine(
            "Usage: prose --universe <u> --graph-query co-occurrence|shared-neighbors|path --a <id|slug> --b <id|slug> [--book <slug>] [--max-hops N]\n" +
            "       prose --universe <u> --graph-query cast (--beat <guid> | --chapter <id|slug>)");
        return 2;
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
