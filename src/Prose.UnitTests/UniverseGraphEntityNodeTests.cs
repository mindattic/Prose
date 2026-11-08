using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Graph;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// PRS-T916: graph nodes are keyed by entity, not by name. Two same-named entities stay two nodes, each
/// Edges row lands on its own entity's node, a name only one entity has keeps its plain name slug (so
/// every name lookup is unchanged), and a cache from before per-entity nodes is rebuilt, not served.
/// </summary>
[TestFixture]
public class UniverseGraphEntityNodeTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-graph-entity-nodes-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        Directory.CreateDirectory(paths.GraphDir);
        dbFactory = TestDbFactory.For(paths, "graph-entity-nodes");
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }

    private UniverseGraphService Graph()
    {
        var graph = new UniverseGraphService(paths, new TestDatabaseService(), dbFactory);
        graph.Rebuild();
        return graph;
    }

    private Entity Add(string type, string name, string slug, string status = "canon", int ageMinutes = 0)
    {
        using var db = dbFactory.CreateDbContext();
        var e = new Entity
        {
            EntityType = type, Name = name, Slug = slug, Status = status,
            CreatedAt = DateTime.UtcNow.AddMinutes(-ageMinutes),
        };
        db.Entities.Add(e);
        db.SaveChanges();
        return e;
    }

    private void Relate(Entity from, Entity to, string kind, bool invalidated = false)
    {
        using var db = dbFactory.CreateDbContext();
        db.Edges.Add(new Edge
        {
            UniverseId = from.UniverseId, SourceId = from.Id, TargetId = to.Id, RelationType = kind,
            InvalidatedAt = invalidated ? DateTime.UtcNow : null,
        });
        db.SaveChanges();
    }

    [Test]
    public void A_live_entity_and_its_retired_namesake_are_two_nodes_and_the_live_one_keeps_its_edges()
    {
        // The SpaceGod prune: the retired pilot is older and was imported first.
        var retired = Add("pilot", "Bertrand Esposito", "pilot-old", status: "archived", ageMinutes: 60);
        var live = Add("pilot", "Bertrand Esposito", "pilot-new");
        var ship = Add("ship", "MV Swiftsure", "ship-1");
        Relate(retired, ship, "flies", invalidated: true);
        Relate(live, ship, "flies");

        var graph = Graph();

        var liveNode = graph.GetNodeForEntity(live.Id);
        var retiredNode = graph.GetNodeForEntity(retired.Id);
        Assert.That(liveNode, Is.Not.Null);
        Assert.That(retiredNode, Is.Not.Null);
        Assert.That(liveNode!.Id, Is.EqualTo("bertrand-esposito"), "the live entity keeps the name slug");
        Assert.That(liveNode.Status, Is.EqualTo("canon"));
        Assert.That(retiredNode!.Id, Is.EqualTo($"bertrand-esposito~{retired.Id:N}"));
        Assert.That(retiredNode.Status, Is.EqualTo("archived"));
        Assert.That(retiredNode.Name, Is.EqualTo("Bertrand Esposito"), "display stays by name");

        Assert.That(graph.GetEdgesFrom(liveNode.Id).Select(e => (e.Target, e.RelationType)),
            Is.EqualTo(new[] { ("mv-swiftsure", "flies") }));
        Assert.That(graph.GetEdgesFrom(retiredNode.Id), Is.Empty, "the retired twin's relation was invalidated");
        Assert.That(graph.NodeCount, Is.EqualTo(3));
    }

    [Test]
    public void Two_live_same_named_entities_stay_two_nodes_each_with_its_own_edges()
    {
        var shipNamesake = Add("ship", "Swiftsure", "swiftsure-ship", ageMinutes: 30);
        var pilotNamesake = Add("pilot", "Swiftsure", "swiftsure-pilot");
        var station = Add("station", "Ceres Yard", "ceres-yard");
        Relate(shipNamesake, station, "docked_at");
        Relate(pilotNamesake, station, "trained_at");

        var graph = Graph();

        var shipNode = graph.GetNodeForEntity(shipNamesake.Id)!;
        var pilotNode = graph.GetNodeForEntity(pilotNamesake.Id)!;
        Assert.That(shipNode.Id, Is.Not.EqualTo(pilotNode.Id));
        Assert.That(shipNode.Id, Is.EqualTo("swiftsure"), "the older entity is primary");
        Assert.That(pilotNode.Id, Is.EqualTo($"swiftsure~{pilotNamesake.Id:N}"));
        Assert.That(shipNode.NodeType, Is.EqualTo("ship"));
        Assert.That(pilotNode.NodeType, Is.EqualTo("pilot"));
        Assert.That(graph.GetEdgesFrom(shipNode.Id).Single().RelationType, Is.EqualTo("docked_at"));
        Assert.That(graph.GetEdgesFrom(pilotNode.Id).Single().RelationType, Is.EqualTo("trained_at"));
        Assert.That(graph.GetEdgesTo("ceres-yard"), Has.Count.EqualTo(2));
        Assert.That(graph.ResolveId("Swiftsure"), Is.EqualTo("swiftsure"), "a name still resolves to the primary");
        Assert.That(graph.NodeCount, Is.EqualTo(3));
    }

    [Test]
    public void A_canon_entity_is_primary_over_an_older_stub_and_a_book_scoped_namesake()
    {
        var stub = Add("stub", "Raphael", "raphael-stub", status: "stub", ageMinutes: 90);
        var scoped = Add("character", "Raphael", "raphael-epic", ageMinutes: 60);
        using (var db = dbFactory.CreateDbContext())
        {
            db.Entities.Single(e => e.Id == scoped.Id).OriginNodeId = Guid.NewGuid();
            db.SaveChanges();
        }
        var wide = Add("character", "Raphael", "raphael");

        var graph = Graph();

        Assert.That(graph.GetNodeIdForEntity(wide.Id), Is.EqualTo("raphael"));
        Assert.That(graph.GetNodeIdForEntity(scoped.Id), Is.EqualTo($"raphael~{scoped.Id:N}"));
        Assert.That(graph.GetNodeIdForEntity(stub.Id), Is.EqualTo($"raphael~{stub.Id:N}"));
        Assert.That(graph.NodeCount, Is.EqualTo(3));
    }

    [Test]
    public void A_canon_concept_keeps_its_own_slug_over_an_older_glossary_namesake_and_a_glossary_text()
    {
        // PRS-T925, GridGame: the imported glossary concept and the glossary text are both older than the
        // Prose-authored canon concept, and all three are named "Essence".
        var glossaryConcept = Add("concept", "Essence", "concept-essence", ageMinutes: 90);
        var glossaryText = Add(ProseTextService.EntityType, "Essence", "glossary-essence", ageMinutes: 60);
        var canon = Add("concept", "Essence", "essence");

        var graph = Graph();

        Assert.That(graph.GetNode("essence")!.EntityId, Is.EqualTo(canon.Id), "the slug asked for is the entity returned");
        Assert.That(graph.GetNodeIdForEntity(glossaryConcept.Id), Is.EqualTo($"essence~{glossaryConcept.Id:N}"));
        Assert.That(graph.GetNodeIdForEntity(glossaryText.Id), Is.EqualTo($"essence~{glossaryText.Id:N}"));
        Assert.That(graph.NodeCount, Is.EqualTo(3), "the term and the concept coexist, each on its own node");
    }

    [Test]
    public void A_glossary_text_never_takes_the_name_slug_from_an_entity()
    {
        var text = Add(ProseTextService.EntityType, "Burning", "glossary-burning", ageMinutes: 60);
        var concept = Add("concept", "Burning", "concept-burning");

        var graph = Graph();

        Assert.That(graph.GetNodeIdForEntity(concept.Id), Is.EqualTo("burning"));
        Assert.That(graph.GetNodeIdForEntity(text.Id), Is.EqualTo($"burning~{text.Id:N}"));
    }

    [Test]
    public void Unique_names_keep_their_name_slug_ids_and_edges_as_before()
    {
        var kyle = Add("character", "Kyle Marsh", "kyle");
        var axiom = Add("corponation", "Axiom Industries", "axiom");
        Relate(kyle, axiom, "works_for");

        var graph = Graph();

        Assert.That(graph.AllNodes().Select(n => n.Id).OrderBy(x => x), Is.EqualTo(new[] { "axiom-industries", "kyle-marsh" }));
        Assert.That(graph.GetNode("kyle-marsh")!.EntityId, Is.EqualTo(kyle.Id));
        Assert.That(graph.ResolveId("Kyle Marsh"), Is.EqualTo("kyle-marsh"));
        Assert.That(graph.GetEdgesFrom("kyle-marsh").Single().Target, Is.EqualTo("axiom-industries"));
    }

    [Test]
    public void The_cache_round_trips_entity_nodes_and_a_pre_entity_cache_is_not_served()
    {
        var retired = Add("pilot", "Bertrand Esposito", "pilot-old", status: "archived", ageMinutes: 60);
        var live = Add("pilot", "Bertrand Esposito", "pilot-new");
        Graph();   // Rebuild saves the cache

        var reloaded = new UniverseGraphService(paths, new TestDatabaseService(), dbFactory);
        reloaded.Load();
        Assert.That(reloaded.AllNodes(), Has.Count.EqualTo(2));
        Assert.That(reloaded.GetNodeIdForEntity(live.Id), Is.EqualTo("bertrand-esposito"));
        Assert.That(reloaded.GetNodeIdForEntity(retired.Id), Is.EqualTo($"bertrand-esposito~{retired.Id:N}"));

        // A cache written before per-entity nodes: no Format, one merged node.
        var cache = Directory.GetFiles(paths.GraphDir, "*_universe_graph.json").Single();
        File.WriteAllText(cache, JsonSerializer.Serialize(new
        {
            Nodes = new[] { new { Id = "bertrand-esposito", Name = "Bertrand Esposito", NodeType = "pilot", Status = "archived" } },
            Edges = Array.Empty<object>(),
        }));
        var stale = new UniverseGraphService(paths, new TestDatabaseService(), dbFactory);
        Assert.That(stale.NodeCount, Is.EqualTo(2), "the merged cache is ignored and the first read rebuilds from SQL");
        Assert.That(stale.GetNode("bertrand-esposito")!.Status, Is.EqualTo("canon"));
        Assert.That(stale.GetNodeIdForEntity(retired.Id), Is.EqualTo($"bertrand-esposito~{retired.Id:N}"));
    }
}
