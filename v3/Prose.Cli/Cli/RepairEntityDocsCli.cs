using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --repair-entity-docs [--dry-run]</c>
///
/// Stamps <c>MarkdownFiles.UniverseId</c> on the entity-doc rows that ALREADY exist, by matching
/// each <c>docs/entities/&lt;slug&gt;.md</c> row back to its entity.
///
/// <para><b>Deliberately does not create docs.</b> The obvious implementation — iterate every
/// active entity and call <c>EntityDocService.EnsureEntityDocAsync</c> — is wrong here: there are
/// ~14.5k active entities but only ~900 entity-doc rows, because docs are materialized on demand
/// by clue-gathering when an entity actually appears in a beat. Materializing all of them would
/// grow <c>MarkdownFiles</c> roughly tenfold and expand the keyword/embedding candidate set that
/// DocContextService scans every beat. That is a behaviour change, not a repair. This command only
/// fixes rows that exist.</para>
///
/// <para>New docs continue to be stamped correctly at creation time by
/// <c>EntityDocService.EnsureEntityDocAsync</c>, which reads <c>Entity.UniverseId</c>.</para>
///
/// Exit codes: 0 = ran, 1 = failed.
/// </summary>
public static class RepairEntityDocsCli
{
    private const string EntityDocPrefix = "docs/entities/";

    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var dryRun    = args.Contains("--dry-run");
        var dbFactory = sp.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        await using var db = await dbFactory.CreateDbContextAsync();

        // IgnoreQueryFilters on both sides: the repair spans every universe by definition.
        var docs = await db.MarkdownFiles.IgnoreQueryFilters()
            .Where(m => m.Category == "entity-doc")
            .ToListAsync();

        var entities = await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Select(e => new { e.Slug, e.UniverseId })
            .ToListAsync();

        // Slug is the join key — EntityDocService builds the path as docs/entities/{Slug}.md.
        // Duplicate slugs across universes would be ambiguous; take the first and report the rest.
        var bySlug = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entities)
        {
            if (string.IsNullOrWhiteSpace(e.Slug)) continue;
            if (!bySlug.TryAdd(e.Slug, e.UniverseId) && bySlug[e.Slug] != e.UniverseId)
                ambiguous.Add(e.Slug);
        }

        Console.WriteLine($"[repair-entity-docs] {docs.Count} entity-doc row(s); {bySlug.Count} distinct active entity slug(s).");
        if (ambiguous.Count > 0)
            Console.WriteLine($"[repair-entity-docs] {ambiguous.Count} slug(s) exist in more than one universe — first wins, listed below.");

        int changed = 0, unmatched = 0;
        var unmatchedSamples = new List<string>();

        foreach (var doc in docs)
        {
            var slug = doc.RelativePath.StartsWith(EntityDocPrefix, StringComparison.OrdinalIgnoreCase)
                ? doc.RelativePath[EntityDocPrefix.Length..].Replace(".md", "", StringComparison.OrdinalIgnoreCase)
                : null;

            if (slug == null || !bySlug.TryGetValue(slug, out var universeId))
            {
                unmatched++;
                if (unmatchedSamples.Count < 10) unmatchedSamples.Add(doc.RelativePath);
                continue;
            }

            if (doc.UniverseId == universeId) continue;
            if (!dryRun) doc.UniverseId = universeId;
            changed++;
        }

        if (!dryRun && changed > 0) await db.SaveChangesAsync();

        foreach (var s in ambiguous.Take(10)) Console.WriteLine($"[repair-entity-docs]   ambiguous slug: {s}");
        foreach (var s in unmatchedSamples) Console.WriteLine($"[repair-entity-docs]   unmatched doc:  {s}");
        if (unmatched > unmatchedSamples.Count)
            Console.WriteLine($"[repair-entity-docs]   … and {unmatched - unmatchedSamples.Count} more unmatched");

        // Post-state, so the result is visible rather than asserted.
        var byUniverse = await db.MarkdownFiles.IgnoreQueryFilters()
            .Where(m => m.Category == "entity-doc")
            .GroupBy(m => m.UniverseId)
            .Select(g => new { UniverseId = g.Key, Count = g.Count() })
            .ToListAsync();

        Console.WriteLine("[repair-entity-docs] entity-doc rows by universe:");
        foreach (var r in byUniverse.OrderByDescending(r => r.Count))
            Console.WriteLine($"[repair-entity-docs]   {r.UniverseId} → {r.Count}");

        Console.WriteLine(dryRun
            ? $"[repair-entity-docs] dry run — {changed} row(s) WOULD be stamped, {unmatched} unmatched."
            : $"[repair-entity-docs] {changed} row(s) stamped, {unmatched} unmatched.");
        return 0;
    }
}
