using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// A book's reading order with its chapter boundaries made explicit, computed in one place.
///
/// <para><b>The chapter boundary is a transition between UNITS, and a unit is the nearest
/// ancestor-or-self of a beat's node that is not a sub-chapter layer</b> (see
/// <see cref="IsSubChapterLayer"/>), with one documented legacy exception below. Before this
/// service, four call sites each decided that question for themselves — and they had already
/// drifted apart, which is the defect this exists to end. On a flat single-node book carrying
/// <c>IsChapterStart</c> markers, the three of them produced three different books:</para>
/// <list type="bullet">
///   <item><c>DocxExportService</c> — <c>if (nodeChanged)</c>: one chapter.</item>
///   <item><c>ManuscriptExportService.LoadAsync</c> (epub/pdf) — <c>nodeChanged || flatMarker</c>: N chapters.</item>
///   <item><c>ManuscriptExportService.ExportMarkdownAsync</c> — <c>(nodeChanged &amp;&amp; multiChapter) || flatMarker</c>: N again, by a third route.</item>
/// </list>
/// <para>The same book exported with a different chapter structure depending on the file extension.
/// This service adopts the epub/pdf rule, because it is the one that yields a usable book for the
/// legacy flat shape; the docx behaviour was the bug. See <see cref="SpineChapter.OpenedByBeatMarker"/>.</para>
///
/// <para><see cref="Beat.IsChapterStart"/> is otherwise NOT a boundary. Both exporters carry
/// comments recording that it is overloaded: it marks genuine mid-chapter sub-headings <i>and</i>
/// legacy beats whose title duplicates the chapter title. It survives here as
/// <see cref="SpineBeat.IsSubHeading"/> — the exporters' own narrower test, not the raw flag — and
/// prose debris is reported through <see cref="SpineBeat.LooksLikeStrayHeading"/> rather than acted
/// on: the author decides whether a beat that opens with "Chapter 7" is a heading or a sentence
/// (RFC 0009).</para>
///
/// <para>Grouping is "a consecutive run of beats sharing a UNIT id". It used to be "sharing a node
/// id", which was the same thing only while the ladder was Book → Chapter → Beat. It stopped being
/// the same thing the first time a chapter was given a scene layer: <c>SceneDerivationService</c>
/// repoints the beats onto new <see cref="SceneNode"/>s, so N scene ids arrive where one chapter id
/// used to, and the parent chapter — now holding no beats of its own — never appears in the walk at
/// all and so could not appear in the spine. A 46-chapter novel exported as 66 chapters headed
/// <c>sidewalk</c>, <c>home terminal</c>, <c>transit platform</c>, and lost the heading of the
/// chapter that had been split. Rolling sub-chapter layers up to their chapter fixes every exporter
/// at once, because they all walk this.</para>
///
/// <para>The roll-up is stated <b>negatively</b> — skip upward past scene/sequence layers only —
/// so a chapter split into a nested Collection (Chapter → Chapter → Beat) is untouched: each leaf
/// sub-chapter is still its own unit, in reading order, exactly as it prints today.</para>
///
/// <para>The scene layer is not lost: <see cref="SpineBeat.NodeId"/> remains the beat's <i>direct</i>
/// node, so the spine reports both altitudes at once, and <see cref="SpineChapter.SubUnitNodeIds"/>
/// is the named way to ask which nodes a unit's beats actually hang off.</para>
/// </summary>
public sealed class BookSpineService(IDbContextFactory<ProseDbContext> dbFactory)
{
    /// <summary>
    /// One unit of a book — a chapter node and the beats that hang off it, in reading order.
    ///
    /// <para><paramref name="Ordinal"/> is this unit's 1-based position in the book. It is
    /// deliberately separate from <c>Parsed.Number</c>, the number the <i>title</i> claims: a book
    /// where those two disagree has a numbering defect, and keeping both is what lets the validator
    /// say so.</para>
    ///
    /// <para><paramref name="IsBookRoot"/> marks beats attached directly to the book node rather
    /// than to any chapter — the "unfiled" state <c>WrapInSingleChapterAsync</c> exists to repair.
    /// It is a real state in the corpus, not an error, so the spine reports it instead of hiding
    /// it.</para>
    /// </summary>
    /// <param name="Title">The node's own <c>Title</c> — the thing a rename edits. On a flat legacy
    /// book several units can share one node, and therefore one Title; that is a data defect for
    /// <c>--validate-chapters</c> to report, not something to paper over here.</param>
    /// <param name="Heading">What a manuscript prints for this unit. Resolved once, using the
    /// precedence both exporters already implement: a chapter-shaped beat title wins, then the
    /// node's title, then any beat title, then the ordinal.</param>
    /// <param name="OpenedByBeatMarker">This unit was opened by a beat's <c>IsChapterStart</c> on a
    /// flat single-node book, not by a node transition — the legacy shape. Every book where this is
    /// true wants <c>SplitIntoCollectionAsync</c> run on it.</param>
    public sealed record SpineChapter(
        Guid                            NodeId,
        int                             Ordinal,
        string                          Title,
        string                          Heading,
        ChapterTitle.ParsedChapterTitle Parsed,
        bool                            IsBookRoot,
        bool                            OpenedByBeatMarker,
        IReadOnlyList<SpineBeat>        Beats)
    {
        public int WordCount => Beats.Sum(b => b.WordCount);

        /// <summary>The nodes this unit's beats actually hang off, in first-sighting order. For a
        /// plain chapter that is just <see cref="NodeId"/>; for a chapter with a scene layer it is
        /// the scenes. This is how a consumer sees the sub-chapter structure the spine rolled up —
        /// notably <c>--validate-chapters</c>, which would otherwise report every scene as an
        /// <c>empty_chapter</c> that "prints in no export" when in fact its prose prints fine.</summary>
        public IEnumerable<Guid> SubUnitNodeIds => Beats.Select(b => b.NodeId).Distinct();
    }

