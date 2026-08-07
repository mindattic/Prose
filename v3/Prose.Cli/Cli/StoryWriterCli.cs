using Prose.Core;
using Prose.Core.Interfaces;
using Prose.Core.Models;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI entry point for the story writer. Produces byte-identical output to Stories.razor
/// by calling the same StoryDirectorService methods and saving through the same repository.
///
/// Invocation:
///   dotnet run --project Prose.Blazor -- --write-story surpriseme [--protagonist NAME]
///   dotnet run --project Prose.Blazor -- --write-story guideme \
///     --protagonist NAME [--protagonist NAME2 ...] \
///     --synopsis "text" [--location NAME] [--beats N] [--format markdown|html] [--output PATH]
///
/// Protagonists are resolved by name, alias, or id — same lookup as the UI dropdown.
/// </summary>
public static class StoryWriterCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var parsed = ParsedArgs.Parse(args);
        if (parsed == null)
        {
            PrintUsage();
            return 1;
        }

        var director = services.GetRequiredService<IStoryDirectorService>();
        var db       = services.GetRequiredService<IDatabaseService>();
        var repo     = services.GetRequiredService<IChapterRepository>();

        // Resolve protagonist identifiers (name/alias/id) to canonical character names.
        var resolved = new List<string>();
        foreach (var ident in parsed.Protagonists)
        {
            var name = ResolveCharacterName(db, ident);
            if (name == null)
            {
                Console.Error.WriteLine($"[write-story] protagonist not found: '{ident}'");
                return 1;
            }
            if (!resolved.Contains(name, StringComparer.OrdinalIgnoreCase))
                resolved.Add(name);
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        // Stream progress to stderr so stdout stays clean for --output piping.
        director.OnProgress += p =>
            Console.Error.WriteLine($"[write-story] {p.CurrentBeat}/{p.TotalBeats} {p.Message}");

        AutonomousStory story;
        try
        {
            story = parsed.Mode switch
            {
                "surpriseme" when resolved.Count == 1 =>
                    await director.SurpriseMeForAsync(resolved[0], cts.Token),

                "surpriseme" =>
                    await director.SurpriseMeAsync(cts.Token),

                "guideme" =>
                    await director.GuidedStoryAsync(resolved, parsed.Synopsis!, parsed.Location, parsed.Beats, cts.Token),

                _ => throw new InvalidOperationException($"unknown mode: {parsed.Mode}")
            };
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("[write-story] cancelled");
            return 130;
        }

        if (!story.Complete)
        {
            Console.Error.WriteLine($"[write-story] generation incomplete: {story.FailureReason ?? "unknown reason"}");
            return 2;
        }

        // Save as a Chapter — same as the UI path in Stories.razor:
        // the ProjectId is reused so checkpoint, outline, and knowledge files stay associated.
        var body = parsed.Format == "html"
            ? AutonomousStoryFormatter.ToHtml(story)
            : AutonomousStoryFormatter.ToMarkdown(story);

        var chapter = new Chapter
        {
            Id         = story.ProjectId,
            Title      = story.Title,
            Characters = story.Characters,
            Html       = body,
            Status     = Constants.Status.Draft,
            Beats      = story.Beats.Select((b, i) => new ChapterBeat
            {
                Index         = i,
                Title         = b.Title,
                Text          = b.Text,
                Act           = b.Act,
                StructureRole = b.StructureRole,
                SceneType     = b.SceneType,
            }).ToList(),
        };
        repo.SaveChapter(chapter);

        if (!string.IsNullOrWhiteSpace(parsed.OutputPath))
        {
            await File.WriteAllTextAsync(parsed.OutputPath, body, cts.Token);
            Console.Error.WriteLine($"[write-story] wrote {parsed.OutputPath}");
        }

        Console.Error.WriteLine($"[write-story] saved project {story.ProjectId}");
        Console.Error.WriteLine($"[write-story] title: {story.Title}");
        Console.Error.WriteLine($"[write-story] protagonist: {story.Protagonist}");
        Console.Error.WriteLine($"[write-story] beats: {story.Beats.Count}");

        // Story body to stdout so callers can pipe: `... > story.md`
        Console.Out.Write(body);
        return 0;
    }

    static string? ResolveCharacterName(IDatabaseService db, string ident)
    {
        var byName = db.FindCharacter(ident);
        if (byName != null) return byName.Name;

        // Fall back to id lookup. FindCharacter only matches name/alias; an id won't match there.
        var byId = db.Characters
            .FirstOrDefault(c => string.Equals(c.Id, ident, StringComparison.OrdinalIgnoreCase));
        return byId?.Name;
    }

    static void PrintUsage()
    {
        Console.Error.WriteLine("""
            Usage:
              --write-story surpriseme [--protagonist NAME]
              --write-story guideme --protagonist NAME [--protagonist NAME2 ...] --synopsis "text" [options]

            Options:
              -p, --protagonist NAME     Character name, alias, or id (repeatable for guideme).
              -s, --synopsis TEXT        Story premise (required for guideme; min 20 chars).
              -l, --location NAME        Optional location (random if omitted).
              -b, --beats N              Beat count for guideme (default 8, clamped 3-16).
              -f, --format FMT           Output format: markdown (default) or html.
              -o, --output PATH          Write the story body to this file (also printed to stdout).

            Modes:
              surpriseme                 Fully autonomous. With --protagonist, forces that character as lead.
              guideme                    User-directed. Requires protagonist(s) + synopsis.
            """);
    }

    class ParsedArgs
    {
        public string Mode = "";
        public List<string> Protagonists = [];
        public string? Synopsis;
        public string? Location;
        public int Beats = 8;
        public string Format = "markdown";
        public string? OutputPath;

        public static ParsedArgs? Parse(string[] args)
        {
            // Strip the --write-story flag and read the mode token that follows.
            var list = args.ToList();
            var idx = list.FindIndex(a => a == "--write-story");
            if (idx < 0 || idx + 1 >= list.Count) return null;

            var p = new ParsedArgs { Mode = list[idx + 1].ToLowerInvariant() };
            if (p.Mode != "surpriseme" && p.Mode != "guideme") return null;

            for (int i = idx + 2; i < list.Count; i++)
            {
                var arg = list[i];
                string? Next() => i + 1 < list.Count ? list[++i] : null;

                switch (arg)
                {
                    case "-p":
                    case "--protagonist":
                        var name = Next(); if (name != null) p.Protagonists.Add(name); break;
                    case "-s":
                    case "--synopsis":
                        p.Synopsis = Next(); break;
                    case "-l":
                    case "--location":
                        p.Location = Next(); break;
                    case "-b":
                    case "--beats":
                        if (int.TryParse(Next(), out var n)) p.Beats = Math.Clamp(n, 3, 16); break;
                    case "-f":
                    case "--format":
                        var fmt = Next()?.ToLowerInvariant();
                        if (fmt is "markdown" or "html") p.Format = fmt;
                        break;
                    case "-o":
                    case "--output":
                        p.OutputPath = Next(); break;
                }
            }

            // Mode-specific validation
            if (p.Mode == "guideme")
            {
                if (p.Protagonists.Count == 0) return null;
                if (string.IsNullOrWhiteSpace(p.Synopsis) || p.Synopsis.Trim().Length < 20) return null;
            }
            else if (p.Mode == "surpriseme" && p.Protagonists.Count > 1)
            {
                // surpriseme accepts 0 or 1 protagonist. Multiple is ambiguous.
                return null;
            }

            return p;
        }
    }
}
