using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI surface for <see cref="CanonExportService"/>. Writes canon JSON to the
/// configured publish directory (Desktop fallback). All filenames are timestamped (yyyyMMdd-HHmmss).
///
///   ss --export global                       every repo, zipped
///   ss --export &lt;repoName&gt;                one repo, zipped (e.g. "people", "weaponry")
///   ss --export &lt;entityId&gt;                one entity, single .json
///   ss --export &lt;slug&gt;                    one entity, looked up by Entity.Slug
///   ss --export &lt;entityId|slug&gt; --deep    entity + every cross-repo record it names, zipped
///
/// Resolution order when the target isn't "global": try Guid → repo name →
/// Entity.Slug. First match wins.
/// </summary>
public static class ExportCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var idx = Array.IndexOf(args, "--export");
        var target = idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
        if (string.IsNullOrWhiteSpace(target))
        {
            Console.Error.WriteLine("Usage: ss --export <global|repoName|entityId|slug> [--deep]");
            return 2;
        }

        var deep = args.Contains("--deep");
        var svc = sp.GetRequiredService<CanonExportService>();
        var discovery = sp.GetRequiredService<Prose.Core.Services.ExportDiscoveryService>();

        try
        {
            CanonExportService.ExportResult result;
            if (string.Equals(target, "global", StringComparison.OrdinalIgnoreCase)
             || string.Equals(target, "all",    StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[export] global pass — packing every repo…");
                result = await svc.ExportAllAsync();
            }
            else if (Guid.TryParse(target, out var id) ||
                     (target.Length == 32 && Guid.TryParseExact(target, "N", out id)))
            {
                Console.WriteLine(deep
                    ? $"[export] entity {id} (deep — bundling cross-repo references)"
                    : $"[export] entity {id}");
                result = deep
                    ? await svc.ExportEntityDeepAsync(id)
                    : await svc.ExportEntityAsync(id);
            }
            else if (discovery.GetAllRepos().Keys
                     .Any(k => string.Equals(k, target, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"[export] repo \"{target}\"");
                result = await svc.ExportRepoAsync(target);
            }
            else
            {
                // Fall back to slug lookup. Lets users type "ss --export kyle_cassidy"
                // when they don't have the Guid handy.
                var resolved = await svc.ResolveEntityIdAsync(target);
                if (resolved == null)
                {
                    Console.Error.WriteLine($"[export] no Guid, repo, or Entity.Slug matched \"{target}\"");
                    return 1;
                }
                Console.WriteLine(deep
                    ? $"[export] entity {resolved} (slug \"{target}\", deep)"
                    : $"[export] entity {resolved} (slug \"{target}\")");
                result = deep
                    ? await svc.ExportEntityDeepAsync(resolved.Value)
                    : await svc.ExportEntityAsync(resolved.Value);
            }

            Console.WriteLine($"  wrote: {result.Path}");
            Console.WriteLine($"  entries: {result.EntryCount}");
            Console.WriteLine($"  size: {FormatBytes(result.Bytes)}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[export] failed: {ex.Message}");
            return 1;
        }
    }

    private static string FormatBytes(long b)
    {
        if (b < 1024) return $"{b} B";
        if (b < 1024 * 1024) return $"{b / 1024.0:F1} KB";
        return $"{b / 1024.0 / 1024.0:F2} MB";
    }
}
