using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>The world's read path never writes; the entity summary has a write path; a document's
/// entity name is its title, so editing its file name renames nothing (2026-10-03: a read probe sent
/// as a write renamed and re-slugged 53 documents).</summary>
[TestFixture]
public class EntityReadAndSummaryTests : WorldFixture
{
    private WorldbuildingDocRepository docs = null!;
    private FactionRepository factions = null!;
    private EntityFieldWriter entityWriter = null!;

    [SetUp]
    public void SetUpWriters()
    {
        docs = new WorldbuildingDocRepository(dbFactory);
        factions = new FactionRepository(dbFactory);
        var services = new ServiceCollection();
        services.AddSingleton(docs);
        services.AddSingleton(factions);
        entityWriter = new EntityFieldWriter(services.BuildServiceProvider(), gate, dbFactory, writer);
    }

    private async Task<(string Name, string Slug, string? Description, DateTime ModifiedAt)> RowAsync(string id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var e = await db.Entities.IgnoreQueryFilters().AsNoTracking().SingleAsync(x => x.Id == Guid.Parse(id));
        return (e.Name, e.Slug, e.Description, e.ModifiedAt);
    }

    [Test]
    public async Task Reading_a_record_writes_nothing()
    {
        var f = new FactionData { Id = Guid.NewGuid().ToString("N"), Name = "The Vultures", Description = "Body pickup." };
        factions.Save(f);
        await BackdateAsync(f.Id);
        var before = await RowAsync(f.Id);

        var r = await entityWriter.GetFieldsAsync(f.Id);

        Assert.That(r.Ok, Is.True, r.Error);
        Assert.That(r.EntityType, Is.EqualTo("faction"));
        Assert.That(r.Record?["name"]?.ToString(), Is.EqualTo("The Vultures"));
        Assert.That(r.Slug, Is.EqualTo(before.Slug));
        Assert.That(await RowAsync(f.Id), Is.EqualTo(before), "a read changed the entity row");
    }

    [Test]
    public async Task The_summary_has_a_write_path_and_an_unchanged_summary_writes_nothing()
    {
        var f = new FactionData { Id = Guid.NewGuid().ToString("N"), Name = "The Vultures", Description = "Body pickup." };
        factions.Save(f);

        var r = await entityWriter.SetFieldsAsync(f.Id, """{"summary":"Body pickup in the Clybourn Corridor."}""");
        Assert.That(r.Ok, Is.True, r.Error);
        Assert.That(r.Changed, Is.EqualTo(new[] { "summary" }));
        Assert.That((await RowAsync(f.Id)).Description, Is.EqualTo("Body pickup in the Clybourn Corridor."));
        Assert.That((await entityWriter.GetFieldsAsync(f.Id)).Summary, Is.EqualTo("Body pickup in the Clybourn Corridor."));

        await BackdateAsync(f.Id);
        var stamp = (await RowAsync(f.Id)).ModifiedAt;
        var again = await entityWriter.SetFieldsAsync(f.Id, """{"summary":"Body pickup in the Clybourn Corridor."}""");
        Assert.That(again.Ok, Is.True, again.Error);
        Assert.That(again.Changed, Is.Empty);
        Assert.That((await RowAsync(f.Id)).ModifiedAt, Is.EqualTo(stamp), "an unchanged summary bumped ModifiedAt");

        var both = await entityWriter.SetFieldsAsync(f.Id, """{"motto":"Nothing wasted.","summary":"Collects bodies."}""");
        Assert.That(both.Ok, Is.True, both.Error);
        Assert.That(both.Changed, Is.EquivalentTo(new[] { "motto", "summary" }));
        Assert.That(factions.GetById(f.Id)!.Motto, Is.EqualTo("Nothing wasted."));

        var bad = await entityWriter.SetFieldsAsync(f.Id, """{"summary":["not","a","string"]}""");
        Assert.That(bad.Ok, Is.False);
    }

    [Test]
    public async Task A_titled_documents_entity_name_and_slug_survive_a_file_name_edit()
    {
        var d = new WorldbuildingDocument { Id = Guid.NewGuid().ToString("N"), FileName = "domestic_robots", Title = "Domestic Robots: The Machines We Won't Let Think", Body = "# Domestic Robots" };
        docs.Save(d);
        var before = await RowAsync(d.Id);
        Assert.That(before.Name, Is.EqualTo(d.Title), "a titled document is named by its title");

        var r = await entityWriter.SetFieldsAsync(d.Id, """{"file_name":"Domestic Robots: The Machines We Won't Let Think"}""");
        Assert.That(r.Ok, Is.True, r.Error);
        var back = await entityWriter.SetFieldsAsync(d.Id, """{"file_name":"domestic_robots"}""");
        Assert.That(back.Ok, Is.True, back.Error);

        var after = await RowAsync(d.Id);
        Assert.That(after.Name, Is.EqualTo(before.Name));
        Assert.That(after.Slug, Is.EqualTo(before.Slug), "a file-name edit regenerated the slug");
        Assert.That(docs.GetById(d.Id)!.FileName, Is.EqualTo("domestic_robots"));
    }