    /// <summary>One beat in reading-order context.</summary>
    /// <param name="Ordinal">1-based position in the whole book, matching the <c>#0001</c> handle
    /// the writer's spine shows.</param>
    /// <param name="IsSubHeading">The honest meaning of <c>Beat.IsChapterStart</c>: a sub-heading
    /// inside a chapter, carrying its own title ("Three Barrels"). Never a chapter boundary. This
    /// is the exporters' test — the flag AND a title that is not itself chapter-shaped — not the
    /// raw flag, which is also set on legacy heading beats. Never true for a chapter's opening
    /// beat: the unit heading already prints there.</param>
    /// <param name="LooksLikeStrayHeading">This beat's first line reads as a unit heading
    /// ("Chapter 7", "## Interlude: Static"). On the first beat of a chapter that is almost always
    /// draft debris duplicating the node title. Reported, never acted on.</param>
    public sealed record SpineBeat(
        Guid    BeatId,
        Guid    NodeId,
        int     Ordinal,
        int     Number,
        string? Title,
        string  Preview,
        int     WordCount,
        bool    IsSubHeading,
        bool    LooksLikeStrayHeading);

    /// <summary>A whole book's spine. <see cref="Entries"/> is the flat reading-order sequence the
    /// writer's left-hand list renders; <see cref="Chapters"/> is the grouped shape the exporters
    /// walk. Both describe the same walk — there is no second computation to disagree.</summary>
    public sealed record BookSpine(Guid BookNodeId, IReadOnlyList<SpineChapter> Chapters)
    {
        public int ChapterCount => Chapters.Count;
        public int BeatCount    => Chapters.Sum(c => c.Beats.Count);
        public int WordCount    => Chapters.Sum(c => c.WordCount);

