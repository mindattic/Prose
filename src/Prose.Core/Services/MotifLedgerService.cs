using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// The live Node-pipeline motif ledger (2026-08-28) — recurring images/objects/gestures per
/// book, persisted to <see cref="BookMotif"/>. Written by the MOTIFS slice of
/// BeatExtractionService's consolidated post-write call; read back by ProseWriterRouter as a
/// "MOTIFS IN PLAY" guidance block (only motifs sighted in 2+ beats — a single sighting is
/// just a detail, not yet a motif). No LLM calls in this class.
///
/// Deliberately NOT the same thing as <see cref="AuthoredMotifRegistry"/> (renamed from
/// MotifService 2026-09-01) — that class is a separate, still-live, manually/LLM-authored motif
/// registry (named/described/kind-tagged motifs, backing the plant_motif/get_motifs/
/// propose_motifs MCP tools), not a legacy predecessor of this one. This class only does
/// automatic per-beat occurrence counting for generation guidance; it has no equivalent for
/// AuthoredMotifRegistry's manual authoring surface, so do not delete or repoint that class
/// under the assumption this one replaces it.
/// </summary>
public class MotifLedgerService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<MotifLedgerService> log;

    /// <summary>A motif must be sighted in at least this many beats to reach guidance.</summary>
    public const int GuidanceOccurrenceFloor = 2;
    public const int GuidanceMaxMotifs = 6;

    public MotifLedgerService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<MotifLedgerService> log)
    {
        this.dbFactory = dbFactory;
        this.log = log;
    }

    /// <summary>Upsert motif sightings for one beat. Pure DB write.</summary>
    public async Task PersistCandidatesAsync(
        Guid nodeId, Guid beatId, IReadOnlyList<string> motifs, CancellationToken ct = default)
    {
        if (nodeId == Guid.Empty || motifs.Count == 0) return;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // Tracks keys already added-but-unsaved within THIS call — a plain DB query for
        // "does this key exist" would miss a same-key duplicate earlier in the same `motifs`
        // list (EF doesn't consult pending, unsaved adds), which used to insert two rows with
        // the same (NodeId, MotifKey) instead of collapsing into one incremented occurrence.
        var addedThisCall = new Dictionary<string, BookMotif>(StringComparer.Ordinal);
        foreach (var raw in motifs.Take(3))
        {
            var display = raw.Trim();
            if (display.Length < 4) continue;
            if (display.Length > 200) display = display[..200];
            var key = display.ToLowerInvariant();

            if (addedThisCall.TryGetValue(key, out var justAdded))
            {
                justAdded.Occurrences++;
                justAdded.LastBeatId = beatId == Guid.Empty ? null : beatId;
                continue;
            }

            var existing = await db.BookMotifs
                .FirstOrDefaultAsync(m => m.NodeId == nodeId && m.MotifKey == key, ct);
            if (existing == null)
            {
                existing = new BookMotif
                {
                    Id = Guid.CreateVersion7(),
                    NodeId = nodeId,
                    MotifKey = key,
                    Display = display,
                    Occurrences = 1,
                    FirstBeatId = beatId == Guid.Empty ? null : beatId,
                    LastBeatId = beatId == Guid.Empty ? null : beatId,
                };
                db.BookMotifs.Add(existing);
            }
            else
            {
                // Same beat re-extracted (regeneration) must not inflate the count. Only
                // meaningful when beatId is known — beatId is always non-empty here since
                // BeatExtractionService gates this call on beatId != Guid.Empty.
                if (beatId != Guid.Empty && existing.LastBeatId == beatId) continue;
                existing.Occurrences++;
                existing.LastBeatId = beatId == Guid.Empty ? null : beatId;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            addedThisCall[key] = existing;
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>The "MOTIFS IN PLAY" generation-guidance block. Empty until a motif recurs.</summary>
    public async Task<string> BuildGuidanceAsync(Guid nodeId, CancellationToken ct = default)
    {
        if (nodeId == Guid.Empty) return "";
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var motifs = await db.BookMotifs.AsNoTracking()
                .Where(m => m.NodeId == nodeId && m.Occurrences >= GuidanceOccurrenceFloor)
                .OrderByDescending(m => m.Occurrences)
                .Take(GuidanceMaxMotifs)
                .Select(m => new { m.Display, m.Occurrences })
                .ToListAsync(ct);
            if (motifs.Count == 0) return "";

            var lines = motifs.Select(m => $"• {m.Display} (seen in {m.Occurrences} beats)");
            return "MOTIFS IN PLAY — recurring images already established in this book. Deepen or "
                 + "refract them when the scene invites it; never reintroduce one as if new, and "
                 + "never explain what a motif means:\n" + string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "[MotifLedger] guidance skipped for node {NodeId}", nodeId);
            return "";
        }
    }
}
