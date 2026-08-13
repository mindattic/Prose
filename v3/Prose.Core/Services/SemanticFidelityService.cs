using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Detects the Semantic Fidelity Gap: the Goodhart's Law failure mode where
/// prose optimises for reviewer scores while drifting away from the story's
/// actual meaning. High score + low semantic alignment = gaming the metric.
///
/// Two checks per scored beat:
///
///   Bible alignment  — cosine similarity between the beat's prose and the
///                      node's Seed/Synopsis ("the north star"). A high-scoring
///                      beat that no longer resembles the story it was born from
///                      has traded meaning for metric.
///
///   Intent alignment — cosine similarity between the beat's Synopsis (what it
///                      was supposed to do) and its actual prose. Drift here means
///                      the rewrite served the score rubric, not the beat's purpose.
///
/// Violations are filed as SEMANTIC-DRIFT findings in FindingsService. The findings
/// workflow (apply / dismiss) is the course-correction mechanism.
///
/// <see cref="AuditNodeAsync"/> evaluates every beat that has prose — it does NOT gate on
/// <see cref="ScoreGamingThreshold"/> (fixed 2026-08-08: under the no-panel-vote regime,
/// SS-A44, fewer than 1% of beats corpus-wide ever have a Beat.Score, so gating on it here
/// silently evaluated ~0 beats on 34 of 35 live books). Only <see cref="AuditNodeAsync"/>'s
/// caller in NodeReviewService still uses the threshold — to decide whether a *just-reviewed*
/// node scored well enough to be worth auditing for gaming at all; that's a legacy-panel-only
/// decision, separate from which beats this method itself checks.
/// It can also be invoked directly via `prose --check-fidelity` or the
/// `check_semantic_fidelity` MCP tool.
/// </summary>
public class SemanticFidelityService
{
    /// <summary>Used only by NodeReviewService to decide whether a node's post-review score is
    /// high enough to bother auditing for score-gaming at all (legacy panel path only — SS-A44).
    /// Does NOT gate which beats <see cref="AuditNodeAsync"/> evaluates; see its remarks above.</summary>
    public const double ScoreGamingThreshold = 70.0;

    /// <summary>Cosine similarity floor for bible alignment. Below this the beat has
    /// drifted significantly from the story's Seed/Synopsis anchor.</summary>
    public const double BibleAlignmentFloor = 0.42;

    /// <summary>Cosine similarity floor for intent alignment. Below this the beat's
    /// prose no longer serves its stated Synopsis/purpose. Necessary but not sufficient as of
    /// 2026-08-10 — see the per-book outlier check in <see cref="AuditNodeAsync"/>.</summary>
    public const double IntentAlignmentFloor = 0.50;

    /// <summary>Minimum beats-with-a-score sample size before trusting a per-book mean/stddev
    /// for the intent-alignment outlier test. Below this, fall back to the absolute floor
    /// alone — too few points to know whether a low score is this book's normal register or a
    /// genuine anomaly.</summary>
    public const int IntentOutlierMinSample = 15;

    /// <summary>How many standard deviations below a book's own mean intent-alignment a beat
    /// must fall to count as a genuine outlier, once <see cref="IntentOutlierMinSample"/> is
    /// met.
    ///
    /// Raised from 1.5 to 2.5 on 2026-08-10, same day as the fix that introduced it: verified
    /// live against TLC's 47 post-fix survivors (z=1.5) by reading a further, larger sample
    /// (8 more beats beyond the original BLST spot-check) and found the per-book normalization
    /// alone was NOT sufficient for a book with more internal score variance than BLST — two
    /// full-paragraph beats (#10125's detailed employment backstory, #10513's exam-table
    /// flashback) were still confirmed near-perfect synopsis matches despite reading as z~1.5-2
    /// outliers, alongside several very short but faithful beats (#10092's bare "412.7." planting
    /// exactly the "cryptic numeric motif" its synopsis asked for). Across 11 total samples
    /// checked this session (BLST + TLC, spanning 1 to 78 words, all three original severity
    /// tiers), zero were confirmed genuine drift — only one (BLST #9042, "Twelve.") was judged
    /// "extreme enough to be worth a second look," and that was at a much larger effective
    /// deviation than the more borderline TLC cases. 2.5 (~0.6% of a normal distribution, vs
    /// 1.5's ~7%) requires a beat to be a much more extreme statistical anomaly before firing,
    /// closer to the deviation level of the one sample that read as plausible on inspection.</summary>
    public const double IntentOutlierZScore = 2.5;

