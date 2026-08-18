using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

public record EntityBeatMention(
    string NodeTitle,
    string NodeSlug,
    Guid   NodeId,
    int    BeatNumber,
    string Handle,
    string Excerpt);

public class EntityMentionService(IDbContextFactory<ProseDbContext> dbFactory)
{
    public async Task<List<EntityBeatMention>> GetBeatsForEntityAsync(
        Guid entityId, int limit = 50, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var rows = await db.BeatEntityMentions
            .Where(m => m.EntityId == entityId)
            .Join(db.BeatNodes,
                m  => m.BeatId,
                sb => sb.BeatId,
                (m, sb) => new { m, sb })
            .Join(db.Nodes,
                x => x.sb.NodeId,
                s => s.Id,
                (x, s) => new { x.m, x.sb, Node = s })
            .Join(db.Beats,
                x => x.m.BeatId,
                b => b.Id,
                (x, b) => new { x.Node, x.sb, Beat = b })
            .OrderBy(x => x.Node.Title)
            .ThenBy(x => x.sb.SortKey)
            .Select(x => new
            {
                NodeTitle = x.Node.Title,
                NodeSlug  = x.Node.Slug,
                NodeId    = x.Node.Id,
                BeatNumber  = x.Beat.Number,
                BeatId      = x.Beat.Id,
                Text        = x.Beat.Text,
            })
            .Take(limit)
            .ToListAsync(ct);

        return rows.Select(r => new EntityBeatMention(
            NodeTitle: r.NodeTitle,
            NodeSlug:  r.NodeSlug,
            NodeId:    r.NodeId,
            BeatNumber:  r.BeatNumber,
            Handle:      $"{r.NodeId}.{r.BeatId}",
            Excerpt:     (r.Text?.Length ?? 0) > 120 ? r.Text![..120] + "…" : r.Text ?? ""
        )).ToList();
    }

    public async Task<(Guid Id, string Name)?> ResolveEntityAsync(
        string idOrSlug, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        if (Guid.TryParse(idOrSlug, out var guid))
        {
            var byId = await db.Entities.AsNoTracking()
                .Where(e => e.Id == guid)
                .Select(e => new { e.Id, e.Name })
                .FirstOrDefaultAsync(ct);
            if (byId != null) return (byId.Id, byId.Name);
        }

        var bySlug = await db.Entities.AsNoTracking()
            .Where(e => e.Slug == idOrSlug)
            .Select(e => new { e.Id, e.Name })
            .FirstOrDefaultAsync(ct);
        return bySlug == null ? null : (bySlug.Id, bySlug.Name);
    }
}
