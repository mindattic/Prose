using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Discussion;

namespace Prose.UnitTests;

/// <summary>
/// What a passage is carrying, and what an edit to it broke.
///
/// <para><b>Every test here plants a real defect and asserts it is caught.</b> That is the project's
/// own rule about instruments and it is not optional here, because this engine BLOCKS the author:
/// a check that cannot catch its own planted defect is not a weak check, it is a check that will
/// report "nothing wrong" on a book that is broken, and "nothing wrong" and "could not look" are
/// the same output. Corpus-wide the findings table has 8 applied out of ~26,000; an instrument
/// earns its place by catching something, measured, not by seeming useful.</para>
///
/// <para>The negative cases matter as much. This thing stops the author working, so a check that
/// fires on an untouched beat is worse than no check at all — one false positive per save and the
/// whole feature gets switched off inside a day, which is exactly how Loop 2 died the first time.</para>
/// </summary>
[TestFixture]
public class RamificationServiceTests
{
    private SqliteConnection connection = null!;
    private DbContextOptions<ProseDbContext> options = null!;
    private RamificationService service = null!;

    private Guid bookId;
    private Guid chapterId;
    private Guid beatId;

    /// <summary>A real earlier beat, so an obligation OPENED elsewhere and paid off here has
    /// somewhere to point. An invented origin id trips the foreign key.</summary>
    private Guid earlierBeatId;

    private const string BeatText =
        "Kyle stepped into the loading dock. The pallets were empty and smelled of camphor. "
        + "He drew Silence and waited for the shift to change.";

    [SetUp]
    public void SetUp()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        options = new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(connection).Options;

        using (var ctx = new ProseDbContext(options))
        {
            ctx.Database.EnsureCreated();

            bookId = Guid.NewGuid();
            chapterId = Guid.NewGuid();
            beatId = Guid.NewGuid();
            earlierBeatId = Guid.NewGuid();

            ctx.Nodes.Add(new BookNode { Id = bookId, Kind = "book", Slug = "test-book", Title = "Test Book" });
            ctx.Nodes.Add(new ChapterNode { Id = chapterId, Kind = "chapter", Slug = "ch1", Title = "Chapter 1", ParentNodeId = bookId });
            ctx.Beats.Add(new Beat
            {
                Id = earlierBeatId, Number = 99, Kind = "prose",
                Text = "Vey wrote the name down and did not look up.",
            });
            ctx.BeatNodes.Add(new BeatNode { NodeId = chapterId, BeatId = earlierBeatId, SortKey = 0 });

            ctx.Beats.Add(new Beat { Id = beatId, Number = 100, Text = BeatText, Kind = "prose" });
            ctx.BeatNodes.Add(new BeatNode { NodeId = chapterId, BeatId = beatId, SortKey = 1 });
            ctx.SaveChanges();
        }

