using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// PRS-T926. Locks in: (1) the name check <c>generate_scene</c> runs on its draft — names the universe's canon does
/// not have are flagged ("Kepler Station", "Kess"), canon names, their words, aliases, titles and the caller's own
/// words are not; (2) request words resolve to entities the way a game asks for them (id, name, article-less name,
/// the shortest name containing the word); (3) a conversation node the game re-enters after <c>$outcome</c> is
/// reachable once the universe declares it, and stays unreachable when it does not; (4) the findings status filter
/// accepts <c>open</c> (it was a 400 on the HTTP surface).
/// Written to run under the suite; per the repo's golden rule they are built, not run, by the agent that wrote them.
/// </summary>
[TestFixture]
public class SceneCanonGroundingTests
{
    // ── (1) the draft's names ──────────────────────────────────────────────

    private static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        "Earth Orbital", "Earth", "Sol Federation", "SFS Constable", "Moon", "The Hierarch", "Hierarch", "the Leech",
        "The Choir's Cathedral", "Choir's Cathedral", "Halcyon", "MV Lucky Strike",
    };

    [Test]
    public void A_known_name_without_its_prefix_is_known()
    {
        var unknown = SceneCanonGroundingService.FindUnknownNames("The hail came from the trader, and Lucky Strike answered.", Known, KnownWords);
        Assert.That(unknown, Has.None.EqualTo("Lucky Strike"));
    }

    private static readonly HashSet<string> KnownWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Earth", "Orbital", "Sol", "Federation", "SFS", "Constable", "Moon", "Hierarch", "Leech", "Choir's", "Cathedral", "Halcyon",
    };

    [Test]
    public void Invented_places_and_people_are_flagged()
    {
        var text = "The cutter rose from Earth Orbital toward Kepler Station, past the docks of Luna Station. " +
                   "\"Hold your course,\" Kess said, and Whitehall nodded.";
        var unknown = SceneCanonGroundingService.FindUnknownNames(text, Known, KnownWords);
        Assert.That(unknown, Is.SupersetOf(new[] { "Kepler Station", "Luna Station", "Kess", "Whitehall" }));
        Assert.That(unknown, Has.None.EqualTo("Earth Orbital"));
    }

    [Test]
    public void Canon_names_their_words_titles_and_placeholders_are_not_flagged()
    {
        var text = "Captain {pilot} of the SFS Constable hailed them. \"The Federation sees you,\" said Commander Hierarch's " +
                   "herald. Under the Moon, the Leech waited in the Choir's Cathedral above Halcyon.";
        var unknown = SceneCanonGroundingService.FindUnknownNames(text, Known, KnownWords);
        Assert.That(unknown, Is.Empty, string.Join(", ", unknown));
    }

    [Test]
    public void A_multi_word_name_is_judged_whole()
    {
        // "Luna" alone could pass as a word of a known name; "Earth Station" is not a canon name although both words are.
        var unknown = SceneCanonGroundingService.FindUnknownNames("They docked at Earth Station before dawn.", Known, KnownWords);
        Assert.That(unknown, Does.Contain("Earth Station"));
    }

    [Test]
    public void Headings_labels_and_line_breaks_make_no_names()
    {
        // Live 2026-11-08: "# BEAT: Inspection\n\nThe patrol…" was flagged as the name "Inspection The".
        var text = "# BEAT: Inspection\n\nThe patrol hails Earth Orbital.\nCalculation\nThe pilot waits.\n\nsceneEnd=false";
        var unknown = SceneCanonGroundingService.FindUnknownNames(text, Known, KnownWords);
        Assert.That(unknown, Has.None.Contains(" The"), string.Join(", ", unknown));
        Assert.That(unknown, Has.None.EqualTo("Inspection"));
    }

    // ── (2) resolving the request ──────────────────────────────────────────

    [Test]
    public void A_graph_name_slug_resolves_to_its_entity()
    {
        var idx = Index();
        // The graph keys "Earth Orbital" as earth-orbital even where the entity's own slug differs.
        var e = new SceneCanonGroundingService.IndexedEntity(Guid.NewGuid(), "Sol Federation", "faction-sol", "faction");
        idx.ById[e.Id] = e;
        idx.BySlug[e.Slug] = e;
        idx.AddName(e.Name, e);
        Assert.That(SceneCanonGroundingService.Resolve(idx, "sol-federation", null).Single().Slug, Is.EqualTo("faction-sol"));
        Assert.That(SceneCanonGroundingService.NameSlug("Barnard's Star"), Is.EqualTo("barnard-s-star"));
    }

    private static SceneCanonGroundingService.NameIndex Index()
    {
        var idx = new SceneCanonGroundingService.NameIndex();
        void Add(string slug, string name, string type)
        {
            var e = new SceneCanonGroundingService.IndexedEntity(Guid.NewGuid(), name, slug, type);
            idx.ById[e.Id] = e;
            idx.BySlug[slug] = e;
            idx.AddName(name, e);
        }
        Add("the-hierarch", "The Hierarch", "character");
        Add("class-hierarch", "The Hierarch", "class");
        Add("the-choir-s-cathedral", "The Choir's Cathedral", "place");
        Add("the-choir-s-cathedral-i", "The Choir's Cathedral I", "place");
        Add("earth-orbital", "Earth Orbital", "settlement");
        return idx;
    }

    [Test]
    public void Request_words_resolve_by_id_name_article_and_containing_word()
    {
        var idx = Index();
        Assert.That(SceneCanonGroundingService.Resolve(idx, "earth-orbital", null).Single().Name, Is.EqualTo("Earth Orbital"));
        Assert.That(SceneCanonGroundingService.Resolve(idx, "earth orbital", null).Single().Slug, Is.EqualTo("earth-orbital"));

        var hierarch = SceneCanonGroundingService.Resolve(idx, "Hierarch", e => e.Type == "character");
        Assert.That(hierarch.First().Type, Is.EqualTo("character"), "the article is optional and the preferred type leads");

        var cathedral = SceneCanonGroundingService.Resolve(idx, "the cathedral", e => e.Type != "character");
        Assert.That(cathedral.Select(c => c.Slug), Is.EqualTo(new[] { "the-choir-s-cathedral" }),
            "the shortest name containing the word, not its numbered battlefields");

        Assert.That(SceneCanonGroundingService.Resolve(idx, "Kepler Station", null), Is.Empty);
    }

    // ── (3) $outcome re-entry nodes ────────────────────────────────────────

    private static ProseTextService.TextVersion Talk() => new()
    {
        Version = 1,
        Payload = JsonSerializer.SerializeToElement(new
        {
            start = "open",
            nodes = new object[]
            {
                new { id = "open", speaker = "$them", line = "State your business.", choices = new object[]
                    { new { text = "Trade.", next = "$outcome", tags = new[] { "trade" } } } },
                new { id = "traded", speaker = "$them", line = "Pleasure." },
                new { id = "fought", speaker = "$them", line = "You'll regret that." },
            },
        }),
    };

    [Test]
    public void Outcome_nodes_are_unreachable_until_the_universe_declares_them()
    {
        var bare = new ProseTextVerifier.Sink("conversation-hail", 1);
        ProseTextVerifier.CheckBranches(Talk(), bare);
        Assert.That(bare.Found.Select(f => f.Code), Does.Contain("unreachable"));

        var declared = new ProseTextVerifier.Sink("conversation-hail", 1);
        ProseTextVerifier.CheckBranches(Talk(), declared, new HashSet<string>(StringComparer.Ordinal) { "traded", "fought", "talked" });
        Assert.That(declared.Found.Select(f => f.Code), Has.None.EqualTo("unreachable"));
        Assert.That(declared.Found.Select(f => f.Code), Has.None.EqualTo("no-ending"));
    }

    [Test]
    public void Declared_outcome_nodes_do_not_excuse_other_orphans()
    {
        var v = Talk();
        v.Payload = JsonSerializer.SerializeToElement(new
        {
            start = "open",
            nodes = new object[]
            {
                new { id = "open", speaker = "$them", line = "Go.", next = "$outcome" },
                new { id = "traded", speaker = "$them", line = "Done." },
                new { id = "orphan", speaker = "$them", line = "Nobody hears this." },
            },
        });
        var sink = new ProseTextVerifier.Sink("conversation-hail", 1);
        ProseTextVerifier.CheckBranches(v, sink, new HashSet<string>(StringComparer.Ordinal) { "traded" });
        var unreachable = sink.Found.Single(f => f.Code == "unreachable");
        Assert.That(unreachable.Summary, Does.Contain("'orphan'"));
        Assert.That(unreachable.Summary, Does.Not.Contain("'traded'"));
    }

    [Test]
    public void Outcome_node_setting_parses_a_json_list_and_ignores_junk()
    {
        Assert.That(ProseTextVerifier.ParseOutcomeNodes("[\"talked\",\" fought \",\"\"]"), Is.EquivalentTo(new[] { "talked", "fought" }));
        Assert.That(ProseTextVerifier.ParseOutcomeNodes("not json"), Is.Empty);
        Assert.That(ProseTextVerifier.ParseOutcomeNodes(null), Is.Empty);
    }

    // ── (4) findings status filter ─────────────────────────────────────────

    [Test]
    public void Findings_status_filter_accepts_open_and_all()
    {
        Assert.That(ProseTextVerifier.TryParseStatusFilter("open", out var open, out _), Is.True);
        Assert.That(open, Is.EquivalentTo(new[] { FindingStatus.New, FindingStatus.Triaged }));

        Assert.That(ProseTextVerifier.TryParseStatusFilter("Dismissed", out var one, out _), Is.True);
        Assert.That(one, Is.EquivalentTo(new[] { FindingStatus.Dismissed }));

        Assert.That(ProseTextVerifier.TryParseStatusFilter("all", out var all, out _), Is.True);
        Assert.That(all, Is.Null);
        Assert.That(ProseTextVerifier.TryParseStatusFilter(null, out var none, out _), Is.True);
        Assert.That(none, Is.Null);

        Assert.That(ProseTextVerifier.TryParseStatusFilter("closed", out _, out var error), Is.False);
        Assert.That(error, Does.Contain("open"));
        Assert.That(ProseTextVerifier.TryParseStatusFilter("7", out _, out _), Is.False, "a number outside the enum is not a status");
    }
}

