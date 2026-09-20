using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// RFC 0007 "Universe Interchange" §5 — the Hub's outbound message queue toward other
/// MindAttic apps' Claude Code sessions. A consumer repo's <c>UserPromptSubmit</c> hook
/// drains <c>GET /api/outbox/{consumer}</c> on every prompt and injects pending summaries
/// as context, so Prose can proactively tell (e.g.) the ExperimentEve session "GDD chapter 3
/// drafted — pull barks" without a human relaying it. Not universe-scoped: <see cref="OutboxEvent.Consumer"/>
/// is an external app identity, not a Prose Universe row.
/// </summary>
public class OutboxService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    public OutboxService(IDbContextFactory<ProseDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    public async Task<OutboxEvent> EnqueueAsync(string consumer, string kind, string summary, object? data = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var ev = new OutboxEvent
        {
            Consumer = consumer.Trim(),
            Kind = kind.Trim(),
            Summary = summary,
            DataJson = data == null ? null : JsonSerializer.Serialize(data),
        };
        db.OutboxEvents.Add(ev);
        await db.SaveChangesAsync(ct);
        return ev;
    }

    /// <summary>Returns pending events for <paramref name="consumer"/>. Unless <paramref name="peek"/>
    /// is true, marks every returned row delivered (the queue itself stays a durable log — rows are
    /// never deleted, just stamped).</summary>
    public async Task<List<OutboxEvent>> DrainAsync(string consumer, bool peek = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var pending = await db.OutboxEvents
            .Where(e => e.Consumer == consumer && e.DeliveredTs == null)
            .OrderBy(e => e.Ts)
            .ToListAsync(ct);

        if (!peek && pending.Count > 0)
        {
            var now = DateTime.UtcNow;
            foreach (var e in pending) e.DeliveredTs = now;
            await db.SaveChangesAsync(ct);
        }

        return pending;
    }
}
