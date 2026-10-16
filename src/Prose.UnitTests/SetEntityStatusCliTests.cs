using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Prose.Cli;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.UnitTests;

/// <summary>
/// <c>--set-entity-status</c> (SS-T92): the one explicit, by-id correction for
/// <c>Entities.Status</c> that neither <c>--set-entity-fields</c> (EntityFieldWriter never
/// touches Status) nor <c>--backfill-stubs</c> (only flips stub/canon by mention presence,
/// never archived) can reach. Found needed live: a book's protagonist and a speaking character
/// were both wrongly left Status='archived' since creation, silently refusing every pronoun/name
/// tag pointing at them (NodeWorkbenchService's "live entity" check).
/// </summary>
[TestFixture]
public class SetEntityStatusCliTests
{
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private ServiceProvider services = null!;

    [SetUp]
    public void SetUp()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ss_set_entity_status_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        paths = new TestPathProviderWithRoot(root);
        dbFactory = TestDbFactory.For(paths, "set-entity-status");
        var sc = new ServiceCollection();
        sc.AddSingleton(dbFactory);
        services = sc.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        services.Dispose();
        TestDbFactory.Reset(paths);
        try { Directory.Delete(paths.DataRoot, recursive: true); } catch { /* best effort */ }
    }

    private async Task<Guid> SeedEntityAsync(string status)
    {
        var id = Guid.CreateVersion7();
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Entities.Add(new Entity
        {
            Id = id,
            UniverseId = Guid.NewGuid(),
            EntityType = "character",
            Name = "Satan",
            Slug = "satan",
            Status = status,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return id;
    }

    [Test]
    public async Task RunAsync_ArchivedToCanon_FlipsStatusAndModifiedAt()
    {
        var id = await SeedEntityAsync("archived");
        await using (var db = await dbFactory.CreateDbContextAsync())
            await db.Entities.IgnoreQueryFilters().Where(e => e.Id == id)
                .ExecuteUpdateAsync(u => u.SetProperty(e => e.ModifiedAt, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        var exit = await SetEntityStatusCli.RunAsync(["--id", id.ToString(), "--status", "canon"], services);

        Assert.That(exit, Is.EqualTo(0));
        await using var db2 = await dbFactory.CreateDbContextAsync();
        var row = await db2.Entities.IgnoreQueryFilters().AsNoTracking().FirstAsync(e => e.Id == id);
        Assert.That(row.Status, Is.EqualTo("canon"));
        Assert.That(row.ModifiedAt, Is.GreaterThan(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public async Task RunAsync_AlreadyAtTargetStatus_IsANoOpThatStillSucceeds()
    {
        var id = await SeedEntityAsync("canon");
        var before = (await dbFactory.CreateDbContextAsync()).Entities.IgnoreQueryFilters().AsNoTracking().First(e => e.Id == id).ModifiedAt;

        var exit = await SetEntityStatusCli.RunAsync(["--id", id.ToString(), "--status", "canon"], services);

        Assert.That(exit, Is.EqualTo(0));
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.Entities.IgnoreQueryFilters().AsNoTracking().FirstAsync(e => e.Id == id);
        Assert.That(row.Status, Is.EqualTo("canon"));
        Assert.That(row.ModifiedAt, Is.EqualTo(before));
    }

    [Test]
    public async Task RunAsync_UnknownId_FailsWithoutWriting()
    {
        var exit = await SetEntityStatusCli.RunAsync(["--id", Guid.NewGuid().ToString(), "--status", "canon"], services);
        Assert.That(exit, Is.EqualTo(1));
    }

    [TestCase("--status", "nonsense")]
    [TestCase("--missing-status-flag", "x")]
    public async Task RunAsync_BadOrMissingStatus_Refuses(string flag, string value)
    {
        var id = await SeedEntityAsync("archived");
        var exit = await SetEntityStatusCli.RunAsync(["--id", id.ToString(), flag, value], services);
        Assert.That(exit, Is.EqualTo(1));
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.Entities.IgnoreQueryFilters().AsNoTracking().FirstAsync(e => e.Id == id);
        Assert.That(row.Status, Is.EqualTo("archived"));
    }

    [Test]
    public async Task RunAsync_MissingId_Refuses()
    {
        var exit = await SetEntityStatusCli.RunAsync(["--status", "canon"], services);
        Assert.That(exit, Is.EqualTo(1));
    }
}