/// <summary>PRS-T926: the outcome-node setting round-trips per universe and feeds the verifier.</summary>
[TestFixture]
public class ProseTextOutcomeNodesTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private ProseTextVerifier verifier = null!;
    private ProseTextService svc = null!;

    [SetUp]
    public async Task SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-outcome-nodes-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "outcome-nodes");
        verifier = new ProseTextVerifier(dbFactory, new FindingsService(dbFactory, paths));
        svc = new ProseTextService(dbFactory, new OutboxService(dbFactory), verifier);
        var seed = await new UniverseInterchangeService(dbFactory).ImportAsync(JsonSerializer.Serialize(new
        {
            universe = new { id = "spacegod", name = "SpaceGod" },
            entities = new object[] { new { id = "faction-sol", type = "faction", name = "Sol Federation", summary = "Earth's navy." } },
        }));
        Assert.That(seed.Success, Is.True, string.Join("; ", seed.Errors));
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }

    [Test]
    public async Task Declared_outcome_nodes_clear_the_unreachable_finding()
    {
        var json = JsonSerializer.Serialize(new
        {
            id = "conversation-spacegod-encounter-patrol", kind = "conversation", key = "spacegod/encounter-patrol",
            about = Array.Empty<string>(), speakers = new[] { "$them" },
            payload = new
            {
                start = "open",
                nodes = new object[]
                {
                    new { id = "open", speaker = "$them", line = "Heave to.", choices = new object[]
                        { new { text = "Comply.", next = "$outcome", tags = new[] { "comply" } } } },
                    new { id = "complied", speaker = "$them", line = "Proceed." },
                    new { id = "ran", speaker = "$them", line = "Pursuit." },
                },
            },
        });
        var draft = await svc.DraftAsync("spacegod", json, "agent");
        Assert.That(draft.Success, Is.True, string.Join("; ", draft.Errors));
        Assert.That(draft.Findings.Select(f => f.Code), Does.Contain("unreachable"));

        var stored = await verifier.SetOutcomeNodesAsync("spacegod", ["ran", "complied", "talked", "ran"]);
        Assert.That(stored, Is.EqualTo(new[] { "complied", "ran", "talked" }));
        Assert.That(await verifier.GetOutcomeNodesAsync("spacegod"), Is.EqualTo(stored));

        var again = await verifier.VerifyAsync("spacegod");
        Assert.That(again.Findings.Select(f => f.Code), Has.None.EqualTo("unreachable"));

        Assert.That(await verifier.SetOutcomeNodesAsync("spacegod", []), Is.Empty);
        Assert.That(await verifier.GetOutcomeNodesAsync("spacegod"), Is.Empty);
        Assert.ThrowsAsync<InvalidOperationException>(() => verifier.SetOutcomeNodesAsync("spacegod", ["$outcome"]));
        Assert.ThrowsAsync<InvalidOperationException>(() => verifier.SetOutcomeNodesAsync("no-such", ["talked"]));
    }
}
