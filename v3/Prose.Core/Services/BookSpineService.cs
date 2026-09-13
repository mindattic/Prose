using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// A book's reading order with its chapter boundaries made explicit, computed in one place.
///
/// <para><b>The node transition is the chapter boundary</b>, with one documented legacy exception
/// below. Before this service, four call sites each decided that question for themselves — and they
/// had already drifted apart, which is the defect this exists to end. On a flat single-node book
/// carrying <c>IsChapterStart</c> markers, the three of them produced three different books:</para>
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
/// <para>Grouping is "a consecutive run of beats sharing a node id", which is what the exporters
/// already did. That falls out correctly for a chapter that has been split into a nested Collection:
/// each leaf becomes its own group, in reading order, exactly as it prints today.</para>
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
    }

    /// <summary>One beat in reading-order context.</summary>
    /// <param name="Ordinal">1-based position in the whole book, matching the <c>#0001</c> handle
    /// the writer's spine shows.</param>
    /// <param name="IsSubHeading">The honest meaning of <c>Beat.IsChapterStart</c>: a sub-heading
    /// inside a chapter, carrying its own title ("Three Barrels"). Never a chapter boundary. This
    /// is the exporters' test — the flag AND a title that is not itself chapter-shaped — not the
    /// raw flag, which is also set on legacy heading beats.</param>
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
        var titles = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => nodeIds.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, n => n.Title, ct);

        // The legacy exception: only a book whose beats all hang off ONE node can have a chapter
        // opened by a beat marker. A properly chaptered book ignores the markers entirely.
        var flatBook = nodeIds.Count == 1;

        var chapters = new List<SpineChapter>();
        var open = new List<SpineBeat>();
        SpineChapter? pending = null;
        var currentNodeId = Guid.Empty;
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

            var nodeChanged = entry.NodeId != currentNodeId;
            var flatMarker = flatBook && beat.IsChapterStart && titleIsChapterShaped;

            if (nodeChanged || flatMarker)
            {
                CloseChapter();
                currentNodeId = entry.NodeId;

                var nodeTitle = titles.GetValueOrDefault(entry.NodeId, "").Trim();
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
                    entry.NodeId,
                    ordinal,
                    nodeTitle,
                    heading,
                    ChapterTitle.Parse(nodeTitle),
                    entry.NodeId == bookNodeId,
                    OpenedByBeatMarker: flatMarker && !nodeChanged,
                    Beats: []);
            }

            beatOrdinal++;
            var plain = ProseWordCount.ToPlainText(beat.Text);

            open.Add(new SpineBeat(
                beat.Id,
                entry.NodeId,
                beatOrdinal,
                beat.Number,
                beat.Title,
                Preview(plain),
                ProseWordCount.CountPlain(plain),
                IsSubHeading: beat.IsChapterStart && beatTitle is not null && !titleIsChapterShaped,
                LooksLikeStrayHeading: ChapterTitle.LooksLikeHeading(ChapterTitle.FirstLine(plain))));
        }

        CloseChapter();
        return new BookSpine(bookNodeId, chapters);
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
