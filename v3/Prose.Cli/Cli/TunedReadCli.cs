using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.Audit;

namespace Prose.Cli;

/// <summary>
/// prose --tuned-read --slug &lt;slug-or-code-or-id&gt; [--dry] [--no-extract] [--max-candidates N] [--json]
///
/// The Story Ledger's reading instrument (Phase 2). Walks a book in true reading order, keeps its
/// fact ledger fresh, pairs claims that an exclusion axiom says cannot both be true, adjudicates
/// only those pairs, and files a finding for each contradiction whose evidence survives the
/// mechanical quote gate.
///
/// <para><b><c>--dry</c> is the one to reach for first.</b> It runs the entire deterministic half
/// — extraction refresh is skipped, candidates are generated and counted — and spends nothing.
/// A candidate count in the hundreds means an axiom is too broad, and finding that out for free
/// is the difference between a useful instrument and a bill.</para>
///
/// <para>Routed through the cost gate (<c>ForwardWithCostGateAsync</c>) because a real run spends
/// LLM money — one Sonnet adjudication per uncached candidate. Deliberately NOT in Program.cs's
/// <c>UniverseAgnosticCommands</c> list: it resolves one book and reads that universe's axioms,
/// so it needs a real scope.</para>
/// </summary>
public static class TunedReadCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var tuned     = services.GetRequiredService<TunedReadService>();

        var slug = Flag(args, "--slug") ?? Flag(args, "--code") ?? Flag(args, "--id");
        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine(
                "Usage: prose --tuned-read --slug <slug-or-code-or-id> [--dry] [--no-extract] [--max-candidates N] [--json]\n" +
                "  --dry             deterministic half only — candidate counts, ZERO LLM spend. Start here.\n" +
                "  --no-extract      skip the hash-gated ledger refresh (use the ledger exactly as it stands)\n" +
                "  --max-candidates  cap adjudications this run (default 60)");
            return 2;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = await NodeRefResolver.ResolveAsync(db, slug);
        if (nodeId == null)
        {
            Console.Error.WriteLine($"[tuned-read] No node matched '{slug}'.");
            return 2;
        }

        var dry = args.Contains("--dry") || args.Contains("--no-adjudicate");
        var max = int.TryParse(Flag(args, "--max-candidates"), out var m) && m > 0 ? m : 60;

        var opts = new TunedReadService.TunedReadOptions(
            ReExtract: !args.Contains("--no-extract") && !dry,
            Adjudicate: !dry,
            MaxCandidates: max);

        var report = await tuned.RunAsync(nodeId.Value, opts);

        if (args.Contains("--json"))
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine($"{report.Title}  [{report.Slug}]");
        Console.WriteLine($"  {report.Chapters} chapter(s), {report.Beats} beat(s), {report.LiveClaims} live ledger claim(s)");
        Console.WriteLine($"  candidates: {report.CandidatesFromOntology} from the exclusion ontology, " +
                          $"{report.CandidatesFromSamePredicate} same-predicate group(s) already flagged by the ledger");
        if (!dry)
            Console.WriteLine($"  adjudicated {report.Adjudicated} (cache hits {report.CacheHits}) → " +
                              $"{report.Confirmed} confirmed, {report.Cleared} cleared, " +
                              $"{report.GroundingRejected} discarded for ungrounded evidence");

        foreach (var n in report.Notes) Console.WriteLine($"  note: {n}");

        if (report.Findings.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("CONTRADICTIONS:");
            foreach (var f in report.Findings)
            {
                Console.WriteLine($"  [{f.Severity}] {f.EntityName}: {f.PredicateA}=\"{Clip(f.ObjectA, 60)}\" " +
                                  $"vs {f.PredicateB}=\"{Clip(f.ObjectB, 60)}\"" +
                                  (f.BeatNumberA.HasValue || f.BeatNumberB.HasValue
                                      ? $"  (beats #{f.BeatNumberA?.ToString() ?? "?"} / #{f.BeatNumberB?.ToString() ?? "?"})"
                                      : ""));
                Console.WriteLine($"        {f.Note}");
                Console.WriteLine($"        evidence: \"{Clip(f.EvidenceQuote, 140)}\"");
            }
        }

        Console.WriteLine();
        Console.WriteLine(dry
            ? "[tuned-read] Dry run — nothing adjudicated, nothing filed, nothing spent."
            : $"[tuned-read] {report.Confirmed} finding(s) filed under \"{TunedReadService.SummaryPrefix.Trim()}\".");
        return 0;
    }

    private static string Clip(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..(max - 1)] + "…";

    private static string? Flag(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
