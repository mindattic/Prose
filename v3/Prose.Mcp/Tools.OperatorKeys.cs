using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Services.Operator;

namespace Prose.Mcp;

// ── Operator BYO-key pool ─────────────────────────────────────────────────
// Manages the personal API-key pool the KDP operator's tool-calling loop
// (AnthropicToolCallingLlm/OpenAiToolCallingLlm) tries before its own default
// credential chain, failing over key-to-key on an auth/rate-limit/server error
// (KeyPoolFailover). Same underlying OperatorByoKeyPoolService as
// `prose --set-byo-key` (SetByoKeyCli) — either surface sees the other's writes
// immediately, since both read/write the same "operator.byokeys" row.
// ─────────────────────────────────────────────────────────────────────────

[McpServerToolType]
public class OperatorKeyTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };
    private static readonly string[] Providers = OperatorByoKeyPoolService.Providers;

    private readonly OperatorByoKeyPoolService pool;
    private readonly HubInvoker hub;

    public OperatorKeyTools(OperatorByoKeyPoolService pool, HubInvoker hub)
    {
        this.pool = pool;
        this.hub = hub;
    }

    private static string? ValidateProvider(string provider) =>
        Providers.Contains(provider, StringComparer.OrdinalIgnoreCase)
            ? null
            : $"unknown_provider: expected one of {string.Join(", ", Providers)}";

    private static string Mask(string key) => key.Length >= 4 ? "…" + key[^4..] : "(short key)";

    [McpServerTool, Description(
        "List the BYO API key pool configured for the KDP operator's tool-calling loop " +
        "(claude or openai) — the keys tried BEFORE that provider's own default credential " +
        "chain, in priority order, failing over to the next on an auth/rate-limit/server error. " +
        "Keys are returned masked (last 4 chars only). Pass no provider to list both.")]
    public Task<string> ListOperatorByoKeys(
        [Description("'claude' or 'openai'. Omit to list both providers.")] string? provider = null) =>
        hub.InvokeAsync(nameof(OperatorKeyTools), nameof(ListOperatorByoKeysImpl), new { provider });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string ListOperatorByoKeysImpl(string? provider = null)
    {
        var targets = provider is null ? Providers : new[] { provider };
        foreach (var p in targets)
        {
            var err = ValidateProvider(p);
            if (err != null) return JsonSerializer.Serialize(new { error = err }, JsonOpts);
        }

        var result = targets.ToDictionary(
            p => p,
            p => (object)pool.GetPool(p).Select(Mask).ToList());
        return JsonSerializer.Serialize(result, JsonOpts);
    }

    [McpServerTool, Description(
        "Replace the WHOLE BYO API key pool for one operator provider (claude or openai) with " +
        "the given keys, tried in that order and failing over on error. Use add_operator_byo_key / " +
        "remove_operator_byo_key instead to edit the pool incrementally without retyping every key.")]
    public Task<string> SetOperatorByoKeys(
        [Description("'claude' or 'openai'.")] string provider,
        [Description("The full pool, in priority order. An empty list clears the provider (same as clear_operator_byo_keys).")] string[] keys) =>
        hub.InvokeAsync(nameof(OperatorKeyTools), nameof(SetOperatorByoKeysImpl), new { provider, keys });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string SetOperatorByoKeysImpl(string provider, string[] keys)
    {
        var err = ValidateProvider(provider);
        if (err != null) return JsonSerializer.Serialize(new { error = err }, JsonOpts);

        pool.SetPool(provider, keys);
        var current = pool.GetPool(provider);
        return JsonSerializer.Serialize(new { provider, count = current.Count, keys = current.Select(Mask) }, JsonOpts);
    }

    [McpServerTool, Description(
        "Append one key to the end of an operator provider's BYO key pool (tried last, after " +
        "every key already configured). No-op if the exact key is already present.")]
    public Task<string> AddOperatorByoKey(
        [Description("'claude' or 'openai'.")] string provider,
        [Description("The raw API key to add.")] string key) =>
        hub.InvokeAsync(nameof(OperatorKeyTools), nameof(AddOperatorByoKeyImpl), new { provider, key });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string AddOperatorByoKeyImpl(string provider, string key)
    {
        var err = ValidateProvider(provider);
        if (err != null) return JsonSerializer.Serialize(new { error = err }, JsonOpts);
        if (string.IsNullOrWhiteSpace(key)) return JsonSerializer.Serialize(new { error = "key must not be blank" }, JsonOpts);

        pool.AddKey(provider, key);
        var current = pool.GetPool(provider);
        return JsonSerializer.Serialize(new { provider, added = Mask(key), count = current.Count, keys = current.Select(Mask) }, JsonOpts);
    }

    [McpServerTool, Description(
        "Remove one key (by exact value) from an operator provider's BYO key pool. " +
        "Returns removed=false if that key wasn't configured.")]
    public Task<string> RemoveOperatorByoKey(
        [Description("'claude' or 'openai'.")] string provider,
        [Description("The exact raw API key to remove.")] string key) =>
        hub.InvokeAsync(nameof(OperatorKeyTools), nameof(RemoveOperatorByoKeyImpl), new { provider, key });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string RemoveOperatorByoKeyImpl(string provider, string key)
    {
        var err = ValidateProvider(provider);
        if (err != null) return JsonSerializer.Serialize(new { error = err }, JsonOpts);

        var removed = pool.RemoveKey(provider, key);
        var current = pool.GetPool(provider);
        return JsonSerializer.Serialize(new { provider, removed, key = Mask(key), count = current.Count, keys = current.Select(Mask) }, JsonOpts);
    }

    [McpServerTool, Description(
        "Clear the ENTIRE BYO key pool for one operator provider (claude or openai) — falls back " +
        "to that adapter's own default credential chain (never the shared pay-per-token Vault key, " +
        "for Claude).")]
    public Task<string> ClearOperatorByoKeys(
        [Description("'claude' or 'openai'.")] string provider) =>
        hub.InvokeAsync(nameof(OperatorKeyTools), nameof(ClearOperatorByoKeysImpl), new { provider });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string ClearOperatorByoKeysImpl(string provider)
    {
        var err = ValidateProvider(provider);
        if (err != null) return JsonSerializer.Serialize(new { error = err }, JsonOpts);

        pool.Clear(provider);
        return JsonSerializer.Serialize(new { provider, cleared = true }, JsonOpts);
    }
}
