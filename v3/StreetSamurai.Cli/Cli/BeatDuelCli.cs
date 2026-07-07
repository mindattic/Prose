using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --duel --beat &lt;guid&gt; --candidate &lt;file&gt; [--goal "..."] [--apply] [--json]
///
/// Blind A/B duel between a beat's current prose and a candidate revision.
/// Round 1: 3 voters (register / structural-goal / cold-reader lenses),
/// three-way ballot. Replace needs ≥2 "better" with zero dissent; splits
/// escalate to a 7-voter panel with written rationales. Contested = keep —
/// a rewrite that can't win a clear majority isn't a clear improvement, and
/// the dissent rationales are the revision fuel for the next attempt.
///
/// --apply: on a "replace" verdict, write the candidate into the beat
/// (Text + TextHash recomputed + Stale=1). Without it, verdict only.
///
/// SS-A44: duels are votes — this command IS the explicit ask; invoking it
/// passes the allowVotes gate.
///
/// Exit codes: 0 = replace, 1 = keep, 2 = error.
/// </summary>
public static class BeatDuelCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? beatIdArg = null, candidatePath = null, goal = null;
        bool apply = args.Contains("--apply");
        bool jsonMode = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--beat-id":      beatIdArg = args[i + 1]; i++; break;
                case "--candidate": candidatePath = args[i + 1]; i++; break;
                case "--goal":      goal = args[i + 1]; i++; break;
            }
        }

        if (beatIdArg == null || candidatePath == null || !Guid.TryParse(beatIdArg, out var beatId))
        {
            Console.Error.WriteLine("Usage: ss --duel --beat-id <guid> --candidate <file> [--goal \"...\"] [--apply] [--json]");
            return 2;
        }
        if (!File.Exists(candidatePath))
        {
            Console.Error.WriteLine($"Candidate file not found: {candidatePath}");
            return 2;
        }

        var duelSvc   = services.GetRequiredService<BeatDuelService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        string originalText, storyTitle, precedingText = "";
        string? registerNotes = null;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId);
            if (beat == null || string.IsNullOrWhiteSpace(beat.Text))
            {
                Console.Error.WriteLine($"Beat {beatId} not found or has no prose.");
                return 2;
            }
            originalText = beat.Text;

            // Resolve owning story (walking up from the beat's owner chapter) for
            // title + register notes; pull the preceding beat for continuity context.
            var owner = await db.BeatNodes.AsNoTracking()
                .Where(bn => bn.BeatId == beatId)
                .Join(db.Nodes.AsNoTracking(), bn => bn.NodeId, n => n.Id, (bn, n) => new { bn.SortKey, Node = n })
                .FirstOrDefaultAsync();
            storyTitle = owner?.Node.Title ?? "(unknown story)";
            if (owner != null)
            {
                var story = owner.Node.ParentNodeId.HasValue
                    ? await db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == owner.Node.ParentNodeId.Value)
                    : owner.Node;
                if (story != null)
                {
                    storyTitle = story.Title;
                    registerNotes = story.NodeBible is { Length: > 0 } bible
                        ? (bible.Length <= 3000 ? bible : bible[..3000])
                        : null;
                }

                var prev = await db.BeatNodes.AsNoTracking()
                    .Where(bn => bn.NodeId == owner.Node.Id && bn.IsEnabled && bn.SortKey < owner.SortKey)
                    .OrderByDescending(bn => bn.SortKey)
                    .Join(db.Beats.AsNoTracking(), bn => bn.BeatId, b => b.Id, (bn, b) => b.Text)
                    .FirstOrDefaultAsync();
                if (prev is { Length: > 0 })
                    precedingText = prev.Length <= 1500 ? prev : prev[^1500..];
            }
        }

        var candidateText = (await File.ReadAllTextAsync(candidatePath)).Trim();
        if (candidateText.Length == 0)
        {
            Console.Error.WriteLine("Candidate file is empty.");
            return 2;
        }

        if (!jsonMode)
            Console.WriteLine($"Dueling beat {beatId.ToString()[..8]}… in '{storyTitle}' — 3 voters, blind A/B…\n");

        var result = await duelSvc.DuelAsync(
            originalText, candidateText,
            new DuelContext(storyTitle, goal, registerNotes, precedingText, beatId),
            allowVotes: true); // this command is the explicit ask (SS-A44)

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"Verdict: {(result.Replace ? "REPLACE — revision wins" : "KEEP — original stands")}" +
                              $"  ({result.BetterVotes} better / {result.WorseVotes} worse / {result.SameVotes} same" +
                              $"{(result.RoundsRun == 2 ? ", escalated to 7 voters" : "")}{(result.FromCache ? ", cached" : "")})");
            foreach (var b in result.Ballots)
                Console.WriteLine($"  [{b.Lens}] {b.Vote} ({b.Confidence:0.0}) — {b.Rationale}");
        }

        if (result.Replace && apply)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var beat = await db.Beats.FirstAsync(b => b.Id == beatId);
            beat.Text      = candidateText;
            beat.TextHash  = NodeWorkbenchService.ComputeTextHash(candidateText);
            beat.Stale     = true;
            beat.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            if (!jsonMode)
                Console.WriteLine("\n✅ Applied — beat text replaced, TextHash recomputed, marked stale for narration.");
        }
        else if (result.Replace && !apply && !jsonMode)
        {
            Console.WriteLine("\n(verdict only — re-run with --apply to write the replacement)");
        }
        else if (!result.Replace && result.RoundsRun == 2 && !jsonMode)
        {
            Console.WriteLine("\nContested — use the rationales above as revision fuel, then duel the next draft.");
        }

        return result.Replace ? 0 : 1;
    }
}
