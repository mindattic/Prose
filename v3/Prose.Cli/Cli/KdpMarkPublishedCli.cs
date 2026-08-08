using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --kdp-mark-published --slug &lt;slug&gt; [--url &lt;amazonUrl&gt;] [--title-id &lt;id&gt;]</c>
/// — thin wrapper over <see cref="KdpMarkPublishedService"/> (shared with the KdpPublish app's
/// <c>mark_published</c> tool). Parses args, calls the service, prints the result.
/// </summary>
public static class KdpMarkPublishedCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, url = null, titleId = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":     if (i + 1 < args.Length) slug = args[++i]; break;
                case "--url":      if (i + 1 < args.Length) url = args[++i]; break;
                case "--title-id": if (i + 1 < args.Length) titleId = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[kdp-mark-published] --slug is required.");
            return 1;
        }

        var service = services.GetRequiredService<KdpMarkPublishedService>();
        var repoRoot = KdpManifestService.FindRepoRoot();
        var result = await service.MarkPublishedAsync(slug, url, titleId, repoRoot);

        if (!result.Ok)
        {
            Console.Error.WriteLine($"[kdp-mark-published] {result.Error}");
            return 1;
        }

        Console.WriteLine($"[kdp-mark-published] {result.Code} \"{result.Title}\" marked Published at {result.KdpPublishedAt:yyyy-MM-dd HH:mm} UTC.");
        if (result.PublishUrl != null) Console.WriteLine($"[kdp-mark-published] PublishUrl: {result.PublishUrl}");
        if (result.RecordedTitleId != null) Console.WriteLine($"[kdp-mark-published] Recorded titleId '{result.RecordedTitleId}' for {result.Code} in tools/kdp/title-ids.json");

        return 0;
    }
}
