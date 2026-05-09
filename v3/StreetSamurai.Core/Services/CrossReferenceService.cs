using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Populates related_entities[] arrays by scanning entity prose for mentions
/// of other entities (by name or alias). Uses the XrefService index so no
/// separate index build is needed. Operates on <c>Records.Json</c>.
/// </summary>
public class CrossReferenceService(XrefService xref, IDbContextFactory<StreetSamuraiDbContext> dbFactory) : DataScanUtility(dbFactory)
{
    private static readonly HashSet<string> ProseKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "description", "body", "background", "personality", "ideology",
        "founding_story", "key_detail", "cultural_context", "lore",
        "notes", "history", "functionality", "common_usage",
        "role", "context", "affiliation", "overview",
    };

    public Task<UtilityResult> RunAsync(
        bool clearFirst = true,
        IProgress<UtilityProgress>? progress = null,
        int parallelism = 4,
        CancellationToken ct = default)
    {
        xref.EnsureBuilt();
        var nameIndex = xref.GetNameIndex();
        var regex = BuildRegex(nameIndex);
        if (regex == null) return Task.FromResult(new UtilityResult(0, 0, 0));

        return RunScanAsync(
            GetFiles(),
            (_, obj) => Process(obj, nameIndex, regex, clearFirst),
            progress, null, parallelism, ct);
    }

    private static int Process(
        JsonObject obj,
        IReadOnlyDictionary<string, XrefEntry> nameIndex,
        Regex regex,
        bool clearFirst)
    {
        var entityId = obj["id"]?.GetValue<string>() ?? "";

        // Collect prose from known fields
        var prose = new System.Text.StringBuilder();
        foreach (var key in ProseKeys)
        {
            if (obj[key]?.GetValueKind() == System.Text.Json.JsonValueKind.String)
                prose.Append(' ').Append(obj[key]!.GetValue<string>());
        }

        var text = prose.ToString();
        if (text.Length < 20) return 0;

        // Find all referenced entity IDs
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (Match m in regex.Matches(text))
            {
                if (!nameIndex.TryGetValue(m.Value, out var entry)) continue;
                if (entry.Id == entityId) continue; // no self-reference
                found.Add(entry.Id);
            }
        }
        catch (RegexMatchTimeoutException) { return 0; }

        if (found.Count == 0 && !clearFirst) return 0;

        // Read existing related_entities
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!clearFirst && obj["related_entities"] is JsonArray existingArr)
            foreach (var n in existingArr)
                if (n?.GetValue<string>() is { Length: > 0 } id) existing.Add(id);

        var merged = clearFirst ? found : new HashSet<string>(existing.Union(found));

        if (!clearFirst && merged.SetEquals(existing)) return 0;

        var arr = new JsonArray();
        foreach (var id in merged.OrderBy(x => x)) arr.Add(JsonValue.Create(id));
        obj["related_entities"] = arr;
        return found.Count;
    }

    private static Regex? BuildRegex(IReadOnlyDictionary<string, XrefEntry> nameIndex)
    {
        var patterns = nameIndex.Keys
            .Where(n => n.Length >= 4)
            .OrderByDescending(n => n.Length)
            .Select(Regex.Escape)
            .ToArray();

        if (patterns.Length == 0) return null;

        return new Regex(
            $@"(?<![a-zA-Z0-9\-\[])({string.Join("|", patterns)})(?![a-zA-Z0-9\-\]])",
            RegexOptions.Compiled, TimeSpan.FromSeconds(5));
    }
}
