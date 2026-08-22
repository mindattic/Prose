using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --migrate-nodes</c> — migrate legacy Books/Chapters/ChapterBeats/Episodes/
/// EpisodeBeats data into the unified Beat/Node schema. Idempotent — safe to re-run.
/// </summary>
public static class MigrateNodesCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var svc = services.GetRequiredService<NodeMigrationService>();
        var report = await svc.MigrateAllAsync();
        Console.WriteLine($"[migrate-nodes] Books={report.BooksAdded} Chapters={report.ChaptersAdded} Beats={report.BeatsAdded} Episodes={report.EpisodesAdded} Standalone={report.StandaloneBeatsAdded} Junctions={report.JunctionRowsAdded}");
        return 0;
    }
}
