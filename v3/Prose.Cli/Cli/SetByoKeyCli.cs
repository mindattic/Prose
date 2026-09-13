using Prose.Core.Services.Operator;

namespace Prose.Cli;

/// <summary>
/// prose --set-byo-key --provider claude|openai
///   (--key &lt;apiKey&gt; [--key &lt;apiKey&gt; ...] | --add-key &lt;apiKey&gt; | --remove-key &lt;apiKey&gt;
///    | --list | --clear)
/// — manages the personal API-key pool the operator tool-calling loop
/// (<see cref="AnthropicToolCallingLlm"/>/<see cref="OpenAiToolCallingLlm"/>, used by
/// <see cref="Prose.Core.Services.Operator.KdpOperatorService"/> today) tries BEFORE its own
/// default credential chain, failing over key-to-key on an auth/rate-limit/server error
/// (<see cref="KeyPoolFailover"/>). One or more <c>--key</c> flags REPLACE the whole pool;
/// <c>--add-key</c>/<c>--remove-key</c> edit it incrementally; <c>--list</c> shows what's
/// configured (masked) without changing anything. Backed by
/// <see cref="OperatorByoKeyPoolService"/> — see <see cref="OperatorByoKeys"/>'s doc comment for
/// why this exists and what it does (and does not) change about Claude's "never silently spend
/// the shared Vault key" guard.
/// </summary>
public static class SetByoKeyCli
{
    public static int Run(string[] args, IServiceProvider services)
    {
        string? provider = null;
        var keys = new List<string>();
        string? addKey = null, removeKey = null;
        var clear = false;
        var list = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--provider":    if (i + 1 < args.Length) provider = args[++i]; break;
                case "--key":         if (i + 1 < args.Length) keys.Add(args[++i]); break;
                case "--add-key":     if (i + 1 < args.Length) addKey = args[++i]; break;
                case "--remove-key":  if (i + 1 < args.Length) removeKey = args[++i]; break;
                case "--clear":       clear = true; break;
                case "--list":        list = true; break;
            }
        }

        if (provider is not ("claude" or "openai"))
        {
            Console.Error.WriteLine("[set-byo-key] --provider claude|openai is required.");
            return 1;
        }

        var pool = services.GetRequiredService<OperatorByoKeyPoolService>();

        if (list)
        {
            var current = pool.GetPool(provider);
            Console.WriteLine(current.Count == 0
                ? $"[set-byo-key] {provider}: no BYO key configured — using the default credential chain."
                : $"[set-byo-key] {provider}: {current.Count} key(s), tried in order:\n" +
                  string.Join("\n", current.Select((k, idx) => $"  {idx + 1}. {Mask(k)}")));
            return 0;
        }

        if (clear)
        {
            pool.Clear(provider);
            Console.WriteLine($"[set-byo-key] {provider} BYO key(s) cleared — falling back to the default credential chain.");
            return 0;
        }

        if (addKey is not null)
        {
            pool.AddKey(provider, addKey);
            Console.WriteLine($"[set-byo-key] {provider}: added {Mask(addKey)} — pool now has {pool.GetPool(provider).Count} key(s).");
            return 0;
        }

        if (removeKey is not null)
        {
            var removed = pool.RemoveKey(provider, removeKey);
            Console.WriteLine(removed
                ? $"[set-byo-key] {provider}: removed {Mask(removeKey)} — pool now has {pool.GetPool(provider).Count} key(s)."
                : $"[set-byo-key] {provider}: {Mask(removeKey)} was not in the pool — nothing changed.");
            return removed ? 0 : 1;
        }

        if (keys.Count == 0)
        {
            Console.Error.WriteLine("[set-byo-key] one of --key <apiKey> (repeatable), --add-key, --remove-key, --list, or --clear is required.");
            return 1;
        }

        pool.SetPool(provider, keys);
        Console.WriteLine(keys.Count == 1
            ? $"[set-byo-key] {provider} BYO key saved — tried before the default credential chain from now on."
            : $"[set-byo-key] {provider} BYO key pool saved ({keys.Count} keys) — tried in order, failing over on error.");
        return 0;
    }

    private static string Mask(string key) => key.Length >= 4 ? "…" + key[^4..] : "(short key)";
}
