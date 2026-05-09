using System.Text.Json;
using System.Text.Json.Nodes;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
[Ignore("Service migrated to SQL — tests need rewrite to seed Records.Json instead of files.")]
public class FixIdentityCorruptionServiceTests
{
    private string tempDir = "";
    private string entityDir = "";
    private FixIdentityCorruptionService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir   = Path.Combine(Path.GetTempPath(), $"ss_wiki_{Guid.NewGuid():N}");
        entityDir = Path.Combine(tempDir, "engine_data", "people");
        Directory.CreateDirectory(entityDir);
        var paths = new TestPathProviderWithRoot(tempDir);
        svc = new FixIdentityCorruptionService(TestDbFactory.For(paths, "identity"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private string WriteEntity(object data)
    {
        var path = Path.Combine(entityDir, $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(data));
        return path;
    }

    private JsonObject ReadEntity(string path) =>
        JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? throw new InvalidOperationException();

    // ── Scalar fields: [[Name|id]] → Name ─────────────────────────────────────

    [Test]
    public async Task Clean_NameWithWikiPipe_ExtractsDisplayText()
    {
        var path = WriteEntity(new { name = "[[Kira Voss|kira_voss_001]]", description = "A runner." });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["name"]?.GetValue<string>(), Is.EqualTo("Kira Voss"));
    }

    [Test]
    public async Task Clean_NameWithWikiNoPipe_ExtractsName()
    {
        var path = WriteEntity(new { name = "[[Kira Voss]]", description = "A runner." });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["name"]?.GetValue<string>(), Is.EqualTo("Kira Voss"));
    }

    [Test]
    public async Task Clean_TitleWithWikiPipe_Cleaned()
    {
        var path = WriteEntity(new { name = "Person", title = "[[Director|dir_001]]" });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["title"]?.GetValue<string>(), Is.EqualTo("Director"));
    }

    [Test]
    public async Task Clean_CodenameWithWikiPipe_Cleaned()
    {
        var path = WriteEntity(new { name = "Agent", codename = "[[Ghost|ghost_42]]" });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["codename"]?.GetValue<string>(), Is.EqualTo("Ghost"));
    }

    [Test]
    public async Task Clean_HeadlineField_Cleaned()
    {
        var path = WriteEntity(new { name = "Article", headline = "[[Corp Breach Exposed|article_9]]" });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["headline"]?.GetValue<string>(), Is.EqualTo("Corp Breach Exposed"));
    }

    [Test]
    public async Task Clean_ProductNameField_Cleaned()
    {
        var path = WriteEntity(new { name = "Widget", product_name = "[[UltraWidget|prod_77]]" });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["product_name"]?.GetValue<string>(), Is.EqualTo("UltraWidget"));
    }

    // ── Array fields: aliases, common_names ──────────────────────────────────

    [Test]
    public async Task Clean_AliasesArray_AllElementsCleaned()
    {
        var path = WriteEntity(new
        {
            name = "Sable",
            aliases = new[] { "[[Sable Orr|sable_orr]]", "[[The Ghost]]", "Plain Alias" }
        });

        await svc.RunAsync();

        var obj = ReadEntity(path);
        var arr = obj["aliases"] as JsonArray;
        Assert.That(arr?[0]?.GetValue<string>(), Is.EqualTo("Sable Orr"));
        Assert.That(arr?[1]?.GetValue<string>(), Is.EqualTo("The Ghost"));
        Assert.That(arr?[2]?.GetValue<string>(), Is.EqualTo("Plain Alias"));
    }

    [Test]
    public async Task Clean_CommonNamesArray_Cleaned()
    {
        var path = WriteEntity(new
        {
            name = "Thing",
            common_names = new[] { "[[The Widget|widget_01]]" }
        });

        await svc.RunAsync();

        var obj = ReadEntity(path);
        var arr = obj["common_names"] as JsonArray;
        Assert.That(arr?[0]?.GetValue<string>(), Is.EqualTo("The Widget"));
    }

    // ── Non-identity fields NOT cleaned ──────────────────────────────────────

    [Test]
    public async Task Clean_DescriptionField_NotCleaned()
    {
        var path = WriteEntity(new { name = "Person", description = "Reports to [[Kira Voss|kira_001]]." });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["description"]?.GetValue<string>(), Is.EqualTo("Reports to [[Kira Voss|kira_001]]."));
    }

    [Test]
    public async Task Clean_TagsField_NotCleaned()
    {
        var path = WriteEntity(new { name = "Entity", tags = new[] { "[[runner]]" } });

        await svc.RunAsync();

        var obj = ReadEntity(path);
        var tags = obj["tags"] as JsonArray;
        Assert.That(tags?[0]?.GetValue<string>(), Is.EqualTo("[[runner]]"));
    }

    // ── Already clean fields not modified ────────────────────────────────────

    [Test]
    public async Task Clean_AlreadyCleanName_NoChange()
    {
        var path = WriteEntity(new { name = "Clean Name", description = "Nothing here." });
        var before = File.ReadAllText(path);

        await svc.RunAsync();

        Assert.That(File.ReadAllText(path), Is.EqualTo(before));
    }

    // ── Multiple wiki links in one field ──────────────────────────────────────

    [Test]
    public async Task Clean_MultipleWikiLinksInName_AllReplaced()
    {
        // Unusual but safe to handle: "[[First]] and [[Second|id]]"
        var path = WriteEntity(new { name = "[[First]] and [[Second|id]]" });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["name"]?.GetValue<string>(), Is.EqualTo("First and Second"));
    }

    // ── Result counting ───────────────────────────────────────────────────────

    [Test]
    public async Task Run_TwoCorruptedFields_CountsTwo()
    {
        WriteEntity(new { name = "[[A|a1]]", title = "[[Director|d1]]" });

        var result = await svc.RunAsync();

        Assert.That(result.ChangesApplied, Is.EqualTo(2));
    }
}
