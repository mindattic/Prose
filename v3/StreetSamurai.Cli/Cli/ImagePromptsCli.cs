using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// CLI surface for <see cref="ImagePromptRegenService"/>.
///
///   ss --image-prompts regen --id &lt;id|slug&gt; [--force]
///       Regenerate one character's prompts. --force bypasses the hash check.
///
///   ss --image-prompts regen --all-changed
///       Sweep every active character; skip those whose stored hash already
///       matches their current genetic_ancestry.
/// </summary>
public static class ImagePromptsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var idx = Array.IndexOf(args, "--image-prompts");
        var sub = idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
        if (sub != "regen" && sub != "backfill-hashes")
        {
            Console.Error.WriteLine("Usage: ss --image-prompts <regen|backfill-hashes> [...]");
            return 2;
        }

        var svc    = sp.GetRequiredService<ImagePromptRegenService>();
        var export = sp.GetRequiredService<CanonExportService>();

        if (sub == "backfill-hashes")
        {
            Console.WriteLine("[image-prompts] backfilling ancestry hashes (no LLM calls)...");
            int last = -1;
            var prog = new Progress<(int done, int total)>(p =>
            {
                var pct = p.total > 0 ? (int)(100.0 * p.done / p.total) : 0;
                if (pct == last) return;
                last = pct;
                Console.Write($"\r[image-prompts] [{p.done,5}/{p.total,5}] {pct,3}%");
            });
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var report = await svc.BackfillHashesAsync(prog);
            sw.Stop();
            Console.WriteLine();
            Console.WriteLine($"=== Backfill done in {sw.Elapsed:mm\\:ss} ===");
            Console.WriteLine($"  scanned:        {report.Scanned}");
            Console.WriteLine($"  stamped:        {report.Stamped}");
            Console.WriteLine($"  already hashed: {report.AlreadyHashed}");
            Console.WriteLine($"  no ancestry:    {report.NoAncestry}");
            return 0;
        }

        var force = args.Contains("--force");

        if (args.Contains("--all-changed"))
        {
            Console.WriteLine("[image-prompts] sweeping all characters...");
            int last = -1;
            var prog = new Progress<(int done, int total)>(p =>
            {
                var pct = p.total > 0 ? (int)(100.0 * p.done / p.total) : 0;
                if (pct == last) return;
                last = pct;
                Console.Write($"\r[image-prompts] [{p.done,5}/{p.total,5}] {pct,3}%");
            });
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var report = await svc.RegenAllChangedAsync(prog);
            sw.Stop();
            Console.WriteLine();
            Console.WriteLine($"=== Regen done in {sw.Elapsed:mm\\:ss} ===");
            Console.WriteLine($"  scanned:      {report.Scanned}");
            Console.WriteLine($"  skipped:      {report.Skipped}  (hash already current)");
            Console.WriteLine($"  regenerated:  {report.Regenerated}");
            Console.WriteLine($"  failed:       {report.Failed}");
            return 0;
        }

        var idArg = GetArg(args, "--id");
        if (idArg == null) { Console.Error.WriteLine("--id <id|slug> required (or --all-changed)"); return 2; }
        var id = await export.ResolveEntityIdAsync(idArg);
        if (id == null) { Console.Error.WriteLine($"could not resolve '{idArg}'"); return 1; }

        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        var result = await svc.RegenForCharacterAsync(id.Value, force);
        sw1.Stop();
        if (result.Updated)
            Console.WriteLine($"regenerated in {sw1.Elapsed:mm\\:ss} — image_prompt + dalle3_prompt updated.");
        else
            Console.WriteLine($"no update: {result.Reason}");
        return result.Updated ? 0 : 1;
    }

    private static string? GetArg(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
