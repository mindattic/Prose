using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Read/write API over the <see cref="EntityStateEvent"/> append-only ledger.
/// Every state change a chapter induces — Kyle fires a shell, Sasha enters
/// Auntie Hoa's, Ada loses the bracelet — lands here as a row keyed to the
/// in-world instant the change happened. Querying "what was true at time T"
/// is then a sorted seek per (entity, aspect).
///
/// The ledger is the substrate for:
///   • timeline cursor → "world state at this dot"
///   • dossier as-of   → "what did Sable know on 2256-04-15?"
///   • contradiction detection → "you wrote Kyle in two places at the same minute"
///   • shotgun-shell math → COUNT(verb=dec) since the last reload
/// </summary>
public class WorldStateLedger
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<WorldStateLedger> log;

    public WorldStateLedger(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<WorldStateLedger> log)
    {
        this.dbFactory = dbFactory;
        this.log = log;
    }

    /// <summary>Fired after a batch of events is committed (subscribers: cache invalidation, UI refresh).</summary>
    public event Action<int>? OnEventsRecorded;

    // ── write ──────────────────────────────────────────────────────────────────

    public async Task<long> RecordAsync(EntityStateEvent ev, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (ev.AtStoryTime == default) ev.AtStoryTime = DateTime.UtcNow;
        if (!ev.InWorldValidFrom.HasValue) ev.InWorldValidFrom = ev.AtStoryTime;

        // Close the prior open window for the same (EntityId, AspectKey) so the
        // ledger maintains [from, to) intervals — point-in-story-time reads can
        // then index-seek a single row instead of MAX(AtStoryTime <= T) per group.
        await CloseOpenWindowAsync(db, ev.EntityId, ev.AspectKey, ev.InWorldValidFrom.Value, ct);

        db.EntityStateEvents.Add(ev);
        await db.SaveChangesAsync(ct);
        try { OnEventsRecorded?.Invoke(1); } catch { }
        return ev.Id;
    }

    public async Task<int> RecordManyAsync(IReadOnlyList<EntityStateEvent> events, CancellationToken ct = default)
    {
        if (events.Count == 0) return 0;
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Process per (EntityId, AspectKey) so we close prior windows correctly
        // even when several events for the same aspect arrive in one batch.
        var ordered = events
            .Select((ev, i) =>
            {
                if (ev.AtStoryTime == default) ev.AtStoryTime = DateTime.UtcNow;
                if (!ev.InWorldValidFrom.HasValue) ev.InWorldValidFrom = ev.AtStoryTime;
                return (ev, i);
            })
            .OrderBy(t => t.ev.AtStoryTime).ThenBy(t => t.i)
            .Select(t => t.ev)
            .ToList();

        foreach (var ev in ordered)
        {
            await CloseOpenWindowAsync(db, ev.EntityId, ev.AspectKey, ev.InWorldValidFrom!.Value, ct);
            db.EntityStateEvents.Add(ev);
        }
        var n = await db.SaveChangesAsync(ct);
        try { OnEventsRecorded?.Invoke(events.Count); } catch { }
        return n;
    }

    /// <summary>
    /// Set <see cref="EntityStateEvent.InWorldValidTo"/> on the most recent
    /// still-open event for (entityId, aspectKey) so that the new event can
    /// open its own window cleanly. Idempotent: skips when nothing's open or
    /// when the open row's InWorldValidFrom is already > newFrom (out-of-order
    /// inserts — those are left alone; we never reach back in time).
    /// </summary>
    private static async Task CloseOpenWindowAsync(
        StreetSamuraiDbContext db, Guid entityId, string aspectKey, DateTime newFrom, CancellationToken ct)
    {
        var openRow = await db.EntityStateEvents
            .Where(e => e.EntityId == entityId
                     && e.AspectKey == aspectKey
                     && e.InWorldValidTo == null)
            .OrderByDescending(e => e.AtStoryTime).ThenByDescending(e => e.Id)
            .FirstOrDefaultAsync(ct);
        if (openRow == null) return;
        if (openRow.InWorldValidFrom.HasValue && openRow.InWorldValidFrom.Value > newFrom) return;
        openRow.InWorldValidTo = newFrom;
    }

    // ── query ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Return the most recent value of (<paramref name="entityId"/>, <paramref name="aspectKey"/>)
    /// at or before <paramref name="atStoryTime"/>. Null when no event matches.
    /// </summary>
    public async Task<EntityStateEvent?> StateAtAsync(
        Guid entityId, string aspectKey, DateTime atStoryTime, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.EntityStateEvents
            .AsNoTracking()
            .Where(e => e.EntityId == entityId && e.AspectKey == aspectKey && e.AtStoryTime <= atStoryTime)
            .OrderByDescending(e => e.AtStoryTime)
            .ThenByDescending(e => e.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Snapshot of every aspect's most recent value for one entity at time T.
    /// Returns a dictionary keyed by AspectKey.
    /// </summary>
    public async Task<Dictionary<string, EntityStateEvent>> SnapshotAsync(
        Guid entityId, DateTime atStoryTime, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // Per-aspect latest row at-or-before T.
        var rows = await db.EntityStateEvents
            .AsNoTracking()
            .Where(e => e.EntityId == entityId && e.AtStoryTime <= atStoryTime)
            .ToListAsync(ct);
        return rows
            .GroupBy(r => r.AspectKey, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.AtStoryTime).ThenByDescending(r => r.Id).First(),
                StringComparer.Ordinal);
    }

    /// <summary>
    /// Chronological list of every event for one entity (or one aspect) within
    /// a time window. Used by the timeline UI's "show me the trail" view.
    /// </summary>
    public async Task<List<EntityStateEvent>> EventsBetweenAsync(
        Guid entityId, DateTime fromTime, DateTime toTime,
        string? aspectKey = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var q = db.EntityStateEvents.AsNoTracking()
            .Where(e => e.EntityId == entityId && e.AtStoryTime >= fromTime && e.AtStoryTime <= toTime);
        if (!string.IsNullOrEmpty(aspectKey)) q = q.Where(e => e.AspectKey == aspectKey);
        return await q
            .OrderBy(e => e.AtStoryTime).ThenBy(e => e.Id)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Chronological list of every event in a time window across all entities.
    /// Powers the timeline's "what's happening at this moment" feed.
    /// </summary>
    public async Task<List<EntityStateEvent>> WorldEventsBetweenAsync(
        DateTime fromTime, DateTime toTime, int max = 500, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.EntityStateEvents.AsNoTracking()
            .Where(e => e.AtStoryTime >= fromTime && e.AtStoryTime <= toTime)
            .OrderBy(e => e.AtStoryTime).ThenBy(e => e.Id)
            .Take(max)
            .ToListAsync(ct);
    }

    /// <summary>
    /// "Shotgun shell" example: how many <c>dec</c> events on this aspect since
    /// the most recent <c>set</c>/<c>inc</c> (treated as a reload). Returns
    /// (currentValue, eventsSinceReload, lastReload).
    /// </summary>
    public async Task<(double? value, List<EntityStateEvent> sinceReload, EntityStateEvent? lastReload)>
        AmmoTrailAsync(Guid entityId, string aspectKey, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var all = await db.EntityStateEvents.AsNoTracking()
            .Where(e => e.EntityId == entityId && e.AspectKey == aspectKey)
            .OrderBy(e => e.AtStoryTime).ThenBy(e => e.Id)
            .ToListAsync(ct);
        if (all.Count == 0) return (null, new(), null);

        var lastReload = all.LastOrDefault(e => e.Verb == "set" || e.Verb == "inc");
        var sinceReload = lastReload == null
            ? all.Where(e => e.Verb == "dec").ToList()
            : all.Where(e => e.AtStoryTime > lastReload.AtStoryTime && e.Verb == "dec").ToList();
        var current = double.TryParse(all[^1].NewValue, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : (double?)null;
        return (current, sinceReload, lastReload);
    }

    // ── schema bootstrap ──────────────────────────────────────────────────────

    /// <summary>
    /// Idempotent: creates <c>EntityStateEvents</c> + indexes on a live DB
    /// without dropping anything. Call before recording on a database that
    /// pre-dates this feature (the <c>--rebuild</c> path picks it up via
    /// <see cref="StreetSamuraiDbContext.OnModelCreating"/>; existing DBs need
    /// this one-shot DDL).
    /// </summary>
    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        const string ddl = """
            IF OBJECT_ID('dbo.EntityStateEvents','U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[EntityStateEvents] (
                    [Id]               BIGINT IDENTITY(1,1) NOT NULL,
                    [EntityId]         UNIQUEIDENTIFIER NOT NULL,
                    [AspectKey]        NVARCHAR(200) NOT NULL,
                    [Verb]             NVARCHAR(20)  NOT NULL,
                    [OldValue]         NVARCHAR(MAX) NULL,
                    [NewValue]         NVARCHAR(MAX) NULL,
                    [Delta]            FLOAT NULL,
                    [AtStoryTime]      DATETIME2(7)  NOT NULL,
                    [InWorldValidFrom] DATETIME2(7)  NULL,
                    [InWorldValidTo]   DATETIME2(7)  NULL,
                    [ChapterId]        UNIQUEIDENTIFIER NULL,
                    [BeatGuid]         UNIQUEIDENTIFIER NULL,
                    [Source]           NVARCHAR(200) NOT NULL,
                    [Confidence]       FLOAT NULL,
                    [Snippet]          NVARCHAR(MAX) NULL,
                    [SysStart]         DATETIME2(7) GENERATED ALWAYS AS ROW START NOT NULL DEFAULT SYSUTCDATETIME(),
                    [SysEnd]           DATETIME2(7) GENERATED ALWAYS AS ROW END   NOT NULL DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
                    PERIOD FOR SYSTEM_TIME ([SysStart], [SysEnd]),
                    CONSTRAINT [PK_EntityStateEvents] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_EntityStateEvents_Entities_EntityId]
                        FOREIGN KEY ([EntityId]) REFERENCES [dbo].[Entities]([Id]) ON DELETE CASCADE
                ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[EntityStateEvents_History]));

                CREATE INDEX [IX_EntityStateEvents_EntityId_AspectKey_AtStoryTime]
                    ON [dbo].[EntityStateEvents]([EntityId], [AspectKey], [AtStoryTime]);
                CREATE INDEX [IX_EntityStateEvents_AtStoryTime]
                    ON [dbo].[EntityStateEvents]([AtStoryTime]);
                CREATE INDEX [IX_EntityStateEvents_ChapterId]
                    ON [dbo].[EntityStateEvents]([ChapterId]);
                CREATE INDEX [IX_EntityStateEvents_BeatGuid]
                    ON [dbo].[EntityStateEvents]([BeatGuid]);
                CREATE INDEX [IX_EntityStateEvents_EntityId_AspectKey_InWorldValidFrom]
                    ON [dbo].[EntityStateEvents]([EntityId], [AspectKey], [InWorldValidFrom])
                    INCLUDE ([InWorldValidTo], [NewValue], [Verb]);
            END
            ELSE
            BEGIN
                -- Idempotent backfill for live DBs that pre-date the bi-temporal columns.
                IF COL_LENGTH('dbo.EntityStateEvents', 'InWorldValidFrom') IS NULL
                    ALTER TABLE [dbo].[EntityStateEvents] ADD [InWorldValidFrom] DATETIME2(7) NULL;
                IF COL_LENGTH('dbo.EntityStateEvents', 'InWorldValidTo') IS NULL
                    ALTER TABLE [dbo].[EntityStateEvents] ADD [InWorldValidTo]   DATETIME2(7) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_EntityStateEvents_EntityId_AspectKey_InWorldValidFrom'
                                 AND object_id = OBJECT_ID('dbo.EntityStateEvents'))
                    CREATE INDEX [IX_EntityStateEvents_EntityId_AspectKey_InWorldValidFrom]
                        ON [dbo].[EntityStateEvents]([EntityId], [AspectKey], [InWorldValidFrom])
                        INCLUDE ([InWorldValidTo], [NewValue], [Verb]);
            END;
            """;
        await db.Database.ExecuteSqlRawAsync(ddl, ct);
    }
}
