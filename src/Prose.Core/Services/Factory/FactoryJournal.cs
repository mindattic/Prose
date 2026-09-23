using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services.Factory;

public sealed record JournalEvent(DateTime At, string Kind, string Detail, string? Actor = null);

public sealed record Journal(DateTime Since, DateTime Until, IReadOnlyList<JournalEvent> Events, bool TemporalHistoryRead, string? TemporalNote);

/// <summary>
/// What happened, reconstructed from the records themselves (RFC 0015 §3.13): every Hub call in the
/// ledger, every factory row written (receipts, notes, verifications, presses, orders, rulings,
/// sessions), and — on SQL Server — every stored version of a beat or an entity from the temporal
/// history, in time order. Nothing here is a log someone had to remember to write: each source is
/// written by the act itself, so a change made anywhere through the Hub shows up.
/// </summary>
public sealed class FactoryJournal(IDbContextFactory<ProseDbContext> dbFactory, BookSpineService spine)
{
    public async Task<Journal> ReadAsync(DateTime since, DateTime? until = null, Guid? bookId = null, CancellationToken ct = default)
    {
        var to = until ?? DateTime.UtcNow;
        var events = new List<JournalEvent>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        HashSet<Guid>? beatIds = null;
        HashSet<Guid>? tagged = null;
        string[] refs = [];
        if (bookId is { } book)
        {
            var sp = await spine.GetAsync(book, ct);
            beatIds = sp.Chapters.SelectMany(c => c.Beats).Select(b => b.BeatId).ToHashSet();
            var ids = beatIds.ToList();
            var texts = await db.Beats.AsNoTracking().Where(b => ids.Contains(b.Id)).Select(b => b.Text).ToListAsync(ct);
            tagged = texts.SelectMany(t => BeatMarkup.ExtractEntityGuids(t)).ToHashSet();
            var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.Id == book)
                .Select(n => new { n.Slug, n.NodeCode }).FirstAsync(ct);
            refs = new[] { node.Slug, node.NodeCode, book.ToString(), book.ToString("N") }.Where(s => !string.IsNullOrEmpty(s)).Select(s => s!).ToArray();
        }

        // Every Hub call. With a book, the calls that name it.
        var calls = await db.CommandLedgerEntries.AsNoTracking().Where(e => e.At >= since && e.At <= to).ToListAsync(ct);
        foreach (var c in calls.Where(c => refs.Length == 0 || refs.Any(r => c.ArgsJson.Contains(r, StringComparison.OrdinalIgnoreCase))))
            events.Add(new(c.At, "call", $"{c.Source} {c.HandlerClass}{(c.Method is null ? "" : "." + c.Method)} {(c.Success ? "ok" : "FAILED")} {Short(c.ArgsJson, 140)}", c.Actor));

