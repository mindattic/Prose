using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --backfill-pov [--slug &lt;slug&gt;] [--dry-run]
///
/// Heuristically assigns a POV entity (BeatEntityPresence, PresenceType='pov') to every beat
/// that has an already-persisted BeatEntities roster but no 'pov' row yet — the highest-scoring
/// character-type row in that roster. No LLM call: pure DB read + upsert, reusing data
/// SceneContextAssembler already computed rather than re-deriving anything.
///
/// Built 2026-08-13 (plan "Making Prose readable, character-true, and legible") after finding
/// BeatEntityPresence has no live write path anywhere in the codebase — DocContextService's
/// per-beat voice-pinning (SS-A46 layer 4) and several audits (SACRED-FLAW, VOICE-DRIFT) depend
/// on this table and were silently starved for POV data on effectively the whole corpus.
/// ProseWriterRouter.WriteAsync now writes this forward for every new beat
/// (SceneContextAssembler.PersistPovAsync); this CLI is the one-time backward pass for beats
/// already written before that fix landed. Where a chapter-level PovCharacter already exists
/// on BookOutline, prefer that over this heuristic (higher confidence) — not implemented in
/// this pass; flagged for a follow-up if BookOutline data proves more reliable in practice.
///
/// Scope: without --slug, processes every book corpus-wide. With --slug, restricts to that
/// node's own leaf-descendant beats.
/// </summary>
public static class BackfillPovCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dryRun = args.Contains("--dry-run");
        string? slug = null;
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[++i];

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var assembler = services.GetRequiredService<SceneContextAssembler>();

        List<Guid>? nodeIdScope = null;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            if (slug != null)
            {
                var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Slug == slug || n.NodeCode == slug);
                if (node == null)
                {
                    Console.Error.WriteLine($"[backfill-pov] No node found with slug or code '{slug}'.");
                    return 1;
                }
                var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id);
                nodeIdScope = leafIds.Count > 0 ? leafIds : new List<Guid> { node.Id };
            }
        }

        await using var db2 = await dbFactory.CreateDbContextAsync();

        // Beats already tagged 'pov' — skip these outright (don't touch existing legitimate data,
        // heuristic or otherwise).
        var alreadyTagged = new HashSet<Guid>(
            await db2.Database.SqlQuery<Guid>($"SELECT DISTINCT BeatId FROM BeatEntityPresence WHERE PresenceType = 'pov'").ToListAsync());

        var candidateBeatIds = await BackfillEntityPresenceCli.SelectCandidateBeatIdsAsync(db2, nodeIdScope);
        var targets = candidateBeatIds.Where(id => !alreadyTagged.Contains(id)).ToList();

        Console.WriteLine($"[backfill-pov] {targets.Count} beat(s) with prose text and no existing 'pov' row.");
        if (targets.Count == 0) return 0;

        // BeatEntities has no EF mapping — raw read, same pattern as PersistRosterAsync.
        var rosterRows = await db2.Database.SqlQuery<RosterRow>(
            $"SELECT BeatId, EntityId, Name, EntityType, Score FROM BeatEntities WHERE EntityType = 'character'")
            .ToListAsync();
        var rosterByBeat = rosterRows
            .GroupBy(r => r.BeatId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.Score).ToList());

        if (dryRun)
        {
            var withRoster = targets.Count(id => rosterByBeat.ContainsKey(id));
            Console.WriteLine($"[backfill-pov] DRY RUN — {withRoster}/{targets.Count} have a character-type roster to pick a POV from.");
            return 0;
        }

        int tagged = 0, noRoster = 0;
        foreach (var beatId in targets)
        {
            if (!rosterByBeat.TryGetValue(beatId, out var characters) || characters.Count == 0)
            {
                noRoster++;
                continue;
            }

            var top = characters[0];
            var ctx = new SceneContext
            {
                Roster = [new SceneEntityRef(top.EntityId, top.Name, "character", "backfill-pov", top.Score)],
                ContextBlock = "",
            };
            await assembler.PersistPovAsync(beatId, ctx);
            tagged++;
        }

        Console.WriteLine($"[backfill-pov] tagged={tagged} no-character-roster={noRoster}");
        return 0;
    }

    private sealed record RosterRow(Guid BeatId, Guid EntityId, string Name, string EntityType, double Score);
}
