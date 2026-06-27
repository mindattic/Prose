using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;
using System.Text;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Tracks what the reader knows at each point in a strand — distinct from what the POV
/// character knows. Manages dramatic irony: the reader may know things the character doesn't
/// (dramatic irony), or the character may act on knowledge the reader hasn't been told yet
/// (mystery withholding). Both are powerful prose tools; unintentional asymmetry is a bug.
///
/// Architecture:
///   - ExtractAsync: LLM call after each completed beat, fire-and-forget.
///     Extracts key revelations and stores them in the Findings table as
///     "READER-KNOWS: ..." entries keyed to the strand.
///   - BuildKnowledgeBlockAsync: called before each beat to inject the current
///     reader knowledge state as a prompt constraint.
///
/// Storage: Findings table with FilePath = "reader-knowledge:{strandId}", Category = Other.
/// No new migrations required — reuses existing infrastructure.
/// </summary>
public class ReaderKnowledgeService(
    ILlmService llm,
    IDbContextFactory<StreetSamuraiDbContext> dbFactory,
    ILogger<ReaderKnowledgeService> log)
{
    private const string Prefix = "READER-KNOWS";
    private const int MaxInjected = 6;
    private const int MaxExtractedPerBeat = 3;

    private static string FilePath(Guid strandId) => $"reader-knowledge:{strandId:N}";

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Extract reader revelations from a just-written beat and persist them.
    /// Fire-and-forget from ProseWriterRouter — never blocks prose output.
    /// </summary>
    public async Task ExtractAsync(string beatText, Guid strandId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(beatText) || strandId == Guid.Empty) return;

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
                ct: ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "ReaderKnowledgeService: LLM extraction failed for strand {StrandId}", strandId);
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

        if (facts.Count == 0) return;

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var fp = FilePath(strandId);
            foreach (var fact in facts)
            {
                var summary = $"{Prefix}: {fact}";
                var dedup = $"{fp}|Other|{summary}".ToLowerInvariant();
                if (dedup.Length > 450) dedup = dedup[..450];

                if (!await db.Findings.AnyAsync(f => f.DedupKey == dedup, ct))
                {
                    db.Findings.Add(new FindingRow
                    {
                        DetectedAt  = DateTime.UtcNow,
                        FilePath    = fp,
                        ChapterId   = null,
                        Category    = FindingCategory.Other.ToString(),
                        Severity    = FindingSeverity.Low.ToString(),
                        Summary     = summary,
                        Snippet     = null,
                        SuggestedFix = null,
                        Status      = FindingStatus.New.ToString(),
                        DedupKey    = dedup,
                    });
                }
            }
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "ReaderKnowledgeService: DB write failed for strand {StrandId}", strandId);
        }
    }

    /// <summary>
    /// Build the reader-knowledge prompt block for an upcoming beat.
    /// Returns empty string when no revelations have been recorded yet.
    /// </summary>
    public async Task<string> BuildKnowledgeBlockAsync(Guid strandId, CancellationToken ct = default)
    {
        if (strandId == Guid.Empty) return "";

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var fp = FilePath(strandId);
            var catKey = FindingCategory.Other.ToString();
            var statusKey = FindingStatus.New.ToString();

            var facts = await db.Findings.AsNoTracking()
                .Where(f => f.FilePath == fp && f.Category == catKey && f.Status == statusKey)
                .OrderByDescending(f => f.DetectedAt)
                .Take(MaxInjected)
                .Select(f => f.Summary)
                .ToListAsync(ct);

            if (facts.Count == 0) return "";

            var sb = new StringBuilder();
            sb.AppendLine("READER KNOWLEDGE STATE — what the reader now knows (as of the end of the previous beat):");
            sb.AppendLine("Write with awareness of this. Do not re-reveal facts the reader already has. Use asymmetries for dramatic irony.");
            foreach (var f in facts)
                sb.AppendLine($"• {f.Replace(Prefix + ": ", "")}");

            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "ReaderKnowledgeService: query failed for strand {StrandId}", strandId);
            return "";
        }
    }

    /// <summary>Mark all reader-knowledge findings for a strand as Dismissed (call on strand reset/restart).</summary>
    public async Task ClearAsync(Guid strandId, CancellationToken ct = default)
    {
        if (strandId == Guid.Empty) return;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var fp = FilePath(strandId);
            var rows = await db.Findings.Where(f => f.FilePath == fp).ToListAsync(ct);
            foreach (var r in rows) r.Status = FindingStatus.Dismissed.ToString();
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "ReaderKnowledgeService: clear failed for strand {StrandId}", strandId);
        }
    }
}