        /// <summary>Chapter breaks and beats interleaved in reading order, for a single flat list.</summary>
        public IEnumerable<SpineEntry> Entries
        {
            get
            {
                foreach (var chapter in Chapters)
                {
                    yield return new SpineEntry(chapter, null);
                    foreach (var beat in chapter.Beats) yield return new SpineEntry(chapter, beat);
                }
            }
        }
    }

    /// <summary>One row of the flat view: a chapter break when <see cref="Beat"/> is null,
    /// otherwise a beat that belongs to <see cref="Chapter"/>.</summary>
    public readonly record struct SpineEntry(SpineChapter Chapter, SpineBeat? Beat)
    {
        public bool IsChapterBreak => Beat is null;
    }

    /// <summary>
    /// Walk the book and group its beats by the node they hang off.
    ///
    /// <para>Uses <see cref="NodeWorkbenchService.WalkAsync"/> statically rather than taking a
    /// <see cref="NodeWorkbenchService"/> dependency — the same route <c>BeatRangeService</c> takes,
    /// and for the same reason: the workbench's own constructor pulls in the post-beat validation
    /// chain, which a read-only spine has no business requiring.</para>
    /// </summary>
    public async Task<BookSpine> GetAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var ordered = new List<NodeWorkbenchService.OrderedBeat>();
        await NodeWorkbenchService.WalkAsync(db, bookNodeId, ordered, new HashSet<Guid>(), false, ct);
        if (ordered.Count == 0) return new BookSpine(bookNodeId, []);

        // IgnoreQueryFilters(): these ids came out of the walk above, already resolved. Without it a
        // book outside whatever universe the ambient scope happens to hold loses every title — the
        // same bug class the walk itself documents.
        var nodeIds = ordered.Select(o => o.NodeId).Distinct().ToList();
        var nodes = await LoadWithAncestorsAsync(db, nodeIds, ct);

        // The legacy exception: only a book whose beats all hang off ONE node can have a chapter
        // opened by a beat marker. A properly chaptered book ignores the markers entirely.
        //
        // Computed on the raw BEAT-BEARING node ids, deliberately, not on unit ids: a single-chapter
        // book that has been scene-derived hangs its beats off several scene nodes that all roll up
        // to one unit, and testing units would call that flat and let stray IsChapterStart markers
        // shatter it into chapters it never had.
        var flatBook = nodeIds.Count == 1;

        // Each beat-bearing node's unit — itself, or the nearest ancestor that is not a scene or
        // sequence. This is the whole fix; everything below is unchanged except for asking it.
        var unitOf = nodeIds.ToDictionary(id => id, id => ResolveUnit(id, nodes, bookNodeId));

        var chapters = new List<SpineChapter>();
        var open = new List<SpineBeat>();
        SpineChapter? pending = null;
        var currentUnitId = Guid.Empty;
        var beatOrdinal = 0;

        void CloseChapter()
        {
            if (pending is null) return;
            chapters.Add(pending with { Beats = open.ToList() });
            open.Clear();
            pending = null;
        }

