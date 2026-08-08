using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --repair-slugs [--apply] [--family entities|nodes|books|series|episodes] [--json]</c>
///
/// Regenerates every slug from its owning row's metadata (Name/Title) and
/// updates the slug-carrying references: beat audio paths, combined-audio and
/// publication paths, on-disk audio directories, and entity alt_slug
/// preservation. Slugs are loose keys — the UUIDv7 id is the real key, and
/// consumers fall back to it when a slug goes stale.
///
/// DRY-RUN BY DEFAULT: without <c>--apply</c> it only reports what would
/// change. Exit 0 = clean or repaired; 1 = warnings emitted.
/// </summary>
public static class SlugRepairCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var svc    = services.GetRequiredService<SlugRepairService>();
        var apply  = args.Contains("--apply");
        var json   = args.Contains("--json");
        var family = Flag(args, "--family") ?? "all";

        if (family != "all" && !SlugRepairService.Families.Contains(family))
        {
            Console.Error.WriteLine($"[repair-slugs] Unknown family '{family}'. Valid: {string.Join(", ", SlugRepairService.Families)} (or omit for all).");
            return 1;
        }

        if (!json)
            Console.WriteLine($"[repair-slugs] {(apply ? "APPLYING" : "Dry run")} — family: {family}…");

        var report = await svc.RepairAsync(apply, family);

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            return report.Warnings.Count > 0 ? 1 : 0;
        }

        if (report.Count == 0)
        {
            Console.WriteLine("[repair-slugs] ✓ All slugs already match their metadata.");
        }
        else
        {
            foreach (var group in report.Changes.GroupBy(c => c.Family))
            {
                Console.WriteLine($"\n── {group.Key} ({group.Count()}) ─────────────────────────");
                foreach (var c in group)
                {
                    Console.WriteLine($"  {c.Label}");
                    Console.WriteLine($"    '{c.OldSlug}' → '{c.NewSlug}'");
                    foreach (var fx in c.SideEffects)
                        Console.WriteLine($"      + {fx}");
                }
            }
            Console.WriteLine($"\n[repair-slugs] {report.Count} slug(s) {(apply ? "repaired" : "would change")}.");
            if (!apply)
                Console.WriteLine("[repair-slugs] Re-run with --apply to write.");
        }

        foreach (var w in report.Warnings)
            Console.Error.WriteLine($"[repair-slugs] WARN: {w}");

        return report.Warnings.Count > 0 ? 1 : 0;
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
