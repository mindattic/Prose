using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>The outcome of one maintenance command: whether it wrote, and what it did or would do.</summary>
public sealed record MaintenanceResult(bool Ok, bool Applied, string Message);

/// <summary>
/// Corrections to world rows that no record writer reaches: an entity's slug, an edge's target and
/// wording, an orphaned tag name, a retired narrative obligation. Each is dry-run unless
/// <c>apply</c>, takes explicit ids (no fuzzy matching), and reads the row back after writing.
///
/// <para><b>Why.</b> Retiring the name "The Narrows" (2026-10-03) left one edge pointing at the
/// wrong place with the old name in its wording, two tag names nothing used, a stale obligation
/// note, and one slug that a bad write had regenerated. Each needed a raw SQL statement, which the
/// project forbids (SS-LAW-1, protocol law 2).</para>
/// </summary>
public sealed class CanonMaintenanceService(IDbContextFactory<ProseDbContext> dbFactory)
{
    private static readonly Regex SlugShape = new(@"^[a-z0-9][a-z0-9_\-]*$", RegexOptions.CultureInvariant);

    /// <summary>Sets an entity's slug. The slug must be lower-case letters, digits, '-' or '_', and
    /// unique among the entities of the same type in the same universe.</summary>
    public async Task<MaintenanceResult> SetEntitySlugAsync(Guid entityId, string slug, bool apply, CancellationToken ct = default)
    {
        slug = (slug ?? "").Trim();
        if (!SlugShape.IsMatch(slug)) return new(false, false, $"'{slug}' is not a slug (lower-case letters, digits, '-' and '_').");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.Entities.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == entityId, ct);
        if (row == null) return new(false, false, $"no entity with id {entityId}.");
        if (row.Slug == slug) return new(true, false, $"{row.Name} already has slug '{slug}'; nothing to do.");
        var taken = await db.Entities.IgnoreQueryFilters().AnyAsync(e =>
            e.UniverseId == row.UniverseId && e.EntityType == row.EntityType && e.Slug == slug && e.Id != entityId, ct);
        if (taken) return new(false, false, $"slug '{slug}' is already used by another {row.EntityType} in this universe.");
        var plan = $"{row.Name} ({row.EntityType}): slug '{row.Slug}' → '{slug}'";
        if (!apply) return new(true, false, "DRY RUN — " + plan);
        row.Slug = slug;
        row.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        var back = await ReadBackAsync(db2 => db2.Entities.IgnoreQueryFilters().Where(e => e.Id == entityId).Select(e => e.Slug).FirstOrDefaultAsync(ct));
        return back == slug ? new(true, true, plan) : new(false, true, $"wrote, but the slug read back '{back}'.");
    }

    /// <summary>Sets the display name on an entity row (Entities.Name) and nothing else: no slug, no
    /// record field. For a row whose name a bad save overwrote; a real rename that the prose must
    /// follow is <c>--rename-entity</c> or the record's own name field.</summary>
    public async Task<MaintenanceResult> SetEntityNameAsync(Guid entityId, string name, bool apply, CancellationToken ct = default)
    {
        name = (name ?? "").Trim();
        if (name.Length == 0) return new(false, false, "a name is required.");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.Entities.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == entityId, ct);
        if (row == null) return new(false, false, $"no entity with id {entityId}.");
        if (row.Name == name) return new(true, false, $"{entityId} is already named '{name}'; nothing to do.");
        var plan = $"{row.EntityType} {entityId}: name '{(row.Name.Length > 80 ? row.Name[..80] + "…" : row.Name)}' → '{name}'";
        if (!apply) return new(true, false, "DRY RUN — " + plan);
        row.Name = name;
        row.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        var back = await ReadBackAsync(db2 => db2.Entities.IgnoreQueryFilters().Where(e => e.Id == entityId).Select(e => e.Name).FirstOrDefaultAsync(ct));
        return back == name ? new(true, true, plan) : new(false, true, $"wrote, but the name read back '{back}'.");
    }

    /// <summary>Points a live edge at another entity and/or rewrites its description. The new target
    /// must exist in the edge's universe; an invalidated edge is refused.</summary>
    public async Task<MaintenanceResult> EditEdgeAsync(long edgeId, Guid? target, string? description, bool apply, CancellationToken ct = default)
    {
        if (target == null && description == null) return new(false, false, "nothing to change: pass a target, a description, or both.");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var edge = await db.Edges.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == edgeId, ct);
        if (edge == null) return new(false, false, $"no edge with id {edgeId}.");
        if (edge.InvalidatedAt != null) return new(false, false, $"edge {edgeId} is invalidated; edit a live edge.");
        if (target is { } t)
        {
            var targetUniverse = await db.Entities.IgnoreQueryFilters().Where(e => e.Id == t).Select(e => (Guid?)e.UniverseId).FirstOrDefaultAsync(ct);
            if (targetUniverse == null) return new(false, false, $"no entity with id {t}.");
            if (targetUniverse != edge.UniverseId) return new(false, false, $"entity {t} is in another universe than edge {edgeId}.");
            if (t == edge.SourceId) return new(false, false, "an edge cannot point at its own source.");
        }
        var parts = new List<string>();
        if (target is { } nt && nt != edge.TargetId) parts.Add($"target {edge.TargetId} → {nt}");
        var newDescription = description == null ? edge.Description : (description.Trim().Length == 0 ? null : description.Trim());
        if (description != null && newDescription != edge.Description) parts.Add($"description \"{edge.Description}\" → \"{newDescription}\"");
        if (parts.Count == 0) return new(true, false, $"edge {edgeId} already reads that way; nothing to do.");
        var plan = $"edge {edgeId} ({edge.RelationType}): " + string.Join("; ", parts);
        if (!apply) return new(true, false, "DRY RUN — " + plan);
        if (target is { } nt2) edge.TargetId = nt2;
        edge.Description = newDescription;
        await db.SaveChangesAsync(ct);
        var back = await ReadBackAsync(db2 => db2.Edges.IgnoreQueryFilters().Where(e => e.Id == edgeId)
            .Select(e => new { e.TargetId, e.Description }).FirstOrDefaultAsync(ct));
        var ok = back != null && back.TargetId == edge.TargetId && back.Description == newDescription;
        return ok ? new(true, true, plan) : new(false, true, "wrote, but the edge read back different.");
    }

    /// <summary>Deletes a tag name that no entity carries and no entity owns. A tag still in use is
    /// refused: take it off its entities first (<c>--entity-tags --remove</c>).</summary>
    public async Task<MaintenanceResult> PruneOrphanTagAsync(string name, bool apply, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return new(false, false, "a tag name is required.");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var tags = await db.Tags.Where(t => t.Name == name).ToListAsync(ct);
        if (tags.Count == 0) return new(false, false, $"no tag named '{name}'.");
        var ids = tags.Select(t => t.Id).ToList();
        var used = await db.EntityTags.CountAsync(et => ids.Contains(et.TagId), ct);
        if (used > 0) return new(false, false, $"tag '{name}' is still on {used} entit{(used == 1 ? "y" : "ies")}; remove it there first.");
        if (tags.Any(t => t.EntityId != null)) return new(false, false, $"tag '{name}' names a canonical entity; it is not an orphan.");
        var plan = $"delete tag '{name}' (id {string.Join(", ", ids)}), carried by no entity";
        if (!apply) return new(true, false, "DRY RUN — " + plan);
        db.Tags.RemoveRange(tags);
        await db.SaveChangesAsync(ct);
        var left = await ReadBackAsync(db2 => db2.Tags.CountAsync(t => t.Name == name, ct));
        return left == 0 ? new(true, true, plan) : new(false, true, "deleted, but the tag still reads back.");
    }

    /// <summary>Deletes one narrative obligation and its journal. Obligations belong to the retired
    /// RFC 0013 pipeline (RFC 0015: nothing is stored about the story but its beats and entities), so
    /// a stale one is removed rather than edited. The dry run prints the row in full, which the
    /// command ledger keeps.</summary>
    public async Task<MaintenanceResult> DeleteObligationAsync(Guid obligationId, bool apply, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.NarrativeObligations.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == obligationId, ct);
        if (row == null) return new(false, false, $"no obligation with id {obligationId}.");
        var events = await db.NarrativeObligationEvents.Where(e => e.ObligationId == obligationId).ToListAsync(ct);
        var plan = $"delete obligation {row.Id} (node {row.NodeId}, {row.Kind}, state {row.State}): \"{row.Description}\" and {events.Count} journal event(s)";
        if (!apply) return new(true, false, "DRY RUN — " + plan);
        db.NarrativeObligationEvents.RemoveRange(events);
        db.NarrativeObligations.Remove(row);
        await db.SaveChangesAsync(ct);
        var left = await ReadBackAsync(db2 => db2.NarrativeObligations.IgnoreQueryFilters().CountAsync(o => o.Id == obligationId, ct));
        return left == 0 ? new(true, true, plan) : new(false, true, "deleted, but the obligation still reads back.");
    }

    private async Task<T> ReadBackAsync<T>(Func<ProseDbContext, Task<T>> read)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await read(db);
    }
}
