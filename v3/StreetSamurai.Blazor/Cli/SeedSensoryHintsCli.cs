using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// ss --seed-sensory-hints               Seed canonical hints for Silence + Chorus
/// ss --seed-sensory-hints --list        Show all current sensory_hints rows
/// ss --seed-sensory-hints --weapon "Silence" --hints "hint1; hint2"   Set for any weapon
/// ss --seed-sensory-hints --force       Overwrite existing rows (default: skip)
/// ss --seed-sensory-hints --seed-carry-edges   Also seed Kyle's carry edges for Silence + Chorus
/// </summary>
public static class SeedSensoryHintsCli
{
    // Canonical sensory palettes for Kyle's signature weapons.
    // Semicolon-delimited — each segment is one injectable detail.
    // Keep it grounded, physical, non-poetic (the LLM poeticises; we just name the texture).
    private static readonly CanonWeapon[] CanonicalWeapons =
    [
        new("Silence",   "silence-katana",  "melee",
            "Kyle's matte-black carbon-nanotube composite katana. Plain steel-CNT construction — no piezo, no glow.",
            "weight at the hip; cloth-wrapped tsuka; lacquered saya; hiss of the draw; " +
            "balanced point in the hand; faint cedar from the saya lining"),

        new("Chorus",    "chorus-revolver", "firearm",
            "Kyle's 5-shot revolver with a birds-head grip worn smooth from use.",
            "cylinder heft in the palm; birds-head grip worn smooth; " +
            "trigger pull weight; spent-powder smell after firing; " +
            "hammer click on cock; cold steel frame against the wrist"),
    ];

    private static readonly Guid KyleId = Guid.Parse("019D6143-A648-7876-9688-0F6D38D70075");

