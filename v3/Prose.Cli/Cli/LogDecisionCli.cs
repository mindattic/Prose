using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Cli;

/// <summary>
/// <c>prose --log-decision --summary "..." [--rationale "..."] [--category ...]
/// [--session ...] [--actor ...] [--related id1,id2]</c> — writes a Decision Ledger row.
///
/// The durable answer to "don't depend on fading context memory": any LLM (this assistant
/// included) can call this to record a higher-level decision or piece of reasoning as a
/// structured, permanent, queryable row — not just a mechanical command invocation (that's
/// <see cref="CommandLedgerEntry"/>, written automatically by Prose.Hub's dispatchers), but
/// the "why" behind one or more of them. A totally fresh session can query this back via
/// <c>prose --decision-log</c> instead of relying on a chat transcript.
/// </summary>
public static class LogDecisionCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? summary = null, rationale = null, category = null, sessionId = null, actor = null, related = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--summary":   if (i + 1 < args.Length) summary = args[++i]; break;
                case "--rationale": if (i + 1 < args.Length) rationale = args[++i]; break;
                case "--category":  if (i + 1 < args.Length) category = args[++i]; break;
                case "--session":   if (i + 1 < args.Length) sessionId = args[++i]; break;
                case "--actor":     if (i + 1 < args.Length) actor = args[++i]; break;
                case "--related":   if (i + 1 < args.Length) related = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(summary))
        {
            Console.Error.WriteLine("[log-decision] --summary is required.");
            return 1;
        }

        string? relatedJson = null;
        if (!string.IsNullOrWhiteSpace(related))
        {
            var ids = related.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            relatedJson = JsonSerializer.Serialize(ids);
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var entry = new DecisionLedgerEntry
        {
            SessionId = sessionId,
            Summary = summary,
            Rationale = rationale,
            Category = category,
            Actor = actor ?? "claude-code",
            RelatedCommandIdsJson = relatedJson,
        };
        db.DecisionLedgerEntries.Add(entry);
        await db.SaveChangesAsync();

        Console.WriteLine($"[log-decision] Recorded {entry.Id} ({entry.At:yyyy-MM-dd HH:mm:ss} UTC): {summary}");
        return 0;
    }
}
