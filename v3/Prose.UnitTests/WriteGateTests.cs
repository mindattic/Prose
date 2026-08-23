using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.WriteGate;

namespace Prose.UnitTests;

/// <summary>
/// Proves the write-gate mechanism actually fires, not just that it should — per the project
/// plan's own verification standard ("Make Prose.Hub the real gatekeeper", 2026-08-22): feed a
/// known-bad input and confirm the operation is now REJECTED where it previously silently
/// succeeded, rather than trusting that wiring code compiles.
/// </summary>
[TestFixture]
public class WriteGateTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private IReadOnlyList<IWriteGateSyncCheck> previousChecks = null!;
    private IWriteAuditService? previousAudit;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-writegate-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "writegate");

        // Save + restore, same pattern UniverseSegregationTests uses for UniverseScope.Current —
        // these are process-wide ambient statics and must not bleed between tests.
        previousChecks = WriteGateScope.SyncChecks;
        previousAudit = WriteGateScope.AuditService;
    }

    [TearDown]
    public void TearDown()
    {
        WriteGateScope.SyncChecks = previousChecks;
        WriteGateScope.AuditService = previousAudit;
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<Guid> SeedCharacterAsync(string name, Guid universeId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.CreateVersion7();
        db.Entities.Add(new Entity
        {
            Id = id,
            UniverseId = universeId,
            EntityType = "character",
            Name = name,
            Slug = name.ToLowerInvariant().Replace(' ', '-') + "-" + id.ToString("N"),
        });
        // CharacterAlias FKs to the Characters subtype table, not directly to Entities — needed
        // for the "allowed" test cases whose write actually reaches base.SaveChangesAsync.
        db.Characters.Add(new Character { Id = id, Name = name });
        await db.SaveChangesAsync();
        return id;
    }

    // ── SelfAliasSyncCheck: the gate's first real sync check ────────────────

    [Test]
    public async Task SelfAliasSyncCheck_RejectsAliasEqualToOwnEntityName()
    {
        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { new SelfAliasSyncCheck() };
        var universeId = Guid.CreateVersion7();
        var kyleId = await SeedCharacterAsync("Kyle", universeId);

        await using var db = await dbFactory.CreateDbContextAsync();
        db.CharacterAliases.Add(new CharacterAlias { CharacterId = kyleId, Value = "Kyle" });

        var ex = Assert.ThrowsAsync<WriteGateRejectedException>(() => db.SaveChangesAsync());
        Assert.That(ex!.Message, Does.Contain("self-alias"));

        // Confirm it never landed — the whole point of a PRE-save rejection.
        await using var verify = await dbFactory.CreateDbContextAsync();
        Assert.That(await verify.CharacterAliases.CountAsync(a => a.CharacterId == kyleId), Is.EqualTo(0));
    }

    [Test]
    public async Task SelfAliasSyncCheck_RejectsCaseInsensitiveMatch()
    {
        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { new SelfAliasSyncCheck() };
        var universeId = Guid.CreateVersion7();
        var kyleId = await SeedCharacterAsync("Kyle", universeId);

        await using var db = await dbFactory.CreateDbContextAsync();
        db.CharacterAliases.Add(new CharacterAlias { CharacterId = kyleId, Value = "  kyle  " });

        Assert.ThrowsAsync<WriteGateRejectedException>(() => db.SaveChangesAsync());
    }

    [Test]
    public async Task SelfAliasSyncCheck_AllowsGenuinelyDifferentAlias()
    {
        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { new SelfAliasSyncCheck() };
        var universeId = Guid.CreateVersion7();
        var kyleId = await SeedCharacterAsync("Kyle", universeId);

        await using var db = await dbFactory.CreateDbContextAsync();
        db.CharacterAliases.Add(new CharacterAlias { CharacterId = kyleId, Value = "Streetsam" });
        await db.SaveChangesAsync();

        await using var verify = await dbFactory.CreateDbContextAsync();
        Assert.That(await verify.CharacterAliases.CountAsync(a => a.CharacterId == kyleId), Is.EqualTo(1));
    }

    [Test]
    public async Task SelfAliasSyncCheck_CatchesEntityAndAliasInsertedInTheSameSaveChangesCall()
    {
        // Regression case named explicitly in the check's own doc comment: a new character and
        // its alias inserted together in ONE SaveChanges batch, before the Entity row is in the
        // database at all — the check must resolve the owner's Name from the ChangeTracker, not
        // a DB query, or this exact case would silently slip through.
        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { new SelfAliasSyncCheck() };
        var universeId = Guid.CreateVersion7();
        var newId = Guid.CreateVersion7();

        await using var db = await dbFactory.CreateDbContextAsync();
        db.Entities.Add(new Entity
        {
            Id = newId, UniverseId = universeId, EntityType = "character",
            Name = "Femi", Slug = "femi-" + newId.ToString("N"),
        });
        db.CharacterAliases.Add(new CharacterAlias { CharacterId = newId, Value = "Femi" });

        Assert.ThrowsAsync<WriteGateRejectedException>(() => db.SaveChangesAsync());
    }

    // ── DefaultWriteAuditService: the gate's first real post-save audit ─────

    [Test]
    public async Task DefaultWriteAuditService_ExactDuplicateEntity_FilesNearDuplicateFinding()
    {
        var universeId = Guid.CreateVersion7();
        var firstId = await SeedCharacterAsync("Renko Moss", universeId);
        await SeedCharacterAsync("Renko Moss", universeId);

        var dupScan = new DuplicateEntityScanService(dbFactory, new FakeLlmService());
        var findings = new FindingsService(dbFactory, paths);
        var audit = new DefaultWriteAuditService(dupScan, findings, dbFactory, NullLogger<DefaultWriteAuditService>.Instance);

        await audit.DispatchAsync(new WriteEvent(WriteSubject.EntityCore, firstId, null, universeId, "test"), CancellationToken.None);

        var open = findings.List(FindingStatus.New);
        Assert.That(open, Has.Some.Matches<Finding>(f =>
            f.Category == FindingCategory.NearDuplicate && f.Summary.Contains("Renko Moss")));
    }

    [Test]
    public async Task DefaultWriteAuditService_NoDuplicate_FilesNothing()
    {
        var universeId = Guid.CreateVersion7();
        var soloId = await SeedCharacterAsync("Unique Name Entirely", universeId);

        var dupScan = new DuplicateEntityScanService(dbFactory, new FakeLlmService());
        var findings = new FindingsService(dbFactory, paths);
        var audit = new DefaultWriteAuditService(dupScan, findings, dbFactory, NullLogger<DefaultWriteAuditService>.Instance);

        await audit.DispatchAsync(new WriteEvent(WriteSubject.EntityCore, soloId, null, universeId, "test"), CancellationToken.None);

        var open = findings.List(FindingStatus.New);
        Assert.That(open, Has.None.Matches<Finding>(f => f.Summary.Contains("Unique Name Entirely")));
    }
}
