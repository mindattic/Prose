using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;
using Prose.Core.Services.Factory;

namespace Prose.UnitTests;

/// <summary>
/// Pronoun tags and beat contributions (author 2026-10-12, WO:01a13278): every pronoun carries an
/// entity tag or the "not a character" mark and survives a save where it was written; every
/// tagged character has one line per beat, bound to the beat's text; the gate refuses anything
/// missing, extra, stale or untagged; a character's history and the story's relations are read
/// back from the lines.
/// </summary>
[TestFixture]
public class BeatContributionTests : WorldFixture
{
    private BeatContributionService contributions = null!;
    private const string NoneMark = "<entity repo=\"none\" guid=\"00000000-0000-0000-0000-000000000000\">";

    [SetUp]
    public void SetUpContributions() => contributions = new BeatContributionService(dbFactory);

    private async Task<CharacterData> PersonAsync(string name, string pronouns)
    {
        var c = NewCharacter(name);
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.Set<Character>().IgnoreQueryFilters().SingleAsync(x => x.Id == Guid.Parse(c.Id));
        row.Pronouns = pronouns;
        await db.SaveChangesAsync();
        return c;
    }

    private static string Tag(CharacterData c, string word) => $"<entity repo=\"character\" guid=\"{Guid.Parse(c.Id)}\">{word}</entity>";

