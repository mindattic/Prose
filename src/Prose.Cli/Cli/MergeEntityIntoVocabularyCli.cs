using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --merge-entity-into-vocabulary --from &lt;sourceGuid&gt; --into &lt;targetVocabularyGuid&gt;
///     --universe &lt;slug&gt; --term "&lt;term&gt;" --definition "&lt;text&gt;" [--origin "&lt;text&gt;"]
///     [--usage "&lt;text&gt;"] [--category "&lt;text&gt;"] [--example "&lt;text&gt;"] [--dry-run]
///
/// One-off duplicate-resolution tool for the case found 2026-09-10: two Entity rows describing
/// the same in-world concept under different casing/type (a rich `document` article and a
/// near-empty `vocabulary` stub with the same slug) — the slug collision means the document row
/// can't simply be retyped in place (see RetypeDocumentToVocabularyCli, which this supersedes for
/// this specific case).
///
/// Merges by: (1) writing the supplied Vocabulary fields onto --into (must already be
/// EntityType=vocabulary), (2) redirecting every Edge that points at --from onto --into instead —
/// dropping any edge that would become a self-loop (--into already linked to itself) or an exact
/// duplicate of an edge --into already has, rather than creating either, (3) hard-deleting --from
/// once no edges reference it, via the same EntityDeleteGuard check DeleteEntityClusterCli uses so
/// an unexpected non-Edge blocker aborts the whole transaction instead of forcing through it.
///
/// Only ever touches the two named entities and the Edge rows directly connected to --from.
/// </summary>
public static class MergeEntityIntoVocabularyCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var fromArg = Flag(args, "--from");
        var intoArg = Flag(args, "--into");
        var universeSlug = Flag(args, "--universe");
        var term = Flag(args, "--term");
        var definition = Flag(args, "--definition");
        var origin = Flag(args, "--origin") ?? "";
        var usage = Flag(args, "--usage") ?? "";
        var category = Flag(args, "--category") ?? "";
        var example = Flag(args, "--example") ?? "";
        var dryRun = args.Contains("--dry-run");

        if (!Guid.TryParse(fromArg, out var fromId) || !Guid.TryParse(intoArg, out var intoId) ||
            string.IsNullOrWhiteSpace(universeSlug) || string.IsNullOrWhiteSpace(definition) || fromId == intoId)
        {
            Console.Error.WriteLine("Usage: prose --merge-entity-into-vocabulary --from <sourceGuid> --into <targetVocabularyGuid> --universe <slug> --term \"<term>\" --definition \"<text>\" [--origin \"<text>\"] [--usage \"<text>\"] [--category \"<text>\"] [--example \"<text>\"] [--dry-run]");
            return 2;
        }

        var canonDocs = services.GetRequiredService<CanonDocumentService>();
        var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
        if (universeId == null)
        {
            Console.Error.WriteLine($"[merge-entity-into-vocabulary] Unknown universe '{universeSlug}'.");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var fromEntity = await db.Entities.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == fromId && e.UniverseId == universeId.Value);
        var intoEntity = await db.Entities.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == intoId && e.UniverseId == universeId.Value);
        if (fromEntity == null) { Console.Error.WriteLine($"[merge-entity-into-vocabulary] No --from entity {fromId} in '{universeSlug}'."); return 1; }
        if (intoEntity == null) { Console.Error.WriteLine($"[merge-entity-into-vocabulary] No --into entity {intoId} in '{universeSlug}'."); return 1; }
        if (intoEntity.EntityType != "vocabulary")
        {
            Console.Error.WriteLine($"[merge-entity-into-vocabulary] --into entity '{intoEntity.Name}' ({intoId}) is EntityType '{intoEntity.EntityType}', not 'vocabulary'. Aborting.");
            return 1;
        }

        var resolvedTerm = string.IsNullOrWhiteSpace(term) ? intoEntity.Name : term;
        var vocab = new VocabularyData
        {
            Id = intoId.ToString("N"),
            Term = resolvedTerm,
            Definition = definition,
            Origin = origin,
            Usage = usage,
            Category = category,
            Example = example,
        };

        var allOutgoing = await db.Edges.IgnoreQueryFilters().Where(e => e.SourceId == fromId).ToListAsync();
        var allIncoming = await db.Edges.IgnoreQueryFilters().Where(e => e.TargetId == fromId).ToListAsync();
        // A self-loop on --from (SourceId == TargetId == fromId) satisfies BOTH queries above and,
        // since EF's change tracker returns the SAME instance for the same key on both queries,
        // mutating it in the outgoing loop makes the incoming loop's own check see the mutated
        // SourceId and delete it as a false duplicate — double-counted and silently dropped instead
        // of becoming a clean self-loop on --into. Handle it once, separately, instead.
        var selfLoops = allOutgoing.Where(e => e.TargetId == fromId).ToList();
        var outgoing = allOutgoing.Where(e => e.TargetId != fromId).ToList();
        var incoming = allIncoming.Where(e => e.SourceId != fromId).ToList();
        var intoExistingKeys = (await db.Edges.IgnoreQueryFilters()
                .Where(e => e.SourceId == intoId || e.TargetId == intoId)
                .Select(e => new { e.SourceId, e.TargetId, e.RelationType })
                .ToListAsync())
            .Select(e => (e.SourceId, e.TargetId, e.RelationType))
            .ToHashSet();

        Console.WriteLine($"[merge-entity-into-vocabulary] {(dryRun ? "Would merge" : "Merging")} '{fromEntity.Name}' [{fromEntity.EntityType}] ({fromId}) into '{intoEntity.Name}' [vocabulary] ({intoId}) in '{universeSlug}'.");
        Console.WriteLine($"  New Vocabulary.Term:       {vocab.Term}");
        Console.WriteLine($"  New Vocabulary.Definition: {vocab.Definition}");
        Console.WriteLine($"  Edges to redirect: {outgoing.Count} outgoing, {incoming.Count} incoming, {selfLoops.Count} self-loop(s) (from '{fromEntity.Name}').");

        if (dryRun)
        {
            Console.WriteLine("[merge-entity-into-vocabulary] Dry run — no changes written.");
            return 0;
        }

        await using var tx = await db.Database.BeginTransactionAsync();

        var redirected = 0;
        var dropped = 0;

        foreach (var e in outgoing)
        {
            if (e.TargetId == intoId || intoExistingKeys.Contains((intoId, e.TargetId, e.RelationType)))
            {
                db.Edges.Remove(e);
                dropped++;
                continue;
            }
            e.SourceId = intoId;
            intoExistingKeys.Add((intoId, e.TargetId, e.RelationType));
            redirected++;
        }

        foreach (var e in incoming)
        {
            if (e.SourceId == intoId || intoExistingKeys.Contains((e.SourceId, intoId, e.RelationType)))
            {
                db.Edges.Remove(e);
                dropped++;
                continue;
            }
            e.TargetId = intoId;
            intoExistingKeys.Add((e.SourceId, intoId, e.RelationType));
            redirected++;
        }

        // A self-loop on --from becomes a self-loop on --into (or is dropped if --into already has
        // an identical one) — handled once, explicitly, never touched by the two loops above.
        foreach (var e in selfLoops)
        {
            if (intoExistingKeys.Contains((intoId, intoId, e.RelationType)))
            {
                db.Edges.Remove(e);
                dropped++;
                continue;
            }
            e.SourceId = intoId;
            e.TargetId = intoId;
            intoExistingKeys.Add((intoId, intoId, e.RelationType));
            redirected++;
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"[merge-entity-into-vocabulary] Redirected {redirected} edge(s), dropped {dropped} self-loop/duplicate edge(s).");

        await VocabularyMapper.PersistAsync(db, intoId, vocab);
        await db.SaveChangesAsync();

        var record = await db.Records.FirstOrDefaultAsync(r => r.EntityId == intoId);
        var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var newJson = JsonSerializer.Serialize(vocab, jsonOpts);
        if (record != null) { record.Json = newJson; record.UpdatedAt = DateTime.UtcNow; }
        else db.Records.Add(new Record { EntityId = intoId, Json = newJson, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        await tx.CommitAsync();

        // The rest of --from goes through the real merge: it relinks EVERY table that points at the
        // entity (mentions, tags, properties, embeddings, state events), rewrites the beat tags, and
        // deletes the loser. Deleting it here moved only Edges — the cascading rows were destroyed
        // and the prose kept <entity guid=…> tags pointing at an entity that no longer existed.
        try
        {
            await services.GetRequiredService<DuplicateEntityScanService>().MergeAsync(intoId, fromId);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[merge-entity-into-vocabulary] Vocabulary fields and edges moved, but merging '{fromEntity.Name}' ({fromId}) failed: {ex.Message}. It still exists; re-run the merge after fixing the cause.");
            return 1;
        }

        Console.WriteLine($"[merge-entity-into-vocabulary] Done. '{fromEntity.Name}' ({fromId}) deleted; content and relationships now live on '{intoEntity.Name}' ({intoId}), EntityType 'vocabulary'.");
        Console.WriteLine("Recoverable via the Entities_History/Edges_History/VocabularyEntries_History temporal tables if this was a mistake.");
        return 0;
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
