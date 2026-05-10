using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Builds the GlobalSearch index in the background at host start so the first
/// user-triggered Search doesn't pay the ~40 s cold deserialize-everything
/// cost. Fire-and-forget on a Task.Run — never blocks host startup. If the
/// build fails, the index will rebuild lazily on first use.
/// </summary>
public sealed class GlobalSearchWarmupService : IHostedService
{
    private readonly GlobalSearchService search;
    private readonly ILogger<GlobalSearchWarmupService> log;

    public GlobalSearchWarmupService(GlobalSearchService search, ILogger<GlobalSearchWarmupService> log)
    {
        this.search = search;
        this.log = log;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(() =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                search.WarmUp();
                log.LogInformation("GlobalSearch index warmed in {ElapsedMs} ms", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "GlobalSearch warm-up failed; index will rebuild lazily on first use");
            }
        }, cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
