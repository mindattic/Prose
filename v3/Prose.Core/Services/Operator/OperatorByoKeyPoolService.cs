namespace Prose.Core.Services.Operator;

/// <summary>
/// CRUD surface over the operator tool-calling loop's BYO-key pool (<see cref="OperatorByoKeys"/>,
/// stored via <see cref="SettingsKvStore"/> under "operator.byokeys" — a SHARED config key, not
/// per-universe, per <see cref="UniverseScope"/>.SharedConfigKeys). One place for this logic so
/// <c>prose --set-byo-key</c> (<see cref="Prose.Cli"/>) and the <c>OperatorKeyTools</c> MCP tools
/// (<see cref="Prose.Mcp"/>) can't drift apart, and so <see cref="Extensions.ServiceCollectionExtensions"/>'s
/// DI wiring for <see cref="AnthropicToolCallingLlm"/>/<see cref="OpenAiToolCallingLlm"/> reads
/// through the exact same resolution as any admin tool that just wrote a key.
/// </summary>
public class OperatorByoKeyPoolService
{
    private const string KvKey = "operator.byokeys";
    public static readonly string[] Providers = ["claude", "openai"];

    private readonly SettingsKvStore kv;

    public OperatorByoKeyPoolService(SettingsKvStore kv)
    {
        this.kv = kv;
    }

    private static void ValidateProvider(string provider)
    {
        if (!Providers.Contains(provider, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Unknown provider '{provider}' — expected one of: {string.Join(", ", Providers)}.", nameof(provider));
    }

    /// <summary>Every key currently configured for <paramref name="provider"/>, in priority order.</summary>
    public IReadOnlyList<string> GetPool(string provider)
    {
        ValidateProvider(provider);
        var current = kv.Get<OperatorByoKeys>(KvKey) ?? new OperatorByoKeys();
        return provider.Equals("claude", StringComparison.OrdinalIgnoreCase)
            ? current.ResolvedAnthropicKeys
            : current.ResolvedOpenAiKeys;
    }

    /// <summary>Replaces the whole pool for <paramref name="provider"/>. Blank/whitespace entries
    /// are dropped; an empty result clears the provider the same way <see cref="Clear"/> does.</summary>
    public void SetPool(string provider, IReadOnlyList<string> keys)
    {
        ValidateProvider(provider);
        var cleaned = keys.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()).ToList();
        var current = kv.Get<OperatorByoKeys>(KvKey) ?? new OperatorByoKeys();
        var updated = provider.Equals("claude", StringComparison.OrdinalIgnoreCase)
            // Clear the legacy singular field too so a pool write is never shadowed by a stale
            // pre-pool value on the next read (ResolvedAnthropicKeys/ResolvedOpenAiKeys prefer the
            // pool field, but only when it's non-empty — an emptied pool must not fall through).
            ? current with { AnthropicApiKey = null, AnthropicApiKeys = cleaned }
            : current with { OpenAiApiKey = null, OpenAiApiKeys = cleaned };
        kv.Set(KvKey, updated);
    }

    /// <summary>Appends one key to the end of the pool (tried last). No-op if it's already present.</summary>
    public void AddKey(string provider, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key must not be blank.", nameof(key));
        var pool = GetPool(provider).ToList();
        if (!pool.Contains(key.Trim(), StringComparer.Ordinal)) pool.Add(key.Trim());
        SetPool(provider, pool);
    }

    /// <summary>Removes one key by exact value. Returns false if it wasn't in the pool.</summary>
    public bool RemoveKey(string provider, string key)
    {
        var pool = GetPool(provider).ToList();
        var removed = pool.RemoveAll(k => string.Equals(k, key, StringComparison.Ordinal)) > 0;
        if (removed) SetPool(provider, pool);
        return removed;
    }

    /// <summary>Clears the whole pool for <paramref name="provider"/> — falls back to that
    /// adapter's own default credential chain (never the shared Vault key, for Claude).</summary>
    public void Clear(string provider) => SetPool(provider, Array.Empty<string>());
}
