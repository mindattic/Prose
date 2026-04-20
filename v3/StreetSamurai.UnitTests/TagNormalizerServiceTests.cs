using System.Text.Json;
using System.Text.Json.Nodes;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class TagNormalizerServiceTests
{
    private string tempDir = "";
    private string peopleDir = "";
    private string syntheticsDir = "";
    private string weaponsDir = "";
    private TagNormalizerService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_tags_{Guid.NewGuid():N}");
        peopleDir     = Path.Combine(tempDir, "engine_data", "people");
        syntheticsDir = Path.Combine(tempDir, "engine_data", "synthetics");
        weaponsDir    = Path.Combine(tempDir, "engine_data", "weaponry");
        Directory.CreateDirectory(peopleDir);
        Directory.CreateDirectory(syntheticsDir);
        Directory.CreateDirectory(weaponsDir);
        svc = new TagNormalizerService(new TestPathProviderWithRoot(tempDir));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private string WriteEntity(string dir, object data)
    {
        var id = Guid.NewGuid().ToString("N");
        var path = Path.Combine(dir, $"{id}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(data));
        return path;
    }

    private List<string> ReadTags(string path)
    {
        var obj = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
        if (obj?["tags"] is not JsonArray arr) return [];
        return arr.Select(n => n?.GetValue<string>() ?? "").ToList();
    }

    // ── Category tag injection ────────────────────────────────────────────────

    [Test]
    public async Task Process_PeopleDir_AddsCategoryTagPerson()
    {
        var path = WriteEntity(peopleDir, new { name = "Vex", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(path), Does.Contain("person"));
    }

    [Test]
    public async Task Process_SyntheticsDir_AddsCategoryTagSynthetic()
    {
        var path = WriteEntity(syntheticsDir, new { name = "Unit-3", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(path), Does.Contain("synthetic"));
    }

    [Test]
    public async Task Process_WeaponsDir_NoCategoryTagAdded()
    {
        var path = WriteEntity(weaponsDir, new { name = "Blade", tags = new[] { "lethal" } });

        await svc.RunAsync();

        var tags = ReadTags(path);
        Assert.That(tags, Does.Not.Contain("person"));
        Assert.That(tags, Does.Not.Contain("synthetic"));
    }

    [Test]
    public async Task Process_PeopleDir_PersonTagNotDuplicated()
    {
        var path = WriteEntity(peopleDir, new { name = "Vex", tags = new[] { "person", "runner" } });

        await svc.RunAsync();

        Assert.That(ReadTags(path).Count(t => t == "person"), Is.EqualTo(1));
    }

    [Test]
    public async Task Process_CategoryTagDisabled_NoCategoryTagAdded()
    {
        var path = WriteEntity(peopleDir, new { name = "Vex", tags = Array.Empty<string>() });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false));

        Assert.That(ReadTags(path), Does.Not.Contain("person"));
    }

    // ── Lowercase normalization ───────────────────────────────────────────────

    [Test]
    public async Task Process_UpperCaseTags_AreLowercased()
    {
        var path = WriteEntity(weaponsDir, new { name = "Spike", tags = new[] { "Lethal", "MELEE" } });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false, ValidateKeywords: false));

        var tags = ReadTags(path);
        Assert.That(tags, Does.Contain("lethal"));
        Assert.That(tags, Does.Contain("melee"));
        Assert.That(tags, Does.Not.Contain("Lethal"));
    }

    // ── Deduplication ────────────────────────────────────────────────────────

    [Test]
    public async Task Process_DuplicateTags_AreRemoved()
    {
        var path = WriteEntity(weaponsDir, new { name = "Spike", tags = new[] { "lethal", "lethal", "melee" } });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false, ValidateKeywords: false));

        var tags = ReadTags(path);
        Assert.That(tags.Count(t => t == "lethal"), Is.EqualTo(1));
    }

    [Test]
    public async Task Process_CaseInsensitiveDuplicates_AreCollapsed()
    {
        var path = WriteEntity(weaponsDir, new { name = "Spike", tags = new[] { "lethal", "Lethal" } });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false, ValidateKeywords: false));

        Assert.That(ReadTags(path), Has.Count.EqualTo(1));
    }

    // ── Keyword validation ────────────────────────────────────────────────────

    [Test]
    public async Task Process_TagKeywordMissing_TagIsRemoved()
    {
        // "war" tag requires war/battle/combat/etc in entity text — none here
        var path = WriteEntity(weaponsDir, new { name = "Paperclip", description = "Mundane office supply.", tags = new[] { "war" } });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false));

        Assert.That(ReadTags(path), Does.Not.Contain("war"));
    }

    [Test]
    public async Task Process_TagKeywordPresent_TagIsKept()
    {
        var path = WriteEntity(weaponsDir, new { name = "Spike", description = "Used in battle and combat.", tags = new[] { "war" } });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false));

        Assert.That(ReadTags(path), Does.Contain("war"));
    }

    [Test]
    public async Task Process_UnknownTag_AlwaysKept()
    {
        // Tags not in the keyword dict are kept unconditionally
        var path = WriteEntity(weaponsDir, new { name = "Spike", description = "No relevant text.", tags = new[] { "my-custom-tag" } });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false));

        Assert.That(ReadTags(path), Does.Contain("my-custom-tag"));
    }

    [Test]
    public async Task Process_ValidationDisabled_TagsNotStripped()
    {
        var path = WriteEntity(weaponsDir, new { name = "Spike", description = "Nothing relevant.", tags = new[] { "war" } });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false, ValidateKeywords: false));

        Assert.That(ReadTags(path), Does.Contain("war"));
    }

    [Test]
    public async Task Process_HackingTagWithCyber_Kept()
    {
        var path = WriteEntity(weaponsDir, new { name = "Brain Tap", description = "Cyber intrusion tool for direct neural hacking.", tags = new[] { "hacking" } });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false));

        Assert.That(ReadTags(path), Does.Contain("hacking"));
    }

    // ── Empty tags array ─────────────────────────────────────────────────────

    [Test]
    public async Task Process_EmptyTagsArray_NoError()
    {
        var path = WriteEntity(weaponsDir, new { name = "Spike", tags = Array.Empty<string>() });

        Assert.DoesNotThrowAsync(async () => await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false)));
    }

    [Test]
    public async Task Process_NoTagsField_NoError()
    {
        var path = WriteEntity(weaponsDir, new { name = "Spike" });

        Assert.DoesNotThrowAsync(async () => await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false)));
    }
}
