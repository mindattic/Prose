namespace Prose.Core.Services.Operator;

/// <summary>
/// Hand-set, explicit "spend MY key, not the shared one" override for the operator tool-calling
/// loop's LLM credentials (<see cref="KdpOperatorService"/> today; any future operator sharing
/// <see cref="IToolCallingLlm"/> gets it for free). Tried before each adapter's own default chain
/// — see <see cref="AnthropicToolCallingLlm"/>/<see cref="OpenAiToolCallingLlm"/>'s DI
/// registration in ServiceCollectionExtensions.cs.
///
/// Stored via <see cref="SettingsKvStore"/> under "operator.byokeys" (added to
/// <see cref="UniverseScope"/>.SharedConfigKeys — a credential is not per-universe content, one
/// row covers every universe). Set/cleared via <c>prose --set-byo-key --provider claude|openai
/// (--key &lt;key&gt; | --clear)</c> (SetByoKeyCli) — no settings UI yet, same as most single-value
/// operator config.
///
/// Mirrors Automata.Core's AutomataSettings BYO-key fields/KeyResolver composition — ported here
/// 2026-09-10 after Prose.KdpPublish's Claude adapter hit a Team-OAuth usage-cap dead end with no
/// way to swap in a personal key. Note the asymmetry this preserves, not removes: Claude's
/// AnthropicToolCallingLlm still never silently falls back to the shared pay-per-token Vault key
/// (2026-08-25 author ruling — an unattended multi-book run could burn real money with no visible
/// warning) — an explicit BYO key is a deliberate opt-in a human typed in, not a silent fallback,
/// so it sits ahead of that guard rather than defeating it.
///
/// 2026-09-13: gained a rotation/failover pool per provider (<see cref="AnthropicApiKeys"/> /
/// <see cref="OpenAiApiKeys"/>), managed via <see cref="OperatorByoKeyPoolService"/> (CLI:
/// <c>prose --set-byo-key --provider claude|openai --key &lt;key&gt; [--key &lt;key&gt; ...] |
/// --add-key &lt;key&gt; | --remove-key &lt;key&gt; | --list | --clear</c>; MCP: the
/// <c>OperatorKeyTools</c> tool class). The original singular fields are kept, unused by any new
/// writer, purely so a value saved before this pool existed still resolves — see
/// <see cref="ResolvedAnthropicKeys"/>/<see cref="ResolvedOpenAiKeys"/>.
/// </summary>
public record OperatorByoKeys(
    string? AnthropicApiKey = null,
    string? OpenAiApiKey = null,
    IReadOnlyList<string>? AnthropicApiKeys = null,
    IReadOnlyList<string>? OpenAiApiKeys = null)
{
    /// <summary>Every Claude key in priority order: the pool when set, else the legacy
    /// single-key field wrapped as a one-element list, else empty.</summary>
    public IReadOnlyList<string> ResolvedAnthropicKeys =>
        AnthropicApiKeys is { Count: > 0 } pool ? pool
        : AnthropicApiKey is { Length: > 0 } single ? new[] { single }
        : Array.Empty<string>();

    /// <summary>Every OpenAI key in priority order: the pool when set, else the legacy
    /// single-key field wrapped as a one-element list, else empty.</summary>
    public IReadOnlyList<string> ResolvedOpenAiKeys =>
        OpenAiApiKeys is { Count: > 0 } pool ? pool
        : OpenAiApiKey is { Length: > 0 } single ? new[] { single }
        : Array.Empty<string>();
}
