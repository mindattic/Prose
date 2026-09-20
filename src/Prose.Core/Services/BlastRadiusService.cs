using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Operationalizes docs/LOGIC.md's "blast radius" concept — previously prose with zero code
/// behind it. Given a beat that was just edited (a fix pass, a manual --edit-beat), returns the
/// set of beats a narrow, cheap re-check should cover: the touched beat itself, its same-chapter
/// neighbors within a small window, and every OTHER beat (anywhere in the book) that shares an
/// entity presence with it — someone whose established state a careless edit could easily
/// contradict without the edit ever mentioning them by name.
///
/// This exists because a fix pass can introduce a regression against its own neighbors that the
/// NEXT full-book sweep — sometimes rounds and days later — is the first thing to catch (VIGL
/// hit this repeatedly this session: the Ocipheus mis-fix, the Ch18 "two places at once"
/// over-correction). Checking the blast radius immediately, in the same turn as the edit, closes
/// that gap instead of waiting on the next independent sweep round.
/// </summary>
public class BlastRadiusService(IDbContextFactory<ProseDbContext> dbFactory)
{
    /// <summary>Beats on either side of the edited beat, within the same chapter, that a reader
    /// would experience as immediately adjacent. 3 is a judgment call, not empirically derived —
    /// wide enough to catch an edit that contradicts "the paragraph before/after," narrow enough
    /// to stay cheap.</summary>
    private const int DefaultChapterWindow = 3;

    /// <summary>Returns the blast-radius beat-ID set for <paramref name="beatId"/>: itself, its
    /// same-chapter neighbors within <paramref name="chapterWindow"/> positions (by SortKey), and
    /// every beat elsewhere in the book sharing an entity presence with it. Empty if the beat
    /// isn't found on any BeatNodes row (orphaned/deleted beat).</summary>
    public async Task<List<Guid>> GetBlastRadiusBeatIdsAsync(
        Guid beatId, int chapterWindow = DefaultChapterWindow, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var home = await db.BeatNodes.AsNoTracking()
            .Where(bn => bn.BeatId == beatId)
            .Select(bn => new { bn.NodeId })
            .FirstOrDefaultAsync(ct);
        if (home == null) return [];

        // Same chapter, ordered by SortKey, windowed around the edited beat's own position.
        var sameChapter = await db.BeatNodes.AsNoTracking()
            .Where(bn => bn.NodeId == home.NodeId)
            .OrderBy(bn => bn.SortKey)
            .Select(bn => bn.BeatId)
            .ToListAsync(ct);
        var idx = sameChapter.IndexOf(beatId);
        var windowed = idx < 0
            ? Enumerable.Empty<Guid>()
            : sameChapter.Skip(Math.Max(0, idx - chapterWindow)).Take(chapterWindow * 2 + 1);

        // Beats anywhere in the book sharing an entity presence with the edited beat.
        // BeatEntityPresence has no EF mapping (project-wide convention — see BookHealthService's
        // PresenceRow/PovRow raw-SQL queries), so this is a small parameterized self-join.
        // Positional {0} (not a provider-specific SqlParameter) so this stays provider-agnostic —
        // SQL Server in production, SQLite under TestDbFactory.
        var shared = await db.Database.SqlQueryRaw<Guid>(
            "SELECT DISTINCT bep2.BeatId FROM BeatEntityPresence bep1 " +
            "JOIN BeatEntityPresence bep2 ON bep2.EntityId = bep1.EntityId AND bep2.BeatId <> bep1.BeatId " +
            "WHERE bep1.BeatId = {0}",
            beatId).ToListAsync(ct);

        var result = new HashSet<Guid>(windowed);
        foreach (var id in shared) result.Add(id);
        result.Add(beatId); // "the touched beats" per docs/LOGIC.md — the edit itself is always in scope
        return result.ToList();
    }
}
