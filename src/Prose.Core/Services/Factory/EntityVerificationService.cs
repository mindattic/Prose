using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Factory;

/// <summary>One beat of the book that tags the entity, in reading order.</summary>
public sealed record MentionBeat(int Position, int Number, Guid BeatId, bool Read, string? Text);

/// <summary>What <see cref="EntityVerificationService.BeginAsync"/> delivers: the record as it
/// stands, the beats that mention it, and the nonce that seals both.</summary>
public sealed record VerificationPacket(
    Guid EntityId, string EntityType, string Name, Guid BookId, string Book,
    DateTime RecordModifiedAt, string MentionsFingerprint,
    int MentionCount, int UnreadMentions, bool TextTruncated,
    JsonNode? Record, IReadOnlyList<MentionBeat> Mentions,
    string Nonce, DateTime ExpiresAt);

/// <summary>
/// Station F1, Verified (RFC 0015 §3.3): "this entity's record was examined against this book, as
/// both stand now".
///
/// <para><b>Begin</b> delivers the canonical record and the beats that tag it, and seals what it
/// delivered in an HMAC nonce: the record's <c>ModifiedAt</c> and a fingerprint of those beats.
/// <b>Commit</b> refuses if either changed since, if any of those beats is unread in the gate (so a
/// verification can only be made after the reading it vouches for), or if the record matches one
/// of the book's law patterns (the world must not hold what the page may not say). Commit writes one
/// <see cref="EntityVerification"/> row and never touches the entity, so it cannot un-read anything.</para>
///
/// <para>Trust point, stated: the row certifies the record was delivered after the beats were read,
/// not that the reader judged it correct. A record found wrong is fixed on the record
/// (<c>set_character_fields</c>), which un-reads its mentions; they are re-read, then it is verified.</para>
///
/// <para>The nonce key lives only in this process: a Hub restart voids open nonces. Begin again.</para>
/// </summary>
public sealed class EntityVerificationService(
    IDbContextFactory<ProseDbContext> dbFactory, BookSpineService spine, ReadGateService gate, FactorySessionService sessions,
    RulingService rulings)
{
    static readonly byte[] Key = RandomNumberGenerator.GetBytes(32);

    /// <summary>How long a begun verification can wait for its commit.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(2);

    /// <summary>Order-free fingerprint of the beats that mention an entity: which beats, at which text.</summary>
    public static string Fingerprint(IEnumerable<(Guid BeatId, string? TextHash)> mentions) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n",
            mentions.OrderBy(m => m.BeatId).Select(m => $"{m.BeatId:N}:{m.TextHash}"))))).ToLowerInvariant();

    /// <summary>Every tagged entity of a book → the fingerprint of its mentions there.</summary>
    public static Dictionary<Guid, string> Fingerprints(IEnumerable<(Guid BeatId, string Text, string? TextHash)> bookBeats) =>
        bookBeats.SelectMany(b => BeatMarkup.ExtractEntityGuids(b.Text).Select(e => (Entity: e, b.BeatId, b.TextHash)))
            .GroupBy(x => x.Entity)
            .ToDictionary(g => g.Key, g => Fingerprint(g.Select(x => (x.BeatId, x.TextHash))));

    private sealed record BookBeat(int Position, int Number, Guid Id, string Text, string? TextHash);
    private sealed record EntityRow(Guid Id, string EntityType, string Name, DateTime ModifiedAt);

    public async Task<VerificationPacket> BeginAsync(Guid entityId, Guid bookId, int textBudgetChars = 40_000, CancellationToken ct = default)
    {
        var (entity, beats, book) = await LoadAsync(entityId, bookId, ct);
        var mentions = beats.Where(b => BeatMarkup.ExtractEntityGuids(b.Text).Contains(entityId)).ToList();
        var unread = (await gate.GetStatusAsync(bookId, ct)).Unread.Select(u => u.BeatId).ToHashSet();
        var fingerprint = Fingerprint(mentions.Select(m => (m.Id, m.TextHash)));

        JsonNode? record;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
            record = CanonRecordLoader.Load(db, entity.EntityType, entityId);

        var budget = Math.Max(0, textBudgetChars);
        var truncated = false;
        var rows = new List<MentionBeat>();
        foreach (var m in mentions)
        {
            string? text = null;
            var plain = BeatMarkup.StripEntityTags(m.Text);
            if (plain.Length <= budget) { text = plain; budget -= plain.Length; }
            else { truncated = true; budget = 0; }
            rows.Add(new MentionBeat(m.Position, m.Number, m.Id, !unread.Contains(m.Id), text));
        }

        var expires = DateTime.UtcNow + Lifetime;
        return new VerificationPacket(entity.Id, entity.EntityType, entity.Name, bookId, book,
            entity.ModifiedAt, fingerprint, rows.Count, rows.Count(r => !r.Read), truncated, record, rows,
            Seal(entity.Id, bookId, entity.ModifiedAt.Ticks, fingerprint, expires.Ticks), expires);
    }

    public async Task<EntityVerification> CommitAsync(string nonce, string by, CancellationToken ct = default)
    {
        var (entityId, bookId, modifiedTicks, fingerprint, expiresTicks) = Open(nonce);
        if (DateTime.UtcNow.Ticks > expiresTicks)
            throw new InvalidOperationException("This verification expired. Begin again and examine the record as it stands.");

        var (entity, beats, _) = await LoadAsync(entityId, bookId, ct);
        if (entity.ModifiedAt.Ticks != modifiedTicks)
            throw new InvalidOperationException($"{entity.Name}'s record changed after begin. Begin again and examine it as it stands.");
        var mentions = beats.Where(b => BeatMarkup.ExtractEntityGuids(b.Text).Contains(entityId)).ToList();
        if (Fingerprint(mentions.Select(m => (m.Id, m.TextHash))) != fingerprint)
            throw new InvalidOperationException($"A beat that mentions {entity.Name} changed, or gained or lost the tag, after begin. Begin again.");

        var unread = (await gate.GetStatusAsync(bookId, ct)).Unread.ToDictionary(u => u.BeatId);
        var unreadMentions = mentions.Where(m => unread.ContainsKey(m.Id)).ToList();
        if (unreadMentions.Count > 0)
            throw new InvalidOperationException(
                $"{unreadMentions.Count} of the {mentions.Count} beats that mention {entity.Name} are unread (positions " +
                $"{ReadGateService.Runs(unreadMentions.Select(m => m.Position))}). A record is verified against the book as read: read them first.");

        // The record must not hold what the book's law forbids (RFC 0015 §3.6).
        var breaches = await rulings.FindRecordViolationsAsync(bookId, entityId, ct: ct);
        if (breaches.Count > 0)
            throw new InvalidOperationException(
                $"{entity.Name}'s record breaks {breaches.Select(b => b.RulingId).Distinct().Count()} law(s): " +
                string.Join("; ", breaches.Take(4).Select(b => $"{b.Field} \"{b.Match}\""))
                + ". Fix the record, then begin again.");

        var sessionId = await sessions.CurrentSessionIdAsync(ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.EntityVerifications.FirstOrDefaultAsync(v => v.EntityId == entityId && v.BookId == bookId, ct);
        if (row == null) { row = new EntityVerification { EntityId = entityId, BookId = bookId }; db.EntityVerifications.Add(row); }
        row.RecordModifiedAt = entity.ModifiedAt;
        row.MentionsFingerprint = fingerprint;
        row.VerifiedAt = DateTime.UtcNow;
        row.By = string.IsNullOrWhiteSpace(by) ? "unknown" : by.Trim();
        row.SessionId = sessionId;
        await db.SaveChangesAsync(ct);
        return row;
    }

    private async Task<(EntityRow Entity, List<BookBeat> Beats, string Book)> LoadAsync(Guid entityId, Guid bookId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await db.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => e.Id == entityId)
                         .Select(e => new EntityRow(e.Id, e.EntityType, e.Name, e.ModifiedAt)).FirstOrDefaultAsync(ct)
                     ?? throw new ArgumentException($"Entity {entityId} not found.");
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == bookId, ct)
                   ?? throw new ArgumentException($"Book {bookId} not found.");
        var sp = await spine.GetAsync(bookId, ct);
        var order = sp.Chapters.SelectMany(c => c.Beats).ToList();
        var ids = order.Select(b => b.BeatId).ToList();
        var text = await db.Beats.AsNoTracking().Where(b => ids.Contains(b.Id))
            .Select(b => new { b.Id, b.Number, b.Text, b.TextHash }).ToDictionaryAsync(b => b.Id, ct);
        var beats = order.Where(b => text.ContainsKey(b.BeatId))
            .Select(b => new BookBeat(b.Ordinal, text[b.BeatId].Number, b.BeatId, text[b.BeatId].Text ?? "", text[b.BeatId].TextHash))
            .ToList();
        return (entity, beats, node.NodeCode ?? node.Slug);
    }

    // ── the nonce: what begin delivered, sealed so commit can hold it to that ──

    private static string Seal(Guid entityId, Guid bookId, long modifiedTicks, string fingerprint, long expiresTicks)
    {
        var body = $"{entityId:N}|{bookId:N}|{modifiedTicks}|{fingerprint}|{expiresTicks}";
        var mac = HMACSHA256.HashData(Key, Encoding.UTF8.GetBytes(body));
        return $"{B64(Encoding.UTF8.GetBytes(body))}.{B64(mac)}";
    }

    private static (Guid EntityId, Guid BookId, long ModifiedTicks, string Fingerprint, long ExpiresTicks) Open(string nonce)
    {
        var parts = (nonce ?? "").Trim().Split('.');
        if (parts.Length != 2) throw new ArgumentException("Not a verification nonce.");
        byte[] body, mac;
        try { body = UnB64(parts[0]); mac = UnB64(parts[1]); }
        catch (FormatException) { throw new ArgumentException("Not a verification nonce."); }
        if (!CryptographicOperations.FixedTimeEquals(mac, HMACSHA256.HashData(Key, body)))
            throw new ArgumentException("This nonce was not issued by this Hub (or the Hub restarted since). Begin again.");
        var f = Encoding.UTF8.GetString(body).Split('|');
        return (Guid.ParseExact(f[0], "N"), Guid.ParseExact(f[1], "N"), long.Parse(f[2]), f[3], long.Parse(f[4]));
    }

    private static string B64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] UnB64(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }
}
