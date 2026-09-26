using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services.Spelling;

namespace Prose.Cli;

/// <summary>
/// The author's spelling dictionary, from the command line. The same rows the Writer's
/// "Add to dictionary" and Settings edit, and the dictionary MCP tools.
///
/// Usage:
///   prose --dictionary list
///   prose --dictionary add &lt;word&gt; [--by author|session:&lt;id&gt;]
///   prose --dictionary remove &lt;word&gt;
///   prose --dictionary check &lt;word&gt; [&lt;word&gt; …]    (misspelled or not, and suggestions)
///
/// One entry covers its inflections: adding CorpoNation also accepts CorpoNations, CorpoNation's
/// and CorpoNations'. Exit codes: 0 ok · 1 bad args.
/// </summary>
public static class SpellingCli
{
    public static async Task<int> RunAsync(IReadOnlyList<string> args, IServiceProvider sp)
    {
        var spelling = sp.GetRequiredService<SpellingService>();
        var at = args.ToList().IndexOf("--dictionary");
        var verb = at >= 0 && at + 1 < args.Count ? args[at + 1] : "";
        var rest = at >= 0 ? args.Skip(at + 2).TakeWhile(a => !a.StartsWith("--", StringComparison.Ordinal)).ToList() : [];
        string? Flag(string name) { var i = args.ToList().IndexOf(name); return i >= 0 && i + 1 < args.Count ? args[i + 1] : null; }
        var word = Flag("--word") ?? rest.FirstOrDefault();

        switch (verb)
        {
            case "list":
            {
                var rows = await spelling.ListAsync();
                foreach (var r in rows) Console.WriteLine($"{r.Word,-32} {r.AddedBy,-16} {r.AddedAt:yyyy-MM-dd}");
                Console.WriteLine($"[dictionary] {rows.Count} word(s).");
                return 0;
            }
            case "add":
            {
                if (string.IsNullOrWhiteSpace(word)) { Console.Error.WriteLine("[dictionary] usage: prose --dictionary add <word>"); return 1; }
                try
                {
                    var r = await spelling.AddAsync(word, Flag("--by") ?? "cli");
                    Console.WriteLine(r.Added ? $"[dictionary] added '{r.Row.Word}'." : $"[dictionary] {r.Note}.");
                    return 0;
                }
                catch (ArgumentException ex) { Console.Error.WriteLine($"[dictionary] {ex.Message}"); return 1; }
            }
            case "remove":
            {
                if (string.IsNullOrWhiteSpace(word)) { Console.Error.WriteLine("[dictionary] usage: prose --dictionary remove <word>"); return 1; }
                Console.WriteLine(await spelling.RemoveAsync(word)
                    ? $"[dictionary] removed '{word}'."
                    : $"[dictionary] '{word}' is not in the dictionary.");
                return 0;
            }
            case "check":
            {
                if (rest.Count == 0) { Console.Error.WriteLine("[dictionary] usage: prose --dictionary check <word> [<word> …]"); return 1; }
                var bad = (await spelling.MisspelledAsync(rest)).ToHashSet(StringComparer.Ordinal);
                foreach (var w in rest.Distinct())
                {
                    if (!bad.Contains(w)) { Console.WriteLine($"{w}: ok"); continue; }
                    var suggestions = string.Join(", ", await spelling.SuggestAsync(w));
                    Console.WriteLine($"{w}: misspelled — {(suggestions.Length > 0 ? suggestions : "no suggestions")}");
                }
                return 0;
            }
            default:
                Console.Error.WriteLine("[dictionary] usage: prose --dictionary list | add <word> | remove <word> | check <word> …");
                return 1;
        }
    }
}
