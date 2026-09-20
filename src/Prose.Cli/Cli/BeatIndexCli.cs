using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --beat-index --slug &lt;s&gt; [--json] [--tsv]</c>
///
/// <para>Dumps one book's per-beat METADATA in reading order — everything the engine already
/// knows about a beat except its prose. Read-only, deterministic, FREE: no LLM call, no write,
/// nothing marked dirty.</para>
///
/// <para>This exists because <c>Beat.PlaceName</c> / <c>Beat.PlaceEntityId</c> had no read path
/// at all. <c>--extract-beat-locations</c> writes them and <c>BeatPlaceService</c> resolves them
/// to canon Places, but no CLI or MCP tool would show you the result, so the one field that says
/// WHERE a beat happens was write-only. The same was true of <c>SceneType</c>, <c>StructureRole</c>
/// and <c>Act</c>: all three are on the row, none were readable in bulk.</para>
///
/// <para>The summary-trust columns matter more than they look. <c>Beat.Description</c> is
/// authorial intent and <c>Beat.EventSummary</c> is what the prose was observed to do; each
/// carries a hash of the text it was written against, so a beat reports one of three states —
/// absent, trustworthy (hash matches <c>TextHash</c>), or provably STALE (hash differs, i.e. the
/// prose changed underneath it). A stale summary is worse than a missing one, because every
/// consumer downstream treats it as current.</para>
/// </summary>
public static class BeatIndexCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var slug = Flag(args, "--slug") ?? Flag(args, "--code") ?? Flag(args, "--id");
        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: prose --beat-index --slug <slug-or-code-or-id> [--json] [--tsv]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();

        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = await NodeRefResolver.ResolveAsync(db, slug!);
        if (nodeId == null) { Console.Error.WriteLine($"[beat-index] {NodeRefResolver.NotFoundMessage(slug!)}"); return 2; }

        var book = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Id == nodeId.Value);
        if (book == null) { Console.Error.WriteLine("[beat-index] Node not found."); return 2; }

        var ordered = await workbench.GetOrderedBeatsAsync(nodeId.Value);
        if (ordered.Count == 0) { Console.Error.WriteLine($"[beat-index] '{book.Title}' has no beats."); return 1; }

        // Chapter titles for the beats' owning nodes, in one query rather than per beat.
        var chapterIds = ordered.Select(o => o.NodeId).Distinct().ToList();
        var chapterTitles = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => chapterIds.Contains(n.Id))
            .Select(n => new { n.Id, n.Title })
            .ToDictionaryAsync(n => n.Id, n => n.Title ?? "");

        // Reading-order ordinal per chapter, assigned on first sighting so it matches the spine.
        var chapterOrdinal = new Dictionary<Guid, int>();
        foreach (var o in ordered)
            if (!chapterOrdinal.ContainsKey(o.NodeId)) chapterOrdinal[o.NodeId] = chapterOrdinal.Count + 1;

        var rows = new List<Row>(ordered.Count);
        var seenInChapter = new Dictionary<Guid, int>();
        foreach (var o in ordered)
        {
            var b = o.Beat;
            seenInChapter[o.NodeId] = seenInChapter.GetValueOrDefault(o.NodeId) + 1;
            rows.Add(new Row(
                Position:       b.StoryPosition,
                ChapterOrdinal: chapterOrdinal[o.NodeId],
                ChapterTitle:   chapterTitles.GetValueOrDefault(o.NodeId, ""),
                ChapterNodeId:  o.NodeId,
                BeatInChapter:  seenInChapter[o.NodeId],
                BeatId:         b.Id,
                Number:         b.Number,
                SortKey:        o.SortKey,
                Kind:           b.Kind ?? "",
                Chars:          b.Text?.Length ?? 0,
                Title:          b.Title ?? "",
                PlaceName:      b.PlaceName ?? "",
                PlaceResolved:  b.PlaceEntityId != null,
                SceneType:      b.SceneType ?? "",
                StructureRole:  b.StructureRole ?? "",
                Act:            b.Act,
                Description:    Trust(b.Description, b.DescriptionHash, b.TextHash),
                EventSummary:   Trust(b.EventSummary, b.EventSummaryHash, b.TextHash),
                IsChapterStart: b.IsChapterStart));
        }

        if (args.Contains("--json"))
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                new { book = book.Title, slug = book.Slug, code = book.NodeCode, beats = rows.Count, rows },
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        if (args.Contains("--tsv"))
        {
            Console.WriteLine("pos\tch\tbeat\tbeat_id\tnum\tkind\tchars\tplace\tplace_resolved\t" +
                              "scene_type\tstructure_role\tact\tdescription\tevent_summary\tchapter");
            foreach (var r in rows)
                Console.WriteLine($"{r.Position}\t{r.ChapterOrdinal}\t{r.BeatInChapter}\t{r.BeatId}\t{r.Number}\t" +
                                  $"{r.Kind}\t{r.Chars}\t{r.PlaceName}\t{(r.PlaceResolved ? "canon" : "")}\t" +
                                  $"{r.SceneType}\t{r.StructureRole}\t{r.Act}\t{r.Description}\t{r.EventSummary}\t" +
                                  $"{r.ChapterTitle}");
            return 0;
        }

        Console.WriteLine($"{book.Title}  [{book.Slug}]  {rows.Count} beats");
        Console.WriteLine();
        Console.WriteLine($"{"pos",5} {"ch",3} {"#",4} {"chars",6}  {"place",-34} {"desc",-6} {"event",-6} kind");
        foreach (var r in rows)
            Console.WriteLine($"{r.Position,5} {r.ChapterOrdinal,3} {r.BeatInChapter,4} {r.Chars,6}  " +
                              $"{Clip(r.PlaceName, 34),-34} {r.Description,-6} {r.EventSummary,-6} {r.Kind}");

        Console.WriteLine();
        Console.WriteLine($"  place named        : {rows.Count(r => r.PlaceName.Length > 0)} / {rows.Count}");
        Console.WriteLine($"  place canon-linked : {rows.Count(r => r.PlaceResolved)} / {rows.Count}");
        Console.WriteLine($"  description        : {Tally(rows.Select(r => r.Description))}");
        Console.WriteLine($"  event summary      : {Tally(rows.Select(r => r.EventSummary))}");
        Console.WriteLine($"  scene type set     : {rows.Count(r => r.SceneType.Length > 0)} / {rows.Count}");
        Console.WriteLine($"  chars/beat         : min {rows.Min(r => r.Chars)}  median " +
                          $"{rows.OrderBy(r => r.Chars).ElementAt(rows.Count / 2).Chars}  max {rows.Max(r => r.Chars)}  " +
                          $"(engine optimal band 4000-7500)");
        return 0;
    }

    /// <summary>
    /// A summary is only as good as the text it was written against. Absent, matching the beat's
    /// current TextHash (trustworthy), or written against prose that has since changed (STALE).
    /// </summary>
    private static string Trust(string? value, string? valueHash, string? textHash) =>
        string.IsNullOrWhiteSpace(value) ? "-"
        : string.IsNullOrEmpty(valueHash) ? "unver"
        : string.Equals(valueHash, textHash, StringComparison.Ordinal) ? "ok"
        : "STALE";

    private static string Tally(IEnumerable<string> states)
    {
        var g = states.GroupBy(s => s).ToDictionary(x => x.Key, x => x.Count());
        return string.Join("  ", new[] { "ok", "STALE", "unver", "-" }
            .Where(k => g.ContainsKey(k)).Select(k => $"{k}={g[k]}"));
    }

    private static string Clip(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..(max - 1)] + "…";

    private static string? Flag(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private record Row(
        int? Position, int ChapterOrdinal, string ChapterTitle, Guid ChapterNodeId, int BeatInChapter,
        Guid BeatId, int Number, double SortKey, string Kind, int Chars, string Title,
        string PlaceName, bool PlaceResolved, string SceneType, string StructureRole, int Act,
        string Description, string EventSummary, bool IsChapterStart);
}
