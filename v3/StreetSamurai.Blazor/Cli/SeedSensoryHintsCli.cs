using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// ss --seed-sensory-hints               Seed canonical hints for Silence + Cacophony
/// ss --seed-sensory-hints --list        Show all current sensory_hints rows
/// ss --seed-sensory-hints --weapon "Silence" --hints "hint1; hint2"   Set for any weapon
/// ss --seed-sensory-hints --force       Overwrite existing rows (default: skip)
/// ss --seed-sensory-hints --seed-carry-edges   Also seed Kyle's carry edges for Silence + Cacophony
///
/// Canonical weapon names and GUIDs come from AmmunitionLinkerService constants — do not
/// duplicate them here. If a name changes, change it there once; this file picks it up.
/// </summary>
public static class SeedSensoryHintsCli
{
    // Canonical sensory palettes for Kyle's signature weapons.
    // Semicolon-delimited — each segment is one injectable detail.
    // Keep it grounded, physical, non-poetic (the LLM poeticises; we just name the texture).
    //
    // Names + GUIDs referenced from AmmunitionLinkerService; never hardcode them here.
    private static readonly CanonWeapon[] CanonicalWeapons =
    [
        new(AmmunitionLinkerService.SilenceWeaponName,
            AmmunitionLinkerService.SilenceWeaponId,
            "silence-katana", "melee",
            "Kyle's matte-black carbon-nanotube composite katana. Plain steel-CNT — no piezo, no glow.",
            "weight at the hip; cloth-wrapped tsuka; lacquered saya; hiss of the draw; " +
            "balanced point in the hand; faint cedar from the saya lining"),

        new(AmmunitionLinkerService.CacophonyWeaponName,
            AmmunitionLinkerService.CacophonyWeaponId,
            "cacophony-revolver", "firearm",
            "Kyle's 5-shot revolver with a birds-head grip worn smooth from use.",
            "cylinder heft in the palm; birds-head grip worn smooth; " +
            "trigger pull weight; spent-powder smell after firing; " +
            "hammer click on cock; cold steel frame against the wrist"),
    ];

    private static readonly Guid KyleId = AmmunitionLinkerService.KyleCharacterId;

    private sealed record CanonWeapon(
        string Name, Guid KnownId, string Slug, string Category,
        string Description, string Hints);

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

        // One-time cleanup: remove the stale "Chorus" entity created before the rename.
        await CleanupStaleChorusEntityAsync(db);

        var result = await SeedCanonical(db, force);

        if (seedEdges)
            await SeedCarryEdges(db);

        return result;
    }

    // ── one-time migration ────────────────────────────────────────────────────

    /// <summary>
    /// Removes the stale "Chorus" entity + its Weapons row, WeaponSpec, and carry edge
    /// that were created before the Chorus→Cacophony rename. Idempotent; no-ops when
    /// the entity is already gone.
    /// </summary>
    private static async Task CleanupStaleChorusEntityAsync(StreetSamuraiDbContext db)
    {
        var stale = await db.Entities
            .FirstOrDefaultAsync(e => e.Slug == "chorus-revolver" && e.EntityType == "weapon");
        if (stale == null) return;

        Console.WriteLine($"  [cleanup] Removing stale 'chorus-revolver' entity ({stale.Id})…");

        // carry edge Kyle → chorus-revolver
        var edge = await db.Edges
            .FirstOrDefaultAsync(e => e.SourceId == KyleId && e.TargetId == stale.Id && e.RelationType == "carries");
        if (edge != null) db.Edges.Remove(edge);

        // WeaponSpec rows
        var specs = await db.WeaponSpecs.Where(s => s.WeaponId == stale.Id).ToListAsync();
        db.WeaponSpecs.RemoveRange(specs);

        // Weapons row
        var weaponRow = await db.Weapons.FindAsync(stale.Id);
        if (weaponRow != null) db.Weapons.Remove(weaponRow);

        // Entity spine last (FK target)
        db.Entities.Remove(stale);

        await db.SaveChangesAsync();
        Console.WriteLine("  [cleanup] Done.");
    }

    // ── canonical seed ────────────────────────────────────────────────────────

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
            var written  = await UpsertSpec(db, weaponId, cw.Hints, force, "seeded 2026-06-16");
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
            // Prefer known GUID; fall back to slug lookup.
            var weaponEntity = cw.KnownId != Guid.Empty
                ? await db.Entities.AsNoTracking().FirstOrDefaultAsync(e => e.Id == cw.KnownId)
                : await db.Entities.AsNoTracking().FirstOrDefaultAsync(e => e.Slug == cw.Slug && e.EntityType == "weapon");

            if (weaponEntity == null) continue;

            var exists = await db.Edges.AnyAsync(
                e => e.SourceId == KyleId && e.TargetId == weaponEntity.Id
                  && e.RelationType == "carries" && e.InvalidatedAt == null);

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
    /// Resolves the Weapon.Id for a CanonWeapon:
    ///   1. KnownId lookup (fast, by GUID)
    ///   2. Entity name lookup (EntityType='weapon')
    ///   3. Create Entity + Weapons rows if neither exists
    /// </summary>
    private static async Task<Guid> FindOrCreateWeaponAsync(StreetSamuraiDbContext db, CanonWeapon cw)
    {
        // 1. GUID hit — preferred path for canonical weapons
        if (cw.KnownId != Guid.Empty)
        {
            var byId = await db.Weapons.FindAsync(cw.KnownId);
            if (byId != null)
            {
                Console.WriteLine($"  {cw.Name}: found by GUID in Weapons");
                return byId.Id;
            }

            // Entity exists but no Weapons row yet
            var entityById = await db.Entities.FindAsync(cw.KnownId);
            if (entityById != null)
            {
                db.Weapons.Add(new Weapon { Id = entityById.Id, Name = entityById.Name, Category = cw.Category, Description = cw.Description });
                await db.SaveChangesAsync();
                Console.WriteLine($"  {cw.Name}: created Weapons row for existing entity ({cw.KnownId})");
                return entityById.Id;
            }
        }

        // 2. Name lookup
        var weaponByName = await db.Weapons.FirstOrDefaultAsync(w => w.Name == cw.Name);
        if (weaponByName != null)
        {
            Console.WriteLine($"  {cw.Name}: found by name in Weapons");
            return weaponByName.Id;
        }

        var entityByName = await db.Entities.FirstOrDefaultAsync(e => e.Name == cw.Name && e.EntityType == "weapon");
        if (entityByName != null)
        {
            db.Weapons.Add(new Weapon { Id = entityByName.Id, Name = cw.Name, Category = cw.Category, Description = cw.Description });
            await db.SaveChangesAsync();
            Console.WriteLine($"  {cw.Name}: created Weapons row for existing entity ({entityByName.Id})");
            return entityByName.Id;
        }

        // 3. Create from scratch
        var entity = new Entity
        {
            Id         = cw.KnownId != Guid.Empty ? cw.KnownId : Guid.CreateVersion7(),
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

        db.Weapons.Add(new Weapon { Id = entity.Id, Name = cw.Name, Category = cw.Category, Description = cw.Description });
        await db.SaveChangesAsync();

        Console.WriteLine($"  {cw.Name}: created Entity + Weapons rows ({entity.Id})");
        return entity.Id;
    }

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
            WeaponId  = weaponId,
            SpecKey   = AmbientDetailInjector.SensoryHintsKey,
            SpecValue = value,
            Notes     = notes,
        });
        await db.SaveChangesAsync();
        return 1;
    }
}
