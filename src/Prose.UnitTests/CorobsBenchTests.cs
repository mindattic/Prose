using NUnit.Framework;
using Prose.Core.Services.Corobs;

namespace Prose.UnitTests;

/// <summary>The pure half of <c>prose --corobs bench</c> (PRS-T865): both ways scored on the same
/// truth, the same seed giving the same numbers, and the new way finding a pair the Edge graph
/// cannot see because only the records share a value.</summary>
[TestFixture]
public class CorobsBenchTests
{
    private static Guid G(int i) => new($"00000000-0000-0000-0000-{i:D12}");

    private static CorobsBenchInput World()
    {
        // 60 filler entities so the shared values stay rare (max-share), plus a crew of three who
        // share a rare turf in their records but have no Edge, and one Edge pair.
        var ents = new List<CorobsBenchEntity>();
        for (var i = 1; i <= 60; i++)
            ents.Add(new(G(i), "character", $"Filler {i}", $$"""{"name":"Filler {{i}}","home_turf":"Block {{i}}"}"""));
        ents.Add(new(G(101), "character", "Ash Vane", """{"name":"Ash Vane","home_turf":"Skillet Row"}"""));
        ents.Add(new(G(102), "character", "Bo Kettle", """{"name":"Bo Kettle","home_turf":"Skillet Row"}"""));
        ents.Add(new(G(103), "place", "Skillet Row", """{"name":"Skillet Row","coordinates":{"lat":0,"lng":0}}"""));
        ents.Add(new(G(104), "character", "Cy Moss", """{"name":"Cy Moss"}"""));
        ents.Add(new(G(105), "character", "Dee Moss", """{"name":"Dee Moss"}"""));

        var edges = new List<CorobsBenchEdge> { new(G(104), G(105), "partner_of", 1) };
        var mentions = new List<(Guid, Guid)>();
        for (var b = 0; b < 3; b++)
        {
            var beat = Guid.NewGuid();
            mentions.Add((beat, G(101))); mentions.Add((beat, G(102)));
            var beat2 = Guid.NewGuid();
            mentions.Add((beat2, G(104))); mentions.Add((beat2, G(105)));
        }
        return new CorobsBenchInput(ents, edges, mentions);
    }

    [Test]
    public void New_way_recovers_a_record_only_pair_the_edge_graph_misses()
    {
        var r = CorobsBenchMath.Run(World(), new CorobsBenchOptions(Holdout: 0.99, MaxShare: 0.1));

        Assert.That(r.TruthPairs, Is.EqualTo(2));
        Assert.That(r.Old.Recall.Recovered, Is.EqualTo(1), "the Edge graph knows only Cy–Dee");
        Assert.That(r.New.Recall.Recovered, Is.EqualTo(2), "Corobs also finds Ash–Bo through the shared turf (and Cy–Dee through the graph ply)");
    }

    [Test]
    public void Without_the_graph_ply_the_new_way_sees_only_the_records()
    {
        var r = CorobsBenchMath.Run(World(), new CorobsBenchOptions(Holdout: 0.99, MaxShare: 0.1, GraphPly: false));
        Assert.That(r.New.Recall.Recovered, Is.EqualTo(1));
    }

    [Test]
    public void Same_seed_same_numbers_and_contradictions_are_could_not_look_without_hits()
    {
        var a = CorobsBenchMath.Run(World(), new CorobsBenchOptions(Seed: 7, MaxShare: 0.1));
        var b = CorobsBenchMath.Run(World(), new CorobsBenchOptions(Seed: 7, MaxShare: 0.1));

        Assert.That(CorobsBenchMath.Table(a), Is.EqualTo(CorobsBenchMath.Table(b)));
        Assert.That(a.New.Surfaced, Is.Null);
        Assert.That(a.New.CascadeYield, Is.Null);
        Assert.That(a.Old.Surfaced, Is.Null, "no hand baseline given");
    }

    [Test]
    public void Hand_baseline_fills_the_old_column()
    {
        var r = CorobsBenchMath.Run(World(), new CorobsBenchOptions(MaxShare: 0.1), new CorobsHandBaseline(80, 70, 67, 296, "pass 2"));
        Assert.That(r.Old.Fixes, Is.EqualTo(67));
        Assert.That(r.Old.CascadeYield, Is.EqualTo(296 / 67.0).Within(1e-9));
        Assert.That(CorobsBenchMath.Detail("glmz", r, r.Old).Length, Is.LessThanOrEqualTo(1000));
    }
}
