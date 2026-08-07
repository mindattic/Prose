using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --generate-glossary --universe &lt;glmz|scry&gt;</c> — regenerate that universe's
/// Master Glossary (Glossary.htm/.json/.txt under docs/universes/{SLUG}/) from the
/// GlossaryTerms table.
///
/// <c>ss --generate-book-glossary --slug &lt;slug&gt; [--all]</c> — regenerate the per-book
/// glossary (docs/nodes/{CODE}-Glossary.htm/.json/.txt) — the subset of that book's universe
/// glossary whose terms actually appear in its live prose.
/// </summary>
public static class GlossaryCli
{
    public static async Task<int> RunMasterAsync(string[] args, IServiceProvider services)
    {
        string? universeSlug = null;
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--universe" && i + 1 < args.Length) universeSlug = args[++i];

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var glossary = services.GetRequiredService<GlossaryService>();

        await using var db = await dbFactory.CreateDbContextAsync();
        var universes = string.IsNullOrWhiteSpace(universeSlug)
            ? await db.Universes.IgnoreQueryFilters().Where(u => u.Id != Universe.SharedId).ToListAsync()
            : await db.Universes.IgnoreQueryFilters()
                .Where(u => u.Slug == universeSlug.ToLowerInvariant()).ToListAsync();

        if (universes.Count == 0)
        {
            Console.Error.WriteLine($"[generate-glossary] No universe found for '{universeSlug}'.");
            Console.Error.WriteLine("Usage: ss --generate-glossary --universe <slug>   (omit --universe for all)");
            return 2;
        }

        foreach (var u in universes)
        {
            var result = await glossary.GenerateMasterAsync(u.Id);
            Console.WriteLine($"[generate-glossary] {u.Name} — {result.TermCount} terms");
            Console.WriteLine($"  {result.HtmlPath}");
            Console.WriteLine($"  {result.JsonPath}");
            Console.WriteLine($"  {result.TxtPath}");
        }
        return 0;
    }

    public static async Task<int> RunBookAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool all = args.Contains("--all");
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[++i];

        if (!all && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[generate-book-glossary] --slug <slug> or --all is required.");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var glossary = services.GetRequiredService<GlossaryService>();

        await using var db = await dbFactory.CreateDbContextAsync();
        // No IgnoreQueryFilters here — --all must stay scoped to the active --universe (the
        // ambient ScopedUniverseId filter), matching every other node query in this codebase.
        // Bypassing it would process every book across every universe in one call.
        var nodes = all
            ? await db.Nodes
                .Where(n => n.NodeCode != null && n is BookNode)
                .OrderBy(n => n.NodeCode)
                .Select(n => new { n.Id, n.NodeCode, n.Title })
                .ToListAsync()
            : await db.Nodes
                .Where(n => n.Slug == slug || n.NodeCode == slug)
                .Select(n => new { n.Id, n.NodeCode, n.Title })
                .ToListAsync();

        if (nodes.Count == 0)
        {
            Console.Error.WriteLine($"[generate-book-glossary] No book found for '{slug}'.");
            return 1;
        }

        int ok = 0, fail = 0;
        foreach (var n in nodes)
        {
            try
            {
                var result = await glossary.GenerateForBookAsync(n.Id);
                Console.WriteLine($"  ✓ {n.NodeCode,-8} {n.Title} — {result.TermCount}/{result.UniverseTermCount} universe terms used");
                ok++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  ✗ {n.NodeCode,-8} {n.Title} — {ex.Message}");
                fail++;
            }
        }
        Console.WriteLine($"[generate-book-glossary] Done: {ok} succeeded, {fail} failed.");
        return fail > 0 ? 1 : 0;
    }
}
