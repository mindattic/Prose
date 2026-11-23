using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.ReadAlong;

/// <summary>
/// The Continuity Inbox (RFC 0019 §5): every disagreement a book has with itself, one card each,
/// both lines quoted and linked. The author rules; the read-along never decides. Every ruling is
/// kept on the card (status, time, note) so "why does the bible say this?" has an answer.
/// </summary>
public sealed class ContinuityInboxService(IDbContextFactory<ProseDbContext> dbFactory)
{
    public sealed record Door(Guid? BeatId, Guid? ChapterNodeId, string ChapterTitle, string Quote);

    public sealed record Card(
        Guid Id, string EntityName, Guid? EntityId, string Kind, string Category, string Attribute, int Confidence,
        string ExistingValue, Door Existing, string ProposedValue, Door Proposed,
        string Status, string? Note, DateTime CreatedAt, DateTime? ResolvedAt);

    public static readonly string[] Actions = ["accept-new", "keep-existing", "retcon", "dismiss"];

    public async Task<List<Card>> ListAsync(Guid bookId, string? status = ReadAlongStatus.Open, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.ContinuityConflicts.AsNoTracking().IgnoreQueryFilters()
            .Where(c => c.BookNodeId == bookId && (status == null || status == "all" || c.Status == status))
            .OrderByDescending(c => c.Status == ReadAlongStatus.Open).ThenByDescending(c => c.Confidence).ThenBy(c => c.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(ToCard).ToList();
    }

    public async Task<int> CountOpenAsync(Guid bookId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.ContinuityConflicts.IgnoreQueryFilters().CountAsync(c => c.BookNodeId == bookId && c.Status == ReadAlongStatus.Open, ct);
    }

    public async Task<Card?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var c = await db.ContinuityConflicts.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);
        return c is null ? null : ToCard(c);
    }

    private static Card ToCard(ContinuityConflict c) => new(
        c.Id, c.EntityName, c.EntityId, c.Kind, c.Category, c.Attribute, c.Confidence,
        c.ExistingValue, new Door(c.ExistingBeatId, c.ExistingChapterNodeId, c.ExistingChapterTitle, c.ExistingQuote),
        c.ProposedValue, new Door(c.ProposedBeatId, c.ProposedChapterNodeId, c.ProposedChapterTitle, c.ProposedQuote),
        c.Status, c.Note, c.CreatedAt, c.ResolvedAt);

    /// <summary>Why the read-along thinks the two lines disagree. Deterministic: the same card
    /// always explains itself the same way.</summary>
    public async Task<string?> ExplainAsync(Guid id, CancellationToken ct = default)
    {
        var c = await GetAsync(id, ct);
        return c is null ? null : StoryFactExtractor.Explain(c.EntityName, c.Attribute, c.ExistingValue, c.Existing.ChapterTitle,
            c.ProposedValue, c.Proposed.ChapterTitle, c.Kind);
    }

    /// <summary>
    /// Rule on a card.
    /// <list type="bullet">
    /// <item><c>accept-new</c>: the later line is the story. The old fact is superseded; the new
    ///   one is confirmed.</item>
    /// <item><c>keep-existing</c>: the earlier line is the story. The old fact is confirmed (and so
    ///   protected); the new observation is dropped. The prose that disagrees is the author's to fix.</item>
    /// <item><c>retcon</c>: the change is deliberate. The new value becomes canon and the old one
    ///   stays on the entry's timeline as retconned history.</item>
    /// <item><c>dismiss</c>: both lines are right in context. Nothing changes; the card stays closed
    ///   (its fingerprint keeps it from being raised again).</item>
    /// </list>
    /// </summary>
    public async Task<(bool Ok, string? Error)> ResolveAsync(Guid id, string action, string? note = null, CancellationToken ct = default)
    {
        action = (action ?? "").Trim().ToLowerInvariant();
        if (!Actions.Contains(action)) return (false, $"action must be one of: {string.Join(", ", Actions)}");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var c = await db.ContinuityConflicts.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return (false, "card not found");
        if (c.Status != ReadAlongStatus.Open) return (false, $"already ruled: {c.Status}");

        var existing = c.ExistingFactId is { } efid ? await db.StoryFacts.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == efid, ct) : null;
        var proposed = await db.StoryFacts.IgnoreQueryFilters()
            .Where(f => f.BookNodeId == c.BookNodeId && f.Attribute == c.Attribute && f.Value == c.ProposedValue
                        && f.BeatId == c.ProposedBeatId && f.SupersededAt == null)
            .FirstOrDefaultAsync(ct);
        var now = DateTime.UtcNow;

        switch (action)
        {
            case "accept-new":
            case "retcon":
                if (existing is not null) existing.SupersededAt = now;
                if (proposed is not null)
                {
                    proposed.Confirmed = true;
                    proposed.EntityId ??= c.EntityId ?? existing?.EntityId;
                    if (action == "retcon") proposed.Source = "retcon";
                }
                c.Status = action == "retcon" ? ReadAlongStatus.Retcon : ReadAlongStatus.AcceptedNew;
                break;
            case "keep-existing":
                if (existing is not null) existing.Confirmed = true;
                if (proposed is not null && !proposed.Confirmed) db.StoryFacts.Remove(proposed);
                c.Status = ReadAlongStatus.KeptExisting;
                break;
            case "dismiss":
                c.Status = ReadAlongStatus.Dismissed;
                break;
        }
        c.ResolvedAt = now;
        c.Note = string.IsNullOrWhiteSpace(note) ? c.Note : note.Trim()[..Math.Min(note.Trim().Length, 1000)];
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <summary>Re-open a ruled card (the author changed their mind). Facts are not rolled back:
    /// rule again to change them.</summary>
    public async Task<bool> ReopenAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var c = await db.ContinuityConflicts.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null || c.Status == ReadAlongStatus.Open) return false;
        c.Status = ReadAlongStatus.Open;
        c.ResolvedAt = null;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
