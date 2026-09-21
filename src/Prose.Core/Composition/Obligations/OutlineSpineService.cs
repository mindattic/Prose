using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Obligations;

namespace Prose.Core.Composition.Obligations;

/// <summary>One outline entry, resolved to where it actually falls in the book as written.</summary>
/// <param name="Plan">The parsed <c>## BEAT SPINE</c> entry.</param>
/// <param name="ChapterOrdinal">1-based chapter this entry is expected to land in.</param>
/// <param name="ChapterNodeId">That chapter's node.</param>
/// <param name="FirstBeatOrdinal">0-based index, in reading order, of the first beat this entry covers.</param>
public sealed record SpineEntry(BeatPlan Plan, int ChapterOrdinal, Guid ChapterNodeId, int FirstBeatOrdinal);

/// <summary>The outline block handed to the writer, plus where in the spine it sits.</summary>
public sealed record OutlineSpineSlice(IReadOnlyList<BeatPlan> Entries, int CurrentIndex, int TotalEntries, string Block);

/// <summary>
/// The outline, used as a spine rather than a fallback.
///
/// <para><b>Why this exists.</b> The outline was only ever injected into a prompt when a separate
/// doc-stack happened to come back empty — consulted as a substitute for context, never as "here
/// is what happens next", and never re-checked against what was actually written. A book could
/// therefore drift off its own outline for thirty chapters without anything noticing, because
/// nothing was ever comparing the two. This service does the two things that turns an outline
/// into a spine: it always hands the writer the next few entries, and it records each entry as an
/// obligation the book has taken on, so an unaddressed one becomes visible instead of forgotten.</para>
///
/// <para><b>The mapping is proportional, and that is a real limitation.</b> A spine has ~14
/// entries; a finished book has hundreds of beats. Nothing in the schema links a beat back to the
/// outline entry it belongs to, so an entry's position is computed by even distribution across
/// the book's beats in reading order — the same distribution <c>NodeOutlineService</c> already
/// uses when it seeds planned beats from a spine. It is an approximation: it says roughly where
/// an entry was meant to land, not where the author would say it landed. Treated as a hint for
/// the writer and a due-date for the ledger, that is enough; it is not evidence of anything and
/// is deliberately never used to CLOSE an obligation, only to open one and to notice silence.</para>
///
/// <para>Nothing here blocks a write. The chapter-close diff files findings
/// (<see cref="FindingCategory.OutlineDrift"/>) and stops — per RFC 0009 a finding describes,
/// it never instructs, and the author decides whether a skipped outline entry is drift or a
/// better idea than the outline had.</para>
/// </summary>
public sealed class OutlineSpineService(
    IDbContextFactory<ProseDbContext> dbFactory,
    NodeWorkbenchService workbench,
    NarrativeObligationService obligations,
    FindingsService? findings = null,
    ILogger<OutlineSpineService>? log = null)
{
    /// <summary>Marks an obligation row as having come from the outline, so the chapter-close
    /// diff can find its own rows again without re-parsing anything.</summary>
    public const string DescriptionPrefix = "Outline beat ";

    /// <summary>
    /// The book's parsed <c>## BEAT SPINE</c>, preferring the structured
    /// <see cref="NodeOutlineSection"/> row and falling back to the <c>Nodes.NodeOutline</c> blob
    /// it replaced. Empty when the book has no spine — which is a normal state, not an error:
    /// most books in this corpus were written before spines existed.
    /// </summary>
    public async Task<IReadOnlyList<BeatPlan>> GetSpineAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var section = await db.NodeOutlineSections.AsNoTracking().IgnoreQueryFilters()
            .Where(s => s.NodeId == bookNodeId && s.SectionType == "BeatSpine")
            .Select(s => s.Content)
            .FirstOrDefaultAsync(ct);

        // The section row holds the section BODY; ParseBeatSpine looks for the heading, so put it
        // back. One parser for both storage shapes beats two that can disagree about the format.
        if (!string.IsNullOrWhiteSpace(section))
            return NodeOutlineService.ParseBeatSpine("## BEAT SPINE\n" + section);

        var blob = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Id == bookNodeId)
            .Select(n => n.NodeOutline)
            .FirstOrDefaultAsync(ct);

        return string.IsNullOrWhiteSpace(blob) ? [] : NodeOutlineService.ParseBeatSpine(blob);
    }

    /// <summary>
    /// Every spine entry with the chapter and beat position it maps onto. Empty when the book has
    /// no spine or no beats.
    /// </summary>
    public async Task<IReadOnlyList<SpineEntry>> ResolveAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        var plans = await GetSpineAsync(bookNodeId, ct);
        if (plans.Count == 0) return [];

        var ordered = await workbench.GetOrderedBeatsAsync(bookNodeId, ct);
        if (ordered.Count == 0) return [];

        // Chapters in reading order, so an entry can be given a chapter-scoped due date.
        var chapterOrder = new List<Guid>();
        foreach (var o in ordered)
            if (chapterOrder.Count == 0 || chapterOrder[^1] != o.NodeId)
                if (!chapterOrder.Contains(o.NodeId)) chapterOrder.Add(o.NodeId);

        var resolved = new List<SpineEntry>(plans.Count);
        for (var i = 0; i < plans.Count; i++)
        {
            var firstBeat = (int)((long)i * ordered.Count / plans.Count);
            if (firstBeat >= ordered.Count) firstBeat = ordered.Count - 1;
            var chapterNodeId = ordered[firstBeat].NodeId;
            var chapterOrdinal = chapterOrder.IndexOf(chapterNodeId) + 1;
            resolved.Add(new SpineEntry(plans[i], chapterOrdinal, chapterNodeId, firstBeat));
        }
        return resolved;
    }

    /// <summary>
    /// The prompt block: the entry covering this beat plus the next <paramref name="lookahead"/>-1.
    ///
    /// <para>Returns null only when the book genuinely has no spine. When it has one this is
    /// ALWAYS present — that is the whole correction. A block that appears only when some other
    /// block is missing teaches the writer that the outline is optional.</para>
    /// </summary>
    public async Task<OutlineSpineSlice?> GetSliceAsync(
        Guid bookNodeId, Guid beatId, int lookahead = 3, CancellationToken ct = default)
    {
        var resolved = await ResolveAsync(bookNodeId, ct);
        if (resolved.Count == 0) return null;

        var ordered = await workbench.GetOrderedBeatsAsync(bookNodeId, ct);
        var beatOrdinal = ordered.FindIndex(o => o.Beat.Id == beatId);
        // An unknown beat (a brand-new one not yet in reading order) reads as "at the start",
        // which is the safe direction: it shows the writer more of what is coming, not less.
        if (beatOrdinal < 0) beatOrdinal = 0;

        var current = 0;
        for (var i = 0; i < resolved.Count; i++)
            if (resolved[i].FirstBeatOrdinal <= beatOrdinal) current = i;

        var slice = resolved.Skip(current).Take(Math.Max(1, lookahead)).Select(r => r.Plan).ToList();

        var lines = slice.Select(p =>
            string.IsNullOrWhiteSpace(p.StructureRole)
                ? $"  {p.Index}. {p.Title} — {p.Goal}"
                : $"  {p.Index}. [{p.StructureRole}] {p.Title} — {p.Goal}");

        var block =
            $"WHAT THE OUTLINE SAYS HAPPENS HERE (entry {slice[0].Index} of {resolved.Count}):\n"
            + string.Join("\n", lines);

        return new OutlineSpineSlice(slice, current, resolved.Count, block);
    }

    /// <summary>
    /// Opens an <c>Authored</c> obligation for every spine entry the book has reached by this
    /// beat, including any earlier entry that was never opened — generation can jump, and an
    /// entry silently skipped is exactly the thing worth catching.
    ///
    /// <para>Idempotent: <see cref="NarrativeObligationService.OpenAsync"/> refuses a duplicate on
    /// its own DedupKey, so re-running over a book already registered is free and changes nothing.
    /// Returns how many rows were newly opened.</para>
    /// </summary>
    public async Task<int> RegisterReachedAsync(
        Guid bookNodeId, Guid beatId, string actor, CancellationToken ct = default)
    {
        var resolved = await ResolveAsync(bookNodeId, ct);
        if (resolved.Count == 0) return 0;

        var ordered = await workbench.GetOrderedBeatsAsync(bookNodeId, ct);
        var beatOrdinal = ordered.FindIndex(o => o.Beat.Id == beatId);
        if (beatOrdinal < 0) return 0;

        var opened = 0;
        foreach (var entry in resolved.Where(r => r.FirstBeatOrdinal <= beatOrdinal))
        {
            var result = await obligations.OpenAsync(
                bookNodeId,
                ObligationKind.Promise,
                Describe(entry.Plan),
                actor,
                trigger: $"outline entry {entry.Plan.Index}",
                dueKind: ObligationDueKind.Chapter,
                dueValue: entry.ChapterOrdinal,
                ct: ct);

            if (result.Ok) opened++;
            // A refusal here is almost always "duplicate", which is the steady state after the
            // first run — only surprises are worth a log line.
            else if (result.Error is not null && !result.Error.StartsWith("duplicate"))
                log?.LogWarning("[outline-spine] entry {Index} not opened: {Error}", entry.Plan.Index, result.Error);
        }
        return opened;
    }

    /// <summary>
    /// The period close: outline entries due by this chapter that are still Open, reported as
    /// <see cref="FindingCategory.OutlineDrift"/> findings.
    ///
    /// <para>Non-blocking by construction, and severity stays Low: a skipped outline entry is
    /// frequently the author having had a better idea, and this cannot tell the difference. It
    /// reports the silence and leaves the judgement where it belongs. Returns the summaries filed.</para>
    /// </summary>
    public async Task<IReadOnlyList<string>> DiffChapterAsync(
        Guid bookNodeId, int chapterOrdinal, CancellationToken ct = default)
    {
        var resolved = await ResolveAsync(bookNodeId, ct);
        if (resolved.Count == 0) return [];

        var open = await obligations.ListAsync(bookNodeId, state: ObligationState.Open, ct: ct);
        var openByDescription = open.Select(o => o.Description).ToHashSet(StringComparer.Ordinal);

        var overdue = resolved
            .Where(r => r.ChapterOrdinal <= chapterOrdinal && openByDescription.Contains(Describe(r.Plan)))
            .ToList();
        if (overdue.Count == 0) return [];

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var slug = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Id == bookNodeId).Select(n => n.Slug).FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(slug)) return [];

        var filed = new List<string>(overdue.Count);
        foreach (var entry in overdue)
        {
            var summary =
                $"OUTLINE-UNADDRESSED [entry {entry.Plan.Index}, due chapter {entry.ChapterOrdinal}] {entry.Plan.Title}";
            findings?.Upsert(
                filePath: $"node:{slug}",
                chapterId: null,
                category: FindingCategory.OutlineDrift,
                severity: FindingSeverity.Low,
                summary: summary,
                snippet: entry.Plan.Goal,
                suggestedFix: "Either write the beat this entry describes, or close/drop the obligation "
                            + "to record that the book went another way on purpose.");
            filed.Add(summary);
        }
        return filed;
    }

    /// <summary>The obligation description for one entry. Stable, because it is also the
    /// DedupKey's input — changing this format orphans every row already written.</summary>
    private static string Describe(BeatPlan plan) =>
        $"{DescriptionPrefix}{plan.Index}: {plan.Title} — {plan.Goal}";
}
