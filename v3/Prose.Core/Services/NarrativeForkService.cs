using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using System.Text.RegularExpressions;

namespace Prose.Core.Services;

/// <summary>
/// At chapter boundaries, generates N competing alternative arcs for the next
/// chapter, scores each with a quick LLM call, and applies the winning arc's
/// beat goals to the DB — letting Legion pick the best narrative direction rather
/// than always committing to the original outline.
///
/// Integrates into ChapterCloseProcessorService (opt-in via ForkCount > 1).
/// Also callable directly for flat nodes via PickNextBeatsArcAsync.
/// </summary>
public class NarrativeForkService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILlmService llm)
{
    /// <summary>
    /// Pick the best arc for the next chapter in a book-mode node.
    /// Loads the chapter at index <paramref name="completedChapterIndex"/> + 1,
    /// generates <paramref name="forkCount"/> alternatives, scores them, and
    /// rewrites that chapter's remaining beat goals to match the winner.
    /// Returns an empty ForkResult when there is no next chapter to act on.
    /// </summary>
    public async Task<ForkResult> PickNextChapterArcAsync(
        Guid parentNodeId,
        int completedChapterIndex,
        string writtenSoFar,
        int forkCount = 3,
        CancellationToken ct = default)
    {
        forkCount = Math.Clamp(forkCount, 2, 5);

        string? bibleText;
        Guid? nextChapterId;
        List<(Guid BeatId, string CurrentGoal)> nextBeats;

        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var node = await db.Nodes.AsNoTracking()
                .Where(s => s.Id == parentNodeId)
                .Select(s => new { s.NodeOutline })
                .FirstOrDefaultAsync(ct);
            bibleText = node?.NodeOutline;

            // Descend to LEAF nodes, not just direct children — a split-collection book
            // (parentNodeId -> "Chapter N" container with 0 direct beats -> real chapters ->
            // beats, e.g. BLST/ICFI/RTR/VIGL) has its real chapters two levels down.
            // Direct-children-only would see just the one container, so completedChapterIndex
            // could never advance past 0 and every later chapter would resolve nextChapterId
            // to null — silently disabling forking for the rest of the book. Preserve
            // GetLeafDescendantIdsAsync's own return order rather than re-sorting by
            // Node.SortKey, which is only comparable within one parent's sibling group.
            var chapterIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, parentNodeId, ct);

            nextChapterId = chapterIds.Count > completedChapterIndex + 1
                ? chapterIds[completedChapterIndex + 1]
                : (Guid?)null;

