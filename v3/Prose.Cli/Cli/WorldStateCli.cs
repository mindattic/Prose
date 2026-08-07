using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// ss --world-state --beat &lt;beatId&gt; [--story-time "2225-03-12"] [--json]
/// Prints the world-state snapshot (entity aspects + active edges) at the given beat.
/// </summary>
public static class WorldStateCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        Guid? beatId = null;
        DateTime? storyTime = null;
        bool json = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--beat":
                    if (Guid.TryParse(args[i + 1], out var g)) { beatId = g; i++; }
                    break;
                case "--story-time":
                    if (DateTime.TryParse(args[i + 1], out var dt)) storyTime = dt;
                    i++;
                    break;
            }
        }

        if (beatId == null)
        {
            Console.Error.WriteLine("Usage: ss --world-state --beat <beatId> [--story-time \"date\"] [--json]");
            return 1;
        }

        var svc = services.GetRequiredService<WorldStateAtBeatService>();
        var snapshot = await svc.SnapshotAsync(beatId.Value, storyTime);

        if (snapshot.StoryTime == null && snapshot.EntityStates.Count == 0 && snapshot.ActiveEdges.Count == 0)
        {
            Console.WriteLine("No world-state events found for this beat. Seed EntityStateEvents first.");
            return 0;
        }

        if (json)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                beatId = snapshot.BeatId,
                storyTime = snapshot.StoryTime,
                entityStates = snapshot.EntityStates.Select(s => new
                {
                    entity = s.EntityName,
                    aspect = s.AspectKey,
                    value = s.Value,
                    verb = s.Verb,
                }),
                activeEdges = snapshot.ActiveEdges.Select(e => new
                {
                    source = e.SourceName,
                    target = e.TargetName,
                    rel = e.RelationType,
                    sentiment = e.Sentiment,
                }),
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine(snapshot.FormatAsContextBlock());
            Console.WriteLine($"[{snapshot.EntityStates.Count} aspect(s), {snapshot.ActiveEdges.Count} edge(s)]");
        }

        return 0;
    }
}
