using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --duplicate-entity-scan-broad --universe &lt;slug&gt; [--entity-type &lt;type&gt;] [--json]
///
/// LLM-assisted duplicate scan (report-only) for name variants the deterministic
/// <see cref="DuplicateEntityScanCli"/> structurally cannot catch — a title, rank/code suffix,
/// or otherwise different name for the same person ("Dame Lyra" vs. "Dame Lyra of House
/// Ocipheus"), not a 1-character typo. See <see cref="DuplicateEntityScanService.ScanBroadAsync"/>
/// for the two-stage cost-bounded design. Costs real LLM calls — gated the same way as other
/// LLM-calling commands (e.g. --generate-cover-prompt) via HubCliClient.ForwardWithCostGateAsync
/// in Program.cs, do not skip the gate.
///
/// Nothing is merged by this command — it only reports candidate groups + an LLM judge verdict
/// for a human to review before ever calling DuplicateEntityScanService.MergeAsync.
/// </summary>
public static class DuplicateEntityScanBroadCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var universeSlug = Flag(args, "--universe");
        var entityType = Flag(args, "--entity-type") ?? "character";
        bool jsonMode = args.Contains("--json");

        if (string.IsNullOrWhiteSpace(universeSlug))
        {
            Console.Error.WriteLine("Usage: prose --duplicate-entity-scan-broad --universe <slug> [--entity-type <type>] [--json]");
            return 2;
        }

        var canonDocs = services.GetRequiredService<CanonDocumentService>();
        var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
        if (universeId == null)
        {
            Console.Error.WriteLine($"[duplicate-entity-scan-broad] Unknown universe '{universeSlug}'.");
            return 2;
        }

        var scanSvc = services.GetRequiredService<DuplicateEntityScanService>();
        var groups = await scanSvc.ScanBroadAsync(universeId.Value, entityType);

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                universe = universeSlug,
                entity_type = entityType,
                group_count = groups.Count,
                groups = groups.Select(g => new
                {
                    verdict = new
                    {
                        same_entity = g.Verdict.SameEntity,
                        confidence = g.Verdict.Confidence,
                        suggested_winner_id = g.Verdict.SuggestedWinnerId,
                        reasoning = g.Verdict.Reasoning,
                    },
                    candidates = g.Candidates.Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        description_snippet = c.DescriptionSnippet,
                        mention_count = c.MentionCount,
                    }),
                }),
            }, new JsonSerializerOptions { WriteIndented = true }));
            return groups.Count > 0 ? 1 : 0;
        }

        Console.WriteLine($"Broad duplicate entity scan: {universeSlug} ({entityType} entities only)");
        Console.WriteLine();

        if (groups.Count == 0)
        {
            Console.WriteLine("✅ No duplicate-candidate clusters found.");
            return 0;
        }

        foreach (var g in groups)
        {
            var mark = g.Verdict.SameEntity ? "⚠️ " : "ℹ️ ";
            Console.WriteLine($"{mark}verdict={(g.Verdict.SameEntity ? "SAME ENTITY" : "different entities")} confidence={g.Verdict.Confidence}");
            Console.WriteLine($"   {g.Verdict.Reasoning}");
            foreach (var c in g.Candidates)
            {
                var winner = g.Verdict.SuggestedWinnerId == c.Id ? " [suggested winner]" : "";
                Console.WriteLine($"   - {c.Name}  [{c.MentionCount} beat mention(s)]{winner}  id={c.Id}");
                if (c.DescriptionSnippet != null)
                    Console.WriteLine($"     \"{c.DescriptionSnippet}\"");
            }
            Console.WriteLine();
        }

        Console.WriteLine(new string('─', 60));
        var confirmedSame = groups.Count(g => g.Verdict.SameEntity);
        Console.WriteLine($"⚠️  {groups.Count} candidate cluster(s), {confirmedSame} judged same-entity. " +
            "Report-only — read the actual prose before merging anything; the merge tool " +
            "(DuplicateEntityScanService.MergeAsync) is not invoked by this command.");

        return 1;
    }

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
