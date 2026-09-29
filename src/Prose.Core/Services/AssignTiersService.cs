using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Assigns social tier (1–5) to character/synthetic entities based on
/// keyword matching against role, description, affiliation, and tags.
/// Tier 5 = power elite; Tier 1 = survival margin. Mutations land in
/// <c>Records.Json</c>.
/// </summary>
public class AssignTiersService(IDbContextFactory<ProseDbContext> dbFactory) : DataScanUtility(dbFactory)
{
    private static readonly (int tier, string[] keywords)[] TierRules =
    [
        (5, ["ceo", "president", "director", "executive", "chief", "founder", "chairman",
             "oligarch", "magnate", "board member", "c-suite", "vp of", "vice president"]),
        (4, ["doctor", "physician", "surgeon", "lawyer", "attorney", "engineer", "architect",
             "professor", "scientist", "researcher", "specialist", "senior manager", "manager",
             "lieutenant", "commander", "captain", "colonel", "consultant", "analyst lead"]),
        (3, ["technician", "analyst", "programmer", "developer", "nurse", "journalist",
             "reporter", "netrunner", "hacker", "operator", "contractor", "sergeant",
             "investigator", "detective", "freelancer", "runner", "mercenary"]),
        (2, ["mechanic", "pilot", "chef", "cook", "teacher", "instructor", "officer",
             "guard", "security", "soldier", "gang leader", "vendor", "merchant",
             "driver", "courier", "enforcer", "bouncer", "electrician", "plumber"]),
        (1, ["laborer", "worker", "scavenger", "student", "homeless", "street kid",
             "refugee", "beggar", "addict", "drifter", "inmate", "prisoner", "slave"]),
    ];

    public Task<UtilityResult> RunAsync(
        bool overwrite = false,
        IProgress<UtilityProgress>? progress = null,
        int parallelism = 4,
        CancellationToken ct = default,
        bool dryRun = false)
        => RunScanAsync(
            GetFiles(["people", "synthetics"]),
            (_, obj) => Assign(obj, overwrite),
            progress, null, parallelism, ct, dryRun);

    private static int Assign(JsonObject obj, bool overwrite)
    {
        if (!overwrite && obj["tier"] is JsonNode existing)
        {
            // An empty string is how SyntheticLifeData serializes "no tier" ("tier": ""), so it is
            // not an existing assignment: counting it as one made the scan skip every synthetic.
            var kind = existing.GetValueKind();
            if (kind == System.Text.Json.JsonValueKind.Number) return 0;
            if (kind == System.Text.Json.JsonValueKind.String
                && !string.IsNullOrWhiteSpace(existing.GetValue<string>())) return 0;
        }

        var text = string.Join(" ",
            Str(obj["role"]),
            Str(obj["description"]),
            Str(obj["affiliation"]),
            string.Join(" ", (obj["tags"] as JsonArray)?.Select(Str) ?? []))
            .ToLowerInvariant();

        int tier = 2; // default
        foreach (var (t, keywords) in TierRules)
        {
            // Whole words: "mischief" contained "chief", "doctored" "doctor", "cookies" "cook".
            if (keywords.Any(kw => System.Text.RegularExpressions.Regex.IsMatch(
                    text, @"(?<!\w)" + System.Text.RegularExpressions.Regex.Escape(kw) + @"(?!\w)"))) { tier = t; break; }
        }

        obj["tier"] = JsonValue.Create(tier);
        return 1;
    }

    // GetValue<string>() throws on a non-string node (a legacy blob with an object "role" or an
    // array "affiliation"), which failed the whole record instead of just ignoring that field.
    private static string Str(JsonNode? node)
        => node is JsonValue v && v.GetValueKind() == System.Text.Json.JsonValueKind.String
            ? v.GetValue<string>() : "";
}
