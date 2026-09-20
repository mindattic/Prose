using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Background tick that advances the global story clock so the world keeps
/// moving even when no one is writing. Foundation for the "living world sim"
/// vision — a place to wire decay rules, scheduled events, NPC routines, etc.
///
/// <para><b>Cadence.</b> Real-time tick every <see cref="RealTimeInterval"/>;
/// each tick advances story-time by <see cref="StoryTimePerTick"/>.</para>
///
/// <para><b>Enable flag.</b> The service reads <c>SettingsService.WorldTickEnabled</c>
/// each tick. Off by default — infrastructure lands cold-disabled, then enabled
/// deliberately once the rule layer exists. Heartbeat log fires either way so
/// a misconfiguration is visible.</para>
///
/// <para><b>EntityStateEvents.</b> When enabled, one event per active character
/// in the current universe is written each tick (AspectKey="world-tick",
/// Verb="set", NewValue="idle") so the story clock's passage is recorded on
/// the entity ledger. Capped at 100 characters per tick for cost safety.</para>
/// </summary>
public class WorldTickService : BackgroundService
{
    /// <summary>How often the tick fires in real time.</summary>
    public static readonly TimeSpan RealTimeInterval = TimeSpan.FromMinutes(5);

    /// <summary>How much story-time advances per real-time tick when enabled.</summary>
    public static readonly TimeSpan StoryTimePerTick = TimeSpan.FromMinutes(5);

    /// <summary>Wait briefly after process start so the home page wins SQL connections first.</summary>
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);

    private static readonly int MaxCharactersPerTick = 100;

    private readonly WorldClockService clock;
    private readonly SettingsService settings;
    private readonly WorldStateLedger ledger;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly IUniverseContext universe;
    private readonly ILogger<WorldTickService> log;

    public WorldTickService(
        WorldClockService clock,
        SettingsService settings,
        WorldStateLedger ledger,
        IDbContextFactory<ProseDbContext> dbFactory,
        IUniverseContext universe,
        ILogger<WorldTickService> log)
    {
        this.clock    = clock;
        this.settings = settings;
        this.ledger   = ledger;
        this.dbFactory = dbFactory;
        this.universe = universe;
        this.log      = log;
    }

    /// <summary>Toggle the tick on/off. Proxies to <see cref="SettingsService.WorldTickEnabled"/>.</summary>
    public bool Enabled
    {
        get => settings.WorldTickEnabled;
        set => settings.WorldTickEnabled = value;
    }

    /// <summary>Diagnostic counters — surfaced on the system status page.</summary>
    public DateTime? LastTickAt     { get; private set; }
    public int       TickCount      { get; private set; }
    public DateTime? LastStoryNow   { get; private set; }
    public int       LastEventCount { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(StartupDelay, stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(RealTimeInterval);
        do
        {
            try
            {
                await OnTickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "WorldTickService: tick failed (will retry)");
            }
        } while (await SafeWaitAsync(timer, stoppingToken));
    }

    private async Task OnTickAsync(CancellationToken ct)
    {
        TickCount++;
        LastTickAt = DateTime.UtcNow;

        if (!settings.WorldTickEnabled)
        {
            log.LogInformation("WorldTickService heartbeat (disabled): tick #{N}", TickCount);
            return;
        }

        var before = clock.GetNow();
        var after  = before + StoryTimePerTick;
        clock.SetNow(after);
        LastStoryNow = after;
        log.LogInformation("WorldTickService tick #{N}: story-time {Before:o} → {After:o}",
            TickCount, before, after);

        // Write one EntityStateEvent per active character in the current universe.
        var universeId = universe.CurrentId;
        List<Guid> characterIds;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            characterIds = await db.Entities
                .Where(e => e.EntityType == "character"
                         && e.UniverseId == universeId)
                .OrderBy(e => e.Id)
                .Take(MaxCharactersPerTick)
                .Select(e => e.Id)
                .ToListAsync(ct);
        }

        if (characterIds.Count == 0)
        {
            log.LogInformation("WorldTickService tick #{N}: no active characters to stamp.", TickCount);
            LastEventCount = 0;
            return;
        }

        var events = characterIds.Select(id => new EntityStateEvent
        {
            UniverseId      = universeId,
            EntityId        = id,
            AspectKey       = "world-tick",
            Verb            = "set",
            NewValue        = "idle",
            AtStoryTime     = after,
            Source          = "world-tick",
            Confidence      = 1.0,
        }).ToList();

        var saved = await ledger.RecordManyAsync(events, ct);
        LastEventCount = saved;
        log.LogInformation("WorldTickService tick #{N}: wrote {Count} EntityStateEvent(s) for {Chars} character(s).",
            TickCount, saved, characterIds.Count);
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }
}
