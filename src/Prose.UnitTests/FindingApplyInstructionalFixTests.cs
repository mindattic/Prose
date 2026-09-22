using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// <b>The landmine.</b> <see cref="FindingApplyService"/> applies a finding by literal
/// substitution — <c>beat.Text.Replace(Snippet, SuggestedFix)</c>. That contract only holds if
/// <c>SuggestedFix</c> is <em>prose to put in the manuscript</em>. If it is editorial instruction
/// <em>about</em> the manuscript, applying the finding pastes the instruction into the novel.
///
/// <para>Found by audit 2026-09-22, before it fired.
/// <c>GripePassService.RunFullOrderReadAsync</c> filed ENGAGEMENT findings with a quote-grounded
/// <c>Snippet</c> (real prose, so the <c>Contains</c> guard passes by construction), a
/// <c>FilePath</c> of <c>node:{slug}#fullorderread/beat:{guid}</c> (contains <c>beat:</c>, so
/// <c>ExtractBeatId</c> routes it to the beat path), and a static instructional
/// <c>SuggestedFix</c>: "Fix structurally, not stylistically: give this beat more page-time…".
/// Every precondition was satisfied. One <c>prose --findings apply &lt;id&gt;</c> would have
/// replaced a paragraph of the book with that sentence and committed it as
/// <see cref="BeatWriteReason.FindingApply"/>.</para>
///
/// <para>Fixed in two places on purpose: the producer no longer sets <c>suggestedFix</c> (the
/// guidance moved to the <c>Summary</c>, which is the field a human reads), and
/// <see cref="FindingApplyService.IsInstructionalFix"/> is the backstop that catches the same
/// shape arriving from any future instrument. These tests pin both halves — the guard, and the
/// end-to-end guarantee that the beat text is byte-identical afterwards.</para>
/// </summary>
[TestFixture]
public class FindingApplyInstructionalFixTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private FindingsService findings = null!;
    private FindingApplyService apply = null!;

    private Guid beatId;
    private const string BeatText =
        "The rain had stopped an hour ago and the street still would not dry. " +
        "Kyle counted the windows across from him and made it eleven, the same as always.";

    [SetUp]
    public async Task SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-apply-guard-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");

        var bookId = Guid.CreateVersion7();
        var chapterId = Guid.CreateVersion7();
        beatId = Guid.CreateVersion7();

        await using (var db = dbFactory.CreateDbContext())
        {
            db.Nodes.Add(new BookNode
            {
                Id = bookId, Slug = "guard-book", NodeCode = "GRD", Title = "Guard Book",
                Kind = "book", UniverseId = Universe.GlmzId,
            });
            db.Nodes.Add(new ChapterNode
            {
                Id = chapterId, Slug = "guard-book-ch1", Title = "Chapter 1 — Teeth",
                Kind = "chapter", ParentNodeId = bookId, UniverseId = Universe.GlmzId,
            });
            db.Beats.Add(new Beat { Id = beatId, Number = 1, Text = BeatText, TextHash = Beat.ComputeHash(BeatText) });
            db.BeatNodes.Add(new BeatNode { NodeId = chapterId, BeatId = beatId, SortKey = 1 });
            await db.SaveChangesAsync();
        }

        findings = new FindingsService(dbFactory, paths);
        var workbench = new NodeWorkbenchService(
            dbFactory, null!, paths, null!, NullLogger<NodeWorkbenchService>.Instance,
            null!, null!, null!, null!, null!);
        apply = new FindingApplyService(
            findings, dbFactory, paths, workbench, NullLogger<FindingApplyService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }

    private async Task<string> BeatTextNowAsync()
    {
        await using var db = dbFactory.CreateDbContext();
        return (await db.Beats.AsNoTracking().FirstAsync(b => b.Id == beatId)).Text;
    }

    // ── the end-to-end guarantee ──────────────────────────────────────────────

    [Test]
    public async Task EngagementShapedFinding_IsRefused_AndLeavesTheBeatByteIdentical()
    {
        // The exact shape RunFullOrderReadAsync used to file: grounded quote + instructional fix,
        // under a FilePath that routes to the beat.
        const string quote = "Kyle counted the windows across from him and made it eleven";
        const string instructionalFix =
            "Fix structurally, not stylistically: give this beat more page-time to accrue pressure " +
            "before it lands, or cut the correct-but-inert scene immediately in front of it. Do NOT " +
            "rewrite this beat's prose to sound more intense at the same length — that is the wrong " +
            "fix (docs/LOGIC.md §10, weight-by-length not weight-by-adjective).";

        var id = findings.Upsert(
            $"node:guard-book#fullorderread/beat:{beatId:N}", null, FindingCategory.ReaderGripe,
            FindingSeverity.High, "ENGAGEMENT beat #1 (3 voter(s), never recovered): lost me here",
            snippet: quote, suggestedFix: instructionalFix);

        var result = await apply.ApplyAsync(id);

        Assert.That(result.Outcome, Is.EqualTo(ApplyOutcome.FixIsNotProse));
        Assert.That(await BeatTextNowAsync(), Is.EqualTo(BeatText),
            "the manuscript must be byte-identical after a refused apply");
    }

    [Test]
    public async Task RefusedApply_LeavesTheFindingOpenSoAHumanCanStillSeeIt()
    {
        var id = findings.Upsert(
            $"node:guard-book#fullorderread/beat:{beatId:N}", null, FindingCategory.ReaderGripe,
            FindingSeverity.High, "ENGAGEMENT beat #1", snippet: "The rain had stopped",
            suggestedFix: "Fix structurally, not stylistically: give this beat more page-time.");

        await apply.ApplyAsync(id);

        Assert.That(findings.Get(id)!.Status, Is.EqualTo(FindingStatus.New),
            "a refusal is not a resolution — the finding stays open for a human to act on");
    }

    [Test]
    public async Task RealProseFix_StillApplies_SoTheGuardIsNotBlockingEverything()
    {
        // The positive control. Without this, a guard that refused every finding would pass the
        // two tests above and silently break the one sanctioned apply path (RFC 0009).
        var id = findings.Upsert(
            $"node:guard-book/beat:{beatId:N}", null, FindingCategory.Contradiction,
            FindingSeverity.Medium, "FACT-LEDGER [dents]: count disagrees with canon",
            snippet: "made it eleven", suggestedFix: "made it twelve");

        var result = await apply.ApplyAsync(id);

        Assert.That(result.Outcome, Is.EqualTo(ApplyOutcome.Applied));
        Assert.That(await BeatTextNowAsync(), Does.Contain("made it twelve"));
    }

    // ── the guard itself, as a pure function ──────────────────────────────────

    [Test]
    public void InstructionalFix_IsRecognised([Values(
        "Fix structurally, not stylistically: give this beat more page-time.",
        "Do NOT rewrite this beat's prose to sound more intense at the same length.",
        "See docs/LOGIC.md for the correct approach.",
        "Per §10, weight-by-length not weight-by-adjective.",
        "RFC 0013 says an obligation is closed only by a quote from the paying beat.",
        "Consider cutting the scene in front of this one.")] string fix)
        => Assert.That(FindingApplyService.IsInstructionalFix(fix), Is.True);

    [Test]
    public void OrdinaryReplacementProse_IsNotRecognisedAsInstruction([Values(
        "made it twelve",
        "The rain had stopped two hours ago and the street still would not dry.",
        "He said nothing. The elevator opened on the second floor.",
        "Mrs. Chen had been coming to the shop for six years without saying a word.")] string fix)
        => Assert.That(FindingApplyService.IsInstructionalFix(fix), Is.False);

    [Test]
    public void EmptyFix_IsNotTreatedAsInstruction()
        // ApplyAsync already rejects empty/whitespace with NoSuggestedFix before reaching the
        // guard; the guard must not claim that case as its own or the outcome would be wrong.
        => Assert.That(FindingApplyService.IsInstructionalFix("   "), Is.False);
}
