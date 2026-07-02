using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

public record EntityBeatMention(
    string StrandTitle,
    string StrandSlug,
    Guid   StrandId,
    int    BeatNumber,
    string Handle,
    string Excerpt);

public class EntityMentionService(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
{
    public async Task<List<EntityBeatMention>> GetBeatsForEntityAsync(
        Guid entityId, int limit = 50, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var rows = await db.BeatEntityMentions
            .Where(m => m.EntityId == entityId)
            .Join(db.StrandBeats,
                m  => m.BeatId,
                sb => sb.BeatId,
                (m, sb) => new { m, sb })
            .Join(db.Strands,
                x => x.sb.StrandId,
                s => s.Id,
                (x, s) => new { x.m, x.sb, Strand = s })
            .Join(db.Beats,
                x => x.m.BeatId,
                b => b.Id,
                (x, b) => new { x.Strand, x.sb, Beat = b })
            .Where(x => x.sb.IsEnabled)
            .OrderBy(x => x.Strand.Title)
            .ThenBy(x => x.sb.SortKey)
            .Select(x => new
            {
                StrandTitle = x.Strand.Title,
                StrandSlug  = x.Strand.Slug,
                StrandId    = x.Strand.Id,
                BeatNumber  = x.Beat.Number,
                BeatId      = x.Beat.Id,
                Text        = x.Beat.Text,
            })
            .Take(limit)
            .ToListAsync(ct);

        return rows.Select(r => new EntityBeatMention(
            StrandTitle: r.StrandTitle,
            StrandSlug:  r.StrandSlug,
            StrandId:    r.StrandId,
            BeatNumber:  r.BeatNumber,
            Handle:      $"{r.StrandId}.{r.BeatId}",
            Excerpt:     r.Text.Length > 120 ? r.Text[..120] + "…" : r.Text
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
            .Where(e => e.Slug == idOrSlug && e.IsActive)
            .Select(e => new { e.Id, e.Name })
            .FirstOrDefaultAsync(ct);
        return bySlug == null ? null : (bySlug.Id, bySlug.Name);
    }
}
