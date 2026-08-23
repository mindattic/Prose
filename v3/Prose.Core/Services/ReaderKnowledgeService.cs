using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using System.Text;

namespace Prose.Core.Services;

/// <summary>
/// Tracks what the reader knows at each point in a node — distinct from what the POV
/// character knows. Manages dramatic irony: the reader may know things the character doesn't
/// (dramatic irony), or the character may act on knowledge the reader hasn't been told yet
/// (mystery withholding). Both are powerful prose tools; unintentional asymmetry is a bug.
///
/// Architecture:
///   - ExtractAsync: LLM call after each completed beat, fire-and-forget.
///     Extracts key revelations and stores them in <see cref="ReaderKnowledgeFact"/>.
///   - BuildKnowledgeBlockAsync: called before each beat to inject the current
///     reader knowledge state as a prompt constraint.
///
/// Storage: dedicated ReaderKnowledgeFacts table, keyed by NodeId. Used to live in the Findings
/// table (Category=ReaderKnows) — moved out 2026-08-13 because that borrowed the wrong lifecycle:
/// a fact here is meant to persist as long as the reader still holds it, which for Findings meant
/// "stays New forever," permanently inflating the human-triage inbox by 1,000+ rows nothing could
/// ever legitimately mark Applied/Dismissed.
/// </summary>
public class ReaderKnowledgeService(
    ILlmService llm,
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<ReaderKnowledgeService> log)
{
    // 2026-08-22 fix: a pure "most recent N" window silently dropped every OLDER reveal forever
    // once a book passed MaxInjected facts — on a long book this meant a foundational fact (a
    // character death, a hidden identity reveal) could scroll out of the window and the writer
    // would lose all guard-rail against re-revealing or contradicting it. Split the budget: a
    // recency window for near-term dramatic irony, plus a small foundational anchor of the
    // OLDEST facts (the ones most likely to be permanent reader knowledge) so nothing vanishes
    // outright — still bounded, not unbounded growth.
    private const int MaxRecentInjected = 8;
    private const int MaxFoundationalInjected = 4;
    private const int MaxExtractedPerBeat = 3;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Extract reader revelations from a just-written beat and persist them.
    /// Fire-and-forget from ProseWriterRouter — never blocks prose output.
    /// </summary>
    public async Task ExtractAsync(string beatText, Guid nodeId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(beatText) || nodeId == Guid.Empty) return;

        var prompt = $"""
            Read this beat of prose. Identify up to {MaxExtractedPerBeat} concrete facts the READER now knows
            that are narratively significant — character secrets revealed, plot mechanics exposed, relationship
            dynamics made explicit, world facts established for the first time.

            Exclude: atmosphere, setting description, action beats with no lasting informational weight.
            Include only: facts a reader would consciously register and remember.

            Respond with one fact per line, starting each with "FACT:". If there are no significant new
            reader revelations in this beat, respond with "NONE".

            BEAT TEXT:
            {beatText[..Math.Min(beatText.Length, 2000)]}
            """;

        string response;
        try
        {
            response = await llm.GenerateAsync(
                "You are a literary analyst extracting reader-knowledge events from fiction.",
                prompt,
                temperature: 0.1,
                maxTokens: 400,
                model: LlmModels.Haiku,
                ct: ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "ReaderKnowledgeService: LLM extraction failed for node {NodeId}", nodeId);
            return;
        }

        if (string.IsNullOrWhiteSpace(response) || response.Trim().Equals("NONE", StringComparison.OrdinalIgnoreCase))
            return;

        var facts = response.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.StartsWith("FACT:", StringComparison.OrdinalIgnoreCase))
            .Select(l => l["FACT:".Length..].Trim())
            .Where(f => f.Length > 10)
            .Take(MaxExtractedPerBeat)
            .ToList();

        await PersistFactsAsync(facts, nodeId, ct);
    }

    /// <summary>
    /// Persist already-extracted reader-knowledge facts (no LLM call here) — split out of
    /// <see cref="ExtractAsync"/> so <see cref="BeatExtractionService"/> can reuse it after a
    /// single consolidated extraction call instead of this class firing its own LLM call too.
    /// </summary>
    public async Task PersistFactsAsync(IReadOnlyList<string> facts, Guid nodeId, CancellationToken ct = default)
    {
        if (facts.Count == 0 || nodeId == Guid.Empty) return;

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            foreach (var fact in facts.Take(MaxExtractedPerBeat))
            {
                if (await db.ReaderKnowledgeFacts.AnyAsync(f => f.NodeId == nodeId && f.Fact == fact, ct))
                    continue;
                db.ReaderKnowledgeFacts.Add(new ReaderKnowledgeFact { NodeId = nodeId, Fact = fact });
                // Save each fact individually so a race on one row doesn't drop the rest of the batch.
                try { await db.SaveChangesAsync(ct); }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException)
                { db.ChangeTracker.Clear(); }
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "ReaderKnowledgeService: DB write failed for node {NodeId}", nodeId);
        }
    }

    /// <summary>
    /// Build the reader-knowledge prompt block for an upcoming beat.
    /// Returns empty string when no revelations have been recorded yet.
    /// </summary>
    public async Task<string> BuildKnowledgeBlockAsync(Guid nodeId, CancellationToken ct = default)
    {
        if (nodeId == Guid.Empty) return "";

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var recent = await db.ReaderKnowledgeFacts.AsNoTracking()
                .Where(f => f.NodeId == nodeId)
                .OrderByDescending(f => f.DetectedAt)
                .Take(MaxRecentInjected)
                .Select(f => new { f.Fact, f.DetectedAt })
                .ToListAsync(ct);

            if (recent.Count == 0) return "";

            var foundational = new List<string>();
            if (recent.Count == MaxRecentInjected) // otherwise fewer facts exist than the window holds — nothing older to anchor
            {
                var recentDetectedAts = recent.Select(f => f.DetectedAt).ToHashSet();
                foundational = await db.ReaderKnowledgeFacts.AsNoTracking()
                    .Where(f => f.NodeId == nodeId && !recentDetectedAts.Contains(f.DetectedAt))
                    .OrderBy(f => f.DetectedAt)
                    .Take(MaxFoundationalInjected)
                    .Select(f => f.Fact)
                    .ToListAsync(ct);
            }

            var sb = new StringBuilder();
            sb.AppendLine("READER KNOWLEDGE STATE — what the reader now knows (as of the end of the previous beat):");
            sb.AppendLine("Write with awareness of this. Do not re-reveal facts the reader already has. Use asymmetries for dramatic irony.");
            foreach (var f in recent)
                sb.AppendLine($"• {f.Fact}");
            if (foundational.Count > 0)
            {
                sb.AppendLine("EARLIER-ESTABLISHED (still true, still known to the reader — do not contradict):");
                foreach (var f in foundational)
                    sb.AppendLine($"• {f}");
            }

            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "ReaderKnowledgeService: query failed for node {NodeId}", nodeId);
            return "";
        }
    }

    /// <summary>Delete all reader-knowledge facts for a node (call on node reset/restart).</summary>
    public async Task ClearAsync(Guid nodeId, CancellationToken ct = default)
    {
        if (nodeId == Guid.Empty) return;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            await db.ReaderKnowledgeFacts.Where(f => f.NodeId == nodeId).ExecuteDeleteAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "ReaderKnowledgeService: clear failed for node {NodeId}", nodeId);
        }
    }

    /// <summary>
    /// One-time move of legacy reader-knowledge facts out of the Findings table (Category=
    /// ReaderKnows) into <see cref="ReaderKnowledgeFact"/>, then deletes the old rows. Idempotent
    /// — a no-op once the legacy rows are gone. Call from --repair's schema-bootstrap.
    /// </summary>
    public async Task MigrateLegacyFindingsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        const string legacyCategory = "ReaderKnows";
        const string filePrefix = "reader-knowledge:";
        const string summaryPrefix = "READER-KNOWS: ";

        var legacy = await db.Findings.Where(f => f.Category == legacyCategory).ToListAsync(ct);
        if (legacy.Count == 0) return;

        var moved = 0;
        foreach (var row in legacy)
        {
            if (!row.FilePath.StartsWith(filePrefix, StringComparison.Ordinal)) continue;
            if (!Guid.TryParseExact(row.FilePath[filePrefix.Length..], "N", out var nodeId)) continue;
            var fact = row.Summary.StartsWith(summaryPrefix, StringComparison.Ordinal)
                ? row.Summary[summaryPrefix.Length..]
                : row.Summary;
            db.ReaderKnowledgeFacts.Add(new ReaderKnowledgeFact { NodeId = nodeId, Fact = fact, DetectedAt = row.DetectedAt });
            moved++;
        }
        db.Findings.RemoveRange(legacy);
        await db.SaveChangesAsync(ct);
        log.LogInformation("ReaderKnowledgeService: migrated {Moved} legacy Findings rows to ReaderKnowledgeFacts ({Deleted} removed)",
            moved, legacy.Count);
    }
}
