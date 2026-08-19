using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI surface for Trinity Reconciliation — autonomous-but-reversible resolution of
/// Bible/Book/Entity divergences for GLMZ/SCRY/FICTION books (never NONFICTION/GOSPEL).
///
///   prose --reconcile-trinity --extract --slug &lt;slug&gt;|--all
///       Phase 1: extract prose+bible claims for any in-scope book that has never had any
///       claims extracted. No voting gate — extraction is a single-LLM analyzer, not a ballot.
///
///   prose --reconcile-trinity --survey --slug &lt;slug&gt;|--all
///       Phase 2: read-only contradiction-group + applied-claim-drift survey, source-pair
///       breakdown. Zero DecideAsync calls.
///
///   prose --reconcile-trinity --slug &lt;slug&gt;|--all --allow-votes --confirm-auto-edit [--dry-run]
///       Phase 3: actually reconcile. TWO independent gates required — --allow-votes (SS-A44
///       VotingGate) AND --confirm-auto-edit (new; this is the first DecideAsync caller that
///       rewrites live prose/bible content, not just a ledger flip or JSON-field pick).
///       --dry-run still requires both flags but calls zero DecideAsync/edit methods — it prints
///       the plan (which group, which source would lose, which mechanism would fire) from data
///       already on hand.
///
///   prose --reconcile-trinity --undo --decision-id &lt;guid&gt;
///       Phase 4: revert one decision's edit(s) and flip the ledger side back.
/// </summary>
public static class ReconcileTrinityCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var svc        = services.GetRequiredService<TrinityReconciliationService>();
        var votingGate = services.GetRequiredService<VotingGate>();

        if (args.Contains("--undo"))
            return await CmdUndo(args, svc);

        if (args.Contains("--extract"))
            return await CmdExtract(args, svc);

        if (args.Contains("--survey"))
            return await CmdSurvey(args, svc);

        return await CmdReconcile(args, svc, votingGate);
    }

    static async Task<int> CmdExtract(string[] args, TrinityReconciliationService svc)
    {
        var slug = ArgValue(args, "--slug");
        var all  = args.Contains("--all");
        if (string.IsNullOrEmpty(slug) && !all) return Fail("--extract requires --slug <slug> or --all");

        var books = await svc.ResolveScopeAsync(slug, all);
        if (books.Count == 0) return Fail("no in-scope (GLMZ/SCRY/FICTION, NarrativeMode=original) book matched.");

        Console.WriteLine($"[trinity] Phase 1 — extraction sweep across {books.Count} book(s):");
        int extracted = 0, skipped = 0;
        foreach (var b in books)
        {
            try
            {
                var r = await svc.ExtractBookIfNeededAsync(b.NodeId);
                if (r.Skipped) { Console.WriteLine($"[trinity]   {b.Slug,-16} skipped — already has claims"); skipped++; }
                else
                {
                    Console.WriteLine($"[trinity]   {b.Slug,-16} {r.ChaptersProcessed} chapters — {r.NewClaims} new, {r.ConfirmedClaims} confirmed, {r.ContradictedClaims} contradicted");
                    extracted++;
                }
            }
            catch (Exception ex) { Console.WriteLine($"[trinity]   {b.Slug,-16} ! {ex.Message}"); }
        }
        Console.WriteLine($"[trinity] Done. {extracted} extracted, {skipped} already had claims.");
        return 0;
    }

    static async Task<int> CmdSurvey(string[] args, TrinityReconciliationService svc)
    {
        var slug = ArgValue(args, "--slug");
        var all  = args.Contains("--all");
        if (string.IsNullOrEmpty(slug) && !all) return Fail("--survey requires --slug <slug> or --all");

        var books = await svc.ResolveScopeAsync(slug, all);
        if (books.Count == 0) return Fail("no in-scope (GLMZ/SCRY/FICTION, NarrativeMode=original) book matched.");

        Console.WriteLine($"[trinity] Phase 2 — survey across {books.Count} book(s) (read-only, zero DecideAsync calls):");
        int totalGroups = 0, totalDrift = 0, totalBeatRepair = 0;
        int totalProseBible = 0, totalProseEntity = 0, totalBibleEntity = 0, totalOther = 0;
        foreach (var b in books)
        {
            var s = await svc.SurveyBookAsync(b.Slug);
            if (s.ContradictionGroups == 0 && s.AppliedDriftFindings == 0) continue;
            Console.WriteLine($"[trinity]   {b.Slug,-16} {s.ContradictionGroups,3} contradiction groups, {s.AppliedDriftFindings,3} applied-drift " +
                $"(prose-vs-bible {s.ProseVsBible}, prose-vs-entity {s.ProseVsEntity}, bible-vs-entity {s.BibleVsEntity}, other {s.OtherPairing}; " +
                $"{s.WouldHitBeatRepair} would hit beat-repair)");
            totalGroups += s.ContradictionGroups; totalDrift += s.AppliedDriftFindings; totalBeatRepair += s.WouldHitBeatRepair;
            totalProseBible += s.ProseVsBible; totalProseEntity += s.ProseVsEntity; totalBibleEntity += s.BibleVsEntity; totalOther += s.OtherPairing;
        }
        Console.WriteLine();
        Console.WriteLine($"[trinity] TOTAL: {totalGroups} contradiction groups, {totalDrift} applied-claim drift findings across {books.Count} books.");
        Console.WriteLine($"[trinity]   source pairs: prose-vs-bible={totalProseBible} prose-vs-entity={totalProseEntity} bible-vs-entity={totalBibleEntity} other={totalOther}");
        Console.WriteLine($"[trinity]   {totalBeatRepair} groups would hit the expensive prose-repair path (BeatRepairService → full 27-service ProseWriterRouter call) if reconciled.");
        return 0;
    }

    static async Task<int> CmdReconcile(string[] args, TrinityReconciliationService svc, VotingGate votingGate)
    {
        var slug          = ArgValue(args, "--slug");
        var all           = args.Contains("--all");
        var dryRun        = args.Contains("--dry-run");
        var onlyEntityId  = ArgValue(args, "--only-entity");
        var onlyPredicate = ArgValue(args, "--only-predicate");
        if (string.IsNullOrEmpty(slug) && !all) return Fail("--reconcile-trinity requires --slug <slug> or --all (or --extract/--survey/--undo)");
        if (!string.IsNullOrEmpty(onlyEntityId) != !string.IsNullOrEmpty(onlyPredicate))
            return Fail("--only-entity and --only-predicate must be passed together");

        // Two independent gates, both required, --dry-run bypasses neither: this is the first
        // DecideAsync caller that rewrites live prose/bible content, not just a ledger flip.
        var allowVotes  = args.Contains("--allow-votes");
        var confirmEdit = args.Contains("--confirm-auto-edit");
        if (!votingGate.IsAllowed(allowVotes) || !confirmEdit)
        {
            Console.WriteLine("[trinity] Refusing to reconcile: requires BOTH --allow-votes (SS-A44 VotingGate) AND --confirm-auto-edit.");
            Console.WriteLine("[trinity] This is the first DecideAsync caller that rewrites live prose/bible content — both flags gate it deliberately.");
            return 1;
        }

        var books = await svc.ResolveScopeAsync(slug, all);
        if (books.Count == 0) return Fail("no in-scope (GLMZ/SCRY/FICTION, NarrativeMode=original) book matched.");

        Console.WriteLine($"[trinity] {(dryRun ? "DRY RUN — " : "")}Reconciling {books.Count} book(s):");
        int totalDecisions = 0;
        foreach (var b in books)
        {
            try
            {
                var result = await svc.ReconcileBookAsync(b.NodeId, dryRun, onlyEntityId: onlyEntityId, onlyPredicate: onlyPredicate);
                Console.WriteLine($"[trinity]   {b.Slug,-16} {result.Decisions.Count} decision(s)");
                foreach (var d in result.Decisions)
                {
                    Console.WriteLine($"[trinity]     [{d.Id}] {d.DivergenceType} {d.EntityName}.{d.Predicate} → \"{d.WinningValue}\" ({d.WinningSourceType}, confidence {d.DecisionConfidence:P0}) via {d.EditMechanism}");
                    if (dryRun) Console.WriteLine($"[trinity]       {d.DecisionReasoning}");
                }
                totalDecisions += result.Decisions.Count;
            }
            catch (Exception ex) { Console.WriteLine($"[trinity]   {b.Slug,-16} ! {ex.Message}"); }
        }
        Console.WriteLine($"[trinity] Done. {totalDecisions} decision(s) across {books.Count} book(s).");
        return 0;
    }

    static async Task<int> CmdUndo(string[] args, TrinityReconciliationService svc)
    {
        var idRaw = ArgValue(args, "--decision-id");
        if (!Guid.TryParse(idRaw, out var id)) return Fail("--undo requires --decision-id <guid>");

        try
        {
            var reverted = await svc.RevertDecisionAsync(id);
            Console.WriteLine(reverted
                ? $"[trinity] Reverted decision {id}."
                : $"[trinity] Decision {id} was already reverted — no-op.");
            return 0;
        }
        catch (Exception ex) { return Fail("undo failed: " + ex.Message); }
    }

    static int Fail(string msg) { Console.Error.WriteLine($"[trinity] {msg}"); PrintUsage(); return 1; }

    static string? ArgValue(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    static void PrintUsage()
    {
        Console.WriteLine("""
            Usage:
              prose --reconcile-trinity --extract --slug <slug>|--all
              prose --reconcile-trinity --survey  --slug <slug>|--all
              prose --reconcile-trinity --slug <slug>|--all --allow-votes --confirm-auto-edit [--dry-run]
                  [--only-entity <entityId> --only-predicate <predicate>]
                  --only-entity/--only-predicate (pass together) restrict to ONE contradiction group and
                  skip the applied-drift loop — the narrow-pilot safety valve for proving the mechanism
                  on a single hand-picked divergence instead of every divergence in the book.
              prose --reconcile-trinity --undo --decision-id <guid>
            """);
    }
}
