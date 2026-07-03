using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// `ss --assemble-scene (--beat &lt;guid&gt; | --text "&lt;prose&gt;") [--budget N]`
/// X-Ray scene assembly (RFC 0002): prints the entity roster detected in the
/// beat/text (name scan + embeddings + graph hop) and the budgeted context
/// block that prose prompts receive. CLI twin of the MCP tool
/// assemble_scene_context, per the foundations doctrine (CLI ⇄ MCP parity).
/// </summary>
public static class AssembleSceneCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Arg(string name)
        {
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        var beatArg = Arg("--beat");
        var textArg = Arg("--text");
        var slugArg = Arg("--slug");
        var budget = int.TryParse(Arg("--budget"), out var b) ? b : 2000;
        var backfill = args.Contains("--backfill");
        var harvest = args.Contains("--harvest");

        if (backfill && slugArg != null)
            return await BackfillAsync(services, slugArg, harvest);

        if (beatArg == null && textArg == null)
        {
            Console.Error.WriteLine("usage: ss --assemble-scene (--beat <guid> | --text \"<prose>\") [--budget N]");
            Console.Error.WriteLine("       ss --assemble-scene --backfill --slug <node-slug> [--harvest]");
            return 2;
        }

        var assembler = services.GetRequiredService<SceneContextAssembler>();
        SceneContext? ctx;
        if (beatArg != null)
        {
            if (!Guid.TryParse(beatArg, out var beatId))
            {
                Console.Error.WriteLine($"--beat is not a guid: {beatArg}");
                return 2;
            }
            ctx = await assembler.AssembleForBeatAsync(beatId, budget);
            if (ctx == null) { Console.Error.WriteLine($"beat not found: {beatId}"); return 1; }
        }
        else
        {
            ctx = await assembler.AssembleAsync(textArg!, budget);
        }

        Console.WriteLine($"[assemble-scene] roster: {ctx.Roster.Count} entities, ~{ctx.EstimatedTokens} tokens");
        foreach (var r in ctx.Roster)
            Console.WriteLine($"  - {r.Name}  ({r.EntityType}, via {r.MatchSource}, score {r.Score:0.00})");
        Console.WriteLine();
        Console.WriteLine(ctx.ContextBlock);
        return 0;
    }

    /// <summary>Backfill BeatEntities for every beat of a node (tree-walking, so a book
    /// slug covers all chapters). --harvest additionally files XRAY-REVEAL findings:
    /// details the prose reveals about in-scene entities, proposed for explicit approval.</summary>
    private static async Task<int> BackfillAsync(IServiceProvider services, string slug, bool harvest)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamurai.Core.Data.StreetSamuraiDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        var assembler = services.GetRequiredService<SceneContextAssembler>();

        Guid nodeId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var node = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstOrDefaultAsync(db.Nodes, s => s.Slug == slug);
            if (node == null) { Console.Error.WriteLine($"node not found: {slug}"); return 1; }
            nodeId = node.Id;
        }

        var beats = await workbench.GetOrderedBeatsAsync(nodeId);
        Console.WriteLine($"[xray-backfill] {slug}: {beats.Count} beats{(harvest ? " (+harvest)" : "")}");

        int done = 0, rosterRows = 0, proposals = 0, failed = 0;
        foreach (var ob in beats)
        {
            try
            {
                var ctx = await assembler.AssembleForBeatAsync(ob.Beat.Id, tokenBudget: 1200);
                if (ctx != null)
                {
                    await assembler.PersistRosterAsync(ob.Beat.Id, ctx);
                    rosterRows += ctx.Roster.Count;
                }
                if (harvest) proposals += await assembler.HarvestRevealedDetailsAsync(ob.Beat.Id);
            }
            catch (Exception ex)
            {
                failed++;
                Console.Error.WriteLine($"  beat {ob.Beat.Id:N} failed: {ex.Message}");
            }
            done++;
            if (done % 25 == 0) Console.WriteLine($"  …{done}/{beats.Count} ({rosterRows} roster rows{(harvest ? $", {proposals} proposals" : "")})");
        }

        Console.WriteLine($"[xray-backfill] done: {done} beats, {rosterRows} roster rows, {proposals} XRAY-REVEAL proposals, {failed} failed.");
        return failed == 0 ? 0 : 1;
    }
}
