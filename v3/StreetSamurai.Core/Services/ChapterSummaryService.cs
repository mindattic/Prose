using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// DB-backed chapter summaries for long-form coherence. Persists a
/// 3-4 sentence factual summary per chapter so later beats can reference
/// what happened earlier — surviving process restarts.
///
/// This supplements (not replaces) the in-memory NarrativeSummaryService,
/// which handles within-session scene-to-scene memory. ChapterSummaryService
/// handles cross-session, cross-chapter continuity.
/// </summary>
public class ChapterSummaryService(
    IDbContextFactory<StreetSamuraiDbContext> dbFactory,
    ILlmService llm)
{
    /// <summary>
    /// Summarize the completed chapter prose and persist it.
    /// Upserts by (strandId, chapterIndex) — safe to call on re-run.
    /// </summary>
    public async Task ExtractAndSaveAsync(
        Guid strandId,
        int chapterIndex,
        string chapterProse,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(chapterProse)) return;

        var summaryText = await SummarizeAsync(chapterProse, ct);
        if (string.IsNullOrWhiteSpace(summaryText)) return;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existing = await db.StrandChapterSummaries
            .FirstOrDefaultAsync(s => s.StrandId == strandId && s.ChapterIndex == chapterIndex, ct);

        if (existing != null)
        {
            existing.SummaryText = summaryText;
            existing.UpdatedAt   = DateTime.UtcNow;
        }
        else
        {
            db.StrandChapterSummaries.Add(new StrandChapterSummary
            {
                Id           = Guid.CreateVersion7(),
                StrandId     = strandId,
                ChapterIndex = chapterIndex,
                SummaryText  = summaryText,
                CreatedAt    = DateTime.UtcNow,
                UpdatedAt    = DateTime.UtcNow,
            });
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Build a formatted prior-chapter context block for injection into BeatContext.
    /// Returns all persisted chapter summaries for this strand — they are always from
    /// prior chapters since the current chapter is not persisted until close.
    /// Returns empty string when no summaries exist yet.
    /// </summary>
    public async Task<string> BuildPriorSummaryContextAsync(
        Guid strandId,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var summaries = await db.StrandChapterSummaries
            .AsNoTracking()
            .Where(s => s.StrandId == strandId)
            .OrderBy(s => s.ChapterIndex)
            .ToListAsync(ct);

        if (summaries.Count == 0) return "";

        var lines = summaries.Select((s, i) => $"Chapter {s.ChapterIndex + 1}: {s.SummaryText}");
        return "PRIOR CHAPTER SUMMARIES (what happened before this chapter — treat as hard continuity constraints):\n"
             + string.Join("\n", lines);
    }

    private async Task<string> SummarizeAsync(string prose, CancellationToken ct)
    {
        var system = """
            You are a story editor. Compress the following chapter into exactly 3-4 sentences.
            Capture: what happened, to whom, what changed emotionally/physically, and what tension remains unresolved.
            Be specific — names, consequences, wounds, discoveries. Do NOT editorialize. Just the facts.
            """;
        return (await llm.GenerateAsync(system, prose, temperature: 0.3, maxTokens: 300, ct: ct)).Trim();
    }
}
