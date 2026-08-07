namespace Prose.Core.Models.Graph;

/// <summary>
/// Standard node types for the world graph. Every entity in the system
/// maps to one of these types. Designed for eventual migration to a
/// graph database — each type becomes a label/collection.
/// </summary>
public static class EntityTypes
{
    public const string Character = "character";
    public const string Place = "place";
    public const string Organization = "organization";
    public const string Faction = "faction";
    public const string Weapon = "weapon";
    public const string Equipment = "equipment";
    public const string Technology = "technology";
    public const string Event = "event";
    public const string Fact = "fact";
    public const string Lore = "lore";
    public const string Unknown = "unknown";

    public static readonly string[] All =
    [
        Character, Place, Organization, Faction,
        Weapon, Equipment, Technology,
        Event, Fact, Lore,
    ];

    public static bool IsValid(string type) =>
        All.Contains(type, StringComparer.OrdinalIgnoreCase);

    /// <summary>Normalize a type string to a known type, or "unknown".</summary>
    public static string Normalize(string type)
    {
        var lower = type.Trim().ToLowerInvariant();
        // Handle common aliases
        return lower switch
        {
            "person" or "npc" or "protagonist" or "antagonist" => Character,
            "location" or "district" or "area" or "building" or "room" => Place,
            "corp" or "corponation" or "company" or "corporation" => Organization,
            "gang" or "group" or "crew" or "militia" => Faction,
            "blade" or "gun" or "sword" or "firearm" => Weapon,
            "gear" or "tool" or "device" or "implant" or "augment" or "augmentation" => Equipment,
            "tech" or "software" or "hardware" or "system" or "program" => Technology,
            "incident" or "battle" or "mission" or "operation" => Event,
            "rule" or "detail" or "note" => Fact,
            "history" or "legend" or "myth" or "tradition" => Lore,
            _ => IsValid(lower) ? lower : Unknown,
        };
    }
}