    private readonly EmbeddingService embeddings;
    private readonly FindingsService findings;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<SemanticFidelityService> log;

    public SemanticFidelityService(
        EmbeddingService embeddings,
        FindingsService findings,
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<SemanticFidelityService> log)
    {
        this.embeddings = embeddings;
        this.findings = findings;
        this.dbFactory = dbFactory;
        this.log = log;
    }

    public record FidelityViolation(
        Guid BeatId,
        int BeatNumber,
        string? BeatTitle,
        double? Score,
        double BibleAlignment,
        double? IntentAlignment,
        string Kind,
        string Message,
        string? SuggestedFix);

    public record FidelityReport(
        Guid NodeId,
        string Slug,
        int BeatsChecked,
        int BeatsEvaluated,
        double? NodeScore,
        double MeanBibleAlignment,
        double? MeanIntentAlignment,
        IReadOnlyList<FidelityViolation> Violations,
        int FindingsEmitted);

    /// <summary>
    /// Audit a node's prose for the Semantic Fidelity Gap. Ensures beat
    /// embeddings are fresh (drift-skipped), ranks every beat against the
    /// story's bible anchor, computes intent alignment for every beat that
    /// has a Synopsis, then files SEMANTIC-DRIFT findings for violators.
    /// Beat.Score is informational only here — it is not a gate.
    /// </summary>
    public async Task<FidelityReport> AuditNodeAsync(
        Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking()
            .Where(s => s.Id == nodeId)
            .Select(s => new { s.Id, s.Slug, s.Title, s.Seed, s.Description, s.Score })
            .FirstOrDefaultAsync(ct);

        if (node == null)
        {
            log.LogWarning("SemanticFidelity: node {Id} not found.", nodeId);
            return new FidelityReport(nodeId, "", 0, 0, null, 0, null, Array.Empty<FidelityViolation>(), 0);
        }

        // Story anchor: the Seed (the original one-line story prompt) is the best north
        // star. Fall back to Description, then Title if neither exists.
        var bibleAnchor = FirstNonEmpty(node.Seed, node.Description, node.Title);
        bool hasBibleAnchor = !string.IsNullOrWhiteSpace(bibleAnchor);

        // SS-A43: beats live on chapter nodes (children), not directly on the story node.
        // Recurses past any nested Collection (2026-08-09 fix).
        var beatNodeIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);

        var beats = await (
            from sb in db.BeatNodes.AsNoTracking()
            join b  in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
            where beatNodeIds.Contains(sb.NodeId) && true
            orderby sb.SortKey
            select new
            {
                b.Id, b.Number, b.Title, b.Description,
                b.Text, b.Score, b.Version
            }
        ).ToListAsync(ct);

        if (beats.Count == 0)
            return new FidelityReport(nodeId, node.Slug, 0, 0, node.Score, 0, null,
                Array.Empty<FidelityViolation>(), 0);

        // Ensure prose embeddings are current (drift-skipped — cheap if nothing changed).
        try { await embeddings.ReembedBeatNodesAsync(nodeId, ct); }
        catch (Exception ex)
        {
            log.LogWarning(ex, "SemanticFidelity: re-embed failed for '{Slug}'", node.Slug);
        }

        // ── Bible alignment ───────────────────────────────────────────────
        // Query all beats in this node ranked by cosine similarity to the story anchor.
        var bibleAlignmentById = new Dictionary<Guid, double>();
        var bibleQuerySucceeded = true;
        if (hasBibleAnchor)
        {
            try
            {
                var hits = await embeddings.FindSimilarBeatNodesAsync(
                    bibleAnchor!, k: beats.Count + 10, nodeScope: nodeId, ct: ct);
                foreach (var hit in hits)
                    bibleAlignmentById[hit.ScopeId] = hit.Similarity;
            }
            catch (Exception ex)
            {
                bibleQuerySucceeded = false;
                log.LogWarning(ex, "SemanticFidelity: bible-alignment query failed for '{Slug}'", node.Slug);
            }
        }