    private async Task<string> TextAsync(Guid beat)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Beats.Where(b => b.Id == beat).Select(b => b.Text!).SingleAsync();
    }

    [Test]
    public async Task Pronoun_tags_survive_a_save_where_they_were_written_and_two_hes_stay_two_people()
    {
        var tomas = await PersonAsync("Tomas Alvarado", "he/him");
        var levin = await PersonAsync("Levin Marsh", "he/him");
        var (book, _) = await BookAsync("Placeholder.");
        var beat = (await BeatIdsAsync(book))[0];

        await workbench.UpdateBeatTextAsync(beat,
            $"Tomas Alvarado watched Levin Marsh. {Tag(tomas, "He")} waited; {Tag(levin, "he")} did not.", BeatWriteReason.AuthorEdit);

        var text = await TextAsync(beat);
        Assert.That(text, Does.Contain(Tag(tomas, "He")));
        Assert.That(text, Does.Contain(Tag(levin, "he")));
        // The old bug: a pinned "he" became a surface name and tagged EVERY "he" as one person.
        Assert.That(BeatMarkup.ExtractPronounTags(text).Count(t => t.EntityId == Guid.Parse(tomas.Id)), Is.EqualTo(1));
        Assert.That(BeatMarkup.StripEntityTags(text), Is.EqualTo("Tomas Alvarado watched Levin Marsh. He waited; he did not."));
        Assert.That(BeatMarkup.UntaggedPronouns(text), Is.Empty);
    }

    [Test]
    public async Task A_pronoun_that_contradicts_the_record_is_refused_and_the_none_mark_is_kept()
    {
        var bram = await PersonAsync("Bram Osei", "he/him");
        var (book, _) = await BookAsync("Placeholder.");
        var beat = (await BeatIdsAsync(book))[0];

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            workbench.UpdateBeatTextAsync(beat, $"Bram Osei sat. {Tag(bram, "She")} waited.", BeatWriteReason.AuthorEdit));
        Assert.That(await TextAsync(beat), Is.EqualTo("Placeholder."), "a refused save writes nothing");

        await workbench.UpdateBeatTextAsync(beat, $"Bram Osei sat. {NoneMark}It</entity> was raining.", BeatWriteReason.AuthorEdit);
        var text = await TextAsync(beat);
        Assert.That(text, Does.Contain(NoneMark + "It</entity>"));
        Assert.That(BeatMarkup.ExtractEntityGuids(text), Does.Not.Contain(Guid.Empty), "the mark names no entity");
        Assert.That(BeatMarkup.Validate(text), Is.Empty);
    }

    [Test]
    public async Task Lines_must_name_exactly_the_tagged_characters_and_go_stale_on_any_edit()
    {
        var bram = await PersonAsync("Bram Osei", "he/him");
        var ekow = await PersonAsync("Ekow Ato", "he/him");
        var (book, _) = await BookAsync("Placeholder.");
        var beat = (await BeatIdsAsync(book))[0];
        await workbench.UpdateBeatTextAsync(beat, $"Ekow Ato asked twice. {Tag(bram, "He")} told him everything.".Replace("him everything", Tag(ekow, "him") + " everything"), BeatWriteReason.AuthorEdit);
        var (b, e) = (Guid.Parse(bram.Id), Guid.Parse(ekow.Id));

        Assert.ThrowsAsync<InvalidOperationException>(() => contributions.SetAsync(beat,
            new Dictionary<Guid, ContributionInput> { [b] = new("Tells Ekow everything.", []) }), "Ekow has no line");
        var stranger = await PersonAsync("Someone Else", "she/her");
        Assert.ThrowsAsync<InvalidOperationException>(() => contributions.SetAsync(beat, new Dictionary<Guid, ContributionInput>
        {
            [b] = new("Tells Ekow everything.", []), [e] = new("Questions Osei.", []), [Guid.Parse(stranger.Id)] = new("Not here.", []),
        }), "a line for a character the beat does not tag");

        await contributions.SetAsync(beat, new Dictionary<Guid, ContributionInput>
        {
            [b] = new("Tells Ekow everything the room saw.", []),
            [e] = new("Questions Osei and learns the trail.", []),
        });
        Assert.That((await contributions.CheckAsync(beat)).Ok, Is.True);

        await workbench.UpdateBeatTextAsync(beat, (await TextAsync(beat)).Replace("twice", "once"), BeatWriteReason.AuthorEdit);
        var check = await contributions.CheckAsync(beat);
        Assert.That(check.Stale, Is.EquivalentTo(new[] { b, e }));
    }

    [Test]
    public async Task The_gate_holds_F3_until_every_pronoun_is_tagged_and_every_line_is_current()
    {
        var bram = await PersonAsync("Bram Osei", "he/him");
        var (book, _) = await BookAsync("Placeholder.");
        var beat = (await BeatIdsAsync(book))[0];
        await workbench.UpdateBeatTextAsync(beat, "Bram Osei sat down. He waited.", BeatWriteReason.AuthorEdit);
        await rulings.RecordAsync(new RulingDraft(RulingKinds.Gate, RulingGates.Contributions, book));

        var s = await factory.StatusAsync(book);
        Assert.That(s.Units[0].Stations["F3"].Pass, Is.False);
        Assert.That(s.Units[0].Stations["F3"].Detail, Does.Contain("untagged pronoun"));

        await workbench.UpdateBeatTextAsync(beat, $"Bram Osei sat down. {Tag(bram, "He")} waited.", BeatWriteReason.AuthorEdit);
        s = await factory.StatusAsync(book);
        Assert.That(s.Units[0].Stations["F3"].Detail, Does.Contain("missing line"));

        await contributions.SetAsync(beat, new Dictionary<Guid, ContributionInput> { [Guid.Parse(bram.Id)] = new("Sits down and waits.", []) });
        s = await factory.StatusAsync(book);
        Assert.That(s.Units[0].Stations["F3"].Pass, Is.True);
    }

    [Test]
    public async Task History_reads_back_in_order_with_citations_and_edges_answer_who_killed_him()
    {
        var bram = await PersonAsync("Bram Osei", "he/him");
        var ekow = await PersonAsync("Ekow Ato", "he/him");
        var (book, _) = await BookAsync("Placeholder one.", "Placeholder two.", "Placeholder three.");
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var n = await db.Nodes.SingleAsync(x => x.Id == book);
            n.NodeCode = "VATD";
            await db.SaveChangesAsync();
        }
        var beats = await BeatIdsAsync(book);
        var (b, e) = (Guid.Parse(bram.Id), Guid.Parse(ekow.Id));

        await workbench.UpdateBeatTextAsync(beats[0], $"Bram Osei dropped out of {Tag(bram, "his")} legs.", BeatWriteReason.AuthorEdit);
        await workbench.UpdateBeatTextAsync(beats[2], $"Ekow Ato killed Bram Osei in the chair.", BeatWriteReason.AuthorEdit);
        await contributions.SetAsync(beats[0], new Dictionary<Guid, ContributionInput> { [b] = new("Loses his financed legs to the recall.", []) });
        await contributions.SetAsync(beats[2], new Dictionary<Guid, ContributionInput>
        {
            [b] = new("Is killed in his chair by Ekow.", [new ContributionEdge("was_killed_by", e)]),
            [e] = new("Kills Osei to close the trail.", []),
        });

        var history = await contributions.HistoryAsync(b);
        Assert.That(history.Select(h => h.Line), Is.EqualTo(new[] { "Loses his financed legs to the recall.", "Is killed in his chair by Ekow." }));
        Assert.That(history.All(h => h.BookCode == "VATD" && h.Chapter == 1 && h.State == "current"), Is.True);

        var killed = await contributions.EdgesAsync(b, "was_killed_by");
        Assert.That(killed, Has.Count.EqualTo(1));
        Assert.That(killed[0].TargetName, Is.EqualTo("Ekow Ato"));
        Assert.That(killed[0].State, Is.EqualTo("current"));

        // An edit leaves the edge in place but stale, and rewriting the lines replaces it.
        await workbench.UpdateBeatTextAsync(beats[2], "Ekow Ato killed Bram Osei in his chair.", BeatWriteReason.AuthorEdit);
        Assert.That((await contributions.EdgesAsync(b, "was_killed_by"))[0].State, Is.EqualTo("stale"));
        await contributions.SetAsync(beats[2], new Dictionary<Guid, ContributionInput>
        {
            [b] = new("Is killed in his chair by Ekow.", [new ContributionEdge("was_killed_by", e)]),
            [e] = new("Kills Osei to close the trail.", []),
        });
        Assert.That(await contributions.EdgesAsync(b, "was_killed_by"), Has.Count.EqualTo(1), "replaced, not duplicated");
    }

    [Test]
    public void Untagged_pronouns_count_contractions_and_skip_words_that_only_contain_them()
    {
        var found = BeatMarkup.UntaggedPronouns("He's there. Theirs is the hive; mother and themes. I said it.");
        Assert.That(found.Select(f => f.Word), Is.EqualTo(new[] { "He", "Theirs", "I", "it" }));
    }

    [Test]
    public async Task A_bare_ambiguous_name_is_flagged_untagged_and_clears_once_one_occurrence_is_pinned()
    {
        // Two characters share one name, universe-wide, with no book-scoped tiebreaker — the exact
        // shape that made the save path silently drop a tag on an ambiguous name (author 2026-09-04,
        // four "Marisol" tags lost against five Marisols; author 2026-10-13, "Sable" the same way
        // when ALL her tags were stripped instead of one kept to pin the rest).
        var sableA = NewCharacter("Sable");
        var sableB = NewCharacter("Sable");
        var (bA, bB) = (Guid.Parse(sableA.Id), Guid.Parse(sableB.Id));
        var (book, _) = await BookAsync("Placeholder.");
        var beat = (await BeatIdsAsync(book))[0];

        // Bare mention, no tag anywhere: the ambiguity guard correctly refuses to guess, and that
        // refusal is now visible instead of only discoverable by a human noticing a missing line.
        await workbench.UpdateBeatTextAsync(beat, "Sable routed the job through the Loop.", BeatWriteReason.AuthorEdit);
        var check = await contributions.CheckAsync(beat);
        Assert.That(check.UntaggedNames.Select(n => n.Text), Does.Contain("Sable"));
        var flagged = check.UntaggedNames.Single(n => n.Text == "Sable");
        Assert.That(flagged.Candidates, Is.EquivalentTo(new[] { bA, bB }));

        // Pinning ONE occurrence resolves every bare occurrence of the same text in the same save
        // (WithPinnedMentions + EntityMentionScanner.Scan) — nothing left bare to flag.
        await workbench.UpdateBeatTextAsync(beat, $"{Tag(sableA, "Sable")} routed the job through the Loop.", BeatWriteReason.AuthorEdit);
        check = await contributions.CheckAsync(beat);
        Assert.That(check.UntaggedNames, Is.Empty);
        Assert.That(BeatMarkup.ExtractEntityGuids(await TextAsync(beat)), Does.Contain(bA));
    }
}
