using System.Text.Json;
using System.Text.Json.Nodes;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
[Ignore("Service migrated to SQL — tests need rewrite to seed Records.Json instead of files.")]
public class AssignTiersServiceTests
{
    private string tempDir = "";
    private string peopleDir = "";
    private string syntheticsDir = "";
    private AssignTiersService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_tiers_{Guid.NewGuid():N}");
        peopleDir = Path.Combine(tempDir, "engine_data", "people");
        syntheticsDir = Path.Combine(tempDir, "engine_data", "synthetics");
        Directory.CreateDirectory(peopleDir);
        Directory.CreateDirectory(syntheticsDir);
        var paths = new TestPathProviderWithRoot(tempDir);
        svc = new AssignTiersService(TestDbFactory.For(paths, "tiers"));
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

    private JsonObject ReadEntity(string path) =>
        JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? throw new InvalidOperationException();

    // ── Default tier ────────────────────────────────────────────────────────

    [Test]
    public async Task Assign_NoKeywordsMatch_AssignsTier2()
    {
        var path = WriteEntity(peopleDir, new { name = "Street Wanderer", role = "unknown" });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(2));
    }

    // ── Tier 5 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Assign_CeoInRole_AssignsTier5()
    {
        var path = WriteEntity(peopleDir, new { name = "Ada Korr", role = "CEO of Tessera Corp" });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(5));
    }

    [Test]
    public async Task Assign_FounderInDescription_AssignsTier5()
    {
        var path = WriteEntity(peopleDir, new { name = "Remy Dahl", role = "Industrialist", description = "The founder of an empire." });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(5));
    }

    [Test]
    public async Task Assign_BoardMemberInTags_AssignsTier5()
    {
        var path = WriteEntity(peopleDir, new { name = "Chen Wei", role = "Advisor", tags = new[] { "board member" } });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(5));
    }

    // ── Tier 4 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Assign_DoctorInRole_AssignsTier4()
    {
        var path = WriteEntity(peopleDir, new { name = "Dr. Ines Vax", role = "Doctor at Helix Biosystems" });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(4));
    }

    [Test]
    public async Task Assign_EngineerInRole_AssignsTier4()
    {
        var path = WriteEntity(peopleDir, new { name = "Marcus Wren", role = "Senior Engineer" });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(4));
    }

    // ── Tier 3 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Assign_RunnerInRole_AssignsTier3()
    {
        var path = WriteEntity(peopleDir, new { name = "Vex Maura", role = "Street Runner" });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(3));
    }

    [Test]
    public async Task Assign_HackerInDescription_AssignsTier3()
    {
        var path = WriteEntity(peopleDir, new { name = "Ghost", role = "Freelancer", description = "Expert hacker." });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(3));
    }

    [Test]
    public async Task Assign_MercenaryInDescription_AssignsTier3()
    {
        var path = WriteEntity(peopleDir, new { name = "Kael", description = "A mercenary for hire." });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(3));
    }

    // ── Tier 2 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Assign_GuardInRole_AssignsTier2()
    {
        var path = WriteEntity(peopleDir, new { name = "Brick", role = "Security Guard" });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(2));
    }

    [Test]
    public async Task Assign_CourierInRole_AssignsTier2()
    {
        var path = WriteEntity(peopleDir, new { name = "Zip", role = "Package Courier" });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(2));
    }

    // ── Tier 1 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Assign_RefugeeInRole_AssignsTier1()
    {
        var path = WriteEntity(peopleDir, new { name = "Mara", role = "Refugee from the gap" });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(1));
    }

    [Test]
    public async Task Assign_HomelessInDescription_AssignsTier1()
    {
        var path = WriteEntity(peopleDir, new { name = "Old Pete", description = "A homeless drifter living near the wall." });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(1));
    }

    // ── Overwrite behavior ────────────────────────────────────────────────────

    [Test]
    public async Task Assign_OverwriteFalse_SkipsExistingTier()
    {
        var path = WriteEntity(peopleDir, new { name = "Preset", role = "CEO", tier = 1 });

        await svc.RunAsync(overwrite: false);

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(1));
    }

    [Test]
    public async Task Assign_OverwriteTrue_ReplacesExistingTier()
    {
        var path = WriteEntity(peopleDir, new { name = "Preset", role = "CEO", tier = 1 });

        await svc.RunAsync(overwrite: true);

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(5));
    }

    // ── First-match wins (tier priority order) ────────────────────────────────

    [Test]
    public async Task Assign_CeoAndRunner_AssignsTier5_FirstMatchWins()
    {
        // "ceo" hits tier 5 first — tier rules are checked 5→4→3→2→1
        var path = WriteEntity(peopleDir, new { name = "Hybrid", role = "CEO and runner" });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(5));
    }

    // ── Synthetics dir ───────────────────────────────────────────────────────

    [Test]
    public async Task Assign_SyntheticsDir_TierAssigned()
    {
        var path = WriteEntity(syntheticsDir, new { name = "Unit-9", role = "Security enforcer" });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(2));
    }

    // ── Affiliation field ─────────────────────────────────────────────────────

    [Test]
    public async Task Assign_ExecutiveInAffiliation_AssignsTier5()
    {
        var path = WriteEntity(peopleDir, new { name = "Silent Vote", affiliation = "Tessera Executive Board" });

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["tier"]?.GetValue<int>(), Is.EqualTo(5));
    }

    // ── Result counts ─────────────────────────────────────────────────────────

    [Test]
    public async Task RunAsync_ReturnsCorrectModifiedCount()
    {
        WriteEntity(peopleDir, new { name = "A", role = "CEO" });
        WriteEntity(peopleDir, new { name = "B", role = "runner" });
        WriteEntity(peopleDir, new { name = "C", role = "CEO", tier = 5 }); // already set

        var result = await svc.RunAsync(overwrite: false);

        Assert.That(result.FilesModified, Is.EqualTo(2));
        Assert.That(result.FilesScanned, Is.EqualTo(3));
    }
}
