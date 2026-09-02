using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --set-edge-validity --edge &lt;edgeId&gt; [--slug &lt;slug&gt;]
///        [--from-beat-number &lt;N&gt;] [--until-beat-number &lt;N&gt;] [--clear-from] [--clear-until]
///
/// Sets/adjusts/clears an existing Edge's beat-scoped validity window
/// (Edge.ValidFromBeatId/ValidUntilBeatId — see BeatRangeService). The common real workflow: an
/// edge already exists as "always true" (created via link_entities with no bounds, the normal
/// case — most facts don't know their own end date at creation time), then the story later
/// establishes when it started or ended, e.g. "Kyle loses the motorcycle at beat 61." This
/// command retroactively bounds/closes it — it does NOT create or merge any edge.
///
/// --from-beat-number/--until-beat-number resolve via the SAME 1-indexed reading-order idiom
/// MoveBeatToNodeCli.cs uses for --beat-number: workbench.GetOrderedBeatsAsync(nodeId)[N-1],
/// relative to --slug's book. --clear-from/--clear-until null out a bound instead.
/// </summary>
public static class SetEdgeValidityCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var edgeArg = Flag(args, "--edge");
        var slug = Flag(args, "--slug");
        var fromBeatNumberArg = Flag(args, "--from-beat-number");
        var untilBeatNumberArg = Flag(args, "--until-beat-number");
        var clearFrom = args.Contains("--clear-from");
        var clearUntil = args.Contains("--clear-until");

        if (!long.TryParse(edgeArg, out var edgeId))
        {
            Console.Error.WriteLine(
                "Usage: prose --set-edge-validity --edge <edgeId> [--slug <slug>] " +
                "[--from-beat-number <N>] [--until-beat-number <N>] [--clear-from] [--clear-until]");
            return 2;
        }

        var wantsFrom = fromBeatNumberArg != null || clearFrom;
        var wantsUntil = untilBeatNumberArg != null || clearUntil;
        if (!wantsFrom && !wantsUntil)
        {
            Console.Error.WriteLine(
                "[set-edge-validity] Nothing to do — pass --from-beat-number/--clear-from and/or " +
                "--until-beat-number/--clear-until.");
            return 2;
        }
        if (fromBeatNumberArg != null && clearFrom)
        {
            Console.Error.WriteLine("[set-edge-validity] --from-beat-number and --clear-from are mutually exclusive.");
            return 2;
        }
        if (untilBeatNumberArg != null && clearUntil)
        {
            Console.Error.WriteLine("[set-edge-validity] --until-beat-number and --clear-until are mutually exclusive.");
            return 2;
        }
        if ((fromBeatNumberArg != null || untilBeatNumberArg != null) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[set-edge-validity] --from-beat-number/--until-beat-number require --slug (which book's reading order).");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var edge = await db.Edges.FirstOrDefaultAsync(e => e.Id == edgeId);
        if (edge == null)
        {
            Console.Error.WriteLine($"[set-edge-validity] No edge with id {edgeId}.");
            return 1;
        }

        Guid? fromBeatId = null, untilBeatId = null;
        if (fromBeatNumberArg != null || untilBeatNumberArg != null)
        {
            var workbench = services.GetRequiredService<NodeWorkbenchService>();
            // Slug OR NodeCode, same as ReadBeatsCli — see GearCheckCli's 2026-09-02 fix note.
            var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(n => n.Slug == slug || n.NodeCode == slug);
            if (node == null)
            {
                Console.Error.WriteLine($"[set-edge-validity] Node '{slug}' not found.");
                return 1;
            }
            var ordered = await workbench.GetOrderedBeatsAsync(node.Id);

            if (fromBeatNumberArg != null)
            {
                if (!int.TryParse(fromBeatNumberArg, out var fromN) || fromN < 1 || fromN > ordered.Count)
                {
                    Console.Error.WriteLine($"[set-edge-validity] --from-beat-number {fromBeatNumberArg} out of range (1-{ordered.Count}) for '{slug}'.");
                    return 1;
                }
                fromBeatId = ordered[fromN - 1].Beat.Id;
            }
            if (untilBeatNumberArg != null)
            {
                if (!int.TryParse(untilBeatNumberArg, out var untilN) || untilN < 1 || untilN > ordered.Count)
                {
                    Console.Error.WriteLine($"[set-edge-validity] --until-beat-number {untilBeatNumberArg} out of range (1-{ordered.Count}) for '{slug}'.");
                    return 1;
                }
                untilBeatId = ordered[untilN - 1].Beat.Id;
            }
        }

        var beforeFrom = edge.ValidFromBeatId;
        var beforeUntil = edge.ValidUntilBeatId;

        if (clearFrom) edge.ValidFromBeatId = null;
        else if (fromBeatId != null) edge.ValidFromBeatId = fromBeatId;

        if (clearUntil) edge.ValidUntilBeatId = null;
        else if (untilBeatId != null) edge.ValidUntilBeatId = untilBeatId;

        await db.SaveChangesAsync();

        Console.WriteLine($"[set-edge-validity] Edge {edgeId} (\"{edge.RelationType}\"):");
        Console.WriteLine($"  ValidFromBeatId:  {beforeFrom} -> {edge.ValidFromBeatId}");
        Console.WriteLine($"  ValidUntilBeatId: {beforeUntil} -> {edge.ValidUntilBeatId}");
        return 0;
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
