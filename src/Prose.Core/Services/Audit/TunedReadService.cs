using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services.Audit;

/// <summary>
/// The Tuned Read — a chapter-aware pass over a whole book that carries verified facts across it
/// at bounded cost, and files a finding when two of them cannot both be true.
///
/// <para><b>The failure it exists to fix.</b> A live contradiction sat in BCODA across ~290 beats
/// of a finished, repeatedly-swept book: one beat named a father for a character the climax
/// establishes as a construct with "no before". Four instruments missed it, and each miss is
/// mechanically explainable:</para>
/// <list type="number">
/// <item><c>LogicSweepService.BuildClampedProse</c> keeps the head 50k and tail 50k of a book and
/// ELIDES the middle above 100k chars. BCODA is ~1.9M. The sweep saw the father claim (head) and
/// the climax (tail); the reveal that reconciles them sat in the elided middle. The evidence was
/// never in the prompt.</item>
/// <item>The fact ledger could not represent the conflict: <c>ContinuityService.Upsert</c> fires
/// only on same-predicate/different-object, and <c>father</c> vs <c>origin</c> are different
/// predicates. Not missed — <i>unrepresentable</i>.</item>
/// <item>Every other instrument windows or shards (range-scoped sweep subagents, a comprehension
/// probe with a three-chapter recap) and none share verified state, so a cross-range
/// contradiction is invisible to all of them at once.</item>
/// <item>With no bounded full-fidelity read path, every reader fell back to the one-line
/// <c>Beat.Description</c> spine — an intent field that had no binding to the prose at all until
/// Phase 1 gave it one.</item>
/// </list>
///
/// <para><b>The resolution gradient (the radio).</b> The cost argument is that the far band is
/// lossless in FACTS and free of prose, so it spans a 500-beat book without growing with it:</para>
/// <list type="bullet">
/// <item><b>Carrier</b> — the anchor beat ± <see cref="CarrierRadius"/>, full verbatim prose.</item>
/// <item><b>Near sideband</b> — the surrounding chapter's <c>Beat.EventSummary</c> lines
/// ("what happened", hash-gated), never <c>Beat.Description</c>.</item>
/// <item><b>Far band</b> — the accumulated ledger claims for the entity under question. No prose.
/// O(distinct facts), not O(beats).</item>
/// </list>
///
/// <para><b>Report-only, by law.</b> docs/LOGIC.md §4 — audits never write prose. This writes
/// ledger claims, findings and verdict rows and nothing else. There is deliberately no bulk
/// rewriter (memory <c>feedback_no_bulk_fix_tools_hand_edit_prose_2026_08_31</c>): findings carry
/// no <c>Snippet</c>, so no apply path can splice over a beat. It is also a COLD-LEDGER
/// instrument only (docs/LOGIC.md §10) — it judges correctness, never whether the book is good,
/// and must never stand in for the full-order read.</para>
/// </summary>
public sealed class TunedReadService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ContinuityService store,
    ContinuityExtractionService extraction,
    PredicateExclusionService exclusions,
    SynopsisExportService synopsis,
    NodeWorkbenchService workbench,
    FindingsService findings,
    ILlmService llm,
    ILogger<TunedReadService> log)
{
    /// <summary>Beats of full verbatim prose either side of an anchor beat. Ten is the plan's
    /// figure: wide enough that a claim's immediate context is present, narrow enough that the
    /// prompt cost is a constant rather than a function of book length.</summary>
    public const int CarrierRadius = 10;

    /// <summary>Prefix on every finding this service files, so the existing
    /// delete-then-recreate lifecycle and staleness reporting work unchanged and TUNEDREAD
    /// findings never collide with the logic sweep's.</summary>
    public const string SummaryPrefix = "TUNEDREAD ";

    /// <summary>Adjudication model. Sonnet, not Haiku: the question is whether two paraphrased
    /// facts are genuinely incompatible, which is exactly the judgement Haiku was measured
    /// failing on for synopsis fidelity (see SynopsisExportService's model note). Cost is per
    /// CANDIDATE and hash-cached, not per beat.</summary>
    private const string AdjudicationModel = "claude-sonnet-5";

    /// <param name="ReExtract">Refresh the ledger from any chapter whose prose changed since
    /// extraction last saw it. Hash-gated, so an unchanged book costs nothing.</param>
    /// <param name="Adjudicate">When false, run only the deterministic half — candidate counts,
    /// zero LLM spend. Useful for checking whether an axiom is too broad before paying to
    /// adjudicate its output.</param>
    /// <param name="MaxCandidates">Hard cap on adjudications per run. A candidate count that
    /// hits this is nearly always a too-broad axiom rather than a genuinely broken book.</param>
    public sealed record TunedReadOptions(
        bool ReExtract = true,
        bool Adjudicate = true,
        int MaxCandidates = 60);

    public sealed record TunedReadFinding(
        string EntityName, string PredicateA, string ObjectA, string PredicateB, string ObjectB,
        string Severity, string Note, string EvidenceQuote, int? BeatNumberA, int? BeatNumberB,
        int? ExclusionRuleId);

    public sealed record TunedReadReport(
        Guid NodeId, string Slug, string Title,
        int Chapters, int Beats, int LiveClaims,
        int CandidatesFromOntology, int CandidatesFromSamePredicate,
        int Adjudicated, int CacheHits, int Confirmed, int Cleared, int GroundingRejected,
        List<TunedReadFinding> Findings,
        List<string> Notes);

    // ── entry point ──────────────────────────────────────────────────────────

    public async Task<TunedReadReport> RunAsync(
        Guid bookNodeId, TunedReadOptions? options = null, CancellationToken ct = default)
    {
        var opts = options ?? new TunedReadOptions();
        var notes = new List<string>();

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // IgnoreQueryFilters(): explicit bookNodeId, not an ambient scope — the same leak that
        // broke --close-all-sessions for every non-default-universe book.
        var book = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
            .Where(n => n.Id == bookNodeId)
            .Select(n => new { n.Id, n.Slug, n.Title, n.UniverseId })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Node {bookNodeId} not found.");

        // ── read order (docs/LOGIC.md §2): chapter units in true reading order ──
        var chapters = await synopsis.GetChapterSourcesAsync(bookNodeId, ct);
        var ordered = await workbench.GetOrderedBeatsAsync(bookNodeId, ct);

        // ── 1. EXTRACT (existing, hash-gated) ───────────────────────────────
        // ContinuityExtractionService already refuses to re-bill a chapter whose content hash is
        // unchanged, and already refuses to extract a book for the first time unsupervised. Both
        // properties are load-bearing here: a re-run on an unchanged book must cost nothing.
        if (opts.ReExtract)
        {
            if (!store.HasAnyClaimsForBook(book.Slug))
            {
                notes.Add(
                    $"'{book.Slug}' has no ledger claims yet, so extraction was skipped — " +
                    "ReExtractChapterIfChangedAsync only keeps an already-opted-in book fresh, it " +
                    "never silently extracts a book for the first time. Run " +
                    "extract_continuity_from_book first to opt this book in.");
            }
            else
            {
                var refreshed = 0;
                foreach (var ch in chapters)
                {
                    ct.ThrowIfCancellationRequested();
                    if (await extraction.ReExtractChapterIfChangedAsync(ch.NodeId, ct: ct)) refreshed++;
                }
                if (refreshed > 0) notes.Add($"Re-extracted {refreshed} chapter(s) whose prose had changed since the ledger last saw them.");
            }
        }

        // ── the far band: every live claim for this book ─────────────────────
        var liveClaims = await LoadLiveClaimsAsync(db, book.Slug, ct);

        // ── 2. COLLIDE ───────────────────────────────────────────────────────
        // (a) the existing same-predicate/different-object mechanism, unchanged.
        var samePredicateGroups = store.GetContradictionGroups(book.Slug);

        // (b) NEW: the exclusion ontology — different predicate, incompatible meaning.
        var rules = await exclusions.GetActiveRulesAsync(book.UniverseId, ct);
        // Reading-order positions for the temporal axioms (docs/LOGIC.md §2 order — chapters by
        // Nodes.SortKey, beats by NodeBeats.SortKey, never Beats.Number, which manufactures false
        // findings). Without this map GenerateCandidates skips every temporal rule rather than
        // evaluating it as a timeless one.
        var beatPositions = ordered
            .Select((o, i) => (o.Beat.Id, i))
            .GroupBy(x => x.Id)
            .ToDictionary(g => g.Key, g => g.First().i);
        var rawCandidates = PredicateExclusionService.GenerateCandidates(liveClaims, rules, beatPositions);
        // Collapse to one question per (entity, axiom). The ledger records the same fact under
        // many predicate names, so the raw cross product asks one question dozens of times —
        // see PredicateExclusionService.Collapse for the measured case (60+ pairs, one defect).
        var ontologyCandidates = PredicateExclusionService.Collapse(rawCandidates);
        if (rawCandidates.Count > ontologyCandidates.Count)
            notes.Add($"Collapsed {rawCandidates.Count} raw claim pair(s) into {ontologyCandidates.Count} " +
                      "distinct question(s) — the ledger records the same facts under several predicate " +
                      "names, and an axiom is a statement about the families, not about two particular rows.");

        if (rules.Count == 0)
            notes.Add("No active exclusion axioms are in scope for this universe, so the " +
                      "cross-predicate half of collision detection found nothing by construction.");

        // "Zero candidates" has two completely different meanings — the book is consistent, or the
        // instrument could not look — and until this diagnostic existed they were indistinguishable
        // from the outside. That ambiguity cost real time: a corpus-wide zero read as "clean" while
        // the actual cause was that 0.1% of the ledger carried a beat anchor. A temporal axiom in
        // particular can be perfectly correct and still never fire, and silence should never be the
        // only thing it reports.
        AppendTemporalDiagnostics(notes, liveClaims, rules, beatPositions);

        // Deterministic, stable order so a truncated run is reproducible rather than arbitrary.
        ontologyCandidates = ontologyCandidates
            .OrderBy(c => c.A.EntityName, StringComparer.Ordinal)
            .ThenBy(c => c.A.ClaimUid, StringComparer.Ordinal)
            .ThenBy(c => c.B.ClaimUid, StringComparer.Ordinal)
            .ToList();

        if (ontologyCandidates.Count > opts.MaxCandidates)
        {
            notes.Add($"{ontologyCandidates.Count} ontology candidates found; adjudicating the first " +
                      $"{opts.MaxCandidates}. A candidate count this high usually means an axiom is too " +
                      "broad — check prose --exclusion-rules before raising the cap.");
            ontologyCandidates = ontologyCandidates.Take(opts.MaxCandidates).ToList();
        }

        var report = new TunedReadReport(
            book.Id, book.Slug, book.Title, chapters.Count, ordered.Count, liveClaims.Count,
            ontologyCandidates.Count, samePredicateGroups.Count,
            0, 0, 0, 0, 0, [], notes);

        if (!opts.Adjudicate)
        {
            notes.Add("Adjudication skipped (--no-adjudicate): candidate counts above are the deterministic, free half.");
            return report;
        }

        // ── 3-5. ADJUDICATE -> GROUND -> FILE ───────────────────────────────
        var beatById = ordered.ToDictionary(o => o.Beat.Id, o => o.Beat);
        var beatOrder = ordered.Select(o => o.Beat).ToList();
        var chapterOfBeat = BuildChapterIndex(ordered, chapters);

        int adjudicated = 0, cacheHits = 0, confirmed = 0, cleared = 0, rejected = 0;
        var results = new List<TunedReadFinding>();

        foreach (var cand in ontologyCandidates)
        {
            ct.ThrowIfCancellationRequested();

            var anchorA = cand.A.SourceBeatId.HasValue ? beatById.GetValueOrDefault(cand.A.SourceBeatId.Value) : null;
            var anchorB = cand.B.SourceBeatId.HasValue ? beatById.GetValueOrDefault(cand.B.SourceBeatId.Value) : null;

            var cacheKey = ComputeCacheKey(cand.A.ClaimUid, cand.B.ClaimUid, cand.Rule.Id,
                anchorA?.TextHash, anchorB?.TextHash);

            var cachedVerdict = await db.TunedReadAdjudications.AsNoTracking()
                .FirstOrDefaultAsync(a => a.CacheKey == cacheKey, ct);

            TunedReadAdjudication verdict;
            if (cachedVerdict != null)
            {
                verdict = cachedVerdict;
                cacheHits++;
            }
            else
            {
                verdict = await AdjudicateAsync(cand, anchorA, anchorB, beatOrder, chapterOfBeat, liveClaims, book.Slug, cacheKey, ct);
                adjudicated++;

                // Cache the verdict either way. A candidate the adjudicator CLEARED must not be
                // re-billed on every future run — that would make a clean book cost the same as a
                // broken one, forever.
                db.TunedReadAdjudications.Add(verdict);
                try { await db.SaveChangesAsync(ct); }
                catch (DbUpdateException ex)
                {
                    // Unique CacheKey collision: another run adjudicated the same question
                    // concurrently. Its answer is as good as ours.
                    log.LogDebug(ex, "[tuned-read] verdict cache collision on {Key} — keeping the existing row.", cacheKey);
                    db.ChangeTracker.Clear();
                }
            }

            if (!string.IsNullOrEmpty(verdict.RejectedReason)) rejected++;

            if (!verdict.IsContradiction) { cleared++; continue; }

            confirmed++;
            var note = verdict.Note ?? "";
            if (cand.FamilySize > 1)
                note += $" (this axiom pairs {cand.FamilySize} claim combinations on this entity — " +
                        "the same fact is recorded under several predicate names)";
            var finding = new TunedReadFinding(
                cand.A.EntityName, cand.A.Predicate, cand.A.Object, cand.B.Predicate, cand.B.Object,
                verdict.Severity ?? "MODERATE", note, verdict.EvidenceQuote ?? "",
                anchorA?.Number, anchorB?.Number, cand.Rule.Id);
            results.Add(finding);

            // Mark every pair the representative stands for, not just the two rows adjudicated —
            // otherwise 63 of 65 fabricated claims stay NEW/CONFIRMED in the store that feeds
            // the ESTABLISHED CANON prompt block.
            var family = rawCandidates
                .Where(c => c.Rule.Id == cand.Rule.Id
                         && string.Equals(c.A.EntityId, cand.A.EntityId, StringComparison.Ordinal))
                .ToList();
            await MarkClaimsContradictedAsync(db, family, ct);
        }

        // Delete-then-recreate at book scope: a contradiction that has since been fixed must lose
        // its finding even though nothing re-emits for it this run.
        findings.DeleteBySummaryPrefix($"node:{book.Slug}", SummaryPrefix);
        foreach (var f in results) FileFinding(book.Slug, f);

        return report with
        {
            Adjudicated = adjudicated,
            CacheHits = cacheHits,
            Confirmed = confirmed,
            Cleared = cleared,
            GroundingRejected = rejected,
            Findings = results,
        };
    }

    /// <summary>
    /// For every temporal axiom that produced nothing, says which gate stopped it: the shape never
    /// matched at all, or it matched and the ordering constraint rejected every pair — and if so,
    /// whether that was for want of a beat anchor or because the second fact simply comes first.
    ///
    /// <para>Re-runs candidate generation with the ordering stripped, which is cheap (a pure
    /// function over claims already in memory) and buys the difference between "this book is
    /// consistent" and "this instrument cannot see".</para>
    /// </summary>
    private static void AppendTemporalDiagnostics(
        List<string> notes,
        List<ContinuityClaim> liveClaims,
        List<PredicateExclusion> rules,
        IReadOnlyDictionary<Guid, int> beatPositions)
    {
        foreach (var rule in rules.Where(PredicateExclusionService.IsTemporal))
        {
            var timeless = new PredicateExclusion
            {
                Id = rule.Id,
                UniverseId = rule.UniverseId,
                PredicateA = rule.PredicateA, ObjectPatternA = rule.ObjectPatternA,
                PredicateB = rule.PredicateB, ObjectPatternB = rule.ObjectPatternB,
                Symmetric = false, TemporalOrder = null,
                Status = rule.Status, Source = rule.Source, Rationale = rule.Rationale,
            };

            var shapeMatches = PredicateExclusionService.GenerateCandidates(liveClaims, [timeless]);
            if (shapeMatches.Count == 0) continue; // shape never occurs here; nothing to explain.

            var kept = shapeMatches.Count(c =>
                PredicateExclusionService.BStrictlyAfterA(c.A, c.B, beatPositions));
            if (kept > 0) continue; // the axiom did fire; no diagnostic needed.

            var unanchored = shapeMatches.Count(c =>
                c.A.SourceBeatId is null || c.B.SourceBeatId is null);
            notes.Add(
                $"Axiom #{rule.Id} matched {shapeMatches.Count} claim pair(s) by shape, but the ordering " +
                $"gate rejected all of them — {unanchored} because a claim carries no beat anchor, " +
                $"{shapeMatches.Count - unanchored} because the second fact is not anchored later than the " +
                "first. This is NOT the same as the book being clean on this axiom" +
                (unanchored > 0
                    ? "; run prose --continuity anchor-beats to recover the anchors the snippets already imply."
                    : "."));
        }
    }

    // ── adjudication ─────────────────────────────────────────────────────────

    private const string AdjudicationSystem = """
You are adjudicating ONE question about a story's internal consistency. You are not reviewing the
prose, judging its quality, or looking for other problems.

You are given two facts recorded about the SAME character or entity, an axiom saying why they may
be incompatible, and the prose around each fact.

Decide whether the two facts genuinely CANNOT both be true of this entity in this story.

Answer NO (not a contradiction) when:
- A later passage reveals, reframes, or explains the earlier one. Revealed later is NOT the same
  as contradicted. A mentor who is eventually revealed to have been a construct was still a real
  mentor in the character's experience; a memory later shown to be implanted was still a memory.
- The two facts describe different moments, and the story shows the change between them.
- One fact is figurative, reported speech, a lie a character tells, or something a character
  merely believes. A character being WRONG is not the story being inconsistent.
- They are two paraphrases of the same underlying fact.

Answer YES only when the story asserts both as literally true of the same entity and no passage
reconciles them.

You MUST cite a verbatim quote, copied EXACTLY character for character from the prose you were
given, that demonstrates the conflicting assertion. A verdict whose quote is not found in that
prose is discarded. Do not paraphrase inside the quote. Do not quote the fact summaries.

Output STRICT JSON, no fences, no commentary, with the fields in EXACTLY this order:
{"note": "one sentence naming what conflicts, or why they are reconcilable", "quote": "verbatim span from the prose", "severity": "BLOCKER"|"MODERATE"|"MINOR", "contradiction": true|false}

Write "note" FIRST and "contradiction" LAST. The note is your reasoning and the verdict must
FOLLOW from it. If your note concludes the facts are reconcilable — revealed later, a different
moment, a lie, a paraphrase — then "contradiction" MUST be false. A note arguing they are
compatible alongside "contradiction": true is a self-refuting answer, and it is worse than either
answer alone because it lands in a findings queue a human then has to un-pick.

severity: BLOCKER if a reader would notice and the story breaks; MODERATE if it survives a
careful read but is wrong; MINOR for a detail. Use "MINOR" and contradiction:false freely —
false is a correct, common answer.
""";

    /// <summary>
    /// Bumped whenever <see cref="AdjudicationSystem"/> changes in a way that should invalidate
    /// cached verdicts. See <c>ClaimGroupAdjudicationService.PromptVersion</c> for the measurement
    /// that motivated v2 — the verdict field moved to LAST so the reasoning precedes it.
    /// </summary>
    private const string PromptVersion = "v2";

    private async Task<TunedReadAdjudication> AdjudicateAsync(
        PredicateExclusionService.ExclusionCandidate cand,
        Beat? anchorA, Beat? anchorB,
        List<Beat> beatOrder,
        Dictionary<Guid, string> chapterOfBeat,
        List<ContinuityClaim> liveClaims,
        string bookSlug, string cacheKey,
        CancellationToken ct)
    {
        var row = new TunedReadAdjudication
        {
            CacheKey = cacheKey,
            ClaimAUid = cand.A.ClaimUid,
            ClaimBUid = cand.B.ClaimUid,
            ExclusionRuleId = cand.Rule.Id,
            BookSlug = bookSlug,
        };

        // Carrier band: verbatim prose around each anchor. This is the ONLY prose in the prompt,
        // and its size is a constant — the whole reason a 1.9M-char book is affordable here.
        var carrierA = BuildCarrier(anchorA, beatOrder);
        var carrierB = BuildCarrier(anchorB, beatOrder);

        if (carrierA.Length == 0 && carrierB.Length == 0)
        {
            // Neither claim has a beat anchor (both predate Phase 2, or both snippets straddled a
            // beat boundary). Adjudicating with no prose would be asking the model to rule on
            // two summaries — which is precisely the paraphrase-only reasoning that produced the
            // original bad report. Refuse instead.
            row.IsContradiction = false;
            row.RejectedReason = "no beat anchor on either claim — cannot adjudicate without prose";
            return row;
        }

        var user = new StringBuilder();
        user.AppendLine($"ENTITY: {cand.A.EntityName}");
        user.AppendLine();
        user.AppendLine("AXIOM UNDER TEST:");
        user.AppendLine($"  {cand.Rule.Rationale}");
        user.AppendLine();

        // Without this the temporal axioms would be self-defeating: the system prompt tells the
        // adjudicator to answer NO when "the two facts describe different moments", and for a
        // temporal axiom the different moments ARE the question. The rule is not "he is dead and
        // he also acts" (true of everyone who dies on-page) but "he was established dead, and the
        // book has him acting afterwards".
        if (PredicateExclusionService.IsTemporal(cand.Rule))
        {
            user.AppendLine("ORDERING — READ THIS BEFORE APPLYING THE GENERAL RULES:");
            user.AppendLine("  Fact 2 is recorded from a beat that comes LATER in the book than fact 1.");
            user.AppendLine("  For this axiom the ordering IS the question, so the general guidance that two");
            user.AppendLine("  facts describing different moments are not a contradiction does NOT apply.");
            user.AppendLine("  Answer YES if fact 1 makes fact 2 impossible at that later point and nothing in");
            user.AppendLine("  the prose reconciles it. Answer NO if the story does reconcile it — a faked or");
            user.AppendLine("  reversed death, a flashback or remembered scene, a recording or message left");
            user.AppendLine("  behind, a namesake, or a different character with a similar name.");
            user.AppendLine();
        }
        user.AppendLine("FACT 1:");
        user.AppendLine($"  {cand.A.Predicate} = {cand.A.Object}");
        if (!string.IsNullOrWhiteSpace(cand.A.Snippet)) user.AppendLine($"  recorded from: \"{cand.A.Snippet}\"");
        if (anchorA != null) user.AppendLine($"  beat #{anchorA.Number}{ChapterSuffix(anchorA, chapterOfBeat)}");
        user.AppendLine();
        user.AppendLine("FACT 2:");
        user.AppendLine($"  {cand.B.Predicate} = {cand.B.Object}");
        if (!string.IsNullOrWhiteSpace(cand.B.Snippet)) user.AppendLine($"  recorded from: \"{cand.B.Snippet}\"");
        if (anchorB != null) user.AppendLine($"  beat #{anchorB.Number}{ChapterSuffix(anchorB, chapterOfBeat)}");
        user.AppendLine();

        // Near sideband: what happened around each fact, from the hash-gated observational
        // EventSummary line — never Beat.Description, which is unbound authorial intent.
        AppendSideband(user, "CONTEXT AROUND FACT 1 (what happened, beat by beat)", anchorA, beatOrder);
        AppendSideband(user, "CONTEXT AROUND FACT 2 (what happened, beat by beat)", anchorB, beatOrder);

        // Far band: everything else the ledger believes about this entity. No prose, so it costs
        // O(facts) and can span the whole book. This is what lets the model see that a reveal
        // elsewhere reconciles the pair — the information the clamped logic sweep never had.
        var others = liveClaims
            .Where(c => string.Equals(c.EntityId, cand.A.EntityId, StringComparison.Ordinal))
            .Where(c => c.ClaimUid != cand.A.ClaimUid && c.ClaimUid != cand.B.ClaimUid)
            .Take(80).ToList();
        if (others.Count > 0)
        {
            user.AppendLine($"EVERYTHING ELSE THE LEDGER RECORDS ABOUT {cand.A.EntityName} (whole book):");
            foreach (var o in others) user.AppendLine($"  - {o.Predicate} = {o.Object}");
            user.AppendLine();
        }

        if (carrierA.Length > 0)
        {
            user.AppendLine("PROSE AROUND FACT 1 (verbatim — quote only from here or the block below):");
            user.AppendLine(carrierA);
            user.AppendLine();
        }
        if (carrierB.Length > 0)
        {
            user.AppendLine("PROSE AROUND FACT 2 (verbatim):");
            user.AppendLine(carrierB);
        }

        string raw;
        try
        {
            raw = await llm.GenerateAsync(AdjudicationSystem, user.ToString(),
                temperature: 0.1, maxTokens: 700, model: AdjudicationModel, ct: ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[tuned-read] adjudication call failed for {A} x {B}", cand.A.ClaimUid, cand.B.ClaimUid);
            row.IsContradiction = false;
            row.RejectedReason = "adjudication call failed: " + ex.Message;
            // Deliberately NOT cached as a clean verdict — see the caller: a row with a
            // RejectedReason is still stored, but the reason distinguishes "we could not ask"
            // from "we asked and it was fine", so a transient outage can never read as a pass.
            return row;
        }

        if (!TryParseVerdict(raw, out var isContradiction, out var severity, out var quote, out var note))
        {
            row.IsContradiction = false;
            row.RejectedReason = "adjudicator response was not parseable JSON";
            return row;
        }

        row.Severity = severity;
        row.EvidenceQuote = quote;
        row.Note = note;

        if (!isContradiction)
        {
            row.IsContradiction = false;
            return row;
        }

        // ── 4. GROUND (existing mechanical gate) ────────────────────────────
        // A contradiction verdict is REJECTED unless its quote actually appears in the prose the
        // adjudicator was shown. This is the single most important line in the file: it is what
        // makes the instrument incapable of the failure it was built to catch. An unquotable
        // assertion about the text is exactly how "Dae-jung Seo" became canon.
        var grounded = QuoteAppearsIn(quote, carrierA) || QuoteAppearsIn(quote, carrierB);
        if (!grounded)
        {
            row.IsContradiction = false;
            row.RejectedReason = "cited quote does not appear verbatim in the prose supplied — verdict discarded";
            log.LogInformation(
                "[tuned-read] Discarded an ungrounded contradiction verdict for {Entity} ({A} x {B}): quote not in prose.",
                cand.A.EntityName, cand.A.Predicate, cand.B.Predicate);
            return row;
        }

        row.IsContradiction = true;
        return row;
    }

    // ── the resolution gradient ──────────────────────────────────────────────

    /// <summary>Carrier band: <see cref="CarrierRadius"/> beats either side of the anchor, full
    /// verbatim prose, entity tags stripped so a quote can match literally.</summary>
    /// <param name="radius">Beats either side of the anchor. Defaults to
    /// <see cref="CarrierRadius"/>. <c>ClaimGroupAdjudicationService</c> passes a much smaller
    /// value: a same-predicate group asks a narrower question than a cross-predicate axiom
    /// (are these two values of ONE named fact compatible?) and does not need the surrounding
    /// scene to answer it, so paying for ±10 beats per anchor there would be spending on prose
    /// nobody reads.</param>
    internal static string BuildCarrier(Beat? anchor, List<Beat> beatOrder, int? radius = null)
    {
        if (anchor == null) return "";
        var idx = beatOrder.FindIndex(b => b.Id == anchor.Id);
        if (idx < 0) return BeatMarkup.StripEntityTags(anchor.Text ?? "");

        var r = radius ?? CarrierRadius;
        var from = Math.Max(0, idx - r);
        var to = Math.Min(beatOrder.Count - 1, idx + r);

        var sb = new StringBuilder();
        for (var i = from; i <= to; i++)
        {
            var b = beatOrder[i];
            if (string.IsNullOrWhiteSpace(b.Text)) continue;
            sb.AppendLine($"[Beat #{b.Number}{(b.Id == anchor.Id ? " — THE FACT IS RECORDED FROM THIS BEAT" : "")}]");
            sb.AppendLine(BeatMarkup.StripEntityTags(b.Text));
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>Near sideband: the observational one-liners around the anchor. Uses
    /// <c>Beat.EventSummary</c> exclusively — <c>Beat.Description</c> is authorial intent and,
    /// before Phase 1, had no binding to the prose at all. A stale summary is labelled rather
    /// than silently trusted (see <c>Beat.SummaryTrustState</c>).</summary>
    private static void AppendSideband(StringBuilder sb, string header, Beat? anchor, List<Beat> beatOrder)
    {
        if (anchor == null) return;
        var idx = beatOrder.FindIndex(b => b.Id == anchor.Id);
        if (idx < 0) return;

        var from = Math.Max(0, idx - 40);
        var to = Math.Min(beatOrder.Count - 1, idx + 40);

        var lines = new List<string>();
        for (var i = from; i <= to; i++)
        {
            var b = beatOrder[i];
            if (string.IsNullOrWhiteSpace(b.EventSummary)) continue;
            var state = b.EventSummaryState;
            var flag = state == "stale" ? " [stale — prose changed since this was written]" : "";
            lines.Add($"  #{b.Number}: {b.EventSummary}{flag}");
        }
        if (lines.Count == 0) return;

        sb.AppendLine(header + ":");
        foreach (var l in lines) sb.AppendLine(l);
        sb.AppendLine();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Mechanical quote grounding, byte-identical in behaviour to
    /// <c>LogicSweepService.QuotedEvidenceAppearsInBeat</c>'s substring half: normalize
    /// whitespace on both sides, then require a literal case-insensitive containment.
    ///
    /// <para>Differs from that method in one deliberate way: it does NOT return true for a
    /// verdict with no quotable span. There, a finding whose evidence contains no quotes at all
    /// is passed through; here an unquotable contradiction is exactly what must be rejected, so
    /// an empty or too-short quote fails closed.</para>
    /// </summary>
    internal static bool QuoteAppearsIn(string? quote, string? prose)
    {
        if (string.IsNullOrWhiteSpace(quote) || string.IsNullOrWhiteSpace(prose)) return false;
        var q = System.Text.RegularExpressions.Regex.Replace(quote, @"\s+", " ").Trim().Trim('"', '\'');
        if (q.Length < 8) return false; // too short to be evidence of anything
        var p = System.Text.RegularExpressions.Regex.Replace(prose, @"\s+", " ");
        return p.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    internal static string ComputeCacheKey(
        string claimAUid, string claimBUid, int ruleId, string? anchorAHash, string? anchorBHash)
    {
        var raw = $"{PromptVersion}|{claimAUid}|{claimBUid}|{ruleId}|{anchorAHash ?? "-"}|{anchorBHash ?? "-"}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant()[..40];
    }

    internal static bool TryParseVerdict(
        string raw, out bool isContradiction, out string severity, out string quote, out string note)
    {
        isContradiction = false; severity = "MODERATE"; quote = ""; note = "";
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start) return false;

        try
        {
            using var doc = JsonDocument.Parse(raw[start..(end + 1)]);
            var r = doc.RootElement;
            isContradiction = r.TryGetProperty("contradiction", out var c)
                && (c.ValueKind == JsonValueKind.True
                    || (c.ValueKind == JsonValueKind.String && bool.TryParse(c.GetString(), out var pb) && pb));
            if (r.TryGetProperty("severity", out var s) && s.ValueKind == JsonValueKind.String)
            {
                var v = (s.GetString() ?? "").Trim().ToUpperInvariant();
                if (v is "BLOCKER" or "MODERATE" or "MINOR") severity = v;
            }
            if (r.TryGetProperty("quote", out var q) && q.ValueKind == JsonValueKind.String) quote = q.GetString() ?? "";
            if (r.TryGetProperty("note", out var n) && n.ValueKind == JsonValueKind.String) note = n.GetString() ?? "";
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task<List<ContinuityClaim>> LoadLiveClaimsAsync(
        ProseDbContext db, string bookSlug, CancellationToken ct)
    {
        // Entity-record claims carry no BookSlug, so a book-slug-only filter would drop exactly
        // the cross-source pairs this instrument exists to catch (a prose claim against an entity
        // record claim). Load this book's prose/outline claims, then pull in every live claim on
        // the entities those name.
        var bookClaims = await db.ContinuityClaims.AsNoTracking()
            .Where(c => c.BookSlug == bookSlug && c.Status != "REJECTED" && c.Status != "SUPERSEDED")
            .ToListAsync(ct);

        var entityIds = bookClaims.Select(c => c.EntityId).Distinct().ToList();
        if (entityIds.Count == 0) return bookClaims;

        var entityClaims = await db.ContinuityClaims.AsNoTracking()
            .Where(c => entityIds.Contains(c.EntityId)
                     && c.BookSlug == null
                     && c.Status != "REJECTED" && c.Status != "SUPERSEDED")
            .ToListAsync(ct);

        return bookClaims.Concat(entityClaims)
            .GroupBy(c => c.ClaimUid, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();
    }

    /// <summary>beatId -> the title of the chapter unit it sits in, so the adjudication prompt
    /// can say "beat #543, Chapter 3 - The Regular" instead of a bare number. Titles come from
    /// the same SynopsisExportService segmentation the read walks, so they match
    /// story-synopsis.txt and --chapters rather than being derived a third way.</summary>
    private static Dictionary<Guid, string> BuildChapterIndex(
        List<NodeWorkbenchService.OrderedBeat> ordered,
        List<SynopsisExportService.ChapterUnit> chapters)
    {
        var titleByNode = chapters
            .GroupBy(c => c.NodeId)
            .ToDictionary(g => g.Key, g => g.First().Title);

        var map = new Dictionary<Guid, string>();
        foreach (var o in ordered)
            if (!map.ContainsKey(o.Beat.Id) && titleByNode.TryGetValue(o.NodeId, out var t) && !string.IsNullOrWhiteSpace(t))
                map[o.Beat.Id] = t;
        return map;
    }

    private static string ChapterSuffix(Beat beat, Dictionary<Guid, string> chapterOfBeat) =>
        chapterOfBeat.TryGetValue(beat.Id, out var title) ? $", {title}" : "";

    /// <summary>Records the exclusion-driven verdict back onto the two claims, so the ledger
    /// itself reflects the contradiction rather than only the Findings inbox. Sets
    /// <c>ExclusionRuleId</c> so the reason is traceable to the axiom that fired.
    ///
    /// <para>A CANONICAL claim is never demoted — the same rule
    /// <c>ContinuityService.Upsert</c> follows: a fact the author has already settled stays
    /// settled until a human explicitly re-resolves it.</para></summary>
    private async Task MarkClaimsContradictedAsync(
        ProseDbContext db, List<PredicateExclusionService.ExclusionCandidate> family, CancellationToken ct)
    {
        if (family.Count == 0) return;
        var ruleId = family[0].Rule.Id;
        var uids = family.SelectMany(c => new[] { c.A.ClaimUid, c.B.ClaimUid }).Distinct().ToArray();
        var rows = await db.ContinuityClaims.Where(c => uids.Contains(c.ClaimUid)).ToListAsync(ct);
        foreach (var r in rows)
        {
            if (r.Status == "CANONICAL") continue;
            r.Status = "CONTRADICTED";
            r.ExclusionRuleId = ruleId;
        }
        await db.SaveChangesAsync(ct);
    }

    private void FileFinding(string bookSlug, TunedReadFinding f)
    {
        var severity = f.Severity switch
        {
            "BLOCKER" => FindingSeverity.High,
            "MINOR" => FindingSeverity.Low,
            _ => FindingSeverity.Medium,
        };

        var where = (f.BeatNumberA, f.BeatNumberB) switch
        {
            (int a, int b) => $" (beats #{a} and #{b})",
            (int a, null) => $" (beat #{a})",
            (null, int b) => $" (beat #{b})",
            _ => "",
        };

        findings.Upsert(
            filePath: $"node:{bookSlug}",
            chapterId: null,
            category: FindingCategory.Contradiction,
            severity: severity,
            summary: $"{SummaryPrefix}[{f.EntityName}] {f.PredicateA}=\"{Clip(f.ObjectA, 80)}\" vs " +
                     $"{f.PredicateB}=\"{Clip(f.ObjectB, 80)}\"{where}: {Clip(f.Note, 400)}",
            // No Snippet, deliberately. docs/LOGIC.md §4 and memory
            // feedback_no_bulk_fix_tools_hand_edit_prose_2026_08_31: without a
            // Snippet/SuggestedFix pair no apply path can splice a machine "fix" over prose. The
            // evidence quote goes in suggestedFix as READING material, not as replacement text.
            snippet: null,
            suggestedFix: $"Evidence: \"{Clip(f.EvidenceQuote, 300)}\". Decide which side is wrong and " +
                          "fix that one beat by hand, or resolve the ledger claim if the prose is right " +
                          "(prose --continuity-resolve). Do not rewrite prose to satisfy the ledger.");
    }

    private static string Clip(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..(max - 1)] + "…";
}
