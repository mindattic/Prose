using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Generates N competing node bibles in parallel, scores each with a single
/// LLM call, commits the winner to the DB, and returns the winning node's Guid.
///
/// Usage: pass --compete N to ss --write-story. When N=1 (or omitted),
/// falls back to NodeBibleService directly (no scoring overhead).
/// </summary>
public class PremiseToOutlineService(
    IDbContextFactory<ProseDbContext> dbFactory,
    NodeBibleService bibleService,
    ILlmService llm)
{
    /// <summary>
    /// Create a node with the best outline from <paramref name="compete"/> competing bibles.
    /// Returns the new node's Guid + the winning bible text.
    /// </summary>
    public async Task<(Guid NodeId, string BibleText, int WinnerIndex)> CreateNodeAsync(
        string seed,
        string? title,
        string kind,
        int beats,
        int compete,
        CancellationToken ct = default)
    {
        compete = Math.Clamp(compete, 2, 5);

        var workingTitle = !string.IsNullOrEmpty(title) ? title : DeriveTitle(seed);

        Console.WriteLine($"[compete] Generating {compete} competing outlines in parallel…");

        // Generate all bibles concurrently
        var tasks = Enumerable.Range(0, compete)
            .Select(i => Task.Run(async () =>
            {
                var text = await bibleService.GenerateTextAsync(seed, workingTitle, beats, ct);
                Console.WriteLine($"[compete]   outline {i + 1}/{compete} done ({text.Length} chars).");
                return (Index: i, Text: text);
            }, ct))
            .ToList();

        var outlines = await Task.WhenAll(tasks);

        // Score each outline
        Console.WriteLine($"[compete] Scoring {compete} outlines…");
        var scores = await Task.WhenAll(outlines.Select(o => ScoreOutlineAsync(o.Index, o.Text, ct)));

        // Pick winner
        var winner = scores.OrderByDescending(s => s.Score).First();
        Console.WriteLine($"[compete] Winner: outline {winner.Index + 1} (score {winner.Score}/100)");
        for (int i = 0; i < scores.Length; i++)
            Console.WriteLine($"[compete]   {i + 1}. score={scores[i].Score,3}  {scores[i].Reason}");

        // Create the node and save the winning bible
        var nodeId = Guid.CreateVersion7();
        var slug = EpisodeGeneratorService.Slugify(workingTitle) + "-" + nodeId.ToString("N")[..8];

        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var node = NodeFactory.Create(kind);
            node.Id        = nodeId;
            node.Title     = workingTitle;
            node.Slug      = slug;
            node.Seed      = seed;
            node.Status    = "draft";
            node.Description  = seed.Length > 200 ? seed[..200] : seed;
            node.CreatedAt = DateTime.UtcNow;
            node.UpdatedAt = DateTime.UtcNow;
            db.Nodes.Add(node);
            await db.SaveChangesAsync(ct);
        }

        var winningBible = outlines[winner.Index].Text;
        await bibleService.SaveBibleAndCreateBeatsAsync(nodeId, winningBible, ct);

        return (nodeId, winningBible, winner.Index + 1);
    }

    private async Task<(int Index, int Score, string Reason)> ScoreOutlineAsync(
        int index, string bibleText, CancellationToken ct)
    {
        var system = """
            You are a story editor judging competing outlines for narrative quality.
            Score on three axes (each 0-33 points):
              1. Structural tension arc — does each act raise the stakes? Does it escalate to a clear climax?
              2. Character transformation completeness — does the protagonist clearly change from start to end?
              3. Payoff density — are the setups in the spine paid off by the final beats?

            Output exactly two lines:
            SCORE: <integer 0-100>
            REASON: <one sentence explaining the main strength or weakness>
            """;

        var raw = await llm.GenerateAsync(system, bibleText, temperature: 0.2, maxTokens: 120, ct: ct);

        var scoreMatch = System.Text.RegularExpressions.Regex.Match(raw, @"SCORE:\s*(\d+)");
        var reasonMatch = System.Text.RegularExpressions.Regex.Match(raw, @"REASON:\s*(.+)");

        var score = scoreMatch.Success && int.TryParse(scoreMatch.Groups[1].Value, out var s)
            ? Math.Clamp(s, 0, 100)
            : 50;
        var reason = reasonMatch.Success ? reasonMatch.Groups[1].Value.Trim() : "no reason";

        return (index, score, reason);
    }

    private static string DeriveTitle(string seed)
    {
        var words = seed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var raw   = string.Join(" ", words.Take(8));
        return raw.Length < seed.Length ? raw + "…" : raw;
    }
}
