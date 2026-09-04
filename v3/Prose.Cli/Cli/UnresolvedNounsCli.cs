using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --unresolved-nouns (--slug &lt;slug|code|guid&gt; | --all) [--min N] [--limit N] [--json]</c>
///
/// <para>Report-only: every capitalized phrase in a book's LIVE BEAT PROSE that resolves to no
/// Entity row, with the beats it appears in. Deterministic, zero LLM calls, writes nothing.</para>
///
/// <para>Added 2026-09-04 to close a real measurement gap. The residue detector
/// <see cref="EntityMentionScanner.FindUnresolvedProperNouns"/> already existed and was already
/// trusted, but both of its callers ran it against OUTLINE text only —
/// <c>CanonDocumentService.SetNodeOutlineSectionAsync</c> (hand-authored sections) and
/// <c>NodeDocService.GenerateAsync</c> (the generated Event Sequence). Nothing ran it against
/// <c>Beats.Text</c>, so "is every named thing in this book actually an entity?" had no answer:
/// <c>--tag-entities</c> applies tags for what it CAN resolve and silently discards the rest
/// without reporting it. A question you cannot measure reads exactly like a question with a
/// clean answer — the same trap that let 99.9% of the story ledger look clean while it was
/// merely unanchored.</para>
///
/// <para>Recall, not precision, by design (see FindUnresolvedProperNouns' own doc): a one-off
/// descriptive phrase that legitimately isn't an entity still surfaces here for a human to
/// dismiss. Sort by frequency and work down — a name appearing in twenty beats is far more
/// likely to be an unseeded entity than a hapax.</para>
/// </summary>
public static class UnresolvedNounsCli
{
    private sealed record Row(string Name, int Beats, List<int> Sample);

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        int min = 1, limit = 200;
        var all = args.Contains("--all");
        var json = args.Contains("--json");
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":
                case "--id":    if (i + 1 < args.Length) slug = args[++i]; break;
                case "--min":   if (i + 1 < args.Length && int.TryParse(args[i + 1], out var m)) { min = m; i++; } break;
                case "--limit": if (i + 1 < args.Length && int.TryParse(args[i + 1], out var l)) { limit = l; i++; } break;
            }
        }

        if (!all && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[unresolved-nouns] --slug <slug|code|guid> or --all is required.");
            Console.Error.WriteLine("Usage: prose --unresolved-nouns (--slug <s> | --all) [--min N] [--limit N] [--json]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        List<Guid> nodeIds;
        if (all)
        {
            nodeIds = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                .Where(n => n.Kind == "book").OrderBy(n => n.Title).Select(n => n.Id).ToListAsync();
        }
        else
        {
            var resolved = await NodeRefResolver.ResolveAsync(db, slug);
            if (resolved == null)
            {
                Console.Error.WriteLine($"[unresolved-nouns] {NodeRefResolver.NotFoundMessage(slug)}");
                return 1;
            }
            nodeIds = [resolved.Value];
        }

        int exit = 0;
        foreach (var nodeId in nodeIds)
        {
            // IgnoreQueryFilters(): explicit ref, not ambient scope (2026-08-17 convention).
            var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId);
            if (node == null) continue;

            // Reading order, same walk every other book-wide reader uses — a flat
            // ParentNodeId query silently misses anything nested deeper than one level.
            var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id);
            var beats = await db.BeatNodes.AsNoTracking()
                .Where(bn => leafIds.Contains(bn.NodeId))
                .OrderBy(bn => bn.SortKey)
                .Join(db.Beats.AsNoTracking(), bn => bn.BeatId, b => b.Id, (bn, b) => new { b.Number, b.Text })
                .ToListAsync();
            if (beats.Count == 0) continue;

            var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, node.UniverseId, node.Id);

            var beatsByName = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in beats)
            {
                if (string.IsNullOrWhiteSpace(b.Text)) continue;
                var plain = BeatMarkup.StripEntityTags(b.Text);
                var matches = EntityMentionScanner.Scan(plain, candidates);
                foreach (var name in EntityMentionScanner.FindUnresolvedProperNouns(plain, matches))
                {
                    if (!beatsByName.TryGetValue(name, out var list))
                        beatsByName[name] = list = [];
                    list.Add(b.Number);
                }
            }

            var rows = beatsByName
                .Where(kv => kv.Value.Count >= min)
                .OrderByDescending(kv => kv.Value.Count).ThenBy(kv => kv.Key)
                .Take(limit)
                .Select(kv => new Row(kv.Key, kv.Value.Count, kv.Value.Take(5).ToList()))
                .ToList();

            if (json)
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
                {
                    slug = node.Slug, code = node.NodeCode, title = node.Title,
                    beats_scanned = beats.Count, distinct_unresolved = beatsByName.Count, rows,
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.WriteLine($"{node.Title}  [{node.Slug}]");
                Console.WriteLine($"  {beats.Count} beat(s) scanned, {candidates.Count} candidate name(s)/alias(es), " +
                                  $"{beatsByName.Count} distinct unresolved proper noun(s) (showing {rows.Count}, --min {min}).");
                if (rows.Count == 0) Console.WriteLine("  none — every capitalized phrase resolves to an entity.");
                foreach (var r in rows)
                    Console.WriteLine($"  {r.Beats,4}x  {r.Name,-38}  e.g. beats {string.Join(", ", r.Sample.Select(n => "#" + n))}");
                Console.WriteLine();
            }

            if (rows.Count > 0) exit = 1;   // informational, mirrors --duplicate-entity-scan
        }

        return exit;
    }
}
