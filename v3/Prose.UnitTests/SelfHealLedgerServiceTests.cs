using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Covers the EF-only surface of <see cref="SelfHealLedgerService"/> — logging and listing
/// actions. The actual row-reversal SQL (<see cref="SelfHealLedgerService.UndoRunAsync"/>'s
/// per-<see cref="RowMutationUndo"/> dispatch) uses SQL Server-only syntax (bracket-qualified
/// `[dbo].[Table]` raw SQL, <c>SYSUTCDATETIME()</c>) and can't run against the SQLite in-memory
/// test provider — same known limitation as <c>DataConsistencyService</c> and
/// <c>BeatDuplicateService</c>, neither of which have unit tests for the same reason. That path is
/// exercised manually against the real LocalDB instance instead (see
/// `prose --auto-correct-nightly --dry-run` / a live run + `--auto-correct-undo`).
/// </summary>
[TestFixture]
public class SelfHealLedgerServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private SelfHealLedgerService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-selfheal-ledger-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "selfheal-ledger");
        svc = new SelfHealLedgerService(dbFactory, NullLogger<SelfHealLedgerService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    [Test]
    public async Task LogAsync_PersistsMutationsAsJson()
    {
        var runId = Guid.NewGuid();
        var mutations = new List<RowMutationUndo>
        {
            new("update", "Entities", "Id", Guid.NewGuid().ToString(), new Dictionary<string, string?> { ["IsActive"] = "1" }),
        };

        var actionId = await svc.LogAsync(runId, sequence: 1, nodeId: null, "entity-merge", mutations, "test merge");

        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.SelfHealActions.SingleAsync(a => a.Id == actionId);
        Assert.That(row.RunId, Is.EqualTo(runId));
        Assert.That(row.ActionType, Is.EqualTo("entity-merge"));
        Assert.That(row.TargetTable, Is.EqualTo("Entities"));
        Assert.That(row.UndoneAt, Is.Null);
        Assert.That(row.BeforeStateJson, Does.Contain("IsActive"));
    }

    [Test]
    public async Task ListRunsAsync_GroupsByRunId_NewestFirst()
    {
        var olderRun = Guid.NewGuid();
        var newerRun = Guid.NewGuid();
        var mutations = new List<RowMutationUndo> { new("update", "Entities", "Id", "x", new Dictionary<string, string?>()) };

        await svc.LogAsync(olderRun, 1, null, "entity-merge", mutations, "older");
        await Task.Delay(5); // ensure distinct AppliedAt ordering
        await svc.LogAsync(newerRun, 1, null, "consistency-fix", mutations, "newer #1");
        await svc.LogAsync(newerRun, 2, null, "consistency-fix", mutations, "newer #2");

        var runs = await svc.ListRunsAsync();

        Assert.That(runs, Has.Count.EqualTo(2));
        Assert.That(runs[0].RunId, Is.EqualTo(newerRun), "most recent run should sort first");
        Assert.That(runs[0].TotalActions, Is.EqualTo(2));
        Assert.That(runs[0].UndoneActions, Is.EqualTo(0));
        Assert.That(runs[1].RunId, Is.EqualTo(olderRun));
        Assert.That(runs[1].TotalActions, Is.EqualTo(1));
    }

    [Test]
    public async Task UndoRunAsync_UnknownRunId_ReversesNothing()
    {
        var count = await svc.UndoRunAsync(Guid.NewGuid());
        Assert.That(count, Is.EqualTo(0));
    }
}