    [Test]
    public async Task An_untitled_document_keeps_the_name_it_has()
    {
        var d = new WorldbuildingDocument { Id = Guid.NewGuid().ToString("N"), FileName = "the_tier_trap_essay", Body = "THE TIER TRAP" };
        docs.Save(d);
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var e = await db.Entities.IgnoreQueryFilters().SingleAsync(x => x.Id == Guid.Parse(d.Id));
            e.Name = "(untitled document)";
            await db.SaveChangesAsync();
        }
        var r = await entityWriter.SetFieldsAsync(d.Id, """{"body":"THE TIER TRAP, revised"}""");
        Assert.That(r.Ok, Is.True, r.Error);
        Assert.That((await RowAsync(d.Id)).Name, Is.EqualTo("(untitled document)"));
        var cleared = await entityWriter.SetFieldsAsync(d.Id, """{"file_name":""}""");
        Assert.That(cleared.Ok, Is.True, cleared.Error);
        Assert.That((await RowAsync(d.Id)).Name, Is.EqualTo("(untitled document)"), "an empty file name blanked the entity name");
        Assert.That(WorldbuildingDocRepository.DocumentEntityName(new WorldbuildingDocument { FileName = "x" }, null), Is.EqualTo("x"),
            "a new untitled document is named by its file name");
    }
}

/// <summary>Slug, edge, orphan-tag and obligation corrections: dry run by default, refusals for the
/// unsafe cases, read back after writing.</summary>
[TestFixture]
public class CanonMaintenanceServiceTests : WorldFixture
{
    private CanonMaintenanceService maintenance = null!;

    [SetUp]
    public void SetUpMaintenance() => maintenance = new CanonMaintenanceService(dbFactory);

