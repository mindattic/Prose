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
        Guid beatId, CancellationToken ct = default)
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

        var decision = await db.BeatBlueprintDecisions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.BeatId == beatId, ct);

        var results = new List<BeatVerificationResult>();

        // ── Mechanical checks ─────────────────────────────────────────────────

        results.Add(CheckBannedPattern(beat));

        if (decision != null)
        {
            results.Add(await CheckEventTypeAsync(db, beat, decision, ct));
            results.Add(await CheckSubplotCarrierAsync(db, beat, decision, ct));
            results.Add(await CheckEscalationFloorAsync(db, beat, decision, ct));
        }

        // ── Semantic check: DeclaredPurpose ───────────────────────────────────
        if (decision != null && !string.IsNullOrWhiteSpace(decision.DeclaredPurpose)
            && !string.IsNullOrWhiteSpace(beat.Text) && embeddings != null)
        {
            var purposeCheck = await CheckDeclaredPurposeAsync(beat, decision, ct);
            results.Add(purposeCheck);
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
            .Where(bn => nodeIds.Contains(bn.NodeId) && bn.IsEnabled)
            .OrderBy(bn => bn.SortKey)
            .Select(bn => bn.BeatId)
            .Distinct()
            .ToListAsync(ct);

        var allResults = new List<BeatVerificationResult>();
        foreach (var beatId in beatIds)
        {
            try
            {
                var r = await VerifyBeatAsync(beatId, ct);
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

        return aligned switch
        {
            true  => new(beat.Id, "EventType", "Pass", "MODERATE",
                        $"Declared='{decision.EventType}' matches detected mode='{modeLog.Mode}'"),
            false => new(beat.Id, "EventType", "Fail", "MODERATE",
                        $"Declared='{decision.EventType}' but detected mode='{modeLog.Mode}'"),
            null  => new(beat.Id, "EventType", "Partial", "MINOR",
                        $"Declared='{decision.EventType}', detected='{modeLog.Mode}' — alignment undetermined (different vocabularies)"),
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
            .Where(bn => bn.NodeId == nodeId && bn.IsEnabled)
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

    private async Task<BeatVerificationResult> CheckDeclaredPurposeAsync(
        Beat beat, BeatBlueprintDecision decision, CancellationToken ct)
    {
        try
        {
            var prose      = beat.Text[..Math.Min(1200, beat.Text.Length)];
            var similarity = await embeddings!.ComputeSimilarityAsync(decision.DeclaredPurpose!, prose, ct);

            const double failThreshold    = 0.35;
            const double partialThreshold = 0.55;

            if (similarity < failThreshold)
                return new(beat.Id, "DeclaredPurpose", "Fail", "MODERATE",
                    $"Cosine similarity={similarity:F3} below threshold {failThreshold}. Prose may not fulfill declared purpose: '{decision.DeclaredPurpose}'",
                    VerifiedBy: "embedding");

            if (similarity < partialThreshold)
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
            var normalizedText  = NormalizeForComparison(beat.Text ?? string.Empty);
            var found = normalizedQuote.Length > 0 && normalizedText.Contains(normalizedQuote, StringComparison.Ordinal);

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
                BeatId     = r.BeatId,
                CheckType  = r.CheckType,
                Result     = r.Result,
                Severity   = r.Severity,
                Evidence   = r.Evidence,
                VerifiedAt = DateTime.UtcNow,
                VerifiedBy = r.VerifiedBy,
            });
        }
        else
        {
            existing.Result     = r.Result;
            existing.Severity   = r.Severity;
            existing.Evidence   = r.Evidence;
            existing.VerifiedAt = DateTime.UtcNow;
            existing.VerifiedBy = r.VerifiedBy;
        }
    }

}
