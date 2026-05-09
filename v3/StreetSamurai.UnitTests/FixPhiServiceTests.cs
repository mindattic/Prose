using System.Text.Json;
using System.Text.Json.Nodes;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
[Ignore("Service migrated to SQL — tests need rewrite to seed Records.Json instead of files.")]
public class FixPhiServiceTests
{
    private string tempDir = "";
    private string entityDir = "";
    private FixPhiService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir   = Path.Combine(Path.GetTempPath(), $"ss_phi_{Guid.NewGuid():N}");
        entityDir = Path.Combine(tempDir, "engine_data", "people");
        Directory.CreateDirectory(entityDir);
        var paths = new TestPathProviderWithRoot(tempDir);
        svc = new FixPhiService(TestDbFactory.For(paths, "phi"));
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

    // ── Phi → Quanta replacement ──────────────────────────────────────────────

    [Test]
    public async Task Run_PhiUppercaseInDescription_ReplacedWithQuanta()
    {
        var path = WriteEntity(new { name = "Test", description = "Costs 50 Phi per dose." });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["description"]?.GetValue<string>(), Is.EqualTo("Costs 50 Quanta per dose."));
    }

    [Test]
    public async Task Run_PhiLowercaseInDescription_ReplacedWithQuanta()
    {
        var path = WriteEntity(new { name = "Test", description = "Price is 100 phi." });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["description"]?.GetValue<string>(), Is.EqualTo("Price is 100 quanta."));
    }

    [Test]
    public async Task Run_PhiSymbolUnchanged()
    {
        var path = WriteEntity(new { name = "Test", description = "Costs Φ500 per unit." });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["description"]?.GetValue<string>(), Is.EqualTo("Costs Φ500 per unit."));
    }

    [Test]
    public async Task Run_MixedPhiAndSymbol_OnlyWordFormReplaced()
    {
        var path = WriteEntity(new { name = "Test", description = "Φ50 phi per transaction." });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["description"]?.GetValue<string>(), Is.EqualTo("Φ50 quanta per transaction."));
    }

    // ── Word-boundary: no partial replacements ────────────────────────────────

    [Test]
    public async Task Run_PhiAsPartOfWord_NotReplaced()
    {
        // "Philip" starts with "Phil" but \bPhi\b should not match "Philip"
        var path = WriteEntity(new { name = "Test", description = "Philip owes 50 Phi." });

        await svc.RunAsync();

        var desc = ReadEntity(path)["description"]?.GetValue<string>();
        Assert.That(desc, Does.Contain("Philip"));
        Assert.That(desc, Does.Contain("Quanta"));
    }

    // ── Skip identity fields ─────────────────────────────────────────────────

    [Test]
    public async Task Run_PhiInNameField_NotReplaced()
    {
        var path = WriteEntity(new { name = "Phi Korvann", description = "A person." });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["name"]?.GetValue<string>(), Is.EqualTo("Phi Korvann"));
    }

    [Test]
    public async Task Run_PhiInTitle_NotReplaced()
    {
        var path = WriteEntity(new { name = "Person", title = "Phi Chancellor", description = "Costs 10 Phi." });

        await svc.RunAsync();

        var obj = ReadEntity(path);
        Assert.That(obj["title"]?.GetValue<string>(), Is.EqualTo("Phi Chancellor"));
        Assert.That(obj["description"]?.GetValue<string>(), Is.EqualTo("Costs 10 Quanta."));
    }

    [Test]
    public async Task Run_PhiInCodename_NotReplaced()
    {
        var path = WriteEntity(new { name = "Agent", codename = "Operation Phi", description = "Budget is 200 phi." });

        await svc.RunAsync();

        var obj = ReadEntity(path);
        Assert.That(obj["codename"]?.GetValue<string>(), Is.EqualTo("Operation Phi"));
        Assert.That(obj["description"]?.GetValue<string>(), Is.EqualTo("Budget is 200 quanta."));
    }

    // ── Nested objects ───────────────────────────────────────────────────────

    [Test]
    public async Task Run_PhiInNestedObject_Replaced()
    {
        var path = WriteEntity(new { name = "Test", stats = new { note = "Earns 500 phi annually." } });

        await svc.RunAsync();

        var obj = ReadEntity(path);
        var stats = obj["stats"] as JsonObject;
        Assert.That(stats?["note"]?.GetValue<string>(), Is.EqualTo("Earns 500 quanta annually."));
    }

    // ── Arrays ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Run_PhiInArrayElement_Replaced()
    {
        var path = WriteEntity(new { name = "Test", notes = new[] { "Costs 100 phi.", "No phi here actually." } });

        await svc.RunAsync();

        var obj = ReadEntity(path);
        var notes = obj["notes"] as JsonArray;
        Assert.That(notes?[0]?.GetValue<string>(), Is.EqualTo("Costs 100 quanta."));
        Assert.That(notes?[1]?.GetValue<string>(), Is.EqualTo("No quanta here actually."));
    }

    // ── No false positives ────────────────────────────────────────────────────

    [Test]
    public async Task Run_NoPhiPresent_NoChange()
    {
        var path = WriteEntity(new { name = "Test", description = "Nothing currency-related here." });
        var before = File.ReadAllText(path);

        await svc.RunAsync();

        Assert.That(File.ReadAllText(path), Is.EqualTo(before));
    }

    // ── Result count ─────────────────────────────────────────────────────────

    [Test]
    public async Task Run_MultiplePhi_ReturnsCorrectChangeCount()
    {
        WriteEntity(new { name = "A", description = "100 Phi for service." });
        WriteEntity(new { name = "B", description = "No currency." });

        var result = await svc.RunAsync();

        Assert.That(result.ChangesApplied, Is.EqualTo(1));
        Assert.That(result.FilesModified, Is.EqualTo(1));
    }
}
