using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// CLI surface for <see cref="AskService"/> — hybrid RAG over the canon.
///
///   ss --ask "Question"                            free-form Q&amp;A (entities + story prose)
///   ss --ask "Question" --k 12                     more retrieved entity context
///   ss --ask "Question" --type character           restrict entity retrieval to one EntityType
///   ss --ask "Question" --strand &lt;slug&gt;            scope the answer to one story's beats
///   ss --ask "Question" --book &lt;strand-slug&gt;       alias for --strand
///
/// When scoped to a strand, that strand's beats are embedded (drift-skipped) and
/// its full text is used as context, so questions about one story are answered
/// exhaustively rather than from a sample.
/// </summary>
public static class AskCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var idx = Array.IndexOf(args, "--ask");
        var question = idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
        if (string.IsNullOrWhiteSpace(question) || question.StartsWith("--"))
        {
            Console.Error.WriteLine("usage: ss --ask \"Your question\" [--k 8] [--type character] [--strand <slug>]");
            return 1;
        }

        int k = 8;
        var kIdx = Array.IndexOf(args, "--k");
        if (kIdx >= 0 && kIdx + 1 < args.Length && int.TryParse(args[kIdx + 1], out var parsed)) k = parsed;

        string[]? types = null;
        var tIdx = Array.IndexOf(args, "--type");
        if (tIdx >= 0 && tIdx + 1 < args.Length) types = new[] { args[tIdx + 1] };

        // --strand (or --book) scopes the prose side to a single story.
        string? scopeSlug = null;
        var sIdx = Array.IndexOf(args, "--strand");
        if (sIdx < 0) sIdx = Array.IndexOf(args, "--book");
        if (sIdx >= 0 && sIdx + 1 < args.Length) scopeSlug = args[sIdx + 1];

        Guid? strandScope = null;
        if (!string.IsNullOrWhiteSpace(scopeSlug))
        {
            var dbFactory = sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync();
            var sid = await db.Strands
                .Where(s => s.Slug == scopeSlug)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync();
            if (sid is null)
            {
                Console.Error.WriteLine($"no strand found with slug '{scopeSlug}'");
                return 1;
            }
            strandScope = sid;

            // Embed the strand's beats (drift-skipped) so the global prose index
            // also covers this story for future unscoped questions. Cheap.
            var embeddings = sp.GetRequiredService<EmbeddingService>();
            Console.WriteLine($"[ask] indexing story beats for '{scopeSlug}' (drift-skipped)…");
            var embedded = await embeddings.ReembedStrandBeatsAsync(sid.Value);
            Console.WriteLine($"[ask] {(embedded == 0 ? "already current" : embedded + " beat(s) embedded")}");
        }

        var svc = sp.GetRequiredService<AskService>();
        Console.WriteLine(strandScope is null ? $"[ask] retrieving top-{k}…" : "[ask] reading the full story…");
        var result = await svc.AnswerAsync(question, retrieveK: k, entityTypes: types, strandScope: strandScope);

        Console.WriteLine();
        Console.WriteLine($"=== Answer ({result.CorpusChunks} chunks · {result.Duration.TotalSeconds:F1}s) ===");
        Console.WriteLine(result.Answer);
        Console.WriteLine();

        if (result.ProseCitations.Count > 0)
        {
            Console.WriteLine("=== Story passages ===");
            foreach (var p in result.ProseCitations)
            {
                var where = p.Position > 0 ? $"Ch {p.Position}" : "passage";
                var sim = p.Similarity >= 0.999 ? "" : $"  similarity={p.Similarity:F3}";
                Console.WriteLine($"  · {p.StrandTitle} — {where}{sim}");
            }
            Console.WriteLine();
        }

        if (result.Citations.Count > 0)
        {
            Console.WriteLine("=== Entity citations ===");
            foreach (var c in result.Citations)
                Console.WriteLine($"  · {c.EntityName}  ({c.EntityType})  similarity={c.Similarity:F3}");
        }
        return 0;
    }
}
