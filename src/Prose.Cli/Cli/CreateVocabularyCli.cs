using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --create-vocabulary --term "&lt;term&gt;" [--definition …] [--origin …] [--usage …]
/// [--category …] [--example …] [--tier …] [--tags a,b,c]</c>
///
/// <para>Create or update a vocabulary entry — the CLI twin of MCP <c>create_vocabulary</c>.
/// Vocabulary is the entity type the prose tags as <c>&lt;entity repo="vocabulary"&gt;</c>, and it
/// had the same gap materials did: a repository that could write it
/// (<c>VocabularyRepository.Save</c>), read surfaces that could show it, and no write path of any
/// kind.</para>
///
/// <para>Omitted scalars are LEFT UNCHANGED, the same contract as <c>--create-material</c> and
/// <c>CreateWeapon</c> — never <c>create_character</c>'s blank-what-you-didn't-mention behaviour.
/// Tags replace by default; <c>[]</c> clears them and <c>--append-tags</c> merges.</para>
///
/// <para>Note the two different surfaces a word can live on, because they are not the same thing
/// and a term usually wants both: a <b>vocabulary entity</b> gives the word a GUID so prose can
/// tag it and the entity wiki can show it, while <c>upsert_glossary_term</c> puts a reader-facing
/// definition in the book's back matter. This command does the former only.</para>
/// </summary>
public static class CreateVocabularyCli
{
    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? term = null, definition = null, origin = null, usage = null,
                tier = null, category = null, example = null, tags = null;
        var append = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--term":       if (i + 1 < args.Length) term = args[++i]; break;
                case "--definition": if (i + 1 < args.Length) definition = args[++i]; break;
                case "--origin":     if (i + 1 < args.Length) origin = args[++i]; break;
                case "--usage":      if (i + 1 < args.Length) usage = args[++i]; break;
                case "--tier":       if (i + 1 < args.Length) tier = args[++i]; break;
                case "--category":   if (i + 1 < args.Length) category = args[++i]; break;
                case "--example":    if (i + 1 < args.Length) example = args[++i]; break;
                case "--tags":       if (i + 1 < args.Length) tags = args[++i]; break;
                case "--append-tags": append = true; break;
            }
        }

        if (string.IsNullOrWhiteSpace(term))
        {
            Console.Error.WriteLine("[create-vocabulary] --term \"<term>\" is required.");
            return Task.FromResult(1);
        }

        var repo = services.GetRequiredService<VocabularyRepository>();

        var existing = repo.GetAll().FirstOrDefault(v =>
            string.Equals(v.Term, term, StringComparison.OrdinalIgnoreCase));
        var isNew = existing is null;
        var v2 = existing ?? new VocabularyData { Term = term };

        if (definition is not null) v2.Definition = definition;
        if (origin     is not null) v2.Origin     = origin;
        if (usage      is not null) v2.Usage      = usage;
        if (tier       is not null) v2.Tier       = tier;
        if (category   is not null) v2.Category   = category;
        if (example    is not null) v2.Example    = example;

        if (tags is not null)
        {
            if (tags.Trim() == "[]") v2.Tags = [];
            else
            {
                var parsed = tags.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
                if (!append) v2.Tags = parsed;
                else foreach (var t in parsed)
                    if (!v2.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)) v2.Tags.Add(t);
            }
        }

        repo.Save(v2);

        // Read back rather than trusting the write.
        var after = repo.GetAll().FirstOrDefault(x =>
            string.Equals(x.Term, term, StringComparison.OrdinalIgnoreCase));
        if (after is null)
        {
            Console.Error.WriteLine($"[create-vocabulary] Save reported success but \"{term}\" could not be read back. Nothing was written.");
            return Task.FromResult(1);
        }

        Console.WriteLine($"[create-vocabulary] {(isNew ? "Created" : "Updated")} \"{after.Term}\"" +
                          $"{(string.IsNullOrWhiteSpace(after.Category) ? "" : $" ({after.Category})")} — id {after.Id}");
        if (!string.IsNullOrWhiteSpace(after.Definition)) Console.WriteLine($"  {Clip(after.Definition, 110)}");
        return Task.FromResult(0);
    }

    private static string Clip(string s, int n) => s.Length <= n ? s : s[..n] + "…";
}
