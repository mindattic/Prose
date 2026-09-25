using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.WriterUi.Services;

/// <summary>
/// Reading the book straight through.
///
/// <para><b>This is the felt pass, and it is an instrument.</b> The question it answers is not
/// "what is wrong with this paragraph" — every other surface in Prose answers that. It is "where
/// did attention die", which can only be measured by reading or listening in sequence, without
/// stopping, and which is destroyed the moment anything invites analysis. That is why this service
/// exposes exactly two verbs: give me the next beat, and record that I stopped caring here.</para>
///
/// <para>Scoped, like the other UI services, for the flow-universe pin.</para>
/// </summary>
public sealed class ReadService(
    IDbContextFactory<ProseDbContext> dbFactory,
    IUniverseContext universe,
    NodeWorkbenchService workbench,
    FindingsService findings)
{
    /// <param name="Text">Reader-visible. No entity chips, no markers, no markup — this surface
    /// shows the book, not the manuscript.</param>
    public sealed record Page(
        Guid BeatId, int Number, string? ChapterTitle, string? PlaceName, string Text);

    public sealed record BookChoice(Guid Id, string Title, string Slug, int BeatCount);

    public async Task<IReadOnlyList<BookChoice>> BooksAsync(CancellationToken ct = default)
    {
        // BookNodes, the same view the editor lists from — a second definition of "which rows are
        // books" is a second place for a book to go missing from one surface and not the other.
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var books = await db.BookNodes.AsNoTracking().IgnoreQueryFilters()
            .OrderBy(b => b.Title)
            .Select(b => new { b.Id, b.Title, b.Slug })
            .ToListAsync(ct);

        var rows = new List<BookChoice>(books.Count);
        foreach (var b in books)
            rows.Add(new BookChoice(b.Id, b.Title ?? "(untitled)", b.Slug ?? "",
                                    await workbench.CountBeatsAsync(b.Id, ct)));

        // A book with no beats cannot be read, and offering it would be an invitation to a blank
        // page rather than to the felt pass.
        return rows.Where(b => b.BeatCount > 0).ToList();
    }

    /// <summary>
    /// The whole book in reading order, as prose.
    /// </summary>
    /// <remarks>
    /// Loaded in one pass rather than a beat at a time. A read is continuous by definition, and a
    /// spinner between beats is precisely the interruption this is trying not to introduce.
    /// </remarks>
    public async Task<IReadOnlyList<Page>> PagesAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);

        var ordered = await workbench.GetOrderedBeatsAsync(bookNodeId, ct);
        if (ordered.Count == 0) return [];

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var nodeIds = ordered.Select(o => o.NodeId).Distinct().ToList();
        var titles = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => nodeIds.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, n => n.Title ?? "", ct);

        return ordered
            .Select(o => new Page(
                o.Beat.Id,
                o.Beat.Number,
                titles.GetValueOrDefault(o.NodeId),
                o.Beat.PlaceName,
                // NarrationText, not the editor's plain text: this is what an ear would get, and
                // the read and the listen must be the same pass or they measure different things.
                NarrationText.Clean(o.Beat.Text ?? "")))
            .Where(p => !string.IsNullOrWhiteSpace(p.Text))
            .ToList();
    }

    /// <summary>
    /// Record that attention died here.
    /// </summary>
    /// <remarks>
    /// <para>Takes no note, no severity and no category, and that is the whole design. Asking the
    /// author WHY turns a measurement into an opinion, and the opinion is available from every
    /// other surface in the application. What is not available anywhere else is the bare fact that
    /// on this pass, at this beat, they stopped.</para>
    ///
    /// <para>Dated in the summary so repeated passes on different days accumulate as separate
    /// rows: one mark is noise, the same beat marked on three passes is the finding.</para>
    /// </remarks>
    public async Task MarkAsync(Guid bookNodeId, Page page, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var slug = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Id == bookNodeId).Select(n => n.Slug).FirstOrDefaultAsync(ct) ?? "";

        findings.Upsert(
            filePath: $"{slug}#{page.Number}",
            chapterId: page.ChapterTitle,
            category: FindingCategory.ReadDeadness,
            severity: FindingSeverity.Medium,
            summary: $"Attention died at beat #{page.Number} on {DateTime.UtcNow:yyyy-MM-dd}.",
            snippet: Shorten(page.Text, 300),
            suggestedFix: null);
    }

    /// <summary>How many times each beat of this book has been marked, so a second pass can see
    /// where the first one died — that is the point of recording them.</summary>
    public async Task<IReadOnlyDictionary<int, int>> MarksByBeatAsync(
        Guid bookNodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var slug = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Id == bookNodeId).Select(n => n.Slug).FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(slug)) return new Dictionary<int, int>();

        var prefix = slug + "#";
        var rows = await db.Findings.AsNoTracking()
            .Where(f => f.Category == nameof(FindingCategory.ReadDeadness)
                        && f.FilePath.StartsWith(prefix))
            .Select(f => f.FilePath)
            .ToListAsync(ct);

        var counts = new Dictionary<int, int>();
        foreach (var path in rows)
            if (int.TryParse(path[prefix.Length..], out var number))
                counts[number] = counts.GetValueOrDefault(number) + 1;
        return counts;
    }

    // NOT async, deliberately. The flow universe is an AsyncLocal, and a value set inside an async
    // method is discarded when that method returns to its caller — so the old async version pinned
    // nothing, and every read after "await ScopeToBookAsync(...)" ran in the Hub's default
    // universe (empty or wrong rows for any book outside it). A synchronous method shares its
    // caller's execution context, so the assignment survives. One single-row lookup.
    private Task ScopeToBookAsync(Guid bookNodeId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var db = dbFactory.CreateDbContext();
        var universeId = db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Id == bookNodeId)
            .Select(n => (Guid?)n.UniverseId)
            .FirstOrDefault();
        if (universeId is { } id && id != Guid.Empty) universe.SetFlowUniverse(id);
        return Task.CompletedTask;
    }

    private static string Shorten(string s, int max)
        => s.Length <= max ? s : s[..max] + "…";
}
