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

    // Order 01a0dc5e-8798: update_plant_payoff failed with "An error occurred while saving the
    // entity changes" on a description a few hundred characters long — the pair's obligation
    // carries "plant → payoff" in a 500-char column. SQLite does not enforce lengths, so the
    // model's limits are pinned directly, beside a real long round trip.
    [Test]
    public void The_columns_hold_real_descriptions_and_the_obligation_holds_both()
    {
        using var db = dbFactory.CreateDbContext();
        int? Max(Type t, string p) => db.Model.FindEntityType(t)!.FindProperty(p)!.GetMaxLength();

        Assert.That(Max(typeof(PlantPayoff), nameof(PlantPayoff.PlantDescription)), Is.EqualTo(PlantPayoff.MaxDescriptionLength));
        Assert.That(Max(typeof(PlantPayoff), nameof(PlantPayoff.PayoffDescription)), Is.EqualTo(PlantPayoff.MaxDescriptionLength));
        Assert.That(PlantPayoff.MaxDescriptionLength, Is.GreaterThanOrEqualTo(1000), "a few hundred characters must fit with room to spare");
        Assert.That(Max(typeof(NarrativeObligation), nameof(NarrativeObligation.Description)),
            Is.GreaterThanOrEqualTo(2 * PlantPayoff.MaxDescriptionLength + " → ".Length),
            "the obligation's \"plant → payoff\" must fit two full descriptions");
    }

    [Test]
    public async Task Long_descriptions_register_and_update_and_read_back_whole()
    {
        var nodeId = await SeedChapterAsync();
        var plant = "Moss moved with the photograph. " + new string('p', 420);
        var payoff = "The struck page: 誠. " + new string('q', 480);
        var pp = await svc.RegisterAsync(nodeId, plant, payoff);

        var longer = "On re-read the struck page is 誠, not 名誉. " + new string('r', PlantPayoff.MaxDescriptionLength - 60);
        var updated = await svc.UpdateDescriptionsAsync(pp.Id, null, longer);
        Assert.That(updated.PayoffDescription, Is.EqualTo(longer));

        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.PlantPayoffs.AsNoTracking().SingleAsync(p => p.Id == pp.Id);
        Assert.That(row.PlantDescription, Is.EqualTo(plant));
        Assert.That(row.PayoffDescription, Is.EqualTo(longer));
        var ob = await db.NarrativeObligations.AsNoTracking().SingleAsync(o => o.Id == row.ObligationId);
        Assert.That(ob.Description, Is.EqualTo($"{plant} → {longer}"));
    }

    [Test]
    public async Task An_over_long_description_is_refused_up_front_naming_the_field_and_limit()
    {
        var nodeId = await SeedChapterAsync();
        var tooLong = new string('x', PlantPayoff.MaxDescriptionLength + 1);

        var reg = Assert.ThrowsAsync<ArgumentException>(() => svc.RegisterAsync(nodeId, "plant", tooLong));
        Assert.That(reg!.Message, Does.Contain("payoffDescription").And.Contain(PlantPayoff.MaxDescriptionLength.ToString()));

        var pp = await svc.RegisterAsync(nodeId, "plant", "payoff");
        var upd = Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateDescriptionsAsync(pp.Id, tooLong, null));
        Assert.That(upd!.Message, Does.Contain("plantDescription").And.Contain(PlantPayoff.MaxDescriptionLength.ToString()));

        await using var db = await dbFactory.CreateDbContextAsync();
        Assert.That((await db.PlantPayoffs.AsNoTracking().SingleAsync(p => p.Id == pp.Id)).PlantDescription, Is.EqualTo("plant"));
    }

    [Test]
    public async Task A_plant_registered_on_a_chapter_with_scene_children_is_found_from_the_book()
    {
        Guid bookId, chapterId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var book = new BookNode { Id = Guid.NewGuid(), Slug = "pp-book-" + Guid.NewGuid().ToString("N")[..8], Title = "Book" };
            var chapter = new ChapterNode { Id = Guid.NewGuid(), Slug = "pp-ch-" + Guid.NewGuid().ToString("N")[..8], Title = "Chapter", ParentNodeId = book.Id, SortKey = 100 };
            var scene = new SceneNode { Id = Guid.NewGuid(), Slug = "pp-sc-" + Guid.NewGuid().ToString("N")[..8], Title = "Scene", ParentNodeId = chapter.Id, SortKey = 100 };
            db.Nodes.AddRange(book, chapter, scene);
            await db.SaveChangesAsync();
            (bookId, chapterId) = (book.Id, chapter.Id);
        }
        var pp = await svc.RegisterAsync(chapterId, "The key under the mat", "The door opens");

        Assert.That((await svc.GetByNodeAsync(bookId)).Select(p => p.Id), Does.Contain(pp.Id));
    }
}
