using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Strips wiki markup [[Name|id]] from identity fields (name, title, aliases, etc.).
/// Extracts the display text portion, leaving clean strings. Operates on
/// <c>Records.Json</c>.
/// </summary>
public class FixIdentityCorruptionService(IDbContextFactory<ProseDbContext> dbFactory) : DataScanUtility(dbFactory)
{
    // Matches [[DisplayText|anything]] or [[DisplayText]]
    private static readonly Regex WikiLink = new(@"\[\[([^\]|]+?)(?:\|[^\]]+?)?\]\]", RegexOptions.Compiled);

    // Scalar identity fields to clean
    private static readonly HashSet<string> ScalarFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "title", "term", "codename", "product_name", "brand_name",
        "full_legal_name", "headline",
    };

    // Array identity fields whose string elements should be cleaned
    private static readonly HashSet<string> ArrayFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "aliases", "common_names",
    };

    public Task<UtilityResult> RunAsync(
        IProgress<UtilityProgress>? progress = null,
        int parallelism = 4,
        CancellationToken ct = default,
        bool dryRun = false)
        => RunScanAsync(GetFiles(), (_, obj) => { int c = 0; Clean(obj, ref c); return c; },
                        progress, null, parallelism, ct, dryRun);

    private static void Clean(JsonObject obj, ref int count)
    {
        foreach (var key in ScalarFields)
        {
            if (obj[key]?.GetValueKind() != System.Text.Json.JsonValueKind.String) continue;
            var s = obj[key]!.GetValue<string>();
            var cleaned = WikiLink.Replace(s, m => m.Groups[1].Value);
            if (cleaned != s) { obj[key] = JsonValue.Create(cleaned); count++; }
        }

        foreach (var key in ArrayFields)
        {
            if (obj[key] is not JsonArray arr) continue;
            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i]?.GetValueKind() != System.Text.Json.JsonValueKind.String) continue;
                var s = arr[i]!.GetValue<string>();
                var cleaned = WikiLink.Replace(s, m => m.Groups[1].Value);
                if (cleaned != s) { arr[i] = JsonValue.Create(cleaned); count++; }
            }
        }
    }
}