        // ── Intent alignment ──────────────────────────────────────────────
        // Every beat with a Synopsis and prose — no score gate (see class remarks: gating on
        // Beat.Score here evaluated ~0 beats corpus-wide once panel voting went opt-in). Embed
        // synopsis+prose as a pair and compute their cosine similarity. One batch call
        // per ~64 beats keeps API cost near zero for typical node lengths.
        var intentAlignmentById = new Dictionary<Guid, double>();
        var intentCandidates = beats
            .Where(b => !string.IsNullOrWhiteSpace(b.Description)
                     && !string.IsNullOrWhiteSpace(b.Text))
            .ToList();

        var intentQuerySucceeded = true;
        if (intentCandidates.Count > 0)
        {
            try
            {
                var pairs = intentCandidates
                    .Select(b => (b.Description!, b.Text!))
                    .ToList();
                var sims = await embeddings.ComputeSimilaritiesBatchAsync(pairs, ct);
                for (int i = 0; i < intentCandidates.Count; i++)
                    intentAlignmentById[intentCandidates[i].Id] = sims[i];
            }
            catch (Exception ex)
            {
                intentQuerySucceeded = false;
                log.LogWarning(ex, "SemanticFidelity: intent-alignment batch failed for '{Slug}'", node.Slug);
            }
        }

        // 2026-08-09 fix: the whole-node purge used to run unconditionally BEFORE either query
        // above, regardless of whether they actually succeeded — a single failed embedding call
        // (bible OR intent) silently deleted every prior real SEMANTIC-DRIFT finding of that kind
        // with nothing computed to replace it (same fail-open bug class as SwainAuditService/
        // CanonContradictionService this session). Purge each kind independently, and only when
        // its own query succeeded this run — a beat that's since been fixed still gets its stale
        // finding cleared (the original purpose of this purge), but a failed query leaves prior
        // findings of that kind untouched instead of silently erasing them.
        if (bibleQuerySucceeded) findings.DeleteBySummaryPrefix($"node:{node.Slug}", "SEMANTIC-DRIFT [bible]");
        if (intentQuerySucceeded) findings.DeleteBySummaryPrefix($"node:{node.Slug}", "SEMANTIC-DRIFT [intent]");
        findings.DeleteBySummaryPrefix($"node:{node.Slug}", "SEMANTIC-DRIFT [incomplete]");

        // ── Identify violations ───────────────────────────────────────────
        // Every beat with prose is checked — Beat.Score is informational only (see class
        // remarks). The alignment floors are the only gate.
        var violations = new List<FidelityViolation>();
        var evaluatedBeats = beats.Where(b => !string.IsNullOrWhiteSpace(b.Text)).ToList();

        // Bug fixed 2026-08-10: IntentAlignmentFloor (0.50) is a fixed GLOBAL floor applied
        // identically to every book regardless of that book's own prose register. Investigated
        // live (BLST, [[project_semantic_drift_false_positive_2026_08_10]]): sampled 6 flagged
        // findings across all three severity tiers (29%-50% similarity) and confirmed 6/6 are
        // faithful, well-executed prose — a book written in a consistently terse, concrete
        // register scores systematically low against its own abstract one-line synopses purely
        // from vocabulary/register mismatch, independent of true fidelity. One sample (#8778, a
        // long multi-paragraph beat) still only scored 50% (right at the floor) — ruling out
        // "just exempt short beats" as the fix, since the effect isn't length-specific, it's
        // register-specific to this book's whole style. No confirmed TRUE positive exists in the
        // sample to calibrate a new absolute number against, so lowering the floor to some other
        // guessed constant would repeat the same mistake with a different number.
        //
        // Fix: flag a beat only if it is BOTH below the absolute floor AND a statistical outlier
        // within its OWN book's intent-alignment distribution (more than IntentOutlierZScore
        // standard deviations below that book's own mean). A book whose style uniformly depresses
        // the raw similarity score has a low mean and tight spread, so nothing in it reads as an
        // outlier — exactly the desired behavior. A book where most beats align well but a few
        // genuinely drifted ones score far below its own baseline still flags those, because they
        // remain real outliers relative to a normal distribution for that book. Requires a
        // minimum sample size (IntentOutlierMinSample) before applying the relative test at all —
        // a handful of beats can't establish a meaningful distribution, so those fall back to the
        // absolute floor alone (matching the prior, unconditional-floor behavior for small books).
        var allIntentAlignsForOutlier = intentAlignmentById.Values.ToList();

