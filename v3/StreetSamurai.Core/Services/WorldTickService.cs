using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Background tick that advances the global story clock so the world keeps
/// moving even when no one is writing. Foundation for the "living world sim"
/// vision — a place to wire decay rules, scheduled events, NPC routines, etc.
/// later. For now it only advances the clock; rule registration is a deliberate
/// future hook (see <see cref="OnTick"/>).
///
/// <para><b>Cadence.</b> Real-time tick every <see cref="RealTimeInterval"/>;
/// each tick advances story-time by <see cref="StoryTimePerTick"/>. The default
/// 1:1 mapping (1 real minute = 1 story minute) is chosen so the clock moves
/// visibly in /timeline without sprinting through canon. Tune via the const at
/// the top of the file — settings-driven configuration is the next step once
/// the rule plug-in surface lands.</para>
///
/// <para><b>Enable flag.</b> The service is registered in DI but starts in
/// <c>EnabledByDefault = false</c>, so no autonomous clock advancement happens
/// until a deliberate flip. Heartbeat log fires either way so a misconfiguration
/// is visible. The intent is to let infrastructure land cold-disabled, then
/// turn the dial up after the rule layer exists.</para>
///
/// <para><b>Why not Quartz/Hangfire.</b> Same reasoning as
/// <see cref="ContinuityLongSweepService"/>: a single <see cref="PeriodicTimer"/>
/// inside a <see cref="BackgroundService"/> matches the cadence and avoids a
/// dependency.</para>
/// </summary>
public class WorldTickService : BackgroundService
{
    /// <summary>How often the tick fires in real time.</summary>
    public static readonly TimeSpan RealTimeInterval = TimeSpan.FromMinutes(5);

    /// <summary>How much story-time advances per real-time tick when enabled.</summary>
    public static readonly TimeSpan StoryTimePerTick = TimeSpan.FromMinutes(5);

    /// <summary>Wait briefly after process start so the home page wins SQL connections first.</summary>
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Conservative default: don't auto-advance the world clock until a future
    /// pass wires up rules and the user has decided to enable it. Heartbeat is
    /// always logged so the service's existence is visible.
    /// </summary>
    public bool Enabled { get; set; } = false;

    private readonly WorldClockService clock;
    private readonly ILogger<WorldTickService> log;

    public WorldTickService(WorldClockService clock, ILogger<WorldTickService> log)
    {
        this.clock = clock;
        this.log   = log;
    }

    /// <summary>Diagnostic counters — surfaced on the system status page.</summary>
    public DateTime? LastTickAt   { get; private set; }
    public int       TickCount    { get; private set; }
    public DateTime? LastStoryNow { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(StartupDelay, stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(RealTimeInterval);
        do
        {
            try
            {
                OnTick();
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "WorldTickService: tick failed (will retry)");
            }
        } while (await SafeWaitAsync(timer, stoppingToken));
    }

    /// <summary>
    /// One tick of the world. Currently advances the clock when
    /// <see cref="Enabled"/>. Future rules go here — decay of transient states,
    /// scheduled story-time events, NPC routine actions. Each rule should be a
    /// thin call to a service that owns its domain (e.g.
    /// <c>WorldStateLedger.EmitDecay(...)</c>) so this method stays a manifest,
    /// not a god method.
    /// </summary>
    private void OnTick()
    {
        TickCount++;
        LastTickAt = DateTime.UtcNow;

        if (!Enabled)
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
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }
}