    private async Task<Guid> UniverseAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var u = await db.Universes.Select(x => x.Id).FirstOrDefaultAsync();
        if (u != Guid.Empty) return u;
        u = Guid.CreateVersion7();
        db.Universes.Add(new Universe { Id = u, Slug = "u-" + u.ToString("N")[..6], Name = "U" });
        await db.SaveChangesAsync();
        return u;
    }

    private async Task<Guid> EntityAsync(string name, string slug, string type = "place")
    {
        var u = await UniverseAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        var e = new Entity { UniverseId = u, EntityType = type, Name = name, Slug = slug, CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow };
        db.Entities.Add(e);
        await db.SaveChangesAsync();
        return e.Id;
    }

    [Test]
    public async Task A_slug_is_set_only_on_apply_and_never_onto_one_already_taken()
    {
        var a = await EntityAsync("Domestic Robots", "domestic-robots-the-machines");
        await EntityAsync("Other", "taken-slug");

        var dry = await maintenance.SetEntitySlugAsync(a, "domestic_robots_the_machines", apply: false);
        Assert.That(dry is { Ok: true, Applied: false });
        var ok = await maintenance.SetEntitySlugAsync(a, "domestic_robots_the_machines", apply: true);
        Assert.That(ok is { Ok: true, Applied: true }, ok.Message);
        Assert.That((await maintenance.SetEntitySlugAsync(a, "taken-slug", apply: true)).Ok, Is.False);
        Assert.That((await maintenance.SetEntitySlugAsync(a, "Has Spaces", apply: true)).Ok, Is.False);
        await using var db = await dbFactory.CreateDbContextAsync();
        Assert.That(await db.Entities.IgnoreQueryFilters().Where(e => e.Id == a).Select(e => e.Slug).SingleAsync(), Is.EqualTo("domestic_robots_the_machines"));
    }

    [Test]
    public async Task An_entity_row_name_is_set_only_on_apply_and_the_slug_is_untouched()
    {
        var q = await EntityAsync("I have lived in this apartment for twenty years.", "unattributed_quote-x", "quote");
        Assert.That((await maintenance.SetEntityNameAsync(q, "(unattributed quote)", apply: false)).Applied, Is.False);
        var r = await maintenance.SetEntityNameAsync(q, "(unattributed quote)", apply: true);
        Assert.That(r is { Ok: true, Applied: true }, r.Message);
        Assert.That((await maintenance.SetEntityNameAsync(q, "  ", apply: true)).Ok, Is.False);
        await using var db = await dbFactory.CreateDbContextAsync();
        var e = await db.Entities.IgnoreQueryFilters().SingleAsync(x => x.Id == q);
        Assert.That((e.Name, e.Slug), Is.EqualTo(("(unattributed quote)", "unattributed_quote-x")));
    }

    [Test]
    public async Task An_edge_is_retargeted_and_reworded_but_a_dead_edge_is_refused()
    {
        var source = await EntityAsync("Rickshaw Drone", "rickshaw-drone", "transportation");
        var wrong = await EntityAsync("Straits of Mackinac Checkpoint", "straits");
        var right = await EntityAsync("The Clybourn Corridor", "clybourn");
        long id, dead;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var u = await UniverseAsync();
            var e = new Edge { UniverseId = u, SourceId = source, TargetId = wrong, RelationType = "used_by", Description = "Used in The Narrows." };
            var d = new Edge { UniverseId = u, SourceId = source, TargetId = wrong, RelationType = "near", InvalidatedAt = DateTime.UtcNow };
            db.Edges.AddRange(e, d);
            await db.SaveChangesAsync();
            id = e.Id; dead = d.Id;
        }

        Assert.That((await maintenance.EditEdgeAsync(id, right, "Used in the Clybourn Corridor.", apply: false)).Applied, Is.False);
        var r = await maintenance.EditEdgeAsync(id, right, "Used in the Clybourn Corridor.", apply: true);
        Assert.That(r is { Ok: true, Applied: true }, r.Message);
        Assert.That((await maintenance.EditEdgeAsync(dead, right, null, apply: true)).Ok, Is.False);
        Assert.That((await maintenance.EditEdgeAsync(id, source, null, apply: true)).Ok, Is.False, "an edge onto its own source");

        await using var check = await dbFactory.CreateDbContextAsync();
        var back = await check.Edges.IgnoreQueryFilters().SingleAsync(e => e.Id == id);
        Assert.That(back.TargetId, Is.EqualTo(right));
        Assert.That(back.Description, Is.EqualTo("Used in the Clybourn Corridor."));
    }

    [Test]
    public async Task Only_a_tag_no_entity_carries_is_pruned()
    {
        var carrier = await EntityAsync("Somewhere", "somewhere");
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Tags.Add(new Tag { Name = "The_Narrows" });
            var used = new Tag { Name = "in-use" };
            db.Tags.Add(used);
            await db.SaveChangesAsync();
            db.EntityTags.Add(new EntityTag { EntityId = carrier, TagId = used.Id });
            await db.SaveChangesAsync();
        }

        Assert.That((await maintenance.PruneOrphanTagAsync("in-use", apply: true)).Ok, Is.False);
        Assert.That((await maintenance.PruneOrphanTagAsync("The_Narrows", apply: false)).Applied, Is.False);
        var r = await maintenance.PruneOrphanTagAsync("The_Narrows", apply: true);
        Assert.That(r is { Ok: true, Applied: true }, r.Message);
        await using var check = await dbFactory.CreateDbContextAsync();
        Assert.That(await check.Tags.AnyAsync(t => t.Name == "The_Narrows"), Is.False);
        Assert.That(await check.Tags.AnyAsync(t => t.Name == "in-use"), Is.True);
    }

    [Test]
    public async Task An_obligation_is_deleted_with_its_journal()
    {
        var (book, _) = await BookAsync("A beat.");
        Guid id;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var o = new NarrativeObligation { NodeId = book, Description = "THREAD: the restaurant's name (Narrows)", DedupKey = "k" };
            db.NarrativeObligations.Add(o);
            db.NarrativeObligationEvents.Add(new NarrativeObligationEvent { ObligationId = o.Id });
            await db.SaveChangesAsync();
            id = o.Id;
        }
        Assert.That((await maintenance.DeleteObligationAsync(id, apply: false)).Applied, Is.False);
        var r = await maintenance.DeleteObligationAsync(id, apply: true);
        Assert.That(r is { Ok: true, Applied: true }, r.Message);
        await using var check = await dbFactory.CreateDbContextAsync();
        Assert.That(await check.NarrativeObligations.IgnoreQueryFilters().AnyAsync(o => o.Id == id), Is.False);
        Assert.That(await check.NarrativeObligationEvents.AnyAsync(e => e.ObligationId == id), Is.False);
    }
}
