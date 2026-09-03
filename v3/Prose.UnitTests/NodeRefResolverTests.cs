using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Behaviour + anti-regression coverage for <see cref="NodeRefResolver"/>, the one sanctioned way
/// to turn a user-supplied node reference into an id. It had no tests at all until 2026-08-24.
///
/// <para>Why it matters: an audit that day found <b>twelve</b> private copies of this helper across
/// Prose.Mcp and Prose.Cli, six of them broken — five with no <c>IgnoreQueryFilters()</c> on either
/// branch (so <c>book_health</c>, <c>chekhov_audit</c>, <c>storyscope_audit</c>,
/// <c>audit_book_commandments</c> and the plant/payoff tools all returned node_not_found for any
/// book outside the ambient universe when addressed by slug), one — <c>Tools.ReaderQa.cs</c>, the
/// last publish gate — carrying the same "GUID branch fixed, slug branch missed" split that had
/// already been found and re-patched four separate times in eight days. Every copy now delegates
/// here, and <see cref="NoPrivateNodeResolversHaveBeenReintroduced"/> keeps it that way.</para>
/// </summary>
[TestFixture]
public class NodeRefResolverTests
{
    private static readonly Guid UniverseGlmz = new("0197e9c9-1aaa-7000-8000-00000000001a");
    private static readonly Guid UniverseScry = new("0197e9c9-1bbb-7000-8000-00000000001b");

    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;
    private FakeUniverseContext universe = null!;
    private IUniverseContext? previousScope;

