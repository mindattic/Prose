using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Rewritten for the 2026-05-08 JSON→SQL canon migration: AssignTiersService now
/// scans Records.Json blobs (via DataScanUtility), not engine_data/*.json files.
/// Seeds an Entity + Record row per fixture instead of writing a file.
/// </summary>
[TestFixture]
public class AssignTiersServiceTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;
    private AssignTiersService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_tiers_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "tiers");
        svc = new AssignTiersService(factory);
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

    private JsonObject ReadEntity(Guid id)
    {
        using var db = factory.CreateDbContext();
        var row = db.Records.First(r => r.EntityId == id);
        return JsonNode.Parse(row.Json) as JsonObject ?? throw new InvalidOperationException();
    }

    // ── Default tier ────────────────────────────────────────────────────────

    [Test]
    public async Task Assign_NoKeywordsMatch_AssignsTier2()
    {
        var id = SeedEntity("character", new { name = "Street Wanderer", role = "unknown" });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(2));
    }

    // ── Tier 5 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Assign_CeoInRole_AssignsTier5()
    {
        var id = SeedEntity("character", new { name = "Ada Korr", role = "CEO of Tessera Corp" });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(5));
    }

    [Test]
    public async Task Assign_FounderInDescription_AssignsTier5()
    {
        var id = SeedEntity("character", new { name = "Remy Dahl", role = "Industrialist", description = "The founder of an empire." });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(5));
    }

    [Test]
    public async Task Assign_BoardMemberInTags_AssignsTier5()
    {
        var id = SeedEntity("character", new { name = "Chen Wei", role = "Advisor", tags = new[] { "board member" } });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(5));
    }

    // ── Tier 4 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Assign_DoctorInRole_AssignsTier4()
    {
        var id = SeedEntity("character", new { name = "Dr. Ines Vax", role = "Doctor at Helix Biosystems" });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(4));
    }

    [Test]
    public async Task Assign_EngineerInRole_AssignsTier4()
    {
        var id = SeedEntity("character", new { name = "Marcus Wren", role = "Senior Engineer" });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(4));
    }

    // ── Tier 3 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Assign_RunnerInRole_AssignsTier3()
    {
        var id = SeedEntity("character", new { name = "Vex Maura", role = "Street Runner" });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(3));
    }

    [Test]
    public async Task Assign_HackerInDescription_AssignsTier3()
    {
        var id = SeedEntity("character", new { name = "Ghost", role = "Freelancer", description = "Expert hacker." });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(3));
    }

    [Test]
    public async Task Assign_MercenaryInDescription_AssignsTier3()
    {
        var id = SeedEntity("character", new { name = "Kael", description = "A mercenary for hire." });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(3));
    }

    // ── Tier 2 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Assign_GuardInRole_AssignsTier2()
    {
        var id = SeedEntity("character", new { name = "Brick", role = "Security Guard" });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(2));
    }

    [Test]
    public async Task Assign_CourierInRole_AssignsTier2()
    {
        var id = SeedEntity("character", new { name = "Zip", role = "Package Courier" });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(2));
    }

    // ── Tier 1 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Assign_RefugeeInRole_AssignsTier1()
    {
        var id = SeedEntity("character", new { name = "Mara", role = "Refugee from the gap" });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(1));
    }

    [Test]
    public async Task Assign_HomelessInDescription_AssignsTier1()
    {
        var id = SeedEntity("character", new { name = "Old Pete", description = "A homeless drifter living near the wall." });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(1));
    }

    // ── Overwrite behavior ────────────────────────────────────────────────────

    [Test]
    public async Task Assign_OverwriteFalse_SkipsExistingTier()
    {
        var id = SeedEntity("character", new { name = "Preset", role = "CEO", tier = 1 });

        await svc.RunAsync(overwrite: false);

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(1));
    }

    [Test]
    public async Task Assign_OverwriteTrue_ReplacesExistingTier()
    {
        var id = SeedEntity("character", new { name = "Preset", role = "CEO", tier = 1 });

        await svc.RunAsync(overwrite: true);

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(5));
    }

    // ── First-match wins (tier priority order) ────────────────────────────────

    [Test]
    public async Task Assign_CeoAndRunner_AssignsTier5_FirstMatchWins()
    {
        // "ceo" hits tier 5 first — tier rules are checked 5→4→3→2→1
        var id = SeedEntity("character", new { name = "Hybrid", role = "CEO and runner" });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(5));
    }

    // ── Synthetics dir ───────────────────────────────────────────────────────

    [Test]
    public async Task Assign_SyntheticsDir_TierAssigned()
    {
        var id = SeedEntity("synthetic", new { name = "Unit-9", role = "Security enforcer" });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(2));
    }

    // ── Affiliation field ─────────────────────────────────────────────────────

    [Test]
    public async Task Assign_ExecutiveInAffiliation_AssignsTier5()
    {
        var id = SeedEntity("character", new { name = "Silent Vote", affiliation = "Tessera Executive Board" });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["tier"]?.GetValue<int>(), Is.EqualTo(5));
    }

    // ── Result counts ─────────────────────────────────────────────────────────

    [Test]
    public async Task RunAsync_ReturnsCorrectModifiedCount()
    {
        SeedEntity("character", new { name = "A", role = "CEO" });
        SeedEntity("character", new { name = "B", role = "runner" });
        SeedEntity("character", new { name = "C", role = "CEO", tier = 5 }); // already set

        // parallelism: 1 — TestDbFactory's SQLite :memory: backing can't service
        // concurrent writes from multiple DbContexts the way production SQL Server
        // does; default parallelism (4) drops writes here with a swallowed "SQL logic
        // error" warning, which is a test-harness limitation, not a service bug.
        var result = await svc.RunAsync(overwrite: false, parallelism: 1);

        Assert.That(result.FilesModified, Is.EqualTo(2));
        Assert.That(result.FilesScanned, Is.EqualTo(3));
    }
}
