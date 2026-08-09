using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix to SanityScanService's "internal node-code leak"
/// check (Check A). NONFICTION books are coded with plain, meaningful values — a historical year
/// ("1381" for a Peasants' Revolt book) or a Gospel author's name ("JOHN", "MATTHEW") — unlike
/// GLMZ's deliberately-obscure abbreviation codes ("BCODA", "ATTE"). The check flagged a book's
/// OWN code appearing in ITS OWN prose as a "leak" even though nothing left the book it belongs
/// to, and never exempted purely numeric codes even though a year appearing in prose is obviously
/// a date, never leaked dev jargon. Confirmed live: node "1381-the-peasants-revolt" (coded "1381")
/// self-flagged 109 times; node "matthew-..." (coded "MATTHEW") self-flagged 13 times — together
/// with the other Gospel-coded books, 164 of 183 (89.6%) of NONFICTION's InternalCodeLeak findings
/// were this exact self-leak/numeric-code false-positive class.
/// </summary>
[TestFixture]
public class SanityScanServiceCodeLeakTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private SanityScanService svc = null!;
    private Guid universeId;
    private int beatNumber;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-sanityscan-codeleak-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "sanity-codeleak");
        svc = new SanityScanService(dbFactory);
        universeId = Guid.CreateVersion7();
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<Guid> SeedBookAsync(string nodeCode, string beatText, params (string Slug, string Code)[] otherNodes)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Universes.Add(new Universe { Id = universeId, Slug = "u-" + Guid.NewGuid().ToString("N")[..8], Name = "U" });

        foreach (var (slug, code) in otherNodes)
        {
            var other = NodeFactory.Create("book");
            other.Id = Guid.CreateVersion7(); other.Slug = slug; other.Title = slug;
            other.Status = "draft"; other.SortKey = 200; other.UniverseId = universeId; other.NodeCode = code;
            db.Nodes.Add(other);
        }

        var id = Guid.CreateVersion7();
        var node = NodeFactory.Create("book");
        node.Id = id; node.Slug = "s-" + Guid.NewGuid().ToString("N")[..8];
        node.Title = "T"; node.Status = "draft"; node.SortKey = 100;
        node.UniverseId = universeId; node.NodeCode = nodeCode;
        db.Nodes.Add(node);

        var beat = new Beat { Id = Guid.CreateVersion7(), Number = ++beatNumber, Text = beatText };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = id, BeatId = beat.Id, SortKey = 1, IsEnabled = true });
        await db.SaveChangesAsync();
        return id;
    }

    [Test]
    public async Task NodesOwnCode_AppearingInItsOwnProse_IsNotFlaggedAsLeak()
    {
        // Case matters: FindCodeInText matches case-sensitively, and the real NONFICTION bug is
        // literal all-caps citation-style mentions ("MATTHEW 5:9"), not title-case prose ("Matthew").
        var nodeId = await SeedBookAsync("MATTHEW",
            "As recorded in MATTHEW 5:9, the author frames Jesus as the new Moses delivering a new law.");

        var report = await svc.ScanAsync(nodeId);

        Assert.That(report.Findings.Any(f => f.Kind == "InternalCodeLeak"), Is.False,
            "a book's own NodeCode appearing in its own prose is not a leak — nothing left the book it belongs to");
    }

    [Test]
    public async Task NumericCode_AppearingInProse_IsNeverFlaggedAsLeak()
    {
        // Even a DIFFERENT node's numeric code showing up must not flag — a year is a date, never
        // leaked dev jargon, regardless of which book it "belongs" to.
        var nodeId = await SeedBookAsync("HERESY",
            "When the rebels arrived in Essex in the spring of 1381, they were not the first armed peasants to march on London.",
            ("some-other-book", "1381"));

        var report = await svc.ScanAsync(nodeId);

        Assert.That(report.Findings.Any(f => f.Kind == "InternalCodeLeak"), Is.False,
            "a purely numeric code can never be a leaked internal dev code — it's a date or quantity");
    }

    [Test]
    public async Task AnotherNodesNonNumericCode_AppearingInProse_IsStillFlagged()
    {
        // Must not become a blanket loophole — a genuine cross-book leak (a DIFFERENT node's
        // non-numeric code, like GLMZ's real MxG/NRST leaks) must still be caught.
        var nodeId = await SeedBookAsync("ATTE",
            "Every job since MxG, she had watched Lace build her reputation one score at a time.",
            ("magenta-gunmetal", "MxG"));

        var report = await svc.ScanAsync(nodeId);

        Assert.That(report.Findings.Any(f => f.Kind == "InternalCodeLeak"), Is.True,
            "a different node's real internal code leaking into this node's prose must still be caught");
    }
}
