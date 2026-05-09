using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Two jobs:
///   <list type="number">
///     <item>Seed Kyle's <em>Chorus</em> with its canonical specs and ammo
///     compatibility (idempotent; safe to re-run).</item>
///     <item>Bulk-link every other firearm in the catalog to one or more
///     <see cref="Entities.Ammunitions"/> rows using an LLM extraction over
///     each weapon's <c>Description</c> + <c>Specifications</c>.</item>
///   </list>
///
/// Output goes to two existing tables: <c>WeaponAmmunitionTypes</c>
/// (weapon → ammo) and <c>WeaponSpecs</c> (per-weapon structured key/value
/// rows). Character ownership of a weapon is recorded by setting
/// <c>CharacterBelongingsGear.GearEntityId</c> to the weapon's
/// <c>Entities.Id</c>.
/// </summary>
public class AmmunitionLinkerService
{
    public static readonly Guid ChorusWeaponId = Guid.Parse("4AB24F74-61D4-4F45-B326-7C6B98C96279");
    public static readonly Guid SilenceWeaponId = Guid.Empty; // looked up by name (multiple "Silence" entities)
    public static readonly Guid KyleCharacterId = Guid.Parse("019D6143-A648-7876-9688-0F6D38D70075");

    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILlmService llm;
    private readonly ILogger<AmmunitionLinkerService> log;

