using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>Moving an entity from one custom repository to another (PRS-T994): entity row, record
/// "type", and the repo attribute of its own tags only.</summary>
[TestFixture]
public class EntityRetypeServiceTests : WorldFixture
{
    private EntityRetypeService retype = null!;
    private Guid universe, brickleg, other;
    private string beatA = "", beatB = "";

    [SetUp]
    public async Task SetUpRetype()
    {
        retype = new EntityRetypeService(dbFactory);
        var (book, _) = await BookAsync("first", "second");
        brickleg = Guid.CreateVersion7();
        other = Guid.CreateVersion7();
        await using var db = await dbFactory.CreateDbContextAsync();
        universe = await db.Nodes.IgnoreQueryFilters().Where(n => n.Id == book).Select(n => n.UniverseId).SingleAsync();
        db.RepositoryDefinitions.Add(new RepositoryDefinition { Slug = "bestiary", Name = "Bestiary", RoutePath = "/repo/bestiary" });
        db.Entities.Add(new Entity { Id = brickleg, UniverseId = universe, EntityType = "schism_arrival", Name = "brickleg", Slug = "brickleg" });
        db.Entities.Add(new Entity { Id = other, UniverseId = universe, EntityType = "schism_arrival", Name = "gutterwing", Slug = "gutterwing" });
        db.Records.Add(new Record { EntityId = brickleg, Json = """{"name":"brickleg","type":"schism_arrival","origin":"lower"}""" });
        beatA = $"The <entity repo=\"schism_arrival\" guid=\"{brickleg}\">brickleg</entity> came up.\r\n\r\n" +
                $"A <entity repo=\"schism_arrival\" guid=\"{other}\">gutterwing</entity> watched  <entity repo=\"schism_arrival\" guid=\"{brickleg}\">it</entity>.";
        beatB = $"Only <entity repo=\"schism_arrival\" guid=\"{other}\">gutterwing</entity> here.\n";
        var beats = await db.Beats.OrderBy(b => b.Text).ToListAsync();
        beats[0].Text = beatA;
        beats[1].Text = beatB;
        await db.SaveChangesAsync();
        db.BeatEntityMentions.Add(new BeatEntityMention { BeatId = beats[0].Id, EntityId = brickleg, EntityName = "brickleg", EntityType = "schism_arrival" });
        await db.SaveChangesAsync();
    }

    [Test]
    public async Task A_dry_run_changes_nothing()
    {
        var plan = await retype.PlanAsync(universe, brickleg, "bestiary");

        Assert.That(plan.Ok, Is.True, plan.Error);
        Assert.That(plan.FromType, Is.EqualTo("schism_arrival"));
        Assert.That(plan.Beats, Has.Count.EqualTo(1));
        Assert.That(plan.TagCount, Is.EqualTo(2));
        Assert.That(plan.Books, Is.EqualTo(new[] { "R" }));
        await using var db = await dbFactory.CreateDbContextAsync();
        Assert.That(await db.Entities.IgnoreQueryFilters().Where(e => e.Id == brickleg).Select(e => e.EntityType).SingleAsync(), Is.EqualTo("schism_arrival"));
        Assert.That(await db.Beats.Select(b => b.Text).ToListAsync(), Is.EquivalentTo(new[] { beatA, beatB }));
    }

    [Test]
    public async Task Apply_rewrites_only_the_entitys_tags_and_moves_the_entity_and_record()
    {
        var r = await retype.ApplyAsync(universe, brickleg, "bestiary");

        Assert.That(r.Ok, Is.True, r.Error);
        Assert.That(r.EntityTypeBack, Is.EqualTo("bestiary"));
        Assert.That(r.RecordTypeBack, Is.EqualTo("bestiary"));
        Assert.That(r.TagsStillOld, Is.Zero);
        await using var db = await dbFactory.CreateDbContextAsync();
        var texts = await db.Beats.AsNoTracking().ToListAsync();
        var a = texts.Single(b => b.Text!.Contains(brickleg.ToString()));
        Assert.That(a.Text, Is.EqualTo(beatA.Replace($"repo=\"schism_arrival\" guid=\"{brickleg}\"", $"repo=\"bestiary\" guid=\"{brickleg}\"")),
            "only the attribute moves: line endings, spacing and the other entity's tag are untouched");
        Assert.That(a.Text, Does.Contain($"<entity repo=\"schism_arrival\" guid=\"{other}\">gutterwing</entity>"));
        Assert.That(a.TextHash, Is.EqualTo(Beat.ComputeHash(a.Text)));
        Assert.That(a.LastWriteReason, Is.EqualTo(nameof(BeatWriteReason.TagMaintenance)));
        Assert.That(texts.Single(b => !b.Text!.Contains(brickleg.ToString())).Text, Is.EqualTo(beatB));
        var record = JsonNode.Parse(await db.Records.IgnoreQueryFilters().Where(x => x.EntityId == brickleg).Select(x => x.Json).SingleAsync())!;
        Assert.That(record["type"]!.ToString(), Is.EqualTo("bestiary"));
        Assert.That(record["origin"]!.ToString(), Is.EqualTo("lower"));
        Assert.That(await db.BeatEntityMentions.Where(m => m.EntityId == brickleg).Select(m => m.EntityType).SingleAsync(), Is.EqualTo("bestiary"));
        Assert.That(await db.Entities.IgnoreQueryFilters().Where(e => e.Id == other).Select(e => e.EntityType).SingleAsync(), Is.EqualTo("schism_arrival"));
    }

    [Test]
    public async Task A_missing_or_built_in_target_is_refused_and_nothing_changes()
    {
        var missing = await retype.ApplyAsync(universe, brickleg, "no_such_repo");
        Assert.That(missing.Ok, Is.False);
        Assert.That(missing.Error, Does.StartWith("target_repository_not_found"));

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.RepositoryDefinitions.Add(new RepositoryDefinition { Slug = "place", Name = "Places", RoutePath = "/repo/place" });
            await db.SaveChangesAsync();
        }
        var builtIn = await retype.ApplyAsync(universe, brickleg, "place");
        Assert.That(builtIn.Error, Does.StartWith("target_is_built_in"));

        await using var check = await dbFactory.CreateDbContextAsync();
        Assert.That(await check.Entities.IgnoreQueryFilters().Where(e => e.Id == brickleg).Select(e => e.EntityType).SingleAsync(), Is.EqualTo("schism_arrival"));
        Assert.That(await check.Beats.Select(b => b.Text).ToListAsync(), Is.EquivalentTo(new[] { beatA, beatB }));
    }

    [Test]
    public void The_rewrite_ignores_tags_of_other_guids_and_tags_already_on_the_target()
    {
        var id = Guid.NewGuid();
        var text = $"<entity guid=\"{id}\" repo=\"x\">a</entity> <entity repo=\"bestiary\" guid=\"{id}\">b</entity> <entity repo=\"x\" guid=\"{Guid.NewGuid()}\">c</entity>";
        Assert.That(EntityRetypeService.CountTagsToRewrite(text, id, "bestiary"), Is.EqualTo(1));
        Assert.That(EntityRetypeService.RewriteRepo(text, id, "bestiary"),
            Is.EqualTo(text.Replace($"<entity guid=\"{id}\" repo=\"x\">", $"<entity guid=\"{id}\" repo=\"bestiary\">")));
    }
}
