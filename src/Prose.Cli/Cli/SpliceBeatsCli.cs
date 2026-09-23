using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --splice-beats</c> — apply a hand-written docket of exact-text replacements across a book.
///
///   --node &lt;slug|code|guid&gt;   The book (or any node) whose beats the docket targets.
///   --file &lt;docket.json&gt;     <c>[{"beat": 20420, "old": "…", "new": "…", "count": 1}]</c>. <c>beat</c> is
///                            the global Beat.Number; <c>old</c> is matched against the beat text with
///                            entity tags stripped (inline <c>*italic*</c> markers kept); <c>count</c>
///                            defaults to 1; empty <c>new</c> deletes.
///   --apply                  Write. Without it the docket is only planned and guarded (dry run).
///   --analyze                Run the per-save analysis tails on every beat (LLM calls). Deferred by
///                            default — a docket wants one analysis pass at the end, not one per beat.
///
/// Any count mismatch anywhere aborts the whole docket with nothing written. After writing, only the
/// written beats are re-read and checked against the promised text. See <see cref="BeatSpliceService"/>.
///
/// Exit codes: 0 = planned clean / applied and verified · 1 = bad args · 2 = guard aborted (nothing
/// written) · 3 = applied with conflicts, errors, or verify misses.
/// </summary>
public static class SpliceBeatsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? nodeRef = null, file = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--node": case "--slug": if (i + 1 < args.Length) nodeRef = args[++i]; break;
                case "--file": if (i + 1 < args.Length) file = args[++i]; break;
            }
        }
        var apply = args.Contains("--apply");
        var deferAnalysis = !args.Contains("--analyze");

        if (string.IsNullOrWhiteSpace(nodeRef) || string.IsNullOrWhiteSpace(file))
        {
            Console.Error.WriteLine("Usage: prose --splice-beats --node <slug|code|guid> --file <docket.json> [--apply] [--analyze]");
            return 1;
        }
        if (!File.Exists(file)) { Console.Error.WriteLine($"[splice-beats] File not found: {file}"); return 1; }

        List<SpliceEdit> docket;
        try { docket = BeatSpliceService.ParseDocket(await File.ReadAllTextAsync(file)); }
        catch (Exception ex) { Console.Error.WriteLine($"[splice-beats] Docket is not valid JSON [{{beat, old, new, count?}}]: {ex.Message}"); return 1; }
        if (docket.Count == 0) { Console.Error.WriteLine("[splice-beats] Docket has no edits."); return 1; }

        var nodeId = await NodeRefResolver.ResolveAsync(
            services.GetRequiredService<IDbContextFactory<ProseDbContext>>(), nodeRef);
        if (nodeId == null) { Console.Error.WriteLine($"[splice-beats] Node '{nodeRef}' not found."); return 1; }

        var report = await services.GetRequiredService<BeatSpliceService>()
            .RunAsync(nodeId.Value, docket, apply, deferAnalysis);

        foreach (var f in report.GuardFailures) Console.WriteLine($"ABORT-GUARD {f}");
        if (report.Aborted)
        {
            Console.WriteLine($"{report.GuardFailures.Count} guard failure(s) — nothing written.");
            return 2;
        }

        var unwrapped = report.Results.Sum(r => r.UnwrappedTags);
        if (!report.Applied)
        {
            Console.WriteLine($"DRY RUN: {report.Beats} beats, {report.Splices} splices planned, all counts match" +
                              (unwrapped > 0 ? $"; {unwrapped} entity tag(s) the edits touch will be re-derived" : "") +
                              ". Re-run with --apply to write.");
            return 0;
        }

        foreach (var r in report.Results.Where(r => r.Status != "verified"))
            Console.WriteLine($"{r.Status.ToUpperInvariant()} #{r.Beat} {r.Detail}");
        var verified = report.Results.Count(r => r.Status == "verified");
        Console.WriteLine($"Applied: {verified}/{report.Beats} beats verified, {report.Splices} splices, " +
                          $"verify misses: {report.VerifyMisses}{(deferAnalysis ? ", analysis deferred" : "")}.");
        return verified == report.Beats ? 0 : 3;
    }
}
