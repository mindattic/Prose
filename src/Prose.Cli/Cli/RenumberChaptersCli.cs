using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --renumber-chapters --slug &lt;slug|id&gt; [--apply]</c> — make a book's numbered
/// chapters run consecutively in reading order. Titles only; no prose is read or written.
/// Dry run unless <c>--apply</c>.
/// </summary>
public static class RenumberChaptersCli
{
    private static readonly string[] KnownFlags = ["--renumber-chapters", "--slug", "--node", "--apply", "--universe"];

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var unknown = args.Where(a => a.StartsWith("--", StringComparison.Ordinal) && !KnownFlags.Contains(a)).ToList();
        if (unknown.Count > 0)
        {
            Console.Error.WriteLine($"unknown flag(s): {string.Join(", ", unknown)}");
            return 2;
        }

        var slug = ArgValue(args, "--slug") ?? ArgValue(args, "--node");
        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("usage: prose --renumber-chapters --slug <slug|id> [--apply]");
            return 2;
        }
        var apply = args.Contains("--apply");

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        Guid bookId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var node = Guid.TryParse(slug, out var parsed)
                ? await db.Nodes.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == parsed)
                : await db.Nodes.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Slug == slug);
            if (node == null) { Console.Error.WriteLine($"node not found: {slug}"); return 1; }
            bookId = node.Id;
        }

        var report = await services.GetRequiredService<ChapterRenumberService>().RenumberAsync(bookId, apply);

        Console.WriteLine($"[renumber-chapters] {report.BookTitle} — {report.Numbered.Count} numbered chapter(s), "
                          + $"{report.Unnumbered.Count} unnumbered unit(s), {report.Changes} change(s) "
                          + (apply ? "APPLIED" : "proposed (dry run)"));
        Console.WriteLine();

        foreach (var r in report.Numbered.Where(r => r.IsChange))
            Console.WriteLine($"  pos {r.Position,3}:  {r.OldTitle}");
        if (report.Changes > 0) Console.WriteLine();
        foreach (var r in report.Numbered.Where(r => r.IsChange))
            Console.WriteLine($"  pos {r.Position,3}:  {r.OldTitle}   →   {r.NewTitle}");

        if (report.Unnumbered.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("  Left alone (unnumbered units keep their titles and their places):");
            foreach (var u in report.Unnumbered) Console.WriteLine($"      {u}");
        }

        if (!apply)
        {
            Console.WriteLine();
            Console.WriteLine("  Dry run — nothing written. Re-run with --apply.");
        }
        return 0;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
