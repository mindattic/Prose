using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --make-group --name "Group B" [--size 128]</c> — create a fixed,
/// named reviewer panel of N enriched personas drawn at random but DISJOINT
/// from every existing focus group (no persona is on two panels). Cheap: just
/// sampling + a DB insert, no LLM calls. Reuse the panel later with
/// <c>--review-story --group "Group B"</c> to track that audience over versions.
/// Running several disjoint panels (A/B/C) gives independent replication —
/// more data mass, lower-variance, less-biased aggregates.
/// </summary>
public static class MakeGroupCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? name = null; int size = 128;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--name": if (i + 1 < args.Length) name = args[++i]; break;
                case "--size": if (i + 1 < args.Length && int.TryParse(args[++i], out var n)) size = n; break;
            }
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.Error.WriteLine("usage: ss --make-group --name \"Group B\" [--size 128]");
            return 1;
        }

        var reviewer = services.GetRequiredService<NodeReviewService>();
        try
        {
            var (id, count) = await reviewer.CreateDisjointGroupAsync(name!, size);
            Console.WriteLine($"[make-group] Created panel '{name}' with {count} personas (disjoint from all existing groups). Id {id}.");
            Console.WriteLine($"[make-group] Run it with:  ss --review-story --slug <slug> --group \"{name}\"");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[make-group] {ex.Message}");
            return 1;
        }
    }
}
