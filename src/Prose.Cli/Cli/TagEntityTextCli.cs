using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --universe &lt;u&gt; --tag-entity-text [--type T] [--id G] [--limit N] [--out tagged.json] [--apply]
///
/// Tags every entity's own prose (its summary and the long text fields of its record) with
/// <c>&lt;entity repo guid&gt;</c> markup, using the same <see cref="EntityMentionScanner"/> index and
/// rules that tag beats, so an entity links to the entities its text names the way a beat does
/// (PRS-T1003). Existing tags are stripped first, so a re-run converges instead of nesting.
///
/// <para>Dry run by default: nothing is written; with <c>--out</c> the tagged text is saved as
/// <c>{guid: {"summary": …, "record": {key: value}}}</c> for consumers such as tools/wiki/prose2wiki.py.
/// <c>--apply</c> writes through <see cref="EntityFieldWriter"/> WITHOUT confirmUnread, so a write that
/// would un-read beats is refused by the read gate and only counted; those entities need an author
/// decision about the gate before they can be tagged in place.</para>
/// </summary>
public static class TagEntityTextCli
{
    static readonly HashSet<string> SkipKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "type", "name", "title", "term", "headline", "codename", "file_name", "aliases", "common_names", "tags",
        "image_prompt", "dalle3_prompt", "changelog", "provenance", "status", "pronouns", "gender", "species",
        "rating", "vote_count", "location", "story_id", "category", "subcategory", "tier", "legality",
    };

    const int MinLength = 25;
    const int CommonWordUses = 3;
    static readonly System.Text.RegularExpressions.Regex LowerWord = new(@"(?<![\p{L}'-])\p{Ll}[\p{Ll}'-]*(?![\p{L}])");
    static readonly System.Text.RegularExpressions.Regex MidCapital = new(@"(?<=[\p{Ll}\p{N},;]\s+)\p{Lu}[\p{L}'-]*(?![\p{L}])");

    static IEnumerable<string> Strings(JsonNode? node) => node switch
    {
        JsonValue v when v.TryGetValue<string>(out var s) => [s],
        JsonArray a => a.SelectMany(Strings),
        JsonObject o => o.Where(p => !SkipKeys.Contains(p.Key)).SelectMany(p => Strings(p.Value)),
        _ => [],
    };

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var type = Flag(args, "--type");
        var id = Flag(args, "--id");
        var outPath = Flag(args, "--out");
        var apply = args.Contains("--apply");
        var limit = int.TryParse(Flag(args, "--limit"), out var l) ? l : int.MaxValue;
        var universeId = UniverseScope.EffectiveId;
        if (universeId == Guid.Empty)
        {
            Console.Error.WriteLine("[tag-entity-text] pass --universe <slug>.");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var writer = services.GetRequiredService<EntityFieldWriter>();
        List<(Guid Id, string Type, string Name)> targets;
        List<EntityMentionScanner.MentionCandidate> candidates;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, null);
            var q = db.Set<Entity>().AsNoTracking().IgnoreQueryFilters()
                .Where(e => e.UniverseId == universeId && e.Status != "archived");
            if (!string.IsNullOrWhiteSpace(type)) q = q.Where(e => e.EntityType == type);
            if (Guid.TryParse(id, out var one)) q = q.Where(e => e.Id == one);
            targets = (await q.OrderBy(e => e.EntityType).ThenBy(e => e.Name)
                    .Select(e => new { e.Id, e.EntityType, e.Name }).ToListAsync())
                .Take(limit).Select(e => (e.Id, e.EntityType ?? "", e.Name ?? "")).ToList();
        }
        var reads = new List<(Guid Id, string Type, string Name, FieldReadResult Read)>();
        var unreadable = 0;
        foreach (var (entityId, entityType, name) in targets)
        {
            var read = await writer.GetFieldsAsync(entityId.ToString());
            if (read.Ok) reads.Add((entityId, entityType, name, read));
            else unreadable++;
        }

        // Entity text has no per-book incidental rulings, so a one-word name that is also an ordinary word
        // ("Park", "Hands", "Origin", an alias "Industries") would tag every sentence that starts with it.
        // The corpus decides: a one-word name the universe's own entity text also uses lowercase is a word.
        // Mid-sentence capitals can only be names ("asked Bear", "with Silence"), sentence-initial ones can be either, so
        // a word whose lowercase uses outnumber its mid-sentence capitalized uses is an ordinary word.
        var lowercase = new Dictionary<string, int>(StringComparer.Ordinal);
        var midCapital = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var r in reads)
            foreach (var s in Strings(r.Read.Record).Append(r.Read.Summary ?? ""))
            {
                var plain = BeatMarkup.StripEntityTags(s);
                foreach (System.Text.RegularExpressions.Match m in LowerWord.Matches(plain))
                    lowercase[m.Value] = lowercase.GetValueOrDefault(m.Value) + 1;
                foreach (System.Text.RegularExpressions.Match m in MidCapital.Matches(plain))
                    midCapital[m.Value] = midCapital.GetValueOrDefault(m.Value) + 1;
            }
        // A one-word name that is also an ordinary word ("Faith", "Hands", "Transit") is only a name where nothing
        // else explains the capital: mid-sentence, with lowercase words on both sides. Sentence starts and longer
        // proper nouns ("Ferrogate Transit") are left untagged.
        var wordLike = candidates
            .Where(c => !c.Text.Contains(' ') && c.Text.Length > 0 && char.IsUpper(c.Text[0])
                        && lowercase.GetValueOrDefault(c.Text.ToLowerInvariant()) >= CommonWordUses)
            .Select(c => c.Text).ToHashSet(StringComparer.Ordinal);
        var ordinary = wordLike.Where(w => lowercase.GetValueOrDefault(w.ToLowerInvariant()) > midCapital.GetValueOrDefault(w))
            .OrderBy(w => w).ToList();
        Console.WriteLine($"[tag-entity-text] {reads.Count} entities, {candidates.Count} name/alias candidates, " +
                          $"{wordLike.Count} one-word names that are also ordinary words (tagged mid-sentence only)" +
                          $"{(apply ? "" : " (dry run)")}.");

        var index = new Index(candidates, wordLike);
        var output = new JsonObject
        {
            ["_wordLikeNames"] = new JsonArray(ordinary.Select(w => (JsonNode)JsonValue.Create(w)!).ToArray()),
        };
        int changedEntities = 0, totalTags = 0, written = 0, gated = 0, failed = 0;
        var gatedBeats = 0;
        foreach (var (entityId, entityType, name, read) in reads)
        {
            var tags = 0;
            var patch = new JsonObject();
            var summary = Tag(read.Summary, entityId, index, ref tags);
            var summaryChanged = summary != null && summary != read.Summary;
            if (read.Record is JsonObject record)
            {
                foreach (var (key, value) in record)
                {
                    if (value == null || SkipKeys.Contains(key)) continue;
                    var before = value.ToJsonString();
                    var after = TagNode(value.DeepClone(), key, entityId, index, ref tags);
                    if (after.ToJsonString() != before) patch[key] = after;
                }
            }
            if (!summaryChanged && patch.Count == 0) continue;
            changedEntities++;
            totalTags += tags;
            output[entityId.ToString()] = new JsonObject
            {
                ["type"] = entityType,
                ["name"] = name,
                ["summary"] = summaryChanged ? summary : null,
                ["record"] = patch.DeepClone(),
            };
            if (!apply) continue;

            var fields = (JsonObject)patch.DeepClone();
            if (summaryChanged) fields[EntityFieldWriter.SummaryKey] = summary;
            var r = await writer.SetFieldsAsync(entityId.ToString(), fields.ToJsonString());
            if (r.Ok) written++;
            else if (r.UnreadCost > 0) { gated++; gatedBeats += r.UnreadCost; }
            else { failed++; Console.Error.WriteLine($"[tag-entity-text] {entityType} '{name}' ({entityId}): {r.Error}"); }
        }

        if (!string.IsNullOrWhiteSpace(outPath))
        {
            await File.WriteAllTextAsync(outPath, output.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"[tag-entity-text] wrote {outPath}");
        }
        Console.WriteLine($"[tag-entity-text] {changedEntities} entities gain {totalTags} tags" +
                          (apply ? $"; written {written}, refused by the read gate {gated} (would un-read {gatedBeats} beat mentions), failed {failed}" : "") +
                          (unreadable > 0 ? $"; {unreadable} unreadable" : "") + ".");
        return failed > 0 ? 1 : 0;
    }

    static JsonNode TagNode(JsonNode node, string key, Guid self, Index index, ref int tags)
    {
        switch (node)
        {
            case JsonValue v when v.TryGetValue<string>(out var s):
                return JsonValue.Create(Tag(s, self, index, ref tags) ?? s)!;
            case JsonArray a:
                for (var i = 0; i < a.Count; i++)
                    if (a[i] != null) a[i] = TagNode(a[i]!.DeepClone(), key, self, index, ref tags);
                return a;
            case JsonObject o:
                foreach (var k in o.Select(p => p.Key).ToList())
                    if (o[k] != null && !SkipKeys.Contains(k)) o[k] = TagNode(o[k]!.DeepClone(), k, self, index, ref tags);
                return o;
            default:
                return node;
        }
    }

    /// <summary>Re-tags one string from scratch; returns null for text too short to be prose.</summary>
    static string? Tag(string? text, Guid self, Index index, ref int tags)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var plain = BeatMarkup.StripEntityTags(text);
        if (plain.Length < MinLength || !plain.Contains(' ')) return text;
        var matches = EntityMentionScanner.Scan(plain, index.Candidates)
            .Where(m => m.EntityId != self)
            .Where(m => !index.WordLike.Contains(plain.Substring(m.Start, m.Length)) || StandsAlone(plain, m.Start, m.Length))
            .ToList();
        tags += matches.Count;
        return EntityMentionScanner.ApplyTags(plain, matches);
    }

    sealed record Index(List<EntityMentionScanner.MentionCandidate> Candidates, HashSet<string> WordLike);

    /// <summary>Mid-sentence with lowercase on both sides: "asked Faith quietly", not "Faith held." or "Ferrogate Transit".</summary>
    static bool StandsAlone(string text, int start, int length)
    {
        var i = start - 1;
        while (i >= 0 && text[i] == ' ') i--;
        if (i < 0 || !(char.IsLower(text[i]) || char.IsDigit(text[i]) || text[i] is ',' or ';')) return false;
        var w = i;
        while (w > 0 && (char.IsLetterOrDigit(text[w - 1]) || text[w - 1] is '\'' or '-')) w--;
        if (char.IsUpper(text[w])) return false;
        var j = start + length;
        if (j < text.Length && (char.IsLetter(text[j]) || text[j] == '-')) return false;
        while (j < text.Length && text[j] == ' ') j++;
        return j >= text.Length || !char.IsUpper(text[j]);
    }

    static string? Flag(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
