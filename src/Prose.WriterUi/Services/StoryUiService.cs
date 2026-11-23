using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.ReadAlong;

namespace Prose.WriterUi.Services;

/// <summary>
/// The Story workspace's own plumbing (RFC 0019): the book list, the manuscript tree (volumes and
/// chapters, reordered by dragging), adding and renaming chapters, and the Word export. The
/// read-along itself lives in Core (<see cref="ReadAlongService"/> and friends); this is only what
/// the page needs around it. Scoped per circuit, like every service in this library, because it
/// pins the flow universe to the book the page has open.
/// </summary>
public sealed class StoryUiService(
    IDbContextFactory<ProseDbContext> dbFactory,
    NodeWorkbenchService workbench,
    DocxExportService docx,
    IUniverseContext universe)
{
    public sealed record BookItem(Guid Id, string Slug, string Title, string? Code, Guid UniverseId);

    public sealed record UniverseItem(Guid Id, string Slug, string Name);

    public sealed record TreeNode(Guid Id, string Title, Guid? ParentId, double SortKey, bool IsVolume, int Depth);

    public async Task<List<BookItem>> ListBooksAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.BookNodes.AsNoTracking().IgnoreQueryFilters().Where(b => !b.IsArchived)
            .OrderBy(b => b.Title)
            .Select(b => new BookItem(b.Id, b.Slug, b.Title, b.NodeCode, b.UniverseId))
            .ToListAsync(ct);
    }

    public async Task<List<UniverseItem>> ListUniversesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Universes.AsNoTracking().OrderBy(u => u.Name).Select(u => new UniverseItem(u.Id, u.Slug, u.Name)).ToListAsync(ct);
    }

    /// <summary>Pin the ambient universe for this circuit's flow (synchronous on purpose — see
    /// WriterService.ScopeToBookAsync: an AsyncLocal set inside an async method does not survive
    /// the return).</summary>
    public void Pin(Guid universeId)
    {
        if (universeId != Guid.Empty) universe.SetFlowUniverse(universeId);
    }

    /// <summary>The book's volumes and chapters (not scenes) in reading order.</summary>
    public async Task<List<TreeNode>> GetTreeAsync(Guid bookId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var all = new List<TreeNode>();
        async Task Walk(Guid parent, int depth)
        {
            var kids = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                .Where(n => n.ParentNodeId == parent && n.Kind != "book")
                .OrderBy(n => n.SortKey).ThenBy(n => n.Id)
                .Select(n => new { n.Id, n.Title, n.ParentNodeId, n.SortKey, IsChapter = n is ChapterNode })
                .ToListAsync(ct);
            foreach (var k in kids.Where(k => k.IsChapter))
            {
                var hasChapterKids = await db.Nodes.IgnoreQueryFilters().AnyAsync(n => n.ParentNodeId == k.Id && n is ChapterNode, ct);
                all.Add(new TreeNode(k.Id, k.Title, k.ParentNodeId, k.SortKey, hasChapterKids, depth));
                if (hasChapterKids && depth < 4) await Walk(k.Id, depth + 1);
            }
        }
        await Walk(bookId, 0);
        return all;
    }

    /// <summary>Drop <paramref name="nodeId"/> just before <paramref name="targetId"/> (same parent
    /// as the target), or into <paramref name="targetId"/> when it is a volume and
    /// <paramref name="into"/> is set. Only the tree moves; no beat is touched.</summary>
    public async Task MoveAsync(Guid nodeId, Guid targetId, bool into = false, CancellationToken ct = default)
    {
        if (nodeId == targetId) return;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var target = await db.Nodes.AsNoTracking().IgnoreQueryFilters().FirstAsync(n => n.Id == targetId, ct);
        if (into)
        {
            var max = await db.Nodes.IgnoreQueryFilters().Where(n => n.ParentNodeId == targetId).Select(n => (double?)n.SortKey).MaxAsync(ct) ?? 0;
            await workbench.ReparentNodeAsync(nodeId, targetId, max + 100, ct);
            return;
        }
        var prev = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.ParentNodeId == target.ParentNodeId && n.SortKey < target.SortKey && n.Id != nodeId)
            .OrderByDescending(n => n.SortKey).Select(n => (double?)n.SortKey).FirstOrDefaultAsync(ct);
        var key = prev is { } p ? (p + target.SortKey) / 2 : target.SortKey - 100;
        await workbench.ReparentNodeAsync(nodeId, target.ParentNodeId, key, ct);
    }

    /// <summary>Move a node to the end of the book (or of a volume).</summary>
    public async Task MoveToEndAsync(Guid nodeId, Guid parentId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var max = await db.Nodes.IgnoreQueryFilters().Where(n => n.ParentNodeId == parentId && n.Id != nodeId).Select(n => (double?)n.SortKey).MaxAsync(ct) ?? 0;
        await workbench.ReparentNodeAsync(nodeId, parentId, max + 100, ct);
    }

    public async Task<Guid> AddChapterAsync(Guid parentId, string title, CancellationToken ct = default)
    {
        var (id, _) = await workbench.CreateNodeAsync(string.IsNullOrWhiteSpace(title) ? "New chapter" : title.Trim(), kind: "chapter", parentNodeId: parentId, ct: ct);
        // A chapter is written in the Writer; give it its first (empty) beat so it opens there.
        await workbench.InsertBeatAsync(id, null, "", ct);
        return id;
    }

    public async Task RenameAsync(Guid nodeId, string title, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title)) return;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var n = await db.Nodes.IgnoreQueryFilters().FirstAsync(x => x.Id == nodeId, ct);
        n.Title = title.Trim();
        n.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>The whole book to Word, formatting intact. Returns the file path the Hub serves.</summary>
    public Task<string> ExportDocxAsync(Guid bookId, CancellationToken ct = default) => docx.ExportNodeAsync(bookId, author: null, ct);
}
