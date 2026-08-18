using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Rewritten for the 2026-05-08 JSON→SQL canon migration: TagNormalizerService now
/// scans Records.Json blobs (via DataScanUtility), not engine_data/*.json files.
/// Seeds an Entity + Record row per fixture instead of writing a file.
/// </summary>
[TestFixture]
public class TagNormalizerServiceTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;
    private TagNormalizerService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_tags_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "tags");
        svc = new TagNormalizerService(factory);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    /// <summary>Seeds an Entity + Records.Json row and returns the EntityId for re-reading.</summary>
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
            
            CreatedAt  = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
        });
        db.Records.Add(new Record { EntityId = id, Json = JsonSerializer.Serialize(data) });
        db.SaveChanges();
        return id;
    }

    private List<string> ReadTags(Guid id)
    {
        using var db = factory.CreateDbContext();
        var row = db.Records.First(r => r.EntityId == id);
        var obj = JsonNode.Parse(row.Json) as JsonObject;
        if (obj?["tags"] is not JsonArray arr) return [];
        return arr.Select(n => n?.GetValue<string>() ?? "").ToList();
    }

    // ── Category tag injection ────────────────────────────────────────────────

    [Test]
    public async Task Process_PeopleDir_AddsCategoryTagPerson()
    {
        var id = SeedEntity("character", new { name = "Vex", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(id), Does.Contain("person"));
    }

    [Test]
    public async Task Process_SyntheticsDir_AddsCategoryTagSynthetic()
    {
        var id = SeedEntity("synthetic", new { name = "Unit-3", tags = Array.Empty<string>() });

        await svc.RunAsync();

        Assert.That(ReadTags(id), Does.Contain("synthetic"));
    }

    [Test]
    public async Task Process_WeaponsDir_NoCategoryTagAdded()
    {
        var id = SeedEntity("weapon", new { name = "Blade", tags = new[] { "lethal" } });

        await svc.RunAsync();

        var tags = ReadTags(id);
        Assert.That(tags, Does.Not.Contain("person"));
        Assert.That(tags, Does.Not.Contain("synthetic"));
    }

    [Test]
    public async Task Process_PeopleDir_PersonTagNotDuplicated()
    {
        var id = SeedEntity("character", new { name = "Vex", tags = new[] { "person", "runner" } });

        await svc.RunAsync();

        Assert.That(ReadTags(id).Count(t => t == "person"), Is.EqualTo(1));
    }

    [Test]
    public async Task Process_CategoryTagDisabled_NoCategoryTagAdded()
    {
        var id = SeedEntity("character", new { name = "Vex", tags = Array.Empty<string>() });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false));

        Assert.That(ReadTags(id), Does.Not.Contain("person"));
    }

    // ── Lowercase normalization ───────────────────────────────────────────────

    [Test]
    public async Task Process_UpperCaseTags_AreLowercased()
    {
        var id = SeedEntity("weapon", new { name = "Spike", tags = new[] { "Lethal", "MELEE" } });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false, ValidateKeywords: false));

        var tags = ReadTags(id);
        Assert.That(tags, Does.Contain("lethal"));
        Assert.That(tags, Does.Contain("melee"));
        Assert.That(tags, Does.Not.Contain("Lethal"));
    }

    // ── Deduplication ────────────────────────────────────────────────────────

    [Test]
    public async Task Process_DuplicateTags_AreRemoved()
    {
        var id = SeedEntity("weapon", new { name = "Spike", tags = new[] { "lethal", "lethal", "melee" } });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false, ValidateKeywords: false));

        var tags = ReadTags(id);
        Assert.That(tags.Count(t => t == "lethal"), Is.EqualTo(1));
    }

    [Test]
    public async Task Process_CaseInsensitiveDuplicates_AreCollapsed()
    {
        var id = SeedEntity("weapon", new { name = "Spike", tags = new[] { "lethal", "Lethal" } });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false, ValidateKeywords: false));

        Assert.That(ReadTags(id), Has.Count.EqualTo(1));
    }

    // ── Keyword validation ────────────────────────────────────────────────────

    [Test]
    public async Task Process_TagKeywordMissing_TagIsRemoved()
    {
        // "war" tag requires war/battle/combat/etc in entity text — none here
        var id = SeedEntity("weapon", new { name = "Paperclip", description = "Mundane office supply.", tags = new[] { "war" } });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false));

        Assert.That(ReadTags(id), Does.Not.Contain("war"));
    }

    [Test]
    public async Task Process_TagKeywordPresent_TagIsKept()
    {
        var id = SeedEntity("weapon", new { name = "Spike", description = "Used in battle and combat.", tags = new[] { "war" } });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false));

        Assert.That(ReadTags(id), Does.Contain("war"));
    }

    [Test]
    public async Task Process_UnknownTag_AlwaysKept()
    {
        // Tags not in the keyword dict are kept unconditionally
        var id = SeedEntity("weapon", new { name = "Spike", description = "No relevant text.", tags = new[] { "my-custom-tag" } });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false));

        Assert.That(ReadTags(id), Does.Contain("my-custom-tag"));
    }

    [Test]
    public async Task Process_ValidationDisabled_TagsNotStripped()
    {
        var id = SeedEntity("weapon", new { name = "Spike", description = "Nothing relevant.", tags = new[] { "war" } });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false, ValidateKeywords: false));

        Assert.That(ReadTags(id), Does.Contain("war"));
    }

    [Test]
    public async Task Process_HackingTagWithCyber_Kept()
    {
        var id = SeedEntity("weapon", new { name = "Brain Tap", description = "Cyber intrusion tool for direct neural hacking.", tags = new[] { "hacking" } });

        await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false));

        Assert.That(ReadTags(id), Does.Contain("hacking"));
    }

    // ── Empty tags array ─────────────────────────────────────────────────────

    [Test]
    public void Process_EmptyTagsArray_NoError()
    {
        SeedEntity("weapon", new { name = "Spike", tags = Array.Empty<string>() });

        Assert.DoesNotThrowAsync(async () => await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false)));
    }

    [Test]
    public void Process_NoTagsField_NoError()
    {
        SeedEntity("weapon", new { name = "Spike" });

        Assert.DoesNotThrowAsync(async () => await svc.RunAsync(new TagNormalizerService.Options(AddCategoryTags: false)));
    }
}
