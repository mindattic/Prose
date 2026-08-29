using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// RFC 0007 "Universe Interchange" — import/export between an app's
/// <c>&lt;app&gt;/universe/&lt;slug&gt;.universe.json</c> contract file
/// (<c>docs/schemas/universe.schema.json</c>) and Prose's Entity spine.
///
///   prose --universe-import &lt;path&gt; [--universe &lt;slug&gt;]
///       Import an interchange file. Universe slug defaults to the file's own universe.id.
///   prose --universe-export &lt;slug&gt; &lt;path&gt;
///       Export a universe to interchange-file JSON at &lt;path&gt;.
///   prose --universe-sync &lt;path&gt;
///       Import &lt;path&gt;, then export back to the same path (normalizes the file — the
///       consumer app commits the normalized copy).
///
/// Each subcommand resolves its own explicit universe (from the file itself, or a required
/// positional slug) — none depend on the ambient --universe scoping flag, so all three are
/// listed in Program.cs's UniverseAgnosticCommands allowlist.
/// </summary>
public static class UniverseInterchangeCli
{
    public static async Task<int> RunImportAsync(string[] args, IServiceProvider services)
    {
        if (args.Length < 1 || args[0].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Usage: prose --universe-import <path> [--universe <slug>]");
            return 1;
        }
        var path = args[0];
        var slug = Flag(args, "--universe");

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"[universe-import] File not found: {path}");
            return 1;
        }

        var json = await File.ReadAllTextAsync(path);
        var svc = services.GetRequiredService<UniverseInterchangeService>();
        var result = await svc.ImportAsync(json, slug);
        PrintImportResult(result);
        return result.Success ? 0 : 1;
    }

    public static async Task<int> RunExportAsync(string[] args, IServiceProvider services)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: prose --universe-export <slug> <path>");
            return 1;
        }
        var slug = args[0];
        var path = args[1];

        var svc = services.GetRequiredService<UniverseInterchangeService>();
        try
        {
            await svc.ExportToFileAsync(slug, path);
            Console.WriteLine($"[universe-export] Wrote {path}");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"[universe-export] {ex.Message}");
            return 1;
        }
    }

    public static async Task<int> RunSyncAsync(string[] args, IServiceProvider services)
    {
        if (args.Length < 1 || args[0].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Usage: prose --universe-sync <path>");
            return 1;
        }
        var path = args[0];
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"[universe-sync] File not found: {path}");
            return 1;
        }

        var svc = services.GetRequiredService<UniverseInterchangeService>();
        var json = await File.ReadAllTextAsync(path);
        var importResult = await svc.ImportAsync(json);
        PrintImportResult(importResult);
        if (!importResult.Success) return 1;

        await svc.ExportToFileAsync(importResult.UniverseSlug, path);
        Console.WriteLine($"[universe-sync] Normalized {path}");
        return 0;
    }

    private static void PrintImportResult(UniverseInterchangeService.ImportResult r)
    {
        if (r.Errors.Count > 0)
        {
            foreach (var e in r.Errors) Console.Error.WriteLine($"[universe-import] ✘ {e}");
            return;
        }
        Console.WriteLine($"[universe-import] universe: {r.UniverseSlug}{(r.UniverseCreated ? " (created)" : "")}");
        Console.WriteLine($"  entities created : {r.EntitiesCreated}");
        Console.WriteLine($"  entities updated : {r.EntitiesUpdated}");
        Console.WriteLine($"  stubs created    : {r.StubsCreated}");
        Console.WriteLine($"  stubs promoted   : {r.StubsPromoted}");
        Console.WriteLine($"  edges created    : {r.EdgesCreated}");
    }

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
