using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Guards the two properties the entity wiki depends on and that are expensive to get wrong.
///
/// <para><b>1. Saving an entity must cost nothing.</b> Until 2026-09-12 an entity save fired
/// <c>EntityRamificationService.ScanDownstreamAsync</c> as fire-and-forget: one LLM call per
/// (mentioning beat × every later beat in that beat's node), uncapped and un-costed. Editing a
/// protagonist's description was worth thousands of calls. The wiki makes entity editing a normal,
/// frequent act, so "a save makes no LLM call" is now a tested invariant, not a convention.</para>
///
/// <para><b>2. The contradiction scan must check each beat once.</b> The old walk re-checked the
/// same downstream beat once per upstream mention — quadratic within a chapter. These tests pin
/// both the dedup and the cap.</para>
/// </summary>
[TestFixture]
public class EntityWikiTests
{
    SqliteConnection connection = null!;
    DbContextOptions<ProseDbContext> options = null!;

    [SetUp]
    public void SetUp()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        options = new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(connection).Options;
        using var ctx = new ProseDbContext(options);
        ctx.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown() => connection.Dispose();

    // ── Fakes ───────────────────────────────────────────────────────────────

    /// <summary>Counts calls, and can be made to fail the test simply by being called at all.</summary>
    sealed class CountingLlm : ILlmService
    {
        public int Calls;
        public string Reply = "REASON: nothing conflicts here.\nVERDICT: CONSISTENT";

        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);

