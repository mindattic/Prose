using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Core.Composition.Window;

/// <summary>One beat of verbatim prior prose inside the scene window.</summary>
public sealed record WindowedBeat(Guid BeatId, Guid ChapterNodeId, string ChapterTitle, string Text, int? StoryPosition);

/// <summary>
/// The direct fix for the v4 plan's confirmed root cause #1: v3's <c>SceneSoFar</c> resets to
/// <c>""</c> at every chapter boundary (<c>AutoRunCli.cs:280</c> and the same pattern in
/// <c>ExpandBeatCli.cs</c>/<c>RunCorpusCli.cs</c>). This service returns a genuine sliding window
/// over the book's actual reading order — built on <see cref="NodeWorkbenchService.GetOrderedBeatsAsync"/>,
/// the same validated "every beat under this book, in order" walk the rest of the codebase already
/// uses — and is chapter-blind BY DESIGN: it does not know or care where a chapter boundary falls,
/// so it can never reset to empty at one.
/// </summary>
public sealed class SceneWindowService
{
    private readonly NodeWorkbenchService workbench;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    public SceneWindowService(NodeWorkbenchService workbench, IDbContextFactory<ProseDbContext> dbFactory)
    {
        this.workbench = workbench;
        this.dbFactory = dbFactory;
    }

    /// <summary>
    /// The last <paramref name="windowSizeBeats"/> beats of verbatim text strictly before
    /// <paramref name="beforeBeatId"/>, in reading order. If <paramref name="beforeBeatId"/> is
    /// not found (e.g. a brand-new beat not yet inserted), the window is the book's last
    /// <paramref name="windowSizeBeats"/> beats.
    /// </summary>
    /// <param name="includeAnchor">True when <paramref name="beforeBeatId"/> is the beat a new one is
    /// being inserted AFTER: the window then ends with it. Without this the insert path's window
    /// stopped one beat early, and the model never saw the beat it was continuing.</param>
    public async Task<IReadOnlyList<WindowedBeat>> GetWindowAsync(
        Guid bookNodeId, Guid beforeBeatId, int windowSizeBeats, CancellationToken ct = default,
        bool includeAnchor = false)
    {
        // DistinctBy: a beat linked to two nodes is walked twice.
        var ordered = (await workbench.GetOrderedBeatsAsync(bookNodeId, ct)).DistinctBy(o => o.Beat.Id).ToList();
        var idx = ordered.FindIndex(o => o.Beat.Id == beforeBeatId);
        var end = idx < 0 ? ordered.Count : includeAnchor ? idx + 1 : idx;
        var start = Math.Max(0, end - windowSizeBeats);
        var slice = ordered.Skip(start).Take(end - start).ToList();
        if (slice.Count == 0) return [];

        // Chapter titles are for DISPLAY ONLY (labeling which chapter a windowed beat came from
        // in the prompt) — never used to gate or reset the window itself. That distinction is the
        // whole fix: v3's SceneSoFar treated the chapter boundary as a reset signal; this doesn't.
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var nodeIds = slice.Select(o => o.NodeId).Distinct().ToList();
        var titles = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => nodeIds.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, n => n.Title ?? "", ct);

        return slice
            // The reader's text: this goes into "SCENE SO FAR" verbatim, and raw <entity guid=…>
            // markup there spent the window's budget on GUIDs and invited the model to echo tags.
            .Select(o => new WindowedBeat(o.Beat.Id, o.NodeId, titles.GetValueOrDefault(o.NodeId, ""),
                BeatMarkup.StripEntityTags(o.Beat.Text), o.Beat.StoryPosition))
            .ToList();
    }
}
