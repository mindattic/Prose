using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Tags every weapon entity with a lethality classification:
///   lethal | less_lethal | non_lethal
/// Uses category-based defaults then keyword overrides.
/// Skips weapons that already have a lethality tag. Operates on
/// <c>Records.Json</c>.
/// </summary>
public class TagWeaponLethalityService(IDbContextFactory<ProseDbContext> dbFactory) : DataScanUtility(dbFactory)
{
    private static readonly HashSet<string> LethalCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "firearm", "rifle", "pistol", "shotgun", "smg", "heavy weapon",
        "explosive", "grenade", "launcher", "sniper", "melee", "blade",
        "edged", "energy weapon", "plasma", "railgun",
    };
    private static readonly HashSet<string> LessLethalCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "stun weapon", "taser", "shock weapon", "riot control", "less-lethal",
    };
    private static readonly HashSet<string> NonLethalCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "signal device", "tracker", "marking device", "non-lethal",
    };

    private static readonly string[] LethalKeywords =
        ["armor-piercing", "explosive", "plasma", "railgun", "flechette", "hollow-point", "incendiary"];
    private static readonly string[] LessLethalKeywords =
        ["stun", "taser", "rubber bullet", "tear gas", "riot control", "shock", "tranquilizer"];
    private static readonly string[] NonLethalKeywords =
        ["pepper spray", "smoke", "flash bang", "tracking device", "dye marker", "signal", "tagging"];

    public Task<UtilityResult> RunAsync(
        bool overwrite = false,
        IProgress<UtilityProgress>? progress = null,
        int parallelism = 4,
        CancellationToken ct = default)
        => RunScanAsync(
            GetFiles(["weaponry", "ammunition"]),
            (_, obj) => Tag(obj, overwrite),
            progress, null, parallelism, ct);

    private static int Tag(JsonObject obj, bool overwrite)
    {
        // Check for existing lethality tag
        var tags = (obj["tags"] is JsonArray arr
            ? arr.Select(n => n?.GetValue<string>() ?? "")
            : Enumerable.Empty<string>()).ToList();

        if (!overwrite && tags.Any(t =>
            t.Equals("lethal", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("less_lethal", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("non_lethal", StringComparison.OrdinalIgnoreCase)))
            return 0;

        var category    = (obj["category"]?.GetValue<string>() ?? "").ToLowerInvariant();
        var name        = (obj["name"]?.GetValue<string>()     ?? "").ToLowerInvariant();
        var description = (obj["description"]?.GetValue<string>() ?? "").ToLowerInvariant();
        var text        = $"{name} {description} {category}";

        string lethality;

        // Keyword overrides take priority
        if (NonLethalKeywords.Any(kw => text.Contains(kw)))
            lethality = "non_lethal";
        else if (LessLethalKeywords.Any(kw => text.Contains(kw)))
            lethality = "less_lethal";
        else if (LethalKeywords.Any(kw => text.Contains(kw)))
            lethality = "lethal";
        // Category defaults
        else if (NonLethalCategories.Contains(category))
            lethality = "non_lethal";
        else if (LessLethalCategories.Contains(category))
            lethality = "less_lethal";
        else if (LethalCategories.Contains(category))
            lethality = "lethal";
        else
            lethality = "lethal"; // assume lethal for unknown weapon categories

        // Remove existing lethality tags, add new
        var newTags = tags
            .Where(t => !t.Equals("lethal", StringComparison.OrdinalIgnoreCase)
                     && !t.Equals("less_lethal", StringComparison.OrdinalIgnoreCase)
                     && !t.Equals("non_lethal", StringComparison.OrdinalIgnoreCase))
            .Append(lethality)
            .ToList();

        var newArr = new JsonArray();
        foreach (var t in newTags) newArr.Add(JsonValue.Create(t));
        obj["tags"] = newArr;
        return 1;
    }
}