        var factory = new TestDbContextFactory(options);
        service = new RamificationService(
            factory,
            new BlastRadiusService(factory),
            new ContinuityService(factory),
            new GearCarryEnforcer(factory));
    }

    [TearDown]
    public void TearDown() => connection.Dispose();

    private void Edit(string newText)
    {
        using var ctx = new ProseDbContext(options);
        var beat = ctx.Beats.Single(b => b.Id == beatId);
        beat.Text = newText;
        beat.TextHash = Beat.ComputeHash(newText);
        ctx.SaveChanges();
    }

    private void AddObligation(bool closingHere, string quote)
    {
        using var ctx = new ProseDbContext(options);
        ctx.NarrativeObligations.Add(new NarrativeObligation
        {
            NodeId = bookId,
            Kind = ObligationKind.Promise,
            Description = "the ledger Vey keeps",
            // Real obligations carry one; two rows with the same empty key collide on the
            // (NodeId, DedupKey) unique index.
            DedupKey = (closingHere ? "closes:" : "opens:") + quote,
            OriginBeatId = closingHere ? earlierBeatId : beatId,
            OriginQuote = closingHere ? "Vey wrote the name down and did not look up." : quote,
            ClosingBeatId = closingHere ? beatId : null,
            ClosingQuote = closingHere ? quote : null,
        });
        ctx.SaveChanges();
    }

    // ── planted defects, each of which must be caught ──────────────────────

    [Test]
    public async Task Catches_a_deleted_line_that_a_promise_was_OPENED_on()
    {
        AddObligation(closingHere: false, quote: "The pallets were empty and smelled of camphor.");
        Edit("Kyle stepped into the loading dock. He drew Silence and waited.");

        var found = await service.ReviewAsync(bookId, beatId);

        Assert.That(found.Any(m => m.Kind == "obligation" && m.Headline.Contains("opened")), Is.True,
            "the quote the promise was opened on is gone and nothing else would notice");
    }

    [Test]
    public async Task Catches_a_deleted_line_that_a_promise_was_PAID_OFF_on()
    {
        // The most serious case in the set: a promise the book made earlier has stopped being kept.
        AddObligation(closingHere: true, quote: "He drew Silence and waited for the shift to change.");
        Edit("Kyle stepped into the loading dock. The pallets were empty and smelled of camphor.");

        var found = await service.ReviewAsync(bookId, beatId);

        var paid = found.SingleOrDefault(m => m.Kind == "obligation" && m.Headline.Contains("PAID OFF"));
        Assert.That(paid, Is.Not.Null);
        Assert.That(paid!.Detail, Does.Contain("now unpaid"));
    }

    [Test]
    public async Task Does_NOT_fire_when_the_quoted_line_is_still_there()
    {
        // The negative control that matters most. This engine blocks the author; a check that
        // fires on an untouched beat gets the whole feature turned off.
        AddObligation(closingHere: true, quote: "He drew Silence and waited for the shift to change.");

        var found = await service.ReviewAsync(bookId, beatId);

        Assert.That(found.Any(m => m.Kind == "obligation"), Is.False);
    }

    [Test]
    public async Task Catches_an_entity_link_that_went_with_the_cut_text()
    {
        var entityId = Guid.NewGuid();
        using (var ctx = new ProseDbContext(options))
        {
            ctx.Entities.Add(new Entity { Id = entityId, Name = "Silence", Slug = "silence", EntityType = "weapon" });
            ctx.SaveChanges();
        }

        var removed = $"<entity repo=\"weapon\" guid=\"{entityId}\">Silence</entity>";
        Edit("Kyle stepped into the loading dock.");

        var found = await service.ReviewAsync(bookId, beatId, removedText: removed);

        Assert.That(found.Any(m => m.Kind == "entity" && m.Headline.Contains("Silence")), Is.True);
    }

    [Test]
    public async Task Catches_an_intent_written_against_prose_that_has_since_changed()
    {
        using (var ctx = new ProseDbContext(options))
        {
            var beat = ctx.Beats.Single(b => b.Id == beatId);
            beat.Description = "Kyle waits in the dock.";
            // Stamped against the ORIGINAL wording, which the edit below then moves.
            beat.DescriptionHash = Beat.ComputeHash(BeatText);
            ctx.SaveChanges();
        }
        Edit("Kyle never went to the dock at all.");

        var found = await service.ReviewAsync(bookId, beatId);

        Assert.That(found.Any(m => m.Kind == "summary"), Is.True);
    }

    [Test]
    public async Task Catches_another_discussion_the_edit_just_detached()
    {
        using (var ctx = new ProseDbContext(options))
        {
            ctx.DiscussionThreads.Add(new DiscussionThread
            {
                BookNodeId = bookId,
                TargetKind = DiscussionTargetKind.Beat,
                TargetId = beatId,
                BeatId = beatId,
                AnchorQuote = "smelled of camphor",
                Title = "why camphor?",
                State = DiscussionThreadState.Live,
            });
            ctx.SaveChanges();
        }
        Edit("Kyle stepped into the loading dock. He drew Silence.");

        var found = await service.ReviewAsync(bookId, beatId);

        Assert.That(found.Any(m => m.Kind == "discussion"), Is.True);
    }

    [Test]
    public async Task A_clean_beat_produces_nothing()
    {
        var found = await service.ReviewAsync(bookId, beatId);

        Assert.That(found, Is.Empty,
            "an untouched beat with nothing recorded against it must not stop the author");
    }

    // ── the dismissal memory ───────────────────────────────────────────────

    [Test]
    public async Task A_dismissed_finding_stops_blocking_at_that_exact_wording()
    {
        AddObligation(closingHere: true, quote: "He drew Silence and waited for the shift to change.");
        Edit("Kyle stepped into the loading dock.");

        var before = await service.GateAsync(bookId, beatId);
        Assert.That(before, Is.Not.Empty);

        await service.DismissAsync(beatId, before[0], "the payoff moved to #418");

        Assert.That(await service.GateAsync(bookId, beatId), Is.Empty);
    }

    [Test]
    public async Task A_dismissal_expires_the_moment_the_prose_changes()
    {
        // Keyed to the TEXT, not the finding. What the author approved is no longer what is on the
        // page, so the question is open again.
        AddObligation(closingHere: true, quote: "He drew Silence and waited for the shift to change.");
        Edit("Kyle stepped into the loading dock.");

        var found = await service.GateAsync(bookId, beatId);
        await service.DismissAsync(beatId, found[0]);
        Assert.That(await service.GateAsync(bookId, beatId), Is.Empty);

        Edit("Kyle stepped into the loading dock. Someone had swept it.");

        Assert.That(await service.GateAsync(bookId, beatId), Is.Not.Empty);
    }

    [Test]
    public async Task Dismissing_one_finding_does_not_silence_a_different_one()
    {
        // Keyed on the headline, not the kind. A beat can carry two obligations and the author may
        // mean one of them and not the other; keying on the kind would silence both from one click.
        AddObligation(closingHere: false, quote: "The pallets were empty and smelled of camphor.");
        AddObligation(closingHere: true, quote: "He drew Silence and waited for the shift to change.");
        Edit("Kyle stepped into the loading dock.");

        var found = await service.GateAsync(bookId, beatId);
        Assert.That(found, Has.Count.EqualTo(2));

        await service.DismissAsync(beatId, found[0]);

        Assert.That(await service.GateAsync(bookId, beatId), Has.Count.EqualTo(1));
    }

    // ── the evidence the assessment reads ──────────────────────────────────

    [Test]
    public async Task Carried_weight_names_the_promise_this_beat_pays_off()
    {
        AddObligation(closingHere: true, quote: "He drew Silence and waited for the shift to change.");

        var carried = await service.CarriedByAsync(bookId, beatId);
        var prompt = RamificationService.ToPrompt(carried);

        Assert.That(carried.ObligationsPaidHere, Has.Count.EqualTo(1));
        Assert.That(prompt, Does.Contain("PAYS OFF A PROMISE"));
        Assert.That(prompt, Does.Contain("the ledger Vey keeps"));
    }

    [Test]
    public async Task Carried_weight_says_plainly_when_a_passage_carries_nothing()
    {
        // The other half of being useful. An assistant told nothing is carried should be able to
        // agree to a change without ceremony — a partner who objects to everything is as useless
        // as one who agrees with everything.
        var prompt = RamificationService.ToPrompt(await service.CarriedByAsync(bookId, beatId));

        Assert.That(prompt, Does.Contain("neither opens nor pays off any recorded promise"));
    }

    [Test]
    public async Task An_empty_record_is_reported_as_empty_and_not_as_safe()
    {
        // "Nothing is on record" and "this passage is free of commitments" are different claims,
        // and conflating them is how an assistant cheerfully approves cutting a load-bearing line.
        var prompt = RamificationService.ToPrompt(await service.CarriedByAsync(bookId, beatId));

        Assert.That(prompt, Does.Contain("NOT that the passage is free of commitments"));
    }

    private sealed class TestDbContextFactory(DbContextOptions<ProseDbContext> options)
        : IDbContextFactory<ProseDbContext>
    {
        public ProseDbContext CreateDbContext() => new(options);
    }
}
