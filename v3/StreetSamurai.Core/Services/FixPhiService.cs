using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Replaces word-form "Phi"/"phi" currency references with "Quanta"/"quanta"
/// in all entity prose. The Φ symbol is never touched. Operates on
/// <c>Records.Json</c>.
/// </summary>
public class FixPhiService(IDbContextFactory<StreetSamuraiDbContext> dbFactory) : DataScanUtility(dbFactory)
{
    private static readonly Regex PhiUpper = new(@"\bPhi\b", RegexOptions.Compiled);
    private static readonly Regex PhiLower = new(@"\bphi\b", RegexOptions.Compiled);

    private static readonly HashSet<string> SkipKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "name", "title", "term", "aliases", "common_names", "codename",
        "product_name", "brand_name", "type", "status", "slug", "route",
        "image_prompt", "dalle3_prompt", "created_at", "updated_at",
    };

    public Task<UtilityResult> RunAsync(
        IProgress<UtilityProgress>? progress = null,
        int parallelism = 4,
        CancellationToken ct = default)
        => RunScanAsync(GetFiles(), (_, obj) => { int c = 0; WalkObj(obj, ref c); return c; },
                        progress, null, parallelism, ct);

    private static void WalkObj(JsonObject obj, ref int count)
    {
        foreach (var key in obj.Select(p => p.Key).ToList())
        {
            if (SkipKeys.Contains(key)) continue;
            var child = obj[key];
            if (child == null) continue;

            if (child.GetValueKind() == System.Text.Json.JsonValueKind.String)
            {
                var s = child.GetValue<string>();
                var r = PhiLower.Replace(PhiUpper.Replace(s, "Quanta"), "quanta");
                if (r != s) { obj[key] = JsonValue.Create(r); count++; }
            }
            else if (child is JsonObject nested) WalkObj(nested, ref count);
            else if (child is JsonArray arr)      WalkArr(arr,    ref count);
        }
    }

    private static void WalkArr(JsonArray arr, ref int count)
    {
        for (int i = 0; i < arr.Count; i++)
        {
            var item = arr[i];
            if (item == null) continue;
            if (item.GetValueKind() == System.Text.Json.JsonValueKind.String)
            {
                var s = item.GetValue<string>();
                var r = PhiLower.Replace(PhiUpper.Replace(s, "Quanta"), "quanta");
                if (r != s) { arr[i] = JsonValue.Create(r); count++; }
            }
            else if (item is JsonObject obj) WalkObj(obj, ref count);
            else if (item is JsonArray arr2) WalkArr(arr2, ref count);
        }
    }
}