        foreach (var b in evaluatedBeats)
        {
            var score       = b.Score;
            var scoreLabel  = score.HasValue ? $"scores {score.Value:0.#}%" : "is unscored";
            var bibleAlign  = bibleAlignmentById.TryGetValue(b.Id, out var ba)  ? ba  : (double?)null;
            var intentAlign = intentAlignmentById.TryGetValue(b.Id, out var ia) ? ia  : (double?)null;

            // Bible drift: prose has drifted from the story's north star. When the beat also
            // has a (legacy panel) score, a high score alongside low alignment is specifically
            // Goodhart's Law — gaming the metric while losing the meaning; call that out.
            if (hasBibleAnchor && bibleAlign.HasValue && bibleAlign.Value < BibleAlignmentFloor)
            {
                var sev = bibleAlign.Value < 0.30 ? FindingSeverity.High
                        : bibleAlign.Value < 0.37 ? FindingSeverity.Medium
                        : FindingSeverity.Low;
                var goodhart = score.HasValue
                    ? " High score / low meaning alignment — Goodhart's Law in prose."
                    : " Low meaning alignment with the story's own intent.";
                var msg  = $"Beat #{b.Number} {scoreLabel} but aligns only {bibleAlign.Value:P0} with the story bible " +
                           $"(floor {BibleAlignmentFloor:P0}).{goodhart}";
                var fix  = $"Revise Beat #{b.Number} to re-anchor in the story's core intent. " +
                           $"Story seed: \"{(bibleAnchor!.Length > 120 ? bibleAnchor[..120] + "…" : bibleAnchor)}\". " +
                           $"Avoid rewriting purely to satisfy stylistic patterns the reviewers reward if it pulls the beat away from the story's centre of gravity.";
                violations.Add(new FidelityViolation(
                    b.Id, b.Number, b.Title, score,
                    bibleAlign.Value, intentAlign, "bible", msg, fix));
                EmitFinding($"node:{node.Slug}", sev,
                    $"SEMANTIC-DRIFT [bible]: {msg}",
                    b.Text?.Length > 200 ? b.Text[..200] : b.Text, fix);
            }

            // Intent drift: prose no longer serves the beat's stated purpose. Below the
            // absolute floor is necessary but no longer sufficient — see IsIntentOutlier's own
            // doc comment and the 2026-08-10 fix note above.
            if (intentAlign.HasValue && intentAlign.Value < IntentAlignmentFloor
                && IsIntentOutlier(intentAlign.Value, allIntentAlignsForOutlier))
            {
                var sev = intentAlign.Value < 0.35 ? FindingSeverity.High
                        : intentAlign.Value < 0.43 ? FindingSeverity.Medium
                        : FindingSeverity.Low;
                var synopsis = b.Description!.Length > 120 ? b.Description[..120] + "…" : b.Description;
                var msg  = $"Beat #{b.Number} {scoreLabel} but its prose aligns only {intentAlign.Value:P0} with its stated intent (\"{synopsis}\"). " +
                           $"The rewrite drifted from the beat's own purpose.";
                var fix  = $"Beat #{b.Number} was supposed to: \"{b.Description}\". " +
                           $"Revise to fulfil that purpose.";
                violations.Add(new FidelityViolation(
                    b.Id, b.Number, b.Title, score,
                    bibleAlign ?? 0, intentAlign, "intent", msg, fix));
                EmitFinding($"node:{node.Slug}", sev,
                    $"SEMANTIC-DRIFT [intent]: {msg}",
                    b.Text?.Length > 200 ? b.Text[..200] : b.Text, fix);
            }
        }

