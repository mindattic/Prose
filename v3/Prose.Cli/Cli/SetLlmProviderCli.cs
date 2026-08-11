using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --set-llm-provider claude-api|claude-team [--dry-run]
///
/// Switches every Settings.json field that governs which Claude credential path the app uses,
/// in one command. Built 2026-08-11 after discovering ClaudeService/LlmRouter/ReviewLlmTransport
/// already fully support a "claude-team" provider (MindAttic.Legion's ClaudeCodeOAuthSource —
/// authenticates via ~/.claude/.credentials.json, the Claude Code CLI's own OAuth session, riding
/// the Team subscription's inference quota instead of pay-per-token API billing) but the app was
/// pinned to the exhausted "claude-api" key by a single settings value with no CLI toggle.
///
/// Fields touched:
///   - ActiveLlmProvider: always set directly to the target (this is the primary, explicit choice).
///   - ReviewJudgeProvider: swapped only if it currently holds the OTHER Claude variant (a judge
///     intentionally set to e.g. "gemini" is left alone).
///   - ReviewAllowedProviders / ReaderQaJuryProviders: comma-separated lists — only entries equal
///     to the OTHER Claude variant are swapped to the target; other providers in the list (openai,
///     gemini, deepseek, kimi, ...) and the list's shape are left untouched.
/// </summary>
public static class SetLlmProviderCli
{
    private const string ClaudeApi = "claude-api";
    private const string ClaudeTeam = "claude-team";

    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dryRun = args.Contains("--dry-run");
        var idx = Array.IndexOf(args, "--set-llm-provider");
        var target = idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;

        if (target != ClaudeApi && target != ClaudeTeam)
        {
            Console.Error.WriteLine($"[set-llm-provider] --set-llm-provider requires exactly '{ClaudeApi}' or '{ClaudeTeam}'.");
            Console.Error.WriteLine($"Usage: prose --set-llm-provider {ClaudeApi}|{ClaudeTeam} [--dry-run]");
            return Task.FromResult(2);
        }

        var other = target == ClaudeApi ? ClaudeTeam : ClaudeApi;
        var settings = services.GetRequiredService<SettingsService>();
        var changes = new List<string>();

        if (settings.ActiveLlmProvider != target)
        {
            changes.Add($"ActiveLlmProvider: {settings.ActiveLlmProvider} -> {target}");
            if (!dryRun) settings.ActiveLlmProvider = target;
        }

        if (settings.ReviewJudgeProvider == other)
        {
            changes.Add($"ReviewJudgeProvider: {other} -> {target}");
            if (!dryRun) settings.ReviewJudgeProvider = target;
        }

        var newAllowed = SwapInList(settings.ReviewAllowedProviders, other, target);
        if (newAllowed != settings.ReviewAllowedProviders)
        {
            changes.Add($"ReviewAllowedProviders: {settings.ReviewAllowedProviders} -> {newAllowed}");
            if (!dryRun) settings.ReviewAllowedProviders = newAllowed;
        }

        var newJury = SwapInList(settings.ReaderQaJuryProviders, other, target);
        if (newJury != settings.ReaderQaJuryProviders)
        {
            changes.Add($"ReaderQaJuryProviders: {settings.ReaderQaJuryProviders} -> {newJury}");
            if (!dryRun) settings.ReaderQaJuryProviders = newJury;
        }

        if (changes.Count == 0)
        {
            Console.WriteLine($"[set-llm-provider] Already fully on '{target}' — no fields needed changing.");
            return Task.FromResult(0);
        }

        foreach (var c in changes) Console.WriteLine($"[set-llm-provider] {c}");

        if (dryRun)
        {
            Console.WriteLine("(DRY RUN — no changes written)");
            return Task.FromResult(0);
        }

        // CLI process is short-lived — force the write now rather than trust the 500ms debounce
        // timer to fire before Main returns.
        settings.Flush();
        Console.WriteLine($"[set-llm-provider] Switched to '{target}'. Settings.json updated.");
        return Task.FromResult(0);
    }

    /// <summary>Replace any comma-separated entry equal to <paramref name="from"/> with
    /// <paramref name="to"/>. Entries for other providers, whitespace, and list order are
    /// preserved untouched. Returns the input unchanged if <paramref name="from"/> never appears.</summary>
    private static string SwapInList(string csv, string from, string to)
    {
        if (string.IsNullOrWhiteSpace(csv)) return csv;
        var parts = csv.Split(',');
        var changed = false;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Trim() == from)
            {
                parts[i] = to;
                changed = true;
            }
        }
        return changed ? string.Join(",", parts) : csv;
    }
}
