using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using System.Text;

namespace Prose.Core.Services;

public class SensoryHint
{
    public string GearName { get; set; } = "";
    public string EntityType { get; set; } = "weapon";
    public List<string> Descriptors { get; set; } = [];
}

public class AmbientPalette
{
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = "";
    public List<SensoryHint> Hints { get; set; } = [];
    public bool IsEmpty => Hints.Count == 0 || Hints.All(h => h.Descriptors.Count == 0);
}

/// <summary>
/// Reads a character's carry/wield/wear edges and pulls sensory_hints WeaponSpec rows
/// to build a sensory palette block for prompt injection. The palette tells the LLM
/// what physical texture to weave in without inventing props the character doesn't carry.
/// </summary>
public class AmbientDetailInjector
{
    public const string SensoryHintsKey = "sensory_hints";

    private static readonly string[] CarryRelTypes = ["carries", "wields", "wears"];

    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    public AmbientDetailInjector(IDbContextFactory<ProseDbContext> dbFactory)
        => this.dbFactory = dbFactory;

    /// <summary>
    /// Builds the sensory palette for <paramref name="characterId"/> at optional
    /// <paramref name="asOfDate"/>. Returns an empty palette (not null) when no carry
    /// edges exist or no sensory_hints specs have been seeded.
    /// </summary>
    public async Task<AmbientPalette> GetPaletteAsync(
        Guid characterId,
        DateTime? asOfDate = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var character = await db.Entities.AsNoTracking()
            .Where(e => e.Id == characterId)
            .Select(e => new { e.Id, e.Name })
            .FirstOrDefaultAsync(ct);

        var palette = new AmbientPalette
        {
            CharacterId = characterId,
            CharacterName = character?.Name ?? "",
        };

        var carryQ = db.Edges.AsNoTracking()
            .Where(e => e.SourceId == characterId
                     && CarryRelTypes.Contains(e.RelationType)
                     && e.InvalidatedAt == null);

        if (asOfDate.HasValue)
            carryQ = carryQ.Where(e =>
                (e.StoryValidFrom == null || e.StoryValidFrom <= asOfDate) &&
                (e.StoryValidUntil == null || e.StoryValidUntil > asOfDate));

        var edges = await carryQ.ToListAsync(ct);
        if (edges.Count == 0) return palette;

        var gearIds = edges.Select(e => e.TargetId).Distinct().ToList();

        // Load entity names + types
        var entities = await db.Entities.AsNoTracking()
            .Where(e => gearIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => new { e.Name, e.EntityType }, ct);

        // Load sensory_hints WeaponSpec rows for any weapons in the carry list
        var weaponEntityIds = entities
            .Where(kv => kv.Value.EntityType.Equals("weapon", StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key)
            .ToList();

        Dictionary<Guid, string> sensoryByEntityId = [];
        if (weaponEntityIds.Count > 0)
        {
            // WeaponSpec.WeaponId == Entity.Id for weapon entities — no intermediate join.
            var specs = await db.WeaponSpecs.AsNoTracking()
                .Where(s => weaponEntityIds.Contains(s.WeaponId) && s.SpecKey == SensoryHintsKey)
                .ToListAsync(ct);

            foreach (var spec in specs)
                sensoryByEntityId[spec.WeaponId] = spec.SpecValue;
        }

        foreach (var gearId in gearIds)
        {
            if (!entities.TryGetValue(gearId, out var ent)) continue;

            var descriptors = new List<string>();
            if (sensoryByEntityId.TryGetValue(gearId, out var hints) && !string.IsNullOrWhiteSpace(hints))
            {
                descriptors.AddRange(
                    hints.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            palette.Hints.Add(new SensoryHint
            {
                GearName = ent.Name,
                EntityType = ent.EntityType,
                Descriptors = descriptors,
            });
        }

        return palette;
    }

    /// Formats the palette as a prompt block. Returns null when the palette is empty.
    public string? FormatPaletteAsPromptBlock(AmbientPalette palette)
    {
        if (palette.IsEmpty) return null;

        var sb = new StringBuilder();
        sb.AppendLine($"## Ambient sensory palette ({palette.CharacterName} carries)");

        foreach (var hint in palette.Hints)
        {
            if (hint.Descriptors.Count > 0)
                sb.AppendLine($"- {hint.GearName} [{hint.EntityType}]: {string.Join("; ", hint.Descriptors)}");
            else
                sb.AppendLine($"- {hint.GearName} [{hint.EntityType}]");
        }

        sb.AppendLine("Weave 1–2 of these sensory details into the scene where natural. Do not force them.");
        return sb.ToString();
    }
}
