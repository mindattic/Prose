using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --set-node-slug --slug &lt;current-slug-or-id&gt; --to &lt;new-slug&gt; [--apply] [--json]</c>
///
/// Give a node the slug you want, and move every slug-carrying reference with it.
///
/// <para>Until now there was no supported way to name a node. <c>update_book</c> has no slug
/// parameter, and <c>--repair-slugs</c> derives the slug from the Title — so the only route to a
/// chosen slug was to rename the book, which is an editorial act rather than a naming one. A slug
/// is a loose key (the UUIDv7 id is the real key), but it is the key a human types.</para>
///
/// <para>DRY-RUN BY DEFAULT, like <c>--repair-slugs</c>: without <c>--apply</c> it reports the
/// rename and every side effect it would perform. The new slug is <b>pinned</b>, so a later
/// <c>--repair-slugs --apply</c> will not quietly rename it back to match the Title.</para>
///
/// <para>Refuses rather than guesses: a value that is not slug-shaped, or one already taken in the
/// same universe, is an error — not something to silently disambiguate with a numeric suffix.</para>
/// </summary>
public static class SetNodeSlugCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { for (var i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1]; return null; }

        var current = Flag("--slug") ?? Flag("--node");
        var target  = Flag("--to");
        var apply   = args.Contains("--apply");
        var json    = args.Contains("--json");

        if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(target))
        {
            Console.Error.WriteLine("Usage: prose --set-node-slug --slug <current-slug-or-id> --to <new-slug> [--apply] [--json]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var svc = services.GetRequiredService<SlugRepairService>();

        Guid nodeId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var resolved = await NodeRefResolver.ResolveAsync(db, current);
            if (resolved == null) { Console.Error.WriteLine($"[set-node-slug] {NodeRefResolver.NotFoundMessage(current)}"); return 1; }
            nodeId = resolved.Value;
        }

        SlugRepairService.SlugChange change;
        try { change = await svc.SetNodeSlugAsync(nodeId, target, apply); }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"[set-node-slug] REFUSED: {ex.Message}");
            return 1;
        }

        if (json) { Console.WriteLine(JsonSerializer.Serialize(new { applied = apply, change }, new JsonSerializerOptions { WriteIndented = true })); return 0; }

        Console.WriteLine($"[set-node-slug] {(apply ? "APPLIED" : "Dry run — nothing written")}");
        Console.WriteLine($"  {change.Label}");
        Console.WriteLine($"    '{change.OldSlug}' → '{change.NewSlug}'");
        foreach (var fx in change.SideEffects) Console.WriteLine($"      + {fx}");
        if (change.SideEffects.Count == 0) Console.WriteLine("      (no slug-carrying references to move)");
        if (apply) Console.WriteLine("  Pinned: --repair-slugs will no longer regenerate this slug from the Title.");
        else Console.WriteLine("  Re-run with --apply to write it.");
        return 0;
    }
}
