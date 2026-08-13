using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// Shared "write a batch of parsed beats onto a node" logic, extracted so
/// <see cref="ImportNodeCli"/> (new node) and <see cref="ReimportNodeCli"/>
/// (replace an existing node's content) don't duplicate the Beat/BeatNode
/// construction rules. Caller owns the transaction and SaveChanges.
/// </summary>
public static class NodeBeatWriter
{
    /// <summary>
    /// Creates a fresh <see cref="Beat"/> + <see cref="BeatNode"/> row for each
    /// parsed beat and attaches it to <paramref name="nodeId"/>, starting at
    /// <paramref name="startSortKey"/> and incrementing by 100 per beat (the
    /// convention used throughout the codebase for newly-inserted siblings).
    /// Does not call SaveChangesAsync — the caller decides when to commit.
    /// </summary>
    public static async Task<int> WriteBeatsAsync(
        ProseDbContext db,
        Guid nodeId,
        IReadOnlyList<ParsedBeat> beats,
        double startSortKey = 100.0)
    {
        // Pre-allocate a contiguous block of Beat.Number values in one round-trip
        // — matches the pattern in NodeWorkbenchService.SplitBeatByParagraphsAsync
        // and the original ImportNodeCli this was extracted from.
        var baseNumber = (await db.Beats.MaxAsync(b => (int?)b.Number) ?? 0) + 1;
        double sortKey = startSortKey;

        for (int i = 0; i < beats.Count; i++)
        {
            var pb = beats[i];
            var beat = new Beat
            {
                Id             = Guid.CreateVersion7(),
                Number         = baseNumber + i,
                Text           = pb.Text,
                TextHash       = string.IsNullOrEmpty(pb.Text) ? null : NodeWorkbenchService.ComputeTextHash(pb.Text),
                Title          = pb.Title,
                IsChapterStart = pb.IsChapterStart,
                Kind           = string.IsNullOrEmpty(pb.Kind) ? "prose" : pb.Kind,
                Description    = pb.Description,
                StructureRole  = pb.StructureRole,
                Act            = pb.Act,
                SceneType      = string.IsNullOrEmpty(pb.SceneType) ? "scene" : pb.SceneType,
                EmotionalTone  = pb.EmotionalTone,
                PaceHint       = pb.PaceHint,
                GapAfterMs     = pb.GapAfterMs,
                VoiceId        = pb.VoiceId,
            };
            db.Beats.Add(beat);
            db.BeatNodes.Add(new BeatNode { NodeId = nodeId, BeatId = beat.Id, SortKey = sortKey });
            sortKey += 100.0;
        }

        return beats.Count;
    }

    /// <summary>Total word count across a set of parsed beats — used by callers
    /// that want a retention sanity check before overwriting live content.</summary>
    public static int CountWords(IEnumerable<ParsedBeat> beats) =>
        beats.Sum(b => b.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length);
}
