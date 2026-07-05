using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// Claude Code <c>UserPromptSubmit</c> hook backend for the Doc Context Stack. Reads the hook's
/// JSON payload on stdin, surfaces the topic <c>.md</c> docs pertinent to the user's latest message
/// (keyword-only — no per-turn embedding API call, for speed), and emits a hook JSON document whose
/// <c>additionalContext</c> injects the rotating cast for this turn. ALWAYS exits 0 and emits valid
/// JSON ({} on any error or no match) so a failure can never block the prompt.
///
///   ss --doc-context-hook        (stdin = Claude Code hook payload)
/// </summary>
public static class DocContextHookCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        try
        {
            var input = await Console.In.ReadToEndAsync();
            string prompt = "", sessionId = "";
            try
            {
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(input) ? "{}" : input);
                if (doc.RootElement.TryGetProperty("prompt", out var p)) prompt = p.GetString() ?? "";
                if (doc.RootElement.TryGetProperty("session_id", out var s)) sessionId = s.GetString() ?? "";
            }
            catch { /* malformed payload → no-op */ }

            if (prompt.Trim().Length < 4) { Console.Out.Write("{}"); return 0; }

            var key = Guid.TryParse(sessionId, out var g) ? g : SessionKey(sessionId);
            var svc = sp.GetRequiredService<DocContextService>();
            var result = await svc.PrepareSessionContextAsync(key, prompt, tokenBudget: 1200, useEmbedding: false);

            if (result.Loaded.Count == 0) { Console.Out.Write("{}"); return 0; }

            var additional =
                "Doc Context Stack — canon .md docs pertinent to this message (loaded on demand, not the full corpus):\n\n"
                + result.Block;

            var payload = new
            {
                hookSpecificOutput = new
                {
                    hookEventName = "UserPromptSubmit",
                    additionalContext = additional,
                },
            };
            // JsonSerializer default encoder escapes non-ASCII to \uXXXX — safe for the hook channel.
            Console.Out.Write(JsonSerializer.Serialize(payload));
            return 0;
        }
        catch
        {
            Console.Out.Write("{}");
            return 0;
        }
    }

    private static Guid SessionKey(string s) =>
        new(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes("doc-hook:" + s)));
}
