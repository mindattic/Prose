using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Makes a book's numbered chapters run consecutively in reading order.
///
/// <para>Numbers drift out of step whenever a chapter is inserted, removed or reordered — most
/// commonly when unnumbered units (an interlude, a coda, a named strand entry) are slotted between
/// numbered ones and consume their positions. The reader then meets "Chapter 3", an interlude, and
/// then "Chapter 5", and reasonably wonders what happened to Chapter 4.</para>
///
/// <para>Only nodes whose title is <c>Chapter N — Title</c> are touched, and only their number.
/// Unnumbered units keep their titles exactly, and keep their place in the running order: the point
/// is that the numbered sequence a reader follows has no holes, not that everything gets a number.
/// Nothing here reads or writes prose.</para>
/// </summary>
public class ChapterRenumberService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<ChapterRenumberService> log)
{
    /// <summary>
    /// <c>Chapter 12 — The Interview</c>. Accepts an em dash, en dash, hyphen or colon as the
    /// separator so a title that drifted from the house style is still recognised as numbered
    /// rather than silently skipped.
    /// </summary>
    private static readonly Regex NumberedChapter =
        new(@"^\s*Chapter\s+(?<num>\d+)\s*(?<sep>[—–\-:])\s*(?<title>.+?)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public sealed record Rename(Guid NodeId, int Position, string OldTitle, string NewTitle)
    {
        public bool IsChange => !string.Equals(OldTitle, NewTitle, StringComparison.Ordinal);
    }

    public sealed record RenumberReport(
        Guid BookId, string BookTitle, IReadOnlyList<Rename> Numbered,
        IReadOnlyList<string> Unnumbered, bool Applied)
    {
        public int Changes => Numbered.Count(r => r.IsChange);
    }

    public async Task<RenumberReport> RenumberAsync(
        Guid bookId, bool apply = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var book = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Id == bookId, ct)
            ?? throw new InvalidOperationException($"Node not found: {bookId}");

        // The book's own children in reading order — the units a reader meets. Not a leaf walk: a
        // chapter that has been given scenes is still one chapter to the reader.
        var children = await db.Nodes.IgnoreQueryFilters()
            .Where(n => n.ParentNodeId == bookId)
            .OrderBy(n => n.SortKey)
            .ToListAsync(ct);

        var renames = new List<Rename>();
        var unnumbered = new List<string>();
        var next = 1;

        for (var i = 0; i < children.Count; i++)
        {
            var node = children[i];
            var m = NumberedChapter.Match(node.Title ?? "");
            if (!m.Success)
            {
                unnumbered.Add(node.Title ?? "(untitled)");
                continue;
            }

            // House style is an em dash, whatever the original separator was.
            var newTitle = $"Chapter {next} — {m.Groups["title"].Value}";
            renames.Add(new Rename(node.Id, i + 1, node.Title ?? "", newTitle));
            next++;
        }

        if (apply)
        {
            foreach (var r in renames.Where(r => r.IsChange))
            {
                var node = children.First(c => c.Id == r.NodeId);
                node.Title = r.NewTitle;
                log.LogInformation("[renumber-chapters] '{Old}' → '{New}'", r.OldTitle, r.NewTitle);
            }
            await db.SaveChangesAsync(ct);
        }

        return new RenumberReport(book.Id, book.Title ?? "", renames, unnumbered, apply);
    }
}
