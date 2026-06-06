using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --list-strands</c> — print every strand as a table (or JSON). Headless
/// equivalent of the <c>/strands</c> page. Sorted most-recently-updated first.
///
/// Args (all optional):
///   --status &lt;status&gt;  Filter by status (draft|generating|narrating|ready|failed|stopped).
///   --kind &lt;kind&gt;      Filter by kind (book|chapter|episode|scene|…).
///   --search &lt;text&gt;    Filter by case-insensitive substring of title or slug.
///   --limit &lt;n&gt;        Show at most N rows.
///   --json              Emit a JSON array instead of the table.
///
/// Exit codes: 0 — listed (even when empty); 1 — bad args.
/// </summary>
public static class ListStrandsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? status = null, kind = null, search = null;
        int? limit = null;
        bool json = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--status": if (i + 1 < args.Length) status = args[++i]; break;
                case "--kind":   if (i + 1 < args.Length) kind = args[++i]; break;
                case "--search": if (i + 1 < args.Length) search = args[++i]; break;
                case "--limit":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var n) && n > 0) limit = n;
                    else { Console.Error.WriteLine("[list-strands] --limit needs a positive integer."); return 1; }
                    break;
                case "--json": json = true; break;
            }
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var query = db.Strands.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status);
        if (!string.IsNullOrWhiteSpace(kind))
            query = query.Where(s => s.Kind == kind);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(s => s.Title.ToLower().Contains(term) || s.Slug.ToLower().Contains(term));
        }

        query = query.OrderByDescending(s => s.UpdatedAt);
        if (limit is int lim) query = query.Take(lim);

        // Project to a flat row + count beats in the same round-trip.
        var rows = await query
            .Select(s => new Row(
                s.Id,
                s.Title,
                s.Slug,
                s.Kind,
                s.Status,
                s.StrandBeats.Count,
                s.Score,
                s.UpdatedAt))
            .ToListAsync();

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        if (rows.Count == 0)
        {
            Console.WriteLine("[list-strands] No strands match.");
            return 0;
        }

        // ── table ──
        Console.WriteLine($"{"ID",-8}  {"STATUS",-10}  {"KIND",-10}  {"BEATS",5}  {"SCORE",6}  {"UPDATED",-16}  TITLE (slug)");
        Console.WriteLine(new string('-', 100));
        foreach (var r in rows)
        {
            var shortId = r.Id.ToString("N")[..8];
            var score = r.Score is double sc ? $"{sc,5:F0}%" : "    —";
            var updated = r.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            Console.WriteLine(
                $"{shortId,-8}  {Trunc(r.Status, 10),-10}  {Trunc(r.Kind, 10),-10}  {r.BeatCount,5}  {score,6}  {updated,-16}  {r.Title} ({r.Slug})");
        }
        Console.WriteLine(new string('-', 100));
        Console.WriteLine($"[list-strands] {rows.Count} strand(s).");
        return 0;
    }

    private static string Trunc(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    private record Row(
        Guid Id, string Title, string Slug, string Kind, string Status,
        int BeatCount, double? Score, DateTime UpdatedAt);
}
