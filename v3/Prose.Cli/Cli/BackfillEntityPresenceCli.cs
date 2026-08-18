using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --backfill-entity-presence [--slug &lt;slug&gt;] [--dry-run]
///
/// Re-runs SceneContextAssembler.AssembleForBeatAsync + PersistRosterAsync (BeatEntities roster)
/// against already-written beats that currently have zero rows there. No LLM call —
/// AssembleForBeatAsync is a pure name/alias/embedding scan (HarvestRevealedDetailsAsync is the
/// only LLM-dependent method on this service, and this tool never calls it).
///
/// Built 2026-08-10 to empirically verify the TRUCE alias fix (Meraq/Vaen registered as
/// CharacterAliases) without needing a live prose-generation pass, since BeatEntities/
/// BeatEntityPresence otherwise only get (re)populated as a fire-and-forget side effect inside
/// ProseWriterRouter.WriteAsync itself — there was previously no standalone way to re-trigger
/// detection against existing prose. See project_service_coverage_alias_gap_2026_08_10 memory.
///
/// Scope: without --slug, processes every enabled beat with non-empty Text and zero BeatEntities
/// rows, corpus-wide. With --slug, restricts to that node's own leaf-descendant beats.
/// </summary>
public static class BackfillEntityPresenceCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dryRun = args.Contains("--dry-run");
        string? slug = null;
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[++i];

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var assembler = services.GetRequiredService<SceneContextAssembler>();

        List<Guid> beatIds;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            List<Guid>? nodeIdScope = null;
            if (slug != null)
            {
                // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
                var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Slug == slug || n.NodeCode == slug);
                if (node == null)
                {
                    Console.Error.WriteLine($"[backfill-entity-presence] No node found with slug or code '{slug}'.");
                    return 1;
                }
                var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id);
                nodeIdScope = leafIds.Count > 0 ? leafIds : new List<Guid> { node.Id };
            }

            beatIds = await SelectCandidateBeatIdsAsync(db, nodeIdScope);
        }

        // BeatEntities has no EF mapping (raw-SQL table, per CLAUDE.md) — filter for "zero rows"
        // via a raw scalar check per beat rather than a DbSet join.
        await using var checkDb = await dbFactory.CreateDbContextAsync();
        var withRoster = new HashSet<Guid>(
            await checkDb.Database.SqlQuery<Guid>($"SELECT DISTINCT BeatId FROM BeatEntities").ToListAsync());
        var targets = beatIds.Where(id => !withRoster.Contains(id)).ToList();

        Console.WriteLine($"[backfill-entity-presence] {targets.Count} beat(s) with prose text and no existing roster.");
        if (targets.Count == 0 || dryRun)
        {
            if (dryRun) Console.WriteLine("(DRY RUN — no changes written)");
            return 0;
        }

        int assembled = 0, foundEntities = 0, empty = 0;
        foreach (var beatId in targets)
        {
            var ctx = await assembler.AssembleForBeatAsync(beatId, tokenBudget: 500);
            if (ctx == null) continue;
            await assembler.PersistRosterAsync(beatId, ctx);
            assembled++;
            if (ctx.Roster.Count > 0) foundEntities++; else empty++;
        }

        Console.WriteLine($"[backfill-entity-presence] processed={assembled} found-entities={foundEntities} still-empty={empty}");
        return 0;
    }

    /// <summary>
    /// Every enabled beat with non-empty text, restricted to <paramref name="nodeIdScope"/> when
    /// given. Always joins through Nodes — even when nodeIdScope is null — because BeatNodes/Beats
    /// carry no UniverseId of their own, so the ambient --universe global query filter (applied
    /// only to Nodes) would otherwise never apply, silently processing the WHOLE corpus regardless
    /// of --universe. Found 2026-08-10: a "--universe scry" run with no --slug reprocessed the
    /// same leftover beats a prior GLMZ run had already queued, because this join was missing.
    /// Internal so it's unit-testable without constructing the full CLI's service dependencies.
    /// </summary>
    internal static async Task<List<Guid>> SelectCandidateBeatIdsAsync(ProseDbContext db, List<Guid>? nodeIdScope)
    {
        var query =
            from n in db.Nodes.AsNoTracking()
            join bn in db.BeatNodes.AsNoTracking() on n.Id equals bn.NodeId
            where true
            join b in db.Beats.AsNoTracking() on bn.BeatId equals b.Id
            where b.Text != null && b.Text != ""
            select new { bn.NodeId, b.Id };

        if (nodeIdScope != null)
            query = query.Where(x => nodeIdScope.Contains(x.NodeId));

        return await query.Select(x => x.Id).ToListAsync();
    }
}
