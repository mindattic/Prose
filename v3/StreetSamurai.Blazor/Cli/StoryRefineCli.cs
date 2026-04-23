using System.Text.Json;
using StreetSamurai.Core;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// Headless CLI for the story refinement pass. Loads a saved story checkpoint,
/// runs the refinement analyzer, and writes structured notes as JSON.
///
/// Invocation:
///   dotnet run --project StreetSamurai.Blazor -- --refine-story &lt;projectId&gt; [-o notes.json]
///
/// Analysis-only — no rewrites. Notes are for human review in the WriteStory UI
/// (or whatever downstream tool reads refinement_report.json).
/// </summary>
public static class StoryRefineCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var parsed = ParsedArgs.Parse(args);
        if (parsed == null)
        {
            PrintUsage();
            return 1;
        }

        var director   = services.GetRequiredService<IStoryDirectorService>();
        var refinement = services.GetRequiredService<StoryRefinementService>();

        var story = director.LoadCheckpoint(parsed.ProjectId);
        if (story == null)
        {
            Console.Error.WriteLine($"[refine-story] no checkpoint found for project '{parsed.ProjectId}'");
            return 1;
        }
        if (!story.Complete)
        {
            Console.Error.WriteLine($"[refine-story] story is incomplete — refine a finished draft instead");
            return 1;
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        Console.Error.WriteLine($"[refine-story] analyzing '{story.Title}' — {story.Beats.Count} beats");
        var report = await refinement.AnalyzeAsync(story, cts.Token);

        if (!string.IsNullOrEmpty(report.Error))
        {
            Console.Error.WriteLine($"[refine-story] error: {report.Error}");
            return 2;
        }

        PrintSummary(report);

        var json = JsonSerializer.Serialize(report, JsonDefaults.Indented);

        if (!string.IsNullOrWhiteSpace(parsed.OutputPath))
        {
            await File.WriteAllTextAsync(parsed.OutputPath, json, cts.Token);
            Console.Error.WriteLine($"[refine-story] wrote {parsed.OutputPath}");
        }

        // JSON to stdout so callers can pipe: `... > notes.json`
        Console.Out.Write(json);
        return 0;
    }

    static void PrintSummary(RefinementReport r)
    {
        var byKind = r.Notes.GroupBy(n => n.Kind)
            .ToDictionary(g => g.Key, g => g.Count());

        Console.Error.WriteLine($"[refine-story] {r.Notes.Count} notes across {r.BeatsAnalyzed} beats");
        foreach (RefinementKind kind in Enum.GetValues<RefinementKind>())
        {
            var count = byKind.GetValueOrDefault(kind);
            if (count > 0) Console.Error.WriteLine($"  {kind,-15} {count}");
        }
    }

    static void PrintUsage() => Console.Error.WriteLine("""
        Usage:
          --refine-story <projectId> [-o notes.json]

        Args:
          projectId           The AutonomousStory.ProjectId (folder name in engine/stories/).
          -o, --output PATH   Optional: also write the JSON report to this file.

        The report is always saved to engine/stories/<folder>/refinement_report.json
        regardless of --output.
        """);

    class ParsedArgs
    {
        public string ProjectId = "";
        public string? OutputPath;

        public static ParsedArgs? Parse(string[] args)
        {
            var list = args.ToList();
            var idx = list.FindIndex(a => a == "--refine-story");
            if (idx < 0 || idx + 1 >= list.Count) return null;

            var p = new ParsedArgs { ProjectId = list[idx + 1] };
            if (string.IsNullOrWhiteSpace(p.ProjectId) || p.ProjectId.StartsWith("-")) return null;

            for (int i = idx + 2; i < list.Count; i++)
            {
                var arg = list[i];
                string? Next() => i + 1 < list.Count ? list[++i] : null;
                if (arg is "-o" or "--output") p.OutputPath = Next();
            }

            return p;
        }
    }
}