            if (nextChapterId.HasValue)
            {
                // 2026-08-09 bug fix: used to query beats attached directly to nextChapterId
                // only. If that chapter is itself a split Collection (chapter -> N
                // sub-chapters -> beats, e.g. a book split via --split-collection), it has
                // ZERO direct beats — nextBeats.Count would silently come out 0 and this
                // whole fork feature would return Empty for that chapter, even though it
                // plainly has unwritten beats one level deeper. GetLeafDescendantIdsAsync
                // resolves nextChapterId itself when unsplit (single-element list) or its
                // sub-chapters when split, in both cases replacing the exact-match filter.
                var nextLeafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nextChapterId.Value, ct);
                // Query per leaf, in leaf order (already correct depth-first SortKey order from
                // GetLeafDescendantIdsAsync), then concatenate — a single OrderBy(SortKey) across
                // multiple leaves would be wrong the moment there's more than one, since SortKey
                // restarts at 100 within each chapter (same fix applied to DcmVizCli earlier).
                nextBeats = [];
                foreach (var leafId in nextLeafIds)
                {
                    var rows = await db.BeatNodes.AsNoTracking()
                        .Where(sb => sb.NodeId == leafId && true)
                        .Join(db.Beats.AsNoTracking().Where(b => b.Text == null || b.Text == ""),
                              sb => sb.BeatId, b => b.Id,
                              (sb, b) => new { b.Id, SortKey = sb.SortKey, Goal = b.Description ?? b.Title ?? "" })
                        .Where(x => x.Goal != "")
                        .OrderBy(x => x.SortKey)
                        .ToListAsync(ct);
                    nextBeats.AddRange(rows.Select(x => (x.Id, x.Goal)));
                }
            }
            else
            {
                nextBeats = [];
            }
        }

        if (nextChapterId == null || nextBeats.Count == 0)
            return ForkResult.Empty;

        return await RunForkAsync(nextChapterId.Value, writtenSoFar, bibleText, nextBeats, forkCount, ct);
    }

    /// <summary>
    /// Pick the best arc for the next N unwritten beats in a flat node.
    /// </summary>
    public async Task<ForkResult> PickNextBeatsArcAsync(
        Guid nodeId,
        string writtenSoFar,
        int forkCount = 3,
        int nextBeatWindow = 6,
        CancellationToken ct = default)
    {
        forkCount = Math.Clamp(forkCount, 2, 5);

        string? bibleText;
        List<(Guid BeatId, string CurrentGoal)> nextBeats;

        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var node = await db.Nodes.AsNoTracking()
                .Where(s => s.Id == nodeId)
                .Select(s => new { s.NodeOutline })
                .FirstOrDefaultAsync(ct);
            bibleText = node?.NodeOutline;

            // Recurses past any nested Collection (2026-08-09 fix).
            var forkSearchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);

            var rows = await db.BeatNodes.AsNoTracking()
                .Where(sb => forkSearchIds.Contains(sb.NodeId) && true)
                .Join(db.Beats.AsNoTracking().Where(b => b.Text == null || b.Text == ""),
                      sb => sb.BeatId, b => b.Id,
                      (sb, b) => new { b.Id, SortKey = sb.SortKey, Goal = b.Description ?? b.Title ?? "" })
                .Where(x => x.Goal != "")
                .OrderBy(x => x.SortKey)
                .Take(nextBeatWindow)
                .ToListAsync(ct);
            nextBeats = rows.Select(x => (x.Id, x.Goal)).ToList();
        }

        if (nextBeats.Count == 0)
            return ForkResult.Empty;

        return await RunForkAsync(nodeId, writtenSoFar, bibleText, nextBeats, forkCount, ct);
    }

    private async Task<ForkResult> RunForkAsync(
        Guid chapterId,
        string writtenSoFar,
        string? bibleText,
        List<(Guid BeatId, string CurrentGoal)> beats,
        int forkCount,
        CancellationToken ct)
    {
        var currentGoals = string.Join("\n", beats.Take(8).Select((b, i) => $"{i + 1}. {b.CurrentGoal}"));
        var bibleExcerpt = bibleText?.Length > 1500 ? bibleText[..1500] : bibleText ?? "";
        var contextSnippet = writtenSoFar.Length > 3000 ? writtenSoFar[^3000..] : writtenSoFar;

        Console.WriteLine($"[fork] Generating {forkCount} alternative arcs…");

        var arcTasks = Enumerable.Range(0, forkCount)
            .Select(i => GenerateArcAsync(i, contextSnippet, bibleExcerpt, currentGoals, beats.Count, ct))
            .ToList();
        var arcs = await Task.WhenAll(arcTasks);

        var scoreTasks = arcs.Select((arc, i) => ScoreArcAsync(i, arc, contextSnippet, ct)).ToList();
        var scores = await Task.WhenAll(scoreTasks);

        var winner = scores.OrderByDescending(s => s.Score).First();
        Console.WriteLine($"[fork] Winner: arc {winner.Index + 1} (score {winner.Score}/100)");
        for (int i = 0; i < scores.Length; i++)
            Console.WriteLine($"[fork]   {i + 1}. score={scores[i].Score,3}  {scores[i].Reason}");

        var updated = await ApplyWinningArcAsync(chapterId, arcs[winner.Index], beats, ct);

        var allScores = scores.Select(s => $"{s.Index + 1}:{s.Score}/100 — {s.Reason}").ToArray();
        return new ForkResult(winner.Index + 1, winner.Score, arcs[winner.Index], updated, allScores);
    }

    private async Task<string> GenerateArcAsync(
        int index,
        string contextSnippet,
        string bibleExcerpt,
        string currentGoals,
        int beatCount,
        CancellationToken ct)
    {
        var raw = await llm.GenerateAsync(
            system: $"""
                You are a story architect proposing a next-chapter arc.
                Given the story so far and the original bible, propose a compelling alternative arc
                for the next chapter that creates fresh dramatic tension and advances character transformation.
                Variant index {index + 1}: explore a genuinely different narrative direction — not a rephrasing of the current goals.

                Output exactly:
                ARC: <one sentence — the chapter's dramatic purpose>
                BEATS:
                1. <beat goal>
                2. <beat goal>
                ...up to {beatCount} beats.
                """,
            user: $"BIBLE EXCERPT:\n{bibleExcerpt}\n\nSTORY SO FAR:\n{contextSnippet}\n\nCURRENT PLANNED BEATS (to improve or replace):\n{currentGoals}",
            temperature: 0.82,
            maxTokens: 600,
            ct: ct);
        return raw;
    }

    private async Task<(int Index, int Score, string Reason)> ScoreArcAsync(
        int index,
        string arcText,
        string contextSoFar,
        CancellationToken ct)
    {
        var raw = await llm.GenerateAsync(
            system: """
                You are a story editor scoring a proposed next-chapter arc.
                Score on three axes (each 0-33 points):
                  1. Tension escalation — does this arc meaningfully raise stakes vs. what came before?
                  2. Character transformation — does this arc create genuine change or pressure on the protagonist?
                  3. Payoff potential — does this arc set up or deliver on promises already made?

                Output exactly:
                SCORE: <integer 0-100>
                REASON: <one sentence — main strength or weakness>
                """,
            user: $"STORY SO FAR:\n{contextSoFar[..Math.Min(1500, contextSoFar.Length)]}\n\nPROPOSED ARC:\n{arcText}",
            temperature: 0.2,
            maxTokens: 100,
            ct: ct);

        var scoreMatch = Regex.Match(raw, @"SCORE:\s*(\d+)");
        var reasonMatch = Regex.Match(raw, @"REASON:\s*(.+)");

        var score = scoreMatch.Success && int.TryParse(scoreMatch.Groups[1].Value, out var s)
            ? Math.Clamp(s, 0, 100) : 50;
        var reason = reasonMatch.Success ? reasonMatch.Groups[1].Value.Trim() : "no reason given";

        return (index, score, reason);
    }

    private async Task<int> ApplyWinningArcAsync(
        Guid chapterId,
        string arcText,
        List<(Guid BeatId, string CurrentGoal)> beats,
        CancellationToken ct)
    {
        var lines = arcText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SkipWhile(l => !l.StartsWith("BEATS:", StringComparison.OrdinalIgnoreCase))
            .Skip(1)
            .Select(l => Regex.Replace(l, @"^\d+\.\s*", "").Trim())
            .Where(l => l.Length > 5)
            .Take(beats.Count)
            .ToList();

        if (lines.Count == 0) return 0;

        int updated = 0;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beatIdSet = beats.Select(b => b.BeatId).ToHashSet();
        var beatMap = await db.Beats
            .Where(b => beatIdSet.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, ct);
        for (int i = 0; i < Math.Min(lines.Count, beats.Count); i++)
        {
            if (!beatMap.TryGetValue(beats[i].BeatId, out var beat)) continue;
            beat.Description  = lines[i].Length > 500 ? lines[i][..500] : lines[i];
            // Clear, never stamp: these are the winning ARC's proposed beat goals — forward
            // intent for prose not yet rewritten, not a reading of what is on the page
            // (Beat.DescriptionHash).
            beat.DescriptionHash = null;
            beat.UpdatedAt = DateTime.UtcNow;
            updated++;
        }
        await db.SaveChangesAsync(ct);
        return updated;
    }

    public record ForkResult(
        int WinnerIndex,
        int WinnerScore,
        string WinningArc,
        int BeatsUpdated,
        string[] AllScores)
    {
        public static ForkResult Empty => new(-1, 0, "", 0, []);
        public bool HasResult => WinnerIndex >= 0;
    }
}
