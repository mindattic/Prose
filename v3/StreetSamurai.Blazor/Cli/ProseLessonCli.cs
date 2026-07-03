using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// Manages the prose-lessons memory store — author rulings that are injected
/// into review ballot prompts so reviewers don't penalise beats the author has
/// already ruled are doing their job.
///
/// <para>Usage:</para>
/// <list type="bullet">
/// <item><c>ss --lesson-add --scope &lt;scope&gt; --kind &lt;kind&gt; --text "&lt;text&gt;"</c><br/>
///   Adds a lesson. Scope examples: <c>global</c>, <c>node:my-node-slug</c>, <c>beat:&lt;guid&gt;</c>.<br/>
///   Kind examples: <c>score-vs-function</c>, <c>delight</c>, <c>voice</c>, <c>pacing</c>, <c>continuity</c>, <c>other</c>.</item>
/// <item><c>ss --lessons-list [--scope &lt;scope&gt;]</c><br/>
///   Lists all lessons, optionally filtered by scope prefix.</item>
/// </list>
/// </summary>
public static class ProseLessonCli
{
    public static Task<int> RunAddAsync(string[] args, IServiceProvider services)
    {
        string? scope = null, kind = null, text = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--scope" when i + 1 < args.Length: scope = args[++i]; break;
                case "--kind"  when i + 1 < args.Length: kind  = args[++i]; break;
                case "--text"  when i + 1 < args.Length: text  = args[++i]; break;
            }
        }

        // Accept trailing positional as text when --text wasn't given explicitly.
        if (string.IsNullOrWhiteSpace(text))
        {
            // Last bare arg that isn't a flag or the add/list verb itself.
            for (int i = args.Length - 1; i >= 0; i--)
            {
                if (!args[i].StartsWith('-'))
                {
                    text = args[i];
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(scope))
        {
            Console.Error.WriteLine("[lesson-add] --scope is required (e.g. global, node:<slug>, beat:<id>).");
            return Task.FromResult(1);
        }
        if (string.IsNullOrWhiteSpace(kind))
        {
            Console.Error.WriteLine("[lesson-add] --kind is required (e.g. score-vs-function, delight, voice, pacing, continuity, other).");
            return Task.FromResult(1);
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            Console.Error.WriteLine("[lesson-add] --text (or trailing positional) is required.");
            return Task.FromResult(1);
        }

        var store = services.GetRequiredService<ProseLessonStore>();
        store.Add(scope, kind, text);
        Console.WriteLine($"[lesson-add] OK — lesson added (scope={scope}, kind={kind}).");
        Console.WriteLine($"  \"{text}\"");
        return Task.FromResult(0);
    }

    public static Task<int> RunListAsync(string[] args, IServiceProvider services)
    {
        string? scopeFilter = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--scope" && i + 1 < args.Length)
                scopeFilter = args[++i];
        }

        var store = services.GetRequiredService<ProseLessonStore>();
        var all = store.ListAll();

        if (!string.IsNullOrWhiteSpace(scopeFilter))
            all = all.Where(l => l.Scope.StartsWith(scopeFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        if (all.Count == 0)
        {
            Console.WriteLine("[lessons-list] No lessons found.");
            return Task.FromResult(0);
        }

        Console.WriteLine($"[lessons-list] {all.Count} lesson(s):");
        foreach (var l in all.OrderBy(x => x.AddedAt))
        {
            Console.WriteLine($"  [{l.Kind}] scope={l.Scope}  id={l.Id}");
            Console.WriteLine($"    {l.Text}");
            Console.WriteLine($"    added={l.AddedAt:u}");
        }
        return Task.FromResult(0);
    }
}
