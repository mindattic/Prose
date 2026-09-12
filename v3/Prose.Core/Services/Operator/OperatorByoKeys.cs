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
/// </summary>
public record OperatorByoKeys(string? AnthropicApiKey = null, string? OpenAiApiKey = null);
