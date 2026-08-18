using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// FK-safety gate for a plain hard-delete of an <c>Entities</c> row with no replacement winner
/// to redirect references to (contrast <see cref="DuplicateEntityScanService.MergeAsync"/>,
/// which always has a winner and relinks everything before deleting). Blocks with a clear,
/// enumerated error rather than silently cascading past a <c>Restrict</c>/<c>NO_ACTION</c> FK —
/// this matches the schema's own existing design (every cross-reference to an arbitrary other
/// entity is already NO_ACTION; only an entity's own private children — properties, tags,
/// embeddings, state events, beat mentions — cascade). No status flag involved: the row either
/// has no blocking dependents and can be deleted outright, or it does and the caller is told
/// exactly which table/column/count is in the way.
/// </summary>
public static class EntityDeleteGuard
{
    public sealed record BlockingReference(string Table, string Column, int Count);

    public static async Task<List<BlockingReference>> CheckBlockingReferencesAsync(
        ProseDbContext db, Guid entityId, CancellationToken ct = default)
    {
        var fks = await EntityForeignKeyCatalog.DiscoverAsync(db, ct);
        var blockers = new List<BlockingReference>();

        foreach (var fk in fks.Where(f => !f.Cascades))
        {
            var count = await db.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(*) AS Value FROM [dbo].[{fk.Table}] WHERE [{fk.Column}] = @id",
                new SqlParameter("@id", entityId)).SingleAsync(ct);
            if (count > 0) blockers.Add(new BlockingReference(fk.Table, fk.Column, count));
        }

        return blockers;
    }

    /// <summary>Builds the message for the exception a blocked delete should throw — one place
    /// so the wording stays consistent between <see cref="Data.EfRepository{T}"/> and any future
    /// caller (BookRepository/ChapterRepository use their own transactional multi-table delete,
    /// not this guard, since a book/chapter's own subtype rows aren't FK'd to Entities at all).</summary>
    public static string DescribeBlockers(string entityLabel, Guid entityId, IReadOnlyList<BlockingReference> blockers) =>
        $"Cannot delete '{entityLabel}' ({entityId}): still referenced by " +
        string.Join(", ", blockers.Select(b => $"{b.Table}.{b.Column} ({b.Count} row(s))")) +
        ". Relink or remove those references first, or use MergeAsync if there's a replacement entity to redirect them to.";
}
