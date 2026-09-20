using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --edit-distribution [--slug &lt;slug&gt;] [--top N] [--json]
///
/// RFC 0009 Phase 0 — measure before cutting. Read-only, deterministic, zero LLM cost.
///
/// <para><c>Beat.Version</c> is a monotonic counter bumped by every <c>UpdateBeatTextAsync</c>.
/// A finished beat should sit at a low, explicable number. This prints the histogram — per book
/// and corpus-wide — plus the most-rewritten beats, so the question "how many times has finished
/// prose been rewritten, and where" is answered by the data rather than asserted. It is the
/// baseline that must not rise again once the autonomous rewriters in RFC 0009 §3 are deleted.</para>
///
/// <para>Corpus-wide by design when no <c>--slug</c> is given (IgnoreQueryFilters — the point is
/// to measure the whole corpus, not one universe). Walks each book's leaf descendants recursively
/// via <see cref="NodeWorkbenchService.GetLeafDescendantIdsAsync"/>, never a flat ParentNodeId
/// query, per the Book→Chapter→Beat hard rule.</para>
/// </summary>
public static class EditDistributionCli
{
    private sealed record BeatRow(int Number, int Version, bool WasCorrected, DateTime CreatedAt, DateTime UpdatedAt);

    private sealed record BookRow(
        string Code, string Title, string Universe,
        int Beats, int Edited, int Corrected,
        double MeanVersion, int MaxVersion, int TotalWrites,
        int[] Buckets, IReadOnlyList<BeatRow> Top);

    // Version buckets: 0 · 1 · 2-3 · 4-9 · 10+
    private static readonly string[] BucketLabels = ["0", "1", "2-3", "4-9", "10+"];
    private static int BucketOf(int v) => v <= 0 ? 0 : v == 1 ? 1 : v <= 3 ? 2 : v <= 9 ? 3 : 4;

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        int top = 10;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[++i];
            else if (args[i] == "--top" && i + 1 < args.Length && int.TryParse(args[i + 1], out var t)) { top = t; i++; }
        }
        bool json = args.Contains("--json");

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var booksQ = db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .OfType<BookNode>()
            .Where(n => n.Status != "archived");
        if (slug != null)
            booksQ = booksQ.Where(n => n.Slug == slug || n.NodeCode == slug);

        var books = await booksQ
            .Select(n => new { n.Id, n.NodeCode, n.Title, n.Slug, n.UniverseId })
            .ToListAsync();

        if (books.Count == 0)
        {
            Console.Error.WriteLine(slug == null
                ? "[edit-distribution] No books found."
                : $"[edit-distribution] No book matched '{slug}'.");
            return 2;
        }

        var universeNames = await db.Universes.AsNoTracking().IgnoreQueryFilters()
            .Select(u => new { u.Id, u.Slug })
            .ToDictionaryAsync(u => u.Id, u => u.Slug);

        var rows = new List<BookRow>();
        foreach (var b in books)
        {
            var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, b.Id);
            var beats = await db.BeatNodes.AsNoTracking()
                .Where(bn => leafIds.Contains(bn.NodeId))
                .Join(db.Beats, bn => bn.BeatId, bt => bt.Id,
                      (bn, bt) => new { bt.Number, bt.Version, bt.WasCorrected, bt.CreatedAt, bt.UpdatedAt, bt.Text })
                .Where(x => x.Text != null && x.Text != "")
                .ToListAsync();

            if (beats.Count == 0) continue;

            var brs = beats.Select(x => new BeatRow(x.Number, x.Version, x.WasCorrected, x.CreatedAt, x.UpdatedAt)).ToList();
            var buckets = new int[BucketLabels.Length];
            foreach (var r in brs) buckets[BucketOf(r.Version)]++;

            rows.Add(new BookRow(
                b.NodeCode ?? b.Slug ?? "", b.Title ?? "",
                universeNames.GetValueOrDefault(b.UniverseId, "?"),
                brs.Count,
                brs.Count(r => r.Version > 0),
                brs.Count(r => r.WasCorrected),
                brs.Average(r => r.Version),
                brs.Max(r => r.Version),
                brs.Sum(r => r.Version),
                buckets,
                brs.OrderByDescending(r => r.Version).ThenByDescending(r => r.UpdatedAt).Take(top).ToList()));
        }

        if (json)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(rows,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        // ── corpus table ──────────────────────────────────────────────────
        var ordered = rows.OrderByDescending(r => r.TotalWrites).ToList();
        Console.WriteLine("Beat.Version distribution — every UpdateBeatTextAsync bumps it by one; 0 = never rewritten after creation.");
        Console.WriteLine();
        Console.WriteLine($"  {"BOOK",-8} {"UNIV",-6} {"BEATS",6} {"EDITED",7} {"CORR",5} {"WRITES",7} {"MEAN",6} {"MAX",4}   v=0    v=1   v=2-3  v=4-9  v=10+");
        foreach (var r in ordered)
        {
            Console.WriteLine($"  {Trunc(r.Code, 8),-8} {Trunc(r.Universe, 6),-6} {r.Beats,6} {r.Edited,7} {r.Corrected,5} {r.TotalWrites,7} {r.MeanVersion,6:0.00} {r.MaxVersion,4}   "
                              + string.Join(" ", r.Buckets.Select(c => $"{c,5}")));
        }

        var allBeats = rows.Sum(r => r.Beats);
        var allEdited = rows.Sum(r => r.Edited);
        var allWrites = rows.Sum(r => r.TotalWrites);
        var allCorr = rows.Sum(r => r.Corrected);
        var allBuckets = new int[BucketLabels.Length];
        foreach (var r in rows) for (int i = 0; i < allBuckets.Length; i++) allBuckets[i] += r.Buckets[i];
        Console.WriteLine();
        Console.WriteLine($"  {"TOTAL",-8} {rows.Count,3} bk {allBeats,6} {allEdited,7} {allCorr,5} {allWrites,7} {(allBeats == 0 ? 0 : (double)allWrites / allBeats),6:0.00} {rows.Max(r => r.MaxVersion),4}   "
                          + string.Join(" ", allBuckets.Select(c => $"{c,5}")));
        Console.WriteLine();
        Console.WriteLine($"  {allEdited} of {allBeats} beats with prose ({Pct(allEdited, allBeats)}) have been rewritten at least once after creation; "
                          + $"{allBuckets[3] + allBuckets[4]} ({Pct(allBuckets[3] + allBuckets[4], allBeats)}) four or more times. "
                          + $"{allWrites} rewrites in total. WasCorrected is set on {allCorr}.");

        // ── most-rewritten beats ──────────────────────────────────────────
        var focus = slug != null ? rows : ordered.Take(3).ToList();
        foreach (var r in focus)
        {
            Console.WriteLine();
            Console.WriteLine($"  {r.Code} — {r.Title}: {Math.Min(top, r.Top.Count)} most-rewritten beat(s)");
            foreach (var t in r.Top)
                Console.WriteLine($"    beat #{t.Number,-6} v{t.Version,-4} {(t.WasCorrected ? "corrected " : "          ")}created {t.CreatedAt:yyyy-MM-dd}  last write {t.UpdatedAt:yyyy-MM-dd HH:mm}");
        }
        return 0;
    }

    private static string Trunc(string s, int n) => s.Length <= n ? s : s[..n];
    private static string Pct(int a, int b) => b == 0 ? "0%" : $"{100.0 * a / b:0.#}%";
}
