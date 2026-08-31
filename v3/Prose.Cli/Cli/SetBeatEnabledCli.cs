using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --set-beat-enabled</c> — enable/disable a beat's membership in a node's reading
/// order, without touching the Beat row itself (its prose/audio and any OTHER node's membership
/// survive). Wraps NodeWorkbenchService.SetBeatMembershipEnabledAsync (new 2026-08-09).
///
/// Use when a beat is found sorted into a chapter/book it has no real connection to and no
/// correct position can be identified. NOTE (corrected 2026-08-31 — this doc previously claimed
/// disabling was reversible/non-destructive; it is not): a BeatNode row IS the enabled state —
/// "disabling" removes that row, and per SetBeatMembershipEnabledAsync, if no other node still
/// references the beat, the Beat row itself is deleted outright (system-versioned temporal table,
/// so recoverable only via a point-in-time restore, not via --enable). Treat --set-beat-enabled
/// (disable direction) as a real, CLI-gated deletion, not a soft toggle.
///
///   --slug &lt;slug&gt;       Node slug.
///   --beat-number &lt;N&gt;   1-indexed beat position in CURRENT reading order (disable only —
///                        a disabled beat drops out of reading order, so re-enabling it needs
///                        --beat-id instead since it's no longer addressable by position).
///   --beat-id &lt;guid&gt;    Beat GUID — works regardless of current enabled state.
///   --enable             Re-enable a previously-disabled membership (default: disable).
///
/// Exit codes: 0 = success, 1 = bad args / node not found / beat not found.
/// </summary>
public static class SetBeatEnabledCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, beatIdStr = null;
        int beatNumber = 0;
        bool enable = args.Contains("--enable");

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":        if (i + 1 < args.Length) slug = args[++i]; break;
                case "--beat-number": if (i + 1 < args.Length) int.TryParse(args[++i], out beatNumber); break;
                case "--beat-id":     if (i + 1 < args.Length) beatIdStr = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[set-beat-enabled] --slug is required.");
            return 1;
        }
        if (beatNumber < 1 && string.IsNullOrWhiteSpace(beatIdStr))
        {
            Console.Error.WriteLine("[set-beat-enabled] --beat-number or --beat-id is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();

        Guid nodeId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
            var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slug);
            if (node == null) { Console.Error.WriteLine($"[set-beat-enabled] Node '{slug}' not found."); return 1; }
            nodeId = node.Id;
        }

        // Resolve against the FULL descendant walk, not the raw --slug node directly — for a
        // book whose beats actually live on a child chapter node (the normal Book->Chapter->Beat
        // shape), SetBeatMembershipEnabledAsync needs the beat's REAL owning node, exactly like
        // MoveBeatToNodeCli's actualFromNodeId/actualToNodeId pattern. Passing the raw --slug
        // node here threw "no membership row" for every VIGL beat (2026-08-31 bug fix) because
        // VIGL's beats live on its chapter node, not the book node --slug resolves to.
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId);

        Guid subjectId;
        if (!string.IsNullOrWhiteSpace(beatIdStr))
        {
            if (!Guid.TryParse(beatIdStr, out subjectId))
            {
                Console.Error.WriteLine($"[set-beat-enabled] --beat-id is not a valid GUID: {beatIdStr}");
                return 1;
            }
        }
        else
        {
            if (beatNumber > ordered.Count)
            {
                Console.Error.WriteLine($"[set-beat-enabled] --beat-number {beatNumber} exceeds beat count ({ordered.Count}).");
                return 1;
            }
            subjectId = ordered[beatNumber - 1].Beat.Id;
        }

        var subjectOrdered = ordered.FirstOrDefault(o => o.Beat.Id == subjectId);
        Guid actualNodeId;
        if (subjectOrdered != null)
        {
            actualNodeId = subjectOrdered.NodeId; // the beat's real owning chapter
        }
        else
        {
            // --beat-id pointed at a beat not currently reachable from --slug's descendant walk
            // (e.g. already disabled, or under an unrelated node) — fall back to a direct lookup.
            await using var db = await dbFactory.CreateDbContextAsync();
            var membership = await db.BeatNodes.AsNoTracking().FirstOrDefaultAsync(bn => bn.BeatId == subjectId);
            if (membership == null)
            {
                Console.Error.WriteLine($"[set-beat-enabled] Beat {subjectId} has no membership row anywhere.");
                return 1;
            }
            actualNodeId = membership.NodeId;
        }

        Console.Write($"[set-beat-enabled] Setting beat {subjectId} IsEnabled={enable} (chapter {actualNodeId})… ");
        await workbench.SetBeatMembershipEnabledAsync(actualNodeId, subjectId, enable);
        Console.WriteLine("ok.");
        return 0;
    }
}
