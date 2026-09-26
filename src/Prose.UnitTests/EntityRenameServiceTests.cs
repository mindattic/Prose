using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;
using Prose.Core.Services.Audit;

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
    public async Task Applying_a_place_rename_moves_the_typed_row_the_entity_row_and_the_prose()
    {
        var (book, _) = await BookAsync("They ate at Harbor Grill that night.", "Harbor Grill was closed by morning.");
        var p = new DistrictData { Id = Guid.NewGuid().ToString("N"), Name = "Harbor Grill", Description = "A restaurant in the Loop." };
        places.Save(p);
        var placeId = Guid.Parse(p.Id);
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var universe = await db.Nodes.IgnoreQueryFilters().Where(n => n.Id == book).Select(n => n.UniverseId).SingleAsync();
            (await db.Entities.IgnoreQueryFilters().SingleAsync(e => e.Id == placeId)).UniverseId = universe;
            await db.SaveChangesAsync();
        }
        var full = new EntityRenameService(dbFactory, workbench,
            new MarkdownFileService(dbFactory, paths, new CanonDocumentTypeRegistry(dbFactory)),
            new ContinuityService(dbFactory),
            new NounConsistencyService(dbFactory, new AuditRunner(new FakeLlmService(), new FindingsService(dbFactory, paths))),
            new EntityFieldWriter(BuildServices(), gate, dbFactory, writer));

        var r = await full.ApplyAsync(p.Id, book.ToString(), "Cuisine");

        Assert.That(r.Ok, Is.True, r.Error);
        Assert.That(r.BeatsChanged, Is.EqualTo(2));
        await using var check = await dbFactory.CreateDbContextAsync();
        Assert.That(CanonRecordLoader.Load(check, "place", placeId)?["name"]?.ToString(), Is.EqualTo("Cuisine"),
            "get_place reads the typed row, and the typed row carries the new name");
        Assert.That(CanonRecordLoader.Load(check, "place", placeId)?["description"]?.ToString(), Is.EqualTo("A restaurant in the Loop."));
        Assert.That(places.GetById(p.Id)!.Name, Is.EqualTo("Cuisine"));
        Assert.That(await check.Entities.IgnoreQueryFilters().Where(e => e.Id == placeId).Select(e => e.Name).SingleAsync(), Is.EqualTo("Cuisine"));
        var texts = await check.Beats.AsNoTracking().Select(b => b.Text).ToListAsync();
        Assert.That(texts, Has.None.Contains("Harbor Grill"));
        Assert.That(texts, Has.Some.Contains("Cuisine"));
    }

    [Test]
    public async Task Every_typed_record_type_has_a_name_field_the_rename_writes_or_is_refused()
    {
        // A quote's name is derived from its text (QuoteMapper: the first 40 characters), so
        // there is no name to write; every other typed record has one.
        foreach (var type in EntityFieldWriter.Repositories.Keys.Where(t => t != "quote"))
            Assert.That(EntityRenameService.RecordNameKey(type), Is.Not.Null, type);
        Assert.That(EntityRenameService.RecordNameKey("place"), Is.EqualTo("name"));
        Assert.That(EntityRenameService.RecordNameKey("vocabulary"), Is.EqualTo("term"));

        // It used to answer null — "no typed record" — and the rename went on, leaving the typed
        // row on the old name. Now it is refused before anything is written.
        Assert.That(EntityRenameService.RecordNameKey("quote"), Is.Null);
        var r = await rename.RenameRecordAsync(Guid.NewGuid(), "quote", "Anything");
        Assert.That(r, Is.Not.Null);
        Assert.That(r!.Ok, Is.False);
        Assert.That(r.Error, Does.Contain("no name field"));
    }

    private IServiceProvider BuildServices()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, places);
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, factions);
        return Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);
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
