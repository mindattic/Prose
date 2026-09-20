using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.Obligations;

namespace Prose.Cli;

/// <summary>
/// prose --ground-entity-records --slug &lt;slug&gt; [--entity "Mrs. Chen"] [--json] [--no-findings]
///
/// Metadata-versus-prose grounding audit (RFC 0013 D6e). Decomposes every character/place/faction
/// record tagged in the book into atomic claims and checks each against the beats with a
/// quote-gated entailment call. Unentailed claims are filed under EntityDrift
/// (node:{slug}#recordground) and matching non-authored ledger claims are quarantined to
/// `inferred`. The record text is never edited here — the author accepts or strikes.
/// Cost-gated (Haiku; a few cents per entity).
/// </summary>
public static class GroundEntityRecordsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1]; return null; }
        var slug = Flag("--slug");
        var entity = Flag("--entity");
        var json = args.Contains("--json");
        if (string.IsNullOrWhiteSpace(slug)) { Console.Error.WriteLine("Usage: prose --ground-entity-records --slug <slug> [--entity NAME] [--json]"); return 2; }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var svc = services.GetRequiredService<EntityRecordGroundingService>();

        Guid root;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var resolved = await NodeRefResolver.ResolveAsync(db, slug);
            if (resolved == null) { Console.Error.WriteLine($"[ground-entity-records] {NodeRefResolver.NotFoundMessage(slug)}"); return 1; }
            root = resolved.Value;
            for (var depth = 0; depth < 10; depth++)
            {
                var parent = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.Id == root).Select(n => n.ParentNodeId).FirstOrDefaultAsync();
                if (parent == null) break;
                root = parent.Value;
            }
        }

        var report = await svc.RunAsync(root, entity, writeFindings: !args.Contains("--no-findings"));
        if (json) { Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })); return report.CouldNotLook ? 1 : 0; }

        Console.WriteLine($"[ground-entity-records] {report.Slug} — examined {report.EntitiesExamined} entity record(s), {report.Claims} atomic claim(s): entailed {report.Entailed}, contradicted {report.Contradicted}, not found {report.NotFound} ({report.UnentailedPct:P0} unentailed) · {report.ClaimsDowngraded} ledger claim(s) quarantined · {report.LlmCalls} LLM call(s)");
        if (report.CouldNotLook) { Console.WriteLine("  COULD NOT LOOK — no tagged entities with records in this book."); return 1; }
        foreach (var e in report.Entities.OrderByDescending(e => e.NotFound + e.Contradicted))
        {
            Console.WriteLine($"  {e.Name} ({e.EntityType}): {e.Claims} claims — entailed {e.Entailed}, contradicted {e.Contradicted}, not found {e.NotFound}");
            foreach (var v in e.Verdicts.Where(v => v.Verdict != "entailed").Take(12))
                Console.WriteLine($"      {v.Verdict,-12} {v.Field}: {v.Claim}{(v.Quote != null ? $"  ⟵ \"{v.Quote}\"" : "")}");
        }
        return 0;
    }
}
