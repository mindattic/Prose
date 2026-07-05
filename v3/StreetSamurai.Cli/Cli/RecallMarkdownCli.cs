using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// Keyword recall for tracked markdown. "Call up" the select few .md files
/// relevant to a topic straight from the DB — print them, and optionally
/// materialize (create) them on disk — instead of keeping hundreds of tiny
/// orphaned .md files on disk all the time.
///
///   ss --recall &lt;keyword&gt; [--content] [--to-disk] [--as-of &lt;datetime-utc&gt;]
///
///   keyword     Substring matched (case-insensitive) against path / file name / category.
///   --content   Also search inside file bodies, not just names.
///   --to-disk   Write each match back to its on-disk location (create on demand).
///               Without this flag the content is printed to stdout only.
///   --as-of     UTC ISO-8601 instant; recall the version current at that time
///               (from the MarkdownFiles_History temporal table).
/// </summary>
public static class RecallMarkdownCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var keyword     = FirstPositional(args);
        var includeBody = args.Contains("--content");
        var toDisk      = args.Contains("--to-disk");
        var asOfStr     = ArgValue(args, "--as-of");

        if (string.IsNullOrWhiteSpace(keyword))
        {
            Console.Error.WriteLine("Usage: ss --recall <keyword> [--content] [--to-disk] [--as-of <datetime-utc>]");
            return 1;
        }

        DateTime? asOf = null;
        if (asOfStr != null)
        {
            if (!DateTime.TryParse(asOfStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            {
                Console.Error.WriteLine($"[recall] cannot parse --as-of '{asOfStr}' (use ISO 8601, e.g. 2026-06-01T00:00:00Z)");
                return 1;
            }
            asOf = parsed.ToUniversalTime();
        }

        var svc = sp.GetRequiredService<MarkdownFileService>();
        var matches = await svc.SearchAsync(keyword!, includeBody);

        if (matches.Count == 0)
        {
            Console.WriteLine($"[recall] no tracked .md matches '{keyword}'"
                + (includeBody ? "" : " (try --content to search inside file bodies)"));
            return 2;
        }

        Console.WriteLine($"[recall] {matches.Count} match(es) for '{keyword}':");
        foreach (var m in matches)
            Console.WriteLine($"  {m.Category,-24} {m.FileRoot}/{m.RelativePath}");
        Console.WriteLine();

        if (toDisk)
        {
            int written = 0;
            foreach (var m in matches)
            {
                var result = await svc.RestoreAsync(m.RelativePath, asOf, dryRun: false);
                written += result.Written;
                foreach (var err in result.Errors) Console.Error.WriteLine($"  ✘ {err}");
            }
            Console.WriteLine($"[recall] materialized {written} file(s) to disk.");
            return 0;
        }

        // Print mode — dump each match's body so it can be read in-place.
        foreach (var m in matches)
        {
            var row = asOf.HasValue ? await svc.GetAsync(m.RelativePath, asOf) : m;
            Console.WriteLine($"===== {m.FileRoot}/{m.RelativePath} =====");
            Console.WriteLine(row?.Content?.TrimEnd() ?? "");
            Console.WriteLine();
        }
        return 0;
    }

    private static string? FirstPositional(string[] args)
    {
        // The first token that is not a flag and not a flag's value.
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--recall") continue;
            if (a == "--as-of") { i++; continue; }      // skip flag value
            if (a.StartsWith("--")) continue;           // boolean flag
            return a;
        }
        return null;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
