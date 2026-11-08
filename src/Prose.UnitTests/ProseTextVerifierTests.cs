using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// PRS-T922 / RFC 0018: game writing as a first-class mode. Locks in the optional <c>context</c> block of
/// <c>prose-text/1</c> (backward compatible, strict about its own fields, part of a version) and the game-mode
/// verifiers (<see cref="ProseTextVerifier"/>): canon, continuity on a partial order, obligations, branches and bark
/// pools; findings filed per version at <c>text:{universe}/{id}#v{n}</c>; a dismissed finding stays dismissed; and
/// a finding never refuses a draft or an approval (RFC 0009).
/// </summary>
[TestFixture]
public class ProseTextVerifierTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private FindingsService findings = null!;
    private ProseTextVerifier verifier = null!;
    private ProseTextService svc = null!;

    [SetUp]
    public async Task SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-prose-text-verify-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "prose-text-verify");
        findings = new FindingsService(dbFactory, paths);
        verifier = new ProseTextVerifier(dbFactory, findings);
        svc = new ProseTextService(dbFactory, new OutboxService(dbFactory), verifier);

        var seed = await new UniverseInterchangeService(dbFactory).ImportAsync(JsonSerializer.Serialize(new
        {
            universe = new { id = "gridgame", name = "GridGame" },
            entities = new object[]
            {
                new { id = "class-ronin", type = "class", name = "Ronin", summary = "A masterless sword." },
                new { id = "class-lich", type = "class", name = "Lich", summary = "A dead mage." },
                new { id = "faction-ember", type = "faction", name = "Ember", summary = "The red rivers." },
                new { id = "place-arkgrave", type = "place", name = "Arkgrave", summary = "A grave city." },
            },
        }));
        Assert.That(seed.Success, Is.True, string.Join("; ", seed.Errors));
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }

    private static string Text(string id, string kind, string key, object payload, string[]? about = null,
        string[]? speakers = null, object? context = null) =>
        JsonSerializer.Serialize(new
        {
            id, kind, key, about = about ?? Array.Empty<string>(), speakers = speakers ?? Array.Empty<string>(),
            context, payload,
        }, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

    private async Task<ProseTextService.DraftResult> Draft(string json)
    {
        var r = await svc.DraftAsync("gridgame", json, "agent");
        Assert.That(r.Success, Is.True, r.Id + ": " + string.Join("; ", r.Errors));
        return r;
    }

    private static string[] Codes(IEnumerable<ProseTextVerifier.TextFinding> found) =>
        found.Select(f => $"{f.Family}/{f.Code}").Distinct().OrderBy(c => c).ToArray();

    // ── context ────────────────────────────────────────────────────────────

    [Test]
    public async Task Context_is_optional_round_trips_and_is_part_of_the_version()
    {
        var plain = await Draft(Text("crawl-prologue", "crawl", "crawl:prologue", new { title = "Prologue", paragraphs = new[] { "Once." } }));
        Assert.That((await svc.GetAsync("gridgame", "crawl-prologue"))!.Context, Is.Null, "no context stays no context");

        var placed = await Draft(Text("crawl-prologue", "crawl", "crawl:prologue", new { title = "Prologue", paragraphs = new[] { "Once." } },
            context: new { place = "place-arkgrave", chapter = "act-1", order = 10, trigger = "stage:1/start" }));
        Assert.That(placed.Version, Is.EqualTo(plain.Version + 1), "a context change alone is a new version");

        var doc = (await svc.GetAsync("gridgame", "crawl-prologue", placed.Version))!;
        Assert.That(doc.Context!.Place, Is.EqualTo("place-arkgrave"));
        Assert.That(doc.Context.Order, Is.EqualTo(10));
        Assert.That(doc.Context.Chapter, Is.EqualTo("act-1"));
        Assert.That(doc.Hash, Is.EqualTo((await svc.GetAsync("gridgame", "crawl-prologue", plain.Version))!.Hash),
            "the hash covers the payload only");

        var again = await Draft(Text("crawl-prologue", "crawl", "crawl:prologue", new { title = "Prologue", paragraphs = new[] { "Once." } },
            context: new { place = "place-arkgrave", chapter = "act-1", order = 10, trigger = "stage:1/start" }));
        Assert.That(again.Unchanged, Is.True, "the same context and payload change nothing");
    }

    [Test]
    public async Task A_malformed_context_or_an_unknown_place_is_refused()
    {
        var bad = await svc.DraftAsync("gridgame", Text("crawl-x", "crawl", "crawl:x", new { title = "X", paragraphs = new[] { "Y" } },
            context: new { place = "Arkgrave", order = "ten", when = "now" }));
        Assert.That(bad.Success, Is.False);
        Assert.That(string.Join("\n", bad.Errors), Does.Contain("context.place").And.Contain("context.order").And.Contain("context.when"));

        var unknown = await svc.DraftAsync("gridgame", Text("crawl-x", "crawl", "crawl:x", new { title = "X", paragraphs = new[] { "Y" } },
            context: new { place = "place-nowhere" }));
        Assert.That(string.Join("\n", unknown.Errors), Does.Contain("place-nowhere"));
    }

    // ── continuity ─────────────────────────────────────────────────────────

    [Test]
    public async Task Naming_an_entity_before_its_introduction_is_found_and_an_after_link_clears_it()
    {
        await Draft(Text("crawl-act-3", "crawl", "crawl:act-3", new { title = "The Ember", paragraphs = new[] { "The rivers burn." } },
            about: ["faction-ember"], context: new { order = 30 }));

        var early = await Draft(Text("conversation-early", "conversation", "dialogue:early",
            new { start = "a", nodes = new object[] { new { id = "a", speaker = "$hero", line = "Beware the Ember." } } },
            speakers: ["$hero"], context: new { order = 10 }));
        var intro = early.Findings.Single(f => f.Code == "before-introduction");
        Assert.That(intro.Summary, Does.Contain("faction-ember").And.Contain("crawl-act-3"));
        Assert.That(intro.Severity, Is.EqualTo(FindingSeverity.Medium));

        var late = await Draft(Text("conversation-late", "conversation", "dialogue:late",
            new { start = "a", nodes = new object[] { new { id = "a", speaker = "$hero", line = "Beware the Ember." } } },
            speakers: ["$hero"], context: new { order = 5, after = new[] { "crawl-act-3" } }));
        Assert.That(late.Findings.Where(f => f.Code == "before-introduction"), Is.Empty,
            "a text that comes after the introduction may name it");

        var unplaced = await Draft(Text("conversation-anytime", "conversation", "dialogue:anytime",
            new { start = "a", nodes = new object[] { new { id = "a", speaker = "$hero", line = "Beware the Ember." } } },
            speakers: ["$hero"]));
        Assert.That(unplaced.Findings.Where(f => f.Family == "CONTINUITY"), Is.Empty, "an unplaced text is not judged");
    }

    [Test]
    public async Task Bad_after_links_and_a_place_that_is_not_a_place_are_found()
    {
        await Draft(Text("crawl-act-3", "crawl", "crawl:act-3", new { title = "Act 3", paragraphs = new[] { "Later." } }, context: new { order = 30 }));
        var r = await Draft(Text("crawl-act-1", "crawl", "crawl:act-1", new { title = "Act 1", paragraphs = new[] { "Sooner." } },
            context: new { order = 10, place = "class-ronin", after = new[] { "crawl-act-3", "crawl-missing" } }));
        Assert.That(Codes(r.Findings), Is.SupersetOf(new[]
            { "CONTINUITY/after-order", "CONTINUITY/place-type", "CONTINUITY/unknown-after" }));
    }

    // ── canon ──────────────────────────────────────────────────────────────

    [Test]
    public async Task A_live_text_that_canon_retired_under_it_is_found_on_reverification()
    {
        await Draft(Text("description-class-lich", "description", "class:lich", new { @short = "A dead mage." }, about: ["class-lich"]));
        var approved = await svc.ApproveAsync("gridgame", ["description-class-lich"], "ryan");
        Assert.That(approved.Success, Is.True);
        Assert.That(approved.Findings.Where(f => f.Family == "CANON"), Is.Empty);

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var lich = await db.Entities.IgnoreQueryFilters().SingleAsync(e => e.Slug == "class-lich");
            lich.Status = "archived";
            await db.SaveChangesAsync();
        }

        var r = await verifier.VerifyAsync("gridgame", ["description-class-lich"]);
        var retired = r.Findings.Single(f => f.Code == "retired-entity");
        Assert.That(retired.Severity, Is.EqualTo(FindingSeverity.High));
        Assert.That((await svc.GetAsync("gridgame", "description-class-lich"))!.Status, Is.EqualTo("approved"),
            "a finding never demotes an approved text (RFC 0009)");
    }

    [Test]
    public async Task Two_glossary_texts_defining_one_term_conflict()
    {
        await Draft(Text("glossary-burning", "glossary", "glossary:burning", new { term = "Burning", definition = "On fire." }));
        var r = await Draft(Text("glossary-burn", "glossary", "glossary:burn", new { term = "Burn", aliases = new[] { "burning" }, definition = "Hot." }));
        Assert.That(r.Findings.Single(f => f.Code == "glossary-conflict").Summary, Does.Contain("glossary-burning"));
    }

    // ── obligations ────────────────────────────────────────────────────────

    [Test]
    public async Task A_quest_with_no_payoff_and_an_undescribed_referent_are_obligations()
    {
        var r = await Draft(Text("quest-ember", "quest", "quest:ember", new { title = "Douse", brief = "Put out the rivers." },
            about: ["faction-ember"]));
        Assert.That(Codes(r.Findings), Is.SupersetOf(new[] { "OBLIGATION/quest-unresolved", "OBLIGATION/undescribed" }));
        Assert.That(r.Findings.Where(f => f.Family == "OBLIGATION").All(f => f.Category == FindingCategory.NarrativeObligation), Is.True);

        await Draft(Text("description-faction-ember", "description", "faction:ember", new { @short = "The red rivers." }, about: ["faction-ember"]));
        var again = await verifier.VerifyAsync("gridgame", ["quest-ember"]);
        Assert.That(again.Findings.Where(f => f.Code == "undescribed"), Is.Empty, "a description pays the referent off");
    }

    // ── branches and barks ─────────────────────────────────────────────────

    [Test]
    public async Task Unreachable_nodes_a_loop_with_no_way_out_and_duplicate_choices_are_found()
    {
        var r = await Draft(Text("conversation-loop", "conversation", "dialogue:loop", new
        {
            start = "a",
            nodes = new object[]
            {
                new { id = "a", speaker = "$hero", line = "Again?", choices = new object[]
                    { new { text = "Yes", next = "b" }, new { text = "yes!", next = "b" } } },
                new { id = "b", speaker = "$hero", line = "Again.", next = "a" },
                new { id = "orphan", speaker = "$hero", line = "Nobody hears this." },
            },
        }, speakers: ["$hero"]));
        Assert.That(Codes(r.Findings), Is.SupersetOf(new[] { "BRANCH/duplicate-choice", "BRANCH/no-ending", "BRANCH/unreachable" }));
        Assert.That(r.Findings.Single(f => f.Code == "no-ending").Severity, Is.EqualTo(FindingSeverity.High));

        var exits = await Draft(Text("conversation-exit", "conversation", "dialogue:exit", new
        {
            start = "a",
            nodes = new object[] { new { id = "a", speaker = "$hero", line = "Done?", next = "$outcome" } },
        }, speakers: ["$hero"]));
        Assert.That(exits.Findings.Where(f => f.Family == "BRANCH"), Is.Empty, "a $token hands back to the game");
    }

    [Test]
    public async Task Bark_pools_report_duplicates_repeats_across_pools_and_long_lines()
    {
        await Draft(Text("bark-ronin-hit", "bark", "bark:class-ronin/hit", new { speaker = "class-ronin", context = "hit", lines = new[] { "Not yet." } },
            speakers: ["class-ronin"]));
        var r = await Draft(Text("bark-ronin-low", "bark", "bark:class-ronin/low-health", new
        {
            speaker = "class-ronin", context = "low-health",
            lines = new[] { "Not yet!", "Hold.", "Hold", new string('a', ProseTextVerifier.MaxBarkChars + 1) },
        }, speakers: ["class-ronin"]));
        Assert.That(Codes(r.Findings), Is.SupersetOf(new[] { "BARK/duplicate", "BARK/long", "BARK/repeated" }));
    }

    // ── the findings contract ──────────────────────────────────────────────

    [Test]
    public async Task Findings_are_filed_per_version_a_dismissal_sticks_and_a_fix_clears_them()
    {
        var v1 = await Draft(Text("quest-x", "quest", "quest:x", new { title = "X", brief = "Go." }));
        var filed = verifier.ListFindingRows("gridgame", "quest-x");
        var row = filed.Single(f => f.Summary.Contains("[quest-unresolved]"));
        Assert.That(row.Version, Is.EqualTo(v1.Version));
        Assert.That(findings.List(filePathPrefix: ProseTextVerifier.PathFor("gridgame", "quest-x", v1.Version)).Single().SuggestedFix,
            Is.Null, "a finding describes; it never instructs");

        findings.SetStatus(row.Id, FindingStatus.Dismissed);
        await verifier.VerifyAsync("gridgame", ["quest-x"]);
        Assert.That(verifier.ListFindingRows("gridgame", "quest-x").Single(f => f.Id == row.Id).Status, Is.EqualTo("Dismissed"),
            "the author's dismissal survives re-verification");

        var v2 = await Draft(Text("quest-x", "quest", "quest:x", new { title = "X", brief = "Go.", complete = "Gone." }));
        Assert.That(v2.Findings.Where(f => f.Code == "quest-unresolved"), Is.Empty);
        Assert.That(verifier.ListFindingRows("gridgame", "quest-x", FindingStatus.New)
            .Where(f => f.Version == v2.Version && f.Summary.Contains("[quest-unresolved]")), Is.Empty);
    }

    [Test]
    public async Task Findings_never_refuse_a_draft_or_an_approval()
    {
        var r = await Draft(Text("conversation-stuck", "conversation", "dialogue:stuck", new
        {
            start = "a",
            nodes = new object[] { new { id = "a", speaker = "$hero", line = "Round.", next = "a" } },
        }, speakers: ["$hero"]));
        Assert.That(r.Findings.Any(f => f.Severity == FindingSeverity.High), Is.True);

        var bulk = await svc.BulkApproveAsync("gridgame", "conversation", "ryan");
        Assert.That(bulk.Approved.Select(a => a.Id), Is.EqualTo(new[] { "conversation-stuck" }));
        Assert.That(bulk.Findings.Any(f => f.Code == "no-ending"), Is.True, "approval reports what was found, and approves anyway");
    }

    [Test]
    public void Text_finding_paths_parse_back()
    {
        Assert.That(ProseTextVerifier.ParsePath("text:gridgame/crawl-prologue#v3"), Is.EqualTo(("gridgame", "crawl-prologue", (int?)3)));
        Assert.That(ProseTextVerifier.ParsePath("text:gridgame/crawl-prologue"), Is.EqualTo(("gridgame", "crawl-prologue", (int?)null)));
        Assert.That(ProseTextVerifier.ParsePath("node:bcoda"), Is.Null);
    }
}