        foreach (var entry in ordered)
        {
            var beat = entry.Beat;
            var beatTitle = string.IsNullOrWhiteSpace(beat.Title) ? null : beat.Title!.Trim();
            var titleIsChapterShaped = ChapterTitle.IsLegacyBeatHeading(beatTitle);

            var unitId = unitOf[entry.NodeId];
            var unitChanged = unitId != currentUnitId;
            var flatMarker = flatBook && beat.IsChapterStart && titleIsChapterShaped;

            if (unitChanged || flatMarker)
            {
                CloseChapter();
                currentUnitId = unitId;

                var nodeTitle = nodes.GetValueOrDefault(unitId)?.Title.Trim() ?? "";
                var ordinal = chapters.Count + 1;

                // The precedence both exporters already implement. A beat title only outranks the
                // node's own title when it is ITSELF chapter-shaped — that is what stops an
                // unrelated beat title ("Across the Hall") from replacing a real chapter heading,
                // and what makes every Interlude print its name (its lead beat carries no title).
                var heading =
                    titleIsChapterShaped         ? beatTitle!
                    : nodeTitle.Length > 0       ? nodeTitle
                    : beatTitle                  ?? ChapterTitle.Format(ordinal, null);

                pending = new SpineChapter(
                    unitId,
                    ordinal,
                    nodeTitle,
                    heading,
                    ChapterTitle.Parse(nodeTitle),
                    unitId == bookNodeId,
                    OpenedByBeatMarker: flatMarker && !unitChanged,
                    Beats: []);
            }

            beatOrdinal++;
            var plain = ProseWordCount.ToPlainText(beat.Text);

            // A chapter's OPENING beat is never also a sub-heading, even when it carries the flag
            // and a plain title: the unit heading is already printing above it, and a sub-heading
            // block there would print the beat's title twice over. This is the `else if` both
            // exporters have always had — stated here once instead, so a consumer that walks
            // chapter.Beats cannot forget it. `open` is empty exactly when this beat opens the
            // chapter, because CloseChapter drains it at every boundary.
            var opensThisChapter = open.Count == 0;

            open.Add(new SpineBeat(
                beat.Id,
                entry.NodeId,
                beatOrdinal,
                beat.Number,
                beat.Title,
                Preview(plain),
                ProseWordCount.CountPlain(plain),
                IsSubHeading: !opensThisChapter && beat.IsChapterStart && beatTitle is not null && !titleIsChapterShaped,
                LooksLikeStrayHeading: ChapterTitle.LooksLikeHeading(ChapterTitle.FirstLine(plain))));
        }

