using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.UnitTests.Fixtures;

namespace Prose.UnitTests;

[TestFixture, NonParallelizable]
public class RepetitionLintServiceTests
{
    private CanonFixture fixture = null!;
    private string tempRoot = "";
    private FindingsService findings = null!;
    private RepetitionLintService lint = null!;
    private IDisposable scope = null!;
    private double nextSortKey = 1000;
    private int nextNumber = 100;

    [SetUp]
    public void SetUp()
    {
        fixture = CanonFixture.Create();
        scope = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-replint-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        findings = new FindingsService(fixture.Factory, new TestPathProviderWithRoot(tempRoot));
        lint = new RepetitionLintService(fixture.Factory, findings, NullLogger<RepetitionLintService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        scope.Dispose();
        fixture.Dispose();
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private Guid AddBeat(string text, double? dialogueProportion = null, int wordCount = 0)
    {
        using var db = fixture.Factory.CreateDbContext();
        var beat = new Beat
        {
            Id = Guid.CreateVersion7(),
            Number = nextNumber++,
            Text = text,
            TextHash = Beat.ComputeHash(text),
            Kind = "prose",
            SceneType = "scene",
        };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = fixture.FlatChapter, BeatId = beat.Id, SortKey = nextSortKey += 100 });
        if (dialogueProportion is { } p)
            db.BeatProseMetrics.Add(new BeatProseMetrics
            {
                BeatId = beat.Id, NodeId = fixture.FlatChapter, WordCount = wordCount, DialogueProportion = p,
            });
        db.SaveChanges();
        return beat.Id;
    }

    private async Task<IReadOnlyList<string>> LintLines(bool dryRun = true) =>
        (await lint.LintAsync(CanonFixture.SharedBookSlug, dryRun)).Lines;

    private IReadOnlyList<Finding> LintFindings() =>
        findings.List(null, 1000, "node:" + CanonFixture.SharedBookSlug)
            .Where(f => f.Summary.StartsWith("LINT ")).ToList();

    [Test]
    public async Task Echo_ThreeNearbyHitsOfADistinctiveWord_IsFlagged()
    {
        AddBeat("The lantern hissed on its hook. A second lantern burned lower. Then every lantern died at once.");

        var lines = await LintLines();

        Assert.That(lines, Has.Some.Contains("\"lantern\" echoes 3x"));
    }

    [Test]
    public async Task Echo_StopwordsShortWordsAndEntityNames_AreIgnored()
    {
        AddBeat("Would she would he would they would. Halloway saw Halloway see Halloway. Dog dog dog dog.");

        var lines = await LintLines();

        Assert.That(lines, Has.None.Contains("\"would\" echoes"));
        Assert.That(lines, Has.None.Contains("\"halloway\" echoes"), "Wren Halloway is an entity; her name repeating is not an echo");
        Assert.That(lines, Has.None.Contains("\"dog\" echoes"), "words under five letters are not echo candidates");
    }

    [Test]
    public async Task CrutchPhrase_RepeatedInOneBeat_ReportsOnlyTheMaximalPhrase()
    {
        AddBeat("The cold blue lantern swung. Much later, the cold blue lantern swung again.");

        var lines = await LintLines();

        Assert.That(lines, Has.Some.Contains("phrase \"cold blue lantern swung\" appears 2x in one beat"));
        Assert.That(lines, Has.None.Contains("phrase \"cold blue lantern\" appears"),
            "a 3-gram fully explained by a recurring 4-gram is suppressed");
    }

    [Test]
    public async Task CrutchPhrase_OncePerBeatAcrossAChapter_IsFlaggedAtThreeUses()
    {
        AddBeat("She held a brass compass needle up to the window.");
        AddBeat("Nothing about brass compass needle work was simple.");
        AddBeat("Only brass compass needle magic would find him.");

        var lines = await LintLines();

        Assert.That(lines, Has.Some.Contains("chapter 'Flat Chapter': phrase \"brass compass needle\" appears 3x across the chapter"));
        Assert.That(lines, Has.None.Contains("phrase \"brass compass needle\" appears 3x in one beat"));
    }