        if (!bibleQuerySucceeded || !intentQuerySucceeded)
        {
            var which = string.Join(" and ", new[]
            {
                !bibleQuerySucceeded  ? "bible-alignment"  : null,
                !intentQuerySucceeded ? "intent-alignment" : null,
            }.Where(x => x != null));
            EmitFinding($"node:{node.Slug}", FindingSeverity.Low,
                $"SEMANTIC-DRIFT [incomplete]: {which} could not be computed this run (embedding errors) — re-run once resolved.",
                snippet: null, fix: null);
        }

        // ── Aggregates ────────────────────────────────────────────────────
        var allBibleAligns  = bibleAlignmentById.Values.ToList();
        var allIntentAligns = intentAlignmentById.Values.ToList();
        var meanBible  = allBibleAligns.Count  > 0 ? allBibleAligns.Average()  : 0.0;
        double? meanIntent = allIntentAligns.Count > 0 ? allIntentAligns.Average() : null;

        log.LogInformation(
            "SemanticFidelity '{Slug}': {Total} beats, {Evaluated} evaluated, {V} violations, " +
            "mean bible={Bible:P0}{Intent}",
            node.Slug, beats.Count, evaluatedBeats.Count, violations.Count, meanBible,
            meanIntent.HasValue ? $", mean intent={meanIntent:P0}" : "");

