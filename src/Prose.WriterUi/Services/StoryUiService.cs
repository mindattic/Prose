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

    /// <param name="Kind">volume (a chapter holding chapters) | chapter | sequence | scene.</param>
    public sealed record TreeNode(Guid Id, string Title, Guid? ParentId, double SortKey, bool IsVolume, int Depth, string Kind = "chapter")
    {
        public bool IsScene => Kind is "scene" or "sequence";
    }

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

    /// <summary>The book's volumes, chapters, sequences and scenes in reading order.</summary>
    public async Task<List<TreeNode>> GetTreeAsync(Guid bookId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await WalkAsync(db, bookId, ct);
    }

    /// <summary>The tree walk, separate so it can be tested on any ProseDbContext (RA-T2.3.1.1).
    /// Book-kind children are draft buckets and are skipped, as every reading-order walk does.</summary>
    public static async Task<List<TreeNode>> WalkAsync(ProseDbContext db, Guid bookId, CancellationToken ct = default)
    {
        var all = new List<TreeNode>();
        async Task Walk(Guid parent, int depth)
        {
            if (depth > 6) return;
            var kids = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                .Where(n => n.ParentNodeId == parent && n.Kind != "book")
                .OrderBy(n => n.SortKey).ThenBy(n => n.Id)
                .ToListAsync(ct);
            foreach (var k in kids)
            {
                if (k is not (ChapterNode or SceneNode or SequenceNode)) continue;
                var hasChapterKids = k is ChapterNode && await db.Nodes.IgnoreQueryFilters().AnyAsync(n => n.ParentNodeId == k.Id && n is ChapterNode, ct);
                var kind = k switch
                {
                    SceneNode => "scene",
                    SequenceNode => "sequence",
                    _ => hasChapterKids ? "volume" : "chapter",
                };
                all.Add(new TreeNode(k.Id, k.Title, k.ParentNodeId, k.SortKey, hasChapterKids, depth, kind));
                await Walk(k.Id, depth + 1);
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

    public Task<Guid> AddChapterAsync(Guid parentId, string title, CancellationToken ct = default) =>
        AddNodeAsync(parentId, title, "chapter", ct);

    /// <summary>A new chapter or scene (kind "chapter" | "scene") at the end of its parent, with one
    /// empty beat so it opens in the Writer.</summary>
    public async Task<Guid> AddNodeAsync(Guid parentId, string title, string kind, CancellationToken ct = default)
    {
        kind = kind == "scene" ? "scene" : "chapter";
        var (id, _) = await workbench.CreateNodeAsync(string.IsNullOrWhiteSpace(title) ? (kind == "scene" ? "New scene" : "New chapter") : title.Trim(),
            kind: kind, parentNodeId: parentId, ct: ct);
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
