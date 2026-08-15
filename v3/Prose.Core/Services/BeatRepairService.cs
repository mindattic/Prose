using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using System.Text;

namespace Prose.Core.Services;

/// <summary>
/// Targeted beat re-write: given a beat and a list of <see cref="LensIssue"/> blockers,
/// rebuilds the beat's prose with MUST FIX constraints injected via
/// <see cref="BeatContext.RepairConstraintContext"/>.
///
/// Routes through <see cref="ProseWriterRouter"/> so repaired beats receive the full
/// 27-service enrichment pipeline (entity context, continuity, world state, etc.) —
/// the same grounding as original generation. The MUST FIX block is kept separate from
/// XRayContext so character profiles remain intact.
///
/// Called by <c>AutoRunCli</c>'s self-repair loop after a post-chapter audit flags specific beats.
/// </summary>
public class BeatRepairService(
    ProseWriterRouter router,
    NodeWorkbenchService workbench,
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<BeatRepairService> log)
{
    private const int MaxSceneSoFarChars = 6000;

    /// <summary>Below this size, a beat is a normal single-scene beat — a repair replacing it
    /// with a differently-sized rewrite is ordinary prose variance, not data loss.</summary>
    private const int MinGuardedBeatLength = 2000;

    /// <summary>A repair on a beat at/above <see cref="MinGuardedBeatLength"/> must keep at
    /// least this fraction of the original length.</summary>
    private const double MinSafeRepairRatio = 0.5;

    /// <summary>
    /// 2026-08-13 VIGL incident: <see cref="ProseWriterRouter"/> generates one normal-sized
    /// scene regardless of how large the beat being "repaired" actually is. On a book whose
    /// beats had been consolidated to chapter-scale (tens of thousands of chars), self-heal
    /// silently replaced three chapters with ~4,000-char fresh scenes — 26% of the novel lost
    /// in one automated pass, recovered only because a backup epub happened to exist. Refuse
    /// any repair that would shrink a substantial beat by more than half; the caller treats a
    /// null return as "repair failed," which correctly leaves the beat untouched and lets the
    /// underlying finding surface for a human instead of being silently destroyed.
    /// </summary>
    internal static bool IsUnsafeShrink(int currentLength, int repairedLength) =>
        currentLength >= MinGuardedBeatLength && repairedLength < currentLength * MinSafeRepairRatio;

    public async Task<string?> RepairAsync(
        Guid beatId, Guid nodeId,
        IReadOnlyList<LensIssue> blockers,
        string? bookBibleOverride = null,
        CancellationToken ct = default)
    {
        if (blockers.Count == 0) return null;

        string? currentText = null;
        string? beatGoal    = null;
        string? subtext     = null;
        string  bookBible   = "";
        Guid    bookNodeId  = nodeId; // updated below if nodeId is a chapter child (book-mode)

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var beat = await db.Beats.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == beatId, ct);
            if (beat == null)
            {
                log.LogWarning("[BeatRepairService] Beat {BeatId} not found", beatId);
                return null;
            }
            currentText = beat.Text;
            beatGoal    = beat.Description ?? beat.Title ?? "";
            subtext     = beat.Subtext;

            var node = await db.Nodes.AsNoTracking()
                .Where(n => n.Id == nodeId)
                .Select(n => new { n.Seed, n.NodeBible, n.ParentNodeId })
                .FirstOrDefaultAsync(ct);
            if (node?.ParentNodeId != null) bookNodeId = node.ParentNodeId.Value;
            if (bookBibleOverride != null)
                bookBible = bookBibleOverride;
            else if (node != null)
                bookBible = node.Seed ?? node.NodeBible ?? "";
        }
        catch (Exception ex)
        {
            // [SS-BeatRepair-001] Failed to load beat/node for repair — check DB connectivity.
            log.LogWarning(ex, "[BeatRepairService] DB load failed for beat {BeatId}", beatId);
            return null;
        }

        // Build SceneSoFar from prior beats and resolve beat index for positional enrichments.
        var sceneSoFar = "";
        var beatIndex  = 0;
        var totalBeats = 0;
        try
        {
            var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);
            totalBeats  = ordered.Count;
            var prior   = new StringBuilder();
            for (int i = 0; i < ordered.Count; i++)
            {
                var ob = ordered[i];
                if (ob.Beat.Id == beatId) { beatIndex = i; break; }
                if (!string.IsNullOrWhiteSpace(ob.Beat.Text))
                    prior.Append("\n\n").Append(ob.Beat.Text);
            }
            var full = prior.ToString();
            sceneSoFar = full.Length > MaxSceneSoFarChars ? full[^MaxSceneSoFarChars..] : full;
        }
        catch (Exception ex)
        {
            // [SS-BeatRepair-002] Failed to reconstruct SceneSoFar — repair will proceed without prior context.
            log.LogWarning(ex, "[BeatRepairService] SceneSoFar load failed for node {NodeId}", nodeId);
        }

        // Build MUST FIX constraint block (kept in RepairConstraintContext, NOT XRayContext,
        // so ProseWriterRouter can populate XRayContext with full character profiles independently).
        var mustFix = new StringBuilder();
        mustFix.AppendLine("MUST FIX — book-lens audit flagged the following defects in this beat:");
        foreach (var issue in blockers)
        {
            mustFix.AppendLine($"• [{issue.Kind}] {issue.Evidence}");
            if (!string.IsNullOrWhiteSpace(issue.Fix))
                mustFix.AppendLine($"  → {issue.Fix}");
        }
        if (!string.IsNullOrWhiteSpace(currentText))
        {
            mustFix.AppendLine();
            mustFix.AppendLine("CURRENT DRAFT (repair target — keep what works, fix what doesn't):");
            mustFix.AppendLine(currentText);
        }

        var ctx = new BeatContext
        {
            NodeId                  = bookNodeId,
            StoryBibleContext       = bookBible,
            SceneSoFar              = sceneSoFar,
            BeatGoal                = beatGoal ?? "",
            Subtext                 = subtext ?? "",
            RepairConstraintContext = mustFix.ToString().TrimEnd(),
        };

        try
        {
            // Route through ProseWriterRouter so the repaired beat gets the full 27-service
            // enrichment (entity context, continuity, world state, blueprints, etc.).
            var repaired = await router.WriteAsync(ctx, beatId, beatIndex, totalBeats, ct: ct);
            if (string.IsNullOrWhiteSpace(repaired)) return null;
            repaired = repaired.Trim();

            if (currentText != null && IsUnsafeShrink(currentText.Length, repaired.Length))
            {
                log.LogWarning(
                    "[BeatRepairService] Refusing repair for beat {BeatId}: would shrink {Before}→{After} chars ({Ratio:P0} of original) — treating as failed repair, beat left unchanged.",
                    beatId, currentText.Length, repaired.Length, (double)repaired.Length / currentText.Length);
                return null;
            }

            return repaired;
        }
        catch (Exception ex)
        {
            // [SS-BeatRepair-003] LLM generation failed during repair — beat left unchanged.
            log.LogWarning(ex, "[BeatRepairService] Generation failed for beat {BeatId}", beatId);
            return null;
        }
    }
}
