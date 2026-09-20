using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --retype-document-to-vocabulary --id &lt;entityGuid&gt; --universe &lt;slug&gt;
///     --term "&lt;term&gt;" --definition "&lt;text&gt;" [--origin "&lt;text&gt;"] [--usage "&lt;text&gt;"]
///     [--category "&lt;text&gt;"] [--example "&lt;text&gt;"] [--dry-run]
///
/// One-off recategorization tool for the class of bug found 2026-09-10: a real, populated
/// `document`-type Entity whose canonical Name is a generic in-world common noun (e.g.
/// "CorpoNation") gets auto-tagged by EntityMentionScanner on every plain use of that word —
/// the same mechanism that deliberately tags a `vocabulary` entity (e.g. "cut", "still standing")
/// everywhere it appears. EntityType has NO effect on that tagging behavior (it's driven purely
/// by name-substring matching), so this tool changes nothing about how the entity is tagged in
/// prose — it only corrects the taxonomy to match what the entity conceptually is: an in-world
/// term, not a worldbuilding document.
///
/// Schema is table-per-type (see Entity.cs), so this is not a single-column UPDATE: it removes
/// the old Document subtype row, flips Entities.EntityType, and inserts a new VocabularyEntries
/// row via VocabularyMapper.PersistAsync. The two schemas don't map field-for-field, so the new
/// Vocabulary content (Term/Definition/Origin/Usage/Category/Example) is supplied by the caller,
/// not auto-derived from Document.Body.
///
/// Also rewrites the entity's Records.Json blob to a VocabularyData-shaped JSON. The usual
/// "Records.Json is additive/untouched" convention (see VocabularyMapper, RebuildAllAsync)
/// protects a blob that is still the correct shape for its entity's type — that doesn't apply
/// here, since the type itself just changed and the prior Document-shaped blob is now stale.
///
/// Only ever touches the ONE entity named by --id. Does not touch Edge rows — Edge.SourceId/
/// TargetId -&gt; Entity.Id stays valid across a type change — so an already-populated entity's
/// real relationships (e.g. CorpoNation's 17 edges) are preserved untouched.
/// </summary>
public static class RetypeDocumentToVocabularyCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var idArg = Flag(args, "--id");
        var universeSlug = Flag(args, "--universe");
        var term = Flag(args, "--term");
        var definition = Flag(args, "--definition");
        var origin = Flag(args, "--origin") ?? "";
        var usage = Flag(args, "--usage") ?? "";
        var category = Flag(args, "--category") ?? "";
        var example = Flag(args, "--example") ?? "";
        var dryRun = args.Contains("--dry-run");

        if (!Guid.TryParse(idArg, out var id) || string.IsNullOrWhiteSpace(universeSlug) || string.IsNullOrWhiteSpace(definition))
        {
            Console.Error.WriteLine("Usage: prose --retype-document-to-vocabulary --id <entityGuid> --universe <slug> --term \"<term>\" --definition \"<text>\" [--origin \"<text>\"] [--usage \"<text>\"] [--category \"<text>\"] [--example \"<text>\"] [--dry-run]");
            return 2;
        }

        var canonDocs = services.GetRequiredService<CanonDocumentService>();
        var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
        if (universeId == null)
        {
            Console.Error.WriteLine($"[retype-document-to-vocabulary] Unknown universe '{universeSlug}'.");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var entity = await db.Entities.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id && e.UniverseId == universeId.Value);
        if (entity == null)
        {
            Console.Error.WriteLine($"[retype-document-to-vocabulary] No entity {id} in universe '{universeSlug}'.");
            return 1;
        }
        if (entity.EntityType != "document")
        {
            Console.Error.WriteLine($"[retype-document-to-vocabulary] Entity '{entity.Name}' ({id}) is EntityType '{entity.EntityType}', not 'document'. Aborting.");
            return 1;
        }

        var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null)
        {
            Console.Error.WriteLine($"[retype-document-to-vocabulary] Entity '{entity.Name}' ({id}) has EntityType 'document' but no Documents row — that's a different bug (see BackfillMissingSubtypeRowsCli), not this tool's job. Aborting.");
            return 1;
        }

        var resolvedTerm = string.IsNullOrWhiteSpace(term) ? entity.Name : term;

        var vocab = new VocabularyData
        {
            Id = id.ToString("N"),
            Term = resolvedTerm,
            Definition = definition,
            Origin = origin,
            Usage = usage,
            Category = category,
            Example = example,
        };

        Console.WriteLine($"[retype-document-to-vocabulary] {(dryRun ? "Would retype" : "Retyping")} '{entity.Name}' ({id}) document -> vocabulary in '{universeSlug}'.");
        Console.WriteLine($"  Document.Title:        {doc.Title}");
        Console.WriteLine($"  New Vocabulary.Term:       {vocab.Term}");
        Console.WriteLine($"  New Vocabulary.Definition: {vocab.Definition}");
        Console.WriteLine($"  New Vocabulary.Origin:     {vocab.Origin}");
        Console.WriteLine($"  New Vocabulary.Category:   {vocab.Category}");

        if (dryRun)
        {
            Console.WriteLine("[retype-document-to-vocabulary] Dry run — no changes written.");
            return 0;
        }

        await using var tx = await db.Database.BeginTransactionAsync();

        db.Documents.Remove(doc);
        entity.EntityType = "vocabulary";
        entity.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await VocabularyMapper.PersistAsync(db, id, vocab);
        await db.SaveChangesAsync();

        var record = await db.Records.FirstOrDefaultAsync(r => r.EntityId == id);
        var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var newJson = JsonSerializer.Serialize(vocab, jsonOpts);
        if (record != null)
        {
            record.Json = newJson;
            record.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.Records.Add(new Record { EntityId = id, Json = newJson, UpdatedAt = DateTime.UtcNow });
        }
        await db.SaveChangesAsync();

        await tx.CommitAsync();

        Console.WriteLine($"[retype-document-to-vocabulary] Done. '{entity.Name}' ({id}) is now EntityType 'vocabulary'.");
        Console.WriteLine("Recoverable via the Entities_History/Documents_History/VocabularyEntries_History temporal tables if this was a mistake.");
        return 0;
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