    public AmmunitionLinkerService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILlmService llm,
        ILogger<AmmunitionLinkerService> log)
    {
        this.dbFactory = dbFactory;
        this.llm = llm;
        this.log = log;
    }

    public sealed class LinkResult
    {
        public int WeaponsScanned { get; set; }
        public int AmmunitionsCreated { get; set; }
        public int CompatibilityRowsAdded { get; set; }
        public int SpecsWritten { get; set; }
        public List<string> Errors { get; } = new();
    }

    // ── idempotent schema helpers ──────────────────────────────────────────────

    /// <summary>
    /// Idempotent: creates <c>WeaponSpecs</c> on a live DB if it isn't already
    /// there (the <c>--rebuild</c> path picks it up via OnModelCreating; live
    /// DBs need this DDL).
    /// </summary>
    public async Task EnsureWeaponSpecsSchemaAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        const string ddl = """
            IF OBJECT_ID('dbo.WeaponSpecs','U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[WeaponSpecs] (
                    [Id]         BIGINT IDENTITY(1,1) NOT NULL,
                    [WeaponId]   UNIQUEIDENTIFIER NOT NULL,
                    [SpecKey]    NVARCHAR(80) NOT NULL,
                    [SpecValue]  NVARCHAR(MAX) NOT NULL,
                    [Notes]      NVARCHAR(MAX) NULL,
                    [SysStart]   DATETIME2(7) GENERATED ALWAYS AS ROW START NOT NULL DEFAULT SYSUTCDATETIME(),
                    [SysEnd]     DATETIME2(7) GENERATED ALWAYS AS ROW END   NOT NULL DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
                    PERIOD FOR SYSTEM_TIME ([SysStart], [SysEnd]),
                    CONSTRAINT [PK_WeaponSpecs] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_WeaponSpecs_Weapons_WeaponId]
                        FOREIGN KEY ([WeaponId]) REFERENCES [dbo].[Weapons]([Id]) ON DELETE CASCADE
                ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[WeaponSpecs_History]));
                CREATE INDEX [IX_WeaponSpecs_WeaponId_SpecKey] ON [dbo].[WeaponSpecs]([WeaponId],[SpecKey]);
                CREATE INDEX [IX_WeaponSpecs_SpecKey]          ON [dbo].[WeaponSpecs]([SpecKey]);
            END;
            """;
        await db.Database.ExecuteSqlRawAsync(ddl, ct);
    }

    /// <summary>Idempotent: adds the <c>GearEntityId</c> column + index to <c>CharacterBelongingsGear</c>.</summary>
    public async Task EnsureGearEntityIdColumnAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        const string ddl = """
            IF COL_LENGTH('dbo.CharacterBelongingsGear','GearEntityId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[CharacterBelongingsGear]
                    ADD [GearEntityId] UNIQUEIDENTIFIER NULL;
            END;
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                           WHERE name = 'FK_CharacterBelongingsGear_Entities_GearEntityId')
            BEGIN
                -- ON DELETE NO ACTION because CharacterBelongingsGear already
                -- cascades from Characters → Entities; a second cascade path
                -- through Entities here would trip SQL Server error 1785.
                -- Entities are archived (IsActive=0), not hard-deleted, so
                -- the FK is purely for join integrity.
                ALTER TABLE [dbo].[CharacterBelongingsGear]
                    ADD CONSTRAINT [FK_CharacterBelongingsGear_Entities_GearEntityId]
                        FOREIGN KEY ([GearEntityId]) REFERENCES [dbo].[Entities]([Id]) ON DELETE NO ACTION;
            END;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_CharacterBelongingsGear_GearEntityId'
                             AND object_id = OBJECT_ID('dbo.CharacterBelongingsGear'))
            BEGIN
                CREATE INDEX [IX_CharacterBelongingsGear_GearEntityId]
                    ON [dbo].[CharacterBelongingsGear]([GearEntityId]);
            END;
            """;
        await db.Database.ExecuteSqlRawAsync(ddl, ct);
    }

    // ── Chorus seed (one-shot, idempotent) ────────────────────────────────────

    /// <summary>
    /// Insert canonical specs + ammo compatibility for Kyle's Chorus, plus
    /// link Kyle's CharacterBelongingsGear row to the Chorus Weapon entity.
    /// Idempotent.
    /// </summary>
    public async Task<LinkResult> SeedChorusAsync(CancellationToken ct = default)
    {
        var result = new LinkResult();
        await EnsureWeaponSpecsSchemaAsync(ct);
        await EnsureGearEntityIdColumnAsync(ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // 1) Ammunition entities — create .45 Long Colt and .410 shotshell when missing.
        var ammo45  = await UpsertAmmunitionAsync(db, ".45 Long Colt",
            "Black-powder-era handgun cartridge originally chambered in the 1873 Colt SAA. " +
            "In the 23rd century it persists as a common loadout for revolver platforms — heavy bullet, " +
            "moderate velocity, generous wound channel.",
            tags: new[] { "revolver", "handgun", "rimmed", "legacy_round" }, ct);
        var ammo410 = await UpsertAmmunitionAsync(db, ".410 Shotshell",
            ".410-bore shotshell. Smallest of the standard shotgun gauges. " +
            "Chambered in revolver platforms (Taurus Judge analogues, Torii Chorus) for door, lock, and close-pattern crowd-stop work.",
            tags: new[] { "shotshell", "shotgun", "revolver_compatible", "small_bore" }, ct);
        if (ammo45.Created)  result.AmmunitionsCreated++;
        if (ammo410.Created) result.AmmunitionsCreated++;

        // 2) WeaponAmmunitionTypes — Chorus chambers both.
        result.CompatibilityRowsAdded += await UpsertWeaponAmmoAsync(db, ChorusWeaponId, ammo45.Id,  ".45 Long Colt", position: 0, ct);
        result.CompatibilityRowsAdded += await UpsertWeaponAmmoAsync(db, ChorusWeaponId, ammo410.Id, ".410 Shotshell", position: 1, ct);

        // 3) WeaponSpecs — every fact the user gave us.
        var specs = new (string key, string value, string? note)[]
        {
            ("chambering",    ".45 Long Colt + .410 Shotshell (interchangeable in same cylinder)", "Taurus-Judge-style multi-caliber"),
            ("capacity",      "5-round cylinder", "Hard ceiling — track via ammo:chorus.shells in EntityStateEvents"),
            ("action",        "double-action revolver",                       null),
            ("grip",          "birds-head",                                    "Designed for one-handed use"),
            ("analogue",      "Taurus Judge",                                  "Real-world design reference"),
            ("handed",        "left",                                          "Cross-dominant from right-hand blade"),
            ("carry_position","low on left hip",                               null),
            ("manufacturer",  "Torii Security Group",                          null),
            ("model",         "TSS-3 'Chorus'",                                null),
        };
        foreach (var (k, v, note) in specs)
            result.SpecsWritten += await UpsertWeaponSpecAsync(db, ChorusWeaponId, k, v, note, ct);

        // 4) Kyle → Chorus link. Set GearEntityId on the matching gear row, or
        //    insert a new gear row when none exists.
        await LinkCharacterToWeaponAsync(db, KyleCharacterId, ChorusWeaponId, "Chorus", ct);

        // 5) Same favor for Silence (Kyle's blade) — Kyle's "primary weapon"
        //    pointer lives in CharacterBelongingsGear.Bucket = 'primary_weapon'
        //    after the 2026-05-08 scalar drop. Use that exact string to resolve
        //    to the right entity (there are multiple "Silence" weapons in canon
        //    — a CV-1 ECM rig, an Ouroboros XB-7 jammer, AND Kyle's katana —
        //    and only the katana matches).
        var kylePrimary = await db.CharacterBelongingsGear.AsNoTracking()
            .Where(g => g.CharacterId == KyleCharacterId && g.Bucket == "primary_weapon")
            .OrderBy(g => g.Position)
            .Select(g => g.GearName)
            .FirstOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(kylePrimary))
        {
            var silenceId = await db.Entities.AsNoTracking()
                .Where(e => e.IsActive && e.EntityType == "weapon" && e.Name == kylePrimary)
                .Select(e => (Guid?)e.Id)
                .FirstOrDefaultAsync(ct);
            if (silenceId.HasValue)
                await LinkCharacterToWeaponAsync(db, KyleCharacterId, silenceId.Value, "Silence", ct);
        }

        return result;
    }

    // ── Bulk LLM linker (every other firearm) ─────────────────────────────────

    /// <summary>
    /// Walk every weapon entity in the canon. For weapons that have no
    /// <see cref="Entities.WeaponAmmunitionType"/> rows yet, prompt the LLM with
    /// the weapon's description + the existing ammo catalog and persist the
    /// returned compatibility list. Ammunition records are created on demand
    /// when the LLM names a round we don't have an entity for.
    /// </summary>
    public async Task<LinkResult> LinkAllFirearmsAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var result = new LinkResult();
        await EnsureWeaponSpecsSchemaAsync(ct);
        await EnsureGearEntityIdColumnAsync(ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Skip melee — only firearms get ammo. Heuristic: Category contains
        // "rifle"/"pistol"/"shotgun"/"smg"/"carbine"/"revolver"/"handgun".
        var allWeapons = await db.Weapons.AsNoTracking().ToListAsync(ct);
        var firearms = allWeapons
            .Where(w => IsFirearm(w.Category) || IsFirearm(w.Name))
            .ToList();

        // Pull existing ammo names so the LLM can reuse rather than inventing.
        var existingAmmoCatalog = await db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "ammunition" && e.IsActive)
            .Select(e => e.Name)
            .ToListAsync(ct);

        var seenWithAmmo = await db.Set<WeaponAmmunitionType>().AsNoTracking()
            .Select(w => w.WeaponId)
            .Distinct()
            .ToListAsync(ct);
        var pending = firearms.Where(w => !seenWithAmmo.Contains(w.Id)).ToList();

        progress?.Report($"  scanning {pending.Count} firearms missing ammo links…");
        int idx = 0;
        foreach (var w in pending)
        {
            if (ct.IsCancellationRequested) break;
            idx++;
            result.WeaponsScanned++;
            try
            {
                var ammoNames = await ExtractAmmoForWeaponAsync(w, existingAmmoCatalog, ct);
                if (ammoNames.Count == 0) continue;

                int pos = 0;
                foreach (var name in ammoNames)
                {
                    var rec = await UpsertAmmunitionAsync(db, name,
                        description: $"Auto-linked from weapon '{w.Name}'.", tags: new[] { "auto_linked" }, ct);
                    if (rec.Created)
                    {
                        result.AmmunitionsCreated++;
                        existingAmmoCatalog.Add(name);
                    }
                    result.CompatibilityRowsAdded +=
                        await UpsertWeaponAmmoAsync(db, w.Id, rec.Id, name, pos++, ct);
                }
                progress?.Report($"  [{idx}/{pending.Count}] {w.Name}: {string.Join(", ", ammoNames)}");
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Ammo extraction failed for {Weapon}", w.Name);
                result.Errors.Add($"{w.Name}: {ex.Message}");
            }
        }
        return result;
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static readonly string[] FirearmHints =
    {
        "rifle","pistol","shotgun","smg","carbine","revolver","handgun","sniper",
        "machine gun","auto","bullpup","launcher","submachine","sidearm",
    };
    private static bool IsFirearm(string? s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        var l = s.ToLowerInvariant();
        return FirearmHints.Any(h => l.Contains(h));
    }

    private async Task<List<string>> ExtractAmmoForWeaponAsync(
        Weapon w, List<string> existingCatalog, CancellationToken ct)
    {
        var system =
            "You determine which ammunition cartridges a fictional firearm chambers. " +
            "You receive the weapon's description and the catalog of ammunition rounds " +
            "already in canon. Output ONLY a JSON array of round names. Reuse exact " +
            "names from the catalog when applicable; introduce new names ONLY when the " +
            "weapon clearly fires a round not in the catalog. Cap at 4 entries. " +
            "Examples: [\".45 Long Colt\", \".410 Shotshell\"], [\"5.56x45mm NATO\"], [].";

        var sb = new StringBuilder();
        sb.AppendLine($"WEAPON: {w.Name}");
        if (!string.IsNullOrWhiteSpace(w.Manufacturer))    sb.AppendLine($"MAKER: {w.Manufacturer}");
        if (!string.IsNullOrWhiteSpace(w.Category))        sb.AppendLine($"CATEGORY: {w.Category}");
        if (!string.IsNullOrWhiteSpace(w.Description))     sb.AppendLine($"DESCRIPTION: {Truncate(w.Description, 1000)}");
        if (!string.IsNullOrWhiteSpace(w.Specifications))  sb.AppendLine($"SPECS: {Truncate(w.Specifications, 800)}");
        sb.AppendLine();
        sb.AppendLine($"EXISTING AMMO CATALOG ({existingCatalog.Count} rounds): " +
            string.Join(", ", existingCatalog.Take(40)));
        sb.AppendLine();
        sb.AppendLine("Output: JSON array of round names.");

        string raw;
        try { raw = await llm.GenerateAsync(system, sb.ToString(), temperature: 0.1, maxTokens: 200, ct: ct); }
        catch (Exception ex)
        {
            log.LogDebug(ex, "LLM ammo-extraction call failed for {Name}", w.Name);
            return new();
        }
        return ParseStringArray(raw);
    }

    private static List<string> ParseStringArray(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new();
        var s = raw.IndexOf('['); var e = raw.LastIndexOf(']');
        if (s < 0 || e <= s) return new();
        try
        {
            using var doc = JsonDocument.Parse(raw[s..(e + 1)]);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return new();
            return doc.RootElement.EnumerateArray()
                .Where(el => el.ValueKind == JsonValueKind.String)
                .Select(el => el.GetString() ?? "")
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(4)
                .ToList();
        }
        catch { return new(); }
    }

    private record AmmoUpsertResult(Guid Id, bool Created);

    private async Task<AmmoUpsertResult> UpsertAmmunitionAsync(
        StreetSamuraiDbContext db, string name, string description, string[] tags, CancellationToken ct)
    {
        var slug = WorldGraphService.Slugify(name);
        var existing = await db.Entities
            .FirstOrDefaultAsync(e => e.EntityType == "ammunition" && (e.Name == name || e.Slug == slug), ct);
        if (existing != null) return new AmmoUpsertResult(existing.Id, Created: false);

        var id = Guid.CreateVersion7();
        db.Entities.Add(new Entity
        {
            Id          = id,
            EntityType  = "ammunition",
            Name        = name,
            Slug        = slug,
            Status      = "canon",
            Description = description,
            CreatedAt   = DateTime.UtcNow,
            ModifiedAt  = DateTime.UtcNow,
            IsActive    = true,
        });
        // Subtype row — Ammunition table has its own columns; minimal row is fine.
        db.Ammunitions.Add(new Ammunition { Id = id, Name = name });
        await db.SaveChangesAsync(ct);
        return new AmmoUpsertResult(id, Created: true);
    }

    private static async Task<int> UpsertWeaponAmmoAsync(StreetSamuraiDbContext db,
        Guid weaponId, Guid ammoId, string alias, int position, CancellationToken ct)
    {
        var existing = await db.Set<WeaponAmmunitionType>()
            .FirstOrDefaultAsync(x => x.WeaponId == weaponId && x.AmmunitionId == ammoId, ct);
        if (existing != null) return 0;
        db.Set<WeaponAmmunitionType>().Add(new WeaponAmmunitionType
        {
            WeaponId = weaponId, AmmunitionId = ammoId, Position = position, Alias = alias,
        });
        await db.SaveChangesAsync(ct);
        return 1;
    }

    private static async Task<int> UpsertWeaponSpecAsync(StreetSamuraiDbContext db,
        Guid weaponId, string specKey, string specValue, string? notes, CancellationToken ct)
    {
        var existing = await db.WeaponSpecs
            .FirstOrDefaultAsync(s => s.WeaponId == weaponId && s.SpecKey == specKey, ct);
        if (existing != null)
        {
            if (existing.SpecValue == specValue && existing.Notes == notes) return 0;
            existing.SpecValue = specValue;
            existing.Notes     = notes;
            await db.SaveChangesAsync(ct);
            return 1;
        }
        db.WeaponSpecs.Add(new WeaponSpec
        {
            WeaponId = weaponId, SpecKey = specKey, SpecValue = specValue, Notes = notes,
        });
        await db.SaveChangesAsync(ct);
        return 1;
    }

    private static async Task LinkCharacterToWeaponAsync(StreetSamuraiDbContext db,
        Guid characterId, Guid weaponId, string aliasContains, CancellationToken ct)
    {
        // 1) Find an existing CharacterBelongingsGear row whose name STARTS with
        //    the alias (case-insensitive). Anchored match prevents false positives
        //    like the Corundum Strop matching "Silence" because its description
        //    happens to mention Silence the blade.
        var prefix = aliasContains;
        var lowered = prefix.ToLowerInvariant();
        var matchedGear = await db.CharacterBelongingsGear
            .Where(g => g.CharacterId == characterId
                && g.GearName != null
                && g.GearName.ToLower().StartsWith(lowered))
            .FirstOrDefaultAsync(ct);
        if (matchedGear != null)
        {
            if (matchedGear.GearEntityId != weaponId)
            {
                matchedGear.GearEntityId = weaponId;
                await db.SaveChangesAsync(ct);
            }
            return;
        }
        // 2) No matching row: insert a fresh signature_gear entry with the FK.
        var pos = await db.CharacterBelongingsGear
            .Where(g => g.CharacterId == characterId && g.Bucket == "signature_gear")
            .CountAsync(ct);
        db.CharacterBelongingsGear.Add(new CharacterBelongingsGear
        {
            CharacterId  = characterId,
            Bucket       = "signature_gear",
            Position     = pos,
            GearName     = aliasContains,
            GearEntityId = weaponId,
        });
        await db.SaveChangesAsync(ct);
    }

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..n];
}
