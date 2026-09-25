using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Services.Audit;

namespace Prose.Core.Services;

/// <summary>
/// A gate check has three answers, not two. <see cref="CouldNotLook"/> is the one the gate was
/// missing: the instrument behind this check never ran (or read nothing, or read a version of the
/// book that no longer exists), so its zero findings are not evidence of anything.
///
/// <para>It blocks like a failure, because an unexamined book is not a ready one — but it reads
/// differently in the report, because the remedy is "go run the thing", not "go fix the prose".</para>
/// </summary>
public enum CheckOutcome { Pass, Fail, CouldNotLook }

public sealed record PublishReadinessCheck(string Name, CheckOutcome Outcome, string Detail)
{
    /// <summary>Kept so every existing consumer (CLI, MCP, the export pre-flight) still reads
    /// <c>c.Pass</c> unchanged. CouldNotLook is not a pass.</summary>
    public bool Pass => Outcome == CheckOutcome.Pass;

    /// <summary>Convenience for the checks whose evidence really is binary — the ones that read a
    /// persisted state rather than count findings.</summary>
    public PublishReadinessCheck(string name, bool pass, string detail)
        : this(name, pass ? CheckOutcome.Pass : CheckOutcome.Fail, detail) { }
}

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
    /// docs/LOGIC.md §9's six-check publish-readiness convergence gate, computed as one answer
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
        // IgnoreQueryFilters: an explicit id; a book outside the ambient universe read as "not found".
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
            .Where(n => n.Id == nodeId).Select(n => new { n.Slug }).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var slug = node.Slug ?? nodeId.ToString("N");
        var prefix = $"node:{slug}";
        // Delimited: a bare StartsWith also took "node:foo-2…" findings, so another book's open
        // findings blocked this one. Paths are "node:{slug}", "node:{slug}/…" or "node:{slug}#…".
        var slash = prefix + "/";
        var hash = prefix + "#";

        var openFindings = await db.Findings.AsNoTracking()
            .Where(f => (f.FilePath == prefix || f.FilePath.StartsWith(slash) || f.FilePath.StartsWith(hash))
                        && (f.Status == "New" || f.Status == "Triaged"))
            .Select(f => new { f.Category, f.Severity, f.Summary })
            .ToListAsync(ct);

        var checks = new List<PublishReadinessCheck>();

        // 1. Zero open BLOCKER/MODERATE logic-sweep findings. Summary prefix "LOGICSWEEP " (with
        // the trailing space) distinguishes the full-book sweep's own findings from other
        // FindingCategory.Contradiction sources (e.g. ContinuityEnforcer's "CONTINUITY-VIOLATION"
        // findings, or the blast-radius mini-sweep's own "LOGICSWEEP-BLAST" prefix, which does
        // NOT match "LOGICSWEEP " since it has a hyphen, not a space, after the word).
        //
        // Zero here used to mean "clean" unconditionally. It doesn't: a book nobody has ever swept
        // also has zero sweep findings. That produced BCODA's 2026-09-22 report, which printed
        // "✅ logic-sweep BLOCKER/MODERATE = 0 — clean" three lines above "❌ 2 consecutive dry
        // sweep rounds — not converged", about the same sweep. The run ledger is the evidence that
        // was missing; the fingerprint makes a sweep of prose that has since been rewritten count
        // as not having looked at THIS book, which is the honest reading.
        var currentFingerprint = await Audit.LogicSweepService.ComputeBookFingerprintAsync(db, nodeId, ct);
        var sweepBad = openFindings.Count(f => f.Summary.StartsWith("LOGICSWEEP ", StringComparison.Ordinal)
            && (f.Severity == "High" || f.Severity == "Medium"));
        var sweepRun = await Audit.InstrumentRunLedger.LastRunAsync(db, nodeId, Audit.InstrumentRunLedger.LogicSweep, ct);
        var (sweepOutcome, sweepDetail) = Audit.InstrumentRunLedger.Evaluate(
            "the logic sweep", Audit.InstrumentRunLedger.InstrumentRunSummary.From(sweepRun), sweepBad,
            "run prose --logic-sweep --slug <slug>", currentFingerprint);
        checks.Add(new PublishReadinessCheck("logic-sweep BLOCKER/MODERATE = 0", sweepOutcome, sweepDetail));

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
        // Same false-green as check 1: a beat the narrow sweep has never been pointed at has zero
        // blast findings. The recheck fires automatically on every save through the workbench, so
        // any book edited since that landed has a run to show; a book that has none has not been
        // checked clean. No fingerprint — this instrument is scoped to a beat radius, never the
        // whole book, so book-level staleness is the wrong question to ask of it.
        //
        // KNOWN GAP, deliberately not papered over: docs/LOGIC.md §9 scopes this condition to
        // "every fix applied SINCE THE LAST DRY ROUND". This counts any currently-open blast
        // finding on any beat, whenever it was raised. Narrowing it needs a per-beat check stamp;
        // recorded in docs/rfc/0014 rather than half-implemented here.
        var blastRun = await Audit.InstrumentRunLedger.LastRunAsync(db, nodeId, Audit.InstrumentRunLedger.BlastRadius, ct);
        var (blastOutcome, blastDetail) = Audit.InstrumentRunLedger.Evaluate(
            "the blast-radius recheck", Audit.InstrumentRunLedger.InstrumentRunSummary.From(blastRun), blastBad,
            "edit a beat, or run prose --logic-sweep --slug <slug>");
        checks.Add(new PublishReadinessCheck("blast-radius recheck clean", blastOutcome, blastDetail));

        // 5. Zero open High/BLOCKER Reader-Proxy QA findings (comprehension, craft-checklist —
        // incl. the LINT/POV/VOICE/HOOK sub-instruments, gripe jury).
        var readerBad = openFindings.Count(f =>
            (f.Category == nameof(FindingCategory.ComprehensionDefect)
             || f.Category == nameof(FindingCategory.CraftChecklist)
             || f.Category == nameof(FindingCategory.ReaderGripe))
            && f.Severity == "High");
        // Third instance of the same false green, and the one with the least excuse: RFC 0010
        // records that the comprehension probes have never produced a single applied finding,
        // corpus-wide, all time — yet this check gates publication, and on a book they had never
        // read it gated it GREEN. Any one of the three reader-proxy instruments having run counts
        // as having looked; none of them having run does not.
        var readerRuns = new[]
        {
            await Audit.InstrumentRunLedger.LastRunAsync(db, nodeId, Audit.InstrumentRunLedger.ReaderQaProbes, ct),
            await Audit.InstrumentRunLedger.LastRunAsync(db, nodeId, Audit.InstrumentRunLedger.ReaderQaGripe, ct),
            await Audit.InstrumentRunLedger.LastRunAsync(db, nodeId, Audit.InstrumentRunLedger.ReaderQaFullOrder, ct),
        };
        var newestReaderRun = readerRuns.Where(r => r != null).MaxBy(r => r!.CompletedAt);
        var (readerOutcome, readerDetail) = Audit.InstrumentRunLedger.Evaluate(
            "Reader-Proxy QA", Audit.InstrumentRunLedger.InstrumentRunSummary.From(newestReaderRun), readerBad,
            "run prose --reader-qa --slug <slug>");
        checks.Add(new PublishReadinessCheck("Reader-Proxy QA High/BLOCKER = 0", readerOutcome, readerDetail));

        // 6. Obligation ledger balanced (RFC 0013 / LOGIC.md §9 item 6): every beat scanned, and
        // zero obligations past due without an author decision. A book whose ledger has no rows
        // FAILS — an empty ledger is "could not look", never "nothing owed".
        checks.Add(await ObligationLedgerCheckAsync(db, nodeId, ct));

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

    /// <summary>Check 6. Reads the ledger directly rather than the findings inbox so a book that
    /// was never reconciled still fails honestly.</summary>
    private static async Task<PublishReadinessCheck> ObligationLedgerCheckAsync(ProseDbContext db, Guid nodeId, CancellationToken ct)
    {
        const string name = "obligation ledger balanced";
        var clock = await Obligations.NarrativeObligationService.LoadClockAsync(db, nodeId, ct);
        if (clock.BeatCount == 0) return new PublishReadinessCheck(name, false, "no beats");

        var beatIds = clock.Beats.Keys.ToList();
        var scanned = await db.Beats.AsNoTracking().CountAsync(b => beatIds.Contains(b.Id) && b.ObligationScanHash != null, ct);
        var rows = await db.NarrativeObligations.AsNoTracking().Where(o => o.NodeId == nodeId && o.State != Data.Entities.ObligationState.Withdrawn).ToListAsync(ct);
        if (rows.Count == 0 || scanned == 0)
            return new PublishReadinessCheck(name, false, $"COULD NOT LOOK — ledger empty (scan coverage {scanned}/{clock.BeatCount}); run prose --obligations rescan --slug <slug>");

        var overdue = rows.Count(o => Data.Entities.ObligationState.IsOutstanding(o.State) && !o.AuthorLocked
                                      && Obligations.NarrativeObligationService.IsPastDue(o, clock, clock.ChapterCount, clock.BeatCount - 1));
        var openBookEnd = rows.Count(o => Data.Entities.ObligationState.IsOutstanding(o.State) && !o.AuthorLocked && o.DueByKind == Data.Entities.ObligationDueKind.BookEnd);
        var parts = new List<string>();
        if (scanned < clock.BeatCount) parts.Add($"scan coverage {scanned}/{clock.BeatCount}");
        if (overdue > 0) parts.Add($"{overdue} obligation(s) past due with no author decision");
        if (openBookEnd > 0) parts.Add($"{openBookEnd} book-end obligation(s) still open at publish");
        return new PublishReadinessCheck(name, parts.Count == 0, parts.Count == 0 ? $"balanced ({rows.Count} obligations, {scanned}/{clock.BeatCount} beats scanned)" : string.Join("; ", parts));
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

    // ── The seventh condition, which is documentation and not code ────────────────────────
    //
    // docs/LOGIC.md §9 said for a while that a book is publish-ready only when ALL SEVEN
    // conditions hold, the seventh being "zero unentailed entity-record claims, or every one
    // quarantined" (RFC 0013 §4). This method computes SIX. The seventh never existed: what stood
    // here was a doc comment describing "Phase D of the Bible/Book/Entities validation triangle"
    // with no method body under it, and nothing in PublishReadinessAsync ever called
    // EntityRecordGroundingService. The instrument is real and runs standalone
    // (prose --ground-entity-records); it simply was never wired to the gate.
    //
    // It is deliberately NOT being wired now. Entity-record grounding has no calibration behind
    // it — no seeded-defect recall, no negative control, no measured false-positive rate — and
    // adding an unmeasured condition to a gate is how the gate got into this state. An instrument
    // earns a place in the gate by evidence. The remaining work is to measure it; until then the
    // honest count is six, and §9 now says six.
    //
    // Doc comment removed rather than left dangling, because a summary with nothing beneath it
    // reads as an implementation to anyone scanning the file, and that is exactly how a
    // documented seven-condition gate came to ship six.
}
