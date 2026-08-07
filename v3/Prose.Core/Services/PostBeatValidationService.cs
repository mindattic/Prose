using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

public record PostBeatValidationResult(
    int ProseViolations,
    int GearViolations,
    int BehaviorViolations)
{
    public int Total => ProseViolations + GearViolations + BehaviorViolations;
}

/// <summary>
/// Auto-engages prose-quality and world-consistency checks after every beat save
/// and files violations as Findings. Two tiers:
///
///   QuickValidateAsync — prose pattern guard only (sync, no DB beyond findings write).
///     Called fire-and-forget by NodeWorkbenchService on every UpdateBeatTextAsync.
///
///   FullValidateAsync  — prose + gear carry + (opt) behavior invariants.
///     Called explicitly via the <c>validate_beat</c> MCP tool or
///     <c>ss --validate-beat</c> CLI when the writer wants a complete audit.
///
/// All methods swallow exceptions — quality checks are enhancers, not blockers.
/// </summary>
public class PostBeatValidationService(
    ProsePatternGuard proseGuard,
    GearCarryEnforcer gearEnforcer,
    BehavioralInvariantEnforcer behaviorEnforcer,
    FindingsService findings,
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<PostBeatValidationService> log)
{
    /// <summary>
    /// Prose guard only — no DB or LLM, safe to fire-and-forget after every beat save.
    /// <paramref name="nodeSlug"/> is used as the finding's filePath prefix.
    /// </summary>
    public Task QuickValidateAsync(string nodeSlug, string beatText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(beatText)) return Task.CompletedTask;
        try
        {
            var violations = proseGuard.Check(beatText);
            foreach (var v in violations)
            {
                findings.Upsert(
                    filePath:     $"node:{nodeSlug}",
                    chapterId:    null,
                    category:     FindingCategory.Cliche,
                    severity:     FindingSeverity.Medium,
                    summary:      $"[{v.Category}]: {v.Rule}",
                    snippet:      SnippetAround(beatText, v.CharOffset),
                    suggestedFix: v.Suggestion);
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "QuickValidate prose guard failed for node {Slug}", nodeSlug);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Full battery: prose guard + gear carry + optional behavior invariants.
    /// Resolves beat text and node slug from DB. When <paramref name="characterIds"/>
    /// is null, derives characters from the beat's indexed BeatEntityMentions.
    /// </summary>
    public async Task<PostBeatValidationResult> FullValidateAsync(
        Guid beatId,
        IReadOnlyList<Guid>? characterIds = null,
        bool checkBehavior = false,
        DateTime? storyTime = null,
        CancellationToken ct = default)
    {
        int proseCount = 0, gearCount = 0, behaviorCount = 0;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId, ct);
            if (string.IsNullOrWhiteSpace(beat?.Text)) return new(0, 0, 0);

            var nodeSlug = await db.BeatNodes.AsNoTracking()
                .Where(sb => sb.BeatId == beatId && sb.IsEnabled)
                .Join(db.Nodes, sb => sb.NodeId, s => s.Id, (_, s) => s.Slug)
                .FirstOrDefaultAsync(ct) ?? beatId.ToString();

            var text = beat.Text;
            proseCount = FileProseViolations(text, nodeSlug);

            var chars = characterIds ?? await CharactersFromMentionsAsync(db, beatId, ct);
            foreach (var charId in chars)
            {
                ct.ThrowIfCancellationRequested();
                gearCount += await FileGearViolationsAsync(text, nodeSlug, charId, storyTime, ct);
                if (checkBehavior)
                    behaviorCount += await FileBehaviorViolationsAsync(text, nodeSlug, charId, ct);
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "FullValidate failed for beat {Id}", beatId);
        }
        return new(proseCount, gearCount, behaviorCount);
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private int FileProseViolations(string text, string nodeSlug)
    {
        var violations = proseGuard.Check(text);
        foreach (var v in violations)
        {
            findings.Upsert(
                filePath:     $"node:{nodeSlug}",
                chapterId:    null,
                category:     FindingCategory.Cliche,
                severity:     FindingSeverity.Medium,
                summary:      $"[{v.Category}]: {v.Rule}",
                snippet:      SnippetAround(text, v.CharOffset),
                suggestedFix: v.Suggestion);
        }
        return violations.Count;
    }

    private async Task<int> FileGearViolationsAsync(
        string text, string nodeSlug, Guid charId, DateTime? storyTime, CancellationToken ct)
    {
        try
        {
            var violations = await gearEnforcer.EnforceAsync(text, charId, storyTime, ct);
            foreach (var v in violations)
            {
                findings.Upsert(
                    filePath:     $"node:{nodeSlug}",
                    chapterId:    null,
                    category:     FindingCategory.GearContradiction,
                    severity:     FindingSeverity.High,
                    summary:      $"GEAR-CARRY: {v.CharacterName} {v.VerbUsed} \"{v.GearName}\" — no carry edge",
                    snippet:      SnippetAround(text, v.CharOffset),
                    suggestedFix: $"Add carries/wields edge for \"{v.GearName}\" or remove the usage");
            }
            return violations.Count;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "GearCarryEnforcer failed for char {Id}", charId);
            return 0;
        }
    }

    private async Task<int> FileBehaviorViolationsAsync(
        string text, string nodeSlug, Guid charId, CancellationToken ct)
    {
        try
        {
            var violations = await behaviorEnforcer.EnforceAsync(text, charId, ct);
            foreach (var v in violations)
            {
                findings.Upsert(
                    filePath:     $"node:{nodeSlug}",
                    chapterId:    null,
                    category:     FindingCategory.BehaviorContradiction,
                    severity:     FindingSeverity.Medium,
                    summary:      $"BEHAVIOR [{v.RuleBucket}] {v.CharacterName}: {v.RuleText}",
                    snippet:      v.Explanation,
                    suggestedFix: null);
            }
            return violations.Count;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "BehavioralInvariantEnforcer failed for char {Id}", charId);
            return 0;
        }
    }

    private static async Task<List<Guid>> CharactersFromMentionsAsync(
        ProseDbContext db, Guid beatId, CancellationToken ct)
    {
        return await db.BeatEntityMentions
            .AsNoTracking()
            .Where(m => m.BeatId == beatId && m.EntityType == "character")
            .Select(m => m.EntityId)
            .ToListAsync(ct);
    }

    private static string SnippetAround(string text, int offset, int window = 80)
    {
        var start = Math.Max(0, offset - window / 2);
        var end   = Math.Min(text.Length, start + window);
        start = Math.Max(0, end - window);
        return text[start..end];
    }
}
