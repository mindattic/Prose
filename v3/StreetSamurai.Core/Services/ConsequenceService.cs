using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Pre-generation validation — loads a character's current state (injuries, status,
/// augmentations, relationships, possessions) and formats it as hard constraints
/// for the scene generator. Prevents continuity errors like dead characters acting
/// or lost limbs reappearing.
/// </summary>
public class ConsequenceService
{
    private readonly CharacterRepository charRepo;

    public ConsequenceService(CharacterRepository charRepo)
    {
        this.charRepo = charRepo;
    }

    /// <summary>Build a constraint block for all characters in a scene.</summary>
    public string BuildConstraints(List<string> characterNames)
    {
        var constraints = new List<string>();

        foreach (var name in characterNames)
        {
            var character = charRepo.GetByName(name);
            if (character == null) continue;

            var parts = new List<string>();
            parts.Add($"CHARACTER STATE — {character.Name}:");

            // Status (alive, dead, missing, injured)
            if (!string.IsNullOrWhiteSpace(character.Status) && character.Status != "alive")
                parts.Add($"  STATUS: {character.Status} — HARD CONSTRAINT: do not write this character as active if dead/missing");

            // Cyberware inventory — what they have installed
            if (character.CyberwareInventory.Count > 0)
            {
                var chrome = string.Join(", ", character.CyberwareInventory
                    .Where(c => c.Condition == "functional")
                    .Select(c => $"{c.Name} ({c.BodyLocation})"));
                if (chrome.Length > 0) parts.Add($"  INSTALLED CYBERWARE: {chrome}");

                var damaged = character.CyberwareInventory
                    .Where(c => c.Condition != "functional")
                    .Select(c => $"{c.Name} ({c.Condition})")
                    .ToList();
                if (damaged.Count > 0) parts.Add($"  DAMAGED/MISSING CHROME: {string.Join(", ", damaged)}");
            }

            // Belongings — what they carry
            var belongings = character.Belongings;
            var gear = new List<string>();
            if (!string.IsNullOrWhiteSpace(belongings.PrimaryWeapon)) gear.Add($"weapon: {belongings.PrimaryWeapon}");
            if (!string.IsNullOrWhiteSpace(belongings.Vehicle)) gear.Add($"vehicle: {belongings.Vehicle}");
            if (!string.IsNullOrWhiteSpace(belongings.Armor)) gear.Add($"armor: {belongings.Armor}");
            if (gear.Count > 0) parts.Add($"  CARRIES: {string.Join(", ", gear)}");

            // Timeline — recent events
            var recentEvents = character.Timeline
                .OrderByDescending(e => e.Date)
                .Take(3)
                .Select(e => $"  - {e.Date}: {e.Event}")
                .ToList();
            if (recentEvents.Count > 0)
            {
                parts.Add("  RECENT HISTORY:");
                parts.AddRange(recentEvents);
            }

            // Location
            if (!string.IsNullOrWhiteSpace(character.Location))
                parts.Add($"  HOME: {character.Location}");

            if (parts.Count > 1) // more than just the header
                constraints.Add(string.Join("\n", parts));
        }

        return constraints.Count > 0
            ? "CHARACTER STATE CONSTRAINTS (do not contradict these facts):\n" + string.Join("\n\n", constraints)
            : "";
    }
}
