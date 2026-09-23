using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.Factory;

namespace Prose.Cli;

/// <summary>
/// The world's write path and station F1, CLI twin of MCP set_character_fields / verify_entity_*
/// (RFC 0015 §3.3–3.4). Runs in the Hub, so it works right after a Hub deploy.
///
///   prose --universe glmz --set-character-fields --id &lt;character id&gt; --file fields.json [--confirm-unread]
///   prose --universe glmz --verify-entity begin --entity &lt;id&gt; --node &lt;book&gt; [--text-budget N] [--out packet.json]
///   prose --universe glmz --verify-entity commit --nonce &lt;nonce&gt; [--by claude]
///
/// fields.json is one JSON object keyed by get_character's snake_case names; null clears a field.
/// Exit codes: 0 ok · 1 bad args / not found · 2 refused (cost not confirmed, a field did not land,
/// or the verification no longer matches the book).
/// </summary>
public static class WorldCli
{
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }

        if (args.Contains("--set-character-fields"))
        {
            var id = Flag("--id");
            var file = Flag("--file");
            if (string.IsNullOrWhiteSpace(id) || file == null || !File.Exists(file))
            {
                Console.Error.WriteLine("[world] usage: --set-character-fields --id <character id> --file fields.json [--confirm-unread]");
                return 1;
            }
            var r = await services.GetRequiredService<CharacterFieldWriter>()
                .SetFieldsAsync(id, await File.ReadAllTextAsync(file), args.Contains("--confirm-unread"));
            Console.WriteLine(JsonSerializer.Serialize(r, Json));
            if (r.Ok) Console.Error.WriteLine($"[world] {(r.Changed.Count == 0 ? "nothing changed" : "changed: " + string.Join(", ", r.Changed))}" +
                                              (r.UnreadCost > 0 ? $" · un-read {r.UnreadCost} beat(s): #{r.UnreadBeats}" : ""));
            else Console.Error.WriteLine($"[world] refused: {r.Error}");
            return r.Ok ? 0 : 2;
        }

        if (args.Contains("--verify-entity"))
        {
            var verifier = services.GetRequiredService<EntityVerificationService>();
            var i = Array.IndexOf(args, "--verify-entity");
            var verb = i + 1 < args.Length ? args[i + 1] : "";
            try
            {
                switch (verb)
                {
                    case "begin":
                    {
                        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
                        if (!Guid.TryParse(Flag("--entity"), out var entity)) { Console.Error.WriteLine("[verify] --entity <id> is required."); return 1; }
                        if (await NodeRefResolver.ResolveAsync(dbFactory, Flag("--node")) is not { } book) { Console.Error.WriteLine("[verify] --node <book> not found."); return 1; }
                        var budget = int.TryParse(Flag("--text-budget"), out var b) ? b : 40_000;
                        var p = await verifier.BeginAsync(entity, book, budget);
                        var json = JsonSerializer.Serialize(p, Json);
                        if (Flag("--out") is { } outPath) { await File.WriteAllTextAsync(outPath, json); Console.WriteLine($"[verify] packet written to {outPath}"); }
                        else Console.WriteLine(json);
                        Console.WriteLine($"[verify] {p.Name} ({p.EntityType}) in {p.Book}: {p.MentionCount} mention beat(s), {p.UnreadMentions} unread" +
                                          $"{(p.TextTruncated ? ", text truncated at the budget" : "")}. Commit before {p.ExpiresAt:u}:");
                        Console.WriteLine($"NONCE {p.Nonce}");
                        return 0;
                    }
                    case "commit":
                    {
                        var nonce = Flag("--nonce");
                        if (string.IsNullOrWhiteSpace(nonce)) { Console.Error.WriteLine("[verify] --nonce is required."); return 1; }
                        var row = await verifier.CommitAsync(nonce, Flag("--by") ?? "claude");
                        Console.WriteLine($"[verify] verified entity {row.EntityId} for book {row.BookId} at record {row.RecordModifiedAt:O} by {row.By}.");
                        return 0;
                    }
                    default:
                        Console.Error.WriteLine("[verify] usage: --verify-entity begin --entity <id> --node <book> | commit --nonce <nonce>");
                        return 1;
                }
            }
            catch (ArgumentException ex) { Console.Error.WriteLine($"[verify] {ex.Message}"); return 1; }
            catch (InvalidOperationException ex) { Console.Error.WriteLine($"[verify] refused: {ex.Message}"); return 2; }
        }

        Console.Error.WriteLine("[world] unknown command.");
        return 1;
    }
}
