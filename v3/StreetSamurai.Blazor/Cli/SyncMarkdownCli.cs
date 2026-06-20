using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// Scan all known markdown file locations and upsert changed files into the
/// database. Only rows whose content hash changed generate a new history row,
/// so unchanged files cost nothing.
///
///   ss --sync-markdown [--dry-run]
///
/// Locations synced:
///   project CLAUDE.md, docs/*.md, docs/registers/*.md, docs/rfc/*.md
///   ~/.claude/CLAUDE.md
///   ~/.claude/projects/{slug}/memory/*.md
/// </summary>
public static class SyncMarkdownCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var dryRun = args.Contains("--dry-run");

        var svc = sp.GetRequiredService<MarkdownFileService>();

        if (dryRun)
            Console.WriteLine("[sync-markdown] dry-run mode — no writes to DB");

        Console.WriteLine("[sync-markdown] discovering files…");
        var discovered = svc.DiscoverFiles().ToList();
        Console.WriteLine($"[sync-markdown] found {discovered.Count} file(s)");
        foreach (var f in discovered)
            Console.WriteLine($"  {f.Category,-24} {f.RelativePath}");

        Console.WriteLine("[sync-markdown] syncing…");
        var result = await svc.SyncAllAsync(dryRun);

        Console.WriteLine($"[sync-markdown] inserted={result.Inserted} updated={result.Updated} unchanged={result.Unchanged} errors={result.Errors.Count}");

        foreach (var err in result.Errors)
            Console.Error.WriteLine($"  ✘ {err}");

        return result.Errors.Count > 0 ? 1 : 0;
    }
}
