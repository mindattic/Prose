using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Background service that keeps <see cref="HomeStatsCache"/> populated.
/// Wakes on startup (immediate refresh), then every "Home stats refresh" minutes
/// (a setting, default 5) while a client has been active inside the "Home stats idle
/// pause" window (default 15 minutes); see <see cref="ShouldRefresh"/>. Each refresh runs the 26+
/// SELECT COUNT(*) queries the home-page tiles + /board sub-tiles need;
/// the page itself just reads from cache, no SQL on the request path.
///
/// Failure mode: if the DB is asleep or unreachable, the refresh logs a
/// warning and keeps the previous values. The next interval retries.
/// Worst case: counts are stale for one cycle. The cache never becomes
/// "negative", "null", or "exception" from the caller's perspective.
///
/// Threading: BackgroundService runs on the host's worker thread, so
/// long-running queries (cold serverless SQL = ~10s wake) don't block
/// the request pipeline.
/// </summary>
public class HomeStatsRefreshService : BackgroundService
{
    // Set BackgroundServices:Enabled=false in App Service config to stop DB keep-alive on zero-user deployments.
    public bool Enabled { get; }

    private readonly IServiceProvider sp;
    private readonly HomeStatsCache cache;
    private readonly ILogger<HomeStatsRefreshService> log;
    private readonly SettingsService settings;

    public HomeStatsRefreshService(
        IServiceProvider sp,
        HomeStatsCache cache,
        ILogger<HomeStatsRefreshService> log,
        IConfiguration configuration,
        SettingsService settings)
    {
        this.sp = sp;
        this.cache = cache;
        this.log = log;
        this.settings = settings;
        Enabled = configuration.GetValue<bool>("BackgroundServices:Enabled", defaultValue: true);
    }

