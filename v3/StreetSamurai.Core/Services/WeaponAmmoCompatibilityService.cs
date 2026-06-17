using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

public class AmmoEntry
{
    public Guid? AmmunitionId { get; set; }
    public string Alias { get; set; } = "";
    public string AmmunitionName { get; set; } = "";
}

public class SiblingWeapon
{
    public Guid WeaponId { get; set; }
    public string WeaponName { get; set; } = "";
    public string SharedAmmoAlias { get; set; } = "";
}

public class AmmoNetwork
{
    public Guid WeaponId { get; set; }
    public string WeaponName { get; set; } = "";
    public List<AmmoEntry> Ammunition { get; set; } = [];
    // Other weapons that chamber at least one of the same ammo types
    public List<SiblingWeapon> SiblingWeapons { get; set; } = [];
}

public class LoadoutWeapon
{
    public string GearName { get; set; } = "";
    public Guid? GearEntityId { get; set; }
    public Guid? WeaponId { get; set; }
    public List<AmmoEntry> Ammunition { get; set; } = [];
}

public class CharacterLoadout
{
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = "";
    public DateTime? AsOfDate { get; set; }
    public List<LoadoutWeapon> Weapons { get; set; } = [];
}

/// <summary>
/// Queries the weapon–ammunition compatibility graph (WeaponAmmunitionTypes +
/// AmmunitionCompatibleWeapons). Use for: continuity (character has ammo for their weapon),
/// scene logistics (can they scavenge compatible rounds), and world enrichment (sibling
/// weapons that share a chambering).
/// </summary>
public class WeaponAmmoCompatibilityService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;

    public WeaponAmmoCompatibilityService(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
        => this.dbFactory = dbFactory;

    /// All weapons compatible with a given ammunition entity ID.
    public async Task<List<Weapon>> GetCompatibleWeaponsAsync(
        Guid ammoEntityId,
        CancellationToken ct = default)
    {
        using var db = dbFactory.CreateDbContext();

        var fromWeaponAmmoTypes = await db.WeaponAmmunitionTypes.AsNoTracking()
            .Where(w => w.AmmunitionId == ammoEntityId)
            .Select(w => w.WeaponId)
            .ToListAsync(ct);

        var fromCompatible = await db.AmmunitionCompatibleWeapons.AsNoTracking()
            .Where(a => a.AmmunitionId == ammoEntityId && a.WeaponId.HasValue)
            .Select(a => a.WeaponId!.Value)
            .ToListAsync(ct);

        var weaponIds = fromWeaponAmmoTypes.Union(fromCompatible).Distinct().ToList();

        if (weaponIds.Count == 0) return [];

        return await db.Weapons.AsNoTracking()
            .Where(w => weaponIds.Contains(w.Id))
            .ToListAsync(ct);
    }

    /// Full ammo network for a weapon: its ammo types + sibling weapons that share any of them.
    public async Task<AmmoNetwork> GetSharedAmmoNetworkAsync(
        Guid weaponId,
        CancellationToken ct = default)
    {
        using var db = dbFactory.CreateDbContext();

        var weapon = await db.Weapons.AsNoTracking()
            .Where(w => w.Id == weaponId)
            .FirstOrDefaultAsync(ct);

        var network = new AmmoNetwork
        {
            WeaponId = weaponId,
            WeaponName = weapon?.Name ?? weaponId.ToString(),
        };

        var ammoRows = await db.WeaponAmmunitionTypes.AsNoTracking()
            .Where(w => w.WeaponId == weaponId)
            .OrderBy(w => w.Position)
            .ToListAsync(ct);

        if (ammoRows.Count == 0) return network;

        // Resolve ammo entity names
        var linkedIds = ammoRows.Where(a => a.AmmunitionId.HasValue).Select(a => a.AmmunitionId!.Value).ToList();
        var ammoNames = linkedIds.Count > 0
            ? await db.Entities.AsNoTracking()
                .Where(e => linkedIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Name, ct)
            : [];

        network.Ammunition = ammoRows.Select(a => new AmmoEntry
        {
            AmmunitionId = a.AmmunitionId,
            Alias = a.Alias,
            AmmunitionName = a.AmmunitionId.HasValue ? ammoNames.GetValueOrDefault(a.AmmunitionId.Value, a.Alias) : a.Alias,
        }).ToList();

        // Find sibling weapons that chamber the same ammo types
        var linkedAmmoIds = linkedIds.ToHashSet();
        if (linkedAmmoIds.Count > 0)
        {
            var siblingIds = await db.WeaponAmmunitionTypes.AsNoTracking()
                .Where(w => w.WeaponId != weaponId && w.AmmunitionId.HasValue && linkedAmmoIds.Contains(w.AmmunitionId!.Value))
                .Select(w => new { w.WeaponId, Alias = w.Alias, AmmoId = w.AmmunitionId!.Value })
                .ToListAsync(ct);

            var siblingWeaponIds = siblingIds.Select(s => s.WeaponId).Distinct().ToList();
            var siblingWeapons = await db.Weapons.AsNoTracking()
                .Where(w => siblingWeaponIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.Name, ct);

            network.SiblingWeapons = siblingIds
                .GroupBy(s => s.WeaponId)
                .Select(g => new SiblingWeapon
                {
                    WeaponId = g.Key,
                    WeaponName = siblingWeapons.GetValueOrDefault(g.Key, g.Key.ToString()),
                    SharedAmmoAlias = g.First().Alias,
                })
                .ToList();
        }

        return network;
    }

    /// A character's weapon loadout (from CharacterBelongingsGear) with ammo for each weapon.
    public async Task<CharacterLoadout> GetCharacterLoadoutAsync(
        Guid characterId,
        DateTime? asOfDate = null,
        CancellationToken ct = default)
    {
        using var db = dbFactory.CreateDbContext();

        var character = await db.Entities.AsNoTracking()
            .Where(e => e.Id == characterId)
            .Select(e => new { e.Id, e.Name })
            .FirstOrDefaultAsync(ct);

        var loadout = new CharacterLoadout
        {
            CharacterId = characterId,
            CharacterName = character?.Name ?? "",
            AsOfDate = asOfDate,
        };

        // Pull signature gear items that are weapons
        var gearItems = await db.CharacterBelongingsGear.AsNoTracking()
            .Where(g => g.CharacterId == characterId && g.Bucket == "signature_gear")
            .OrderBy(g => g.Position)
            .ToListAsync(ct);

        if (gearItems.Count == 0) return loadout;

        // Look up ammo for gear items that link to a weapon entity
        var linkedEntityIds = gearItems
            .Where(g => g.GearEntityId.HasValue)
            .Select(g => g.GearEntityId!.Value)
            .ToList();

        // Weapon.Id is the same as the entity Id for weapon entities in this schema
        var weaponAmmoMap = new Dictionary<Guid, List<AmmoEntry>>();
        if (linkedEntityIds.Count > 0)
        {
            var allAmmoRows = await db.WeaponAmmunitionTypes.AsNoTracking()
                .Where(w => linkedEntityIds.Contains(w.WeaponId))
                .OrderBy(w => w.WeaponId).ThenBy(w => w.Position)
                .ToListAsync(ct);

            var ammoEntityIds = allAmmoRows
                .Where(a => a.AmmunitionId.HasValue).Select(a => a.AmmunitionId!.Value).Distinct().ToList();
            var ammoNames = ammoEntityIds.Count > 0
                ? await db.Entities.AsNoTracking()
                    .Where(e => ammoEntityIds.Contains(e.Id))
                    .ToDictionaryAsync(e => e.Id, e => e.Name, ct)
                : [];

            foreach (var row in allAmmoRows)
            {
                if (!weaponAmmoMap.ContainsKey(row.WeaponId))
                    weaponAmmoMap[row.WeaponId] = [];
                weaponAmmoMap[row.WeaponId].Add(new AmmoEntry
                {
                    AmmunitionId = row.AmmunitionId,
                    Alias = row.Alias,
                    AmmunitionName = row.AmmunitionId.HasValue ? ammoNames.GetValueOrDefault(row.AmmunitionId.Value, row.Alias) : row.Alias,
                });
            }
        }

        loadout.Weapons = gearItems.Select(g => new LoadoutWeapon
        {
            GearName = g.GearName,
            GearEntityId = g.GearEntityId,
            WeaponId = g.GearEntityId,
            Ammunition = g.GearEntityId.HasValue ? weaponAmmoMap.GetValueOrDefault(g.GearEntityId.Value, []) : [],
        }).ToList();

        return loadout;
    }
}
