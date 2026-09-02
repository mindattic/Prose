using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Cli;

/// <summary>
/// prose --merge-edge --keep &lt;edgeId&gt; --dedupe &lt;edgeId&gt; [--as &lt;canonicalRelationType&gt;] [--register-alias]
///
/// The execution half of <see cref="ScanEdgeDuplicatesCli"/> (report-only): a human, having
/// confirmed from real book/prose knowledge that two Edge rows describe the same relationship
/// fact under different wording (e.g. "owns" vs "has"), collapses them to one. Deliberately takes
/// only two Edge ids — no fuzzy matching, no LLM call; the identity judgment must already be made
/// by the caller before this runs (same design as <see cref="MergeEntityCli"/> for entities).
///
/// Soft-deletes the loser via <see cref="Edge.InvalidatedAt"/> rather than a hard delete — Edges
/// is not one of the system-versioned temporal tables the "no SQL deletes" hard rule covers, and
/// InvalidatedAt is the field the Edge model already designed for exactly this.
/// </summary>
public static class MergeEdgeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var keepArg = Flag(args, "--keep");
        var dedupeArg = Flag(args, "--dedupe");
        var asRelationType = Flag(args, "--as");
        var registerAlias = args.Contains("--register-alias");

        if (!long.TryParse(keepArg, out var keepId) || !long.TryParse(dedupeArg, out var dedupeId))
        {
            Console.Error.WriteLine(
                "Usage: prose --merge-edge --keep <edgeId> --dedupe <edgeId> [--as <canonicalRelationType>] [--register-alias]");
            return 2;
        }

        if (keepId == dedupeId)
        {
            Console.Error.WriteLine("[merge-edge] --keep and --dedupe must be different edges.");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var keep = await db.Edges.FirstOrDefaultAsync(e => e.Id == keepId);
        var dedupe = await db.Edges.FirstOrDefaultAsync(e => e.Id == dedupeId);

        if (keep == null)
        {
            Console.Error.WriteLine($"[merge-edge] No edge with id {keepId} (--keep).");
            return 1;
        }
        if (dedupe == null)
        {
            Console.Error.WriteLine($"[merge-edge] No edge with id {dedupeId} (--dedupe).");
            return 1;
        }
        if (dedupe.InvalidatedAt != null)
        {
            Console.Error.WriteLine($"[merge-edge] Edge {dedupeId} is already invalidated.");
            return 1;
        }
        if (keep.SourceId != dedupe.SourceId || keep.TargetId != dedupe.TargetId)
        {
            Console.Error.WriteLine(
                $"[merge-edge] Edge {keepId} and {dedupeId} do not share the same (Source, Target) pair — " +
                "refusing to merge edges between different entity pairs.");
            return 1;
        }

        var originalDedupeRelationType = dedupe.RelationType;
        var originalKeepRelationType = keep.RelationType;

        dedupe.InvalidatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(asRelationType))
            keep.RelationType = asRelationType.Trim();

        if (registerAlias)
        {
            // Same normalization POST /api/edges applies at write time, so this alias actually
            // matches future free-text of this wording.
            var aliasText = originalDedupeRelationType.Trim().ToLowerInvariant().Replace(' ', '_');
            var canonical = keep.RelationType.Trim().ToLowerInvariant().Replace(' ', '_');
            var existing = await db.Set<RelationTypeAlias>()
                .FirstOrDefaultAsync(a => a.Alias.ToLower() == aliasText);
            if (existing == null && aliasText != canonical)
            {
                db.Set<RelationTypeAlias>().Add(new RelationTypeAlias
                {
                    Alias = aliasText,
                    CanonicalRelationType = canonical,
                    Notes = $"Registered by `prose --merge-edge` collapsing edge {dedupeId} into {keepId}.",
                });
            }
        }

        await db.SaveChangesAsync();

        Console.WriteLine($"[merge-edge] Invalidated edge {dedupeId} (\"{originalDedupeRelationType}\").");
        if (!string.IsNullOrWhiteSpace(asRelationType) && asRelationType.Trim() != originalKeepRelationType)
            Console.WriteLine($"[merge-edge] Relabeled edge {keepId}: \"{originalKeepRelationType}\" -> \"{keep.RelationType}\".");
        if (registerAlias)
            Console.WriteLine($"[merge-edge] Registered alias: \"{originalDedupeRelationType}\" -> \"{keep.RelationType}\".");

        return 0;
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
