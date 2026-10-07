using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.Hub;

/// <summary>
/// Follows a command's existing HubConsoleEcho log line with a one-sentence, Haiku-generated
/// plain-English gloss of what it did — "still write out the command, but follow that up with a
/// human readable description" (author, 2026-10-07). Off by default (<see
/// cref="SettingsService.HubNarrationEnabled"/>), toggleable at runtime via
/// <c>prose --hub-narration on|off</c> with no Hub restart.
///
/// Always fire-and-forget: this must never add latency to a tool response, and during a bulk
/// sweep (a logic-sweep or splice pass can fire hundreds of calls/minute) nothing should block
/// waiting on a narration line to land. It prints whenever the Haiku call returns, which may be
/// after the next command's own lines — acceptable, since this is a side annotation nothing
/// else depends on, not a thing the caller reads synchronously.
///
/// This is NOT an LLM judge: it never evaluates, scores, or votes on anything — it only
/// paraphrases a mechanical command line for a human reading the console. Does not touch the
/// factory's forbidden-instrument list.
/// </summary>
public sealed class CommandNarrator(ILlmService llm, SettingsService settings, ILogger<CommandNarrator> log)
{
    private const string SystemPrompt =
        "You turn one Prose Hub command log line into a single short plain-English sentence " +
        "describing what it did. One sentence, no preamble, no markdown, no quotes.";

    public void NarrateFireAndForget(
        string label, string redactedArgs, bool success, string? outputSummary, string? error, double durationMs)
    {
        if (!settings.HubNarrationEnabled) return;

        _ = Task.Run(async () =>
        {
            try
            {
                if (!await llm.IsConfiguredAsync())
                {
                    HubConsoleEcho.Narration(label, "(narration unavailable)");
                    return;
                }

                var user =
                    $"{label} args={Clip(redactedArgs, 300)} success={success} durationMs={durationMs:F0} " +
                    $"output={Clip(outputSummary, 300)} error={Clip(error, 200)}";

                var gloss = await llm.GenerateAsync(
                    SystemPrompt, user, temperature: 0.2, maxTokens: 60, model: LlmModels.Haiku);

                HubConsoleEcho.Narration(label, string.IsNullOrWhiteSpace(gloss) ? "(narration unavailable)" : gloss.Trim());
            }
            catch (Exception ex)
            {
                // Visible absence, never a silently wrong or silently missing line — RFC 0011
                // Brick 3's degraded-mode doctrine applied to this one low-stakes path too.
                HubConsoleEcho.Narration(label, "(narration unavailable)");
                log.LogWarning(ex, "Command narration failed for {Label}", label);
            }
        });
    }

    private static string Clip(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= max ? s : s[..max].TrimEnd() + "…";
    }
}
