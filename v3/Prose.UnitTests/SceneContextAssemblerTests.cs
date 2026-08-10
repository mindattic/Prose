using NUnit.Framework;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-10 fix: <c>BeatEntities</c> has a PRIMARY KEY on
/// (BeatId, EntityId), but a character can legitimately match a beat's text twice in one
/// <see cref="SceneContextAssembler.AssembleAsync"/> call — once via their canonical Name,
/// once via a registered alias both present in the same passage. A corpus-wide
/// <c>--backfill-entity-presence</c> run crashed on exactly this the day first-name
/// aliases were bulk-added for 105 characters. Fixed via
/// <see cref="SceneContextAssembler.DedupeByEntityId"/>. Tested directly (not through
/// PersistRosterAsync, which uses SQL Server-only DDL the SQLite test fixture can't run).
/// </summary>
[TestFixture]
public class SceneContextAssemblerTests
{
    [Test]
    public void DedupeByEntityId_CollapsesDuplicateEntityMatches_KeepingHighestScore()
    {
        var id = Guid.NewGuid();
        var roster = new List<SceneEntityRef>
        {
            new(id, "Yemina Fola", "character", "name", 3.0),
            new(id, "Yemina", "character", "name", 1.5),
        };

        var deduped = SceneContextAssembler.DedupeByEntityId(roster);

        Assert.That(deduped, Has.Count.EqualTo(1));
        Assert.That(deduped[0].EntityId, Is.EqualTo(id));
        Assert.That(deduped[0].Score, Is.EqualTo(3.0), "the higher-scoring match must survive");
    }

    [Test]
    public void DedupeByEntityId_PreservesDistinctEntities()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var roster = new List<SceneEntityRef>
        {
            new(a, "Idris Kovac", "character", "name", 3.0),
            new(b, "Bishop Alaoui", "character", "name", 3.0),
        };

        var deduped = SceneContextAssembler.DedupeByEntityId(roster);

        Assert.That(deduped, Has.Count.EqualTo(2));
        Assert.That(deduped.Select(r => r.EntityId), Is.EquivalentTo(new[] { a, b }));
    }

    [Test]
    public void DedupeByEntityId_EmptyRoster_ReturnsEmpty()
    {
        var deduped = SceneContextAssembler.DedupeByEntityId(new List<SceneEntityRef>());
        Assert.That(deduped, Is.Empty);
    }
}
