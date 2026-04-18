using System.Text.Json.Nodes;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Assigns social tier (1–5) to character entities in people/ and synthetics/
/// based on keyword matching against role, description, affiliation, and tags.
/// Tier 5 = power elite; Tier 1 = survival margin.
/// Ported from: scripts/py/assign_tiers.py
/// </summary>
public class AssignTiersService(IPathProvider paths) : DataScanUtility(paths)
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
        CancellationToken ct = default)
        => RunScanAsync(
            GetFiles(["people", "synthetics"]),
            (_, obj) => Assign(obj, overwrite),
            progress, null, parallelism, ct);

    private static int Assign(JsonObject obj, bool overwrite)
    {
        if (!overwrite && obj["tier"] != null)
        {
            var existing = obj["tier"]?.GetValueKind();
            if (existing == System.Text.Json.JsonValueKind.Number ||
                existing == System.Text.Json.JsonValueKind.String)
                return 0;
        }

        var text = string.Join(" ",
            obj["role"]?.GetValue<string>()        ?? "",
            obj["description"]?.GetValue<string>() ?? "",
            obj["affiliation"]?.GetValue<string>() ?? "",
            string.Join(" ", (obj["tags"] as JsonArray)?.Select(t => t?.GetValue<string>() ?? "") ?? []))
            .ToLowerInvariant();

        int tier = 2; // default
        foreach (var (t, keywords) in TierRules)
        {
            if (keywords.Any(kw => text.Contains(kw))) { tier = t; break; }
        }

        obj["tier"] = JsonValue.Create(tier);
        return 1;
    }
}
