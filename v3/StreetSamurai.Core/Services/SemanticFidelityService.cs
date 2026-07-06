using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

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
/// The service is called automatically in the background after every review run
/// (via NodeReviewService) whenever the node score meets the gaming threshold.
/// It can also be invoked directly via `ss --check-fidelity` or the
/// `check_semantic_fidelity` MCP tool.
/// </summary>
public class SemanticFidelityService
{
    /// <summary>Minimum beat score before fidelity checks engage. Below this threshold
    /// there is no meaningful signal — the beat is not scoring well enough to suspect gaming.</summary>
    public const double ScoreGamingThreshold = 70.0;

    /// <summary>Cosine similarity floor for bible alignment. Below this the beat has
    /// drifted significantly from the story's Seed/Synopsis anchor.</summary>
    public const double BibleAlignmentFloor = 0.42;

    /// <summary>Cosine similarity floor for intent alignment. Below this the beat's
    /// prose no longer serves its stated Synopsis/purpose.</summary>
    public const double IntentAlignmentFloor = 0.50;

    private readonly EmbeddingService embeddings;
    private readonly FindingsService findings;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<SemanticFidelityService> log;

    public SemanticFidelityService(
        EmbeddingService embeddings,
        FindingsService findings,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
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
        double Score,
        double BibleAlignment,
        double? IntentAlignment,
        string Kind,
        string Message,
        string? SuggestedFix);

    public record FidelityReport(
        Guid NodeId,
        string Slug,
        int BeatsChecked,
        int BeatsScored,
        double? NodeScore,
        double MeanBibleAlignment,
        double? MeanIntentAlignment,
        IReadOnlyList<FidelityViolation> Violations,
        int FindingsEmitted);

    /// <summary>
    /// Audit a node's prose for the Semantic Fidelity Gap. Ensures beat
    /// embeddings are fresh (drift-skipped), ranks every beat against the
    /// story's bible anchor, computes intent alignment for scored beats that
    /// have a Synopsis, then files SEMANTIC-DRIFT findings for violators.
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

        var beats = await (
            from sb in db.BeatNodes.AsNoTracking()
            join b  in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
            where sb.NodeId == nodeId && sb.IsEnabled
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
        if (hasBibleAnchor)
        {
            try
            {
                var hits = await embeddings.FindSimilarBeatNodesAsync(
                    bibleAnchor!, k: 500, nodeScope: nodeId, ct: ct);
                foreach (var hit in hits)
                    bibleAlignmentById[hit.ScopeId] = hit.Similarity;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "SemanticFidelity: bible-alignment query failed for '{Slug}'", node.Slug);
            }
        }

