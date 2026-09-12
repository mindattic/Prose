using Prose.Core.Services;
using Prose.Core.Services.Operator;

namespace Prose.Cli;

/// <summary>
/// prose --set-byo-key --provider claude|openai (--key &lt;apiKey&gt; | --clear)
/// — sets or clears a personal API key the operator tool-calling loop
/// (<see cref="AnthropicToolCallingLlm"/>/<see cref="OpenAiToolCallingLlm"/>, used by
/// <see cref="Prose.Core.Services.Operator.KdpOperatorService"/> today) tries BEFORE its own
/// default credential chain. Stored via <see cref="SettingsKvStore"/> as
/// <see cref="OperatorByoKeys"/> under "operator.byokeys" — see that record's doc comment for why
/// this exists and what it does (and does not) change about Claude's "never silently spend the
/// shared Vault key" guard.
/// </summary>
public static class SetByoKeyCli
{
    public static int Run(string[] args, IServiceProvider services)
    {
        string? provider = null, key = null;
        var clear = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--provider": if (i + 1 < args.Length) provider = args[++i]; break;
                case "--key":      if (i + 1 < args.Length) key = args[++i]; break;
                case "--clear":    clear = true; break;
            }
        }

        if (provider is not ("claude" or "openai"))
        {
            Console.Error.WriteLine("[set-byo-key] --provider claude|openai is required.");
            return 1;
        }
        if (!clear && string.IsNullOrWhiteSpace(key))
        {
            Console.Error.WriteLine("[set-byo-key] --key <apiKey> is required (or --clear to remove it).");
            return 1;
        }

        var kv = services.GetRequiredService<SettingsKvStore>();
        var current = kv.Get<OperatorByoKeys>("operator.byokeys") ?? new OperatorByoKeys();
        var updated = provider == "claude"
            ? current with { AnthropicApiKey = clear ? null : key }
            : current with { OpenAiApiKey = clear ? null : key };
        kv.Set("operator.byokeys", updated);

        Console.WriteLine(clear
            ? $"[set-byo-key] {provider} BYO key cleared — falling back to the default credential chain."
            : $"[set-byo-key] {provider} BYO key saved — tried before the default credential chain from now on.");
        return 0;
    }
}
