using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;
using Prose.Core.Services.Audit;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix to <see cref="NounConsistencyService"/>'s deprecated-
/// name scan. "ANGEL" is a retired GLMZ acronym (Aerogel Null Globe Evacuated Lifter -> Eigenlift)
/// registered in DeprecatedEntityNames — but the scan's case-insensitive whole-word match also
/// caught the ordinary English word "Angel"/"angel" wherever it appeared, e.g. an in-world ad
/// slogan quoting "Voice of an Angel" that has nothing to do with the retired aerostatic tech.
/// Fixed by requiring exact-case matches for deprecated names that are themselves all-caps
/// (acronym-shaped) while leaving ordinary mixed-case name renames case-insensitive as before.
/// </summary>
[TestFixture]
public class NounConsistencyServiceCaseSensitivityTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private NounConsistencyService svc = null!;
    private Guid universeId;
    private int beatNumber;

    private sealed class NeverCalledLlmService : ILlmService
    {
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user,
            double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default) =>
            throw new InvalidOperationException("NounConsistencyService's scan never calls the LLM.");
    }

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-nounconsistency-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nouns");
        var auditRunner = new AuditRunner(new NeverCalledLlmService(), new FindingsService(dbFactory, paths));
        svc = new NounConsistencyService(dbFactory, auditRunner);
        universeId = Guid.CreateVersion7();
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<Guid> SeedNodeWithBeatAsync(string beatText)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Universes.Add(new Universe { Id = universeId, Slug = "u-" + Guid.NewGuid().ToString("N")[..8], Name = "U" });
        var id = Guid.CreateVersion7();
        var node = NodeFactory.Create("book");
        node.Id = id;
        node.Slug = "s-" + Guid.NewGuid().ToString("N")[..8];
        node.Title = "T";
        node.Status = "draft";
        node.SortKey = 100;
        node.UniverseId = universeId;
        db.Nodes.Add(node);
        var beat = new Beat { Id = Guid.CreateVersion7(), Number = ++beatNumber, Text = beatText };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = id, BeatId = beat.Id, SortKey = 1 });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task AddAcronymRuleAsync() =>
        await svc.AddRuleAsync(universeId, "ANGEL", "Eigenlift", "retired acronym");

    [Test]
    public async Task AllCapsAcronymUsage_IsFlagged()
    {
        await AddAcronymRuleAsync();
        var nodeId = await SeedNodeWithBeatAsync("The old ANGEL rig hummed under the floor plates.");

        var report = await svc.ValidateAsync(nodeId);

        Assert.That(report.IsClean, Is.False, "genuine all-caps acronym usage must still be flagged");
    }

    [Test]
    public async Task OrdinaryWordUsage_MixedCase_IsNotFlagged()
    {
        // The exact real false positive: a title-case ad slogan quoting "Angel" as an ordinary
        // English word, unrelated to the retired ANGEL acronym.
        await AddAcronymRuleAsync();
        var nodeId = await SeedNodeWithBeatAsync("*Voice of an Angel; Looks of an Adonis and Sex Appeal*");

        var report = await svc.ValidateAsync(nodeId);

        Assert.That(report.IsClean, Is.True,
            "the ordinary English word 'Angel' must not be flagged against an ALL-CAPS acronym rule");
    }

    [Test]
    public async Task OrdinaryWordUsage_Lowercase_IsNotFlagged()
    {
        await AddAcronymRuleAsync();
        var nodeId = await SeedNodeWithBeatAsync("She fought like an angel with nothing left to lose.");

        var report = await svc.ValidateAsync(nodeId);

        Assert.That(report.IsClean, Is.True);
    }

    [Test]
    public async Task OrdinaryMixedCaseNameRename_StaysCaseInsensitive()
    {
        // A normal (non-acronym) rename must still catch lowercase/mixed-case occurrences —
        // the exact-case requirement only applies to ALL-CAPS acronym-shaped deprecated names.
        await svc.AddRuleAsync(universeId, "Kyle Marrow", "Kyle Voss", "character renamed");
        var nodeId = await SeedNodeWithBeatAsync("kyle marrow walked in, same as always.");

        var report = await svc.ValidateAsync(nodeId);

        Assert.That(report.IsClean, Is.False,
            "ordinary mixed-case name renames must remain case-insensitive");
    }
}
