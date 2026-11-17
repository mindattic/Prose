using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --universe &lt;u&gt; --retype-entity --id &lt;guid&gt;[,&lt;guid&gt;...] --to &lt;repo-slug&gt; [--apply]
///
/// Moves custom-type entities into another custom repository (PRS-T994) through
/// <see cref="EntityRetypeService"/>. Without --apply it is a dry run that writes nothing.
/// </summary>
public static class RetypeEntityCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var universeSlug = Flag(args, "--universe");
        var idsArg = Flag(args, "--id");
        var toType = Flag(args, "--to")?.Trim();
        var apply = args.Contains("--apply");
        var ids = (idsArg ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null).ToList();

        if (string.IsNullOrWhiteSpace(universeSlug) || string.IsNullOrWhiteSpace(toType) || ids.Count == 0 || ids.Any(g => g == null))
        {
            Console.Error.WriteLine("Usage: prose --universe <u> --retype-entity --id <guid>[,<guid>...] --to <repo-slug> [--apply]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        Guid universeId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            universeId = await db.Universes.AsNoTracking().Where(u => u.Slug == universeSlug).Select(u => u.Id).FirstOrDefaultAsync();
            if (universeId == Guid.Empty)
            {
                Console.Error.WriteLine($"[retype-entity] Unknown universe '{universeSlug}'.");
                return 2;
            }
        }

        var svc = ActivatorUtilities.CreateInstance<EntityRetypeService>(services);
        Console.WriteLine($"[retype-entity] {(apply ? "APPLY" : "DRY RUN")}: {ids.Count} entit{(ids.Count == 1 ? "y" : "ies")} -> '{toType}' in '{universeSlug}'.");
        var failures = 0;
        foreach (var id in ids.Select(g => g!.Value))
        {
            if (!apply)
            {
                var plan = await svc.PlanAsync(universeId, id, toType);
                Print(plan);
                if (!plan.Ok) failures++;
                continue;
            }
            var r = await svc.ApplyAsync(universeId, id, toType);
            Print(r.Plan);
            if (!r.Plan.Ok) { failures++; continue; }
            Console.WriteLine($"    read back: entity type={r.EntityTypeBack ?? "(none)"}, record type={r.RecordTypeBack ?? "(no record)"}, " +
                              $"tags still on another repo={r.TagsStillOld} -> {(r.Ok ? "OK" : "MISMATCH")}");
            if (!r.Ok) failures++;
        }
        Console.WriteLine(failures == 0
            ? $"[retype-entity] Done{(apply ? "" : " (dry run, nothing written; pass --apply)")}."
            : $"[retype-entity] {failures} entit{(failures == 1 ? "y" : "ies")} refused or failed read-back.");
        return failures == 0 ? 0 : 1;
    }

    private static void Print(EntityRetypePlan p)
    {
        if (!p.Ok)
        {
            Console.WriteLine($"  {p.EntityId}  {(p.Name.Length > 0 ? p.Name : "?")}: REFUSED ({p.Error})");
            return;
        }
        Console.WriteLine($"  {p.EntityId}  {p.Name}: {p.FromType} -> {p.ToType}; {p.Beats.Count} beat(s), {p.TagCount} tag(s)");
        if (p.Books.Count > 0) Console.WriteLine($"    books: {string.Join("; ", p.Books)}");
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
