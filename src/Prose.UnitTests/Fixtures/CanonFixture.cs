using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests.Fixtures;

/// <summary>
/// A deliberately trap-shaped canon, for proving that a query actually reaches the rows it claims
/// to cover.
///
/// <para><b>The bug class this exists to catch.</b> Prose's signature defect is a query that
/// returns zero rows and reads as good news. It comes in three shapes:</para>
/// <list type="number">
///   <item>Joining beats straight to a book id. The hierarchy is Book → Chapter → Beat, so a book
///     node owns <em>no beats directly</em> — that is the correct, healthy shape, confirmed against
///     the real Bushido Coda. A query that asks the book node for its beats gets 0 and reports
///     "nothing to fix". Worse, a chapter may itself hold sub-chapters (a Collection, ARCHITECTURE
///     §2c), so even a one-level descent misses beats.</item>
///   <item>An explicit-id lookup silently filtered out by the ambient universe scope, for want of
///     <c>IgnoreQueryFilters()</c>.</item>
///   <item>No universe scope wired at all, which makes <c>ScopedUniverseId</c> <c>Guid.Empty</c> and
///     turns every global filter off — so the query quietly reads the entire corpus.</item>
/// </list>
///
/// <para><b>Why two universes.</b> Universe B is not padding. It mirrors A's names and slugs and
/// hangs a beat directly off its book node, so a query that forgets the universe filter comes back
/// with a <em>wrong row</em> rather than an empty set. A wrong answer fails loudly; an empty one
/// looks like a clean bill of health, which is how these survived for months.</para>
///
/// <para>Shape of universe A. One branch runs the full ladder, the other stays flat, so a single
/// fixture proves both that deep nesting is reachable and that the older chapter-holds-beats shape
/// still works. Five beats; the book node owns none of them:</para>
/// <code>
/// BookA  "trap-book"                        ← owns NO beats (the trap)
///   ├── Chapter  "Part One"                 ← holds a sequence, no beats of its own
///   │     └── Sequence "The Approach"
///   │           ├── Scene  "Arrival"        → beats 1, 2
///   │           └── Sequel "The Reckoning"  → beat 3      (Kind="sequel")
///   └── Chapter  "Flat Chapter"             → beats 4, 5  (legacy shape, still valid)
/// </code>
/// <para>Depth from book to beat is 4 on one branch and 1 on the other. Any walk that assumes a
/// fixed depth gets a wrong answer from one of them.</para>
/// </summary>
public sealed class CanonFixture : IDisposable
{
    public static readonly Guid UniverseA = new("0a11e0a0-0000-7000-8000-00000000000a");
    public static readonly Guid UniverseB = new("0b22e0b0-0000-7000-8000-00000000000b");

    /// <summary>Deliberately identical in both universes, so a missing filter returns the wrong row.</summary>
    public const string SharedBookSlug = "trap-book";
    public const string SharedCharacterName = "Wren Halloway";

    private readonly SqliteConnection connection;

    public IDbContextFactory<ProseDbContext> Factory { get; }

    // ── Universe A ────────────────────────────────────────────────────────────
    public Guid BookA { get; private set; }
    public Guid DeepChapter { get; private set; }
    public Guid Sequence { get; private set; }
    public Guid Scene { get; private set; }
    public Guid Sequel { get; private set; }
    public Guid FlatChapter { get; private set; }

    /// <summary>All five beats under <see cref="BookA"/>, in reading order.</summary>
    public IReadOnlyList<Guid> BeatsA { get; private set; } = [];

    /// <summary>The three beats down the deep branch — reachable only by recursing four levels.</summary>
    public IReadOnlyList<Guid> DeepBeats { get; private set; } = [];

    public Guid CharacterA { get; private set; }

    // ── Universe B (decoy) ────────────────────────────────────────────────────
    public Guid BookB { get; private set; }

    /// <summary>A beat hanging DIRECTLY off B's book node — the wrong row a filterless query finds.</summary>
    public Guid BeatOnBookB { get; private set; }

    public Guid CharacterB { get; private set; }

