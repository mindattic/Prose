using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

public record PostBeatValidationResult(
    int ProseViolations,
    int GearViolations)
{
    public int Total => ProseViolations + GearViolations;
}

/// <summary>
/// Auto-engages prose-quality and world-consistency checks after every beat save
/// and files violations as Findings. Two tiers:
///
///   FullValidateAsync  — gear carry. (The prose-pattern-guard tier, QuickValidateAsync, was
///     DELETED 2026-09-06 under RFC 0009: 46 [Cliche] findings on BCODA alone, a corpus apply
///     rate of zero, and the only thing it ever produced was pressure to restyle finished
///     prose. ProseViolations in the result is kept at 0 for report-shape compatibility.)
///     Called explicitly via the <c>validate_beat</c> MCP tool or
///     <c>prose --validate-beat</c> CLI when the writer wants a complete audit.
///
/// All methods swallow exceptions — quality checks are enhancers, not blockers.
/// </summary>
public class PostBeatValidationService(
    GearCarryEnforcer gearEnforcer,
    FindingsService findings,
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<PostBeatValidationService> log)
{
    /// <summary>
    /// Full battery: prose guard + gear carry.
    /// Resolves beat text and node slug from DB. When <paramref name="characterIds"/>
    /// is null, derives characters from the beat's indexed BeatEntityMentions.
    /// The behaviour-invariant tier was REMOVED 2026-09-06 (author ruling) — see the retirement
    /// note on BookHealthService's check list for why that check was unsound for fiction.
    /// </summary>
    public async Task<PostBeatValidationResult> FullValidateAsync(
        Guid beatId,
        IReadOnlyList<Guid>? characterIds = null,
        DateTime? storyTime = null,
        CancellationToken ct = default)
    {
        const int proseCount = 0; int gearCount = 0;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId, ct);
            if (string.IsNullOrWhiteSpace(beat?.Text)) return new(0, 0);

            var nodeSlug = await db.BeatNodes.AsNoTracking()
                .Where(sb => sb.BeatId == beatId && true)
                .Join(db.Nodes, sb => sb.NodeId, s => s.Id, (_, s) => s.Slug)
                .FirstOrDefaultAsync(ct) ?? beatId.ToString();

            var text = beat.Text;

            var chars = characterIds ?? await CharactersFromMentionsAsync(db, beatId, ct);
            foreach (var charId in chars)
            {
                ct.ThrowIfCancellationRequested();
                gearCount += await FileGearViolationsAsync(text, nodeSlug, charId, storyTime, beatId, ct);
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "FullValidate failed for beat {Id}", beatId);
        }
        return new(proseCount, gearCount);
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private async Task<int> FileGearViolationsAsync(
        string text, string nodeSlug, Guid charId, DateTime? storyTime, Guid asOfBeatId, CancellationToken ct)
    {
        try
        {
            var violations = await gearEnforcer.EnforceAsync(text, charId, storyTime, asOfBeatId, ct);
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
