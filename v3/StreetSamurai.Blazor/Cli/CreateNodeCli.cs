using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --create-node</c> — create a new empty root node (no beats). The
/// bible-first entry point for a brand-new story; write the bible + beats
/// afterward (UI, <c>--edit-beat</c>, or <c>--write-node</c>).
///
///   --title "..."          Display title. Required.
///   --code &lt;CODE&gt;          Optional short reference code (e.g. SRZR). Upper-cased; must be unique.
///   --kind &lt;k&gt;             Category — "story" (default), "book", "chapter", "vignette"…
///   --synopsis "..."       Optional one-line synopsis.
///   --logline "..."        Optional one-line generator seed / logline (stored in Node.Seed).
///                          (Named --logline, not --seed, to avoid the global DB seed-runner flag.)
///   --previous &lt;slug|id&gt;   Optional prior node this one continues (sequel commandments).
///   --parent &lt;slug|id&gt;     Optional parent node (makes this a sub-node).
///
/// Exit codes: 0 = success, 1 = error (clash / parent or previous not found), 2 = bad args.
/// </summary>
public static class CreateNodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? title = null, code = null, synopsis = null, seed = null, previous = null, parent = null;
        string kind = "story";

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--title":    if (i + 1 < args.Length) title    = args[++i]; break;
                case "--code":     if (i + 1 < args.Length) code     = args[++i]; break;
                case "--kind":     if (i + 1 < args.Length) kind     = args[++i]; break;
                case "--synopsis": if (i + 1 < args.Length) synopsis = args[++i]; break;
                case "--logline":  if (i + 1 < args.Length) seed     = args[++i]; break;
                case "--previous": if (i + 1 < args.Length) previous = args[++i]; break;
                case "--parent":   if (i + 1 < args.Length) parent   = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            Console.Error.WriteLine("[create-node] --title is required.");
            Console.Error.WriteLine("Usage: ss --create-node --title \"...\" [--code SRZR] [--kind story] [--synopsis \"...\"] [--logline \"...\"] [--previous <slug|id>] [--parent <slug|id>]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();

        Guid? previousId = await ResolveNodeAsync(dbFactory, previous);
        if (previous != null && previousId == null)
        {
            Console.Error.WriteLine($"[create-node] --previous node not found: {previous}");
            return 1;
        }
        Guid? parentId = await ResolveNodeAsync(dbFactory, parent);
        if (parent != null && parentId == null)
        {
            Console.Error.WriteLine($"[create-node] --parent node not found: {parent}");
            return 1;
        }

        try
        {
            var (id, slug) = await workbench.CreateNodeAsync(
                title!, kind, synopsis, seed, code, previousId, parentId);

            Console.WriteLine($"[create-node] Created node:");
            Console.WriteLine($"   Id:    {id}");
            Console.WriteLine($"   Slug:  {slug}");
            Console.WriteLine($"   Title: {title}");
            Console.WriteLine($"   Code:  {(string.IsNullOrWhiteSpace(code) ? "-" : code!.Trim().ToUpperInvariant())}");
            Console.WriteLine($"   Kind:  {kind}");
            Console.WriteLine($"   URL:   https://localhost:7103/node/{slug}");
            Console.WriteLine($"   Next:  add beats via the UI, ss --edit-beat --insert-after N, or ss --write-node.");
            return 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine($"[create-node] {ex.Message}");
            return 1;
        }
    }

    /// <summary>Resolve a node reference (GUID or slug) to its id. Null input → null.</summary>
    private static async Task<Guid?> ResolveNodeAsync(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory, string? slugOrId)
    {
        if (string.IsNullOrWhiteSpace(slugOrId)) return null;
        await using var db = await dbFactory.CreateDbContextAsync();
        if (Guid.TryParse(slugOrId, out var gid))
            return await db.Nodes.AsNoTracking().AnyAsync(s => s.Id == gid) ? gid : null;
        var hit = await db.Nodes.AsNoTracking()
            .Where(s => s.Slug == slugOrId).Select(s => (Guid?)s.Id).FirstOrDefaultAsync();
        return hit;
    }
}
