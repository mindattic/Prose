using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// RFC 0011, Brick 1 — the Verification Context Provider.
///
/// <para>Generation solved "what do I already know about this beat that should shape the
/// output" months ago: SS-A46's DCM four-layer hierarchy (Base/Universe/BookOutline/Register),
/// with <see cref="DocContextService"/> pinning a beat's POV character's register dominant.
/// Verification never got the same treatment — every check service that needed "who's narrating
/// this beat, and what's their established voice" re-implemented the same
/// <c>BeatEntityPresence</c> lookup independently. Found 2026-08-10 as the shared root cause of
/// three separate incidents in one session (SemanticFidelityService and BeatVerificationService's
/// DeclaredPurpose check both scoring register-blind; BeatChecklistGateService flagging a
/// character's own on-file voice as an AI tic). This service is the fix: one place for "who is
/// narrating, what's their voice" so future checks get it for free instead of re-deriving it.</para>
///
/// <para>This starts narrow — POV resolution only, extracted from the two places it was
/// duplicated (<see cref="ProseWriterRouter"/>'s inline raw-SQL lookup and
/// <c>BeatChecklistGateService.GetPovVoiceHintAsync</c>). The per-book statistical-outlier
/// baseline (<see cref="SemanticFidelityService.IsIntentOutlier"/>) is a second instance of
/// "context a check needs" that is already correctly shared between two services via a plain
/// static method — deliberately left where it is rather than moved here for its own sake; this
/// service exists to end duplication, not to centralize things that already aren't duplicated.</para>
/// </summary>
public class VerificationContextService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<VerificationContextService> log)
{
    /// <summary>This beat's POV entity id, from the bible's POV map
    /// (<c>BeatEntityPresence.PresenceType = 'pov'</c>) — the same row
    /// <see cref="DocContextService.PrepareForNodeAsync"/> pins dominant per SS-A46 layer 4.
    /// Null if the beat has no recorded POV.</summary>
    public async Task<Guid?> GetPovEntityIdAsync(Guid beatId, CancellationToken ct = default)
    {
        if (beatId == Guid.Empty) return null;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var ids = await db.Database
                .SqlQuery<Guid>($"SELECT TOP 1 EntityId FROM BeatEntityPresence WHERE BeatId = {beatId} AND PresenceType = 'pov'")
                .ToListAsync(ct);
            return ids.Count > 0 ? ids[0] : null;
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "[VerificationContextService] POV lookup skipped for beat {BeatId}", beatId);
            return null;
        }
    }

    /// <summary>
    /// This beat's POV character's on-file voice, as a short "Role — SpeechVocabulary" hint —
    /// the exact string a checklist-style evaluator needs to distinguish an authentic,
    /// established character voice from a generic AI tic. Null if there's no recorded POV or
    /// that character has no on-file vocabulary. <paramref name="cache"/> is caller-owned and
    /// keyed on POV entity id, since most beats in one run share a single narrator — pass the
    /// same dictionary across a batch of calls (e.g. one node's worth of beats) to avoid
    /// re-querying the same character repeatedly.
    /// </summary>
    public async Task<string?> GetPovVoiceHintAsync(
        Guid beatId, Dictionary<Guid, string?>? cache = null, CancellationToken ct = default)
    {
        var povId = await GetPovEntityIdAsync(beatId, ct);
        if (povId is not { } id) return null;

        if (cache != null && cache.TryGetValue(id, out var cached)) return cached;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var character = await db.Characters.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new { c.Role, c.SpeechVocabulary })
            .FirstOrDefaultAsync(ct);
        var hint = character != null && !string.IsNullOrWhiteSpace(character.SpeechVocabulary)
            ? $"{character.Role} — {character.SpeechVocabulary}"
            : null;
        if (cache != null) cache[id] = hint;
        return hint;
    }
}