        return new FidelityReport(
            NodeId:           nodeId,
            Slug:               node.Slug,
            BeatsChecked:       beats.Count,
            BeatsEvaluated:     evaluatedBeats.Count,
            NodeScore:        node.Score,
            MeanBibleAlignment: meanBible,
            MeanIntentAlignment: meanIntent,
            Violations:         violations,
            FindingsEmitted:    violations.Count);
    }

    /// <summary>
    /// Lightweight per-beat drift check wired into the beat-save path.
    /// Runs without a score gate — unlike <see cref="AuditNodeAsync"/>, this
    /// fires the moment prose changes so drift is caught before a review run.
    /// Swallows all exceptions: quality checks must never block a save.
    /// </summary>
    public async Task CheckBeatIntentDriftAsync(
        int beatNumber, string nodeSlug, string beatText, string synopsis,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(synopsis) || string.IsNullOrWhiteSpace(beatText))
            return;
        try
        {
            // 2026-08-09 fix: the delete used to run BEFORE ComputeSimilarityAsync, inside the
            // same try block that swallows every exception ("quality checks must never block a
            // save" — that contract is kept). If the embedding call threw, the beat's real prior
            // SEMANTIC-DRIFT finding was already deleted with nothing to replace it — same
            // fail-open bug class as SwainAuditService/CanonContradictionService this session,
            // just at beat-hook scope. Compute first; only clear the prior finding once there's
            // a fresh verdict ready to take its place.
            var similarity = await embeddings.ComputeSimilarityAsync(synopsis, beatText, ct);

            // Beat-scoped so a since-fixed beat's stale finding is cleared even when this
            // save no longer triggers a re-emit below (Upsert alone never removes a row whose
            // triggering condition has stopped holding — see AuditNodeAsync's book-wide purge
            // for the same reasoning at whole-book scope).
            var filePath = $"node:{nodeSlug}/beat:{beatNumber}";
            findings.DeleteBySummaryPrefix(filePath, "SEMANTIC-DRIFT [intent]:");

            if (similarity >= IntentAlignmentFloor) return;

            var sev = similarity < 0.35 ? FindingSeverity.High
                    : similarity < 0.43 ? FindingSeverity.Medium
                    : FindingSeverity.Low;
            var snip  = synopsis.Length > 120 ? synopsis[..120] + "…" : synopsis;
            var msg   = $"Beat #{beatNumber} prose aligns only {similarity:P0} with its stated intent (\"{snip}\"). " +
                        "Prose may have drifted from its purpose on save.";
            var fix   = $"Beat #{beatNumber} was supposed to: \"{synopsis}\". " +
                        "Revise to fulfil that purpose.";
            EmitFinding(filePath, sev,
                $"SEMANTIC-DRIFT [intent]: {msg}",
                beatText.Length > 200 ? beatText[..200] : beatText, fix);
        }
        catch (Exception ex)
        {
            // Deliberately still swallow — quality checks must never block a save (unchanged
            // contract). The prior finding for this beat survives untouched, since the delete
            // above now only runs after a successful similarity computation.
            log.LogWarning(ex, "CheckBeatIntentDrift failed for Beat #{Number} node {Slug}",
                beatNumber, nodeSlug);
        }
    }

    /// <summary>
    /// Convenience overload for callers that only have the beat/node ids on hand (e.g.
    /// <see cref="ProseWriterRouter"/>'s post-write hook) — resolves the beat number and node
    /// slug the base overload needs, then delegates. Added 2026-08-08: this check was already
    /// wired into the manual-edit save path (<see cref="NodeWorkbenchService"/>) but never into
    /// the CLI/MCP generation path that actually authors the vast majority of beats, so it never
    /// covered generated prose at all — only hand-edits made through the Blazor UI.
    /// </summary>
    public async Task CheckBeatIntentDriftAsync(
        Guid beatId, Guid nodeId, string beatText, string synopsis, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(synopsis) || string.IsNullOrWhiteSpace(beatText)) return;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beatNumber = await db.Beats.AsNoTracking().Where(b => b.Id == beatId).Select(b => b.Number).FirstOrDefaultAsync(ct);
        var nodeSlug = await db.Nodes.AsNoTracking().Where(n => n.Id == nodeId).Select(n => n.Slug).FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(nodeSlug)) return;
        await CheckBeatIntentDriftAsync(beatNumber, nodeSlug, beatText, synopsis, ct);
    }

    private void EmitFinding(
        string filePath, FindingSeverity severity, string summary, string? snippet, string? fix)
    {
        try
        {
            findings.Upsert(
                filePath:     filePath,
                chapterId:    null,
                category:     FindingCategory.SemanticDrift,
                severity:     severity,
                summary:      summary,
                snippet:      snippet,
                suggestedFix: fix);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "SemanticFidelity: failed to emit finding for {FilePath}", filePath);
        }
    }

    /// <summary>
    /// Whether <paramref name="intentAlign"/> is a genuine statistical anomaly within its own
    /// book's intent-alignment distribution (<paramref name="allIntentAligns"/>), rather than
    /// just being below the fixed <see cref="IntentAlignmentFloor"/>.
    ///
    /// Fixed 2026-08-10: the floor alone flagged 213/339 beats in BLST — confirmed 6/6 sampled
    /// findings (across all three severity tiers, 29%-50% similarity) as faithful, well-executed
    /// prose. A book written in a consistently terse, concrete register scores systematically
    /// low against its own abstract one-line synopses purely from vocabulary/register mismatch,
    /// independent of true fidelity — and one long multi-paragraph sample still only scored 50%,
    /// ruling out "just exempt short beats" as a fix. With no confirmed true positive in the
    /// sample to calibrate a replacement absolute number against, lowering the floor to a
    /// different constant would just repeat the mistake. Instead: flag only beats that are more
    /// than <see cref="IntentOutlierZScore"/> standard deviations below their OWN book's mean.
    ///
    ///   - Sample smaller than <see cref="IntentOutlierMinSample"/>: not enough points for a
    ///     meaningful distribution — fall back to the floor alone (true), matching prior behavior.
    ///   - Stddev ~0 (every beat scores near-identically, e.g. BLST): nothing is a real per-beat
    ///     anomaly — a uniformly low mean IS the book's register, not N independent defects
    ///     (false).
    ///   - Real spread exists: flag only beats genuinely far below this book's own mean, not an
    ///     absolute number blind to the book's style.
    /// </summary>
    internal static bool IsIntentOutlier(double intentAlign, IReadOnlyList<double> allIntentAligns)
    {
        if (allIntentAligns.Count < IntentOutlierMinSample) return true;

        var mean = allIntentAligns.Average();
        var variance = allIntentAligns.Select(x => (x - mean) * (x - mean)).Average();
        var stdDev = Math.Sqrt(variance);

        if (stdDev < 1e-9) return false;
        return intentAlign < mean - IntentOutlierZScore * stdDev;
    }

    private static string? FirstNonEmpty(params string?[] candidates)
    {
        foreach (var s in candidates)
            if (!string.IsNullOrWhiteSpace(s)) return s;
        return null;
    }
}