    private sealed record CanonWeapon(string Name, string Slug, string Category, string Description, string Hints);

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        bool list      = args.Contains("--list");
        bool force     = args.Contains("--force");
        bool seedEdges = args.Contains("--seed-carry-edges");
        string? weaponName  = null;
        string? customHints = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--weapon": weaponName  = args[i + 1]; i++; break;
                case "--hints":  customHints = args[i + 1]; i++; break;
            }
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        using var db = dbFactory.CreateDbContext();

        if (list)
            return await ListHints(db);

        if (weaponName != null && customHints != null)
            return await SetHints(db, weaponName, customHints, force);

        if (weaponName != null || customHints != null)
        {
            Console.Error.WriteLine("--weapon and --hints must be provided together.");
            Console.Error.WriteLine("Usage: ss --seed-sensory-hints --weapon \"Silence\" --hints \"hint1; hint2\"");
            return 1;
        }

        var result = await SeedCanonical(db, force);

        if (seedEdges)
            await SeedCarryEdges(db);

        return result;
    }

    private static async Task<int> ListHints(StreetSamuraiDbContext db)
    {
        var rows = await db.WeaponSpecs.AsNoTracking()
            .Where(s => s.SpecKey == AmbientDetailInjector.SensoryHintsKey)
            .Join(db.Weapons, s => s.WeaponId, w => w.Id, (s, w) => new { w.Name, s.SpecValue })
            .OrderBy(r => r.Name)
            .ToListAsync();

        if (rows.Count == 0)
        {
            Console.WriteLine("No sensory_hints rows found. Run 'ss --seed-sensory-hints' to seed canonical ones.");
            return 0;
        }

        Console.WriteLine($"sensory_hints ({rows.Count} weapon(s)):");
        foreach (var r in rows)
        {
            Console.WriteLine($"\n  {r.Name}:");
            foreach (var hint in r.SpecValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                Console.WriteLine($"    * {hint}");
        }
        return 0;
    }

    private static async Task<int> SetHints(StreetSamuraiDbContext db, string weaponName, string hints, bool force)
    {
        var weaponId = await ResolveWeaponIdByName(db, weaponName);
        if (weaponId == null)
        {
            Console.Error.WriteLine($"Weapon '{weaponName}' not found in Weapons or Entities tables.");
            return 1;
        }

        var written = await UpsertSpec(db, weaponId.Value, hints, force, "set via CLI 2026-06-16");
        Console.WriteLine(written == 0
            ? $"  {weaponName}: already set, skipped (use --force to overwrite)"
            : $"  {weaponName}: sensory_hints set ({hints.Split(';').Length} hint(s))");
        return 0;
    }

    private static async Task<int> SeedCanonical(StreetSamuraiDbContext db, bool force)
    {
        int total = 0;
        foreach (var cw in CanonicalWeapons)
        {
            var weaponId = await FindOrCreateWeaponAsync(db, cw);

            var written = await UpsertSpec(db, weaponId, cw.Hints, force, "seeded 2026-06-16");
            if (written == 0)
                Console.WriteLine($"  {cw.Name}: already has sensory_hints (use --force to overwrite)");
            else
            {
                Console.WriteLine($"  {cw.Name}: seeded {cw.Hints.Split(';').Length} hint(s)");
                total++;
            }
        }

        Console.WriteLine(total == 0
            ? "\nAll canonical hints already present."
            : $"\n{total} weapon(s) seeded.");
        return 0;
    }

    private static async Task SeedCarryEdges(StreetSamuraiDbContext db)
    {
        Console.WriteLine("\nSeeding Kyle carry edges...");
        foreach (var cw in CanonicalWeapons)
        {
            var weaponEntity = await db.Entities.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Slug == cw.Slug && e.EntityType == "weapon");
            if (weaponEntity == null) continue;

            var exists = await db.Edges.AnyAsync(
                e => e.SourceId == KyleId && e.TargetId == weaponEntity.Id && e.RelationType == "carries"
                  && e.InvalidatedAt == null);

            if (exists)
            {
                Console.WriteLine($"  Kyle --carries--> {cw.Name}: already present");
                continue;
            }

            db.Edges.Add(new Edge
            {
                UniverseId   = Universe.GlmzId,
                SourceId     = KyleId,
                TargetId     = weaponEntity.Id,
                RelationType = "carries",
                Weight       = 1.0,
                Sentiment    = "neutral",
                Source       = "canon",
            });
            await db.SaveChangesAsync();
            Console.WriteLine($"  Kyle --carries--> {cw.Name}: seeded");
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Looks up by name in Weapons first, then Entities (EntityType='weapon').
    /// Creates an Entity + Weapon row if neither exists.
    /// Returns the Weapon.Id (= Entity.Id) guaranteed.
    /// </summary>
    private static async Task<Guid> FindOrCreateWeaponAsync(StreetSamuraiDbContext db, CanonWeapon cw)
    {
        // 1. Direct Weapons table hit
        var weapon = await db.Weapons.FirstOrDefaultAsync(w => w.Name == cw.Name);
        if (weapon != null)
        {
            Console.WriteLine($"  {cw.Name}: found in Weapons table");
            return weapon.Id;
        }

        // 2. Entity spine exists but no Weapons row
        var entity = await db.Entities
            .FirstOrDefaultAsync(e => e.Name == cw.Name && e.EntityType == "weapon");

        if (entity == null)
        {
            // 3. Create Entity + Weapon from scratch
            entity = new Entity
            {
                Id         = Guid.CreateVersion7(),
                UniverseId = Universe.GlmzId,
                EntityType = "weapon",
                Name       = cw.Name,
                Slug       = cw.Slug,
                Status     = "canon",
                Description = cw.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(entity);
            await db.SaveChangesAsync();
            Console.WriteLine($"  {cw.Name}: created Entity row ({entity.Id})");
        }

        // 4. Create the Weapons row (Id must match Entity.Id)
        db.Weapons.Add(new Weapon
        {
            Id          = entity.Id,
            Name        = cw.Name,
            Category    = cw.Category,
            Description = cw.Description,
        });
        await db.SaveChangesAsync();
        Console.WriteLine($"  {cw.Name}: created Weapons row");
        return entity.Id;
    }

    /// <summary>Looks up an existing weapon by name without creating anything.</summary>
    private static async Task<Guid?> ResolveWeaponIdByName(StreetSamuraiDbContext db, string name)
    {
        var weapon = await db.Weapons.AsNoTracking().FirstOrDefaultAsync(w => w.Name == name);
        if (weapon != null) return weapon.Id;

        var entity = await db.Entities.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Name == name && e.EntityType == "weapon");
        return entity?.Id;
    }

    private static async Task<int> UpsertSpec(
        StreetSamuraiDbContext db, Guid weaponId, string value, bool force, string notes)
    {
        var existing = await db.WeaponSpecs
            .FirstOrDefaultAsync(s => s.WeaponId == weaponId && s.SpecKey == AmbientDetailInjector.SensoryHintsKey);

        if (existing != null)
        {
            if (!force) return 0;
            existing.SpecValue = value;
            existing.Notes = notes;
            await db.SaveChangesAsync();
            return 1;
        }

        db.WeaponSpecs.Add(new WeaponSpec
        {
            WeaponId = weaponId,
            SpecKey  = AmbientDetailInjector.SensoryHintsKey,
            SpecValue = value,
            Notes    = notes,
        });
        await db.SaveChangesAsync();
        return 1;
    }
}
