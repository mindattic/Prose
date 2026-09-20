using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.WriterUi.Services;

/// <summary>
/// The findings inbox, in the editor.
///
/// <para><b>Why this exists.</b> Every instrument in Prose writes findings — the logic sweep the
/// editor itself kicks off on every finished beat, the fact ledger, Read Mode's deadness marks —
/// and until now nothing in the Writer read a single one of them back. The author was paying for
/// analysis whose output they could only see by running a CLI command in another window, which
/// is the same as not having it.</para>
///
/// <para>Scoped, for the flow-universe pin, like the other UI services.</para>
/// </summary>
public sealed class FindingsUiService(
    IDbContextFactory<ProseDbContext> dbFactory,
    IUniverseContext universe,
    FindingsService findings)
{
    /// <param name="BeatId">Set when the finding names a beat in its path — the convention is
    /// <c>node:{slug}/beat:{guid}</c>. Null for a book-level finding, which cannot be jumped to.</param>
    /// <param name="BeatNumber">Set for a Read Mode mark, whose path is <c>{slug}#{number}</c>.</param>
    public sealed record Row(
        long Id,
        string Category,
        string Severity,
        string Summary,
        string? Snippet,
        DateTime DetectedAt,
        Guid? BeatId,
        int? BeatNumber);

    /// <summary>
    /// Everything still untriaged for the open book.
    /// </summary>
    /// <remarks>
    /// Scoped by path prefix rather than listed globally. Without that, a book's own Medium
    /// findings are invisible in practice the moment the corpus-wide High backlog exceeds the
    /// limit — which is exactly the failure that made this data feel absent even to the tools
    /// that did read it.
    /// </remarks>
    public async Task<IReadOnlyList<Row>> ForBookAsync(
        Guid bookNodeId, bool includeTriaged = false, CancellationToken ct = default)
    {
        var slug = await SlugAsync(bookNodeId, ct);
        if (string.IsNullOrEmpty(slug)) return [];

        var rows = new List<Row>();
        foreach (var status in includeTriaged
                     ? new[] { FindingStatus.New, FindingStatus.Triaged }
                     : [FindingStatus.New])
        {
            // Two prefixes: instruments key findings "node:{slug}", Read Mode keys its marks
            // "{slug}#{beat}". Both belong to this book and both belong in one list.
            foreach (var prefix in new[] { $"node:{slug}", slug + "#" })
                rows.AddRange(findings.List(status, limit: 200, filePathPrefix: prefix)
                                      .Select(Map));
        }

        return rows
            .DistinctBy(r => r.Id)
            .OrderBy(r => r.Severity == "High" ? 0 : r.Severity == "Medium" ? 1 : 2)
            .ThenByDescending(r => r.DetectedAt)
            .ToList();
    }

    /// <summary>Triaged, not deleted: a finding the author has looked at and left alone is a
    /// different thing from one that was never read, and the inbox has to be able to tell.</summary>
    public void Triage(long id) => findings.SetStatus(id, FindingStatus.Triaged);

    public void Dismiss(long id) => findings.SetStatus(id, FindingStatus.Dismissed);

    private static Row Map(Finding f)
    {
        Guid? beatId = null;
        int? number = null;

        var at = f.FilePath.IndexOf("/beat:", StringComparison.Ordinal);
        if (at >= 0 && Guid.TryParse(f.FilePath[(at + 6)..], out var id)) beatId = id;

        var hash = f.FilePath.LastIndexOf('#');
        if (hash >= 0 && int.TryParse(f.FilePath[(hash + 1)..], out var n)) number = n;

        return new Row(f.Id, f.Category.ToString(), f.Severity.ToString(),
                       f.Summary, f.Snippet, f.DetectedAt, beatId, number);
    }

    private async Task<string?> SlugAsync(Guid bookNodeId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Id == bookNodeId)
            .Select(n => new { n.Slug, n.UniverseId })
            .FirstOrDefaultAsync(ct);
        if (row is null) return null;
        if (row.UniverseId != Guid.Empty) universe.SetFlowUniverse(row.UniverseId);
        return row.Slug;
    }
}
