using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Produces a "fair context" view around any beat: N beats before, N beats
/// after, plus the most recent ElevenLabs request-ids for prosodic stitching.
/// Same window feeds both narration (ElevenLabs previous_text / next_text /
/// previous_request_ids) and LLM regeneration prompts ("here's what came
/// before, here's what comes after, rewrite the middle beat").
///
/// Cheap because of the SortKey index on EpisodeBeats — a single range scan
/// returns the entire chapter in SortKey order, then we slice the window in
/// memory. For a 2000-beat chapter that's still ~50 KB read; with the request
/// shape we have it's well under 10 ms even on a cold cache.
/// </summary>
public class BeatContextService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<BeatContextService> log;

    public BeatContextService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<BeatContextService> log)
    {
        this.dbFactory = dbFactory;
        this.log = log;
    }

    /// <summary>Get a context window centered on the beat at the given stable
    /// Index inside the given episode. Returns the target, up to
    /// <paramref name="before"/> preceding beats (in order, nearest-last), up
    /// to <paramref name="after"/> following beats (in order, nearest-first),
    /// and up to 3 most recent narration request-ids (newest last) suitable
    /// for ElevenLabs previous_request_ids stitching.</summary>
    public async Task<BeatWindow?> GetWindowAsync(
        Guid episodeId,
        int beatIndex,
        int before = 12,
        int after  = 12,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var all = await db.EpisodeBeats
            .AsNoTracking()
            .Where(b => b.EpisodeId == episodeId)
            .OrderBy(b => b.SortKey).ThenBy(b => b.Index)
            .ToListAsync(ct);

        var pos = all.FindIndex(b => b.Index == beatIndex);
        if (pos < 0) return null;

        var target   = all[pos];
        var previous = all.Skip(Math.Max(0, pos - before)).Take(Math.Min(pos, before)).ToList();
        var next     = all.Skip(pos + 1).Take(after).ToList();

        // Sliding window of the 3 most recent rendered request-ids — only beats
        // that have been narrated successfully contribute. Note: we don't yet
        // persist request-ids per beat (they live transiently inside the
        // narration loop), so this is empty until a future migration adds the
        // column. The narration loop's own in-memory stitchWindow remains the
        // authoritative source while a single run is in flight.
        var requestIds = Array.Empty<string>();

        return new BeatWindow(target, previous, next, requestIds);
    }

    /// <summary>Render the previous-text and next-text strings the narration
    /// loop hands to ElevenLabs. Walks outward from the target collecting
    /// paragraph text until <paramref name="contextChars"/> is reached.</summary>
    public static (string? previous, string? next) AsTextWindow(
        BeatWindow window, int contextChars = 1500)
    {
        string? prev = null;
        string? next = null;

        var prevBuf = new System.Text.StringBuilder();
        // Walk previous in reverse (nearest-last → nearest-first while
        // accumulating) but produce text in reading order.
        var prevParts = new List<string>();
        for (int i = window.Previous.Count - 1; i >= 0; i--)
        {
            var t = window.Previous[i].Text;
            if (string.IsNullOrEmpty(t)) continue;
            if (prevBuf.Length + t.Length > contextChars) break;
            prevBuf.Append(t);
            prevParts.Insert(0, t);
        }
        if (prevParts.Count > 0) prev = string.Join("\n\n", prevParts);

        var nextBuf = new System.Text.StringBuilder();
        foreach (var b in window.Next)
        {
            if (string.IsNullOrEmpty(b.Text)) continue;
            if (nextBuf.Length + b.Text.Length > contextChars) break;
            nextBuf.Append(b.Text).Append('\n');
        }
        if (nextBuf.Length > 0) next = nextBuf.ToString().TrimEnd();

        return (prev, next);
    }

    /// <summary>Render a brief that an LLM regenerator can use as the "what
    /// is this beat accomplishing" framing. Combines the target's narrative
    /// metadata with a textual context summary so the model can rewrite the
    /// target while staying on tone and on structure.</summary>
    public static string AsRegenerationBrief(BeatWindow window)
    {
        var sb = new System.Text.StringBuilder();
        var t = window.Target;
        sb.AppendLine("BEAT TO REWRITE:");
        if (!string.IsNullOrEmpty(t.BeatTitle))     sb.AppendLine($"  Title: {t.BeatTitle}");
        if (!string.IsNullOrEmpty(t.Synopsis))      sb.AppendLine($"  Synopsis: {t.Synopsis}");
        if (!string.IsNullOrEmpty(t.StructureRole)) sb.AppendLine($"  Role: {t.StructureRole}");
        if (t.Act > 0)                              sb.AppendLine($"  Act: {t.Act}");
        if (!string.IsNullOrEmpty(t.FacetTag))      sb.AppendLine($"  Facet: {t.FacetTag}");
        if (!string.IsNullOrEmpty(t.EmotionalTone)) sb.AppendLine($"  Tone: {t.EmotionalTone}");
        if (!string.IsNullOrEmpty(t.PaceHint))      sb.AppendLine($"  Pace: {t.PaceHint}");
        sb.AppendLine();
        sb.AppendLine("CURRENT PROSE:");
        sb.AppendLine(t.Text);
        sb.AppendLine();
        if (window.Previous.Count > 0)
        {
            sb.AppendLine("WHAT CAME BEFORE (in order):");
            foreach (var p in window.Previous) sb.AppendLine(p.Text);
            sb.AppendLine();
        }
        if (window.Next.Count > 0)
        {
            sb.AppendLine("WHAT COMES AFTER (in order):");
            foreach (var p in window.Next) sb.AppendLine(p.Text);
        }
        return sb.ToString();
    }
}

/// <summary>The fair-context view of a beat. Previous is in reading order
/// (nearest-last); Next is in reading order (nearest-first). RecentRequestIds
/// is empty for now until per-beat request-id persistence ships.</summary>
public record BeatWindow(
    EpisodeBeat Target,
    IReadOnlyList<EpisodeBeat> Previous,
    IReadOnlyList<EpisodeBeat> Next,
    IReadOnlyList<string> RecentRequestIds);