        CloseChapter();
        return new BookSpine(bookNodeId, chapters);
    }

    /// <summary>What a node is for the purpose of chapter boundaries, read off the TPH
    /// discriminator.</summary>
    private sealed record NodeRow(Guid Id, Guid? ParentNodeId, string Title, string NodeType);

    /// <summary>The layers that sit BELOW a chapter and must never print as one.
    ///
    /// <para>Tested on <c>NodeType</c>, the discriminator, and never on <c>Kind</c>. <c>Node</c>'s
    /// own header states the rule — the CLR type is the structural truth, <c>Kind</c> is a
    /// free-form category hint the author can edit — and three consequences follow. Legacy rows
    /// where <c>Kind="scene"</c> sits on a <c>ChapterNode</c> have printed as chapters their whole
    /// life and keep doing so, so this change is a no-op for them. <c>SceneNode.Sequel()</c> keeps
    /// <c>Kind="sequel"</c> on a <c>SceneNode</c>, so sequels roll up without naming them here. And
    /// a <c>ChapterNode</c> somebody typed <c>Kind="scene"</c> on does not silently vanish from the
    /// manuscript.</para>
    ///
    /// <para>A <c>SequenceNode</c> holds scenes rather than beats today, but it is listed because a
    /// Chapter → Sequence → Scene book must roll up two levels, and because a sequence that is
    /// given beats by hand must not become a chapter either.</para></summary>
    private static bool IsSubChapterLayer(NodeRow node) =>
        node.NodeType is "scene" or "sequence";

    /// <summary>The nodes the walk found, plus every ancestor needed to resolve their units.
    ///
    /// <para>The walk hands out each beat's DIRECT node id and nothing else, so the parent chain is
    /// not available from it. Closing over ancestors here — rather than widening
    /// <c>OrderedBeat</c> — keeps the walk dumb and confines a fact only the spine needs to the
    /// spine. The ladder tops out at Series → Book → Chapter → Sequence → Scene, so this is one
    /// extra cheap read per level, usually one in total and none at all for a book with no scene
    /// layer.</para></summary>
    private static async Task<Dictionary<Guid, NodeRow>> LoadWithAncestorsAsync(
        ProseDbContext db, List<Guid> nodeIds, CancellationToken ct)
    {
        var known = new Dictionary<Guid, NodeRow>();
        var wanted = new List<Guid>(nodeIds);

        // Bounded by the ladder's depth; the visited set makes a cyclic parent chain terminate
        // rather than hang, the same guard WalkAsync carries for the same reason.
        while (wanted.Count > 0)
        {
            // IgnoreQueryFilters(): these ids came out of the walk, already resolved. Without it a
            // book outside whatever universe the ambient scope happens to hold loses every title —
            // the same bug class the walk itself documents.
            var rows = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                .Where(n => wanted.Contains(n.Id))
                .Select(n => new NodeRow(n.Id, n.ParentNodeId, n.Title, EF.Property<string>(n, "NodeType")))
                .ToListAsync(ct);

            foreach (var row in rows) known[row.Id] = row;

            wanted = rows
                .Where(r => r.ParentNodeId is not null && !known.ContainsKey(r.ParentNodeId.Value))
                .Select(r => r.ParentNodeId!.Value)
                .Distinct()
                .ToList();
        }

        return known;
    }

    /// <summary>The unit a beat-bearing node belongs to: itself, or the nearest ancestor that is
    /// not a sub-chapter layer.
    ///
    /// <para>Never rolls up into the book root. A scene parented straight to a book is creatable,
    /// and merging those would collapse a whole book into one headingless unit and trip
    /// <c>--validate-chapters</c>'s <c>unfiled_beats</c> blocker. A scene with no real chapter
    /// ancestor stays its own unit — visibly wrong in the validator's terms, which is where a
    /// structural defect belongs, rather than silently wrong in the manuscript.</para></summary>
    private static Guid ResolveUnit(Guid nodeId, IReadOnlyDictionary<Guid, NodeRow> nodes, Guid bookNodeId)
    {
        if (!nodes.TryGetValue(nodeId, out var node) || !IsSubChapterLayer(node)) return nodeId;

        var visited = new HashSet<Guid> { nodeId };
        var current = node;
        while (current.ParentNodeId is { } parentId
               && visited.Add(parentId)
               && parentId != bookNodeId
               && nodes.TryGetValue(parentId, out var parent))
        {
            if (!IsSubChapterLayer(parent)) return parentId;
            current = parent;
        }

        return nodeId;
    }

    /// <summary>The first line of a beat, short enough to sit in a list. This is what makes a beat
    /// findable in a 500-page book nobody has re-read.</summary>
    private const int PreviewLength = 90;

    private static string Preview(string plain)
    {
        var line = ChapterTitle.FirstLine(plain);
        if (line.Length == 0) return "(empty)";
        return line.Length <= PreviewLength ? line : line[..PreviewLength].TrimEnd() + "…";
    }
}

/// <summary>
/// Counting the words in a beat, which is not the same job as counting the words in an export.
///
/// <para>Beat text carries <c>&lt;entity&gt;</c> tags and inline emphasis markers. The export-side
/// counters in <c>DocxExportService</c> and <c>NodeFullExportService</c> are fed text that has
/// already been flattened, so they split on whitespace and are right to; counting a beat that way
/// would count <c>guid="01a0…"</c> as words. Strip first, then split.</para>
/// </summary>
public static class ProseWordCount
{
    private static readonly char[] Whitespace = [' ', '\t', '\n', '\r'];

    /// <summary>Tagged beat prose reduced to the words a reader sees.</summary>
    public static string ToPlainText(string? tagged) =>
        ProseInline.StripFormatting(BeatMarkup.StripEntityTags(tagged));

    /// <summary>Word count of tagged beat prose.</summary>
    public static int Count(string? tagged) => CountPlain(ToPlainText(tagged));

    /// <summary>Word count of text that has already been flattened by
    /// <see cref="ToPlainText"/>.</summary>
    public static int CountPlain(string? plain) =>
        string.IsNullOrEmpty(plain) ? 0 : plain.Split(Whitespace, StringSplitOptions.RemoveEmptyEntries).Length;
}
