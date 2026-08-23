using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Models.Canon;

namespace Prose.Core.Services;

/// <summary>
/// Pre-generation validation — loads a character's current state (injuries, status,
/// augmentations, relationships, possessions) and formats it as hard constraints
/// for the scene generator. Prevents continuity errors like dead characters acting
/// or lost limbs reappearing.
/// </summary>
public class ConsequenceService
{
    private readonly CharacterRepository charRepo;
    private readonly IDbContextFactory<ProseDbContext>? dbFactory;

    private static readonly string[] CarryRelTypes = ["carries", "wields", "wears", "owns"];

    public ConsequenceService(CharacterRepository charRepo, IDbContextFactory<ProseDbContext>? dbFactory = null)
    {
        this.charRepo = charRepo;
        this.dbFactory = dbFactory;
    }

    /// <summary>Build a constraint block for all characters in a scene.</summary>
    public async Task<string> BuildConstraintsAsync(List<string> characterNames, DateTime? storyTime = null, CancellationToken ct = default)
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

            // Gear — 2026-08-22 fix: prefer the SAME time-scoped carries/wields/wears/owns Edges
            // GearCarryEnforcer's post-generation check reads (GearCarryEnforcer.cs), so the
            // prompt-side constraint and the post-check enforcement are never built from two
            // disconnected stores that can silently disagree (the flat Belongings snapshot below
            // is never updated when a character picks up gear mid-book via a graph edge — that
            // used to make GearCarryEnforcer raise false-positive violations for organically-
            // acquired gear the prompt never told the writer about). Falls back to the flat
            // Belongings fields only for a character with no carry edges yet (never graph-linked).
            var gear = await BuildGearFromEdgesAsync(character.Id, storyTime, ct);
            if (gear.Count == 0)
            {
                var belongings = character.Belongings;
                if (!string.IsNullOrWhiteSpace(belongings.PrimaryWeapon)) gear.Add($"weapon: {belongings.PrimaryWeapon}");
                if (!string.IsNullOrWhiteSpace(belongings.Vehicle)) gear.Add($"vehicle: {belongings.Vehicle}");
                if (!string.IsNullOrWhiteSpace(belongings.Armor)) gear.Add($"armor: {belongings.Armor}");
            }
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

    /// <summary>Same query shape as GearCarryEnforcer.EnforceAsync — carries/wields/wears/owns
    /// edges from this character, valid at storyTime (or currently valid when storyTime is
    /// null). Returns an empty list (triggering the flat-Belongings fallback above) when this
    /// process has no DbContextFactory (test fixtures), the character id doesn't parse, or the
    /// character genuinely has no carry edges yet.</summary>
    private async Task<List<string>> BuildGearFromEdgesAsync(string characterIdRaw, DateTime? storyTime, CancellationToken ct)
    {
        if (dbFactory == null) return [];
        if (!TryParseCharacterId(characterIdRaw, out var characterId)) return [];

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var q = db.Set<Data.Entities.Edge>().AsNoTracking()
            .Where(e => e.SourceId == characterId && CarryRelTypes.Contains(e.RelationType) && e.InvalidatedAt == null);
        if (storyTime.HasValue)
            q = q.Where(e => (e.StoryValidFrom == null || e.StoryValidFrom <= storyTime) && (e.StoryValidUntil == null || e.StoryValidUntil > storyTime));

        var edges = await q.ToListAsync(ct);
        if (edges.Count == 0) return [];

        var gearIds = edges.Select(e => e.TargetId).ToHashSet();
        var names = await db.Entities.AsNoTracking()
            .Where(e => gearIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name, ct);

        return edges.Select(e => $"{e.RelationType} {names.GetValueOrDefault(e.TargetId, "?")}").ToList();
    }

    private static bool TryParseCharacterId(string raw, out Guid id)
    {
        if (Guid.TryParse(raw, out id)) return true;
        if (raw.Length == 32 && Guid.TryParseExact(raw, "N", out id)) return true;
        id = Guid.Empty;
        return false;
    }
}