    private CanonFixture()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        Factory = new SqliteFactory(connection);
        using var db = Factory.CreateDbContext();
        db.Database.EnsureCreated();
    }

    public static CanonFixture Create()
    {
        var fixture = new CanonFixture();
        fixture.Seed();
        return fixture;
    }

    private void Seed()
    {
        // Seed each universe under its own ambient scope so insert-stamping agrees with the
        // UniverseId set explicitly, rather than fighting it (StampUniverseOnAdded only fills a
        // UniverseId that is still Guid.Empty, but matching them keeps the intent legible).
        using (ScopeTo(UniverseA))
        {
            using var db = Factory.CreateDbContext();

            BookA = AddNode(db, new BookNode(), UniverseA, SharedBookSlug, "Trap Book", null, 100);
            DeepChapter = AddNode(db, new ChapterNode(), UniverseA, "part-one", "Part One", BookA, 100);
            Sequence = AddNode(db, new SequenceNode(), UniverseA, "the-approach", "The Approach", DeepChapter, 100);
            Scene = AddNode(db, new SceneNode(), UniverseA, "arrival", "Arrival", Sequence, 100);
            Sequel = AddNode(db, SceneNode.Sequel(), UniverseA, "the-reckoning", "The Reckoning", Sequence, 200);
            FlatChapter = AddNode(db, new ChapterNode(), UniverseA, "flat-chapter", "Flat Chapter", BookA, 200);

            var deep = new[]
            {
                AddBeat(db, Scene, 1, "The rain had opinions about the awning.", 100),
                AddBeat(db, Scene, 2, "She counted the exits twice and believed neither count.", 200),
                // The sequel: reaction and decision, not action.
                AddBeat(db, Sequel, 3, "Afterwards she sat with it, and chose the worse of two doors.", 100),
            };
            var flat = new[]
            {
                AddBeat(db, FlatChapter, 4, "Morning arrived the colour of a dead screen.", 100),
                AddBeat(db, FlatChapter, 5, "He went home under the lights.", 200),
            };

            DeepBeats = deep;
            BeatsA = [.. deep, .. flat];

            CharacterA = AddCharacterEntity(db, UniverseA, SharedCharacterName);
            db.SaveChanges();
        }

        using (ScopeTo(UniverseB))
        {
            using var db = Factory.CreateDbContext();

            // Same slug, same title, different universe.
            BookB = AddNode(db, new BookNode(), UniverseB, SharedBookSlug, "Trap Book", null, 100);

            // The decoy: a beat hanging directly off the BOOK node. Universe A has none, precisely
            // so a book-id join that forgets the universe filter finds THIS instead of nothing.
            BeatOnBookB = AddBeat(db, BookB, 900, "This beat belongs to another universe entirely.", 100);

            CharacterB = AddCharacterEntity(db, UniverseB, SharedCharacterName);
            db.SaveChanges();
        }
    }

    // ── scope control ─────────────────────────────────────────────────────────

    /// <summary>
    /// Set the ambient universe for the duration of the block, restoring whatever was there before.
    /// <see cref="UniverseScope.Current"/> is a process-wide mutable static, so every test touching
    /// it must be <c>[NonParallelizable]</c> and must restore it — a leaked scope shows up as
    /// unrelated tests failing at random, which destroys trust in the whole harness.
    /// </summary>
    public static IDisposable ScopeTo(Guid universeId) => new ScopeGuard(universeId);

    /// <summary>
    /// No ambient universe at all — <c>EffectiveId</c> becomes <c>Guid.Empty</c> and every global
    /// filter switches off. This is the "leak" control: a query run under it either refuses, or is
    /// deliberately corpus-wide. Anything else is reading other universes by accident.
    /// </summary>
    public static IDisposable Unscoped() => new ScopeGuard(null);

    private sealed class ScopeGuard : IDisposable
    {
        private readonly IUniverseContext? previous;

        public ScopeGuard(Guid? universeId)
        {
            previous = UniverseScope.Current;
            UniverseScope.Current = universeId is { } id ? new FixedUniverseContext(id) : null;
        }

        public void Dispose() => UniverseScope.Current = previous;
    }

    // ── seeding helpers ───────────────────────────────────────────────────────

    private static Guid AddNode(ProseDbContext db, Node node, Guid universeId, string slug, string title, Guid? parentId, double sortKey)
    {
        node.Id = Guid.CreateVersion7();
        node.UniverseId = universeId;
        node.Slug = slug;
        node.Title = title;
        node.ParentNodeId = parentId;
        node.SortKey = sortKey;
        // Kind is set by each node type's constructor (and deliberately overridden to "sequel" by
        // SceneNode.Sequel()), so it must not be reassigned here.
        db.Nodes.Add(node);
        return node.Id;
    }

    private static Guid AddBeat(ProseDbContext db, Guid nodeId, int number, string text, double sortKey)
    {
        var beat = new Beat
        {
            Id = Guid.CreateVersion7(),
            Number = number,
            Text = text,
            TextHash = Beat.ComputeHash(text),
            Kind = "prose",
            SceneType = "scene",
        };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = nodeId, BeatId = beat.Id, SortKey = sortKey });
        return beat.Id;
    }

    private static Guid AddCharacterEntity(ProseDbContext db, Guid universeId, string name)
    {
        var entity = new Entity
        {
            Id = Guid.CreateVersion7(),
            UniverseId = universeId,
            EntityType = "character",
            Name = name,
            Slug = name.ToLowerInvariant().Replace(' ', '-'),
            Status = "canon",
        };
        db.Entities.Add(entity);
        return entity.Id;
    }

    public void Dispose()
    {
        connection.Dispose();
    }

    private sealed class SqliteFactory(SqliteConnection conn) : IDbContextFactory<ProseDbContext>
    {
        private readonly DbContextOptions<ProseDbContext> options =
            new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options;

        public ProseDbContext CreateDbContext() => new(options);
    }
}
