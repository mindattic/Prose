using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// Scan all known markdown file locations and upsert changed files into the
/// database. Only rows whose content hash changed generate a new history row,
/// so unchanged files cost nothing.
///
///   ss --sync-markdown [--dry-run]
///
/// Phase 1 — file sync:
///   project CLAUDE.md, docs/*.md, docs/registers/*.md, docs/rfc/*.md
///   ~/.claude/CLAUDE.md
///   ~/.claude/projects/{slug}/memory/*.md
///
/// Phase 2 — canon DB sync (Truth-First Architecture):
///   CanonDocumentSections → MarkdownFiles (world docs: BIBLE, WORLD, FRANCHISE, CAUL)
///   NodeBibleSections    → MarkdownFiles (node bibles: docs/nodes/{CODE}.md)
///   DB content always wins — overwrites any file-synced version. This is how
///   hand-edits to .md files are detected and silently corrected.
/// </summary>
public static class SyncMarkdownCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var dryRun = args.Contains("--dry-run");

        var svc = sp.GetRequiredService<MarkdownFileService>();

        if (dryRun)
            Console.WriteLine("[sync-markdown] dry-run mode — no writes to DB");

        // Phase 1: file scan
        Console.WriteLine("[sync-markdown] phase 1 — discovering files…");
        var discovered = svc.DiscoverFiles().ToList();
        Console.WriteLine($"[sync-markdown] found {discovered.Count} file(s)");
        foreach (var f in discovered)
            Console.WriteLine($"  {f.Category,-24} {f.RelativePath}");

        Console.WriteLine("[sync-markdown] syncing files…");
        var fileResult = await svc.SyncAllAsync(dryRun);
        Console.WriteLine($"[sync-markdown] file sync: inserted={fileResult.Inserted} updated={fileResult.Updated} unchanged={fileResult.Unchanged} errors={fileResult.Errors.Count}");
        foreach (var err in fileResult.Errors)
            Console.Error.WriteLine($"  ✘ {err}");

        // Phase 2: canon DB sync (DB source overwrites file-synced content)
        Console.WriteLine("[sync-markdown] phase 2 — syncing from canon DB…");
        var dbResult = await svc.SyncFromCanonDbAsync(dryRun);
        Console.WriteLine($"[sync-markdown] db sync:   inserted={dbResult.Inserted} updated={dbResult.Updated} unchanged={dbResult.Unchanged} errors={dbResult.Errors.Count}");
        foreach (var err in dbResult.Errors)
            Console.Error.WriteLine($"  ✘ {err}");

        var totalErrors = fileResult.Errors.Count + dbResult.Errors.Count;
        return totalErrors > 0 ? 1 : 0;
    }
}
