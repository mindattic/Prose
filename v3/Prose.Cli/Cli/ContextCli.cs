using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --context</c> — manage user context overrides for the DocContextStack.
///
/// Subcommands:
///   <c>add</c>     Pin a doc so it is always included in beat prompts.
///   <c>remove</c>  Remove any override (pin or exclude) for a doc.
///   <c>exclude</c> Exclude a doc that would normally be injected.
///   <c>clear</c>   Remove all active overrides for this session.
///   <c>status</c>  Show currently active overrides with estimated tokens.
///
/// Flags:
///   <c>--doc &lt;relative-path-or-id&gt;</c>  Identify the markdown doc (required for add/remove/exclude).
///   <c>--node &lt;slug&gt;</c>               Scope the override to a specific node (default = global).
/// </summary>
public static class ContextCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var subcommand = args.FirstOrDefault()?.ToLowerInvariant();
        if (subcommand is null or "--help" or "-h")
        {
            PrintUsage();
            return 0;
        }

        string? docArg = null, nodeSlug = null;
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--doc"  && i + 1 < args.Length) docArg   = args[++i];
            if (args[i] == "--node" && i + 1 < args.Length) nodeSlug = args[++i];
        }

        var userCtx = services.GetRequiredService<UserContextService>();

        // Resolve optional node scope
        Guid? nodeId = null;
        if (!string.IsNullOrWhiteSpace(nodeSlug))
        {
            nodeId = await ResolveNodeAsync(nodeSlug, services);
            if (nodeId == null)
            {
                Console.Error.WriteLine($"[context] Node '{nodeSlug}' not found.");
                return 1;
            }
        }

        switch (subcommand)
        {
            case "add":
            case "pin":
                return await PinAsync(docArg, nodeId, userCtx, services);

            case "exclude":
                return await ExcludeAsync(docArg, nodeId, userCtx, services);

            case "remove":
                return await RemoveAsync(docArg, nodeId, userCtx, services);

            case "clear":
                await userCtx.ClearAsync(nodeId);
                var scope = nodeId.HasValue ? $" (node-scoped)" : " (global)";
                Console.WriteLine($"[context] Cleared all active overrides{scope}.");
                return 0;

            case "status":
                return await PrintStatusAsync(userCtx);

            default:
                Console.Error.WriteLine($"[context] Unknown subcommand '{subcommand}'. Run 'ss --context --help'.");
                return 1;
        }
    }

    // ── Subcommand handlers ────────────────────────────────────────────────────

    private static async Task<int> PinAsync(
        string? docArg, Guid? nodeId, UserContextService userCtx, IServiceProvider services)
    {
        var docId = await ResolveDocAsync(docArg, services);
        if (docId == null) return 1;

        await userCtx.PinAsync(docId.Value, nodeId);
        var scope = nodeId.HasValue ? $" for node {nodeId}" : " (global)";
        Console.WriteLine($"[context] Pinned '{docArg}'{scope} — expires in 24h.");
        return 0;
    }

    private static async Task<int> ExcludeAsync(
        string? docArg, Guid? nodeId, UserContextService userCtx, IServiceProvider services)
    {
        var docId = await ResolveDocAsync(docArg, services);
        if (docId == null) return 1;

        await userCtx.ExcludeAsync(docId.Value, nodeId);
        var scope = nodeId.HasValue ? $" for node {nodeId}" : " (global)";
        Console.WriteLine($"[context] Excluded '{docArg}'{scope} — expires in 24h.");
        return 0;
    }

    private static async Task<int> RemoveAsync(
        string? docArg, Guid? nodeId, UserContextService userCtx, IServiceProvider services)
    {
        var docId = await ResolveDocAsync(docArg, services);
        if (docId == null) return 1;

        await userCtx.RemoveAsync(docId.Value, nodeId);
        Console.WriteLine($"[context] Removed override for '{docArg}'.");
        return 0;
    }

    private static async Task<int> PrintStatusAsync(UserContextService userCtx)
    {
        var report = await userCtx.GetStatusAsync();
        if (report.Entries.Count == 0)
        {
            Console.WriteLine($"[context] No active overrides for session '{report.SessionKey}'.");
            return 0;
        }

        Console.WriteLine($"[context] Active overrides  session={report.SessionKey}");
        Console.WriteLine(new string('─', 64));
        foreach (var e in report.Entries)
        {
            var scope   = e.NodeId.HasValue ? $"node:{e.NodeId}" : "global";
            var action  = e.Action.ToUpperInvariant().PadRight(7);
            var expires = e.ExpiresAt.ToString("HH:mm UTC");
            Console.WriteLine($"  {action}  {e.RelativePath,-40}  {scope,-20}  exp {expires}");
        }
        return 0;
    }

    // ── Resolvers ──────────────────────────────────────────────────────────────

    private static async Task<Guid?> ResolveDocAsync(string? docArg, IServiceProvider services)
    {
        if (string.IsNullOrWhiteSpace(docArg))
        {
            Console.Error.WriteLine("[context] --doc is required. Provide a relative path or GUID.");
            return null;
        }

        // Try parse as GUID directly
        if (Guid.TryParse(docArg, out var g)) return g;

        // Look up by RelativePath (partial match OK)
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var hits = await db.MarkdownFiles.AsNoTracking()
            .Where(m => m.RelativePath.Contains(docArg))
            .Select(m => new { m.Id, m.RelativePath })
            .Take(5)
            .ToListAsync();

        if (hits.Count == 0)
        {
            Console.Error.WriteLine($"[context] No markdown doc found matching '{docArg}'. Run 'ss --recall <keyword>' to browse.");
            return null;
        }

        if (hits.Count > 1)
        {
            Console.Error.WriteLine($"[context] Ambiguous — {hits.Count} docs match '{docArg}':");
            foreach (var h in hits)
                Console.Error.WriteLine($"  {h.Id}  {h.RelativePath}");
            Console.Error.WriteLine("Provide a more specific path or paste the GUID.");
            return null;
        }

        return hits[0].Id;
    }

    private static async Task<Guid?> ResolveNodeAsync(string slug, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Nodes.AsNoTracking()
            .Where(n => n.Slug == slug)
            .Select(n => (Guid?)n.Id)
            .FirstOrDefaultAsync();
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
            ss --context <subcommand> [options]

            Subcommands:
              add      --doc <path|guid> [--node <slug>]   Pin doc into every beat prompt
              exclude  --doc <path|guid> [--node <slug>]   Exclude doc even if it matches
              remove   --doc <path|guid> [--node <slug>]   Remove existing override
              clear    [--node <slug>]                     Remove all active overrides
              status                                       Show active overrides

            All overrides expire after 24h or on explicit 'clear'.
            --node scopes the override to one story; omit for session-global.
            """);
    }
}
