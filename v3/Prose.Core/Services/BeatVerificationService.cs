using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using System.Text.RegularExpressions;

namespace Prose.Core.Services;

public record BeatVerificationResult(
    Guid BeatId,
    string CheckType,
    string Result,
    string Severity,
    string? Evidence,
    string VerifiedBy = "mechanical");

public record BookVerificationSummary(
    Guid NodeId,
    string Slug,
    int BeatsChecked,
    int Blockers,
    int Moderates,
    int Minors,
    int Passed,
    int Skipped,
    List<BeatVerificationResult> Findings,
    int Partials = 0);

/// <summary>
/// Beat Verification Engine (Track C — Truth-First Architecture).
/// Runs mechanical and semantic checks against each beat's prose to verify it
/// fulfilled its declared BeatBlueprintDecision contract.
///
/// Mechanical checks (pure SQL + pattern matching):
///   EscalationFloor    — declared floor vs emotional depth score (when scored)
///   EventType          — declared event type vs BeatModeLog.Mode (approximate)
///   BannedPattern      — "internal_understanding" and epilogue anti-patterns
///   SubplotCarrier     — subplot character absent from BeatEntities when declared
///   EscalationMonotonic — book-wide curve regression detection
///
/// Semantic checks (embedding similarity — requires EmbeddingService):
///   DeclaredPurpose    — embed cosine similarity between declared purpose and prose
///
/// Results are upserted into BeatVerification (one row per (BeatId, CheckType)).
/// </summary>
public class BeatVerificationService
{
    /// <summary>
    /// Bump whenever a check's LOGIC changes — a new threshold, a new outlier gate, a Result
    /// mapping change (mirrors <see cref="BeatChecklistGateService"/>'s PromptVersion). Stamped
    /// onto every <see cref="BeatVerification"/> row on write; <see cref="GetStaleBookSlugsAsync"/>
    /// uses it to answer "which books still have findings computed under old logic" as a direct
    /// query. "v1" here is the outlier-gate + EventType Skip-vs-Partial fix (2026-08-10) — the
    /// first version this project ever stamped; every row without a matching value (including
    /// pre-existing NULL rows from before this column existed) is stale by definition.
    /// </summary>
    public const string CurrentRuleVersion = "v1";

    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<BeatVerificationService> log;
    private readonly EmbeddingService? embeddings;