        // The factory's own rows.
        foreach (var r in await db.BeatReadReceipts.AsNoTracking().Where(r => r.ReadAt >= since && r.ReadAt <= to).ToListAsync(ct))
            if (beatIds == null || beatIds.Contains(r.BeatId)) events.Add(new(r.ReadAt, "read", $"beat {r.BeatId} read at {Short(r.TextHash, 12)}", r.ReadBy));
        foreach (var n in await db.BeatReadNotes.AsNoTracking().Where(n => n.At >= since && n.At <= to).ToListAsync(ct))
            if (beatIds == null || beatIds.Contains(n.BeatId)) events.Add(new(n.At, "note", $"{n.Kind} ({n.Status}) on beat {n.BeatId}: {Short(n.Text, 140)}", n.ReadBy));
        foreach (var v in await db.EntityVerifications.AsNoTracking().Where(v => v.VerifiedAt >= since && v.VerifiedAt <= to).ToListAsync(ct))
            if (bookId == null || v.BookId == bookId) events.Add(new(v.VerifiedAt, "verify", $"entity {v.EntityId} verified at record {v.RecordModifiedAt:O}", v.By));
        foreach (var x in await db.Exports.AsNoTracking().Where(x => x.At >= since && x.At <= to).ToListAsync(ct))
            if (bookId == null || x.BookId == bookId) events.Add(new(x.At, "press", $"{x.Format} V{x.Version} at {Short(x.BookFingerprint, 12)}: {x.Path}"));
        foreach (var r in await db.Rulings.AsNoTracking().Where(r => r.At >= since && r.At <= to).ToListAsync(ct))
            if (bookId == null || r.BookId == bookId || r.BookId == null) events.Add(new(r.At, "ruling", $"{r.Kind}: {Short(r.Text, 140)}", r.Source));
        foreach (var o in await db.WorkOrders.AsNoTracking().Where(o => (o.OpenedAt >= since && o.OpenedAt <= to) || (o.ClosedAt >= since && o.ClosedAt <= to)).ToListAsync(ct))
        {
            if (bookId != null && o.NodeId != null && o.NodeId != bookId) continue;
            if (o.OpenedAt >= since && o.OpenedAt <= to) events.Add(new(o.OpenedAt, "order", $"opened {o.Kind} {o.Id}: {o.Title}"));
            if (o.ClosedAt is { } closed && closed >= since && closed <= to) events.Add(new(closed, "order", $"{o.Status} {o.Kind} {o.Id}: {o.Title}{(o.CommitHash is null ? "" : $" @ {o.CommitHash}")}"));
        }
        foreach (var s in await db.FactorySessions.AsNoTracking().Where(s => (s.StartedAt >= since && s.StartedAt <= to) || (s.EndedAt >= since && s.EndedAt <= to)).ToListAsync(ct))
        {
            if (s.StartedAt >= since && s.StartedAt <= to) events.Add(new(s.StartedAt, "session", $"started {s.Id}"));
            if (s.EndedAt is { } ended && ended >= since && ended <= to) events.Add(new(ended, "session", $"ended {s.Id}: {Short(s.EndSummaryJson ?? "", 160)}"));
        }

        // Every stored version of a beat or an entity, from SQL Server's own history.
        var (read, note) = await ReadTemporalAsync(db, since, to, beatIds, tagged, events, ct);
        return new Journal(since, to, events.OrderBy(e => e.At).ToList(), read, note);
    }

    private static async Task<(bool Read, string? Note)> ReadTemporalAsync(ProseDbContext db, DateTime since, DateTime to,
        HashSet<Guid>? beatIds, HashSet<Guid>? tagged, List<JournalEvent> events, CancellationToken ct)
    {
        if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
            return (false, $"temporal history is SQL Server's; this provider ({db.Database.ProviderName}) has none");
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync(ct);
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT Id, Number, Version, LastWriteReason, SysStart FROM Beats FOR SYSTEM_TIME ALL
                WHERE SysStart >= @since AND SysStart <= @to
                """;
            Param(cmd, "@since", since); Param(cmd, "@to", to);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.GetGuid(0);
                if (beatIds != null && !beatIds.Contains(id)) continue;
                events.Add(new(r.GetDateTime(4), "beat", $"#{r.GetInt32(1)} v{r.GetInt32(2)} ({(r.IsDBNull(3) ? "no reason" : r.GetString(3))})"));
            }
        }
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT Id, Name, EntityType, SysStart FROM Entities FOR SYSTEM_TIME ALL
                WHERE SysStart >= @since AND SysStart <= @to
                """;
            Param(cmd, "@since", since); Param(cmd, "@to", to);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.GetGuid(0);
                if (tagged != null && !tagged.Contains(id)) continue;
                events.Add(new(r.GetDateTime(3), "entity", $"{r.GetString(1)} ({r.GetString(2)}) record version"));
            }
        }
        return (true, null);
    }

    /// <summary>An instant for a journal window: ISO 8601 (UTC unless it carries an offset), or a span
    /// back from now — <c>90m</c>, <c>6h</c>, <c>2d</c>.</summary>
    public static bool TryParseInstant(string? s, out DateTime utc)
    {
        utc = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        var unit = char.ToLowerInvariant(s[^1]);
        if (s.Length > 1 && unit is 'm' or 'h' or 'd'
            && double.TryParse(s[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && n >= 0)
        {
            utc = DateTime.UtcNow - unit switch { 'm' => TimeSpan.FromMinutes(n), 'h' => TimeSpan.FromHours(n), _ => TimeSpan.FromDays(n) };
            return true;
        }
        if (!DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at)) return false;
        utc = at.UtcDateTime;
        return true;
    }

    private static void Param(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }

    private static string Short(string s, int n) => s.Length <= n ? s : s[..n] + "…";
}
