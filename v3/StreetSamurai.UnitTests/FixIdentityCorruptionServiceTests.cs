using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Rewritten for the 2026-05-08 JSON→SQL canon migration: FixIdentityCorruptionService
/// now scans Records.Json blobs (via DataScanUtility), not engine_data/*.json files.
/// Seeds an Entity + Record row per fixture instead of writing a file.
/// </summary>
[TestFixture]
public class FixIdentityCorruptionServiceTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<StreetSamuraiDbContext> factory = null!;
    private FixIdentityCorruptionService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_wiki_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "identity");
        svc = new FixIdentityCorruptionService(factory);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private Guid SeedEntity(object data)
    {
        var id = Guid.NewGuid();
        using var db = factory.CreateDbContext();
        db.Entities.Add(new Entity
        {
            Id         = id,
            EntityType = "character",
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

    private JsonObject ReadEntity(Guid id)
    {
        using var db = factory.CreateDbContext();
        var row = db.Records.First(r => r.EntityId == id);
        return JsonNode.Parse(row.Json) as JsonObject ?? throw new InvalidOperationException();
    }

    // ── Scalar fields: [[Name|id]] → Name ─────────────────────────────────────

    [Test]
    public async Task Clean_NameWithWikiPipe_ExtractsDisplayText()
    {
        var id = SeedEntity(new { name = "[[Kira Voss|kira_voss_001]]", description = "A runner." });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["name"]?.GetValue<string>(), Is.EqualTo("Kira Voss"));
    }

    [Test]
    public async Task Clean_NameWithWikiNoPipe_ExtractsName()
    {
        var id = SeedEntity(new { name = "[[Kira Voss]]", description = "A runner." });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["name"]?.GetValue<string>(), Is.EqualTo("Kira Voss"));
    }

    [Test]
    public async Task Clean_TitleWithWikiPipe_Cleaned()
    {
        var id = SeedEntity(new { name = "Person", title = "[[Director|dir_001]]" });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["title"]?.GetValue<string>(), Is.EqualTo("Director"));
    }

    [Test]
    public async Task Clean_CodenameWithWikiPipe_Cleaned()
    {
        var id = SeedEntity(new { name = "Agent", codename = "[[Ghost|ghost_42]]" });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["codename"]?.GetValue<string>(), Is.EqualTo("Ghost"));
    }

    [Test]
    public async Task Clean_HeadlineField_Cleaned()
    {
        var id = SeedEntity(new { name = "Article", headline = "[[Corp Breach Exposed|article_9]]" });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["headline"]?.GetValue<string>(), Is.EqualTo("Corp Breach Exposed"));
    }

    [Test]
    public async Task Clean_ProductNameField_Cleaned()
    {
        var id = SeedEntity(new { name = "Widget", product_name = "[[UltraWidget|prod_77]]" });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["product_name"]?.GetValue<string>(), Is.EqualTo("UltraWidget"));
    }

    // ── Array fields: aliases, common_names ──────────────────────────────────

    [Test]
    public async Task Clean_AliasesArray_AllElementsCleaned()
    {
        var id = SeedEntity(new
        {
            name = "Sable",
            aliases = new[] { "[[Sable Orr|sable_orr]]", "[[The Ghost]]", "Plain Alias" }
        });

        await svc.RunAsync();

        var obj = ReadEntity(id);
        var arr = obj["aliases"] as JsonArray;
        Assert.That(arr?[0]?.GetValue<string>(), Is.EqualTo("Sable Orr"));
        Assert.That(arr?[1]?.GetValue<string>(), Is.EqualTo("The Ghost"));
        Assert.That(arr?[2]?.GetValue<string>(), Is.EqualTo("Plain Alias"));
    }

    [Test]
    public async Task Clean_CommonNamesArray_Cleaned()
    {
        var id = SeedEntity(new
        {
            name = "Thing",
            common_names = new[] { "[[The Widget|widget_01]]" }
        });

        await svc.RunAsync();

        var obj = ReadEntity(id);
        var arr = obj["common_names"] as JsonArray;
        Assert.That(arr?[0]?.GetValue<string>(), Is.EqualTo("The Widget"));
    }

    // ── Non-identity fields NOT cleaned ──────────────────────────────────────

    [Test]
    public async Task Clean_DescriptionField_NotCleaned()
    {
        var id = SeedEntity(new { name = "Person", description = "Reports to [[Kira Voss|kira_001]]." });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["description"]?.GetValue<string>(), Is.EqualTo("Reports to [[Kira Voss|kira_001]]."));
    }

    [Test]
    public async Task Clean_TagsField_NotCleaned()
    {
        var id = SeedEntity(new { name = "Entity", tags = new[] { "[[runner]]" } });

        await svc.RunAsync();

        var obj = ReadEntity(id);
        var tags = obj["tags"] as JsonArray;
        Assert.That(tags?[0]?.GetValue<string>(), Is.EqualTo("[[runner]]"));
    }

    // ── Already clean fields not modified ────────────────────────────────────

    [Test]
    public async Task Clean_AlreadyCleanName_NoChange()
    {
        var id = SeedEntity(new { name = "Clean Name", description = "Nothing here." });
        var before = ReadEntity(id).ToJsonString();

        await svc.RunAsync();

        Assert.That(ReadEntity(id).ToJsonString(), Is.EqualTo(before));
    }

    // ── Multiple wiki links in one field ──────────────────────────────────────

    [Test]
    public async Task Clean_MultipleWikiLinksInName_AllReplaced()
    {
        // Unusual but safe to handle: "[[First]] and [[Second|id]]"
        var id = SeedEntity(new { name = "[[First]] and [[Second|id]]" });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["name"]?.GetValue<string>(), Is.EqualTo("First and Second"));
    }

    // ── Result counting ───────────────────────────────────────────────────────

    [Test]
    public async Task Run_TwoCorruptedFields_CountsTwo()
    {
        SeedEntity(new { name = "[[A|a1]]", title = "[[Director|d1]]" });

        var result = await svc.RunAsync();

        Assert.That(result.ChangesApplied, Is.EqualTo(2));
    }
}