        public Task<string> GenerateAsync(string system, string user, double temperature = 0.8,
            int maxTokens = 4096, string? model = null, CancellationToken ct = default)
        {
            Interlocked.Increment(ref Calls);
            return Task.FromResult(Reply);
        }
    }

    sealed class TestDbFactory(DbContextOptions<ProseDbContext> options)
        : IDbContextFactory<ProseDbContext>
    {
        public ProseDbContext CreateDbContext() => new(options);
    }

    EntityRamificationService NewService(CountingLlm llm) =>
        new(new TestDbFactory(options), llm, new TokenLedger(),
            NullLogger<EntityRamificationService>.Instance);

    Guid SeedEntityWithMentions(int beatCount)
    {
        using var ctx = new ProseDbContext(options);

        var entityId = Guid.NewGuid();
        ctx.Entities.Add(new Entity
        {
            Id = entityId,
            EntityType = "character",
            Name = "Testsubject",
            Slug = "testsubject",
            Status = "canon",
            Description = "A person who exists only in this test.",
        });

        for (var i = 0; i < beatCount; i++)
        {
            var beat = new Beat
            {
                Id = Guid.NewGuid(),
                Number = 1000 + i,
                Text = $"Testsubject did something in beat {i}.",
                Kind = "prose",
            };
            ctx.Beats.Add(beat);

            ctx.BeatEntityMentions.Add(new BeatEntityMention
            {
                BeatId = beat.Id,
                EntityId = entityId,
                EntityName = "Testsubject",
                EntityType = "character",
                CreatedAt = DateTime.UtcNow,
            });
        }

        ctx.SaveChanges();
        return entityId;
    }

    [Test]
    public async Task Scan_reports_the_count_it_actually_checked_without_a_progress_callback()
    {
        // Regression: the loop used to read `progress?.Report((++done, total))`. The
        // null-conditional short-circuits the whole expression — arguments included — so with no
        // progress callback the counter never moved and a scan that really did call the model N
        // times reported "0 beats checked". The LLM spend was real; the report was not.
        var entityId = SeedEntityWithMentions(beatCount: 3);
        var llm = new CountingLlm();

        var result = await NewService(llm).ScanForContradictionsAsync(entityId, maxBeats: 100, progress: null);

        Assert.That(llm.Calls, Is.EqualTo(3));
        Assert.That(result.Checked, Is.EqualTo(llm.Calls),
            "What the scan reports having checked must equal what it actually paid for.");
    }

    // ── The save path is free ───────────────────────────────────────────────

    [Test]
    public async Task Saving_an_entity_makes_no_LLM_call()
    {
        var entityId = SeedEntityWithMentions(beatCount: 5);
        var llm = new CountingLlm();
        var svc = NewService(llm);

        await svc.ProcessEntityUpdateAsync(entityId, "Testsubject");

        Assert.That(llm.Calls, Is.Zero,
            "An entity save must never reach the LLM. This is the regression that made editing a " +
            "heavily-mentioned character cost thousands of uncapped calls.");
    }

    [Test]
    public async Task Saving_an_entity_flags_every_beat_that_mentions_it()
    {
        var entityId = SeedEntityWithMentions(beatCount: 5);
        var svc = NewService(new CountingLlm());

        var flagged = await svc.ProcessEntityUpdateAsync(entityId, "Testsubject");

        Assert.That(flagged, Is.EqualTo(5));

        using var ctx = new ProseDbContext(options);
        Assert.That(ctx.Beats.Count(b => b.EntityStale), Is.EqualTo(5));
    }

    [Test]
    public async Task Saving_an_entity_nothing_mentions_flags_nothing_and_costs_nothing()
    {
        using (var ctx = new ProseDbContext(options))
        {
            ctx.Entities.Add(new Entity
            {
                Id = Guid.NewGuid(), EntityType = "place", Name = "Nowhere",
                Slug = "nowhere", Status = "stub",
            });
            ctx.SaveChanges();
        }

        var llm = new CountingLlm();
        var svc = NewService(llm);

        var flagged = await svc.ProcessEntityUpdateAsync(Guid.NewGuid(), "Nowhere");

        Assert.That(flagged, Is.Zero);
        Assert.That(llm.Calls, Is.Zero);
    }

    // ── The paid scan is bounded ────────────────────────────────────────────

    [Test]
    public async Task Contradiction_scan_checks_each_beat_exactly_once()
    {
        var entityId = SeedEntityWithMentions(beatCount: 3);
        var llm = new CountingLlm();
        var svc = NewService(llm);

        var result = await svc.ScanForContradictionsAsync(entityId, maxBeats: 100);

        Assert.That(llm.Calls, Is.EqualTo(3),
            "One call per beat. The old downstream walk re-checked the same beat once per upstream " +
            "mention, which is quadratic within a chapter.");
        Assert.That(result.Checked, Is.EqualTo(3));
        Assert.That(result.Skipped, Is.Zero);
    }

    [Test]
    public async Task Contradiction_scan_finds_the_entity_regardless_of_the_ambient_universe()
    {
        // Looking an entity up by an id the caller already holds must not go through the universe
        // query filter: with no ambient universe pinned, the filter hides the row and the scan
        // silently reports "0 beats checked" as though the entity had no mentions at all.
        var entityId = SeedEntityWithMentions(beatCount: 3);

        var result = await NewService(new CountingLlm()).ScanForContradictionsAsync(entityId, maxBeats: 10);

        Assert.That(result.Checked, Is.EqualTo(3),
            "An explicit-id lookup must use IgnoreQueryFilters, or a wrong ambient universe reads " +
            "as a clean bill of health.");
    }

    [Test]
    public async Task Contradiction_scan_honours_its_cap_and_reports_what_it_skipped()
    {
        var entityId = SeedEntityWithMentions(beatCount: 10);
        var llm = new CountingLlm();
        var svc = NewService(llm);

        var result = await svc.ScanForContradictionsAsync(entityId, maxBeats: 4);

        Assert.That(llm.Calls, Is.EqualTo(4));
        Assert.That(result.Checked, Is.EqualTo(4));
        Assert.That(result.Skipped, Is.EqualTo(6),
            "A truncated scan must say so rather than quietly implying the whole book was checked.");
    }

    [Test]
    public async Task Contradiction_scan_reports_a_hit_only_on_an_explicit_CONFLICT_verdict()
    {
        var entityId = SeedEntityWithMentions(beatCount: 1);
        var llm = new CountingLlm
        {
            Reply = "REASON: the beat says she is left-handed and the record says right.\nVERDICT: CONFLICT",
        };

        var result = await NewService(llm).ScanForContradictionsAsync(entityId, maxBeats: 10);

        Assert.That(result.Hits, Has.Count.EqualTo(1));
        Assert.That(result.Hits[0].Reason, Does.Contain("left-handed"),
            "The reason is what makes a finding reviewable; a bare verdict is not actionable.");
    }

    [Test]
    public async Task A_malformed_LLM_reply_never_manufactures_a_finding()
    {
        var entityId = SeedEntityWithMentions(beatCount: 1);
        var llm = new CountingLlm { Reply = "I'm not sure, possibly?" };

        var result = await NewService(llm).ScanForContradictionsAsync(entityId, maxBeats: 10);

        Assert.That(result.Hits, Is.Empty,
            "Unparseable output must fail closed. A false contradiction costs author time to chase.");
    }

    [Test]
    public async Task Counting_mentioning_beats_is_free()
    {
        var entityId = SeedEntityWithMentions(beatCount: 7);
        var llm = new CountingLlm();

        var count = await NewService(llm).CountMentioningBeatsAsync(entityId);

        Assert.That(count, Is.EqualTo(7));
        Assert.That(llm.Calls, Is.Zero, "Pricing the scan must not itself cost anything.");
    }

    // ── The history diff ────────────────────────────────────────────────────

    static EntitySnapshot Snap(params (string Key, string? Value)[] fields) =>
        new(DateTime.UtcNow, fields.ToDictionary(f => f.Key, f => f.Value));

    [Test]
    public void Diff_reports_added_removed_and_changed_fields()
    {
        var before = Snap(("Name", "Kyle"), ("Status", "stub"), ("Gone", "x"));
        var after = Snap(("Name", "Kyle"), ("Status", "canon"), ("New", "y"));

        var diff = EntityHistoryService.Diff(before, after);

        Assert.Multiple(() =>
        {
            Assert.That(diff.Any(d => d.Field == "Name"), Is.False, "Unchanged fields are not noise.");
            Assert.That(diff.Single(d => d.Field == "Status").Kind, Is.EqualTo(FieldChangeKind.Changed));
            Assert.That(diff.Single(d => d.Field == "Status").Before, Is.EqualTo("stub"));
            Assert.That(diff.Single(d => d.Field == "Status").After, Is.EqualTo("canon"));
            Assert.That(diff.Single(d => d.Field == "Gone").Kind, Is.EqualTo(FieldChangeKind.Removed));
            Assert.That(diff.Single(d => d.Field == "New").Kind, Is.EqualTo(FieldChangeKind.Added));
        });
    }

    [Test]
    public void Diff_against_a_missing_earlier_version_reads_as_all_added()
    {
        var diff = EntityHistoryService.Diff(null, Snap(("Name", "Kyle"), ("Status", "canon")));

        Assert.That(diff, Has.Count.EqualTo(2));
        Assert.That(diff.All(d => d.Kind == FieldChangeKind.Added), Is.True,
            "An entity that did not exist at the earlier instant has every field as new.");
    }

    [Test]
    public void Diff_distinguishes_an_empty_value_from_an_absent_field()
    {
        var diff = EntityHistoryService.Diff(
            Snap(("Note", null)),
            Snap(("Note", "")));

        Assert.That(diff.Single().Kind, Is.EqualTo(FieldChangeKind.Changed),
            "null (never set) and \"\" (explicitly cleared) are different facts about the record.");
    }
}
