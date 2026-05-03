using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// CLI surface for the local-LLM Q&amp;A primitive.
///
///   ss --ask "question"                 Answer a question against the live corpus.
///   ss --ask --reindex                  Walk the data tree and re-embed any changed files.
///   ss --ask --stats                    Show index size, last-indexed time, Ollama reachability.
/// </summary>
public static class AskCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var idx = Array.FindIndex(args, a => a == "--ask");
        if (idx < 0) { PrintUsage(); return 1; }
        var rest = args[(idx + 1)..];

        var index = services.GetRequiredService<EmbeddingIndexService>();
        var rag   = services.GetRequiredService<LocalRagService>();

        if (rest.Contains("--stats"))
        {
            var s = index.GetStats();
            Console.WriteLine($"[ask] files indexed: {s.FileCount}");
            Console.WriteLine($"[ask] chunks:        {s.ChunkCount}");
            Console.WriteLine($"[ask] last indexed:  {s.LastIndexed?.ToLocalTime():g}");
            Console.WriteLine($"[ask] ollama up:     {await index.OllamaReachableAsync()}");
            return 0;
        }

        if (rest.Contains("--reindex"))
        {
            Console.WriteLine("[ask] reindexing changed files…");
            var n = await index.ReindexAllAsync();
            Console.WriteLine($"[ask] re-embedded {n} file(s).");
            return 0;
        }

        // Question = positional args (joined). Handle quoted phrases too.
        var question = string.Join(' ', rest).Trim();
        if (string.IsNullOrWhiteSpace(question)) { PrintUsage(); return 1; }

        if (!await index.OllamaReachableAsync())
        {
            Console.Error.WriteLine("[ask] Ollama is not reachable at localhost:11434.");
            return 2;
        }

        var hits = await index.SearchAsync(question, k: 8);
        var answer = await rag.AnswerWithHitsAsync(question, hits);

        Console.WriteLine(answer);
        Console.WriteLine();
        Console.WriteLine("─── citations ───");
        foreach (var h in hits)
        {
            Console.WriteLine($"  {h.Score:F3}  {h.FilePath} · chunk {h.ChunkIndex}");
        }
        return 0;
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  ss --ask \"question\"        Ask the corpus.");
        Console.WriteLine("  ss --ask --reindex          Re-embed changed files.");
        Console.WriteLine("  ss --ask --stats            Show index status.");
    }
}
