using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// CLI surface for <see cref="AskService"/> — cloud RAG over the canon.
///
///   ss --ask "Question"                       free-form Q&amp;A
///   ss --ask "Question" --k 12                more retrieved context
///   ss --ask "Question" --type character      restrict retrieval to one EntityType
/// </summary>
public static class AskCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var idx = Array.IndexOf(args, "--ask");
        var question = idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
        if (string.IsNullOrWhiteSpace(question) || question.StartsWith("--"))
        {
            Console.Error.WriteLine("usage: ss --ask \"Your question\" [--k 8] [--type character]");
            return 1;
        }

        int k = 8;
        var kIdx = Array.IndexOf(args, "--k");
        if (kIdx >= 0 && kIdx + 1 < args.Length && int.TryParse(args[kIdx + 1], out var parsed)) k = parsed;

        string[]? types = null;
        var tIdx = Array.IndexOf(args, "--type");
        if (tIdx >= 0 && tIdx + 1 < args.Length) types = new[] { args[tIdx + 1] };

        var svc = sp.GetRequiredService<AskService>();
        Console.WriteLine($"[ask] retrieving top-{k}…");
        var result = await svc.AnswerAsync(question, retrieveK: k, entityTypes: types);

        Console.WriteLine();
        Console.WriteLine($"=== Answer ({result.CorpusChunks} chunks · {result.Duration.TotalSeconds:F1}s) ===");
        Console.WriteLine(result.Answer);
        Console.WriteLine();
        if (result.Citations.Count > 0)
        {
            Console.WriteLine("=== Citations ===");
            foreach (var c in result.Citations)
                Console.WriteLine($"  · {c.EntityName}  ({c.EntityType})  similarity={c.Similarity:F3}");
        }
        return 0;
    }
}
