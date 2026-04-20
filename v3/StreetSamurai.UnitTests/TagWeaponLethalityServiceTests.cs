using System.Text.Json;
using System.Text.Json.Nodes;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class TagWeaponLethalityServiceTests
{
    private string tempDir = "";
    private string weaponsDir = "";
    private string ammoDir = "";
    private TagWeaponLethalityService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_lethality_{Guid.NewGuid():N}");
        weaponsDir = Path.Combine(tempDir, "engine_data", "weaponry");
        ammoDir    = Path.Combine(tempDir, "engine_data", "ammunition");
        Directory.CreateDirectory(weaponsDir);
        Directory.CreateDirectory(ammoDir);
        svc = new TagWeaponLethalityService(new TestPathProviderWithRoot(tempDir));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private string WriteWeapon(object data)
    {
        var path = Path.Combine(weaponsDir, $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(data));
        return path;
    }

    private string WriteAmmo(object data)
    {
        var path = Path.Combine(ammoDir, $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(data));
        return path;
    }

    private List<string> ReadTags(string path)
    {
        var obj = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
        if (obj?["tags"] is not JsonArray arr) return [];
        return arr.Select(n => n?.GetValue<string>() ?? "").ToList();
    }

    // ── Lethal category defaults ─────────────────────────────────────────────

    [Test]
    public async Task Tag_FirearmCategory_IsLethal()
    {
        var path = WriteWeapon(new { name = "Pulse 9mm", category = "Firearm", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(path), Does.Contain("lethal"));
    }

    [Test]
    public async Task Tag_BladeCategory_IsLethal()
    {
        var path = WriteWeapon(new { name = "Mono-Blade", category = "blade", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(path), Does.Contain("lethal"));
    }

    [Test]
    public async Task Tag_ExplosiveCategory_IsLethal()
    {
        var path = WriteWeapon(new { name = "Frag Grenade", category = "explosive", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(path), Does.Contain("lethal"));
    }

    // ── Less-lethal category defaults ─────────────────────────────────────────

    [Test]
    public async Task Tag_TaserCategory_IsLessLethal()
    {
        var path = WriteWeapon(new { name = "Volt Prod", category = "taser", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(path), Does.Contain("less_lethal"));
    }

    [Test]
    public async Task Tag_StunWeaponCategory_IsLessLethal()
    {
        var path = WriteWeapon(new { name = "Shocker", category = "stun weapon", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(path), Does.Contain("less_lethal"));
    }

    // ── Non-lethal category defaults ─────────────────────────────────────────

    [Test]
    public async Task Tag_TrackerCategory_IsNonLethal()
    {
        var path = WriteWeapon(new { name = "Dart Tag", category = "tracker", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(path), Does.Contain("non_lethal"));
    }

    // ── Keyword overrides ─────────────────────────────────────────────────────

    [Test]
    public async Task Tag_StunKeywordInDescription_IsLessLethal_OverridesFirearmCategory()
    {
        var path = WriteWeapon(new { name = "Stun Pistol", category = "pistol", description = "Fires stun rounds.", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(path), Does.Contain("less_lethal"));
        Assert.That(ReadTags(path), Does.Not.Contain("lethal"));
    }

    [Test]
    public async Task Tag_SmokeKeyword_IsNonLethal()
    {
        var path = WriteWeapon(new { name = "Smoke Grenade", category = "grenade", description = "Emits dense smoke for cover.", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(path), Does.Contain("non_lethal"));
    }

    [Test]
    public async Task Tag_ArmorPiercingKeyword_IsLethal()
    {
        var path = WriteWeapon(new { name = "AP Round", category = "ammunition", description = "armor-piercing projectile.", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(path), Does.Contain("lethal"));
    }

    // ── Non-lethal keyword wins over less-lethal keyword ──────────────────────

    [Test]
    public async Task Tag_NonLethalKeywordBeatsLessLethal()
    {
        // "smoke" (non-lethal) beats "stun" (less-lethal) because non-lethal is checked first
        var path = WriteWeapon(new { name = "Smoke Stunner", description = "Smoke and stun combined.", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(path), Does.Contain("non_lethal"));
        Assert.That(ReadTags(path), Does.Not.Contain("less_lethal"));
    }

    // ── Unknown category defaults to lethal ───────────────────────────────────

    [Test]
    public async Task Tag_UnknownCategory_DefaultsToLethal()
    {
        var path = WriteWeapon(new { name = "Mystery Device", category = "unknown-type", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(path), Does.Contain("lethal"));
    }

    // ── Overwrite behavior ────────────────────────────────────────────────────

    [Test]
    public async Task Tag_ExistingLethalityTag_SkippedByDefault()
    {
        var path = WriteWeapon(new { name = "Tagged Gun", category = "firearm", tags = new[] { "non_lethal" } });

        await svc.RunAsync(overwrite: false);

        Assert.That(ReadTags(path), Does.Contain("non_lethal"));
        Assert.That(ReadTags(path), Does.Not.Contain("lethal"));
    }

    [Test]
    public async Task Tag_ExistingLethalityTag_OverwrittenWhenEnabled()
    {
        var path = WriteWeapon(new { name = "Tagged Gun", category = "firearm", tags = new[] { "non_lethal" } });

        await svc.RunAsync(overwrite: true);

        Assert.That(ReadTags(path), Does.Contain("lethal"));
        Assert.That(ReadTags(path), Does.Not.Contain("non_lethal"));
    }

    // ── Tag replacement (only one lethality tag) ──────────────────────────────

    [Test]
    public async Task Tag_ReplacesOldLethalityTag_NoDuplicates()
    {
        var path = WriteWeapon(new { name = "Confusing Gun", category = "taser", tags = new[] { "lethal", "other-tag" } });

        await svc.RunAsync(overwrite: true);

        var tags = ReadTags(path);
        Assert.That(tags.Count(t => t == "lethal" || t == "less_lethal" || t == "non_lethal"), Is.EqualTo(1));
        Assert.That(tags, Does.Contain("less_lethal"));
        Assert.That(tags, Does.Contain("other-tag"));
    }

    // ── Ammunition dir ───────────────────────────────────────────────────────

    [Test]
    public async Task Tag_AmmoDir_TagsAssigned()
    {
        var path = WriteAmmo(new { name = "Hollow Points", category = "pistol", description = "hollow-point rounds.", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(path), Does.Contain("lethal"));
    }
}
