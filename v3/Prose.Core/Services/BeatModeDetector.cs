using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Prose.Core.Services;

/// <summary>
/// Classifies a beat's dominant mode from the BeatGoal (and optionally SceneSoFar) using
/// keyword scanning. Results are persisted to BeatModeLog so patterns can be queried later.
///
/// Called by ProseWriterRouter before enriching BeatContext.
/// </summary>
public class BeatModeDetector(IDbContextFactory<ProseDbContext> dbFactory)
{
    static readonly string[] CombatKw    = ["fight", "blade", "gun", "shot", "combat", "battle", "clash", "kill", "kills", "attack", "attacks", "dodge", "chase", "chased", "ambush", "standoff", "firefight", "brawl", "shoot", "stab", "punches", "evade", "escapes", "shootout"];
    static readonly string[] EmotionalKw = ["grief", "loss", "mourning", "hollow", "breaks down", "confront truth", "confess", "confession", "face the cost", "falls apart", "weeping", "weeps", "processes"];
    static readonly string[] DialogueKw  = ["negotiation", "interrogation", "interview", "debrief", "argument", "confrontation", "meeting", "talks", "convinces", "persuades", "asks", "tells", "says", "proposes", "warns", "informs", "explains", "demands", "admits", "denies", "threatens", "bargains", "conversation", "exchange", "speaks", "addresses", "queries", "questions"];
    static readonly string[] TransitionKw = ["travels", "commutes", "arrives", "departs", "moves through", "en route", "journey"];
    static readonly string[] RevelationKw = ["discovers", "realizes", "learns", "uncovers", "decodes", "solves", "pieces together"];

    /// <summary>
    /// Detect the dominant BeatMode from beat goal text and an optional prose hint (SceneSoFar tail).
    /// Returns (mode, confidence 0..1, detection method label).
    /// </summary>
    public (BeatMode Mode, float Confidence, string Method) Detect(string? beatGoal, string? proseHint = null)
    {
        if (string.IsNullOrWhiteSpace(beatGoal))
            return (BeatMode.Narrative, 0.5f, "default");

        var text = beatGoal.ToLowerInvariant();
        if (proseHint != null)
            text = text + " " + (proseHint.Length > 500 ? proseHint[..500] : proseHint).ToLowerInvariant();

        if (CombatKw.Any(k => text.Contains(k)))     return (BeatMode.Combat,          0.85f, "keyword");
        if (EmotionalKw.Any(k => text.Contains(k)))  return (BeatMode.EmotionalClimax,  0.80f, "keyword");
        if (DialogueKw.Any(k => text.Contains(k)))   return (BeatMode.Dialogue,         0.75f, "keyword");
        if (TransitionKw.Any(k => text.Contains(k))) return (BeatMode.Transition,       0.70f, "keyword");
        if (RevelationKw.Any(k => text.Contains(k))) return (BeatMode.Revelation,       0.70f, "keyword");

        return (BeatMode.Narrative, 0.5f, "default");
    }

    /// <summary>
    /// Persist the detected mode to BeatModeLog. Upserts by BeatId.
    /// Non-blocking — silently swallows exceptions so callers are never interrupted.
    /// </summary>
    public async Task PersistAsync(Guid beatId, Guid universeId, BeatMode mode, float confidence, string method, CancellationToken ct = default)
    {
        if (beatId == Guid.Empty) return;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var existing = await db.BeatModeLogs.FindAsync(new object[] { beatId }, ct);
            if (existing != null)
            {
                existing.Mode = mode.ToString();
                existing.Confidence = confidence;
                existing.DetectionMethod = method;
                existing.DetectedAt = DateTime.UtcNow;
            }
            else
            {
                db.BeatModeLogs.Add(new BeatModeLog
                {
                    BeatId = beatId,
                    UniverseId = universeId,
                    Mode = mode.ToString(),
                    Confidence = confidence,
                    DetectionMethod = method,
                });
            }
            await db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) { throw; }
        catch { /* non-blocking */ }
    }
}
