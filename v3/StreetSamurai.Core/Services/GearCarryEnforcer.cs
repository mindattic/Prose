using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using System.Text.RegularExpressions;

namespace StreetSamurai.Core.Services;

public class GearUsageViolation
{
    public string GearName { get; set; } = "";
    public string VerbUsed { get; set; } = "";
    public string CharacterName { get; set; } = "";
    public string Issue { get; set; } = "";
    public int CharOffset { get; set; }
}

/// <summary>
/// Post-generation prose validator. Detects gear/weapon usage verbs (drew, fired, holstered…)
/// in beat text and checks whether the subject character has a carries/wields/wears edge to
/// that gear at the beat's story time. Raises GearUsageViolation for each mismatch so the
/// writer knows a prop was used without being established.
///
/// Limitations: entity name matching is exact-ish (case-insensitive, normalised); it will
/// miss gear referred to by nickname only. LLM-based extraction of gear-use events would
/// improve recall — this service handles the deterministic subset.
/// </summary>
public class GearCarryEnforcer
{
    private static readonly string[] CarryRelTypes = ["carries", "wields", "wears", "owns"];

    // Verb patterns that indicate a character is actively using a piece of gear.
    // Each pattern ends with an optional gear-name capture group.
    private static readonly Regex[] UsagePatterns =
    [
        new(@"\b(drew|drawing|draws)\s+(?:his\s+|her\s+|the\s+)?(?<gear>[A-Za-z][A-Za-z0-9'\- ]{2,30})", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\b(raised|raises|raising)\s+(?:his\s+|her\s+|the\s+)?(?<gear>[A-Za-z][A-Za-z0-9'\- ]{2,30})", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\b(fired|fires|firing|shot|shoots|shooting)\s+(?:his\s+|her\s+|the\s+)?(?<gear>[A-Za-z][A-Za-z0-9'\- ]{2,30})", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\b(holstered|holsters|holstering)\s+(?:his\s+|her\s+|the\s+)?(?<gear>[A-Za-z][A-Za-z0-9'\- ]{2,30})", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\b(leveled|levelled|levels)\s+(?:his\s+|her\s+|the\s+)?(?<gear>[A-Za-z][A-Za-z0-9'\- ]{2,30})", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\b(aimed|aims|aiming)\s+(?:his\s+|her\s+|the\s+)?(?<gear>[A-Za-z][A-Za-z0-9'\- ]{2,30})", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\b(threw|throws|throwing|hurled|hurls)\s+(?:his\s+|her\s+|the\s+)?(?<gear>[A-Za-z][A-Za-z0-9'\- ]{2,30})", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\b(unsheathed|unsheathes|drew|sheathes?|sheathed)\s+(?:his\s+|her\s+|the\s+)?(?<gear>[A-Za-z][A-Za-z0-9'\- ]{2,30})", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    ];

    // Common words that follow usage verbs but are NOT gear names — skip these matches.
    private static readonly HashSet<string> FalsePositiveGearWords =
    [
        "his", "her", "the", "a", "an", "it", "them", "there", "back", "up", "down",
        "out", "away", "around", "forward", "toward", "through", "breath", "hand",
        "hands", "arm", "arms", "eyes", "attention", "sight", "cover", "position",
    ];

    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;

    public GearCarryEnforcer(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
        => this.dbFactory = dbFactory;

    /// <summary>
    /// Scans <paramref name="beatText"/> for gear usage verbs and checks whether
    /// <paramref name="characterId"/> carries each gear item at <paramref name="storyTime"/>.
    /// Returns a violation for each gear item used but not found in the carry edges.
    /// </summary>
    public async Task<List<GearUsageViolation>> EnforceAsync(
        string beatText,
        Guid characterId,
        DateTime? storyTime = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(beatText)) return [];

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Load character name
        var character = await db.Entities.AsNoTracking()
            .Where(e => e.Id == characterId)
            .Select(e => new { e.Id, e.Name })
            .FirstOrDefaultAsync(ct);

        // Load all gear the character carries at this story time
        var carryQ = db.Edges.AsNoTracking()
            .Where(e => e.SourceId == characterId
                     && CarryRelTypes.Contains(e.RelationType)
                     && e.InvalidatedAt == null);

        if (storyTime.HasValue)
            carryQ = carryQ.Where(e =>
                (e.StoryValidFrom == null || e.StoryValidFrom <= storyTime) &&
                (e.StoryValidUntil == null || e.StoryValidUntil > storyTime));

        var edges = await carryQ.ToListAsync(ct);
        var gearEntityIds = edges.Select(e => e.TargetId).ToHashSet();

        // Load entity names for carried gear
        var carriedNames = gearEntityIds.Count > 0
            ? await db.Entities.AsNoTracking()
                .Where(e => gearEntityIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Name.ToLowerInvariant(), ct)
            : [];

        var carriedNameSet = carriedNames.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Extract gear usage mentions from text
        var violations = new List<GearUsageViolation>();
        var checkedGear = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pattern in UsagePatterns)
        {
            foreach (Match m in pattern.Matches(beatText))
            {
                var gearName = m.Groups["gear"].Value.Trim().TrimEnd('.', ',', ';', '!', '?');
                if (string.IsNullOrWhiteSpace(gearName)) continue;
                if (FalsePositiveGearWords.Contains(gearName.Split(' ')[0].ToLowerInvariant())) continue;
                if (checkedGear.Contains(gearName)) continue;

                // Skip if any carried item's name contains this gear name or vice versa
                if (carriedNameSet.Any(n =>
                    n.Contains(gearName, StringComparison.OrdinalIgnoreCase) ||
                    gearName.Contains(n, StringComparison.OrdinalIgnoreCase)))
                    continue;

                // Only flag if the gear name matches something that looks like a proper noun
                // (starts with uppercase) — avoids flagging generic verbs ("raised the alarm")
                if (!char.IsUpper(beatText[m.Groups["gear"].Index])) continue;

                checkedGear.Add(gearName);
                violations.Add(new GearUsageViolation
                {
                    GearName = gearName,
                    VerbUsed = m.Groups[1].Value,
                    CharacterName = character?.Name ?? characterId.ToString(),
                    Issue = $"{character?.Name ?? "Character"} uses '{gearName}' but has no carry/wield edge for it at this story time",
                    CharOffset = m.Index,
                });
            }
        }

        return violations;
    }
}