        // ── Intent alignment ──────────────────────────────────────────────
        // For beats that score above the gaming threshold AND have a Synopsis, embed
        // synopsis+prose as a pair and compute their cosine similarity. One batch call
        // per ~64 beats keeps API cost near zero for typical node lengths.
        var intentAlignmentById = new Dictionary<Guid, double>();
        var intentCandidates = beats
            .Where(b => (b.Score ?? 0) >= ScoreGamingThreshold
                     && !string.IsNullOrWhiteSpace(b.Description)
                     && !string.IsNullOrWhiteSpace(b.Text))
            .ToList();

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
                log.LogWarning(ex, "SemanticFidelity: intent-alignment batch failed for '{Slug}'", node.Slug);
            }
        }

        // ── Identify violations ───────────────────────────────────────────
        var violations = new List<FidelityViolation>();
        var scoredBeats = beats.Where(b => (b.Score ?? 0) >= ScoreGamingThreshold).ToList();

        foreach (var b in scoredBeats)
        {
            var score       = b.Score!.Value;
            var bibleAlign  = bibleAlignmentById.TryGetValue(b.Id, out var ba)  ? ba  : (double?)null;
            var intentAlign = intentAlignmentById.TryGetValue(b.Id, out var ia) ? ia  : (double?)null;

            // Bible drift: high score + low alignment to the story's north star.
            if (hasBibleAnchor && bibleAlign.HasValue && bibleAlign.Value < BibleAlignmentFloor)
            {
                var sev = bibleAlign.Value < 0.30 ? FindingSeverity.High
                        : bibleAlign.Value < 0.37 ? FindingSeverity.Medium
                        : FindingSeverity.Low;
                var msg  = $"Beat #{b.Number} scores {score:0.#}% but aligns only {bibleAlign.Value:P0} with the story bible " +
                           $"(floor {BibleAlignmentFloor:P0}). High score / low meaning alignment — Goodhart's Law in prose.";
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

            // Intent drift: high score + prose no longer serves the beat's stated purpose.
            if (intentAlign.HasValue && intentAlign.Value < IntentAlignmentFloor)
            {
                var sev = intentAlign.Value < 0.35 ? FindingSeverity.High
                        : intentAlign.Value < 0.43 ? FindingSeverity.Medium
                        : FindingSeverity.Low;
                var synopsis = b.Description!.Length > 120 ? b.Description[..120] + "…" : b.Description;
                var msg  = $"Beat #{b.Number} scores {score:0.#}% but its prose aligns only {intentAlign.Value:P0} with its stated intent (\"{synopsis}\"). " +
                           $"The rewrite served the score rubric, not the beat's purpose.";
                var fix  = $"Beat #{b.Number} was supposed to: \"{b.Description}\". " +
                           $"Revise to fulfil that purpose rather than chasing stylistic reviewer rewards.";
                violations.Add(new FidelityViolation(
                    b.Id, b.Number, b.Title, score,
                    bibleAlign ?? 0, intentAlign, "intent", msg, fix));
                EmitFinding($"node:{node.Slug}", sev,
                    $"SEMANTIC-DRIFT [intent]: {msg}",
                    b.Text?.Length > 200 ? b.Text[..200] : b.Text, fix);
            }
        }

        // ── Aggregates ────────────────────────────────────────────────────
        var allBibleAligns  = bibleAlignmentById.Values.ToList();
        var allIntentAligns = intentAlignmentById.Values.ToList();
        var meanBible  = allBibleAligns.Count  > 0 ? allBibleAligns.Average()  : 0.0;
        double? meanIntent = allIntentAligns.Count > 0 ? allIntentAligns.Average() : null;

        log.LogInformation(
            "SemanticFidelity '{Slug}': {Total} beats, {Scored} scored, {V} violations, " +
            "mean bible={Bible:P0}{Intent}",
            node.Slug, beats.Count, scoredBeats.Count, violations.Count, meanBible,
            meanIntent.HasValue ? $", mean intent={meanIntent:P0}" : "");

        return new FidelityReport(
            NodeId:           nodeId,
            Slug:               node.Slug,
            BeatsChecked:       beats.Count,
            BeatsScored:        scoredBeats.Count,
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
            var similarity = await embeddings.ComputeSimilarityAsync(synopsis, beatText, ct);
            if (similarity >= IntentAlignmentFloor) return;

            var sev = similarity < 0.35 ? FindingSeverity.High
                    : similarity < 0.43 ? FindingSeverity.Medium
                    : FindingSeverity.Low;
            var snip  = synopsis.Length > 120 ? synopsis[..120] + "…" : synopsis;
            var msg   = $"Beat #{beatNumber} prose aligns only {similarity:P0} with its stated intent (\"{snip}\"). " +
                        "Prose may have drifted from its purpose on save.";
            var fix   = $"Beat #{beatNumber} was supposed to: \"{synopsis}\". " +
                        "Revise to fulfil that purpose.";
            EmitFinding($"node:{nodeSlug}", sev,
                $"SEMANTIC-DRIFT [intent]: {msg}",
                beatText.Length > 200 ? beatText[..200] : beatText, fix);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "CheckBeatIntentDrift failed for Beat #{Number} node {Slug}",
                beatNumber, nodeSlug);
        }
    }

    private void EmitFinding(
        string filePath, FindingSeverity severity, string summary, string? snippet, string? fix)
    {
        try
        {
            findings.Upsert(
                filePath:     filePath,
                chapterId:    null,
                category:     FindingCategory.Other,
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

    private static string? FirstNonEmpty(params string?[] candidates)
    {
        foreach (var s in candidates)
            if (!string.IsNullOrWhiteSpace(s)) return s;
        return null;
    }
}
