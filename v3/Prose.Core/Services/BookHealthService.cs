using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Services.Audit;

namespace Prose.Core.Services;

public sealed record PublishReadinessCheck(string Name, bool Pass, string Detail);
public sealed record PublishReadinessReport(Guid NodeId, string Slug, bool Ready, IReadOnlyList<PublishReadinessCheck> Checks);

/// <summary>
/// Publish readiness (docs/LOGIC.md §9) and the Story-Ledger fact check that feeds it. That is
/// all that is left of this class, and deliberately so.
///
/// <para><b>The Full Battery was torn out on 2026-09-06 (author ruling; docs/rfc/0010).</b> This
/// file used to be <c>prose --audit-book</c>: 32 checks across FREE/DEEP/FULL tiers, ~30 injected
/// services, and a "Structural Integrity Index" rolled up from their findings. Measured against the
/// only evidence that matters — findings the author actually applied — three of the 32 checks had
/// ever produced one (fact-ledger, logic-sweep, lint-prose); the FULL tier had produced none, at
/// $27–$135 a run, ~$1,100 over two days. The SII had been retired in spirit on 2026-08-03 and read
/// 0 on a book that passed the publish gate 5/5. Twenty-seven checks existed to feed that number.</para>
///
/// <para>What survives is what other things depend on: <see cref="PublishReadinessAsync"/> (the
/// gate — <c>--publish-readiness</c>, <c>--export-node</c>'s pre-flight, MCP <c>publish_readiness</c>)
/// and <see cref="FactLedgerAsync"/> (<c>--fact-ledger-refresh</c>). Every instrument the gate reads
/// — logic sweep, Story Ledger, tuned read, reader-proxy QA, lint — is invoked by its own command.
/// A bundle that runs everything is not a design; it is the absence of one.</para>
/// </summary>
public class BookHealthService(
    IDbContextFactory<ProseDbContext> dbFactory,
    FindingsService findingsSvc,
    LogicSweepService logicSweep,
    ContinuityService continuity)
{
    /// <summary>
    /// docs/LOGIC.md §9's five-point publish-readiness convergence gate, computed as one answer
    /// (2026-08-30 fix) — previously nothing in the codebase computed this as a single readout;
    /// a user/agent had to manually cross-reference audit-book's findings rollup, the
    /// --until-dry round history, fact-ledger findings, and Reader-Proxy QA findings by hand.
    /// Read-only: makes no LLM calls and runs no new checks — it only reads what earlier
    /// sweep/audit/ledger runs already filed or persisted, so this is safe (and cheap) to call
    /// at any time, not just after a fresh --audit-book run.
    /// </summary>
    public async Task<PublishReadinessReport> PublishReadinessAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking()
            .Where(n => n.Id == nodeId).Select(n => new { n.Slug }).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var slug = node.Slug ?? nodeId.ToString("N");
        var prefix = $"node:{slug}";

        var openFindings = await db.Findings.AsNoTracking()
            .Where(f => f.FilePath.StartsWith(prefix) && (f.Status == "New" || f.Status == "Triaged"))
            .Select(f => new { f.Category, f.Severity, f.Summary })
            .ToListAsync(ct);

        var checks = new List<PublishReadinessCheck>();

        // 1. Zero open BLOCKER/MODERATE logic-sweep findings. Summary prefix "LOGICSWEEP " (with
        // the trailing space) distinguishes the full-book sweep's own findings from other
        // FindingCategory.Contradiction sources (e.g. ContinuityEnforcer's "CONTINUITY-VIOLATION"
        // findings, or the blast-radius mini-sweep's own "LOGICSWEEP-BLAST" prefix, which does
        // NOT match "LOGICSWEEP " since it has a hyphen, not a space, after the word).
        var sweepBad = openFindings.Count(f => f.Summary.StartsWith("LOGICSWEEP ", StringComparison.Ordinal)
            && (f.Severity == "High" || f.Severity == "Medium"));
        checks.Add(new PublishReadinessCheck("logic-sweep BLOCKER/MODERATE = 0", sweepBad == 0,
            sweepBad == 0 ? "clean" : $"{sweepBad} open BLOCKER/MODERATE logic-sweep finding(s)"));

        // 2. Zero open CONTRADICTED claims in the STORY LEDGER (Phase 4, 2026-09-03).
        //
        // This gate used to read one detector: FactLedgerAsync's "FACT-LEDGER [" findings, which
        // come from ContinuityService's same-predicate/different-object rule over a six-item
        // numeric-predicate allowlist. That is a numeric drift detector, and docs/LOGIC.md §9
        // leaned on it as if it were a general consistency gate — so a book could pass here
        // having had exactly nothing checked. It is the mechanism that let a character's
        // fabricated father coexist with his "no before" origin across ~290 beats of a
        // repeatedly-swept book: father vs origin are different predicates, so the old rule
        // could not represent the conflict, let alone flag it.
        //
        // The widened ledger has three faces and this gate now reads all three:
        //   (a) the claim rows themselves — Status == "CONTRADICTED", which BOTH detectors write
        //       (ContinuityService.Upsert for same-predicate, TunedReadService for the exclusion
        //       ontology). Scoped to this book's own claims: an entity-record claim carries no
        //       BookSlug and belongs to no single book, so counting those here would fail every
        //       book that merely mentions a contested entity. The finding side (c) still carries
        //       those, because a TUNEDREAD finding is filed at book scope.
        //   (b) same-predicate findings — unchanged.
        //   (c) TUNEDREAD findings — the cross-predicate half, previously invisible here.
        //
        // "Never extracted" now FAILS instead of passing silently. "[not-extracted]" is a real
        // honest-gap marker and was correctly excluded from a CONTRADICTION count — but a book
        // whose ledger was never populated has not been checked clean, it has not been checked.
        // With no claims there is also nothing for the tuned read to collide, so this single
        // condition covers "never checked" for both detectors at once.
        var hasLedger = await db.ContinuityClaims.AsNoTracking().AnyAsync(c => c.BookSlug == slug, ct);
        // Volatile predicates are excluded here for the same reason GetContradictionGroups
        // excludes them (2026-09-01): location_current/carrying/mood record where a character was
        // at one moment, and a later different value is the character having moved on, not the
        // book contradicting itself. Upsert stopped MARKING those CONTRADICTED in the same fix,
        // but every row written before it still carries the status — counting them here would
        // resurrect exactly the false failures that fix removed, one layer up.
        var contradictedClaims = (await db.ContinuityClaims.AsNoTracking()
                .Where(c => c.BookSlug == slug && c.Status == "CONTRADICTED")
                .Select(c => c.Predicate)
                .ToListAsync(ct))
            .Count(p => !ContinuityService.IsVolatilePredicate(p));
        var samePredicate = openFindings.Count(f => f.Summary.StartsWith("FACT-LEDGER [", StringComparison.Ordinal)
            && !f.Summary.Contains("[not-extracted]", StringComparison.Ordinal));
        var tunedReadOpen = openFindings.Count(f =>
            f.Summary.StartsWith(Audit.TunedReadService.SummaryPrefix, StringComparison.Ordinal));

        checks.Add(StoryLedgerCheck(hasLedger, contradictedClaims, samePredicate, tunedReadOpen));

        // 3. Two consecutive dry sweep rounds, fresh against the book's CURRENT text.
        var converged = await logicSweep.IsConvergedAsync(nodeId, ct: ct);
        checks.Add(new PublishReadinessCheck("2 consecutive dry sweep rounds", converged,
            converged ? "converged" : "not converged — run prose --logic-sweep --slug <slug> --until-dry"));

        // 4. Blast-radius recheck clean for every beat in this book — RunNarrowAsync scopes its
        // findings under "beat:{id}:blast", not "node:{slug}", so this needs its own query
        // rather than the book-prefixed openFindings list above.
        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        var beatIds = await db.BeatNodes.AsNoTracking()
            .Where(bn => searchIds.Contains(bn.NodeId)).Select(bn => bn.BeatId).Distinct().ToListAsync(ct);
        var blastPaths = beatIds.Select(id => $"beat:{id:N}:blast").ToHashSet();
        var openBlastPaths = await db.Findings.AsNoTracking()
            .Where(f => (f.Status == "New" || f.Status == "Triaged") && f.FilePath.EndsWith(":blast"))
            .Select(f => f.FilePath)
            .ToListAsync(ct);
        var blastBad = openBlastPaths.Count(blastPaths.Contains);
        checks.Add(new PublishReadinessCheck("blast-radius recheck clean", blastBad == 0,
            blastBad == 0 ? "clean" : $"{blastBad} open blast-radius finding(s) on this book's beats"));

        // 5. Zero open High/BLOCKER Reader-Proxy QA findings (comprehension, craft-checklist —
        // incl. the LINT/POV/VOICE/HOOK sub-instruments, gripe jury).
        var readerBad = openFindings.Count(f =>
            (f.Category == nameof(FindingCategory.ComprehensionDefect)
             || f.Category == nameof(FindingCategory.CraftChecklist)
             || f.Category == nameof(FindingCategory.ReaderGripe))
            && f.Severity == "High");
        checks.Add(new PublishReadinessCheck("Reader-Proxy QA High/BLOCKER = 0", readerBad == 0,
            readerBad == 0 ? "clean" : $"{readerBad} open High-severity Reader-Proxy QA finding(s)"));

        return new PublishReadinessReport(nodeId, slug, checks.All(c => c.Pass), checks);
    }

    /// <summary>
    /// docs/LOGIC.md §9 gate 2, as a pure decision over the four numbers
    /// <see cref="PublishReadinessAsync"/> reads — separated out so the rule itself is testable
    /// without standing up the whole battery, and so the "never extracted fails" clause cannot
    /// silently regress back to the pass it used to be.
    /// </summary>
    internal static PublishReadinessCheck StoryLedgerCheck(
        bool hasLedger, int contradictedClaims, int samePredicateFindings, int tunedReadFindings)
    {
        var parts = new List<string>();
        if (!hasLedger)
            parts.Add("the fact ledger has never been populated for this book — nothing was checked " +
                      "(run prose --continuity extract --book <slug>, then prose --tuned-read --slug <slug>)");
        if (contradictedClaims > 0) parts.Add($"{contradictedClaims} claim row(s) still CONTRADICTED");
        if (samePredicateFindings > 0) parts.Add($"{samePredicateFindings} open same-predicate finding(s)");
        if (tunedReadFindings > 0) parts.Add($"{tunedReadFindings} open tuned-read contradiction(s)");
        return new PublishReadinessCheck("story-ledger CONTRADICTED = 0", parts.Count == 0,
            parts.Count == 0 ? "clean" : string.Join("; ", parts));
    }

    /// <summary>Wires ContinuityService's ledger of atomic (entity, predicate, object) claims —
    /// fully built (Upsert, contradiction detection, resolution lifecycle) but never called from
    /// the automated battery before this fix — into a per-book Finding. Numeric predicates
    /// (ages, tenures, etc.) are compared arithmetic-safely by ContinuityService.ObjectsMatch
    /// (2026-08-14 fix), so re-derived phrasing across sweep rounds ("fifty" vs "50") no longer
    /// manufactures the false contradiction that VIGL hit repeatedly this session — only a
    /// genuine numeric discrepancy (fifty vs sixty) still surfaces here.
    /// This check's coverage is bounded by whether ContinuityExtractionService has ever been run
    /// for this book (nothing runs it automatically per beat save yet) — HasAnyClaimsForBook
    /// distinguishes "extracted and clean" from "never extracted," same honest-gap pattern as
    /// the (since-deleted) battery's no-pov-data finding.</summary>
    /// <summary>Public (2026-09-01) so a narrow, zero-LLM-cost CLI command
    /// (<c>prose --fact-ledger-refresh</c>) can re-run just this check on demand — the only
    /// existing entry point was the cost-gated <c>--audit-book --deep</c> bundle (~15 other
    /// LLM-call checks alongside this free one), which made "did my ContinuityService fix
    /// actually shrink this book's fact-ledger count" a ~$70 question to answer.</summary>
    public Task FactLedgerAsync(string slug, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Read before touching Findings — a read failure here (e.g. DB connectivity) should
        // propagate to the caller (--fact-ledger-refresh) and leave prior Findings untouched, same
        // "never purge on a failed read" discipline as every other check in this file.
        var hasClaims = continuity.HasAnyClaimsForBook(slug);
        var groups = hasClaims ? continuity.GetContradictionGroups(slug) : [];

        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "FACT-LEDGER ");

        if (!hasClaims)
        {
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.Other, FindingSeverity.Low,
                "FACT-LEDGER [not-extracted]: no continuity claims tagged for this book — the fact ledger has " +
                "never been populated here, not because no hard facts needed tracking.",
                snippet: null,
                suggestedFix: "Run prose --continuity extract --book <slug> (or the MCP ExtractContinuityFromBook tool) to backfill the ledger.");
            return Task.CompletedTask;
        }

        // Only groups that still hold a CONTRADICTED member (2026-09-04). A group is defined by
        // divergent VALUES, not by status, so every group re-formed here even after its verdict
        // had been settled — `--continuity reassess` clearing it under the corrected rules, or
        // `--ledger-adjudicate` reading the prose and finding the values compatible. The claim-row
        // half of publish-readiness gate 2 already reflected those settlements while this half
        // re-filed a finding for all 371/310/275 groups regardless, so the same book reported 128
        // contradicted rows and 371 contradiction findings at once. Nothing can be hidden by this:
        // a divergence nobody has judged still carries the CONTRADICTED rows Upsert wrote.
        groups = groups
            .Where(g => g.Claims.Any(c => c.Status == "CONTRADICTED"))
            .ToList();

        foreach (var g in groups)
        {
            var variants = string.Join(" vs. ", g.Claims.Select(c =>
                $"\"{c.Object}\" ({c.SourceType}{(c.SourceChapterNumber.HasValue ? $", ch.{c.SourceChapterNumber}" : "")})"));
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.Contradiction, FindingSeverity.Medium,
                $"FACT-LEDGER [{g.EntityName}.{g.Predicate}]: conflicting values — {variants}",
                snippet: null,
                suggestedFix: "Resolve via the /continuity UI (or ContinuityService.Resolve/MakeCanonical) — pick the load-bearing value, or supply a custom one if neither is right.");
        }
        return Task.CompletedTask;
    }

    /// <summary>Phase D of the Bible/Book/Entities validation triangle: for every claim already
    /// applied to its entity's canon record (<c>ContinuityApplyService.ApplyAsync</c>, which sets
    /// AppliedAt/AppliedToField), verify the field still says what the claim asserted. Answers
    /// "are all entities mentioned actually correct in the repo" for the applied subset —
    /// deterministic (JSON field comparison), no LLM call. Same honest-gap framing as
    /// FactLedgerAsync/HasAnyClaimsForBook: zero applied claims for a book means "nothing has ever
    /// been applied here," not "verified clean."</summary>
}
