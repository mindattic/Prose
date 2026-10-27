using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --list-books</c> — print every node as a table (or JSON). Headless
/// equivalent of the <c>/nodes</c> page. Sorted most-recently-updated first.
///
/// Args (all optional):
///   --archived          Show only archived nodes (default: only non-archived).
///   --all-archived       Show both archived and non-archived nodes.
///   --kind &lt;kind&gt;      Filter by kind (book|chapter|episode|scene|…).
///   --search &lt;text&gt;    Filter by case-insensitive substring of title or slug.
///   --limit &lt;n&gt;        Show at most N rows.
///   --scores            Sort by score descending instead of updated-at; include page estimates.
///   --json              Emit a JSON array instead of the table.
///
/// Exit codes: 0 — listed (even when empty); 1 — bad args.
/// </summary>
public static class ListNodesCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? kind = null, search = null;
        int? limit = null;
        bool json = false, scores = false, archivedOnly = false, allArchived = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--kind":   if (i + 1 < args.Length) kind = args[++i]; break;
                case "--search": if (i + 1 < args.Length) search = args[++i]; break;
                case "--limit":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var n) && n > 0) limit = n;
                    else { Console.Error.WriteLine("[list-books] --limit needs a positive integer."); return 1; }
                    break;
                case "--scores":       scores = true; break;
                case "--json":         json = true; break;
                case "--archived":     archivedOnly = true; break;
                case "--all-archived": allArchived = true; break;
            }
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var query = db.Nodes.AsNoTracking();
        if (archivedOnly)
            query = query.Where(s => s.IsArchived);
        else if (!allArchived)
            query = query.Where(s => !s.IsArchived);
        if (!string.IsNullOrWhiteSpace(kind))
            query = query.Where(s => s.Kind == kind);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(s => s.Title.ToLower().Contains(term) || s.Slug.ToLower().Contains(term));
        }

        query = scores
            ? query.OrderBy(s => s.Score == null ? 1 : 0).ThenByDescending(s => s.Score ?? 0)
            : query.OrderByDescending(s => s.UpdatedAt);
        if (limit is int lim) query = query.Take(lim);

        var nodes = await query
            .Select(s => new { s.Id, s.Title, s.Slug, s.Kind, s.UniverseId, s.Timeline, s.IsArchived, s.NodeCode, s.Score, s.UpdatedAt, BeatCount = s.BeatNodes.Count(nb => true) })
            .ToListAsync();

        // Word / page count — one join instead of N queries
        var ids = nodes.Select(s => s.Id).ToList();
        var charCounts = await db.BeatNodes
            .AsNoTracking()
            .Where(sb => ids.Contains(sb.NodeId))
            .Join(db.Beats.AsNoTracking().Where(b => b.Text != null && b.Text != ""),
                  sb => sb.BeatId, b => b.Id, (sb, b) => new { sb.NodeId, b.Text })
            .GroupBy(x => x.NodeId)
            .Select(g => new { NodeId = g.Key, Chars = g.Sum(x => (long)x.Text!.Length) })
            .ToDictionaryAsync(x => x.NodeId);

        var rows = nodes.Select(s =>
        {
            charCounts.TryGetValue(s.Id, out var cc);
            var words = cc != null ? (int)(cc.Chars / 5.2) : 0;
            return new Row(s.Id, s.Title, s.Slug, s.Kind, s.UniverseId, s.Timeline, s.IsArchived, s.NodeCode, s.BeatCount, s.Score, words / 250, s.UpdatedAt);
        }).ToList();

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        if (rows.Count == 0)
        {
            Console.WriteLine("[list-books] No nodes match.");
            return 0;
        }

        // ── table ──
        Console.WriteLine($"{"CODE",-6}  {"TIMELINE",-9}  {"KIND",-10}  {"PG",4}  {"SCORE",6}  TITLE");
        Console.WriteLine(new string('-', 110));
        foreach (var r in rows)
        {
            var code  = Trunc(r.Code ?? "—", 6);
            var score = r.Score is double sc ? $"{sc,5:F0}%" : "     —";
            var timeline = r.Timeline is decimal tl ? tl.ToString("0.######") : "—";
            var title = r.IsArchived ? $"{r.Title} [archived]" : r.Title;
            Console.WriteLine(
                $"{code,-6}  {Trunc(timeline, 9),-9}  {Trunc(r.Kind, 10),-10}  {r.Pages,4}  {score,6}  {title}");
        }
        Console.WriteLine(new string('-', 110));
        Console.WriteLine($"[list-books] {rows.Count} node(s).");
        return 0;
    }

    private static string Trunc(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    private record Row(
        Guid Id, string Title, string Slug, string Kind, Guid UniverseId, decimal? Timeline, bool IsArchived,
        string? Code, int BeatCount, double? Score, int Pages, DateTime UpdatedAt);
}
