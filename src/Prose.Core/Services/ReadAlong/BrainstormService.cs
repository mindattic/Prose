using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.ReadAlong;

/// <summary>The brainstorm canvas (RFC 0019 §7): free cards a book's author places by hand —
/// ideas, questions, plot threads. Never read as canon, never fed to the story bible, never sent to
/// an LLM. Cards can be tied to a story-bible entry or threaded to each other.</summary>
public sealed class BrainstormService(IDbContextFactory<ProseDbContext> dbFactory)
{
    public static readonly string[] Colors = ["plain", "idea", "question", "plot", "character", "world"];

    public sealed record Card(Guid Id, string Title, string Body, double X, double Y, string Color, Guid? EntityId, Guid? LinkedCardId, DateTime UpdatedAt);

    public async Task<List<Card>> ListAsync(Guid bookId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.BrainstormCards.AsNoTracking().IgnoreQueryFilters().Where(c => c.BookNodeId == bookId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new Card(c.Id, c.Title, c.Body, c.X, c.Y, c.Color, c.EntityId, c.LinkedCardId, c.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<Card> AddAsync(Guid bookId, string title, string body = "", double x = 40, double y = 40, string color = "idea", Guid? entityId = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universeId = await db.Nodes.IgnoreQueryFilters().Where(n => n.Id == bookId).Select(n => n.UniverseId).FirstAsync(ct);
        var c = new BrainstormCard
        {
            UniverseId = universeId, BookNodeId = bookId, Title = Clip(title, 200), Body = body ?? "", X = x, Y = y,
            Color = Colors.Contains(color) ? color : "plain", EntityId = entityId,
        };
        db.BrainstormCards.Add(c);
        await db.SaveChangesAsync(ct);
        return new Card(c.Id, c.Title, c.Body, c.X, c.Y, c.Color, c.EntityId, c.LinkedCardId, c.UpdatedAt);
    }

    public async Task<bool> UpdateAsync(Guid id, string? title = null, string? body = null, double? x = null, double? y = null,
        string? color = null, Guid? linkedCardId = null, bool clearLink = false, CancellationToken ct = default,
        Guid? entityId = null, bool clearEntity = false)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var c = await db.BrainstormCards.IgnoreQueryFilters().FirstOrDefaultAsync(x0 => x0.Id == id, ct);
        if (c is null) return false;
        if (title is not null) c.Title = Clip(title, 200);
        if (body is not null) c.Body = body;
        if (x is { } nx) c.X = nx;
        if (y is { } ny) c.Y = ny;
        if (color is not null && Colors.Contains(color)) c.Color = color;
        if (clearEntity) c.EntityId = null;
        else if (entityId is { } eid) c.EntityId = eid;
        if (clearLink) c.LinkedCardId = null;
        else if (linkedCardId is { } l && l != id) c.LinkedCardId = l;
        c.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var c = await db.BrainstormCards.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return false;
        foreach (var other in await db.BrainstormCards.IgnoreQueryFilters().Where(o => o.LinkedCardId == id).ToListAsync(ct))
            other.LinkedCardId = null;
        db.BrainstormCards.Remove(c);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static string Clip(string s, int max) => (s ?? "").Trim() is var t && t.Length > max ? t[..max] : (s ?? "").Trim();
}