    [Test]
    public async Task PetWord_InThirtyPercentOfBeats_IsFlaggedOnlyWithTwentyBeats()
    {
        string[] places = ["harbor", "market", "stairwell", "rooftop", "station", "garden", "chapel", "tunnel",
                           "bridge", "kitchen", "alley", "foundry", "library", "orchard", "quarry", "terrace"];
        foreach (var place in places)
            AddBeat($"Somewhere near the {place}, a lantern was waiting for her.");

        var lines = await LintLines();

        Assert.That(lines, Has.Some.Matches<string>(l => l.Contains("pet words") && l.Contains("lantern (16/21 beats)")));
    }

    [Test]
    public async Task PetWord_UnderTwentyBeats_IsNotChecked()
    {
        for (int i = 0; i < 10; i++)
            AddBeat($"A lantern number {new string('x', i + 1)} waited.");

        var lines = await LintLines();

        Assert.That(lines, Has.None.Contains("pet words"));
    }

    [Test]
    public async Task Dialogue_FiveUnattributedQuotedParagraphs_IsFlagged()
    {
        AddBeat("\"Go.\"\n\"Where?\"\n\"North.\"\n\"Why north?\"\n\"Because it's there.\"");

        var lines = await LintLines();

        Assert.That(lines, Has.Some.Contains("5 consecutive dialogue paragraphs with no attribution"));
    }

    [Test]
    public async Task Dialogue_AttributionBreaksTheRun()
    {
        AddBeat("\"Go.\"\n\"Where?\"\n\"North,\" she said, pointing.\n\"Why north?\"\n\"Because it's there.\"");

        var lines = await LintLines();

        Assert.That(lines, Has.None.Contains("consecutive dialogue paragraphs"));
    }

    [Test]
    public async Task Airless_SixLongBeatsWithoutDialogue_IsFlagged()
    {
        for (int i = 0; i < 6; i++)
            AddBeat($"Narration only, part {new string('a', i + 1)}.", dialogueProportion: 0.0, wordCount: 200);
        AddBeat("\"Finally,\" she said.", dialogueProportion: 0.5, wordCount: 200);

        var lines = await LintLines();

        Assert.That(lines, Has.Some.Contains("6 consecutive beats with almost no dialogue"));
    }

    [Test]
    public async Task Airless_FiveBeatsIsUnderTheFloor()
    {
        for (int i = 0; i < 5; i++)
            AddBeat($"Narration only, part {new string('a', i + 1)}.", dialogueProportion: 0.0, wordCount: 200);
        AddBeat("\"Finally,\" she said.", dialogueProportion: 0.5, wordCount: 200);

        var lines = await LintLines();

        Assert.That(lines, Has.None.Contains("consecutive beats with almost no dialogue"));
    }

    [Test]
    public async Task FloatingHeads_LongBeatThatIsAlmostAllDialogue_IsFlagged()
    {
        AddBeat("\"Talk.\" \"Talk back.\"", dialogueProportion: 0.95, wordCount: 700);
        AddBeat("\"Short.\"", dialogueProportion: 0.95, wordCount: 300);

        var lines = await LintLines();

        Assert.That(lines, Has.Exactly(1).Matches<string>(l => l.Contains("floating heads")), "only the 600+ word beat qualifies");
    }

    [Test]
    public async Task DryRun_FilesNoFindings()
    {
        AddBeat("The lantern hissed on its hook. A second lantern burned lower. Then every lantern died at once.");

        await lint.LintAsync(CanonFixture.SharedBookSlug, dryRun: true);

        Assert.That(LintFindings(), Is.Empty);
    }

    [Test]
    public async Task Rerun_ReplacesFindingsInsteadOfDuplicatingThem()
    {
        AddBeat("The lantern hissed on its hook. A second lantern burned lower. Then every lantern died at once.");
        AddBeat("\"Go.\"\n\"Where?\"\n\"North.\"\n\"Why north?\"\n\"Because it's there.\"");

        var first = await lint.LintAsync(CanonFixture.SharedBookSlug, dryRun: false);
        var afterFirst = LintFindings().Count;
        await lint.LintAsync(CanonFixture.SharedBookSlug, dryRun: false);
        var afterSecond = LintFindings().Count;

        Assert.That(afterFirst, Is.GreaterThan(0));
        Assert.That(afterSecond, Is.EqualTo(afterFirst));
        Assert.That(first.EchoFindings, Is.EqualTo(1));
        Assert.That(first.DialogueFindings, Is.EqualTo(1));
    }
}
