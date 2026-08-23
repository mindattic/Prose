using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --create-book</c> — create a new empty root node (no beats). The
/// bible-first entry point for a brand-new book; write the bible + beats
/// afterward (UI, <c>--edit-beat</c>, or <c>--write-story</c>).
///
///   --title "..."          Display title. Required.
///   --code &lt;CODE&gt;          Optional short reference code (e.g. SRZR). Upper-cased; must be unique.
///   --kind &lt;k&gt;             Category — "book" (default), "chapter", "vignette"…
///   --description "..."       Optional one-line description.
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
        string? title = null, code = null, description = null, seed = null, previous = null, parent = null;
        string kind = "book";

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--title":    if (i + 1 < args.Length) title    = args[++i]; break;
                case "--code":     if (i + 1 < args.Length) code     = args[++i]; break;
                case "--kind":     if (i + 1 < args.Length) kind     = args[++i]; break;
                case "--description": if (i + 1 < args.Length) description = args[++i]; break;
                case "--logline":  if (i + 1 < args.Length) seed     = args[++i]; break;
                case "--previous": if (i + 1 < args.Length) previous = args[++i]; break;
                case "--parent":   if (i + 1 < args.Length) parent   = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            Console.Error.WriteLine("[create-book] --title is required.");
            Console.Error.WriteLine("Usage: prose --create-book --title \"...\" [--code SRZR] [--kind book] [--description \"...\"] [--logline \"...\"] [--previous <slug|id>] [--parent <slug|id>]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();

        Guid? previousId = await ResolveNodeAsync(dbFactory, previous);
        if (previous != null && previousId == null)
        {
            Console.Error.WriteLine($"[create-book] --previous node not found: {previous}");
            return 1;
        }
        Guid? parentId = await ResolveNodeAsync(dbFactory, parent);
        if (parent != null && parentId == null)
        {
            Console.Error.WriteLine($"[create-book] --parent node not found: {parent}");
            return 1;
        }

        try
        {
            var (id, slug) = await workbench.CreateNodeAsync(
                title!, kind, description, seed, code, previousId, parentId);

            Console.WriteLine($"[create-book] Created node:");
            Console.WriteLine($"   Id:    {id}");
            Console.WriteLine($"   Slug:  {slug}");
            Console.WriteLine($"   Title: {title}");
            Console.WriteLine($"   Code:  {(string.IsNullOrWhiteSpace(code) ? "-" : code!.Trim().ToUpperInvariant())}");
            Console.WriteLine($"   Kind:  {kind}");
            Console.WriteLine($"   URL:   https://localhost:7103/node/{slug}");
            Console.WriteLine($"   Next:  add beats via the UI, prose --edit-beat --insert-after N, or prose --write-story.");
            return 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine($"[create-book] {ex.Message}");
            return 1;
        }
    }

    /// <summary>Resolve a node reference (GUID or slug) to its id. Null input → null.</summary>
    /// <summary>
    /// 2026-08-23: was a 7th private copy of "resolve a node reference" — slug-only (so
    /// <c>--previous BCODA</c>, a NodeCode, failed outright while creating a sequel) and missing
    /// <c>IgnoreQueryFilters()</c> on its slug branch, so a cross-universe parent/previous also
    /// resolved to null. Delegates to <see cref="NodeRefResolver"/>, which accepts slug, NodeCode,
    /// GUID, or a unique GUID prefix, and applies the filter fix on every branch.
    /// </summary>
    private static Task<Guid?> ResolveNodeAsync(
        IDbContextFactory<ProseDbContext> dbFactory, string? slugOrId) =>
        NodeRefResolver.ResolveAsync(dbFactory, slugOrId);
}
