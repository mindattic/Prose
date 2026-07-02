using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Checks whether the prose produced so far is still tracking toward the
/// original beat spine, and recalibrates remaining beat goals when it has drifted.
///
/// Called at chapter boundaries by ChapterCloseProcessorService (or AutoRunCli directly).
/// Uses cheap Haiku-class calls — one check per chapter close.
/// </summary>
public class OutlineAdherenceService(
    IDbContextFactory<StreetSamuraiDbContext> dbFactory,
    ILlmService llm)
{
    /// <summary>
    /// Check adherence between what was written and what the remaining beat spine promises.
    /// Returns a drift score (0=completely off track, 100=perfectly on track) and a summary.
    /// </summary>
    public async Task<AdherenceResult> CheckAsync(
        Guid strandId,
        string chapterSummaryText,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(chapterSummaryText))
            return new AdherenceResult(100, "No chapter summary to evaluate — skipping drift check.");

        // Load the strand bible and remaining beat goals
        string? bibleText;
        List<string> remainingGoals;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var strand = await db.Strands.AsNoTracking()
                .Where(s => s.Id == strandId)
                .Select(s => new { s.StrandBible })
                .FirstOrDefaultAsync(ct);

            bibleText = strand?.StrandBible;

            remainingGoals = await db.StrandBeats.AsNoTracking()
                .Where(sb => sb.StrandId == strandId && sb.IsEnabled)
                .Join(db.Beats.AsNoTracking().Where(b => b.Text == null || b.Text == ""),
                      sb => sb.BeatId, b => b.Id, (sb, b) => b.Synopsis ?? b.BeatTitle ?? "")
                .Where(g => g.Length > 0)
                .ToListAsync(ct);
        }

        if (remainingGoals.Count == 0)
            return new AdherenceResult(100, "All beats written — nothing left to track.");

        if (string.IsNullOrWhiteSpace(bibleText))
            return new AdherenceResult(80, "No bible found — cannot evaluate arc alignment.");

        var remainingBlock = string.Join("\n", remainingGoals.Take(8).Select((g, i) => $"{i + 1}. {g}"));

        var raw = await llm.GenerateAsync(
            system: """
                You are a story editor checking outline adherence.
                Given a summary of what was written and the remaining planned beats,
                assess whether the story can still reach its planned destination.
                Output exactly:
                SCORE: <integer 0-100>  (100=fully on track, 0=completely off course)
                SUMMARY: <one sentence — what drifted or what's still aligned>
                """,
            user: $"CHAPTER SUMMARY:\n{chapterSummaryText}\n\nREMAINING PLANNED BEATS:\n{remainingBlock}",
            temperature: 0.2,
            maxTokens: 150,
            ct: ct);

        var scoreMatch   = System.Text.RegularExpressions.Regex.Match(raw, @"SCORE:\s*(\d+)");
        var summaryMatch = System.Text.RegularExpressions.Regex.Match(raw, @"SUMMARY:\s*(.+)");

        var score   = scoreMatch.Success && int.TryParse(scoreMatch.Groups[1].Value, out var s) ? Math.Clamp(s, 0, 100) : 70;
        var summary = summaryMatch.Success ? summaryMatch.Groups[1].Value.Trim() : raw.Trim();

        return new AdherenceResult(score, summary);
    }

    /// <summary>
    /// When drift score is below the threshold, rewrite the remaining beat goals to
    /// re-anchor the story toward the original bible's promised arc.
    /// Updates Beat.Synopsis in DB. Returns the number of beats recalibrated.
    /// </summary>
    public async Task<int> RecalibrateAsync(
        Guid strandId,
        string driftReason,
        string? bibleText,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(bibleText)) return 0;

        List<(Guid BeatId, string CurrentGoal)> emptyBeats;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var rows = await db.StrandBeats.AsNoTracking()
                .Where(sb => sb.StrandId == strandId && sb.IsEnabled)
                .Join(db.Beats.AsNoTracking().Where(b => b.Text == null || b.Text == ""),
                      sb => sb.BeatId, b => b.Id,
                      (sb, b) => new { b.Id, Goal = b.Synopsis ?? b.BeatTitle ?? "" })
                .Where(x => x.Goal != "")
                .OrderBy(x => x.Id)
                .ToListAsync(ct);

            emptyBeats = rows.Select(x => (x.Id, x.Goal)).ToList();
        }

        if (emptyBeats.Count == 0) return 0;

        var currentGoals = string.Join("\n", emptyBeats.Take(10).Select((b, i) => $"{i + 1}. {b.CurrentGoal}"));

        var raw = await llm.GenerateAsync(
            system: """
                You are a story editor recalibrating off-track planned beats.
                Given the original bible, the drift problem, and the current remaining beat goals,
                rewrite each beat goal to re-orient the story toward the bible's promised arc —
                preserving the character names and general structure but redirecting the events.
                Output one rewritten goal per line, numbered to match the input.
                Output ONLY the numbered list. No preamble.
                """,
            user: $"BIBLE EXCERPT:\n{bibleText[..Math.Min(2000, bibleText.Length)]}\n\nDRIFT REASON: {driftReason}\n\nCURRENT REMAINING BEAT GOALS:\n{currentGoals}",
            temperature: 0.5,
            maxTokens: 800,
            ct: ct);

        var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(l => System.Text.RegularExpressions.Regex.Replace(l, @"^\d+\.\s*", "").Trim())
            .Where(l => l.Length > 10)
            .Take(emptyBeats.Count)
            .ToList();

        int updated = 0;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            for (int i = 0; i < Math.Min(lines.Count, emptyBeats.Count); i++)
            {
                var beat = await db.Beats.FindAsync([emptyBeats[i].BeatId], ct);
                if (beat == null) continue;
                beat.Synopsis  = lines[i].Length > 500 ? lines[i][..500] : lines[i];
                beat.UpdatedAt = DateTime.UtcNow;
                updated++;
            }
            await db.SaveChangesAsync(ct);
        }

        return updated;
    }

    public record AdherenceResult(int Score, string Summary);
}
