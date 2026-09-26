using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Rename a node (series, book, chapter, scene): its Title and, optionally, its Slug.
///
/// <para>The slug half is <see cref="SlugRepairService.SetNodeSlugAsync"/> — the one supported
/// slug write — so a new slug is pinned and every slug-carrying reference (beat audio paths,
/// publication paths, on-disk directories) moves with it. Both halves are validated before
/// anything is written: an empty or over-long title, a slug that is not slug-shaped, or one
/// already taken in the universe is refused, and nothing changes.</para>
///
/// <para>MCP: <c>rename_node</c>. CLI: <c>prose --rename-node --node &lt;id|slug&gt; --title "…" [--slug …]</c>.
/// Every call through either door lands in the Hub's command ledger; the service logs the change.</para>
/// </summary>
public sealed class NodeRenameService(
    IDbContextFactory<ProseDbContext> dbFactory,
    SlugRepairService slugs,
    ILogger<NodeRenameService> log)
{
    public const int MaxTitleLength = 400; // Nodes.Title column

    public sealed record RenameResult(Guid Id, string Kind, string OldTitle, string NewTitle, string OldSlug, string NewSlug, IReadOnlyList<string> SlugSideEffects);

    /// <exception cref="InvalidOperationException">The node does not exist, or the title or slug is refused.</exception>
    public async Task<RenameResult> RenameAsync(Guid nodeId, string title, string? newSlug = null, CancellationToken ct = default)
    {
        var t = (title ?? "").Trim();
        if (t.Length == 0) throw new InvalidOperationException("A title is required.");
        if (t.Length > MaxTitleLength) throw new InvalidOperationException($"title is {t.Length} characters; the limit is {MaxTitleLength}.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // Explicit id, not ambient scope: IgnoreQueryFilters (2026-08-17 convention).
        var node = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
                   ?? throw new InvalidOperationException($"No node with id {nodeId}.");
        var oldTitle = node.Title;
        var oldSlug = node.Slug;

        var wantsSlug = !string.IsNullOrWhiteSpace(newSlug);
        // Dry-run first: a refused slug must leave the title alone too.
        if (wantsSlug) await slugs.SetNodeSlugAsync(nodeId, newSlug!, apply: false, ct);

        node.Title = t;
        node.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        IReadOnlyList<string> effects = [];
        var slugNow = oldSlug;
        if (wantsSlug)
        {
            var change = await slugs.SetNodeSlugAsync(nodeId, newSlug!, apply: true, ct);
            effects = change.SideEffects;
            slugNow = change.NewSlug;
        }

        log.LogInformation("Rename node {Id} ({Kind}): \"{OldTitle}\" → \"{NewTitle}\"; slug {OldSlug} → {NewSlug}",
            node.Id, node.Kind, oldTitle, t, oldSlug, slugNow);
        return new RenameResult(node.Id, node.Kind, oldTitle, t, oldSlug, slugNow, effects);
    }
}