    [SetUp]
    public void SetUp()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ss_noderef_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "engine_data"));
        paths = new TestPathProviderWithRoot(root);
        factory = TestDbFactory.For(paths, "noderef");
        universe = new FakeUniverseContext();
        previousScope = UniverseScope.Current;
        UniverseScope.Current = universe;
    }

    [TearDown]
    public void TearDown()
    {
        UniverseScope.Current = previousScope;
        TestDbFactory.Reset(paths);
        try { Directory.Delete(paths.DataRoot, recursive: true); } catch { /* best effort */ }
    }

    private Guid AddBook(Guid forUniverse, string slug, string? code, Guid? id = null)
    {
        universe.CurrentId = forUniverse;               // drives StampUniverseOnAdded
        using var db = factory.CreateDbContext();
        var node = new BookNode
        {
            Id = id ?? Guid.CreateVersion7(),
            Slug = slug,
            NodeCode = code,
            Title = slug,
        };
        db.Nodes.Add(node);
        db.SaveChanges();
        return node.Id;
    }

    // ── The cross-universe regression the six broken copies all shared ────────

    [Test]
    public async Task ResolvesBySlug_ForABookOutsideTheAmbientUniverse()
    {
        var vigl = AddBook(UniverseScry, "vigil-s-end", "VIGL");
        universe.CurrentId = UniverseGlmz;             // ambient scope is the OTHER universe

        await using var db = factory.CreateDbContext();
        Assert.That(await NodeRefResolver.ResolveAsync(db, "vigil-s-end"), Is.EqualTo(vigl),
            "an explicit slug names exactly one node — ambient universe scope can only suppress the right answer");
    }

    [Test]
    public async Task ResolvesByNodeCode_ForABookOutsideTheAmbientUniverse()
    {
        var vigl = AddBook(UniverseScry, "vigil-s-end", "VIGL");
        universe.CurrentId = UniverseGlmz;

        await using var db = factory.CreateDbContext();
        Assert.That(await NodeRefResolver.ResolveAsync(db, "VIGL"), Is.EqualTo(vigl));
    }

    [Test]
    public async Task ResolvesByGuid_ForABookOutsideTheAmbientUniverse()
    {
        var vigl = AddBook(UniverseScry, "vigil-s-end", "VIGL");
        universe.CurrentId = UniverseGlmz;

        await using var db = factory.CreateDbContext();
        Assert.That(await NodeRefResolver.ResolveAsync(db, vigl.ToString()), Is.EqualTo(vigl));
    }

    [Test]
    public async Task SlugAndCodeMatching_AreCaseInsensitive()
    {
        var bcoda = AddBook(UniverseGlmz, "bushido_coda", "BCODA");

        await using var db = factory.CreateDbContext();
        Assert.That(await NodeRefResolver.ResolveAsync(db, "BUSHIDO_CODA"), Is.EqualTo(bcoda));
        Assert.That(await NodeRefResolver.ResolveAsync(db, "bcoda"), Is.EqualTo(bcoda));
        Assert.That(await NodeRefResolver.ResolveAsync(db, "  bcoda  "), Is.EqualTo(bcoda),
            "surrounding whitespace from a copy-paste must not defeat the lookup");
    }

    // ── Not-found cases: a wrong answer here is worse than a clean null ───────

    [Test]
    public async Task WellFormedGuidThatMatchesNoNode_ReturnsNull()
    {
        AddBook(UniverseGlmz, "bushido_coda", "BCODA");

        await using var db = factory.CreateDbContext();
        Assert.That(await NodeRefResolver.ResolveAsync(db, Guid.CreateVersion7().ToString()), Is.Null,
            "returning the parsed GUID verbatim just moves the failure downstream into a confusing error");
    }

    [Test]
    public async Task UnknownReference_AndEmptyReference_ReturnNull()
    {
        AddBook(UniverseGlmz, "bushido_coda", "BCODA");

        await using var db = factory.CreateDbContext();
        Assert.That(await NodeRefResolver.ResolveAsync(db, "no-such-book"), Is.Null);
        Assert.That(await NodeRefResolver.ResolveAsync(db, ""), Is.Null);
        Assert.That(await NodeRefResolver.ResolveAsync(db, "   "), Is.Null);
        Assert.That(await NodeRefResolver.ResolveAsync(db, null), Is.Null);
    }

    // ── GUID prefix ──────────────────────────────────────────────────────────

    [Test]
    public async Task ResolvesAUniqueGuidPrefix()
    {
        var id = new Guid("0197e9c9-abcd-7000-8000-0000000000ff");
        AddBook(UniverseGlmz, "prefixed", "PFX", id);

        await using var db = factory.CreateDbContext();
        Assert.That(await NodeRefResolver.ResolveAsync(db, "0197e9c9-abcd"), Is.EqualTo(id));
    }

    [Test]
    public async Task AmbiguousGuidPrefix_ReturnsNull_RatherThanAnArbitraryMatch()
    {
        AddBook(UniverseGlmz, "twin-a", "TWNA", new Guid("0197e9c9-eeee-7000-8000-00000000aaaa"));
        AddBook(UniverseGlmz, "twin-b", "TWNB", new Guid("0197e9c9-eeee-7000-8000-00000000bbbb"));

        await using var db = factory.CreateDbContext();
        Assert.That(await NodeRefResolver.ResolveAsync(db, "0197e9c9-eeee"), Is.Null,
            "silently picking one of two books is far worse than failing the lookup");
    }

    [Test]
    public async Task AnExactSlugWins_OverAGuidPrefixInterpretation()
    {
        // A slug made only of hex characters ("beefed") is both a real slug and a plausible GUID
        // prefix. The slug is the more specific reading and must win.
        var beefed = AddBook(UniverseGlmz, "beefed", null);
        AddBook(UniverseGlmz, "other", null, new Guid("beefed00-0000-7000-8000-000000000001"));

        await using var db = factory.CreateDbContext();
        Assert.That(await NodeRefResolver.ResolveAsync(db, "beefed"), Is.EqualTo(beefed));
    }

    // ── The structural guard that stops copy #13 ─────────────────────────────

    [Test]
    public void NoPrivateNodeResolversHaveBeenReintroduced()
    {
        var root = FindRepoRoot();
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "v3"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                file.EndsWith("NodeRefResolver.cs", StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith("NodeRefResolverTests.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var text = File.ReadAllText(file);

            // A method whose name says it resolves a node reference, whose body queries db.Nodes
            // itself instead of delegating. Matches the brace-bodied form only — the delegating
            // one-liners this audit left behind are expression-bodied and carry no body to scan.
            foreach (var m in Regex.Matches(
                         text,
                         @"Task<Guid\??>\s+Resolve\w*Node\w*Async\s*\([^)]*\)\s*\{(?<body>(?:[^{}]|\{[^{}]*\})*)\}",
                         RegexOptions.Singleline))
            {
                var body = ((Match)m).Groups["body"].Value;
                if (body.Contains(".Nodes"))
                    offenders.Add(Path.GetRelativePath(root, file));
            }
        }

        Assert.That(offenders, Is.Empty,
            "These files hand-roll node reference resolution instead of calling NodeRefResolver. "
          + "Twelve such copies existed on 2026-08-24 and six were silently broken cross-universe — "
          + "delegate to NodeRefResolver.ResolveAsync instead: " + string.Join(", ", offenders.Distinct()));
    }

    // Minimal IUniverseContext — only CurrentId drives the query filters. (Every universe-scoping
    // fixture in this project keeps its own private copy of this; matching that convention rather
    // than hoisting one shared fake as a drive-by refactor.)
    private sealed class FakeUniverseContext : IUniverseContext
    {
        public Guid CurrentId { get; set; } = Guid.Empty;
        // A fake that pins CurrentId HAS named its universe — that is exactly what an explicit
        // scope means (Story Ledger Phase 3, UnscopedUniverseWriteCheck). Guid.Empty means no
        // universe is wired at all, where scoping is a no-op and nothing gates on this.
        public bool IsExplicitlyScoped => CurrentId != Guid.Empty;

        public string CurrentSlug => "test";
        public UniverseInfo? CurrentUniverse => new(CurrentId, CurrentSlug, "Test", null, "a test world", true, 100);
        public IReadOnlyList<UniverseInfo> ListUniverses() => new List<UniverseInfo>();
        public bool IsGlmz => CurrentId == Guid.Empty;
        public string UniverseGroundingOr(string glmzFallback) => IsGlmz ? glmzFallback : "a self-contained fictional world";
        public void UseUniverse(Guid id) { CurrentId = id; UniverseScope.BumpEpoch(); }
        public bool UseUniverseBySlug(string slug) => false;
        public void SetFlowUniverse(Guid? id) { }
        public void PersistAsDefault(Guid id) { }
        public void Refresh() { }
    }

    private static string FindRepoRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "v3", "Prose.Core"))) return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        Assert.Fail("Could not locate the repo root (no v3/Prose.Core above the test binary).");
        return "";
    }
}