    public BeatVerificationService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<BeatVerificationService> log,
        EmbeddingService? embeddings = null)
    {
        this.dbFactory  = dbFactory;
        this.log        = log;
        this.embeddings = embeddings;
    }

    // ── Verify a single beat ─────────────────────────────────────────────────

    public async Task<List<BeatVerificationResult>> VerifyBeatAsync(
        Guid beatId, CancellationToken ct = default) =>
        await VerifyBeatAsync(beatId, declaredPurposeBaseline: null, ct);

    /// <param name="declaredPurposeBaseline">This book's other DeclaredPurpose cosine
    /// similarities, for the same per-book outlier normalization SemanticFidelityService uses
    /// (see its 2026-08-10 fix note) — null when verifying a beat standalone (CLI/MCP single-
    /// beat calls), in which case the absolute thresholds alone decide, same as before.</param>
    public async Task<List<BeatVerificationResult>> VerifyBeatAsync(
        Guid beatId, IReadOnlyList<double>? declaredPurposeBaseline, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var beat = await db.Beats
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (beat == null)
        {
            log.LogWarning("[BeatVerification] Beat {BeatId} not found", beatId);
            return new List<BeatVerificationResult>();
        }
        // `beat` is AsNoTracking (detached, never saved back) — safe to mutate in place so every
        // check below (regex pattern matching, embedding similarity, comparison normalization)
        // sees plain reader-facing text instead of having to remember to strip it individually.
        beat.Text = BeatMarkup.StripEntityTags(beat.Text);

        var decision = await db.BeatBlueprintDecisions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.BeatId == beatId, ct);

        var results = new List<BeatVerificationResult>();

        // ── Mechanical checks ─────────────────────────────────────────────────

        // BannedPattern encodes StoryScope/avalanche-ending fiction-craft anti-tells (no
        // epilogue, no internal-understanding narration, no lesson-statement closes) — the
        // same fiction-shaped assumptions as EventType/SubplotCarrier/EscalationFloor below, so
        // it shares their decision != null gate. Found 2026-08-12: it ran unconditionally on
        // every beat corpus-wide, so nonfiction/historical-analysis books that never went
        // through Stage 6 blueprint generation (GOSPEL's Matthew/Mark/Luke/John — 0
        // BeatBlueprintDecisions rows, same as the nonfiction books in RFC 0011 Brick 4) got
        // BLOCKER-severity false positives on legitimate scholarly usage, e.g. "the Gospel's
        // epilogue" (John 21) tripping the literal "no epilogue" fiction rule and blocking
        // export.
        if (decision != null)
        {
            results.Add(CheckBannedPattern(beat));
            results.Add(await CheckEventTypeAsync(db, beat, decision, ct));
            results.Add(await CheckSubplotCarrierAsync(db, beat, decision, ct));
            results.Add(await CheckEscalationFloorAsync(db, beat, decision, ct));
        }

        // ── Semantic check: DeclaredPurpose ───────────────────────────────────
        if (decision != null && !string.IsNullOrWhiteSpace(decision.DeclaredPurpose)
            && !string.IsNullOrWhiteSpace(beat.Text) && embeddings != null)
        {
            var purposeCheck = await CheckDeclaredPurposeAsync(beat, decision, declaredPurposeBaseline, ct);
            results.Add(purposeCheck);
        }

        // ── Reap orphaned rows for checks that no longer apply ────────────────
        //
        // Found 2026-08-10: a beat's BeatBlueprintDecision can be deleted out from under it
        // (a blueprint restructuring pass, a chapter-granular consolidation fix) while the beat
        // itself stays live and enabled. EventType/SubplotCarrier/EscalationFloor/DeclaredPurpose
        // are only added to `results` when their precondition (decision != null, etc.) still
        // holds — so once a precondition stops holding, the OLD row from when it did just sits
        // in BeatVerifications forever. It is never in `results`, so the upsert loop below never
        // touches it, and — critically — it is immune to every future re-run: TRUCE beat #16241
        // survived 3+ separate --audit-book passes across one session this way, each one
        // correctly computing 1 result (BannedPattern) and never revisiting the other 4 stale
        // rows because nothing ever told them they were no longer wanted. Delete any existing
        // per-beat-check row whose CheckType isn't in this run's results — "this run says the
        // check doesn't apply" is itself the authoritative, current answer for that CheckType.
        var perBeatCheckTypes = new[] { "BannedPattern", "EventType", "SubplotCarrier", "EscalationFloor", "DeclaredPurpose" };
        var currentCheckTypes = results.Select(r => r.CheckType).ToHashSet();
        var orphaned = await db.BeatVerifications
            .Where(v => v.BeatId == beatId && perBeatCheckTypes.Contains(v.CheckType) && !currentCheckTypes.Contains(v.CheckType))
            .ToListAsync(ct);
        if (orphaned.Count > 0)
        {
            db.BeatVerifications.RemoveRange(orphaned);
            log.LogInformation(
                "[BeatVerification] Beat {BeatId}: reaped {Count} orphaned row(s) for check(s) no longer applicable — {Types}",
                beatId, orphaned.Count, string.Join(", ", orphaned.Select(o => o.CheckType)));
        }

        // ── Upsert results to DB ──────────────────────────────────────────────
        foreach (var r in results)
            await UpsertVerificationAsync(db, r, ct);

        await db.SaveChangesAsync(ct);

        log.LogInformation(
            "[BeatVerification] Beat {BeatId}: {Count} checks — {Blockers} BLOCKER, {Moderates} MODERATE, {Minors} MINOR, {Passed} Pass, {Skipped} Skip",
            beatId, results.Count,
            results.Count(r => r.Result == "Fail" && r.Severity == "BLOCKER"),
            results.Count(r => r.Result == "Fail" && r.Severity == "MODERATE"),
            results.Count(r => r.Result == "Fail" && r.Severity == "MINOR"),
            results.Count(r => r.Result == "Pass"),
            results.Count(r => r.Result == "Skipped"));

        return results;
    }

    // ── Verify all beats in a book ──────────────────────────────────────────

    public async Task<BookVerificationSummary> VerifyBookAsync(
        string slugOrCode, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Slug == slugOrCode || n.NodeCode == slugOrCode, ct);
        if (node == null)
            throw new InvalidOperationException($"Node '{slugOrCode}' not found.");

        // Beats may live directly on the book node OR on chapter children (SS-A43 hierarchy).
        // Recurses past any nested Collection (2026-08-09 fix).
        var nodeIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id, ct);

        var beatIds = await db.BeatNodes
            .Where(bn => nodeIds.Contains(bn.NodeId) && true)
            .OrderBy(bn => bn.SortKey)
            .Select(bn => bn.BeatId)
            .Distinct()
            .ToListAsync(ct);

        // Pre-pass: compute this book's own DeclaredPurpose cosine-similarity distribution so
        // CheckDeclaredPurposeAsync can flag outliers relative to the book's own register
        // instead of a fixed absolute threshold blind to it (2026-08-10 fix — see that method's
        // own doc comment). Best-effort: a failure here just means every beat in this run falls
        // back to the absolute-threshold-only behavior, same as a standalone single-beat call.
        IReadOnlyList<double>? declaredPurposeBaseline = null;
        if (embeddings != null)
        {
            try
            {
                var candidates = await (
                    from bn2 in db.BeatNodes.AsNoTracking()
                    join b2 in db.Beats.AsNoTracking() on bn2.BeatId equals b2.Id
                    join d2 in db.BeatBlueprintDecisions.AsNoTracking() on b2.Id equals d2.BeatId
                    where beatIds.Contains(b2.Id)
                       && !string.IsNullOrWhiteSpace(d2.DeclaredPurpose)
                       && !string.IsNullOrWhiteSpace(b2.Text)
                    select new { d2.DeclaredPurpose, b2.Text }
                ).ToListAsync(ct);

                if (candidates.Count > 0)
                {
                    var pairs = candidates
                        .Select(c =>
                        {
                            var clean = BeatMarkup.StripEntityTags(c.Text);
                            return (c.DeclaredPurpose!, clean[..Math.Min(1200, clean.Length)]);
                        })
                        .ToList();
                    declaredPurposeBaseline = await embeddings.ComputeSimilaritiesBatchAsync(pairs, ct);
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "[BeatVerification] DeclaredPurpose baseline pre-pass failed for '{Slug}' — falling back to absolute thresholds", slugOrCode);
            }
        }

        var allResults = new List<BeatVerificationResult>();
        foreach (var beatId in beatIds)
        {
            try
            {
                var r = await VerifyBeatAsync(beatId, declaredPurposeBaseline, ct);
                allResults.AddRange(r);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "[BeatVerification] Beat {BeatId} verification failed, skipping", beatId);
            }
        }

        // Book-wide escalation monotonicity check
        var escalationResults = await CheckEscalationMonotonicAsync(db, node.Id, ct);
        allResults.AddRange(escalationResults);
        foreach (var r in escalationResults)
            await UpsertVerificationAsync(db, r, ct);
        await db.SaveChangesAsync(ct);

        var slug = node.Slug;
        return new BookVerificationSummary(
            node.Id, slug,
            beatIds.Count,
            allResults.Count(r => r.Result == "Fail" && r.Severity == "BLOCKER"),
            allResults.Count(r => r.Result == "Fail" && r.Severity == "MODERATE"),
            allResults.Count(r => r.Result == "Fail" && r.Severity == "MINOR"),
            allResults.Count(r => r.Result == "Pass"),
            allResults.Count(r => r.Result == "Skipped"),
            // BUG FIX: "Partial" results (EventType alignment-undetermined, DeclaredPurpose
            // partial-similarity) were previously invisible everywhere — not counted in any
            // bucket above and excluded from Findings (Fail-only), so an inconclusive check
            // silently vanished from every report instead of surfacing as "needs a human look."
            allResults.Where(r => r.Result == "Fail" || r.Result == "Partial").ToList(),
            allResults.Count(r => r.Result == "Partial"));
    }

    // ── Mechanical check implementations ─────────────────────────────────────

    private static BeatVerificationResult CheckBannedPattern(Beat beat)
    {
        if (string.IsNullOrWhiteSpace(beat.Text))
            return new(beat.Id, "BannedPattern", "Skipped", "BLOCKER", "No prose text");

        var patterns = new[]
        {
            (@"\b(finally|suddenly)\s+(understood|realized|saw clearly|knew)\b", "Internal-understanding resolution: '{match}' — show the change, never name the insight."),
            (@"\bepilogue\b", "Epilogue keyword — resolution must arrive in the avalanche, never after."),
            (@"everything (would|could|might) be okay", "False reassurance close — costs must be visible and permanent."),
            (@"learned (his|her|their|a) lesson", "Lesson-statement close — insight must be shown through action, not narrated."),
        };

        foreach (var (pattern, template) in patterns)
        {
            var m = Regex.Match(beat.Text, pattern, RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var evidence = template.Replace("{match}", m.Value);
                return new(beat.Id, "BannedPattern", "Fail", "BLOCKER", evidence);
            }
        }

        return new(beat.Id, "BannedPattern", "Pass", "BLOCKER", null);
    }

    private static async Task<BeatVerificationResult> CheckEventTypeAsync(
        ProseDbContext db, Beat beat, BeatBlueprintDecision decision, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(decision.EventType))
            return new(beat.Id, "EventType", "Skipped", "MODERATE", "No declared event type");

        var modeLog = await db.BeatModeLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.BeatId == beat.Id, ct);

        if (modeLog == null)
            return new(beat.Id, "EventType", "Skipped", "MODERATE", "No BeatModeLog row — run after prose is generated");

        // Map declared event type to expected beat mode (approximate alignment)
        var declaredLower = decision.EventType.ToLowerInvariant();
        var detectedLower = modeLog.Mode.ToLowerInvariant();

        bool? aligned =
            (declaredLower == "combat"                     && detectedLower == "combat") ? true :
            (declaredLower.Contains("dialogue")            && detectedLower == "dialogue") ? true :
            (declaredLower.Contains("revelation")          && detectedLower == "revelation") ? true :
            (declaredLower.Contains("transition")          && detectedLower == "transition") ? true :
            (declaredLower.Contains("climax")              && detectedLower == "emotionalclimax") ? true :
            null;   // vocabularies differ — cannot determine alignment

        // Bug fixed 2026-08-10: the null case used to return "Partial"/MINOR, which
        // BookHealthService.BeatVerificationAsync's filter (Result != "Pass" && Result !=
        // "Skipped") treats as finding-worthy — every declared event type outside the 5
        // explicitly mapped keywords (combat/dialogue/revelation/transition/climax) falls
        // through here, and the blueprint's own event-type palette is a rich narrative
        // vocabulary (departure, loss, discovery, confrontation, ...) that mostly ISN'T one of
        // those 5 — so this fired "undetermined" as a permanent, uninformative Finding for the
        // large majority of beats corpus-wide (56 instances measured across the books checked
        // this session), the same "the check can never produce a real signal but still files
        // one anyway" shape as the UNSCORED bug fixed earlier this session. "Partial" is
        // semantically wrong here: the check didn't find ambiguous evidence, it simply can't
        // compare two vocabularies that don't overlap. "Skipped" is the correct, honest label —
        // already excluded from Finding-generation, matching the other genuinely-inapplicable
        // cases in this method (no declared event type, no BeatModeLog row yet).
        return aligned switch
        {
            true  => new(beat.Id, "EventType", "Pass", "MODERATE",
                        $"Declared='{decision.EventType}' matches detected mode='{modeLog.Mode}'"),
            false => new(beat.Id, "EventType", "Fail", "MODERATE",
                        $"Declared='{decision.EventType}' but detected mode='{modeLog.Mode}'"),
            null  => new(beat.Id, "EventType", "Skipped", "MINOR",
                        $"Declared='{decision.EventType}', detected='{modeLog.Mode}' — no comparable vocabulary (not a defect)"),
        };
    }

    private static async Task<BeatVerificationResult> CheckSubplotCarrierAsync(
        ProseDbContext db, Beat beat, BeatBlueprintDecision decision, CancellationToken ct)
    {
        if (!decision.SubplotCarrier)
            return new(beat.Id, "SubplotCarrier", "Skipped", "MODERATE", "Beat not declared as subplot carrier");

        var entityCount = await db.BeatEntityMentions
            .Where(be => be.BeatId == beat.Id)
            .CountAsync(ct);

        if (entityCount == 0)
            return new(beat.Id, "SubplotCarrier", "Fail", "MODERATE",
                "SubplotCarrier=true but no BeatEntityMentions populated for this beat — ensure entities are seeded and SceneContextAssembler ran.");

        return new(beat.Id, "SubplotCarrier", "Pass", "MODERATE",
            $"SubplotCarrier=true and {entityCount} entity mentions found in BeatEntityMentions");
    }

    private static async Task<BeatVerificationResult> CheckEscalationFloorAsync(
        ProseDbContext db, Beat beat, BeatBlueprintDecision decision, CancellationToken ct)
    {
        if (decision.EscalationFloor == null)
            return new(beat.Id, "EscalationFloor", "Skipped", "MODERATE", "No escalation floor declared");

        // Find the most recent emotional examination that scored this beat
        var beatScore = await db.EmotionalBeatScores
            .AsNoTracking()
            .Where(s => s.BeatNumber == beat.Number)
            .OrderByDescending(s => s.ExaminationId)
            .FirstOrDefaultAsync(ct);

        if (beatScore == null)
            return new(beat.Id, "EscalationFloor", "Skipped", "MODERATE",
                "No EmotionalBeatScore exists — run prose --examine-emotion first");

        // Depth 0–4; floor is 0–10. Scale: floor/10 * 4 = expected depth
        var expectedDepth = (double)decision.EscalationFloor.Value / 10.0 * 4.0;
        var actualDepth   = (double)beatScore.Depth;

        if (actualDepth < expectedDepth - 0.75)  // half-grade tolerance
            return new(beat.Id, "EscalationFloor", "Fail", "MODERATE",
                $"Depth={beatScore.Depth}/4 ({actualDepth:F1}) below floor {decision.EscalationFloor}/10 (≈{expectedDepth:F1} depth). {beatScore.Note}");

        return new(beat.Id, "EscalationFloor", "Pass", "MODERATE",
            $"Depth={beatScore.Depth}/4 meets floor {decision.EscalationFloor}/10");
    }

    private static async Task<List<BeatVerificationResult>> CheckEscalationMonotonicAsync(
        ProseDbContext db, Guid nodeId, CancellationToken ct)
    {
        var results = new List<BeatVerificationResult>();

        // Load all BeatBlueprintDecisions for this node in order
        var decisions = await db.BeatNodes
            .Where(bn => bn.NodeId == nodeId && true)
            .OrderBy(bn => bn.SortKey)
            .Join(db.BeatBlueprintDecisions, bn => bn.BeatId, d => d.BeatId, (bn, d) => new
            {
                d.BeatId,
                d.EscalationFloor,
                bn.SortKey,
            })
            .Where(x => x.EscalationFloor != null)
            .ToListAsync(ct);

        if (decisions.Count < 2) return results;

        for (int i = 1; i < decisions.Count; i++)
        {
            var prev = decisions[i - 1];
            var curr = decisions[i];
            if (curr.EscalationFloor < prev.EscalationFloor)
            {
                results.Add(new(curr.BeatId, "EscalationMonotonic", "Fail", "MODERATE",
                    $"Escalation regression: floor dropped from {prev.EscalationFloor} to {curr.EscalationFloor} at SortKey {curr.SortKey}"));
            }
        }

        if (results.Count == 0 && decisions.Count > 0)
        {
            // Report Pass on the last beat as a book-level summary
            var lastBeatId = decisions[^1].BeatId;
            results.Add(new(lastBeatId, "EscalationMonotonic", "Pass", "MODERATE",
                $"Escalation curve is non-decreasing across {decisions.Count} declared beats"));
        }

        return results;
    }

    // ── Semantic check: DeclaredPurpose ──────────────────────────────────────

    // Bug fixed 2026-08-10: fixed absolute thresholds (0.35 Fail / 0.55 Partial) applied
    // identically to every book, same shape as SemanticFidelityService's IntentAlignmentFloor
    // bug (see its own fix note — 6/6 sampled findings there were confirmed faithful prose
    // false-flagged for register mismatch between an abstract declared-purpose sentence and
    // concrete prose). Confirmed the same failure here directly: beat #4200's synopsis
    // ("Establishes the translation problem... bridging them requires deliberate, effortful
    // reformulation") against its actual prose — a full, rich, unambiguous scene embodying
    // exactly that (Elias's "Are you safe?" exchange) — scored 0.281 (Fail). Reused
    // SemanticFidelityService.IsIntentOutlier rather than duplicating the logic: a beat below
    // the absolute threshold must ALSO be a genuine statistical outlier within this book's own
    // DeclaredPurpose-similarity distribution to count, exactly as for SemanticDrift.
    private async Task<BeatVerificationResult> CheckDeclaredPurposeAsync(
        Beat beat, BeatBlueprintDecision decision, IReadOnlyList<double>? bookBaseline, CancellationToken ct)
    {
        try
        {
            var prose      = beat.Text[..Math.Min(1200, beat.Text.Length)];
            var similarity = await embeddings!.ComputeSimilarityAsync(decision.DeclaredPurpose!, prose, ct);

            const double failThreshold    = 0.35;
            const double partialThreshold = 0.55;

            // No baseline (standalone single-beat verification, no book-wide context available)
            // falls back to the absolute thresholds alone — an outlier test needs other beats
            // to compare against.
            var isOutlier = bookBaseline == null || SemanticFidelityService.IsIntentOutlier(similarity, bookBaseline);

            if (similarity < failThreshold && isOutlier)
                return new(beat.Id, "DeclaredPurpose", "Fail", "MODERATE",
                    $"Cosine similarity={similarity:F3} below threshold {failThreshold}. Prose may not fulfill declared purpose: '{decision.DeclaredPurpose}'",
                    VerifiedBy: "embedding");

            if (similarity < partialThreshold && isOutlier)
                return new(beat.Id, "DeclaredPurpose", "Partial", "MINOR",
                    $"Cosine similarity={similarity:F3} — partial alignment with declared purpose.",
                    VerifiedBy: "embedding");

            return new(beat.Id, "DeclaredPurpose", "Pass", "MODERATE",
                $"Cosine similarity={similarity:F3} — prose aligns with declared purpose.",
                VerifiedBy: "embedding");
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[BeatVerification] DeclaredPurpose embedding check failed for beat {BeatId}", beat.Id);
            return new(beat.Id, "DeclaredPurpose", "Skipped", "MODERATE",
                $"Embedding check failed: {ex.Message}");
        }
    }

    // ── Quote grounding (audit-claim verification) ───────────────────────────
    //
    // Logic-sweep audit agents report findings as quoted text attributed to a SortKey/BeatId.
    // Two real incidents (2026-07-24 VIGL sweep) showed agents can misattribute or fabricate
    // an exact quote — usually by reading the wrong beat under time pressure, not by intent.
    // This check is the mechanical guard: before a claimed finding is trusted for triage/fix,
    // confirm the claimed quote actually appears in the beat it's attributed to. Unlike the
    // other checks above (one row per BeatId+CheckType, upserted — they describe a beat's
    // current intrinsic state), each quote-grounding call is a distinct historical claim-check,
    // so rows are always INSERTED, never upserted — a beat can accumulate many of these across
    // many sweeps without one overwriting another.

    /// <summary>
    /// Verifies that <paramref name="claimedQuote"/> actually appears in the given beat's text.
    /// Comparison is normalized (dash variants, curly/straight quotes, collapsed whitespace)
    /// so a claim isn't rejected merely because sqlcmd/terminal display altered punctuation —
    /// only genuine misattribution or fabrication fails this check.
    /// </summary>
    public async Task<BeatVerificationResult> VerifyQuoteGroundingAsync(
        Guid beatId, string claimedQuote, string? claimedBy = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId, ct);

        BeatVerificationResult result;
        if (beat == null)
        {
            result = new(beatId, "QuoteGrounding", "Fail", "BLOCKER",
                $"Beat {beatId} does not exist — the claim's SortKey/BeatId is itself wrong.",
                claimedBy ?? "unknown");
        }
        else if (string.IsNullOrWhiteSpace(claimedQuote))
        {
            result = new(beatId, "QuoteGrounding", "Skipped", "BLOCKER", "No quote supplied to check.",
                claimedBy ?? "unknown");
        }
        else
        {
            var normalizedQuote = NormalizeForComparison(claimedQuote);
            // Strip inline <entity guid="...">Name</entity> tags before comparing — same fix
            // LogicSweepService.QuotedEvidenceAppearsInBeat already applies (2026-08-14): a tag
            // wrapping a proper noun inside a genuinely-quoted span breaks literal Contains()
            // continuity and turns a true, correctly-cited quote into a false "fabricated" verdict.
            // Confirmed live 2026-08-22 (BCODA sweep): "Moss, from an earlier job" and "the catalog
            // value of the Atlas hardware..." both failed this check purely because "Moss"/"Atlas"
            // were entity-tagged in the stored text, not because the quotes were fabricated.
            var normalizedText = NormalizeForComparison(BeatMarkup.StripEntityTags(beat.Text ?? string.Empty));
            // Case-insensitive: a re-typed or paraphrase-adjacent quote (e.g. mid-sentence lowercase
            // vs. the beat's actual sentence-initial capital) is not fabrication — same "don't reject
            // over incidental transcription differences" principle as the dash/quote normalization
            // above.
            var found = normalizedQuote.Length > 0 && normalizedText.Contains(normalizedQuote, StringComparison.OrdinalIgnoreCase);

            result = found
                ? new(beatId, "QuoteGrounding", "Pass", "BLOCKER",
                    $"Quote confirmed present in beat (normalized match). Quote: \"{Truncate(claimedQuote, 160)}\"",
                    claimedBy ?? "unknown")
                : new(beatId, "QuoteGrounding", "Fail", "BLOCKER",
                    $"Quote NOT found in this beat's text — likely misattributed to the wrong beat or fabricated. Reject this finding until re-verified. Quote: \"{Truncate(claimedQuote, 160)}\"",
                    claimedBy ?? "unknown");
        }

        db.BeatVerifications.Add(new BeatVerification
        {
            BeatId     = beatId,
            CheckType  = "QuoteGrounding",
            Result     = result.Result,
            Severity   = result.Severity,
            Evidence   = result.Evidence,
            VerifiedAt = DateTime.UtcNow,
            VerifiedBy = result.VerifiedBy,
        });
        await db.SaveChangesAsync(ct);

        if (result.Result == "Fail")
            log.LogWarning("[QuoteGrounding] REJECTED — beat {BeatId} does not contain claimed quote (claimed by {ClaimedBy}): {Evidence}",
                beatId, claimedBy ?? "unknown", result.Evidence);

        return result;
    }

    /// <summary>
    /// Batch form: verify every (BeatId, Quote) claim from an audit report in one pass.
    /// Use this to gate an entire audit report before triage — any Fail means that specific
    /// finding must be re-verified against the real beat before it's acted on.
    /// </summary>
    public async Task<List<BeatVerificationResult>> VerifyQuoteGroundingBatchAsync(
        IEnumerable<(Guid BeatId, string Quote)> claims, string? claimedBy = null, CancellationToken ct = default)
    {
        var results = new List<BeatVerificationResult>();
        foreach (var (beatId, quote) in claims)
            results.Add(await VerifyQuoteGroundingAsync(beatId, quote, claimedBy, ct));
        return results;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    private static string NormalizeForComparison(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;

        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var c in s)
        {
            char n = c switch
            {
                '‐' or '‑' or '‒' or '–' or '—' or '―' => '-',
                '‘' or '’' or '‚' or '‛'                        => '\'',
                '“' or '”' or '„' or '‟'                        => '"',
                _ => c,
            };
            sb.Append(n);
        }

        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    // ── Upsert helper ────────────────────────────────────────────────────────

    private static async Task UpsertVerificationAsync(
        ProseDbContext db, BeatVerificationResult r, CancellationToken ct)
    {
        var existing = await db.BeatVerifications
            .FirstOrDefaultAsync(v => v.BeatId == r.BeatId && v.CheckType == r.CheckType, ct);

        if (existing == null)
        {
            db.BeatVerifications.Add(new BeatVerification
            {
                BeatId      = r.BeatId,
                CheckType   = r.CheckType,
                Result      = r.Result,
                Severity    = r.Severity,
                Evidence    = r.Evidence,
                VerifiedAt  = DateTime.UtcNow,
                VerifiedBy  = r.VerifiedBy,
                RuleVersion = CurrentRuleVersion,
            });
        }
        else
        {
            existing.Result      = r.Result;
            existing.Severity    = r.Severity;
            existing.Evidence    = r.Evidence;
            existing.VerifiedAt  = DateTime.UtcNow;
            existing.VerifiedBy  = r.VerifiedBy;
            existing.RuleVersion = CurrentRuleVersion;
        }
    }

    // ── Staleness reporting ──────────────────────────────────────────────────

    public sealed record StaleBook(string Slug, string Title, int StaleRows, int TotalRows);

    /// <summary>
    /// Every book with at least one enabled beat carrying a <see cref="BeatVerification"/> row
    /// whose <see cref="BeatVerification.RuleVersion"/> doesn't match <see cref="CurrentRuleVersion"/>
    /// (including legacy rows with a null RuleVersion, predating this column). Answers "which
    /// books need a `--verify-book`/`--audit-book` re-run after a check-logic change" directly —
    /// see the RuleVersion doc comment for why this exists (the same staleness gap was found and
    /// manually re-diffed twice in one session before this method did).
    /// </summary>
    public async Task<List<StaleBook>> GetStaleBookSlugsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // IgnoreQueryFilters — this report is deliberately corpus-wide across every universe,
        // not scoped to whatever universe happens to be ambient for this CLI invocation (see
        // Program.cs's UniverseAgnosticCommands entry for --verification-staleness).
        var rows = await (
            from bv in db.BeatVerifications.AsNoTracking().IgnoreQueryFilters()
            join bn in db.BeatNodes.AsNoTracking().IgnoreQueryFilters() on bv.BeatId equals bn.BeatId
            where true
            select new { bn.NodeId, bv.RuleVersion }
        ).ToListAsync(ct);
        if (rows.Count == 0) return new List<StaleBook>();

        var nodeIds = rows.Select(r => r.NodeId).Distinct().ToList();
        var bookByLeaf = new Dictionary<Guid, Guid>();
        foreach (var leafId in nodeIds)
        {
            var bookId = await ResolveBookAncestorIdAsync(db, leafId, ct);
            bookByLeaf[leafId] = bookId;
        }

        var byBook = rows.GroupBy(r => bookByLeaf[r.NodeId]).Select(g => new
        {
            BookId = g.Key,
            Total = g.Count(),
            Stale = g.Count(r => r.RuleVersion != CurrentRuleVersion),
        }).Where(g => g.Stale > 0).ToList();
        if (byBook.Count == 0) return new List<StaleBook>();

        var bookIds = byBook.Select(b => b.BookId).ToList();
        var titles = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => bookIds.Contains(n.Id))
            .Select(n => new { n.Id, n.Slug, n.Title })
            .ToDictionaryAsync(n => n.Id, ct);

        return byBook
            .Where(b => titles.ContainsKey(b.BookId))
            .Select(b => new StaleBook(titles[b.BookId].Slug ?? "", titles[b.BookId].Title ?? "", b.Stale, b.Total))
            .OrderByDescending(b => b.StaleRows)
            .ToList();
    }

    /// <summary>Walks ParentNodeId up from a leaf (chapter) node to its book ancestor (no parent,
    /// or a Collection-kind root) — same walk-up shape as the rest of this service's book-scoping,
    /// inverted (leaf-to-root instead of root-to-leaf via GetLeafDescendantIdsAsync).</summary>
    private static async Task<Guid> ResolveBookAncestorIdAsync(ProseDbContext db, Guid leafId, CancellationToken ct)
    {
        var currentId = leafId;
        for (var i = 0; i < 10; i++)
        {
            var parentId = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                .Where(n => n.Id == currentId).Select(n => n.ParentNodeId).FirstOrDefaultAsync(ct);
            if (parentId is not { } p) return currentId;
            currentId = p;
        }
        return currentId;
    }
}
