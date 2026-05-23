using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// CLI round-trip integration coverage. Without running the real LLM
/// (which costs money + needs an API key), we stand up the same shape of
/// rows the EpisodeGeneratorService writes — Episode + EpisodeBeats — then
/// run the migration the CLI runs, and verify the strand opens correctly
/// in the workbench (the same code path the UI uses).
///
/// This is the contract test for the user's specific ask:
/// "writing strands in the CLI gets filed away correctly in this new schema
/// so it can get opened in the application and have all the strand/beats
/// aligned and ready to be modified, recorded, etc."
/// </summary>
[TestFixture]
public class StrandCliRoundTripTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<StreetSamuraiDbContext> dbFactory = null!;
    private StrandMigrationService migration = null!;
    private StrandWorkbenchService workbench = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-cli-roundtrip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "cli-roundtrip");
        migration = new StrandMigrationService(dbFactory, NullLogger<StrandMigrationService>.Instance);
        workbench = new StrandWorkbenchService(dbFactory, null!, paths, NullLogger<StrandWorkbenchService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<Guid> SimulateGeneratorAsync(string seed, string title, int beats, string? voiceId = null, string? chapterId = null)
    {
        var id = Guid.CreateVersion7();
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Episodes.Add(new Episode
        {
            Id = id,
            Seed = seed,
            Title = title,
            // Slug intentionally left empty — matches the "force-recompute on rename" path
            // when --title overrides; migration must mint a slug from Title in that case.
            Slug = "",
            Status = "ready_for_audio",
            VoiceId = voiceId,
            StartedAt = DateTime.UtcNow,
            GenerationCompletedAt = DateTime.UtcNow,
            ChapterId = chapterId,
        });
        for (int i = 0; i < beats; i++)
        {
            db.EpisodeBeats.Add(new EpisodeBeat
            {
                EpisodeId = id,
                Index = i,
                SortKey = i * 100.0,
                Text = $"Paragraph {i + 1} of {title}.",
                SceneType = "scene",
                Act = 1,
            });
        }
        await db.SaveChangesAsync();
        return id;
    }

    [Test]
    public async Task CliWriteStrand_RoundTrip_ProducesEditableStrand()
    {
        // Generator writes the legacy rows.
        var episodeId = await SimulateGeneratorAsync(
            seed:  "Kyle finds a folded note in his coat pocket.",
            title: "The Folded Note",
            beats: 5);

        // CLI runs the migration.
        var report = await migration.MigrateAllAsync();
        Assert.That(report.EpisodesAdded,        Is.EqualTo(1));
        Assert.That(report.StandaloneBeatsAdded, Is.EqualTo(5));

        // App opens the strand the way Strand.razor would.
        await using var db = await dbFactory.CreateDbContextAsync();
        var strand = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Id == episodeId);
        Assert.That(strand, Is.Not.Null);
        Assert.That(strand!.Kind,   Is.EqualTo("episode"));
        Assert.That(strand.Title,   Is.EqualTo("The Folded Note"));
        Assert.That(strand.Status,  Is.EqualTo("ready_for_audio"), "User can hit Record immediately");
        Assert.That(strand.Slug,    Is.Not.Empty.And.Contains("folded-note"), "Slug must reflect title for /strand/{slug} URL");

        // Beats land in correct order and ready to modify.
        var ordered = await workbench.GetOrderedBeatsAsync(strand.Id);
        Assert.That(ordered, Has.Count.EqualTo(5));
        var texts = ordered.Select(o => o.Beat.Text).ToArray();
        Assert.That(texts[0], Does.StartWith("Paragraph 1"));
        Assert.That(texts[4], Does.StartWith("Paragraph 5"));
        foreach (var ob in ordered)
        {
            Assert.That(ob.Beat.AudioPath, Is.Null, "No audio yet — Record button drives the next step");
            Assert.That(ob.Beat.TextHash,  Is.Not.Empty, "Hash set so the desync sweep is honest on later edits");
            Assert.That(ob.Beat.SceneType, Is.EqualTo("scene"));
        }
    }

    [Test]
    public async Task CliWriteStrand_TitleOverride_ProducesCoherentSlug()
    {
        // Generator picks a title; user overrides via --title before migration runs.
        var episodeId = await SimulateGeneratorAsync(
            seed:  "anything",
            title: "Auto Generated Title",
            beats: 3);
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var ep = await db.Episodes.FirstAsync(e => e.Id == episodeId);
            ep.Title = "User Override Title";
            ep.Slug  = ""; // force recompute — same shape the CLI applies
            await db.SaveChangesAsync();
        }

        await migration.MigrateAllAsync();

        await using var probe = await dbFactory.CreateDbContextAsync();
        var strand = await probe.Strands.AsNoTracking().FirstAsync(s => s.Id == episodeId);
        Assert.That(strand.Title, Is.EqualTo("User Override Title"));
        // Slug must derive from the OVERRIDDEN title so /strand/{slug} resolves.
        Assert.That(strand.Slug, Does.Contain("user-override-title"));
    }

    [Test]
    public async Task CliWriteStrand_BeatsCanBeEdited_ImmediatelyAfterImport()
    {
        var episodeId = await SimulateGeneratorAsync(
            seed: "anything", title: "Editable", beats: 3);
        await migration.MigrateAllAsync();

        var ordered = await workbench.GetOrderedBeatsAsync(episodeId);
        var first = ordered[0].Beat;

        // Same call the unified UI makes from SaveBeatEdit.
        await workbench.UpdateBeatTextAsync(first.Id, "Rewrote the opening paragraph.");

        await using var probe = await dbFactory.CreateDbContextAsync();
        var refreshed = await probe.Beats.AsNoTracking().FirstAsync(b => b.Id == first.Id);
        Assert.That(refreshed.Text,         Is.EqualTo("Rewrote the opening paragraph."));
        Assert.That(refreshed.Stale,        Is.True);
        Assert.That(refreshed.WasCorrected, Is.True);
    }

    [Test]
    public async Task CliWriteStrand_BeatsCanBeInsertedSplitJoinedDeleted_AfterImport()
    {
        var episodeId = await SimulateGeneratorAsync(
            seed: "anything", title: "Mutable", beats: 3);
        await migration.MigrateAllAsync();

        // Insert a new beat at the top.
        var inserted = await workbench.InsertBeatAsync(episodeId, afterBeatId: null, "Brand new opener.");
        var afterInsert = await workbench.GetOrderedBeatsAsync(episodeId);
        Assert.That(afterInsert, Has.Count.EqualTo(4));
        Assert.That(afterInsert[0].Beat.Id, Is.EqualTo(inserted.Id));

        // Split the second beat (the original first paragraph) at its midpoint.
        var splittable = afterInsert[1].Beat.Id;
        await workbench.UpdateBeatTextAsync(splittable,
            "First half is here so we have enough to split. Second half kicks in after the period.");
        var split = await workbench.SplitBeatAsync(episodeId, splittable);
        var afterSplit = await workbench.GetOrderedBeatsAsync(episodeId);
        Assert.That(afterSplit, Has.Count.EqualTo(5));

        // Join the split's second half back into its predecessor.
        await workbench.JoinBeatWithPreviousAsync(episodeId, split.Id);
        var afterJoin = await workbench.GetOrderedBeatsAsync(episodeId);
        Assert.That(afterJoin, Has.Count.EqualTo(4));

        // Delete the top beat we inserted.
        await workbench.DeleteBeatAsync(episodeId, inserted.Id);
        var afterDelete = await workbench.GetOrderedBeatsAsync(episodeId);
        Assert.That(afterDelete, Has.Count.EqualTo(3), "Back to the original count after all edits");
    }

    [Test]
    public async Task CliWriteStrand_MetadataCanBeSetAfterImport_AndDrivesPromptBuilder()
    {
        var episodeId = await SimulateGeneratorAsync(
            seed: "anything", title: "Metadata", beats: 1);
        await migration.MigrateAllAsync();

        var ordered = await workbench.GetOrderedBeatsAsync(episodeId);
        var beat = ordered[0].Beat;

        await workbench.UpdateBeatMetadataAsync(beat.Id, new StrandWorkbenchService.BeatMetadataUpdate(
            BeatTitle:      "The whisper",
            Synopsis:       "Quiet open — set the mood",
            EmotionalTone:  "quiet",
            PaceHint:       "languorous",
            FacetTag:       null,
            StructureRole:  "opening",
            Act:            1,
            SceneType:      "scene",
            IsChapterStart: false,
            Kind:           "prose"));

        await using var probe = await dbFactory.CreateDbContextAsync();
        var refreshed = await probe.Beats.AsNoTracking().FirstAsync(b => b.Id == beat.Id);
        Assert.That(refreshed.EmotionalTone, Is.EqualTo("quiet"));
        Assert.That(refreshed.PaceHint,      Is.EqualTo("languorous"));
        // The prompt builder now picks the right v3 tag from this metadata.
        var prompt = BeatPromptBuilder.Build(refreshed, "eleven_v3", tagsEnabled: true,
            baselineStability: 0.5, baselineSimilarityBoost: 0.75, baselineStyle: 0.0);
        Assert.That(prompt.Text, Does.StartWith("[whispering]"));
        Assert.That(prompt.Stability, Is.GreaterThan(0.5), "Quiet tone biases stability up for controlled delivery");
    }
}
