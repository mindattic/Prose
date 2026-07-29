using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Beats.TextHash must stay in lockstep with Beats.Text on every save.
///
/// Why this is guarded by tests: NodeReviewService decides which beats changed since they
/// were last scored by comparing Beat.TextHash against the hash recorded at review time. A
/// write path that updates prose and forgets the hash makes an edited beat look UNCHANGED,
/// so it keeps a score awarded to different words and nothing reports it. That is a silent
/// correctness failure, and DistributedWorkerCoordinator was exactly that bug.
/// </summary>
[TestFixture]
public class BeatTextHashStampingTests
{
    SqliteConnection connection = null!;
    DbContextOptions<StreetSamuraiDbContext> options = null!;

    [SetUp]
    public void SetUp()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        options = new DbContextOptionsBuilder<StreetSamuraiDbContext>().UseSqlite(connection).Options;
        using var ctx = new StreetSamuraiDbContext(options);
        ctx.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown() => connection.Dispose();

    static Beat NewBeat(string text) => new()
    {
        Id = Guid.NewGuid(),
        Number = Random.Shared.Next(1_000_000, 9_999_999),
        Text = text,
        Kind = "prose",
    };

    [Test]
    public void Added_beat_gets_its_hash_stamped_even_when_the_caller_forgets()
    {
        var id = Guid.Empty;
        using (var ctx = new StreetSamuraiDbContext(options))
        {
            var beat = NewBeat("The bottle had cost him money he did not have.");
            beat.TextHash = null;              // caller forgot, as DistributedWorkerCoordinator did
            ctx.Beats.Add(beat);
            ctx.SaveChanges();
            id = beat.Id;
        }
        using (var ctx = new StreetSamuraiDbContext(options))
        {
            var saved = ctx.Beats.Single(b => b.Id == id);
            Assert.That(saved.TextHash, Is.EqualTo(Beat.ComputeHash(saved.Text)),
                "a newly written beat must carry the hash of its own prose");
        }
    }

    [Test]
    public void Editing_the_prose_refreshes_the_hash()
    {
        var id = Guid.Empty;
        using (var ctx = new StreetSamuraiDbContext(options))
        {
            var beat = NewBeat("original prose");
            ctx.Beats.Add(beat);
            ctx.SaveChanges();
            id = beat.Id;
        }
        string? hashBefore;
        using (var ctx = new StreetSamuraiDbContext(options))
        {
            var beat = ctx.Beats.Single(b => b.Id == id);
            hashBefore = beat.TextHash;
            beat.Text = "the prose after a gripe cut removed a clause";
            ctx.SaveChanges();               // caller does NOT touch TextHash
        }
        using (var ctx = new StreetSamuraiDbContext(options))
        {
            var saved = ctx.Beats.Single(b => b.Id == id);
            Assert.That(saved.TextHash, Is.Not.EqualTo(hashBefore), "an edit must move the hash");
            Assert.That(saved.TextHash, Is.EqualTo(Beat.ComputeHash(saved.Text)));
        }
    }

    [Test]
    public void An_unrelated_edit_does_not_bless_an_already_wrong_hash()
    {
        // A hash that is already wrong is EVIDENCE that prose drifted. Saving an unrelated
        // field must not quietly launder it, or the drift becomes unrecoverable.
        var id = Guid.NewGuid();
        using (var ctx = new StreetSamuraiDbContext(options))
        {
            var beat = NewBeat("prose that will keep a deliberately wrong hash");
            beat.Id = id;
            ctx.Beats.Add(beat);
            ctx.SaveChanges();
            // Force a wrong hash the way raw SQL would, bypassing the change tracker.
            ctx.Database.ExecuteSqlRaw("UPDATE Beats SET TextHash = 'deadbeef' WHERE Id = {0}", id);
        }
        using (var ctx = new StreetSamuraiDbContext(options))
        {
            var beat = ctx.Beats.Single(b => b.Id == id);
            Assert.That(beat.TextHash, Is.EqualTo("deadbeef"), "precondition: hash is wrong");
            beat.Stale = !beat.Stale;         // unrelated field only
            ctx.SaveChanges();
        }
        using (var ctx = new StreetSamuraiDbContext(options))
        {
            var saved = ctx.Beats.Single(b => b.Id == id);
            Assert.That(saved.TextHash, Is.EqualTo("deadbeef"),
                "an unrelated save must leave the wrong hash visible for the audit to catch");
        }
    }
}
