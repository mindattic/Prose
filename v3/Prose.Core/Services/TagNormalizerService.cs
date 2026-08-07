using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Three-pass tag maintenance:
///   1. Add category tag ("person" for people/, "synthetic" for synthetics/)
///   2. Lowercase + deduplicate all tags
///   3. Remove tags whose required keywords are absent from entity text
/// Operates on <c>Records.Json</c>.
/// </summary>
public class TagNormalizerService(IDbContextFactory<ProseDbContext> dbFactory) : DataScanUtility(dbFactory)
{
    public record Options(bool AddCategoryTags = true, bool ValidateKeywords = true);

    // Repos that get an automatic category tag
    private static readonly Dictionary<string, string> RepoCategoryTag =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["people"]     = "person",
            ["synthetics"] = "synthetic",
        };

    // Tags that require at least one keyword to appear somewhere in the entity text.
    // Tags not listed here are kept unconditionally (unknown tags are not removed).
    private static readonly Dictionary<string, string[]> TagKeywords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ai"]           = ["ai", "artificial intelligence", "machine learning", "neural", "algorithm", "language model"],
            ["war"]          = ["war", "battle", "combat", "military", "conflict", "soldier", "armed"],
            ["death"]        = ["death", "kill", "murder", "dead", "corpse", "funeral", "assassin", "fatal"],
            ["love"]         = ["love", "romance", "relationship", "heart", "wedding", "marriage", "affection"],
            ["rain"]         = ["rain", "storm", "weather", "wet", "flood", "cloud", "downpour"],
            ["bar"]          = ["bar", "tavern", "pub", "drink", "alcohol", "beer", "spirits", "nightclub"],
            ["train"]        = ["train", "rail", "subway", "metro", "locomotive", "transit"],
            ["fire"]         = ["fire", "flame", "burn", "arson", "ember", "ignite"],
            ["secret"]       = ["secret", "hidden", "covert", "shadow", "clandestine", "concealed"],
            ["water"]        = ["water", "ocean", "sea", "river", "harbor", "aquatic", "flood", "rain"],
            ["music"]        = ["music", "song", "sound", "beat", "audio", "concert", "band", "instrument"],
            ["drugs"]        = ["drug", "narcotic", "stimulant", "pharmaceutical", "substance", "chemical"],
            ["hacking"]      = ["hack", "exploit", "breach", "intrusion", "code", "cyber", "virus", "malware"],
            ["religion"]     = ["religion", "faith", "worship", "cult", "ritual", "divine", "sacred", "spiritual"],
            ["surgery"]      = ["surgery", "implant", "procedure", "medical", "clinic", "doctor", "augment"],
            ["radiation"]    = ["radiation", "nuclear", "radioactive", "contamination", "fallout"],
            ["underground"]  = ["underground", "tunnel", "sewer", "sub-level", "beneath", "hidden"],
            ["corporate"]    = ["corporate", "corponation", "corp", "executive", "board", "division", "subsidiary"],
            ["surveillance"] = ["surveillance", "monitor", "camera", "track", "observe", "watch", "data"],
            ["genetic"]      = ["genetic", "dna", "gene", "ancestry", "hereditary", "clone", "splice"],
            ["ghost"]        = ["ghost", "consciousness", "digital", "upload", "mind", "persona"],
            ["weapons"]      = ["weapon", "firearm", "gun", "blade", "explosive", "ammo"],
            ["vehicle"]      = ["vehicle", "car", "ship", "aircraft", "transport", "drive", "pilot"],
            ["food"]         = ["food", "eat", "restaurant", "cuisine", "meal", "flavor", "nutrient"],
        };

    public Task<UtilityResult> RunAsync(
        Options? opts = null,
        IProgress<UtilityProgress>? progress = null,
        int parallelism = 4,
        CancellationToken ct = default)
    {
        opts ??= new Options();
        return RunScanAsync(GetFiles(), (file, obj) => Process(file, obj, opts),
                            progress, null, parallelism, ct);
    }

    private int Process(string file, JsonObject obj, Options opts)
    {
        int count = 0;

        // --- 1. Category tag ---
        if (opts.AddCategoryTags)
        {
            var repo = Path.GetFileName(Path.GetDirectoryName(file)) ?? "";
            if (RepoCategoryTag.TryGetValue(repo, out var catTag))
            {
                var tags = GetTagList(obj);
                if (!tags.Contains(catTag, StringComparer.OrdinalIgnoreCase))
                {
                    SetTagList(obj, [catTag, .. tags]);
                    count++;
                }
            }
        }

        // --- 2. Lowercase + deduplicate ---
        {
            var tags = GetTagList(obj);
            var normalized = tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.ToLowerInvariant().Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (!tags.SequenceEqual(normalized, StringComparer.Ordinal))
            {
                SetTagList(obj, normalized);
                count++;
            }
        }

        // --- 3. Keyword validation ---
        if (opts.ValidateKeywords)
        {
            var tags = GetTagList(obj);
            var entityText = CombineText(obj,
                "name", "title", "description", "body", "background",
                "role", "notes", "history", "context").ToLowerInvariant();

            var valid = tags.Where(tag =>
            {
                if (!TagKeywords.TryGetValue(tag, out var required)) return true;
                return required.Any(kw => entityText.Contains(kw));
            }).ToList();

            if (valid.Count < tags.Count)
            {
                SetTagList(obj, valid);
                count += tags.Count - valid.Count;
            }
        }

        return count;
    }

    private static List<string> GetTagList(JsonObject obj)
    {
        if (obj["tags"] is JsonArray arr)
            return arr.Select(n => n?.GetValue<string>() ?? "").Where(s => s.Length > 0).ToList();
        return [];
    }

    private static void SetTagList(JsonObject obj, List<string> tags)
    {
        var arr = new JsonArray();
        foreach (var t in tags) arr.Add(JsonValue.Create(t));
        obj["tags"] = arr;
    }
}
