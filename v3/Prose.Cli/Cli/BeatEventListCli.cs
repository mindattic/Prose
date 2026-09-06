using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --generate-event-list --slug &lt;slug&gt; [--force] [--limit N] [--dry-run] [--model &lt;id&gt;]
///
/// Fills the per-beat plot-EVENT one-liner (Beat.EventSummary) — "what happened" — hash-gated
/// on TextHash so unchanged beats cost nothing on re-run. Haiku by default.
/// </summary>
public static class GenerateEventListCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null; int? limit = null; string? model = null;
        bool dryRun = args.Contains("--dry-run");
        bool force = args.Contains("--force");
        HashSet<int>? beats = null;
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
            if (args[i] == "--limit" && int.TryParse(args[i + 1], out var l)) { limit = l; i++; }
            if (args[i] == "--model") { model = args[i + 1]; i++; }
            if (args[i] == "--beats")
            {
                beats = args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => int.TryParse(s, out var n) ? n : -1).Where(n => n >= 0).ToHashSet();
                i++;
            }
        }
        if (slug == null)
        {
            Console.Error.WriteLine("Usage: prose --generate-event-list --slug <slug> [--force] [--limit N] [--dry-run] [--model <id>] [--beats 30400,30450,...]");
            return 2;
        }

        var svc = services.GetRequiredService<BeatEventSummaryService>();
        var mode = beats != null ? $"targeted retry ({beats.Count} beat(s), one-at-a-time)" : force ? "regenerate ALL" : "fill changed/missing";
        Console.WriteLine($"Event list {mode} for {slug}{(dryRun ? " (dry run)" : "")}...");
        var r = await svc.GenerateAsync(slug, limit, dryRun, force, model, beats, Console.WriteLine);

        Console.WriteLine();
        Console.WriteLine($"Node             : {r.NodeCode}");
        Console.WriteLine($"Candidates       : {r.Candidates}");
        Console.WriteLine($"Generated        : {r.Generated}{(dryRun ? " (dry run — not saved)" : "")}");
        Console.WriteLine($"Failed           : {r.Failed}");
        Console.WriteLine($"Skipped (cached) : {r.SkippedFromCache}");
        return 0;
    }
}

/// <summary>
/// prose --audit-event-summaries --slug &lt;slug&gt;
///
/// Read-only, deterministic, zero LLM cost. Finds stored EventSummary lines that describe a
/// DIFFERENT beat than the one they are attached to — the batch ref-shift that
/// BeatEventSummaryService now guards at write time. The guard only protects NEW writes; anything
/// written before it carries EventSummaryHash == TextHash, so the hash gate will never re-derive
/// it and a wrong synopsis stays wrong forever. Every synopsis-altitude instrument reads these
/// lines, so a shift here silently corrupts the 100 ft view of the book.
///
/// Found on BCODA ch.31 by hand-reading: beat #3056 (Kyle vs KT's Kinetix arm) carried "Kyle
/// strikes Rook's Meridian Conduct port housing" and #3057 (Rook) carried Nines's event.
/// Fix a reported beat with: prose --generate-event-list --slug &lt;slug&gt; --force --beats N,N,N
/// </summary>
public static class AuditEventSummariesCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--slug") { slug = args[i + 1]; break; }

        if (slug == null)
        {
            Console.Error.WriteLine("Usage: prose --audit-event-summaries --slug <slug>");
            return 2;
        }

        var svc = services.GetRequiredService<BeatEventSummaryService>();
        var mismatches = await svc.AuditAttributionAsync(slug);

        if (mismatches.Count == 0)
        {
            Console.WriteLine($"[audit-event-summaries] {slug}: no misattributed summaries found.");
            return 0;
        }

        Console.WriteLine($"[audit-event-summaries] {slug}: {mismatches.Count} summary/beat mismatch(es)\n");
        foreach (var m in mismatches.OrderBy(m => m.Number))
        {
            Console.WriteLine($"  beat #{m.Number}  ({m.Chapter})");
            Console.WriteLine($"    summary : {m.Summary}");
            Console.WriteLine($"    names \"{m.Name}\", who is absent here but present in beat #{m.MatchesBeatNumber}");
            Console.WriteLine();
        }
        Console.WriteLine("Fix with: prose --generate-event-list --slug <slug> --force --beats "
                          + string.Join(",", mismatches.Select(m => m.Number).Distinct().OrderBy(n => n)));
        return 1;
    }
}

/// <summary>
/// prose --export-event-list --slug &lt;slug&gt;
///
/// Writes the current DB state (no LLM call) to {CODE}-Events.txt in the node's
/// publish-export folder (same layout as description.txt / {CODE}-dcm-viz.htm — not
/// docs/nodes) — flat, SK-ordered, one line per enabled beat — and prints it to console.
/// </summary>
public static class ExportEventListCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
        }
        if (slug == null)
        {
            Console.Error.WriteLine("Usage: prose --export-event-list --slug <slug>");
            return 2;
        }

        var svc = services.GetRequiredService<BeatEventSummaryService>();
        var path = await svc.ExportTxtAsync(slug);
        Console.WriteLine($"Wrote {path}");
        Console.WriteLine();
        Console.WriteLine(await File.ReadAllTextAsync(path));
        return 0;
    }
}
