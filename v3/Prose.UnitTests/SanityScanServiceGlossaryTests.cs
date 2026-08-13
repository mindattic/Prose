using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix to SanityScanService's "undefined all-caps acronym"
/// check (Check B). It builds its "known tokens" set from active Entity names only, never
/// consulting GlossaryTerms — so a properly back-matter-glossaried acronym (SS-LAW-20: the
/// Glossary, not in-voice explanation, is the designated fix for unglossed acronyms) was
/// re-flagged as "possible placeholder or leaked code" on every single mention. Confirmed live:
/// "NCID" (Neuretic Crime Investigation Division, fully defined in GlossaryTerms) alone produced
/// dozens of false positives spread across nearly every chapter of one book — the single largest
/// contributor to this check's overall false-positive volume (471 of 595 GLMZ warnings were this
/// check; NCID alone accounted for 9 of the ~348 distinct flagged tokens, appearing repeatedly).
/// </summary>
[TestFixture]
public class SanityScanServiceGlossaryTests
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
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-sanityscan-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "sanity");
        svc = new SanityScanService(dbFactory);
        universeId = Guid.CreateVersion7();
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<Guid> SeedNodeAsync(string beatText, params string[] glossaryTerms)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Universes.Add(new Universe { Id = universeId, Slug = "u-" + Guid.NewGuid().ToString("N")[..8], Name = "U" });
        foreach (var term in glossaryTerms)
            db.GlossaryTerms.Add(new GlossaryTerm { Id = Guid.CreateVersion7(), UniverseId = universeId, Term = term, Definition = "Test definition." });

        var id = Guid.CreateVersion7();
        var node = NodeFactory.Create("book");
        node.Id = id;
        node.Slug = "s-" + Guid.NewGuid().ToString("N")[..8];
        node.Title = "T";
        node.Status = "draft";
        node.SortKey = 100;
        node.UniverseId = universeId;
        db.Nodes.Add(node);
        // Beats need to repeat past the 50-page floor's threshold isn't relevant here — just
        // needs enough words that BelowLengthFloor doesn't dominate the findings list, though
        // it's a separate Kind so it wouldn't interfere with UndefinedAcronym assertions anyway.
        var beat = new Beat { Id = Guid.CreateVersion7(), Number = ++beatNumber, Text = beatText };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = id, BeatId = beat.Id, SortKey = 1 });
        await db.SaveChangesAsync();
        return id;
    }

    [Test]
    public async Task GlossariedAcronym_IsNotFlaggedAsUndefined()
    {
        var nodeId = await SeedNodeAsync(
            "The NCID agents moved through the checkpoint without slowing down.",
            "NCID");

        var report = await svc.ScanAsync(nodeId);

        Assert.That(report.Findings.Any(f => f.Kind == "UndefinedAcronym"), Is.False,
            "an acronym with a real Glossary entry must not be flagged as a possible leaked code / placeholder");
    }

    [Test]
    public async Task NonGlossariedAcronym_IsStillFlagged()
    {
        // Must not become a blanket loophole — a genuinely unrecognized token still fires.
        var nodeId = await SeedNodeAsync(
            "The XKQZ readout blinked twice before the technician noticed.");
        // No glossary terms seeded at all.

        var report = await svc.ScanAsync(nodeId);

        Assert.That(report.Findings.Any(f => f.Kind == "UndefinedAcronym"), Is.True,
            "a token with no Glossary entry and no matching entity must still be flagged");
    }

    [Test]
    public async Task GlossaryTermFromADifferentUniverse_DoesNotSuppressTheFlag()
    {
        // Glossary lookup must be universe-scoped — a term glossaried in a DIFFERENT universe
        // must not suppress the flag for a book in THIS universe.
        var otherUniverseId = Guid.CreateVersion7();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Universes.Add(new Universe { Id = otherUniverseId, Slug = "other-" + Guid.NewGuid().ToString("N")[..8], Name = "Other" });
            db.GlossaryTerms.Add(new GlossaryTerm { Id = Guid.CreateVersion7(), UniverseId = otherUniverseId, Term = "NCID", Definition = "Unrelated definition in another universe." });
            await db.SaveChangesAsync();
        }
        var nodeId = await SeedNodeAsync("The NCID agents moved through the checkpoint.");
        // Note: no glossary term seeded for THIS node's own universe.

        var report = await svc.ScanAsync(nodeId);

        Assert.That(report.Findings.Any(f => f.Kind == "UndefinedAcronym"), Is.True,
            "a Glossary term registered in a different universe must not suppress this universe's flag");
    }
}
