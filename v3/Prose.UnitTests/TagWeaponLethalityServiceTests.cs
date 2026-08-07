using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Rewritten for the 2026-05-08 JSON→SQL canon migration: TagWeaponLethalityService
/// now scans Records.Json blobs (via DataScanUtility), not engine_data/*.json files.
/// Seeds an Entity + Record row per fixture instead of writing a file.
/// </summary>
[TestFixture]
public class TagWeaponLethalityServiceTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;
    private TagWeaponLethalityService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_lethality_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "lethality");
        svc = new TagWeaponLethalityService(factory);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private Guid SeedEntity(string entityType, object data)
    {
        var id = Guid.NewGuid();
        using var db = factory.CreateDbContext();
        db.Entities.Add(new Entity
        {
            Id         = id,
            EntityType = entityType,
            Name       = "Test Entity",
            Slug       = $"test-entity-{id:N}",
            Status     = "canon",
            IsActive   = true,
            CreatedAt  = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
        });
        db.Records.Add(new Record { EntityId = id, Json = JsonSerializer.Serialize(data) });
        db.SaveChanges();
        return id;
    }

    private Guid SeedWeapon(object data) => SeedEntity("weapon", data);
    private Guid SeedAmmo(object data) => SeedEntity("ammunition", data);

    private List<string> ReadTags(Guid id)
    {
        using var db = factory.CreateDbContext();
        var row = db.Records.First(r => r.EntityId == id);
        var obj = JsonNode.Parse(row.Json) as JsonObject;
        if (obj?["tags"] is not JsonArray arr) return [];
        return arr.Select(n => n?.GetValue<string>() ?? "").ToList();
    }

    // ── Lethal category defaults ─────────────────────────────────────────────

    [Test]
    public async Task Tag_FirearmCategory_IsLethal()
    {
        var id = SeedWeapon(new { name = "Pulse 9mm", category = "Firearm", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(id), Does.Contain("lethal"));
    }

    [Test]
    public async Task Tag_BladeCategory_IsLethal()
    {
        var id = SeedWeapon(new { name = "Mono-Blade", category = "blade", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(id), Does.Contain("lethal"));
    }

    [Test]
    public async Task Tag_ExplosiveCategory_IsLethal()
    {
        var id = SeedWeapon(new { name = "Frag Grenade", category = "explosive", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(id), Does.Contain("lethal"));
    }

    // ── Less-lethal category defaults ─────────────────────────────────────────

    [Test]
    public async Task Tag_TaserCategory_IsLessLethal()
    {
        var id = SeedWeapon(new { name = "Volt Prod", category = "taser", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(id), Does.Contain("less_lethal"));
    }

    [Test]
    public async Task Tag_StunWeaponCategory_IsLessLethal()
    {
        var id = SeedWeapon(new { name = "Shocker", category = "stun weapon", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(id), Does.Contain("less_lethal"));
    }

    // ── Non-lethal category defaults ─────────────────────────────────────────

    [Test]
    public async Task Tag_TrackerCategory_IsNonLethal()
    {
        var id = SeedWeapon(new { name = "Dart Tag", category = "tracker", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(id), Does.Contain("non_lethal"));
    }

    // ── Keyword overrides ─────────────────────────────────────────────────────

    [Test]
    public async Task Tag_StunKeywordInDescription_IsLessLethal_OverridesFirearmCategory()
    {
        var id = SeedWeapon(new { name = "Stun Pistol", category = "pistol", description = "Fires stun rounds.", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(id), Does.Contain("less_lethal"));
        Assert.That(ReadTags(id), Does.Not.Contain("lethal"));
    }

    [Test]
    public async Task Tag_SmokeKeyword_IsNonLethal()
    {
        var id = SeedWeapon(new { name = "Smoke Grenade", category = "grenade", description = "Emits dense smoke for cover.", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(id), Does.Contain("non_lethal"));
    }

    [Test]
    public async Task Tag_ArmorPiercingKeyword_IsLethal()
    {
        var id = SeedWeapon(new { name = "AP Round", category = "ammunition", description = "armor-piercing projectile.", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(id), Does.Contain("lethal"));
    }

    // ── Non-lethal keyword wins over less-lethal keyword ──────────────────────

    [Test]
    public async Task Tag_NonLethalKeywordBeatsLessLethal()
    {
        // "smoke" (non-lethal) beats "stun" (less-lethal) because non-lethal is checked first
        var id = SeedWeapon(new { name = "Smoke Stunner", description = "Smoke and stun combined.", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(id), Does.Contain("non_lethal"));
        Assert.That(ReadTags(id), Does.Not.Contain("less_lethal"));
    }

    // ── Unknown category defaults to lethal ───────────────────────────────────

    [Test]
    public async Task Tag_UnknownCategory_DefaultsToLethal()
    {
        var id = SeedWeapon(new { name = "Mystery Device", category = "unknown-type", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(id), Does.Contain("lethal"));
    }

    // ── Overwrite behavior ────────────────────────────────────────────────────

    [Test]
    public async Task Tag_ExistingLethalityTag_SkippedByDefault()
    {
        var id = SeedWeapon(new { name = "Tagged Gun", category = "firearm", tags = new[] { "non_lethal" } });

        await svc.RunAsync(overwrite: false);

        Assert.That(ReadTags(id), Does.Contain("non_lethal"));
        Assert.That(ReadTags(id), Does.Not.Contain("lethal"));
    }

    [Test]
    public async Task Tag_ExistingLethalityTag_OverwrittenWhenEnabled()
    {
        var id = SeedWeapon(new { name = "Tagged Gun", category = "firearm", tags = new[] { "non_lethal" } });

        await svc.RunAsync(overwrite: true);

        Assert.That(ReadTags(id), Does.Contain("lethal"));
        Assert.That(ReadTags(id), Does.Not.Contain("non_lethal"));
    }

    // ── Tag replacement (only one lethality tag) ──────────────────────────────

    [Test]
    public async Task Tag_ReplacesOldLethalityTag_NoDuplicates()
    {
        var id = SeedWeapon(new { name = "Confusing Gun", category = "taser", tags = new[] { "lethal", "other-tag" } });

        await svc.RunAsync(overwrite: true);

        var tags = ReadTags(id);
        Assert.That(tags.Count(t => t == "lethal" || t == "less_lethal" || t == "non_lethal"), Is.EqualTo(1));
        Assert.That(tags, Does.Contain("less_lethal"));
        Assert.That(tags, Does.Contain("other-tag"));
    }

    // ── Ammunition dir ───────────────────────────────────────────────────────

    [Test]
    public async Task Tag_AmmoDir_TagsAssigned()
    {
        var id = SeedAmmo(new { name = "Hollow Points", category = "pistol", description = "hollow-point rounds.", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(id), Does.Contain("lethal"));
    }
}
