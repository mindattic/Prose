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
/// correct position can be identified — disabling removes it from active reading order without
/// destroying it (reversible via --enable), which is more honest than forcing a placement that
/// only trades one confusing spot for another.
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
            var ordered = await workbench.GetOrderedBeatsAsync(nodeId);
            if (beatNumber > ordered.Count)
            {
                Console.Error.WriteLine($"[set-beat-enabled] --beat-number {beatNumber} exceeds beat count ({ordered.Count}).");
                return 1;
            }
            subjectId = ordered[beatNumber - 1].Beat.Id;
        }

        Console.Write($"[set-beat-enabled] Setting beat {subjectId} IsEnabled={enable}… ");
        await workbench.SetBeatMembershipEnabledAsync(nodeId, subjectId, enable);
        Console.WriteLine("ok.");
        return 0;
    }
}