    /// <summary>
    /// The pure decision for one tick (PRS-T1004): refresh only while the interval is on and a client has
    /// been active inside the idle window. An idle process must not query the database every few minutes,
    /// or a serverless database never pauses.
    /// </summary>
    public static bool ShouldRefresh(int refreshMinutes, int idleMinutes, DateTime lastActivityUtc, DateTime nowUtc) =>
        refreshMinutes > 0 && !ProseActivity.IsIdle(TimeSpan.FromMinutes(Math.Max(0, idleMinutes)), lastActivityUtc, nowUtc);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Enabled)
        {
            log.LogInformation("HomeStatsRefreshService disabled (BackgroundServices:Enabled=false).");
            return;
        }
        if (settings.HomeStatsRefreshMinutes <= 0)
        {
            log.LogInformation("HomeStatsRefreshService disabled (Home stats refresh = 0 minutes).");
            return;
        }

        // Run an immediate refresh so the first home-page hit after startup
        // doesn't see all zeros. Subsequent refreshes are interval-paced.
        await RefreshAsync(stoppingToken);

        var standingDown = false;
        while (!stoppingToken.IsCancellationRequested)
        {
            // Both numbers are settings read on every pass, so a change applies from the next tick.
            var minutes = settings.HomeStatsRefreshMinutes;
            if (minutes <= 0)
            {
                log.LogInformation("HomeStatsRefreshService stopped (Home stats refresh set to 0 minutes); it starts again with the process.");
                return;
            }
            try { await Task.Delay(TimeSpan.FromMinutes(minutes), stoppingToken); }
            catch (OperationCanceledException) { return; }

            if (!ShouldRefresh(settings.HomeStatsRefreshMinutes, settings.HomeStatsIdleMinutes,
                    ProseActivity.LastActivityUtc, DateTime.UtcNow))
            {
                if (!standingDown)
                    log.LogInformation("HomeStats refresh standing down: no client activity for {min} min. It resumes with the next request.",
                        settings.HomeStatsIdleMinutes);
                standingDown = true;
                continue;
            }
            standingDown = false;
            await RefreshAsync(stoppingToken);
        }
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // Scope a fresh DI scope for this refresh — repositories are
            // singletons but the DbContext factory + scoped services need
            // their own scope.
            using var scope = sp.CreateScope();
            var s = scope.ServiceProvider;

            // Pull each repository and ask it for COUNT(*). The repos already
            // do SELECT COUNT(*) under the hood (see [Use repo.Count() not
            // GetAll().Count] in project memory — that fix cut cold load from
            // 42s to <1s). We just batch them here.
            cache.Characters      = s.GetRequiredService<CharacterRepository>().Count();
            cache.Archetypes      = s.GetRequiredService<ArchetypeRepository>().Count();

            cache.Corponations    = s.GetRequiredService<CorponationRepository>().Count();
            cache.Subsidiaries    = s.GetRequiredService<SubsidiaryRepository>().Count();
            cache.Factions        = s.GetRequiredService<FactionRepository>().Count();

            cache.Weaponry        = s.GetRequiredService<WeaponryRepository>().Count();
            cache.Ammunition      = s.GetRequiredService<AmmunitionRepository>().Count();
            cache.Cyberware       = s.GetRequiredService<CyberwareRepository>().Count();
            cache.Equipment       = s.GetRequiredService<EquipmentRepository>().Count();
            cache.Apparel         = s.GetRequiredService<ApparelRepository>().Count();
            cache.Genemods        = s.GetRequiredService<GenemodRepository>().Count();
            cache.Pharmaceuticals = s.GetRequiredService<PharmaceuticalRepository>().Count();
            cache.Psionics        = s.GetRequiredService<PsionicRepository>().Count();

            cache.Places          = s.GetRequiredService<DistrictRepository>().Count();
            cache.Transportation  = s.GetRequiredService<TransportationRepository>().Count();
            cache.Materials       = s.GetRequiredService<MaterialRepository>().Count();
            cache.Technology      = s.GetRequiredService<TechnologyRepository>().Count();
            cache.Automata        = s.GetRequiredService<AutomatonRepository>().Count();
            cache.SyntheticLife   = s.GetRequiredService<SyntheticLifeRepository>().Count();
            cache.LabSpecimens    = s.GetRequiredService<LabSpecimenRepository>().Count();
            cache.Wasteland       = s.GetRequiredService<FlyoverEntityRepository>().Count();

            cache.Documents       = s.GetRequiredService<WorldbuildingDocRepository>().Count();
            cache.News            = s.GetRequiredService<NewsRepository>().Count();
            cache.Entertainment   = s.GetRequiredService<EntertainmentRepository>().Count();
            cache.ConsumerGoods   = s.GetRequiredService<ConsumerGoodRepository>().Count();

            cache.Contracts       = s.GetRequiredService<ContractRepository>().Count();
            cache.Quotes          = s.GetRequiredService<QuoteRepository>().Count();
            cache.Vocabulary      = s.GetRequiredService<VocabularyRepository>().Count();

            // Episodes lives on the DbContext directly, not behind a repo.
            // Short-lived context — open, count, dispose.
            var dbFactory = s.GetRequiredService<IDbContextFactory<ProseDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            cache.Episodes        = await db.Episodes.CountAsync(ct);

            cache.LastRefreshedAt    = DateTime.UtcNow;
            cache.LastRefreshDuration = sw.Elapsed;
            log.LogInformation(
                "HomeStats refreshed in {ms} ms (chars={chars}, gear={gear}, world={world}, episodes={ep})",
                sw.ElapsedMilliseconds, cache.CharactersTotal, cache.GearTotal, cache.WorldTotal, cache.Episodes);
        }
        catch (Exception ex)
        {
            // Don't let one bad refresh kill the service. Log + keep the
            // previous values; next interval will retry.
            log.LogWarning(ex,
                "HomeStats refresh failed after {ms} ms; keeping last known values. Will retry in {min} min.",
                sw.ElapsedMilliseconds, settings.HomeStatsRefreshMinutes);
        }
    }
}
