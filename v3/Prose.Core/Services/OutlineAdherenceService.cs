using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Checks whether the prose produced so far is still tracking toward the
/// original beat spine, and recalibrates remaining beat goals when it has drifted.
///
/// Called at chapter boundaries by ChapterCloseProcessorService (or AutoRunCli directly).
/// Uses cheap Haiku-class calls — one check per chapter close.
/// </summary>
public class OutlineAdherenceService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILlmService llm)
{
    /// <summary>
    /// Check adherence between what was written and what the remaining beat spine promises.
    /// Returns a drift score (0=completely off track, 100=perfectly on track) and a summary.
    /// </summary>
    public async Task<AdherenceResult> CheckAsync(
        Guid nodeId,
        string chapterSummaryText,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(chapterSummaryText))
            return new AdherenceResult(100, "No chapter summary to evaluate — skipping drift check.");

        // Load the node bible and remaining beat goals
        string? bibleText;
        List<string> remainingGoals;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var node = await db.Nodes.AsNoTracking()
                .Where(s => s.Id == nodeId)
                .Select(s => new { s.NodeOutline })
                .FirstOrDefaultAsync(ct);

            bibleText = node?.NodeOutline;

            // SS-A43: beats live on chapter nodes (children), not directly on the book node.
            // Recurses past any nested Collection (2026-08-09 fix).
            var beatNodeIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);

            // beatNodeIds is already in correct global reading order (depth-first,
            // SortKey-per-level) — order by its list POSITION, not by re-fetching each beat's
            // chapter Node.SortKey, which is only comparable among siblings under the SAME
            // parent, not globally across branches (same bug class found the same day in
            // NodeDocService/SynopsisExportService/BeatLensServices/EmotionalDepthService).
            var goalRows = await (
                from sb in db.BeatNodes.AsNoTracking()
                where beatNodeIds.Contains(sb.NodeId) && true
                join b in db.Beats.AsNoTracking().Where(b => b.Text == null || b.Text == "") on sb.BeatId equals b.Id
                where (b.Description ?? b.Title ?? "").Length > 0
                select new { sb.NodeId, sb.SortKey, Goal = b.Description ?? b.Title ?? "" }
            ).ToListAsync(ct);
            remainingGoals = goalRows
                .OrderBy(g => beatNodeIds.IndexOf(g.NodeId)).ThenBy(g => g.SortKey)
                .Select(g => g.Goal).ToList();
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
            user: $"BOOK BIBLE:\n{bibleText![..Math.Min(1500, bibleText.Length)]}\n\nCHAPTER SUMMARY:\n{chapterSummaryText}\n\nREMAINING PLANNED BEATS:\n{remainingBlock}",
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
    /// re-anchor the book toward the original bible's promised arc.
    /// Updates Beat.Description in DB. Returns the number of beats recalibrated.
    /// </summary>
    public async Task<int> RecalibrateAsync(
        Guid nodeId,
        string driftReason,
        string? bibleText,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(bibleText)) return 0;

        List<(Guid BeatId, string CurrentGoal)> emptyBeats;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            // SS-A43: beats live on chapter nodes (children), not directly on the book node.
            // Recurses past any nested Collection (2026-08-09 fix) — the returned list is
            // already in proper reading order (depth-first, SortKey-ordered), so it doubles
            // as the chapter-position ordering used below.
            var beatNodeIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);

            var rows = await db.BeatNodes.AsNoTracking()
                .Where(sb => beatNodeIds.Contains(sb.NodeId) && true)
                .Join(db.Beats.AsNoTracking().Where(b => b.Text == null || b.Text == ""),
                      sb => sb.BeatId, b => b.Id,
                      (sb, b) => new { b.Id, sb.NodeId, BeatSortKey = sb.SortKey, Goal = b.Description ?? b.Title ?? "" })
                .Where(x => x.Goal != "")
                .ToListAsync(ct);

            // SS-A43: for book-mode nodes order by chapter position first, then beat SortKey within chapter.
            emptyBeats = rows
                .OrderBy(x => beatNodeIds.IndexOf(x.NodeId))
                .ThenBy(x => x.BeatSortKey)
                .Select(x => (x.Id, x.Goal))
                .ToList();
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
            var ids = emptyBeats.Take(lines.Count).Select(e => e.BeatId).ToHashSet();
            var beatMap = await db.Beats
                .Where(b => ids.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, ct);
            for (int i = 0; i < Math.Min(lines.Count, emptyBeats.Count); i++)
            {
                if (!beatMap.TryGetValue(emptyBeats[i].BeatId, out var beat)) continue;
                beat.Description  = lines[i].Length > 500 ? lines[i][..500] : lines[i];
                beat.UpdatedAt = DateTime.UtcNow;
                updated++;
            }
            await db.SaveChangesAsync(ct);
        }

        return updated;
    }

    public record AdherenceResult(int Score, string Summary);
}
