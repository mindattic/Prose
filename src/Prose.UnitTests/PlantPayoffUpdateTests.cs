using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Pins PlantPayoffService.UpdateDescriptionsAsync (2026-09-23): a registered pair can be
/// corrected in place when the page and the register disagree, and its plant obligation's
/// "plant → payoff" text follows. BCODA read 3 found four rows the register had wrong
/// (a sixth man where the page has a seventh; 誠 and 名誉 swapped) with no path to fix them.
/// </summary>
[TestFixture]
public class PlantPayoffUpdateTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private PlantPayoffService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "plant-update-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
        svc = new PlantPayoffService(dbFactory);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
    }

    private async Task<Guid> SeedChapterAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var chapter = new ChapterNode { Id = Guid.NewGuid(), Slug = "pp-" + Guid.NewGuid().ToString("N")[..8], Title = "Chapter" };
        db.Nodes.Add(chapter);
        await db.SaveChangesAsync();
        return chapter.Id;
    }

    [Test]
    public async Task Updates_the_payoff_and_keeps_the_plant_and_the_obligation_in_step()
    {
        var nodeId = await SeedChapterAsync();
        var pp = await svc.RegisterAsync(nodeId, "Moss moved with the photograph", "The struck page: 名誉");

        var updated = await svc.UpdateDescriptionsAsync(pp.Id, null, "The struck page: 誠");

        Assert.That(updated.PlantDescription, Is.EqualTo("Moss moved with the photograph"));
        Assert.That(updated.PayoffDescription, Is.EqualTo("The struck page: 誠"));

        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.PlantPayoffs.AsNoTracking().SingleAsync(p => p.Id == pp.Id);
        Assert.That(row.PayoffDescription, Is.EqualTo("The struck page: 誠"));
        if (row.ObligationId is Guid obId)
        {
            var ob = await db.NarrativeObligations.AsNoTracking().SingleAsync(o => o.Id == obId);
            Assert.That(ob.Description, Is.EqualTo("Moss moved with the photograph → The struck page: 誠"));
        }
    }

    [Test]
    public async Task Refuses_an_update_that_changes_nothing()
    {
        var nodeId = await SeedChapterAsync();
        var pp = await svc.RegisterAsync(nodeId, "plant", "payoff");
        Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateDescriptionsAsync(pp.Id, null, "  "));
    }
}
