using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.Audit;

namespace Prose.Cli;

/// <summary>
/// prose --reconcile-obligations (--slug &lt;slug&gt; | --all [--status draft|complete|published]) [--deep] [--json] [--no-findings]
///
/// The obligation trial balance as an instrument (RFC 0013 D6a/b). Free: six deterministic rules
/// over the ledger, filed as FindingCategory.NarrativeObligation under node:{slug}#obligations.
/// --deep adds the paid resurfacing judge (cost-gated under its own command name,
/// "--reconcile-obligations-deep"): for every open obligation it asks one Haiku call which later
/// beats close or advance it, verbatim quote required, verdicts cached by candidate text hash.
/// Prints "examined N obligations over M beats" and COULD NOT LOOK when the ledger is empty.
/// --all walks every non-archived book in every universe (a Nodes query — no universe list).
/// </summary>
public static class ReconcileObligationsCli
{
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1]; return null; }
        var slug = Flag("--slug");
        var all = args.Contains("--all");
        var deep = args.Contains("--deep");
        var json = args.Contains("--json");
        var noFindings = args.Contains("--no-findings");
        var statusFilter = Flag("--status");

        if (string.IsNullOrWhiteSpace(slug) && !all)
        {
            Console.Error.WriteLine("Usage: prose --reconcile-obligations (--slug <slug> | --all [--status draft|complete|published]) [--deep] [--json]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var reconciler = services.GetRequiredService<ObligationReconciliationService>();

        var targets = new List<(Guid Id, string Slug, string Title)>();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            if (all)
            {
                var books = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                    .Where(n => n.ParentNodeId == null && n.Kind == "book" && n.Status != "archived")
                    .Select(n => new { n.Id, n.Slug, n.Title, n.Status, n.KdpPublishedAt })
                    .ToListAsync();
                foreach (var b in books)
                {
                    var atEnd = ObligationReconciliationService.IsBookAtEnd(b.Status, null, b.KdpPublishedAt);
                    var ok = statusFilter switch
                    {
                        "published" => b.KdpPublishedAt != null,
                        "complete"  => atEnd,
                        "draft"     => !atEnd,
                        _ => true,
                    };
                    if (ok) targets.Add((b.Id, b.Slug ?? b.Id.ToString("N"), b.Title ?? ""));
                }
                // KDP-live first, then complete, then drafts — a reader can find the defect today.
                targets = targets.OrderByDescending(t => books.First(b => b.Id == t.Id).KdpPublishedAt != null)
                                 .ThenByDescending(t => ObligationReconciliationService.IsBookAtEnd(books.First(b => b.Id == t.Id).Status, null, null))
                                 .ToList();
            }
            else
            {
                var resolved = await NodeRefResolver.ResolveAsync(db, slug!);
                if (resolved == null) { Console.Error.WriteLine($"[reconcile-obligations] {NodeRefResolver.NotFoundMessage(slug!)}"); return 1; }
                var root = await BookRootAsync(db, resolved.Value);
                var n = await db.Nodes.AsNoTracking().IgnoreQueryFilters().Where(x => x.Id == root).Select(x => new { x.Slug, x.Title }).FirstAsync();
                targets.Add((root, n.Slug ?? root.ToString("N"), n.Title ?? ""));
            }
        }

        var reports = new List<ObligationReconciliationService.Report>();
        var exit = 0;
        foreach (var t in targets)
        {
            var report = await reconciler.RunAsync(t.Id, deep, writeFindings: !noFindings);
            reports.Add(report);
            if (!json) Print(report);
            if (report.CouldNotLook || report.Balance.OverdueWithoutDecision.Count > 0) exit = 1;
        }

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(reports.Select(r => new
            {
                r.NodeId, r.Slug, r.Title, r.Examined, r.TotalBeats, r.ScannedBeats, r.CouldNotLook, r.BookAtEnd,
                balanced = r.Balance.Balanced, overdue_without_decision = r.Balance.OverdueWithoutDecision,
                rule_counts = r.RuleCounts,
                findings = r.Verdicts.Where(v => v.Severity != "PASS").Select(v => new { v.RuleKey, v.Severity, v.Evidence, beat_id = v.Location }),
                deep = r.Deep, snapshot = r.Snapshot,
            }), Json));
        }
        return exit;
    }

    static void Print(ObligationReconciliationService.Report r)
    {
        Console.WriteLine($"OBLIGATION RECONCILIATION {r.Title} ({r.Slug}) — examined {r.Examined} obligation(s) over {r.TotalBeats} beat(s) (scan coverage {r.ScannedBeats}/{r.TotalBeats}){(r.BookAtEnd ? " · book at end" : "")}");
        if (r.CouldNotLook)
        {
            Console.WriteLine("  COULD NOT LOOK — the ledger is empty for this book; run `prose --obligations rescan --slug …` first. An empty ledger fails, it does not pass.");
            return;
        }
        var b = r.Balance;
        Console.WriteLine($"  Introduced {b.Opened} | Closed {b.Closed} | Advanced-only {b.Advanced} | Dropped {b.Dropped} | Deferred {b.Deferred} | Withdrawn {b.Withdrawn} | Open {b.CarriedForward} | Overdue {b.OverdueWithoutDecision.Count}");
        Console.WriteLine("  " + string.Join(" · ", r.RuleCounts.Select(kv => $"{kv.Key} {kv.Value}")));
        if (r.Deep != null)
            Console.WriteLine($"  deep: examined {r.Deep.Examined}, closed {r.Deep.Closed}, advanced {r.Deep.Advanced}, not addressed {r.Deep.NotAddressed}, ungrounded discarded {r.Deep.DiscardedUngrounded}, cache hits {r.Deep.CacheHits}, LLM calls {r.Deep.LlmCalls}{(r.Deep.Evaluated ? "" : " — SOME CALLS FAILED")}");
        foreach (var v in r.Verdicts.Where(v => v.Severity != "PASS").OrderBy(v => v.Severity == "BLOCKER" ? 0 : v.Severity == "MODERATE" ? 1 : 2))
            Console.WriteLine($"  {v.Severity,-8} {v.RuleKey,-16} {v.Evidence}");
        if (r.Snapshot is { } s)
            Console.WriteLine($"  health: words {s.WordCount:N0} · payoff coverage {s.PayoffCoveragePct:P0} · unnamed referents/10k {s.UnnamedReferentDensity10k:F2} · consistency errors/10k {s.ConsistencyErrorDensity10k:F3}");
        Console.WriteLine(b.Balanced ? "  RESULT: BALANCED" : "  RESULT: NOT BALANCED");
    }

    static async Task<Guid> BookRootAsync(ProseDbContext db, Guid nodeId)
    {
        var walk = nodeId;
        for (var depth = 0; depth < 10; depth++)
        {
            var parent = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.Id == walk).Select(n => n.ParentNodeId).FirstOrDefaultAsync();
            if (parent == null) return walk;
            walk = parent.Value;
        }
        return walk;
    }
}
