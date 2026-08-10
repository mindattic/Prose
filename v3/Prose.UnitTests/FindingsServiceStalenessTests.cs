using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0011 Brick 2: <see cref="FindingsService.GetStaleCategoriesAsync"/> is the generic
/// staleness query every check category shares — a category joins in by stamping
/// <c>sourceRuleVersion</c> on <see cref="FindingsService.Upsert"/> and passing its own
/// "what's current" value here, no bespoke query or CLI flag needed per category.
/// </summary>
[TestFixture]
public class FindingsServiceStalenessTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private FindingsService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-findings-staleness-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
        svc = new FindingsService(dbFactory, paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    [Test]
    public async Task Upsert_WithNoVersion_IsReportedStaleAgainstAnyCurrentVersion()
    {
        svc.Upsert("node:s1", null, FindingCategory.CraftChecklist, FindingSeverity.Low,
            "CHECKLIST beat #1: something", null, null); // no sourceRuleVersion passed — legacy shape

        var stale = await svc.GetStaleCategoriesAsync(new Dictionary<string, string> { ["CraftChecklist"] = "v2" });

        Assert.That(stale, Has.Count.EqualTo(1));
        Assert.That(stale[0].Category, Is.EqualTo("CraftChecklist"));
        Assert.That(stale[0].StaleCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Upsert_WithCurrentVersion_IsNotReportedStale()
    {
        svc.Upsert("node:s1", null, FindingCategory.CraftChecklist, FindingSeverity.Low,
            "CHECKLIST beat #1: something", null, null, sourceRuleVersion: "v2");

        var stale = await svc.GetStaleCategoriesAsync(new Dictionary<string, string> { ["CraftChecklist"] = "v2" });

        Assert.That(stale, Is.Empty);
    }

    [Test]
    public async Task Upsert_WithOldVersion_IsReportedStale()
    {
        svc.Upsert("node:s1", null, FindingCategory.CraftChecklist, FindingSeverity.Low,
            "CHECKLIST beat #1: something", null, null, sourceRuleVersion: "v1-old");

        var stale = await svc.GetStaleCategoriesAsync(new Dictionary<string, string> { ["CraftChecklist"] = "v2" });

        Assert.That(stale, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task DismissedFinding_IsExcludedRegardlessOfVersion()
    {
        var id = svc.Upsert("node:s1", null, FindingCategory.CraftChecklist, FindingSeverity.Low,
            "CHECKLIST beat #1: something", null, null); // stale shape
        svc.SetStatus(id, FindingStatus.Dismissed);

        var stale = await svc.GetStaleCategoriesAsync(new Dictionary<string, string> { ["CraftChecklist"] = "v2" });

        Assert.That(stale, Is.Empty, "a dismissed finding's staleness no longer matters");
    }

    [Test]
    public async Task PerBeatFindings_GroupUnderTheirOwningBook()
    {
        svc.Upsert("node:s1/beat:aaa", null, FindingCategory.StructuralFailure, FindingSeverity.Low,
            "VERIFY [EventType] beat #1: x", null, null); // stale
        svc.Upsert("node:s1/beat:bbb", null, FindingCategory.StructuralFailure, FindingSeverity.Low,
            "VERIFY [EventType] beat #2: y", null, null); // stale

        var stale = await svc.GetStaleCategoriesAsync(new Dictionary<string, string> { ["StructuralFailure"] = "v1" });

        Assert.That(stale, Has.Count.EqualTo(1), "two per-beat findings under the same book should group into one row");
        Assert.That(stale[0].FilePath, Is.EqualTo("node:s1"));
        Assert.That(stale[0].StaleCount, Is.EqualTo(2));
    }

    [Test]
    public async Task UnwiredCategory_NeverAppearsInReport()
    {
        svc.Upsert("node:s1", null, FindingCategory.Voice, FindingSeverity.Low, "some voice finding", null, null);

        var stale = await svc.GetStaleCategoriesAsync(new Dictionary<string, string> { ["CraftChecklist"] = "v2" });

        Assert.That(stale, Is.Empty, "a category the caller didn't ask about must never appear, versioned or not");
    }
}
