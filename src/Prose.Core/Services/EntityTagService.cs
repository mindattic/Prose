using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// List, add and REMOVE an entity's tags (the <c>EntityTags</c> bridge).
///
/// <para><b>Why this exists (2026-09-03).</b> Tags could be added and never taken away. The only
/// write path was <c>create_character</c>'s <c>tags</c> parameter, and — like <c>aliases</c> —
/// <c>CharacterMapper</c> merges rather than replaces, so passing a shorter list silently keeps
/// the old entries. <c>--tag-entities</c> is a different thing entirely (inline entity-GUID
/// tagging inside beat text). So a wrong tag was permanent. Found when Kressida Haun, cut from
/// VIGL on 2026-07-22, could not have her <c>vigl</c> tag removed — a stale book tag is not
/// cosmetic, it can pull a character into that book's context loads.</para>
///
/// <para>Removes the <c>EntityTags</c> row only. The <c>Tags</c> row itself is shared vocabulary
/// and is left alone — deleting it would strip the tag from every other entity using it.</para>
/// </summary>
public class EntityTagService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<EntityTagService> log;

    public EntityTagService(IDbContextFactory<ProseDbContext> dbFactory, ILogger<EntityTagService> log)
    {
        this.dbFactory = dbFactory;
        this.log = log;
    }

    /// <summary>Every tag name on this entity, alphabetical.</summary>
    public async Task<List<string>> ListAsync(Guid entityId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.EntityTags.AsNoTracking()
            .Where(et => et.EntityId == entityId)
            .Join(db.Tags.AsNoTracking(), et => et.TagId, t => t.Id, (et, t) => t.Name)
            .OrderBy(n => n)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Remove the named tags from one entity. Case-insensitive. Returns the names actually
    /// removed — a name the entity did not carry is simply absent from the result rather than an
    /// error, so removing the same list twice is safe.
    /// </summary>
    public async Task<List<string>> RemoveAsync(Guid entityId, IReadOnlyCollection<string> tagNames, CancellationToken ct = default)
    {
        if (tagNames.Count == 0) return new();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var wanted = tagNames.Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
        var rows = await db.EntityTags
            .Where(et => et.EntityId == entityId)
            .Join(db.Tags, et => et.TagId, t => t.Id, (et, t) => new { Link = et, t.Name })
            .ToListAsync(ct);

        var doomed = rows
            .Where(r => wanted.Any(w => string.Equals(w, r.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (doomed.Count == 0) return new();

        // Only the bridge row goes. The Tags row is shared vocabulary — deleting it would strip
        // the tag from every other entity that legitimately carries it.
        db.EntityTags.RemoveRange(doomed.Select(d => d.Link));
        await db.SaveChangesAsync(ct);

        var removed = doomed.Select(d => d.Name).OrderBy(n => n).ToList();
        log.LogInformation("Removed {Count} tag(s) from entity {EntityId}: {Tags}",
            removed.Count, entityId, string.Join(", ", removed));
        return removed;
    }

    /// <summary>
    /// Add the named tags to one entity, creating any <c>Tags</c> vocabulary row that does not
    /// exist yet. Returns the names actually added (already-present tags are skipped).
    /// </summary>
    public async Task<List<string>> AddAsync(Guid entityId, IReadOnlyCollection<string> tagNames, CancellationToken ct = default)
    {
        if (tagNames.Count == 0) return new();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var existing = await db.EntityTags.AsNoTracking()
            .Where(et => et.EntityId == entityId)
            .Join(db.Tags.AsNoTracking(), et => et.TagId, t => t.Id, (et, t) => t.Name)
            .ToListAsync(ct);
        var have = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        var added = new List<string>();
        foreach (var raw in tagNames.Select(t => t.Trim()).Where(t => t.Length > 0))
        {
            if (!have.Add(raw)) continue;

            var tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == raw, ct);
            if (tag == null)
            {
                tag = new Tag { Name = raw };
                db.Tags.Add(tag);
                await db.SaveChangesAsync(ct);   // need the generated Id for the bridge row
            }
            db.EntityTags.Add(new EntityTag { EntityId = entityId, TagId = tag.Id });
            added.Add(raw);
        }

        if (added.Count > 0) await db.SaveChangesAsync(ct);
        return added;
    }
}
