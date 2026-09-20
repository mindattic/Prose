using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --beat-positions [--slug &lt;s&gt;] [--all] [--dry] [--json]</c>
///
/// <para>Stamps <c>Beat.StoryPosition</c> — a book's reading order as a number, which is the
/// engine's authoritative story clock (author ruling 2026-09-04: track time in beats; keep the
/// wall clock only for day/night alignment and short timers that have to add up).</para>
///
/// <para>Deterministic and FREE: it reads the order
/// <c>NodeWorkbenchService.GetOrderedBeatsAsync</c> already defines and writes an integer. No LLM
/// call, no prose touched, and beats are not marked dirty — a position is bookkeeping about where
/// a beat sits, so stamping it must not invalidate the engine's hash-gated audits.</para>
///
/// <para>Run it after anything that changes reading order: inserting, deleting, splitting or
/// reordering beats, or reparenting a chapter. Positions are recomputed wholesale, so re-running
/// is always safe.</para>
///
/// <para><c>--all</c> is CORPUS-WIDE, every universe, and needs no <c>--universe</c> scope — a
/// beat's position in its own book is structural. See
/// <see cref="BeatStoryPositionService.StampAllAsync"/> for why that is not a scoping oversight.</para>
/// </summary>
public static class BeatPositionsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var svc = services.GetRequiredService<BeatStoryPositionService>();
        var dry = args.Contains("--dry");
        var all = args.Contains("--all");
        var slug = Flag(args, "--slug") ?? Flag(args, "--code") ?? Flag(args, "--id");

        if (!all && string.IsNullOrWhiteSpace(slug))
        {
            var (total, stamped) = await svc.CoverageAsync();
            var pct = total == 0 ? 0 : stamped * 100.0 / total;
            Console.WriteLine($"  Beat story positions: {stamped:N0} / {total:N0} stamped ({pct:F1}%)");
            if (stamped < total)
            {
                Console.WriteLine("  An unstamped beat has NO position on the story timeline, and every " +
                                  "consumer must fall back rather than guess — which is exactly how a " +
                                  "world-state query used to answer with the end of the book.");
                Console.WriteLine();
                var groups = await svc.UnstampedByNodeAsync();
                if (groups.Count > 0)
                {
                    Console.WriteLine("  Nodes holding unstamped beats (these have a BeatNodes row, so they are NOT");
                    Console.WriteLine("  orphans — they are simply not reached by any book's reading order):");
                    foreach (var g in groups)
                        Console.WriteLine($"    {g.Beats,6}  [{g.NodeKind,-8}] {Clip(g.NodeTitle, 52),-52} " +
                                          $"{(g.HasBookAncestor ? "" : "NO BOOK ANCESTOR")}");
                    var unreachable = groups.Where(g => !g.HasBookAncestor).Sum(g => g.Beats);
                    if (unreachable > 0)
                        Console.WriteLine($"    → {unreachable:N0} beat(s) sit under a node with no book above it. " +
                                          "That is the shape that hid BCODA's Ghost Period.");
                }
            }
            Console.WriteLine();
            Console.WriteLine("Usage: prose --beat-positions (--slug <slug-or-code-or-id> | --all) [--dry] [--json]");
            Console.WriteLine("  --dry   report what would change; writes nothing. Free either way.");
            return 0;
        }

        if (all)
        {
            var report = await svc.StampAllAsync(apply: !dry);
            if (args.Contains("--json"))
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                    report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                return 0;
            }
            foreach (var b in report.Books.Where(b => b.Changed > 0))
                Console.WriteLine($"  {b.Title,-44} {b.Changed,6} of {b.Beats,6} beat(s)");
            Console.WriteLine();
            Console.WriteLine($"[beat-positions] {report.TotalChanged:N0} beat(s) across {report.BooksTouched} " +
                              $"book(s) {(dry ? "would be" : "were")} stamped; {report.TotalBeats:N0} beat(s) walked.");
            return 0;
        }

        var dbFactory = services.GetRequiredService<
            Microsoft.EntityFrameworkCore.IDbContextFactory<Prose.Core.Data.ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = await NodeRefResolver.ResolveAsync(db, slug!);
        if (nodeId == null) { Console.Error.WriteLine($"[beat-positions] No node matched '{slug}'."); return 2; }

        var one = await svc.StampBookAsync(nodeId.Value, apply: !dry);
        Console.WriteLine($"{one.Title}  [{one.Slug}]");
        Console.WriteLine($"  {one.Beats} beat(s) in reading order; {one.Changed} position(s) " +
                          $"{(dry ? "would change" : "stamped")}.");
        if (dry) Console.WriteLine("[beat-positions] Dry run — nothing written.");
        return 0;
    }

    private static string Clip(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..(max - 1)] + "…";

    private static string? Flag(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
