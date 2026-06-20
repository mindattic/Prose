using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// Restore markdown files from database to disk. Supports point-in-time
/// recovery via --as-of so any historical version can be resurfaced from the
/// MarkdownFiles_History temporal table.
///
///   ss --restore-markdown [--file &lt;relativePath&gt;] [--as-of &lt;datetime-utc&gt;] [--dry-run] [--list]
///
///   --file   Restore only this one file (e.g. "CLAUDE.md" or "docs/BIBLE.md").
///            Omit to restore all tracked files.
///   --as-of  UTC datetime string (ISO 8601). Restores the version that was
///            current at that moment. E.g. "2026-06-01T00:00:00Z".
///            Omit to restore the latest version.
///   --dry-run  Print what would be written without touching the filesystem.
///   --list     Print all tracked files and exit (no restore performed).
/// </summary>
public static class RestoreMarkdownCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var list    = args.Contains("--list");
        var dryRun  = args.Contains("--dry-run");
        var file    = ArgValue(args, "--file");
        var asOfStr = ArgValue(args, "--as-of");

        DateTime? asOf = null;
        if (asOfStr != null)
        {
            if (!DateTime.TryParse(asOfStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            {
                Console.Error.WriteLine($"[restore-markdown] cannot parse --as-of value '{asOfStr}' (use ISO 8601, e.g. 2026-06-01T00:00:00Z)");
                return 1;
            }
            asOf = parsed.ToUniversalTime();
        }

        var svc = sp.GetRequiredService<MarkdownFileService>();

        if (list)
        {
            var all = await svc.ListAsync();
            Console.WriteLine($"[restore-markdown] {all.Count} tracked file(s):");
            foreach (var f in all)
            {
                var dest = svc.ResolveAbsolutePath(f.FileRoot, f.RelativePath) ?? "?";
                Console.WriteLine($"  {f.Category,-24} {f.RelativePath}");
                Console.WriteLine($"    hash={f.ContentHash[..12]}… synced={f.LastSyncedAt:u} dest={dest}");
            }
            return 0;
        }

        if (dryRun)
            Console.WriteLine("[restore-markdown] dry-run mode — no files written");

        if (asOf.HasValue)
            Console.WriteLine($"[restore-markdown] restoring as-of {asOf:u}");

        var result = await svc.RestoreAsync(file, asOf, dryRun);

        Console.WriteLine($"[restore-markdown] written={result.Written} skipped={result.Skipped} errors={result.Errors.Count}");
        foreach (var err in result.Errors)
            Console.Error.WriteLine($"  ✘ {err}");

        return result.Errors.Count > 0 ? 1 : 0;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
