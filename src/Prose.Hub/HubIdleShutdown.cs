using System.Diagnostics;
using Prose.Core.Services;

namespace Prose.Hub;

/// <summary>
/// Idle self-shutdown (PRS-T129). The Hub holds ~900 MB resident (the graph, the context
/// stacks, the EF model) and used to stay up forever after the last Prose session closed.
/// Nothing is lost by stopping it: the SessionStart hook (.prose/hooks/start-prose-hub.ps1)
/// starts it again for the next CLI session, and Writer relaunches it when opened.
///
/// <para>The Hub stops itself, gracefully (so the ApplicationStopping hook marks an open DCM run
/// and flushes the log), once ALL of these have held for <see cref="IdleWindow"/>:</para>
/// <list type="bullet">
/// <item>no Prose client process is running: Prose.Mcp (one per Claude Code session, alive for
/// the session's whole life), Prose.Cli, Writer, KdpPublish, Launcher;</item>
/// <item>no HTTP request is in flight (a Writer page, the observer's SignalR stream or a long
/// command keeps the Hub up) and none has started within the window, health probes excepted so
/// a status poller cannot keep the Hub alive on its own;</item>
/// <item>no DCM run is active.</item>
/// </list>
/// <para>The window starts at Hub startup, so a freshly started Hub gets the full window for its
/// first client to connect. Config <c>Hub:IdleShutdownMinutes</c> (env
/// <c>Hub__IdleShutdownMinutes</c>) sets the window; 0 turns idle shutdown off. Default 5.</para>
/// </summary>
public sealed class HubIdleShutdown : BackgroundService
{
    public const string ConfigKey = "Hub:IdleShutdownMinutes";
    public const int DefaultMinutes = 5;

    /// <summary>Process names whose presence means a Prose session is open.</summary>
    public static readonly IReadOnlyList<string> ClientProcessNames =
        ["Prose.Mcp", "Prose.Cli", "Writer", "KdpPublish", "Launcher"];

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    private readonly IHostApplicationLifetime lifetime;
    private readonly ContextTelemetryService telemetry;
    private readonly ILogger<HubIdleShutdown> logger;
    private readonly Func<bool> clientProcessAlive;

    private int inFlight;
    private long lastActivityTicks = DateTime.UtcNow.Ticks;

    public TimeSpan IdleWindow { get; }

    public HubIdleShutdown(IConfiguration config, IHostApplicationLifetime lifetime,
        ContextTelemetryService telemetry, ILogger<HubIdleShutdown> logger)
        : this(ReadWindow(config), lifetime, telemetry, logger, AnyClientProcessAlive) { }

    internal HubIdleShutdown(TimeSpan idleWindow, IHostApplicationLifetime lifetime,
        ContextTelemetryService telemetry, ILogger<HubIdleShutdown> logger, Func<bool> clientProcessAlive)
    {
        IdleWindow = idleWindow;
        this.lifetime = lifetime;
        this.telemetry = telemetry;
        this.logger = logger;
        this.clientProcessAlive = clientProcessAlive;
    }

    public static TimeSpan ReadWindow(IConfiguration config)
    {
        var minutes = config.GetValue<int?>(ConfigKey) ?? DefaultMinutes;
        return TimeSpan.FromMinutes(Math.Max(0, minutes));
    }

    /// <summary>The pure decision: shut down only when idle on every signal for the whole window.</summary>
    public static bool ShouldShutDown(TimeSpan idleWindow, bool clientProcessAlive, int requestsInFlight,
        bool dcmRunActive, DateTime lastActivityUtc, DateTime nowUtc) =>
        idleWindow > TimeSpan.Zero
        && !clientProcessAlive
        && requestsInFlight == 0
        && !dcmRunActive
        && nowUtc - lastActivityUtc >= idleWindow;

    /// <summary>Middleware: counts in-flight requests and stamps activity. Health probes are
    /// neither counted nor stamped.</summary>
    public async Task TrackAsync(HttpContext context, Func<Task> next)
    {
        if (context.Request.Path.StartsWithSegments("/api/health"))
        {
            await next();
            return;
        }

        Touch();
        Interlocked.Increment(ref inFlight);
        try { await next(); }
        finally
        {
            Interlocked.Decrement(ref inFlight);
            Touch();
        }
    }

    internal int RequestsInFlight => Volatile.Read(ref inFlight);

    internal DateTime LastActivityUtc => new(Interlocked.Read(ref lastActivityTicks), DateTimeKind.Utc);

    internal void Touch() => Interlocked.Exchange(ref lastActivityTicks, DateTime.UtcNow.Ticks);

    /// <summary>One check; true when it asked the host to stop.</summary>
    internal bool CheckOnce(DateTime nowUtc)
    {
        var clientAlive = clientProcessAlive();
        // A live client resets the clock, so the window counts from the moment the last one left.
        if (clientAlive) Touch();

        if (!ShouldShutDown(IdleWindow, clientAlive, RequestsInFlight, telemetry.IsActive, LastActivityUtc, nowUtc))
            return false;

        var msg = $"[{DateTime.Now:HH:mm:ss}] *** Hub idle for {IdleWindow.TotalMinutes:0.#} min with no Prose session open - shutting down (the next session's hook restarts it) ***";
        HubConsoleEcho.Out.WriteLine(msg);
        logger.LogInformation("Hub idle shutdown: no Prose client process, no requests and no DCM run for {Minutes} min", IdleWindow.TotalMinutes);
        lifetime.StopApplication();
        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (IdleWindow <= TimeSpan.Zero)
        {
            logger.LogInformation("Hub idle shutdown disabled ({Key}=0)", ConfigKey);
            return;
        }

        using var timer = new PeriodicTimer(CheckInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    if (CheckOnce(DateTime.UtcNow)) return;
                }
                catch (Exception ex)
                {
                    // A failed check must never take the Hub down; it just tries again next tick.
                    logger.LogWarning(ex, "Hub idle check failed");
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private static bool AnyClientProcessAlive()
    {
        foreach (var name in ClientProcessNames)
        {
            var procs = Process.GetProcessesByName(name);
            try
            {
                if (procs.Length > 0) return true;
            }
            finally
            {
                foreach (var p in procs) p.Dispose();
            }
        }
        return false;
    }
}
