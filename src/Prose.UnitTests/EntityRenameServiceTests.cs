using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>A rename reaches the canonical record, not only the entity row (engine order 01a0d164: a
/// place renamed Atlas to Cuisine kept reading its old name from its own table).</summary>
[TestFixture]
public class EntityRenameServiceTests : WorldFixture
{
    private DistrictRepository places = null!;
    private FactionRepository factions = null!;
    private EntityRenameService rename = null!;

    [SetUp]
    public void SetUpRename()
    {
        places = new DistrictRepository(dbFactory);
        factions = new FactionRepository(dbFactory);
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, places);
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, factions);
        var sp = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);
        var entityWriter = new EntityFieldWriter(sp, gate, dbFactory, writer);
        // The steps under test never reach the markdown sync or the noun rules.
        rename = new EntityRenameService(dbFactory, workbench, null!, new ContinuityService(dbFactory), null!, entityWriter);
    }

    [Test]
    public async Task A_renamed_place_reads_the_new_name_from_its_own_record()
    {
        var p = new DistrictData { Id = Guid.NewGuid().ToString("N"), Name = "Harbor Grill", Description = "A restaurant in the Loop." };
        places.Save(p);

        var r = await rename.RenameRecordAsync(Guid.Parse(p.Id), "place", "Cuisine");

        Assert.That(r, Is.Not.Null);
        Assert.That(r!.Ok, Is.True, r.Error);
        Assert.That(r.Changed, Is.EqualTo(new[] { "name" }));
        Assert.That(places.GetById(p.Id)!.Name, Is.EqualTo("Cuisine"));
        Assert.That(places.GetById(p.Id)!.Description, Is.EqualTo("A restaurant in the Loop."), "only the name moves");
        await using var db = await dbFactory.CreateDbContextAsync();
        Assert.That(CanonRecordLoader.Load(db, "place", Guid.Parse(p.Id))?["name"]?.ToString(), Is.EqualTo("Cuisine"),
            "the record every reader loads carries the new name");
    }

    [Test]
    public async Task Any_repository_type_is_renamed_the_same_way()
    {
        var f = new FactionData { Id = Guid.NewGuid().ToString("N"), Name = "Halvorsen", Description = "A recovery firm." };
        factions.Save(f);

        var r = await rename.RenameRecordAsync(Guid.Parse(f.Id), "faction", "Halvorsen Civic Recovery");

        Assert.That(r!.Ok, Is.True, r.Error);
        Assert.That(factions.GetById(f.Id)!.Name, Is.EqualTo("Halvorsen Civic Recovery"));
    }

    [Test]
    public async Task A_character_is_left_to_its_own_row()
    {
        var c = NewCharacter("Brennan Drum");

        Assert.That(await rename.RenameRecordAsync(Guid.Parse(c.Id), "character", "Brennan Dunnit"), Is.Null);
        Assert.That(characters.GetById(c.Id)!.Name, Is.EqualTo("Brennan Drum"), "the record step does not touch a character");
    }

    [Test]
    public async Task A_rename_whose_record_cannot_be_written_changes_nothing()
    {
        var (book, _) = await BookAsync("They met at the Ghost Stop.");
        var id = Guid.CreateVersion7();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var universe = await db.Nodes.IgnoreQueryFilters().Where(n => n.Id == book).Select(n => n.UniverseId).SingleAsync();
            // An entity row with no typed record behind it: the write door has nothing to write to.
            db.Entities.Add(new Entity { Id = id, UniverseId = universe, EntityType = "place", Name = "Ghost Stop", Slug = "ghost-stop" });
            await db.SaveChangesAsync();
        }

        var r = await rename.ApplyAsync(id.ToString(), book.ToString(), "Last Stop");

        Assert.That(r.Ok, Is.False);
        Assert.That(r.Error, Does.StartWith("record_name_not_written"));
        await using var check = await dbFactory.CreateDbContextAsync();
        Assert.That(await check.Entities.IgnoreQueryFilters().Where(e => e.Id == id).Select(e => e.Name).SingleAsync(),
            Is.EqualTo("Ghost Stop"), "a refused record write leaves the entity row as it was");
        var texts = await check.Beats.AsNoTracking().Select(b => b.Text).ToListAsync();
        Assert.That(texts, Has.Some.Contains("Ghost Stop"), "and the beats as they were");
    }
}
